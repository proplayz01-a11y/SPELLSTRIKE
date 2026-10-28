using UnityEngine;

/// <summary>
/// Pure contextual attack eligibility for the V011 review. It chooses only
/// behaviors backed by approved animations; the Titan controller remains the
/// sole owner of battle state, navigation, animation and cooldowns.
/// </summary>
public static class TitanAttackDecision
{
    // Append after existing legacy numeric values; never reorder saved states.
    public enum Intent { Chase, Reposition, Hammerfist, EarthbreakerSlam, ColossalStompV017 }

    /// <summary>
    /// Full Titan production hierarchy. No Random.Range, hidden RNG, attacks
    /// from another tier, or wall-clock cycling. Horizontal WORLD metres:
    /// (A) <=18: four-stomp reservation only, else back away if unsafe;
    /// (B) ~18..34: two-hand slam where physically/visually eligible;
    /// (C) ~34..50: original Hammerfist contact band overlaps slam;
    ///     prefer Hammer if the last attack was Slam, otherwise Slam;
    /// (D) remaining valid ~50..65: slam, then chase/reposition.
    ///
    /// The actual band edges come from the configured physical Hammerfist
    /// reach/tolerance and authored Slam range, not fabricated hit radii.
    /// The controller separately gates awakening, NavMesh, animation state,
    /// real V017 stance/sole geometry, cooldown, HP, CC and interruptions.
    /// </summary>
    public static Intent EvaluateHierarchy(
        float distance, float stompMaxRange, bool reserveCloseRangeForStomp,
        float hammerReach, float hammerTolerance, float hammerAngle,
        float hammerAimTolerance, bool hammerAvailable,
        float slamMinRange, float slamMaxRange, float slamAngle,
        float slamFacingTolerance, bool slamAvailable,
        bool attackReady, bool lastAttackWasHammerfist,
        bool slamRepeatLocked = false,
        bool requireVerifiedSlamHitForOtherAttacks = false)
    {
        if (!float.IsFinite(distance) || distance < 0f)
            return Intent.Chase;

        // Close stomp has priority in ordinary combat, but after a previous
        // Slam MISSED its real frame-71/zone physics check, only a later
        // genuinely confirmed Slam may unlock the other authored attacks.
        if (!requireVerifiedSlamHitForOtherAttacks &&
            reserveCloseRangeForStomp && distance <= stompMaxRange)
            return Intent.Reposition;

        if (!attackReady)
            return Intent.Chase;

        float hammerMin = Mathf.Max(0f, hammerReach - hammerTolerance);
        float hammerMax = hammerReach + hammerTolerance;
        bool hammerEligible = hammerAvailable &&
            distance >= hammerMin && distance <= hammerMax &&
            hammerAngle <= hammerAimTolerance;
        bool slamInRangeAndFacing = slamAvailable &&
            distance >= slamMinRange && distance <= slamMaxRange &&
            slamAngle <= slamFacingTolerance;
        // Once Slam actually starts, the boss cannot cast it again simply
        // because a recovery timer expired while both actors stood still.
        // Only a different started attack or ACTUAL Titan NavMesh displacement
        // unlocks it; the controller owns and verifies that state.
        bool slamEligible = slamInRangeAndFacing && !slamRepeatLocked;

        if (requireVerifiedSlamHitForOtherAttacks)
        {
            if (slamEligible) return Intent.EarthbreakerSlam;
            if (slamRepeatLocked || distance < slamMinRange) return Intent.Reposition;
            return Intent.Chase;
        }

        if (hammerEligible && slamEligible)
            return lastAttackWasHammerfist ? Intent.EarthbreakerSlam : Intent.Hammerfist;
        if (hammerEligible) return Intent.Hammerfist;
        if (slamEligible) return Intent.EarthbreakerSlam;

        // The boss cannot swing its right fist through its own body to reach
        // a player underneath it. Move away until authored contact is legal.
        if (slamRepeatLocked && slamInRangeAndFacing) return Intent.Reposition;
        if (hammerAvailable && distance < hammerMin) return Intent.Reposition;
        return Intent.Chase;
    }

    /// <summary>
    /// Count actual horizontal Titan movement, not player movement, yaw, time
    /// or vertical NavMesh baseOffset. Reject nonfinite coordinates, so an
    /// invalid transform can never unlock repeated Slam.
    /// </summary>
    public static bool HasRepositionedSinceSlam(
        Vector3 currentTitanRoot, Vector3 lastSlamTitanRoot, float requiredWorldMeters)
    {
        if (!float.IsFinite(currentTitanRoot.x) || !float.IsFinite(currentTitanRoot.z) ||
            !float.IsFinite(lastSlamTitanRoot.x) || !float.IsFinite(lastSlamTitanRoot.z) ||
            !float.IsFinite(requiredWorldMeters) || requiredWorldMeters < 2f)
            return false;
        float dx = currentTitanRoot.x - lastSlamTitanRoot.x;
        float dz = currentTitanRoot.z - lastSlamTitanRoot.z;
        return dx * dx + dz * dz >= requiredWorldMeters * requiredWorldMeters;
    }

    /// <summary>
    /// ONE follow-up opportunity opened by a verified real Slam hit. A hit
    /// only grants eligibility, never a free hit, animation cancel, homing,
    /// extra range or cooldown bypass. Slam itself is not a follow-up choice.
    /// The controller checks native V017 stance/Animator/NavMesh separately.
    /// </summary>
    public static Intent EvaluateVerifiedSlamFollowup(float playerPlanarDistance,
        bool attackReady, bool v017Available, float v017MaxRootRange,
        bool hammerAvailable, float hammerRootReach, float hammerTolerance,
        float hammerFacingError, float hammerFacingTolerance)
    {
        if (!float.IsFinite(playerPlanarDistance) || playerPlanarDistance < 0f ||
            !float.IsFinite(hammerRootReach) || hammerRootReach <= 0f ||
            !float.IsFinite(hammerTolerance) || hammerTolerance < 0f ||
            !float.IsFinite(v017MaxRootRange) || v017MaxRootRange < 0f ||
            !float.IsFinite(hammerFacingError) ||
            !float.IsFinite(hammerFacingTolerance))
            return Intent.Chase;

        // If Van is genuinely close, reserve the approved four-stomp
        // planner. The live controller may still refuse invalid left ground,
        // body clearance, Animator state or target motion.
        if (v017Available && playerPlanarDistance <= v017MaxRootRange)
            return attackReady ? Intent.ColossalStompV017 : Intent.Reposition;

        float fistMin = Mathf.Max(0f, hammerRootReach - hammerTolerance);
        float fistMax = hammerRootReach + hammerTolerance;
        if (hammerAvailable && playerPlanarDistance >= fistMin &&
            playerPlanarDistance <= fistMax &&
            hammerFacingError <= hammerFacingTolerance && attackReady)
            return Intent.Hammerfist;

        // Back away when too close, approach when too far, or simply turn / 
        // wait for recovery while inside the real fist-contact band.
        return Intent.Reposition;
    }

    public static Intent Evaluate(float planarDistance, float desiredFistReach,
        float reachTolerance, float angularError, float aimTolerance,
        bool attackReady)
    {
        float reach = Mathf.Max(0.5f, desiredFistReach);
        float tolerance = Mathf.Max(0.5f, reachTolerance);
        if (planarDistance < reach - tolerance)
            return Intent.Reposition;
        if (!attackReady || planarDistance > reach + tolerance ||
            angularError > aimTolerance)
            return Intent.Chase;
        return Intent.Hammerfist;
    }

    /// <summary>
    /// V013 deterministic two-attack review. The boss must not pick a move
    /// while committed or cooling down. Alternate the approved Hammerfist
    /// and Earthbreaker Slam, subject to each move's spatial requirements.
    /// </summary>
    public static Intent EvaluateWithSlam(float distance, float hammerReach,
        float hammerTolerance, float hammerAngle, float hammerAimTolerance,
        float slamFacingError, float slamFacingTolerance, float slamMinRange,
        float slamMaxRange, bool isReady, bool preferSlam, bool slamAvailable)
    {
        if (distance < Mathf.Max(0.5f, hammerReach - hammerTolerance))
            return Intent.Reposition;
        if (!isReady)
            return Intent.Chase;
        if (preferSlam && slamAvailable)
        {
            if (distance >= slamMinRange && distance <= slamMaxRange
                && slamFacingError <= slamFacingTolerance)
                return Intent.EarthbreakerSlam;
            return Intent.Chase;
        }
        return Evaluate(distance, hammerReach, hammerTolerance, hammerAngle,
            hammerAimTolerance, true);
    }
}
