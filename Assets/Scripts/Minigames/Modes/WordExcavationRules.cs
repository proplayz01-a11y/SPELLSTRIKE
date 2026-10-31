using System.Collections.Generic;

/// <summary>
/// Pure Word Excavation validation and rewards, independent of the UI and timer.
/// </summary>
public static class WordExcavationRules
{
    public static bool CanBuildWordFromSource(string word, string source)
    {
        if (word == null || source == null)
            return false;

        Dictionary<char, int> available = new Dictionary<char, int>();
        foreach (char c in source)
        {
            if (!available.ContainsKey(c))
                available[c] = 0;
            available[c]++;
        }

        foreach (char c in word)
        {
            if (!available.ContainsKey(c) || available[c] <= 0)
                return false;
            available[c]--;
        }

        return true;
    }

    public static int GetPoints(int wordLength)
    {
        return wordLength > 3 ? wordLength - 2 : 1;
    }

    public static int GetTimeBonus(string answer, string sourceWord)
    {
        if (string.IsNullOrWhiteSpace(answer))
            return 0;

        int length = answer.Length;
        if (!string.IsNullOrWhiteSpace(sourceWord) && length == sourceWord.Length)
            return 10;
        if (length >= 7)
            return 7;
        if (length == 6)
            return 5;
        if (length == 5)
            return 3;
        return length >= 3 ? 1 : 0;
    }
}
