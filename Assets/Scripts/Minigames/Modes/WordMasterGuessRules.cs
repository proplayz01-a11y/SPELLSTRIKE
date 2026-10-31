using System;

/// <summary>
/// Pure, engine-independent Word Master rules. Repeated letters are consumed
/// only once, with exact-position matches taking priority.
/// </summary>
public static class WordMasterGuessRules
{
    public enum Clue
    {
        Missing,
        Present,
        Correct
    }

    public static Clue[] Evaluate(string guess, string target)
    {
        if (guess == null)
            throw new ArgumentNullException(nameof(guess));
        if (target == null)
            throw new ArgumentNullException(nameof(target));

        Clue[] clues = new Clue[guess.Length];
        bool[] targetUsed = new bool[target.Length];
        bool[] guessUsed = new bool[guess.Length];

        for (int i = 0; i < guess.Length && i < target.Length; i++)
        {
            if (guess[i] != target[i])
                continue;

            clues[i] = Clue.Correct;
            targetUsed[i] = true;
            guessUsed[i] = true;
        }

        for (int i = 0; i < guess.Length; i++)
        {
            if (guessUsed[i])
                continue;

            clues[i] = Clue.Missing;
            for (int j = 0; j < target.Length; j++)
            {
                if (targetUsed[j] || guess[i] != target[j])
                    continue;

                clues[i] = Clue.Present;
                targetUsed[j] = true;
                break;
            }
        }

        return clues;
    }
}
