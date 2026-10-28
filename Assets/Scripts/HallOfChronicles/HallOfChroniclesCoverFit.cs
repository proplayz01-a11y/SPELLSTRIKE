using UnityEngine;

/// <summary>Cover, never stretch, the 2400x1080 layered environment as one unit.</summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class HallOfChroniclesCoverFit : MonoBehaviour
{
    [SerializeField] private RectTransform viewport;
    [SerializeField] private Vector2 sourceSize = new Vector2(2400f, 1080f);
    private RectTransform ownRect;

    public void Configure(RectTransform viewportRect)
    {
        viewport = viewportRect;
        Refresh();
    }

    private void OnEnable() => Refresh();
    private void OnRectTransformDimensionsChange() => Refresh();
    private void LateUpdate() => Refresh();

    public void Refresh()
    {
        if (viewport == null)
            return;
        if (ownRect == null)
            ownRect = transform as RectTransform;
        if (ownRect == null)
            return;

        Rect r = viewport.rect;
        if (r.width <= 0f || r.height <= 0f || sourceSize.x <= 0f || sourceSize.y <= 0f)
            return;
        float scale = Mathf.Max(r.width / sourceSize.x, r.height / sourceSize.y);
        Vector3 desired = new Vector3(scale, scale, 1f);
        if ((ownRect.localScale - desired).sqrMagnitude > 0.0000001f)
            ownRect.localScale = desired;
        if (ownRect.anchoredPosition != Vector2.zero)
            ownRect.anchoredPosition = Vector2.zero;
    }
}
