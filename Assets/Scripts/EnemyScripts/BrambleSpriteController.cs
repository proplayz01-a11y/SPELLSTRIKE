using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class BrambleSpriteController : MonoBehaviour
{
    public event System.Action<BrambleSpriteController> OnDeathStarted;

    public enum EnemyState
    {
        Scouting, CloseAttack, ComboAttack, BeingHit, Dead
    }

    [Header("References")]
    public Transform player;
    public NavMeshAgent agent;
    public Animator animator;

    [Header("Stats")]
    public float maxHealth = 80f;
    private float currentHealth;
    // attackDamage, leapDamage and comboDamage removed

    [Header("Movement")]
    public float walkSpeed = 3.5f;
    public float runSpeed = 6f;

    [Header("Scouting")]
    public float scoutTime = 6f;
    public float scoutStrafeDistance = 4f;
    public float scoutFaceTurnSpeed = 1080f;

    [Header("Ranges")]
    public float closeAttackRange = 2.5f; // retained for editor tuning if needed by animation timing/radius visuals

    [Header("Attack Timing")]
    public float timeBetweenAttacks = 1.5f;

    [Header("Hit Reaction")]
    public float hitScoutReduction = 3f;
    private float remainingScoutTime = 0f;
    private float scoutTimeRemaining = 0f;

    [Header("Root Motion")]
    public bool useRootMotion = false;

    [Header("Abilities")]
    public string snareAnimationTrigger = "CloseAttack";
    public string thornTossAnimationTrigger = "ComboAttack";
    public float snareStunDuration = 2f;

    public GameObject thornPrefab;
    public Transform thornSpawnPoint;
    public int thornDamage = 8;
    public float thornSpeed = 14f;
    public float thornLifeTime = 5f;

    public GameObject snareVFXPrefab;
    public Transform snareVFXAnchor;
    public float snareSlowMultiplier = 0.5f; // optional extra slow after stun
    public float snareDuration = 2f;

    // -- State --
    public EnemyState currentState = EnemyState.Scouting;
    private bool battleStarted = false;
    private bool isAttacking = false;
    private bool isBeingHit = false;
    private bool isTransitioning = false; // Prevents EnterState() from being called during state changes
    private Coroutine stateCoroutine = null;  // Track current coroutine
    // -- Debug --
    private EnemyState lastLoggedState;
    private string lastLoggedClip;
    private bool deathEventSent = false;

    // -----------------------------------------
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
        agent.updateRotation = false;
        agent.updatePosition = !useRootMotion;
        agent.speed = walkSpeed;
        agent.stoppingDistance = 0f;
        agent.autoBraking = true;
        animator.applyRootMotion = useRootMotion;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B)) StartBattle();
        if (currentState == EnemyState.Dead) return;
        if (player == null) return;
        FaceTarget(player.position);

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

        StopAllCoroutines(); // IMPORTANT
        stateCoroutine = null;

        isAttacking = false;
        isBeingHit = false;
        isTransitioning = false;

        currentState = EnemyState.Scouting;

        EnterState(EnemyState.Scouting);
    }

    // -- EnterState: StopAllCoroutines guarantees no stacking --
    // -- EnterState: HARD GUARDS + strict coroutine management --
    private void EnterState(EnemyState newState)
    {
        // Prevent re-entrance and overlapping transitions
        // === CALLER CONTEXT DEBUG ===
        var stackTrace = System.Environment.StackTrace;
        var callerInfo = stackTrace.Split('\n')[1]; // Get immediate caller
        Debug.Log($"[EnterState] Called from: {callerInfo.Trim()}");
        Debug.Log($"[EnterState] Transitioning from {currentState} → {newState}");

        // === ALL GUARDS PASSED - SAFE TO TRANSITION ===
        isTransitioning = true;
        currentState = newState;
        StopAllCoroutines();

        // === STOP OLD COROUTINE (only stop if different) ===
        if (stateCoroutine != null)
        {
            Debug.Log($"[EnterState] Stopping previous coroutine for {currentState}");
            StopCoroutine(stateCoroutine);
            stateCoroutine = null;
        }

        Debug.Log($"[EnterState] === TRANSITION COMPLETE: Now in {newState} ===\n");

        // === START NEW STATE COROUTINE ===
        switch (newState)
        {
            case EnemyState.Scouting:
                Debug.Log("[EnterState] Starting ScoutRoutine");
                stateCoroutine = StartCoroutine(ScoutRoutine());
                break;
            case EnemyState.CloseAttack:
                Debug.Log("[EnterState] Starting CloseAttackRoutine");
                isAttacking = true;
                stateCoroutine = StartCoroutine(CloseAttackRoutine());
                break;
            case EnemyState.ComboAttack:
                Debug.Log("[EnterState] Starting ComboAttackRoutine");
                isAttacking = true;
                stateCoroutine = StartCoroutine(ComboAttackRoutine());
                break;
            case EnemyState.BeingHit:
                Debug.Log("[EnterState] Starting BeingHitRoutine");
                isBeingHit = true;
                stateCoroutine = StartCoroutine(BeingHitRoutine());
                break;
            case EnemyState.Dead:
                Debug.Log("[EnterState] Starting DieRoutine");
                stateCoroutine = StartCoroutine(DieRoutine());
                break;
        }

        isTransitioning = false;
    }

    // -----------------------------------------
    //  SCOUTING — strafe left/right facing player
    // -----------------------------------------
    private IEnumerator ScoutRoutine()
    {
        agent.speed = walkSpeed * 0.5f;
        agent.isStopped = false;
        agent.stoppingDistance = 0f;
        animator.SetFloat("Speed", 0f);

        // === INITIAL DEBUG ===
        Debug.Log("[Scout] === ENTERING SCOUT ROUTINE ===");
        Debug.Log($"[Scout] Agent enabled: {agent.enabled}");
        Debug.Log($"[Scout] Agent isStopped: {agent.isStopped}");
        Debug.Log($"[Scout] Agent isOnNavMesh: {agent.isOnNavMesh}");
        Debug.Log($"[Scout] Agent hasPath: {agent.hasPath}");
        Debug.Log($"[Scout] Agent velocity: {agent.velocity}");
        Debug.Log($"[Scout] Agent speed: {agent.speed}");
        Debug.Log($"[Scout] Agent stoppingDistance: {agent.stoppingDistance}");

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
        animator.CrossFade(startLeft ? "ScoutLeft" : "ScoutRight", 0.1f);
        float elapsed = 0f;
        int frameCount = 0;
        bool hasSetDestinationPhase1 = false;
        Vector3 destPhase1 = Vector3.zero;
        
        while (elapsed < firstDuration && currentState == EnemyState.Scouting)
        {
            elapsed += Time.deltaTime;
            scoutTimeRemaining = Mathf.Max(0f, (firstDuration - elapsed) + secondDuration);
            FaceTarget(player.position, scoutFaceTurnSpeed);
            animator.SetFloat("Speed", 0f);
            Vector3 strafeDir = startLeft ? -transform.right : transform.right;
            Vector3 strafeTarget = transform.position + strafeDir * scoutStrafeDistance;
            
            // === ONLY SET DESTINATION ONCE ===
            if (!hasSetDestinationPhase1)
            {
                if (NavMesh.SamplePosition(strafeTarget, out NavMeshHit hit1, scoutStrafeDistance, NavMesh.AllAreas))
                {
                    destPhase1 = hit1.position;
                    agent.SetDestination(destPhase1);
                    hasSetDestinationPhase1 = true;
                    Debug.Log($"[Scout] FIRST PHASE - Destination SET (only once): {destPhase1}");
                }
                else
                {
                    Debug.LogWarning($"[Scout] FIRST PHASE - NavMesh.SamplePosition FAILED for target {strafeTarget}");
                    yield break; // Exit if we can't find a valid destination
                }
            }
            
            // === PER-FRAME DEBUG (every 30 frames to avoid spam) ===
            frameCount++;
            if (frameCount % 30 == 0)
            {
                Debug.Log($"[Scout] FIRST PHASE - Frame {frameCount}:");
                Debug.Log($"  → Current position: {transform.position}");
                Debug.Log($"  → Distance to destination: {Vector3.Distance(transform.position, destPhase1):F2}");
                Debug.Log($"  → isStopped: {agent.isStopped}");
                Debug.Log($"  → hasPath: {agent.hasPath}");
                Debug.Log($"  → isOnNavMesh: {agent.isOnNavMesh}");
                Debug.Log($"  → velocity: {agent.velocity.magnitude:F2} m/s");
                Debug.Log($"  → pathPending: {agent.pathPending}");
                
                if (agent.hasPath)
                {
                    Debug.Log($"  → remainingDistance: {agent.remainingDistance:F2}");
                    Debug.Log($"  → pathStatus: {agent.pathStatus}");
                }
            }
            
            yield return null;
        }
        if (currentState != EnemyState.Scouting) yield break;

        // Second direction
        animator.CrossFade(startLeft ? "ScoutRight" : "ScoutLeft", 0.1f);
        elapsed = 0f;
        frameCount = 0;
        bool hasSetDestinationPhase2 = false;
        Vector3 destPhase2 = Vector3.zero;
        
        while (elapsed < secondDuration && currentState == EnemyState.Scouting)
        {
            elapsed += Time.deltaTime;
            scoutTimeRemaining = Mathf.Max(0f, secondDuration - elapsed);
            FaceTarget(player.position, scoutFaceTurnSpeed);
            animator.SetFloat("Speed", 0f);
            Vector3 strafeDir = startLeft ? transform.right : -transform.right;
            Vector3 strafeTarget = transform.position + strafeDir * scoutStrafeDistance;
            
            // === ONLY SET DESTINATION ONCE ===
            if (!hasSetDestinationPhase2)
            {
                if (NavMesh.SamplePosition(strafeTarget, out NavMeshHit hit2, scoutStrafeDistance, NavMesh.AllAreas))
                {
                    destPhase2 = hit2.position;
                    agent.SetDestination(destPhase2);
                    hasSetDestinationPhase2 = true;
                    Debug.Log($"[Scout] SECOND PHASE - Destination SET (only once): {destPhase2}");
                }
                else
                {
                    Debug.LogWarning($"[Scout] SECOND PHASE - NavMesh.SamplePosition FAILED for target {strafeTarget}");
                    yield break; // Exit if we can't find a valid destination
                }
            }
            
            // === PER-FRAME DEBUG (every 30 frames to avoid spam) ===
            frameCount++;
            if (frameCount % 30 == 0)
            {
                Debug.Log($"[Scout] SECOND PHASE - Frame {frameCount}:");
                Debug.Log($"  → Current position: {transform.position}");
                Debug.Log($"  → Distance to destination: {Vector3.Distance(transform.position, destPhase2):F2}");
                Debug.Log($"  → isStopped: {agent.isStopped}");
                Debug.Log($"  → hasPath: {agent.hasPath}");
                Debug.Log($"  → isOnNavMesh: {agent.isOnNavMesh}");
                Debug.Log($"  → velocity: {agent.velocity.magnitude:F2} m/s");
                Debug.Log($"  → pathPending: {agent.pathPending}");
                
                if (agent.hasPath)
                {
                    Debug.Log($"  → remainingDistance: {agent.remainingDistance:F2}");
                    Debug.Log($"  → pathStatus: {agent.pathStatus}");
                }
            }
            
            yield return null;
        }
        if (currentState != EnemyState.Scouting) yield break;

        EnterState(EnemyState.CloseAttack);
    }

    // -----------------------------------------
    //  CLOSE ATTACK (Bramble Snare) ? chains into Thorn Toss only when snare lands
    // -----------------------------------------
    private IEnumerator CloseAttackRoutine()
    {
        Debug.Log("[BrambleSprite] === ENTERING CLOSE ATTACK ===");
        agent.isStopped = true;
        agent.ResetPath();
        animator.CrossFade(snareAnimationTrigger, 0.1f);
        FaceTarget(player.position);
        Debug.Log("[BrambleSprite] Animation ? Bramble Snare");

        // === WAIT FOR FULL ANIMATION COMPLETION ===
        yield return WaitForAnimationToComplete(snareAnimationTrigger);
        Debug.Log("[BrambleSprite] Close Attack animation fully completed. Now transitioning to Combo Attack.");

        isAttacking = false;
        EnterState(EnemyState.ComboAttack);
    }

    // -----------------------------------------
    //  COMBO ATTACK (Thorn Toss) ? wait cooldown ? back to Scouting
    // -----------------------------------------
    private IEnumerator ComboAttackRoutine()
    {
        Debug.Log("[BrambleSprite] === ENTERING COMBO ATTACK ===");
        
        // Ensure we stay stopped during this attack
        agent.isStopped = true;
        agent.ResetPath();
        
        animator.CrossFade(thornTossAnimationTrigger, 0.1f);
        FaceTarget(player.position);
        Debug.Log("[BrambleSprite] Animation ? Thorn Toss");
        Debug.Log("[BrambleSprite] Animation → Thorn Toss");

        // === WAIT FOR FULL ANIMATION COMPLETION ===
        Debug.Log("[BrambleSprite] Waiting for ComboAttack animation to complete...");
        yield return WaitForAnimationToComplete(thornTossAnimationTrigger);
        Debug.Log("[BrambleSprite] Combo Attack animation fully completed. Now returning to Scouting.");

        // Return to scouting only after animation fully completes
        agent.isStopped = false;
        isAttacking = false;
        Debug.Log("[BrambleSprite] ComboAttack finished. Calling EnterState(Scouting)");
        EnterState(EnemyState.Scouting);
    }

    // -----------------------------------------
    //  BEING HIT — interrupts everything
    // -----------------------------------------
    private IEnumerator BeingHitRoutine()
    {
        isAttacking = false;
        agent.isStopped = true;
        agent.ResetPath();
        animator.SetFloat("Speed", 0f);
        animator.SetTrigger("BeingHit");

        yield return WaitForAnimationToComplete("BeingHit");

        isBeingHit = false;
        agent.isStopped = false;
        EnterState(EnemyState.Scouting);
    }

    // -----------------------------------------
    //  DEATH
    // -----------------------------------------
    private IEnumerator DieRoutine()
    {
        if (!deathEventSent)
        {
            deathEventSent = true;
            OnDeathStarted?.Invoke(this);
        }

        agent.isStopped = true;
        agent.enabled = false;
        animator.SetBool("Dead", true);
        Debug.Log($"{name} died.");
        yield return new WaitForSeconds(5f);
        Destroy(gameObject);
    }

    // -----------------------------------------
    //  ANIMATION EVENTS
    // -----------------------------------------
    public void OnCloseHit()
    {
        if (player == null) return;
        if (Vector3.Distance(transform.position, player.position) <= closeAttackRange + 0.5f)
            DealDamageToPlayer(thornDamage);
    }
    public void OnAttackHit() => OnCloseHit();

    public void OnComboHit()
    {
        if (player == null) return;
        if (Vector3.Distance(transform.position, player.position) <= closeAttackRange + 0.5f)
            DealDamageToPlayer(thornDamage);
    }
    public void OnAttack2Hit() => OnComboHit();

   public void OnSnareCast()
{
    if (player == null) return;

    PlayerMovement movement = player.GetComponentInParent<PlayerMovement>();
    if (movement == null)
        movement = player.GetComponentInChildren<PlayerMovement>();

    if (movement != null)
    {
        movement.ApplyStun(snareStunDuration);
        movement.ApplySpeedDebuff(snareSlowMultiplier, snareDuration);
    }

    if (snareVFXPrefab != null)
    {
        Vector3 spawnPos = player.position;
        spawnPos.y -= 0.9f;

        GameObject vfx = Instantiate(snareVFXPrefab, spawnPos, Quaternion.identity);

        // 🔥 THIS IS THE IMPORTANT PART
        SnareRoot root = vfx.GetComponent<SnareRoot>();
        if (root != null)
        {
            root.Init(player.position);
        }
    }

    Debug.Log("[BrambleSprite] Snare hit executed.");
}

    public void OnThornToss()
    {
        if (thornPrefab == null || thornSpawnPoint == null || player == null)
            return;

        GameObject thorn = Instantiate(thornPrefab, thornSpawnPoint.position, thornSpawnPoint.rotation);
        Vector3 direction = player.position - thornSpawnPoint.position;

        ThornProjectile thornComponent = thorn.GetComponent<ThornProjectile>();
        if (thornComponent != null)
        {
            thornComponent.speed = thornSpeed;
            thornComponent.lifeTime = thornLifeTime;
            thornComponent.Launch(direction, thornDamage);
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

    // -----------------------------------------
    //  HEALTH
    // -----------------------------------------
    public void TakeDamage(float damage)
    {
        if (currentState == EnemyState.Dead) return;
        if (!battleStarted) return;

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

    // -----------------------------------------
    //  HELPERS
    // -----------------------------------------
    private IEnumerator WaitForAnimationToComplete(string stateName)
    {
        if (animator == null) yield break;
        yield return null;
        float timeout = 10f;
        float elapsedTime = 0f;
        while (elapsedTime < timeout)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName(stateName) && stateInfo.normalizedTime >= 1f)
            {
                Debug.Log($"[BrambleSprite] Animation '{stateName}' completed.");
                yield break;
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        Debug.LogWarning($"[BrambleSprite] Animation '{stateName}' timeout.");
    }

    private void FaceTarget(Vector3 targetPos, float turnSpeedDegreesPerSecond = 720f)
    {
        Vector3 dir = targetPos - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(dir.normalized);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                Mathf.Max(0f, turnSpeedDegreesPerSecond) * Time.deltaTime
            );
        }
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
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, closeAttackRange);
    }
}
