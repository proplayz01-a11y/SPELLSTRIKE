using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ThornProjectile : MonoBehaviour
{
    public float speed = 14f;
    public int damage = 8;
    public float lifeTime = 5f;
    public float knockbackForce = 8f;

    private Vector3 direction;
    private bool launched;

    public void Launch(Vector3 directionVector, int damageAmount)
    {
        direction = directionVector.sqrMagnitude > 0.01f ? directionVector.normalized : transform.forward;
        damage = Mathf.Max(1, damageAmount);
        launched = true;
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (!launched) return;
        transform.position += direction * speed * Time.deltaTime;
        if (direction.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(direction);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.ApplyDamage(damage);
        }
        else
        {
            other.SendMessage("ApplyDamage", damage, SendMessageOptions.DontRequireReceiver);
        }

        var movement = other.GetComponentInParent<PlayerMovement>();
        if (movement != null)
            movement.ApplyKnockback(direction, knockbackForce);

        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        OnTriggerEnter(collision.collider);
    }
}
