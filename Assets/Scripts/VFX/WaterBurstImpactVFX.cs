using UnityEngine;

public class WaterBurstImpactVFX : MonoBehaviour
{
    [Header("Timing")]
    public float duration = 1.35f;
    public float riseDuration = 0.35f;
    public float peakHoldDuration = 0.2f;
    public float collapseDuration = 0.8f;
    public bool destroyOnComplete = true;

    [Header("Animation Curves")]
    public AnimationCurve riseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public AnimationCurve collapseCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("Shape")]
    public float radius = 1.35f;
    public float height = 4.5f;
    public float groundOffset = 0.05f;

    [Header("Procedural Water Column")]
    [Range(8, 48)] public int heightSegments = 18;
    [Range(12, 96)] public int radialSegments = 40;
    public float baseRadiusMultiplier = 1.25f;
    public float waistRadiusMultiplier = 0.72f;
    public float topRadiusMultiplier = 0.95f;
    public float topCrestHeight = 0.55f;
    public float topCrestRadiusMultiplier = 0.06f;
    public float surfaceNoiseAmount = 0.18f;
    public float surfaceNoiseScale = 3.2f;
    public float flowSpeed = 2.6f;
    public float twistAmount = 1.35f;
    public Texture2D waterNoiseTexture;
    public Vector2 textureTiling = new Vector2(2f, 1.4f);

    [Header("Color")]
    public Color waterColor = new Color(0.05f, 0.65f, 1f, 0.55f);
    public Color coreColor = new Color(0.75f, 1f, 1f, 0.85f);
    public Color foamColor = new Color(0.85f, 1f, 1f, 0.85f);

    [Header("Material Polish")]
    public Color waterEmissionColor = new Color(0.1f, 0.95f, 1f, 1f);
    public Color coreEmissionColor = new Color(0.8f, 1f, 1f, 1f);
    public float waterEmissionIntensity = 1.25f;
    public float coreEmissionIntensity = 1.6f;
    public float textureScrollX = 0.03f;
    public float textureScrollY = -0.22f;
    [Range(0f, 1f)] public float textureAlphaPulse = 0.16f;

    [Header("Base Splash")]
    public int foamCrownBurstCount = 42;
    public int sideSplashBurstCount = 34;
    public int groundMistBurstCount = 24;
    public float baseSplashRadiusMultiplier = 1.1f;
    public float sideSplashSpeed = 4.2f;
    public float foamCrownSpeed = 2.4f;
    public float groundMistSpeed = 1.2f;
    public float baseSplashParticleSize = 0.22f;
    public float groundMistParticleSize = 0.55f;

    private Transform waterColumn;
    private Transform coreColumn;
    private Transform shockRing;
    private Transform returnRing;
    private ParticleSystem sprayParticles;
    private ParticleSystem foamCrownParticles;
    private ParticleSystem sideSplashParticles;
    private ParticleSystem groundMistParticles;
    private ParticleSystem collapseDropletParticles;
    private ParticleSystem corruptedSeaMoteParticles;
    private Material waterMaterial;
    private Material coreMaterial;
    private Material foamMaterial;
    private Material particleMaterial;
    private Material corruptionMaterial;
    private Material streakMaterial;
    private Material impactFlashMaterial;
    private Material pressureRippleMaterial;
    private Transform impactFlash;
    private LineRenderer[] waterStreaks;
    private LineRenderer primaryPressureRipple;
    private LineRenderer secondaryPressureRipple;
    private Mesh waterColumnMesh;
    private Vector3[] waterColumnVertices;
    private Vector2[] waterColumnUvs;
    private Mesh coreColumnMesh;
    private Vector3[] coreColumnVertices;
    private Vector2[] coreColumnUvs;
    private float elapsed;
    private const int WaterStreakCount = 7;
    private const int WaterStreakPointCount = 11;
    private const int PressureRipplePointCount = 72;

    private void Awake()
    {
        BuildVisuals();
    }

    private void OnEnable()
    {
        elapsed = 0f;
        if (sprayParticles != null)
            sprayParticles.Play(true);
        if (foamCrownParticles != null)
            foamCrownParticles.Play(true);
        if (sideSplashParticles != null)
            sideSplashParticles.Play(true);
        if (groundMistParticles != null)
            groundMistParticles.Play(true);
        if (collapseDropletParticles != null)
            collapseDropletParticles.Play(true);
        if (corruptedSeaMoteParticles != null)
            corruptedSeaMoteParticles.Play(true);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float totalLifetime = GetTotalLifetime();
        float progress = totalLifetime > 0f ? Mathf.Clamp01(elapsed / totalLifetime) : 1f;
        float columnAmount = GetColumnAmount();
        float fade = Mathf.Clamp01(1f - progress * 0.82f);
        float wobble = 1f + Mathf.Sin(Time.time * 24f) * 0.06f;
        float currentHeight = height * columnAmount;

        if (waterColumn != null)
        {
            waterColumn.localPosition = new Vector3(0f, groundOffset, 0f);
            waterColumn.localScale = new Vector3(wobble, 1f, wobble);
            UpdateWaterColumnMesh(columnAmount);
            ScrollWaterMaterial(progress);
            SetMaterialAlpha(waterMaterial, waterColor.a * fade);
        }

        if (coreColumn != null)
        {
            coreColumn.localPosition = new Vector3(0f, groundOffset, 0f);
            coreColumn.localScale = Vector3.one;
            UpdateCoreColumnMesh(columnAmount);
            SetMaterialAlpha(coreMaterial, coreColor.a * fade);
        }

        UpdateWaterStreaks(columnAmount, fade);
        UpdateImpactReadability(columnAmount);

        if (collapseDropletParticles != null)
        {
            float crestHeight = Mathf.Max(0f, topCrestHeight) * columnAmount;
            collapseDropletParticles.transform.localPosition = new Vector3(0f, groundOffset + currentHeight + crestHeight * 0.85f, 0f);
        }

        if (shockRing != null)
        {
            float ringProgress = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, riseDuration + peakHoldDuration));
            float ringScale = Mathf.Lerp(radius * 0.5f, radius * 3.2f, Mathf.SmoothStep(0f, 1f, ringProgress));
            shockRing.localScale = new Vector3(ringScale, 0.025f, ringScale);
            SetMaterialAlpha(foamMaterial, foamColor.a * fade);
        }

        if (returnRing != null)
        {
            float collapseProgress = GetCollapseProgress();
            float ringScale = Mathf.Lerp(radius * 3.1f, radius * 0.85f, Mathf.SmoothStep(0f, 1f, collapseProgress));
            returnRing.localScale = new Vector3(ringScale, 0.018f, ringScale);
            SetMaterialAlpha(foamMaterial, foamColor.a * collapseProgress * (1f - progress));
        }

        if (destroyOnComplete && elapsed >= totalLifetime)
            Destroy(gameObject);
    }

    private void BuildVisuals()
    {
        if (waterColumn != null)
            return;

        waterMaterial = CreateTransparentMaterial("Water Burst Column", waterColor);
        ConfigureWaterMaterial(waterMaterial, waterEmissionColor, waterEmissionIntensity);
        ApplyNoiseTexture(waterMaterial);
        coreMaterial = CreateTransparentMaterial("Water Burst Core", coreColor);
        ConfigureWaterMaterial(coreMaterial, coreEmissionColor, coreEmissionIntensity);
        foamMaterial = CreateTransparentMaterial("Water Burst Foam", foamColor);
        ConfigureWaterMaterial(foamMaterial, foamColor, 0.8f);
        particleMaterial = CreateParticleMaterial("Water Burst Splash Particles", foamColor);
        corruptionMaterial = CreateParticleMaterial("Water Burst Corruption Motes", new Color(0.08f, 0.28f, 0.24f, 0.85f));
        streakMaterial = CreateTransparentMaterial("Water Burst Streaks", foamColor);
        ConfigureWaterMaterial(streakMaterial, foamColor, 1.15f);
        impactFlashMaterial = CreateTransparentMaterial("Water Burst Impact Flash", coreColor);
        ConfigureWaterMaterial(impactFlashMaterial, coreColor, 2.2f);
        pressureRippleMaterial = CreateTransparentMaterial("Water Burst Pressure Ripple", foamColor);
        ConfigureWaterMaterial(pressureRippleMaterial, foamColor, 1.25f);

        waterColumn = CreateWaterColumnMesh();
        coreColumn = CreateCoreColumnMesh();
        shockRing = CreateCylinder("GroundShockRing", radius * 0.5f, 0.025f, foamMaterial);
        returnRing = CreateCylinder("ReturnSplashRing", radius * 3.1f, 0.018f, foamMaterial);
        impactFlash = CreateCylinder("ImpactFlashCore", radius * 0.45f, height * 0.36f, impactFlashMaterial);
        SetMaterialAlpha(impactFlashMaterial, 0f);
        CreatePressureRipples();
        CreateWaterStreaks();
        CreateSprayParticles();
        CreateBaseSplashParticles();
        CreateCollapseDroplets();
        CreateCorruptedSeaMotes();
    }

    private float GetColumnAmount()
    {
        float rise = Mathf.Max(0.01f, riseDuration);
        float holdEnd = rise + Mathf.Max(0f, peakHoldDuration);
        float fall = Mathf.Max(0.01f, collapseDuration);

        if (elapsed <= rise)
            return Evaluate01(riseCurve, elapsed / rise);

        if (elapsed <= holdEnd)
            return 1f;

        float fallProgress = Mathf.Clamp01((elapsed - holdEnd) / fall);
        return Evaluate01(collapseCurve, fallProgress);
    }

    private float GetCollapseProgress()
    {
        float collapseStart = Mathf.Max(0.01f, riseDuration + peakHoldDuration);
        float collapseTime = Mathf.Max(0.01f, collapseDuration);
        return Mathf.Clamp01((elapsed - collapseStart) / collapseTime);
    }

    private float GetTotalLifetime()
    {
        float animationLifetime = Mathf.Max(0.01f, riseDuration) + Mathf.Max(0f, peakHoldDuration) + Mathf.Max(0.01f, collapseDuration);
        return Mathf.Max(duration, animationLifetime);
    }

    private float Evaluate01(AnimationCurve curve, float time)
    {
        if (curve == null || curve.length == 0)
            return Mathf.Clamp01(time);

        return Mathf.Clamp01(curve.Evaluate(Mathf.Clamp01(time)));
    }

    private Transform CreateCylinder(string objectName, float cylinderRadius, float cylinderHalfHeight, Material material)
    {
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = objectName;
        cylinder.transform.SetParent(transform, false);
        cylinder.transform.localPosition = new Vector3(0f, groundOffset + cylinderHalfHeight, 0f);
        cylinder.transform.localScale = new Vector3(cylinderRadius, cylinderHalfHeight, cylinderRadius);
        DestroyCollider(cylinder);
        SetRendererMaterial(cylinder, material);
        return cylinder.transform;
    }

    private Transform CreateWaterColumnMesh()
    {
        GameObject columnObject = new GameObject("ProceduralWaterColumn");
        columnObject.transform.SetParent(transform, false);
        columnObject.transform.localPosition = new Vector3(0f, groundOffset, 0f);

        MeshFilter meshFilter = columnObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = columnObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = waterMaterial;

        waterColumnMesh = new Mesh { name = "Procedural Water Burst Column" };
        meshFilter.sharedMesh = waterColumnMesh;
        BuildWaterColumnMesh();
        return columnObject.transform;
    }

    private Transform CreateCoreColumnMesh()
    {
        GameObject coreObject = new GameObject("ProceduralWhiteCore");
        coreObject.transform.SetParent(transform, false);
        coreObject.transform.localPosition = new Vector3(0f, groundOffset, 0f);

        MeshFilter meshFilter = coreObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = coreObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = coreMaterial;

        coreColumnMesh = new Mesh { name = "Procedural Water Burst Core" };
        meshFilter.sharedMesh = coreColumnMesh;
        BuildCoreColumnMesh();
        return coreObject.transform;
    }

    private void BuildWaterColumnMesh()
    {
        int verticalCount = Mathf.Max(2, heightSegments + 1);
        int radialCount = Mathf.Max(12, radialSegments);
        int capRingStart = verticalCount * radialCount;
        int apexIndex = capRingStart + radialCount;
        int vertexCount = apexIndex + 1;

        waterColumnVertices = new Vector3[vertexCount];
        waterColumnUvs = new Vector2[vertexCount];
        int[] triangles = new int[((heightSegments + 1) * radialCount * 12) + (radialCount * 6)];

        int triangleIndex = 0;
        for (int y = 0; y < heightSegments; y++)
        {
            for (int x = 0; x < radialCount; x++)
            {
                int nextX = (x + 1) % radialCount;
                int current = y * radialCount + x;
                int next = y * radialCount + nextX;
                int above = (y + 1) * radialCount + x;
                int aboveNext = (y + 1) * radialCount + nextX;

                triangles[triangleIndex++] = current;
                triangles[triangleIndex++] = above;
                triangles[triangleIndex++] = next;
                triangles[triangleIndex++] = next;
                triangles[triangleIndex++] = above;
                triangles[triangleIndex++] = aboveNext;

                triangles[triangleIndex++] = current;
                triangles[triangleIndex++] = next;
                triangles[triangleIndex++] = above;
                triangles[triangleIndex++] = next;
                triangles[triangleIndex++] = aboveNext;
                triangles[triangleIndex++] = above;
            }
        }

        int topRingStart = (verticalCount - 1) * radialCount;
        for (int x = 0; x < radialCount; x++)
        {
            int nextX = (x + 1) % radialCount;
            int current = topRingStart + x;
            int next = topRingStart + nextX;
            int above = capRingStart + x;
            int aboveNext = capRingStart + nextX;

            triangles[triangleIndex++] = current;
            triangles[triangleIndex++] = above;
            triangles[triangleIndex++] = next;
            triangles[triangleIndex++] = next;
            triangles[triangleIndex++] = above;
            triangles[triangleIndex++] = aboveNext;

            triangles[triangleIndex++] = current;
            triangles[triangleIndex++] = next;
            triangles[triangleIndex++] = above;
            triangles[triangleIndex++] = next;
            triangles[triangleIndex++] = aboveNext;
            triangles[triangleIndex++] = above;

            triangles[triangleIndex++] = above;
            triangles[triangleIndex++] = apexIndex;
            triangles[triangleIndex++] = aboveNext;
            triangles[triangleIndex++] = above;
            triangles[triangleIndex++] = aboveNext;
            triangles[triangleIndex++] = apexIndex;
        }

        waterColumnMesh.Clear();
        waterColumnMesh.vertices = waterColumnVertices;
        waterColumnMesh.uv = waterColumnUvs;
        waterColumnMesh.triangles = triangles;
        UpdateWaterColumnMesh(0f);
    }

    private void UpdateWaterColumnMesh(float columnAmount)
    {
        if (waterColumnMesh == null || waterColumnVertices == null)
            return;

        int verticalCount = Mathf.Max(2, heightSegments + 1);
        int radialCount = Mathf.Max(12, radialSegments);
        float visibleHeight = Mathf.Max(0.001f, height * columnAmount);
        float crestHeight = Mathf.Max(0f, topCrestHeight) * columnAmount;
        float timeOffset = Time.time * flowSpeed;

        for (int y = 0; y < verticalCount; y++)
        {
            float v = (float)y / (verticalCount - 1);
            float yPosition = visibleHeight * v;
            float profile = GetColumnProfile(v);
            float twist = v * Mathf.PI * 2f * twistAmount + timeOffset * 0.55f;

            for (int x = 0; x < radialCount; x++)
            {
                float u = (float)x / radialCount;
                float angle = u * Mathf.PI * 2f + twist;
                float noise = GetSurfaceNoise(angle, v, timeOffset);
                float localRadius = radius * profile * (1f + noise * surfaceNoiseAmount);

                int index = y * radialCount + x;
                waterColumnVertices[index] = new Vector3(Mathf.Cos(angle) * localRadius, yPosition, Mathf.Sin(angle) * localRadius);
                waterColumnUvs[index] = new Vector2(u * textureTiling.x, (v * textureTiling.y) - timeOffset * 0.12f);
            }
        }

        int capRingStart = verticalCount * radialCount;
        int apexIndex = capRingStart + radialCount;
        float capRingY = visibleHeight + crestHeight * 0.55f;
        float capRingProfile = Mathf.Lerp(GetColumnProfile(1f), Mathf.Max(0.01f, topCrestRadiusMultiplier), 0.72f);
        float capTwist = Mathf.PI * 2f * twistAmount + timeOffset * 0.55f;

        for (int x = 0; x < radialCount; x++)
        {
            float u = (float)x / radialCount;
            float angle = u * Mathf.PI * 2f + capTwist;
            float noise = GetSurfaceNoise(angle + 2.4f, 1.08f, timeOffset * 1.2f);
            float localRadius = radius * capRingProfile * (1f + noise * surfaceNoiseAmount * 0.65f);
            int index = capRingStart + x;
            waterColumnVertices[index] = new Vector3(Mathf.Cos(angle) * localRadius, capRingY, Mathf.Sin(angle) * localRadius);
            waterColumnUvs[index] = new Vector2(u * textureTiling.x, (textureTiling.y * 1.08f) - timeOffset * 0.12f);
        }

        float apexWander = radius * 0.045f * columnAmount;
        waterColumnVertices[apexIndex] = new Vector3(
            Mathf.Sin(Time.time * 4.1f) * apexWander,
            visibleHeight + crestHeight,
            Mathf.Cos(Time.time * 3.6f) * apexWander);
        waterColumnUvs[apexIndex] = new Vector2(textureTiling.x * 0.5f, textureTiling.y * 1.18f);

        waterColumnMesh.vertices = waterColumnVertices;
        waterColumnMesh.uv = waterColumnUvs;
        waterColumnMesh.RecalculateNormals();
        waterColumnMesh.RecalculateBounds();
    }

    private void BuildCoreColumnMesh()
    {
        int verticalCount = Mathf.Max(2, heightSegments + 1);
        int radialCount = Mathf.Max(12, radialSegments / 2);
        int capRingStart = verticalCount * radialCount;
        int apexIndex = capRingStart + radialCount;
        int vertexCount = apexIndex + 1;

        coreColumnVertices = new Vector3[vertexCount];
        coreColumnUvs = new Vector2[vertexCount];
        int[] triangles = new int[((heightSegments + 1) * radialCount * 12) + (radialCount * 6)];

        int triangleIndex = 0;
        for (int y = 0; y < heightSegments; y++)
        {
            for (int x = 0; x < radialCount; x++)
            {
                int nextX = (x + 1) % radialCount;
                int current = y * radialCount + x;
                int next = y * radialCount + nextX;
                int above = (y + 1) * radialCount + x;
                int aboveNext = (y + 1) * radialCount + nextX;

                triangles[triangleIndex++] = current;
                triangles[triangleIndex++] = above;
                triangles[triangleIndex++] = next;
                triangles[triangleIndex++] = next;
                triangles[triangleIndex++] = above;
                triangles[triangleIndex++] = aboveNext;

                triangles[triangleIndex++] = current;
                triangles[triangleIndex++] = next;
                triangles[triangleIndex++] = above;
                triangles[triangleIndex++] = next;
                triangles[triangleIndex++] = aboveNext;
                triangles[triangleIndex++] = above;
            }
        }

        int topRingStart = (verticalCount - 1) * radialCount;
        for (int x = 0; x < radialCount; x++)
        {
            int nextX = (x + 1) % radialCount;
            int current = topRingStart + x;
            int next = topRingStart + nextX;
            int above = capRingStart + x;
            int aboveNext = capRingStart + nextX;

            triangles[triangleIndex++] = current;
            triangles[triangleIndex++] = above;
            triangles[triangleIndex++] = next;
            triangles[triangleIndex++] = next;
            triangles[triangleIndex++] = above;
            triangles[triangleIndex++] = aboveNext;

            triangles[triangleIndex++] = current;
            triangles[triangleIndex++] = next;
            triangles[triangleIndex++] = above;
            triangles[triangleIndex++] = next;
            triangles[triangleIndex++] = aboveNext;
            triangles[triangleIndex++] = above;

            triangles[triangleIndex++] = above;
            triangles[triangleIndex++] = apexIndex;
            triangles[triangleIndex++] = aboveNext;
            triangles[triangleIndex++] = above;
            triangles[triangleIndex++] = aboveNext;
            triangles[triangleIndex++] = apexIndex;
        }

        coreColumnMesh.Clear();
        coreColumnMesh.vertices = coreColumnVertices;
        coreColumnMesh.uv = coreColumnUvs;
        coreColumnMesh.triangles = triangles;
        UpdateCoreColumnMesh(0f);
    }

    private void UpdateCoreColumnMesh(float columnAmount)
    {
        if (coreColumnMesh == null || coreColumnVertices == null)
            return;

        int verticalCount = Mathf.Max(2, heightSegments + 1);
        int radialCount = Mathf.Max(12, radialSegments / 2);
        float visibleHeight = Mathf.Max(0.001f, height * columnAmount);
        float crestHeight = Mathf.Max(0f, topCrestHeight) * 0.82f * columnAmount;
        float timeOffset = Time.time * flowSpeed;

        for (int y = 0; y < verticalCount; y++)
        {
            float v = (float)y / (verticalCount - 1);
            float yPosition = visibleHeight * v;
            float profile = Mathf.Lerp(0.2f, 0.38f, Mathf.Sin(v * Mathf.PI));
            float twist = v * Mathf.PI * 2f * (twistAmount * 0.65f) - timeOffset * 0.35f;

            for (int x = 0; x < radialCount; x++)
            {
                float u = (float)x / radialCount;
                float angle = u * Mathf.PI * 2f + twist;
                float noise = GetSurfaceNoise(angle + 1.7f, v, timeOffset * 1.4f);
                float localRadius = radius * profile * (1f + noise * surfaceNoiseAmount * 0.45f);

                int index = y * radialCount + x;
                coreColumnVertices[index] = new Vector3(Mathf.Cos(angle) * localRadius, yPosition, Mathf.Sin(angle) * localRadius);
                coreColumnUvs[index] = new Vector2(u, v);
            }
        }

        int capRingStart = verticalCount * radialCount;
        int apexIndex = capRingStart + radialCount;
        float capRingY = visibleHeight + crestHeight * 0.55f;
        float capRingProfile = Mathf.Lerp(0.2f, Mathf.Max(0.01f, topCrestRadiusMultiplier), 0.75f);
        float capTwist = Mathf.PI * 2f * (twistAmount * 0.65f) - timeOffset * 0.35f;

        for (int x = 0; x < radialCount; x++)
        {
            float u = (float)x / radialCount;
            float angle = u * Mathf.PI * 2f + capTwist;
            float noise = GetSurfaceNoise(angle + 3.9f, 1.08f, timeOffset * 1.5f);
            float localRadius = radius * capRingProfile * (1f + noise * surfaceNoiseAmount * 0.32f);
            int index = capRingStart + x;
            coreColumnVertices[index] = new Vector3(Mathf.Cos(angle) * localRadius, capRingY, Mathf.Sin(angle) * localRadius);
            coreColumnUvs[index] = new Vector2(u, 1.08f);
        }

        float apexWander = radius * 0.025f * columnAmount;
        coreColumnVertices[apexIndex] = new Vector3(
            Mathf.Sin(Time.time * 4.8f) * apexWander,
            visibleHeight + crestHeight,
            Mathf.Cos(Time.time * 4.2f) * apexWander);
        coreColumnUvs[apexIndex] = new Vector2(0.5f, 1.18f);

        coreColumnMesh.vertices = coreColumnVertices;
        coreColumnMesh.uv = coreColumnUvs;
        coreColumnMesh.RecalculateNormals();
        coreColumnMesh.RecalculateBounds();
    }

    private float GetColumnProfile(float heightPercent)
    {
        float baseBlend = 1f - Mathf.SmoothStep(0f, 0.38f, heightPercent);
        float topBlend = Mathf.SmoothStep(0.72f, 1f, heightPercent);
        float waistBlend = Mathf.Sin(heightPercent * Mathf.PI);

        float profile = Mathf.Lerp(1f, baseRadiusMultiplier, baseBlend);
        profile = Mathf.Lerp(profile, waistRadiusMultiplier, waistBlend * 0.55f);
        profile = Mathf.Lerp(profile, topRadiusMultiplier, topBlend);
        return Mathf.Max(0.05f, profile);
    }

    private float GetSurfaceNoise(float angle, float heightPercent, float timeOffset)
    {
        float sampleX = Mathf.Cos(angle) * surfaceNoiseScale + timeOffset;
        float sampleY = Mathf.Sin(angle) * surfaceNoiseScale + heightPercent * surfaceNoiseScale + timeOffset * 0.37f;
        return Mathf.PerlinNoise(sampleX, sampleY) * 2f - 1f;
    }

    private void CreateWaterStreaks()
    {
        GameObject streakRoot = new GameObject("RisingWaterStreaks");
        streakRoot.transform.SetParent(transform, false);

        waterStreaks = new LineRenderer[WaterStreakCount];
        for (int i = 0; i < waterStreaks.Length; i++)
        {
            GameObject streakObject = new GameObject($"WaterStreak_{i + 1}");
            streakObject.transform.SetParent(streakRoot.transform, false);

            LineRenderer line = streakObject.AddComponent<LineRenderer>();
            line.positionCount = WaterStreakPointCount;
            line.useWorldSpace = false;
            line.loop = false;
            line.numCapVertices = 3;
            line.numCornerVertices = 3;
            line.textureMode = LineTextureMode.Stretch;
            line.alignment = LineAlignment.View;
            line.sharedMaterial = streakMaterial;
            line.widthCurve = new AnimationCurve(
                new Keyframe(0f, 0.25f),
                new Keyframe(0.2f, 0.95f),
                new Keyframe(0.72f, 0.65f),
                new Keyframe(1f, 0.08f));

            waterStreaks[i] = line;
        }
    }

    private void UpdateWaterStreaks(float columnAmount, float fade)
    {
        if (waterStreaks == null || waterStreaks.Length == 0)
            return;

        float visibleHeight = height * columnAmount;
        float crestHeight = Mathf.Max(0f, topCrestHeight) * columnAmount;
        float lineAlpha = fade * Mathf.SmoothStep(0f, 0.35f, columnAmount);
        SetMaterialAlpha(streakMaterial, foamColor.a * lineAlpha * 0.7f);

        for (int i = 0; i < waterStreaks.Length; i++)
        {
            LineRenderer line = waterStreaks[i];
            if (line == null)
                continue;

            line.enabled = columnAmount > 0.02f;
            if (!line.enabled)
                continue;

            float seed = i * 1.618f;
            float phase = ((float)i / waterStreaks.Length) * Mathf.PI * 2f;
            float climb = Time.time * flowSpeed * 0.28f;
            float brightness = 0.55f + Mathf.PerlinNoise(seed, Time.time * 0.8f) * 0.45f;
            Color startColor = Color.Lerp(waterColor, foamColor, brightness);
            Color endColor = foamColor;
            startColor.a = foamColor.a * lineAlpha * 0.72f;
            endColor.a = foamColor.a * lineAlpha * 0.12f;
            line.startColor = startColor;
            line.endColor = endColor;
            line.widthMultiplier = Mathf.Max(0.015f, radius * Mathf.Lerp(0.024f, 0.045f, brightness));

            for (int point = 0; point < WaterStreakPointCount; point++)
            {
                float t = (float)point / (WaterStreakPointCount - 1);
                float crestT = Mathf.SmoothStep(0.78f, 1f, t);
                float yPosition = groundOffset + visibleHeight * t + crestHeight * crestT;
                float profile = GetColumnProfile(Mathf.Clamp01(t));
                profile = Mathf.Lerp(profile, Mathf.Max(0.02f, topCrestRadiusMultiplier * 1.8f), crestT);

                float angle = phase + t * Mathf.PI * 2f * (twistAmount * 0.34f) + climb + Mathf.Sin(Time.time * 2.2f + seed + t * 4.5f) * 0.16f;
                float ripple = Mathf.PerlinNoise(seed + t * 3.2f, Time.time * 1.4f) * 2f - 1f;
                float localRadius = radius * profile * (1.04f + ripple * 0.08f);
                Vector3 position = new Vector3(Mathf.Cos(angle) * localRadius, yPosition, Mathf.Sin(angle) * localRadius);
                line.SetPosition(point, position);
            }
        }
    }

    private void CreatePressureRipples()
    {
        GameObject rippleRoot = new GameObject("ImpactPressureRipples");
        rippleRoot.transform.SetParent(transform, false);

        primaryPressureRipple = CreatePressureRipple("PrimaryPressureRipple", rippleRoot.transform);
        secondaryPressureRipple = CreatePressureRipple("SecondaryPressureRipple", rippleRoot.transform);
    }

    private LineRenderer CreatePressureRipple(string objectName, Transform parent)
    {
        GameObject rippleObject = new GameObject(objectName);
        rippleObject.transform.SetParent(parent, false);

        LineRenderer line = rippleObject.AddComponent<LineRenderer>();
        line.positionCount = PressureRipplePointCount;
        line.useWorldSpace = false;
        line.loop = true;
        line.numCapVertices = 3;
        line.numCornerVertices = 3;
        line.textureMode = LineTextureMode.Stretch;
        line.alignment = LineAlignment.View;
        line.sharedMaterial = pressureRippleMaterial;
        line.enabled = false;
        return line;
    }

    private void UpdateImpactReadability(float columnAmount)
    {
        float hitStart = Mathf.Max(0.01f, riseDuration * 0.92f);
        float flashProgress = Mathf.Clamp01((elapsed - hitStart) / 0.24f);
        bool flashActive = elapsed >= hitStart && flashProgress < 1f;
        float flashPulse = flashActive ? Mathf.Sin(flashProgress * Mathf.PI) * (1f - flashProgress * 0.25f) : 0f;

        if (impactFlash != null)
        {
            impactFlash.gameObject.SetActive(flashPulse > 0.01f);
            if (impactFlash.gameObject.activeSelf)
            {
                float currentHeight = Mathf.Max(0.01f, height * Mathf.Max(columnAmount, 0.18f));
                float flashHalfHeight = currentHeight * Mathf.Lerp(0.2f, 0.48f, flashPulse);
                float flashRadius = radius * Mathf.Lerp(0.42f, 1.08f, flashProgress);

                impactFlash.localPosition = new Vector3(0f, groundOffset + flashHalfHeight, 0f);
                impactFlash.localScale = new Vector3(flashRadius, flashHalfHeight, flashRadius);
                SetMaterialAlpha(impactFlashMaterial, coreColor.a * flashPulse * 0.82f);
            }
        }

        float primaryProgress = Mathf.Clamp01((elapsed - hitStart) / 0.46f);
        float secondaryProgress = Mathf.Clamp01((elapsed - hitStart - 0.08f) / 0.52f);
        UpdatePressureRipple(primaryPressureRipple, primaryProgress, elapsed >= hitStart, 1f);
        UpdatePressureRipple(secondaryPressureRipple, secondaryProgress, elapsed >= hitStart + 0.08f, 0.58f);
    }

    private void UpdatePressureRipple(LineRenderer line, float rippleProgress, bool hasStarted, float alphaMultiplier)
    {
        if (line == null)
            return;

        bool active = hasStarted && rippleProgress < 1f;
        line.enabled = active;
        if (!active)
            return;

        float eased = Mathf.SmoothStep(0f, 1f, rippleProgress);
        float rippleRadius = Mathf.Lerp(radius * 0.55f, radius * 2.75f, eased);
        float alpha = Mathf.Sin(rippleProgress * Mathf.PI) * alphaMultiplier * 0.82f;

        Color startColor = foamColor;
        Color endColor = waterColor;
        startColor.a = foamColor.a * alpha;
        endColor.a = waterColor.a * alpha * 0.35f;
        line.startColor = startColor;
        line.endColor = endColor;
        line.widthMultiplier = Mathf.Lerp(0.12f, 0.08f, rippleProgress);

        for (int i = 0; i < PressureRipplePointCount; i++)
        {
            float t = (float)i / PressureRipplePointCount;
            float angle = t * Mathf.PI * 2f;
            float wobble = 1f + Mathf.Sin(angle * 5f + Time.time * 10f) * 0.018f;
            Vector3 position = new Vector3(
                Mathf.Cos(angle) * rippleRadius * wobble,
                groundOffset + 0.045f,
                Mathf.Sin(angle) * rippleRadius * wobble);
            line.SetPosition(i, position);
        }
    }

    private void ApplyNoiseTexture(Material material)
    {
        if (material == null || waterNoiseTexture == null)
            return;

        material.mainTexture = waterNoiseTexture;
        material.mainTextureScale = textureTiling;
        material.SetTexture("_BaseMap", waterNoiseTexture);
        material.SetTextureScale("_BaseMap", textureTiling);
    }

    private void ScrollWaterMaterial(float progress)
    {
        if (waterMaterial == null || waterNoiseTexture == null)
            return;

        Vector2 offset = new Vector2(Time.time * textureScrollX, Time.time * textureScrollY + progress * 0.2f);
        waterMaterial.mainTextureOffset = offset;
        waterMaterial.SetTextureOffset("_BaseMap", offset);
    }

    private void CreateSprayParticles()
    {
        GameObject sprayObject = new GameObject("UpwardSpray");
        sprayObject.transform.SetParent(transform, false);
        sprayObject.transform.localPosition = new Vector3(0f, groundOffset + 0.15f, 0f);

        sprayParticles = sprayObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = sprayParticles.main;
        main.duration = GetTotalLifetime();
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.75f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 8.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.22f);
        main.startColor = new ParticleSystem.MinMaxGradient(foamColor, waterColor);
        main.gravityModifier = 1.3f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = true;

        ParticleSystem.EmissionModule emission = sprayParticles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, 55),
            new ParticleSystem.Burst(0.08f, 28)
        });

        ParticleSystem.ShapeModule shape = sprayParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.radius = radius * 0.55f;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = sprayParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(foamColor, 0f),
                new GradientColorKey(waterColor, 0.55f),
                new GradientColorKey(Color.clear, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.95f, 0f),
                new GradientAlphaKey(0.65f, 0.55f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;
    }

    private void CreateBaseSplashParticles()
    {
        foamCrownParticles = CreateSplashParticleSystem(
            "FoamCrown",
            foamCrownBurstCount,
            foamCrownSpeed,
            new Vector2(baseSplashParticleSize * 0.55f, baseSplashParticleSize * 1.15f),
            new Vector2(0.28f, 0.58f),
            0.8f,
            42f,
            radius * baseSplashRadiusMultiplier,
            0.02f,
            new ParticleSystem.MinMaxGradient(foamColor, coreColor));

        sideSplashParticles = CreateSplashParticleSystem(
            "SideSplashes",
            sideSplashBurstCount,
            sideSplashSpeed,
            new Vector2(baseSplashParticleSize * 0.4f, baseSplashParticleSize),
            new Vector2(0.35f, 0.82f),
            1.4f,
            68f,
            radius * baseSplashRadiusMultiplier * 0.85f,
            0.08f,
            new ParticleSystem.MinMaxGradient(waterColor, foamColor));

        groundMistParticles = CreateSplashParticleSystem(
            "GroundMist",
            groundMistBurstCount,
            groundMistSpeed,
            new Vector2(groundMistParticleSize * 0.55f, groundMistParticleSize),
            new Vector2(0.45f, 0.95f),
            0.05f,
            88f,
            radius * baseSplashRadiusMultiplier * 1.25f,
            0.01f,
            new ParticleSystem.MinMaxGradient(new Color(foamColor.r, foamColor.g, foamColor.b, 0.35f), new Color(waterColor.r, waterColor.g, waterColor.b, 0.18f)));
    }

    private void CreateCollapseDroplets()
    {
        GameObject dropletObject = new GameObject("CollapseFallingWater");
        dropletObject.transform.SetParent(transform, false);
        dropletObject.transform.localPosition = new Vector3(0f, groundOffset + height + Mathf.Max(0f, topCrestHeight), 0f);

        collapseDropletParticles = dropletObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = collapseDropletParticles.main;
        main.duration = GetTotalLifetime();
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.85f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 1.25f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.19f);
        main.startColor = new ParticleSystem.MinMaxGradient(coreColor, foamColor);
        main.gravityModifier = 2.35f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = true;

        ParticleSystem.EmissionModule emission = collapseDropletParticles.emission;
        emission.rateOverTime = 0f;
        float collapseStart = Mathf.Max(0.01f, riseDuration + peakHoldDuration);
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(collapseStart, 34),
            new ParticleSystem.Burst(collapseStart + 0.12f, 22),
            new ParticleSystem.Burst(collapseStart + 0.26f, 12)
        });

        ParticleSystem.ShapeModule shape = collapseDropletParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = radius * 0.42f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = collapseDropletParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(foamColor, 0f),
                new GradientColorKey(waterColor, 0.55f),
                new GradientColorKey(Color.clear, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.9f, 0f),
                new GradientAlphaKey(0.55f, 0.55f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        ParticleSystemRenderer particleRenderer = collapseDropletParticles.GetComponent<ParticleSystemRenderer>();
        if (particleRenderer != null)
        {
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.sharedMaterial = particleMaterial;
        }
    }

    private void CreateCorruptedSeaMotes()
    {
        GameObject moteObject = new GameObject("CorruptedSeaMotes");
        moteObject.transform.SetParent(transform, false);
        moteObject.transform.localPosition = new Vector3(0f, groundOffset + height * 0.32f, 0f);

        corruptedSeaMoteParticles = moteObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = corruptedSeaMoteParticles.main;
        main.duration = GetTotalLifetime();
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 1.05f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.45f, 1.55f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.105f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.02f, 0.13f, 0.11f, 0.82f),
            new Color(0.18f, 0.55f, 0.38f, 0.58f));
        main.gravityModifier = 0.38f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = true;

        ParticleSystem.EmissionModule emission = corruptedSeaMoteParticles.emission;
        emission.rateOverTime = 0f;
        float hitStart = Mathf.Max(0.01f, riseDuration * 0.92f);
        float collapseStart = Mathf.Max(0.01f, riseDuration + peakHoldDuration);
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0.04f, 12),
            new ParticleSystem.Burst(hitStart, 24),
            new ParticleSystem.Burst(collapseStart + 0.08f, 18),
            new ParticleSystem.Burst(collapseStart + 0.28f, 10)
        });

        ParticleSystem.ShapeModule shape = corruptedSeaMoteParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = radius * 1.15f;

        ParticleSystem.NoiseModule noise = corruptedSeaMoteParticles.noise;
        noise.enabled = true;
        noise.strength = 0.28f;
        noise.frequency = 0.85f;
        noise.scrollSpeed = 0.35f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = corruptedSeaMoteParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.05f, 0.2f, 0.17f), 0f),
                new GradientColorKey(new Color(0.2f, 0.58f, 0.38f), 0.45f),
                new GradientColorKey(Color.clear, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.72f, 0f),
                new GradientAlphaKey(0.42f, 0.55f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        ParticleSystemRenderer particleRenderer = corruptedSeaMoteParticles.GetComponent<ParticleSystemRenderer>();
        if (particleRenderer != null)
        {
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.sharedMaterial = corruptionMaterial;
        }
    }

    private ParticleSystem CreateSplashParticleSystem(
        string objectName,
        int burstCount,
        float startSpeed,
        Vector2 sizeRange,
        Vector2 lifetimeRange,
        float gravity,
        float coneAngle,
        float shapeRadius,
        float yOffset,
        ParticleSystem.MinMaxGradient startColor)
    {
        GameObject particleObject = new GameObject(objectName);
        particleObject.transform.SetParent(transform, false);
        particleObject.transform.localPosition = new Vector3(0f, groundOffset + yOffset, 0f);

        ParticleSystem particles = particleObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = particles.main;
        main.duration = GetTotalLifetime();
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetimeRange.x, lifetimeRange.y);
        main.startSpeed = new ParticleSystem.MinMaxCurve(startSpeed * 0.65f, startSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(sizeRange.x, sizeRange.y);
        main.startColor = startColor;
        main.gravityModifier = gravity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = true;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, (short)Mathf.Max(0, burstCount)),
            new ParticleSystem.Burst(0.08f, (short)Mathf.Max(0, Mathf.RoundToInt(burstCount * 0.35f)))
        });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = coneAngle;
        shape.radius = shapeRadius;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(foamColor, 0f),
                new GradientColorKey(waterColor, 0.65f),
                new GradientColorKey(Color.clear, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.95f, 0f),
                new GradientAlphaKey(0.45f, 0.65f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        ParticleSystemRenderer particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
        if (particleRenderer != null)
        {
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.sharedMaterial = particleMaterial;
        }

        return particles;
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

    private Material CreateParticleMaterial(string materialName, Color color)
    {
        Material material = new Material(Shader.Find("Particles/Standard Unlit"));
        if (material.shader == null)
            material = new Material(Shader.Find("Sprites/Default"));

        material.name = materialName;
        material.color = color;
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 1f);
        material.renderQueue = 3000;
        return material;
    }

    private void ConfigureWaterMaterial(Material material, Color emissionColor, float emissionIntensity)
    {
        if (material == null)
            return;

        material.EnableKeyword("_EMISSION");
        Color finalEmission = emissionColor * Mathf.Max(0f, emissionIntensity);
        material.SetColor("_EmissionColor", finalEmission);
        material.SetColor("_BaseColor", material.color);
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.renderQueue = 3000;
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
        if (material == waterMaterial)
            alpha *= 1f + Mathf.Sin(Time.time * 12f) * textureAlphaPulse;

        color.a = alpha;
        material.color = color;
        material.SetColor("_BaseColor", color);
    }

    private void DestroyCollider(GameObject target)
    {
        Collider collider = target.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
    }
}
