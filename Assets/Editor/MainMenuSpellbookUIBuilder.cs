using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MainMenuSpellbookUIBuilder
{
    private const string ScenePath = "Assets/Scenes/Main Menu Scene.unity";
    private const string PrefabFolder = "Assets/Prefabs/UI";
    private const string PrefabPath =
        PrefabFolder + "/MainMenuSpellbookUI.prefab";
    private const string RootName = "MainMenuSpellbookUI";

    private static readonly Color Navy =
        new Color(0.025f, 0.065f, 0.14f, 0.98f);
    private static readonly Color NavyLight =
        new Color(0.055f, 0.12f, 0.22f, 0.98f);
    private static readonly Color Parchment =
        new Color(0.93f, 0.91f, 0.79f, 1f);
    private static readonly Color Gold =
        new Color(0.96f, 0.67f, 0.16f, 1f);
    private static readonly Color Cyan =
        new Color(0.37f, 0.88f, 1f, 1f);

    [MenuItem("SPELLSTRIKE/UI/Build Spellbook UI Prefab")]
    public static void BuildPrefabMenu()
    {
        BuildPrefabAsset();
    }

    private static GameObject BuildPrefabAsset()
    {
        EnsurePrefabFolder();

        GameObject root = BuildHierarchy();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = prefab;
        Debug.Log($"[SpellbookUI] Created or updated {PrefabPath}.");
        return prefab;
    }

    [MenuItem("SPELLSTRIKE/UI/Build And Install Main Menu Spellbook UI")]
    public static void BuildAndInstall()
    {
        GameObject prefab = BuildPrefabAsset();
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            Debug.LogError("[SpellbookUI] Main Menu Canvas was not found.");
            return;
        }

        Transform existing = canvas.transform.Find(RootName);
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        GameObject instance =
            (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
        instance.name = RootName;

        RectTransform rect = instance.GetComponent<RectTransform>();
        Stretch(rect);
        rect.localScale = Vector3.one;
        rect.SetAsLastSibling();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = instance;

        Debug.Log("[SpellbookUI] Installed the clickable Spellbook UI in the Main Menu scene.");
    }

    private static GameObject BuildHierarchy()
    {
        GameObject root = CreateUIObject(RootName, null);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        Stretch(rootRect);

        MainMenuSpellbookUI controller =
            root.AddComponent<MainMenuSpellbookUI>();

        Button openButton = CreateButton(
            rootRect,
            "SpellbookButton",
            new Vector2(300f, 82f),
            "SPELLBOOK",
            "VIEW DISCOVERED WORDS");
        TMP_Text openButtonSubtitle =
            openButton.transform.Find("Subheading")
                .GetComponent<TMP_Text>();
        RectTransform openRect = openButton.GetComponent<RectTransform>();
        openRect.anchorMin = Vector2.one;
        openRect.anchorMax = Vector2.one;
        openRect.pivot = Vector2.one;
        openRect.anchoredPosition = new Vector2(-34f, -34f);

        CreateBookMark(openRect);

        GameObject panelRoot = CreateUIObject("SpellbookPanel", rootRect);
        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        Stretch(panelRect);
        Image dimmer = panelRoot.AddComponent<Image>();
        dimmer.color = new Color(0.005f, 0.01f, 0.025f, 0.84f);
        dimmer.raycastTarget = true;

        GameObject window = CreatePanel(
            panelRect,
            "SpellbookWindow",
            Navy,
            Gold,
            3f);
        RectTransform windowRect = window.GetComponent<RectTransform>();
        SetCenteredSize(windowRect, new Vector2(980f, 680f));

        TMP_Text title = CreateText(
            windowRect,
            "Title",
            "THE SPELLBOOK",
            38f,
            FontStyles.Bold,
            Parchment,
            TextAlignmentOptions.Left);
        SetAnchoredRect(
            title.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(42f, -88f),
            new Vector2(-160f, -28f));

        TMP_Text subtitle = CreateText(
            windowRect,
            "Subtitle",
            "Every valid word cast in combat is preserved here.",
            18f,
            FontStyles.Normal,
            new Color(0.70f, 0.78f, 0.86f, 1f),
            TextAlignmentOptions.Left);
        SetAnchoredRect(
            subtitle.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(44f, -118f),
            new Vector2(-170f, -88f));

        TMP_Text count = CreateText(
            windowRect,
            "DiscoveredCount",
            "0 WORDS DISCOVERED",
            18f,
            FontStyles.Bold,
            Cyan,
            TextAlignmentOptions.Right);
        SetAnchoredRect(
            count.rectTransform,
            new Vector2(0.55f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, -92f),
            new Vector2(-118f, -48f));

        Button closeButton = CreateSquareButton(
            windowRect,
            "CloseButton",
            "×",
            new Vector2(64f, 54f));
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = Vector2.one;
        closeRect.anchorMax = Vector2.one;
        closeRect.pivot = Vector2.one;
        closeRect.anchoredPosition = new Vector2(-26f, -24f);

        TMP_InputField searchInput =
            CreateSearchField(windowRect, new Vector2(360f, 52f));
        RectTransform searchRect = searchInput.GetComponent<RectTransform>();
        searchRect.anchorMin = new Vector2(0f, 1f);
        searchRect.anchorMax = new Vector2(0f, 1f);
        searchRect.pivot = new Vector2(0f, 1f);
        searchRect.anchoredPosition = new Vector2(42f, -142f);

        GameObject listFrame = CreatePanel(
            windowRect,
            "WordListFrame",
            new Color(0.015f, 0.035f, 0.075f, 0.92f),
            new Color(Gold.r, Gold.g, Gold.b, 0.62f),
            2f);
        RectTransform listFrameRect = listFrame.GetComponent<RectTransform>();
        SetAnchoredRect(
            listFrameRect,
            Vector2.zero,
            Vector2.one,
            new Vector2(40f, 64f),
            new Vector2(-40f, -214f));

        ScrollRect scrollRect = listFrame.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 28f;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        GameObject viewport = CreateUIObject("Viewport", listFrameRect);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        Stretch(viewportRect, 14f);
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.001f);
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        GameObject content = CreateUIObject("Content", viewportRect);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(4, 4, 4, 4);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        MainMenuSpellbookEntryView entryTemplate =
            CreateEntryTemplate(contentRect);

        TMP_Text emptyState = CreateText(
            viewportRect,
            "EmptyState",
            "No words recorded yet.\nCast valid words in combat to fill your Spellbook.",
            24f,
            FontStyles.Italic,
            new Color(0.65f, 0.70f, 0.78f, 1f),
            TextAlignmentOptions.Center);
        SetAnchoredRect(
            emptyState.rectTransform,
            new Vector2(0.12f, 0.25f),
            new Vector2(0.88f, 0.75f),
            Vector2.zero,
            Vector2.zero);

        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;

        TMP_Text footer = CreateText(
            windowRect,
            "Footer",
            "SEARCH BY WORD, DEFINITION, OR RARITY",
            14f,
            FontStyles.Bold,
            new Color(0.55f, 0.64f, 0.74f, 1f),
            TextAlignmentOptions.Left);
        SetAnchoredRect(
            footer.rectTransform,
            Vector2.zero,
            new Vector2(1f, 0f),
            new Vector2(42f, 22f),
            new Vector2(-42f, 52f));

        controller.ConfigureReferences(
            openButton,
            openButtonSubtitle,
            closeButton,
            panelRoot,
            searchInput,
            contentRect,
            entryTemplate,
            count,
            emptyState);

        panelRoot.SetActive(false);
        return root;
    }

    private static MainMenuSpellbookEntryView CreateEntryTemplate(
        RectTransform parent)
    {
        GameObject entry = CreatePanel(
            parent,
            "EntryTemplate",
            NavyLight,
            new Color(Gold.r, Gold.g, Gold.b, 0.36f),
            1f);
        LayoutElement layout = entry.AddComponent<LayoutElement>();
        layout.preferredHeight = 112f;
        layout.minHeight = 112f;

        RectTransform entryRect = entry.GetComponent<RectTransform>();
        Image accent = CreateUIObject("RarityAccent", entryRect)
            .AddComponent<Image>();
        RectTransform accentRect = accent.rectTransform;
        SetAnchoredRect(
            accentRect,
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            Vector2.zero,
            new Vector2(7f, 0f));
        accent.color = Gold;
        accent.raycastTarget = false;

        TMP_Text word = CreateText(
            entryRect,
            "Word",
            "EXAMPLE",
            28f,
            FontStyles.Bold,
            Parchment,
            TextAlignmentOptions.Left);
        SetAnchoredRect(
            word.rectTransform,
            new Vector2(0f, 0f),
            new Vector2(0.30f, 1f),
            new Vector2(24f, 18f),
            new Vector2(-12f, -18f));

        TMP_Text metadata = CreateText(
            entryRect,
            "Metadata",
            "COMMON  •  7 LETTERS  •  USED 1x",
            15f,
            FontStyles.Bold,
            Gold,
            TextAlignmentOptions.Left);
        SetAnchoredRect(
            metadata.rectTransform,
            new Vector2(0.30f, 0.52f),
            new Vector2(1f, 1f),
            new Vector2(12f, 0f),
            new Vector2(-20f, -14f));

        TMP_Text definition = CreateText(
            entryRect,
            "Definition",
            "A dictionary-based definition appears here.",
            18f,
            FontStyles.Normal,
            new Color(0.83f, 0.87f, 0.91f, 1f),
            TextAlignmentOptions.TopLeft);
        SetAnchoredRect(
            definition.rectTransform,
            new Vector2(0.30f, 0f),
            new Vector2(1f, 0.58f),
            new Vector2(12f, 12f),
            new Vector2(-20f, 0f));
        definition.enableWordWrapping = true;

        MainMenuSpellbookEntryView view =
            entry.AddComponent<MainMenuSpellbookEntryView>();
        view.ConfigureReferences(accent, word, metadata, definition);
        entry.SetActive(false);
        return view;
    }

    private static Button CreateButton(
        RectTransform parent,
        string name,
        Vector2 size,
        string heading,
        string subheading)
    {
        GameObject buttonObject =
            CreatePanel(parent, name, Navy, Gold, 3f);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = size;

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.90f, 0.65f, 1f);
        colors.pressedColor = new Color(0.70f, 0.82f, 0.95f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        button.targetGraphic = buttonObject.GetComponent<Image>();

        TMP_Text title = CreateText(
            rect,
            "Heading",
            heading,
            24f,
            FontStyles.Bold,
            Parchment,
            TextAlignmentOptions.Left);
        SetAnchoredRect(
            title.rectTransform,
            Vector2.zero,
            Vector2.one,
            new Vector2(72f, 30f),
            new Vector2(-14f, -10f));

        TMP_Text subtitle = CreateText(
            rect,
            "Subheading",
            subheading,
            11f,
            FontStyles.Bold,
            Cyan,
            TextAlignmentOptions.Left);
        SetAnchoredRect(
            subtitle.rectTransform,
            Vector2.zero,
            Vector2.one,
            new Vector2(74f, 10f),
            new Vector2(-14f, -42f));

        return button;
    }

    private static void CreateBookMark(RectTransform parent)
    {
        GameObject icon = CreatePanel(
            parent,
            "BookIcon",
            new Color(0.08f, 0.17f, 0.34f, 1f),
            Gold,
            2f);
        RectTransform iconRect = icon.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(14f, 0f);
        iconRect.sizeDelta = new Vector2(46f, 54f);

        Image spine = CreateUIObject("Spine", iconRect).AddComponent<Image>();
        spine.color = Gold;
        spine.raycastTarget = false;
        SetAnchoredRect(
            spine.rectTransform,
            Vector2.zero,
            new Vector2(0f, 1f),
            Vector2.zero,
            new Vector2(6f, 0f));

        TMP_Text rune = CreateText(
            iconRect,
            "Rune",
            "S",
            26f,
            FontStyles.Bold,
            Cyan,
            TextAlignmentOptions.Center);
        Stretch(rune.rectTransform, 5f);
    }

    private static TMP_InputField CreateSearchField(
        RectTransform parent,
        Vector2 size)
    {
        GameObject root = CreatePanel(
            parent,
            "SearchField",
            new Color(0.02f, 0.045f, 0.09f, 1f),
            new Color(Gold.r, Gold.g, Gold.b, 0.65f),
            1f);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = size;

        TMP_InputField input = root.AddComponent<TMP_InputField>();

        GameObject textArea = CreateUIObject("TextArea", rect);
        RectTransform textAreaRect = textArea.GetComponent<RectTransform>();
        Stretch(textAreaRect);
        textAreaRect.offsetMin = new Vector2(18f, 8f);
        textAreaRect.offsetMax = new Vector2(-18f, -8f);
        textArea.AddComponent<RectMask2D>();

        TMP_Text placeholder = CreateText(
            textAreaRect,
            "Placeholder",
            "Search discovered words...",
            18f,
            FontStyles.Italic,
            new Color(0.48f, 0.55f, 0.64f, 1f),
            TextAlignmentOptions.Left);
        Stretch(placeholder.rectTransform);

        TMP_Text text = CreateText(
            textAreaRect,
            "Text",
            string.Empty,
            18f,
            FontStyles.Normal,
            Parchment,
            TextAlignmentOptions.Left);
        Stretch(text.rectTransform);

        input.textViewport = textAreaRect;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.characterLimit = 32;
        input.targetGraphic = root.GetComponent<Image>();
        return input;
    }

    private static Button CreateSquareButton(
        RectTransform parent,
        string name,
        string label,
        Vector2 size)
    {
        GameObject root = CreatePanel(parent, name, NavyLight, Gold, 2f);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        Button button = root.AddComponent<Button>();
        button.targetGraphic = root.GetComponent<Image>();

        TMP_Text text = CreateText(
            rect,
            "Label",
            label,
            38f,
            FontStyles.Normal,
            Parchment,
            TextAlignmentOptions.Center);
        Stretch(text.rectTransform);
        return button;
    }

    private static GameObject CreatePanel(
        RectTransform parent,
        string name,
        Color background,
        Color outlineColor,
        float outlineDistance)
    {
        GameObject root = CreateUIObject(name, parent);
        Image image = root.AddComponent<Image>();
        image.color = background;

        Outline outline = root.AddComponent<Outline>();
        outline.effectColor = outlineColor;
        outline.effectDistance =
            new Vector2(outlineDistance, -outlineDistance);
        outline.useGraphicAlpha = true;
        return root;
    }

    private static TMP_Text CreateText(
        RectTransform parent,
        string name,
        string value,
        float fontSize,
        FontStyles style,
        Color color,
        TextAlignmentOptions alignment)
    {
        GameObject root = CreateUIObject(name, parent);
        TextMeshProUGUI text = root.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        return text;
    }

    private static GameObject CreateUIObject(
        string name,
        RectTransform parent)
    {
        GameObject root = new GameObject(
            name,
            typeof(RectTransform));
        root.layer = LayerMask.NameToLayer("UI");
        RectTransform rect = root.GetComponent<RectTransform>();

        if (parent != null)
        {
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
        }

        return root;
    }

    private static void Stretch(RectTransform rect, float inset = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    private static void SetCenteredSize(
        RectTransform rect,
        Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
    }

    private static void SetAnchoredRect(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static void EnsurePrefabFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
            AssetDatabase.CreateFolder("Assets/Prefabs", "UI");

        Directory.CreateDirectory(PrefabFolder);
    }
}
