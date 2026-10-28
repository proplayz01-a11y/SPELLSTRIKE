using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class Stage3RiddleController : MonoBehaviour
{
    [Serializable]
    public sealed class RiddleEntry
    {
        [TextArea(3, 6)] public string question;
        public string answer;
        [Tooltip("Extra shuffled letters added to the cyan Target Challenge pool.")]
        public string distractorLetters;
    }

    [Header("Riddles")]
    [SerializeField] private RiddleEntry[] riddles = Array.Empty<RiddleEntry>();
    [SerializeField, Range(0.01f, 1f)] private float damageBonusPerCorrect = 0.10f;
    [SerializeField, Range(0.01f, 1f)] private float wrongAnswerDamagePercent = 0.15f;
    [SerializeField, Min(1)] private int maximumPoolTiles = 16;

    [Header("Runtime References")]
    [SerializeField] private Stage3RiddleHUDView hudView;
    [SerializeField] private TileManager tileManager;
    [SerializeField] private AttackController attackController;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private VocabularyBarrier riddleBarrier;
    [SerializeField] private VocabularyWorldTarget worldTarget;

    [Header("Outcome Laser (Optional Authored Origin)")]
    [SerializeField] private LaserBeam laserPrefab;
    [SerializeField] private Transform laserOrigin;
    [SerializeField] private Color correctBeamColor = new Color(0.25f, 0.78f, 1f, 1f);
    [SerializeField] private Color wrongBeamColor = new Color(1f, 0.12f, 0.14f, 1f);

    [Header("Post-Riddle Awakening Handoff (No Combat)")]
    [SerializeField] private GameObject bossRoot;
    [SerializeField] private MonoBehaviour bossBattleReceiver;
    // Retained only to deserialize older scene files. Never dispatch StartBattle
    // from the fourth-riddle laser: it must trigger Awakening only.
    [SerializeField] private string bossStartMessage = "StartBattle";
    [SerializeField] private bool enableBossRootAfterRiddles = true;
    [SerializeField] private bool showDormantBossDuringRiddles = true;

    [Header("Timing")]
    [Tooltip("How long the sky-blue reward or red punishment beam remains visible.")]
    [SerializeField, Min(0.1f)] private float correctFeedbackSeconds = 1.15f;
    [Tooltip("Brief pause with no riddle panel before the result beam appears.")]
    [SerializeField, Min(0f)] private float panelTransitionSeconds = 0.40f;
    [Header("Final Titan Awakening Laser")]
    [Tooltip("After the fourth riddle UI closes, shoot Titan in the chest. Only awaken him AFTER this separate laser finishes.")]
    [SerializeField, Min(0.1f)] private float titanAwakeningLaserSeconds = 1.8f;

    [Header("Events")]
    [SerializeField] private UnityEvent onRiddleTrialStarted;
    [SerializeField] private UnityEvent onWrongAnswer;
    [SerializeField] private UnityEvent onRiddleCorrect;
    [SerializeField] private UnityEvent onRiddleSequenceCompleted;

    private int currentRiddleIndex;
    private int correctCount;
    private bool trialActive;
    private bool trialCompleted;
    private bool transitioning;
    private bool subscribed;
    private Coroutine transitionRoutine;
    private MovementofPlayer lockedPlayerMovement;
    private bool ownsPlayerMovementLock;
    private bool awakeningTriggered;
    private bool normalCombatPoolRestoredForHandoff;
    private bool verifiedPoolReadyForTechnicalRetry;
    private const string RiddleLaserSpawnPointName = "LaserSpawnPoint";

    public int CurrentRiddleIndex => currentRiddleIndex;
    public int CorrectCount => correctCount;
    public bool TrialActive => trialActive;
    public bool TrialCompleted => trialCompleted;

    // Called only by this Node 4's trigger after a technical failure has
    // recovered the exact original tile pool. Never allows an incomplete
    // riddle sequence, dead player, or awakened Titan to restart combat.
    public bool CanRetryAfterTechnicalFailure
    {
        get
        {
            ResolvePlayerHealth();
            return verifiedPoolReadyForTechnicalRetry
                && isActiveAndEnabled
                && !trialActive && !trialCompleted && !transitioning
                && tileManager != null
                && tileManager.CurrentMode == TilePoolMode.NormalCombat
                && !tileManager.HasTemporaryTargetChallengePool
                && !tileManager.HasNormalCombatPoolSnapshot
                && playerHealth != null && playerHealth.currentHealth > 0
                && bossRoot != null && bossRoot.activeInHierarchy
                && bossBattleReceiver is TitanEarthbreakerController titan
                && titan.gameObject == bossRoot
                && titan.isActiveAndEnabled
                && titan.State == TitanEarthbreakerController.TitanState.Dormant
                && !awakeningTriggered;
        }
    }

    /// <summary>
    /// Review setup reads the authored riddle answer set, including the
    /// production defaults when this scene has no explicit riddle entries.
    /// It does not start, answer, or modify the gameplay trial.
    /// </summary>
    public string[] GetRiddleAnswersForReview()
    {
        RiddleEntry[] configured = riddles != null && riddles.Length > 0
            ? riddles
            : BuildDefaultRiddles();
        string[] answers = new string[configured.Length];
        for (int i = 0; i < configured.Length; i++)
            answers[i] = configured[i]?.answer?.Trim().ToUpperInvariant() ?? string.Empty;
        return answers;
    }

    private void Reset()
    {
        riddles = BuildDefaultRiddles();
    }

    private void Awake()
    {
        if (riddles == null || riddles.Length == 0)
            riddles = BuildDefaultRiddles();

        ResolveReferences();
        EnsureSubscriptions();

        // The *same* animated Titan should stand visibly as a statue before
        // Awakening. Only older staging configured for hidden spawn is hidden.
        if (bossRoot != null && enableBossRootAfterRiddles && !showDormantBossDuringRiddles)
            bossRoot.SetActive(false);
    }

    private void OnEnable()
    {
        ResolveReferences();
        EnsureSubscriptions();
    }

    private void OnDisable()
    {
        RemoveSubscriptions();

        if (trialActive && !trialCompleted)
            RecoverInterruptedTrial();
        else
            ReleasePlayerMovementLock();
    }

    private void OnDestroy()
    {
        ReleasePlayerMovementLock();

        // The bonus intentionally survives the riddle-to-boss handoff while this
        // Stage 3 controller exists, then disappears when Stage 3 unloads.
        if (attackController != null)
            attackController.ClearStage3RiddleDamageBonus();
    }

    public void StartBattle()
    {
        BeginRiddleTrial();
    }

    public bool BeginRiddleTrial()
    {
        if (trialActive || trialCompleted)
            return false;

        ResolveReferences();
        EnsureSubscriptions();

        if (tileManager == null || attackController == null || riddleBarrier == null)
        {
            Debug.LogError(
                "[Stage3RiddleController] Cannot start: TileManager, AttackController, or riddle VocabularyBarrier is missing.",
                this);
            return false;
        }

        if (riddles == null || riddles.Length == 0)
        {
            Debug.LogError("[Stage3RiddleController] No riddles are configured.", this);
            return false;
        }

        if (tileManager.HasNormalCombatPoolSnapshot)
        {
            Debug.LogError(
                "[Stage3RiddleController] A Normal Combat snapshot is already active. Refusing to overwrite it.",
                this);
            return false;
        }

        if (!tileManager.TrySnapshotAndHideNormalCombatPool())
            return false;

        // Hold Van in place for the entire four-riddle sequence, including the
        // feedback/transition pauses. This is separate from Attack's temporary
        // disableMovement toggle, so attack animations cannot unlock walking.
        if (!AcquirePlayerMovementLock())
        {
            RecoverInterruptedTrial();
            return false;
        }

        // Node 4's BoxCollider/StageNodeTrigger is the authority that starts
        // this trial. Keep the whole riddle HUD root inactive everywhere else,
        // then wake it only after the encounter has genuinely begun.
        if (hudView != null && !hudView.gameObject.activeSelf)
            hudView.gameObject.SetActive(true);

        tileManager.SetPreserveNormalCombatSnapshotAfterTargetChallengeSuccess(true);
        attackController.ClearStage3RiddleDamageBonus();
        normalCombatPoolRestoredForHandoff = false;
        verifiedPoolReadyForTechnicalRetry = false;
        currentRiddleIndex = 0;
        correctCount = 0;
        trialActive = true;
        trialCompleted = false;
        transitioning = false;

        if (!BeginCurrentRiddle())
        {
            RecoverInterruptedTrial();
            return false;
        }

        onRiddleTrialStarted?.Invoke();
        Debug.Log("[Stage3RiddleController] Four-riddle trial started.", this);
        return true;
    }

    private bool BeginCurrentRiddle()
    {
        if (!trialActive || currentRiddleIndex < 0 || currentRiddleIndex >= riddles.Length)
            return false;

        RiddleEntry entry = riddles[currentRiddleIndex];
        if (entry == null || string.IsNullOrWhiteSpace(entry.answer))
        {
            Debug.LogError($"[Stage3RiddleController] Riddle {currentRiddleIndex + 1} has no answer.", this);
            return false;
        }

        // Riddle challenges always fill the existing 8x2 TilePoolPanel. Do not
        // silently show a partially empty second row if someone changes clues
        // or distractors in the Inspector later.
        int letterCount = CountChallengeLetters(entry.answer)
            + CountChallengeLetters(entry.distractorLetters);
        if (letterCount != maximumPoolTiles || maximumPoolTiles != 16)
        {
            Debug.LogError(
                $"[Stage3RiddleController] Riddle {currentRiddleIndex + 1} must contain "
                + $"exactly 16 cyan tiles, but has {letterCount}. Check answer/distractors.",
                this);
            return false;
        }

        VocabularyWordData targetWord = VocabularyWordData.CreateRuntimeChallenge(
            entry.answer,
            entry.distractorLetters,
            $"stage3_riddle_{currentRiddleIndex + 1}");

        riddleBarrier.SetRuntimeTargetWord(targetWord);
        riddleBarrier.SetBlockNonTargetDamageWhileActive(true);
        riddleBarrier.SetGuaranteeTargetLettersOnStart(false);
        riddleBarrier.BeginChallenge();

        if (!tileManager.TryCreateTemporaryTargetChallengePool(targetWord, maximumPoolTiles))
        {
            Debug.LogError(
                $"[Stage3RiddleController] Could not create cyan challenge tiles for riddle {currentRiddleIndex + 1}.",
                this);
            return false;
        }

        // TileManager protects the transition by locking input while it swaps the
        // pool. Once the new cyan pool exists, the normal Attack button owns
        // validation again.
        // On the initial question unlock as usual. During subsequent question
        // transitions, wait until the panel finishes fading back in.
        attackController.SetAttackInputLocked(transitioning);

        if (hudView != null)
            hudView.ShowRiddle(
                currentRiddleIndex + 1,
                riddles.Length,
                entry.question,
                immediate: currentRiddleIndex == 0);

        Debug.Log(
            $"[Stage3RiddleController] Riddle {currentRiddleIndex + 1}/{riddles.Length} active. "
            + $"Answer={targetWord.Word}, cyanTiles={tileManager.GetTemporaryTargetChallengeLetters()}.",
            this);
        return true;
    }

    private void HandleSubmissionRejected(VocabularyWordData targetWord, string submittedWord)
    {
        if (!trialActive || transitioning || !MatchesCurrentRiddle(targetWord))
            return;

        transitioning = true;
        attackController?.SetAttackInputLocked(true);
        transitionRoutine = StartCoroutine(ResolveOutcome(false));
        Debug.Log(
            $"[Stage3RiddleController] Wrong riddle answer '{submittedWord}'. "
            + "Fading out, then red punishment laser, then retrying this riddle.",
            this);
    }

    private void HandleTargetChallengeCompleted(VocabularyWordData targetWord, string submittedWord)
    {
        if (!trialActive || transitioning || !MatchesCurrentRiddle(targetWord))
            return;

        transitioning = true;
        attackController?.SetAttackInputLocked(true);
        transitionRoutine = StartCoroutine(ResolveOutcome(true));
    }

    private IEnumerator ResolveOutcome(bool correct)
    {
        // Strict order: remove the panel from view, reveal Van, then fire the
        // outcome beam. Keep both movement and Attack locked during the beat.
        if (hudView != null)
            hudView.HideRiddle(false);

        yield return new WaitForSecondsRealtime(
            (hudView != null ? hudView.FadeDuration : 0f) + panelTransitionSeconds);

        ResolvePlayerHealth();
        ResolveLaserOrigin();
        Stage3RiddleOutcomeBeam.Play(
            playerHealth, laserOrigin,
            correct ? correctBeamColor : wrongBeamColor,
            correctFeedbackSeconds);

        if (correct)
        {
            // The sky-blue beam itself marks the +10% reward. Do not apply the
            // buff early while the HUD is still obscuring the player.
            correctCount++;
            attackController.SetStage3RiddleDamageBonus(correctCount * damageBonusPerCorrect);
            hudView?.ShowCorrectFeedback(
                $"Attack +{damageBonusPerCorrect * 100f:0}% (Total +{attackController.Stage3RiddleDamageBonusFraction * 100f:0}%)");
            onRiddleCorrect?.Invoke();
            Debug.Log(
                $"[Stage3RiddleController] SKY-BLUE reward beam -> attack damage "
                + $"+{attackController.Stage3RiddleDamageBonusFraction * 100f:0}% total.",
                this);
        }
        else
        {
            // PlayerHealth is the sole damage authority. The red laser only
            // visualizes this exact scripted penalty; no raycast double hit.
            ApplyWrongAnswerDamage();
            hudView?.ShowWrongFeedback($"-{wrongAnswerDamagePercent * 100f:0}% Max HP");
            onWrongAnswer?.Invoke();
            Debug.Log(
                $"[Stage3RiddleController] RED punishment beam -> "
                + $"{wrongAnswerDamagePercent * 100f:0}% max HP damage.",
                this);
        }

        // Keep the left-side feedback readable until the laser is over, then
        // slide it BACK out to the left before the next parchment fades in.
        // The full entrance/exit is separate from the scene-space laser.
        yield return new WaitForSecondsRealtime(Mathf.Max(
            correctFeedbackSeconds,
            hudView != null ? hudView.FeedbackHoldSeconds : 0f));
        if (hudView != null)
            yield return hudView.SlideFeedbackBackLeft();

        if (!correct && playerHealth != null && playerHealth.currentHealth <= 0)
        {
            RecoverInterruptedTrial();
            yield break;
        }

        if (correct && currentRiddleIndex >= riddles.Length - 1)
        {
            // No extra panel flash after the final riddle: let the final blue
            // laser finish, restore tiles, then start Awakening ONLY.
            CompleteRiddleSequence();
            yield break;
        }

        if (correct)
        {
            currentRiddleIndex++;
            if (!BeginCurrentRiddle())
            {
                RecoverInterruptedTrial();
                yield break;
            }
        }
        else if (hudView != null)
        {
            // Wrong answer never changes the question or regenerates its tiles.
            hudView.ShowRiddle(
                currentRiddleIndex + 1, riddles.Length,
                riddles[currentRiddleIndex].question);
        }

        yield return new WaitForSecondsRealtime(hudView != null ? hudView.FadeDuration : 0f);

        attackController?.SetAttackInputLocked(false);
        transitioning = false;
        transitionRoutine = null;
    }

    private void CompleteRiddleSequence()
    {
        tileManager.SetPreserveNormalCombatSnapshotAfterTargetChallengeSuccess(false);

        // All four answers were required. The final handoff is still pending:
        // only exact pool restoration AND a verified Titan awakening can mark
        // the trial complete. A missing snapshot is not proof of restoration.
        bool hadSnapshot = tileManager.HasNormalCombatPoolSnapshot;
        bool removed = tileManager.CurrentMode == TilePoolMode.NormalCombat
            && !tileManager.HasTemporaryTargetChallengePool;
        if (!removed)
            removed = tileManager.TryRemoveTemporaryTargetChallengePool();

        bool restored = hadSnapshot && removed && tileManager.TryRestoreNormalCombatPool();
        normalCombatPoolRestoredForHandoff = restored
            && tileManager.CurrentMode == TilePoolMode.NormalCombat
            && !tileManager.HasTemporaryTargetChallengePool
            && !tileManager.HasNormalCombatPoolSnapshot;

        if (!normalCombatPoolRestoredForHandoff)
        {
            transitionRoutine = null;
            HandleTechnicalHandoffFailure("Riddle 4 was correct, but exact Normal Combat pool restoration failed.");
            return;
        }

        transitionRoutine = null;
        riddleBarrier.HideChallenge();
        // Pool swaps preserve their previous input-lock state. Riddles are
        // complete, but the boss is NOT combat-active during Awakening.
        attackController?.SetAttackInputLocked(false);

        if (hudView != null)
        {
            hudView.HideRiddle(true);
            hudView.gameObject.SetActive(false);
        }

        // Keep trialActive true until the handoff is verified, but do not
        // strand Van in the riddle movement lock during the final laser.
        ReleasePlayerMovementLock();

        // Strict cinematic order: closed riddle HUD -> Titan-directed laser
        // (separate from Van's last reward beam) -> awakening animation.
        transitionRoutine = StartCoroutine(FireFinalTitanLaserThenAwaken());
    }

    private IEnumerator FireFinalTitanLaserThenAwaken()
    {
        if (bossRoot == null)
        {
            transitionRoutine = null;
            HandleTechnicalHandoffFailure("Final Titan laser blocked: no authored boss root.");
            yield break;
        }

        ResolveLaserOrigin();
        float laserSeconds = Mathf.Max(0.1f, titanAwakeningLaserSeconds);
        bool fired = Stage3RiddleOutcomeBeam.PlayTowardsTitan(
            bossRoot, laserOrigin, correctBeamColor, laserSeconds);
        if (!fired)
        {
            transitionRoutine = null;
            HandleTechnicalHandoffFailure("Final Titan laser failed. Awakening withheld.");
            yield break;
        }

        Debug.Log(
            $"[Stage3RiddleController] Riddle UI closed -> final sky-blue laser HIT Titan. "
            + $"Awakening remains locked for {laserSeconds:0.00}s.", this);
        // Beam uses realtime too; the extra frame ensures it fully disappears
        // before any part of the Awakening animation starts.
        yield return new WaitForSecondsRealtime(laserSeconds + 0.08f);
        if (!isActiveAndEnabled)
            yield break;

        Debug.Log("[Stage3RiddleController] Final Titan laser finished -> unlocking Awakening V004.", this);
        transitionRoutine = null;
        if (!TriggerBossAwakeningOnly())
        {
            HandleTechnicalHandoffFailure("Titan did not enter the authored Awakening state after the final laser.");
            yield break;
        }

        trialActive = false;
        trialCompleted = true;
        transitioning = false;
        normalCombatPoolRestoredForHandoff = false;
        verifiedPoolReadyForTechnicalRetry = false;
        onRiddleSequenceCompleted?.Invoke();
        Debug.Log(
            $"[Stage3RiddleController] Trial complete: {correctCount}/{riddles.Length} correct, "
            + $"boss attack bonus +{attackController.Stage3RiddleDamageBonusFraction * 100f:0}%.",
            this);
    }

    private bool TriggerBossAwakeningOnly()
    {
        if (awakeningTriggered)
            return false;

        // Deliberately ignore the old serialized bossStartMessage, which may
        // still contain "StartBattle" in V003/canonical scene YAML.
        // Production verifies the real boss state instead of assuming a
        // SendMessage or animation-only preview accepted the handoff.
        if (!(bossBattleReceiver is TitanEarthbreakerController titan)
            || bossRoot == null || titan.gameObject != bossRoot
            || !titan.isActiveAndEnabled
            || titan.State != TitanEarthbreakerController.TitanState.Dormant)
            return false;

        titan.StartAwakening();
        if (titan.State != TitanEarthbreakerController.TitanState.Awakening)
            return false;

        awakeningTriggered = true;
        Debug.Log("[Stage3RiddleController] Fourth-riddle blue laser -> verified Titan Awakening V004. Combat remains disabled.", this);
        return true;
    }

    private void HandleTechnicalHandoffFailure(string reason)
    {
        Debug.LogError($"[Stage3RiddleController] {reason} Technical recovery required; no boss handoff or riddle credit.", this);
        bool exactPoolRecovered = RecoverInterruptedTrial();
        verifiedPoolReadyForTechnicalRetry = exactPoolRecovered;

        if (!exactPoolRecovered)
        {
            Debug.LogError("[Stage3RiddleController] Exact Normal Combat restoration remains unverified. Node 4 stays locked; restart the stage after resolving the tile-pool fault.", this);
            return;
        }

        StageNodeTrigger trigger = GetComponent<StageNodeTrigger>();
        if (trigger == null || !trigger.TryRearmNode4AfterTechnicalFailure(this))
        {
            Debug.LogError("[Stage3RiddleController] Node 4 retry denied: requires a living player, dormant active Titan and valid Node 4 progression. Restart the stage if these cannot be restored.", this);
        }
    }

    private void HandleRestorationFailed(VocabularyWordData targetWord, string submittedWord)
    {
        if (!trialActive || !MatchesCurrentRiddle(targetWord))
            return;

        // A matching, active target-challenge restoration error is technical,
        // not a wrong answer. The shared handler only rearms Node 4 after
        // exact pool recovery and living-player/dormant-Titan checks pass.
        HandleTechnicalHandoffFailure(
            $"Target challenge restoration failed unexpectedly for '{submittedWord}'.");
    }

    private void ApplyWrongAnswerDamage()
    {
        ResolvePlayerHealth();
        if (playerHealth == null)
        {
            Debug.LogWarning("[Stage3RiddleController] Wrong answer could not damage Van: PlayerHealth missing.", this);
            return;
        }

        int damage = Mathf.Max(1, Mathf.RoundToInt(playerHealth.maxHealth * wrongAnswerDamagePercent));

        // PlayerHealth is authoritative; the separate colored outcome beam is
        // presentation-only and cannot double-damage via physics raycasts.
        playerHealth.TakeDamage(damage);
    }

    private bool MatchesCurrentRiddle(VocabularyWordData targetWord)
    {
        if (targetWord == null || currentRiddleIndex < 0 || currentRiddleIndex >= riddles.Length)
            return false;

        RiddleEntry current = riddles[currentRiddleIndex];
        return current != null && targetWord.Matches(current.answer);
    }

    private bool RecoverInterruptedTrial()
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        bool exactPoolRecovered = normalCombatPoolRestoredForHandoff;
        if (tileManager != null)
        {
            tileManager.SetPreserveNormalCombatSnapshotAfterTargetChallengeSuccess(false);
            bool removed = tileManager.CurrentMode == TilePoolMode.NormalCombat
                && !tileManager.HasTemporaryTargetChallengePool;
            if (!removed)
                removed = tileManager.TryRemoveTemporaryTargetChallengePool();
            if (removed && tileManager.HasNormalCombatPoolSnapshot)
                exactPoolRecovered = tileManager.TryRestoreNormalCombatPool();

            exactPoolRecovered = exactPoolRecovered && removed
                && tileManager.CurrentMode == TilePoolMode.NormalCombat
                && !tileManager.HasTemporaryTargetChallengePool
                && !tileManager.HasNormalCombatPoolSnapshot;
        }
        else exactPoolRecovered = false;

        if (attackController != null)
            attackController.ClearStage3RiddleDamageBonus();

        if (riddleBarrier != null)
            riddleBarrier.HideChallenge();
        if (hudView != null)
        {
            hudView.HideRiddle(true);
            hudView.gameObject.SetActive(false);
        }

        trialActive = false;
        trialCompleted = false;
        transitioning = false;
        attackController?.SetAttackInputLocked(false);
        ReleasePlayerMovementLock();
        normalCombatPoolRestoredForHandoff = false;
        return exactPoolRecovered;
    }

    private bool AcquirePlayerMovementLock()
    {
        if (ownsPlayerMovementLock)
            return true;

        MovementofPlayer movement = PersistentPlayerRoot.Instance != null
            ? PersistentPlayerRoot.Instance.Movement
            : null;
        movement ??= attackController != null ? attackController.movementController : null;
        movement ??= attackController != null ? attackController.GetComponent<MovementofPlayer>() : null;
        movement ??= FindFirstObjectByType<MovementofPlayer>();

        if (movement == null || !movement.isActiveAndEnabled)
        {
            Debug.LogError(
                "[Stage3RiddleController] Cannot begin riddle trial: active player MovementofPlayer "
                + "was not found. Refusing to start without freezing Van.",
                this);
            return false;
        }

        movement.AcquireExternalMovementLock();
        lockedPlayerMovement = movement;
        ownsPlayerMovementLock = true;
        Debug.Log("[Stage3RiddleController] Player movement locked for four-riddle trial.", this);
        return true;
    }

    private void ReleasePlayerMovementLock()
    {
        if (!ownsPlayerMovementLock)
            return;

        ownsPlayerMovementLock = false;
        if (lockedPlayerMovement != null)
            lockedPlayerMovement.ReleaseExternalMovementLock();
        lockedPlayerMovement = null;
        Debug.Log("[Stage3RiddleController] Player movement unlocked after riddle trial.", this);
    }

    private void ResolveReferences()
    {
        tileManager ??= TileManager.Instance != null
            ? TileManager.Instance
            : FindFirstObjectByType<TileManager>();
        attackController ??= FindFirstObjectByType<AttackController>();
        hudView ??= FindFirstObjectByType<Stage3RiddleHUDView>(FindObjectsInactive.Include);
        riddleBarrier ??= GetComponentInChildren<VocabularyBarrier>(true);
        worldTarget ??= GetComponentInChildren<VocabularyWorldTarget>(true);
        ResolvePlayerHealth();
        ResolveLaserOrigin();
    }

    private void ResolveLaserOrigin()
    {
        // A manually assigned origin always takes priority. This scene-local
        // fallback also handles an unsaved LaserSpawnPoint in the live editor
        // scene without rewriting Stage3Scene or touching other emitters.
        if (laserOrigin != null)
            return;

        Scene scene = gameObject.scene;
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        Transform found = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name != RiddleLaserSpawnPointName)
                    continue;

                if (found != null)
                {
                    Debug.LogError(
                        "[Stage3RiddleController] Multiple LaserSpawnPoint objects found in Stage 3. "
                        + "Assign laserOrigin explicitly so the riddle uses the correct emitter.",
                        this);
                    return;
                }

                found = candidate;
            }
        }

        if (found == null)
            return;

        laserOrigin = found;
        Debug.Log(
            $"[Stage3RiddleController] Using authored riddle laser origin: {found.name} "
            + $"at {found.position}. Both blue and red beams share this source.",
            this);
    }

    private void ResolvePlayerHealth()
    {
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();
    }

    private void EnsureSubscriptions()
    {
        if (subscribed || tileManager == null)
            return;

        tileManager.TargetChallengeSubmissionRejected += HandleSubmissionRejected;
        tileManager.TargetChallengeCompleted += HandleTargetChallengeCompleted;
        tileManager.TargetChallengeRestorationFailed += HandleRestorationFailed;
        subscribed = true;
    }

    private void RemoveSubscriptions()
    {
        if (!subscribed || tileManager == null)
            return;

        tileManager.TargetChallengeSubmissionRejected -= HandleSubmissionRejected;
        tileManager.TargetChallengeCompleted -= HandleTargetChallengeCompleted;
        tileManager.TargetChallengeRestorationFailed -= HandleRestorationFailed;
        subscribed = false;
    }

    private static RiddleEntry[] BuildDefaultRiddles()
    {
        return new[]
        {
            new RiddleEntry
            {
                question = "I drift across the desert sea, shaping dunes with every breath. "
                    + "I have no feet, yet I roam free. What am I?",
                answer = "WIND",
                distractorLetters = "SCALETRPUKYO"
            },
            new RiddleEntry
            {
                question = "I have no walls, yet stretch above every dune. By day I burn blue; "
                    + "by night I cradle stars. What am I?",
                answer = "SKY",
                distractorLetters = "WINDOASLCTRPE"
            },
            new RiddleEntry
            {
                question = "I stretch farther than the eye can see, where heat rules and water "
                    + "is rare. Dunes are my waves, yet I am no sea. What am I?",
                answer = "DESERT",
                distractorLetters = "WINOSKYALP"
            },
            new RiddleEntry
            {
                question = "I cross the heavens without feet, turn stone to fire, and vanish "
                    + "beyond the dunes each night. What am I?",
                answer = "SUN",
                distractorLetters = "OASIWINDKYLTR"
            }
        };
    }

    private static int CountChallengeLetters(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        int count = 0;
        foreach (char raw in value)
        {
            char letter = char.ToUpperInvariant(raw);
            if (letter >= 'A' && letter <= 'Z')
                count++;
        }
        return count;
    }
}
