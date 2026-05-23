using UnityEngine;

public class Node3Controller : MonoBehaviour
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
            playerHealth.RestoreFullHealth();
    }

    public void OnFragmentCollected()
    {
        if (nodeCompleted) return;
        nodeCompleted = true;
        if (progressionManager != null)
            progressionManager.MarkNode3Complete();
        if (goalsPanelUI != null)
            goalsPanelUI.IncrementFragmentCount();

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

        // Later: save progress / database update here.
    }
}
