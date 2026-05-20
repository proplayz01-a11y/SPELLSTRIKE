using UnityEngine;

public class Stage1BossController : MonoBehaviour
{
    [Header("Boss References")]
    public HollowKnightController hollowKnight;
    public BossFragmentPickup bossFragment;

    [Header("Arena References")]
    public GameObject bossArenaBoundary;

    [Header("State")]
    public bool bossDefeated = false;
    public bool fragmentCollected = false;

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
    }

    public void OnBossFragmentCollected()
    {
        if (fragmentCollected) return;

        fragmentCollected = true;

        Debug.Log("[Stage1BossController] Stage 1 boss fragment collected.");

        // Later layers:
        // - Show Stage 1 Complete panel
        // - Unlock next stage
        // - Add sword reward
        // - Save progress
        // - Return to Stage Select

        // Optional for now: disable arena boundary after collecting the fragment.
        if (bossArenaBoundary != null)
        {
            bossArenaBoundary.SetActive(false);
            Debug.Log("[Stage1BossController] Boss arena boundary disabled.");
        }
    }
}