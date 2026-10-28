using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public sealed class TutorialDirector : MonoBehaviour
{
    public enum TutorialStep
    {
        IntroWelcome,
        IntroGameExplanation,
        MoveToBridge,
        MoveToShrine,
        TileIntroduction,
        TileSelectionExplanation,
        FormPlay,
        StrikePlay,
        CollectReplacementTiles,
        DefeatTrainingDummy,
        ClaimSpellbook,
        Complete
    }

    [Header("Core References")]
    [SerializeField] private TutorialHUDController hud;
    [SerializeField] private TileManager tileManager;
    [SerializeField] private AttackController attackController;
    [SerializeField] private MovementofPlayer movementController;
    [SerializeField] private TileSpawner tileSpawner;
    [SerializeField] private GameObject trainingDummy;
    [SerializeField] private TutorialSpellbookPickup spellbookPickup;

    [Header("Tutorial Settings")]
    [SerializeField] private string openingWord = "PLAY";
    [SerializeField] private bool startAutomatically = true;
    [SerializeField] private bool resetSavedCompletionForTesting;

    [Header("Optional Step Objects")]
    [SerializeField] private GameObject continueInputBlocker;
    [SerializeField] private TutorialNavigationGuide navigationGuide;
    [SerializeField] private Transform bridgeCheckpoint;
    [SerializeField] private Transform shrineCheckpoint;
    [SerializeField] private GameObject arenaEntranceIndicator;
    [SerializeField] private GameObject spellbookIndicator;
    [SerializeField] private GameObject mobileCollectButton;

    [Header("Events")]
    [SerializeField] private UnityEvent onTutorialStarted;
    [SerializeField] private UnityEvent onOpeningWordCompleted;
    [SerializeField] private UnityEvent onDummyDefeated;
    [SerializeField] private UnityEvent onSpellbookClaimed;
    [SerializeField] private UnityEvent onTutorialCompleted;

    private const string CompletionKey = "SpellStrike_TutorialCompleted";
    private TutorialStep currentStep;
    private bool running;
    private readonly List<WorldTile> replacementTiles = new();
    private int replacementTileTotal;

    public TutorialStep CurrentStep => currentStep;
    public bool IsRunning => running;

    private void OnEnable()
    {
        TutorialResponsiveLayout.EnsureForScene(gameObject.scene);

        if (attackController != null)
        {
            attackController.WordSubmitted += HandleWordSubmitted;
            attackController.WordSubmissionRejected += HandleWordSubmissionRejected;
        }

        if (spellbookPickup != null)
        {
            spellbookPickup.Collected += HandleSpellbookCollected;
            spellbookPickup.PlayerRangeChanged += HandleSpellbookRangeChanged;
        }

        SubscribeToTileManager();
    }

    private void OnDisable()
    {
        if (attackController != null)
        {
            attackController.WordSubmitted -= HandleWordSubmitted;
            attackController.WordSubmissionRejected -= HandleWordSubmissionRejected;
            attackController.ClearWordSubmissionFilter();
        }

        if (spellbookPickup != null)
        {
            spellbookPickup.Collected -= HandleSpellbookCollected;
            spellbookPickup.PlayerRangeChanged -= HandleSpellbookRangeChanged;
        }

        SetOptionalObject(mobileCollectButton, false);

        UnsubscribeFromTileSpawner();
        ClearReplacementTileTracking();

        if (tileManager != null)
        {
            tileManager.WordChanged -= HandleGuidedWordChanged;
            tileManager.TileSelectionRejected -= HandleGuidedTileRejected;
            tileManager.ClearTileSelectionFilter();
        }
    }

    private void Start()
    {
        if (resetSavedCompletionForTesting)
            PlayerPrefs.DeleteKey(CompletionKey);

        if (startAutomatically)
            BeginTutorial();
    }

    public void BeginTutorial()
    {
        if (running)
            return;

        running = true;

        ResolveReferences();
        SubscribeToTileManager();
        SetMovementLocked(true);

        if (attackController != null)
            attackController.SetAttackInputLocked(true);

        if (trainingDummy != null)
            trainingDummy.SetActive(false);

        if (spellbookPickup != null)
            spellbookPickup.PrepareHidden();

        SetOptionalObject(arenaEntranceIndicator, false);
        SetOptionalObject(spellbookIndicator, false);
        if (navigationGuide != null)
            navigationGuide.ClearTarget();
        SetOptionalObject(continueInputBlocker, true);

        if (hud != null)
        {
            hud.SetVisible(true);
            hud.SetPlatformPrompts(TutorialResponsiveLayout.IsMobilePresentation);
        }

        onTutorialStarted?.Invoke();
        StartCoroutine(PrepareIntroduction());
    }

    private IEnumerator PrepareIntroduction()
    {
        yield return null;
        yield return null;

        if (tileManager == null)
        {
            Debug.LogError("[TutorialDirector] TileManager reference is missing.");
            yield break;
        }

        tileManager.ClearAllTiles();
        SetStep(TutorialStep.IntroWelcome);
    }

    public void AdvanceIntroductionPanel()
    {
        if (!running)
            return;

        switch (currentStep)
        {
            case TutorialStep.IntroWelcome:
                SetStep(TutorialStep.IntroGameExplanation);
                return;

            case TutorialStep.IntroGameExplanation:
                SetOptionalObject(continueInputBlocker, false);
                SetMovementLocked(false);

                if (hud != null)
                    hud.SetVisible(true);

                SetStep(TutorialStep.MoveToBridge);
                return;

            case TutorialStep.TileIntroduction:
                SetStep(TutorialStep.TileSelectionExplanation);
                return;

            case TutorialStep.TileSelectionExplanation:
                SetOptionalObject(continueInputBlocker, false);
                BeginFormPlay();
                return;
        }
    }

    public void NotifyCheckpointReached(TutorialCheckpoint.CheckpointType checkpoint)
    {
        if (!running)
            return;

        if (checkpoint == TutorialCheckpoint.CheckpointType.Bridge
            && currentStep == TutorialStep.MoveToBridge)
        {
            SetStep(TutorialStep.MoveToShrine);
            return;
        }

        if (checkpoint != TutorialCheckpoint.CheckpointType.Shrine
            || currentStep != TutorialStep.MoveToShrine)
        {
            return;
        }

        if (navigationGuide != null)
            navigationGuide.ClearTarget();

        SetMovementLocked(true);
        SetOptionalObject(continueInputBlocker, true);
        SetStep(TutorialStep.TileIntroduction);
    }

    public void BeginFormPlay()
    {
        if (!running)
            return;

        StartCoroutine(PrepareOpeningWord());
    }

    private IEnumerator PrepareOpeningWord()
    {
        yield return null;

        if (tileManager == null)
        {
            Debug.LogError("[TutorialDirector] TileManager reference is missing.");
            yield break;
        }

        bool prepared = tileManager.PrepareGuidedWordPool(openingWord, 16);
        if (!prepared)
            Debug.LogError($"[TutorialDirector] Could not prepare opening word '{openingWord}'.");

        if (attackController != null)
        {
            attackController.SetAttackInputLocked(true);
            attackController.SetWordSubmissionFilter(IsOpeningWord);
        }

        tileManager.SetTileSelectionFilter(IsExpectedGuidedTile);

        if (hud != null)
            hud.SetVisible(true);

        SetStep(TutorialStep.FormPlay);
        RefreshGuidedTilePointer();
    }

    private void ResolveReferences()
    {
        if (hud == null)
        {
            TutorialHUDController[] huds = Object.FindObjectsByType<TutorialHUDController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < huds.Length; i++)
            {
                if (huds[i] != null && huds[i].gameObject.scene == gameObject.scene)
                {
                    hud = huds[i];
                    break;
                }
            }
        }

        if (movementController == null && attackController != null)
            movementController = attackController.movementController;

        if (movementController == null)
            movementController = FindFirstObjectByType<MovementofPlayer>();

        if (tileSpawner == null)
            tileSpawner = FindFirstObjectByType<TileSpawner>();

        SubscribeToTileSpawner();

        if (navigationGuide != null && movementController != null)
            navigationGuide.SetPlayer(movementController.transform);
    }

    private void SetMovementLocked(bool locked)
    {
        if (movementController != null)
            movementController.disableMovement = locked;
    }

    private bool IsOpeningWord(string submittedWord)
    {
        return string.Equals(
            submittedWord,
            openingWord,
            System.StringComparison.OrdinalIgnoreCase);
    }

    private void SubscribeToTileManager()
    {
        if (tileManager == null)
            return;

        tileManager.WordChanged -= HandleGuidedWordChanged;
        tileManager.TileSelectionRejected -= HandleGuidedTileRejected;
        tileManager.WordChanged += HandleGuidedWordChanged;
        tileManager.TileSelectionRejected += HandleGuidedTileRejected;
    }

    private bool IsExpectedGuidedTile(Tile tile)
    {
        if (tile == null || currentStep != TutorialStep.FormPlay)
            return false;

        string currentWord = tileManager.GetCurrentWord().ToUpperInvariant();
        string requiredWord = openingWord.ToUpperInvariant();

        return currentWord.Length < requiredWord.Length
               && char.ToUpperInvariant(tile.letter) == requiredWord[currentWord.Length];
    }

    private void HandleGuidedWordChanged(string currentWord)
    {
        if (!running
            || (currentStep != TutorialStep.FormPlay
                && currentStep != TutorialStep.StrikePlay))
        {
            return;
        }

        currentWord = currentWord.ToUpperInvariant();
        string requiredWord = openingWord.ToUpperInvariant();

        if (currentStep == TutorialStep.StrikePlay)
        {
            if (currentWord != requiredWord)
            {
                SetStep(TutorialStep.FormPlay);
                RefreshGuidedTilePointer();
            }

            return;
        }

        if (currentWord == requiredWord)
        {
            if (hud != null)
                hud.PointAt(null);

            SetStep(TutorialStep.StrikePlay);
            return;
        }

        RefreshGuidedTilePointer();
    }

    private void HandleGuidedTileRejected(Tile rejectedTile)
    {
        if (!running || currentStep != TutorialStep.FormPlay || hud == null)
            return;

        int nextIndex = tileManager.GetCurrentWord().Length;
        string requiredWord = openingWord.ToUpperInvariant();
        if (nextIndex >= requiredWord.Length)
            return;

        RefreshGuidedTilePointer();
        hud.SetDialogue(
            $"{char.ToUpperInvariant(rejectedTile.letter)} is not the next letter. Select {requiredWord[nextIndex]}.");
    }

    private void RefreshGuidedTilePointer()
    {
        if (hud == null || tileManager == null)
            return;

        string requiredWord = openingWord.ToUpperInvariant();
        int nextIndex = tileManager.GetCurrentWord().Length;

        if (nextIndex >= requiredWord.Length)
        {
            hud.PointAt(null);
            return;
        }

        char expectedLetter = requiredWord[nextIndex];
        RectTransform expectedTile = null;

        foreach (Transform child in tileManager.tilePoolPanel)
        {
            Tile tile = child.GetComponent<Tile>();
            if (tile != null
                && tile.IsSelectable()
                && char.ToUpperInvariant(tile.letter) == expectedLetter)
            {
                expectedTile = child as RectTransform;
                break;
            }
        }

        hud.PointAt(expectedTile);
        hud.SetObjectives(
            $"Form {requiredWord} from the Tile Pool",
            $"Select {expectedLetter} next.");
        hud.SetDialogue($"Build the word in order: {GetGuidedProgress(requiredWord, nextIndex)}");
    }

    private static string GetGuidedProgress(string requiredWord, int selectedCount)
    {
        System.Text.StringBuilder progress = new();

        for (int i = 0; i < requiredWord.Length; i++)
        {
            if (i > 0)
                progress.Append(' ');

            progress.Append(i < selectedCount ? requiredWord[i] : '_');
        }

        return progress.ToString();
    }

    private void HandleWordSubmitted(string submittedWord)
    {
        if (!running || currentStep != TutorialStep.StrikePlay)
            return;

        onOpeningWordCompleted?.Invoke();

        if (attackController != null)
        {
            attackController.SetAttackInputLocked(true);
            attackController.ClearWordSubmissionFilter();
        }

        if (tileManager != null)
            tileManager.ClearTileSelectionFilter();

        if (hud != null)
            hud.PointAt(null);

        SetStep(TutorialStep.CollectReplacementTiles);
    }

    private void SubscribeToTileSpawner()
    {
        if (tileSpawner == null)
            return;

        tileSpawner.SpecificTilesSpawned -= HandleSpecificTilesSpawned;
        tileSpawner.SpecificTilesSpawned += HandleSpecificTilesSpawned;
    }

    private void UnsubscribeFromTileSpawner()
    {
        if (tileSpawner != null)
            tileSpawner.SpecificTilesSpawned -= HandleSpecificTilesSpawned;
    }

    private void HandleSpecificTilesSpawned(IReadOnlyList<WorldTile> spawnedTiles)
    {
        if (!running || currentStep != TutorialStep.CollectReplacementTiles)
            return;

        ClearReplacementTileTracking();

        if (spawnedTiles != null)
        {
            foreach (WorldTile tile in spawnedTiles)
            {
                if (tile == null)
                    continue;

                replacementTiles.Add(tile);
                tile.Collected -= HandleReplacementTileCollected;
                tile.Collected += HandleReplacementTileCollected;
            }
        }

        replacementTileTotal = replacementTiles.Count;

        if (replacementTileTotal == 0)
        {
            Debug.LogError("[TutorialDirector] No replacement world tiles were spawned.");
            return;
        }

        SetMovementLocked(false);
        RefreshReplacementTileGuide();
    }

    private void HandleReplacementTileCollected(WorldTile collectedTile)
    {
        if (!running || currentStep != TutorialStep.CollectReplacementTiles)
            return;

        if (collectedTile != null)
            collectedTile.Collected -= HandleReplacementTileCollected;

        replacementTiles.Remove(collectedTile);

        if (replacementTiles.Count > 0)
        {
            RefreshReplacementTileGuide();
            return;
        }

        if (navigationGuide != null)
            navigationGuide.ClearTarget();

        if (trainingDummy != null)
            trainingDummy.SetActive(true);

        SetStep(TutorialStep.DefeatTrainingDummy);
    }

    private void RefreshReplacementTileGuide()
    {
        replacementTiles.RemoveAll(tile => tile == null);

        if (replacementTiles.Count == 0)
        {
            if (navigationGuide != null)
                navigationGuide.ClearTarget();
            return;
        }

        Transform playerTransform =
            movementController != null ? movementController.transform : null;
        WorldTile nearestTile = null;
        float nearestDistance = float.PositiveInfinity;

        foreach (WorldTile tile in replacementTiles)
        {
            float distance = playerTransform != null
                ? (tile.transform.position - playerTransform.position).sqrMagnitude
                : 0f;

            if (nearestTile == null || distance < nearestDistance)
            {
                nearestTile = tile;
                nearestDistance = distance;
            }
        }

        if (navigationGuide != null)
            navigationGuide.SetTarget(nearestTile.transform);

        if (hud != null)
        {
            int collectedCount = replacementTileTotal - replacementTiles.Count;
            hud.SetObjectives(
                "COLLECT REPLACEMENT TILES",
                $"Collected {collectedCount} of {replacementTileTotal}. Follow the guide to the nearest tile.");
            hud.SetDialogue("Walk into the highlighted letter tile to collect it.");
        }
    }

    private void ClearReplacementTileTracking()
    {
        foreach (WorldTile tile in replacementTiles)
        {
            if (tile != null)
                tile.Collected -= HandleReplacementTileCollected;
        }

        replacementTiles.Clear();
        replacementTileTotal = 0;
    }

    private void HandleWordSubmissionRejected(string submittedWord)
    {
        if (!running || currentStep != TutorialStep.StrikePlay || hud == null)
            return;

        hud.SetObjectives(
            $"{openingWord.ToUpperInvariant()} required",
            $"“{submittedWord.ToUpperInvariant()}” was returned without attacking.");
        hud.SetDialogue($"This guided STRIKE requires {openingWord.ToUpperInvariant()}.");
    }

    private IEnumerator ActivateTrainingDummyAfterOpeningCast()
    {
        yield return null;

        if (attackController != null)
            yield return new WaitUntil(() => !attackController.IsAttackInProgress);

        if (running
            && currentStep == TutorialStep.DefeatTrainingDummy
            && trainingDummy != null)
        {
            trainingDummy.SetActive(true);
        }
    }

    public void NotifyTrainingDummyDefeated()
    {
        if (!running || currentStep != TutorialStep.DefeatTrainingDummy)
            return;

        if (attackController != null)
            attackController.SetAttackInputLocked(true);

        onDummyDefeated?.Invoke();
        SetOptionalObject(arenaEntranceIndicator, false);
        SetOptionalObject(spellbookIndicator, true);

        if (spellbookPickup != null)
        {
            spellbookPickup.Reveal();
            if (navigationGuide != null)
                navigationGuide.SetTarget(spellbookPickup.GuideTarget);
        }

        SetStep(TutorialStep.ClaimSpellbook);
    }

    private void HandleSpellbookCollected()
    {
        if (!running || currentStep != TutorialStep.ClaimSpellbook)
            return;

        if (navigationGuide != null)
            navigationGuide.ClearTarget();

        onSpellbookClaimed?.Invoke();
        SetOptionalObject(spellbookIndicator, false);
        SetOptionalObject(mobileCollectButton, false);
        CompleteTutorial();
    }

    private void HandleSpellbookRangeChanged(bool playerInRange)
    {
        if (!running
            || currentStep != TutorialStep.ClaimSpellbook)
        {
            return;
        }

        SetOptionalObject(
            mobileCollectButton,
            playerInRange && TutorialResponsiveLayout.IsMobilePresentation);

        if (hud == null)
            return;

        if (playerInRange)
        {
            string interactionInstruction = Application.isMobilePlatform
                ? "Tap COLLECT to claim the Spellbook."
                : $"Press {spellbookPickup.InteractionKeyLabel} to claim the Spellbook.";

            hud.SetObjectives("Claim the Spellbook", interactionInstruction);
            hud.SetDialogue("The Spellbook is within reach.");
            return;
        }

        hud.SetObjectives(
            "Claim the Spellbook",
            "Approach the floating book at the shrine.");
        hud.SetDialogue(
            "The trial is complete. The Spellbook will preserve the words you discover and use.");
    }

    public void TryClaimSpellbook()
    {
        if (!running
            || currentStep != TutorialStep.ClaimSpellbook
            || spellbookPickup == null)
        {
            return;
        }

        spellbookPickup.TryCollect();
    }

    public void CompleteTutorial()
    {
        if (!running)
            return;

        running = false;
        currentStep = TutorialStep.Complete;
        SetOptionalObject(mobileCollectButton, false);

        PlayerPrefs.SetInt(CompletionKey, 1);
        PlayerPrefs.Save();

        if (GameDatabaseManager.Instance != null)
            GameDatabaseManager.Instance.SetTutorialCompleted(true);

        if (hud != null)
        {
            hud.SetStep(12, 12);
            hud.SetObjectives("Tutorial complete", "The Spellbook is now yours.");
            hud.SetDialogue("Every valid word you cast can now be recorded in your Spellbook.");
            hud.SetVisible(false);
        }

        SetMovementLocked(true);
        if (attackController != null)
            attackController.SetAttackInputLocked(true);

        TutorialCompletionPanel.Show();
        onTutorialCompleted?.Invoke();
        Debug.Log("[TutorialDirector] Tutorial completed.");
    }

    private void SetStep(TutorialStep step)
    {
        currentStep = step;

        if (attackController != null)
        {
            if (step == TutorialStep.FormPlay)
                attackController.SetAttackInputLocked(true);
            else if (step == TutorialStep.StrikePlay)
                attackController.SetAttackInputLocked(false);
        }

        if (hud == null)
            return;

        switch (step)
        {
            case TutorialStep.IntroWelcome:
                hud.SetStep(1, 12);
                hud.SetObjectives("WELCOME TO SPELLSTRIKE", "In this world, words become spells.");
                hud.SetDialogue("Click or tap anywhere on this panel to continue.");
                break;

            case TutorialStep.IntroGameExplanation:
                hud.SetStep(2, 12);
                hud.SetObjectives("WORD-BASED COMBAT", "Collect letters, form valid English words, and use those words in combat.");
                hud.SetDialogue("Click or tap anywhere on this panel to continue.");
                break;

            case TutorialStep.MoveToBridge:
                hud.SetStep(3, 12);
                hud.SetObjectives("TUTORIAL: MOVE", "Follow the guide to the bridge.");
                hud.SetDialogue("");
                if (navigationGuide != null)
                    navigationGuide.SetTarget(bridgeCheckpoint);
                break;

            case TutorialStep.MoveToShrine:
                hud.SetStep(4, 12);
                hud.SetObjectives("TUTORIAL: MOVE", "Cross the bridge and follow the guide to the training shrine.");
                hud.SetDialogue("");
                if (navigationGuide != null)
                    navigationGuide.SetTarget(shrineCheckpoint);
                break;

            case TutorialStep.TileIntroduction:
                hud.SetStep(5, 12);
                hud.ShowTileLessonImage(true);
                hud.SetObjectives(
                    "LETTER TILES",
                    "Letter tiles are used to form the words that power your spells.");
                hud.SetDialogue("Click or tap anywhere on this panel to continue.");
                break;

            case TutorialStep.TileSelectionExplanation:
                hud.SetStep(6, 12);
                hud.ShowTileLessonImage(false);
                hud.SetObjectives(
                    "FORMING WORDS",
                    "Select letter tiles to place them in the Word Bar. Arrange the instructed letters in the correct order.");
                hud.SetDialogue("Click or tap anywhere on this panel to begin.");
                break;

            case TutorialStep.FormPlay:
                hud.SetStep(7, 12);
                hud.ShowTileLessonImage(false);
                hud.SetObjectives($"Form {openingWord.ToUpperInvariant()} from the Tile Pool", "Select the letters in the correct order, then press STRIKE.");
                hud.SetDialogue("Words are your spells. Begin by forming a simple command.");
                break;

            case TutorialStep.StrikePlay:
                hud.SetStep(8, 12);
                hud.ShowTileLessonImage(false);
                hud.SetObjectives(
                    $"{openingWord.ToUpperInvariant()} formed",
                    "Press STRIKE to cast your word.");
                hud.SetDialogue("Your word is valid and ready to become a spell.");
                if (attackController != null && attackController.attackButton != null)
                {
                    hud.PointAt(
                        attackController.attackButton.transform as RectTransform);
                }
                break;

            case TutorialStep.CollectReplacementTiles:
                hud.SetStep(9, 12);
                hud.ShowTileLessonImage(false);
                hud.PointAt(null);
                hud.SetObjectives(
                    "COLLECT REPLACEMENT TILES",
                    "PLAY consumed four tiles. Wait for their replacement letters to appear.");
                hud.SetDialogue("The guide will point to the nearest replacement tile.");
                break;

            case TutorialStep.DefeatTrainingDummy:
                SetMovementLocked(false);
                if (attackController != null)
                    attackController.SetAttackInputLocked(false);
                hud.SetStep(10, 12);
                hud.SetObjectives("Enter the training shrine and defeat the dummy", "Collect replacement tiles when needed, form a valid word, then press STRIKE.");
                hud.SetDialogue("PLAY was cast normally. Its four used tiles have entered the existing replacement-tile flow.");
                break;

            case TutorialStep.ClaimSpellbook:
                hud.SetStep(11, 12);
                hud.SetObjectives("Claim the Spellbook", "Approach the floating book at the shrine.");
                hud.SetDialogue("The trial is complete. The Spellbook will preserve the words you discover and use.");
                break;
        }
    }

    private static void SetOptionalObject(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }
}
