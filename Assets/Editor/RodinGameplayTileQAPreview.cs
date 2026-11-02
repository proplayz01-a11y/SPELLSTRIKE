#if UNITY_EDITOR
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Disposable live preview. Adds only runtime-only pickups/UI tiles to the
/// current Play Mode instance. Never modifies a scene or saved tutorial state.
/// </summary>
public static class RodinGameplayTileQAPreview
{
    [MenuItem("SPELLSTRIKE/Tiles/QA Load Stage 2 (Play Mode) %&2")]
    private static void LoadStage2()
    {
        if (Process.GetProcessesByName("VALORANT-Win64-Shipping").Length != 0 ||
            !EditorApplication.isPlaying)
        {
            UnityEngine.Debug.LogWarning("[Rodin QA] Play Mode required; VALORANT must be closed.");
            return;
        }
        SceneManager.LoadScene("Stage2Scene");
    }

    [MenuItem("SPELLSTRIKE/Tiles/QA Show Rodin World + UI Tiles (Play Mode) %&t")]
    private static void Preview()
    {
        if (Process.GetProcessesByName("VALORANT-Win64-Shipping").Length != 0)
        {
            UnityEngine.Debug.LogWarning("[Rodin QA] VALORANT is running; cancel preview.");
            return;
        }
        if (!EditorApplication.isPlaying)
        {
            UnityEngine.Debug.LogWarning("[Rodin QA] Enter Play Mode first.");
            return;
        }

        RodinTileCatalog catalog = RodinTileCatalog.Load();
        if (catalog == null || !catalog.IsComplete)
        {
            UnityEngine.Debug.LogError("[Rodin QA] 26-letter visual catalog incomplete.");
            return;
        }
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/3d_Tile.prefab");
        Camera camera = Camera.main;
        TileManager manager = Object.FindFirstObjectByType<TileManager>();
        if (prefab == null || camera == null || manager == null || manager.tilePoolPanel == null)
        {
            UnityEngine.Debug.LogError("[Rodin QA] Missing world prefab, camera or TileManager.");
            return;
        }

        Transform group = new GameObject("RodinV11_QA_RuntimeOnly").transform;
        char[] letters = { 'A', 'W', 'Q' };
        for (int i = 0; i < letters.Length; i++)
        {
            Vector3 position = camera.transform.position +
                               camera.transform.forward * 9f +
                               camera.transform.right * ((i - 1) * 3f) -
                               camera.transform.up * 1.4f;
            GameObject tile = Object.Instantiate(prefab, position, Quaternion.identity, group);
            tile.name = "RodinWorld_QA_" + letters[i];
            WorldTile pickup = tile.GetComponent<WorldTile>();
            if (pickup != null) pickup.SetLetter(letters[i]);
            // Never collect QA-only pickups or disturb the tutorial lesson.
            foreach (Collider c in tile.GetComponentsInChildren<Collider>(true))
                c.enabled = false;
            if (manager.CurrentMode == TilePoolMode.NormalCombat)
                manager.CreateTileInPool(letters[i]);
        }

        UnityEngine.Debug.Log(
            "[Rodin QA] Spawned Bronze A, Silver W, Gold Q(Qu) as world prefabs " +
            "and 3 temporary TileManager UI tiles. Scene=" +
            SceneManager.GetActiveScene().name + "; no save/commit.");
    }
}
#endif
