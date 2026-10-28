using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Tide Wraith foundation controller.
///
/// Current approved layer:
/// - encounter activation
/// - NavMesh walk approach to a configurable ranged stopping distance
/// - Brine Bolt basic ranged attack with animation-event release timing
/// - Riptide Eruption fixed-position pressure ability
/// - Tidal Wave fixed-direction damage, knockback, and stun ability
/// - hit reaction and death completion
/// </summary>
public class TideWraithController : MonoBehaviour
{
    private enum AbilityChoice
    {
        None,
        BrineBolt,
        Riptide,
        TidalWave
    }

    [Header("Core References")]
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform actorRoot;
    [SerializeField] private Transform player;

    [Header("World Tile Spawn Area")]
    [Tooltip("Tide Wraith arena used by the shared TileSpawner. Falls back to TD_ArenaCenter when unassigned.")]
    [SerializeField] private Transform tileSpawnArenaCenter;
    [SerializeField, Min(0.5f)] private float tileSpawnArenaRadius = 71.9f;
    [SerializeField] private Transform tileSpawnExclusionCenter;
    [SerializeField, Min(0f)] private float tileSpawnExclusionRadius;

    [Header("Approach Movement")]
    [Tooltip("Maximum walking speed while approaching the ranged stopping distance.")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 3.5f;
    [Tooltip("Time required to reach the currently configured maximum speed.")]
    [SerializeField, Min(0.05f)] private float accelerationTime = 0.5f;
    [Tooltip("Brine Bolt's long-range engagement distance and the Wraith's normal stopping distance.")]
    [SerializeField, Min(0f)] private float preferredMinimumRange = 5f;
    [SerializeField, Min(1f)] private float rotationSpeed = 420f;
    [SerializeField, Min(0.1f)] private float navMeshSearchRadius = 4f;

    [Header("Brine Bolt Basic Attack")]
    [SerializeField] private Transform brineBoltOrigin;
    [SerializeField] private BrineBoltProjectile brineBoltProjectilePrefab;
    [SerializeField, Min(0.1f)] private float brineBoltCooldown = 2.5f;
    [SerializeField, Min(0f)] private float brineBoltInitialDelay = 1f;
    [SerializeField, Min(0f)] private float brineBoltDamage = 10f;
    [SerializeField, Min(0.1f)] private float brineBoltProjectileSpeed = 65f;
    [SerializeField, Min(0.1f)] private float brineBoltProjectileLifetime = 4f;
    [SerializeField, Min(0.05f)] private float brineBoltHitRadius = 0.45f;
    [Tooltip("Fallback release time used only when the animation event does not fire.")]
    [SerializeField, Min(0.01f)] private float brineBoltReleaseFallback = 0.55f;
    [Tooltip("Fallback total cast time used only when the animation-finished event does not fire.")]
    [SerializeField, Min(0.05f)] private float brineBoltFinishFallback = 1.15f;

    [Header("Riptide Eruption Pressure Ability")]
    [SerializeField] private GameObject riptideWarningPrefab;
    [Tooltip("Visual-only eruption spawned when the Riptide resolve Animation Event fires. It does not control damage or radius.")]
    [SerializeField] private GameObject riptideImpactPrefab;
    [Tooltip("Mid-range distance at which Riptide Eruption may begin. Keep this below the Brine Bolt range.")]
    [SerializeField, Min(0.1f)] private float riptideCastRange = 90f;
    [SerializeField, Min(0.1f)] private float riptideCooldown = 8f;
    [SerializeField, Min(0f)] private float riptideInitialDelay = 4f;
    [SerializeField, Min(0f)] private float riptideDamage = 18f;
    [SerializeField, Min(0.1f)] private float riptideRadius = 3.5f;
    [SerializeField, Min(0.01f)] private float riptideResolveFallback = 0.9f;
    [SerializeField, Min(0.05f)] private float riptideFinishFallback = 1.4f;

    [Header("Tidal Wave Signature Ability")]
    [SerializeField] private Transform tidalWaveOrigin;
    [SerializeField] private TidalWaveProjectile tidalWaveProjectilePrefab;
    [Tooltip("Shortest-range ability. Tidal Wave receives priority when the player is inside this range and the ability is ready.")]
    [SerializeField, Min(0.1f)] private float tidalWaveCastRange = 65f;
    [SerializeField, Min(0.1f)] private float tidalWaveCooldown = 12f;
    [SerializeField, Min(0f)] private float tidalWaveInitialDelay = 6f;
    [SerializeField, Min(0f)] private float tidalWaveDamage = 22f;
    [SerializeField, Min(0.1f)] private float tidalWaveProjectileSpeed = 45f;
    [SerializeField, Min(0.1f)] private float tidalWaveProjectileLifetime = 3f;
    [SerializeField, Min(0.1f)] private float tidalWaveHitRadius = 4f;
    [SerializeField, Min(0f)] private float tidalWaveKnockbackForce = 18f;
    [SerializeField, Min(0f)] private float tidalWaveStunDuration = 1.25f;
    [Tooltip("Safety release used only if the event at approximately 1.97 seconds is missing.")]
    [SerializeField, Min(0.01f)] private float tidalWaveReleaseFallback = 2.15f;
    [Tooltip("Safety finish used only if the event at approximately 3.53 seconds is missing.")]
    [SerializeField, Min(0.05f)] private float tidalWaveFinishFallback = 3.75f;

    [Header("Reaction Timing Fallbacks")]
    [SerializeField, Min(0.05f)] private float hitFinishFallback = 0.75f;
    [SerializeField, Min(0.05f)] private float deathFinishFallback = 2f;

    [Header("Ability Decision Lock")]
    [Tooltip("The player must remain in the same eligible ability band for this duration before that ability is committed.")]
    [SerializeField, Min(0f)] private float abilityRangeCommitDuration = 0.5f;

    [Header("Encounter Completion")]
    [SerializeField] private MonoBehaviour deathReceiver;
    [SerializeField] private string deathMessage = "OnEnemyDefeated";
    [SerializeField] private GameObject rootToDisableOnDeath;

    [Header("Animator Parameters")]
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private string beingHitTrigger = "BeingHit";
    [SerializeField] private string deathTrigger = "Dead";
    [Tooltip("Animator state that must immediately override every active behaviour when the Tide Wraith dies.")]
    [SerializeField] private string deathStateName = "Death";
    [SerializeField] private string brineBoltTrigger = "BrineBolt";
    [SerializeField] private string riptideEruptionTrigger = "RiptideEruption";
    [SerializeField] private string tidalWaveTrigger = "TidalWave";

    [Header("Debug")]
    [SerializeField] private bool enableDebugStartBattleKey;
    [SerializeField] private KeyCode debugStartBattleKey = KeyCode.T;
    [SerializeField] private bool debugLogs = true;
    [Tooltip("Seconds between runtime decision-state diagnostics while Debug Logs is enabled.")]
    [SerializeField, Min(0.1f)] private float debugStateLogInterval = 0.5f;
    [SerializeField] private bool showGizmos = true;

    private bool battleStarted;
    private bool isBeingHit;
    private bool isDead;
    private bool deathFinished;
    private bool isCastingBrineBolt;
    private bool brineBoltReleased;
    private float nextBrineBoltTime;
    private bool isCastingRiptide;
    private bool riptideResolved;
    private float nextRiptideTime;
    private Vector3 capturedRiptidePosition;
    private GameObject activeRiptideWarning;
    private bool isCastingTidalWave;
    private bool tidalWaveReleased;
    private float nextTidalWaveTime;
    private Vector3 capturedTidalWaveDirection;
    private float nextDebugStateLogTime;
    private bool loggedMissingNavMesh;
    private Coroutine hitRoutine;
    private Coroutine deathRoutine;
    private Coroutine brineBoltRoutine;
    private Coroutine riptideRoutine;
    private Coroutine tidalWaveRoutine;
    private AbilityChoice pendingAbility;
    private float pendingAbilitySince;

    public bool BattleStarted => battleStarted;
    public bool IsDead => isDead;

    private Transform ActorTransform => actorRoot != null ? actorRoot : transform;
    private Vector3 ActorPosition => ActorTransform.position;

    private void Awake()
    {
        ResolveReferences();
        ConfigureAgent();
    }

    private void Start()
    {
        StopMovement();
        SetAnimatorSpeed(0f);
    }

    private void OnEnable()
    {
        ResolveReferences();
        ConfigureAgent();
        StopMovement();
        SetAnimatorSpeed(0f);
    }

    private void OnDisable()
    {
        ResetPendingAbility();
        StopRoutine(ref hitRoutine);
        StopRoutine(ref deathRoutine);
        CancelBrineBolt(false);
        CancelRiptide(false);
        CancelTidalWave(false);
    }

    private void Update()
    {
        if (enableDebugStartBattleKey && !battleStarted && !isDead && Input.GetKeyDown(debugStartBattleKey))
            StartBattle();

        TrackRuntimeState();

        if (isDead)
        {
            StopMovement();
            SetAnimatorSpeed(0f);
            return;
        }

        if (!battleStarted || !ResolvePlayer())
        {
            SetAnimatorSpeed(0f);
            return;
        }

        if (isCastingBrineBolt || isCastingRiptide || isCastingTidalWave || isBeingHit)
        {
            StopMovement();
            SetAnimatorSpeed(0f);
            FacePlayer();
            return;
        }

        DecideAction();
    }

    public void StartBattle()
    {
        if (battleStarted || isDead)
            return;

        ResolveReferences();
        ConfigureAgent();
        ConfigureTileSpawnArea();

        battleStarted = true;
        isBeingHit = false;
        deathFinished = false;
        isCastingBrineBolt = false;
        brineBoltReleased = false;
        isCastingRiptide = false;
        riptideResolved = false;
        isCastingTidalWave = false;
        tidalWaveReleased = false;
        ResetPendingAbility();
        nextBrineBoltTime = Time.time + Mathf.Max(0f, brineBoltInitialDelay);
        nextRiptideTime = Time.time + Mathf.Max(0f, riptideInitialDelay);
        nextTidalWaveTime = Time.time + Mathf.Max(0f, tidalWaveInitialDelay);

        if (!TryPrepareAgentForMovement())
        {
            StopMovement();
            SetAnimatorSpeed(0f);
        }

        Log("Battle started. Approach layer active.");
    }

    private void ConfigureTileSpawnArea()
    {
        if (TileSpawner.Instance == null)
        {
            Debug.LogError("[TideWraith] Cannot configure replacement-tile spawning because TileSpawner.Instance is missing.", this);
            return;
        }

        if (tileSpawnArenaCenter == null)
        {
            GameObject fallbackCenter = GameObject.Find("TD_ArenaCenter");
            if (fallbackCenter != null)
                tileSpawnArenaCenter = fallbackCenter.transform;
        }

        if (tileSpawnArenaCenter == null)
        {
            Debug.LogError("[TideWraith] Tile Spawn Arena Center is unassigned and TD_ArenaCenter was not found.", this);
            return;
        }

        TileSpawner.Instance.ConfigureArenaBounds(
            tileSpawnArenaCenter,
            tileSpawnArenaRadius,
            tileSpawnExclusionCenter,
            tileSpawnExclusionRadius
        );

        Log($"Replacement tiles configured for {tileSpawnArenaCenter.name} (radius {tileSpawnArenaRadius:0.##}).");
    }

    private void DecideAction()
    {
        float distance = GetFlatDistanceToPlayer();
        AbilityChoice eligibleAbility = GetHighestPriorityEligibleAbility(distance);

        if (TryCommitAbility(eligibleAbility))
            return;

        if (distance > GetEffectiveBrineBoltRange())
        {
            ChasePlayer();
            return;
        }

        HoldRangedPosition();
    }

    private AbilityChoice GetHighestPriorityEligibleAbility(float distance)
    {
        // Special abilities keep priority inside their intended bands. When
        // that special is cooling down, Brine Bolt acts as the repeatable
        // ranged basic attack instead of leaving Tide Wraith idle.
        if (distance <= GetEffectiveTidalWaveRange())
        {
            if (CanStartTidalWave(distance))
                return AbilityChoice.TidalWave;

            return CanStartBrineBolt(distance) ? AbilityChoice.BrineBolt : AbilityChoice.None;
        }

        if (distance <= GetEffectiveRiptideRange())
        {
            if (CanStartRiptide(distance))
                return AbilityChoice.Riptide;

            return CanStartBrineBolt(distance) ? AbilityChoice.BrineBolt : AbilityChoice.None;
        }

        if (distance <= GetEffectiveBrineBoltRange())
            return CanStartBrineBolt(distance) ? AbilityChoice.BrineBolt : AbilityChoice.None;

        return AbilityChoice.None;
    }

    private bool TryCommitAbility(AbilityChoice eligibleAbility)
    {
        if (eligibleAbility == AbilityChoice.None)
        {
            ResetPendingAbility();
            return false;
        }

        if (pendingAbility != eligibleAbility)
        {
            pendingAbility = eligibleAbility;
            pendingAbilitySince = Time.time;
            Log($"{pendingAbility} range detected. Holding for {abilityRangeCommitDuration:0.00}s before lock-in.");
            return false;
        }

        if (Time.time - pendingAbilitySince < Mathf.Max(0f, abilityRangeCommitDuration))
            return false;

        AbilityChoice committedAbility = pendingAbility;
        ResetPendingAbility();
        Log($"{committedAbility} locked in. Other abilities cannot replace it during this cast.");

        switch (committedAbility)
        {
            case AbilityChoice.TidalWave:
                BeginTidalWave();
                return isCastingTidalWave;
            case AbilityChoice.Riptide:
                BeginRiptide();
                return isCastingRiptide;
            case AbilityChoice.BrineBolt:
                BeginBrineBolt();
                return isCastingBrineBolt;
            default:
                return false;
        }
    }

    private void ResetPendingAbility()
    {
        pendingAbility = AbilityChoice.None;
        pendingAbilitySince = 0f;
    }

    private bool CanStartTidalWave(float distance)
    {
        return battleStarted
            && !isDead
            && !isBeingHit
            && !isCastingBrineBolt
            && !isCastingRiptide
            && !isCastingTidalWave
            && distance <= GetEffectiveTidalWaveRange()
            && Time.time >= nextTidalWaveTime;
    }

    private bool CanStartRiptide(float distance)
    {
        return battleStarted
            && !isDead
            && !isBeingHit
            && !isCastingBrineBolt
            && !isCastingRiptide
            && !isCastingTidalWave
            && distance <= GetEffectiveRiptideRange()
            && Time.time >= nextRiptideTime;
    }

    private bool CanStartBrineBolt(float distance)
    {
        return battleStarted
            && !isDead
            && !isBeingHit
            && !isCastingBrineBolt
            && !isCastingRiptide
            && !isCastingTidalWave
            && distance <= GetEffectiveBrineBoltRange()
            && Time.time >= nextBrineBoltTime;
    }

    private void ChasePlayer()
    {
        if (!TryPrepareAgentForMovement())
        {
            StopMovement();
            SetAnimatorSpeed(0f);
            return;
        }

        float stopRange = GetEffectiveBrineBoltRange();
        FacePlayer();
        EnemyNavMovement.ApplySpeed(agent, moveSpeed, accelerationTime);
        agent.stoppingDistance = stopRange;
        agent.isStopped = false;
        if (!EnemyNavMovement.TrySetDestination(agent, player.position))
        {
            bool persistentPlayerMatch = PersistentPlayerRoot.Instance != null
                                         && player == PersistentPlayerRoot.Instance.transform;
            Debug.LogWarning(
                $"[TideWraith] Chase destination failed | actor={ActorPosition:F2} | " +
                $"player={player.position:F2} | persistentPlayerMatch={persistentPlayerMatch} | " +
                $"agentNext={agent.nextPosition:F2} | onMesh={agent.isOnNavMesh}.",
                this);
            StopMovement();
            SetAnimatorSpeed(0f);
            return;
        }

        SetAnimatorSpeed(0.5f);
    }

    private void HoldRangedPosition()
    {
        StopMovement();
        SetAnimatorSpeed(0f);
        FacePlayer();
    }

    public void TriggerBeingHit()
    {
        if (!battleStarted || isDead || isBeingHit)
            return;

        ResetPendingAbility();
        CancelBrineBolt(true);
        CancelRiptide(true);
        CancelTidalWave(true);
        isBeingHit = true;
        StopMovement();
        SetAnimatorSpeed(0f);

        if (TrySetTrigger(beingHitTrigger))
            hitRoutine = StartCoroutine(HitFallbackRoutine());
        else
            OnBeingHitFinished();
    }

    public void OnBeingHitFinished()
    {
        if (!isBeingHit || isDead)
            return;

        StopRoutine(ref hitRoutine);
        isBeingHit = false;
        Log("BeingHit finished. Approach resumed.");
    }

    public void TriggerDeath()
    {
        if (isDead)
            return;

        isDead = true;
        battleStarted = false;
        isBeingHit = false;
        deathFinished = false;
        ResetPendingAbility();

        StopRoutine(ref hitRoutine);
        StopRoutine(ref deathRoutine);
        CancelBrineBolt(false);
        CancelRiptide(false);
        CancelTidalWave(false);
        StopMovement();
        SetAnimatorSpeed(0f);

        if (ForceDeathAnimation())
            deathRoutine = StartCoroutine(DeathFallbackRoutine());
        else
            OnDeathFinished();

        Log("Death triggered.");
    }

    private bool ForceDeathAnimation()
    {
        if (animator == null)
            return false;

        // Death is terminal. Remove every queued ability/hit transition before
        // taking over the Animator so an active cast cannot resume afterward.
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger)
                animator.ResetTrigger(parameter.nameHash);
        }

        if (!string.IsNullOrWhiteSpace(deathStateName))
        {
            int stateHash = Animator.StringToHash(deathStateName);
            int fullPathHash = Animator.StringToHash($"Base Layer.{deathStateName}");

            if (animator.HasState(0, stateHash))
            {
                animator.Play(stateHash, 0, 0f);
                animator.Update(0f);
                return true;
            }

            if (animator.HasState(0, fullPathHash))
            {
                animator.Play(fullPathHash, 0, 0f);
                animator.Update(0f);
                return true;
            }

            Log($"Animator death state '{deathStateName}' is missing. Falling back to trigger '{deathTrigger}'.");
        }

        return TrySetTrigger(deathTrigger);
    }

    public void OnDeathFinished()
    {
        if (!isDead || deathFinished)
            return;

        deathFinished = true;
        StopRoutine(ref deathRoutine);

        if (deathReceiver != null && !string.IsNullOrWhiteSpace(deathMessage))
            deathReceiver.SendMessage(deathMessage, SendMessageOptions.DontRequireReceiver);

        GameObject target = rootToDisableOnDeath != null ? rootToDisableOnDeath : gameObject;
        target.SetActive(false);
        Log("Death finished and progression callback sent.");
    }

    private void BeginBrineBolt()
    {
        if (isCastingBrineBolt || isCastingRiptide || isCastingTidalWave || isBeingHit || isDead || !battleStarted || !ResolvePlayer())
        {
            Log($"Brine Bolt start rejected | Battle: {battleStarted} | Dead: {isDead} | Hit: {isBeingHit} | Casting: {isCastingBrineBolt} | Player: {player != null}");
            return;
        }

        isCastingBrineBolt = true;
        brineBoltReleased = false;
        StopMovement();
        SetAnimatorSpeed(0f);
        FacePlayer();

        if (!TrySetTrigger(brineBoltTrigger))
        {
            Log($"Animator trigger '{brineBoltTrigger}' is unavailable. Resolving Brine Bolt through fallback timing.");
        }

        StopRoutine(ref brineBoltRoutine);
        brineBoltRoutine = StartCoroutine(BrineBoltFallbackRoutine());
        Log("Brine Bolt cast started.");
    }

    public void OnBrineBoltStart()
    {
        if (!battleStarted || isDead || isBeingHit)
            return;

        if (!isCastingBrineBolt)
        {
            isCastingBrineBolt = true;
            brineBoltReleased = false;
            StopMovement();
            SetAnimatorSpeed(0f);
        }
    }

    public void OnBrineBoltRelease()
    {
        if (!isCastingBrineBolt || brineBoltReleased || isDead || isBeingHit || !ResolvePlayer())
            return;

        brineBoltReleased = true;

        Vector3 origin = GetBrineBoltOrigin();
        Vector3 targetPoint = GetPlayerAimPoint();
        Vector3 direction = targetPoint - origin;
        if (direction.sqrMagnitude <= 0.001f)
            direction = ActorTransform.forward;

        BrineBoltProjectile projectile = brineBoltProjectilePrefab != null
            ? Instantiate(brineBoltProjectilePrefab, origin, Quaternion.LookRotation(direction.normalized))
            : new GameObject("BrineBoltProjectile_Runtime").AddComponent<BrineBoltProjectile>();

        projectile.Launch(
            origin,
            direction,
            ActorTransform,
            player,
            brineBoltDamage,
            brineBoltProjectileSpeed,
            brineBoltProjectileLifetime,
            brineBoltHitRadius
        );

        Log("Brine Bolt released in a fixed, dodgeable direction.");
    }

    public void OnBrineBoltFinished()
    {
        if (!isCastingBrineBolt)
            return;

        StopRoutine(ref brineBoltRoutine);
        isCastingBrineBolt = false;
        brineBoltReleased = false;
        nextBrineBoltTime = Time.time + Mathf.Max(0.1f, brineBoltCooldown);
        Log($"Brine Bolt finished. Decision loop resumed; cooldown: {Mathf.Max(0.1f, brineBoltCooldown):0.00} seconds.");
    }

    private void BeginRiptide()
    {
        if (isCastingRiptide || isCastingBrineBolt || isCastingTidalWave || isBeingHit || isDead || !battleStarted || !ResolvePlayer())
            return;

        isCastingRiptide = true;
        riptideResolved = false;
        capturedRiptidePosition = player.position;
        capturedRiptidePosition.y = ActorPosition.y;

        StopMovement();
        SetAnimatorSpeed(0f);
        FacePlayer();
        ShowRiptideWarning();

        if (!TrySetTrigger(riptideEruptionTrigger))
            Log($"Animator trigger '{riptideEruptionTrigger}' is unavailable. Resolving Riptide through fallback timing.");

        StopRoutine(ref riptideRoutine);
        riptideRoutine = StartCoroutine(RiptideFallbackRoutine());
        Log($"Riptide Eruption started at {capturedRiptidePosition}.");
    }

    // BeginRiptide owns cast state. This callback remains for clip compatibility.
    public void OnRiptideEruptionStart() { }

    public void OnRiptideEruptionResolve()
    {
        if (!isCastingRiptide || riptideResolved || isDead)
            return;

        riptideResolved = true;
        HideRiptideWarning();
        SpawnRiptideImpact();

        float distance = player != null
            ? FlatDistance(player.position, capturedRiptidePosition)
            : float.PositiveInfinity;

        if (distance <= riptideRadius && TryDamagePlayer(riptideDamage))
            Log($"Riptide Eruption hit for {riptideDamage:0.##} damage.");
        else
            Log("Riptide Eruption missed; the player left the marked radius.");
    }

    public void OnRiptideEruptionFinished()
    {
        if (!isCastingRiptide)
            return;

        StopRoutine(ref riptideRoutine);
        HideRiptideWarning();
        isCastingRiptide = false;
        riptideResolved = false;
        nextRiptideTime = Time.time + Mathf.Max(0.1f, riptideCooldown);
        Log($"Riptide Eruption finished. Cooldown: {Mathf.Max(0.1f, riptideCooldown):0.00} seconds.");
    }

    private void BeginTidalWave()
    {
        if (isCastingTidalWave || isCastingRiptide || isCastingBrineBolt || isBeingHit || isDead || !battleStarted || !ResolvePlayer())
            return;

        isCastingTidalWave = true;
        tidalWaveReleased = false;
        capturedTidalWaveDirection = player.position - ActorPosition;
        capturedTidalWaveDirection.y = 0f;
        if (capturedTidalWaveDirection.sqrMagnitude <= 0.001f)
            capturedTidalWaveDirection = ActorTransform.forward;
        capturedTidalWaveDirection.Normalize();

        StopMovement();
        SetAnimatorSpeed(0f);
        FacePlayer();

        if (!TrySetTrigger(tidalWaveTrigger))
            Log($"Animator trigger '{tidalWaveTrigger}' is unavailable. Resolving Tidal Wave through fallback timing.");

        StopRoutine(ref tidalWaveRoutine);
        tidalWaveRoutine = StartCoroutine(TidalWaveFallbackRoutine());
        Log($"Tidal Wave cast started in fixed direction {capturedTidalWaveDirection}.");
    }

    // BeginTidalWave owns cast state. This callback remains for clip compatibility.
    public void OnTidalWaveStart() { }

    public void OnTidalWaveRelease()
    {
        if (!isCastingTidalWave || tidalWaveReleased || isDead || isBeingHit || !ResolvePlayer())
            return;

        tidalWaveReleased = true;
        Vector3 origin = GetTidalWaveOrigin();
        Vector3 direction = capturedTidalWaveDirection.sqrMagnitude > 0.001f
            ? capturedTidalWaveDirection
            : ActorTransform.forward;

        TidalWaveProjectile projectile = tidalWaveProjectilePrefab != null
            ? Instantiate(tidalWaveProjectilePrefab, origin, Quaternion.LookRotation(direction))
            : new GameObject("TidalWaveProjectile_Runtime").AddComponent<TidalWaveProjectile>();

        projectile.Launch(
            origin,
            direction,
            ActorTransform,
            player,
            tidalWaveDamage,
            tidalWaveProjectileSpeed,
            tidalWaveProjectileLifetime,
            tidalWaveHitRadius,
            tidalWaveKnockbackForce,
            tidalWaveStunDuration
        );

        Log("Tidal Wave released in its captured, dodgeable direction.");
    }

    // Projectile impact owns damage and crowd-control resolution.
    public void OnTidalWaveResolve() { }

    public void OnTidalWaveFinished()
    {
        if (!isCastingTidalWave)
            return;

        StopRoutine(ref tidalWaveRoutine);
        isCastingTidalWave = false;
        tidalWaveReleased = false;
        nextTidalWaveTime = Time.time + Mathf.Max(0.1f, tidalWaveCooldown);
        Log($"Tidal Wave finished. Cooldown: {Mathf.Max(0.1f, tidalWaveCooldown):0.00} seconds.");
    }

    private IEnumerator TidalWaveFallbackRoutine()
    {
        float releaseDelay = Mathf.Max(0.01f, tidalWaveReleaseFallback);
        float finishDelay = Mathf.Max(releaseDelay, tidalWaveFinishFallback);

        yield return new WaitForSeconds(releaseDelay);
        if (isCastingTidalWave && !tidalWaveReleased)
        {
            Log("Tidal Wave release Animation Event was not received. Using fallback release.");
            OnTidalWaveRelease();
        }

        yield return new WaitForSeconds(Mathf.Max(0f, finishDelay - releaseDelay));
        tidalWaveRoutine = null;
        if (isCastingTidalWave)
        {
            Log("Tidal Wave finished Animation Event was not received. Using fallback finish.");
            OnTidalWaveFinished();
        }
    }

    private IEnumerator RiptideFallbackRoutine()
    {
        float resolveDelay = Mathf.Max(0.01f, riptideResolveFallback);
        float finishDelay = Mathf.Max(resolveDelay, riptideFinishFallback);

        yield return new WaitForSeconds(resolveDelay);
        if (isCastingRiptide && !riptideResolved)
        {
            Log("Riptide resolve Animation Event was not received. Using fallback resolve.");
            OnRiptideEruptionResolve();
        }

        yield return new WaitForSeconds(Mathf.Max(0f, finishDelay - resolveDelay));
        riptideRoutine = null;
        if (isCastingRiptide)
        {
            Log("Riptide finished Animation Event was not received. Using fallback finish.");
            OnRiptideEruptionFinished();
        }
    }

    private IEnumerator BrineBoltFallbackRoutine()
    {
        float releaseDelay = Mathf.Max(0.01f, brineBoltReleaseFallback);
        float finishDelay = Mathf.Max(releaseDelay, brineBoltFinishFallback);

        yield return new WaitForSeconds(releaseDelay);
        if (isCastingBrineBolt && !brineBoltReleased)
        {
            Log("Brine Bolt release Animation Event was not received. Using fallback release.");
            OnBrineBoltRelease();
        }

        yield return new WaitForSeconds(Mathf.Max(0f, finishDelay - releaseDelay));
        brineBoltRoutine = null;

        if (isCastingBrineBolt)
        {
            Log("Brine Bolt finished Animation Event was not received. Using fallback finish.");
            OnBrineBoltFinished();
        }
    }

    private IEnumerator HitFallbackRoutine()
    {
        yield return new WaitForSeconds(Mathf.Max(0.05f, hitFinishFallback));
        hitRoutine = null;
        OnBeingHitFinished();
    }

    private IEnumerator DeathFallbackRoutine()
    {
        yield return new WaitForSeconds(Mathf.Max(0.05f, deathFinishFallback));
        deathRoutine = null;
        OnDeathFinished();
    }

    private void ResolveReferences()
    {
        if (animator == null)
            animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();

        if (agent == null)
            agent = GetComponent<NavMeshAgent>() ?? GetComponentInParent<NavMeshAgent>();

        if (actorRoot == null)
            actorRoot = agent != null ? agent.transform : transform;

        if (rootToDisableOnDeath == null)
            rootToDisableOnDeath = ActorTransform.gameObject;

        if (animator != null)
            animator.applyRootMotion = false;

        ResolvePlayer();
    }

    private Vector3 GetBrineBoltOrigin()
    {
        if (brineBoltOrigin != null)
            return brineBoltOrigin.position;

        return ActorPosition + Vector3.up * 1.4f + ActorTransform.forward * 0.5f;
    }

    private Vector3 GetTidalWaveOrigin()
    {
        if (tidalWaveOrigin != null)
            return tidalWaveOrigin.position;

        return ActorPosition + Vector3.up * 0.8f + ActorTransform.forward * 1.2f;
    }

    private Vector3 GetPlayerAimPoint()
    {
        if (player == null)
            return ActorPosition + ActorTransform.forward;

        Collider targetCollider = player.GetComponentInChildren<Collider>();
        return targetCollider != null ? targetCollider.bounds.center : player.position + Vector3.up;
    }

    private float GetFlatDistanceToPlayer()
    {
        return player != null
            ? FlatDistance(ActorPosition, player.position)
            : float.PositiveInfinity;
    }

    private float GetEffectiveBrineBoltRange()
    {
        return Mathf.Max(0f, preferredMinimumRange);
    }

    private float GetEffectiveRiptideRange()
    {
        return Mathf.Max(0.1f, Mathf.Min(riptideCastRange, GetEffectiveBrineBoltRange()));
    }

    private float GetEffectiveTidalWaveRange()
    {
        return Mathf.Max(0.1f, Mathf.Min(tidalWaveCastRange, GetEffectiveRiptideRange()));
    }

    private void CancelBrineBolt(bool applyCooldown)
    {
        bool wasCasting = isCastingBrineBolt;
        StopRoutine(ref brineBoltRoutine);
        isCastingBrineBolt = false;
        brineBoltReleased = false;

        if (applyCooldown && wasCasting)
            nextBrineBoltTime = Time.time + Mathf.Max(0.1f, brineBoltCooldown);
    }

    private void CancelRiptide(bool applyCooldown)
    {
        bool wasCasting = isCastingRiptide;
        StopRoutine(ref riptideRoutine);
        HideRiptideWarning();
        isCastingRiptide = false;
        riptideResolved = false;

        if (applyCooldown && wasCasting)
            nextRiptideTime = Time.time + Mathf.Max(0.1f, riptideCooldown);
    }

    private void CancelTidalWave(bool applyCooldown)
    {
        bool wasCasting = isCastingTidalWave;
        StopRoutine(ref tidalWaveRoutine);
        isCastingTidalWave = false;
        tidalWaveReleased = false;

        if (applyCooldown && wasCasting)
            nextTidalWaveTime = Time.time + Mathf.Max(0.1f, tidalWaveCooldown);
    }

    private void ShowRiptideWarning()
    {
        HideRiptideWarning();
        if (riptideWarningPrefab != null)
            activeRiptideWarning = Instantiate(riptideWarningPrefab, capturedRiptidePosition, Quaternion.identity);
    }

    private void HideRiptideWarning()
    {
        if (activeRiptideWarning == null)
            return;

        Destroy(activeRiptideWarning);
        activeRiptideWarning = null;
    }

    private void SpawnRiptideImpact()
    {
        if (riptideImpactPrefab != null)
            Instantiate(riptideImpactPrefab, capturedRiptidePosition, Quaternion.identity);
    }

    private bool TryDamagePlayer(float damage)
    {
        if (player == null || damage <= 0f)
            return false;

        PlayerHealth health = player.GetComponent<PlayerHealth>()
            ?? player.GetComponentInParent<PlayerHealth>()
            ?? player.GetComponentInChildren<PlayerHealth>();

        if (health == null)
        {
            Log("Riptide could not find PlayerHealth on the assigned player.");
            return false;
        }

        health.TakeDamage(damage);
        return true;
    }

    private bool ResolvePlayer()
    {
        if (PersistentPlayerRoot.Instance != null)
        {
            player = PersistentPlayerRoot.Instance.transform;
            return true;
        }

        if (player != null)
            return true;

        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject == null)
            return false;

        player = playerObject.transform;
        return true;
    }

    private void ConfigureAgent()
    {
        if (agent == null)
            return;

        EnemyNavMovement.ApplySpeed(agent, moveSpeed, accelerationTime);
        agent.stoppingDistance = Mathf.Max(0f, preferredMinimumRange);
        agent.updateRotation = false;
        agent.autoBraking = true;
    }

    private bool TryPrepareAgentForMovement()
    {
        if (agent == null || !agent.enabled)
        {
            LogMissingNavMesh("No enabled NavMeshAgent is assigned.");
            return false;
        }

        if (!agent.isOnNavMesh)
        {
            if (!NavMesh.SamplePosition(ActorPosition, out NavMeshHit hit, navMeshSearchRadius, NavMesh.AllAreas) || !agent.Warp(hit.position))
            {
                LogMissingNavMesh("NavMeshAgent is not on a baked NavMesh.");
                return false;
            }
        }

        loggedMissingNavMesh = false;
        return true;
    }

    private void StopMovement()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        agent.isStopped = true;
        agent.ResetPath();
    }

    private void FacePlayer()
    {
        if (player == null)
            return;

        Vector3 direction = player.position - ActorPosition;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        ActorTransform.rotation = Quaternion.RotateTowards(
            ActorTransform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private bool TrySetTrigger(string parameterName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(parameterName))
            return false;

        int hash = Animator.StringToHash(parameterName);
        if (!HasAnimatorParameter(hash, AnimatorControllerParameterType.Trigger))
        {
            Log($"Animator trigger '{parameterName}' is missing.");
            return false;
        }

        animator.SetTrigger(hash);
        return true;
    }

    private void SetAnimatorSpeed(float value)
    {
        if (animator == null || string.IsNullOrWhiteSpace(speedParameter))
            return;

        int hash = Animator.StringToHash(speedParameter);
        if (HasAnimatorParameter(hash, AnimatorControllerParameterType.Float))
            animator.SetFloat(hash, value, 0.1f, Time.deltaTime);
    }

    private bool HasAnimatorParameter(int hash, AnimatorControllerParameterType type)
    {
        if (animator == null)
            return false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == hash && parameter.type == type)
                return true;
        }

        return false;
    }

    private void StopRoutine(ref Coroutine routine)
    {
        if (routine == null)
            return;

        StopCoroutine(routine);
        routine = null;
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void LogMissingNavMesh(string message)
    {
        if (loggedMissingNavMesh)
            return;

        loggedMissingNavMesh = true;
        Log(message);
    }

    private void Log(string message)
    {
        if (debugLogs)
            Debug.Log($"[TideWraith] {message}", this);
    }

    private void TrackRuntimeState()
    {
        if (!debugLogs || Time.unscaledTime < nextDebugStateLogTime)
            return;

        nextDebugStateLogTime = Time.unscaledTime + Mathf.Max(0.1f, debugStateLogInterval);

        float distance = player != null ? GetFlatDistanceToPlayer() : float.PositiveInfinity;
        float range = GetEffectiveBrineBoltRange();
        float riptideRange = GetEffectiveRiptideRange();
        float tidalWaveRange = GetEffectiveTidalWaveRange();
        float cooldownRemaining = Mathf.Max(0f, nextBrineBoltTime - Time.time);
        float riptideCooldownRemaining = Mathf.Max(0f, nextRiptideTime - Time.time);
        float tidalWaveCooldownRemaining = Mathf.Max(0f, nextTidalWaveTime - Time.time);
        string decision = GetRuntimeDecisionLabel(distance, range, cooldownRemaining);
        string animatorState = GetAnimatorStateLabel();

        bool agentEnabled = agent != null && agent.enabled;
        bool agentOnMesh = agentEnabled && agent.isOnNavMesh;
        string agentStopped = agentOnMesh ? agent.isStopped.ToString() : "N/A";
        string distanceText = float.IsPositiveInfinity(distance) ? "N/A" : distance.ToString("0.00");

        // Debug.Log(
        //     $"[TideWraith State] Decision: {decision} | Animator: {animatorState} | " +
        //     $"Distance: {distanceText} | Brine Range: {range:0.00} | Riptide Range: {riptideRange:0.00} | Tidal Range: {tidalWaveRange:0.00} | " +
        //     $"Brine CD: {cooldownRemaining:0.00}s | Riptide CD: {riptideCooldownRemaining:0.00}s | Tidal CD: {tidalWaveCooldownRemaining:0.00}s | " +
        //     $"Battle: {battleStarted} | Brine: {isCastingBrineBolt} | Riptide: {isCastingRiptide} | Tidal: {isCastingTidalWave} | Hit: {isBeingHit} | Dead: {isDead} | " +
        //     $"Agent enabled: {agentEnabled} | OnMesh: {agentOnMesh} | Stopped: {agentStopped}",
        //     this
        // );
    }

    private string GetRuntimeDecisionLabel(float distance, float range, float cooldownRemaining)
    {
        if (!gameObject.activeInHierarchy || !enabled)
            return "COMPONENT_INACTIVE";
        if (isDead)
            return "DEAD";
        if (!battleStarted)
            return "WAITING_FOR_BATTLE";
        if (player == null)
            return "NO_PLAYER";
        if (isBeingHit)
            return "BEING_HIT";
        if (isCastingBrineBolt)
            return "CASTING_BRINE_BOLT";
        if (isCastingRiptide)
            return "CASTING_RIPTIDE";
        if (isCastingTidalWave)
            return "CASTING_TIDAL_WAVE";
        if (pendingAbility != AbilityChoice.None)
            return $"COMMITTING_{pendingAbility.ToString().ToUpperInvariant()}";
        if (distance > range)
            return "APPROACHING_PLAYER";
        if (cooldownRemaining > 0f)
            return "HOLDING_RANGE_COOLDOWN";

        return "READY_TO_CAST";
    }

    private string GetAnimatorStateLabel()
    {
        if (animator == null)
            return "NO_ANIMATOR";
        if (!animator.isActiveAndEnabled)
            return "ANIMATOR_INACTIVE";

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (state.IsName("Idle") || state.IsName("Base Layer.Idle")) return "Idle";
        if (state.IsName("Walk") || state.IsName("Base Layer.Walk")) return "Walk";
        if (state.IsName("BrineBolt") || state.IsName("Base Layer.BrineBolt")) return "BrineBolt";
        if (state.IsName("RiptideEruption") || state.IsName("Base Layer.RiptideEruption")) return "RiptideEruption";
        if (state.IsName("TidalWave") || state.IsName("Base Layer.TidalWave")) return "TidalWave";
        if (state.IsName("BeingHit") || state.IsName("Base Layer.BeingHit")) return "BeingHit";
        if (state.IsName("Death") || state.IsName("Base Layer.Death")) return "Death";

        return $"Hash:{state.shortNameHash}";
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos)
            return;

        Vector3 center = actorRoot != null ? actorRoot.position : transform.position;

        Gizmos.color = new Color(0.15f, 0.8f, 1f, 0.65f);
        Gizmos.DrawWireSphere(center, Mathf.Max(0f, preferredMinimumRange));

        Gizmos.color = new Color(0.1f, 1f, 0.75f, 0.75f);
        Gizmos.DrawWireSphere(center, GetEffectiveRiptideRange());

        Gizmos.color = new Color(0.05f, 0.65f, 1f, 0.85f);
        Gizmos.DrawWireSphere(center, GetEffectiveTidalWaveRange());
    }
}
