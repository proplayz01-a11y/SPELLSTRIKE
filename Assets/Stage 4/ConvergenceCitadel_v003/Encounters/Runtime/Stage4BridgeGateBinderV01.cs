using UnityEngine;

/// <summary>Stage 4 bridge locks read the canonical StageProgressionManager.</summary>
public sealed class Stage4BridgeGateBinderV01 : MonoBehaviour
{
    [SerializeField] private StageProgressionManager progressionManager;
    [SerializeField] private GameObject bridgeGate02;
    [SerializeField] private GameObject bridgeGate03;
    [SerializeField] private GameObject bridgeGate04;

    public void Configure(StageProgressionManager progression, GameObject gate02, GameObject gate03, GameObject gate04)
    {
        progressionManager = progression;
        bridgeGate02 = gate02;
        bridgeGate03 = gate03;
        bridgeGate04 = gate04;
        Sync();
    }

    private void Awake() => Sync();
    private void Update() => Sync();

    private void Sync()
    {
        if (progressionManager == null)
            return;
        Toggle(bridgeGate02, !progressionManager.IsNodeComplete(1));
        Toggle(bridgeGate03, !progressionManager.IsNodeComplete(2));
        Toggle(bridgeGate04, !progressionManager.IsNodeComplete(3));
    }

    private static void Toggle(GameObject gate, bool active)
    {
        if (gate != null && gate.activeSelf != active)
            gate.SetActive(active);
    }
}
