#if UNITY_EDITOR
using System.IO;
using UnityEditor;

[InitializeOnLoad]
internal static class Stage2QuickBuildOnce
{
    private const string Marker = "Stage2QuickBuild.trigger";

    static Stage2QuickBuildOnce()
    {
        EditorApplication.update += TryRun;
    }

    private static void TryRun()
    {
        if (!File.Exists(Marker) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        File.Delete(Marker);
        EditorApplication.update -= TryRun;
        TemporaryAndroidBuildRunner.BuildStage2QuickTest();
    }
}
#endif
