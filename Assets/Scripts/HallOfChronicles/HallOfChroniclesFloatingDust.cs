using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lightweight atmospheric dust for the isolated Hall of Chronicles backdrop.
/// Uses the actual imported golden dust texture; no particle materials, physics,
/// camera dependencies, input interception, gameplay ownership or prefabs.
/// Particles are positioned in the exposed side margins around the sharp book.
/// </summary>
[DisallowMultipleComponent]
public sealed class HallOfChroniclesFloatingDust : MonoBehaviour
{
    private const string SpriteResource = "HallOfChronicles/particle_dust_soft";
    private const int ParticleCount = 36;
    private const float Bottom = -485f;
    private const float Top = 490f;

    private sealed class Mote
    {
        public RectTransform rect;
        public Image image;
        public bool isLeft;
        public float centerX;
        public float y;
        public float riseSpeed;
        public float swayRange;
        public float swaySpeed;
        public float phase;
        public float glowPhase;
        public float alpha;
    }

    private readonly System.Random random = new System.Random(4871);
    private readonly Mote[] motes = new Mote[ParticleCount];
    private HallOfChroniclesAmbient ambient;
    private GameObject visibilityGuard;
    private GameObject dustRoot;
    private float elapsed;
    private bool initialized;

    public void Configure(HallOfChroniclesAmbient ambientOwner, GameObject bookRoot)
    {
        ambient = ambientOwner;
        visibilityGuard = bookRoot;
        if (initialized)
            return;

        Sprite sprite = Resources.Load<Sprite>(SpriteResource);
        if (sprite == null)
        {
            Debug.LogWarning("[HallOfChronicles] Golden dust sprite unavailable in Resources. Background remains functional.");
            return;
        }

        RectTransform root = transform as RectTransform;
        if (root == null)
            return;

        dustRoot = new GameObject("AtmosphericGoldenDust", typeof(RectTransform));
        RectTransform dustRect = dustRoot.GetComponent<RectTransform>();
        dustRect.SetParent(root, false);
        dustRect.anchorMin = dustRect.anchorMax = new Vector2(0.5f, 0.5f);
        dustRect.sizeDelta = new Vector2(2400f, 1080f);
        dustRect.anchoredPosition = Vector2.zero;
        dustRect.localScale = Vector3.one;
        dustRect.SetAsLastSibling();

        // A 64x64 image with a ~6px bright center needs an enlarged UI quad for
        // the golden core to remain perceptible in a downscaled mobile Game View.
        for (int i = 0; i < motes.Length; i++)
        {
            var go = new GameObject("GoldenDust_" + i.ToString("00"),
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(dustRect, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            float quadSize = Lerp(76f, 137f, Next01());
            rect.sizeDelta = new Vector2(quadSize, quadSize);
            rect.localScale = Vector3.one;

            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = new Color(1f, 0.91f, 0.72f, 0f);

            Mote mote = new Mote
            {
                rect = rect,
                image = image,
                isLeft = (i % 2 == 0),
                y = Lerp(-445f, 420f, Next01()),
                riseSpeed = Lerp(13f, 29f, Next01()),
                swayRange = Lerp(9f, 27f, Next01()),
                swaySpeed = Lerp(0.45f, 0.9f, Next01()),
                phase = Next01() * Mathf.PI * 2f,
                glowPhase = Next01() * Mathf.PI * 2f,
                alpha = Lerp(0.51f, 0.87f, Next01())
            };
            RespawnX(mote);
            motes[i] = mote;
        }

        initialized = true;
        dustRoot.SetActive(false);
    }

    private void Update()
    {
        if (!initialized)
            return;

        bool shouldShow = ambient != null && ambient.IsAnimating
            && (visibilityGuard == null || visibilityGuard.activeInHierarchy);
        if (!shouldShow)
        {
            if (dustRoot.activeSelf)
                dustRoot.SetActive(false);
            return;
        }

        if (!dustRoot.activeSelf)
            dustRoot.SetActive(true);

        float delta = Mathf.Min(Time.unscaledDeltaTime, 0.08f);
        elapsed += delta;

        for (int i = 0; i < motes.Length; i++)
        {
            Mote mote = motes[i];
            mote.y += mote.riseSpeed * delta;
            if (mote.y > Top)
            {
                mote.y = Bottom + Lerp(0f, 75f, Next01());
                RespawnX(mote);
            }

            float x = mote.centerX +
                Mathf.Sin(elapsed * mote.swaySpeed + mote.phase) * mote.swayRange +
                Mathf.Sin(elapsed * 0.31f + mote.phase * 2.1f) * 7f;
            mote.rect.anchoredPosition = new Vector2(x, mote.y);

            // Soft fade at the bottom/top, plus slow shimmer independent per mote.
            float fadeBottom = Mathf.Clamp01((mote.y - Bottom) / 90f);
            float fadeTop = Mathf.Clamp01((Top - mote.y) / 100f);
            float shimmer = 0.79f + 0.21f *
                Mathf.Sin(elapsed * 1.1f + mote.glowPhase);
            float alpha = mote.alpha * shimmer * fadeBottom * fadeTop;
            mote.image.color = new Color(1f, 0.91f, 0.72f, alpha);
        }
    }

    private void RespawnX(Mote mote)
    {
        // These lanes remain visible at 2400x1080 and largely visible when the
        // environment cover-crops horizontally to 16:9. The book always renders
        // above this decorative Canvas.
        mote.centerX = mote.isLeft
            ? Lerp(-970f, -665f, Next01())
            : Lerp(665f, 970f, Next01());
    }

    private float Next01() => (float)random.NextDouble();
    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
