using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Spellstrike.Vfx
{
    /// <summary>
    /// Strong-only imported Vefects Zap VFX presentation.
    /// Three imported Zap hits are triggered by StrongAttack Animation Events:
    /// Yellow, DarkBlue, CyanBlue at three locations INSIDE the cast magic circle.
    /// The accepted word/barrier resolves ONCE, then each lightning strike
    /// applies its own integer portion of the approved total damage.
    /// No vendor prefab, shared material, or Basic/Medium asset is modified.
    ///
    /// The initial 0.20-second descent and vendor impact particles terminate
    /// on the GROUND, never on a head bone. A separate animation event plays
    /// the Hovl Plexus AoE and Lightning Aura as a visual-only aftershock.
    /// </summary>
    // After the circle's LateUpdate: floor lightning and chest aftershocks
    // render from the newest homing / death-animation positions.
    [DefaultExecutionOrder(550)]
    [DisallowMultipleComponent]
    public sealed class ArcaneSurgeZapVisual : MonoBehaviour
    {
        private enum Variant { Yellow, DarkBlue, CyanBlue }

        private const int BoltPoints = 13;
        public const float ImpactIntervalSeconds = 0.20f;
        private const float EffectCleanupSeconds = 5.00f;
        // Double-Giant Zap preset. The source Vefects Zap billboard uses
        // startSizeX for width and startSizeY for its vertical bolt length.
        // These are multipliers relative to ORIGINAL vendor sizes; do not
        // multiply the previous modifier settings again.
        private const float CoreBoltWidth = 3.00f;
        private const float BackgroundBoltWidth = 2.70f;
        private const float BranchBoltWidth = 2.50f;
        private const float CoreBoltLength = 1.90f;
        private const float BackgroundBoltLength = 1.65f;
        private const float BranchBoltLength = 1.55f;
        // Exactly twice the previous 1.45x runtime prefab scale. The release
        // bolt reuses this same scaled prefab, not a separate skinny tracer.
        private const float OverallZapScale = 2.90f;
        private static readonly int EmissiveIntensityId = Shader.PropertyToID("_EmissiveIntensity");

        private ArcaneSurgeZapLibrary library;
        private LineRenderer brightTracer;
        private LineRenderer softTracer;
        private GameObject releaseZap;
        private readonly List<GameObject> releaseHiddenSystems = new List<GameObject>(6);
        private readonly Vector3[] cachedBoltPositions = new Vector3[BoltPoints];
        private Material tracerMaterial;
        private bool ownsMaterial;
        private Vector3 groundPoint;
        private float height;
        private float descent01;
        private bool impacted;
        private bool aborted;
        private int nextImpactIndex;
        private float seed;
        private Vector3 areaCenter;
        private float areaRadius;
        private readonly Vector3[] areaStrikePoints = new Vector3[3];
        private bool areaConfigured;
        private bool aftershockTriggered;
        private ArcaneSurgeGroundField groundField;
        private static readonly RaycastHit[] FeetGroundHits = new RaycastHit[32];
        private sealed class AffectedEnemy
        {
            public int Id;
            public Component Receiver;
            public Transform LeftFoot;
            public Transform RightFoot;
            public Collider BodyCollider;
            public Vector3 ColliderLocalFeet;
            public Transform AftershockRoot;
            public Vector3 LastFeet;
            public float BodySize;

            public void FollowFeet()
            {
                // Track the midpoint between the animated feet, projected
                // onto solid ground. A generic-rig/body-collider fallback
                // also works for enemies without humanoid foot bones.
                if (Receiver != null && Receiver.gameObject.activeInHierarchy)
                    LastFeet = GroundPointBetweenFeet(
                        Receiver, LeftFoot, RightFoot, BodyCollider,
                        ColliderLocalFeet, LastFeet, BodySize);

                // Effects belong to this independent VFX object, not the
                // enemy GameObject, so death/despawn cannot destroy them.
                // If the enemy disappears, they stay at its LAST ground point.
                if (AftershockRoot != null)
                    AftershockRoot.position = LastFeet;
            }
        }
        private readonly List<AffectedEnemy> affectedEnemies = new List<AffectedEnemy>();
        private Transform movingTarget;
        private Collider movingCollider;
        private Vector3 lastContact;
        private float lastGroundY;
        private EnemyController damageEnemy;
        private BrambleSpriteController damageBramble;
        private EnemyHealth damageHealth;
        private int damageWordLength;
        private int totalApprovedDamage;
        private bool damageSequenceConfigured;

        public void SetGroundField(ArcaneSurgeGroundField field)
        {
            if (field == null) return;
            groundField = field;
            RefreshGroundArea();
        }

        private void RefreshGroundArea()
        {
            if (groundField == null)
                return; // Hold the last valid AoE center if its owner despawns.
            areaCenter = groundField.Center;
            areaRadius = groundField.Radius;
            for (int i = 0; i < 3; i++)
                areaStrikePoints[i] = groundField.StrikePoint(i);
            areaConfigured = true;
        }

        /// <summary>
        /// Divide a single approved damage total without losing remainder.
        /// 30 => 10/10/10, 31 => 10/10/11, 32 => 10/11/11.
        /// </summary>
        public static int DamageForStrike(int approvedDamage, int strikeIndex)
        {
            if (strikeIndex < 0 || strikeIndex >= 3)
                return 0;
            int nonnegative = Mathf.Max(0, approvedDamage);
            int baseShare = nonnegative / 3;
            int remainder = nonnegative % 3;
            return baseShare + (strikeIndex >= 3 - remainder ? 1 : 0);
        }

        /// <summary>
        /// Called once by the original projectile after it resolves the
        /// vocabulary barrier and combat modifiers. Never resolves the word
        /// or calls the attack owner again.
        /// </summary>
        public bool ConfigureDamageSequence(
            int approvedDamage, int wordLength, EnemyController enemy,
            BrambleSpriteController bramble, EnemyHealth health)
        {
            if (impacted || aborted || damageSequenceConfigured || approvedDamage <= 0
                || (enemy == null && bramble == null && health == null))
                return false;

            totalApprovedDamage = approvedDamage;
            damageWordLength = wordLength;
            damageEnemy = enemy;
            damageBramble = enemy == null ? bramble : null;
            damageHealth = enemy == null && bramble == null ? health : null;
            damageSequenceConfigured = true;
            return true;
        }

        private void ApplyDamageStrike(int strikeIndex)
        {
            if (!damageSequenceConfigured || !areaConfigured)
                return;
            RefreshGroundArea();

            int splitDamage = DamageForStrike(totalApprovedDamage, strikeIndex);
            if (splitDamage <= 0)
                return;
            // Exactly three event-driven impacts per cast: refresh the physics
            // world here (not every frame) so moving enemies' collider bounds
            // agree with the homing circle at the instant AoE damage is dealt.
            Physics.SyncTransforms();

            // The accepted projectile resolves the word/barrier EXACTLY ONCE.
            // Every actual enemy within the circle receives only this strike's
            // portion. Re-query at each event so moving enemies can enter/exit.
            // Never damage outside the visible circle, hit dead actors, or
            // bypass a second enemy's active word barrier.
            foreach (EnemyController enemy in
                Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
            {
                if (enemy == null || !enemy.gameObject.activeInHierarchy ||
                    enemy.currentState == EnemyController.EnemyState.Dead ||
                    !IsInsideCircle(enemy) || IsProtectedSecondaryTarget(enemy))
                    continue;
                float healthBefore = enemy.CurrentHealth;
                Collider bodyBefore = FindBodyCollider(enemy);
                float bodySizeBefore = GetVisualBodySize(enemy, bodyBefore);
                Vector3 feetBefore = FindFeetPosition(enemy);
                enemy.TakeDamage(splitDamage, damageWordLength);
                if (enemy.CurrentHealth < healthBefore)
                    RememberAffectedEnemy(enemy, feetBefore, bodySizeBefore, bodyBefore);
            }

            foreach (BrambleSpriteController bramble in
                Object.FindObjectsByType<BrambleSpriteController>(FindObjectsSortMode.None))
            {
                if (bramble == null || !bramble.gameObject.activeInHierarchy ||
                    bramble.currentState == BrambleSpriteController.EnemyState.Dead ||
                    bramble.GetComponentInParent<EnemyController>() != null ||
                    !IsInsideCircle(bramble) || IsProtectedSecondaryTarget(bramble))
                    continue;
                float healthBefore = bramble.CurrentHealth;
                Collider bodyBefore = FindBodyCollider(bramble);
                float bodySizeBefore = GetVisualBodySize(bramble, bodyBefore);
                Vector3 feetBefore = FindFeetPosition(bramble);
                bramble.TakeDamage(splitDamage);
                if (bramble.CurrentHealth < healthBefore)
                    RememberAffectedEnemy(bramble, feetBefore, bodySizeBefore, bodyBefore);
            }

            foreach (EnemyHealth health in
                Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
            {
                if (health == null || !health.gameObject.activeInHierarchy ||
                    health.IsDead || health.IsInvulnerable ||
                    health.GetComponentInParent<EnemyController>() != null ||
                    health.GetComponentInParent<BrambleSpriteController>() != null ||
                    !IsInsideCircle(health) || IsProtectedSecondaryTarget(health))
                    continue;
                float healthBefore = health.currentHealth;
                Collider bodyBefore = FindBodyCollider(health);
                float bodySizeBefore = GetVisualBodySize(health, bodyBefore);
                Vector3 feetBefore = FindFeetPosition(health);
                health.TakeDamage(splitDamage);
                if (health.currentHealth < healthBefore)
                    RememberAffectedEnemy(health, feetBefore, bodySizeBefore, bodyBefore);
            }
        }

        private void RememberAffectedEnemy(Component receiver, Vector3 beforeDamageFeet,
            float bodySizeBefore, Collider bodyBefore)
        {
            if (receiver == null) return;
            int id = receiver.GetInstanceID();
            foreach (AffectedEnemy existing in affectedEnemies)
            {
                if (existing.Id != id) continue;
                existing.FollowFeet();
                return;
            }

            FindFootBones(receiver, out Transform leftFoot, out Transform rightFoot);
            // Snapshot the size and body collider BEFORE lethal damage can
            // deactivate its mesh/colliders. Giant bosses must keep their
            // appropriately scaled aftershock even if they die on Yellow.
            Collider body = bodyBefore;
            float bodySize = bodySizeBefore;
            Vector3 localFeet = body != null
                ? body.transform.InverseTransformPoint(
                    new Vector3(body.bounds.center.x, body.bounds.min.y, body.bounds.center.z))
                : Vector3.zero;
            AffectedEnemy entry = new AffectedEnemy
            {
                Id = id,
                Receiver = receiver,
                LeftFoot = leftFoot,
                RightFoot = rightFoot,
                BodyCollider = body,
                ColliderLocalFeet = localFeet,
                LastFeet = beforeDamageFeet,
                BodySize = bodySize
            };
            // If lethal damage immediately hides the enemy, retain the
            // pre-hit ground midpoint rather than its vanished rig transform.
            entry.FollowFeet();
            affectedEnemies.Add(entry);
        }

        private static Collider FindBodyCollider(Component receiver)
        {
            Collider fallback = null;
            foreach (Collider collider in receiver.GetComponentsInChildren<Collider>(true))
            {
                if (collider == null || collider.isTrigger || !collider.enabled)
                    continue;
                if (collider is CapsuleCollider || collider is CharacterController)
                    return collider;
                if (fallback == null)
                    fallback = collider;
            }
            return fallback;
        }

        /// <summary>
        /// Measure the actual visible enemy body instead of the spell radius.
        /// Prefer skinned character meshes (including giants); otherwise use
        /// a body collider or visible static mesh/sprite. Ignore particles,
        /// weapons-only trail renderers and HUD. The fallback is deliberately
        /// small rather than giving tiny enemies an oversized effect.
        /// </summary>
        private static float GetVisualBodySize(Component receiver, Collider body)
        {
            Bounds visualBounds = default;
            bool hasSkinnedBody = false;
            foreach (SkinnedMeshRenderer renderer in
                receiver.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer == null || !renderer.enabled ||
                    !renderer.gameObject.activeInHierarchy ||
                    renderer.bounds.size.sqrMagnitude <= 0.0001f)
                    continue;
                if (!hasSkinnedBody)
                    visualBounds = renderer.bounds;
                else
                    visualBounds.Encapsulate(renderer.bounds);
                hasSkinnedBody = true;
            }

            if (hasSkinnedBody)
                return SizeFromBounds(visualBounds);

            if (body != null && body.bounds.size.sqrMagnitude > 0.0001f)
                return SizeFromBounds(body.bounds);

            bool hasMesh = false;
            foreach (Renderer renderer in receiver.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || !renderer.enabled ||
                    !renderer.gameObject.activeInHierarchy ||
                    !(renderer is MeshRenderer || renderer is SpriteRenderer) ||
                    renderer.bounds.size.sqrMagnitude <= 0.0001f)
                    continue;
                if (!hasMesh)
                    visualBounds = renderer.bounds;
                else
                    visualBounds.Encapsulate(renderer.bounds);
                hasMesh = true;
            }
            return hasMesh ? SizeFromBounds(visualBounds) : 1.0f;
        }

        private static float SizeFromBounds(Bounds bounds)
        {
            float width = Mathf.Max(bounds.size.x, bounds.size.z);
            return Mathf.Max(0.25f, bounds.size.y, width * 0.9f);
        }

        // Hovl Plexus's Glow particles have a ~4-unit source size, so a
        // 2-unit-tall enemy at 0.9x produces a visible ~3.6-unit effect.
        // Both VFX scale proportionally with the individual character,
        // independently from the Magic Circle and the AoE damage radius.
        public static float PlexusScaleForEnemySize(float bodySize) =>
            Mathf.Clamp(bodySize * 0.45f, 0.3f, 14f);

        public static float AuraScaleForEnemySize(float bodySize) =>
            Mathf.Clamp(bodySize * 0.65f, 0.35f, 18f);

        private static Vector3 FindFeetPosition(Component receiver)
        {
            if (receiver == null) return Vector3.zero;
            FindFootBones(receiver, out Transform leftFoot, out Transform rightFoot);
            Collider body = FindBodyCollider(receiver);
            Vector3 fallback = receiver.transform.position;
            Vector3 localFeet = Vector3.zero;
            if (body != null)
            {
                Bounds bounds = body.bounds;
                fallback = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                localFeet = body.transform.InverseTransformPoint(fallback);
            }
            return GroundPointBetweenFeet(receiver, leftFoot, rightFoot,
                body, localFeet, fallback, GetVisualBodySize(receiver, body));
        }

        private static void FindFootBones(Component receiver,
            out Transform leftFoot, out Transform rightFoot)
        {
            leftFoot = null;
            rightFoot = null;
            if (receiver == null) return;

            Animator animator = receiver.GetComponentInChildren<Animator>(true);
            if (animator == null)
                animator = receiver.GetComponentInParent<Animator>();
            if (animator != null && animator.isHuman)
            {
                leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            }
            if (leftFoot != null && rightFoot != null) return;

            // Support generic, non-humanoid enemy skeletons. Accept common
            // naming patterns such as LeftFoot, mixamorig:RightFoot,
            // Foot_L, L_Foot, Bip001 L Foot and matching ankle bones.
            int bestLeft = -1, bestRight = -1;
            foreach (Transform candidate in receiver.GetComponentsInChildren<Transform>(true))
            {
                string lower = candidate.name.ToLowerInvariant();
                bool foot = lower.Contains("foot");
                bool ankle = lower.Contains("ankle");
                if (!foot && !ankle) continue;
                int score = foot ? 2 : 1;
                bool left = lower.Contains("left") || lower.EndsWith("_l") ||
                    lower.StartsWith("l_") || lower.Contains(" l foot") ||
                    lower.Contains(" l ankle");
                bool right = lower.Contains("right") || lower.EndsWith("_r") ||
                    lower.StartsWith("r_") || lower.Contains(" r foot") ||
                    lower.Contains(" r ankle");
                if (left && !right && (leftFoot == null || bestLeft >= 0) &&
                    score > bestLeft)
                {
                    leftFoot = candidate;
                    bestLeft = score;
                }
                if (right && !left && (rightFoot == null || bestRight >= 0) &&
                    score > bestRight)
                {
                    rightFoot = candidate;
                    bestRight = score;
                }
            }
        }

        private static Vector3 GroundPointBetweenFeet(
            Component receiver, Transform leftFoot, Transform rightFoot,
            Collider body, Vector3 colliderLocalFeet, Vector3 lastKnown,
            float bodySize)
        {
            if (receiver == null || !receiver.gameObject.activeInHierarchy)
                return lastKnown; // Dead/despawned: leave the visual on ground.

            bool leftActive = leftFoot != null && leftFoot.gameObject.activeInHierarchy;
            bool rightActive = rightFoot != null && rightFoot.gameObject.activeInHierarchy;
            Vector3 feet;
            if (leftActive && rightActive)
            {
                // Use the horizontal midpoint, with the lower foot as the
                // reference Y so walking/running doesn't raise the effect.
                feet = (leftFoot.position + rightFoot.position) * 0.5f;
                feet.y = Mathf.Min(leftFoot.position.y, rightFoot.position.y);
            }
            else if (leftActive || rightActive)
                feet = leftActive ? leftFoot.position : rightFoot.position;
            else if (body != null && body.enabled && body.gameObject.activeInHierarchy)
                feet = body.transform.TransformPoint(colliderLocalFeet);
            else
                return lastKnown;

            // The anchor remains at floor level even when a death animation
            // collapses the character or a leg rises during a step. Ignore
            // actors and prefer the highest solid, walkable floor below feet.
            float maxHeight = Mathf.Max(8f, bodySize + 6f);
            Vector3 origin = feet + Vector3.up * Mathf.Max(1.5f, bodySize * 0.15f);
            int count = Physics.RaycastNonAlloc(origin, Vector3.down,
                FeetGroundHits, maxHeight,
                ~((1 << 2) | (1 << 5)), QueryTriggerInteraction.Ignore);
            float floorY = float.NegativeInfinity;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = FeetGroundHits[i];
                if (hit.collider == null || hit.normal.y < 0.55f ||
                    hit.point.y > feet.y + 0.65f ||
                    hit.collider.GetComponentInParent<EnemyController>() != null ||
                    hit.collider.GetComponentInParent<BrambleSpriteController>() != null ||
                    hit.collider.GetComponentInParent<EnemyHealth>() != null)
                    continue;
                floorY = Mathf.Max(floorY, hit.point.y);
            }
            if (!float.IsNegativeInfinity(floorY))
                feet.y = floorY;
            return feet + Vector3.up * 0.06f; // Prevent ground Z-fighting.
        }

        private bool IsProtectedSecondaryTarget(Component receiver)
        {
            bool primary = receiver == damageEnemy ||
                receiver == damageBramble || receiver == damageHealth;
            if (primary) return false; // It already passed the approved barrier.

            VocabularyBarrier barrier = receiver.GetComponentInChildren<VocabularyBarrier>();
            return barrier != null && barrier.BarrierActive;
        }

        private bool IsInsideCircle(Component receiver)
        {
            // Use solid collider footprints, not the head (a giant boss can be
            // taller than the whole circle). The damage area is a horizontal
            // disc matching the visible imported magic-circle diameter.
            float radiusSquared = areaRadius * areaRadius;
            bool hadCollider = false;
            foreach (Collider hitbox in receiver.GetComponentsInChildren<Collider>())
            {
                if (hitbox == null || !hitbox.enabled || hitbox.isTrigger ||
                    !hitbox.gameObject.activeInHierarchy)
                    continue;
                hadCollider = true;
                Bounds bounds = hitbox.bounds;
                float dx = Mathf.Max(bounds.min.x - areaCenter.x, 0f,
                    areaCenter.x - bounds.max.x);
                float dz = Mathf.Max(bounds.min.z - areaCenter.z, 0f,
                    areaCenter.z - bounds.max.z);
                if (dx * dx + dz * dz <= radiusSquared)
                    return true;
            }
            if (hadCollider)
                return false;

            Vector3 origin = receiver.transform.position;
            float offsetX = origin.x - areaCenter.x;
            float offsetZ = origin.z - areaCenter.z;
            return offsetX * offsetX + offsetZ * offsetZ <= radiusSquared;
        }

        public bool Initialize(ArcaneSurgeZapLibrary loadedLibrary)
        {
            if (loadedLibrary == null || !loadedLibrary.IsConfigured)
                return false;

            library = loadedLibrary;
            // The first descent ends in the Yellow hit. Subsequent hits use
            // their own imported vendor colors and approved damage shares.
            Color yellow = new Color(1.0f, 0.85f, 0.16f, 1f);

            tracerMaterial = Resources.Load<Material>("Spellstrike/VFX/ArcaneSurgeEnergy");
            if (tracerMaterial == null)
            {
                Shader shader = Shader.Find("SPELLSTRIKE/VFX/BasicChargeEnergyMobileSafe");
                if (shader == null)
                {
                    Debug.LogWarning("[ArcaneSurge] No URP lightning tracer material is available.");
                    return false;
                }
                tracerMaterial = new Material(shader);
                ownsMaterial = true;
            }

            softTracer = CreateTracer("Imported Zap - descending halo", 2.40f, yellow * 0.60f);
            brightTracer = CreateTracer("Imported Zap - descending core", 1.10f,
                Color.Lerp(yellow, Color.white, 0.72f));
            seed = Time.time * 33.1f + GetInstanceID() * 0.013f;
            return true;
        }

        /// <summary>
        /// Capture the accepted enemy BEFORE the original hit can kill it.
        /// The bolts stay at their three ground positions inside the circle;
        /// only the eventual Lightning Aura follows a surviving enemy.
        /// </summary>
        public void SetImpactTarget(Transform acceptedTarget, Collider acceptedCollider)
        {
            movingTarget = acceptedTarget;
            movingCollider = acceptedCollider;
        }

        private LineRenderer CreateTracer(string label, float width, Color tint)
        {
            GameObject child = new GameObject(label);
            child.transform.SetParent(transform, false);
            LineRenderer line = child.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.sharedMaterial = tracerMaterial;
            line.alignment = LineAlignment.View;
            line.positionCount = BoltPoints;
            line.widthMultiplier = width;
            line.numCornerVertices = 0;
            line.numCapVertices = 0;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            line.startColor = tint;
            line.endColor = tint;
            return line;
        }

        public void SetDescent(Vector3 groundLanding, float skyHeight, float time01)
        {
            if (impacted || aborted) return;
            groundPoint = groundLanding;
            height = skyHeight;
            descent01 = Mathf.Clamp01(time01);
        }

        /// <summary>
        /// Started by the OnStrongAttackZapRelease animation event through
        /// ArcaneSurgePresentation.Begin. The release uses the SAME scaled
        /// imported Yellow bolt as the impact. We suppress its impact-only
        /// sparks/flare until the Yellow contact is accepted.
        /// </summary>
        public void BeginReleaseLightning()
        {
            if (releaseZap != null || impacted || aborted || library == null
                || library.Yellow == null)
                return;

            releaseZap = Instantiate(library.Yellow,
                groundPoint + Vector3.up * height, Quaternion.identity, transform);
            releaseZap.name = "ArcaneSurge_ReleaseLightning_Yellow";
            PrepareGiantZap(releaseZap, Variant.Yellow);

            // The vendor main root also has a particle emitter; stop it for
            // the wind-up without deactivating its child lightning hierarchy.
            foreach (ParticleSystem particle in
                releaseZap.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (particle.gameObject == releaseZap)
                {
                    particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                    continue;
                }

                if (!IsMainLightningLayer(particle.gameObject.name))
                {
                    particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                    if (particle.gameObject.activeSelf)
                    {
                        releaseHiddenSystems.Add(particle.gameObject);
                        particle.gameObject.SetActive(false);
                    }
                    continue;
                }

                if (particle.gameObject.activeInHierarchy)
                {
                    particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                    particle.Play(false);
                }
            }

            // Keep these as visual fallbacks only; the actual release has
            // the same giant Vefects bolt proportions as each hit.
            brightTracer.enabled = false;
            softTracer.enabled = false;
        }

        private static bool IsMainLightningLayer(string name)
        {
            return name == "Zap" || name == "Zap LUT"
                || name == "Zap BG" || name == "Zap Add";
        }

        private void LateUpdate()
        {
            RefreshGroundArea();
            foreach (AffectedEnemy enemy in affectedEnemies)
                enemy.FollowFeet();

            if (impacted || aborted || brightTracer == null || softTracer == null)
                return;

            // Both endpoints are a FIXED ground point within the cast circle.
            // Only the tip descends from the sky to the ground over exactly
            // ArcaneSurgePresentation.DescentSeconds (0.20 seconds).
            Vector3 top = groundPoint + Vector3.up * height;
            float smooth = descent01 * descent01 * (3f - 2f * descent01);
            Vector3 tip = Vector3.LerpUnclamped(top, groundPoint, smooth);
            // Vefects uses local-space lightning particles, so the giant
            // release bolt follows the moving target's descending tip.
            if (releaseZap != null)
                releaseZap.transform.position = tip;
            for (int i = 0; i < BoltPoints; i++)
            {
                float v = i / (float)(BoltPoints - 1);
                float wave = Mathf.Sin(seed + v * 19.5f + Time.time * 43f) * 0.31f
                    + Mathf.Sin(seed * 0.5f + v * 39f - Time.time * 17f) * 0.10f;
                float envelope = Mathf.Sin(v * Mathf.PI);
                cachedBoltPositions[i] = Vector3.LerpUnclamped(top, tip, v)
                    + new Vector3(wave * envelope, 0f,
                        Mathf.Sin(seed + v * 24f) * 0.22f * envelope);
            }
            brightTracer.SetPositions(cachedBoltPositions);
            softTracer.SetPositions(cachedBoltPositions);
        }

        public void ConfirmedImpact(Vector3 contact, float groundY)
        {
            if (impacted || aborted) return;
            impacted = true;
            if (brightTracer != null) brightTracer.enabled = false;
            if (softTracer != null) softTracer.enabled = false;
            lastContact = contact;
            lastGroundY = groundY;

            // This event fires ONLY after HomingProjectile's accepted contact.
            ApplyDamageStrike(0);
            // Three distinct camera punches: build toward a heavier finisher.
            EmitVisualImpact(Variant.Yellow, 0.62f, 0.60f, 0.18f);
            nextImpactIndex = 1;
            Destroy(gameObject, EffectCleanupSeconds);
        }

        /// <summary>Invoked only by OnStrongZapDarkBlue Animation Event.</summary>
        public void TriggerDarkBlueImpact()
        {
            if (!impacted || aborted || nextImpactIndex != 1)
                return;
            nextImpactIndex = 2;
            SelectGroundStrike(1);
            ApplyDamageStrike(1);
            EmitVisualImpact(Variant.DarkBlue, 0.55f, 0.85f, 0.20f);
        }

        /// <summary>Invoked only by OnStrongZapCyanBlue Animation Event.</summary>
        public void TriggerCyanBlueImpact()
        {
            if (!impacted || aborted || nextImpactIndex != 2)
                return;
            nextImpactIndex = 3;
            SelectGroundStrike(2);
            ApplyDamageStrike(2);
            EmitVisualImpact(Variant.CyanBlue, 0.70f, 1.15f, 0.26f);
        }

        private void SelectGroundStrike(int strikeIndex)
        {
            // Each strike uses the MOVING ground-circle center, not an
            // abandoned cast position or the target's overhead head bone.
            RefreshGroundArea();
            if (!areaConfigured) return;
            lastContact = areaStrikePoints[Mathf.Clamp(strikeIndex, 0, 2)];
            lastGroundY = areaCenter.y;
        }

        /// <summary>
        /// Invoked by OnStrongAftershock on the StrongAttack clip, after Cyan
        /// and before animation finish. Both Hovl effects are visual-only.
        /// </summary>
        public void TriggerAftershock()
        {
            if (!impacted || aborted || nextImpactIndex != 3 ||
                aftershockTriggered || library == null || !areaConfigured ||
                !damageSequenceConfigured)
                return;
            aftershockTriggered = true;
            // One Plexus AND one Lightning Aura for EACH enemy actually hit.
            // A runtime Foot VFX Anchor is positioned between the enemy's
            // feet at ground level and follows locomotion/death animation.
            // When the enemy disappears, it stays at that last floor point.
            foreach (AffectedEnemy enemy in affectedEnemies)
            {
                enemy.FollowFeet();
                GameObject anchor = new GameObject(
                    "OnStrongAftershock - Foot VFX Anchor - " +
                    (enemy.Receiver != null ? enemy.Receiver.name : enemy.Id.ToString()));
                anchor.transform.SetParent(transform, true);
                anchor.transform.position = enemy.LastFeet;
                enemy.AftershockRoot = anchor.transform;

                if (library.PlexusAoe != null)
                {
                    GameObject plexus = Instantiate(library.PlexusAoe,
                        enemy.LastFeet, Quaternion.identity, anchor.transform);
                    plexus.name = "OnStrongAftershock - Imported Hovl Plexus AoE";
                    plexus.transform.localScale *=
                        PlexusScaleForEnemySize(enemy.BodySize);
                    DisableRealTimeLights(plexus);
                }

                if (library.LightningAura != null)
                {
                    GameObject aura = Instantiate(library.LightningAura,
                        enemy.LastFeet, Quaternion.identity, anchor.transform);
                    aura.name = "OnStrongAftershock - Imported Hovl Lightning Aura";
                    aura.transform.localScale *=
                        AuraScaleForEnemySize(enemy.BodySize);
                    DisableRealTimeLights(aura);
                }
            }
        }

        private static void DisableRealTimeLights(GameObject effect)
        {
            foreach (Light light in effect.GetComponentsInChildren<Light>(true))
                light.enabled = false;
            // Local-space particles keep the imported effect on the moving
            // floor anchor; a killed enemy cannot leave a floating particle
            // trail at the old chest position (vendor assets unchanged).
            foreach (ParticleSystem particles in
                effect.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = particles.main;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
            }
        }

        private void EmitVisualImpact(
            Variant variant, float audioVolume, float cameraStrength, float cameraDuration)
        {
            GameObject prefab = PickVendorPrefab(variant);
            if (prefab == null)
            {
                Debug.LogWarning("[ArcaneSurge] Missing Zap visual variant " + variant, this);
                return;
            }

            // The Yellow impact REUSES its animation-event release prefab.
            // No extra fourth bolt, damage call, audio or camera impulse.
            bool reuseRelease = variant == Variant.Yellow && releaseZap != null;
            GameObject zap = reuseRelease
                ? releaseZap
                : Instantiate(prefab, lastContact, Quaternion.identity, transform);
            if (reuseRelease)
            {
                releaseZap = null;
                zap.transform.position = lastContact;
                foreach (GameObject hidden in releaseHiddenSystems)
                    if (hidden != null) hidden.SetActive(true);
                releaseHiddenSystems.Clear();
            }
            else
            {
                PrepareGiantZap(zap, variant);
            }
            zap.name = "ArcaneSurge_ImportedVefects_" + variant;
            PlaceFloorEffects(zap.transform,
                new Vector3(lastContact.x, lastGroundY + 0.05f, lastContact.z));
            RestartActiveEmitters(zap);

            if (library.BigZapSound != null)
                AudioSource.PlayClipAtPoint(library.BigZapSound, lastContact, audioVolume);
            // Each confirmed Zap impact triggers a fresh, separate shake.
            ArcaneSurgeCameraImpulse.Pulse(cameraDuration, cameraStrength);
        }

        private GameObject PickVendorPrefab(Variant variant)
        {
            switch (variant)
            {
                case Variant.Yellow: return library.Yellow;
                case Variant.DarkBlue: return library.DarkBlue;
                default: return library.CyanBlue;
            }
        }

        private static void PrepareGiantZap(GameObject zap, Variant variant)
        {
            // Changes ONLY the spawned runtime copy, never vendor assets.
            zap.transform.localScale *= OverallZapScale;
            // Vendor ships its main colored bolt ('Zap LUT') disabled.
            // Turn it on in OUR spawned effect to show a clear lightning
            // shaft rather than an enlarged background glow.
            Transform coloredCore = zap.transform.Find("Zap LUT");
            if (coloredCore != null) coloredCore.gameObject.SetActive(true);
            SetMobileSafeEffects(zap);
            TintVariant(zap, variant);
            ThickenLightningBolt(zap);
        }

        private static void PlaceFloorEffects(Transform root, Vector3 groundPoint)
        {
            Transform floor = root.Find("Zap Add Floor");
            if (floor != null) floor.position = groundPoint;

            Transform scorch = root.Find("Scorch");
            if (scorch != null) scorch.position = groundPoint;
        }

        private static void SetMobileSafeEffects(GameObject zap)
        {
            // The imported prefabs' Point Light is already off. Force it off
            // for this spell: high-contrast URP particles are sufficient.
            foreach (Light source in zap.GetComponentsInChildren<Light>(true))
                source.enabled = false;
        }

        /// <summary>
        /// Make the main imported Vefects lightning particles BOTH wider and
        /// longer by scaling their 3D particle start-size X/Y independently.
        /// The overall 1.45x instance scale happens before this method.
        /// Does not edit vendor prefab/material assets or their particle count.
        /// </summary>
        private static void ThickenLightningBolt(GameObject zap)
        {
            foreach (ParticleSystem particle in zap.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (particle == null) continue;
                float widthFactor;
                float lengthFactor;
                switch (particle.gameObject.name)
                {
                    case "Zap":
                    case "Zap LUT":
                        widthFactor = CoreBoltWidth;
                        lengthFactor = CoreBoltLength;
                        break;
                    case "Zap BG":
                        widthFactor = BackgroundBoltWidth;
                        lengthFactor = BackgroundBoltLength;
                        break;
                    case "Zap Add":
                        widthFactor = BranchBoltWidth;
                        lengthFactor = BranchBoltLength;
                        break;
                    default:
                        continue; // Keep Scorch, Flare, Zap Add Floor unchanged.
                }

                ParticleSystem.MainModule main = particle.main;
                if (main.startSize3D)
                {
                    // Vendor billboard X = bolt width, Y = vertical height.
                    // Z and local emission offsets are intentionally retained.
                    main.startSizeXMultiplier *= widthFactor;
                    main.startSizeYMultiplier *= lengthFactor;
                }
                else
                {
                    // Future non-3D prefab fallback: widen base particles,
                    // while compensating the Stretch renderer's length ratio.
                    main.startSizeMultiplier *= widthFactor;
                    ParticleSystemRenderer renderer = particle.GetComponent<ParticleSystemRenderer>();
                    if (renderer != null && renderer.renderMode == ParticleSystemRenderMode.Stretch)
                        renderer.lengthScale *= lengthFactor / widthFactor;
                }
            }
        }

        private static void TintVariant(GameObject zap, Variant variant)
        {
            // Vendor's URP shader multiplies the imported LUT result by vertex
            // color. Tint runtime particle start colors only, with no editing
            // of imported shared materials or any other attack tier.
            Color multiplier = variant == Variant.Yellow
                ? Color.white
                : variant == Variant.DarkBlue
                    ? new Color(0.48f, 0.57f, 0.92f, 1f)
                    : new Color(0.23f, 0.91f, 1f, 1f);

            foreach (ParticleSystem particle in zap.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (particle == null) continue;
                ParticleSystem.MainModule main = particle.main;
                ParticleSystem.MinMaxGradient original = main.startColor;
                if (original.mode == ParticleSystemGradientMode.Color)
                {
                    Color baseColor = original.color;
                    main.startColor = new ParticleSystem.MinMaxGradient(
                        new Color(baseColor.r * multiplier.r, baseColor.g * multiplier.g,
                            baseColor.b * multiplier.b, baseColor.a));
                }
                else if (original.mode == ParticleSystemGradientMode.TwoColors)
                {
                    Color low = original.colorMin;
                    Color high = original.colorMax;
                    main.startColor = new ParticleSystem.MinMaxGradient(
                        new Color(low.r * multiplier.r, low.g * multiplier.g,
                            low.b * multiplier.b, low.a),
                        new Color(high.r * multiplier.r, high.g * multiplier.g,
                            high.b * multiplier.b, high.a));
                }
            }

            if (variant != Variant.DarkBlue) return;

            // Deep-blue variant is deliberately less emissive than cyan.
            foreach (ParticleSystemRenderer renderer in
                zap.GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                Material shared = renderer.sharedMaterial;
                if (shared == null || !shared.HasProperty(EmissiveIntensityId))
                    continue;
                MaterialPropertyBlock properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                properties.SetFloat(EmissiveIntensityId,
                    shared.GetFloat(EmissiveIntensityId) * 0.70f);
                renderer.SetPropertyBlock(properties);
            }
        }

        private static void RestartActiveEmitters(GameObject zap)
        {
            foreach (ParticleSystem particle in zap.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (particle == null || !particle.gameObject.activeInHierarchy)
                    continue;
                particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                particle.Play(false);
            }
        }

        public void Abort()
        {
            aborted = true;
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (ownsMaterial && tracerMaterial != null)
                Destroy(tracerMaterial);
        }
    }
}
