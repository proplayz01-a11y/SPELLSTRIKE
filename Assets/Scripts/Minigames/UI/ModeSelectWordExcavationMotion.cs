using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Hover-only, lightweight animation for Word Excavation's independently
/// separated illustration. Never changes the static card frame, baked text,
/// reflection mask, button hitbox, or navigation.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class ModeSelectWordExcavationMotion : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Approved separated artwork")]
    [SerializeField] private RectTransform excavationGlow;
    [SerializeField] private RectTransform rocksAndLetter;
    [SerializeField] private RectTransform pickaxe;
    [SerializeField] private RectTransform sparkles;

    [Header("Hover transition")]
    [SerializeField, Min(0.01f)] private float fadeInSeconds = 0.16f;
    [SerializeField, Min(0.01f)] private float settleSeconds = 0.20f;

    [Header("Restrained magical motion")]
    [SerializeField, Range(0f, 0.08f)] private float glowScalePulse = 0.028f;
    [SerializeField, Min(1f)] private float glowPeriod = 3.4f;
    [SerializeField, Range(0f, 5f)] private float pickaxeRockDegrees = 2.5f;
    [SerializeField, Range(0f, 5f)] private float pickaxeBobPixels = 1.7f;
    [SerializeField, Min(1f)] private float pickaxePeriod = 3.0f;
    [SerializeField, Range(0f, 2f)] private float rocksParallaxPixels = 0.55f;
    [SerializeField, Min(1f)] private float rocksPeriod = 4.4f;
    [SerializeField, Range(0f, 5f)] private float sparklesDriftPixels = 2.2f;
    [SerializeField, Range(0f, 0.1f)] private float sparklesScalePulse = 0.035f;
    [SerializeField, Min(1f)] private float sparklesPeriod = 2.7f;

    private const float SourceWidth = 872f;
    private const float SourceHeight = 172f;
    private const float TwoPi = 6.28318530718f;

    private sealed class Layer
    {
        public RectTransform rect;
        public Vector2 originalPivot;
        public Vector2 originalPosition;
        public Vector2 contentPosition;
        public Vector3 originalScale;
        public Quaternion originalRotation;
        public CanvasGroup canvasGroup;
        public float originalAlpha;
        public bool createdCanvasGroup;
    }

    private Layer glowLayer;
    private Layer rocksLayer;
    private Layer pickaxeLayer;
    private Layer sparkleLayer;
    private bool ready;
    private bool pointerInside;
    private bool pointerHeld;
    private float animatedTime;
    private float blend;

    private void OnEnable()
    {
        if (!Application.isPlaying) return;

        if (excavationGlow == null) excavationGlow = FindLayer("WE_03_MagicalGlow");
        if (rocksAndLetter == null) rocksAndLetter = FindLayer("WE_05_RocksAndLetter");
        if (pickaxe == null) pickaxe = FindLayer("WE_04_Pickaxe");
        if (sparkles == null) sparkles = FindLayer("WE_06_Sparkles");

        if (excavationGlow == null || rocksAndLetter == null ||
            pickaxe == null || sparkles == null)
        {
            Debug.LogWarning("[Mode Select Word Excavation] Missing approved illustration layers.", this);
            return;
        }

        // Alpha bounds from the approved asset separation manifest. The images
        // share a padded 872x172 canvas, so normal (0.5, 0.5) pivots would rotate
        // the entire card instead of the actual axe, stones and magic.
        glowLayer = Capture(excavationGlow, 30f, 25f, 244f, 115f, true);
        rocksLayer = Capture(rocksAndLetter, 47f, 47f, 227f, 94f, false);
        pickaxeLayer = Capture(pickaxe, 77f, 15f, 126f, 121f, false);
        sparkleLayer = Capture(sparkles, 84f, 26f, 148f, 61f, true);
        pointerInside = pointerHeld = false;
        animatedTime = blend = 0f;
        ready = true;
    }

    private RectTransform FindLayer(string name)
    {
        Transform child = transform.Find(name);
        return child == null ? null : child as RectTransform;
    }

    private static Layer Capture(RectTransform rect, float x, float y,
        float width, float height, bool needsAlpha)
    {
        var layer = new Layer
        {
            rect = rect,
            originalPivot = rect.pivot,
            originalPosition = rect.anchoredPosition,
            originalScale = rect.localScale,
            originalRotation = rect.localRotation
        };

        Vector2 pivot = new Vector2((x + width * 0.5f) / SourceWidth,
            1f - (y + height * 0.5f) / SourceHeight);
        rect.anchoredPosition += Vector2.Scale(pivot - rect.pivot, rect.rect.size);
        rect.pivot = pivot;
        layer.contentPosition = rect.anchoredPosition;

        if (needsAlpha)
        {
            layer.canvasGroup = rect.GetComponent<CanvasGroup>();
            if (layer.canvasGroup == null)
            {
                layer.canvasGroup = rect.gameObject.AddComponent<CanvasGroup>();
                layer.createdCanvasGroup = true;
            }
            layer.originalAlpha = layer.canvasGroup.alpha;
        }
        return layer;
    }

    public void OnPointerEnter(PointerEventData eventData) { pointerInside = true; }
    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        pointerHeld = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData != null && eventData.button == PointerEventData.InputButton.Left)
            pointerHeld = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pointerHeld = false;
        // A tap has no sustained hover on Android.
        if (Application.isMobilePlatform && !Application.isEditor)
            pointerInside = false;
    }

    private static float Wave(float t, float period, float offset)
    {
        return Mathf.Sin(TwoPi * t / period + offset);
    }

    private void Update()
    {
        if (!ready) return;
        bool hovered = pointerInside || pointerHeld;
        float previousBlend = blend;
        blend = Mathf.MoveTowards(blend, hovered ? 1f : 0f,
            Time.unscaledDeltaTime / (hovered ? fadeInSeconds : settleSeconds));

        // On exit the animation clock stops immediately and the four sprites
        // settle to their unchanged original poses. Idle updates are skipped.
        if (hovered) animatedTime += Time.unscaledDeltaTime;
        if (blend <= 0f && previousBlend <= 0f) return;

        float t = animatedTime;
        float pixelScale = pickaxeLayer.rect.rect.width / SourceWidth;

        float glowWave = Wave(t, glowPeriod, 0.4f);
        glowLayer.rect.localScale = glowLayer.originalScale *
            (1f + blend * glowScalePulse * glowWave);
        glowLayer.canvasGroup.alpha = glowLayer.originalAlpha *
            Mathf.Lerp(1f, 0.83f + 0.17f * (0.5f + 0.5f * glowWave), blend);

        // Tiny enchanted levitation without a distracting pickaxe strike.
        float axeWave = Wave(t, pickaxePeriod, 0.2f);
        pickaxeLayer.rect.localRotation = pickaxeLayer.originalRotation *
            Quaternion.Euler(0f, 0f, blend * pickaxeRockDegrees * axeWave);
        pickaxeLayer.rect.anchoredPosition = pickaxeLayer.contentPosition +
            new Vector2(0f, blend * pickaxeBobPixels * pixelScale *
                Wave(t, pickaxePeriod * 1.13f, 1.1f));

        // The carved stone stays nearly fixed, preserving the letter's legibility.
        rocksLayer.rect.anchoredPosition = rocksLayer.contentPosition +
            new Vector2(0f, blend * rocksParallaxPixels * pixelScale *
                Wave(t, rocksPeriod, 1.35f));

        float twinkle = Wave(t, sparklesPeriod, 0.85f);
        sparkleLayer.rect.localScale = sparkleLayer.originalScale *
            (1f + blend * sparklesScalePulse * twinkle);
        sparkleLayer.rect.anchoredPosition = sparkleLayer.contentPosition +
            new Vector2(blend * sparklesDriftPixels * pixelScale *
                Wave(t, sparklesPeriod * 1.35f, 0.25f),
                blend * sparklesDriftPixels * pixelScale *
                Wave(t, sparklesPeriod, 1.9f));
        sparkleLayer.canvasGroup.alpha = sparkleLayer.originalAlpha *
            Mathf.Lerp(1f, 0.73f + 0.27f * (0.5f + 0.5f * twinkle), blend);
    }

    private void OnDisable()
    {
        ready = false;
        pointerInside = pointerHeld = false;
        blend = 0f;
        Restore(glowLayer);
        Restore(rocksLayer);
        Restore(pickaxeLayer);
        Restore(sparkleLayer);
        glowLayer = rocksLayer = pickaxeLayer = sparkleLayer = null;
    }

    private static void Restore(Layer layer)
    {
        if (layer == null || layer.rect == null) return;
        layer.rect.localScale = layer.originalScale;
        layer.rect.localRotation = layer.originalRotation;
        layer.rect.pivot = layer.originalPivot;
        layer.rect.anchoredPosition = layer.originalPosition;

        if (layer.canvasGroup == null) return;
        layer.canvasGroup.alpha = layer.originalAlpha;
        if (layer.createdCanvasGroup) Destroy(layer.canvasGroup);
    }
}
