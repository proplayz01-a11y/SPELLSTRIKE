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

    [Header("Spectral Visual")]
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

    [Header("Ghost Shell / Aura")]
    public Color shellColor = new Color(0.55f, 1f, 0.9f, 0.22f);
    public float shellScaleMultiplier = 2.1f;
    public float shellSpinSpeed = 40f;

    [Header("Misty Trail")]
    public Color mistColor = new Color(0.5f, 1f, 0.88f, 0.35f);
    public int mistMaxParticles = 24;
    public float mistRateOverDistance = 4f;
    public float mistLifetime = 0.55f;

    [Header("Backward Wisps")]
    public Color wispColor = new Color(0.65f, 1f, 0.92f, 0.7f);
    public int wispMaxParticles = 12;
    public float wispRate = 16f;
    public float wispLifetime = 0.4f;
    public float wispSize = 0.07f;

    [Header("Impact Burst")]
    public Color impactColor = new Color(0.6f, 1f, 0.9f, 1f);
    public int impactParticleCount = 14;
    public float impactDuration = 0.28f;
    public float impactShellRadius = 0.9f;

    [Header("Optional Subtle Light")]
    public bool enableProjectileLight = false;
    public Color projectileLightColor = new Color(0.5f, 1f, 0.85f, 1f);
    public float projectileLightIntensity = 1.2f;
    public float projectileLightRange = 3.5f;

    [Header("Debug")]
    public bool debugLogs = true;

    private const float CoreScaleFactor = 0.55f;

    private Transform owner;
    private Transform target;
    private Collider targetCollider;
    private Vector3 direction;
    private Vector3 baseCoreScale;
    private Transform coreVisual;
    private Transform shellVisual;
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
        BuildSpectralLayers();

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
        AnimateSpectralVisual();
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
        SpawnImpactBurst(transform.position);
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

    private void BuildSpectralLayers()
    {
        DisableAuthoredBallVisual();
        BuildSpectralCore();
        BuildGhostShell();
        BuildMistyTrail();
        BuildBackwardWisps();
        BuildOptionalLight();
    }

    // The authored prefab ships a plain glowing sphere + TrailRenderer. We replace
    // that look with layered spectral VFX, so hide the originals at runtime while
    // keeping the collider/gameplay intact.
    private void DisableAuthoredBallVisual()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
            meshRenderer.enabled = false;

        TrailRenderer trailRenderer = GetComponent<TrailRenderer>();
        if (trailRenderer != null)
            trailRenderer.enabled = false;
    }

    private void BuildSpectralCore()
    {
        GameObject coreObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        coreObject.name = "SpectralCore";
        coreObject.transform.SetParent(transform, false);
        coreObject.transform.localPosition = Vector3.zero;
        coreObject.transform.localScale = Vector3.one * (fallbackVisualScale * CoreScaleFactor);

        Collider coreCollider = coreObject.GetComponent<Collider>();
        if (coreCollider != null)
            Destroy(coreCollider);

        Renderer coreRenderer = coreObject.GetComponent<Renderer>();
        if (coreRenderer != null)
        {
            coreRenderer.sharedMaterial = CreateSpectralMaterial(coreColor, true);
            coreRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            coreRenderer.receiveShadows = false;
        }

        coreVisual = coreObject.transform;
        baseCoreScale = coreVisual.localScale;
    }

    private void BuildGhostShell()
    {
        GameObject shellObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shellObject.name = "GhostShell";
        shellObject.transform.SetParent(transform, false);
        shellObject.transform.localPosition = Vector3.zero;
        shellObject.transform.localScale = Vector3.one * (fallbackVisualScale * CoreScaleFactor * shellScaleMultiplier);

        Collider shellCollider = shellObject.GetComponent<Collider>();
        if (shellCollider != null)
            Destroy(shellCollider);

        Renderer shellRenderer = shellObject.GetComponent<Renderer>();
        if (shellRenderer != null)
        {
            shellRenderer.sharedMaterial = CreateSpectralMaterial(shellColor, true);
            shellRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            shellRenderer.receiveShadows = false;
        }

        shellVisual = shellObject.transform;
    }

    private void BuildMistyTrail()
    {
        if (!createFallbackTrail)
            return;

        GameObject mistObject = new GameObject("MistyTrail");
        mistObject.transform.SetParent(transform, false);
        mistObject.transform.localPosition = Vector3.zero;

        ParticleSystem system = mistObject.AddComponent<ParticleSystem>();
        system.Stop();

        ParticleSystem.MainModule main = system.main;
        main.startLifetime = mistLifetime;
        main.startSpeed = 0f;
        main.startSize = fallbackVisualScale * 0.6f;
        main.startColor = mistColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = Mathf.Max(1, mistMaxParticles);

        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = 0f;
        emission.rateOverDistance = mistRateOverDistance;

        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = fallbackVisualScale * 0.2f;

        ParticleSystem.SizeOverLifetimeModule sizeOverLife = system.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 1.4f));

        ParticleSystem.ColorOverLifetimeModule colorOverLife = system.colorOverLifetime;
        colorOverLife.enabled = true;
        colorOverLife.color = SpectralVFXUtility.FadeGradient(mistColor);

        SpectralVFXUtility.ConfigureParticleRenderer(system, mistColor);
        system.Play();
    }

    private void BuildBackwardWisps()
    {
        GameObject wispObject = new GameObject("BackwardWisps");
        wispObject.transform.SetParent(transform, false);
        wispObject.transform.localPosition = Vector3.zero;
        wispObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // Emit opposite the travel direction.

        ParticleSystem system = wispObject.AddComponent<ParticleSystem>();
        system.Stop();

        ParticleSystem.MainModule main = system.main;
        main.startLifetime = wispLifetime;
        main.startSpeed = 1.5f;
        main.startSize = wispSize;
        main.startColor = wispColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = Mathf.Max(1, wispMaxParticles);

        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = wispRate;

        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.radius = 0.1f;

        ParticleSystem.NoiseModule noise = system.noise;
        noise.enabled = true;
        noise.strength = 0.25f;
        noise.frequency = 1.2f;

        ParticleSystem.ColorOverLifetimeModule colorOverLife = system.colorOverLifetime;
        colorOverLife.enabled = true;
        colorOverLife.color = SpectralVFXUtility.FadeGradient(wispColor);

        SpectralVFXUtility.ConfigureParticleRenderer(system, wispColor);
        system.Play();
    }

    private void BuildOptionalLight()
    {
        if (!enableProjectileLight)
            return;

        GameObject lightObject = new GameObject("SpectralLight");
        lightObject.transform.SetParent(transform, false);
        lightObject.transform.localPosition = Vector3.zero;

        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = projectileLightColor;
        light.intensity = projectileLightIntensity;
        light.range = projectileLightRange;
        light.shadows = LightShadows.None;
    }

    private void AnimateSpectralVisual()
    {
        if (coreVisual != null)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            coreVisual.localScale = baseCoreScale * pulse;
            coreVisual.Rotate(spinSpeed * Time.deltaTime, Space.Self);
        }

        if (shellVisual != null)
            shellVisual.Rotate(Vector3.up * shellSpinSpeed * Time.deltaTime, Space.Self);
    }

    private void SpawnImpactBurst(Vector3 position)
    {
        GameObject burstObject = new GameObject("PhantomShotImpactBurst");
        burstObject.transform.position = position;

        // Radial spectral puff.
        ParticleSystem system = burstObject.AddComponent<ParticleSystem>();
        system.Stop();

        ParticleSystem.MainModule main = system.main;
        main.duration = impactDuration;
        main.loop = false;
        main.startLifetime = impactDuration;
        main.startSpeed = 4f;
        main.startSize = 0.12f;
        main.startColor = impactColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = Mathf.Max(1, impactParticleCount);

        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)impactParticleCount) });

        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        ParticleSystem.ColorOverLifetimeModule colorOverLife = system.colorOverLifetime;
        colorOverLife.enabled = true;
        colorOverLife.color = SpectralVFXUtility.FadeGradient(impactColor);

        SpectralVFXUtility.ConfigureParticleRenderer(system, impactColor);
        system.Play();

        // Quick expanding shell flash.
        GameObject shellObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shellObject.name = "ImpactShell";
        shellObject.transform.SetParent(burstObject.transform, false);
        shellObject.transform.localScale = Vector3.zero;

        Collider shellCollider = shellObject.GetComponent<Collider>();
        if (shellCollider != null)
            Destroy(shellCollider);

        Renderer shellRenderer = shellObject.GetComponent<Renderer>();
        if (shellRenderer != null)
        {
            shellRenderer.sharedMaterial = CreateSpectralMaterial(impactColor, true);
            shellRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            shellRenderer.receiveShadows = false;
        }

        SpectralImpactShell shell = shellObject.AddComponent<SpectralImpactShell>();
        shell.Initialize(shellRenderer, impactColor, impactShellRadius, impactDuration);

        Destroy(burstObject, impactDuration + 0.2f);
    }

    private Material CreateSpectralMaterial(Color color, bool additive)
    {
        Material material = SpectralVFXUtility.CreateMaterial(color, additive);

        if (material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", emissionColor * Mathf.Max(0f, emissionIntensity));
            material.EnableKeyword("_EMISSION");
        }

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

    /// <summary>
    /// Animates the short expanding, fading shell of the impact burst.
    /// </summary>
    private class SpectralImpactShell : MonoBehaviour
    {
        private Renderer shellRenderer;
        private Material material;
        private Color color;
        private float maxRadius;
        private float duration;
        private float startTime;

        public void Initialize(Renderer targetRenderer, Color shellColor, float radius, float shellDuration)
        {
            shellRenderer = targetRenderer;
            material = targetRenderer != null ? targetRenderer.sharedMaterial : null;
            color = shellColor;
            maxRadius = Mathf.Max(0.1f, radius);
            duration = Mathf.Max(0.05f, shellDuration);
            startTime = Time.time;
        }

        private void Update()
        {
            float progress = Mathf.Clamp01((Time.time - startTime) / duration);
            float scale = maxRadius * 2f * Mathf.SmoothStep(0f, 1f, progress);
            transform.localScale = Vector3.one * scale;

            if (material != null)
            {
                Color faded = color;
                faded.a *= 1f - progress;

                if (material.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", faded);
                if (material.HasProperty("_Color"))
                    material.SetColor("_Color", faded);
            }
        }
    }
}
