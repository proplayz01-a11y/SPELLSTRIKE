using UnityEngine;

public class Node3Controller : MonoBehaviour
{
    [Header("Story — after collecting Cursed Jester's fragment")]
    [SerializeField] private Stage1PostJesterEchoCall postJesterEchoCall;
    [Header("Progression")]
    public Stage1ProgressionManager progressionManager;
    public GoalsPanelUI goalsPanelUI;
    public PlayerHealth playerHealth;

    [Header("Progression Objects")]
    public GameObject fragmentToUnlock;
    public GameObject nextPathIndicator;
    public GameObject arenaBoundary;

    [Header("Node State")]
    public bool cursedJesterDefeated = false;
    public bool nodeCompleted = false;

    private void Start()
    {
        if (goalsPanelUI == null)
            goalsPanelUI = FindFirstObjectByType<GoalsPanelUI>();
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();
    }

    public void OnCursedJesterDefeated()
    {
        if (cursedJesterDefeated) return;
        cursedJesterDefeated = true;
        ObjectiveManager.ReportEnemyDefeated(gameObject.scene, 3);

        Debug.Log("[Node3Controller] Cursed Jester defeated. Fragment is now unlocked.");

        if (fragmentToUnlock != null)
        {
            FragmentPickup pickup = fragmentToUnlock.GetComponent<FragmentPickup>();
            if (pickup != null)
            {
                pickup.SetLocked(false);
                Debug.Log("[Node3Controller] Fragment is now collectable.");
            }
            else
            {
                Debug.LogWarning("[Node3Controller] FragmentPickup component missing on fragment.");
            }
        }
        else
        {
            Debug.LogWarning("[Node3Controller] Fragment reference is missing.");
        }

        if (playerHealth != null)
            playerHealth.RestoreFullHealthAfterEnemyDefeat();
    }

    public void OnFragmentCollected()
    {
        if (nodeCompleted) return;
        nodeCompleted = true;
        if (progressionManager != null)
            progressionManager.MarkNode3Complete();
        ObjectiveManager.ReportFragmentCollected(gameObject.scene, 3);

        Debug.Log("[Node3Controller] Node 3 fragment collected. Node 3 completed.");

        if (nextPathIndicator != null)
        {
            nextPathIndicator.SetActive(true);
            Debug.Log("[Node3Controller] Next path indicator enabled.");
        }

        if (arenaBoundary != null)
        {
            arenaBoundary.SetActive(false);
            Debug.Log("[Node3Controller] Arena boundary disabled.");
        }

        // The pickup calls this BEFORE deactivating the crystal. The new
        // cinematic waits until end-of-frame so the collected fragment is
        // gone first. Van initiates the rune call; the Spirit does not.
        if (postJesterEchoCall != null)
            postJesterEchoCall.RequestAfterPickup();

        // Later: save progress / database update here.
    }
}
