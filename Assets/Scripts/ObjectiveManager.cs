using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Scene-owned authority for campaign objective state, independent of UI.</summary>
[DisallowMultipleComponent]
public class ObjectiveManager : MonoBehaviour
{
    [Header("Stage Goals")]
    public bool allGoalsCompleted;
    public event Action ProgressChanged;
    public event Action AllObjectivesCompleted;
    private static ObjectiveManager cached;
    private bool initialized, rewardEnabled = true, restoredCompletion;
    private int stage, fragmentTarget = 4, secondaryTarget = 1, wordTarget = 3, longLength = 10;
    private int fragments, secondary, words;
    private readonly HashSet<int> collected = new();
    private readonly HashSet<int> defeated = new();
    private readonly HashSet<string> acceptedMaritime = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> castMaritime = new(StringComparer.OrdinalIgnoreCase);
    private EnemyHealth activeEncounter;
    private bool spiritCoreObserved;

    public int Stage => stage;
    public int Fragments => fragments;
    public int Secondary => secondary;
    public int Words => words;
    public bool CompletedBeforeCurrentRun => restoredCompletion;
    public IReadOnlyCollection<string> CastMaritimeWords => castMaritime;
    public bool NeedsSpiritGuidance => stage == 1 && (secondary == 0 ||
        (GameDatabaseManager.Instance != null && !GameDatabaseManager.Instance.HasEchoRuneCore()));
    public int NextFragmentNode
    {
        get
        {
            for (int i = 1; i <= fragmentTarget; i++)
                if (!collected.Contains(i)) return i;
            return 0;
        }
    }
    public bool IsFragmentAwaitingPickup(int node) => node > 0 &&
        defeated.Contains(node) && !collected.Contains(node);
    public bool HasActiveEncounter => activeEncounter != null &&
        activeEncounter.gameObject.activeInHierarchy && !activeEncounter.IsDead &&
        activeEncounter.currentHealth > 0f;

    public static void ReportEncounterStarted(EnemyHealth enemy)
    {
        if (enemy == null) return;
        var manager = Resolve(enemy.gameObject.scene);
        if (manager != null) manager.activeEncounter = enemy;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache()
    {
        cached = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Resolve(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Resolve(scene);

    public static ObjectiveManager Resolve(Scene scene)
    {
        if (!Application.isPlaying || !scene.IsValid() || !scene.isLoaded
            || (scene.name != "SampleScene" && scene.name != "Stage2Scene")) return null;
        if (cached != null && cached.gameObject.scene == scene) return cached;
        ObjectiveManager manager = null;
        Stage2ObjectiveTracker tracker = null;
        GoalsPanelUI authoredGoals = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (manager == null) manager = root.GetComponentInChildren<ObjectiveManager>(true);
            if (tracker == null) tracker = root.GetComponentInChildren<Stage2ObjectiveTracker>(true);
            if (authoredGoals == null) authoredGoals = root.GetComponentInChildren<GoalsPanelUI>(true);
        }
        if (manager == null)
        {
            var go = new GameObject("SmartObjectiveManager");
            SceneManager.MoveGameObjectToScene(go, scene);
            manager = go.AddComponent<ObjectiveManager>();
        }
        cached = manager;
        manager.Initialize(scene.name == "Stage2Scene" ? 2 : 1, tracker, authoredGoals);
        // Keep the old Stage 2 public API for tile-assist and reward screens.
        if (manager.stage == 2 && tracker == null)
            manager.gameObject.AddComponent<Stage2ObjectiveTracker>();
        return manager;
    }

    private void Initialize(int stageNumber, Stage2ObjectiveTracker tracker, GoalsPanelUI goals)
    {
        if (initialized) return;
        initialized = true;
        stage = stageNumber;
        allGoalsCompleted = false;
        var db = GameDatabaseManager.EnsureInstance();
        if (stage == 1)
        {
            rewardEnabled = goals == null || goals.grantGreatAttractorOnAllGoalsComplete;
            secondary = db != null && db.HasMetSpiritRemnant() ? 1 : 0;
            spiritCoreObserved = db != null && db.HasEchoRuneCore();
            return;
        }
        fragmentTarget = tracker != null ? tracker.FragmentTarget : 4;
        secondaryTarget = tracker != null ? tracker.TenLetterWordTarget : 1;
        wordTarget = tracker != null ? tracker.MaritimeWordTarget : 3;
        longLength = tracker != null ? tracker.RequiredLongWordLength : 10;
        foreach (string word in tracker != null ? tracker.MaritimeWords : Stage2ObjectiveVocabulary.CreateMaritimeWordCopy())
            if (!string.IsNullOrWhiteSpace(word)) acceptedMaritime.Add(Normalize(word));
        var inventory = PassiveItemInventory.GetOrCreate();
        bool pearl = inventory != null && (inventory.HasOwnedPassive(PassiveItemId.PurifyingPearl)
            || inventory.HasPendingPassive(PassiveItemId.PurifyingPearl));
        if (db == null || (!db.HasCompletedStage2Objectives() && !pearl)) return;
        if (!db.HasCompletedStage2Objectives()) db.MarkStage2ObjectivesCompleted();
        restoredCompletion = allGoalsCompleted = true;
        fragments = fragmentTarget;
        secondary = secondaryTarget;
        words = wordTarget;
        for (int i = 1; i <= fragmentTarget; i++) collected.Add(i);
        foreach (string word in acceptedMaritime)
        {
            if (castMaritime.Count >= wordTarget) break;
            castMaritime.Add(word);
        }
    }

    public static void ReportEnemyDefeated(Scene scene, int node) => Resolve(scene)?.EnemyDefeated(node);
    public static void ReportFragmentCollected(Scene scene, int node) => Resolve(scene)?.FragmentCollected(node);
    public static void ReportSpiritMet(Scene scene) => Resolve(scene)?.SpiritMet();
    public static void ReportWordCast(string word) => Resolve(SceneManager.GetActiveScene())?.WordCast(word);
    public static void ReportNormalWordLanded(string word) => Resolve(SceneManager.GetActiveScene())?.NormalWordLanded(word);

    public void EnemyDefeated(int node)
    {
        if (node < 1 || node > fragmentTarget || !defeated.Add(node)) return;
        // The authoritative node defeat callback may precede the mirrored
        // EnemyHealth death flag (Bramble uses a separate health controller).
        activeEncounter = null;
        ProgressChanged?.Invoke();
    }

    public void FragmentCollected(int node)
    {
        if (node < 1 || node > fragmentTarget || !collected.Add(node)) return;
        fragments = Mathf.Min(fragmentTarget, Mathf.Max(fragments, collected.Count));
        Evaluate();
    }

    public void SpiritMet()
    {
        if (stage != 1) return;
        bool hasCore = GameDatabaseManager.Instance != null && GameDatabaseManager.Instance.HasEchoRuneCore();
        if (secondary != 0 && spiritCoreObserved == hasCore) return;
        spiritCoreObserved = hasCore;
        secondary = 1;
        Evaluate();
    }

    public void WordCast(string word)
    {
        // Preserve Stage 1's accepted-cast rule, including Target Challenge casts.
        if (stage != 1 || Normalize(word).Length < 6 || words >= wordTarget) return;
        words++;
        Evaluate();
    }

    public void NormalWordLanded(string word)
    {
        if (stage != 2) return;
        string normalized = Normalize(word);
        bool changed = false;
        if (normalized.Length == longLength && secondary < secondaryTarget)
        {
            secondary++;
            changed = true;
        }
        if (acceptedMaritime.Contains(normalized) && castMaritime.Add(normalized))
        {
            words = Mathf.Min(wordTarget, castMaritime.Count);
            changed = true;
        }
        if (changed) Evaluate();
    }

    public bool HasCastMaritimeWord(string word) => castMaritime.Contains(Normalize(word));
    private static string Normalize(string word) => string.IsNullOrWhiteSpace(word) ? "" : word.Trim().ToUpperInvariant();

    // Compatibility for legacy UI callers; shipped gameplay uses identified events.
    public void SetLegacyStage1Count(int index, int count)
    {
        if (stage != 1) return;
        if (index == 0)
        {
            int target = Mathf.Clamp(count, fragments, fragmentTarget);
            for (int i = 1; i <= fragmentTarget && collected.Count < target; i++) collected.Add(i);
            fragments = Mathf.Max(fragments, collected.Count);
        }
        else if (index == 2) words = Mathf.Clamp(count, words, wordTarget);
        Evaluate();
    }

    public string CurrentObjective
    {
        get
        {
            if (NeedsSpiritGuidance) return "Speak with the Spirit Remnant";
            for (int i = 1; i <= fragmentTarget; i++)
            {
                if (collected.Contains(i)) continue;
                string[] first = { "Bramble Sprite", "Stone Sentinel", "Cursed Jester", "Hollow Knight" };
                string[] second = { "Barnacle Husk", "Corsair Phantom", "Tide Wraith", "Drowned Captain" };
                string enemy = i <= 4 ? (stage == 1 ? first : second)[i - 1] : "the guardian";
                return defeated.Contains(i) ? "Recover the fragment" : "Defeat " + enemy;
            }
            if (stage == 2 && secondary < secondaryTarget) return $"Land a {longLength}-letter spell";
            if (words < wordTarget) return stage == 1 ? "Cast more 6+ letter words" : "Land more unique sea words";
            return "All stage objectives completed";
        }
    }

    private void Evaluate()
    {
        bool complete = fragments >= fragmentTarget && secondary >= secondaryTarget && words >= wordTarget;
        bool newlyCompleted = complete && !allGoalsCompleted;
        if (newlyCompleted)
        {
            allGoalsCompleted = true;
            if (stage == 1 && rewardEnabled)
                PassiveRewardManager.GrantObjectiveCompletionReward("Objective completion");
            else if (stage == 2)
            {
                GameDatabaseManager.EnsureInstance()?.MarkStage2ObjectivesCompleted();
                PassiveRewardManager.GrantStage2ObjectiveCompletionReward("Stage 2 objective completion");
            }
        }
        ProgressChanged?.Invoke();
        if (newlyCompleted) AllObjectivesCompleted?.Invoke();
    }
}
