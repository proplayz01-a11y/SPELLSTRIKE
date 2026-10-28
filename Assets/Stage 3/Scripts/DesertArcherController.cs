using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations.Rigging;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public sealed class DesertArcherController : MonoBehaviour
{
    private const string BattleActiveParameter = "BattleActive";
    private const string FullBodyActionParameter = "FullBodyAction";
    private const string MoveXParameter = "MoveX";
    private const string MoveZParameter = "MoveZ";
    private const string QuickShotTriggerParameter = "QuickShotTrigger";
    private const string RollTriggerParameter = "RollTrigger";
    private const string ChargedRuneTriggerParameter = "ChargedRuneTrigger";

    private enum CombatState
    {
        Dormant,
        SoftAim,
        QuickShot,
        Rolling,
        ChargedRuneShot,
        PiercingRuneShot,
        Dead
    }

    [Header("Core References")]
    [SerializeField] private Animator animator;
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private NavMeshAgent navAgent;
    [SerializeField] private CapsuleCollider combatCollider;
    [SerializeField] private Transform player;
    [SerializeField] private GameObject arrowProjectilePrefab;

    [Header("Weapon / Socket References")]
    [SerializeField] private Transform heldBow;
    [SerializeField] private Transform heldArrowVisual;
    [SerializeField] private Transform heldArrowSocket;
    [SerializeField] private Transform arrowReleaseSpawn;
    [SerializeField] private Transform aimTarget;
    [SerializeField] private DesertArcherBowDrawDriver bowDrawDriver;

    [Header("Tile Defense / Aim Assist")]
    [SerializeField] private Transform tileClusterCenterOverride;
    [SerializeField, Min(0.1f)] private float aimAssistStartRadius = 18f;
    [SerializeField, Min(0.1f)] private float tileLockRadius = 6f;
    [SerializeField, Range(0f, 1f)] private float minimumAimAssistWeight = 0.25f;
    [SerializeField, Min(1f)] private float softTurnSpeed = 90f;
    [SerializeField, Min(1f)] private float hardTurnSpeed = 360f;
    [SerializeField, Min(1f)] private float maximumAttackDistance = 40f;
    [SerializeField, Min(0.05f)] private float tileScanInterval = 0.25f;

    [Header("Defensive Locomotion")]
    [SerializeField] private bool enableDefensiveLocomotion = true;
    [SerializeField, Min(0.5f)] private float defenseAnchorRadius = 7f;
    [Tooltip("Maximum distance from the Node 3 arena center that kiting destinations may use.")]
    [SerializeField, Min(1f)] private float kiteLeashRadius = 50f;
    [SerializeField, Min(0.1f)] private float locomotionSpeed = 3.4f;
    [SerializeField, Min(0.05f)] private float accelerationTime = 0.28f;
    [SerializeField] private bool keepMovingDuringUpperBodyShots = true;
    [Tooltip("Charged Rune and Piercing Rune move at this fraction of normal kite speed while sniping.")]
    [SerializeField, Range(0.05f, 1f)] private float sniperShotMoveSpeedMultiplier = 0.28f;
    [SerializeField, Min(0.5f)] private float kiteRetreatDistance = 5.5f;
    [SerializeField, Min(0.5f)] private float kiteStepDistance = 4f;
    [SerializeField, Min(0.1f)] private float kiteDirectionHoldTime = 1.6f;
    [SerializeField, Min(0.05f)] private float kiteRepathInterval = 0.3f;
    [SerializeField, Min(0f)] private float kiteStoppingDistance = 0.35f;

    [Header("Anti-Cover Pressure")]
    [Tooltip("Stops players from safely cheesing the encounter by sitting behind solid cover.")]
    [SerializeField] private bool enableAntiCoverPressure = true;
    [Tooltip("How long line of sight may remain blocked before the Archer starts actively flanking.")]
    [SerializeField, Min(0.1f)] private float coverGraceTime = 1.25f;
    [Tooltip("How often line of sight is sampled while combat is active.")]
    [SerializeField, Min(0.02f)] private float coverSightCheckInterval = 0.10f;
    [Tooltip("SoftAim movement multiplier while breaking sustained cover.")]
    [SerializeField, Range(1f, 2f)] private float coverFlankSpeedMultiplier = 1.20f;
    [Tooltip("How quickly the Archer may repath while trying to expose a hiding player.")]
    [SerializeField, Min(0.05f)] private float coverFlankRepathInterval = 0.15f;
    [Tooltip("Maximum orbit radius used when searching for a firing angle around the player.")]
    [SerializeField, Min(2f)] private float coverFlankRadius = 10f;
    [Tooltip("Minimum delay before another sustained-cover punish can be armed.")]
    [SerializeField, Min(0f)] private float coverPunishCooldown = 4f;

    [Header("Facing")]
    [SerializeField] private bool debugFacingLogs;

    [Header("Upper Body Bow Aim Rig")]
    [SerializeField] private Rig upperBodyAimRig;
    [SerializeField] private Rig bowPrecisionAimRig;
    [SerializeField] private MultiAimConstraint upperBodyAimConstraint;
    [SerializeField] private MultiAimConstraint bowHandAimConstraint;
    [SerializeField] private Transform bowAimReference;
    [SerializeField] private bool enableUpperBodyAimRig = true;
    [SerializeField, Range(0f, 1f)] private float upperBodyAimWeight = 1f;
    [SerializeField, Min(0.1f)] private float bowAimDebugInterval = 0.5f;

    [Header("Roll")]
    [SerializeField, Min(0f)] private float rollSpeed = 6.5f;
    [Tooltip("World-space travel distance for each post-burst tumble. Scene gizmos show both possible lateral endpoints.")]
    [SerializeField, Min(0f)] private float rollDistance = 4f;
    [SerializeField, Min(0.25f)] private float rollNavMeshProbeRadius = 1.5f;
    [SerializeField] private bool drawRollDistanceGizmo;

    [Header("Arrow Damage")]
    [SerializeField, Min(0f)] private float quickArrowBaseDamage = 6f;
    [SerializeField, Min(0f)] private float chargedRuneBaseDamage = 10f;
    [SerializeField, Min(0f)] private float piercingRuneBaseDamage = 8f;
    [SerializeField, Min(0f)] private float nearDamageDistance = 5f;
    [SerializeField, Min(0.1f)] private float farDamageDistance = 25f;
    [SerializeField, Min(0f)] private float nearDamageMultiplier = 1f;
    [SerializeField, Min(0f)] private float farDamageMultiplier = 1.8f;

    [Header("Projectile Flight")]
    [SerializeField, Min(0.1f)] private float quickArrowSpeed = 24f;
    [SerializeField, Min(0.1f)] private float chargedRuneSpeed = 28f;
    [SerializeField, Min(0.1f)] private float piercingRuneSpeed = 34f;
    [SerializeField, Min(0.25f)] private float maxProjectileTravelTime = 3.25f;
    [SerializeField, Min(1f)] private float maxLongRangeProjectileSpeed = 90f;
    [SerializeField, Min(0.1f)] private float projectileLifetime = 4f;
    [SerializeField, Min(0.02f)] private float projectileHitRadius = 0.22f;
    [SerializeField] private float playerAimHeight = 1.1f;

    [Header("Combo")]
    [SerializeField, Min(1)] private int quickShotsPerCombo = 3;
    [SerializeField, Min(0f)] private float firstBurstDelay = 0.75f;
    [Tooltip("Desert Archer's signature is uninterrupted pressure. Keep this at 0 for no downtime between combat cycles.")]
    [SerializeField, Min(0f)] private float comboCooldown = 0f;

    [Header("Animator State Names")]
    [SerializeField] private string idleStateName = "Idle";
    [SerializeField] private string quickShotStateName = "Quick Shot";
    [SerializeField] private string rollStateName = "Roll Forward";
    [SerializeField] private string chargedRuneStateName = "Charged Rune Shot";
    [SerializeField] private string aimWalkBackStateName = "Standing Aim Walk Back";
    [SerializeField] private string aimWalkLeftStateName = "Standing Aim Walk Left";
    [SerializeField] private string aimWalkRightStateName = "Standing Aim Walk Right";
    [SerializeField] private string upperBodyShotLayerName = "Upper Body Shots";
    [SerializeField] private string lowerBodyLocomotionLayerName = "Lower Body Locomotion";
    [SerializeField] private string deathStateName = "Death";
    [SerializeField, Min(0f)] private float animatorTransitionSeconds = 0.06f;

    [Header("Death")]
    [Tooltip("Safety net only. The authored OnDeathFinished Animation Event is authoritative.")]
    [SerializeField, Min(0.1f)] private float deathCompletionFallbackTime = 4f;
    [Tooltip("Drops one persistent collectible Desert Rune Arrow beside the corpse when Death finishes.")]
    [SerializeField] private bool dropRuneArrowOnDeath = true;
    [SerializeField] private string deathRuneArrowItemName = "Desert Rune Arrow";
    [SerializeField, Min(1)] private int deathRuneArrowAmount = 1;
    [SerializeField, Min(0.15f)] private float deathRuneArrowPickupRadius = 1.0f;
    [SerializeField, Min(0f)] private float deathRuneArrowGroundOffset = 0.08f;

    [Header("Aim Hit Reaction")]
    [Tooltip("Additive torso/head-only layer used for regular damage reactions without interrupting bow attacks.")]
    [SerializeField] private string aimHitReactionLayerName = "Aim Hit Reaction";
    [SerializeField] private string aimHitReactionStateName = "Aim Hit Reaction";
    [Tooltip("Keeps the recoil readable without yanking the bow pose too far off its authored aim.")]
    [SerializeField, Range(0f, 1f)] private float aimHitReactionWeight = 0.65f;
    [Tooltip("The additive reaction begins fading out after this normalized point in the non-looping clip.")]
    [SerializeField, Range(0.1f, 0.99f)] private float aimHitReactionFadeOutStart = 0.72f;

    [Header("Animation Timing Safety Fallbacks")]
    // Keep these safely after the authored Animation Events. They are emergency
    // recovery only, not the primary timing source. Values that sit on the same
    // frame as an event can win Update ordering and silently bypass the relay.
    [SerializeField, Min(0.1f)] private float quickReleaseFallbackTime = 1.15f;
    [SerializeField, Min(0.1f)] private float quickEndFallbackTime = 1.85f;
    [SerializeField, Min(0.1f)] private float chargedReleaseFallbackTime = 5.40f;
    [SerializeField, Min(0.1f)] private float chargedEndFallbackTime = 6.08f;
    [SerializeField, Min(0.1f)] private float rollEndFallbackTime = 1.70f;

    [Header("Defeat Callback")]
    [SerializeField] private MonoBehaviour deathReceiver;
    [SerializeField] private string deathMessage = "OnEnemyDefeated";

    [Header("Debug")]
    [SerializeField] private bool debugLogs;
    [SerializeField] private bool debugShotMovementLogs;
    [SerializeField] private bool drawAimGizmos;

    private CombatState state = CombatState.Dormant;
    private DesertArcherArrowType activeArrowType = DesertArcherArrowType.Quick;
    private Vector3 tileClusterPosition;
    private bool hasTileCluster;
    private float nextTileScanTime;
    private float aimAssistWeight;
    private float nextComboAllowedTime;
    private float stateStartedTime;
    private int quickShotCount;
    private bool releaseHandled;
    private bool drawEndHandled;
    private bool rollEndHandled;
    private bool chargedAnimationEnded;
    private bool chargedResultReceived;
    private bool chargedHit;
    private bool droughtMarkConfirmed;
    private Vector3 rollDirection;
    private float rollTravelled;
    private bool battleStarted;
    private string currentLocomotionState;
    private string currentLowerBodyLocomotionState;
    private bool warnedMissingTileSource;
    private bool warnedMissingProjectile;
    private int quickArrowsSpawnedTotal;
    private int chargedRuneArrowsSpawnedTotal;
    private int piercingRuneArrowsSpawnedTotal;
    private int chargedRuneHitsTotal;
    private bool defeatNotified;
    private bool deathCompleted;
    private float deathStartedTime = -1f;
    private bool heldArrowWasVisibleAtDeath;
    private bool deathRuneArrowSpawned;
    private int upperBodyShotLayerIndex = -1;
    private int lowerBodyLocomotionLayerIndex = -1;
    private int aimHitReactionLayerIndex = -1;
    private bool aimHitReactionActive;
    private int aimHitReactionStartedFrame = -1;
    private bool warnedMissingAimHitReaction;
    private int kiteStrafeSide = 1;
    private float nextKiteDirectionSwapTime;
    private float nextKiteRepathTime;
    private float nextCoverSightCheckTime;
    private float coverBlockedSince = -1f;
    private float nextCoverPunishAllowedTime;
    private bool playerLineOfSightClear = true;
    private bool coverPressureActive;
    private bool coverPunishPending;
    private int antiCoverPunishesStartedTotal;
    private int preferredRollSide = 1;
    private int lastRollSide;
    private float lastRollLateralDot;
    private int rollsStartedTotal;
    private int aimWalkBackUsedTotal;
    private int aimWalkLeftUsedTotal;
    private int aimWalkRightUsedTotal;
    private int quickShotsReleasedWhileWalkingTotal;
    private int quickShotsReleasedStationaryTotal;
    private float maxQuickReleaseBowAimErrorDegrees;
    private Transform humanoidHips;
    private Transform humanoidTorso;
    private bool hipsFacingReferenceCaptured;
    private float hipsFacingReferenceYaw;
    private bool torsoFacingReferenceCaptured;
    private float torsoFacingReferenceYaw;
    private float visualFacingErrorDegrees;
    private float torsoFacingErrorDegrees;
    private float rootFacingErrorDegrees;
    private float bowAimErrorDegrees;
    private float nextBowAimDebugLogTime;

    public float AimAssistWeight => aimAssistWeight;
    public bool DroughtMarkConfirmed => droughtMarkConfirmed;
    public int QuickShotCount => quickShotCount;
    public int QuickArrowsSpawnedTotal => quickArrowsSpawnedTotal;
    public int ChargedRuneArrowsSpawnedTotal => chargedRuneArrowsSpawnedTotal;
    public int PiercingRuneArrowsSpawnedTotal => piercingRuneArrowsSpawnedTotal;
    public int ChargedRuneHitsTotal => chargedRuneHitsTotal;
    public bool BattleStarted => battleStarted;
    public bool IsDead => state == CombatState.Dead;
    public bool DeathCompleted => deathCompleted;
    public int LastRollSide => lastRollSide;
    public float LastRollLateralDot => lastRollLateralDot;
    public int RollsStartedTotal => rollsStartedTotal;
    public int AimWalkBackUsedTotal => aimWalkBackUsedTotal;
    public int AimWalkLeftUsedTotal => aimWalkLeftUsedTotal;
    public int AimWalkRightUsedTotal => aimWalkRightUsedTotal;
    public int QuickShotsReleasedWhileWalkingTotal => quickShotsReleasedWhileWalkingTotal;
    public int QuickShotsReleasedStationaryTotal => quickShotsReleasedStationaryTotal;
    public float MaxQuickReleaseBowAimErrorDegrees => maxQuickReleaseBowAimErrorDegrees;
    public float VisualFacingErrorDegrees => visualFacingErrorDegrees;
    public float TorsoFacingErrorDegrees => torsoFacingErrorDegrees;
    public float RootFacingErrorDegrees => rootFacingErrorDegrees;
    public float BowAimErrorDegrees => bowAimErrorDegrees;
    public bool HeldArrowVisible => heldArrowVisual != null && heldArrowVisual.gameObject.activeSelf;
    public float BowDraw01 => bowDrawDriver != null ? bowDrawDriver.CurrentDraw01 : 0f;
    public bool PlayerLineOfSightClear => playerLineOfSightClear;
    public bool CoverPressureActive => coverPressureActive;
    public bool CoverPunishPending => coverPunishPending;
    public int AntiCoverPunishesStartedTotal => antiCoverPunishesStartedTotal;

    private void Awake()
    {
        animator ??= GetComponent<Animator>();
        enemyHealth ??= GetComponent<EnemyHealth>();
        navAgent ??= GetComponent<NavMeshAgent>();
        combatCollider ??= GetComponent<CapsuleCollider>();
        bowDrawDriver ??= GetComponent<DesertArcherBowDrawDriver>();

        AutoResolveWeaponReferences();
        if (upperBodyAimRig == null)
        {
            Rig[] rigs = GetComponentsInChildren<Rig>(true);
            for (int i = 0; i < rigs.Length; i++)
            {
                if (rigs[i] != null && rigs[i].name == "UpperBodyAimRig")
                {
                    upperBodyAimRig = rigs[i];
                    break;
                }
            }
        }
        if (bowPrecisionAimRig == null)
        {
            Rig[] rigs = GetComponentsInChildren<Rig>(true);
            for (int i = 0; i < rigs.Length; i++)
            {
                if (rigs[i] != null && rigs[i].name == "BowPrecisionAimRig")
                {
                    bowPrecisionAimRig = rigs[i];
                    break;
                }
            }
        }
        upperBodyAimConstraint ??= GetComponentInChildren<MultiAimConstraint>(true);
        if (bowHandAimConstraint == null)
        {
            MultiAimConstraint[] aimConstraints = GetComponentsInChildren<MultiAimConstraint>(true);
            for (int i = 0; i < aimConstraints.Length; i++)
            {
                if (aimConstraints[i] != null && aimConstraints[i] != upperBodyAimConstraint
                    && aimConstraints[i].name == "BowHandAim_MultiAim")
                {
                    bowHandAimConstraint = aimConstraints[i];
                    break;
                }
            }
        }
        bowAimReference ??= arrowReleaseSpawn;
        if (animator != null && animator.isHuman)
        {
            humanoidHips = animator.GetBoneTransform(HumanBodyBones.Hips);
            humanoidTorso = animator.GetBoneTransform(HumanBodyBones.UpperChest)
                ?? animator.GetBoneTransform(HumanBodyBones.Chest)
                ?? animator.GetBoneTransform(HumanBodyBones.Spine);
        }
        ResolveUpperBodyShotLayer();
        ResolveLowerBodyLocomotionLayer();
        ResolveAimHitReactionLayer();
        SetUpperBodyShotLayerWeight(0f);
        SetLowerBodyLocomotionLayerWeight(0f);
        SetAimHitReactionLayerWeight(0f);
        ResetAnimatorCombatParameters();
        PlayAnimatorState(idleStateName, false);
        SetHeldArrowVisible(false);
        bowDrawDriver?.SetDraw01(0f);
        UpdateUpperBodyAimRigWeight();

        if (navAgent != null)
        {
            navAgent.updateRotation = false;
            navAgent.stoppingDistance = kiteStoppingDistance;
            EnemyNavMovement.ApplySpeed(navAgent, locomotionSpeed, accelerationTime);
        }

        // Establish the authored encounter's dormant state before the first
        // physics/Update tick. Node 3's trigger is the only production entry to
        // StartBattle; the enemy must not be hittable or moving beforehand.
        MaintainPreBattleState();
    }

    private void Update()
    {
        UpdateUpperBodyAimRigWeight();
        UpdateAimHitReactionLayer();
        UpdateDeathCompletionFallback();

        if (!battleStarted)
        {
            if (state != CombatState.Dead)
                MaintainPreBattleState();
            return;
        }

        if (state == CombatState.Dead)
            return;

        ResolvePlayer();
        RefreshTileClusterIfNeeded();
        UpdateCoverPressure();
        UpdateAimAssist();
        UpdateAimTarget();
        UpdateFacing();
        UpdateBowDraw();
        UpdateRollMovement();
        UpdateAnimationEventFallbacks();

        if (state == CombatState.Rolling)
            return;

        if (IsShotState(state))
        {
            // Shot clips own the full Base Layer. Charged/Piercing additionally use
            // a legs-only locomotion layer so they visibly walk while preserving the
            // authored torso/bow pose. Quick Shot keeps its already-approved setup.
            if (keepMovingDuringUpperBodyShots
                && state == CombatState.QuickShot)
            {
                UpdateDefensiveLocomotion();
            }
            else if (keepMovingDuringUpperBodyShots
                && (state == CombatState.ChargedRuneShot
                    || state == CombatState.PiercingRuneShot))
            {
                UpdateDefensiveLocomotion();
                UpdateLowerBodyLocomotionAnimation();
            }
            return;
        }

        UpdateDefensiveLocomotion();
        UpdateLocomotionAnimation();

        if (TryStartAntiCoverPunish())
            return;

        if (CanAcquireHardLock())
            BeginQuickShotCombo();
    }

    private void LateUpdate()
    {
        if (animator == null || humanoidHips == null)
            return;

        if (!battleStarted)
        {
            CaptureHipsFacingReference();
            CaptureTorsoFacingReference();
            return;
        }

        if (state == CombatState.Dormant
            || state == CombatState.Dead
            || state == CombatState.Rolling
            || player == null)
        {
            return;
        }

        // Diagnostics are evaluated after Animator + Animation Rigging so this
        // measures the final visual skeleton without mutating the hips/root pose.
        Vector3 targetDirection = player.position - transform.position;
        targetDirection.y = 0f;
        if (targetDirection.sqrMagnitude <= 0.001f)
            return;

        targetDirection.Normalize();
        CaptureHipsFacingReference();
        CaptureTorsoFacingReference();

        ApplyFinalBowAimCorrection();
        UpdateBowAimError();

        Vector3 rootForward = transform.forward;
        rootForward.y = 0f;
        if (rootForward.sqrMagnitude > 0.001f)
        {
            rootForward.Normalize();
            rootFacingErrorDegrees = Mathf.Abs(Vector3.SignedAngle(rootForward, targetDirection, Vector3.up));
        }

        Vector3 desiredHipsForward = Quaternion.AngleAxis(hipsFacingReferenceYaw, Vector3.up) * targetDirection;
        Vector3 currentHipsForward = humanoidHips.forward;
        currentHipsForward.y = 0f;
        if (currentHipsForward.sqrMagnitude <= 0.001f)
            return;

        currentHipsForward.Normalize();
        visualFacingErrorDegrees = Mathf.Abs(Vector3.SignedAngle(currentHipsForward, desiredHipsForward, Vector3.up));

        if (humanoidTorso != null && torsoFacingReferenceCaptured)
        {
            Vector3 desiredTorsoForward = Quaternion.AngleAxis(torsoFacingReferenceYaw, Vector3.up) * targetDirection;
            Vector3 currentTorsoForward = humanoidTorso.forward;
            currentTorsoForward.y = 0f;
            if (currentTorsoForward.sqrMagnitude > 0.001f)
            {
                currentTorsoForward.Normalize();
                torsoFacingErrorDegrees = Mathf.Abs(Vector3.SignedAngle(currentTorsoForward, desiredTorsoForward, Vector3.up));
            }
        }

        if (debugLogs && debugFacingLogs && Time.time >= nextBowAimDebugLogTime)
        {
            nextBowAimDebugLogTime = Time.time + bowAimDebugInterval;
            float rigWeight = upperBodyAimRig != null ? upperBodyAimRig.weight : 0f;
            Debug.Log(
                $"[DesertArcher][BOW-AIM] state={state} walking={IsActivelyKiting()} " +
                $"error={bowAimErrorDegrees:0.0}deg rigWeight={rigWeight:0.00} " +
                $"reference='{(bowAimReference != null ? bowAimReference.name : "<none>")}'",
                this);
        }
    }

    private void UpdateUpperBodyAimRigWeight()
    {
        if (upperBodyAimRig == null && bowPrecisionAimRig == null)
            return;

        bool stateAllowsRig = state == CombatState.SoftAim
            || state == CombatState.QuickShot
            || state == CombatState.PiercingRuneShot;

        float targetWeight = enableUpperBodyAimRig && battleStarted && stateAllowsRig
            ? upperBodyAimWeight
            : 0f;

        // Quick Shot and Piercing both reuse the authored Draw -> Aim Recoil motion
        // that also bookends Charged Rune v003. Preserve that animation's natural
        // sideways archer twist instead of re-aiming Spine2 procedurally. The bow
        // precision rig remains active, so the weapon line can still finish exactly
        // on Van while the lower body continues the In-Place aim-walk locomotion.
        bool authoredShotTorso = state == CombatState.QuickShot
            || state == CombatState.PiercingRuneShot;
        float torsoWeight = authoredShotTorso ? 0f : targetWeight;

        if (upperBodyAimRig != null)
            upperBodyAimRig.weight = torsoWeight;
        if (bowPrecisionAimRig != null)
            bowPrecisionAimRig.weight = state == CombatState.QuickShot ? 0f : targetWeight;
    }

    private void ApplyFinalBowAimCorrection()
    {
        if (!battleStarted
            || player == null
            || bowAimReference == null
            || bowHandAimConstraint == null)
        {
            return;
        }

        bool stateAllowsFinalCorrection = state == CombatState.SoftAim
            || state == CombatState.PiercingRuneShot;
        if (!stateAllowsFinalCorrection)
            return;

        MultiAimConstraintData precisionData = bowHandAimConstraint.data;
        Transform bowPivot = precisionData.constrainedObject;
        if (bowPivot == null)
            return;

        Vector3 toTarget = GetPlayerAimPoint() - bowAimReference.position;
        Vector3 currentBowForward = bowAimReference.forward;
        if (toTarget.sqrMagnitude <= 0.0001f || currentBowForward.sqrMagnitude <= 0.0001f)
            return;

        Quaternion correction = Quaternion.FromToRotation(
            currentBowForward.normalized,
            toTarget.normalized);
        bowPivot.rotation = correction * bowPivot.rotation;
    }

    private void UpdateBowAimError()
    {
        if (bowAimReference == null || player == null)
        {
            bowAimErrorDegrees = 180f;
            return;
        }

        Vector3 toTarget = GetPlayerAimPoint() - bowAimReference.position;
        if (toTarget.sqrMagnitude <= 0.0001f)
        {
            bowAimErrorDegrees = 0f;
            return;
        }

        Vector3 bowForward = bowAimReference.forward;
        if (bowForward.sqrMagnitude <= 0.0001f)
        {
            bowAimErrorDegrees = 180f;
            return;
        }

        bowAimErrorDegrees = Vector3.Angle(bowForward.normalized, toTarget.normalized);
    }

    private string GetBowAxisDebugText()
    {
        if (bowAimReference == null || player == null)
            return "<unavailable>";

        Vector3 toTarget = GetPlayerAimPoint() - bowAimReference.position;
        if (toTarget.sqrMagnitude <= 0.0001f)
            return "target-overlap";

        toTarget.Normalize();
        float px = Vector3.Angle(bowAimReference.right, toTarget);
        float nx = Vector3.Angle(-bowAimReference.right, toTarget);
        float py = Vector3.Angle(bowAimReference.up, toTarget);
        float ny = Vector3.Angle(-bowAimReference.up, toTarget);
        float pz = Vector3.Angle(bowAimReference.forward, toTarget);
        float nz = Vector3.Angle(-bowAimReference.forward, toTarget);
        float rigTargetError = aimTarget != null
            ? Vector3.Angle(
                bowAimReference.forward,
                (aimTarget.position - bowAimReference.position).normalized)
            : 180f;
        float rigTargetDistanceFromPlayer = aimTarget != null
            ? Vector3.Distance(aimTarget.position, GetPlayerAimPoint())
            : -1f;
        Vector3 torsoLocalBow = humanoidTorso != null
            ? humanoidTorso.InverseTransformDirection(bowAimReference.forward).normalized
            : Vector3.zero;
        string handCalibration = "handCal=<none>";
        if (bowHandAimConstraint != null)
        {
            MultiAimConstraintData handData = bowHandAimConstraint.data;
            Transform precisionObject = handData.constrainedObject;
            if (precisionObject != null)
            {
                Vector3 handLocalBow = precisionObject.InverseTransformDirection(bowAimReference.forward).normalized;
                Vector3 handAimAxis = DebugAimAxisVector(handData.aimAxis);
                Quaternion offsetRotation = Quaternion.Euler(handData.offset);
                float forwardMapError = Vector3.Angle(offsetRotation * handLocalBow, handAimAxis);
                float inverseMapError = Vector3.Angle(Quaternion.Inverse(offsetRotation) * handLocalBow, handAimAxis);
                Vector3 fromPrecisionToTarget = aimTarget != null
                    ? aimTarget.position - precisionObject.position
                    : Vector3.zero;
                float precisionOriginError = fromPrecisionToTarget.sqrMagnitude > 0.0001f
                    ? Vector3.Angle(precisionObject.forward, fromPrecisionToTarget.normalized)
                    : 180f;
                Vector3 preOffsetAimWorld = precisionObject.rotation
                    * Quaternion.Inverse(offsetRotation)
                    * handAimAxis;
                float preOffsetAimError = fromPrecisionToTarget.sqrMagnitude > 0.0001f
                    ? Vector3.Angle(preOffsetAimWorld.normalized, fromPrecisionToTarget.normalized)
                    : 180f;
                float constraintWeight = bowHandAimConstraint.weight;
                float sourceWeight = handData.sourceObjects.Count > 0
                    ? handData.sourceObjects[0].weight
                    : 0f;
                float releaseOffset = Vector3.Distance(precisionObject.position, bowAimReference.position);
                float targetDistance = fromPrecisionToTarget.magnitude;
                handCalibration =
                    $"precision='{precisionObject.name}' localBow=({handLocalBow.x:0.000},{handLocalBow.y:0.000},{handLocalBow.z:0.000}) " +
                    $"precisionAxis={handData.aimAxis} precisionOffset=({handData.offset.x:0.0},{handData.offset.y:0.0},{handData.offset.z:0.0}) " +
                    $"mapErr={forwardMapError:0.0} invMapErr={inverseMapError:0.0} " +
                    $"preOffsetAimErr={preOffsetAimError:0.0} constraintWeight={constraintWeight:0.00} sourceWeight={sourceWeight:0.00} " +
                    $"precisionOriginErr={precisionOriginError:0.0} releaseOffset={releaseOffset:0.00} targetDist={targetDistance:0.00}";
            }
        }
        return $"+X:{px:0.0}/-X:{nx:0.0} +Y:{py:0.0}/-Y:{ny:0.0} +Z:{pz:0.0}/-Z:{nz:0.0} " +
               $"rigTargetErr={rigTargetError:0.0} rigTargetDelta={rigTargetDistanceFromPlayer:0.00} " +
               $"torsoLocalBow=({torsoLocalBow.x:0.000},{torsoLocalBow.y:0.000},{torsoLocalBow.z:0.000}) {handCalibration}";
    }

    private static Vector3 DebugAimAxisVector(MultiAimConstraintData.Axis axis)
    {
        return axis switch
        {
            MultiAimConstraintData.Axis.X => Vector3.right,
            MultiAimConstraintData.Axis.X_NEG => Vector3.left,
            MultiAimConstraintData.Axis.Y => Vector3.up,
            MultiAimConstraintData.Axis.Y_NEG => Vector3.down,
            MultiAimConstraintData.Axis.Z => Vector3.forward,
            MultiAimConstraintData.Axis.Z_NEG => Vector3.back,
            _ => Vector3.forward
        };
    }

    private void CaptureHipsFacingReference()
    {
        if (hipsFacingReferenceCaptured || humanoidHips == null)
            return;

        Vector3 rootForward = transform.forward;
        Vector3 hipsForward = humanoidHips.forward;
        rootForward.y = 0f;
        hipsForward.y = 0f;
        if (rootForward.sqrMagnitude <= 0.001f || hipsForward.sqrMagnitude <= 0.001f)
            return;

        rootForward.Normalize();
        hipsForward.Normalize();
        hipsFacingReferenceYaw = Vector3.SignedAngle(rootForward, hipsForward, Vector3.up);
        hipsFacingReferenceCaptured = true;

        if (debugLogs && debugFacingLogs)
            Debug.Log($"[DesertArcher][FACING] Idle hips reference yaw={hipsFacingReferenceYaw:0.0} degrees.", this);
    }

    private void CaptureTorsoFacingReference()
    {
        if (torsoFacingReferenceCaptured || humanoidTorso == null)
            return;

        Vector3 rootForward = transform.forward;
        Vector3 torsoForward = humanoidTorso.forward;
        rootForward.y = 0f;
        torsoForward.y = 0f;
        if (rootForward.sqrMagnitude <= 0.001f || torsoForward.sqrMagnitude <= 0.001f)
            return;

        rootForward.Normalize();
        torsoForward.Normalize();
        torsoFacingReferenceYaw = Vector3.SignedAngle(rootForward, torsoForward, Vector3.up);
        torsoFacingReferenceCaptured = true;

        if (debugLogs && debugFacingLogs)
            Debug.Log($"[DesertArcher][FACING] Idle torso reference yaw={torsoFacingReferenceYaw:0.0} degrees.", this);
    }

    private void MaintainPreBattleState()
    {
        state = CombatState.Dormant;

        if (combatCollider != null && combatCollider.enabled)
            combatCollider.enabled = false;

        if (!CompareTag("Untagged"))
            gameObject.tag = "Untagged";

        if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
        {
            if (!navAgent.isStopped)
                navAgent.isStopped = true;
            if (navAgent.hasPath)
                navAgent.ResetPath();
        }

        SetUpperBodyShotLayerWeight(0f);
        SetLowerBodyLocomotionLayerWeight(0f);
        StopAimHitReaction();
        SetAnimatorBool(BattleActiveParameter, false);
        SetAnimatorBool(FullBodyActionParameter, false);
        SetAnimatorLocomotion(0f, 0f);
        SetHeldArrowVisible(false);
        bowDrawDriver?.SetDraw01(0f);

        if (animator != null && animator.layerCount > 0)
        {
            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
            if (!current.IsName(idleStateName))
                PlayAnimatorState(idleStateName, false);
        }
    }

    public void StartBattle()
    {
        if (state == CombatState.Dead)
            return;

        battleStarted = true;
        ResolvePlayer();
        RefreshTileCluster(true);
        state = CombatState.SoftAim;
        UpdateUpperBodyAimRigWeight();
        nextComboAllowedTime = Time.time + firstBurstDelay;
        quickShotCount = 0;
        droughtMarkConfirmed = false;
        defeatNotified = false;
        deathCompleted = false;
        deathStartedTime = -1f;
        heldArrowWasVisibleAtDeath = false;
        deathRuneArrowSpawned = false;
        currentLocomotionState = null;
        kiteStrafeSide = 1;
        nextKiteDirectionSwapTime = Time.time + kiteDirectionHoldTime;
        nextKiteRepathTime = Time.time;
        nextCoverSightCheckTime = Time.unscaledTime;
        coverBlockedSince = -1f;
        nextCoverPunishAllowedTime = Time.unscaledTime;
        playerLineOfSightClear = true;
        coverPressureActive = false;
        coverPunishPending = false;
        antiCoverPunishesStartedTotal = 0;
        preferredRollSide = 1;
        lastRollSide = 0;
        lastRollLateralDot = 0f;
        rollsStartedTotal = 0;
        aimWalkBackUsedTotal = 0;
        aimWalkLeftUsedTotal = 0;
        aimWalkRightUsedTotal = 0;
        quickShotsReleasedWhileWalkingTotal = 0;
        quickShotsReleasedStationaryTotal = 0;
        maxQuickReleaseBowAimErrorDegrees = 0f;
        SetUpperBodyShotLayerWeight(0f);
        SetLowerBodyLocomotionLayerWeight(0f);
        StopAimHitReaction();
        ResetAnimatorTriggers();
        SetAnimatorBool(BattleActiveParameter, true);
        SetAnimatorBool(FullBodyActionParameter, false);
        SetAnimatorLocomotion(0f, 0f);

        if (CompareTag("Untagged"))
            gameObject.tag = "Enemy";

        if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
            navAgent.isStopped = false;

        if (combatCollider != null)
            combatCollider.enabled = true;

        Log($"Battle started. Kiting active; first Quick Shot x{quickShotsPerCombo} burst in {firstBurstDelay:0.00}s.");
    }

    [ContextMenu("Debug/Start Battle")]
    private void DebugStartBattle()
    {
        StartBattle();
    }

    public void TriggerBeingHit()
    {
        // The Archer's normal combat pose is already an aiming pose, so a regular
        // full-body BeingHit state would tear him out of Quick/Charged/Piercing and
        // could also skip the authoritative bow Animation Events. Damage therefore
        // plays a short additive torso/head reaction on its own masked layer while
        // the current Base Layer shot and lower-body locomotion keep running.
        if (!battleStarted || state == CombatState.Dead || state == CombatState.Rolling)
            return;

        PlayAimHitReaction();
    }

    public void TriggerDeath()
    {
        if (state == CombatState.Dead)
            return;

        battleStarted = false;
        state = CombatState.Dead;
        deathCompleted = false;
        deathStartedTime = Time.time;
        heldArrowWasVisibleAtDeath = HeldArrowVisible;
        coverBlockedSince = -1f;
        coverPressureActive = false;
        coverPunishPending = false;
        playerLineOfSightClear = true;
        UpdateUpperBodyAimRigWeight();
        SetUpperBodyShotLayerWeight(0f);
        SetLowerBodyLocomotionLayerWeight(0f);
        StopAimHitReaction();
        ResetAnimatorTriggers();
        SetAnimatorBool(BattleActiveParameter, false);
        SetAnimatorBool(FullBodyActionParameter, false);
        SetAnimatorLocomotion(0f, 0f);
        // If an arrow is physically nocked when the fatal hit lands, keep it in
        // the hand through the authored death motion. It becomes the corpse loot
        // at animation completion instead of vanishing on the fatal frame.
        if (!heldArrowWasVisibleAtDeath)
            SetHeldArrowVisible(false);
        bowDrawDriver?.SetDraw01(0f);
        DisarmOwnedProjectiles();

        if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
        {
            navAgent.ResetPath();
            navAgent.velocity = Vector3.zero;
            navAgent.isStopped = true;
            navAgent.enabled = false;
        }

        if (combatCollider != null)
            combatCollider.enabled = false;

        bool hasDeathState = HasAnimatorState(deathStateName);
        if (hasDeathState)
            PlayAnimatorState(deathStateName, true);

        gameObject.tag = "Untagged";
        if (!hasDeathState)
        {
            Debug.LogWarning(
                $"[DesertArcher] Death state '{deathStateName}' is missing. Completing defeat immediately so progression cannot soft-lock.",
                this);
            CompleteDeath();
            return;
        }

        Log("Death received. Combat logic stopped; waiting for OnDeathFinished.");
    }

    public void HandleDeathFinished()
    {
        if (state != CombatState.Dead || deathCompleted)
            return;

        CompleteDeath();
    }

    private void CompleteDeath()
    {
        if (deathCompleted)
            return;

        deathCompleted = true;
        SpawnDeathRuneArrowPickup();
        SetHeldArrowVisible(false);
        NotifyDefeatReceiver();
        Log("Death animation complete. Defeat receiver notified exactly once.");
    }

    private void UpdateDeathCompletionFallback()
    {
        if (state != CombatState.Dead
            || deathCompleted
            || deathStartedTime < 0f
            || Time.time - deathStartedTime < deathCompletionFallbackTime)
        {
            return;
        }

        Debug.LogWarning(
            $"[DesertArcher] OnDeathFinished did not arrive within {deathCompletionFallbackTime:0.00}s. Completing death via safety fallback.",
            this);
        CompleteDeath();
    }

    private void DisarmOwnedProjectiles()
    {
        DesertArcherArrowProjectile[] activeProjectiles =
            FindObjectsByType<DesertArcherArrowProjectile>(FindObjectsSortMode.None);

        for (int i = 0; i < activeProjectiles.Length; i++)
        {
            DesertArcherArrowProjectile projectile = activeProjectiles[i];
            if (projectile != null && projectile.OwnerController == this)
                projectile.DisarmAfterOwnerDeath();
        }
    }

    private void SpawnDeathRuneArrowPickup()
    {
        if (!dropRuneArrowOnDeath
            || deathRuneArrowSpawned
            || arrowProjectilePrefab == null)
        {
            return;
        }

        deathRuneArrowSpawned = true;

        // Use the corpse root as the authoritative drop anchor. Some authored
        // death poses can carry the hand/socket several metres away from the root,
        // which would make the loot look like it teleported across the arena.
        Vector3 sourcePosition =
            transform.position
            + transform.right * 0.55f
            + transform.forward * 0.20f
            + Vector3.up * 1.0f;
        Quaternion sourceRotation = heldArrowVisual != null
            ? heldArrowVisual.rotation
            : arrowProjectilePrefab.transform.rotation;
        Vector3 sourceScale = heldArrowVisual != null
            ? heldArrowVisual.lossyScale
            : arrowProjectilePrefab.transform.localScale;

        Vector3 dropPosition = sourcePosition;
        Vector3 rayStart = sourcePosition + Vector3.up * 1.5f;
        if (Physics.Raycast(
                rayStart,
                Vector3.down,
                out RaycastHit hit,
                5f,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore))
        {
            dropPosition = hit.point + Vector3.up * deathRuneArrowGroundOffset;
        }
        else
        {
            dropPosition.y = Mathf.Max(
                transform.position.y + deathRuneArrowGroundOffset,
                dropPosition.y);
        }

        GameObject droppedArrow = Instantiate(
            arrowProjectilePrefab,
            dropPosition,
            sourceRotation);
        droppedArrow.name = "S3N3_DesertRuneArrow_Collectible";
        droppedArrow.transform.localScale = sourceScale;

        DesertArcherArrowProjectile projectile =
            droppedArrow.GetComponent<DesertArcherArrowProjectile>();
        if (projectile != null)
        {
            projectile.enabled = false;
            Destroy(projectile);
        }

        SphereCollider sphere = droppedArrow.GetComponent<SphereCollider>();
        if (sphere == null)
            sphere = droppedArrow.AddComponent<SphereCollider>();
        sphere.isTrigger = true;
        sphere.radius = deathRuneArrowPickupRadius;

        Rigidbody body = droppedArrow.GetComponent<Rigidbody>();
        if (body == null)
            body = droppedArrow.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;

        DesertArcherRuneArrowPickup pickup =
            droppedArrow.GetComponent<DesertArcherRuneArrowPickup>();
        if (pickup == null)
            pickup = droppedArrow.AddComponent<DesertArcherRuneArrowPickup>();
        pickup.Configure(
            deathRuneArrowItemName,
            deathRuneArrowAmount,
            deathRuneArrowPickupRadius);

        Log(
            $"Dropped collectible {deathRuneArrowItemName} x{deathRuneArrowAmount} " +
            $"at {dropPosition}.");
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

    public void HandleDrawStarted()
    {
        if (!IsShotState(state))
            return;

        // The held arrow should appear with the authored draw animation, not one
        // frame earlier when combat code changes state. Keeping this event-driven
        // also makes the visual lifecycle deterministic for Quick/Charged/Piercing.
        SetHeldArrowVisible(true);
    }

    public void HandleArrowNocked()
    {
        if (!IsShotState(state))
            return;

        SetHeldArrowVisible(true);
    }

    public void HandleArrowReleased()
    {
        if (!IsShotState(state) || releaseHandled)
            return;

        if (state == CombatState.PiercingRuneShot
            && keepMovingDuringUpperBodyShots)
        {
            UpdateDefensiveLocomotion();
            UpdateLowerBodyLocomotionAnimation();
            if (!IsActivelyKiting())
            {
                if (debugLogs)
                    Log("Piercing release event fired while aim-walk locomotion was not active; honoring the animation event anyway.");
            }

            float releaseAimError = MeasureCurrentBowAimError();
            if (releaseAimError > 3f)
            {
                if (debugLogs)
                    Log($"Piercing release event fired with bow error={releaseAimError:0.0}deg; honoring the animation event anyway.");
            }
        }

        // Capture the launch handoff before hiding the held arrow/resetting the bow.
        // This guarantees the flying arrow starts from the live bow socket on the
        // exact release frame and heads toward Van from that same frame.
        ResolvePlayer();
        Vector3 releasePosition = arrowReleaseSpawn != null
            ? arrowReleaseSpawn.position
            : transform.position;
        Vector3 releaseAimPoint = GetPlayerAimPoint();
        // Match the flying projectile to the exact visible size of the arrow in
        // the right hand. lossyScale is intentional here: it includes every
        // parent/socket/bone scale that contributes to the held arrow on screen.
        Vector3 releaseArrowScale = heldArrowVisual != null
            ? heldArrowVisual.lossyScale
            : arrowProjectilePrefab != null
                ? arrowProjectilePrefab.transform.localScale
                : Vector3.one;

        releaseHandled = true;
        SetHeldArrowVisible(false);
        bowDrawDriver?.SetDraw01(0f);

        LogShotMovementCheck();
        SpawnActiveArrow(releasePosition, releaseAimPoint, releaseArrowScale);

        if (state == CombatState.QuickShot)
        {
            quickShotCount++;
            Log($"Quick Shot released: {quickShotCount}/{quickShotsPerCombo}.");
        }
    }

    public void HandleDrawEnded()
    {
        if (!IsShotState(state) || drawEndHandled)
            return;

        if (!releaseHandled)
        {
            Debug.LogWarning(
                "[DesertArcher] Draw ended before OnArrowReleased. Projectile launch is animation-event authoritative, so no fallback arrow was spawned.",
                this);
            return;
        }

        drawEndHandled = true;

        switch (state)
        {
            case CombatState.QuickShot:
                if (quickShotCount < quickShotsPerCombo)
                    BeginShot(CombatState.QuickShot, DesertArcherArrowType.Quick, quickShotStateName);
                else
                    BeginRoll();
                break;

            case CombatState.ChargedRuneShot:
                chargedAnimationEnded = true;
                TryResolveChargedFollowUp();
                break;

            case CombatState.PiercingRuneShot:
                EndCombo();
                break;
        }
    }

    public void HandleRollStarted()
    {
        if (state != CombatState.Rolling)
            return;

        rollTravelled = 0f;
    }

    public void HandleRollEnded()
    {
        if (state != CombatState.Rolling || rollEndHandled)
            return;

        rollEndHandled = true;
        BeginChargedRuneShot();
    }

    public void ReportProjectileResolved(DesertArcherArrowType type, bool hitPlayer)
    {
        if (state == CombatState.Dead)
            return;

        if (type != DesertArcherArrowType.ChargedRune)
            return;

        chargedResultReceived = true;
        chargedHit = hitPlayer;
        droughtMarkConfirmed = hitPlayer;
        if (hitPlayer)
            chargedRuneHitsTotal++;

        if (hitPlayer)
            Log("Charged Rune hit: Drought Mark confirmed. Piercing follow-up armed.");
        else
            Log("Charged Rune missed: no Drought Mark and no Piercing follow-up.");

        TryResolveChargedFollowUp();
    }

    private void BeginQuickShotCombo()
    {
        quickShotCount = 0;
        droughtMarkConfirmed = false;
        BeginShot(CombatState.QuickShot, DesertArcherArrowType.Quick, quickShotStateName);
        Log("Hard tile lock acquired. Quick Shot x3 sequence started.");
    }

    private void BeginShot(CombatState newState, DesertArcherArrowType type, string animatorState)
    {
        state = newState;
        UpdateUpperBodyAimRigWeight();
        activeArrowType = type;
        stateStartedTime = Time.time;
        releaseHandled = false;
        drawEndHandled = false;
        // Wait for OnDrawStarted so the arrow appears together with the animation.
        // Otherwise the arrow pops into the hand before the first authored frame.
        SetHeldArrowVisible(false);

        // Stationary Quick Shot visual-isolation test: play Quick Shot as a
        // full-body Base Layer action, matching the presentation path used by
        // Charged Rune Shot. This removes the Upper Body Shots Avatar Mask from
        // the equation without changing the authored Quick Shot clip itself.
        // Quick, Charged and Piercing all keep their authored full-body shot on
        // Base Layer. Charged/Piercing get walking from a separate legs-only layer.
        bool useUpperBodyLayer = false;
        bool movingFullBodyShot = (newState == CombatState.QuickShot
            || newState == CombatState.ChargedRuneShot
            || newState == CombatState.PiercingRuneShot)
            && keepMovingDuringUpperBodyShots;
        bool movingUpperBodyShot = useUpperBodyLayer
            && keepMovingDuringUpperBodyShots;

        if (movingFullBodyShot || movingUpperBodyShot)
        {
            ResumeAgentForDefense();
            UpdateDefensiveLocomotion();
        }
        else
        {
            StopAgentForAttack();
        }

        if (useUpperBodyLayer)
        {
            SetAnimatorBool(FullBodyActionParameter, false);
            SetUpperBodyShotLayerWeight(1f);
            if (!SetAnimatorTrigger(QuickShotTriggerParameter))
                RestartAnimatorState(animatorState, upperBodyShotLayerIndex);
        }
        else
        {
            SetAnimatorBool(FullBodyActionParameter, true);
            SetUpperBodyShotLayerWeight(0f);
            if (newState == CombatState.ChargedRuneShot)
            {
                if (!SetAnimatorTrigger(ChargedRuneTriggerParameter))
                    RestartAnimatorState(animatorState, 0);
            }
            else
            {
                RestartAnimatorState(animatorState, 0);
            }
        }

        bool useLowerBodyLocomotion = keepMovingDuringUpperBodyShots
            && (newState == CombatState.ChargedRuneShot
                || newState == CombatState.PiercingRuneShot)
            && lowerBodyLocomotionLayerIndex >= 0;
        SetLowerBodyLocomotionLayerWeight(useLowerBodyLocomotion ? 1f : 0f);
        currentLowerBodyLocomotionState = null;
        if (useLowerBodyLocomotion)
            UpdateLowerBodyLocomotionAnimation();
    }

    private void BeginRoll()
    {
        state = CombatState.Rolling;
        UpdateUpperBodyAimRigWeight();
        stateStartedTime = Time.time;
        rollEndHandled = false;
        rollTravelled = 0f;
        SetHeldArrowVisible(false);
        bowDrawDriver?.SetDraw01(0f);
        SetUpperBodyShotLayerWeight(0f);
        SetLowerBodyLocomotionLayerWeight(0f);
        StopAimHitReaction();

        ChooseLateralRollDirection();
        rollsStartedTotal++;

        if (rollDirection.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(rollDirection, Vector3.up);

        StopAgentForAttack();
        SetAnimatorBool(FullBodyActionParameter, true);
        if (!SetAnimatorTrigger(RollTriggerParameter))
            RestartAnimatorState(rollStateName);
        Log($"Quick Shot x3 complete. Lateral roll started side={(lastRollSide < 0 ? "LEFT" : "RIGHT")} lateralDot={lastRollLateralDot:0.000}.");
    }

    private void BeginChargedRuneShot()
    {
        chargedAnimationEnded = false;
        chargedResultReceived = false;
        chargedHit = false;
        droughtMarkConfirmed = false;
        BeginShot(CombatState.ChargedRuneShot, DesertArcherArrowType.ChargedRune, chargedRuneStateName);
        Log("Roll complete. Charged Rune Shot started.");
    }

    private void BeginPiercingRuneShot()
    {
        // No dedicated Piercing animation exists in the user's current controller,
        // so the follow-up reuses the authored Quick Shot release while the code
        // changes projectile type and behavior.
        BeginShot(CombatState.PiercingRuneShot, DesertArcherArrowType.PiercingRune, quickShotStateName);
        Log("Drought Mark gate passed. Piercing Rune Arrow started.");
    }

    private void TryResolveChargedFollowUp()
    {
        if (state != CombatState.ChargedRuneShot || !chargedAnimationEnded || !chargedResultReceived)
            return;

        if (chargedHit && droughtMarkConfirmed)
            BeginPiercingRuneShot();
        else
            EndCombo();
    }

    private void EndCombo()
    {
        state = CombatState.SoftAim;
        UpdateUpperBodyAimRigWeight();
        nextComboAllowedTime = Time.time + comboCooldown;
        SetUpperBodyShotLayerWeight(0f);
        SetLowerBodyLocomotionLayerWeight(0f);
        SetAnimatorBool(FullBodyActionParameter, false);
        SetHeldArrowVisible(false);
        bowDrawDriver?.SetDraw01(0f);
        currentLocomotionState = null;
        currentLowerBodyLocomotionState = null;

        if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
            navAgent.isStopped = false;

        // No recovery/idle beat between cycles. As soon as the charged/piercing
        // action finishes, immediately reacquire a kite destination and resume
        // lower-body aim-walk locomotion. The next Quick Shot burst may begin on
        // the following Update when comboCooldown is zero.
        UpdateDefensiveLocomotion();
        UpdateLocomotionAnimation();

        Log("Combo complete. Continuous kite pressure resumed with no recovery break.");
    }

    private void SpawnActiveArrow(Vector3 spawnPosition, Vector3 aimPoint, Vector3 projectileScale)
    {
        if (arrowProjectilePrefab == null || arrowReleaseSpawn == null)
        {
            if (!warnedMissingProjectile)
            {
                warnedMissingProjectile = true;
                Debug.LogError("[DesertArcher] Arrow projectile prefab or ArrowReleaseSpawn is missing.", this);
            }

            if (activeArrowType == DesertArcherArrowType.ChargedRune)
                ReportProjectileResolved(activeArrowType, false);
            return;
        }

        float distance = player != null
            ? Vector3.Distance(spawnPosition, aimPoint)
            : nearDamageDistance;
        float damage = ResolveDistanceScaledDamage(activeArrowType, distance);
        float baseSpeed = ResolveProjectileSpeed(activeArrowType);
        float longRangeSpeed = distance / Mathf.Max(0.25f, maxProjectileTravelTime);
        float speed = Mathf.Clamp(
            Mathf.Max(baseSpeed, longRangeSpeed),
            baseSpeed,
            Mathf.Max(baseSpeed, maxLongRangeProjectileSpeed));
        float effectiveLifetime = Mathf.Max(
            projectileLifetime,
            distance / Mathf.Max(0.1f, speed) + 1f);

        Vector3 launchDirection = aimPoint - spawnPosition;
        if (launchDirection.sqrMagnitude <= 0.0001f)
            launchDirection = transform.forward;
        launchDirection.Normalize();

        Quaternion launchRotation = Quaternion.LookRotation(launchDirection, Vector3.up)
            * arrowProjectilePrefab.transform.localRotation;

        GameObject arrow = Instantiate(
            arrowProjectilePrefab,
            spawnPosition,
            launchRotation);
        arrow.name = $"S3N3_{activeArrowType}_Arrow_Runtime";
        arrow.transform.localScale = projectileScale;

        DesertArcherArrowProjectile projectile = arrow.GetComponent<DesertArcherArrowProjectile>();
        if (projectile == null)
            projectile = arrow.AddComponent<DesertArcherArrowProjectile>();

        projectile.Launch(
            this,
            activeArrowType,
            transform,
            player,
            spawnPosition,
            aimPoint,
            damage,
            speed,
            effectiveLifetime,
            projectileHitRadius,
            arrowProjectilePrefab.transform.localRotation);

        switch (activeArrowType)
        {
            case DesertArcherArrowType.ChargedRune:
                chargedRuneArrowsSpawnedTotal++;
                break;
            case DesertArcherArrowType.PiercingRune:
                piercingRuneArrowsSpawnedTotal++;
                break;
            default:
                quickArrowsSpawnedTotal++;
                break;
        }

        Log($"{activeArrowType} arrow spawned | distance={distance:0.0} | damage={damage:0.0}.");
    }

    private float ResolveDistanceScaledDamage(DesertArcherArrowType type, float distance)
    {
        float near = Mathf.Max(0f, nearDamageDistance);
        float far = Mathf.Max(near + 0.01f, farDamageDistance);
        float minMultiplier = Mathf.Max(0f, nearDamageMultiplier);
        float maxMultiplier = Mathf.Max(minMultiplier, farDamageMultiplier);
        float t = Mathf.InverseLerp(near, far, distance);
        float multiplier = Mathf.Lerp(minMultiplier, maxMultiplier, t);

        float baseDamage = type switch
        {
            DesertArcherArrowType.ChargedRune => chargedRuneBaseDamage,
            DesertArcherArrowType.PiercingRune => piercingRuneBaseDamage,
            _ => quickArrowBaseDamage
        };

        return Mathf.Max(0f, baseDamage * multiplier);
    }

    private float ResolveProjectileSpeed(DesertArcherArrowType type)
    {
        return type switch
        {
            DesertArcherArrowType.ChargedRune => chargedRuneSpeed,
            DesertArcherArrowType.PiercingRune => piercingRuneSpeed,
            _ => quickArrowSpeed
        };
    }

    private void RefreshTileClusterIfNeeded()
    {
        if (Time.time < nextTileScanTime)
            return;

        RefreshTileCluster(false);
    }

    private void RefreshTileCluster(bool force)
    {
        if (!force && Time.time < nextTileScanTime)
            return;

        nextTileScanTime = Time.time + tileScanInterval;

        if (tileClusterCenterOverride != null)
        {
            tileClusterPosition = tileClusterCenterOverride.position;
            hasTileCluster = true;
            return;
        }

        // StageNodeTrigger configures this to the active node before StartBattle().
        // Restrict the scan to that arena so stale/replacement tiles from another
        // Stage 3 node cannot drag the defended cluster center across the map.
        TileSpawner tileSpawner = TileSpawner.Instance;
        Transform activeArenaCenter = tileSpawner != null ? tileSpawner.arenaCenter : null;
        float activeArenaRadius = tileSpawner != null ? Mathf.Max(0.5f, tileSpawner.arenaRadius) : 0f;

        WorldTile[] worldTiles = FindObjectsByType<WorldTile>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        Vector3 sum = Vector3.zero;
        int count = 0;

        for (int i = 0; i < worldTiles.Length; i++)
        {
            WorldTile tile = worldTiles[i];
            if (tile == null || !tile.gameObject.activeInHierarchy || tile.GetComponentInParent<Canvas>() != null)
                continue;
            if (activeArenaCenter != null &&
                HorizontalDistance(tile.transform.position, activeArenaCenter.position) > activeArenaRadius)
            {
                continue;
            }

            sum += tile.transform.position;
            count++;
        }

        if (count > 0)
        {
            tileClusterPosition = sum / count;
            hasTileCluster = true;
            warnedMissingTileSource = false;
            return;
        }

        // StageNodeTrigger configures TileSpawner.arenaCenter for the active node.
        // It is the canonical fallback when no replacement WorldTiles are alive yet.
        if (activeArenaCenter != null)
        {
            tileClusterPosition = activeArenaCenter.position;
            hasTileCluster = true;
            warnedMissingTileSource = false;
            return;
        }

        hasTileCluster = false;
        if (!warnedMissingTileSource)
        {
            warnedMissingTileSource = true;
            Debug.LogWarning("[DesertArcher] No world-tile cluster or TileSpawner arena center is available yet.", this);
        }
    }

    private void UpdateAimAssist()
    {
        if (player == null || !hasTileCluster)
        {
            aimAssistWeight = minimumAimAssistWeight;
            return;
        }

        float distanceToTiles = HorizontalDistance(player.position, tileClusterPosition);
        float start = Mathf.Max(tileLockRadius + 0.01f, aimAssistStartRadius);
        float lockRadius = Mathf.Min(tileLockRadius, start - 0.01f);
        float proximity = 1f - Mathf.InverseLerp(lockRadius, start, distanceToTiles);
        aimAssistWeight = Mathf.Lerp(minimumAimAssistWeight, 1f, proximity);
    }

    private void UpdateCoverPressure()
    {
        if (!enableAntiCoverPressure || player == null)
        {
            playerLineOfSightClear = true;
            coverBlockedSince = -1f;
            coverPressureActive = false;
            return;
        }

        // Do not let a pause menu secretly advance the anti-cover timer. Outside
        // of a pause, use unscaled time so the behavior remains deterministic even
        // if combat briefly changes timeScale for hit-stop or other effects.
        if (Time.timeScale <= 0f)
            return;

        float now = Time.unscaledTime;
        if (now < nextCoverSightCheckTime)
            return;

        nextCoverSightCheckTime = now + coverSightCheckInterval;
        bool clear = HasClearShotToPlayer();

        if (clear)
        {
            bool wasPressuringCover = coverPressureActive;
            playerLineOfSightClear = true;
            coverBlockedSince = -1f;
            coverPressureActive = false;

            if (wasPressuringCover)
            {
                // Once the Archer successfully flushes the player out from behind
                // cover, queue a Charged Rune instead of returning to the ordinary
                // Quick x3 opener. This makes repeated corner camping actively worse
                // than fighting him in the open, while still using an existing,
                // clearly telegraphed attack state.
                coverPunishPending = true;
                Log("Anti-cover: firing angle reacquired. Charged Rune punish queued.");
            }

            return;
        }

        if (playerLineOfSightClear || coverBlockedSince < 0f)
            coverBlockedSince = now;

        playerLineOfSightClear = false;

        if (coverPressureActive
            || now < nextCoverPunishAllowedTime
            || now - coverBlockedSince < coverGraceTime)
        {
            return;
        }

        coverPressureActive = true;
        coverPunishPending = true;
        nextKiteRepathTime = Time.time;
        Log($"Anti-cover: player hidden for {now - coverBlockedSince:0.00}s. Flank pressure engaged.");
    }

    private bool TryStartAntiCoverPunish()
    {
        if (!enableAntiCoverPressure
            || !coverPunishPending
            || !playerLineOfSightClear
            || state != CombatState.SoftAim
            || player == null
            || Time.unscaledTime < nextCoverPunishAllowedTime)
        {
            return false;
        }

        if (Vector3.Distance(transform.position, player.position) > maximumAttackDistance)
            return false;

        coverPunishPending = false;
        coverPressureActive = false;
        coverBlockedSince = -1f;
        nextCoverPunishAllowedTime = Time.unscaledTime + coverPunishCooldown;
        antiCoverPunishesStartedTotal++;

        BeginChargedRuneShot();
        Log($"Anti-cover punish #{antiCoverPunishesStartedTotal}: skipped Quick x3 and opened with Charged Rune.");
        return true;
    }

    private bool HasClearShotToPlayer()
    {
        Vector3 origin = arrowReleaseSpawn != null
            ? arrowReleaseSpawn.position
            : transform.position + Vector3.up * Mathf.Max(0.5f, playerAimHeight);

        return HasClearShotFrom(origin);
    }

    private bool HasClearShotFrom(Vector3 origin)
    {
        if (player == null)
            return false;

        Vector3 targetPosition = GetPlayerAimPoint();
        Vector3 segment = targetPosition - origin;
        float distance = segment.magnitude;
        if (distance <= 0.05f)
            return true;

        Vector3 direction = segment / distance;
        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            direction,
            distance,
            Physics.AllLayers,
            QueryTriggerInteraction.Ignore);

        Collider nearestCollider = null;
        float nearestDistance = float.PositiveInfinity;
        for (int i = 0; i < hits.Length; i++)
        {
            Collider candidate = hits[i].collider;
            if (candidate == null || IsOwnerColliderForSight(candidate))
                continue;

            float candidateDistance = hits[i].distance;
            if (candidateDistance >= nearestDistance)
                continue;

            nearestDistance = candidateDistance;
            nearestCollider = candidate;
        }

        // Nothing solid between the bow and the aim point is a clear shot. If the
        // nearest collider belongs to Van, that is also a clear shot.
        return nearestCollider == null || IsPlayerColliderForSight(nearestCollider);
    }

    private bool IsOwnerColliderForSight(Collider candidate)
    {
        if (candidate == null)
            return false;

        Transform candidateTransform = candidate.transform;
        return candidateTransform == transform
            || candidateTransform.IsChildOf(transform);
    }

    private bool IsPlayerColliderForSight(Collider candidate)
    {
        if (candidate == null || player == null)
            return false;

        Transform candidateTransform = candidate.transform;
        if (candidateTransform == player
            || candidateTransform.IsChildOf(player))
        {
            return true;
        }

        Transform root = candidateTransform.root;
        if (root != null && root.CompareTag("Player"))
            return true;

        return candidate.GetComponentInParent<PlayerHealth>() != null;
    }

    private bool CanAcquireHardLock()
    {
        if (!battleStarted || state != CombatState.SoftAim || player == null || Time.time < nextComboAllowedTime)
            return false;

        // Do not waste the normal three-arrow burst into solid geometry. Sustained
        // occlusion is handled by the anti-cover flank/punish path instead.
        if (enableAntiCoverPressure && !playerLineOfSightClear)
            return false;

        // Quick Shot is an upper-body attack by design. Do not begin the burst
        // until the lower body is actually moving in one of the authored aim-walk
        // states; this prevents the enemy from becoming a stationary turret.
        if (!IsActivelyKiting())
            return false;

        return Vector3.Distance(transform.position, player.position) <= maximumAttackDistance;
    }

    private bool IsActivelyKiting()
    {
        if (navAgent == null || !navAgent.enabled || !navAgent.isOnNavMesh || navAgent.isStopped)
            return false;

        Vector3 velocity = navAgent.velocity;
        velocity.y = 0f;
        if (velocity.sqrMagnitude <= 0.01f)
            return false;

        return currentLocomotionState == aimWalkLeftStateName
            || currentLocomotionState == aimWalkRightStateName
            || currentLocomotionState == aimWalkBackStateName;
    }

    private void UpdateAimTarget()
    {
        if (aimTarget == null || player == null)
            return;

        Vector3 targetPosition = GetPlayerAimPoint();
        bool rigTrackingState = state == CombatState.SoftAim
            || state == CombatState.QuickShot
            || state == CombatState.PiercingRuneShot;
        if (enableUpperBodyAimRig && rigTrackingState)
        {
            aimTarget.position = targetPosition;
            return;
        }

        float followRate = Mathf.Lerp(4f, 18f, aimAssistWeight);
        float t = 1f - Mathf.Exp(-followRate * Time.deltaTime);
        aimTarget.position = Vector3.Lerp(aimTarget.position, targetPosition, t);
    }

    private void UpdateFacing()
    {
        if (player == null
            || state == CombatState.Rolling)
            return;

        Vector3 flatDirection = player.position - transform.position;
        flatDirection.y = 0f;
        if (flatDirection.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(flatDirection.normalized, Vector3.up);
        float speed = Mathf.Lerp(softTurnSpeed, hardTurnSpeed, aimAssistWeight);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, speed * Time.deltaTime);
    }

    private void UpdateDefensiveLocomotion()
    {
        if (!enableDefensiveLocomotion || navAgent == null || !navAgent.enabled || !navAgent.isOnNavMesh || !hasTileCluster)
            return;

        float movementSpeed = locomotionSpeed;
        if (state == CombatState.ChargedRuneShot
            || state == CombatState.PiercingRuneShot)
        {
            movementSpeed *= sniperShotMoveSpeedMultiplier;
        }
        else if (coverPressureActive && state == CombatState.SoftAim)
        {
            movementSpeed *= coverFlankSpeedMultiplier;
        }
        EnemyNavMovement.ApplySpeed(navAgent, movementSpeed, accelerationTime);

        if (player == null)
        {
            Vector3 fallback = tileClusterPosition + transform.forward * defenseAnchorRadius;
            EnemyNavMovement.TrySetDestination(navAgent, fallback);
            return;
        }

        if (Time.time < nextKiteRepathTime && navAgent.hasPath && !navAgent.isStopped)
            return;

        nextKiteRepathTime = Time.time
            + (coverPressureActive ? coverFlankRepathInterval : kiteRepathInterval);

        if (coverPressureActive && TrySetCoverBreakDestination())
            return;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude <= 0.001f)
            toPlayer = transform.forward;
        toPlayer.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, toPlayer).normalized;
        Vector3 back = -toPlayer;
        float playerDistance = HorizontalDistance(transform.position, player.position);

        if (Time.time >= nextKiteDirectionSwapTime)
        {
            kiteStrafeSide *= -1;
            nextKiteDirectionSwapTime = Time.time + kiteDirectionHoldTime;
        }

        bool retreat = playerDistance < kiteRetreatDistance;
        Vector3 preferred = retreat ? back : right * kiteStrafeSide;
        Vector3 alternateStrafe = right * -kiteStrafeSide;

        if (TrySetKiteDestination(preferred, kiteStepDistance))
            return;
        if (TrySetKiteDestination(alternateStrafe, kiteStepDistance))
            return;
        if (!retreat && TrySetKiteDestination(back, kiteStepDistance))
            return;

        // If a full kite step is blocked, immediately try shorter steps instead
        // of dropping to Idle. Continuous movement is this enemy's signature.
        float shortStep = Mathf.Max(1f, kiteStepDistance * 0.5f);
        if (TrySetKiteDestination(preferred, shortStep))
            return;
        if (TrySetKiteDestination(alternateStrafe, shortStep))
            return;
        if (TrySetKiteDestination(back, shortStep))
            return;

        // Last fallback: orbit tangentially around the defended tile cluster.
        // This keeps the Archer moving even when the player-relative strafe
        // directions happen to point into an invalid NavMesh edge.
        Vector3 radial = transform.position - tileClusterPosition;
        radial.y = 0f;
        if (radial.sqrMagnitude > 0.001f)
        {
            Vector3 orbit = Vector3.Cross(Vector3.up, radial.normalized) * kiteStrafeSide;
            if (TrySetKiteDestination(orbit, shortStep)
                || TrySetKiteDestination(-orbit, shortStep))
            {
                return;
            }
        }

        // Broad local sweep as a final movement-preserving fallback. This is
        // intentionally player-agnostic: if the preferred kite vectors land on
        // a NavMesh seam, take any nearby valid lateral-ish step rather than
        // standing still during a Quick Shot.
        Vector3 sweepBasis = right.sqrMagnitude > 0.001f ? right : transform.right;
        for (int angle = 45; angle < 360; angle += 45)
        {
            Vector3 sweepDirection = Quaternion.AngleAxis(angle * kiteStrafeSide, Vector3.up) * sweepBasis;
            if (TrySetKiteDestination(sweepDirection, shortStep))
                return;
        }

        // True no-path case only. Don't manufacture sliding through invalid
        // geometry; log it so a NavMesh/layout problem is visible immediately.
        navAgent.isStopped = true;
        Log("Kite movement exhausted all NavMesh directions; movement paused until next repath.");
    }

    private bool TrySetCoverBreakDestination()
    {
        if (player == null || navAgent == null || !navAgent.enabled || !navAgent.isOnNavMesh || !hasTileCluster)
            return false;

        Vector3 fromPlayer = transform.position - player.position;
        fromPlayer.y = 0f;
        if (fromPlayer.sqrMagnitude <= 0.001f)
            fromPlayer = -transform.forward;
        fromPlayer.Normalize();

        float currentDistance = HorizontalDistance(transform.position, player.position);
        float minimumRadius = Mathf.Max(kiteRetreatDistance + 0.75f, 4f);
        float maximumRadius = Mathf.Max(minimumRadius, coverFlankRadius);
        float orbitRadius = Mathf.Clamp(currentDistance, minimumRadius, maximumRadius);
        float leashRadius = Mathf.Max(kiteLeashRadius, defenseAnchorRadius + kiteStepDistance);
        int side = kiteStrafeSide >= 0 ? 1 : -1;

        // Search both sides of the player's cover, favoring the current strafe
        // direction first. These are destination probes only, not teleports. The
        // NavMeshAgent still has to calculate and travel a valid route.
        float[] angles = { 55f, -55f, 90f, -90f, 125f, -125f, 180f };
        for (int i = 0; i < angles.Length; i++)
        {
            float angle = angles[i] * side;
            Vector3 radial = Quaternion.AngleAxis(angle, Vector3.up) * fromPlayer;
            Vector3 requested = player.position + radial * orbitRadius;

            if (!NavMesh.SamplePosition(requested, out NavMeshHit hit, 2.5f, navAgent.areaMask))
                continue;
            if (HorizontalDistance(hit.position, tileClusterPosition) > leashRadius)
                continue;
            if (HorizontalDistance(hit.position, player.position) < minimumRadius)
                continue;

            float releaseHeight = arrowReleaseSpawn != null
                ? Mathf.Max(0.5f, arrowReleaseSpawn.position.y - transform.position.y)
                : Mathf.Max(0.5f, playerAimHeight);
            Vector3 candidateShotOrigin = hit.position + Vector3.up * releaseHeight;
            if (!HasClearShotFrom(candidateShotOrigin))
                continue;

            navAgent.isStopped = false;
            navAgent.stoppingDistance = kiteStoppingDistance;
            if (!EnemyNavMovement.TrySetDestination(navAgent, hit.position))
                continue;

            return true;
        }

        // If no immediately-exposed firing position was found, keep sweeping
        // laterally around the obstacle rather than standing still and feeding
        // arrows into the wall. Repeated repaths will eventually reach a corner.
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude <= 0.001f)
            toPlayer = transform.forward;
        toPlayer.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, toPlayer).normalized;
        float flankStep = Mathf.Max(kiteStepDistance, 5f);
        if (TrySetKiteDestination(right * side, flankStep)
            || TrySetKiteDestination(right * -side, flankStep))
        {
            return true;
        }

        return false;
    }

    private bool TrySetKiteDestination(Vector3 direction, float stepDistance)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
            return false;

        direction.Normalize();
        stepDistance = Mathf.Max(0.5f, stepDistance);
        Vector3 requested = transform.position + direction * stepDistance;
        float leashRadius = Mathf.Max(kiteLeashRadius, defenseAnchorRadius + kiteStepDistance);

        if (!NavMesh.SamplePosition(requested, out NavMeshHit hit, 1.5f, navAgent.areaMask))
            return false;
        if (HorizontalDistance(hit.position, tileClusterPosition) > leashRadius)
            return false;
        if (HorizontalDistance(hit.position, transform.position) < 0.75f)
            return false;

        navAgent.isStopped = false;
        navAgent.stoppingDistance = kiteStoppingDistance;
        return EnemyNavMovement.TrySetDestination(navAgent, hit.position);
    }

    private void ChooseLateralRollDirection()
    {
        Vector3 toPlayer = player != null ? player.position - transform.position : transform.forward;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.001f)
            toPlayer = transform.forward;
        toPlayer.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, toPlayer).normalized;
        int requestedSide = preferredRollSide >= 0 ? 1 : -1;

        if (TryResolveRollSide(right, requestedSide, out Vector3 resolvedDirection))
        {
            lastRollSide = requestedSide;
            rollDirection = resolvedDirection;
        }
        else if (TryResolveRollSide(right, -requestedSide, out resolvedDirection))
        {
            lastRollSide = -requestedSide;
            rollDirection = resolvedDirection;
        }
        else
        {
            lastRollSide = requestedSide;
            rollDirection = right * requestedSide;
            Log("No full-distance lateral NavMesh endpoint was found; using constrained lateral roll fallback.");
        }

        rollDirection.y = 0f;
        if (rollDirection.sqrMagnitude < 0.001f)
            rollDirection = right * requestedSide;
        rollDirection.Normalize();

        lastRollLateralDot = Mathf.Abs(Vector3.Dot(rollDirection, toPlayer));
        preferredRollSide = -lastRollSide;
        kiteStrafeSide = lastRollSide;
    }

    private bool TryResolveRollSide(Vector3 right, int side, out Vector3 direction)
    {
        direction = right * (side >= 0 ? 1f : -1f);
        if (navAgent == null || !navAgent.enabled || !navAgent.isOnNavMesh)
            return true;

        Vector3 requested = transform.position + direction * Mathf.Max(0.25f, rollDistance);
        if (!NavMesh.SamplePosition(requested, out NavMeshHit hit, rollNavMeshProbeRadius, navAgent.areaMask))
            return false;

        float leashRadius = Mathf.Max(kiteLeashRadius, defenseAnchorRadius + rollDistance + 1f);
        if (hasTileCluster && HorizontalDistance(hit.position, tileClusterPosition) > leashRadius)
            return false;

        Vector3 resolved = hit.position - transform.position;
        resolved.y = 0f;
        if (resolved.sqrMagnitude < 0.25f)
            return false;

        resolved.Normalize();
        // Do not accept a sampled endpoint that turned a side tumble into a
        // forward/backward dodge. The roll is intentionally Vayne-style lateral.
        Vector3 forwardToPlayer = player != null ? player.position - transform.position : transform.forward;
        forwardToPlayer.y = 0f;
        if (forwardToPlayer.sqrMagnitude > 0.001f)
        {
            forwardToPlayer.Normalize();
            if (Mathf.Abs(Vector3.Dot(resolved, forwardToPlayer)) > 0.35f)
                return false;
        }

        direction = resolved;
        return true;
    }

    private void UpdateLocomotionAnimation()
    {
        if (animator == null || navAgent == null || !navAgent.enabled || !navAgent.isOnNavMesh)
            return;

        Vector3 velocity = navAgent.velocity;
        velocity.y = 0f;
        if (velocity.sqrMagnitude < 0.04f)
        {
            SetLocomotionState(idleStateName);
            return;
        }

        Vector3 local = transform.InverseTransformDirection(velocity.normalized);
        if (Mathf.Abs(local.x) > Mathf.Abs(local.z))
            SetLocomotionState(local.x < 0f ? aimWalkLeftStateName : aimWalkRightStateName);
        else if (local.z < 0f)
            SetLocomotionState(aimWalkBackStateName);
        else
        {
            // No forward aim-walk exists. Keep a strafe walk during short forward
            // NavMesh path corners so a moving Archer never visually slides in Idle.
            if (currentLocomotionState == aimWalkLeftStateName
                || currentLocomotionState == aimWalkRightStateName)
            {
                SetLocomotionState(currentLocomotionState);
            }
            else
            {
                SetLocomotionState(kiteStrafeSide < 0 ? aimWalkLeftStateName : aimWalkRightStateName);
            }
        }
    }

    private void UpdateLowerBodyLocomotionAnimation()
    {
        if (animator == null
            || lowerBodyLocomotionLayerIndex < 0
            || lowerBodyLocomotionLayerIndex >= animator.layerCount
            || navAgent == null
            || !navAgent.enabled
            || !navAgent.isOnNavMesh)
        {
            return;
        }

        Vector3 velocity = navAgent.velocity;
        velocity.y = 0f;
        if (velocity.sqrMagnitude < 0.04f)
        {
            SetLowerBodyLocomotionState(idleStateName);
            return;
        }

        Vector3 local = transform.InverseTransformDirection(velocity.normalized);
        if (Mathf.Abs(local.x) > Mathf.Abs(local.z))
        {
            SetLowerBodyLocomotionState(local.x < 0f
                ? aimWalkLeftStateName
                : aimWalkRightStateName);
        }
        else if (local.z < 0f)
        {
            SetLowerBodyLocomotionState(aimWalkBackStateName);
        }
        else
        {
            SetLowerBodyLocomotionState(kiteStrafeSide < 0
                ? aimWalkLeftStateName
                : aimWalkRightStateName);
        }
    }

    private void SetLowerBodyLocomotionState(string stateName)
    {
        if (string.IsNullOrWhiteSpace(stateName)
            || currentLowerBodyLocomotionState == stateName
            || lowerBodyLocomotionLayerIndex < 0)
        {
            return;
        }

        if (!HasAnimatorState(stateName, lowerBodyLocomotionLayerIndex))
        {
            Debug.LogWarning(
                $"[DesertArcher] Lower-body locomotion state '{stateName}' is missing from layer '{lowerBodyLocomotionLayerName}'.",
                this);
            return;
        }

        currentLowerBodyLocomotionState = stateName;
        RestartAnimatorState(stateName, lowerBodyLocomotionLayerIndex);
    }

    private void SetLocomotionState(string stateName)
    {
        if (string.IsNullOrWhiteSpace(stateName) || currentLocomotionState == stateName)
            return;

        currentLocomotionState = stateName;
        if (stateName == aimWalkBackStateName)
            aimWalkBackUsedTotal++;
        else if (stateName == aimWalkLeftStateName)
            aimWalkLeftUsedTotal++;
        else if (stateName == aimWalkRightStateName)
            aimWalkRightUsedTotal++;

        if (stateName == aimWalkLeftStateName)
            SetAnimatorLocomotion(-1f, 0f);
        else if (stateName == aimWalkRightStateName)
            SetAnimatorLocomotion(1f, 0f);
        else if (stateName == aimWalkBackStateName)
            SetAnimatorLocomotion(0f, -1f);
        else
            SetAnimatorLocomotion(0f, 0f);
    }

    private void UpdateRollMovement()
    {
        if (state != CombatState.Rolling || rollDistance <= 0f || rollSpeed <= 0f || rollTravelled >= rollDistance)
            return;

        float step = Mathf.Min(rollSpeed * Time.deltaTime, rollDistance - rollTravelled);
        Vector3 delta = rollDirection * step;

        if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
            navAgent.Move(delta);
        else
            transform.position += delta;

        rollTravelled += step;
    }

    private void UpdateBowDraw()
    {
        if (bowDrawDriver == null)
            return;

        float elapsed = Time.time - stateStartedTime;
        float draw01 = 0f;

        if (state == CombatState.QuickShot || state == CombatState.PiercingRuneShot)
        {
            if (!releaseHandled)
                draw01 = Mathf.Clamp01(elapsed / Mathf.Max(0.05f, quickReleaseFallbackTime));
        }
        else if (state == CombatState.ChargedRuneShot && !releaseHandled)
        {
            const float normalDrawComplete = 31f / 30f;
            const float fullOverdrawReached = 145f / 30f;

            if (elapsed <= normalDrawComplete)
            {
                draw01 = Mathf.Lerp(0f, 0.68f, Mathf.InverseLerp(0f, normalDrawComplete, elapsed));
            }
            else
            {
                draw01 = Mathf.Lerp(
                    0.68f,
                    1f,
                    Mathf.InverseLerp(normalDrawComplete, fullOverdrawReached, elapsed));
            }
        }

        bowDrawDriver.SetDraw01(draw01);
    }

    private void UpdateAnimationEventFallbacks()
    {
        float elapsed = Time.time - stateStartedTime;

        if (state == CombatState.QuickShot || state == CombatState.PiercingRuneShot)
        {
            if (!drawEndHandled && elapsed >= quickEndFallbackTime)
            {
                Log("Animation Event fallback fired for draw end.");
                HandleDrawEnded();
            }
        }
        else if (state == CombatState.ChargedRuneShot)
        {
            if (!drawEndHandled && elapsed >= chargedEndFallbackTime)
            {
                Log("Animation Event fallback fired for Charged Rune draw end.");
                HandleDrawEnded();
            }
        }
        else if (state == CombatState.Rolling && !rollEndHandled && elapsed >= rollEndFallbackTime)
        {
            Log("Animation Event fallback fired for Roll end.");
            HandleRollEnded();
        }
    }

    private void StopAgentForAttack()
    {
        if (navAgent == null || !navAgent.enabled || !navAgent.isOnNavMesh)
            return;

        navAgent.isStopped = true;
        navAgent.ResetPath();
    }

    private void ResumeAgentForDefense()
    {
        if (navAgent == null || !navAgent.enabled || !navAgent.isOnNavMesh)
            return;

        navAgent.isStopped = false;
    }

    private void SetHeldArrowVisible(bool visible)
    {
        if (heldArrowVisual != null && heldArrowVisual.gameObject.activeSelf != visible)
            heldArrowVisual.gameObject.SetActive(visible);
    }

    private Vector3 GetPlayerAimPoint()
    {
        if (player == null)
            return transform.position + transform.forward * 10f;

        PlayerHealth health = player.GetComponentInChildren<PlayerHealth>();
        Collider collider = health != null ? health.GetComponentInChildren<Collider>() : player.GetComponentInChildren<Collider>();
        if (collider != null)
            return collider.bounds.center;

        return player.position + Vector3.up * playerAimHeight;
    }

    private void ResolvePlayer()
    {
        if (player != null && player.gameObject.activeInHierarchy)
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        player = playerObject != null ? playerObject.transform : null;
    }

    private void AutoResolveWeaponReferences()
    {
        if (animator != null)
        {
            Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (heldArrowSocket == null && rightHand != null)
                heldArrowSocket = FindChildRecursive(rightHand, "HeldArrowSocket_R");
        }

        heldBow ??= FindChildRecursive(transform, "S3N2_HeldBow");
        heldArrowVisual ??= FindChildRecursive(transform, "S3N2_HeldArrow");
        arrowReleaseSpawn ??= FindChildRecursive(transform, "ArrowReleaseSpawn");
        aimTarget ??= FindChildRecursive(transform, "AimTarget");

        if (arrowReleaseSpawn == null && heldBow != null)
            arrowReleaseSpawn = FindChildRecursive(heldBow, "ArrowRest");

        if (bowDrawDriver != null && heldBow != null)
            bowDrawDriver.BindBowRoot(heldBow);
    }

    private static Transform FindChildRecursive(Transform root, string exactName)
    {
        if (root == null || string.IsNullOrEmpty(exactName))
            return null;

        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == exactName)
                return transforms[i];
        }
        return null;
    }

    private bool HasAnimatorState(string stateName)
    {
        return HasAnimatorState(stateName, 0);
    }

    private bool HasAnimatorState(string stateName, int layerIndex)
    {
        if (animator == null || string.IsNullOrWhiteSpace(stateName) || layerIndex < 0 || layerIndex >= animator.layerCount)
            return false;

        return animator.HasState(layerIndex, Animator.StringToHash(stateName));
    }

    private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType type)
    {
        if (animator == null || string.IsNullOrWhiteSpace(parameterName))
            return false;

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == parameterName && parameters[i].type == type)
                return true;
        }

        return false;
    }

    private void ResetAnimatorCombatParameters()
    {
        if (animator == null)
            return;

        ResetAnimatorTriggers();
        SetAnimatorBool(BattleActiveParameter, false);
        SetAnimatorBool(FullBodyActionParameter, false);
        SetAnimatorLocomotion(0f, 0f);
    }

    private void ResetAnimatorTriggers()
    {
        if (animator == null)
            return;

        if (HasAnimatorParameter(QuickShotTriggerParameter, AnimatorControllerParameterType.Trigger))
            animator.ResetTrigger(QuickShotTriggerParameter);
        if (HasAnimatorParameter(RollTriggerParameter, AnimatorControllerParameterType.Trigger))
            animator.ResetTrigger(RollTriggerParameter);
        if (HasAnimatorParameter(ChargedRuneTriggerParameter, AnimatorControllerParameterType.Trigger))
            animator.ResetTrigger(ChargedRuneTriggerParameter);
    }

    private void SetAnimatorBool(string parameterName, bool value)
    {
        if (HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Bool))
            animator.SetBool(parameterName, value);
    }

    private bool SetAnimatorTrigger(string parameterName)
    {
        if (!HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Trigger))
            return false;

        animator.ResetTrigger(parameterName);
        animator.SetTrigger(parameterName);
        return true;
    }

    private void SetAnimatorLocomotion(float moveX, float moveZ)
    {
        if (animator == null)
            return;

        if (HasAnimatorParameter(MoveXParameter, AnimatorControllerParameterType.Float))
            animator.SetFloat(MoveXParameter, moveX);
        if (HasAnimatorParameter(MoveZParameter, AnimatorControllerParameterType.Float))
            animator.SetFloat(MoveZParameter, moveZ);
    }

    private void PlayAnimatorState(string stateName, bool crossFade)
    {
        if (!HasAnimatorState(stateName))
            return;

        if (crossFade && animatorTransitionSeconds > 0f)
            animator.CrossFadeInFixedTime(stateName, animatorTransitionSeconds, 0, 0f);
        else
            animator.Play(stateName, 0, 0f);
    }

    private void RestartAnimatorState(string stateName)
    {
        RestartAnimatorState(stateName, 0);
    }

    private void RestartAnimatorState(string stateName, int layerIndex)
    {
        if (!HasAnimatorState(stateName, layerIndex))
        {
            Debug.LogError($"[DesertArcher] Animator state '{stateName}' is missing from layer {layerIndex}.", this);
            return;
        }

        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(layerIndex);
        if (current.IsName(stateName))
            animator.Play(stateName, layerIndex, 0f);
        else
            animator.CrossFadeInFixedTime(stateName, animatorTransitionSeconds, layerIndex, 0f);
    }

    private void ResolveUpperBodyShotLayer()
    {
        upperBodyShotLayerIndex = animator != null && !string.IsNullOrWhiteSpace(upperBodyShotLayerName)
            ? animator.GetLayerIndex(upperBodyShotLayerName)
            : -1;

        if (animator != null && upperBodyShotLayerIndex < 0)
            Debug.LogWarning($"[DesertArcher] Animator layer '{upperBodyShotLayerName}' is missing. Quick/Piercing shots will fall back to Base Layer.", this);
    }

    private void ResolveLowerBodyLocomotionLayer()
    {
        lowerBodyLocomotionLayerIndex = animator != null && !string.IsNullOrWhiteSpace(lowerBodyLocomotionLayerName)
            ? animator.GetLayerIndex(lowerBodyLocomotionLayerName)
            : -1;

        if (animator != null && lowerBodyLocomotionLayerIndex < 0)
        {
            Debug.LogWarning(
                $"[DesertArcher] Animator layer '{lowerBodyLocomotionLayerName}' is missing. Charged/Piercing will move without leg animation.",
                this);
        }
    }

    private void ResolveAimHitReactionLayer()
    {
        aimHitReactionLayerIndex = animator != null && !string.IsNullOrWhiteSpace(aimHitReactionLayerName)
            ? animator.GetLayerIndex(aimHitReactionLayerName)
            : -1;

        if (animator != null && aimHitReactionLayerIndex < 0 && !warnedMissingAimHitReaction)
        {
            warnedMissingAimHitReaction = true;
            Debug.LogWarning(
                $"[DesertArcher] Animator layer '{aimHitReactionLayerName}' is missing. Damage will still apply, but the aiming hit reaction cannot play.",
                this);
        }
    }

    private void SetUpperBodyShotLayerWeight(float weight)
    {
        if (animator == null || upperBodyShotLayerIndex < 0 || upperBodyShotLayerIndex >= animator.layerCount)
            return;

        animator.SetLayerWeight(upperBodyShotLayerIndex, Mathf.Clamp01(weight));
    }

    private void SetLowerBodyLocomotionLayerWeight(float weight)
    {
        if (animator == null
            || lowerBodyLocomotionLayerIndex < 0
            || lowerBodyLocomotionLayerIndex >= animator.layerCount)
        {
            return;
        }

        animator.SetLayerWeight(lowerBodyLocomotionLayerIndex, Mathf.Clamp01(weight));
    }

    private void SetAimHitReactionLayerWeight(float weight)
    {
        if (animator == null
            || aimHitReactionLayerIndex < 0
            || aimHitReactionLayerIndex >= animator.layerCount)
        {
            return;
        }

        animator.SetLayerWeight(aimHitReactionLayerIndex, Mathf.Clamp01(weight));
    }

    private void PlayAimHitReaction()
    {
        if (animator == null)
            return;

        if (aimHitReactionLayerIndex < 0 || aimHitReactionLayerIndex >= animator.layerCount)
        {
            ResolveAimHitReactionLayer();
            if (aimHitReactionLayerIndex < 0)
                return;
        }

        if (!HasAnimatorState(aimHitReactionStateName, aimHitReactionLayerIndex))
        {
            if (!warnedMissingAimHitReaction)
            {
                warnedMissingAimHitReaction = true;
                Debug.LogWarning(
                    $"[DesertArcher] Animator state '{aimHitReactionStateName}' is missing from layer '{aimHitReactionLayerName}'.",
                    this);
            }
            return;
        }

        warnedMissingAimHitReaction = false;
        aimHitReactionActive = true;
        aimHitReactionStartedFrame = Time.frameCount;
        SetAimHitReactionLayerWeight(aimHitReactionWeight);

        // Restart from the additive reference pose on every hit. Because the mask
        // excludes arms, fingers, hips and legs, this only adds the authored
        // chest/head recoil to whatever bow shot is already playing underneath.
        animator.Play(aimHitReactionStateName, aimHitReactionLayerIndex, 0f);
    }

    private void UpdateAimHitReactionLayer()
    {
        if (!aimHitReactionActive
            || animator == null
            || aimHitReactionLayerIndex < 0
            || aimHitReactionLayerIndex >= animator.layerCount)
        {
            return;
        }

        // Animator.Play takes effect during Animator evaluation, after ordinary
        // MonoBehaviour Update order. On a repeated hit the layer may still report
        // the previous, already-finished reaction for the rest of this frame. Give
        // the Animator one evaluation frame before judging normalizedTime so a new
        // hit cannot be immediately cancelled by stale state info.
        if (Time.frameCount <= aimHitReactionStartedFrame + 1)
            return;

        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(aimHitReactionLayerIndex);
        if (!info.IsName(aimHitReactionStateName))
        {
            StopAimHitReaction();
            return;
        }

        float normalizedTime = info.normalizedTime;
        if (normalizedTime >= 1f)
        {
            StopAimHitReaction();
            return;
        }

        float fadeStart = Mathf.Clamp(aimHitReactionFadeOutStart, 0.1f, 0.99f);
        float maxWeight = Mathf.Clamp01(aimHitReactionWeight);
        float weight = normalizedTime <= fadeStart
            ? maxWeight
            : maxWeight * (1f - Mathf.InverseLerp(fadeStart, 1f, normalizedTime));
        SetAimHitReactionLayerWeight(weight);
    }

    private void StopAimHitReaction()
    {
        aimHitReactionActive = false;
        aimHitReactionStartedFrame = -1;
        SetAimHitReactionLayerWeight(0f);
    }

    private static bool IsShotState(CombatState value)
    {
        return value == CombatState.QuickShot
            || value == CombatState.ChargedRuneShot
            || value == CombatState.PiercingRuneShot;
    }

    private static bool IsUpperBodyShotState(CombatState value)
    {
        return value == CombatState.QuickShot || value == CombatState.PiercingRuneShot;
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void Log(string message)
    {
        if (debugLogs)
            Debug.Log("[DesertArcher] " + message, this);
    }

    private void LogShotMovementCheck()
    {
        bool agentReady = navAgent != null && navAgent.enabled && navAgent.isOnNavMesh;
        float speed = agentReady ? navAgent.velocity.magnitude : 0f;
        float releaseBowAimError = MeasureCurrentBowAimError();
        bool walking = agentReady
            && !navAgent.isStopped
            && speed > 0.1f
            && (currentLocomotionState == aimWalkLeftStateName
                || currentLocomotionState == aimWalkRightStateName
                || currentLocomotionState == aimWalkBackStateName);

        if (activeArrowType == DesertArcherArrowType.Quick)
        {
            maxQuickReleaseBowAimErrorDegrees = Mathf.Max(maxQuickReleaseBowAimErrorDegrees, releaseBowAimError);
            if (walking)
                quickShotsReleasedWhileWalkingTotal++;
            else
                quickShotsReleasedStationaryTotal++;
        }

        if (!debugLogs || !debugShotMovementLogs)
            return;

        string locomotion = string.IsNullOrWhiteSpace(currentLocomotionState)
            ? "<none>"
            : currentLocomotionState;
        string baseAnimatorState = DescribeBaseAnimatorState();
        float upperWeight = animator != null && upperBodyShotLayerIndex >= 0 && upperBodyShotLayerIndex < animator.layerCount
            ? animator.GetLayerWeight(upperBodyShotLayerIndex)
            : 0f;

        Debug.Log(
            $"[DesertArcher][SHOOT-MOVE] arrow={activeArrowType} walking={walking} " +
            $"speed={speed:0.00} navStopped={(agentReady ? navAgent.isStopped : true)} " +
            $"locomotion='{locomotion}' baseAnim='{baseAnimatorState}' upperWeight={upperWeight:0.00} " +
            $"visualFacingError={visualFacingErrorDegrees:0.0}deg bowAimError={releaseBowAimError:0.0}deg",
            this);
    }

    private float MeasureCurrentBowAimError()
    {
        if (bowAimReference == null || player == null)
            return 180f;

        Vector3 toTarget = GetPlayerAimPoint() - bowAimReference.position;
        Vector3 bowForward = bowAimReference.forward;
        if (toTarget.sqrMagnitude <= 0.0001f)
            return 0f;
        if (bowForward.sqrMagnitude <= 0.0001f)
            return 180f;

        return Vector3.Angle(bowForward.normalized, toTarget.normalized);
    }

    private string DescribeBaseAnimatorState()
    {
        if (animator == null || animator.layerCount <= 0)
            return "<no animator>";

        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
        AnimatorStateInfo next = animator.IsInTransition(0)
            ? animator.GetNextAnimatorStateInfo(0)
            : default;

        string[] knownStates =
        {
            aimWalkLeftStateName,
            aimWalkRightStateName,
            aimWalkBackStateName,
            rollStateName,
            chargedRuneStateName,
            idleStateName
        };

        for (int i = 0; i < knownStates.Length; i++)
        {
            string stateName = knownStates[i];
            if (string.IsNullOrWhiteSpace(stateName))
                continue;
            if (current.IsName(stateName) || (animator.IsInTransition(0) && next.IsName(stateName)))
                return stateName;
        }

        return $"hash:{current.shortNameHash}";
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawAimGizmos)
            return;

        Vector3 center = tileClusterCenterOverride != null
            ? tileClusterCenterOverride.position
            : tileClusterPosition;

        if (tileClusterCenterOverride != null || hasTileCluster)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(center, aimAssistStartRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(center, tileLockRadius);
        }

        if (arrowReleaseSpawn != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(arrowReleaseSpawn.position, arrowReleaseSpawn.forward * 2f);
        }

        if (drawRollDistanceGizmo && rollDistance > 0f)
        {
            Vector3 rollRight = transform.right;
            if (player != null)
            {
                Vector3 toPlayer = player.position - transform.position;
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude > 0.001f)
                    rollRight = Vector3.Cross(Vector3.up, toPlayer.normalized).normalized;
            }

            Vector3 leftEnd = transform.position - rollRight * rollDistance;
            Vector3 rightEnd = transform.position + rollRight * rollDistance;
            float markerRadius = Mathf.Clamp(rollDistance * 0.06f, 0.12f, 0.4f);

            Gizmos.color = new Color(1f, 0.45f, 0.1f, 1f);
            Gizmos.DrawLine(transform.position, leftEnd);
            Gizmos.DrawLine(transform.position, rightEnd);
            Gizmos.DrawWireSphere(leftEnd, markerRadius);
            Gizmos.DrawWireSphere(rightEnd, markerRadius);
        }
    }
}
