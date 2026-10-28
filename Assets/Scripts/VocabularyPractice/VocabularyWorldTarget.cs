using UnityEngine;
using UnityEngine.Events;

public class VocabularyWorldTarget : MonoBehaviour
{
    public event System.Action<VocabularyWorldTarget> TargetCompleted;

    [Header("Vocabulary Encounter")]
    [SerializeField] private VocabularyEncounterFlowController encounterFlow;
    [SerializeField] private VocabularyBarrier vocabularyBarrier;
    [SerializeField] private Transform targetPoint;

    [Header("Completion Presentation")]
    [SerializeField] private GameObject intactVisual;
    [SerializeField] private GameObject fracturedVisual;
    [SerializeField] private UnityEvent onTargetCompleted;

    private bool completed;
    private bool subscribed;

    public Transform TargetPoint => targetPoint != null ? targetPoint : transform;
    public VocabularyBarrier Barrier => vocabularyBarrier;
    public bool CanBeTargeted =>
        !completed
        && isActiveAndEnabled
        && vocabularyBarrier != null
        && vocabularyBarrier.BarrierActive
        && vocabularyBarrier.ChallengeVisible;

    private void Awake()
    {
        if (targetPoint == null)
            targetPoint = transform;

        if (intactVisual == null)
            intactVisual = gameObject;

        if (fracturedVisual != null)
            fracturedVisual.SetActive(false);
    }

    private void OnEnable()
    {
        EnsureSubscription();
    }

    private void Start()
    {
        EnsureSubscription();
    }

    private void OnDisable()
    {
        RemoveSubscription();
    }

    public void CompleteTarget()
    {
        if (completed)
            return;

        completed = true;

        if (fracturedVisual != null)
            fracturedVisual.SetActive(true);

        TargetCompleted?.Invoke(this);
        onTargetCompleted?.Invoke();

        if (intactVisual != null)
            intactVisual.SetActive(false);

        Debug.Log("[VocabularyWorldTarget] Environmental target completed.", this);
    }

    private void HandleEncounterCompleted(VocabularyEncounterFlowController source)
    {
        if (source == encounterFlow)
            CompleteTarget();
    }

    private void EnsureSubscription()
    {
        if (subscribed || encounterFlow == null)
            return;

        encounterFlow.EncounterCompleted += HandleEncounterCompleted;
        subscribed = true;
    }

    private void RemoveSubscription()
    {
        if (!subscribed || encounterFlow == null)
            return;

        encounterFlow.EncounterCompleted -= HandleEncounterCompleted;
        subscribed = false;
    }

    public static VocabularyWorldTarget FindClosestActive(Vector3 origin)
    {
        VocabularyWorldTarget[] targets = FindObjectsByType<VocabularyWorldTarget>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        VocabularyWorldTarget closest = null;
        float closestSqrDistance = float.PositiveInfinity;

        for (int i = 0; i < targets.Length; i++)
        {
            VocabularyWorldTarget candidate = targets[i];
            if (candidate == null || !candidate.CanBeTargeted)
                continue;

            float sqrDistance = (candidate.TargetPoint.position - origin).sqrMagnitude;
            if (sqrDistance >= closestSqrDistance)
                continue;

            closest = candidate;
            closestSqrDistance = sqrDistance;
        }

        return closest;
    }
}
