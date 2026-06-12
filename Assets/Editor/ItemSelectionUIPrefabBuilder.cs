using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ItemSelectionUIPrefabBuilder
{
    private const string PrefabPath = "Assets/Prefabs/UI/ItemSelectionUI.prefab";
    private const string ScenePath = "Assets/Scenes/ItemSelectionScene.unity";
    private const int UiLayer = 5;

    private static readonly Color TitlePlateColor = new Color(0.13f, 0.04f, 0.015f, 0.94f);
    private static readonly Color PanelColor = new Color(0.63f, 0.48f, 0.24f, 0.32f);
    private static readonly Color SlotColor = new Color(0.55f, 0.39f, 0.17f, 0.72f);
    private static readonly Color SelectedSlotColor = new Color(0.62f, 0.44f, 0.17f, 0.88f);
    private static readonly Color TextColor = new Color(0.12f, 0.07f, 0.03f, 1f);
    private static readonly Color MutedTextColor = new Color(0.37f, 0.25f, 0.13f, 1f);
    private static readonly Color GoldColor = new Color(0.92f, 0.66f, 0.12f, 1f);

    [MenuItem("SPELLSTRIKE/UI/Rebuild Item Selection UI Prefab")]
    public static void RebuildPrefabAndPlaceInScene()
    {
        Directory.CreateDirectory("Assets/Prefabs/UI");

        GameObject prefabRoot = BuildPrefabRoot();
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
        Object.DestroyImmediate(prefabRoot);

        AssetDatabase.SaveAssets();
        PlacePrefabInScene();

        Debug.Log($"[ItemSelectionUI] Rebuilt prefab at {PrefabPath} and placed it in ItemSelectionScene.");
    }

    public static void RebuildFromCommandLine()
    {
        RebuildPrefabAndPlaceInScene();
    }

    private static GameObject BuildPrefabRoot()
    {
        RectTransform root = CreateRect("ItemSelectionUI_Prefab", null);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        RectTransform content = CreateRect("WizardVaultContent", root);
        content.anchorMin = new Vector2(0.5f, 0.5f);
        content.anchorMax = new Vector2(0.5f, 0.5f);
        content.pivot = new Vector2(0.5f, 0.5f);
        content.sizeDelta = new Vector2(1520f, 720f);
        content.anchoredPosition = new Vector2(0f, -106f);

        VerticalLayoutGroup contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(18, 18, 0, 0);
        contentLayout.spacing = 18f;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        CreateBody(content);
        CreateFooter(content);
        CreateEditableHeader(root);

        return root.gameObject;
    }

    private static void CreateEditableHeader(Transform parent)
    {
        RectTransform plate = CreatePanel(parent, "EditableVaultHeader", TitlePlateColor, new Vector2(680f, 84f));
        Image plateImage = plate.GetComponent<Image>();
        plateImage.raycastTarget = false;

        plate.anchorMin = new Vector2(0.5f, 1f);
        plate.anchorMax = new Vector2(0.5f, 1f);
        plate.pivot = new Vector2(0.5f, 0.5f);
        plate.anchoredPosition = new Vector2(0f, -86f);

        TextMeshProUGUI title = CreateText(plate, "VaultTitleText", "WIZARD'S VAULT", 28f, FontStyles.Bold, GoldColor);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.42f);
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = new Vector2(20f, 0f);
        titleRect.offsetMax = new Vector2(-20f, -4f);
        title.alignment = TextAlignmentOptions.Center;
        title.characterSpacing = 5f;

        TextMeshProUGUI subtitle = CreateText(plate, "VaultSubtitleText", "PREPARE YOUR LOADOUT", 15f, FontStyles.Bold, GoldColor);
        RectTransform subtitleRect = subtitle.GetComponent<RectTransform>();
        subtitleRect.anchorMin = Vector2.zero;
        subtitleRect.anchorMax = new Vector2(1f, 0.48f);
        subtitleRect.offsetMin = new Vector2(20f, 6f);
        subtitleRect.offsetMax = new Vector2(-20f, 2f);
        subtitle.alignment = TextAlignmentOptions.Center;
        subtitle.characterSpacing = 8f;
    }

    private static void CreateBody(Transform parent)
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

    private static void CreateStagePage(Transform parent)
    {
        RectTransform page = CreatePanel(parent, "StagePage", PanelColor, new Vector2(575f, 500f));
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

        TextMeshProUGUI heading = CreateText(page, "Heading", "SELECTED STAGE", 18f, FontStyles.Bold, MutedTextColor);
        heading.characterSpacing = 7f;
        heading.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

        TextMeshProUGUI stageTitle = CreateText(page, "StageTitle", "STAGE 1: ENCHANTED KINGDOM", 29f, FontStyles.Bold, TextColor);
        stageTitle.textWrappingMode = TextWrappingModes.Normal;
        stageTitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 92f;

        TextMeshProUGUI note = CreateText(page, "StageNote", "Choose up to 3 passive items before entering the stage. Potions are carried into battle and used from the in-game Bag.", 18f, FontStyles.Normal, MutedTextColor);
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

        TextMeshProUGUI potionHeader = CreateText(potionPanel, "PotionHeader", "POTIONS", 16f, FontStyles.Bold, MutedTextColor);
        potionHeader.characterSpacing = 7f;
        potionHeader.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;

        TextMeshProUGUI potionCounts = CreateText(potionPanel, "PotionCounts", "H x0   |   P x0   |   U x0", 22f, FontStyles.Bold, TextColor);
        potionCounts.gameObject.AddComponent<LayoutElement>().preferredHeight = 42f;
    }

    private static void CreateItemPage(Transform parent)
    {
        RectTransform page = CreatePanel(parent, "ItemPage", PanelColor, new Vector2(860f, 500f));
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

        TextMeshProUGUI heading = CreateText(page, "Heading", "PASSIVE ITEMS", 18f, FontStyles.Bold, MutedTextColor);
        heading.characterSpacing = 7f;
        heading.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

        CreateEquippedSlots(page);
        CreatePassiveList(page);
    }

    private static void CreateEquippedSlots(Transform parent)
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

        for (int i = 1; i <= 3; i++)
            CreateEquippedSlot(row, i);
    }

    private static void CreateEquippedSlot(Transform parent, int slotNumber)
    {
        RectTransform slot = CreatePanel(parent, "EquippedSlot" + slotNumber, SelectedSlotColor, new Vector2(244f, 68f));
        LayoutElement layout = slot.gameObject.AddComponent<LayoutElement>();
        layout.minWidth = 244f;
        layout.preferredWidth = 244f;
        layout.minHeight = 68f;
        layout.preferredHeight = 68f;

        TextMeshProUGUI text = CreateText(slot, "Text", "SLOT " + slotNumber + "\nEmpty", 16f, FontStyles.Bold, TextColor);
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 8f);
        textRect.offsetMax = new Vector2(-12f, -8f);
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.Normal;
    }

    private static void CreatePassiveList(Transform parent)
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

        foreach (PassiveItemInfo item in PassiveItemCatalog.GetAllItems())
            CreatePassiveRow(content, item.id + "Row", item.displayName.ToUpperInvariant(), item.effectSummary, "Locked");
    }

    private static void CreatePassiveRow(Transform parent, string objectName, string titleText, string bodyText, string statusText)
    {
        Button button = CreateButton(parent, objectName, string.Empty, SlotColor, new Vector2(0f, 92f));
        LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = 92f;
        layout.preferredHeight = 92f;

        RectTransform rowRect = button.GetComponent<RectTransform>();

        TextMeshProUGUI title = CreateText(rowRect, "Title", titleText, 21f, FontStyles.Bold, TextColor);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.5f);
        titleRect.anchorMax = new Vector2(0.64f, 1f);
        titleRect.offsetMin = new Vector2(18f, -2f);
        titleRect.offsetMax = new Vector2(-8f, -10f);

        TextMeshProUGUI body = CreateText(rowRect, "Body", bodyText, 15f, FontStyles.Italic, MutedTextColor);
        RectTransform bodyRect = body.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(0.74f, 0.54f);
        bodyRect.offsetMin = new Vector2(18f, 10f);
        bodyRect.offsetMax = new Vector2(-8f, 2f);
        body.textWrappingMode = TextWrappingModes.Normal;

        TextMeshProUGUI status = CreateText(rowRect, "Status", statusText, 14f, FontStyles.Bold, GoldColor);
        RectTransform statusRect = status.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.72f, 0f);
        statusRect.anchorMax = new Vector2(1f, 1f);
        statusRect.offsetMin = new Vector2(8f, 0f);
        statusRect.offsetMax = new Vector2(-18f, 0f);
        status.alignment = TextAlignmentOptions.Center;
        status.textWrappingMode = TextWrappingModes.Normal;
    }

    private static void CreateFooter(Transform parent)
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

        CreateButton(footer, "BackButton", "BACK", new Color(0.55f, 0.39f, 0.17f, 0.34f), new Vector2(190f, 56f));
        CreateButton(footer, "ContinueButton", "CONTINUE", SelectedSlotColor, new Vector2(190f, 56f));
    }

    private static Button CreateButton(Transform parent, string objectName, string text, Color color, Vector2 size)
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
            TextMeshProUGUI label = CreateText(rect, "Text", text, 22f, FontStyles.Bold, TextColor);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            label.alignment = TextAlignmentOptions.Center;
        }

        return button;
    }

    private static RectTransform CreatePanel(Transform parent, string objectName, Color color, Vector2 size)
    {
        RectTransform rect = CreateRect(objectName, parent);
        rect.sizeDelta = size;

        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
        return rect;
    }

    private static RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject obj = new GameObject(objectName, typeof(RectTransform));
        obj.layer = UiLayer;

        RectTransform rect = obj.GetComponent<RectTransform>();
        if (parent != null)
            rect.SetParent(parent, false);

        return rect;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string objectName, string text, float fontSize, FontStyles fontStyle, Color color)
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

    private static void PlacePrefabInScene()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[ItemSelectionUI] ItemSelectionScene has no Canvas.");
            return;
        }

        RemoveExistingChild(canvas.transform, "ItemSelectionUI_Prefab");
        RemoveExistingChild(canvas.transform, "ItemSelectionUI_AutoGenerated");

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
        instance.name = "ItemSelectionUI_Prefab";

        RectTransform instanceRect = instance.GetComponent<RectTransform>();
        instanceRect.anchorMin = Vector2.zero;
        instanceRect.anchorMax = Vector2.one;
        instanceRect.offsetMin = Vector2.zero;
        instanceRect.offsetMax = Vector2.zero;
        instanceRect.SetAsLastSibling();

        ItemSelectionSceneController controller = canvas.GetComponent<ItemSelectionSceneController>();
        if (controller == null)
            controller = canvas.gameObject.AddComponent<ItemSelectionSceneController>();

        controller.prebuiltUiRoot = instanceRect;
        controller.buildRuntimeUiIfMissing = false;

        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void RemoveExistingChild(Transform parent, string childName)
    {
        Transform existing = parent.Find(childName);
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);
    }
}
