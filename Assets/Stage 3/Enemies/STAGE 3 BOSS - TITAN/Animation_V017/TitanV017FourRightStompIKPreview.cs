using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations.Rigging;

/// <summary>
/// Isolated V017 (264 frames / 30 FPS / 8.8 seconds) visual and physical review.
/// Plays four RIGHT-foot stomps with a continuously planted LEFT support foot.
/// No direct boss AI, damage, health, crowd control, navigation or progression
/// modifications exist in this solver. It emits a verified-sole event that a
/// separate opt-in production boss can independently test against the player.
///
/// Aim is sampled exactly once per stomp, at its authored windup. The descent
/// target then stays in world space and never chases the moving reference.
/// Every impact requires post-RigBuilder real skinned mesh vertices AND the
/// separately calibrated sole proxy, never the IK targets or a timer alone.
///
/// At a ~40x Titan scale, the foot bone may be ~9.4 world metres ABOVE the
/// rendered sole. Sole probes (or carefully calibrated foot-local offsets)
/// MUST represent actual sole geometry, NOT ankle or a bare ToeBase pivot.
/// Both IK targets MUST be outside the Animator's animated hierarchy.
/// </summary>
[DisallowMultipleComponent]
public sealed class TitanV017FourRightStompIKPreview : MonoBehaviour
{
    private const string PreviewStatePath = "Base Layer.ColossalStompRightFootV017";
    private const string ExpectedVisibleBodyName = "S3N4_Titan_OpenStoneHands_Skinned";
    private const int RequiredStomps = 4;
    private const float SourceFps = 30f;
    // 264 rendered video frames occupy 8.8s. The first-to-last keyed pose
    // spans 263/30 = 8.766667s in many Unity imports (final-frame dwell
    // is not an additional animation key). Validate either within tolerance.
    private const float SourceClipSeconds = 8.8f;
    // V017 frames 15..27 contain an intentional LEFT preparation step.
    // The left support finally plants at frame 28 and stays there thereafter.
    private const float LeftPlantStartSeconds = (28f - 1f) / SourceFps;

    [Serializable]
    private struct StompWindow
    {
        [Min(0f)] public float windupStart;
        [Min(0f)] public float descentStart;
        [Min(0f)] public float visualImpact;
        [Min(0f)] public float recoveryStart;
        [Min(0f)] public float releaseEnd;

        public StompWindow(int windupFrame, int descentFrame, int impactFrame,
            int recoveryFrame, int releaseFrame)
        {
            windupStart = (windupFrame - 1) / SourceFps;
            descentStart = (descentFrame - 1) / SourceFps;
            visualImpact = (impactFrame - 1) / SourceFps;
            recoveryStart = (recoveryFrame - 1) / SourceFps;
            releaseEnd = (releaseFrame - 1) / SourceFps;
        }
    }

    [Header("ISOLATED V017 ONLY - OFF until manually approved")]
    [SerializeField] private bool allowIsolatedPreview = false;
    [SerializeField] private bool showReviewButtons = true;
    [SerializeField] private Animator animator;
    [SerializeField] private RigBuilder rigBuilder;
    [SerializeField, Min(0.1f)] private float expectedClipSeconds = SourceClipSeconds;
    [SerializeField, Min(0f)] private float clipLengthTolerance = 0.08f;
    [Tooltip("Smoothly return to the previously captured isolated review Animator state. V017 frame264 does not match frame1; inspect transition for large Titan-scale foot movement.")]
    [SerializeField, Range(0.1f, 1.5f)] private float returnToPreviousStateCrossfadeSeconds = 0.6f;

    [Header("V017 Clip Seconds: windup / descent / impact / recovery / release")]
    [Tooltip("V017 beats (frames 1..264, 30 FPS). Descent 47/87/127/170, impacts 53/93/133/176. Verify the selected import in Unity.")]
    [SerializeField] private StompWindow[] stomps =
    {
        new StompWindow(28, 47, 53, 55, 62),
        new StompWindow(70, 87, 93, 95, 103),
        new StompWindow(110, 127, 133, 135, 143),
        new StompWindow(150, 170, 176, 178, 188)
    };

    [Header("65-Bone V017 Leg Chains / World Targets")]
    [SerializeField] private Transform rightUpperLeg;
    [SerializeField] private Transform rightLowerLeg;
    [SerializeField] private Transform rightFoot;
    [SerializeField] private Transform leftUpperLeg;
    [SerializeField] private Transform leftLowerLeg;
    [SerializeField] private Transform leftFoot;
    [Tooltip("A freely moving Van marker. Sampled ONCE at each windup, never tracked afterward.")]
    [SerializeField] private Transform previewAimReference;
    [SerializeField] private TwoBoneIKConstraint rightFootIK;
    [SerializeField] private Transform rightIKTarget;
    [SerializeField] private TwoBoneIKConstraint leftFootPlantIK;
    [SerializeField] private Transform leftPlantTarget;
    [Header("V017 authored left preparation step: unrigged until frame 28")]
    [Tooltip("Blend left TwoBoneIK on AFTER the left step reaches its frame-28 plant. Short enough to settle before stomp 1.")]
    [SerializeField, Range(0.01f, 0.4f)] private float leftPlantBlendSeconds = 0.15f;
    [Tooltip("Fail if frame-28 plant is skipped on a hitch, rather than freezing left foot mid-stride.")]
    [SerializeField, Min(0.01f)] private float maxLeftPlantCaptureDelaySeconds = 0.15f;
    [Tooltip("QA experiment only: fade supporting-foot IK OFF near 94% reach and verify the native Astra sole still plants. Never force a long leg into the target.")]
    [SerializeField] private bool preserveNativeLeftWhenIKUnreachable = false;

    [Header("V017 Measured Rendered-Sole Calibration - required for PASS")]
    [Tooltip("Visible, skinned Titan BODY renderer used for REAL contact validation. " +
        "If unassigned, resolve the uniquely named S3N4_Titan_OpenStoneHands_Skinned. " +
        "Production MUST use a CPU-readable mesh on this same visible renderer; " +
        "a readable QA-only copy elsewhere is not accepted.")]
    [SerializeField] private SkinnedMeshRenderer soleBodyRenderer;
    [Tooltip("Must be enabled only AFTER visually confirming the points below match the bottom of each deformed foot mesh at the imported scale.")]
    [SerializeField] private bool measuredSoleCalibrationConfirmed = false;
    [Tooltip("Optional child of RightFoot, ideally a precise sole marker. ToeBase alone is not guaranteed to be the sole's lowest vertex.")]
    [SerializeField] private Transform rightSoleReference;
    [Tooltip("Metres in rightSoleReference local space; if no reference, in RightFoot local space. Account for ~0.235m source ankle clearance times imported scale.")]
    [SerializeField] private Vector3 rightSoleLocalOffset;
    [SerializeField] private Transform leftSoleReference;
    [Tooltip("Metres in leftSoleReference local space; if no reference, in LeftFoot local space.")]
    [SerializeField] private Vector3 leftSoleLocalOffset;
    [Tooltip("Allowed difference between each starting rendered sole and its ground plane, in world metres.")]
    [SerializeField, Min(0.01f)] private float initialSoleGroundTolerance = 0.35f;

    [Header("Ground and anatomical safety (world metres)")]
    [SerializeField] private LayerMask terrainLayers = ~0;
    [SerializeField, Min(0.1f)] private float groundRayLift = 35f;
    [SerializeField, Min(0.1f)] private float groundRayDownDistance = 160f;
    [SerializeField, Range(0.5f, 1f)] private float minGroundNormalY = 0.7f;
    [SerializeField, Min(0f)] private float maxLevelHeightDelta = 1f;
    [Tooltip("At most 94% of the actual two-bone leg length; never increase past 0.94.")]
    [SerializeField, Range(0.5f, 0.94f)] private float anatomicalReachFraction = 0.94f;
    [SerializeField, Min(0.01f)] private float maxActorRootDrift = 0.05f;
    [SerializeField, Range(0.01f, 5f)] private float maxActorRootTurnDegrees = 1f;
    [SerializeField, Min(0.01f)] private float maxLeftFootPlanarDrift = 0.6f;
    [SerializeField, Min(0.01f)] private float maxLeftFootVerticalDrift = 0.35f;
    [SerializeField, Min(0.01f)] private float maxRecoveryTargetSpeed = 28f;
    [SerializeField, Min(0.01f)] private float maxRecoveryTargetTravel = 15f;

    [Header("Four physical impacts (no timer-only PASS)")]
    [Tooltip("An actual lifting right sole must be observed before its impact can count.")]
    [SerializeField, Min(0.01f)] private float minRightSoleLiftBeforeImpact = 0.35f;
    [SerializeField, Min(0.01f)] private float maxRightSolePlanarContactError = 0.75f;
    [SerializeField, Min(0.01f)] private float maxRightSoleVerticalContactError = 0.30f;
    [SerializeField, Min(0.01f)] private float maxLeftSolePlanarContactError = 0.60f;
    [SerializeField, Min(0.01f)] private float maxLeftSoleVerticalContactError = 0.30f;
    [SerializeField, Min(0.01f)] private float maxRightAnkleIKError = 1.0f;
    [SerializeField, Min(0.01f)] private float maxLeftAnkleIKError = 1.0f;

    [Header("Read-only Runtime Diagnostics")]
    [SerializeField] private bool running;
    [SerializeField] private int currentStompIndex = -1;
    [SerializeField] private int completedStomps;
    [SerializeField] private int verifiedPhysicalContacts;
    [SerializeField] private float currentClipSeconds;
    [SerializeField] private bool descentHardLocked;
    [SerializeField] private bool leftPlantCommitted;
    [SerializeField] private float highestObservedRightSoleLift;
    [SerializeField] private Vector3 committedRightSoleGround;
    [SerializeField] private Vector3 leftSoleGround;
    [SerializeField] private float lastRightSoleHeightError;
    [SerializeField] private float lastRightSolePlanarError;
    [SerializeField] private float lastLeftSoleHeightError;
    [SerializeField] private float lastRightDeformedMeshPlanarError;
    [SerializeField] private float lastRightDeformedMeshHeightError;
    [SerializeField] private float lastLeftDeformedMeshPlanarError;
    [SerializeField] private float lastLeftDeformedMeshHeightError;
    [SerializeField] private float highestObservedRightDeformedMeshLift;
    [SerializeField] private string status = "V017 preview disabled. Set up external IK targets and measured soles.";
    [SerializeField] private string lastContactGate = "No contact-window sample yet.";

    private int previewStateHash;
    private int previousStateHash;
    private float previousStateNormalizedTime;
    private float previousStateLengthSeconds;
    private bool havePreviousState;
    private bool cachedRigState;
    private float initialRightWeight;
    private float initialLeftWeight;
    private Vector3 initialRightTargetPosition;
    private Vector3 initialLeftTargetPosition;
    private Quaternion initialRightTargetRotation;
    private Quaternion initialLeftTargetRotation;
    private Vector3 startRootPosition;
    private Quaternion startRootRotation;
    private Vector3 rightSoleOffsetFromAnkle;
    private Vector3 leftSoleOffsetFromAnkle;
    private Vector3 plantedLeftAnkle;
    private Vector3 committedRightAnkle;
    private Quaternion initialRightFootRotation;
    private Quaternion initialLeftFootRotation;
    private float maxRightReach;
    private float maxLeftReach;
    private float previousClipSeconds;
    private int nextStompIndex;
    private bool sawRightSoleLift;
    private bool sawRightDeformedMeshLift;
    private TitanV017DeformedSoleLbs renderedMeshSoles;
    private bool lastStompPassed;
    private bool naturallyCompletedEntireClip;
    // A recovery overshoot must not be hidden by a later four-contact PASS.
    private bool unsafeRecoveryIKObserved;
    // Store the FIRST frame-28 ungrounded pose but only PRINT if the entire
    // already-authorized 0.15s native planting window eventually FAILS.
    private string firstFrame28UngroundedTrace;

    // Read-only handshake for the Stage 3 Titan controller. The IK script
    // remains the sole owner of foot targets and physical-contact validation.
    public bool IsRunning => running;
    public bool LastStompPassed => lastStompPassed;
    public int VerifiedSoleContacts => verifiedPhysicalContacts;
    /// <summary>
    /// Production COMBAT MAY LISTEN to genuine right-sole ground contacts.
    /// Invoked only once per stomp AFTER all real post-RigBuilder/LBS/left
    /// support/ground/94%-reach checks pass. This is NOT a player hit.
    /// Solo QA scenes without subscribers retain zero gameplay effects.
    /// </summary>
    public event Action<int, Vector3> CertifiedRightSoleGroundContact;

    private void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (!rigBuilder) rigBuilder = GetComponent<RigBuilder>();
        previewStateHash = Animator.StringToHash(PreviewStatePath);
    }

    /// <summary>Explicit operator action for the isolated art-review scene only.</summary>
    public void BeginPreview()
    {
        if (running) StopPreview();
        lastStompPassed = false;
        naturallyCompletedEntireClip = false;
        unsafeRecoveryIKObserved = false;
        firstFrame28UngroundedTrace = null;
        lastContactGate = "No contact-window sample yet.";
        if (!Application.isPlaying || !allowIsolatedPreview)
        {
            status = "Denied: isolated V017 opt-in is OFF, or not in Play Mode.";
            return;
        }
        if (!ValidateConfiguration(out string issue))
        {
            Deny(issue);
            return;
        }
        if (!TryResolveVisibleSoleBody(out SkinnedMeshRenderer body, out issue))
        {
            Deny(issue);
            return;
        }
        if (!TitanV017DeformedSoleLbs.TryCreate(animator.transform, body,
                rightFoot, leftFoot,
                out renderedMeshSoles, out issue))
        {
            Deny(issue);
            return;
        }

        // Cache everything BEFORE changing Animator / constraint weights.
        AnimatorStateInfo prior = animator.GetCurrentAnimatorStateInfo(0);
        if (animator.IsInTransition(0) || prior.fullPathHash == 0 ||
            prior.fullPathHash == previewStateHash || !animator.HasState(0, prior.fullPathHash))
        {
            Deny("Start from a stable non-V017 review/idle Animator state.");
            return;
        }
        previousStateHash = prior.fullPathHash;
        previousStateNormalizedTime = Mathf.Repeat(prior.normalizedTime, 1f);
        previousStateLengthSeconds = prior.length;
        havePreviousState = true;
        initialRightTargetPosition = rightIKTarget.position;
        initialRightTargetRotation = rightIKTarget.rotation;
        initialLeftTargetPosition = leftPlantTarget.position;
        initialLeftTargetRotation = leftPlantTarget.rotation;
        initialRightWeight = rightFootIK.weight;
        initialLeftWeight = leftFootPlantIK.weight;
        cachedRigState = true;

        // Rig off while sampling the AUTHORED first frame. Without this, a
        // previous rigged idle pose can corrupt our sole/ankle calibration.
        rightFootIK.weight = 0f;
        leftFootPlantIK.weight = 0f;
        animator.Play(previewStateHash, 0, 0f);
        animator.Update(0f);
        AnimatorStateInfo sampled = animator.GetCurrentAnimatorStateInfo(0);
        if (animator.IsInTransition(0) || sampled.fullPathHash != previewStateHash ||
            Mathf.Abs(sampled.length - SourceClipSeconds) > clipLengthTolerance)
        {
            DenyAndRestore("Imported Animator first frame/state or 8.8s duration mismatch.");
            return;
        }
        if (!TryGround(GetRightSole(), out Vector3 rightGround) ||
            !TryGround(GetLeftSole(), out Vector3 leftGround))
        {
            DenyAndRestore("Cannot sample terrain directly beneath BOTH rendered soles.");
            return;
        }
        if (Mathf.Abs(GetRightSole().y - rightGround.y) > initialSoleGroundTolerance ||
            Mathf.Abs(GetLeftSole().y - leftGround.y) > initialSoleGroundTolerance ||
            Mathf.Abs(rightGround.y - leftGround.y) > maxLevelHeightDelta)
        {
            var nav = GetComponent<NavMeshAgent>();
            Debug.LogWarning("[V017 F1 TERRAIN] Native authored start rejected, no IK/damage: " +
                "rightSoleY=" + GetRightSole().y.ToString("F4") +
                " rightFloorY=" + rightGround.y.ToString("F4") +
                " leftSoleY=" + GetLeftSole().y.ToString("F4") +
                " leftFloorY=" + leftGround.y.ToString("F4") +
                " rootY=" + transform.position.y.ToString("F4") +
                " navOffset=" + (nav ? nav.baseOffset.ToString("F5") : "none") +
                " navY=" + (nav && nav.enabled && nav.isOnNavMesh
                    ? nav.nextPosition.y.ToString("F4") : "none") +
                " maxSole=" + initialSoleGroundTolerance.ToString("F2") +
                ". Do not loosen physical ground checks.", this);
            DenyAndRestore("Rendered sole calibration is inaccurate, penetrates ground, or starts on unequal terrain.");
            return;
        }
        if (!renderedMeshSoles.TrySample(out Vector3 rightMeshFirst,
                out Vector3 leftMeshFirst) ||
            Mathf.Abs(rightMeshFirst.y - rightGround.y) > initialSoleGroundTolerance ||
            Mathf.Abs(leftMeshFirst.y - leftGround.y) > initialSoleGroundTolerance ||
            PlanarDistance(rightMeshFirst, GetRightSole()) > maxRightSolePlanarContactError ||
            PlanarDistance(leftMeshFirst, GetLeftSole()) > maxLeftSolePlanarContactError)
        {
            DenyAndRestore("LBS-deformed foot mesh does not match initial grounded calibrated sole geometry.");
            return;
        }

        startRootPosition = transform.position;
        startRootRotation = transform.rotation;
        rightSoleOffsetFromAnkle = GetRightSole() - rightFoot.position;
        leftSoleOffsetFromAnkle = GetLeftSole() - leftFoot.position;
        initialRightFootRotation = rightFoot.rotation;
        initialLeftFootRotation = leftFoot.rotation;
        float rightUpperLength = Vector3.Distance(rightUpperLeg.position, rightLowerLeg.position);
        float rightLowerLength = Vector3.Distance(rightLowerLeg.position, rightFoot.position);
        float leftUpperLength = Vector3.Distance(leftUpperLeg.position, leftLowerLeg.position);
        float leftLowerLength = Vector3.Distance(leftLowerLeg.position, leftFoot.position);
        if (!IsFinite(rightUpperLength) || !IsFinite(rightLowerLength) ||
            !IsFinite(leftUpperLength) || !IsFinite(leftLowerLength) ||
            rightUpperLength <= 0.01f || rightLowerLength <= 0.01f ||
            leftUpperLength <= 0.01f || leftLowerLength <= 0.01f)
        {
            DenyAndRestore("V017 left/right leg contains a collapsed or nonfinite segment; anatomical 94% reach cannot be trusted.");
            return;
        }
        maxRightReach = (rightUpperLength + rightLowerLength) * anatomicalReachFraction;
        maxLeftReach = (leftUpperLength + leftLowerLength) * anatomicalReachFraction;
        leftSoleGround = leftGround;
        // Footframe 1 calibration is only a sanity check. V017's left leg
        // intentionally STEPS at frames 15..27. Its actual plant point and
        // ankle-to-sole vector must be sampled again at frame 28.
        plantedLeftAnkle = leftFoot.position;
        leftPlantCommitted = false;
        if (!IsFinite(maxRightReach) || !IsFinite(maxLeftReach) ||
            maxRightReach <= 0.01f || maxLeftReach <= 0.01f)
        {
            DenyAndRestore("Invalid V017 two-bone leg lengths.");
            return;
        }

        rightIKTarget.SetPositionAndRotation(rightFoot.position, initialRightFootRotation);
        leftPlantTarget.SetPositionAndRotation(leftFoot.position, initialLeftFootRotation);
        rightFootIK.weight = 0f;
        leftFootPlantIK.weight = 0f; // Preserve authored prep-step lift.
        rigBuilder.SyncLayers();
        completedStomps = 0;
        verifiedPhysicalContacts = 0;
        currentStompIndex = -1;
        nextStompIndex = 0;
        currentClipSeconds = 0f;
        previousClipSeconds = 0f;
        highestObservedRightSoleLift = 0f;
        sawRightSoleLift = false;
        highestObservedRightDeformedMeshLift = 0f;
        sawRightDeformedMeshLift = false;
        descentHardLocked = false;
        running = true;
        status = "V017 active: native left prep step, then four right stomps. Solver never directly applies HP/CC.";
        Debug.Log("[V017 Stomp QA] Preview started; support mode=" +
                  (preserveNativeLeftWhenIKUnreachable
                      ? "QA native-left fallback (strict deformed-mesh plant required)"
                      : "STRICT left IK (abort before 94% support reach)") +
                  "; deformed mesh LBS calibrated at authored first frame (" +
                  renderedMeshSoles.RendererName + "; rightVertices=" +
                  renderedMeshSoles.RightVertexCount + ", leftVertices=" +
                  renderedMeshSoles.LeftVertexCount + ").", this);
    }

    private bool ValidateConfiguration(out string issue)
    {
        issue = null;
        if (!animator || !animator.isActiveAndEnabled || !animator.runtimeAnimatorController ||
            !animator.gameObject.activeInHierarchy)
            return Fail("Animator/controller missing or disabled.", out issue);
        if (animator.applyRootMotion || animator.updateMode != AnimatorUpdateMode.Normal ||
            Mathf.Abs(animator.speed - 1f) > 0.001f)
            return Fail("Animator requires root motion OFF, Normal update, speed 1.", out issue);
        if (!animator.HasState(0, previewStateHash))
            return Fail("Exact Animator state Base Layer.ColossalStompRightFootV017 absent.", out issue);
        if (Mathf.Abs(expectedClipSeconds - SourceClipSeconds) > 0.001f)
            return Fail("Expected clip must stay at V017 8.8 seconds.", out issue);
        if (!rigBuilder || !rigBuilder.isActiveAndEnabled || !rightFootIK ||
            !leftFootPlantIK || !rightFootIK.isActiveAndEnabled ||
            !leftFootPlantIK.isActiveAndEnabled || !rightIKTarget || !leftPlantTarget)
            return Fail("Enabled RigBuilder and BOTH TwoBoneIK constraints/targets required.", out issue);
        if (!rigBuilder.graph.IsValid() || rigBuilder.layers.Count == 0 ||
            rigBuilder.layers[0].rig == null || !rigBuilder.layers[0].active ||
            !rigBuilder.layers[0].rig.isActiveAndEnabled ||
            rigBuilder.layers[0].rig.weight < 0.999f ||
            !rightFootIK.IsValid() || !leftFootPlantIK.IsValid() ||
            !rightFootIK.transform.IsChildOf(rigBuilder.layers[0].rig.transform) ||
            !leftFootPlantIK.transform.IsChildOf(rigBuilder.layers[0].rig.transform))
            return Fail("V017 foot constraints require the active, built FIRST RigBuilder layer at full weight and valid IK jobs.", out issue);
        if (!previewAimReference || !rightUpperLeg || !rightLowerLeg || !rightFoot ||
            !leftUpperLeg || !leftLowerLeg || !leftFoot)
            return Fail("Missing aim marker or complete 65-bone V017 leg references.", out issue);
        if (previewAimReference.IsChildOf(animator.transform) ||
            rightIKTarget.IsChildOf(animator.transform) ||
            leftPlantTarget.IsChildOf(animator.transform))
            return Fail("Aim marker and IK targets MUST be outside animated Titan hierarchy.", out issue);
        if (!rightFoot.IsChildOf(rightLowerLeg) || !rightLowerLeg.IsChildOf(rightUpperLeg) ||
            !leftFoot.IsChildOf(leftLowerLeg) || !leftLowerLeg.IsChildOf(leftUpperLeg) ||
            !rightUpperLeg.IsChildOf(animator.transform) ||
            !leftUpperLeg.IsChildOf(animator.transform))
            return Fail("V017 leg chain transforms do not match Animator skeleton.", out issue);
        var rightData = rightFootIK.data;
        var leftData = leftFootPlantIK.data;
        if (rightData.root != rightUpperLeg || rightData.mid != rightLowerLeg ||
            rightData.tip != rightFoot || rightData.target != rightIKTarget ||
            leftData.root != leftUpperLeg || leftData.mid != leftLowerLeg ||
            leftData.tip != leftFoot || leftData.target != leftPlantTarget)
            return Fail("TwoBoneIK chain or target wiring differs from inspector references.", out issue);
        if (rightIKTarget == leftPlantTarget ||
            rightIKTarget.IsChildOf(leftPlantTarget) || leftPlantTarget.IsChildOf(rightIKTarget))
            return Fail("Foot IK targets must be separate independent world-space transforms.", out issue);
        if (!measuredSoleCalibrationConfirmed)
            return Fail("Rendered sole calibration must be measured and confirmed in Inspector.", out issue);
        if ((rightSoleReference && !rightSoleReference.IsChildOf(rightFoot)) ||
            (leftSoleReference && !leftSoleReference.IsChildOf(leftFoot)))
            return Fail("Sole references must follow their OWN posed foot chain.", out issue);
        if ((!rightSoleReference && rightSoleLocalOffset.sqrMagnitude < 0.0001f) ||
            (!leftSoleReference && leftSoleLocalOffset.sqrMagnitude < 0.0001f) ||
            !IsFinite(rightSoleLocalOffset) || !IsFinite(leftSoleLocalOffset))
            return Fail("Configure non-zero measured foot-local sole offsets or attached sole probes.", out issue);
        if (stomps == null || stomps.Length != RequiredStomps)
            return Fail("V017 requires exactly four stomp timing windows.", out issue);
        float previousRelease = -0.01f;
        for (int i = 0; i < stomps.Length; i++)
        {
            StompWindow w = stomps[i];
            if (!IsFinite(w.windupStart) || !IsFinite(w.descentStart) ||
                !IsFinite(w.visualImpact) || !IsFinite(w.recoveryStart) ||
                !IsFinite(w.releaseEnd) || w.windupStart <= previousRelease ||
                w.descentStart <= w.windupStart || w.visualImpact <= w.descentStart ||
                w.recoveryStart <= w.visualImpact || w.releaseEnd <= w.recoveryStart ||
                w.releaseEnd > SourceClipSeconds)
                return Fail("Invalid/overlapping V017 timing at stomp " + (i + 1), out issue);
            previousRelease = w.releaseEnd;
        }
        // No hidden dependence on any boss controller or stage scene.
        if (anatomicalReachFraction > 0.94f)
            return Fail("Anatomical reach may NEVER exceed 94%.", out issue);
        return true;
    }

    private bool TryResolveVisibleSoleBody(out SkinnedMeshRenderer body, out string issue)
    {
        issue = null;
        body = soleBodyRenderer;
        // Verify uniqueness EVEN when the optional serialized reference is
        // assigned. A second same-named readable QA mesh cannot silently
        // replace the real body, and a wrongly wired side mesh must not pass.
        SkinnedMeshRenderer uniquelyNamedBody = null;
        int matches = 0;
        foreach (SkinnedMeshRenderer candidate in
            animator.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (candidate && candidate.name == ExpectedVisibleBodyName)
            {
                matches++;
                uniquelyNamedBody = candidate;
            }
        }
        if (matches != 1 || (body && body != uniquelyNamedBody))
        {
            issue = "Expected exactly ONE canonical V017 body renderer '" +
                ExpectedVisibleBodyName + "' (found " + matches +
                "); the configured soleBodyRenderer must be that exact visible body, not a QA clone.";
            return false;
        }
        if (!body) body = uniquelyNamedBody;
        if (!body || !body.transform.IsChildOf(animator.transform) ||
            !body.enabled || !body.gameObject.activeInHierarchy || !body.sharedMesh)
        {
            issue = "V017 soleBodyRenderer must be the ACTIVE visible Titan skinned body under its Animator, with an assigned mesh.";
            return false;
        }
        return true;
    }

    private void Update()
    {
        if (!running)
        {
            // A whole-GO disable can defer Animator.Play restoration until
            // Unity enables the Animator again on a subsequent frame.
            if (havePreviousState) RestoreState();
            return;
        }
        if (!allowIsolatedPreview || !measuredSoleCalibrationConfirmed ||
            !animator || !animator.isActiveAndEnabled || !rigBuilder ||
            !rigBuilder.isActiveAndEnabled || !rigBuilder.graph.IsValid() ||
            rigBuilder.layers.Count == 0 || !rigBuilder.layers[0].active ||
            !rigBuilder.layers[0].rig || !rigBuilder.layers[0].rig.isActiveAndEnabled ||
            rigBuilder.layers[0].rig.weight < 0.999f ||
            !rightFootIK || !leftFootPlantIK || !rightFootIK.isActiveAndEnabled ||
            !leftFootPlantIK.isActiveAndEnabled || animator.applyRootMotion ||
            !rightIKTarget || !leftPlantTarget ||
            rightIKTarget.IsChildOf(animator.transform) ||
            leftPlantTarget.IsChildOf(animator.transform) ||
            Vector3.Distance(transform.position, startRootPosition) > maxActorRootDrift ||
            Quaternion.Angle(transform.rotation, startRootRotation) > maxActorRootTurnDegrees)
        {
            Abort("Animator/rig disabled, root moved, root motion enabled, or opt-in revoked.");
            return;
        }
        if (animator.IsInTransition(0))
        {
            Abort("Animator entered a transition before completing V017.");
            return;
        }
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (state.fullPathHash != previewStateHash ||
            Mathf.Abs(state.length - SourceClipSeconds) > clipLengthTolerance ||
            state.normalizedTime < 0f || state.normalizedTime > 1.02f)
        {
            Abort("Unexpected V017 Animator state/time/length or looping.");
            return;
        }
        currentClipSeconds = Mathf.Clamp01(state.normalizedTime) * state.length;
        if (currentClipSeconds + 0.002f < previousClipSeconds)
        {
            Abort("V017 clip rewound; timing/targets no longer trustworthy.");
            return;
        }
        previousClipSeconds = currentClipSeconds;
        // Do not abort a pending frame-28 support in Update: these bones still
        // contain the previous Animator pose. LateUpdate alone enforces the
        // ORIGINAL 0.15s deadline against the freshly evaluated native pose.
        float leftRequestedReach = leftPlantCommitted
            ? Vector3.Distance(leftUpperLeg.position, plantedLeftAnkle) : 0f;
        if (leftPlantCommitted && leftRequestedReach > maxLeftReach &&
            !preserveNativeLeftWhenIKUnreachable)
        {
            Abort("Left plant exceeded 94% anatomical leg reach.");
            return;
        }
        if (leftPlantCommitted)
        {
            leftPlantTarget.SetPositionAndRotation(plantedLeftAnkle, initialLeftFootRotation);
            float blend = Mathf.InverseLerp(LeftPlantStartSeconds,
                LeftPlantStartSeconds + leftPlantBlendSeconds, currentClipSeconds);
            // Astra's authentic planted foot sometimes uses a nearly fully
            // straight support leg. A 94%-limited IK correction cannot reach
            // that pose. In isolated QA, release IK to the ORIGINAL animation
            // before reaching the hard cap; continue only while actual mesh
            // contact proves that this native foot remains truly planted.
            float safeWeight = 1f;
            if (preserveNativeLeftWhenIKUnreachable)
            {
                // The huge source mocap hip can rise >1m across a single
                // evaluated frame. Release corrective IK BEFORE the 94%
                // boundary; only Astra's actual grounded native left foot
                // remains authoritative beyond that point. Independently
                // verify rendered-mesh left sole stays on the terrain.
                float nextFrameReserve = Mathf.Max(1.5f, maxLeftReach * 0.07f);
                float releaseBand = Mathf.Max(1f, maxLeftReach * 0.07f);
                float headroom = maxLeftReach - nextFrameReserve - leftRequestedReach;
                safeWeight = Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01(headroom / releaseBand));
            }
            leftFootPlantIK.weight = Mathf.SmoothStep(0f, 1f, blend) * safeWeight;
            if (currentClipSeconds > 1.21f && currentClipSeconds < 1.225f)
                Debug.Log("[V017 IK WEIGHT TRACE UPDATE] frame=" + Time.frameCount +
                    " elapsed=" + currentClipSeconds.ToString("F4") +
                    " blend=" + blend.ToString("F4") +
                    " constraintID=" + leftFootPlantIK.GetInstanceID() +
                    " assignedWeight=" + leftFootPlantIK.weight.ToString("F4") +
                    " rigWeight=" + rigBuilder.layers[0].rig.weight.ToString("F4"), this);
        }
        else
        {
            // Frames 1..27 remain fully mocap, including the left lift/step.
            leftFootPlantIK.weight = 0f;
        }

        if (currentStompIndex >= 0 && currentClipSeconds >= stomps[currentStompIndex].releaseEnd)
        {
            if (verifiedPhysicalContacts <= currentStompIndex)
            {
                Abort("Stomp " + (currentStompIndex + 1) +
                      " released without REAL sole contact. Last gate: " + lastContactGate);
                return;
            }
            rightFootIK.weight = 0f;
            completedStomps++;
            nextStompIndex = currentStompIndex + 1;
            currentStompIndex = -1;
            descentHardLocked = false;
        }
        if (currentStompIndex < 0 && nextStompIndex < RequiredStomps &&
            currentClipSeconds >= stomps[nextStompIndex].windupStart)
        {
            if (currentClipSeconds >= stomps[nextStompIndex].descentStart)
            {
                Abort("Missed windup " + (nextStompIndex + 1) + " target snapshot; no late homing.");
                return;
            }
            if (!leftPlantCommitted)
            {
                // Windup #1 begins at the same authored frame as the support
                // plant. Wait for LateUpdate rather than sampling frame 27.
                // The target will still lock before descent or else abort.
                rightFootIK.weight = 0f;
                return;
            }
            if (!CaptureWindup(nextStompIndex)) return;
        }
        if (currentStompIndex >= 0 && !UpdateCurrentStomp(stomps[currentStompIndex])) return;

        if (currentStompIndex == 0 && currentClipSeconds >= stomps[0].visualImpact &&
            currentClipSeconds < stomps[0].recoveryStart)
        {
            Debug.Log("[V017 P0 IK IMPACT UPDATE] frame=" + Time.frameCount +
                " clip=" + currentClipSeconds.ToString("F4") +
                " rightWeight=" + rightFootIK.weight.ToString("F3") +
                " leftWeight=" + leftFootPlantIK.weight.ToString("F3") +
                " reach=" + Vector3.Distance(rightUpperLeg.position, committedRightAnkle).ToString("F3") +
                "/" + maxRightReach.ToString("F3"), this);
        }

        // RigBuilder uses the external targets when Animator evaluates this
        // frame. LateUpdate samples the REAL posed bones/sole after evaluation.
        rigBuilder.SyncLayers();
        // The actual imported AnimationClip may end at 8.766667s, even
        // though the source video decodes 264 frames / 8.800s. Never wait
        // for a nonexistent 8.800s pose. Completion still requires four
        // fully observed, separately VERIFIED sole contacts.
        if (state.normalizedTime >= 0.998f ||
            currentClipSeconds >= state.length - (0.5f / SourceFps))
        {
            if (completedStomps == RequiredStomps && verifiedPhysicalContacts == RequiredStomps)
            {
                // Explicit clean completion is required. Manual StopPreview,
                // Abort, watchdog, and late rig failure must NEVER turn an
                // already-contacted partial sequence into a false PASS.
                naturallyCompletedEntireClip = true;
                StopPreview();
            }
            else
                Abort("Clip finished without four verified physical sole contacts.");
        }
    }

    private bool CommitLeftPlant()
    {
        // After native preparation frames 15..27, sample true posed geometry
        // at frame 28 or during its EXISTING 0.15s settling window, not the
        // stale left ankle/sole from frame 1. Left IK remains OFF until a
        // genuine native planted sole is independently mesh-confirmed.
        Vector3 newSole = GetLeftSole();
        bool strictGroundHit = TryGround(newSole, out Vector3 sampledGround);
        Vector3 ignoredRightMeshSole = default;
        Vector3 meshLeftSole = default;
        bool meshSampled = renderedMeshSoles != null &&
            renderedMeshSoles.TrySample(out ignoredRightMeshSole, out meshLeftSole);
        bool nativeProxyPlanted = strictGroundHit &&
            Mathf.Abs(newSole.y - sampledGround.y) <= initialSoleGroundTolerance;
        bool realMeshPlanted = nativeProxyPlanted && meshSampled &&
            Mathf.Abs(meshLeftSole.y - sampledGround.y) <= initialSoleGroundTolerance &&
            PlanarDistance(meshLeftSole, newSole) <= maxLeftSolePlanarContactError;
        if (!nativeProxyPlanted || !realMeshPlanted)
        {
            // Retain the FIRST sampled native frame-28 pose, including its
            // raw ground and NavMesh diagnostics. It will be logged ONLY if
            // the original bounded capture window genuinely fails.
            if (firstFrame28UngroundedTrace == null)
                firstFrame28UngroundedTrace = BuildFrame28LeftPlantFailureTrace(
                    newSole, strictGroundHit, sampledGround);
            return false;
        }
        leftSoleGround = sampledGround;
        leftSoleOffsetFromAnkle = newSole - leftFoot.position;
        plantedLeftAnkle = leftSoleGround - leftSoleOffsetFromAnkle;
        initialLeftFootRotation = leftFoot.rotation;
        float plantedReach = Vector3.Distance(leftUpperLeg.position, plantedLeftAnkle);
        if (plantedReach > maxLeftReach && !preserveNativeLeftWhenIKUnreachable)
        {
            Abort("Frame-28 left support ankle exceeds 94% anatomical reach.");
            return false;
        }
        if (plantedReach > maxLeftReach)
        {
            // AUTHENTIC NATIVE-LEFT FALLBACK (production opt-in): actual frame28
            // left proxy + skinned mesh have ALREADY proved physical ground.
            // NEVER engage an unreachable IK limb or extend >94%. Retain
            // source mocap, and keep checking native rendered-mesh support
            // at every later frame/contact. Any skating still ABORTS.
            leftFootPlantIK.weight = 0f;
            Debug.Log("[V017 NATIVE LEFT SUPPORT] Grounded original frame28 sole " +
                "verified by proxy+LBS, corrective IK prohibited at reach=" +
                plantedReach.ToString("F3") + "m > 94% max=" +
                maxLeftReach.ToString("F3") +
                "m. Keeping original left animation; later mesh support checks mandatory.", this);
        }
        leftPlantTarget.SetPositionAndRotation(plantedLeftAnkle, initialLeftFootRotation);
        leftPlantCommitted = true;
        firstFrame28UngroundedTrace = null;
        Debug.Log("[V017 Stomp QA] Authored LEFT preparation step retained; planted support anchored at frame 28.", this);
        return true;
    }

    private void AbortFailedFrame28Plant()
    {
        // Always record the FINAL evaluated pose before Abort destroys it,
        // with the first frame-28 failure snapshot if settling was attempted.
        Vector3 nativeSole = GetLeftSole();
        bool strictGroundHit = TryGround(nativeSole, out Vector3 sampledGround);
        string finalTrace = BuildFrame28LeftPlantFailureTrace(
            nativeSole, strictGroundHit, sampledGround);
        Debug.LogWarning("[V017 FRAME28 LEFT PLANT FAIL-ONLY] FIRST=" +
            (firstFrame28UngroundedTrace ?? finalTrace) +
            " FINAL=" + finalTrace +
            " No automatic root correction, tolerance increase, IK stretching " +
            "or synthetic plant was applied.", this);
        Abort("LEFT sole has not planted on the ground at V017 frame 28 " +
            "within its existing " + maxLeftPlantCaptureDelaySeconds.ToString("F3") +
            "s post-frame-28 capture window (real proxy AND deformed mesh required).");
    }

    private string BuildFrame28LeftPlantFailureTrace(Vector3 nativeSole,
        bool strictGroundHit, Vector3 strictGround)
    {
        // This method MUST stay diagnostic-only: no transform/target/weight,
        // terrain, root, NavMeshAgent or pass/fail state is ever modified.
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        float evaluatedSeconds = Mathf.Clamp01(state.normalizedTime) * state.length;
        Vector3 ignoredRightMeshSole = default;
        Vector3 leftMeshSole = default;
        bool hasMeshSole = renderedMeshSoles != null &&
            renderedMeshSoles.TrySample(out ignoredRightMeshSole,
                out leftMeshSole);
        string meshSoleInfo = hasMeshSole ? leftMeshSole.ToString("F4") : "UNAVAILABLE";
        string strictGroundInfo = strictGroundHit
            ? strictGround.ToString("F4") : "NONE (strict height/layer/normal filter)";
        string strictDeviation = strictGroundHit
            ? (nativeSole.y - strictGround.y).ToString("F4") : "UNAVAILABLE";

        // Unlike TryGround, the next two probes deliberately DO NOT reject
        // surfaces above soleY+0.35m. This can expose penetration, an
        // incorrect terrain layer or a NavMesh baseOffset mismatch, but the
        // results are NEVER accepted as a planted sole.
        bool hasMaskedCandidate = TryDiagnosticFrame28Ground(nativeSole,
            terrainLayers, out RaycastHit maskedCandidate);
        bool hasAnyLayerCandidate = TryDiagnosticFrame28Ground(nativeSole,
            ~0, out RaycastHit anyLayerCandidate);

        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (!agent) agent = GetComponentInParent<NavMeshAgent>();
        string navInfo = "NONE";
        if (agent)
        {
            bool onMesh = agent.enabled && agent.isOnNavMesh;
            float scaledOffset = agent.baseOffset * agent.transform.lossyScale.y;
            navInfo = "enabled=" + agent.enabled + " onNavMesh=" + onMesh +
                " agentTransform=" + agent.transform.position.ToString("F4") +
                " agentNext=" + (onMesh ? agent.nextPosition.ToString("F4") : "UNAVAILABLE") +
                " baseOffsetLocal=" + agent.baseOffset.ToString("F5") +
                " baseOffsetWorld=" + scaledOffset.ToString("F4") +
                " agentScaleY=" + agent.transform.lossyScale.y.ToString("F4") +
                " updatePosition=" + agent.updatePosition +
                " isStopped=" + (onMesh ? agent.isStopped.ToString() : "UNAVAILABLE");
        }

        return "frame=" + Time.frameCount +
            " clipUpdate=" + currentClipSeconds.ToString("F5") +
            " clipEvaluated=" + evaluatedSeconds.ToString("F5") +
            " authoredPlant=" + LeftPlantStartSeconds.ToString("F5") +
            " normalized=" + state.normalizedTime.ToString("F6") +
            " stateHash=" + state.fullPathHash +
            " nativeAnkle=" + leftFoot.position.ToString("F4") +
            " nativeSoleProxy=" + nativeSole.ToString("F4") +
            " soleMarker=" + (leftSoleReference
                ? leftSoleReference.position.ToString("F4") : "NONE (foot-bone basis)") +
            " localSoleOffset=" + leftSoleLocalOffset.ToString("F5") +
            " strictRayHit=" + strictGroundHit +
            " strictGround=" + strictGroundInfo +
            " soleMinusStrictGroundY=" + strictDeviation +
            " maximumAllowedAbsError=" + initialSoleGroundTolerance.ToString("F3") +
            " maskedGround=" + FormatFrame28GroundCandidate(hasMaskedCandidate, maskedCandidate, nativeSole) +
            " anyLayerGround=" + FormatFrame28GroundCandidate(hasAnyLayerCandidate, anyLayerCandidate, nativeSole) +
            " deformedLeftMeshSole=" + meshSoleInfo +
            " startLeftGround=" + leftSoleGround.ToString("F4") +
            " startLeftSoleOffset=" + leftSoleOffsetFromAnkle.ToString("F4") +
            " actorRoot=" + transform.position.ToString("F4") +
            " actorRootAtStart=" + startRootPosition.ToString("F4") +
            " animatorRoot=" + animator.transform.position.ToString("F4") +
            " rootDelta=" + (transform.position - startRootPosition).ToString("F4") +
            " nav={" + navInfo + "}" +
            " terrainMask=" + terrainLayers.value;
    }

    private bool TryDiagnosticFrame28Ground(Vector3 nativeSole, int layerMask,
        out RaycastHit nearest)
    {
        nearest = default;
        float nearestAbsHeight = float.PositiveInfinity;
        RaycastHit[] hits = Physics.RaycastAll(nativeSole + Vector3.up * groundRayLift,
            Vector3.down, groundRayDownDistance, layerMask, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            if (!hit.collider || hit.collider.transform.IsChildOf(animator.transform) ||
                hit.normal.y < minGroundNormalY)
                continue;
            float height = Mathf.Abs(nativeSole.y - hit.point.y);
            if (height >= nearestAbsHeight) continue;
            nearest = hit;
            nearestAbsHeight = height;
        }
        return nearestAbsHeight < float.PositiveInfinity;
    }

    private static string FormatFrame28GroundCandidate(bool found,
        RaycastHit candidate, Vector3 nativeSole)
    {
        if (!found) return "NONE";
        return candidate.point.ToString("F4") +
            " soleMinusGroundY=" + (nativeSole.y - candidate.point.y).ToString("F4") +
            " collider=" + candidate.collider.name +
            " layer=" + candidate.collider.gameObject.layer +
            " normal=" + candidate.normal.ToString("F3");
    }

    private bool CaptureWindup(int index)
    {
        // The one and ONLY moving reference sample for this stomp.
        Vector3 capturedAim = previewAimReference.position;
        if (!TryGround(capturedAim, out Vector3 grounded))
        {
            Abort("Windup " + (index + 1) + ": no valid ground beneath Van marker.");
            return false;
        }
        if (Mathf.Abs(grounded.y - leftSoleGround.y) > maxLevelHeightDelta)
        {
            Abort("Windup " + (index + 1) + ": unequal landing and plant ground elevations.");
            return false;
        }
        Vector3 desiredAnkle = grounded - rightSoleOffsetFromAnkle;
        // During windup and lift, the RIGHT IK weight is zero. The authored
        // crouch makes the fixed world landing reachable later in descent.
        // Never bend a leg past 94%: enforce the guard when IK is engaged.
        if (!IsFinite(desiredAnkle))
        {
            Abort("Windup " + (index + 1) + ": invalid ground ankle.");
            return false;
        }
        committedRightSoleGround = grounded;
        committedRightAnkle = desiredAnkle;
        rightIKTarget.SetPositionAndRotation(committedRightAnkle, initialRightFootRotation);
        rightFootIK.weight = 0f; // Mocap lift remains visible.
        highestObservedRightSoleLift = 0f;
        sawRightSoleLift = false;
        highestObservedRightDeformedMeshLift = 0f;
        sawRightDeformedMeshLift = false;
        lastContactGate = "Windup committed; awaiting physical impact.";
        currentStompIndex = index;
        descentHardLocked = false;
        status = "Windup " + (index + 1) + "/4 snapshotted; target does not follow Van.";
        Debug.Log("[V017 Stomp QA] Windup #" + (index + 1) +
                  " committed sole ground " + committedRightSoleGround.ToString("F2"), this);
        return true;
    }

    private bool UpdateCurrentStomp(StompWindow w)
    {
        if (currentClipSeconds >= w.descentStart && !descentHardLocked)
        {
            if (currentClipSeconds >= w.visualImpact)
            {
                Abort("Descent boundary skipped before impact; hard lock cannot be guaranteed.");
                return false;
            }
            descentHardLocked = true;
            status = "Stomp " + (currentStompIndex + 1) + "/4 descent HARD LOCKED.";
        }
        float lockedReachDistance = Vector3.Distance(rightUpperLeg.position, committedRightAnkle);
        if (!IsFinite(lockedReachDistance))
        {
            Abort("Invalid right leg reach on locked descent.");
            return false;
        }
        bool canSolveAt94Percent = lockedReachDistance <= maxRightReach;
        // Animator evaluates *after* this MonoBehaviour Update. At an exact
        // impact transition its pose is still the previous rendered pose,
        // even though state.normalizedTime already crossed visualImpact.
        // Verify the anatomical limit against the evaluated bones in
        // LateUpdate, rather than falsely aborting on a stale hip transform.
        if (currentClipSeconds < w.descentStart)
        {
            rightFootIK.weight = 0f;
        }
        else if (currentClipSeconds < w.visualImpact)
        {
            // Fixed world target, no homing. Keep IK completely OFF until
            // the animated thigh is within the 94% anatomical radius.
            rightFootIK.weight = canSolveAt94Percent
                ? Mathf.SmoothStep(0f, 1f,
                    Mathf.InverseLerp(w.descentStart, w.visualImpact, currentClipSeconds))
                : 0f;
        }
        else if (currentClipSeconds < w.recoveryStart)
        {
            if (!canSolveAt94Percent)
            {
                rightFootIK.weight = 0f;
            }
            else if (verifiedPhysicalContacts > currentStompIndex)
            {
                // The genuine mesh/sole impact has already passed. Give the
                // Animator at least one entire pose-evaluation step to return
                // to native Astra recovery BEFORE its hip begins rising.
                // Only taper AFTER a verified contact, never inside an
                // unverified impact window. At 30 FPS this is approximately
                // frames 53->55 (and their later-stomp counterparts).
                float lookahead = Mathf.Max(Time.deltaTime, 1f / SourceFps);
                float releaseBefore = w.recoveryStart - lookahead;
                float taperStart = Mathf.Min(w.visualImpact, releaseBefore);
                rightFootIK.weight = currentClipSeconds >= releaseBefore ? 0f :
                    1f - Mathf.SmoothStep(0f, 1f,
                        Mathf.InverseLerp(taperStart, releaseBefore, currentClipSeconds));
            }
            else
            {
                rightFootIK.weight = 1f;
            }
        }
        else
        {
            // No future-hip estimate based on the PREVIOUS evaluated pose can
            // safely keep this 40x-scale IK engaged during recovery. Tests
            // observed >94%-reach at t=1.8841/3.2112 despite a small weight.
            // The authored impact is over: release ALL corrective IK before
            // the Animator evaluates its recovery, leaving the world target
            // frozen and the original Astra animation fully authoritative.
            // Missed impacts still fail their normal physical-contact gates.
            rightFootIK.weight = 0f;
        }
        return true;
    }

    private void LateUpdate()
    {
        if (!running) return;
        if (!leftPlantCommitted)
        {
            // The Animator and rig have evaluated since Update. The authored
            // left lift/step finishes at frame 28: do not anchor its frame-27
            // airborne pose simply because Update's clock crossed 0.9s.
            AnimatorStateInfo evaluated = animator.GetCurrentAnimatorStateInfo(0);
            float evaluatedSeconds = Mathf.Clamp01(evaluated.normalizedTime) * evaluated.length;
            if (evaluatedSeconds < LeftPlantStartSeconds) return;
            float nativePlantDeadline = LeftPlantStartSeconds + maxLeftPlantCaptureDelaySeconds;
            if (evaluatedSeconds > nativePlantDeadline)
            {
                // No accept-after-deadline, even if this evaluated pose lands.
                AbortFailedFrame28Plant();
                return;
            }
            // This can remain uncommitted for the original bounded window
            // while Astra's actual native support toe finishes settling.
            // No IK target or plant is manufactured during these frames.
            if (!CommitLeftPlant() && running &&
                evaluatedSeconds >= nativePlantDeadline)
                AbortFailedFrame28Plant();
            return;
        }
        if (currentStompIndex >= 0 && descentHardLocked)
        {
            StompWindow reachWindow = stomps[currentStompIndex];
            // The right target remains frozen after windup. Recovery must
            // stop solving toward it altogether, not chase the mocap ankle:
            // its 40x-scale hip may rise beyond 94% in a single frame.
            bool releasing = currentClipSeconds >= reachWindow.recoveryStart;
            Vector3 currentReachGoal = committedRightAnkle;
            float posedReach = Vector3.Distance(rightUpperLeg.position, currentReachGoal);
            if (rightFootIK.weight > 0.001f && posedReach > maxRightReach)
            {
                if (releasing)
                {
                    // Fail closed if a different Animator/constraint writer
                    // re-engaged IK after our Update-time release. This is a
                    // safety violation, NOT a legitimate four-stomp PASS.
                    float unexpectedWeight = rightFootIK.weight;
                    bool firstUnsafeRelease = !unsafeRecoveryIKObserved;
                    unsafeRecoveryIKObserved = true;
                    rightFootIK.weight = 0f;
                    rigBuilder.SyncLayers();
                    if (firstUnsafeRelease)
                        Debug.LogWarning("[V017 RECOVERY IK GUARD] Unexpected engaged right IK " +
                            "past 94% during native recovery; force-released. clip=" +
                            currentClipSeconds.ToString("F4") + "s dist=" + posedReach.ToString("F3") +
                            "m max=" + maxRightReach.ToString("F3") + "m weight=" +
                            unexpectedWeight.ToString("F3") + ". Full-run PASS disallowed.", this);
                }
                else
                {
                    Abort("RIGHT engaged IK outside 94% after Animator evaluation: clip=" +
                        currentClipSeconds.ToString("F4") + "s dist=" + posedReach.ToString("F3") +
                        "m max=" + maxRightReach.ToString("F3") + "m weight=" +
                        rightFootIK.weight.ToString("F3"));
                    return;
                }
            }
            if (!releasing && verifiedPhysicalContacts <= currentStompIndex &&
                currentClipSeconds >= reachWindow.visualImpact && posedReach > maxRightReach)
            {
                Abort("RIGHT impact outside 94% after Animator evaluation: clip=" +
                    currentClipSeconds.ToString("F4") + "s dist=" + posedReach.ToString("F3") +
                    "m max=" + maxRightReach.ToString("F3") + "m weight=" +
                    rightFootIK.weight.ToString("F3"));
                return;
            }
        }
        // Only enforce the support AFTER its intentional native step landed.
        if (!leftPlantCommitted) return;
        float evaluatedLeftReach = Vector3.Distance(leftUpperLeg.position, plantedLeftAnkle);
        if (leftFootPlantIK.weight > 0.001f && evaluatedLeftReach > maxLeftReach)
        {
            Abort("LEFT support IK engaged beyond 94% anatomical reach.");
            return;
        }
        if (currentClipSeconds > 1.21f && currentClipSeconds < 1.225f)
            Debug.Log("[V017 IK WEIGHT TRACE LATE] frame=" + Time.frameCount +
                " elapsed=" + currentClipSeconds.ToString("F4") +
                " constraintID=" + leftFootPlantIK.GetInstanceID() +
                " lateWeight=" + leftFootPlantIK.weight.ToString("F4") +
                " rigWeight=" + rigBuilder.layers[0].rig.weight.ToString("F4"), this);
        Vector3 leftSole = GetLeftSole();
        lastLeftSoleHeightError = Mathf.Abs(leftSole.y - leftSoleGround.y);
        float leftPlanar = PlanarDistance(leftSole, leftSoleGround);
        float actualLeftAnkleError = Vector3.Distance(leftFoot.position, plantedLeftAnkle);
        if (leftPlanar > maxLeftFootPlanarDrift ||
            lastLeftSoleHeightError > maxLeftFootVerticalDrift ||
            actualLeftAnkleError > maxLeftAnkleIKError)
        {
            Abort("LEFT planted sole/ankle drift after Animation Rigging: clip=" + currentClipSeconds.ToString("F3") +
                  "s planar=" + leftPlanar.ToString("F3") + "m vertical=" + lastLeftSoleHeightError.ToString("F3") +
                  "m ankleError=" + actualLeftAnkleError.ToString("F3") + "m leftIKWeight=" + leftFootPlantIK.weight.ToString("F3") +
                  " target=" + leftPlantTarget.position.ToString("F2") + " actualAnkle=" + leftFoot.position.ToString("F2") +
                  " maxReach=" + maxLeftReach.ToString("F2") + "m hipDistance=" +
                  Vector3.Distance(leftUpperLeg.position, plantedLeftAnkle).ToString("F2") + "m");
            return;
        }
        if (renderedMeshSoles == null ||
            !renderedMeshSoles.TrySample(out Vector3 meshRightSole, out Vector3 meshLeftSole))
        {
            Abort("Actual V017 deformed foot mesh LBS sampling became unavailable.");
            return;
        }
        lastLeftDeformedMeshPlanarError = PlanarDistance(meshLeftSole, leftSoleGround);
        lastLeftDeformedMeshHeightError = Mathf.Abs(meshLeftSole.y - leftSoleGround.y);
        if (lastLeftDeformedMeshPlanarError > maxLeftFootPlanarDrift ||
            lastLeftDeformedMeshHeightError > maxLeftFootVerticalDrift)
        {
            Abort("LEFT real deformed mesh sole drift: clip=" + currentClipSeconds.ToString("F3") +
                  "s planar=" + lastLeftDeformedMeshPlanarError.ToString("F3") +
                  "m vertical=" + lastLeftDeformedMeshHeightError.ToString("F3") + "m.");
            return;
        }
        if (currentStompIndex < 0 || verifiedPhysicalContacts > currentStompIndex) return;
        StompWindow w = stomps[currentStompIndex];
        Vector3 rightSole = GetRightSole();
        if (currentStompIndex == 0 && currentClipSeconds >= w.visualImpact &&
            currentClipSeconds < w.recoveryStart)
        {
            Debug.Log("[V017 P0 IK IMPACT LATE] frame=" + Time.frameCount +
                " clip=" + currentClipSeconds.ToString("F4") +
                " rightWeight=" + rightFootIK.weight.ToString("F3") +
                " leftWeight=" + leftFootPlantIK.weight.ToString("F3") +
                " reach=" + Vector3.Distance(rightUpperLeg.position, committedRightAnkle).ToString("F3") +
                "/" + maxRightReach.ToString("F3"), this);
        }
        if (currentClipSeconds < w.visualImpact)
        {
            float lifted = rightSole.y - committedRightSoleGround.y;
            highestObservedRightSoleLift = Mathf.Max(highestObservedRightSoleLift, lifted);
            if (lifted >= minRightSoleLiftBeforeImpact) sawRightSoleLift = true;
            float meshLifted = meshRightSole.y - committedRightSoleGround.y;
            highestObservedRightDeformedMeshLift =
                Mathf.Max(highestObservedRightDeformedMeshLift, meshLifted);
            if (meshLifted >= minRightSoleLiftBeforeImpact) sawRightDeformedMeshLift = true;
            return;
        }
        // A real contact may be evaluated anywhere in the planted window.
        // No frame count, marker trigger, or target position can pass by itself.
        // Retain the *last actual impact-window* diagnostic. Recovery frames
        // cannot qualify as contact and must not overwrite the reason why
        // the genuine impact window failed.
        if (currentClipSeconds >= w.recoveryStart) return;
        if (!descentHardLocked || !sawRightSoleLift || !sawRightDeformedMeshLift)
        {
            lastContactGate = "Contact-window IK/lift gate: clip=" +
                currentClipSeconds.ToString("F3") + " rightWeight=" +
                rightFootIK.weight.ToString("F3") + " leftWeight=" +
                leftFootPlantIK.weight.ToString("F3") + " rightLift=" +
                sawRightSoleLift + " meshLift=" + sawRightDeformedMeshLift +
                " locked=" + descentHardLocked;
            return;
        }
        lastRightSoleHeightError = Mathf.Abs(rightSole.y - committedRightSoleGround.y);
        lastRightSolePlanarError = PlanarDistance(rightSole, committedRightSoleGround);
        lastContactGate = "Sole alignment: clip=" + currentClipSeconds.ToString("F3") +
            " rightPlanar=" + lastRightSolePlanarError.ToString("F3") +
            " rightHeight=" + lastRightSoleHeightError.ToString("F3") +
            " leftPlanar=" + leftPlanar.ToString("F3") +
            " leftHeight=" + lastLeftSoleHeightError.ToString("F3") +
            " ankle=" + Vector3.Distance(rightFoot.position, committedRightAnkle).ToString("F3");
        if (lastRightSoleHeightError > maxRightSoleVerticalContactError ||
            lastRightSolePlanarError > maxRightSolePlanarContactError ||
            leftPlanar > maxLeftSolePlanarContactError ||
            lastLeftSoleHeightError > maxLeftSoleVerticalContactError ||
            Vector3.Distance(rightFoot.position, committedRightAnkle) > maxRightAnkleIKError)
            return;
        lastRightDeformedMeshHeightError =
            Mathf.Abs(meshRightSole.y - committedRightSoleGround.y);
        lastRightDeformedMeshPlanarError =
            PlanarDistance(meshRightSole, committedRightSoleGround);
        lastContactGate = "Deformed-mesh alignment: clip=" + currentClipSeconds.ToString("F3") +
            " rightMeshPlanar=" + lastRightDeformedMeshPlanarError.ToString("F3") +
            " rightMeshHeight=" + lastRightDeformedMeshHeightError.ToString("F3") +
            " leftMeshPlanar=" + lastLeftDeformedMeshPlanarError.ToString("F3") +
            " leftMeshHeight=" + lastLeftDeformedMeshHeightError.ToString("F3");
        if (lastRightDeformedMeshHeightError > maxRightSoleVerticalContactError ||
            lastRightDeformedMeshPlanarError > maxRightSolePlanarContactError ||
            lastLeftDeformedMeshHeightError > maxLeftSoleVerticalContactError ||
            lastLeftDeformedMeshPlanarError > maxLeftSolePlanarContactError)
            return;
        if (!TryGround(rightSole, out Vector3 actualRightGround) ||
            !TryGround(leftSole, out Vector3 actualLeftGround) ||
            !TryGround(meshRightSole, out Vector3 actualMeshRightGround) ||
            !TryGround(meshLeftSole, out Vector3 actualMeshLeftGround) ||
            Mathf.Abs(rightSole.y - actualRightGround.y) > maxRightSoleVerticalContactError ||
            Mathf.Abs(leftSole.y - actualLeftGround.y) > maxLeftSoleVerticalContactError ||
            Mathf.Abs(meshRightSole.y - actualMeshRightGround.y) > maxRightSoleVerticalContactError ||
            Mathf.Abs(meshLeftSole.y - actualMeshLeftGround.y) > maxLeftSoleVerticalContactError ||
            Mathf.Abs(actualRightGround.y - committedRightSoleGround.y) > maxLevelHeightDelta)
            return;

        // Keep real geometry diagnostics even when Animator/RigBuilder has
        // clobbered the requested constraint weights. Physically grounded
        // source mocap is NOT proof that procedural IK authority worked.
        if (rightFootIK.weight < 0.98f ||
            (!preserveNativeLeftWhenIKUnreachable && leftFootPlantIK.weight < 0.98f))
        {
            lastContactGate = "Real geometry sampled, but IK controller was not authoritative: rightWeight=" +
                rightFootIK.weight.ToString("F3") + " leftWeight=" +
                leftFootPlantIK.weight.ToString("F3") + ".";
            return;
        }

        verifiedPhysicalContacts = currentStompIndex + 1;
        status = "V017 DEFORMED MESH sole contact verified " + verifiedPhysicalContacts + "/4.";
        Debug.Log("[V017 DEFORMED MESH CONTACT] #" + verifiedPhysicalContacts +
                  " right LBS planar=" + lastRightDeformedMeshPlanarError.ToString("F3") +
                  "m height=" + lastRightDeformedMeshHeightError.ToString("F3") +
                  "m left LBS planar=" + lastLeftDeformedMeshPlanarError.ToString("F3") +
                  "m height=" + lastLeftDeformedMeshHeightError.ToString("F3") +
                  "m meshLift=" + highestObservedRightDeformedMeshLift.ToString("F3") +
                  "m; solver emits only physical proof, gameplay receiver is separate.", this);
        Debug.Log("[V017 PHYSICAL CONTACT] #" + verifiedPhysicalContacts +
                  " right sole planar=" + lastRightSolePlanarError.ToString("F3") +
                  "m height=" + lastRightSoleHeightError.ToString("F3") +
                  "m left sole planar=" + leftPlanar.ToString("F3") +
                  "m lift=" + highestObservedRightSoleLift.ToString("F3") +
                  "m; no direct solver damage/CC.", this);
        CertifiedRightSoleGroundContact?.Invoke(verifiedPhysicalContacts,
            actualMeshRightGround);
    }

    private Vector3 GetRightSole()
    {
        Transform basis = rightSoleReference ? rightSoleReference : rightFoot;
        return basis.TransformPoint(rightSoleLocalOffset);
    }

    private Vector3 GetLeftSole()
    {
        Transform basis = leftSoleReference ? leftSoleReference : leftFoot;
        return basis.TransformPoint(leftSoleLocalOffset);
    }

    private bool TryGround(Vector3 worldPosition, out Vector3 ground)
    {
        ground = default;
        float greatestY = float.NegativeInfinity;
        RaycastHit[] hits = Physics.RaycastAll(worldPosition + Vector3.up * groundRayLift,
            Vector3.down, groundRayDownDistance, terrainLayers, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            if (!hit.collider || hit.collider.transform.IsChildOf(animator.transform) ||
                hit.normal.y < minGroundNormalY || hit.point.y <= greatestY ||
                hit.point.y > worldPosition.y + initialSoleGroundTolerance)
                continue;
            ground = hit.point;
            greatestY = hit.point.y;
        }
        return greatestY > float.NegativeInfinity;
    }

    private static bool IsFinite(Vector3 v)
    {
        return float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
    }

    private static bool IsFinite(float f)
    {
        return float.IsFinite(f);
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        return new Vector2(a.x - b.x, a.z - b.z).magnitude;
    }

    private static bool Fail(string reason, out string issue)
    {
        issue = reason;
        return false;
    }

    private void Deny(string reason)
    {
        status = "DENIED V017: " + reason;
        Debug.LogWarning("[V017 Stomp QA] " + status, this);
    }

    private void DenyAndRestore(string reason)
    {
        // The preview did not start successfully: restore the prior idle
        // immediately rather than leave a failed V017 transitional frame.
        RestoreState(true);
        Deny(reason);
    }

    private void Abort(string reason)
    {
        StopPreview();
        status = "ABORTED V017 (no full 4/4 credit; earlier certified player hits may exist): " + reason;
        Debug.LogWarning("[V017 Stomp QA] " + status, this);
    }

    /// <summary>Restore original IK weights, targets, and previously playing state.</summary>
    public void StopPreview()
    {
        if (!running && !cachedRigState && !havePreviousState) return;
        bool passed = running && naturallyCompletedEntireClip && !unsafeRecoveryIKObserved &&
                      completedStomps == RequiredStomps &&
                      verifiedPhysicalContacts == RequiredStomps;
        lastStompPassed = passed;
        running = false;
        naturallyCompletedEntireClip = false;
        RestoreState();
        status = passed
            ? "PASS V017: four REAL right-sole contacts, planted left sole; conditional player effects external."
            : "V017 stopped; previous Animator state and original IK/targets restored.";
        if (passed) Debug.Log("[V017 Stomp QA] " + status, this);
    }

    private void RestoreState(bool immediateAnimatorRestore = false)
    {
        if (cachedRigState)
        {
            if (rightFootIK) rightFootIK.weight = initialRightWeight;
            if (leftFootPlantIK) leftFootPlantIK.weight = initialLeftWeight;
            if (rightIKTarget)
                rightIKTarget.SetPositionAndRotation(initialRightTargetPosition, initialRightTargetRotation);
            if (leftPlantTarget)
                leftPlantTarget.SetPositionAndRotation(initialLeftTargetPosition, initialLeftTargetRotation);
            cachedRigState = false;
        }
        // Explicitly exit V017 on stop/abort. Do not leave its clip looping
        // behind a disabled component and NEVER toggle the Titan AI controller.
        if (havePreviousState && animator && animator.isActiveAndEnabled &&
            animator.gameObject.activeInHierarchy && animator.HasState(0, previousStateHash))
        {
            if (immediateAnimatorRestore || !Application.isPlaying)
                animator.Play(previousStateHash, 0, previousStateNormalizedTime);
            else
                animator.CrossFadeInFixedTime(previousStateHash,
                    returnToPreviousStateCrossfadeSeconds, 0,
                    previousStateNormalizedTime * previousStateLengthSeconds);
            animator.Update(0f);
            havePreviousState = false;
        }
        // When an entire GO is disabled, Animator cannot immediately
        // accept Play(). Keep the restore pending for its next OnEnable.
        if (rigBuilder && rigBuilder.isActiveAndEnabled) rigBuilder.SyncLayers();
    }

    private void OnEnable()
    {
        if (havePreviousState && !running) RestoreState();
    }

    private void OnDisable()
    {
        StopPreview();
    }

    private void OnGUI()
    {
        if (!Application.isPlaying || !showReviewButtons) return;
        GUILayout.BeginArea(new Rect(12, 12, 510, 162),
            "V017 FOUR RIGHT STOMPS | ISOLATED NO-DAMAGE QA", GUI.skin.window);
        if (GUILayout.Button("Begin V017 Preview", GUILayout.Height(28))) BeginPreview();
        if (GUILayout.Button("Stop / Restore Animator + IK", GUILayout.Height(26))) StopPreview();
        GUILayout.Label(status);
        GUILayout.Label("Timed: " + completedStomps + "/4   REAL contacts: " +
            verifiedPhysicalContacts + "/4   V017 clip: " + currentClipSeconds.ToString("F2") + "s");
        GUILayout.EndArea();
    }

    private void OnDrawGizmosSelected()
    {
        if (!running) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(leftSoleGround, 0.3f);
        if (currentStompIndex < 0) return;
        Gizmos.color = descentHardLocked ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(committedRightSoleGround, 0.4f);
        Gizmos.DrawLine(committedRightAnkle, committedRightSoleGround);
    }
}
