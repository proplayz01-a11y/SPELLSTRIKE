using UnityEngine;

public class StoneSentinelTriggerEncounter : MonoBehaviour
{
    [Header("Stone Sentinel")]
    public StoneSentinelsController stoneSentinel;

    [Header("Tile Spawn Settings")]
    public Transform stoneSentinelArenaCenter;
    public float stoneSentinelArenaRadius = 10f;
    public Transform stoneSentinelShrineCenter;
    public float stoneSentinelShrineExclusionRadius = 3f;

        [Header("Arena Lock")]
public GameObject arenaBoundary;

    private bool encounterStarted = false;

   public void StartStoneSentinelEncounter()
{
    if (encounterStarted) return;
    encounterStarted = true;

    if (TileSpawner.Instance == null)
    {
        Debug.LogError("[StoneSentinelTriggerEncounter] TileSpawner.Instance is NULL.");
        return;
    }

    if (stoneSentinel == null)
    {
        Debug.LogError("[StoneSentinelTriggerEncounter] Stone Sentinel reference is missing.");
        return;
    }

   TileSpawner.Instance.ConfigureArenaBounds(
    stoneSentinelArenaCenter,
    stoneSentinelArenaRadius,
    stoneSentinelShrineCenter,
    stoneSentinelShrineExclusionRadius
);

if (arenaBoundary != null)
{
    arenaBoundary.SetActive(true);
    Debug.Log("[StoneSentinelTriggerEncounter] Arena boundary enabled.");
}

if (!stoneSentinel.gameObject.activeSelf)
{
    stoneSentinel.gameObject.SetActive(true);
    Debug.Log("[StoneSentinelTriggerEncounter] Stone Sentinel enabled.");
}

stoneSentinel.StartBattle();
}

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        StartStoneSentinelEncounter();
    }

    private void OnDrawGizmosSelected()
    {
        if (stoneSentinelArenaCenter != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(stoneSentinelArenaCenter.position, stoneSentinelArenaRadius);
        }

        if (stoneSentinelShrineCenter != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(stoneSentinelShrineCenter.position, stoneSentinelShrineExclusionRadius);
        }
    }
}