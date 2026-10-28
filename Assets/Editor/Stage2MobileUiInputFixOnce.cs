#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class Stage2MobileUiInputFixOnce
{
    private const string Marker = "Temp/Stage2MobileUiInputFix.trigger";
    private const string TargetScenePath = "Assets/Scenes/Stage2Scene/Stage2Scene.unity";

    static Stage2MobileUiInputFixOnce() => EditorApplication.update += TryRun;

    private static void TryRun()
    {
        string marker = Path.GetFullPath(Marker);
        if (!File.Exists(marker)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        File.Delete(marker);

        Scene target = SceneManager.GetSceneByPath(TargetScenePath);
        bool targetWasLoaded = target.IsValid() && target.isLoaded;
        if (!targetWasLoaded)
            target = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Additive);

        try
        {
            EventSystem eventSystem = null;
            foreach (GameObject root in target.GetRootGameObjects())
            {
                eventSystem = root.GetComponentInChildren<EventSystem>(true);
                if (eventSystem != null) break;
            }
            if (eventSystem == null)
                throw new InvalidOperationException("Stage2 EventSystem was not found.");

            GameObject go = eventSystem.gameObject;
            string backupPath = Path.Combine(Path.GetFullPath("Temp"), $"Stage2Scene_before_legacy_ui_input_{DateTime.Now:yyyyMMdd_HHmmss}.unity");
            File.Copy(Path.GetFullPath(TargetScenePath), backupPath, false);

            int removed = 0;
            foreach (BaseInputModule module in go.GetComponents<BaseInputModule>())
            {
                if (module is StandaloneInputModule) continue;
                if (module.GetType().Name == "InputSystemUIInputModule")
                {
                    Undo.DestroyObjectImmediate(module);
                    removed++;
                }
            }

            StandaloneInputModule legacyModule = go.GetComponent<StandaloneInputModule>();
            if (legacyModule == null)
                legacyModule = Undo.AddComponent<StandaloneInputModule>(go);

            legacyModule.horizontalAxis = "Horizontal";
            legacyModule.verticalAxis = "Vertical";
            legacyModule.submitButton = "Submit";
            legacyModule.cancelButton = "Cancel";
            legacyModule.inputActionsPerSecond = 10f;
            legacyModule.repeatDelay = 0.5f;
            EditorUtility.SetDirty(legacyModule);

            EditorSceneManager.MarkSceneDirty(target);
            if (!EditorSceneManager.SaveScene(target))
                throw new InvalidOperationException("Unity failed to save Stage2Scene after mobile UI input fix.");

            Debug.Log($"[Stage2 Mobile UI Input] COMPLETE removedInputSystemModules={removed} standaloneInputModule=True backup={backupPath}");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
        finally
        {
            if (!targetWasLoaded && target.IsValid() && target.isLoaded)
                EditorSceneManager.CloseScene(target, true);
        }
    }
}
#endif
