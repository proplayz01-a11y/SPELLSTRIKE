using UnityEngine;

/// <summary>
/// Editable, visual-only presentation for Riptide Eruption. Gameplay damage,
/// targeting, and radius remain owned by TideWraithController.
/// </summary>
public class RiptideEruptionVisual : MonoBehaviour
{
    public enum VisualPhase
    {
        Warning,
        Impact
    }

    [Header("Presentation")]
    [SerializeField] private VisualPhase phase = VisualPhase.Warning;
    [Tooltip("Visual-only size in world units. This does not change Riptide's gameplay radius.")]
    [SerializeField, Min(0.1f)] private float visualRadius = 7f;
    [SerializeField] private Color primaryColor = new Color(0.05f, 0.9f, 1f, 0.88f);
    [SerializeField] private Color secondaryColor = new Color(0.02f, 0.25f, 0.65f, 0.7f);
    [SerializeField] private Color foamColor = new Color(0.65f, 0.95f, 1f, 0.9f);

    [Header("Warning")]
    [SerializeField, Min(3)] private int ringSegments = 64;
    [SerializeField, Min(0.01f)] private float ringWidth = 0.22f;
    [SerializeField, Min(0f)] private float pulseAmount = 0.12f;
    [SerializeField, Min(0.01f)] private float pulseSpeed = 3.5f;

    [Header("Impact")]
    [SerializeField, Min(1)] private int burstCount = 42;
    [SerializeField, Min(0.1f)] private float eruptionHeight = 5f;
    [SerializeField, Min(0.05f)] private float lifetime = 1.25f;

    private Transform pulseRoot;
    private Vector3 basePulseScale = Vector3.one;

    public void Configure(VisualPhase visualPhase)
    {
        phase = visualPhase;
    }

    private void Awake()
    {
        if (phase == VisualPhase.Warning)
            BuildWarning();
        else
            BuildImpact();
    }

    private void Update()
    {
        if (phase != VisualPhase.Warning || pulseRoot == null)
            return;

        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        pulseRoot.localScale = basePulseScale * pulse;
    }

    private void BuildWarning()
    {
        GameObject root = new GameObject("RiptideWarningRing");
        root.transform.SetParent(transform, false);
        pulseRoot = root.transform;
        basePulseScale = Vector3.one;

        CreateRing(root.transform, visualRadius, ringWidth, primaryColor, 0.08f);
        CreateRing(root.transform, visualRadius * 0.68f, ringWidth * 0.55f, secondaryColor, 0.1f);
    }

    private void BuildImpact()
    {
        CreateRing(transform, visualRadius, ringWidth * 1.6f, primaryColor, 0.1f);
        CreateRing(transform, visualRadius * 0.58f, ringWidth, foamColor, 0.12f);

        GameObject burstObject = new GameObject("RiptideWaterBurst");
        burstObject.transform.SetParent(transform, false);

        ParticleSystem particles = burstObject.AddComponent<ParticleSystem>();
        // Unity starts a newly added ParticleSystem immediately. Stop and clear
        // it before changing duration or other settings that are immutable
        // while the system is playing.
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = particles.main;
        main.loop = false;
        main.duration = Mathf.Max(0.1f, lifetime * 0.65f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.45f, lifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(eruptionHeight * 0.55f, eruptionHeight);
        main.startSize = new ParticleSystem.MinMaxCurve(visualRadius * 0.06f, visualRadius * 0.18f);
        main.startColor = new ParticleSystem.MinMaxGradient(primaryColor, foamColor);
        main.gravityModifier = 0.45f;
        main.maxParticles = Mathf.Max(burstCount, 8);

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Clamp(burstCount, 1, short.MaxValue)) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = visualRadius * 0.72f;
        shape.radiusThickness = 0.8f;

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        // All XYZ velocity curves must use the same MinMaxCurve mode. Explicit
        // two-constant zero curves keep them compatible with the Y curve.
        velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.y = new ParticleSystem.MinMaxCurve(eruptionHeight * 0.55f, eruptionHeight);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.radial = new ParticleSystem.MinMaxCurve(0.4f, 1.35f);

        ParticleSystemRenderer renderer = burstObject.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = CreateMaterial(primaryColor);

        particles.Play();
        Destroy(gameObject, lifetime + 0.35f);
    }

    private void CreateRing(Transform parent, float radius, float width, Color color, float height)
    {
        GameObject ringObject = new GameObject("Ring");
        ringObject.transform.SetParent(parent, false);
        ringObject.transform.localPosition = Vector3.up * height;

        LineRenderer ring = ringObject.AddComponent<LineRenderer>();
        ring.loop = true;
        ring.useWorldSpace = false;
        ring.positionCount = Mathf.Max(3, ringSegments);
        ring.startWidth = width;
        ring.endWidth = width;
        ring.numCornerVertices = 3;
        ring.numCapVertices = 3;
        ring.material = CreateMaterial(color);
        ring.startColor = color;
        ring.endColor = color;

        for (int i = 0; i < ring.positionCount; i++)
        {
            float angle = i * Mathf.PI * 2f / ring.positionCount;
            ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
        }
    }

    private static Material CreateMaterial(Color color)
    {
        return Stage2RuntimeVFXMaterialUtility.CreateMaterial(color, false);
    }
}
