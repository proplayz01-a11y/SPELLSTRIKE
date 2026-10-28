using UnityEngine;
using UnityEngine.Events;

public class Node1Controller : MonoBehaviour
{
    [Header("References")]
    public BrambleSpriteController brambleSprite; 
    public FragmentPickup fragmentPickup;
    public GameObject fragmentVisual;
    public Stage1ProgressionManager progressionManager;
    public GoalsPanelUI goalsPanelUI;
    public PlayerHealth playerHealth;

    [Header("MITIGATE Barrier-Gated Encounter")]
    [SerializeField] private VocabularyBarrier vocabularyBarrier;
    [SerializeField] private VocabularyEncounterFlowController vocabularyEncounterFlow;
    [SerializeField] private bool requireBarrierBreakBeforeCombat = true;
    [SerializeField] private bool keepBrambleVisibleWhileDormant = true;
    [SerializeField] private bool hideDormantBrambleFromTargeting = true;
    [SerializeField] private bool blockNonTargetDamageWhileBarrierActive = true;
    [SerializeField] private GameObject fragmentFeedingVisual;
    [SerializeField] private UnityEvent onBarrierChallengeStarted;
    [SerializeField] private UnityEvent onBrambleDisturbed;

    private bool encounterStarted = false;
    private bool battleStarted = false;
    private bool barrierBreakHandled = false;
    private bool enemyDefeated = false;
    private bool nodeCompleted = false;
    private string brambleCombatTag;

    private void Awake()
    {
        ResolveBarrierReference();
        ResolveVocabularyFlowReference();

        if (brambleSprite != null)
            brambleCombatTag = brambleSprite.gameObject.tag;
    }

    private void Start()
    {
        if (goalsPanelUI == null)
            goalsPanelUI = FindFirstObjectByType<GoalsPanelUI>();
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (brambleSprite != null)
        {
            if (requireBarrierBreakBeforeCombat && keepBrambleVisibleWhileDormant)
                PrepareDormantBramble();
            else
                brambleSprite.gameObject.SetActive(false);
        }

        if (fragmentPickup != null)
            fragmentPickup.SetLocked(true);
    }

    private void OnEnable()
    {
        if (brambleSprite != null)
            brambleSprite.OnDeathStarted += HandleBrambleDeath;

        ResolveBarrierReference();
        if (vocabularyBarrier != null)
            vocabularyBarrier.BarrierBroken += HandleBarrierBroken;

        ResolveVocabularyFlowReference();
        if (vocabularyEncounterFlow != null)
            vocabularyEncounterFlow.ChallengeStarted += HandleVocabularyChallengeStarted;
    }

    private void OnDisable()
    {
        if (brambleSprite != null)
            brambleSprite.OnDeathStarted -= HandleBrambleDeath;

        if (vocabularyBarrier != null)
            vocabularyBarrier.BarrierBroken -= HandleBarrierBroken;

        if (vocabularyEncounterFlow != null)
            vocabularyEncounterFlow.ChallengeStarted -= HandleVocabularyChallengeStarted;
    }

    private void HandleBrambleDeath(BrambleSpriteController sprite)
    {
        OnBrambleSpriteDefeated();
    }

    public void TriggerEncounter()
    {
        if (encounterStarted || nodeCompleted) return;

        encounterStarted = true;

        if (brambleSprite != null)
        {
            brambleSprite.gameObject.SetActive(true);
            RestoreBrambleCombatTag();
            EnemyEncounterHUD.ShowFor(brambleSprite);
        }

        if (requireBarrierBreakBeforeCombat)
        {
            ResolveBarrierReference();

            if (vocabularyBarrier == null)
            {
                Debug.LogError(
                    "[Node1Controller] MITIGATE barrier gate is enabled, but Bramble Sprite has no VocabularyBarrier. Starting combat to avoid a soft lock.",
                    this
                );
                StartBrambleCombat();
                return;
            }

            vocabularyBarrier.SetBlockNonTargetDamageWhileActive(blockNonTargetDamageWhileBarrierActive);

            ResolveVocabularyFlowReference();
            if (vocabularyEncounterFlow != null)
            {
                if (vocabularyEncounterFlow.BeginEncounter())
                {
                    if (fragmentFeedingVisual != null)
                        fragmentFeedingVisual.SetActive(true);

                    Debug.Log(
                        "[Node1Controller] Node 1 Teach started. The MITIGATE barrier will reveal after the panel is dismissed.",
                        this);
                    return;
                }

                Debug.LogError(
                    "[Node1Controller] The MITIGATE educational flow could not start. Starting Bramble combat to avoid a soft lock.",
                    this);
                StartBrambleCombat();
                return;
            }

            vocabularyBarrier.BeginChallenge();

            if (fragmentFeedingVisual != null)
                fragmentFeedingVisual.SetActive(true);

            onBarrierChallengeStarted?.Invoke();
            Debug.Log(
                "[Node1Controller] Bramble Sprite remains dormant. Retrieve MITIGATE to break the barrier and disturb it.",
                this
            );
            return;
        }

        StartBrambleCombat();
    }

    private void HandleBarrierBroken(VocabularyBarrier brokenBarrier)
    {
        if (!requireBarrierBreakBeforeCombat || !encounterStarted || barrierBreakHandled)
            return;

        if (vocabularyBarrier != null && brokenBarrier != vocabularyBarrier)
            return;

        barrierBreakHandled = true;

        if (fragmentFeedingVisual != null)
            fragmentFeedingVisual.SetActive(false);

        onBrambleDisturbed?.Invoke();
        StartBrambleCombat();
    }

    private void HandleVocabularyChallengeStarted(VocabularyEncounterFlowController flowController)
    {
        if (flowController != vocabularyEncounterFlow || !encounterStarted)
            return;

        onBarrierChallengeStarted?.Invoke();
        Debug.Log(
            "[Node1Controller] Teach dismissed. Temporary MITIGATE challenge and barrier are now active.",
            this);
    }

    private void StartBrambleCombat()
    {
        if (battleStarted || brambleSprite == null)
            return;

        battleStarted = true;
        RestoreBrambleCombatTag();
        brambleSprite.gameObject.SetActive(true);
        EnemyEncounterHUD.ShowFor(brambleSprite);
        brambleSprite.StartBattleDisturbed();

        Debug.Log("Node 1 combat started at full health. Bramble Sprite was disturbed from the fragment.");
    }

    private void PrepareDormantBramble()
    {
        brambleSprite.gameObject.SetActive(true);

        ResolveBarrierReference();
        if (vocabularyBarrier != null)
        {
            vocabularyBarrier.SetBlockNonTargetDamageWhileActive(blockNonTargetDamageWhileBarrierActive);
            vocabularyBarrier.HideChallenge();
        }

        if (fragmentFeedingVisual != null)
            fragmentFeedingVisual.SetActive(true);

        brambleSprite.SetDormant(true);

        if (hideDormantBrambleFromTargeting && brambleSprite.gameObject.CompareTag("Enemy"))
            brambleSprite.gameObject.tag = "Untagged";
    }

    private void RestoreBrambleCombatTag()
    {
        if (brambleSprite == null || string.IsNullOrWhiteSpace(brambleCombatTag))
            return;

        brambleSprite.gameObject.tag = brambleCombatTag;
    }

    private void ResolveBarrierReference()
    {
        if (vocabularyBarrier == null && brambleSprite != null)
            vocabularyBarrier = brambleSprite.GetComponent<VocabularyBarrier>();
    }

    private void ResolveVocabularyFlowReference()
    {
        if (vocabularyEncounterFlow == null)
            vocabularyEncounterFlow = GetComponent<VocabularyEncounterFlowController>();
    }

    public void OnBrambleSpriteDefeated()
    {
        if (enemyDefeated) return;

        enemyDefeated = true;

        if (fragmentPickup != null)
            fragmentPickup.SetLocked(false);
        if (playerHealth != null)
            playerHealth.RestoreFullHealth();

        Debug.Log("Bramble Sprite defeated. Fragment unlocked.");
    }

    public void OnFragmentCollected()
    {
        if (nodeCompleted) return;

        nodeCompleted = true;
        if (progressionManager != null)
            progressionManager.MarkNode1Complete();
        if (goalsPanelUI != null)
            goalsPanelUI.IncrementFragmentCount();

        if (fragmentVisual != null)
            fragmentVisual.SetActive(false);

        Debug.Log("Node 1 complete.");
    }
}
