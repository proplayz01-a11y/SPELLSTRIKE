using UnityEngine;

[DisallowMultipleComponent]
public sealed class SunboundWardenRadiantWave : MonoBehaviour
{
    private const string ShaderResourcePath = "Shaders/Stage3RadiantWave";

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

    private Material coreMaterial;
    private Material glowMaterial;
    private Mesh coreMesh;
    private Mesh glowMesh;

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
        float stunTime,
        float visualWidth,
        float visualHeight)
    {
        owner = ownerTransform;
        target = targetTransform;
        direction = launchDirection;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
            direction = owner != null ? owner.forward : Vector3.forward;
        direction.Normalize();

        damage = Mathf.Max(0f, damageAmount);
        speed = Mathf.Max(0.1f, projectileSpeed);
        lifetime = Mathf.Max(0.1f, projectileLifetime);
        hitRadius = Mathf.Max(0.25f, projectileHitRadius);
        knockbackForce = Mathf.Max(0f, knockbackAmount);
        stunDuration = Mathf.Max(0f, stunTime);

        transform.SetPositionAndRotation(
            spawnPosition,
            Quaternion.LookRotation(direction, Vector3.up));

        BuildVisual(Mathf.Max(0.5f, visualWidth), Mathf.Max(0.5f, visualHeight));
        EnsureTriggerCollider(visualWidth, visualHeight);

        spawnTime = Time.time;
        launched = true;
        resolved = false;
    }

    private void Update()
    {
        if (!launched || resolved)
            return;

        float age = Time.time - spawnTime;
        if (age >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 start = transform.position;
        Vector3 end = start + direction * speed * Time.deltaTime;
        transform.position = end;

        float fadeWindow = Mathf.Min(0.18f, lifetime * 0.35f);
        float fade = fadeWindow > 0.001f
            ? Mathf.Clamp01((lifetime - age) / fadeWindow)
            : 1f;
        float pulse = 1f + Mathf.Sin(age * 22f) * 0.035f;
        transform.localScale = Vector3.one * pulse;
        SetMaterialAlpha(coreMaterial, 0.88f * fade);
        SetMaterialAlpha(glowMaterial, 0.34f * fade);

        if (TargetIntersectsSegment(start, end))
            ResolveHit();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!launched || resolved || other == null || IsOwnerCollider(other))
            return;

        if (IsPlayerCollider(other))
            ResolveHit();
    }

    private bool TargetIntersectsSegment(Vector3 start, Vector3 end)
    {
        if (target == null)
            return false;

        Collider targetCollider = target.GetComponentInChildren<Collider>();
        Vector3 point = targetCollider != null
            ? targetCollider.bounds.center
            : target.position + Vector3.up;

        Vector3 segment = end - start;
        float lengthSquared = segment.sqrMagnitude;
        float t = lengthSquared > 0.0001f
            ? Mathf.Clamp01(Vector3.Dot(point - start, segment) / lengthSquared)
            : 0f;

        return Vector3.Distance(point, start + segment * t) <= hitRadius;
    }

    private void ResolveHit()
    {
        if (resolved)
            return;

        resolved = true;
        ApplyDamage(target, damage);
        ApplyCrowdControl(target, direction, knockbackForce, stunDuration);
        SpawnImpact();

        Collider trigger = GetComponent<Collider>();
        if (trigger != null)
            trigger.enabled = false;

        Destroy(gameObject, 0.12f);
    }

    private bool IsPlayerCollider(Collider other)
    {
        if (target != null &&
            (other.transform == target ||
             other.transform.IsChildOf(target) ||
             target.IsChildOf(other.transform)))
        {
            return true;
        }

        return other.CompareTag("Player") || other.transform.root.CompareTag("Player");
    }

    private bool IsOwnerCollider(Collider other)
    {
        return owner != null &&
               (other.transform == owner || other.transform.IsChildOf(owner));
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

    private static void ApplyCrowdControl(
        Transform playerTransform,
        Vector3 knockbackDirection,
        float force,
        float duration)
    {
        if (playerTransform == null)
            return;

        PlayerMovement currentMovement = playerTransform.GetComponentInChildren<PlayerMovement>();
        if (currentMovement == null)
            currentMovement = playerTransform.GetComponentInParent<PlayerMovement>();

        if (currentMovement != null)
        {
            currentMovement.ApplyKnockback(knockbackDirection, force);
            currentMovement.ApplyStun(duration);
            return;
        }

        MovementofPlayer movement = playerTransform.GetComponentInChildren<MovementofPlayer>();
        if (movement == null)
            movement = playerTransform.GetComponentInParent<MovementofPlayer>();
        if (movement == null)
            return;

        Stage3RadiantRushCrowdControl effect =
            movement.GetComponent<Stage3RadiantRushCrowdControl>();
        if (effect == null)
            effect = movement.gameObject.AddComponent<Stage3RadiantRushCrowdControl>();

        effect.Begin(movement, knockbackDirection, force, duration);
    }

    private void EnsureTriggerCollider(float visualWidth, float visualHeight)
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null)
            box = gameObject.AddComponent<BoxCollider>();

        box.isTrigger = true;
        box.center = new Vector3(0f, Mathf.Max(0.35f, visualHeight * 0.22f), 0f);
        box.size = new Vector3(
            Mathf.Max(1f, visualWidth * 0.92f),
            Mathf.Max(1f, visualHeight),
            0.9f);

        Rigidbody body = GetComponent<Rigidbody>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    private void BuildVisual(float width, float height)
    {
        Shader shader = Resources.Load<Shader>(ShaderResourcePath);
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");
        if (shader == null)
            return;

        coreMaterial = new Material(shader)
        {
            name = "RadiantRushWave_Core_Runtime"
        };
        glowMaterial = new Material(shader)
        {
            name = "RadiantRushWave_Glow_Runtime"
        };

        SetMaterialColor(coreMaterial, new Color(1f, 0.76f, 0.16f, 0.88f));
        SetMaterialColor(glowMaterial, new Color(1f, 0.94f, 0.48f, 0.34f));

        coreMesh = BuildArcMesh(width, height, 0.58f, 20, 118f);
        glowMesh = BuildArcMesh(width * 1.12f, height * 1.12f, 0.52f, 20, 124f);

        CreateLayer("RadiantWaveGlow", glowMesh, glowMaterial, 0.035f);
        CreateLayer("RadiantWaveCore", coreMesh, coreMaterial, 0f);
    }

    private void CreateLayer(string objectName, Mesh mesh, Material material, float zOffset)
    {
        GameObject layer = new GameObject(objectName);
        layer.transform.SetParent(transform, false);
        layer.transform.localPosition = new Vector3(0f, 0f, zOffset);

        MeshFilter filter = layer.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;

        MeshRenderer renderer = layer.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
    }

    private static Mesh BuildArcMesh(
        float width,
        float height,
        float innerScale,
        int segments,
        float arcDegrees)
    {
        int vertexCount = (segments + 1) * 2;
        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        int[] triangles = new int[segments * 6];

        float halfArc = arcDegrees * 0.5f;
        float halfWidth = width * 0.5f;
        // Keep the crescent centered around the sword instead of floating above
        // the Warden's hand. The tips dip below the origin while the bright crest
        // rises through torso height.
        float verticalShift = -height * 0.78f;

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = Mathf.Lerp(-halfArc, halfArc, t) * Mathf.Deg2Rad;
            float sin = Mathf.Sin(angle);
            float cos = Mathf.Cos(angle);

            Vector3 outer = new Vector3(
                sin * halfWidth,
                cos * height + verticalShift,
                0f);
            Vector3 inner = new Vector3(
                sin * halfWidth * innerScale,
                cos * height * innerScale + verticalShift + height * 0.04f,
                0f);

            int outerIndex = i * 2;
            int innerIndex = outerIndex + 1;
            vertices[outerIndex] = outer;
            vertices[innerIndex] = inner;
            uvs[outerIndex] = new Vector2(t, 1f);
            uvs[innerIndex] = new Vector2(t, 0f);

            if (i >= segments)
                continue;

            int tri = i * 6;
            int nextOuter = outerIndex + 2;
            int nextInner = outerIndex + 3;
            triangles[tri] = outerIndex;
            triangles[tri + 1] = nextOuter;
            triangles[tri + 2] = innerIndex;
            triangles[tri + 3] = nextOuter;
            triangles[tri + 4] = nextInner;
            triangles[tri + 5] = innerIndex;
        }

        Mesh mesh = new Mesh
        {
            name = "RadiantRushWave_RuntimeMesh"
        };
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        return mesh;
    }

    private static void SetMaterialColor(Material material, Color color)
    {
        if (material == null)
            return;

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        else if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
    }

    private static void SetMaterialAlpha(Material material, float alpha)
    {
        if (material == null)
            return;

        string property = material.HasProperty("_BaseColor")
            ? "_BaseColor"
            : material.HasProperty("_Color")
                ? "_Color"
                : null;
        if (property == null)
            return;

        Color color = material.GetColor(property);
        color.a = Mathf.Clamp01(alpha);
        material.SetColor(property, color);
    }

    private void SpawnImpact()
    {
        GameObject impact = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        impact.name = "RadiantRushWaveImpact_Runtime";
        impact.transform.position = transform.position;
        impact.transform.localScale = Vector3.one * 0.65f;

        Collider collider = impact.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        Renderer renderer = impact.GetComponent<Renderer>();
        if (renderer != null)
        {
            Shader shader = Resources.Load<Shader>(ShaderResourcePath);
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");

            if (shader != null)
            {
                Material material = new Material(shader)
                {
                    name = "RadiantRushWaveImpact_Runtime"
                };
                SetMaterialColor(material, new Color(1f, 0.9f, 0.35f, 0.82f));
                renderer.material = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                Destroy(material, 0.2f);
            }
        }

        Destroy(impact, 0.18f);
    }

    private void OnDestroy()
    {
        if (coreMaterial != null)
            Destroy(coreMaterial);
        if (glowMaterial != null)
            Destroy(glowMaterial);
        if (coreMesh != null)
            Destroy(coreMesh);
        if (glowMesh != null)
            Destroy(glowMesh);
    }
}
