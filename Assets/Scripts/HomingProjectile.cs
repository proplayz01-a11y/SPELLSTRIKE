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
    private bool targetChallengeCast;

    void Start()
    {
        if (target == null)
            FindNearestEnemy();
        if (target != null)
            Debug.Log("Projectile spawned. Target: " + target.name);
    }

    // Called by AttackController to pass dynamic damage
    public void Launch(
        int incomingDamage,
        int incomingWordLength = 0,
        AttackController owner = null,
        string incomingWord = "",
        bool isTargetChallengeCast = false,
        Transform explicitTarget = null)
    {
        if (incomingDamage <= 0)
        {
            Debug.LogWarning("Projectile Launch() called with non-positive damage (" + incomingDamage + "). Keeping current damage: " + damage);
            return;
        }

        damage = incomingDamage;
        wordLength = incomingWordLength;
        attackOwner = owner;
        targetChallengeCast = isTargetChallengeCast;
        if (explicitTarget != null)
            target = explicitTarget;
        submittedWord = string.IsNullOrWhiteSpace(incomingWord)
            ? string.Empty
            : incomingWord.Trim().ToUpperInvariant();
        Debug.Log("Projectile damage set to: " + damage + " | Word length: " + wordLength + " | Word: " + submittedWord);
    }

    void Update()
    {
        if (target == null || hasHit) return;
        var titan = target.GetComponent<TitanEarthbreakerController>();
        Vector3 targetPosition = titan != null ? titan.SpellAimPosition : target.position;
        Vector3 direction = targetPosition - transform.position;
        if (direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(direction);
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
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
        VocabularyWorldTarget worldTarget = other.GetComponentInParent<VocabularyWorldTarget>();

        bool validWorldTargetImpact =
            targetChallengeCast
            && worldTarget != null
            && worldTarget.CanBeTargeted;

        if (enemyController == null
            && brambleController == null
            && enemyHealth == null
            && !validWorldTargetImpact)
        {
            return;
        }

        hasHit = true;
        bool matchedActiveChallengeBarrier =
            !targetChallengeCast
            || (attackOwner != null
                && attackOwner.CanResolveTargetChallengeAgainst(vocabularyBarrier));
        bool matchedExactTargetWord =
            targetChallengeCast
            && matchedActiveChallengeBarrier
            && vocabularyBarrier != null
            && vocabularyBarrier.TargetWord != null
            && vocabularyBarrier.TargetWord.Matches(submittedWord);

        int resolvedDamage;
        if (targetChallengeCast && !matchedActiveChallengeBarrier)
        {
            resolvedDamage = 0;
        }
        else
        {
            resolvedDamage = vocabularyBarrier != null
                ? vocabularyBarrier.ResolveHit(
                    submittedWord,
                    damage,
                    targetChallengeCast)
                : damage;
        }

        Debug.Log("Projectile hit: " + other.name + " | Damage: " + resolvedDamage + " | Word length: " + wordLength + " | Word: " + submittedWord);

        if (targetChallengeCast && !matchedActiveChallengeBarrier)
        {
            Debug.LogWarning(
                "[HomingProjectile] Target Challenge projectile hit an unrelated or inactive barrier target. "
                + "No damage was applied.");
        }
        else if (matchedExactTargetWord && resolvedDamage <= 0)
        {
            Debug.Log(
                "[HomingProjectile] The exact target word broke the vocabulary barrier with a scripted zero-damage impact. "
                + "Enemy health was not reduced.");
        }
        else if (targetChallengeCast && resolvedDamage <= 0)
        {
            Debug.Log(
                "[HomingProjectile] The active vocabulary barrier absorbed the valid non-target spell. "
                + "No enemy damage or hit reaction was applied.");
        }
        else if (enemyController != null)
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

        if (targetChallengeCast && attackOwner != null)
        {
            attackOwner.NotifyTargetChallengeProjectileResolved(
                submittedWord,
                vocabularyBarrier,
                matchedActiveChallengeBarrier);
        }
        else if (wordLength > 0 && attackOwner != null)
        {
            attackOwner.NotifySuccessfulAttackLanded(submittedWord);
        }

        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        OnTriggerEnter(collision.collider);
    }
}
