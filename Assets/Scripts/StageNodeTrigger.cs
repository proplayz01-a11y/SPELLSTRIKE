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

    [Header("Events")]
    [SerializeField] private UnityEvent onCombatStarted;

    private bool hasTriggered;

    private void Reset()
    {
        Collider triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (triggerOnce && hasTriggered) return;

        if (progressionManager != null && !progressionManager.CanStartNode(nodeIndex))
        {
            Log(lockedMessage);
            return;
        }

        hasTriggered = true;
        StartEncounter();
    }

    public void StartEncounter()
    {
        if (arenaBoundary != null)
        {
            arenaBoundary.SetActive(true);
        }

        ConfigureTileSpawner();
        EnableEnemy();
        SendStartBattleMessage();

        Log(combatStartedMessage);
        onCombatStarted?.Invoke();
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
