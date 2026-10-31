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
    public void ShowWordExcavationEditPreview()
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

        currentMode = MinigameMode.WordExcavation;
        EnsureMinigameOverlayUI();
        HideMinigameOverlays();
        score = 0;
        roundsPlayed = 0;
        excavationSession.Begin(string.Empty, excavationRoundSeconds);
        excavationActive = false;
        selectedTileIndexes.Clear();

        if (selectionScreen != null)
            selectionScreen.gameObject.SetActive(false);

        if (playScreen != null)
            playScreen.gameObject.SetActive(true);

        ApplyWordExcavationPlayLayout();
        SetTitle("WORD EXCAVATION");
        SetObjectActive(answerInput, false);
        SetObjectActive(foundWordsText, false);
        SetObjectActive(timerText, false);
        SetObjectActive(scoreText, false);
        SetObjectActive(optionsRoot, true);
        SetTilePickerVisible(true);

        if (promptText != null)
            promptText.text = "Find smaller valid words before time runs out.";

        if (tileInstructionText != null)
            tileInstructionText.text = "Click source tiles to build a word, then submit.";

        BuildWordExcavationBoard();
        ConfigureTilePicker(string.Empty);
        UpdateTimerText();
        UpdateFoundWordsText();
    }

    private void StartExcavationRound()
    {
        ApplyWordExcavationPlayLayout();
        SetTitle("WORD EXCAVATION");
        SetObjectActive(answerInput, false);
        SetObjectActive(foundWordsText, false);
        SetObjectActive(timerText, false);
        SetObjectActive(scoreText, false);
        SetObjectActive(optionsRoot, true);
        SetTilePickerVisible(true);

        excavationSession.Begin(GetRandomWordExcavationSourceWord(), excavationRoundSeconds);

        if (promptText != null)
            promptText.text = "Find smaller valid words before time runs out.";

        if (tileInstructionText != null)
            tileInstructionText.text = "Click source tiles to build a word, then submit.";

        BuildWordExcavationBoard();
        ConfigureTilePicker(excavationSourceWord);
        UpdateTimerText();
        UpdateFoundWordsText();
    }

    private void ResolveExcavationAnswer(string answer)
    {
        WordExcavationSession.SubmissionStatus status = excavationSession.Submit(
            answer, excavationMinimumWordLength,
            word => dictionary != null ? dictionary.IsValidWord(word) : IsFallbackValidWord(word),
            excavationRoundSeconds, out int points);
        switch (status)
        {
            case WordExcavationSession.SubmissionStatus.Inactive:
                SetFeedback("Start a new round first.", badColor);
                return;
            case WordExcavationSession.SubmissionStatus.TooShort:
                SetFeedback($"Use at least {excavationMinimumWordLength} letters.", badColor);
                return;
            case WordExcavationSession.SubmissionStatus.Duplicate:
                SetFeedback("Already found.", badColor);
                return;
            case WordExcavationSession.SubmissionStatus.CannotBuild:
                SetFeedback("Those letters are not all available.", badColor);
                return;
            case WordExcavationSession.SubmissionStatus.InvalidWord:
                SetFeedback("Not in the dictionary.", badColor);
                return;
        }

        score = excavationSession.Score;
        roundsPlayed = foundExcavationWords.Count;
        SetFeedback($"+{points} points", goodColor);
        ClearTileSelection();
        RefreshScoreText();
        UpdateFoundWordsText();
        UpdateWordExcavationPrizeProgress();
    }

    private string GetRandomWordExcavationSourceWord()
    {
        int firstLength = Random.value < 0.5f ? 8 : 9;
        int secondLength = firstLength == 8 ? 9 : 8;

        if (TryGetDefinedWordByExactLength(firstLength, out string firstWord))
            return firstWord;

        if (TryGetDefinedWordByExactLength(secondLength, out string secondWord))
            return secondWord;

        List<string> fallbackWords = new List<string>();
        foreach (string word in ExcavationFallbackSourceWords)
        {
            if (word.Length == firstLength || word.Length == secondLength)
                fallbackWords.Add(word);
        }

        return fallbackWords.Count > 0
            ? fallbackWords[Random.Range(0, fallbackWords.Count)]
            : "TREASURE";
    }

    private bool TryGetDefinedWordByExactLength(int length, out string word)
    {
        if (dictionary != null)
        {
            for (int i = 0; i < 80; i++)
            {
                string candidate = dictionary.GetRandomWordByLengthRange(length, length);
                string definition = dictionary.GetDefinition(candidate);
                if (!string.IsNullOrWhiteSpace(candidate)
                    && candidate.Length == length
                    && IsUsableDefinition(definition))
                {
                    word = candidate.ToUpperInvariant();
                    return true;
                }
            }
        }

        List<string> fallbackWords = new List<string>();
        foreach (string fallback in ExcavationFallbackSourceWords)
        {
            if (fallback.Length == length)
                fallbackWords.Add(fallback);
        }

        if (fallbackWords.Count > 0)
        {
            word = fallbackWords[Random.Range(0, fallbackWords.Count)];
            return true;
        }

        word = string.Empty;
        return false;
    }

    private void ApplyWordExcavationTimeBonus(string answer)
    {
        int bonusSeconds = GetWordExcavationTimeBonus(answer);
        if (bonusSeconds <= 0)
            return;

        excavationTimer = Mathf.Min(excavationRoundSeconds, excavationTimer + bonusSeconds);
        UpdateTimerText();
    }

    private int GetWordExcavationTimeBonus(string answer)
    {
        return WordExcavationRules.GetTimeBonus(answer, excavationSourceWord);
    }

    private void BuildWordExcavationBoard()
    {
        if (optionsRoot == null)
            return;

        RectTransform existingBoard = FindRect(optionsRoot, "WordExcavationBoard");
        if (existingBoard != null)
        {
            CacheWordExcavationBoardReferences(existingBoard);
            UpdateWordExcavationBoard();
            return;
        }

        ClearChildren(optionsRoot);

        RectTransform board = CreatePanel(optionsRoot, "WordExcavationBoard", new Color(0.055f, 0.033f, 0.018f, 0.96f), new Vector2(510f, 330f));
        LayoutElement boardLayout = board.gameObject.AddComponent<LayoutElement>();
        boardLayout.minWidth = 510f;
        boardLayout.preferredWidth = 510f;
        boardLayout.minHeight = 602.4f;
        boardLayout.preferredHeight = 330f;

        Outline boardTrim = board.gameObject.AddComponent<Outline>();
        boardTrim.effectColor = new Color(0.82f, 0.58f, 0.16f, 0.92f);
        boardTrim.effectDistance = new Vector2(3f, -3f);

        HorizontalLayoutGroup boardGroup = board.gameObject.AddComponent<HorizontalLayoutGroup>();
        boardGroup.padding = new RectOffset(14, 14, 12, 12);
        boardGroup.spacing = 10f;
        boardGroup.childAlignment = TextAnchor.MiddleCenter;
        boardGroup.childControlWidth = false;
        boardGroup.childControlHeight = false;
        boardGroup.childForceExpandWidth = false;
        boardGroup.childForceExpandHeight = false;

        CreateWordExcavationTimerColumn(board);
        CreateWordExcavationCenterPanel(board);
        CreateWordExcavationPrizeColumn(board);

        CacheWordExcavationBoardReferences(board);
        UpdateWordExcavationBoard();
    }

    private void CacheWordExcavationBoardReferences(RectTransform board)
    {
        wordExcavationTimerFill = FindRect(board, "ExcavationTimeFill");
        wordExcavationTimerText = FindText(board, "ExcavationTimeText");
        EnsureWordExcavationTimerText(board);
        RectTransform sourceText = FindRect(board, "ExcavationSourceText");
        if (sourceText != null)
            DestroySafe(sourceText.gameObject);

        wordExcavationFoundWordsText = FindText(board, "ExcavationFoundWordsText");
        ApplyWordExcavationFoundWordsTextLayout();
        EnsureWordExcavationFoundWordsTierTexts(board);
    }

    private void EnsureWordExcavationFoundWordsTierTexts(RectTransform board)
    {
        RectTransform panel = FindRect(board, "ExcavationWordPanel");
        if (panel == null)
            return;

        for (int i = 1; i < 5; i++)
        {
            TextMeshProUGUI tierText = FindText(panel, GetWordExcavationFoundWordsTierName(i));
            if (tierText == null)
                tierText = CreateText(panel, GetWordExcavationFoundWordsTierName(i), string.Empty, 17f, FontStyles.Bold, lightTextColor);

            ApplyWordExcavationFoundWordsTextLayout(tierText, i);
        }
    }

    private void CreateWordExcavationTimerColumn(Transform parent)
    {
        RectTransform column = CreatePanel(parent, "ExcavationTimeColumn", new Color(0.13f, 0.09f, 0.045f, 0.95f), new Vector2(58f, 585.33f));
        AddFixedLayout(column, 58f, 306f);

        TextMeshProUGUI label = CreateText(column, "ExcavationTimeLabel", "Time", 15f, FontStyles.Bold, lightTextColor);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(2f, -28f);
        labelRect.offsetMax = new Vector2(-2f, -4f);
        label.alignment = TextAlignmentOptions.Center;

        RectTransform track = CreatePanel(column, "ExcavationTimeTrack", new Color(0.04f, 0.025f, 0.015f, 1f), new Vector2(20f, 230f));
        track.anchorMin = new Vector2(0.5f, 0f);
        track.anchorMax = new Vector2(0.5f, 1f);
        track.pivot = new Vector2(0.5f, 0f);
        track.offsetMin = new Vector2(-10f, 42f);
        track.offsetMax = new Vector2(10f, -34f);

        wordExcavationTimerFill = CreatePanel(track, "ExcavationTimeFill", Color.green, Vector2.zero);
        wordExcavationTimerFill.anchorMin = Vector2.zero;
        wordExcavationTimerFill.anchorMax = Vector2.one;
        wordExcavationTimerFill.offsetMin = new Vector2(3f, 3f);
        wordExcavationTimerFill.offsetMax = new Vector2(-3f, -3f);

        wordExcavationTimerText = CreateText(column, "ExcavationTimeText", "60", 20f, FontStyles.Bold, lightTextColor);
        ApplyWordExcavationTimerTextLayout();
    }

    private void EnsureWordExcavationTimerText(RectTransform board)
    {
        RectTransform column = FindRect(board, "ExcavationTimeColumn");
        if (column == null)
            return;

        if (wordExcavationTimerText == null)
            wordExcavationTimerText = CreateText(column, "ExcavationTimeText", "60", 20f, FontStyles.Bold, lightTextColor);

        ApplyWordExcavationTimerTextLayout();
    }

    private void ApplyWordExcavationTimerTextLayout()
    {
        if (wordExcavationTimerText == null)
            return;

        RectTransform timeRect = wordExcavationTimerText.GetComponent<RectTransform>();
        timeRect.anchorMin = new Vector2(0.5f, 0f);
        timeRect.anchorMax = new Vector2(0.5f, 0f);
        timeRect.pivot = new Vector2(0.5f, 0f);
        timeRect.sizeDelta = new Vector2(44f, 30f);
        timeRect.anchoredPosition = new Vector2(0f, 7f);
        wordExcavationTimerText.alignment = TextAlignmentOptions.Center;
        wordExcavationTimerText.fontSize = 20f;
        wordExcavationTimerText.fontStyle = FontStyles.Bold;
        wordExcavationTimerText.color = lightTextColor;
        wordExcavationTimerText.gameObject.SetActive(true);
    }

    private void CreateWordExcavationCenterPanel(Transform parent)
    {
        RectTransform panel = CreatePanel(parent, "ExcavationWordPanel", new Color(0.035f, 0.035f, 0.022f, 0.98f), new Vector2(501.6f, 581.9f));
        AddFixedLayout(panel, 330f, 306f);

        wordExcavationFoundWordsText = CreateText(panel, "ExcavationFoundWordsText", string.Empty, 17f, FontStyles.Bold, lightTextColor);
        ApplyWordExcavationFoundWordsTextLayout(wordExcavationFoundWordsText, 0);

        for (int i = 1; i < 5; i++)
        {
            TextMeshProUGUI tierText = CreateText(panel, GetWordExcavationFoundWordsTierName(i), string.Empty, 17f, FontStyles.Bold, lightTextColor);
            ApplyWordExcavationFoundWordsTextLayout(tierText, i);
        }
    }

    private void ApplyWordExcavationFoundWordsTextLayout()
    {
        ApplyWordExcavationFoundWordsTextLayout(wordExcavationFoundWordsText, 0);
    }

    private void ApplyWordExcavationFoundWordsTextLayout(TextMeshProUGUI text, int tierIndex)
    {
        if (text == null)
            return;

        float bottom = GetWordExcavationFoundWordsTierBottom(tierIndex);
        const float height = 58f;

        RectTransform foundRect = text.GetComponent<RectTransform>();
        foundRect.anchorMin = new Vector2(0f, 0f);
        foundRect.anchorMax = new Vector2(1f, 0f);
        foundRect.offsetMin = new Vector2(16f, bottom);
        foundRect.offsetMax = new Vector2(-16f, bottom + height);

        text.alignment = TextAlignmentOptions.BottomLeft;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
    }

    private void CreateWordExcavationPrizeColumn(Transform parent)
    {
        RectTransform column = CreatePanel(parent, "ExcavationPrizeColumn", new Color(0.13f, 0.09f, 0.045f, 0.95f), new Vector2(78f, 605.4f));
        AddFixedLayout(column, 78f, 306f);

        VerticalLayoutGroup layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 7, 7);
        layout.spacing = 62.1f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        TextMeshProUGUI label = CreateText(column, "ExcavationPrizeLabel", "Prize", 15f, FontStyles.Bold, lightTextColor);
        AddFixedLayout(label.GetComponent<RectTransform>(), 62f, 24f);
        label.alignment = TextAlignmentOptions.Center;

        string[] prizes = { "10,000", "5,000", "2,500", "1,000", "500" };
        for (int i = 0; i < prizes.Length; i++)
            CreateWordExcavationPrizeBox(column, prizes[i]);
    }

    private void CreateWordExcavationPrizeBox(Transform parent, string value)
    {
        RectTransform box = CreatePanel(parent, "ExcavationPrize_" + value.Replace(",", string.Empty), new Color(0.17f, 0.13f, 0.08f, 0.92f), new Vector2(62f, 45f));
        AddFixedLayout(box, 62f, 45f);

        int requiredWords = GetWordExcavationPrizeRequiredWordCount(value);
        TextMeshProUGUI text = CreateText(box, "Text", value, 15f, FontStyles.Bold, GetWordExcavationPrizeTextColor(requiredWords));
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(3f, 2f);
        textRect.offsetMax = new Vector2(-3f, -2f);
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 10f;
        text.fontSizeMax = 15f;
    }

    private void UpdateWordExcavationBoard()
    {
        UpdateWordExcavationTimerMeter();
        UpdateWordExcavationFoundWordsText();
        UpdateWordExcavationPrizeProgress();
    }

    private void UpdateWordExcavationTimerMeter()
    {
        float maxTime = Mathf.Max(1f, excavationRoundSeconds);
        float ratio = Mathf.Clamp01(excavationTimer / maxTime);

        if (wordExcavationTimerFill != null)
        {
            wordExcavationTimerFill.anchorMin = Vector2.zero;
            wordExcavationTimerFill.anchorMax = new Vector2(1f, ratio);
            wordExcavationTimerFill.offsetMin = new Vector2(3f, 3f);
            wordExcavationTimerFill.offsetMax = new Vector2(-3f, -3f);

            Image fillImage = wordExcavationTimerFill.GetComponent<Image>();
            if (fillImage != null)
                fillImage.color = Color.Lerp(new Color(0.34f, 0.02f, 0.02f, 1f), new Color(0.42f, 1f, 0.08f, 1f), ratio);
        }

        if (wordExcavationTimerText != null)
            wordExcavationTimerText.text = Mathf.CeilToInt(excavationTimer).ToString();
    }

    private void UpdateWordExcavationFoundWordsText()
    {
        if (wordExcavationFoundWordsText == null)
            return;

        for (int i = 0; i < 5; i++)
        {
            TextMeshProUGUI tierText = GetWordExcavationFoundWordsTierText(i);
            if (tierText == null)
                continue;

            tierText.text = BuildWordExcavationFoundWordsTierGrid(i);
        }
    }

    private TextMeshProUGUI GetWordExcavationFoundWordsTierText(int tierIndex)
    {
        if (tierIndex == 0)
            return wordExcavationFoundWordsText;

        if (optionsRoot == null)
            return null;

        return FindText(optionsRoot, GetWordExcavationFoundWordsTierName(tierIndex));
    }

    private string BuildWordExcavationFoundWordsTierGrid(int tierIndex)
    {
        const int wordsPerRow = 3;
        const int rowsPerTier = 2;
        const int columnWidth = 20;
        int wordsPerTier = wordsPerRow * rowsPerTier;
        int tierStart = tierIndex * wordsPerTier;

        if (tierStart >= foundExcavationWordsInOrder.Count)
            return string.Empty;

        List<string> tierRows = new List<string>();
        int tierEnd = Mathf.Min(tierStart + wordsPerTier, foundExcavationWordsInOrder.Count);

        for (int rowStart = tierStart; rowStart < tierEnd; rowStart += wordsPerRow)
        {
            List<string> columns = new List<string>();
            int rowEnd = Mathf.Min(rowStart + wordsPerRow, tierEnd);

            for (int j = rowStart; j < rowEnd; j++)
            {
                string word = foundExcavationWordsInOrder[j].ToUpperInvariant();
                columns.Add(j < rowEnd - 1 ? word.PadRight(columnWidth) : word);
            }

            tierRows.Add(string.Join(string.Empty, columns));
        }

        tierRows.Reverse();
        return string.Join("\n", tierRows);
    }

    private string GetWordExcavationFoundWordsTierName(int tierIndex)
    {
        switch (tierIndex)
        {
            case 1:
                return "ExcavationFoundWordsText_1000";
            case 2:
                return "ExcavationFoundWordsText_2500";
            case 3:
                return "ExcavationFoundWordsText_5000";
            case 4:
                return "ExcavationFoundWordsText_10000";
            default:
                return "ExcavationFoundWordsText";
        }
    }

    private float GetWordExcavationFoundWordsTierBottom(int tierIndex)
    {
        const float bottomTierY = 15.70001f;
        const float prizeTierStepY = 107.1f;
        return bottomTierY + (Mathf.Clamp(tierIndex, 0, 4) * prizeTierStepY);
    }

    private void UpdateWordExcavationPrizeProgress()
    {
        if (optionsRoot == null)
            return;

        string[] prizes = { "10,000", "5,000", "2,500", "1,000", "500" };
        for (int i = 0; i < prizes.Length; i++)
        {
            RectTransform prize = FindRect(optionsRoot, "ExcavationPrize_" + prizes[i].Replace(",", string.Empty));
            if (prize == null)
                continue;

            TextMeshProUGUI text = FindText(prize, "Text");
            if (text == null)
                continue;

            int requiredWords = GetWordExcavationPrizeRequiredWordCount(prizes[i]);
            text.color = GetWordExcavationPrizeTextColor(requiredWords);
        }
    }

    private int GetWordExcavationPrizeRequiredWordCount(string prizeText)
    {
        switch (prizeText)
        {
            case "10,000":
                return 25;
            case "5,000":
                return 19;
            case "2,500":
                return 13;
            case "1,000":
                return 7;
            case "500":
                return 3;
            default:
                return int.MaxValue;
        }
    }

    private Color GetWordExcavationPrizeTextColor(int requiredWords)
    {
        return foundExcavationWordsInOrder.Count >= requiredWords
            ? new Color(0.42f, 1f, 0.08f, 1f)
            : lightTextColor;
    }
}
