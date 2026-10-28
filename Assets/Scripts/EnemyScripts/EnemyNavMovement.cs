using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Applies the shared normal-locomotion contract used by enemy NavMeshAgents.
/// Special attacks that intentionally move without NavMesh acceleration remain
/// controlled by their own ability scripts.
/// </summary>
public static class EnemyNavMovement
{
    private const float MinimumAccelerationTime = 0.05f;
    private static readonly float[] DestinationSampleRadii = { 1.5f, 4f, 12f, 32f };

    public static void ApplySpeed(NavMeshAgent agent, float maximumSpeed, float accelerationTime)
    {
        if (agent == null)
            return;

        float safeSpeed = Mathf.Max(0f, maximumSpeed);
        float safeAccelerationTime = Mathf.Max(MinimumAccelerationTime, accelerationTime);

        agent.speed = safeSpeed;
        agent.acceleration = safeSpeed / safeAccelerationTime;
    }

    /// <summary>
    /// Resolves a world-space target to the baked NavMesh before assigning it to an
    /// agent. This is important for the DDOL player: its transform may be a small
    /// distance above/beside the walkable surface after scene-spawn settling, and
    /// raw SetDestination(player.position) can then report success without ever
    /// producing a runtime path on some Android devices.
    /// </summary>
    public static bool TrySetDestination(NavMeshAgent agent, Vector3 worldTarget)
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return false;

        int areaMask = agent.areaMask;
        Vector3 resolvedTarget = worldTarget;
        bool sampled = false;

        foreach (float radius in DestinationSampleRadii)
        {
            if (!NavMesh.SamplePosition(worldTarget, out NavMeshHit hit, radius, areaMask))
                continue;

            resolvedTarget = hit.position;
            sampled = true;
            break;
        }

        if (!sampled)
            return false;

        NavMeshPath path = new NavMeshPath();
        // Use the agent's current bound NavMesh polygon as the path origin.
        // Stage 2 registers multiple baked surfaces, and a global world-space
        // CalculatePath can rediscover the wrong start surface after an agent
        // moves even a tiny amount near adjacent/stacked NavMesh geometry.
        bool calculated = agent.CalculatePath(resolvedTarget, path);

        if (!calculated || path.status == NavMeshPathStatus.PathInvalid || path.corners.Length < 2)
        {
            // Runtime-registered Stage 2 NavMesh islands can report an agent as
            // correctly bound while NavMeshAgent.CalculatePath still refuses a
            // route that the runtime NavMesh can resolve. Keep the agent-bound
            // calculation as the primary path, then retry from nextPosition so
            // the already-working enemies are unaffected unless that first path
            // calculation genuinely fails.
            path = new NavMeshPath();
            calculated = NavMesh.CalculatePath(agent.nextPosition, resolvedTarget, areaMask, path);
        }

        if ((!calculated || path.status == NavMeshPathStatus.PathInvalid || path.corners.Length < 2)
            && sampled)
        {
            // On Stage 2's stacked/disconnected baked surfaces, SamplePosition can
            // return the geometrically nearest polygon even when it belongs to a
            // different NavMesh island than this agent. In that case, retry the
            // original target so Unity can resolve the endpoint from the agent's
            // currently bound island instead of being forced onto the wrong sample.
            path = new NavMeshPath();
            calculated = agent.CalculatePath(worldTarget, path);

            if (!calculated || path.status == NavMeshPathStatus.PathInvalid || path.corners.Length < 2)
            {
                path = new NavMeshPath();
                calculated = NavMesh.CalculatePath(agent.nextPosition, worldTarget, areaMask, path);
            }
        }

        if (!calculated || path.status == NavMeshPathStatus.PathInvalid || path.corners.Length < 2)
            return false;

        agent.isStopped = false;
        return agent.SetPath(path);
    }
}
