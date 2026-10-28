#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class Stage1RemoveOldMapFromOptimizationTest
{
    private const string Marker = "Temp/Stage1RemoveOldMap.trigger";
    private const string ScenePath = "Assets/Scenes/OptimizationTests/Stage1_OptimizedMap_Test.unity";
    private const string OldRootName = "level1_REVAMPED";
    private const string OldMapPath = "Assets/Maps/level1_REVAMPED.glb";

    static Stage1RemoveOldMapFromOptimizationTest()
    {
        EditorApplication.delayCall += TryRun;
    }

    [MenuItem("SPELLSTRIKE/Optimization/A05s Remove Old Map From Test %#&r")]
    private static void RunFromMenu()
    {
        if (File.Exists(Marker))
            File.Delete(Marker);

        RunRemoval();
    }

    private static void TryRun()
    {
        if (!File.Exists(Marker)) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += TryRun;
            return;
        }

        File.Delete(Marker);

        RunRemoval();
    }

    private static void RunRemoval()
    {

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var roots = scene.GetRootGameObjects();
        var matches = roots.Where(go => go != null && go.name == OldRootName).ToArray();
        Debug.Log($"[Stage1 A05s Memory Fix] Found old map roots={matches.Length}; dependencyBefore={AssetDatabase.GetDependencies(ScenePath, true).Contains(OldMapPath)}");

        foreach (var go in matches)
        {
            Debug.Log($"[Stage1 A05s Memory Fix] Removing root '{go.name}' active={go.activeSelf} children={go.transform.childCount}");
            Object.DestroyImmediate(go);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        bool stillDepends = AssetDatabase.GetDependencies(ScenePath, true).Contains(OldMapPath);
        Debug.Log($"[Stage1 A05s Memory Fix] COMPLETE | removed={matches.Length} | oldMapDependencyAfter={stillDepends}");
    }
}
#endif
