using UnityEngine;

/// <summary>Fits the 1448x1086 source layout inside PC/Android landscape canvases.</summary>
[ExecuteAlways]
public sealed class ItemSelectionRevampTestScale : MonoBehaviour
{
    [SerializeField] private float sourceWidth = 1448f;
    [SerializeField] private float sourceHeight = 1086f;
    [SerializeField] private float breathingRoom = 0.96f;
    private RectTransform rect;

    private void LateUpdate()
    {
        if (rect == null) rect = transform as RectTransform;
        if (rect == null || !(rect.parent is RectTransform parent)) return;
        float width = Mathf.Max(1f, parent.rect.width);
        float height = Mathf.Max(1f, parent.rect.height);
        float factor = Mathf.Min(width / sourceWidth, height / sourceHeight);
        factor = Mathf.Max(0.01f, factor * breathingRoom);
        Vector3 target = Vector3.one * factor;
        if ((rect.localScale - target).sqrMagnitude > 0.000001f)
            rect.localScale = target;
    }
}
