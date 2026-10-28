#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
internal static class Stage2CopyTutorialMobileControlsOnce
{
    private const string Marker = "Temp/Stage2CopyTutorialMobileControls.trigger";
    private const string TutorialScenePath = "Assets/Scenes/TutorialScene.unity";
    private const string TargetScenePath = "Assets/Scenes/Stage2Scene/Stage2Scene.unity";

    static Stage2CopyTutorialMobileControlsOnce() => EditorApplication.update += TryRun;

    private static void TryRun()
    {
        string marker = Path.GetFullPath(Marker);
        if (!File.Exists(marker))
            return;

        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        File.Delete(marker);

        Scene target = SceneManager.GetSceneByPath(TargetScenePath);
        bool targetWasLoaded = target.IsValid() && target.isLoaded;
        if (!targetWasLoaded)
            target = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Additive);

        Scene tutorial = SceneManager.GetSceneByPath(TutorialScenePath);
        bool tutorialWasLoaded = tutorial.IsValid() && tutorial.isLoaded;
        if (!tutorialWasLoaded)
            tutorial = EditorSceneManager.OpenScene(TutorialScenePath, OpenSceneMode.Additive);

        try
        {
            RectTransform sourceJoystick = FindRect(tutorial, "MobileMovementJoystick");
            if (sourceJoystick == null)
                throw new InvalidOperationException("TutorialScene MobileMovementJoystick was not found.");

            RectTransform attackButton = FindRect(target, "AttackButton");
            if (attackButton == null)
                throw new InvalidOperationException("Stage2 AttackButton was not found.");

            Canvas hostCanvas = attackButton.GetComponentInParent<Canvas>();
            if (hostCanvas == null)
                throw new InvalidOperationException("Stage2 AttackButton has no parent Canvas.");

            RectTransform existingJoystick = FindRect(target, "MobileMovementJoystick");
            if (existingJoystick != null)
                UnityEngine.Object.DestroyImmediate(existingJoystick.gameObject);

            GameObject clone = UnityEngine.Object.Instantiate(sourceJoystick.gameObject, hostCanvas.transform, false);
            clone.name = "MobileMovementJoystick";
            RectTransform joystick = clone.GetComponent<RectTransform>();
            joystick.SetAsLastSibling();

            // Keep the exact Tutorial joystick component setup and child references.
            MobileJoystickController joystickController = clone.GetComponent<MobileJoystickController>();
            RectTransform knob = clone.transform.Find("Knob") as RectTransform;
            if (joystickController == null || knob == null)
                throw new InvalidOperationException("Copied Stage2 joystick is missing MobileJoystickController or Knob.");

            CombatCamera combatCamera = FindComponentInScene<CombatCamera>(target);
            if (combatCamera == null)
                throw new InvalidOperationException("Stage2 CombatCamera was not found.");

            MobileStageControls controls = hostCanvas.GetComponent<MobileStageControls>();
            if (controls == null)
                controls = Undo.AddComponent<MobileStageControls>(hostCanvas.gameObject);

            controls.Configure(joystick, null, null, null, combatCamera);
            EditorUtility.SetDirty(controls);
            EditorUtility.SetDirty(joystickController);
            EditorUtility.SetDirty(combatCamera);

            string backupDir = Path.GetFullPath("Temp");
            Directory.CreateDirectory(backupDir);
            string backupPath = Path.Combine(backupDir, $"Stage2Scene_before_mobile_controls_{DateTime.Now:yyyyMMdd_HHmmss}.unity");
            File.Copy(Path.GetFullPath(TargetScenePath), backupPath, false);

            EditorSceneManager.MarkSceneDirty(target);
            if (!EditorSceneManager.SaveScene(target))
                throw new InvalidOperationException("Unity failed to save Stage2Scene after mobile-control authoring.");

            Debug.Log(
                $"[Stage2 Mobile Controls] COMPLETE exact Tutorial joystick copied | " +
                $"joystick={joystick.sizeDelta} range=52 | right-side camera lock wired via MobileStageControls + CombatCamera | " +
                $"backup={backupPath}");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
        finally
        {
            if (!tutorialWasLoaded && tutorial.IsValid() && tutorial.isLoaded)
                EditorSceneManager.CloseScene(tutorial, true);
            if (!targetWasLoaded && target.IsValid() && target.isLoaded)
                EditorSceneManager.CloseScene(target, true);
        }
    }

    private static RectTransform FindRect(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
                if (rect.name == objectName)
                    return rect;
        return null;
    }

    private static T FindComponentInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null)
                return component;
        }
        return null;
    }
}
#endif
