using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BoulderProjectile : MonoBehaviour
{
    public float damage = 12f;
    public float lifeTime = 5f;
    public GameObject impactEffect;

    [Header("Heavy Ground Bounce")]
    [SerializeField, Range(0, 3)] private int maxGroundBounces = 2;
    [SerializeField, Range(0f, 0.8f)] private float firstBounceRetention = 0.38f;
    [SerializeField, Range(0f, 1f)] private float secondBounceRetention = 0.20f;
    [SerializeField, Range(0f, 1f)] private float horizontalImpactRetention = 0.58f;
    [SerializeField, Min(0f)] private float minimumGroundImpactSpeed = 4f;
    [SerializeField, Min(0.1f)] private float settleTime = 0.55f;

    private Rigidbody rb;
    private bool hasHit = false;
    private bool launched;
    private float gravityMultiplier = 1f;
    private int groundBounceCount;
    private Vector3 velocityBeforePhysics;

    public int GroundBounceCount => groundBounceCount;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Launch(Vector3 velocity, float projectileDamage, float projectileLifeTime)
    {
        Launch(velocity, projectileDamage, projectileLifeTime, 1f, 0f, null);
    }

    public void Launch(
        Vector3 velocity, float projectileDamage, float projectileLifeTime,
        float heavyGravityMultiplier, float tumbleRadiansPerSecond,
        Collider[] throwerColliders)
    {
        damage = projectileDamage;
        lifeTime = Mathf.Max(0.1f, projectileLifeTime);
        gravityMultiplier = Mathf.Max(1f, heavyGravityMultiplier);
        hasHit = false;
        launched = true;
        groundBounceCount = 0;
        velocityBeforePhysics = velocity;

        // Let Unity physics integrate a REAL parabolic flight, not a
        // script-controlled linear projectile. High-speed collision checks
        // help prevent tunneling through the player/terrain.
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.mass = 30f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0.025f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.linearVelocity = velocity;
        rb.maxAngularVelocity = Mathf.Max(
            rb.maxAngularVelocity, tumbleRadiansPerSecond + 2f);

        // Angular velocity is applied to the Rigidbody, making the large
        // rock visibly tumble around its sideways throw axis while airborne.
        Vector3 sideways = Vector3.Cross(velocity.normalized, Vector3.up);
        if (sideways.sqrMagnitude < 0.01f)
            sideways = transform.right;
        Vector3 tumbleAxis = (sideways.normalized + velocity.normalized * 0.18f).normalized;
        rb.angularVelocity = tumbleAxis * Mathf.Max(0f, tumbleRadiansPerSecond);

        // The Stone Sentinel must not instantly collide with the giant rock
        // on release while its animation hand is still overlapping it.
        Collider[] boulderColliders = GetComponentsInChildren<Collider>(true);
        if (throwerColliders != null)
        {
            foreach (Collider rockCollider in boulderColliders)
                foreach (Collider throwerCollider in throwerColliders)
                    if (rockCollider != null && throwerCollider != null)
                        Physics.IgnoreCollision(rockCollider, throwerCollider, true);
        }

        Destroy(gameObject, lifeTime);
    }

    private void FixedUpdate()
    {
        if (!launched || hasHit || rb == null || rb.isKinematic)
            return;

        // OnCollisionEnter fires after the physics solver. Remember the
        // approach velocity so a heavy floor hit still has a proper rebound
        // even when the solver has already removed the downward component.
        velocityBeforePhysics = rb.linearVelocity;

        // Extra downward acceleration matches the Stage 1 game's very large
        // world scale, while the ballistic aim solves against the same value.
        // Unity's normal gravity remains enabled on the Rigidbody.
        if (gravityMultiplier > 1f)
            rb.AddForce(Physics.gravity * (gravityMultiplier - 1f),
                ForceMode.Acceleration);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit || !launched) return;

        IDamageable damageable = collision.collider.GetComponentInParent<IDamageable>();

        if (damageable != null)
        {
            hasHit = true; // Player damage is authorized by one impact ONLY.
            damageable.ApplyDamage(damage);
            Debug.Log("[BoulderProjectile] Dealt " + damage + " damage.");
            SpawnImpactEffect(collision);
            Destroy(gameObject);
            return;
        }

        // Heavy rock: it may hit the walkable floor, bounce once clearly,
        // make one smaller hop, then settle/roll. A wall or ceiling instead
        // stops it. No kinematic teleport or fake movement is used.
        Vector3 groundNormal = Vector3.zero;
        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;
            if (normal.y > groundNormal.y)
                groundNormal = normal;
        }

        if (groundNormal.y >= 0.55f && groundBounceCount < maxGroundBounces)
        {
            float approachSpeed = Mathf.Max(
                Mathf.Max(0f, -Vector3.Dot(velocityBeforePhysics, groundNormal)),
                Mathf.Abs(Vector3.Dot(collision.relativeVelocity, groundNormal)));
            if (approachSpeed >= minimumGroundImpactSpeed)
            {
                float retention = groundBounceCount == 0
                    ? firstBounceRetention : secondBounceRetention;
                // Cap the rebound so it looks like a massive boulder rather
                // than a rubber ball, even with scaled Stage 1 gravity.
                float reboundSpeed = Mathf.Min(24f, approachSpeed * retention);
                Vector3 tangential = Vector3.ProjectOnPlane(
                    velocityBeforePhysics, groundNormal) * horizontalImpactRetention;
                rb.linearVelocity = tangential + groundNormal * reboundSpeed;
                rb.angularVelocity *= 0.70f;
                groundBounceCount++;
                SpawnImpactEffect(collision);
                Debug.Log($"[BoulderProjectile] Ground bounce {groundBounceCount}/{maxGroundBounces} " +
                          $"at {reboundSpeed:0.0} units/s.");
                return;
            }
        }

        // Final ground contact or solid wall: retain a little physical
        // inertia for a short, heavy settle/roll before cleanup.
        hasHit = true;
        rb.linearDamping = 6f;
        rb.angularDamping = 8f;
        SpawnImpactEffect(collision);
        Destroy(gameObject, settleTime);
    }

    private void SpawnImpactEffect(Collision collision)
    {
        if (impactEffect == null)
            return;
        Vector3 point = collision.contactCount > 0
            ? collision.GetContact(0).point : transform.position;
        Instantiate(impactEffect, point, Quaternion.identity);
    }
}
