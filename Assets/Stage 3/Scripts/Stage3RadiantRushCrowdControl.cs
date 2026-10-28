using UnityEngine;

[DisallowMultipleComponent]
public sealed class Stage3RadiantRushCrowdControl : MonoBehaviour
{
    private MovementofPlayer movement;
    private CharacterController controller;
    private Vector3 knockbackVelocity;
    private float stunRemaining;
    private bool originalDisableMovement;
    private bool active;

    public void Begin(
        MovementofPlayer targetMovement,
        Vector3 direction,
        float force,
        float duration)
    {
        if (targetMovement == null)
            return;

        force = PassiveCrowdControlModifier.ModifyKnockback(force);
        duration = PassiveCrowdControlModifier.ModifyStun(duration);

        movement = targetMovement;
        controller = movement.GetComponent<CharacterController>();

        if (!active)
            originalDisableMovement = movement.disableMovement;

        Vector3 flatDirection = direction;
        flatDirection.y = 0f;
        if (flatDirection.sqrMagnitude <= 0.001f)
            flatDirection = movement.transform.forward;

        knockbackVelocity = flatDirection.normalized * Mathf.Max(0f, force);
        stunRemaining = Mathf.Max(stunRemaining, Mathf.Max(0.01f, duration));
        active = true;
        movement.disableMovement = true;
    }

    private void Update()
    {
        if (!active)
            return;

        if (controller != null &&
            controller.enabled &&
            knockbackVelocity.sqrMagnitude > 0.01f)
        {
            controller.Move(knockbackVelocity * Time.deltaTime);
        }

        knockbackVelocity = Vector3.Lerp(
            knockbackVelocity,
            Vector3.zero,
            10f * Time.deltaTime);

        stunRemaining -= Time.deltaTime;
        if (stunRemaining > 0f)
            return;

        active = false;
        if (movement != null)
            movement.disableMovement = originalDisableMovement;

        Destroy(this);
    }

    private void OnDisable()
    {
        if (active && movement != null)
            movement.disableMovement = originalDisableMovement;
    }
}
