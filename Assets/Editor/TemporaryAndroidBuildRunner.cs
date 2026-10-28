#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

public static class TemporaryAndroidBuildRunner
{
    private const string Stage1OptimizedTestScene = "Assets/Scenes/OptimizationTests/Stage1_OptimizedMap_Test.unity";
    private const string Stage2Scene = "Assets/Scenes/Stage2Scene/Stage2Scene.unity";
    private const string MobilePipeline = "Assets/Settings/Mobile_RPAsset.asset";
    private const string Shadow512Pipeline = "Assets/Settings/OptimizationTests/Stage1_A05s_Shadow512_RPAsset.asset";

    [MenuItem("SPELLSTRIKE/Testing/Build Android Dev APK")]
    private static void QueueBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.ExitPlaymode();
            EditorApplication.delayCall += WaitForEditModeAndBuild;
            return;
        }

        Build();
    }

    [MenuItem("SPELLSTRIKE/Testing/Build Stage1 Optimized Test APK _F8")]
    private static void QueueStage1OptimizedTestBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.ExitPlaymode();
            EditorApplication.delayCall += WaitForEditModeAndBuildStage1OptimizedTest;
            return;
        }

        BuildStage1OptimizedTest();
    }

    [MenuItem("SPELLSTRIKE/Testing/Build Stage1 Optimized Release-Like APK")]
    public static void BuildStage1OptimizedReleaseLike()
    {
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", "Android", "SPELLSTRIKE-Stage1OptimizedTest-release.apk"));
        Directory.CreateDirectory(Path.GetDirectoryName(output));

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Stage1OptimizedTestScene) == null)
            throw new FileNotFoundException("Stage 1 optimized test scene was not found.", Stage1OptimizedTestScene);

        Debug.Log($"[AndroidBuild] Building RELEASE-LIKE optimized Stage 1 test scene to {output}");

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { Stage1OptimizedTestScene },
            locationPathName = output,
            target = BuildTarget.Android,
            options = BuildOptions.None,
            extraScriptingDefines = new[] { "SPELLSTRIKE_STAGE1_A05S_TEST" }
        };

        RenderPipelineAsset originalPipeline = QualitySettings.renderPipeline;
        RenderPipelineAsset shadow512Pipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(Shadow512Pipeline);
        if (shadow512Pipeline == null)
            throw new FileNotFoundException("Stage 1 A05s 512-shadow test pipeline was not found.", Shadow512Pipeline);

        BuildReport report;
        try
        {
            QualitySettings.renderPipeline = shadow512Pipeline;
            Debug.Log($"[AndroidBuild] A05s test-only pipeline override={Shadow512Pipeline} (main shadow 512). Production {MobilePipeline} is not modified by this build override.");
            report = BuildPipeline.BuildPlayer(options);
        }
        finally
        {
            QualitySettings.renderPipeline = originalPipeline;
        }

        BuildSummary summary = report.summary;
        Debug.Log($"[AndroidBuild] Stage1 release-like result={summary.result} size={summary.totalSize} bytes duration={summary.totalTime} warnings={summary.totalWarnings} errors={summary.totalErrors}");

        if (summary.result != BuildResult.Succeeded)
            throw new Exception($"Stage 1 optimized Android release-like build failed: {summary.result} ({summary.totalErrors} errors, {summary.totalWarnings} warnings)");
    }

    private static void WaitForEditModeAndBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += WaitForEditModeAndBuild;
            return;
        }

        Build();
    }

    private static void WaitForEditModeAndBuildStage1OptimizedTest()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += WaitForEditModeAndBuildStage1OptimizedTest;
            return;
        }

        BuildStage1OptimizedTest();
    }

    public static void Build()
    {
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", "Android", "SPELLSTRIKE-dev.apk"));
        Directory.CreateDirectory(Path.GetDirectoryName(output));

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
            throw new Exception("No enabled scenes found in EditorBuildSettings.");

        Debug.Log($"[AndroidBuild] Building {scenes.Length} enabled scenes to {output}");

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = output,
            target = BuildTarget.Android,
            options = BuildOptions.Development
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        Debug.Log($"[AndroidBuild] Result={summary.result} size={summary.totalSize} bytes duration={summary.totalTime} warnings={summary.totalWarnings} errors={summary.totalErrors}");

        if (summary.result != BuildResult.Succeeded)
            throw new Exception($"Android build failed: {summary.result} ({summary.totalErrors} errors, {summary.totalWarnings} warnings)");
    }

    [MenuItem("SPELLSTRIKE/Testing/Build Stage2 Android Regression APK")]
    public static void BuildStage2Regression()
    {
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", "Android", "SPELLSTRIKE-Stage2Regression-dev.apk"));
        Directory.CreateDirectory(Path.GetDirectoryName(output));

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Stage2Scene) == null)
            throw new FileNotFoundException("Stage 2 scene was not found.", Stage2Scene);

        Debug.Log($"[AndroidBuild] Building Stage 2 regression APK to {output}");
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { Stage2Scene },
            locationPathName = output,
            target = BuildTarget.Android,
            options = BuildOptions.Development | BuildOptions.AllowDebugging
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        Debug.Log($"[AndroidBuild] Stage2 regression result={summary.result} size={summary.totalSize} bytes duration={summary.totalTime} warnings={summary.totalWarnings} errors={summary.totalErrors}");

        if (summary.result != BuildResult.Succeeded)
            throw new Exception($"Stage 2 Android regression build failed: {summary.result} ({summary.totalErrors} errors, {summary.totalWarnings} warnings)");
    }

    [MenuItem("SPELLSTRIKE/Testing/Build Stage2 QUICK Android Test APK")]
    public static void BuildStage2QuickTest()
    {
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", "Android", "SPELLSTRIKE-Stage2Quick-dev.apk"));
        Directory.CreateDirectory(Path.GetDirectoryName(output));

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Stage2Scene) == null)
            throw new FileNotFoundException("Stage 2 scene was not found.", Stage2Scene);

        // This is intentionally a Stage-2-only iteration build. The normal regression
        // build above remains the IL2CPP/final-confidence path. For quick Android control
        // and UI tests, temporarily use Mono so Unity does not spend ~30 minutes rebuilding
        // IL2CPP native code every time. The project setting is always restored afterward.
        ScriptingImplementation originalBackend = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android);
        BuildReport report;

        try
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.Mono2x);
            Debug.Log($"[AndroidBuild] Building QUICK Stage 2-only APK (Mono) to {output}");

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { Stage2Scene },
                locationPathName = output,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            };

            report = BuildPipeline.BuildPlayer(options);
        }
        finally
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, originalBackend);
            Debug.Log($"[AndroidBuild] Restored Android scripting backend to {originalBackend} after Stage 2 quick build.");
        }

        BuildSummary summary = report.summary;
        Debug.Log($"[AndroidBuild] Stage2 QUICK result={summary.result} size={summary.totalSize} bytes duration={summary.totalTime} warnings={summary.totalWarnings} errors={summary.totalErrors}");

        if (summary.result != BuildResult.Succeeded)
            throw new Exception($"Stage 2 QUICK Android build failed: {summary.result} ({summary.totalErrors} errors, {summary.totalWarnings} warnings)");
    }

    [MenuItem("SPELLSTRIKE/Testing/Build Stage2 CPU Skinning QUICK Test APK")]
    public static void BuildStage2CpuSkinningQuickTest()
    {
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", "Android", "SPELLSTRIKE-Stage2-CPU-Skinning-Quick-dev.apk"));
        Directory.CreateDirectory(Path.GetDirectoryName(output));

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Stage2Scene) == null)
            throw new FileNotFoundException("Stage 2 scene was not found.", Stage2Scene);

        ScriptingImplementation originalBackend = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android);
        MeshDeformation originalMeshDeformation = PlayerSettings.meshDeformation;
        AndroidArchitecture originalArchitectures = PlayerSettings.Android.targetArchitectures;
        BuildReport report;

        try
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.Mono2x);
            // Unity 6 supports the Mono backend on Android ARMv7. The A05s supports
            // armeabi-v7a, so use ARMv7 only for this disposable diagnostic APK.
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7;
            PlayerSettings.meshDeformation = MeshDeformation.CPU;
            Debug.Log($"[AndroidBuild] Building Stage 2 CPU-skinning QUICK diagnostic APK to {output} (meshDeformation={PlayerSettings.meshDeformation}, backend={PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android)}, architectures={PlayerSettings.Android.targetArchitectures})");

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { Stage2Scene },
                locationPathName = output,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            };

            report = BuildPipeline.BuildPlayer(options);
        }
        finally
        {
            // This diagnostic is disposable. Restore SPELLSTRIKE's canonical
            // production Android settings explicitly so a prior failed/partial
            // diagnostic cannot leave the project on Mono/ARMv7/CPU deformation.
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.meshDeformation = MeshDeformation.GPUBatched;
            AssetDatabase.SaveAssets();
            Debug.Log($"[AndroidBuild] Restored Android backend={PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android)}, architectures={PlayerSettings.Android.targetArchitectures}, meshDeformation={PlayerSettings.meshDeformation} after CPU-skinning diagnostic build.");
        }

        BuildSummary summary = report.summary;
        Debug.Log($"[AndroidBuild] Stage2 CPU-skinning QUICK result={summary.result} size={summary.totalSize} bytes duration={summary.totalTime} warnings={summary.totalWarnings} errors={summary.totalErrors}");

        if (summary.result != BuildResult.Succeeded)
            throw new Exception($"Stage 2 CPU-skinning quick Android build failed: {summary.result} ({summary.totalErrors} errors, {summary.totalWarnings} warnings)");
    }

    public static void BuildStage1OptimizedTest()
    {
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", "Android", "SPELLSTRIKE-Stage1OptimizedTest-dev.apk"));
        Directory.CreateDirectory(Path.GetDirectoryName(output));

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Stage1OptimizedTestScene) == null)
            throw new FileNotFoundException("Stage 1 optimized test scene was not found.", Stage1OptimizedTestScene);

        Debug.Log($"[AndroidBuild] Building optimized Stage 1 test scene to {output}");

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { Stage1OptimizedTestScene },
            locationPathName = output,
            target = BuildTarget.Android,
            options = BuildOptions.Development | BuildOptions.ConnectWithProfiler | BuildOptions.AllowDebugging,
            extraScriptingDefines = new[] { "SPELLSTRIKE_STAGE1_A05S_TEST" }
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        Debug.Log($"[AndroidBuild] Stage1 optimized test result={summary.result} size={summary.totalSize} bytes duration={summary.totalTime} warnings={summary.totalWarnings} errors={summary.totalErrors}");

        if (summary.result != BuildResult.Succeeded)
            throw new Exception($"Stage 1 optimized Android test build failed: {summary.result} ({summary.totalErrors} errors, {summary.totalWarnings} warnings)");
    }
}
#endif
