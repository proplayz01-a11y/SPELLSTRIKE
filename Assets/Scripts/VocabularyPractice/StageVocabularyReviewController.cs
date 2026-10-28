using System;
using System.Collections;
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
        Summary
    }

    private class ReviewChoice
    {
        public VocabularyReviewChoice data;
    }

    private class AnswerButtonVisualSnapshot
    {
        public Button button;
        public ColorBlock buttonColors;
        public Graphic[] graphics;
        public Color[] graphicColors;
    }

    [Header("Vocabulary")]
    [SerializeField] private StageVocabularyProfile profile;

    [Header("Question UI")]
    [SerializeField] private GameObject reviewPanel;
    [SerializeField] private GameObject headerRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text targetWordText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private GameObject answerButtonsRoot;
    [SerializeField] private Button[] answerButtons = new Button[4];
    [SerializeField] private TMP_Text[] answerLabels = new TMP_Text[4];

    [Header("Answer Feedback Colors")]
    [SerializeField] private Color correctAnswerColor = new Color(0.18f, 0.49f, 0.20f, 1f);
    [SerializeField] private Color incorrectAnswerColor = new Color(0.66f, 0.23f, 0.23f, 1f);
    [SerializeField] private Color answerFeedbackTextColor = Color.white;

    [Header("Feedback UI")]
    [SerializeField] private CanvasGroup feedbackCanvasGroup;
    [SerializeField] private TMP_Text feedbackTitleText;
    [SerializeField] private TMP_Text feedbackExplanationText;
    [SerializeField] private Button continueButton;
    [SerializeField] private TMP_Text continueButtonLabel;

    [Header("Player-Controlled Transitions")]
    [SerializeField] private CanvasGroup reviewCanvasGroup;
    [SerializeField, Min(0f)] private float panelFadeDuration = 0.25f;
    [SerializeField, Min(0f)] private float feedbackFadeDuration = 0.18f;

    [Header("Summary UI")]
    [SerializeField] private GameObject summaryPanel;
    [SerializeField] private TMP_Text summaryTitleText;
    [SerializeField] private TMP_Text scoreValueText;
    [SerializeField] private TMP_Text needsReviewText;
    [SerializeField] private TMP_Text completionText;

    [Header("Progress Indicators")]
    [SerializeField] private Image[] progressIndicators = new Image[4];
    [SerializeField, Min(1f)] private float progressIndicatorSpacing = 72f;
    [SerializeField] private Color completedIndicatorColor = new Color(0.95f, 0.7f, 0.22f, 1f);
    [SerializeField] private Color currentIndicatorColor = new Color(0.16f, 0.82f, 0.9f, 1f);
    [SerializeField] private Color pendingIndicatorColor = new Color(1f, 1f, 1f, 0.38f);

    [Header("Gameplay References")]
    [SerializeField] private AttackController attackController;
    [SerializeField] private VocabularyPracticeSession session;

    [Header("Events")]
    [SerializeField] private UnityEvent onReviewCompleted;

    private readonly List<ReviewChoice> choices = new List<ReviewChoice>();
    private readonly List<string> missedWords = new List<string>();
    private ReviewState state = ReviewState.Hidden;
    private VocabularyWordData currentWord;
    private int currentWordIndex;
    private int firstAttemptCorrectCount;
    private bool attackWasLocked;
    private Action completionCallback;
    private Coroutine transitionCoroutine;
    private Coroutine feedbackFadeCoroutine;
    private bool transitionInProgress;
    private AnswerButtonVisualSnapshot[] answerButtonVisualSnapshots;
    private RectTransform progressIndicatorRow;
    private Vector2 progressIndicatorRowOriginalPosition;
    // Optional visual-only skin; production scenes without it keep legacy behavior.
    private StageVocabularyReviewArtSkin artSkin;
    private UnityAction[] answerClickActions;
#if UNITY_EDITOR
    // Only the isolated Editor test scene may enable this. Real review sessions
    // never suppress progress/analytics or completion event ownership.
    private bool sandboxReview;
#endif

    public bool ReviewCompleted { get; private set; }
    public StageVocabularyProfile Profile => profile;

#if UNITY_EDITOR
    /// <summary>
    /// Starts a completely non-persistent manual UI test. Never used by the
    /// production StageSelectManager or included in Android/PC builds.
    /// </summary>
    public bool BeginSandboxReview(StageVocabularyProfile selectedProfile)
    {
        if (!Application.isPlaying ||
            gameObject.scene.path !=
                "Assets/Scenes/StageSelectBook_VocabularyReviewArt_Isolated.unity" ||
            StageVocabularyReviewPendingState.HasPendingReview ||
            selectedProfile == null)
            return false;

        StopTransitionCoroutine();
        if (feedbackFadeCoroutine != null)
        {
            StopCoroutine(feedbackFadeCoroutine);
            feedbackFadeCoroutine = null;
        }

        sandboxReview = true;
        session = null;
        attackController = null;
        profile = selectedProfile;
        state = ReviewState.Hidden;
        BeginReview();
        return state == ReviewState.Question;
    }
#endif

    public bool IsConfigurationValid()
    {
        return ValidateProfile();
    }

    public bool TrySetProfile(StageVocabularyProfile selectedProfile)
    {
        if (selectedProfile == null)
        {
            Debug.LogError("[StageVocabularyReview] Cannot select a null profile.", this);
            return false;
        }

        if (state != ReviewState.Hidden || transitionInProgress)
        {
            Debug.LogError(
                "[StageVocabularyReview] Profile changes are only allowed while the review is hidden.",
                this);
            return false;
        }

        profile = selectedProfile;
        EnsureProgressIndicatorCapacity(profile.WordCount);
        return ValidateProfile();
    }

    private void Awake()
    {
        artSkin = GetComponent<StageVocabularyReviewArtSkin>();
        ResolveCanvasGroups();
        CacheAnswerButtonVisuals();

        if (reviewPanel != null)
            reviewPanel.SetActive(false);
        if (summaryPanel != null)
            summaryPanel.SetActive(false);

        EnsureReviewButtonListeners();
    }

    private void OnDestroy()
    {
        if (continueButton != null)
            continueButton.onClick.RemoveListener(OnContinuePressed);
        if (answerClickActions != null)
            for (int i = 0; i < answerButtons.Length && i < answerClickActions.Length; i++)
                if (answerButtons[i] != null && answerClickActions[i] != null)
                    answerButtons[i].onClick.RemoveListener(answerClickActions[i]);
    }

    private void EnsureReviewButtonListeners()
    {
        // Some review canvases start inactive and become active only when the
        // mandatory gate opens. Bind idempotently at BeginReview as well as
        // Awake so an Editor reload or inactive startup cannot strand buttons.
        if (answerClickActions == null || answerClickActions.Length != answerButtons.Length)
            answerClickActions = new UnityAction[answerButtons.Length];

        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (answerButtons[i] == null)
                continue;
            if (answerClickActions[i] == null)
            {
                int capturedIndex = i;
                answerClickActions[i] = () => SelectAnswer(capturedIndex);
            }
            answerButtons[i].onClick.RemoveListener(answerClickActions[i]);
            answerButtons[i].onClick.AddListener(answerClickActions[i]);
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnContinuePressed);
            continueButton.onClick.AddListener(OnContinuePressed);
        }
    }

    public void BeginReview()
    {
        BeginReview(null);
    }

    public void BeginReview(Action onCompleted)
    {
        ResolveReferences();
        EnsureReviewButtonListeners();
        completionCallback = onCompleted;

        if (!ValidateProfile())
        {
            Debug.LogError(
                "[StageVocabularyReview] The Stage Vocabulary Profile must contain sentence choices with exactly one correct answer for every word.",
                this);
            completionCallback = null;
            // An invalid or missing review must NEVER mark the mandatory
            // post-stage gate complete or unlock Stage Select.
            return;
        }

        ReviewCompleted = false;
        EnsureProgressIndicatorCapacity(profile.WordCount);
        currentWordIndex = 0;
        firstAttemptCorrectCount = 0;
        missedWords.Clear();
        attackWasLocked = attackController != null && attackController.IsAttackInputLocked;
        attackController?.SetAttackInputLocked(true);

        if (reviewPanel != null)
            reviewPanel.SetActive(true);
        if (summaryPanel != null)
            summaryPanel.SetActive(false);

        ShowQuestion();
        BeginReviewFadeIn();
        Debug.Log(
            $"[StageVocabularyReview] Mandatory {profile.WordCount}-item contextual review started.",
            this);
    }

    public void SelectAnswer(int index)
    {
        if (state != ReviewState.Question || index < 0 || index >= choices.Count)
            return;

        VocabularyReviewChoice selected = choices[index].data;
        bool correct = selected.Correct;
#if UNITY_EDITOR
        if (!sandboxReview)
#endif
            session?.RecordReviewAnswer(profile, currentWord, selected.Sentence, correct);

        if (correct)
            firstAttemptCorrectCount++;
        else if (!missedWords.Contains(currentWord.Word))
            missedWords.Add(currentWord.Word);

        state = ReviewState.Feedback;
        SetAnswersInteractable(false);
        ShowAnswerFeedback(index, correct);
        artSkin?.SetFeedbackAccent(correct);

        VocabularyReviewChoice correctChoice = FindCorrectChoice();
        if (feedbackTitleText != null)
            feedbackTitleText.text = correct ? "CORRECT" : "NOT QUITE";

        if (feedbackExplanationText != null)
        {
            if (correct)
            {
                feedbackExplanationText.text =
                    $"{selected.Explanation}\n\n"
                    + $"{currentWord.Word} means {LowercaseFirst(currentWord.Definition)}";
            }
            else
            {
                string correctSentence = correctChoice != null
                    ? correctChoice.Sentence
                    : currentWord.ContextSentence;

                feedbackExplanationText.text =
                    $"{selected.Explanation}\n\n"
                    + $"Correct use: \"{correctSentence}\"\n"
                    + $"{currentWord.Word} means {LowercaseFirst(currentWord.Definition)}";
            }
        }

        UpdateProgressIndicators(currentWordIndex, true);
        bool hasNextQuestion = currentWordIndex + 1 < profile.WordCount;
        SetContinueButton(true, hasNextQuestion ? "NEXT QUESTION" : "VIEW SUMMARY");
        BeginFeedbackFadeIn();
    }

    private void OnContinuePressed()
    {
        if (transitionInProgress)
            return;

        if (state == ReviewState.Feedback)
        {
            if (currentWordIndex + 1 < profile.WordCount)
            {
                BeginContentTransition(() =>
                {
                    currentWordIndex++;
                    ShowQuestion();
                });
            }
            else
            {
                BeginContentTransition(ShowSummary);
            }

            return;
        }

        if (state == ReviewState.Summary)
            BeginReviewCompletionTransition();
    }

    private void ShowQuestion()
    {
        currentWord = profile.GetWord(currentWordIndex);
        BuildChoices();
        state = ReviewState.Question;
        SetQuestionPresentationVisible(true);
        ResetAnswerButtonVisuals();
        artSkin?.ResetAnswerSprites(answerButtons);
        artSkin?.SetFeedbackAccent(null);
        artSkin?.SetSummaryMode(false);

        if (titleText != null)
            titleText.text = "STAGE VOCABULARY REVIEW";
        if (targetWordText != null)
            targetWordText.text = currentWord.Word.ToUpperInvariant();
        if (progressText != null)
            progressText.text = $"{currentWordIndex + 1}/{profile.WordCount}";

        if (questionText != null)
            questionText.text = currentWord.ReviewQuestion;

        if (feedbackTitleText != null)
            feedbackTitleText.text = string.Empty;
        if (feedbackExplanationText != null)
            feedbackExplanationText.text = string.Empty;
        HideFeedbackImmediately();
        if (summaryPanel != null)
            summaryPanel.SetActive(false);
        UpdateProgressIndicators(currentWordIndex, false);

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
                answerLabels[i].text = choices[i].data.Sentence;
        }

        SetContinueButton(false, "CONTINUE");
    }

    private void ShowSummary()
    {
        state = ReviewState.Summary;
        SetQuestionPresentationVisible(false);
        SetAnswersVisible(false);

        if (feedbackTitleText != null)
            feedbackTitleText.text = string.Empty;
        if (feedbackExplanationText != null)
            feedbackExplanationText.text = string.Empty;
        HideFeedbackImmediately();
        if (summaryPanel != null)
            summaryPanel.SetActive(true);

        int totalItems = Mathf.Max(1, profile.WordCount);
        float percentage = firstAttemptCorrectCount * 100f / totalItems;
        string missedSummary = missedWords.Count == 0
            ? "None"
            : string.Join(", ", missedWords);

        if (summaryTitleText != null)
            summaryTitleText.text = $"STAGE {profile.StageNumber} REVIEW COMPLETE";
        if (scoreValueText != null)
            scoreValueText.text = $"{firstAttemptCorrectCount} / {totalItems}";
        if (needsReviewText != null)
            needsReviewText.text = missedSummary;
        if (completionText != null)
            completionText.text = missedWords.Count == 0
                ? "Review completed. No words were marked for review."
                : "Review completed. Missed words have been marked for review.";

        UpdateProgressIndicators(profile.WordCount, true);
        artSkin?.SetSummaryMode(true);

        SetContinueButton(true, "CONTINUE");
    }

    private void CompleteReview()
    {
        ReviewCompleted = true;
        state = ReviewState.Hidden;
        attackController?.SetAttackInputLocked(attackWasLocked);

        if (reviewPanel != null)
            reviewPanel.SetActive(false);
        if (summaryPanel != null)
            summaryPanel.SetActive(false);

        Action callback = completionCallback;
        completionCallback = null;
        callback?.Invoke();
#if UNITY_EDITOR
        if (sandboxReview)
        {
            Debug.Log("[Vocabulary UI QA] Sandbox review finished. No progression/completion events were fired.", this);
            return;
        }
#endif
        onReviewCompleted?.Invoke();
        Debug.Log("[StageVocabularyReview] Four-item contextual review and summary completed.", this);
    }

    private void ResolveCanvasGroups()
    {
        if (reviewCanvasGroup == null && reviewPanel != null)
            reviewCanvasGroup = reviewPanel.GetComponent<CanvasGroup>();
    }

    private void BeginReviewFadeIn()
    {
        ResolveCanvasGroups();
        StopTransitionCoroutine();

        if (reviewCanvasGroup == null || panelFadeDuration <= 0f)
        {
            SetReviewCanvasState(1f, true);
            return;
        }

        SetReviewCanvasState(0f, false);
        transitionCoroutine = StartCoroutine(FadeReviewInRoutine());
    }

    private IEnumerator FadeReviewInRoutine()
    {
        transitionInProgress = true;
        yield return FadeCanvasGroup(reviewCanvasGroup, 0f, 1f, panelFadeDuration);
        SetReviewCanvasState(1f, true);
        transitionInProgress = false;
        transitionCoroutine = null;
    }

    private void BeginFeedbackFadeIn()
    {
        if (feedbackFadeCoroutine != null)
            StopCoroutine(feedbackFadeCoroutine);

        if (feedbackCanvasGroup == null || feedbackFadeDuration <= 0f)
        {
            SetFeedbackCanvasState(1f, true);
            return;
        }

        SetFeedbackCanvasState(0f, false);
        feedbackFadeCoroutine = StartCoroutine(FadeFeedbackInRoutine());
    }

    private IEnumerator FadeFeedbackInRoutine()
    {
        yield return FadeCanvasGroup(feedbackCanvasGroup, 0f, 1f, feedbackFadeDuration);
        SetFeedbackCanvasState(1f, true);
        feedbackFadeCoroutine = null;
    }

    private void HideFeedbackImmediately()
    {
        if (feedbackFadeCoroutine != null)
        {
            StopCoroutine(feedbackFadeCoroutine);
            feedbackFadeCoroutine = null;
        }

        SetFeedbackCanvasState(0f, false);
    }

    private void BeginContentTransition(Action replaceContent)
    {
        ResolveCanvasGroups();
        StopTransitionCoroutine();

        if (reviewCanvasGroup == null || panelFadeDuration <= 0f)
        {
            replaceContent?.Invoke();
            SetReviewCanvasState(1f, true);
            return;
        }

        transitionCoroutine = StartCoroutine(ContentTransitionRoutine(replaceContent));
    }

    private IEnumerator ContentTransitionRoutine(Action replaceContent)
    {
        transitionInProgress = true;
        SetReviewCanvasState(reviewCanvasGroup.alpha, false);
        yield return FadeCanvasGroup(reviewCanvasGroup, reviewCanvasGroup.alpha, 0f, panelFadeDuration);

        replaceContent?.Invoke();

        yield return FadeCanvasGroup(reviewCanvasGroup, 0f, 1f, panelFadeDuration);
        SetReviewCanvasState(1f, true);
        transitionInProgress = false;
        transitionCoroutine = null;
    }

    private void BeginReviewCompletionTransition()
    {
        ResolveCanvasGroups();
        StopTransitionCoroutine();

        if (reviewCanvasGroup == null || panelFadeDuration <= 0f)
        {
            CompleteReview();
            return;
        }

        transitionCoroutine = StartCoroutine(ReviewCompletionRoutine());
    }

    private IEnumerator ReviewCompletionRoutine()
    {
        transitionInProgress = true;
        SetReviewCanvasState(reviewCanvasGroup.alpha, false);
        yield return FadeCanvasGroup(reviewCanvasGroup, reviewCanvasGroup.alpha, 0f, panelFadeDuration);
        transitionCoroutine = null;
        transitionInProgress = false;
        CompleteReview();
    }

    private static IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float from, float to, float duration)
    {
        if (canvasGroup == null)
            yield break;

        if (duration <= 0f)
        {
            canvasGroup.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        canvasGroup.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        canvasGroup.alpha = to;
    }

    private void SetReviewCanvasState(float alpha, bool interactive)
    {
        if (reviewCanvasGroup == null)
            return;

        reviewCanvasGroup.alpha = alpha;
        reviewCanvasGroup.interactable = interactive;
        reviewCanvasGroup.blocksRaycasts = interactive;
    }

    private void SetFeedbackCanvasState(float alpha, bool interactive)
    {
        if (feedbackCanvasGroup == null)
            return;

        feedbackCanvasGroup.alpha = alpha;
        feedbackCanvasGroup.interactable = interactive;
        feedbackCanvasGroup.blocksRaycasts = interactive;
    }

    private void StopTransitionCoroutine()
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }

        transitionInProgress = false;
    }

    private void BuildChoices()
    {
        choices.Clear();
        foreach (VocabularyReviewChoice configuredChoice in currentWord.ReviewChoices)
        {
            if (configuredChoice == null || string.IsNullOrWhiteSpace(configuredChoice.Sentence))
                continue;

            choices.Add(new ReviewChoice { data = configuredChoice });
        }

        for (int i = 0; i < choices.Count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, choices.Count);
            ReviewChoice temporary = choices[i];
            choices[i] = choices[randomIndex];
            choices[randomIndex] = temporary;
        }
    }

    private VocabularyReviewChoice FindCorrectChoice()
    {
        foreach (ReviewChoice choice in choices)
        {
            if (choice.data != null && choice.data.Correct)
                return choice.data;
        }

        return null;
    }

    private bool ValidateProfile()
    {
        if (profile == null || profile.WordCount == 0)
            return false;

        for (int wordIndex = 0; wordIndex < profile.WordCount; wordIndex++)
        {
            VocabularyWordData word = profile.GetWord(wordIndex);
            if (word == null || word.ReviewChoices == null || word.ReviewChoices.Count < 2)
                return false;

            int usableChoices = 0;
            int correctChoices = 0;
            foreach (VocabularyReviewChoice choice in word.ReviewChoices)
            {
                if (choice == null || string.IsNullOrWhiteSpace(choice.Sentence))
                    continue;

                usableChoices++;
                if (choice.Correct)
                    correctChoices++;
            }

            if (usableChoices < 2 || correctChoices != 1)
                return false;
        }

        return true;
    }

    private void SetAnswersInteractable(bool interactable)
    {
        foreach (Button button in answerButtons)
        {
            if (button != null)
                button.interactable = interactable;
        }
    }

    private void CacheAnswerButtonVisuals()
    {
        answerButtonVisualSnapshots = new AnswerButtonVisualSnapshot[answerButtons.Length];

        for (int i = 0; i < answerButtons.Length; i++)
        {
            Button button = answerButtons[i];
            if (button == null)
                continue;

            Graphic[] graphics = button.GetComponentsInChildren<Graphic>(true);
            Color[] graphicColors = new Color[graphics.Length];
            for (int graphicIndex = 0; graphicIndex < graphics.Length; graphicIndex++)
                graphicColors[graphicIndex] = graphics[graphicIndex].color;

            answerButtonVisualSnapshots[i] = new AnswerButtonVisualSnapshot
            {
                button = button,
                buttonColors = button.colors,
                graphics = graphics,
                graphicColors = graphicColors
            };
        }
    }

    private void ResetAnswerButtonVisuals()
    {
        if (answerButtonVisualSnapshots == null
            || answerButtonVisualSnapshots.Length != answerButtons.Length)
        {
            CacheAnswerButtonVisuals();
        }

        foreach (AnswerButtonVisualSnapshot snapshot in answerButtonVisualSnapshots)
        {
            if (snapshot == null || snapshot.button == null)
                continue;

            snapshot.button.colors = snapshot.buttonColors;
            int graphicCount = Mathf.Min(snapshot.graphics.Length, snapshot.graphicColors.Length);
            for (int i = 0; i < graphicCount; i++)
            {
                if (snapshot.graphics[i] != null)
                    snapshot.graphics[i].color = snapshot.graphicColors[i];
            }
        }
    }

    private void ShowAnswerFeedback(int selectedIndex, bool selectedWasCorrect)
    {
        int correctIndex = -1;
        for (int i = 0; i < choices.Count; i++)
        {
            if (choices[i].data != null && choices[i].data.Correct)
            {
                correctIndex = i;
                break;
            }
        }

        if (!selectedWasCorrect)
            SetAnswerButtonFeedbackColor(selectedIndex, incorrectAnswerColor);

        SetAnswerButtonFeedbackColor(correctIndex, correctAnswerColor);
        artSkin?.ShowAnswerResult(answerButtons, selectedIndex, correctIndex);
    }

    private void SetAnswerButtonFeedbackColor(int index, Color backgroundColor)
    {
        if (index < 0 || index >= answerButtons.Length || answerButtons[index] == null)
            return;

        Button button = answerButtons[index];
        if (artSkin != null && artSkin.SetAnswerFeedbackSprite(button, backgroundColor == correctAnswerColor))
            return;
        ColorBlock colors = button.colors;
        colors.normalColor = backgroundColor;
        colors.highlightedColor = backgroundColor;
        colors.pressedColor = backgroundColor;
        colors.selectedColor = backgroundColor;
        colors.disabledColor = backgroundColor;
        colors.fadeDuration = 0f;
        button.colors = colors;

        Graphic[] graphics = button.GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphic in graphics)
        {
            Color feedbackColor = graphic is TMP_Text
                ? answerFeedbackTextColor
                : backgroundColor;
            feedbackColor.a = graphic.color.a;
            graphic.color = feedbackColor;
        }
    }

    private void SetAnswersVisible(bool visible)
    {
        if (answerButtonsRoot != null)
            answerButtonsRoot.SetActive(visible);

        foreach (Button button in answerButtons)
        {
            if (button != null)
                button.gameObject.SetActive(visible);
        }
    }

    private void SetQuestionPresentationVisible(bool visible)
    {
        if (headerRoot != null)
            headerRoot.SetActive(visible);
        if (questionText != null)
            questionText.gameObject.SetActive(visible);
        if (answerButtonsRoot != null)
            answerButtonsRoot.SetActive(visible);
    }

    private void UpdateProgressIndicators(int activeIndex, bool activeItemCompleted)
    {
        int visibleCount = profile != null
            ? Mathf.Min(profile.WordCount, progressIndicators.Length)
            : progressIndicators.Length;

        for (int i = 0; i < progressIndicators.Length; i++)
        {
            Image indicator = progressIndicators[i];
            if (indicator == null)
                continue;

            Transform indicatorRoot = indicator.transform.parent;
            if (indicatorRoot != null)
                indicatorRoot.gameObject.SetActive(i < visibleCount);
            if (i >= visibleCount)
                continue;

            bool completed = i < activeIndex || (activeItemCompleted && i == activeIndex);
            bool current = i == activeIndex && !activeItemCompleted;
            bool showFill = completed || current;
            if (artSkin != null && artSkin.SetProgressSprite(indicator, completed, current))
                continue;

            // ProgressFill is layered over the always-visible white diamond outline.
            // Disable pending fills completely so their translucent tint cannot mask
            // the outline, then explicitly re-enable the next active/completed fill.
            indicator.enabled = showFill;
            if (!showFill)
                continue;

            indicator.color = completed ? completedIndicatorColor : currentIndicatorColor;
            indicator.canvasRenderer.SetAlpha(1f);
            indicator.SetVerticesDirty();
        }
    }

    private void EnsureProgressIndicatorCapacity(int requiredCount)
    {
        requiredCount = Mathf.Max(1, requiredCount);
        List<Image> indicators = new List<Image>();
        if (progressIndicators != null)
        {
            for (int i = 0; i < progressIndicators.Length; i++)
            {
                if (progressIndicators[i] != null)
                    indicators.Add(progressIndicators[i]);
            }
        }

        if (indicators.Count == 0)
        {
            Debug.LogWarning(
                "[StageVocabularyReview] No progress-indicator template is assigned.",
                this);
            return;
        }

        Image templateFill = indicators[indicators.Count - 1];
        RectTransform templateRoot = templateFill.transform.parent as RectTransform;
        if (templateRoot == null || templateRoot.parent == null)
        {
            Debug.LogWarning(
                "[StageVocabularyReview] Progress-indicator template hierarchy is incomplete.",
                this);
            return;
        }

        while (indicators.Count < requiredCount)
        {
            int newIndex = indicators.Count;
            GameObject clonedRoot = Instantiate(templateRoot.gameObject, templateRoot.parent);
            clonedRoot.name = $"Diamond_{newIndex + 1}";

            Transform fillTransform = clonedRoot.transform.Find("ProgressFill");
            Image clonedFill = fillTransform != null
                ? fillTransform.GetComponent<Image>()
                : null;
            if (clonedFill == null)
            {
                Destroy(clonedRoot);
                Debug.LogError(
                    "[StageVocabularyReview] Cloned indicator has no ProgressFill Image.",
                    this);
                break;
            }

            indicators.Add(clonedFill);
        }

        progressIndicators = indicators.ToArray();

        // Longer reviews (Stage 3 has seven items) need breathing room before
        // the bottom-right Next Question button. Reposition the whole row, not
        // individual diamonds, preserving their authored spacing. The short
        // Stage 1/2 reviews retain exactly their original placement.
        if (progressIndicators.Length > 0 && progressIndicators[0] != null)
        {
            RectTransform row = progressIndicators[0].transform.parent.parent as RectTransform;
            if (row != null)
            {
                if (progressIndicatorRow != row)
                {
                    progressIndicatorRow = row;
                    progressIndicatorRowOriginalPosition = row.anchoredPosition;
                }

                // Seven entries shift left by 85 canvas units. At the authored
                // Stage Select Game View size this leaves ~53px between the
                // seventh diamond and the Next Question button, rather than
                // the previous ~32px overlap.
                float extraLeft = Mathf.Max(0, requiredCount - 5) * 42.5f;
                row.anchoredPosition = progressIndicatorRowOriginalPosition +
                                       Vector2.left * extraLeft;
            }
        }

        float startX = -0.5f * (requiredCount - 1) * progressIndicatorSpacing;
        for (int i = 0; i < progressIndicators.Length; i++)
        {
            Image indicator = progressIndicators[i];
            if (indicator == null || indicator.transform.parent is not RectTransform root)
                continue;

            bool visible = i < requiredCount;
            root.gameObject.SetActive(visible);
            if (!visible)
                continue;

            Vector2 anchoredPosition = root.anchoredPosition;
            anchoredPosition.x = startX + i * progressIndicatorSpacing;
            root.anchoredPosition = anchoredPosition;
        }
    }

    private void SetContinueButton(bool interactable, string label)
    {
        if (continueButton != null)
            continueButton.interactable = interactable;
        if (continueButtonLabel != null)
            continueButtonLabel.text = label;
        artSkin?.SetContinueSprite(continueButton, interactable);
    }

    private void ResolveReferences()
    {
#if UNITY_EDITOR
        if (sandboxReview)
            return;
#endif
        if (attackController == null)
            attackController = FindFirstObjectByType<AttackController>();
        if (session == null)
        {
            session = VocabularyPracticeSession.Instance != null
                ? VocabularyPracticeSession.Instance
                : FindFirstObjectByType<VocabularyPracticeSession>();
        }
    }

    private static string LowercaseFirst(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return char.ToLowerInvariant(value[0]) + value.Substring(1);
    }
}
