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

    private char[] alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SpawnTilesInPool(16);
    }

    public void ConfigureArenaBounds(Transform center, float radius)
    {
        if (center != null)
            arenaCenter = center;

        arenaRadius = Mathf.Max(0.5f, radius);
    }

    private Vector3 GetRandomSpawnPosition()
    {
        Vector3 centerPos = arenaCenter != null ? arenaCenter.position : Vector3.zero;
        Vector2 randomCircle = Random.insideUnitCircle * arenaRadius;
        Vector3 spawnPos = centerPos + new Vector3(randomCircle.x, 0f, randomCircle.y);

        // Raycast downward from above to hit terrain/ground and get final Y.
        Vector3 rayOrigin = new Vector3(spawnPos.x, 100f, spawnPos.z);
        Ray ray = new Ray(rayOrigin, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, 200f))
        {
            return hit.point;
        }

        // fallback if raycast misses
        return new Vector3(spawnPos.x, spawnY, spawnPos.z);
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
        Gizmos.DrawWireSphere(arenaCenter.position, arenaRadius);
    }
}
