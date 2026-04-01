using UnityEngine;

public class TileSpawner : MonoBehaviour
{
    public static TileSpawner Instance;

    public GameObject worldTilePrefab;
    public Transform tilePoolPanel;

    [Header("Arena Spawn Range")]
    public Transform arenaCenter;    // assign the center of your arena
    public float arenaRangeX = 10f; // how far left/right tiles can spawn
    public float arenaRangeZ = 10f; // how far forward/back tiles can spawn
    public float spawnY = 0f;       // ground level

    private char[] alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SpawnTilesInPool(16);
    }

    private Vector3 GetRandomSpawnPosition()
    {
        float centerX = arenaCenter != null ? arenaCenter.position.x : 0f;
        float centerZ = arenaCenter != null ? arenaCenter.position.z : 0f;

        float randomX = Random.Range(centerX - arenaRangeX, centerX + arenaRangeX);
        float randomZ = Random.Range(centerZ - arenaRangeZ, centerZ + arenaRangeZ);

        // Raycast downward from high above to hit actual terrain surface
        Vector3 rayOrigin = new Vector3(randomX, 100f, randomZ);
        Ray ray = new Ray(rayOrigin, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, 200f))
        {
            Debug.Log("Tile spawning on terrain at: " + hit.point);
            return hit.point; // exact terrain surface position
        }

        // fallback if raycast misses
        return new Vector3(randomX, spawnY, randomZ);
    }

    public void SpawnTiles(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            Vector3 spawnPos = GetRandomSpawnPosition();

            GameObject tile = Instantiate(worldTilePrefab, spawnPos, Quaternion.identity);

            // Adjust Y so tile sits on ground properly
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

            WorldTilePickup tileScript = tileObj.GetComponent<WorldTilePickup>();
            tileScript.SetLetter(randomLetter);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (arenaCenter == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(
            arenaCenter.position,
            new Vector3(arenaRangeX * 2, 0.1f, arenaRangeZ * 2)
        );
    }
}