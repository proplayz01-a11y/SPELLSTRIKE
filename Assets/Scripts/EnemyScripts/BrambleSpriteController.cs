using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class BrambleSpriteController : MonoBehaviour
{
    public enum EnemyState
    {
        Patrol, Scouting, Charging, LeapAttack, CloseAttack, ComboAttack, BeingHit, Dead
    }

    [Header("References")]
    public Transform player;
    public NavMeshAgent agent;
    public Animator animator;

    [Header("Stats")]
    public float maxHealth = 80f;
    private float currentHealth;
    public int attackDamage = 8;
    public int leapDamage = 14;
    public int comboDamage = 10;

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
    public float scoutTime = 6f;
    public float scoutStrafeDistance = 4f;

    [Header("Ranges")]
    public float leapTriggerRange = 6f;   // halfway to player — triggers leap
    public float closeAttackRange = 2.5f; // triggers CloseAttack after leap lands

    [Header("Attack Timing")]
    public float timeBetweenAttacks = 1.5f;

    [Header("Hit Reaction")]
    public float hitScoutReduction = 3f;
    private float remainingScoutTime = 0f;
    private float scoutTimeRemaining = 0f;

    [Header("Root Motion")]
    public bool useRootMotion = false;

    [Header("Abilities")]
    public GameObject thornPrefab;
    public Transform thornSpawnPoint;
    public int thornDamage = 8;
    public float thornSpeed = 14f;
    public float thornLifeTime = 5f;

    public GameObject snareVFXPrefab;
    public Transform snareVFXAnchor;
    public float snareSlowMultiplier = 0.5f;
    public float snareDuration = 2f;

    // ── State ──
    public EnemyState currentState = EnemyState.Patrol;
    private bool battleStarted = false;
    private bool isAttacking = false; // previously assigned but never used - now actively used to guard transitions
    private bool isBeingHit = false;
    private Coroutine stateCoroutine = null;
    public float leapCooldown = 5f;
    private bool leapOnCooldown = false;


    // ── Debug ──
    private EnemyState lastLoggedState;
    private string lastLoggedClip;

    // ─────────────────────────────────────────
    private void Awake()
    {
        agent ??= GetComponent<NavMeshAgent>();
        animator ??= GetComponent<Animator>();
        currentHealth = maxHealth;

        if (player == null)
        {
            var pgo = GameObject.FindWithTag("Player");
            if (pgo != null) player = pgo.transform;
        }
    }

    private void Start()
    {
        spawnPosition = transform.position;
        agent.updateRotation = false;
        agent.updatePosition = !useRootMotion;
        agent.speed = walkSpeed;
        agent.stoppingDistance = 0f;
        agent.autoBraking = true;
        animator.applyRootMotion = useRootMotion;
        EnterState(EnemyState.Patrol);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B)) StartBattle();
        if (currentState == EnemyState.Dead) return;
        if (player == null) return;

        // Drive blend tree
        animator.SetFloat("Speed", agent != null ? agent.velocity.magnitude / runSpeed : 0f);

        // Rotate toward movement direction (but NOT while scouting; scouting faces player)
        if (agent != null && agent.velocity.sqrMagnitude > 0.1f &&
            currentState != EnemyState.Scouting)
        {
            Quaternion look = Quaternion.LookRotation(agent.velocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * 10f);
        }


        // Debug logger
        string currentClip = GetCurrentClipName();
        if (currentState != lastLoggedState || currentClip != lastLoggedClip)
        {
            
            lastLoggedState = currentState;
            lastLoggedClip = currentClip;
        }
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

    public void StartBattle()
    {
        if (battleStarted) return;
        battleStarted = true;
        Debug.Log("[BrambleSprite] Battle started!");
        EnterState(EnemyState.Scouting);
    }

    // ── EnterState: StopAllCoroutines guarantees no stacking ──
    private void EnterState(EnemyState newState)
    {
        if (currentState == EnemyState.Dead) return;
        if (newState == EnemyState.BeingHit && isBeingHit) return;

        // Prevent interrupting an ongoing attack with other non-critical transitions.
        if (isAttacking && newState != EnemyState.BeingHit && newState != EnemyState.Dead)
        {
            Debug.Log($"[BrambleSprite] Ignoring EnterState({newState}) because isAttacking");
            return;
        }

        currentState = newState;
        StopAllCoroutines();
        stateCoroutine = null;

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
                isAttacking = true;
                stateCoroutine = StartCoroutine(LeapAttackRoutine());
                break;
            case EnemyState.CloseAttack:
                isAttacking = true;
                stateCoroutine = StartCoroutine(CloseAttackRoutine());
                break;
            case EnemyState.ComboAttack:
                isAttacking = true;
                stateCoroutine = StartCoroutine(ComboAttackRoutine());
                break;
            case EnemyState.BeingHit:
                isBeingHit = true;
                stateCoroutine = StartCoroutine(BeingHitRoutine());
                break;
            case EnemyState.Dead:
                stateCoroutine = StartCoroutine(DieRoutine());
                break;
        }
    }

    // ─────────────────────────────────────────
    //  PATROL — walk/run randomly before battle
    // ─────────────────────────────────────────
    private IEnumerator PatrolRoutine()
    {
        agent.speed = walkSpeed;
        agent.isStopped = false;
        animator.SetBool("Charging", false);
        hasPatrolTarget = false;

        while (currentState == EnemyState.Patrol)
        {
            if (!hasPatrolTarget || (!agent.pathPending && agent.remainingDistance < patrolStopDistance))
            {
                currentPatrolTarget = GetRandomTerrainPoint(spawnPosition, patrolRadius);
                agent.SetDestination(currentPatrolTarget);
                hasPatrolTarget = true;
            }
            yield return null;
        }
    }

    // ─────────────────────────────────────────
    //  SCOUTING — strafe left/right facing player
    // ─────────────────────────────────────────
    private IEnumerator ScoutRoutine()
    {
        agent.speed = walkSpeed * 0.5f;
        agent.isStopped = false;
        agent.stoppingDistance = 0f;
        animator.SetBool("Charging", false);
        animator.SetFloat("Speed", 0f);

        float totalScout = remainingScoutTime > 0f ? remainingScoutTime : scoutTime;
        remainingScoutTime = 0f;
        scoutTimeRemaining = totalScout;
        totalScout = Mathf.Max(0.1f, totalScout);

        float minPhase = Mathf.Min(1f, totalScout);
        float firstDuration = Random.Range(minPhase, Mathf.Max(minPhase, totalScout - minPhase));
        float secondDuration = Mathf.Max(0f, totalScout - firstDuration);
        bool startLeft = Random.value > 0.5f;

        Debug.Log($"[Scout] Total: {totalScout:F1}s | First: {(startLeft ? "Left" : "Right")} {firstDuration:F1}s | Second: {(startLeft ? "Right" : "Left")} {secondDuration:F1}s");

        // First direction
        animator.SetTrigger(startLeft ? "ScoutLeft" : "ScoutRight");
        float elapsed = 0f;
        while (elapsed < firstDuration && currentState == EnemyState.Scouting)
        {
            elapsed += Time.deltaTime;
            scoutTimeRemaining = Mathf.Max(0f, (firstDuration - elapsed) + secondDuration);
            FaceTarget(player.position);
            animator.SetFloat("Speed", 0f);
            Vector3 strafeDir = startLeft ? -transform.right : transform.right;
            Vector3 strafeTarget = transform.position + strafeDir * scoutStrafeDistance;
            if (NavMesh.SamplePosition(strafeTarget, out NavMeshHit hit1, scoutStrafeDistance, NavMesh.AllAreas))
                agent.SetDestination(hit1.position);
            yield return null;
        }
        if (currentState != EnemyState.Scouting) yield break;

        // Second direction
        animator.SetTrigger(startLeft ? "ScoutRight" : "ScoutLeft");
        elapsed = 0f;
        while (elapsed < secondDuration && currentState == EnemyState.Scouting)
        {
            elapsed += Time.deltaTime;
            scoutTimeRemaining = Mathf.Max(0f, secondDuration - elapsed);
            FaceTarget(player.position);
            animator.SetFloat("Speed", 0f);
            Vector3 strafeDir = startLeft ? transform.right : -transform.right;
            Vector3 strafeTarget = transform.position + strafeDir * scoutStrafeDistance;
            if (NavMesh.SamplePosition(strafeTarget, out NavMeshHit hit2, scoutStrafeDistance, NavMesh.AllAreas))
                agent.SetDestination(hit2.position);
            yield return null;
        }
        if (currentState != EnemyState.Scouting) yield break;

        EnterState(EnemyState.Charging);
    }

    // ─────────────────────────────────────────
    //  CHARGING — sprint toward player, leap at halfway
    // ─────────────────────────────────────────
    private bool hasLeapedThisCharge = false;

    private IEnumerator ChargeRoutine()
    {
        agent.speed = runSpeed;
        agent.isStopped = false;
        agent.stoppingDistance = 0f;
        animator.SetBool("Charging", true);
        hasLeapedThisCharge = false;

        while (currentState == EnemyState.Charging)
        {
            float dist = Vector3.Distance(transform.position, player.position);
            agent.SetDestination(player.position);
            FaceTarget(player.position);

            // Only leap once per charge
            if (!hasLeapedThisCharge && !leapOnCooldown && dist <= leapTriggerRange && dist > closeAttackRange)
            {
                hasLeapedThisCharge = true;
                leapOnCooldown = true;
                StartCoroutine(LeapCooldownRoutine());
                EnterState(EnemyState.LeapAttack);
                yield break; // stop this coroutine safely
            }

            // Close attack if in range
            if (dist <= closeAttackRange)
            {
                EnterState(EnemyState.CloseAttack);
                yield break;
            }

            yield return null;
        }
    }

    // ─────────────────────────────────────────
    //  LEAP ATTACK — lunge toward player, land → CloseAttack
    // ─────────────────────────────────────────

    private IEnumerator LeapCooldownRoutine()
    {
        yield return new WaitForSeconds(leapCooldown);
        leapOnCooldown = false;
    }


    private IEnumerator LeapAttackRoutine()
    {
        agent.isStopped = true;
        agent.ResetPath();
        animator.SetBool("Charging", false);
        animator.SetTrigger("LeapAttack");
        FaceTarget(player.position);
        Debug.Log("[BrambleSprite] Animation → LeapAttack");

        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        dirToPlayer.y = 0f;

        float clipLength = GetClipLength("LeapAttack");
        float leapDuration = clipLength > 0 ? clipLength : 1.2f;
        float launchDuration = leapDuration * 0.5f;
        float leapSpeed = 12f;
        float elapsed = 0f;

        while (elapsed < leapDuration)
        {
            elapsed += Time.deltaTime;
            if (elapsed < launchDuration)
            {
                Vector3 move = dirToPlayer * leapSpeed * Time.deltaTime;
                if (elapsed < leapDuration * 0.25f) move.y = 8f * Time.deltaTime;
                else if (elapsed < launchDuration) move.y = -4f * Time.deltaTime;
                transform.position += move;
                agent.nextPosition = transform.position;
            }
            yield return null;
        }

        agent.isStopped = false;

        // Clear attack flag now that leap animation finished
        isAttacking = false;

        // After landing → CloseAttack if in range, else charge again
        float distAfterLeap = Vector3.Distance(transform.position, player.position);
        if (distAfterLeap <= closeAttackRange + 1f)
            EnterState(EnemyState.CloseAttack);
        else
            EnterState(EnemyState.Charging);
    }

    // ─────────────────────────────────────────
    //  CLOSE ATTACK → always chains into Combo
    // ─────────────────────────────────────────
    private IEnumerator CloseAttackRoutine()
    {
        agent.isStopped = true;
        agent.ResetPath();
        animator.SetBool("Charging", false);
        animator.SetTrigger("CloseAttack");
        FaceTarget(player.position);
        Debug.Log("[BrambleSprite] Animation → CloseAttack");

        float clipLength = GetClipLength("CloseAttack");
        yield return new WaitForSeconds(clipLength > 0 ? clipLength : 0.8f);

        // Always chain into ComboAttack
        EnterState(EnemyState.ComboAttack);
    }

    // ─────────────────────────────────────────
    //  COMBO ATTACK → wait cooldown → back to Scouting
    // ─────────────────────────────────────────
    private IEnumerator ComboAttackRoutine()
    {
        animator.SetTrigger("ComboAttack");
        FaceTarget(player.position);
        Debug.Log("[BrambleSprite] Animation → ComboAttack");

        float clipLength = GetClipLength("ComboAttack");
        yield return new WaitForSeconds(clipLength > 0 ? clipLength : 1.0f);

        // Cooldown — isAttacking stays true, blocks any re-entry
        agent.isStopped = false;
        yield return new WaitForSeconds(timeBetweenAttacks);

        isAttacking = false;
        EnterState(EnemyState.Scouting);
    }

    // ─────────────────────────────────────────
    //  BEING HIT — interrupts everything
    // ─────────────────────────────────────────
    private IEnumerator BeingHitRoutine()
    {
        isAttacking = false;
        agent.isStopped = true;
        agent.ResetPath();
        animator.SetBool("Charging", false);
        animator.SetFloat("Speed", 0f);
        animator.SetTrigger("BeingHit");

        float clipLength = GetClipLength("BeingHit");
        yield return new WaitForSeconds(clipLength > 0 ? clipLength : 0.4f);

        isBeingHit = false;
        agent.isStopped = false;
        EnterState(battleStarted ? EnemyState.Scouting : EnemyState.Patrol);
    }

    // ─────────────────────────────────────────
    //  DEATH
    // ─────────────────────────────────────────
    private IEnumerator DieRoutine()
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
    //  ANIMATION EVENTS
    // ─────────────────────────────────────────
    public void OnLeapHit()
    {
        if (player == null) return;
        if (Vector3.Distance(transform.position, player.position) <= leapTriggerRange + 1f)
            DealDamageToPlayer(leapDamage);
    }

    public void OnCloseHit()
    {
        if (player == null) return;
        if (Vector3.Distance(transform.position, player.position) <= closeAttackRange + 0.5f)
            DealDamageToPlayer(attackDamage);
    }
    public void OnAttackHit() => OnCloseHit();

    public void OnComboHit()
    {
        if (player == null) return;
        if (Vector3.Distance(transform.position, player.position) <= closeAttackRange + 0.5f)
            DealDamageToPlayer(comboDamage);
    }
    public void OnAttack2Hit() => OnComboHit();

    public void OnSnareCast()
    {
        if (player == null) return;

        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        if (movement != null)
            movement.ApplySpeedDebuff(snareSlowMultiplier, snareDuration);

        if (snareVFXPrefab != null && snareVFXAnchor != null)
        {
            GameObject vfx = Instantiate(snareVFXPrefab, snareVFXAnchor.position, snareVFXAnchor.rotation);
            vfx.transform.SetParent(snareVFXAnchor);
            Destroy(vfx, snareDuration + 0.1f);
        }

        Debug.Log("[BrambleSprite] Snare applied to player.");
    }

    public void OnThornToss()
    {
        if (thornPrefab == null || thornSpawnPoint == null || player == null)
            return;

        GameObject thorn = Instantiate(thornPrefab, thornSpawnPoint.position, thornSpawnPoint.rotation);
        Vector3 direction = player.position - thornSpawnPoint.position;

        Component thornComponent = thorn.GetComponent("ThornProjectile");
        if (thornComponent != null)
        {
            var method = thornComponent.GetType().GetMethod("Launch", new System.Type[] { typeof(Vector3), typeof(int) });
            if (method != null)
                method.Invoke(thornComponent, new object[] { direction, thornDamage });
            else
                Debug.LogWarning("ThornProjectile component found but Launch(Vector3,int) method is missing.", thorn);
        }
        else
        {
            Rigidbody rb = thorn.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = direction.normalized * thornSpeed;
            }
            else
            {
                Debug.LogWarning("Thorn prefab must have either a ThornProjectile component or a Rigidbody.", thorn);
            }
        }

        Debug.Log("[BrambleSprite] Thorn Toss launched.");
    }

    private void DealDamageToPlayer(int damage)
    {
        var damageable = player.root.GetComponentInChildren<IDamageable>();
        if (damageable != null) damageable.ApplyDamage(damage);
        else player.SendMessage("ApplyDamage", damage, SendMessageOptions.DontRequireReceiver);
        Debug.Log($"[BrambleSprite] Dealt {damage} damage to player.");
    }

    // ─────────────────────────────────────────
    //  HEALTH
    // ─────────────────────────────────────────
    public void TakeDamage(float damage)
    {
        if (currentState == EnemyState.Dead) return;
        if (!battleStarted) battleStarted = true;

        float scoutBase = remainingScoutTime > 0f ? remainingScoutTime
            : (currentState == EnemyState.Scouting ? scoutTimeRemaining : scoutTime);
        remainingScoutTime = Mathf.Max(0.1f, scoutBase - hitScoutReduction);

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        Debug.Log($"[BrambleSprite] Took {damage} dmg. HP: {currentHealth}/{maxHealth}");

        EnemyHealth enemyHealth = GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.currentHealth = currentHealth;
            if (enemyHealth.healthSlider != null)
                enemyHealth.healthSlider.value = currentHealth;
        }

        if (currentHealth <= 0f) { EnterState(EnemyState.Dead); return; }
        if (!isBeingHit && currentState != EnemyState.BeingHit)
            EnterState(EnemyState.BeingHit);
    }

    // ─────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────
    private void FaceTarget(Vector3 targetPos)
    {
        Vector3 dir = targetPos - transform.position; dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(dir.normalized), Time.deltaTime * 10f);
    }

    private Vector3 GetRandomTerrainPoint(Vector3 center, float radius)
    {
        Vector2 randomCircle = Random.insideUnitCircle * radius;
        Vector3 randomPoint = center + new Vector3(randomCircle.x, 0f, randomCircle.y);
        Ray ray = new Ray(randomPoint + Vector3.up * 50f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f)) randomPoint = hit.point;
        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit navHit, radius, NavMesh.AllAreas))
            return navHit.position;
        return center;
    }

    private float GetClipLength(string clipName)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return 0f;
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(spawnPosition == Vector3.zero ? transform.position : spawnPosition, patrolRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, leapTriggerRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, closeAttackRange);
    }
}