using UnityEngine;

public class Stage1BossController : MonoBehaviour
{
    [Header("Progression")]
    public Stage1ProgressionManager progressionManager;
    public GoalsPanelUI goalsPanelUI;
    public PlayerHealth playerHealth;

    [Header("Boss References")]
    public HollowKnightController hollowKnight;
    public BossFragmentPickup bossFragment;
    public SwordRewardPickup swordReward;

    [Header("Arena References")]
    public GameObject bossArenaBoundary;

    [Header("State")]
    public bool bossDefeated = false;
    public bool fragmentCollected = false;
    public bool swordCollected = false;

    [Header("UI References")]
    public StageCompleteUI stageCompleteUI;

    private void Start()
    {
        if (goalsPanelUI == null)
            goalsPanelUI = FindFirstObjectByType<GoalsPanelUI>();
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();
    }

    public void OnHollowKnightDefeated()
    {
        if (bossDefeated) return;

        bossDefeated = true;

        Debug.Log("[Stage1BossController] Hollow Knight defeated.");

        if (bossFragment != null)
        {
            bossFragment.UnlockFragment();
        }
        else
        {
            Debug.LogWarning("[Stage1BossController] Boss fragment reference missing.");
        }

        if (swordReward != null)
        {
            swordReward.UnlockSwordPickup();
        }
        else
        {
            Debug.LogWarning("[Stage1BossController] Sword reward reference missing.");
        }

        if (playerHealth != null)
            playerHealth.RestoreFullHealth();
    }

    public void OnSwordRewardCollected()
    {
        if (swordCollected) return;

        swordCollected = true;

        Debug.Log("[Stage1BossController] Sky Sword obtained manually.");
        Debug.Log("[Stage1BossController] Passive unlocked: +10% word attack damage starting Stage 2.");
        PassiveRewardManager.GrantSkySwordReward("Hollow Knight reward");

    }

    public void OnBossFragmentCollected()
    {
        if (fragmentCollected) return;

        fragmentCollected = true;
        if (progressionManager != null)
            progressionManager.MarkBossComplete();
        if (goalsPanelUI != null)
            goalsPanelUI.IncrementFragmentCount();

        Debug.Log("[Stage1BossController] Stage 1 boss fragment collected.");

        if (!swordCollected)
        {
            GrantSwordRewardAutomatically();
        }

        if (bossArenaBoundary != null)
        {
            bossArenaBoundary.SetActive(false);
            Debug.Log("[Stage1BossController] Boss arena boundary disabled.");
        }

        // Later layers:
        if (stageCompleteUI != null)
        {
            stageCompleteUI.ShowStageComplete();
        }
        else
        {
            Debug.LogWarning("[Stage1BossController] StageCompleteUI reference missing.");
        }
        // - Unlock Stage 2
        // - Save progress
        // - Return to Stage Select
    }

    private void GrantSwordRewardAutomatically()
    {
        swordCollected = true;

        Debug.Log("[Stage1BossController] Sky Sword auto-granted after fragment pickup.");
        Debug.Log("[Stage1BossController] Passive unlocked: +10% word attack damage starting Stage 2.");
        PassiveRewardManager.GrantSkySwordReward("Hollow Knight reward");

        if (swordReward != null)
        {
            swordReward.AutoGrantSwordReward();
        }

    }
}
