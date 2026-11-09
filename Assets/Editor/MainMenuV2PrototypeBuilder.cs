using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds a brand-new, isolated Main Menu V2 review scene.
/// NEVER touches the production Main Menu, build settings, TutorialScene, or cinematics.
/// </summary>
public static class MainMenuV2PrototypeBuilder
{
    public const string ScenePath =
        "Assets/Scenes/MenuConcepts/MainMenuV2_Prototype_20261030.unity";

    private const string Art = "Assets/MenuV2/Art/";

    private static readonly Color Navy = new Color(0.035f, 0.075f, 0.15f, .96f);
    private static readonly Color Gold = new Color(.82f, .63f, .30f, 1f);
    private static readonly Color Parchment = new Color(.96f, .90f, .74f, 1f);

    [MenuItem("SPELLSTRIKE/Main Menu V2/Build Isolated Prototype")]
    public static void Build()
    {
        if (System.Diagnostics.Process.GetProcessesByName("VALORANT-Win64-Shipping").Length > 0)
        {
            Debug.LogError("[MenuV2] VALORANT running; menu construction stopped.");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            Debug.LogWarning("[MenuV2] Prototype already exists. Refusing to overwrite " + ScenePath);
            return;
        }

        Scene existing = SceneManager.GetActiveScene();
        if (!existing.IsValid() || existing.isDirty || EditorApplication.isPlaying ||
            EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            Debug.LogError("[MenuV2] Save/close your current changes or stop import/play before building.");
            return;
        }

        string[] imports =
        {
            "MainMenu_Background_NoObjects_2400x1080.png",
            "Spellstrike_Logo.png", "VanAert_Approved.png",
            "Adventure_Arch_Base.png", "Adventure_Arch_Hover.png",
            "Adventure_Vortex.png",
            "Minigame_Tent_Base.png", "Minigame_Tent_Hover.png",
            "Options_Workbench_Base.png", "Options_Workbench_Hover.png",
            "Quit_Sign_Base.png", "Quit_Sign_Hover.png"
        };
        foreach (string file in imports)
        {
            TextureImporter importer = AssetImporter.GetAtPath(Art + file) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError("[MenuV2] Missing art: " + Art + file);
                return;
            }
            if (importer.textureType != TextureImporterType.Sprite || importer.mipmapEnabled)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
        }

        Scene preview = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(preview);
        try
        {
            BuildPreview();
            EditorSceneManager.SaveScene(preview, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[MenuV2] Saved isolated review scene: " + ScenePath);
        }
        finally
        {
            SceneManager.SetActiveScene(existing);
            EditorSceneManager.CloseScene(preview, true);
        }
    }

    private static void BuildPreview()
    {
        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0, 0, -10);
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.55f, .74f, .91f);
        camera.orthographic = true;

        GameObject canvasObject = new GameObject("MainMenuV2_Canvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2400, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = .5f;
        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();

        GameObject bg = UI("ApprovedNoObjectsBackground", canvasRect);
        Image bgImage = bg.AddComponent<Image>();
        bgImage.sprite = Sprite("MainMenu_Background_NoObjects_2400x1080.png");
        bgImage.raycastTarget = false;
        Stretch(bg.GetComponent<RectTransform>());
        AspectRatioFitter fitter = bg.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = 2400f / 1080f;

        AdventureButtonHandler adventureHandler =
            new GameObject("ExistingAdventureRoute").AddComponent<AdventureButtonHandler>();

        // Vortex is layered BEHIND the hollow arch. Static in this review scene:
        // the approved Shader Graph inward motion is the next isolated VFX pass.
        GameObject vortex = UI("PortalVortex_ShaderGraphPending", canvasRect);
        Center(vortex.GetComponent<RectTransform>(), 440, 560, 338, 420);
        Image vortexImage = vortex.AddComponent<Image>();
        vortexImage.sprite = Sprite("Adventure_Vortex.png");
        vortexImage.color = new Color(1f, 1f, 1f, .84f);
        vortexImage.preserveAspect = false; // oval opening
        vortexImage.raycastTarget = false;

        GameObject optionsPanel = CreateModal(canvasRect, "OPTIONS", false);
        GameObject quitPanel = CreateModal(canvasRect, "QUIT SPELLSTRIKE?", true);

        MakeTarget(canvasRect, "AdventureGateway", "Adventure_Arch_Base.png",
            "Adventure_Arch_Hover.png", "ADVENTURE", 430, 578, 700, 715,
            MenuV2HoverTarget.TargetAction.Adventure, adventureHandler, optionsPanel, quitPanel);
        MakeTarget(canvasRect, "MinigameTent", "Minigame_Tent_Base.png",
            "Minigame_Tent_Hover.png", "MINIGAME", 1970, 570, 750, 670,
            MenuV2HoverTarget.TargetAction.Minigame, adventureHandler, optionsPanel, quitPanel);

        // Approved canonical older Van remains an unanimated 2D illustration.
        // His five 3D orbiting tiles are a separate pending composition pass.
        GameObject van = UI("ApprovedOlderVanAert", canvasRect);
        Center(van.GetComponent<RectTransform>(), 1190, 365, 560, 700);
        Image vanImage = van.AddComponent<Image>();
        vanImage.sprite = Sprite("VanAert_Approved.png");
        vanImage.preserveAspect = true;
        vanImage.raycastTarget = false;

        MakeTarget(canvasRect, "OptionsWorkbench", "Options_Workbench_Base.png",
            "Options_Workbench_Hover.png", "OPTIONS", 265, 156, 345, 315,
            MenuV2HoverTarget.TargetAction.Options, adventureHandler, optionsPanel, quitPanel);
        MakeTarget(canvasRect, "QuitSign", "Quit_Sign_Base.png",
            "Quit_Sign_Hover.png", "QUIT", 2215, 154, 286, 282,
            MenuV2HoverTarget.TargetAction.Quit, adventureHandler, optionsPanel, quitPanel);

        // The actual Tutorial Spellbook 3D prefab will be attached in a
        // separate pass after prefab mesh/material and animation verification.
        GameObject anchor = UI("Spellbook3D_LowerRight_IntegrationAnchor", canvasRect);
        Center(anchor.GetComponent<RectTransform>(), 1805, 180, 260, 230);

        GameObject logo = UI("ApprovedSpellstrikeLogo", canvasRect);
        Center(logo.GetComponent<RectTransform>(), 1200, 940, 1020, 268);
        Image logoImage = logo.AddComponent<Image>();
        logoImage.sprite = Sprite("Spellstrike_Logo.png");
        logoImage.preserveAspect = true;
        logoImage.raycastTarget = false;

        // Modal windows are drawn in front of all Main Menu objects.
        optionsPanel.transform.SetAsLastSibling();
        quitPanel.transform.SetAsLastSibling();
        optionsPanel.SetActive(false);
        quitPanel.SetActive(false);

        GameObject eventSystem = new GameObject(
            "EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        eventSystem.transform.SetAsLastSibling();
    }

    private static void MakeTarget(RectTransform canvas, string objectName,
        string normal, string hover, string label,
        float x, float y, float width, float height,
        MenuV2HoverTarget.TargetAction action, AdventureButtonHandler handler,
        GameObject settings, GameObject quit)
    {
        GameObject root = UI(objectName, canvas);
        Center(root.GetComponent<RectTransform>(), x, y, width, height);

        Image baseImage = UI("Normal", root.transform).AddComponent<Image>();
        Stretch(baseImage.rectTransform);
        baseImage.sprite = Sprite(normal);
        baseImage.preserveAspect = true;
        baseImage.raycastTarget = false;

        Image hoveredImage = UI("Hover_GoldOutline", root.transform).AddComponent<Image>();
        Stretch(hoveredImage.rectTransform);
        hoveredImage.sprite = Sprite(hover);
        hoveredImage.preserveAspect = true;
        hoveredImage.raycastTarget = false;

        // Label only appears on hover and uses HUD navy/gold styling.
        GameObject plaque = UI("HoverLabel", root.transform);
        RectTransform plaqueRect = plaque.GetComponent<RectTransform>();
        CenterRelative(plaqueRect, 0, -(height * .5f + 16f), Mathf.Min(385, width * .70f), 55);
        Image plaqueImage = plaque.AddComponent<Image>();
        plaqueImage.color = Navy;
        plaqueImage.raycastTarget = false;
        Outline outline = plaque.AddComponent<Outline>();
        outline.effectColor = Gold;
        outline.effectDistance = new Vector2(2, -2);
        CanvasGroup labelGroup = plaque.AddComponent<CanvasGroup>();
        labelGroup.blocksRaycasts = false;

        TextMeshProUGUI labelText = CreateText(plaque.transform, "HUDStyleLabel", label, 30);
        Stretch(labelText.rectTransform);
        labelText.color = Parchment;
        labelText.alignment = TextAlignmentOptions.Center;

        // Keep all hover images non-raycasting so they cannot steal mouse focus.
        GameObject area = UI("PointerHotspot", root.transform);
        Stretch(area.GetComponent<RectTransform>());
        Image hit = area.AddComponent<Image>();
        hit.color = new Color(1, 1, 1, .003f);
        hit.raycastTarget = true;
        Button button = area.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        MenuV2HoverTarget target = area.AddComponent<MenuV2HoverTarget>();
        target.Configure(action, hoveredImage, labelGroup, handler, settings, quit, null);
    }

    private static GameObject CreateModal(RectTransform canvas, string heading, bool quit)
    {
        GameObject root = UI(quit ? "QuitConfirmPanel" : "OptionsPanel", canvas);
        Stretch(root.GetComponent<RectTransform>());
        Image dimmer = root.AddComponent<Image>();
        dimmer.color = new Color(.015f, .025f, .04f, .75f);
        dimmer.raycastTarget = true;

        GameObject panel = UI("NavyGoldPanel", root.transform);
        CenterRelative(panel.GetComponent<RectTransform>(), 0, 0, 750, 405);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = Navy;
        Outline panelOutline = panel.AddComponent<Outline>();
        panelOutline.effectColor = Gold;
        panelOutline.effectDistance = new Vector2(3, -3);

        TextMeshProUGUI title = CreateText(panel.transform, "Title", heading, 43);
        CenterRelative(title.rectTransform, 0, 125, 650, 65);

        if (quit)
        {
            CreateModalButton(panel.transform, "EXIT GAME", -155, -88, root, true);
            CreateModalButton(panel.transform, "CANCEL", 155, -88, root, false);
        }
        else
        {
            TextMeshProUGUI volumeLabel =
                CreateText(panel.transform, "VolumeLabel", "MASTER VOLUME", 29);
            CenterRelative(volumeLabel.rectTransform, 0, 25, 480, 52);
            GameObject sliderObject = UI("VolumeSlider", panel.transform);
            CenterRelative(sliderObject.GetComponent<RectTransform>(), 0, -36, 430, 35);
            Slider slider = sliderObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.value = 1f;

            Image track = UI("Track", sliderObject.transform).AddComponent<Image>();
            Stretch(track.rectTransform);
            track.color = new Color(.20f, .29f, .35f, 1f);
            slider.targetGraphic = track;
            RectTransform fillArea = UI("FillArea", sliderObject.transform)
                .GetComponent<RectTransform>();
            Stretch(fillArea);
            Image fill = UI("Fill", fillArea).AddComponent<Image>();
            Stretch(fill.rectTransform);
            fill.color = Gold;
            slider.fillRect = fill.rectTransform;
            Image handle = UI("Handle", sliderObject.transform).AddComponent<Image>();
            handle.color = Parchment;
            CenterRelative(handle.rectTransform, 0, 0, 32, 46);
            slider.handleRect = handle.rectTransform;
            slider.direction = Slider.Direction.LeftToRight;
            sliderObject.AddComponent<MenuV2VolumeSetting>().Configure(slider);

            CreateModalButton(panel.transform, "CLOSE", 0, -124, root, false);
        }
        return root;
    }

    private static void CreateModalButton(Transform parent, string caption,
        float x, float y, GameObject modal, bool quit)
    {
        GameObject go = UI(caption.Replace(" ", "") + "Button", parent);
        CenterRelative(go.GetComponent<RectTransform>(), x, y, 260, 72);
        Image image = go.AddComponent<Image>();
        image.color = new Color(.08f, .20f, .28f, .98f);
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = Gold;
        outline.effectDistance = new Vector2(2, -2);
        go.AddComponent<Button>();
        go.AddComponent<MenuV2PanelAction>().Configure(modal, quit);
        TextMeshProUGUI text = CreateText(go.transform, "Caption", caption, 29);
        Stretch(text.rectTransform);
    }

    private static TextMeshProUGUI CreateText(
        Transform parent, string name, string text, float fontSize)
    {
        GameObject go = UI(name, parent);
        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = FontStyles.Bold;
        label.color = Parchment;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = false;
        label.raycastTarget = false;
        return label;
    }

    private static GameObject UI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        if (parent != null)
            go.transform.SetParent(parent, false);
        return go;
    }

    private static Sprite Sprite(string file)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + file);
        if (sprite == null)
            throw new System.InvalidOperationException("Missing Unity Sprite: " + file);
        return sprite;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }

    private static void Center(RectTransform rect,
        float screenX, float screenY, float w, float h)
    {
        CenterRelative(rect, screenX - 1200f, screenY - 540f, w, h);
    }

    private static void CenterRelative(RectTransform rect,
        float x, float y, float w, float h)
    {
        rect.anchorMin = new Vector2(.5f, .5f);
        rect.anchorMax = new Vector2(.5f, .5f);
        rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = new Vector2(w, h);
        rect.anchoredPosition = new Vector2(x, y);
    }
}
