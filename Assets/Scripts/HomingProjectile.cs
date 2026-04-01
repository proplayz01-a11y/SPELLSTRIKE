using UnityEngine;
public class HomingProjectile : MonoBehaviour
{
    public float speed = 8f;
    public int damage = 25; // fallback only, gets overridden by Launch()
    private Transform target;
    private bool hasHit = false;
    void Start()
    {
        FindNearestEnemy();
        if (target != null)
            Debug.Log("Projectile spawned. Target: " + target.name);
    }
    // Called by AttackController to pass dynamic damage
    public void Launch(int incomingDamage)
    {
        if (incomingDamage <= 0)
        {
            Debug.LogWarning("Projectile Launch() called with non-positive damage (" + incomingDamage + "). Keeping current damage: " + damage);
            return;
        }

        damage = incomingDamage;
        Debug.Log("Projectile damage set to: " + damage);
    }
    void Update()
    {
        if (target == null || hasHit) return;
        transform.LookAt(target);
        transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
    }
    void FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        float closestDistance = Mathf.Infinity;
        Transform closestEnemy = null;
        foreach (GameObject enemy in enemies)
        {
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestEnemy = enemy.transform;
            }
        }
        target = closestEnemy;
        if (target != null)
            Debug.Log("Nearest enemy: " + target.name);
    }
    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        EnemyController enemyController = other.GetComponentInParent<EnemyController>();
        BrambleSpriteController brambleController = other.GetComponentInParent<BrambleSpriteController>();
        EnemyHealth enemyHealth = other.GetComponentInParent<EnemyHealth>();

        if (enemyController == null && brambleController == null && enemyHealth == null) return;

        hasHit = true;
        Debug.Log("Projectile hit: " + other.name + " | Damage: " + damage);

        if (enemyController != null)
            enemyController.TakeDamage(damage);
        else if (brambleController != null)
            brambleController.TakeDamage(damage);
        else
            enemyHealth.TakeDamage(damage);

        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        OnTriggerEnter(collision.collider);
    }
}
