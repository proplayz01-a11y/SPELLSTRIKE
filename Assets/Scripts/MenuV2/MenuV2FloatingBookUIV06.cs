using UnityEngine;

/// <summary>
/// Gently floats the original tutorial 3D book RenderTexture presentation
/// above the right-hand stone. Does not change mesh/prefab or Word Chronicle.
/// </summary>
public sealed class MenuV2FloatingBookUIV06 : MonoBehaviour
{
    [SerializeField] private float amplitudePixels = 9f;
    [SerializeField] private float bobSpeed = 1.05f;
    private RectTransform rect;
    private Vector2 baseline;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        baseline = rect.anchoredPosition;
    }

    private void LateUpdate()
    {
        if (rect == null) return;
        rect.anchoredPosition = baseline +
            Vector2.up * Mathf.Sin(Time.unscaledTime * bobSpeed) * amplitudePixels;
    }
}
