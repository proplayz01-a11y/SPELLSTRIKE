using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class StoneSentinelCounterspellController : MonoBehaviour
{
    private enum PhaseState
    {
        WaitingForThreshold,
        Equipping,
        Teaching,
        Challenge,
        Breaking,
        Completed,
        Failed
    }

    [Header("Required References")]
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private StoneSentinelsController sentinelController;
    [SerializeField] private VocabularyEncounterFlowController encounterFlow;
    [SerializeField] private VocabularyBarrier vocabularyBarrier;
    [SerializeField] private Animator animator;

    [Header("Shield Visuals")]
    [Tooltip("Optional shield prop placed on the ground before it is picked up.")]
    [SerializeField] private GameObject groundShield;
    [Tooltip("Shield parented to the Sentinel's left hand. Keep disabled before the phase.")]
    [SerializeField] private GameObject heldShield;

    [Header("Trigger")]
    [Range(0.05f, 0.95f)]
    [SerializeField] private float healthThreshold = 0.5f;

    [Header("Animator State Names")]
    [SerializeField] private string pickUpState = "Counterspell_PickUp";
    [SerializeField] private string crouchState = "Counterspell_Crouch";
    [SerializeField] private string idleState = "Counterspell_Idle";
    [SerializeField] private string blockState = "Counterspell_Block";
    [Min(0f)]
    [SerializeField] private float crossFadeDuration = 0.08f;
    [Min(0.5f)]
    [SerializeField] private float stateEntryTimeout = 3f;
    [Range(0.5f, 1f)]
    [SerializeField] private float oneShotExitNormalizedTime = 0.95f;

    [Header("Events")]
    [SerializeField] private UnityEvent onCounterspellStarted;
    [SerializeField] private UnityEvent onShieldEquipped;
    [SerializeField] private UnityEvent onShieldBlockedSpell;
    [SerializeField] private UnityEvent onShieldFractured;
    [SerializeField] private UnityEvent onCounterspellCompleted;
    [SerializeField] private UnityEvent onCounterspellFailed;

    private PhaseState state = PhaseState.WaitingForThreshold;
    private Coroutine animationRoutine;
    private bool shieldEquipped;
    private bool barrierBroken;
    private bool flowCompleted;
    private bool breakReactionCompleted;
    private bool subscribed;

    public bool HasTriggered => state != PhaseState.WaitingForThreshold;
    public bool IsCounterspellActive =>
        state == PhaseState.Equipping
        || state == PhaseState.Teaching
        || state == PhaseState.Challenge
        || state == PhaseState.Breaking;

    private void Awake()
    {
        ResolveReferences();

        if (groundShield != null)
            groundShield.SetActive(false);
        if (heldShield != null)
            heldShield.SetActive(false);
    }

    private void OnEnable()
    {
        ResolveReferences();
        EnsureSubscriptions();
    }

    private void OnDisable()
    {
        RemoveSubscriptions();

        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

        if (state == PhaseState.Equipping)
            encounterFlow?.CancelPreparedInputLock();
    }

    private void HandleHealthChanged(float currentHealth, float maxHealth)
    {
        if (state != PhaseState.WaitingForThreshold
            || sentinelController == null
            || !sentinelController.IsBattleStarted
            || maxHealth <= 0f
            || currentHealth <= 0f
            || currentHealth / maxHealth > healthThreshold)
        {
            return;
        }

        BeginCounterspellSequence();
    }

    public bool BeginCounterspellSequence()
    {
        ResolveReferences();
        EnsureSubscriptions();

        if (state != PhaseState.WaitingForThreshold)
            return false;

        if (enemyHealth == null
            || sentinelController == null
            || encounterFlow == null
            || vocabularyBarrier == null
            || animator == null)
        {
            FailPhase("Required FRACTURE Counterspell references are incomplete.");
            return false;
        }

        state = PhaseState.Equipping;
        sentinelController.SetVocabularyPaused(true);
        vocabularyBarrier.HideChallenge();
        encounterFlow.PrepareInputLockAfterAttackImpact();

        if (heldShield != null)
            heldShield.SetActive(false);
        if (groundShield != null)
            groundShield.SetActive(true);

        onCounterspellStarted?.Invoke();

        if (animationRoutine != null)
            StopCoroutine(animationRoutine);
        animationRoutine = StartCoroutine(EquipAndOpenTeachRoutine());

        Debug.Log(
            $"[StoneSentinelCounterspell] FRACTURE phase triggered at "
            + $"{enemyHealth.currentHealth:0.##}/{enemyHealth.maxHealth:0.##} HP.",
            this);
        return true;
    }

    private IEnumerator EquipAndOpenTeachRoutine()
    {
        yield return PlayOneShotState(pickUpState);

        // An Animation Event can call this earlier at the exact contact frame.
        EquipCounterspellShield();

        yield return PlayOneShotState(crouchState);
        PlayLoopingState(idleState);

        state = PhaseState.Teaching;
        animationRoutine = null;

        if (!encounterFlow.BeginEncounter())
            FailPhase("The reusable vocabulary encounter flow could not start.");
    }

    public void EquipCounterspellShield()
    {
        if (shieldEquipped || state != PhaseState.Equipping)
            return;

        shieldEquipped = true;

        if (groundShield != null)
            groundShield.SetActive(false);
        if (heldShield != null)
            heldShield.SetActive(true);

        onShieldEquipped?.Invoke();
        Debug.Log("[StoneSentinelCounterspell] Stone shield equipped.", this);
    }

    private void HandleChallengeStarted(VocabularyEncounterFlowController source)
    {
        if (source != encounterFlow || state != PhaseState.Teaching)
            return;

        state = PhaseState.Challenge;
        PlayLoopingState(idleState);
        Debug.Log("[StoneSentinelCounterspell] FRACTURE Counterspell challenge active.", this);
    }

    private void HandleIncorrectTargetSubmitted(
        VocabularyEncounterFlowController source,
        string submittedWord)
    {
        if (source != encounterFlow || state != PhaseState.Challenge)
            return;

        onShieldBlockedSpell?.Invoke();

        if (animationRoutine != null)
            StopCoroutine(animationRoutine);
        animationRoutine = StartCoroutine(PlayBlockAndReturnToIdleRoutine());

        Debug.Log(
            $"[StoneSentinelCounterspell] Shield absorbed '{submittedWord}'.",
            this);
    }

    private IEnumerator PlayBlockAndReturnToIdleRoutine()
    {
        yield return PlayOneShotState(blockState);

        if (state == PhaseState.Challenge)
            PlayLoopingState(idleState);

        animationRoutine = null;
    }

    private void HandleBarrierBroken(VocabularyBarrier source)
    {
        if (source != vocabularyBarrier || state != PhaseState.Challenge)
            return;

        barrierBroken = true;
        state = PhaseState.Breaking;

        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

        if (heldShield != null)
            heldShield.SetActive(false);
        if (groundShield != null)
            groundShield.SetActive(false);

        onShieldFractured?.Invoke();
        sentinelController.TriggerCounterspellBreakReaction();
        TryCompletePhase();

        Debug.Log(
            "[StoneSentinelCounterspell] FRACTURE broke the shield; "
            + "waiting for hit reaction and Normal Combat restoration.",
            this);
    }

    private void HandleEncounterCompleted(VocabularyEncounterFlowController source)
    {
        if (source != encounterFlow || state != PhaseState.Breaking)
            return;

        flowCompleted = true;
        TryCompletePhase();
    }

    private void HandleBeingHitFinished()
    {
        if (state != PhaseState.Breaking)
            return;

        breakReactionCompleted = true;
        TryCompletePhase();
    }

    private void TryCompletePhase()
    {
        if (state != PhaseState.Breaking
            || !barrierBroken
            || !flowCompleted
            || !breakReactionCompleted)
        {
            return;
        }

        state = PhaseState.Completed;
        sentinelController.SetVocabularyPaused(false);
        onCounterspellCompleted?.Invoke();

        Debug.Log(
            "[StoneSentinelCounterspell] FRACTURE phase complete; "
            + "Stone Sentinel combat resumed.",
            this);
    }

    private void HandleEncounterFailed(VocabularyEncounterFlowController source)
    {
        if (source != encounterFlow || state == PhaseState.Completed)
            return;

        FailPhase("The reusable vocabulary encounter flow failed.");
    }

    private void FailPhase(string reason)
    {
        if (state == PhaseState.Completed || state == PhaseState.Failed)
            return;

        state = PhaseState.Failed;

        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

        if (heldShield != null)
            heldShield.SetActive(false);
        if (groundShield != null)
            groundShield.SetActive(false);

        sentinelController?.SetVocabularyPaused(false);
        encounterFlow?.CancelPreparedInputLock();
        onCounterspellFailed?.Invoke();
        Debug.LogError($"[StoneSentinelCounterspell] {reason}", this);
    }

    private IEnumerator PlayOneShotState(string stateName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(stateName))
            yield break;

        int stateHash = Animator.StringToHash(stateName);
        animator.CrossFadeInFixedTime(stateHash, crossFadeDuration, 0);

        float entryElapsed = 0f;
        while (entryElapsed < stateEntryTimeout)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (!animator.IsInTransition(0) && stateInfo.shortNameHash == stateHash)
                break;

            entryElapsed += Time.deltaTime;
            yield return null;
        }

        if (entryElapsed >= stateEntryTimeout)
        {
            Debug.LogWarning(
                $"[StoneSentinelCounterspell] Animator state '{stateName}' "
                + "was not entered before timeout. Check its exact Base Layer name.",
                this);
            yield break;
        }

        while (true)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.shortNameHash != stateHash
                || stateInfo.normalizedTime >= oneShotExitNormalizedTime)
            {
                break;
            }

            yield return null;
        }
    }

    private void PlayLoopingState(string stateName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(stateName))
            return;

        animator.CrossFadeInFixedTime(
            Animator.StringToHash(stateName),
            crossFadeDuration,
            0);
    }

    private void ResolveReferences()
    {
        if (enemyHealth == null)
            enemyHealth = GetComponent<EnemyHealth>();
        if (sentinelController == null)
            sentinelController = GetComponent<StoneSentinelsController>();
        if (vocabularyBarrier == null)
            vocabularyBarrier = GetComponent<VocabularyBarrier>();
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void EnsureSubscriptions()
    {
        if (subscribed)
            return;

        if (enemyHealth != null)
            enemyHealth.HealthChanged += HandleHealthChanged;
        if (encounterFlow != null)
        {
            encounterFlow.ChallengeStarted += HandleChallengeStarted;
            encounterFlow.IncorrectTargetSubmitted += HandleIncorrectTargetSubmitted;
            encounterFlow.EncounterCompleted += HandleEncounterCompleted;
            encounterFlow.EncounterFailed += HandleEncounterFailed;
        }
        if (vocabularyBarrier != null)
            vocabularyBarrier.BarrierBroken += HandleBarrierBroken;
        if (sentinelController != null)
            sentinelController.BeingHitFinished += HandleBeingHitFinished;

        subscribed = true;
    }

    private void RemoveSubscriptions()
    {
        if (!subscribed)
            return;

        if (enemyHealth != null)
            enemyHealth.HealthChanged -= HandleHealthChanged;
        if (encounterFlow != null)
        {
            encounterFlow.ChallengeStarted -= HandleChallengeStarted;
            encounterFlow.IncorrectTargetSubmitted -= HandleIncorrectTargetSubmitted;
            encounterFlow.EncounterCompleted -= HandleEncounterCompleted;
            encounterFlow.EncounterFailed -= HandleEncounterFailed;
        }
        if (vocabularyBarrier != null)
            vocabularyBarrier.BarrierBroken -= HandleBarrierBroken;
        if (sentinelController != null)
            sentinelController.BeingHitFinished -= HandleBeingHitFinished;

        subscribed = false;
    }
}
