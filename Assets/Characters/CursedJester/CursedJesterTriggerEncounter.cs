using UnityEngine;

public class CursedJesterTriggerEncounter : MonoBehaviour
{
    [Header("Progression")]
    public Stage1ProgressionManager progressionManager;

    [Header("Encounter References")]
    public CursedJesterController cursedJester;
    public GameObject arenaBoundary;
    [SerializeField] private VocabularyEncounterFlowController vocabularyEncounterFlow;

    [Header("NULLIFY Pre-Combat Gate")]
    [SerializeField] private bool requireNullifyBeforeCombat = true;

    [Header("Arena Bounds")]
    public Transform arenaCenter;
    public float arenaRadius = 10f;

    public Transform shrineCenter;
    public float shrineExclusionRadius = 3f;

    private bool triggered = false;
    private bool battleStarted = false;

    private void OnEnable()
    {
        if (vocabularyEncounterFlow != null)
        {
            vocabularyEncounterFlow.EncounterCompleted += HandleVocabularyGateCompleted;
            vocabularyEncounterFlow.EncounterFailed += HandleVocabularyGateFailed;
        }
    }

    private void OnDisable()
    {
        if (vocabularyEncounterFlow != null)
        {
            vocabularyEncounterFlow.EncounterCompleted -= HandleVocabularyGateCompleted;
            vocabularyEncounterFlow.EncounterFailed -= HandleVocabularyGateFailed;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;
        if (progressionManager != null && !progressionManager.CanStartNode3())
        {
            Debug.Log("[CursedJesterTriggerEncounter] Node 3 is locked. Complete Node 2 first.");
            return;
        }

        triggered = true;

        if (arenaBoundary != null)
            arenaBoundary.SetActive(true);

        if (TileSpawner.Instance != null)
        {
            TileSpawner.Instance.ConfigureArenaBounds(
                arenaCenter,
                arenaRadius,
                shrineCenter,
                shrineExclusionRadius
            );
        }
        else
        {
            Debug.LogWarning("[CursedJesterTriggerEncounter] TileSpawner.Instance is missing.");
        }

        if (cursedJester != null)
        {
            cursedJester.gameObject.SetActive(true);
            EnemyEncounterHUD.ShowFor(cursedJester);

            if (requireNullifyBeforeCombat)
            {
                if (vocabularyEncounterFlow != null
                    && vocabularyEncounterFlow.BeginEncounter())
                {
                    Debug.Log(
                        "[CursedJesterTriggerEncounter] Node 3 Teach started. "
                        + "Retrieve NULLIFY before Cursed Jester combat begins.",
                        this);
                    return;
                }

                Debug.LogError(
                    "[CursedJesterTriggerEncounter] NULLIFY is required, but its "
                    + "Vocabulary Encounter Flow is missing or failed to start. "
                    + "Starting the legacy battle to avoid a progression soft lock.",
                    this);
            }

            StartJesterBattle();
        }
        else
        {
            Debug.LogWarning("[CursedJesterTriggerEncounter] Cursed Jester reference is missing.");
        }
    }

    private void HandleVocabularyGateCompleted(
        VocabularyEncounterFlowController completedFlow)
    {
        if (!triggered || completedFlow != vocabularyEncounterFlow)
            return;

        Debug.Log(
            "[CursedJesterTriggerEncounter] NULLIFY retrieved and the exact "
            + "Normal Combat pool restored. Disturbing Cursed Jester before combat.",
            this);
        StartJesterBattle(playBarrierBreakReaction: true);
    }

    private void HandleVocabularyGateFailed(
        VocabularyEncounterFlowController failedFlow)
    {
        if (!triggered || failedFlow != vocabularyEncounterFlow)
            return;

        Debug.LogError(
            "[CursedJesterTriggerEncounter] NULLIFY flow failed. Starting the "
            + "legacy battle to prevent a Node 3 soft lock.",
            this);
        StartJesterBattle();
    }

    private void StartJesterBattle(bool playBarrierBreakReaction = false)
    {
        if (battleStarted || cursedJester == null)
            return;

        battleStarted = true;
        cursedJester.gameObject.SetActive(true);
        EnemyEncounterHUD.ShowFor(cursedJester);
        cursedJester.StartBattle();

        if (playBarrierBreakReaction)
            cursedJester.TriggerBeingHit();

        Debug.Log(
            playBarrierBreakReaction
                ? "[CursedJesterTriggerEncounter] Cursed Jester battle started with the zero-damage barrier-break BeingHit reaction."
                : "[CursedJesterTriggerEncounter] Cursed Jester battle started.",
            this);
    }

    private void OnDrawGizmos()
    {
        if (arenaCenter != null)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
            Gizmos.DrawSphere(arenaCenter.position, arenaRadius);
            Gizmos.color = new Color(0f, 1f, 0f, 1f);
            Gizmos.DrawWireSphere(arenaCenter.position, arenaRadius);
        }

        if (shrineCenter != null)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            Gizmos.DrawSphere(shrineCenter.position, shrineExclusionRadius);
            Gizmos.color = new Color(1f, 0f, 0f, 1f);
            Gizmos.DrawWireSphere(shrineCenter.position, shrineExclusionRadius);
        }
    }
}
