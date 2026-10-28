#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class TutorialHUDBuilder
{
    private static readonly Color Navy = new(0.025f, 0.075f, 0.105f, 0.94f);
    private static readonly Color NavyLight = new(0.055f, 0.14f, 0.18f, 0.96f);
    private static readonly Color Gold = new(0.91f, 0.61f, 0.19f, 1f);
    private static readonly Color Parchment = new(0.92f, 0.84f, 0.65f, 1f);
    private static readonly Color Ivory = new(0.96f, 0.94f, 0.84f, 1f);
    private static readonly Color Cyan = new(0.16f, 0.78f, 1f, 1f);

    [MenuItem("SPELLSTRIKE/Tutorial/Create Tutorial HUD Preview")]
    public static void CreateTutorialHUD()
    {
        GameObject existing = GameObject.Find("TutorialHUD_Preview");
        if (existing != null)
        {
            Selection.activeGameObject = existing;
            EditorUtility.DisplayDialog(
                "Tutorial HUD already exists",
                "TutorialHUD_Preview already exists in this scene. It was selected instead of creating a duplicate.",
                "OK");
            return;
        }

        EnsureEventSystem();

        GameObject canvasObject = NewUI("TutorialHUD_Preview", null);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        CanvasGroup canvasGroup = canvasObject.AddComponent<CanvasGroup>();
        TutorialHUDController controller = canvasObject.AddComponent<TutorialHUDController>();

        GameObject safeArea = Panel("SafeArea", canvasObject.transform, Color.clear);
        Stretch(safeArea.GetComponent<RectTransform>(), 24f, 24f, 24f, 24f);

        GameObject health = FramedPanel("PlayerHealth", safeArea.transform, Navy, new Vector2(24f, -24f),
            new Vector2(430f, 116f), new Vector2(0f, 1f));
        Text("Portrait", health.transform, "VA", 32, TextAlignmentOptions.Center, Gold,
            new Vector2(16f, -16f), new Vector2(82f, 82f), new Vector2(0f, 1f));
        Text("HealthValue", health.transform, "100 / 100", 28, TextAlignmentOptions.Center, Ivory,
            new Vector2(116f, -18f), new Vector2(286f, 34f), new Vector2(0f, 1f));
        GameObject healthTrack = Panel("HealthTrack", health.transform, new Color(0.19f, 0.03f, 0.04f, 1f));
        SetRect(healthTrack.GetComponent<RectTransform>(), new Vector2(116f, -62f), new Vector2(286f, 28f), new Vector2(0f, 1f));
        GameObject healthFill = Panel("HealthFill", healthTrack.transform, new Color(0.75f, 0.08f, 0.08f, 1f));
        Stretch(healthFill.GetComponent<RectTransform>(), 4f, 4f, 4f, 4f);

        GameObject objective = FramedPanel("ObjectivePanel", safeArea.transform, Navy, new Vector2(0f, -24f),
            new Vector2(620f, 176f), new Vector2(0.5f, 1f));
        TMP_Text stepText = Text("StepText", objective.transform, "TUTORIAL 3/6", 34,
            TextAlignmentOptions.Center, Parchment, new Vector2(0f, -14f), new Vector2(560f, 42f), new Vector2(0.5f, 1f));
        TMP_Text primaryText = Text("PrimaryObjective", objective.transform, "◆  Collect the glowing letters", 27,
            TextAlignmentOptions.Left, Ivory, new Vector2(0f, -64f), new Vector2(530f, 34f), new Vector2(0.5f, 1f));
        TMP_Text secondaryText = Text("SecondaryObjective", objective.transform, "◆  Form a valid word", 27,
            TextAlignmentOptions.Left, Ivory, new Vector2(0f, -102f), new Vector2(530f, 34f), new Vector2(0.5f, 1f));

        Image[] pips = new Image[6];
        GameObject pipRow = NewUI("ProgressPips", objective.transform);
        SetRect(pipRow.GetComponent<RectTransform>(), new Vector2(0f, -150f), new Vector2(280f, 24f), new Vector2(0.5f, 1f));
        HorizontalLayoutGroup pipLayout = pipRow.AddComponent<HorizontalLayoutGroup>();
        pipLayout.spacing = 16f;
        pipLayout.childAlignment = TextAnchor.MiddleCenter;
        pipLayout.childControlHeight = false;
        pipLayout.childControlWidth = false;
        for (int i = 0; i < pips.Length; i++)
        {
            GameObject pip = Panel($"Pip_{i + 1}", pipRow.transform, i < 3 ? Gold : NavyLight);
            RectTransform rect = pip.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(22f, 22f);
            Outline outline = pip.AddComponent<Outline>();
            outline.effectColor = Gold;
            outline.effectDistance = new Vector2(2f, -2f);
            pips[i] = pip.GetComponent<Image>();
        }

        GameObject pause = FramedPanel("PauseButton", safeArea.transform, Navy, new Vector2(-24f, -24f),
            new Vector2(76f, 76f), new Vector2(1f, 1f));
        Text("PauseIcon", pause.transform, "Ⅱ", 34, TextAlignmentOptions.Center, Parchment,
            Vector2.zero, new Vector2(70f, 70f), new Vector2(0.5f, 0.5f));
        pause.AddComponent<Button>();

        GameObject dialogue = FramedPanel("GuideDialogue", safeArea.transform, Navy, new Vector2(24f, 208f),
            new Vector2(570f, 150f), new Vector2(0f, 0f));
        Text("GuidePortrait", dialogue.transform, "GUIDE", 22, TextAlignmentOptions.Center, Cyan,
            new Vector2(18f, 16f), new Vector2(110f, 110f), new Vector2(0f, 0f));
        TMP_Text dialogueText = Text("DialogueText", dialogue.transform, "Select the highlighted tile.", 28,
            TextAlignmentOptions.Left, Ivory, new Vector2(152f, 22f), new Vector2(380f, 104f), new Vector2(0f, 0f));

        GameObject keyboard = FramedPanel("KeyboardPrompt", safeArea.transform, Navy, new Vector2(24f, 24f),
            new Vector2(270f, 160f), new Vector2(0f, 0f));
        Text("Keys", keyboard.transform, "  W\nA S D", 36, TextAlignmentOptions.Center, Parchment,
            new Vector2(0f, 20f), new Vector2(220f, 100f), new Vector2(0.5f, 0f));
        Text("PlatformNote", keyboard.transform, "PC MOVEMENT", 18, TextAlignmentOptions.Center, Ivory,
            new Vector2(0f, 8f), new Vector2(220f, 28f), new Vector2(0.5f, 0f));

        GameObject mobile = FramedPanel("MobilePrompt", safeArea.transform, Navy, new Vector2(24f, 24f),
            new Vector2(270f, 160f), new Vector2(0f, 0f));
        Text("JoystickIcon", mobile.transform, "◉", 70, TextAlignmentOptions.Center, Cyan,
            new Vector2(0f, 40f), new Vector2(120f, 90f), new Vector2(0.5f, 0f));
        Text("PlatformNote", mobile.transform, "DRAG TO MOVE", 18, TextAlignmentOptions.Center, Ivory,
            new Vector2(0f, 8f), new Vector2(220f, 28f), new Vector2(0.5f, 0f));
        mobile.SetActive(false);

        GameObject wordArea = NewUI("WordCombatArea", safeArea.transform);
        SetRect(wordArea.GetComponent<RectTransform>(), new Vector2(0f, 24f), new Vector2(900f, 250f), new Vector2(0.5f, 0f));

        GameObject wordSlots = NewUI("WordSlots", wordArea.transform);
        SetRect(wordSlots.GetComponent<RectTransform>(), new Vector2(-60f, 140f), new Vector2(650f, 72f), new Vector2(0.5f, 0f));
        HorizontalLayoutGroup slotLayout = wordSlots.AddComponent<HorizontalLayoutGroup>();
        ConfigureRow(slotLayout, 10f);
        for (int i = 0; i < 6; i++)
            CreateTile($"WordSlot_{i + 1}", wordSlots.transform, "", NavyLight, Ivory);

        GameObject tileRow = NewUI("TilePoolPreview", wordArea.transform);
        SetRect(tileRow.GetComponent<RectTransform>(), new Vector2(-60f, 32f), new Vector2(760f, 92f), new Vector2(0.5f, 0f));
        HorizontalLayoutGroup tileLayout = tileRow.AddComponent<HorizontalLayoutGroup>();
        ConfigureRow(tileLayout, 12f);
        char[] letters = { 'S', 'T', 'O', 'N', 'E', 'A' };
        GameObject highlightedTile = null;
        for (int i = 0; i < letters.Length; i++)
        {
            GameObject tile = CreateTile($"Tile_{letters[i]}", tileRow.transform, letters[i].ToString(),
                Parchment, new Color(0.12f, 0.08f, 0.03f, 1f));
            tile.AddComponent<Button>();
            if (letters[i] == 'N')
            {
                highlightedTile = tile;
                Outline glow = tile.AddComponent<Outline>();
                glow.effectColor = Cyan;
                glow.effectDistance = new Vector2(6f, -6f);
            }
        }

        GameObject strike = FramedPanel("StrikeButtonPreview", safeArea.transform, new Color(0.42f, 0.23f, 0.035f, 0.98f),
            new Vector2(-24f, 40f), new Vector2(250f, 150f), new Vector2(1f, 0f));
        Text("StrikeText", strike.transform, "STRIKE", 46, TextAlignmentOptions.Center, Parchment,
            Vector2.zero, new Vector2(230f, 130f), new Vector2(0.5f, 0.5f));
        strike.AddComponent<Button>();

        GameObject pointer = TextObject("AnimatedPointer", highlightedTile.transform, "▼", 48,
            TextAlignmentOptions.Center, Gold);
        RectTransform pointerRect = pointer.GetComponent<RectTransform>();
        pointerRect.anchorMin = pointerRect.anchorMax = new Vector2(0.5f, 1f);
        pointerRect.sizeDelta = new Vector2(70f, 70f);
        pointerRect.anchoredPosition = new Vector2(0f, 44f);
        pointer.AddComponent<TutorialUIPulse>();

        SerializedObject serializedController = new(controller);
        serializedController.FindProperty("rootCanvasGroup").objectReferenceValue = canvasGroup;
        serializedController.FindProperty("keyboardPrompt").objectReferenceValue = keyboard;
        serializedController.FindProperty("mobilePrompt").objectReferenceValue = mobile;
        serializedController.FindProperty("animatedPointer").objectReferenceValue = pointer;
        serializedController.FindProperty("stepText").objectReferenceValue = stepText;
        serializedController.FindProperty("primaryObjectiveText").objectReferenceValue = primaryText;
        serializedController.FindProperty("secondaryObjectiveText").objectReferenceValue = secondaryText;
        serializedController.FindProperty("dialogueText").objectReferenceValue = dialogueText;
        SerializedProperty pipProperty = serializedController.FindProperty("progressPips");
        pipProperty.arraySize = pips.Length;
        for (int i = 0; i < pips.Length; i++)
            pipProperty.GetArrayElementAtIndex(i).objectReferenceValue = pips[i];
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        Undo.RegisterCreatedObjectUndo(canvasObject, "Create SPELLSTRIKE Tutorial HUD");
        Selection.activeGameObject = canvasObject;
        EditorUtility.SetDirty(canvasObject);
        Debug.Log("Created TutorialHUD_Preview. It is a visual shell; wire the real Tile Pool and STRIKE controls only after Play Mode validation.");
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject eventSystem = new("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
        Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
    }

    private static GameObject FramedPanel(string name, Transform parent, Color fill, Vector2 position,
        Vector2 size, Vector2 anchor)
    {
        GameObject panel = Panel(name, parent, fill);
        SetRect(panel.GetComponent<RectTransform>(), position, size, anchor);
        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = Gold;
        outline.effectDistance = new Vector2(3f, -3f);
        return panel;
    }

    private static GameObject Panel(string name, Transform parent, Color color)
    {
        GameObject result = NewUI(name, parent);
        Image image = result.AddComponent<Image>();
        image.color = color;
        return result;
    }

    private static GameObject CreateTile(string name, Transform parent, string label, Color fill, Color textColor)
    {
        GameObject tile = Panel(name, parent, fill);
        tile.GetComponent<RectTransform>().sizeDelta = new Vector2(92f, 82f);
        LayoutElement layout = tile.AddComponent<LayoutElement>();
        layout.preferredWidth = 92f;
        layout.preferredHeight = 82f;
        Outline outline = tile.AddComponent<Outline>();
        outline.effectColor = Gold;
        outline.effectDistance = new Vector2(3f, -3f);
        Text("Letter", tile.transform, label, 46, TextAlignmentOptions.Center, textColor,
            Vector2.zero, new Vector2(88f, 78f), new Vector2(0.5f, 0.5f));
        return tile;
    }

    private static TMP_Text Text(string name, Transform parent, string value, float size,
        TextAlignmentOptions alignment, Color color, Vector2 position, Vector2 dimensions, Vector2 anchor)
    {
        GameObject textObject = TextObject(name, parent, value, size, alignment, color);
        SetRect(textObject.GetComponent<RectTransform>(), position, dimensions, anchor);
        return textObject.GetComponent<TMP_Text>();
    }

    private static GameObject TextObject(string name, Transform parent, string value, float size,
        TextAlignmentOptions alignment, Color color)
    {
        GameObject result = NewUI(name, parent);
        TextMeshProUGUI text = result.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null)
            text.font = TMP_Settings.defaultFontAsset;
        return result;
    }

    private static GameObject NewUI(string name, Transform parent)
    {
        GameObject result = new(name, typeof(RectTransform));
        if (parent != null)
            result.transform.SetParent(parent, false);
        return result;
    }

    private static void SetRect(RectTransform rect, Vector2 position, Vector2 dimensions, Vector2 anchor)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
    }

    private static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void ConfigureRow(HorizontalLayoutGroup layout, float spacing)
    {
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = false;
        layout.childControlWidth = false;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;
    }
}
#endif
