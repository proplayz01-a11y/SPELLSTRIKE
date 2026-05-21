using UnityEngine;

public class CursedJesterTriggerEncounter : MonoBehaviour
{
    [Header("Encounter References")]
    public CursedJesterController cursedJester;
    public GameObject arenaBoundary;

    [Header("Arena Bounds")]
    public Transform arenaCenter;
    public float arenaRadius = 10f;

    public Transform shrineCenter;
    public float shrineExclusionRadius = 3f;

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;

        if (arenaBoundary != null)
            arenaBoundary.SetActive(true);

        if (TileSpawner.Instance != null)
        {
            TileSpawner.Instance.ConfigureArenaBounds(
                arenaCenter,
                arenaRadius,
                shrineCenter,
                shrineExclusionRadius
            );
        }
        else
        {
            Debug.LogWarning("[CursedJesterTriggerEncounter] TileSpawner.Instance is missing.");
        }

        if (cursedJester != null)
        {
            cursedJester.gameObject.SetActive(true);
            cursedJester.StartBattle();
        }
        else
        {
            Debug.LogWarning("[CursedJesterTriggerEncounter] Cursed Jester reference is missing.");
        }

        Debug.Log("[CursedJesterTriggerEncounter] Player entered trigger. Cursed Jester battle started.");
    }

    private void OnDrawGizmos()
    {
        if (arenaCenter != null)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
            Gizmos.DrawSphere(arenaCenter.position, arenaRadius);
            Gizmos.color = new Color(0f, 1f, 0f, 1f);
            Gizmos.DrawWireSphere(arenaCenter.position, arenaRadius);
        }

        if (shrineCenter != null)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            Gizmos.DrawSphere(shrineCenter.position, shrineExclusionRadius);
            Gizmos.color = new Color(1f, 0f, 0f, 1f);
            Gizmos.DrawWireSphere(shrineCenter.position, shrineExclusionRadius);
        }
    }
}