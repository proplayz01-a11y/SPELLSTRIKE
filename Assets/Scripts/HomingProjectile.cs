using System;
using UnityEngine;

public class HomingProjectile : MonoBehaviour
{
    private const float MediumAttackImpactScale = 9f;

    public float speed = 8f;
    public int damage = 25; // fallback only, gets overridden by Launch()
    public int wordLength = 0;
    [SerializeField, Min(0f)] private float launchVisibilityDelay = 0.12f;
    [Header("Medium Attack Distance Bonus")]
    [SerializeField, Min(0f)] private float mediumBonusStartDistance = 4f;
    [SerializeField, Min(0.1f)] private float mediumBonusFullDistance = 24f;
    [SerializeField, Range(0f, 0.5f)] private float mediumMaximumDistanceBonus = 0.3f;
    private Transform target;
    private bool hasHit = false;
    private AttackController attackOwner;
    private string submittedWord = string.Empty;
    private bool targetChallengeCast;
    private float moveAfterTime;
    private bool isMediumAttack;
    private Vector3 lastTrackedPosition;
    private float distanceTravelled;

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
        moveAfterTime = Time.time + Mathf.Max(0f, launchVisibilityDelay);
        lastTrackedPosition = transform.position;
        distanceTravelled = 0f;
        if (explicitTarget != null)
            target = explicitTarget;
        submittedWord = string.IsNullOrWhiteSpace(incomingWord)
            ? string.Empty
            : incomingWord.Trim().ToUpperInvariant();
        Debug.Log("Projectile damage set to: " + damage + " | Word length: " + wordLength + " | Word: " + submittedWord);
    }

    /// <summary>
    /// Enables the Medium Attack's paired visual presentation without creating
    /// another collider, damage source, or owner-notification path.
    /// </summary>
    public bool EnableMediumTwinVisuals()
    {
        // The attack tier also controls its impact presentation. Keep this true
        // even if a missing visual component forces a single-orb visual fallback.
        isMediumAttack = true;
        Spellstrike.Vfx.MediumTwinOrbPresentation presentation =
            GetComponent<Spellstrike.Vfx.MediumTwinOrbPresentation>();
        return presentation != null && presentation.Activate(target);
    }

    private void ApplyDamageWithPresentationScale(Action applyDamage)
    {
        if (applyDamage == null)
            return;

        if (isMediumAttack)
        {
            Spellstrike.Vfx.EnemyDamageImpactTrigger.WithImpactScale(
                MediumAttackImpactScale,
                applyDamage);
            return;
        }

        applyDamage();
    }

    void Update()
    {
        if (target == null || hasHit || Time.time < moveAfterTime) return;
        var titan = target.GetComponent<TitanEarthbreakerController>();
        Vector3 targetPosition = titan != null ? titan.SpellAimPosition : target.position;
        Vector3 direction = targetPosition - transform.position;
        if (direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(direction);
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
        distanceTravelled += Vector3.Distance(lastTrackedPosition, transform.position);
        lastTrackedPosition = transform.position;
    }

    private int ApplyMediumDistanceDamageBonus(int incomingDamage)
    {
        // Target Challenge casts retain their existing barrier-only semantics.
        if (!isMediumAttack || targetChallengeCast || incomingDamage <= 0 || mediumMaximumDistanceBonus <= 0f)
            return incomingDamage;

        float startDistance = Mathf.Max(0f, mediumBonusStartDistance);
        float fullDistance = Mathf.Max(startDistance + 0.1f, mediumBonusFullDistance);
        float rangeProgress = Mathf.InverseLerp(startDistance, fullDistance, distanceTravelled);
        float damageBonus = rangeProgress * mediumMaximumDistanceBonus;
        if (damageBonus <= 0f)
            return incomingDamage;

        int boostedDamage = Mathf.Max(1, Mathf.RoundToInt(incomingDamage * (1f + damageBonus)));
        Debug.Log(
            $"[HomingProjectile] Medium range bonus: travelled {distanceTravelled:0.0} units, "
            + $"damage +{damageBonus:P0} ({incomingDamage} -> {boostedDamage}).");
        return boostedDamage;
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
        if (other == null) return;

        ResolveAcceptedImpact(other);
    }

    private void ResolveAcceptedImpact(Collider other)
    {
        if (hasHit || other == null) return;

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
        // Enemy-owned damage receivers now trigger the impact visual only after
        // actual health loss; zero-damage barrier contacts do not spawn enemy hits.

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

        resolvedDamage = ApplyMediumDistanceDamageBonus(resolvedDamage);

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
            ApplyDamageWithPresentationScale(
                () => enemyController.TakeDamage(resolvedDamage, wordLength));
        }
        else if (brambleController != null)
        {
            // BrambleSpriteController has only TakeDamage(float)
            ApplyDamageWithPresentationScale(
                () => brambleController.TakeDamage(resolvedDamage));
        }
        else if (enemyHealth != null)
        {
            ApplyDamageWithPresentationScale(
                () => enemyHealth.TakeDamage(resolvedDamage));
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

        if (isMediumAttack)
        {
            Spellstrike.Vfx.MediumTwinOrbPresentation presentation =
                GetComponent<Spellstrike.Vfx.MediumTwinOrbPresentation>();
            if (presentation != null)
                presentation.ReleaseTrails();
        }

        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.collider == null || hasHit) return;
        ResolveAcceptedImpact(collision.collider);
    }
}
