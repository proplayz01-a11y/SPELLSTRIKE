using UnityEngine;

[DisallowMultipleComponent]
public sealed class StoneGolemSlamDebuff : MonoBehaviour
{
    private enum Phase { None, Stun, Slow }

    private MovementofPlayer movement;
    private PlayerHealth health;
    private Phase phase;
    private float remaining;
    private float pendingSlowMultiplier;
    private float pendingSlowDuration;
    private bool ownsMovementLock;

    public bool IsActive => phase != Phase.None;
    public bool IsStunned => phase == Phase.Stun;
    public bool IsSlowed => phase == Phase.Slow;
    public float Remaining => Mathf.Max(0f, remaining);

    public void Begin(
        MovementofPlayer targetMovement,
        PlayerHealth targetHealth,
        float stunDuration,
        float slowMultiplier,
        float slowDuration)
    {
        if (targetMovement == null)
            return;

        movement = targetMovement;
        health = targetHealth;
        pendingSlowMultiplier = Mathf.Clamp(slowMultiplier, 0.01f, 1f);
        pendingSlowDuration = Mathf.Max(0f, slowDuration);

        // This encounter uses a fixed authored stun window. Do not shorten it
        // through generic passive crowd-control resistance, otherwise the Golem's
        // punish sequence becomes visually inconsistent and too brief.
        float effectiveStun = Mathf.Max(0f, stunDuration);
        if (effectiveStun > 0f)
        {
            if (!ownsMovementLock)
            {
                movement.AcquireExternalMovementLock();
                ownsMovementLock = true;
            }
            phase = Phase.Stun;
            remaining = effectiveStun;
            Debug.Log($"[StoneGolemSlamDebuff] STUN START {effectiveStun:0.00}s.", this);
            return;
        }

        BeginSlow();
    }

    private void Update()
    {
        if (phase == Phase.None || movement == null)
            return;

        if (health != null && health.currentHealth <= 0)
        {
            ReleaseMovementLock();
            phase = Phase.None;
            Destroy(this);
            return;
        }

        remaining -= Time.deltaTime;

        if (phase == Phase.Stun)
        {
            if (remaining > 0f)
                return;

            ReleaseMovementLock();
            BeginSlow();
            return;
        }

        if (phase == Phase.Slow && remaining <= 0f)
        {
            phase = Phase.None;
            Debug.Log("[StoneGolemSlamDebuff] DEBUFF END.", this);
            Destroy(this);
        }
    }

    private void BeginSlow()
    {
        if (movement == null || pendingSlowDuration <= 0f)
        {
            phase = Phase.None;
            Destroy(this);
            return;
        }

        movement.ApplySpeedDebuff(pendingSlowMultiplier, pendingSlowDuration);
        phase = Phase.Slow;
        remaining = pendingSlowDuration;
        Debug.Log($"[StoneGolemSlamDebuff] SLOW START x{pendingSlowMultiplier:0.00} for {pendingSlowDuration:0.00}s.", this);
    }

    private void OnDisable()
    {
        ReleaseMovementLock();
    }

    private void ReleaseMovementLock()
    {
        if (!ownsMovementLock || movement == null)
            return;

        movement.ReleaseExternalMovementLock();
        ownsMovementLock = false;
    }
}
