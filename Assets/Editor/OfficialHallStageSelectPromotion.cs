// Explicit visible Unity Editor menu to promote the already-approved Hall
// of Chronicles book scene to the existing GUID-stable production scene.
// No PlayerPrefs, stage progress, gameplay scenes or build indices are altered.
#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class OfficialHallStageSelectPromotion
{
    private const string Production = "Assets/Scenes/StageSelectScene.unity";
    private const string Preview = "Assets/Scenes/StageSelectBook_HallOfChronicles_Preview.unity";
    private const string BackupRoot = "Library/PresentationUIProductionPromotion_20261020_2352";
    private const string Backup = BackupRoot + "/OfficialStageSelectPromotion/StageSelectScene_before_hall_promote.unity";

    [MenuItem("SPELLSTRIKE/Hall of Chronicles/Promote Approved Hall to OFFICIAL StageSelectScene (Preserve GUID)")]
    private static void Promote()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            SceneManager.sceneCount != 1 ||
            SceneManager.GetActiveScene().isDirty ||
            SceneManager.GetActiveScene().path == Production)
        {
            Debug.LogError("[Official Stage Select] Exit Play and save current scene first; StageSelect must not be loaded.");
            return;
        }

        Scene test = default;
        bool copied = false;
        try
        {
            if (!File.Exists(Production) || !File.Exists(Preview) || File.Exists(Backup))
                throw new InvalidOperationException("Missing StageSelect source or existing official backup; refusing repeat.");
            string initialBackup = BackupRoot + "/Assets/Scenes/StageSelectScene.unity";
            if (!File.Exists(initialBackup) ||
                !Hash(Production).Equals(Hash(initialBackup), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Official StageSelect was edited after the approved backup. Refusing to overwrite new work.");

            string previewText = File.ReadAllText(Preview);
            const string oldName = "  - Stage4_CursedGothic";
            if (!previewText.Contains(oldName) || previewText.Split(new[] { oldName }, StringSplitOptions.None).Length != 2 ||
                !previewText.Contains("stageSceneNames:") || !previewText.Contains("Stage5_Final") ||
                !previewText.Contains("HallOfChronicles") || !previewText.Contains("StageSelectManager"))
                throw new InvalidOperationException("Latest preview content is unexpected. No production overwrite.");

            // Keep all scene-local Hall of Chronicles art overrides, controls,
            // mandatory review panels and UI references; fix only Stage4 routing.
            string replacement = previewText.Replace(oldName, "  - Stage4Scene");
            Directory.CreateDirectory(Path.GetDirectoryName(Backup));
            File.Copy(Production, Backup, overwrite: false);
            File.WriteAllText(Production, replacement, new System.Text.UTF8Encoding(false));
            copied = true;
            AssetDatabase.ImportAsset(Production, ImportAssetOptions.ForceUpdate);
            test = EditorSceneManager.OpenScene(Production, OpenSceneMode.Additive);

            StageSelectManager[] managers = test.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<StageSelectManager>(true)).ToArray();
            StageVocabularyReviewController[] review = test.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<StageVocabularyReviewController>(true)).ToArray();
            if (managers.Length != 1 || review.Length != 1 ||
                managers[0].stageSceneNames.Length < 4 || managers[0].stageSceneNames[3] != "Stage4Scene" ||
                managers[0].stageButtons.Length != 5 || managers[0].enterButton == null ||
                test.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<Transform>(true))
                    .All(x => x.name != "LibraryBackground"))
                throw new InvalidOperationException("Official Hall validation failed: manager, review, stage buttons, Hall art or Stage4 route.");
            var serialized = new SerializedObject(managers[0]);
            foreach (string name in new[] {"stage1VocabularyReviewProfile", "stage2VocabularyReviewProfile", "stage3VocabularyReviewProfile"})
                if (serialized.FindProperty(name)?.objectReferenceValue == null)
                    throw new InvalidOperationException("Pending vocabulary review profile is missing: " + name);
            bool enterCallback = false;
            for (int i=0; i < managers[0].enterButton.onClick.GetPersistentEventCount(); i++)
                if (managers[0].enterButton.onClick.GetPersistentTarget(i) == managers[0] &&
                    managers[0].enterButton.onClick.GetPersistentMethodName(i) == "OnEnterPressed")
                    enterCallback = true;
            if (!enterCallback)
                throw new InvalidOperationException("Enter button is not wired to existing StageSelectManager.");

            Debug.Log("[Official Stage Select] PASS: Approved Hall of Chronicles animated book is now official GUID-stable StageSelectScene. Stage4 routes to Stage4Scene; mandatory vocabulary gate and Enter bindings verified. SHA=" + Hash(Production));
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            if (test.IsValid() && test.isLoaded)
                EditorSceneManager.CloseScene(test, true);
            test = default;
            if (copied && File.Exists(Backup))
            {
                File.Copy(Backup, Production, overwrite: true);
                AssetDatabase.ImportAsset(Production, ImportAssetOptions.ForceUpdate);
                Debug.LogError("[Official Stage Select] Failed, restored original StageSelectScene from exact prechange backup.");
            }
        }
        finally
        {
            if (test.IsValid() && test.isLoaded)
                EditorSceneManager.CloseScene(test, true);
        }
    }
    private static string Hash(string path)
    {
        using (var sha = SHA256.Create())
        using (var stream = File.OpenRead(path))
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
    }
}
#endif
