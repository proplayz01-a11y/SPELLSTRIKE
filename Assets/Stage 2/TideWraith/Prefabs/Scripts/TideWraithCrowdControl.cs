using UnityEngine;

/// <summary>
/// Bridges Tidal Wave knockback/stun to either player movement implementation
/// currently present in SPELLSTRIKE.
/// </summary>
public class TideWraithCrowdControl : MonoBehaviour
{
    private MovementofPlayer legacyMovement;
    private CharacterController characterController;
    private Vector3 knockbackVelocity;
    private float stunRemaining;
    private bool originalDisableMovement;
    private bool active;

    public static void Apply(Transform player, Vector3 direction, float knockbackForce, float stunDuration)
    {
        if (player == null)
            return;

        PlayerMovement currentMovement = player.GetComponentInChildren<PlayerMovement>();
        if (currentMovement == null)
            currentMovement = player.GetComponentInParent<PlayerMovement>();

        if (currentMovement != null)
        {
            currentMovement.ApplyKnockback(direction, knockbackForce);
            currentMovement.ApplyStun(stunDuration);
            return;
        }

        MovementofPlayer movement = player.GetComponentInChildren<MovementofPlayer>();
        if (movement == null)
            movement = player.GetComponentInParent<MovementofPlayer>();
        if (movement == null)
            return;

        TideWraithCrowdControl effect = movement.GetComponent<TideWraithCrowdControl>();
        if (effect == null)
            effect = movement.gameObject.AddComponent<TideWraithCrowdControl>();
        effect.Begin(movement, direction, knockbackForce, stunDuration);
    }

    private void Begin(MovementofPlayer movement, Vector3 direction, float force, float duration)
    {
        force = PassiveCrowdControlModifier.ModifyKnockback(force);
        duration = PassiveCrowdControlModifier.ModifyStun(duration);
        legacyMovement = movement;
        characterController = movement.GetComponent<CharacterController>();

        if (!active)
            originalDisableMovement = movement.disableMovement;

        Vector3 flatDirection = direction;
        flatDirection.y = 0f;
        if (flatDirection.sqrMagnitude <= 0.001f)
            flatDirection = movement.transform.forward;

        knockbackVelocity = flatDirection.normalized * Mathf.Max(0f, force);
        stunRemaining = Mathf.Max(stunRemaining, Mathf.Max(0.01f, duration));
        active = true;
        legacyMovement.disableMovement = true;
    }

    private void Update()
    {
        if (!active)
            return;

        if (characterController != null && characterController.enabled && knockbackVelocity.sqrMagnitude > 0.01f)
            characterController.Move(knockbackVelocity * Time.deltaTime);

        knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, 8f * Time.deltaTime);
        stunRemaining -= Time.deltaTime;

        if (stunRemaining > 0f)
            return;

        active = false;
        if (legacyMovement != null)
            legacyMovement.disableMovement = originalDisableMovement;
        Destroy(this);
    }

    private void OnDisable()
    {
        if (active && legacyMovement != null)
            legacyMovement.disableMovement = originalDisableMovement;
    }
}
