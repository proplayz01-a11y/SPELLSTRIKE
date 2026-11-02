using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Small, allocation-free unscaled-time UI motion for the awarded Spellbook.
/// Runs only while the completion overlay exists and is visible.
/// </summary>
public sealed class TutorialCompletionBookMotion : MonoBehaviour
{
    private RectTransform bookPivot;
    private RectTransform castShadow;
    private RectTransform groundShadow;
    private Image halo;
    private Image rune;
    private Image sparkles;
    private Vector2 bookRest;
    private Vector2 castRest;
    private Vector3 groundScale;
    private Color haloBase;
    private Color runeBase;
    private Color sparklesBase;
    private float elapsed;
    private bool ready;

    public void Initialize(
        RectTransform book, RectTransform cast, RectTransform ground,
        Image haloImage, Image runeImage, Image sparkleImage)
    {
        bookPivot = book;
        castShadow = cast;
        groundShadow = ground;
        halo = haloImage;
        rune = runeImage;
        sparkles = sparkleImage;
        bookRest = bookPivot.anchoredPosition;
        castRest = castShadow.anchoredPosition;
        groundScale = groundShadow.localScale;
        haloBase = halo.color;
        runeBase = rune.color;
        sparklesBase = sparkles.color;
        ready = true;
        Restart();
    }

    public void Restart()
    {
        if (!ready) return;
        ResetPose();
        elapsed = 0f;
    }

    private void Update()
    {
        if (!ready) return;

        elapsed += Time.unscaledDeltaTime;
        // Very small motion keeps the supplied 2D projection believable.
        float wave = Mathf.Sin(elapsed * 1.5f);
        float elevation = 6f + 6f * wave;
        bookPivot.anchoredPosition = bookRest + new Vector2(0f, elevation);
        bookPivot.localRotation = Quaternion.Euler(0f, 0f, 1.65f * Mathf.Sin(elapsed * 0.9f));

        // Cast shadow follows partially. The floor shadow stays anchored.
        castShadow.anchoredPosition = castRest + new Vector2(1.5f, elevation * 0.32f);
        groundShadow.localScale = groundScale * (1f - elevation * 0.0025f);

        SetAlpha(halo, haloBase, 0.53f + 0.10f * Mathf.Sin(elapsed * 1.7f));
        SetAlpha(rune, runeBase, 0.12f + 0.22f * (0.5f + 0.5f * Mathf.Sin(elapsed * 2.2f)));
        SetAlpha(sparkles, sparklesBase,
            0.57f + 0.19f * Mathf.Sin(elapsed * 1.34f + 0.6f)
                 + 0.06f * Mathf.Sin(elapsed * 3.61f));
    }

    private static void SetAlpha(Image image, Color baseColor, float alpha)
    {
        baseColor.a = Mathf.Clamp01(alpha);
        image.color = baseColor;
    }

    private void ResetPose()
    {
        bookPivot.anchoredPosition = bookRest;
        bookPivot.localRotation = Quaternion.identity;
        castShadow.anchoredPosition = castRest;
        groundShadow.localScale = groundScale;
        halo.color = haloBase;
        rune.color = runeBase;
        sparkles.color = sparklesBase;
    }

    private void OnDisable()
    {
        if (ready) ResetPose();
    }
}
