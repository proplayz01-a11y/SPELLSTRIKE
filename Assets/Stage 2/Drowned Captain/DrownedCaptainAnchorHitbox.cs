using UnityEngine;

/// <summary>
/// Shared hitbox script for the Drowned Captain's anchor attacks.
/// Attached to the AnchorCollider child GameObject with a trigger BoxCollider.
/// Forwards collision events to the DrownedCaptainController for attack-specific resolution.
/// Used by both Anchor Spin Sweep and Anchor Pursuit Strike.
/// </summary>
public class DrownedCaptainAnchorHitbox : MonoBehaviour
{
    [SerializeField] private DrownedCaptainController controller;

    private void OnTriggerEnter(Collider other)
    {
        if (controller == null)
        {
            Debug.LogWarning("[DrownedCaptainAnchorHitbox] DrownedCaptainController reference is not assigned.", this);
            return;
        }

        // Only process colliders belonging to the player hierarchy.
        if (!other.CompareTag("Player") && !other.transform.root.CompareTag("Player"))
            return;

        controller.TryResolveAnchorWeaponHit(other);
    }
}
