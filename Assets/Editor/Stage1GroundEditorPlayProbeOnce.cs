#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
internal static class Stage1GroundEditorPlayProbeOnce
{
    private const string Marker = "Temp/Stage1GroundEditorPlayProbe.trigger";
    private const string ScenePath = "Assets/Scenes/OptimizationTests/Stage1_OptimizedMap_Test.unity";

    static Stage1GroundEditorPlayProbeOnce()
    {
        EditorApplication.delayCall += TryRun;
    }

    private static void TryRun()
    {
        if (!File.Exists(Marker)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += TryRun;
            return;
        }

        File.Delete(Marker);
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Debug.Log("[Stage1 Ground Editor Probe] Entering Play Mode for 15-second ground stability validation.");
        EditorApplication.EnterPlaymode();
    }
}
#endif
