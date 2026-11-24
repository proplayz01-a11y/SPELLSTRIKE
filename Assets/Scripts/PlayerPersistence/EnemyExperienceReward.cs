using UnityEngine;

/// <summary>One reward per defeated enemy instance, regardless of damage route.</summary>
[DisallowMultipleComponent]
public sealed class EnemyExperienceReward : MonoBehaviour
{
    private bool awarded;

    public static int StageNumber(string sceneName)
    {
        switch (sceneName)
        {
            case "SampleScene": return 1;
            case "Stage2Scene": return 2;
            case "Stage3Scene": return 3;
            case "Stage4Scene": return 4;
            default: return 0; // No XP in tutorial, cinematics, or minigames.
        }
    }

    public static int RewardFor(int stage, bool boss)
    {
        if (stage < 1) return 0;
        return boss ? 80 + (stage - 1) * 20 : 35 + (stage - 1) * 10;
    }

    public static void Award(GameObject enemy)
    {
        if (enemy == null || !Application.isPlaying)
            return;
        int stage = StageNumber(enemy.scene.name);
        if (stage == 0) return;
        var health = enemy.GetComponent<EnemyHealth>();
        bool boss = health != null ? health.UsesBossHud :
            enemy.GetComponent<HollowKnightBossController>() != null;
        var stamp = enemy.GetComponent<EnemyExperienceReward>();
        if (stamp == null) stamp = enemy.AddComponent<EnemyExperienceReward>();
        if (stamp.awarded) return;
        stamp.awarded = true;
        PlayerProgression.GrantExperience(RewardFor(stage, boss));
        PotionDropper.DropForOtherEncounter(enemy, boss);
    }
}
