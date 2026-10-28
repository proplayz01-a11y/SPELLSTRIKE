#if UNITY_EDITOR
using UnityEditor;

internal static class TemporaryAndroidBuildMenu
{
    [MenuItem("SPELLSTRIKE/Testing/Build Fresh Android APK _F4")]
    private static void BuildFreshAndroidApk()
    {
        TemporaryAndroidBuildRunner.Build();
    }
}
#endif
