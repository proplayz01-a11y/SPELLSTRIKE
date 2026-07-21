using System.Collections.Generic;
using UnityEngine;

public class PiercingScreamWaveVFX : MonoBehaviour
{
    [Header("Travel")]
    public float duration = 0.42f;
    public float range = 7f;
    public float startOffset = 0.12f;

    [Header("Wave Shape")]
    public int waveCount = 6;
    public int segments = 64;
    public float startHeight = 0.18f;
    public float endHeight = 2.8f;
    public float startWidth = 0.08f;
    public float endWidth = 1.8f;
    public float lineWidth = 0.08f;
    public float waveSpacing = 0.12f;

    [Header("Cone Fit")]
    [Range(1f, 180f)] public float coneAngle = 45f;
    public bool matchWidthToCone = true;
    public float coneWidthScale = 0.82f;

    [Header("Spectral Noise")]
    public float noiseAmount = 0.12f;
    public float noiseScale = 3.4f;
    public float noiseSpeed = 8f;

    [Header("Color")]
    public Color startColor = new Color(0.32f, 1f, 1f, 0.9f);
    public Color endColor = new Color(0.08f, 0.32f, 0.42f, 0f);
    public Color emissionColor = new Color(0.35f, 1f, 1f, 1f);
    public float emissionIntensity = 2.5f;

    [Header("Lifecycle")]
    public bool destroyOnComplete = true;

    private readonly List<LineRenderer> rings = new List<LineRenderer>();
    private readonly List<float> ringOffsets = new List<float>();
    private Material ringMaterial;
    private Vector3 travelDirection = Vector3.forward;
    private Vector3 rightDirection = Vector3.right;
    private Vector3 upDirection = Vector3.up;
    private float spawnTime;

    public void Play(Vector3 origin, Vector3 forward)
    {
        transform.position = origin;
        travelDirection = forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
        rightDirection = Vector3.Cross(Vector3.up, travelDirection).normalized;

        if (rightDirection.sqrMagnitude <= 0.001f)
            rightDirection = Vector3.Cross(Vector3.right, travelDirection).normalized;

        if (rightDirection.sqrMagnitude <= 0.001f)
            rightDirection = Vector3.right;

        upDirection = Vector3.Cross(travelDirection, rightDirection).normalized;

        if (upDirection.sqrMagnitude <= 0.001f)
            upDirection = Vector3.up;

        spawnTime = Time.time;

        BuildRings();
        UpdateRings(0f);
    }

    private void Awake()
    {
        spawnTime = Time.time;
    }

    private void Update()
    {
        float normalizedTime = Mathf.Clamp01((Time.time - spawnTime) / Mathf.Max(0.01f, duration));
        UpdateRings(normalizedTime);

        if (normalizedTime >= 1f && destroyOnComplete)
            Destroy(gameObject);
    }

    private void BuildRings()
    {
        ClearRings();
        ringMaterial = CreateWaveMaterial();

        int safeWaveCount = Mathf.Max(1, waveCount);
        for (int i = 0; i < safeWaveCount; i++)
        {
            GameObject ringObject = new GameObject($"ScreamWaveRing_{i + 1}");
            ringObject.transform.SetParent(transform, false);

            LineRenderer line = ringObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = true;
            line.positionCount = Mathf.Max(8, segments);
            line.widthMultiplier = lineWidth;
            line.numCornerVertices = 3;
            line.numCapVertices = 3;
            line.alignment = LineAlignment.View;
            line.material = ringMaterial;

            rings.Add(line);

            float offset = safeWaveCount == 1 ? 0f : (float)i / (safeWaveCount - 1);
            ringOffsets.Add(offset * waveSpacing);
        }
    }

    private void ClearRings()
    {
        for (int i = rings.Count - 1; i >= 0; i--)
        {
            if (rings[i] != null)
                Destroy(rings[i].gameObject);
        }

        rings.Clear();
        ringOffsets.Clear();
    }

    private void UpdateRings(float normalizedTime)
    {
        if (rings.Count == 0)
            BuildRings();

        for (int i = 0; i < rings.Count; i++)
        {
            LineRenderer ring = rings[i];
            if (ring == null)
                continue;

            float delayedTime = normalizedTime - ringOffsets[i];
            if (delayedTime < 0f)
            {
                ring.enabled = false;
                continue;
            }

            ring.enabled = true;

            float ringTime = Mathf.Clamp01(delayedTime / Mathf.Max(0.01f, 1f - ringOffsets[i]));
            float distance = startOffset + range * ringTime;
            float height = Mathf.Lerp(startHeight, endHeight, ringTime);
            float width = Mathf.Lerp(startWidth, endWidth, ringTime);

            if (matchWidthToCone)
            {
                float coneRadius = Mathf.Tan(coneAngle * 0.5f * Mathf.Deg2Rad) * distance * coneWidthScale;
                width = Mathf.Max(width, coneRadius);
            }

            float alpha = Mathf.SmoothStep(1f, 0f, ringTime);

            Color color = Color.Lerp(startColor, endColor, ringTime);
            color.a *= alpha;
            ring.startColor = color;
            ring.endColor = color;
            ring.widthMultiplier = Mathf.Lerp(lineWidth * 1.25f, lineWidth * 0.35f, ringTime);

            Vector3 center = transform.position + travelDirection * distance;
            int safeSegments = Mathf.Max(8, segments);
            ring.positionCount = safeSegments;

            for (int p = 0; p < safeSegments; p++)
            {
                float angle = (p / (float)safeSegments) * Mathf.PI * 2f;
                float wobble = Mathf.PerlinNoise(
                    Mathf.Cos(angle) * noiseScale + Time.time * noiseSpeed,
                    Mathf.Sin(angle) * noiseScale + i * 2.17f
                );
                wobble = (wobble - 0.5f) * noiseAmount * (1f + ringTime);

                Vector3 offset =
                    rightDirection * (Mathf.Cos(angle) * (width + wobble)) +
                    upDirection * (Mathf.Sin(angle) * (height + wobble));

                ring.SetPosition(p, center + offset);
            }
        }
    }

    private Material CreateWaveMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        Material material = new Material(shader);

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", startColor);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", startColor);
        if (material.HasProperty("_EmissionColor"))
            material.SetColor("_EmissionColor", emissionColor * Mathf.Max(0f, emissionIntensity));

        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 0f);

        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.EnableKeyword("_EMISSION");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        return material;
    }
}
