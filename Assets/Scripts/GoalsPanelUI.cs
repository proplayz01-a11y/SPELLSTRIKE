using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GoalsPanelUI : MonoBehaviour
{
    public enum GoalsProfile
    {
        Auto,
        Stage1,
        Stage2
    }

    [Header("Canvas References")]
    public GameObject objectivesCanvas;
    public Canvas targetCanvas;

    [Header("Runtime HUD")]
    public bool buildUIOnStart = true;
    public bool startOpen = false;
    public bool autoActivateInSampleScene = true;

    [Header("Reusable Goal Profile")]
    public GoalsProfile goalsProfile = GoalsProfile.Auto;
    public Stage2ObjectiveTracker stage2ObjectiveTracker;

    [Header("Layout")]
    public Vector2 toggleButtonSize = new Vector2(96f, 42f);
    public Vector2 toggleButtonAnchoredPosition = new Vector2(24f, -96f);
    public Vector2 panelSize = new Vector2(560f, 132f);
    public Vector2 panelAnchoredPosition = new Vector2(136f, -96f);

    [Header("Colors")]
    public Color panelColor = new Color(0.04f, 0.05f, 0.08f, 0.88f);
    public Color headerColor = new Color(0.12f, 0.18f, 0.24f, 0.95f);
    public Color slotColor = new Color(0.14f, 0.15f, 0.18f, 0.92f);
    public Color textColor = Color.white;
    public Color mutedTextColor = new Color(0.72f, 0.75f, 0.78f, 1f);

    [Header("Goal Texts (TMP)")]
    public TextMeshProUGUI goal1Text;
    public TextMeshProUGUI goal2Text;
    public TextMeshProUGUI goal3Text;

    [Header("Goal Colors")]
    public Color defaultGoalColor = Color.white;
    public Color completedGoalColor = Color.green;

    [Header("Objective Manager")]
    public ObjectiveManager objectiveManager;

    [Header("Passive Reward")]
    public bool grantGreatAttractorOnAllGoalsComplete = true;

    private const int Goal1Target = 4;
    private const int Goal3Target = 3;

    private RectTransform uiRoot;
    private GameObject flyoutPanel;
    private Button toggleButton;
    private Button exitButton;
    private TextMeshProUGUI fragmentSummaryText;
    private TextMeshProUGUI spiritSummaryText;
    private TextMeshProUGUI wordSummaryText;
    private bool isOpen;

    private int currentFragments => objectiveManager != null ? objectiveManager.Fragments : 0;
    private int currentLongWords => objectiveManager != null ? objectiveManager.Words : 0;
    private bool goal1Completed => currentFragments >= Goal1Target;
    private bool goal2Completed => objectiveManager != null && objectiveManager.Secondary >= 1;
    private bool goal3Completed => currentLongWords >= Goal3Target;
    private TextMeshProUGUI currentObjectiveText;
    private RectTransform drawerRect;
    private RectTransform drawerViewport;
    private RectTransform edgeTabRect;
    private TextMeshProUGUI edgeTabLabel;
    private float drawerProgress;
    private float drawerWidth;
    private float drawerTop;
    private float tabLeft;
    private float drawerLeft;
    private RectTransform bagButtonRect;
    private float nextBagLookup;
    private Vector2 lastRootSize;
    private Rect lastSafeArea;
    private ObjectiveGuideNotification notification;
    private readonly int[] lastNoticeCounts = new int[3];
    private bool noticeBaselineReady;
    private GameObject journalCanvasObject;
    private RectTransform journalSafeRoot;
    private CanvasGroup journalGroup;
    private Image journalScrim;
    private TextMeshProUGUI journalRewardText;
    private readonly RectTransform[] journalFills = new RectTransform[3];
    private readonly TextMeshProUGUI[] journalDescriptions = new TextMeshProUGUI[3];
    private readonly float[] journalTargets = new float[3];
    private readonly float[] journalValues = new float[3];
    private MovementofPlayer journalMovement;
    private float previousJournalTimeScale;
    private static GoalsPanelUI journalOwner;
    public static bool IsJournalOpen => journalOwner != null;
    private const float JournalWidth = 760f;
    private const float JournalHeight = 680f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        ActivateSupportedSceneGoalsPanel(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ActivateSupportedSceneGoalsPanel(scene);
    }

    private static void ActivateSupportedSceneGoalsPanel(Scene scene)
    {
        if (scene.name != "SampleScene" && scene.name != "Stage2Scene")
            return;

        GoalsPanelUI[] panels = Resources.FindObjectsOfTypeAll<GoalsPanelUI>();
        foreach (GoalsPanelUI panel in panels)
        {
            if (panel == null || !panel.autoActivateInSampleScene)
                continue;

            if (!panel.gameObject.scene.IsValid())
                continue;

            if (panel.gameObject.scene.name != scene.name)
                continue;

            if (!panel.gameObject.activeSelf)
                panel.gameObject.SetActive(true);
        }
    }

    private void Start()
    {
        ResolveReferences();
        ResolveStage2Tracker();
        HideLegacyPanelIfNeeded();

        if (buildUIOnStart)
            BuildUI();

        SetFlyoutOpen(startOpen);
        RefreshGoalTexts();
        if (objectiveManager != null) objectiveManager.ProgressChanged += RefreshGoalTexts;
    }

    private void OnDestroy()
    {
        if (objectiveManager != null) objectiveManager.ProgressChanged -= RefreshGoalTexts;
        ReleaseJournalLock();
        if (journalCanvasObject != null) Destroy(journalCanvasObject);
    }

    private void OnDisable()
    {
        isOpen = false;
        drawerProgress = 0f;
        if (journalCanvasObject != null) journalCanvasObject.SetActive(false);
        ReleaseJournalLock();
    }

    public void ToggleGoalsPanel()
    {
        SetFlyoutOpen(!isOpen);
        Debug.Log($"[GoalsPanelUI] Objectives panel toggled: {(isOpen ? "Opened" : "Closed")}");
    }

    public void OpenGoalsPanel()
    {
        SetFlyoutOpen(true);
    }

    public void CloseGoalsPanel()
    {
        SetFlyoutOpen(false);
        Debug.Log("[GoalsPanelUI] Objectives panel closed.");
    }

    public void UpdateFragmentCount(int current)
    {
        ObjectiveManager.Resolve(gameObject.scene)?.SetLegacyStage1Count(0, current);
    }

    public void IncrementFragmentCount(int amount = 1)
    {
        UpdateFragmentCount(currentFragments + Mathf.Max(0, amount));
    }

    public void CompleteGoal2()
    {
        ObjectiveManager.ReportSpiritMet(gameObject.scene);
    }

    public void UpdateWordCount(int current)
    {
        ObjectiveManager.Resolve(gameObject.scene)?.SetLegacyStage1Count(2, current);
    }

    public void IncrementWordCount(int amount = 1)
    {
        UpdateWordCount(currentLongWords + Mathf.Max(0, amount));
    }

    private void ResolveReferences()
    {
        if (targetCanvas == null)
            targetCanvas = FindCanvasByName("HUDCanvas");

        if (targetCanvas == null)
            targetCanvas = GetComponentInParent<Canvas>();

        if (targetCanvas == null)
            targetCanvas = FindFirstObjectByType<Canvas>();

        if (targetCanvas == null)
            targetCanvas = CreateOverlayCanvas();

        objectiveManager = ObjectiveManager.Resolve(gameObject.scene);
    }

    private bool UsesStage2Profile()
    {
        if (goalsProfile == GoalsProfile.Stage2)
            return true;

        if (goalsProfile == GoalsProfile.Stage1)
            return false;

        return gameObject.scene.IsValid() && gameObject.scene.name == "Stage2Scene";
    }

    private void ResolveStage2Tracker()
    {
        if (!UsesStage2Profile() || stage2ObjectiveTracker != null)
            return;

        stage2ObjectiveTracker = FindFirstObjectByType<Stage2ObjectiveTracker>();
        if (stage2ObjectiveTracker == null)
            stage2ObjectiveTracker = gameObject.AddComponent<Stage2ObjectiveTracker>();
    }

    private void HideLegacyPanelIfNeeded()
    {
        if (objectivesCanvas == null)
            return;

        if (objectivesCanvas == gameObject)
        {
            Transform legacyPanel = transform.Find("ObjectivesPanel");
            if (legacyPanel != null)
                legacyPanel.gameObject.SetActive(false);

            return;
        }

        objectivesCanvas.SetActive(false);
    }

    private Canvas FindCanvasByName(string canvasName)
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            if (canvas != null && canvas.name == canvasName)
                return canvas;
        }

        return null;
    }

    private Canvas CreateOverlayCanvas()
    {
        GameObject canvasObject = new GameObject("GoalsHUDCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 48;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private void BuildUI()
    {
        if (targetCanvas == null || uiRoot != null)
            return;

        uiRoot = CreateRect("GoalsSidebarUI_AutoGenerated", targetCanvas.transform);
        uiRoot.anchorMin = Vector2.zero;
        uiRoot.anchorMax = Vector2.one;
        uiRoot.offsetMin = Vector2.zero;
        uiRoot.offsetMax = Vector2.zero;
        uiRoot.SetAsLastSibling();

        // Keep one edge affordance instead of separate on-screen Goals/Exit buttons.
        uiRoot.gameObject.AddComponent<RectMask2D>();
        toggleButton = CreateButton(uiRoot, "GoalsButton", ">", headerColor, new Vector2(48f, 72f));
        RectTransform toggleRect = toggleButton.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(0f, 1f);
        toggleRect.anchorMax = new Vector2(0f, 1f);
        toggleRect.pivot = new Vector2(0f, 1f);
        edgeTabRect = toggleRect;
        edgeTabLabel = toggleButton.GetComponentInChildren<TextMeshProUGUI>();
        edgeTabLabel.richText = false;
        edgeTabLabel.fontSize = 30f;
        edgeTabLabel.color = new Color(.95f,.79f,.47f);
        toggleButton.onClick.AddListener(ToggleGoalsPanel);

        // Dedicated modal canvas stays above the persistent combat HUD.
        // The original small trigger remains above the Bag.
        journalCanvasObject = new GameObject("QuestJournalModalCanvas", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        SceneManager.MoveGameObjectToScene(journalCanvasObject, gameObject.scene);
        var modalCanvas = journalCanvasObject.GetComponent<Canvas>();
        modalCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        modalCanvas.sortingOrder = 90;
        var modalScaler = journalCanvasObject.GetComponent<CanvasScaler>();
        modalScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        modalScaler.referenceResolution = new Vector2(1920f, 1080f);
        modalScaler.matchWidthOrHeight = 1f;
        journalGroup = journalCanvasObject.GetComponent<CanvasGroup>();
        var scrimRect = CreatePanel(journalCanvasObject.transform, "JournalOutsideDismiss",
            new Color(.015f, .025f, .045f, .56f), Vector2.zero);
        scrimRect.anchorMin = Vector2.zero;
        scrimRect.anchorMax = Vector2.one;
        scrimRect.offsetMin = scrimRect.offsetMax = Vector2.zero;
        journalScrim = scrimRect.GetComponent<Image>();
        var dismiss = scrimRect.gameObject.AddComponent<Button>();
        dismiss.transition = Selectable.Transition.None;
        dismiss.targetGraphic = journalScrim;
        dismiss.onClick.AddListener(CloseGoalsPanel);
        journalSafeRoot = CreateRect("JournalSafeArea", journalCanvasObject.transform);
        journalSafeRoot.offsetMin = journalSafeRoot.offsetMax = Vector2.zero;

        drawerViewport = CreateRect("GoalsDrawerViewport", journalSafeRoot);
        drawerViewport.anchorMin = drawerViewport.anchorMax = new Vector2(0f, 1f);
        drawerViewport.pivot = new Vector2(0f, 1f);
        drawerViewport.gameObject.AddComponent<RectMask2D>();
        flyoutPanel = CreatePanel(drawerViewport, "GoalsFlyout",
            new Color(.045f,.075f,.12f,.99f), new Vector2(JournalWidth, JournalHeight)).gameObject;
        RectTransform flyoutRect = flyoutPanel.GetComponent<RectTransform>();
        flyoutRect.anchorMin = new Vector2(0f, 1f);
        flyoutRect.anchorMax = new Vector2(0f, 1f);
        flyoutRect.pivot = new Vector2(0f, 1f);
        drawerRect = flyoutRect;

        VerticalLayoutGroup panelLayout = flyoutPanel.AddComponent<VerticalLayoutGroup>();
        panelLayout.padding = new RectOffset(24, 24, 20, 20);
        panelLayout.spacing = 12f;
        panelLayout.childControlWidth = true;
        panelLayout.childControlHeight = true;
        panelLayout.childForceExpandWidth = true;
        panelLayout.childForceExpandHeight = false;

        var title = CreateText(flyoutPanel.transform, "ObjectivesHeading",
            UsesStage2Profile() ? "QUEST JOURNAL  |  STAGE II" : "QUEST JOURNAL  |  STAGE I", 28f,
            FontStyles.Bold, new Color(.95f,.79f,.47f));
        title.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;
        title.raycastTarget = false;
        currentObjectiveText = CreateText(flyoutPanel.transform, "CurrentObjective", "", 23f,
            FontStyles.Bold, new Color(.95f,.79f,.47f));
        currentObjectiveText.enableAutoSizing = true;
        currentObjectiveText.fontSizeMin = 18f;
        currentObjectiveText.fontSizeMax = 23f;
        currentObjectiveText.textWrappingMode = TextWrappingModes.Normal;
        currentObjectiveText.raycastTarget = false;
        currentObjectiveText.gameObject.AddComponent<LayoutElement>().preferredHeight = 42f;
        // Preserve the existing goal text references and event-driven updates.
        var scroll = CreateRect("QuestScroll", flyoutPanel.transform);
        var scrollLayout = scroll.gameObject.AddComponent<LayoutElement>();
        scrollLayout.preferredHeight = 350f;
        scrollLayout.flexibleHeight = 1f;
        var scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        var viewport = CreateRect("QuestViewport", scroll);
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = viewport.offsetMax = Vector2.zero;
        viewport.gameObject.AddComponent<RectMask2D>();
        var touchSurface = viewport.gameObject.AddComponent<Image>();
        touchSurface.color = Color.clear;
        var content = CreateRect("QuestRows", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = Vector2.one;
        content.pivot = new Vector2(.5f, 1f);
        content.sizeDelta = new Vector2(0f, 0f);
        content.anchoredPosition = Vector2.zero;
        var rows = content.gameObject.AddComponent<VerticalLayoutGroup>();
        rows.spacing = 10f;
        rows.childControlWidth = rows.childControlHeight = true;
        rows.childForceExpandWidth = true;
        rows.childForceExpandHeight = false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.viewport = viewport;
        scrollRect.content = content;
        // Narrative order for Stage 1: Spirit, fragments, word mastery.
        if (UsesStage2Profile())
        {
            goal1Text = CreateJournalGoal(content, "Goal1Text", 0);
            goal2Text = CreateJournalGoal(content, "Goal2Text", 1);
        }
        else
        {
            goal2Text = CreateJournalGoal(content, "Goal2Text", 1);
            goal1Text = CreateJournalGoal(content, "Goal1Text", 0);
        }
        goal3Text = CreateJournalGoal(content, "Goal3Text", 2);
        journalRewardText = CreateText(flyoutPanel.transform, "CompletionReward", "", 23f,
            FontStyles.Normal, new Color(.95f,.79f,.47f));
        journalRewardText.textWrappingMode = TextWrappingModes.Normal;
        journalRewardText.enableAutoSizing = true;
        journalRewardText.fontSizeMin = 18f;
        journalRewardText.fontSizeMax = 23f;
        journalRewardText.raycastTarget = false;
        journalRewardText.gameObject.AddComponent<LayoutElement>().preferredHeight = 100f;
        exitButton = CreateButton(flyoutPanel.transform, "ExitButton", "Exit to Stage Select",
            headerColor, new Vector2(240f, 42f));
        exitButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;
        exitButton.onClick.AddListener(ExitToStageSelect);
        toggleRect.SetAsLastSibling();

        objectivesCanvas = flyoutPanel;
        notification = uiRoot.gameObject.AddComponent<ObjectiveGuideNotification>();
        notification.Initialize(uiRoot);
        UpdateDrawerLayout(true);
        journalCanvasObject.SetActive(false);
    }

    private TextMeshProUGUI CreateJournalGoal(Transform parent, string name, int index)
    {
        var row = CreatePanel(parent, name + "Panel", new Color(.09f,.13f,.19f,.96f), Vector2.zero);
        row.GetComponent<Image>().raycastTarget = false;
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 114f;
        var title = CreateText(row, name, "", 24f, FontStyles.Bold, textColor);
        title.enableAutoSizing = true;
        title.fontSizeMin = 18f;
        title.fontSizeMax = 24f;
        title.raycastTarget = false;
        PlaceJournalChild(title.rectTransform, .64f, .98f, 14f);
        var description = CreateText(row, "Description", "", 20f, FontStyles.Normal, mutedTextColor);
        description.textWrappingMode = TextWrappingModes.Normal;
        description.enableAutoSizing = true;
        description.fontSizeMin = 16f;
        description.fontSizeMax = 20f;
        description.raycastTarget = false;
        PlaceJournalChild(description.rectTransform, .22f, .64f, 14f);
        journalDescriptions[index] = description;
        var track = CreatePanel(row, "ProgressTrack", new Color(.18f,.24f,.33f), Vector2.zero);
        track.GetComponent<Image>().raycastTarget = false;
        PlaceJournalChild(track, .08f, .14f, 14f);
        var fill = CreatePanel(track, "ProgressFill", new Color(.88f,.68f,.30f), Vector2.zero);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = new Vector2(0f, 1f);
        fill.offsetMin = fill.offsetMax = Vector2.zero;
        fill.GetComponent<Image>().raycastTarget = false;
        journalFills[index] = fill;
        return title;
    }

    private static void PlaceJournalChild(RectTransform rect, float bottom, float top, float margin)
    {
        rect.anchorMin = new Vector2(0f, bottom);
        rect.anchorMax = new Vector2(1f, top);
        rect.offsetMin = new Vector2(margin, 0f);
        rect.offsetMax = new Vector2(-margin, 0f);
    }

    private TextMeshProUGUI CreateDrawerGoal(Transform parent, string objectName)
    {
        var slot = CreatePanel(parent, objectName + "Panel", slotColor, new Vector2(320f, 62f));
        slot.GetComponent<Image>().raycastTarget = false;
        var layout = slot.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 62f;
        layout.minHeight = 40f;
        var text = CreateText(slot, objectName, string.Empty, 18f, FontStyles.Bold, textColor);
        text.enableAutoSizing = true;
        text.fontSizeMin = 12f;
        text.fontSizeMax = 18f;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        var rect = text.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(12f, 5f);
        rect.offsetMax = new Vector2(-10f, -5f);
        return text;
    }

    private void LateUpdate()
    {
        if (drawerRect == null || uiRoot == null) return;
        UpdateDrawerLayout(false);
        if (isOpen && !CanKeepJournalOpen()) CloseGoalsPanel();
        float target = isOpen ? 1f : 0f;
        drawerProgress = Mathf.MoveTowards(drawerProgress, target, Time.unscaledDeltaTime / .20f);
        for (int i = 0; i < journalFills.Length; i++)
        {
            if (journalFills[i] == null) continue;
            journalValues[i] = Mathf.MoveTowards(journalValues[i], journalTargets[i], Time.unscaledDeltaTime * 2f);
            journalFills[i].anchorMax = new Vector2(journalValues[i], 1f);
        }
        ApplyDrawerPosition();
        if (!isOpen && drawerProgress <= 0f)
        {
            if (flyoutPanel.activeSelf) flyoutPanel.SetActive(false);
            if (journalCanvasObject != null && journalCanvasObject.activeSelf) journalCanvasObject.SetActive(false);
            ReleaseJournalLock();
        }
    }

    private void UpdateDrawerLayout(bool force)
    {
        Rect safe = Screen.safeArea;
        if (force || safe != lastSafeArea)
        {
            lastSafeArea = safe;
            uiRoot.anchorMin = new Vector2(safe.xMin / Mathf.Max(1, Screen.width),
                safe.yMin / Mathf.Max(1, Screen.height));
            uiRoot.anchorMax = new Vector2(safe.xMax / Mathf.Max(1, Screen.width),
                safe.yMax / Mathf.Max(1, Screen.height));
            uiRoot.offsetMin = uiRoot.offsetMax = Vector2.zero;
        }
        Vector2 size = uiRoot.rect.size;
        if (force || size != lastRootSize)
        {
            lastRootSize = size;
            drawerWidth = JournalWidth;
            drawerRect.sizeDelta = new Vector2(JournalWidth, JournalHeight);
            drawerViewport.sizeDelta = drawerRect.sizeDelta;
            edgeTabRect.sizeDelta = new Vector2(Mathf.Clamp(size.x * .025f, 42f, 56f), 48f);
        }
        // The persistent Bag lives on a different Canvas (and can have a
        // different scale factor). Convert its screen corners into this
        // safe-area root's local coordinates rather than copying anchors.
        if (bagButtonRect == null || !bagButtonRect.gameObject.activeInHierarchy)
        {
            if (Time.unscaledTime >= nextBagLookup || force)
            {
                nextBagLookup = Time.unscaledTime + .25f;
                var bag = GameObject.Find("PotionBagButton");
                bagButtonRect = bag != null ? bag.transform as RectTransform : null;
            }
        }
        tabLeft = 24f;
        drawerTop = -size.y * .33f;
        drawerLeft = tabLeft + edgeTabRect.sizeDelta.x + 10f;
        if (bagButtonRect != null && bagButtonRect.gameObject.activeInHierarchy)
        {
            var bagCanvas = bagButtonRect.GetComponentInParent<Canvas>();
            var bagCamera = bagCanvas != null && bagCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? bagCanvas.worldCamera : null;
            var goalCamera = targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? targetCanvas.worldCamera : null;
            var bagTopLeft = bagButtonRect.TransformPoint(new Vector3(bagButtonRect.rect.xMin, bagButtonRect.rect.yMax, 0f));
            var bagTopRight = bagButtonRect.TransformPoint(new Vector3(bagButtonRect.rect.xMax, bagButtonRect.rect.yMax, 0f));
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(uiRoot,
                RectTransformUtility.WorldToScreenPoint(bagCamera, bagTopLeft), goalCamera, out var left)
                && RectTransformUtility.ScreenPointToLocalPointInRectangle(uiRoot,
                RectTransformUtility.WorldToScreenPoint(bagCamera, bagTopRight), goalCamera, out var right))
            {
                tabLeft = Mathf.Max(0f, left.x - uiRoot.rect.xMin);
                drawerTop = left.y - uiRoot.rect.yMax + 10f + edgeTabRect.sizeDelta.y;
                drawerLeft = Mathf.Max(tabLeft + edgeTabRect.sizeDelta.x, right.x - uiRoot.rect.xMin) + 10f;
            }
        }
        drawerTop = Mathf.Clamp(drawerTop, -Mathf.Max(0f, size.y - 302f), 0f);
        ApplyDrawerPosition();
    }

    private void ApplyDrawerPosition()
    {
        if (drawerRect == null) return;
        if (journalSafeRoot != null)
        {
            Rect safe = Screen.safeArea;
            journalSafeRoot.anchorMin = new Vector2(safe.xMin / Mathf.Max(1, Screen.width), safe.yMin / Mathf.Max(1, Screen.height));
            journalSafeRoot.anchorMax = new Vector2(safe.xMax / Mathf.Max(1, Screen.width), safe.yMax / Mathf.Max(1, Screen.height));
            journalSafeRoot.offsetMin = journalSafeRoot.offsetMax = Vector2.zero;
            Vector2 available = journalSafeRoot.rect.size;
            float scale = Mathf.Max(.01f, Mathf.Min(available.x * .48f / JournalWidth, available.y * .82f / JournalHeight));
            drawerViewport.localScale = Vector3.one * scale;
            drawerViewport.anchoredPosition = new Vector2(available.x * .055f, -(available.y - JournalHeight * scale) * .5f);
            float eased = ObjectiveGuideNotification.SmoothVisibility(drawerProgress);
            drawerRect.anchoredPosition = new Vector2(JournalWidth * (eased - 1f), 0f);
            journalGroup.alpha = eased;
        }
        edgeTabRect.anchoredPosition = new Vector2(tabLeft, drawerTop);
        edgeTabLabel.text = isOpen ? "<" : ">";
    }

    private void ExitToStageSelect()
    {
        ReleaseJournalLock();
        Time.timeScale = 1f;
        SceneManager.LoadScene("StageSelectScene");
    }

    private void CreateSummaryRow(Transform parent)
    {
        RectTransform row = CreateRow(parent, "GoalsSummaryRow", 36f);
        CreateRowLabel(row, "Goals:");

        fragmentSummaryText = CreateSummarySlot(row, "FragmentsSummary", "Fragments 0/4");
        spiritSummaryText = CreateSummarySlot(row, "SpiritSummary", "Spirit --");
        wordSummaryText = CreateSummarySlot(row, "WordsSummary", "Words 0/3");
    }

    private void CreateGoalDetailRows(Transform parent)
    {
        RectTransform row = CreateRow(parent, "GoalsDetailRow", 70f);
        row.GetComponent<HorizontalLayoutGroup>().spacing = 8f;

        goal1Text = CreateDetailSlot(row, "Goal1Text");
        goal2Text = CreateDetailSlot(row, "Goal2Text");
        goal3Text = CreateDetailSlot(row, "Goal3Text");
    }

    private RectTransform CreateRow(Transform parent, string objectName, float height)
    {
        RectTransform row = CreateRect(objectName, parent);
        LayoutElement rowLayout = row.gameObject.AddComponent<LayoutElement>();
        rowLayout.minHeight = height;
        rowLayout.preferredHeight = height;

        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        return row;
    }

    private void CreateRowLabel(Transform parent, string label)
    {
        TextMeshProUGUI labelText = CreateText(parent, "GoalsLabel", label, 16f, FontStyles.Bold, mutedTextColor);
        LayoutElement layout = labelText.gameObject.AddComponent<LayoutElement>();
        layout.minWidth = 82f;
        layout.preferredWidth = 82f;
    }

    private TextMeshProUGUI CreateSummarySlot(Transform parent, string objectName, string text)
    {
        RectTransform slot = CreatePanel(parent, objectName, slotColor, new Vector2(128f, 34f));
        LayoutElement layout = slot.gameObject.AddComponent<LayoutElement>();
        layout.minWidth = 128f;
        layout.preferredWidth = 128f;
        layout.minHeight = 34f;
        layout.preferredHeight = 34f;

        TextMeshProUGUI slotText = CreateText(slot, "Text", text, 15f, FontStyles.Bold, textColor);
        RectTransform textRect = slotText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        slotText.alignment = TextAlignmentOptions.Center;
        return slotText;
    }

    private TextMeshProUGUI CreateDetailSlot(Transform parent, string objectName)
    {
        RectTransform slot = CreatePanel(parent, objectName + "Panel", slotColor, new Vector2(170f, 66f));
        LayoutElement layout = slot.gameObject.AddComponent<LayoutElement>();
        layout.minWidth = 170f;
        layout.preferredWidth = 170f;
        layout.minHeight = 66f;
        layout.preferredHeight = 66f;

        TextMeshProUGUI text = CreateText(slot, objectName, string.Empty, 13f, FontStyles.Bold, textColor);
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8f, 6f);
        textRect.offsetMax = new Vector2(-8f, -6f);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    private Button CreateButton(Transform parent, string objectName, string text, Color color, Vector2 size)
    {
        RectTransform rect = CreatePanel(parent, objectName, color, size);
        Image image = rect.GetComponent<Image>();
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        colors.pressedColor = new Color(0.84f, 0.84f, 0.84f, 1f);
        colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.75f);
        button.colors = colors;

        TextMeshProUGUI buttonText = CreateText(rect, "Text", text, 16f, FontStyles.Bold, textColor);
        RectTransform textRect = buttonText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        buttonText.alignment = TextAlignmentOptions.Center;
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

    private RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject obj = new GameObject(objectName, typeof(RectTransform));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private TextMeshProUGUI CreateText(Transform parent, string objectName, string text, float fontSize, FontStyles fontStyle, Color color)
    {
        RectTransform rect = CreateRect(objectName, parent);
        TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = fontStyle;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        return tmp;
    }

    private void SetFlyoutOpen(bool open)
    {
        if (open && journalCanvasObject != null)
        {
            if (!CanKeepJournalOpen() || (journalOwner != null && journalOwner != this)
                || (journalOwner != this && Time.timeScale <= 0f)) return;
            if (journalOwner != this)
            {
                journalOwner = this;
                previousJournalTimeScale = Time.timeScale;
                journalMovement = PersistentPlayerRoot.Instance != null ? PersistentPlayerRoot.Instance.Movement : null;
                if (journalMovement != null) journalMovement.AcquireExternalMovementLock();
                Time.timeScale = 0f;
            }
            RefreshJournalReward();
            journalCanvasObject.SetActive(true);
        }
        isOpen = open;

        if (flyoutPanel != null)
        {
            if (drawerRect != null)
            {
                if (isOpen) flyoutPanel.SetActive(true);
                // Closing finishes in LateUpdate; no invisible full-screen input blocker.
                ApplyDrawerPosition();
            }
            else
                flyoutPanel.SetActive(isOpen);
            return;
        }

        if (objectivesCanvas != null && objectivesCanvas != gameObject)
            objectivesCanvas.SetActive(isOpen);
    }

    private bool CanKeepJournalOpen()
    {
        var hud = PersistentHUDRoot.Instance;
        var player = PersistentPlayerRoot.Instance;
        return (hud == null || hud.GameplayHealthVisible)
            && player != null && player.Health != null && player.Health.currentHealth > 0
            && !SpiritRemnantCinematic.IsCinematicActive
            && !Stage1FirstFragmentReveal.IsCinematicActive
            && !Stage1PostJesterEchoCall.IsCinematicActive
            && !Stage1HollowKnightConfrontation.IsCinematicActive;
    }

    private void ReleaseJournalLock()
    {
        if (journalOwner != this) return;
        if (journalMovement != null) journalMovement.ReleaseExternalMovementLock();
        journalMovement = null;
        journalOwner = null;
        if (Time.timeScale == 0f) Time.timeScale = previousJournalTimeScale;
    }

    private void RefreshJournalReward()
    {
        if (journalRewardText == null) return;
        var item = UsesStage2Profile() ? PassiveItemId.PurifyingPearl : PassiveItemId.GreatAttractor;
        var inventory = PassiveItemInventory.Instance;
        bool owned = inventory != null && inventory.HasOwnedPassive(item);
        bool pending = inventory != null && inventory.HasPendingPassive(item);
        journalRewardText.text = "COMPLETION REWARD\n" + PassiveItemCatalog.GetDisplayName(item)
            + (owned ? "  |  Acquired" : pending ? "  |  Earned — pending" : "  |  Locked")
            + "\n" + PassiveItemCatalog.GetEffectSummary(item);
    }

    private void UpdateJournalGoal(int index, int count, int target, string title, string description)
    {
        var label = index == 0 ? goal1Text : index == 1 ? goal2Text : goal3Text;
        if (journalFills[index] == null || label == null) return;
        bool done = count >= target;
        label.text = (done ? "COMPLETE  |  " : "") + title + $"  {count}/{target}";
        label.color = done ? new Color(.55f,.85f,.62f) : new Color(.95f,.87f,.72f);
        journalDescriptions[index].text = description;
        journalTargets[index] = Mathf.Clamp01((float)count / Mathf.Max(1, target));
        journalFills[index].GetComponent<Image>().color = done ? new Color(.55f,.85f,.62f) : new Color(.88f,.68f,.30f);
    }

    private void RefreshGoalTexts()
    {
        if (currentObjectiveText != null && objectiveManager != null)
            currentObjectiveText.text = "NEXT: " + objectiveManager.CurrentObjective;
        if (UsesStage2Profile())
        {
            RefreshStage2GoalTexts();
            if (stage2ObjectiveTracker != null)
                RefreshNotifications(
                    stage2ObjectiveTracker.FragmentsCollected, stage2ObjectiveTracker.FragmentTarget, "Recover fragments",
                    stage2ObjectiveTracker.TenLetterWordsCast, stage2ObjectiveTracker.TenLetterWordTarget, $"Cast {stage2ObjectiveTracker.RequiredLongWordLength}-letter words",
                    stage2ObjectiveTracker.UniqueMaritimeWordsCast, stage2ObjectiveTracker.MaritimeWordTarget, "Cast sea words");
            return;
        }

        string goal1Prefix = goal1Completed ? "Check" : "[ ]";
        string goal2Prefix = goal2Completed ? "Check" : "[ ]";
        string goal3Prefix = goal3Completed ? "Check" : "[ ]";

        if (goal1Text != null)
        {
            goal1Text.text = goal1Completed
                ? $"{goal1Prefix} Collect\nFragments {Goal1Target}/{Goal1Target}"
                : $"{goal1Prefix} Collect\nFragments {currentFragments}/{Goal1Target}";
            goal1Text.color = goal1Completed ? completedGoalColor : defaultGoalColor;
        }

        if (goal2Text != null)
        {
            goal2Text.text = goal2Completed
                ? $"{goal2Prefix} Find\nSpirit Remnant"
                : $"{goal2Prefix} Find\nSpirit Remnant";
            goal2Text.color = goal2Completed ? completedGoalColor : defaultGoalColor;
        }

        if (goal3Text != null)
        {
            goal3Text.text = goal3Completed
                ? $"{goal3Prefix} Spell\n6+ Words {Goal3Target}/{Goal3Target}"
                : $"{goal3Prefix} Spell\n6+ Words {currentLongWords}/{Goal3Target}";
            goal3Text.color = goal3Completed ? completedGoalColor : defaultGoalColor;
        }

        if (fragmentSummaryText != null)
        {
            fragmentSummaryText.text = $"Fragments {currentFragments}/{Goal1Target}";
            fragmentSummaryText.color = goal1Completed ? completedGoalColor : textColor;
        }

        if (spiritSummaryText != null)
        {
            spiritSummaryText.text = goal2Completed ? "Spirit Found" : "Spirit --";
            spiritSummaryText.color = goal2Completed ? completedGoalColor : textColor;
        }

        if (wordSummaryText != null)
        {
            wordSummaryText.text = $"Words {currentLongWords}/{Goal3Target}";
            wordSummaryText.color = goal3Completed ? completedGoalColor : textColor;
        }
        RefreshNotifications(currentFragments, Goal1Target, "Recover fragments",
            goal2Completed ? 1 : 0, 1, "Find Spirit Remnant",
            currentLongWords, Goal3Target, "Cast 6+ letter words");
        UpdateJournalGoal(0, currentFragments, Goal1Target, "Recover Nevali's fragments",
            "Defeat corrupted guardians and recover their soul fragments.");
        UpdateJournalGoal(1, goal2Completed ? 1 : 0, 1, "Find the Spirit Remnant",
            "Discover the truth behind Nevali's disappearance.");
        UpdateJournalGoal(2, currentLongWords, Goal3Target, "Master the power of words",
            "Cast three words containing 6 or more letters.");
        RefreshJournalReward();
    }

    private void RefreshNotifications(int a, int aTarget, string aName,
        int b, int bTarget, string bName, int c, int cTarget, string cName)
    {
        NotifyProgress(0, a, aTarget, aName);
        NotifyProgress(1, b, bTarget, bName);
        NotifyProgress(2, c, cTarget, cName);
        // Initial or restored progress is a baseline, not a new achievement.
        noticeBaselineReady = true;
    }

    private void NotifyProgress(int index, int count, int target, string name)
    {
        int before = lastNoticeCounts[index];
        lastNoticeCounts[index] = count;
        if (!noticeBaselineReady || notification == null || count <= before) return;
        notification.Enqueue(index, count >= target
            ? $"Objective completed!\n{name}"
            : $"{name}\n{count}/{target}");
    }

    private void RefreshStage2GoalTexts()
    {
        if (stage2ObjectiveTracker == null)
            return;

        int fragments = stage2ObjectiveTracker.FragmentsCollected;
        int fragmentTarget = stage2ObjectiveTracker.FragmentTarget;
        int longWords = stage2ObjectiveTracker.TenLetterWordsCast;
        int longWordTarget = stage2ObjectiveTracker.TenLetterWordTarget;
        int requiredLongWordLength = stage2ObjectiveTracker.RequiredLongWordLength;
        int words = stage2ObjectiveTracker.UniqueMaritimeWordsCast;
        int wordTarget = stage2ObjectiveTracker.MaritimeWordTarget;

        bool fragmentsDone = fragments >= fragmentTarget;
        bool longWordDone = longWords >= longWordTarget;
        bool wordsDone = words >= wordTarget;

        SetGoalText(goal1Text, fragmentsDone, $"Collect\nFragments {fragments}/{fragmentTarget}");
        SetGoalText(goal2Text, longWordDone, $"Cast\n{requiredLongWordLength}-Letter {longWords}/{longWordTarget}");
        SetGoalText(goal3Text, wordsDone, $"Cast Sea\nWords {words}/{wordTarget}");

        SetSummaryText(fragmentSummaryText, fragmentsDone, $"Fragments {fragments}/{fragmentTarget}");
        SetSummaryText(spiritSummaryText, longWordDone, $"{requiredLongWordLength}-Letter {longWords}/{longWordTarget}");
        SetSummaryText(wordSummaryText, wordsDone, $"Sea Words {words}/{wordTarget}");
        UpdateJournalGoal(0, fragments, fragmentTarget, "Recover lost fragments", "Recover the fragments hidden in the Sunken Seas.");
        UpdateJournalGoal(1, longWords, longWordTarget, "Master longer words", $"Land {longWordTarget} spell(s) of exactly {requiredLongWordLength} letters.");
        UpdateJournalGoal(2, words, wordTarget, "Speak the language of the sea", $"Land {wordTarget} unique maritime words.");
        RefreshJournalReward();
    }

    private void SetGoalText(TextMeshProUGUI target, bool complete, string body)
    {
        if (target == null)
            return;

        target.text = $"{(complete ? "Check" : "[ ]")} {body}";
        target.color = complete ? completedGoalColor : defaultGoalColor;
    }

    private void SetSummaryText(TextMeshProUGUI target, bool complete, string body)
    {
        if (target == null)
            return;

        target.text = body;
        target.color = complete ? completedGoalColor : textColor;
    }

}
