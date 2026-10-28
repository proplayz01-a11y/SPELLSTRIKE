#if UNITY_EDITOR
using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class SyncTutorialJoystickToStagesOnce
{
    private const string Marker = "Temp/SyncTutorialJoystickToStages.trigger";
    private const string TutorialPath = "Assets/Scenes/TutorialScene.unity";
    // Optimization work stays isolated from the production scenes. The canonical
    // Tutorial joystick is copied into the dedicated Stage 1 Android test scene.
    private static readonly string[] StagePaths =
    {
        "Assets/Scenes/OptimizationTests/Stage1_OptimizedMap_Test.unity"
    };

    static SyncTutorialJoystickToStagesOnce() => EditorApplication.update += TryRun;

    private static void TryRun()
    {
        if (!File.Exists(Path.GetFullPath(Marker)) || EditorApplication.isCompiling
            || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        File.Delete(Path.GetFullPath(Marker));
        Scene originalActive = SceneManager.GetActiveScene();
        Scene tutorial = EditorSceneManager.OpenScene(TutorialPath, OpenSceneMode.Additive);
        try
        {
            RectTransform canonical = FindNamedRect(tutorial, "MobileMovementJoystick");
            if (canonical == null)
                throw new InvalidOperationException("Tutorial MobileMovementJoystick not found.");

            // Keep the tutorial scene loaded only as the immutable source. Opening and
            // closing one destination at a time avoids Unity's additive-scene lifecycle
            // from invalidating the next destination while this one-shot runs.
            foreach (string stagePath in StagePaths)
            {
                SyncStage(stagePath, canonical);
                SceneManager.SetActiveScene(tutorial);
            }

            Debug.Log("[Tutorial Joystick Sync] COMPLETE - Stage 1 optimization test scene now uses an exact clone of TutorialScene MobileMovementJoystick.");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
        finally
        {
            if (tutorial.IsValid() && tutorial.isLoaded)
                EditorSceneManager.CloseScene(tutorial, true);
            if (originalActive.IsValid() && originalActive.isLoaded)
                SceneManager.SetActiveScene(originalActive);
        }
    }

    private static void SyncStage(string stagePath, RectTransform canonical)
    {
        string backupDir = Path.GetFullPath("Temp/TutorialJoystickSyncBackups");
        Directory.CreateDirectory(backupDir);
        string backupPath = Path.Combine(backupDir, Path.GetFileNameWithoutExtension(stagePath) + "_before_joystick_sync.unity");
        File.Copy(Path.GetFullPath(stagePath), backupPath, true);

        Scene stage = EditorSceneManager.OpenScene(stagePath, OpenSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(stage);
            RectTransform oldJoystick = FindNamedRect(stage, "MobileMovementJoystick");
            MobileStageControls controls = FindComponent<MobileStageControls>(stage);
            if (controls == null)
                throw new InvalidOperationException($"MobileStageControls not found in {stagePath}.");

            Transform host = oldJoystick != null ? oldJoystick.parent : controls.transform;
            int sibling = oldJoystick != null ? oldJoystick.GetSiblingIndex() : host.childCount;
            if (oldJoystick != null)
                UnityEngine.Object.DestroyImmediate(oldJoystick.gameObject);

            GameObject cloneObject = UnityEngine.Object.Instantiate(canonical.gameObject, host, false);
            cloneObject.name = canonical.gameObject.name;
            RectTransform clone = cloneObject.GetComponent<RectTransform>();
            clone.SetSiblingIndex(Mathf.Min(sibling, host.childCount - 1));

            SerializedObject so = new SerializedObject(controls);
            GameObject interactionRoot = so.FindProperty("interactionButtonRoot").objectReferenceValue as GameObject;
            Button interactionButton = so.FindProperty("interactionButton").objectReferenceValue as Button;
            TMP_Text interactionLabel = so.FindProperty("interactionLabel").objectReferenceValue as TMP_Text;
            CombatCamera camera = so.FindProperty("combatCamera").objectReferenceValue as CombatCamera;
            controls.Configure(clone, interactionRoot, interactionButton, interactionLabel, camera);
            EditorUtility.SetDirty(controls);

            EditorSceneManager.MarkSceneDirty(stage);
            if (!EditorSceneManager.SaveScene(stage))
                throw new InvalidOperationException($"Failed to save {stagePath}.");

            Debug.Log($"[Tutorial Joystick Sync] Synced exact Tutorial joystick into {stagePath}; backup={backupPath}");
        }
        finally
        {
            if (tutorialSceneIsLoaded())
                SceneManager.SetActiveScene(SceneManager.GetSceneByPath(TutorialPath));
            if (stage.IsValid() && stage.isLoaded)
                EditorSceneManager.CloseScene(stage, true);
        }
    }

    private static bool tutorialSceneIsLoaded()
    {
        Scene scene = SceneManager.GetSceneByPath(TutorialPath);
        return scene.IsValid() && scene.isLoaded;
    }

    private static RectTransform FindNamedRect(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
                if (rect.name == name)
                    return rect;
        return null;
    }

    private static T FindComponent<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T value = root.GetComponentInChildren<T>(true);
            if (value != null)
                return value;
        }
        return null;
    }
}
#endif
