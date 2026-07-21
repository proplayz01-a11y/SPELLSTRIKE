using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class VocabularyDistractor
{
    [SerializeField] private string word;
    [TextArea(2, 4)] [SerializeField] private string explanation;

    public string Word => string.IsNullOrWhiteSpace(word) ? string.Empty : word.Trim().ToUpperInvariant();
    public string Explanation => explanation;

    public VocabularyDistractor(string distractorWord, string distractorExplanation)
    {
        word = distractorWord;
        explanation = distractorExplanation;
    }
}

[Serializable]
public class VocabularyWordData
{
    [SerializeField] private string wordId = "stage1_mitigate";
    [SerializeField] private string word = "MITIGATE";
    [SerializeField] private string partOfSpeech = "verb";
    [TextArea(2, 4)] [SerializeField] private string definition = "To reduce the severity or harmful effect of something.";
    [TextArea(2, 4)] [SerializeField] private string contextSentence = "The shield helped mitigate the damage.";
    [TextArea(2, 4)] [SerializeField] private string combatClue = "Form the word that means to reduce the severity of harm.";
    [TextArea(2, 4)] [SerializeField] private string reviewQuestion = "Which word means to reduce the severity of a problem?";
    [SerializeField] private List<VocabularyDistractor> distractors = new List<VocabularyDistractor>();

    public string WordId => string.IsNullOrWhiteSpace(wordId) ? Word : wordId.Trim();
    public string Word => string.IsNullOrWhiteSpace(word) ? string.Empty : word.Trim().ToUpperInvariant();
    public string PartOfSpeech => partOfSpeech;
    public string Definition => definition;
    public string ContextSentence => contextSentence;
    public string CombatClue => combatClue;
    public string ReviewQuestion => reviewQuestion;
    public IReadOnlyList<VocabularyDistractor> Distractors => distractors;

    public bool Matches(string candidate)
    {
        return !string.IsNullOrWhiteSpace(candidate) &&
               string.Equals(Word, candidate.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public VocabularyDistractor FindDistractor(string candidate)
    {
        if (distractors == null || string.IsNullOrWhiteSpace(candidate))
            return null;

        return distractors.Find(item => item != null &&
            string.Equals(item.Word, candidate.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public static VocabularyWordData CreateMitigatePrototype()
    {
        VocabularyWordData data = new VocabularyWordData();
        data.distractors = new List<VocabularyDistractor>
        {
            new VocabularyDistractor("INTENSIFY", "Intensify means to make something stronger or more severe."),
            new VocabularyDistractor("EXPAND", "Expand means to increase in size, amount, or scope."),
            new VocabularyDistractor("IGNORE", "Ignore means to deliberately pay no attention to something.")
        };
        return data;
    }
}

[CreateAssetMenu(fileName = "StageVocabularyProfile", menuName = "SPELLSTRIKE/Vocabulary/Stage Vocabulary Profile")]
public class StageVocabularyProfile : ScriptableObject
{
    [Min(1)] [SerializeField] private int stageNumber = 1;
    [SerializeField] private List<VocabularyWordData> words = new List<VocabularyWordData>();

    public int StageNumber => stageNumber;
    public int WordCount => words != null ? words.Count : 0;
    public IReadOnlyList<VocabularyWordData> Words => words;

    public VocabularyWordData GetWord(int index)
    {
        if (words == null || index < 0 || index >= words.Count)
            return null;

        return words[index];
    }

    public void ConfigurePrototype(int configuredStageNumber, List<VocabularyWordData> configuredWords)
    {
        stageNumber = Mathf.Max(1, configuredStageNumber);
        words = configuredWords ?? new List<VocabularyWordData>();
    }
}
