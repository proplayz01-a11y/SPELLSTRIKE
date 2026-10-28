using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manual one-click route to the user's ALREADY PENDING Stage 3 vocabulary review.
/// Keeps the open Stage 3 production scene intact, uses the normal Stage Select
/// review/controller/summary/save flow, and restores ordinary Play Mode afterward.
/// This is NOT an automatic QA host or a progress/unlock cheat.
/// </summary>
[InitializeOnLoad]
public static class Stage3PendingVocabularyReviewLauncher
{
    private const string MenuPath =
        "SPELLSTRIKE/Stage 3/Play Pending Vocabulary Review";
    private const string StageSelectPath = "Assets/Scenes/StageSelectScene.unity";
    private const string ReviewProfilePath = "Assets/Data/Vocabulary/Stage3_Vocabulary_Review.asset";
    private const string OverrideArmedKey = "SPELLSTRIKE.Stage3ReviewQuickLaunch.PlayOverrideArmed";

    static Stage3PendingVocabularyReviewLauncher()
    {
        // Subscribe only. This does not automatically launch, reposition the
        // player, answer questions, save progress, or touch the current scene.
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    [MenuItem(MenuPath)]
    public static void PlayPendingReview()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            Debug.LogWarning("[STAGE3 REVIEW QUICK LAUNCH] Exit Play Mode and finish imports first.");
            return;
        }

        Scene currentScene = SceneManager.GetActiveScene();
        if (currentScene.IsValid() && currentScene.isDirty)
        {
            Debug.LogWarning("[STAGE3 REVIEW QUICK LAUNCH] Current scene has unsaved changes. " +
                             "Save it manually before launching; no scene was changed.");
            return;
        }

        if (StageVocabularyReviewPendingState.PendingStageNumber != 3)
        {
            Debug.LogWarning("[STAGE3 REVIEW QUICK LAUNCH] No pending Stage 3 review found. " +
                             "Finish Stage 3 normally first. This shortcut never invents or skips progress.");
            return;
        }

        StageVocabularyProfile profile = AssetDatabase.LoadAssetAtPath<StageVocabularyProfile>(ReviewProfilePath);
        SceneAsset stageSelect = AssetDatabase.LoadAssetAtPath<SceneAsset>(StageSelectPath);
        if (stageSelect == null || profile == null || profile.StageNumber != 3 ||
            profile.WordCount != 7 ||
            EditorSceneManager.playModeStartScene != null ||
            SessionState.GetBool(OverrideArmedKey, false))
        {
            Debug.LogError("[STAGE3 REVIEW QUICK LAUNCH] Review profile, Stage Select scene, or " +
                           "Play Mode override state is unavailable. Nothing was modified.");
            return;
        }

        // The pending flag already exists from a genuine Stage 3 completion.
        // StageSelectManager.Start() will open the actual seven-question review.
        SessionState.SetBool(OverrideArmedKey, true);
        EditorSceneManager.playModeStartScene = stageSelect;
        Debug.Log("[STAGE3 REVIEW QUICK LAUNCH] Opening the existing pending seven-question " +
                  "Stage 3 review in Stage Select. The pending review and save data are unchanged.");
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode ||
            !SessionState.GetBool(OverrideArmedKey, false))
            return;

        SceneAsset stageSelect = AssetDatabase.LoadAssetAtPath<SceneAsset>(StageSelectPath);
        // Never erase a different start-scene override configured by the user.
        if (stageSelect != null && EditorSceneManager.playModeStartScene == stageSelect)
            EditorSceneManager.playModeStartScene = null;

        SessionState.EraseBool(OverrideArmedKey);
        Debug.Log("[STAGE3 REVIEW QUICK LAUNCH] Normal Unity Play Mode start scene restored.");
    }
}
