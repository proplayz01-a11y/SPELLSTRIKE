using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class VocabularyEncounterFlowController : MonoBehaviour
{
    private enum EncounterState
    {
        Idle,
        Teaching,
        Challenge,
        Feedback,
        Reviewing,
        Completed,
        Failed
    }

    public event Action<VocabularyEncounterFlowController> ChallengeStarted;
    public event Action<VocabularyEncounterFlowController, string> IncorrectTargetSubmitted;
    public event Action<VocabularyEncounterFlowController> EncounterCompleted;
    public event Action<VocabularyEncounterFlowController> EncounterFailed;

    [Header("Encounter References")]
    [SerializeField] private VocabularyTeachController teachController;
    [SerializeField] private VocabularyBarrier vocabularyBarrier;
    [SerializeField] private TileManager tileManager;
    [SerializeField] private AttackController attackController;
    [SerializeField] private MovementofPlayer movementController;

    [Header("Feedback UI")]
    [SerializeField] private GameObject feedbackPanel;
    [Tooltip("Dynamic first line, for example: The barrier absorbed ATE.")]
    [SerializeField] private TMP_Text feedbackText;
    [Tooltip("Optional dynamic second line. Leave empty to use the original combined-message layout.")]
    [SerializeField] private TMP_Text feedbackExplanationText;
    [Tooltip("Use a transparent full-panel Button so PC and Android share one click/tap action.")]
    [SerializeField] private Button feedbackContinueButton;

    [Header("Challenge")]
    [Min(1)]
    [SerializeField] private int maximumPoolTiles = 16;

    [Header("Events")]
    [SerializeField] private UnityEvent onChallengeStarted;
    [SerializeField] private UnityEvent onIncorrectTargetFeedback;
    [SerializeField] private UnityEvent onFlowFailed;

    private EncounterState state = EncounterState.Idle;
    private bool subscribed;
    private bool movementWasLocked;
    private bool attackWasLocked;
    private bool originalInputCaptured;
    private bool preparedInputLockActive;

    public bool IsRunning =>
        state != EncounterState.Idle
        && state != EncounterState.Completed
        && state != EncounterState.Failed;

    public void PrepareInputLock()
    {
        PrepareInputLockCore(false);
    }

    public void PrepareInputLockAfterAttackImpact()
    {
        PrepareInputLockCore(true);
    }

    private void PrepareInputLockCore(bool currentMovementLockIsTransient)
    {
        ResolveReferences();
        CaptureOriginalInputState();

        if (currentMovementLockIsTransient)
            movementWasLocked = false;

        preparedInputLockActive = true;
        SetNormalCombatWorldPickupSuspension(true);
        SetPanelInputLock(true);
    }

    public void CancelPreparedInputLock()
    {
        if (state != EncounterState.Idle || !originalInputCaptured)
            return;

        preparedInputLockActive = false;
        SetNormalCombatWorldPickupSuspension(false);
        RestoreOriginalInputState();
    }

    public bool BeginEncounter()
    {
        ResolveReferences();
        EnsureSubscriptions();

        if (state != EncounterState.Idle)
        {
            Debug.LogWarning("[VocabularyEncounterFlow] Encounter flow already started.", this);
            return false;
        }

        VocabularyWordData teachWord = teachController != null
            ? teachController.CurrentWord
            : null;
        VocabularyWordData barrierWord = vocabularyBarrier != null
            ? vocabularyBarrier.TargetWord
            : null;

        if (teachController == null
            || vocabularyBarrier == null
            || tileManager == null
            || teachWord == null
            || barrierWord == null)
        {
            return FailBeforeStart(
                "Teach, barrier, TileManager, or target-word references are incomplete.");
        }

        if (!teachWord.Matches(barrierWord.Word))
        {
            return FailBeforeStart(
                $"Teach word '{teachWord.Word}' does not match barrier word '{barrierWord.Word}'.");
        }

        CaptureOriginalInputState();
        SetNormalCombatWorldPickupSuspension(true);

        vocabularyBarrier.SetGuaranteeTargetLettersOnStart(false);
        vocabularyBarrier.HideChallenge();

        if (!tileManager.TrySnapshotAndHideNormalCombatPool())
            return FailBeforeStart("The Normal Combat pool could not be captured.");

        state = EncounterState.Teaching;
        SetPanelInputLock(true);

        if (!teachController.ShowTeach())
        {
            RecoverSavedPool();
            return FailBeforeStart("The Teach panel could not be shown.");
        }

        Debug.Log(
            $"[VocabularyEncounterFlow] Saved Normal Combat and opened Teach for {teachWord.Word}.",
            this);
        return true;
    }

    private void Awake()
    {
        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);

        if (feedbackContinueButton != null)
            feedbackContinueButton.onClick.AddListener(ContinueFromFeedback);
    }

    private void OnEnable()
    {
        ResolveReferences();
        EnsureSubscriptions();
    }

    private void LateUpdate()
    {
        if (preparedInputLockActive
            || state == EncounterState.Teaching
            || state == EncounterState.Feedback
            || state == EncounterState.Reviewing)
        {
            // AttackController.EndAttack can release MovementofPlayer after a
            // projectile resolves, so panel states enforce their own lock.
            if (movementController != null)
                movementController.disableMovement = true;
        }
    }

    private void OnDisable()
    {
        RemoveSubscriptions();
    }

    private void OnDestroy()
    {
        if (feedbackContinueButton != null)
            feedbackContinueButton.onClick.RemoveListener(ContinueFromFeedback);
    }

    private void HandleTeachDismissed(VocabularyTeachController dismissedController)
    {
        if (dismissedController != teachController)
            return;

        if (state == EncounterState.Teaching)
        {
            StartTargetChallenge();
            return;
        }

        if (state == EncounterState.Reviewing)
            ResumeTargetChallenge();
    }

    private void StartTargetChallenge()
    {
        VocabularyWordData targetWord = vocabularyBarrier != null
            ? vocabularyBarrier.TargetWord
            : null;

        if (targetWord == null
            || !tileManager.TryCreateTemporaryTargetChallengePool(targetWord, maximumPoolTiles))
        {
            FailDuringEncounter("The temporary Target Challenge pool could not be created.");
            return;
        }

        vocabularyBarrier.BeginChallenge();
        state = EncounterState.Challenge;
        preparedInputLockActive = false;
        SetPanelInputLock(false);

        ChallengeStarted?.Invoke(this);
        onChallengeStarted?.Invoke();
        Debug.Log(
            $"[VocabularyEncounterFlow] Target Challenge started for {targetWord.Word}.",
            this);
    }

    private void HandleSubmissionRejected(VocabularyWordData targetWord, string submittedWord)
    {
        if (state != EncounterState.Challenge
            || vocabularyBarrier == null
            || targetWord == null
            || !targetWord.Matches(vocabularyBarrier.TargetWord?.Word))
        {
            return;
        }

        state = EncounterState.Feedback;
        SetPanelInputLock(true);

        if (feedbackText != null)
        {
            string normalizedSubmission = string.IsNullOrWhiteSpace(submittedWord)
                ? "That word"
                : submittedWord.Trim().ToUpperInvariant();
            string highlightedSubmission =
                $"<color=#55DFF7>{normalizedSubmission}</color>";

            if (feedbackExplanationText != null)
            {
                feedbackText.text =
                    $"The barrier absorbed {highlightedSubmission}.";
                feedbackExplanationText.text =
                    $"{highlightedSubmission} is valid, but it is not the target word.";
            }
            else
            {
                feedbackText.text =
                    $"The barrier absorbed {highlightedSubmission}.\n\n"
                    + $"{highlightedSubmission} is valid, but it is not the target word.";
            }
        }

        if (feedbackPanel != null)
            feedbackPanel.SetActive(true);

        IncorrectTargetSubmitted?.Invoke(this, submittedWord);
        onIncorrectTargetFeedback?.Invoke();
        Debug.Log(
            $"[VocabularyEncounterFlow] Showing absorbed-hit feedback for '{submittedWord}'.",
            this);
    }

    public void ContinueFromFeedback()
    {
        if (state != EncounterState.Feedback)
            return;

        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);

        state = EncounterState.Reviewing;
        SetPanelInputLock(true);

        if (!teachController.ShowReview())
            FailDuringEncounter("The Teach review could not be shown after incorrect retrieval.");
    }

    private void ResumeTargetChallenge()
    {
        state = EncounterState.Challenge;
        SetPanelInputLock(false);
        Debug.Log(
            "[VocabularyEncounterFlow] Teach review closed. Resuming the same Target Challenge pool.",
            this);
    }

    private void HandleChallengeCompleted(VocabularyWordData targetWord, string submittedWord)
    {
        if (state != EncounterState.Challenge
            || vocabularyBarrier == null
            || targetWord == null
            || !targetWord.Matches(vocabularyBarrier.TargetWord?.Word))
        {
            return;
        }

        state = EncounterState.Completed;
        preparedInputLockActive = false;

        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);

        SetNormalCombatWorldPickupSuspension(false);
        RestoreOriginalInputState();
        EncounterCompleted?.Invoke(this);
        Debug.Log(
            $"[VocabularyEncounterFlow] Completed {submittedWord}; Normal Combat input restored.",
            this);
    }

    private void HandleRestorationFailed(VocabularyWordData targetWord, string submittedWord)
    {
        if (state == EncounterState.Completed || state == EncounterState.Failed)
            return;

        FailDuringEncounter(
            $"Target word '{submittedWord}' was accepted, but exact Normal Combat restoration failed.");
    }

    private bool FailBeforeStart(string reason)
    {
        state = EncounterState.Failed;
        preparedInputLockActive = false;
        SetNormalCombatWorldPickupSuspension(false);
        RestoreOriginalInputState();
        EncounterFailed?.Invoke(this);
        onFlowFailed?.Invoke();
        Debug.LogError($"[VocabularyEncounterFlow] {reason}", this);
        return false;
    }

    private void FailDuringEncounter(string reason)
    {
        state = EncounterState.Failed;
        preparedInputLockActive = false;

        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);

        RecoverSavedPool();
        SetNormalCombatWorldPickupSuspension(false);
        RestoreOriginalInputState();
        EncounterFailed?.Invoke(this);
        onFlowFailed?.Invoke();
        Debug.LogError($"[VocabularyEncounterFlow] {reason}", this);
    }

    private void RecoverSavedPool()
    {
        if (tileManager == null)
            return;

        if (tileManager.HasTemporaryTargetChallengePool)
            tileManager.TryRemoveTemporaryTargetChallengePool();

        if (tileManager.HasNormalCombatPoolSnapshot)
            tileManager.TryRestoreNormalCombatPool();
    }

    private void SetPanelInputLock(bool locked)
    {
        if (movementController != null)
            movementController.disableMovement = locked || movementWasLocked;

        if (attackController != null)
            attackController.SetAttackInputLocked(locked || attackWasLocked);
    }

    private void SetNormalCombatWorldPickupSuspension(bool suspended)
    {
        if (tileManager != null)
            tileManager.SetNormalCombatWorldPickupsSuspended(suspended);
    }

    private void CaptureOriginalInputState()
    {
        if (originalInputCaptured)
            return;

        movementWasLocked = movementController != null && movementController.disableMovement;
        attackWasLocked = attackController != null && attackController.IsAttackInputLocked;
        originalInputCaptured = true;
    }

    private void RestoreOriginalInputState()
    {
        if (!originalInputCaptured)
            return;

        if (movementController != null)
            movementController.disableMovement = movementWasLocked;

        if (attackController != null)
            attackController.SetAttackInputLocked(attackWasLocked);

        originalInputCaptured = false;
    }

    private void ResolveReferences()
    {
        if (tileManager == null)
            tileManager = FindFirstObjectByType<TileManager>();
        if (attackController == null)
            attackController = FindFirstObjectByType<AttackController>();
        if (movementController == null && attackController != null)
            movementController = attackController.movementController;
        if (movementController == null)
            movementController = FindFirstObjectByType<MovementofPlayer>();
    }

    private void EnsureSubscriptions()
    {
        if (subscribed
            || teachController == null
            || tileManager == null)
        {
            return;
        }

        teachController.TeachDismissed += HandleTeachDismissed;
        tileManager.TargetChallengeSubmissionRejected += HandleSubmissionRejected;
        tileManager.TargetChallengeCompleted += HandleChallengeCompleted;
        tileManager.TargetChallengeRestorationFailed += HandleRestorationFailed;
        subscribed = true;
    }

    private void RemoveSubscriptions()
    {
        if (!subscribed)
            return;

        if (teachController != null)
            teachController.TeachDismissed -= HandleTeachDismissed;
        if (tileManager != null)
        {
            tileManager.TargetChallengeSubmissionRejected -= HandleSubmissionRejected;
            tileManager.TargetChallengeCompleted -= HandleChallengeCompleted;
            tileManager.TargetChallengeRestorationFailed -= HandleRestorationFailed;
        }

        subscribed = false;
    }
}
