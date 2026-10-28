using UnityEngine;

/// <summary>
/// A guarded physical traversal for steps too tall for native CC.stepOffset.
/// Leading foot reaches a verified landing, then the real CharacterController
/// climbs via collision-aware Move calls. No transform teleport, noclip,
/// Rigidbody force, bypass of attack locks, or per-frame allocations.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public sealed class VanGuardedHighStepTraversal : MonoBehaviour
{
    [SerializeField, Range(.17f, .32f)] private float kneeReachAsCapsuleHeight = .255f;
    [SerializeField, Range(.12f, .45f)] private float footLeadSeconds = .24f;
    [SerializeField, Range(.15f, 1f)] private float verticalClimbSeconds = .60f;
    [SerializeField, Range(0f, 70f)] private float maximumLandingSlope = 42f;
    [SerializeField] private LayerMask obstacles = Physics.DefaultRaycastLayers;

    private enum TraversalPhase { None, LeadFoot, LiftBody, CarryAcross }
    private TraversalPhase phase;
    private CharacterController cc;
    private Animator animator;
    private VanAnimationRiggingFootPlacementV002 feet;
    private readonly RaycastHit[] rayHits = new RaycastHit[32];
    private readonly Collider[] occupied = new Collider[32];
    private Vector3 approachDirection;
    private Vector3 ledgeContact;
    private Vector3 landing;
    private float ledgeHeight;
    private float startingBottom;
    private float phaseTime;
    private float horizontalProgress;
    private float blockedTime;
    private float targetRise;
    private bool requestedTrailingFoot;

    public bool IsTraversing => phase != TraversalPhase.None;
    public bool IsRaisingBody => phase == TraversalPhase.LiftBody;
    public float TargetLandingY => ledgeHeight;
    public float CurrentStepRise => targetRise;
    public int CompletedHighSteps { get; private set; }
    public int RejectedHighSteps { get; private set; }
    public string LastRejectedClearance { get; private set; } = "none";

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        feet = GetComponent<VanAnimationRiggingFootPlacementV002>();
    }

    private void OnDisable() => CancelStep();

    /// <summary>Called by MovementofPlayer just before normal horizontal CC.Move.</summary>
    public bool TryMove(Vector3 desiredHorizontal, float dt, out float animationFraction)
    {
        animationFraction = 0f;
        if (!cc || !cc.enabled || dt <= 0f)
            return false;

        Vector3 horizontal = Vector3.ProjectOnPlane(desiredHorizontal, Vector3.up);
        float distance = horizontal.magnitude;
        if (distance <= .0001f)
        {
            if (IsTraversing) CancelStep();
            return false;
        }
        Vector3 direction = horizontal / distance;
        if (IsTraversing && Vector3.Dot(direction, approachDirection) < .75f)
        {
            CancelStep();
            return false;
        }
        if (!IsTraversing)
        {
            if (!cc.isGrounded || !feet || !feet.enabled ||
                !feet.IsReady || !feet.RigGraphValid ||
                !TryFindSafeHighStep(direction, out ledgeContact, out landing,
                    out ledgeHeight, out targetRise))
                return false;
            approachDirection = direction;
            startingBottom = cc.bounds.min.y;
            phase = TraversalPhase.LeadFoot;
            phaseTime = 0f;
            horizontalProgress = 0f;
            blockedTime = 0f;
            requestedTrailingFoot = false;
            // No fake foot contacts. The selected surface came from a real
            // upward-facing collider raycast and checked capsule headroom.
            if (!feet.BeginHighObstacleStep(landing, approachDirection, false))
            {
                CancelStep();
                return false;
            }
        }

        animationFraction = .85f; // maintain gait for intentional step action
        float timeStep = Mathf.Min(dt, .05f);
        phaseTime += timeStep;
        if (phase == TraversalPhase.LeadFoot)
        {
            if (phaseTime >= footLeadSeconds)
            {
                phase = TraversalPhase.LiftBody;
                phaseTime = 0f;
            }
        }
        else if (phase == TraversalPhase.LiftBody)
        {
            float remaining = Mathf.Max(0f, targetRise + .09f -
                (cc.bounds.min.y - startingBottom));
            // Smooth upward motion with genuine CC collision checks.
            float vertical = Mathf.Min(remaining,
                Mathf.Max(cc.height * Mathf.Abs(transform.lossyScale.y) * .24f,
                          targetRise / verticalClimbSeconds) * timeStep);
            float beforeY = cc.bounds.min.y;
            if (vertical > .0005f)
            {
                CollisionFlags flags = cc.Move(Vector3.up * vertical);
                if ((flags & CollisionFlags.Above) != 0 ||
                    cc.bounds.min.y - beforeY < vertical * .35f)
                {
                    CancelStep();
                    return true;
                }
            }
            if (!requestedTrailingFoot &&
                cc.bounds.min.y - startingBottom > targetRise * .55f)
            {
                requestedTrailingFoot = feet.BeginHighObstacleStep(
                    landing, approachDirection, true);
            }
            if (cc.bounds.min.y - startingBottom >= targetRise + .045f)
            {
                phase = TraversalPhase.CarryAcross;
                phaseTime = 0f;
            }
        }
        else if (phase == TraversalPhase.CarryAcross)
        {
            float before = Vector3.Dot(transform.position, approachDirection);
            cc.Move(horizontal);
            cc.Move(Vector3.down * Mathf.Min(timeStep * 2f, .08f));
            float actual = Mathf.Max(0f,
                Vector3.Dot(transform.position, approachDirection) - before);
            horizontalProgress += actual;
            blockedTime = actual < distance * .15f
                ? blockedTime + timeStep : 0f;
            animationFraction = Mathf.Clamp01(actual / distance);
            if (blockedTime > .35f)
            {
                CancelStep();
                return true;
            }
            // Move the whole collision capsule past the raised tread before
            // normal gravity/animation resumes, while still obeying CC.Move.
            float worldRadius = cc.radius *
                Mathf.Max(Mathf.Abs(transform.lossyScale.x),
                          Mathf.Abs(transform.lossyScale.z));
            if (horizontalProgress >= worldRadius * 1.8f + .6f)
            {
                CompletedHighSteps++;
                CancelStep();
            }
        }
        return true;
    }

    public void CancelStep()
    {
        if (phase != TraversalPhase.None && feet)
            feet.EndHighObstacleStep();
        phase = TraversalPhase.None;
        requestedTrailingFoot = false;
    }

    private bool TryFindSafeHighStep(Vector3 direction,
        out Vector3 contact, out Vector3 top, out float topY, out float rise)
    {
        contact = top = Vector3.zero;
        topY = rise = 0f;
        // A wide capsule may already be lifted against the rounded corner
        // of a real stair while its support foot still stands on the lower
        // tread. The capsule bottom is NOT the lower tread height. Sample
        // the actual walkable surface directly under Van.
        float physicalBottom = cc.bounds.min.y;
        float floor = physicalBottom;
        float worldHeight = cc.height * Mathf.Abs(transform.lossyScale.y);
        float radius = cc.radius * Mathf.Max(
            Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z));
        int supportCount = Physics.RaycastNonAlloc(
            transform.position + Vector3.up * Mathf.Max(2f, worldHeight * .35f),
            Vector3.down, rayHits, worldHeight * 1.4f,
            obstacles, QueryTriggerInteraction.Ignore);
        float supportY = float.NegativeInfinity;
        for (int i = 0; i < supportCount; i++)
        {
            RaycastHit support = rayHits[i];
            if (!IsUsable(support.collider) ||
                Vector3.Angle(support.normal, Vector3.up) > maximumLandingSlope ||
                support.point.y > physicalBottom + .20f ||
                support.point.y <= supportY)
                continue;
            supportY = support.point.y;
        }
        if (!float.IsNegativeInfinity(supportY))
            floor = supportY;
        float kneeMax = worldHeight * kneeReachAsCapsuleHeight;
        if (animator && animator.isHuman)
        {
            Transform knee = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            Transform ankle = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            if (knee && ankle)
                kneeMax = Mathf.Min(kneeMax,
                    Mathf.Max(0f, knee.position.y - floor - worldHeight * .018f));
        }
        float minRise = cc.stepOffset + .09f;
        if (kneeMax < minRise + .05f)
            return false;

        Vector3 side = Vector3.Cross(Vector3.up, direction).normalized;
        float probeLength = radius + Mathf.Max(.4f, worldHeight * .09f);
        float bestDistance = float.PositiveInfinity;
        RaycastHit wall = default;
        for (int lane = -1; lane <= 1; lane++)
        {
            Vector3 origin = transform.position +
                side * (lane * radius * .55f);
            origin.y = floor + Mathf.Min(worldHeight * .024f, .40f);
            int n = Physics.RaycastNonAlloc(origin, direction, rayHits,
                probeLength, obstacles, QueryTriggerInteraction.Ignore);
            for (int j = 0; j < n; j++)
            {
                RaycastHit h = rayHits[j];
                if (!IsUsable(h.collider) ||
                    Vector3.Dot(h.normal, direction) > -.45f ||
                    h.distance >= bestDistance)
                    continue;
                wall = h;
                bestDistance = h.distance;
            }
        }
        if (!wall.collider) return false;

        // Find the real top immediately behind the front face of the obstacle,
        // not some remote floor behind an otherwise impassable wall.
        for (int sample = 0; sample < 6; sample++)
        {
            float behind = Mathf.Min(radius * .92f,
                Mathf.Max(.35f, worldHeight * (.045f + sample * .038f)));
            Vector3 origin = wall.point + direction * behind;
            origin.y = floor + kneeMax + Mathf.Max(.30f, worldHeight * .06f);
            float castLength = kneeMax + worldHeight * .12f;
            int n = Physics.RaycastNonAlloc(origin, Vector3.down,
                rayHits, castLength, obstacles, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                RaycastHit hit = rayHits[i];
                if (!IsUsable(hit.collider) ||
                    Vector3.Angle(hit.normal, Vector3.up) > maximumLandingSlope)
                    continue;
                float delta = hit.point.y - floor;
                if (delta < minRise || delta > kneeMax ||
                    hit.collider != wall.collider)
                    continue;
                // The capsule must be able to travel upward without passing
                // through an overhead solid, and occupy the upper approach.
                if (!ClearForBodyRise(delta, direction, radius, worldHeight,
                        wall.collider))
                {
                    RejectedHighSteps++;
                    continue;
                }
                contact = wall.point;
                top = hit.point;
                topY = hit.point.y;
                // Only raise the physical capsule as far as its current
                // actual bottom needs to clear the new tread.
                rise = Mathf.Max(0f, hit.point.y - physicalBottom);
                return true;
            }
        }
        return false;
    }

    private bool ClearForBodyRise(float rise, Vector3 dir,
        float radius, float height, Collider candidateStep)
    {
        // Use the actual character collider with collision geometries rather
        // than an unchecked transform position change.
        Vector3 center = transform.TransformPoint(cc.center);
        float capsuleRadius = Mathf.Max(.05f, radius - cc.skinWidth);
        Vector3 bottom = center + Vector3.down * (height * .5f - radius);
        Vector3 upper = center + Vector3.up * (height * .5f - radius);
        int n = Physics.CapsuleCastNonAlloc(bottom, upper, capsuleRadius,
            Vector3.up, rayHits, rise + .11f, obstacles,
            QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            Collider c = rayHits[i].collider;
            if (!IsUsable(c)) continue;
            // While standing beside a ledge, an upward sweep can touch its
            // SIDE face. Side contact isn't a ceiling; CC.Move remains the
            // authority for the actual ascent and stops on real collisions.
            if (c == candidateStep && rayHits[i].normal.y < .25f)
                continue;
            LastRejectedClearance = "vertical:" + c.name +
                " normalY=" + rayHits[i].normal.y.ToString("F2");
            return false;
        }

        // Check occupancy at the top after the capsule advances across the
        // wall face, with full-body dimensions (not a single foot probe).
        Vector3 offset = Vector3.up * (rise + .12f) +
                         dir * Mathf.Max(.3f, radius * .9f);
        n = Physics.OverlapCapsuleNonAlloc(bottom + offset,
            upper + offset, capsuleRadius, occupied, obstacles,
            QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            Collider collider = occupied[i];
            if (!IsUsable(collider)) continue;
            if (Physics.ComputePenetration(cc, transform.position + offset,
                transform.rotation, collider, collider.transform.position,
                collider.transform.rotation, out _, out float depth) &&
                depth > .035f)
            {
                LastRejectedClearance = "occupied:" + collider.name +
                    " depth=" + depth.ToString("F2");
                return false;
            }
        }
        return true;
    }

    private bool IsUsable(Collider candidate)
    {
        return candidate && candidate.enabled && !candidate.isTrigger &&
               !candidate.transform.IsChildOf(transform) &&
               (!candidate.attachedRigidbody ||
                candidate.attachedRigidbody.isKinematic);
    }
}
