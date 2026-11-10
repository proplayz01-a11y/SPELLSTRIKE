using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class StoneSentinelsController : MonoBehaviour
{
    public enum EnemyState
    {
        Approach,
        CloseAttack,
        Recover,
        BeingHit,
        Dead,
        BoulderThrow,
        JumpAttack,
        Counterspell
    }

    [Header("References")]
    public Transform player;
    public NavMeshAgent agent;
    public Animator animator;

    [Header("Counterspell Facing")]
    [SerializeField, Min(1f)] private float counterspellTurnSpeed = 210f;

    [Header("Death / Progression")]
public MonoBehaviour deathReceiver;
public string deathMessage = "OnStoneSentinelDefeated";

    [Header("Potion Drop")]
    public bool dropPotionOnDeath = true;
    public string potionDropLogSource = "StoneSentinel";
    [Range(0f, 1f)] public float potionDropChance = 1f;
    public int potionDropMinCount = 1;
    public int potionDropMaxCount = 1;
    public int potionDropAmountPerDrop = 1;
    public bool canDropHealthPotion = true;
    public bool canDropPurifyPotion = true;
    public bool canDropPowerUpPotion = true;

    [Header("Stats")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("Movement")]
    public float walkSpeed = 2.5f;
    [SerializeField, Min(0.05f)] private float accelerationTime = 0.5f;

    [Header("Ranges")]
    public float closeAttackRange = 3f;
    public float jumpAttackRange = 7f;

    [Header("Combat")]
    public float closeAttackDamage = 10f;
    public float recoverTime = 0.7f;

    [Header("State")]
    public EnemyState currentState = EnemyState.Approach;

    [Header("Boulder Throw")]
    public float boulderCooldown = 3f;
    private float boulderCooldownTimer = 0f;

    [Header("Boulder Projectile")]
    public GameObject boulderPrefab;
    public Transform boulderSpawnPoint;
    public float boulderDamage = 12f;
    public float boulderLifeTime = 5f;
    public float boulderSpeed = 15f;

    [Header("Boulder Visual / Projectile")]
    public GameObject heldBoulder;
    public float boulderArcAngle = 45f;

    [Header("Heavy Boulder Throw - Runtime Physics")]
    [SerializeField, Range(1f, 1.5f)] private float boulderSizeMultiplier = 1.2f;
    [SerializeField, Range(1f, 2f)] private float boulderSpeedMultiplier = 1.25f;
    [SerializeField, Range(1f, 15f)] private float boulderGravityMultiplier = 9f;
    [SerializeField, Range(1f, 25f)] private float boulderTumbleSpeed = 17f;
    private Vector3 originalHeldBoulderScale;
    private bool heldBoulderScaleCached;

    [Header("Jump Attack")]
    public float jumpMoveDuration = 0.6f;
    public float jumpLandingOffset = 1.5f;
    public float jumpHeight = 1.2f;
    public float jumpAttackDamage = 18f;
    public float jumpAttackDamageRadius = 2.5f;

    private Coroutine jumpMoveCoroutine;
    private bool jumpAttackStarted = false;
    private bool jumpLeapStarted = false;

    private bool battleStarted = false;
    private bool closeAttackStarted = false;
    private Coroutine recoverCoroutine;
    private bool boulderThrowStarted = false;
    private bool potionDropResolved = false;
    private bool vocabularyPaused = false;

    public event Action BeingHitFinished;
    public bool IsBattleStarted => battleStarted;
    public bool IsVocabularyPaused => vocabularyPaused;

    private void Awake()
    {
        agent ??= GetComponent<NavMeshAgent>();
        animator ??= GetComponent<Animator>();
        currentHealth = maxHealth;

        if (player == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }
    }

    private void Start()
    {
        if (agent != null)
        {
            agent.updateRotation = false;
            agent.updatePosition = true;
            EnemyNavMovement.ApplySpeed(agent, walkSpeed, accelerationTime);
            agent.stoppingDistance = 0f;
            agent.autoBraking = true;
        }

        if (animator != null)
            animator.applyRootMotion = false;
    }

    private void Update()
    {
        // Temporary manual test key
        if (Input.GetKeyDown(KeyCode.B))
            StartBattle();

        if (Input.GetKeyDown(KeyCode.T))
        {
            currentState = EnemyState.BoulderThrow;
            boulderThrowStarted = false;
        }

        if (!battleStarted) return;
        if (currentState == EnemyState.Dead) return;
        if (vocabularyPaused)
        {
            if (player != null)
                FaceTarget(player.position, counterspellTurnSpeed);

            return;
        }
        if (player == null) return;

        FaceTarget(player.position);

        switch (currentState)
        {
            case EnemyState.Approach:
                HandleApproach();
                break;

            case EnemyState.CloseAttack:
                HandleCloseAttack();
                break;

            case EnemyState.Recover:
                break;

            case EnemyState.BeingHit:
                break;
            case EnemyState.BoulderThrow:
                HandleBoulderThrow();
                break;
            case EnemyState.JumpAttack:
                HandleJumpAttack();
                break;
        }
    }

    private void HandleApproach()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // CLOSE RANGE -> CloseAttack
        if (distanceToPlayer <= closeAttackRange)
        {
            agent.isStopped = true;
            agent.ResetPath();

            currentState = EnemyState.CloseAttack;
            closeAttackStarted = false;
            return;
        }

        // MID RANGE -> JumpAttack
        if (distanceToPlayer <= jumpAttackRange)
        {
            agent.isStopped = true;
            agent.ResetPath();

            currentState = EnemyState.JumpAttack;
            jumpAttackStarted = false;
            return;
        }

        // FAR RANGE -> BoulderThrow if cooldown ready
        if (boulderCooldownTimer <= 0f)
        {
            agent.isStopped = true;
            agent.ResetPath();

            currentState = EnemyState.BoulderThrow;
            boulderThrowStarted = false;
            return;
        }

        // Far but BoulderThrow on cooldown -> approach
        agent.isStopped = false;
        agent.SetDestination(player.position);
    }

    private void HandleCloseAttack()
    {
        agent.isStopped = true;
        agent.ResetPath();

        if (!closeAttackStarted)
        {
            closeAttackStarted = true;

            if (animator != null)
                animator.SetTrigger("CloseAttack");
        }
    }

    public void OnCloseAttackHit()
    {
        if (vocabularyPaused) return;
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= closeAttackRange + 0.5f)
            DealDamageToPlayer(closeAttackDamage);
    }

    public void OnCloseAttackFinished()
    {
        if (vocabularyPaused) return;
        EnterRecover();
    }

    private void HandleBoulderThrow()
    {
        agent.isStopped = true;
        agent.ResetPath();

        if (!boulderThrowStarted)
        {
            boulderThrowStarted = true;

            if (animator != null)
                animator.SetTrigger("BoulderThrow");
        }
    }

    public void OnBoulderPickup()
    {
        if (vocabularyPaused || currentState == EnemyState.Dead) return;
        Debug.Log("[StoneSentinel] OnBoulderPickup fired.");

        if (heldBoulder == null)
        {
            Debug.LogWarning("[StoneSentinel] heldBoulder is NULL. Assign it in Inspector.");
            return;
        }

        // Match the held rock to the enlarged thrown prefab; cache its authored
        // scale once so repeated attack cycles NEVER keep enlarging the prop.
        if (!heldBoulderScaleCached)
        {
            originalHeldBoulderScale = heldBoulder.transform.localScale;
            heldBoulderScaleCached = true;
        }
        heldBoulder.transform.localScale = originalHeldBoulderScale * boulderSizeMultiplier;
        heldBoulder.SetActive(true);
        Debug.Log("[StoneSentinel] HeldBoulder activated.");
    }

    public void OnBoulderThrow()
    {
        if (vocabularyPaused || currentState == EnemyState.Dead) return;
        Debug.Log("[StoneSentinel] OnBoulderThrow fired. Hiding HeldBoulder.");

        if (heldBoulder != null)
            heldBoulder.SetActive(false);

        if (boulderPrefab == null || boulderSpawnPoint == null || player == null)
            return;

        GameObject boulder = Instantiate(
            boulderPrefab,
            boulderSpawnPoint.position,
            Quaternion.identity
        );

        // A physical throw, not a gravity-disabled straight-line homing shot.
        // Aim at the CENTER of the player's actual body collider: in Stage 1
        // the models/rock are much larger than one world unit, and aiming
        // only +1 above ground makes the large stone hit the floor too early.
        // Sample only once at the animation event (no mid-flight homing).
        CharacterController playerCharacter = player.GetComponentInChildren<CharacterController>();
        Collider playerCollider = playerCharacter != null
            ? (Collider)playerCharacter
            : player.GetComponentInChildren<Collider>();
        Vector3 targetPosition = playerCollider != null
            ? playerCollider.bounds.center
            : player.position + Vector3.up * 1.0f;
        Vector3 launchVelocity = CalculateBoulderThrowVelocity(
            boulderSpawnPoint.position, targetPosition);
        boulder.transform.localScale *= boulderSizeMultiplier;

        BoulderProjectile projectile = boulder.GetComponent<BoulderProjectile>();

        if (projectile != null)
        {
            projectile.Launch(
                launchVelocity, boulderDamage, boulderLifeTime,
                boulderGravityMultiplier, boulderTumbleSpeed,
                GetComponentsInChildren<Collider>(true));
        }
        else
        {
            Rigidbody rb = boulder.GetComponent<Rigidbody>();
            if (rb != null && boulder.GetComponentInChildren<Collider>() != null)
            {
                // Fallback still uses exactly the same single-hit
                // BoulderProjectile authority, rather than silently making
                // a gravity-only, non-damaging boulder.
                projectile = boulder.AddComponent<BoulderProjectile>();
                projectile.Launch(
                    launchVelocity, boulderDamage, boulderLifeTime,
                    boulderGravityMultiplier, boulderTumbleSpeed,
                    GetComponentsInChildren<Collider>(true));
            }
            else
                Destroy(boulder);
        }

        Debug.Log(
            $"[StoneSentinel] Heavy tumbling boulder thrown at " +
            $"{launchVelocity.magnitude:0.0} units/s (gravity x{boulderGravityMultiplier:0.0}).");
    }

    public void OnBoulderThrowFinished()
    {
        if (vocabularyPaused) return;
        Debug.Log("[StoneSentinel] BoulderThrow finished -> Recover");

        boulderCooldownTimer = boulderCooldown;
        EnterRecover();
    }
    private Vector3 CalculateBoulderThrowVelocity(Vector3 start, Vector3 target)
    {
        Vector3 travel = target - start;
        Vector3 horizontal = Vector3.ProjectOnPlane(travel, Vector3.up);
        float distance = horizontal.magnitude;

        // The level already authors boulderSpeed (about 102 units/sec in
        // Stage 1). Retain that world scale and make it ~25% faster instead
        // of replacing the stage's speed with a much slower fixed value.
        float horizontalSpeed = Mathf.Max(10f, boulderSpeed * boulderSpeedMultiplier);
        // boulderArcAngle now meaningfully controls the minimum loft time:
        // even at short range, the giant rock has a visible curved throw.
        float minLoftSeconds = Mathf.Lerp(
            0.42f, 0.78f, Mathf.InverseLerp(15f, 70f, boulderArcAngle));
        float flightSeconds = Mathf.Max(distance / horizontalSpeed, minLoftSeconds);
        float effectiveGravity = Mathf.Max(0.1f,
            Mathf.Abs(Physics.gravity.y) * boulderGravityMultiplier);

        // Solve the ballistic intercept under the SAME gravity used by the
        // actual Rigidbody, including vertical difference to the player.
        // No mid-flight homing or forced straight-line trajectory.
        float initialY = (travel.y + 0.5f * effectiveGravity
            * flightSeconds * flightSeconds) / flightSeconds;
        return horizontal / flightSeconds + Vector3.up * initialY;
    }

    private void HandleJumpAttack()
    {
        agent.isStopped = true;
        agent.ResetPath();

        if (!jumpAttackStarted)
        {
            jumpAttackStarted = true;
            jumpLeapStarted = false;

            if (animator != null)
                animator.SetTrigger("JumpAttack");

            Debug.Log("[StoneSentinel] Triggered JumpAttack animation.");
        }
    }

    public void OnJumpAttackLeap()
    {
        if (vocabularyPaused) return;
        if (jumpLeapStarted) return;
        if (player == null) return;

        jumpLeapStarted = true;

        Vector3 directionToPlayer = player.position - transform.position;
        directionToPlayer.y = 0f;

        if (directionToPlayer.sqrMagnitude < 0.01f)
            return;

        directionToPlayer.Normalize();

        Vector3 targetPosition = player.position - directionToPlayer * jumpLandingOffset;

        if (jumpMoveCoroutine != null)
            StopCoroutine(jumpMoveCoroutine);

        jumpMoveCoroutine = StartCoroutine(JumpMoveRoutine(targetPosition));

        Debug.Log("[StoneSentinel] JumpAttack leap movement started.");
    }
    private IEnumerator JumpMoveRoutine(Vector3 targetPosition)
    {
        Vector3 startPosition = transform.position;

        // Keep landing on the same ground height as the start for now
        targetPosition.y = startPosition.y;

        float elapsed = 0f;

        while (elapsed < jumpMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / jumpMoveDuration);

            // Smooth horizontal travel
            Vector3 flatPosition = Vector3.Lerp(startPosition, targetPosition, t);

            // Smooth parabolic arc
            float arc = 4f * jumpHeight * t * (1f - t);

            flatPosition.y = startPosition.y + arc;

            transform.position = flatPosition;

            if (agent != null && agent.enabled)
                agent.nextPosition = transform.position;

            yield return null;
        }

        transform.position = targetPosition;

        if (agent != null && agent.enabled)
            agent.nextPosition = transform.position;

        jumpMoveCoroutine = null;
    }

    public void OnJumpAttackFinished()
    {
        if (vocabularyPaused) return;
        Debug.Log("[StoneSentinel] JumpAttack finished -> Recover");
        EnterRecover();
    }

    public void OnJumpAttackImpact()
    {
        if (vocabularyPaused) return;
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= jumpAttackDamageRadius)
        {
            DealDamageToPlayer(jumpAttackDamage);
            Debug.Log("[StoneSentinel] JumpAttack hit player.");
        }
        else
        {
            Debug.Log("[StoneSentinel] JumpAttack missed.");
        }
    }

   public void OnBeingHitFinished()
{
    if (currentState == EnemyState.Dead) return;

    closeAttackStarted = false;
    boulderThrowStarted = false;
    jumpAttackStarted = false;
    jumpLeapStarted = false;

    BeingHitFinished?.Invoke();

    if (vocabularyPaused)
    {
        currentState = EnemyState.Counterspell;
        Debug.Log("[StoneSentinel] BeingHit finished while Counterspell remains active.");
        return;
    }

    Debug.Log("[StoneSentinel] BeingHit finished -> Approach");
    currentState = EnemyState.Approach;
}

public void TriggerBeingHit()
{
    if (!battleStarted) return;
    if (currentState == EnemyState.Dead) return;
    if (vocabularyPaused) return;

    EnterBeingHit();
}

public void TriggerCounterspellBreakReaction()
{
    if (!battleStarted) return;
    if (currentState == EnemyState.Dead) return;
    if (!vocabularyPaused) return;

    EnterBeingHit();
}

private void EnterBeingHit()
{
    currentState = EnemyState.BeingHit;

    if (agent != null && agent.enabled)
    {
        agent.isStopped = true;
        agent.ResetPath();
    }

    if (recoverCoroutine != null)
    {
        StopCoroutine(recoverCoroutine);
        recoverCoroutine = null;
    }

    if (jumpMoveCoroutine != null)
{
    StopCoroutine(jumpMoveCoroutine);
    jumpMoveCoroutine = null;

    SnapToGround();
}

    if (heldBoulder != null)
        heldBoulder.SetActive(false);

    closeAttackStarted = false;
    boulderThrowStarted = false;
    jumpAttackStarted = false;
    jumpLeapStarted = false;

    if (animator != null)
    {
        animator.ResetTrigger("CloseAttack");
        animator.ResetTrigger("BoulderThrow");
        animator.ResetTrigger("JumpAttack");
        animator.SetTrigger("BeingHit");
    }

    Debug.Log("[StoneSentinel] Entered BeingHit state.");
}

private void SnapToGround()
{
    Vector3 rayStart = transform.position + Vector3.up * 2f;

    if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 15f))
    {
        Vector3 groundPosition = hit.point;

        if (NavMesh.SamplePosition(groundPosition, out NavMeshHit navHit, 3f, NavMesh.AllAreas))
        {
            transform.position = navHit.position;

            if (agent != null && agent.enabled)
            {
                agent.Warp(navHit.position);
                agent.nextPosition = navHit.position;
            }

            Debug.Log("[StoneSentinel] Snapped to NavMesh ground after interrupted jump.");
        }
        else
        {
            transform.position = groundPosition;

            if (agent != null && agent.enabled)
                agent.nextPosition = transform.position;

            Debug.LogWarning("[StoneSentinel] Snapped to ground, but no nearby NavMesh found.");
        }
    }
    else
    {
        Debug.LogWarning("[StoneSentinel] SnapToGround failed. No ground detected below.");
    }
    
}

public void SetVocabularyPaused(bool paused)
{
    if (currentState == EnemyState.Dead)
        return;

    if (vocabularyPaused == paused)
        return;

    vocabularyPaused = paused;

    if (paused)
    {
        if (recoverCoroutine != null)
        {
            StopCoroutine(recoverCoroutine);
            recoverCoroutine = null;
        }

        if (jumpMoveCoroutine != null)
        {
            StopCoroutine(jumpMoveCoroutine);
            jumpMoveCoroutine = null;
            SnapToGround();
        }

        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        if (heldBoulder != null)
            heldBoulder.SetActive(false);

        closeAttackStarted = false;
        boulderThrowStarted = false;
        jumpAttackStarted = false;
        jumpLeapStarted = false;
        currentState = EnemyState.Counterspell;

        if (animator != null)
        {
            animator.ResetTrigger("CloseAttack");
            animator.ResetTrigger("BoulderThrow");
            animator.ResetTrigger("JumpAttack");
            animator.ResetTrigger("BeingHit");
        }

        Debug.Log("[StoneSentinel] Combat AI paused for Counterspell.");
        return;
    }

    closeAttackStarted = false;
    boulderThrowStarted = false;
    jumpAttackStarted = false;
    jumpLeapStarted = false;
    currentState = EnemyState.Approach;

    if (battleStarted && agent != null && agent.enabled)
    {
        agent.isStopped = false;
        agent.ResetPath();
    }

    Debug.Log("[StoneSentinel] Counterspell ended - combat AI resumed.");
}

public void TriggerDeath()
{
    if (currentState == EnemyState.Dead) return;

    EnterDead();
}

private void EnterDead()
{
    currentState = EnemyState.Dead;
    battleStarted = false;
    vocabularyPaused = false;

    if (agent != null && agent.enabled)
    {
        agent.isStopped = true;
        agent.ResetPath();
        agent.enabled = false;
    }

    if (recoverCoroutine != null)
    {
        StopCoroutine(recoverCoroutine);
        recoverCoroutine = null;
    }

    if (jumpMoveCoroutine != null)
    {
        StopCoroutine(jumpMoveCoroutine);
        jumpMoveCoroutine = null;
    }

    if (heldBoulder != null)
        heldBoulder.SetActive(false);

    closeAttackStarted = false;
    boulderThrowStarted = false;
    jumpAttackStarted = false;
    jumpLeapStarted = false;

    if (animator != null)
    {
        animator.ResetTrigger("CloseAttack");
        animator.ResetTrigger("BoulderThrow");
        animator.ResetTrigger("JumpAttack");
        animator.ResetTrigger("BeingHit");
        animator.SetTrigger("Dead");
    }

    Debug.Log("[StoneSentinel] Entered Dead state.");
}

public void OnDeathFinished()
{
    Debug.Log("[StoneSentinel] Death animation finished.");

    TryDropPotionReward();

    if (deathReceiver != null && !string.IsNullOrEmpty(deathMessage))
    {
        deathReceiver.SendMessage(deathMessage, SendMessageOptions.DontRequireReceiver);
        Debug.Log($"[StoneSentinel] Sent death message: {deathMessage}");
    }
    else
    {
        Debug.LogWarning("[StoneSentinel] No death receiver assigned. Progression will not be notified.");
    }

    gameObject.SetActive(false);
}

private void TryDropPotionReward()
{
    if (!dropPotionOnDeath || potionDropResolved)
        return;

    potionDropResolved = true;

    PotionDropper.TryDropRandomPotion(
        potionDropLogSource,
        potionDropChance,
        potionDropMinCount,
        potionDropMaxCount,
        potionDropAmountPerDrop,
        canDropHealthPotion,
        canDropPurifyPotion,
        canDropPowerUpPotion
    );
}

    private void EnterRecover()
    {
        if (vocabularyPaused) return;
        currentState = EnemyState.Recover;
        agent.isStopped = true;
        agent.ResetPath();

        if (recoverCoroutine != null)
            StopCoroutine(recoverCoroutine);

        recoverCoroutine = StartCoroutine(RecoverRoutine());
    }

    private IEnumerator RecoverRoutine()
    {
        yield return new WaitForSeconds(recoverTime);

        recoverCoroutine = null;
        closeAttackStarted = false;
        boulderThrowStarted = false;
        jumpAttackStarted = false;

        currentState = EnemyState.Approach;
    }

    private void DealDamageToPlayer(float damage)
    {
        var damageable = player.root.GetComponentInChildren<IDamageable>();

        if (damageable != null)
            damageable.ApplyDamage(damage);
        else
            player.SendMessage("ApplyDamage", damage, SendMessageOptions.DontRequireReceiver);
    }

    public void StartBattle()
    {
        if (battleStarted) return;

        battleStarted = true;
        currentState = EnemyState.Approach;
        potionDropResolved = false;

        if (agent != null)
        {
            agent.isStopped = false;
            agent.ResetPath();
        }

        Debug.Log("[StoneSentinel] Battle started � entering Approach.");
    }

    private void FaceTarget(Vector3 targetPos, float turnSpeed = 360f)
    {
        Vector3 dir = targetPos - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRot,
                turnSpeed * Time.deltaTime
            );
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, closeAttackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, jumpAttackRange);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, jumpAttackDamageRadius);
    }
}
