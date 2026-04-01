using UnityEngine;

public class PlayerProjectile : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 20;
    private Transform target;

    public void Launch(Transform targetEnemy)
    {
        target = targetEnemy;
        gameObject.SetActive(true);
    }

    void Update()
    {
        if (target == null) return;

        transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target.position) < 0.1f)
        {
            var damageable = target.GetComponentInParent<IDamageable>();
            if (damageable != null)
                damageable.ApplyDamage(damage);

            gameObject.SetActive(false);
        }
    }
}