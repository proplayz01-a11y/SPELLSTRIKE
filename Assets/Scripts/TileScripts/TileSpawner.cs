using System.Collections.Generic;
using UnityEngine;

public class TileSpawner : MonoBehaviour
{
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


    private char[] alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SpawnTilesInPool(16);
    }



    public void ConfigureArenaBounds(Transform center, float radius, Transform shrine, float exclusionRadius)
    {
        if (center != null)
            arenaCenter = center;

        arenaRadius = Mathf.Max(0.5f, radius);

        shrineCenter = shrine;
        shrineExclusionRadius = Mathf.Max(0f, exclusionRadius);

        Debug.Log(
            $"[TileSpawner] Configured arena: {(arenaCenter != null ? arenaCenter.name : "NULL")} | " +
            $"Radius: {arenaRadius} | " +
            $"Shrine: {(shrineCenter != null ? shrineCenter.name : "NULL")} | " +
            $"Exclusion: {shrineExclusionRadius}"
        );
    }

    private Vector3 GetRandomSpawnPosition()
    {

        Vector3 centerPos = arenaCenter != null ? arenaCenter.position : Vector3.zero;
        Vector2 randomCircle = Random.insideUnitCircle * arenaRadius;
        Vector3 spawnPos = centerPos + new Vector3(randomCircle.x, 0f, randomCircle.y);

        Vector3 rayOrigin = new Vector3(spawnPos.x, 500f, spawnPos.z);
        Ray ray = new Ray(rayOrigin, Vector3.down);
        Debug.Log($"Ray origin: {rayOrigin}");

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
        {
            if (shrineCenter != null &&
                Vector3.Distance(hit.point, shrineCenter.position) < shrineExclusionRadius)
            {
                return Vector3.positiveInfinity;
            }

            Debug.Log($"Hit: {hit.point} on {hit.collider.gameObject.name}");
            return hit.point;
        }

        return new Vector3(spawnPos.x, spawnY, spawnPos.z);
    }

    public void SpawnSpecificTiles(List<char> letters)
    {

        if (letters == null || letters.Count == 0)
            return;

        Debug.Log($"[TileSpawner] Spawning specific tiles around: {(arenaCenter != null ? arenaCenter.name : "NULL")}");

        foreach (char letter in letters)
        {
            Vector3 spawnPos = GetRandomSpawnPosition();

            if (spawnPos == Vector3.positiveInfinity)
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
                tileScript.SetLetter(letter);
        }
    }

    public void SpawnTiles(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            Vector3 spawnPos = GetRandomSpawnPosition();

            if (spawnPos == Vector3.positiveInfinity)
            {
                i--;
                continue;
            }

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
                tileScript.AssignRandomLetter();
        }
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
