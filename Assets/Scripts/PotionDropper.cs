using System.Collections.Generic;
using UnityEngine;

public static class PotionDropper
{
    public static bool TryDropRandomPotion(
        string logSource,
        float dropChance,
        int minDropCount,
        int maxDropCount,
        int amountPerDrop,
        bool canDropHealthPotion,
        bool canDropPurifyPotion,
        bool canDropPowerUpPotion)
    {
        PotionInventory inventory = GetOrCreatePotionInventory();

        if (inventory == null)
        {
            Debug.LogWarning($"[{logSource}] Potion drop failed. PotionInventory could not be created.");
            return false;
        }

        if (Random.value > Mathf.Clamp01(dropChance))
            return false;

        List<PotionType> possibleDrops = new List<PotionType>();

        if (canDropHealthPotion)
            possibleDrops.Add(PotionType.Health);

        if (canDropPurifyPotion)
            possibleDrops.Add(PotionType.Cleansing);

        if (canDropPowerUpPotion)
            possibleDrops.Add(PotionType.PowerUp);

        if (possibleDrops.Count == 0)
        {
            Debug.LogWarning($"[{logSource}] Potion drop failed. No potion types are enabled.");
            return false;
        }

        int minCount = Mathf.Max(0, Mathf.Min(minDropCount, maxDropCount));
        int maxCount = Mathf.Max(0, Mathf.Max(minDropCount, maxDropCount));
        int dropCount = Random.Range(minCount, maxCount + 1);
        int safeAmount = Mathf.Max(1, amountPerDrop);
        bool addedAnyPotion = false;

        for (int i = 0; i < dropCount; i++)
        {
            PotionType potionType = possibleDrops[Random.Range(0, possibleDrops.Count)];
            int added = inventory.AddPotion(potionType, safeAmount);
            string potionName = PotionInventory.GetDisplayName(potionType);

            if (added > 0)
            {
                addedAnyPotion = true;
                Debug.Log($"[{logSource}] Dropped {potionName}.");
            }
            else
            {
                Debug.Log($"[{logSource}] Rolled {potionName}, but inventory is full.");
            }
        }

        return addedAnyPotion;
    }

    private static T FindAnyObjectOfType<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<T>();
#else
        return Object.FindObjectOfType<T>();
#endif
    }

    private static PotionInventory GetOrCreatePotionInventory()
    {
        if (PotionInventory.Instance != null)
            return PotionInventory.Instance;

        PotionInventory inventory = FindAnyObjectOfType<PotionInventory>();
        if (inventory != null)
            return inventory;

        GameObject inventoryObject = new GameObject("PotionInventory_AutoCreated");
        return inventoryObject.AddComponent<PotionInventory>();
    }
}
