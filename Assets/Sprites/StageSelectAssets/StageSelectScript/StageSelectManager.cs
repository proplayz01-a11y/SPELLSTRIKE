using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// ============================================================
//  StageSelectManager.cs — SpellStrike
//  Attach to a GameObject named "StageSelectManager" in your
//  Stage Select scene.
// ============================================================

public class StageSelectManager : MonoBehaviour
{
    // --------------------------------------------------------
    // Inspector References
    // --------------------------------------------------------

    [Header("Stage Node Buttons (assign in Inspector)")]
    public Button[] stageButtons;          // 5 buttons, index 0 = Stage 1

    [Header("Stage Node Artwork (Image components on each node)")]
    public Image[] stageNodeImages;        // Same order as stageButtons

    [Header("Lock Overlays (semi-transparent dark Image over each node)")]
    public GameObject[] lockOverlays;      // GameObject with Image + lock icon

    [Header("Sparkle Effects (ParticleSystem per node)")]
    public ParticleSystem[] sparkleEffects; // One per stage node

    [Header("Checkmark Objects (tick icon shown on completed stages)")]
    public GameObject[] checkmarks;        // One per stage node

    [Header("Dotted Path Segments (Image or LineRenderer per segment)")]
    public GameObject[] pathSegments;      // S1→S2, S2→S3, S3→S4, S4→S5

    [Header("Stage Info Panels (one GameObject per stage — Stage1_Info to Stage5_Info)")]
    public GameObject[] stageInfoPanels;   // Assign Stage1_Info, Stage2_Info, etc.

    [Header("Stage Preview Image (updates when stage is selected)")]
    public Image infoStagePreview;
    public Sprite[] stagePreviewSprites;   // One sprite per stage

    [Header("Enter Button")]
    public Button enterButton;

    [Header("Lock Warning Popup")]
    public GameObject lockWarningPopup;    // Panel with "Stage Locked!" text
    public float lockWarningDuration = 2f;

    [Header("Canvas Scaler (auto-assigned)")]
    public CanvasScaler canvasScaler;

    [Header("Scene Names (must match Build Settings exactly)")]
    public string[] stageSceneNames = {
        "SampleScene",
        "Stage2Scene",
        "Stage3_MythicRuins",
        "Stage4_CursedGothic",
        "Stage5_Final"
    };
    public string itemSelectionSceneName = "ItemSelectionScene";

    // --------------------------------------------------------
    // Stage Data
    // --------------------------------------------------------

    private struct StageInfo
    {
        public string title;
        public string enemies;
        public string boss;
        public string flavour;
    }

    private StageInfo[] stageInfoData = new StageInfo[]
    {
        new StageInfo {
            title    = "Stage 1: Enchanted Kingdom",
            enemies  = "Bramble Sprite  •  Stone Sentinel  •  Cursed Jester",
            boss     = "Boss: Hollow Knight",
            flavour  = "Tutorial stage — no debuffs. Your journey into the living book begins."
        },
        new StageInfo {
            title    = "Stage 2: Sunken Seas",
            enemies  = "Barnacle Husk  •  Corsair Phantom  •  Tide Wraith",
            boss     = "Boss: Drowned Captain (summons Barnacle Husks)",
            flavour  = "All 3 debuffs active: Tile Locking, Cracking & Timed Pressure."
        },
        new StageInfo {
            title    = "Stage 3: Mythic Ruins",
            enemies  = "Runic Golem  •  Specter Archer  •  Minotaur Shade",
            boss     = "Boss: Titan Colossus — 3 phases, Rune Shield",
            flavour  = "Something feels wrong. You find the Master Wizard's seal..."
        },
        new StageInfo {
            title    = "Stage 4: Cursed Gothic",
            enemies  = "Plague Wraith  •  Vampiric Shade  •  Gargoyle Sentinel",
            boss     = "Boss: Crimson Duchess — transforms to shadow dragon at 25% HP",
            flavour  = "Full rage. The truth is finally revealed."
        },
        new StageInfo {
            title    = "Stage 5: ???",
            enemies  = "Unknown enemies await...",
            boss     = "Final Boss: Master Wizard",
            flavour  = "Nevali is close. This ends now."
        }
    };

    // --------------------------------------------------------
    // Private State
    // --------------------------------------------------------

    private int selectedStage = -1;        // 0-indexed, -1 = none selected
    private int highestUnlocked = 0;       // 0-indexed
    private Coroutine lockWarnCoroutine;

    // PlayerPrefs keys
    private const string PREF_HIGHEST_STAGE = "HighestStageUnlocked";

    // --------------------------------------------------------
    // Unity Lifecycle
    // --------------------------------------------------------

    void Awake()
    {
        SetupCanvasScaler();
    }

    void Start()
    {
        LoadProgress();
        InitialiseNodes();
        InitialisePathSegments();
        HideAllInfoPanels();

        // Enter button disabled until stage is selected
        if (enterButton != null)
            enterButton.interactable = false;

        // Auto-select the highest unlocked stage (current stage to play)
        SelectStage(highestUnlocked);

        // Hide lock warning at start
        if (lockWarningPopup != null)
            lockWarningPopup.SetActive(false);
    }

    // --------------------------------------------------------
    // Canvas Scaler — landscape, scale with screen size
    // --------------------------------------------------------

    void SetupCanvasScaler()
    {
        if (canvasScaler == null)
            canvasScaler = FindObjectOfType<CanvasScaler>();

        if (canvasScaler != null)
        {
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080); // PC base
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f; // balance width & height scaling
        }
    }

    // --------------------------------------------------------
    // Progress — save & load via PlayerPrefs
    // --------------------------------------------------------

    void LoadProgress()
    {
        // Default: only Stage 1 unlocked
        highestUnlocked = PlayerPrefs.GetInt(PREF_HIGHEST_STAGE, 0);
        highestUnlocked = Mathf.Clamp(highestUnlocked, 0, stageSceneNames.Length - 1);
    }

    // Call this from your GameManager after a stage is cleared
    public static void CompleteStage(int stageIndex)
    {
        int current = PlayerPrefs.GetInt(PREF_HIGHEST_STAGE, 0);
        if (stageIndex + 1 > current)
        {
            PlayerPrefs.SetInt(PREF_HIGHEST_STAGE, stageIndex + 1);
            PlayerPrefs.Save();
        }
    }

    // For testing in editor — unlocks all stages
    [ContextMenu("DEBUG — Unlock All Stages")]
    void DebugUnlockAll()
    {
        PlayerPrefs.SetInt(PREF_HIGHEST_STAGE, stageSceneNames.Length - 1);
        PlayerPrefs.Save();
        LoadProgress();
        InitialiseNodes();
        InitialisePathSegments();
        SelectStage(highestUnlocked);
    }

    [ContextMenu("DEBUG — Reset Progress")]
    void DebugResetProgress()
    {
        PlayerPrefs.SetInt(PREF_HIGHEST_STAGE, 0);
        PlayerPrefs.Save();
        LoadProgress();
        InitialiseNodes();
        InitialisePathSegments();
        SelectStage(0);
    }

    // --------------------------------------------------------
    // Node Initialisation
    // --------------------------------------------------------

    void InitialiseNodes()
    {
        for (int i = 0; i < stageButtons.Length; i++)
        {
            bool isUnlocked = i <= highestUnlocked;
            bool isCompleted = i < highestUnlocked;

            // --- Button interactability ---
            if (stageButtons[i] != null)
            {
                int capturedIndex = i; // capture for lambda
                stageButtons[i].onClick.RemoveAllListeners();

                if (isUnlocked)
                    stageButtons[i].onClick.AddListener(() => SelectStage(capturedIndex));
                else
                    stageButtons[i].onClick.AddListener(() => ShowLockedWarning());

                stageButtons[i].interactable = true; // always "clickable" — we handle lock in code
            }

            // --- Grayscale on locked nodes ---
            if (stageNodeImages[i] != null)
            {
                stageNodeImages[i].color = isUnlocked
                    ? Color.white
                    : new Color(0.4f, 0.4f, 0.4f, 1f); // desaturated
            }

            // --- Lock overlay ---
            if (i < lockOverlays.Length && lockOverlays[i] != null)
                lockOverlays[i].SetActive(!isUnlocked);

            // --- Checkmark ---
            if (i < checkmarks.Length && checkmarks[i] != null)
                checkmarks[i].SetActive(isCompleted);

            // --- Sparkle — stop all first ---
            if (i < sparkleEffects.Length && sparkleEffects[i] != null)
                sparkleEffects[i].Stop();
        }
    }

    // --------------------------------------------------------
    // Path Segment Visibility
    // --------------------------------------------------------

    void InitialisePathSegments()
    {
        // pathSegments[0] = S1→S2, [1] = S2→S3, etc.
        for (int i = 0; i < pathSegments.Length; i++)
        {
            if (pathSegments[i] == null) continue;
            // Show path segment if the destination stage is unlocked
            bool show = (i + 1) <= highestUnlocked;
            pathSegments[i].SetActive(show);
        }
    }

    // --------------------------------------------------------
    // Info Panel — Hide All
    // --------------------------------------------------------

    void HideAllInfoPanels()
    {
        foreach (var panel in stageInfoPanels)
            if (panel != null) panel.SetActive(false);
    }

    // --------------------------------------------------------
    // Stage Selection
    // --------------------------------------------------------

    public void SelectStage(int index)
    {
        if (index < 0 || index >= stageButtons.Length) return;
        if (index > highestUnlocked)
        {
            ShowLockedWarning();
            return;
        }

        // Stop sparkle on previously selected
        if (selectedStage >= 0 && selectedStage < sparkleEffects.Length)
            if (sparkleEffects[selectedStage] != null)
                sparkleEffects[selectedStage].Stop();

        selectedStage = index;

        // Start sparkle on newly selected
        if (selectedStage < sparkleEffects.Length)
            if (sparkleEffects[selectedStage] != null)
                sparkleEffects[selectedStage].Play();

        // Update info panel
        UpdateInfoPanel(index);

        // Activate enter button
        if (enterButton != null)
            enterButton.interactable = true;
    }

    // --------------------------------------------------------
    // Info Panel Update
    // --------------------------------------------------------

    void UpdateInfoPanel(int index)
    {
        // Hide all first
        HideAllInfoPanels();

        // Show the selected stage's info panel
        if (index < stageInfoPanels.Length && stageInfoPanels[index] != null)
            stageInfoPanels[index].SetActive(true);

        // Update preview image
        if (infoStagePreview != null && index < stagePreviewSprites.Length && stagePreviewSprites[index] != null)
            infoStagePreview.sprite = stagePreviewSprites[index];
    }

    // --------------------------------------------------------
    // Enter Button — called by Enter Stage button's onClick
    // --------------------------------------------------------

    public void OnEnterPressed()
    {
        if (selectedStage < 0 || selectedStage > highestUnlocked) return;

        string sceneName = stageSceneNames[selectedStage];
        string stageTitle = selectedStage < stageInfoData.Length ? stageInfoData[selectedStage].title : sceneName;
        StageRunSelection.SetSelectedStage(selectedStage, sceneName, stageTitle);
        StartCoroutine(LoadStageScene(itemSelectionSceneName));
    }

    IEnumerator LoadStageScene(string sceneName)
    {
        // Optional: play transition animation here
        // e.g. Animator on a black overlay panel

        yield return new WaitForSeconds(0.1f);

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone)
            yield return null;
    }

    // --------------------------------------------------------
    // Lock Warning Popup
    // --------------------------------------------------------

    void ShowLockedWarning()
    {
        if (lockWarningPopup == null) return;

        if (lockWarnCoroutine != null)
            StopCoroutine(lockWarnCoroutine);

        lockWarnCoroutine = StartCoroutine(LockWarnRoutine());
    }

    IEnumerator LockWarnRoutine()
    {
        lockWarningPopup.SetActive(true);
        yield return new WaitForSeconds(lockWarningDuration);
        lockWarningPopup.SetActive(false);
    }

    // --------------------------------------------------------
    // Back Button — called by Back button's onClick
    // --------------------------------------------------------

    public void OnBackPressed()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
