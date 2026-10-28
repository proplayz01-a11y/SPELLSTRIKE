using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// Re-registers Stage 2's baked NavMesh data after the scene is fully loaded and
/// rebinds any agents that were enabled before the NavMeshSurface instances became
/// available in the Android player.
/// </summary>
public static class Stage2NavMeshRuntimeBootstrap
{
    private const string Stage2SceneName = "Stage2Scene";
    private const string RuntimeDriverName = "Stage2_NavMeshRuntimeDriver";
    private static Stage2NavMeshRuntimeBootstrapBehaviour driver;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHook()
    {
        EnsureDriver();
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureForScene(scene);
    }

    /// <summary>
    /// Idempotently installs the delayed Stage 2 NavMesh registration/rebind pass.
    /// PersistentPlayerRoot calls this explicitly when it binds Stage2Scene, while
    /// sceneLoaded remains as a fallback for direct scene entry.
    /// </summary>
    public static void EnsureForScene(Scene scene)
    {
        if (!scene.IsValid() || scene.name != Stage2SceneName)
            return;

        EnsureDriver().Schedule(scene);
    }

    private static Stage2NavMeshRuntimeBootstrapBehaviour EnsureDriver()
    {
        if (driver != null)
            return driver;

        Stage2NavMeshRuntimeBootstrapBehaviour existing = Object.FindFirstObjectByType<Stage2NavMeshRuntimeBootstrapBehaviour>();
        if (existing != null)
        {
            driver = existing;
            return driver;
        }

        GameObject host = new GameObject(RuntimeDriverName);
        Object.DontDestroyOnLoad(host);
        driver = host.AddComponent<Stage2NavMeshRuntimeBootstrapBehaviour>();
        return driver;
    }
}

internal sealed class Stage2NavMeshRuntimeBootstrapBehaviour : MonoBehaviour
{
    private const string Stage2SceneName = "Stage2Scene";
    private Scene pendingScene;
    private int framesUntilBootstrap;
    private bool pending;

    public void Schedule(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded || scene.name != Stage2SceneName)
            return;

        // sceneLoaded and PersistentPlayerRoot can both request setup. Resetting to
        // two Update frames is intentional and idempotent: the DDOL driver existed
        // before the scene transition, so these frames are guaranteed to execute
        // after scene integration instead of inside the sceneLoaded callback.
        pendingScene = scene;
        framesUntilBootstrap = 2;
        pending = true;
        Debug.Log($"[Stage2NavMesh] Bootstrap queued sceneHandle={scene.handle}; waiting two Update frames.");
    }

    private void Update()
    {
        if (!pending)
            return;

        Scene scene = pendingScene;
        if (!scene.IsValid() || !scene.isLoaded || scene.name != Stage2SceneName)
        {
            pending = false;
            return;
        }

        if (framesUntilBootstrap > 0)
        {
            framesUntilBootstrap--;
            return;
        }

        pending = false;
        RunBootstrap(scene);
    }

    private void RunBootstrap(Scene scene)
    {

        NavMeshSurface[] surfaces = Resources.FindObjectsOfTypeAll<NavMeshSurface>();
        int registeredSurfaces = 0;

        foreach (NavMeshSurface surface in surfaces)
        {
            if (surface == null || !surface.gameObject.scene.IsValid() || surface.gameObject.scene.name != Stage2SceneName)
                continue;

            if (!surface.isActiveAndEnabled || surface.navMeshData == null)
                continue;

            // Force a clean runtime registration. Both Stage 2 baked surfaces are
            // intentional scene data and must be present for the full map.
            surface.RemoveData();
            surface.AddData();
            registeredSurfaces++;

            Debug.Log(
                $"[Stage2NavMesh] Registered surface '{surface.gameObject.name}' data='{surface.navMeshData.name}' " +
                $"agentType={surface.agentTypeID} position={surface.transform.position:F2}",
                surface);
        }

        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
        Debug.Log(
            $"[Stage2NavMesh] Runtime mesh ready surfaces={registeredSurfaces} " +
            $"vertices={triangulation.vertices.Length} indices={triangulation.indices.Length}");

        NavMeshAgent[] agents = Resources.FindObjectsOfTypeAll<NavMeshAgent>();
        int rebound = 0;
        int alreadyBound = 0;
        int failed = 0;

        foreach (NavMeshAgent agent in agents)
        {
            if (agent == null || !agent.gameObject.scene.IsValid() || agent.gameObject.scene.name != Stage2SceneName)
                continue;

            // Stage 2 enemy agents are authored disabled so they cannot bind to a
            // stale/wrong NavMesh layer before the baked surfaces are registered.
            // Their transform is therefore still the exact scene-authored spawn
            // position here, which is the safest point from which to choose the
            // correct stacked NavMesh surface.
            if (!agent.enabled)
            {
                Vector3 authoredPosition = agent.transform.position;
                if (!TryFindNearestNavMeshPoint(authoredPosition, out NavMeshHit authoredHit))
                {
                    failed++;
                    Debug.LogWarning(
                        $"[Stage2NavMesh] Could not find baked NavMesh near disabled agent '{agent.gameObject.name}' " +
                        $"authored={authoredPosition:F3}.",
                        agent);
                    continue;
                }

                agent.transform.position = authoredHit.position;
                agent.enabled = true;

                if (!agent.isOnNavMesh && !agent.Warp(authoredHit.position))
                {
                    failed++;
                    Debug.LogWarning(
                        $"[Stage2NavMesh] Could not enable/rebind agent '{agent.gameObject.name}' " +
                        $"at authored NavMesh point {authoredHit.position:F3}.",
                        agent);
                    continue;
                }

                agent.isStopped = true;
                agent.ResetPath();
                rebound++;

                Debug.Log(
                    $"[Stage2NavMesh] Enabled/rebound '{agent.gameObject.name}' from authored={authoredPosition:F3} " +
                    $"to nav={authoredHit.position:F3} yDelta={(authoredHit.position.y - authoredPosition.y):0.000}.",
                    agent);
                continue;
            }

            if (agent.isOnNavMesh)
            {
                alreadyBound++;
                continue;
            }

            if (!TryFindNearestNavMeshPoint(agent.transform.position, out NavMeshHit hit))
            {
                failed++;
                Debug.LogWarning(
                    $"[Stage2NavMesh] Could not find nearby baked NavMesh for agent '{agent.gameObject.name}' " +
                    $"at {agent.transform.position:F2}.",
                    agent);
                continue;
            }

            // A NavMeshAgent that failed creation before any NavMesh existed can stay
            // detached even after data is registered. Re-enable it at a valid point.
            agent.enabled = false;
            agent.transform.position = hit.position;
            agent.enabled = true;

            if (!agent.isOnNavMesh && !agent.Warp(hit.position))
            {
                failed++;
                Debug.LogWarning(
                    $"[Stage2NavMesh] Rebind failed for agent '{agent.gameObject.name}' at {hit.position:F2}.",
                    agent);
                continue;
            }

            // Stage 2 combat is gated by vocabulary/start triggers, so keep newly
            // rebound agents parked until their normal StartBattle() releases them.
            agent.isStopped = true;
            rebound++;
            Debug.Log(
                $"[Stage2NavMesh] Rebound agent '{agent.gameObject.name}' to {hit.position:F2}.",
                agent);
        }

        Debug.Log(
            $"[Stage2NavMesh] Rebind summary alreadyBound={alreadyBound} rebound={rebound} failed={failed}");

        // The driver is DDOL and existed before Stage 2 loaded, so its coroutines
        // are safe for diagnostic telemetry. Core setup above remains Update-driven.
        StartCoroutine(LogAgentTelemetry());
    }

    private static bool TryFindNearestNavMeshPoint(Vector3 position, out NavMeshHit hit)
    {
        // Keep the snap conservative first, then widen only if needed for scene-load
        // discrepancies between authored enemy positions and the baked walkable mesh.
        float[] radii = { 2f, 8f, 24f, 64f };
        foreach (float radius in radii)
        {
            if (NavMesh.SamplePosition(position, out hit, radius, NavMesh.AllAreas))
                return true;
        }

        hit = default;
        return false;
    }

    private IEnumerator LogAgentTelemetry()
    {
        WaitForSecondsRealtime wait = new WaitForSecondsRealtime(2f);

        while (SceneManager.GetActiveScene().IsValid()
            && SceneManager.GetActiveScene().name == Stage2SceneName)
        {
            yield return wait;

            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject == null)
                continue;

            Vector3 playerPosition = playerObject.transform.position;
            NavMeshAgent[] agents = Resources.FindObjectsOfTypeAll<NavMeshAgent>();

            foreach (NavMeshAgent agent in agents)
            {
                if (agent == null
                    || !agent.enabled
                    || !agent.gameObject.scene.IsValid()
                    || agent.gameObject.scene.name != Stage2SceneName)
                    continue;

                Vector3 position = agent.transform.position;
                Vector3 a = position;
                Vector3 b = playerPosition;
                a.y = 0f;
                b.y = 0f;
                float flatDistance = Vector3.Distance(a, b);

                if (!agent.isOnNavMesh)
                {
                    Debug.Log($"[Stage2NavAgent] {agent.gameObject.name} onMesh=false dist={flatDistance:0.00}", agent);
                    continue;
                }

                NavMeshPath testPath = new NavMeshPath();
                bool calculated = NavMesh.CalculatePath(position, playerPosition, NavMesh.AllAreas, testPath);
                bool sampledPlayer = TryFindNearestNavMeshPoint(playerPosition, out NavMeshHit playerHit);
                NavMeshPath sampledPath = new NavMeshPath();
                bool sampledCalculated = sampledPlayer
                    && NavMesh.CalculatePath(position, playerHit.position, NavMesh.AllAreas, sampledPath);
                string runtimeStatus = agent.pathPending ? "Pending" : agent.pathStatus.ToString();
                string testStatus = calculated ? testPath.status.ToString() : "CalculateFailed";
                string sampledStatus = sampledCalculated ? sampledPath.status.ToString() : "CalculateFailed";
                float playerSampleDistance = sampledPlayer
                    ? Vector3.Distance(playerPosition, playerHit.position)
                    : float.PositiveInfinity;

                Debug.Log(
                    $"[Stage2NavAgent] {agent.gameObject.name} dist={flatDistance:0.00} " +
                    $"stopped={agent.isStopped} hasPath={agent.hasPath} pending={agent.pathPending} " +
                    $"status={runtimeStatus} remaining={agent.remainingDistance:0.00} speed={agent.speed:0.00} " +
                    $"vel={agent.velocity.magnitude:0.00} desiredVel={agent.desiredVelocity.magnitude:0.00} " +
                    $"updatePos={agent.updatePosition} next={agent.nextPosition:F2} dest={agent.destination:F2} " +
                    $"calcPath={testStatus} corners={testPath.corners.Length} " +
                    $"playerSample={(sampledPlayer ? playerHit.position.ToString("F2") : "none")} " +
                    $"sampleDelta={playerSampleDistance:0.00} samplePath={sampledStatus} sampleCorners={sampledPath.corners.Length}",
                    agent);
            }
        }

        Destroy(gameObject);
    }
}
