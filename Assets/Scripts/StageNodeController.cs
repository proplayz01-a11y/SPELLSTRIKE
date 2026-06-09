using UnityEngine;
using UnityEngine.Events;

public class StageNodeController : MonoBehaviour
{
    [Header("Progression")]
    [SerializeField] private StageProgressionManager progressionManager;
    [SerializeField, Min(1)] private int nodeIndex = 1;

    [Header("Fragment")]
    [SerializeField] private FragmentPickup fragmentPickup;
    [SerializeField] private GameObject fragmentVisual;
    [SerializeField] private bool lockFragmentAtStart = true;
    [SerializeField] private bool unlockFragmentOnEnemyDefeat = true;
    [SerializeField] private bool hideFragmentVisualOnComplete = true;

    [Header("Player")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private bool restoreFullHealthOnEnemyDefeat = true;

    [Header("Objective UI")]
    [SerializeField] private GoalsPanelUI goalsPanelUI;
    [SerializeField] private int fragmentGoalIncrement = 1;

    [Header("Scene Objects")]
    [SerializeField] private GameObject nextPathIndicator;
    [SerializeField] private GameObject completedMarker;
    [SerializeField] private GameObject arenaBoundary;

    [Header("Stage Completion")]
    [SerializeField] private StageCompleteUI stageCompleteUI;
    [SerializeField] private bool showStageCompleteOnNodeComplete;

    [Header("Messages")]
    [SerializeField] private bool debugLogs = true;
    [SerializeField] private string enemyDefeatedMessage = "[StageNodeController] Enemy defeated. Fragment unlocked.";
    [SerializeField] private string nodeCompletedMessage = "[StageNodeController] Node completed.";

    [Header("Events")]
    [SerializeField] private UnityEvent onEnemyDefeated;
    [SerializeField] private UnityEvent onNodeCompleted;

    [Header("Runtime State")]
    [SerializeField] private bool enemyDefeated;
    [SerializeField] private bool nodeCompleted;

    public bool EnemyDefeated => enemyDefeated;
    public bool NodeCompleted => nodeCompleted;

    private void Start()
    {
        if (goalsPanelUI == null)
        {
            goalsPanelUI = FindFirstObjectByType<GoalsPanelUI>();
        }

        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        }

        if (fragmentPickup != null && lockFragmentAtStart)
        {
            fragmentPickup.SetLocked(true);
        }

        if (nextPathIndicator != null)
        {
            nextPathIndicator.SetActive(false);
        }

        if (completedMarker != null)
        {
            completedMarker.SetActive(false);
        }
    }

    public void OnEnemyDefeated()
    {
        if (enemyDefeated) return;

        enemyDefeated = true;

        if (fragmentPickup != null && unlockFragmentOnEnemyDefeat)
        {
            fragmentPickup.SetLocked(false);
        }

        if (playerHealth != null && restoreFullHealthOnEnemyDefeat)
        {
            playerHealth.RestoreFullHealth();
        }

        Log(enemyDefeatedMessage);
        onEnemyDefeated?.Invoke();
    }

    public void OnFragmentCollected()
    {
        CompleteNode();
    }

    public void CompleteNode()
    {
        if (nodeCompleted) return;

        nodeCompleted = true;

        if (progressionManager != null)
        {
            progressionManager.MarkNodeComplete(nodeIndex);
        }

        if (goalsPanelUI != null && fragmentGoalIncrement > 0)
        {
            goalsPanelUI.IncrementFragmentCount(fragmentGoalIncrement);
        }

        if (fragmentVisual != null && hideFragmentVisualOnComplete)
        {
            fragmentVisual.SetActive(false);
        }

        if (arenaBoundary != null)
        {
            arenaBoundary.SetActive(false);
        }

        if (nextPathIndicator != null)
        {
            nextPathIndicator.SetActive(true);
        }

        if (completedMarker != null)
        {
            completedMarker.SetActive(true);
        }

        if (stageCompleteUI != null && showStageCompleteOnNodeComplete)
        {
            stageCompleteUI.ShowStageComplete();
        }

        Log(nodeCompletedMessage);
        onNodeCompleted?.Invoke();
    }

    private void Log(string message)
    {
        if (!debugLogs || string.IsNullOrWhiteSpace(message)) return;
        Debug.Log(message);
    }
}
