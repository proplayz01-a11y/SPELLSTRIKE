using System.Collections.Generic;
using UnityEngine;

public class TileDebuffManager : MonoBehaviour
{
    [Header("References")]
    public Transform tilePoolPanel;

    [Header("Stage Debuff Settings")]
    public int defaultCounter = 2;
    public int maxBrokenTiles = 2;
    public int minimumVisibleTiles = 8;

    [Header("Temporary Debug Keys")]
    public bool enableDebugKeys = true;
    public KeyCode debugLockKey = KeyCode.L;
    public KeyCode debugCrackKey = KeyCode.C;
    public KeyCode debugReduceCountersKey = KeyCode.V;
    public KeyCode debugRestoreBrokenKey = KeyCode.R;

    private void Awake()
    {
        ResolveTilePoolPanel();
    }

    private void Update()
    {
        if (!enableDebugKeys)
            return;

        if (Input.GetKeyDown(debugLockKey))
            ApplyTileLocking(Random.Range(1, 3), defaultCounter);

        if (Input.GetKeyDown(debugCrackKey))
            ApplyTileCracking(1, defaultCounter);

        if (Input.GetKeyDown(debugReduceCountersKey))
            ReduceTileDebuffCountersOnSuccessfulAttack();

        if (Input.GetKeyDown(debugRestoreBrokenKey))
            RestoreBrokenTilesAfterEnemyDefeat();
    }

    public void ApplyTileLocking()
    {
        ApplyTileLocking(1, defaultCounter);
    }

    public void ApplyTileLocking(int tileCount, int counter)
    {
        int targetCount = Mathf.Clamp(tileCount, 0, 2);
        int resolvedCounter = ResolveCounter(counter);
        List<Tile> eligibleTiles = GetActiveTilesByState(TileState.Normal);
        int lockedCount = 0;

        for (int i = 0; i < targetCount && eligibleTiles.Count > 0; i++)
        {
            Tile tile = TakeRandomTile(eligibleTiles);
            tile.SetTileState(TileState.Locked, resolvedCounter);
            lockedCount++;
        }

        Debug.Log($"[TileDebuff] Locked {lockedCount} tile(s).");
    }

    public void ApplyTileCracking()
    {
        ApplyTileCracking(1, defaultCounter);
    }

    public void ApplyTileCracking(int tileCount, int counter)
    {
        int targetCount = Mathf.Clamp(tileCount, 0, 1);
        int resolvedCounter = ResolveCounter(counter);
        int crackedCount = 0;

        for (int i = 0; i < targetCount; i++)
        {
            List<Tile> eligibleTiles = GetCrackingEligibleTiles();
            if (eligibleTiles.Count == 0)
                break;

            Tile tile = TakeRandomTile(eligibleTiles);
            if (tile.CurrentState == TileState.Normal)
            {
                tile.SetTileState(TileState.Cracked, resolvedCounter);
                crackedCount++;
            }
            else if (tile.CurrentState == TileState.Cracked && CanBreakAnotherTile())
            {
                tile.SetTileState(TileState.Broken);
                Debug.Log("[TileDebuff] Tile broke.");
            }
        }

        Debug.Log($"[TileDebuff] Cracked {crackedCount} tile(s).");
    }

    public void ReduceTileDebuffCountersOnSuccessfulAttack()
    {
        foreach (Tile tile in GetActiveTileButtons())
        {
            if (tile.CurrentState != TileState.Locked && tile.CurrentState != TileState.Cracked)
                continue;

            int nextCounter = Mathf.Max(0, tile.DebuffCounter - 1);
            if (nextCounter == 0)
                tile.SetTileState(TileState.Normal);
            else
                tile.SetDebuffCounter(nextCounter);
        }

        Debug.Log("[TileDebuff] Counters reduced after successful attack.");
    }

    public void RestoreBrokenTilesAfterEnemyDefeat()
    {
        foreach (Tile tile in GetAllTileButtons())
        {
            if (tile.CurrentState != TileState.Broken)
                continue;

            if (!tile.gameObject.activeSelf)
                tile.gameObject.SetActive(true);

            tile.SetTileState(TileState.Normal);
        }

        Debug.Log("[TileDebuff] Broken tiles restored.");
    }

    public int ClearAllTileDebuffs(bool restoreBrokenTiles = true)
    {
        int clearedCount = 0;

        foreach (Tile tile in GetAllTileButtons())
        {
            if (tile.CurrentState != TileState.Locked
                && tile.CurrentState != TileState.Cracked
                && tile.CurrentState != TileState.Broken)
            {
                continue;
            }

            if (tile.CurrentState == TileState.Broken && restoreBrokenTiles && !tile.gameObject.activeSelf)
                tile.gameObject.SetActive(true);

            if (tile.CurrentState != TileState.Broken || restoreBrokenTiles)
            {
                tile.SetTileState(TileState.Normal);
                clearedCount++;
            }
        }

        Debug.Log($"[TileDebuff] Cleared {clearedCount} tile debuff(s).");
        return clearedCount;
    }

    public int CountActiveTileDebuffs(bool includeBrokenTiles = true)
    {
        int debuffCount = 0;

        foreach (Tile tile in GetAllTileButtons())
        {
            if (tile.CurrentState == TileState.Locked || tile.CurrentState == TileState.Cracked)
                debuffCount++;
            else if (includeBrokenTiles && tile.CurrentState == TileState.Broken)
                debuffCount++;
        }

        return debuffCount;
    }

    private List<Tile> GetActiveTileButtons()
    {
        List<Tile> tiles = new List<Tile>();
        if (!ResolveTilePoolPanel())
            return tiles;

        foreach (Transform child in tilePoolPanel)
        {
            if (!child.gameObject.activeInHierarchy)
                continue;

            Tile tile = child.GetComponent<Tile>();
            if (tile != null)
                tiles.Add(tile);
        }

        return tiles;
    }

    private List<Tile> GetAllTileButtons()
    {
        List<Tile> tiles = new List<Tile>();
        if (!ResolveTilePoolPanel())
            return tiles;

        tilePoolPanel.GetComponentsInChildren(true, tiles);
        return tiles;
    }

    private List<Tile> GetActiveTilesByState(TileState state)
    {
        List<Tile> matchingTiles = new List<Tile>();
        foreach (Tile tile in GetActiveTileButtons())
        {
            if (tile.CurrentState == state)
                matchingTiles.Add(tile);
        }

        return matchingTiles;
    }

    private List<Tile> GetCrackingEligibleTiles()
    {
        List<Tile> eligibleTiles = new List<Tile>();
        bool canBreakTile = CanBreakAnotherTile();

        foreach (Tile tile in GetActiveTileButtons())
        {
            if (tile.CurrentState == TileState.Normal)
                eligibleTiles.Add(tile);
            else if (tile.CurrentState == TileState.Cracked && canBreakTile)
                eligibleTiles.Add(tile);
        }

        return eligibleTiles;
    }

    private Tile TakeRandomTile(List<Tile> tiles)
    {
        int index = Random.Range(0, tiles.Count);
        Tile tile = tiles[index];
        tiles.RemoveAt(index);
        return tile;
    }

    private bool CanBreakAnotherTile()
    {
        return CountBrokenTiles() < maxBrokenTiles && CountVisibleTiles() > minimumVisibleTiles;
    }

    private int CountBrokenTiles()
    {
        int brokenCount = 0;
        foreach (Tile tile in GetAllTileButtons())
        {
            if (tile.CurrentState == TileState.Broken)
                brokenCount++;
        }

        return brokenCount;
    }

    private int CountVisibleTiles()
    {
        int visibleCount = 0;
        foreach (Tile tile in GetActiveTileButtons())
        {
            if (tile.gameObject.activeInHierarchy)
                visibleCount++;
        }

        return visibleCount;
    }

    private int ResolveCounter(int counter)
    {
        return counter > 0 ? counter : defaultCounter;
    }

    private bool ResolveTilePoolPanel()
    {
        if (tilePoolPanel != null)
            return true;

        TileManager tileManager = Object.FindAnyObjectByType<TileManager>();
        if (tileManager != null && tileManager.tilePoolPanel != null)
        {
            tilePoolPanel = tileManager.tilePoolPanel;
            return true;
        }

        GameObject tilePoolObject = GameObject.Find("TilePoolPanel");
        if (tilePoolObject != null)
        {
            tilePoolPanel = tilePoolObject.transform;
            return true;
        }

        Debug.LogWarning("[TileDebuff] TilePoolPanel not found.");
        return false;
    }
}
