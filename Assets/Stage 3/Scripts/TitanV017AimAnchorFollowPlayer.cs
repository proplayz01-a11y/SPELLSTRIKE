using UnityEngine;

/// <summary>
/// Updates the non-animated aim REFERENCE only. V017 samples its position once
/// on each authored windup and locks the right-foot target for descent; this
/// script never moves either IK target or an already committed landing.
/// </summary>
[DefaultExecutionOrder(-400)]
[DisallowMultipleComponent]
public sealed class TitanV017AimAnchorFollowPlayer : MonoBehaviour
{
    [SerializeField] private Transform aimAnchor;
    [SerializeField] private PlayerHealth player;
    private float nextLookup;

    private void Update()
    {
        if (!aimAnchor || transform.childCount == 0 ||
            aimAnchor.IsChildOf(transform.GetChild(0)))
            return; // The anchor must not sit beneath the animated Titan.
        if (!player && Time.time >= nextLookup)
        {
            player = FindFirstObjectByType<PlayerHealth>();
            nextLookup = Time.time + 0.5f;
        }
        if (player && player.currentHealth > 0)
            aimAnchor.position = player.transform.position;
    }
}
