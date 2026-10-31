using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using LetterClue = WordMasterGuessRules.Clue;

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

public partial class MinigamesSceneController : MonoBehaviour
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
    // Owned by plain C# models. The scene-facing partial MonoBehaviour is a
    // compatibility adapter for existing serialized Unity scenes and UI.
    private readonly MinigameTileSelection tileSelection = new MinigameTileSelection();
    private readonly WordMasterSession wordMasterSession = new WordMasterSession();
    private readonly SpellItOutSession spellingSession = new SpellItOutSession();
    private readonly WordExcavationSession excavationSession = new WordExcavationSession();

    private List<int> selectedTileIndexes => tileSelection.SelectedIndices;
    private HashSet<int> wordMasterConsumedLockedInputPositions => wordMasterSession.ConsumedHintPositions;
    private readonly List<PracticeEntry> spellItOutCuratedEntries = new List<PracticeEntry>();
    private List<string> spellItOutSessionWords => spellingSession.Words;
    private List<bool> spellItOutSessionCorrect => spellingSession.Correct;
    private char[] currentSourceLetters { get => tileSelection.SourceLetters; set => tileSelection.SourceLetters = value; }
    private char[] wordMasterLockedLetters { get => wordMasterSession.LockedLetters; set => wordMasterSession.LockedLetters = value; }
    private int lastSourceTileClickFrame = -1;
    private int lastSourceTileClickIndex = -1;

    private string currentWord { get => spellingSession.Word; set => spellingSession.Word = value; }
    private string currentDefinition { get => spellingSession.Definition; set => spellingSession.Definition = value; }
    private string wordMasterTargetWord { get => wordMasterSession.TargetWord; set => wordMasterSession.TargetWord = value; }
    private string excavationSourceWord { get => excavationSession.SourceWord; set => excavationSession.SourceWord = value; }
    private float excavationTimer { get => excavationSession.SecondsLeft; set => excavationSession.SecondsLeft = value; }
    private int score;
    private int roundsPlayed;
    private int wordMasterTurn { get => wordMasterSession.Turn; set => wordMasterSession.Turn = value; }
    private int wordMasterHintIndex { get => wordMasterSession.HintIndex; set => wordMasterSession.HintIndex = value; }
    private int spellItOutWordIndex { get => spellingSession.WordIndex; set => spellingSession.WordIndex = value; }
    private float spellItOutTimer { get => spellingSession.SecondsLeft; set => spellingSession.SecondsLeft = value; }
    private bool wordMasterSolved { get => wordMasterSession.Solved; set => wordMasterSession.Solved = value; }
    private bool wordMasterHintPanelDismissed { get => wordMasterSession.HintDismissed; set => wordMasterSession.HintDismissed = value; }
    private bool spellItOutActive { get => spellingSession.Active; set => spellingSession.Active = value; }
    private bool excavationActive { get => excavationSession.Active; set => excavationSession.Active = value; }
    private bool minigamePaused;
    private bool resultOverlayShowing;
    private bool spellItOutCuratedEntriesLoaded;
    private HashSet<string> foundExcavationWords => excavationSession.FoundWords;
    private List<string> foundExcavationWordsInOrder => excavationSession.FoundWordsInOrder;
    private List<string> wordMasterGuesses => wordMasterSession.Guesses;
    private List<LetterClue[]> wordMasterGuessClues => wordMasterSession.GuessClues;
    private readonly Dictionary<char, Color> wordMasterKeyboardColors = new Dictionary<char, Color>();

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

}
