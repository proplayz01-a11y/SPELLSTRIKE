using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Low-cost magical idle animation for the already-approved Word Master selection card.
/// Animates only the four independently separated icon layers; never the text,
/// parchment, inner green trim, card hitbox, or gold-hover reflection.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class ModeSelectWordMasterMagicMotion : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Existing separated Word Master icon layers")]
    [SerializeField] private RectTransform greenAura;
    [SerializeField] private RectTransform spellbook;
    [SerializeField] private RectTransform magicRings;
    [SerializeField] private RectTransform magicStar;

    [Header("Gentle, Android-friendly motion")]
    [SerializeField, Range(0f, 6f)] private float bookBobPixels = 2.8f;
    [SerializeField, Range(0f, 3f)] private float bookTiltDegrees = 1.15f;
    [SerializeField, Min(1f)] private float bookCycleSeconds = 3.4f;
    [SerializeField, Range(0f, 0.12f)] private float auraScalePulse = 0.035f;
    [SerializeField, Min(1f)] private float auraCycleSeconds = 4.2f;
    [SerializeField, Range(0f, 12f)] private float ringsTiltDegrees = 5.5f;
    [SerializeField, Min(2f)] private float ringsCycleSeconds = 7.5f;
    [SerializeField, Range(0f, 0.12f)] private float starScalePulse = 0.035f;
    [SerializeField, Min(1f)] private float starCycleSeconds = 3.1f;
    [Header("Hover-only animation")]
    [SerializeField, Min(0.01f)] private float hoverFadeInSeconds = 0.16f;
    [SerializeField, Min(0.01f)] private float hoverSettleSeconds = 0.20f;

    private sealed class Layer
    {
        public RectTransform rect;
        public Vector2 originalPosition;
        public Vector2 originalPivot;
        public Quaternion originalRotation;
        public Vector3 originalScale;
        public Vector2 centeredPosition;
        public CanvasGroup alpha;
        public float originalAlpha;
        public bool createdAlpha;
    }

    private Layer auraLayer, bookLayer, ringsLayer, starLayer;
    private float animationTime;
    private float hoverBlend;
    private bool pointerInside;
    private bool pointerHeld;
    private bool ready;

    private const float CanvasSourceWidth = 872f;
    private const float CanvasSourceHeight = 172f;
    private const float Tau = 6.28318530718f;

    private void OnEnable()
    {
        if (!Application.isPlaying) return;

        if (greenAura == null) greenAura = FindLayer("WM_03_GreenAura");
        if (spellbook == null) spellbook = FindLayer("WM_04_Spellbook");
        if (magicRings == null) magicRings = FindLayer("WM_06_MagicRings");
        if (magicStar == null) magicStar = FindLayer("WM_05_MagicStar");

        if (greenAura == null || spellbook == null || magicRings == null || magicStar == null)
        {
            Debug.LogWarning("[Mode Select Word Master] Missing one or more separated magical layers.", this);
            return;
        }

        // Pivots use the visible painted icon bounds, not the 872x172 card canvas.
        // This keeps the original composited artwork perfectly aligned at rest.
        auraLayer = Capture(greenAura, 34f, 25f, 239f, 115f, true);
        bookLayer = Capture(spellbook, 75f, 77f, 188f, 68f, false);
        ringsLayer = Capture(magicRings, 98f, 50f, 135f, 35f, false);
        starLayer = Capture(magicStar, 125f, 16f, 82f, 86f, true);
        animationTime = 0f;
        hoverBlend = 0f;
        pointerInside = false;
        pointerHeld = false;
        ready = true;
    }

    public void OnPointerEnter(PointerEventData eventData) { pointerInside = true; }
    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        pointerHeld = false;
    }

    // Touch devices have no persistent hover; animate only during the press.
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData != null && eventData.button == PointerEventData.InputButton.Left)
            pointerHeld = true;
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        pointerHeld = false;
        if (Application.isMobilePlatform && !Application.isEditor)
            pointerInside = false;
    }

    private RectTransform FindLayer(string name)
    {
        Transform child = transform.Find(name);
        return child == null ? null : child as RectTransform;
    }

    private static Layer Capture(RectTransform rect, float x, float y, float w, float h, bool animatedOpacity)
    {
        Layer state = new Layer();
        state.rect = rect;
        state.originalPosition = rect.anchoredPosition;
        state.originalPivot = rect.pivot;
        state.originalRotation = rect.localRotation;
        state.originalScale = rect.localScale;

        Vector2 contentPivot = new Vector2(
            (x + w * 0.5f) / CanvasSourceWidth,
            1f - (y + h * 0.5f) / CanvasSourceHeight);
        rect.anchoredPosition += Vector2.Scale(contentPivot - rect.pivot, rect.rect.size);
        rect.pivot = contentPivot;
        state.centeredPosition = rect.anchoredPosition;

        if (animatedOpacity)
        {
            state.alpha = rect.GetComponent<CanvasGroup>();
            if (state.alpha == null)
            {
                state.alpha = rect.gameObject.AddComponent<CanvasGroup>();
                state.createdAlpha = true;
            }
            state.originalAlpha = state.alpha.alpha;
        }
        return state;
    }

    private static float Wave(float time, float cycle, float offset)
    {
        return Mathf.Sin(Tau * time / cycle + offset);
    }

    private void Update()
    {
        if (!ready) return;
        bool active = pointerInside || pointerHeld;
        float previousBlend = hoverBlend;
        hoverBlend = Mathf.MoveTowards(hoverBlend, active ? 1f : 0f,
            Time.unscaledDeltaTime / (active ? hoverFadeInSeconds : hoverSettleSeconds));

        // Stop advancing the magic immediately on pointer exit. Only a short
        // neutral-pose settle remains; at rest no animation work is performed.
        if (active) animationTime += Time.unscaledDeltaTime;
        if (hoverBlend <= 0f && previousBlend <= 0f) return;
        float t = animationTime;
        float pixelScale = bookLayer.rect.rect.width / CanvasSourceWidth;

        float auraWave = Wave(t, auraCycleSeconds, 0.45f);
        auraLayer.rect.localScale = auraLayer.originalScale * (1f + hoverBlend * auraScalePulse * auraWave);
        // Alpha pulses remain independent of the pressed-state tint's Graphic.color.
        auraLayer.alpha.alpha = auraLayer.originalAlpha * Mathf.Lerp(
            1f, 0.89f + 0.11f * (0.5f + 0.5f * auraWave), hoverBlend);

        float bookWave = Wave(t, bookCycleSeconds, 0.15f);
        bookLayer.rect.anchoredPosition = bookLayer.centeredPosition +
            new Vector2(0f, hoverBlend * bookBobPixels * pixelScale * bookWave);
        bookLayer.rect.localRotation = bookLayer.originalRotation *
            Quaternion.Euler(0f, 0f, hoverBlend * bookTiltDegrees *
                Wave(t, bookCycleSeconds * 1.38f, 1.4f));

        // A shallow orbital tilt: the thin perspective rings never stand vertically
        // or clip through the card's outer gold bevel.
        ringsLayer.rect.localRotation = ringsLayer.originalRotation *
            Quaternion.Euler(0f, 0f, hoverBlend * ringsTiltDegrees *
                Wave(t, ringsCycleSeconds, 0.8f));
        ringsLayer.rect.localScale = ringsLayer.originalScale *
            (1f + hoverBlend * 0.012f * Wave(t, ringsCycleSeconds * 0.8f, 0.35f));

        float starWave = Wave(t, starCycleSeconds, 1.0f);
        starLayer.rect.localScale = starLayer.originalScale * (1f + hoverBlend * starScalePulse * starWave);
        starLayer.rect.anchoredPosition = starLayer.centeredPosition +
            new Vector2(0f, hoverBlend * 1.4f * pixelScale *
                Wave(t, starCycleSeconds * 1.2f, 0.5f));
        starLayer.alpha.alpha = starLayer.originalAlpha * Mathf.Lerp(
            1f, 0.86f + 0.14f * (0.5f + 0.5f * starWave), hoverBlend);
    }

    private void OnDisable()
    {
        ready = false;
        pointerInside = pointerHeld = false;
        hoverBlend = 0f;
        Restore(auraLayer);
        Restore(bookLayer);
        Restore(ringsLayer);
        Restore(starLayer);
        auraLayer = bookLayer = ringsLayer = starLayer = null;
    }

    private static void Restore(Layer state)
    {
        if (state == null || state.rect == null) return;
        state.rect.localRotation = state.originalRotation;
        state.rect.localScale = state.originalScale;
        state.rect.pivot = state.originalPivot;
        state.rect.anchoredPosition = state.originalPosition;
        if (state.alpha == null) return;
        state.alpha.alpha = state.originalAlpha;
        if (state.createdAlpha) Destroy(state.alpha);
    }
}
