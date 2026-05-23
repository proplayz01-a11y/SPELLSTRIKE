using UnityEngine;

public class Node1Controller : MonoBehaviour
{
    [Header("References")]
    public BrambleSpriteController brambleSprite; 
    public FragmentPickup fragmentPickup;
    public GameObject fragmentVisual;
    public Stage1ProgressionManager progressionManager;
    public GoalsPanelUI goalsPanelUI;
    public PlayerHealth playerHealth;

    private bool encounterStarted = false;
    private bool enemyDefeated = false;
    private bool nodeCompleted = false;

    private void Start()
    {
        if (goalsPanelUI == null)
            goalsPanelUI = FindFirstObjectByType<GoalsPanelUI>();
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (brambleSprite != null)
            brambleSprite.gameObject.SetActive(false);

        if (fragmentPickup != null)
            fragmentPickup.SetLocked(true);
    }

    private void OnEnable()
    {
        if (brambleSprite != null)
            brambleSprite.OnDeathStarted += HandleBrambleDeath;
    }

    private void OnDisable()
    {
        if (brambleSprite != null)
            brambleSprite.OnDeathStarted -= HandleBrambleDeath;
    }

    private void HandleBrambleDeath(BrambleSpriteController sprite)
    {
        OnBrambleSpriteDefeated();
    }

    public void TriggerEncounter()
    {
        if (encounterStarted || nodeCompleted) return;

        encounterStarted = true;

        if (brambleSprite != null)
        {
            brambleSprite.gameObject.SetActive(true);
            brambleSprite.StartBattle();
        }

        Debug.Log("Node 1 encounter started.");
    }

    public void OnBrambleSpriteDefeated()
    {
        if (enemyDefeated) return;

        enemyDefeated = true;

        if (fragmentPickup != null)
            fragmentPickup.SetLocked(false);
        if (playerHealth != null)
            playerHealth.RestoreFullHealth();

        Debug.Log("Bramble Sprite defeated. Fragment unlocked.");
    }

    public void OnFragmentCollected()
    {
        if (nodeCompleted) return;

        nodeCompleted = true;
        if (progressionManager != null)
            progressionManager.MarkNode1Complete();
        if (goalsPanelUI != null)
            goalsPanelUI.IncrementFragmentCount();

        if (fragmentVisual != null)
            fragmentVisual.SetActive(false);

        Debug.Log("Node 1 complete.");
    }
}
