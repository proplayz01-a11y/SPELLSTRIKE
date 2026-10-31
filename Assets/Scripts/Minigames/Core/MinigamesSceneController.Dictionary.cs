using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

// Maintained as a partial MonoBehaviour to preserve all existing Unity scene references.
public partial class MinigamesSceneController
{
    private PracticeEntry GetRandomDefinedEntry(int minLength, int maxLength)
    {
        if (dictionary != null)
        {
            for (int i = 0; i < 80; i++)
            {
                string word = dictionary.GetRandomWordByLengthRange(minLength, maxLength);
                string definition = dictionary.GetDefinition(word);
                if (!string.IsNullOrWhiteSpace(word) && IsUsableDefinition(definition))
                    return new PracticeEntry(word.ToUpperInvariant(), definition);
            }
        }

        List<PracticeEntry> candidates = new List<PracticeEntry>();
        foreach (PracticeEntry entry in FallbackEntries)
        {
            if (entry.word.Length >= minLength && entry.word.Length <= maxLength)
                candidates.Add(entry);
        }

        if (candidates.Count == 0)
            candidates.AddRange(FallbackEntries);

        return candidates[Random.Range(0, candidates.Count)];
    }

    private bool IsFallbackValidWord(string word)
    {
        foreach (PracticeEntry entry in FallbackEntries)
        {
            if (entry.word == word)
                return true;
        }

        return false;
    }

    private bool ContainsOptionDefinition(List<PracticeEntry> entries, string definition)
    {
        foreach (PracticeEntry entry in entries)
        {
            if (entry.definition == definition)
                return true;
        }

        return false;
    }

    private bool CanBuildWordFromSource(string word, string source)
    {
        return WordExcavationRules.CanBuildWordFromSource(word, source);
    }
}
