using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Stage3RiddleHUDSetupMenu
{
    private const string HudPrefabPath = "Assets/Prefabs/PersistentGameplayHUD.prefab";
    private const string CinzelPath = "Assets/Fonts/SPELLSTRIKE/Cinzel-Variable SDF.asset";
    private const string AlegreyaPath = "Assets/Fonts/SPELLSTRIKE/Alegreya-Variable SDF.asset";
    private const string RiddleUiFolder = "Assets/Stage 3/UI/Riddle/Modular";
    // Use the original modular art at its authored aspect ratio. The previous
    // safe-center expansion solved width coverage but made the composition look
    // visibly stretched compared with the locked reference.
    private const string ParchmentPath = RiddleUiFolder + "/Riddle_ParchmentBase.png";
    private const string TitlePlaquePath = RiddleUiFolder + "/Riddle_TitlePlaque.png";
    private const string ProgressPlatePath = RiddleUiFolder + "/Riddle_ProgressPlate.png";
    private const string ProgressActiveDotPath = RiddleUiFolder + "/Riddle_ProgressActiveDot.png";
    private const string CornerOrnamentPath = RiddleUiFolder + "/Riddle_CornerOrnament.png";
    private const string CrystalPath = RiddleUiFolder + "/Riddle_Crystal.png";
    private const string DividerPath = RiddleUiFolder + "/Riddle_Divider.png";
    private const string PreviewSessionKey = "SpellStrike.Stage3RiddleHUDPreviewPending";
    private const string CapturePath = "Temp/Stage3_Riddle_GameView_Final.png";

    private const string PreviewRiddle =
        "I drift across the desert sea,\n"
        + "shaping dunes for all to see.\n"
        + "I have no feet, yet I roam free.\n"
        + "<b>What am I?</b>";

    private static int captureCountdown;

    static Stage3RiddleHUDSetupMenu()
    {
        EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
    }

    [MenuItem("SPELLSTRIKE/Stage 3/Riddle/Reset Riddle HUD To Reference (Destructive)")]
    public static void InstallRiddleHudVisual()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(HudPrefabPath);
        try
        {
            Canvas combatCanvas = prefabRoot.GetComponentsInChildren<Canvas>(true)
                .FirstOrDefault(canvas => FindDescendant(canvas.transform, "WordBarPanel") != null);
            if (combatCanvas == null)
                throw new System.InvalidOperationException("Persistent combat Canvas with WordBarPanel was not found.");

            Transform existing = FindDescendant(combatCanvas.transform, "RiddleChallengeRoot");
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            TMP_FontAsset titleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CinzelPath);
            TMP_FontAsset bodyFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AlegreyaPath);
            if (titleFont == null || bodyFont == null)
                throw new System.InvalidOperationException("Cinzel or Alegreya TMP font asset is missing.");

            Sprite parchment = LoadRequiredSprite(ParchmentPath);
            Sprite titlePlaque = LoadRequiredSprite(TitlePlaquePath);
            Sprite progressPlate = LoadRequiredSprite(ProgressPlatePath);
            Sprite progressActiveDot = LoadRequiredSprite(ProgressActiveDotPath);
            Sprite cornerOrnament = LoadRequiredSprite(CornerOrnamentPath);
            Sprite crystal = LoadRequiredSprite(CrystalPath);
            Sprite divider = LoadRequiredSprite(DividerPath);

            BuildHud(
                combatCanvas.transform,
                titleFont,
                bodyFont,
                parchment,
                titlePlaque,
                progressPlate,
                progressActiveDot,
                cornerOrnament,
                crystal,
                divider);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, HudPrefabPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Stage3RiddleHUDSetup] Installed RiddleChallengeRoot into PersistentGameplayHUD.prefab.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    [MenuItem("SPELLSTRIKE/Stage 3/Riddle/Preview + Capture Game View")]
    public static void PreviewAndCapture()
    {
        if (EditorApplication.isPlaying)
        {
            ShowPreviewAndScheduleCapture();
            return;
        }

        SessionState.SetBool(PreviewSessionKey, true);
        EditorApplication.EnterPlaymode();
    }

    private static void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode)
            return;
        if (!SessionState.GetBool(PreviewSessionKey, false))
            return;

        SessionState.SetBool(PreviewSessionKey, false);
        ShowPreviewAndScheduleCapture();
    }

    private static void ShowPreviewAndScheduleCapture()
    {
        if (SceneManager.GetActiveScene().name != "Stage3Scene")
        {
            Debug.LogError("[Stage3RiddleHUDSetup] Stage3Scene must be active for the riddle HUD capture.");
            return;
        }

        Stage3RiddleHUDView view = Object.FindFirstObjectByType<Stage3RiddleHUDView>(FindObjectsInactive.Include);
        if (view == null)
        {
            Debug.LogError("[Stage3RiddleHUDSetup] Stage3RiddleHUDView was not found in the persistent HUD.");
            return;
        }

        Screen.SetResolution(2400, 1080, FullScreenMode.Windowed);
        view.ShowRiddle(1, 4, PreviewRiddle, true);

        captureCountdown = 24;
        EditorApplication.update -= CaptureWhenReady;
        EditorApplication.update += CaptureWhenReady;
        Debug.Log("[Stage3RiddleHUDSetup] Preview shown. Scheduling actual Game View capture.");
    }

    private static void CaptureWhenReady()
    {
        if (!EditorApplication.isPlaying)
        {
            EditorApplication.update -= CaptureWhenReady;
            return;
        }

        captureCountdown--;
        if (captureCountdown > 0)
            return;

        EditorApplication.update -= CaptureWhenReady;
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
        string absolutePath = Path.Combine(projectRoot, CapturePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath) ?? projectRoot);
        ScreenCapture.CaptureScreenshot(absolutePath, 1);
        Debug.Log($"[Stage3RiddleHUDSetup] Game View screenshot requested at '{absolutePath}'.");
    }

    private static Stage3RiddleHUDView BuildHud(
        Transform canvasTransform,
        TMP_FontAsset titleFont,
        TMP_FontAsset bodyFont,
        Sprite parchmentSprite,
        Sprite titlePlaqueSprite,
        Sprite progressPlateSprite,
        Sprite progressActiveDotSprite,
        Sprite cornerOrnamentSprite,
        Sprite crystalSprite,
        Sprite dividerSprite)
    {
        Color goldPale = new Color(1f, 0.86f, 0.44f, 1f);
        Color ink = new Color(0.14f, 0.065f, 0.025f, 1f);
        Color blueGem = new Color(0.05f, 0.66f, 1f, 1f);

        GameObject root = CreateUiObject("RiddleChallengeRoot", canvasTransform);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        Stretch(rootRect);
        CanvasGroup group = root.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        // The reference panel is roughly 3:1, not the previous ~5.9:1 banner.
        // Keep the native parchment ratio so the border, desert art, and corner
        // crystals retain their intended proportions.
        GameObject bodyFrame = CreateUiObject("RiddleBodyFrame", root.transform);
        // Keep the original parchment aspect and place it fully inside the
        // 600-high reference canvas. The old y=300 placement pushed the top
        // above the canvas, which made the composition look vertically wrong.
        SetFixedBottomCenter(bodyFrame.GetComponent<RectTransform>(), 90f, 760f, 254f);

        GameObject parchmentArt = CreateSpriteImage("RiddleParchmentArt", bodyFrame.transform, parchmentSprite);
        SetFixedRect(
            parchmentArt.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(760f, 254f));

        GameObject titlePlaque = CreateSpriteImage("RiddleTitlePlaqueBody", root.transform, titlePlaqueSprite);
        SetFixedRect(
            titlePlaque.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 292f),
            new Vector2(460f, 153f));

        GameObject progressPlate = CreateSpriteImage("RiddleProgressPlate", root.transform, progressPlateSprite);
        SetFixedRect(
            progressPlate.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0.5f),
            new Vector2(300f, 278f),
            new Vector2(150f, 75f));

        GameObject divider = CreateSpriteImage("RiddleDivider", bodyFrame.transform, dividerSprite);
        SetFixedRect(
            divider.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -112f),
            new Vector2(66f, 18f));

        TMP_Text title = CreateText("Title", titlePlaque.transform, titleFont, "RIDDLE CHALLENGE", 29f, goldPale, TextAlignmentOptions.Center);
        SetAnchors(title.rectTransform, new Vector2(0.10f, 0.34f), new Vector2(0.90f, 0.72f), Vector2.zero, Vector2.zero);
        title.fontStyle = FontStyles.Bold;
        title.enableAutoSizing = true;
        title.fontSizeMin = 20f;
        title.fontSizeMax = 40f;

        TMP_Text subtitle = CreateText("Subtitle", titlePlaque.transform, titleFont, "Stage 3 Trial", 15f, goldPale, TextAlignmentOptions.Center);
        SetAnchors(subtitle.rectTransform, new Vector2(0.24f, 0.12f), new Vector2(0.76f, 0.34f), Vector2.zero, Vector2.zero);
        subtitle.enableAutoSizing = true;
        subtitle.fontSizeMin = 10f;
        subtitle.fontSizeMax = 19f;

        TMP_Text progress = CreateText("ProgressText", progressPlate.transform, titleFont, "Riddle 1 / 4", 15f, goldPale, TextAlignmentOptions.Center);
        SetAnchors(progress.rectTransform, new Vector2(0.12f, 0.56f), new Vector2(0.88f, 0.82f), Vector2.zero, Vector2.zero);
        progress.enableAutoSizing = true;
        progress.fontSizeMin = 10f;
        progress.fontSizeMax = 18f;

        TMP_Text dots = CreateText("ProgressDots", progressPlate.transform, titleFont, "●  ○  ○  ○", 15f, blueGem, TextAlignmentOptions.Center);
        SetAnchors(dots.rectTransform, new Vector2(0.10f, 0.33f), new Vector2(0.90f, 0.59f), Vector2.zero, Vector2.zero);
        dots.enableAutoSizing = true;
        dots.fontSizeMin = 9f;
        dots.fontSizeMax = 17f;
        // The current progress-plate artwork already contains the four gold
        // ring sockets. Keep the legacy TMP dot row hidden so it does not
        // double-render over the authored rings. The active blue fill is a
        // separate Unity-side indicator, not baked into this sprite.
        dots.gameObject.SetActive(false);

        GameObject activeDotObject = CreateSpriteImage("ProgressActiveDot", progressPlate.transform, progressActiveDotSprite);
        RectTransform activeDotRect = activeDotObject.GetComponent<RectTransform>();
        SetFixedRect(
            activeDotRect,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(-24.9f, -7f),
            new Vector2(8.5f, 8.5f));
        Image activeDot = activeDotObject.GetComponent<Image>();

        TMP_Text body = CreateText("RiddleBody", bodyFrame.transform, bodyFont, PreviewRiddle, 24f, ink, TextAlignmentOptions.Center);
        SetAnchors(body.rectTransform, new Vector2(0.20f, 0.27f), new Vector2(0.80f, 0.56f), Vector2.zero, Vector2.zero);
        body.fontStyle = FontStyles.Bold;
        body.enableAutoSizing = true;
        body.fontSizeMin = 20f;
        body.fontSizeMax = 34f;
        body.lineSpacing = -4f;

        TMP_Text instruction = CreateText("Instruction", bodyFrame.transform, bodyFont, "Answer using the tiles below", 17f, new Color(0.44f, 0.25f, 0.09f, 0.95f), TextAlignmentOptions.Center);
        SetAnchors(instruction.rectTransform, new Vector2(0.30f, 0.17f), new Vector2(0.70f, 0.28f), Vector2.zero, Vector2.zero);
        instruction.enableAutoSizing = true;
        instruction.fontSizeMin = 13f;
        instruction.fontSizeMax = 20f;

        GameObject feedbackRoot = CreateUiObject("FeedbackPopup", bodyFrame.transform);
        SetAnchors(feedbackRoot.GetComponent<RectTransform>(), new Vector2(0.70f, 0.42f), new Vector2(0.98f, 0.58f), Vector2.zero, Vector2.zero);
        Image feedbackBackdrop = feedbackRoot.AddComponent<Image>();
        feedbackBackdrop.raycastTarget = false;
        feedbackBackdrop.color = new Color(0.08f, 0.82f, 0.36f, 0.94f);
        AddOutline(feedbackRoot, new Color(0.18f, 1f, 0.53f, 1f), 2.3f);

        GameObject feedbackAccentObject = CreateImage("Accent", feedbackRoot.transform, new Color(0.18f, 1f, 0.53f, 1f), false);
        SetAnchors(feedbackAccentObject.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0.028f, 1f), Vector2.zero, Vector2.zero);
        Image feedbackAccent = feedbackAccentObject.GetComponent<Image>();

        TMP_Text feedbackTitle = CreateText("FeedbackTitle", feedbackRoot.transform, titleFont, "Correct!", 20f, Color.white, TextAlignmentOptions.MidlineLeft);
        SetAnchors(feedbackTitle.rectTransform, new Vector2(0.12f, 0.43f), new Vector2(0.94f, 0.92f), Vector2.zero, Vector2.zero);
        feedbackTitle.fontStyle = FontStyles.Bold;
        feedbackTitle.enableAutoSizing = true;
        feedbackTitle.fontSizeMin = 12f;
        feedbackTitle.fontSizeMax = 22f;

        TMP_Text feedbackBody = CreateText("FeedbackBody", feedbackRoot.transform, bodyFont, "Attack Damage +10%", 14f, goldPale, TextAlignmentOptions.MidlineLeft);
        SetAnchors(feedbackBody.rectTransform, new Vector2(0.12f, 0.08f), new Vector2(0.94f, 0.48f), Vector2.zero, Vector2.zero);
        feedbackBody.enableAutoSizing = true;
        feedbackBody.fontSizeMin = 9f;
        feedbackBody.fontSizeMax = 16f;
        feedbackRoot.SetActive(false);

        Stage3RiddleHUDView view = root.AddComponent<Stage3RiddleHUDView>();
        view.Configure(
            group,
            title,
            subtitle,
            progress,
            dots,
            activeDot,
            body,
            instruction,
            feedbackRoot,
            feedbackBackdrop,
            feedbackAccent,
            feedbackTitle,
            feedbackBody);

        Stage3RiddleHUDLayoutAuthoring authoring = root.AddComponent<Stage3RiddleHUDLayoutAuthoring>();
        authoring.Configure(
            bodyFrame.GetComponent<RectTransform>(),
            parchmentArt.GetComponent<RectTransform>(),
            titlePlaque.GetComponent<RectTransform>(),
            progressPlate.GetComponent<RectTransform>(),
            divider.GetComponent<RectTransform>(),
            title,
            subtitle,
            progress,
            dots,
            body,
            instruction);

        // Production default: the entire riddle HUD stays inactive until
        // Stage 3 Node 4's trigger calls Stage3RiddleController.StartBattle().
        root.SetActive(false);

        root.transform.SetAsLastSibling();
        return view;
    }

    private static Sprite LoadRequiredSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            throw new System.InvalidOperationException($"Required riddle UI sprite is missing or not imported as Sprite: '{path}'.");
        return sprite;
    }

    private static GameObject CreateSpriteImage(string name, Transform parent, Sprite sprite)
    {
        GameObject go = CreateImage(name, parent, Color.white, false);
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return go;
    }

    private static void CreateCornerOrnament(
        Transform parent,
        string name,
        Sprite sprite,
        Vector2 anchor,
        Vector2 position,
        float rotation,
        float size = 88f)
    {
        GameObject ornament = CreateSpriteImage(name, parent, sprite);
        SetFixedRect(
            ornament.GetComponent<RectTransform>(),
            anchor,
            new Vector2(0.5f, 0.5f),
            position,
            new Vector2(size, size),
            rotation);
    }

    private static void CreateBorderStrip(
        Transform parent,
        string name,
        Vector2 anchor,
        Vector2 position,
        Vector2 size,
        Color color)
    {
        GameObject strip = CreateImage(name, parent, color, false);
        SetFixedRect(strip.GetComponent<RectTransform>(), anchor, new Vector2(0.5f, 0.5f), position, size);
    }

    private static void SetFixedBottomCenter(RectTransform rect, float bottom, float width, float height)
    {
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, bottom);
        rect.sizeDelta = new Vector2(width, height);
        rect.localScale = Vector3.one;
    }

    private static void SetFixedRect(
        RectTransform rect,
        Vector2 anchor,
        Vector2 pivot,
        Vector2 position,
        Vector2 size,
        float rotation = 0f)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        rect.localEulerAngles = new Vector3(0f, 0f, rotation);
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        return go;
    }

    private static GameObject CreateImage(string name, Transform parent, Color color, bool raycastTarget)
    {
        GameObject go = CreateUiObject(name, parent);
        Image image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;
        return go;
    }

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        TMP_FontAsset font,
        string text,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment)
    {
        GameObject go = CreateUiObject(name, parent);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.font = font;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        return tmp;
    }

    private static void AddOutline(GameObject go, Color color, float distance)
    {
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(distance, -distance);
        outline.useGraphicAlpha = true;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        rect.localScale = Vector3.one;
    }

    private static Transform FindDescendant(Transform root, string objectName)
    {
        foreach (Transform child in root)
        {
            if (child.name == objectName)
                return child;

            Transform found = FindDescendant(child, objectName);
            if (found != null)
                return found;
        }

        return null;
    }
}
