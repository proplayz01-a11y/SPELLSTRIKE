using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public enum MinigameMode
{
    WordMaster,
    SpellItOut,
    WordExcavation
}

public enum MinigameSceneStartup
{
    LegacyAuto,
    ModeSelect,
    WordMaster,
    SpellItOut,
    WordExcavation
}

public class MinigamesSceneController : MonoBehaviour
{
    private const string WordMasterSourceAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const int SpellItOutDefinitionMaxLength = 140;
    private const int SpellItOutCandidateAttempts = 160;

    [Header("Scene Flow")]
    public MinigameSceneStartup startupScene = MinigameSceneStartup.LegacyAuto;
    public string mainMenuSceneName = "Main Menu Scene";
    public string modeSelectSceneName = "MinigameModeSelectScene";
    public string wordMasterSceneName = "WordMasterScene";
    public string spellItOutSceneName = "SpellItOutScene";
    public string wordExcavationSceneName = "WordExcavationScene";

    [Header("Editable UI")]
    public bool useEditableSceneUI = true;
    public bool rebuildUIOnStart = false;

    [Header("Word Master")]
    public int wordMasterWordLength = 5;
    public int wordMasterMaxTurns = 5;
    public bool wordMasterUseStartHint = true;
    public Color wordMasterCorrectColor = new Color(0.94f, 0.72f, 0.16f, 1f);
    public Color wordMasterPresentColor = new Color(0.73f, 0.72f, 0.66f, 1f);
    public Color wordMasterMissingColor = new Color(0.24f, 0.20f, 0.16f, 1f);

    [Header("Spell It Out")]
    public int spellItOutMinLength = 4;
    public int spellItOutMaxLength = 8;
    public int spellItOutTotalWords = 5;
    public float spellItOutSecondsPerWord = 30f;
    public TextAsset spellItOutDictionaryFile;
    public string spellItOutResourceName = "spell_it_out_dictionary";

    [Header("Word Excavation")]
    public int excavationSourceMinLength = 8;
    public int excavationSourceMaxLength = 9;
    public int excavationMinimumWordLength = 3;
    public float excavationRoundSeconds = 60f;

    [Header("Colors")]
    public Color backgroundColor = new Color(0.08f, 0.035f, 0.015f, 1f);
    public Color panelColor = new Color(0.78f, 0.62f, 0.34f, 0.92f);
    public Color cardColor = new Color(0.42f, 0.28f, 0.12f, 0.88f);
    public Color buttonColor = new Color(0.55f, 0.34f, 0.10f, 0.95f);
    public Color selectedButtonColor = new Color(0.82f, 0.57f, 0.15f, 1f);
    public Color textColor = new Color(0.12f, 0.07f, 0.03f, 1f);
    public Color lightTextColor = new Color(0.96f, 0.88f, 0.68f, 1f);
    public Color goodColor = new Color(0.22f, 0.55f, 0.18f, 1f);
    public Color badColor = new Color(0.62f, 0.13f, 0.08f, 1f);

    private DictionaryManager dictionary;
    private MinigameMode currentMode = MinigameMode.WordMaster;

    private RectTransform root;
    private RectTransform selectionScreen;
    private RectTransform playScreen;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI promptText;
    private TextMeshProUGUI feedbackText;
    private TextMeshProUGUI scoreText;
    private TextMeshProUGUI timerText;
    private TextMeshProUGUI foundWordsText;
    private TMP_InputField answerInput;
    private RectTransform optionsRoot;
    private TextMeshProUGUI tileInstructionText;
    private RectTransform currentWordTilesRoot;
    private RectTransform sourceWordTilesRoot;
    private RectTransform wordMasterRowsRoot;
    private RectTransform wordMasterHintPanel;
    private TextMeshProUGUI wordMasterStatsText;
    private RectTransform wordExcavationTimerFill;
    private TextMeshProUGUI wordExcavationTimerText;
    private TextMeshProUGUI wordExcavationFoundWordsText;
    private RectTransform pauseOverlay;
    private RectTransform resultOverlay;
    private TextMeshProUGUI resultTitleText;
    private TextMeshProUGUI resultScoreText;
    private TextMeshProUGUI resultWordsText;
    private TextMeshProUGUI resultContinueText;
    private Button clearButton;
    private Button newRoundButton;
    private Button submitButton;
    private Button modeSelectButton;
    private Button playBackButton;
    private Button optionsButton;
    private Button quitMiniGameButton;
    private Button returnToGameButton;
    private Button resultContinueButton;
    private Button selectionBackButton;
    private Button wordMasterButton;
    private Button spellItOutButton;
    private Button wordExcavationButton;
    private readonly List<Button> optionButtons = new List<Button>();
    private readonly List<int> selectedTileIndexes = new List<int>();
    private readonly HashSet<int> wordMasterConsumedLockedInputPositions = new HashSet<int>();
    private readonly List<PracticeEntry> spellItOutCuratedEntries = new List<PracticeEntry>();
    private readonly List<string> spellItOutSessionWords = new List<string>();
    private readonly List<bool> spellItOutSessionCorrect = new List<bool>();
    private char[] currentSourceLetters = new char[0];
    private char[] wordMasterLockedLetters = new char[0];
    private int lastSourceTileClickFrame = -1;
    private int lastSourceTileClickIndex = -1;

    private string currentWord = string.Empty;
    private string currentDefinition = string.Empty;
    private string wordMasterTargetWord = string.Empty;
    private string excavationSourceWord = string.Empty;
    private float excavationTimer;
    private int score;
    private int roundsPlayed;
    private int wordMasterTurn;
    private int wordMasterHintIndex = -1;
    private int spellItOutWordIndex;
    private float spellItOutTimer;
    private bool wordMasterSolved;
    private bool wordMasterHintPanelDismissed;
    private bool spellItOutActive;
    private bool excavationActive;
    private bool minigamePaused;
    private bool resultOverlayShowing;
    private bool spellItOutCuratedEntriesLoaded;
    private readonly HashSet<string> foundExcavationWords = new HashSet<string>();
    private readonly List<string> foundExcavationWordsInOrder = new List<string>();
    private readonly List<string> wordMasterGuesses = new List<string>();
    private readonly List<LetterClue[]> wordMasterGuessClues = new List<LetterClue[]>();
    private readonly Dictionary<char, Color> wordMasterKeyboardColors = new Dictionary<char, Color>();

    private enum LetterClue
    {
        Missing,
        Present,
        Correct
    }

    private struct PracticeEntry
    {
        public string word;
        public string definition;

        public PracticeEntry(string word, string definition)
        {
            this.word = word;
            this.definition = definition;
        }
    }

#pragma warning disable 0649
    [Serializable]
    private class SpellItOutDictionaryEntry
    {
        public string word;
        public string definition;
        public int length;
        public string rarity;
    }

    [Serializable]
    private class SpellItOutDictionaryWrapper
    {
        public SpellItOutDictionaryEntry[] entries;
    }
#pragma warning restore 0649

    private static readonly PracticeEntry[] FallbackEntries =
    {
        new PracticeEntry("SPELL", "To form a word by naming or arranging letters in order."),
        new PracticeEntry("OCEAN", "A very large body of salt water."),
        new PracticeEntry("MAGIC", "Power used to make impossible things happen in stories."),
        new PracticeEntry("WIZARD", "A person who uses magic in fantasy stories."),
        new PracticeEntry("PHANTOM", "A ghost or spirit."),
        new PracticeEntry("KINGDOM", "A land ruled by a king or queen."),
        new PracticeEntry("CORAL", "A hard sea material formed by tiny marine animals."),
        new PracticeEntry("SHADOW", "A dark shape made when light is blocked."),
        new PracticeEntry("MEMORY", "Something remembered from the past."),
        new PracticeEntry("RUNIC", "Related to ancient magical symbols.")
    };

    private static readonly PracticeEntry[] SpellItOutFallbackEntries =
    {
        new PracticeEntry("TRUTH", "The real facts about something."),
        new PracticeEntry("BRAVE", "Ready to face danger or difficulty."),
        new PracticeEntry("OCEAN", "A very large area of salt water."),
        new PracticeEntry("HONESTY", "The quality of telling the truth and being fair."),
        new PracticeEntry("COURAGE", "The ability to do something even when it is difficult or scary."),
        new PracticeEntry("MEMORY", "Something remembered from the past."),
        new PracticeEntry("BALANCE", "A steady state where things are even or stable."),
        new PracticeEntry("JUSTICE", "Fair treatment under rules or laws."),
        new PracticeEntry("DISCOVER", "To find or learn something for the first time."),
        new PracticeEntry("PATIENCE", "The ability to wait calmly without getting upset."),
        new PracticeEntry("CREATIVE", "Able to make or imagine new things."),
        new PracticeEntry("KNOWLEDGE", "Information and understanding gained through learning.")
    };

    private static readonly Dictionary<string, string> SpellItOutDefinitionOverrides = new Dictionary<string, string>
    {
        { "HONESTY", "The quality of telling the truth and being fair." },
        { "COURAGE", "The ability to do something even when it is difficult or scary." },
        { "JUSTICE", "Fair treatment under rules or laws." },
        { "PATIENCE", "The ability to wait calmly without getting upset." }
    };

    private static readonly string[] SpellItOutForbiddenDefinitionTerms =
    {
        "archaic",
        "botanic",
        "botanical",
        "botany",
        "called also",
        "common name",
        "cruciferous",
        "family",
        "flower",
        "genus",
        "herb",
        "latin",
        "obsolete",
        "obsolete spelling",
        "partitions",
        "perennial",
        "plant",
        "pods",
        "scientific",
        "species",
        "taxonomic",
        "taxonomy"
    };

    private static readonly string[] ExcavationFallbackSourceWords =
    {
        "TREASURE",
        "MOUNTAIN",
        "SPELLING",
        "NOTEBOOK",
        "DEPARTING",
        "PROTECTOR",
        "ADVENTURE",
        "STARGAZE"
    };

    private void Start()
    {
        dictionary = DictionaryManager.Instance;
        if (dictionary == null)
            dictionary = FindFirstObjectByType<DictionaryManager>();

        PrepareUI();
        ShowInitialScreen();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying || !useEditableSceneUI)
            return;

        UnityEditor.EditorApplication.delayCall -= RefreshEditableOverlayUIInEditor;
        UnityEditor.EditorApplication.delayCall += RefreshEditableOverlayUIInEditor;
    }

    private void RefreshEditableOverlayUIInEditor()
    {
        if (this == null || Application.isPlaying || !useEditableSceneUI)
            return;

        CacheExistingUIReferences();
        if (playScreen == null)
            return;

        EnsureMinigameOverlayUI();
        ApplyPlayableFooterButtons();
        HideMinigameOverlays();

        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
#endif

    [ContextMenu("Rebuild Editable Minigames UI")]
    public void RebuildEditableUI()
    {
        if (dictionary == null)
            dictionary = FindFirstObjectByType<DictionaryManager>();

        ClearGeneratedUI();
        BuildUI();
        CacheExistingUIReferences();
        EnsureMinigameOverlayUI();
        WireButtonEvents();

        if (Application.isPlaying)
        {
            ShowModeSelection();
        }
        else
        {
            switch (startupScene)
            {
                case MinigameSceneStartup.WordExcavation:
                    ShowWordExcavationEditPreview();
                    break;
                case MinigameSceneStartup.WordMaster:
                default:
                    ShowWordMasterEditPreview();
                    break;
            }
        }
    }

    private void ShowInitialScreen()
    {
        switch (startupScene)
        {
            case MinigameSceneStartup.ModeSelect:
                ShowModeSelection();
                ApplySceneIsolation();
                return;
            case MinigameSceneStartup.WordMaster:
                SelectMode(MinigameMode.WordMaster);
                ApplySceneIsolation();
                return;
            case MinigameSceneStartup.SpellItOut:
                SelectMode(MinigameMode.SpellItOut);
                ApplySceneIsolation();
                return;
            case MinigameSceneStartup.WordExcavation:
                SelectMode(MinigameMode.WordExcavation);
                ApplySceneIsolation();
                return;
        }

        bool startInWordMaster = useEditableSceneUI
            && playScreen != null
            && playScreen.gameObject.activeSelf
            && (selectionScreen == null || !selectionScreen.gameObject.activeSelf);

        if (startInWordMaster)
        {
            Debug.Log("[WordMaster] Play screen active at start. Starting Word Master directly.");
            SelectMode(MinigameMode.WordMaster);
            return;
        }

        Debug.Log("[WordMaster] Showing minigame mode selection.");
        ShowModeSelection();
    }

    public void ShowWordMasterEditPreview()
    {
        if (root == null)
            CacheExistingUIReferences();

        if (root == null)
        {
            BuildUI();
            CacheExistingUIReferences();
            EnsureMinigameOverlayUI();
            WireButtonEvents();
        }

        currentMode = MinigameMode.WordMaster;
        EnsureMinigameOverlayUI();
        HideMinigameOverlays();
        score = 0;
        roundsPlayed = 0;
        excavationActive = false;
        wordMasterTargetWord = string.Empty;
        wordMasterTurn = 0;
        wordMasterHintIndex = -1;
        wordMasterSolved = false;
        wordMasterHintPanelDismissed = false;
        wordMasterLockedLetters = new char[Mathf.Max(0, wordMasterWordLength)];
        wordMasterGuesses.Clear();
        wordMasterGuessClues.Clear();
        wordMasterKeyboardColors.Clear();
        selectedTileIndexes.Clear();

        if (selectionScreen != null)
            selectionScreen.gameObject.SetActive(false);

        if (playScreen != null)
            playScreen.gameObject.SetActive(true);

        ApplyWordMasterPlayLayout();
        SetTitle("WORD MASTER");

        if (answerInput != null)
            answerInput.gameObject.SetActive(false);

        if (foundWordsText != null)
            foundWordsText.gameObject.SetActive(false);

        if (timerText != null)
            timerText.gameObject.SetActive(false);

        if (scoreText != null)
            scoreText.gameObject.SetActive(false);

        if (optionsRoot != null)
            optionsRoot.gameObject.SetActive(true);

        SetTilePickerVisible(true);

        if (currentWordTilesRoot != null)
            currentWordTilesRoot.gameObject.SetActive(false);

        if (tileInstructionText != null)
            tileInstructionText.gameObject.SetActive(false);

        if (promptText != null)
            promptText.text = $"Guess the hidden {wordMasterWordLength}-letter word. Gold = right spot. Silver = right letter.";

        BuildWordMasterBoard();
        ConfigureTilePicker(WordMasterSourceAlphabet);
    }

    public void ShowWordExcavationEditPreview()
    {
        if (root == null)
            CacheExistingUIReferences();

        if (root == null)
        {
            BuildUI();
            CacheExistingUIReferences();
            EnsureMinigameOverlayUI();
            WireButtonEvents();
        }

        currentMode = MinigameMode.WordExcavation;
        EnsureMinigameOverlayUI();
        HideMinigameOverlays();
        score = 0;
        roundsPlayed = 0;
        excavationActive = false;
        excavationSourceWord = string.Empty;
        excavationTimer = excavationRoundSeconds;
        foundExcavationWords.Clear();
        foundExcavationWordsInOrder.Clear();
        selectedTileIndexes.Clear();

        if (selectionScreen != null)
            selectionScreen.gameObject.SetActive(false);

        if (playScreen != null)
            playScreen.gameObject.SetActive(true);

        ApplyWordExcavationPlayLayout();
        SetTitle("WORD EXCAVATION");
        SetObjectActive(answerInput, false);
        SetObjectActive(foundWordsText, false);
        SetObjectActive(timerText, false);
        SetObjectActive(scoreText, false);
        SetObjectActive(optionsRoot, true);
        SetTilePickerVisible(true);

        if (promptText != null)
            promptText.text = "Find smaller valid words before time runs out.";

        if (tileInstructionText != null)
            tileInstructionText.text = "Click source tiles to build a word, then submit.";

        BuildWordExcavationBoard();
        ConfigureTilePicker(string.Empty);
        UpdateTimerText();
        UpdateFoundWordsText();
    }

    private void PrepareUI()
    {
        if (rebuildUIOnStart)
        {
            RebuildEditableUI();
            return;
        }

        if (useEditableSceneUI)
            CacheExistingUIReferences();

        if (root == null)
        {
            ClearGeneratedUI();
            BuildUI();
            CacheExistingUIReferences();
        }

        EnsureMinigameOverlayUI();
        WireButtonEvents();
    }

    private void ClearGeneratedUI()
    {
        RectTransform existingRoot = FindRect(transform, "MinigamesUI");
        if (existingRoot != null)
            DestroySafe(existingRoot.gameObject);

        root = null;
        selectionScreen = null;
        playScreen = null;
        titleText = null;
        promptText = null;
        feedbackText = null;
        scoreText = null;
        timerText = null;
        foundWordsText = null;
        answerInput = null;
        optionsRoot = null;
        tileInstructionText = null;
        currentWordTilesRoot = null;
        sourceWordTilesRoot = null;
        wordMasterRowsRoot = null;
        wordMasterHintPanel = null;
        wordMasterStatsText = null;
        wordExcavationTimerFill = null;
        wordExcavationTimerText = null;
        wordExcavationFoundWordsText = null;
        pauseOverlay = null;
        resultOverlay = null;
        resultTitleText = null;
        resultScoreText = null;
        resultWordsText = null;
        resultContinueText = null;
        optionsButton = null;
        quitMiniGameButton = null;
        returnToGameButton = null;
        resultContinueButton = null;
    }

    private void CacheExistingUIReferences()
    {
        root = FindRect(transform, "MinigamesUI");
        if (root == null)
            return;

        selectionScreen = FindRect(root, "MinigameSelectionScreen");
        playScreen = FindRect(root, "MinigamePlayScreen");

        titleText = FindText(playScreen, "Title");
        promptText = FindText(playScreen, "Prompt");
        feedbackText = FindText(playScreen, "Feedback");
        scoreText = FindText(playScreen, "Score");
        timerText = FindText(playScreen, "Timer");
        foundWordsText = FindText(playScreen, "FoundWords");
        answerInput = FindComponent<TMP_InputField>(playScreen, "AnswerInput");
        optionsRoot = FindRect(playScreen, "Options");
        tileInstructionText = FindText(playScreen, "TileInstruction");
        currentWordTilesRoot = FindRect(playScreen, "CurrentWordTiles");
        sourceWordTilesRoot = FindRect(playScreen, "SourceWordTiles");
        wordExcavationTimerFill = FindRect(optionsRoot, "ExcavationTimeFill");
        wordExcavationTimerText = FindText(optionsRoot, "ExcavationTimeText");
        wordExcavationFoundWordsText = FindText(optionsRoot, "ExcavationFoundWordsText");

        pauseOverlay = FindRect(playScreen, "PauseOverlay");
        resultOverlay = FindRect(playScreen, "ResultOverlay");
        resultTitleText = FindText(resultOverlay, "ResultTitleText");
        resultScoreText = FindText(resultOverlay, "ResultScoreText");
        resultWordsText = FindText(resultOverlay, "ResultWordsText");
        resultContinueText = FindText(resultOverlay, "ResultContinueText");
        optionsButton = FindButton(pauseOverlay, "OptionsButton");
        quitMiniGameButton = FindButton(pauseOverlay, "QuitMiniGameButton");
        returnToGameButton = FindButton(pauseOverlay, "ReturnToGameButton");
        resultContinueButton = FindButton(resultOverlay, "ResultContinueButton");
        if (resultContinueButton == null && resultOverlay != null)
            resultContinueButton = resultOverlay.GetComponent<Button>();

        wordMasterButton = FindButton(selectionScreen, "WordMasterButton");
        spellItOutButton = FindButton(selectionScreen, "SpellItOutButton");
        wordExcavationButton = FindButton(selectionScreen, "WordExcavationButton");
        selectionBackButton = FindButton(selectionScreen, "BackButton");

        newRoundButton = FindButton(playScreen, "NewRoundButton");
        clearButton = FindButton(playScreen, "ClearButton");
        submitButton = FindButton(playScreen, "SubmitButton");
        modeSelectButton = FindButton(playScreen, "ModeSelectButton");
        playBackButton = FindButton(playScreen, "BackButton");
    }

    private void WireButtonEvents()
    {
        WireButton(wordMasterButton, OpenWordMasterScene);
        WireButton(spellItOutButton, OpenSpellItOutScene);
        WireButton(wordExcavationButton, OpenWordExcavationScene);
        WireButton(selectionBackButton, ReturnToMainMenu);

        WireButton(newRoundButton, StartNewRound);
        WireButton(clearButton, ClearTileSelection);
        WireButton(submitButton, SubmitTypedAnswer);
        WireButton(modeSelectButton, ReturnToModeSelect);
        WireButton(playBackButton, OpenPauseMenu);
        WireButton(optionsButton, ShowOptionsPlaceholder);
        WireButton(quitMiniGameButton, ReturnToModeSelect);
        WireButton(returnToGameButton, ClosePauseMenu);
        WireButton(resultContinueButton, ReturnToModeSelect);
    }

    private void WireButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private void EnsureMinigameOverlayUI()
    {
        if (playScreen == null)
            return;

        bool createdOverlay = false;

        if (pauseOverlay == null)
        {
            CreatePauseOverlay(playScreen);
            createdOverlay = true;
        }

        if (resultOverlay == null)
        {
            CreateResultOverlay(playScreen);
            createdOverlay = true;
        }

        if (createdOverlay)
            CacheExistingUIReferences();

        if (pauseOverlay != null)
            pauseOverlay.SetAsLastSibling();

        if (resultOverlay != null)
            resultOverlay.SetAsLastSibling();
    }

    private void HideMinigameOverlays()
    {
        minigamePaused = false;
        resultOverlayShowing = false;

        if (pauseOverlay != null)
            pauseOverlay.gameObject.SetActive(false);

        if (resultOverlay != null)
            resultOverlay.gameObject.SetActive(false);
    }

    public void OpenPauseMenu()
    {
        if (resultOverlayShowing)
            return;

        EnsureMinigameOverlayUI();

        if (pauseOverlay == null)
            return;

        minigamePaused = true;
        pauseOverlay.gameObject.SetActive(true);
        pauseOverlay.SetAsLastSibling();
    }

    public void ClosePauseMenu()
    {
        minigamePaused = false;

        if (pauseOverlay != null)
            pauseOverlay.gameObject.SetActive(false);
    }

    private void ShowOptionsPlaceholder()
    {
        SetFeedback("Options coming soon.", lightTextColor);
    }

    private void ShowMinigameResultOverlay(string title)
    {
        EnsureMinigameOverlayUI();

        minigamePaused = true;
        resultOverlayShowing = true;

        if (pauseOverlay != null)
            pauseOverlay.gameObject.SetActive(false);

        if (resultTitleText != null)
        {
            resultTitleText.text = title;
            ApplyResultTitleTextStyle();
        }

        if (resultScoreText != null)
        {
            if (currentMode == MinigameMode.WordMaster)
                resultScoreText.text = "Secret word";
            else if (currentMode == MinigameMode.SpellItOut)
                resultScoreText.text = $"Score: {score}/{Mathf.Max(1, spellItOutTotalWords)}";
            else
                resultScoreText.text = $"Score: {score}";
        }

        if (resultWordsText != null)
        {
            if (currentMode == MinigameMode.WordMaster)
                resultWordsText.text = string.IsNullOrWhiteSpace(wordMasterTargetWord) ? "Unknown" : wordMasterTargetWord;
            else if (currentMode == MinigameMode.SpellItOut)
                resultWordsText.text = BuildSpellItOutResultWordsText();
            else if (currentMode == MinigameMode.WordExcavation)
                resultWordsText.text = $"Words: {foundExcavationWordsInOrder.Count}";
            else
                resultWordsText.text = $"Rounds: {roundsPlayed}";

            ApplyResultWordsTextStyle();
        }

        if (resultContinueText != null)
            resultContinueText.text = "Click anywhere to continue";

        if (resultOverlay != null)
        {
            resultOverlay.gameObject.SetActive(true);
            resultOverlay.SetAsLastSibling();
        }
    }

    private void Update()
    {
        if (minigamePaused || resultOverlayShowing)
            return;

        if (excavationActive)
        {
            excavationTimer -= Time.deltaTime;
            if (excavationTimer <= 0f)
            {
                excavationTimer = 0f;
                excavationActive = false;
                UpdateTimerText();
                ShowMinigameResultOverlay("Time Up");
                return;
            }

            UpdateTimerText();
        }

        if (spellItOutActive)
        {
            spellItOutTimer -= Time.deltaTime;
            if (spellItOutTimer <= 0f)
            {
                spellItOutTimer = 0f;
                UpdateTimerText();
                HandleSpellItOutWordTimeout();
                return;
            }

            UpdateTimerText();
        }
    }

    public void SelectMode(MinigameMode mode)
    {
        currentMode = mode;
        score = 0;
        roundsPlayed = 0;
        excavationActive = false;
        spellItOutActive = false;
        HideMinigameOverlays();
        foundExcavationWords.Clear();
        foundExcavationWordsInOrder.Clear();

        if (selectionScreen != null)
            selectionScreen.gameObject.SetActive(false);

        if (playScreen != null)
            playScreen.gameObject.SetActive(true);

        StartNewRound();
    }

    public void ShowModeSelection()
    {
        excavationActive = false;
        spellItOutActive = false;
        HideMinigameOverlays();
        ClearOptionButtons();

        if (selectionScreen != null)
            selectionScreen.gameObject.SetActive(true);

        if (playScreen != null)
            playScreen.gameObject.SetActive(false);
    }

    public void OpenWordMasterScene()
    {
        OpenConfiguredMinigameScene(wordMasterSceneName, MinigameMode.WordMaster);
    }

    public void OpenSpellItOutScene()
    {
        OpenConfiguredMinigameScene(spellItOutSceneName, MinigameMode.SpellItOut);
    }

    public void OpenWordExcavationScene()
    {
        OpenConfiguredMinigameScene(wordExcavationSceneName, MinigameMode.WordExcavation);
    }

    public void ReturnToModeSelect()
    {
        if (!string.IsNullOrWhiteSpace(modeSelectSceneName))
        {
            SceneManager.LoadScene(modeSelectSceneName);
            return;
        }

        ShowModeSelection();
    }

    private void OpenConfiguredMinigameScene(string sceneName, MinigameMode fallbackMode)
    {
        if (!string.IsNullOrWhiteSpace(sceneName))
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        SelectMode(fallbackMode);
    }

    public void ApplySceneIsolation()
    {
        if (startupScene == MinigameSceneStartup.ModeSelect)
        {
            if (playScreen != null)
            {
                DestroySafe(playScreen.gameObject);
                playScreen = null;
            }

            return;
        }

        if (startupScene == MinigameSceneStartup.WordMaster
            || startupScene == MinigameSceneStartup.SpellItOut
            || startupScene == MinigameSceneStartup.WordExcavation)
        {
            if (selectionScreen != null)
            {
                DestroySafe(selectionScreen.gameObject);
                selectionScreen = null;
            }
        }
    }

    public void StartNewRound()
    {
        HideMinigameOverlays();
        ClearOptionButtons();
        SetFeedback(string.Empty, lightTextColor);

        switch (currentMode)
        {
            case MinigameMode.WordMaster:
                StartWordMasterRound();
                break;
            case MinigameMode.SpellItOut:
                StartSpellItOutRound();
                break;
            case MinigameMode.WordExcavation:
                StartExcavationRound();
                break;
        }

        RefreshScoreText();
    }

    public void SubmitTypedAnswer()
    {
        if (currentMode == MinigameMode.WordMaster)
        {
            string tileAnswer = GetWordMasterCurrentGuess();
            if (string.IsNullOrWhiteSpace(tileAnswer))
                return;

            ResolveWordMasterGuess(tileAnswer);
            return;
        }

        if (currentMode == MinigameMode.SpellItOut)
        {
            string tileAnswer = GetSelectedTileWord();
            if (string.IsNullOrWhiteSpace(tileAnswer))
                return;

            ResolveSpellItOut(tileAnswer);
            return;
        }

        if (currentMode == MinigameMode.WordExcavation)
        {
            string tileAnswer = GetSelectedTileWord();
            if (string.IsNullOrWhiteSpace(tileAnswer))
                return;

            ResolveExcavationAnswer(tileAnswer);
            return;
        }

        if (answerInput == null)
            return;

        string answer = answerInput.text.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(answer))
            return;
    }

    public void ClearTileSelection()
    {
        selectedTileIndexes.Clear();
        if (currentMode == MinigameMode.WordMaster)
            wordMasterConsumedLockedInputPositions.Clear();

        RefreshTilePickerVisuals();
    }

    public void SelectSourceTile(int tileIndex)
    {
        if (Time.frameCount == lastSourceTileClickFrame && tileIndex == lastSourceTileClickIndex)
            return;

        lastSourceTileClickFrame = Time.frameCount;
        lastSourceTileClickIndex = tileIndex;

        if (currentMode == MinigameMode.WordMaster)
        {
            Debug.Log($"[WordMaster] Source tile clicked. Index: {tileIndex}, Source letters: {currentSourceLetters.Length}, Selected: {selectedTileIndexes.Count}.");

            if (currentSourceLetters.Length == 0)
            {
                Debug.LogWarning("[WordMaster] Source letters were empty during click. Reinitializing Word Master source letters.");
                EnsureWordMasterRoundState();
                ConfigureTilePicker(WordMasterSourceAlphabet);
            }
        }

        ToggleSourceTile(tileIndex);
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void StartWordMasterRound()
    {
        ApplyWordMasterPlayLayout();
        SetTitle("WORD MASTER");

        if (answerInput != null)
            answerInput.gameObject.SetActive(false);

        if (foundWordsText != null)
            foundWordsText.gameObject.SetActive(false);

        if (timerText != null)
            timerText.gameObject.SetActive(false);

        if (scoreText != null)
            scoreText.gameObject.SetActive(false);

        if (optionsRoot != null)
            optionsRoot.gameObject.SetActive(true);

        SetTilePickerVisible(true);

        if (currentWordTilesRoot != null)
            currentWordTilesRoot.gameObject.SetActive(false);

        if (tileInstructionText != null)
            tileInstructionText.gameObject.SetActive(false);

        wordMasterTargetWord = GetRandomWordMasterTarget();
        wordMasterTurn = 0;
        wordMasterHintIndex = wordMasterUseStartHint ? 0 : -1;
        wordMasterSolved = false;
        wordMasterHintPanelDismissed = false;
        ResetWordMasterLockedLetters();
        wordMasterGuesses.Clear();
        wordMasterGuessClues.Clear();
        wordMasterKeyboardColors.Clear();

        if (promptText != null)
            promptText.text = $"Guess the hidden {wordMasterWordLength}-letter word. Gold = right spot. Silver = right letter.";

        BuildWordMasterBoard();
        ConfigureTilePicker(WordMasterSourceAlphabet);

        Debug.Log($"[WordMaster] Round started. Target length: {wordMasterTargetWord.Length}, Source letters: {currentSourceLetters.Length}, Rows root: {(wordMasterRowsRoot != null ? wordMasterRowsRoot.name : "null")}.");
    }

    private void EnsureWordMasterRoundState()
    {
        if (currentMode != MinigameMode.WordMaster)
            return;

        bool missingTarget = string.IsNullOrWhiteSpace(wordMasterTargetWord)
            || wordMasterTargetWord.Length != wordMasterWordLength;

        if (!missingTarget)
            return;

        wordMasterTargetWord = GetRandomWordMasterTarget();
        wordMasterTurn = 0;
        wordMasterHintIndex = wordMasterUseStartHint ? 0 : -1;
        wordMasterSolved = false;
        wordMasterHintPanelDismissed = false;
        ResetWordMasterLockedLetters();
        wordMasterGuesses.Clear();
        wordMasterGuessClues.Clear();
        wordMasterKeyboardColors.Clear();

        if (promptText != null)
            promptText.text = $"Guess the hidden {wordMasterWordLength}-letter word. Gold = right spot. Silver = right letter.";

        BuildWordMasterBoard();
        Debug.Log("[WordMaster] Round state was missing. Created a new Word Master target.");
    }

    private void ResolveWordMasterGuess(string guess)
    {
        if (wordMasterSolved)
        {
            SetFeedback("Already solved. Start a new round.", goodColor);
            return;
        }

        if (wordMasterTurn >= wordMasterMaxTurns)
        {
            SetFeedback("Game over. Try again.", badColor);
            if (promptText != null)
                promptText.text = "Game over. Try again.";

            ShowMinigameResultOverlay("Game Over");
            return;
        }

        if (guess.Length != wordMasterWordLength)
        {
            SetFeedback($"Build a {wordMasterWordLength}-letter guess first.", badColor);
            return;
        }

        if (guess.Contains(" "))
        {
            SetFeedback("Fill every letter slot first.", badColor);
            return;
        }

        bool validGuess = dictionary != null ? dictionary.IsValidWord(guess) : IsFallbackValidWord(guess);
        if (!validGuess)
        {
            SetFeedback("Not in the dictionary.", badColor);
            return;
        }

        LetterClue[] clues = EvaluateWordMasterGuess(guess, wordMasterTargetWord);
        wordMasterGuesses.Add(guess);
        wordMasterGuessClues.Add(clues);
        LockWordMasterCorrectLetters(guess, clues);
        UpdateWordMasterKeyboard(guess, clues);

        wordMasterTurn++;
        roundsPlayed = wordMasterTurn;

        if (guess == wordMasterTargetWord || IsWordMasterFullyLocked())
        {
            wordMasterSolved = true;
            score++;
            SetFeedback("Solved. Clean guess.", goodColor);
        }
        else if (wordMasterTurn >= wordMasterMaxTurns)
        {
            SetFeedback("Game over. Try again.", badColor);
            if (promptText != null)
                promptText.text = "Game over. Try again.";

            ShowMinigameResultOverlay("Game Over");
        }
        else
        {
            SetFeedback(BuildWordMasterGuessSummary(guess, clues), lightTextColor);
        }

        ClearTileSelection();
        RefreshScoreText();
    }

    private void StartSpellItOutRound()
    {
        ApplySpellItOutPlayLayout();
        SetTitle("SPELL IT OUT");
        SetObjectActive(answerInput, false);
        SetObjectActive(foundWordsText, false);
        SetObjectActive(timerText, true);
        SetObjectActive(scoreText, true);
        SetObjectActive(optionsRoot, false);
        SetTilePickerVisible(true);

        score = 0;
        roundsPlayed = 0;
        spellItOutWordIndex = 0;
        spellItOutActive = true;
        spellItOutSessionWords.Clear();
        spellItOutSessionCorrect.Clear();

        StartNextSpellItOutWord();
        RefreshScoreText();
    }

    private void StartNextSpellItOutWord()
    {
        if (spellItOutWordIndex >= Mathf.Max(1, spellItOutTotalWords))
        {
            FinishSpellItOutSession("Complete");
            return;
        }

        int minLength = GetSpellItOutStageMinLength(spellItOutWordIndex);
        int maxLength = GetSpellItOutStageMaxLength(spellItOutWordIndex);
        PracticeEntry entry = GetRandomSpellItOutEntry(minLength, maxLength);
        currentWord = entry.word;
        currentDefinition = entry.definition;
        spellItOutTimer = Mathf.Max(1f, spellItOutSecondsPerWord);
        SetFeedback(string.Empty, lightTextColor);

        if (promptText != null)
            promptText.text = $"Spell the word that matches this definition:\n<size=30>{currentDefinition}</size>\n\nHint: {BuildLetterHint(currentWord)}";

        if (tileInstructionText != null)
            tileInstructionText.text = "Click the jumbled tiles in the correct order.";

        ConfigureTilePicker(GetShuffledLetters(currentWord));
        UpdateTimerText();
        RefreshScoreText();
    }

    private void ResolveSpellItOut(string answer)
    {
        if (!spellItOutActive)
            return;

        roundsPlayed++;

        bool correct = answer == currentWord;
        RecordSpellItOutWordResult(correct);

        if (correct)
        {
            score++;
            SetFeedback("Correct.", goodColor);
        }
        else
        {
            SetFeedback($"Answer: {currentWord}", badColor);
        }

        ClearTileSelection();
        RefreshScoreText();
        AdvanceSpellItOutSession();
    }

    private void HandleSpellItOutWordTimeout()
    {
        if (!spellItOutActive)
            return;

        roundsPlayed++;
        RecordSpellItOutWordResult(false);
        SetFeedback($"Time. Answer: {currentWord}", badColor);
        ClearTileSelection();
        RefreshScoreText();
        AdvanceSpellItOutSession();
    }

    private void RecordSpellItOutWordResult(bool correct)
    {
        string word = string.IsNullOrWhiteSpace(currentWord) ? "UNKNOWN" : currentWord;
        spellItOutSessionWords.Add(word);
        spellItOutSessionCorrect.Add(correct);
    }

    private void AdvanceSpellItOutSession()
    {
        spellItOutWordIndex++;

        if (spellItOutWordIndex >= Mathf.Max(1, spellItOutTotalWords))
        {
            FinishSpellItOutSession("Complete");
            return;
        }

        StartNextSpellItOutWord();
    }

    private void FinishSpellItOutSession(string title)
    {
        spellItOutActive = false;
        ClearTileSelection();
        UpdateTimerText();
        RefreshScoreText();
        ShowMinigameResultOverlay(title);
    }

    private string BuildSpellItOutResultWordsText()
    {
        if (spellItOutSessionWords.Count == 0)
            return $"Words: {roundsPlayed}/{Mathf.Max(1, spellItOutTotalWords)}";

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Secret words:");

        for (int i = 0; i < spellItOutSessionWords.Count; i++)
        {
            bool correct = i < spellItOutSessionCorrect.Count && spellItOutSessionCorrect[i];
            builder.Append(i + 1);
            builder.Append(". ");
            builder.Append(spellItOutSessionWords[i]);
            builder.Append(correct ? " - correct" : " - missed");

            if (i < spellItOutSessionWords.Count - 1)
                builder.AppendLine();
        }

        return builder.ToString();
    }

    private void ApplyResultWordsTextStyle()
    {
        if (resultWordsText == null)
            return;

        resultWordsText.textWrappingMode = TextWrappingModes.Normal;

        if (currentMode == MinigameMode.SpellItOut)
        {
            resultWordsText.fontSize = 18f;
            resultWordsText.lineSpacing = -8f;
            resultWordsText.overflowMode = TextOverflowModes.Overflow;
            resultWordsText.alignment = TextAlignmentOptions.Center;
            SetPreferredHeight(resultWordsText, 190f);

            RectTransform panel = resultWordsText.transform.parent as RectTransform;
            if (panel != null)
            {
                panel.sizeDelta = new Vector2(Mathf.Max(panel.sizeDelta.x, 620f), Mathf.Max(panel.sizeDelta.y, 470f));

                VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
                if (layout != null)
                {
                    layout.padding = new RectOffset(54, 54, 36, 30);
                    layout.spacing = 8f;
                    layout.childControlWidth = true;
                    layout.childControlHeight = true;
                    layout.childForceExpandWidth = true;
                    layout.childForceExpandHeight = false;
                }
            }
        }
        else
        {
            resultWordsText.fontSize = 30f;
            resultWordsText.lineSpacing = 0f;
            resultWordsText.overflowMode = TextOverflowModes.Ellipsis;
            resultWordsText.alignment = TextAlignmentOptions.Center;
            SetPreferredHeight(resultWordsText, 44f);
        }
    }

    private void ApplyResultTitleTextStyle()
    {
        if (resultTitleText == null)
            return;

        resultTitleText.alignment = TextAlignmentOptions.Center;
        resultTitleText.textWrappingMode = TextWrappingModes.NoWrap;
        resultTitleText.overflowMode = TextOverflowModes.Ellipsis;
        resultTitleText.enableAutoSizing = true;
        resultTitleText.fontSizeMin = 26f;
        resultTitleText.fontSizeMax = currentMode == MinigameMode.SpellItOut ? 42f : 48f;
        SetPreferredHeight(resultTitleText, 66f);
    }

    private int GetSpellItOutStageMinLength(int stageIndex)
    {
        switch (stageIndex)
        {
            case 0:
                return 5;
            case 1:
                return 6;
            case 2:
                return 7;
            case 3:
                return 8;
            default:
                return 9;
        }
    }

    private int GetSpellItOutStageMaxLength(int stageIndex)
    {
        switch (stageIndex)
        {
            case 0:
                return 6;
            case 1:
                return 7;
            case 2:
                return 8;
            case 3:
                return 9;
            default:
                return 10;
        }
    }

    private void StartExcavationRound()
    {
        ApplyWordExcavationPlayLayout();
        SetTitle("WORD EXCAVATION");
        SetObjectActive(answerInput, false);
        SetObjectActive(foundWordsText, false);
        SetObjectActive(timerText, false);
        SetObjectActive(scoreText, false);
        SetObjectActive(optionsRoot, true);
        SetTilePickerVisible(true);

        excavationSourceWord = GetRandomWordExcavationSourceWord();
        excavationTimer = excavationRoundSeconds;
        excavationActive = true;
        foundExcavationWords.Clear();
        foundExcavationWordsInOrder.Clear();

        if (promptText != null)
            promptText.text = "Find smaller valid words before time runs out.";

        if (tileInstructionText != null)
            tileInstructionText.text = "Click source tiles to build a word, then submit.";

        BuildWordExcavationBoard();
        ConfigureTilePicker(excavationSourceWord);
        UpdateTimerText();
        UpdateFoundWordsText();
    }

    private void ResolveExcavationAnswer(string answer)
    {
        if (!excavationActive)
        {
            SetFeedback("Start a new round first.", badColor);
            return;
        }

        if (answer.Length < excavationMinimumWordLength)
        {
            SetFeedback($"Use at least {excavationMinimumWordLength} letters.", badColor);
            return;
        }

        if (foundExcavationWords.Contains(answer))
        {
            SetFeedback("Already found.", badColor);
            return;
        }

        if (!CanBuildWordFromSource(answer, excavationSourceWord))
        {
            SetFeedback("Those letters are not all available.", badColor);
            return;
        }

        bool valid = dictionary != null ? dictionary.IsValidWord(answer) : IsFallbackValidWord(answer);
        if (!valid)
        {
            SetFeedback("Not in the dictionary.", badColor);
            return;
        }

        foundExcavationWords.Add(answer);
        foundExcavationWordsInOrder.Add(answer);
        score += Mathf.Max(1, answer.Length - 2);
        ApplyWordExcavationTimeBonus(answer);
        roundsPlayed = foundExcavationWords.Count;
        SetFeedback($"+{Mathf.Max(1, answer.Length - 2)} points", goodColor);
        ClearTileSelection();
        RefreshScoreText();
        UpdateFoundWordsText();
        UpdateWordExcavationPrizeProgress();
    }

    private string GetRandomWordMasterTarget()
    {
        PracticeEntry entry = GetRandomDefinedEntry(wordMasterWordLength, wordMasterWordLength);
        if (!string.IsNullOrWhiteSpace(entry.word) && entry.word.Length == wordMasterWordLength)
            return entry.word;

        string[] fallbackTargets = { "SPELL", "OCEAN", "MAGIC" };
        return fallbackTargets[Random.Range(0, fallbackTargets.Length)];
    }

    private string GetRandomWordExcavationSourceWord()
    {
        int firstLength = Random.value < 0.5f ? 8 : 9;
        int secondLength = firstLength == 8 ? 9 : 8;

        if (TryGetDefinedWordByExactLength(firstLength, out string firstWord))
            return firstWord;

        if (TryGetDefinedWordByExactLength(secondLength, out string secondWord))
            return secondWord;

        List<string> fallbackWords = new List<string>();
        foreach (string word in ExcavationFallbackSourceWords)
        {
            if (word.Length == firstLength || word.Length == secondLength)
                fallbackWords.Add(word);
        }

        return fallbackWords.Count > 0
            ? fallbackWords[Random.Range(0, fallbackWords.Count)]
            : "TREASURE";
    }

    private bool TryGetDefinedWordByExactLength(int length, out string word)
    {
        if (dictionary != null)
        {
            for (int i = 0; i < 80; i++)
            {
                string candidate = dictionary.GetRandomWordByLengthRange(length, length);
                string definition = dictionary.GetDefinition(candidate);
                if (!string.IsNullOrWhiteSpace(candidate)
                    && candidate.Length == length
                    && IsUsableDefinition(definition))
                {
                    word = candidate.ToUpperInvariant();
                    return true;
                }
            }
        }

        List<string> fallbackWords = new List<string>();
        foreach (string fallback in ExcavationFallbackSourceWords)
        {
            if (fallback.Length == length)
                fallbackWords.Add(fallback);
        }

        if (fallbackWords.Count > 0)
        {
            word = fallbackWords[Random.Range(0, fallbackWords.Count)];
            return true;
        }

        word = string.Empty;
        return false;
    }

    private void ApplyWordExcavationTimeBonus(string answer)
    {
        int bonusSeconds = GetWordExcavationTimeBonus(answer);
        if (bonusSeconds <= 0)
            return;

        excavationTimer = Mathf.Min(excavationRoundSeconds, excavationTimer + bonusSeconds);
        UpdateTimerText();
    }

    private int GetWordExcavationTimeBonus(string answer)
    {
        if (string.IsNullOrWhiteSpace(answer))
            return 0;

        int length = answer.Length;
        if (!string.IsNullOrWhiteSpace(excavationSourceWord)
            && length == excavationSourceWord.Length)
        {
            return 10;
        }

        if (length >= 7)
            return 7;

        if (length == 6)
            return 5;

        if (length == 5)
            return 3;

        if (length >= 3)
            return 1;

        return 0;
    }

    private LetterClue[] EvaluateWordMasterGuess(string guess, string target)
    {
        LetterClue[] clues = new LetterClue[guess.Length];
        bool[] targetUsed = new bool[target.Length];
        bool[] guessUsed = new bool[guess.Length];

        for (int i = 0; i < guess.Length && i < target.Length; i++)
        {
            if (guess[i] != target[i])
                continue;

            clues[i] = LetterClue.Correct;
            targetUsed[i] = true;
            guessUsed[i] = true;
        }

        for (int i = 0; i < guess.Length; i++)
        {
            if (guessUsed[i])
                continue;

            clues[i] = LetterClue.Missing;

            for (int j = 0; j < target.Length; j++)
            {
                if (targetUsed[j] || guess[i] != target[j])
                    continue;

                clues[i] = LetterClue.Present;
                targetUsed[j] = true;
                break;
            }
        }

        return clues;
    }

    private void BuildWordMasterBoard()
    {
        if (optionsRoot == null)
            return;

        RectTransform existingBoard = FindRect(optionsRoot, "WordMasterBoard");
        if (existingBoard != null)
        {
            CacheWordMasterBoardReferences(existingBoard);
            RebuildWordMasterRows();
            return;
        }

        ClearChildren(optionsRoot);

        RectTransform board = CreateRect("WordMasterBoard", optionsRoot);
        LayoutElement boardLayout = board.gameObject.AddComponent<LayoutElement>();
        boardLayout.minWidth = 510f;
        boardLayout.preferredWidth = 510f;
        boardLayout.minHeight = 402f;
        boardLayout.preferredHeight = 402f;

        RectTransform boardPanel = CreatePanel(board, "WordMasterBoardPanel", new Color(0.055f, 0.033f, 0.018f, 0.96f), new Vector2(510f, 402f));
        boardPanel.anchorMin = new Vector2(0.5f, 0.5f);
        boardPanel.anchorMax = new Vector2(0.5f, 0.5f);
        boardPanel.pivot = new Vector2(0.5f, 0.5f);
        boardPanel.anchoredPosition = Vector2.zero;

        Outline boardTrim = boardPanel.gameObject.AddComponent<Outline>();
        boardTrim.effectColor = new Color(0.82f, 0.58f, 0.16f, 0.92f);
        boardTrim.effectDistance = new Vector2(3f, -3f);

        VerticalLayoutGroup panelGroup = boardPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        panelGroup.padding = new RectOffset(18, 18, 16, 18);
        panelGroup.spacing = 7f;
        panelGroup.childAlignment = TextAnchor.UpperCenter;
        panelGroup.childControlWidth = false;
        panelGroup.childControlHeight = false;
        panelGroup.childForceExpandWidth = false;
        panelGroup.childForceExpandHeight = false;

        RectTransform header = CreateRect("WordMasterHeader", boardPanel);
        LayoutElement headerLayout = header.gameObject.AddComponent<LayoutElement>();
        headerLayout.minWidth = 456f;
        headerLayout.preferredWidth = 456f;
        headerLayout.minHeight = 28f;
        headerLayout.preferredHeight = 28f;

        HorizontalLayoutGroup headerGroup = header.gameObject.AddComponent<HorizontalLayoutGroup>();
        headerGroup.spacing = 6f;
        headerGroup.childAlignment = TextAnchor.MiddleCenter;
        headerGroup.childControlWidth = false;
        headerGroup.childControlHeight = false;
        headerGroup.childForceExpandWidth = false;
        headerGroup.childForceExpandHeight = false;

        CreateWordMasterHeaderLabel(header, "WordMasterTurnLabel", "Turns", 54f, TextAlignmentOptions.Center);
        CreateWordMasterHeaderLabel(header, "WordMasterTilesLabel", string.Empty, 290f, TextAlignmentOptions.Center);
        CreateWordMasterHeaderLabel(header, "WordMasterPrizeLabel", "Prize", 94f, TextAlignmentOptions.Center);

        wordMasterRowsRoot = CreateRect("TurnRows", boardPanel);
        LayoutElement rowsLayout = wordMasterRowsRoot.gameObject.AddComponent<LayoutElement>();
        rowsLayout.minWidth = 456f;
        rowsLayout.preferredWidth = 456f;
        rowsLayout.minHeight = 334f;
        rowsLayout.preferredHeight = 334f;

        VerticalLayoutGroup rowsGroup = wordMasterRowsRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        rowsGroup.spacing = 7f;
        rowsGroup.childAlignment = TextAnchor.MiddleCenter;
        rowsGroup.childControlWidth = false;
        rowsGroup.childControlHeight = false;
        rowsGroup.childForceExpandWidth = false;
        rowsGroup.childForceExpandHeight = false;

        CreateWordMasterHintPanel(boardPanel);

        RebuildWordMasterRows();
    }

    private void CacheWordMasterBoardReferences(RectTransform board)
    {
        wordMasterRowsRoot = FindRect(board, "TurnRows");
        wordMasterStatsText = FindText(board, "StatsText");
        wordMasterHintPanel = FindRect(board, "HintPanel");
        RectTransform boardPanel = FindRect(board, "WordMasterBoardPanel");
        RectTransform turnPanel = boardPanel != null ? boardPanel : FindRect(board, "TurnBoard");

        if (wordMasterRowsRoot == null)
        {
            if (turnPanel != null)
            {
                wordMasterRowsRoot = CreateRect("TurnRows", turnPanel);
                wordMasterRowsRoot.anchorMin = Vector2.zero;
                wordMasterRowsRoot.anchorMax = Vector2.one;
                wordMasterRowsRoot.offsetMin = new Vector2(18f, 18f);
                wordMasterRowsRoot.offsetMax = new Vector2(-18f, -52f);

                VerticalLayoutGroup rowsGroup = wordMasterRowsRoot.gameObject.AddComponent<VerticalLayoutGroup>();
                rowsGroup.spacing = 6f;
                rowsGroup.childAlignment = TextAnchor.MiddleCenter;
                rowsGroup.childControlWidth = true;
                rowsGroup.childControlHeight = false;
                rowsGroup.childForceExpandWidth = true;
                rowsGroup.childForceExpandHeight = false;
            }
        }

        if (wordMasterHintPanel == null && turnPanel != null && ShouldUseWordMasterStartHint())
            CreateWordMasterHintPanel(turnPanel);

        WireWordMasterHintPanel();
        UpdateWordMasterHintPanel();
    }

    private void RebuildWordMasterRows()
    {
        if (wordMasterRowsRoot == null)
            return;

        if (wordMasterHintPanel != null)
        {
            wordMasterHintPanel.gameObject.SetActive(ShouldShowWordMasterHintPanel());
            UpdateWordMasterHintPanel();
        }

        if (RefreshExistingWordMasterRows())
            return;

        ClearChildren(wordMasterRowsRoot);

        for (int i = 0; i < wordMasterMaxTurns; i++)
        {
            GetWordMasterRowState(i, out string guess, out LetterClue[] clues, out bool active);
            CreateWordMasterRow(wordMasterRowsRoot, i, guess, clues, active);
        }
    }

    private bool RefreshExistingWordMasterRows()
    {
        if (wordMasterRowsRoot == null)
            return false;

        List<RectTransform> rows = new List<RectTransform>();
        for (int i = 0; i < wordMasterMaxTurns; i++)
        {
            RectTransform row = FindRect(wordMasterRowsRoot, "WordMasterRowUI_" + (i + 1));
            if (row == null)
                row = FindRect(wordMasterRowsRoot, "GuessRow" + (i + 1));

            if (row == null || row.childCount < wordMasterWordLength + 1)
                return false;

            rows.Add(row);
        }

        for (int i = 0; i < rows.Count; i++)
        {
            GetWordMasterRowState(i, out string guess, out LetterClue[] clues, out bool active);
            if (!UpdateWordMasterRow(rows[i], i, guess, clues, active))
                return false;
        }

        return true;
    }

    private void GetWordMasterRowState(int rowIndex, out string guess, out LetterClue[] clues, out bool active)
    {
        guess = string.Empty;
        clues = null;
        active = rowIndex == wordMasterTurn && !wordMasterSolved && wordMasterTurn < wordMasterMaxTurns;

        if (rowIndex < wordMasterGuesses.Count)
        {
            guess = wordMasterGuesses[rowIndex];
            clues = wordMasterGuessClues[rowIndex];
            active = false;
            return;
        }

        if (active)
            guess = GetWordMasterCurrentGuess();
    }

    private void CreateWordMasterHeaderLabel(Transform parent, string objectName, string label, float width, TextAlignmentOptions alignment)
    {
        TextMeshProUGUI text = CreateText(parent, objectName, label, 18f, FontStyles.Bold, lightTextColor);
        text.alignment = alignment;

        LayoutElement layout = text.gameObject.AddComponent<LayoutElement>();
        layout.minWidth = width;
        layout.preferredWidth = width;
        layout.minHeight = 28f;
        layout.preferredHeight = 28f;
    }

    private void CreateWordMasterRow(Transform parent, int rowIndex, string guess, LetterClue[] clues, bool active)
    {
        Color rowColor = active
            ? new Color(0.28f, 0.19f, 0.06f, 0.92f)
            : new Color(0.08f, 0.045f, 0.02f, 0.58f);

        RectTransform row = CreatePanel(parent, "WordMasterRowUI_" + (rowIndex + 1), rowColor, new Vector2(456f, 60f));

        LayoutElement rowLayout = row.gameObject.AddComponent<LayoutElement>();
        rowLayout.minWidth = 456f;
        rowLayout.preferredWidth = 456f;
        rowLayout.minHeight = 60f;
        rowLayout.preferredHeight = 60f;

        if (active)
        {
            Outline activeOutline = row.gameObject.AddComponent<Outline>();
            activeOutline.effectColor = new Color(1f, 0.88f, 0.18f, 1f);
            activeOutline.effectDistance = new Vector2(3f, -3f);
        }

        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(6, 6, 4, 4);
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        RectTransform turnBadge = CreatePanel(row, "TurnBadge", active ? new Color(0.94f, 0.82f, 0.24f, 1f) : new Color(0.22f, 0.19f, 0.13f, 0.96f), new Vector2(54f, 52f));
        LayoutElement turnBadgeLayout = turnBadge.gameObject.AddComponent<LayoutElement>();
        turnBadgeLayout.minWidth = 54f;
        turnBadgeLayout.preferredWidth = 54f;
        turnBadgeLayout.minHeight = 52f;
        turnBadgeLayout.preferredHeight = 52f;

        TextMeshProUGUI turnText = CreateText(turnBadge, "Turn", (rowIndex + 1).ToString(), 30f, FontStyles.Bold, active ? textColor : lightTextColor);
        RectTransform turnTextRect = turnText.GetComponent<RectTransform>();
        turnTextRect.anchorMin = Vector2.zero;
        turnTextRect.anchorMax = Vector2.one;
        turnTextRect.offsetMin = new Vector2(2f, 1f);
        turnTextRect.offsetMax = new Vector2(-2f, -1f);
        turnText.alignment = TextAlignmentOptions.Center;

        for (int i = 0; i < wordMasterWordLength; i++)
        {
            char letter = i < guess.Length ? guess[i] : ' ';
            Color color = active && letter != ' '
                ? new Color(0.93f, 0.84f, 0.56f, 0.98f)
                : clues == null ? new Color(0.16f, 0.10f, 0.05f, 0.88f) : GetWordMasterClueColor(clues[i]);

            if (active && IsWordMasterLockedPosition(i))
                color = wordMasterCorrectColor;

            UnityEngine.Events.UnityAction clickAction = null;
            if (active && letter != ' ' && !IsWordMasterLockedPosition(i))
            {
                int capturedPosition = i;
                clickAction = () => RemoveWordMasterLettersFromGuessPosition(capturedPosition);
            }

            CreateLetterTileDisplay(row, letter, color, new Vector2(52f, 52f), clickAction, "WordMasterTileUI_" + (i + 1));
        }

        CreateWordMasterPrizeUI(row, rowIndex, active);
    }

    private bool UpdateWordMasterRow(RectTransform row, int rowIndex, string guess, LetterClue[] clues, bool active)
    {
        if (row == null)
            return false;

        row.name = "WordMasterRowUI_" + (rowIndex + 1);

        if (row.childCount < wordMasterWordLength + 1)
            return false;

        Image rowImage = row.GetComponent<Image>();
        if (rowImage != null)
        {
            rowImage.color = active
                ? new Color(0.28f, 0.19f, 0.06f, 0.92f)
                : new Color(0.08f, 0.045f, 0.02f, 0.58f);
        }

        Outline activeOutline = row.GetComponent<Outline>();
        if (active)
        {
            if (activeOutline == null)
                activeOutline = row.gameObject.AddComponent<Outline>();

            activeOutline.enabled = true;
            activeOutline.effectColor = new Color(1f, 0.88f, 0.18f, 1f);
            activeOutline.effectDistance = new Vector2(3f, -3f);
        }
        else if (activeOutline != null)
        {
            activeOutline.enabled = false;
        }

        RectTransform turnBadge = row.GetChild(0) as RectTransform;
        if (turnBadge == null)
            return false;

        turnBadge.name = "TurnBadge";
        Image turnBadgeImage = turnBadge.GetComponent<Image>();
        if (turnBadgeImage != null)
            turnBadgeImage.color = active ? new Color(0.94f, 0.82f, 0.24f, 1f) : new Color(0.22f, 0.19f, 0.13f, 0.96f);

        TextMeshProUGUI turnText = FindText(turnBadge, "Turn");
        if (turnText != null)
        {
            turnText.text = (rowIndex + 1).ToString();
            turnText.color = active ? textColor : lightTextColor;
        }

        for (int i = 0; i < wordMasterWordLength; i++)
        {
            RectTransform slot = row.GetChild(i + 1) as RectTransform;
            if (slot == null)
                return false;

            slot.name = "WordMasterTileUI_" + (i + 1);
            char letter = i < guess.Length ? guess[i] : ' ';
            Color color = active && letter != ' '
                ? new Color(0.93f, 0.84f, 0.56f, 0.98f)
                : clues == null ? new Color(0.16f, 0.10f, 0.05f, 0.88f) : GetWordMasterClueColor(clues[i]);

            if (active && IsWordMasterLockedPosition(i))
                color = wordMasterCorrectColor;

            Image slotImage = slot.GetComponent<Image>();
            if (slotImage != null)
                slotImage.color = color;

            TextMeshProUGUI slotText = FindText(slot, "Text");
            if (slotText != null)
            {
                slotText.text = letter == ' ' ? string.Empty : letter.ToString();
                slotText.color = GetWordMasterTileTextColor(color);
            }

            Button button = slot.GetComponent<Button>();
            if (button != null)
                button.onClick.RemoveAllListeners();

            if (active && letter != ' ' && !IsWordMasterLockedPosition(i))
            {
                if (button == null)
                    button = slot.gameObject.AddComponent<Button>();

                int capturedPosition = i;
                button.targetGraphic = slotImage;
                button.onClick.AddListener(() => RemoveWordMasterLettersFromGuessPosition(capturedPosition));
            }
        }

        RectTransform prize = row.childCount > wordMasterWordLength + 1
            ? row.GetChild(wordMasterWordLength + 1) as RectTransform
            : null;

        if (prize == null)
            CreateWordMasterPrizeUI(row, rowIndex, active);
        else
            UpdateWordMasterPrizeUI(prize, rowIndex, active);

        return true;
    }

    private void CreateWordMasterPrizeUI(Transform parent, int rowIndex, bool active)
    {
        RectTransform prize = CreatePanel(parent, "WordMasterPrizeUI", GetWordMasterPrizeColor(rowIndex, active), new Vector2(94f, 52f));
        LayoutElement layout = prize.gameObject.AddComponent<LayoutElement>();
        layout.minWidth = 94f;
        layout.preferredWidth = 94f;
        layout.minHeight = 52f;
        layout.preferredHeight = 52f;

        TextMeshProUGUI text = CreateText(prize, "PrizeText", GetWordMasterPrizeText(rowIndex), 18f, FontStyles.Bold, GetWordMasterPrizeTextColor(rowIndex, active));
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(4f, 2f);
        textRect.offsetMax = new Vector2(-4f, -2f);
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 12f;
        text.fontSizeMax = 18f;

        TextMeshProUGUI crossText = CreateText(prize, "PrizeCrossText", "X", 40f, FontStyles.Bold, new Color(0.95f, 0.06f, 0.04f, 1f));
        RectTransform crossRect = crossText.GetComponent<RectTransform>();
        crossRect.anchorMin = Vector2.zero;
        crossRect.anchorMax = Vector2.one;
        crossRect.offsetMin = new Vector2(2f, -2f);
        crossRect.offsetMax = new Vector2(-2f, 2f);
        crossText.alignment = TextAlignmentOptions.Center;
        crossText.enableAutoSizing = true;
        crossText.fontSizeMin = 20f;
        crossText.fontSizeMax = 40f;
        crossText.gameObject.SetActive(IsWordMasterPrizeCrossed(rowIndex));
    }

    private void UpdateWordMasterPrizeUI(RectTransform prize, int rowIndex, bool active)
    {
        prize.name = "WordMasterPrizeUI";

        Image image = prize.GetComponent<Image>();
        if (image != null)
            image.color = GetWordMasterPrizeColor(rowIndex, active);

        TextMeshProUGUI text = FindText(prize, "PrizeText");
        if (text != null)
        {
            text.text = GetWordMasterPrizeText(rowIndex);
            text.color = GetWordMasterPrizeTextColor(rowIndex, active);
            text.fontSizeMax = 18f;
        }

        TextMeshProUGUI crossText = FindText(prize, "PrizeCrossText");
        if (crossText == null)
        {
            crossText = CreateText(prize, "PrizeCrossText", "X", 40f, FontStyles.Bold, new Color(0.95f, 0.06f, 0.04f, 1f));
            RectTransform crossRect = crossText.GetComponent<RectTransform>();
            crossRect.anchorMin = Vector2.zero;
            crossRect.anchorMax = Vector2.one;
            crossRect.offsetMin = new Vector2(2f, -2f);
            crossRect.offsetMax = new Vector2(-2f, 2f);
            crossText.alignment = TextAlignmentOptions.Center;
            crossText.enableAutoSizing = true;
            crossText.fontSizeMin = 20f;
            crossText.fontSizeMax = 40f;
        }

        crossText.text = "X";
        crossText.color = new Color(0.95f, 0.06f, 0.04f, 1f);
        crossText.gameObject.SetActive(IsWordMasterPrizeCrossed(rowIndex));
    }

    private string GetWordMasterPrizeText(int rowIndex)
    {
        switch (rowIndex)
        {
            case 0:
                return "10,000";
            case 1:
                return "5,000";
            case 2:
                return "2,500";
            case 3:
                return "1,000";
            default:
                return "500";
        }
    }

    private Color GetWordMasterPrizeColor(int rowIndex, bool active)
    {
        if (IsWordMasterPrizeCrossed(rowIndex))
            return new Color(0.08f, 0.06f, 0.04f, 0.94f);

        return active
            ? new Color(0.46f, 0.34f, 0.10f, 0.96f)
            : new Color(0.17f, 0.13f, 0.08f, 0.92f);
    }

    private Color GetWordMasterPrizeTextColor(int rowIndex, bool active)
    {
        if (IsWordMasterPrizeCrossed(rowIndex))
            return new Color(0.48f, 0.42f, 0.30f, 0.72f);

        return active ? new Color(1f, 0.92f, 0.42f, 1f) : lightTextColor;
    }

    private bool IsWordMasterPrizeCrossed(int rowIndex)
    {
        if (string.IsNullOrWhiteSpace(GetWordMasterPrizeText(rowIndex))
            || rowIndex < 0
            || rowIndex >= wordMasterGuesses.Count
            || rowIndex >= wordMasterGuessClues.Count)
        {
            return false;
        }

        LetterClue[] clues = wordMasterGuessClues[rowIndex];
        if (clues == null || clues.Length == 0)
            return false;

        for (int i = 0; i < clues.Length; i++)
        {
            if (clues[i] != LetterClue.Correct)
                return true;
        }

        return false;
    }

    private void BuildWordExcavationBoard()
    {
        if (optionsRoot == null)
            return;

        RectTransform existingBoard = FindRect(optionsRoot, "WordExcavationBoard");
        if (existingBoard != null)
        {
            CacheWordExcavationBoardReferences(existingBoard);
            UpdateWordExcavationBoard();
            return;
        }

        ClearChildren(optionsRoot);

        RectTransform board = CreatePanel(optionsRoot, "WordExcavationBoard", new Color(0.055f, 0.033f, 0.018f, 0.96f), new Vector2(510f, 330f));
        LayoutElement boardLayout = board.gameObject.AddComponent<LayoutElement>();
        boardLayout.minWidth = 510f;
        boardLayout.preferredWidth = 510f;
        boardLayout.minHeight = 602.4f;
        boardLayout.preferredHeight = 330f;

        Outline boardTrim = board.gameObject.AddComponent<Outline>();
        boardTrim.effectColor = new Color(0.82f, 0.58f, 0.16f, 0.92f);
        boardTrim.effectDistance = new Vector2(3f, -3f);

        HorizontalLayoutGroup boardGroup = board.gameObject.AddComponent<HorizontalLayoutGroup>();
        boardGroup.padding = new RectOffset(14, 14, 12, 12);
        boardGroup.spacing = 10f;
        boardGroup.childAlignment = TextAnchor.MiddleCenter;
        boardGroup.childControlWidth = false;
        boardGroup.childControlHeight = false;
        boardGroup.childForceExpandWidth = false;
        boardGroup.childForceExpandHeight = false;

        CreateWordExcavationTimerColumn(board);
        CreateWordExcavationCenterPanel(board);
        CreateWordExcavationPrizeColumn(board);

        CacheWordExcavationBoardReferences(board);
        UpdateWordExcavationBoard();
    }

    private void CacheWordExcavationBoardReferences(RectTransform board)
    {
        wordExcavationTimerFill = FindRect(board, "ExcavationTimeFill");
        wordExcavationTimerText = FindText(board, "ExcavationTimeText");
        EnsureWordExcavationTimerText(board);
        RectTransform sourceText = FindRect(board, "ExcavationSourceText");
        if (sourceText != null)
            DestroySafe(sourceText.gameObject);

        wordExcavationFoundWordsText = FindText(board, "ExcavationFoundWordsText");
        ApplyWordExcavationFoundWordsTextLayout();
        EnsureWordExcavationFoundWordsTierTexts(board);
    }

    private void EnsureWordExcavationFoundWordsTierTexts(RectTransform board)
    {
        RectTransform panel = FindRect(board, "ExcavationWordPanel");
        if (panel == null)
            return;

        for (int i = 1; i < 5; i++)
        {
            TextMeshProUGUI tierText = FindText(panel, GetWordExcavationFoundWordsTierName(i));
            if (tierText == null)
                tierText = CreateText(panel, GetWordExcavationFoundWordsTierName(i), string.Empty, 17f, FontStyles.Bold, lightTextColor);

            ApplyWordExcavationFoundWordsTextLayout(tierText, i);
        }
    }

    private void CreateWordExcavationTimerColumn(Transform parent)
    {
        RectTransform column = CreatePanel(parent, "ExcavationTimeColumn", new Color(0.13f, 0.09f, 0.045f, 0.95f), new Vector2(58f, 585.33f));
        AddFixedLayout(column, 58f, 306f);

        TextMeshProUGUI label = CreateText(column, "ExcavationTimeLabel", "Time", 15f, FontStyles.Bold, lightTextColor);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(2f, -28f);
        labelRect.offsetMax = new Vector2(-2f, -4f);
        label.alignment = TextAlignmentOptions.Center;

        RectTransform track = CreatePanel(column, "ExcavationTimeTrack", new Color(0.04f, 0.025f, 0.015f, 1f), new Vector2(20f, 230f));
        track.anchorMin = new Vector2(0.5f, 0f);
        track.anchorMax = new Vector2(0.5f, 1f);
        track.pivot = new Vector2(0.5f, 0f);
        track.offsetMin = new Vector2(-10f, 42f);
        track.offsetMax = new Vector2(10f, -34f);

        wordExcavationTimerFill = CreatePanel(track, "ExcavationTimeFill", Color.green, Vector2.zero);
        wordExcavationTimerFill.anchorMin = Vector2.zero;
        wordExcavationTimerFill.anchorMax = Vector2.one;
        wordExcavationTimerFill.offsetMin = new Vector2(3f, 3f);
        wordExcavationTimerFill.offsetMax = new Vector2(-3f, -3f);

        wordExcavationTimerText = CreateText(column, "ExcavationTimeText", "60", 20f, FontStyles.Bold, lightTextColor);
        ApplyWordExcavationTimerTextLayout();
    }

    private void EnsureWordExcavationTimerText(RectTransform board)
    {
        RectTransform column = FindRect(board, "ExcavationTimeColumn");
        if (column == null)
            return;

        if (wordExcavationTimerText == null)
            wordExcavationTimerText = CreateText(column, "ExcavationTimeText", "60", 20f, FontStyles.Bold, lightTextColor);

        ApplyWordExcavationTimerTextLayout();
    }

    private void ApplyWordExcavationTimerTextLayout()
    {
        if (wordExcavationTimerText == null)
            return;

        RectTransform timeRect = wordExcavationTimerText.GetComponent<RectTransform>();
        timeRect.anchorMin = new Vector2(0.5f, 0f);
        timeRect.anchorMax = new Vector2(0.5f, 0f);
        timeRect.pivot = new Vector2(0.5f, 0f);
        timeRect.sizeDelta = new Vector2(44f, 30f);
        timeRect.anchoredPosition = new Vector2(0f, 7f);
        wordExcavationTimerText.alignment = TextAlignmentOptions.Center;
        wordExcavationTimerText.fontSize = 20f;
        wordExcavationTimerText.fontStyle = FontStyles.Bold;
        wordExcavationTimerText.color = lightTextColor;
        wordExcavationTimerText.gameObject.SetActive(true);
    }

    private void CreateWordExcavationCenterPanel(Transform parent)
    {
        RectTransform panel = CreatePanel(parent, "ExcavationWordPanel", new Color(0.035f, 0.035f, 0.022f, 0.98f), new Vector2(501.6f, 581.9f));
        AddFixedLayout(panel, 330f, 306f);

        wordExcavationFoundWordsText = CreateText(panel, "ExcavationFoundWordsText", string.Empty, 17f, FontStyles.Bold, lightTextColor);
        ApplyWordExcavationFoundWordsTextLayout(wordExcavationFoundWordsText, 0);

        for (int i = 1; i < 5; i++)
        {
            TextMeshProUGUI tierText = CreateText(panel, GetWordExcavationFoundWordsTierName(i), string.Empty, 17f, FontStyles.Bold, lightTextColor);
            ApplyWordExcavationFoundWordsTextLayout(tierText, i);
        }
    }

    private void ApplyWordExcavationFoundWordsTextLayout()
    {
        ApplyWordExcavationFoundWordsTextLayout(wordExcavationFoundWordsText, 0);
    }

    private void ApplyWordExcavationFoundWordsTextLayout(TextMeshProUGUI text, int tierIndex)
    {
        if (text == null)
            return;

        float bottom = GetWordExcavationFoundWordsTierBottom(tierIndex);
        const float height = 58f;

        RectTransform foundRect = text.GetComponent<RectTransform>();
        foundRect.anchorMin = new Vector2(0f, 0f);
        foundRect.anchorMax = new Vector2(1f, 0f);
        foundRect.offsetMin = new Vector2(16f, bottom);
        foundRect.offsetMax = new Vector2(-16f, bottom + height);

        text.alignment = TextAlignmentOptions.BottomLeft;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
    }

    private void CreateWordExcavationPrizeColumn(Transform parent)
    {
        RectTransform column = CreatePanel(parent, "ExcavationPrizeColumn", new Color(0.13f, 0.09f, 0.045f, 0.95f), new Vector2(78f, 605.4f));
        AddFixedLayout(column, 78f, 306f);

        VerticalLayoutGroup layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 7, 7);
        layout.spacing = 62.1f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        TextMeshProUGUI label = CreateText(column, "ExcavationPrizeLabel", "Prize", 15f, FontStyles.Bold, lightTextColor);
        AddFixedLayout(label.GetComponent<RectTransform>(), 62f, 24f);
        label.alignment = TextAlignmentOptions.Center;

        string[] prizes = { "10,000", "5,000", "2,500", "1,000", "500" };
        for (int i = 0; i < prizes.Length; i++)
            CreateWordExcavationPrizeBox(column, prizes[i]);
    }

    private void CreateWordExcavationPrizeBox(Transform parent, string value)
    {
        RectTransform box = CreatePanel(parent, "ExcavationPrize_" + value.Replace(",", string.Empty), new Color(0.17f, 0.13f, 0.08f, 0.92f), new Vector2(62f, 45f));
        AddFixedLayout(box, 62f, 45f);

        int requiredWords = GetWordExcavationPrizeRequiredWordCount(value);
        TextMeshProUGUI text = CreateText(box, "Text", value, 15f, FontStyles.Bold, GetWordExcavationPrizeTextColor(requiredWords));
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(3f, 2f);
        textRect.offsetMax = new Vector2(-3f, -2f);
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 10f;
        text.fontSizeMax = 15f;
    }

    private void UpdateWordExcavationBoard()
    {
        UpdateWordExcavationTimerMeter();
        UpdateWordExcavationFoundWordsText();
        UpdateWordExcavationPrizeProgress();
    }

    private void UpdateWordExcavationTimerMeter()
    {
        float maxTime = Mathf.Max(1f, excavationRoundSeconds);
        float ratio = Mathf.Clamp01(excavationTimer / maxTime);

        if (wordExcavationTimerFill != null)
        {
            wordExcavationTimerFill.anchorMin = Vector2.zero;
            wordExcavationTimerFill.anchorMax = new Vector2(1f, ratio);
            wordExcavationTimerFill.offsetMin = new Vector2(3f, 3f);
            wordExcavationTimerFill.offsetMax = new Vector2(-3f, -3f);

            Image fillImage = wordExcavationTimerFill.GetComponent<Image>();
            if (fillImage != null)
                fillImage.color = Color.Lerp(new Color(0.34f, 0.02f, 0.02f, 1f), new Color(0.42f, 1f, 0.08f, 1f), ratio);
        }

        if (wordExcavationTimerText != null)
            wordExcavationTimerText.text = Mathf.CeilToInt(excavationTimer).ToString();
    }

    private void UpdateWordExcavationFoundWordsText()
    {
        if (wordExcavationFoundWordsText == null)
            return;

        for (int i = 0; i < 5; i++)
        {
            TextMeshProUGUI tierText = GetWordExcavationFoundWordsTierText(i);
            if (tierText == null)
                continue;

            tierText.text = BuildWordExcavationFoundWordsTierGrid(i);
        }
    }

    private TextMeshProUGUI GetWordExcavationFoundWordsTierText(int tierIndex)
    {
        if (tierIndex == 0)
            return wordExcavationFoundWordsText;

        if (optionsRoot == null)
            return null;

        return FindText(optionsRoot, GetWordExcavationFoundWordsTierName(tierIndex));
    }

    private string BuildWordExcavationFoundWordsTierGrid(int tierIndex)
    {
        const int wordsPerRow = 3;
        const int rowsPerTier = 2;
        const int columnWidth = 20;
        int wordsPerTier = wordsPerRow * rowsPerTier;
        int tierStart = tierIndex * wordsPerTier;

        if (tierStart >= foundExcavationWordsInOrder.Count)
            return string.Empty;

        List<string> tierRows = new List<string>();
        int tierEnd = Mathf.Min(tierStart + wordsPerTier, foundExcavationWordsInOrder.Count);

        for (int rowStart = tierStart; rowStart < tierEnd; rowStart += wordsPerRow)
        {
            List<string> columns = new List<string>();
            int rowEnd = Mathf.Min(rowStart + wordsPerRow, tierEnd);

            for (int j = rowStart; j < rowEnd; j++)
            {
                string word = foundExcavationWordsInOrder[j].ToUpperInvariant();
                columns.Add(j < rowEnd - 1 ? word.PadRight(columnWidth) : word);
            }

            tierRows.Add(string.Join(string.Empty, columns));
        }

        tierRows.Reverse();
        return string.Join("\n", tierRows);
    }

    private string GetWordExcavationFoundWordsTierName(int tierIndex)
    {
        switch (tierIndex)
        {
            case 1:
                return "ExcavationFoundWordsText_1000";
            case 2:
                return "ExcavationFoundWordsText_2500";
            case 3:
                return "ExcavationFoundWordsText_5000";
            case 4:
                return "ExcavationFoundWordsText_10000";
            default:
                return "ExcavationFoundWordsText";
        }
    }

    private float GetWordExcavationFoundWordsTierBottom(int tierIndex)
    {
        const float bottomTierY = 15.70001f;
        const float prizeTierStepY = 107.1f;
        return bottomTierY + (Mathf.Clamp(tierIndex, 0, 4) * prizeTierStepY);
    }

    private void UpdateWordExcavationPrizeProgress()
    {
        if (optionsRoot == null)
            return;

        string[] prizes = { "10,000", "5,000", "2,500", "1,000", "500" };
        for (int i = 0; i < prizes.Length; i++)
        {
            RectTransform prize = FindRect(optionsRoot, "ExcavationPrize_" + prizes[i].Replace(",", string.Empty));
            if (prize == null)
                continue;

            TextMeshProUGUI text = FindText(prize, "Text");
            if (text == null)
                continue;

            int requiredWords = GetWordExcavationPrizeRequiredWordCount(prizes[i]);
            text.color = GetWordExcavationPrizeTextColor(requiredWords);
        }
    }

    private int GetWordExcavationPrizeRequiredWordCount(string prizeText)
    {
        switch (prizeText)
        {
            case "10,000":
                return 25;
            case "5,000":
                return 19;
            case "2,500":
                return 13;
            case "1,000":
                return 7;
            case "500":
                return 3;
            default:
                return int.MaxValue;
        }
    }

    private Color GetWordExcavationPrizeTextColor(int requiredWords)
    {
        return foundExcavationWordsInOrder.Count >= requiredWords
            ? new Color(0.42f, 1f, 0.08f, 1f)
            : lightTextColor;
    }

    private void AddFixedLayout(RectTransform rect, float width, float height)
    {
        LayoutElement layout = rect.gameObject.GetComponent<LayoutElement>();
        if (layout == null)
            layout = rect.gameObject.AddComponent<LayoutElement>();

        layout.minWidth = width;
        layout.preferredWidth = width;
        layout.minHeight = height;
        layout.preferredHeight = height;
    }

    private void CreateWordMasterHintPanel(Transform parent)
    {
        wordMasterHintPanel = null;
        if (!ShouldUseWordMasterStartHint())
            return;

        char hintLetter = wordMasterTargetWord[wordMasterHintIndex];
        wordMasterHintPanel = CreatePanel(parent, "HintPanel", new Color(0.06f, 0.035f, 0.015f, 0.96f), new Vector2(330f, 82f));
        wordMasterHintPanel.anchorMin = new Vector2(0f, 0.5f);
        wordMasterHintPanel.anchorMax = new Vector2(0f, 0.5f);
        wordMasterHintPanel.pivot = new Vector2(0f, 0.5f);
        wordMasterHintPanel.anchoredPosition = new Vector2(32f, -36f);

        Image border = wordMasterHintPanel.GetComponent<Image>();
        if (border != null)
            border.color = new Color(0.08f, 0.045f, 0.02f, 0.96f);

        TextMeshProUGUI title = CreateText(wordMasterHintPanel, "HintTitle", "Hint!", 30f, FontStyles.Bold, wordMasterCorrectColor);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.48f);
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = new Vector2(18f, 0f);
        titleRect.offsetMax = new Vector2(-18f, -4f);
        title.alignment = TextAlignmentOptions.Center;

        TextMeshProUGUI body = CreateText(wordMasterHintPanel, "HintBody", $"{hintLetter} is the first letter of the mystery word.", 18f, FontStyles.Bold, new Color(0.92f, 0.35f, 0.24f, 1f));
        RectTransform bodyRect = body.GetComponent<RectTransform>();
        bodyRect.anchorMin = Vector2.zero;
        bodyRect.anchorMax = new Vector2(1f, 0.5f);
        bodyRect.offsetMin = new Vector2(18f, 8f);
        bodyRect.offsetMax = new Vector2(-18f, 2f);
        body.alignment = TextAlignmentOptions.Center;
        body.textWrappingMode = TextWrappingModes.Normal;
        WireWordMasterHintPanel();
    }

    private void UpdateWordMasterHintPanel()
    {
        if (wordMasterHintPanel == null || !ShouldUseWordMasterStartHint())
            return;

        char hintLetter = wordMasterTargetWord[wordMasterHintIndex];

        TextMeshProUGUI title = FindText(wordMasterHintPanel, "HintTitle");
        if (title != null)
            title.text = "Hint!";

        TextMeshProUGUI body = FindText(wordMasterHintPanel, "HintBody");
        if (body != null)
            body.text = $"{hintLetter} is the first letter of the mystery word.";
    }

    private void WireWordMasterHintPanel()
    {
        if (wordMasterHintPanel == null)
            return;

        Image image = wordMasterHintPanel.GetComponent<Image>();
        if (image != null)
            image.raycastTarget = true;

        Button button = wordMasterHintPanel.GetComponent<Button>();
        if (button == null)
            button = wordMasterHintPanel.gameObject.AddComponent<Button>();

        button.targetGraphic = image;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(DismissWordMasterHintPanel);
    }

    private void DismissWordMasterHintPanel()
    {
        wordMasterHintPanelDismissed = true;

        if (wordMasterHintPanel != null)
            wordMasterHintPanel.gameObject.SetActive(false);
    }

    private void UpdateWordMasterKeyboard(string guess, LetterClue[] clues)
    {
        for (int i = 0; i < guess.Length; i++)
        {
            char letter = guess[i];
            Color clueColor = GetWordMasterClueColor(clues[i]);

            if (!wordMasterKeyboardColors.TryGetValue(letter, out Color existingColor)
                || IsBetterWordMasterClue(clueColor, existingColor))
            {
                wordMasterKeyboardColors[letter] = clueColor;
            }
        }
    }

    private bool IsBetterWordMasterClue(Color candidate, Color existing)
    {
        return GetWordMasterClueRank(candidate) > GetWordMasterClueRank(existing);
    }

    private int GetWordMasterClueRank(Color color)
    {
        if (ColorsApproximatelyEqual(color, wordMasterCorrectColor))
            return 3;

        if (ColorsApproximatelyEqual(color, wordMasterPresentColor))
            return 2;

        if (ColorsApproximatelyEqual(color, wordMasterMissingColor))
            return 1;

        return 0;
    }

    private bool ColorsApproximatelyEqual(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < 0.02f
            && Mathf.Abs(a.g - b.g) < 0.02f
            && Mathf.Abs(a.b - b.b) < 0.02f;
    }

    private Color GetWordMasterClueColor(LetterClue clue)
    {
        switch (clue)
        {
            case LetterClue.Correct:
                return wordMasterCorrectColor;
            case LetterClue.Present:
                return wordMasterPresentColor;
            default:
                return wordMasterMissingColor;
        }
    }

    private void BuildUI()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = gameObject.AddComponent<Canvas>();

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = gameObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        root = CreateRect("MinigamesUI", transform);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        Image background = root.gameObject.AddComponent<Image>();
        background.color = backgroundColor;

        CreateSelectionScreen(root);
        CreatePlayScreen(root);
    }

    private void CreateSelectionScreen(Transform parent)
    {
        selectionScreen = CreateRect("MinigameSelectionScreen", parent);
        selectionScreen.anchorMin = Vector2.zero;
        selectionScreen.anchorMax = Vector2.one;
        selectionScreen.offsetMin = Vector2.zero;
        selectionScreen.offsetMax = Vector2.zero;

        RectTransform panel = CreatePanel(selectionScreen, "SelectionBoard", panelColor, new Vector2(920f, 790f));
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup panelLayout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        panelLayout.padding = new RectOffset(54, 54, 42, 42);
        panelLayout.spacing = 16f;
        panelLayout.childControlWidth = true;
        panelLayout.childControlHeight = true;
        panelLayout.childForceExpandWidth = true;
        panelLayout.childForceExpandHeight = false;

        TextMeshProUGUI heading = CreateText(panel, "Title", "SPELLSTRIKE\nMINI-GAMES", 50f, FontStyles.Bold, textColor);
        heading.alignment = TextAlignmentOptions.Center;
        heading.characterSpacing = 4f;
        heading.lineSpacing = -20f;
        heading.gameObject.AddComponent<LayoutElement>().preferredHeight = 138f;

        CreateMinigameOption(panel, "Word Master", "Guess the hidden word from gold and silver clues.", new Color(0.62f, 0.85f, 0.16f, 0.95f), OpenWordMasterScene);
        CreateMinigameOption(panel, "Spell It Out", "Build the word from jumbled tiles.", new Color(0.95f, 0.83f, 0.20f, 0.95f), OpenSpellItOutScene);
        CreateMinigameOption(panel, "Word Excavation", "Find smaller words inside a longer word.", new Color(0.98f, 0.49f, 0.12f, 0.95f), OpenWordExcavationScene);

        RectTransform spacer = CreateRect("Spacer", panel);
        spacer.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        CreateButton(panel, "BackButton", "Main Menu", buttonColor, new Vector2(360f, 58f), ReturnToMainMenu);
    }

    private void CreateMinigameOption(Transform parent, string optionTitle, string description, Color color, UnityEngine.Events.UnityAction action)
    {
        Button button = CreateButton(parent, optionTitle.Replace(" ", string.Empty) + "Button", string.Empty, color, new Vector2(0f, 104f), action);
        LayoutElement layout = button.GetComponent<LayoutElement>();
        if (layout != null)
        {
            layout.minWidth = 0f;
            layout.preferredWidth = 0f;
            layout.minHeight = 104f;
            layout.preferredHeight = 104f;
        }

        RectTransform rect = button.GetComponent<RectTransform>();

        TextMeshProUGUI title = CreateText(rect, "Title", optionTitle, 36f, FontStyles.Bold, textColor);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.43f);
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = new Vector2(24f, 0f);
        titleRect.offsetMax = new Vector2(-24f, -6f);
        title.alignment = TextAlignmentOptions.Center;

        TextMeshProUGUI body = CreateText(rect, "Description", description, 22f, FontStyles.Bold, textColor);
        RectTransform bodyRect = body.GetComponent<RectTransform>();
        bodyRect.anchorMin = Vector2.zero;
        bodyRect.anchorMax = new Vector2(1f, 0.48f);
        bodyRect.offsetMin = new Vector2(24f, 8f);
        bodyRect.offsetMax = new Vector2(-24f, 2f);
        body.alignment = TextAlignmentOptions.Center;
        body.textWrappingMode = TextWrappingModes.Normal;
    }

    private void CreatePlayScreen(Transform parent)
    {
        playScreen = CreateRect("MinigamePlayScreen", parent);
        playScreen.anchorMin = Vector2.zero;
        playScreen.anchorMax = Vector2.one;
        playScreen.offsetMin = Vector2.zero;
        playScreen.offsetMax = Vector2.zero;

        RectTransform panel = CreatePanel(playScreen, "BookPanel", panelColor, new Vector2(1420f, 790f));
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup panelLayout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        panelLayout.padding = new RectOffset(52, 52, 40, 40);
        panelLayout.spacing = 18f;
        panelLayout.childControlWidth = true;
        panelLayout.childControlHeight = true;
        panelLayout.childForceExpandWidth = true;
        panelLayout.childForceExpandHeight = false;

        titleText = CreateText(panel, "Title", "SPELLSTRIKE MINIGAMES", 42f, FontStyles.Bold, textColor);
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.characterSpacing = 5f;
        titleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 58f;

        RectTransform playArea = CreatePanel(panel, "PlayArea", cardColor, new Vector2(0f, 560f));
        LayoutElement playAreaLayout = playArea.gameObject.AddComponent<LayoutElement>();
        playAreaLayout.minHeight = 560f;
        playAreaLayout.preferredHeight = 560f;

        VerticalLayoutGroup playLayout = playArea.gameObject.AddComponent<VerticalLayoutGroup>();
        playLayout.padding = new RectOffset(34, 34, 28, 28);
        playLayout.spacing = 74.79f;
        playLayout.childControlWidth = true;
        playLayout.childControlHeight = true;
        playLayout.childForceExpandWidth = true;
        playLayout.childForceExpandHeight = false;

        promptText = CreateText(playArea, "Prompt", string.Empty, 32f, FontStyles.Bold, lightTextColor);
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.textWrappingMode = TextWrappingModes.Normal;
        promptText.gameObject.AddComponent<LayoutElement>().preferredHeight = 150f;

        timerText = CreateText(playArea, "Timer", string.Empty, 22f, FontStyles.Bold, lightTextColor);
        timerText.alignment = TextAlignmentOptions.Center;
        timerText.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

        optionsRoot = CreateRect("Options", playArea);
        LayoutElement optionsLayout = optionsRoot.gameObject.AddComponent<LayoutElement>();
        optionsLayout.minHeight = 190f;
        optionsLayout.preferredHeight = 190f;

        VerticalLayoutGroup optionsGroup = optionsRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        optionsGroup.spacing = 10f;
        optionsGroup.childControlWidth = true;
        optionsGroup.childControlHeight = true;
        optionsGroup.childForceExpandWidth = true;
        optionsGroup.childForceExpandHeight = false;

        tileInstructionText = CreateText(playArea, "TileInstruction", string.Empty, 21f, FontStyles.Bold, lightTextColor);
        tileInstructionText.alignment = TextAlignmentOptions.Center;
        tileInstructionText.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;

        currentWordTilesRoot = CreateTileRow(playArea, "CurrentWordTiles", 62f);
        sourceWordTilesRoot = CreateTileRow(playArea, "SourceWordTiles", 72f);

        answerInput = CreateInputField(playArea, "AnswerInput");

        foundWordsText = CreateText(playArea, "FoundWords", string.Empty, 20f, FontStyles.Normal, lightTextColor);
        foundWordsText.textWrappingMode = TextWrappingModes.Normal;
        foundWordsText.gameObject.AddComponent<LayoutElement>().preferredHeight = 66f;

        feedbackText = CreateText(playArea, "Feedback", string.Empty, 22f, FontStyles.Bold, lightTextColor);
        feedbackText.alignment = TextAlignmentOptions.Center;
        feedbackText.textWrappingMode = TextWrappingModes.Normal;
        feedbackText.gameObject.AddComponent<LayoutElement>().preferredHeight = 58f;

        scoreText = CreateText(playArea, "Score", string.Empty, 20f, FontStyles.Bold, lightTextColor);
        scoreText.alignment = TextAlignmentOptions.Center;
        scoreText.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

        CreateFooter(panel);
        CreatePauseOverlay(playScreen);
        CreateResultOverlay(playScreen);
    }

    private void CreatePauseOverlay(Transform parent)
    {
        pauseOverlay = CreatePanel(parent, "PauseOverlay", new Color(0f, 0f, 0f, 0.66f), Vector2.zero);
        StretchToFillParent(pauseOverlay);

        RectTransform panel = CreatePanel(pauseOverlay, "PauseMenuPanel", new Color(0.84f, 0.70f, 0.38f, 0.98f), new Vector2(620f, 390f));
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = Vector2.zero;

        Outline outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.18f, 0.10f, 0.03f, 0.95f);
        outline.effectDistance = new Vector2(5f, -5f);

        VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(76, 76, 44, 44);
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        TextMeshProUGUI title = CreateText(panel, "PauseMenuTitleText", "Menu", 54f, FontStyles.Bold, textColor);
        title.alignment = TextAlignmentOptions.Center;
        title.gameObject.AddComponent<LayoutElement>().preferredHeight = 82f;

        optionsButton = CreateButton(panel, "OptionsButton", "Options", selectedButtonColor, new Vector2(430f, 58f), ShowOptionsPlaceholder);
        quitMiniGameButton = CreateButton(panel, "QuitMiniGameButton", "Quit Mini-game", selectedButtonColor, new Vector2(430f, 58f), ReturnToModeSelect);
        returnToGameButton = CreateButton(panel, "ReturnToGameButton", "Return To Game", selectedButtonColor, new Vector2(430f, 58f), ClosePauseMenu);

        pauseOverlay.gameObject.SetActive(false);
    }

    private void CreateResultOverlay(Transform parent)
    {
        resultOverlay = CreatePanel(parent, "ResultOverlay", new Color(0f, 0f, 0f, 0.72f), Vector2.zero);
        StretchToFillParent(resultOverlay);

        RectTransform clickCatcher = CreatePanel(resultOverlay, "ResultContinueButton", new Color(0f, 0f, 0f, 0f), Vector2.zero);
        StretchToFillParent(clickCatcher);
        resultContinueButton = clickCatcher.gameObject.AddComponent<Button>();
        resultContinueButton.targetGraphic = clickCatcher.GetComponent<Image>();
        resultContinueButton.onClick.AddListener(ReturnToModeSelect);

        RectTransform panel = CreatePanel(resultOverlay, "ResultPanel", new Color(0.84f, 0.70f, 0.38f, 0.98f), new Vector2(620f, 470f));
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = Vector2.zero;

        Image panelImage = panel.GetComponent<Image>();
        if (panelImage != null)
            panelImage.raycastTarget = false;

        Outline outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.18f, 0.10f, 0.03f, 0.95f);
        outline.effectDistance = new Vector2(5f, -5f);

        VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(54, 54, 38, 34);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        resultTitleText = CreateText(panel, "ResultTitleText", "Time Up", 48f, FontStyles.Bold, textColor);
        resultTitleText.alignment = TextAlignmentOptions.Center;
        resultTitleText.enableAutoSizing = true;
        resultTitleText.fontSizeMin = 26f;
        resultTitleText.fontSizeMax = 48f;
        resultTitleText.overflowMode = TextOverflowModes.Ellipsis;
        resultTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 66f;

        resultScoreText = CreateText(panel, "ResultScoreText", "Score: 0", 30f, FontStyles.Bold, textColor);
        resultScoreText.alignment = TextAlignmentOptions.Center;
        resultScoreText.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;

        resultWordsText = CreateText(panel, "ResultWordsText", "Words: 0", 18f, FontStyles.Bold, textColor);
        resultWordsText.alignment = TextAlignmentOptions.Center;
        resultWordsText.textWrappingMode = TextWrappingModes.Normal;
        resultWordsText.overflowMode = TextOverflowModes.Overflow;
        resultWordsText.lineSpacing = -8f;
        resultWordsText.gameObject.AddComponent<LayoutElement>().preferredHeight = 190f;

        resultContinueText = CreateText(panel, "ResultContinueText", "Click anywhere to continue", 22f, FontStyles.Bold, lightTextColor);
        resultContinueText.alignment = TextAlignmentOptions.Center;
        resultContinueText.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;

        resultOverlay.gameObject.SetActive(false);
    }

    private void CreateFooter(Transform parent)
    {
        RectTransform row = CreateRect("Footer", parent);
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;

        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        CreateButton(row, "NewRoundButton", "New Round", buttonColor, new Vector2(230f, 56f), StartNewRound);
        clearButton = CreateButton(row, "ClearButton", "Clear", buttonColor, new Vector2(170f, 56f), ClearTileSelection);
        CreateButton(row, "SubmitButton", "Submit", selectedButtonColor, new Vector2(220f, 56f), SubmitTypedAnswer);
        CreateButton(row, "ModeSelectButton", "Mode Select", buttonColor, new Vector2(230f, 56f), ReturnToModeSelect);
        CreateButton(row, "BackButton", "Menu", buttonColor, new Vector2(210f, 56f), OpenPauseMenu);
    }

    private TMP_InputField CreateInputField(Transform parent, string objectName)
    {
        RectTransform fieldRect = CreatePanel(parent, objectName, new Color(0.92f, 0.78f, 0.46f, 0.95f), new Vector2(0f, 66f));
        fieldRect.gameObject.AddComponent<LayoutElement>().preferredHeight = 66f;

        TMP_InputField input = fieldRect.gameObject.AddComponent<TMP_InputField>();
        TextMeshProUGUI text = CreateText(fieldRect, "Text", string.Empty, 28f, FontStyles.Bold, textColor);
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(22f, 8f);
        textRect.offsetMax = new Vector2(-22f, -8f);

        TextMeshProUGUI placeholder = CreateText(fieldRect, "Placeholder", "Type answer...", 26f, FontStyles.Italic, new Color(textColor.r, textColor.g, textColor.b, 0.55f));
        RectTransform placeholderRect = placeholder.GetComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = new Vector2(22f, 8f);
        placeholderRect.offsetMax = new Vector2(-22f, -8f);

        input.textComponent = text;
        input.placeholder = placeholder;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.characterValidation = TMP_InputField.CharacterValidation.Alphanumeric;
        input.onSubmit.AddListener(_ => SubmitTypedAnswer());
        return input;
    }

    private RectTransform CreateTileRow(Transform parent, string objectName, float preferredHeight)
    {
        RectTransform row = CreatePanel(parent, objectName, new Color(0.10f, 0.055f, 0.025f, 0.72f), new Vector2(0f, preferredHeight));
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = preferredHeight;

        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 8, 8);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        return row;
    }

    private void SetTilePickerVisible(bool visible)
    {
        if (tileInstructionText != null)
            tileInstructionText.gameObject.SetActive(visible);

        if (currentWordTilesRoot != null)
            currentWordTilesRoot.gameObject.SetActive(visible);

        if (sourceWordTilesRoot != null)
            sourceWordTilesRoot.gameObject.SetActive(visible);
    }

    private void ApplyDefaultPlayLayout()
    {
        ApplyPlayableFooterButtons();

        if (promptText != null)
            promptText.fontSize = 32f;

        if (timerText != null)
            timerText.fontSize = 22f;

        SetPlayAreaSpacing(16f);
        SetOptionRowSpacing(10f);
        SetSourceTileRootLayout(false);
        SetPreferredHeight(promptText, 150f);
        SetPreferredHeight(timerText, 34f);
        SetPreferredHeight(optionsRoot, 190f);
        SetPreferredHeight(tileInstructionText, 28f);
        SetPreferredHeight(currentWordTilesRoot, 62f);
        SetPreferredHeight(sourceWordTilesRoot, 72f);
        SetPreferredHeight(feedbackText, 58f);
        SetPreferredHeight(scoreText, 34f);
    }

    private void ApplySpellItOutPlayLayout()
    {
        ApplyDefaultPlayLayout();

        if (promptText != null)
            promptText.fontSize = 28f;

        if (timerText != null)
        {
            timerText.fontSize = 28f;
            timerText.color = new Color(1f, 0.92f, 0.42f, 1f);
            timerText.alignment = TextAlignmentOptions.Center;
        }

        SetPreferredHeight(promptText, 130f);
        SetPreferredHeight(timerText, 44f);
        SetPreferredHeight(feedbackText, 44f);
        SetPreferredHeight(scoreText, 42f);
    }

    private void ApplyWordMasterPlayLayout()
    {
        ApplyPlayableFooterButtons();

        if (promptText != null)
            promptText.fontSize = 22f;

        SetPlayAreaSpacing(74.79f);
        SetOptionRowSpacing(6f);
        SetSourceTileRootLayout(true);
        SetPreferredHeight(promptText, 30f);
        SetPreferredHeight(optionsRoot, 328f);
        SetPreferredHeight(tileInstructionText, 0f);
        SetPreferredHeight(currentWordTilesRoot, 0f);
        SetPreferredHeight(sourceWordTilesRoot, 124f);
        SetPreferredHeight(feedbackText, 0f);
        SetPreferredHeight(scoreText, 0f);
    }

    private void ApplyWordExcavationPlayLayout()
    {
        ApplyPlayableFooterButtons();

        if (promptText != null)
            promptText.fontSize = 20f;

        SetLayoutHeights(promptText != null ? promptText.transform.parent : null, 865.6f, 560f);
        SetPlayAreaSpacing(10f);
        SetOptionRowSpacing(6f);
        SetSourceTileRootLayout(false);
        SetPreferredHeight(promptText, 28f);
        SetLayoutHeights(optionsRoot, 598.5f, 330f);
        SetPreferredHeight(tileInstructionText, 24f);
        SetPreferredHeight(currentWordTilesRoot, 58f);
        SetPreferredHeight(sourceWordTilesRoot, 72f);
        SetPreferredHeight(feedbackText, 36f);
        SetPreferredHeight(scoreText, 0f);
    }

    private void SetPlayAreaSpacing(float spacing)
    {
        if (promptText == null || promptText.transform.parent == null)
            return;

        VerticalLayoutGroup layout = promptText.transform.parent.GetComponent<VerticalLayoutGroup>();
        if (layout != null)
            layout.spacing = spacing;
    }

    private void SetOptionRowSpacing(float spacing)
    {
        if (optionsRoot == null)
            return;

        VerticalLayoutGroup layout = optionsRoot.GetComponent<VerticalLayoutGroup>();
        if (layout != null)
            layout.spacing = spacing;
    }

    private void SetPreferredHeight(Component component, float height)
    {
        SetLayoutHeights(component, height, height);
    }

    private void SetLayoutHeights(Component component, float minHeight, float preferredHeight)
    {
        if (component == null)
            return;

        LayoutElement layout = component.GetComponent<LayoutElement>();
        if (layout == null)
            return;

        layout.minHeight = minHeight;
        layout.preferredHeight = preferredHeight;
    }

    private void SetObjectActive(Component component, bool active)
    {
        if (component != null)
            component.gameObject.SetActive(active);
    }

    private void ApplyPlayableFooterButtons()
    {
        SetButtonVisible(newRoundButton, false);
        SetButtonVisible(clearButton, false);
        SetButtonVisible(modeSelectButton, false);
        SetButtonVisible(submitButton, true);
        SetButtonVisible(playBackButton, true);
        SetButtonLabel(playBackButton, "Menu");
    }

    private void SetButtonVisible(Button button, bool visible)
    {
        if (button != null)
            button.gameObject.SetActive(visible);
    }

    private void SetButtonLabel(Button button, string label)
    {
        if (button == null)
            return;

        TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text != null)
            text.text = label;
    }

    private void SetSourceTileRootLayout(bool wordMasterLayout)
    {
        if (sourceWordTilesRoot == null)
            return;

        HorizontalLayoutGroup horizontal = sourceWordTilesRoot.GetComponent<HorizontalLayoutGroup>();

        if (wordMasterLayout)
        {
            if (horizontal != null)
                horizontal.enabled = false;
        }
        else
        {
            if (horizontal != null)
            {
                horizontal.enabled = true;
                horizontal.padding = new RectOffset(10, 10, 8, 8);
                horizontal.spacing = 8f;
                horizontal.childAlignment = TextAnchor.MiddleCenter;
            }
        }
    }

    private void ConfigureTilePicker(string sourceLetters)
    {
        currentSourceLetters = string.IsNullOrWhiteSpace(sourceLetters)
            ? new char[0]
            : sourceLetters.ToUpperInvariant().ToCharArray();

        selectedTileIndexes.Clear();
        if (currentMode == MinigameMode.WordMaster)
            wordMasterConsumedLockedInputPositions.Clear();

        RefreshTilePickerVisuals();
    }

    private void ToggleSourceTile(int tileIndex)
    {
        if (tileIndex < 0 || tileIndex >= currentSourceLetters.Length)
        {
            Debug.LogWarning($"[WordMaster] Ignored source tile click. Index {tileIndex} is outside source letters length {currentSourceLetters.Length}.");
            return;
        }

        if (currentMode == MinigameMode.WordMaster)
        {
            if (wordMasterSolved)
            {
                SetFeedback("Solved. Start a new round.", goodColor);
                return;
            }

            if (wordMasterTurn >= wordMasterMaxTurns)
            {
                SetFeedback("Game over. Try again.", badColor);
                return;
            }

            if (TryConsumeWordMasterLockedLetterClick(currentSourceLetters[tileIndex]))
                return;

            int blankIndex = selectedTileIndexes.IndexOf(-1);
            if (blankIndex >= 0)
            {
                selectedTileIndexes[blankIndex] = tileIndex;
                RefreshTilePickerVisuals();
                Debug.Log($"[WordMaster] Guess now: '{GetWordMasterCurrentGuess()}'.");
                return;
            }

            if (selectedTileIndexes.Count >= GetWordMasterSelectionLimit())
            {
                SetFeedback("All open slots are filled.", badColor);
                return;
            }

            selectedTileIndexes.Add(tileIndex);
            RefreshTilePickerVisuals();
            Debug.Log($"[WordMaster] Guess now: '{GetWordMasterCurrentGuess()}'.");
            return;
        }

        if (selectedTileIndexes.Contains(tileIndex))
            selectedTileIndexes.Remove(tileIndex);
        else
            selectedTileIndexes.Add(tileIndex);

        RefreshTilePickerVisuals();
    }

    private void RemoveSelectedTileAt(int selectedIndex)
    {
        if (selectedIndex < 0 || selectedIndex >= selectedTileIndexes.Count)
            return;

        selectedTileIndexes.RemoveAt(selectedIndex);
        RefreshTilePickerVisuals();
    }

    private void TrimSelectedTilesFrom(int selectedIndex)
    {
        if (selectedIndex < 0 || selectedIndex >= selectedTileIndexes.Count)
            return;

        selectedTileIndexes.RemoveRange(selectedIndex, selectedTileIndexes.Count - selectedIndex);
        RefreshTilePickerVisuals();
    }

    private string GetSelectedTileWord()
    {
        StringBuilder builder = new StringBuilder();
        foreach (int tileIndex in selectedTileIndexes)
        {
            if (tileIndex < 0 || tileIndex >= currentSourceLetters.Length)
                continue;

            builder.Append(currentSourceLetters[tileIndex]);
        }

        return builder.ToString();
    }

    private string GetWordMasterCurrentGuess()
    {
        StringBuilder builder = new StringBuilder();
        int selectedCursor = 0;

        for (int i = 0; i < wordMasterWordLength; i++)
        {
            if (IsWordMasterLockedPosition(i))
            {
                builder.Append(wordMasterLockedLetters[i]);
                continue;
            }

            if (selectedCursor >= selectedTileIndexes.Count)
            {
                builder.Append(' ');
                continue;
            }

            int sourceIndex = selectedTileIndexes[selectedCursor];
            selectedCursor++;

            if (sourceIndex < 0 || sourceIndex >= currentSourceLetters.Length)
            {
                builder.Append(' ');
                continue;
            }

            builder.Append(currentSourceLetters[sourceIndex]);
        }

        return builder.ToString();
    }

    private int GetWordMasterSelectionLimit()
    {
        return CountWordMasterUnlockedPositions();
    }

    private void ResetWordMasterLockedLetters()
    {
        wordMasterLockedLetters = new char[Mathf.Max(0, wordMasterWordLength)];

        if (!wordMasterUseStartHint
            || string.IsNullOrWhiteSpace(wordMasterTargetWord)
            || wordMasterHintIndex < 0
            || wordMasterHintIndex >= wordMasterLockedLetters.Length
            || wordMasterHintIndex >= wordMasterTargetWord.Length)
        {
            return;
        }

        wordMasterLockedLetters[wordMasterHintIndex] = wordMasterTargetWord[wordMasterHintIndex];
    }

    private void LockWordMasterCorrectLetters(string guess, LetterClue[] clues)
    {
        EnsureWordMasterLockedLetters();

        for (int i = 0; i < guess.Length && i < clues.Length && i < wordMasterLockedLetters.Length; i++)
        {
            if (clues[i] == LetterClue.Correct)
                wordMasterLockedLetters[i] = wordMasterTargetWord[i];
        }
    }

    private void EnsureWordMasterLockedLetters()
    {
        if (wordMasterLockedLetters != null && wordMasterLockedLetters.Length == wordMasterWordLength)
            return;

        ResetWordMasterLockedLetters();
    }

    private bool IsWordMasterLockedPosition(int guessPosition)
    {
        return wordMasterLockedLetters != null
            && guessPosition >= 0
            && guessPosition < wordMasterLockedLetters.Length
            && wordMasterLockedLetters[guessPosition] != '\0';
    }

    private int CountWordMasterUnlockedPositions()
    {
        EnsureWordMasterLockedLetters();

        int count = 0;
        for (int i = 0; i < wordMasterWordLength; i++)
        {
            if (!IsWordMasterLockedPosition(i))
                count++;
        }

        return count;
    }

    private bool TryConsumeWordMasterLockedLetterClick(char clickedLetter)
    {
        EnsureWordMasterLockedLetters();

        for (int i = 0; i < wordMasterWordLength; i++)
        {
            if (!IsWordMasterLockedPosition(i)
                || wordMasterConsumedLockedInputPositions.Contains(i)
                || wordMasterLockedLetters[i] != clickedLetter
                || !AreWordMasterOpenSlotsBeforeFilled(i))
            {
                continue;
            }

            wordMasterConsumedLockedInputPositions.Add(i);
            return true;
        }

        return false;
    }

    private bool AreWordMasterOpenSlotsBeforeFilled(int guessPosition)
    {
        int unlockedSlotIndex = 0;

        for (int i = 0; i < guessPosition; i++)
        {
            if (IsWordMasterLockedPosition(i))
                continue;

            if (unlockedSlotIndex >= selectedTileIndexes.Count || selectedTileIndexes[unlockedSlotIndex] < 0)
                return false;

            unlockedSlotIndex++;
        }

        return true;
    }

    private bool IsWordMasterFullyLocked()
    {
        return CountWordMasterUnlockedPositions() == 0;
    }

    private string BuildWordMasterGuessSummary(string guess, LetterClue[] clues)
    {
        List<char> gold = new List<char>();
        List<char> silver = new List<char>();
        List<char> missing = new List<char>();

        for (int i = 0; i < guess.Length && i < clues.Length; i++)
        {
            switch (clues[i])
            {
                case LetterClue.Correct:
                    gold.Add(guess[i]);
                    break;
                case LetterClue.Present:
                    silver.Add(guess[i]);
                    break;
                default:
                    missing.Add(guess[i]);
                    break;
            }
        }

        List<string> parts = new List<string>();
        if (gold.Count > 0)
            parts.Add("Gold locked: " + string.Join(", ", gold));

        if (silver.Count > 0)
            parts.Add("Silver: " + string.Join(", ", silver));

        if (missing.Count > 0)
            parts.Add("Missing: " + string.Join(", ", missing));

        return parts.Count > 0 ? string.Join(" | ", parts) : "Keep going.";
    }

    private bool ShouldUseWordMasterStartHint()
    {
        return wordMasterUseStartHint
            && wordMasterTurn == 0
            && !wordMasterSolved
            && wordMasterHintIndex >= 0
            && wordMasterHintIndex < wordMasterTargetWord.Length
            && wordMasterHintIndex < wordMasterWordLength;
    }

    private bool ShouldShowWordMasterHintPanel()
    {
        return ShouldUseWordMasterStartHint() && !wordMasterHintPanelDismissed;
    }

    private bool IsWordMasterHintPosition(int guessPosition)
    {
        return IsWordMasterLockedPosition(guessPosition);
    }

    private void RemoveWordMasterLettersFromGuessPosition(int guessPosition)
    {
        int selectedIndex = GetWordMasterSelectedIndexForGuessPosition(guessPosition);
        if (selectedIndex < 0 || selectedIndex >= selectedTileIndexes.Count)
            return;

        selectedTileIndexes[selectedIndex] = -1;
        RefreshTilePickerVisuals();
    }

    private int GetWordMasterSelectedIndexForGuessPosition(int guessPosition)
    {
        if (IsWordMasterLockedPosition(guessPosition))
            return -1;

        int selectedIndex = 0;
        for (int i = 0; i < guessPosition; i++)
        {
            if (!IsWordMasterLockedPosition(i))
                selectedIndex++;
        }

        return selectedIndex;
    }

    private void RefreshTilePickerVisuals()
    {
        RebuildCurrentWordTiles();
        RebuildSourceWordTiles();
    }

    private void RebuildCurrentWordTiles()
    {
        if (currentWordTilesRoot == null)
            return;

        if (currentMode == MinigameMode.WordMaster)
        {
            RebuildWordMasterRows();
            return;
        }

        ClearChildren(currentWordTilesRoot);

        if (selectedTileIndexes.Count == 0)
        {
            string placeholderText = currentMode == MinigameMode.WordMaster ? "Build guess" : "Current word";
            TextMeshProUGUI placeholder = CreateText(currentWordTilesRoot, "EmptyText", placeholderText, 22f, FontStyles.Italic, new Color(lightTextColor.r, lightTextColor.g, lightTextColor.b, 0.6f));
            placeholder.alignment = TextAlignmentOptions.Center;
            placeholder.gameObject.AddComponent<LayoutElement>().preferredWidth = 240f;
            return;
        }

        for (int i = 0; i < selectedTileIndexes.Count; i++)
        {
            int sourceIndex = selectedTileIndexes[i];
            if (sourceIndex < 0 || sourceIndex >= currentSourceLetters.Length)
                continue;

            int capturedIndex = i;
            if (currentMode == MinigameMode.WordExcavation)
                CreateLetterTile(currentWordTilesRoot, currentSourceLetters[sourceIndex], selectedButtonColor, () => TrimSelectedTilesFrom(capturedIndex));
            else
                CreateLetterTile(currentWordTilesRoot, currentSourceLetters[sourceIndex], selectedButtonColor, () => RemoveSelectedTileAt(capturedIndex));
        }
    }

    private void RebuildSourceWordTiles()
    {
        if (sourceWordTilesRoot == null)
            return;

        if (currentMode == MinigameMode.WordMaster)
        {
            RebuildWordMasterKeyboard();
            return;
        }

        ClearChildren(sourceWordTilesRoot);

        for (int i = 0; i < currentSourceLetters.Length; i++)
        {
            int capturedIndex = i;
            bool selected = currentMode != MinigameMode.WordMaster && selectedTileIndexes.Contains(i);
            Color tileColor = new Color(0.93f, 0.84f, 0.56f, 0.98f);

            if (currentMode == MinigameMode.WordMaster
                && wordMasterKeyboardColors.TryGetValue(currentSourceLetters[i], out Color clueColor))
            {
                tileColor = clueColor;
            }
            else if (selected)
            {
                tileColor = new Color(0.30f, 0.25f, 0.18f, 0.86f);
            }

            Button button = CreateLetterTile(sourceWordTilesRoot, currentSourceLetters[i], tileColor, () => ToggleSourceTile(capturedIndex));
            TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                bool darkWordMasterTile = currentMode == MinigameMode.WordMaster
                    && wordMasterKeyboardColors.ContainsKey(currentSourceLetters[i])
                    && ColorsApproximatelyEqual(tileColor, wordMasterMissingColor);

                text.color = selected
                    ? new Color(0.08f, 0.06f, 0.04f, 0.48f)
                    : darkWordMasterTile ? lightTextColor : textColor;
            }
        }
    }

    private void RebuildWordMasterKeyboard()
    {
        if (RefreshExistingWordMasterKeyboard())
            return;

        ClearChildren(sourceWordTilesRoot);

        CreateWordMasterKeyboardRow(0, 13, 24f);
        CreateWordMasterKeyboardRow(13, 13, -26f);
    }

    private bool RefreshExistingWordMasterKeyboard()
    {
        if (sourceWordTilesRoot == null || currentSourceLetters.Length == 0)
            return false;

        List<Button> buttons = new List<Button>();
        for (int i = 0; i < currentSourceLetters.Length; i++)
        {
            Button button = FindButton(sourceWordTilesRoot, "LetterTile_" + currentSourceLetters[i]);
            if (button == null)
                return false;

            buttons.Add(button);
        }

        for (int i = 0; i < buttons.Count; i++)
        {
            Button button = buttons[i];
            char letter = currentSourceLetters[i];
            Color tileColor = wordMasterKeyboardColors.TryGetValue(letter, out Color clueColor)
                ? clueColor
                : new Color(0.93f, 0.84f, 0.56f, 0.98f);

            Image image = button.targetGraphic as Image;
            if (image == null)
                image = button.GetComponent<Image>();

            if (image != null)
                image.color = tileColor;

            TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                bool missingLetter = wordMasterKeyboardColors.ContainsKey(letter)
                    && ColorsApproximatelyEqual(tileColor, wordMasterMissingColor);

                text.text = letter.ToString();
                text.color = missingLetter ? lightTextColor : textColor;
            }

            int capturedIndex = i;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectSourceTile(capturedIndex));
        }

        return true;
    }

    private void CreateWordMasterKeyboardRow(int startIndex, int count, float anchoredY)
    {
        RectTransform row = CreateRect("KeyboardRow" + startIndex, sourceWordTilesRoot);
        row.sizeDelta = new Vector2(840f, 46f);
        row.anchorMin = new Vector2(0.5f, 0.5f);
        row.anchorMax = new Vector2(0.5f, 0.5f);
        row.pivot = new Vector2(0.5f, 0.5f);
        row.anchoredPosition = new Vector2(0f, anchoredY);

        LayoutElement rowLayout = row.gameObject.AddComponent<LayoutElement>();
        rowLayout.minWidth = 840f;
        rowLayout.preferredWidth = 840f;
        rowLayout.minHeight = 46f;
        rowLayout.preferredHeight = 46f;

        HorizontalLayoutGroup rowGroup = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowGroup.spacing = 5f;
        rowGroup.childAlignment = TextAnchor.MiddleCenter;
        rowGroup.childControlWidth = false;
        rowGroup.childControlHeight = false;
        rowGroup.childForceExpandWidth = false;
        rowGroup.childForceExpandHeight = false;

        int endIndex = Mathf.Min(startIndex + count, currentSourceLetters.Length);
        for (int i = startIndex; i < endIndex; i++)
        {
            int capturedIndex = i;
            char letter = currentSourceLetters[i];
            Color tileColor = wordMasterKeyboardColors.TryGetValue(letter, out Color clueColor)
                ? clueColor
                : new Color(0.93f, 0.84f, 0.56f, 0.98f);

            Button button = CreateLetterTile(row, letter, tileColor, () => SelectSourceTile(capturedIndex));
            TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                bool missingLetter = wordMasterKeyboardColors.ContainsKey(letter)
                    && ColorsApproximatelyEqual(tileColor, wordMasterMissingColor);

                text.color = missingLetter ? lightTextColor : textColor;
            }
        }
    }

    private Button CreateLetterTile(Transform parent, char letter, Color color, UnityEngine.Events.UnityAction action)
    {
        bool wordMasterKeyboardTile = currentMode == MinigameMode.WordMaster
            && sourceWordTilesRoot != null
            && parent.IsChildOf(sourceWordTilesRoot);

        Vector2 tileSize = wordMasterKeyboardTile ? new Vector2(58f, 44f) : new Vector2(58f, 50f);
        Button button = CreateButton(parent, "LetterTile_" + letter, letter.ToString(), color, tileSize, action);
        TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.fontSize = wordMasterKeyboardTile ? 30f : 30f;
            text.color = textColor;

            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.offsetMin = wordMasterKeyboardTile ? new Vector2(2f, 3f) : new Vector2(8f, 4f);
            textRect.offsetMax = wordMasterKeyboardTile ? new Vector2(-2f, -3f) : new Vector2(-8f, -4f);
        }

        return button;
    }

    private Color GetWordMasterTileTextColor(Color color)
    {
        return ColorsApproximatelyEqual(color, wordMasterMissingColor)
            ? lightTextColor
            : textColor;
    }

    private TextMeshProUGUI CreateLetterTileDisplay(Transform parent, char letter, Color color, Vector2 size, UnityEngine.Events.UnityAction clickAction = null, string objectName = null)
    {
        RectTransform rect = CreatePanel(parent, objectName ?? "LetterDisplay_" + (letter == ' ' ? "Empty" : letter.ToString()), color, size);
        LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
        layout.minWidth = size.x;
        layout.preferredWidth = size.x;
        layout.minHeight = size.y;
        layout.preferredHeight = size.y;

        if (clickAction != null)
        {
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            button.onClick.AddListener(clickAction);
        }

        Color labelColor = GetWordMasterTileTextColor(color);
        TextMeshProUGUI text = CreateText(rect, "Text", letter == ' ' ? string.Empty : letter.ToString(), 24f, FontStyles.Bold, labelColor);
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(2f, 1f);
        textRect.offsetMax = new Vector2(-2f, -1f);
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 12f;
        text.fontSizeMax = size.x >= 60f ? 30f : 24f;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    private void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            GameObject child = parent.GetChild(i).gameObject;
            child.SetActive(false);
            DestroySafe(child);
        }
    }

    private void CreateOptionButton(string label, UnityEngine.Events.UnityAction action)
    {
        Button button = CreateButton(optionsRoot, "OptionButton", label, buttonColor, new Vector2(0f, 42f), action);
        button.GetComponent<LayoutElement>().preferredHeight = 42f;
        optionButtons.Add(button);
    }

    private Button CreateButton(Transform parent, string objectName, string label, Color color, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        RectTransform rect = CreatePanel(parent, objectName, color, size);
        LayoutElement layoutElement = rect.gameObject.AddComponent<LayoutElement>();
        layoutElement.minWidth = size.x;
        layoutElement.preferredWidth = size.x;
        layoutElement.minHeight = size.y;
        layoutElement.preferredHeight = size.y;

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        if (action != null)
            button.onClick.AddListener(action);

        TextMeshProUGUI text = CreateText(rect, "Text", label, 22f, FontStyles.Bold, lightTextColor);
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(16f, 5f);
        textRect.offsetMax = new Vector2(-16f, -5f);
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.Normal;

        return button;
    }

    private RectTransform CreatePanel(Transform parent, string objectName, Color color, Vector2 size)
    {
        RectTransform rect = CreateRect(objectName, parent);
        rect.sizeDelta = size;
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
        return rect;
    }

    private void StretchToFillParent(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }

    private RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject obj = new GameObject(objectName, typeof(RectTransform));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private RectTransform FindRect(Transform parent, string objectName)
    {
        return FindComponent<RectTransform>(parent, objectName);
    }

    private Button FindButton(Transform parent, string objectName)
    {
        return FindComponent<Button>(parent, objectName);
    }

    private TextMeshProUGUI FindText(Transform parent, string objectName)
    {
        return FindComponent<TextMeshProUGUI>(parent, objectName);
    }

    private T FindComponent<T>(Transform parent, string objectName) where T : Component
    {
        if (parent == null)
            return null;

        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name != objectName)
                continue;

            T component = child.GetComponent<T>();
            if (component != null)
                return component;
        }

        return null;
    }

    private void DestroySafe(GameObject target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }

    private TextMeshProUGUI CreateText(Transform parent, string objectName, string text, float fontSize, FontStyles fontStyle, Color color)
    {
        RectTransform rect = CreateRect(objectName, parent);
        TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = fontStyle;
        tmp.color = color;
        tmp.raycastTarget = false;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        return tmp;
    }

    private PracticeEntry GetRandomDefinedEntry(int minLength, int maxLength)
    {
        if (dictionary != null)
        {
            for (int i = 0; i < 80; i++)
            {
                string word = dictionary.GetRandomWordByLengthRange(minLength, maxLength);
                string definition = dictionary.GetDefinition(word);
                if (!string.IsNullOrWhiteSpace(word) && IsUsableDefinition(definition))
                    return new PracticeEntry(word.ToUpperInvariant(), definition);
            }
        }

        List<PracticeEntry> candidates = new List<PracticeEntry>();
        foreach (PracticeEntry entry in FallbackEntries)
        {
            if (entry.word.Length >= minLength && entry.word.Length <= maxLength)
                candidates.Add(entry);
        }

        if (candidates.Count == 0)
            candidates.AddRange(FallbackEntries);

        return candidates[Random.Range(0, candidates.Count)];
    }

    private PracticeEntry GetRandomSpellItOutEntry(int minLength, int maxLength)
    {
        List<PracticeEntry> curatedCandidates = GetSpellItOutCuratedCandidates(minLength, maxLength);
        if (curatedCandidates.Count > 0)
            return curatedCandidates[Random.Range(0, curatedCandidates.Count)];

        PracticeEntry bestEntry = default;
        int bestScore = int.MaxValue;
        bool hasBestEntry = false;

        if (dictionary != null)
        {
            for (int i = 0; i < SpellItOutCandidateAttempts; i++)
            {
                string word = dictionary.GetRandomWordByLengthRange(minLength, maxLength);
                if (string.IsNullOrWhiteSpace(word))
                    continue;

                word = word.Trim().ToUpperInvariant();
                string rawDefinition = dictionary.GetDefinition(word);
                if (!TryGetCleanSpellItOutDefinition(word, rawDefinition, out string cleanDefinition, out int score))
                    continue;

                if (score >= bestScore)
                    continue;

                bestEntry = new PracticeEntry(word, cleanDefinition);
                bestScore = score;
                hasBestEntry = true;
            }
        }

        if (hasBestEntry)
            return bestEntry;

        List<PracticeEntry> fallbackCandidates = new List<PracticeEntry>();
        foreach (PracticeEntry entry in SpellItOutFallbackEntries)
        {
            if (entry.word.Length >= minLength && entry.word.Length <= maxLength)
                fallbackCandidates.Add(entry);
        }

        if (fallbackCandidates.Count == 0)
            fallbackCandidates.AddRange(SpellItOutFallbackEntries);

        return fallbackCandidates[Random.Range(0, fallbackCandidates.Count)];
    }

    private List<PracticeEntry> GetSpellItOutCuratedCandidates(int minLength, int maxLength)
    {
        EnsureSpellItOutCuratedEntriesLoaded();

        List<PracticeEntry> candidates = new List<PracticeEntry>();
        foreach (PracticeEntry entry in spellItOutCuratedEntries)
        {
            if (entry.word.Length >= minLength && entry.word.Length <= maxLength)
                candidates.Add(entry);
        }

        return candidates;
    }

    private void EnsureSpellItOutCuratedEntriesLoaded()
    {
        if (spellItOutCuratedEntriesLoaded)
            return;

        spellItOutCuratedEntriesLoaded = true;
        spellItOutCuratedEntries.Clear();

        TextAsset source = spellItOutDictionaryFile;
        if (source == null && !string.IsNullOrWhiteSpace(spellItOutResourceName))
            source = Resources.Load<TextAsset>(spellItOutResourceName);

        if (source == null || string.IsNullOrWhiteSpace(source.text))
            return;

        SpellItOutDictionaryWrapper wrapper = JsonUtility.FromJson<SpellItOutDictionaryWrapper>(source.text);
        if (wrapper?.entries == null)
            return;

        foreach (SpellItOutDictionaryEntry entry in wrapper.entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.word))
                continue;

            string word = entry.word.Trim().ToUpperInvariant();
            string definition = entry.definition;
            if (!TryGetCleanSpellItOutDefinition(word, definition, out string cleanDefinition, out _))
                continue;

            spellItOutCuratedEntries.Add(new PracticeEntry(word, cleanDefinition));
        }
    }

    private bool TryGetCleanSpellItOutDefinition(string word, string rawDefinition, out string cleanDefinition, out int score)
    {
        cleanDefinition = string.Empty;
        score = int.MaxValue;

        if (string.IsNullOrWhiteSpace(word))
            return false;

        string upperWord = word.Trim().ToUpperInvariant();
        if (SpellItOutDefinitionOverrides.TryGetValue(upperWord, out string overrideDefinition))
        {
            cleanDefinition = NormalizeDefinitionText(overrideDefinition);
            score = 0;
            return true;
        }

        cleanDefinition = NormalizeDefinitionText(rawDefinition);
        if (string.IsNullOrWhiteSpace(cleanDefinition))
            return false;

        if (!IsUsableDefinition(cleanDefinition))
            return false;

        if (cleanDefinition.Length > SpellItOutDefinitionMaxLength)
            return false;

        if (cleanDefinition.Contains(";"))
            return false;

        if (cleanDefinition.Contains("--") || cleanDefinition.Contains("—"))
            return false;

        if (IsDictionaryRedirectDefinition(cleanDefinition))
            return false;

        if (ContainsForbiddenSpellItOutTerm(cleanDefinition))
            return false;

        if (ContainsAnswerOrCloseInflection(upperWord, cleanDefinition))
            return false;

        score = cleanDefinition.Length;
        if (cleanDefinition.Contains(","))
            score += 12;

        if (CountSentenceEnders(cleanDefinition) > 1)
            score += 30;

        return true;
    }

    private string NormalizeDefinitionText(string definition)
    {
        if (string.IsNullOrWhiteSpace(definition))
            return string.Empty;

        string[] parts = definition.Trim().Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", parts);
    }

    private bool ContainsForbiddenSpellItOutTerm(string definition)
    {
        if (string.IsNullOrWhiteSpace(definition))
            return true;

        foreach (string term in SpellItOutForbiddenDefinitionTerms)
        {
            if (definition.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        string lower = definition.ToLowerInvariant();
        return lower.StartsWith("l.") || lower.Contains(" l.");
    }

    private bool IsDictionaryRedirectDefinition(string definition)
    {
        if (string.IsNullOrWhiteSpace(definition))
            return true;

        string lower = definition.Trim().ToLowerInvariant();
        return lower.StartsWith("see ")
            || lower.StartsWith("see also ")
            || lower.StartsWith("same as ")
            || lower.StartsWith("variant of ")
            || lower.StartsWith("alternative form of ")
            || lower.StartsWith("alternate form of ")
            || lower.StartsWith("plural of ")
            || lower.StartsWith("past tense of ")
            || lower.StartsWith("present participle of ")
            || lower.StartsWith("a form of ")
            || lower.StartsWith("form of ")
            || lower.StartsWith("archaic form of ")
            || lower.StartsWith("obsolete form of ");
    }

    private bool ContainsAnswerOrCloseInflection(string word, string definition)
    {
        HashSet<string> variants = BuildAnswerLeakVariants(word);
        foreach (string token in TokenizeDefinitionWords(definition))
        {
            if (variants.Contains(token))
                return true;
        }

        return false;
    }

    private HashSet<string> BuildAnswerLeakVariants(string word)
    {
        string lowerWord = word.Trim().ToLowerInvariant();
        HashSet<string> variants = new HashSet<string>();
        if (string.IsNullOrWhiteSpace(lowerWord))
            return variants;

        variants.Add(lowerWord);
        variants.Add(lowerWord + "s");
        variants.Add(lowerWord + "es");
        variants.Add(lowerWord + "ed");
        variants.Add(lowerWord + "ing");
        variants.Add(lowerWord + "ly");

        if (lowerWord.Length > 4 && lowerWord.EndsWith("s"))
            variants.Add(lowerWord.Substring(0, lowerWord.Length - 1));

        if (lowerWord.Length > 5 && lowerWord.EndsWith("es"))
            variants.Add(lowerWord.Substring(0, lowerWord.Length - 2));

        if (lowerWord.Length > 4 && lowerWord.EndsWith("e"))
        {
            variants.Add(lowerWord.Substring(0, lowerWord.Length - 1));
            variants.Add(lowerWord.Substring(0, lowerWord.Length - 1) + "ing");
        }

        if (lowerWord.Length > 4 && lowerWord.EndsWith("y"))
        {
            string stem = lowerWord.Substring(0, lowerWord.Length - 1);
            variants.Add(stem);
            variants.Add(stem + "ies");
            variants.Add(stem + "ily");
            variants.Add(stem + "ly");
        }

        if (lowerWord.Length > 6 && lowerWord.EndsWith("ing"))
            variants.Add(lowerWord.Substring(0, lowerWord.Length - 3));

        if (lowerWord.Length > 5 && lowerWord.EndsWith("ed"))
            variants.Add(lowerWord.Substring(0, lowerWord.Length - 2));

        return variants;
    }

    private List<string> TokenizeDefinitionWords(string definition)
    {
        List<string> tokens = new List<string>();
        if (string.IsNullOrWhiteSpace(definition))
            return tokens;

        StringBuilder builder = new StringBuilder();
        foreach (char c in definition.ToLowerInvariant())
        {
            if (char.IsLetter(c))
            {
                builder.Append(c);
                continue;
            }

            if (builder.Length == 0)
                continue;

            tokens.Add(builder.ToString());
            builder.Clear();
        }

        if (builder.Length > 0)
            tokens.Add(builder.ToString());

        return tokens;
    }

    private int CountSentenceEnders(string text)
    {
        int count = 0;
        foreach (char c in text)
        {
            if (c == '.' || c == '?' || c == '!')
                count++;
        }

        return count;
    }

    private bool IsUsableDefinition(string definition)
    {
        if (string.IsNullOrWhiteSpace(definition))
            return false;

        return !definition.Contains("unavailable");
    }

    private bool IsFallbackValidWord(string word)
    {
        foreach (PracticeEntry entry in FallbackEntries)
        {
            if (entry.word == word)
                return true;
        }

        return false;
    }

    private bool ContainsOptionDefinition(List<PracticeEntry> entries, string definition)
    {
        foreach (PracticeEntry entry in entries)
        {
            if (entry.definition == definition)
                return true;
        }

        return false;
    }

    private bool CanBuildWordFromSource(string word, string source)
    {
        Dictionary<char, int> available = new Dictionary<char, int>();
        foreach (char c in source)
        {
            if (!available.ContainsKey(c))
                available[c] = 0;

            available[c]++;
        }

        foreach (char c in word)
        {
            if (!available.ContainsKey(c) || available[c] <= 0)
                return false;

            available[c]--;
        }

        return true;
    }

    private string BuildLetterHint(string word)
    {
        if (string.IsNullOrEmpty(word))
            return string.Empty;

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < word.Length; i++)
        {
            if (i > 0)
                builder.Append(' ');

            builder.Append('_');
        }

        return builder.ToString();
    }

    private string GetShuffledLetters(string word)
    {
        if (string.IsNullOrWhiteSpace(word) || word.Length <= 1)
            return word;

        List<char> letters = new List<char>(word.ToUpperInvariant().ToCharArray());
        string original = new string(letters.ToArray());

        for (int attempt = 0; attempt < 6; attempt++)
        {
            Shuffle(letters);
            string shuffled = new string(letters.ToArray());
            if (shuffled != original)
                return shuffled;
        }

        return new string(letters.ToArray());
    }

    private void SetTitle(string title)
    {
        if (titleText != null)
            titleText.text = title;
    }

    private void SetFeedback(string message, Color color)
    {
        if (feedbackText == null)
            return;

        feedbackText.text = message;
        feedbackText.color = color;
    }

    private void RefreshScoreText()
    {
        if (scoreText == null)
            return;

        if (currentMode == MinigameMode.WordMaster)
        {
            int displayTurn = wordMasterSolved || wordMasterTurn >= wordMasterMaxTurns
                ? wordMasterTurn
                : wordMasterTurn + 1;

            scoreText.text = $"Wins: {score} | Turn: {Mathf.Clamp(displayTurn, 1, wordMasterMaxTurns)}/{wordMasterMaxTurns}";

            if (wordMasterStatsText != null)
                wordMasterStatsText.text = $"Score\n{score}\n\nWords\n{wordMasterGuesses.Count}";
        }
        else if (currentMode == MinigameMode.WordExcavation)
        {
            scoreText.text = $"Score: {score} | Found: {foundExcavationWords.Count}";
        }
        else if (currentMode == MinigameMode.SpellItOut)
        {
            int totalWords = Mathf.Max(1, spellItOutTotalWords);
            int displayWord = spellItOutActive
                ? Mathf.Clamp(spellItOutWordIndex + 1, 1, totalWords)
                : Mathf.Clamp(roundsPlayed, 0, totalWords);
            scoreText.text = $"Score: {score}/{totalWords} | Word: {displayWord}/{totalWords}";
        }
        else
        {
            scoreText.text = $"Score: {score}/{Mathf.Max(1, roundsPlayed)}";
        }
    }

    private void UpdateTimerText()
    {
        if (timerText != null)
        {
            if (currentMode == MinigameMode.SpellItOut)
                timerText.text = $"Time: {Mathf.CeilToInt(spellItOutTimer)}s";
            else
                timerText.text = $"Time: {Mathf.CeilToInt(excavationTimer)}s";
        }

        if (currentMode == MinigameMode.WordExcavation)
            UpdateWordExcavationTimerMeter();
    }

    private void UpdateFoundWordsText()
    {
        if (foundWordsText == null)
            return;

        if (foundExcavationWords.Count == 0)
        {
            foundWordsText.text = "Found words: none yet";
            UpdateWordExcavationFoundWordsText();
            return;
        }

        foundWordsText.text = "Found words: " + string.Join(", ", foundExcavationWordsInOrder);
        UpdateWordExcavationFoundWordsText();
        UpdateWordExcavationPrizeProgress();
    }

    private void ClearOptionButtons()
    {
        foreach (Button button in optionButtons)
        {
            if (button != null)
                Destroy(button.gameObject);
        }

        optionButtons.Clear();
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }
    }
}
