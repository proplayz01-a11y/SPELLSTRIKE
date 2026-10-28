using UnityEngine;

/// <summary>
/// Terrain-aware Humanoid foot IK for Van's existing Mixamo walk/run blend tree.
/// The Animator/CharacterController remain authoritative for locomotion,
/// combat, jump, knockback and root motion. This component only corrects
/// ankle height/tilt against real walkable colliders during grounded locomotion.
///
/// Requires the Base Layer "IK Pass" on VanANIMCONTROLLER.
/// Uses bone-derived world measurements instead of hard-coded metres because
/// the production Stage 3 Van instance has a 10.71x scale.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Animator), typeof(CharacterController))]
public sealed class VanProceduralFootPlacement : MonoBehaviour
{
    [Header("Safety")]
    [SerializeField] private bool enableTerrainFootIK = true;
    [SerializeField, Range(0f, 70f)] private float maximumGroundSlope = 52f;
    [SerializeField] private LayerMask surfaceLayers = Physics.DefaultRaycastLayers;

    [Header("World offsets relative to each leg's physical length")]
    [SerializeField, Range(0f, 0.075f)]
    private float soleClearanceFraction = 0.052f;
    [SerializeField, Range(0.05f, 0.3f)]
    private float maximumVerticalCorrectionFraction = 0.17f;
    [SerializeField, Range(0.01f, 0.2f)]
    private float swingLiftStartFraction = 0.055f;
    [SerializeField, Range(0.06f, 0.35f)]
    private float swingLiftEndFraction = 0.19f;
    [Header("Foot planting")]
    [SerializeField, Range(0.4f, 0.95f)] private float plantAtIKWeight = 0.74f;
    [SerializeField, Range(0.05f, 0.65f)] private float releaseAtIKWeight = 0.28f;
    [SerializeField, Range(0.12f, 0.48f)]
    private float maximumPlantReachFraction = 0.32f;

    [Header("Smoothing")]
    [SerializeField, Range(2f, 35f)] private float ikFadeSpeed = 13f;
    [SerializeField, Range(2f, 35f)] private float groundTargetFollowSpeed = 30f;

    private readonly RaycastHit[] groundHits = new RaycastHit[24];
    private Animator animator;
    private CharacterController controller;
    private MovementofPlayer movement;
    private Foot left;
    private Foot right;
    private bool ready;
    private float legLengthWorld;

    private sealed class Foot
    {
        public AvatarIKGoal goal;
        public Transform ankle;
        public Transform toes;
        public Vector3 smoothedTarget;
        public Vector3 smoothedNormal = Vector3.up;
        public float weight;
        public bool hasTarget;
        public bool planted;
        public bool readyForNextPlant = true;
        public Transform plantedSurface;
        public Vector3 plantedLocalAnkle;
        public Vector3 plantedLocalNormal;
        public float previousAuthoredForward;
        public bool hasPreviousForward;
        public float authoredForwardVelocity;
        public float toeClearanceCompensation;
    }

    // Read-only telemetry, useful for proving that the real Animator IK pass ran.
    public int AnimatorIKFrameCount { get; private set; }
    public float LeftIKWeight => left != null ? left.weight : 0f;
    public float RightIKWeight => right != null ? right.weight : 0f;
    public bool LastLeftGroundValid { get; private set; }
    public bool LastRightGroundValid { get; private set; }
    public bool LeftFootPlanted => left != null && left.planted;
    public bool RightFootPlanted => right != null && right.planted;
    public Vector3 LeftIKTarget => left != null ? left.smoothedTarget : Vector3.zero;
    public Vector3 RightIKTarget => right != null ? right.smoothedTarget : Vector3.zero;
    public float LeftAuthoredForwardSpeed => left != null ? left.authoredForwardVelocity : 0f;
    public float RightAuthoredForwardSpeed => right != null ? right.authoredForwardVelocity : 0f;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
        movement = GetComponent<MovementofPlayer>();

        if (!animator || !animator.isHuman || !animator.avatar ||
            !animator.avatar.isValid || !animator.avatar.isHuman)
        {
            Debug.LogWarning("[VAN FOOT IK] Requires Van's valid Humanoid Animator. IK disabled.", this);
            enabled = false;
            return;
        }

        left = CreateFoot(AvatarIKGoal.LeftFoot, HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes);
        right = CreateFoot(AvatarIKGoal.RightFoot, HumanBodyBones.RightFoot, HumanBodyBones.RightToes);
        if (left == null || right == null)
        {
            Debug.LogWarning("[VAN FOOT IK] Humanoid ankle/toe bones unavailable. IK disabled.", this);
            enabled = false;
            return;
        }

        // Measure two actual bone segments so this works for world scales
        // from 1x (canonical prefab) to the 10.71x Stage 3 scene instance.
        Transform thigh = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        Transform knee = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        if (thigh == null || knee == null)
        {
            enabled = false;
            return;
        }
        legLengthWorld = Vector3.Distance(thigh.position, knee.position)
                       + Vector3.Distance(knee.position, left.ankle.position);
        ready = legLengthWorld > 0.05f &&
                !float.IsNaN(legLengthWorld) && !float.IsInfinity(legLengthWorld);
        if (!ready)
        {
            Debug.LogWarning("[VAN FOOT IK] Invalid scaled leg length. IK disabled.", this);
            enabled = false;
        }
    }

    private Foot CreateFoot(AvatarIKGoal goal, HumanBodyBones ankleBone, HumanBodyBones toeBone)
    {
        Transform ankle = animator.GetBoneTransform(ankleBone);
        Transform toes = animator.GetBoneTransform(toeBone);
        return ankle != null && toes != null
            ? new Foot { goal = goal, ankle = ankle, toes = toes }
            : null;
    }

    private void OnDisable()
    {
        ResetFootState(left);
        ResetFootState(right);
        LastLeftGroundValid = false;
        LastRightGroundValid = false;
    }

    private static void ResetFootState(Foot foot)
    {
        if (foot == null) return;
        foot.weight = 0f;
        foot.hasTarget = false;
        foot.planted = false;
        foot.readyForNextPlant = true;
        foot.plantedSurface = null;
        foot.hasPreviousForward = false;
        foot.authoredForwardVelocity = 0f;
        foot.toeClearanceCompensation = 0f;
        foot.smoothedNormal = Vector3.up;
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (!ready || layerIndex != 0)
            return;

        AnimatorIKFrameCount++;
        bool locomoting = enableTerrainFootIK && animator.isActiveAndEnabled &&
            controller && controller.enabled && controller.isGrounded &&
            (movement == null || !movement.IsMovementDisabled) &&
            animator.GetCurrentAnimatorStateInfo(0).IsName("Blend Tree") &&
            (!animator.IsInTransition(0) ||
             animator.GetNextAnimatorStateInfo(0).IsName("Blend Tree"));

        float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
        LastLeftGroundValid = SolveFoot(left, locomoting, deltaTime);
        LastRightGroundValid = SolveFoot(right, locomoting, deltaTime);
    }

    private bool SolveFoot(Foot foot, bool locomoting, float deltaTime)
    {
        Vector3 authoredAnkle = animator.GetIKPosition(foot.goal);
        Quaternion authoredRotation = animator.GetIKRotation(foot.goal);
        // Use the PRE-IK animated ankle, relative to Van's root, for swing
        // detection. Reading the deformed toe after IK can trap a planted
        // foot near the floor throughout the next walk cycle.
        float forwardOffset = Vector3.Dot(
            authoredAnkle - transform.position, transform.forward);
        float authoredForwardSpeed = foot.hasPreviousForward && deltaTime > 0.0001f
            ? (forwardOffset - foot.previousAuthoredForward) / deltaTime
            : 0f;
        if (!foot.hasPreviousForward ||
            Mathf.Abs(forwardOffset - foot.previousAuthoredForward) > legLengthWorld * 0.5f)
            authoredForwardSpeed = 0f; // teleport / initial gait transition
        foot.previousAuthoredForward = forwardOffset;
        foot.hasPreviousForward = true;
        foot.authoredForwardVelocity = authoredForwardSpeed;

        bool hasWalkCycle = locomoting && animator.GetFloat("Speed") > 0.15f;
        bool forwardSwing = hasWalkCycle &&
            authoredForwardSpeed > legLengthWorld * 0.15f;
        float intendedWeight = 0f;
        bool grounded = false;
        Vector3 goalPosition = authoredAnkle;
        Vector3 goalNormal = Vector3.up;
        Transform supportedSurface = null;

        if (locomoting && FindFootGround(foot, out RaycastHit hit))
        {
            // Preserve the Animator's CURRENT ankle-to-toe vertical difference,
            // including ankle pitch. A fixed ankle offset would float/sink Van.
            float ankleToToe = foot.ankle.position.y - foot.toes.position.y;
            float animatedToeToFloor = foot.toes.position.y - hit.point.y;
            float maxCorrection = legLengthWorld * maximumVerticalCorrectionFraction;
            float liftStart = legLengthWorld * swingLiftStartFraction;
            float liftEnd = Mathf.Max(liftStart + 0.01f, legLengthWorld * swingLiftEndFraction);

            // The swinging foot follows its authored gait. Only the near-ground
            // stance portion gets IK, so legs cannot be pinned mid-stride.
            float swingProgress = Mathf.InverseLerp(
                liftStart, liftEnd, Mathf.Max(0f, animatedToeToFloor));
            intendedWeight = 1f - Mathf.SmoothStep(0f, 1f, swingProgress);
            if (forwardSwing)
                intendedWeight = 0f;
            float requestedShift = hit.point.y +
                legLengthWorld * soleClearanceFraction + ankleToToe - authoredAnkle.y;
            if (Mathf.Abs(requestedShift) <= maxCorrection)
            {
                goalPosition = authoredAnkle + Vector3.up * requestedShift;
                goalNormal = hit.normal;
                supportedSurface = hit.collider.transform;
                grounded = true;
            }
            else
            {
                intendedWeight = 0f;
            }
        }

        // One planted foot must stay in one *world-space* place while the
        // pelvis moves forward. Otherwise merely correcting Y still produces
        // unmistakable foot sliding on stairs and slopes. The anchor is stored
        // in collider-local space so kinematic moving platforms work too.
        // The next swing must free the foot before another plant can begin.
        if (!locomoting || !grounded || intendedWeight <= releaseAtIKWeight)
        {
            foot.planted = false;
            foot.readyForNextPlant = true;
            foot.plantedSurface = null;
            foot.toeClearanceCompensation = 0f;
        }
        else if (foot.planted)
        {
            Vector3 plantedWorld = foot.plantedSurface
                ? foot.plantedSurface.TransformPoint(foot.plantedLocalAnkle)
                : foot.smoothedTarget;
            float horizontalReach = Vector2.Distance(
                new Vector2(plantedWorld.x, plantedWorld.z),
                new Vector2(authoredAnkle.x, authoredAnkle.z));
            float verticalReach = Mathf.Abs(plantedWorld.y - authoredAnkle.y);
            if (horizontalReach > legLengthWorld * maximumPlantReachFraction ||
                verticalReach > legLengthWorld * maximumVerticalCorrectionFraction ||
                (supportedSurface != null && foot.plantedSurface != supportedSurface))
            {
                // Never pin a limb to a distant step or a different surface.
                // Wait for an actual swing before allowing it to replant.
                foot.planted = false;
                foot.readyForNextPlant = false;
                foot.plantedSurface = null;
                foot.toeClearanceCompensation = 0f;
            }
            else
            {
                goalPosition = plantedWorld;
                if (foot.plantedSurface)
                    goalNormal = foot.plantedSurface.TransformDirection(
                        foot.plantedLocalNormal).normalized;
            }
        }

        if (grounded && !foot.planted && foot.readyForNextPlant &&
            intendedWeight >= plantAtIKWeight && supportedSurface &&
            (!hasWalkCycle || authoredForwardSpeed <= 0f))
        {
            foot.planted = true;
            foot.plantedSurface = supportedSurface;
            foot.plantedLocalAnkle = supportedSurface.InverseTransformPoint(goalPosition);
            foot.plantedLocalNormal = supportedSurface.InverseTransformDirection(
                goalNormal).normalized;
            foot.toeClearanceCompensation = 0f;
        }

        if (foot.planted)
            goalPosition.y += foot.toeClearanceCompensation;

        float fade = 1f - Mathf.Exp(-ikFadeSpeed * deltaTime);
        foot.weight = Mathf.MoveTowards(foot.weight, intendedWeight, fade);

        if (grounded && intendedWeight > 0.001f)
        {
            float smooth = 1f - Mathf.Exp(-groundTargetFollowSpeed * deltaTime);
            if (!foot.hasTarget)
            {
                foot.smoothedTarget = goalPosition;
                foot.smoothedNormal = goalNormal;
                foot.hasTarget = true;
            }
            else
            {
                // Smooth terrain corrections; during stance, converge on the
                // planted world-space ankle, rather than dragging with the
                // player's root transform.
                foot.smoothedTarget = Vector3.Lerp(
                    foot.smoothedTarget, goalPosition, smooth);
                foot.smoothedNormal = Vector3.Slerp(
                    foot.smoothedNormal, goalNormal, smooth).normalized;
            }
        }
        else if (foot.weight < 0.01f)
        {
            foot.hasTarget = false;
        }

        animator.SetIKPositionWeight(foot.goal, foot.weight);
        animator.SetIKRotationWeight(foot.goal, foot.weight);
        if (foot.weight > 0.001f && foot.hasTarget)
        {
            // X/Z stays animated during swing, but is locked to the foot
            // anchor during ground contact. Blending prevents hard snaps.
            Vector3 adjusted = foot.planted
                ? foot.smoothedTarget
                : new Vector3(authoredAnkle.x, foot.smoothedTarget.y, authoredAnkle.z);
            animator.SetIKPosition(foot.goal, adjusted);
            Quaternion terrainTilt = Quaternion.FromToRotation(Vector3.up, foot.smoothedNormal);
            animator.SetIKRotation(foot.goal, terrainTilt * authoredRotation);
        }

        return grounded && intendedWeight > 0.001f;
    }

    private void LateUpdate()
    {
        // Toe bones can swing below the surface even when the ankle IK goal
        // is grounded, especially on the scaled Mixamo rig. Measure the
        // *solved* toe after Animator IK and add only the clearance required
        // for the next frame. No pelvis, CC, or animation clip changes.
        if (!ready || !controller || !controller.isGrounded)
            return;
        CorrectPlantedToe(left);
        CorrectPlantedToe(right);
    }

    private void CorrectPlantedToe(Foot foot)
    {
        if (foot == null || !foot.planted || foot.weight < 0.65f ||
            !foot.plantedSurface || !FindFootGround(foot, out RaycastHit hit) ||
            hit.collider.transform != foot.plantedSurface)
            return;

        float clearance = foot.toes.position.y - hit.point.y;
        float desired = legLengthWorld * 0.012f;
        if (clearance < desired)
        {
            foot.toeClearanceCompensation = Mathf.Min(
                legLengthWorld * 0.075f,
                foot.toeClearanceCompensation + (desired - clearance) * 0.7f);
        }
        else if (clearance > desired + legLengthWorld * 0.017f)
        {
            foot.toeClearanceCompensation = Mathf.MoveTowards(
                foot.toeClearanceCompensation, 0f,
                legLengthWorld * 0.16f * Time.deltaTime);
        }
    }

    private bool FindFootGround(Foot foot, out RaycastHit best)
    {
        float raise = legLengthWorld * 0.28f;
        Vector3 origin = foot.toes.position + Vector3.up * raise;
        float probeDistance = legLengthWorld * 0.68f;
        int count = Physics.RaycastNonAlloc(origin, Vector3.down, groundHits,
            probeDistance, surfaceLayers, QueryTriggerInteraction.Ignore);
        best = default;
        float bestDistance = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            RaycastHit candidate = groundHits[i];
            Collider collider = candidate.collider;
            if (!collider || candidate.distance >= bestDistance ||
                collider.transform.IsChildOf(transform) ||
                Vector3.Angle(candidate.normal, Vector3.up) > maximumGroundSlope)
                continue;
            Rigidbody body = collider.attachedRigidbody;
            if (body != null && !body.isKinematic)
                continue;
            best = candidate;
            bestDistance = candidate.distance;
        }
        return bestDistance < float.PositiveInfinity;
    }
}
