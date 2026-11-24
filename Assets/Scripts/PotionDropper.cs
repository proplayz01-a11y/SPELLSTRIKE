using System.Collections.Generic;
using UnityEngine;

public static class PotionDropper
{
    public static bool TryDropRandomPotion(
        Transform source,
        string logSource,
        float dropChance,
        int minDropCount,
        int maxDropCount,
        int amountPerDrop,
        bool canDropHealthPotion,
        bool canDropPurifyPotion,
        bool canDropPowerUpPotion)
    {
        if (source == null || !Application.isPlaying) return false;
        var inventory = PotionInventory.EnsureInstance();
        if (inventory == null) return false;
        if (Random.value > Mathf.Clamp01(dropChance))
            return false;

        List<PotionType> possibleDrops = new List<PotionType>();

        if (canDropHealthPotion)
            possibleDrops.Add(PotionType.Health);

        if (canDropPurifyPotion)
            possibleDrops.Add(PotionType.Cleansing);

        if (canDropPowerUpPotion)
            possibleDrops.Add(PotionType.PowerUp);

        // Count uncollected world loot in this scene as reserved capacity.
        // A later enemy must not drop another bottle for a slot already covered
        // by existing loot. Inventory itself changes only on collection.
        var available = new Dictionary<PotionType, int>();
        foreach (var type in possibleDrops)
            available[type] = Mathf.Max(0, inventory.maxPotionCount - inventory.GetCount(type));
        foreach (var pickup in Object.FindObjectsByType<PotionLootPickup>(FindObjectsSortMode.None))
        {
            if (pickup.gameObject.scene != source.gameObject.scene || pickup.Remaining <= 0
                || !available.ContainsKey(pickup.Type)) continue;
            available[pickup.Type] = Mathf.Max(0, available[pickup.Type] - pickup.Remaining);
        }
        possibleDrops.RemoveAll(type => available[type] <= 0);
        if (possibleDrops.Count == 0)
            return false; // EXP remains independent of loot.

        int minCount = Mathf.Max(0, Mathf.Min(minDropCount, maxDropCount));
        int maxCount = Mathf.Max(0, Mathf.Max(minDropCount, maxDropCount));
        int dropCount = Random.Range(minCount, maxCount + 1);
        int safeAmount = Mathf.Max(1, amountPerDrop);
        bool addedAnyPotion = false;

        for (int i = 0; i < dropCount; i++)
        {
            if (possibleDrops.Count == 0) break;
            PotionType potionType = possibleDrops[Random.Range(0, possibleDrops.Count)];
            string potionName = PotionInventory.GetDisplayName(potionType);
            int quantity = Mathf.Min(safeAmount, available[potionType]);

            if (PotionLootPickup.Spawn(source, potionType, quantity, i) != null)
            {
                addedAnyPotion = true;
                available[potionType] -= quantity;
                if (available[potionType] <= 0) possibleDrops.Remove(potionType);
                Debug.Log($"[{logSource}] Dropped {potionName}.");
            }
            else
            {
                Debug.LogWarning($"[{logSource}] Could not spawn {potionName}.");
            }
        }

        return addedAnyPotion;
    }

    // The four older controllers retain their authored drop configuration and
    // death-animation callbacks. All other authored campaign encounters use
    // the shared once-per-death reward path.
    public static void DropForOtherEncounter(GameObject enemy, bool boss)
    {
        if (enemy == null || enemy.GetComponent<BrambleSpriteController>() != null
            || enemy.GetComponent<StoneSentinelsController>() != null
            || enemy.GetComponent<CursedJesterController>() != null
            || enemy.GetComponent<BarnacleHuskController>() != null) return;
        int stage = EnemyExperienceReward.StageNumber(enemy.scene.name);
        if (StageEnemyDifficulty.Resolve(enemy, 0f) <= 0f || stage == 0) return;
        TryDropRandomPotion(enemy.transform, enemy.name, boss ? 1f : .6f,
            1, 1, 1, true, true, true);
    }
}
