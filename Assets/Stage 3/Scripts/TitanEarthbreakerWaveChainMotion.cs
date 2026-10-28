using UnityEngine;

/// <summary>
/// Physical, player-owned V014 Slam aftermath. Each REAL wave collider hit
/// refreshes a short stun and pushes toward the NEXT authored annulus.
/// CharacterController.Move respects solid world geometry: no teleports,
/// collision bypasses, forced zone hits or direct health modification.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(15000)]
public sealed class TitanEarthbreakerWaveChainMotion : MonoBehaviour
{
    private MovementofPlayer movement;
    private CharacterController body;
    private PlayerHealth health;
    private Vector3 outward;
    private Vector3 targetWorld;
    private float travelSpeed;
    private float travelEndsAt;
    private float stunEndsAt;
    private bool ownsMovementLock;
    private int lastWave;
    private int hitCount;

    public int HitCount => hitCount;
    public int LastWave => lastWave;
    public bool IsStunned => ownsMovementLock && Time.time < stunEndsAt;

    public static float NextRingRadius(float inner, float outer)
    {
        if (!float.IsFinite(inner) || !float.IsFinite(outer) ||
            inner < 0f || outer <= inner)
            return -1f;
        return Mathf.Lerp(inner, outer, .28f);
    }

    public static float FinalWaveRadius(float actualRadius)
    {
        if (!float.IsFinite(actualRadius) || actualRadius < 0f) return -1f;
        return actualRadius + 4f;
    }

    public bool BeginWaveHit(MovementofPlayer receiver, PlayerHealth targetHealth,
        Vector3 impactCenter, float currentOuter, float nextOuter,
        float knockbackSeconds, float stunSeconds, float speedLimit, int wave)
    {
        if (!receiver || !receiver.isActiveAndEnabled || !targetHealth ||
            targetHealth.currentHealth <= 0 ||
            !float.IsFinite(impactCenter.x) || !float.IsFinite(impactCenter.z) ||
            !float.IsFinite(currentOuter) || currentOuter <= 0f ||
            !float.IsFinite(knockbackSeconds) || knockbackSeconds <= 0f ||
            !float.IsFinite(stunSeconds) || stunSeconds <= 0f ||
            !float.IsFinite(speedLimit) || speedLimit <= 0f ||
            wave < 1)
            return false;

        var cc = receiver.GetComponent<CharacterController>();
        if (!cc || !cc.enabled || !cc.gameObject.activeInHierarchy)
            return false;

        if (movement && movement != receiver)
            ReleaseLock();
        movement = receiver;
        body = cc;
        health = targetHealth;

        Vector3 fromCenter = receiver.transform.position - impactCenter;
        fromCenter.y = 0f;
        // A perfectly centered contact still has a deterministic outward
        // direction. Avoid randomized impulses and undefined zero vectors.
        if (fromCenter.sqrMagnitude < .0001f)
        {
            fromCenter = receiver.transform.forward;
            fromCenter.y = 0f;
            if (fromCenter.sqrMagnitude < .0001f) fromCenter = Vector3.forward;
        }
        outward = fromCenter.normalized;

        // The next wave is only a TARGET FOR MOTION, not guaranteed contact.
        // If there is no next annulus, apply a bounded final outward shove.
        float targetRadius = nextOuter > currentOuter
            ? NextRingRadius(currentOuter, nextOuter)
            : FinalWaveRadius(fromCenter.magnitude);
        if (targetRadius < 0f) return false;
        Vector3 centerAtPlayerHeight = new Vector3(
            impactCenter.x, receiver.transform.position.y, impactCenter.z);
        targetWorld = centerAtPlayerHeight + outward * targetRadius;
        float wantedTravel = Mathf.Max(0f,
            Vector3.Dot(targetWorld - receiver.transform.position, outward));

        travelSpeed = Mathf.Min(Mathf.Max(0f, speedLimit),
            wantedTravel / Mathf.Max(.05f, knockbackSeconds));
        travelEndsAt = Time.time + Mathf.Max(.05f, knockbackSeconds);
        // Reapply the FULL new stun on each successive wave, even if already
        // stunned by the previous zone. Exactly one owned movement lock.
        float adjustedStun = PassiveCrowdControlModifier.ModifyStun(stunSeconds);
        stunEndsAt = Time.time + Mathf.Max(0f, adjustedStun);
        if (adjustedStun > 0f && !ownsMovementLock)
        {
            movement.AcquireExternalMovementLock();
            ownsMovementLock = true;
        }
        lastWave = wave;
        hitCount++;
        Debug.Log("[TITAN WAVE CHAIN] HIT wave=" + wave +
                  " actualRadius=" + fromCenter.magnitude.ToString("F2") +
                  "m, physically pushing to radius=" + targetRadius.ToString("F2") +
                  "m, speed=" + travelSpeed.ToString("F2") +
                  "m/s, refreshedStun=" + adjustedStun.ToString("F2") +
                  "s, hits=" + hitCount + ". Not an automatic next-wave hit.", this);
        return true;
    }

    private void LateUpdate()
    {
        if (!movement || !body || !body.enabled ||
            !body.gameObject.activeInHierarchy || !health || health.currentHealth <= 0)
        {
            ReleaseLock();
            enabled = false;
            return;
        }

        if (ownsMovementLock && Time.time >= stunEndsAt)
            ReleaseLock();

        if (Time.time < travelEndsAt && travelSpeed > 0f)
        {
            // Only the CharacterController moves the actual player, and every
            // collision is respected. Never set transform.position.
            Vector3 toDestination = targetWorld - transform.position;
            toDestination.y = 0f;
            float along = Vector3.Dot(toDestination, outward);
            if (along > .001f)
            {
                float step = Mathf.Min(travelSpeed * Time.deltaTime, along);
                body.Move(outward * step);
            }
        }

        if (!ownsMovementLock && Time.time >= travelEndsAt)
            enabled = false;
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
