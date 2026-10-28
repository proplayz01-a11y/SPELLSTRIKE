using System;
using UnityEngine;

/// <summary>
/// V014: STATIC concentric earthquake zones triggered in order.
/// Wave 1 is the filled central disk; each later wave is a fixed annulus
/// between consecutive inspector radii. Nothing expands or travels.
/// The frame-71 animation event starts this timed sequence exactly once.
/// </summary>
[DisallowMultipleComponent]
public sealed class TitanEarthbreakerProgressiveWaves : MonoBehaviour
{
    [Serializable]
    public sealed class WaveZone
    {
        public string label = "Wave";
        [Min(0.25f)] public float outerRadius = 10f;
        [Min(0f)] public float damage = 30f;
        [Tooltip("Wait after the previous wave, measured from its activation.")]
        [Min(0f)] public float delayAfterPrevious = .35f;
        public Color gizmoColor = new Color(1f, .22f, .12f, 1f);
    }

    [Header("Fixed concentric gizmos and sequential effects")]
    [Tooltip("Index 0 hits 0..radius; index 1 hits radius0..radius1, etc. Entries MUST have increasing radii.")]
    [SerializeField] private WaveZone[] waves =
    {
        new WaveZone { label="Wave 1 / Core", outerRadius=10f, damage=6f, delayAfterPrevious=0f, gizmoColor=new Color(1f,.12f,.1f,1f) },
        new WaveZone { label="Wave 2", outerRadius=20f, damage=5f, delayAfterPrevious=.35f, gizmoColor=new Color(1f,.35f,.1f,1f) },
        new WaveZone { label="Wave 3", outerRadius=30f, damage=4f, delayAfterPrevious=.35f, gizmoColor=new Color(1f,.6f,.12f,1f) },
        new WaveZone { label="Wave 4", outerRadius=40f, damage=3f, delayAfterPrevious=.35f, gizmoColor=new Color(.95f,.8f,.2f,1f) },
        new WaveZone { label="Wave 5", outerRadius=52f, damage=2f, delayAfterPrevious=.35f, gizmoColor=new Color(.7f,.9f,.35f,1f) },
    };

    [Header("Finite sequence loop, not endless arena damage")]
    [SerializeField] private bool loopSequence;
    [SerializeField, Range(1, 5)] private int repeatCycles = 2;
    [SerializeField, Min(0f)] private float delayBetweenCycles = .5f;
    [SerializeField, Min(0f)] private float holdAfterLastWave = .35f;

    [Header("Player collision and fair avoidance")]
    [SerializeField, Min(0.1f)] private float verticalTolerance = 12f;
    [SerializeField] private bool allowJumpToAvoidOuterZones = true;
    [SerializeField, Min(0f)] private float jumpClearanceAboveImpact = 1.5f;
    [SerializeField] private bool respectSolidCover = true;
    [Tooltip("SECOND explicit HP damage gate. Only real unblocked/undodged player collider hits apply the configured damage; independent of wave knockback/stun.")]
    [SerializeField] private bool allowDamageInReview;
    [Tooltip("ON allows one damage event for each independently verified wave. OFF allows only one HP damage event per entire Slam.")]
    [SerializeField] private bool allowRepeatedDamagePerSlam;
    [Tooltip("Optional safety budget across every zone and cycle within ONE Slam. 0 keeps older prefabs uncapped; Stage3 Titan uses 20 HP maximum, versus Van's 100 HP.")]
    [SerializeField, Min(0f)] private float maxTotalDamagePerSlam;

    [Header("V014: HIT-CONFIRMED Sequential Wave Knockback / Restun")]
    [Tooltip("ONLY physical Van collider hits receive horizontal knockback toward the next annulus and a freshly timed stun. Every wave rechecks real contact, cover and jumping; no forced chain and no HP damage implied.")]
    [SerializeField] private bool enableHitConfirmedWaveChain;
    [Tooltip("0.5 seconds of world-space horizontal knockback, shorter than the production 0.6s wave interval; obstacles still block the player.")]
    [SerializeField, Range(.1f, .95f)] private float chainKnockbackSeconds = .5f;
    [Tooltip("Stun refreshes on each separate actual wave contact. It ends before the next wave to leave a small jump/dodge window.")]
    [SerializeField, Range(.05f, 1f)] private float chainStunSecondsPerHit = .32f;
    [Tooltip("Maximum genuine player horizontal motion speed; not a teleport or guaranteed push into another zone.")]
    [SerializeField, Range(2f, 65f)] private float chainMaxKnockbackWorldSpeed = 48f;

    [Header("Scene gizmo (matches the Paint concentric-ring layout)")]
    [SerializeField] private bool showSceneGizmos = true;
    [SerializeField] private bool showGizmosWhenNotSelected;
    [Tooltip("Root-local projected midpoint of the two fists at contact frame71. Editable for the arena.")]
    [SerializeField] private Vector3 previewCenterLocal = new Vector3(0f, .0f, .4f);
    [Header("Temporary game-view line indicators (not final earthquake VFX)")]
    [SerializeField] private bool showRuntimeRingIndicators = true;
    [SerializeField, Range(24, 180)] private int lineSegments = 96;
    [SerializeField, Min(.02f)] private float lineWidth = .35f;
    [SerializeField, Min(0f)] private float pulseHighlightSeconds = .3f;

    [Header("Read-only live QA counters")]
    [SerializeField] private bool sequenceActive;
    [SerializeField] private int currentCycle;
    [SerializeField] private int nextZoneIndex;
    [SerializeField] private int totalZonesFired;
    [SerializeField] private int totalPlayerHits;
    [SerializeField] private int totalActualDamageEvents;
    [SerializeField] private float actualDamageThisSlam;
    [SerializeField] private int lastZoneFired = -1;
    [SerializeField] private float lastPotentialDamage;
    [SerializeField] private bool lastPlayerContact;
    [SerializeField] private bool lastBlockedByCover;
    [SerializeField] private bool lastAvoidedByJump;
    [SerializeField] private Vector3 lastImpactCenter;
    [SerializeField] private int totalWaveKnockbacks;
    [SerializeField] private bool lastWaveKnockbackApplied;

    private TitanEarthbreakerController owner;
    private PlayerHealth target;
    private bool ownerAllowsDamage;
    private bool alreadyDealtDamage;
    private float nextZoneAt;
    private bool previewActive;
    private Vector3 previewCenter;
    private LineRenderer[] lines;
    private Material lineMaterial;
    private int litZoneIndex = -1;
    private float litUntil;

    public bool IsSequenceActive => sequenceActive;
    public int TotalZonesFired => totalZonesFired;
    public int TotalPlayerHits => totalPlayerHits;
    public int TotalActualDamageEvents => totalActualDamageEvents;
    public float ActualDamageThisSlam => actualDamageThisSlam;
    public int LastZoneFired => lastZoneFired;
    public int CurrentCycle => currentCycle;
    public Vector3 ImpactCenter => lastImpactCenter;
    public float LastPotentialDamage => lastPotentialDamage;
    public bool LastPlayerContact => lastPlayerContact;
    public int TotalWaveKnockbacks => totalWaveKnockbacks;
    public bool LastWaveKnockbackApplied => lastWaveKnockbackApplied;
    public int ZoneCount => waves == null ? 0 : waves.Length;
    public int CycleCount => loopSequence ? Mathf.Clamp(repeatCycles, 1, 5) : 1;
    /// <summary>
    /// One-indexed zone number for a planar distance; zero means outside the
    /// final circle. Exactly one zone owns each radius, including boundaries.
    /// </summary>
    public int ZoneForPlanarDistance(float planarDistance)
    {
        if (!ValidateZones() || planarDistance < 0f) return 0;
        for (int i = 0; i < waves.Length; i++)
            if (planarDistance <= waves[i].outerRadius)
                return i + 1;
        return 0;
    }
    public float PlannedDurationSeconds
    {
        get
        {
            if (waves == null || waves.Length == 0) return 0f;
            float perCycle = 0f;
            foreach (var zone in waves)
                if (zone != null) perCycle += Mathf.Max(0f, zone.delayAfterPrevious);
            return perCycle * CycleCount +
                   Mathf.Max(0f, delayBetweenCycles) * (CycleCount - 1) +
                   Mathf.Max(0f, holdAfterLastWave);
        }
    }

    private void OnValidate()
    {
        maxTotalDamagePerSlam = Mathf.Max(0f, maxTotalDamagePerSlam);
        if (waves == null) return;
        // Never silently reorder custom inspector entries; radii must be
        // strictly increasing for non-overlapping concentric annuli.
        float previous = 0f;
        foreach (var zone in waves)
        {
            if (zone == null) continue;
            zone.outerRadius = Mathf.Max(previous + .25f, zone.outerRadius);
            zone.damage = Mathf.Max(0f, zone.damage);
            zone.delayAfterPrevious = Mathf.Max(0f, zone.delayAfterPrevious);
            previous = zone.outerRadius;
        }
    }

    public void ArmPreview(TitanEarthbreakerController titan)
    {
        owner = titan;
        if (!ValidateZones()) return;
        sequenceActive = false;
        previewActive = true;
        previewCenter = transform.TransformPoint(previewCenterLocal);
        if (TryProjectGround(previewCenter, out var floor))
            previewCenter = floor;
        litZoneIndex = -1;
        UpdateRuntimeRings(previewCenter);
    }

    public bool BeginAtImpact(TitanEarthbreakerController titan, PlayerHealth player,
        Vector3 groundedCenter, bool damageGate)
    {
        StopSequence();
        if (!ValidateZones() || titan == null || !titan.IsBattleStarted ||
            titan.State != TitanEarthbreakerController.TitanState.EarthbreakerSlam)
            return false;
        owner = titan;
        target = player;
        ownerAllowsDamage = damageGate;
        lastImpactCenter = groundedCenter;
        sequenceActive = true;
        previewActive = false;
        currentCycle = 1;
        nextZoneIndex = 0;
        totalZonesFired = 0;
        totalPlayerHits = 0;
        totalActualDamageEvents = 0;
        actualDamageThisSlam = 0f;
        alreadyDealtDamage = false;
        lastZoneFired = -1;
        lastPlayerContact = false;
        lastBlockedByCover = false;
        lastAvoidedByJump = false;
        totalWaveKnockbacks = 0;
        lastWaveKnockbackApplied = false;
        nextZoneAt = Time.time + Mathf.Max(0f, waves[0].delayAfterPrevious);
        UpdateRuntimeRings(lastImpactCenter);
        ProcessDueZones();
        return true;
    }

    private bool ValidateZones()
    {
        if (waves == null || waves.Length == 0) return false;
        float last = 0f;
        foreach (var zone in waves)
        {
            if (zone == null || zone.outerRadius <= last || zone.outerRadius <= 0f)
                return false;
            last = zone.outerRadius;
        }
        return true;
    }

    private void Update()
    {
        if (sequenceActive)
        {
            if (!owner || !owner.IsBattleStarted ||
                owner.State != TitanEarthbreakerController.TitanState.EarthbreakerSlam)
            {
                StopSequence();
                return;
            }
            ProcessDueZones();
        }
        if (litZoneIndex >= 0 && Time.time >= litUntil)
        {
            litZoneIndex = -1;
            UpdateRingColors();
        }
    }

    private void ProcessDueZones()
    {
        int guard = 0;
        while (sequenceActive && Time.time >= nextZoneAt && guard++ < 100)
        {
            if (nextZoneIndex < waves.Length)
            {
                int index = nextZoneIndex++;
                ActivateZone(index);
                float delay = nextZoneIndex < waves.Length
                    ? waves[nextZoneIndex].delayAfterPrevious
                    : (currentCycle < CycleCount ? delayBetweenCycles : holdAfterLastWave);
                nextZoneAt += Mathf.Max(0f, delay);
            }
            else if (currentCycle < CycleCount)
            {
                currentCycle++;
                nextZoneIndex = 0;
                nextZoneAt += Mathf.Max(0f, waves[0].delayAfterPrevious);
            }
            else
            {
                sequenceActive = false;
                previewActive = false;
                HideRuntimeRings();
                Debug.Log("[TitanEarthquake] Sequence completed; attack may recover.", this);
            }
        }
    }

    private void ActivateZone(int index)
    {
        var zone = waves[index];
        float inner = index == 0 ? 0f : waves[index - 1].outerRadius;
        float outer = zone.outerRadius;
        lastZoneFired = index + 1;
        totalZonesFired++;
        litZoneIndex = index;
        litUntil = Time.time + Mathf.Max(0f, pulseHighlightSeconds);
        lastPlayerContact = false;
        lastBlockedByCover = false;
        lastAvoidedByJump = false;
        lastWaveKnockbackApplied = false;
        lastPotentialDamage = 0f;

        if (target && target.currentHealth > 0)
        {
            var delta = target.transform.position - lastImpactCenter;
            float planar = new Vector2(delta.x, delta.z).magnitude;
            if (ZoneForPlanarDistance(planar) == index + 1 &&
                Mathf.Abs(delta.y) <= verticalTolerance &&
                HasActualPlayerCollider(outer))
            {
                if (respectSolidCover && IsCovered(target.transform.position))
                    lastBlockedByCover = true;
                else
                {
                    var cc = target.GetComponent<CharacterController>();
                    if (index > 0 && allowJumpToAvoidOuterZones &&
                        cc && !cc.isGrounded && delta.y >= jumpClearanceAboveImpact)
                        lastAvoidedByJump = true;
                    else
                    {
                        lastPlayerContact = true;
                        lastPotentialDamage = zone.damage;
                        totalPlayerHits++;
                        // This fires ONLY after the authentic timed wave hit
                        // the live player collider and passed cover/jump gates.
                        // The boss uses this evidence for follow-up animation
                        // permission EVEN WITH ALL HP/CC TOGGLES OFF.
                        if (owner)
                            owner.ReportEarthbreakerZonePhysicsHit(
                                this, target, currentCycle, index + 1);
                        if (enableHitConfirmedWaveChain)
                        {
                            MovementofPlayer movement = target.GetComponent<MovementofPlayer>();
                            CharacterController playerBody = target.GetComponent<CharacterController>();
                            if (movement && playerBody && playerBody.enabled &&
                                movement.isActiveAndEnabled)
                            {
                                var chain = movement.GetComponent<TitanEarthbreakerWaveChainMotion>();
                                if (!chain)
                                    chain = movement.gameObject.AddComponent<TitanEarthbreakerWaveChainMotion>();
                                chain.enabled = true;
                                float nextOuter = index + 1 < waves.Length
                                    ? waves[index + 1].outerRadius : -1f;
                                // Physical knockback must finish before the
                                // next authored zone; each zone's next delay
                                // may differ from our inspector default.
                                float nextDelay = index + 1 < waves.Length
                                    ? waves[index + 1].delayAfterPrevious
                                    : chainKnockbackSeconds;
                                float travelSeconds = Mathf.Min(chainKnockbackSeconds,
                                    Mathf.Max(.05f, nextDelay - .025f));
                                if (chain.BeginWaveHit(movement, target,
                                    lastImpactCenter, outer, nextOuter,
                                    travelSeconds, chainStunSecondsPerHit,
                                    chainMaxKnockbackWorldSpeed, index + 1))
                                {
                                    lastWaveKnockbackApplied = true;
                                    totalWaveKnockbacks++;
                                }
                            }
                        }
                        if (allowDamageInReview && ownerAllowsDamage &&
                            (!alreadyDealtDamage || allowRepeatedDamagePerSlam))
                        {
                            float actualDamage = BalanceCappedDamage(
                                zone.damage, actualDamageThisSlam, maxTotalDamagePerSlam);
                            if (actualDamage > 0f)
                            {
                                // Per-zone physical HIT/cover/jump guards are
                                // authoritative. A chain push NEVER credits
                                // damage on an upcoming wave automatically.
                                target.TakeDamage(actualDamage);
                                actualDamageThisSlam += actualDamage;
                                totalActualDamageEvents++;
                                alreadyDealtDamage = true;
                            }
                        }
                    }
                }
            }
        }
        UpdateRingColors();
        Debug.Log($"[TitanEarthquake] cycle={currentCycle}/{CycleCount} " +
            $"zone={index + 1}/{waves.Length} radius=({inner:0.0},{outer:0.0}] " +
            $"potentialDamage={lastPotentialDamage:0.0} " +
            $"hit={lastPlayerContact} cover={lastBlockedByCover} " +
            $"jumpAvoid={lastAvoidedByJump} chain={lastWaveKnockbackApplied} " +
            $"HPappliedThisSlam={actualDamageThisSlam:0.#} " +
            $"HPenabled={allowDamageInReview && ownerAllowsDamage}.", this);
    }

    // Pure, independently testable safety budget. A configured zero cap is
    // legacy-unlimited; the Stage3 production prefab sets 20 HP explicitly.
    public static float BalanceCappedDamage(float requested, float alreadyApplied,
        float cap)
    {
        if (!float.IsFinite(requested) || !float.IsFinite(alreadyApplied) ||
            !float.IsFinite(cap) || requested <= 0f || alreadyApplied < 0f || cap < 0f)
            return 0f;
        if (cap == 0f) return requested;
        return Mathf.Min(requested, Mathf.Max(0f, cap - alreadyApplied));
    }

    private bool HasActualPlayerCollider(float radius)
    {
        foreach (var hit in Physics.OverlapSphere(lastImpactCenter, radius,
                     Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            if (hit && hit.GetComponentInParent<PlayerHealth>() == target)
                return true;
        return false;
    }

    private bool IsCovered(Vector3 playerPosition)
    {
        var origin = lastImpactCenter + Vector3.up * 3f;
        var destination = playerPosition + Vector3.up * 3f;
        var direction = destination - origin;
        float length = direction.magnitude;
        if (length < .1f) return false;
        foreach (var hit in Physics.RaycastAll(origin, direction / length, length,
                     Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (!hit.collider || hit.collider.transform.IsChildOf(transform)
                || hit.collider.GetComponentInParent<PlayerHealth>() == target
                || hit.distance < .2f)
                continue;
            if (hit.normal.y > .8f && hit.point.y <= lastImpactCenter.y + .2f)
                continue;
            return true;
        }
        return false;
    }

    private bool TryProjectGround(Vector3 location, out Vector3 ground)
    {
        ground = location;
        float best = float.NegativeInfinity;
        foreach (var hit in Physics.RaycastAll(location + Vector3.up * 30f,
                     Vector3.down, 90f, Physics.DefaultRaycastLayers,
                     QueryTriggerInteraction.Ignore))
        {
            if (!hit.collider || hit.collider.transform.IsChildOf(transform)
                || hit.normal.y < .6f || hit.point.y > location.y + 10f ||
                hit.point.y <= best)
                continue;
            best = hit.point.y;
            ground = hit.point;
        }
        return best > float.NegativeInfinity;
    }

    public void StopSequence()
    {
        sequenceActive = false;
        previewActive = false;
        target = null;
        ownerAllowsDamage = false;
        litZoneIndex = -1;
        HideRuntimeRings();
    }

    private void OnDisable() => StopSequence();
    private void OnDestroy()
    {
        if (lineMaterial != null) Destroy(lineMaterial);
    }

    private void UpdateRuntimeRings(Vector3 center)
    {
        if (!showRuntimeRingIndicators || !Application.isPlaying || !ValidateZones())
            return;
        if (lines == null || lines.Length != waves.Length)
        {
            if (lines != null)
                foreach (var old in lines) if (old) Destroy(old.gameObject);
            lines = new LineRenderer[waves.Length];
            var shader = Shader.Find("Sprites/Default");
            if (!shader) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (!shader) return;
            if (lineMaterial != null) Destroy(lineMaterial);
            lineMaterial = new Material(shader);
            lineMaterial.hideFlags = HideFlags.DontSave;
            for (int i = 0; i < lines.Length; i++)
            {
                var child = new GameObject($"QA Static Earthquake Wave {i + 1}");
                child.transform.SetParent(transform, false);
                var ring = child.AddComponent<LineRenderer>();
                ring.useWorldSpace = true;
                ring.loop = true;
                ring.widthMultiplier = lineWidth;
                ring.positionCount = lineSegments;
                ring.sharedMaterial = lineMaterial;
                ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                ring.receiveShadows = false;
                lines[i] = ring;
            }
        }
        for (int i = 0; i < lines.Length; i++)
        {
            if (!lines[i]) continue;
            lines[i].gameObject.SetActive(true);
            float r = waves[i].outerRadius;
            for (int j = 0; j < lineSegments; j++)
            {
                float a = j * Mathf.PI * 2f / lineSegments;
                lines[i].SetPosition(j, center + new Vector3(Mathf.Cos(a) * r, .2f, Mathf.Sin(a) * r));
            }
        }
        UpdateRingColors();
    }

    private void UpdateRingColors()
    {
        if (lines == null) return;
        for (int i = 0; i < lines.Length && i < waves.Length; i++)
        {
            if (!lines[i]) continue;
            var c = waves[i].gizmoColor;
            c.a = i == litZoneIndex ? 1f : .25f;
            lines[i].startColor = c;
            lines[i].endColor = c;
        }
    }

    private void HideRuntimeRings()
    {
        if (lines == null) return;
        foreach (var ring in lines) if (ring) ring.gameObject.SetActive(false);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected() => DrawCircles();
    private void OnDrawGizmos()
    {
        if (showGizmosWhenNotSelected) DrawCircles();
    }
    private void DrawCircles()
    {
        if (!showSceneGizmos || !ValidateZones()) return;
        Vector3 c = sequenceActive ? lastImpactCenter :
            (previewActive ? previewCenter : transform.TransformPoint(previewCenterLocal));
        if (!sequenceActive && !previewActive && TryProjectGround(c, out var floor))
            c = floor;
        for (int i = 0; i < waves.Length; i++)
        {
            var w = waves[i];
            UnityEditor.Handles.color = w.gizmoColor;
            UnityEditor.Handles.DrawWireDisc(c, Vector3.up, w.outerRadius);
            UnityEditor.Handles.Label(c + Vector3.right * w.outerRadius + Vector3.up * .35f,
                $"{i + 1}: {w.label} / damage={w.damage:0.#}");
        }
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(c + Vector3.up * 1.5f, "Slam impact center");
    }
#endif
}
