using UnityEngine;

public class Node2Controller : MonoBehaviour
{
    [Header("Progression")]
    public Stage1ProgressionManager progressionManager;
    public GoalsPanelUI goalsPanelUI;
    public PlayerHealth playerHealth;

    [Header("Progression Objects")]
    public GameObject fragmentToUnlock;
    public GameObject nextPathIndicator;
    public GameObject arenaBoundary;

    [Header("Node State")]
    public bool stoneSentinelDefeated = false;
    public bool nodeCompleted = false;

    private void Start()
    {
        if (goalsPanelUI == null)
            goalsPanelUI = FindFirstObjectByType<GoalsPanelUI>();
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();
    }

    public void OnStoneSentinelDefeated()
    {
        if (stoneSentinelDefeated) return;
        stoneSentinelDefeated = true;
        ObjectiveManager.ReportEnemyDefeated(gameObject.scene, 2);

        Debug.Log("[Node2Controller] Stone Sentinel defeated. Fragment is now unlocked.");

        if (fragmentToUnlock != null)
        {
            FragmentPickup pickup = fragmentToUnlock.GetComponent<FragmentPickup>();

            if (pickup != null)
            {
                pickup.SetLocked(false);
                Debug.Log("[Node2Controller] Fragment is now collectable.");
            }
            else
            {
                Debug.LogWarning("[Node2Controller] FragmentPickup component missing on fragment.");
            }
        }
        else
        {
            Debug.LogWarning("[Node2Controller] Fragment reference is missing.");
        }

        if (playerHealth != null)
            playerHealth.RestoreFullHealthAfterEnemyDefeat();
    }

    public void OnFragmentCollected()
    {
        if (nodeCompleted) return;
        nodeCompleted = true;
        if (progressionManager != null)
            progressionManager.MarkNode2Complete();
        ObjectiveManager.ReportFragmentCollected(gameObject.scene, 2);

        Debug.Log("[Node2Controller] Node 2 fragment collected. Node 2 completed.");

        if (nextPathIndicator != null)
        {
            nextPathIndicator.SetActive(true);
            Debug.Log("[Node2Controller] Next path indicator enabled.");
        }

        if (arenaBoundary != null)
        {
            arenaBoundary.SetActive(false);
            Debug.Log("[Node2Controller] Arena boundary disabled.");
        }

        // Later: save progress / database update here.
    }
}
