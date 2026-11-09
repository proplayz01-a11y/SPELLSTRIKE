using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Spellstrike.Vfx
{
    /// <summary>
    /// A short, mobile-safe magic impact burst built from one additive mesh.
    /// It deliberately avoids ParticleSystem materials because the previous
    /// radial particle material did not render reliably in the gameplay camera.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BasicAttackImpactVfx : MonoBehaviour
    {
        [Header("Material")]
        [SerializeField] private Material energyMaterial;
        [ColorUsage(false, true)]
        [SerializeField] private Color cyan = new Color(0.05f, 0.65f, 1f, 1f);

        [Header("Timing and scale")]
        [Min(0.1f)] [SerializeField] private float duration = 0.58f;
        [Min(0.01f)] [SerializeField] private float coreStartRadius = 0.14f;
        [Min(0.01f)] [SerializeField] private float coreEndRadius = 0.72f;
        [Min(0.01f)] [SerializeField] private float ringStartRadius = 0.28f;
        [Min(0.01f)] [SerializeField] private float ringEndRadius = 1.65f;
        [Min(0.001f)] [SerializeField] private float ringStartWidth = 0.105f;
        [Min(0.001f)] [SerializeField] private float ringEndWidth = 0.035f;

        [Header("Burst streaks")]
        [Range(6, 20)] [SerializeField] private int streakCount = 14;
        [Min(0.01f)] [SerializeField] private float streakStartRadius = 0.18f;
        [Min(0.01f)] [SerializeField] private float streakLength = 0.95f;
        [Min(0.001f)] [SerializeField] private float streakWidth = 0.065f;
        [Range(12, 96)] [SerializeField] private int ringSegments = 56;

        private readonly List<Vector3> vertices = new List<Vector3>(256);
        private readonly List<Color> colors = new List<Color>(256);
        private readonly List<Vector4> uvs = new List<Vector4>(256);
        private readonly List<int> triangles = new List<int>(512);

        private Mesh mesh;
        private MeshRenderer meshRenderer;
        private float elapsed;

        private void Awake()
        {
            mesh = new Mesh { name = "Magic impact burst — additive mesh" };
            mesh.MarkDynamic();

            GameObject renderObject = new GameObject("Impact Core + Ring + Streaks");
            renderObject.transform.SetParent(transform, false);
            renderObject.layer = gameObject.layer;

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

        private void Update()
        {
            if (energyMaterial == null || meshRenderer == null)
            {
                Destroy(gameObject);
                return;
            }

            elapsed += Time.deltaTime;
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            RenderBurst(t);

            if (elapsed >= duration)
                Destroy(gameObject);
        }

        private void RenderBurst(float t)
        {
            vertices.Clear();
            colors.Clear();
            uvs.Clear();
            triangles.Clear();

            float eased = 1f - Mathf.Pow(1f - t, 3f);
            float fade = 1f - Mathf.SmoothStep(0f, 1f, t);

            float coreRadius = Mathf.Lerp(coreStartRadius, coreEndRadius, eased);
            float ringRadius = Mathf.Lerp(ringStartRadius, ringEndRadius, eased);
            float ringWidth = Mathf.Lerp(ringStartWidth, ringEndWidth, eased);

            Color coreColor = Color.Lerp(cyan, Color.white, 0.72f);
            coreColor.a = fade * 0.95f;
            Color ringColor = cyan;
            ringColor.a = fade * 0.9f;
            Color streakColor = Color.Lerp(Color.white, cyan, 0.28f);
            streakColor.a = fade * 0.95f;

            DrawDisc(coreRadius, coreColor);
            DrawRing(ringRadius, ringWidth, ringColor, ringSegments);
            DrawStreaks(t, ringRadius, streakColor);

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0, true);
            mesh.RecalculateBounds();
            meshRenderer.enabled = true;
        }

        private void DrawDisc(float radius, Color color)
        {
            int start = vertices.Count;
            AddVertex(new Vector3(-radius, -radius, 0f), color, new Vector4(0f, 0f, 1f, 0f));
            AddVertex(new Vector3(-radius, radius, 0f), color, new Vector4(0f, 1f, 1f, 0f));
            AddVertex(new Vector3(radius, -radius, 0f), color, new Vector4(1f, 0f, 1f, 0f));
            AddVertex(new Vector3(radius, radius, 0f), color, new Vector4(1f, 1f, 1f, 0f));
            Join(start);
        }

        private void DrawRing(float radius, float width, Color color, int segments)
        {
            int start = vertices.Count;
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                AddVertex(direction * (radius - width), color, new Vector4((float)i / segments, 0f, 0f, 0f));
                AddVertex(direction * (radius + width), color, new Vector4((float)i / segments, 1f, 0f, 0f));
                if (i > 0)
                    Join(start + (i - 1) * 2);
            }
        }

        private void DrawStreaks(float t, float ringRadius, Color color)
        {
            float fade = 1f - t;
            float length = streakLength * (0.35f + 0.65f * fade);
            float halfWidth = streakWidth * fade;

            for (int i = 0; i < streakCount; i++)
            {
                float angle = i * Mathf.PI * 2f / streakCount;
                Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                Vector3 side = new Vector3(-direction.y, direction.x, 0f);
                Vector3 start = direction * Mathf.Max(streakStartRadius, ringRadius * 0.52f);
                Vector3 end = start + direction * length;

                Color startColor = color;
                Color endColor = color;
                endColor.a = 0f;

                int first = vertices.Count;
                AddVertex(start - side * halfWidth, startColor, new Vector4(0f, 0f, 0f, 0f));
                AddVertex(start + side * halfWidth, startColor, new Vector4(0f, 1f, 0f, 0f));
                AddVertex(end - side * halfWidth * 0.08f, endColor, new Vector4(1f, 0f, 0f, 0f));
                AddVertex(end + side * halfWidth * 0.08f, endColor, new Vector4(1f, 1f, 0f, 0f));
                Join(first);
            }
        }

        private void AddVertex(Vector3 position, Color color, Vector4 uv)
        {
            vertices.Add(position);
            colors.Add(color);
            uvs.Add(uv);
        }

        private void Join(int start)
        {
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start + 2);
            triangles.Add(start + 1);
            triangles.Add(start + 3);
        }

        private void OnDestroy()
        {
            if (mesh == null)
                return;

            if (Application.isPlaying)
                Destroy(mesh);
            else
                DestroyImmediate(mesh);
        }
    }
}
