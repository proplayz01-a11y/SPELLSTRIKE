using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Creates the Stage 3 post-boss review from the three genuine encounter
/// vocabulary entries and all four answers used by the Node 4 Titan riddles.
/// Reuses the existing Stage Select review UI, answer/feedback flow and summary.
/// Does not replace the original encounter profile or alter any combat scene.
/// </summary>
public static class Stage3VocabularyReviewSetupMenu
{
    private const string Stage3Path = "Assets/Scenes/Stage3Scene/Stage3Scene.unity";
    private const string StageSelectPath = "Assets/Scenes/StageSelectScene.unity";
    private const string SourcePath = "Assets/Data/Vocabulary/Stage3_Vocabulary_Candidate.asset";
    private const string ReviewPath = "Assets/Data/Vocabulary/Stage3_Vocabulary_Review.asset";

    // These explanations and contextual sentences apply to the actual riddle
    // answers. If the authored riddle answers change, setup fails closed until
    // an educator supplies reviewed questions for the new answers.
    private sealed class RiddleReview
    {
        public readonly string Definition;
        public readonly string Example;
        public readonly string Correct;
        public readonly string Incorrect1;
        public readonly string Incorrect2;
        public readonly string Incorrect3;

        public RiddleReview(string definition, string example, string correct,
            string incorrect1, string incorrect2, string incorrect3)
        {
            Definition = definition;
            Example = example;
            Correct = correct;
            Incorrect1 = incorrect1;
            Incorrect2 = incorrect2;
            Incorrect3 = incorrect3;
        }
    }

    private static readonly Dictionary<string, RiddleReview> RiddleReviewContent =
        new Dictionary<string, RiddleReview>(StringComparer.OrdinalIgnoreCase)
        {
            {
                "WIND", new RiddleReview(
                    "Air moving naturally across the Earth's surface.",
                    "The wind swept loose sand across the dunes.",
                    "The wind carried sand through the desert canyon.",
                    "The traveler wore a wind on each foot instead of shoes.",
                    "The wind was a stone staircase leading underground.",
                    "The map showed a wind as a unit of distance.")
            },
            {
                "SKY", new RiddleReview(
                    "The space above the ground where clouds, the Sun, and stars appear.",
                    "The clear sky stretched above the desert at dawn.",
                    "Stars appeared in the dark sky over the sleeping camp.",
                    "The traveler folded the sky and put it inside a pocket.",
                    "The blacksmith used the sky to sharpen a sword blade.",
                    "The sky was a small jar of drinking water.")
            },
            {
                "DESERT", new RiddleReview(
                    "A very dry region that receives little rainfall.",
                    "The travelers crossed the hot desert with limited water.",
                    "The desert received so little rain that few plants survived.",
                    "The soldier used a desert to lock the wooden gate.",
                    "The desert chimed loudly when the clock struck noon.",
                    "The baker poured a desert into the cup as a drink.")
            },
            {
                "SUN", new RiddleReview(
                    "The star that gives Earth light and heat.",
                    "The Sun sank below the dunes at the end of the day.",
                    "The Sun warmed the desert sand after sunrise.",
                    "The traveler packed the Sun in a small leather bag.",
                    "The Sun is a type of fish living beneath the sand.",
                    "The merchant used the Sun as a name for a pair of boots.")
            }
        };

    [MenuItem("SPELLSTRIKE/Stage 3/Install Post-Boss Vocabulary and Riddle Review")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            Debug.LogError("[STAGE3 REVIEW] Run in idle Edit Mode only.");
            return;
        }

        Scene originalScene = SceneManager.GetActiveScene();
        if (!originalScene.IsValid() || originalScene.path != Stage3Path ||
            originalScene.isDirty)
        {
            Debug.LogError("[STAGE3 REVIEW] Open the saved Stage3Scene in Edit Mode first; refusing to disturb unsaved scene changes.");
            return;
        }

        var riddles = originalScene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Stage3RiddleController>(true)).ToArray();
        if (riddles.Length != 1)
        {
            Debug.LogError("[STAGE3 REVIEW] Expected exactly one actual Node 4 riddle controller.");
            return;
        }

        string[] answers = riddles[0].GetRiddleAnswersForReview();
        if (answers.Length != 4 || answers.Distinct(StringComparer.OrdinalIgnoreCase).Count() != 4 ||
            answers.Any(answer => !RiddleReviewContent.ContainsKey(answer)))
        {
            Debug.LogError("[STAGE3 REVIEW] Actual Node 4 riddle answers changed or are incomplete. " +
                           "Update the educator-reviewed sentences before installing.");
            return;
        }

        StageVocabularyProfile source = AssetDatabase.LoadAssetAtPath<StageVocabularyProfile>(SourcePath);
        if (!source || source.StageNumber != 3 ||
            source.WordCount < 3 ||
            source.GetWord(0).Word != "SCORCHED" ||
            source.GetWord(1).Word != "DROUGHT" ||
            source.GetWord(2).Word != "SAND")
        {
            Debug.LogError("[STAGE3 REVIEW] Node 1–3 canonical profile is missing or changed. Refusing incorrect review content.");
            return;
        }

        var words = new List<VocabularyWordData>(7);
        for (int i = 0; i < 3; i++)
            words.Add(source.GetWord(i));

        foreach (string answer in answers)
        {
            RiddleReview content = RiddleReviewContent[answer];
            words.Add(VocabularyWordData.CreateStage3RiddleReviewWord(answer,
                content.Definition, content.Example, content.Correct,
                content.Incorrect1, content.Incorrect2, content.Incorrect3));
        }

        foreach (VocabularyWordData word in words)
        {
            if (word == null || word.ReviewChoices == null ||
                word.ReviewChoices.Count != 4 ||
                word.ReviewChoices.Count(c => c != null && c.Correct) != 1 ||
                word.ReviewChoices.Any(c => c == null || string.IsNullOrWhiteSpace(c.Sentence)))
            {
                Debug.LogError("[STAGE3 REVIEW] Invalid four-choice question: " + word?.Word);
                return;
            }
        }

        Scene selectScene = SceneManager.GetSceneByPath(StageSelectPath);
        bool alreadyLoaded = selectScene.IsValid() && selectScene.isLoaded;
        if (!alreadyLoaded)
            selectScene = EditorSceneManager.OpenScene(StageSelectPath, OpenSceneMode.Additive);

        try
        {
            if (selectScene.isDirty)
            {
                Debug.LogError("[STAGE3 REVIEW] StageSelectScene has unsaved changes; refusing to overwrite them.");
                return;
            }

            StageSelectManager[] managers = selectScene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<StageSelectManager>(true)).ToArray();
            StageVocabularyReviewController[] controllers = selectScene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<StageVocabularyReviewController>(true)).ToArray();
            if (managers.Length != 1 || controllers.Length != 1)
            {
                Debug.LogError("[STAGE3 REVIEW] Expected one StageSelectManager and one functional review canvas.");
                return;
            }

            // Preserve the previous Stage Select scene bytes before a deliberate
            // one-time wiring change. Production Stage 3 stays untouched.
            string backupFolder = "Library/Stage3VocabularyReview_20261015";
            Directory.CreateDirectory(backupFolder);
            string backup = Path.Combine(backupFolder, "StageSelectScene_before_stage3_review.unity");
            if (!File.Exists(backup))
                File.Copy(StageSelectPath, backup);

            StageVocabularyProfile review = AssetDatabase.LoadAssetAtPath<StageVocabularyProfile>(ReviewPath);
            if (!review)
            {
                review = ScriptableObject.CreateInstance<StageVocabularyProfile>();
                AssetDatabase.CreateAsset(review, ReviewPath);
            }
            review.Configure(3, words);
            EditorUtility.SetDirty(review);
            AssetDatabase.SaveAssets();

            var manager = new SerializedObject(managers[0]);
            var controllerReference = manager.FindProperty("vocabularyReviewController");
            var reviewReference = manager.FindProperty("stage3VocabularyReviewProfile");
            if (controllerReference == null || reviewReference == null)
                throw new InvalidOperationException("Stage Select review serialization contract changed.");

            controllerReference.objectReferenceValue = controllers[0];
            reviewReference.objectReferenceValue = review;
            manager.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(managers[0]);
            EditorSceneManager.MarkSceneDirty(selectScene);
            if (!EditorSceneManager.SaveScene(selectScene))
                throw new InvalidOperationException("StageSelectScene could not be saved.");

            Debug.Log("[STAGE3 REVIEW] PASS. Post-Titan review routed to Stage Select: " +
                string.Join(" -> ", words.Select(w => w.Word)) +
                ". Seven contextual questions, four choices each, one correct each. " +
                "Existing Stage 1/2 review profiles and actual Stage 3 gameplay unaffected.");
        }
        finally
        {
            if (!alreadyLoaded && selectScene.isLoaded)
                EditorSceneManager.CloseScene(selectScene, true);
            SceneManager.SetActiveScene(originalScene);
        }
    }
}
