using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class Stage2ObjectiveTracker : MonoBehaviour
{
    public event Action ProgressChanged;
    public event Action AllObjectivesCompleted;

    [Header("Objective Targets")]
    [SerializeField, Min(1)] private int fragmentTarget = 4;
    [SerializeField, Min(1)] private int tenLetterWordTarget = 1;
    [SerializeField, Min(1)] private int requiredLongWordLength = 10;
    [SerializeField, Min(1)] private int maritimeWordTarget = 3;

    [Header("Runtime Sources")]
    [SerializeField] private AttackController attackController;
    [SerializeField] private ObjectiveManager objectiveManager;

    [Header("Sunken Seas Word Set")]
    [SerializeField] private string[] maritimeWords = Stage2ObjectiveVocabulary.CreateMaritimeWordCopy();

    [Header("Runtime State")]
    [SerializeField] private int fragmentsCollected;
    [SerializeField] private int tenLetterWordsCast;
    [SerializeField] private int uniqueMaritimeWordsCast;
    [SerializeField] private bool allObjectivesComplete;

    private readonly HashSet<int> collectedFragmentNodes = new HashSet<int>();
    private readonly HashSet<string> acceptedMaritimeWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> castMaritimeWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private bool completedBeforeCurrentRun;

    public int FragmentsCollected => fragmentsCollected;
    public int FragmentTarget => fragmentTarget;
    public int TenLetterWordsCast => tenLetterWordsCast;
    public int TenLetterWordTarget => tenLetterWordTarget;
    public int RequiredLongWordLength => requiredLongWordLength;
    public int UniqueMaritimeWordsCast => uniqueMaritimeWordsCast;
    public int MaritimeWordTarget => maritimeWordTarget;
    public bool AllObjectivesComplete => allObjectivesComplete;
    public bool CompletedBeforeCurrentRun => completedBeforeCurrentRun;
    public bool PearlEarnedThisRun => allObjectivesComplete && !completedBeforeCurrentRun;
    public IReadOnlyCollection<string> CastMaritimeWords => castMaritimeWords;

    public bool HasCastMaritimeWord(string word)
    {
        return castMaritimeWords.Contains(NormalizeWord(word));
    }

    private void Awake()
    {
        RebuildAcceptedWordSet();
        ResolveReferences();
        RestorePersistentCompletion();
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeToRuntimeSources();
    }

    private void OnDisable()
    {
        UnsubscribeFromRuntimeSources();
    }

    private void OnValidate()
    {
        fragmentTarget = Mathf.Max(1, fragmentTarget);
        tenLetterWordTarget = Mathf.Max(1, tenLetterWordTarget);
        requiredLongWordLength = Mathf.Max(1, requiredLongWordLength);
        maritimeWordTarget = Mathf.Max(1, maritimeWordTarget);
        RebuildAcceptedWordSet();
    }

    public void RegisterFragmentCollected(int nodeIndex)
    {
        if (nodeIndex <= 0 || !collectedFragmentNodes.Add(nodeIndex))
            return;

        fragmentsCollected = Mathf.Min(fragmentTarget, collectedFragmentNodes.Count);
        Debug.Log($"[Stage2Objectives] Lost Fragments: {fragmentsCollected}/{fragmentTarget}.", this);
        EvaluateCompletion();
    }

    private void HandleSuccessfulNormalCombatWordLanded(string submittedWord)
    {
        string normalizedWord = NormalizeWord(submittedWord);
        bool progressChanged = false;

        if (normalizedWord.Length == requiredLongWordLength
            && tenLetterWordsCast < tenLetterWordTarget)
        {
            tenLetterWordsCast++;
            progressChanged = true;
            Debug.Log(
                $"[Stage2Objectives] {requiredLongWordLength}-letter word landed: {normalizedWord} " +
                $"({tenLetterWordsCast}/{tenLetterWordTarget}).",
                this);
        }

        if (acceptedMaritimeWords.Contains(normalizedWord) && castMaritimeWords.Add(normalizedWord))
        {
            uniqueMaritimeWordsCast = Mathf.Min(maritimeWordTarget, castMaritimeWords.Count);
            progressChanged = true;
            Debug.Log(
                $"[Stage2Objectives] Sunken Sea spell accepted: {normalizedWord} " +
                $"({uniqueMaritimeWordsCast}/{maritimeWordTarget}).",
                this);
        }

        if (progressChanged)
            EvaluateCompletion();
    }

    private void EvaluateCompletion()
    {
        bool complete = fragmentsCollected >= fragmentTarget
                        && tenLetterWordsCast >= tenLetterWordTarget
                        && uniqueMaritimeWordsCast >= maritimeWordTarget;

        if (objectiveManager != null)
            objectiveManager.allGoalsCompleted = complete;

        ProgressChanged?.Invoke();

        if (!complete || allObjectivesComplete)
            return;

        allObjectivesComplete = true;
        GameDatabaseManager.EnsureInstance()?.MarkStage2ObjectivesCompleted();
        Debug.Log("[Stage2Objectives] All Sunken Seas objectives completed.", this);
        PassiveRewardManager.GrantStage2ObjectiveCompletionReward("Stage 2 objective completion");
        AllObjectivesCompleted?.Invoke();
    }

    private void ResolveReferences()
    {
        if (attackController == null)
            attackController = FindFirstObjectByType<AttackController>();

        if (objectiveManager == null)
            objectiveManager = FindFirstObjectByType<ObjectiveManager>();
    }

    private void RestorePersistentCompletion()
    {
        GameDatabaseManager database = GameDatabaseManager.EnsureInstance();
        PassiveItemInventory inventory = PassiveItemInventory.GetOrCreate();
        bool pearlAlreadyEarned = inventory != null
                                  && (inventory.HasOwnedPassive(PassiveItemId.PurifyingPearl)
                                      || inventory.HasPendingPassive(PassiveItemId.PurifyingPearl));

        if (database == null || (!database.HasCompletedStage2Objectives() && !pearlAlreadyEarned))
            return;

        if (!database.HasCompletedStage2Objectives())
            database.MarkStage2ObjectivesCompleted();

        completedBeforeCurrentRun = true;
        fragmentsCollected = fragmentTarget;
        tenLetterWordsCast = tenLetterWordTarget;
        uniqueMaritimeWordsCast = maritimeWordTarget;
        allObjectivesComplete = true;

        collectedFragmentNodes.Clear();
        for (int nodeIndex = 1; nodeIndex <= fragmentTarget; nodeIndex++)
            collectedFragmentNodes.Add(nodeIndex);

        castMaritimeWords.Clear();
        foreach (string maritimeWord in acceptedMaritimeWords)
        {
            if (castMaritimeWords.Count >= maritimeWordTarget)
                break;

            castMaritimeWords.Add(maritimeWord);
        }

        if (objectiveManager != null)
            objectiveManager.allGoalsCompleted = true;

        Debug.Log("[Stage2Objectives] Restored completed objectives from persistent progress.", this);
    }

    private void SubscribeToRuntimeSources()
    {
        UnsubscribeFromRuntimeSources();

        if (attackController != null)
            attackController.SuccessfulNormalCombatWordLanded += HandleSuccessfulNormalCombatWordLanded;

    }

    private void UnsubscribeFromRuntimeSources()
    {
        if (attackController != null)
            attackController.SuccessfulNormalCombatWordLanded -= HandleSuccessfulNormalCombatWordLanded;

    }

    private void RebuildAcceptedWordSet()
    {
        acceptedMaritimeWords.Clear();
        if (maritimeWords == null)
            return;

        foreach (string candidate in maritimeWords)
        {
            string normalizedWord = NormalizeWord(candidate);
            if (!string.IsNullOrEmpty(normalizedWord))
                acceptedMaritimeWords.Add(normalizedWord);
        }
    }

    private static string NormalizeWord(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToUpperInvariant();
    }
}
