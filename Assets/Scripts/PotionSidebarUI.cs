using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PotionSidebarUI : MonoBehaviour
{
    [Header("References")]
    public Canvas targetCanvas;
    public PotionInventory potionInventory;
    public PotionSystem potionSystem;
    public PassiveItemInventory passiveItemInventory;

    [Header("Behavior")]
    public bool buildUIOnStart = true;
    public bool startOpen = false;
    public float refreshInterval = 0.2f;
    public bool showTooltipOnHover = true;
    public float holdTooltipDelay = 0.45f;

    [Header("Layout")]
    public Vector2 toggleButtonSize = new Vector2(96f, 42f);
    public Vector2 toggleButtonAnchoredPosition = new Vector2(24f, -150f);
    public Vector2 sidebarSize = new Vector2(520f, 92f);
    public Vector2 sidebarAnchoredPosition = new Vector2(136f, -150f);
    public Vector2 tooltipSize = new Vector2(330f, 74f);
    public Vector2 tooltipAnchoredPosition = new Vector2(136f, -252f);

    [Header("Colors")]
    public Color panelColor = new Color(0.04f, 0.05f, 0.08f, 0.88f);
    public Color headerColor = new Color(0.12f, 0.18f, 0.24f, 0.95f);
    public Color slotColor = new Color(0.14f, 0.15f, 0.18f, 0.92f);
    public Color disabledSlotColor = new Color(0.09f, 0.09f, 0.1f, 0.76f);
    public Color tooltipColor = new Color(0.03f, 0.035f, 0.05f, 0.96f);
    public Color textColor = Color.white;
    public Color mutedTextColor = new Color(0.72f, 0.75f, 0.78f, 1f);
    public Color healthColor = new Color(0.9f, 0.16f, 0.2f, 1f);
    public Color purifyColor = new Color(0.26f, 0.86f, 1f, 1f);
    public Color powerUpColor = new Color(1f, 0.72f, 0.18f, 1f);

    private RectTransform uiRoot;
    private GameObject flyoutPanel;
    private GameObject tooltipPanel;
    private TextMeshProUGUI tooltipTitleText;
    private TextMeshProUGUI tooltipBodyText;
    private Button toggleButton;
    private PotionSlot healthSlot;
    private PotionSlot purifySlot;
    private PotionSlot powerUpSlot;
    private readonly System.Collections.Generic.List<PassiveSlot> passiveSlots = new System.Collections.Generic.List<PassiveSlot>();
    private bool isOpen;
    private float nextRefreshTime;

    private class PotionSlot
    {
        public Button button;
        public Image background;
        public TextMeshProUGUI labelText;
        public PotionType type;
        public string shortLabel;
    }

    private class PassiveSlot
    {
        public TextMeshProUGUI labelText;
        public PotionBagTooltipTrigger tooltipTrigger;
        public int slotIndex;
    }

    private void Start()
    {
        // Stage 2 still has its original scene-authored PotionSidebarUI.
        // When the canonical persistent HUD already owns a bag, leave the
        // old component serialized but don't spawn a duplicate overlay.
        PersistentHUDRoot hud = PersistentHUDRoot.Instance;
        if (hud != null &&
            hud.GetComponentsInChildren<PotionSidebarUI>(true)
                .Any(sidebar => sidebar != null && sidebar != this && sidebar.enabled))
        {
            enabled = false;
            return;
        }

        ResolveReferences();

        // The official HUD now contains the exact same authored Bag hierarchy
        // in Edit Mode. Rebind its non-serialized UI callbacks at runtime
        // rather than building another visible Bag on top of it.
        bool authoredBagFound = BindExistingAuthoredUI();
        if (!authoredBagFound && buildUIOnStart)
            BuildUI();

        SetFlyoutOpen(startOpen);
        Refresh();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefreshTime)
            return;

        nextRefreshTime = Time.unscaledTime + Mathf.Max(0.05f, refreshInterval);
        Refresh();
    }

    public void ToggleSidebar()
    {
        SetFlyoutOpen(!isOpen);
    }

    public void OpenSidebar()
    {
        SetFlyoutOpen(true);
    }

    public void CloseSidebar()
    {
        SetFlyoutOpen(false);
    }

    public void Refresh()
    {
        if (potionInventory == null)
            potionInventory = PotionInventory.Instance != null ? PotionInventory.Instance : FindFirstObjectByType<PotionInventory>();

        if (potionSystem == null)
            potionSystem = PotionSystem.Instance != null ? PotionSystem.Instance : FindFirstObjectByType<PotionSystem>();

        if (passiveItemInventory == null)
            passiveItemInventory = PassiveItemInventory.Instance != null ? PassiveItemInventory.Instance : FindFirstObjectByType<PassiveItemInventory>();

        RefreshPotionSlot(healthSlot);
        RefreshPotionSlot(purifySlot);
        RefreshPotionSlot(powerUpSlot);
        RefreshPassiveSlots();
    }

    public void ShowTooltip(string title, string body)
    {
        if (tooltipPanel == null)
            return;

        tooltipTitleText.text = title;
        tooltipBodyText.text = body;
        tooltipPanel.SetActive(true);
    }

    public void HideTooltip()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    public void HandleTooltipHold(string title, string body)
    {
        ShowTooltip(title, body);
    }

    private void ResolveReferences()
    {
        if (targetCanvas == null)
            targetCanvas = GetComponentInParent<Canvas>();

        if (targetCanvas == null)
            targetCanvas = FindCanvasByName("HUDCanvas");

        if (targetCanvas == null)
            targetCanvas = FindFirstObjectByType<Canvas>();

        if (targetCanvas == null)
            targetCanvas = CreateOverlayCanvas();

        if (potionInventory == null)
            potionInventory = PotionInventory.Instance != null ? PotionInventory.Instance : FindFirstObjectByType<PotionInventory>();

        if (passiveItemInventory == null)
            passiveItemInventory = PassiveItemInventory.Instance != null ? PassiveItemInventory.Instance : FindFirstObjectByType<PassiveItemInventory>();
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
        GameObject canvasObject = new GameObject("PotionHUDCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

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

        uiRoot = CreateRect("PotionSidebarUI_AutoGenerated", targetCanvas.transform);
        uiRoot.anchorMin = Vector2.zero;
        uiRoot.anchorMax = Vector2.one;
        uiRoot.offsetMin = Vector2.zero;
        uiRoot.offsetMax = Vector2.zero;
        uiRoot.SetAsLastSibling();

        toggleButton = CreateButton(uiRoot, "PotionBagButton", "Bag", headerColor, toggleButtonSize);
        RectTransform toggleRect = toggleButton.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(0f, 1f);
        toggleRect.anchorMax = new Vector2(0f, 1f);
        toggleRect.pivot = new Vector2(0f, 1f);
        toggleRect.anchoredPosition = toggleButtonAnchoredPosition;
        toggleButton.onClick.AddListener(ToggleSidebar);

        flyoutPanel = CreatePanel(uiRoot, "BagFlyout", panelColor, sidebarSize).gameObject;
        RectTransform flyoutRect = flyoutPanel.GetComponent<RectTransform>();
        flyoutRect.anchorMin = new Vector2(0f, 1f);
        flyoutRect.anchorMax = new Vector2(0f, 1f);
        flyoutRect.pivot = new Vector2(0f, 1f);
        flyoutRect.anchoredPosition = sidebarAnchoredPosition;

        VerticalLayoutGroup flyoutLayout = flyoutPanel.AddComponent<VerticalLayoutGroup>();
        flyoutLayout.padding = new RectOffset(10, 10, 8, 8);
        flyoutLayout.spacing = 6f;
        flyoutLayout.childControlWidth = true;
        flyoutLayout.childControlHeight = true;
        flyoutLayout.childForceExpandWidth = true;
        flyoutLayout.childForceExpandHeight = false;

        CreatePotionRow(flyoutPanel.transform);
        CreatePassiveRow(flyoutPanel.transform);
        CreateTooltip();
    }

    // A canvas can contain a Bag created by the guarded Editor authoring
    // command. Its UI components survive serialization; fields/callbacks
    // below do not, so recover the same named objects at runtime.
    private bool BindExistingAuthoredUI()
    {
        if (targetCanvas == null)
            return false;

        Transform found = targetCanvas.transform.Find("PotionSidebarUI_AutoGenerated");
        if (found == null)
            return false;

        uiRoot = found as RectTransform;
        if (uiRoot == null)
        {
            Debug.LogError("[PotionSidebarUI] Authored Bag root is not a RectTransform.", this);
            enabled = false;
            return true; // Never overlay a second Bag onto a corrupt authored one.
        }

        toggleButton = uiRoot.Find("PotionBagButton")?.GetComponent<Button>();
        flyoutPanel = uiRoot.Find("BagFlyout")?.gameObject;
        tooltipPanel = uiRoot.Find("BagTooltip")?.gameObject;
        tooltipTitleText = uiRoot.Find("BagTooltip/TooltipTitle")?.GetComponent<TextMeshProUGUI>();
        tooltipBodyText = uiRoot.Find("BagTooltip/TooltipBody")?.GetComponent<TextMeshProUGUI>();

        if (toggleButton == null || flyoutPanel == null || tooltipPanel == null ||
            tooltipTitleText == null || tooltipBodyText == null)
        {
            Debug.LogError("[PotionSidebarUI] Authored Bag is incomplete. Duplicate runtime Bag suppressed.", this);
            enabled = false;
            return true;
        }

        healthSlot = BindAuthoredPotionSlot("H", PotionType.Health);
        purifySlot = BindAuthoredPotionSlot("P", PotionType.Cleansing);
        powerUpSlot = BindAuthoredPotionSlot("U", PotionType.PowerUp);
        passiveSlots.Clear();
        for (int index = 0; index < 3; index++)
        {
            Transform item = uiRoot.Find($"BagFlyout/PassiveRow/Slot{index + 1}PassiveSlot");
            // Imported/scene-authored legacy UI may serialize this auxiliary
            // tooltip component without a valid MonoScript reference. The
            // passive slot itself is required, but the tooltip handler can
            // safely be rebound on that same slot at runtime.
            PotionBagTooltipTrigger tooltip = item?.GetComponent<PotionBagTooltipTrigger>();
            if (tooltip == null && item != null)
                tooltip = item.gameObject.AddComponent<PotionBagTooltipTrigger>();
            passiveSlots.Add(new PassiveSlot
            {
                slotIndex = index,
                labelText = item?.Find("Text")?.GetComponent<TextMeshProUGUI>(),
                tooltipTrigger = tooltip
            });
        }
        if (healthSlot == null || purifySlot == null || powerUpSlot == null ||
            passiveSlots.Any(slot => slot.labelText == null || slot.tooltipTrigger == null))
        {
            Debug.LogError("[PotionSidebarUI] Missing authored potion/passive slot on " +
                gameObject.name + ": H=" + (healthSlot != null) +
                " P=" + (purifySlot != null) + " U=" + (powerUpSlot != null) +
                " passives=" + string.Join(", ", passiveSlots.Select(slot =>
                    (slot.slotIndex + 1) + ":text=" + (slot.labelText != null) +
                    "/tooltip=" + (slot.tooltipTrigger != null))) +
                ". Duplicate runtime Bag suppressed.", this);
            enabled = false;
            return true;
        }

        toggleButton.onClick.AddListener(ToggleSidebar);
        healthSlot.button.onClick.AddListener(() => HandlePotionClicked(PotionType.Health));
        purifySlot.button.onClick.AddListener(() => HandlePotionClicked(PotionType.Cleansing));
        powerUpSlot.button.onClick.AddListener(() => HandlePotionClicked(PotionType.PowerUp));
        Debug.Log("[PotionSidebarUI] Reused and rebound existing authored Bag; no duplicate created.", this);
        return true;
    }

    private PotionSlot BindAuthoredPotionSlot(string shortName, PotionType type)
    {
        Transform slot = uiRoot.Find($"BagFlyout/PotionRow/{shortName}PotionSlot");
        Button button = slot?.GetComponent<Button>();
        Image image = slot?.GetComponent<Image>();
        TextMeshProUGUI label = slot?.Find("Text")?.GetComponent<TextMeshProUGUI>();
        if (button == null || image == null || label == null)
            return null;
        PotionSlot result = new PotionSlot
        {
            button = button, background = image, labelText = label,
            type = type, shortLabel = shortName
        };
        PotionBagTooltipTrigger trigger = slot.GetComponent<PotionBagTooltipTrigger>();
        if (trigger != null)
            trigger.Initialize(this, PotionInventory.GetDisplayName(type),
                type == PotionType.Health ? "Restores 40% of max HP." :
                type == PotionType.Cleansing ? "Removes active debuffs." :
                "Next 2 valid words deal +25% damage.");
        return result;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Author the same Bag used by runtime as normal serialized UI, using an
    /// explicit, source-controlled Editor menu. No Play Mode or inventory read.
    /// </summary>
    public void AuthorEditModeBag(Canvas combatCanvas)
    {
        if (Application.isPlaying || combatCanvas == null)
            throw new System.InvalidOperationException("Authoring requires a real canvas outside Play Mode.");
        if (combatCanvas.transform.Find("PotionSidebarUI_AutoGenerated") != null)
            throw new System.InvalidOperationException("Existing Bag UI must not be duplicated.");
        targetCanvas = combatCanvas;
        buildUIOnStart = true;
        startOpen = false;
        toggleButtonAnchoredPosition = new Vector2(24f, -245f);
        sidebarAnchoredPosition = new Vector2(136f, -245f);
        tooltipAnchoredPosition = new Vector2(136f, -348f);
        BuildUI();
        SetFlyoutOpen(false);
    }
#endif

    private void CreatePotionRow(Transform parent)
    {
        RectTransform row = CreateRow(parent, "PotionRow");
        CreateRowLabel(row, "Potions:");

        healthSlot = CreatePotionSlot(row, PotionType.Health, "H", healthColor, "Health Potion", "Restores 40% of max HP.");
        purifySlot = CreatePotionSlot(row, PotionType.Cleansing, "P", purifyColor, "Purify Potion", "Removes active debuffs.");
        powerUpSlot = CreatePotionSlot(row, PotionType.PowerUp, "U", powerUpColor, "Power Up Potion", "Next 2 valid words deal +25% damage.");
    }

    private void CreatePassiveRow(Transform parent)
    {
        RectTransform row = CreateRow(parent, "PassiveRow");
        CreateRowLabel(row, "Passives:");

        passiveSlots.Clear();
        passiveSlots.Add(CreatePassiveSlot(row, 0, "Slot 1", "Passive Slot 1", "No passive item equipped."));
        passiveSlots.Add(CreatePassiveSlot(row, 1, "Slot 2", "Passive Slot 2", "No passive item equipped."));
        passiveSlots.Add(CreatePassiveSlot(row, 2, "Slot 3", "Passive Slot 3", "No passive item equipped."));
    }

    private RectTransform CreateRow(Transform parent, string objectName)
    {
        RectTransform row = CreateRect(objectName, parent);
        LayoutElement rowLayout = row.gameObject.AddComponent<LayoutElement>();
        rowLayout.minHeight = 36f;
        rowLayout.preferredHeight = 36f;

        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 0, 0);
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
        TextMeshProUGUI labelText = CreateText(parent, label.Replace(":", "") + "Label", label, 16f, FontStyles.Bold, mutedTextColor);
        LayoutElement layout = labelText.gameObject.AddComponent<LayoutElement>();
        layout.minWidth = 82f;
        layout.preferredWidth = 82f;
    }

    private PotionSlot CreatePotionSlot(Transform parent, PotionType type, string shortLabel, Color accentColor, string tooltipTitle, string tooltipBody)
    {
        Button button = CreateButton(parent, shortLabel + "PotionSlot", shortLabel + " x0", slotColor, new Vector2(82f, 34f));
        LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
        layout.minWidth = 82f;
        layout.preferredWidth = 82f;
        layout.minHeight = 34f;
        layout.preferredHeight = 34f;

        TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>();
        text.color = accentColor;

        PotionSlot slot = new PotionSlot
        {
            button = button,
            background = button.GetComponent<Image>(),
            labelText = text,
            type = type,
            shortLabel = shortLabel
        };

        ConfigureTooltip(button.gameObject, tooltipTitle, tooltipBody);
        button.onClick.AddListener(() => HandlePotionClicked(type));
        return slot;
    }

    private PassiveSlot CreatePassiveSlot(Transform parent, int slotIndex, string label, string tooltipTitle, string tooltipBody)
    {
        RectTransform slot = CreatePanel(parent, label.Replace(" ", "") + "PassiveSlot", slotColor, new Vector2(108f, 34f));
        LayoutElement layout = slot.gameObject.AddComponent<LayoutElement>();
        layout.minWidth = 108f;
        layout.preferredWidth = 108f;
        layout.minHeight = 34f;
        layout.preferredHeight = 34f;

        TextMeshProUGUI text = CreateText(slot, "Text", label, 15f, FontStyles.Bold, mutedTextColor);
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        text.alignment = TextAlignmentOptions.Center;

        PotionBagTooltipTrigger tooltipTrigger =
            ConfigureTooltip(slot.gameObject, tooltipTitle, tooltipBody);
        return new PassiveSlot
        {
            labelText = text,
            tooltipTrigger = tooltipTrigger,
            slotIndex = slotIndex
        };
    }

    private void CreateTooltip()
    {
        tooltipPanel = CreatePanel(uiRoot, "BagTooltip", tooltipColor, tooltipSize).gameObject;
        RectTransform tooltipRect = tooltipPanel.GetComponent<RectTransform>();
        tooltipRect.anchorMin = new Vector2(0f, 1f);
        tooltipRect.anchorMax = new Vector2(0f, 1f);
        tooltipRect.pivot = new Vector2(0f, 1f);
        tooltipRect.anchoredPosition = tooltipAnchoredPosition;

        VerticalLayoutGroup layout = tooltipPanel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 8, 8);
        layout.spacing = 2f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        tooltipTitleText = CreateText(tooltipPanel.transform, "TooltipTitle", string.Empty, 16f, FontStyles.Bold, textColor);
        LayoutElement titleLayout = tooltipTitleText.gameObject.AddComponent<LayoutElement>();
        titleLayout.preferredHeight = 22f;

        tooltipBodyText = CreateText(tooltipPanel.transform, "TooltipBody", string.Empty, 13f, FontStyles.Normal, mutedTextColor);
        tooltipBodyText.textWrappingMode = TextWrappingModes.Normal;
        LayoutElement bodyLayout = tooltipBodyText.gameObject.AddComponent<LayoutElement>();
        bodyLayout.preferredHeight = 36f;

        tooltipPanel.SetActive(false);
    }

    private PotionBagTooltipTrigger ConfigureTooltip(GameObject target, string title, string body)
    {
        PotionBagTooltipTrigger trigger = target.AddComponent<PotionBagTooltipTrigger>();
        trigger.Initialize(this, title, body);
        return trigger;
    }

    private void HandlePotionClicked(PotionType type)
    {
        int count = potionInventory != null ? potionInventory.GetCount(type) : 0;
        if (count <= 0)
        {
            Debug.Log($"[PotionSidebarUI] No {PotionInventory.GetDisplayName(type)} available.");
            return;
        }

        if (potionSystem == null)
            potionSystem = PotionSystem.Instance != null ? PotionSystem.Instance : FindFirstObjectByType<PotionSystem>();

        if (potionSystem == null)
        {
            Debug.LogWarning("[PotionSidebarUI] PotionSystem not found. Cannot use potion.");
            return;
        }

        potionSystem.UsePotion(type);
        Refresh();
    }

    private void RefreshPotionSlot(PotionSlot slot)
    {
        if (slot == null)
            return;

        int count = potionInventory != null ? potionInventory.GetCount(slot.type) : 0;
        bool hasPotion = count > 0;

        slot.labelText.text = $"{slot.shortLabel} x{count}";
        slot.background.color = hasPotion ? slotColor : disabledSlotColor;
        slot.labelText.color = hasPotion ? textColor : mutedTextColor;
    }

    private void RefreshPassiveSlots()
    {
        foreach (PassiveSlot slot in passiveSlots)
        {
            if (slot == null || slot.labelText == null)
                continue;

            PassiveItemId itemId = PassiveItemId.None;
            if (passiveItemInventory != null)
                itemId = passiveItemInventory.GetEquippedPassive(slot.slotIndex);

            slot.labelText.text = itemId == PassiveItemId.None
                ? $"Slot {slot.slotIndex + 1}"
                : PassiveItemCatalog.GetShortName(itemId);
            slot.labelText.color = itemId == PassiveItemId.None ? mutedTextColor : textColor;

            if (slot.tooltipTrigger == null)
                continue;

            if (itemId == PassiveItemId.None)
            {
                slot.tooltipTrigger.Initialize(
                    this,
                    $"Passive Slot {slot.slotIndex + 1}",
                    "No passive item equipped.");
                continue;
            }

            PassiveItemInfo item = PassiveItemCatalog.GetInfo(itemId);
            string tooltipBody = string.IsNullOrWhiteSpace(item.effectSummary)
                ? item.description
                : $"{item.description}\n{item.effectSummary}";
            slot.tooltipTrigger.Initialize(this, item.displayName, tooltipBody);
        }
    }

    private void SetFlyoutOpen(bool open)
    {
        isOpen = open;

        if (flyoutPanel != null)
            flyoutPanel.SetActive(isOpen);

        if (!isOpen)
            HideTooltip();
    }

    private RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject obj = new GameObject(objectName, typeof(RectTransform));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
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

        if (!string.IsNullOrEmpty(text))
        {
            TextMeshProUGUI buttonText = CreateText(rect, "Text", text, 16f, FontStyles.Bold, textColor);
            RectTransform textRect = buttonText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            buttonText.alignment = TextAlignmentOptions.Center;
        }

        return button;
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
}

public class PotionBagTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private PotionSidebarUI owner;
    private string title;
    private string body;
    private bool pointerInside;
    private bool pointerHeld;
    private bool holdTooltipShown;
    private float pointerDownTime;

    public void Initialize(PotionSidebarUI tooltipOwner, string tooltipTitle, string tooltipBody)
    {
        owner = tooltipOwner;
        title = tooltipTitle;
        body = tooltipBody;
    }

    private void Update()
    {
        if (!pointerHeld || holdTooltipShown || owner == null)
            return;

        if (Time.unscaledTime - pointerDownTime < owner.holdTooltipDelay)
            return;

        holdTooltipShown = true;
        owner.HandleTooltipHold(title, body);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        if (owner != null && owner.showTooltipOnHover)
            owner.ShowTooltip(title, body);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        pointerHeld = false;
        holdTooltipShown = false;
        owner?.HideTooltip();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pointerHeld = true;
        holdTooltipShown = false;
        pointerDownTime = Time.unscaledTime;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pointerHeld = false;
        holdTooltipShown = false;

        if (!pointerInside)
            owner?.HideTooltip();
    }
}
