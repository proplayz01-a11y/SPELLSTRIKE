using System;
using System.Collections.Generic;
using UnityEngine;

public class DictionaryManager : MonoBehaviour
{
    public static DictionaryManager Instance;

    [Header("Dictionary Source")]
    public TextAsset cleanGameplayDictionaryFile; // assign clean_gameplay_dictionary.json

    [Header("Fallback Word Validation")]
    public TextAsset fallbackWordListFile; // optional dictionary.txt.txt
    public bool autoLoadFallbackWordList = true;
    public string fallbackResourceName = "dictionary.txt";
    public bool allowFallbackWordsWithoutDefinitions = true;

    private Dictionary<string, GameplayWordData> words = new Dictionary<string, GameplayWordData>();
    private readonly HashSet<string> fallbackWords = new HashSet<string>();

    private readonly List<string> shortWords = new List<string>();   // 3-5
    private readonly List<string> mediumWords = new List<string>();  // 6-9
    private readonly List<string> longWords = new List<string>();    // 10+

    [Serializable]
    public class GameplayWordData
    {
        public string definition;
        public int length;
        public string rarity;
    }

    [Serializable]
    private class GameplayWordEntry
    {
        public string word;
        public string definition;
        public int length;
        public string rarity;
    }

    [Serializable]
    private class GameplayDictionaryWrapper
    {
        public GameplayWordEntry[] entries;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            LoadDictionary();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadDictionary()
    {
        if (cleanGameplayDictionaryFile == null)
        {
            Debug.LogError("[DictionaryManager] No clean gameplay dictionary JSON assigned!");
            return;
        }

        // Unity JsonUtility cannot parse a raw JSON object dictionary directly.
        // So we manually parse the top-level JSON object.
        string json = cleanGameplayDictionaryFile.text;

        try
        {
            words = ParseGameplayDictionary(json);
            LoadFallbackWordList();

            foreach (KeyValuePair<string, GameplayWordData> pair in words)
            {
                string word = pair.Key;
                int len = word.Length;

                if (len >= 3 && len <= 5)
                    shortWords.Add(word);
                else if (len >= 6 && len <= 9)
                    mediumWords.Add(word);
                else if (len >= 10)
                    longWords.Add(word);
            }

            Debug.Log($"[DictionaryManager] Loaded {words.Count} gameplay words.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[DictionaryManager] Failed to load clean gameplay dictionary: {e.Message}");
        }
    }

    public bool IsValidWord(string word)
    {
        if (string.IsNullOrWhiteSpace(word)) return false;

        string cleaned = word.Trim().ToUpper();
        return words.ContainsKey(cleaned) || fallbackWords.Contains(cleaned);
    }

    public string GetDefinition(string word)
    {
        if (string.IsNullOrWhiteSpace(word)) return "";

        string cleaned = word.Trim().ToUpper();

        if (words.TryGetValue(cleaned, out GameplayWordData data))
            return data.definition;

        if (fallbackWords.Contains(cleaned))
            return "Definition unavailable in the gameplay dictionary.";

        return "";
    }

    public string GetRarity(string word)
    {
        if (string.IsNullOrWhiteSpace(word)) return "";

        string cleaned = word.Trim().ToUpper();

        if (words.TryGetValue(cleaned, out GameplayWordData data))
            return data.rarity;

        if (fallbackWords.Contains(cleaned))
            return CalculateRarity(cleaned);

        return "";
    }

    public int GetWordLength(string word)
    {
        if (string.IsNullOrWhiteSpace(word)) return 0;

        string cleaned = word.Trim().ToUpper();

        if (words.TryGetValue(cleaned, out GameplayWordData data))
            return data.length;

        if (fallbackWords.Contains(cleaned))
            return cleaned.Length;

        return 0;
    }

    public string GetRandomLongWord(int minLength, int maxLength)
    {
        return GetRandomWordByLengthRange(minLength, maxLength);
    }

    public string GetRandomWordByLengthRange(int minLength, int maxLength)
    {
        if (minLength == 3 && maxLength == 5 && shortWords.Count > 0)
            return shortWords[UnityEngine.Random.Range(0, shortWords.Count)];

        if (minLength == 6 && maxLength == 9 && mediumWords.Count > 0)
            return mediumWords[UnityEngine.Random.Range(0, mediumWords.Count)];

        if (minLength >= 10 && longWords.Count > 0)
            return longWords[UnityEngine.Random.Range(0, longWords.Count)];

        List<string> candidates = new List<string>();

        foreach (string word in words.Keys)
        {
            if (word.Length >= minLength && word.Length <= maxLength)
                candidates.Add(word);
        }

        if (candidates.Count == 0)
            return "";

        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    private Dictionary<string, GameplayWordData> ParseGameplayDictionary(string json)
    {
        Dictionary<string, GameplayWordData> parsedWords = new Dictionary<string, GameplayWordData>();

        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogError("[DictionaryManager] Dictionary JSON is empty.");
            return parsedWords;
        }

        GameplayDictionaryWrapper wrapper = JsonUtility.FromJson<GameplayDictionaryWrapper>(json);
        if (wrapper?.entries == null)
        {
            Debug.LogError("[DictionaryManager] Dictionary JSON did not contain an entries array.");
            return parsedWords;
        }

        foreach (GameplayWordEntry entry in wrapper.entries)
        {
            if (entry == null) continue;

            string word = string.IsNullOrWhiteSpace(entry.word)
                ? string.Empty
                : entry.word.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(word)) continue;

            parsedWords[word] = new GameplayWordData
            {
                definition = entry.definition,
                length = entry.length > 0 ? entry.length : word.Length,
                rarity = entry.rarity
            };
        }

        if (parsedWords.Count == 0)
        {
            Debug.LogError("[DictionaryManager] No gameplay dictionary entries were parsed.");
        }

        return parsedWords;
    }

    private void LoadFallbackWordList()
    {
        fallbackWords.Clear();

        if (!allowFallbackWordsWithoutDefinitions)
            return;

        TextAsset source = fallbackWordListFile;

        if (source == null && autoLoadFallbackWordList && !string.IsNullOrWhiteSpace(fallbackResourceName))
            source = Resources.Load<TextAsset>(fallbackResourceName);

        if (source == null && autoLoadFallbackWordList)
            source = Resources.Load<TextAsset>("dictionary");

        if (source == null)
        {
            Debug.Log("[DictionaryManager] No fallback word list loaded.");
            return;
        }

        string[] lines = source.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in lines)
        {
            string candidate = line.Trim();
            if (!IsFallbackWordCandidate(candidate))
                continue;

            string upper = candidate.ToUpperInvariant();
            if (words.ContainsKey(upper))
                continue;

            fallbackWords.Add(upper);
        }

        Debug.Log($"[DictionaryManager] Loaded {fallbackWords.Count} fallback validation words.");
    }

    private bool IsFallbackWordCandidate(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
            return false;

        if (word.Length < 3 || word.Length > 16)
            return false;

        if (word != word.ToLowerInvariant())
            return false;

        foreach (char c in word)
        {
            if (c < 'a' || c > 'z')
                return false;
        }

        return true;
    }

    private string CalculateRarity(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
            return "common";

        string cleaned = word.Trim().ToUpperInvariant();
        bool hasRareLetter = false;
        bool hasUncommonLetter = false;
        int rareLetterCount = 0;

        foreach (char c in cleaned)
        {
            if (c == 'Q' || c == 'X' || c == 'Z')
            {
                hasRareLetter = true;
                rareLetterCount++;
            }

            if (c == 'J' || c == 'K' || c == 'V' || c == 'W' || c == 'Y')
                hasUncommonLetter = true;
        }

        if (cleaned.Length >= 14 || rareLetterCount >= 2)
            return "mythic";

        if (hasRareLetter)
            return "rare";

        if (cleaned.Length >= 8 || hasUncommonLetter)
            return "uncommon";

        return "common";
    }
}
