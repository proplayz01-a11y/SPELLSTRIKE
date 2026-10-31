using UnityEngine;
using UnityEngine.UI;

/// <summary>Very subtle shadow size/intensity response to book float height.</summary>
[RequireComponent(typeof(Image))]
public sealed class MenuV2BookShadowPulseV10 : MonoBehaviour
{
    private Image image;
    private RectTransform rect;
    private Vector2 size;

    private void Awake()
    {
        image = GetComponent<Image>();
        rect = GetComponent<RectTransform>();
        size = rect.sizeDelta;
    }

    private void LateUpdate()
    {
        float floatHeight = (Mathf.Sin(Time.unscaledTime * 1.05f) + 1f) * .5f;
        rect.sizeDelta = size * Mathf.Lerp(1f, 1.085f, floatHeight);
        Color c = image.color;
        c.a = Mathf.Lerp(.92f, .72f, floatHeight);
        image.color = c;
    }
}
