using System.Collections.Generic;
using UnityEngine;

public class TileSpawner : MonoBehaviour
{
    public event System.Action<IReadOnlyList<WorldTile>> SpecificTilesSpawned;

    public static TileSpawner Instance;

    public GameObject worldTilePrefab;
    public Transform tilePoolPanel;

    [Header("Arena Spawn Area")]
    public Transform arenaCenter;
    public float arenaRadius = 10f;
    public float spawnY = 0f;

    [Header("Spawn Filtering")]
    public LayerMask groundLayer;
    public Transform shrineCenter;
    public float shrineExclusionRadius = 3f;
    [SerializeField, Min(1)] private int maximumSpawnAttemptsPerTile = 24;
    [SerializeField, Min(0f)] private float groundSurfaceOffset = 0.1f;
    [Tooltip("Reject spawn surfaces that are implausibly far above/below the active arena floor. Prevents props/ruins on the Ground layer from becoming tile spawn surfaces.")]
    [SerializeField, Min(0.5f)] private float maximumSurfaceHeightDeltaFromArenaCenter = 8f;
    [SerializeField] private bool allowDefaultLayerSpawnSurfaceFallback = true;

    [Header("Startup")]
    [SerializeField] private bool spawnInitialTilesInPool = true;

    [Header("Replacement Budget")]
    [SerializeField, Min(1)] private int maximumNormalCombatTileBudget = 16;

    private char[] alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
    private readonly HashSet<WorldTile> pendingReplacementTiles = new HashSet<WorldTile>();

    public int PendingReplacementTileCount
    {
        get
        {
            pendingReplacementTiles.RemoveWhere(tile => tile == null);
            return pendingReplacementTiles.Count;
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (!spawnInitialTilesInPool)
            return;

        // TileManager is the sole owner of the playable UI Tile Pool. This
        // spawner owns only world replacements once combat consumes UI tiles.
        TileManager tileManager = FindFirstObjectByType<TileManager>();
        if (tileManager != null && tileManager.prefillTilePoolOnStart)
        {
            Debug.Log(
                "[TileSpawner] Startup UI-pool spawn skipped because TileManager owns Normal Combat prefill.",
                this);
            return;
        }

        SpawnTilesInPool(16);
    }



    public void ConfigureArenaBounds(Transform center, float radius, Transform shrine, float exclusionRadius)
    {
        ConfigureArenaBounds(center, radius, shrine, exclusionRadius, groundLayer);
    }

    public void ConfigureArenaBounds(
        Transform center,
        float radius,
        Transform shrine,
        float exclusionRadius,
        LayerMask spawnSurfaceLayers)
    {
        if (center != null)
            arenaCenter = center;

        arenaRadius = Mathf.Max(0.5f, radius);

        shrineCenter = shrine;
        shrineExclusionRadius = Mathf.Max(0f, exclusionRadius);

        if (spawnSurfaceLayers.value != 0)
            groundLayer = spawnSurfaceLayers;

        Debug.Log(
            $"[TileSpawner] Configured arena: {(arenaCenter != null ? arenaCenter.name : "NULL")} | " +
            $"Radius: {arenaRadius} | " +
            $"Shrine: {(shrineCenter != null ? shrineCenter.name : "NULL")} | " +
            $"Exclusion: {shrineExclusionRadius} | " +
            $"Surface Mask: {groundLayer.value}"
        );
    }

    private bool TryGetRandomSpawnPosition(out Vector3 spawnPosition)
    {
        spawnPosition = default;

        if (arenaCenter == null)
        {
            Debug.LogError("[TileSpawner] Cannot spawn world tiles because Arena Center is not assigned.", this);
            return false;
        }

        if (groundLayer.value == 0)
        {
            Debug.LogError("[TileSpawner] Cannot spawn world tiles because Ground Layer is empty.", this);
            return false;
        }

        int attempts = Mathf.Max(1, maximumSpawnAttemptsPerTile);
        Vector3 centerPosition = arenaCenter.position;

        for (int attempt = 0; attempt < attempts; attempt++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * arenaRadius;
            Vector3 candidate = centerPosition + new Vector3(randomCircle.x, 0f, randomCircle.y);
            Vector3 rayOrigin = new Vector3(candidate.x, centerPosition.y + 500f, candidate.z);

            if (!Physics.Raycast(
                    rayOrigin,
                    Vector3.down,
                    out RaycastHit hit,
                    1000f,
                    groundLayer,
                    QueryTriggerInteraction.Ignore))
            {
                // The optimized Stage 1 GLB keeps its imported walkable map on
                // Unity's Default layer. Preserve the authored Ground mask first,
                // then fall back to Default only when Ground finds nothing.
                // This fixes replacement tiles without changing the user's map
                // collider or broadening the primary spawn mask.
                int defaultLayerMask = 1 << LayerMask.NameToLayer("Default");
                if (!allowDefaultLayerSpawnSurfaceFallback ||
                    !Physics.Raycast(
                        rayOrigin,
                        Vector3.down,
                        out hit,
                        1000f,
                        defaultLayerMask,
                        QueryTriggerInteraction.Ignore))
                    continue;
            }

            if (Mathf.Abs(hit.point.y - centerPosition.y) > maximumSurfaceHeightDeltaFromArenaCenter)
                continue;

            if (shrineCenter != null &&
                Vector3.Distance(hit.point, shrineCenter.position) < shrineExclusionRadius)
                continue;

            spawnPosition = hit.point + Vector3.up * groundSurfaceOffset;
            return true;
        }

        Debug.LogWarning(
            $"[TileSpawner] No valid Ground position found after {attempts} attempts around " +
            $"{arenaCenter.name} (radius {arenaRadius:0.##}).",
            this);
        return false;
    }

    public void SpawnSpecificTiles(List<char> letters)
    {

        if (letters == null || letters.Count == 0)
            return;

        TileManager tileManager = FindFirstObjectByType<TileManager>();
        int ownedTileCount = tileManager != null ? tileManager.GetNormalCombatOwnedTileCount() : CountPoolTiles();
        int pendingBeforeSpawn = PendingReplacementTileCount;
        int availableReplacementSlots = Mathf.Max(
            0,
            maximumNormalCombatTileBudget - ownedTileCount - pendingBeforeSpawn);
        int spawnCount = Mathf.Min(letters.Count, availableReplacementSlots);

        Debug.Log($"[TileSpawner] Spawning specific tiles around: {(arenaCenter != null ? arenaCenter.name : "NULL")}");
        List<WorldTile> spawnedTiles = new List<WorldTile>(spawnCount);

        for (int i = 0; i < spawnCount; i++)
        {
            char letter = letters[i];
            if (!TryGetRandomSpawnPosition(out Vector3 spawnPos))
                continue;

            GameObject tile = Instantiate(worldTilePrefab, spawnPos, Quaternion.identity);

            Renderer rend = tile.GetComponent<Renderer>();
            if (rend != null)
            {
                float halfHeight = rend.bounds.size.y / 2f;
                Vector3 pos = tile.transform.position;
                pos.y += halfHeight;
                tile.transform.position = pos;
            }

            WorldTile tileScript = tile.GetComponent<WorldTile>();
            if (tileScript != null)
            {
                tileScript.SetLetter(letter);
                pendingReplacementTiles.Add(tileScript);
                spawnedTiles.Add(tileScript);
            }
        }

        if (spawnCount < letters.Count)
        {
            Debug.LogError(
                $"[TileSpawner] Replacement budget blocked {letters.Count - spawnCount} tile(s). " +
                $"Owned: {ownedTileCount} | Pending before spawn: {pendingBeforeSpawn} | " +
                $"Budget: {maximumNormalCombatTileBudget}.",
                this);
        }

        Debug.Log(
            $"[TileSpawner] Spawned {spawnedTiles.Count}/{letters.Count} replacement tiles " +
            $"inside {(arenaCenter != null ? arenaCenter.name : "NULL")}. " +
            $"Owned + pending: {ownedTileCount + PendingReplacementTileCount}/{maximumNormalCombatTileBudget}.",
            this);
        SpecificTilesSpawned?.Invoke(spawnedTiles);
    }

    public void SpawnTiles(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            if (!TryGetRandomSpawnPosition(out Vector3 spawnPos))
                continue;

            GameObject tile = Instantiate(worldTilePrefab, spawnPos, Quaternion.identity);

            Renderer rend = tile.GetComponent<Renderer>();
            if (rend != null)
            {
                float halfHeight = rend.bounds.size.y / 2f;
                Vector3 pos = tile.transform.position;
                pos.y += halfHeight + 0.1f; 
                tile.transform.position = pos;
            }

            WorldTile tileScript = tile.GetComponent<WorldTile>();
            if (tileScript != null)
            {
                tileScript.AssignRandomLetter();
                pendingReplacementTiles.Add(tileScript);
            }
        }
    }

    public void NotifyReplacementTileCollected(WorldTile tile)
    {
        if (tile != null)
            pendingReplacementTiles.Remove(tile);
    }

    private int CountPoolTiles()
    {
        if (tilePoolPanel == null)
            return 0;

        int count = 0;
        for (int i = 0; i < tilePoolPanel.childCount; i++)
        {
            if (tilePoolPanel.GetChild(i).GetComponent<Tile>() != null)
                count++;
        }

        return count;
    }

    public void SpawnTilesInPool(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            char randomLetter = alphabet[Random.Range(0, alphabet.Length)];

            GameObject tileObj = Instantiate(worldTilePrefab, tilePoolPanel);
            tileObj.transform.localScale = Vector3.one;

            WorldTilePickup pickupTile = tileObj.GetComponent<WorldTilePickup>();
            if (pickupTile != null)
            {
                pickupTile.SetLetter(randomLetter);
                continue;
            }

            WorldTile worldTile = tileObj.GetComponent<WorldTile>();
            if (worldTile != null)
            {
                worldTile.SetLetter(randomLetter);
                continue;
            }

            Debug.LogWarning("[TileSpawner] Spawned tile prefab has no WorldTilePickup or WorldTile component.");
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (arenaCenter != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(arenaCenter.position, arenaRadius);
        }

        if (shrineCenter != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(shrineCenter.position, shrineExclusionRadius);
        }
    }
}
