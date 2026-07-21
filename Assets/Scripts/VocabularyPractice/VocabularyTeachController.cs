using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class VocabularyTeachController : MonoBehaviour
{
    private enum TeachState
    {
        Hidden,
        Presenting,
        Reconstructing,
        ReadyToContinue
    }

    [Header("Vocabulary")]
    [SerializeField] private StageVocabularyProfile profile;
    [Min(0)] [SerializeField] private int wordIndex;
    [SerializeField] private bool showOnStart;
    [SerializeField] private bool requireGuidedReconstruction = true;

    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text wordText;
    [SerializeField] private TMP_Text partOfSpeechText;
    [SerializeField] private TMP_Text definitionText;
    [SerializeField] private TMP_Text contextText;
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private Button primaryButton;
    [SerializeField] private TMP_Text primaryButtonLabel;
    [SerializeField] private Button hintButton;

    [Header("Gameplay References")]
    [SerializeField] private TileManager tileManager;
    [SerializeField] private AttackController attackController;
    [SerializeField] private VocabularyPracticeSession session;
    [SerializeField] private bool pauseGameplayWhileTeaching = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onTeachCompleted;

    private TeachState state = TeachState.Hidden;
    private int hintLevel;
    private float previousTimeScale = 1f;

    public bool TeachCompleted { get; private set; }
    public VocabularyWordData CurrentWord => profile != null ? profile.GetWord(wordIndex) : null;

    private void Awake()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (primaryButton != null)
            primaryButton.onClick.AddListener(OnPrimaryPressed);

        if (hintButton != null)
            hintButton.onClick.AddListener(ShowNextHint);
    }

    private void Start()
    {
        ResolveReferences();
        if (showOnStart)
            ShowTeach();
    }

    private void Update()
    {
        if (state != TeachState.Reconstructing || tileManager == null || CurrentWord == null)
            return;

        string formedWord = tileManager.GetCurrentWord();
        if (CurrentWord.Matches(formedWord))
        {
            state = TeachState.ReadyToContinue;
            if (feedbackText != null)
                feedbackText.text = $"Correct. {CurrentWord.Word} means {LowercaseFirst(CurrentWord.Definition)}";
            if (instructionText != null)
                instructionText.text = "Guided reconstruction completed.";
            SetPrimaryButton(true, "CONTINUE");
            if (hintButton != null)
                hintButton.gameObject.SetActive(false);
            return;
        }

        if (formedWord.Length >= CurrentWord.Word.Length)
        {
            if (feedbackText != null)
                feedbackText.text = "Not yet. Review the meaning and try the shuffled letters again.";
            tileManager.ResetTileSelection();
        }
    }

    public void ShowTeach()
    {
        ResolveReferences();
        VocabularyWordData word = CurrentWord;
        if (word == null)
        {
            Debug.LogWarning("[VocabularyTeach] No vocabulary word is configured.", this);
            return;
        }

        TeachCompleted = false;
        hintLevel = 0;
        state = TeachState.Presenting;

        if (pauseGameplayWhileTeaching)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        attackController?.SetAttackInputLocked(true);
        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (wordText != null)
            wordText.text = word.Word;
        if (partOfSpeechText != null)
            partOfSpeechText.text = word.PartOfSpeech;
        if (definitionText != null)
            definitionText.text = word.Definition;
        if (contextText != null)
            contextText.text = $"Example: {word.ContextSentence}";
        if (instructionText != null)
            instructionText.text = requireGuidedReconstruction
                ? "Study the word, its meaning, and how it is used."
                : "Study the word before continuing to combat.";
        if (feedbackText != null)
            feedbackText.text = string.Empty;

        SetPrimaryButton(true, requireGuidedReconstruction ? "PRACTICE" : "CONTINUE");
        if (hintButton != null)
            hintButton.gameObject.SetActive(false);
    }

    public void ShowNextHint()
    {
        VocabularyWordData word = CurrentWord;
        if (state != TeachState.Reconstructing || word == null)
            return;

        hintLevel++;
        if (instructionText == null)
            return;

        if (hintLevel == 1)
        {
            instructionText.text = $"Hint: The word starts with '{word.Word[0]}'.";
        }
        else if (hintLevel == 2)
        {
            int revealCount = Mathf.Min(2, word.Word.Length);
            instructionText.text = $"Hint: {word.Word.Substring(0, revealCount)}{new string('_', word.Word.Length - revealCount)}";
        }
        else
        {
            instructionText.text = $"Review: {word.Word}. Rebuild it from the shuffled pool.";
            tileManager?.EnsureWordLettersAvailable(word.Word, 16);
        }
    }

    private void OnPrimaryPressed()
    {
        if (state == TeachState.Presenting && requireGuidedReconstruction)
        {
            BeginGuidedReconstruction();
            return;
        }

        if (state == TeachState.Presenting || state == TeachState.ReadyToContinue)
            CompleteTeach();
    }

    private void BeginGuidedReconstruction()
    {
        VocabularyWordData word = CurrentWord;
        if (word == null || tileManager == null)
        {
            Debug.LogWarning("[VocabularyTeach] TileManager is required for guided reconstruction.", this);
            return;
        }

        state = TeachState.Reconstructing;
        tileManager.ResetTileSelection();
        bool lettersAvailable = tileManager.EnsureWordLettersAvailable(word.Word, 16);

        if (wordText != null)
            wordText.text = BuildBlankWord(word.Word.Length);
        if (instructionText != null)
            instructionText.text = lettersAvailable
                ? "Reconstruct the taught word using the shuffled Tile Pool."
                : "Some required tiles could not be prepared. Check the Tile Pool setup.";
        if (feedbackText != null)
            feedbackText.text = string.Empty;

        SetPrimaryButton(false, "CONTINUE");
        if (hintButton != null)
            hintButton.gameObject.SetActive(true);
    }

    private void CompleteTeach()
    {
        state = TeachState.Hidden;
        TeachCompleted = true;
        tileManager?.ResetTileSelection();
        attackController?.SetAttackInputLocked(false);

        if (session != null)
            session.RecordTeachCompleted(profile, CurrentWord);

        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (pauseGameplayWhileTeaching)
            Time.timeScale = previousTimeScale;

        onTeachCompleted?.Invoke();
        Debug.Log($"[VocabularyTeach] Completed Teach for {CurrentWord?.Word}.", this);
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

    private void SetPrimaryButton(bool interactable, string label)
    {
        if (primaryButton != null)
            primaryButton.interactable = interactable;
        if (primaryButtonLabel != null)
            primaryButtonLabel.text = label;
    }

    private static string BuildBlankWord(int length)
    {
        return string.Join(" ", new string('_', Mathf.Max(1, length)).ToCharArray());
    }

    private static string LowercaseFirst(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return char.ToLowerInvariant(value[0]) + value.Substring(1);
    }
}
