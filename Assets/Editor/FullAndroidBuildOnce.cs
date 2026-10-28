#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
internal static class FullAndroidBuildOnce
{
    private const string FlagPath = "RecoveryBackups/RUN_FULL_ANDROID_BUILD.flag";
    private const string ReportPath = "RecoveryBackups/FullAndroidBuild_20260919.txt";
    private const string OutputPath = "Builds/Android/SPELLSTRIKE-Full-dev.apk";

    private static readonly string[] Scenes =
    {
        "Assets/Scenes/Main Menu Scene.unity",
        "Assets/Scenes/StageSelectScene.unity",
        "Assets/Scenes/SampleScene.unity",
        "Assets/Scenes/ItemSelectionScene.unity",
        "Assets/Scenes/WizardsVault_ItemSelectionLive.unity",
        "Assets/Scenes/Stage2Scene/Stage2Scene.unity",
        "Assets/Scenes/Stage3Scene/Stage3Scene.unity",
        "Assets/Scenes/MinigamesScene.unity",
        "Assets/Scenes/MinigameModeSelectScene.unity",
        "Assets/Scenes/WordMasterScene.unity",
        "Assets/Scenes/SpellItOutScene.unity",
        "Assets/Scenes/WordExcavationScene.unity",
        "Assets/Scenes/StoryScene.unity",
        "Assets/Scenes/TutorialScene.unity"
    };

    static FullAndroidBuildOnce()
    {
        EditorApplication.delayCall += TryRun;
    }

    private static void TryRun()
    {
        if (!File.Exists(FlagPath))
            return;

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.ExitPlaymode();
            EditorApplication.delayCall += TryRun;
            return;
        }

        if (EditorApplication.isCompiling || EditorApplication.isUpdating || BuildPipeline.isBuildingPlayer)
        {
            EditorApplication.delayCall += TryRun;
            return;
        }

        File.Delete(FlagPath);
        RunBuild();
    }

    public static void RunBuild()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputPath));
        Directory.CreateDirectory(Path.GetDirectoryName(output));

        var sb = new StringBuilder();
        sb.AppendLine("SPELLSTRIKE FULL ANDROID BUILD");
        sb.AppendLine($"Started={DateTime.Now:O}");
        sb.AppendLine($"Output={output}");
        sb.AppendLine($"ActiveBuildTarget={EditorUserBuildSettings.activeBuildTarget}");
        sb.AppendLine($"ScriptingBackend={PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android)}");
        sb.AppendLine("Scenes:");

        foreach (string scene in Scenes)
        {
            sb.AppendLine("  " + scene);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scene) == null)
            {
                sb.AppendLine("RESULT=FAILED");
                sb.AppendLine("MissingScene=" + scene);
                File.WriteAllText(ReportPath, sb.ToString());
                throw new FileNotFoundException("Required full-build scene is missing.", scene);
            }
        }

        // Capture the exact current Stage 2 / water-resistance work before building.
        if (!Application.isBatchMode && !EditorSceneManager.SaveOpenScenes())
        {
            sb.AppendLine("RESULT=FAILED");
            sb.AppendLine("Reason=SaveOpenScenes returned false");
            File.WriteAllText(ReportPath, sb.ToString());
            throw new InvalidOperationException("Could not save the currently open Unity scenes before the full Android build.");
        }
        AssetDatabase.SaveAssets();

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = output,
            target = BuildTarget.Android,
            options = BuildOptions.Development
        };

        Debug.Log($"[FullAndroidBuild] START scenes={Scenes.Length} output={output}");
        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        sb.AppendLine($"Finished={DateTime.Now:O}");
        sb.AppendLine($"RESULT={summary.result}");
        sb.AppendLine($"SizeBytes={summary.totalSize}");
        sb.AppendLine($"Duration={summary.totalTime}");
        sb.AppendLine($"Warnings={summary.totalWarnings}");
        sb.AppendLine($"Errors={summary.totalErrors}");
        File.WriteAllText(ReportPath, sb.ToString());

        Debug.Log($"[FullAndroidBuild] RESULT={summary.result} size={summary.totalSize} duration={summary.totalTime} warnings={summary.totalWarnings} errors={summary.totalErrors}");

        if (summary.result != BuildResult.Succeeded)
            throw new Exception($"Full Android build failed: {summary.result} ({summary.totalErrors} errors, {summary.totalWarnings} warnings)");
    }
}
#endif
