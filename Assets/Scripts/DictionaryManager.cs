using System.Collections.Generic;
using UnityEngine;

public class DictionaryManager : MonoBehaviour
{
    public static DictionaryManager Instance;
    public TextAsset dictionaryFile; // assign dictionary.txt
    private HashSet<string> words = new HashSet<string>();

    public string GetRandomLongWord(int minLength, int maxLength)
    {
        List<string> candidates = new List<string>();
        foreach (string word in words)
        {
            if (word.Length >= minLength && word.Length <= maxLength)
                candidates.Add(word);
        }
        if (candidates.Count == 0)
            return "";
        int index = Random.Range(0, candidates.Count);
        return candidates[index];
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
                words.Add(cleaned);
        }
            



    }

    public bool IsValidWord(string word)
    {
        return words.Contains(word.Trim().ToUpper());
    }
}