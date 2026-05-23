using UnityEngine;

public class ConfettiPellet : MonoBehaviour
{
    [Header("Lifetime")]
    public float lifetime = 2f;

    private Vector3 moveDirection;
    private float moveSpeed;
    private float damage;
    private bool initialized = false;

    public void Init(Vector3 direction, float speed, float pelletDamage)
    {
        moveDirection = direction.normalized;
        moveSpeed = speed;
        damage = pelletDamage;
        initialized = true;

        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (!initialized) return;

        transform.position += moveDirection * moveSpeed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!initialized) return;

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

        Destroy(gameObject);
    }
}
