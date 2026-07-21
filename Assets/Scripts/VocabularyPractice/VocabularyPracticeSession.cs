using System;
using System.Collections.Generic;
using UnityEngine;

public enum VocabularyActivityType
{
    Teach,
    CombatRetrieval,
    ReviewFirstAttempt,
    Retry
}

[Serializable]
public class VocabularyAttemptRecord
{
    public int stageNumber;
    public string wordId;
    public string targetWord;
    public VocabularyActivityType activityType;
    public string submittedResponse;
    public bool correct;
    public bool firstAttempt;
    public int attemptNumber;
    public string timestampUtc;
}

public class VocabularyPracticeSession : MonoBehaviour
{
    public static VocabularyPracticeSession Instance { get; private set; }

    [SerializeField] private bool persistAcrossScenes;
    [SerializeField] private List<VocabularyAttemptRecord> attempts = new List<VocabularyAttemptRecord>();

    public IReadOnlyList<VocabularyAttemptRecord> Attempts => attempts;
    public int FirstAttemptCorrect { get; private set; }
    public int FirstAttemptIncorrect { get; private set; }
    public int RetryAttempts { get; private set; }
    public int CombatRetrievals { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (persistAcrossScenes)
            DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void ResetSession()
    {
        attempts.Clear();
        FirstAttemptCorrect = 0;
        FirstAttemptIncorrect = 0;
        RetryAttempts = 0;
        CombatRetrievals = 0;
    }

    public void RecordTeachCompleted(StageVocabularyProfile profile, VocabularyWordData word)
    {
        AddRecord(profile, word, VocabularyActivityType.Teach, word != null ? word.Word : string.Empty, true, false, 1);
    }

    public void RecordCombatSubmission(StageVocabularyProfile profile, VocabularyWordData word, string submittedWord, bool correct)
    {
        AddRecord(profile, word, VocabularyActivityType.CombatRetrieval, submittedWord, correct, false,
            CountAttempts(word, VocabularyActivityType.CombatRetrieval) + 1);

        if (correct)
            CombatRetrievals++;
    }

    public void RecordReviewAnswer(StageVocabularyProfile profile, VocabularyWordData word, string selectedAnswer, bool correct)
    {
        AddRecord(profile, word, VocabularyActivityType.ReviewFirstAttempt, selectedAnswer, correct, true, 1);
        if (correct)
            FirstAttemptCorrect++;
        else
            FirstAttemptIncorrect++;
    }

    public void RecordRetry(StageVocabularyProfile profile, VocabularyWordData word, string submittedWord, bool correct)
    {
        int attemptNumber = CountAttempts(word, VocabularyActivityType.Retry) + 1;
        AddRecord(profile, word, VocabularyActivityType.Retry, submittedWord, correct, false, attemptNumber);
        RetryAttempts++;
    }

    public string BuildPrototypeSummary(int totalItems)
    {
        int safeTotal = Mathf.Max(1, totalItems);
        float percentage = FirstAttemptCorrect * 100f / safeTotal;
        return $"First attempt: {FirstAttemptCorrect}/{safeTotal} ({percentage:0}%)\n" +
               $"Missed words: {FirstAttemptIncorrect}\n" +
               $"Retry attempts: {RetryAttempts}\n" +
               $"Combat target-word retrievals: {CombatRetrievals}";
    }

    private int CountAttempts(VocabularyWordData word, VocabularyActivityType type)
    {
        if (word == null)
            return 0;

        return attempts.FindAll(record => record.activityType == type && record.wordId == word.WordId).Count;
    }

    private void AddRecord(
        StageVocabularyProfile profile,
        VocabularyWordData word,
        VocabularyActivityType type,
        string response,
        bool correct,
        bool firstAttempt,
        int attemptNumber)
    {
        attempts.Add(new VocabularyAttemptRecord
        {
            stageNumber = profile != null ? profile.StageNumber : 1,
            wordId = word != null ? word.WordId : string.Empty,
            targetWord = word != null ? word.Word : string.Empty,
            activityType = type,
            submittedResponse = response ?? string.Empty,
            correct = correct,
            firstAttempt = firstAttempt,
            attemptNumber = Mathf.Max(1, attemptNumber),
            timestampUtc = DateTime.UtcNow.ToString("o")
        });
    }
}
