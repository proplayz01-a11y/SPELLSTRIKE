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
    public void ClearTileSelection()
    {
        selectedTileIndexes.Clear();
        if (currentMode == MinigameMode.WordMaster)
            wordMasterConsumedLockedInputPositions.Clear();

        RefreshTilePickerVisuals();
    }

    public void SelectSourceTile(int tileIndex)
    {
        if (Time.frameCount == lastSourceTileClickFrame && tileIndex == lastSourceTileClickIndex)
            return;

        lastSourceTileClickFrame = Time.frameCount;
        lastSourceTileClickIndex = tileIndex;

        if (currentMode == MinigameMode.WordMaster)
        {
            Debug.Log($"[WordMaster] Source tile clicked. Index: {tileIndex}, Source letters: {currentSourceLetters.Length}, Selected: {selectedTileIndexes.Count}.");

            if (currentSourceLetters.Length == 0)
            {
                Debug.LogWarning("[WordMaster] Source letters were empty during click. Reinitializing Word Master source letters.");
                EnsureWordMasterRoundState();
                ConfigureTilePicker(WordMasterSourceAlphabet);
            }
        }

        ToggleSourceTile(tileIndex);
    }

    private void ConfigureTilePicker(string sourceLetters)
    {
        tileSelection.Configure(sourceLetters);
        if (currentMode == MinigameMode.WordMaster)
            wordMasterConsumedLockedInputPositions.Clear();

        RefreshTilePickerVisuals();
    }

    private void ToggleSourceTile(int tileIndex)
    {
        if (tileIndex < 0 || tileIndex >= currentSourceLetters.Length)
        {
            Debug.LogWarning($"[WordMaster] Ignored source tile click. Index {tileIndex} is outside source letters length {currentSourceLetters.Length}.");
            return;
        }

        if (currentMode == MinigameMode.WordMaster)
        {
            if (wordMasterSolved)
            {
                SetFeedback("Solved. Start a new round.", goodColor);
                return;
            }

            if (wordMasterTurn >= wordMasterMaxTurns)
            {
                SetFeedback("Game over. Try again.", badColor);
                return;
            }

            // Locked gold positions are already filled into the guess automatically.
            // Do not swallow clicks of the same letter: the target may contain that
            // letter again in another position (e.g. TRUTH with a locked first T).
            // The old consumption gate made duplicate-letter guesses need extra taps.

            int blankIndex = selectedTileIndexes.IndexOf(-1);
            if (blankIndex >= 0)
            {
                selectedTileIndexes[blankIndex] = tileIndex;
                RefreshTilePickerVisuals();
                Debug.Log($"[WordMaster] Guess now: '{GetWordMasterCurrentGuess()}'.");
                return;
            }

            if (selectedTileIndexes.Count >= GetWordMasterSelectionLimit())
            {
                SetFeedback("All open slots are filled.", badColor);
                return;
            }

            selectedTileIndexes.Add(tileIndex);
            RefreshTilePickerVisuals();
            Debug.Log($"[WordMaster] Guess now: '{GetWordMasterCurrentGuess()}'.");
            return;
        }

        if (selectedTileIndexes.Contains(tileIndex))
            selectedTileIndexes.Remove(tileIndex);
        else
            selectedTileIndexes.Add(tileIndex);

        RefreshTilePickerVisuals();
    }

    private void RemoveSelectedTileAt(int selectedIndex)
    {
        if (selectedIndex < 0 || selectedIndex >= selectedTileIndexes.Count)
            return;

        selectedTileIndexes.RemoveAt(selectedIndex);
        RefreshTilePickerVisuals();
    }

    private void TrimSelectedTilesFrom(int selectedIndex)
    {
        if (selectedIndex < 0 || selectedIndex >= selectedTileIndexes.Count)
            return;

        selectedTileIndexes.RemoveRange(selectedIndex, selectedTileIndexes.Count - selectedIndex);
        RefreshTilePickerVisuals();
    }

    private string GetSelectedTileWord()
    {
        StringBuilder builder = new StringBuilder();
        foreach (int tileIndex in selectedTileIndexes)
        {
            if (tileIndex < 0 || tileIndex >= currentSourceLetters.Length)
                continue;

            builder.Append(currentSourceLetters[tileIndex]);
        }

        return builder.ToString();
    }

    private void RefreshTilePickerVisuals()
    {
        RebuildCurrentWordTiles();
        RebuildSourceWordTiles();
    }

    private void RebuildCurrentWordTiles()
    {
        if (currentWordTilesRoot == null)
            return;

        if (currentMode == MinigameMode.WordMaster)
        {
            RebuildWordMasterRows();
            return;
        }

        ClearChildren(currentWordTilesRoot);

        if (selectedTileIndexes.Count == 0)
        {
            string placeholderText = currentMode == MinigameMode.WordMaster ? "Build guess" : "Current word";
            TextMeshProUGUI placeholder = CreateText(currentWordTilesRoot, "EmptyText", placeholderText, 22f, FontStyles.Italic, new Color(lightTextColor.r, lightTextColor.g, lightTextColor.b, 0.6f));
            placeholder.alignment = TextAlignmentOptions.Center;
            placeholder.gameObject.AddComponent<LayoutElement>().preferredWidth = 240f;
            return;
        }

        for (int i = 0; i < selectedTileIndexes.Count; i++)
        {
            int sourceIndex = selectedTileIndexes[i];
            if (sourceIndex < 0 || sourceIndex >= currentSourceLetters.Length)
                continue;

            int capturedIndex = i;
            if (currentMode == MinigameMode.WordExcavation)
                CreateLetterTile(currentWordTilesRoot, currentSourceLetters[sourceIndex], selectedButtonColor, () => TrimSelectedTilesFrom(capturedIndex));
            else
                CreateLetterTile(currentWordTilesRoot, currentSourceLetters[sourceIndex], selectedButtonColor, () => RemoveSelectedTileAt(capturedIndex));
        }
    }

    private void RebuildSourceWordTiles()
    {
        if (sourceWordTilesRoot == null)
            return;

        if (currentMode == MinigameMode.WordMaster)
        {
            RebuildWordMasterKeyboard();
            return;
        }

        ClearChildren(sourceWordTilesRoot);

        for (int i = 0; i < currentSourceLetters.Length; i++)
        {
            int capturedIndex = i;
            bool selected = currentMode != MinigameMode.WordMaster && selectedTileIndexes.Contains(i);
            Color tileColor = new Color(0.93f, 0.84f, 0.56f, 0.98f);

            if (currentMode == MinigameMode.WordMaster
                && wordMasterKeyboardColors.TryGetValue(currentSourceLetters[i], out Color clueColor))
            {
                tileColor = clueColor;
            }
            else if (selected)
            {
                tileColor = new Color(0.30f, 0.25f, 0.18f, 0.86f);
            }

            Button button = CreateLetterTile(sourceWordTilesRoot, currentSourceLetters[i], tileColor, () => ToggleSourceTile(capturedIndex));
            TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                bool darkWordMasterTile = currentMode == MinigameMode.WordMaster
                    && wordMasterKeyboardColors.ContainsKey(currentSourceLetters[i])
                    && ColorsApproximatelyEqual(tileColor, wordMasterMissingColor);

                text.color = selected
                    ? new Color(0.08f, 0.06f, 0.04f, 0.48f)
                    : darkWordMasterTile ? lightTextColor : textColor;
            }
        }
    }

    private Button CreateLetterTile(Transform parent, char letter, Color color, UnityEngine.Events.UnityAction action)
    {
        bool wordMasterKeyboardTile = currentMode == MinigameMode.WordMaster
            && sourceWordTilesRoot != null
            && parent.IsChildOf(sourceWordTilesRoot);

        Vector2 tileSize = wordMasterKeyboardTile ? new Vector2(58f, 44f) : new Vector2(58f, 50f);
        Button button = CreateButton(parent, "LetterTile_" + letter, letter.ToString(), color, tileSize, action);
        TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.fontSize = wordMasterKeyboardTile ? 30f : 30f;
            text.color = textColor;

            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.offsetMin = wordMasterKeyboardTile ? new Vector2(2f, 3f) : new Vector2(8f, 4f);
            textRect.offsetMax = wordMasterKeyboardTile ? new Vector2(-2f, -3f) : new Vector2(-8f, -4f);
        }

        return button;
    }

    private Color GetWordMasterTileTextColor(Color color)
    {
        return ColorsApproximatelyEqual(color, wordMasterMissingColor)
            ? lightTextColor
            : textColor;
    }

    private TextMeshProUGUI CreateLetterTileDisplay(Transform parent, char letter, Color color, Vector2 size, UnityEngine.Events.UnityAction clickAction = null, string objectName = null)
    {
        RectTransform rect = CreatePanel(parent, objectName ?? "LetterDisplay_" + (letter == ' ' ? "Empty" : letter.ToString()), color, size);
        LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
        layout.minWidth = size.x;
        layout.preferredWidth = size.x;
        layout.minHeight = size.y;
        layout.preferredHeight = size.y;

        if (clickAction != null)
        {
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            button.onClick.AddListener(clickAction);
        }

        Color labelColor = GetWordMasterTileTextColor(color);
        TextMeshProUGUI text = CreateText(rect, "Text", letter == ' ' ? string.Empty : letter.ToString(), 24f, FontStyles.Bold, labelColor);
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(2f, 1f);
        textRect.offsetMax = new Vector2(-2f, -1f);
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 12f;
        text.fontSizeMax = size.x >= 60f ? 30f : 24f;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }
}
