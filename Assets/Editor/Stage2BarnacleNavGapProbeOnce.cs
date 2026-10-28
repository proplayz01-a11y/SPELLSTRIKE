#if UNITY_EDITOR
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class Stage2BarnacleNavGapProbeOnce
{
    private const string Marker = "Stage2BarnacleNavGapProbe.trigger";
    private const string ScenePath = "Assets/Scenes/Stage2Scene/Stage2Scene.unity";

    static Stage2BarnacleNavGapProbeOnce()
    {
        EditorApplication.update += TryRun;
    }

    private static void TryRun()
    {
        if (!File.Exists(Marker)
            || EditorApplication.isCompiling
            || EditorApplication.isUpdating
            || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        File.Delete(Marker);
        EditorApplication.update -= TryRun;

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
        {
            Debug.LogError($"[Stage2NavGapProbe] Wrong active scene '{scene.path}'.");
            return;
        }

        foreach (NavMeshSurface surface in Resources.FindObjectsOfTypeAll<NavMeshSurface>())
        {
            if (surface != null
                && surface.gameObject.scene.IsValid()
                && surface.gameObject.scene.path == ScenePath
                && surface.navMeshData != null)
            {
                surface.RemoveData();
                surface.AddData();
            }
        }

        BarnacleHuskController barnacle = Object.FindFirstObjectByType<BarnacleHuskController>();
        if (barnacle == null)
        {
            Debug.LogError("[Stage2NavGapProbe] BarnacleHuskController not found.");
            return;
        }

        Vector3 authored = barnacle.transform.position;

        // Representative upper-arena point captured from the Android Node 1 battle.
        // Its Android NavMesh projection was within 0.84 m and is known to be on the
        // player-reachable arena surface.
        Vector3 playerIslandPoint = new Vector3(350.40f, 0.49f, 104.16f);

        if (!NavMesh.SamplePosition(authored, out NavMeshHit enemyHit, 8f, NavMesh.AllAreas))
        {
            Debug.LogError($"[Stage2NavGapProbe] No NavMesh near authored Barnacle position {authored:F3}.");
            return;
        }

        if (!NavMesh.SamplePosition(playerIslandPoint, out NavMeshHit playerHit, 4f, NavMesh.AllAreas))
        {
            Debug.LogError($"[Stage2NavGapProbe] No NavMesh near player-island point {playerIslandPoint:F3}.");
            return;
        }

        NavMeshPath path = new NavMeshPath();
        bool calculated = NavMesh.CalculatePath(enemyHit.position, playerHit.position, NavMesh.AllAreas, path);
        Vector3 lastCorner = path.corners.Length > 0 ? path.corners[path.corners.Length - 1] : enemyHit.position;

        Debug.Log(
            $"[Stage2NavGapProbe] authored={authored:F3} enemySample={enemyHit.position:F3} " +
            $"playerSample={playerHit.position:F3} calculated={calculated} status={path.status} " +
            $"corners={path.corners.Length} lastCorner={lastCorner:F3}.");

        Vector3 flatDirection = playerHit.position - lastCorner;
        flatDirection.y = 0f;
        if (flatDirection.sqrMagnitude < 0.0001f)
        {
            Debug.Log("[Stage2NavGapProbe] Partial endpoint already overlaps player target in XZ.");
            return;
        }
        flatDirection.Normalize();

        bool found = false;
        for (float step = 0.25f; step <= 40f; step += 0.25f)
        {
            Vector3 probe = lastCorner + flatDirection * step;
            probe.y = playerHit.position.y;

            if (!NavMesh.SamplePosition(probe, out NavMeshHit candidate, 0.6f, NavMesh.AllAreas))
                continue;

            NavMeshPath candidatePath = new NavMeshPath();
            if (!NavMesh.CalculatePath(candidate.position, playerHit.position, NavMesh.AllAreas, candidatePath)
                || candidatePath.status != NavMeshPathStatus.PathComplete)
                continue;

            float gap3D = Vector3.Distance(lastCorner, candidate.position);
            Vector3 a = lastCorner;
            Vector3 b = candidate.position;
            a.y = 0f;
            b.y = 0f;
            float gapXZ = Vector3.Distance(a, b);

            Debug.Log(
                $"[Stage2NavGapProbe] FIRST_PLAYER_REACHABLE step={step:0.00} " +
                $"candidate={candidate.position:F3} gap3D={gap3D:0.000} gapXZ={gapXZ:0.000} " +
                $"verticalDelta={(candidate.position.y - lastCorner.y):0.000} " +
                $"candidateCorners={candidatePath.corners.Length}.");
            found = true;
            break;
        }

        if (!found)
            Debug.LogWarning("[Stage2NavGapProbe] No player-reachable NavMesh found within 40 m from the partial endpoint along the target direction.");
    }
}
#endif
