using System;
using UnityEngine;


/// <summary>Rest-relative tent ambience, with stronger but localized visual motion.</summary>
[DisallowMultipleComponent]
public sealed class ModeSelectBackgroundAmbientMotion : MonoBehaviour
{
    public enum LayerKind { Lantern, Flame, Glow }

    [Serializable]
    public sealed class Layer
    {
        public RectTransform target;
        public UnityEngine.UI.Image image;
        public LayerKind kind;
        [Tooltip("Deterministic phase offset, in cycles.")]
        public float phase;
        [Tooltip("Main cycle in seconds; lanterns typically use 8–11 seconds.")]
        public float period = 9f;
        [Tooltip("Maximum main rotation in degrees. Keep the lantern swing under 5 degrees to preserve repaired-plate fidelity.")]
        public float swayDegrees = 2f;
        [Tooltip("Flame width variation as a fraction of its rest width.")]
        public float widthVariation = 0.015f;
        [Tooltip("Flame height variation as a fraction of its rest height.")]
        public float heightVariation = 0.04f;
        [Tooltip("Glow alpha variation as a fraction of its rest alpha.")]
        public float alphaVariation = 0.12f;
        [Tooltip("Lantern brightness variation as a fraction of its rest color; paired with localized glass glow.")]
        public float brightnessVariation = 0.03f;
        [Tooltip("Lantern brightness noise speed, independent of sway period.")]
        public float flickerSpeed = 0.65f;
    }

    [Serializable]
    public sealed class ChainLink
    {
        public RectTransform target;
        public RectTransform attachment;
        public Vector3 attachmentLocalPoint;
    }

    private struct ChainRest
    {
        public RectTransform target;
        public RectTransform attachment;
        public Transform parent;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public Vector3 baseVector;
    }

    private struct RestPose
    {
        public RectTransform target;
        public UnityEngine.UI.Image image;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public Color color;
    }

    [SerializeField] private Layer[] layers = Array.Empty<Layer>();
    [SerializeField] private ChainLink[] chains = Array.Empty<ChainLink>();
    private RestPose[] rests;
    private ChainRest[] chainRests;
    private bool captured;
    private double epoch;
    private const double TwoPi = Math.PI * 2.0;

    public int LayerCount => layers == null ? 0 : layers.Length;

    private void OnEnable()
    {
        CaptureIfNeeded();
        epoch = Time.unscaledTimeAsDouble;
    }

    private void Update()
    {
        SampleAt(Time.unscaledTimeAsDouble - epoch);
    }

    private void OnDisable()
    {
        Restore();
    }

    /// <summary>Restores old bindings and captures the current poses of the new bindings.</summary>
    public void Configure(Layer[] bindings) => Configure(bindings, null);

    public void Configure(Layer[] bindings, ChainLink[] chainBindings)
    {
        Restore();
        layers = bindings ?? Array.Empty<Layer>();
        chains = chainBindings ?? Array.Empty<ChainLink>();
        captured = false;
        CaptureIfNeeded();
        epoch = Time.unscaledTimeAsDouble;
    }

    /// <summary>Restore and release before changing the layout, then configure again.</summary>
    public void RestoreAndClearBindings()
    {
        Restore();
        layers = Array.Empty<Layer>();
        rests = null;
        chains = Array.Empty<ChainLink>();
        chainRests = null;
        captured = false;
    }

    /// <summary>Applies the deterministic rest-relative pose at the given elapsed seconds.</summary>
    public void SampleAt(double elapsedSeconds)
    {
        CaptureIfNeeded();
        if (double.IsNaN(elapsedSeconds) || double.IsInfinity(elapsedSeconds))
            elapsedSeconds = 0.0;
        if (layers == null || rests == null)
            return;

        int count = Math.Min(layers.Length, rests.Length);
        for (int i = 0; i < count; i++)
        {
            Layer layer = layers[i];
            RestPose rest = rests[i];
            if (layer == null || !rest.target || layer.target != rest.target || layer.image != rest.image)
                continue;

            double period = Finite(layer.period) && layer.period > 0f ? layer.period : 9.0;
            double phase = Finite(layer.phase) ? layer.phase : 0.0;
            double cycles = elapsedSeconds / period + phase;
            float sway = Value(layer.swayDegrees, 0f);
            // Two low-frequency waves prevent a perfectly repeating pendulum.
            float primary = (float)Math.Sin((cycles % 1.0) * TwoPi);
            float secondary = (float)Math.Sin(((cycles * 0.617 + 0.31) % 1.0) * TwoPi);
            float noise = Noise(cycles * 0.58 + phase * 3.17);
            float noise2 = Noise(cycles * 0.71 + phase * 5.29 + 13.7);

            if (layer.kind == LayerKind.Lantern)
            {
                float degrees = Mathf.Clamp(sway, -5f, 5f);
                rest.target.localRotation = rest.rotation * Quaternion.Euler(0f, 0f, degrees * (0.82f * primary + 0.18f * secondary));
                if (rest.image)
                {
                    float brightnessNoise = Noise(elapsedSeconds * Value(layer.flickerSpeed, 0.65f) + phase * 3.17);
                    float brightness = 1f - Mathf.Clamp(Value(layer.brightnessVariation, 0f), 0f, 0.12f) * brightnessNoise;
                    rest.image.color = new Color(rest.color.r * brightness, rest.color.g * brightness, rest.color.b * brightness, rest.color.a);
                }
            }
            else if (layer.kind == LayerKind.Flame)
            {
                // A visible, low-frequency gust controls the lateral lean.
                // Pure Perlin noise stayed near its midpoint and barely moved
                // the old candle artwork despite high configured amplitudes.
                // Keep the flame pivot attached to the original wick base.
                float gust = (float)Math.Sin((cycles % 1.0) * TwoPi);
                float lean = Mathf.Clamp(0.78f * gust + 0.22f * (noise * 2f - 1f), -1f, 1f);
                rest.target.localRotation = rest.rotation * Quaternion.Euler(0f, 0f, Mathf.Clamp(sway, -8f, 8f) * lean);
                float width = 1f + Value(layer.widthVariation, 0f) *
                    (0.65f * (noise2 * 2f - 1f) + 0.35f * gust);
                float height = 1f + Value(layer.heightVariation, 0f) *
                    (0.65f * (noise * 2f - 1f) + 0.35f * gust);
                rest.target.localScale = Vector3.Scale(rest.scale, new Vector3(width, height, 1f));
            }
            else if (layer.kind == LayerKind.Glow && rest.image)
            {
                // Irregular flicker plus a slow, soft pulse. The localized
                // overlay is stronger, never a full-screen flashing layer.
                float pulse = (float)Math.Sin(elapsedSeconds * TwoPi / period + phase * TwoPi);
                float fluctuation = Mathf.Clamp(0.72f * (noise * 2f - 1f) + 0.28f * pulse, -1f, 1f);
                float alpha = rest.color.a * (1f + Mathf.Clamp(Value(layer.alphaVariation, 0f), 0f, 0.9f) * fluctuation);
                rest.image.color = new Color(rest.color.r, rest.color.g, rest.color.b, Mathf.Clamp01(alpha));
            }
        }
        ApplyChains();
    }

    private void CaptureIfNeeded()
    {
        if (captured)
            return;
        int count = layers == null ? 0 : layers.Length;
        rests = new RestPose[count];
        for (int i = 0; i < count; i++)
        {
            Layer layer = layers[i];
            if (layer == null || !layer.target)
                continue;
            rests[i] = new RestPose
            {
                target = layer.target,
                image = layer.image,
                position = layer.target.localPosition,
                rotation = layer.target.localRotation,
                scale = layer.target.localScale,
                color = layer.image ? layer.image.color : Color.white
            };
        }
        int chainCount = chains == null ? 0 : chains.Length;
        chainRests = new ChainRest[chainCount];
        for (int i = 0; i < chainCount; i++)
        {
            ChainLink chain = chains[i];
            if (chain == null || !chain.target || !chain.target.parent || !chain.attachment)
                continue;
            Transform parent = chain.target.parent;
            chainRests[i] = new ChainRest
            {
                target = chain.target, attachment = chain.attachment, parent = parent,
                position = chain.target.localPosition, rotation = chain.target.localRotation,
                scale = chain.target.localScale,
                baseVector = parent.InverseTransformPoint(chain.attachment.TransformPoint(chain.attachmentLocalPoint)) - chain.target.localPosition
            };
        }
        captured = true;
    }

    private void ApplyChains()
    {
        if (chains == null || chainRests == null)
            return;
        int count = Math.Min(chains.Length, chainRests.Length);
        for (int i = 0; i < count; i++)
        {
            ChainLink chain = chains[i];
            ChainRest rest = chainRests[i];
            if (chain == null || !rest.target || !rest.attachment || !rest.parent ||
                chain.target != rest.target || chain.attachment != rest.attachment || rest.target.parent != rest.parent)
                continue;
            Vector3 desired = rest.parent.InverseTransformPoint(rest.attachment.TransformPoint(chain.attachmentLocalPoint)) - rest.position;
            float baseLength = rest.baseVector.magnitude;
            float length = desired.magnitude;
            if (baseLength <= 0.00001f || length <= 0.00001f)
                continue;
            float delta = (Mathf.Atan2(desired.y, desired.x) - Mathf.Atan2(rest.baseVector.y, rest.baseVector.x)) * Mathf.Rad2Deg;
            rest.target.localPosition = rest.position;
            rest.target.localRotation = rest.rotation * Quaternion.Euler(0f, 0f, delta);
            rest.target.localScale = rest.scale * (length / baseLength);
        }
    }

    private void Restore()
    {
        if (!captured || rests == null)
            return;
        if (chainRests != null)
        {
            for (int i = 0; i < chainRests.Length; i++)
            {
                ChainRest chain = chainRests[i];
                if (!chain.target) continue;
                chain.target.localPosition = chain.position;
                chain.target.localRotation = chain.rotation;
                chain.target.localScale = chain.scale;
            }
        }
        for (int i = 0; i < rests.Length; i++)
        {
            RestPose rest = rests[i];
            if (rest.target)
            {
                rest.target.localPosition = rest.position;
                rest.target.localRotation = rest.rotation;
                rest.target.localScale = rest.scale;
            }
            if (rest.image)
                rest.image.color = rest.color;
        }
    }

    // Offset inputs into the positive range; modulo keeps long sessions numerically stable.
    private static float Noise(double input)
    {
        double wrapped = input % 10000.0;
        if (wrapped < 0.0)
            wrapped += 10000.0;
        return Mathf.Clamp01(Mathf.PerlinNoise((float)wrapped, 27.13f));
    }

    private static float Value(float value, float fallback) => Finite(value) ? value : fallback;
    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
