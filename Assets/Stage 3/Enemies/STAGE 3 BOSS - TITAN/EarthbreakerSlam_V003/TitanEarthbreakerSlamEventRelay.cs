using UnityEngine;

/// <summary>
/// Review-only sink for the independently approved, two-handed Earthbreaker
/// Slam V003 clip. Its impact AnimationEvent is not a gameplay damage event.
/// Keep this on the V012 review Titan only until a separate AoE contract is
/// tested and approved.
/// </summary>
[DisallowMultipleComponent]
public sealed class TitanEarthbreakerSlamEventRelay : MonoBehaviour
{
    [SerializeField] private int impactEventCount;
    private TitanEarthbreakerController controller;

    public int ImpactEventCount => impactEventCount;

    public void OnEarthbreakerSlamImpact()
    {
        impactEventCount++;
        if (!controller) controller = GetComponent<TitanEarthbreakerController>();
        controller?.ResolveEarthbreakerSlamImpact();
        Debug.Log("[Titan] Earthbreaker Slam V003 frame 71 received. " +
                  "V013 effect gating belongs exclusively to TitanEarthbreakerController.", this);
    }
}
