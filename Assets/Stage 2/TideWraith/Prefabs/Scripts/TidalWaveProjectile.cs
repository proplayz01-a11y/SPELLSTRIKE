using UnityEngine;

/// <summary>
/// Wide, straight-moving Tide Wraith wave. This is the Tide Wraith's only
/// knockback/stun source; Riptide Eruption intentionally deals damage only.
/// </summary>
public class TidalWaveProjectile : MonoBehaviour
{
    [Header("Collision")]
    [SerializeField] private string playerTag = "Player";

    [Header("Procedural Visual")]
    [SerializeField] private bool buildFallbackVisual = true;
    [Tooltip("Visual-only width. This does not change the gameplay hit radius.")]
    [SerializeField, Min(0.1f)] private float visualWidth = 9f;
    [SerializeField, Min(0.1f)] private float visualHeight = 3.5f;
    [SerializeField, Min(0.05f)] private float visualDepth = 0.8f;
    [SerializeField] private Color fallbackColor = new Color(0.05f, 0.85f, 1f, 0.82f);
    [SerializeField] private Color deepWaterColor = new Color(0.01f, 0.18f, 0.55f, 0.72f);
    [SerializeField] private Color foamColor = new Color(0.72f, 0.98f, 1f, 0.92f);

    [Header("Trail")]
    [SerializeField, Min(0.01f)] private float trailDuration = 0.38f;
    [SerializeField, Min(0.01f)] private float trailWidthMultiplier = 0.8f;

    [Header("Impact")]
    [SerializeField, Min(1)] private int impactBurstCount = 36;
    [SerializeField, Min(0.1f)] private float impactScale = 1f;
    [SerializeField, Min(0f)] private float fallbackImpactLinger = 0.15f;

    private Transform owner;
    private Transform target;
    private Vector3 direction;
    private float damage;
    private float speed;
    private float lifetime;
    private float hitRadius;
    private float knockbackForce;
    private float stunDuration;
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
        float projectileHitRadius,
        float knockbackAmount,
        float stunTime)
    {
        transform.position = spawnPosition;
        owner = ownerTransform;
        target = targetTransform;
        direction = launchDirection.sqrMagnitude > 0.001f ? launchDirection.normalized : transform.forward;
        direction.y = 0f;
        direction.Normalize();
        damage = Mathf.Max(0f, damageAmount);
        speed = Mathf.Max(0.1f, projectileSpeed);
        lifetime = Mathf.Max(0.1f, projectileLifetime);
        hitRadius = Mathf.Max(0.1f, projectileHitRadius);
        knockbackForce = Mathf.Max(0f, knockbackAmount);
        stunDuration = Mathf.Max(0f, stunTime);
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
        TideWraithCrowdControl.Apply(target, direction, knockbackForce, stunDuration);

        Collider trigger = GetComponent<Collider>();
        if (trigger != null)
            trigger.enabled = false;

        Destroy(gameObject, Mathf.Max(0f, fallbackImpactLinger));
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

        CreateWaveLayer("TidalWaveDeepWater", new Vector3(visualWidth, visualHeight * 0.72f, visualDepth),
            Vector3.up * visualHeight * 0.34f, deepWaterColor);
        CreateWaveLayer("TidalWaveBody", new Vector3(visualWidth * 0.92f, visualHeight, visualDepth * 0.72f),
            Vector3.up * visualHeight * 0.5f, fallbackColor);
        CreateWaveLayer("TidalWaveFoam", new Vector3(visualWidth, visualHeight * 0.18f, visualDepth * 1.05f),
            Vector3.up * visualHeight, foamColor);

        TrailRenderer trail = gameObject.AddComponent<TrailRenderer>();
        trail.name = "TidalWaveFallbackTrail";
        trail.time = trailDuration;
        trail.minVertexDistance = 0.1f;
        trail.startWidth = Mathf.Max(0.5f, visualWidth * trailWidthMultiplier);
        trail.endWidth = Mathf.Max(0.15f, visualWidth * 0.2f);
        trail.numCornerVertices = 3;
        trail.numCapVertices = 3;
        trail.startColor = fallbackColor;
        trail.endColor = new Color(fallbackColor.r, fallbackColor.g, fallbackColor.b, 0f);
        trail.material = CreateMaterial(fallbackColor);
    }

    private void CreateWaveLayer(string objectName, Vector3 scale, Vector3 localPosition, Color color)
    {
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = objectName;
        visual.transform.SetParent(transform, false);
        visual.transform.localPosition = localPosition;
        visual.transform.localScale = scale;
        Destroy(visual.GetComponent<Collider>());

        Renderer renderer = visual.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material = CreateMaterial(color);
    }

    private void SpawnFallbackImpact()
    {
        if (!buildFallbackVisual)
            return;

        GameObject impact = new GameObject("TidalWaveImpact_Runtime");
        impact.transform.position = transform.position + Vector3.up * visualHeight * 0.5f;

        ParticleSystem particles = impact.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = particles.main;
        main.loop = false;
        main.duration = 0.3f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.65f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f * impactScale, 0.45f * impactScale);
        main.startColor = new ParticleSystem.MinMaxGradient(fallbackColor, foamColor);
        main.gravityModifier = 0.35f;
        main.maxParticles = Mathf.Max(impactBurstCount + 8, 48);
        main.stopAction = ParticleSystemStopAction.Destroy;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Clamp(impactBurstCount, 1, short.MaxValue)) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(visualWidth * 0.75f, visualHeight * 0.3f, visualDepth);

        ParticleSystemRenderer particleRenderer = impact.GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.material = CreateMaterial(foamColor);
        particles.Play();
    }

    private static Material CreateMaterial(Color color)
    {
        Material material = Stage2RuntimeVFXMaterialUtility.CreateMaterial(color, false);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", color * 2f);
            material.EnableKeyword("_EMISSION");
        }
        return material;
    }
}
