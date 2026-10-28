using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(CapsuleCollider))]
public sealed class SunboundWardenController : MonoBehaviour
{
    private enum RadiantRushPhase
    {
        None,
        Startup,
        Sprint,
        Sword
    }

    private enum WardenState
    {
        PreBattle,
        CrouchGuard,
        WalkingToTiles,
        WalkingToPlayer,
        RadiantRush,
        Sunbind,
        CrouchSlash,
        BeingHitCrouched,
        SunSlash,
        Recovery,
        Dead
    }

    [Header("Core References")]
    public Animator animator;
    public NavMeshAgent agent;
    public EnemyHealth enemyHealth;
    public Transform player;

    [Header("Basic Movement")]
    public float moveSpeed = 4.2f;
    public float acceleration = 18f;
    public float rotationSpeed = 540f;
    public float stoppingDistance = 2.15f;

    [Header("Radiant Rush")]
    [Tooltip("World-space speed while the sprint section of Radiant Rush is active.")]
    public float radiantRushSpeed = 10f;
    [Tooltip("Source clip sample rate. The current Radiant Rush clip is authored at 30 FPS.")]
    public float radiantRushClipFrameRate = 30f;
    [Tooltip("Frame in the old Radiant Rush clip where the startup hands off to the dedicated sprint animation.")]
    public int radiantRushSprintStartFrame = 18;
    [Tooltip("Frame in the old Radiant Rush clip where the sword finisher begins.")]
    public int radiantRushSwordStartFrame = 33;
    [Tooltip("When Van reaches this planar distance, leave the dedicated sprint state and commit to the sword finisher.")]
    public float radiantRushSwordAttackRange = 3f;
    [Tooltip("Minimum gap the Radiant Rush sprint keeps from Van before committing to the sword finisher. Keep this at or below Sword Attack Range.")]
    public float radiantRushStoppingDistance = 2.5f;
    [Tooltip("Damage dealt when the physical sword hitbox connects during Radiant Rush.")]
    public float radiantRushSwordDamage = 18f;
    [Tooltip("First source-clip frame where the Radiant Rush sword can damage Van.")]
    public int radiantRushSwordHitboxOpenFrame = 50;
    [Tooltip("First source-clip frame where the Radiant Rush sword can no longer damage Van.")]
    public int radiantRushSwordHitboxCloseFrame = 60;
    [Tooltip("How far from the current NavMesh the rush may sample each forward step.")]
    public float radiantRushNavMeshSampleRadius = 1.25f;

    [Header("Sunbind Anti-Kite")]
    [Tooltip("Seconds Van may continuously kite the Radiant Rush sprint before Sunbind interrupts it.")]
    public float sunbindAntiKiteSeconds = 4f;
    [Range(0.01f, 1f)]
    [Tooltip("Temporary movement multiplier applied by Sunbind. Tune after gameplay testing.")]
    public float sunbindSlowMultiplier = 0.6f;
    [Tooltip("Duration of the Sunbind slow. Tune after gameplay testing.")]
    public float sunbindSlowDuration = 2.5f;
    [Tooltip("After guaranteed Sunbind lands, sprint until Van is this close, then use Sun Slash directly.")]
    public float sunbindSunSlashRange = 3f;
    [Tooltip("Damage dealt when the physical sword collider connects during Sun Slash.")]
    public float sunSlashDamage = 20f;
    public string sunbindStateName = "Sunbind";
    public string beingHitCrouchedStateName = "BEING HIT CROUCHED";
    public string crouchGuardIdleStateName = "IDLE";

    [Header("Standing Hit Overlay")]
    [Tooltip("Animator layer that contains the upper-body-only standing/walking hit reaction.")]
    public string standingHitLayerName = "Upper Body Hit";
    public string standingHitStateName = "Standing Hit";
    [Tooltip("Seconds used to blend the upper-body hit reaction on over the walking base layer.")]
    public float standingHitBlendInSeconds = 0.06f;
    [Tooltip("Seconds used to blend the upper-body hit reaction back out to the walking base layer.")]
    public float standingHitBlendOutSeconds = 0.12f;
    [Range(0.5f, 0.95f)]
    [Tooltip("Normalized hit-animation time at which the overlay begins blending back to locomotion.")]
    public float standingHitBlendOutNormalizedTime = 0.78f;

    [Header("Tile Guard")]
    [Tooltip("Maximum spacing between tiles for them to count as one connected cluster.")]
    public float tileClusterLinkDistance = 5f;
    [Tooltip("NavMeshAgent speed used while running toward the selected tile cluster.")]
    public float tileRunSpeed = 15f;
    [Tooltip("How close the Warden must get to the selected cluster center before crouching.")]
    public float tileGuardArrivalDistance = 1.1f;
    [Tooltip("Radius used when snapping a computed tile-cluster center onto the NavMesh.")]
    public float tileGuardNavMeshSampleRadius = 4f;
    [Tooltip("Maximum time the Warden will guard a cluster before anti-stall offense begins.")]
    public float tileGuardMaxSeconds = 6f;
    [Tooltip("Player range that lets the crouched Warden repeatedly slash while guarding.")]
    public float crouchSlashTriggerRange = 3.25f;
    [Tooltip("Minimum time between repeated crouch slashes.")]
    public float crouchSlashRepeatCooldown = 1.75f;
    [Tooltip("Damage dealt by one physical Crouch Slash sword contact.")]
    public float crouchSlashDamage = 12f;

    [Header("Animator Parameters")]
    public string walkingToTilesParameter = "WalkingToTiles";
    public string walkingToPlayerParameter = "WalkingToPlayer";
    public string greatSwordWalkStateName = "Great Sword Walk";
    public string greatSwordRunStateName = "Great Sword Run";
    public string crouchSlashStateName = "Crouch Slash";
    public string radiantRushParameter = "RadiantRush";
    public string radiantRushStartStateName = "Radiant Rush Start";
    public string radiantRushSprintStateName = "Radiant Rush Sprint";
    public string radiantRushSwordStateName = "Radiant Rush Sword";
    public string sunSlashStateName = "Sun Slash";
    public string recoveryStateName = "Recovery_Merged";
    public string deathStateName = "Death";
    public string deathParameterName = "Death";

    [Header("Encounter Gating")]
    [SerializeField] private bool hideFromTargetingBeforeBattle = true;
    [SerializeField] private string combatTag = "Enemy";

    [Header("Defeat Callback")]
    public MonoBehaviour deathReceiver;
    public string deathMessage = "OnEnemyDefeated";
    [Tooltip("Safety-net delay used only if the death Animation Event does not fire.")]
    public float deathFallbackFinishTime = 4.25f;

    [Header("Debug")]
    public bool autoStartBattleForTesting = false;
    [Tooltip("Temporary movement diagnostics. Remove after Warden locomotion is validated.")]
    public bool debugMovementLogs = true;
    [Tooltip("Seconds between temporary movement diagnostic log lines.")]
    public float debugMovementLogInterval = 0.25f;

    private bool battleStarted;
    private bool isDead;
    private bool deathFinished;
    private bool deathAnimationEventReceived;
    private bool defeatNotified;
    private Coroutine deathFallbackRoutine;
    private CapsuleCollider damageCollider;
    private SunboundWardenSwordHitbox swordHitbox;
    private WardenState state = WardenState.PreBattle;
    private Vector3 tileGuardDestination;
    private Vector3 radiantRushDirection;
    private RadiantRushPhase radiantRushPhase = RadiantRushPhase.None;
    private bool radiantRushAttackCommitted;
    private bool radiantRushHitConfirmed;
    private bool radiantRushHitboxWindowOpen;
    private bool radiantRushSwordResolved;
    private float radiantRushSprintStartedAt = -1f;
    private bool sunbindFollowupSprint;
    private bool sunbindEffectApplied;
    private readonly List<WorldTile> pendingGuardCluster = new List<WorldTile>();
    private readonly HashSet<WorldTile> guardedTiles = new HashSet<WorldTile>();
    private float tileGuardStartedAt = -1f;
    private float nextCrouchSlashAllowedAt;
    private bool guardCounterQueued;
    private bool crouchSlashHitboxWindowOpen;
    private bool crouchSlashHitConfirmed;
    private int standingHitLayerIndex = -1;
    private int standingHitStateHash;
    private bool standingHitOverlayActive;
    private bool sunSlashHitConfirmed;
    private bool sunSlashHitboxWindowOpen;
    private bool recoveryStateEntered;
    private float recoveryStartedAt = -1f;
    private float nextMovementDebugLogTime;

    public bool BattleStarted => battleStarted;
    public bool IsDead => isDead;
    public bool DeathFinished => deathFinished;
    public bool DeathAnimationEventReceived => deathAnimationEventReceived;
    public bool IsRadiantRushActive => state == WardenState.RadiantRush;
    public bool RadiantRushHitConfirmed => radiantRushHitConfirmed;
    public string RadiantRushPhaseName => radiantRushPhase.ToString();
    public Vector3 RadiantRushDirection => radiantRushDirection;

    public float RadiantRushNormalizedTime
    {
        get
        {
            if (animator == null)
                return -1f;

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            return stateInfo.IsName(radiantRushStartStateName) ||
                   stateInfo.IsName(radiantRushSprintStateName) ||
                   stateInfo.IsName(radiantRushSwordStateName)
                ? stateInfo.normalizedTime
                : -1f;
        }
    }

    private void Awake()
    {
        ResolveAnimator();
        agent ??= GetComponent<NavMeshAgent>();
        enemyHealth ??= GetComponent<EnemyHealth>();
        damageCollider = GetComponent<CapsuleCollider>();
        ResolveSwordHitbox();

        ResolvePlayer();
        ConfigureAgent();
        SetWalkingToTiles(false);
        SetWalkingToPlayer(false);
        SetCombatTargetable(false);
    }

    private void Start()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.isStopped = true;

        if (autoStartBattleForTesting)
            StartBattle();
    }

    private void Update()
    {
        LogMovementDiagnostics();

        if (isDead)
        {
            StopMovement();
            return;
        }

        if (!battleStarted)
        {
            StopMovement();
            return;
        }

        switch (state)
        {
            case WardenState.WalkingToTiles:
                UpdateWalkingToTiles();
                break;

            case WardenState.WalkingToPlayer:
                UpdateWalkingToPlayer();
                break;

            case WardenState.RadiantRush:
            case WardenState.Sunbind:
            case WardenState.CrouchSlash:
            case WardenState.BeingHitCrouched:
            case WardenState.SunSlash:
            case WardenState.Recovery:
                StopMovement();
                break;

            case WardenState.CrouchGuard:
                StopMovement();
                ResolvePlayer();
                if (player != null)
                    FaceDirection(player.position - transform.position);
                break;

            case WardenState.PreBattle:
            default:
                StopMovement();
                break;
        }
    }

    private void LogMovementDiagnostics()
    {
        if (!debugMovementLogs || !battleStarted || Time.unscaledTime < nextMovementDebugLogTime)
            return;

        nextMovementDebugLogTime = Time.unscaledTime + Mathf.Max(0.05f, debugMovementLogInterval);
        ResolvePlayer();

        string animationName = "<no animator>";
        float animationNormalizedTime = -1f;
        ResolveAnimator();
        if (animator != null)
        {
            AnimatorStateInfo animationState = animator.GetCurrentAnimatorStateInfo(0);
            animationNormalizedTime = animationState.normalizedTime;

            if (animationState.IsName("IDLE"))
                animationName = "IDLE";
            else if (animationState.IsName("Great Sword Walk"))
                animationName = "Great Sword Walk";
            else if (animationState.IsName(greatSwordRunStateName))
                animationName = greatSwordRunStateName;
            else if (animationState.IsName(radiantRushStartStateName))
                animationName = radiantRushStartStateName;
            else if (animationState.IsName(radiantRushSprintStateName))
                animationName = radiantRushSprintStateName;
            else if (animationState.IsName(radiantRushSwordStateName))
                animationName = radiantRushSwordStateName;
            else if (animationState.IsName(sunSlashStateName))
                animationName = sunSlashStateName;
            else if (animationState.IsName(recoveryStateName))
                animationName = recoveryStateName;
            else if (animationState.IsName("Sunbind"))
                animationName = "Sunbind";
            else if (animationState.IsName(beingHitCrouchedStateName))
                animationName = beingHitCrouchedStateName;
            else
                animationName = $"hash:{animationState.fullPathHash}";
        }

        Vector3 playerPosition = player != null ? player.position : Vector3.positiveInfinity;
        float configuredSpeed = agent != null ? agent.speed : moveSpeed;
        float actualSpeed = agent != null ? agent.velocity.magnitude : 0f;
        bool hasPath = agent != null && agent.enabled && agent.isOnNavMesh && agent.hasPath;
        float remainingDistance =
            agent != null && agent.enabled && agent.isOnNavMesh && agent.hasPath
                ? agent.remainingDistance
                : -1f;

        Debug.Log(
            $"[SunboundWarden][MOVE-DEBUG] state={state} rushPhase={radiantRushPhase} anim={animationName} " +
            $"animT={animationNormalizedTime:0.00} wardenPos={transform.position:F2} " +
            $"playerPos={playerPosition:F2} configuredSpeed={configuredSpeed:0.00} " +
            $"actualSpeed={actualSpeed:0.00} hasPath={hasPath} remaining={remainingDistance:0.00}",
            this);
    }

    private void ResolveAnimator()
    {
        if (animator != null)
            return;

        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
        if (animator == null)
            animator = GetComponentInParent<Animator>();
    }

    private void LateUpdate()
    {
        if (!battleStarted || isDead)
            return;

        UpdateStandingHitOverlay();

        switch (state)
        {
            case WardenState.RadiantRush:
                // Apply the committed burst after Animator evaluation so animation
                // root/body curves cannot visually pin the actor in place.
                UpdateRadiantRush();
                break;

            case WardenState.Sunbind:
                UpdateSunbind();
                break;

            case WardenState.CrouchGuard:
                UpdateCrouchGuard();
                break;

            case WardenState.CrouchSlash:
                UpdateCrouchSlash();
                break;

            case WardenState.BeingHitCrouched:
                UpdateBeingHitCrouched();
                break;

            case WardenState.SunSlash:
                UpdateSunSlash();
                break;

            case WardenState.Recovery:
                UpdateRecovery();
                break;

        }
    }

    public void StartBattle()
    {
        if (battleStarted || isDead)
            return;

        ResolvePlayer();
        if (player == null)
        {
            Debug.LogWarning("[SunboundWarden] Cannot start battle because Player was not found.", this);
            return;
        }

        battleStarted = true;
        SetCombatTargetable(true);
        ConfigureAgent();
        SetWalkingToTiles(false);
        SetWalkingToPlayer(false);
        StopMovement();

        BeginPlayerOffense();
    }

    public void TriggerBeingHit()
    {
        if (!battleStarted || isDead)
            return;

        // Walking reactions are an upper-body-only overlay. The base locomotion
        // state and NavMeshAgent keep running, so the Warden can visibly recoil
        // without planting his feet or abandoning his destination.
        if (CanUseStandingHitOverlay())
        {
            BeginStandingHitOverlay();
            return;
        }

        // The existing crouched reaction remains a full-body state and is valid
        // only after the Warden has actually settled into CrouchGuard/IDLE.
        if (state != WardenState.CrouchGuard)
            return;

        ResolveAnimator();
        if (animator == null || animator.IsInTransition(0))
            return;

        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
        if (!current.IsName(crouchGuardIdleStateName))
            return;

        BeginBeingHitCrouched();
    }

    private void BeginStandingHitOverlay()
    {
        ResolveAnimator();
        if (animator == null)
            return;

        standingHitLayerIndex = animator.GetLayerIndex(standingHitLayerName);
        if (standingHitLayerIndex < 0 || standingHitLayerIndex >= animator.layerCount)
        {
            Debug.LogWarning(
                $"[SunboundWarden] Upper-body hit layer '{standingHitLayerName}' was not found; walking continues without a hit flinch.",
                this);
            standingHitLayerIndex = -1;
            return;
        }

        standingHitStateHash = Animator.StringToHash(
            $"{standingHitLayerName}.{standingHitStateName}");
        if (!animator.HasState(standingHitLayerIndex, standingHitStateHash))
        {
            Debug.LogWarning(
                $"[SunboundWarden] Upper-body hit state '{standingHitStateName}' is missing from layer '{standingHitLayerName}'.",
                this);
            return;
        }

        animator.Play(standingHitStateHash, standingHitLayerIndex, 0f);
        standingHitOverlayActive = true;

        Debug.Log(
            "[SunboundWarden] Walking hit -> upper-body reaction; locomotion preserved.",
            this);
    }

    private void UpdateStandingHitOverlay()
    {
        ResolveAnimator();
        if (animator == null)
            return;

        if (standingHitLayerIndex < 0 || standingHitLayerIndex >= animator.layerCount)
            standingHitLayerIndex = animator.GetLayerIndex(standingHitLayerName);

        if (standingHitLayerIndex < 0 || standingHitLayerIndex >= animator.layerCount)
            return;

        float currentWeight = animator.GetLayerWeight(standingHitLayerIndex);
        if (!standingHitOverlayActive)
        {
            if (currentWeight > 0f)
                animator.SetLayerWeight(standingHitLayerIndex, 0f);
            return;
        }

        bool stillWalking = CanUseStandingHitOverlay();

        AnimatorStateInfo hitState =
            animator.GetCurrentAnimatorStateInfo(standingHitLayerIndex);
        bool hitStatePlaying =
            standingHitStateHash != 0 &&
            hitState.fullPathHash == standingHitStateHash;

        bool blendOut =
            !stillWalking ||
            !hitStatePlaying ||
            hitState.normalizedTime >= standingHitBlendOutNormalizedTime;

        float targetWeight = blendOut ? 0f : 1f;
        float blendSeconds = blendOut
            ? Mathf.Max(0f, standingHitBlendOutSeconds)
            : Mathf.Max(0f, standingHitBlendInSeconds);
        float nextWeight = blendSeconds <= 0.0001f
            ? targetWeight
            : Mathf.MoveTowards(
                currentWeight,
                targetWeight,
                Time.deltaTime / blendSeconds);

        animator.SetLayerWeight(standingHitLayerIndex, nextWeight);

        if (blendOut && nextWeight <= 0.001f)
        {
            animator.SetLayerWeight(standingHitLayerIndex, 0f);
            standingHitOverlayActive = false;
        }
    }

    private void CancelStandingHitOverlay()
    {
        standingHitOverlayActive = false;
        ResolveAnimator();
        if (animator == null)
            return;

        if (standingHitLayerIndex < 0 || standingHitLayerIndex >= animator.layerCount)
            standingHitLayerIndex = animator.GetLayerIndex(standingHitLayerName);

        if (standingHitLayerIndex >= 0 && standingHitLayerIndex < animator.layerCount)
            animator.SetLayerWeight(standingHitLayerIndex, 0f);
    }

    private bool CanUseStandingHitOverlay()
    {
        return
            state == WardenState.WalkingToTiles ||
            state == WardenState.WalkingToPlayer ||
            (state == WardenState.RadiantRush &&
             radiantRushPhase == RadiantRushPhase.Sprint);
    }

    private void BeginBeingHitCrouched()
    {
        ResolveAnimator();
        CancelStandingHitOverlay();
        CloseRadiantRushSwordHitboxInternal();
        CloseSunSlashHitboxInternal();
        StopMovement();
        SetWalkingToTiles(false);
        SetWalkingToPlayer(false);

        if (animator == null)
        {
            BeginRadiantRush();
            return;
        }

        int hitHash = Animator.StringToHash(beingHitCrouchedStateName);
        if (!animator.HasState(0, hitHash))
        {
            Debug.LogWarning(
                $"[SunboundWarden] Animator state '{beingHitCrouchedStateName}' is missing; breaking guard into Radiant Rush.",
                this);
            BeginRadiantRush();
            return;
        }

        state = WardenState.BeingHitCrouched;
        animator.CrossFadeInFixedTime(hitHash, 0.04f, 0, 0f);
        Debug.Log("[SunboundWarden] Guard hit -> Being Hit Crouched.", this);
    }

    private void UpdateBeingHitCrouched()
    {
        ResolveAnimator();
        if (animator == null)
        {
            BeginRadiantRush();
            return;
        }

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (!stateInfo.IsName(beingHitCrouchedStateName))
            return;

        ResolvePlayer();
        if (player != null)
            FaceDirection(player.position - transform.position);

        if (stateInfo.normalizedTime < 0.985f)
            return;

        Debug.Log("[SunboundWarden] Being Hit Crouched finished -> Radiant Rush.", this);
        BeginRadiantRush();
    }

    private void BeginRadiantRush()
    {
        ClearGuardedTileTracking();
        CloseCrouchSlashHitboxInternal();
        ResolvePlayer();
        if (player == null)
        {
            state = WardenState.CrouchGuard;
            Debug.LogWarning("[SunboundWarden] Radiant Rush skipped because Player was not found.", this);
            return;
        }

        state = WardenState.RadiantRush;
        StopMovement();
        SetWalkingToTiles(false);
        SetWalkingToPlayer(false);

        // Radiant Rush is one gameplay ability split across three animation
        // phases: startup -> dedicated sprint -> committed sword finisher.
        Vector3 facing = player.position - transform.position;
        facing.y = 0f;
        radiantRushDirection = facing.sqrMagnitude > 0.001f
            ? facing.normalized
            : transform.forward;
        radiantRushDirection.y = 0f;
        radiantRushDirection.Normalize();
        radiantRushPhase = RadiantRushPhase.Startup;
        radiantRushAttackCommitted = false;
        radiantRushHitConfirmed = false;
        radiantRushHitboxWindowOpen = false;
        radiantRushSwordResolved = false;
        radiantRushSprintStartedAt = -1f;
        sunbindFollowupSprint = false;
        sunbindEffectApplied = false;
        ResolveSwordHitbox();
        if (swordHitbox != null)
            swordHitbox.CloseDamageWindow();
        FaceDirection(radiantRushDirection);

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            if (agent.hasPath)
                agent.ResetPath();

            // During the committed burst the controller owns world position.
            // Keep the agent enabled for NavMesh sampling, but prevent it from
            // snapping the transform back to its internal position.
            agent.updatePosition = false;
            agent.nextPosition = transform.position;
        }

        ResolveAnimator();

        if (animator != null)
        {
            int rushStartHash = Animator.StringToHash(radiantRushStartStateName);
            if (animator.HasState(0, rushStartHash))
            {
                if (!string.IsNullOrWhiteSpace(radiantRushParameter))
                    animator.ResetTrigger(radiantRushParameter);

                // Do not depend on the IDLE -> Radiant Rush trigger transition.
                // Guard exits can happen while Crouch Slash is still the active
                // Animator state, and that state intentionally has no outgoing
                // transition. Cross-fading directly keeps gameplay state and
                // Animator state synchronized.
                animator.CrossFadeInFixedTime(rushStartHash, 0.06f, 0, 0f);
            }
            else if (!string.IsNullOrWhiteSpace(radiantRushParameter))
            {
                animator.ResetTrigger(radiantRushParameter);
                animator.SetTrigger(radiantRushParameter);
                Debug.LogWarning(
                    $"[SunboundWarden] Animator state '{radiantRushStartStateName}' is missing; using legacy RadiantRush trigger fallback.",
                    this);
            }
            else
            {
                Debug.LogWarning("[SunboundWarden] Radiant Rush Animator startup could not be fired.", this);
            }
        }
    }

    private void BeginPlayerOffense()
    {
        ResolvePlayer();
        if (player == null)
        {
            state = WardenState.CrouchGuard;
            StopMovement();
            return;
        }

        // Radiant Rush no longer has an activation range. Its own running
        // section pursues Van until the sword finisher can connect.
        BeginRadiantRush();
    }

    private bool BeginWalkToPlayer()
    {
        ResolvePlayer();
        if (player == null || agent == null || !agent.enabled || !agent.isOnNavMesh)
        {
            Debug.LogWarning("[SunboundWarden] Cannot approach Player because Player/NavMeshAgent is not ready.", this);
            return false;
        }

        RestoreAgentPositionOwnership();
        state = WardenState.WalkingToPlayer;
        SetWalkingToTiles(false);
        SetWalkingToPlayer(true);

        agent.isStopped = false;
        agent.speed = moveSpeed;
        agent.acceleration = acceleration;
        agent.angularSpeed = rotationSpeed;
        agent.stoppingDistance = stoppingDistance;
        agent.SetDestination(player.position);

        Debug.Log(
            $"[SunboundWarden] Player is outside Radiant Rush range ({PlanarDistanceToPlayer():0.00}u); approaching.",
            this);
        return true;
    }

    private void UpdateWalkingToPlayer()
    {
        ResolvePlayer();
        if (player == null || agent == null || !agent.enabled || !agent.isOnNavMesh)
        {
            SetWalkingToPlayer(false);
            StopMovement();
            state = WardenState.CrouchGuard;
            return;
        }

        // Van may keep retreating, so refresh the chase destination while the
        // Warden is still in this legacy approach state. New combat flow does
        // not enter WalkingToPlayer; Radiant Rush owns offensive pursuit.
        agent.SetDestination(player.position);

        Vector3 facing = agent.velocity;
        facing.y = 0f;
        if (facing.sqrMagnitude > 0.01f)
            FaceDirection(facing);
    }

    private float PlanarDistanceToPlayer()
    {
        if (player == null)
            return float.PositiveInfinity;

        Vector3 delta = player.position - transform.position;
        delta.y = 0f;
        return delta.magnitude;
    }

    private void RestoreAgentPositionOwnership()
    {
        if (agent == null || !agent.enabled)
            return;

        if (agent.isOnNavMesh)
        {
            agent.nextPosition = transform.position;
            agent.Warp(transform.position);
        }

        agent.updatePosition = true;
    }

    private void UpdateRadiantRush()
    {
        ResolveAnimator();
        ResolvePlayer();
        if (animator == null)
            return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        bool inRadiantRushAnimation =
            stateInfo.IsName(radiantRushStartStateName) ||
            stateInfo.IsName(radiantRushSprintStateName) ||
            stateInfo.IsName(radiantRushSwordStateName);
        if (!inRadiantRushAnimation)
            return;

        if (player == null)
            return;

        if (radiantRushPhase == RadiantRushPhase.Startup)
        {
            if (!stateInfo.IsName(radiantRushStartStateName))
                return;

            float clipLength = Mathf.Max(0.001f, stateInfo.length);
            float frameRate = Mathf.Max(1f, radiantRushClipFrameRate);
            int sprintStartFrame = Mathf.Max(0, radiantRushSprintStartFrame);
            float sprintStartNormalized =
                Mathf.Clamp01((sprintStartFrame / frameRate) / clipLength);

            Vector3 startupFacing = player.position - transform.position;
            startupFacing.y = 0f;
            if (startupFacing.sqrMagnitude > 0.001f)
                FaceDirection(startupFacing);

            if (stateInfo.normalizedTime >= sprintStartNormalized)
                BeginRadiantRushSprint();

            return;
        }

        if (radiantRushPhase == RadiantRushPhase.Sword)
        {
            UpdateRadiantRushSword(stateInfo);
            return;
        }

        if (radiantRushPhase != RadiantRushPhase.Sprint)
            return;

        if (!sunbindFollowupSprint &&
            radiantRushSprintStartedAt >= 0f &&
            Time.time - radiantRushSprintStartedAt >= Mathf.Max(0.1f, sunbindAntiKiteSeconds))
        {
            BeginSunbind();
            return;
        }

        float attackRange = Mathf.Max(0.1f, radiantRushSwordAttackRange);
        if (sunbindFollowupSprint)
            attackRange = Mathf.Max(0.1f, sunbindSunSlashRange);
        float stopDistance = Mathf.Clamp(radiantRushStoppingDistance, 0.05f, attackRange);
        float playerDistance = PlanarDistanceToPlayer();
        if (playerDistance <= attackRange)
        {
            if (sunbindFollowupSprint)
            {
                sunbindFollowupSprint = false;
                radiantRushPhase = RadiantRushPhase.None;
                BeginSunSlash();
            }
            else
            {
                BeginRadiantRushSword();
            }
            return;
        }

        // Dedicated sprint animation owns the visual run cycle. The controller
        // owns world movement and continuously pursues Van until sword range.
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude <= 0.001f)
            return;

        radiantRushDirection = toPlayer.normalized;
        // Never let the controller-driven sprint overshoot Van. The stopping
        // distance is independent from the sword trigger range so the final
        // approach can be tuned visually without changing melee reach.
        float remainingTravel = Mathf.Max(0f, playerDistance - stopDistance);
        float step = Mathf.Min(
            Mathf.Max(0f, radiantRushSpeed) * Time.deltaTime,
            remainingTravel);
        if (step <= 0f)
        {
            BeginRadiantRushSword();
            return;
        }

        Vector3 desiredPosition = transform.position + radiantRushDirection * step;
        int areaMask = agent != null ? agent.areaMask : NavMesh.AllAreas;

        if (!NavMesh.SamplePosition(
                desiredPosition,
                out NavMeshHit hit,
                Mathf.Max(0.1f, radiantRushNavMeshSampleRadius),
                areaMask))
        {
            Debug.LogWarning(
                "[SunboundWarden] Radiant Rush run step could not be sampled on the NavMesh; holding this frame.",
                this);
            return;
        }

        transform.position = hit.position;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.nextPosition = hit.position;

        FaceDirection(radiantRushDirection);
    }

    private void BeginRadiantRushSprint()
    {
        if (animator == null)
            return;

        int sprintHash = Animator.StringToHash(radiantRushSprintStateName);
        if (!animator.HasState(0, sprintHash))
        {
            Debug.LogWarning(
                $"[SunboundWarden] Animator state '{radiantRushSprintStateName}' is missing.",
                this);
            return;
        }

        radiantRushPhase = RadiantRushPhase.Sprint;
        radiantRushSprintStartedAt = Time.time;
        animator.CrossFadeInFixedTime(sprintHash, 0.05f, 0, 0f);
        Debug.Log("[SunboundWarden] Radiant Rush entered dedicated Sprint phase.", this);
    }

    private void BeginSunbind()
    {
        ClearGuardedTileTracking();
        CloseCrouchSlashHitboxInternal();
        ResolveAnimator();
        if (animator == null)
            return;

        int sunbindHash = Animator.StringToHash(sunbindStateName);
        if (!animator.HasState(0, sunbindHash))
        {
            Debug.LogWarning($"[SunboundWarden] Animator state '{sunbindStateName}' is missing.", this);
            radiantRushSprintStartedAt = Time.time;
            return;
        }

        StopMovement();
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            if (agent.hasPath)
                agent.ResetPath();

            // Sunbind can now be entered directly from CrouchGuard as the
            // six-second anti-stall branch. Its follow-up sprint uses the same
            // controller-owned world movement as Radiant Rush, so transfer
            // position ownership here even when no Rush preceded the cast.
            agent.updatePosition = false;
            agent.nextPosition = transform.position;
        }
        radiantRushPhase = RadiantRushPhase.None;
        radiantRushAttackCommitted = false;
        sunbindEffectApplied = false;
        state = WardenState.Sunbind;
        animator.CrossFadeInFixedTime(sunbindHash, 0.06f, 0, 0f);
        Debug.Log("[SunboundWarden] Entering Sunbind.", this);
    }

    private void UpdateSunbind()
    {
        ResolveAnimator();
        if (animator == null)
            return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (!stateInfo.IsName(sunbindStateName))
            return;

        ResolvePlayer();
        if (player != null)
            FaceDirection(player.position - transform.position);

        if (stateInfo.normalizedTime >= 0.985f)
        {
            if (!sunbindEffectApplied)
                HandleSunbindEffect();
            BeginSunbindFollowupSprint();
        }
    }

    // Animation Event hook. Sunbind is guaranteed once the anti-kite branch
    // starts, so this applies directly to the current player with no range check.
    public void HandleSunbindEffect()
    {
        if (state != WardenState.Sunbind || sunbindEffectApplied)
            return;

        ResolvePlayer();
        if (player == null)
            return;

        PlayerMovement currentMovement =
            player.GetComponent<PlayerMovement>() ??
            player.GetComponentInParent<PlayerMovement>() ??
            player.GetComponentInChildren<PlayerMovement>();

        MovementofPlayer legacyMovement =
            player.GetComponent<MovementofPlayer>() ??
            player.GetComponentInParent<MovementofPlayer>() ??
            player.GetComponentInChildren<MovementofPlayer>();

        float multiplier = Mathf.Clamp(sunbindSlowMultiplier, 0.01f, 1f);
        float duration = Mathf.Max(0.01f, sunbindSlowDuration);
        if (currentMovement != null)
            currentMovement.ApplySpeedDebuff(multiplier, duration);
        else if (legacyMovement != null)
            legacyMovement.ApplySpeedDebuff(multiplier, duration);
        else
        {
            Debug.LogWarning("[SunboundWarden] Sunbind found Van but no supported movement component.", this);
            return;
        }

        sunbindEffectApplied = true;
        Debug.Log($"[SunboundWarden] Sunbind guaranteed slow applied: x{multiplier:0.00} for {duration:0.00}s.", this);
    }

    private void BeginSunbindFollowupSprint()
    {
        sunbindFollowupSprint = true;
        state = WardenState.RadiantRush;
        BeginRadiantRushSprint();
        Debug.Log("[SunboundWarden] Sunbind -> pursuit sprint; next contact uses Sun Slash directly.", this);
    }

    private void BeginRadiantRushSword()
    {
        if (animator == null || player == null)
            return;

        int swordHash = Animator.StringToHash(radiantRushSwordStateName);
        if (!animator.HasState(0, swordHash))
        {
            Debug.LogWarning(
                $"[SunboundWarden] Animator state '{radiantRushSwordStateName}' is missing.",
                this);
            return;
        }

        radiantRushAttackCommitted = true;
        radiantRushPhase = RadiantRushPhase.Sword;

        Vector3 committedFacing = player.position - transform.position;
        committedFacing.y = 0f;
        if (committedFacing.sqrMagnitude > 0.001f)
        {
            radiantRushDirection = committedFacing.normalized;
            FaceDirection(radiantRushDirection);
        }

        float sourceClipLength = 2.4666667f;
        float frameRate = Mathf.Max(1f, radiantRushClipFrameRate);
        float swordStartNormalized = Mathf.Clamp01(
            (Mathf.Max(0, radiantRushSwordStartFrame) / frameRate) /
            Mathf.Max(0.001f, sourceClipLength));

        animator.CrossFade(swordHash, 0.03f, 0, swordStartNormalized);

        Debug.Log(
            $"[SunboundWarden] Radiant Rush sword commit at {PlanarDistanceToPlayer():0.00}u.",
            this);
    }

    private void UpdateRadiantRushSword(AnimatorStateInfo stateInfo)
    {
        if (!stateInfo.IsName(radiantRushSwordStateName) || radiantRushSwordResolved)
            return;

        // Resolve very near the end of the non-looping source clip. Using the
        // state's real normalized time keeps the HIT/MISS branch stable. The
        // sword damage window itself is controlled exclusively by Animation
        // Events through the relay's Open/CloseRadiantRushSwordHitbox().
        if (stateInfo.normalizedTime >= 0.985f)
            FinishRadiantRushSword();
    }

    public void HandleRadiantRushSwordHitboxOpen()
    {
        if (!battleStarted ||
            isDead ||
            state != WardenState.RadiantRush ||
            radiantRushPhase != RadiantRushPhase.Sword ||
            !radiantRushAttackCommitted ||
            radiantRushSwordResolved)
        {
            return;
        }

        ResolveSwordHitbox();
        if (swordHitbox == null)
        {
            Debug.LogWarning(
                "[SunboundWarden] Animation Event tried to open the sword hitbox, but no SunboundWardenSwordHitbox was found.",
                this);
            return;
        }

        swordHitbox.OpenDamageWindow();
        radiantRushHitboxWindowOpen = true;
        Debug.Log("[SunboundWarden] Animation Event -> sword hitbox OPEN.", this);
    }

    public void HandleRadiantRushSwordHitboxClose()
    {
        CloseRadiantRushSwordHitboxInternal();
        Debug.Log("[SunboundWarden] Animation Event -> sword hitbox CLOSED.", this);
    }

    private void CloseRadiantRushSwordHitboxInternal(float sourceFrame = -1f)
    {
        ResolveSwordHitbox();
        if (swordHitbox != null)
            swordHitbox.CloseDamageWindow();

        if (!radiantRushHitboxWindowOpen)
            return;

        radiantRushHitboxWindowOpen = false;
        if (sourceFrame >= 0f)
        {
            Debug.Log(
                $"[SunboundWarden] Radiant Rush sword hitbox CLOSED at source frame {sourceFrame:0.0}.",
                this);
        }
    }

    private void FinishRadiantRushSword()
    {
        if (radiantRushSwordResolved)
            return;

        radiantRushSwordResolved = true;
        CloseRadiantRushSwordHitboxInternal();

        if (radiantRushHitConfirmed)
        {
            Debug.Log("[SunboundWarden] Radiant Rush resolved as HIT -> Sun Slash.", this);
            BeginSunSlash();
        }
        else
        {
            Debug.Log("[SunboundWarden] Radiant Rush resolved as MISS -> Recovery_Merged.", this);
            BeginRecovery();
        }
    }

    private void BeginSunSlash()
    {
        ResolveAnimator();
        if (animator == null)
        {
            BeginRecovery();
            return;
        }

        int sunSlashHash = Animator.StringToHash(sunSlashStateName);
        if (!animator.HasState(0, sunSlashHash))
        {
            Debug.LogWarning(
                $"[SunboundWarden] Animator state '{sunSlashStateName}' is missing; falling back to Recovery_Merged.",
                this);
            BeginRecovery();
            return;
        }

        state = WardenState.SunSlash;
        radiantRushPhase = RadiantRushPhase.None;
        sunSlashHitConfirmed = false;
        CloseSunSlashHitboxInternal();
        StopMovement();
        animator.CrossFadeInFixedTime(sunSlashHash, 0.05f, 0, 0f);
    }

    private void UpdateSunSlash()
    {
        ResolveAnimator();
        if (animator == null)
            return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (!stateInfo.IsName(sunSlashStateName))
            return;

        if (stateInfo.normalizedTime >= 0.985f)
        {
            Debug.Log("[SunboundWarden] Sun Slash finished -> Recovery_Merged.", this);
            BeginRecovery();
        }
    }

    private void BeginRecovery()
    {
        ResolveAnimator();
        CloseRadiantRushSwordHitboxInternal();
        CloseSunSlashHitboxInternal();
        radiantRushPhase = RadiantRushPhase.None;
        radiantRushAttackCommitted = false;
        recoveryStateEntered = false;
        recoveryStartedAt = Time.time;

        RestoreAgentPositionOwnership();
        StopMovement();

        if (animator == null)
        {
            ResolvePostRecoveryDecision();
            return;
        }

        int recoveryHash = Animator.StringToHash(recoveryStateName);
        if (!animator.HasState(0, recoveryHash))
        {
            Debug.LogWarning(
                $"[SunboundWarden] Animator state '{recoveryStateName}' is missing; resolving the post-recovery branch immediately.",
                this);
            ResolvePostRecoveryDecision();
            return;
        }

        state = WardenState.Recovery;
        animator.CrossFadeInFixedTime(recoveryHash, 0.05f, 0, 0f);
    }

    private void UpdateRecovery()
    {
        ResolveAnimator();
        if (animator == null)
        {
            ResolvePostRecoveryDecision();
            return;
        }

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName(recoveryStateName))
        {
            recoveryStateEntered = true;

            if (stateInfo.normalizedTime < 0.985f)
                return;

            Debug.Log(
                "[SunboundWarden] Recovery_Merged finished; scanning remaining world tiles.",
                this);
            ResolvePostRecoveryDecision();
            return;
        }

        if (animator.IsInTransition(0))
        {
            AnimatorStateInfo nextStateInfo = animator.GetNextAnimatorStateInfo(0);
            if (nextStateInfo.IsName(recoveryStateName))
                return;
        }

        if (recoveryStateEntered)
        {
            Debug.Log(
                "[SunboundWarden] Animator left Recovery_Merged before controller resolution; continuing post-recovery behavior.",
                this);
            ResolvePostRecoveryDecision();
            return;
        }

        if (recoveryStartedAt >= 0f &&
            Time.time - recoveryStartedAt >= 0.5f)
        {
            Debug.LogWarning(
                "[SunboundWarden] Recovery_Merged did not become active within 0.5s; resolving post-recovery behavior to avoid an AI stall.",
                this);
            ResolvePostRecoveryDecision();
        }
    }

    private void ResolvePostRecoveryDecision()
    {
        recoveryStateEntered = false;
        recoveryStartedAt = -1f;

        if (!battleStarted || isDead)
            return;

        if (TryFindTileGuardDestination(out Vector3 destination))
        {
            StartTileWalk(destination);
            return;
        }

        Debug.Log(
            "[SunboundWarden] Post-recovery scan found no reachable world tiles -> Radiant Rush.",
            this);
        pendingGuardCluster.Clear();
        BeginRadiantRush();
    }

    private void UpdateCrouchGuard()
    {
        int remaining = RefreshGuardedTileCount();
        if (remaining <= 0)
        {
            Debug.Log(
                "[SunboundWarden] Guarded cluster exhausted -> Sunbind before renewed offense.",
                this);
            BeginSunbind();
            return;
        }

        if (tileGuardStartedAt >= 0f &&
            Time.time - tileGuardStartedAt >= Mathf.Max(0.1f, tileGuardMaxSeconds))
        {
            Debug.Log(
                $"[SunboundWarden] Guard timeout ({tileGuardMaxSeconds:0.##}s) with {remaining} tile(s) left -> Sunbind anti-kite.",
                this);
            BeginSunbind();
            return;
        }

        ResolvePlayer();
        if (player == null)
            return;

        float distance = PlanarDistanceToPlayer();
        if (Time.time < nextCrouchSlashAllowedAt)
            return;

        if (guardCounterQueued && distance <= Mathf.Max(0.1f, crouchSlashTriggerRange + 0.75f))
        {
            guardCounterQueued = false;
            BeginCrouchSlash("tile pickup counter");
            return;
        }

        if (distance <= Mathf.Max(0.1f, crouchSlashTriggerRange))
            BeginCrouchSlash("proximity guard");
    }

    private void BeginCrouchSlash(string reason)
    {
        ResolveAnimator();
        if (animator == null)
            return;

        int slashHash = Animator.StringToHash(crouchSlashStateName);
        if (!animator.HasState(0, slashHash))
        {
            Debug.LogWarning(
                $"[SunboundWarden] Animator state '{crouchSlashStateName}' is missing; crouch guard remains active.",
                this);
            return;
        }

        state = WardenState.CrouchSlash;
        StopMovement();
        crouchSlashHitConfirmed = false;
        crouchSlashHitboxWindowOpen = false;
        nextCrouchSlashAllowedAt =
            Time.time + Mathf.Max(0.1f, crouchSlashRepeatCooldown);

        ResolvePlayer();
        if (player != null)
            FaceDirection(player.position - transform.position);

        animator.CrossFadeInFixedTime(slashHash, 0.04f, 0, 0f);
        Debug.Log($"[SunboundWarden] Crouch Slash -> {reason}.", this);
    }

    private void UpdateCrouchSlash()
    {
        int remaining = RefreshGuardedTileCount();
        if (remaining <= 0)
        {
            CloseCrouchSlashHitboxInternal();
            Debug.Log(
                "[SunboundWarden] Last guarded tile collected during Crouch Slash -> Sunbind before renewed offense.",
                this);
            BeginSunbind();
            return;
        }

        if (tileGuardStartedAt >= 0f &&
            Time.time - tileGuardStartedAt >= Mathf.Max(0.1f, tileGuardMaxSeconds))
        {
            CloseCrouchSlashHitboxInternal();
            Debug.Log(
                $"[SunboundWarden] Guard timeout reached during Crouch Slash with {remaining} tile(s) left -> Sunbind anti-kite.",
                this);
            BeginSunbind();
            return;
        }

        ResolveAnimator();
        if (animator == null)
            return;

        ResolvePlayer();
        if (player != null)
            FaceDirection(player.position - transform.position);

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (!stateInfo.IsName(crouchSlashStateName))
            return;

        if (stateInfo.normalizedTime < 0.985f)
            return;

        // Safety close in case the animation is interrupted after its open
        // event but before the close event can fire.
        CloseCrouchSlashHitboxInternal();
        state = WardenState.CrouchGuard;

        int idleHash = Animator.StringToHash(crouchGuardIdleStateName);
        if (animator.HasState(0, idleHash))
            animator.CrossFadeInFixedTime(idleHash, 0.08f, 0, 0f);
    }

    // Animation Event hook. Put this on the first frame where the Crouch Slash
    // blade should physically damage Van.
    public void HandleCrouchSlashHitboxOpen()
    {
        if (!battleStarted || isDead || state != WardenState.CrouchSlash)
            return;

        ResolveSwordHitbox();
        if (swordHitbox == null)
            return;

        crouchSlashHitConfirmed = false;
        crouchSlashHitboxWindowOpen = true;
        swordHitbox.OpenDamageWindow();
        Debug.Log("[SunboundWarden] Animation Event -> Crouch Slash hitbox OPEN.", this);
    }

    // Animation Event hook. Put this immediately after the active blade swing.
    public void HandleCrouchSlashHitboxClose()
    {
        bool wasOpen = crouchSlashHitboxWindowOpen;
        CloseCrouchSlashHitboxInternal();

        if (wasOpen)
            Debug.Log("[SunboundWarden] Animation Event -> Crouch Slash hitbox CLOSED.", this);
    }

    private void CloseCrouchSlashHitboxInternal()
    {
        ResolveSwordHitbox();
        if (swordHitbox != null && crouchSlashHitboxWindowOpen)
            swordHitbox.CloseDamageWindow();

        crouchSlashHitboxWindowOpen = false;
    }

    public bool TryResolveCrouchSlashHit(Collider other)
    {
        if (other == null ||
            !battleStarted ||
            isDead ||
            state != WardenState.CrouchSlash ||
            !crouchSlashHitboxWindowOpen ||
            crouchSlashHitConfirmed)
        {
            return false;
        }

        ResolvePlayer();
        if (player == null || other.transform.root != player.root)
            return false;

        PlayerHealth playerHealth =
            player.GetComponent<PlayerHealth>() ??
            player.GetComponentInParent<PlayerHealth>() ??
            player.GetComponentInChildren<PlayerHealth>();
        if (playerHealth == null)
            return false;

        crouchSlashHitConfirmed = true;
        playerHealth.TakeDamage(Mathf.Max(0f, crouchSlashDamage));
        Debug.Log(
            $"[SunboundWarden] Crouch Slash HIT confirmed for {crouchSlashDamage:0.##} damage.",
            this);
        return true;
    }

    public bool TryResolveSwordHit(Collider other)
    {
        if (other == null ||
            !battleStarted ||
            isDead ||
            state != WardenState.RadiantRush ||
            !radiantRushAttackCommitted ||
            radiantRushHitConfirmed)
        {
            return false;
        }

        ResolvePlayer();
        if (player == null || other.transform.root != player.root)
            return false;

        PlayerHealth playerHealth =
            player.GetComponent<PlayerHealth>() ??
            player.GetComponentInParent<PlayerHealth>() ??
            player.GetComponentInChildren<PlayerHealth>();

        if (playerHealth == null)
        {
            Debug.LogWarning(
                "[SunboundWarden] Sword hit Van's hierarchy, but PlayerHealth was not found.",
                this);
            return false;
        }

        radiantRushHitConfirmed = true;
        playerHealth.TakeDamage(Mathf.Max(0f, radiantRushSwordDamage));

        Debug.Log(
            $"[SunboundWarden] Radiant Rush sword HIT confirmed for " +
            $"{Mathf.Max(0f, radiantRushSwordDamage):0.##} damage.",
            this);
        return true;
    }

    // Animation Event hooks for the Sun Slash clip. They intentionally reuse
    // the physical sword collider so damage follows the blade instead of an
    // invisible distance check.
    public void HandleSunSlashHitboxOpen()
    {
        if (!battleStarted || isDead || state != WardenState.SunSlash)
            return;

        ResolveSwordHitbox();
        if (swordHitbox == null)
        {
            Debug.LogWarning(
                "[SunboundWarden] Animation Event tried to open the Sun Slash hitbox, but no sword hitbox was found.",
                this);
            return;
        }

        sunSlashHitConfirmed = false;
        sunSlashHitboxWindowOpen = true;
        swordHitbox.OpenDamageWindow();
        Debug.Log("[SunboundWarden] Animation Event -> Sun Slash hitbox OPEN.", this);
    }

    public void HandleSunSlashHitboxClose()
    {
        CloseSunSlashHitboxInternal();
    }

    private void CloseSunSlashHitboxInternal()
    {
        ResolveSwordHitbox();
        if (swordHitbox != null && sunSlashHitboxWindowOpen)
            swordHitbox.CloseDamageWindow();

        sunSlashHitboxWindowOpen = false;
    }

    public bool TryResolveSunSlashHit(Collider other)
    {
        if (other == null ||
            !battleStarted ||
            isDead ||
            state != WardenState.SunSlash ||
            !sunSlashHitboxWindowOpen ||
            sunSlashHitConfirmed)
        {
            return false;
        }

        ResolvePlayer();
        if (player == null || other.transform.root != player.root)
            return false;

        PlayerHealth playerHealth =
            player.GetComponent<PlayerHealth>() ??
            player.GetComponentInParent<PlayerHealth>() ??
            player.GetComponentInChildren<PlayerHealth>();
        if (playerHealth == null)
            return false;

        sunSlashHitConfirmed = true;
        playerHealth.TakeDamage(Mathf.Max(0f, sunSlashDamage));
        Debug.Log($"[SunboundWarden] Sun Slash sword HIT confirmed for {sunSlashDamage:0.##} damage.", this);
        return true;
    }

    public void TriggerDeath()
    {
        if (isDead)
            return;

        isDead = true;
        deathFinished = false;
        deathAnimationEventReceived = false;
        battleStarted = false;
        state = WardenState.Dead;
        CancelStandingHitOverlay();
        ClearGuardedTileTracking();
        CloseCrouchSlashHitboxInternal();
        CloseRadiantRushSwordHitboxInternal();
        CloseSunSlashHitboxInternal();
        recoveryStateEntered = false;
        recoveryStartedAt = -1f;

        StopMovement();
        SetWalkingToTiles(false);
        SetWalkingToPlayer(false);
        SetCombatTargetable(false);

        if (agent != null && agent.enabled)
            agent.enabled = false;

        ResolveAnimator();
        if (animator != null && !string.IsNullOrWhiteSpace(deathStateName))
        {
            int deathHash = Animator.StringToHash(deathStateName);
            if (animator.HasState(0, deathHash))
            {
                if (!string.IsNullOrWhiteSpace(deathParameterName))
                    animator.SetBool(deathParameterName, true);

                StopDeathFallbackRoutine();
                deathFallbackRoutine = StartCoroutine(DeathFallbackRoutine());
            }
            else
            {
                Debug.LogWarning(
                    $"[SunboundWarden] Animator state '{deathStateName}' is missing; completing defeat immediately.",
                    this);
                CompleteDeath(false);
            }
        }
        else
        {
            Debug.LogWarning(
                "[SunboundWarden] Animator missing; completing defeat immediately.",
                this);
            CompleteDeath(false);
        }

        Debug.Log("[SunboundWarden] Death state reached. Waiting for death animation completion.", this);
    }

    public void OnDeathFinished()
    {
        CompleteDeath(true);
    }

    private void CompleteDeath(bool animationEventReceived)
    {
        if (!isDead || deathFinished)
            return;

        deathAnimationEventReceived = animationEventReceived;
        deathFinished = true;
        StopDeathFallbackRoutine();
        NotifyDefeatReceiver();
        Debug.Log(
            animationEventReceived
                ? "[SunboundWarden] Death Animation Event received. Animation finished; progression notified; disabling enemy."
                : "[SunboundWarden] Death completed without Animation Event. Progression notified; disabling enemy via fallback.",
            this);

        gameObject.SetActive(false);
    }

    private IEnumerator DeathFallbackRoutine()
    {
        yield return new WaitForSeconds(Mathf.Max(0.05f, deathFallbackFinishTime));
        deathFallbackRoutine = null;

        if (isDead && !deathFinished)
        {
            Debug.LogWarning(
                "[SunboundWarden] Death Animation Event did not fire; fallback completed defeat.",
                this);
            CompleteDeath(false);
        }
    }

    private void StopDeathFallbackRoutine()
    {
        if (deathFallbackRoutine == null)
            return;

        StopCoroutine(deathFallbackRoutine);
        deathFallbackRoutine = null;
    }

    /// <summary>
    /// Future attack/recovery states call this when the Warden should return
    /// to the densest remaining group of world letter tiles.
    /// </summary>
    public bool BeginWalkToTileCluster()
    {
        if (!battleStarted || isDead)
            return false;

        if (!TryFindTileGuardDestination(out Vector3 destination))
        {
            SetWalkingToTiles(false);
            StopMovement();
            Debug.Log("[SunboundWarden] No active/reachable world-tile cluster found.", this);
            return false;
        }

        return StartTileWalk(destination);
    }

    private bool StartTileWalk(Vector3 destination)
    {
        if (!battleStarted || isDead)
            return false;

        ClearGuardedTileTracking();
        RestoreAgentPositionOwnership();

        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
        {
            Debug.LogWarning("[SunboundWarden] Cannot run to tiles because NavMeshAgent is not ready.", this);
            return false;
        }

        tileGuardDestination = destination;
        state = WardenState.WalkingToTiles;
        SetWalkingToTiles(true);

        agent.isStopped = false;
        agent.speed = Mathf.Max(0.1f, tileRunSpeed);
        agent.acceleration = acceleration;
        agent.angularSpeed = rotationSpeed;
        agent.stoppingDistance = Mathf.Max(0.05f, tileGuardArrivalDistance);
        agent.autoBraking = true;
        agent.autoRepath = true;

        if (!agent.SetDestination(tileGuardDestination))
        {
            SetWalkingToTiles(false);
            StopMovement();
            Debug.LogWarning($"[SunboundWarden] NavMesh rejected tile-guard destination {tileGuardDestination:F2}.", this);
            return false;
        }

        // SetDestination can be called while the previous attack/recovery frame
        // still has the agent stopped. Reassert movement after the path request
        // so the first run frame cannot inherit the recovery stop.
        agent.isStopped = false;

        ResolveAnimator();
        if (animator != null && !string.IsNullOrWhiteSpace(greatSwordRunStateName))
        {
            int runHash = Animator.StringToHash(greatSwordRunStateName);
            if (animator.HasState(0, runHash))
                animator.CrossFadeInFixedTime(runHash, 0.12f, 0, 0f);
        }

        Debug.Log(
            $"[SunboundWarden] Running to tile cluster center {tileGuardDestination:F2} at {agent.speed:0.##} m/s.",
            this);
        return true;
    }

    private void UpdateWalkingToTiles()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
        {
            SetWalkingToTiles(false);
            Debug.LogWarning(
                "[SunboundWarden] Tile run lost its NavMeshAgent -> Radiant Rush fallback.",
                this);
            BeginRadiantRush();
            return;
        }

        if (agent.pathPending)
            return;

        if (!agent.hasPath || agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            SetWalkingToTiles(false);
            Debug.LogWarning(
                "[SunboundWarden] Tile run has no valid path -> Radiant Rush fallback.",
                this);
            BeginRadiantRush();
            return;
        }

        if (agent.isStopped)
            agent.isStopped = false;

        Vector3 facing = agent.velocity;
        facing.y = 0f;
        if (facing.sqrMagnitude > 0.01f)
            FaceDirection(facing);

        float arrival = Mathf.Max(0.05f, tileGuardArrivalDistance);
        if (agent.remainingDistance > arrival)
            return;

        StopMovement();
        SetWalkingToTiles(false);
        EnterCrouchGuard();
    }

    private void EnterCrouchGuard()
    {
        StopMovement();
        SetWalkingToTiles(false);
        state = WardenState.CrouchGuard;
        tileGuardStartedAt = Time.time;
        nextCrouchSlashAllowedAt = Time.time;
        guardCounterQueued = false;
        BindGuardedTileTracking();

        ResolveAnimator();
        if (animator != null)
        {
            int crouchIdleHash = Animator.StringToHash("IDLE");
            if (animator.HasState(0, crouchIdleHash))
                animator.CrossFadeInFixedTime(crouchIdleHash, 0.18f, 0, 0f);
        }

        int guardedCount = RefreshGuardedTileCount();
        Debug.Log(
            $"[SunboundWarden] Reached tile cluster; blending Great Sword Run -> Crouch Guard. Guarding {guardedCount} tile(s), timeout={tileGuardMaxSeconds:0.##}s.",
            this);

        if (guardedCount <= 0)
            BeginSunbind();
    }

    private bool TryFindTileGuardDestination(out Vector3 destination)
    {
        destination = transform.position;
        pendingGuardCluster.Clear();

        WorldTile[] found = Object.FindObjectsByType<WorldTile>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        List<WorldTile> tiles = new List<WorldTile>(found.Length);
        foreach (WorldTile tile in found)
        {
            if (tile == null || !tile.gameObject.activeInHierarchy)
                continue;

            // TileSpawner can also use WorldTile for UI-pool entries. The Warden
            // only guards physical world pickups, never HUD tiles.
            if (tile.GetComponentInParent<Canvas>() != null)
                continue;

            tiles.Add(tile);
        }

        if (tiles.Count == 0)
            return false;

        Vector3 rawCenter;
        List<WorldTile> selectedCluster = new List<WorldTile>();
        if (tiles.Count == 1)
        {
            rawCenter = tiles[0].transform.position;
            selectedCluster.Add(tiles[0]);
        }
        else
        {
            List<WorldTile> bestCluster = FindDensestConnectedTileCluster(tiles);

            if (bestCluster.Count >= 2)
            {
                rawCenter = ComputeTileCentroid(bestCluster);
                selectedCluster.AddRange(bestCluster);
            }
            else
            {
                // Fully scattered case: guard the midpoint of the closest pair,
                // matching the design that he protects whichever letters are
                // nearest to one another.
                FindClosestTilePair(tiles, out WorldTile first, out WorldTile second);
                rawCenter = (first.transform.position + second.transform.position) * 0.5f;
                selectedCluster.Add(first);
                selectedCluster.Add(second);
            }
        }

        int areaMask = agent != null ? agent.areaMask : NavMesh.AllAreas;
        if (NavMesh.SamplePosition(
                rawCenter,
                out NavMeshHit hit,
                Mathf.Max(0.25f, tileGuardNavMeshSampleRadius),
                areaMask))
        {
            destination = hit.position;
            pendingGuardCluster.AddRange(selectedCluster);
            return true;
        }

        // If the centroid lands on scenery or a NavMesh gap, try the actual
        // closest physical tile as a safe fallback before giving up.
        WorldTile fallback = FindClosestTileToPoint(tiles, rawCenter);
        if (fallback != null &&
            NavMesh.SamplePosition(
                fallback.transform.position,
                out hit,
                Mathf.Max(0.25f, tileGuardNavMeshSampleRadius),
                areaMask))
        {
            destination = hit.position;
            pendingGuardCluster.AddRange(selectedCluster);
            return true;
        }

        pendingGuardCluster.Clear();
        return false;
    }

    private void BindGuardedTileTracking()
    {
        ClearGuardedTileTracking();

        foreach (WorldTile tile in pendingGuardCluster)
        {
            if (tile == null || !tile.gameObject.activeInHierarchy)
                continue;

            if (!guardedTiles.Add(tile))
                continue;

            tile.Collected += HandleGuardedTileCollected;
        }
    }

    private void HandleGuardedTileCollected(WorldTile tile)
    {
        if (tile == null || !guardedTiles.Remove(tile))
            return;

        tile.Collected -= HandleGuardedTileCollected;
        int remaining = RefreshGuardedTileCount();

        Debug.Log(
            $"[SunboundWarden] Guarded tile collected. Remaining guarded tiles: {remaining}.",
            this);

        if (!battleStarted || isDead)
            return;

        if (remaining <= 0)
        {
            guardCounterQueued = false;
            CloseCrouchSlashHitboxInternal();
            Debug.Log(
                "[SunboundWarden] All guarded tiles collected -> Sunbind before renewed offense.",
                this);
            BeginSunbind();
            return;
        }

        guardCounterQueued = true;

        if (state != WardenState.CrouchGuard ||
            Time.time < nextCrouchSlashAllowedAt)
        {
            return;
        }

        ResolvePlayer();
        if (player != null &&
            PlanarDistanceToPlayer() <= Mathf.Max(0.1f, crouchSlashTriggerRange + 0.75f))
        {
            guardCounterQueued = false;
            BeginCrouchSlash("tile pickup counter");
        }
    }

    private int RefreshGuardedTileCount()
    {
        if (guardedTiles.Count == 0)
            return 0;

        List<WorldTile> stale = null;
        foreach (WorldTile tile in guardedTiles)
        {
            if (tile != null && tile.gameObject.activeInHierarchy)
                continue;

            stale ??= new List<WorldTile>();
            stale.Add(tile);
        }

        if (stale != null)
        {
            foreach (WorldTile tile in stale)
            {
                if (tile != null)
                    tile.Collected -= HandleGuardedTileCollected;
                guardedTiles.Remove(tile);
            }
        }

        return guardedTiles.Count;
    }

    private void ClearGuardedTileTracking()
    {
        foreach (WorldTile tile in guardedTiles)
        {
            if (tile != null)
                tile.Collected -= HandleGuardedTileCollected;
        }

        guardedTiles.Clear();
        tileGuardStartedAt = -1f;
        guardCounterQueued = false;
    }

    private List<WorldTile> FindDensestConnectedTileCluster(List<WorldTile> tiles)
    {
        float linkDistance = Mathf.Max(0.1f, tileClusterLinkDistance);
        float linkDistanceSqr = linkDistance * linkDistance;
        HashSet<WorldTile> unvisited = new HashSet<WorldTile>(tiles);
        List<WorldTile> best = new List<WorldTile>();
        float bestCompactness = float.PositiveInfinity;

        while (unvisited.Count > 0)
        {
            WorldTile seed = null;
            foreach (WorldTile candidate in unvisited)
            {
                seed = candidate;
                break;
            }

            Queue<WorldTile> queue = new Queue<WorldTile>();
            List<WorldTile> cluster = new List<WorldTile>();
            queue.Enqueue(seed);
            unvisited.Remove(seed);

            while (queue.Count > 0)
            {
                WorldTile current = queue.Dequeue();
                cluster.Add(current);

                List<WorldTile> linked = new List<WorldTile>();
                foreach (WorldTile candidate in unvisited)
                {
                    Vector3 delta = candidate.transform.position - current.transform.position;
                    delta.y = 0f;
                    if (delta.sqrMagnitude <= linkDistanceSqr)
                        linked.Add(candidate);
                }

                foreach (WorldTile candidate in linked)
                {
                    unvisited.Remove(candidate);
                    queue.Enqueue(candidate);
                }
            }

            float compactness = ComputeClusterCompactness(cluster);
            if (cluster.Count > best.Count ||
                (cluster.Count == best.Count && compactness < bestCompactness))
            {
                best = cluster;
                bestCompactness = compactness;
            }
        }

        return best;
    }

    private static Vector3 ComputeTileCentroid(List<WorldTile> tiles)
    {
        Vector3 sum = Vector3.zero;
        foreach (WorldTile tile in tiles)
            sum += tile.transform.position;
        return sum / Mathf.Max(1, tiles.Count);
    }

    private static float ComputeClusterCompactness(List<WorldTile> tiles)
    {
        if (tiles.Count <= 1)
            return float.PositiveInfinity;

        Vector3 center = ComputeTileCentroid(tiles);
        float total = 0f;
        foreach (WorldTile tile in tiles)
        {
            Vector3 delta = tile.transform.position - center;
            delta.y = 0f;
            total += delta.sqrMagnitude;
        }

        return total / tiles.Count;
    }

    private static void FindClosestTilePair(
        List<WorldTile> tiles,
        out WorldTile first,
        out WorldTile second)
    {
        first = tiles[0];
        second = tiles[1];
        float bestDistanceSqr = float.PositiveInfinity;

        for (int i = 0; i < tiles.Count - 1; i++)
        {
            for (int j = i + 1; j < tiles.Count; j++)
            {
                Vector3 delta = tiles[j].transform.position - tiles[i].transform.position;
                delta.y = 0f;
                float distanceSqr = delta.sqrMagnitude;
                if (distanceSqr >= bestDistanceSqr)
                    continue;

                bestDistanceSqr = distanceSqr;
                first = tiles[i];
                second = tiles[j];
            }
        }
    }

    private static WorldTile FindClosestTileToPoint(List<WorldTile> tiles, Vector3 point)
    {
        WorldTile best = null;
        float bestDistanceSqr = float.PositiveInfinity;

        foreach (WorldTile tile in tiles)
        {
            Vector3 delta = tile.transform.position - point;
            delta.y = 0f;
            float distanceSqr = delta.sqrMagnitude;
            if (distanceSqr >= bestDistanceSqr)
                continue;

            bestDistanceSqr = distanceSqr;
            best = tile;
        }

        return best;
    }

    private void SetWalkingToTiles(bool walking)
    {
        ResolveAnimator();
        if (animator == null || string.IsNullOrWhiteSpace(walkingToTilesParameter))
            return;

        animator.SetBool(walkingToTilesParameter, walking);
    }

    private void SetWalkingToPlayer(bool walking)
    {
        ResolveAnimator();
        if (animator == null || string.IsNullOrWhiteSpace(walkingToPlayerParameter))
            return;

        animator.SetBool(walkingToPlayerParameter, walking);
    }

    private void StopMovement()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        agent.isStopped = true;
        if (agent.hasPath)
            agent.ResetPath();
    }

    private void FaceDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime);
    }

    private void ConfigureAgent()
    {
        if (agent == null)
            return;

        agent.updatePosition = true;
        agent.updateRotation = false;
        agent.speed = moveSpeed;
        agent.acceleration = acceleration;
        agent.angularSpeed = rotationSpeed;
        agent.stoppingDistance = tileGuardArrivalDistance;
        agent.autoBraking = true;
        agent.autoRepath = true;
    }

    private void ResolvePlayer()
    {
        if (player != null && player.gameObject.activeInHierarchy)
            return;

        player = null;
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
            player = playerObject.transform;
    }

    private void ResolveSwordHitbox()
    {
        if (swordHitbox != null)
            return;

        swordHitbox = GetComponentInChildren<SunboundWardenSwordHitbox>(true);
    }

    private void SetCombatTargetable(bool targetable)
    {
        if (damageCollider == null)
            damageCollider = GetComponent<CapsuleCollider>();

        if (damageCollider != null)
            damageCollider.enabled = targetable;

        if (!hideFromTargetingBeforeBattle)
            return;

        if (targetable)
        {
            if (!string.IsNullOrWhiteSpace(combatTag))
                gameObject.tag = combatTag;
        }
        else if (!CompareTag("Untagged"))
        {
            gameObject.tag = "Untagged";
        }
    }

    private void NotifyDefeatReceiver()
    {
        if (defeatNotified)
            return;

        defeatNotified = true;
        if (deathReceiver == null || string.IsNullOrWhiteSpace(deathMessage))
            return;

        deathReceiver.SendMessage(deathMessage, SendMessageOptions.DontRequireReceiver);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = transform.position;

        float slashRange = Mathf.Max(0.1f, crouchSlashTriggerRange);
        Gizmos.color = new Color(1f, 0.85f, 0.15f, 0.95f);
        Gizmos.DrawWireSphere(center, slashRange);

        float counterRange = slashRange + 0.75f;
        Gizmos.color = new Color(1f, 0.45f, 0.1f, 0.75f);
        Gizmos.DrawWireSphere(center, counterRange);
    }
}
