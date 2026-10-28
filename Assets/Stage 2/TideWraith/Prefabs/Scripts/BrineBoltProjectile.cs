using UnityEngine;

/// <summary>
/// Straight-line Tide Wraith projectile. The launch direction is captured once,
/// so the player can dodge after the release frame.
/// </summary>
public class BrineBoltProjectile : MonoBehaviour
{
    [Header("Collision")]
    [SerializeField] private string playerTag = "Player";

    [Header("Procedural Visual")]
    [SerializeField] private bool buildFallbackVisual = true;
    [Tooltip("Visual-only multiplier. This does not enlarge the gameplay hit radius.")]
    [SerializeField, Min(0.1f)] private float visualScale = 3f;
    [SerializeField, Min(0.1f)] private float visualLength = 1.65f;
    [SerializeField] private Color fallbackColor = new Color(0.05f, 0.85f, 1f, 0.85f);
    [SerializeField] private Color fallbackOuterColor = new Color(0.02f, 0.22f, 0.55f, 0.55f);
    [SerializeField] private Color fallbackTrailColor = new Color(0.1f, 0.9f, 1f, 0.8f);

    [Header("Trail")]
    [SerializeField, Min(0.01f)] private float trailDuration = 0.22f;
    [SerializeField, Min(0.01f)] private float trailWidthMultiplier = 0.55f;

    [Header("Droplets")]
    [SerializeField, Min(0f)] private float dropletEmissionRate = 18f;
    [SerializeField, Min(1)] private int maximumDroplets = 28;

    [Header("Impact")]
    [SerializeField, Min(1)] private int impactBurstCount = 18;
    [SerializeField, Min(0.1f)] private float impactScale = 1f;

    private Transform owner;
    private Transform target;
    private Vector3 direction;
    private float damage;
    private float speed;
    private float lifetime;
    private float hitRadius;
    private float spawnTime;
    private bool launched;
    private bool resolved;

    public void Launch(
        Vector3 spawnPosition,
        Vector3 launchDirection,
        Transform ownerTransform,
        Transform targetTransform,
        float damageAmount,
        float projectileSpeed,
        float projectileLifetime,
        float projectileHitRadius)
    {
        transform.position = spawnPosition;
        owner = ownerTransform;
        target = targetTransform;
        direction = launchDirection.sqrMagnitude > 0.001f ? launchDirection.normalized : transform.forward;
        damage = Mathf.Max(0f, damageAmount);
        speed = Mathf.Max(0.1f, projectileSpeed);
        lifetime = Mathf.Max(0.1f, projectileLifetime);
        hitRadius = Mathf.Max(0.05f, projectileHitRadius);
        spawnTime = Time.time;
        launched = true;
        resolved = false;
        transform.rotation = Quaternion.LookRotation(direction);

        EnsureTriggerCollider();
        EnsureFallbackVisual();
    }

    private void Update()
    {
        if (!launched || resolved)
            return;

        if (Time.time - spawnTime >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 start = transform.position;
        Vector3 end = start + direction * speed * Time.deltaTime;
        transform.position = end;

        if (TargetIntersectsSegment(start, end))
            HitPlayer();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!launched || resolved || IsOwnerCollider(other))
            return;

        if (IsPlayerCollider(other))
            HitPlayer();
    }

    private bool TargetIntersectsSegment(Vector3 start, Vector3 end)
    {
        if (target == null)
            return false;

        Collider targetCollider = target.GetComponentInChildren<Collider>();
        Vector3 point = targetCollider != null ? targetCollider.bounds.center : target.position + Vector3.up;
        Vector3 segment = end - start;
        float lengthSquared = segment.sqrMagnitude;
        float t = lengthSquared > 0.0001f ? Mathf.Clamp01(Vector3.Dot(point - start, segment) / lengthSquared) : 0f;
        return Vector3.Distance(point, start + segment * t) <= hitRadius;
    }

    private void HitPlayer()
    {
        if (resolved)
            return;

        resolved = true;
        SpawnFallbackImpact();
        ApplyDamage(target, damage);
        Destroy(gameObject);
    }

    private bool IsPlayerCollider(Collider other)
    {
        if (target != null && (other.transform == target || other.transform.IsChildOf(target) || target.IsChildOf(other.transform)))
            return true;
        return other.CompareTag(playerTag) || other.transform.root.CompareTag(playerTag);
    }

    private bool IsOwnerCollider(Collider other)
    {
        return owner != null && (other.transform == owner || other.transform.IsChildOf(owner));
    }

    private static void ApplyDamage(Transform targetTransform, float amount)
    {
        if (targetTransform == null || amount <= 0f)
            return;

        IDamageable damageable = targetTransform.GetComponentInChildren<IDamageable>();
        if (damageable == null)
            damageable = targetTransform.GetComponentInParent<IDamageable>();

        if (damageable != null)
            damageable.ApplyDamage(amount);
        else
            targetTransform.SendMessage("ApplyDamage", amount, SendMessageOptions.DontRequireReceiver);
    }

    private void EnsureTriggerCollider()
    {
        SphereCollider sphere = GetComponent<SphereCollider>();
        if (sphere == null)
            sphere = gameObject.AddComponent<SphereCollider>();
        sphere.isTrigger = true;
        sphere.radius = hitRadius;

        Rigidbody body = GetComponent<Rigidbody>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
    }

    private void EnsureFallbackVisual()
    {
        if (!buildFallbackVisual || GetComponentInChildren<Renderer>() != null)
            return;

        float visualSize = Mathf.Max(0.15f, hitRadius * 1.5f) * visualScale;

        GameObject outer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        outer.name = "BrineBoltOuterShell";
        outer.transform.SetParent(transform, false);
        outer.transform.localScale = new Vector3(visualSize * 0.9f, visualSize * 0.9f, visualSize * visualLength);
        Destroy(outer.GetComponent<Collider>());

        Renderer outerRenderer = outer.GetComponent<Renderer>();
        if (outerRenderer != null)
            outerRenderer.material = CreateMaterial(fallbackOuterColor);

        GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.name = "BrineBoltFallbackVisual";
        core.transform.SetParent(transform, false);
        core.transform.localScale = new Vector3(visualSize * 0.48f, visualSize * 0.48f, visualSize * visualLength * 0.76f);
        Destroy(core.GetComponent<Collider>());

        Renderer renderer = core.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material = CreateMaterial(fallbackColor);

        CreateFallbackTrail(visualSize);
        CreateFallbackDroplets(visualSize);
    }

    private void CreateFallbackTrail(float visualSize)
    {
        TrailRenderer trail = gameObject.AddComponent<TrailRenderer>();
        trail.time = trailDuration;
        trail.minVertexDistance = 0.05f;
        trail.startWidth = visualSize * trailWidthMultiplier;
        trail.endWidth = 0f;
        trail.numCornerVertices = 3;
        trail.numCapVertices = 3;
        trail.alignment = LineAlignment.View;
        trail.material = CreateMaterial(fallbackTrailColor);

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(fallbackTrailColor, 0f),
                new GradientColorKey(new Color(0.02f, 0.35f, 0.75f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(fallbackTrailColor.a, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        trail.colorGradient = gradient;
    }

    private void CreateFallbackDroplets(float visualSize)
    {
        GameObject droplets = new GameObject("BrineBoltDroplets");
        droplets.transform.SetParent(transform, false);

        ParticleSystem particles = droplets.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.65f);
        main.startSize = new ParticleSystem.MinMaxCurve(visualSize * 0.07f, visualSize * 0.18f);
        main.startColor = new ParticleSystem.MinMaxGradient(fallbackColor, fallbackTrailColor);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = maximumDroplets;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = dropletEmissionRate;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = visualSize * 0.28f;

        ParticleSystemRenderer particleRenderer = droplets.GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.material = CreateMaterial(fallbackTrailColor);
    }

    private void SpawnFallbackImpact()
    {
        if (!buildFallbackVisual)
            return;

        GameObject impact = new GameObject("BrineBoltImpact_Runtime");
        impact.transform.position = transform.position;

        ParticleSystem particles = impact.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = false;
        main.duration = 0.25f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.42f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(
            0.05f * impactScale,
            Mathf.Max(0.12f, hitRadius * 0.32f) * impactScale);
        main.startColor = new ParticleSystem.MinMaxGradient(fallbackColor, fallbackTrailColor);
        main.maxParticles = Mathf.Max(impactBurstCount + 4, 24);
        main.stopAction = ParticleSystemStopAction.Destroy;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Clamp(impactBurstCount, 1, short.MaxValue)) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = Mathf.Max(0.08f, hitRadius * 0.25f) * impactScale;

        ParticleSystemRenderer particleRenderer = impact.GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.material = CreateMaterial(fallbackTrailColor);

        particles.Play();
    }

    private static Material CreateMaterial(Color color)
    {
        Material material = Stage2RuntimeVFXMaterialUtility.CreateMaterial(color, false);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", color * 2.5f);
            material.EnableKeyword("_EMISSION");
        }
        return material;
    }
}
