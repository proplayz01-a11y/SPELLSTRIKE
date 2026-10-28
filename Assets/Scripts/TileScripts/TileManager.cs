using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TileManager : MonoBehaviour, ITilePoolModeProvider
{
    public static TileManager Instance { get; private set; }

    public event System.Action<string> WordChanged;
    public event System.Action<Tile> TileSelectionRejected;
    public event System.Action<VocabularyWordData, string> TargetChallengeCastStarted;
    public event System.Action<VocabularyWordData, string> TargetChallengeSubmissionRejected;
    public event System.Action<VocabularyWordData, string> TargetChallengeCompleted;
    public event System.Action<VocabularyWordData, string> TargetChallengeRestorationFailed;

    private sealed class NormalCombatTileSnapshot
    {
        public Tile Tile;
        public char Letter;
        public TileState State;
        public int DebuffCounter;
        public int SiblingIndex;
        public bool ActiveSelf;
        public bool ButtonInteractable;
    }

    private readonly HashSet<char> difficultLetters = new HashSet<char>() { 'Q', 'X', 'Z', 'J', 'K', 'V', 'W', 'Y' };
    [Header("Panels")]
    public Transform tilePoolPanel;
    public Transform wordBarPanel;
    public GameObject tileButtonPrefab;

    [Header("Gameplay")]
    public List<Tile> selectedTiles = new List<Tile>();
    public List<Tile> wordBarTiles = new List<Tile>();
    public Sprite[] letterSpritesUI; // 26 sprites, drag them in inspector from A-Z

    [Header("Stage Settings")]
    public int currentStage = 1; // set this per stage scene

    [Header("Initialization")]
    public bool prefillTilePoolOnStart = true;

    public AttackController attackController;
    private GridLayoutGroup tilePoolGrid;
    private GridLayoutGroup wordBarGrid;
    private Coroutine startupInitializationCoroutine;
    private bool startupPoolInitialized;
    public GameObject tileUIPrefab;
    private readonly Dictionary<Tile, float> wordBarBaseTmpFontSizes = new Dictionary<Tile, float>();
    private readonly Dictionary<Tile, int> wordBarBaseLegacyFontSizes = new Dictionary<Tile, int>();
    private System.Func<Tile, bool> tileSelectionFilter;
    private readonly List<NormalCombatTileSnapshot> normalCombatPoolSnapshot = new List<NormalCombatTileSnapshot>();
    private bool hasNormalCombatPoolSnapshot;
    private string normalCombatPoolSnapshotSignature = string.Empty;
    private readonly List<Tile> temporaryTargetChallengeTiles = new List<Tile>();
    private TilePoolMode currentMode = TilePoolMode.NormalCombat;
    private VocabularyWordData activeTargetWord;
    private bool attackLockStateBeforeTargetChallenge;
    private bool hasStoredAttackLockState;
    private bool targetChallengeSubmissionPending;
    private string pendingTargetChallengeWord = string.Empty;
    private bool preserveNormalCombatSnapshotAfterTargetChallengeSuccess;
    private bool normalCombatWorldPickupsSuspended;
    private string debugMainWord = string.Empty;
    private string debugRemainderWord = string.Empty;
    private string stage3RiddleMainWord = string.Empty;
    private string stage3RiddleRemainderWord = string.Empty;
    private string stage3VocabularyMainWord = string.Empty;
    private string stage3VocabularyRemainderWord = string.Empty;

    public bool HasNormalCombatPoolSnapshot => hasNormalCombatPoolSnapshot;
    public int NormalCombatPoolSnapshotTileCount => normalCombatPoolSnapshot.Count;
    public bool HasTemporaryTargetChallengePool => temporaryTargetChallengeTiles.Count > 0;
    public int TemporaryTargetChallengeTileCount => temporaryTargetChallengeTiles.Count;
    public bool HasPendingTargetChallengeSubmission => targetChallengeSubmissionPending;
    public bool PreservesNormalCombatSnapshotAfterTargetChallengeSuccess =>
        preserveNormalCombatSnapshotAfterTargetChallengeSuccess;
    public bool AreNormalCombatWorldPickupsSuspended =>
        normalCombatWorldPickupsSuspended || currentMode == TilePoolMode.TargetChallenge;
    public TilePoolMode CurrentMode => currentMode;
    public VocabularyWordData ActiveTargetWord => activeTargetWord;
    public int CurrentStage => currentStage;
    public bool HasStage3RiddleWords => !string.IsNullOrEmpty(stage3RiddleMainWord);
    public bool HasStage3VocabularyWords => !string.IsNullOrEmpty(stage3VocabularyMainWord);

    [Header("Vocabulary Target Challenge")]
    [SerializeField] private Color targetChallengeTileTint = new Color(0.58f, 0.92f, 1f, 1f);
    [SerializeField] private Color targetChallengeOutlineColor = new Color(1f, 0.72f, 0.16f, 1f);
    [SerializeField] private Vector2 targetChallengeOutlineDistance = new Vector2(3f, -3f);
    [SerializeField] private StageVocabularyProfile layer3DebugProfile;
    [SerializeField, Min(0)] private int layer3DebugWordIndex;

    [Header("Word Bar Adaptive Layout")]
    public float wordBarBaseCellWidth = 76.5f;
    public float wordBarMinCellWidth = 45f;
    public float wordBarSpacing = 0.15f;
    public int wordBarPaddingLeft = 10;
    public int wordBarPaddingRight = 10;
    public int wordBarPaddingTop = 5;
    public int wordBarPaddingBottom = 5;
    [Range(0.5f, 1f)] public float wordBarMinFontScale = 0.72f;

    private void Awake()
    {
        Scene owningScene = gameObject.scene;
        if (IsPersistentCombatScene(owningScene.name))
        {
            if (Instance != null && Instance != this)
            {
                Instance.ConfigureForScene(owningScene);
                gameObject.SetActive(false);
                Destroy(gameObject);
                return;
            }

            Instance = this;
            transform.SetParent(null, true);
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += HandleSceneLoaded;
            ConfigureForScene(owningScene, resetStageState: false);
        }

        // Stage scenes using the persistent HUD intentionally serialize these as
        // null; PersistentPlayerRoot binds the DDOL panels immediately after scene
        // load. Do not throw before that binding gets a chance to happen.
        tilePoolGrid = tilePoolPanel != null
            ? tilePoolPanel.GetComponent<GridLayoutGroup>()
            : null;
        wordBarGrid = wordBarPanel != null
            ? wordBarPanel.GetComponent<GridLayoutGroup>()
            : null;
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        Instance = null;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ConfigureForScene(scene);
    }

    public static bool IsPersistentCombatScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return false;

        return sceneName == "SampleScene"
               || sceneName == "Stage1_OptimizedMap_Test"
               || sceneName == "Stage2Scene"
               || sceneName == "Stage3Scene"
               || sceneName.StartsWith("Stage4", System.StringComparison.OrdinalIgnoreCase)
               || sceneName.StartsWith("Stage5", System.StringComparison.OrdinalIgnoreCase);
    }

    public static int ResolveStageNumber(string sceneName)
    {
        if (sceneName == "SampleScene" || sceneName == "Stage1_OptimizedMap_Test")
            return 1;
        if (sceneName == "Stage2Scene")
            return 2;
        if (sceneName == "Stage3Scene")
            return 3;
        if (!string.IsNullOrWhiteSpace(sceneName)
            && sceneName.StartsWith("Stage4", System.StringComparison.OrdinalIgnoreCase))
            return 4;
        if (!string.IsNullOrWhiteSpace(sceneName)
            && sceneName.StartsWith("Stage5", System.StringComparison.OrdinalIgnoreCase))
            return 5;

        return 0;
    }

    public void ConfigureForScene(Scene scene, bool resetStageState = true)
    {
        if (!scene.IsValid())
            return;

        int resolvedStage = ResolveStageNumber(scene.name);
        if (resolvedStage <= 0)
            return;

        bool changed = currentStage != resolvedStage;
        currentStage = resolvedStage;

        if (changed && resetStageState)
            ResetTransientStateForStageChange();

        TileGenerationDebugOverlay.Show(
            debugMainWord,
            debugRemainderWord,
            currentStage,
            HasStage3RiddleWords,
            HasStage3VocabularyWords);
        Debug.Log(
            $"[TileManager] Persistent vocabulary/tile runtime configured for Stage {currentStage} ({scene.name}).",
            this);
    }

    private void ResetTransientStateForStageChange()
    {
        // The manager survives, but a stage's tile inventory does not. Clear
        // the persistent HUD immediately so hidden Target Challenge snapshots
        // or old-stage letters cannot leak into the next combat scene.
        DetachAndDestroyChildren(tilePoolPanel);
        DetachAndDestroyChildren(wordBarPanel);

        selectedTiles.Clear();
        wordBarTiles.Clear();
        ClearTileSelectionFilter();
        normalCombatWorldPickupsSuspended = false;

        temporaryTargetChallengeTiles.Clear();
        normalCombatPoolSnapshot.Clear();
        normalCombatPoolSnapshotSignature = string.Empty;
        hasNormalCombatPoolSnapshot = false;
        activeTargetWord = null;
        targetChallengeSubmissionPending = false;
        pendingTargetChallengeWord = string.Empty;
        currentMode = TilePoolMode.NormalCombat;

        if (hasStoredAttackLockState && attackController != null)
            attackController.SetAttackInputLocked(attackLockStateBeforeTargetChallenge);
        hasStoredAttackLockState = false;

        debugMainWord = string.Empty;
        debugRemainderWord = string.Empty;
        stage3RiddleMainWord = string.Empty;
        stage3RiddleRemainderWord = string.Empty;
        stage3VocabularyMainWord = string.Empty;
        stage3VocabularyRemainderWord = string.Empty;
    }

    /// <summary>
    /// Stage 3's future riddle controller can feed the exact answer words into
    /// the already-persistent tile pipeline without creating another manager.
    /// Until a riddle is supplied, Stage 3 falls back to the normal dictionary
    /// planner so direct scene testing remains usable.
    /// </summary>
    public void SetStage3RiddleWords(
        string mainWord,
        string remainderWord = "",
        bool rebuildNormalCombatPool = true)
    {
        stage3RiddleMainWord = NormalizeDebugWord(mainWord);
        stage3RiddleRemainderWord = NormalizeDebugWord(remainderWord);

        if (currentStage != 3)
            return;

        SetDebugWordPair(stage3RiddleMainWord, stage3RiddleRemainderWord);

        if (rebuildNormalCombatPool
            && currentMode == TilePoolMode.NormalCombat
            && tilePoolPanel != null
            && wordBarPanel != null)
        {
            selectedTiles.Clear();
            wordBarTiles.Clear();
            DetachAndDestroyChildren(tilePoolPanel);
            DetachAndDestroyChildren(wordBarPanel);
            SetTilePoolPanelGrid();
            SetWordBarPanelGrid();
            PrefillTilePool(16, currentStage);
            CleanupTilePoolPanel();
            startupPoolInitialized = true;
            attackController?.CheckWord();
        }
    }

    public void SetStage3VocabularyWords(
        string mainWord,
        string remainderWord = "",
        bool rebuildNormalCombatPool = true)
    {
        // Vocabulary targets are temporary challenge content, not the Stage 3
        // canonical MainBatch/RemBatch. Keep the word registered for diagnostics,
        // but do not rebuild Normal Combat tiles and do not overwrite the two
        // persistent debug/display slots. The VocabularyEncounterFlowController
        // already snapshots Normal Combat and creates its own temporary pool.
        stage3VocabularyMainWord = NormalizeDebugWord(mainWord);
        stage3VocabularyRemainderWord = NormalizeDebugWord(remainderWord);

        if (currentStage == 3)
        {
            TileGenerationDebugOverlay.Show(
                debugMainWord,
                debugRemainderWord,
                currentStage,
                HasStage3RiddleWords,
                HasStage3VocabularyWords);

            Debug.Log(
                $"[Stage3Tiles] Vocabulary challenge registered: {stage3VocabularyMainWord}. " +
                $"MainBatch='{debugMainWord}', RemBatch='{debugRemainderWord}' preserved.",
                this);
        }
    }

    private void Start()
    {
        if (startupInitializationCoroutine == null && !startupPoolInitialized)
            startupInitializationCoroutine = StartCoroutine(InitAfterLayout(resetExistingTiles: false));

        // Keep this diagnostic generation silent. It must never overwrite the
        // live Main/Remainder debug slots shown to the player.
        List<char> testRefill = GenerateRefillLettersFromBudget(11);
        Debug.Log("Test refill letters: " + string.Join(", ", testRefill));
    }

    public void RebindPersistentHud(
        Transform persistentTilePoolPanel,
        Transform persistentWordBarPanel,
        AttackController persistentAttackController,
        bool clearExistingTiles)
    {
        ConfigureForScene(SceneManager.GetActiveScene(), resetStageState: false);

        tilePoolPanel = persistentTilePoolPanel;
        wordBarPanel = persistentWordBarPanel;
        attackController = persistentAttackController;

        tilePoolGrid = tilePoolPanel != null
            ? tilePoolPanel.GetComponent<GridLayoutGroup>()
            : null;
        wordBarGrid = wordBarPanel != null
            ? wordBarPanel.GetComponent<GridLayoutGroup>()
            : null;

        selectedTiles.Clear();
        wordBarTiles.Clear();
        ClearTileSelectionFilter();
        normalCombatWorldPickupsSuspended = false;

        startupPoolInitialized = false;
        if (startupInitializationCoroutine != null)
        {
            StopCoroutine(startupInitializationCoroutine);
            startupInitializationCoroutine = null;
        }

        if (clearExistingTiles && currentMode == TilePoolMode.NormalCombat)
        {
            // A persistent HUD can arrive with the previous stage's runtime tiles.
            // Detach/deactivate them immediately so this stage can own a clean pool
            // without waiting for Unity's deferred Destroy() queue.
            DetachAndDestroyChildren(tilePoolPanel);
            DetachAndDestroyChildren(wordBarPanel);

            SetTilePoolPanelGrid();
            SetWordBarPanelGrid();

            if (prefillTilePoolOnStart)
                PrefillTilePool(16, currentStage);

            CleanupTilePoolPanel();
            startupPoolInitialized = true;

            int ownedCount = GetNormalCombatOwnedTileCount();
            Debug.Log(
                $"[TileManager] Persistent HUD rebound for Stage {currentStage}: " +
                $"{ownedCount}/16 normal combat tiles ready.",
                this);
            return;
        }

        startupInitializationCoroutine = StartCoroutine(InitAfterLayout(resetExistingTiles: false));
    }

    private IEnumerator InitAfterLayout(bool resetExistingTiles)
    {
        yield return null; // wait one frame

        if (resetExistingTiles && currentMode == TilePoolMode.NormalCombat)
        {
            selectedTiles.Clear();
            wordBarTiles.Clear();
            DestroyChildren(tilePoolPanel);
            DestroyChildren(wordBarPanel);

            // Destroy() is deferred until the end of the frame. Wait one more
            // frame before refilling so the persistent HUD cannot briefly hold
            // the previous stage's 16 tiles plus the new stage's 16 tiles.
            yield return null;
        }

        SetTilePoolPanelGrid();
        SetWordBarPanelGrid();
        if (prefillTilePoolOnStart)
            PrefillTilePool(16, currentStage);
        CleanupTilePoolPanel();

        if (prefillTilePoolOnStart)
            yield return StartCoroutine(VerifyStartupTilePoolAfterCleanup(16, currentStage));

        startupPoolInitialized = true;
        startupInitializationCoroutine = null;
    }

    void PrefillTilePool(int amount, int stage = 1)
    {
        List<char> lettersToSpawn = GenerateDictionaryWeightedLetters(amount, stage);

        if (lettersToSpawn.Count < amount)
        {
            Debug.LogWarning(
                $"[TileManager] Stage {stage} startup generation returned " +
                $"{lettersToSpawn.Count}/{amount} letters. Filling the missing startup budget.",
                this);
            FillBalancedFallbackLetters(lettersToSpawn, amount);
            ShuffleLetters(lettersToSpawn);
        }
        else if (lettersToSpawn.Count > amount)
        {
            Debug.LogError(
                $"[TileManager] Stage {stage} startup generation exceeded its budget: " +
                $"{lettersToSpawn.Count}/{amount}. Trimming to preserve the invariant.",
                this);
            lettersToSpawn.RemoveRange(amount, lettersToSpawn.Count - amount);
        }
     
        foreach (char letter in lettersToSpawn)
            CreateTileInPool(letter);

        int createdCount = GetNormalCombatOwnedTileCount();
        if (createdCount == amount)
        {
            Debug.Log(
                $"[TileManager] Successfully filled Tile Pool with {createdCount}/{amount} UI tiles " +
                $"for Stage {stage} startup.",
                this);
        }
        else
        {
            Debug.LogError(
                $"[TileManager] Stage {stage} startup Tile Pool invariant failed after creation: " +
                $"{createdCount}/{amount} tiles.",
                this);
        }
    }

    private IEnumerator VerifyStartupTilePoolAfterCleanup(int expectedCount, int stage)
    {
        // Destroy() is deferred until the end of the current frame. Verify on
        // the following frame so startup cleanup and other Start() calls have
        // had a chance to affect the real UI pool.
        yield return null;

        int verifiedCount = GetNormalCombatOwnedTileCount();
        if (verifiedCount == expectedCount)
        {
            Debug.Log(
                $"[TileManager] Startup Tile Pool verified after cleanup: " +
                $"{verifiedCount}/{expectedCount} UI tiles for Stage {stage}.",
                this);
            yield break;
        }

        Debug.LogError(
            $"[TileManager] Startup Tile Pool changed after creation: " +
            $"{verifiedCount}/{expectedCount} UI tiles for Stage {stage}. " +
            "Another startup path may be removing or replacing Tile Pool children.",
            this);
    }

    public void ToggleTilePanel(Tile tile)
    {
        if (tile == null || !tile.IsSelectable())
            return;

        if (currentMode == TilePoolMode.TargetChallenge && targetChallengeSubmissionPending)
        {
            Debug.LogWarning(
                "[TileManager] Target Challenge tiles are locked until the active projectile resolves.",
                this);
            return;
        }

        // FROM TILE POOL → WORD BAR
        if (tile.transform.parent == tilePoolPanel)
        {
            if (tileSelectionFilter != null && !tileSelectionFilter(tile))
            {
                TileSelectionRejected?.Invoke(tile);
                return;
            }

            MoveTile(tile, wordBarPanel);

            if (!selectedTiles.Contains(tile))
                selectedTiles.Add(tile);

            if (!wordBarTiles.Contains(tile))
                wordBarTiles.Add(tile);
        }
        // FROM WORD BAR → TILE POOL
        else if (tile.transform.parent == wordBarPanel)
        {
            int index = selectedTiles.IndexOf(tile);

            if (index >= 0)
            {
                for (int i = selectedTiles.Count - 1; i >= index; i--)
                {
                    Tile t = selectedTiles[i];
                    MoveTile(t, tilePoolPanel);

                    selectedTiles.RemoveAt(i);
                    wordBarTiles.Remove(t);
                }
            }
        }

        SetWordBarPanelGrid();

        if (attackController != null)
            attackController.CheckWord();

        NotifyWordChanged();
        Debug.Log("Tiles in wordBar: " + wordBarTiles.Count);
    }

    private void MoveTile(Tile tile, Transform newParent)
    {
        tile.transform.SetParent(newParent, false);
        tile.transform.localScale = Vector3.one;

        RectTransform rect = tile.GetComponent<RectTransform>();
        rect.anchoredPosition = Vector2.zero;

        if (newParent == wordBarPanel)
            KeepWordBarArtAroundSelectedTiles();
    }

    // Screen Space UI draws later siblings on top. Reparenting a clicked tile
    // makes it the last WordBar child, which used to cover the ornate trim.
    // Keep the transparent-centre gold frame in front of the tiles, with the
    // navy backing behind them. Both decorations already ignore grid layout;
    // tile order, click targets, and TilePoolPanel are left untouched.
    private void KeepWordBarArtAroundSelectedTiles()
    {
        if (wordBarPanel == null)
            return;

        Transform backing = wordBarPanel.Find("WordBar_OrnateBackdrop");
        Transform frame = wordBarPanel.Find("WordBar_OrnateFrame");
        if (backing == null || frame == null
            || !IsNonTileLayoutDecoration(backing)
            || !IsNonTileLayoutDecoration(frame))
            return;

        if (backing.GetSiblingIndex() != 0)
            backing.SetAsFirstSibling();
        if (frame.GetSiblingIndex() != wordBarPanel.childCount - 1)
            frame.SetAsLastSibling();
    }

    public string GetCurrentWord()
    {
        string word = "";
        foreach (Tile t in selectedTiles)
            word += t.letter.ToString();
        return word;
    }

    public void ClearWordBar()
    {
        foreach (Tile t in selectedTiles)
        {
            MoveTile(t, tilePoolPanel);
        }

        selectedTiles.Clear();

        if (attackController != null)
            attackController.CheckWord();

        NotifyWordChanged();
    }

    public void ClearAllTiles()
    {
        if (currentMode == TilePoolMode.TargetChallenge)
        {
            Debug.LogError(
                "[TileManager] ClearAllTiles is blocked while a temporary Target Challenge pool is active.",
                this);
            return;
        }

        selectedTiles.Clear();
        wordBarTiles.Clear();

        DestroyChildren(tilePoolPanel);
        DestroyChildren(wordBarPanel);

        SetWordBarPanelGrid();

        if (attackController != null)
            attackController.CheckWord();

        NotifyWordChanged();
    }

    private static void DestroyChildren(Transform parent)
    {
        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (IsNonTileLayoutDecoration(child))
                continue;

            Destroy(child.gameObject);
        }
    }

    private static void DetachAndDestroyChildren(Transform parent)
    {
        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (IsNonTileLayoutDecoration(child))
                continue;

            child.gameObject.SetActive(false);
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }
    }

    // The Word Bar has artist-editable Images excluded from its tile GridLayoutGroup.
    // Clearing/rebinding tiles must not destroy those permanent visual children.
    private static bool IsNonTileLayoutDecoration(Transform child)
    {
        if (child.name != "WordBar_OrnateBackdrop" &&
            child.name != "WordBar_OrnateFrame")
            return false;

        LayoutElement layout = child.GetComponent<LayoutElement>();
        return layout != null && layout.ignoreLayout && child.GetComponent<Tile>() == null;
    }

    public void DestroyUsedTiles()
    {
        if (currentMode == TilePoolMode.TargetChallenge)
        {
            Debug.LogError(
                "[TileManager] Normal tile consumption is blocked while a temporary Target Challenge pool is active.",
                this);
            return;
        }

        foreach (Tile tile in wordBarTiles)
        {
            Destroy(tile.gameObject);
        }

        wordBarTiles.Clear();
        selectedTiles.Clear();

        if (attackController != null)
            attackController.CheckWord();

        NotifyWordChanged();
    }

    public int GetWordTileCount()
    {
        return wordBarTiles.Count;
    }

    public void SetTileSelectionFilter(System.Func<Tile, bool> selectionFilter)
    {
        tileSelectionFilter = selectionFilter;
    }

    public void ClearTileSelectionFilter()
    {
        tileSelectionFilter = null;
    }

    private void NotifyWordChanged()
    {
        WordChanged?.Invoke(GetCurrentWord());
    }

    public void CreateTileInPool(char letter)
    {
        if (currentMode == TilePoolMode.TargetChallenge)
        {
            Debug.LogWarning(
                $"[TileManager] Ignored Normal Combat tile '{char.ToUpperInvariant(letter)}' while Target Challenge is active.",
                this);
            return;
        }

        CreateTileInPoolInternal(letter);
    }

    public int GetNormalCombatOwnedTileCount()
    {
        if (tilePoolPanel == null)
            return 0;

        int ownedCount = 0;
        for (int i = 0; i < tilePoolPanel.childCount; i++)
        {
            if (tilePoolPanel.GetChild(i).GetComponent<Tile>() != null)
                ownedCount++;
        }

        // Use the logical selection list instead of Word Bar childCount. Unity
        // defers Destroy() until the end of the frame, but replacement spawning
        // begins immediately after DestroyUsedTiles clears this list.
        for (int i = 0; i < wordBarTiles.Count; i++)
        {
            if (wordBarTiles[i] != null)
                ownedCount++;
        }

        return ownedCount;
    }

    public bool TryCollectNormalCombatWorldTile(char letter, int maximumOwnedTiles = 16)
    {
        if (currentMode != TilePoolMode.NormalCombat
            || AreNormalCombatWorldPickupsSuspended
            || maximumOwnedTiles <= 0)
        {
            return false;
        }

        int ownedCount = GetNormalCombatOwnedTileCount();
        if (ownedCount >= maximumOwnedTiles)
            return false;

        return CreateTileInPoolInternal(char.ToUpperInvariant(letter)) != null;
    }

    private Tile CreateTileInPoolInternal(char letter)
    {
        GameObject tileObj = Instantiate(tileUIPrefab, tilePoolPanel);
        tileObj.transform.localScale = Vector3.one;

        Tile tileScript = tileObj.GetComponent<Tile>();
        Button button = tileObj.GetComponent<Button>();
        if (button == null)
            button = tileObj.AddComponent<Button>();

        Image img = tileObj.GetComponent<Image>();
        if (img != null)
        {
            img.raycastTarget = true;
            button.targetGraphic = img;
        }

        if (tileScript == null)
        {
            Debug.LogError("[TileManager] The configured Tile UI prefab has no Tile component.", tileObj);
            Destroy(tileObj);
            return null;
        }

        tileScript.button = button;
        tileScript.SetLetter(letter);

        // Assign the sprite for the letter
        int index = letter - 'A';
        if (index >= 0 && index < letterSpritesUI.Length)
        {
            if (img != null)
                img.sprite = letterSpritesUI[index];
        }

        SetTilePoolPanelGrid();
        return tileScript;
    }

    public void AddTilesToPool(int count)
    {
        List<char> lettersToSpawn = GenerateDictionaryWeightedLetters(count, currentStage);
        for (int i = 0; i < lettersToSpawn.Count; i++)
        {
            CreateTileInPool(lettersToSpawn[i]);
        }
    }

    private List<char> GenerateDictionaryWeightedLetters(int amount, int stage)
    {
        List<char> finalLetters = new List<char>(amount);

        if (DictionaryManager.Instance == null)
        {
            Debug.LogWarning(
                $"[TileManager] DictionaryManager is unavailable during startup. " +
                $"Using balanced fallback letters to preserve {amount}/{amount} Tile Pool initialization.",
                this);
            FillBalancedFallbackLetters(finalLetters, amount);
            ShuffleLetters(finalLetters);
            return finalLetters;
        }

        if (stage == 3
            && HasStage3RiddleWords
            && TryBuildStage3RiddleOpportunity(amount, out string stage3MainWord, out string stage3RemainderWord))
        {
            AddWordLetters(finalLetters, stage3MainWord, amount);
            AddWordLetters(finalLetters, stage3RemainderWord, amount);
            FillBalancedFallbackLetters(finalLetters, amount);
            ShuffleLetters(finalLetters);

            Debug.Log(
                $"[Stage3Tiles] Riddle opportunity: {stage3MainWord} + {stage3RemainderWord} " +
                $"| Filled: {finalLetters.Count}/{amount}.",
                this);
            SetDebugWordPair(stage3MainWord, stage3RemainderWord);
            return finalLetters;
        }

        if (stage == 2 && TryBuildStage2ObjectiveOpportunity(amount, out string objectiveWord1, out string objectiveWord2))
        {
            AddWordLetters(finalLetters, objectiveWord1, amount);
            AddWordLetters(finalLetters, objectiveWord2, amount);
            FillBalancedFallbackLetters(finalLetters, amount);
            ShuffleLetters(finalLetters);

            Debug.Log(
                $"[Stage2Tiles] Objective opportunity: {objectiveWord1} + {objectiveWord2} " +
                $"| Filled: {finalLetters.Count}/{amount}.",
                this);
            SetDebugWordPair(objectiveWord1, objectiveWord2);
            return finalLetters;
        }

        int safety = 0;

        while (safety < 100)
        {
            string mainWord = GetPlayableWordInRange(9, 13);
            if (string.IsNullOrEmpty(mainWord))
            {
                safety++;
                continue;
            }

            int remainder = amount - mainWord.Length;
            if (remainder < 0)
            {
                safety++;
                continue;
            }

            string remainderWord = "";

            if (remainder > 0)
            {
                remainderWord = GetPlayableWordInRange(remainder, remainder);
                if (string.IsNullOrEmpty(remainderWord))
                {
                    safety++;
                    continue;
                }
            }

            Debug.Log($"Main word: {mainWord} | Remainder word: {remainderWord}");
            SetDebugWordPair(mainWord, remainderWord);

            foreach (char c in mainWord)
            {
                if (char.IsLetter(c))
                    finalLetters.Add(char.ToUpperInvariant(c));
            }

            foreach (char c in remainderWord)
            {
                if (char.IsLetter(c) && finalLetters.Count < amount)
                    finalLetters.Add(char.ToUpperInvariant(c));
            }

            break;
        }



        if (finalLetters.Count < amount)
            Debug.LogWarning(
                $"[TileManager] Dictionary planning generated {finalLetters.Count}/{amount} startup letters. " +
                "Balanced fallback letters will preserve the Tile Pool count.",
                this);

        FillBalancedFallbackLetters(finalLetters, amount);

        ShuffleLetters(finalLetters);

        return finalLetters;
    }
    private string GetPlayableWordInRange(int minLength, int maxLength)
    {
        int safety = 0;

        while (safety < 100)
        {
            string word = DictionaryManager.Instance.GetRandomWordByLengthRange(minLength, maxLength);

            if (!string.IsNullOrEmpty(word)
                && word.Length >= minLength
                && word.Length <= maxLength
                && IsPlayableWord(word))
            {
                return word.ToUpperInvariant();
            }

            safety++;
        }

        return "";
    }
    private bool IsPlayableWord(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
            return false;

        word = word.ToUpperInvariant();

        if (word.Length < 3)
            return false;

        int vowelCount = 0;
        int rareCount = 0;
        Dictionary<char, int> charCounts = new Dictionary<char, int>();

        foreach (char c in word)
        {
            if (!char.IsLetter(c))
                return false;

            if ("AEIOU".Contains(c))
                vowelCount++;

            if ("QXZJVWKY".Contains(c))
                rareCount++;

            if (!charCounts.ContainsKey(c))
                charCounts[c] = 0;

            charCounts[c]++;
        }

        float vowelRatio = (float)vowelCount / word.Length;

        // Must have enough vowels
        if (vowelCount == 0)
            return false;

        if (word.Length <= 4 && vowelCount < 1)
            return false;

        if (word.Length >= 5 && vowelCount < 2)
            return false;

        // Too consonant-heavy
        if (vowelRatio < 0.30f)
            return false;

        // Too many ugly letters
        if (rareCount >= 2)
            return false;

        // Reject words with any letter repeated too many times
        foreach (var kvp in charCounts)
        {
            if (kvp.Value >= 3)
                return false;
        }

        return true;
    }
    private string GetLongestPlayableWordFitting(int budget)
    {
        for (int length = budget; length >= 3; length--)
        {
            string word = GetPlayableWordInRange(length, length);
            if (!string.IsNullOrEmpty(word))
                return word;
        }

        return "";
    }

    private Vector3 GetStageLengthWeights(int stage)
    {
        if (stage <= 1) return new Vector3(0.70f, 0.20f, 0.10f);
        if (stage == 2) return new Vector3(0.62f, 0.25f, 0.13f);
        if (stage == 3) return new Vector3(0.55f, 0.28f, 0.17f);
        if (stage == 4) return new Vector3(0.48f, 0.31f, 0.21f);

        // Stage 5+: most balanced while keeping short > medium > long.
        return new Vector3(0.42f, 0.33f, 0.25f);
    }

    private string TakeLettersFromWord(string word, int budget)
    {
        if (string.IsNullOrEmpty(word) || budget <= 0)
            return "";

        int takeCount = Mathf.Min(word.Length, budget);
        return word.Substring(0, takeCount).ToUpperInvariant();
    }

    public List<char> GenerateRefillLettersFromBudget(int budget, string consumedWord = null)
    {
        List<char> refillLetters = new List<char>(Mathf.Max(0, budget));

        if (budget <= 0)
            return refillLetters;

        string word1 = "";
        string word2 = "";

        if (DictionaryManager.Instance != null)
        {
            if (currentStage == 3
                && HasStage3RiddleWords
                && TryBuildStage3RiddleOpportunity(budget, out word1, out word2))
            {
                AddWordLetters(refillLetters, word1, budget);
                AddWordLetters(refillLetters, word2, budget);
            }
            else if (currentStage == 2 && TryBuildStage2ObjectiveOpportunity(budget, out word1, out word2))
            {
                AddWordLetters(refillLetters, word1, budget);
                AddWordLetters(refillLetters, word2, budget);
            }

            // Prefer one exact word so the complete replacement set contains
            // an immediately playable option matching the consumed budget.
            if (refillLetters.Count == 0)
                word1 = GetPlayableWordInRange(budget, budget);

            // If no exact word exists, find two complete words whose lengths
            // exactly partition the budget. Never accept a partial word here.
            if (refillLetters.Count == 0 && string.IsNullOrEmpty(word1))
            {
                for (int firstLength = budget - 3; firstLength >= 3; firstLength--)
                {
                    int secondLength = budget - firstLength;
                    if (secondLength < 3)
                        continue;

                    string firstCandidate = GetPlayableWordInRange(firstLength, firstLength);
                    if (string.IsNullOrEmpty(firstCandidate))
                        continue;

                    string secondCandidate = GetPlayableWordInRange(secondLength, secondLength);
                    if (string.IsNullOrEmpty(secondCandidate))
                        continue;

                    word1 = firstCandidate;
                    word2 = secondCandidate;
                    break;
                }
            }

            if (refillLetters.Count == 0 && !string.IsNullOrEmpty(word1))
                AddWordLetters(refillLetters, word1, budget);

            if (refillLetters.Count == 0 && !string.IsNullOrEmpty(word2))
                AddWordLetters(refillLetters, word2, budget);

            // Preserve the old "longest playable word" behavior as a partial
            // opportunity only when an exact one- or two-word plan was not
            // available. Balanced fallback letters close the remaining gap.
            if (refillLetters.Count == 0)
            {
                word1 = GetLongestPlayableWordFitting(budget);
                if (!string.IsNullOrEmpty(word1))
                    AddWordLetters(refillLetters, word1, budget);
            }
        }
        else
        {
            Debug.LogError(
                "DictionaryManager is missing. Using balanced fallback letters to preserve the refill count.");
        }

        int fallbackCount = budget - refillLetters.Count;
        FillBalancedFallbackLetters(refillLetters, budget);

        Debug.Log(
            $"[NEXT REFILL] Used Budget: {budget} | Word1: {word1} | Word2: {word2} "
            + $"| Fallback: {fallbackCount} | Spawned: {string.Join(", ", refillLetters)} "
            + $"| Filled: {refillLetters.Count}/{budget}");

        if (refillLetters.Count != budget)
            Debug.LogError(
                $"[REFILL] Count invariant failed: generated {refillLetters.Count}/{budget} replacement letters.");

        // The two labels are persistent semantic slots, not a dump of the most
        // recent refill. A refill caused by consuming Main may only replace Main;
        // consuming Remainder may only replace Remainder. This prevents a short
        // remainder refill from hijacking the Main label.
        UpdateDebugWordPairAfterConsumption(consumedWord, word1, word2);

        return refillLetters;
    }

    private void SetDebugWordPair(string mainWord, string remainderWord)
    {
        debugMainWord = NormalizeDebugWord(mainWord);
        debugRemainderWord = NormalizeDebugWord(remainderWord);
        TileGenerationDebugOverlay.Show(
            debugMainWord,
            debugRemainderWord,
            currentStage,
            HasStage3RiddleWords,
            HasStage3VocabularyWords);
    }

    private void UpdateDebugWordPairAfterConsumption(
        string consumedWord,
        string refillWord1,
        string refillWord2)
    {
        string consumed = NormalizeDebugWord(consumedWord);
        if (string.IsNullOrEmpty(consumed))
            return;

        string replacement = NormalizeDebugWord(refillWord1);
        if (string.IsNullOrEmpty(replacement))
            replacement = NormalizeDebugWord(refillWord2);

        bool consumedMain = !string.IsNullOrEmpty(debugMainWord)
                            && string.Equals(
                                consumed,
                                debugMainWord,
                                System.StringComparison.Ordinal);
        bool consumedRemainder = !string.IsNullOrEmpty(debugRemainderWord)
                                 && string.Equals(
                                     consumed,
                                     debugRemainderWord,
                                     System.StringComparison.Ordinal);

        // If the same word ever occupies both slots, Main wins deterministically.
        // More importantly, a Remainder match can never write into Main.
        if (consumedMain)
        {
            debugMainWord = replacement;
            Debug.Log(
                $"[TileDebugSlots] MAIN consumed '{consumed}' -> '{debugMainWord}'. "
                + $"Remainder preserved as '{debugRemainderWord}'.",
                this);
        }
        else if (consumedRemainder)
        {
            debugRemainderWord = replacement;
            Debug.Log(
                $"[TileDebugSlots] REMAINDER consumed '{consumed}' -> '{debugRemainderWord}'. "
                + $"Main preserved as '{debugMainWord}'.",
                this);
        }
        else
        {
            // Valid free-form words can consume tiles too. They are deliberately
            // not allowed to relabel either tracked slot because doing so would
            // destroy Main/Remainder identity.
            Debug.Log(
                $"[TileDebugSlots] Consumed untracked word '{consumed}'. "
                + $"Main='{debugMainWord}', Remainder='{debugRemainderWord}' preserved.",
                this);
        }

        TileGenerationDebugOverlay.Show(
            debugMainWord,
            debugRemainderWord,
            currentStage,
            HasStage3RiddleWords,
            HasStage3VocabularyWords);
    }

    private static string NormalizeDebugWord(string word)
    {
        return string.IsNullOrWhiteSpace(word)
            ? string.Empty
            : word.Trim().ToUpperInvariant();
    }

    private bool TryBuildStage3RiddleOpportunity(
        int budget,
        out string mainWord,
        out string remainderWord)
    {
        mainWord = stage3RiddleMainWord;
        remainderWord = stage3RiddleRemainderWord;

        if (string.IsNullOrEmpty(mainWord))
            return false;

        int totalLetters = mainWord.Length + remainderWord.Length;
        if (mainWord.Length > budget || totalLetters > budget)
        {
            Debug.LogWarning(
                $"[Stage3Tiles] Riddle words need {totalLetters} tile(s), but the current budget is {budget}. " +
                "Falling back to the normal dictionary planner for this refill.",
                this);
            mainWord = string.Empty;
            remainderWord = string.Empty;
            return false;
        }

        if (DictionaryManager.Instance != null)
        {
            if (!DictionaryManager.Instance.IsValidWord(mainWord))
                Debug.LogWarning($"[Stage3Tiles] Riddle main word '{mainWord}' is not in DictionaryManager.", this);

            if (!string.IsNullOrEmpty(remainderWord)
                && !DictionaryManager.Instance.IsValidWord(remainderWord))
            {
                Debug.LogWarning(
                    $"[Stage3Tiles] Riddle remainder word '{remainderWord}' is not in DictionaryManager.",
                    this);
            }
        }

        return true;
    }

    private bool TryBuildStage2ObjectiveOpportunity(int budget, out string word1, out string word2)
    {
        word1 = string.Empty;
        word2 = string.Empty;

        if (budget < 3 || DictionaryManager.Instance == null)
            return false;

        Stage2ObjectiveTracker tracker = FindFirstObjectByType<Stage2ObjectiveTracker>();
        bool needsTenLetterWord = tracker == null || tracker.TenLetterWordsCast < tracker.TenLetterWordTarget;
        bool needsMaritimeWord = tracker == null || tracker.UniqueMaritimeWordsCast < tracker.MaritimeWordTarget;

        if (needsTenLetterWord && budget >= 10)
        {
            word1 = GetPlayableWordInRange(10, 10);
            if (!string.IsNullOrEmpty(word1))
            {
                int remaining = budget - word1.Length;
                if (needsMaritimeWord && remaining >= 3)
                    word2 = GetStage2MaritimeWord(remaining, true, tracker);

                if (string.IsNullOrEmpty(word2) && remaining >= 3)
                    word2 = GetPlayableWordInRange(remaining, remaining);

                return true;
            }
        }

        if (!needsMaritimeWord)
            return false;

        word1 = GetStage2MaritimeWord(budget, false, tracker);
        if (string.IsNullOrEmpty(word1))
            return false;

        int remainder = budget - word1.Length;
        if (remainder >= 3)
            word2 = GetPlayableWordInRange(remainder, remainder);

        return true;
    }

    private string GetStage2MaritimeWord(
        int availableLength,
        bool requireExactLength,
        Stage2ObjectiveTracker tracker)
    {
        List<string> preferred = new List<string>();
        List<string> alreadyCast = new List<string>();

        foreach (string candidate in Stage2ObjectiveVocabulary.CreateMaritimeWordCopy())
        {
            if (string.IsNullOrWhiteSpace(candidate))
                continue;

            string normalized = candidate.Trim().ToUpperInvariant();
            bool lengthFits = requireExactLength
                ? normalized.Length == availableLength
                : normalized.Length <= availableLength;

            if (!lengthFits
                || !DictionaryManager.Instance.IsValidWord(normalized)
                || !IsPlayableWord(normalized))
            {
                continue;
            }

            if (tracker != null && tracker.HasCastMaritimeWord(normalized))
                alreadyCast.Add(normalized);
            else
                preferred.Add(normalized);
        }

        List<string> candidates = preferred.Count > 0 ? preferred : alreadyCast;
        return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : string.Empty;
    }

    private static void AddWordLetters(List<char> destination, string word, int maximumCount)
    {
        if (destination == null || string.IsNullOrEmpty(word))
            return;

        foreach (char character in word)
        {
            if (destination.Count >= maximumCount)
                break;

            if (char.IsLetter(character))
                destination.Add(char.ToUpperInvariant(character));
        }
    }

    private static void FillBalancedFallbackLetters(List<char> letters, int targetCount)
    {
        const string weightedVowels = "EEEEEEEEAAAAAAIIIIIOOOOUU";
        const string weightedConsonants = "RRRRRRTTTTTTNNNNNNSSSSSLLLLLDDDDGGGCCCMMMBBBPPPHHFFVVWWYYKJXQZ";

        while (letters.Count < targetCount)
        {
            int vowelCount = 0;
            foreach (char letter in letters)
            {
                if ("AEIOU".IndexOf(letter) >= 0)
                    vowelCount++;
            }

            bool needsVowel = letters.Count == 0
                || (float)vowelCount / letters.Count < 0.35f;
            string source = needsVowel ? weightedVowels : weightedConsonants;
            letters.Add(source[Random.Range(0, source.Length)]);
        }
    }

    public void ResetTileSelection()
    {
        ReturnAllWordBarTilesToPool();
        selectedTiles.Clear();
        wordBarTiles.Clear();
        SetWordBarPanelGrid();

        if (attackController != null)
            attackController.CheckWord();

        NotifyWordChanged();
    }

    public void SetNormalCombatWorldPickupsSuspended(bool suspended)
    {
        normalCombatWorldPickupsSuspended = suspended;
    }

    public bool TrySnapshotAndHideNormalCombatPool()
    {
        if (hasNormalCombatPoolSnapshot)
        {
            Debug.LogWarning(
                "[TileManager] A Normal Combat pool snapshot is already active. Restore it before creating another snapshot.",
                this);
            return false;
        }

        if (tilePoolPanel == null || wordBarPanel == null)
        {
            Debug.LogError(
                "[TileManager] Cannot snapshot the Normal Combat pool because a required panel reference is missing.",
                this);
            return false;
        }

        ResetTileSelection();
        normalCombatPoolSnapshot.Clear();

        for (int siblingIndex = 0; siblingIndex < tilePoolPanel.childCount; siblingIndex++)
        {
            Transform child = tilePoolPanel.GetChild(siblingIndex);
            Tile tile = child.GetComponent<Tile>();
            if (tile == null)
                continue;

            normalCombatPoolSnapshot.Add(new NormalCombatTileSnapshot
            {
                Tile = tile,
                Letter = char.ToUpperInvariant(tile.letter),
                State = tile.CurrentState,
                DebuffCounter = tile.DebuffCounter,
                SiblingIndex = siblingIndex,
                ActiveSelf = tile.gameObject.activeSelf,
                ButtonInteractable = tile.button != null && tile.button.interactable
            });
        }

        normalCombatPoolSnapshotSignature = BuildSnapshotSignature(normalCombatPoolSnapshot);
        hasNormalCombatPoolSnapshot = true;

        foreach (NormalCombatTileSnapshot entry in normalCombatPoolSnapshot)
        {
            if (entry.Tile == null)
                continue;

            if (entry.Tile.button != null)
                entry.Tile.button.interactable = false;

            entry.Tile.gameObject.SetActive(false);
        }

        Debug.Log(
            $"[TileManager] Captured and hid {normalCombatPoolSnapshot.Count} Normal Combat tiles. Signature: {normalCombatPoolSnapshotSignature}",
            this);
        return true;
    }

    public bool TryRestoreNormalCombatPool()
    {
        if (!hasNormalCombatPoolSnapshot)
        {
            Debug.LogWarning("[TileManager] No Normal Combat pool snapshot is available to restore.", this);
            return false;
        }

        if (HasTemporaryTargetChallengePool || currentMode == TilePoolMode.TargetChallenge)
        {
            Debug.LogError(
                "[TileManager] Remove the temporary Target Challenge pool before restoring Normal Combat.",
                this);
            return false;
        }

        for (int i = 0; i < normalCombatPoolSnapshot.Count; i++)
        {
            NormalCombatTileSnapshot entry = normalCombatPoolSnapshot[i];
            if (entry.Tile == null)
            {
                Debug.LogError(
                    "[TileManager] Exact Normal Combat restoration failed because a snapshotted tile was destroyed.",
                    this);
                return false;
            }
        }

        normalCombatPoolSnapshot.Sort((left, right) => left.SiblingIndex.CompareTo(right.SiblingIndex));

        foreach (NormalCombatTileSnapshot entry in normalCombatPoolSnapshot)
        {
            Tile tile = entry.Tile;
            MoveTile(tile, tilePoolPanel);
            tile.transform.SetSiblingIndex(entry.SiblingIndex);
            tile.gameObject.SetActive(true);
            SetTileLetterAndSprite(tile, entry.Letter);
            tile.SetTileState(entry.State, entry.DebuffCounter);
            tile.SetDebuffCounter(entry.DebuffCounter);

            if (tile.button != null)
                tile.button.interactable = entry.ButtonInteractable;

            tile.gameObject.SetActive(entry.ActiveSelf);
        }

        SetTilePoolPanelGrid();
        SetWordBarPanelGrid();

        if (attackController != null)
            attackController.CheckWord();

        NotifyWordChanged();

        string restoredSignature = BuildCurrentNormalCombatPoolSignature();
        bool exactMatch = string.Equals(
            normalCombatPoolSnapshotSignature,
            restoredSignature,
            System.StringComparison.Ordinal);

        if (exactMatch)
        {
            Debug.Log(
                $"[TileManager] Restored the exact Normal Combat pool. Signature: {restoredSignature}",
                this);
        }
        else
        {
            Debug.LogError(
                $"[TileManager] Normal Combat pool restoration mismatch. Expected: {normalCombatPoolSnapshotSignature} | Actual: {restoredSignature}",
                this);
        }

        if (exactMatch)
        {
            normalCombatPoolSnapshot.Clear();
            normalCombatPoolSnapshotSignature = string.Empty;
            hasNormalCombatPoolSnapshot = false;
        }
        else
        {
            Debug.LogError(
                "[TileManager] The Normal Combat snapshot was retained because exact restoration was not verified.",
                this);
        }

        return exactMatch;
    }

    public string GetCurrentNormalCombatPoolSignature()
    {
        return BuildCurrentNormalCombatPoolSignature();
    }

    public bool TryCreateTemporaryTargetChallengePool(
        VocabularyWordData targetWord,
        int maximumPoolTiles = 16)
    {
        if (!hasNormalCombatPoolSnapshot)
        {
            Debug.LogError(
                "[TileManager] Cannot create a Target Challenge pool before the Normal Combat pool is captured.",
                this);
            return false;
        }

        if (HasTemporaryTargetChallengePool || currentMode == TilePoolMode.TargetChallenge)
        {
            Debug.LogWarning(
                "[TileManager] A temporary Target Challenge pool is already active.",
                this);
            return false;
        }

        if (targetWord == null || string.IsNullOrWhiteSpace(targetWord.Word))
        {
            Debug.LogError("[TileManager] Cannot create a Target Challenge pool without target-word data.", this);
            return false;
        }

        if (tileUIPrefab == null || tilePoolPanel == null)
        {
            Debug.LogError(
                "[TileManager] Cannot create a Target Challenge pool because the tile prefab or Tile Pool reference is missing.",
                this);
            return false;
        }

        string targetLetters = NormalizeChallengeLetters(targetWord.Word);
        string distractorLetters = NormalizeChallengeLetters(targetWord.ChallengeDistractorLetters);
        List<char> challengeLetters =
            new List<char>((targetLetters + distractorLetters).ToCharArray());

        if (targetLetters.Length == 0
            || challengeLetters.Count == 0
            || challengeLetters.Count > maximumPoolTiles)
        {
            Debug.LogError(
                $"[TileManager] Invalid Target Challenge configuration for '{targetWord.Word}'. "
                + $"Target letters: {targetLetters.Length}, total letters: {challengeLetters.Count}, maximum: {maximumPoolTiles}.",
                this);
            return false;
        }

        ShuffleLetters(challengeLetters);

        attackLockStateBeforeTargetChallenge =
            attackController != null && attackController.IsAttackInputLocked;
        hasStoredAttackLockState = attackController != null;

        if (attackController != null)
            attackController.SetAttackInputLocked(true);

        temporaryTargetChallengeTiles.Clear();
        activeTargetWord = targetWord;
        currentMode = TilePoolMode.TargetChallenge;
        targetChallengeSubmissionPending = false;
        pendingTargetChallengeWord = string.Empty;

        for (int i = 0; i < challengeLetters.Count; i++)
        {
            Tile tile = CreateTileInPoolInternal(challengeLetters[i]);
            if (tile == null)
            {
                Debug.LogError(
                    $"[TileManager] Target Challenge tile creation failed at index {i}. Removing the incomplete temporary pool.",
                    this);
                TryRemoveTemporaryTargetChallengePool();
                return false;
            }

            tile.gameObject.name = $"TargetChallengeTile_{i:00}_{challengeLetters[i]}";
            ApplyTargetChallengeVisual(tile);
            temporaryTargetChallengeTiles.Add(tile);
        }

        Debug.Log(
            $"[TileManager] Created {temporaryTargetChallengeTiles.Count} temporary Target Challenge tiles "
            + $"for {targetWord.Word}. Shuffled letters: {GetTemporaryTargetChallengeLetters()}. "
            + "Normal replacement spawning remains locked.",
            this);
        return true;
    }

    public void SetPreserveNormalCombatSnapshotAfterTargetChallengeSuccess(bool preserve)
    {
        preserveNormalCombatSnapshotAfterTargetChallengeSuccess = preserve;
    }

    public bool TryRemoveTemporaryTargetChallengePool()
    {
        if (!HasTemporaryTargetChallengePool && currentMode != TilePoolMode.TargetChallenge)
        {
            Debug.LogWarning("[TileManager] No temporary Target Challenge pool is active.", this);
            return false;
        }

        ResetTileSelection();

        for (int i = 0; i < temporaryTargetChallengeTiles.Count; i++)
        {
            Tile tile = temporaryTargetChallengeTiles[i];
            if (tile == null)
                continue;

            tile.gameObject.SetActive(false);
            tile.transform.SetParent(null, false);
            Destroy(tile.gameObject);
        }

        int removedCount = temporaryTargetChallengeTiles.Count;
        temporaryTargetChallengeTiles.Clear();
        targetChallengeSubmissionPending = false;
        pendingTargetChallengeWord = string.Empty;
        activeTargetWord = null;
        currentMode = TilePoolMode.NormalCombat;

        if (hasStoredAttackLockState && attackController != null)
            attackController.SetAttackInputLocked(attackLockStateBeforeTargetChallenge);

        hasStoredAttackLockState = false;
        Debug.Log(
            $"[TileManager] Removed {removedCount} temporary Target Challenge tiles without creating world replacements. "
            + "The saved Normal Combat snapshot remains available.",
            this);
        return true;
    }

    public string GetTemporaryTargetChallengeLetters()
    {
        if (temporaryTargetChallengeTiles.Count == 0)
            return string.Empty;

        System.Text.StringBuilder builder =
            new System.Text.StringBuilder(temporaryTargetChallengeTiles.Count);

        for (int i = 0; i < temporaryTargetChallengeTiles.Count; i++)
        {
            Tile tile = temporaryTargetChallengeTiles[i];
            if (tile != null)
                builder.Append(char.ToUpperInvariant(tile.letter));
        }

        return builder.ToString();
    }

    public bool IsTemporaryTargetChallengeTile(Tile tile)
    {
        return tile != null && temporaryTargetChallengeTiles.Contains(tile);
    }

    public TargetChallengeSubmissionResult TryBeginTargetChallengeSubmission(string submittedWord)
    {
        if (currentMode != TilePoolMode.TargetChallenge
            || !HasTemporaryTargetChallengePool
            || activeTargetWord == null)
        {
            Debug.LogWarning(
                "[TileManager] Ignored Target Challenge submission because no complete challenge is active.",
                this);
            return TargetChallengeSubmissionResult.NotActive;
        }

        string normalizedSubmission = string.IsNullOrWhiteSpace(submittedWord)
            ? string.Empty
            : submittedWord.Trim().ToUpperInvariant();

        if (targetChallengeSubmissionPending)
        {
            Debug.LogWarning(
                "[TileManager] A Target Challenge projectile is already awaiting impact.",
                this);
            return TargetChallengeSubmissionResult.NotActive;
        }

        if (normalizedSubmission.Length == 0 || selectedTiles.Count == 0)
            return TargetChallengeSubmissionResult.NotActive;

        for (int i = 0; i < selectedTiles.Count; i++)
        {
            if (!IsTemporaryTargetChallengeTile(selectedTiles[i]))
            {
                Debug.LogError(
                    "[TileManager] Target Challenge submission contained a tile outside the temporary challenge set.",
                    this);
                return TargetChallengeSubmissionResult.NotActive;
            }
        }

        targetChallengeSubmissionPending = true;
        pendingTargetChallengeWord = normalizedSubmission;
        TargetChallengeCastStarted?.Invoke(activeTargetWord, normalizedSubmission);
        Debug.Log(
            $"[TileManager] Target Challenge cast '{normalizedSubmission}' is frozen until confirmed projectile impact.",
            this);
        return TargetChallengeSubmissionResult.PendingImpact;
    }

    public bool DoesActiveTargetChallengeMatch(VocabularyWordData barrierTargetWord)
    {
        return currentMode == TilePoolMode.TargetChallenge
               && activeTargetWord != null
               && barrierTargetWord != null
               && activeTargetWord.Matches(barrierTargetWord.Word);
    }

    public TargetChallengeSubmissionResult ResolveTargetChallengeImpact(string submittedWord)
    {
        if (currentMode != TilePoolMode.TargetChallenge
            || !HasTemporaryTargetChallengePool
            || activeTargetWord == null
            || !targetChallengeSubmissionPending)
        {
            Debug.LogWarning(
                "[TileManager] Ignored Target Challenge impact because no submission is pending.",
                this);
            return TargetChallengeSubmissionResult.NotActive;
        }

        VocabularyWordData targetWord = activeTargetWord;
        string normalizedSubmission = string.IsNullOrWhiteSpace(submittedWord)
            ? string.Empty
            : submittedWord.Trim().ToUpperInvariant();

        if (!string.Equals(
                pendingTargetChallengeWord,
                normalizedSubmission,
                System.StringComparison.Ordinal))
        {
            Debug.LogError(
                $"[TileManager] Target Challenge impact word mismatch. Pending: '{pendingTargetChallengeWord}' | "
                + $"Impact: '{normalizedSubmission}'.",
                this);
            return TargetChallengeSubmissionResult.NotActive;
        }

        targetChallengeSubmissionPending = false;
        pendingTargetChallengeWord = string.Empty;

        if (!targetWord.Matches(normalizedSubmission))
        {
            ResetTileSelection();
            TargetChallengeSubmissionRejected?.Invoke(targetWord, normalizedSubmission);
            Debug.Log(
                $"[TileManager] Barrier absorbed '{normalizedSubmission}'. Temporary tiles returned without consumption.",
                this);
            return TargetChallengeSubmissionResult.IncorrectTarget;
        }

        if (!TryRemoveTemporaryTargetChallengePool())
        {
            Debug.LogError(
                $"[TileManager] Exact target '{normalizedSubmission}' was recognized, but temporary-pool cleanup failed.",
                this);
            return TargetChallengeSubmissionResult.CleanupFailed;
        }

        if (preserveNormalCombatSnapshotAfterTargetChallengeSuccess)
        {
            TargetChallengeCompleted?.Invoke(targetWord, normalizedSubmission);
            Debug.Log(
                $"[TileManager] Completed chained Target Challenge '{targetWord.Word}'. "
                + "The saved Normal Combat snapshot remains hidden for the next scripted challenge.",
                this);
            return TargetChallengeSubmissionResult.Completed;
        }

        if (!TryRestoreNormalCombatPool())
        {
            TargetChallengeRestorationFailed?.Invoke(targetWord, normalizedSubmission);
            Debug.LogError(
                $"[TileManager] Exact target '{normalizedSubmission}' was recognized, but the saved Normal Combat pool "
                + "could not be verified and restored. Success was not emitted.",
                this);
            return TargetChallengeSubmissionResult.RestorationFailed;
        }

        TargetChallengeCompleted?.Invoke(targetWord, normalizedSubmission);
        Debug.Log(
            $"[TileManager] Completed Target Challenge '{targetWord.Word}' and restored the exact Normal Combat pool.",
            this);
        return TargetChallengeSubmissionResult.Completed;
    }

    public void CancelPendingTargetChallengeSubmission(string reason)
    {
        if (!targetChallengeSubmissionPending)
            return;

        targetChallengeSubmissionPending = false;
        pendingTargetChallengeWord = string.Empty;
        ResetTileSelection();
        Debug.LogWarning(
            $"[TileManager] Target Challenge cast cancelled. Temporary tiles returned without consumption. Reason: {reason}",
            this);
    }

    [ContextMenu("Vocabulary/Layer 3 - Create Debug Target Challenge Pool")]
    private void DebugCreateTemporaryTargetChallengePool()
    {
        if (layer3DebugProfile == null)
        {
            Debug.LogError(
                "[TileManager] Assign Layer 3 Debug Profile before creating a debug Target Challenge pool.",
                this);
            return;
        }

        VocabularyWordData targetWord = layer3DebugProfile.GetWord(layer3DebugWordIndex);
        if (targetWord == null)
        {
            Debug.LogError(
                $"[TileManager] Layer 3 Debug Word Index {layer3DebugWordIndex} is outside the assigned profile.",
                this);
            return;
        }

        TryCreateTemporaryTargetChallengePool(targetWord);
    }

    [ContextMenu("Vocabulary/Layer 3 - Remove Debug Target Challenge Pool")]
    private void DebugRemoveTemporaryTargetChallengePool()
    {
        TryRemoveTemporaryTargetChallengePool();
    }

    [ContextMenu("Vocabulary/Layer 2 - Snapshot and Hide Normal Combat Pool")]
    private void DebugSnapshotAndHideNormalCombatPool()
    {
        TrySnapshotAndHideNormalCombatPool();
    }

    [ContextMenu("Vocabulary/Layer 2 - Restore Normal Combat Pool")]
    private void DebugRestoreNormalCombatPool()
    {
        TryRestoreNormalCombatPool();
    }

    private void ReturnAllWordBarTilesToPool()
    {
        if (tilePoolPanel == null || wordBarPanel == null)
            return;

        List<Tile> tilesToReturn = new List<Tile>();
        HashSet<Tile> seen = new HashSet<Tile>();

        for (int i = 0; i < wordBarPanel.childCount; i++)
        {
            Tile tile = wordBarPanel.GetChild(i).GetComponent<Tile>();
            if (tile != null && seen.Add(tile))
                tilesToReturn.Add(tile);
        }

        foreach (Tile tile in selectedTiles)
        {
            if (tile != null && seen.Add(tile))
                tilesToReturn.Add(tile);
        }

        foreach (Tile tile in wordBarTiles)
        {
            if (tile != null && seen.Add(tile))
                tilesToReturn.Add(tile);
        }

        foreach (Tile tile in tilesToReturn)
            MoveTile(tile, tilePoolPanel);
    }

    private string BuildCurrentNormalCombatPoolSignature()
    {
        if (tilePoolPanel == null)
            return string.Empty;

        List<NormalCombatTileSnapshot> currentEntries = new List<NormalCombatTileSnapshot>();
        for (int siblingIndex = 0; siblingIndex < tilePoolPanel.childCount; siblingIndex++)
        {
            Tile tile = tilePoolPanel.GetChild(siblingIndex).GetComponent<Tile>();
            if (tile == null)
                continue;

            currentEntries.Add(new NormalCombatTileSnapshot
            {
                Tile = tile,
                Letter = char.ToUpperInvariant(tile.letter),
                State = tile.CurrentState,
                DebuffCounter = tile.DebuffCounter,
                SiblingIndex = siblingIndex,
                ActiveSelf = tile.gameObject.activeSelf,
                ButtonInteractable = tile.button != null && tile.button.interactable
            });
        }

        return BuildSnapshotSignature(currentEntries);
    }

    private static string BuildSnapshotSignature(List<NormalCombatTileSnapshot> entries)
    {
        if (entries == null || entries.Count == 0)
            return "<empty>";

        List<string> parts = new List<string>(entries.Count);
        foreach (NormalCombatTileSnapshot entry in entries)
        {
            parts.Add(
                $"{entry.SiblingIndex}:{entry.Letter}:{entry.State}:{entry.DebuffCounter}:{entry.ActiveSelf}:{entry.ButtonInteractable}");
        }

        return string.Join("|", parts);
    }

    private static string NormalizeChallengeLetters(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        System.Text.StringBuilder builder = new System.Text.StringBuilder(value.Length);
        for (int i = 0; i < value.Length; i++)
        {
            char character = char.ToUpperInvariant(value[i]);
            if (character >= 'A' && character <= 'Z')
                builder.Append(character);
        }

        return builder.ToString();
    }

    private static void ShuffleLetters(List<char> letters)
    {
        for (int i = letters.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            char temporary = letters[i];
            letters[i] = letters[randomIndex];
            letters[randomIndex] = temporary;
        }
    }

    private void ApplyTargetChallengeVisual(Tile tile)
    {
        if (tile == null)
            return;

        Image tileImage = tile.GetComponent<Image>();
        if (tileImage != null)
            tileImage.color = targetChallengeTileTint;

        Outline challengeOutline = tile.GetComponent<Outline>();
        if (challengeOutline == null)
            challengeOutline = tile.gameObject.AddComponent<Outline>();

        challengeOutline.effectColor = targetChallengeOutlineColor;
        challengeOutline.effectDistance = targetChallengeOutlineDistance;
        challengeOutline.useGraphicAlpha = true;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            CleanupTilePoolPanel();
        }
    }

    public bool EnsureWordLettersAvailable(string targetWord, int maximumPoolTiles = 16)
    {
        if (currentMode == TilePoolMode.TargetChallenge)
        {
            Debug.LogError(
                "[TileManager] Live-pool letter mutation is blocked while Target Challenge is active.",
                this);
            return false;
        }

        if (string.IsNullOrWhiteSpace(targetWord) || tilePoolPanel == null)
            return false;

        targetWord = targetWord.Trim().ToUpperInvariant();
        Dictionary<char, int> requiredCounts = BuildLetterCounts(targetWord);
        List<Tile> poolTiles = new List<Tile>();

        foreach (Transform child in tilePoolPanel)
        {
            Tile tile = child.GetComponent<Tile>();
            if (tile != null && tile.gameObject.activeInHierarchy && tile.IsSelectable())
                poolTiles.Add(tile);
        }
        int currentPoolTileCount = poolTiles.Count;

        Dictionary<char, int> availableCounts = new Dictionary<char, int>();
        foreach (Tile tile in poolTiles)
            IncrementCount(availableCounts, char.ToUpperInvariant(tile.letter));

        List<char> missingLetters = new List<char>();
        foreach (KeyValuePair<char, int> required in requiredCounts)
        {
            availableCounts.TryGetValue(required.Key, out int available);
            for (int i = available; i < required.Value; i++)
                missingLetters.Add(required.Key);
        }

        foreach (char missingLetter in missingLetters)
        {
            Tile replacement = FindReplacementTile(poolTiles, availableCounts, requiredCounts);
            if (replacement != null)
            {
                char previousLetter = char.ToUpperInvariant(replacement.letter);
                availableCounts[previousLetter] = Mathf.Max(0, availableCounts[previousLetter] - 1);
                SetTileLetterAndSprite(replacement, missingLetter);
                IncrementCount(availableCounts, missingLetter);
                poolTiles.Remove(replacement);
                continue;
            }

            if (currentPoolTileCount < maximumPoolTiles)
            {
                CreateTileInPool(missingLetter);
                IncrementCount(availableCounts, missingLetter);
                currentPoolTileCount++;
                continue;
            }

            Debug.LogWarning($"[TileManager] Could not guarantee all letters for {targetWord}. No safe replacement tile remained.", this);
            ShuffleTilePoolOrder();
            return false;
        }

        ShuffleTilePoolOrder();
        Debug.Log($"[TileManager] Guaranteed shuffled target letters for {targetWord} without displaying their order.", this);
        return true;
    }

    public bool PrepareGuidedWordPool(string targetWord, int poolSize = 16)
    {
        if (currentMode == TilePoolMode.TargetChallenge)
        {
            Debug.LogError(
                "[TileManager] Tutorial guided-pool creation is blocked while Target Challenge is active.",
                this);
            return false;
        }

        if (string.IsNullOrWhiteSpace(targetWord)
            || tilePoolPanel == null
            || poolSize <= 0)
        {
            return false;
        }

        targetWord = targetWord.Trim().ToUpperInvariant();
        List<char> letters = new List<char>();

        foreach (char character in targetWord)
        {
            if (char.IsLetter(character))
                letters.Add(character);
        }

        if (letters.Count == 0 || letters.Count > poolSize)
        {
            Debug.LogWarning(
                $"[TileManager] Cannot prepare guided pool for '{targetWord}' with pool size {poolSize}.",
                this);
            return false;
        }

        int fillerCount = poolSize - letters.Count;
        letters.AddRange(GenerateDictionaryWeightedLetters(fillerCount, currentStage));

        for (int i = letters.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            char temporary = letters[i];
            letters[i] = letters[randomIndex];
            letters[randomIndex] = temporary;
        }

        ClearAllTiles();

        foreach (char letter in letters)
            CreateTileInPool(letter);

        Debug.Log(
            $"[TileManager] Prepared {letters.Count} shuffled tutorial tiles containing {targetWord}.",
            this);
        return true;
    }

    private Dictionary<char, int> BuildLetterCounts(string value)
    {
        Dictionary<char, int> counts = new Dictionary<char, int>();
        foreach (char character in value)
        {
            if (char.IsLetter(character))
                IncrementCount(counts, char.ToUpperInvariant(character));
        }
        return counts;
    }

    private void IncrementCount(Dictionary<char, int> counts, char character)
    {
        if (!counts.ContainsKey(character))
            counts[character] = 0;
        counts[character]++;
    }

    private Tile FindReplacementTile(
        List<Tile> candidates,
        Dictionary<char, int> availableCounts,
        Dictionary<char, int> requiredCounts)
    {
        foreach (Tile candidate in candidates)
        {
            char candidateLetter = char.ToUpperInvariant(candidate.letter);
            availableCounts.TryGetValue(candidateLetter, out int available);
            requiredCounts.TryGetValue(candidateLetter, out int required);
            if (available > required)
                return candidate;
        }

        return null;
    }

    private void SetTileLetterAndSprite(Tile tile, char letter)
    {
        if (tile == null)
            return;

        letter = char.ToUpperInvariant(letter);
        tile.SetLetter(letter);

        Image image = tile.GetComponent<Image>();
        int spriteIndex = letter - 'A';
        if (image != null && letterSpritesUI != null && spriteIndex >= 0 && spriteIndex < letterSpritesUI.Length)
            image.sprite = letterSpritesUI[spriteIndex];
    }

    private void ShuffleTilePoolOrder()
    {
        if (tilePoolPanel == null)
            return;

        List<Transform> children = new List<Transform>();
        foreach (Transform child in tilePoolPanel)
            children.Add(child);

        for (int i = children.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            Transform temporary = children[i];
            children[i] = children[randomIndex];
            children[randomIndex] = temporary;
        }

        for (int i = 0; i < children.Count; i++)
            children[i].SetSiblingIndex(i);

        SetTilePoolPanelGrid();
    }

    private void CleanupTilePoolPanel()
    {
        
        foreach (Transform child in tilePoolPanel)
        {
            MeshRenderer mr = child.GetComponent<MeshRenderer>();
            if (mr != null || child.name.Contains("3d_Tile"))
            {
                Debug.LogWarning("Removing child: " + child.name);
                Destroy(child.gameObject);
            }
            else
            {
                //Debug.Log("Keeping child: " + child.name);
            }

        }
    }

    private void SetTilePoolPanelGrid()
    {
        if (tilePoolGrid == null) return;

        RectTransform rect = tilePoolPanel.GetComponent<RectTransform>();
        float panelWidth = rect.rect.width;
        float panelHeight = rect.rect.height;

        int columns = 8;
        int rows = 2;
        float spacing = 5f;

        float cellWidth = (panelWidth - (spacing * (columns - 1)) - 20f) / columns;
        float cellHeight = (panelHeight - (spacing * (rows - 1)) - 10f) / rows;

        tilePoolGrid.cellSize = new Vector2(cellWidth, cellHeight);
        tilePoolGrid.spacing = new Vector2(spacing, spacing);
        tilePoolGrid.padding.left = 10;
        tilePoolGrid.padding.right = 10;
        tilePoolGrid.padding.top = 5;
        tilePoolGrid.padding.bottom = 5;
        tilePoolGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        tilePoolGrid.constraintCount = columns;
    }

    private void SetWordBarPanelGrid()
    {
        if (wordBarGrid == null) return;

        RectTransform rect = wordBarPanel.GetComponent<RectTransform>();
        float panelWidth = rect.rect.width;
        float panelHeight = rect.rect.height;

        int tileCount = Mathf.Max(1, wordBarTiles.Count);
        float spacing = wordBarSpacing;
        float cellHeight = panelHeight - (wordBarPaddingTop + wordBarPaddingBottom);

        float usableWidth = panelWidth - (wordBarPaddingLeft + wordBarPaddingRight);
        float maxWidthPerTile = (usableWidth - (spacing * (tileCount - 1))) / tileCount;
        float cellWidth = Mathf.Clamp(maxWidthPerTile, wordBarMinCellWidth, wordBarBaseCellWidth);

        wordBarGrid.cellSize = new Vector2(cellWidth, cellHeight);
        wordBarGrid.spacing = new Vector2(spacing, spacing);
        wordBarGrid.padding.left = wordBarPaddingLeft;
        wordBarGrid.padding.right = wordBarPaddingRight;
        wordBarGrid.padding.top = wordBarPaddingTop;
        wordBarGrid.padding.bottom = wordBarPaddingBottom;
        wordBarGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        wordBarGrid.constraintCount = tileCount;
        wordBarGrid.childAlignment = TextAnchor.MiddleCenter;

        ApplyAdaptiveWordBarTextScale(cellWidth);
    }

    private void ApplyAdaptiveWordBarTextScale(float currentCellWidth)
    {
        if (wordBarTiles == null || wordBarTiles.Count == 0) return;

        float scale = Mathf.Clamp(currentCellWidth / Mathf.Max(1f, wordBarBaseCellWidth), wordBarMinFontScale, 1f);

        foreach (Tile tile in wordBarTiles)
        {
            if (tile == null) continue;

            if (tile.textTMP != null)
            {
                if (!wordBarBaseTmpFontSizes.ContainsKey(tile))
                    wordBarBaseTmpFontSizes[tile] = tile.textTMP.fontSize;

                tile.textTMP.fontSize = wordBarBaseTmpFontSizes[tile] * scale;
            }

            if (tile.letterText != null)
            {
                if (!wordBarBaseLegacyFontSizes.ContainsKey(tile))
                    wordBarBaseLegacyFontSizes[tile] = tile.letterText.fontSize;

                tile.letterText.fontSize = Mathf.RoundToInt(wordBarBaseLegacyFontSizes[tile] * scale);
            }
        }
    }
}
