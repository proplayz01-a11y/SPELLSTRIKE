using UnityEngine;

/// <summary>
/// Keeps the reference-composition (1448x1086) artwork proportional on
/// both 16:9 and tall Android displays. Only attached to the isolated preview.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class StageSelectReferenceFitToCanvas : MonoBehaviour
{
    [SerializeField] private Vector2 referenceSize = new Vector2(1448f, 1086f);
    [SerializeField, Range(0.8f, 1f)] private float safeAreaFraction = 0.97f;

    private void LateUpdate()
    {
        var rect = transform as RectTransform;
        var parent = transform.parent as RectTransform;
        if (rect == null || parent == null) return;
        float scale = Mathf.Min(parent.rect.width / referenceSize.x,
            parent.rect.height / referenceSize.y) * safeAreaFraction;
        if (scale <= 0f || !float.IsFinite(scale)) return;
        var target = new Vector3(scale, scale, 1f);
        if ((rect.localScale - target).sqrMagnitude > 0.000001f)
            rect.localScale = target;
    }
}
