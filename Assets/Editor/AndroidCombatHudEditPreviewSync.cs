using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Explicit WYSIWYG Android HUD preview for the Stage 1 scene and canonical
/// PersistentGameplayHUD prefab. Uses the same responsive layout authority as Play Mode.
/// </summary>
internal static class AndroidCombatHudEditPreviewSync
{
    private const string Stage1Scene = "Assets/Scenes/SampleScene.unity";
    private const string PersistentHudPrefab = "Assets/Prefabs/PersistentGameplayHUD.prefab";
    private const string BackupFolder = "RecoveryBackups/HUD_FRAME_FILL_20261104";
    private const string BackupName = "PersistentGameplayHUD.prefab.pre_AndroidPrefabEditPreviewSync.bak";

    [MenuItem("SPELLSTRIKE/HUD/Sync Android Edit Preview", priority = 220)]
    private static void Sync()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[HUD Edit Preview] Exit Play Mode before syncing an Edit Mode preview.");
            return;
        }

        PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        if (prefabStage != null)
        {
            SyncPersistentHudPrefab(prefabStage);
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != Stage1Scene)
        {
            Debug.LogWarning("[HUD Edit Preview] Open SampleScene or PersistentGameplayHUD.prefab in Edit Mode.");
            return;
        }

        if (scene.isDirty)
        {
            Debug.LogWarning(
                "[HUD Edit Preview] Scene has unsaved changes. Save or back it up before re-syncing; nothing was changed.");
            return;
        }

        if (TutorialResponsiveLayout.SyncAndroidEditModeCombatHud(scene, out string message))
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

    private static void SyncPersistentHudPrefab(PrefabStage prefabStage)
    {
        if (prefabStage.assetPath != PersistentHudPrefab)
        {
            Debug.LogWarning(
                "[HUD Edit Preview] This Prefab Mode command only targets " + PersistentHudPrefab + ".");
            return;
        }

        if (prefabStage.scene.isDirty)
        {
            Debug.LogWarning(
                "[HUD Edit Preview] Prefab Stage already has unsaved edits. Save them first, then run the sync again.");
            return;
        }

        string absolutePrefabPath = Path.GetFullPath(PersistentHudPrefab);
        string absoluteBackupFolder = Path.GetFullPath(BackupFolder);
        Directory.CreateDirectory(absoluteBackupFolder);
        string backupPath = Path.Combine(absoluteBackupFolder, BackupName);
        if (File.Exists(backupPath))
        {
            string stem = Path.GetFileNameWithoutExtension(BackupName);
            string extension = Path.GetExtension(BackupName);
            int suffix = 2;
            do
            {
                backupPath = Path.Combine(
                    absoluteBackupFolder,
                    stem + "_" + suffix + extension);
                suffix++;
            }
            while (File.Exists(backupPath));
        }

        File.Copy(absolutePrefabPath, backupPath, false);
        Debug.Log("[HUD Edit Preview] Pre-sync prefab backup: " + backupPath);

        if (TutorialResponsiveLayout.SyncAndroidPersistentHudPrefabPreview(
                prefabStage.scene, out string message))
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
        if (EditorApplication.isPlayingOrWillChangePlaymode
            || EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            return false;
        }

        PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        if (prefabStage != null)
        {
            return prefabStage.assetPath == PersistentHudPrefab
                && !prefabStage.scene.isDirty;
        }

        Scene scene = SceneManager.GetActiveScene();
        return scene.path == Stage1Scene && !scene.isDirty;
    }
}