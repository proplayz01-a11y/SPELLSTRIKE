using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Builds the approved v02 completion card from independently trimmed sprites.
/// Coordinates are source-native top-left pixels on its 1811x868 reference canvas.
/// No second full-canvas copy, baked typography overlay or duplicate button label.
/// </summary>
public static class TutorialCompletionArtBuilder
{
    private const float CanvasWidth = 1811f;
    private const float CanvasHeight = 868f;
    private static readonly Color Ink = new(21f / 255f, 25f / 255f, 31f / 255f, 1f);
    private static readonly Color Cream = new(0.98f, 0.91f, 0.75f, 1f);

    public static TutorialCompletionBookMotion Build(
        Transform root, TutorialCompletionVisualAssets art,
        UnityAction onContinue, UnityAction onMainMenu,
        out Material[] ownedMaterials)
    {
        RectTransform backdrop = Rect("Backdrop", root);
        Stretch(backdrop);
        Image backdropImage = backdrop.gameObject.AddComponent<Image>();
        backdropImage.color = new Color(0.005f, 0.009f, 0.02f, 0.64f);
        backdropImage.raycastTarget = true; // intentional modal click-through protection

        RectTransform canvas = Rect("CompletionArt_1811x868", root);
        canvas.anchorMin = canvas.anchorMax = new Vector2(0.5f, 0.5f);
        canvas.pivot = new Vector2(0.5f, 0.5f);
        canvas.anchoredPosition = Vector2.zero;
        canvas.sizeDelta = new Vector2(CanvasWidth, CanvasHeight);

        // Source-native layer order, with the requested reused buttons in
        // place of the pack's two button faces.
        Place("Panel_Shadow", canvas, art.PanelShadow, 258, 78, 1304, 734);
        Place("ParchmentFill", canvas, art.ParchmentFill, 302, 122, 1209, 639);
        Place("Frame_FlatTopRail", canvas, art.PanelFrame, 279, 94, 1252, 684);
        Place("HeaderDivider", canvas, art.HeaderDivider, 512, 240, 798, 38);

        Image groundShadow = Place("Spellbook_GroundShadow", canvas, art.GroundShadow,
            405, 559, 495, 89);
        Image castShadow = Place("Spellbook_CastShadow", canvas, art.CastShadow,
            498, 259, 327, 374);
        Image halo = Place("Spellbook_Halo", canvas, art.Halo, 411, 242, 481, 399);

        // Source pivot (651,432), with the native front/back effect separation
        // preserved inside the animated group.
        RectTransform bookPivot = Rect("Spellbook_MotionPivot_651_432", canvas);
        bookPivot.anchorMin = bookPivot.anchorMax = new Vector2(0.5f, 0.5f);
        bookPivot.pivot = new Vector2(0.5f, 0.5f);
        bookPivot.anchoredPosition = new Vector2(651f - CanvasWidth / 2f, CanvasHeight / 2f - 432f);
        bookPivot.sizeDelta = Vector2.zero;
        PlaceAtBookPivot("Spellbook_OrbitBack", bookPivot, art.OrbitBack, 432, 308, 431, 272);
        PlaceAtBookPivot("Spellbook_Body_PlumbBrown_CyanRune", bookPivot,
            art.Spellbook, 510, 267, 281, 329);
        Image rune = PlaceAtBookPivot("Spellbook_OptionalRuneGlow", bookPivot,
            art.RuneGlow, 615, 384, 106, 113);
        rune.color = new Color(1f, 1f, 1f, 0f);
        PlaceAtBookPivot("Spellbook_OrbitFront", bookPivot,
            art.OrbitFront, 549, 532, 292, 97);
        Image sparkles = PlaceAtBookPivot("Spellbook_SparseSparkles", bookPivot,
            art.Sparkles, 416, 287, 442, 314);

        Place("Continue_ContactShadow", canvas, art.ContinueShadow, 553, 617, 351, 104);
        Place("MainMenu_ContactShadow", canvas, art.MainMenuShadow, 928, 625, 326, 91);

        // Word Master's actual gold SUBMIT face, locally letter-cleared so a
        // single live TMP "CONTINUE" label can replace the baked "Submit".
        Image continueFace = Place("ContinueButton", canvas,
            art.ContinueWordMaster, 565, 610, 325, 108);
        continueFace.raycastTarget = true;
        Button continueButton = continueFace.gameObject.AddComponent<Button>();
        continueButton.targetGraphic = continueFace;
        continueButton.transition = Selectable.Transition.SpriteSwap;
        SpriteState spriteState = continueButton.spriteState;
        spriteState.highlightedSprite = art.ContinueWordMasterHover;
        spriteState.pressedSprite = art.ContinueWordMasterHover;
        spriteState.selectedSprite = art.ContinueWordMasterHover;
        continueButton.spriteState = spriteState;
        continueButton.onClick.AddListener(onContinue);

        // This source sprite has its own baked label. Never add TMP on top.
        Image mainMenuFace = Place("MainMenuButton", canvas, art.MainMenuOriginal,
            940, 633, 300, 63);
        mainMenuFace.raycastTarget = true;
        Button mainMenuButton = mainMenuFace.gameObject.AddComponent<Button>();
        mainMenuButton.targetGraphic = mainMenuFace;
        mainMenuButton.onClick.AddListener(onMainMenu);
        ColorBlock menuColors = mainMenuButton.colors;
        menuColors.highlightedColor = new Color(1f, 0.94f, 0.78f, 1f);
        menuColors.pressedColor = new Color(0.80f, 0.74f, 0.64f, 1f);
        mainMenuButton.colors = menuColors;

        TMP_FontAsset cinzel = art.CinzelFont != null ? art.CinzelFont : TMP_Settings.defaultFontAsset;
        TMP_FontAsset italic = art.AlegreyaItalicFont != null ? art.AlegreyaItalicFont : cinzel;
        // Allow TMP to populate its dynamic Cinzel atlas before requesting
        // independent material styling. Cloning its empty default atlas here
        // makes the large headings invisible on some Unity/TMP installations.
        ownedMaterials = Array.Empty<Material>();

        TMP_Text completionHeading = Text("TutorialComplete_Heading", canvas, "Tutorial Complete!",
            500, 154, 810, 116, cinzel, 62f, FontWeight.Regular,
            FontStyles.Normal, TextAlignmentOptions.Center, null, 50f);
        Text("NewItemAcquired_Heading", canvas, "New item acquired:",
            914, 319, 525, 42, cinzel, 32f, FontWeight.Regular,
            FontStyles.Normal, TextAlignmentOptions.Left, null, 28f);
        TMP_Text spellbookHeading = Text("Spellbook_Title", canvas, "Spellbook!",
            914, 350, 525, 110, cinzel, 72f, FontWeight.Regular,
            FontStyles.Normal, TextAlignmentOptions.Left, null, 60f);
        Text("Spellbook_Description", canvas,
            "Every valid word you cast can now be recorded in your Spellbook.",
            914, 464, 525, 102, italic, 39f, FontWeight.Regular,
            FontStyles.Italic, TextAlignmentOptions.Left, null, 32f);
        // Per-text TMP outline for engraved depth, never a raster text
        // underlay or shared font material mutation.
        StyleHeading(completionHeading);
        StyleHeading(spellbookHeading);

        TMP_Text continueText = Text("Continue_TMP_OnlyLabel", continueFace.transform, "CONTINUE",
            0, 0, 325, 108, cinzel, 33f, FontWeight.SemiBold, FontStyles.Normal,
            TextAlignmentOptions.Center, null, 29f, useParentRect: true);
        continueText.color = Ink;

        TutorialCompletionBookMotion motion = canvas.gameObject.AddComponent<TutorialCompletionBookMotion>();
        motion.Initialize(bookPivot, castShadow.rectTransform,
            groundShadow.rectTransform, halo, rune, sparkles);
        return motion;
    }

    private static void StyleHeading(TMP_Text text)
    {
        // TMP creates a per-text material copy (no global Cinzel mutation).
        // A very thin warm edge gives the dark ink restrained engraved depth.
        text.ForceMeshUpdate();
        text.outlineColor = new Color(0.26f, 0.15f, 0.07f, 0.28f);
        text.outlineWidth = 0.025f;
    }

    private static TMP_Text Text(
        string name, Transform parent, string content,
        float x, float y, float width, float height,
        TMP_FontAsset font, float fontSize, FontWeight weight,
        FontStyles style, TextAlignmentOptions alignment, Material material,
        float minimumSize, bool useParentRect = false)
    {
        RectTransform rect = Rect(name, parent);
        if (useParentRect) Stretch(rect);
        else AtSource(rect, x, y, width, height);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.font = font;
        if (material != null) text.fontSharedMaterial = material;
        text.fontSize = fontSize;
        text.fontWeight = weight;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Ink;
        text.enableAutoSizing = true;
        text.fontSizeMin = minimumSize;
        text.fontSizeMax = fontSize;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private static Image Place(
        string name, Transform parent, Sprite sprite,
        float x, float y, float width, float height)
    {
        RectTransform rect = Rect(name, parent);
        AtSource(rect, x, y, width, height);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.raycastTarget = false;
        image.type = Image.Type.Simple;
        return image;
    }

    private static Image PlaceAtBookPivot(
        string name, RectTransform parent, Sprite sprite,
        float x, float y, float width, float height)
    {
        RectTransform rect = Rect(name, parent);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(
            x + width / 2f - 651f, 432f - y - height / 2f);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        return image;
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        return rect;
    }

    private static void AtSource(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(
            x + width / 2f - CanvasWidth / 2f,
            CanvasHeight / 2f - y - height / 2f);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
