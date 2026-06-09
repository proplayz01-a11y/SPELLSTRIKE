using UnityEngine;

public class WaterBurstWarningVFX : MonoBehaviour
{
    [Header("Timing")]
    public float duration = 1f;
    public bool destroyOnComplete = false;

    [Header("Shape")]
    public float radius = 2.25f;
    public float ringWidth = 0.18f;
    public float pulseAmount = 0.18f;
    public float heightOffset = 0.03f;

    [Header("Procedural Water Noise")]
    [Range(24, 160)] public int radialSegments = 96;
    public float edgeNoiseAmount = 0.18f;
    public float edgeNoiseScale = 2.4f;
    public float edgeNoiseSpeed = 1.7f;
    public float ringNoiseAmount = 0.08f;

    [Header("Color")]
    public Color warningColor = new Color(0.1f, 0.85f, 1f, 0.38f);
    public Color ringColor = new Color(0.7f, 1f, 1f, 0.75f);

    private Transform disc;
    private Transform ringA;
    private Transform ringB;
    private Material discMaterial;
    private Material ringMaterial;
    private Mesh discMesh;
    private Vector3[] discVertices;
    private float elapsed;

    private void Awake()
    {
        BuildVisuals();
    }

    private void OnEnable()
    {
        elapsed = 0f;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float progress = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
        float pulse = 1f + Mathf.Sin(Time.time * 12f) * pulseAmount;
        float fade = Mathf.Lerp(1f, 0.2f, progress);

        if (disc != null)
        {
            UpdateDiscMesh(pulse);
            SetMaterialAlpha(discMaterial, warningColor.a * fade);
        }

        AnimateRing(ringA, 0f, pulse, fade);
        AnimateRing(ringB, 90f, 1f + (pulse - 1f) * 0.6f, fade);

        if (destroyOnComplete && elapsed >= duration)
            Destroy(gameObject);
    }

    private void BuildVisuals()
    {
        if (disc != null)
            return;

        discMaterial = CreateTransparentMaterial("Water Burst Warning Disc", warningColor);
        ringMaterial = CreateTransparentMaterial("Water Burst Warning Ring", ringColor);

        GameObject discObject = new GameObject("WarningDisc");
        discObject.name = "WarningDisc";
        discObject.transform.SetParent(transform, false);
        discObject.transform.localPosition = new Vector3(0f, heightOffset, 0f);
        discObject.transform.localScale = Vector3.one;
        MeshFilter meshFilter = discObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = discObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = discMaterial;
        discMesh = new Mesh { name = "Procedural Water Burst Warning Disc" };
        meshFilter.sharedMesh = discMesh;
        BuildDiscMesh();
        disc = discObject.transform;

        ringA = CreateRing("WarningRing_A", 0f);
        ringB = CreateRing("WarningRing_B", 90f);
    }

    private Transform CreateRing(string ringName, float yRotation)
    {
        GameObject ringObject = new GameObject(ringName);
        ringObject.name = ringName;
        ringObject.transform.SetParent(transform, false);
        ringObject.transform.localPosition = new Vector3(0f, heightOffset + 0.02f, 0f);
        ringObject.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
        ringObject.transform.localScale = Vector3.one;

        LineRenderer lineRenderer = ringObject.AddComponent<LineRenderer>();
        lineRenderer.sharedMaterial = ringMaterial;
        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = true;
        lineRenderer.positionCount = radialSegments;
        lineRenderer.widthMultiplier = ringWidth;
        lineRenderer.numCapVertices = 4;

        UpdateRingShape(lineRenderer, 1f, 0f);

        return ringObject.transform;
    }

    private void AnimateRing(Transform ring, float rotationOffset, float pulse, float fade)
    {
        if (ring == null)
            return;

        ring.localRotation = Quaternion.Euler(0f, rotationOffset + Time.time * 80f, 0f);
        ring.localScale = new Vector3(radius * pulse, 1f, radius * pulse);
        LineRenderer lineRenderer = ring.GetComponent<LineRenderer>();
        if (lineRenderer != null)
            UpdateRingShape(lineRenderer, pulse, rotationOffset);

        SetMaterialAlpha(ringMaterial, ringColor.a * fade);
    }

    private void BuildDiscMesh()
    {
        int segments = Mathf.Max(24, radialSegments);
        discVertices = new Vector3[segments + 1];
        int[] triangles = new int[segments * 3];

        discVertices[0] = Vector3.zero;
        for (int i = 0; i < segments; i++)
        {
            int next = i == segments - 1 ? 1 : i + 2;
            int triIndex = i * 3;
            triangles[triIndex] = 0;
            triangles[triIndex + 1] = i + 1;
            triangles[triIndex + 2] = next;
        }

        discMesh.Clear();
        discMesh.vertices = discVertices;
        discMesh.triangles = triangles;
        discMesh.RecalculateBounds();
        UpdateDiscMesh(1f);
    }

    private void UpdateDiscMesh(float pulse)
    {
        if (discMesh == null || discVertices == null)
            return;

        float timeOffset = Time.time * edgeNoiseSpeed;
        discVertices[0] = Vector3.zero;

        for (int i = 0; i < radialSegments; i++)
        {
            float angle = (Mathf.PI * 2f * i) / radialSegments;
            float noise = GetCircularNoise(angle, timeOffset);
            float noisyRadius = radius * pulse * (1f + noise * edgeNoiseAmount);
            discVertices[i + 1] = new Vector3(Mathf.Cos(angle) * noisyRadius, 0f, Mathf.Sin(angle) * noisyRadius);
        }

        discMesh.vertices = discVertices;
        discMesh.RecalculateBounds();
    }

    private void UpdateRingShape(LineRenderer lineRenderer, float pulse, float rotationOffset)
    {
        float timeOffset = Time.time * edgeNoiseSpeed + rotationOffset * 0.01f;
        for (int i = 0; i < lineRenderer.positionCount; i++)
        {
            float angle = (Mathf.PI * 2f * i) / lineRenderer.positionCount;
            float noise = GetCircularNoise(angle, timeOffset);
            float noisyRadius = 1f + noise * ringNoiseAmount;
            lineRenderer.SetPosition(i, new Vector3(Mathf.Cos(angle) * noisyRadius, 0f, Mathf.Sin(angle) * noisyRadius));
        }
    }

    private float GetCircularNoise(float angle, float timeOffset)
    {
        float sampleX = Mathf.Cos(angle) * edgeNoiseScale + timeOffset;
        float sampleY = Mathf.Sin(angle) * edgeNoiseScale + timeOffset * 0.73f;
        return Mathf.PerlinNoise(sampleX, sampleY) * 2f - 1f;
    }

    private Material CreateTransparentMaterial(string materialName, Color color)
    {
        Material material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        if (material.shader == null)
            material = new Material(Shader.Find("Sprites/Default"));

        material.name = materialName;
        material.color = color;
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.renderQueue = 3000;
        return material;
    }

    private void SetRendererMaterial(GameObject target, Material material)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;
    }

    private void SetMaterialAlpha(Material material, float alpha)
    {
        if (material == null)
            return;

        Color color = material.color;
        color.a = alpha;
        material.color = color;
    }

    private void DestroyCollider(GameObject target)
    {
        Collider collider = target.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
    }
}
