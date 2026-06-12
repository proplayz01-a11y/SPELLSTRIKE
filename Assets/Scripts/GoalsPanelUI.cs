using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GoalsPanelUI : MonoBehaviour
{
    [Header("Canvas References")]
    public GameObject objectivesCanvas;
    public Canvas targetCanvas;

    [Header("Runtime HUD")]
    public bool buildUIOnStart = true;
    public bool startOpen = false;
    public bool autoActivateInSampleScene = true;

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
    private TextMeshProUGUI fragmentSummaryText;
    private TextMeshProUGUI spiritSummaryText;
    private TextMeshProUGUI wordSummaryText;
    private bool isOpen;

    private int currentFragments = 0;
    private int currentLongWords = 0;

    private bool goal1Completed = false;
    private bool goal2Completed = false;
    private bool goal3Completed = false;
    private bool allGoalsRewardQueued = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        ActivateSampleSceneGoalsPanel(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ActivateSampleSceneGoalsPanel(scene);
    }

    private static void ActivateSampleSceneGoalsPanel(Scene scene)
    {
        if (scene.name != "SampleScene")
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
        HideLegacyPanelIfNeeded();

        if (buildUIOnStart)
            BuildUI();

        SetFlyoutOpen(startOpen);
        RefreshGoalTexts();
        EvaluateAllGoalsCompleted();
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
        currentFragments = Mathf.Clamp(current, 0, Goal1Target);
        goal1Completed = currentFragments >= Goal1Target;

        RefreshGoalTexts();
        Debug.Log($"[GoalsPanelUI] Goal 1 updated: {currentFragments}/4");
        EvaluateAllGoalsCompleted();
    }

    public void IncrementFragmentCount(int amount = 1)
    {
        UpdateFragmentCount(currentFragments + Mathf.Max(0, amount));
    }

    public void CompleteGoal2()
    {
        goal2Completed = true;

        RefreshGoalTexts();
        Debug.Log("[GoalsPanelUI] Goal 2 completed.");
        EvaluateAllGoalsCompleted();
    }

    public void UpdateWordCount(int current)
    {
        currentLongWords = Mathf.Clamp(current, 0, Goal3Target);
        goal3Completed = currentLongWords >= Goal3Target;

        RefreshGoalTexts();
        Debug.Log($"[GoalsPanelUI] Goal 3 updated: {currentLongWords}/3");
        EvaluateAllGoalsCompleted();
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

        if (objectiveManager == null)
            objectiveManager = FindFirstObjectByType<ObjectiveManager>();
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

        toggleButton = CreateButton(uiRoot, "GoalsButton", "Goals", headerColor, toggleButtonSize);
        RectTransform toggleRect = toggleButton.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(0f, 1f);
        toggleRect.anchorMax = new Vector2(0f, 1f);
        toggleRect.pivot = new Vector2(0f, 1f);
        toggleRect.anchoredPosition = toggleButtonAnchoredPosition;
        toggleButton.onClick.AddListener(ToggleGoalsPanel);

        flyoutPanel = CreatePanel(uiRoot, "GoalsFlyout", panelColor, panelSize).gameObject;
        RectTransform flyoutRect = flyoutPanel.GetComponent<RectTransform>();
        flyoutRect.anchorMin = new Vector2(0f, 1f);
        flyoutRect.anchorMax = new Vector2(0f, 1f);
        flyoutRect.pivot = new Vector2(0f, 1f);
        flyoutRect.anchoredPosition = panelAnchoredPosition;

        VerticalLayoutGroup panelLayout = flyoutPanel.AddComponent<VerticalLayoutGroup>();
        panelLayout.padding = new RectOffset(10, 10, 8, 8);
        panelLayout.spacing = 6f;
        panelLayout.childControlWidth = true;
        panelLayout.childControlHeight = true;
        panelLayout.childForceExpandWidth = true;
        panelLayout.childForceExpandHeight = false;

        CreateSummaryRow(flyoutPanel.transform);
        CreateGoalDetailRows(flyoutPanel.transform);

        objectivesCanvas = flyoutPanel;
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
        isOpen = open;

        if (flyoutPanel != null)
        {
            flyoutPanel.SetActive(isOpen);
            return;
        }

        if (objectivesCanvas != null && objectivesCanvas != gameObject)
            objectivesCanvas.SetActive(isOpen);
    }

    private void RefreshGoalTexts()
    {
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
    }

    private void EvaluateAllGoalsCompleted()
    {
        bool allDone = goal1Completed && goal2Completed && goal3Completed;

        if (objectiveManager != null)
        {
            objectiveManager.allGoalsCompleted = allDone;
            if (allDone)
            {
                Debug.Log("[GoalsPanelUI] All goals completed. ObjectiveManager flag set to true.");
                QueueAllGoalsReward();
            }
        }
        else
        {
            Debug.LogWarning("[GoalsPanelUI] ObjectiveManager reference missing. Cannot set allGoalsCompleted.");
        }
    }

    private void QueueAllGoalsReward()
    {
        if (!grantGreatAttractorOnAllGoalsComplete || allGoalsRewardQueued)
            return;

        allGoalsRewardQueued = true;
        PassiveRewardManager.GrantObjectiveCompletionReward("Objective completion");
    }
}
