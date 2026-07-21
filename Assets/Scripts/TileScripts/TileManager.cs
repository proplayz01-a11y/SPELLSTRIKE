using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TileManager : MonoBehaviour
{
    private readonly HashSet<char> difficultLetters = new HashSet<char>() { 'Q', 'X', 'Z', 'J', 'K', 'V', 'W', 'Y' };
    [Header("Panels")]
    public Transform tilePoolPanel;
    public Transform wordBarPanel;
    public GameObject tileButtonPrefab;

    [Header("Gameplay")]
    public List<Tile> selectedTiles = new List<Tile>();
    public List<Tile> wordBarTiles = new List<Tile>();
    public Sprite[] letterSpritesUI; // 26 sprites, drag them in inspector from A-Z

    [Header("Stage Settings")]
    public int currentStage = 1; // set this per stage scene

    public AttackController attackController;
    private GridLayoutGroup tilePoolGrid;
    private GridLayoutGroup wordBarGrid;
    public GameObject tileUIPrefab;
    private readonly Dictionary<Tile, float> wordBarBaseTmpFontSizes = new Dictionary<Tile, float>();
    private readonly Dictionary<Tile, int> wordBarBaseLegacyFontSizes = new Dictionary<Tile, int>();

    [Header("Word Bar Adaptive Layout")]
    public float wordBarBaseCellWidth = 76.5f;
    public float wordBarMinCellWidth = 45f;
    public float wordBarSpacing = 0.15f;
    public int wordBarPaddingLeft = 10;
    public int wordBarPaddingRight = 10;
    public int wordBarPaddingTop = 5;
    public int wordBarPaddingBottom = 5;
    [Range(0.5f, 1f)] public float wordBarMinFontScale = 0.72f;

    private void Awake()
    {
        tilePoolGrid = tilePoolPanel.GetComponent<GridLayoutGroup>();
        wordBarGrid = wordBarPanel.GetComponent<GridLayoutGroup>();
    }

    private void Start()
    {
        StartCoroutine(InitAfterLayout());
        List<char> testRefill = GenerateRefillLettersFromBudget(11);
        Debug.Log("Test refill letters: " + string.Join(", ", testRefill));
    }

    private IEnumerator InitAfterLayout()
    {
        yield return null; // wait one frame
        SetTilePoolPanelGrid();
        SetWordBarPanelGrid();
        PrefillTilePool(16, currentStage);
        CleanupTilePoolPanel();
    }

    void PrefillTilePool(int amount, int stage = 1)
    {
        List<char> lettersToSpawn = GenerateDictionaryWeightedLetters(amount, stage);
     
        foreach (char letter in lettersToSpawn)
            CreateTileInPool(letter);
    }

    public void ToggleTilePanel(Tile tile)
    {
        if (tile == null || !tile.IsSelectable())
            return;

        // FROM TILE POOL → WORD BAR
        if (tile.transform.parent == tilePoolPanel)
        {
            MoveTile(tile, wordBarPanel);

            if (!selectedTiles.Contains(tile))
                selectedTiles.Add(tile);

            if (!wordBarTiles.Contains(tile))
                wordBarTiles.Add(tile);
        }
        // FROM WORD BAR → TILE POOL
        else if (tile.transform.parent == wordBarPanel)
        {
            int index = selectedTiles.IndexOf(tile);

            if (index >= 0)
            {
                for (int i = selectedTiles.Count - 1; i >= index; i--)
                {
                    Tile t = selectedTiles[i];
                    MoveTile(t, tilePoolPanel);

                    selectedTiles.RemoveAt(i);
                    wordBarTiles.Remove(t);
                }
            }
        }

        SetWordBarPanelGrid();

        if (attackController != null)
            attackController.CheckWord();

        Debug.Log("Tiles in wordBar: " + wordBarTiles.Count);
    }

    private void MoveTile(Tile tile, Transform newParent)
    {
        tile.transform.SetParent(newParent, false);
        tile.transform.localScale = Vector3.one;

        RectTransform rect = tile.GetComponent<RectTransform>();
        rect.anchoredPosition = Vector2.zero;
    }

    public string GetCurrentWord()
    {
        string word = "";
        foreach (Tile t in selectedTiles)
            word += t.letter.ToString();
        return word;
    }

    public void ClearWordBar()
    {
        foreach (Tile t in selectedTiles)
        {
            MoveTile(t, tilePoolPanel);
        }

        selectedTiles.Clear();

        if (attackController != null)
            attackController.CheckWord();
    }

    public void DestroyUsedTiles()
    {
        foreach (Tile tile in wordBarTiles)
        {
            Destroy(tile.gameObject);
        }

        wordBarTiles.Clear();
        selectedTiles.Clear();

        if (attackController != null)
            attackController.CheckWord();
    }

    public int GetWordTileCount()
    {
        return wordBarTiles.Count;
    }

    public void CreateTileInPool(char letter)
    {
        GameObject tileObj = Instantiate(tileUIPrefab, tilePoolPanel);
        tileObj.transform.localScale = Vector3.one;

        Tile tileScript = tileObj.GetComponent<Tile>();
        Button button = tileObj.GetComponent<Button>();
        if (button == null)
            button = tileObj.AddComponent<Button>();

        Image img = tileObj.GetComponent<Image>();
        if (img != null)
        {
            img.raycastTarget = true;
            button.targetGraphic = img;
        }

        if (tileScript != null)
            tileScript.button = button;

        tileScript.SetLetter(letter);

        // Assign the sprite for the letter
        int index = letter - 'A';
        if (index >= 0 && index < letterSpritesUI.Length)
        {
            if (img != null)
                img.sprite = letterSpritesUI[index];
        }

        SetTilePoolPanelGrid();
    }

    public void AddTilesToPool(int count)
    {
        List<char> lettersToSpawn = GenerateDictionaryWeightedLetters(count, currentStage);
        for (int i = 0; i < lettersToSpawn.Count; i++)
        {
            CreateTileInPool(lettersToSpawn[i]);
        }
    }

    private List<char> GenerateDictionaryWeightedLetters(int amount, int stage)
    {
        List<char> finalLetters = new List<char>(amount);

        if (DictionaryManager.Instance == null)
        {
            Debug.LogError("DictionaryManager is missing. Cannot run dictionary-based tile spawning.");
            return finalLetters;
        }

        int safety = 0;

        while (safety < 100)
        {
            string mainWord = GetPlayableWordInRange(9, 13);
            if (string.IsNullOrEmpty(mainWord))
            {
                safety++;
                continue;
            }

            int remainder = amount - mainWord.Length;
            if (remainder < 0)
            {
                safety++;
                continue;
            }

            string remainderWord = "";

            if (remainder > 0)
            {
                remainderWord = GetPlayableWordInRange(remainder, remainder);
                if (string.IsNullOrEmpty(remainderWord))
                {
                    safety++;
                    continue;
                }
            }

            Debug.Log($"Main word: {mainWord} | Remainder word: {remainderWord}");

            foreach (char c in mainWord)
            {
                if (char.IsLetter(c))
                    finalLetters.Add(char.ToUpperInvariant(c));
            }

            foreach (char c in remainderWord)
            {
                if (char.IsLetter(c) && finalLetters.Count < amount)
                    finalLetters.Add(char.ToUpperInvariant(c));
            }

            break;
        }



        if (finalLetters.Count < amount)
        {
            Debug.LogWarning($"Only generated {finalLetters.Count}/{amount} dictionary-based letters.");
        }

        for (int i = 0; i < finalLetters.Count; i++)
        {
            int randIndex = Random.Range(i, finalLetters.Count);
            char temp = finalLetters[i];
            finalLetters[i] = finalLetters[randIndex];
            finalLetters[randIndex] = temp;
        }

        return finalLetters;
    }
    private string GetPlayableWordInRange(int minLength, int maxLength)
    {
        int safety = 0;

        while (safety < 100)
        {
            string word = DictionaryManager.Instance.GetRandomWordByLengthRange(minLength, maxLength);

            if (!string.IsNullOrEmpty(word) && IsPlayableWord(word))
            {
                return word.ToUpperInvariant();
            }

            safety++;
        }

        return "";
    }
    private bool IsPlayableWord(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
            return false;

        word = word.ToUpperInvariant();

        if (word.Length < 3)
            return false;

        int vowelCount = 0;
        int rareCount = 0;
        Dictionary<char, int> charCounts = new Dictionary<char, int>();

        foreach (char c in word)
        {
            if (!char.IsLetter(c))
                return false;

            if ("AEIOU".Contains(c))
                vowelCount++;

            if ("QXZJVWKY".Contains(c))
                rareCount++;

            if (!charCounts.ContainsKey(c))
                charCounts[c] = 0;

            charCounts[c]++;
        }

        float vowelRatio = (float)vowelCount / word.Length;

        // Must have enough vowels
        if (vowelCount == 0)
            return false;

        if (word.Length <= 4 && vowelCount < 1)
            return false;

        if (word.Length >= 5 && vowelCount < 2)
            return false;

        // Too consonant-heavy
        if (vowelRatio < 0.30f)
            return false;

        // Too many ugly letters
        if (rareCount >= 2)
            return false;

        // Reject words with any letter repeated too many times
        foreach (var kvp in charCounts)
        {
            if (kvp.Value >= 3)
                return false;
        }

        return true;
    }
    private string GetLongestPlayableWordFitting(int budget)
    {
        for (int length = budget; length >= 3; length--)
        {
            string word = GetPlayableWordInRange(length, length);
            if (!string.IsNullOrEmpty(word))
                return word;
        }

        return "";
    }

    private Vector3 GetStageLengthWeights(int stage)
    {
        if (stage <= 1) return new Vector3(0.70f, 0.20f, 0.10f);
        if (stage == 2) return new Vector3(0.62f, 0.25f, 0.13f);
        if (stage == 3) return new Vector3(0.55f, 0.28f, 0.17f);
        if (stage == 4) return new Vector3(0.48f, 0.31f, 0.21f);

        // Stage 5+: most balanced while keeping short > medium > long.
        return new Vector3(0.42f, 0.33f, 0.25f);
    }

    private string TakeLettersFromWord(string word, int budget)
    {
        if (string.IsNullOrEmpty(word) || budget <= 0)
            return "";

        int takeCount = Mathf.Min(word.Length, budget);
        return word.Substring(0, takeCount).ToUpperInvariant();
    }

    public List<char> GenerateRefillLettersFromBudget(int budget)
    {
        List<char> refillLetters = new List<char>();

        if (DictionaryManager.Instance == null)
        {
            Debug.LogError("DictionaryManager is missing. Cannot generate refill letters.");
            return refillLetters;
        }

        int remaining = budget;
        string word1 = "";
        string word2 = "";

        // Word 1
        word1 = GetLongestPlayableWordFitting(remaining);
        if (!string.IsNullOrEmpty(word1))
        {
            string letters1 = TakeLettersFromWord(word1, remaining);
            foreach (char c in letters1)
                refillLetters.Add(c);

            remaining -= letters1.Length;
        }

        // Word 2
        if (remaining > 0)
        {
            word2 = GetLongestPlayableWordFitting(remaining);
            if (!string.IsNullOrEmpty(word2))
            {
                string letters2 = TakeLettersFromWord(word2, remaining);
                foreach (char c in letters2)
                    refillLetters.Add(c);

                remaining -= letters2.Length;
            }
        }

        Debug.Log($"[NEXT REFILL] Used Budget: {budget} | Word1: {word1} | Word2: {word2} | Spawned: {string.Join(", ", refillLetters)} | Filled: {budget - remaining}/{budget}");

        if (remaining > 0)
            Debug.LogWarning($"[REFILL] Underfill: {remaining} slots unfilled.");

        return refillLetters;
    }

    public void ResetTileSelection()
    {
        foreach (Tile t in selectedTiles)
        {
            MoveTile(t, tilePoolPanel);
        }

        selectedTiles.Clear();
        wordBarTiles.Clear();
        SetWordBarPanelGrid();

        if (attackController != null)
            attackController.CheckWord();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            CleanupTilePoolPanel();
        }
    }

    public bool EnsureWordLettersAvailable(string targetWord, int maximumPoolTiles = 16)
    {
        if (string.IsNullOrWhiteSpace(targetWord) || tilePoolPanel == null)
            return false;

        targetWord = targetWord.Trim().ToUpperInvariant();
        Dictionary<char, int> requiredCounts = BuildLetterCounts(targetWord);
        List<Tile> poolTiles = new List<Tile>();

        foreach (Transform child in tilePoolPanel)
        {
            Tile tile = child.GetComponent<Tile>();
            if (tile != null && tile.gameObject.activeInHierarchy && tile.IsSelectable())
                poolTiles.Add(tile);
        }
        int currentPoolTileCount = poolTiles.Count;

        Dictionary<char, int> availableCounts = new Dictionary<char, int>();
        foreach (Tile tile in poolTiles)
            IncrementCount(availableCounts, char.ToUpperInvariant(tile.letter));

        List<char> missingLetters = new List<char>();
        foreach (KeyValuePair<char, int> required in requiredCounts)
        {
            availableCounts.TryGetValue(required.Key, out int available);
            for (int i = available; i < required.Value; i++)
                missingLetters.Add(required.Key);
        }

        foreach (char missingLetter in missingLetters)
        {
            Tile replacement = FindReplacementTile(poolTiles, availableCounts, requiredCounts);
            if (replacement != null)
            {
                char previousLetter = char.ToUpperInvariant(replacement.letter);
                availableCounts[previousLetter] = Mathf.Max(0, availableCounts[previousLetter] - 1);
                SetTileLetterAndSprite(replacement, missingLetter);
                IncrementCount(availableCounts, missingLetter);
                poolTiles.Remove(replacement);
                continue;
            }

            if (currentPoolTileCount < maximumPoolTiles)
            {
                CreateTileInPool(missingLetter);
                IncrementCount(availableCounts, missingLetter);
                currentPoolTileCount++;
                continue;
            }

            Debug.LogWarning($"[TileManager] Could not guarantee all letters for {targetWord}. No safe replacement tile remained.", this);
            ShuffleTilePoolOrder();
            return false;
        }

        ShuffleTilePoolOrder();
        Debug.Log($"[TileManager] Guaranteed shuffled target letters for {targetWord} without displaying their order.", this);
        return true;
    }

    private Dictionary<char, int> BuildLetterCounts(string value)
    {
        Dictionary<char, int> counts = new Dictionary<char, int>();
        foreach (char character in value)
        {
            if (char.IsLetter(character))
                IncrementCount(counts, char.ToUpperInvariant(character));
        }
        return counts;
    }

    private void IncrementCount(Dictionary<char, int> counts, char character)
    {
        if (!counts.ContainsKey(character))
            counts[character] = 0;
        counts[character]++;
    }

    private Tile FindReplacementTile(
        List<Tile> candidates,
        Dictionary<char, int> availableCounts,
        Dictionary<char, int> requiredCounts)
    {
        foreach (Tile candidate in candidates)
        {
            char candidateLetter = char.ToUpperInvariant(candidate.letter);
            availableCounts.TryGetValue(candidateLetter, out int available);
            requiredCounts.TryGetValue(candidateLetter, out int required);
            if (available > required)
                return candidate;
        }

        return null;
    }

    private void SetTileLetterAndSprite(Tile tile, char letter)
    {
        if (tile == null)
            return;

        letter = char.ToUpperInvariant(letter);
        tile.SetLetter(letter);

        Image image = tile.GetComponent<Image>();
        int spriteIndex = letter - 'A';
        if (image != null && letterSpritesUI != null && spriteIndex >= 0 && spriteIndex < letterSpritesUI.Length)
            image.sprite = letterSpritesUI[spriteIndex];
    }

    private void ShuffleTilePoolOrder()
    {
        if (tilePoolPanel == null)
            return;

        List<Transform> children = new List<Transform>();
        foreach (Transform child in tilePoolPanel)
            children.Add(child);

        for (int i = children.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            Transform temporary = children[i];
            children[i] = children[randomIndex];
            children[randomIndex] = temporary;
        }

        for (int i = 0; i < children.Count; i++)
            children[i].SetSiblingIndex(i);

        SetTilePoolPanelGrid();
    }

    private void CleanupTilePoolPanel()
    {
        
        foreach (Transform child in tilePoolPanel)
        {
            MeshRenderer mr = child.GetComponent<MeshRenderer>();
            if (mr != null || child.name.Contains("3d_Tile"))
            {
                Debug.LogWarning("Removing child: " + child.name);
                Destroy(child.gameObject);
            }
            else
            {
                //Debug.Log("Keeping child: " + child.name);
            }

        }
    }

    private void SetTilePoolPanelGrid()
    {
        if (tilePoolGrid == null) return;

        RectTransform rect = tilePoolPanel.GetComponent<RectTransform>();
        float panelWidth = rect.rect.width;
        float panelHeight = rect.rect.height;

        int columns = 8;
        int rows = 2;
        float spacing = 5f;

        float cellWidth = (panelWidth - (spacing * (columns - 1)) - 20f) / columns;
        float cellHeight = (panelHeight - (spacing * (rows - 1)) - 10f) / rows;

        tilePoolGrid.cellSize = new Vector2(cellWidth, cellHeight);
        tilePoolGrid.spacing = new Vector2(spacing, spacing);
        tilePoolGrid.padding.left = 10;
        tilePoolGrid.padding.right = 10;
        tilePoolGrid.padding.top = 5;
        tilePoolGrid.padding.bottom = 5;
        tilePoolGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        tilePoolGrid.constraintCount = columns;
    }

    private void SetWordBarPanelGrid()
    {
        if (wordBarGrid == null) return;

        RectTransform rect = wordBarPanel.GetComponent<RectTransform>();
        float panelWidth = rect.rect.width;
        float panelHeight = rect.rect.height;

        int tileCount = Mathf.Max(1, wordBarTiles.Count);
        float spacing = wordBarSpacing;
        float cellHeight = panelHeight - (wordBarPaddingTop + wordBarPaddingBottom);

        float usableWidth = panelWidth - (wordBarPaddingLeft + wordBarPaddingRight);
        float maxWidthPerTile = (usableWidth - (spacing * (tileCount - 1))) / tileCount;
        float cellWidth = Mathf.Clamp(maxWidthPerTile, wordBarMinCellWidth, wordBarBaseCellWidth);

        wordBarGrid.cellSize = new Vector2(cellWidth, cellHeight);
        wordBarGrid.spacing = new Vector2(spacing, spacing);
        wordBarGrid.padding.left = wordBarPaddingLeft;
        wordBarGrid.padding.right = wordBarPaddingRight;
        wordBarGrid.padding.top = wordBarPaddingTop;
        wordBarGrid.padding.bottom = wordBarPaddingBottom;
        wordBarGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        wordBarGrid.constraintCount = tileCount;
        wordBarGrid.childAlignment = TextAnchor.MiddleCenter;

        ApplyAdaptiveWordBarTextScale(cellWidth);
    }

    private void ApplyAdaptiveWordBarTextScale(float currentCellWidth)
    {
        if (wordBarTiles == null || wordBarTiles.Count == 0) return;

        float scale = Mathf.Clamp(currentCellWidth / Mathf.Max(1f, wordBarBaseCellWidth), wordBarMinFontScale, 1f);

        foreach (Tile tile in wordBarTiles)
        {
            if (tile == null) continue;

            if (tile.textTMP != null)
            {
                if (!wordBarBaseTmpFontSizes.ContainsKey(tile))
                    wordBarBaseTmpFontSizes[tile] = tile.textTMP.fontSize;

                tile.textTMP.fontSize = wordBarBaseTmpFontSizes[tile] * scale;
            }

            if (tile.letterText != null)
            {
                if (!wordBarBaseLegacyFontSizes.ContainsKey(tile))
                    wordBarBaseLegacyFontSizes[tile] = tile.letterText.fontSize;

                tile.letterText.fontSize = Mathf.RoundToInt(wordBarBaseLegacyFontSizes[tile] * scale);
            }
        }
    }
}
