using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StageCompleteUI : MonoBehaviour
{
    [Header("Panel Reference")]
    public GameObject stageCompletePanel;
    public GameObject dimOverlay;

    [Header("Reusable Stage Content")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bossNameText;
    [SerializeField] private TextMeshProUGUI rewardsText;

    [Header("Optional Vocabulary Review Gate")]
    [SerializeField] private bool requireVocabularyReview;
    [SerializeField] private StageVocabularyReviewController vocabularyReviewController;
    [SerializeField] private bool showLegacyStageCompletePanelAfterReview;
    [SerializeField] private bool presentVocabularyReviewInStageSelectScene = true;
    [SerializeField, Min(1)] private int completedStageNumber = 1;

    private bool vocabularyReviewCompleted;
    private bool completionRecorded;
    private StageRewardPresentation rewardPresentation;

    private void Start()
    {
        ResolveStageNumberFromScene();
        ResolveContentReferences();
        ApplyStageContent();

        if (stageCompletePanel != null)
            stageCompletePanel.SetActive(false);

        if (dimOverlay != null)
            dimOverlay.SetActive(false);
    }

    public void ShowStageComplete()
    {
        ResolveStageNumberFromScene();
        ResolveContentReferences();
        ApplyStageContent();
        // Persist the mandatory Stage Select review BEFORE granting the next
        // stage unlock. A player can close/restart the game while this modal
        // is visible, before clicking Continue.
        EnsureStageSelectReviewPending();
        RecordStageCompletion();

        if (dimOverlay != null)
            dimOverlay.SetActive(true);

        Time.timeScale = 0f;

        if (requireVocabularyReview
            && !presentVocabularyReviewInStageSelectScene
            && !vocabularyReviewCompleted)
        {
            if (vocabularyReviewController != null)
            {
                if (stageCompletePanel != null)
                    stageCompletePanel.SetActive(false);

                vocabularyReviewController.BeginReview(OnVocabularyReviewCompleted);
                Debug.Log("[StageCompleteUI] Stage combat cleared. Vocabulary review started before Stage Select.");
                return;
            }

            Debug.LogWarning("[StageCompleteUI] Vocabulary review is required but no review controller is assigned. Falling back to the existing Stage Complete panel.");
        }

        // New reusable tutorial-themed card. Keep the legacy panel as a
        // safe fallback only when its shared art prefab is missing.
        if (rewardPresentation != null)
            return;
        rewardPresentation = OpenRewardPresentation(OnContinuePressed, OnMainMenuPressed);
        if (rewardPresentation != null)
        {
            if (stageCompletePanel != null) stageCompletePanel.SetActive(false);
            if (dimOverlay != null) dimOverlay.SetActive(false);
        }
        else if (stageCompletePanel != null)
            stageCompletePanel.SetActive(true);

        Debug.Log("[StageCompleteUI] Stage complete panel shown. Game paused.");
    }

    private void OnVocabularyReviewCompleted()
    {
        vocabularyReviewCompleted = true;

        if (showLegacyStageCompletePanelAfterReview)
        {
            ShowStageComplete();
            return;
        }

        OnContinuePressed();
    }

    public void OnContinuePressed()
    {
        NavigateAfterCompletion("StageSelectScene");
    }

    public void OnMainMenuPressed()
    {
        NavigateAfterCompletion("Main Menu Scene");
    }

    private void NavigateAfterCompletion(string destination)
    {
        ResolveStageNumberFromScene();
        EnsureStageSelectReviewPending();
        RecordStageCompletion();

        if (requireVocabularyReview
            && presentVocabularyReviewInStageSelectScene
            && !vocabularyReviewCompleted)
        {
            VocabularyPracticeSession.Instance?.EnsureCrossScenePersistence();
        }

        // Release our modal's input locks and restore canvas state BEFORE
        // scene transition, so persistent HUD ownership in the destination
        // is not overwritten by a delayed OnDisable from this screen.
        if (rewardPresentation != null)
        {
            rewardPresentation.gameObject.SetActive(false);
            Destroy(rewardPresentation.gameObject);
            rewardPresentation = null;
        }
        Time.timeScale = 1f;
        Debug.Log("[StageCompleteUI] Continuing to " + destination);
        SceneManager.LoadScene(destination);
    }

    private StageRewardPresentation OpenRewardPresentation(System.Action onContinue, System.Action onMainMenu)
    {
        string title = titleText != null ? titleText.text : $"STAGE {completedStageNumber} CLEARED!";
        string boss = bossNameText != null ? bossNameText.text : "";
        string rewards = rewardsText != null ? rewardsText.text : "";
        return StageRewardPresentation.Open(completedStageNumber, title, boss, rewards,
            onContinue, onMainMenu);
    }

#if UNITY_EDITOR
    // Non-persistent visual QA; never records completion or changes review state.
    public bool PreviewRewardPresentation()
    {
        if (!Application.isPlaying || rewardPresentation != null) return false;
        ResolveStageNumberFromScene();
        ResolveContentReferences();
        ApplyStageContent();
        float previousScale = Time.timeScale;
        Time.timeScale = 0f;
        System.Action close = () =>
        {
            if (rewardPresentation != null)
            {
                rewardPresentation.gameObject.SetActive(false);
                Destroy(rewardPresentation.gameObject);
                rewardPresentation = null;
            }
            Time.timeScale = previousScale;
        };
        rewardPresentation = OpenRewardPresentation(close, close);
        if (rewardPresentation == null) Time.timeScale = previousScale;
        return rewardPresentation != null;
    }
#endif

    private void EnsureStageSelectReviewPending()
    {
        if (!requireVocabularyReview
            || !presentVocabularyReviewInStageSelectScene
            || vocabularyReviewCompleted
            || StageVocabularyReviewPendingState.PendingStageNumber == completedStageNumber)
            return;

        StageVocabularyReviewPendingState.MarkPending(completedStageNumber);
        Debug.Log(
            $"[StageCompleteUI] Stage {completedStageNumber} mandatory vocabulary review persisted before stage unlock.");
    }

    private void ResolveContentReferences()
    {
        if (stageCompletePanel == null)
            return;

        TextMeshProUGUI[] texts = stageCompletePanel.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI text in texts)
        {
            if (text == null)
                continue;

            switch (text.gameObject.name)
            {
                case "TitleText":
                    if (titleText == null)
                        titleText = text;
                    break;
                case "BossNameText":
                    if (bossNameText == null)
                        bossNameText = text;
                    break;
                case "RewardsText":
                    if (rewardsText == null)
                        rewardsText = text;
                    break;
            }
        }
    }

    private void ResolveStageNumberFromScene()
    {
        string sceneName = gameObject.scene.IsValid()
            ? gameObject.scene.name
            : SceneManager.GetActiveScene().name;

        if (sceneName == "SampleScene")
            completedStageNumber = 1;
        else if (sceneName == "Stage2Scene")
            completedStageNumber = 2;
        else if (sceneName == "Stage3Scene")
            completedStageNumber = 3;
        else if (sceneName == "Stage4Scene")
            completedStageNumber = 4;
    }

    private void ApplyStageContent()
    {
        if (completedStageNumber == 2)
        {
            ApplyStage2Content();
            return;
        }
        if (completedStageNumber == 3)
        {
            if (titleText != null)
                titleText.text = "STAGE 3 CLEARED!";
            if (bossNameText != null)
                bossNameText.text = "Titan Earthbreaker";
            if (rewardsText != null)
                rewardsText.text = "REWARDS\n\nTitan Earthbreaker Fragment";
            return;
        }
        // Stage 4 can reuse this skin when its actual reward content is
        // authored; never accidentally display Stage 1's rewards.
        if (completedStageNumber >= 4)
        {
            if (titleText != null) titleText.text = $"STAGE {completedStageNumber} CLEARED!";
            return;
        }

        if (titleText != null)
            titleText.text = "STAGE CLEARED!";

        if (bossNameText != null)
            bossNameText.text = "Hollow Knight";

        if (rewardsText != null)
        {
            rewardsText.text =
                "REWARDS\n\nSoul Fragment (Voice)\nHollow Knight's Blade\n+10% Word Damage";
        }
    }

    private void ApplyStage2Content()
    {
        Stage2ObjectiveTracker tracker = FindFirstObjectByType<Stage2ObjectiveTracker>();
        bool pearlEarned = tracker != null && tracker.PearlEarnedThisRun;
        bool objectivesPreviouslyCompleted = tracker != null && tracker.CompletedBeforeCurrentRun;

        if (titleText != null)
            titleText.text = "STAGE 2 CLEARED!";

        if (bossNameText != null)
            bossNameText.text = "Drowned Captain";

        if (rewardsText != null)
        {
            if (pearlEarned)
            {
                rewardsText.text = "REWARDS\n\nDrowned Captain Fragment\nPurifying Pearl\nCleansing Current";
            }
            else if (objectivesPreviouslyCompleted)
            {
                rewardsText.text = "REWARDS\n\nDrowned Captain Fragment";
            }
            else
            {
                rewardsText.text = "REWARDS\n\nDrowned Captain Fragment\n\n"
                                   + "OBJECTIVE REWARD NOT EARNED\nPurifying Pearl\n"
                                   + "Complete all Stage 2 objectives on a replay.";
            }
        }
    }

    private void RecordStageCompletion()
    {
        if (completionRecorded)
            return;

        completionRecorded = true;
        int completedStageIndex = Mathf.Max(0, completedStageNumber - 1);

        StageSelectManager.CompleteStage(completedStageIndex);

        GameDatabaseManager database = GameDatabaseManager.EnsureInstance();
        if (database != null)
        {
            database.SaveStageProgress(completedStageIndex, true);
            database.FlushSave();
        }

        Debug.Log($"[StageCompleteUI] Recorded completion for Stage {completedStageNumber}.");
    }
}
