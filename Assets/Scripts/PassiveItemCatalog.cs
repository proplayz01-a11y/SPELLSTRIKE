using System.Collections.Generic;

public struct PassiveItemInfo
{
    public PassiveItemId id;
    public string displayName;
    public string shortName;
    public string description;
    public string effectSummary;
}

public static class PassiveItemCatalog
{
    private static readonly PassiveItemInfo[] Items =
    {
        new PassiveItemInfo
        {
            id = PassiveItemId.SkySword,
            displayName = "Sky Sword",
            shortName = "Sky Sword",
            description = "A purified blade claimed from the Hollow Knight.",
            effectSummary = "+10% word attack damage."
        },
        new PassiveItemInfo
        {
            id = PassiveItemId.GreatAttractor,
            displayName = "The Great Attractor",
            shortName = "Attractor",
            description = "A gravity relic earned by completing the objective.",
            effectSummary = "Nearby world letter tiles slowly move toward the player."
        },
        new PassiveItemInfo
        {
            id = PassiveItemId.PurifyingPearl,
            displayName = "Purifying Pearl",
            shortName = "Pearl",
            description = "A cleansing relic recovered from the Sunken Seas.",
            effectSummary = "After a successful word attack, one random Locked or Cracked tile loses one additional debuff counter."
        },
        new PassiveItemInfo
        {
            id = PassiveItemId.TidecarvedShell,
            displayName = "Tidecarved Shell",
            shortName = "Shell",
            description = "A resilient shell uncovered inside an ancient sea-worn boulder.",
            effectSummary = "Steady Current: Reduces enemy knockback distance and stun duration by 25%."
        }
    };

    public static IReadOnlyList<PassiveItemInfo> GetAllItems()
    {
        return Items;
    }

    public static PassiveItemInfo GetInfo(PassiveItemId id)
    {
        foreach (PassiveItemInfo item in Items)
        {
            if (item.id == id)
                return item;
        }

        return new PassiveItemInfo
        {
            id = PassiveItemId.None,
            displayName = "Empty",
            shortName = "Empty",
            description = "No passive item equipped.",
            effectSummary = string.Empty
        };
    }

    public static string GetDisplayName(PassiveItemId id)
    {
        return GetInfo(id).displayName;
    }

    public static string GetShortName(PassiveItemId id)
    {
        return GetInfo(id).shortName;
    }

    public static string GetDescription(PassiveItemId id)
    {
        return GetInfo(id).description;
    }

    public static string GetEffectSummary(PassiveItemId id)
    {
        return GetInfo(id).effectSummary;
    }
}
