#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Stage1ResponsiveHudAuthoringMenu
{
    private const string TestScenePath = "Assets/Scenes/OptimizationTests/Stage1_OptimizedMap_Test.unity";
    private const string TriggerPath = "Temp/Stage1ResponsiveHudAuthor.trigger";

    [InitializeOnLoadMethod]
    private static void InstallWatcher()
    {
        EditorApplication.update -= CheckTrigger;
        EditorApplication.update += CheckTrigger;
    }

    private static void CheckTrigger()
    {
        if (!File.Exists(TriggerPath))
            return;
        try { File.Delete(TriggerPath); } catch { return; }
        AuthorOnCurrentTestScene();
    }

    [MenuItem("SPELLSTRIKE/Optimization/9. Author Responsive HUD On Test Scene")]
    public static void AuthorOnCurrentTestScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[Stage1 Responsive HUD] Exit Play Mode before authoring the test scene.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded || !string.Equals(scene.path, TestScenePath, StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogError($"[Stage1 Responsive HUD] Open {TestScenePath} first.");
            return;
        }

        RectTransform attackButton = FindNamedRect(scene, "AttackButton");
        Canvas combatCanvas = attackButton != null ? attackButton.GetComponentInParent<Canvas>() : null;
        if (combatCanvas == null)
        {
            Debug.LogError("[Stage1 Responsive HUD] AttackButton/combat Canvas was not found. No scene changes made.");
            return;
        }

        TutorialResponsiveLayout layout = combatCanvas.GetComponent<TutorialResponsiveLayout>();
        if (layout == null)
            layout = Undo.AddComponent<TutorialResponsiveLayout>(combatCanvas.gameObject);

        EditorUtility.SetDirty(layout);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log($"[Stage1 Responsive HUD] Authored on {combatCanvas.name} and saved {scene.path}.", layout);
    }

    private static RectTransform FindNamedRect(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
            {
                if (rect.name == objectName)
                    return rect;
            }
        }
        return null;
    }
}
#endif
