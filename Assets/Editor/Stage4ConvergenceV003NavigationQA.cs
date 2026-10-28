// Explicit visible-menu navigation build. Uses copied Stage 4 collision only;
// never edits global NavMesh agent settings or a production scene.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class Stage4ConvergenceV003NavigationQA
{
    private const string QaScenePath = "Assets/Scenes/Stage4Scene/Stage4Scene_ConvergenceCitadel_v003_Validation.unity";
    private const string OutputAsset = "Assets/Stage 4/ConvergenceCitadel_v003/Navigation/ConvergenceCitadel_Player535_UnityQA_NavMesh.asset";
    private const string OutputReport = "Assets/Stage 4/ConvergenceCitadel_v003/Validation/UnityNavigationQA_20261020.md";

    [MenuItem("Stage 4/Convergence Citadel v003/Bake Isolated Player-Sized NavMesh QA (No Play)")]
    public static void BakeQA()
    {
        if (File.Exists(OutputAsset))
        {
            Debug.LogError("[Stage4 NavQA] Existing test NavMesh asset will not be overwritten: " + OutputAsset);
            return;
        }
        var report = new StringBuilder();
        report.AppendLine("# Stage 4 v003 standalone NavMesh QA in Unity Editor");
        report.AppendLine("No Play Mode. Geometry sources restricted to the 19 isolated imported collision meshes, not SampleScene.");
        report.AppendLine("Temporary NavMeshData is added for path checks, then removed. No global NavMesh settings altered.");
        Scene original = SceneManager.GetActiveScene();
        Scene added = default;
        NavMeshDataInstance handle = default;
        try
        {
            added = EditorSceneManager.OpenScene(QaScenePath, OpenSceneMode.Additive);
            var root = added.GetRootGameObjects().First(x => x.name.StartsWith("Stage4_ConvergenceCitadel_v003"));
            var collidersRoot = root.GetComponentsInChildren<Transform>(true).First(x => x.name == "Independent_StaticMeshColliders");
            var colliders = collidersRoot.GetComponentsInChildren<MeshCollider>(true);
            if (colliders.Length != 19 || colliders.Any(x => x.sharedMesh == null || !x.enabled))
                throw new InvalidOperationException("Expected 19 enabled source colliders with actual meshes; found " + colliders.Length);

            // Overrides are confined to a local NavMeshBuildSettings value.
            var settings = NavMesh.GetSettingsByID(0);
            settings.agentRadius = 5.355f;
            settings.agentHeight = 21.42f;
            settings.agentClimb = 3.213f; // local stepOffset 0.3 * saved Stage3 scale 10.71
            settings.agentSlope = 45f;
            settings.overrideVoxelSize = true;
            settings.voxelSize = 1.0f;
            settings.overrideTileSize = true;
            settings.tileSize = 128;
            report.AppendLine($"- Agent: radius={settings.agentRadius}, height={settings.agentHeight}, climb={settings.agentClimb}, slope={settings.agentSlope}, voxel={settings.voxelSize}, tileSize={settings.tileSize}");

            var sources = new List<NavMeshBuildSource>();
            NavMeshBuilder.CollectSources(collidersRoot, ~0, NavMeshCollectGeometry.PhysicsColliders, 0, new List<NavMeshBuildMarkup>(), sources);
            report.AppendLine("- Physics collision sources collected: " + sources.Count);
            if (sources.Count < 19) throw new InvalidOperationException("Not all expected physics colliders collected");
            Bounds bounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++) bounds.Encapsulate(colliders[i].bounds);
            bounds.Expand(new Vector3(60f, 80f, 60f));
            report.AppendLine($"- Bake bounds min={bounds.min} max={bounds.max}");
            var data = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
            if (data == null) throw new InvalidOperationException("BuildNavMeshData returned null");
            report.AppendLine("- Standalone in-memory bake: SUCCEEDED");
            handle = NavMesh.AddNavMeshData(data);
            if (!handle.valid) throw new InvalidOperationException("NavMeshDataInstance invalid after AddNavMeshData");

            var points = new[]
            {
                new Vector3(6f, 1.44f, 12f),
                new Vector3(-276f, 6f, -132f),
                new Vector3(-624f, 12f, -306f),
                new Vector3(-1280f, 18f, -620f),
                new Vector3(-2020f, 30f, -980f)
            };
            var onNav = new bool[5];
            var navPositions = new Vector3[5];
            string[] names = { "Start", "HollowKnight", "DrownedCaptain", "Titan", "VanderPoel" };
            for (int i = 0; i < points.Length; i++)
            {
                onNav[i] = NavMesh.SamplePosition(points[i] + Vector3.up * 3f, out var hit, 40f, NavMesh.AllAreas);
                navPositions[i] = hit.position;
                report.AppendLine($"- {names[i]} nav sample: {(onNav[i] ? "FOUND" : "NOT FOUND")}, position={hit.position}");
            }
            int completed = 0;
            for (int i = 0; i < 4; i++)
            {
                if (!onNav[i] || !onNav[i + 1])
                {
                    report.AppendLine($"- {names[i]} <-> {names[i + 1]}: BLOCKED (NavMesh sample missing)");
                    continue;
                }
                for (int dir = 0; dir < 2; dir++)
                {
                    var path = new NavMeshPath();
                    var a = dir == 0 ? navPositions[i] : navPositions[i + 1];
                    var b = dir == 0 ? navPositions[i + 1] : navPositions[i];
                    bool found = NavMesh.CalculatePath(a, b, NavMesh.AllAreas, path);
                    bool okay = found && path.status == NavMeshPathStatus.PathComplete;
                    if (okay) completed++;
                    report.AppendLine($"- {names[i]} {(dir == 0 ? "->" : "<-")} {names[i + 1]}: {(okay ? "PASS" : "FAIL")} status={path.status} corners={path.corners.Length}");
                }
            }
            report.AppendLine($"- Adjacent directional paths complete: {completed}/8; do not claim Play Mode traversal from this fixture.");
            if (!File.Exists(OutputAsset))
            {
                // Save a QA-only asset, not a production navigation asset binding.
                AssetDatabase.CreateAsset(data, OutputAsset);
                report.AppendLine("- Independent test NavMesh asset: " + OutputAsset);
            }
        }
        catch (Exception e)
        {
            report.AppendLine("- FAIL/INCOMPLETE: " + e);
            Debug.LogException(e);
        }
        finally
        {
            if (handle.valid) handle.Remove();
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            if (added.IsValid() && added.isLoaded) EditorSceneManager.CloseScene(added, true);
            report.AppendLine("- Titan IK/deformed contacts, player walking, camera, attacks and Galaxy A05s: NOT TESTED");
            File.WriteAllText(OutputReport, report.ToString(), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(OutputReport);
            Debug.Log("[Stage4 NavQA] Results saved: " + OutputReport + "\n" + report);
        }
    }
}
#endif
