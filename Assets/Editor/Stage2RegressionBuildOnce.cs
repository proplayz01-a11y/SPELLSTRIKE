#if UNITY_EDITOR
using System.IO;
using UnityEditor;

[InitializeOnLoad]
internal static class Stage2RegressionBuildOnce
{
    // One-shot marker listener. Keep this file compiling so InitializeOnLoad
    // re-registers after each diagnostic iteration.
    // Android regression build trigger. Legacy input is intentional for this device build:
    // SPELLSTRIKE's mobile joystick/camera path uses UnityEngine.Input touch/axes.
    private const string Marker = "Stage2RegressionBuild.trigger";
    private const string CpuSkinQuickMarker = "Stage2CpuSkinQuickBuild.trigger";

    static Stage2RegressionBuildOnce()
    {
        EditorApplication.update += TryRun;
    }

    private static void TryRun()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (File.Exists(CpuSkinQuickMarker))
        {
            File.Delete(CpuSkinQuickMarker);
            EditorApplication.update -= TryRun;
            TemporaryAndroidBuildRunner.BuildStage2CpuSkinningQuickTest();
            return;
        }

        if (!File.Exists(Marker))
            return;

        File.Delete(Marker);
        EditorApplication.update -= TryRun;
        TemporaryAndroidBuildRunner.BuildStage2Regression();
    }
}
#endif
