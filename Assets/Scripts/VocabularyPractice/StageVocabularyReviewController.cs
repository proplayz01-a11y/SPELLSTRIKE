using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class StageVocabularyReviewController : MonoBehaviour
{
    private enum ReviewState
    {
        Hidden,
        Question,
        Feedback,
        Retry,
        Summary
    }

    private class ReviewChoice
    {
        public string word;
        public string incorrectExplanation;
        public bool correct;
    }

    [Header("Vocabulary")]
    [SerializeField] private StageVocabularyProfile profile;
    [Min(0)] [SerializeField] private int prototypeWordIndex;

    [Header("Question UI")]
    [SerializeField] private GameObject reviewPanel;
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private Button[] answerButtons = new Button[4];
    [SerializeField] private TMP_Text[] answerLabels = new TMP_Text[4];

    [Header("Feedback and Retry UI")]
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private TMP_Text retryInstructionText;
    [SerializeField] private Button continueButton;
    [SerializeField] private TMP_Text continueButtonLabel;
    [SerializeField] private Button hintButton;

    [Header("Summary UI")]
    [SerializeField] private GameObject summaryPanel;
    [SerializeField] private TMP_Text summaryText;

    [Header("Gameplay References")]
    [SerializeField] private TileManager tileManager;
    [SerializeField] private AttackController attackController;
    [SerializeField] private VocabularyPracticeSession session;

    [Header("Events")]
    [SerializeField] private UnityEvent onReviewCompleted;

    private readonly List<ReviewChoice> choices = new List<ReviewChoice>();
    private ReviewState state = ReviewState.Hidden;
    private VocabularyWordData currentWord;
    private bool firstAnswerCorrect;
    private bool retryRequired;
    private bool retryCompleted;
    private int retryHintLevel;
    private Action completionCallback;

    public bool ReviewCompleted { get; private set; }

    private void Awake()
    {
        if (reviewPanel != null)
            reviewPanel.SetActive(false);
        if (summaryPanel != null)
            summaryPanel.SetActive(false);

        for (int i = 0; i < answerButtons.Length; i++)
        {
            int capturedIndex = i;
            if (answerButtons[i] != null)
                answerButtons[i].onClick.AddListener(() => SelectAnswer(capturedIndex));
        }

        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinuePressed);
        if (hintButton != null)
            hintButton.onClick.AddListener(ShowRetryHint);
    }

    private void Update()
    {
        if (state != ReviewState.Retry || currentWord == null || tileManager == null)
            return;

        string formedWord = tileManager.GetCurrentWord();
        if (currentWord.Matches(formedWord))
        {
            session?.RecordRetry(profile, currentWord, formedWord, true);
            retryCompleted = true;
            state = ReviewState.Feedback;
            if (feedbackText != null)
                feedbackText.text = $"Corrective retry completed. {currentWord.Word} means {LowercaseFirst(currentWord.Definition)}\nExample: {currentWord.ContextSentence}";
            if (retryInstructionText != null)
                retryInstructionText.text = string.Empty;
            if (hintButton != null)
                hintButton.gameObject.SetActive(false);
            SetContinueButton(true, "VIEW SUMMARY");
            return;
        }

        if (formedWord.Length >= currentWord.Word.Length)
        {
            session?.RecordRetry(profile, currentWord, formedWord, false);
            if (feedbackText != null)
                feedbackText.text = "That reconstruction is not correct yet. Use the meaning and context, then try again.";
            tileManager.ResetTileSelection();
        }
    }

    public void BeginReview()
    {
        BeginReview(null);
    }

    public void BeginReview(Action onCompleted)
    {
        ResolveReferences();
        currentWord = profile != null ? profile.GetWord(prototypeWordIndex) : null;
        if (currentWord == null)
        {
            Debug.LogWarning("[StageVocabularyReview] No prototype word is configured.", this);
            onCompleted?.Invoke();
            return;
        }

        completionCallback = onCompleted;
        ReviewCompleted = false;
        firstAnswerCorrect = false;
        retryRequired = false;
        retryCompleted = false;
        retryHintLevel = 0;
        attackController?.SetAttackInputLocked(true);
        tileManager?.ResetTileSelection();

        if (reviewPanel != null)
            reviewPanel.SetActive(true);
        if (summaryPanel != null)
            summaryPanel.SetActive(false);

        BuildChoices();
        ShowQuestion();
        Debug.Log($"[StageVocabularyReview] Review started for {currentWord.Word}.", this);
    }

    public void SelectAnswer(int index)
    {
        if (state != ReviewState.Question || index < 0 || index >= choices.Count)
            return;

        ReviewChoice selected = choices[index];
        firstAnswerCorrect = selected.correct;
        retryRequired = !selected.correct;
        session?.RecordReviewAnswer(profile, currentWord, selected.word, selected.correct);
        state = ReviewState.Feedback;

        SetAnswersInteractable(false);
        if (feedbackText != null)
        {
            feedbackText.text = selected.correct
                ? $"Correct. {currentWord.Word} means {LowercaseFirst(currentWord.Definition)}\nExample: {currentWord.ContextSentence}"
                : $"Not quite. {selected.incorrectExplanation}\nThe correct word is {currentWord.Word}, which means {LowercaseFirst(currentWord.Definition)}\nExample: {currentWord.ContextSentence}";
        }

        SetContinueButton(true, selected.correct ? "VIEW SUMMARY" : "RETRY MISSED WORD");
    }

    public void ShowRetryHint()
    {
        if (state != ReviewState.Retry || currentWord == null || retryInstructionText == null)
            return;

        retryHintLevel++;
        if (retryHintLevel == 1)
            retryInstructionText.text = $"Hint: The word begins with '{currentWord.Word[0]}'.";
        else if (retryHintLevel == 2)
            retryInstructionText.text = $"Hint: {currentWord.Word.Substring(0, Mathf.Min(2, currentWord.Word.Length))}{new string('_', Mathf.Max(0, currentWord.Word.Length - 2))}";
        else
            retryInstructionText.text = $"Review the word {currentWord.Word}, then rebuild it from the shuffled tiles.";
    }

    private void OnContinuePressed()
    {
        if (state == ReviewState.Feedback && retryRequired && !retryCompleted)
        {
            BeginRetry();
            return;
        }

        if (state == ReviewState.Feedback)
        {
            ShowSummary();
            return;
        }

        if (state == ReviewState.Summary)
            CompleteReview();
    }

    private void ShowQuestion()
    {
        state = ReviewState.Question;
        if (questionText != null)
            questionText.text = currentWord.ReviewQuestion;
        if (feedbackText != null)
            feedbackText.text = string.Empty;
        if (retryInstructionText != null)
            retryInstructionText.text = string.Empty;
        if (hintButton != null)
            hintButton.gameObject.SetActive(false);

        int visibleChoices = Mathf.Min(answerButtons.Length, choices.Count);
        for (int i = 0; i < answerButtons.Length; i++)
        {
            bool visible = i < visibleChoices;
            if (answerButtons[i] != null)
            {
                answerButtons[i].gameObject.SetActive(visible);
                answerButtons[i].interactable = visible;
            }
            if (visible && i < answerLabels.Length && answerLabels[i] != null)
                answerLabels[i].text = choices[i].word;
        }

        SetContinueButton(false, "CONTINUE");
    }

    private void BeginRetry()
    {
        if (tileManager == null)
        {
            Debug.LogWarning("[StageVocabularyReview] TileManager is required for retry.", this);
            ShowSummary();
            return;
        }

        state = ReviewState.Retry;
        retryHintLevel = 0;
        tileManager.ResetTileSelection();
        bool lettersAvailable = tileManager.EnsureWordLettersAvailable(currentWord.Word, 16);
        SetAnswersVisible(false);

        if (retryInstructionText != null)
            retryInstructionText.text = lettersAvailable
                ? $"Use the definition and context to reconstruct the missed word from shuffled tiles.\n{currentWord.Definition}"
                : "Required retry tiles could not be prepared. Check the Tile Pool setup.";
        if (hintButton != null)
            hintButton.gameObject.SetActive(true);
        SetContinueButton(false, "VIEW SUMMARY");
    }

    private void ShowSummary()
    {
        state = ReviewState.Summary;
        tileManager?.ResetTileSelection();
        SetAnswersVisible(false);
        if (questionText != null)
            questionText.text = "STAGE VOCABULARY REVIEW SUMMARY";
        if (feedbackText != null)
            feedbackText.text = string.Empty;
        if (retryInstructionText != null)
            retryInstructionText.text = string.Empty;
        if (hintButton != null)
            hintButton.gameObject.SetActive(false);
        if (summaryPanel != null)
            summaryPanel.SetActive(true);
        if (summaryText != null)
            summaryText.text = session != null
                ? session.BuildPrototypeSummary(1)
                : $"First attempt: {(firstAnswerCorrect ? 1 : 0)}/1\nReview completed.";
        SetContinueButton(true, "CONTINUE");
    }

    private void CompleteReview()
    {
        ReviewCompleted = true;
        state = ReviewState.Hidden;
        attackController?.SetAttackInputLocked(false);
        if (reviewPanel != null)
            reviewPanel.SetActive(false);
        if (summaryPanel != null)
            summaryPanel.SetActive(false);

        Action callback = completionCallback;
        completionCallback = null;
        callback?.Invoke();
        onReviewCompleted?.Invoke();
        Debug.Log("[StageVocabularyReview] Review and summary completed.", this);
    }

    private void BuildChoices()
    {
        choices.Clear();
        choices.Add(new ReviewChoice { word = currentWord.Word, correct = true, incorrectExplanation = string.Empty });

        if (currentWord.Distractors != null)
        {
            foreach (VocabularyDistractor distractor in currentWord.Distractors)
            {
                if (distractor == null || string.IsNullOrWhiteSpace(distractor.Word))
                    continue;

                choices.Add(new ReviewChoice
                {
                    word = distractor.Word,
                    correct = false,
                    incorrectExplanation = distractor.Explanation
                });
            }
        }

        for (int i = 0; i < choices.Count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, choices.Count);
            ReviewChoice temporary = choices[i];
            choices[i] = choices[randomIndex];
            choices[randomIndex] = temporary;
        }
    }

    private void SetAnswersInteractable(bool interactable)
    {
        foreach (Button button in answerButtons)
        {
            if (button != null)
                button.interactable = interactable;
        }
    }

    private void SetAnswersVisible(bool visible)
    {
        foreach (Button button in answerButtons)
        {
            if (button != null)
                button.gameObject.SetActive(visible);
        }
    }

    private void SetContinueButton(bool interactable, string label)
    {
        if (continueButton != null)
            continueButton.interactable = interactable;
        if (continueButtonLabel != null)
            continueButtonLabel.text = label;
    }

    private void ResolveReferences()
    {
        if (tileManager == null)
            tileManager = FindFirstObjectByType<TileManager>();
        if (attackController == null)
            attackController = FindFirstObjectByType<AttackController>();
        if (session == null)
            session = VocabularyPracticeSession.Instance != null
                ? VocabularyPracticeSession.Instance
                : FindFirstObjectByType<VocabularyPracticeSession>();
    }

    private static string LowercaseFirst(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return char.ToLowerInvariant(value[0]) + value.Substring(1);
    }
}
