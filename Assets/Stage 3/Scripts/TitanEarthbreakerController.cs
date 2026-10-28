using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

/// <summary>
/// Titan Earthbreaker initial encounter controller.
/// Fourth riddle -> StartAwakening ONLY; combat cannot begin before
/// AwakeningV004 reaches AwakenedHoldV004. Only approved Walk/Hammerfist
/// animations are used. Earthbreaker Slam V003 is review-gated. Production-only
/// hit overlay and authored V015 death are driven by genuine EnemyHealth events.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public sealed class TitanEarthbreakerController : MonoBehaviour
{
    // Append Slam AFTER Dead to preserve existing serialized enum values.
    // Append V017 after EarthbreakerSlam; older serialized numeric states stay intact.
    public enum TitanState { Dormant, Awakening, AwakenedHold, Chasing, Hammerfist, Dead, EarthbreakerSlam, ColossalStompV017 }

    private static readonly int DormantHash = Animator.StringToHash("Base Layer.DormantStatueV004");
    private static readonly int AwakeningHash = Animator.StringToHash("Base Layer.AwakeningV004");
    private static readonly int ReadyHash = Animator.StringToHash("Base Layer.AwakenedHoldV004");
    private static readonly int HeavyWalkStateHash = Animator.StringToHash("Base Layer.ColossalHeavyWalk");
    private static readonly int WalkingHash = Animator.StringToHash("Walking");
    private static readonly int AwakenHash = Animator.StringToHash("Awaken");
    private static readonly int HammerHash = Animator.StringToHash("Hammerfist");
    private static readonly int HammerStateHash = Animator.StringToHash("Base Layer.HammerfistV003");
    private static readonly int SlamHash = Animator.StringToHash("EarthbreakerSlam");
    private static readonly int SlamStateHash = Animator.StringToHash("Base Layer.EarthbreakerSlamV003");
    private static readonly int DeathStateHash = Animator.StringToHash("Base Layer.TitanDeathFromRightPreview");
    private const string HitLayerName = "Titan Upper Body BeingHit";
    private static readonly int HitLayerStateHash = Animator.StringToHash(HitLayerName + ".BeingHit");
    private static readonly int HitEmptyStateHash = Animator.StringToHash(HitLayerName + ".Empty");

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private EnemyHealth health;
    [SerializeField] private Collider combatCollider;
    [SerializeField] private Collider physicalBodyCollider;
    [SerializeField] private GameObject spellTarget;
    [SerializeField] private PlayerHealth player;
    [SerializeField] private StageNodeController defeatReceiver;
    [SerializeField] private Transform arenaCenter;

    [Header("Production: Golem-style upper-body BeingHit overlay")]
    [Tooltip("Dedicated Titan 65-bone masked additive layer, following the Golem layer pattern. Never interrupts the authored V017 foot solver, Slam impact or Hammerfist event.")]
    [SerializeField] private bool enableBeingHitOverlay;
    [SerializeField, Range(0.1f, 1f)] private float beingHitLayerWeight = 0.65f;
    [SerializeField, Min(0.1f)] private float beingHitReactionSeconds = 0.52f;
    [SerializeField, Min(0.05f)] private float beingHitCooldownSeconds = 0.6f;
    [SerializeField] private int beingHitReactionsStarted;
    [SerializeField] private string beingHitStatus = "Not triggered";

    [Header("Production: authored V015 death and defeat completion")]
    [SerializeField] private bool enableAuthoredDeath;
    [Tooltip("Isolated copy of the V015 3.3s animation with OnDeathFinished event; source V015 clip unchanged.")]
    [SerializeField] private AnimationClip authoredDeathClip;
    [SerializeField, Min(0f)] private float deathBlendSeconds = 0.14f;
    [SerializeField, Min(0.1f)] private float deathFallbackPadding = 0.6f;
    [SerializeField] private bool deathAnimationStarted;
    [SerializeField] private bool deathEventReceived;
    [SerializeField] private string deathStatus = "Alive";

    [Header("Awakening Gate")]
    [Tooltip("Only the final-riddle laser calls StartAwakening. No movement or damage before the animation ends.")]
    [SerializeField] private bool autoBeginCombatAfterAwakening = true;

    [Header("Approved Heavy Walk")]
    [SerializeField, Min(0.05f)] private float walkSpeed = 2.2f;
    [SerializeField, Min(0.1f)] private float acceleration = 5f;
    [SerializeField, Min(0.1f)] private float repathSeconds = 0.3f;
    [SerializeField, Min(0.1f)] private float turnSpeedDegrees = 75f;

    [Header("Approved Hammerfist V003")]
    [SerializeField, Min(0.5f)] private float attackRange = 10f;
    [SerializeField, Min(0.1f)] private float attackCooldown = 5.5f;
    [SerializeField, Range(0f, 180f)] private float attackFacingDegrees = 55f;
    [SerializeField, Min(0f)] private float impactDamage = 15f;
    [SerializeField, Min(0.1f)] private float impactRadius = 7f;
    [SerializeField, Min(0.1f)] private float impactVerticalTolerance = 8f;
    [Tooltip("Optional right-hand bone override. Defaults to the existing mixamorig:RightHand bone; no proxy object needed.")]
    [SerializeField] private Transform hammerfistImpactHand;
    [SerializeField, Min(0.1f)] private float impactGroundProbeLift = 5f;
    [SerializeField, Min(0.5f)] private float impactMaxGroundClearance = 12f;
    [Tooltip("Master gate for real Hammerfist frame134 HP damage only. No impact, covered or jump-avoided contact means no damage.")]
    [SerializeField] private bool enableCombatDamage = false;
    [Header("Hammerfist: THREE concentric physical damage zones")]
    [Tooltip("Use exactly three ground-plane tiers around the REAL right-fist landing, not the Titan root. Requires master Hammerfist HP gate and genuine unblocked collider contact.")]
    [SerializeField] private bool enableHammerfistThreeZoneDamage;
    [Tooltip("Inner strongest ring in WORLD metres from the nearest real player collider point to the grounded fist.")]
    [SerializeField, Min(.1f)] private float hammerInnerRadius = 4f;
    [SerializeField, Min(.1f)] private float hammerMiddleRadius = 9f;
    [Tooltip("Damage is always zero beyond the actual Hammerfist outer impact radius.")]
    [SerializeField, Min(0f)] private float hammerInnerDamage = 12f;
    [SerializeField, Min(0f)] private float hammerMiddleDamage = 8f;
    [SerializeField, Min(0f)] private float hammerOuterDamage = 4f;
    [SerializeField] private int lastHammerfistDamageZone;
    [SerializeField] private float lastHammerfistAppliedDamage;
    [SerializeField] private float lastHammerfistColliderPlanarDistance = -1f;
    [Tooltip("Fallback only for missed end transitions; does NOT synthesize impact damage.")]
    [SerializeField, Min(8.4f)] private float attackWatchdogSeconds = 10f;

    [Header("Hammerfist Contact-Aligned Approach (V009 review only)")]
    [Tooltip("Off for the accepted V008 scene. Uses the sampled V003 fist contact pose to stage Titan at real giant-scale striking distance.")]
    [SerializeField] private bool useContactAlignedApproach = false;
    [Tooltip("Right-hand landing offset in Titan root-local units, sampled from V003 frame 134 at scale 40.01.")]
    [SerializeField] private Vector2 hammerfistContactLocalXZ = new Vector2(0.44f, 0.94f);
    [SerializeField, Min(0.5f)] private float hammerfistRangeTolerance = 8f;
    [SerializeField, Range(1f, 45f)] private float hammerfistAimToleranceDegrees = 12f;

    [Header("Hammerfist: real HIT -> airborne lock -> landing slow")]
    [Tooltip("Opt-in Hammerfist launch after authentic frame134 physical contact only. Voluntary movement remains locked throughout actual flight; slow begins only on landing. Independent of HP damage.")]
    [SerializeField] private bool enableHammerfistCrowdControl = false;
    [Tooltip("Native Van vertical launch velocity in world metres/second, scaled once by Steady Current passive.")]
    [SerializeField, Min(0f)] private float hammerfistLaunchSpeed = 14f;
    [Tooltip("Steady Current-aware minimum airborne lock reference; physical landing, not a fixed stun timer, ends input lock.")]
    [SerializeField, Min(0f)] private float hammerfistStunSeconds = 1.6f;
    [Tooltip("Player voluntary movement multiplier, applied AFTER actual landing.")]
    [SerializeField, Range(0.01f, 1f)] private float hammerfistSlowMultiplier = 0.6f;
    [SerializeField, Min(0f)] private float hammerfistSlowSeconds = 2.5f;
    [Header("Hammerfist HIT -> V017 stomp pursuit")]
    [Tooltip("Only after a real Hammerfist contact AND successful physical launch: prioritize a NavMesh approach to V017 safe stance during landing slow. No guaranteed stomp/hit, no IK or cooldown bypass.")]
    [SerializeField] private bool enableHammerfistStompPursuit;
    [SerializeField] private bool hammerfistStompPursuitPending;
    [SerializeField] private string hammerfistStompPursuitStatus = "None";
    [Tooltip("Maximum time from a REAL frame134 launch to attempt a legal follow-up stance. The 8s landing slow still expires normally; remaining time is AI pursuit, never extra player stun.")]
    [SerializeField, Range(10f, 25f)] private float hammerfistStompPursuitMaxSeconds = 18f;

    [Header("V011 Combat Decision Review (default OFF)")]
    [Tooltip("Contextual Chase / Reposition / Hammerfist chooser. Does not introduce unapproved attacks.")]
    [SerializeField] private bool enableTacticalDecisionReview = false;
    [Tooltip("Extra no-attack window after the full Hammerfist animation ends. Titan still resumes approved Heavy Walk.")]
    [SerializeField, Min(0f)] private float postHammerfistAttackDelay = 2f;
    [Tooltip("The committed fist targets the point established when the attack begins. Editor-only diagnostic.")]
    [SerializeField] private bool drawHammerfistTelegraphGizmo = true;
    [SerializeField] private Vector3 committedHammerfistPoint;

    [Header("V011 Hammerfist AoE Fairness (default OFF)")]
    [SerializeField] private bool respectSolidCover = false;
    [Tooltip("Review-only instant impact ring, not a travelling shockwave or finished visual effect.")]
    [SerializeField] private bool allowJumpAvoidOuterImpact = false;
    [SerializeField, Min(0.1f)] private float directImpactRadius = 6f;
    [SerializeField, Min(0.1f)] private float outerImpactJumpClearance = 1.5f;

    [Header("V013 Two-Handed Earthbreaker Slam (review only)")]
    [Tooltip("OFF on existing V008-V012. Alternates the two approved attack animations using the V012 Animator.")]
    [SerializeField] private bool enableEarthbreakerSlamAI = false;
    [SerializeField, Min(0f)] private float slamRecoverySeconds = 2.5f;
    [SerializeField, Min(8f)] private float slamWatchdogSeconds = 9f;
    [SerializeField, Min(0.1f)] private float slamMinAttackRange = 20f;
    [SerializeField, Min(1f)] private float slamMaxAttackRange = 65f;
    [SerializeField, Range(1f, 180f)] private float slamFacingToleranceDegrees = 65f;
    [Tooltip("Outer capsule radius around the ground segment between BOTH fists. Review balance, not final.")]
    [SerializeField, Min(0.5f)] private float slamImpactRadius = 26f;
    [Tooltip("Direct-fist core radius around EACH ground contact; jumping cannot avoid this core.")]
    [SerializeField, Min(0.1f)] private float slamDirectImpactRadius = 10f;
    [SerializeField, Min(0.1f)] private float slamJumpClearance = 1.8f;
    [SerializeField, Min(0.1f)] private float slamVerticalTolerance = 12f;
    [SerializeField, Min(0.1f)] private float slamFloorProbeLift = 6f;
    [SerializeField, Min(0.5f)] private float slamFloorMaxClearance = 19f;
    [SerializeField, Min(0f)] private float slamMaxGroundHeightDifference = 12f;
    [Tooltip("Only enable after separate AoE damage approval. OFF during V013 tests.")]
    [SerializeField] private bool enableSlamCombatDamage = false;
    [SerializeField, Min(0f)] private float slamDamage = 20f;
    [Tooltip("Separate review-only CC. OFF until the large-slam effect is designed.")]
    [SerializeField] private bool enableSlamCrowdControl = false;
    [SerializeField, Range(0.01f, 1f)] private float slamSlowMultiplier = 0.7f;
    [SerializeField, Min(0f)] private float slamSlowSeconds = 2f;

    [Header("V014 Fixed Concentric Earthquake Zones (default OFF)")]
    [Tooltip("Replace V013 instant two-fist capsule with frame-71 progressive, non-expanding zones. Each zone is resolved once at its own time.")]
    [SerializeField] private bool enableProgressiveEarthbreakerZones = false;
    [SerializeField] private TitanEarthbreakerProgressiveWaves progressiveEarthquake;

    [Header("V017 Four-Right-Foot Colossal Stomp (LBS-certified gameplay hits)")]
    [Tooltip("OFF for legacy scenes. Requires a fully calibrated V017 RigBuilder actor and world-space IK targets.")]
    [SerializeField] private bool enableColossalStompV017 = false;
    [SerializeField] private TitanV017FourRightStompIKPreview colossalStompV017;
    [Tooltip("Read-only, physical-capsule/NavMesh stance gate. Required for V017 combat entry.")]
    [SerializeField] private TitanV017StompStancePreflight colossalStompV017StancePreflight;
    [Tooltip("The same world-space aim marker used by the IK preview. Assign explicitly; never move it to fake foot contact.")]
    [SerializeField] private Transform colossalStompV017AimMarker;
    [Tooltip("When enabled, select the stomp in combat only from a stable Heavy Walk and after awakening.")]
    [SerializeField] private bool autoSelectColossalStompV017 = false;
    [Tooltip("Additional opt-in for NavMesh walking and turning into the planner's safe right-sole stance. OFF until production rig is verified.")]
    [SerializeField] private bool enableColossalStompV017StanceApproach = false;
    [SerializeField, Min(2f)] private float colossalStompV017MaxPlayerRange = 18f;
    [SerializeField, Min(0.05f)] private float colossalStompV017RootStanceTolerance = 0.75f;
    [SerializeField, Range(0.5f, 15f)] private float colossalStompV017FacingToleranceDegrees = 8f;
    [SerializeField, Min(0.05f)] private float colossalStompV017AimMarkerTolerance = 0.75f;
    [SerializeField, Min(0.05f)] private float colossalStompV017PlayerMotionTolerance = 1f;
    [SerializeField, Min(0.05f)] private float colossalStompV017SettleSeconds = 0.4f;
    [SerializeField, Min(0.5f)] private float colossalStompV017StanceApproachTimeout = 16f;
    [Tooltip("During REAL HeavyWalk stance approach only, smoothly ground the giant's first-frame sole by correcting scaled NavMeshAgent baseOffset toward the sampled safe floor. Never change root height after V017 commitment.")]
    [SerializeField] private bool enableV017GroundAlignedWalk;
    [SerializeField, Range(0.1f, 1.5f)] private float v017GroundAlignWorldSpeed = 0.8f;
    [Tooltip("Only for the genuinely slowed Hammerfist->stomp approach. Small sand-floor planting bias (world metres) during HeavyWalk, never a stomp-time root correction. Strict first-frame 0.35m and frame28 / LBS checks still decide.")]
    [SerializeField, Range(-0.3f, 0f)] private float hammerStompGroundingBiasWorld = -0.15f;
    [SerializeField] private string v017GroundAlignmentStatus = "Not requested";
    [SerializeField, Min(8.8f)] private float colossalStompV017WatchdogSeconds = 10.5f;
    [SerializeField, Min(0f)] private float colossalStompV017RecoverySeconds = 9f;

    [Header("V017 four certified right-sole impacts: actual player hit range")]
    [Tooltip("Only a real post-RigBuilder, deformed-mesh VERIFIED right sole impact can cause damage, once per each of the four authored impacts. Never damage from clip timing or aim marker alone.")]
    [SerializeField] private bool enableV017VerifiedStompDamage;
    [Tooltip("World-space radii about actual right-sole floor contact, measured to the nearest point of Van's real CharacterController (not player root).")]
    [SerializeField, Min(.1f)] private float stompCoreRadius = 7f;
    [SerializeField, Min(.1f)] private float stompMiddleRadius = 10f;
    [SerializeField, Min(.1f)] private float stompOuterRadius = 14f;
    [SerializeField, Min(.1f)] private float stompPlayerVerticalTolerance = 2f;
    [SerializeField, Min(0f)] private float stompCoreDamage = 5f;
    [SerializeField, Min(0f)] private float stompMiddleDamage = 3f;
    [SerializeField, Min(0f)] private float stompOuterDamage = 1f;
    [Tooltip("Actual damage budget per whole 4-stomp clip, regardless of repeated events; never exceeds 20 HP with production 5/3/1 tiers.")]
    [SerializeField, Min(0f)] private float stompTotalDamageCap = 20f;
    [SerializeField] private bool showV017StompRangeGizmos = true;
    [SerializeField] private int stompPlayerHitCount;
    [SerializeField] private float stompActualDamageThisAttack;
    [SerializeField] private int stompLastImpactIndex;
    [SerializeField] private int stompLastDamageZone;
    [SerializeField] private float stompLastPlayerColliderDistance = -1f;
    [SerializeField] private Vector3 stompLastCertifiedFootGround;
    [SerializeField] private string stompLastHitStatus = "No player hit evaluated";

    [Header("PRODUCTION: Deterministic Attack Hierarchy (no RNG)")]
    [Tooltip("OFF preserves historical V011/V013 selection in older Titan scenes. ON: <=18m V017 priority, ~18-34m Slam, ~34-50m alternate valid Hammer/Slam, ~50-65m Slam, else HeavyWalk. All world-space planar distances. Safety/animation/cooldown gates remain mandatory.")]
    [SerializeField] private bool enableAttackHierarchy = false;
    [Tooltip("Live chosen band/intent, editor inspection only. Attack eligibility is separate from verified collision/damage.")]
    [SerializeField] private string lastAttackHierarchyDecision = "Awaiting combat.";

    [Header("PRODUCTION: Earthbreaker Anti-Repeat (deterministic)")]
    [Tooltip("If ON together with attack hierarchy, a second Earthbreaker requires a DIFFERENT successfully started attack or real Titan NavMesh reposition. Prevents stationary 25m Slam spam; does not change damage or source animation.")]
    [SerializeField] private bool enableSlamAntiRepeat = false;
    [Tooltip("Horizontal world metres the Titan itself must travel after beginning a Slam. A moving Van, elapsed cooldown, turning, or NavMesh vertical offset does not count.")]
    [SerializeField, Min(2f)] private float slamAntiRepeatMoveMeters = 10f;
    [SerializeField] private bool slamAntiRepeatLocked;
    [SerializeField] private Vector3 slamAntiRepeatOrigin;
    [SerializeField] private string slamAntiRepeatStatus = "Ready";

    [Header("PRODUCTION: Earthbreaker Verified-Hit Follow-Up Gate")]
    [Tooltip("After a Slam begins, Hammerfist/V017 follow-ups remain unavailable until a real frame-71 impact or a progressive Earthquake zone verifies player collider contact without cover/jump avoidance. A miss requires real reposition before reattempt. No HP damage is implied.")]
    [SerializeField] private bool requireSlamHitForFollowupAnimations = false;
    [Tooltip("Read-only live gate: TRUE after a Slam starts or misses, FALSE only when physical impact confirms a hit. Another attack never opens it by timer alone.")]
    [SerializeField] private bool slamFollowupsWaitingForHit;
    [SerializeField] private bool lastSlamVerifiedHit;
    [SerializeField] private int slamVerifiedHits;
    [SerializeField] private string slamHitCheckerStatus = "No Slam yet";

    [Header("PRODUCTION: Verified Slam HIT -> attack or physical approach")]
    [Tooltip("After a genuine Slam zone HIT and FULL authored recovery, prioritize one other attack: valid close V017 stance, else Hammerfist in actual reach, else walk/turn toward Hammerfist range. Never repeat Slam before that other attack starts.")]
    [SerializeField] private bool enableVerifiedSlamRangeFollowup;
    [SerializeField] private bool verifiedSlamFollowupPending;
    [SerializeField] private string verifiedSlamFollowupStatus = "None";

    [Header("Events")]
    [SerializeField] private UnityEvent onAwakeningFinished;
    [SerializeField] private UnityEvent onHammerfistImpact;
    [SerializeField] private UnityEvent onTitanDefeated;

    [Header("Runtime Diagnostics (read-only)")]
    [SerializeField] private TitanState state = TitanState.Dormant;
    [SerializeField] private bool awakened;
    [SerializeField] private bool battleStarted;
    [SerializeField] private bool hammerImpactReceived;
    [SerializeField] private int hammerImpactCount;
    [SerializeField] private int playerHits;
    [SerializeField] private bool lastHammerfistGroundFound;
    [SerializeField] private bool lastHammerfistPlayerContact;
    [SerializeField] private Vector3 lastHammerfistGroundPoint;
    [SerializeField] private float lastHammerfistPlayerPlanarDistance;
    [SerializeField] private bool lastHammerfistBlockedByCover;
    [SerializeField] private bool lastHammerfistJumpAvoided;
    [SerializeField] private int earthbreakerSlamsStarted;
    [SerializeField] private int earthbreakerImpacts;
    [SerializeField] private bool lastEarthbreakerGroundFound;
    [SerializeField] private bool lastEarthbreakerPlayerContact;
    [SerializeField] private bool lastEarthbreakerCoverBlocked;
    [SerializeField] private bool lastEarthbreakerJumpAvoided;
    [SerializeField] private Vector3 lastEarthbreakerGroundPoint;
    [SerializeField] private Vector3 lastEarthbreakerLeftGround;
    [SerializeField] private Vector3 lastEarthbreakerRightGround;
    [SerializeField] private string lastV017StanceRejection = "Not checked.";

    private float nextAttackTime;
    private float nextRepathTime;
    private float hammerStartedAt;
    private bool hammerStateObserved;
    private bool noNavMeshLogged;
    private bool defeatReported;
    private float nextPlayerLookup;
    private float nextHealthBarCheck;
    private float rootOffsetAboveNavMesh;
    private Transform cachedChest;
    private Transform slamLeftHand;
    private Transform slamRightHand;
    private bool lastAttackWasHammerfist;
    private bool slamImpactReceived;
    private bool slamStateObserved;
    private float slamStartedAt;
    private float colossalStompStartedAt;
    private Vector3 colossalStompCommittedAimPosition;
    private int colossalStompsVerified;
    private float nextV017StanceWarningTime;
    private float nextV017StanceAttemptTime;
    private bool v017StanceApproaching;
    private Vector3 v017ApproachRootDestination;
    private Quaternion v017ApproachFacing;
    private Vector3 v017ApproachPlayerPosition;
    private Vector3 v017ApproachLandingGround;
    private float v017ApproachStartedAt;
    private float v017ApproachSettledAt = -1f;
    private float v017PreviousStoppingDistance;
    private float nextSlamAntiRepeatNotice;
    private TitanHammerfistCrowdControl hammerfistStompVictimMotion;
    private float nextHammerfistPursuitNotice;
    private float hammerfistStompPursuitDeadline;
    private int stompLastProcessedSoleIndex;
    private int beingHitLayerIndex = -1;
    private float beingHitStartedTime = -1f;
    private float nextBeingHitTime;
    private bool beingHitOverlayActive;
    private bool warnedMissingBeingHit;
    private bool queuedBeingHit;
    private float deathStartedTime = -1f;

    public TitanState State => state;
    public bool IsAwakened => awakened;
    public bool IsBattleStarted => battleStarted;
    // The visible health bar is gated by the actual end of AwakeningV004,
    // regardless of early StageNodeTrigger encounter/HUD requests.
    public bool IsHealthBarUnlocked => awakened && state != TitanState.Dead
        && !defeatReported && health != null && !health.IsDead;
    public int HammerImpacts => hammerImpactCount;
    public int PlayerHits => playerHits;
    public bool LastHammerfistGroundFound => lastHammerfistGroundFound;
    public bool LastHammerfistPlayerContact => lastHammerfistPlayerContact;
    public Vector3 LastHammerfistGroundPoint => lastHammerfistGroundPoint;
    public bool LastHammerfistBlockedByCover => lastHammerfistBlockedByCover;
    public bool LastHammerfistJumpAvoided => lastHammerfistJumpAvoided;
    public float HammerfistContactReach => transform.TransformVector(
        new Vector3(hammerfistContactLocalXZ.x, 0f, hammerfistContactLocalXZ.y)).magnitude;
    public int EarthbreakerSlamsStarted => earthbreakerSlamsStarted;
    public int EarthbreakerImpacts => earthbreakerImpacts;
    public bool LastEarthbreakerGroundFound => lastEarthbreakerGroundFound;
    public bool LastEarthbreakerPlayerContact => lastEarthbreakerPlayerContact;
    public bool LastEarthbreakerCoverBlocked => lastEarthbreakerCoverBlocked;
    public bool LastEarthbreakerJumpAvoided => lastEarthbreakerJumpAvoided;
    public Vector3 LastEarthbreakerGroundPoint => lastEarthbreakerGroundPoint;
    public Vector3 LastEarthbreakerLeftGround => lastEarthbreakerLeftGround;
    public Vector3 LastEarthbreakerRightGround => lastEarthbreakerRightGround;
    public int ColossalStompsVerified => colossalStompsVerified;
    public string LastV017StanceRejection => lastV017StanceRejection;
    // Homing spells can target the main Titan GameObject and still fly toward
    // the animated chest. No separate Enemy-tagged target object is needed.
    public Vector3 SpellAimPosition
    {
        get
        {
            if (!cachedChest)
            {
                foreach (var bone in GetComponentsInChildren<Transform>(true))
                {
                    if (bone.name.EndsWith("Spine2", StringComparison.OrdinalIgnoreCase))
                    {
                        cachedChest = bone;
                        break;
                    }
                }
            }
            return cachedChest ? cachedChest.position : transform.position + Vector3.up * 18f;
        }
    }

    private void Reset()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<EnemyHealth>();
        combatCollider = GetComponent<Collider>();
    }

    private void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!health) health = GetComponent<EnemyHealth>();
        if (!combatCollider) combatCollider = GetComponent<Collider>();
        if (!colossalStompV017StancePreflight)
            colossalStompV017StancePreflight = GetComponent<TitanV017StompStancePreflight>();
        if (animator == null)
        {
            enabled = false;
            return;
        }
        if (!animator.HasState(0, DormantHash) || !animator.HasState(0, AwakeningHash)
            || !animator.HasState(0, ReadyHash) || !animator.HasState(0, HeavyWalkStateHash)
            || !animator.HasState(0, HammerStateHash))
        {
            Debug.LogError("[Titan] Animator is missing one of the required V004 states. Fail closed.", this);
            enabled = false;
            return;
        }
        if (enableEarthbreakerSlamAI &&
            (!animator.HasState(0, SlamStateHash) ||
             GetComponent<TitanEarthbreakerSlamEventRelay>() == null))
        {
            Debug.LogError("[Titan] V013 Slam enabled without V012 Animator/impact relay. Fail closed.", this);
            enabled = false;
            return;
        }
        if (enableProgressiveEarthbreakerZones)
        {
            if (!progressiveEarthquake)
                progressiveEarthquake = GetComponent<TitanEarthbreakerProgressiveWaves>();
            if (!enableEarthbreakerSlamAI || !progressiveEarthquake)
            {
                Debug.LogError("[Titan] V014 Earthquake requires Slam AI and the progressive zone component. Fail closed.", this);
                enabled = false;
                return;
            }
        }
        if (enableColossalStompV017 &&
            (!animator.HasState(0, Animator.StringToHash("Base Layer.ColossalStompRightFootV017")) ||
             !colossalStompV017 || !colossalStompV017.isActiveAndEnabled ||
             !colossalStompV017StancePreflight || !colossalStompV017StancePreflight.isActiveAndEnabled))
        {
            Debug.LogError("[Titan] V017 enabled without the approved Animator state, IK solver and stance preflight. V017 disabled; older boss attacks remain intact.", this);
            enableColossalStompV017 = false;
        }
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        beingHitLayerIndex = animator.GetLayerIndex(HitLayerName);
        ResetBeingHitOverlay();
        animator.SetBool(WalkingHash, false);
        animator.ResetTrigger(AwakenHash);
        animator.ResetTrigger(HammerHash);
        if (enableEarthbreakerSlamAI) animator.ResetTrigger(SlamHash);
        animator.Play(DormantHash, 0, 0f);
        if (agent)
        {
            // Never start walking during riddles or awakening.
            if (agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
            agent.enabled = false;
        }
        if (combatCollider) combatCollider.enabled = false;
        if (physicalBodyCollider) physicalBodyCollider.enabled = true;
        if (spellTarget) spellTarget.tag = "Untagged";
        if (health) health.SetInvulnerable(true);
        if (health) health.Died += OnHealthDied;
        var hud = PersistentHUDRoot.Instance;
        if (health != null && hud != null && hud.BoundEnemyHealth == health)
            hud.HideEnemyHealth(health);
        state = TitanState.Dormant;
        awakened = false;
        battleStarted = false;
        hammerImpactReceived = false;
        slamImpactReceived = false;
        lastAttackWasHammerfist = false;
        slamAntiRepeatLocked = false;
        slamAntiRepeatStatus = "Ready";
        slamFollowupsWaitingForHit = false;
        lastSlamVerifiedHit = false;
        slamVerifiedHits = 0;
        slamHitCheckerStatus = "No Slam yet";
        verifiedSlamFollowupPending = false;
        verifiedSlamFollowupStatus = "None";
        hammerfistStompPursuitPending = false;
        hammerfistStompPursuitStatus = "None";
        hammerfistStompVictimMotion = null;
        deathStartedTime = -1f;
        deathAnimationStarted = false;
        deathEventReceived = false;
        deathStatus = "Alive";
        beingHitStatus = "Ready for real player spell hits";
        gameObject.tag = "Untagged";
    }

    private void OnDestroy()
    {
        if (colossalStompV017)
            colossalStompV017.CertifiedRightSoleGroundContact -= OnCertifiedV017SoleGroundContact;
        if (progressiveEarthquake) progressiveEarthquake.StopSequence();
        if (health) health.Died -= OnHealthDied;
    }

    private void OnDisable()
    {
        ResetBeingHitOverlay();
        if (colossalStompV017)
            colossalStompV017.CertifiedRightSoleGroundContact -= OnCertifiedV017SoleGroundContact;
        hammerfistStompPursuitPending = false;
        hammerfistStompVictimMotion = null;
        hammerfistStompPursuitStatus = "Cancelled: boss disabled";
        verifiedSlamFollowupPending = false;
        verifiedSlamFollowupStatus = "Cancelled: disabled";
        if (progressiveEarthquake) progressiveEarthquake.StopSequence();
        if (colossalStompV017 && colossalStompV017.IsRunning) colossalStompV017.StopPreview();
        FinishV017StanceApproach(false);
        if (state == TitanState.ColossalStompV017)
        {
            state = TitanState.Chasing;
            nextAttackTime = Mathf.Max(nextAttackTime, Time.time + colossalStompV017RecoverySeconds);
        }
    }

    /// <summary>
    /// Only the fourth-riddle blue-laser handoff calls this.
    /// Does NOT start the fight, deal damage or move the actor.
    /// </summary>
    public void StartAwakening()
    {
        if (state != TitanState.Dormant || !isActiveAndEnabled) return;
        state = TitanState.Awakening;
        StopNavigation();
        animator.SetBool(WalkingHash, false);
        animator.ResetTrigger(HammerHash);
        if (enableEarthbreakerSlamAI) animator.ResetTrigger(SlamHash);
        animator.Play(DormantHash, 0, 0f);
        animator.Update(0f);
        animator.SetTrigger(AwakenHash);
        Debug.Log("[Titan] Fourth-riddle laser -> Awakening V004. Combat still locked.", this);
    }

    // Deliberately separate from StartAwakening, never called by riddle code.
    public void StartBattle()
    {
        if (!awakened || state != TitanState.AwakenedHold || battleStarted || defeatReported)
            return;
        if (health != null && health.IsDead) return;
        if (!ResolvePlayer()) return;
        if (!agent)
        {
            Debug.LogWarning("[Titan] Awake and ready. Battle waiting for authored NavMeshAgent.", this);
            return;
        }
        if (!agent.enabled)
        {
            if (!NavMesh.SamplePosition(transform.position, out var start, 10f, NavMesh.AllAreas))
            {
                Debug.LogWarning("[Titan] No nearby baked NavMesh under the arena. Remain awakened/idle.", this);
                return;
            }
            if (agent.transform == transform)
            {
                // Single-GameObject setup: keep the Titan's approved world
                // height above the arena navmesh. Never teleport the giant
                // actor to the navmesh Y or alter his 40.01 local scale.
                // NavMeshAgent.baseOffset is multiplied by transform scale.
                // On the approved 40.01x Titan, a raw 3m offset becomes
                // ~120m and launches the statue into the sky.
                float verticalScale = Mathf.Max(0.001f, Mathf.Abs(transform.lossyScale.y));
                agent.baseOffset = (transform.position.y - start.position.y) / verticalScale;
                agent.updatePosition = true;
            }
            else
            {
                // Backward compatible with isolated V005–V007 review copies.
                agent.transform.position = start.position;
            }
            agent.enabled = true;
        }
        if (!agent.isOnNavMesh)
        {
            agent.enabled = false;
            if (!noNavMeshLogged)
            {
                Debug.LogWarning("[Titan] NavMeshAgent not on baked NavMesh. Combat remains locked.", this);
                noNavMeshLogged = true;
            }
            return;
        }
        agent.speed = walkSpeed;
        agent.acceleration = acceleration;
        agent.angularSpeed = turnSpeedDegrees;
        agent.stoppingDistance = useContactAlignedApproach
            ? Mathf.Max(0.25f, HammerfistContactReach)
            : Mathf.Max(0.25f, attackRange * 0.7f);
        agent.autoBraking = true;
        agent.updateRotation = false;
        rootOffsetAboveNavMesh = transform.position.y - agent.transform.position.y;
        agent.isStopped = false;
        if (combatCollider) combatCollider.enabled = true;
        if (spellTarget)
        {
            // HomingProjectile.FindNearestEnemy targets an Enemy-tagged
            // Transform. Target the giant's animated chest, NOT its foot-level
            // root at Y=4.69, and keep the EnemyHealth on the parent.
            spellTarget.tag = "Enemy";
            gameObject.tag = "Untagged";
        }
        else
        {
            // In consolidated V008, root has the collider and EnemyHealth.
            // HomingProjectile uses SpellAimPosition to target the chest.
            gameObject.tag = "Enemy";
        }
        if (health) health.SetInvulnerable(false);
        battleStarted = true;
        nextAttackTime = Time.time + 0.5f;
        nextRepathTime = 0f;
        state = TitanState.Chasing;
        EnterHeavyWalk();
        Debug.Log("[Titan] Awakening finished -> combat started and Heavy Walk engaged. No distance-based chase leash.", this);
    }

    private void Update()
    {
#if UNITY_EDITOR
        HandleStage3BeingHitDeathDebugKeys();
#endif
        if (state == TitanState.Dead)
        {
            UpdateDeathCompletionFallback();
            return;
        }
        UpdateBeingHitOverlay();
        // The persistent HUD can spawn after the Titan. Retry only once every
        // half-second, and never reveal its bar during riddles or Awakening.
        if (IsHealthBarUnlocked && Time.time >= nextHealthBarCheck)
        {
            nextHealthBarCheck = Time.time + 0.5f;
            var hud = PersistentHUDRoot.Instance;
            if (hud != null && (hud.BoundEnemyHealth != health
                || (hud.EnemyHealthSlider != null && !hud.EnemyHealthSlider.gameObject.activeSelf)))
                EnemyEncounterHUD.ShowFor(health);
        }
        if (state == TitanState.Dormant) return;
        if (state == TitanState.Awakening)
        {
            var info = animator.GetCurrentAnimatorStateInfo(0);
            if (!animator.IsInTransition(0) && info.fullPathHash == ReadyHash)
                CompleteAwakening();
            return;
        }
        if (health && health.IsDead) { TriggerDeath(); return; }
        if (state == TitanState.AwakenedHold) return;
        if (beingHitOverlayActive && state == TitanState.Chasing)
        {
            // Golem-style brief hit-stop while an isolated UPPER-body overlay
            // is actually visible. Never cancel Hammerfist, moving Slam wave,
            // V017 four-sole solver, or the original Awakening animation.
            StopNavigation();
            animator.SetBool(WalkingHash, false);
            return;
        }
        if (state == TitanState.Hammerfist) { UpdateHammerfist(); return; }
        if (state == TitanState.EarthbreakerSlam) { UpdateEarthbreakerSlam(); return; }
        if (state == TitanState.ColossalStompV017) { UpdateColossalStompV017(); return; }
        if (!battleStarted || !agent || !agent.enabled || !agent.isOnNavMesh)
        {
            FinishV017StanceApproach(false);
            StopNavigation();
            animator.SetBool(WalkingHash, false);
            state = TitanState.AwakenedHold;
            battleStarted = false;
            return;
        }
        if (agent.transform != transform)
        {
            // Visual skeleton follows only XZ navigation. Its rig-specific
            // height above the ground stays fixed, avoiding giant Y jumps.
            Vector3 navPosition = agent.transform.position;
            transform.position = new Vector3(navPosition.x,
                navPosition.y + rootOffsetAboveNavMesh, navPosition.z);
        }
        if (!player && Time.time >= nextPlayerLookup)
        {
            ResolvePlayer();
            nextPlayerLookup = Time.time + 0.5f;
        }
        if (!player || player.currentHealth <= 0)
        {
            FinishV017StanceApproach(false);
            StopNavigation();
            animator.SetBool(WalkingHash, false);
            return;
        }
        // Animator can restart at Dormant after an editor domain reload while
        // runtime controller fields are preserved. Repair the animation to
        // match active combat instead of leaving a walking statue frozen.
        if (!animator.IsInTransition(0) &&
            animator.GetCurrentAnimatorStateInfo(0).fullPathHash == DormantHash)
            EnterHeavyWalk();
        Vector3 diff = player.transform.position - transform.position;
        Vector3 flat = new Vector3(diff.x, 0f, diff.z);
        if (enableAttackHierarchy && enableHammerfistStompPursuit &&
            hammerfistStompPursuitPending)
        {
            if (Time.time >= hammerfistStompPursuitDeadline ||
                !player || player.currentHealth <= 0 ||
                !enableColossalStompV017 || !autoSelectColossalStompV017 ||
                !enableColossalStompV017StanceApproach ||
                !colossalStompV017 || !colossalStompV017.isActiveAndEnabled)
            {
                // Do not leave an older pursuit-controlled V017 approach
                // running beyond its explicit fair attack opportunity.
                if (v017StanceApproaching)
                    FinishV017StanceApproach(true);
                hammerfistStompPursuitPending = false;
                hammerfistStompPursuitStatus = "Expired: real attack opportunity ended or V017 unavailable";
                hammerfistStompVictimMotion = null;
                Debug.Log("[TITAN HAMMER->STOMP] Time-bounded pursuit expired. " +
                    "Normal hierarchy resumes; no unverified stomp.", this);
            }
            else if (hammerfistStompVictimMotion &&
                     hammerfistStompVictimMotion.IsAirborneLocked)
            {
                // Do not sample or commit V017 foot contact against airborne
                // Van. The boss can only stage the actual grounded follow-up.
                StopNavigation();
                animator.SetBool(WalkingHash, false);
                hammerfistStompPursuitStatus = "Holding: Van still physically airborne";
                return;
            }
        }

        // Native full-scene ground/solid-body/left support/94% IK is still the
        // only entrance into V017, whether pursuing a Hammerfist or not.
        if (TryHandleV017StanceApproach(flat.magnitude)) return;
        // Unlock only after real root displacement on a valid NavMesh during
        // Chasing. Cooldown/time, moving Van, yaw and baseOffset are irrelevant.
        if (enableAttackHierarchy && enableSlamAntiRepeat && slamAntiRepeatLocked &&
            !(enableVerifiedSlamRangeFollowup && verifiedSlamFollowupPending) &&
            TitanAttackDecision.HasRepositionedSinceSlam(
                transform.position, slamAntiRepeatOrigin, slamAntiRepeatMoveMeters))
            ClearSlamRepeatLock("Titan physically repositioned " +
                new Vector2(transform.position.x - slamAntiRepeatOrigin.x,
                    transform.position.z - slamAntiRepeatOrigin.z).magnitude.ToString("F1") + "m");
        Quaternion desiredFacing = transform.rotation;
        if (flat.sqrMagnitude > 0.001f)
        {
            desiredFacing = Quaternion.LookRotation(flat.normalized, Vector3.up);
            if (useContactAlignedApproach)
            {
                // V003's right fist lands to the right of Titan's forward
                // axis. Offset facing so the hand, not his torso, aims at Van.
                float fistAzimuth = Mathf.Atan2(hammerfistContactLocalXZ.x,
                    hammerfistContactLocalXZ.y) * Mathf.Rad2Deg;
                desiredFacing *= Quaternion.Euler(0f, -fistAzimuth, 0f);
            }
            transform.rotation = Quaternion.RotateTowards(transform.rotation,
                desiredFacing, turnSpeedDegrees * Time.deltaTime);
        }
        var distance = flat.magnitude;
        if (enableColossalStompV017 && autoSelectColossalStompV017 &&
            !enableColossalStompV017StanceApproach &&
            Time.time >= nextAttackTime && Time.time >= nextV017StanceAttemptTime &&
            distance <= colossalStompV017MaxPlayerRange &&
            !animator.IsInTransition(0) &&
            animator.GetCurrentAnimatorStateInfo(0).fullPathHash == HeavyWalkStateHash &&
            BeginColossalStompV017())
            return;
        // The approved right-fist slam lands ~41m from the giant's root.
        // `attackRange=10` belongs only to the legacy unaligned mode, NOT
        // this giant-scale hierarchy or the Hammerfist physical contact band.
        bool attackAligned = useContactAlignedApproach
            ? Mathf.Abs(distance - HammerfistContactReach) <= hammerfistRangeTolerance
                && Quaternion.Angle(transform.rotation, desiredFacing) <= hammerfistAimToleranceDegrees
            : distance <= attackRange
                && Vector3.Angle(transform.forward, flat) <= attackFacingDegrees;
        TitanAttackDecision.Intent intent = TitanAttackDecision.Intent.Chase;
        if (enableAttackHierarchy && enableHammerfistStompPursuit &&
            hammerfistStompPursuitPending)
        {
            // Hammerfist has already CONFIRMED authentic physical contact.
            // During the actual landing slow, prefer bringing the original
            // four-stomp stance into legal range, never a second Slam/Hammer.
            intent = TitanAttackDecision.Intent.Reposition;
            attackAligned = false;
            string decision = "AUTHENTIC HAMMER HIT: ground-followup V017 pursuit";
            if (hammerfistStompPursuitStatus != decision)
            {
                hammerfistStompPursuitStatus = decision;
                Debug.Log("[TITAN HAMMER->STOMP] " + decision +
                    "; player planar=" + distance.ToString("F2") +
                    "m; V017 gate=" + colossalStompV017MaxPlayerRange.ToString("F2") +
                    "m. Real NavMesh + strict anatomical stance required.", this);
            }
        }
        else if (enableAttackHierarchy && enableVerifiedSlamRangeFollowup &&
            verifiedSlamFollowupPending)
        {
            // An actual Slam hit does not grant an instant animation cancel.
            // We arrive here ONLY after authored Slam/zone recovery has fully
            // returned to Chasing. V017's legal-stance planner (called above)
            // already gets first chance to own a close range approach.
            intent = TitanAttackDecision.EvaluateVerifiedSlamFollowup(
                distance, Time.time >= nextAttackTime,
                enableColossalStompV017 && autoSelectColossalStompV017 &&
                    enableColossalStompV017StanceApproach &&
                    colossalStompV017 && colossalStompV017.isActiveAndEnabled,
                colossalStompV017MaxPlayerRange, useContactAlignedApproach,
                HammerfistContactReach, hammerfistRangeTolerance,
                Quaternion.Angle(transform.rotation, desiredFacing),
                hammerfistAimToleranceDegrees);

            // If a close V017 attempt was rejected by REAL stance geometry,
            // don't count it as executed. Navigate toward the physically
            // eligible fist band instead; never skip the left-foot preflight.
            if (intent == TitanAttackDecision.Intent.ColossalStompV017)
                intent = TitanAttackDecision.Intent.Reposition;
            string decision = "Verified Slam HIT: " +
                (intent == TitanAttackDecision.Intent.Hammerfist
                    ? "HAMMERFIST NOW (real range/facing/recovery passed)"
                    : "APPROACH / TURN for actual legal next attack");
            if (verifiedSlamFollowupStatus != decision)
            {
                verifiedSlamFollowupStatus = decision;
                Debug.Log("[TITAN SLAM FOLLOWUP] " + decision +
                    "; current Van range=" + distance.ToString("F2") + "m" +
                    "; eligible Hammerfist=" +
                    (HammerfistContactReach - hammerfistRangeTolerance).ToString("F2") +
                    ".." + (HammerfistContactReach + hammerfistRangeTolerance).ToString("F2") +
                    "m. Slam repeat blocked until another real attack starts.", this);
            }
            attackAligned = intent == TitanAttackDecision.Intent.Hammerfist;
        }
        else if (enableAttackHierarchy)
        {
            // V017 has ALREADY received first priority above via
            // TryHandleV017StanceApproach. An unsafe/blocked close stomp must
            // not fall through to a slam that bypasses its collision preflight.
            // This method describes the NEXT safe action, never runs V017 IK.
            bool reservedClose = enableColossalStompV017 && autoSelectColossalStompV017;
            bool waitingOnRealSlamHit = requireSlamHitForFollowupAnimations &&
                slamFollowupsWaitingForHit;
            intent = TitanAttackDecision.EvaluateHierarchy(
                distance, colossalStompV017MaxPlayerRange, reservedClose,
                HammerfistContactReach, hammerfistRangeTolerance,
                Quaternion.Angle(transform.rotation, desiredFacing),
                hammerfistAimToleranceDegrees, useContactAlignedApproach,
                slamMinAttackRange, slamMaxAttackRange,
                Vector3.Angle(transform.forward, flat),
                slamFacingToleranceDegrees, enableEarthbreakerSlamAI &&
                    animator.HasState(0, SlamStateHash),
                Time.time >= nextAttackTime, lastAttackWasHammerfist,
                enableSlamAntiRepeat && slamAntiRepeatLocked,
                waitingOnRealSlamHit);

            string band = reservedClose && distance <= colossalStompV017MaxPlayerRange
                ? "CLOSE: V017 verified stance or reposition"
                : distance < HammerfistContactReach - hammerfistRangeTolerance
                    ? "NEAR: Earthbreaker or reposition"
                    : distance <= HammerfistContactReach + hammerfistRangeTolerance
                        ? "MID: Hammerfist/Earthbreaker contextual alternation"
                        : distance <= slamMaxAttackRange
                            ? "FAR: Earthbreaker or chase"
                            : "OUTSIDE: HeavyWalk chase";
            string choice = band + " => " + intent +
                (requireSlamHitForFollowupAnimations
                    ? (waitingOnRealSlamHit ? " [SLAM HIT REQUIRED]" : " [FOLLOW-UPS OPEN]")
                    : "");
            if (lastAttackHierarchyDecision != choice)
            {
                lastAttackHierarchyDecision = choice;
                Debug.Log("[TITAN ATTACK HIERARCHY] " + choice +
                    "; playerPlanar=" + distance.ToString("F2") + "m; " +
                    "HammerReach=" + HammerfistContactReach.ToString("F2") +
                    "+/-" + hammerfistRangeTolerance.ToString("F2") +
                    "m, Slam=" + slamMinAttackRange.ToString("F1") +
                    ".." + slamMaxAttackRange.ToString("F1") +
                    "m, V017<= " + colossalStompV017MaxPlayerRange.ToString("F1") +
                    "m; previousHammer=" + lastAttackWasHammerfist +
                    ", attackReady=" + (Time.time >= nextAttackTime) +
                    ", slamRepeatLocked=" + (enableSlamAntiRepeat && slamAntiRepeatLocked) +
                    ", followupsAwaitHit=" + waitingOnRealSlamHit, this);
            }
            attackAligned = intent == TitanAttackDecision.Intent.Hammerfist;
            if (intent == TitanAttackDecision.Intent.EarthbreakerSlam)
            {
                BeginEarthbreakerSlam();
                return;
            }
        }
        else if (enableTacticalDecisionReview && useContactAlignedApproach)
        {
            intent = TitanAttackDecision.Evaluate(distance, HammerfistContactReach,
                hammerfistRangeTolerance, Quaternion.Angle(transform.rotation, desiredFacing),
                hammerfistAimToleranceDegrees, Time.time >= nextAttackTime);
            attackAligned = intent == TitanAttackDecision.Intent.Hammerfist;
        }
        if (!enableAttackHierarchy && enableEarthbreakerSlamAI && enableTacticalDecisionReview &&
            useContactAlignedApproach)
        {
            intent = TitanAttackDecision.EvaluateWithSlam(distance,
                HammerfistContactReach, hammerfistRangeTolerance,
                Quaternion.Angle(transform.rotation, desiredFacing),
                hammerfistAimToleranceDegrees,
                Vector3.Angle(transform.forward, flat),
                slamFacingToleranceDegrees, slamMinAttackRange,
                slamMaxAttackRange, Time.time >= nextAttackTime,
                lastAttackWasHammerfist, true);
            attackAligned = intent == TitanAttackDecision.Intent.Hammerfist;
            if (intent == TitanAttackDecision.Intent.EarthbreakerSlam)
            {
                BeginEarthbreakerSlam();
                return;
            }
        }
        if (attackAligned && Time.time >= nextAttackTime &&
            !(enableAttackHierarchy && requireSlamHitForFollowupAnimations &&
              slamFollowupsWaitingForHit))
        {
            BeginHammerfist();
            return;
        }
        // NO chase-radius / player-distance leash. Once the four riddles and
        // Awakening are complete, Titan chases continuously while alive.
        // Walkable NavMesh and authored arena geometry, not a made-up radial
        // distance from Node 4, determine where his feet can travel.
        if (Time.time >= nextRepathTime)
        {
            Vector3 navTarget = player.transform.position;
            bool stompFollowupApproach = enableAttackHierarchy &&
                enableHammerfistStompPursuit && hammerfistStompPursuitPending &&
                intent == TitanAttackDecision.Intent.Reposition;
            if (stompFollowupApproach)
            {
                // The original giant's fist range (~41.5m) is much farther
                // than its V017 entry gate (18m). Natural HeavyWalk must close
                // that distance. Never teleport Van, ignore collision, or
                // claim four foot impacts until the strict IK verifier runs.
                agent.stoppingDistance = Mathf.Max(0.5f,
                    colossalStompV017MaxPlayerRange - 2f);
                bool routed = false;
                if (NavMesh.SamplePosition(player.transform.position,
                        out var stompNavTarget, 8f, NavMesh.AllAreas))
                {
                    NavMeshPath path = new NavMeshPath();
                    routed = agent.CalculatePath(stompNavTarget.position, path) &&
                        path.status == NavMeshPathStatus.PathComplete &&
                        agent.SetDestination(stompNavTarget.position);
                }
                if (!routed)
                {
                    agent.ResetPath();
                    agent.isStopped = true;
                    animator.SetBool(WalkingHash, false);
                    if (Time.time >= nextHammerfistPursuitNotice)
                    {
                        nextHammerfistPursuitNotice = Time.time + 5f;
                        Debug.LogWarning("[TITAN HAMMER->STOMP] No complete " +
                            "NavMesh path to close-range entry; wait safely. " +
                            "Never warp Van or fake a V017 stance.", this);
                    }
                    nextRepathTime = Time.time + repathSeconds;
                    return;
                }
                nextRepathTime = Time.time + repathSeconds;
            }
            bool physicalHitFollowupApproach = enableAttackHierarchy &&
                enableVerifiedSlamRangeFollowup && verifiedSlamFollowupPending &&
                intent == TitanAttackDecision.Intent.Reposition;
            bool insideHammerBand = useContactAlignedApproach &&
                Mathf.Abs(distance - HammerfistContactReach) <= hammerfistRangeTolerance;
            if (physicalHitFollowupApproach && insideHammerBand &&
                distance > colossalStompV017MaxPlayerRange)
            {
                // Already in genuine fist reach: no pointless closing into
                // Van's collider. Rotate in place and wait for original Slam
                // recovery/cooldown; next Update re-evaluates facing live.
                if (agent.hasPath) agent.ResetPath();
                agent.isStopped = true;
                animator.SetBool(WalkingHash, false);
                nextRepathTime = Time.time + repathSeconds;
                return;
            }
            bool antiRepeatReposition = enableAttackHierarchy && enableSlamAntiRepeat &&
                slamAntiRepeatLocked && intent == TitanAttackDecision.Intent.Reposition &&
                distance > colossalStompV017MaxPlayerRange;
            if (!stompFollowupApproach && (antiRepeatReposition || physicalHitFollowupApproach))
            {
                // Seek the REAL Hammerfist contact band, instead of repeatedly
                // using Slam from its 25m sweet spot. With the player at 58m,
                // this approaches; at 25m it backs off. Either way requires
                // a physically reachable NavMesh path before walking.
                agent.stoppingDistance = 0.5f;
                if (!TrySetSlamAntiRepeatDestination(flat, distance) &&
                    Time.time >= nextSlamAntiRepeatNotice)
                {
                    nextSlamAntiRepeatNotice = Time.time + 5f;
                    Debug.LogWarning("[TITAN SLAM FOLLOWUP] No complete NavMesh path toward Hammerfist band. Holding follow-up, not teleporting or repeating Slam.", this);
                }
                nextRepathTime = Time.time + repathSeconds;
            }
            else if (!stompFollowupApproach && intent == TitanAttackDecision.Intent.Reposition)
            {
                // Van is under the giant's hand/body. Make room for the
                // approved fist strike rather than stall at NavMesh stoppingDistance.
                Vector3 away = -flat.normalized;
                if (flat.sqrMagnitude < 0.001f) away = -transform.forward;
                navTarget = transform.position + away *
                    Mathf.Clamp(HammerfistContactReach - distance + 4f, 4f, 16f);
                agent.stoppingDistance = 0.5f;
            }
            else if (!stompFollowupApproach && useContactAlignedApproach)
            {
                agent.stoppingDistance = Mathf.Max(0.25f, HammerfistContactReach);
            }
            if (!stompFollowupApproach && !antiRepeatReposition &&
                !physicalHitFollowupApproach &&
                NavMesh.SamplePosition(navTarget, out var destination, 4f, NavMesh.AllAreas))
                agent.SetDestination(destination.position);
            nextRepathTime = Time.time + repathSeconds;
        }
        if (enableAttackHierarchy && enableHammerfistStompPursuit &&
            hammerfistStompPursuitPending &&
            distance > colossalStompV017MaxPlayerRange &&
            !agent.pathPending && !agent.hasPath)
        {
            // A failed route must not play HeavyWalk in place and must not
            // hide a missing path behind a cosmetic animation.
            agent.isStopped = true;
            animator.SetBool(WalkingHash, false);
            return;
        }
        if (agent.isStopped) agent.isStopped = false;
        animator.SetBool(WalkingHash, true);
    }

    // This is an ordinary, visible NavMesh HeavyWalk reposition, not an
    // invisible reposition or a target warp. Aim for the authored fist band
    // after Slam; use side arcs when a platform/wall blocks the direct route.
    // A failed path NEVER unlocks the anti-repeat rule.
    private bool TrySetSlamAntiRepeatDestination(Vector3 flatToPlayer, float distance)
    {
        if (!agent || !agent.enabled || !agent.isOnNavMesh || !player ||
            distance < 0.1f || flatToPlayer.sqrMagnitude < 0.01f)
            return false;

        Vector3 direction = flatToPlayer.normalized;
        Vector3 playerPos = player.transform.position;
        float desiredReach = HammerfistContactReach;
        float[] sideAngles = { 0f, 25f, -25f, 50f, -50f, 80f, -80f };
        for (int i = 0; i < sideAngles.Length; i++)
        {
            Vector3 radial = Quaternion.AngleAxis(sideAngles[i], Vector3.up) * direction;
            Vector3 point = playerPos - radial * desiredReach;
            if (!NavMesh.SamplePosition(point, out NavMeshHit sampled, 4f, NavMesh.AllAreas))
                continue;
            Vector3 destination = sampled.position;
            Vector2 delta = new Vector2(destination.x - transform.position.x,
                destination.z - transform.position.z);
            if (delta.magnitude < 2f) continue;
            NavMeshPath path = new NavMeshPath();
            if (!agent.CalculatePath(destination, path) ||
                path.status != NavMeshPathStatus.PathComplete ||
                !agent.SetDestination(destination))
                continue;
            slamAntiRepeatStatus = "Reposition via HeavyWalk toward Hammerfist band";
            return true;
        }
        slamAntiRepeatStatus = "Locked: no complete physical NavMesh path";
        agent.ResetPath();
        return false;
    }

    private void ClearSlamRepeatLock(string reason)
    {
        if (!slamAntiRepeatLocked) return;
        slamAntiRepeatLocked = false;
        slamAntiRepeatStatus = "Unlocked: " + reason;
        Debug.Log("[TITAN ANTI-REPEAT] Earthbreaker unlocked: " + reason +
                  ". Normal hierarchy can select a future Slam when its own cooldown, range and facing pass.", this);
    }

    private void EnterHeavyWalk()
    {
        if (state != TitanState.Chasing || !awakened || !battleStarted)
            return;
        animator.ResetTrigger(HammerHash);
        if (enableEarthbreakerSlamAI) animator.ResetTrigger(SlamHash);
        animator.SetBool(WalkingHash, true);
        // The Animator only reaches here after AwakenedHold or a completed
        // Hammerfist; explicitly enter the approved loop so there is no
        // dormant/idle pause between Awakening and the opening chase.
        animator.CrossFade(HeavyWalkStateHash, 0.12f, 0, 0f);
    }

    /// <summary>
    /// Production-safe V017 animation entry. Only after the riddle -> Awakening
    /// -> battle gate, never on a timer during riddles. Gameplay HP depends
    /// on each post-RigBuilder certified sole contact AND current Van collider.
    /// BeginPreview is reused as the proven IK/mesh-contact solver; the Titan
    /// controller owns navigation and return-to-combat instead of competing AI.
    /// </summary>
    public bool BeginColossalStompV017()
    {
        if (!enableColossalStompV017 || !colossalStompV017 ||
            (enableAttackHierarchy && requireSlamHitForFollowupAnimations &&
             slamFollowupsWaitingForHit) ||
            !colossalStompV017.isActiveAndEnabled || colossalStompV017.IsRunning ||
            state != TitanState.Chasing ||
            !battleStarted || !awakened || defeatReported ||
            (health && health.IsDead) || !ResolvePlayer() || player.currentHealth <= 0 ||
            !agent || !agent.enabled || !agent.isOnNavMesh ||
            Time.time < nextAttackTime || animator.IsInTransition(0) ||
            animator.GetCurrentAnimatorStateInfo(0).fullPathHash != HeavyWalkStateHash)
            return false;

        // The solid body can physically exclude Van from the authored right
        // sole landing. Verify the full stance BEFORE committing the external
        // aim marker; never reposition the Titan or stretch IK to compensate.
        if (!TryValidateV017Stance(out Vector3 validatedLandingGround)) return false;

        stompLastProcessedSoleIndex = 0;
        stompPlayerHitCount = 0;
        stompActualDamageThisAttack = 0f;
        stompLastImpactIndex = 0;
        stompLastDamageZone = 0;
        stompLastPlayerColliderDistance = -1f;
        stompLastHitStatus = "Waiting for authentic deformed-mesh sole impact";
        // Subscribe only to genuine LBS sole contacts AFTER the stricter
        // physical stance preflight. No animation event/timer proxy allowed.
        colossalStompV017.CertifiedRightSoleGroundContact -= OnCertifiedV017SoleGroundContact;
        colossalStompV017.CertifiedRightSoleGroundContact += OnCertifiedV017SoleGroundContact;

        StopNavigation();
        animator.SetBool(WalkingHash, false);
        animator.ResetTrigger(HammerHash);
        if (enableEarthbreakerSlamAI) animator.ResetTrigger(SlamHash);
        // This is the ONLY marker write. The independent world-space reference
        // follows Van solely by being RECOMMITTED between attacks, after a
        // fresh physical stance pass. All four windups read this fixed point.
        colossalStompV017AimMarker.position = validatedLandingGround;
        colossalStompCommittedAimPosition = validatedLandingGround;
        Debug.Log("[Titan V017] Committed verified world-space right-sole landing " +
            colossalStompCommittedAimPosition.ToString("F2") + "; aim marker locked for all four stomps.", this);
        colossalStompV017.BeginPreview();
        if (!colossalStompV017.IsRunning)
        {
            // A missing/uneven floor, unreadable mesh or unreachable target
            // must NEVER produce a fake stomp or disable the existing AI.
            nextAttackTime = Time.time + colossalStompV017RecoverySeconds;
            lastV017StanceRejection = "IK solver refused to start or did not meet its physical calibration gates.";
            EnterHeavyWalk();
            return false;
        }
        state = TitanState.ColossalStompV017;
        if (hammerfistStompPursuitPending)
        {
            hammerfistStompPursuitPending = false;
            hammerfistStompVictimMotion = null;
            hammerfistStompPursuitStatus = "COMMITTED real 4-stomp V017 after native stance pass";
            Debug.Log("[TITAN HAMMER->STOMP] COMMITTED four original right stomps " +
                "only after physically legal stance and live Animator/IK startup. " +
                "The prior Hammerfist launch did NOT guarantee foot impact.", this);
        }
        if (enableAttackHierarchy && enableVerifiedSlamRangeFollowup &&
            verifiedSlamFollowupPending)
            CompleteVerifiedSlamFollowup("four-stomp V017 started after authentic stance verification");
        if (enableAttackHierarchy && enableSlamAntiRepeat)
            ClearSlamRepeatLock("a different authentic V017 attack started");
        colossalStompStartedAt = Time.time;
        nextAttackTime = Time.time + colossalStompV017RecoverySeconds;
        Debug.Log("[Titan V017] Four-stomp Animation Rigging started; HP is conditional on each real sole+player overlap, never on animation time.", this);
        return true;
    }

    private void UpdateColossalStompV017()
    {
        bool interrupted = !battleStarted || !awakened || defeatReported ||
            !ResolvePlayer() || player.currentHealth <= 0 ||
            !colossalStompV017 || !colossalStompV017.isActiveAndEnabled ||
            !colossalStompV017AimMarker ||
            (colossalStompV017AimMarker &&
             (!IsFiniteV017WorldPoint(colossalStompV017AimMarker.position) ||
              Vector3.Distance(colossalStompV017AimMarker.position,
                  colossalStompCommittedAimPosition) > colossalStompV017AimMarkerTolerance)) ||
            Time.time - colossalStompStartedAt > colossalStompV017WatchdogSeconds;
        if (interrupted)
        {
            if (colossalStompV017 && colossalStompV017.IsRunning)
                colossalStompV017.StopPreview();
            Debug.LogWarning("[Titan V017] Target/encounter lost, watchdog expired or solver disabled. Aborted without contact credit, damage or CC.", this);
        }
        else if (colossalStompV017.IsRunning)
        {
            // No navigation or yaw turn during a committed world-space stomp.
            StopNavigation();
            return;
        }

        if (!interrupted && colossalStompV017.LastStompPassed &&
            colossalStompV017.VerifiedSoleContacts == 4)
        {
            colossalStompsVerified += colossalStompV017.VerifiedSoleContacts;
            Debug.Log("[Titan V017] Four post-rig physical foot contacts verified. " +
                "Actual player hits=" + stompPlayerHitCount +
                " HP this clip=" + stompActualDamageThisAttack.ToString("F1") +
                "; certified soles alone never auto-damage a player.", this);
        }
        else Debug.LogWarning("[Titan V017] Four-stomp solver aborted or stopped: no full 4/4 credit. " +
            "Already-certified individual player hits=" + stompPlayerHitCount +
            " HP=" + stompActualDamageThisAttack.ToString("F1") +
            " are genuine and are NOT retroactively fabricated or reversed.", this);
        if (progressiveEarthquake) progressiveEarthquake.StopSequence();
        nextAttackTime = Mathf.Max(nextAttackTime, Time.time + colossalStompV017RecoverySeconds);
        state = TitanState.Chasing;
        if (!battleStarted || !player || player.currentHealth <= 0)
        {
            StopNavigation();
            animator.SetBool(WalkingHash, false);
            return;
        }
        if (agent && agent.enabled && agent.isOnNavMesh) agent.isStopped = false;
        EnterHeavyWalk();
    }

    public static int CertifiedStompDamageZone(float nearestColliderXZ,
        float inner, float middle, float outer)
    {
        return HammerfistDamageZone(nearestColliderXZ, inner, middle, outer);
    }

    /// <summary>
    /// A separate PLAYER hit checker, never the source of the sole PASS.
    /// The V017 solver calls this ONLY after genuine post-rig skinned mesh
    /// contact, native left grounding and the strict 94% reach proof.
    /// An impact on bare floor is still a perfectly good sole contact but
    /// causes zero HP unless Van's actual live collider is in range NOW.
    /// </summary>
    private void OnCertifiedV017SoleGroundContact(int contactIndex,
        Vector3 actualDeformedSoleGround)
    {
        if (state != TitanState.ColossalStompV017 || !battleStarted || !awakened ||
            defeatReported || !colossalStompV017 || !colossalStompV017.IsRunning ||
            contactIndex < 1 || contactIndex > 4 ||
            contactIndex != stompLastProcessedSoleIndex + 1 ||
            !IsFiniteV017WorldPoint(actualDeformedSoleGround) ||
            Vector2.Distance(new Vector2(actualDeformedSoleGround.x, actualDeformedSoleGround.z),
                new Vector2(colossalStompCommittedAimPosition.x, colossalStompCommittedAimPosition.z)) >
                    colossalStompV017AimMarkerTolerance ||
            Mathf.Abs(actualDeformedSoleGround.y - colossalStompCommittedAimPosition.y) > .3f)
            return;

        stompLastProcessedSoleIndex = contactIndex;
        stompLastImpactIndex = contactIndex;
        stompLastCertifiedFootGround = actualDeformedSoleGround;
        stompLastDamageZone = 0;
        stompLastPlayerColliderDistance = -1f;
        stompLastHitStatus = "Genuine sole on ground; checking live player collider";

        if (!enableV017VerifiedStompDamage || !ResolvePlayer() || !player ||
            player.currentHealth <= 0 ||
            !float.IsFinite(stompCoreDamage) || !float.IsFinite(stompMiddleDamage) ||
            !float.IsFinite(stompOuterDamage) ||
            !float.IsFinite(stompTotalDamageCap) ||
            stompCoreDamage < 0f || stompMiddleDamage < 0f ||
            stompOuterDamage < 0f || stompTotalDamageCap <= 0f ||
            !(stompCoreRadius > 0f && stompCoreRadius < stompMiddleRadius &&
              stompMiddleRadius < stompOuterRadius))
        {
            stompLastHitStatus = "Player damage disabled, unavailable or invalid zone settings";
            return;
        }

        CharacterController vanBody = player.GetComponent<CharacterController>();
        if (!vanBody || !vanBody.enabled || !vanBody.gameObject.activeInHierarchy ||
            Mathf.Abs(player.transform.position.y - actualDeformedSoleGround.y) >
                stompPlayerVerticalTolerance ||
            (!vanBody.isGrounded &&
             player.transform.position.y - actualDeformedSoleGround.y > 1f))
        {
            stompLastHitStatus = "Van absent, physically airborne or vertically out of range";
            return;
        }

        var colliders = Physics.OverlapSphere(actualDeformedSoleGround,
            stompOuterRadius, Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);
        bool overlap = false;
        foreach (var hit in colliders)
        {
            if (hit != vanBody) continue; // no root-proximity or proxy hits
            overlap = true;
            Vector3 nearest = vanBody.ClosestPoint(actualDeformedSoleGround);
            stompLastPlayerColliderDistance = new Vector2(
                nearest.x - actualDeformedSoleGround.x,
                nearest.z - actualDeformedSoleGround.z).magnitude;
            break;
        }
        if (!overlap || (respectSolidCover && HammerfistHasBlockingCover(
                actualDeformedSoleGround, player.transform.position)))
        {
            stompLastHitStatus = overlap ? "SOLID COVER blocked foot shock" :
                "MISS: no real Van CharacterController inside ground-impact range";
            Debug.Log("[TITAN STOMP HIT] #" + contactIndex +
                " certified sole contact but no real player hit: " + stompLastHitStatus, this);
            return;
        }

        stompLastDamageZone = CertifiedStompDamageZone(stompLastPlayerColliderDistance,
            stompCoreRadius, stompMiddleRadius, stompOuterRadius);
        float tier = HammerfistZoneDamage(stompLastDamageZone,
            stompCoreDamage, stompMiddleDamage, stompOuterDamage);
        float awarded = Mathf.Min(tier, Mathf.Max(0f,
            stompTotalDamageCap - stompActualDamageThisAttack));
        if (awarded > 0f)
        {
            player.TakeDamage(awarded);
            stompActualDamageThisAttack += awarded;
            stompPlayerHitCount++;
        }
        stompLastHitStatus = "Certified foot #" + contactIndex + " hit zone " +
            stompLastDamageZone + "/3, applied " + awarded.ToString("F1") + " HP";
        Debug.Log("[TITAN STOMP HIT] #" + contactIndex +
            " realSOLE=True playerCollider=True zone=" + stompLastDamageZone +
            "/3 nearest=" + stompLastPlayerColliderDistance.ToString("F2") +
            "m HP=" + awarded.ToString("F1") +
            " totalThisClip=" + stompActualDamageThisAttack.ToString("F1") +
            " hitCount=" + stompPlayerHitCount +
            ". No automatic hit or target homing.", this);
    }

    private bool TryValidateV017Stance(out Vector3 validatedLandingGround)
    {
        validatedLandingGround = Vector3.zero;
        if (!TryGetV017StancePlan(out Vector3 rootDestination,
                out Quaternion facing, out Vector3 landingGround))
            return false;

        Vector3 toRoot = rootDestination - transform.position;
        if (new Vector2(toRoot.x, toRoot.z).magnitude > colossalStompV017RootStanceTolerance)
            return RejectV017Stance("Titan must WALK to the planner's natural sole stance before attacking.");
        if (Quaternion.Angle(transform.rotation, facing) > colossalStompV017FacingToleranceDegrees)
            return RejectV017Stance("Titan is not facing the planner's authored right-sole landing angle.");

        // The REAL Stage3 Van capsule can be scaled ~10.71x (XZ world radius
        // ~5.355m). A foot landing on Van's CENTER would penetrate the player.
        // Enforce the grounded near-side rim at actual radius + the planner's
        // physical clearance margin, not the previous .75m center assumption.
        CharacterController vanBody = player.GetComponent<CharacterController>();
        if (!vanBody || !vanBody.enabled || !vanBody.gameObject.activeInHierarchy)
            return RejectV017Stance("Live Van physical CharacterController missing at V017 rim commitment.");
        Vector3 vanScale = vanBody.transform.lossyScale;
        float vanWorldRadius = vanBody.radius *
            Mathf.Max(Mathf.Abs(vanScale.x), Mathf.Abs(vanScale.z));
        Vector3 vanCapsuleCenter = vanBody.transform.TransformPoint(vanBody.center);
        Vector3 titanToVan = vanCapsuleCenter - transform.position;
        titanToVan.y = 0f;
        if (!IsFiniteV017WorldPoint(vanCapsuleCenter) ||
            float.IsNaN(vanWorldRadius) || float.IsInfinity(vanWorldRadius) ||
            vanWorldRadius <= 0f || titanToVan.sqrMagnitude < 0.01f)
            return RejectV017Stance("Van capsule radius/direction invalid for near-edge ground commitment.");
        float safetyGap = colossalStompV017StancePreflight.PlayerLandingSafetyGap;
        float requiredRimDistance = vanWorldRadius + safetyGap;
        Vector3 rimTarget = vanCapsuleCenter - titanToVan.normalized * requiredRimDistance;
        Vector2 rimErrorXZ = new Vector2(landingGround.x - rimTarget.x,
            landingGround.z - rimTarget.z);
        Vector2 landingFromCenterXZ = new Vector2(landingGround.x - vanCapsuleCenter.x,
            landingGround.z - vanCapsuleCenter.z);
        if (landingFromCenterXZ.magnitude + 0.01f < requiredRimDistance ||
            rimErrorXZ.magnitude > colossalStompV017RootStanceTolerance)
            return RejectV017Stance("Verified sole landing is not at Van's safe near-side grounded rim: " +
                "radius=" + vanWorldRadius.ToString("F2") + "m, gap=" + safetyGap.ToString("F2") +
                "m, landing distance=" + landingFromCenterXZ.magnitude.ToString("F2") +
                "m, near-side error=" + rimErrorXZ.magnitude.ToString("F2") + "m.");

        // The marker is intentionally allowed to be anywhere BEFORE the
        // commitment. It must be a separate world-space transform, not part
        // of the actor, its NavMesh movement or Van's moving hierarchy.
        if (!colossalStompV017AimMarker ||
            colossalStompV017AimMarker.IsChildOf(transform) ||
            transform.IsChildOf(colossalStompV017AimMarker) ||
            colossalStompV017AimMarker.IsChildOf(player.transform) ||
            player.transform.IsChildOf(colossalStompV017AimMarker) ||
            (agent && (colossalStompV017AimMarker.IsChildOf(agent.transform) ||
                       agent.transform.IsChildOf(colossalStompV017AimMarker))))
            return RejectV017Stance("V017 aim marker must be independent of Titan, its agent and moving Van.");
        if (!IsFiniteV017WorldPoint(landingGround))
            return RejectV017Stance("Validated V017 landing ground has invalid coordinates.");

        validatedLandingGround = landingGround;
        lastV017StanceRejection = string.Empty;
        return true;
    }

    private static bool IsFiniteV017WorldPoint(Vector3 point)
    {
        return !float.IsNaN(point.x) && !float.IsNaN(point.y) && !float.IsNaN(point.z) &&
               !float.IsInfinity(point.x) && !float.IsInfinity(point.y) && !float.IsInfinity(point.z);
    }

    private bool TryGetV017StancePlan(out Vector3 rootDestination,
        out Quaternion facing, out Vector3 landingGround)
    {
        rootDestination = Vector3.zero;
        facing = Quaternion.identity;
        landingGround = Vector3.zero;
        if (!colossalStompV017StancePreflight ||
            !colossalStompV017StancePreflight.isActiveAndEnabled)
            return RejectV017Stance("Required physical stance preflight is missing or disabled.");

        CharacterController playerBody = player.GetComponent<CharacterController>();
        if (!playerBody || !playerBody.enabled || !playerBody.gameObject.activeInHierarchy)
            return RejectV017Stance("Van has no active physical CharacterController to measure clearance.");

        Vector3 offset = player.transform.position - transform.position;
        float planarDistance = new Vector2(offset.x, offset.z).magnitude;
        if (planarDistance > colossalStompV017MaxPlayerRange)
            return RejectV017Stance("Van is outside the configured V017 approach distance.");

        Vector3 scale = playerBody.transform.lossyScale;
        float worldRadius = playerBody.radius *
            Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        if (!colossalStompV017StancePreflight.TryPlanStance(
                player.transform.position, worldRadius, playerBody,
                out rootDestination, out facing,
                out landingGround, out string reason))
            return RejectV017Stance("Stance planner rejected: " + reason);
        return true;
    }

    // Optional AI-owned navigation into a fixed, physically checked stance.
    // Only the approach moves the root; the V017 clip itself remains planted.
    private bool TryHandleV017StanceApproach(float distanceToPlayer)
    {
        if (enableAttackHierarchy && requireSlamHitForFollowupAnimations &&
            slamFollowupsWaitingForHit)
        {
            // No V017 windup may begin until a REAL Slam/zone hit is confirmed.
            // Ongoing V017 cannot be interrupted by a later Slam because this
            // gate is only set when entering the Slam state from Chasing.
            if (v017StanceApproaching) FinishV017StanceApproach(true);
            return false;
        }
        if (!v017StanceApproaching)
        {
            if (!enableColossalStompV017 || !autoSelectColossalStompV017 ||
                !enableColossalStompV017StanceApproach ||
                !colossalStompV017 || !colossalStompV017.isActiveAndEnabled ||
                colossalStompV017.IsRunning || !player || player.currentHealth <= 0 ||
                Time.time < nextAttackTime || Time.time < nextV017StanceAttemptTime ||
                distanceToPlayer > colossalStompV017MaxPlayerRange)
                return false;

            if (!TryGetV017StancePlan(out v017ApproachRootDestination,
                    out v017ApproachFacing, out Vector3 landing))
                return false;

            v017PreviousStoppingDistance = agent.stoppingDistance;
            agent.stoppingDistance = Mathf.Min(0.3f,
                colossalStompV017RootStanceTolerance * 0.5f);
            agent.isStopped = false;
            if (!agent.SetDestination(v017ApproachRootDestination))
            {
                agent.stoppingDistance = v017PreviousStoppingDistance;
                return RejectV017Stance("NavMeshAgent refused the verified natural stance destination.");
            }
            v017StanceApproaching = true;
            v017ApproachPlayerPosition = player.transform.position;
            v017ApproachLandingGround = landing;
            v017ApproachStartedAt = Time.time;
            v017ApproachSettledAt = -1f;
            nextRepathTime = Time.time + repathSeconds;
        }

        if (!enableColossalStompV017 || !autoSelectColossalStompV017 ||
            !enableColossalStompV017StanceApproach || !player || player.currentHealth <= 0 ||
            !agent || !agent.enabled || !agent.isOnNavMesh ||
            !colossalStompV017 || !colossalStompV017.isActiveAndEnabled ||
            Time.time - v017ApproachStartedAt > colossalStompV017StanceApproachTimeout ||
            Vector2.Distance(new Vector2(player.transform.position.x, player.transform.position.z),
                new Vector2(v017ApproachPlayerPosition.x, v017ApproachPlayerPosition.z)) >
                    colossalStompV017PlayerMotionTolerance)
        {
            FinishV017StanceApproach(true);
            RejectV017Stance("Stance approach interrupted, timed out, or Van moved; replan from live geometry.");
            return false;
        }

        if (!agent.pathPending && agent.pathStatus != NavMeshPathStatus.PathComplete &&
            agent.hasPath)
        {
            FinishV017StanceApproach(true);
            RejectV017Stance("NavMesh approach became partial/invalid.");
            return false;
        }

        transform.rotation = Quaternion.RotateTowards(transform.rotation,
            v017ApproachFacing, turnSpeedDegrees * Time.deltaTime);
        animator.SetBool(WalkingHash, true);
        Vector3 rootOffset = transform.position - v017ApproachRootDestination;
        float rootPlanarError = new Vector2(rootOffset.x, rootOffset.z).magnitude;
        if (enableV017GroundAlignedWalk && rootPlanarError < 2.5f)
        {
            // The giant is ~40x scale. Its original Agent.baseOffset .03
            // raises BOTH native first-frame soles ~1.20 WORLD metres above
            // lower ground when chasing Van. Gradually align ONLY DURING WALK
            // (never while V017 is committed). This is real root movement,
            // not a fake foot target, gravity bypass, teleport, or IK stretch.
            float verticalScale = Mathf.Abs(transform.lossyScale.y);
            if (verticalScale < .001f ||
                !NavMesh.SamplePosition(transform.position, out NavMeshHit navUnderfoot,
                    3f, NavMesh.AllAreas) ||
                Vector2.Distance(new Vector2(navUnderfoot.position.x, navUnderfoot.position.z),
                    new Vector2(transform.position.x, transform.position.z)) > 1.5f)
            {
                FinishV017StanceApproach(true);
                return RejectV017Stance("No supporting NavMesh ground for continuous pre-stomp root-height alignment.");
            }
            float groundBias = hammerfistStompPursuitPending ?
                hammerStompGroundingBiasWorld : 0f;
            float desiredBaseOffset = Mathf.Clamp(
                (v017ApproachLandingGround.y + groundBias - navUnderfoot.position.y) /
                    verticalScale, -.01f, .06f);
            agent.baseOffset = Mathf.MoveTowards(agent.baseOffset,
                desiredBaseOffset, v017GroundAlignWorldSpeed * Time.deltaTime / verticalScale);
            v017GroundAlignmentStatus = "REAL HeavyWalk grounding, currentRootY=" +
                transform.position.y.ToString("F3") +
                " actualFootFloorY=" + v017ApproachLandingGround.y.ToString("F3") +
                " navBaseOffset=" + agent.baseOffset.ToString("F5");
        }
        bool positioned = !agent.pathPending &&
            rootPlanarError <= colossalStompV017RootStanceTolerance &&
            agent.velocity.sqrMagnitude <= 0.25f;
        bool faced = Quaternion.Angle(transform.rotation,
            v017ApproachFacing) <= colossalStompV017FacingToleranceDegrees;

        if (!positioned || !faced)
        {
            v017ApproachSettledAt = -1f;
            if (agent.isStopped && !positioned) agent.isStopped = false;
            return true;
        }

        if (enableV017GroundAlignedWalk &&
            Mathf.Abs(transform.position.y - v017ApproachLandingGround.y) > .30f)
        {
            // The native f1 sole calibration below independently enforces
            // <= .35m for EACH rendered left and right foot. A successful
            // navigation turn is not permission to attack with floating soles.
            v017ApproachSettledAt = -1f;
            return true;
        }

        // Freeze both navigation and yaw during the settle window. Subsequent
        // Begin re-runs the complete, real-world physics/NavMesh preflight.
        agent.isStopped = true;
        if (v017ApproachSettledAt < 0f)
            v017ApproachSettledAt = Time.time;
        if (Time.time - v017ApproachSettledAt < colossalStompV017SettleSeconds ||
            animator.IsInTransition(0) ||
            animator.GetCurrentAnimatorStateInfo(0).fullPathHash != HeavyWalkStateHash)
            return true;

        if (BeginColossalStompV017())
        {
            FinishV017StanceApproach(false);
            return true;
        }

        FinishV017StanceApproach(true);
        return false;
    }

    private void FinishV017StanceApproach(bool resumeNavigation)
    {
        if (!v017StanceApproaching) return;
        v017StanceApproaching = false;
        v017ApproachSettledAt = -1f;
        if (agent && agent.enabled && agent.isOnNavMesh)
        {
            agent.stoppingDistance = v017PreviousStoppingDistance;
            if (resumeNavigation)
            {
                agent.ResetPath();
                agent.isStopped = false;
                nextRepathTime = 0f;
            }
        }
    }

    private bool RejectV017Stance(string reason)
    {
        lastV017StanceRejection = reason;
        // Auto-selection may run every frame; avoid redoing a NavMesh path
        // calculation every frame while awaiting a legal natural stance.
        nextV017StanceAttemptTime = Time.time + 0.3f;
        if (Time.time >= nextV017StanceWarningTime)
        {
            nextV017StanceWarningTime = Time.time + 5f;
            Debug.LogWarning("[Titan V017] Attack denied: " + reason + " No position/target shift or gameplay impact.", this);
        }
        return false;
    }

    private void CompleteAwakening()
    {
        if (awakened) return;
        awakened = true;
        state = TitanState.AwakenedHold;
        animator.SetBool(WalkingHash, false);
        // Reveal on the first frame the Animator has actually entered
        // AwakenedHold, before chase/combat AI tries to start.
        EnemyEncounterHUD.ShowFor(health);
        nextHealthBarCheck = Time.time + 0.5f;
        onAwakeningFinished?.Invoke();
        Debug.Log("[Titan] Awakening complete, now AwakenedHoldV004; no premature attack.", this);
        if (autoBeginCombatAfterAwakening) StartBattle();
    }

    private void BeginHammerfist()
    {
        if (state != TitanState.Chasing || !battleStarted || !awakened ||
            (enableAttackHierarchy && requireSlamHitForFollowupAnimations &&
             slamFollowupsWaitingForHit)) return;
        state = TitanState.Hammerfist;
        if (enableAttackHierarchy && enableVerifiedSlamRangeFollowup &&
            verifiedSlamFollowupPending)
            CompleteVerifiedSlamFollowup("Hammerfist animation started at genuine fist range");
        if (enableAttackHierarchy && enableSlamAntiRepeat)
            ClearSlamRepeatLock("a different authored Hammerfist attack started");
        StopNavigation();
        animator.SetBool(WalkingHash, false);
        animator.ResetTrigger(HammerHash);
        if (enableEarthbreakerSlamAI) animator.ResetTrigger(SlamHash);
        hammerImpactReceived = false;
        hammerStateObserved = false;
        lastHammerfistGroundFound = false;
        lastHammerfistPlayerContact = false;
        lastHammerfistDamageZone = 0;
        lastHammerfistAppliedDamage = 0f;
        lastHammerfistColliderPlanarDistance = -1f;
        hammerStartedAt = Time.time;
        if (enableEarthbreakerSlamAI) lastAttackWasHammerfist = true;
        nextAttackTime = Time.time + attackCooldown;
        committedHammerfistPoint = transform.TransformPoint(new Vector3(
            hammerfistContactLocalXZ.x, 0f, hammerfistContactLocalXZ.y));
        animator.SetTrigger(HammerHash);
    }

    private void UpdateHammerfist()
    {
        var current = animator.GetCurrentAnimatorStateInfo(0);
        if (current.fullPathHash == HammerStateHash) hammerStateObserved = true;
        if ((hammerStateObserved && !animator.IsInTransition(0) && current.fullPathHash == ReadyHash)
            || Time.time - hammerStartedAt > attackWatchdogSeconds)
        {
            // Watchdog NEVER deals damage. Only the authored impact event can.
            animator.ResetTrigger(HammerHash);
            state = TitanState.Chasing;
            if (enableTacticalDecisionReview)
                nextAttackTime = Mathf.Max(nextAttackTime,
                    Time.time + postHammerfistAttackDelay);
            if (agent && agent.enabled && agent.isOnNavMesh) agent.isStopped = false;
            EnterHeavyWalk();
        }
    }

    private void BeginEarthbreakerSlam()
    {
        if (!enableEarthbreakerSlamAI || !battleStarted || !awakened ||
            state != TitanState.Chasing || !animator.HasState(0, SlamStateHash) ||
            (enableAttackHierarchy && enableVerifiedSlamRangeFollowup &&
             verifiedSlamFollowupPending) ||
            (enableAttackHierarchy && enableSlamAntiRepeat && slamAntiRepeatLocked))
            return;
        state = TitanState.EarthbreakerSlam;
        if (enableAttackHierarchy && requireSlamHitForFollowupAnimations)
        {
            slamFollowupsWaitingForHit = true;
            lastSlamVerifiedHit = false;
            slamHitCheckerStatus = "Pending authentic frame-71 Slam and zone collider checks";
            Debug.Log("[TITAN SLAM HIT CHECKER] Armed at Slam startup. " +
                "Other attack animations locked until actual physics hit; HP/CC unchanged.", this);
        }
        if (enableAttackHierarchy && enableSlamAntiRepeat)
        {
            slamAntiRepeatLocked = true;
            slamAntiRepeatOrigin = transform.position;
            slamAntiRepeatStatus = "Locked after Slam; requires other attack or " +
                slamAntiRepeatMoveMeters.ToString("F1") + "m real walking";
            Debug.Log("[TITAN ANTI-REPEAT] Earthbreaker locked at " +
                slamAntiRepeatOrigin.ToString("F2") +
                ": next Slam requires a different attack or >=" +
                slamAntiRepeatMoveMeters.ToString("F1") + "m real Titan NavMesh movement.", this);
        }
        StopNavigation();
        animator.SetBool(WalkingHash, false);
        animator.ResetTrigger(HammerHash);
        animator.ResetTrigger(SlamHash);
        slamStateObserved = false;
        slamImpactReceived = false;
        lastEarthbreakerGroundFound = false;
        lastEarthbreakerPlayerContact = false;
        lastEarthbreakerCoverBlocked = false;
        lastEarthbreakerJumpAvoided = false;
        slamStartedAt = Time.time;
        earthbreakerSlamsStarted++;
        if (enableProgressiveEarthbreakerZones && progressiveEarthquake)
            progressiveEarthquake.ArmPreview(this);
        // Freeze rotation/navigation throughout the wind-up and impact. The
        // animation never magically follows Van after commitment.
        // A direct authored-state crossfade avoids a Walk->Hold transition
        // swallowing a trigger while Walking flips false in the same frame.
        animator.CrossFadeInFixedTime(SlamStateHash, .1f, 0, 0f);
        Debug.Log("[Titan] Earthbreaker Slam V003 committed. Telegraph is the authored two-fist wind-up.", this);
    }

    private void UpdateEarthbreakerSlam()
    {
        var current = animator.GetCurrentAnimatorStateInfo(0);
        if (current.fullPathHash == SlamStateHash)
            slamStateObserved = true;
        bool earthquakeStillActive = enableProgressiveEarthbreakerZones &&
            progressiveEarthquake && progressiveEarthquake.IsSequenceActive;
        float watchdog = Mathf.Max(slamWatchdogSeconds,
            enableProgressiveEarthbreakerZones && progressiveEarthquake
                ? 4f + progressiveEarthquake.PlannedDurationSeconds
                : slamWatchdogSeconds);
        if ((slamStateObserved && !animator.IsInTransition(0) &&
             current.fullPathHash == ReadyHash && !earthquakeStillActive) ||
            Time.time - slamStartedAt > watchdog)
        {
            if (enableAttackHierarchy && requireSlamHitForFollowupAnimations &&
                !lastSlamVerifiedHit)
            {
                slamHitCheckerStatus = "MISS: Slam ended without verified player contact; reposition/retry Slam";
                Debug.Log("[TITAN SLAM HIT CHECKER] MISS: original Slam recovered without " +
                    "a confirmed frame-71/earthquake-zone player hit. " +
                    "Other animations remain locked; retry requires real positioning/cooldown.", this);
            }
            // A missing animation event must NEVER manufacture impact damage.
            if (progressiveEarthquake) progressiveEarthquake.StopSequence();
            animator.ResetTrigger(SlamHash);
            state = TitanState.Chasing;
            lastAttackWasHammerfist = false;
            nextAttackTime = Mathf.Max(nextAttackTime,
                Time.time + slamRecoverySeconds);
            // A newly unlocked attack cannot be selected just because time
            // advanced. Refresh actual NavMesh path for the locked walk phase.
            if (enableAttackHierarchy && enableSlamAntiRepeat)
                nextRepathTime = 0f;
            if (agent && agent.enabled && agent.isOnNavMesh)
                agent.isStopped = false;
            EnterHeavyWalk();
        }
    }

    /// <summary>
    /// Invoked solely by TitanEarthbreakerSlamEventRelay, from the approved
    /// V003 clip's frame-71 impact. Not the Hammerfist frame-134 event.
    /// </summary>
    public void ResolveEarthbreakerSlamImpact()
    {
        if (!enableEarthbreakerSlamAI || !battleStarted || !awakened ||
            defeatReported || state != TitanState.EarthbreakerSlam ||
            slamImpactReceived)
            return;
        slamImpactReceived = true;
        earthbreakerImpacts++;
        lastEarthbreakerGroundFound = TryResolveEarthbreakerSlamGround(out var point);
        lastEarthbreakerGroundPoint = point;
        lastEarthbreakerPlayerContact = false;
        lastEarthbreakerCoverBlocked = false;
        lastEarthbreakerJumpAvoided = false;
        if (!lastEarthbreakerGroundFound || !ResolvePlayer() ||
            player.currentHealth <= 0)
        {
            if (enableProgressiveEarthbreakerZones && progressiveEarthquake)
                progressiveEarthquake.StopSequence();
            Debug.LogWarning("[Titan] Earthbreaker impact has no valid ground/player; no hit.", this);
            return;
        }
        if (enableProgressiveEarthbreakerZones)
        {
            // Exactly ONE authored event starts the time-ordered fixed zones.
            // The zone component is the sole owner of per-zone HP/cover/jump.
            // Never additionally apply V013's immediate capsule hit.
            if (!progressiveEarthquake ||
                !progressiveEarthquake.BeginAtImpact(this, player, point,
                    enableSlamCombatDamage))
                Debug.LogWarning("[Titan] Progressive Earthbreaker zones did not arm; no fallback damage.", this);
            return;
        }

        // The shockwave forms a capsule across BOTH fists; a circular
        // midpoint zone would omit their actual landing spots.
        Vector3 segment = lastEarthbreakerRightGround - lastEarthbreakerLeftGround;
        Vector2 horizontal = new Vector2(segment.x, segment.z);
        Vector3 playerPosition = player.transform.position;
        Vector2 playerDelta = new Vector2(playerPosition.x - lastEarthbreakerLeftGround.x,
            playerPosition.z - lastEarthbreakerLeftGround.z);
        float portion = horizontal.sqrMagnitude > .001f
            ? Mathf.Clamp01(Vector2.Dot(playerDelta, horizontal) / horizontal.sqrMagnitude)
            : 0f;
        Vector3 nearGround = Vector3.Lerp(lastEarthbreakerLeftGround,
            lastEarthbreakerRightGround, portion);
        Vector3 offset = playerPosition - nearGround;
        float planarDistance = new Vector2(offset.x, offset.z).magnitude;
        float leftDistance = Vector2.Distance(new Vector2(playerPosition.x, playerPosition.z),
            new Vector2(lastEarthbreakerLeftGround.x, lastEarthbreakerLeftGround.z));
        float rightDistance = Vector2.Distance(new Vector2(playerPosition.x, playerPosition.z),
            new Vector2(lastEarthbreakerRightGround.x, lastEarthbreakerRightGround.z));
        bool directFistHit = Mathf.Min(leftDistance, rightDistance) <= slamDirectImpactRadius;
        if (Mathf.Abs(offset.y) <= slamVerticalTolerance &&
            planarDistance <= slamImpactRadius)
        {
            var overlaps = Physics.OverlapCapsule(lastEarthbreakerLeftGround,
                lastEarthbreakerRightGround, slamImpactRadius,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            foreach (var hit in overlaps)
            {
                if (!hit || hit.GetComponentInParent<PlayerHealth>() != player)
                    continue;
                if (respectSolidCover &&
                    HammerfistHasBlockingCover(nearGround, player.transform.position))
                    lastEarthbreakerCoverBlocked = true;
                else
                {
                    var body = player.GetComponent<CharacterController>();
                    if (body && !body.isGrounded &&
                        !directFistHit &&
                        offset.y >= slamJumpClearance)
                        lastEarthbreakerJumpAvoided = true;
                    else
                        lastEarthbreakerPlayerContact = true;
                }
                break;
            }
        }
        Debug.Log($"[Titan] Earthbreaker Slam V003 frame 71: left={lastEarthbreakerLeftGround}, right={lastEarthbreakerRightGround}, " +
            $"radius={slamImpactRadius:0.0}, distance={planarDistance:0.0}, " +
            $"hit={lastEarthbreakerPlayerContact}, covered={lastEarthbreakerCoverBlocked}, " +
            $"jumpAvoid={lastEarthbreakerJumpAvoided}, HPdamage={enableSlamCombatDamage}.", this);
        if (lastEarthbreakerPlayerContact)
            ConfirmSlamPhysicsHit("original frame-71 two-fist collider/contact check");
        if (!lastEarthbreakerPlayerContact || player.currentHealth <= 0)
            return;
        if (enableSlamCombatDamage)
            player.TakeDamage(slamDamage);
        if (enableSlamCrowdControl && player.currentHealth > 0)
        {
            var movement = player.GetComponent<MovementofPlayer>();
            if (movement)
                movement.ApplySpeedDebuff(slamSlowMultiplier, slamSlowSeconds);
        }
    }

    /// <summary>
    /// A progressive earthquake zone calls this only AFTER it has actually
    /// overlapped the live PlayerHealth collider in its authored timed zone,
    /// passed vertical bounds, and rejected cover/jump avoidance. No HP/CC
    /// is needed for hit confirmation. A missing frame-71 relay or a different
    /// source cannot open the follow-up gate.
    /// </summary>
    public void ReportEarthbreakerZonePhysicsHit(
        TitanEarthbreakerProgressiveWaves source, PlayerHealth victim,
        int cycle, int zone)
    {
        if (!enableAttackHierarchy || !requireSlamHitForFollowupAnimations ||
            !enableProgressiveEarthbreakerZones ||
            state != TitanState.EarthbreakerSlam || !battleStarted || !awakened ||
            defeatReported || !slamImpactReceived || !lastEarthbreakerGroundFound ||
            !source || source != progressiveEarthquake || !source.IsSequenceActive ||
            !source.LastPlayerContact || source.CurrentCycle != cycle ||
            source.LastZoneFired != zone || !victim || victim != player ||
            victim.currentHealth <= 0)
            return;
        lastEarthbreakerPlayerContact = true;
        ConfirmSlamPhysicsHit("progressive earthquake cycle=" + cycle + " zone=" + zone);
    }

    private void ConfirmSlamPhysicsHit(string verifiedSource)
    {
        if (!enableAttackHierarchy || !requireSlamHitForFollowupAnimations ||
            !slamFollowupsWaitingForHit || lastSlamVerifiedHit ||
            state != TitanState.EarthbreakerSlam || !slamImpactReceived ||
            !lastEarthbreakerGroundFound || !player || player.currentHealth <= 0)
            return;
        lastSlamVerifiedHit = true;
        slamFollowupsWaitingForHit = false;
        slamVerifiedHits++;
        if (enableVerifiedSlamRangeFollowup)
        {
            verifiedSlamFollowupPending = true;
            verifiedSlamFollowupStatus = "Verified hit: wait for FULL Slam recovery before range decision";
            Debug.Log("[TITAN SLAM FOLLOWUP] Authentic Slam HIT: one follow-up earned. " +
                "Finish authored recovery, then execute an eligible move if Van is in range; " +
                "otherwise approach to real foot/fist stance. No second Slam until follow-up starts.", this);
        }
        slamHitCheckerStatus = "HIT: follow-up attacks eligible after recovery, source=" + verifiedSource;
        Debug.Log("[TITAN SLAM HIT CHECKER] HIT verified by " + verifiedSource +
            ". Hammerfist/V017 follow-ups UNLOCKED after authored recovery; " +
            "normal cooldown, NavMesh and contact safety still required; " +
            "HPdamage=" + enableSlamCombatDamage + ".", this);
    }

    private void CompleteVerifiedSlamFollowup(string actualAttack)
    {
        if (!verifiedSlamFollowupPending) return;
        verifiedSlamFollowupPending = false;
        verifiedSlamFollowupStatus = "Completed: " + actualAttack;
        Debug.Log("[TITAN SLAM FOLLOWUP] COMMITTED " + actualAttack +
            ". Normal attack hierarchy resumes after full authored recovery.", this);
    }

    private bool TryResolveEarthbreakerSlamGround(out Vector3 point)
    {
        point = Vector3.zero;
        if (!slamLeftHand || !slamRightHand)
        {
            foreach (var bone in GetComponentsInChildren<Transform>(true))
            {
                if (bone.name == "mixamorig:LeftHand")
                    slamLeftHand = bone;
                else if (bone.name == "mixamorig:RightHand")
                    slamRightHand = bone;
            }
        }
        if (!slamLeftHand || !slamRightHand ||
            !slamLeftHand.IsChildOf(transform) || !slamRightHand.IsChildOf(transform))
            return false;
        if (!TryFindSlamFloor(slamLeftHand.position, out var left) ||
            !TryFindSlamFloor(slamRightHand.position, out var right) ||
            Mathf.Abs(left.y - right.y) > slamMaxGroundHeightDifference)
            return false;
        lastEarthbreakerLeftGround = left;
        lastEarthbreakerRightGround = right;
        point = (left + right) * .5f;
        return true;
    }

    private bool TryFindSlamFloor(Vector3 hand, out Vector3 point)
    {
        point = Vector3.zero;
        var hits = Physics.RaycastAll(hand + Vector3.up * slamFloorProbeLift,
            Vector3.down, slamFloorProbeLift + slamFloorMaxClearance,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        float bestY = float.NegativeInfinity;
        foreach (var hit in hits)
        {
            if (!hit.collider || hit.collider.transform.IsChildOf(transform)
                || hit.collider.GetComponentInParent<PlayerHealth>() != null
                || hit.normal.y < .6f)
                continue;
            float clearance = hand.y - hit.point.y;
            if (clearance < -.25f || clearance > slamFloorMaxClearance ||
                hit.point.y <= bestY)
                continue;
            point = hit.point;
            bestY = hit.point.y;
        }
        return bestY > float.NegativeInfinity;
    }

    /// <summary>
    /// Authored exactly once at Hammerfist V003 frame 134 (4.433333 s).
    /// </summary>
    public static int HammerfistDamageZone(float planarColliderDistance,
        float innerRadius, float middleRadius, float outerRadius)
    {
        if (!float.IsFinite(planarColliderDistance) || planarColliderDistance < 0f ||
            !float.IsFinite(innerRadius) || !float.IsFinite(middleRadius) ||
            !float.IsFinite(outerRadius) || innerRadius <= 0f ||
            middleRadius <= innerRadius || outerRadius <= middleRadius)
            return 0;
        if (planarColliderDistance <= innerRadius) return 1;
        if (planarColliderDistance <= middleRadius) return 2;
        return planarColliderDistance <= outerRadius ? 3 : 0;
    }

    public static float HammerfistZoneDamage(int zone, float inner,
        float middle, float outer)
    {
        if (!float.IsFinite(inner) || !float.IsFinite(middle) ||
            !float.IsFinite(outer) || inner < 0f || middle < 0f || outer < 0f)
            return 0f;
        return zone == 1 ? inner : zone == 2 ? middle : zone == 3 ? outer : 0f;
    }

    public void OnHammerfistImpact()
    {
        if (state != TitanState.Hammerfist || !battleStarted || hammerImpactReceived
            || !awakened || defeatReported)
            return;
        hammerImpactReceived = true;
        hammerImpactCount++;
        // This UnityEvent is cosmetic-only. The controller is the sole owner
        // of Hammerfist HP damage, and only the authored frame-134 event can hit.
        onHammerfistImpact?.Invoke();
        lastHammerfistGroundFound = TryResolveHammerfistGroundPoint(out var impactPoint);
        lastHammerfistGroundPoint = impactPoint;
        lastHammerfistPlayerContact = false;
        lastHammerfistBlockedByCover = false;
        lastHammerfistJumpAvoided = false;
        lastHammerfistPlayerPlanarDistance = -1f;
        lastHammerfistColliderPlanarDistance = -1f;
        lastHammerfistDamageZone = 0;
        lastHammerfistAppliedDamage = 0f;
        if (!lastHammerfistGroundFound)
        {
            Debug.LogWarning("[Titan] Hammerfist frame 134: no walkable surface under the animated right fist; no hit.", this);
            return;
        }
        if (player && player.currentHealth > 0)
        {
            Vector3 toPlayer = player.transform.position - impactPoint;
            lastHammerfistPlayerPlanarDistance = new Vector2(toPlayer.x, toPlayer.z).magnitude;
            if (Mathf.Abs(toPlayer.y) <= impactVerticalTolerance)
            {
                // Require contact with Van's actual physics collider, not
                // proximity to Titan's root, which is ~42m behind his fist.
                var overlaps = Physics.OverlapSphere(impactPoint, impactRadius,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                foreach (var hit in overlaps)
                {
                    if (hit && hit.GetComponentInParent<PlayerHealth>() == player)
                    {
                        if (respectSolidCover && HammerfistHasBlockingCover(impactPoint, player.transform.position))
                            lastHammerfistBlockedByCover = true;
                        else if (allowJumpAvoidOuterImpact &&
                            player.GetComponent<CharacterController>() != null &&
                            !player.GetComponent<CharacterController>().isGrounded &&
                            lastHammerfistPlayerPlanarDistance > directImpactRadius &&
                            player.transform.position.y - impactPoint.y >= outerImpactJumpClearance)
                            lastHammerfistJumpAvoided = true;
                        else
                        {
                            lastHammerfistPlayerContact = true;
                            // The real Van CC has a ~5m world-space radius.
                            // Classify its NEAREST PHYSICAL COLLIDER SURFACE,
                            // not its root center (which would misclassify
                            // hits that merely graze the outer shock ring).
                            Vector3 closest = hit.ClosestPoint(impactPoint);
                            Vector2 nearestXZ = new Vector2(
                                closest.x - impactPoint.x,
                                closest.z - impactPoint.z);
                            lastHammerfistColliderPlanarDistance = nearestXZ.magnitude;
                            if (enableHammerfistThreeZoneDamage)
                                lastHammerfistDamageZone = HammerfistDamageZone(
                                    lastHammerfistColliderPlanarDistance,
                                    hammerInnerRadius, hammerMiddleRadius, impactRadius);
                        }
                        break;
                    }
                }
            }
        }
        Debug.Log($"[Titan] Hammerfist frame 134: ground={impactPoint}, playerPlanar={lastHammerfistPlayerPlanarDistance:0.0}m, physicsContact={lastHammerfistPlayerContact}, coverBlocked={lastHammerfistBlockedByCover}, jumpAvoided={lastHammerfistJumpAvoided}, damageEnabled={enableCombatDamage}.", this);
        if (enableCombatDamage && lastHammerfistPlayerContact && player && player.currentHealth > 0)
        {
            float awarded = enableHammerfistThreeZoneDamage
                ? HammerfistZoneDamage(lastHammerfistDamageZone,
                    hammerInnerDamage, hammerMiddleDamage, hammerOuterDamage)
                : impactDamage;
            if (awarded > 0f)
            {
                player.TakeDamage(awarded);
                lastHammerfistAppliedDamage = awarded;
                playerHits++;
            }
        }
        Debug.Log("[TITAN HAMMER ZONES] " +
            (lastHammerfistPlayerContact ? "HIT" : "MISS/BLOCKED/DODGED") +
            " zone=" + lastHammerfistDamageZone + "/3" +
            " physicalEdgeDistance=" + lastHammerfistColliderPlanarDistance.ToString("F2") +
            "m rings=0.." + hammerInnerRadius.ToString("F1") +
            "/" + hammerMiddleRadius.ToString("F1") +
            "/" + impactRadius.ToString("F1") +
            "m appliedHP=" + lastHammerfistAppliedDamage.ToString("F1") +
            " cover=" + lastHammerfistBlockedByCover +
            " jumpAvoid=" + lastHammerfistJumpAvoided +
            ". No additional damage from the launch, slow or stomp pursuit.", this);
        if (enableHammerfistCrowdControl && lastHammerfistPlayerContact
            && player && player.currentHealth > 0)
        {
            var movement = player.GetComponent<MovementofPlayer>();
            if (!movement)
                movement = player.GetComponentInParent<MovementofPlayer>();
            if (movement)
            {
                var crowdControl = movement.GetComponent<TitanHammerfistCrowdControl>();
                if (!crowdControl)
                    crowdControl = movement.gameObject.AddComponent<TitanHammerfistCrowdControl>();
                if (!crowdControl.enabled && !crowdControl.IsActive)
                    crowdControl.enabled = true;
                bool launched = crowdControl.Begin(movement, player,
                    hammerfistLaunchSpeed, hammerfistStunSeconds,
                    hammerfistSlowMultiplier, hammerfistSlowSeconds);
                if (launched && enableAttackHierarchy &&
                    enableHammerfistStompPursuit &&
                    enableColossalStompV017 && autoSelectColossalStompV017 &&
                    enableColossalStompV017StanceApproach)
                {
                    hammerfistStompPursuitPending = true;
                    hammerfistStompVictimMotion = crowdControl;
                    hammerfistStompPursuitDeadline = Time.time +
                        hammerfistStompPursuitMaxSeconds;
                    hammerfistStompPursuitStatus =
                        "Authentic fist HIT: waiting for actual landing, then NavMesh V017 pursuit";
                    Debug.Log("[TITAN HAMMER->STOMP] AUTHENTIC Hammerfist HIT " +
                        "confirmed: Van launched with input locked until ground. " +
                        "After normal Hammerfist recovery, prioritize original " +
                        "four-stomp approach. Player slow expires naturally after " +
                        hammerfistSlowSeconds.ToString("F1") + "s on landing; " +
                        "Titan may attempt real safe stance for up to " +
                        hammerfistStompPursuitMaxSeconds.ToString("F1") +
                        "s after hit (no extra CC).", this);
                }
            }
        }
    }

    private bool HammerfistHasBlockingCover(Vector3 impactPoint, Vector3 playerPosition)
    {
        // Sample the low torso above the terrain; discard floor contacts,
        // triggers, the attacking Titan and the target player. Solid scene
        // geometry between the fist and Van blocks BOTH HP and CC.
        // Lift the test above uneven terrain. Otherwise a ground mesh can
        // report an almost-zero-distance hit and count as a wall.
        Vector3 origin = impactPoint + Vector3.up * 3f;
        Vector3 destination = playerPosition + Vector3.up * 3f;
        Vector3 direction = destination - origin;
        float distance = direction.magnitude;
        if (distance <= 0.1f)
            return false;
        var hits = Physics.RaycastAll(origin, direction / distance,
            distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        foreach (var hit in hits)
        {
            if (!hit.collider || hit.collider.transform.IsChildOf(transform)
                || hit.collider.GetComponentInParent<PlayerHealth>() == player)
                continue;
            if (hit.distance <= 0.2f)
                continue; // Starting on the fist-impact terrain is not cover.
            // A terrain ray grazing the floor is not protective cover.
            if (hit.normal.y > 0.8f && hit.point.y <= impactPoint.y + 0.2f)
                continue;
            return true;
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (showV017StompRangeGizmos && enableV017VerifiedStompDamage &&
            (state == TitanState.ColossalStompV017 || v017StanceApproaching ||
             hammerfistStompPursuitPending))
        {
#if UNITY_EDITOR
            Vector3 footCenter = state == TitanState.ColossalStompV017 &&
                stompLastImpactIndex > 0 ? stompLastCertifiedFootGround :
                colossalStompV017AimMarker ? colossalStompV017AimMarker.position :
                colossalStompCommittedAimPosition;
            if (IsFiniteV017WorldPoint(footCenter))
            {
                footCenter.y += .07f;
                DrawHammerDamageDisc(footCenter, stompOuterRadius,
                    new Color(.9f, .78f, .1f, 1f), stompOuterDamage, "STOMP OUTER");
                DrawHammerDamageDisc(footCenter, stompMiddleRadius,
                    new Color(1f, .38f, .1f, 1f), stompMiddleDamage, "STOMP MID");
                DrawHammerDamageDisc(footCenter, stompCoreRadius,
                    new Color(1f, .06f, .08f, 1f), stompCoreDamage, "STOMP CORE");
                UnityEditor.Handles.Label(footCenter + Vector3.up * 1.2f,
                    stompLastImpactIndex > 0 ? "ACTUAL LBS STOMP #" + stompLastImpactIndex :
                    "PREDICTED STOMP FOOT LANDING (NOT A HIT)");
            }
#endif
        }
        if (!drawHammerfistTelegraphGizmo)
            return;
        // Before a swing, predict from the authored sampled local hand offset.
        // After frame134 ground raycast, show the ACTUAL grounded impact.
        Vector3 center = lastHammerfistGroundFound ? lastHammerfistGroundPoint
            : Application.isPlaying && state == TitanState.Hammerfist
                ? committedHammerfistPoint
                : transform.TransformPoint(new Vector3(
                    hammerfistContactLocalXZ.x, 0f, hammerfistContactLocalXZ.y));
        center.y += .045f;
        if (enableHammerfistThreeZoneDamage)
        {
#if UNITY_EDITOR
            // Exactly THREE visibly distinct concentric ground-plane rings.
            // Gizmos only, never render-side collision or gameplay damage.
            DrawHammerDamageDisc(center, impactRadius,
                new Color(1f, .72f, .09f, 1f), hammerOuterDamage, "OUTER");
            DrawHammerDamageDisc(center, hammerMiddleRadius,
                new Color(1f, .36f, .06f, 1f), hammerMiddleDamage, "MID");
            DrawHammerDamageDisc(center, hammerInnerRadius,
                new Color(1f, .05f, .05f, 1f), hammerInnerDamage, "CORE");
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.Label(center + Vector3.up * 1.1f,
                lastHammerfistGroundFound ? "HAMMERFIST: ACTUAL RIGHT-FIST IMPACT"
                    : "HAMMERFIST: PREDICTED RIGHT-FIST IMPACT");
#endif
        }
        else
        {
            Gizmos.color = new Color(1f, .65f, .05f, .75f);
            Gizmos.DrawWireSphere(center, Mathf.Max(.1f, impactRadius));
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(center, Mathf.Max(.1f, directImpactRadius));
        }
        if (lastEarthbreakerGroundFound && !enableProgressiveEarthbreakerZones)
        {
            Gizmos.color = new Color(.8f, .25f, .05f, .85f);
            Gizmos.DrawWireSphere(lastEarthbreakerLeftGround, slamImpactRadius);
            Gizmos.DrawWireSphere(lastEarthbreakerRightGround, slamImpactRadius);
            Gizmos.DrawLine(lastEarthbreakerLeftGround, lastEarthbreakerRightGround);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(lastEarthbreakerLeftGround, slamDirectImpactRadius);
            Gizmos.DrawWireSphere(lastEarthbreakerRightGround, slamDirectImpactRadius);
        }
    }

#if UNITY_EDITOR
    private static void DrawHammerDamageDisc(Vector3 center, float radius,
        Color color, float damage, string name)
    {
        if (!float.IsFinite(radius) || radius <= 0f) return;
        UnityEditor.Handles.color = new Color(color.r, color.g, color.b, .07f);
        UnityEditor.Handles.DrawSolidDisc(center, Vector3.up, radius);
        UnityEditor.Handles.color = color;
        UnityEditor.Handles.DrawWireDisc(center, Vector3.up, radius);
        UnityEditor.Handles.Label(center + Vector3.right * radius + Vector3.up * .12f,
            name + "  " + damage.ToString("0.#") + " HP  (" + radius.ToString("0.#") + "m)");
    }
#endif

    private bool TryResolveHammerfistGroundPoint(out Vector3 point)
    {
        point = Vector3.zero;
        if (!hammerfistImpactHand)
        {
            foreach (var bone in GetComponentsInChildren<Transform>(true))
            {
                if (bone.name == "mixamorig:RightHand")
                {
                    hammerfistImpactHand = bone;
                    break;
                }
            }
        }
        if (!hammerfistImpactHand || !hammerfistImpactHand.IsChildOf(transform))
            return false; // Fail closed if the approved rig changes.

        Vector3 hand = hammerfistImpactHand.position;
        var hits = Physics.RaycastAll(hand + Vector3.up * impactGroundProbeLift,
            Vector3.down, impactGroundProbeLift + impactMaxGroundClearance,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        float bestY = float.NegativeInfinity;
        foreach (var hit in hits)
        {
            if (!hit.collider || hit.collider.transform.IsChildOf(transform)
                || hit.collider.GetComponentInParent<PlayerHealth>() != null
                || hit.normal.y < 0.6f)
                continue;
            float clearance = hand.y - hit.point.y;
            if (clearance < -0.25f || clearance > impactMaxGroundClearance
                || hit.point.y <= bestY)
                continue;
            point = hit.point;
            bestY = hit.point.y;
        }
        return bestY > float.NegativeInfinity;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Play Mode ONLY. The user can focus Game view and press plain T/H to
    /// test the real Titan BeingHit/death lifecycle. Never compiled into a
    /// player build, serialized into a scene, or treated as a real spell hit.
    /// Uses EnemyHealth.TakeDamage so HealthChanged/Died and all production
    /// reaction/death callbacks remain the sole authoritative route.
    /// </summary>
    private void HandleStage3BeingHitDeathDebugKeys()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path !=
            "Assets/Scenes/Stage3Scene/Stage3Scene.unity") return;

        // Ignore combinations used by existing Unity Editor QA menu items.
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
            Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) return;

        bool testDeath = Input.GetKeyDown(KeyCode.H);
        bool testHit = Input.GetKeyDown(KeyCode.T);
        if (!testDeath && !testHit) return;

        if (!battleStarted || !awakened || state == TitanState.Dormant ||
            state == TitanState.Awakening || state == TitanState.Dead ||
            !health || health.IsDead || health.IsInvulnerable)
        {
            Debug.LogWarning("[TITAN T/H QA] " + (testDeath ? "H" : "T") +
                " ignored: Titan must be awake, alive and vulnerable in genuine Stage3 combat. " +
                "No invulnerability/health gate bypass.", this);
            return;
        }

        if (testDeath)
        {
            float hp = health.currentHealth;
            if (hp <= 0f) return;
            Debug.Log("[TITAN T/H QA] H: zeroing genuine Titan EnemyHealth " +
                hp.ToString("F1") + " HP. Expect authored V015 death + single Node4 completion. " +
                "EDITOR PLAY ONLY, not a player spell.", this);
            health.TakeDamage(hp);
            return;
        }

        // Repeated T taps are safely nonlethal: H alone tests death. A hit
        // during an authored attack queues the BeingHit upper-body recoil
        // until the base-layer attack/IK has fully recovered.
        float damage = Mathf.Min(12f, Mathf.Max(0f, health.currentHealth - 1f));
        if (damage <= 0f)
        {
            Debug.Log("[TITAN T/H QA] T: Titan already at 1 HP; use H for death.", this);
            return;
        }
        Debug.Log("[TITAN T/H QA] T: authentic EnemyHealth.TakeDamage(" +
            damage.ToString("F1") + "), remaining=" +
            (health.currentHealth - damage).ToString("F1") +
            " HP. Expect Titan BeingHit masked recoil now or after authored attack recovery. " +
            "EDITOR PLAY ONLY, not a player spell.", this);
        health.TakeDamage(damage);
    }
#endif

    // Reuse the GOLEM'S LAYERED architecture, never its incompatible bone-
    // bound Golem clips. EnemyHealth already invokes TriggerBeingHit ONCE per
    // real nonlethal TakeDamage. No direct HP or spell-system modifications.
    public void TriggerBeingHit()
    {
        if (!enableBeingHitOverlay || !battleStarted || !awakened ||
            state == TitanState.Dead || !health || health.IsDead) return;
        if (Time.time < nextBeingHitTime) return;

        // Do not interrupt an authored fist impact, moving earthquake wave,
        // 94%-limited 4-stomp IK or Awakening. Keep one visible reaction for
        // AFTER the fully committed move, rather than trashing its events.
        if (state != TitanState.Chasing && state != TitanState.AwakenedHold)
        {
            queuedBeingHit = true;
            beingHitStatus = "Real HP damage received; waiting for safe attack recovery";
            return;
        }
        BeginBeingHitOverlay();
    }

    private void BeginBeingHitOverlay()
    {
        queuedBeingHit = false;
        if (!animator || !animator.isActiveAndEnabled ||
            beingHitLayerIndex < 1 ||
            !animator.HasState(beingHitLayerIndex, HitLayerStateHash))
        {
            if (!warnedMissingBeingHit)
                Debug.LogWarning("[TITAN BEING HIT] Isolated Titan upper-body overlay layer/state missing. " +
                    "Combat HP still applies, but no fake full-body Golem animation is played.", this);
            warnedMissingBeingHit = true;
            beingHitStatus = "Missing Titan-specific upper-body layer";
            return;
        }
        warnedMissingBeingHit = false;
        animator.Play(HitLayerStateHash, beingHitLayerIndex, 0f);
        animator.SetLayerWeight(beingHitLayerIndex, 0f);
        beingHitOverlayActive = true;
        beingHitStartedTime = Time.time;
        nextBeingHitTime = Time.time + beingHitCooldownSeconds;
        beingHitReactionsStarted++;
        beingHitStatus = "Active: additive upper-body recoil (original movement continues)";
        Debug.Log("[TITAN BEING HIT] Genuine spell HP damage: Titan upper-body layer recoil #" +
            beingHitReactionsStarted + ", no attack/foot/IK interruption.", this);
    }

    private void UpdateBeingHitOverlay()
    {
        if (queuedBeingHit && !beingHitOverlayActive &&
            (state == TitanState.Chasing || state == TitanState.AwakenedHold) &&
            Time.time >= nextBeingHitTime)
            BeginBeingHitOverlay();
        if (!beingHitOverlayActive) return;
        if (!animator || !animator.isActiveAndEnabled ||
            state != TitanState.Chasing && state != TitanState.AwakenedHold)
        {
            ResetBeingHitOverlay();
            return;
        }
        float elapsed = Time.time - beingHitStartedTime;
        float total = Mathf.Max(0.1f, beingHitReactionSeconds);
        if (elapsed >= total)
        {
            ResetBeingHitOverlay();
            beingHitStatus = "Completed: returned to uninterrupted base-layer locomotion";
            return;
        }
        float fadeIn = Mathf.Clamp01(elapsed / Mathf.Min(0.09f, total * 0.3f));
        float fadeOut = Mathf.Clamp01((total - elapsed) / Mathf.Min(0.17f, total * 0.4f));
        animator.SetLayerWeight(beingHitLayerIndex,
            Mathf.Clamp01(beingHitLayerWeight) * Mathf.Min(fadeIn, fadeOut));
    }

    private void ResetBeingHitOverlay()
    {
        beingHitOverlayActive = false;
        if (!animator || beingHitLayerIndex < 1 || beingHitLayerIndex >= animator.layerCount)
            return;
        animator.SetLayerWeight(beingHitLayerIndex, 0f);
        if (animator.isActiveAndEnabled && animator.HasState(beingHitLayerIndex, HitEmptyStateHash))
            animator.Play(HitEmptyStateHash, beingHitLayerIndex, 0f);
    }

    // EnemyHealth Died event is the source of death, not a timer or player
    // position. No HP resurrection, prefab destruction or fake victory.
    public void TriggerDeath()
    {
        if (state == TitanState.Dead) return;
        queuedBeingHit = false;
        ResetBeingHitOverlay();
        hammerfistStompPursuitPending = false;
        hammerfistStompVictimMotion = null;
        hammerfistStompPursuitStatus = "Cancelled: Titan defeated";
        verifiedSlamFollowupPending = false;
        verifiedSlamFollowupStatus = "Cancelled: Titan defeated";
        FinishV017StanceApproach(false);
        if (colossalStompV017 && colossalStompV017.IsRunning) colossalStompV017.StopPreview();
        if (progressiveEarthquake) progressiveEarthquake.StopSequence();
        battleStarted = false;
        state = TitanState.Dead;
        StopNavigation();
        if (agent) agent.enabled = false;
        if (combatCollider) combatCollider.enabled = false;
        if (physicalBodyCollider) physicalBodyCollider.enabled = true;
        if (spellTarget) spellTarget.tag = "Untagged";
        if (health) health.SetInvulnerable(true);
        animator.SetBool(WalkingHash, false);
        animator.ResetTrigger(HammerHash);
        if (enableEarthbreakerSlamAI) animator.ResetTrigger(SlamHash);
        gameObject.tag = "Untagged";
        deathStartedTime = Time.time;
        deathEventReceived = false;
        deathAnimationStarted = enableAuthoredDeath && animator != null &&
            animator.isActiveAndEnabled && animator.HasState(0, DeathStateHash);
        if (deathAnimationStarted)
        {
            animator.CrossFadeInFixedTime(DeathStateHash,
                Mathf.Max(0f, deathBlendSeconds), 0, 0f);
            deathStatus = "Playing authored 3.3s Titan V015 death, awaiting real event";
            Debug.Log("[TITAN DEATH] Genuine 0 HP: V015 death animation playing. " +
                "Navigation, attacks, projectile hurtbox and hit overlay disabled; " +
                "solid corpse preserved. Await authored finish event or bounded fallback.", this);
        }
        else
        {
            deathStatus = "Authored death state missing: immediate safe defeat completion";
            Debug.LogError("[TITAN DEATH] Authored V015 state disabled/missing. " +
                "Notify defeat once instead of soft-locking Node 4.", this);
            CompleteDeath(false);
        }
    }

    private void OnHealthDied(EnemyHealth _) => TriggerDeath();

    private void UpdateDeathCompletionFallback()
    {
        if (defeatReported || !deathAnimationStarted || deathStartedTime < 0f) return;
        float expected = authoredDeathClip ? authoredDeathClip.length : 3.3f;
        if (Time.time - deathStartedTime < expected + Mathf.Max(0.1f, deathFallbackPadding))
            return;
        Debug.LogWarning("[TITAN DEATH] Death finish event was not received; " +
            "bounded 3.3s authored-clip fallback completes Node 4 once.", this);
        CompleteDeath(false);
    }

    /// <summary>Authored event near final frame on isolated V015 clip copy.</summary>
    public void OnDeathFinished()
    {
        CompleteDeath(true);
    }

    private void CompleteDeath(bool animationEvent)
    {
        if (state != TitanState.Dead || defeatReported) return;
        defeatReported = true;
        deathEventReceived = animationEvent;
        deathStatus = animationEvent ? "V015 final animation event delivered" :
            "Fallback: no event, encounter still safely completed";
        if (!defeatReceiver)
        {
            GameObject node4 = GameObject.Find("Node 4");
            if (node4) defeatReceiver = node4.GetComponent<StageNodeController>();
        }
        defeatReceiver?.OnEnemyDefeated();
        onTitanDefeated?.Invoke();
        Debug.Log("[TITAN DEATH] FINISHED event=" + animationEvent +
            " Node4Notified=" + (defeatReceiver != null) +
            " once. Corpse and solid colliders retained.", this);
    }

    private bool ResolvePlayer()
    {
        if (player) return true;
        player = FindFirstObjectByType<PlayerHealth>();
        return player != null;
    }

    private void StopNavigation()
    {
        if (agent && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }
}
