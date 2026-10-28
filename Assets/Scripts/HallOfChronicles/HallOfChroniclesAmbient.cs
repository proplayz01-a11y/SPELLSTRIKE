using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Noninteractive ambience for the isolated Hall of Chronicles preview.
/// v2: visibly dancing flames and localized light variation, with wick positions fixed.
/// Does not own Stage Select navigation, book art, or persistence.
/// </summary>
[DisallowMultipleComponent]
public sealed class HallOfChroniclesAmbient : MonoBehaviour
{
    [Serializable]
    public sealed class Layer
    {
        public Image image;
        public float minAlpha = 1f;
        public float maxAlpha = 1f;
        public float minScale = 1f;
        public float maxScale = 1f;
        public float periodSeconds = 3f;
        public float phaseRadians;
    }

    [SerializeField] private bool animate = false;
    [SerializeField] private GameObject visibilityGuard;
    [SerializeField] private List<Layer> layers = new List<Layer>();

    private Canvas owningCanvas;
    private float elapsed;
    private bool suspended;
    private readonly Dictionary<Image, Image> flameAccents = new Dictionary<Image, Image>();

    // The original source-sprite alpha is faint, especially on the little candles.
    // Stronger movement plus a tiny additional pass of each NEAR flame makes the
    // ambience readable even when Game View is scaled down for a phone preview.
    private const float NearFlameMinAlpha = 0.49f;
    private const float NearFlameMinHeight = 0.84f;
    private const float NearFlameMaxHeight = 1.18f;
    private const float NearFlameMaxRotation = 4.5f;

    public bool IsAnimating => animate;
    public int LayerCount => layers.Count;

    private void Awake()
    {
        owningCanvas = GetComponentInParent<Canvas>();
        CreateNearFlameAccents();
        RestoreManifestDefaults();

        // Separate runtime-only dust layer. No prefab, scene or book edits
        // required: uses the existing golden dust artwork via Resources.
        HallOfChroniclesFloatingDust dust = GetComponent<HallOfChroniclesFloatingDust>();
        if (dust == null)
            dust = gameObject.AddComponent<HallOfChroniclesFloatingDust>();
        dust.Configure(this, visibilityGuard);
    }

    private void OnEnable()
    {
        elapsed = 0f;
        suspended = false;
        RestoreManifestDefaults();
    }

    private void OnDisable()
    {
        RestoreManifestDefaults();
    }

    private void Update()
    {
        if (!animate)
            return;

        // The animated room is presentation-only. Never keep updating a hidden screen.
        bool visible = (visibilityGuard == null || visibilityGuard.activeInHierarchy)
            && (owningCanvas == null || owningCanvas.isActiveAndEnabled);
        if (!visible)
        {
            if (!suspended)
                RestoreManifestDefaults();
            suspended = true;
            return;
        }

        suspended = false;
        elapsed += Time.unscaledDeltaTime;
        for (int i = 0; i < layers.Count; i++)
        {
            Layer layer = layers[i];
            if (layer.image == null)
                continue;
            float period = Mathf.Max(0.25f, layer.periodSeconds);
            float omega = Mathf.PI * 2f / period;
            float slow = 0.5f + 0.5f * Mathf.Sin(elapsed * omega + layer.phaseRadians);
            float fast = 0.5f + 0.5f * Mathf.Sin(elapsed * omega * 3.1f + layer.phaseRadians * 1.47f);
            float flutter = Mathf.PerlinNoise(layer.phaseRadians * 1.83f + 11f, elapsed * 2.1f);
            float wave = Mathf.Clamp01(slow * 0.45f + fast * 0.37f + flutter * 0.18f);
            Apply(layer, wave, slow, fast);
        }
    }

    private void Apply(Layer layer, float t, float slow, float fast)
    {
        bool isFlame = layer.image.name.Contains("_flame_");
        bool nearFlame = isFlame && layer.maxScale > layer.minScale + 0.001f;
        float minAlpha = isFlame ? (nearFlame ? NearFlameMinAlpha : 0.62f) : 0.42f;
        // Preserves the original sprite/manifest brightness as the normal peak.
        float alpha = Mathf.Lerp(minAlpha, 1f, Mathf.Clamp01(t));
        float scale = nearFlame ? Mathf.Lerp(NearFlameMinHeight, NearFlameMaxHeight, t) : 1f;
        layer.image.color = new Color(1f, 1f, 1f, alpha);
        // The builder already sets a wick-centered pivot for near flames.
        // A tiny rotation / asymmetric scaling leaves that wick exactly fixed.
        layer.image.rectTransform.localScale = nearFlame
            ? new Vector3(Mathf.Lerp(0.95f, 1.07f, fast), scale, 1f)
            : Vector3.one;
        layer.image.rectTransform.localRotation = nearFlame
            ? Quaternion.Euler(0f, 0f, (slow - 0.5f) * 2f * NearFlameMaxRotation)
            : Quaternion.identity;

        Image accent;
        if (flameAccents.TryGetValue(layer.image, out accent) && accent != null)
        {
            // Normal alpha blend, not an additive/bloom material.
            // Only prominent near candle flames get this local highlight.
            accent.color = new Color(1f, 0.88f, 0.66f,
                Mathf.Lerp(0.02f, 0.46f, Mathf.Pow(t, 1.6f)));
        }
    }

    private void CreateNearFlameAccents()
    {
        foreach (Layer layer in layers)
        {
            if (layer == null || layer.image == null ||
                !layer.image.name.Contains("_flame_") ||
                layer.maxScale <= layer.minScale + 0.001f)
                continue;

            var go = new GameObject("NearFlameFlickerAccent",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(layer.image.rectTransform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = layer.image.rectTransform.pivot;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            var accent = go.GetComponent<Image>();
            accent.sprite = layer.image.sprite;
            accent.type = Image.Type.Simple;
            accent.raycastTarget = false;
            accent.color = new Color(1f, 0.88f, 0.66f, 0f);
            flameAccents.Add(layer.image, accent);
        }
    }

    public void RestoreManifestDefaults()
    {
        for (int i = 0; i < layers.Count; i++)
        {
            Layer layer = layers[i];
            if (layer.image == null)
                continue;
            layer.image.color = Color.white;
            layer.image.rectTransform.localScale = Vector3.one;
            layer.image.rectTransform.localRotation = Quaternion.identity;
        }
        foreach (Image accent in flameAccents.Values)
            if (accent != null)
                accent.color = new Color(1f, 0.88f, 0.66f, 0f);
    }

#if UNITY_EDITOR
    // Blue archway is occluded by the approved Stage Select book. Remove the
    // hidden object and its update binding, leaving the pack asset unchanged.
    public bool RetireOccludedBlueArchway()
    {
        bool removed = false;
        for (int i = layers.Count - 1; i >= 0; i--)
        {
            Image image = layers[i].image;
            if (image == null || !image.name.EndsWith("_glow_blue_archway", StringComparison.Ordinal))
                continue;
            image.gameObject.SetActive(false);
            layers.RemoveAt(i);
            removed = true;
        }
        return removed;
    }

    // Build-time inspector configuration, never used by live gameplay managers.
    public void Configure(GameObject bookRoot, List<Layer> configuredLayers)
    {
        visibilityGuard = bookRoot;
        animate = false;
        layers = configuredLayers ?? new List<Layer>();
        RestoreManifestDefaults();
    }

    public void SetAnimationPreviewEnabled(bool enabled)
    {
        animate = enabled;
        elapsed = 0f;
        RestoreManifestDefaults();
    }

    public void PreviewBound(bool maximum)
    {
        for (int i = 0; i < layers.Count; i++)
            if (layers[i].image != null)
                Apply(layers[i], maximum ? 1f : 0f,
                    maximum ? 1f : 0f, maximum ? 1f : 0f);
    }
#endif
}
