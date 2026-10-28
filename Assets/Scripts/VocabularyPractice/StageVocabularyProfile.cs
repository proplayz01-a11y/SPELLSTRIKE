using System;
using System.Collections.Generic;
using UnityEngine;

public enum TilePoolMode
{
    NormalCombat = 0,
    TargetChallenge = 1
}

public enum TargetChallengeSubmissionResult
{
    NotActive = 0,
    PendingImpact = 1,
    IncorrectTarget = 2,
    Completed = 3,
    InvalidBarrier = 4,
    CleanupFailed = 5,
    RestorationFailed = 6
}

public enum VocabularyOpportunityTiming
{
    Unassigned = 0,
    PreCombatGate = 1,
    MidCombatState = 2,
    ReactiveSpecial = 3,
    ScriptedBossState = 4
}

public interface ITilePoolModeProvider
{
    TilePoolMode CurrentMode { get; }
    VocabularyWordData ActiveTargetWord { get; }
}

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
public class VocabularyReviewChoice
{
    [TextArea(2, 4)] [SerializeField] private string sentence;
    [SerializeField] private bool correct;
    [TextArea(2, 4)] [SerializeField] private string explanation;

    public string Sentence => sentence;
    public bool Correct => correct;
    public string Explanation => explanation;

    public VocabularyReviewChoice(string configuredSentence, bool isCorrect, string configuredExplanation)
    {
        sentence = configuredSentence;
        correct = isCorrect;
        explanation = configuredExplanation;
    }
}

[Serializable]
public class VocabularyWordData
{
    [SerializeField] private string wordId = "stage1_mitigate";
    [SerializeField] private string word = "MITIGATE";
    [SerializeField] private string encounterId = "stage1_bramble_sprite";
    [SerializeField] private VocabularyOpportunityTiming opportunityTiming = VocabularyOpportunityTiming.PreCombatGate;
    [SerializeField] private string successEffectId = "mitigate_fragment_barrier";
    [SerializeField] private string partOfSpeech = "verb";
    [Tooltip("General American IPA transcription used by the Teach presentation.")]
    [SerializeField] private string phoneticSpelling;
    [TextArea(2, 4)] [SerializeField] private string definition = "To reduce the severity or harmful effect of something.";
    [TextArea(2, 4)] [SerializeField] private string contextSentence = "The shield helped mitigate the damage.";
    [TextArea(2, 4)] [SerializeField] private string combatClue = "Form the word that means to reduce the severity of harm.";
    [Tooltip("Extra letters shown with the target letters in the temporary Target Challenge pool.")]
    [SerializeField] private string challengeDistractorLetters = "RONSLPUD";
    [TextArea(2, 4)] [SerializeField] private string reviewQuestion = "Which word means to reduce the severity of a problem?";
    [SerializeField] private List<VocabularyDistractor> distractors = new List<VocabularyDistractor>();
    [SerializeField] private List<VocabularyReviewChoice> reviewChoices = new List<VocabularyReviewChoice>();

    public string WordId => string.IsNullOrWhiteSpace(wordId) ? Word : wordId.Trim();
    public string Word => string.IsNullOrWhiteSpace(word) ? string.Empty : word.Trim().ToUpperInvariant();
    public string EncounterId => string.IsNullOrWhiteSpace(encounterId) ? string.Empty : encounterId.Trim();
    public VocabularyOpportunityTiming OpportunityTiming => opportunityTiming;
    public string SuccessEffectId => string.IsNullOrWhiteSpace(successEffectId) ? string.Empty : successEffectId.Trim();
    public string PartOfSpeech => partOfSpeech;
    public string PhoneticSpelling => phoneticSpelling;
    public string Definition => definition;
    public string ContextSentence => contextSentence;
    public string CombatClue => combatClue;
    public string ChallengeDistractorLetters => NormalizeLetters(challengeDistractorLetters);
    public string ReviewQuestion => reviewQuestion;
    public IReadOnlyList<VocabularyDistractor> Distractors => distractors;
    public IReadOnlyList<VocabularyReviewChoice> ReviewChoices => reviewChoices;

    public bool Matches(string candidate)
    {
        return !string.IsNullOrWhiteSpace(candidate) &&
               string.Equals(Word, candidate.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public static VocabularyWordData CreateRuntimeChallenge(
        string challengeWord,
        string distractorLetters,
        string runtimeWordId = "stage3_riddle_runtime")
    {
        VocabularyWordData data = new VocabularyWordData
        {
            wordId = string.IsNullOrWhiteSpace(runtimeWordId)
                ? "stage3_riddle_runtime"
                : runtimeWordId.Trim(),
            word = string.IsNullOrWhiteSpace(challengeWord)
                ? string.Empty
                : challengeWord.Trim().ToUpperInvariant(),
            encounterId = "stage3_riddle_trial",
            opportunityTiming = VocabularyOpportunityTiming.ScriptedBossState,
            successEffectId = "stage3_riddle_correct",
            challengeDistractorLetters = distractorLetters ?? string.Empty
        };

        return data;
    }

    /// <summary>
    /// Persistent, contextual vocabulary-review entry for one of Stage 3's
    /// actual four Titan riddle answers. The in-combat riddle remains unchanged.
    /// </summary>
    public static VocabularyWordData CreateStage3RiddleReviewWord(
        string configuredWord,
        string configuredDefinition,
        string configuredContextSentence,
        string correctSentence,
        string incorrectSentenceOne,
        string incorrectSentenceTwo,
        string incorrectSentenceThree)
    {
        string normalizedWord = (configuredWord ?? string.Empty).Trim().ToUpperInvariant();
        return new VocabularyWordData
        {
            wordId = "stage3_node4_riddle_" + normalizedWord.ToLowerInvariant(),
            word = normalizedWord,
            encounterId = "stage3_node4_riddle",
            opportunityTiming = VocabularyOpportunityTiming.ScriptedBossState,
            successEffectId = "stage3_riddle_correct",
            partOfSpeech = "noun",
            definition = configuredDefinition,
            contextSentence = configuredContextSentence,
            combatClue = string.Empty,
            challengeDistractorLetters = string.Empty,
            reviewQuestion = $"Which sentence correctly uses the word {normalizedWord}?",
            distractors = new List<VocabularyDistractor>(),
            reviewChoices = new List<VocabularyReviewChoice>
            {
                new VocabularyReviewChoice(correctSentence, true,
                    $"{normalizedWord} is used correctly in this sentence."),
                new VocabularyReviewChoice(incorrectSentenceOne, false,
                    $"This does not match the meaning of {normalizedWord}."),
                new VocabularyReviewChoice(incorrectSentenceTwo, false,
                    $"This does not match the meaning of {normalizedWord}."),
                new VocabularyReviewChoice(incorrectSentenceThree, false,
                    $"This does not match the meaning of {normalizedWord}.")
            }
        };
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

    public static VocabularyWordData CreateStage1MitigateCandidate()
    {
        return CreateStage1Candidate(
            "stage1_mitigate",
            "MITIGATE",
            "stage1_bramble_sprite",
            VocabularyOpportunityTiming.PreCombatGate,
            "mitigate_fragment_barrier",
            "To reduce the seriousness or harmful effect of something.",
            "The emergency plan helped mitigate the damage caused by the flood.",
            "Form the word that means to reduce a harmful effect.",
            "RONSLPUD",
            new List<VocabularyReviewChoice>
            {
                new VocabularyReviewChoice(
                    "The engineers installed drainage channels to mitigate the risk of flooding.",
                    true,
                    "Mitigate correctly means to reduce the seriousness or harmful effect of a risk."),
                new VocabularyReviewChoice(
                    "The committee mitigated the report into three separate folders.",
                    false,
                    "Mitigate does not mean to divide or organize something."),
                new VocabularyReviewChoice(
                    "The student mitigated the textbook by reading every chapter.",
                    false,
                    "Mitigate does not mean to read or complete something."),
                new VocabularyReviewChoice(
                    "The chef mitigated the vegetables into the serving bowl.",
                    false,
                    "Mitigate does not mean to transfer or place an object somewhere.")
            });
    }

    public static VocabularyWordData CreateStage1FractureCandidate()
    {
        return CreateStage1Candidate(
            "stage1_fracture",
            "FRACTURE",
            "stage1_stone_sentinel",
            VocabularyOpportunityTiming.MidCombatState,
            "fracture_reinforced_stone",
            "To break or crack something.",
            "The heavy impact was strong enough to fracture the stone.",
            "Form the word that means to break or crack something.",
            "BLONISDM",
            new List<VocabularyReviewChoice>
            {
                new VocabularyReviewChoice(
                    "The force of the collision could fracture the reinforced stone.",
                    true,
                    "Fracture correctly means to break or crack something."),
                new VocabularyReviewChoice(
                    "The nurse used a cast to fracture the patient's injured arm.",
                    false,
                    "A cast supports an injury; fracture means to cause a break or crack."),
                new VocabularyReviewChoice(
                    "The editor fractured the spelling error in the final paragraph.",
                    false,
                    "Fracture does not mean to correct or revise an error."),
                new VocabularyReviewChoice(
                    "The speaker fractured the audience by greeting them politely.",
                    false,
                    "Fracture does not mean to greet or address people.")
            });
    }

    public static VocabularyWordData CreateStage1NullifyCandidate()
    {
        return CreateStage1Candidate(
            "stage1_nullify",
            "NULLIFY",
            "stage1_cursed_jester",
            VocabularyOpportunityTiming.PreCombatGate,
            "nullify_jester_barrier",
            "To make something ineffective by counteracting its effect.",
            "The countermeasure helped nullify the harmful effect.",
            "Form the word that means to make an effect ineffective.",
            "ARETOSPDM",
            new List<VocabularyReviewChoice>
            {
                new VocabularyReviewChoice(
                    "The updated security patch can nullify the effect of the malicious code.",
                    true,
                    "Nullify correctly means to make an effect ineffective or without force."),
                new VocabularyReviewChoice(
                    "The chef nullified the soup into four serving bowls.",
                    false,
                    "Nullify does not mean to divide or serve something."),
                new VocabularyReviewChoice(
                    "The student nullified the article by reading it carefully.",
                    false,
                    "Nullify does not mean to read or examine information."),
                new VocabularyReviewChoice(
                    "The courier nullified the package across the city.",
                    false,
                    "Nullify does not mean to carry or deliver an object.")
            });
    }

    public static VocabularyWordData CreateStage1LiberateCandidate()
    {
        return CreateStage1Candidate(
            "stage1_liberate",
            "LIBERATE",
            "stage1_hollow_knight",
            VocabularyOpportunityTiming.ScriptedBossState,
            "liberate_bound_fragment",
            "To free someone or something from confinement or control.",
            "The rescue team worked to liberate the captives.",
            "Form the word that means to free something from control.",
            "CONFUSDM",
            new List<VocabularyReviewChoice>
            {
                new VocabularyReviewChoice(
                    "The rescue team worked to liberate the captives from the locked compound.",
                    true,
                    "Liberate correctly means to free someone or something from confinement or control."),
                new VocabularyReviewChoice(
                    "The guard used additional chains to liberate the prisoner.",
                    false,
                    "Adding restraints confines a prisoner; liberate means to set someone free."),
                new VocabularyReviewChoice(
                    "The technician liberated the computer by increasing its screen brightness.",
                    false,
                    "Liberate does not mean to adjust a device setting."),
                new VocabularyReviewChoice(
                    "The chef liberated the vegetables in boiling water.",
                    false,
                    "Liberate does not mean to cook or prepare food.")
            });
    }

    public static VocabularyWordData CreateStage2SubmergeCandidate()
    {
        return CreateStage2Candidate(
            "stage2_submerge", "SUBMERGE", "stage2_barnacle_husk",
            VocabularyOpportunityTiming.PreCombatGate, "submerge_barnacle_barrier", "verb", "/səbˈmɜːrdʒ/",
            "To cause something to go completely under water.",
            "The storm surge can submerge low roads near the coast.",
            "Form the word that means to put something completely under water.", "TALONSPY",
            "Heavy rain can submerge low roads beneath water.",
            "The captain submerged the route by drawing it on a map.",
            "The sailor submerged the compass by polishing it.",
            "The crew submerged the anchor by tying it to the mast.");
    }

    public static VocabularyWordData CreateStage2NavigateCandidate()
    {
        return CreateStage2Candidate(
            "stage2_navigate", "NAVIGATE", "stage2_corsair_phantom",
            VocabularyOpportunityTiming.PreCombatGate, "navigate_phantom_course", "verb", "/ˈnævɪɡeɪt/",
            "To plan and direct the route of a ship, vehicle, or journey.",
            "The navigator used the stars to navigate the ship through fog.",
            "Form the word that means to plan and direct a route.", "CLOUDERS",
            "The navigator used the stars to navigate the ship through fog.",
            "The sailor navigated the anchor into a heavier metal.",
            "The crew navigated the waves by making them disappear.",
            "The captain navigated the treasure by hiding it in the hold.");
    }

    public static VocabularyWordData CreateStage2UndertowCandidate()
    {
        return CreateStage2Candidate(
            "stage2_undertow", "UNDERTOW", "stage2_tide_wraith",
            VocabularyOpportunityTiming.PreCombatGate, "undertow_wraith_current", "noun", "/ˈʌndərtoʊ/",
            "A strong current beneath the water's surface that pulls away from shore.",
            "The lifeguard warned swimmers about the undertow near the reef.",
            "Form the word for a strong current that pulls water away from shore.", "SILKPACE",
            "The lifeguard warned swimmers about the undertow near the reef.",
            "The undertow repaired the broken lighthouse lamp.",
            "The undertow is a tool sailors use to measure distance.",
            "The undertow is the safe, shallow area beside a beach.");
    }

    public static VocabularyWordData CreateStage2NauticalCandidate()
    {
        return CreateStage2Candidate(
            "stage2_nautical", "NAUTICAL", "stage2_drowned_captain",
            VocabularyOpportunityTiming.PreCombatGate, "nautical_captain_command", "adjective", "/ˈnɔːtɪkəl/",
            "Related to ships, sailors, or travel on the sea.",
            "The captain kept nautical charts beside the ship's wheel.",
            "Form the word that means related to ships or travel on the sea.", "BENDROPS",
            "The captain kept nautical charts beside the ship's wheel.",
            "The crew used nautical ropes to grow vegetables on deck.",
            "Nautical means silent and unable to make a sound.",
            "The sailor called the ship's anchor nautical because it was made of glass.");
    }

    public static VocabularyWordData CreateStage2ErodeCandidate()
    {
        return CreateStage2Candidate(
            "stage2_erode", "ERODE", "stage2_erode_boulder",
            VocabularyOpportunityTiming.PreCombatGate, "erode_boulder_passage", "verb", "/ɪˈroʊd/",
            "To gradually wear away rock, soil, or another surface through water, wind, or friction.",
            "Powerful waves can erode the rocks along the shore.",
            "Form the word that means to gradually wear away rock or soil.", "TALSNIPWUCK",
            "Powerful waves can erode the rocks along the shore.",
            "The sailor eroded the map by using it to navigate north.",
            "The captain eroded the anchor by lifting it onto the deck.",
            "The crew eroded the lighthouse by switching on its lamp.");
    }

    public static VocabularyWordData CreateStage3ScorchedCandidate()
    {
        return CreateStage3Candidate(
            "stage3_scorched", "SCORCHED", "stage3_sunbound_warden",
            "scorched_warden_gate", "adjective", "/skɔːrtʃt/",
            "Burned, dried, or damaged by intense heat.",
            "The scorched ground cracked beneath the desert sun.",
            "Form the word that means burned or dried by intense heat.", "BLUNTIFY",
            "The scorched ground was dry and blackened by the intense heat.",
            "The map was scorched because the traveler folded it carefully.",
            "The guard called the cool spring water scorched.",
            "The merchant scorched the coins by counting them twice.");
    }

    public static VocabularyWordData CreateStage3DroughtCandidate()
    {
        return CreateStage3Candidate(
            "stage3_drought", "DROUGHT", "stage3_desert_archer",
            "drought_archer_gate", "noun", "/draʊt/",
            "A long period with little or no rainfall.",
            "The village stored water to survive the severe drought.",
            "Form the word for a long period with very little rain.", "SANDCLIME",
            "The long drought left the wells nearly empty.",
            "The drought repaired the broken bridge before sunset.",
            "A drought is a short burst of heavy rainfall.",
            "The traveler used a drought to measure the distance between towns.");
    }

    public static VocabularyWordData CreateStage3SandCandidate()
    {
        return CreateStage3Candidate(
            "stage3_sand", "SAND", "stage3_stone_golem",
            "sand_golem_gate", "noun", "/sænd/",
            "Loose grains of rock or mineral found on beaches and in deserts.",
            "Wind pushed the sand across the desert path.",
            "Form the word for loose grains of rock found across the desert.", "HEATROCKWIND",
            "Strong wind carried sand across the desert floor.",
            "Sand is a metal tool used to repair armor.",
            "The traveler used sand as another word for a mountain peak.",
            "Sand means a deep pool of fresh water beneath the ground.");
    }

    public static VocabularyWordData CreateStage3EndureCandidate()
    {
        return CreateStage3Candidate(
            "stage3_endure", "ENDURE", "stage3_node4_riddle",
            "endure_riddle_gate", "verb", "/ɪnˈdʊr/",
            "To continue through difficulty, pain, or hardship without giving up.",
            "The travelers had to endure the heat until nightfall.",
            "Form the word that means to continue through hardship without giving up.", "SANDSTORMX",
            "The travelers had to endure the desert heat and keep moving.",
            "The traveler endured the map by placing it inside a bag.",
            "Endure means to avoid every difficult situation immediately.",
            "The merchant endured the coins by arranging them in a straight line.");
    }

    private static VocabularyWordData CreateStage1Candidate(
        string configuredWordId,
        string configuredWord,
        string configuredEncounterId,
        VocabularyOpportunityTiming configuredTiming,
        string configuredSuccessEffectId,
        string configuredDefinition,
        string configuredContextSentence,
        string configuredCombatClue,
        string configuredDistractorLetters,
        List<VocabularyReviewChoice> configuredReviewChoices)
    {
        VocabularyWordData data = new VocabularyWordData
        {
            wordId = configuredWordId,
            word = configuredWord,
            encounterId = configuredEncounterId,
            opportunityTiming = configuredTiming,
            successEffectId = configuredSuccessEffectId,
            partOfSpeech = "verb",
            definition = configuredDefinition,
            contextSentence = configuredContextSentence,
            combatClue = configuredCombatClue,
            challengeDistractorLetters = configuredDistractorLetters,
            reviewQuestion = $"Which sentence correctly uses the word {configuredWord.ToUpperInvariant()}?",
            distractors = new List<VocabularyDistractor>(),
            reviewChoices = configuredReviewChoices ?? new List<VocabularyReviewChoice>()
        };

        return data;
    }

    private static VocabularyWordData CreateStage2Candidate(
        string configuredWordId,
        string configuredWord,
        string configuredEncounterId,
        VocabularyOpportunityTiming configuredTiming,
        string configuredSuccessEffectId,
        string configuredPartOfSpeech,
        string configuredPhoneticSpelling,
        string configuredDefinition,
        string configuredContextSentence,
        string configuredCombatClue,
        string configuredDistractorLetters,
        string correctSentence,
        string incorrectSentenceOne,
        string incorrectSentenceTwo,
        string incorrectSentenceThree)
    {
        VocabularyWordData data = new VocabularyWordData
        {
            wordId = configuredWordId,
            word = configuredWord,
            encounterId = configuredEncounterId,
            opportunityTiming = configuredTiming,
            successEffectId = configuredSuccessEffectId,
            partOfSpeech = configuredPartOfSpeech,
            phoneticSpelling = configuredPhoneticSpelling,
            definition = configuredDefinition,
            contextSentence = configuredContextSentence,
            combatClue = configuredCombatClue,
            challengeDistractorLetters = configuredDistractorLetters,
            reviewQuestion = $"Which sentence correctly uses the word {configuredWord}?",
            distractors = new List<VocabularyDistractor>(),
            reviewChoices = new List<VocabularyReviewChoice>
            {
                new VocabularyReviewChoice(correctSentence, true, $"{configuredWord} is used correctly in this sentence."),
                new VocabularyReviewChoice(incorrectSentenceOne, false, $"This does not match the meaning of {configuredWord}."),
                new VocabularyReviewChoice(incorrectSentenceTwo, false, $"This does not match the meaning of {configuredWord}."),
                new VocabularyReviewChoice(incorrectSentenceThree, false, $"This does not match the meaning of {configuredWord}.")
            }
        };

        return data;
    }

    private static VocabularyWordData CreateStage3Candidate(
        string configuredWordId,
        string configuredWord,
        string configuredEncounterId,
        string configuredSuccessEffectId,
        string configuredPartOfSpeech,
        string configuredPhoneticSpelling,
        string configuredDefinition,
        string configuredContextSentence,
        string configuredCombatClue,
        string configuredDistractorLetters,
        string correctSentence,
        string incorrectSentenceOne,
        string incorrectSentenceTwo,
        string incorrectSentenceThree)
    {
        VocabularyWordData data = new VocabularyWordData
        {
            wordId = configuredWordId,
            word = configuredWord,
            encounterId = configuredEncounterId,
            opportunityTiming = VocabularyOpportunityTiming.PreCombatGate,
            successEffectId = configuredSuccessEffectId,
            partOfSpeech = configuredPartOfSpeech,
            phoneticSpelling = configuredPhoneticSpelling,
            definition = configuredDefinition,
            contextSentence = configuredContextSentence,
            combatClue = configuredCombatClue,
            challengeDistractorLetters = configuredDistractorLetters,
            reviewQuestion = $"Which sentence correctly uses the word {configuredWord}?",
            distractors = new List<VocabularyDistractor>(),
            reviewChoices = new List<VocabularyReviewChoice>
            {
                new VocabularyReviewChoice(correctSentence, true, $"{configuredWord} is used correctly in this sentence."),
                new VocabularyReviewChoice(incorrectSentenceOne, false, $"This does not match the meaning of {configuredWord}."),
                new VocabularyReviewChoice(incorrectSentenceTwo, false, $"This does not match the meaning of {configuredWord}."),
                new VocabularyReviewChoice(incorrectSentenceThree, false, $"This does not match the meaning of {configuredWord}.")
            }
        };

        return data;
    }

    private static string NormalizeLetters(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        char[] source = value.ToUpperInvariant().ToCharArray();
        List<char> letters = new List<char>(source.Length);
        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] >= 'A' && source[i] <= 'Z')
                letters.Add(source[i]);
        }

        return new string(letters.ToArray());
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

    public VocabularyWordData FindWord(string candidate)
    {
        if (words == null || string.IsNullOrWhiteSpace(candidate))
            return null;

        return words.Find(item => item != null && item.Matches(candidate));
    }

    public VocabularyWordData FindWordById(string candidateWordId)
    {
        if (words == null || string.IsNullOrWhiteSpace(candidateWordId))
            return null;

        string normalizedId = candidateWordId.Trim();
        return words.Find(item => item != null &&
            string.Equals(item.WordId, normalizedId, StringComparison.OrdinalIgnoreCase));
    }

    public void Configure(int configuredStageNumber, List<VocabularyWordData> configuredWords)
    {
        stageNumber = Mathf.Max(1, configuredStageNumber);
        words = configuredWords ?? new List<VocabularyWordData>();
    }

    public void ConfigurePrototype(int configuredStageNumber, List<VocabularyWordData> configuredWords)
    {
        Configure(configuredStageNumber, configuredWords);
    }
}
