using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class StageNodeTrigger : MonoBehaviour
{
    [Header("Progression")]
    [SerializeField] private StageProgressionManager progressionManager;
    [SerializeField, Min(1)] private int nodeIndex = 1;
    [SerializeField] private bool triggerOnce = true;

    [Header("Player Detection")]
    [SerializeField] private string playerTag = "Player";

    [Header("Encounter")]
    [SerializeField] private GameObject enemyRoot;
    [SerializeField] private MonoBehaviour battleReceiver;
    [SerializeField] private string startBattleMessage = "StartBattle";
    [SerializeField] private bool sendStartBattleMessage = true;

    [Header("Optional Pre-Combat Vocabulary Gate")]
    [SerializeField] private bool requireVocabularyBeforeCombat;
    [SerializeField] private VocabularyEncounterFlowController vocabularyEncounterFlow;
    [SerializeField] private bool startCombatIfVocabularyFlowFails = true;

    [Header("Stage 3 Vocabulary Tile Feed")]
    [SerializeField] private bool configureStage3VocabularyTiles;
    [SerializeField] private string stage3VocabularyMainWord;
    [SerializeField] private string stage3VocabularyRemainderWord;

    [Header("Tile Spawn Area")]
    [SerializeField] private bool configureTileSpawner = true;
    [SerializeField] private Transform arenaCenter;
    [SerializeField] private float arenaRadius = 10f;
    [SerializeField] private Transform shrineExclusionCenter;
    [SerializeField] private float shrineExclusionRadius;

    [Header("Arena Lock")]
    [SerializeField] private GameObject arenaBoundary;

    [Header("Messages")]
    [SerializeField] private bool debugLogs = true;
    [SerializeField] private string lockedMessage = "[StageNodeTrigger] Node is locked.";
    [SerializeField] private string combatStartedMessage = "[StageNodeTrigger] Combat started.";
    [SerializeField] private string vocabularyStartedMessage = "[StageNodeTrigger] Pre-combat vocabulary flow started.";
    [SerializeField] private string vocabularyCompletedMessage = "[StageNodeTrigger] Vocabulary flow completed. Starting combat.";
    [SerializeField] private string vocabularyFailedMessage = "[StageNodeTrigger] Vocabulary flow failed. Using the configured fallback.";

    [Header("Events")]
    [SerializeField] private UnityEvent onCombatStarted;

    private bool hasTriggered;
    private bool encounterStarted;
    private bool vocabularyGateRunning;
    private bool vocabularyFlowSubscribed;
    private float nextNode4RiddleRetryTime;

    private void Reset()
    {
        Collider triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;
    }

    private void OnDisable()
    {
        RemoveVocabularyFlowSubscriptions();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryStartEncounter(other, true);
    }

    private void OnTriggerStay(Collider other)
    {
        // A player can already be standing inside the next combat zone when
        // the preceding node becomes complete. Re-evaluate the progression
        // gate here so that case does not require exiting and re-entering.
        TryStartEncounter(other, false);
    }

    private void TryStartEncounter(Collider other, bool logLockedState)
    {
        if (!IsPlayerCollider(other)) return;
        if (triggerOnce && hasTriggered) return;
        if (nodeIndex == 4 && Time.time < nextNode4RiddleRetryTime) return;

        if (progressionManager != null && !progressionManager.CanStartNode(nodeIndex))
        {
            if (logLockedState)
            {
                Log(lockedMessage);
            }

            return;
        }

        ConfigureStage3VocabularyTiles();
        hasTriggered = true;
        EnemyEncounterHUD.ShowFor(enemyRoot);
        if (TryBeginVocabularyGate())
            return;

        StartEncounter();
    }

    private void ConfigureStage3VocabularyTiles()
    {
        if (!configureStage3VocabularyTiles || string.IsNullOrWhiteSpace(stage3VocabularyMainWord))
            return;

        TileManager manager = TileManager.Instance != null
            ? TileManager.Instance
            : FindFirstObjectByType<TileManager>();
        if (manager == null || manager.CurrentStage != 3)
            return;

        manager.SetStage3VocabularyWords(
            stage3VocabularyMainWord,
            stage3VocabularyRemainderWord,
            true);
    }

    private bool TryBeginVocabularyGate()
    {
        if (!requireVocabularyBeforeCombat)
            return false;

        if (vocabularyEncounterFlow == null)
        {
            Debug.LogError(
                "[StageNodeTrigger] Vocabulary is required before combat, but no "
                + "VocabularyEncounterFlowController is assigned. Starting the legacy combat path.",
                this);
            return false;
        }

        EnsureVocabularyFlowSubscriptions();
        vocabularyGateRunning = true;
        PrepareEncounterArea();
        Log(vocabularyStartedMessage);

        bool started = vocabularyEncounterFlow.BeginEncounter();
        if (!started && vocabularyGateRunning)
        {
            vocabularyGateRunning = false;
            HandleVocabularyFailureFallback();
        }

        // The vocabulary path owns the trigger once requested. BeginEncounter
        // emits EncounterFailed on a synchronous setup failure, so returning
        // true also prevents the caller from starting combat a second time.
        return true;
    }

    private void EnsureVocabularyFlowSubscriptions()
    {
        if (vocabularyFlowSubscribed || vocabularyEncounterFlow == null)
            return;

        vocabularyEncounterFlow.EncounterCompleted += HandleVocabularyCompleted;
        vocabularyEncounterFlow.EncounterFailed += HandleVocabularyFailed;
        vocabularyFlowSubscribed = true;
    }

    private void RemoveVocabularyFlowSubscriptions()
    {
        if (!vocabularyFlowSubscribed || vocabularyEncounterFlow == null)
            return;

        vocabularyEncounterFlow.EncounterCompleted -= HandleVocabularyCompleted;
        vocabularyEncounterFlow.EncounterFailed -= HandleVocabularyFailed;
        vocabularyFlowSubscribed = false;
    }

    private void HandleVocabularyCompleted(VocabularyEncounterFlowController source)
    {
        if (!vocabularyGateRunning || source != vocabularyEncounterFlow)
            return;

        vocabularyGateRunning = false;
        Log(vocabularyCompletedMessage);
        StartEncounter();
    }

    private void HandleVocabularyFailed(VocabularyEncounterFlowController source)
    {
        if (!vocabularyGateRunning || source != vocabularyEncounterFlow)
            return;

        vocabularyGateRunning = false;
        HandleVocabularyFailureFallback();
    }

    private void HandleVocabularyFailureFallback()
    {
        Log(vocabularyFailedMessage);

        if (startCombatIfVocabularyFlowFails)
        {
            StartEncounter();
            return;
        }

        // Allow the player to leave and re-enter the trigger after a failed
        // setup when the designer explicitly disables the combat fallback.
        if (arenaBoundary != null)
            arenaBoundary.SetActive(false);
        hasTriggered = false;
    }

    private bool IsPlayerCollider(Collider other)
    {
        if (other == null) return false;
        if (other.CompareTag(playerTag)) return true;

        Rigidbody attachedBody = other.attachedRigidbody;
        if (attachedBody != null && attachedBody.CompareTag(playerTag))
        {
            return true;
        }

        Transform root = other.transform.root;
        return root != null && root.CompareTag(playerTag);
    }

    public void StartEncounter()
    {
        if (encounterStarted)
            return;

        encounterStarted = true;

        PrepareEncounterArea();
        EnableEnemy();
        EnemyEncounterHUD.ShowFor(enemyRoot);
        // Node 4's riddle can refuse startup if TileManager, HUD, movement or
        // the challenge pool is unavailable. Do not permanently consume the
        // trigger in that case: OnTriggerStay can retry after dependencies
        // recover, without replaying boss awakening or changing Nodes 1-3.
        if (nodeIndex == 4 && sendStartBattleMessage &&
            startBattleMessage == "StartBattle" &&
            battleReceiver is Stage3RiddleController riddle)
        {
            if (!riddle.BeginRiddleTrial())
            {
                encounterStarted = false;
                hasTriggered = false;
                nextNode4RiddleRetryTime = Time.time + 1f;
                if (arenaBoundary != null) arenaBoundary.SetActive(false);
                Log("[Stage3 Node4] Riddle startup declined. Trigger remains retryable.");
                return;
            }
        }
        else
        {
            SendStartBattleMessage();
        }

        Log(combatStartedMessage);
        onCombatStarted?.Invoke();
    }

    // Stage 3 Node 4 alone may rearm after a late technical riddle failure.
    // Never restart an awakened boss, bypass a riddle, or consume a damaged
    // Normal Combat pool snapshot. The controller owns exact restoration.
    public bool TryRearmNode4AfterTechnicalFailure(Stage3RiddleController source)
    {
        if (nodeIndex != 4 || !triggerOnce || !hasTriggered || !encounterStarted
            || source == null || battleReceiver != source
            || progressionManager == null || !progressionManager.CanStartNode(nodeIndex)
            || !source.CanRetryAfterTechnicalFailure)
            return false;

        encounterStarted = false;
        hasTriggered = false;
        nextNode4RiddleRetryTime = Time.time + 1f;
        if (arenaBoundary != null)
            arenaBoundary.SetActive(false);
        Log("[Stage3 Node4] Exact pool recovered and player/Titan guards passed. All four riddles must be answered again before Awakening.");
        return true;
    }

    private void PrepareEncounterArea()
    {
        if (arenaBoundary != null)
            arenaBoundary.SetActive(true);

        ConfigureTileSpawner();
    }

    private void ConfigureTileSpawner()
    {
        if (!configureTileSpawner) return;
        if (TileSpawner.Instance == null) return;
        if (arenaCenter == null) return;

        TileSpawner.Instance.ConfigureArenaBounds(
            arenaCenter,
            arenaRadius,
            shrineExclusionCenter,
            shrineExclusionRadius
        );
    }

    private void EnableEnemy()
    {
        if (enemyRoot == null) return;
        enemyRoot.SetActive(true);
    }

    private void SendStartBattleMessage()
    {
        if (!sendStartBattleMessage || string.IsNullOrWhiteSpace(startBattleMessage)) return;

        if (battleReceiver != null)
        {
            battleReceiver.SendMessage(startBattleMessage, SendMessageOptions.DontRequireReceiver);
            return;
        }

        if (enemyRoot != null)
        {
            enemyRoot.SendMessage(startBattleMessage, SendMessageOptions.DontRequireReceiver);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (arenaCenter != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(arenaCenter.position, arenaRadius);
        }

        if (shrineExclusionCenter != null && shrineExclusionRadius > 0f)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(shrineExclusionCenter.position, shrineExclusionRadius);
        }
    }

    private void Log(string message)
    {
        if (!debugLogs || string.IsNullOrWhiteSpace(message)) return;
        Debug.Log(message);
    }
}
