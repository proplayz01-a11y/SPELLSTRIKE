using UnityEngine;

public class TileStateDebugTester : MonoBehaviour
{
    private const int DebugDebuffCounter = 2;

    public Transform tilePoolPanel;

    private void Start()
    {
        ResolveTilePoolPanel();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            SetFirstActiveTile(TileState.Normal, 0, null);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            SetFirstActiveTile(TileState.Locked, DebugDebuffCounter, "[TileDebug] Set tile to Locked.");

        if (Input.GetKeyDown(KeyCode.Alpha3))
            SetFirstActiveTile(TileState.Cracked, DebugDebuffCounter, "[TileDebug] Set tile to Cracked.");

        if (Input.GetKeyDown(KeyCode.Alpha4))
            SetFirstActiveTile(TileState.Broken, 0, "[TileDebug] Set tile to Broken.");

        if (Input.GetKeyDown(KeyCode.Alpha5))
            ResetAllPoolTilesToNormal();
    }

    private void SetFirstActiveTile(TileState state, int counter, string logMessage)
    {
        Tile tile = GetFirstActiveTile();
        if (tile == null)
        {
            Debug.LogWarning("[TileDebug] No active tile found in TilePoolPanel.");
            return;
        }

        tile.SetTileState(state, counter);

        if (!string.IsNullOrEmpty(logMessage))
            Debug.Log(logMessage);
    }

    private Tile GetFirstActiveTile()
    {
        if (!ResolveTilePoolPanel())
            return null;

        foreach (Transform child in tilePoolPanel)
        {
            if (!child.gameObject.activeInHierarchy)
                continue;

            Tile tile = child.GetComponent<Tile>();
            if (tile != null)
                return tile;
        }

        return null;
    }

    private void ResetAllPoolTilesToNormal()
    {
        if (!ResolveTilePoolPanel())
            return;

        Tile[] tiles = tilePoolPanel.GetComponentsInChildren<Tile>(true);
        foreach (Tile tile in tiles)
        {
            if (tile == null)
                continue;

            if (!tile.gameObject.activeSelf)
                tile.gameObject.SetActive(true);

            tile.SetTileState(TileState.Normal);
        }

        Debug.Log("[TileDebug] Reset all tiles to Normal.");
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

        Debug.LogWarning("[TileDebug] TilePoolPanel not found.");
        return false;
    }
}
