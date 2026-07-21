using UnityEngine;

/// <summary>
/// Short "gathering" charge that plays at the Phantom Shot origin during the
/// windup, before the projectile is released. Visual only; it never affects
/// gameplay. Layers: a growing emissive core plus inward-converging spectral
/// wisps. The component owns and cleans up everything it creates.
/// </summary>
public class PhantomShotChargeVFX : MonoBehaviour
{
    [Header("Charge Core")]
    public Color coreColor = new Color(0.5f, 1f, 0.85f, 1f);
    public float coreMaxSize = 0.4f;
    public float pulseSpeed = 10f;
    public float pulseAmount = 0.14f;

    [Header("Converging Wisps")]
    public Color wispColor = new Color(0.6f, 1f, 0.9f, 1f);
    public int wispCount = 12;
    public float wispRadius = 0.7f;
    public float wispSize = 0.07f;

    private float chargeDuration = 0.6f;
    private float startTime;
    private Transform core;
    private Material coreMaterial;
    private Camera billboardCamera;

    /// <summary>
    /// Starts the charge build-up parented to the given origin transform.
    /// </summary>
    /// <param name="origin">Muzzle/mouth transform the charge follows.</param>
    /// <param name="windupDuration">Seconds until the projectile releases.</param>
    public void BeginCharge(Transform origin, float windupDuration)
    {
        chargeDuration = Mathf.Max(0.01f, windupDuration);
        startTime = Time.time;
        billboardCamera = Camera.main;

        if (origin != null)
            transform.SetParent(origin, false);

        transform.localPosition = Vector3.zero;

        BuildCore();
        BuildConvergingWisps();

        Destroy(gameObject, chargeDuration + 0.25f);
    }

    /// <summary>
    /// Ends the charge early when the shot is interrupted before release.
    /// </summary>
    public void Cancel()
    {
        Destroy(gameObject, 0.1f);
    }

    private void Update()
    {
        float progress = Mathf.Clamp01((Time.time - startTime) / chargeDuration);

        if (billboardCamera == null)
            billboardCamera = Camera.main;

        if (core != null)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            core.localScale = Vector3.one * (coreMaxSize * Mathf.SmoothStep(0f, 1f, progress) * pulse);

            if (billboardCamera != null)
            {
                core.rotation = Quaternion.LookRotation(
                    core.position - billboardCamera.transform.position,
                    billboardCamera.transform.up);
            }

            if (coreMaterial != null)
            {
                Color color = coreColor;
                color.a *= progress;
                ApplyColor(coreMaterial, color);
            }
        }
    }

    private void BuildCore()
    {
        GameObject coreObject = new GameObject("ChargeCore");
        coreObject.transform.SetParent(transform, false);
        coreObject.transform.localPosition = Vector3.zero;
        coreObject.transform.localScale = Vector3.zero;

        MeshFilter filter = coreObject.AddComponent<MeshFilter>();
        filter.sharedMesh = SpectralVFXUtility.QuadMesh();

        MeshRenderer renderer = coreObject.AddComponent<MeshRenderer>();
        coreMaterial = SpectralVFXUtility.CreateMaterial(coreColor, true);
        renderer.sharedMaterial = coreMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        core = coreObject.transform;
    }

    private void BuildConvergingWisps()
    {
        GameObject wispObject = new GameObject("ChargeWisps");
        wispObject.transform.SetParent(transform, false);
        wispObject.transform.localPosition = Vector3.zero;

        ParticleSystem system = wispObject.AddComponent<ParticleSystem>();
        system.Stop();

        ParticleSystem.MainModule main = system.main;
        main.duration = chargeDuration;
        main.loop = true;
        main.startLifetime = Mathf.Max(0.1f, wispRadius / 2.5f);
        main.startSpeed = -2.5f; // Negative pulls wisps inward along the sphere normal.
        main.startSize = wispSize;
        main.startColor = wispColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = Mathf.Max(1, wispCount);

        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = wispCount / Mathf.Max(0.1f, chargeDuration);

        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = wispRadius;
        shape.radiusThickness = 0f;

        ParticleSystem.SizeOverLifetimeModule sizeOverLife = system.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f));

        SpectralVFXUtility.ConfigureParticleRenderer(system, wispColor);
        system.Play();
    }

    private void ApplyColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
    }
}
