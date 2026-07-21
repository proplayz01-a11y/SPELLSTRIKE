using UnityEngine;

public class HomingProjectile : MonoBehaviour
{
    public float speed = 8f;
    public int damage = 25; // fallback only, gets overridden by Launch()
    public int wordLength = 0;
    private Transform target;
    private bool hasHit = false;
    private AttackController attackOwner;
    private string submittedWord = string.Empty;

    void Start()
    {
        FindNearestEnemy();
        if (target != null)
            Debug.Log("Projectile spawned. Target: " + target.name);
    }

    // Called by AttackController to pass dynamic damage
    public void Launch(
        int incomingDamage,
        int incomingWordLength = 0,
        AttackController owner = null,
        string incomingWord = "")
    {
        if (incomingDamage <= 0)
        {
            Debug.LogWarning("Projectile Launch() called with non-positive damage (" + incomingDamage + "). Keeping current damage: " + damage);
            return;
        }

        damage = incomingDamage;
        wordLength = incomingWordLength;
        attackOwner = owner;
        submittedWord = string.IsNullOrWhiteSpace(incomingWord)
            ? string.Empty
            : incomingWord.Trim().ToUpperInvariant();
        Debug.Log("Projectile damage set to: " + damage + " | Word length: " + wordLength + " | Word: " + submittedWord);
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
        VocabularyBarrier vocabularyBarrier = other.GetComponentInParent<VocabularyBarrier>();

        if (enemyController == null && brambleController == null && enemyHealth == null) return;

        hasHit = true;
        int resolvedDamage = vocabularyBarrier != null
            ? vocabularyBarrier.ResolveHit(submittedWord, damage)
            : damage;

        Debug.Log("Projectile hit: " + other.name + " | Damage: " + resolvedDamage + " | Word length: " + wordLength + " | Word: " + submittedWord);

        if (enemyController != null)
        {
            // EnemyController supports (float damage, int wordLength)
            enemyController.TakeDamage(resolvedDamage, wordLength);
        }
        else if (brambleController != null)
        {
            // BrambleSpriteController has only TakeDamage(float)
            brambleController.TakeDamage(resolvedDamage);
        }
        else if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(resolvedDamage);
        }

        if (wordLength > 0 && attackOwner != null)
            attackOwner.NotifySuccessfulAttackLanded();

        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        OnTriggerEnter(collision.collider);
    }
}
