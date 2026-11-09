using UnityEngine;

namespace Spellstrike.Vfx
{
    /// <summary>
    /// Draws two mirrored, visual-only flanking arcs around one logical
    /// HomingProjectile. The projectile root remains the only collider and
    /// the sole owner of damage, vocabulary, and Target Challenge resolution.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MediumTwinOrbPresentation : MonoBehaviour
    {
        private const float ElectricTrailBrightnessMultiplier = 5f;

        [Header("Visual source")]
        [SerializeField] private GameObject orbVisualPrefab;
        [SerializeField] private GameObject electricTrailPrefab;
        [SerializeField, Min(0.1f)] private float visualScale = 7.5f;
        [SerializeField, Min(0f)] private float glowBrightnessMultiplier = 2f;

        [Header("Missile trails")]
        [SerializeField, Min(0.05f)] private float innerTrailLifetime = 0.45f;
        [SerializeField, Min(0.05f)] private float outerTrailLifetime = 0.6f;
        // Match the imported Electric Trail's authored TrailRenderer widths.
        [SerializeField, Min(0.01f)] private float innerTrailWidth = 2f;
        [SerializeField, Min(0.01f)] private float outerTrailWidth = 4f;

        [Header("Keyframed missile spread")]
        [Tooltip("Flight progress (0–1) mapped to lateral distance from the centerline in world units. "
            + "The authored keys are 0% = 0, 50% = 13, 70% = 14, and 100% = 0.")]
        [SerializeField] private AnimationCurve lateralOffsetByProgress = CreateDefaultLateralCurve();
        [SerializeField, Min(0.1f)] private float convergeBeforeImpactDistance = 2.5f;

        private Transform target;
        private Transform leftOrb;
        private Transform rightOrb;
        private GameObject leftTrailRoot;
        private GameObject rightTrailRoot;
        private Vector3 launchRight;
        private float launchDistance;
        private float progress;
        private bool isActive;

        private static AnimationCurve CreateDefaultLateralCurve()
        {
            // First leg is fast, the short 50–70% sweep is deliberately slow,
            // and the final leg accelerates inward like a pair of homing missiles.
            return new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 32f),
                new Keyframe(0.5f, 13f, 24f, 5f),
                new Keyframe(0.7f, 14f, 5f, -35f),
                new Keyframe(1f, 0f, -70f, 0f));
        }

        private void Awake()
        {
            if (lateralOffsetByProgress == null || lateralOffsetByProgress.length < 4)
                lateralOffsetByProgress = CreateDefaultLateralCurve();
        }

        public bool Activate(Transform attackTarget)
        {
            if (isActive)
                return true;

            if (orbVisualPrefab == null)
            {
                Debug.LogError("[MediumTwinOrbPresentation] Orb visual prefab is not assigned.", this);
                return false;
            }

            if (attackTarget == null)
            {
                Debug.LogError("[MediumTwinOrbPresentation] Cannot activate without an attack target.", this);
                return false;
            }

            target = attackTarget;
            Vector3 launchOrigin = transform.position;

            Vector3 initialToTarget = GetTargetAimPosition() - launchOrigin;
            launchDistance = Mathf.Max(0.1f, initialToTarget.magnitude);
            Vector3 launchForward = initialToTarget.sqrMagnitude > 0.0001f
                ? initialToTarget.normalized
                : transform.forward;

            // Stable lateral axis for the pair. A camera-right fallback covers
            // near-vertical shots where world up and forward are parallel.
            launchRight = Vector3.Cross(Vector3.up, launchForward);
            if (launchRight.sqrMagnitude <= 0.0001f && Camera.main != null)
                launchRight = Vector3.ProjectOnPlane(Camera.main.transform.right, launchForward);
            if (launchRight.sqrMagnitude <= 0.0001f)
                launchRight = Vector3.right;
            launchRight.Normalize();

            GameObject leftObject = Instantiate(orbVisualPrefab, transform, false);
            GameObject rightObject = Instantiate(orbVisualPrefab, transform, false);
            if (leftObject == null || rightObject == null)
            {
                if (leftObject != null)
                    Destroy(leftObject);
                if (rightObject != null)
                    Destroy(rightObject);
                Debug.LogError("[MediumTwinOrbPresentation] Failed to create both orb visuals.", this);
                return false;
            }

            leftObject.name = "Medium Twin Orb Left";
            rightObject.name = "Medium Twin Orb Right";
            leftOrb = leftObject.transform;
            rightOrb = rightObject.transform;
            leftOrb.localScale = Vector3.one * visualScale;
            rightOrb.localScale = Vector3.one * visualScale;

            ApplyMediumGlow(leftObject);
            ApplyMediumGlow(rightObject);
            leftTrailRoot = CreateElectricTrail("Medium Electric Trail Left", leftOrb.position);
            rightTrailRoot = CreateElectricTrail("Medium Electric Trail Right", rightOrb.position);

            // The original visual remains unchanged in the production prefab;
            // only this Medium Attack projectile instance swaps to the pair.
            Transform originalVisual = transform.Find("MagicOrb Visual");
            if (originalVisual != null)
                originalVisual.gameObject.SetActive(false);

            progress = 0f;
            isActive = true;
            UpdateOrbPositions();
            return true;
        }

        private void LateUpdate()
        {
            if (!isActive || leftOrb == null || rightOrb == null || target == null)
                return;

            UpdateOrbPositions();
        }

        private void UpdateOrbPositions()
        {
            Vector3 targetPosition = GetTargetAimPosition();
            float remainingDistance = Vector3.Distance(transform.position, targetPosition);

            // The root HomingProjectile advances directly toward the same target.
            // Use its remaining distance to drive both visual curves together,
            // while keeping progress monotonic if the target moves slightly.
            float convergenceDistance = Mathf.Min(
                convergeBeforeImpactDistance,
                launchDistance * 0.35f);
            float visibleTravelDistance = Mathf.Max(0.1f, launchDistance - convergenceDistance);
            float measuredProgress = 1f - Mathf.Clamp01(
                (remainingDistance - convergenceDistance) / visibleTravelDistance);
            progress = Mathf.Max(progress, measuredProgress);

            // Keep the pair's centerline exactly on the real homing projectile.
            // That ensures the visual convergence and the single hit collider
            // arrive together, even if the enemy moves during flight.
            Vector3 pathCenter = transform.position;
            float lateralOffset = Mathf.Clamp(
                lateralOffsetByProgress.Evaluate(progress),
                0f,
                14f);
            Vector3 mirroredOffset = launchRight * lateralOffset;

            // Both paths begin at the staff, bow outward on opposite flanks,
            // sweep slowly from 13 to 14 units, then accelerate inward to meet
            // at the same target point: the keyframed missile-lens curve.
            leftOrb.position = pathCenter - mirroredOffset;
            rightOrb.position = pathCenter + mirroredOffset;

            if (leftTrailRoot != null)
                leftTrailRoot.transform.position = leftOrb.position;
            if (rightTrailRoot != null)
                rightTrailRoot.transform.position = rightOrb.position;
        }

        private void ApplyMediumGlow(GameObject orb)
        {
            if (orb == null)
                return;

            MagicOrbGlowBillboard glowBillboard = orb.GetComponent<MagicOrbGlowBillboard>();
            if (glowBillboard != null)
                glowBillboard.SetBrightnessMultiplier(glowBrightnessMultiplier);

            // Intensify the dedicated soft aura only. Core/mote particle systems
            // are deliberately left untouched so the orb keeps a crisp center.
            ParticleSystem[] particleSystems = orb.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem particleSystem in particleSystems)
            {
                if (particleSystem == null || particleSystem.gameObject.name != "Hovl_SoftSphericalGlow")
                    continue;

                ParticleSystem.MainModule main = particleSystem.main;
                Color boosted = main.startColor.color;
                boosted.r *= glowBrightnessMultiplier;
                boosted.g *= glowBrightnessMultiplier;
                boosted.b *= glowBrightnessMultiplier;
                main.startColor = new ParticleSystem.MinMaxGradient(boosted);
            }
        }

private GameObject CreateElectricTrail(string trailName, Vector3 startPosition)
        {
            if (electricTrailPrefab == null)
            {
                Debug.LogWarning("[MediumTwinOrbPresentation] Electric Trail prefab is not assigned; continuing without imported missile trails.", this);
                return null;
            }

            // Keep the imported prefab and its shared materials untouched. Each
            // gameplay trail receives runtime-only material copies so the
            // five-times emission boost affects Medium Attack only.
            GameObject trailRoot = Instantiate(electricTrailPrefab, transform, false);
            if (trailRoot == null)
                return null;

            trailRoot.name = trailName;
            trailRoot.transform.position = startPosition;

            AudioSource[] audioSources = trailRoot.GetComponentsInChildren<AudioSource>(true);
            foreach (AudioSource audioSource in audioSources)
            {
                audioSource.Stop();
                audioSource.enabled = false;
            }

            // The imported distortion pass is not required for a readable
            // electric streak and stays disabled for mobile-friendly rendering.
            Transform distortion = trailRoot.transform.Find("Distortion");
            if (distortion != null)
                distortion.gameObject.SetActive(false);

            TrailRenderer[] trails = trailRoot.GetComponentsInChildren<TrailRenderer>(true);
            int boostedRendererCount = 0;
            foreach (TrailRenderer trail in trails)
            {
                if (trail == null || !trail.gameObject.activeInHierarchy)
                    continue;

                bool isInnerTrail = trail.gameObject.name == "Trail 01";
                trail.enabled = true;
                trail.time = isInnerTrail ? innerTrailLifetime : outerTrailLifetime;
                trail.widthMultiplier = isInnerTrail ? innerTrailWidth : outerTrailWidth;
                trail.minVertexDistance = 0.06f;
                trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                trail.receiveShadows = false;
                trail.autodestruct = false;

                Material[] sourceMaterials = trail.sharedMaterials;
                Material[] boostedMaterials = new Material[sourceMaterials.Length];
                for (int i = 0; i < sourceMaterials.Length; i++)
                {
                    Material sourceMaterial = sourceMaterials[i];
                    if (sourceMaterial == null)
                        continue;

                    Material runtimeMaterial = new Material(sourceMaterial);
                    if (runtimeMaterial.HasProperty("_EmissiveIntensity"))
                    {
                        float authoredIntensity = runtimeMaterial.GetFloat("_EmissiveIntensity");
                        runtimeMaterial.SetFloat(
                            "_EmissiveIntensity",
                            authoredIntensity * ElectricTrailBrightnessMultiplier);
                    }

                    boostedMaterials[i] = runtimeMaterial;
                }

                trail.sharedMaterials = boostedMaterials;
                trail.Clear();
                trail.emitting = true;
                boostedRendererCount++;
            }

            if (boostedRendererCount < 2)
            {
                Debug.LogWarning(
                    "[MediumTwinOrbPresentation] Imported Electric Trail prefab did not provide both active trail layers.",
                    trailRoot);
            }
            else
            {
                Debug.Log(
                    $"[MediumTwinOrbPresentation] {trailName} active with {boostedRendererCount} trail layers at {ElectricTrailBrightnessMultiplier:0}x emission.",
                    trailRoot);
            }

            return trailRoot;
        }

        /// <summary>
        /// Preserve the short trail tails after the projectile root is destroyed
        /// by the single accepted hit callback.
        /// </summary>
        public void ReleaseTrails()
        {
            ReleaseTrailRoot(leftTrailRoot);
            ReleaseTrailRoot(rightTrailRoot);
            leftTrailRoot = null;
            rightTrailRoot = null;
        }

private void ReleaseTrailRoot(GameObject trailRoot)
        {
            if (trailRoot == null)
                return;

            TrailRenderer[] trails = trailRoot.GetComponentsInChildren<TrailRenderer>(true);
            float cleanupDelay = 0f;
            foreach (TrailRenderer trail in trails)
            {
                trail.emitting = false;
                trail.autodestruct = true;
                cleanupDelay = Mathf.Max(cleanupDelay, trail.time);
            }

            // Runtime-only boosted materials are owned by these trail renderers.
            // Keep them alive for the fade-out, then release them with the roots.
            foreach (TrailRenderer trail in trails)
            {
                if (trail == null || !trail.gameObject.activeInHierarchy)
                    continue;

                foreach (Material material in trail.sharedMaterials)
                {
                    if (material != null)
                        Destroy(material, cleanupDelay + 0.1f);
                }
            }

            trailRoot.transform.SetParent(null, true);
            Destroy(trailRoot, cleanupDelay + 0.1f);
        }

        private Vector3 GetTargetAimPosition()
        {
            if (target == null)
                return transform.position;

            TitanEarthbreakerController titan = target.GetComponent<TitanEarthbreakerController>();
            return titan != null ? titan.SpellAimPosition : target.position;
        }
    }
}
