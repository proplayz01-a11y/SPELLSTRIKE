using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TileManager : MonoBehaviour
{
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

    // vowels more likely
    private char[] weightedAlphabet = new char[]
    {
    'A','A','E','E','E','I','I','O','O','O','U','U', // vowels x3
    'B','C','D','F','G','H','J','K','L','M','N','P','Q','R','S','T','V','W','X','Y','Z'
    };
    private GridLayoutGroup tilePoolGrid;
    private GridLayoutGroup wordBarGrid;
    public GameObject tileUIPrefab;

    private void Awake()
    {
        tilePoolGrid = tilePoolPanel.GetComponent<GridLayoutGroup>();
        wordBarGrid = wordBarPanel.GetComponent<GridLayoutGroup>();
    }

    private void Start()
    {
        StartCoroutine(InitAfterLayout());
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
        List<char> lettersToSpawn = new List<char>();

        // Per-stage long word probability
        float longWordChance;
        if (stage <= 2) longWordChance = 0.70f;
        else if (stage <= 4) longWordChance = 0.80f;
        else longWordChance = 0.85f; // Stage 5

        bool injectLongWord = Random.value < longWordChance;
        if (injectLongWord)
        {
            string longWord = DictionaryManager.Instance.GetRandomLongWord(8, 13);
            if (!string.IsNullOrEmpty(longWord))
            {
                foreach (char c in longWord)
                    lettersToSpawn.Add(c);
            }
        }

        while (lettersToSpawn.Count < amount)
        {
            char randomLetter = weightedAlphabet[Random.Range(0, weightedAlphabet.Length)];
            lettersToSpawn.Add(randomLetter);
        }

        // Shuffle
        for (int i = 0; i < lettersToSpawn.Count; i++)
        {
            int randIndex = Random.Range(0, lettersToSpawn.Count);
            char temp = lettersToSpawn[i];
            lettersToSpawn[i] = lettersToSpawn[randIndex];
            lettersToSpawn[randIndex] = temp;
        }

        foreach (char letter in lettersToSpawn)
            CreateTileInPool(letter);
    }

    public void ToggleTilePanel(Tile tile)
    {
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
        tileScript.SetLetter(letter);

        // Assign the sprite for the letter
        int index = letter - 'A';
        if (index >= 0 && index < letterSpritesUI.Length)
        {
            Image img = tileObj.GetComponent<Image>();
            if (img != null)
                img.sprite = letterSpritesUI[index];
        }

        SetTilePoolPanelGrid();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            CleanupTilePoolPanel();
        }
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
        float spacing = 0.15f;
        float cellHeight = panelHeight - 10f;
        float cellWidth;

        if (tileCount <= 11)
        {
            cellWidth = 76.5f;
        }
        else
        {
            // manually calculated for 12-16 tiles
            cellWidth = Mathf.Lerp(76.5f, 56.8f, (tileCount - 11f) / 5f);
            // smoothly scales from 76.5 down to 45 as tiles go from 12 to 16
        }

        wordBarGrid.cellSize = new Vector2(cellWidth, cellHeight);
        wordBarGrid.spacing = new Vector2(spacing, spacing);
        wordBarGrid.padding.left = 10;
        wordBarGrid.padding.right = 10;
        wordBarGrid.padding.top = 5;
        wordBarGrid.padding.bottom = 5;
        wordBarGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        wordBarGrid.constraintCount = tileCount;
        wordBarGrid.childAlignment = TextAnchor.MiddleCenter;
    }
}
