using System;
using System.Collections.Generic;

/// <summary>Independent word inventory, timer and scoring for Word Excavation.</summary>
public sealed class WordExcavationSession
{
    public enum SubmissionStatus { Inactive, TooShort, Duplicate, CannotBuild, InvalidWord, Accepted }

    public string SourceWord { get; set; } = string.Empty;
    public float SecondsLeft { get; set; }
    public bool Active { get; set; }
    public int Score { get; private set; }
    public HashSet<string> FoundWords { get; } = new HashSet<string>();
    public List<string> FoundWordsInOrder { get; } = new List<string>();

    public void Begin(string source, float seconds)
    {
        SourceWord = source ?? string.Empty;
        SecondsLeft = seconds;
        Active = true;
        Score = 0;
        FoundWords.Clear();
        FoundWordsInOrder.Clear();
    }

    public bool Tick(float deltaSeconds)
    {
        if (!Active) return false;
        SecondsLeft = Math.Max(0, SecondsLeft - Math.Max(0, deltaSeconds));
        if (SecondsLeft > 0) return false;
        Active = false;
        return true;
    }

    public SubmissionStatus Submit(string word, int minimumLength, Func<string, bool> isValid, float roundSeconds, out int points)
    {
        points = 0;
        if (!Active) return SubmissionStatus.Inactive;
        if (string.IsNullOrEmpty(word) || word.Length < minimumLength) return SubmissionStatus.TooShort;
        if (FoundWords.Contains(word)) return SubmissionStatus.Duplicate;
        if (!WordExcavationRules.CanBuildWordFromSource(word, SourceWord)) return SubmissionStatus.CannotBuild;
        if (isValid == null || !isValid(word)) return SubmissionStatus.InvalidWord;

        FoundWords.Add(word);
        FoundWordsInOrder.Add(word);
        points = WordExcavationRules.GetPoints(word.Length);
        Score += points;
        SecondsLeft = Math.Min(roundSeconds, SecondsLeft + WordExcavationRules.GetTimeBonus(word, SourceWord));
        return SubmissionStatus.Accepted;
    }
}
