using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Spellstrike.Vfx
{
    /// <summary>
    /// Strong-only imported Vefects Zap VFX presentation.
    /// Three imported Zap hits are triggered by StrongAttack Animation Events:
    /// Yellow, DarkBlue, CyanBlue at distinct clip frames 0.20s apart.
    /// The accepted word/barrier resolves ONCE, then each lightning strike
    /// applies its own integer portion of the approved total damage.
    /// No vendor prefab, shared material, or Basic/Medium asset is modified.
    ///
    /// The very short pre-impact streak draws the exact 0.20-second descending
    /// tip; the vendor-authored particles then provide the heavy head contact
    /// burst, floor arcs, and scorch after the accepted projectile hit.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ArcaneSurgeZapVisual : MonoBehaviour
    {
        private enum Variant { Yellow, DarkBlue, CyanBlue }

        private const int BoltPoints = 13;
        public const float ImpactIntervalSeconds = 0.20f;
        private const float EffectCleanupSeconds = 2.60f;
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
        private Vector3 head;
        private float height;
        private float descent01;
        private bool impacted;
        private bool aborted;
        private int nextImpactIndex;
        private float seed;
        private Transform movingTarget;
        private Collider movingCollider;
        private Transform headBone;
        private Vector3 lastContact;
        private float lastGroundY;
        private EnemyController damageEnemy;
        private BrambleSpriteController damageBramble;
        private EnemyHealth damageHealth;
        private int damageWordLength;
        private int totalApprovedDamage;
        private bool damageSequenceConfigured;

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
            if (!damageSequenceConfigured)
                return;

            int splitDamage = DamageForStrike(totalApprovedDamage, strikeIndex);
            if (splitDamage <= 0)
                return;

            // Do not hit a corpse or a target removed between two impacts.
            // In that case the visual still finishes at the last contact point.
            if (damageEnemy != null && damageEnemy.gameObject.activeInHierarchy
                && damageEnemy.currentState != EnemyController.EnemyState.Dead)
                damageEnemy.TakeDamage(splitDamage, damageWordLength);
            else if (damageBramble != null && damageBramble.gameObject.activeInHierarchy
                && damageBramble.currentState != BrambleSpriteController.EnemyState.Dead)
                damageBramble.TakeDamage(splitDamage);
            else if (damageHealth != null && damageHealth.gameObject.activeInHierarchy
                && !damageHealth.IsDead && !damageHealth.IsInvulnerable)
                damageHealth.TakeDamage(splitDamage);
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
        /// Follow-up visuals re-sample a living enemy's head; if it dies, they
        /// finish at the last known hit position without applying more damage.
        /// </summary>
        public void SetImpactTarget(Transform acceptedTarget, Collider acceptedCollider)
        {
            movingTarget = acceptedTarget;
            movingCollider = acceptedCollider;
            headBone = null;
            if (movingTarget == null) return;

            Animator animator = movingTarget.GetComponentInParent<Animator>();
            if (animator != null && animator.isHuman)
                headBone = animator.GetBoneTransform(HumanBodyBones.Head);
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

        public void SetDescent(Vector3 liveHead, float skyHeight, float time01)
        {
            if (impacted || aborted) return;
            head = liveHead;
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
                head + Vector3.up * height, Quaternion.identity, transform);
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
            if (impacted || aborted || brightTracer == null || softTracer == null)
                return;

            // Both endpoints are relative to the MOVING enemy's current head.
            // Only the tip descends from the sky to the enemy over exactly
            // ArcaneSurgePresentation.DescentSeconds (0.20 seconds).
            Vector3 top = head + Vector3.up * height;
            float smooth = descent01 * descent01 * (3f - 2f * descent01);
            Vector3 tip = Vector3.LerpUnclamped(top, head, smooth);
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
            RefreshContactPosition();
            ApplyDamageStrike(1);
            EmitVisualImpact(Variant.DarkBlue, 0.55f, 0.85f, 0.20f);
        }

        /// <summary>Invoked only by OnStrongZapCyanBlue Animation Event.</summary>
        public void TriggerCyanBlueImpact()
        {
            if (!impacted || aborted || nextImpactIndex != 2)
                return;
            nextImpactIndex = 3;
            RefreshContactPosition();
            ApplyDamageStrike(2);
            EmitVisualImpact(Variant.CyanBlue, 0.70f, 1.15f, 0.26f);
        }

        private void RefreshContactPosition()
        {
            if (movingTarget == null || !movingTarget.gameObject.activeInHierarchy
                || movingCollider == null || !movingCollider.enabled
                || !movingCollider.gameObject.activeInHierarchy)
                return; // Dead/despawned enemy: retain last accepted location.

            Bounds bounds = movingCollider.bounds;
            lastContact = headBone != null && headBone.gameObject.activeInHierarchy
                ? headBone.position
                : new Vector3(bounds.center.x, bounds.max.y - 0.08f, bounds.center.z);
            lastGroundY = bounds.min.y;
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
