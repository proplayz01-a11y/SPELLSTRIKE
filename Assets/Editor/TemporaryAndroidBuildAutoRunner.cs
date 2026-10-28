#if UNITY_EDITOR
using System.IO;
using UnityEditor;

[InitializeOnLoad]
internal static class TemporaryAndroidBuildAutoRunner
{
    private const string SessionKey = "SpellStrike_AndroidBuildAutoRunner_20260912_Stage1MemoryFix";
    private const string AutoRunMarkerPath = "Temp/Stage1BuildRequest.txt";

    static TemporaryAndroidBuildAutoRunner()
    {
        // Keep this temporary helper opt-in. A normal Editor launch must never
        // start an Android build automatically while doing scene work/profiling.
        if (!File.Exists(AutoRunMarkerPath))
            return;

        File.Delete(AutoRunMarkerPath);

        if (SessionState.GetBool(SessionKey, false))
            return;

        SessionState.SetBool(SessionKey, true);
        EditorApplication.delayCall += Queue;
    }

    private static void Queue()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.ExitPlaymode();
            EditorApplication.delayCall += Queue;
            return;
        }

        TemporaryAndroidBuildRunner.BuildStage1OptimizedTest();
    }
}
#endif
