using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Dedicated royal UI for Word Master only. All authoritative game rules remain
// in MinigamesSceneController/WordMasterSession.
// The UI is an overlay canvas; no changes to Spell It Out or Word Excavation.
[DisallowMultipleComponent]
public sealed class WordMasterRoyalUI : MonoBehaviour
{
    private const string ResourcesRoot = "WordMasterRoyal/";
    private static readonly int[] Prizes = { 10000, 5000, 2500, 1500, 500 };
    private static readonly Color NavyInk = new Color(0.10f, 0.11f, 0.16f, 1f);
    private static readonly Color Ivory = new Color(1f, .95f, .84f, 1f);

    [SerializeField] private MinigamesSceneController controller;

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

    private Sprite backgroundSprite, boardSprite, keyboardFrameSprite, headerSprite;
    private Sprite slotSprite, normalSprite, goldSprite, silverSprite, graySprite, redSprite;
    private Sprite hintSprite, hintGlowSprite, arrowSprite, submitSprite, submitHoverSprite, submitDisabledSprite, menuSprite;
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

    private static Sprite Load(string name) => Resources.Load<Sprite>(ResourcesRoot + name);

    private void LoadAssets()
    {
        if (assetsReady) return;
        backgroundSprite = Load("Background");
        boardSprite = Load("Board");
        keyboardFrameSprite = Load("KeyboardFrame");
        headerSprite = Load("Header");
        slotSprite = Load("Slot");
        normalSprite = Load("Normal");
        goldSprite = Load("Gold");
        silverSprite = Load("Silver");
        graySprite = Load("DarkGray");
        redSprite = Load("Red");
        hintSprite = Load("Hint");
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
    }

    private void Start()
    {
        if (Application.isPlaying)
        {
            showingPause = false;
            if (pauseGroup != null) pauseGroup.SetActive(false);
            lastState = string.Empty;
            RefreshGameplay(true);
        }
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

    private static TextMeshProUGUI CreateText(string name, Transform parent, string contents,
        Vector2 center, Vector2 size, float fontSize, Color color, bool bold = true)
    {
        RectTransform rect = CreateRect(name, parent, center, size);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
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
        CreateImage("ParchmentBoard", board, boardSprite, Vector2.zero, board.sizeDelta, false, true);
        CreateText("Instructions", board,
            "Guess the hidden 5-letter word.\nGold = right spot. Silver = right letter.",
            new Vector2(0, 228), new Vector2(860, 75), 28, NavyInk, false);
        CreateImage("ParchmentDivider", board, null, new Vector2(0, 171), new Vector2(790, 2)).color =
            new Color(.62f, .37f, .16f, .56f);
        CreateText("TurnsHeader", board, "Turns", new Vector2(-390, 147), new Vector2(155, 48), 31, NavyInk);
        CreateText("PrizeHeader", board, "Prize", new Vector2(356, 147), new Vector2(166, 48), 31, NavyInk);

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
                    MakeButton(slot, () => { if (!showingPause && controller != null && capturedRow == controller.RoyalTurn) controller.RoyalRemoveAt(capturedCol); });
            }

            Image prize = CreateImage("PrizePlaque", line, prizeInactiveSprite, new Vector2(356,0), new Vector2(152,80));
            prizePlaque[row] = prize;
            prizeGlow[row] = CreateImage("PrizeGlow", prize.transform, prizeGlowSprite, Vector2.zero, new Vector2(152,80));
            prizeText[row] = CreateText("PrizeNumber", prize.transform, Prizes[row].ToString("N0"),
                Vector2.zero, new Vector2(140,66), 27, NavyInk);
        }

        Image ribbon = CreateImage("TitleRibbon", layout, headerSprite, new Vector2(75, 463), new Vector2(848, 151));
        headerTitle = CreateText("WordMasterTitle", ribbon.transform, "WORD MASTER", new Vector2(0, 8),
            new Vector2(735,90), 58, new Color(.99f, .84f, .42f, 1f));

        RectTransform scoreboard = CreateRect("ScoreBoard",layout,new Vector2(-812,385),new Vector2(490,178));
        CreateImage("ScorePlaque",scoreboard,boardSprite,Vector2.zero,scoreboard.sizeDelta);
        scoreLabel = CreateText("ScoreLabel",scoreboard,"Score   0",new Vector2(0,35),new Vector2(410,55),34,NavyInk);
        wordsLabel = CreateText("WordsLabel",scoreboard,"Words   0",new Vector2(0,-30),new Vector2(410,55),34,NavyInk);
        CreateImage("ScoreDivider",scoreboard,null,new Vector2(0,1),new Vector2(310,2)).color =
            new Color(.57f,.38f,.18f,.64f);

        RectTransform hint = CreateRect("HintPanelGroup", layout, new Vector2(-812, 66), new Vector2(560,308));
        hintGroup = hint.gameObject;
        CreateImage("HintParchment", hint, hintSprite, Vector2.zero, hint.sizeDelta);
        hintGlow = CreateImage("HintOutsideGlow", hint, hintGlowSprite, Vector2.zero, hint.sizeDelta);
        hintTitle = CreateText("HintTitle", hint, "Hint!", new Vector2(0, 77), new Vector2(490,72), 56, NavyInk);
        CreateImage("HintDivider", hint, null, new Vector2(0, 27), new Vector2(355, 2)).color = new Color(.55f,.36f,.21f,.70f);
        hintMessage = CreateText("HintMessage", hint, "The first letter is revealed!", new Vector2(0,-33),
            new Vector2(455,110), 33, NavyInk, false);
        Image dismissImage = CreateImage("DismissHint",hint, null,new Vector2(235,119),new Vector2(45,35));
        dismissImage.color = new Color(1f, 1f, 1f, .01f);
        MakeButton(dismissImage, () => {
            if(controller!=null) controller.RoyalDismissHint();
        });
        CreateText("HintCloseX", hint, "×", new Vector2(235,119), new Vector2(38,34), 24, NavyInk);

        tutorialArrow = CreateImage("TutorialArrow",layout,arrowSprite,new Vector2(-358,160),new Vector2(364,173)).gameObject;

        RectTransform keyboard = CreateRect("Keyboard", layout, new Vector2(0,-319), new Vector2(1590,218));
        // Frame first; opaque navy inset above its old brown-tinted center, tiles above both.
        CreateImage("KeyboardNavyFrame", keyboard, keyboardFrameSprite, Vector2.zero, keyboard.sizeDelta, false, true);
        Image backing = CreateImage("KeyboardNavyBacking",keyboard,null,Vector2.zero,new Vector2(1536,196));
        backing.color=new Color(.016f,.026f,.065f,1f);
        Outline backingBorder=backing.gameObject.AddComponent<Outline>();
        backingBorder.effectColor=new Color(.77f,.53f,.19f,.98f);
        backingBorder.effectDistance=new Vector2(3f,-3f);
        for (int i = 0; i < 26; i++)
        {
            int captured = i;
            char ch = (char)('A'+i);
            float x = -630 + (i%13) * 105f;
            float y = i < 13 ? 49f : -49f;
            RectTransform key = CreateRect("Key_"+ch,keyboard,new Vector2(x,y),new Vector2(93,93));
            Image tile = CreateImage("Tile",key,normalSprite,Vector2.zero,key.sizeDelta,true);
            keyBase[i] = tile;
            Image letter = CreateImage("Letter",key,letters[i],Vector2.zero,key.sizeDelta);
            keyLetter[i] = letter;
            Button button = MakeButton(tile, () => { if (!showingPause && controller != null) controller.RoyalPress(ch); });
            keyButton[i] = button;
        }

        Image submitImage = CreateImage("SubmitButton", layout, submitSprite, new Vector2(-215,-476),new Vector2(354,102),true);
        submitButton = MakeButton(submitImage, () => { if(!showingPause && controller!=null) controller.RoyalSubmit(); });
        submitButton.transition = Selectable.Transition.SpriteSwap;
        SpriteState submitState = submitButton.spriteState;
        submitState.highlightedSprite = submitHoverSprite;
        submitState.selectedSprite = submitHoverSprite;
        submitState.pressedSprite = submitHoverSprite;
        submitState.disabledSprite = submitDisabledSprite;
        submitButton.spriteState = submitState;

        MakeButton(CreateImage("MenuButton",layout,menuSprite,new Vector2(215,-476),new Vector2(354,102),true),
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
        pauseGroup.SetActive(false);
        resultGroup.SetActive(false);
        SetEditPreview();
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
            for(int col=0;col<5;col++)
            {
                var cell=line.Find("Slot"+(col+1));
                tileBase[row,col]=cell?.Find("TileBase")?.GetComponent<Image>();
                tileLetter[row,col]=cell?.Find("LetterOverlay")?.GetComponent<Image>();
                var holder=cell?.Find("Holder")?.GetComponent<Image>();
                var b=holder!=null?holder.GetComponent<Button>():null;
                if(b!=null){b.onClick.RemoveAllListeners();int captured=col;b.onClick.AddListener(()=>{if(!showingPause&&controller!=null&&boundRow==controller.RoyalTurn)controller.RoyalRemoveAt(captured);});}
            }
        }
        var k=layout.Find("Keyboard");
        for(int i=0;i<26;i++)
        {
            char ch=(char)('A'+i);var key=k?.Find("Key_"+ch);
            keyBase[i]=key?.Find("Tile")?.GetComponent<Image>();
            keyLetter[i]=key?.Find("Letter")?.GetComponent<Image>();
            keyButton[i]=keyBase[i]?.GetComponent<Button>();
            if(keyButton[i]!=null){keyButton[i].onClick.RemoveAllListeners();char captured=ch;keyButton[i].onClick.AddListener(()=>{if(!showingPause&&controller!=null)controller.RoyalPress(captured);});}
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

    private void SetPauseVisible(bool show)
    {
        showingPause=show;
        if(pauseGroup!=null)pauseGroup.SetActive(show);
    }

    private void Update()
    {
        if(!Application.isPlaying || controller==null || !controller.RoyalReady)return;
        RefreshGameplay(false);
        float pulse=.5f+.5f*Mathf.Sin(Time.unscaledTime*3.5f);
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
        sb.Append('|').Append(controller.RoyalFeedback);
        return sb.ToString();
    }

    private void RefreshGameplay(bool force)
    {
        if(controller==null || !controller.RoyalReady)return;
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
            if(prizeText[row]!=null)prizeText[row].color=active?NavyInk:Ivory;

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
                tile.sprite=code==2?goldSprite:code==3?silverSprite:code==4?graySprite:normalSprite;
                letter.sprite=letters[guess[col]-'A'];
            }
        }

        for(int i=0;i<26;i++)
        {
            var baseImage=keyBase[i];
            if(baseImage==null)continue;
            int status=controller.RoyalKeyState((char)('A'+i));
            baseImage.sprite=status==3?goldSprite:status==2?silverSprite:status==1?redSprite:normalSprite;
            if(keyButton[i]!=null)keyButton[i].interactable=!complete && !showingPause;
        }
        if(hintGroup!=null)hintGroup.SetActive(true);
        if(tutorialArrow!=null)tutorialArrow.SetActive(controller.RoyalShowHint);
        if(hintTitle!=null)hintTitle.text=controller.RoyalShowHint?"Hint!":"Clues";
        if(hintMessage!=null)
        {
            if(controller.RoyalShowHint)
            {
                hintMessage.fontSize=33;
                hintMessage.text="<color=#B52B26>"+controller.RoyalHintLetter+"</color> is the first letter\nof the mystery word!";
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
        if(hintClose!=null)hintClose.gameObject.SetActive(controller.RoyalShowHint);
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
                    ? "Mystery word solved!\nPrize: "+Prizes[Mathf.Clamp(turn-1,0,4)].ToString("N0")
                    : "The mystery word was "+controller.RoyalEndWord+".";
            }
        }
    }
}
