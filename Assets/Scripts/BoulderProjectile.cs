using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BoulderProjectile : MonoBehaviour
{
    public float damage = 12f;
    public float lifeTime = 5f;
    public GameObject impactEffect;

    private Rigidbody rb;
    private bool hasHit = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Launch(Vector3 velocity, float projectileDamage, float projectileLifeTime)
    {
        damage = projectileDamage;
        lifeTime = projectileLifeTime;

        rb.useGravity = false;
        rb.isKinematic = false;
        rb.linearVelocity = velocity;

        Destroy(gameObject, lifeTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        hasHit = true;

        Debug.Log("[BoulderProjectile] Hit: " + collision.collider.name);

        IDamageable damageable = collision.collider.GetComponentInParent<IDamageable>();

        if (damageable != null)
        {
            damageable.ApplyDamage(damage);
            Debug.Log("[BoulderProjectile] Dealt " + damage + " damage.");
        }
        else
        {
            Debug.LogWarning("[BoulderProjectile] No IDamageable found on hit object.");
        }

        if (impactEffect != null)
            Instantiate(impactEffect, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}