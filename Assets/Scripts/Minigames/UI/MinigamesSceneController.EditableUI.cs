using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

// Maintained as a partial MonoBehaviour to preserve all existing Unity scene references.
public partial class MinigamesSceneController
{
    [ContextMenu("Rebuild Editable Minigames UI")]
    public void RebuildEditableUI()
    {
        if (dictionary == null)
            dictionary = FindFirstObjectByType<DictionaryManager>();

        ClearGeneratedUI();
        BuildUI();
        CacheExistingUIReferences();
        EnsureMinigameOverlayUI();
        WireButtonEvents();

        if (Application.isPlaying)
        {
            ShowModeSelection();
        }
        else
        {
            switch (startupScene)
            {
                case MinigameSceneStartup.WordExcavation:
                    ShowWordExcavationEditPreview();
                    break;
                case MinigameSceneStartup.WordMaster:
                default:
                    ShowWordMasterEditPreview();
                    break;
            }
        }
    }

    private void PrepareUI()
    {
        if (rebuildUIOnStart)
        {
            RebuildEditableUI();
            return;
        }

        if (useEditableSceneUI)
            CacheExistingUIReferences();

        if (root == null)
        {
            ClearGeneratedUI();
            BuildUI();
            CacheExistingUIReferences();
        }

        EnsureMinigameOverlayUI();
        WireButtonEvents();
    }

    private void ClearGeneratedUI()
    {
        RectTransform existingRoot = FindRect(transform, "MinigamesUI");
        if (existingRoot != null)
            DestroySafe(existingRoot.gameObject);

        root = null;
        selectionScreen = null;
        playScreen = null;
        titleText = null;
        promptText = null;
        feedbackText = null;
        scoreText = null;
        timerText = null;
        foundWordsText = null;
        answerInput = null;
        optionsRoot = null;
        tileInstructionText = null;
        currentWordTilesRoot = null;
        sourceWordTilesRoot = null;
        wordMasterRowsRoot = null;
        wordMasterHintPanel = null;
        wordMasterStatsText = null;
        wordExcavationTimerFill = null;
        wordExcavationTimerText = null;
        wordExcavationFoundWordsText = null;
        pauseOverlay = null;
        resultOverlay = null;
        resultTitleText = null;
        resultScoreText = null;
        resultWordsText = null;
        resultContinueText = null;
        optionsButton = null;
        quitMiniGameButton = null;
        returnToGameButton = null;
        resultContinueButton = null;
    }

    private void CacheExistingUIReferences()
    {
        root = FindRect(transform, "MinigamesUI");
        if (root == null)
            return;

        selectionScreen = FindRect(root, "MinigameSelectionScreen");
        playScreen = FindRect(root, "MinigamePlayScreen");

        titleText = FindText(playScreen, "Title");
        promptText = FindText(playScreen, "Prompt");
        feedbackText = FindText(playScreen, "Feedback");
        scoreText = FindText(playScreen, "Score");
        timerText = FindText(playScreen, "Timer");
        foundWordsText = FindText(playScreen, "FoundWords");
        answerInput = FindComponent<TMP_InputField>(playScreen, "AnswerInput");
        optionsRoot = FindRect(playScreen, "Options");
        tileInstructionText = FindText(playScreen, "TileInstruction");
        currentWordTilesRoot = FindRect(playScreen, "CurrentWordTiles");
        sourceWordTilesRoot = FindRect(playScreen, "SourceWordTiles");
        wordExcavationTimerFill = FindRect(optionsRoot, "ExcavationTimeFill");
        wordExcavationTimerText = FindText(optionsRoot, "ExcavationTimeText");
        wordExcavationFoundWordsText = FindText(optionsRoot, "ExcavationFoundWordsText");

        pauseOverlay = FindRect(playScreen, "PauseOverlay");
        resultOverlay = FindRect(playScreen, "ResultOverlay");
        resultTitleText = FindText(resultOverlay, "ResultTitleText");
        resultScoreText = FindText(resultOverlay, "ResultScoreText");
        resultWordsText = FindText(resultOverlay, "ResultWordsText");
        resultContinueText = FindText(resultOverlay, "ResultContinueText");
        optionsButton = FindButton(pauseOverlay, "OptionsButton");
        quitMiniGameButton = FindButton(pauseOverlay, "QuitMiniGameButton");
        returnToGameButton = FindButton(pauseOverlay, "ReturnToGameButton");
        resultContinueButton = FindButton(resultOverlay, "ResultContinueButton");
        if (resultContinueButton == null && resultOverlay != null)
            resultContinueButton = resultOverlay.GetComponent<Button>();

        wordMasterButton = FindButton(selectionScreen, "WordMasterButton");
        spellItOutButton = FindButton(selectionScreen, "SpellItOutButton");
        wordExcavationButton = FindButton(selectionScreen, "WordExcavationButton");
        selectionBackButton = FindButton(selectionScreen, "BackButton");

        newRoundButton = FindButton(playScreen, "NewRoundButton");
        clearButton = FindButton(playScreen, "ClearButton");
        submitButton = FindButton(playScreen, "SubmitButton");
        modeSelectButton = FindButton(playScreen, "ModeSelectButton");
        playBackButton = FindButton(playScreen, "BackButton");
    }

    private void WireButtonEvents()
    {
        WireButton(wordMasterButton, OpenWordMasterScene);
        WireButton(spellItOutButton, OpenSpellItOutScene);
        WireButton(wordExcavationButton, OpenWordExcavationScene);
        WireButton(selectionBackButton, ReturnToMainMenu);

        WireButton(newRoundButton, StartNewRound);
        WireButton(clearButton, ClearTileSelection);
        WireButton(submitButton, SubmitTypedAnswer);
        WireButton(modeSelectButton, ReturnToModeSelect);
        WireButton(playBackButton, OpenPauseMenu);
        WireButton(optionsButton, ShowOptionsPlaceholder);
        WireButton(quitMiniGameButton, ReturnToModeSelect);
        WireButton(returnToGameButton, ClosePauseMenu);
        WireButton(resultContinueButton, ReturnToModeSelect);
    }

    private void WireButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private void EnsureMinigameOverlayUI()
    {
        if (playScreen == null)
            return;

        bool createdOverlay = false;

        if (pauseOverlay == null)
        {
            CreatePauseOverlay(playScreen);
            createdOverlay = true;
        }

        if (resultOverlay == null)
        {
            CreateResultOverlay(playScreen);
            createdOverlay = true;
        }

        if (createdOverlay)
            CacheExistingUIReferences();

        if (pauseOverlay != null)
            pauseOverlay.SetAsLastSibling();

        if (resultOverlay != null)
            resultOverlay.SetAsLastSibling();
    }

    private void HideMinigameOverlays()
    {
        minigamePaused = false;
        resultOverlayShowing = false;

        if (pauseOverlay != null)
            pauseOverlay.gameObject.SetActive(false);

        if (resultOverlay != null)
            resultOverlay.gameObject.SetActive(false);
    }

    public void OpenPauseMenu()
    {
        if (resultOverlayShowing)
            return;

        EnsureMinigameOverlayUI();

        if (pauseOverlay == null)
            return;

        minigamePaused = true;
        pauseOverlay.gameObject.SetActive(true);
        pauseOverlay.SetAsLastSibling();
    }

    public void ClosePauseMenu()
    {
        minigamePaused = false;

        if (pauseOverlay != null)
            pauseOverlay.gameObject.SetActive(false);
    }

    private void ShowOptionsPlaceholder()
    {
        SetFeedback("Options coming soon.", lightTextColor);
    }

    private void ShowMinigameResultOverlay(string title)
    {
        EnsureMinigameOverlayUI();

        minigamePaused = true;
        resultOverlayShowing = true;

        if (pauseOverlay != null)
            pauseOverlay.gameObject.SetActive(false);

        if (resultTitleText != null)
        {
            resultTitleText.text = title;
            ApplyResultTitleTextStyle();
        }

        if (resultScoreText != null)
        {
            if (currentMode == MinigameMode.WordMaster)
                resultScoreText.text = "Secret word";
            else if (currentMode == MinigameMode.SpellItOut)
                resultScoreText.text = $"Score: {score}/{Mathf.Max(1, spellItOutTotalWords)}";
            else
                resultScoreText.text = $"Score: {score}";
        }

        if (resultWordsText != null)
        {
            if (currentMode == MinigameMode.WordMaster)
                resultWordsText.text = string.IsNullOrWhiteSpace(wordMasterTargetWord) ? "Unknown" : wordMasterTargetWord;
            else if (currentMode == MinigameMode.SpellItOut)
                resultWordsText.text = BuildSpellItOutResultWordsText();
            else if (currentMode == MinigameMode.WordExcavation)
                resultWordsText.text = $"Words: {foundExcavationWordsInOrder.Count}";
            else
                resultWordsText.text = $"Rounds: {roundsPlayed}";

            ApplyResultWordsTextStyle();
        }

        if (resultContinueText != null)
            resultContinueText.text = "Click anywhere to continue";

        if (resultOverlay != null)
        {
            resultOverlay.gameObject.SetActive(true);
            resultOverlay.SetAsLastSibling();
        }
    }

    private void ApplyResultWordsTextStyle()
    {
        if (resultWordsText == null)
            return;

        resultWordsText.textWrappingMode = TextWrappingModes.Normal;

        if (currentMode == MinigameMode.SpellItOut)
        {
            resultWordsText.fontSize = 18f;
            resultWordsText.lineSpacing = -8f;
            resultWordsText.overflowMode = TextOverflowModes.Overflow;
            resultWordsText.alignment = TextAlignmentOptions.Center;
            SetPreferredHeight(resultWordsText, 190f);

            RectTransform panel = resultWordsText.transform.parent as RectTransform;
            if (panel != null)
            {
                panel.sizeDelta = new Vector2(Mathf.Max(panel.sizeDelta.x, 620f), Mathf.Max(panel.sizeDelta.y, 470f));

                VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
                if (layout != null)
                {
                    layout.padding = new RectOffset(54, 54, 36, 30);
                    layout.spacing = 8f;
                    layout.childControlWidth = true;
                    layout.childControlHeight = true;
                    layout.childForceExpandWidth = true;
                    layout.childForceExpandHeight = false;
                }
            }
        }
        else
        {
            resultWordsText.fontSize = 30f;
            resultWordsText.lineSpacing = 0f;
            resultWordsText.overflowMode = TextOverflowModes.Ellipsis;
            resultWordsText.alignment = TextAlignmentOptions.Center;
            SetPreferredHeight(resultWordsText, 44f);
        }
    }

    private void ApplyResultTitleTextStyle()
    {
        if (resultTitleText == null)
            return;

        resultTitleText.alignment = TextAlignmentOptions.Center;
        resultTitleText.textWrappingMode = TextWrappingModes.NoWrap;
        resultTitleText.overflowMode = TextOverflowModes.Ellipsis;
        resultTitleText.enableAutoSizing = true;
        resultTitleText.fontSizeMin = 26f;
        resultTitleText.fontSizeMax = currentMode == MinigameMode.SpellItOut ? 42f : 48f;
        SetPreferredHeight(resultTitleText, 66f);
    }

    private void AddFixedLayout(RectTransform rect, float width, float height)
    {
        LayoutElement layout = rect.gameObject.GetComponent<LayoutElement>();
        if (layout == null)
            layout = rect.gameObject.AddComponent<LayoutElement>();

        layout.minWidth = width;
        layout.preferredWidth = width;
        layout.minHeight = height;
        layout.preferredHeight = height;
    }

    private void BuildUI()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = gameObject.AddComponent<Canvas>();

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = gameObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        root = CreateRect("MinigamesUI", transform);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        Image background = root.gameObject.AddComponent<Image>();
        background.color = backgroundColor;

        CreateSelectionScreen(root);
        CreatePlayScreen(root);
    }

    private void CreateSelectionScreen(Transform parent)
    {
        selectionScreen = CreateRect("MinigameSelectionScreen", parent);
        selectionScreen.anchorMin = Vector2.zero;
        selectionScreen.anchorMax = Vector2.one;
        selectionScreen.offsetMin = Vector2.zero;
        selectionScreen.offsetMax = Vector2.zero;

        RectTransform panel = CreatePanel(selectionScreen, "SelectionBoard", panelColor, new Vector2(920f, 790f));
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup panelLayout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        panelLayout.padding = new RectOffset(54, 54, 42, 42);
        panelLayout.spacing = 16f;
        panelLayout.childControlWidth = true;
        panelLayout.childControlHeight = true;
        panelLayout.childForceExpandWidth = true;
        panelLayout.childForceExpandHeight = false;

        TextMeshProUGUI heading = CreateText(panel, "Title", "SPELLSTRIKE\nMINI-GAMES", 50f, FontStyles.Bold, textColor);
        heading.alignment = TextAlignmentOptions.Center;
        heading.characterSpacing = 4f;
        heading.lineSpacing = -20f;
        heading.gameObject.AddComponent<LayoutElement>().preferredHeight = 138f;

        CreateMinigameOption(panel, "Word Master", "Guess the hidden word from gold and silver clues.", new Color(0.62f, 0.85f, 0.16f, 0.95f), OpenWordMasterScene);
        CreateMinigameOption(panel, "Spell It Out", "Build the word from jumbled tiles.", new Color(0.95f, 0.83f, 0.20f, 0.95f), OpenSpellItOutScene);
        CreateMinigameOption(panel, "Word Excavation", "Find smaller words inside a longer word.", new Color(0.98f, 0.49f, 0.12f, 0.95f), OpenWordExcavationScene);

        RectTransform spacer = CreateRect("Spacer", panel);
        spacer.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        CreateButton(panel, "BackButton", "Main Menu", buttonColor, new Vector2(360f, 58f), ReturnToMainMenu);
    }

    private void CreateMinigameOption(Transform parent, string optionTitle, string description, Color color, UnityEngine.Events.UnityAction action)
    {
        Button button = CreateButton(parent, optionTitle.Replace(" ", string.Empty) + "Button", string.Empty, color, new Vector2(0f, 104f), action);
        LayoutElement layout = button.GetComponent<LayoutElement>();
        if (layout != null)
        {
            layout.minWidth = 0f;
            layout.preferredWidth = 0f;
            layout.minHeight = 104f;
            layout.preferredHeight = 104f;
        }

        RectTransform rect = button.GetComponent<RectTransform>();

        TextMeshProUGUI title = CreateText(rect, "Title", optionTitle, 36f, FontStyles.Bold, textColor);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.43f);
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = new Vector2(24f, 0f);
        titleRect.offsetMax = new Vector2(-24f, -6f);
        title.alignment = TextAlignmentOptions.Center;

        TextMeshProUGUI body = CreateText(rect, "Description", description, 22f, FontStyles.Bold, textColor);
        RectTransform bodyRect = body.GetComponent<RectTransform>();
        bodyRect.anchorMin = Vector2.zero;
        bodyRect.anchorMax = new Vector2(1f, 0.48f);
        bodyRect.offsetMin = new Vector2(24f, 8f);
        bodyRect.offsetMax = new Vector2(-24f, 2f);
        body.alignment = TextAlignmentOptions.Center;
        body.textWrappingMode = TextWrappingModes.Normal;
    }

    private void CreatePlayScreen(Transform parent)
    {
        playScreen = CreateRect("MinigamePlayScreen", parent);
        playScreen.anchorMin = Vector2.zero;
        playScreen.anchorMax = Vector2.one;
        playScreen.offsetMin = Vector2.zero;
        playScreen.offsetMax = Vector2.zero;

        RectTransform panel = CreatePanel(playScreen, "BookPanel", panelColor, new Vector2(1420f, 790f));
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup panelLayout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        panelLayout.padding = new RectOffset(52, 52, 40, 40);
        panelLayout.spacing = 18f;
        panelLayout.childControlWidth = true;
        panelLayout.childControlHeight = true;
        panelLayout.childForceExpandWidth = true;
        panelLayout.childForceExpandHeight = false;

        titleText = CreateText(panel, "Title", "SPELLSTRIKE MINIGAMES", 42f, FontStyles.Bold, textColor);
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.characterSpacing = 5f;
        titleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 58f;

        RectTransform playArea = CreatePanel(panel, "PlayArea", cardColor, new Vector2(0f, 560f));
        LayoutElement playAreaLayout = playArea.gameObject.AddComponent<LayoutElement>();
        playAreaLayout.minHeight = 560f;
        playAreaLayout.preferredHeight = 560f;

        VerticalLayoutGroup playLayout = playArea.gameObject.AddComponent<VerticalLayoutGroup>();
        playLayout.padding = new RectOffset(34, 34, 28, 28);
        playLayout.spacing = 74.79f;
        playLayout.childControlWidth = true;
        playLayout.childControlHeight = true;
        playLayout.childForceExpandWidth = true;
        playLayout.childForceExpandHeight = false;

        promptText = CreateText(playArea, "Prompt", string.Empty, 32f, FontStyles.Bold, lightTextColor);
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.textWrappingMode = TextWrappingModes.Normal;
        promptText.gameObject.AddComponent<LayoutElement>().preferredHeight = 150f;

        timerText = CreateText(playArea, "Timer", string.Empty, 22f, FontStyles.Bold, lightTextColor);
        timerText.alignment = TextAlignmentOptions.Center;
        timerText.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

        optionsRoot = CreateRect("Options", playArea);
        LayoutElement optionsLayout = optionsRoot.gameObject.AddComponent<LayoutElement>();
        optionsLayout.minHeight = 190f;
        optionsLayout.preferredHeight = 190f;

        VerticalLayoutGroup optionsGroup = optionsRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        optionsGroup.spacing = 10f;
        optionsGroup.childControlWidth = true;
        optionsGroup.childControlHeight = true;
        optionsGroup.childForceExpandWidth = true;
        optionsGroup.childForceExpandHeight = false;

        tileInstructionText = CreateText(playArea, "TileInstruction", string.Empty, 21f, FontStyles.Bold, lightTextColor);
        tileInstructionText.alignment = TextAlignmentOptions.Center;
        tileInstructionText.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;

        currentWordTilesRoot = CreateTileRow(playArea, "CurrentWordTiles", 62f);
        sourceWordTilesRoot = CreateTileRow(playArea, "SourceWordTiles", 72f);

        answerInput = CreateInputField(playArea, "AnswerInput");

        foundWordsText = CreateText(playArea, "FoundWords", string.Empty, 20f, FontStyles.Normal, lightTextColor);
        foundWordsText.textWrappingMode = TextWrappingModes.Normal;
        foundWordsText.gameObject.AddComponent<LayoutElement>().preferredHeight = 66f;

        feedbackText = CreateText(playArea, "Feedback", string.Empty, 22f, FontStyles.Bold, lightTextColor);
        feedbackText.alignment = TextAlignmentOptions.Center;
        feedbackText.textWrappingMode = TextWrappingModes.Normal;
        feedbackText.gameObject.AddComponent<LayoutElement>().preferredHeight = 58f;

        scoreText = CreateText(playArea, "Score", string.Empty, 20f, FontStyles.Bold, lightTextColor);
        scoreText.alignment = TextAlignmentOptions.Center;
        scoreText.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

        CreateFooter(panel);
        CreatePauseOverlay(playScreen);
        CreateResultOverlay(playScreen);
    }

    private void CreatePauseOverlay(Transform parent)
    {
        pauseOverlay = CreatePanel(parent, "PauseOverlay", new Color(0f, 0f, 0f, 0.66f), Vector2.zero);
        StretchToFillParent(pauseOverlay);

        RectTransform panel = CreatePanel(pauseOverlay, "PauseMenuPanel", new Color(0.84f, 0.70f, 0.38f, 0.98f), new Vector2(620f, 390f));
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = Vector2.zero;

        Outline outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.18f, 0.10f, 0.03f, 0.95f);
        outline.effectDistance = new Vector2(5f, -5f);

        VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(76, 76, 44, 44);
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        TextMeshProUGUI title = CreateText(panel, "PauseMenuTitleText", "Menu", 54f, FontStyles.Bold, textColor);
        title.alignment = TextAlignmentOptions.Center;
        title.gameObject.AddComponent<LayoutElement>().preferredHeight = 82f;

        optionsButton = CreateButton(panel, "OptionsButton", "Options", selectedButtonColor, new Vector2(430f, 58f), ShowOptionsPlaceholder);
        quitMiniGameButton = CreateButton(panel, "QuitMiniGameButton", "Quit Mini-game", selectedButtonColor, new Vector2(430f, 58f), ReturnToModeSelect);
        returnToGameButton = CreateButton(panel, "ReturnToGameButton", "Return To Game", selectedButtonColor, new Vector2(430f, 58f), ClosePauseMenu);

        pauseOverlay.gameObject.SetActive(false);
    }

    private void CreateResultOverlay(Transform parent)
    {
        resultOverlay = CreatePanel(parent, "ResultOverlay", new Color(0f, 0f, 0f, 0.72f), Vector2.zero);
        StretchToFillParent(resultOverlay);

        RectTransform clickCatcher = CreatePanel(resultOverlay, "ResultContinueButton", new Color(0f, 0f, 0f, 0f), Vector2.zero);
        StretchToFillParent(clickCatcher);
        resultContinueButton = clickCatcher.gameObject.AddComponent<Button>();
        resultContinueButton.targetGraphic = clickCatcher.GetComponent<Image>();
        resultContinueButton.onClick.AddListener(ReturnToModeSelect);

        RectTransform panel = CreatePanel(resultOverlay, "ResultPanel", new Color(0.84f, 0.70f, 0.38f, 0.98f), new Vector2(620f, 470f));
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = Vector2.zero;

        Image panelImage = panel.GetComponent<Image>();
        if (panelImage != null)
            panelImage.raycastTarget = false;

        Outline outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.18f, 0.10f, 0.03f, 0.95f);
        outline.effectDistance = new Vector2(5f, -5f);

        VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(54, 54, 38, 34);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        resultTitleText = CreateText(panel, "ResultTitleText", "Time Up", 48f, FontStyles.Bold, textColor);
        resultTitleText.alignment = TextAlignmentOptions.Center;
        resultTitleText.enableAutoSizing = true;
        resultTitleText.fontSizeMin = 26f;
        resultTitleText.fontSizeMax = 48f;
        resultTitleText.overflowMode = TextOverflowModes.Ellipsis;
        resultTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 66f;

        resultScoreText = CreateText(panel, "ResultScoreText", "Score: 0", 30f, FontStyles.Bold, textColor);
        resultScoreText.alignment = TextAlignmentOptions.Center;
        resultScoreText.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;

        resultWordsText = CreateText(panel, "ResultWordsText", "Words: 0", 18f, FontStyles.Bold, textColor);
        resultWordsText.alignment = TextAlignmentOptions.Center;
        resultWordsText.textWrappingMode = TextWrappingModes.Normal;
        resultWordsText.overflowMode = TextOverflowModes.Overflow;
        resultWordsText.lineSpacing = -8f;
        resultWordsText.gameObject.AddComponent<LayoutElement>().preferredHeight = 190f;

        resultContinueText = CreateText(panel, "ResultContinueText", "Click anywhere to continue", 22f, FontStyles.Bold, lightTextColor);
        resultContinueText.alignment = TextAlignmentOptions.Center;
        resultContinueText.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;

        resultOverlay.gameObject.SetActive(false);
    }

    private void CreateFooter(Transform parent)
    {
        RectTransform row = CreateRect("Footer", parent);
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;

        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        CreateButton(row, "NewRoundButton", "New Round", buttonColor, new Vector2(230f, 56f), StartNewRound);
        clearButton = CreateButton(row, "ClearButton", "Clear", buttonColor, new Vector2(170f, 56f), ClearTileSelection);
        CreateButton(row, "SubmitButton", "Submit", selectedButtonColor, new Vector2(220f, 56f), SubmitTypedAnswer);
        CreateButton(row, "ModeSelectButton", "Mode Select", buttonColor, new Vector2(230f, 56f), ReturnToModeSelect);
        CreateButton(row, "BackButton", "Menu", buttonColor, new Vector2(210f, 56f), OpenPauseMenu);
    }

    private TMP_InputField CreateInputField(Transform parent, string objectName)
    {
        RectTransform fieldRect = CreatePanel(parent, objectName, new Color(0.92f, 0.78f, 0.46f, 0.95f), new Vector2(0f, 66f));
        fieldRect.gameObject.AddComponent<LayoutElement>().preferredHeight = 66f;

        TMP_InputField input = fieldRect.gameObject.AddComponent<TMP_InputField>();
        TextMeshProUGUI text = CreateText(fieldRect, "Text", string.Empty, 28f, FontStyles.Bold, textColor);
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(22f, 8f);
        textRect.offsetMax = new Vector2(-22f, -8f);

        TextMeshProUGUI placeholder = CreateText(fieldRect, "Placeholder", "Type answer...", 26f, FontStyles.Italic, new Color(textColor.r, textColor.g, textColor.b, 0.55f));
        RectTransform placeholderRect = placeholder.GetComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = new Vector2(22f, 8f);
        placeholderRect.offsetMax = new Vector2(-22f, -8f);

        input.textComponent = text;
        input.placeholder = placeholder;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.characterValidation = TMP_InputField.CharacterValidation.Alphanumeric;
        input.onSubmit.AddListener(_ => SubmitTypedAnswer());
        return input;
    }

    private RectTransform CreateTileRow(Transform parent, string objectName, float preferredHeight)
    {
        RectTransform row = CreatePanel(parent, objectName, new Color(0.10f, 0.055f, 0.025f, 0.72f), new Vector2(0f, preferredHeight));
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = preferredHeight;

        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 8, 8);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        return row;
    }

    private void SetTilePickerVisible(bool visible)
    {
        if (tileInstructionText != null)
            tileInstructionText.gameObject.SetActive(visible);

        if (currentWordTilesRoot != null)
            currentWordTilesRoot.gameObject.SetActive(visible);

        if (sourceWordTilesRoot != null)
            sourceWordTilesRoot.gameObject.SetActive(visible);
    }

    private void ApplyDefaultPlayLayout()
    {
        ApplyPlayableFooterButtons();

        if (promptText != null)
            promptText.fontSize = 32f;

        if (timerText != null)
            timerText.fontSize = 22f;

        SetPlayAreaSpacing(16f);
        SetOptionRowSpacing(10f);
        SetSourceTileRootLayout(false);
        SetPreferredHeight(promptText, 150f);
        SetPreferredHeight(timerText, 34f);
        SetPreferredHeight(optionsRoot, 190f);
        SetPreferredHeight(tileInstructionText, 28f);
        SetPreferredHeight(currentWordTilesRoot, 62f);
        SetPreferredHeight(sourceWordTilesRoot, 72f);
        SetPreferredHeight(feedbackText, 58f);
        SetPreferredHeight(scoreText, 34f);
    }

    private void ApplySpellItOutPlayLayout()
    {
        ApplyDefaultPlayLayout();

        if (promptText != null)
            promptText.fontSize = 28f;

        if (timerText != null)
        {
            timerText.fontSize = 28f;
            timerText.color = new Color(1f, 0.92f, 0.42f, 1f);
            timerText.alignment = TextAlignmentOptions.Center;
        }

        SetPreferredHeight(promptText, 130f);
        SetPreferredHeight(timerText, 44f);
        SetPreferredHeight(feedbackText, 44f);
        SetPreferredHeight(scoreText, 42f);
    }

    private void ApplyWordMasterPlayLayout()
    {
        ApplyPlayableFooterButtons();

        if (promptText != null)
            promptText.fontSize = 22f;

        SetPlayAreaSpacing(74.79f);
        SetOptionRowSpacing(6f);
        SetSourceTileRootLayout(true);
        SetPreferredHeight(promptText, 30f);
        SetPreferredHeight(optionsRoot, 328f);
        SetPreferredHeight(tileInstructionText, 0f);
        SetPreferredHeight(currentWordTilesRoot, 0f);
        SetPreferredHeight(sourceWordTilesRoot, 124f);
        SetPreferredHeight(feedbackText, 0f);
        SetPreferredHeight(scoreText, 0f);
    }

    private void ApplyWordExcavationPlayLayout()
    {
        ApplyPlayableFooterButtons();

        if (promptText != null)
            promptText.fontSize = 20f;

        SetLayoutHeights(promptText != null ? promptText.transform.parent : null, 865.6f, 560f);
        SetPlayAreaSpacing(10f);
        SetOptionRowSpacing(6f);
        SetSourceTileRootLayout(false);
        SetPreferredHeight(promptText, 28f);
        SetLayoutHeights(optionsRoot, 598.5f, 330f);
        SetPreferredHeight(tileInstructionText, 24f);
        SetPreferredHeight(currentWordTilesRoot, 58f);
        SetPreferredHeight(sourceWordTilesRoot, 72f);
        SetPreferredHeight(feedbackText, 36f);
        SetPreferredHeight(scoreText, 0f);
    }

    private void SetPlayAreaSpacing(float spacing)
    {
        if (promptText == null || promptText.transform.parent == null)
            return;

        VerticalLayoutGroup layout = promptText.transform.parent.GetComponent<VerticalLayoutGroup>();
        if (layout != null)
            layout.spacing = spacing;
    }

    private void SetOptionRowSpacing(float spacing)
    {
        if (optionsRoot == null)
            return;

        VerticalLayoutGroup layout = optionsRoot.GetComponent<VerticalLayoutGroup>();
        if (layout != null)
            layout.spacing = spacing;
    }

    private void SetPreferredHeight(Component component, float height)
    {
        SetLayoutHeights(component, height, height);
    }

    private void SetLayoutHeights(Component component, float minHeight, float preferredHeight)
    {
        if (component == null)
            return;

        LayoutElement layout = component.GetComponent<LayoutElement>();
        if (layout == null)
            return;

        layout.minHeight = minHeight;
        layout.preferredHeight = preferredHeight;
    }

    private void SetObjectActive(Component component, bool active)
    {
        if (component != null)
            component.gameObject.SetActive(active);
    }

    private void ApplyPlayableFooterButtons()
    {
        SetButtonVisible(newRoundButton, false);
        SetButtonVisible(clearButton, false);
        SetButtonVisible(modeSelectButton, false);
        SetButtonVisible(submitButton, true);
        SetButtonVisible(playBackButton, true);
        SetButtonLabel(playBackButton, "Menu");
    }

    private void SetButtonVisible(Button button, bool visible)
    {
        if (button != null)
            button.gameObject.SetActive(visible);
    }

    private void SetButtonLabel(Button button, string label)
    {
        if (button == null)
            return;

        TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text != null)
            text.text = label;
    }

    private void SetSourceTileRootLayout(bool wordMasterLayout)
    {
        if (sourceWordTilesRoot == null)
            return;

        HorizontalLayoutGroup horizontal = sourceWordTilesRoot.GetComponent<HorizontalLayoutGroup>();

        if (wordMasterLayout)
        {
            if (horizontal != null)
                horizontal.enabled = false;
        }
        else
        {
            if (horizontal != null)
            {
                horizontal.enabled = true;
                horizontal.padding = new RectOffset(10, 10, 8, 8);
                horizontal.spacing = 8f;
                horizontal.childAlignment = TextAnchor.MiddleCenter;
            }
        }
    }

    private void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            GameObject child = parent.GetChild(i).gameObject;
            child.SetActive(false);
            DestroySafe(child);
        }
    }

    private void CreateOptionButton(string label, UnityEngine.Events.UnityAction action)
    {
        Button button = CreateButton(optionsRoot, "OptionButton", label, buttonColor, new Vector2(0f, 42f), action);
        button.GetComponent<LayoutElement>().preferredHeight = 42f;
        optionButtons.Add(button);
    }

    private Button CreateButton(Transform parent, string objectName, string label, Color color, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        RectTransform rect = CreatePanel(parent, objectName, color, size);
        LayoutElement layoutElement = rect.gameObject.AddComponent<LayoutElement>();
        layoutElement.minWidth = size.x;
        layoutElement.preferredWidth = size.x;
        layoutElement.minHeight = size.y;
        layoutElement.preferredHeight = size.y;

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        if (action != null)
            button.onClick.AddListener(action);

        TextMeshProUGUI text = CreateText(rect, "Text", label, 22f, FontStyles.Bold, lightTextColor);
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(16f, 5f);
        textRect.offsetMax = new Vector2(-16f, -5f);
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.Normal;

        return button;
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

    private void StretchToFillParent(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }

    private RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject obj = new GameObject(objectName, typeof(RectTransform));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private RectTransform FindRect(Transform parent, string objectName)
    {
        return FindComponent<RectTransform>(parent, objectName);
    }

    private Button FindButton(Transform parent, string objectName)
    {
        return FindComponent<Button>(parent, objectName);
    }

    private TextMeshProUGUI FindText(Transform parent, string objectName)
    {
        return FindComponent<TextMeshProUGUI>(parent, objectName);
    }

    private T FindComponent<T>(Transform parent, string objectName) where T : Component
    {
        if (parent == null)
            return null;

        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name != objectName)
                continue;

            T component = child.GetComponent<T>();
            if (component != null)
                return component;
        }

        return null;
    }

    private void DestroySafe(GameObject target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
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

    private void SetTitle(string title)
    {
        if (titleText != null)
            titleText.text = title;
    }

    private void SetFeedback(string message, Color color)
    {
        if (feedbackText == null)
            return;

        feedbackText.text = message;
        feedbackText.color = color;
    }

    private void RefreshScoreText()
    {
        if (scoreText == null)
            return;

        if (currentMode == MinigameMode.WordMaster)
        {
            int displayTurn = wordMasterSolved || wordMasterTurn >= wordMasterMaxTurns
                ? wordMasterTurn
                : wordMasterTurn + 1;

            scoreText.text = $"Wins: {score} | Turn: {Mathf.Clamp(displayTurn, 1, wordMasterMaxTurns)}/{wordMasterMaxTurns}";

            if (wordMasterStatsText != null)
                wordMasterStatsText.text = $"Score\n{score}\n\nWords\n{wordMasterGuesses.Count}";
        }
        else if (currentMode == MinigameMode.WordExcavation)
        {
            scoreText.text = $"Score: {score} | Found: {foundExcavationWords.Count}";
        }
        else if (currentMode == MinigameMode.SpellItOut)
        {
            int totalWords = Mathf.Max(1, spellItOutTotalWords);
            int displayWord = spellItOutActive
                ? Mathf.Clamp(spellItOutWordIndex + 1, 1, totalWords)
                : Mathf.Clamp(roundsPlayed, 0, totalWords);
            scoreText.text = $"Score: {score}/{totalWords} | Word: {displayWord}/{totalWords}";
        }
        else
        {
            scoreText.text = $"Score: {score}/{Mathf.Max(1, roundsPlayed)}";
        }
    }

    private void UpdateTimerText()
    {
        if (timerText != null)
        {
            if (currentMode == MinigameMode.SpellItOut)
                timerText.text = $"Time: {Mathf.CeilToInt(spellItOutTimer)}s";
            else
                timerText.text = $"Time: {Mathf.CeilToInt(excavationTimer)}s";
        }

        if (currentMode == MinigameMode.WordExcavation)
            UpdateWordExcavationTimerMeter();
    }

    private void UpdateFoundWordsText()
    {
        if (foundWordsText == null)
            return;

        if (foundExcavationWords.Count == 0)
        {
            foundWordsText.text = "Found words: none yet";
            UpdateWordExcavationFoundWordsText();
            return;
        }

        foundWordsText.text = "Found words: " + string.Join(", ", foundExcavationWordsInOrder);
        UpdateWordExcavationFoundWordsText();
        UpdateWordExcavationPrizeProgress();
    }

    private void ClearOptionButtons()
    {
        foreach (Button button in optionButtons)
        {
            if (button != null)
                Destroy(button.gameObject);
        }

        optionButtons.Clear();
    }
}
