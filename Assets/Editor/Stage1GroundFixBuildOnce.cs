#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class Stage1GroundFixBuildOnce
{
    private const string Marker = "Temp/Stage1GroundFixBuild.trigger";

    static Stage1GroundFixBuildOnce()
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
        Debug.Log("[Stage1 Ground Fix Build] Starting Android validation build.");
        TemporaryAndroidBuildRunner.BuildStage1OptimizedTest();
    }
}
#endif
