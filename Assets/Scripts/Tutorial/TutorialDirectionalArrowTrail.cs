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

    private readonly List<SpriteRenderer> arrowRenderers = new();
    private Transform player;
    private Transform target;
    private Transform runtimeRoot;
    private bool visible;

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
        float phase = arrowSpacing > 0f
            ? Mathf.Repeat(Time.unscaledTime * flowSpeed, arrowSpacing)
            : 0f;
        int requestedCount = Mathf.Min(
            maximumArrows,
            Mathf.CeilToInt(usableDistance / arrowSpacing) + 1);

        EnsurePool(requestedCount);

        Quaternion arrowRotation =
            Quaternion.LookRotation(Vector3.up, direction)
            * Quaternion.Euler(0f, 0f, spriteRotationOffset);
        float uniformScale = arrowWorldWidth / arrowSprite.bounds.size.x;
        int activeCount = 0;

        for (int index = 0; index < requestedCount; index++)
        {
            float alongTrail = index * arrowSpacing + phase;
            if (alongTrail > usableDistance)
                continue;

            float distanceFromPlayer = startPadding + alongTrail;
            float normalizedDistance = Mathf.Clamp01(distanceFromPlayer / distance);
            Vector3 position = Vector3.Lerp(
                playerPosition,
                targetPosition,
                normalizedDistance);
            position.y += heightOffset;

            SpriteRenderer renderer = arrowRenderers[activeCount];
            renderer.gameObject.SetActive(true);
            renderer.transform.SetPositionAndRotation(position, arrowRotation);
            renderer.transform.localScale = Vector3.one * uniformScale;

            float endFade = Mathf.InverseLerp(
                usableDistance,
                Mathf.Max(0f, usableDistance - arrowSpacing * 1.5f),
                alongTrail);
            Color color = arrowColor;
            color.a *= Mathf.Lerp(minimumEndAlpha, 1f, endFade);
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
        }
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
