using UnityEngine;

namespace Spellstrike.Vfx
{
    /// <summary>
    /// Strong Attack (10-13 letters) presentation/movement adapter.
    /// The HomingProjectile remains the ONLY owner of the accepted contact,
    /// barrier resolution and word notification. Its approved damage is then
    /// dealt in three timed strikes on the independent Zap VFX root.
    /// No physics events are permitted to resolve a hit before the 0.20s descent.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ArcaneSurgePresentation : MonoBehaviour
    {
        public const float DescentSeconds = 0.20f;
        private const float MissingYellowEventTimeoutSeconds = 2.0f;

        [Header("Vertical descent (presentation only)")]
        // Long, screen-dominant strike like the Vefects reference:
        // the descending tip starts far above the live target's head.
        [SerializeField, Min(3f)] private float heightAboveHead = 18f;
        [SerializeField, Min(0f)] private float headInset = 0.08f;

        private Transform target;
        private Collider targetCollider;
        private SphereCollider projectileCollider;
        private HomingProjectile owner;
        private ArcaneSurgeZapVisual visual;
        private float elapsed;
        private float aliveElapsed;
        private bool completed;
        private bool yellowImpactRequested;
        public bool DescentComplete => completed;
        public ArcaneSurgeZapVisual ZapVisual => visual;

        /// <summary>
        /// The first impact is authorized by the StrongAttack clip's Yellow
        /// animation event, never by a self-timed 0.20-second hit callback.
        /// Update resolves it after both the event and the visible descent.
        /// </summary>
        public void RequestYellowImpact()
        {
            if (!completed && !yellowImpactRequested)
                yellowImpactRequested = true;
        }

        /// <summary>
        /// Called by HomingProjectile after its single barrier/word resolution.
        /// The visual root survives projectile destruction and delivers the
        /// three timed HP hits without re-running that resolution.
        /// </summary>
        public bool QueueDamageSequence(
            int approvedDamage,
            int wordLength,
            EnemyController enemy,
            BrambleSpriteController bramble,
            EnemyHealth health)
        {
            return completed && visual != null
                && visual.ConfigureDamageSequence(approvedDamage, wordLength, enemy, bramble, health);
        }

        public bool Begin(Transform liveTarget)
        {
            owner = GetComponent<HomingProjectile>();
            if (liveTarget == null || owner == null)
                return false;

            target = liveTarget;
            targetCollider = FindTargetCollider(target);
            if (targetCollider == null)
            {
                Debug.LogWarning("[ArcaneSurge] No target collider; retain normal homing behavior.", this);
                return false;
            }

            projectileCollider = GetComponent<SphereCollider>();
            if (projectileCollider == null)
                return false;

            ArcaneSurgeZapLibrary library = Resources.Load<ArcaneSurgeZapLibrary>(
                ArcaneSurgeZapLibrary.ResourcePath);
            if (library == null || !library.IsConfigured)
            {
                Debug.LogWarning(
                    "[ArcaneSurge] Imported Zap VFX library is missing or incomplete. "
                    + "Keeping the original homing projectile and effects.",
                    this);
                return false;
            }

            GameObject vfx = new GameObject("Arcane Surge - imported Vefects Zap VFX");
            visual = vfx.AddComponent<ArcaneSurgeZapVisual>();
            if (!visual.Initialize(library))
            {
                Destroy(vfx);
                return false;
            }

            // Disable the existing projectile collider only for the presentation
            // descent. The real impact still passes through ResolveAcceptedImpact.
            projectileCollider.enabled = false;
            Transform oldOrb = transform.Find("MagicOrb Visual");
            if (oldOrb != null)
                oldOrb.gameObject.SetActive(false);

            visual.SetDescent(GetHeadPoint(), heightAboveHead, 0f);
            visual.BeginReleaseLightning();
            return true;
        }

        private static Collider FindTargetCollider(Transform candidate)
        {
            if (candidate == null) return null;
            // The explicit attack target can be a head/SpellAimPoint child.
            // Prefer the nearest damage/barrier owner over any unrelated collider.
            Component receiver = candidate.GetComponentInParent<VocabularyWorldTarget>();
            if (receiver == null) receiver = candidate.GetComponentInParent<VocabularyBarrier>();
            if (receiver == null) receiver = candidate.GetComponentInParent<EnemyController>();
            if (receiver == null) receiver = candidate.GetComponentInParent<BrambleSpriteController>();
            if (receiver == null) receiver = candidate.GetComponentInParent<EnemyHealth>();
            Transform root = receiver != null ? receiver.transform : candidate;
            // A live vocabulary barrier must continue to receive the spell,
            // rather than bypassing its damage adjustment via a sibling body.
            VocabularyBarrier barrier = root.GetComponentInChildren<VocabularyBarrier>();
            if (barrier != null && barrier.BarrierActive)
            {
                // Never route around an active barrier. If there is no safe
                // collider under it, Begin returns false and the original
                // projectile path handles this special encounter instead.
                return FirstCollider(barrier.transform);
            }
            return FirstCollider(root);
        }

        private static Collider FirstCollider(Transform root)
        {
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            Collider triggerFallback = null;
            foreach (Collider collider in colliders)
            {
                if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy)
                    continue;
                // A model/weapon/range collider on an arbitrary parent could be
                // hit visually without reaching the existing damage receiver.
                // Reject it and retain the legacy projectile fallback instead.
                if (collider.GetComponentInParent<EnemyController>() == null
                    && collider.GetComponentInParent<BrambleSpriteController>() == null
                    && collider.GetComponentInParent<EnemyHealth>() == null
                    && collider.GetComponentInParent<VocabularyWorldTarget>() == null)
                    continue;
                if (!collider.isTrigger) return collider;
                if (triggerFallback == null) triggerFallback = collider;
            }
            return triggerFallback;
        }

        private Vector3 GetHeadPoint()
        {
            if (target == null) return transform.position;

            // Target Challenge targets are deliberate hit points, not humanoid heads.
            if (target.GetComponentInParent<VocabularyWorldTarget>() != null)
                return target.position;

            Animator animator = target.GetComponentInParent<Animator>();
            if (animator != null && animator.isHuman)
            {
                Transform headBone = animator.GetBoneTransform(HumanBodyBones.Head);
                if (headBone != null) return headBone.position;
            }

            if (targetCollider != null)
            {
                Bounds bounds = targetCollider.bounds; // updates with moving targets
                return new Vector3(bounds.center.x, bounds.max.y - headInset, bounds.center.z);
            }
            return target.position + Vector3.up;
        }

        private void Update()
        {
            if (completed) return;
            if (target == null || targetCollider == null || !targetCollider.enabled)
            {
                Abort();
                return;
            }

            // No silent hit if the clip transitions or its Yellow event is
            // skipped: eventually cancel the visual-only hanging projectile.
            aliveElapsed += Time.deltaTime;
            if (!yellowImpactRequested && aliveElapsed >= MissingYellowEventTimeoutSeconds)
            {
                Abort();
                return;
            }

            // Time is measured from OnStrongAttackZapRelease via Begin.
            elapsed = Mathf.Min(DescentSeconds, elapsed + Time.deltaTime);
            float t = elapsed / DescentSeconds;
            Vector3 head = GetHeadPoint();
            Vector3 start = head + Vector3.up * heightAboveHead;
            Vector3 tip = Vector3.LerpUnclamped(start, head, t * t * (3f - 2f * t));
            transform.position = tip; // physical projectile converges with the bolt
            if (visual != null) visual.SetDescent(head, heightAboveHead, t);

            // The clip controls the release of the first hit. If its frame
            // arrives slightly before Update crosses 0.20s, honor the request
            // once the minimum descent is complete rather than hitting early.
            if (elapsed < DescentSeconds || !yellowImpactRequested) return;

            completed = true;
            // Do not rely on an OnTriggerEnter that teleportation can skip.
            // Exactly one explicit contact uses the ORIGINAL accepted-impact path.
            Collider acceptedCollider = FindTargetCollider(target);
            if (acceptedCollider == null)
            {
                Abort();
                return;
            }
            if (projectileCollider != null) projectileCollider.enabled = true;
            // Capture the ground contact before ResolveAcceptedImpact can kill,
            // disable, or replace the enemy's visual/collider hierarchy.
            float groundY = acceptedCollider.bounds.min.y;
            transform.position = acceptedCollider.ClosestPoint(head);
            // Store the moving target on the independent visual BEFORE a
            // lethal first hit can destroy the projectile or enemy hierarchy.
            if (visual != null)
                visual.SetImpactTarget(target, acceptedCollider);
            if (owner != null && owner.ResolveArcaneSurgeContact(acceptedCollider))
            {
                if (visual != null) visual.ConfirmedImpact(head, groundY);
            }
            else
            {
                Abort();
            }
        }

        private void Abort()
        {
            completed = true;
            if (visual != null) visual.Abort();
            // Target Challenge cannot remain frozen when the target disappeared.
            // Ordinary shots simply expire without applying phantom damage.
            if (owner != null)
                owner.NotifyArcaneSurgeCancelled();
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            // Unlike the projectile, the VFX root is independent so a lethal hit
            // can leave its aftershock behind. Unexpected projectile destruction
            // during descent must NOT leave a permanently animating beam.
            if (!completed && visual != null)
                visual.Abort();
        }
    }
}
