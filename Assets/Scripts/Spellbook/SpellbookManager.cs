using System.Collections.Generic;
using UnityEngine;

public class SpellbookManager : MonoBehaviour
{
    private static SpellbookManager instance;

    public static SpellbookManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindFirstObjectByType<SpellbookManager>();

            if (instance == null)
            {
                GameObject go = new GameObject("SpellbookManager");
                instance = go.AddComponent<SpellbookManager>();
            }

            return instance;
        }
    }

    private readonly Dictionary<string, SpellbookEntry> entriesByWord = new Dictionary<string, SpellbookEntry>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RecordWord(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
            return;

        string normalizedWord = word.Trim().ToUpperInvariant();

        if (entriesByWord.TryGetValue(normalizedWord, out SpellbookEntry existingEntry))
        {
            existingEntry.timesUsed++;
            Debug.Log($"[SpellbookManager] Existing word reused: {normalizedWord} | Times used: {existingEntry.timesUsed}");
            Debug.Log($"[SpellbookManager] Total spellbook entries: {entriesByWord.Count}");
            return;
        }

        if (DictionaryManager.Instance == null)
        {
            Debug.LogWarning("[SpellbookManager] DictionaryManager missing. Cannot record new spellbook entry.");
            return;
        }

        string definition = DictionaryManager.Instance.GetDefinition(normalizedWord);
        string rarity = DictionaryManager.Instance.GetRarity(normalizedWord);
        int dictionaryLength = DictionaryManager.Instance.GetWordLength(normalizedWord);

        if (string.IsNullOrWhiteSpace(definition) && string.IsNullOrWhiteSpace(rarity) && dictionaryLength <= 0)
        {
            Debug.LogWarning($"[SpellbookManager] Dictionary entry not found for valid word: {normalizedWord}");
            return;
        }

        SpellbookEntry newEntry = new SpellbookEntry(
            normalizedWord,
            definition,
            rarity,
            dictionaryLength > 0 ? dictionaryLength : normalizedWord.Length,
            1
        );

        entriesByWord.Add(normalizedWord, newEntry);

        Debug.Log($"[SpellbookManager] New word discovered: {normalizedWord} | Rarity: {newEntry.rarity} | Length: {newEntry.length}");
        Debug.Log($"[SpellbookManager] Total spellbook entries: {entriesByWord.Count}");
    }

    public IReadOnlyCollection<SpellbookEntry> GetAllEntries()
    {
        return entriesByWord.Values;
    }
}
