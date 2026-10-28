using UnityEngine;

/// <summary>
/// Real frame-134 Hammerfist HIT aftermath on the original Van controller.
/// Lock voluntary input through the ENTIRE physical flight; keep native
/// gravity/collision, and only start the movement slow AFTER actual landing.
/// Never moves the Transform, credits an impact or modifies HP directly.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(16000)]
public sealed class TitanHammerfistCrowdControl : MonoBehaviour
{
    private enum Phase { None, AirborneLocked, Slowed }

    private MovementofPlayer movement;
    private CharacterController controller;
    private PlayerHealth health;
    private Phase phase;
    private float remaining;
    private float launchAt;
    private float lastGroundedAt = -1f;
    private float queuedSlowMultiplier;
    private float queuedSlowSeconds;
    private bool observedAirborne;
    private bool ownsMovementLock;

    public bool IsActive => phase != Phase.None;
    public bool IsStunned => phase == Phase.AirborneLocked;
    public bool IsAirborneLocked => phase == Phase.AirborneLocked;
    public bool IsSlowed => phase == Phase.Slowed;
    public float Remaining => Mathf.Max(0f, remaining);

    public bool Begin(MovementofPlayer target, PlayerHealth targetHealth,
        float launchSpeed, float stunSeconds, float slowMultiplier, float slowSeconds)
    {
        if (!target || !target.isActiveAndEnabled ||
            !targetHealth || targetHealth.currentHealth <= 0 || IsActive ||
            !float.IsFinite(launchSpeed) || launchSpeed <= 0f ||
            !float.IsFinite(stunSeconds) || stunSeconds < 0f ||
            !float.IsFinite(slowMultiplier) || slowMultiplier <= 0f ||
            !float.IsFinite(slowSeconds) || slowSeconds <= 0f)
            return false;

        CharacterController body = target.GetComponent<CharacterController>();
        if (!body || !body.enabled || !body.gameObject.activeInHierarchy)
            return false;

        float launch = PassiveCrowdControlModifier.ModifyKnockback(launchSpeed);
        float minLock = PassiveCrowdControlModifier.ModifyStun(stunSeconds);
        if (!float.IsFinite(launch) || launch <= 0f ||
            !float.IsFinite(minLock) || minLock < 0f)
            return false;

        movement = target;
        controller = body;
        health = targetHealth;
        queuedSlowMultiplier = Mathf.Clamp(slowMultiplier, 0.01f, 1f);
        queuedSlowSeconds = slowSeconds;
        observedAirborne = false;
        launchAt = Time.time;
        lastGroundedAt = -1f;
        remaining = 0f;

        movement.AcquireExternalMovementLock();
        ownsMovementLock = true;
        phase = Phase.AirborneLocked;
        // This is native MovementofPlayer gravity/collision, not a second
        // mover. Horizontal input stays disabled until real landing.
        movement.ApplyExternalVerticalLaunch(launch);
        Debug.Log("[TITAN HAMMER LAUNCH] AUTHENTIC frame134 HIT: upwardSpeed=" +
            launch.ToString("F2") + "m/s; airborne controls LOCKED until grounded; " +
            "slow=x" + queuedSlowMultiplier.ToString("F2") + " for " +
            queuedSlowSeconds.ToString("F1") + "s begins ON LANDING. " +
            "Steady Current passive already applied; no extra HP damage.", this);
        return true;
    }

    // LateUpdate samples collision AFTER MovementofPlayer.Update has applied
    // the actual vertical CharacterController.Move and gravity.
    private void LateUpdate()
    {
        if (phase == Phase.None) return;
        if (!movement || !movement.isActiveAndEnabled || !controller ||
            !controller.enabled || !controller.gameObject.activeInHierarchy ||
            !health || health.currentHealth <= 0)
        {
            EndEffect("Van unavailable, dead or controller disabled");
            return;
        }

        if (phase == Phase.AirborneLocked)
        {
            float flightTime = Time.time - launchAt;
            if (!controller.isGrounded)
            {
                observedAirborne = true;
                lastGroundedAt = -1f;
            }
            else if (observedAirborne)
            {
                if (lastGroundedAt < 0f) lastGroundedAt = Time.time;
                if (Time.time - lastGroundedAt >= 0.06f)
                {
                    BeginLandingSlow(flightTime);
                    return;
                }
            }
            else if (flightTime > 0.40f)
            {
                // Ceiling / obstructed launch: don't trap the player. This
                // is not a fabricated airborne success or hit confirmation.
                Debug.LogWarning("[TITAN HAMMER LAUNCH] Launch could not clear ground. " +
                    "Starting grounded recovery without a fake flight.", this);
                BeginLandingSlow(flightTime);
                return;
            }

            if (flightTime > 5.0f)
            {
                // Absolute failsafe if Van fell from the map/ground collision
                // never resolves. Input cannot remain hard-locked forever.
                Debug.LogWarning("[TITAN HAMMER LAUNCH] Landing timeout; " +
                    "releasing movement lock safely. NO grounded PASS claimed.", this);
                EndEffect("landing timeout");
            }
        }
        else if (phase == Phase.Slowed)
        {
            remaining -= Time.deltaTime;
            if (remaining <= 0f) EndEffect("landing slow completed");
        }
    }

    private void BeginLandingSlow(float flightTime)
    {
        ReleaseLock();
        Debug.Log("[TITAN HAMMER LAND] Actual CharacterController grounded " +
            "after " + flightTime.ToString("F2") +
            "s flight. Airborne movement unlocked; starting landing slow.", this);
        if (!movement || queuedSlowSeconds <= 0f || queuedSlowMultiplier >= 1f)
        {
            EndEffect("no landing slow configured");
            return;
        }
        movement.ApplySpeedDebuff(queuedSlowMultiplier, queuedSlowSeconds);
        phase = Phase.Slowed;
        remaining = queuedSlowSeconds;
        Debug.Log("[TITAN HAMMER LAND] Slow applied x" +
            queuedSlowMultiplier.ToString("F2") + " for " +
            queuedSlowSeconds.ToString("F1") +
            "s. Van can still dodge; this does not guarantee stomp contact.", this);
    }

    private void EndEffect(string reason)
    {
        ReleaseLock();
        phase = Phase.None;
        Debug.Log("[TITAN HAMMER LAND] Effect finished: " + reason + ".", this);
        enabled = false;
        Destroy(this);
    }

    private void OnDisable() => ReleaseLock();
    private void OnDestroy() => ReleaseLock();

    private void ReleaseLock()
    {
        if (!ownsMovementLock) return;
        if (movement) movement.ReleaseExternalMovementLock();
        ownsMovementLock = false;
    }
}
