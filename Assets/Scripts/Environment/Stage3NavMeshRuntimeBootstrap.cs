using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// Registers Stage 3 baked NavMesh data after scene integration and safely binds
/// scene-authored agents that are intentionally disabled in prefabs.
/// </summary>
public static class Stage3NavMeshRuntimeBootstrap
{
    private const string Stage3SceneName = "Stage3Scene";
    private const string RuntimeDriverName = "Stage3_NavMeshRuntimeDriver";
    private static Stage3NavMeshRuntimeBootstrapBehaviour driver;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHook()
    {
        EnsureDriver();
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.IsValid() && scene.name == Stage3SceneName)
            EnsureDriver().Schedule(scene);
    }

    private static Stage3NavMeshRuntimeBootstrapBehaviour EnsureDriver()
    {
        if (driver != null)
            return driver;

        Stage3NavMeshRuntimeBootstrapBehaviour existing =
            Object.FindFirstObjectByType<Stage3NavMeshRuntimeBootstrapBehaviour>();
        if (existing != null)
        {
            driver = existing;
            return driver;
        }

        GameObject host = new GameObject(RuntimeDriverName);
        Object.DontDestroyOnLoad(host);
        driver = host.AddComponent<Stage3NavMeshRuntimeBootstrapBehaviour>();
        return driver;
    }
}

internal sealed class Stage3NavMeshRuntimeBootstrapBehaviour : MonoBehaviour
{
    private const string Stage3SceneName = "Stage3Scene";
    private const float MaximumVerticalRebindDelta = 2.5f;
    private Scene pendingScene;
    private int framesUntilBootstrap;
    private bool pending;

    public void Schedule(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded || scene.name != Stage3SceneName)
            return;

        pendingScene = scene;
        framesUntilBootstrap = 2;
        pending = true;
        Debug.Log($"[Stage3NavMesh] Bootstrap queued sceneHandle={scene.handle}; waiting two Update frames.");
    }

    private void Update()
    {
        if (!pending)
            return;

        Scene scene = pendingScene;
        if (!scene.IsValid() || !scene.isLoaded || scene.name != Stage3SceneName)
        {
            pending = false;
            return;
        }

        if (framesUntilBootstrap-- > 0)
            return;

        pending = false;
        RunBootstrap(scene);
    }

    private static void RunBootstrap(Scene scene)
    {
        int registeredSurfaces = 0;
        foreach (NavMeshSurface surface in Resources.FindObjectsOfTypeAll<NavMeshSurface>())
        {
            if (surface == null
                || !surface.gameObject.scene.IsValid()
                || surface.gameObject.scene.handle != scene.handle
                || !surface.isActiveAndEnabled
                || surface.navMeshData == null)
            {
                continue;
            }

            surface.RemoveData();
            surface.AddData();
            registeredSurfaces++;
            Debug.Log(
                $"[Stage3NavMesh] Registered surface '{surface.gameObject.name}' data='{surface.navMeshData.name}'.",
                surface);
        }

        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
        Debug.Log(
            $"[Stage3NavMesh] Runtime mesh ready surfaces={registeredSurfaces} " +
            $"vertices={triangulation.vertices.Length} indices={triangulation.indices.Length}");

        int alreadyBound = 0;
        int rebound = 0;
        int failed = 0;

        foreach (NavMeshAgent agent in Resources.FindObjectsOfTypeAll<NavMeshAgent>())
        {
            if (agent == null
                || !agent.gameObject.scene.IsValid()
                || agent.gameObject.scene.handle != scene.handle)
            {
                continue;
            }

            if (agent.enabled && agent.isOnNavMesh)
            {
                alreadyBound++;
                agent.isStopped = true;
                continue;
            }

            Vector3 authoredPosition = agent.transform.position;
            if (!TryFindNearestNavMeshPoint(authoredPosition, out NavMeshHit hit))
            {
                failed++;
                Debug.LogWarning(
                    $"[Stage3NavMesh] Could not find baked NavMesh near agent '{agent.gameObject.name}' " +
                    $"authored={authoredPosition:F3}.",
                    agent);
                continue;
            }

            if (agent.enabled)
                agent.enabled = false;

            agent.transform.position = hit.position;
            agent.enabled = true;

            if (!agent.isOnNavMesh && !agent.Warp(hit.position))
            {
                failed++;
                Debug.LogWarning(
                    $"[Stage3NavMesh] Rebind failed for '{agent.gameObject.name}' at {hit.position:F3}.",
                    agent);
                continue;
            }

            agent.isStopped = true;
            agent.ResetPath();
            rebound++;
            Debug.Log(
                $"[Stage3NavMesh] Enabled/rebound '{agent.gameObject.name}' from authored={authoredPosition:F3} " +
                $"to nav={hit.position:F3} yDelta={(hit.position.y - authoredPosition.y):0.000}.",
                agent);
        }

        Debug.Log(
            $"[Stage3NavMesh] Rebind summary alreadyBound={alreadyBound} rebound={rebound} failed={failed}");
    }

    private static bool TryFindNearestNavMeshPoint(Vector3 position, out NavMeshHit hit)
    {
        // Stage 3 authored spawns should already sit on their intended walkable
        // surface. Keep this tight both horizontally and vertically so stacked
        // arena geometry cannot silently bind an enemy to the floor underneath.
        float[] radii = { 2f, 6f };
        foreach (float radius in radii)
        {
            if (!NavMesh.SamplePosition(position, out NavMeshHit candidate, radius, NavMesh.AllAreas))
                continue;

            if (Mathf.Abs(candidate.position.y - position.y) > MaximumVerticalRebindDelta)
                continue;

            hit = candidate;
            return true;
        }

        hit = default;
        return false;
    }
}
