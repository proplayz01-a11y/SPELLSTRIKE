using System.Collections.Generic;
using UnityEngine;

public class DictionaryManager : MonoBehaviour
{
    public static DictionaryManager Instance;
    public TextAsset dictionaryFile; // assign dictionary.txt
    private HashSet<string> words = new HashSet<string>();
    private readonly List<string> shortWords = new List<string>();   // 3-5
    private readonly List<string> mediumWords = new List<string>();  // 6-9
    private readonly List<string> longWords = new List<string>();    // 10+

    public string GetRandomLongWord(int minLength, int maxLength)
    {
        return GetRandomWordByLengthRange(minLength, maxLength);
    }

    public string GetRandomWordByLengthRange(int minLength, int maxLength)
    {
        if (minLength == 3 && maxLength == 5 && shortWords.Count > 0)
            return shortWords[Random.Range(0, shortWords.Count)];

        if (minLength == 6 && maxLength == 9 && mediumWords.Count > 0)
            return mediumWords[Random.Range(0, mediumWords.Count)];

        if (minLength >= 10 && longWords.Count > 0)
            return longWords[Random.Range(0, longWords.Count)];

        List<string> candidates = new List<string>();
        foreach (string word in words)
        {
            if (word.Length >= minLength && word.Length <= maxLength)
                candidates.Add(word);
        }

        if (candidates.Count == 0)
            return "";

        return candidates[Random.Range(0, candidates.Count)];
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
        LoadDictionary();
    }

    void LoadDictionary()
    {
        if (dictionaryFile == null)
        {
            Debug.LogError("No dictionary file assigned!");
            return;
        }

        string[] lines = dictionaryFile.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

        foreach (string line in lines)
        {
            string cleaned = line.Trim().ToUpper();
            if(!string.IsNullOrEmpty(cleaned))
            {
                words.Add(cleaned);
                int len = cleaned.Length;
                if (len >= 3 && len <= 5)
                    shortWords.Add(cleaned);
                else if (len >= 6 && len <= 9)
                    mediumWords.Add(cleaned);
                else if (len >= 10)
                    longWords.Add(cleaned);
            }
        }
            



    }

    public bool IsValidWord(string word)
    {
        return words.Contains(word.Trim().ToUpper());
    }
}
