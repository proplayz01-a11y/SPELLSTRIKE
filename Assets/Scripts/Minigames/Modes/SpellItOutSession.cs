using System;
using System.Collections.Generic;

/// <summary>Independent timed spelling session, including per-word result history.</summary>
public sealed class SpellItOutSession
{
    public string Word { get; set; } = string.Empty;
    public string Definition { get; set; } = string.Empty;
    public int WordIndex { get; set; }
    public float SecondsLeft { get; set; }
    public bool Active { get; set; }
    public List<string> Words { get; } = new List<string>();
    public List<bool> Correct { get; } = new List<bool>();

    public void Begin()
    {
        Word = string.Empty;
        Definition = string.Empty;
        WordIndex = 0;
        SecondsLeft = 0;
        Active = true;
        Words.Clear();
        Correct.Clear();
    }

    public void Present(string word, string definition, float seconds)
    {
        Word = word ?? string.Empty;
        Definition = definition ?? string.Empty;
        SecondsLeft = Math.Max(1f, seconds);
    }

    public bool Tick(float deltaSeconds)
    {
        if (!Active) return false;
        SecondsLeft = Math.Max(0, SecondsLeft - Math.Max(0, deltaSeconds));
        return SecondsLeft <= 0;
    }

    public bool RecordAnswer(string answer)
    {
        bool isCorrect = string.Equals(answer, Word, StringComparison.Ordinal);
        Record(isCorrect);
        return isCorrect;
    }

    public void Record(bool correct)
    {
        Words.Add(string.IsNullOrWhiteSpace(Word) ? "UNKNOWN" : Word);
        Correct.Add(correct);
    }

    public bool Advance(int totalWords) => ++WordIndex >= Math.Max(1, totalWords);
    public void Finish() => Active = false;
}
