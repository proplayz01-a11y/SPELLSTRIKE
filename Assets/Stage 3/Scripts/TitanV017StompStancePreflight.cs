using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Isolated, no-side-effects preflight for walking a Titan into a natural
/// V017 right-sole landing stance. Never warps/moves the Titan, changes
/// colliders, plays clips, or initiates damage. Navigation remains AI-owned.
/// If the physical capsule prevents Van standing under the authored sole,
/// reports impossible rather than using stretched IK or fake target hits.
/// </summary>
[DisallowMultipleComponent]
public sealed class TitanV017StompStancePreflight : MonoBehaviour
{
    [SerializeField] private Transform titanActor;
    [SerializeField] private NavMeshAgent navAgent;
    [SerializeField] private CapsuleCollider solidBody;
    [SerializeField] private PlayerHealth player;

    [Header("Measured from RESTORED Astra V017 in Unity, scale applied via actor")]
    [Tooltip("Frame 53/93/133/176: physical right sole in Actor local space; do NOT use ankle position.")]
    [SerializeField] private Vector3 authoredRightSoleAtImpact = new Vector3(0.26137f, 0f, 0.10064f);
    [Tooltip("Frame 28+ planted left sole after original preparation step.")]
    [SerializeField] private Vector3 authoredLeftSoleAfterStep = new Vector3(-0.04616f, 0f, 0.11911f);

    [Header("Preposition acceptance")]
    [SerializeField, Min(0.5f)] private float bodySeparationMargin = 0.5f;
    [SerializeField, Min(0f)] private float targetLandingTolerance = 0.75f;
    [SerializeField, Min(0.1f)] private float navMeshSampleRadius = 2f;
    [SerializeField, Min(0.1f)] private float maximumRootPlacementError = 1f;
    [SerializeField, Min(0.1f)] private float maximumLeftRightGroundDelta = 1f;
    [SerializeField, Min(0.05f)] private float maximumRightSoleLandingPlanarError = 0.75f;
    [SerializeField, Min(0.05f)] private float maximumRightSoleLandingHeightError = 0.5f;
    [SerializeField, Min(0.1f)] private float maximumPlayerGroundHeightError = 2f;
    [SerializeField, Min(1f)] private float maximumNavMeshPathLength = 200f;
    [SerializeField, Min(0.25f)] private float pathCollisionSampleStep = 2f;
    [SerializeField, Range(8, 256)] private int maximumPathCollisionSamples = 128;
    [SerializeField, Range(0.5f, 1f)] private float minimumGroundNormalY = 0.7f;
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField, Min(1f)] private float groundRayHeight = 40f;
    [SerializeField, Min(1f)] private float groundRayLength = 180f;

    [Header("Read-only QA result")]
    [SerializeField] private string lastRejection = "Not evaluated.";
    [SerializeField] private Vector3 suggestedRootPosition;
    [SerializeField] private Vector3 proposedImpactGround;
    [SerializeField] private Vector3 measuredVanCapsuleCenter;
    [SerializeField] private float measuredVanWorldRadius;
    [SerializeField] private Vector3 proposedSafeNearEdgeLanding;
    [SerializeField] private float physicalMinimumSeparation;
    [SerializeField] private float nativeImpactRadius;
    [SerializeField] private string lastCollisionDiagnostic = "No collision evaluation yet.";

    public string LastRejection => lastRejection;
    public float PhysicalMinimumSeparation => physicalMinimumSeparation;
    public float NativeImpactRadius => nativeImpactRadius;
    public string LastCollisionDiagnostic => lastCollisionDiagnostic;
    // The authored sole must land on the FLOOR outside Van, not on his center.
    // This is the same physical gap used by the projected body/leg clearance.
    public float PlayerLandingSafetyGap => Mathf.Max(0.5f, bodySeparationMargin);

    /// <summary>
    /// Pure pre-attack query. AI may WALK along the returned NavMesh path and
    /// turn toward the returned facing, then must recheck stance and both
    /// planted soles before stopping and beginning the authored animation.
    /// </summary>
    public bool TryPlanStance(Vector3 playerPosition, float playerWorldRadius,
        out Vector3 rootDestination, out Quaternion actorFacing,
        out Vector3 impactGround, out string rejection)
    {
        CharacterController playerBody = player ? player.GetComponent<CharacterController>() : null;
        return TryPlanStance(playerPosition, playerWorldRadius, playerBody,
            out rootDestination, out actorFacing, out impactGround, out rejection);
    }

    /// <summary>
    /// Combat-safe overload using the live player's physical collider. The
    /// original overload remains available to preexisting QA callers.
    /// </summary>
    public bool TryPlanStance(Vector3 playerPosition, float playerWorldRadius,
        CharacterController playerBody, out Vector3 rootDestination,
        out Quaternion actorFacing, out Vector3 impactGround, out string rejection)
    {
        rootDestination = Vector3.zero;
        impactGround = Vector3.zero;
        actorFacing = Quaternion.identity;
        rejection = string.Empty;
        lastCollisionDiagnostic = "Collision checks not reached.";

        if (!titanActor || !solidBody)
            return Reject("Titan actor or physical capsule unavailable.", out rejection);
        if (solidBody.isTrigger || !solidBody.enabled || !solidBody.gameObject.activeInHierarchy ||
            !solidBody.transform.IsChildOf(titanActor))
            return Reject("Enabled blocking Titan body collider in the actor hierarchy required.", out rejection);
        if (!playerBody || !playerBody.enabled ||
            !playerBody.transform.gameObject.activeInHierarchy ||
            PlanarDistance(playerBody.transform.position, playerPosition) > 0.1f)
            return Reject("Actual Van CharacterController must match the queried world-space target.", out rejection);
        if (!IsFinite(playerPosition) || !IsFinite(playerWorldRadius) || playerWorldRadius < 0f ||
            !IsFinite(titanActor.position) || !IsFinite(titanActor.lossyScale) ||
            !IsFinite(authoredRightSoleAtImpact) || !IsFinite(authoredLeftSoleAfterStep) ||
            authoredRightSoleAtImpact.sqrMagnitude <= 0.0001f)
            return Reject("Invalid player/actor scale or source-sole measurement.", out rejection);

        Vector3 scale = titanActor.lossyScale;
        Vector3 flatImpact = Vector3.Scale(new Vector3(authoredRightSoleAtImpact.x, 0f,
            authoredRightSoleAtImpact.z), scale);
        nativeImpactRadius = flatImpact.magnitude;
        physicalMinimumSeparation = solidBody.radius *
            Mathf.Max(Mathf.Abs(solidBody.transform.lossyScale.x),
                Mathf.Abs(solidBody.transform.lossyScale.z)) +
            playerWorldRadius + PlayerLandingSafetyGap;
        float actualPlayerWorldRadius = playerBody.radius * Mathf.Max(
            Mathf.Abs(playerBody.transform.lossyScale.x),
            Mathf.Abs(playerBody.transform.lossyScale.z));
        if (!IsFinite(nativeImpactRadius) || !IsFinite(physicalMinimumSeparation) ||
            nativeImpactRadius <= 0f || physicalMinimumSeparation <= 0f ||
            !IsFinite(actualPlayerWorldRadius) ||
            playerWorldRadius < actualPlayerWorldRadius - 0.01f)
            return Reject("Non-finite or degenerate physical contact clearance.", out rejection);

        // Radius alone is NOT a valid exclusion test when the torso capsule
        // is elevated above Van and moving leg colliders provide lower-body
        // solidity. Verify actual 3D surfaces/penetration below instead.

        if (!navAgent || !navAgent.enabled || !navAgent.isOnNavMesh ||
            (navAgent.transform != titanActor &&
             !navAgent.transform.IsChildOf(titanActor) &&
             !titanActor.IsChildOf(navAgent.transform)))
            return Reject("No valid NavMeshAgent ready to WALK into the natural stomp stance.", out rejection);

        // Van's production CharacterController is scaled with its hierarchy;
        // its XZ center/radius, not a test-scene .5m estimate, define the rim.
        Vector3 playerCapsuleCenter = playerBody.transform.TransformPoint(playerBody.center);
        measuredVanCapsuleCenter = playerCapsuleCenter;
        measuredVanWorldRadius = actualPlayerWorldRadius;
        if (!IsFinite(playerCapsuleCenter))
            return Reject("Van physical capsule center is not finite.", out rejection);
        if (!TryGround(playerCapsuleCenter, out RaycastHit centerGround, playerBody.transform))
            return Reject("No valid solid, walkable ground beneath Van.", out rejection);
        if (Mathf.Abs(playerPosition.y - centerGround.point.y) > maximumPlayerGroundHeightError)
            return Reject("Van is not near the verified solid landing floor.", out rejection);

        // The right foot lands on a stationary FLOORED RIM outside Van's
        // actual capsule (radius + safety gap). This is NOT a player hit.
        // Aim from Titan toward Van's physical center only to pick the near
        // edge before movement. A fresh plan is required after walking.
        Vector3 toVan = playerCapsuleCenter - titanActor.position;
        toVan.y = 0f;
        if (toVan.sqrMagnitude < 0.01f)
            return Reject("Player is directly inside the giant's root; no legal stance.", out rejection);
        Vector3 towardVan = toVan.normalized;
        Vector3 rimQuery = playerCapsuleCenter - towardVan *
            (actualPlayerWorldRadius + PlayerLandingSafetyGap);
        rimQuery.y = centerGround.point.y;
        if (!IsFinite(rimQuery) ||
            !TryGround(rimQuery, out RaycastHit rimGround, playerBody.transform))
            return Reject("No walkable grounded near-side point outside Van's actual capsule and safety gap.", out rejection);
        impactGround = rimGround.point;
        proposedSafeNearEdgeLanding = impactGround;
        if (Mathf.Abs(impactGround.y - centerGround.point.y) > maximumRightSoleLandingHeightError)
            return Reject("Van center floor and safe right-foot rim floor have incompatible elevations.", out rejection);
        float effectiveRimError = Mathf.Min(maximumRightSoleLandingPlanarError, targetLandingTolerance);
        float actualRimDistance = PlanarDistance(impactGround, playerCapsuleCenter);
        if (actualRimDistance + 0.01f < actualPlayerWorldRadius + PlayerLandingSafetyGap ||
            PlanarDistance(impactGround, rimQuery) > effectiveRimError)
            return Reject("Safe rim ground projection deviates from physical Van radius+gap (radius " +
                actualPlayerWorldRadius.ToString("F2") + "m, gap " +
                PlayerLandingSafetyGap.ToString("F2") + "m, rim " +
                actualRimDistance.ToString("F2") + "m).", out rejection);

        // Preserve the exact source right-sole offset. Root stance (and yaw)
        // moves to the legal rim; femur/IK reach and animated mesh stay native.
        float authoredFootAzimuth = Mathf.Atan2(flatImpact.x, flatImpact.z) * Mathf.Rad2Deg;
        actorFacing = Quaternion.LookRotation(towardVan, Vector3.up) *
            Quaternion.Euler(0f, -authoredFootAzimuth, 0f);
        Vector3 rightOffset = actorFacing * flatImpact;
        Vector3 proposedRoot = impactGround - rightOffset;
        if (!IsFinite(proposedRoot) || !IsFinite(rightOffset))
            return Reject("Natural root/sole destination is not finite.", out rejection);
        if (!NavMesh.SamplePosition(proposedRoot, out NavMeshHit rootHit,
                navMeshSampleRadius, NavMesh.AllAreas) ||
            !IsFinite(rootHit.position) ||
            PlanarDistance(rootHit.position, proposedRoot) > maximumRootPlacementError)
            return Reject("No navigable position under the natural V017 stance.", out rejection);

        NavMeshPath path = new NavMeshPath();
        if (!navAgent.CalculatePath(rootHit.position, path) ||
            path.status != NavMeshPathStatus.PathComplete ||
            !IsFinitePath(path))
            return Reject("Titan cannot WALK to the proposed stance on its current NavMesh.", out rejection);

        // NavMesh projection can silently slide a proposed root sideways.
        // Re-evaluate BOTH physical soles from the candidate that the agent
        // can actually reach; a nearby NavMesh point is not proof of contact.
        float effectiveRightLandingTolerance = Mathf.Min(
            maximumRightSoleLandingPlanarError, targetLandingTolerance);
        Vector3 predictedRightSole = rootHit.position + rightOffset;
        if (!IsFinite(predictedRightSole) ||
            PlanarDistance(predictedRightSole, impactGround) > effectiveRightLandingTolerance ||
            !TryGround(predictedRightSole, out RaycastHit rightGround, playerBody.transform) ||
            PlanarDistance(rightGround.point, impactGround) > effectiveRightLandingTolerance ||
            Mathf.Abs(rightGround.point.y - impactGround.y) > maximumRightSoleLandingHeightError)
            return Reject("Reachable NavMesh root does not place the REAL right-sole landing on Van's floor.", out rejection);

        Collider[] bossColliders = titanActor.GetComponentsInChildren<Collider>(true);
        if (!HasSolidCollider(bossColliders))
            return Reject("No enabled non-trigger Titan collision geometry for stance clearance.", out rejection);
        if (!CheckActorClearance(playerBody, actualPlayerWorldRadius,
                rootHit.position, actorFacing, bossColliders, out string candidateIssue))
        {
            lastCollisionDiagnostic = "Candidate root: " + candidateIssue;
            return Reject(lastCollisionDiagnostic, out rejection);
        }
        if (!CheckNavMeshPathClearance(path, playerBody,
                actualPlayerWorldRadius, actorFacing, bossColliders, out string pathIssue))
        {
            lastCollisionDiagnostic = "NavMesh path: " + pathIssue;
            return Reject(lastCollisionDiagnostic, out rejection);
        }

        Vector3 leftGroundQuery = rootHit.position +
            actorFacing * Vector3.Scale(new Vector3(authoredLeftSoleAfterStep.x, 0f,
                authoredLeftSoleAfterStep.z), scale);
        if (!IsFinite(leftGroundQuery) ||
            !TryGround(leftGroundQuery, out RaycastHit leftGround, playerBody.transform))
            return Reject("No solid ground underneath the frame-28 left support foot.", out rejection);
        // Real Stage3 frame-28 QA observed a native +0.725m airborne left
        // sole, because this preflight formerly allowed 1.0m of floor-height
        // difference. Astra's authored left step is level with the right sole.
        // Reject that UNGROUNDABLE terrain before entering the 8.8s attack;
        // keep the solver's independent strict 0.35m sole/mesh requirement.
        float supportedGroundDelta = Mathf.Min(maximumLeftRightGroundDelta, 0.30f);
        float actualGroundDelta = Mathf.Abs(leftGround.point.y - impactGround.y);
        if (actualGroundDelta > supportedGroundDelta)
            return Reject("Right impact and authored frame-28 LEFT support have incompatible real ground: " +
                "rightY=" + impactGround.y.ToString("F3") +
                " leftY=" + leftGround.point.y.ToString("F3") +
                " delta=" + actualGroundDelta.ToString("F3") +
                " allowed=" + supportedGroundDelta.ToString("F3") +
                ". Search a different natural stance; do not fake left planting.", out rejection);

        rootDestination = rootHit.position;
        suggestedRootPosition = rootDestination;
        proposedImpactGround = impactGround;
        lastRejection = string.Empty;
        lastCollisionDiagnostic = "Candidate and sampled NavMesh path clear against current Titan collider poses.";
        return true;
    }

    [ContextMenu("QA: Check Natural Right-Stomp Stance (No Movement)")]
    public void CheckCurrentVanStance()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[Titan V017 Stance] Run this only in an isolated QA Play Mode scene.", this);
            return;
        }
        if (!player)
        {
            GameObject van = GameObject.FindWithTag("Player");
            if (van) player = van.GetComponent<PlayerHealth>();
        }
        if (!player || player.currentHealth <= 0)
        {
            Debug.LogWarning("[Titan V017 Stance] No living real Van player found.", this);
            return;
        }
        CharacterController controller = player.GetComponent<CharacterController>();
        float radius = controller
            ? controller.radius * Mathf.Max(Mathf.Abs(controller.transform.lossyScale.x),
                Mathf.Abs(controller.transform.lossyScale.z)) : 0.5f;
        if (!TryPlanStance(player.transform.position, radius, out Vector3 root,
                out Quaternion facing, out Vector3 landing, out string issue))
        {
            Debug.LogWarning("[Titan V017 Stance] REJECT: " + issue + " No foot IK, movement or damage applied.", this);
            return;
        }
        Debug.Log("[Titan V017 Stance] WALK plan only: NavMesh root=" + root.ToString("F2") +
            " facing=" + facing.eulerAngles.ToString("F1") +
            " landing=" + landing.ToString("F2") + ". Recheck ground after arrival. No attack started.", this);
    }

    private bool TryGround(Vector3 at, out RaycastHit ground, Transform playerRoot)
    {
        ground = default;
        if (!IsFinite(at)) return false;
        RaycastHit[] hits = Physics.RaycastAll(at + Vector3.up * groundRayHeight,
            Vector3.down, groundRayLength, groundLayers, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (!hit.collider || !IsFinite(hit.point) || !IsFinite(hit.normal) ||
                hit.normal.y < minimumGroundNormalY ||
                hit.collider is CharacterController ||
                hit.collider.transform.IsChildOf(titanActor) ||
                (playerRoot && hit.collider.transform.IsChildOf(playerRoot)) ||
                (player && hit.collider.transform.IsChildOf(player.transform)))
                continue;
            ground = hit;
            return true;
        }
        return false;
    }

    private bool IsFinitePath(NavMeshPath path)
    {
        Vector3[] corners = path.corners;
        if (corners == null || corners.Length == 0) return false;
        float totalLength = 0f;
        for (int i = 0; i < corners.Length; i++)
        {
            if (!IsFinite(corners[i])) return false;
            if (i > 0) totalLength += Vector3.Distance(corners[i - 1], corners[i]);
            if (!IsFinite(totalLength) || totalLength > maximumNavMeshPathLength)
                return false;
        }
        return true;
    }

    private static bool HasSolidCollider(Collider[] colliders)
    {
        foreach (Collider collider in colliders)
            if (collider && collider.enabled && collider.gameObject.activeInHierarchy &&
                !collider.isTrigger)
                return true;
        return false;
    }

    // Evaluate a future WALKED actor pose without moving any Unity Transform.
    // Child bone colliders keep their CURRENT animation-local poses. The IK
    // solver remains responsible for later live foot clearance and contacts.
    private bool CheckActorClearance(CharacterController playerBody,
        float worldPlayerRadius, Vector3 navRoot, Quaternion actorRotation,
        Collider[] bossColliders, out string issue)
    {
        issue = string.Empty;
        if (!IsFinite(navRoot))
        {
            issue = "Non-finite candidate NavMesh root " + navRoot.ToString("F3") + ".";
            return false;
        }
        Vector3 futureActorOrigin = new Vector3(navRoot.x,
            titanActor.position.y, navRoot.z);
        Vector3 currentScale = titanActor.lossyScale;
        Vector3 controllerCenter = playerBody.transform.TransformPoint(playerBody.center);
        Vector3 playerAxis = playerBody.transform.up;
        float playerHeight = Mathf.Abs(playerBody.transform.lossyScale.y) * playerBody.height;
        float playerAxisHalfLength = Mathf.Max(0f, playerHeight * 0.5f - worldPlayerRadius);
        if (!IsFinite(futureActorOrigin) || !IsFinite(controllerCenter) ||
            !IsFinite(playerHeight) || !IsFinite(playerAxisHalfLength))
        {
            issue = "Invalid future actor/player capsule coordinates at navRoot " +
                navRoot.ToString("F3") + ".";
            return false;
        }

        foreach (Collider collider in bossColliders)
        {
            if (!collider || !collider.enabled || !collider.gameObject.activeInHierarchy ||
                collider.isTrigger || collider == playerBody) continue;

            // Physics.ClosestPoint supports primitive and convex mesh shapes.
            // Nonconvex MeshCollider cannot be evaluated safely in this pose.
            if (collider is MeshCollider mesh && !mesh.convex)
            {
                issue = "Unsupported non-convex solid " + ColliderIdentity(collider) +
                    " at navRoot " + navRoot.ToString("F3") + ".";
                return false;
            }
            Vector3 localColliderOrigin = titanActor.InverseTransformPoint(collider.transform.position);
            Vector3 projectedColliderPosition = futureActorOrigin + actorRotation *
                Vector3.Scale(localColliderOrigin, currentScale);
            Quaternion projectedColliderRotation = actorRotation *
                Quaternion.Inverse(titanActor.rotation) * collider.transform.rotation;
            if (!IsFinite(projectedColliderPosition))
            {
                issue = "Invalid projected origin of " + ColliderIdentity(collider) +
                    " at navRoot " + navRoot.ToString("F3") +
                    ", currentOrigin=" + collider.transform.position.ToString("F3") + ".";
                return false;
            }

            string poseDetails = null;

            if (Physics.ComputePenetration(playerBody,
                    playerBody.transform.position, playerBody.transform.rotation,
                    collider, projectedColliderPosition, projectedColliderRotation,
                    out Vector3 direction, out float penetrationDepth) &&
                penetrationDepth > 0f)
            {
                poseDetails = DescribeColliderProjection(collider, navRoot,
                    actorRotation, projectedColliderPosition, projectedColliderRotation);
                issue = "PENETRATION " + ColliderIdentity(collider) +
                    ": depth=" + penetrationDepth.ToString("F3") +
                    "m, resolveDir=" + direction.ToString("F3") + "; " + poseDetails;
                return false;
            }

            // ClosestPoint at multiple locations along Van's capsule spine
            // detects insufficient air gap even when colliders do not overlap.
            for (int sample = 0; sample <= 4; sample++)
            {
                Vector3 axisPoint = controllerCenter + playerAxis *
                    (playerAxisHalfLength * (sample * 0.5f - 1f));
                Vector3 surfacePoint = Physics.ClosestPoint(axisPoint, collider,
                    projectedColliderPosition, projectedColliderRotation);
                float centerToSurface = Vector3.Distance(surfacePoint, axisPoint);
                float clearance = centerToSurface - worldPlayerRadius;
                if (!IsFinite(surfacePoint) || !IsFinite(clearance) ||
                    clearance < PlayerLandingSafetyGap)
                {
                    poseDetails = DescribeColliderProjection(collider, navRoot,
                        actorRotation, projectedColliderPosition, projectedColliderRotation);
                    issue = "CLEARANCE " + ColliderIdentity(collider) +
                        ": playerSpineSample=" + sample + "/4, point=" + axisPoint.ToString("F3") +
                        ", nearest=" + surfacePoint.ToString("F3") +
                        ", centerToSurface=" + centerToSurface.ToString("F3") +
                        "m, playerRadius=" + worldPlayerRadius.ToString("F3") +
                        "m, signedGap=" + clearance.ToString("F3") +
                        "m, requiredGap=" + PlayerLandingSafetyGap.ToString("F3") +
                        "m; " + poseDetails;
                    return false;
                }
            }
        }
        return true;
    }

    private bool CheckNavMeshPathClearance(NavMeshPath path,
        CharacterController playerBody, float worldPlayerRadius,
        Quaternion finalFacing,
        Collider[] bossColliders, out string issue)
    {
        issue = string.Empty;
        Vector3[] corners = path.corners;
        if (corners.Length == 0)
        {
            issue = "Complete NavMesh path has no corners.";
            return false;
        }
        int sampleCount = 0;
        Vector3 prior = navAgent.transform.position;
        for (int segment = 0; segment < corners.Length; segment++)
        {
            Vector3 next = corners[segment];
            float length = PlanarDistance(prior, next);
            int steps = Mathf.Max(1, Mathf.CeilToInt(length / pathCollisionSampleStep));
            if (steps > maximumPathCollisionSamples - sampleCount)
            {
                issue = "Path collision samples exceed budget " + maximumPathCollisionSamples +
                    " at segment=" + segment + ", length=" + length.ToString("F2") + "m.";
                return false;
            }
            for (int sample = 1; sample <= steps; sample++)
            {
                Vector3 waypoint = Vector3.Lerp(prior, next, (float)sample / steps);
                if (!CheckActorClearance(playerBody, worldPlayerRadius,
                        waypoint, titanActor.rotation, bossColliders, out string detail))
                {
                    issue = DescribePathSample(segment, sample, steps, waypoint,
                        "start yaw", detail);
                    return false;
                }
                if (!CheckActorClearance(playerBody, worldPlayerRadius,
                        waypoint, Quaternion.Slerp(titanActor.rotation, finalFacing, 0.5f),
                        bossColliders, out detail))
                {
                    issue = DescribePathSample(segment, sample, steps, waypoint,
                        "mid yaw", detail);
                    return false;
                }
                if (!CheckActorClearance(playerBody, worldPlayerRadius,
                        waypoint, finalFacing, bossColliders, out detail))
                {
                    issue = DescribePathSample(segment, sample, steps, waypoint,
                        "final yaw", detail);
                    return false;
                }
                sampleCount++;
            }
            prior = next;
        }
        return true;
    }

    private static string DescribePathSample(int segment, int sample, int steps,
        Vector3 waypoint, string yaw, string detail)
    {
        return "segment=" + segment + ", sample=" + sample + "/" + steps +
            ", waypoint=" + waypoint.ToString("F3") + ", orientation=" + yaw +
            "; " + detail;
    }

    private string DescribeColliderProjection(Collider collider, Vector3 navRoot,
        Quaternion actorFacing, Vector3 projectedOrigin, Quaternion projectedRotation)
    {
        Vector3 actualOrigin = collider.transform.position;
        Vector3 futureActorOrigin = new Vector3(navRoot.x,
            titanActor.position.y, navRoot.z);
        Vector3 expectedTranslatedOrigin = actualOrigin + futureActorOrigin - titanActor.position;
        Vector3 localOrigin = titanActor.InverseTransformPoint(actualOrigin);
        Vector3 actualCenter = collider.transform.TransformPoint(GetLocalColliderCenter(collider));
        Vector3 projectedCenter = GetWorldColliderCenter(collider,
            projectedOrigin, projectedRotation);
        return "navRoot=" + navRoot.ToString("F3") +
            ", actorY=" + titanActor.position.y.ToString("F3") +
            ", actorYaw=" + actorFacing.eulerAngles.y.ToString("F1") +
            "deg, currentOrigin=" + actualOrigin.ToString("F3") +
            ", projectedOrigin=" + projectedOrigin.ToString("F3") +
            ", currentCenter=" + actualCenter.ToString("F3") +
            ", projectedCenterApprox=" + projectedCenter.ToString("F3") +
            ", actorScale=" + titanActor.lossyScale.ToString("F3") +
            ", colliderScale=" + collider.transform.lossyScale.ToString("F3") +
            ", projectedYaw=" + projectedRotation.eulerAngles.y.ToString("F1") +
            ", localOrigin=" + localOrigin.ToString("F3") +
            ", simpleTranslateOrigin=" + expectedTranslatedOrigin.ToString("F3") +
            ", yawProjectionDelta=" +
                Vector3.Distance(projectedOrigin, expectedTranslatedOrigin).ToString("F3") +
            "m (current animated child pose projected; no transform moved)";
    }

    private static Vector3 GetWorldColliderCenter(Collider collider,
        Vector3 worldOrigin, Quaternion worldRotation)
    {
        Vector3 localCenter = GetLocalColliderCenter(collider);
        Vector3 worldScaledCenter = Vector3.Scale(localCenter, collider.transform.lossyScale);
        return worldOrigin + worldRotation * worldScaledCenter;
    }

    private static Vector3 GetLocalColliderCenter(Collider collider)
    {
        if (collider is CapsuleCollider capsule) return capsule.center;
        if (collider is SphereCollider sphere) return sphere.center;
        if (collider is BoxCollider box) return box.center;
        return Vector3.zero;
    }

    private string ColliderIdentity(Collider collider)
    {
        string relativePath = collider.name;
        Transform current = collider.transform.parent;
        while (current && current != titanActor)
        {
            relativePath = current.name + "/" + relativePath;
            current = current.parent;
        }
        return "'" + titanActor.name + "/" + relativePath + "' [" +
            collider.GetType().Name + ", instanceID=" + collider.GetInstanceID() + "]";
    }

    private static bool IsFinite(Vector3 value) =>
        IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);

    private static bool IsFinite(float value) =>
        !float.IsNaN(value) && !float.IsInfinity(value);

    private bool Reject(string reason, out string rejection)
    {
        rejection = lastRejection = reason;
        return false;
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
    }
}
