#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class StageVocabularyReviewProfileSetupMenu
{
    private const string MenuPath =
        "SPELLSTRIKE/Vocabulary/Configure Shared Stage Review Profiles";
    private const string Stage1ProfilePath =
        "Assets/Data/Vocabulary/Stage1_Vocabulary_Candidate.asset";
    private const string Stage2ProfilePath =
        "Assets/Data/Vocabulary/Stage2_Vocabulary_Candidate.asset";

    [MenuItem(MenuPath)]
    public static void Configure()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded || scene.name != "StageSelectScene")
        {
            Debug.LogError(
                "[Review Profile Setup] Open StageSelectScene before running this command. "
                + "No scene changes were made.");
            return;
        }

        StageVocabularyProfile stage1Profile =
            AssetDatabase.LoadAssetAtPath<StageVocabularyProfile>(Stage1ProfilePath);
        StageVocabularyProfile stage2Profile =
            AssetDatabase.LoadAssetAtPath<StageVocabularyProfile>(Stage2ProfilePath);
        StageSelectManager stageSelectManager = FindSceneComponent<StageSelectManager>(scene);
        StageVocabularyReviewController reviewController =
            FindSceneComponent<StageVocabularyReviewController>(scene);

        if (stage1Profile == null
            || stage2Profile == null
            || stage1Profile.StageNumber != 1
            || stage2Profile.StageNumber != 2
            || stage1Profile.WordCount != 4
            || stage2Profile.WordCount != 5
            || stageSelectManager == null
            || reviewController == null)
        {
            Debug.LogError(
                "[Review Profile Setup] Required StageSelect references or review profiles are missing. "
                + $"Stage1Profile={stage1Profile != null}, Stage2Profile={stage2Profile != null}, "
                + $"StageSelectManager={stageSelectManager != null}, ReviewController={reviewController != null}. "
                + "No scene changes were made.");
            return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Configure Shared Stage Review Profiles");
        int undoGroup = Undo.GetCurrentGroup();

        try
        {
            Undo.RecordObject(stageSelectManager, "Wire Shared Stage Review Profiles");
            SerializedObject serializedManager = new SerializedObject(stageSelectManager);
            Set(serializedManager, "vocabularyReviewController", reviewController);
            Set(serializedManager, "stage1VocabularyReviewProfile", stage1Profile);
            Set(serializedManager, "stage2VocabularyReviewProfile", stage2Profile);
            serializedManager.ApplyModifiedProperties();

            EditorUtility.SetDirty(stageSelectManager);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeObject = stageSelectManager;
            EditorGUIUtility.PingObject(stageSelectManager);

            Debug.Log(
                "[Review Profile Setup] Wired the existing shared ReviewRoot controller to "
                + "Stage 1 and Stage 2 profiles. The scene is dirty but was not auto-saved.",
                stageSelectManager);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[Review Profile Setup] Setup failed: {exception.Message}");
            Undo.RevertAllDownToGroup(undoGroup);
        }
        finally
        {
            Undo.CollapseUndoOperations(undoGroup);
        }
    }

    private static void Set(
        SerializedObject serializedObject,
        string propertyName,
        UnityEngine.Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
            throw new InvalidOperationException(
                $"Serialized field '{propertyName}' was not found on {serializedObject.targetObject.GetType().Name}.");

        property.objectReferenceValue = value;
    }

    private static T FindSceneComponent<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null)
                return component;
        }

        return null;
    }
}
#endif
