using UnityEngine;
using UnityEngine.AI;
using System.Collections;

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
        JumpAttack
    }

    [Header("References")]
    public Transform player;
    public NavMeshAgent agent;
    public Animator animator;

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
            agent.speed = walkSpeed;
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
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= closeAttackRange + 0.5f)
            DealDamageToPlayer(closeAttackDamage);
    }

    public void OnCloseAttackFinished()
    {
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
        Debug.Log("[StoneSentinel] OnBoulderPickup fired.");

        if (heldBoulder == null)
        {
            Debug.LogWarning("[StoneSentinel] heldBoulder is NULL. Assign it in Inspector.");
            return;
        }

        heldBoulder.SetActive(true);
        Debug.Log("[StoneSentinel] HeldBoulder activated.");
    }

    public void OnBoulderThrow()
    {
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

        Vector3 targetPosition = player.position + Vector3.up * 1.0f; 
        Vector3 direction = (targetPosition - boulderSpawnPoint.position).normalized;

        BoulderProjectile projectile = boulder.GetComponent<BoulderProjectile>();

        if (projectile != null)
        {
            projectile.Launch(direction * boulderSpeed, boulderDamage, boulderLifeTime);
        }
        else
        {
            Rigidbody rb = boulder.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.useGravity = false;
                rb.linearVelocity = direction * boulderSpeed;
            }
        }

        Debug.Log("[StoneSentinel] Boulder thrown in linear trajectory.");
    }

    public void OnBoulderThrowFinished()
    {
        Debug.Log("[StoneSentinel] BoulderThrow finished -> Recover");

        boulderCooldownTimer = boulderCooldown;
        EnterRecover();
    }
    private Vector3 CalculateArcVelocity(Vector3 start, Vector3 target, float angleDegrees)
    {
        float gravity = Mathf.Abs(Physics.gravity.y);

        float angle = angleDegrees * Mathf.Deg2Rad;

        Vector3 direction = target - start;
        Vector3 horizontalDirection = new Vector3(direction.x, 0f, direction.z);

        float distance = horizontalDirection.magnitude;
        float heightDifference = direction.y;

        float speedSquared = gravity * distance * distance /
            (2f * Mathf.Cos(angle) * Mathf.Cos(angle) *
            (distance * Mathf.Tan(angle) - heightDifference));

        if (speedSquared <= 0f || float.IsNaN(speedSquared))
        {
            // fallback kung weird ang angle/distance
            return horizontalDirection.normalized * boulderSpeed + Vector3.up * 6f;
        }

        float speed = Mathf.Sqrt(speedSquared);

        Vector3 velocity =
            horizontalDirection.normalized * speed * Mathf.Cos(angle) +
            Vector3.up * speed * Mathf.Sin(angle);

        return velocity;
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
        Debug.Log("[StoneSentinel] JumpAttack finished -> Recover");
        EnterRecover();
    }

    public void OnJumpAttackImpact()
    {
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

    Debug.Log("[StoneSentinel] BeingHit finished -> Approach");

    closeAttackStarted = false;
    boulderThrowStarted = false;
    jumpAttackStarted = false;
    jumpLeapStarted = false;

    currentState = EnemyState.Approach;
}

    public void TriggerBeingHit()
{
    if (!battleStarted) return;
    if (currentState == EnemyState.Dead) return;

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

public void TriggerDeath()
{
    if (currentState == EnemyState.Dead) return;

    EnterDead();
}

private void EnterDead()
{
    currentState = EnemyState.Dead;
    battleStarted = false;

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
