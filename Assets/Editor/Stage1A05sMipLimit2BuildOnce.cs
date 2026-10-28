#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class Stage1A05sMipLimit2BuildOnce
{
    // Temp is cleared on a fresh Unity launch, which caused this one-shot build
    // hook to fire again every time the editor reopened. Keep the marker outside
    // Unity's disposable Temp folder so the build really remains one-shot.
    private const string StartedMarker = "RecoveryBackups/Stage1A05sMipStripBuild.started";

    static Stage1A05sMipLimit2BuildOnce()
    {
        EditorApplication.delayCall += TryStart;
    }

    private static void TryStart()
    {
        if (File.Exists(StartedMarker))
            return;

        if (System.Diagnostics.Process.GetProcessesByName("VALORANT-Win64-Shipping").Length > 0)
        {
            Debug.LogWarning("[Stage1 A05s MipStrip Build] VALORANT is running. Build was not started.");
            return;
        }

        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += TryStart;
            return;
        }

        File.WriteAllText(StartedMarker, System.DateTime.UtcNow.ToString("O"));
        Debug.Log("[Stage1 A05s MipStrip Build] Starting dedicated Android test build with PlayerSettings mip stripping enabled.");
        TemporaryAndroidBuildRunner.BuildStage1OptimizedTest();
    }
}
#endif
