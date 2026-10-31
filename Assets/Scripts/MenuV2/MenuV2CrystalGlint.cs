using UnityEngine;
using UnityEngine.UI;

/// <summary>Only the central logo crystal glints; the wordmark stays static.</summary>
public sealed class MenuV2CrystalGlint : MonoBehaviour
{
    [SerializeField] private Image glintImage;
    [SerializeField] private float repeatSeconds = 3.6f;
    [SerializeField] private float activeSeconds = .7f;

    public void Configure(Image image) => glintImage = image;

    private void Update()
    {
        if (glintImage == null) return;
        float elapsed = Mathf.Repeat(Time.unscaledTime, repeatSeconds);
        float pulse = elapsed < activeSeconds
            ? Mathf.Pow(Mathf.Sin(Mathf.PI * elapsed / activeSeconds), 2f)
            : 0f;
        Color color = glintImage.color;
        color.a = pulse * .68f;
        glintImage.color = color;
    }
}
