using System;
using System.Collections.Generic;

/// <summary>Word Master gameplay state and attempt resolution; no Unity UI or MonoBehaviour dependency.</summary>
public sealed class WordMasterSession
{
    public enum GuessStatus { AlreadySolved, OutOfTurns, WrongLength, Incomplete, NotInDictionary, Accepted, Solved, GameOver }

    public string TargetWord { get; set; } = string.Empty;
    public int Turn { get; set; }
    public int HintIndex { get; set; } = -1;
    public bool Solved { get; set; }
    public bool HintDismissed { get; set; }
    public char[] LockedLetters { get; set; } = Array.Empty<char>();
    public List<string> Guesses { get; } = new List<string>();
    public List<WordMasterGuessRules.Clue[]> GuessClues { get; } = new List<WordMasterGuessRules.Clue[]>();
    public HashSet<int> ConsumedHintPositions { get; } = new HashSet<int>();

    public void Begin(string target, int wordLength, bool useStartHint)
    {
        TargetWord = target ?? string.Empty;
        Turn = 0;
        HintIndex = useStartHint ? 0 : -1;
        Solved = false;
        HintDismissed = false;
        Guesses.Clear();
        GuessClues.Clear();
        ConsumedHintPositions.Clear();
        ResetLockedLetters(wordLength, useStartHint);
    }

    public void ResetLockedLetters(int wordLength, bool useStartHint)
    {
        LockedLetters = new char[Math.Max(0, wordLength)];
        if (useStartHint && HintIndex >= 0 && HintIndex < LockedLetters.Length && HintIndex < TargetWord.Length)
            LockedLetters[HintIndex] = TargetWord[HintIndex];
    }

    public void LockCorrectLetters(string guess, WordMasterGuessRules.Clue[] clues)
    {
        int count = Math.Min(Math.Min(guess.Length, clues.Length), LockedLetters.Length);
        for (int i = 0; i < count; i++)
            if (clues[i] == WordMasterGuessRules.Clue.Correct && i < TargetWord.Length)
                LockedLetters[i] = TargetWord[i];
    }

    public bool IsLocked(int position) =>
        position >= 0 && position < LockedLetters.Length && LockedLetters[position] != '\0';

    public int CountUnlocked(int length)
    {
        int count = 0;
        for (int i = 0; i < length; i++)
            if (!IsLocked(i)) count++;
        return count;
    }

    public GuessStatus Submit(string guess, int length, int maxTurns, Func<string, bool> isValid, out WordMasterGuessRules.Clue[] clues)
    {
        clues = null;
        if (Solved) return GuessStatus.AlreadySolved;
        if (Turn >= maxTurns) return GuessStatus.OutOfTurns;
        if (guess == null || guess.Length != length) return GuessStatus.WrongLength;
        if (guess.Contains(" ")) return GuessStatus.Incomplete;
        if (isValid == null || !isValid(guess)) return GuessStatus.NotInDictionary;

        clues = WordMasterGuessRules.Evaluate(guess, TargetWord);
        Guesses.Add(guess);
        GuessClues.Add(clues);
        LockCorrectLetters(guess, clues);
        Turn++;

        if (guess == TargetWord || CountUnlocked(length) == 0)
        {
            Solved = true;
            return GuessStatus.Solved;
        }
        return Turn >= maxTurns ? GuessStatus.GameOver : GuessStatus.Accepted;
    }
}
