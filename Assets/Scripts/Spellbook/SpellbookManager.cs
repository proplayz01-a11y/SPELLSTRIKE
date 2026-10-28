using System;
using System.Collections.Generic;
using UnityEngine;

public class SpellbookManager : MonoBehaviour
{
    public event Action EntriesChanged;

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
        GameDatabaseManager.EnsureInstance()
            .RecordWordHistory(normalizedWord, true);

        if (entriesByWord.TryGetValue(normalizedWord, out SpellbookEntry existingEntry))
        {
            existingEntry.timesUsed++;
            Debug.Log($"[SpellbookManager] Existing word reused: {normalizedWord} | Times used: {existingEntry.timesUsed}");
            Debug.Log($"[SpellbookManager] Total spellbook entries: {entriesByWord.Count}");
            EntriesChanged?.Invoke();
            return;
        }

        DictionaryManager dictionary = DictionaryManager.EnsureInstance();
        SpellbookEntry newEntry = CreateEntry(normalizedWord, 1, dictionary);
        entriesByWord.Add(normalizedWord, newEntry);

        Debug.Log($"[SpellbookManager] New word discovered: {normalizedWord} | Rarity: {newEntry.rarity} | Length: {newEntry.length}");
        Debug.Log($"[SpellbookManager] Total spellbook entries: {entriesByWord.Count}");
        EntriesChanged?.Invoke();
    }

    public void RefreshFromPersistentHistory()
    {
        DictionaryManager dictionary = DictionaryManager.EnsureInstance();
        GameDatabaseManager database = GameDatabaseManager.EnsureInstance();
        Dictionary<string, int> usageCounts =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        entriesByWord.Clear();

        if (database.Data?.wordHistory != null)
        {
            foreach (WordHistoryEntry historyEntry in database.Data.wordHistory)
            {
                if (historyEntry == null
                    || !historyEntry.valid
                    || string.IsNullOrWhiteSpace(historyEntry.word))
                {
                    continue;
                }

                string normalizedWord =
                    historyEntry.word.Trim().ToUpperInvariant();

                if (usageCounts.TryGetValue(normalizedWord, out int count))
                    usageCounts[normalizedWord] = count + 1;
                else
                    usageCounts.Add(normalizedWord, 1);
            }
        }

        foreach (KeyValuePair<string, int> usage in usageCounts)
        {
            entriesByWord.Add(
                usage.Key,
                CreateEntry(usage.Key, usage.Value, dictionary));
        }

        EntriesChanged?.Invoke();
    }

    public IReadOnlyCollection<SpellbookEntry> GetAllEntries()
    {
        return entriesByWord.Values;
    }

    private static SpellbookEntry CreateEntry(
        string normalizedWord,
        int timesUsed,
        DictionaryManager dictionary)
    {
        string definition = dictionary != null
            ? dictionary.GetDefinition(normalizedWord)
            : string.Empty;
        string rarity = dictionary != null
            ? dictionary.GetRarity(normalizedWord)
            : string.Empty;
        int dictionaryLength = dictionary != null
            ? dictionary.GetWordLength(normalizedWord)
            : 0;

        if (string.IsNullOrWhiteSpace(definition))
            definition = "Definition unavailable in the gameplay dictionary.";
        if (string.IsNullOrWhiteSpace(rarity))
            rarity = "unclassified";

        return new SpellbookEntry(
            normalizedWord,
            definition,
            rarity,
            dictionaryLength > 0 ? dictionaryLength : normalizedWord.Length,
            Mathf.Max(1, timesUsed));
    }
}
