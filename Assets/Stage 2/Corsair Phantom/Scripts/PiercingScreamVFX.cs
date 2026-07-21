using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Layered visual composition for the Corsair Phantom's Piercing Scream.
/// This effect only supports gameplay timing; it never applies damage,
/// stun, knockback, tile cracking, or death. Layers:
///   1. Mouth charge  - emissive glow + inward wisps during the windup.
///   2. Warning cone   - faint translucent telegraph during the windup.
///   3. Scream rings   - fast travelling rings on release (PiercingScreamWaveVFX).
///   4. Spectral wisps - small ghostly particles trailing the scream.
///   5. Hit pulse      - short bright flash at the impact point (only on hit).
/// The component owns and cleans up every sub-object it creates.
/// </summary>
public class PiercingScreamVFX : MonoBehaviour
{
    [Header("Mouth Charge (Windup)")]
    public Color chargeColor = new Color(0.35f, 1f, 1f, 1f);
    public float chargeGlowMaxSize = 0.55f;
    public float chargePulseSpeed = 9f;
    public float chargePulseAmount = 0.12f;
    public int chargeParticleCount = 14;
    public float chargeParticleRadius = 0.75f;
    public float chargeParticleSize = 0.09f;

    [Header("Warning Cone (Windup)")]
    public Color warningConeColor = new Color(0.3f, 0.95f, 1f, 0.14f);
    public int warningConeSegments = 24;
    [Range(0f, 1f)] public float warningConePeakAlpha = 0.16f;

    [Header("Scream Rings (Release)")]
    public PiercingScreamWaveVFX ringLayerPrefab;
    public bool createRingLayerIfPrefabMissing = true;

    [Header("Spectral Wisps (Release)")]
    public Color wispColor = new Color(0.55f, 1f, 1f, 1f);
    public int wispCount = 16;
    public float wispSize = 0.08f;
    public float wispLifetime = 0.5f;
    public float wispSpeed = 9f;

    [Header("Hit Pulse (Release, hit only)")]
    public Color hitPulseColor = new Color(0.6f, 1f, 1f, 1f);
    public float hitPulseDuration = 0.18f;
    public float hitPulseMaxRadius = 1.35f;
    public int hitPulseSegments = 40;

    private const float ReleaseTailTime = 0.9f;
    private const float MissingReleaseSafetyTime = 0.35f;

    private Transform mouthTransform;
    private Vector3 travelDirection = Vector3.forward;
    private float travelRange = 7f;
    private float coneAngle = 45f;
    private float chargeDuration = 0.6f;
    private float chargeStartTime;
    private bool released;

    private Transform chargeGlow;
    private Material chargeGlowMaterial;
    private MeshRenderer warningConeRenderer;
    private Material warningConeMaterial;
    private Camera billboardCamera;

    private readonly List<GameObject> ownedObjects = new List<GameObject>();
    private readonly List<Material> ownedMaterials = new List<Material>();

    /// <summary>
    /// Starts the windup layers (mouth charge + faint warning cone).
    /// </summary>
    /// <param name="mouth">Transform the effect follows (the phantom's mouth).</param>
    /// <param name="forward">Direction the scream will travel toward the target.</param>
    /// <param name="windupDuration">Seconds the charge builds before release.</param>
    /// <param name="range">Cone range used to size the warning telegraph.</param>
    /// <param name="angleDegrees">Cone angle used to size the warning telegraph.</param>
    public void BeginCharge(Transform mouth, Vector3 forward, float windupDuration, float range, float angleDegrees)
    {
        mouthTransform = mouth;
        travelDirection = Normalize(forward, Vector3.forward);
        chargeDuration = Mathf.Max(0.01f, windupDuration);
        travelRange = Mathf.Max(0.1f, range);
        coneAngle = Mathf.Clamp(angleDegrees, 1f, 179f);
        chargeStartTime = Time.time;
        released = false;

        billboardCamera = Camera.main;

        if (mouthTransform != null)
            transform.SetParent(mouthTransform, false);
        else
            transform.position = forward == Vector3.zero ? transform.position : transform.position;

        transform.rotation = Quaternion.LookRotation(travelDirection, Vector3.up);

        BuildChargeGlow();
        BuildChargeParticles();
        BuildWarningCone();
    }

    /// <summary>
    /// Fires the release layers (rings + wisps) and, when the cone connected,
    /// a short readable hit pulse at the impact point.
    /// </summary>
    /// <param name="forward">Final travel direction at the moment of release.</param>
    /// <param name="hit">True when the player was inside the cone.</param>
    /// <param name="impactPoint">World position for the hit pulse.</param>
    public void Release(Vector3 forward, bool hit, Vector3 impactPoint)
    {
        if (released)
            return;

        released = true;
        travelDirection = Normalize(forward, travelDirection);
        transform.rotation = Quaternion.LookRotation(travelDirection, Vector3.up);

        SpawnScreamRings();
        SpawnSpectralWisps();

        if (hit)
            SpawnHitPulse(impactPoint);

        FadeOutCharge();

        Destroy(gameObject, ReleaseTailTime);
    }

    /// <summary>
    /// Cancels the effect when the scream is interrupted before release.
    /// </summary>
    public void Cancel()
    {
        Destroy(gameObject, 0.15f);
    }

    private void Update()
    {
        if (!released)
            UpdateCharge();

        if (billboardCamera == null)
            billboardCamera = Camera.main;

        FaceCamera(chargeGlow);

        // Fallback cleanup if the release hook never arrives.
        if (!released && Time.time - chargeStartTime > chargeDuration + MissingReleaseSafetyTime)
            Cancel();
    }

    private void UpdateCharge()
    {
        float chargeProgress = Mathf.Clamp01((Time.time - chargeStartTime) / chargeDuration);

        if (chargeGlow != null)
        {
            float pulse = 1f + Mathf.Sin(Time.time * chargePulseSpeed) * chargePulseAmount;
            float size = chargeGlowMaxSize * Mathf.SmoothStep(0f, 1f, chargeProgress) * pulse;
            chargeGlow.localScale = Vector3.one * size;

            if (chargeGlowMaterial != null)
                ApplyColor(chargeGlowMaterial, ScaleAlpha(chargeColor, chargeProgress));
        }

        if (warningConeMaterial != null)
        {
            float coneAlpha = warningConePeakAlpha * Mathf.SmoothStep(0f, 1f, chargeProgress);
            ApplyColor(warningConeMaterial, ScaleAlpha(warningConeColor, coneAlpha / Mathf.Max(0.0001f, warningConeColor.a)));
        }
    }

    private void BuildChargeGlow()
    {
        GameObject glow = new GameObject("MouthChargeGlow");
        glow.transform.SetParent(transform, false);
        glow.transform.localPosition = Vector3.zero;

        MeshFilter filter = glow.AddComponent<MeshFilter>();
        filter.sharedMesh = CreateQuadMesh();

        MeshRenderer renderer = glow.AddComponent<MeshRenderer>();
        chargeGlowMaterial = CreateUnlitMaterial(chargeColor, true);
        renderer.sharedMaterial = chargeGlowMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        glow.transform.localScale = Vector3.zero;
        chargeGlow = glow.transform;
        Register(glow);
    }

    private void BuildChargeParticles()
    {
        GameObject particleObject = new GameObject("MouthChargeParticles");
        particleObject.transform.SetParent(transform, false);
        particleObject.transform.localPosition = Vector3.zero;

        ParticleSystem system = particleObject.AddComponent<ParticleSystem>();
        system.Stop();

        ParticleSystem.MainModule main = system.main;
        main.duration = chargeDuration;
        main.loop = true;
        main.startLifetime = Mathf.Max(0.1f, chargeParticleRadius / 2.5f);
        main.startSpeed = -2.5f; // Negative pulls particles inward along the sphere normal.
        main.startSize = chargeParticleSize;
        main.startColor = chargeColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = Mathf.Max(1, chargeParticleCount);

        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = chargeParticleCount / Mathf.Max(0.1f, chargeDuration);

        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = chargeParticleRadius;
        shape.radiusThickness = 0f; // Emit from the shell so wisps converge on the mouth.

        ParticleSystem.SizeOverLifetimeModule sizeOverLife = system.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f));

        ConfigureParticleRenderer(system, chargeColor);
        system.Play();
        Register(particleObject);
    }

    private void BuildWarningCone()
    {
        GameObject coneObject = new GameObject("WarningCone");
        coneObject.transform.SetParent(transform, false);
        coneObject.transform.localPosition = Vector3.zero;
        coneObject.transform.localRotation = Quaternion.identity;

        MeshFilter filter = coneObject.AddComponent<MeshFilter>();
        filter.sharedMesh = CreateConeMesh(travelRange, coneAngle, Mathf.Max(6, warningConeSegments));

        warningConeRenderer = coneObject.AddComponent<MeshRenderer>();
        warningConeMaterial = CreateUnlitMaterial(ScaleAlpha(warningConeColor, 0f), false);
        warningConeRenderer.sharedMaterial = warningConeMaterial;
        warningConeRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        warningConeRenderer.receiveShadows = false;

        Register(coneObject);
    }

    private void SpawnScreamRings()
    {
        Vector3 origin = transform.position;
        PiercingScreamWaveVFX rings = null;

        if (ringLayerPrefab != null)
            rings = Instantiate(ringLayerPrefab, origin, Quaternion.LookRotation(travelDirection));
        else if (createRingLayerIfPrefabMissing)
            rings = new GameObject("ScreamWaveRings").AddComponent<PiercingScreamWaveVFX>();

        if (rings == null)
            return;

        rings.range = travelRange;
        rings.coneAngle = coneAngle;
        rings.Play(origin, travelDirection);
    }

    private void SpawnSpectralWisps()
    {
        GameObject wispObject = new GameObject("SpectralWisps");
        wispObject.transform.position = transform.position;
        wispObject.transform.rotation = Quaternion.LookRotation(travelDirection, Vector3.up);

        ParticleSystem system = wispObject.AddComponent<ParticleSystem>();
        system.Stop();

        ParticleSystem.MainModule main = system.main;
        main.duration = 0.4f;
        main.loop = false;
        main.startLifetime = wispLifetime;
        main.startSpeed = wispSpeed;
        main.startSize = wispSize;
        main.startColor = wispColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = Mathf.Max(1, wispCount);
        main.gravityModifier = -0.05f; // Gentle upward drift for a spectral feel.

        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)wispCount) });

        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = coneAngle * 0.4f;
        shape.radius = 0.15f;

        ParticleSystem.NoiseModule noise = system.noise;
        noise.enabled = true;
        noise.strength = 0.35f;
        noise.frequency = 1.4f;
        noise.scrollSpeed = 1.2f;

        ParticleSystem.ColorOverLifetimeModule colorOverLife = system.colorOverLifetime;
        colorOverLife.enabled = true;
        colorOverLife.color = CreateFadeGradient(wispColor);

        ConfigureParticleRenderer(system, wispColor);
        system.Play();

        Destroy(wispObject, wispLifetime + 0.4f);
    }

    private void SpawnHitPulse(Vector3 impactPoint)
    {
        GameObject pulseObject = new GameObject("ScreamHitPulse");
        pulseObject.transform.position = impactPoint;

        LineRenderer ring = pulseObject.AddComponent<LineRenderer>();
        ring.useWorldSpace = true;
        ring.loop = true;
        ring.alignment = LineAlignment.View;
        ring.numCornerVertices = 2;
        ring.positionCount = Mathf.Max(8, hitPulseSegments);
        ring.material = CreateUnlitMaterial(hitPulseColor, true);
        ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        HitPulseAnimator animator = pulseObject.AddComponent<HitPulseAnimator>();
        animator.Initialize(ring, hitPulseColor, hitPulseMaxRadius, hitPulseDuration);

        Destroy(pulseObject, hitPulseDuration + 0.05f);
    }

    private void FadeOutCharge()
    {
        if (chargeGlow != null)
            Destroy(chargeGlow.gameObject, 0.15f);

        if (warningConeRenderer != null)
            Destroy(warningConeRenderer.gameObject, 0.15f);
    }

    private void FaceCamera(Transform target)
    {
        if (target == null || billboardCamera == null)
            return;

        target.rotation = Quaternion.LookRotation(
            target.position - billboardCamera.transform.position,
            billboardCamera.transform.up);
    }

    private void ConfigureParticleRenderer(ParticleSystem system, Color color)
    {
        ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = CreateUnlitMaterial(color, true);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private Gradient CreateFadeGradient(Color color)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new[] { new GradientAlphaKey(color.a, 0f), new GradientAlphaKey(0f, 1f) });
        return gradient;
    }

    private Mesh CreateQuadMesh()
    {
        Mesh mesh = new Mesh { name = "ScreamGlowQuad" };
        mesh.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3(0.5f, -0.5f, 0f),
            new Vector3(0.5f, 0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f)
        };
        mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
        mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        mesh.RecalculateBounds();
        return mesh;
    }

    private Mesh CreateConeMesh(float length, float angleDegrees, int segments)
    {
        float baseRadius = Mathf.Tan(angleDegrees * 0.5f * Mathf.Deg2Rad) * length;

        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 6];

        vertices[0] = Vector3.zero; // Apex at the mouth (local space, +Z is forward).
        Vector3 baseCenter = Vector3.forward * length;

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments * Mathf.PI * 2f;
            vertices[i + 1] = baseCenter + new Vector3(Mathf.Cos(t) * baseRadius, Mathf.Sin(t) * baseRadius, 0f);
        }

        int tri = 0;
        for (int i = 1; i <= segments; i++)
        {
            int next = i + 1;
            // Two-sided so the cone reads from any camera angle.
            triangles[tri++] = 0;
            triangles[tri++] = i;
            triangles[tri++] = next;

            triangles[tri++] = 0;
            triangles[tri++] = next;
            triangles[tri++] = i;
        }

        Mesh mesh = new Mesh { name = "ScreamWarningCone" };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        return mesh;
    }

    private Material CreateUnlitMaterial(Color color, bool additive)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        Material material = new Material(shader);
        ApplyColor(material, color);

        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 0f);

        UnityEngine.Rendering.BlendMode destination = additive
            ? UnityEngine.Rendering.BlendMode.One
            : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;

        if (material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend"))
            material.SetFloat("_DstBlend", (float)destination);

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        ownedMaterials.Add(material);
        return material;
    }

    private void ApplyColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
    }

    private Color ScaleAlpha(Color color, float alphaScale)
    {
        color.a *= Mathf.Clamp01(alphaScale);
        return color;
    }

    private Vector3 Normalize(Vector3 value, Vector3 fallback)
    {
        return value.sqrMagnitude > 0.0001f ? value.normalized : fallback;
    }

    private void Register(GameObject owned)
    {
        ownedObjects.Add(owned);
    }

    private void OnDestroy()
    {
        for (int i = 0; i < ownedMaterials.Count; i++)
        {
            if (ownedMaterials[i] != null)
                Destroy(ownedMaterials[i]);
        }

        ownedMaterials.Clear();
        ownedObjects.Clear();
    }

    /// <summary>
    /// Animates a single expanding, fading ring for the readable hit pulse.
    /// </summary>
    private class HitPulseAnimator : MonoBehaviour
    {
        private LineRenderer ring;
        private Material material;
        private Color color;
        private float maxRadius;
        private float duration;
        private float startTime;

        public void Initialize(LineRenderer targetRing, Color pulseColor, float radius, float pulseDuration)
        {
            ring = targetRing;
            material = targetRing.material;
            color = pulseColor;
            maxRadius = Mathf.Max(0.1f, radius);
            duration = Mathf.Max(0.05f, pulseDuration);
            startTime = Time.time;
        }

        private void Update()
        {
            if (ring == null)
                return;

            float progress = Mathf.Clamp01((Time.time - startTime) / duration);
            float radius = maxRadius * Mathf.SmoothStep(0f, 1f, progress);
            float alpha = 1f - progress;

            ring.widthMultiplier = Mathf.Lerp(0.14f, 0.03f, progress);

            Color faded = color;
            faded.a *= alpha;
            ring.startColor = faded;
            ring.endColor = faded;

            if (material != null)
            {
                if (material.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", faded);
                if (material.HasProperty("_Color"))
                    material.SetColor("_Color", faded);
            }

            int count = ring.positionCount;
            Vector3 center = transform.position;
            Vector3 forward = Vector3.up; // Face the ring upward so it reads on the ground plane.
            Vector3 right = Vector3.right;
            Vector3 planeUp = Vector3.forward;

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count * Mathf.PI * 2f;
                Vector3 point = center + (right * Mathf.Cos(t) + planeUp * Mathf.Sin(t)) * radius;
                ring.SetPosition(i, point);
            }
        }
    }
}
