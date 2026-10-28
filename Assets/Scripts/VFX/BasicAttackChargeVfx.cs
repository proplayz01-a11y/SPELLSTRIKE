using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Spellstrike.Vfx
{
    /// <summary>Presentation only. One bounded mesh, one material, no gameplay callbacks.</summary>
    [DisallowMultipleComponent]
    public sealed class BasicAttackChargeVfx : MonoBehaviour
    {
        public enum Quality { Automatic, Desktop, Mobile }

        [Header("Appearance")]
        [SerializeField] private Material energyMaterial;
        [ColorUsage(false, true)] [SerializeField] private Color cyan = new Color(0.08f, 0.8f, 1f);
        [Range(0f, 3f)] [SerializeField] private float chargeIntensity = 1f;
        [Range(0f, 1f)] [SerializeField] private float streakDensity = 1f;
        [Min(0.01f)] [SerializeField] private float orbitRadius = 0.34f;
        [Min(0f)] [SerializeField] private float orbitSpeed = 7f;
        [Range(0.25f, 4f)] [SerializeField] private float attractionStrength = 1.65f;
        [Min(0.001f)] [SerializeField] private float streakWidth = 0.012f;
        [Min(0.01f)] [SerializeField] private float ringRadius = 0.29f;
        [Min(0f)] [SerializeField] private float ringEmission = 0.75f;
        [Min(0.001f)] [SerializeField] private float ringWidth = 0.006f;
        [Min(0f)] [SerializeField] private float coreIntensity = 5f;
        [Min(0.001f)] [SerializeField] private float coreRadius = 0.047f;

        [Header("Continuous buildup - loose/faint to compressed/bright")]
        [Tooltip("Final pre-release radius multiplier. Lower values produce a tighter frame-27 compression.")]
        [Range(0.05f, 0.5f)] [SerializeField] private float finalCompression = 0.18f;
        [Tooltip("Charge01 point where the strong inward collapse begins.")]
        [Range(0.45f, 0.9f)] [SerializeField] private float compressionStart = 0.68f;

        [Header("Bounded quality — same composition on both platforms")]
        [SerializeField] private Quality quality = Quality.Automatic;
        [Range(4, 18)] [SerializeField] private int desktopStreaks = 12;
        [Range(4, 12)] [SerializeField] private int mobileStreaks = 8;
        [Range(4, 24)] [SerializeField] private int desktopMotes = 18;
        [Range(4, 16)] [SerializeField] private int mobileMotes = 10;

        private readonly List<Vector3> vertices = new List<Vector3>(1600);
        private readonly List<Color> colors = new List<Color>(1600);
        private readonly List<Vector4> uvs = new List<Vector4>(1600);
        private readonly List<int> triangles = new List<int>(4800);
        private Mesh mesh;
        private GameObject renderObject;
        private MeshRenderer meshRenderer;
        private Vector3 viewRight, viewUp, viewForward;

        public float Charge01 { get; private set; }
        public float Visibility01 { get; private set; }
        public int VertexCount => mesh != null ? mesh.vertexCount : 0;
        public bool IsVisible => meshRenderer != null && meshRenderer.enabled;
        public Quality QualityLevel { get => quality; set => quality = value; }

        private void EnsureMesh()
        {
            if (mesh != null) return;
            mesh = new Mesh { name = "Basic charge — bounded ribbons" };
            mesh.MarkDynamic();
            renderObject = new GameObject("Streaks + Wisps + Motes + ONE Ring + Core");
            renderObject.hideFlags = HideFlags.DontSave;
            renderObject.layer = gameObject.layer;
            renderObject.transform.SetParent(transform, false);
            renderObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            meshRenderer = renderObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = energyMaterial;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.lightProbeUsage = LightProbeUsage.Off;
            meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            meshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            meshRenderer.enabled = false;
        }

        /// <summary>Caller supplies the animation's pre-release progress and time; never drives an attack.</summary>
        public void RenderCharge(float charge01, float animationSeconds, Camera view, float visibility = 1f)
        {
            EnsureMesh();
            Charge01 = Mathf.Clamp01(charge01);
            Visibility01 = Mathf.Clamp01(visibility);
            if (view == null || energyMaterial == null || !isActiveAndEnabled || Visibility01 <= 0f)
            {
                Hide();
                return;
            }
            viewRight = transform.InverseTransformDirection(view.transform.right).normalized;
            viewUp = transform.InverseTransformDirection(view.transform.up).normalized;
            viewForward = transform.InverseTransformDirection(view.transform.forward).normalized;
            vertices.Clear(); colors.Clear(); uvs.Clear(); triangles.Clear();
            bool mobile = quality == Quality.Mobile || (quality == Quality.Automatic && Application.isMobilePlatform);
            float q = Charge01;
            // Keep the charge loose while it builds, then collapse every layer toward the crystal.
            float collapse01 = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(compressionStart, 1f, q));
            float size = Mathf.Lerp(1f, finalCompression, collapse01);
            float radius = orbitRadius * size;
            float energy = chargeIntensity * Mathf.Lerp(0.16f, 2.4f, q * q);
            // Integral of increasing speed, so angular velocity never jumps between frames.
            float motion = animationSeconds * orbitSpeed * (1f + 1.35f * q * q);
            int streaks = Mathf.Clamp(mobile ? mobileStreaks : desktopStreaks, 4, mobile ? 12 : 18);
            int segments = mobile ? 12 : 18;
            for (int i = 0; i < streaks; i++)
            {
                float density = Mathf.Clamp01(streakDensity * streaks - i);
                float activity = Mathf.Lerp(0.32f, 1f, q);
                DrawStreak(i, segments, radius, motion, q, streakWidth * size,
                    energy * density * activity, false);
            }
            // Wisps use the same force field, at lower opacity and a different length/tilt.
            for (int i = 0; i < (mobile ? 2 : 3); i++)
                DrawStreak(i + 31, segments, radius, motion * 0.83f, q,
                    streakWidth * size * 1.8f, energy * 0.12f, true);

            DrawRing(ringRadius * size,
                ringWidth * size,
                energy * ringEmission * Mathf.Lerp(0.24f, 0.85f, q), mobile ? 48 : 64);

            int motes = Mathf.Clamp(mobile ? mobileMotes : desktopMotes, 4, mobile ? 16 : 24);
            for (int i = 0; i < motes; i++)
            {
                float seed = Hash(i + 101);
                float life = Mathf.Repeat(animationSeconds * (1.1f + q * 2f) + seed, 1f);
                float r = radius * Mathf.Pow(1f - life, attractionStrength + q);
                float angle = seed * 38f + motion * (0.65f + seed);
                Vector3 p = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle * 1.17f) * 0.65f, Mathf.Sin(angle)) * r;
                float fade = Mathf.Sin(life * Mathf.PI);
                DrawBillboard(p, 0.004f * size,
                    Tint(energy * 0.6f * fade, q * 0.4f));
            }

            float core = coreRadius * size;
            DrawBillboard(Vector3.zero, core * 2.3f, Tint(energy * 0.32f, 0f));
            DrawBillboard(Vector3.zero, core, Tint(chargeIntensity * coreIntensity * Mathf.Lerp(0.12f, 1f, q * q),
                Mathf.Lerp(0.1f, 0.9f, q * q)));
            mesh.Clear();
            mesh.SetVertices(vertices); mesh.SetColors(colors); mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0, true);
            meshRenderer.enabled = true;
        }

        private void DrawStreak(int index, int segments, float radius, float motion, float q,
            float width, float brightness, bool wisp)
        {
            float seed = Hash(index + 1);
            float life = Mathf.Repeat(motion * (0.12f + seed * 0.09f) + seed * 7f, 1f);
            float fade = Mathf.SmoothStep(0f, 1f, life / 0.12f) * (1f - Mathf.SmoothStep(0.75f, 1f, life));
            float length = (wisp ? 0.42f : 0.24f) + seed * 0.16f;
            Quaternion tilt = Quaternion.Euler(seed * 125f - 62f, Hash(index + 63) * 180f, seed * 360f);
            int start = vertices.Count;
            for (int s = 0; s <= segments; s++)
            {
                float u = (float)s / segments;
                float p = Mathf.Max(0f, life - length * (1f - u));
                Vector3 center = StreakPoint(p, seed, radius, motion, q, tilt);
                Vector3 tangent = StreakPoint(p + 0.003f, seed, radius, motion, q, tilt) - center;
                Vector3 side = Vector3.Cross(viewForward, tangent).normalized;
                if (side.sqrMagnitude < 0.1f) side = viewRight;
                float taper = Mathf.Sin(u * Mathf.PI) * Mathf.Lerp(0.42f, 1f, u);
                float halfWidth = width * taper;
                Color color = Tint(brightness * fade, q * 0.18f);
                AddVertex(center - side * halfWidth, color, new Vector4(u, 0f, 0f, 0f));
                AddVertex(center + side * halfWidth, color, new Vector4(u, 1f, 0f, 0f));
                if (s > 0) Join(start + (s - 1) * 2);
            }
        }

        private Vector3 StreakPoint(float p, float seed, float radius, float motion, float q, Quaternion tilt)
        {
            float inward = Mathf.Pow(Mathf.Max(0f, 1f - p), attractionStrength + q * 1.6f);
            float r = radius * (0.72f + seed * 0.4f) * inward;
            float angle = seed * 25f + motion * (0.3f + seed * 0.32f) + p * (3.2f + seed * 2.4f);
            float bend = 0.12f * Mathf.Sin(p * 8f + seed * 19f);
            return tilt * new Vector3(Mathf.Cos(angle) * r,
                Mathf.Sin(p * 5.5f + seed * 12f) * r * 0.28f,
                Mathf.Sin(angle + bend) * r * (0.7f + seed * 0.25f));
        }

        private void DrawRing(float radius, float width, float brightness, int segments)
        {
            int start = vertices.Count;
            // A single clean ring, slightly tilted in the view plane, never stacked copies.
            Vector3 vertical = (viewUp * 0.91f + viewForward * 0.41f).normalized;
            for (int s = 0; s <= segments; s++)
            {
                float angle = s * Mathf.PI * 2f / segments;
                Vector3 direction = viewRight * Mathf.Cos(angle) + vertical * Mathf.Sin(angle);
                AddVertex(direction * (radius - width), Tint(brightness, 0.04f), new Vector4(0.5f, 0f, 0f, 0f));
                AddVertex(direction * (radius + width), Tint(brightness, 0.04f), new Vector4(0.5f, 1f, 0f, 0f));
                if (s > 0) Join(start + (s - 1) * 2);
            }
        }

        private void DrawBillboard(Vector3 center, float radius, Color color)
        {
            int start = vertices.Count;
            AddVertex(center - viewRight * radius - viewUp * radius, color, new Vector4(0, 0, 1, 0));
            AddVertex(center - viewRight * radius + viewUp * radius, color, new Vector4(0, 1, 1, 0));
            AddVertex(center + viewRight * radius - viewUp * radius, color, new Vector4(1, 0, 1, 0));
            AddVertex(center + viewRight * radius + viewUp * radius, color, new Vector4(1, 1, 1, 0));
            Join(start);
        }

        private Color Tint(float brightness, float white)
        {
            Color result = Color.Lerp(cyan, Color.white, white) * (brightness * Visibility01);
            result.a = 1f;
            return result;
        }

        private void AddVertex(Vector3 position, Color color, Vector4 uv)
        {
            vertices.Add(position); colors.Add(color); uvs.Add(uv);
        }

        private void Join(int start)
        {
            triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
            triangles.Add(start + 2); triangles.Add(start + 1); triangles.Add(start + 3);
        }

        private static float Hash(int value) => Mathf.Repeat(Mathf.Sin(value * 127.1f) * 43758.5453f, 1f);

        public void Hide()
        {
            Charge01 = 0f;
            Visibility01 = 0f;
            if (meshRenderer != null) meshRenderer.enabled = false;
        }

        private void OnDisable() => Hide();

        private void OnDestroy()
        {
            if (Application.isPlaying)
            {
                if (mesh != null) Destroy(mesh);
                if (renderObject != null) Destroy(renderObject);
            }
            else
            {
                if (mesh != null) DestroyImmediate(mesh);
                if (renderObject != null) DestroyImmediate(renderObject);
            }
        }
    }
}
