using UnityEngine;
using UnityEngine.Rendering;

namespace Spellstrike.Vfx
{
    /// <summary>
    /// Adds a camera-facing, mobile-safe additive aura to the Basic Attack orb.
    /// The small quad uses the same radial-disc shader as the validated impact
    /// burst, avoiding the inconsistent ParticleSystem glow material path.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MagicOrbGlowBillboard : MonoBehaviour
    {
        [Header("Glow appearance")]
        [SerializeField] private Material glowMaterial;
        [SerializeField] private Color glowColor = new Color(0.12f, 0.62f, 1f, 0.82f);
        [Min(0.1f)] [SerializeField] private float localDiameter = 0.76f;
        [Min(0f)] [SerializeField] private float depthOffset = 0.025f;

        private Transform billboard;
        private Mesh glowMesh;
        private Color baseGlowColor;

        private void Awake()
        {
            baseGlowColor = glowColor;

            if (glowMaterial == null)
            {
                Debug.LogWarning("[MagicOrbGlowBillboard] Glow material is not assigned.", this);
                enabled = false;
                return;
            }

            GameObject glowObject = new GameObject("Basic Attack Additive Glow");
            glowObject.layer = gameObject.layer;
            billboard = glowObject.transform;
            billboard.SetParent(transform, false);
            billboard.localPosition = Vector3.zero;
            billboard.localRotation = Quaternion.identity;
            billboard.localScale = Vector3.one * localDiameter;

            glowMesh = new Mesh { name = "Basic Attack radial glow billboard" };
            glowMesh.MarkDynamic();
            glowMesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(-0.5f,  0.5f, 0f),
                new Vector3( 0.5f, -0.5f, 0f),
                new Vector3( 0.5f,  0.5f, 0f)
            };
            glowMesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f)
            };
            glowMesh.SetUVs(0, new[]
            {
                new Vector4(0f, 0f, 1f, 0f),
                new Vector4(0f, 1f, 1f, 0f),
                new Vector4(1f, 0f, 1f, 0f),
                new Vector4(1f, 1f, 1f, 0f)
            });
            glowMesh.colors = new[] { glowColor, glowColor, glowColor, glowColor };
            glowMesh.triangles = new[] { 0, 1, 2, 2, 1, 3 };
            glowMesh.RecalculateBounds();

            glowObject.AddComponent<MeshFilter>().sharedMesh = glowMesh;
            MeshRenderer glowRenderer = glowObject.AddComponent<MeshRenderer>();
            glowRenderer.sharedMaterial = glowMaterial;
            glowRenderer.shadowCastingMode = ShadowCastingMode.Off;
            glowRenderer.receiveShadows = false;
            glowRenderer.lightProbeUsage = LightProbeUsage.Off;
            glowRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            glowRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        }

        /// <summary>
        /// Boosts only this runtime instance's additive vertex glow. The shared
        /// material and source prefab remain unchanged for Basic Attack.
        /// </summary>
        public void SetBrightnessMultiplier(float multiplier)
        {
            if (glowMesh == null)
                return;

            float safeMultiplier = Mathf.Max(0f, multiplier);
            Color boosted = baseGlowColor;
            boosted.r *= safeMultiplier;
            boosted.g *= safeMultiplier;
            boosted.b *= safeMultiplier;
            glowMesh.colors = new[] { boosted, boosted, boosted, boosted };
        }

        private void LateUpdate()
        {
            if (billboard == null)
                return;

            Camera viewCamera = Camera.main;
            if (viewCamera == null)
                return;

            Vector3 towardCamera = viewCamera.transform.position - transform.position;
            if (towardCamera.sqrMagnitude <= 0.0001f)
                return;

            towardCamera.Normalize();
            billboard.rotation = Quaternion.LookRotation(towardCamera, viewCamera.transform.up);
            // Keep the aura just behind the central orb so the core stays crisp.
            billboard.position = transform.position - towardCamera * depthOffset;
        }

        private void OnDestroy()
        {
            if (glowMesh == null)
                return;

            if (Application.isPlaying)
                Destroy(glowMesh);
            else
                DestroyImmediate(glowMesh);
        }
    }
}
