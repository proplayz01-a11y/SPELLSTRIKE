#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Promotes ONLY the isolated, Play-Mode-verified Bridge 04 collision proxy to
// the two new Stage 4 scenes. Original imported visual & GLBs are untouched.
public static class Stage4V003Bridge4PromoteValidatedCollision
{
    private const string Root = "Assets/Stage 4/ConvergenceCitadel_v003";
    private const string Scene1 = "Assets/Scenes/Stage4Scene/Stage4Scene.unity";
    private const string Scene2 = "Assets/Scenes/Stage4Scene/Stage4Scene_ConvergenceCitadel_v003_Validation.unity";
    private const string Qa = "Assets/Scenes/Stage4Scene/Stage4Scene_ConvergenceCitadel_v003_RuntimeQA.unity";
    private const string MeshPath = Root + "/Navigation/Bridge04_SmoothCollision_QA.mesh.asset";
    private const string Report = Root + "/Validation/UnityBridge04Promotion_20261020.md";
    private const string BackupDir = "Library/Stage4Bridge04_ValidatedPromotion_20261020";
    private const string ReplacementName = "Bridge04_SmoothCollision_Validated_Traversable";
    private const string OldName = "COL_Bridge_04_Deck_PHYSICS";

    [MenuItem("Stage 4/Convergence Citadel v003/Promote Verified Bridge04 Collision to Stage4 Scenes")]
    public static void Promote()
    {
        var scene = SceneManager.GetActiveScene();
        if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode ||
            !scene.IsValid() || scene.path != Qa || scene.isDirty)
        {
            Debug.LogError("[Stage4 Bridge04] Must be in CLEAN RuntimeQA scene Edit Mode. No edit permitted.");
            return;
        }
        if (!File.Exists(Root + "/Validation/UnityRuntimeAllBridges_AfterBridge4Proxy_20261020.md") ||
            !File.ReadAllText(Root + "/Validation/UnityRuntimeAllBridges_AfterBridge4Proxy_20261020.md")
              .Contains("Final four-bridge runtime collision regression: 8/8 directional bridge runs pass"))
        {
            Debug.LogError("[Stage4 Bridge04] Verified 8/8 real runtime crossing evidence missing. Refusing promotion.");
            return;
        }
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
        // 3 deck spans * 4 closed side quads * 6 triangle indices + two end caps * 6.
        if (mesh == null || mesh.vertexCount != 16 || mesh.triangles.Length != 84)
        {
            Debug.LogError("[Stage4 Bridge04] Approved QA mesh does not match expected closed four-station geometry.");
            return;
        }
        if (File.Exists(Report) || File.Exists(BackupDir + "/Stage4Scene.unity.before_bridge04") ||
            File.Exists(BackupDir + "/Stage4Scene_ConvergenceCitadel_v003_Validation.unity.before_bridge04"))
        {
            Debug.LogError("[Stage4 Bridge04] Evidence or backup already exists. Refusing repeat.");
            return;
        }
        foreach (string path in new[] { Scene1, Scene2 })
        {
            if (!File.Exists(path)) { Debug.LogError("[Stage4 Bridge04] Missing target " + path); return; }
            var existing = SceneManager.GetSceneByPath(path);
            if (existing.IsValid() && existing.isLoaded)
            {
                Debug.LogError("[Stage4 Bridge04] Scene already open. Close and recheck it first: " + path);
                return;
            }
        }

        Directory.CreateDirectory(BackupDir);
        File.Copy(Scene1, BackupDir + "/Stage4Scene.unity.before_bridge04");
        File.Copy(Scene2, BackupDir + "/Stage4Scene_ConvergenceCitadel_v003_Validation.unity.before_bridge04");

        var notes = new System.Text.StringBuilder();
        notes.AppendLine("# Stage4 Bridge04 collision-only promotion, 2026-10-20");
        notes.AppendLine("- Verified RuntimeQA source: all four bridges, eight directions PASS via CharacterController.Move; previous 6/8 failure preserved.");
        notes.AppendLine("- Visual GLB and source Blender geometry unchanged; only original Bridge04 deck collision is disabled and the new smooth collision proxy is enabled.");
        notes.AppendLine("- Proxy built from original authored width 30.72 and piecewise y profile 15->15->27->27 Unity units.");
        notes.AppendLine("- Backups: `" + BackupDir + "`. No original SampleScene, Stage2, Stage3, prefabs, saves or Build Settings edited.");
        bool ok = true;
        Scene originalActive = SceneManager.GetActiveScene();
        foreach (string path in new[] { Scene1, Scene2 })
        {
            Scene target = default;
            try
            {
                target = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                Transform physics = target.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                    .Single(t => t.name == "Independent_StaticMeshColliders");
                var old = physics.GetComponentsInChildren<MeshCollider>(true).Single(c => c.name == OldName);
                if (!old.enabled || !old.gameObject.isStatic || old.sharedMesh == null ||
                    physics.GetComponentsInChildren<Transform>(true).Any(t => t.name == ReplacementName))
                    throw new InvalidOperationException("Original target Bridge04 collision state was changed; refusing unsafe overwrite.");
                var proxy = new GameObject(ReplacementName);
                proxy.transform.SetParent(physics, false);
                proxy.isStatic = true;
                var collision = proxy.AddComponent<MeshCollider>();
                collision.sharedMesh = mesh;
                collision.convex = false;
                collision.isTrigger = false;
                old.enabled = false;
                if (physics.GetComponentsInChildren<MeshCollider>(true).Count(c => c.enabled) != 19)
                    throw new InvalidOperationException("Expected exactly 19 enabled collision geometries after replacement.");
                EditorSceneManager.MarkSceneDirty(target);
                if (!EditorSceneManager.SaveScene(target, path)) throw new IOException("Unable to save target scene " + path);
                notes.AppendLine("- PASS: `" + path + "`, original disabled, proxy enabled, 19 total enabled collider instances.");
                Debug.Log("[Stage4 Bridge04] Collision-only proxy promoted to " + path);
            }
            catch (Exception e)
            {
                ok = false;
                notes.AppendLine("- FAIL/PARTIAL: `" + path + "` " + e);
                Debug.LogException(e);
                break;
            }
            finally
            {
                if (originalActive.IsValid() && originalActive.isLoaded) SceneManager.SetActiveScene(originalActive);
                if (target.IsValid() && target.isLoaded) EditorSceneManager.CloseScene(target, true);
            }
        }
        notes.AppendLine(ok ? "- STATUS: PASS, verified collision proxy in both isolated Stage4 scenes. Canonical player/Titan animation QA still pending." : "- STATUS: FAILED/PARTIAL, do not rerun or integrate; inspect backups.");
        File.WriteAllText(Report, notes.ToString(), new System.Text.UTF8Encoding(false));
        AssetDatabase.ImportAsset(Report);
    }
}
#endif
