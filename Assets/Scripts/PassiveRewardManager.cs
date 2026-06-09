using UnityEngine;

public static class PassiveRewardManager
{
    public static bool GrantPassiveReward(PassiveItemId itemId, string source)
    {
        PassiveItemInventory inventory = PassiveItemInventory.GetOrCreate();
        if (inventory == null)
        {
            Debug.LogWarning("[PassiveReward] PassiveItemInventory could not be created.");
            return false;
        }

        bool granted = inventory.AddPendingPassive(itemId, source);
        if (granted)
            Debug.Log($"[PassiveReward] {PassiveItemCatalog.GetDisplayName(itemId)} reward queued.");

        return granted;
    }

    public static bool GrantSkySwordReward(string source)
    {
        return GrantPassiveReward(PassiveItemId.SkySword, source);
    }

    public static bool GrantObjectiveCompletionReward(string source)
    {
        return GrantPassiveReward(PassiveItemId.GreatAttractor, source);
    }
}
