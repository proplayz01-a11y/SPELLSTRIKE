using UnityEngine;

/// <summary>
/// First-pass campaign HP tuning. Absolute encounter targets avoid compounding
/// prefab overrides, preserve boss identity on rematches, and leave tutorial,
/// unlisted summons and noncampaign enemies at their authored health.
/// </summary>
public static class StageEnemyDifficulty
{
    public static float Resolve(GameObject enemy, float authoredMaximum)
    {
        if (enemy == null) return authoredMaximum;
        int stage = EnemyExperienceReward.StageNumber(enemy.scene.name);
        if (stage == 0) return authoredMaximum;
        foreach (var component in enemy.GetComponents<MonoBehaviour>())
        {
            if (component == null) continue;
            float tuned = TargetHealth(stage, component.GetType().Name);
            if (tuned > 0f) return tuned;
        }
        return authoredMaximum;
    }

    // Zero indicates that this encounter is not explicitly tuned.
    public static float TargetHealth(int stage, string controller)
    {
        switch (stage)
        {
            case 1:
                switch (controller)
                {
                    case "BrambleSpriteController": return 25f;
                    case "StoneSentinelsController": return 35f;
                    case "CursedJesterController": return 45f;
                    case "HollowKnightController": return 65f;
                }
                break;
            case 2:
                switch (controller)
                {
                    case "BarnacleHuskController": return 50f;
                    case "CorsairPhantomController": return 60f;
                    case "TideWraithController": return 55f;
                    case "DrownedCaptainController": return 95f;
                }
                break;
            case 3:
                switch (controller)
                {
                    case "SunboundWardenController": return 75f;
                    case "StoneGolemController": return 90f;
                    case "DesertArcherController": return 85f;
                    case "TitanEarthbreakerController": return 140f;
                }
                break;
            case 4:
                switch (controller)
                {
                    case "HollowKnightController": return 155f;
                    case "DrownedCaptainController": return 175f;
                    case "TitanEarthbreakerController": return 195f;
                    case "Stage4VanderPoelPreviewEncounterV01": return 240f;
                    case "VanderPoelController": return 240f;
                }
                break;
        }
        return 0f;
    }
}
