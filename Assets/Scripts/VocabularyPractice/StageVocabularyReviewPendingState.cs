using UnityEngine;

public static class StageVocabularyReviewPendingState
{
    private const string PendingStageKey = "SPELLSTRIKE.PendingVocabularyReviewStage";

    public static int PendingStageNumber => PlayerPrefs.GetInt(PendingStageKey, 0);
    public static bool HasPendingReview => PendingStageNumber > 0;

    public static void MarkPending(int stageNumber)
    {
        PlayerPrefs.SetInt(PendingStageKey, Mathf.Max(1, stageNumber));
        PlayerPrefs.Save();
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(PendingStageKey);
        PlayerPrefs.Save();
    }
}
