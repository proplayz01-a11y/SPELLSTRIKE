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
    private void StartSpellItOutRound()
    {
        ApplySpellItOutPlayLayout();
        SetTitle("SPELL IT OUT");
        SetObjectActive(answerInput, false);
        SetObjectActive(foundWordsText, false);
        SetObjectActive(timerText, true);
        SetObjectActive(scoreText, true);
        SetObjectActive(optionsRoot, false);
        SetTilePickerVisible(true);

        score = 0;
        roundsPlayed = 0;
        spellingSession.Begin();

        StartNextSpellItOutWord();
        RefreshScoreText();
    }

    private void StartNextSpellItOutWord()
    {
        if (spellItOutWordIndex >= Mathf.Max(1, spellItOutTotalWords))
        {
            FinishSpellItOutSession("Complete");
            return;
        }

        int minLength = GetSpellItOutStageMinLength(spellItOutWordIndex);
        int maxLength = GetSpellItOutStageMaxLength(spellItOutWordIndex);
        PracticeEntry entry = GetRandomSpellItOutEntry(minLength, maxLength);
        spellingSession.Present(entry.word, entry.definition, spellItOutSecondsPerWord);
        SetFeedback(string.Empty, lightTextColor);

        if (promptText != null)
            promptText.text = $"Spell the word that matches this definition:\n<size=30>{currentDefinition}</size>\n\nHint: {BuildLetterHint(currentWord)}";

        if (tileInstructionText != null)
            tileInstructionText.text = "Click the jumbled tiles in the correct order.";

        ConfigureTilePicker(GetShuffledLetters(currentWord));
        UpdateTimerText();
        RefreshScoreText();
    }

    private void ResolveSpellItOut(string answer)
    {
        if (!spellItOutActive)
            return;

        roundsPlayed++;

        bool correct = spellingSession.RecordAnswer(answer);

        if (correct)
        {
            score++;
            SetFeedback("Correct.", goodColor);
        }
        else
        {
            SetFeedback($"Answer: {currentWord}", badColor);
        }

        ClearTileSelection();
        RefreshScoreText();
        AdvanceSpellItOutSession();
    }

    private void HandleSpellItOutWordTimeout()
    {
        if (!spellItOutActive)
            return;

        roundsPlayed++;
        spellingSession.Record(false);
        SetFeedback($"Time. Answer: {currentWord}", badColor);
        ClearTileSelection();
        RefreshScoreText();
        AdvanceSpellItOutSession();
    }

    private void RecordSpellItOutWordResult(bool correct)
    {
        spellingSession.Record(correct);
    }

    private void AdvanceSpellItOutSession()
    {
        if (spellingSession.Advance(spellItOutTotalWords))
        {
            FinishSpellItOutSession("Complete");
            return;
        }

        StartNextSpellItOutWord();
    }

    private void FinishSpellItOutSession(string title)
    {
        spellingSession.Finish();
        ClearTileSelection();
        UpdateTimerText();
        RefreshScoreText();
        ShowMinigameResultOverlay(title);
    }

    private string BuildSpellItOutResultWordsText()
    {
        if (spellItOutSessionWords.Count == 0)
            return $"Words: {roundsPlayed}/{Mathf.Max(1, spellItOutTotalWords)}";

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Secret words:");

        for (int i = 0; i < spellItOutSessionWords.Count; i++)
        {
            bool correct = i < spellItOutSessionCorrect.Count && spellItOutSessionCorrect[i];
            builder.Append(i + 1);
            builder.Append(". ");
            builder.Append(spellItOutSessionWords[i]);
            builder.Append(correct ? " - correct" : " - missed");

            if (i < spellItOutSessionWords.Count - 1)
                builder.AppendLine();
        }

        return builder.ToString();
    }

    private int GetSpellItOutStageMinLength(int stageIndex)
    {
        switch (stageIndex)
        {
            case 0:
                return 5;
            case 1:
                return 6;
            case 2:
                return 7;
            case 3:
                return 8;
            default:
                return 9;
        }
    }

    private int GetSpellItOutStageMaxLength(int stageIndex)
    {
        switch (stageIndex)
        {
            case 0:
                return 6;
            case 1:
                return 7;
            case 2:
                return 8;
            case 3:
                return 9;
            default:
                return 10;
        }
    }

    private PracticeEntry GetRandomSpellItOutEntry(int minLength, int maxLength)
    {
        List<PracticeEntry> curatedCandidates = GetSpellItOutCuratedCandidates(minLength, maxLength);
        if (curatedCandidates.Count > 0)
            return curatedCandidates[Random.Range(0, curatedCandidates.Count)];

        PracticeEntry bestEntry = default;
        int bestScore = int.MaxValue;
        bool hasBestEntry = false;

        if (dictionary != null)
        {
            for (int i = 0; i < SpellItOutCandidateAttempts; i++)
            {
                string word = dictionary.GetRandomWordByLengthRange(minLength, maxLength);
                if (string.IsNullOrWhiteSpace(word))
                    continue;

                word = word.Trim().ToUpperInvariant();
                string rawDefinition = dictionary.GetDefinition(word);
                if (!TryGetCleanSpellItOutDefinition(word, rawDefinition, out string cleanDefinition, out int score))
                    continue;

                if (score >= bestScore)
                    continue;

                bestEntry = new PracticeEntry(word, cleanDefinition);
                bestScore = score;
                hasBestEntry = true;
            }
        }

        if (hasBestEntry)
            return bestEntry;

        List<PracticeEntry> fallbackCandidates = new List<PracticeEntry>();
        foreach (PracticeEntry entry in SpellItOutFallbackEntries)
        {
            if (entry.word.Length >= minLength && entry.word.Length <= maxLength)
                fallbackCandidates.Add(entry);
        }

        if (fallbackCandidates.Count == 0)
            fallbackCandidates.AddRange(SpellItOutFallbackEntries);

        return fallbackCandidates[Random.Range(0, fallbackCandidates.Count)];
    }

    private List<PracticeEntry> GetSpellItOutCuratedCandidates(int minLength, int maxLength)
    {
        EnsureSpellItOutCuratedEntriesLoaded();

        List<PracticeEntry> candidates = new List<PracticeEntry>();
        foreach (PracticeEntry entry in spellItOutCuratedEntries)
        {
            if (entry.word.Length >= minLength && entry.word.Length <= maxLength)
                candidates.Add(entry);
        }

        return candidates;
    }

    private void EnsureSpellItOutCuratedEntriesLoaded()
    {
        if (spellItOutCuratedEntriesLoaded)
            return;

        spellItOutCuratedEntriesLoaded = true;
        spellItOutCuratedEntries.Clear();

        TextAsset source = spellItOutDictionaryFile;
        if (source == null && !string.IsNullOrWhiteSpace(spellItOutResourceName))
            source = Resources.Load<TextAsset>(spellItOutResourceName);

        if (source == null || string.IsNullOrWhiteSpace(source.text))
            return;

        SpellItOutDictionaryWrapper wrapper = JsonUtility.FromJson<SpellItOutDictionaryWrapper>(source.text);
        if (wrapper?.entries == null)
            return;

        foreach (SpellItOutDictionaryEntry entry in wrapper.entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.word))
                continue;

            string word = entry.word.Trim().ToUpperInvariant();
            string definition = entry.definition;
            if (!TryGetCleanSpellItOutDefinition(word, definition, out string cleanDefinition, out _))
                continue;

            spellItOutCuratedEntries.Add(new PracticeEntry(word, cleanDefinition));
        }
    }

    private bool TryGetCleanSpellItOutDefinition(string word, string rawDefinition, out string cleanDefinition, out int score)
    {
        cleanDefinition = string.Empty;
        score = int.MaxValue;

        if (string.IsNullOrWhiteSpace(word))
            return false;

        string upperWord = word.Trim().ToUpperInvariant();
        if (SpellItOutDefinitionOverrides.TryGetValue(upperWord, out string overrideDefinition))
        {
            cleanDefinition = NormalizeDefinitionText(overrideDefinition);
            score = 0;
            return true;
        }

        cleanDefinition = NormalizeDefinitionText(rawDefinition);
        if (string.IsNullOrWhiteSpace(cleanDefinition))
            return false;

        if (!IsUsableDefinition(cleanDefinition))
            return false;

        if (cleanDefinition.Length > SpellItOutDefinitionMaxLength)
            return false;

        if (cleanDefinition.Contains(";"))
            return false;

        if (cleanDefinition.Contains("--") || cleanDefinition.Contains("—"))
            return false;

        if (IsDictionaryRedirectDefinition(cleanDefinition))
            return false;

        if (ContainsForbiddenSpellItOutTerm(cleanDefinition))
            return false;

        if (ContainsAnswerOrCloseInflection(upperWord, cleanDefinition))
            return false;

        score = cleanDefinition.Length;
        if (cleanDefinition.Contains(","))
            score += 12;

        if (CountSentenceEnders(cleanDefinition) > 1)
            score += 30;

        return true;
    }

    private string NormalizeDefinitionText(string definition)
    {
        if (string.IsNullOrWhiteSpace(definition))
            return string.Empty;

        string[] parts = definition.Trim().Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", parts);
    }

    private bool ContainsForbiddenSpellItOutTerm(string definition)
    {
        if (string.IsNullOrWhiteSpace(definition))
            return true;

        foreach (string term in SpellItOutForbiddenDefinitionTerms)
        {
            if (definition.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        string lower = definition.ToLowerInvariant();
        return lower.StartsWith("l.") || lower.Contains(" l.");
    }

    private bool IsDictionaryRedirectDefinition(string definition)
    {
        if (string.IsNullOrWhiteSpace(definition))
            return true;

        string lower = definition.Trim().ToLowerInvariant();
        return lower.StartsWith("see ")
            || lower.StartsWith("see also ")
            || lower.StartsWith("same as ")
            || lower.StartsWith("variant of ")
            || lower.StartsWith("alternative form of ")
            || lower.StartsWith("alternate form of ")
            || lower.StartsWith("plural of ")
            || lower.StartsWith("past tense of ")
            || lower.StartsWith("present participle of ")
            || lower.StartsWith("a form of ")
            || lower.StartsWith("form of ")
            || lower.StartsWith("archaic form of ")
            || lower.StartsWith("obsolete form of ");
    }

    private bool ContainsAnswerOrCloseInflection(string word, string definition)
    {
        HashSet<string> variants = BuildAnswerLeakVariants(word);
        foreach (string token in TokenizeDefinitionWords(definition))
        {
            if (variants.Contains(token))
                return true;
        }

        return false;
    }

    private HashSet<string> BuildAnswerLeakVariants(string word)
    {
        string lowerWord = word.Trim().ToLowerInvariant();
        HashSet<string> variants = new HashSet<string>();
        if (string.IsNullOrWhiteSpace(lowerWord))
            return variants;

        variants.Add(lowerWord);
        variants.Add(lowerWord + "s");
        variants.Add(lowerWord + "es");
        variants.Add(lowerWord + "ed");
        variants.Add(lowerWord + "ing");
        variants.Add(lowerWord + "ly");

        if (lowerWord.Length > 4 && lowerWord.EndsWith("s"))
            variants.Add(lowerWord.Substring(0, lowerWord.Length - 1));

        if (lowerWord.Length > 5 && lowerWord.EndsWith("es"))
            variants.Add(lowerWord.Substring(0, lowerWord.Length - 2));

        if (lowerWord.Length > 4 && lowerWord.EndsWith("e"))
        {
            variants.Add(lowerWord.Substring(0, lowerWord.Length - 1));
            variants.Add(lowerWord.Substring(0, lowerWord.Length - 1) + "ing");
        }

        if (lowerWord.Length > 4 && lowerWord.EndsWith("y"))
        {
            string stem = lowerWord.Substring(0, lowerWord.Length - 1);
            variants.Add(stem);
            variants.Add(stem + "ies");
            variants.Add(stem + "ily");
            variants.Add(stem + "ly");
        }

        if (lowerWord.Length > 6 && lowerWord.EndsWith("ing"))
            variants.Add(lowerWord.Substring(0, lowerWord.Length - 3));

        if (lowerWord.Length > 5 && lowerWord.EndsWith("ed"))
            variants.Add(lowerWord.Substring(0, lowerWord.Length - 2));

        return variants;
    }

    private List<string> TokenizeDefinitionWords(string definition)
    {
        List<string> tokens = new List<string>();
        if (string.IsNullOrWhiteSpace(definition))
            return tokens;

        StringBuilder builder = new StringBuilder();
        foreach (char c in definition.ToLowerInvariant())
        {
            if (char.IsLetter(c))
            {
                builder.Append(c);
                continue;
            }

            if (builder.Length == 0)
                continue;

            tokens.Add(builder.ToString());
            builder.Clear();
        }

        if (builder.Length > 0)
            tokens.Add(builder.ToString());

        return tokens;
    }

    private int CountSentenceEnders(string text)
    {
        int count = 0;
        foreach (char c in text)
        {
            if (c == '.' || c == '?' || c == '!')
                count++;
        }

        return count;
    }

    private bool IsUsableDefinition(string definition)
    {
        if (string.IsNullOrWhiteSpace(definition))
            return false;

        return !definition.Contains("unavailable");
    }

    private string BuildLetterHint(string word)
    {
        if (string.IsNullOrEmpty(word))
            return string.Empty;

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < word.Length; i++)
        {
            if (i > 0)
                builder.Append(' ');

            builder.Append('_');
        }

        return builder.ToString();
    }

    private string GetShuffledLetters(string word)
    {
        if (string.IsNullOrWhiteSpace(word) || word.Length <= 1)
            return word;

        List<char> letters = new List<char>(word.ToUpperInvariant().ToCharArray());
        string original = new string(letters.ToArray());

        for (int attempt = 0; attempt < 6; attempt++)
        {
            Shuffle(letters);
            string shuffled = new string(letters.ToArray());
            if (shuffled != original)
                return shuffled;
        }

        return new string(letters.ToArray());
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }
    }
}
