using UnityEngine;
using UnityEngine.SceneManagement;

public class StageCompleteUI : MonoBehaviour
{
    [Header("Panel Reference")]
    public GameObject stageCompletePanel;
    public GameObject dimOverlay;

    [Header("Optional Vocabulary Review Gate")]
    [SerializeField] private bool requireVocabularyReview;
    [SerializeField] private StageVocabularyReviewController vocabularyReviewController;
    [SerializeField] private bool showLegacyStageCompletePanelAfterReview;

    private bool vocabularyReviewCompleted;

    private void Start()
    {
        if (stageCompletePanel != null)
            stageCompletePanel.SetActive(false);

        if (dimOverlay != null)
            dimOverlay.SetActive(false);
    }

    public void ShowStageComplete()
    {
        if (dimOverlay != null)
            dimOverlay.SetActive(true);

        Time.timeScale = 0f;

        if (requireVocabularyReview && !vocabularyReviewCompleted)
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

        if (stageCompletePanel != null)
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
        Time.timeScale = 1f;
        Debug.Log("[StageCompleteUI] Continuing to Stage Select.");
        SceneManager.LoadScene("StageSelectScene");
    }
}
