using UnityEngine;

public class HollowKnightTriggerEncounter : MonoBehaviour
{
    [Header("Hollow Knight")]
    public HollowKnightController hollowKnight;

    [Header("Tile Spawn Settings")]
    public Transform bossArenaCenter;
    public float bossArenaRadius = 12f;

    public Transform bossShrineCenter;
    public float bossShrineExclusionRadius = 3f;

    [Header("Arena Lock")]
    public GameObject bossArenaBoundary;

    private bool encounterStarted = false;

    private void OnTriggerEnter(Collider other)
    {
        if (encounterStarted) return;
        if (!other.CompareTag("Player")) return;

        StartBossEncounter();
    }

    public void StartBossEncounter()
    {
        encounterStarted = true;

        Debug.Log("[HollowKnightTriggerEncounter] Boss encounter started.");

        ConfigureTileSpawner();
        EnableArenaBoundary();
        EnableAndStartBoss();
    }

    private void ConfigureTileSpawner()
    {
        if (TileSpawner.Instance == null)
        {
            Debug.LogWarning("[HollowKnightTriggerEncounter] TileSpawner.Instance is missing.");
            return;
        }

        if (bossArenaCenter == null)
        {
            Debug.LogWarning("[HollowKnightTriggerEncounter] Boss arena center missing.");
            return;
        }

        if (bossShrineCenter == null)
        {
            Debug.LogWarning("[HollowKnightTriggerEncounter] Boss shrine center missing.");
            return;
        }

        TileSpawner.Instance.ConfigureArenaBounds(
            bossArenaCenter,
            bossArenaRadius,
            bossShrineCenter,
            bossShrineExclusionRadius
        );

        Debug.Log("[HollowKnightTriggerEncounter] TileSpawner configured for Hollow Knight arena.");
    }

    private void EnableArenaBoundary()
    {
        if (bossArenaBoundary == null)
        {
            Debug.LogWarning("[HollowKnightTriggerEncounter] Boss arena boundary missing.");
            return;
        }

        bossArenaBoundary.SetActive(true);
        Debug.Log("[HollowKnightTriggerEncounter] Boss arena boundary enabled.");
    }

    private void EnableAndStartBoss()
    {
        if (hollowKnight == null)
        {
            Debug.LogWarning("[HollowKnightTriggerEncounter] Hollow Knight reference missing.");
            return;
        }

        if (!hollowKnight.gameObject.activeSelf)
        {
            hollowKnight.gameObject.SetActive(true);
            Debug.Log("[HollowKnightTriggerEncounter] Hollow Knight enabled.");
        }

        hollowKnight.StartBattle();
    }

    private void OnDrawGizmosSelected()
{
    if (bossArenaCenter != null)
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(bossArenaCenter.position, bossArenaRadius);
    }

    if (bossShrineCenter != null)
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(bossShrineCenter.position, bossShrineExclusionRadius);
    }
}
}