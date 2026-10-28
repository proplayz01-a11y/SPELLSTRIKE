using System.Globalization;
using TMPro;
using UnityEngine;

/// <summary>
/// Presentation-only floating damage feedback. Created on the first confirmed
/// enemy HP loss; one camera-space Canvas and a bounded, lazily grown pool of
/// TMP labels serve all enemies in the scene. Nothing here changes hit authority.
///
/// V01 deliberately animates UI text in C# instead of using a particle/VFX
/// Graph: each glyph string is different and is owned by TextMesh Pro.
/// </summary>
public sealed class FloatingDamageNumbersV01 : MonoBehaviour
{
    private const int PoolSize = 20;
    private const float LifetimeSeconds = 0.85f;
    private const float RisePixels = 76f;
    private static readonly Color NumberColor = new Color(1f, 0.86f, 0.55f, 1f);

    private sealed class Slot
    {
        public RectTransform Rect;
        public TextMeshProUGUI Label;
        public Vector2 Origin;
        public float Age;
        public bool Active;
    }

    private static FloatingDamageNumbersV01 instance;
    private readonly Slot[] slots = new Slot[PoolSize];
    private RectTransform overlayRoot;
    private TMP_FontAsset labelFont;
    private int nextSlot;

    /// <summary>
    /// Call ONLY after a health owner has actually reduced the enemy's HP.
    /// The amount should be beforeHP - afterHP, never the requested damage.
    /// </summary>
    public static void Show(Component enemy, float confirmedLoss)
    {
        if (!Application.isPlaying || enemy == null || confirmedLoss <= 0f ||
            float.IsNaN(confirmedLoss) || float.IsInfinity(confirmedLoss))
            return;

        Camera viewCamera = Camera.main;
        if (viewCamera == null || !viewCamera.isActiveAndEnabled)
            return;

        Vector3 screen = viewCamera.WorldToScreenPoint(ResolveAnchor(enemy));
        // Do not clamp offscreen/behind-camera damage into an unrelated HUD area.
        if (screen.z <= viewCamera.nearClipPlane ||
            screen.x < 0f || screen.x > Screen.width ||
            screen.y < 0f || screen.y > Screen.height)
            return;

        if (instance == null)
        {
            var root = new GameObject(
                "FloatingDamageNumbersV01_Runtime", typeof(RectTransform), typeof(Canvas));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            instance = root.AddComponent<FloatingDamageNumbersV01>();
            instance.Initialize(root.GetComponent<RectTransform>());
        }

        instance.Emit(confirmedLoss, new Vector2(screen.x, screen.y));
    }

    private static Vector3 ResolveAnchor(Component enemy)
    {
        // Sample once at impact, not in Update. Giant model roots and character
        // pivots are often at the feet, so prefer the actual character render.
        Renderer model = enemy.GetComponentInChildren<SkinnedMeshRenderer>();
        if (model == null)
            model = enemy.GetComponentInChildren<MeshRenderer>();

        if (model != null && model.bounds.size.y > 0.01f)
        {
            Bounds bounds = model.bounds;
            return bounds.center + Vector3.up * (bounds.extents.y * 0.35f);
        }

        Collider hitBody = enemy.GetComponent<Collider>();
        if (hitBody == null)
            hitBody = enemy.GetComponentInChildren<Collider>();
        if (hitBody != null)
            return hitBody.bounds.center + Vector3.up * (hitBody.bounds.extents.y * 0.35f);

        return enemy.transform.position + Vector3.up * 1.5f;
    }

    private void Initialize(RectTransform root)
    {
        overlayRoot = root;
        labelFont = TMP_Settings.defaultFontAsset;
        if (labelFont == null)
            labelFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
    }

    private Slot CreateSlot(int index)
    {
        // Allocate only as many labels as concurrent hits need. In particular,
        // the first hit should not instantiate all twenty TMP objects on A05s.
        var labelObject = new GameObject(
            "Damage_" + index, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.SetParent(overlayRoot, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(152f, 70f);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        if (labelFont != null)
            label.font = labelFont;
        label.fontSize = 38f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.enableAutoSizing = false;
        label.color = NumberColor;
        label.text = string.Empty;

        labelObject.SetActive(false);
        return new Slot { Rect = rect, Label = label };
    }

    private void Emit(float loss, Vector2 screen)
    {
        // Reuse an inactive label *before* growing the pool; otherwise even
        // widely spaced hits would eventually allocate all twenty labels.
        int ordinal = -1;
        int unused = -1;
        for (int i = 0; i < PoolSize; i++)
        {
            int index = (nextSlot + i) % PoolSize;
            Slot candidate = slots[index];
            if (candidate != null && !candidate.Active)
            {
                ordinal = index;
                break;
            }
            if (candidate == null && unused < 0)
                unused = index;
        }
        if (ordinal < 0)
            ordinal = unused >= 0 ? unused : nextSlot;

        Slot slot = slots[ordinal];
        if (slot == null)
        {
            slot = CreateSlot(ordinal);
            slots[ordinal] = slot;
        }
        nextSlot = (ordinal + 1) % PoolSize;

        // No scene Camera/Renderer references are retained after the hit.
        slot.Origin = screen -
            new Vector2(Screen.width * 0.5f, Screen.height * 0.5f) +
            new Vector2(((ordinal % 5) - 2) * 13f, (ordinal % 3) * 7f);
        slot.Age = 0f;
        slot.Active = true;
        slot.Label.text = loss.ToString("0.##", CultureInfo.InvariantCulture);
        slot.Label.color = NumberColor;
        slot.Rect.anchoredPosition = slot.Origin;
        slot.Rect.gameObject.SetActive(true);
    }

    private void Update()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            Slot slot = slots[i];
            if (slot == null || !slot.Active)
                continue;

            slot.Age += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(slot.Age / LifetimeSeconds);
            if (progress >= 1f)
            {
                slot.Active = false;
                slot.Rect.gameObject.SetActive(false);
                continue;
            }

            slot.Rect.anchoredPosition = slot.Origin + Vector2.up * (RisePixels * progress);
            float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 1f, progress));
            slot.Label.color = new Color(NumberColor.r, NumberColor.g, NumberColor.b, fade);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
