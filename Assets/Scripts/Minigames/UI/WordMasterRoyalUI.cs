using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

// Dedicated royal UI for Word Master only. All authoritative game rules remain
// in MinigamesSceneController/WordMasterSession.
// The UI is an overlay canvas; no changes to Spell It Out or Word Excavation.
[DisallowMultipleComponent]
public sealed class WordMasterRoyalUI : MonoBehaviour
{
    private const string ResourcesRoot = "WordMasterRoyal/";
    private static readonly int[] Prizes = { 10000, 5000, 2500, 1000, 500 };
    private static readonly Color NavyInk = new Color(0.10f, 0.11f, 0.16f, 1f);
    private static readonly Color Ivory = new Color(1f, .95f, .84f, 1f);

    [SerializeField] private MinigamesSceneController controller;
    [SerializeField] private TMP_FontAsset cinzelFont;

    private readonly Sprite[] letters = new Sprite[26];
    private readonly Image[,] tileBase = new Image[5, 5];
    private readonly Image[,] tileLetter = new Image[5, 5];
    private readonly Image[] keyBase = new Image[26];
    private readonly Button[] keyButton = new Button[26];
    private readonly Image[] turnPlaque = new Image[5];
    private readonly Image[] prizePlaque = new Image[5];
    private readonly Image[] turnGlow = new Image[5];
    private readonly Image[] prizeGlow = new Image[5];
    private readonly Image[] activeRowFrame = new Image[5];
    private readonly TextMeshProUGUI[] turnText = new TextMeshProUGUI[5];
    private readonly TextMeshProUGUI[] prizeText = new TextMeshProUGUI[5];
    private readonly Image[] keyLetter = new Image[26];
    private readonly TextMeshProUGUI[] wrongMarks = new TextMeshProUGUI[5];

    private Sprite backgroundSprite, boardSprite, keyboardFrameSprite, headerSprite;
    private Sprite approvedWordMasterRibbonSprite;
    private const string StageSelectBackdropPrefab = "StageSelectAnimatedBackdrop";
    private Sprite slotSprite, normalSprite, goldSprite, silverSprite, graySprite, redSprite;
    private Sprite hintSprite, mainHintSprite, hintGlowSprite, arrowSprite, submitSprite, submitHoverSprite, submitDisabledSprite, menuSprite;
    private Sprite turnActiveSprite, turnInactiveSprite, prizeActiveSprite, prizeInactiveSprite, turnGlowSprite, prizeGlowSprite;

    private RectTransform layout;
    private RectTransform board;
    private GameObject hintGroup, pauseGroup, resultGroup, tutorialArrow;
    private Image hintGlow;
    private TextMeshProUGUI hintMessage, hintTitle, feedbackLabel, resultTitle, resultMessage, headerTitle, scoreLabel, wordsLabel;
    private Button submitButton;
    private bool showingPause;
    private string lastState = string.Empty;
    private bool assetsReady;
    private string contextualClue = string.Empty;
    private int previouslyRenderedTurn=-1;
    private readonly Image[] victoryStars = new Image[3];
    private Sprite victoryStarSprite;
    // Bookworm reference callouts. Built at runtime for both the current
    // serialized scene and the full UI builder path. Clicks are intercepted
    // while a hint is visible, so input never leaks into gameplay.
    private readonly GameObject[] infoPanels = new GameObject[3];
    private readonly TextMeshProUGUI[] infoBodies = new TextMeshProUGUI[3];
    private readonly bool[] infoExpanded = new bool[3];
    private readonly bool[] infoPinned = new bool[3];
    private readonly bool[] infoDismissed = new bool[3];
    private string lastInvalidGuess = string.Empty;
    private Button dismissAnywhereButton;
    private int infoTurn = -1;
    private bool infoPreviousStartHint;
    private const int InfoTurns = 0, InfoPrize = 1, InfoInvalid = 2;

    private static Sprite Load(string name) => Resources.Load<Sprite>(ResourcesRoot + name);

    private void LoadAssets()
    {
        if (assetsReady) return;
        backgroundSprite = Load("Background");
        boardSprite = Load("Board");
        keyboardFrameSprite = Load("KeyboardFrameCompact");
        headerSprite = Load("Header");
        approvedWordMasterRibbonSprite = Load("WordMasterFantasyBanner");
        victoryStarSprite=Load("VictoryStar");
        slotSprite = Load("Slot");
        normalSprite = Load("Normal");
        goldSprite = Load("Gold");
        silverSprite = Load("Silver");
        graySprite = Load("DarkGray");
        redSprite = Load("Red");
        // The approved ornate art already has an opaque charcoal interior.
        // Aspect-fitted variants preserve its stars and center crest without
        // stretching them when used by the two different hint sizes.
        hintSprite = Load("HintDarkOrnatePopup") ?? Load("Hint");
        mainHintSprite = Load("HintDarkOrnateMain") ?? hintSprite;
        hintGlowSprite = Load("HintGlow");
        arrowSprite = Load("Arrow");
        submitSprite = Load("Submit");
        submitHoverSprite = Load("SubmitHover");
        submitDisabledSprite = Load("SubmitDisabled");
        menuSprite = Load("Menu");
        turnActiveSprite = Load("TurnActive");
        turnInactiveSprite = Load("TurnInactive");
        turnGlowSprite = Load("TurnGlow");
        prizeActiveSprite = Load("PrizeActive");
        prizeInactiveSprite = Load("PrizeInactive");
        prizeGlowSprite = Load("PrizeGlow");
        for (int i = 0; i < 26; i++) letters[i] = Load("L_" + (char)('A' + i));
        assetsReady = slotSprite != null && normalSprite != null && goldSprite != null
            && silverSprite != null && boardSprite != null && letters[0] != null && letters[25] != null;
        if (!assetsReady)
            Debug.LogError("[WordMasterRoyalUI] Required Royal sprites are missing from Resources/WordMasterRoyal.");
    }

    private void Awake()
    {
        if (controller == null)
            controller = FindFirstObjectByType<MinigamesSceneController>();
        LoadAssets();
        if (transform.Find("RoyalLayout") == null)
            BuildLayout();
        else
            BindExisting();

        ApplyApprovedHintSkin();
        ApplyCinzelToWordMasterTexts();

        // Keep the approved layout, board, buttons and game rules intact.
        // The ribbon, hint artwork and Cinzel typography are visual-only.
        ApplyApprovedWordMasterRibbon();
        if (Application.isPlaying)
            EnsureStageSelectAnimatedBackground();
    }

    private void ApplyApprovedWordMasterRibbon()
    {
        if (layout == null || approvedWordMasterRibbonSprite == null)
            return;

        RectTransform ribbon = layout.Find("TitleRibbon") as RectTransform;
        if (ribbon == null)
            return;

        Image image = ribbon.GetComponent<Image>();
        if (image == null)
            return;

        image.sprite = approvedWordMasterRibbonSprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = false; // match the existing wide Stage Select ribbon treatment
        image.color = Color.white;
        image.raycastTarget = false;

        // Exact author-approved Play Mode placement (2026-11-01).
        // Mirror the persisted Edit Mode RectTransform, including the user's
        // nonuniform XY/Z scaling; never reset their approved proportions.
        ribbon.anchoredPosition = new Vector2(75f, 446.73f);
        ribbon.sizeDelta = new Vector2(970f, 182f);
        ribbon.localScale = new Vector3(.91f, 1.59f, .81f);

        // The approved asset already contains the correct gold serif WORD
        // MASTER lettering. Hide the old TMP overlay to prevent ghost text.
        TextMeshProUGUI oldTitle = ribbon.Find("WordMasterTitle")?.GetComponent<TextMeshProUGUI>();
        if (oldTitle != null)
            oldTitle.enabled = false;
    }

    private void ApplyApprovedHintSkin()
    {
        if (hintGroup == null || mainHintSprite == null)
            return;

        // Update the already serialized WordMasterScene as well as a fresh
        // BuildLayout instance. Never insert a second background panel.
        Transform root = hintGroup.transform;
        Image artwork = root.Find("HintParchment")?.GetComponent<Image>();
        if (artwork != null)
        {
            artwork.sprite = mainHintSprite;
            artwork.type = Image.Type.Simple;
            artwork.preserveAspect = false;
            artwork.color = Color.white;
            artwork.raycastTarget = false;
        }

        // These were decorations for the old pale parchment; the new PNG
        // already includes its black interior and complete gold border.
        if (hintGlow != null) hintGlow.enabled = false;
        Image divider = root.Find("HintDivider")?.GetComponent<Image>();
        if (divider != null) divider.enabled = false;

        if (hintTitle != null) hintTitle.color = new Color(1f, .82f, .43f, 1f);
        if (hintMessage != null) hintMessage.color = Ivory;
        var closeX = root.Find("HintCloseX");
        if (closeX != null) closeX.gameObject.SetActive(false);
    }

    private void ApplyCinzelToWordMasterTexts()
    {
        if (layout == null || cinzelFont == null)
            return;

        // Tile letters are artwork, not TMP; all other Word Master labels
        // share the approved Cinzel asset without changing other scenes.
        foreach (TextMeshProUGUI label in layout.GetComponentsInChildren<TextMeshProUGUI>(true))
            if (label != null && label.font != cinzelFont)
                label.font = cinzelFont;
    }

    private void EnsureStageSelectAnimatedBackground()
    {
        if (layout == null || layout.Find(StageSelectBackdropPrefab) != null)
            return;

        // This prefab is an isolated copy of Stage Select's original
        // HallOfChronicles LibraryBackground: base 2400x1080 artwork,
        // 39 flickering light/candle layers and runtime golden dust.
        GameObject source = Resources.Load<GameObject>(ResourcesRoot + StageSelectBackdropPrefab);
        if (source == null)
        {
            Debug.LogWarning("[WordMasterRoyalUI] Stage Select animated background prefab unavailable; keeping existing backdrop.");
            return;
        }

        GameObject backdrop = Instantiate(source, layout, false);
        backdrop.name = StageSelectBackdropPrefab;
        RectTransform rect = backdrop.transform as RectTransform;
        if (rect != null)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(2400f, 1080f);
            rect.localScale = Vector3.one;
        }
        backdrop.transform.SetAsFirstSibling();

        // Disable only the old static background Image. Its GameObject and
        // every unrelated Word Master object remain intact.
        Image staticBackdrop = layout.Find("LibraryBackdrop")?.GetComponent<Image>();
        if (staticBackdrop != null)
            staticBackdrop.enabled = false;

        HallOfChroniclesAmbient ambience = backdrop.GetComponent<HallOfChroniclesAmbient>();
        if (ambience == null || !ambience.IsAnimating)
            Debug.LogWarning("[WordMasterRoyalUI] Stage Select backdrop instantiated but flicker animation is disabled.");
    }

    private void Start()
    {
        if (Application.isPlaying)
        {
            showingPause = false;
            if (pauseGroup != null) pauseGroup.SetActive(false);
            lastState = string.Empty;
            EnsureInfoPanels();
            ApplyCinzelToWordMasterTexts();
            EnsureDismissAnywhereButton();
            RefreshGameplay(true);
            RefreshInfoPanels();
        }
    }

    // Bookworm-style dense Word Master keyboard reference:
    // A-M in row one, N-Z in row two, centered with tight edge-to-edge gaps.
    private static Vector2 GetCompactKeyPosition(int index)
    {
        return new Vector2(((index % 13) - 6) * 99f, index < 13 ? 46f : -46f);
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 center, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = center;
        rect.sizeDelta = size;
        return rect;
    }

    private static Image CreateImage(string name, Transform parent, Sprite sprite, Vector2 center, Vector2 size,
        bool raycast = false, bool sliced = false)
    {
        RectTransform rect = CreateRect(name, parent, center, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = raycast;
        image.preserveAspect = false; // shared normalized slot/tile/letter canvases stay aligned
        image.type = sliced && sprite != null && sprite.border.sqrMagnitude > .01f
            ? Image.Type.Sliced : Image.Type.Simple;
        return image;
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, string contents,
        Vector2 center, Vector2 size, float fontSize, Color color, bool bold = true)
    {
        RectTransform rect = CreateRect(name, parent, center, size);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (cinzelFont != null) text.font = cinzelFont;
        text.fontSize = fontSize;
        text.text = contents;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    private static Button MakeButton(Image image, Action onClick)
    {
        image.raycastTarget = true;
        Button b = image.gameObject.AddComponent<Button>();
        b.targetGraphic = image;
        b.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = b.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, .98f, .88f, 1f);
        colors.pressedColor = new Color(.75f, .78f, .84f, 1f);
        colors.disabledColor = new Color(.58f, .58f, .60f, 1f);
        colors.fadeDuration = .12f;
        b.colors = colors;
        if (onClick != null)
            b.onClick.AddListener(() => onClick());
        return b;
    }

    // Invoked once by the Word Master editor builder. Does not modify other scenes.
    public void BuildLayout()
    {
        if (controller == null)
            controller = FindFirstObjectByType<MinigamesSceneController>();
        LoadAssets();
        if (!assetsReady) return;

        Transform existing = transform.Find("RoyalLayout");
        if (existing != null)
        {
            if (Application.isPlaying) Destroy(existing.gameObject);
            else DestroyImmediate(existing.gameObject);
        }

        layout = CreateRect("RoyalLayout", transform, Vector2.zero, new Vector2(2400, 1080));
        Image background = CreateImage("LibraryBackdrop", layout, backgroundSprite, Vector2.zero, new Vector2(2400,1080), true);
        background.color = Color.white;
        CreateImage("RoyalBackdropDim", layout, null, Vector2.zero, new Vector2(2400,1080)).color =
            new Color(0f, .016f, .037f, .27f);

        board = CreateRect("Board", layout, new Vector2(75, 110), new Vector2(1230, 845));
        Image parchmentImage = CreateImage("ParchmentBoard", board, boardSprite, new Vector2(0,-34), board.sizeDelta, false, true);
        parchmentImage.rectTransform.localScale = new Vector3(.98f,1.23f,1f);
        CreateText("Instructions", board,
            "Guess the hidden 5-letter word.\nGold = right spot. Silver = right letter.",
            new Vector2(0, 205), new Vector2(860, 75), 28, NavyInk, false);
        CreateImage("ParchmentDivider", board, null, new Vector2(0, 171), new Vector2(790, 2)).color =
            new Color(.62f, .37f, .16f, .56f);
        CreateText("TurnsHeader", board, "Turns", new Vector2(-366, 192), new Vector2(155, 48), 31, NavyInk);
        CreateText("PrizeHeader", board, "Prize", new Vector2(356, 199), new Vector2(166, 48), 31, NavyInk);

        const float rowTop = 109f;
        const float rowStep = 92f;
        for (int row = 0; row < 5; row++)
        {
            int capturedRow = row;
            float y = rowTop - row * rowStep;
            RectTransform line = CreateRect("Row" + (row+1), board, new Vector2(0, y), new Vector2(950, 94));

            Image rowOutline = CreateImage("ActiveRowGlow", line, keyboardFrameSprite, new Vector2(-55, 0), new Vector2(584, 98), false, true);
            rowOutline.color = new Color(1f, .79f, .23f, .19f);
            activeRowFrame[row] = rowOutline;

            Image turn = CreateImage("TurnPlaque", line, turnInactiveSprite, new Vector2(-390,0), new Vector2(118,80));
            turnPlaque[row] = turn;
            turnGlow[row] = CreateImage("TurnGlow", turn.transform, turnGlowSprite, Vector2.zero, new Vector2(118,80));
            turnText[row] = CreateText("TurnNumber", turn.transform, (row+1).ToString(), Vector2.zero,
                new Vector2(98,65), 32, NavyInk);

            for (int col = 0; col < 5; col++)
            {
                int capturedCol = col;
                RectTransform cell = CreateRect("Slot" + (col+1), line,
                    new Vector2(-275 + col * 111, 0), new Vector2(97, 97));
                Image slot = CreateImage("Holder", cell, slotSprite, Vector2.zero, cell.sizeDelta);
                Image tile = CreateImage("TileBase", cell, normalSprite, Vector2.zero, cell.sizeDelta);
                Image letter = CreateImage("LetterOverlay", cell, null, Vector2.zero, cell.sizeDelta);
                tileBase[row, col] = tile;
                tileLetter[row, col] = letter;
                tile.gameObject.SetActive(false);
                letter.gameObject.SetActive(false);
                if (slot != null)
                {
                    MakeButton(slot, () =>
                    {
                        if (showingPause || controller == null) return;
                        if (capturedRow == controller.RoyalTurn)
                            controller.RoyalRemoveAt(capturedCol);
                        else
                            ShowContextClue(capturedRow, capturedCol);
                    });
                    BindTileHover(slot, capturedRow, capturedCol);
                }
            }

            Image prize = CreateImage("PrizePlaque", line, prizeInactiveSprite, new Vector2(356,0), new Vector2(152,80));
            prizePlaque[row] = prize;
            prizeGlow[row] = CreateImage("PrizeGlow", prize.transform, prizeGlowSprite, Vector2.zero, new Vector2(152,80));
            prizeText[row] = CreateText("PrizeNumber", prize.transform, Prizes[row].ToString("N0"),
                Vector2.zero, new Vector2(140,66), 27, NavyInk);
            // Bookworm's clear red X appears beside each forfeited prize.
            wrongMarks[row] = CreateText("WrongCross", line, "X", new Vector2(470,0),
                new Vector2(76,84), 72, new Color(.86f,.06f,.045f,1f));
            wrongMarks[row].gameObject.SetActive(false);
        }

        Image ribbon = CreateImage("TitleRibbon", layout, headerSprite, new Vector2(75, 446.73f), new Vector2(970, 182));
        headerTitle = CreateText("WordMasterTitle", ribbon.transform, "WORD MASTER", new Vector2(0, 8),
            new Vector2(735,90), 58, new Color(.99f, .84f, .42f, 1f));

        RectTransform scoreboard = CreateRect("ScoreBoard",layout,new Vector2(-812,385),new Vector2(490,178));
        CreateImage("ScorePlaque",scoreboard,boardSprite,Vector2.zero,scoreboard.sizeDelta);
        scoreLabel = CreateText("ScoreLabel",scoreboard,"Score   0",new Vector2(0,35),new Vector2(410,55),34,NavyInk);
        wordsLabel = CreateText("WordsLabel",scoreboard,"Words   0",new Vector2(0,-30),new Vector2(410,55),34,NavyInk);
        CreateImage("ScoreDivider",scoreboard,null,new Vector2(0,1),new Vector2(310,2)).color =
            new Color(.57f,.38f,.18f,.64f);

        RectTransform hint = CreateRect("HintPanelGroup", layout, new Vector2(-795, 66), new Vector2(560,308));
        hintGroup = hint.gameObject;
        CreateImage("HintParchment", hint, mainHintSprite, Vector2.zero, hint.sizeDelta);
        hintGlow = CreateImage("HintOutsideGlow", hint, hintGlowSprite, Vector2.zero, hint.sizeDelta);
        hintTitle = CreateText("HintTitle", hint, "Hint!", new Vector2(0, 77), new Vector2(490,72), 56, new Color(1f,.82f,.43f,1f));
        CreateImage("HintDivider", hint, null, new Vector2(0, 27), new Vector2(355, 2)).color = new Color(.55f,.36f,.21f,.70f);
        hintMessage = CreateText("HintMessage", hint, "The first letter is revealed!", new Vector2(0,-33),
            new Vector2(455,110), 33, Ivory, false);
        Image dismissImage = CreateImage("DismissHint",hint, null,new Vector2(235,119),new Vector2(45,35));
        dismissImage.color = new Color(1f, 1f, 1f, .01f);
        MakeButton(dismissImage, () => {
            if(controller!=null) controller.RoyalDismissHint();
        });
        CreateText("HintCloseX", hint, "×", new Vector2(235,119), new Vector2(38,34), 24, NavyInk);

        tutorialArrow = CreateImage("TutorialArrow",layout,arrowSprite,new Vector2(-358,160),new Vector2(364,173)).gameObject;

        RectTransform keyboard = CreateRect("Keyboard", layout, new Vector2(0,-318), new Vector2(1570,220));
        // Use the user's approved Ornate Empty Fantasy UI Panel directly.
        // Its artwork already has the complete navy center and gold details.
        // An opaque overlay would hide the compass corners and ornate border.
        Image frameArt = CreateImage("KeyboardNavyFrame", keyboard, keyboardFrameSprite, Vector2.zero, keyboard.sizeDelta);
        frameArt.rectTransform.localScale = new Vector3(.92f,1.26f,1f);
        for (int i = 0; i < 26; i++)
        {
            int captured = i;
            char ch = (char)('A'+i);
            // The reference shows two compact, non-staggered lines of thirteen tiles.
            // 126-unit canvas has a ~99x92-unit visible tile after alpha padding.
            // 99-unit columns and 92-unit rows make adjacent tiles visually touch.
            Vector2 keyPosition = GetCompactKeyPosition(i);
            RectTransform key = CreateRect("Key_"+ch,keyboard,keyPosition,new Vector2(126,126));
            Image tile = CreateImage("Tile",key,normalSprite,Vector2.zero,key.sizeDelta,false);
            keyBase[i] = tile;
            Image letter = CreateImage("Letter",key,letters[i],Vector2.zero,new Vector2(155,155));
            letter.rectTransform.localScale = new Vector3(.75f,.75f,.75f);
            keyLetter[i] = letter;
            // The transparent PNG includes ~20% padding. Use a smaller hitbox
            // matching the visible tile so neighbouring compact keys never
            // steal each other's pointer events (including across both rows).
            Image hitbox = CreateImage("Hitbox",key,null,Vector2.zero,new Vector2(96,90),true);
            hitbox.color=new Color(1f,1f,1f,.01f);
            Button button = MakeButton(hitbox, () => { if (!showingPause && controller != null) controller.RoyalPress(ch); });
            button.targetGraphic=tile;
            keyButton[i] = button;
        }

        Image submitImage = CreateImage("SubmitButton", layout, submitSprite, new Vector2(-215,-492),new Vector2(354,102),true);
        submitButton = MakeButton(submitImage, () => { if(!showingPause && controller!=null) controller.RoyalSubmit(); });
        submitButton.transition = Selectable.Transition.SpriteSwap;
        SpriteState submitState = submitButton.spriteState;
        submitState.highlightedSprite = submitHoverSprite;
        submitState.selectedSprite = submitHoverSprite;
        submitState.pressedSprite = submitHoverSprite;
        submitState.disabledSprite = submitDisabledSprite;
        submitButton.spriteState = submitState;

        MakeButton(CreateImage("MenuButton",layout,menuSprite,new Vector2(215,-491.8f),new Vector2(354,102),true),
            () => SetPauseVisible(true));
        feedbackLabel = CreateText("Feedback", layout, string.Empty, new Vector2(-815,-135),
            new Vector2(520,130), 29, Ivory, false);
        // All Word Master feedback now lives inside the ornate Hint/Clues frame.
        feedbackLabel.gameObject.SetActive(false);

        pauseGroup = CreatePopup("RoyalPause", "PAUSED", "Continue, restart, or leave Word Master.",
            new [] { "Resume", "New Round", "Mode Select" },
            new Action[] { () => SetPauseVisible(false), () => { SetPauseVisible(false); if(controller!=null)controller.RoyalRestart(); }, () => { if(controller!=null)controller.RoyalExitToModeSelect(); } });
        resultGroup = CreatePopup("RoyalResult", "WORD MASTER", "Round complete.",
            new [] { "New Round", "Mode Select", "Main Menu" },
            new Action[] { () => { if(controller!=null) controller.RoyalRestart(); }, () => { if(controller!=null)controller.RoyalExitToModeSelect(); }, () => { if(controller!=null) controller.RoyalExitToMainMenu(); } });
        resultTitle = resultGroup.transform.Find("Dialog/PopupTitle")?.GetComponent<TextMeshProUGUI>();
        resultMessage = resultGroup.transform.Find("Dialog/PopupMessage")?.GetComponent<TextMeshProUGUI>();
        for(int i=0;i<victoryStars.Length;i++)
        {
            victoryStars[i]=CreateImage("VictoryStar_"+i,resultGroup.transform.Find("Dialog"),
                victoryStarSprite,new Vector2((i-1)*120,295),new Vector2(74,74));
            victoryStars[i].gameObject.SetActive(false);
        }
        pauseGroup.SetActive(false);
        resultGroup.SetActive(false);
        SetEditPreview();
        ApplyApprovedWordMasterRibbon();
        lastState = string.Empty;
    }

    private GameObject CreatePopup(string name, string title, string message, string[] labels, Action[] actions)
    {
        RectTransform root = CreateRect(name,layout,Vector2.zero,new Vector2(2400,1080));
        CreateImage("Dimmer",root,null,Vector2.zero,root.sizeDelta,true).color = new Color(.02f,.025f,.042f,.81f);
        RectTransform dialog = CreateRect("Dialog",root,Vector2.zero,new Vector2(895,780));
        CreateImage("Panel",dialog,boardSprite,Vector2.zero,dialog.sizeDelta);
        CreateText("PopupTitle",dialog,title,new Vector2(0,187),new Vector2(645,105),60,NavyInk);
        CreateText("PopupMessage",dialog,message,new Vector2(0,91),new Vector2(650,102),32,NavyInk,false);
        for(int i=0;i<labels.Length && i<actions.Length;i++)
        {
            Image image = CreateImage("Action_"+i,dialog,turnActiveSprite,new Vector2(0,-35-i*106),new Vector2(415,88),true);
            Button b=MakeButton(image,actions[i]);
            CreateText("Label",image.transform,labels[i],Vector2.zero,new Vector2(380,76),35,NavyInk);
        }
        return root.gameObject;
    }

    private void SetEditPreview()
    {
        if(hintGroup!=null) hintGroup.SetActive(true);
        if(tutorialArrow!=null)tutorialArrow.SetActive(true);
        if(hintMessage!=null) hintMessage.text="<color=#B52B26>S</color> is the first letter\nof the mystery word!";
        for(int row=0;row<5;row++)
        {
            turnPlaque[row].sprite = row==0?turnActiveSprite:turnInactiveSprite;
            prizePlaque[row].sprite = row==0?prizeActiveSprite:prizeInactiveSprite;
            if(turnGlow[row]!=null) turnGlow[row].gameObject.SetActive(row==0);
            if(prizeGlow[row]!=null) prizeGlow[row].gameObject.SetActive(row==0);
            if(activeRowFrame[row]!=null)activeRowFrame[row].gameObject.SetActive(row==0);
            turnText[row].color = row==0 ? NavyInk : Ivory;
            prizeText[row].color = row==0 ? NavyInk : Ivory;
        }
        if(tileBase[0,0]!=null)
        {
            tileBase[0,0].sprite=goldSprite;
            tileBase[0,0].gameObject.SetActive(true);
            tileLetter[0,0].sprite=letters['S'-'A'];
            tileLetter[0,0].gameObject.SetActive(true);
        }
        if(submitButton!=null)submitButton.interactable=false;
    }

    private void BindExisting()
    {
        layout = transform.Find("RoyalLayout") as RectTransform;
        if(layout==null){BuildLayout();return;}
        board = layout.Find("Board") as RectTransform;
        if(board==null){BuildLayout();return;}
        for(int row=0;row<5;row++)
        {
            int boundRow = row;
            var line=board.Find("Row"+(row+1));
            if(line==null){BuildLayout();return;}
            activeRowFrame[row]=line.Find("ActiveRowGlow")?.GetComponent<Image>();
            turnPlaque[row]=line.Find("TurnPlaque")?.GetComponent<Image>();
            prizePlaque[row]=line.Find("PrizePlaque")?.GetComponent<Image>();
            turnGlow[row]=turnPlaque[row]?.transform.Find("TurnGlow")?.GetComponent<Image>();
            prizeGlow[row]=prizePlaque[row]?.transform.Find("PrizeGlow")?.GetComponent<Image>();
            turnText[row]=turnPlaque[row]?.transform.Find("TurnNumber")?.GetComponent<TextMeshProUGUI>();
            prizeText[row]=prizePlaque[row]?.transform.Find("PrizeNumber")?.GetComponent<TextMeshProUGUI>();
            wrongMarks[row]=line.Find("WrongCross")?.GetComponent<TextMeshProUGUI>();
            // Older serialized scenes get this light-weight overlay at runtime.
            if(wrongMarks[row]==null && Application.isPlaying)
                wrongMarks[row]=CreateText("WrongCross",line,"X",new Vector2(470,0),
                    new Vector2(76,84),72,new Color(.86f,.06f,.045f,1f));
            for(int col=0;col<5;col++)
            {
                var cell=line.Find("Slot"+(col+1));
                tileBase[row,col]=cell?.Find("TileBase")?.GetComponent<Image>();
                tileLetter[row,col]=cell?.Find("LetterOverlay")?.GetComponent<Image>();
                var holder=cell?.Find("Holder")?.GetComponent<Image>();
                var b=holder!=null?holder.GetComponent<Button>():null;
                if(b!=null)
                {
                    b.onClick.RemoveAllListeners();
                    int captured=col;
                    b.onClick.AddListener(()=>
                    {
                        if(showingPause || controller==null)return;
                        if(boundRow==controller.RoyalTurn)controller.RoyalRemoveAt(captured);
                        else ShowContextClue(boundRow,captured);
                    });
                }
                if(holder!=null)BindTileHover(holder,boundRow,col);
            }
        }
        var k=layout.Find("Keyboard");
        // Migration for scenes serialized before the approved ornate panel was
        // properly exposed. Runtime-only adjustment preserves other unsaved
        // scene edits and does not require regenerating or copying textures.
        if(k!=null)
        {
            var keyboardRect=k as RectTransform;
            if(keyboardRect!=null)
            {
                keyboardRect.sizeDelta=new Vector2(1570,220);
                keyboardRect.anchoredPosition=new Vector2(0,-318);
            }
            var ornateFrame=k.Find("KeyboardNavyFrame") as RectTransform;
            if(ornateFrame!=null)
            {
                ornateFrame.sizeDelta=new Vector2(1570,220);
                ornateFrame.localScale=new Vector3(.92f,1.26f,1f);
                var ornateImage=ornateFrame.GetComponent<Image>();
                if(ornateImage!=null)
                {
                    ornateImage.sprite=keyboardFrameSprite;
                    ornateImage.type=Image.Type.Simple;
                    ornateImage.color=Color.white;
                }
            }
            var oldOpaqueBacking=k.Find("KeyboardNavyBacking");
            if(oldOpaqueBacking!=null) oldOpaqueBacking.gameObject.SetActive(false);
        }
        for(int i=0;i<26;i++)
        {
            char ch=(char)('A'+i);var key=k?.Find("Key_"+ch);
            // The production scene already serialized a looser 93px/105px
            // grid. Migrate only the key RectTransforms on load, without
            // changing the saved scene or the approved source artwork.
            var keyRect = key as RectTransform;
            if(keyRect!=null)
            {
                keyRect.anchoredPosition=GetCompactKeyPosition(i);
                keyRect.sizeDelta=new Vector2(126,126);
                var tileRect=key.Find("Tile") as RectTransform;
                var letterRect=key.Find("Letter") as RectTransform;
                if(tileRect!=null)tileRect.sizeDelta=new Vector2(126,126);
                if(letterRect!=null)
                {
                    letterRect.sizeDelta=new Vector2(155,155);
                    letterRect.localScale=new Vector3(.75f,.75f,.75f);
                }
            }
            keyBase[i]=key?.Find("Tile")?.GetComponent<Image>();
            keyLetter[i]=key?.Find("Letter")?.GetComponent<Image>();
            if(keyBase[i]!=null)keyBase[i].raycastTarget=false;
            if(keyLetter[i]!=null)keyLetter[i].raycastTarget=false;

            // Upgrade the older scene's full-size Button in Play Mode only.
            // The invisible 96x90 hitbox fits within the visual tile's alpha
            // bounds; graphics and hover transitions still use the Tile image.
            var oldButton=keyBase[i]!=null?keyBase[i].GetComponent<Button>():null;
            if(oldButton!=null)oldButton.enabled=false;
            var hit=key?.Find("Hitbox")?.GetComponent<Image>();
            if(hit==null && key!=null)
            {
                hit=CreateImage("Hitbox",key,null,Vector2.zero,new Vector2(96,90),true);
                hit.color=new Color(1f,1f,1f,.01f);
            }
            if(hit!=null)
            {
                hit.raycastTarget=true;
                var hitRect=hit.rectTransform;
                hitRect.sizeDelta=new Vector2(96,90);
                var button=hit.GetComponent<Button>();
                if(button==null)button=MakeButton(hit,null);
                button.targetGraphic=keyBase[i];
                button.onClick.RemoveAllListeners();
                char captured=ch;
                button.onClick.AddListener(()=>{if(!showingPause&&controller!=null)controller.RoyalPress(captured);});
                keyButton[i]=button;
            }
        }
        var ribbon=layout.Find("TitleRibbon");
        headerTitle=ribbon?.Find("WordMasterTitle")?.GetComponent<TextMeshProUGUI>();
        var scoreBoard = layout.Find("ScoreBoard");
        scoreLabel = scoreBoard?.Find("ScoreLabel")?.GetComponent<TextMeshProUGUI>();
        wordsLabel = scoreBoard?.Find("WordsLabel")?.GetComponent<TextMeshProUGUI>();
        var hint=layout.Find("HintPanelGroup");
        hintGroup=hint?.gameObject;
        hintTitle=hint?.Find("HintTitle")?.GetComponent<TextMeshProUGUI>();
        tutorialArrow=layout.Find("TutorialArrow")?.gameObject;
        hintGlow=hint?.Find("HintOutsideGlow")?.GetComponent<Image>();
        hintMessage=hint?.Find("HintMessage")?.GetComponent<TextMeshProUGUI>();
        var dismiss=hint?.Find("DismissHint")?.GetComponent<Button>();
        if(dismiss!=null){dismiss.onClick.RemoveAllListeners();dismiss.onClick.AddListener(()=>{if(controller!=null)controller.RoyalDismissHint();});}
        submitButton=layout.Find("SubmitButton")?.GetComponent<Button>();
        if(submitButton!=null){submitButton.onClick.RemoveAllListeners();submitButton.onClick.AddListener(()=>{if(!showingPause&&controller!=null)controller.RoyalSubmit();});}
        var menu=layout.Find("MenuButton")?.GetComponent<Button>();
        if(menu!=null){menu.onClick.RemoveAllListeners();menu.onClick.AddListener(()=>SetPauseVisible(true));}
        feedbackLabel=layout.Find("Feedback")?.GetComponent<TextMeshProUGUI>();
        pauseGroup=layout.Find("RoyalPause")?.gameObject;
        resultGroup=layout.Find("RoyalResult")?.gameObject;
        resultTitle=resultGroup?.transform.Find("Dialog/PopupTitle")?.GetComponent<TextMeshProUGUI>();
        resultMessage=resultGroup?.transform.Find("Dialog/PopupMessage")?.GetComponent<TextMeshProUGUI>();
        for(int i=0;i<victoryStars.Length;i++)
        {
            victoryStars[i]=resultGroup?.transform.Find("Dialog/VictoryStar_"+i)?.GetComponent<Image>();
            if(victoryStars[i]==null && resultGroup!=null && Application.isPlaying)
                victoryStars[i]=CreateImage("VictoryStar_"+i,resultGroup.transform.Find("Dialog"),
                    victoryStarSprite,new Vector2((i-1)*120,295),new Vector2(74,74));
            if(victoryStars[i]!=null)victoryStars[i].sprite=victoryStarSprite;
        }
        BindPopupButtons(pauseGroup, new Action[]{()=>SetPauseVisible(false),()=>{SetPauseVisible(false);if(controller!=null)controller.RoyalRestart();},()=>{if(controller!=null)controller.RoyalExitToModeSelect();}});
        BindPopupButtons(resultGroup,new Action[]{()=>{if(controller!=null)controller.RoyalRestart();},()=>{if(controller!=null)controller.RoyalExitToModeSelect();},()=>{if(controller!=null)controller.RoyalExitToMainMenu();}});
    }

    private static void BindPopupButtons(GameObject overlay, Action[] actions)
    {
        var dialog = overlay != null ? overlay.transform.Find("Dialog") : null;
        if(dialog==null)return;
        for(int i=0;i<actions.Length;i++)
        {
            var b=dialog.Find("Action_"+i)?.GetComponent<Button>();
            if(b==null)continue;
            b.onClick.RemoveAllListeners();
            int index=i;b.onClick.AddListener(()=>actions[index]());
        }
    }

    private void BindInfoHover(Image image, int index)
    {
        if(image==null)return;
        image.raycastTarget=true;
        var trigger=image.GetComponent<EventTrigger>();
        if(trigger==null)trigger=image.gameObject.AddComponent<EventTrigger>();
        if(trigger.triggers==null)trigger.triggers=new List<EventTrigger.Entry>();
        var enter=new EventTrigger.Entry {eventID=EventTriggerType.PointerEnter};
        enter.callback.AddListener(_=>{infoExpanded[index]=true;});
        trigger.triggers.Add(enter);
        var exit=new EventTrigger.Entry {eventID=EventTriggerType.PointerExit};
        exit.callback.AddListener(_=>{infoExpanded[index]=false;});
        trigger.triggers.Add(exit);
        // On Android there is no hover. Tapping the plaque or hint toggles
        // the detailed explanation without changing the guess/score.
        var tap=new EventTrigger.Entry {eventID=EventTriggerType.PointerClick};
        tap.callback.AddListener(_=>
        {
            if(infoPanels[index]!=null && infoPanels[index].activeInHierarchy)
                DismissVisibleHints();
            else infoPinned[index]=!infoPinned[index];
        });
        trigger.triggers.Add(tap);
    }

    private void EnsureInfoPanels()
    {
        if(!Application.isPlaying || layout==null || infoPanels[0]!=null)return;
        string[] names={"TurnsInfoPopup","PrizeInfoPopup","InvalidSubmitPopup"};
        for(int i=0;i<names.Length;i++)
        {
            var group=CreateRect(names[i],layout,Vector2.zero,new Vector2(490,228));
            var artwork=CreateImage("ApprovedHintFrame",group,hintSprite,Vector2.zero,group.sizeDelta,true);
            artwork.color=Color.white;
            BindInfoHover(artwork,i);
            // The approved artwork includes the charcoal body. No extra
            // DarkHintInterior or black overlay is needed (or permitted).
            var title=CreateText("HintTitle",group,"Hint!",new Vector2(0,50),new Vector2(415,50),39,
                new Color(1f,.81f,.38f,1f));
            title.raycastTarget=false;
            infoBodies[i]=CreateText("HintBody",group,string.Empty,new Vector2(0,-24),
                new Vector2(426,123),27,new Color(.99f,.95f,.85f,1f),false);
            var point=CreateImage("HintDirection",group,arrowSprite,
                i==InfoInvalid?new Vector2(0,-123):Vector2.zero,
                new Vector2(155,68));
            if(i==InfoInvalid)
                point.rectTransform.localRotation=Quaternion.Euler(0,0,-90f);
            else
                point.rectTransform.SetAsFirstSibling(); // shaft begins behind Hint frame
            point.raycastTarget=false;
            infoPanels[i]=group.gameObject;
            group.gameObject.SetActive(false);
        }
        for(int row=0;row<5;row++)
        {
            // Plaques are read-only informational targets. Their glow and
            // number are child graphics, and do not intercept pointer input.
            int id=row;
            BindInfoHover(turnPlaque[id],InfoTurns);
            BindInfoHover(prizePlaque[id],InfoPrize);
        }
        var submitGraphic=submitButton?.GetComponent<Image>();
        BindInfoHover(submitGraphic,InfoInvalid);
    }

    private void RefreshInfoPanels()
    {
        if(infoPanels[0]==null || controller==null)return;
        bool complete=controller.RoyalSolved || controller.RoyalGameOver;
        if(complete || showingPause)
        {
            if(complete)infoTurn=-1; // replay also works after a first-turn victory
            for(int i=0;i<infoPanels.Length;i++)infoPanels[i].SetActive(false);
            UpdateDismissShield(false);
            return;
        }
        bool justRestartedHint=controller.RoyalShowHint && !infoPreviousStartHint;
        infoPreviousStartHint=controller.RoyalShowHint;
        if(justRestartedHint)infoTurn=-1;
        int turn=controller.RoyalTurn;
        if(infoTurn!=turn)
        {
            infoTurn=turn;
            for(int i=0;i<infoExpanded.Length;i++)
            {
                infoExpanded[i]=false;
                infoPinned[i]=false;
                infoDismissed[i]=false;
            }
            lastInvalidGuess=string.Empty;
        }
        bool initialHint=controller.RoyalShowHint;
        bool tileClueActive=!string.IsNullOrEmpty(contextualClue);
        string guess=controller.RoyalGuess(turn);
        bool filled=guess.Length==controller.RoyalLength && guess.IndexOf(' ')<0;
        bool invalid=filled && !controller.RoyalCanSubmit;
        if(!invalid)
        {
            infoPinned[InfoInvalid]=false;
            lastInvalidGuess=string.Empty;
        }
        else if(lastInvalidGuess!=guess)
        {
            lastInvalidGuess=guess;
            infoDismissed[InfoInvalid]=false;
        }
        // Bookworm behavior: the hint persists until a click/tap.
        bool turnExpanded=infoExpanded[InfoTurns]||infoPinned[InfoTurns];
        bool prizeExpanded=infoExpanded[InfoPrize]||infoPinned[InfoPrize];
        bool invalidExpanded=infoExpanded[InfoInvalid]||infoPinned[InfoInvalid];
        bool showTurn=!initialHint && !tileClueActive && !infoDismissed[InfoTurns];
        bool showPrize=!initialHint && !tileClueActive && !infoDismissed[InfoPrize];
        bool showInvalid=invalid && !infoDismissed[InfoInvalid];
        infoPanels[InfoTurns].SetActive(showTurn);
        infoPanels[InfoPrize].SetActive(showPrize);
        infoPanels[InfoInvalid].SetActive(showInvalid);
        UpdateDismissShield(initialHint || showTurn || showPrize || showInvalid);
        float rowY=109f-turn*92f+110f;
        var turnRect=infoPanels[InfoTurns].GetComponent<RectTransform>();
        turnRect.anchoredPosition=new Vector2(-740,Mathf.Clamp(rowY-175f,-260f,130f));
        infoBodies[InfoTurns].fontSize=turnExpanded?25:27;
        infoBodies[InfoTurns].text=turnExpanded
            ? "Figure out the mystery word in <color=#E33B30>5 turns or less</color> to win!"
            : "<color=#E33B30>Turn "+(turn+1)+" of 5</color>\n<size=19>Hover for details. Click to continue.</size>";

        var prizeRect=infoPanels[InfoPrize].GetComponent<RectTransform>();
        // Give the Prize arrow a little space outside the board edge while
        // keeping the same approved, compact popup dimensions.
        prizeRect.anchoredPosition=new Vector2(825,Mathf.Clamp(rowY+140f,45f,330f));
        AlignPlaqueArrows(turn);
        infoBodies[InfoPrize].fontSize=prizeExpanded?25:27;
        infoBodies[InfoPrize].text=prizeExpanded
            ? "Solve the word now to earn <color=#FFDC70>"+Prizes[Mathf.Clamp(turn,0,4)].ToString("N0")+
              "</color> points! The prize gets smaller after each wrong guess."
            : "<color=#E33B30>Win on this turn, win this prize!</color>\n<size=19>Click anywhere to continue.</size>";

        var invalidRect=infoPanels[InfoInvalid].GetComponent<RectTransform>();
        invalidRect.anchoredPosition=new Vector2(-215,-297);
        infoBodies[InfoInvalid].fontSize=invalidExpanded?22:27;
        infoBodies[InfoInvalid].text=invalidExpanded
            ? "Oops! <color=#E33B30>Not a valid word.</color>\nSubmit stays disabled.\nClick a placed tile to <color=#E33B30>remove</color> it,\nthen keep trying."
            : "<color=#E33B30>Disabled: invalid word.</color>\n<size=19>Hover for details. Click to continue.</size>";
    }

    // Arrow.png is a diagonal, right/up arrow. Its pointed tip lies near
    // (0.91, 0.75) in image UV space, with its tail near (0.04, 0.15).
    // Set the arrow's pivot to that tip, then position that pivot directly
    // on the *current* Turn or Prize plaque. No fixed 90-degree rotations or
    // approximate row offsets: local transforms and scaling work in both
    // Screen Space Overlay and on 2400 x 1080 Android Game View.
    private void AlignPlaqueArrows(int currentTurn)
    {
        int row=Mathf.Clamp(currentTurn,0,4);
        AlignPlaqueArrow(InfoTurns,turnPlaque[row],true);
        AlignPlaqueArrow(InfoPrize,prizePlaque[row],false);
    }

    private void AlignPlaqueArrow(int index,Image targetImage,bool isTurns)
    {
        var popup=infoPanels[index]?.GetComponent<RectTransform>();
        var pointer=popup?.Find("HintDirection") as RectTransform;
        var target=targetImage?.rectTransform;
        if(popup==null || pointer==null || target==null)return;

        // Target inside the outer-facing edge, so the arrow points AT the
        // plaque without obscuring its turn number or prize amount.
        float edgeX=isTurns?target.rect.xMin+12f:target.rect.xMax-12f;
        Vector3 targetWorld=target.TransformPoint(
            new Vector3(edgeX,target.rect.center.y,0f));
        Vector3 localTip=popup.InverseTransformPoint(targetWorld);

        // Tail starts behind the nearest edge of the approved Hint card.
        // Only the shaft outside the panel remains visible.
        Vector2 tail=isTurns
            ?new Vector2(popup.rect.xMax-26f,popup.rect.yMax-40f)
            :new Vector2(popup.rect.xMin+128f,popup.rect.yMin+55f);
        Vector2 direction=(Vector2)localTip-tail;
        if(direction.sqrMagnitude<1f)return;

        const float width=155f, height=68f;
        const float tipX=.91f, tipY=.75f;
        Vector2 spriteVector=new Vector2((tipX-.04f)*width,
            (tipY-.15f)*height);
        float length=spriteVector.magnitude;
        float scaledLength=direction.magnitude/length;
        float intendedAngle=Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg;
        float spriteAngle=Mathf.Atan2(spriteVector.y,spriteVector.x)*Mathf.Rad2Deg;

        pointer.anchorMin=pointer.anchorMax=new Vector2(.5f,.5f);
        pointer.pivot=new Vector2(tipX,tipY);
        pointer.sizeDelta=new Vector2(width,height)*scaledLength;
        pointer.localRotation=Quaternion.Euler(0f,0f,intendedAngle-spriteAngle);
        pointer.anchoredPosition=(Vector2)localTip;
        // Behind frame & text, but visible along the gap to its target.
        if(pointer.GetSiblingIndex()!=0)pointer.SetAsFirstSibling();
    }

    private void EnsureDismissAnywhereButton()
    {
        if(!Application.isPlaying || layout==null || dismissAnywhereButton!=null)return;
        Image shield=CreateImage("ClickAnywhereToDismissHints",layout,null,
            Vector2.zero,new Vector2(2400,1080),true);
        shield.color=new Color(1f,1f,1f,.001f);
        dismissAnywhereButton=MakeButton(shield,DismissVisibleHints);
        dismissAnywhereButton.transition=Selectable.Transition.None;
        dismissAnywhereButton.gameObject.SetActive(false);
    }

    private void UpdateDismissShield(bool show)
    {
        if(dismissAnywhereButton==null)EnsureDismissAnywhereButton();
        if(dismissAnywhereButton==null)return;
        bool newlyActivated=show && !dismissAnywhereButton.gameObject.activeSelf;
        dismissAnywhereButton.gameObject.SetActive(show);
        if(!show)return;
        // Shield consumes the click before it reaches a tile or Submit.
        // The existing popup cards stay above it for hover explanations.
        dismissAnywhereButton.transform.SetAsLastSibling();
        for(int i=0;i<infoPanels.Length;i++)
            if(infoPanels[i]!=null && infoPanels[i].activeSelf)
                infoPanels[i].transform.SetAsLastSibling();
        if(newlyActivated)Canvas.ForceUpdateCanvases();
    }

    private void DismissVisibleHints()
    {
        if(showingPause || controller==null || !controller.RoyalReady)return;
        if(controller.RoyalShowHint)
        {
            controller.RoyalDismissHint();
            // One click clears the starting hint without immediately opening
            // another mandatory popup for that same turn.
            infoDismissed[InfoTurns]=true;
            infoDismissed[InfoPrize]=true;
        }
        for(int i=0;i<infoPanels.Length;i++)
            if(infoPanels[i]!=null && infoPanels[i].activeSelf)
            {
                infoDismissed[i]=true;
                infoExpanded[i]=false;
                infoPinned[i]=false;
            }
        HideContextClue();
        if(dismissAnywhereButton!=null)
            dismissAnywhereButton.gameObject.SetActive(false);
        lastState=string.Empty;
        RefreshGameplay(true);
        RefreshInfoPanels();
    }

    private void BindTileHover(Image holder,int row,int col)
    {
        if(holder==null || !Application.isPlaying)return;
        var trigger=holder.GetComponent<EventTrigger>();
        if(trigger==null)trigger=holder.gameObject.AddComponent<EventTrigger>();
        if(trigger.triggers==null)trigger.triggers=new List<EventTrigger.Entry>();
        trigger.triggers.Clear();
        var enter=new EventTrigger.Entry { eventID=EventTriggerType.PointerEnter };
        enter.callback.AddListener(_=>ShowContextClue(row,col));
        trigger.triggers.Add(enter);
        var exit=new EventTrigger.Entry { eventID=EventTriggerType.PointerExit };
        exit.callback.AddListener(_=>HideContextClue());
        trigger.triggers.Add(exit);
    }

    private void ShowContextClue(int row,int col)
    {
        if(controller==null || !controller.RoyalReady || showingPause)return;
        if(row>=controller.RoyalTurn)return;
        int clue=controller.RoyalCellState(row,col);
        string word=controller.RoyalGuess(row);
        if(col<0 || col>=word.Length)return;
        string meaning=clue==2?"Gold: correct letter and exact position."
            :clue==3?"Silver: the letter is in the word, but elsewhere."
            :clue==4?"Gray: this letter is not in the mystery word."
            :string.Empty;
        contextualClue=string.IsNullOrEmpty(meaning)?string.Empty:
            word[col]+": "+meaning;
        lastState=string.Empty;
    }

    private void HideContextClue()
    {
        if(string.IsNullOrEmpty(contextualClue))return;
        contextualClue=string.Empty;
        lastState=string.Empty;
    }

    private void HandleDesktopKeyboard()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        if(showingPause || (dismissAnywhereButton!=null && dismissAnywhereButton.gameObject.activeSelf)
           || controller==null || !controller.RoyalReady
           || controller.RoyalSolved || controller.RoyalGameOver || Application.isMobilePlatform)return;
        // Type letters and press Return/Backspace without requiring a mouse.
        foreach(char ch in Input.inputString)
        {
            if(ch=='\b')
            {
                string current=controller.RoyalGuess(controller.RoyalTurn);
                for(int i=current.Length-1;i>=0;i--)
                    if(controller.RoyalCellState(controller.RoyalTurn,i)==1)
                    { controller.RoyalRemoveAt(i); break; }
            }
            else if(ch=='\r' || ch=='\n')
            {
                if(controller.RoyalCanSubmit)controller.RoyalSubmit();
            }
            else if(ch>='a'&&ch<='z'||ch>='A'&&ch<='Z')
                controller.RoyalPress(char.ToUpperInvariant(ch));
        }
        if(Input.GetKeyDown(KeyCode.Escape))SetPauseVisible(true);
#endif
    }

    private void SetPauseVisible(bool show)
    {
        HideContextClue();
        showingPause=show;
        if(pauseGroup!=null)pauseGroup.SetActive(show);
    }

    private void Update()
    {
        if(!Application.isPlaying || controller==null || !controller.RoyalReady)return;
        HandleDesktopKeyboard();
        RefreshGameplay(false);
        float pulse=.5f+.5f*Mathf.Sin(Time.unscaledTime*3.5f);
        RefreshInfoPanels();
        for(int i=0;i<victoryStars.Length;i++)
        {
            var star=victoryStars[i];
            if(star==null || !star.gameObject.activeSelf)continue;
            float shimmer=.5f+.5f*Mathf.Sin(Time.unscaledTime*3f+i*.85f);
            star.color=new Color(1f,1f,1f,.73f+.27f*shimmer);
            star.rectTransform.localScale=Vector3.one*(.97f+.05f*shimmer);
        }
        for(int row=0;row<5;row++)
        {
            bool active=row==controller.RoyalTurn && !controller.RoyalSolved && !controller.RoyalGameOver;
            if(!active)continue;
            if(turnGlow[row]!=null) turnGlow[row].color=new Color(1f,1f,1f,.2f+.52f*pulse);
            if(prizeGlow[row]!=null) prizeGlow[row].color=new Color(1f,1f,1f,.2f+.52f*pulse);
            if(activeRowFrame[row]!=null)activeRowFrame[row].color=new Color(1f,.88f,.22f,.18f+.33f*pulse);
        }
        if(hintGlow!=null && hintGroup!=null && hintGroup.activeSelf)
            hintGlow.color=new Color(1f,.86f,.52f,.13f+.24f*pulse);
    }

    private string ComputeState()
    {
        StringBuilder sb=new StringBuilder(200);
        sb.Append(controller.RoyalTurn).Append(':').Append(controller.RoyalSolved).Append(':').Append(controller.RoyalGameOver);
        sb.Append(':').Append(controller.RoyalShowHint).Append(controller.RoyalHintLetter).Append(':').Append(controller.RoyalCanSubmit);
        sb.Append(':').Append(controller.RoyalPrizePoints).Append(':').Append(controller.RoyalWordsSolved);
        for(int row=0;row<5;row++)
        {
            sb.Append('|').Append(controller.RoyalGuess(row));
            for(int col=0;col<5;col++)sb.Append(controller.RoyalCellState(row,col));
        }
        for(char ch='A';ch<='Z';ch++)sb.Append(controller.RoyalKeyState(ch));
        sb.Append('|').Append(controller.RoyalFeedback).Append('|').Append(contextualClue);
        return sb.ToString();
    }

    private void RefreshGameplay(bool force)
    {
        if(controller==null || !controller.RoyalReady)return;
        // Dismiss stale hover guidance when a new round replaces the last one.
        if(previouslyRenderedTurn>0 && controller.RoyalTurn==0)
            contextualClue=string.Empty;
        previouslyRenderedTurn=controller.RoyalTurn;
        string state=ComputeState();
        if(!force && state==lastState)return;
        lastState=state;
        int turn=controller.RoyalTurn;
        bool complete=controller.RoyalSolved || controller.RoyalGameOver;
        for(int row=0;row<5;row++)
        {
            bool active=row==turn && !complete;
            if(activeRowFrame[row]!=null)activeRowFrame[row].gameObject.SetActive(active);
            if(turnPlaque[row]!=null)turnPlaque[row].sprite=active?turnActiveSprite:turnInactiveSprite;
            if(prizePlaque[row]!=null)prizePlaque[row].sprite=active?prizeActiveSprite:prizeInactiveSprite;
            if(turnGlow[row]!=null)turnGlow[row].gameObject.SetActive(active);
            if(prizeGlow[row]!=null)prizeGlow[row].gameObject.SetActive(active);
            if(turnText[row]!=null)turnText[row].color=active?NavyInk:Ivory;
            if(prizeText[row]!=null)
            {
                prizeText[row].color=active?NavyInk:Ivory;
                // Existing scenes were serialized with the previous 1,500
                // artwork label. Update TMP from the authoritative prize table.
                prizeText[row].text=Prizes[row].ToString("N0");
            }
            if(wrongMarks[row]!=null)
                wrongMarks[row].gameObject.SetActive(row<turn && (!controller.RoyalSolved || row<turn-1));

            string guess=controller.RoyalGuess(row);
            for(int col=0;col<5;col++)
            {
                Image tile=tileBase[row,col];
                Image letter=tileLetter[row,col];
                if(tile==null||letter==null)continue;
                int code=controller.RoyalCellState(row,col);
                bool visible=code>0 && col<guess.Length && guess[col]>='A' && guess[col]<='Z';
                tile.gameObject.SetActive(visible);
                letter.gameObject.SetActive(visible);
                if(!visible)continue;
                tile.sprite=code==2?goldSprite:code==3?silverSprite:code==4?graySprite:normalSprite; // 5 = frozen ivory
                letter.sprite=letters[guess[col]-'A'];
            }
        }

        for(int i=0;i<26;i++)
        {
            var baseImage=keyBase[i];
            if(baseImage==null)continue;
            int status=controller.RoyalKeyState((char)('A'+i));
            // Bookworm leaves usable keyboard letters ivory. Only eliminated
            // letters turn red; gold and silver belong on the guess board.
            baseImage.sprite=status==1?redSprite:normalSprite;
            if(keyButton[i]!=null)keyButton[i].interactable=!complete && !showingPause;
        }
        // The original hint card is used for the first letter and the
        // detailed per-tile clues. Other explanations use contextual popups.
        if(hintGroup!=null)hintGroup.SetActive(controller.RoyalShowHint ||
            !string.IsNullOrEmpty(contextualClue));
        if(tutorialArrow!=null)tutorialArrow.SetActive(controller.RoyalShowHint);
        if(hintTitle!=null)hintTitle.text=!string.IsNullOrEmpty(contextualClue)?"Tile clue":controller.RoyalShowHint?"Hint!":"Clues";
        if(hintMessage!=null)
        {
            if(!string.IsNullOrEmpty(contextualClue))
            {
                hintMessage.fontSize=27;
                hintMessage.text=contextualClue;
            }
            else if(controller.RoyalShowHint)
            {
                hintMessage.fontSize=33;
                hintMessage.text="<color=#B52B26>"+controller.RoyalHintLetter+"</color> is the first letter\nof the mystery word!\n<size=20>Click anywhere to continue.</size>";
            }
            else
            {
                hintMessage.fontSize=24;
                string clue=controller.RoyalFeedback;
                hintMessage.text=string.IsNullOrWhiteSpace(clue)
                    ? "Gold = right spot.\nSilver = right letter."
                    : clue.Replace(" | ","\n");
            }
        }
        var hintDismiss=hintGroup!=null?hintGroup.transform.Find("DismissHint"):null;
        if(hintDismiss!=null)hintDismiss.gameObject.SetActive(controller.RoyalShowHint);
        var hintClose=hintGroup!=null?hintGroup.transform.Find("HintCloseX"):null;
        if(hintClose!=null)hintClose.gameObject.SetActive(false);
        if(feedbackLabel!=null)
            feedbackLabel.text=string.Empty;
        if(scoreLabel!=null)
            scoreLabel.text="Score   "+controller.RoyalPrizePoints.ToString("N0");
        if(wordsLabel!=null)
            wordsLabel.text="Words   "+controller.RoyalWordsSolved.ToString("N0");
        if(submitButton!=null)submitButton.interactable=!showingPause && controller.RoyalCanSubmit;

        if(resultGroup!=null)
        {
            resultGroup.SetActive(complete);
            if(complete)
            {
                if(resultTitle!=null)resultTitle.text=controller.RoyalSolved?"VICTORY!":"GAME OVER";
                if(resultMessage!=null)resultMessage.text=controller.RoyalSolved
                    ? "Mystery word: "+controller.RoyalEndWord+"\nPrize: "+Prizes[Mathf.Clamp(turn-1,0,4)].ToString("N0")
                      +"   Score: "+controller.RoyalPrizePoints.ToString("N0")+"\nWords solved: "+controller.RoyalWordsSolved
                    : "The mystery word was "+controller.RoyalEndWord+"\nScore: "+controller.RoyalPrizePoints.ToString("N0")
                      +"   Words solved: "+controller.RoyalWordsSolved;
                for(int i=0;i<victoryStars.Length;i++)
                    if(victoryStars[i]!=null)victoryStars[i].gameObject.SetActive(controller.RoyalSolved);
            }
        }
    }
}
