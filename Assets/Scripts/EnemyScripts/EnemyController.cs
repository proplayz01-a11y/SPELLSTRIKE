using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class EnemyController : MonoBehaviour
{
    // ─────────────────────────────────────────
    //  ENUMS
    // ─────────────────────────────────────────
    public enum EnemyState
    {
        Patrol,
        Scouting,
        Charging,
        LeapAttack,
        CloseAttack,
        ComboAttack,
        BeingHit,  // ← ADD
        Dead
    }

    // ─────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────
    [Header("References")]
    public Transform player;
    public NavMeshAgent agent;
    public Animator animator;

    [Header("Hit Reaction")]
    public float hitScoutReduction = 3f; // reduces scout timer by this much per hit
    private bool isBeingHit = false;
    private float remainingScoutTime = 0f; // tracks remaining scout time for reduction
    private float scoutTimeRemaining = 0f; // remaining time in current scout routine

    [Header("Stats")]
    public float maxHealth = 100f;
    protected float currentHealth;
    public int attackDamage = 10;
    public int leapDamage = 20;
    public int comboDamage = 15;

    [Header("Movement")]
    public float walkSpeed = 3.5f;
    public float runSpeed = 6f;

    [Header("Patrol")]
    public float patrolRadius = 10f;
    public float patrolStopDistance = 1f;
    private Vector3 spawnPosition;
    private Vector3 currentPatrolTarget;
    private bool hasPatrolTarget = false;

    [Header("Scouting")]
    [Tooltip("Total scout duration before charging")]
    public float scoutTime = 6f; // reduced from 10s per ChatGPT feedback

    [Header("Ranges")]
    public float leapAttackRange = 6f;
    public float closeAttackRange = 2.5f;

    [Header("Attack Timing")]
    public float timeBetweenAttacks = 1.5f;
    public float leapCooldown = 5f;

    [Range(0f, 1f)]
    public float comboChance = 0.60f;

    [Header("Root Motion")]
    public bool useRootMotion = false;

    // ─────────────────────────────────────────
    //  PRIVATE STATE
    // ─────────────────────────────────────────
    public EnemyState currentState = EnemyState.Patrol;
    private bool battleStarted = false;
    protected bool isAttacking = false;
    protected bool alreadyAttacked = false;
    protected bool leapOnCooldown = false;
    private EnemyState lastLoggedState;
    private string lastLoggedClip;
    private Coroutine stateCoroutine;

    // ─────────────────────────────────────────
    //  UNITY LIFECYCLE
    // ─────────────────────────────────────────
    private void Awake()
    {
        agent ??= GetComponent<NavMeshAgent>();
        animator ??= GetComponent<Animator>();

        if (player == null)
        {
            var pgo = GameObject.FindWithTag("Player");
            if (pgo != null) player = pgo.transform;
            else
            {
                var found = GameObject.Find("PlayerObj");
                if (found != null) player = found.transform;
            }
        }

        currentHealth = maxHealth;
    }

    private void Start()
    {
        spawnPosition = transform.position;

        if (agent != null)
        {
            agent.updateRotation = false;
            agent.updatePosition = !useRootMotion;
            agent.speed = walkSpeed;
            agent.stoppingDistance = closeAttackRange * 0.9f;
            agent.autoBraking = true;
        }

        if (animator != null)
            animator.applyRootMotion = useRootMotion;

        EnterState(EnemyState.Patrol);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
            StartBattle();
        if (currentState == EnemyState.Dead) return;
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        // Sync Speed to Blend Tree
        float speed = agent != null ? agent.velocity.magnitude : 0f;
        animator.SetFloat("Speed", speed);

        if (battleStarted && !isAttacking && !isBeingHit &&
     currentState != EnemyState.CloseAttack &&
     currentState != EnemyState.ComboAttack &&
     currentState != EnemyState.BeingHit &&
     currentState != EnemyState.Dead &&
     !alreadyAttacked)
        {
            if (dist <= closeAttackRange)
            {
                EnterState(EnemyState.CloseAttack);
                return;
            }
        }

        // Debug logger
        string currentClip = GetCurrentClipName();
        if (currentState != lastLoggedState || currentClip != lastLoggedClip)
        {
            Debug.Log($"[Enemy] State: {currentState} | Clip: {currentClip}");
            lastLoggedState = currentState;
            lastLoggedClip = currentClip;
        }

        // Rotate toward movement direction
        if (agent != null && agent.velocity.sqrMagnitude > 0.1f)
        {
            Quaternion look = Quaternion.LookRotation(agent.velocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * 10f);
        }

        // GLOBAL CloseAttack interrupt
        // Triggers from ANY state (except attacking/dead) when player too close
       if (battleStarted && !isAttacking && !isBeingHit &&
    currentState != EnemyState.CloseAttack &&
    currentState != EnemyState.ComboAttack &&
    currentState != EnemyState.BeingHit &&
    currentState != EnemyState.Dead)
{
    if (dist <= closeAttackRange)
    {
        EnterState(EnemyState.CloseAttack);
        return;
    }
}

        EvaluateStateTransitions(dist);
    }

    void OnAnimatorMove()
    {
        if (!useRootMotion || animator == null || agent == null) return;
        Vector3 delta = animator.deltaPosition;
        if (delta.sqrMagnitude <= Mathf.Epsilon) return;
        Vector3 nextPos = agent.nextPosition + delta;
        nextPos.y = agent.nextPosition.y;
        transform.position = nextPos;
        agent.nextPosition = nextPos;
    }

    // ─────────────────────────────────────────
    //  BATTLE START
    // ─────────────────────────────────────────

    /// <summary>
    /// Called by the arena trigger when player steps on platform.
    /// Discards patrol and starts combat loop.
    /// </summary>
    public void StartBattle()
    {
        if (battleStarted) return;
        battleStarted = true;
        Debug.Log($"[Enemy] Battle started!");
        EnterState(EnemyState.Scouting);
    }

    // ─────────────────────────────────────────
    //  STATE MACHINE
    // ─────────────────────────────────────────
    private void EvaluateStateTransitions(float dist)
    {
        // Block transitions while attacking
        if (isAttacking) return;
        if (isBeingHit) return;

        switch (currentState)
        {
            case EnemyState.Patrol:
                UpdatePatrol();
                break;
            case EnemyState.Scouting:
                // Scouting is fully handled by ScoutRoutine coroutine
                break;
            case EnemyState.Charging:
                UpdateCharging(dist);
                break;
            case EnemyState.BeingHit:
                // BeingHit is fully handled by BeingHitRoutine coroutine started in EnterState.
                break;
        }
    }

    private void EnterState(EnemyState newState)
    {
        if (currentState == EnemyState.Dead) return;
        if (newState == EnemyState.BeingHit && (isBeingHit || currentState == EnemyState.BeingHit)) return;

        currentState = newState;

        if (stateCoroutine != null)
        {
            StopCoroutine(stateCoroutine);
            stateCoroutine = null;
        }

        switch (newState)
        {
            case EnemyState.Patrol:
                stateCoroutine = StartCoroutine(PatrolRoutine());
                break;
            case EnemyState.Scouting:
                stateCoroutine = StartCoroutine(ScoutRoutine());
                break;
            case EnemyState.Charging:
                stateCoroutine = StartCoroutine(ChargeRoutine());
                break;
            case EnemyState.LeapAttack:
                stateCoroutine = StartCoroutine(LeapAttackRoutine());
                break;
            case EnemyState.CloseAttack:
                stateCoroutine = StartCoroutine(CloseAttackRoutine());
                break;
            case EnemyState.ComboAttack:
                stateCoroutine = StartCoroutine(ComboAttackRoutine());
                break;
            case EnemyState.Dead:
                stateCoroutine = StartCoroutine(DieRoutine());
                break;
            case EnemyState.BeingHit:
                isBeingHit = true;
                stateCoroutine = StartCoroutine(BeingHitRoutine());
                break;

        }
    }

    // ─────────────────────────────────────────
    //  PATROL
    // ─────────────────────────────────────────
    private void UpdatePatrol()
    {
        // If battle started, immediately switch to Scouting
        if (battleStarted)
        {
            EnterState(EnemyState.Scouting);
            return;
        }
    }

    private IEnumerator BeingHitRoutine()
    {
        Debug.Log($"[BeingHit] Triggered! HP remaining: {currentHealth}"); // ← ADD

        isBeingHit = true;
        isAttacking = false;
        alreadyAttacked = false;

        agent.isStopped = true;
        agent.ResetPath();
        animator.SetBool("Charging", false);
        animator.SetFloat("Speed", 0f);
        animator.SetTrigger("BeingHit");

        Debug.Log($"[BeingHit] Animation trigger fired!"); // ← ADD

        float clipLength = GetClipLength("BeingHit");
        Debug.Log($"[BeingHit] Clip length: {clipLength}"); // ← ADD

        yield return new WaitForSeconds(clipLength > 0 ? clipLength : 0.4f);

        isBeingHit = false;
        agent.isStopped = false;

        EnemyState nextState = battleStarted ? EnemyState.Scouting : EnemyState.Patrol;
        Debug.Log($"[BeingHit] Finished, returning to {nextState}"); // ← ADD
        EnterState(nextState);
    }

    private IEnumerator PatrolRoutine()
    {
        agent.speed = walkSpeed;
        animator.SetBool("Charging", false);
        hasPatrolTarget = false;

        while (currentState == EnemyState.Patrol)
        {
            if (!hasPatrolTarget ||
                (!agent.pathPending && agent.remainingDistance < patrolStopDistance))
            {
                currentPatrolTarget = GetRandomTerrainPoint(spawnPosition, patrolRadius);
                agent.SetDestination(currentPatrolTarget);
                hasPatrolTarget = true;
            }

            yield return null;
        }
    }

    // ─────────────────────────────────────────
    //  SCOUTING
    // ─────────────────────────────────────────
    private IEnumerator ScoutRoutine()
    {
        agent.speed = walkSpeed * 0.5f;
        animator.SetBool("Charging", false);
        animator.SetFloat("Speed", 0f);

        // Use remaining scout time if set (from hit reduction), otherwise full scoutTime
        float totalScoutTime = remainingScoutTime > 0 ? remainingScoutTime : scoutTime;
        remainingScoutTime = 0f; // reset after use
        scoutTimeRemaining = totalScoutTime;

        totalScoutTime = Mathf.Max(0.1f, totalScoutTime);
        float minPhase = Mathf.Min(1f, totalScoutTime);
        float maxFirst = Mathf.Max(minPhase, totalScoutTime - minPhase);
        float firstDuration = Random.Range(minPhase, maxFirst);
        float secondDuration = Mathf.Max(0f, totalScoutTime - firstDuration);

        bool startLeft = Random.value > 0.5f;

        Debug.Log($"[Scout] Total: {totalScoutTime:F1}s | " +
                  $"First: {(startLeft ? "Left" : "Right")} {firstDuration:F1}s | " +
                  $"Second: {(startLeft ? "Right" : "Left")} {secondDuration:F1}s");

        // FIRST DIRECTION
        animator.SetTrigger(startLeft ? "ScoutLeft" : "ScoutRight");

        float elapsed = 0f;
        while (elapsed < firstDuration && currentState == EnemyState.Scouting)
        {
            elapsed += Time.deltaTime;
            scoutTimeRemaining = Mathf.Max(0f, (firstDuration - elapsed) + secondDuration);
            FaceTarget(player.position);
            animator.SetFloat("Speed", 0f);

            Vector3 strafeDir = startLeft ? -transform.right : transform.right;
            Vector3 strafeTarget = transform.position + strafeDir * 2f;

            Ray ray = new Ray(strafeTarget + Vector3.up * 50f, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                strafeTarget = hit.point;

            if (NavMesh.SamplePosition(strafeTarget, out NavMeshHit navHit, 2f, NavMesh.AllAreas))
                agent.SetDestination(navHit.position);

            yield return null;
        }

        if (currentState != EnemyState.Scouting) yield break;

        // SECOND DIRECTION
        animator.SetTrigger(startLeft ? "ScoutRight" : "ScoutLeft");

        elapsed = 0f;
        while (elapsed < secondDuration && currentState == EnemyState.Scouting)
        {
            elapsed += Time.deltaTime;
            scoutTimeRemaining = Mathf.Max(0f, secondDuration - elapsed);
            FaceTarget(player.position);
            animator.SetFloat("Speed", 0f);

            Vector3 strafeDir = startLeft ? transform.right : -transform.right;
            Vector3 strafeTarget = transform.position + strafeDir * 2f;

            Ray ray = new Ray(strafeTarget + Vector3.up * 50f, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                strafeTarget = hit.point;

            if (NavMesh.SamplePosition(strafeTarget, out NavMeshHit navHit, 2f, NavMesh.AllAreas))
                agent.SetDestination(navHit.position);

            yield return null;
        }

        if (currentState != EnemyState.Scouting) yield break;

        EnterState(EnemyState.Charging);
    }

    // ─────────────────────────────────────────
    //  CHARGING
    // ─────────────────────────────────────────
    private void UpdateCharging(float dist)
    {
        // Leap attack if in range and not on cooldown
        if (dist <= leapAttackRange && dist > closeAttackRange
            && !leapOnCooldown && !alreadyAttacked)
        {
            EnterState(EnemyState.LeapAttack);
            return;
        }
    }

    private IEnumerator ChargeRoutine()
    {
        agent.speed = runSpeed;
        animator.SetBool("Charging", true);

        while (currentState == EnemyState.Charging)
        {
            if (player != null)
                agent.SetDestination(player.position);

            FaceTarget(player.position);
            yield return null;
        }
    }

    // ─────────────────────────────────────────
    //  LEAP ATTACK
    // ─────────────────────────────────────────
    private IEnumerator LeapAttackRoutine()
    {
        isAttacking = true;
        alreadyAttacked = true;
        leapOnCooldown = true;

        agent.isStopped = true;
        agent.ResetPath();
        animator.SetBool("Charging", false);
        animator.SetTrigger("LeapAttack");

        // Get direction toward player
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        dirToPlayer.y = 0f;

        // Face player before leaping
        FaceTarget(player.position);

        float clipLength = GetClipLength("LeapAttack");
        float leapDuration = clipLength > 0 ? clipLength : 1.2f;
        float elapsed = 0f;

        // Launch phase — move toward player during first half of animation
        float launchDuration = leapDuration * 0.5f;
        float leapSpeed = 12f; // how fast enemy lunges forward

        while (elapsed < leapDuration)
        {
            elapsed += Time.deltaTime;

            // Only move forward during first half (launch phase)
            if (elapsed < launchDuration)
            {
                // Move enemy physically toward player
                Vector3 leapMovement = dirToPlayer * leapSpeed * Time.deltaTime;

                // Add upward arc during first quarter
                if (elapsed < leapDuration * 0.25f)
                    leapMovement.y = 8f * Time.deltaTime; // rise up
                else if (elapsed < launchDuration)
                    leapMovement.y = -4f * Time.deltaTime; // come down

                transform.position += leapMovement;

                // Sync NavMesh agent position
                agent.nextPosition = transform.position;
            }

            yield return null;
        }

        isAttacking = false;
        agent.isStopped = false;
        alreadyAttacked = false;
        StartCoroutine(LeapCooldownRoutine());

        // Always back to Scouting
        EnterState(EnemyState.Scouting);
    }

    private IEnumerator LeapCooldownRoutine()
    {
        yield return new WaitForSeconds(leapCooldown);
        leapOnCooldown = false;
    }

    // ─────────────────────────────────────────
    //  CLOSE ATTACK
    // ─────────────────────────────────────────
    private IEnumerator CloseAttackRoutine()
    {
        isAttacking = true;

        agent.isStopped = true;
        agent.ResetPath();
        animator.SetBool("Charging", false);
        animator.SetTrigger("CloseAttack");
        FaceTarget(player.position);

        float clipLength = GetClipLength("CloseAttack");
        float elapsed = 0f;

        while (elapsed < (clipLength > 0 ? clipLength : 0.8f))
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        isAttacking = false;
        agent.isStopped = false;

        // 60% combo — only if player still close
        if (Random.value <= comboChance)
        {
            float distComboCheck = Vector3.Distance(transform.position, player.position);
            if (distComboCheck <= closeAttackRange + 1f)
            {
                EnterState(EnemyState.ComboAttack);
                yield break;
            }
        }

        // Check distance AFTER animation finishes
        float distAfter = Vector3.Distance(transform.position, player.position);
        if (distAfter <= closeAttackRange)
        {
            // Player still close → fire again ✅
            EnterState(EnemyState.CloseAttack);
        }
        else
        {
            // Player moved away → back to Scouting ✅
            alreadyAttacked = false;
            EnterState(EnemyState.Scouting);
        }
    }
    // ─────────────────────────────────────────
    //  COMBO ATTACK
    // ─────────────────────────────────────────
    private IEnumerator ComboAttackRoutine()
    {
        isAttacking = true;
        alreadyAttacked = true; // blocks re-entry

        animator.SetTrigger("ComboAttack");
        FaceTarget(player.position);

        float clipLength = GetClipLength("ComboAttack");
        float elapsed = 0f;

        while (elapsed < (clipLength > 0 ? clipLength : 1.0f))
        {
            elapsed += Time.deltaTime;

            // Cancel mid-animation if player moved away
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist > closeAttackRange + 1.5f)
            {
                Debug.Log("[ComboAttack] Player escaped, cancelling");
                break;
            }

            yield return null;
        }

        isAttacking = false;
        agent.isStopped = false;

        // Back to Scouting first
        EnterState(EnemyState.Scouting);

        // Cooldown THEN reset — blocks combo from firing again until cooldown done
        yield return new WaitForSeconds(timeBetweenAttacks);
        alreadyAttacked = false;
    }
    // ─────────────────────────────────────────
    //  ANIMATION EVENTS
    // ─────────────────────────────────────────
    public void OnLeapHit()
    {
        if (player == null) return;
        if (Vector3.Distance(transform.position, player.position) <= leapAttackRange + 1f)
            DealDamageToPlayer(leapDamage);
    }

    public void OnCloseHit()
    {
        if (player == null) return;
        if (Vector3.Distance(transform.position, player.position) <= closeAttackRange + 0.5f)
            DealDamageToPlayer(attackDamage);
    }

    public void OnComboHit()
    {
        if (player == null) return;
        if (Vector3.Distance(transform.position, player.position) <= closeAttackRange + 0.5f)
            DealDamageToPlayer(comboDamage);
    }

    private void DealDamageToPlayer(int damage)
    {
        var damageable = player.root.GetComponentInChildren<IDamageable>();
        if (damageable != null)
        {
            damageable.ApplyDamage(damage);
            Debug.Log($"{name} dealt {damage} damage to player.");
        }
        else
        {
            player.SendMessage("ApplyDamage", damage, SendMessageOptions.DontRequireReceiver);
        }
    }

    // ─────────────────────────────────────────
    //  HEALTH / DEATH
    // ─────────────────────────────────────────
    public virtual void TakeDamage(float damage, int wordLength = 0)
    {
        if (currentState == EnemyState.Dead) return;

        // Getting hit should count as starting combat.
        if (!battleStarted)
        {
            battleStarted = true;
            Debug.Log("[Enemy] Battle started (took damage).");
        }

        float scoutBaseTime = remainingScoutTime > 0f
            ? remainingScoutTime
            : (currentState == EnemyState.Scouting ? scoutTimeRemaining : scoutTime);
        remainingScoutTime = Mathf.Max(0.1f, scoutBaseTime - hitScoutReduction);

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        Debug.Log($"{name} took {damage} damage. HP: {currentHealth}/{maxHealth}");

        // Sync health bar UI
        EnemyHealth enemyHealth = GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.currentHealth = currentHealth;
            if (enemyHealth.healthSlider != null)
                enemyHealth.healthSlider.value = currentHealth;
        }

        if (currentHealth <= 0f)
        {
            EnterState(EnemyState.Dead);
            return;
        }

        // Trigger hit reaction
        if (!isBeingHit && currentState != EnemyState.BeingHit)
            EnterState(EnemyState.BeingHit);
    }

    protected virtual IEnumerator DieRoutine()
    {
        agent.isStopped = true;
        agent.enabled = false;
        animator.SetBool("Dead", true);
        animator.SetBool("Charging", false);
        Debug.Log($"{name} died.");
        yield return new WaitForSeconds(5f);
        Destroy(gameObject);
    }

    // ─────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────
    private void FaceTarget(Vector3 targetPos)
    {
        Vector3 dir = targetPos - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir.normalized),
                Time.deltaTime * 10f);
    }

    private Vector3 GetRandomTerrainPoint(Vector3 center, float radius)
    {
        Vector2 randomCircle = Random.insideUnitCircle * radius;
        Vector3 randomPoint = center + new Vector3(randomCircle.x, 0f, randomCircle.y);

        Ray ray = new Ray(randomPoint + Vector3.up * 50f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            randomPoint = hit.point;

        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit navHit, radius, NavMesh.AllAreas))
            return navHit.position;

        return center;
    }

    private float GetClipLength(string clipName)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return 0f;
        foreach (var clip in animator.runtimeAnimatorController.animationClips)
            if (clip.name == clipName) return clip.length;
        return 0f;
    }

    private string GetCurrentClipName()
    {
        if (animator == null) return "None";
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName("Blend Tree"))
        {
            AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
            if (clipInfo.Length > 0) return clipInfo[0].clip.name;
            return "Blend Tree";
        }
        foreach (var clip in animator.runtimeAnimatorController.animationClips)
            if (stateInfo.IsName(clip.name)) return clip.name;
        return stateInfo.fullPathHash.ToString();
    }

    // ─────────────────────────────────────────
    //  GIZMOS
    // ─────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(spawnPosition == Vector3.zero ?
            transform.position : spawnPosition, patrolRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, leapAttackRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, closeAttackRange);
    }
}
