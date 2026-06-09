using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class HollowKnightController : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public NavMeshAgent agent;
    public Animator animator;

    [Header("Movement Speeds")]
    public float walkSpeed = 12f;
    public float runSpeed = 27.04f;

    [Header("Ranges")]
    public float runRange = 80f;
    public float stoppingDistance = 25f;

    [Header("Rotation")]
    public float rotationSpeed = 10f;

    [Header("State")]
    public bool battleStarted = false;

    [Header("Weapon")]
    public BossSwordDamage swordDamage;

    [Header("Attack Settings")]
    public float swordSlashRange = 25f;
    public float swordSlashCooldown = 2f;

    [Header("Recovery")]
    public float recoverTime = 0.8f;

    [Header("Charge Attack Settings")]
    public float chargeMinRange = 30f;
    public float chargeMaxRange = 75f;
    public float chargeSpeed = 55f;
    public float chargeDuration = 0.7f;
    public float chargeCooldown = 4f;

    [Header("Hit Reaction")]
    public float hitRecoverTime = 0.5f;

    [Header("Sword Drop")]
    public Transform swordPivot;
    public Rigidbody swordRigidbody;
    public Collider swordPhysicalCollider;
    public Collider swordHitboxCollider;

    [Header("Stage Boss Controller")]
    public Stage1BossController stageBossController;    

private bool swordDropped = false;

    private bool isBeingHit = false;

private bool isCharging = false;

private bool isDead = false;
private float chargeCooldownTimer = 0f;
private Vector3 chargeTargetPosition;

    private bool isAttacking = false;
    private bool isRecovering = false;

    private float swordSlashCooldownTimer = 0f;

    private float speedLogTimer = 2f; // Timer for console logging
    private bool decidedToCloseIn = false;

    private Coroutine recoverCoroutine;

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void Start()
    {
        SetupAgent();
        SetAnimatorSpeed(0f);

        if (swordDamage != null)
            swordDamage.DisableDamage();
    }

   private void Update()
{
    speedLogTimer -= Time.deltaTime;
    if (speedLogTimer <= 0f)
    {
        float currentSpeed = 0f;
        
        // Check standard movement speed
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            currentSpeed = agent.velocity.magnitude;
        }

        // If charging, agent.velocity might be bypassed by agent.Move(), so we show charge speed
        if (isCharging)
        {
            currentSpeed = chargeSpeed;
        }

        // Debug.Log($"[HollowKnight Debug] Current Speed: {currentSpeed:F2}");
        speedLogTimer = 2f; // Reset the timer
    }
    if (swordSlashCooldownTimer > 0f)
        swordSlashCooldownTimer -= Time.deltaTime;

    if (chargeCooldownTimer > 0f)
        chargeCooldownTimer -= Time.deltaTime;

    if (isDead)
    {
    StopMovement();
    return;
    }

    if (!battleStarted)
    {
        SetAnimatorSpeed(0f);
        return;
    }

   if (isCharging)
{
    HandleChargeMovement();
    return;
}

if (isAttacking || isRecovering || isBeingHit)
{
    StopMovement();
    FacePlayer();
    return;
}

    DecideAction();
}
    private void SetupAgent()
    {
        if (agent == null)
        {
            Debug.LogWarning("[HollowKnight] NavMeshAgent missing.");
            return;
        }

        agent.speed = walkSpeed;
        agent.stoppingDistance = stoppingDistance;
        agent.isStopped = true;
    }

    public void StartBattle()
    {
        if (player == null)
        {
            Debug.LogWarning("[HollowKnight] Cannot start battle. Player reference missing.");
            return;
        }

        if (agent == null)
        {
            Debug.LogWarning("[HollowKnight] Cannot start battle. NavMeshAgent missing.");
            return;
        }

        if (!agent.enabled)
        {
            Debug.LogWarning("[HollowKnight] Cannot start battle. NavMeshAgent disabled.");
            return;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning("[HollowKnight] Cannot start battle. Agent is not on NavMesh.");
            return;
        }

        battleStarted = true;

        isAttacking = false;
        isRecovering = false;

        if (recoverCoroutine != null)
        {
            StopCoroutine(recoverCoroutine);
            recoverCoroutine = null;
        }

        if (swordDamage != null)
            swordDamage.DisableDamage();

        agent.isStopped = false;
        agent.stoppingDistance = stoppingDistance;

        Debug.Log("[HollowKnight] Battle started.");
    }

    private void DecideAction()
    {
        if (player == null)
        {
            Debug.LogWarning("[HollowKnight] Player reference missing.");
            StopMovement();
            return;
        }

        if (agent == null || !agent.enabled)
        {
            Debug.LogWarning("[HollowKnight] Agent missing or disabled.");
            return;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning("[HollowKnight] Agent is not on NavMesh.");
            return;
        }

       float distanceToPlayer = Vector3.Distance(transform.position, player.position);

if (distanceToPlayer <= swordSlashRange)
{
    StopMovement();
    FacePlayer();

    if (swordSlashCooldownTimer <= 0f)
    {
        TriggerSwordSlash();
    }

    return;
}

if (!decidedToCloseIn &&
    distanceToPlayer >= chargeMinRange &&
    distanceToPlayer <= chargeMaxRange &&
    chargeCooldownTimer <= 0f)
{
    float chargeRoll = Random.value;

    if (chargeRoll <= 0.6f)
{
    float variantRoll = Random.value;

    if (variantRoll <= 0.5f)
    {
        TriggerChargeAttack();
        Debug.Log("[HollowKnight] Charge variant: SlideAttack.");
    }
    else
    {
        TriggerChargeAttack2();
        Debug.Log("[HollowKnight] Charge variant: ChargeAttack2.");
    }

    return;
}
}

ChasePlayer(distanceToPlayer);
    }

    private void ChasePlayer(float distanceToPlayer)
    {
        if (agent == null || !agent.enabled)
        {
            Debug.LogWarning("[HollowKnight] Agent missing or disabled.");
            return;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning("[HollowKnight] Agent is not on NavMesh.");
            return;
        }

        if (distanceToPlayer > runRange)
        {
            agent.speed = walkSpeed;
        }
        else
        {
            agent.speed = runSpeed;
        }

        agent.isStopped = false;
        agent.SetDestination(player.position);

        SetAnimatorSpeed(agent.velocity.magnitude);
    }

    private void TriggerSwordSlash()
    {
        if (isAttacking || isRecovering) return;

         decidedToCloseIn = false;

        isAttacking = true;
        swordSlashCooldownTimer = swordSlashCooldown;

        StopMovement();
        FacePlayer();

        if (swordDamage != null)
            swordDamage.DisableDamage();

        if (animator != null)
        {
            animator.SetTrigger("SwordSlash");
            Debug.Log("[HollowKnight] SwordSlash triggered.");
        }
        else
        {
            Debug.LogWarning("[HollowKnight] Animator missing. Cannot trigger SwordSlash.");
            isAttacking = false;
        }
    }

    public void OnSwordSlashFinished()
    {
        Debug.Log("[HollowKnight] SwordSlash finished. Entering recover.");

        isAttacking = false;

        if (swordDamage != null)
            swordDamage.DisableDamage();

        if (recoverCoroutine != null)
            StopCoroutine(recoverCoroutine);

        recoverCoroutine = StartCoroutine(RecoverRoutine());
    }

    private void TriggerChargeAttack()
{
    if (isAttacking || isRecovering || isCharging) return;
    if (player == null) return;

    isCharging = true;
    isAttacking = true;
    chargeCooldownTimer = chargeCooldown;

    chargeTargetPosition = player.position;
    chargeTargetPosition.y = transform.position.y;

    StopMovement();
    FacePlayer();

    if (swordDamage != null)
        swordDamage.DisableDamage();

    if (animator != null)
    {
        animator.SetTrigger("ChargeAttack");
        Debug.Log("[HollowKnight] ChargeAttack triggered.");
    }
    else
    {
        Debug.LogWarning("[HollowKnight] Animator missing. Cannot trigger ChargeAttack.");
        isCharging = false;
        isAttacking = false;
    }
}

private void HandleChargeMovement()
{
    if (agent == null || !agent.enabled || !agent.isOnNavMesh)
        return;

    Vector3 direction = chargeTargetPosition - transform.position;
    direction.y = 0f;

    if (direction.sqrMagnitude <= 0.5f)
    {
        return;
    }

    Vector3 moveDirection = direction.normalized;
    agent.Move(moveDirection * chargeSpeed * Time.deltaTime);

    if (moveDirection.sqrMagnitude > 0.001f)
    {
        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    SetAnimatorSpeed(0f);
}

public void OnChargeAttackFinished()
{
    Debug.Log("[HollowKnight] ChargeAttack finished. Entering recover.");

    isCharging = false;
    isAttacking = false;

    if (swordDamage != null)
        swordDamage.DisableDamage();

    if (recoverCoroutine != null)
        StopCoroutine(recoverCoroutine);

    recoverCoroutine = StartCoroutine(RecoverRoutine());
}

private void TriggerChargeAttack2()
{
    if (isAttacking || isRecovering || isCharging) return;
    if (player == null) return;

    isCharging = true;
    isAttacking = true;
    chargeCooldownTimer = chargeCooldown;

    chargeTargetPosition = player.position;
    chargeTargetPosition.y = transform.position.y;

    StopMovement();
    FacePlayer();

    if (swordDamage != null)
        swordDamage.DisableDamage();

    Debug.Log("[HollowKnight] ChargeAttack2 triggered. (Animation call placeholder)");

    animator.SetTrigger("CloseAttack2"); 
}

public void OnChargeAttack2Finished()
{
    Debug.Log("[HollowKnight] ChargeAttack2 finished. Entering recover.");

    isCharging = false;
    isAttacking = false;

    if (swordDamage != null)
        swordDamage.DisableDamage();

    if (recoverCoroutine != null)
        StopCoroutine(recoverCoroutine);

    recoverCoroutine = StartCoroutine(RecoverRoutine());
}

    public void TriggerBeingHit()
{
    if (!battleStarted) return;
    if (isBeingHit) return;

    Debug.Log("[HollowKnight] BeingHit triggered.");

    isBeingHit = true;
    isAttacking = false;
    isCharging = false;

    StopMovement();

    if (swordDamage != null)
        swordDamage.DisableDamage();

    if (recoverCoroutine != null)
    {
        StopCoroutine(recoverCoroutine);
        recoverCoroutine = null;
    }

    if (animator != null)
    {
        animator.ResetTrigger("SwordSlash");
        animator.ResetTrigger("ChargeAttack");
        animator.SetTrigger("BeingHit");
    }
}

public void OnBeingHitFinished()
{
    Debug.Log("[HollowKnight] BeingHit finished.");

    isBeingHit = false;

    if (recoverCoroutine != null)
        StopCoroutine(recoverCoroutine);

    recoverCoroutine = StartCoroutine(HitRecoverRoutine());
}

    public void TriggerDeath()
{
    if (isDead) return;

    Debug.Log("[HollowKnight] Death triggered.");

    isDead = true;
    battleStarted = false;

    isAttacking = false;
    isCharging = false;
    isRecovering = false;
    isBeingHit = false;

    if (recoverCoroutine != null)
    {
        StopCoroutine(recoverCoroutine);
        recoverCoroutine = null;
    }

    StopMovement();

    if (swordDamage != null)
        swordDamage.DisableDamage();

    if (agent != null && agent.enabled && agent.isOnNavMesh)
    {
        agent.isStopped = true;
        agent.ResetPath();
        agent.enabled = false;
    }

    Rigidbody rb = GetComponent<Rigidbody>();

    if (rb != null)
    {
        if (!rb.isKinematic)
        {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        }

    rb.useGravity = false;
    rb.isKinematic = true;
    }

    if (animator != null)
    {
        animator.ResetTrigger("SwordSlash");
        animator.ResetTrigger("ChargeAttack");
        animator.ResetTrigger("BeingHit");
        animator.SetTrigger("Dead");
    }
    else
    {
        Debug.LogWarning("[HollowKnight] Animator missing. Cannot play death animation.");
    }
}

public void OnDeathFinished()
{
    Debug.Log("[HollowKnight] Death animation finished. Corpse remains in scene.");

    if (swordDamage != null)
        swordDamage.DisableDamage();

    SetAnimatorSpeed(0f);

    if (stageBossController != null)
{
    stageBossController.OnHollowKnightDefeated();
}
else
{
    Debug.LogWarning("[HollowKnight] StageBossController reference missing.");
}

    // Do NOT destroy or deactivate this GameObject.
    // Hollow Knight must remain as a corpse for fragment pickup and sword reward later.
}

private IEnumerator HitRecoverRoutine()
{
    isRecovering = true;

    StopMovement();
    FacePlayer();

    yield return new WaitForSeconds(hitRecoverTime);

    isRecovering = false;
    recoverCoroutine = null;

    Debug.Log("[HollowKnight] Hit recover finished.");
}

    private IEnumerator RecoverRoutine()
    {
        isRecovering = true;

        StopMovement();
        FacePlayer();

        Debug.Log("[HollowKnight] Recover started.");

        yield return new WaitForSeconds(recoverTime);

        isRecovering = false;
        recoverCoroutine = null;

        Debug.Log("[HollowKnight] Recover finished.");
    }

    public void EnableSwordDamage()
    {
        if (swordDamage != null)
        {
            swordDamage.EnableDamage();
        }
        else
        {
            Debug.LogWarning("[HollowKnight] SwordDamage reference missing. Cannot enable sword damage.");
        }
    }

    public void DisableSwordDamage()
    {
        if (swordDamage != null)
        {
            swordDamage.DisableDamage();
        }
        else
        {
            Debug.LogWarning("[HollowKnight] SwordDamage reference missing. Cannot disable sword damage.");
        }
    }

    public void DropSword()
{
    if (swordDropped) return;

    swordDropped = true;

    Debug.Log("[HollowKnight] Sword dropped.");

    if (swordDamage != null)
        swordDamage.DisableDamage();

    if (swordHitboxCollider != null)
        swordHitboxCollider.enabled = false;

    if (swordPivot != null)
    {
        swordPivot.SetParent(null, true);
    }
    else
    {
        Debug.LogWarning("[HollowKnight] SwordPivot reference missing. Cannot detach sword.");
        return;
    }

    if (swordPhysicalCollider != null)
    {
        swordPhysicalCollider.enabled = true;
        swordPhysicalCollider.isTrigger = false;
    }

    if (swordRigidbody != null)
    {
        swordRigidbody.isKinematic = false;
        swordRigidbody.useGravity = true;

        swordRigidbody.linearVelocity = Vector3.zero;
        swordRigidbody.angularVelocity = Vector3.zero;

        swordRigidbody.AddForce(transform.forward * 2f + Vector3.up * 1f, ForceMode.Impulse);
        swordRigidbody.AddTorque(transform.right * 2f, ForceMode.Impulse);
    }
    else
    {
        Debug.LogWarning("[HollowKnight] SwordRigidbody reference missing. Sword will detach but not fall with physics.");
    }
}

    private void StopMovement()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        SetAnimatorSpeed(0f);
    }

    private void FacePlayer()
    {
        if (player == null) return;

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private void SetAnimatorSpeed(float speed)
    {
        if (animator != null)
        {
            animator.SetFloat("Speed", speed);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = transform.position;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, swordSlashRange);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(center, chargeMinRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(center, chargeMaxRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, runRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(center, stoppingDistance);
    }
}