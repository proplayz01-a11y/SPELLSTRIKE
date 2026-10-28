using System;

public static class Stage2ObjectiveVocabulary
{
    private static readonly string[] MaritimeWordsInternal =
    {
        "TIDE", "WAVE", "REEF", "CORAL", "OCEAN", "STORM", "SAILOR",
        "HARBOR", "VESSEL", "MARINE", "PIRATE", "ANCHOR", "NAUTICAL"
    };

    public static string[] CreateMaritimeWordCopy()
    {
        string[] copy = new string[MaritimeWordsInternal.Length];
        Array.Copy(MaritimeWordsInternal, copy, MaritimeWordsInternal.Length);
        return copy;
    }
}
