using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ItemSelectionSceneController : MonoBehaviour
{
    [Header("Scene")]
    public string stageSelectSceneName = "StageSelectScene";

    [Header("Temporary Debug Keys")]
    public bool enableDebugGrantKeys = true;
    public KeyCode grantSkySwordKey = KeyCode.F6;
    public KeyCode grantGreatAttractorKey = KeyCode.F7;

    [Header("Layout")]
    public Vector2 contentSize = new Vector2(1520f, 720f);
    public Vector2 contentAnchoredPosition = new Vector2(0f, -106f);
    public Vector2 footerButtonSize = new Vector2(190f, 56f);

    [Header("Prefab UI")]
    public RectTransform prebuiltUiRoot;
    public bool buildRuntimeUiIfMissing = true;

    [Header("Editable Header")]
    public bool buildEditableHeader = true;
    public string vaultTitleText = "WIZARD'S VAULT";
    public string vaultSubtitleText = "PREPARE YOUR LOADOUT";
    public Vector2 titlePlateSize = new Vector2(680f, 84f);
    public Vector2 titlePlateAnchoredPosition = new Vector2(0f, -86f);
    public Color titlePlateColor = new Color(0.13f, 0.04f, 0.015f, 0.94f);

    [Header("Colors")]
    public Color pageShadowColor = new Color(0.20f, 0.11f, 0.04f, 1f);
    public Color panelColor = new Color(0.63f, 0.48f, 0.24f, 0.32f);
    public Color slotColor = new Color(0.55f, 0.39f, 0.17f, 0.72f);
    public Color selectedSlotColor = new Color(0.62f, 0.44f, 0.17f, 0.88f);
    public Color lockedSlotColor = new Color(0.58f, 0.46f, 0.27f, 0.24f);
    public Color textColor = new Color(0.12f, 0.07f, 0.03f, 1f);
    public Color mutedTextColor = new Color(0.37f, 0.25f, 0.13f, 1f);
    public Color goldColor = new Color(0.92f, 0.66f, 0.12f, 1f);
    public Color greenColor = new Color(0.23f, 0.48f, 0.14f, 1f);
    public Color redColor = new Color(0.58f, 0.16f, 0.08f, 1f);

    private Canvas targetCanvas;
    private RectTransform uiRoot;
    private PassiveItemInventory passiveInventory;
    private PotionInventory potionInventory;

    private TextMeshProUGUI stageTitleText;
    private TextMeshProUGUI potionCountsText;
    private readonly List<TextMeshProUGUI> equippedSlotTexts = new List<TextMeshProUGUI>();
    private readonly List<PassiveRow> passiveRows = new List<PassiveRow>();

    private class PassiveRow
    {
        public PassiveItemId itemId;
        public Button button;
        public Image background;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI bodyText;
        public TextMeshProUGUI statusText;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureControllerForScene(scene);
    }

    private static void EnsureControllerForScene(Scene scene)
    {
        if (scene.name != "ItemSelectionScene")
            return;

        if (FindFirstObjectByType<ItemSelectionSceneController>() != null)
            return;

        GameObject controllerObject = new GameObject("ItemSelectionSceneController_AutoCreated");
        controllerObject.AddComponent<ItemSelectionSceneController>();
    }

    private void Start()
    {
        ResolveReferences();
        AcceptPendingRewards();
        BindOrBuildUI();
        RefreshUI();
    }

    private void Update()
    {
        if (!enableDebugGrantKeys || passiveInventory == null)
            return;

        bool granted = false;

        if (Input.GetKeyDown(grantSkySwordKey))
            granted |= PassiveRewardManager.GrantSkySwordReward("ItemSelection debug");

        if (Input.GetKeyDown(grantGreatAttractorKey))
            granted |= PassiveRewardManager.GrantObjectiveCompletionReward("ItemSelection debug");

        if (!granted)
            return;

        AcceptPendingRewards();
        RefreshUI();
    }

    private void ResolveReferences()
    {
        GameDatabaseManager.EnsureInstance();

        passiveInventory = PassiveItemInventory.GetOrCreate();

        if (PotionInventory.Instance != null)
            potionInventory = PotionInventory.Instance;
        else
            potionInventory = FindFirstObjectByType<PotionInventory>();

        if (potionInventory == null)
        {
            GameObject potionInventoryObject = new GameObject("PotionInventory_AutoCreated");
            potionInventory = potionInventoryObject.AddComponent<PotionInventory>();
        }

        targetCanvas = FindFirstObjectByType<Canvas>();
        if (targetCanvas == null)
            targetCanvas = CreateCanvas();

        ConfigureCanvasScaler(targetCanvas);
    }

    private void BindOrBuildUI()
    {
        if (targetCanvas == null)
            return;

        if (prebuiltUiRoot == null)
        {
            Transform prefabRoot = targetCanvas.transform.Find("ItemSelectionUI_Prefab");
            if (prefabRoot == null)
                prefabRoot = targetCanvas.transform.Find("ItemSelectionUI_AutoGenerated");

            if (prefabRoot != null)
                prebuiltUiRoot = prefabRoot.GetComponent<RectTransform>();
        }

        if (prebuiltUiRoot != null)
        {
            uiRoot = prebuiltUiRoot;
            uiRoot.SetAsLastSibling();
            CachePrebuiltReferences();
            return;
        }

        if (buildRuntimeUiIfMissing)
            BuildUI();
        else
            Debug.LogWarning("[ItemSelection] No Item Selection UI prefab is assigned.");
    }

    private void CachePrebuiltReferences()
    {
        equippedSlotTexts.Clear();
        passiveRows.Clear();

        stageTitleText = FindUiComponent<TextMeshProUGUI>("WizardVaultContent/Body/StagePage/StageTitle");
        potionCountsText = FindUiComponent<TextMeshProUGUI>("WizardVaultContent/Body/StagePage/PotionSummaryPanel/PotionCounts");

        for (int i = 1; i <= 3; i++)
        {
            TextMeshProUGUI slotText = FindUiComponent<TextMeshProUGUI>($"WizardVaultContent/Body/ItemPage/EquippedSlots/EquippedSlot{i}/Text");
            if (slotText != null)
                equippedSlotTexts.Add(slotText);
        }

        foreach (PassiveItemInfo item in PassiveItemCatalog.GetAllItems())
            CachePassiveRow(item);

        Button backButton = FindUiComponent<Button>("WizardVaultContent/Footer/BackButton");
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackPressed);
        }

        Button continueButton = FindUiComponent<Button>("WizardVaultContent/Footer/ContinueButton");
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinuePressed);
        }
    }

    private void CachePassiveRow(PassiveItemInfo item)
    {
        Button button = FindUiComponent<Button>($"WizardVaultContent/Body/ItemPage/PassiveList/PassiveListViewport/PassiveListContent/{item.id}Row");
        if (button == null)
            button = FindUiComponent<Button>($"WizardVaultContent/Body/ItemPage/PassiveList/{item.id}Row");

        if (button == null)
            return;

        TextMeshProUGUI title = FindChildComponent<TextMeshProUGUI>(button.transform, "Title");
        TextMeshProUGUI body = FindChildComponent<TextMeshProUGUI>(button.transform, "Body");
        TextMeshProUGUI status = FindChildComponent<TextMeshProUGUI>(button.transform, "Status");

        PassiveItemId capturedItemId = item.id;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => HandlePassiveClicked(capturedItemId));

        passiveRows.Add(new PassiveRow
        {
            itemId = item.id,
            button = button,
            background = button.GetComponent<Image>(),
            titleText = title,
            bodyText = body,
            statusText = status
        });
    }

    private T FindUiComponent<T>(string path) where T : Component
    {
        if (uiRoot == null)
            return null;

        Transform found = uiRoot.Find(path);
        return found == null ? null : found.GetComponent<T>();
    }

    private T FindChildComponent<T>(Transform parent, string childName) where T : Component
    {
        if (parent == null)
            return null;

        Transform child = parent.Find(childName);
        return child == null ? null : child.GetComponent<T>();
    }

    private void AcceptPendingRewards()
    {
        if (passiveInventory == null)
            return;

        int promoted = passiveInventory.PromotePendingPassivesToOwned();
        if (promoted > 0)
            Debug.Log($"[ItemSelection] {promoted} pending passive reward(s) became available.");
    }

    private void BuildUI()
    {
        if (targetCanvas == null || uiRoot != null)
            return;

        uiRoot = CreateRect("ItemSelectionUI_AutoGenerated", targetCanvas.transform);
        uiRoot.anchorMin = Vector2.zero;
        uiRoot.anchorMax = Vector2.one;
        uiRoot.offsetMin = Vector2.zero;
        uiRoot.offsetMax = Vector2.zero;
        uiRoot.SetAsLastSibling();

        RectTransform content = CreateRect("WizardVaultContent", uiRoot);
        content.anchorMin = new Vector2(0.5f, 0.5f);
        content.anchorMax = new Vector2(0.5f, 0.5f);
        content.pivot = new Vector2(0.5f, 0.5f);
        content.sizeDelta = contentSize;
        content.anchoredPosition = contentAnchoredPosition;

        VerticalLayoutGroup contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(18, 18, 0, 0);
        contentLayout.spacing = 18f;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        CreateBody(content);
        CreateFooter(content);

        if (buildEditableHeader)
            CreateEditableHeader(uiRoot);
    }

    private void CreateEditableHeader(Transform parent)
    {
        RectTransform plate = CreatePanel(parent, "EditableVaultHeader", titlePlateColor, titlePlateSize);
        Image plateImage = plate.GetComponent<Image>();
        if (plateImage != null)
            plateImage.raycastTarget = false;

        plate.anchorMin = new Vector2(0.5f, 1f);
        plate.anchorMax = new Vector2(0.5f, 1f);
        plate.pivot = new Vector2(0.5f, 0.5f);
        plate.anchoredPosition = titlePlateAnchoredPosition;

        TextMeshProUGUI title = CreateText(plate, "VaultTitleText", vaultTitleText, 28f, FontStyles.Bold, goldColor);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.42f);
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = new Vector2(20f, 0f);
        titleRect.offsetMax = new Vector2(-20f, -4f);
        title.alignment = TextAlignmentOptions.Center;
        title.characterSpacing = 5f;

        TextMeshProUGUI subtitle = CreateText(plate, "VaultSubtitleText", vaultSubtitleText, 15f, FontStyles.Bold, goldColor);
        RectTransform subtitleRect = subtitle.GetComponent<RectTransform>();
        subtitleRect.anchorMin = Vector2.zero;
        subtitleRect.anchorMax = new Vector2(1f, 0.48f);
        subtitleRect.offsetMin = new Vector2(20f, 6f);
        subtitleRect.offsetMax = new Vector2(-20f, 2f);
        subtitle.alignment = TextAlignmentOptions.Center;
        subtitle.characterSpacing = 8f;
    }

    private void CreateBody(Transform parent)
    {
        RectTransform body = CreateRect("Body", parent);
        LayoutElement bodyLayout = body.gameObject.AddComponent<LayoutElement>();
        bodyLayout.minHeight = 530f;
        bodyLayout.preferredHeight = 530f;

        HorizontalLayoutGroup layout = body.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 34f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        CreateStagePage(body);
        CreateItemPage(body);
    }

    private void CreateStagePage(Transform parent)
    {
        RectTransform page = CreatePanel(parent, "StagePage", panelColor, new Vector2(575f, 500f));
        LayoutElement pageLayout = page.gameObject.AddComponent<LayoutElement>();
        pageLayout.minWidth = 575f;
        pageLayout.preferredWidth = 575f;
        pageLayout.minHeight = 500f;
        pageLayout.preferredHeight = 500f;

        VerticalLayoutGroup layout = page.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(26, 26, 22, 22);
        layout.spacing = 14f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TextMeshProUGUI heading = CreateText(page, "Heading", "SELECTED STAGE", 18f, FontStyles.Bold, mutedTextColor);
        heading.characterSpacing = 7f;
        heading.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

        stageTitleText = CreateText(page, "StageTitle", string.Empty, 29f, FontStyles.Bold, textColor);
        stageTitleText.textWrappingMode = TextWrappingModes.Normal;
        stageTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 92f;

        TextMeshProUGUI note = CreateText(page, "StageNote", "Choose up to 3 passive items before entering the stage. Potions are carried into battle and used from the in-game Bag.", 18f, FontStyles.Normal, mutedTextColor);
        note.textWrappingMode = TextWrappingModes.Normal;
        note.gameObject.AddComponent<LayoutElement>().preferredHeight = 118f;

        RectTransform potionPanel = CreatePanel(page, "PotionSummaryPanel", new Color(0.55f, 0.39f, 0.17f, 0.25f), new Vector2(0f, 112f));
        LayoutElement potionLayout = potionPanel.gameObject.AddComponent<LayoutElement>();
        potionLayout.minHeight = 112f;
        potionLayout.preferredHeight = 112f;

        VerticalLayoutGroup potionGroup = potionPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        potionGroup.padding = new RectOffset(16, 16, 12, 12);
        potionGroup.spacing = 6f;
        potionGroup.childControlWidth = true;
        potionGroup.childControlHeight = true;
        potionGroup.childForceExpandWidth = true;
        potionGroup.childForceExpandHeight = false;

        TextMeshProUGUI potionHeader = CreateText(potionPanel, "PotionHeader", "POTIONS", 16f, FontStyles.Bold, mutedTextColor);
        potionHeader.characterSpacing = 7f;
        potionHeader.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;

        potionCountsText = CreateText(potionPanel, "PotionCounts", string.Empty, 22f, FontStyles.Bold, textColor);
        potionCountsText.gameObject.AddComponent<LayoutElement>().preferredHeight = 42f;
    }

    private void CreateItemPage(Transform parent)
    {
        RectTransform page = CreatePanel(parent, "ItemPage", panelColor, new Vector2(860f, 500f));
        LayoutElement pageLayout = page.gameObject.AddComponent<LayoutElement>();
        pageLayout.minWidth = 860f;
        pageLayout.preferredWidth = 860f;
        pageLayout.minHeight = 500f;
        pageLayout.preferredHeight = 500f;

        VerticalLayoutGroup layout = page.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(26, 26, 22, 22);
        layout.spacing = 12f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TextMeshProUGUI heading = CreateText(page, "Heading", "PASSIVE ITEMS", 18f, FontStyles.Bold, mutedTextColor);
        heading.characterSpacing = 7f;
        heading.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

        CreateEquippedSlots(page);
        CreatePassiveList(page);
    }

    private void CreateEquippedSlots(Transform parent)
    {
        RectTransform row = CreateRect("EquippedSlots", parent);
        LayoutElement rowLayout = row.gameObject.AddComponent<LayoutElement>();
        rowLayout.minHeight = 72f;
        rowLayout.preferredHeight = 72f;

        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 12f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        equippedSlotTexts.Clear();
        for (int i = 0; i < 3; i++)
            equippedSlotTexts.Add(CreateEquippedSlot(row, i + 1));
    }

    private TextMeshProUGUI CreateEquippedSlot(Transform parent, int slotNumber)
    {
        RectTransform slot = CreatePanel(parent, "EquippedSlot" + slotNumber, selectedSlotColor, new Vector2(244f, 68f));
        LayoutElement layout = slot.gameObject.AddComponent<LayoutElement>();
        layout.minWidth = 244f;
        layout.preferredWidth = 244f;
        layout.minHeight = 68f;
        layout.preferredHeight = 68f;

        TextMeshProUGUI text = CreateText(slot, "Text", "SLOT " + slotNumber + "\nEmpty", 16f, FontStyles.Bold, textColor);
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 8f);
        textRect.offsetMax = new Vector2(-12f, -8f);
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }

    private void CreatePassiveList(Transform parent)
    {
        RectTransform list = CreatePanel(parent, "PassiveList", new Color(0.55f, 0.39f, 0.17f, 0.18f), new Vector2(0f, 334f));
        LayoutElement listLayout = list.gameObject.AddComponent<LayoutElement>();
        listLayout.minHeight = 334f;
        listLayout.preferredHeight = 334f;

        ScrollRect scrollRect = list.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.inertia = true;
        scrollRect.scrollSensitivity = 34f;

        RectTransform viewport = CreateRect("PassiveListViewport", list);
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(12f, 12f);
        viewport.offsetMax = new Vector2(-12f, -12f);
        viewport.gameObject.AddComponent<RectMask2D>();

        RectTransform content = CreateRect("PassiveListContent", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.spacing = 9f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter contentSizeFitter = content.gameObject.AddComponent<ContentSizeFitter>();
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewport;
        scrollRect.content = content;

        passiveRows.Clear();
        foreach (PassiveItemInfo item in PassiveItemCatalog.GetAllItems())
            passiveRows.Add(CreatePassiveRow(content, item));
    }

    private PassiveRow CreatePassiveRow(Transform parent, PassiveItemInfo item)
    {
        Button button = CreateButton(parent, item.id + "Row", string.Empty, slotColor, new Vector2(0f, 92f));
        LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = 92f;
        layout.preferredHeight = 92f;

        RectTransform rowRect = button.GetComponent<RectTransform>();

        TextMeshProUGUI title = CreateText(rowRect, "Title", item.displayName.ToUpperInvariant(), 21f, FontStyles.Bold, textColor);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.5f);
        titleRect.anchorMax = new Vector2(0.64f, 1f);
        titleRect.offsetMin = new Vector2(18f, -2f);
        titleRect.offsetMax = new Vector2(-8f, -10f);

        TextMeshProUGUI body = CreateText(rowRect, "Body", item.effectSummary, 15f, FontStyles.Italic, mutedTextColor);
        RectTransform bodyRect = body.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(0.74f, 0.54f);
        bodyRect.offsetMin = new Vector2(18f, 10f);
        bodyRect.offsetMax = new Vector2(-8f, 2f);
        body.textWrappingMode = TextWrappingModes.Normal;

        TextMeshProUGUI status = CreateText(rowRect, "Status", string.Empty, 14f, FontStyles.Bold, goldColor);
        RectTransform statusRect = status.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.72f, 0f);
        statusRect.anchorMax = new Vector2(1f, 1f);
        statusRect.offsetMin = new Vector2(8f, 0f);
        statusRect.offsetMax = new Vector2(-18f, 0f);
        status.alignment = TextAlignmentOptions.Center;
        status.textWrappingMode = TextWrappingModes.Normal;

        PassiveRow row = new PassiveRow
        {
            itemId = item.id,
            button = button,
            background = button.GetComponent<Image>(),
            titleText = title,
            bodyText = body,
            statusText = status
        };

        button.onClick.AddListener(() => HandlePassiveClicked(row.itemId));
        return row;
    }

    private void CreateFooter(Transform parent)
    {
        RectTransform footer = CreateRect("Footer", parent);
        LayoutElement footerLayout = footer.gameObject.AddComponent<LayoutElement>();
        footerLayout.minHeight = 72f;
        footerLayout.preferredHeight = 72f;

        HorizontalLayoutGroup layout = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        Button backButton = CreateButton(footer, "BackButton", "BACK", new Color(0.55f, 0.39f, 0.17f, 0.34f), footerButtonSize);
        backButton.onClick.AddListener(OnBackPressed);

        Button continueButton = CreateButton(footer, "ContinueButton", "CONTINUE", selectedSlotColor, footerButtonSize);
        continueButton.onClick.AddListener(OnContinuePressed);
    }

    private void HandlePassiveClicked(PassiveItemId itemId)
    {
        if (passiveInventory == null)
            return;

        if (!passiveInventory.HasOwnedPassive(itemId))
        {
            Debug.Log($"[ItemSelection] Passive locked: {PassiveItemCatalog.GetDisplayName(itemId)}.");
            return;
        }

        if (passiveInventory.IsEquipped(itemId))
            passiveInventory.TryUnequipPassive(itemId);
        else
            passiveInventory.TryEquipPassive(itemId);

        RefreshUI();
    }

    private void RefreshUI()
    {
        if (stageTitleText != null)
            stageTitleText.text = StageRunSelection.SelectedStageTitle;

        if (potionCountsText != null && potionInventory != null)
        {
            potionCountsText.text = $"H x{potionInventory.GetCount(PotionType.Health)}   |   P x{potionInventory.GetCount(PotionType.Cleansing)}   |   U x{potionInventory.GetCount(PotionType.PowerUp)}";
        }

        RefreshEquippedSlots();
        RefreshPassiveRows();
    }

    private void RefreshEquippedSlots()
    {
        if (passiveInventory == null)
            return;

        for (int i = 0; i < equippedSlotTexts.Count; i++)
        {
            PassiveItemId itemId = passiveInventory.GetEquippedPassive(i);
            string itemName = itemId == PassiveItemId.None ? "Empty" : PassiveItemCatalog.GetDisplayName(itemId);
            if (equippedSlotTexts[i] == null)
                continue;

            equippedSlotTexts[i].text = $"Slot {i + 1}\n{itemName}";
            equippedSlotTexts[i].color = itemId == PassiveItemId.None ? mutedTextColor : textColor;
        }
    }

    private void RefreshPassiveRows()
    {
        if (passiveInventory == null)
            return;

        foreach (PassiveRow row in passiveRows)
        {
            bool owned = passiveInventory.HasOwnedPassive(row.itemId);
            bool equipped = passiveInventory.IsEquipped(row.itemId);

            if (equipped)
            {
                if (row.background != null)
                    row.background.color = selectedSlotColor;
                if (row.statusText != null)
                {
                    row.statusText.text = "EQUIPPED\nClick to remove";
                    row.statusText.color = goldColor;
                }
                if (row.titleText != null)
                    row.titleText.color = textColor;
                if (row.bodyText != null)
                    row.bodyText.color = textColor;
                if (row.button != null)
                    row.button.interactable = true;
            }
            else if (owned)
            {
                if (row.background != null)
                    row.background.color = slotColor;
                if (row.statusText != null)
                {
                    row.statusText.text = "+ EQUIP";
                    row.statusText.color = greenColor;
                }
                if (row.titleText != null)
                    row.titleText.color = textColor;
                if (row.bodyText != null)
                    row.bodyText.color = mutedTextColor;
                if (row.button != null)
                    row.button.interactable = true;
            }
            else
            {
                if (row.background != null)
                    row.background.color = lockedSlotColor;
                if (row.statusText != null)
                {
                    row.statusText.text = "Locked";
                    row.statusText.color = mutedTextColor;
                }
                if (row.titleText != null)
                    row.titleText.color = mutedTextColor;
                if (row.bodyText != null)
                    row.bodyText.color = mutedTextColor;
                if (row.button != null)
                    row.button.interactable = true;
            }
        }
    }

    private void OnContinuePressed()
    {
        GameDatabaseManager database = GameDatabaseManager.EnsureInstance();
        database.FlushSave();

        Debug.Log($"[ItemSelection] Continuing to {StageRunSelection.SelectedStageSceneName}.");
        StageRunSelection.LoadSelectedStage();
    }

    private void OnBackPressed()
    {
        Debug.Log("[ItemSelection] Returning to Stage Select.");
        SceneManager.LoadScene(stageSelectSceneName);
    }

    private Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("ItemSelectionCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        return canvas;
    }

    private void ConfigureCanvasScaler(Canvas canvas)
    {
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();
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
        colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.72f);
        button.colors = colors;

        if (!string.IsNullOrEmpty(text))
        {
            TextMeshProUGUI label = CreateText(rect, "Text", text, 22f, FontStyles.Bold, textColor);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            label.alignment = TextAlignmentOptions.Center;
        }

        return button;
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
        tmp.raycastTarget = false;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        return tmp;
    }
}
