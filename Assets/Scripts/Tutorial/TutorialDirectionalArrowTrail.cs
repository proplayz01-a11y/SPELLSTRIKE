using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class TutorialDirectionalArrowTrail : MonoBehaviour
{
    [Header("Arrow Visual")]
    [SerializeField] private Sprite arrowSprite;
    [SerializeField] private Material arrowMaterial;
    [SerializeField] private Color arrowColor = new(1f, 0.72f, 0.08f, 0.82f);
    [SerializeField, Min(0.05f)] private float arrowWorldWidth = 0.9f;
    [SerializeField] private float spriteRotationOffset;
    [SerializeField] private int sortingOrder = 10;

    [Header("Trail Placement")]
    [SerializeField, Min(0f)] private float startPadding = 1.6f;
    [SerializeField, Min(0f)] private float destinationPadding = 0.7f;
    [SerializeField, Min(0.1f)] private float arrowSpacing = 0.85f;
    [SerializeField] private float heightOffset = 0.12f;
    [SerializeField, Min(1)] private int maximumArrows = 48;

    [Header("Animation")]
    [SerializeField, Min(0f)] private float flowSpeed = 1.25f;
    [SerializeField, Range(0f, 1f)] private float minimumEndAlpha = 0.2f;

    [Header("Optional visibility boost (Stage 1 only)")]
    [SerializeField, Range(1f, 5f)] private float visibilityBoost = 1f;
    // Retained to read older scenes, but intentionally no second renderer is
    // created: stacked, differently sized arrows obscured Van's feet.
    [SerializeField, HideInInspector] private bool showGlowSilhouette;
    [SerializeField] private bool coverFullRoute;
    [SerializeField] private bool followGround;
    [SerializeField, Min(0.05f)] private float groundClearance = 0.55f;
    [SerializeField, Min(0.05f)] private float groundRefreshSeconds = 0.25f;
    [SerializeField, Range(0f, 0.3f)] private float gentleGlowPulse = 0.12f;

    private readonly List<SpriteRenderer> arrowRenderers = new();
    private readonly List<float> cachedGroundHeights = new();
    private readonly List<Vector2> lastGroundProbePositions = new();
    private readonly RaycastHit[] groundHits = new RaycastHit[16];
    private Transform player;
    private Transform target;
    private Transform runtimeRoot;
    private bool visible;
    private float nextGroundRefresh;

    /// <summary>
    /// Stage 1 visibility mode. Leave the Tutorial scene at its original
    /// defaults: this changes only the Stage 1 arrow component instance.
    /// </summary>
    public void ConfigureStageVisibility(float boost = 5f, int maxSegments = 120)
    {
        visibilityBoost = Mathf.Clamp(boost, 1f, 5f);
        showGlowSilhouette = false;
        coverFullRoute = true;
        followGround = true;
        // Stage 1 is much larger than the tutorial: these are deliberately
        // ankle-sized ground markers, not torso-height billboards.
        arrowWorldWidth = 7.0f;
        heightOffset = 0.45f;
        groundClearance = 0.55f;
        maximumArrows = Mathf.Max(maximumArrows, maxSegments);
    }

    public void SetEndpoints(Transform playerTransform, Transform targetTransform)
    {
        player = playerTransform;
        target = targetTransform;
        RefreshRootVisibility();
    }

    public void SetVisible(bool shouldShow)
    {
        visible = shouldShow;
        RefreshRootVisibility();
    }

    private void LateUpdate()
    {
        if (!visible || player == null || target == null || arrowSprite == null)
        {
            SetActiveArrowCount(0);
            return;
        }

        Vector3 playerPosition = player.position;
        Vector3 targetPosition = target.position;
        Vector3 flatDirection = targetPosition - playerPosition;
        flatDirection.y = 0f;

        float distance = flatDirection.magnitude;
        float usableDistance = distance - startPadding - destinationPadding;
        if (usableDistance <= 0.05f)
        {
            SetActiveArrowCount(0);
            return;
        }

        Vector3 direction = flatDirection / distance;
        // Unlike the tutorial, Stage 1 has routes stretching over 1,000
        // world units. With a fixed 48-arrow pool, the old path stopped
        // after ~350 units; adapt spacing so even long paths remain visible.
        float effectiveSpacing = coverFullRoute
            ? Mathf.Max(arrowSpacing, usableDistance / Mathf.Max(1, maximumArrows - 1))
            : arrowSpacing;
        float phase = effectiveSpacing > 0f
            ? Mathf.Repeat(Time.unscaledTime * flowSpeed, effectiveSpacing)
            : 0f;
        int requestedCount = Mathf.Min(
            maximumArrows,
            Mathf.CeilToInt(usableDistance / effectiveSpacing) + 1);

        EnsurePool(requestedCount);

        Quaternion arrowRotation =
            Quaternion.LookRotation(Vector3.up, direction)
            * Quaternion.Euler(0f, 0f, spriteRotationOffset);
        // ONE arrow renderer per segment at a constant footprint. Strong HDR
        // tint + gentle pulsation provides glow under the stage's bloom, with
        // no large/small overlapping silhouettes.
        float uniformScale = arrowWorldWidth / arrowSprite.bounds.size.x;
        bool refreshGround = Time.unscaledTime >= nextGroundRefresh;
        if (refreshGround)
            nextGroundRefresh = Time.unscaledTime + groundRefreshSeconds;
        int activeCount = 0;

        for (int index = 0; index < requestedCount; index++)
        {
            float alongTrail = index * effectiveSpacing + phase;
            if (alongTrail > usableDistance)
                continue;

            float distanceFromPlayer = startPadding + alongTrail;
            float normalizedDistance = Mathf.Clamp01(distanceFromPlayer / distance);
            Vector3 position = Vector3.Lerp(
                playerPosition,
                targetPosition,
                normalizedDistance);
            position.y = followGround
                ? GetGroundArrowY(activeCount, position, playerPosition,
                    targetPosition, refreshGround)
                : position.y + heightOffset;

            SpriteRenderer renderer = arrowRenderers[activeCount];
            renderer.gameObject.SetActive(true);
            renderer.transform.SetPositionAndRotation(position, arrowRotation);
            renderer.transform.localScale = Vector3.one * uniformScale;

            float endFade = Mathf.InverseLerp(
                usableDistance,
                Mathf.Max(0f, usableDistance - effectiveSpacing * 1.5f),
                alongTrail);
            Color color = arrowColor;
            float pulse = 1f + gentleGlowPulse *
                Mathf.Sin(Time.unscaledTime * 2.6f - alongTrail * 0.08f);
            color.r *= visibilityBoost * pulse;
            color.g *= visibilityBoost * pulse;
            color.b *= visibilityBoost * pulse;
            color.a = Mathf.Clamp01(color.a + (visibilityBoost - 1f) * 0.1f)
                * Mathf.Lerp(minimumEndAlpha, 1f, endFade);
            renderer.color = color;
            activeCount++;
        }

        SetActiveArrowCount(activeCount);
    }

    private void EnsurePool(int requiredCount)
    {
        if (runtimeRoot == null)
        {
            GameObject rootObject = new("RuntimeArrowTrail");
            runtimeRoot = rootObject.transform;
            runtimeRoot.SetParent(transform, false);
        }

        while (arrowRenderers.Count < requiredCount)
        {
            GameObject arrowObject =
                new($"ArrowSegment_{arrowRenderers.Count + 1:00}");
            arrowObject.transform.SetParent(runtimeRoot, false);

            SpriteRenderer renderer = arrowObject.AddComponent<SpriteRenderer>();
            renderer.sprite = arrowSprite;
            renderer.sortingOrder = sortingOrder;
            if (arrowMaterial != null)
                renderer.sharedMaterial = arrowMaterial;

            arrowRenderers.Add(renderer);

            cachedGroundHeights.Add(float.NaN);
            lastGroundProbePositions.Add(new Vector2(float.NaN, float.NaN));
        }
    }

    private float GetGroundArrowY(int index, Vector3 position,
        Vector3 start, Vector3 destination, bool refresh)
    {
        // Use interpolation if there is no walkable ground hit (for example,
        // a gap or bridge). Otherwise anchor the arrow above the ground,
        // not 10.5 world units over Van's ankles.
        float fallback = position.y + heightOffset;
        if (index >= cachedGroundHeights.Count)
            return fallback;
        Vector2 flat = new Vector2(position.x, position.z);
        if (refresh || float.IsNaN(cachedGroundHeights[index]) ||
            Vector2.SqrMagnitude(flat - lastGroundProbePositions[index]) > 2.25f)
        {
            float highest = Mathf.Max(start.y, destination.y, position.y);
            Vector3 origin = new Vector3(position.x, highest + 35f, position.z);
            float distance = Mathf.Abs(start.y - destination.y) + 125f;
            int count = Physics.RaycastNonAlloc(origin, Vector3.down,
                groundHits, distance, ~((1 << 2) | (1 << 5)),
                QueryTriggerInteraction.Ignore);
            float best = float.PositiveInfinity;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                var hit = groundHits[i];
                if (hit.collider == null || hit.normal.y < 0.62f)
                    continue;
                if (hit.collider.GetComponentInParent<MovementofPlayer>() != null ||
                    hit.collider.GetComponentInParent<EnemyHealth>() != null)
                    continue;
                // Highest walkable hit below the sampling point.
                if (hit.distance >= best)
                    continue;
                best = hit.distance;
                cachedGroundHeights[index] = hit.point.y + groundClearance;
                found = true;
            }
            if (!found)
                cachedGroundHeights[index] = fallback;
            lastGroundProbePositions[index] = flat;
        }
        return cachedGroundHeights[index];
    }

    private void SetActiveArrowCount(int activeCount)
    {
        for (int index = 0; index < arrowRenderers.Count; index++)
            arrowRenderers[index].gameObject.SetActive(index < activeCount);
    }

    private void RefreshRootVisibility()
    {
        if (runtimeRoot != null)
        {
            runtimeRoot.gameObject.SetActive(
                visible && player != null && target != null);
        }

        if (!visible)
            SetActiveArrowCount(0);
    }
}
