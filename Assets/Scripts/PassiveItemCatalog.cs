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
            effectSummary = "Later: nearby tiles slowly move toward the player."
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
