using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class BarnacleHuskController : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public NavMeshAgent agent;
    public Transform player;

    [Header("Movement")]
    public float walkSpeed = 3f;
    public float rotationSpeed = 360f;
    public float stoppingDistance = 1.75f;
    public float navMeshSearchRadius = 4f;

    [Header("Barnacle Slash")]
    public float meleeAttackRange = 2f;
    public float meleeHitRange = 2.2f;
    public int meleeDamage = 5;
    public float attackWindupTime = 0.4f;
    public bool useTimedHitFallback = false;
    public float attackFallbackFinishTime = 3f;
    public float attackCooldown = 2f;

    [Header("Recovery")]
    public float recoverTime = 0.6f;
    public float hitFallbackFinishTime = 0.6f;
    public bool useDeathFallback = false;
    public float deathFallbackFinishTime = 2f;

    [Header("Water Burst")]
    public bool enableWaterBurst = true;
    public GameObject waterBurstWarningPrefab;
    public GameObject waterBurstImpactPrefab;
    public TileDebuffManager tileDebuffManager;
    public float waterBurstCooldown = 9f;
    public float waterBurstFirstCastDelay = 2.5f;
    public float waterBurstWarningDelay = 1f;
    public float waterBurstHitRadius = 2.25f;
    public bool matchWarningVisualRadiusToHitRadius = true;
    public float waterBurstWarningVisualRadius = 14.2f;
    public int waterBurstDamage = 6;
    public float waterBurstRecoveryTime = 0.8f;
    public int waterBurstMinLockedTiles = 1;
    public int waterBurstMaxLockedTiles = 2;
    public int waterBurstMinLockCounter = 1;
    public int waterBurstMaxLockCounter = 2;
    public bool useWaterBurstTimedFallback = true;
    public float waterBurstFinishFallbackTime = 1.5f;
    public float waterBurstGroundSnapRadius = 4f;
    public float waterBurstVisualGroundOffset = 0.06f;

    [Header("Death Callback")]
    public MonoBehaviour deathReceiver;
    public string deathMessage = "OnBarnacleHuskDefeated";

    [Header("Potion Drop")]
    public bool dropPotionOnDeath = true;
    public string potionDropLogSource = "BarnacleHusk";
    [Range(0f, 1f)] public float potionDropChance = 1f;
    public int potionDropMinCount = 1;
    public int potionDropMaxCount = 1;
    public int potionDropAmountPerDrop = 1;
    public bool canDropHealthPotion = true;
    public bool canDropPurifyPotion = true;
    public bool canDropPowerUpPotion = true;

    [Header("Animator Parameters")]
    public string speedParameter = "Speed";
    public string barnacleSlashTrigger = "BarnacleSlash";
    public string waterCastTrigger = "WaterCast";
    public string hitTrigger = "BeingHit";
    public string deathTrigger = "Death";

    [Header("Debug Testing")]
    public bool enableDebugKillSwitch = true;
    public KeyCode debugKillKey = KeyCode.L;
    public bool enableDebugWaterBurstKey = true;
    public KeyCode debugWaterBurstKey = KeyCode.B;

    [Header("Debug Gizmos")]
    public bool showRangeGizmos = true;
    public bool showNavMeshSearchGizmo = true;
    public bool showWaterBurstGizmo = true;
    public Color meleeAttackGizmoColor = new Color(1f, 0.2f, 0.2f, 0.3f);
    public Color meleeHitGizmoColor = new Color(1f, 0.65f, 0.05f, 0.25f);
    public Color stoppingDistanceGizmoColor = new Color(0.25f, 0.6f, 1f, 0.25f);
    public Color navMeshSearchGizmoColor = new Color(0.1f, 1f, 0.8f, 0.2f);
    public Color waterBurstGizmoColor = new Color(0.1f, 0.85f, 1f, 0.35f);

    private bool battleStarted = false;
    private bool isAttacking = false;
    private bool isRecovering = false;
    private bool isCastingWaterBurst = false;
    private bool isBeingHit = false;
    private bool isDead = false;
    private bool slashHitResolved = false;
    private bool deathFinished = false;
    private bool loggedMissingNavMesh = false;
    private bool logClawCooldownWhenRecoverEnds = false;
    private float nextWaterBurstTime = 0f;
    private Vector3 waterBurstTargetPosition;
    private Vector3 lastWaterBurstTargetPosition;
    private bool hasWaterBurstTarget = false;
    private bool waterBurstWarningStarted = false;
    private bool waterBurstImpactResolved = false;
    private bool potionDropResolved = false;

    private Coroutine attackFallbackCoroutine;
    private Coroutine recoverCoroutine;
    private Coroutine waterBurstCoroutine;
    private Coroutine waterBurstVisualCleanupCoroutine;
    private Coroutine hitFallbackCoroutine;
    private Coroutine deathFallbackCoroutine;
    private GameObject activeWaterBurstWarning;
    private GameObject activeWaterBurstImpact;

    private void Awake()
    {
        agent ??= GetComponent<NavMeshAgent>();
        animator ??= GetComponent<Animator>();

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
            ConfigureAgent();

            if (battleStarted && TryPrepareAgentForMovement())
                agent.isStopped = false;
            else if (agent.enabled && agent.isOnNavMesh)
                agent.isStopped = true;
        }

        if (animator != null)
        {
            animator.applyRootMotion = false;
            SetAnimatorSpeed(0f);
        }
    }

    public void StartBattle()
    {
        if (battleStarted || isDead)
            return;

        battleStarted = true;
        isAttacking = false;
        isRecovering = false;
        isCastingWaterBurst = false;
        isBeingHit = false;
        slashHitResolved = false;
        potionDropResolved = false;
        logClawCooldownWhenRecoverEnds = false;
        nextWaterBurstTime = Time.time + Mathf.Max(0f, waterBurstFirstCastDelay);

        StopAttackFallback();
        StopRecover();
        StopWaterBurst();
        StopHitFallback();
        StopDeathFallback();

        if (agent != null)
        {
            ConfigureAgent();

            if (TryPrepareAgentForMovement())
            {
                agent.isStopped = false;
                agent.ResetPath();
            }
        }

        Debug.Log("[BarnacleHusk] Battle started.");
    }

    private void Update()
    {
        HandleDebugInput();

        if (isDead)
        {
            StopMovement();
            return;
        }

        if (!battleStarted || player == null)
        {
            SetAnimatorSpeed(0f);
            return;
        }

        if (isAttacking || isRecovering || isCastingWaterBurst || isBeingHit)
        {
            StopMovement();
            FaceTarget(player.position);
            return;
        }

        DecideAction();
    }

    private void DecideAction()
    {
        float distance = GetFlatDistanceToPlayer();

        if (CanStartWaterBurst())
        {
            TriggerWaterBurst();
            return;
        }

        if (distance <= GetEffectiveMeleeAttackRange())
        {
            TriggerBarnacleSlash();
            return;
        }

        ChasePlayer();
    }

    private void HandleDebugInput()
    {
        if (isDead)
            return;

        if (enableDebugKillSwitch && Input.GetKeyDown(debugKillKey))
            DebugKill();

        if (enableDebugWaterBurstKey && battleStarted && Input.GetKeyDown(debugWaterBurstKey))
        {
            nextWaterBurstTime = Time.time;
            TriggerWaterBurst();
        }
    }

    public void DebugKill()
    {
        if (isDead)
            return;

        Debug.Log("[BarnacleHusk] Debug kill switch triggered.");

        EnemyHealth enemyHealth = GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            float lethalDamage = Mathf.Max(enemyHealth.currentHealth, enemyHealth.maxHealth);
            enemyHealth.TakeDamage(lethalDamage);
            return;
        }

        TriggerDeath();
    }

    private void ChasePlayer()
    {
        if (!TryPrepareAgentForMovement())
        {
            FaceTarget(player.position);
            SetAnimatorSpeed(0f);
            return;
        }

        float distance = GetFlatDistanceToPlayer();
        if (distance <= stoppingDistance)
        {
            StopMovement();
            FaceTarget(player.position);
            return;
        }

        agent.isStopped = false;
        agent.speed = walkSpeed;
        agent.stoppingDistance = stoppingDistance;
        agent.SetDestination(player.position);

        FaceTarget(player.position);
        SetAnimatorSpeed(agent.velocity.magnitude);
    }

    private void TriggerBarnacleSlash()
    {
        if (isAttacking || isRecovering || isBeingHit || isDead)
            return;

        isAttacking = true;
        slashHitResolved = false;

        StopMovement();
        FaceTarget(player.position);

        TrySetAnimatorTrigger(barnacleSlashTrigger);

        Debug.Log("[BarnacleHusk] Claw attack started.");

        StopAttackFallback();
        attackFallbackCoroutine = StartCoroutine(BarnacleSlashFallbackRoutine());
    }

    // Animation Event: place this on the BarnacleSlash damage/contact frame.
    public void OnHit()
    {
        Debug.Log("[BarnacleHusk] OnHit event fired.");
        ResolveBarnacleSlashHit();
    }

    // Optional clearer alias if you prefer this name on later clips.
    public void OnBarnacleSlashHit()
    {
        Debug.Log("[BarnacleHusk] BarnacleSlash hit event fired.");
        ResolveBarnacleSlashHit();
    }

    // Animation Event: place this on the final frame of BarnacleSlash.
    public void OnBarnacleSlashIsFinished()
    {
        Debug.Log("[BarnacleHusk] BarnacleSlash finished event fired.");
        FinishBarnacleSlash(true);
    }

    private IEnumerator BarnacleSlashFallbackRoutine()
    {
        yield return new WaitForSeconds(attackWindupTime);

        if (useTimedHitFallback && isAttacking && !slashHitResolved)
            ResolveBarnacleSlashHit();

        float finishDelay = Mathf.Max(0f, attackFallbackFinishTime - attackWindupTime);
        yield return new WaitForSeconds(finishDelay);

        if (isAttacking)
        {
            if (!slashHitResolved && !useTimedHitFallback)
            {
                Debug.LogWarning("[BarnacleHusk] BarnacleSlash finished by fallback before hit event fired. Add OnBarnacleSlashHit to the impact frame, or enable Use Timed Hit Fallback for timing tests.");
            }

            FinishBarnacleSlash(false);
        }

        attackFallbackCoroutine = null;
    }

    private void ResolveBarnacleSlashHit()
    {
        if (!isAttacking || slashHitResolved)
            return;

        slashHitResolved = true;
        float hitDistance = GetFlatDistanceToPlayerHitTarget();

        if (player != null && hitDistance <= meleeHitRange)
        {
            DealDamageToPlayer(meleeDamage);
            Debug.Log($"[BarnacleHusk] Claw hit player. Distance: {hitDistance:F2}/{meleeHitRange:F2}");
            return;
        }

        Debug.Log($"[BarnacleHusk] Claw missed. Distance: {hitDistance:F2}/{meleeHitRange:F2}");
    }

    private void FinishBarnacleSlash(bool stopFallbackRoutine)
    {
        if (!isAttacking)
            return;

        if (!slashHitResolved)
        {
            slashHitResolved = true;
            Debug.LogWarning("[BarnacleHusk] BarnacleSlash finished before damage was resolved. Check the OnBarnacleSlashHit animation event.");
        }

        isAttacking = false;

        if (stopFallbackRoutine)
            StopAttackFallback();

        EnterRecover(attackCooldown, true);
    }

    private void EnterRecover(float duration, bool logClawCooldown = false)
    {
        if (isDead)
            return;

        isRecovering = true;
        logClawCooldownWhenRecoverEnds = logClawCooldown;
        StopMovement();

        StopRecover();
        recoverCoroutine = StartCoroutine(RecoverRoutine(Mathf.Max(0f, duration)));
    }

    private IEnumerator RecoverRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);

        isRecovering = false;
        recoverCoroutine = null;

        if (logClawCooldownWhenRecoverEnds)
        {
            logClawCooldownWhenRecoverEnds = false;
            Debug.Log("[BarnacleHusk] Claw cooldown finished.");
        }
    }

    private bool CanStartWaterBurst()
    {
        return enableWaterBurst
            && battleStarted
            && player != null
            && !isAttacking
            && !isRecovering
            && !isCastingWaterBurst
            && !isBeingHit
            && !isDead
            && Time.time >= nextWaterBurstTime;
    }

    public void TriggerWaterBurst()
    {
        if (!CanStartWaterBurst())
            return;

        isCastingWaterBurst = true;
        waterBurstWarningStarted = false;
        waterBurstImpactResolved = false;
        nextWaterBurstTime = Time.time + Mathf.Max(0f, waterBurstCooldown);

        StopMovement();
        FaceTarget(player.position);
        TrySetAnimatorTrigger(waterCastTrigger);

        Debug.Log("[WaterBurst] Cast started.");

        StopWaterBurstCoroutineOnly();
        waterBurstCoroutine = StartCoroutine(WaterBurstCastFallbackRoutine());
    }

    private IEnumerator WaterBurstCastFallbackRoutine()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, waterBurstFinishFallbackTime));

        if (useWaterBurstTimedFallback && isCastingWaterBurst && !waterBurstWarningStarted)
            BeginWaterBurstWarning(false);
    }

    private IEnumerator WaterBurstWarningRoutine()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, waterBurstWarningDelay));

        if (isCastingWaterBurst && !waterBurstImpactResolved)
            ResolveWaterBurstImpact(false);

        FinishWaterBurst(false);
        waterBurstCoroutine = null;
    }

    // Animation Event: place this where the ground warning should start.
    public void OnWaterBurstWarning()
    {
        Debug.Log("[WaterBurst] Warning event fired.");
        BeginWaterBurstWarning(true);
    }

    // Backwards-compatible alias: if this is placed before warning starts, it starts the warning phase.
    public void OnWaterBurstImpact()
    {
        Debug.Log("[WaterBurst] Impact event fired.");

        if (!waterBurstWarningStarted)
        {
            BeginWaterBurstWarning(true);
            return;
        }

        ResolveWaterBurstImpact(true);
        FinishWaterBurst(true);
    }

    // Animation Event: place this on the final frame of WaterCast to start the dodge warning.
    public void OnWaterCastFinished()
    {
        Debug.Log("[WaterBurst] Cast finished event fired.");
        BeginWaterBurstWarning(true);
    }

    private void BeginWaterBurstWarning(bool stopExistingRoutine)
    {
        if (!isCastingWaterBurst || waterBurstWarningStarted)
            return;

        if (stopExistingRoutine)
            StopWaterBurstCoroutineOnly();

        waterBurstWarningStarted = true;
        Vector3 rawTargetPosition = player != null ? player.position : transform.position;
        waterBurstTargetPosition = ResolveWaterBurstGroundPosition(rawTargetPosition);
        lastWaterBurstTargetPosition = waterBurstTargetPosition;
        hasWaterBurstTarget = true;
        Debug.Log("[WaterBurst] Target position saved.");

        activeWaterBurstWarning = SpawnWaterBurstVFX(waterBurstWarningPrefab, waterBurstTargetPosition);
        ConfigureWarningVFX(activeWaterBurstWarning);
        Debug.Log("[WaterBurst] Warning spawned.");

        waterBurstCoroutine = StartCoroutine(WaterBurstWarningRoutine());
    }

    private void ResolveWaterBurstImpact(bool fromAnimationEvent)
    {
        if (!isCastingWaterBurst || waterBurstImpactResolved)
            return;

        waterBurstImpactResolved = true;

        activeWaterBurstImpact = SpawnWaterBurstVFX(waterBurstImpactPrefab, waterBurstTargetPosition);
        ScheduleWaterBurstVisualCleanup(activeWaterBurstImpact);
        Debug.Log("[WaterBurst] Burst spawned.");

        if (player != null && GetFlatDistanceToPoint(player.position, waterBurstTargetPosition) <= waterBurstHitRadius)
        {
            DealDamageToPlayer(waterBurstDamage);
            Debug.Log("[WaterBurst] Player hit.");
            ApplyWaterBurstTileLocking();
        }
        else
        {
            Debug.Log("[WaterBurst] Player dodged.");
        }
    }

    private void FinishWaterBurst(bool stopFallbackRoutine)
    {
        if (!isCastingWaterBurst)
            return;

        if (!waterBurstWarningStarted)
        {
            BeginWaterBurstWarning(false);
            return;
        }

        if (!waterBurstImpactResolved)
        {
            ResolveWaterBurstImpact(false);
        }

        isCastingWaterBurst = false;
        waterBurstWarningStarted = false;
        waterBurstImpactResolved = false;

        if (stopFallbackRoutine)
            StopWaterBurstCoroutineOnly();

        EnterRecover(waterBurstRecoveryTime);
    }

    private GameObject SpawnWaterBurstVFX(GameObject prefab, Vector3 position)
    {
        if (prefab == null)
            return null;

        return Instantiate(prefab, position, Quaternion.identity);
    }

    private void ConfigureWarningVFX(GameObject warning)
    {
        if (warning == null)
            return;

        WaterBurstWarningVFX warningVFX = warning.GetComponent<WaterBurstWarningVFX>();
        if (warningVFX == null)
            return;

        warningVFX.duration = Mathf.Max(0.01f, waterBurstWarningDelay + GetWaterBurstImpactLifetime(waterBurstImpactPrefab));
        warningVFX.radius = matchWarningVisualRadiusToHitRadius
            ? waterBurstHitRadius
            : waterBurstWarningVisualRadius;
    }

    private void ScheduleWaterBurstVisualCleanup(GameObject impact)
    {
        StopWaterBurstVisualCleanup();
        float cleanupDelay = GetWaterBurstImpactLifetime(impact) + 0.05f;
        waterBurstVisualCleanupCoroutine = StartCoroutine(CleanupWaterBurstVisualsAfterDelay(cleanupDelay));
    }

    private IEnumerator CleanupWaterBurstVisualsAfterDelay(float delay)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, delay));

        if (activeWaterBurstWarning != null)
        {
            Destroy(activeWaterBurstWarning);
            activeWaterBurstWarning = null;
        }

        if (activeWaterBurstImpact != null)
        {
            Destroy(activeWaterBurstImpact);
            activeWaterBurstImpact = null;
        }

        waterBurstVisualCleanupCoroutine = null;
    }

    private float GetWaterBurstImpactLifetime(GameObject source)
    {
        if (source == null)
            return 1.5f;

        WaterBurstImpactVFX impactVFX = source.GetComponent<WaterBurstImpactVFX>();
        if (impactVFX == null)
            return 1.5f;

        float animationLifetime = Mathf.Max(0.01f, impactVFX.riseDuration)
            + Mathf.Max(0f, impactVFX.peakHoldDuration)
            + Mathf.Max(0.01f, impactVFX.collapseDuration);

        return Mathf.Max(impactVFX.duration, animationLifetime);
    }

    private void ApplyWaterBurstTileLocking()
    {
        if (tileDebuffManager == null)
            tileDebuffManager = FindFirstObjectByType<TileDebuffManager>();

        if (tileDebuffManager == null)
            return;

        int minTiles = Mathf.Min(waterBurstMinLockedTiles, waterBurstMaxLockedTiles);
        int maxTiles = Mathf.Max(waterBurstMinLockedTiles, waterBurstMaxLockedTiles);
        int minCounter = Mathf.Min(waterBurstMinLockCounter, waterBurstMaxLockCounter);
        int maxCounter = Mathf.Max(waterBurstMinLockCounter, waterBurstMaxLockCounter);
        int tileCount = Random.Range(Mathf.Max(0, minTiles), Mathf.Max(0, maxTiles) + 1);
        int counter = Random.Range(Mathf.Max(1, minCounter), Mathf.Max(1, maxCounter) + 1);

        tileDebuffManager.ApplyTileLocking(tileCount, counter);
        Debug.Log("[WaterBurst] Applied tile locking.");
    }

    public void TriggerBeingHit()
    {
        if (!battleStarted || isDead || isBeingHit)
            return;

        Debug.Log("[BarnacleHusk] Hit triggered.");

        isBeingHit = true;
        isAttacking = false;
        isRecovering = false;
        slashHitResolved = true;
        logClawCooldownWhenRecoverEnds = false;

        StopAttackFallback();
        StopRecover();
        StopWaterBurst();
        StopMovement();
        ResetActionTriggers();

        if (TrySetAnimatorTrigger(hitTrigger, false))
        {
            StopHitFallback();
            hitFallbackCoroutine = StartCoroutine(HitFallbackRoutine());
        }
        else
        {
            OnBeingHitFinished();
        }
    }

    // Animation Event: place this on the final frame of the Hit animation.
    public void OnBeingHitFinished()
    {
        if (!isBeingHit)
            return;

        Debug.Log("[BarnacleHusk] Hit finished.");

        StopHitFallback();
        isBeingHit = false;
        EnterRecover(recoverTime);
    }

    // Optional Barnacle-specific alias for animation events.
    public void OnBarnacleHuskHitFinished()
    {
        OnBeingHitFinished();
    }

    public void TriggerDeath()
    {
        if (isDead)
            return;

        Debug.Log("[BarnacleHusk] Death triggered.");

        isDead = true;
        deathFinished = false;
        battleStarted = false;
        isAttacking = false;
        isRecovering = false;
        isCastingWaterBurst = false;
        isBeingHit = false;
        slashHitResolved = true;
        logClawCooldownWhenRecoverEnds = false;

        StopAttackFallback();
        StopRecover();
        StopWaterBurst();
        StopHitFallback();
        StopDeathFallback();
        StopMovement();

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

        ResetActionTriggers();

        if (TrySetAnimatorTrigger(deathTrigger, false))
        {
            if (useDeathFallback)
                deathFallbackCoroutine = StartCoroutine(DeathFallbackRoutine());
        }
        else
        {
            OnDeathFinished();
        }
    }

    // Animation Event: place this on the final frame of the Death animation.
    public void OnDeathFinished()
    {
        if (deathFinished)
            return;

        deathFinished = true;
        StopDeathFallback();

        Debug.Log("[BarnacleHusk] Death finished.");

        TryDropPotionReward();

        if (deathReceiver != null)
            deathReceiver.SendMessage(deathMessage, SendMessageOptions.DontRequireReceiver);

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

    private IEnumerator HitFallbackRoutine()
    {
        yield return new WaitForSeconds(hitFallbackFinishTime);
        OnBeingHitFinished();
    }

    private IEnumerator DeathFallbackRoutine()
    {
        yield return new WaitForSeconds(deathFallbackFinishTime);
        OnDeathFinished();
    }

    private void StopAttackFallback()
    {
        if (attackFallbackCoroutine == null)
            return;

        StopCoroutine(attackFallbackCoroutine);
        attackFallbackCoroutine = null;
    }

    private void StopRecover()
    {
        if (recoverCoroutine == null)
            return;

        StopCoroutine(recoverCoroutine);
        recoverCoroutine = null;
    }

    private void StopWaterBurst()
    {
        StopWaterBurstCoroutineOnly();
        StopWaterBurstVisualCleanup();
        isCastingWaterBurst = false;
        waterBurstWarningStarted = false;
        waterBurstImpactResolved = false;

        if (activeWaterBurstWarning != null)
        {
            Destroy(activeWaterBurstWarning);
            activeWaterBurstWarning = null;
        }

        if (activeWaterBurstImpact != null)
        {
            Destroy(activeWaterBurstImpact);
            activeWaterBurstImpact = null;
        }
    }

    private void StopWaterBurstCoroutineOnly()
    {
        if (waterBurstCoroutine == null)
            return;

        StopCoroutine(waterBurstCoroutine);
        waterBurstCoroutine = null;
    }

    private void StopWaterBurstVisualCleanup()
    {
        if (waterBurstVisualCleanupCoroutine == null)
            return;

        StopCoroutine(waterBurstVisualCleanupCoroutine);
        waterBurstVisualCleanupCoroutine = null;
    }

    private void StopHitFallback()
    {
        if (hitFallbackCoroutine == null)
            return;

        StopCoroutine(hitFallbackCoroutine);
        hitFallbackCoroutine = null;
    }

    private void StopDeathFallback()
    {
        if (deathFallbackCoroutine == null)
            return;

        StopCoroutine(deathFallbackCoroutine);
        deathFallbackCoroutine = null;
    }

    private void ConfigureAgent()
    {
        if (agent == null)
            return;

        agent.speed = walkSpeed;
        agent.updateRotation = false;
        agent.updatePosition = true;
        agent.stoppingDistance = stoppingDistance;
        agent.autoBraking = true;
    }

    private bool TryPrepareAgentForMovement()
    {
        if (agent == null || !agent.enabled)
            return false;

        if (agent.isOnNavMesh)
            return true;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, navMeshSearchRadius, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            loggedMissingNavMesh = false;
            return agent.isOnNavMesh;
        }

        if (!loggedMissingNavMesh)
        {
            Debug.LogWarning("[BarnacleHusk] NavMeshAgent is not on a NavMesh.");
            loggedMissingNavMesh = true;
        }

        return false;
    }

    private float GetFlatDistanceToPlayer()
    {
        if (player == null)
            return 0f;

        Vector3 currentPosition = transform.position;
        Vector3 playerPosition = player.position;
        currentPosition.y = 0f;
        playerPosition.y = 0f;
        return Vector3.Distance(currentPosition, playerPosition);
    }

    private float GetFlatDistanceToPlayerHitTarget()
    {
        if (player == null)
            return Mathf.Infinity;

        return GetFlatDistanceToPlayer();
    }

    private float GetFlatDistanceToPoint(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private Vector3 ResolveWaterBurstGroundPosition(Vector3 position)
    {
        Vector3 resolvedPosition = position;

        if (NavMesh.SamplePosition(position, out NavMeshHit navMeshHit, waterBurstGroundSnapRadius, NavMesh.AllAreas))
        {
            resolvedPosition.y = navMeshHit.position.y;
        }
        else if (Physics.Raycast(position + Vector3.up * waterBurstGroundSnapRadius, Vector3.down, out RaycastHit groundHit, waterBurstGroundSnapRadius * 3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            resolvedPosition.y = groundHit.point.y;
        }

        resolvedPosition.y += waterBurstVisualGroundOffset;
        return resolvedPosition;
    }

    private float GetEffectiveMeleeAttackRange()
    {
        return Mathf.Max(meleeAttackRange, stoppingDistance + 0.1f);
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

    private void FaceTarget(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private void SetAnimatorSpeed(float speedValue)
    {
        if (animator != null && !string.IsNullOrWhiteSpace(speedParameter))
            animator.SetFloat(speedParameter, speedValue);
    }

    private bool TrySetAnimatorTrigger(string triggerName, bool warnIfMissing = true)
    {
        if (animator == null || string.IsNullOrWhiteSpace(triggerName))
            return false;

        int triggerHash = Animator.StringToHash(triggerName);
        if (!HasAnimatorParameter(triggerHash, AnimatorControllerParameterType.Trigger))
        {
            if (warnIfMissing)
                Debug.LogWarning($"[BarnacleHusk] Animator is missing the {triggerName} trigger.");

            return false;
        }

        animator.SetTrigger(triggerHash);
        return true;
    }

    private void ResetActionTriggers()
    {
        ResetAnimatorTrigger(barnacleSlashTrigger);
        ResetAnimatorTrigger(waterCastTrigger);
        ResetAnimatorTrigger(hitTrigger);
    }

    private void ResetAnimatorTrigger(string triggerName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(triggerName))
            return;

        int triggerHash = Animator.StringToHash(triggerName);
        if (HasAnimatorParameter(triggerHash, AnimatorControllerParameterType.Trigger))
            animator.ResetTrigger(triggerHash);
    }

    private bool HasAnimatorParameter(int parameterHash, AnimatorControllerParameterType parameterType)
    {
        if (animator == null)
            return false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == parameterHash && parameter.type == parameterType)
                return true;
        }

        return false;
    }

    private void DealDamageToPlayer(float damage)
    {
        if (player == null)
            return;

        IDamageable damageable = player.GetComponentInChildren<IDamageable>();
        if (damageable == null)
            damageable = player.GetComponentInParent<IDamageable>();

        if (damageable != null)
            damageable.ApplyDamage(damage);
        else
            player.SendMessage("ApplyDamage", damage, SendMessageOptions.DontRequireReceiver);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showRangeGizmos)
            return;

        Vector3 center = transform.position;
        center.y += 0.1f;

        DrawFilledAndWireSphere(center, GetEffectiveMeleeAttackRange(), meleeAttackGizmoColor);
        DrawFilledAndWireSphere(center, meleeHitRange, meleeHitGizmoColor);
        DrawWireSphere(center, stoppingDistance, stoppingDistanceGizmoColor);

        if (showNavMeshSearchGizmo)
            DrawWireSphere(transform.position, navMeshSearchRadius, navMeshSearchGizmoColor);

        if (showWaterBurstGizmo)
            DrawWaterBurstGizmo(center);
    }

    private void DrawWaterBurstGizmo(Vector3 barnacleCenter)
    {
        Vector3 targetCenter = hasWaterBurstTarget ? lastWaterBurstTargetPosition : barnacleCenter;
        targetCenter.y += 0.06f;

        DrawFilledAndWireSphere(targetCenter, waterBurstHitRadius, waterBurstGizmoColor);

        Gizmos.color = new Color(waterBurstGizmoColor.r, waterBurstGizmoColor.g, waterBurstGizmoColor.b, 1f);
        Gizmos.DrawLine(barnacleCenter, targetCenter);
    }

    private void DrawFilledAndWireSphere(Vector3 center, float radius, Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawSphere(center, radius);

        DrawWireSphere(center, radius, new Color(color.r, color.g, color.b, 1f));
    }

    private void DrawWireSphere(Vector3 center, float radius, Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawWireSphere(center, radius);
    }
}
