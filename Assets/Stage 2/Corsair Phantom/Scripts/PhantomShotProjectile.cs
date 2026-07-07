using UnityEngine;

public class PhantomShotProjectile : MonoBehaviour
{
    [Header("Flight")]
    public float speed = 12f;
    public float lifetime = 3f;
    public float hitRadius = 0.45f;

    [Header("Damage")]
    public float damage = 6f;
    public string playerTag = "Player";

    [Header("Temporary Visual")]
    public bool createFallbackVisualIfMissing = true;
    public bool createFallbackTrail = true;
    public float fallbackVisualScale = 0.7f;
    public float pulseAmount = 0.12f;
    public float pulseSpeed = 8f;
    public Vector3 spinSpeed = new Vector3(0f, 120f, 80f);
    public Color coreColor = new Color(0.28f, 0.08f, 1f, 0.72f);
    public Color trailColor = new Color(0.42f, 0.12f, 1f, 0.45f);
    public Color emissionColor = new Color(0.38f, 0.12f, 1f, 1f);
    public float emissionIntensity = 3f;

    [Header("Debug")]
    public bool debugLogs = true;

    private Transform owner;
    private Transform target;
    private Collider targetCollider;
    private Vector3 direction;
    private Vector3 baseVisualScale;
    private Transform visualRoot;
    private float spawnTime;
    private bool launched;
    private bool hasHit;

    public void Launch(
        Vector3 spawnPosition,
        Vector3 targetPosition,
        Transform ownerTransform,
        Transform targetTransform,
        float damageAmount,
        float projectileSpeed,
        float projectileLifetime,
        float projectileHitRadius)
    {
        owner = ownerTransform;
        target = targetTransform;
        damage = damageAmount;
        speed = Mathf.Max(0.1f, projectileSpeed);
        lifetime = Mathf.Max(0.1f, projectileLifetime);
        hitRadius = Mathf.Max(0.05f, projectileHitRadius);
        spawnTime = Time.time;
        launched = true;
        hasHit = false;

        transform.position = spawnPosition;

        direction = targetPosition - spawnPosition;
        if (direction.sqrMagnitude <= 0.001f)
            direction = owner != null ? owner.forward : transform.forward;

        direction.Normalize();
        transform.rotation = Quaternion.LookRotation(direction);

        ResolveTargetCollider();
        EnsureTriggerCollider();
        EnsureFallbackVisual();
        EnsureFallbackTrail();

        Log("[PhantomShot] Projectile launched.");
    }

    private void Update()
    {
        if (!launched || hasHit)
            return;

        if (Time.time - spawnTime >= lifetime)
        {
            Log("[PhantomShot] Projectile expired.");
            Destroy(gameObject);
            return;
        }

        Vector3 startPosition = transform.position;
        Vector3 endPosition = startPosition + direction * speed * Time.deltaTime;

        if (TryHitPlayerBetween(startPosition, endPosition))
            return;

        transform.position = endPosition;
        AnimateFallbackVisual();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!launched || hasHit)
            return;

        if (IsPlayerCollider(other))
            HitPlayer(other);
    }

    private bool TryHitPlayerBetween(Vector3 startPosition, Vector3 endPosition)
    {
        Vector3 castDirection = endPosition - startPosition;
        float castDistance = castDirection.magnitude;

        if (castDistance > 0.001f)
        {
            RaycastHit[] hits = Physics.SphereCastAll(
                startPosition,
                hitRadius,
                castDirection.normalized,
                castDistance,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide
            );

            for (int i = 0; i < hits.Length; i++)
            {
                Collider hitCollider = hits[i].collider;
                if (hitCollider != null && IsPlayerCollider(hitCollider))
                {
                    transform.position = hits[i].point;
                    HitPlayer(hitCollider);
                    return true;
                }
            }
        }

        Collider[] overlaps = Physics.OverlapSphere(
            endPosition,
            hitRadius,
            Physics.AllLayers,
            QueryTriggerInteraction.Collide
        );

        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider overlap = overlaps[i];
            if (overlap != null && IsPlayerCollider(overlap))
            {
                transform.position = endPosition;
                HitPlayer(overlap);
                return true;
            }
        }

        return false;
    }

    private bool IsPlayerCollider(Collider candidate)
    {
        if (candidate == null)
            return false;

        if (owner != null && candidate.transform.IsChildOf(owner))
            return false;

        if (target != null && (candidate.transform == target || candidate.transform.IsChildOf(target)))
            return true;

        if (candidate.CompareTag(playerTag))
            return true;

        Transform candidateRoot = candidate.transform.root;
        if (candidateRoot != null && candidateRoot.CompareTag(playerTag))
            return true;

        return candidate.GetComponentInParent<PlayerHealth>() != null;
    }

    private void HitPlayer(Collider hitCollider)
    {
        if (hasHit)
            return;

        hasHit = true;

        IDamageable damageable = hitCollider.GetComponentInParent<IDamageable>();
        if (damageable == null && target != null)
            damageable = target.GetComponentInChildren<IDamageable>();
        if (damageable == null && target != null)
            damageable = target.GetComponentInParent<IDamageable>();

        if (damageable != null)
            damageable.ApplyDamage(damage);
        else if (target != null)
            target.SendMessage("ApplyDamage", damage, SendMessageOptions.DontRequireReceiver);
        else
            hitCollider.SendMessageUpwards("ApplyDamage", damage, SendMessageOptions.DontRequireReceiver);

        Log("[PhantomShot] Projectile hit player.");
        Destroy(gameObject);
    }

    private void ResolveTargetCollider()
    {
        targetCollider = null;

        if (target == null)
            return;

        PlayerHealth playerHealth = target.GetComponentInChildren<PlayerHealth>();
        if (playerHealth != null)
            targetCollider = playerHealth.GetComponentInChildren<Collider>();

        if (targetCollider == null)
            targetCollider = target.GetComponentInChildren<Collider>();
    }

    private void EnsureTriggerCollider()
    {
        SphereCollider sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider == null)
            sphereCollider = gameObject.AddComponent<SphereCollider>();

        sphereCollider.isTrigger = true;
        sphereCollider.radius = hitRadius;

        Rigidbody body = GetComponent<Rigidbody>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody>();

        body.isKinematic = true;
        body.useGravity = false;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    private void EnsureFallbackVisual()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0 || !createFallbackVisualIfMissing)
        {
            visualRoot = renderers.Length > 0 ? renderers[0].transform : null;
            if (visualRoot != null)
                baseVisualScale = visualRoot.localScale;
            return;
        }

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "GhostBlobVisual";
        visual.transform.SetParent(transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one * fallbackVisualScale;

        Collider visualCollider = visual.GetComponent<Collider>();
        if (visualCollider != null)
            Destroy(visualCollider);

        Renderer visualRenderer = visual.GetComponent<Renderer>();
        if (visualRenderer != null)
            visualRenderer.material = CreateGhostMaterial(coreColor, emissionColor, emissionIntensity);

        visualRoot = visual.transform;
        baseVisualScale = visualRoot.localScale;
    }

    private void EnsureFallbackTrail()
    {
        if (!createFallbackTrail)
            return;

        TrailRenderer trail = GetComponent<TrailRenderer>();
        if (trail == null)
            trail = gameObject.AddComponent<TrailRenderer>();

        trail.time = 0.28f;
        trail.startWidth = Mathf.Max(0.05f, fallbackVisualScale * 0.8f);
        trail.endWidth = 0.03f;
        trail.minVertexDistance = 0.05f;
        trail.autodestruct = false;
        trail.material = CreateGhostMaterial(trailColor, emissionColor, emissionIntensity);
        trail.startColor = trailColor;
        trail.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
    }

    private void AnimateFallbackVisual()
    {
        if (visualRoot == null)
            return;

        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        visualRoot.localScale = baseVisualScale * pulse;
        visualRoot.Rotate(spinSpeed * Time.deltaTime, Space.Self);
    }

    private Material CreateGhostMaterial(Color color, Color glowColor, float glowIntensity)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader);

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        if (material.HasProperty("_EmissionColor"))
            material.SetColor("_EmissionColor", glowColor * Mathf.Max(0f, glowIntensity));

        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend"))
            material.SetFloat("_Blend", 0f);
        if (material.HasProperty("_AlphaClip"))
            material.SetFloat("_AlphaClip", 0f);

        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.EnableKeyword("_EMISSION");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        return material;
    }

    private void Log(string message)
    {
        if (debugLogs)
            Debug.Log(message);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.35f, 1f, 1f, 0.75f);
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
}
