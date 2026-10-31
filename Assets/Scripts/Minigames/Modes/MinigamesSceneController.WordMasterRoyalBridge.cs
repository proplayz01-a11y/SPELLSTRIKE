using System;
using UnityEngine;
using LetterClue = WordMasterGuessRules.Clue;

// Thin view-model adapter. The existing WordMasterSession remains authoritative;
// it owns target generation, the dictionary, turn acceptance, and duplicate-letter evaluation.
public partial class MinigamesSceneController
{
    public bool RoyalReady => currentMode == MinigameMode.WordMaster
        && !string.IsNullOrEmpty(wordMasterTargetWord)
        && wordMasterTargetWord.Length == wordMasterWordLength
        && currentSourceLetters != null && currentSourceLetters.Length == 26;

    private int royalPrizePoints;
    public int RoyalPrizePoints => royalPrizePoints;
    public int RoyalWordsSolved => score;

    // Score tracks prize points; the existing score field counts solved words.
    private void RoyalAwardPrizeForSolvedTurn()
    {
        int[] prizes = { 10000, 5000, 2500, 1500, 500 };
        int index = Mathf.Clamp(wordMasterTurn - 1, 0, prizes.Length - 1);
        royalPrizePoints += prizes[index];
    }

    public int RoyalTurn => wordMasterTurn;
    public int RoyalMaxTurns => wordMasterMaxTurns;
    public int RoyalLength => wordMasterWordLength;
    public bool RoyalSolved => wordMasterSolved;
    public bool RoyalGameOver => !wordMasterSolved && wordMasterTurn >= wordMasterMaxTurns;
    public string RoyalFeedback
    {
        get
        {
            string feedback = feedbackText != null ? feedbackText.text : string.Empty;
            if (!RoyalReady || RoyalSolved || RoyalGameOver)
                return feedback;
            string guess = GetWordMasterCurrentGuess();
            if (guess.Length == wordMasterWordLength && guess.IndexOf(' ') < 0
                && !(dictionary != null ? dictionary.IsValidWord(guess) : IsFallbackValidWord(guess)))
                return "Not in dictionary.";
            return feedback;
        }
    }
    public bool RoyalShowHint => wordMasterTurn == 0 && !wordMasterHintPanelDismissed
        && wordMasterUseStartHint && wordMasterHintIndex >= 0
        && wordMasterHintIndex < wordMasterTargetWord.Length;
    public char RoyalHintLetter => RoyalShowHint ? wordMasterTargetWord[wordMasterHintIndex] : '\0';
    public string RoyalEndWord => (RoyalSolved || RoyalGameOver) ? wordMasterTargetWord : string.Empty;

    public string RoyalGuess(int row)
    {
        if (row < 0 || row >= wordMasterMaxTurns)
            return string.Empty;
        if (row < wordMasterGuesses.Count)
            return wordMasterGuesses[row];
        if (row == wordMasterTurn && !wordMasterSolved)
            return GetWordMasterCurrentGuess();
        return string.Empty;
    }

    // Board code: 0 empty; 1 normal/current; 2 gold; 3 silver; 4 dark-gray.
    public int RoyalCellState(int row, int col)
    {
        if (row < 0 || row >= wordMasterMaxTurns || col < 0 || col >= wordMasterWordLength)
            return 0;

        if (row < wordMasterGuessClues.Count)
        {
            LetterClue[] clues = wordMasterGuessClues[row];
            if (clues == null || col >= clues.Length)
                return 0;
            if (clues[col] == LetterClue.Correct) return 2;
            if (clues[col] == LetterClue.Present) return 3;
            return 4;
        }

        if (row == wordMasterTurn && !wordMasterSolved)
        {
            if (IsWordMasterLockedPosition(col)) return 2;
            string guess = GetWordMasterCurrentGuess();
            return col < guess.Length && guess[col] >= 'A' && guess[col] <= 'Z' ? 1 : 0;
        }
        return 0;
    }

    // Keyboard priority: gold > silver > red > unused. Duplicate guesses are evaluated
    // through the existing WordMasterGuessRules rather than letter-set guesses.
    public int RoyalKeyState(char letter)
    {
        int best = 0;
        for (int row = 0; row < wordMasterGuesses.Count && row < wordMasterGuessClues.Count; row++)
        {
            string guess = wordMasterGuesses[row];
            LetterClue[] clues = wordMasterGuessClues[row];
            if (string.IsNullOrEmpty(guess) || clues == null)
                continue;
            for (int col = 0; col < guess.Length && col < clues.Length; col++)
            {
                if (guess[col] != letter) continue;
                int rank = clues[col] == LetterClue.Correct ? 3
                         : clues[col] == LetterClue.Present ? 2 : 1;
                if (rank > best) best = rank;
            }
        }
        return best; // 0=normal, 1=red, 2=silver, 3=gold
    }

    public bool RoyalCanSubmit
    {
        get
        {
            if (!RoyalReady || RoyalSolved || RoyalGameOver)
                return false;
            string guess = GetWordMasterCurrentGuess();
            return guess.Length == wordMasterWordLength && guess.IndexOf(' ') < 0
                && (dictionary != null ? dictionary.IsValidWord(guess) : IsFallbackValidWord(guess));
        }
    }

    public void RoyalPress(char letter)
    {
        if (!RoyalReady || RoyalSolved || RoyalGameOver)
            return;
        int sourceIndex = WordMasterSourceAlphabet.IndexOf(char.ToUpperInvariant(letter));
        if (sourceIndex >= 0)
            SelectSourceTile(sourceIndex);
    }

    public void RoyalRemoveAt(int col)
    {
        if (RoyalReady && !RoyalSolved && !RoyalGameOver && !IsWordMasterLockedPosition(col))
            RemoveWordMasterLettersFromGuessPosition(col);
    }

    public void RoyalSubmit()
    {
        if (RoyalReady && !RoyalSolved && !RoyalGameOver)
            SubmitTypedAnswer();
    }

    public void RoyalRestart() => StartNewRound();
    public void RoyalExitToModeSelect() => ReturnToModeSelect();
    public void RoyalExitToMainMenu() => ReturnToMainMenu();
    public void RoyalDismissHint() => wordMasterHintPanelDismissed = true;
}
