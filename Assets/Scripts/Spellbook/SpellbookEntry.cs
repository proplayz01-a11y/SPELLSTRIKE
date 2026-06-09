using UnityEngine;

[System.Serializable]
public class SpellbookEntry
{
    public string word;
    public string definition;
    public string rarity;
    public int length;
    public int timesUsed;

    public SpellbookEntry(string word, string definition, string rarity, int length, int timesUsed)
    {
        this.word = word;
        this.definition = definition;
        this.rarity = rarity;
        this.length = length;
        this.timesUsed = timesUsed;
    }
}
