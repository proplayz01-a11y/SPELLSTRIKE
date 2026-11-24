using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Compatibility facade for Stage 2 vocabulary assistance and reward UI.</summary>
public sealed class Stage2ObjectiveTracker : MonoBehaviour
{
    public event Action ProgressChanged;
    public event Action AllObjectivesCompleted;
    [Header("Objective Targets")]
    [SerializeField, Min(1)] private int fragmentTarget = 4;
    [SerializeField, Min(1)] private int tenLetterWordTarget = 1;
    [SerializeField, Min(1)] private int requiredLongWordLength = 10;
    [SerializeField, Min(1)] private int maritimeWordTarget = 3;
    [Header("Sunken Seas Word Set")]
    [SerializeField] private string[] maritimeWords = Stage2ObjectiveVocabulary.CreateMaritimeWordCopy();
    private ObjectiveManager manager;
    public int FragmentTarget => Mathf.Max(1, fragmentTarget);
    public int TenLetterWordTarget => Mathf.Max(1, tenLetterWordTarget);
    public int RequiredLongWordLength => Mathf.Max(1, requiredLongWordLength);
    public int MaritimeWordTarget => Mathf.Max(1, maritimeWordTarget);
    public string[] MaritimeWords => maritimeWords ?? Stage2ObjectiveVocabulary.CreateMaritimeWordCopy();
    public int FragmentsCollected => manager != null ? manager.Fragments : 0;
    public int TenLetterWordsCast => manager != null ? manager.Secondary : 0;
    public int UniqueMaritimeWordsCast => manager != null ? manager.Words : 0;
    public bool AllObjectivesComplete => manager != null && manager.allGoalsCompleted;
    public bool CompletedBeforeCurrentRun => manager != null && manager.CompletedBeforeCurrentRun;
    public bool PearlEarnedThisRun => AllObjectivesComplete && !CompletedBeforeCurrentRun;
    public IReadOnlyCollection<string> CastMaritimeWords => manager != null ? manager.CastMaritimeWords : Array.Empty<string>();
    public bool HasCastMaritimeWord(string word) => manager != null && manager.HasCastMaritimeWord(word);

    private void OnEnable()
    {
        Bind();
    }

    private void Start() => Bind();

    private void Bind()
    {
        if (manager != null) return;
        manager = ObjectiveManager.Resolve(gameObject.scene);
        if (manager == null) return;
        manager.ProgressChanged += ForwardProgress;
        manager.AllObjectivesCompleted += ForwardCompletion;
    }

    private void OnDisable()
    {
        if (manager == null) return;
        manager.ProgressChanged -= ForwardProgress;
        manager.AllObjectivesCompleted -= ForwardCompletion;
        manager = null;
    }

    private void ForwardProgress() => ProgressChanged?.Invoke();
    private void ForwardCompletion() => AllObjectivesCompleted?.Invoke();
    public void RegisterFragmentCollected(int nodeIndex) => ObjectiveManager.ReportFragmentCollected(gameObject.scene, nodeIndex);
}
