#if UNITY_EDITOR
using System.IO;
using UnityEditor;

[InitializeOnLoad]
internal static class Stage1OptimizedAndroidBuildTrigger
{
    private const string ReleaseLikeMarker = "Temp/Stage1OptimizedReleaseLikeBuild.trigger";

    static Stage1OptimizedAndroidBuildTrigger() => EditorApplication.update += TryRun;

    private static void TryRun()
    {
        string marker = Path.GetFullPath(ReleaseLikeMarker);
        if (!File.Exists(marker)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        File.Delete(marker);
        TemporaryAndroidBuildRunner.BuildStage1OptimizedReleaseLike();
    }
}
#endif
