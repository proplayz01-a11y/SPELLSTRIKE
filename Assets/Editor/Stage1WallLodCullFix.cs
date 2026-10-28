#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class Stage1WallLodCullFix
{
    private const string MarkerPath = "Temp/Stage1WallLodCullFix.trigger";
    private const string TestScenePath = "Assets/Scenes/OptimizationTests/Stage1_OptimizedMap_Test.unity";
    private const float FinalWallLodThreshold = 0.001f;

    static Stage1WallLodCullFix()
    {
        EditorApplication.update += TryConsumeMarker;
    }

    private static void TryConsumeMarker()
    {
        if (!File.Exists(MarkerPath) || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        File.Delete(MarkerPath);
        Apply();
    }

    [MenuItem("SPELLSTRIKE/Optimization/Fix Stage 1 Wall LOD Cull Threshold")]
    public static void Apply()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != TestScenePath)
        {
            Debug.LogError($"[Stage1 Wall LOD Fix] Open {TestScenePath} before applying.");
            return;
        }

        int changed = 0;
        int found = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (LODGroup group in root.GetComponentsInChildren<LODGroup>(true))
            {
                if (!group.name.StartsWith("StoneWall_LODGroup_"))
                    continue;

                found++;
                LOD[] lods = group.GetLODs();
                if (lods.Length == 0)
                    continue;

                int last = lods.Length - 1;
                if (!Mathf.Approximately(lods[last].screenRelativeTransitionHeight, FinalWallLodThreshold))
                {
                    Undo.RecordObject(group, "Extend Stone Wall LOD3 Range");
                    lods[last].screenRelativeTransitionHeight = FinalWallLodThreshold;
                    group.SetLODs(lods);
                    EditorUtility.SetDirty(group);
                    changed++;
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log($"[Stage1 Wall LOD Fix] COMPLETE | found={found} | changed={changed} | finalThreshold={FinalWallLodThreshold:P1} | walls now stay on LOD3 until near camera/far-clip cutoff.");
    }
}
#endif
