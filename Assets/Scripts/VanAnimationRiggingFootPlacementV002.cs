using UnityEngine;
using UnityEngine.Animations.Rigging;

/// <summary>
/// Tutorial-inspired, isolated Animation Rigging implementation for Van.
/// Drives two TwoBoneIK targets + knee hints, two independent foot rotation
/// constraints, and a weighted pelvis height constraint.
/// CharacterController and authored locomotion always own the actual player root.
/// Do not enable simultaneously with VanProceduralFootPlacement.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Animator), typeof(CharacterController), typeof(RigBuilder))]
public sealed class VanAnimationRiggingFootPlacementV002 : MonoBehaviour
{
    [Header("Rig references")]
    [SerializeField] private Rig footRig;
    [SerializeField] private MultiPositionConstraint pelvisConstraint;
    [SerializeField] private Transform pelvisTarget;
    [SerializeField] private TwoBoneIKConstraint leftLeg;
    [SerializeField] private TwoBoneIKConstraint rightLeg;
    [SerializeField] private MultiRotationConstraint leftFootRotation;
    [SerializeField] private MultiRotationConstraint rightFootRotation;
    [SerializeField] private Transform leftTarget;
    [SerializeField] private Transform rightTarget;
    [SerializeField] private Transform leftHint;
    [SerializeField] private Transform rightHint;
    [SerializeField] private Transform leftRotationTarget;
    [SerializeField] private Transform rightRotationTarget;

    [Header("Ground and calibration (all fractions of the scaled leg length)")]
    [SerializeField] private LayerMask groundLayers = Physics.DefaultRaycastLayers;
    [SerializeField, Range(0f, 70f)] private float maxSlopeDegrees = 52f;
    [SerializeField, Range(0.05f, 0.35f)] private float maxCorrectionFraction = 0.20f;
    [SerializeField, Range(0f, 0.25f)] private float maxHipAdjustmentFraction = 0.11f;
    [SerializeField, Range(0f, 0.6f)] private float hipFollowFraction = 0.35f;
    [SerializeField, Range(2f, 30f)] private float targetFollow = 17f;
    [SerializeField, Range(2f, 30f)] private float weightFollow = 12f;

    [Header("Video-style stance and swing")]
    [SerializeField, Range(.45f, .95f)] private float plantWeightThreshold = .72f;
    [SerializeField, Range(.05f, .45f)] private float releaseWeightThreshold = .27f;
    [SerializeField, Range(.12f, .5f)] private float maxPlantedReachFraction = .34f;
    [SerializeField, Range(.025f, .2f)] private float changedTreadFraction = .075f;
    // Scaled leg is ~8.45m in Stage3: 0.009 = ~7.6cm, not 38cm!
    [SerializeField, Range(0f, .035f)] private float landingClearanceFraction = .009f;
    [SerializeField, Range(.05f, .40f)] private float stepArcDuration = .22f;
    [SerializeField, Range(.01f, .24f)] private float stepArcHeightFraction = .10f;

    private Animator animator;
    private CharacterController cc;
    private MovementofPlayer movement;
    private RigBuilder builder;
    private Transform hips;
    private Vector3 hipsRestLocal;
    private float legLength;
    private Vector3 lastWorldScale;
    private bool ready;
    private readonly RaycastHit[] hits = new RaycastHit[32];
    private readonly Foot left = new Foot();
    private readonly Foot right = new Foot();
    private bool highStepLeadingLeft;

    private sealed class Foot
    {
        public Transform ankle, toe, knee, thigh, target, hint, rotationTarget;
        public TwoBoneIKConstraint legConstraint;
        public MultiRotationConstraint rotationConstraint;
        public float weight;
        public float lastForward;
        public bool lastForwardValid;
        public bool surfaceValid;
        public float correctionY;
        public float targetY;
        public float groundY;
        public bool planted;
        public bool readyForPlant = true;
        public Transform plantedSurface;
        public Vector3 plantedLocalTarget;
        public Vector3 plantedLocalGround;
        public Quaternion plantedLocalRotation;
        public Vector3 groundNormal;
        public bool stepping;
        public float stepElapsed;
        public Vector3 stepStart;
        public Vector3 stepFinish;
        public Quaternion stepStartRotation;
        public Quaternion stepFinishRotation;
        public bool externallyGuided;
    }

    public bool IsReady => ready;
    public float LeftWeight => left.weight;
    public float RightWeight => right.weight;
    public bool LeftGrounded => left.surfaceValid;
    public bool RightGrounded => right.surfaceValid;
    public bool LeftPlanted => left.planted;
    public bool RightPlanted => right.planted;
    public Vector3 LeftTargetPosition => left.target ? left.target.position : Vector3.zero;
    public Vector3 RightTargetPosition => right.target ? right.target.position : Vector3.zero;
    public int LeftPlantCount { get; private set; }
    public int RightPlantCount { get; private set; }
    public int RaisedStepCount { get; private set; }
    public int HighObstacleFootLifts { get; private set; }
    public float CurrentHipOffset { get; private set; }
    public int UpdateCount { get; private set; }
    public bool RigGraphValid => builder != null && builder.graph.IsValid();
    public float WorldLegLength => legLength;

    private void Awake()
    {
        Initialize(false);
    }

    // Explicitly consume authored animation root motion. Van's real
    // MovementofPlayer + CharacterController own world-space displacement,
    // not the Mixamo clip or Animation Rigging PlayableGraph.
    // Unity's Animator evaluates RigBuilder's playable output after normal
    // Update; without an OnAnimatorMove handler, that evaluation can restore
    // the root to the clip's reference pose and undo legitimate CC stepping.
    private void OnAnimatorMove()
    {
        // Deliberately no animator.ApplyBuiltinRootMotion().
    }

    /// <summary>
    /// Rebind after a Play-Mode-only Editor setup has added this component
    /// and assigned all serialized target refs. Production prefab instances
    /// initialize normally during Awake.
    /// </summary>
    public bool ReinitializeConfiguredRig()
    {
        bool result = Initialize(true);
        if (result) enabled = true;
        return result;
    }

    /// <summary>
    /// PersistentPlayerRoot can resize the player AFTER the Animator and
    /// RigBuilder initialize. The playable graph keeps its originally bound
    /// root scale, so recalculate and rebind it with the new scale while the
    /// CharacterController is disabled during a stage teleport.
    /// </summary>
    public bool RebuildAfterStageTeleport()
    {
        builder = GetComponent<RigBuilder>();
        if (!builder || !builder.enabled)
            return false;
        builder.Clear();
        if (!builder.Build())
            return false;
        return ReinitializeConfiguredRig();
    }

    private bool Initialize(bool reportFailure)
    {
        animator = GetComponent<Animator>();
        cc = GetComponent<CharacterController>();
        movement = GetComponent<MovementofPlayer>();
        builder = GetComponent<RigBuilder>();
        if (animator == null || !animator.isHuman || footRig == null ||
            pelvisConstraint == null || pelvisTarget == null || builder == null ||
            !ConfigureFoot(left, HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg,
                HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes,
                leftLeg, leftFootRotation, leftTarget, leftHint, leftRotationTarget) ||
            !ConfigureFoot(right, HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg,
                HumanBodyBones.RightFoot, HumanBodyBones.RightToes,
                rightLeg, rightFootRotation, rightTarget, rightHint, rightRotationTarget))
        {
            if (reportFailure)
                Debug.LogError("[VAN RIG V002] Missing Humanoid/constraint/target references.", this);
            enabled = false;
            return false;
        }

        hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        legLength = Vector3.Distance(left.thigh.position, left.knee.position) +
                    Vector3.Distance(left.knee.position, left.ankle.position);
        if (hips == null || legLength < .05f || float.IsNaN(legLength))
        {
            enabled = false;
            return false;
        }
        hipsRestLocal = transform.InverseTransformPoint(hips.position);
        lastWorldScale = transform.lossyScale;
        ready = true;
        footRig.weight = 1f;
        ResetConstraints();
        return true;
    }

    private bool ConfigureFoot(Foot f, HumanBodyBones upper, HumanBodyBones lower,
        HumanBodyBones ankle, HumanBodyBones toe, TwoBoneIKConstraint leg,
        MultiRotationConstraint rot, Transform target, Transform hint, Transform rotationTarget)
    {
        f.thigh = animator.GetBoneTransform(upper);
        f.knee = animator.GetBoneTransform(lower);
        f.ankle = animator.GetBoneTransform(ankle);
        f.toe = animator.GetBoneTransform(toe);
        f.legConstraint = leg;
        f.rotationConstraint = rot;
        f.target = target;
        f.hint = hint;
        f.rotationTarget = rotationTarget;
        return f.thigh && f.knee && f.ankle && f.toe &&
               f.legConstraint && f.rotationConstraint &&
               f.target && f.hint && f.rotationTarget;
    }

    private void OnDisable()
    {
        ResetConstraints();
        CurrentHipOffset = 0f;
    }

    private void ResetConstraints()
    {
        left.weight = right.weight = 0f;
        left.lastForwardValid = right.lastForwardValid = false;
        left.surfaceValid = right.surfaceValid = false;
        ReleasePlant(left, true);
        ReleasePlant(right, true);
        left.externallyGuided = right.externallyGuided = false;
        if (left.legConstraint) left.legConstraint.weight = 0f;
        if (right.legConstraint) right.legConstraint.weight = 0f;
        if (left.rotationConstraint) left.rotationConstraint.weight = 0f;
        if (right.rotationConstraint) right.rotationConstraint.weight = 0f;
        if (pelvisConstraint) pelvisConstraint.weight = 0f;
    }

    private void Update()
    {
        if (!ready) return;
        // PersistentPlayerRoot changes Van from 1x in the menu/stages to 10x
        // in Stage3 after Awake. World-space reach, probes and pelvis offsets
        // must follow the CURRENT scale, not the prefab's initial scale.
        Vector3 currentScale = transform.lossyScale;
        if ((currentScale - lastWorldScale).sqrMagnitude > .0001f)
        {
            legLength = Vector3.Distance(left.thigh.position, left.knee.position) +
                        Vector3.Distance(left.knee.position, left.ankle.position);
            hipsRestLocal = transform.InverseTransformPoint(hips.position);
            lastWorldScale = currentScale;
            ResetConstraints();
        }
        UpdateCount++;

        bool locomotion = cc.enabled && cc.isGrounded &&
            (movement == null || !movement.IsMovementDisabled) &&
            animator.GetCurrentAnimatorStateInfo(0).IsName("Blend Tree") &&
            (!animator.IsInTransition(0) ||
             animator.GetNextAnimatorStateInfo(0).IsName("Blend Tree"));
        float dt = Mathf.Max(Time.deltaTime, .0001f);
        float walkSpeed = animator.GetFloat("Speed");
        float cycle = Mathf.Repeat(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, 1f);
        // Baked from Van's OWN original Walk (0.967s) and Run (0.533s)
        // humanoid toe heights. No original imported clips are modified.
        // OpenKCC-style support states follow the authored gait phase
        // rather than using a one-frame ankle velocity estimate.
        float runMix = Mathf.InverseLerp(.48f, .90f, walkSpeed);
        float leftSupport = walkSpeed <= .12f ? 1f : Mathf.Lerp(
            PhaseSupport(cycle, .22f, .78f), PhaseSupport(cycle, .23f, .57f), runMix);
        float rightSupport = walkSpeed <= .12f ? 1f : Mathf.Lerp(
            PhaseSupport(cycle, .72f, .28f), PhaseSupport(cycle, .73f, .08f), runMix);
        SolveFoot(left, locomotion, dt, leftSupport);
        SolveFoot(right, locomotion, dt, rightSupport);

        // Do not require BOTH feet to be planted: during an actual walk the
        // support leg frequently carries the body alone. Lower the pelvis
        // only if that supporting ankle target is substantially BELOW the
        // authored ankle (e.g. trailing foot on the lower arena tread).
        // This avoids the original V002 crouching on flat swing animations.
        float wantedHip = 0f;
        if (locomotion)
        {
            float lowerReach = 0f;
            if (left.planted && left.weight > .5f)
                lowerReach = Mathf.Min(lowerReach, left.target.position.y - left.ankle.position.y);
            if (right.planted && right.weight > .5f)
                lowerReach = Mathf.Min(lowerReach, right.target.position.y - right.ankle.position.y);
            float deadzone = legLength * .03f;
            if (lowerReach < -deadzone)
                wantedHip = Mathf.Clamp((lowerReach + deadzone) * hipFollowFraction,
                    -legLength * maxHipAdjustmentFraction, 0f);
        }

        float alpha = 1f - Mathf.Exp(-targetFollow * dt);
        CurrentHipOffset = Mathf.Lerp(CurrentHipOffset, wantedHip, alpha);
        // pelvisTarget is a sibling of the animated skeleton and stays
        // anchored to the original root-relative hips height.
        pelvisTarget.position = transform.TransformPoint(hipsRestLocal) +
                                Vector3.up * CurrentHipOffset;
        pelvisConstraint.weight = locomotion
            ? Mathf.Clamp01(Mathf.Abs(CurrentHipOffset) /
                Mathf.Max(.02f, legLength * maxHipAdjustmentFraction))
            : 0f;
    }

    private static float PhaseSupport(float cycle, float start, float end)
    {
        float span = Mathf.Repeat(end - start + 1f, 1f);
        float elapsed = Mathf.Repeat(cycle - start, 1f);
        if (elapsed >= span) return 0f;
        const float edge = .045f;
        float fadeIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / edge));
        float fadeOut = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((span - elapsed) / edge));
        return Mathf.Min(fadeIn, fadeOut);
    }

    private void SolveFoot(Foot f, bool locomotion, float dt, float support)
    {
        if (f.externallyGuided)
        {
            // A collision-validated high step owns this foot while the
            // physical controller climbs. Don't cancel the lifted foot when
            // the still-low animated toe ray hits the previous lower tread.
            f.stepElapsed += dt;
            float phase = Mathf.Clamp01(f.stepElapsed / stepArcDuration);
            float eased = Mathf.SmoothStep(0f, 1f, phase);
            if (f.stepping)
            {
                f.target.position =
                    Vector3.Lerp(f.stepStart, f.stepFinish, eased) +
                    Vector3.up * Mathf.Sin(Mathf.PI * phase) *
                    (legLength * stepArcHeightFraction);
                f.rotationTarget.rotation = Quaternion.Slerp(
                    f.stepStartRotation, f.stepFinishRotation, eased);
                if (phase >= 1f)
                {
                    f.stepping = false;
                    f.planted = true;
                }
            }
            else if (f.planted && f.plantedSurface)
            {
                f.target.position =
                    f.plantedSurface.TransformPoint(f.plantedLocalTarget);
                f.rotationTarget.rotation = f.plantedSurface.rotation *
                                            f.plantedLocalRotation;
            }
            f.surfaceValid = true;
            f.groundY = f.plantedSurface
                ? f.plantedSurface.TransformPoint(f.plantedLocalGround).y
                : f.stepFinish.y;
            f.weight = Mathf.MoveTowards(f.weight, 1f, dt * weightFollow);
            f.legConstraint.weight = f.weight;
            f.rotationConstraint.weight = f.weight;
            UpdateKneeHint(f, dt);
            return;
        }
        Vector3 ankle = f.ankle.position;
        Vector3 toe = f.toe.position;
        float w = 0f;
        Vector3 wanted = ankle;
        Quaternion wantedRot = f.ankle.rotation;
        f.surfaceValid = false;
        f.groundY = 0f;
        f.groundNormal = Vector3.up;
        Transform groundSurface = null;
        Vector3 groundPosition = Vector3.zero;

        if (locomotion && TryGround(toe, out RaycastHit hit))
        {
            // Look slightly forward during an impending landing. The current
            // toe can be on the LOWER tread as the next foot is approaching
            // the raised tread. Never anticipate beyond the native step
            // allowance, and never consider walls taller than the controller
            // may actually climb.
            if (support > .1f && animator.GetFloat("Speed") > .15f &&
                TryGround(toe + transform.forward *
                    Mathf.Min(cc.stepOffset * .42f, legLength * .06f),
                    out RaycastHit ahead) &&
                ahead.point.y > hit.point.y + legLength * .02f &&
                ahead.point.y - hit.point.y < cc.stepOffset + .05f)
                hit = ahead;

            float ankleToeHeight = ankle.y - toe.y;
            float desiredY = hit.point.y + ankleToeHeight +
                legLength * landingClearanceFraction;
            float correction = desiredY - ankle.y;
            f.correctionY = correction;
            if (Mathf.Abs(correction) <= legLength * maxCorrectionFraction)
            {
                f.surfaceValid = true;
                f.groundY = hit.point.y;
                f.groundNormal = hit.normal;
                groundSurface = hit.collider.transform;
                groundPosition = hit.point;
                wanted.y = desiredY;
                wantedRot = Quaternion.FromToRotation(Vector3.up, hit.normal) * f.ankle.rotation;
                w = support;
            }
        }

        // An OpenKCC-style grounded foot stays on its actual surface. In
        // swing (weight near zero) it follows Van's authentic Mixamo motion.
        if (!locomotion || !f.surfaceValid || support <= releaseWeightThreshold)
            ReleasePlant(f, true);
        else if (f.planted)
        {
            Vector3 anchored = f.plantedSurface.TransformPoint(f.plantedLocalTarget);
            Vector3 originalGround = f.plantedSurface.TransformPoint(f.plantedLocalGround);
            float horizontalDistance = Vector2.Distance(
                new Vector2(anchored.x, anchored.z), new Vector2(ankle.x, ankle.z));
            bool changedTread = Mathf.Abs(f.groundY - originalGround.y) >
                Mathf.Max(.08f, legLength * changedTreadFraction);
            bool anotherSurface = groundSurface != f.plantedSurface;
            bool outOfReach = horizontalDistance > legLength * maxPlantedReachFraction ||
                              Mathf.Abs(anchored.y - ankle.y) > legLength * maxCorrectionFraction;
            if (changedTread || anotherSurface)
            {
                // The arena steps are all inside one Boss_Stage collider.
                // When another tread is detected, *lift* into the new target,
                // rather than pinning the previous foot through its face.
                BeginRaisedStep(f, wanted, wantedRot, groundSurface, groundPosition);
            }
            else if (outOfReach)
            {
                ReleasePlant(f, false);
            }
            else
            {
                wanted = anchored;
                wantedRot = f.plantedSurface.rotation * f.plantedLocalRotation;
            }
        }

        if (f.stepping)
        {
            f.stepElapsed += dt;
            float fraction = Mathf.Clamp01(f.stepElapsed / stepArcDuration);
            float eased = Mathf.SmoothStep(0f, 1f, fraction);
            wanted = Vector3.Lerp(f.stepStart, f.stepFinish, eased) +
                     Vector3.up * Mathf.Sin(Mathf.PI * fraction) *
                     (legLength * stepArcHeightFraction);
            wantedRot = Quaternion.Slerp(f.stepStartRotation, f.stepFinishRotation, eased);
            if (fraction >= 1f)
            {
                f.stepping = false;
                f.planted = true;
                wanted = f.plantedSurface.TransformPoint(f.plantedLocalTarget);
                wantedRot = f.plantedSurface.rotation * f.plantedLocalRotation;
            }
        }
        else if (!f.planted && f.readyForPlant && groundSurface &&
                 f.surfaceValid && w >= plantWeightThreshold)
        {
            PlantFoot(f, wanted, wantedRot, groundSurface, groundPosition);
        }

        float smoothing = 1f - Mathf.Exp(-targetFollow * dt);
        f.target.position = f.planted || f.stepping
            ? wanted
            : Vector3.Lerp(f.target.position, wanted, smoothing);
        f.rotationTarget.rotation = Quaternion.Slerp(
            f.rotationTarget.rotation, wantedRot, smoothing);

        // Preserve the authored forward knee plane without locking the knee
        // to the character root as it walks into the raised ground.
        UpdateKneeHint(f, dt);

        f.weight = Mathf.MoveTowards(f.weight,
            f.stepping ? 1f : Mathf.Clamp01(w), dt * weightFollow);
        f.legConstraint.weight = f.weight;
        f.rotationConstraint.weight = f.weight;
    }

    /// <summary>
    /// Lift the leading or trailing foot onto a verified physical ledge.
    /// Only VanGuardedHighStepTraversal calls this after its collider/top
    /// and full-body clearance checks. No animation clip mutation.
    /// </summary>
    public bool BeginHighObstacleStep(Vector3 verifiedTop, Vector3 forward,
        bool trailing)
    {
        if (!ready || !RigGraphValid || !cc.enabled) return false;
        Foot primary = Vector3.Dot(left.toe.position - right.toe.position, forward) >= 0f
            ? left : right;
        Foot f = trailing ? (highStepLeadingLeft ? right : left) : primary;
        if (f.externallyGuided) return true;
        if (TryLandHighFoot(f, verifiedTop, forward))
        {
            if (!trailing) highStepLeadingLeft = ReferenceEquals(f, left);
            return true;
        }
        // On a curved central-ring stair, the foot further forward in the
        // authored clip can be over the LOWER tread, while the other foot is
        // beside a reachable upper tread. Try that foot instead, but only if
        // its actual collider contact and IK reach pass all checks.
        if (!trailing)
        {
            f = ReferenceEquals(primary, left) ? right : left;
            if (TryLandHighFoot(f, verifiedTop, forward))
            {
                highStepLeadingLeft = ReferenceEquals(f, left);
                return true;
            }
        }
        return false;
    }

    private bool TryLandHighFoot(Foot f, Vector3 verifiedTop, Vector3 forward)
    {
        if (f.externallyGuided) return true;

        // Match the chosen foot's lateral position. Height is from a genuine
        // elevated physics surface, plus the authored toe-to-ankle offset.
        Vector3 contact = verifiedTop;
        // Crucial for the REAL Boss_Stage ring: it is curved. Projecting
        // all the way onto the animated toe lane can leave the actual upper
        // tread. Keep the verified top as the first candidate, and test a
        // short patch around it instead.
        Vector3 side = Vector3.Cross(Vector3.up, forward).normalized;
        float towardFoot = Mathf.Sign(Vector3.Dot(
            f.toe.position - contact, side));
        for (int sample = 0; sample < 8; sample++)
        {
            float lateral = (sample % 4) * .34f * towardFoot;
            float longitudinal = sample < 4 ? 0f : -.35f;
            contact = verifiedTop + side * lateral +
                      forward * longitudinal;
        Vector3 rayStart = contact + Vector3.up * Mathf.Max(.3f, legLength * .12f);
        int count = Physics.RaycastNonAlloc(rayStart, Vector3.down,
            hits, Mathf.Max(.7f, legLength * .24f),
            groundLayers, QueryTriggerInteraction.Ignore);
        RaycastHit supported = default;
        float nearest = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            RaycastHit h = hits[i];
            if (!h.collider || h.collider.transform.IsChildOf(transform) ||
                Vector3.Angle(h.normal, Vector3.up) > maxSlopeDegrees ||
                Mathf.Abs(h.point.y - verifiedTop.y) > .28f ||
                h.distance >= nearest)
                continue;
            nearest = h.distance;
            supported = h;
        }
        if (!supported.collider) continue;
        float ankleToeOffset = f.ankle.position.y - f.toe.position.y;
        Vector3 target = supported.point +
            Vector3.up * (ankleToeOffset + legLength * landingClearanceFraction);
        // During the lead phase Van's torso is still on the lower tread.
        // Allow a small, controlled temporary extension until the physical
        // capsule rises, then IK returns to its native reach.
        if (Vector3.Distance(target, f.thigh.position) > legLength * 1.075f ||
            Vector3.Distance(f.toe.position, supported.point) > legLength * .85f)
            continue;

        f.externallyGuided = true;
        f.stepping = true;
        f.planted = false;
        f.readyForPlant = false;
        f.stepStart = f.target.position;
        f.stepFinish = target;
        f.stepStartRotation = f.rotationTarget.rotation;
        f.stepFinishRotation =
            Quaternion.FromToRotation(Vector3.up, supported.normal) *
            f.ankle.rotation;
        f.stepElapsed = 0f;
        f.plantedSurface = supported.collider.transform;
        f.plantedLocalTarget = f.plantedSurface.InverseTransformPoint(target);
        f.plantedLocalGround =
            f.plantedSurface.InverseTransformPoint(supported.point);
        f.plantedLocalRotation =
            Quaternion.Inverse(f.plantedSurface.rotation) * f.stepFinishRotation;
        HighObstacleFootLifts++;
        return true;
        }
        return false;
    }

    public void EndHighObstacleStep()
    {
        left.externallyGuided = right.externallyGuided = false;
        highStepLeadingLeft = false;
        ReleasePlant(left, true);
        ReleasePlant(right, true);
    }

    private void UpdateKneeHint(Foot f, float dt)
    {
        Vector3 kneeOut = f.knee.position -
            Vector3.Lerp(f.thigh.position, f.ankle.position, .5f);
        if (kneeOut.sqrMagnitude < .0001f)
            kneeOut = transform.forward;
        Vector3 hint = f.knee.position + kneeOut.normalized * legLength * .18f;
        f.hint.position = Vector3.Lerp(f.hint.position, hint,
            1f - Mathf.Exp(-targetFollow * dt));
    }

    private void PlantFoot(Foot f, Vector3 position, Quaternion rotation,
        Transform surface, Vector3 ground)
    {
        if (!surface) return;
        f.planted = true;
        f.readyForPlant = true;
        f.plantedSurface = surface;
        f.plantedLocalTarget = surface.InverseTransformPoint(position);
        f.plantedLocalGround = surface.InverseTransformPoint(ground);
        f.plantedLocalRotation = Quaternion.Inverse(surface.rotation) * rotation;
        f.target.position = position;
        f.rotationTarget.rotation = rotation;
        if (ReferenceEquals(f, left)) LeftPlantCount++;
        else RightPlantCount++;
    }

    private void BeginRaisedStep(Foot f, Vector3 goal, Quaternion rotation,
        Transform surface, Vector3 ground)
    {
        if (!surface) return;
        f.stepStart = f.target.position;
        f.stepStartRotation = f.rotationTarget.rotation;
        f.stepFinish = goal;
        f.stepFinishRotation = rotation;
        f.stepElapsed = 0f;
        f.planted = false;
        f.stepping = true;
        f.plantedSurface = surface;
        f.plantedLocalTarget = surface.InverseTransformPoint(goal);
        f.plantedLocalGround = surface.InverseTransformPoint(ground);
        f.plantedLocalRotation = Quaternion.Inverse(surface.rotation) * rotation;
        RaisedStepCount++;
    }

    private static void ReleasePlant(Foot foot, bool allowReplant)
    {
        foot.planted = false;
        foot.stepping = false;
        foot.plantedSurface = null;
        foot.readyForPlant = allowReplant;
    }

    private bool TryGround(Vector3 toe, out RaycastHit hit)
    {
        float rise = legLength * .25f;
        int count = Physics.RaycastNonAlloc(toe + Vector3.up * rise,
            Vector3.down, hits, legLength * .70f,
            groundLayers, QueryTriggerInteraction.Ignore);
        hit = default;
        float nearest = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            RaycastHit current = hits[i];
            if (!current.collider || current.distance >= nearest ||
                current.collider.transform.IsChildOf(transform) ||
                Vector3.Angle(current.normal, Vector3.up) > maxSlopeDegrees ||
                (current.rigidbody != null && !current.rigidbody.isKinematic))
                continue;
            nearest = current.distance;
            hit = current;
        }
        return nearest < float.PositiveInfinity;
    }
}
