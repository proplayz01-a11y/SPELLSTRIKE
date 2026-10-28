// Visible, one-off production Stage4-only art correction: the Blender v003
// placeholder gateway plane renders as an opaque, oversaturated magenta sheet
// in Android-targeted Play Mode. Preserve both source GLBs, materials and all
// other scenes; suppress only the two scene instances of this candidate mesh.
#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class Stage4ProductionPortalCleanup
{
    private const string ScenePath = "Assets/Scenes/Stage4Scene/Stage4Scene.unity";
    private const string BackupPath = "Library/Stage4ProductionSceneIntegration_20261020/Stage4Scene_before_portal_cleanup.unity";
    private const string Name = "LastGate_Portal_URPShaderCandidate";

    [MenuItem("Stage 4/Convergence Citadel v003/FIX Actual Stage4 Magenta Gateway Plane")]
    internal static void Fix()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[Stage4Gateway] Exit Play Mode first; no change made.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath || !scene.isLoaded || File.Exists(BackupPath))
        {
            Debug.LogError("[Stage4Gateway] Open the real Stage4Scene and verify it is unmodified; previous backup is not overwritten.");
            return;
        }

        var matches = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Where(t => t.name == Name)
            .ToArray();

        if (matches.Length != 2 || matches.Any(t => !t.gameObject.activeSelf || t.GetComponent<MeshRenderer>() == null))
        {
            Debug.LogError("[Stage4Gateway] Expected exactly two active LOD0/LOD1 portal material candidates; nothing was changed. Found " + matches.Length);
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(BackupPath));
        File.Copy(ScenePath, BackupPath);
        foreach (var target in matches)
        {
            target.gameObject.SetActive(false);
            Debug.Log("[Stage4Gateway] Disabled only production mesh instance: " + target.name + " at " + target.position);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new IOException("[Stage4Gateway] Could not save production scene; original is backed up at " + BackupPath);

        Debug.Log("[Stage4Gateway] PASS: Both placeholder gateway surfaces suppressed; source GLBs and existing colliders, markers, player/HUD and LODGroup structure unchanged.");
    }
}
#endif
