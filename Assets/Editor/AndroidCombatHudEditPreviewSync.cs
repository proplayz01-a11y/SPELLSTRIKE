using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// An explicit WYSIWYG preview for the Android HUD. No ExecuteAlways layout
/// component, scene clone, Play Mode edits or prefab modifications required.
/// </summary>
internal static class AndroidCombatHudEditPreviewSync
{
    [MenuItem("SPELLSTRIKE/HUD/Sync Android Edit Preview", priority = 220)]
    private static void Sync()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode
            || scene.path != "Assets/Scenes/SampleScene.unity")
        {
            Debug.LogWarning("[HUD Edit Preview] Open SampleScene in Edit Mode.");
            return;
        }

        if (scene.isDirty)
        {
            Debug.LogWarning(
                "[HUD Edit Preview] Scene has unsaved changes. Save or back it " +
                "up before re-syncing; nothing was changed.");
            return;
        }

        if (TutorialResponsiveLayout.SyncAndroidEditModeCombatHud(
                scene, out string message))
        {
            Debug.Log("[HUD Edit Preview] " + message);
            SceneView.RepaintAll();
            EditorApplication.QueuePlayerLoopUpdate();
        }
        else
        {
            Debug.LogWarning("[HUD Edit Preview] " + message);
        }
    }

    [MenuItem("SPELLSTRIKE/HUD/Sync Android Edit Preview", true)]
    private static bool CanSync()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode
            && SceneManager.GetActiveScene().path ==
                "Assets/Scenes/SampleScene.unity"
            && EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android;
    }
}
