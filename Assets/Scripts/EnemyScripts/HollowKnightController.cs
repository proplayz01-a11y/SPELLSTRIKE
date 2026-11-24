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
    [SerializeField, Min(0.05f)] private float accelerationTime = 0.5f;

    [Header("Movement safety - readable at scene scale")]
    [SerializeField, Min(1f)] private float maximumWalkSpeed = 24f;
    [SerializeField, Min(1f)] private float maximumRunSpeed = 42f;

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
    public float chargeDuration = 1.10f;
    public float chargeCooldown = 4f;
    [SerializeField, Min(0f)] private float chargeWindup = 0.23f;
    [SerializeField, Min(0.02f)] private float chargeAccelerationWindow = 0.16f;
    [SerializeField, Min(0.02f)] private float chargeBrakingWindow = 0.22f;
    [SerializeField, Min(0f)] private float chargeStopDistance = 24f;
    [Header("Relentless Knight - sprint combo")]
    [SerializeField, Min(0f)] private float dashExecutionStopDistance = 20f;
    private const float SprintComboOpeningDuration = 17f / 30f;
    private const float SprintComboBurstStart = 29f / 30f;
    private const float SprintComboBurstEnd = 33f / 30f;
    [Header("Revamped Dash Execution")]
    [SerializeField, Min(0f)] private float heavyExecutionFirstLungeMax = 8f;
    [SerializeField, Min(0f)] private float heavyExecutionFinalLungeMax = 10f;
    private const float HeavyOpeningDuration = 21f / 30f;
    private const float HeavyFirstLungeStart = 44f / 30f;
    private const float HeavyFirstLungeEnd = 49f / 30f;
    private const float HeavyFinalLungeStart = 64f / 30f;
    private const float HeavyFinalLungeEnd = 73f / 30f;
    private bool isHeavyExecution;
    private float heavyFinalLungeDistance;
    private float heavyFinalLungeProgress;
    [SerializeField, Min(0f)] private float sprintComboBurstMaxDistance = 12f;
    private float sprintComboBurstDistance;
    private float sprintComboBurstProgress;
    private bool dashExecutionEntered;
    private float dashExecutionDistance;
    private float dashExecutionProgress;
    private float dashExecutionWait;

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
private Vector3 chargeDirection;
private float chargeElapsed;
private float chargeTravelled;
private float chargeLastFrameSpeed;

public bool IsCharging => isCharging;
public bool IsDashExecution => isCharging && isHeavyExecution;
public float ChargeLastFrameSpeed => chargeLastFrameSpeed;
public float ChargeTravelled => chargeTravelled;

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
    // Both mocap charges are synchronized to their visible opening
    // frames in LateUpdate, after Animator evaluation.
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

        EnemyNavMovement.ApplySpeed(agent,
            Mathf.Min(walkSpeed, maximumWalkSpeed), accelerationTime);
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
    // The full sprint combo has an opening sprint and a second burst.
    // Select only an attack whose bounded approach can reach the target.
    float sprintReach = dashExecutionStopDistance +
        Mathf.Min(chargeSpeed, 55f) * SprintComboOpeningDuration / 1.5f +
        sprintComboBurstMaxDistance;
    float heavyReach = dashExecutionStopDistance +
        Mathf.Min(chargeSpeed, 55f) * HeavyOpeningDuration / 1.5f +
        heavyExecutionFirstLungeMax + heavyExecutionFinalLungeMax;
    bool canSprint = distanceToPlayer <= sprintReach + 5f;
    bool canHeavy = distanceToPlayer <= heavyReach + 5f;
    if (canHeavy && (!canSprint || Random.value < 0.5f))
    {
        TriggerChargeAttack2();
        return;
    }
    if (canSprint)
    {
        TriggerChargeAttack();
        Debug.Log("[HollowKnight] Charge variant: SprintCombo.");
        return;
    }
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
            EnemyNavMovement.ApplySpeed(agent,
                Mathf.Min(walkSpeed, maximumWalkSpeed), accelerationTime);
        }
        else
        {
            EnemyNavMovement.ApplySpeed(agent,
                Mathf.Min(runSpeed, maximumRunSpeed), accelerationTime);
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
        BeginAnimatedCharge(false);
    }

    private void TriggerChargeAttack2()
    {
        BeginAnimatedCharge(true);
    }

    private void BeginAnimatedCharge(bool heavy)
{
    if (isAttacking || isRecovering || isCharging) return;
    if (player == null) return;

    isCharging = true;
    isAttacking = true;
    isHeavyExecution = heavy;
    dashExecutionEntered = false;
    dashExecutionProgress = 0f;
    dashExecutionWait = 0f;
    chargeCooldownTimer = chargeCooldown;

    chargeTargetPosition = player.position;
    chargeTargetPosition.y = transform.position.y;
    chargeDirection = chargeTargetPosition - transform.position;
    chargeDirection.y = 0f;
    chargeDirection = chargeDirection.sqrMagnitude > 0.001f
        ? chargeDirection.normalized : transform.forward;
    chargeElapsed = 0f;
    chargeTravelled = 0f;
    chargeLastFrameSpeed = 0f;

    StopMovement();
    // Reserve some of the approach for the actual frame 29-33 lunge.
    // Otherwise the opening sprint consumes all available distance and
    // the second dash cannot move at typical engagement ranges.
    float approachRoom = Mathf.Max(0f,
        Vector3.Distance(chargeTargetPosition, transform.position) - dashExecutionStopDistance);
    float burstCap = heavy ? heavyExecutionFirstLungeMax + heavyExecutionFinalLungeMax : sprintComboBurstMaxDistance;
    float openingDuration = heavy ? HeavyOpeningDuration : SprintComboOpeningDuration;
    float reservedBurst = Mathf.Min(burstCap, approachRoom * 0.4f);
    dashExecutionDistance = Mathf.Clamp(approachRoom - reservedBurst,
        0f, Mathf.Min(chargeSpeed, 55f) * openingDuration / 1.5f);
    float remainingBudget = Mathf.Min(burstCap, Mathf.Max(0f, approachRoom - dashExecutionDistance));
    sprintComboBurstDistance = heavy && burstCap > 0f ?
        remainingBudget * heavyExecutionFirstLungeMax / burstCap : remainingBudget;
    heavyFinalLungeDistance = heavy ? remainingBudget - sprintComboBurstDistance : 0f;
    heavyFinalLungeProgress = 0f;
    sprintComboBurstProgress = 0f;
    if (chargeDirection.sqrMagnitude > 0.001f)
        transform.rotation = Quaternion.LookRotation(chargeDirection, Vector3.up);

    if (swordDamage != null)
        swordDamage.DisableDamage();

    if (animator != null)
    {
        animator.SetTrigger(heavy ? "CloseAttack2" : "ChargeAttack");
        Debug.Log(heavy ? "[HollowKnight] Revamped Dash Execution triggered." : "[HollowKnight] ChargeAttack triggered.");
    }
    else
    {
        Debug.LogWarning("[HollowKnight] Animator missing. Cannot trigger ChargeAttack.");
        isCharging = false;
        isHeavyExecution = false;
        isAttacking = false;
    }
}

public void OnChargeAttackFinished()
{
    Debug.Log("[HollowKnight] ChargeAttack finished. Entering recover.");

    isCharging = false;
    isHeavyExecution = false;
    isAttacking = false;
    chargeLastFrameSpeed = 0f;

    if (swordDamage != null)
        swordDamage.DisableDamage();

    if (recoverCoroutine != null)
        StopCoroutine(recoverCoroutine);

    recoverCoroutine = StartCoroutine(RecoverRoutine());
}

private void LateUpdate()
{
    if (battleStarted && !isDead && isCharging)
        HandleSprintComboMovement();
}

private void HandleSprintComboMovement()
{
    chargeLastFrameSpeed = 0f;
    SetAnimatorSpeed(0f);
    if (animator == null || agent == null || !agent.enabled || !agent.isOnNavMesh)
    {
        FinishAnimatedCharge();
        return;
    }

    string chargeState = isHeavyExecution ? "CloseAttack2" : "ChargeAttack";
    float openingDuration = isHeavyExecution ? HeavyOpeningDuration : SprintComboOpeningDuration;
    AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
    bool active = state.IsName(chargeState);
    if (animator.IsInTransition(0))
    {
        AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(0);
        if (next.IsName(chargeState))
        {
            state = next;
            active = true;
        }
    }

    if (!active)
    {
        dashExecutionWait += Time.deltaTime;
        // Prevent a missing/interrupted animation from leaving the AI locked.
        if (dashExecutionEntered || dashExecutionWait > 1.5f)
            FinishAnimatedCharge();
        return;
    }

    dashExecutionEntered = true;
    // Opening sprint, then one explosive lunge on authored frames 29-33.
    // Progress is monotonic and evaluated from animation time, not FPS.
    float clipTime = state.normalizedTime * state.length;
    float t = Mathf.Clamp01(clipTime / openingDuration);
    float progress = Mathf.SmoothStep(0f, 1f, t);
    float delta = Mathf.Max(0f, progress - dashExecutionProgress);
    dashExecutionProgress = Mathf.Max(dashExecutionProgress, progress);
    float requestedStep = dashExecutionDistance * delta;
    float burstT = Mathf.InverseLerp(
        isHeavyExecution ? HeavyFirstLungeStart : SprintComboBurstStart,
        isHeavyExecution ? HeavyFirstLungeEnd : SprintComboBurstEnd, clipTime);
    float burstProgress = Mathf.SmoothStep(0f, 1f, burstT);
    float burstDelta = Mathf.Max(0f, burstProgress - sprintComboBurstProgress);
    sprintComboBurstProgress = Mathf.Max(sprintComboBurstProgress, burstProgress);
    requestedStep += sprintComboBurstDistance * burstDelta;
    if (isHeavyExecution)
    {
        float finalProgress = Mathf.SmoothStep(0f, 1f,
            Mathf.InverseLerp(HeavyFinalLungeStart, HeavyFinalLungeEnd, clipTime));
        requestedStep += heavyFinalLungeDistance * Mathf.Max(0f, finalProgress - heavyFinalLungeProgress);
        heavyFinalLungeProgress = Mathf.Max(heavyFinalLungeProgress, finalProgress);
    }
    if (requestedStep <= 0f) return;

    Vector3 remaining = chargeTargetPosition - transform.position;
    remaining.y = 0f;
    float room = Mathf.Max(0f, Vector3.Dot(remaining, chargeDirection) - dashExecutionStopDistance);
    float step = Mathf.Min(room, requestedStep);
    if (step <= 0.001f) return;

    // NavMesh movement, not Warp or animation root motion. Never cross a
    // NavMesh edge, wall or the locked target's stopping margin.
    Vector3 before = transform.position;
    if (agent.Raycast(before + chargeDirection * step, out NavMeshHit obstruction))
        step = Mathf.Min(step, Mathf.Max(0f, Vector3.Dot(obstruction.position - before, chargeDirection) - 0.5f));
    if (step > 0.001f)
    {
        // Synchronize the agent and visible transform immediately after the
        // Animator has evaluated. Raycast above bounds this to the NavMesh;
        // nextPosition also keeps the agent's own simulation authoritative.
        agent.nextPosition = before + chargeDirection * step;
        transform.position = agent.nextPosition;
        float moved = Vector3.Distance(before, transform.position);
        chargeTravelled += moved;
        chargeLastFrameSpeed = moved / Mathf.Max(Time.deltaTime, 0.001f);
    }
}

private void FinishAnimatedCharge()
{
    OnChargeAttackFinished();
}

public void OnChargeAttack2Finished()
{
    if (IsDashExecution) OnChargeAttackFinished();
}

    public void TriggerBeingHit()
{
    if (!battleStarted) return;
    if (isBeingHit) return;

    Debug.Log("[HollowKnight] BeingHit triggered.");

    isBeingHit = true;
    isAttacking = false;
    isCharging = false;
    isHeavyExecution = false;
    chargeLastFrameSpeed = 0f;

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
        animator.ResetTrigger("CloseAttack2");
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
    isHeavyExecution = false;
    chargeLastFrameSpeed = 0f;
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
        animator.ResetTrigger("CloseAttack2");
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
