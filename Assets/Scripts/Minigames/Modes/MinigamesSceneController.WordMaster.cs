using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using LetterClue = WordMasterGuessRules.Clue;

// Maintained as a partial MonoBehaviour to preserve all existing Unity scene references.
public partial class MinigamesSceneController
{
    public void ShowWordMasterEditPreview()
    {
        if (root == null)
            CacheExistingUIReferences();

        if (root == null)
        {
            BuildUI();
            CacheExistingUIReferences();
            EnsureMinigameOverlayUI();
            WireButtonEvents();
        }

        currentMode = MinigameMode.WordMaster;
        EnsureMinigameOverlayUI();
        HideMinigameOverlays();
        score = 0;
        roundsPlayed = 0;
        excavationActive = false;
        wordMasterTargetWord = string.Empty;
        wordMasterTurn = 0;
        wordMasterHintIndex = -1;
        wordMasterSolved = false;
        wordMasterHintPanelDismissed = false;
        wordMasterLockedLetters = new char[Mathf.Max(0, wordMasterWordLength)];
        wordMasterGuesses.Clear();
        wordMasterGuessClues.Clear();
        wordMasterKeyboardColors.Clear();
        selectedTileIndexes.Clear();

        if (selectionScreen != null)
            selectionScreen.gameObject.SetActive(false);

        if (playScreen != null)
            playScreen.gameObject.SetActive(true);

        ApplyWordMasterPlayLayout();
        SetTitle("WORD MASTER");

        if (answerInput != null)
            answerInput.gameObject.SetActive(false);

        if (foundWordsText != null)
            foundWordsText.gameObject.SetActive(false);

        if (timerText != null)
            timerText.gameObject.SetActive(false);

        if (scoreText != null)
            scoreText.gameObject.SetActive(false);

        if (optionsRoot != null)
            optionsRoot.gameObject.SetActive(true);

        SetTilePickerVisible(true);

        if (currentWordTilesRoot != null)
            currentWordTilesRoot.gameObject.SetActive(false);

        if (tileInstructionText != null)
            tileInstructionText.gameObject.SetActive(false);

        if (promptText != null)
            promptText.text = $"Guess the hidden {wordMasterWordLength}-letter word. Gold = right spot. Silver = right letter.";

        BuildWordMasterBoard();
        ConfigureTilePicker(WordMasterSourceAlphabet);
    }

    private void StartWordMasterRound()
    {
        ApplyWordMasterPlayLayout();
        SetTitle("WORD MASTER");

        if (answerInput != null)
            answerInput.gameObject.SetActive(false);

        if (foundWordsText != null)
            foundWordsText.gameObject.SetActive(false);

        if (timerText != null)
            timerText.gameObject.SetActive(false);

        if (scoreText != null)
            scoreText.gameObject.SetActive(false);

        if (optionsRoot != null)
            optionsRoot.gameObject.SetActive(true);

        SetTilePickerVisible(true);

        if (currentWordTilesRoot != null)
            currentWordTilesRoot.gameObject.SetActive(false);

        if (tileInstructionText != null)
            tileInstructionText.gameObject.SetActive(false);

        wordMasterTargetWord = GetRandomWordMasterTarget();
        wordMasterTurn = 0;
        wordMasterHintIndex = wordMasterUseStartHint ? 0 : -1;
        wordMasterSolved = false;
        wordMasterHintPanelDismissed = false;
        ResetWordMasterLockedLetters();
        wordMasterGuesses.Clear();
        wordMasterGuessClues.Clear();
        wordMasterKeyboardColors.Clear();

        if (promptText != null)
            promptText.text = $"Guess the hidden {wordMasterWordLength}-letter word. Gold = right spot. Silver = right letter.";

        BuildWordMasterBoard();
        ConfigureTilePicker(WordMasterSourceAlphabet);

        Debug.Log($"[WordMaster] Round started. Target length: {wordMasterTargetWord.Length}, Source letters: {currentSourceLetters.Length}, Rows root: {(wordMasterRowsRoot != null ? wordMasterRowsRoot.name : "null")}.");
    }

    private void EnsureWordMasterRoundState()
    {
        if (currentMode != MinigameMode.WordMaster)
            return;

        bool missingTarget = string.IsNullOrWhiteSpace(wordMasterTargetWord)
            || wordMasterTargetWord.Length != wordMasterWordLength;

        if (!missingTarget)
            return;

        wordMasterTargetWord = GetRandomWordMasterTarget();
        wordMasterTurn = 0;
        wordMasterHintIndex = wordMasterUseStartHint ? 0 : -1;
        wordMasterSolved = false;
        wordMasterHintPanelDismissed = false;
        ResetWordMasterLockedLetters();
        wordMasterGuesses.Clear();
        wordMasterGuessClues.Clear();
        wordMasterKeyboardColors.Clear();

        if (promptText != null)
            promptText.text = $"Guess the hidden {wordMasterWordLength}-letter word. Gold = right spot. Silver = right letter.";

        BuildWordMasterBoard();
        Debug.Log("[WordMaster] Round state was missing. Created a new Word Master target.");
    }

    private void ResolveWordMasterGuess(string guess)
    {
        if (wordMasterSolved)
        {
            SetFeedback("Already solved. Start a new round.", goodColor);
            return;
        }

        if (wordMasterTurn >= wordMasterMaxTurns)
        {
            SetFeedback("Game over. Try again.", badColor);
            if (promptText != null)
                promptText.text = "Game over. Try again.";

            ShowMinigameResultOverlay("Game Over");
            return;
        }

        if (guess.Length != wordMasterWordLength)
        {
            SetFeedback($"Build a {wordMasterWordLength}-letter guess first.", badColor);
            return;
        }

        if (guess.Contains(" "))
        {
            SetFeedback("Fill every letter slot first.", badColor);
            return;
        }

        bool validGuess = dictionary != null ? dictionary.IsValidWord(guess) : IsFallbackValidWord(guess);
        if (!validGuess)
        {
            SetFeedback("Not in the dictionary.", badColor);
            return;
        }

        LetterClue[] clues = EvaluateWordMasterGuess(guess, wordMasterTargetWord);
        wordMasterGuesses.Add(guess);
        wordMasterGuessClues.Add(clues);
        LockWordMasterCorrectLetters(guess, clues);
        UpdateWordMasterKeyboard(guess, clues);

        wordMasterTurn++;
        roundsPlayed = wordMasterTurn;

        if (guess == wordMasterTargetWord || IsWordMasterFullyLocked())
        {
            wordMasterSolved = true;
            score++;
            SetFeedback("Solved. Clean guess.", goodColor);
        }
        else if (wordMasterTurn >= wordMasterMaxTurns)
        {
            SetFeedback("Game over. Try again.", badColor);
            if (promptText != null)
                promptText.text = "Game over. Try again.";

            ShowMinigameResultOverlay("Game Over");
        }
        else
        {
            SetFeedback(BuildWordMasterGuessSummary(guess, clues), lightTextColor);
        }

        ClearTileSelection();
        RefreshScoreText();
    }

    private string GetRandomWordMasterTarget()
    {
        PracticeEntry entry = GetRandomDefinedEntry(wordMasterWordLength, wordMasterWordLength);
        if (!string.IsNullOrWhiteSpace(entry.word) && entry.word.Length == wordMasterWordLength)
            return entry.word;

        string[] fallbackTargets = { "SPELL", "OCEAN", "MAGIC" };
        return fallbackTargets[Random.Range(0, fallbackTargets.Length)];
    }

    private LetterClue[] EvaluateWordMasterGuess(string guess, string target)
    {
        return WordMasterGuessRules.Evaluate(guess, target);
    }

    private void BuildWordMasterBoard()
    {
        if (optionsRoot == null)
            return;

        RectTransform existingBoard = FindRect(optionsRoot, "WordMasterBoard");
        if (existingBoard != null)
        {
            CacheWordMasterBoardReferences(existingBoard);
            RebuildWordMasterRows();
            return;
        }

        ClearChildren(optionsRoot);

        RectTransform board = CreateRect("WordMasterBoard", optionsRoot);
        LayoutElement boardLayout = board.gameObject.AddComponent<LayoutElement>();
        boardLayout.minWidth = 510f;
        boardLayout.preferredWidth = 510f;
        boardLayout.minHeight = 402f;
        boardLayout.preferredHeight = 402f;

        RectTransform boardPanel = CreatePanel(board, "WordMasterBoardPanel", new Color(0.055f, 0.033f, 0.018f, 0.96f), new Vector2(510f, 402f));
        boardPanel.anchorMin = new Vector2(0.5f, 0.5f);
        boardPanel.anchorMax = new Vector2(0.5f, 0.5f);
        boardPanel.pivot = new Vector2(0.5f, 0.5f);
        boardPanel.anchoredPosition = Vector2.zero;

        Outline boardTrim = boardPanel.gameObject.AddComponent<Outline>();
        boardTrim.effectColor = new Color(0.82f, 0.58f, 0.16f, 0.92f);
        boardTrim.effectDistance = new Vector2(3f, -3f);

        VerticalLayoutGroup panelGroup = boardPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        panelGroup.padding = new RectOffset(18, 18, 16, 18);
        panelGroup.spacing = 7f;
        panelGroup.childAlignment = TextAnchor.UpperCenter;
        panelGroup.childControlWidth = false;
        panelGroup.childControlHeight = false;
        panelGroup.childForceExpandWidth = false;
        panelGroup.childForceExpandHeight = false;

        RectTransform header = CreateRect("WordMasterHeader", boardPanel);
        LayoutElement headerLayout = header.gameObject.AddComponent<LayoutElement>();
        headerLayout.minWidth = 456f;
        headerLayout.preferredWidth = 456f;
        headerLayout.minHeight = 28f;
        headerLayout.preferredHeight = 28f;

        HorizontalLayoutGroup headerGroup = header.gameObject.AddComponent<HorizontalLayoutGroup>();
        headerGroup.spacing = 6f;
        headerGroup.childAlignment = TextAnchor.MiddleCenter;
        headerGroup.childControlWidth = false;
        headerGroup.childControlHeight = false;
        headerGroup.childForceExpandWidth = false;
        headerGroup.childForceExpandHeight = false;

        CreateWordMasterHeaderLabel(header, "WordMasterTurnLabel", "Turns", 54f, TextAlignmentOptions.Center);
        CreateWordMasterHeaderLabel(header, "WordMasterTilesLabel", string.Empty, 290f, TextAlignmentOptions.Center);
        CreateWordMasterHeaderLabel(header, "WordMasterPrizeLabel", "Prize", 94f, TextAlignmentOptions.Center);

        wordMasterRowsRoot = CreateRect("TurnRows", boardPanel);
        LayoutElement rowsLayout = wordMasterRowsRoot.gameObject.AddComponent<LayoutElement>();
        rowsLayout.minWidth = 456f;
        rowsLayout.preferredWidth = 456f;
        rowsLayout.minHeight = 334f;
        rowsLayout.preferredHeight = 334f;

        VerticalLayoutGroup rowsGroup = wordMasterRowsRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        rowsGroup.spacing = 7f;
        rowsGroup.childAlignment = TextAnchor.MiddleCenter;
        rowsGroup.childControlWidth = false;
        rowsGroup.childControlHeight = false;
        rowsGroup.childForceExpandWidth = false;
        rowsGroup.childForceExpandHeight = false;

        CreateWordMasterHintPanel(boardPanel);

        RebuildWordMasterRows();
    }

    private void CacheWordMasterBoardReferences(RectTransform board)
    {
        wordMasterRowsRoot = FindRect(board, "TurnRows");
        wordMasterStatsText = FindText(board, "StatsText");
        wordMasterHintPanel = FindRect(board, "HintPanel");
        RectTransform boardPanel = FindRect(board, "WordMasterBoardPanel");
        RectTransform turnPanel = boardPanel != null ? boardPanel : FindRect(board, "TurnBoard");

        if (wordMasterRowsRoot == null)
        {
            if (turnPanel != null)
            {
                wordMasterRowsRoot = CreateRect("TurnRows", turnPanel);
                wordMasterRowsRoot.anchorMin = Vector2.zero;
                wordMasterRowsRoot.anchorMax = Vector2.one;
                wordMasterRowsRoot.offsetMin = new Vector2(18f, 18f);
                wordMasterRowsRoot.offsetMax = new Vector2(-18f, -52f);

                VerticalLayoutGroup rowsGroup = wordMasterRowsRoot.gameObject.AddComponent<VerticalLayoutGroup>();
                rowsGroup.spacing = 6f;
                rowsGroup.childAlignment = TextAnchor.MiddleCenter;
                rowsGroup.childControlWidth = true;
                rowsGroup.childControlHeight = false;
                rowsGroup.childForceExpandWidth = true;
                rowsGroup.childForceExpandHeight = false;
            }
        }

        if (wordMasterHintPanel == null && turnPanel != null && ShouldUseWordMasterStartHint())
            CreateWordMasterHintPanel(turnPanel);

        WireWordMasterHintPanel();
        UpdateWordMasterHintPanel();
    }

    private void RebuildWordMasterRows()
    {
        if (wordMasterRowsRoot == null)
            return;

        if (wordMasterHintPanel != null)
        {
            wordMasterHintPanel.gameObject.SetActive(ShouldShowWordMasterHintPanel());
            UpdateWordMasterHintPanel();
        }

        if (RefreshExistingWordMasterRows())
            return;

        ClearChildren(wordMasterRowsRoot);

        for (int i = 0; i < wordMasterMaxTurns; i++)
        {
            GetWordMasterRowState(i, out string guess, out LetterClue[] clues, out bool active);
            CreateWordMasterRow(wordMasterRowsRoot, i, guess, clues, active);
        }
    }

    private bool RefreshExistingWordMasterRows()
    {
        if (wordMasterRowsRoot == null)
            return false;

        List<RectTransform> rows = new List<RectTransform>();
        for (int i = 0; i < wordMasterMaxTurns; i++)
        {
            RectTransform row = FindRect(wordMasterRowsRoot, "WordMasterRowUI_" + (i + 1));
            if (row == null)
                row = FindRect(wordMasterRowsRoot, "GuessRow" + (i + 1));

            if (row == null || row.childCount < wordMasterWordLength + 1)
                return false;

            rows.Add(row);
        }

        for (int i = 0; i < rows.Count; i++)
        {
            GetWordMasterRowState(i, out string guess, out LetterClue[] clues, out bool active);
            if (!UpdateWordMasterRow(rows[i], i, guess, clues, active))
                return false;
        }

        return true;
    }

    private void GetWordMasterRowState(int rowIndex, out string guess, out LetterClue[] clues, out bool active)
    {
        guess = string.Empty;
        clues = null;
        active = rowIndex == wordMasterTurn && !wordMasterSolved && wordMasterTurn < wordMasterMaxTurns;

        if (rowIndex < wordMasterGuesses.Count)
        {
            guess = wordMasterGuesses[rowIndex];
            clues = wordMasterGuessClues[rowIndex];
            active = false;
            return;
        }

        if (active)
            guess = GetWordMasterCurrentGuess();
    }

    private void CreateWordMasterHeaderLabel(Transform parent, string objectName, string label, float width, TextAlignmentOptions alignment)
    {
        TextMeshProUGUI text = CreateText(parent, objectName, label, 18f, FontStyles.Bold, lightTextColor);
        text.alignment = alignment;

        LayoutElement layout = text.gameObject.AddComponent<LayoutElement>();
        layout.minWidth = width;
        layout.preferredWidth = width;
        layout.minHeight = 28f;
        layout.preferredHeight = 28f;
    }

    private void CreateWordMasterRow(Transform parent, int rowIndex, string guess, LetterClue[] clues, bool active)
    {
        Color rowColor = active
            ? new Color(0.28f, 0.19f, 0.06f, 0.92f)
            : new Color(0.08f, 0.045f, 0.02f, 0.58f);

        RectTransform row = CreatePanel(parent, "WordMasterRowUI_" + (rowIndex + 1), rowColor, new Vector2(456f, 60f));

        LayoutElement rowLayout = row.gameObject.AddComponent<LayoutElement>();
        rowLayout.minWidth = 456f;
        rowLayout.preferredWidth = 456f;
        rowLayout.minHeight = 60f;
        rowLayout.preferredHeight = 60f;

        if (active)
        {
            Outline activeOutline = row.gameObject.AddComponent<Outline>();
            activeOutline.effectColor = new Color(1f, 0.88f, 0.18f, 1f);
            activeOutline.effectDistance = new Vector2(3f, -3f);
        }

        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(6, 6, 4, 4);
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        RectTransform turnBadge = CreatePanel(row, "TurnBadge", active ? new Color(0.94f, 0.82f, 0.24f, 1f) : new Color(0.22f, 0.19f, 0.13f, 0.96f), new Vector2(54f, 52f));
        LayoutElement turnBadgeLayout = turnBadge.gameObject.AddComponent<LayoutElement>();
        turnBadgeLayout.minWidth = 54f;
        turnBadgeLayout.preferredWidth = 54f;
        turnBadgeLayout.minHeight = 52f;
        turnBadgeLayout.preferredHeight = 52f;

        TextMeshProUGUI turnText = CreateText(turnBadge, "Turn", (rowIndex + 1).ToString(), 30f, FontStyles.Bold, active ? textColor : lightTextColor);
        RectTransform turnTextRect = turnText.GetComponent<RectTransform>();
        turnTextRect.anchorMin = Vector2.zero;
        turnTextRect.anchorMax = Vector2.one;
        turnTextRect.offsetMin = new Vector2(2f, 1f);
        turnTextRect.offsetMax = new Vector2(-2f, -1f);
        turnText.alignment = TextAlignmentOptions.Center;

        for (int i = 0; i < wordMasterWordLength; i++)
        {
            char letter = i < guess.Length ? guess[i] : ' ';
            Color color = active && letter != ' '
                ? new Color(0.93f, 0.84f, 0.56f, 0.98f)
                : clues == null ? new Color(0.16f, 0.10f, 0.05f, 0.88f) : GetWordMasterClueColor(clues[i]);

            if (active && IsWordMasterLockedPosition(i))
                color = wordMasterCorrectColor;

            UnityEngine.Events.UnityAction clickAction = null;
            if (active && letter != ' ' && !IsWordMasterLockedPosition(i))
            {
                int capturedPosition = i;
                clickAction = () => RemoveWordMasterLettersFromGuessPosition(capturedPosition);
            }

            CreateLetterTileDisplay(row, letter, color, new Vector2(52f, 52f), clickAction, "WordMasterTileUI_" + (i + 1));
        }

        CreateWordMasterPrizeUI(row, rowIndex, active);
    }

    private bool UpdateWordMasterRow(RectTransform row, int rowIndex, string guess, LetterClue[] clues, bool active)
    {
        if (row == null)
            return false;

        row.name = "WordMasterRowUI_" + (rowIndex + 1);

        if (row.childCount < wordMasterWordLength + 1)
            return false;

        Image rowImage = row.GetComponent<Image>();
        if (rowImage != null)
        {
            rowImage.color = active
                ? new Color(0.28f, 0.19f, 0.06f, 0.92f)
                : new Color(0.08f, 0.045f, 0.02f, 0.58f);
        }

        Outline activeOutline = row.GetComponent<Outline>();
        if (active)
        {
            if (activeOutline == null)
                activeOutline = row.gameObject.AddComponent<Outline>();

            activeOutline.enabled = true;
            activeOutline.effectColor = new Color(1f, 0.88f, 0.18f, 1f);
            activeOutline.effectDistance = new Vector2(3f, -3f);
        }
        else if (activeOutline != null)
        {
            activeOutline.enabled = false;
        }

        RectTransform turnBadge = row.GetChild(0) as RectTransform;
        if (turnBadge == null)
            return false;

        turnBadge.name = "TurnBadge";
        Image turnBadgeImage = turnBadge.GetComponent<Image>();
        if (turnBadgeImage != null)
            turnBadgeImage.color = active ? new Color(0.94f, 0.82f, 0.24f, 1f) : new Color(0.22f, 0.19f, 0.13f, 0.96f);

        TextMeshProUGUI turnText = FindText(turnBadge, "Turn");
        if (turnText != null)
        {
            turnText.text = (rowIndex + 1).ToString();
            turnText.color = active ? textColor : lightTextColor;
        }

        for (int i = 0; i < wordMasterWordLength; i++)
        {
            RectTransform slot = row.GetChild(i + 1) as RectTransform;
            if (slot == null)
                return false;

            slot.name = "WordMasterTileUI_" + (i + 1);
            char letter = i < guess.Length ? guess[i] : ' ';
            Color color = active && letter != ' '
                ? new Color(0.93f, 0.84f, 0.56f, 0.98f)
                : clues == null ? new Color(0.16f, 0.10f, 0.05f, 0.88f) : GetWordMasterClueColor(clues[i]);

            if (active && IsWordMasterLockedPosition(i))
                color = wordMasterCorrectColor;

            Image slotImage = slot.GetComponent<Image>();
            if (slotImage != null)
                slotImage.color = color;

            TextMeshProUGUI slotText = FindText(slot, "Text");
            if (slotText != null)
            {
                slotText.text = letter == ' ' ? string.Empty : letter.ToString();
                slotText.color = GetWordMasterTileTextColor(color);
            }

            Button button = slot.GetComponent<Button>();
            if (button != null)
                button.onClick.RemoveAllListeners();

            if (active && letter != ' ' && !IsWordMasterLockedPosition(i))
            {
                if (button == null)
                    button = slot.gameObject.AddComponent<Button>();

                int capturedPosition = i;
                button.targetGraphic = slotImage;
                button.onClick.AddListener(() => RemoveWordMasterLettersFromGuessPosition(capturedPosition));
            }
        }

        RectTransform prize = row.childCount > wordMasterWordLength + 1
            ? row.GetChild(wordMasterWordLength + 1) as RectTransform
            : null;

        if (prize == null)
            CreateWordMasterPrizeUI(row, rowIndex, active);
        else
            UpdateWordMasterPrizeUI(prize, rowIndex, active);

        return true;
    }

    private void CreateWordMasterPrizeUI(Transform parent, int rowIndex, bool active)
    {
        RectTransform prize = CreatePanel(parent, "WordMasterPrizeUI", GetWordMasterPrizeColor(rowIndex, active), new Vector2(94f, 52f));
        LayoutElement layout = prize.gameObject.AddComponent<LayoutElement>();
        layout.minWidth = 94f;
        layout.preferredWidth = 94f;
        layout.minHeight = 52f;
        layout.preferredHeight = 52f;

        TextMeshProUGUI text = CreateText(prize, "PrizeText", GetWordMasterPrizeText(rowIndex), 18f, FontStyles.Bold, GetWordMasterPrizeTextColor(rowIndex, active));
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(4f, 2f);
        textRect.offsetMax = new Vector2(-4f, -2f);
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 12f;
        text.fontSizeMax = 18f;

        TextMeshProUGUI crossText = CreateText(prize, "PrizeCrossText", "X", 40f, FontStyles.Bold, new Color(0.95f, 0.06f, 0.04f, 1f));
        RectTransform crossRect = crossText.GetComponent<RectTransform>();
        crossRect.anchorMin = Vector2.zero;
        crossRect.anchorMax = Vector2.one;
        crossRect.offsetMin = new Vector2(2f, -2f);
        crossRect.offsetMax = new Vector2(-2f, 2f);
        crossText.alignment = TextAlignmentOptions.Center;
        crossText.enableAutoSizing = true;
        crossText.fontSizeMin = 20f;
        crossText.fontSizeMax = 40f;
        crossText.gameObject.SetActive(IsWordMasterPrizeCrossed(rowIndex));
    }

    private void UpdateWordMasterPrizeUI(RectTransform prize, int rowIndex, bool active)
    {
        prize.name = "WordMasterPrizeUI";

        Image image = prize.GetComponent<Image>();
        if (image != null)
            image.color = GetWordMasterPrizeColor(rowIndex, active);

        TextMeshProUGUI text = FindText(prize, "PrizeText");
        if (text != null)
        {
            text.text = GetWordMasterPrizeText(rowIndex);
            text.color = GetWordMasterPrizeTextColor(rowIndex, active);
            text.fontSizeMax = 18f;
        }

        TextMeshProUGUI crossText = FindText(prize, "PrizeCrossText");
        if (crossText == null)
        {
            crossText = CreateText(prize, "PrizeCrossText", "X", 40f, FontStyles.Bold, new Color(0.95f, 0.06f, 0.04f, 1f));
            RectTransform crossRect = crossText.GetComponent<RectTransform>();
            crossRect.anchorMin = Vector2.zero;
            crossRect.anchorMax = Vector2.one;
            crossRect.offsetMin = new Vector2(2f, -2f);
            crossRect.offsetMax = new Vector2(-2f, 2f);
            crossText.alignment = TextAlignmentOptions.Center;
            crossText.enableAutoSizing = true;
            crossText.fontSizeMin = 20f;
            crossText.fontSizeMax = 40f;
        }

        crossText.text = "X";
        crossText.color = new Color(0.95f, 0.06f, 0.04f, 1f);
        crossText.gameObject.SetActive(IsWordMasterPrizeCrossed(rowIndex));
    }

    private string GetWordMasterPrizeText(int rowIndex)
    {
        switch (rowIndex)
        {
            case 0:
                return "10,000";
            case 1:
                return "5,000";
            case 2:
                return "2,500";
            case 3:
                return "1,000";
            default:
                return "500";
        }
    }

    private Color GetWordMasterPrizeColor(int rowIndex, bool active)
    {
        if (IsWordMasterPrizeCrossed(rowIndex))
            return new Color(0.08f, 0.06f, 0.04f, 0.94f);

        return active
            ? new Color(0.46f, 0.34f, 0.10f, 0.96f)
            : new Color(0.17f, 0.13f, 0.08f, 0.92f);
    }

    private Color GetWordMasterPrizeTextColor(int rowIndex, bool active)
    {
        if (IsWordMasterPrizeCrossed(rowIndex))
            return new Color(0.48f, 0.42f, 0.30f, 0.72f);

        return active ? new Color(1f, 0.92f, 0.42f, 1f) : lightTextColor;
    }

    private bool IsWordMasterPrizeCrossed(int rowIndex)
    {
        if (string.IsNullOrWhiteSpace(GetWordMasterPrizeText(rowIndex))
            || rowIndex < 0
            || rowIndex >= wordMasterGuesses.Count
            || rowIndex >= wordMasterGuessClues.Count)
        {
            return false;
        }

        LetterClue[] clues = wordMasterGuessClues[rowIndex];
        if (clues == null || clues.Length == 0)
            return false;

        for (int i = 0; i < clues.Length; i++)
        {
            if (clues[i] != LetterClue.Correct)
                return true;
        }

        return false;
    }

    private void CreateWordMasterHintPanel(Transform parent)
    {
        wordMasterHintPanel = null;
        if (!ShouldUseWordMasterStartHint())
            return;

        char hintLetter = wordMasterTargetWord[wordMasterHintIndex];
        wordMasterHintPanel = CreatePanel(parent, "HintPanel", new Color(0.06f, 0.035f, 0.015f, 0.96f), new Vector2(330f, 82f));
        wordMasterHintPanel.anchorMin = new Vector2(0f, 0.5f);
        wordMasterHintPanel.anchorMax = new Vector2(0f, 0.5f);
        wordMasterHintPanel.pivot = new Vector2(0f, 0.5f);
        wordMasterHintPanel.anchoredPosition = new Vector2(32f, -36f);

        Image border = wordMasterHintPanel.GetComponent<Image>();
        if (border != null)
            border.color = new Color(0.08f, 0.045f, 0.02f, 0.96f);

        TextMeshProUGUI title = CreateText(wordMasterHintPanel, "HintTitle", "Hint!", 30f, FontStyles.Bold, wordMasterCorrectColor);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.48f);
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = new Vector2(18f, 0f);
        titleRect.offsetMax = new Vector2(-18f, -4f);
        title.alignment = TextAlignmentOptions.Center;

        TextMeshProUGUI body = CreateText(wordMasterHintPanel, "HintBody", $"{hintLetter} is the first letter of the mystery word.", 18f, FontStyles.Bold, new Color(0.92f, 0.35f, 0.24f, 1f));
        RectTransform bodyRect = body.GetComponent<RectTransform>();
        bodyRect.anchorMin = Vector2.zero;
        bodyRect.anchorMax = new Vector2(1f, 0.5f);
        bodyRect.offsetMin = new Vector2(18f, 8f);
        bodyRect.offsetMax = new Vector2(-18f, 2f);
        body.alignment = TextAlignmentOptions.Center;
        body.textWrappingMode = TextWrappingModes.Normal;
        WireWordMasterHintPanel();
    }

    private void UpdateWordMasterHintPanel()
    {
        if (wordMasterHintPanel == null || !ShouldUseWordMasterStartHint())
            return;

        char hintLetter = wordMasterTargetWord[wordMasterHintIndex];

        TextMeshProUGUI title = FindText(wordMasterHintPanel, "HintTitle");
        if (title != null)
            title.text = "Hint!";

        TextMeshProUGUI body = FindText(wordMasterHintPanel, "HintBody");
        if (body != null)
            body.text = $"{hintLetter} is the first letter of the mystery word.";
    }

    private void WireWordMasterHintPanel()
    {
        if (wordMasterHintPanel == null)
            return;

        Image image = wordMasterHintPanel.GetComponent<Image>();
        if (image != null)
            image.raycastTarget = true;

        Button button = wordMasterHintPanel.GetComponent<Button>();
        if (button == null)
            button = wordMasterHintPanel.gameObject.AddComponent<Button>();

        button.targetGraphic = image;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(DismissWordMasterHintPanel);
    }

    private void DismissWordMasterHintPanel()
    {
        wordMasterHintPanelDismissed = true;

        if (wordMasterHintPanel != null)
            wordMasterHintPanel.gameObject.SetActive(false);
    }

    private void UpdateWordMasterKeyboard(string guess, LetterClue[] clues)
    {
        for (int i = 0; i < guess.Length; i++)
        {
            char letter = guess[i];
            Color clueColor = GetWordMasterClueColor(clues[i]);

            if (!wordMasterKeyboardColors.TryGetValue(letter, out Color existingColor)
                || IsBetterWordMasterClue(clueColor, existingColor))
            {
                wordMasterKeyboardColors[letter] = clueColor;
            }
        }
    }

    private bool IsBetterWordMasterClue(Color candidate, Color existing)
    {
        return GetWordMasterClueRank(candidate) > GetWordMasterClueRank(existing);
    }

    private int GetWordMasterClueRank(Color color)
    {
        if (ColorsApproximatelyEqual(color, wordMasterCorrectColor))
            return 3;

        if (ColorsApproximatelyEqual(color, wordMasterPresentColor))
            return 2;

        if (ColorsApproximatelyEqual(color, wordMasterMissingColor))
            return 1;

        return 0;
    }

    private bool ColorsApproximatelyEqual(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < 0.02f
            && Mathf.Abs(a.g - b.g) < 0.02f
            && Mathf.Abs(a.b - b.b) < 0.02f;
    }

    private Color GetWordMasterClueColor(LetterClue clue)
    {
        switch (clue)
        {
            case LetterClue.Correct:
                return wordMasterCorrectColor;
            case LetterClue.Present:
                return wordMasterPresentColor;
            default:
                return wordMasterMissingColor;
        }
    }

    private string GetWordMasterCurrentGuess()
    {
        StringBuilder builder = new StringBuilder();
        int selectedCursor = 0;

        for (int i = 0; i < wordMasterWordLength; i++)
        {
            if (IsWordMasterLockedPosition(i))
            {
                builder.Append(wordMasterLockedLetters[i]);
                continue;
            }

            if (selectedCursor >= selectedTileIndexes.Count)
            {
                builder.Append(' ');
                continue;
            }

            int sourceIndex = selectedTileIndexes[selectedCursor];
            selectedCursor++;

            if (sourceIndex < 0 || sourceIndex >= currentSourceLetters.Length)
            {
                builder.Append(' ');
                continue;
            }

            builder.Append(currentSourceLetters[sourceIndex]);
        }

        return builder.ToString();
    }

    private int GetWordMasterSelectionLimit()
    {
        return CountWordMasterUnlockedPositions();
    }

    private void ResetWordMasterLockedLetters()
    {
        wordMasterLockedLetters = new char[Mathf.Max(0, wordMasterWordLength)];

        if (!wordMasterUseStartHint
            || string.IsNullOrWhiteSpace(wordMasterTargetWord)
            || wordMasterHintIndex < 0
            || wordMasterHintIndex >= wordMasterLockedLetters.Length
            || wordMasterHintIndex >= wordMasterTargetWord.Length)
        {
            return;
        }

        wordMasterLockedLetters[wordMasterHintIndex] = wordMasterTargetWord[wordMasterHintIndex];
    }

    private void LockWordMasterCorrectLetters(string guess, LetterClue[] clues)
    {
        EnsureWordMasterLockedLetters();

        for (int i = 0; i < guess.Length && i < clues.Length && i < wordMasterLockedLetters.Length; i++)
        {
            if (clues[i] == LetterClue.Correct)
                wordMasterLockedLetters[i] = wordMasterTargetWord[i];
        }
    }

    private void EnsureWordMasterLockedLetters()
    {
        if (wordMasterLockedLetters != null && wordMasterLockedLetters.Length == wordMasterWordLength)
            return;

        ResetWordMasterLockedLetters();
    }

    private bool IsWordMasterLockedPosition(int guessPosition)
    {
        return wordMasterLockedLetters != null
            && guessPosition >= 0
            && guessPosition < wordMasterLockedLetters.Length
            && wordMasterLockedLetters[guessPosition] != '\0';
    }

    private int CountWordMasterUnlockedPositions()
    {
        EnsureWordMasterLockedLetters();

        int count = 0;
        for (int i = 0; i < wordMasterWordLength; i++)
        {
            if (!IsWordMasterLockedPosition(i))
                count++;
        }

        return count;
    }

    private bool TryConsumeWordMasterLockedLetterClick(char clickedLetter)
    {
        EnsureWordMasterLockedLetters();

        for (int i = 0; i < wordMasterWordLength; i++)
        {
            if (!IsWordMasterLockedPosition(i)
                || wordMasterConsumedLockedInputPositions.Contains(i)
                || wordMasterLockedLetters[i] != clickedLetter
                || !AreWordMasterOpenSlotsBeforeFilled(i))
            {
                continue;
            }

            wordMasterConsumedLockedInputPositions.Add(i);
            return true;
        }

        return false;
    }

    private bool AreWordMasterOpenSlotsBeforeFilled(int guessPosition)
    {
        int unlockedSlotIndex = 0;

        for (int i = 0; i < guessPosition; i++)
        {
            if (IsWordMasterLockedPosition(i))
                continue;

            if (unlockedSlotIndex >= selectedTileIndexes.Count || selectedTileIndexes[unlockedSlotIndex] < 0)
                return false;

            unlockedSlotIndex++;
        }

        return true;
    }

    private bool IsWordMasterFullyLocked()
    {
        return CountWordMasterUnlockedPositions() == 0;
    }

    private string BuildWordMasterGuessSummary(string guess, LetterClue[] clues)
    {
        List<char> gold = new List<char>();
        List<char> silver = new List<char>();
        List<char> missing = new List<char>();

        for (int i = 0; i < guess.Length && i < clues.Length; i++)
        {
            switch (clues[i])
            {
                case LetterClue.Correct:
                    gold.Add(guess[i]);
                    break;
                case LetterClue.Present:
                    silver.Add(guess[i]);
                    break;
                default:
                    missing.Add(guess[i]);
                    break;
            }
        }

        List<string> parts = new List<string>();
        if (gold.Count > 0)
            parts.Add("Gold locked: " + string.Join(", ", gold));

        if (silver.Count > 0)
            parts.Add("Silver: " + string.Join(", ", silver));

        if (missing.Count > 0)
            parts.Add("Missing: " + string.Join(", ", missing));

        return parts.Count > 0 ? string.Join(" | ", parts) : "Keep going.";
    }

    private bool ShouldUseWordMasterStartHint()
    {
        return wordMasterUseStartHint
            && wordMasterTurn == 0
            && !wordMasterSolved
            && wordMasterHintIndex >= 0
            && wordMasterHintIndex < wordMasterTargetWord.Length
            && wordMasterHintIndex < wordMasterWordLength;
    }

    private bool ShouldShowWordMasterHintPanel()
    {
        return ShouldUseWordMasterStartHint() && !wordMasterHintPanelDismissed;
    }

    private bool IsWordMasterHintPosition(int guessPosition)
    {
        return IsWordMasterLockedPosition(guessPosition);
    }

    private void RemoveWordMasterLettersFromGuessPosition(int guessPosition)
    {
        int selectedIndex = GetWordMasterSelectedIndexForGuessPosition(guessPosition);
        if (selectedIndex < 0 || selectedIndex >= selectedTileIndexes.Count)
            return;

        selectedTileIndexes[selectedIndex] = -1;
        RefreshTilePickerVisuals();
    }

    private int GetWordMasterSelectedIndexForGuessPosition(int guessPosition)
    {
        if (IsWordMasterLockedPosition(guessPosition))
            return -1;

        int selectedIndex = 0;
        for (int i = 0; i < guessPosition; i++)
        {
            if (!IsWordMasterLockedPosition(i))
                selectedIndex++;
        }

        return selectedIndex;
    }

    private void RebuildWordMasterKeyboard()
    {
        if (RefreshExistingWordMasterKeyboard())
            return;

        ClearChildren(sourceWordTilesRoot);

        CreateWordMasterKeyboardRow(0, 13, 24f);
        CreateWordMasterKeyboardRow(13, 13, -26f);
    }

    private bool RefreshExistingWordMasterKeyboard()
    {
        if (sourceWordTilesRoot == null || currentSourceLetters.Length == 0)
            return false;

        List<Button> buttons = new List<Button>();
        for (int i = 0; i < currentSourceLetters.Length; i++)
        {
            Button button = FindButton(sourceWordTilesRoot, "LetterTile_" + currentSourceLetters[i]);
            if (button == null)
                return false;

            buttons.Add(button);
        }

        for (int i = 0; i < buttons.Count; i++)
        {
            Button button = buttons[i];
            char letter = currentSourceLetters[i];
            Color tileColor = wordMasterKeyboardColors.TryGetValue(letter, out Color clueColor)
                ? clueColor
                : new Color(0.93f, 0.84f, 0.56f, 0.98f);

            Image image = button.targetGraphic as Image;
            if (image == null)
                image = button.GetComponent<Image>();

            if (image != null)
                image.color = tileColor;

            TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                bool missingLetter = wordMasterKeyboardColors.ContainsKey(letter)
                    && ColorsApproximatelyEqual(tileColor, wordMasterMissingColor);

                text.text = letter.ToString();
                text.color = missingLetter ? lightTextColor : textColor;
            }

            int capturedIndex = i;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectSourceTile(capturedIndex));
        }

        return true;
    }

    private void CreateWordMasterKeyboardRow(int startIndex, int count, float anchoredY)
    {
        RectTransform row = CreateRect("KeyboardRow" + startIndex, sourceWordTilesRoot);
        row.sizeDelta = new Vector2(840f, 46f);
        row.anchorMin = new Vector2(0.5f, 0.5f);
        row.anchorMax = new Vector2(0.5f, 0.5f);
        row.pivot = new Vector2(0.5f, 0.5f);
        row.anchoredPosition = new Vector2(0f, anchoredY);

        LayoutElement rowLayout = row.gameObject.AddComponent<LayoutElement>();
        rowLayout.minWidth = 840f;
        rowLayout.preferredWidth = 840f;
        rowLayout.minHeight = 46f;
        rowLayout.preferredHeight = 46f;

        HorizontalLayoutGroup rowGroup = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowGroup.spacing = 5f;
        rowGroup.childAlignment = TextAnchor.MiddleCenter;
        rowGroup.childControlWidth = false;
        rowGroup.childControlHeight = false;
        rowGroup.childForceExpandWidth = false;
        rowGroup.childForceExpandHeight = false;

        int endIndex = Mathf.Min(startIndex + count, currentSourceLetters.Length);
        for (int i = startIndex; i < endIndex; i++)
        {
            int capturedIndex = i;
            char letter = currentSourceLetters[i];
            Color tileColor = wordMasterKeyboardColors.TryGetValue(letter, out Color clueColor)
                ? clueColor
                : new Color(0.93f, 0.84f, 0.56f, 0.98f);

            Button button = CreateLetterTile(row, letter, tileColor, () => SelectSourceTile(capturedIndex));
            TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                bool missingLetter = wordMasterKeyboardColors.ContainsKey(letter)
                    && ColorsApproximatelyEqual(tileColor, wordMasterMissingColor);

                text.color = missingLetter ? lightTextColor : textColor;
            }
        }
    }
}
