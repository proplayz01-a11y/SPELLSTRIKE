using UnityEngine;

public class HollowKnightTriggerEncounter : MonoBehaviour
{
    [Header("Progression")]
    public Stage1ProgressionManager progressionManager;

    [Header("Hollow Knight")]
    public HollowKnightController hollowKnight;

    [Header("Tile Spawn Settings")]
    public Transform bossArenaCenter;
    public float bossArenaRadius = 12f;

    public Transform bossShrineCenter;
    public float bossShrineExclusionRadius = 3f;

    [Header("Arena Lock")]
    public GameObject bossArenaBoundary;

    [Header("Node 4 - LIBERATE Vocabulary Gate")]
    [Tooltip(
        "Optional. When assigned, Hollow Knight is enabled but stays dormant " +
        "until this encounter reports EncounterCompleted. Leave empty to keep " +
        "the original immediate StartBattle() behavior (Nodes without a gate).")]
    public VocabularyEncounterFlowController vocabularyEncounterFlow;

    private bool encounterStarted = false;
    private bool bossCombatStarted = false;
    private bool subscribedToVocabularyFlow = false;

    private void OnTriggerEnter(Collider other)
    {
        if (encounterStarted) return;
        if (!other.CompareTag("Player")) return;
        if (progressionManager != null && !progressionManager.CanStartBoss())
        {
            Debug.Log("[HollowKnightTriggerEncounter] Boss is locked. Complete Node 3 first.");
            return;
        }

        StartBossEncounter();
    }

    public void StartBossEncounter()
    {
        if (encounterStarted) return;
        encounterStarted = true;

        Debug.Log("[HollowKnightTriggerEncounter] Boss encounter started.");

        ConfigureTileSpawner();
        EnableArenaBoundary();
        EnableDormantBossAndBeginGate();
    }

    private void ConfigureTileSpawner()
    {
        if (TileSpawner.Instance == null)
        {
            Debug.LogWarning("[HollowKnightTriggerEncounter] TileSpawner.Instance is missing.");
            return;
        }

        if (bossArenaCenter == null)
        {
            Debug.LogWarning("[HollowKnightTriggerEncounter] Boss arena center missing.");
            return;
        }

        if (bossShrineCenter == null)
        {
            Debug.LogWarning("[HollowKnightTriggerEncounter] Boss shrine center missing.");
            return;
        }

        TileSpawner.Instance.ConfigureArenaBounds(
            bossArenaCenter,
            bossArenaRadius,
            bossShrineCenter,
            bossShrineExclusionRadius
        );

        Debug.Log("[HollowKnightTriggerEncounter] TileSpawner configured for Hollow Knight arena.");
    }

    private void EnableArenaBoundary()
    {
        if (bossArenaBoundary == null)
        {
            Debug.LogWarning("[HollowKnightTriggerEncounter] Boss arena boundary missing.");
            return;
        }

        bossArenaBoundary.SetActive(true);
        Debug.Log("[HollowKnightTriggerEncounter] Boss arena boundary enabled.");
    }

    private void EnableDormantBossAndBeginGate()
    {
        if (hollowKnight == null)
        {
            Debug.LogWarning("[HollowKnightTriggerEncounter] Hollow Knight reference missing.");
            return;
        }

        if (!hollowKnight.gameObject.activeSelf)
        {
            hollowKnight.gameObject.SetActive(true);
            Debug.Log("[HollowKnightTriggerEncounter] Hollow Knight enabled.");
        }

        EnemyEncounterHUD.ShowFor(hollowKnight);

        if (vocabularyEncounterFlow == null)
        {
            // No Node 4 gate wired up. Preserve the original behavior exactly
            // so Nodes/bosses without a vocabulary gate are unaffected.
            StartBossCombat();
            return;
        }

        SubscribeToVocabularyFlow();

        Debug.Log(
            "[HollowKnightTriggerEncounter] Hollow Knight enabled and dormant. " +
            "Starting the LIBERATE vocabulary encounter before combat.");

        bool started = vocabularyEncounterFlow.BeginEncounter();
        if (!started)
        {
            Debug.LogError(
                "[HollowKnightTriggerEncounter] LIBERATE encounter failed to start " +
                "(see VocabularyEncounterFlowController error above). Falling back to " +
                "immediate boss combat so the player is not trapped in a closed arena.");
            StartBossCombat();
        }
    }

    private void SubscribeToVocabularyFlow()
    {
        if (subscribedToVocabularyFlow || vocabularyEncounterFlow == null)
            return;

        vocabularyEncounterFlow.EncounterCompleted += HandleLiberateCompleted;
        vocabularyEncounterFlow.EncounterFailed += HandleLiberateFailed;
        subscribedToVocabularyFlow = true;
    }

    private void UnsubscribeFromVocabularyFlow()
    {
        if (!subscribedToVocabularyFlow || vocabularyEncounterFlow == null)
            return;

        vocabularyEncounterFlow.EncounterCompleted -= HandleLiberateCompleted;
        vocabularyEncounterFlow.EncounterFailed -= HandleLiberateFailed;
        subscribedToVocabularyFlow = false;
    }

    private void HandleLiberateCompleted(VocabularyEncounterFlowController flow)
    {
        if (flow != vocabularyEncounterFlow) return;

        Debug.Log(
            "[HollowKnightTriggerEncounter] LIBERATE encounter completed. " +
            "Starting Hollow Knight combat.");
        StartBossCombat();
    }

    private void HandleLiberateFailed(VocabularyEncounterFlowController flow)
    {
        if (flow != vocabularyEncounterFlow) return;

        Debug.LogError(
            "[HollowKnightTriggerEncounter] LIBERATE encounter reported EncounterFailed. " +
            "Falling back to immediate boss combat so the player is not trapped in a " +
            "closed arena. Investigate the VocabularyEncounterFlowController error log " +
            "above (missing references, mismatched Teach/Barrier word, or a pool-capture " +
            "failure).");
        StartBossCombat();
    }

    private void StartBossCombat()
    {
        if (bossCombatStarted) return;
        bossCombatStarted = true;

        UnsubscribeFromVocabularyFlow();

        if (hollowKnight == null)
        {
            Debug.LogWarning("[HollowKnightTriggerEncounter] Hollow Knight reference missing. Cannot start battle.");
            return;
        }

        EnemyEncounterHUD.ShowFor(hollowKnight);
        hollowKnight.StartBattle();
    }

    private void OnDestroy()
    {
        UnsubscribeFromVocabularyFlow();
    }

    private void OnDrawGizmosSelected()
    {
        if (bossArenaCenter != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(bossArenaCenter.position, bossArenaRadius);
        }

        if (bossShrineCenter != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(bossShrineCenter.position, bossShrineExclusionRadius);
        }
    }
}
