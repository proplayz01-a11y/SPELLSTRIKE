using UnityEngine;

public enum DesertArcherArrowType
{
    Quick,
    ChargedRune,
    PiercingRune
}

[DisallowMultipleComponent]
public sealed class DesertArcherArrowProjectile : MonoBehaviour
{
    [Header("Collision")]
    [SerializeField] private string playerTag = "Player";

    private DesertArcherController ownerController;
    private Transform owner;
    private Transform target;
    private DesertArcherArrowType arrowType;
    private Vector3 direction;
    private float damage;
    private float speed;
    private float lifetime;
    private float hitRadius;
    private Quaternion visualRotationOffset = Quaternion.identity;
    private float spawnTime;
    private bool launched;
    private bool resolved;
    private bool reportedPlayerHit;

    public DesertArcherArrowType ArrowType => arrowType;
    public float Damage => damage;
    public DesertArcherController OwnerController => ownerController;

    public void DisarmAfterOwnerDeath()
    {
        if (!launched || resolved)
            return;

        // Keep the arrow physically present and let it finish its current flight,
        // but make post-mortem hits harmless and stop reporting combat follow-ups.
        damage = 0f;
        ownerController = null;
    }

    public void Launch(
        DesertArcherController controller,
        DesertArcherArrowType type,
        Transform ownerTransform,
        Transform targetTransform,
        Vector3 spawnPosition,
        Vector3 targetPosition,
        float damageAmount,
        float projectileSpeed,
        float projectileLifetime,
        float projectileHitRadius,
        Quaternion projectileVisualRotationOffset)
    {
        ownerController = controller;
        arrowType = type;
        owner = ownerTransform;
        target = targetTransform;
        damage = Mathf.Max(0f, damageAmount);
        speed = Mathf.Max(0.1f, projectileSpeed);
        lifetime = Mathf.Max(0.1f, projectileLifetime);
        hitRadius = Mathf.Max(0.02f, projectileHitRadius);
        visualRotationOffset = projectileVisualRotationOffset;
        spawnTime = Time.time;
        launched = true;
        resolved = false;
        reportedPlayerHit = false;

        transform.position = spawnPosition;
        direction = targetPosition - spawnPosition;
        if (direction.sqrMagnitude <= 0.0001f)
            direction = owner != null ? owner.forward : transform.forward;
        direction.Normalize();

        // The authored rune-arrow wrapper contract is +Z = flight direction.
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up) * visualRotationOffset;
        EnsurePhysicsSupport();
    }

    private void Update()
    {
        if (!launched || resolved)
            return;

        if (Time.time - spawnTime >= lifetime)
        {
            ResolveMiss();
            return;
        }

        Vector3 start = transform.position;
        Vector3 end = start + direction * speed * Time.deltaTime;

        if (TryResolveSegment(start, end))
            return;

        transform.position = end;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!launched || resolved || other == null || IsOwnerCollider(other))
            return;

        if (IsPlayerCollider(other))
        {
            if (!reportedPlayerHit)
                HitPlayer(other);
            return;
        }

        if (!other.isTrigger)
            ResolveMiss();
    }

    private bool TryResolveSegment(Vector3 start, Vector3 end)
    {
        Vector3 segment = end - start;
        float distance = segment.magnitude;
        if (distance <= 0.0001f)
            return false;

        RaycastHit[] hits = Physics.SphereCastAll(
            start,
            hitRadius,
            segment / distance,
            distance,
            Physics.AllLayers,
            QueryTriggerInteraction.Collide);

        if (hits == null || hits.Length == 0)
            return false;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null || IsOwnerCollider(hitCollider))
                continue;

            if (IsPlayerCollider(hitCollider))
            {
                if (reportedPlayerHit)
                    continue;

                transform.position = hits[i].point;
                HitPlayer(hitCollider);
                return resolved;
            }

            // Trigger volumes such as node gates and pickups must not eat arrows.
            if (hitCollider.isTrigger)
                continue;

            transform.position = hits[i].point;
            ResolveMiss();
            return true;
        }

        return false;
    }

    private void HitPlayer(Collider hitCollider)
    {
        if (reportedPlayerHit || resolved)
            return;

        reportedPlayerHit = true;
        ApplyDamage(hitCollider, damage);
        ownerController?.ReportProjectileResolved(arrowType, true);

        resolved = true;
        Destroy(gameObject);
    }

    private void ResolveMiss()
    {
        if (resolved)
            return;

        resolved = true;
        if (!reportedPlayerHit)
            ownerController?.ReportProjectileResolved(arrowType, false);
        Destroy(gameObject);
    }

    private bool IsOwnerCollider(Collider candidate)
    {
        if (owner == null || candidate == null)
            return false;

        Transform candidateTransform = candidate.transform;
        return candidateTransform == owner
            || candidateTransform.IsChildOf(owner)
            || owner.IsChildOf(candidateTransform);
    }

    private bool IsPlayerCollider(Collider candidate)
    {
        if (candidate == null)
            return false;

        Transform candidateTransform = candidate.transform;
        if (target != null &&
            (candidateTransform == target
             || candidateTransform.IsChildOf(target)
             || target.IsChildOf(candidateTransform)))
        {
            return true;
        }

        if (candidate.CompareTag(playerTag))
            return true;

        Transform root = candidateTransform.root;
        if (root != null && root.CompareTag(playerTag))
            return true;

        return candidate.GetComponentInParent<PlayerHealth>() != null;
    }

    private void ApplyDamage(Collider hitCollider, float amount)
    {
        if (amount <= 0f)
            return;

        IDamageable damageable = hitCollider != null
            ? hitCollider.GetComponentInParent<IDamageable>()
            : null;

        if (damageable == null && target != null)
            damageable = target.GetComponentInChildren<IDamageable>();
        if (damageable == null && target != null)
            damageable = target.GetComponentInParent<IDamageable>();

        if (damageable != null)
            damageable.ApplyDamage(amount);
        else if (target != null)
            target.SendMessage("ApplyDamage", amount, SendMessageOptions.DontRequireReceiver);
    }

    private void EnsurePhysicsSupport()
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
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }
}
