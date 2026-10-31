using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Restrained cyan-white specular twinkle on older Van's EXISTING painted
/// staff crystal; no editing the source illustration or glowing his whole staff.
/// </summary>
[RequireComponent(typeof(Image))]
public sealed class MenuV2StaffCrystalGlintV10 : MonoBehaviour
{
    [SerializeField] private float repeatSeconds = 4.5f;
    [SerializeField] private float activeSeconds = .72f;
    [SerializeField] private float maxOpacity = .76f;
    private Image image;
    private RectTransform rect;
    private Vector2 initialSize;

    private void Awake()
    {
        image = GetComponent<Image>();
        rect = GetComponent<RectTransform>();
        initialSize = rect.sizeDelta;
    }

    private void Update()
    {
        float phase = Mathf.Repeat(Time.unscaledTime + .8f, repeatSeconds);
        float pulse = phase < activeSeconds
            ? Mathf.Pow(Mathf.Sin(Mathf.PI * phase / activeSeconds), 2)
            : 0f;
        Color c = image.color;
        c.a = pulse * maxOpacity;
        image.color = c;
        rect.sizeDelta = initialSize * (1f + .12f * pulse);
    }
}
