using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One small camera-facing HP display per *active regular encounter enemy*.
/// Presentation only: reads EnemyHealth and follows the enemy horizontally, never changes
/// enemy damage, AI, hitboxes, scene hierarchy or boss health presentation.
/// Created lazily by EnemyEncounterHUD (or on the first surviving hit for summons).
/// </summary>
[DisallowMultipleComponent]
public sealed class RegularEnemyOverheadHealthBar : MonoBehaviour
{
    // These are TWO independent transparent sprites from the approved
    // compact normal-enemy HUD: fixed frame over a red, HP-driven fill.
    private const string FrameSpritePath = "SpellstrikeHUD/RegularEnemyHP_Frame";
    private const string FillSpritePath = "SpellstrikeHUD/RegularEnemyHP_Fill";
    private const float DisplayWidth = 160f;
    private const float DisplayHeight = 27.25f;
    [SerializeField, Range(65f, 170f)] private float desiredScreenWidthPixels = 118f;
    [SerializeField, Min(0f)] private float verticalClearance = 0.28f;
    [SerializeField, Min(1f)] private float maxViewDistance = 180f;

    private static readonly Color FrameGold = new Color(0.83f, 0.62f, 0.29f, 0.97f);
    private static readonly Color PanelNavy = new Color(0.035f, 0.085f, 0.12f, 0.96f);
    private static readonly Color EmptyTrack = new Color(0.18f, 0.21f, 0.24f, 0.98f);
    private static readonly Color HealthyFill = new Color(0.92f, 0.19f, 0.22f, 1f);
    private static readonly Color CriticalFill = new Color(0.97f, 0.41f, 0.18f, 1f);

    private EnemyHealth health;
    private BrambleSpriteController brambleHealthOwner;
    private Camera viewCamera;
    private GameObject presentation;
    private RectTransform canvasRect;
    private Image hpFill;
    private bool usesApprovedFillSprite;
    private Renderer[] modelRenderers;
    private Collider fallbackCollider;
    private float cachedTopY;
    private float cachedCenterX;
    private float cachedCenterZ;
    private float cachedModelHeight;
    private float fixedWorldHeight;
    private Vector3 centerOffsetLocal;
    private bool anchorCaptured;
    private bool requested;
    private float lastRenderedHealth = float.NaN;
    private float lastRenderedMaximum = float.NaN;

    public static void EnsureVisible(EnemyHealth target)
    {
        if (target == null || target.IsDead || target.UsesBossHud)
            return;

        RegularEnemyOverheadHealthBar display =
            target.GetComponent<RegularEnemyOverheadHealthBar>();
        if (display == null)
            display = target.gameObject.AddComponent<RegularEnemyOverheadHealthBar>();
        display.Show(target);
    }

    private void Show(EnemyHealth target)
    {
        if (health != target)
        {
            Unsubscribe();
            health = target;
            brambleHealthOwner = target.GetComponent<BrambleSpriteController>();
            health.HealthChanged += HandleHealthChanged;
            health.Died += HandleDied;
            anchorCaptured = false;
        }

        requested = true;
        if (presentation == null)
            CreatePresentation();
        modelRenderers = health.GetComponentsInChildren<Renderer>(true);
        fallbackCollider = health.GetComponent<Collider>();
        if (fallbackCollider == null)
            fallbackCollider = health.GetComponentInChildren<Collider>(true);

        if (!anchorCaptured)
            CaptureStableAnchor();
        UpdateHealth(health.currentHealth, GetDisplayMaximumHealth());
        if (presentation != null)
            presentation.SetActive(isActiveAndEnabled && !health.IsDead);
    }

    private void OnEnable()
    {
        // Stage triggers can request the bar while its enemy root is still
        // inactive. Wait until activation to measure the actual model
        // renderers, rather than anchoring to a dormant/invisible collider.
        anchorCaptured = false;
        if (requested && presentation != null && health != null && !health.IsDead)
            presentation.SetActive(true);
    }

    private void OnDisable()
    {
        if (presentation != null)
            presentation.SetActive(false);
        // If this enemy is later respawned/re-enabled at a new position,
        // establish its resting head height again on the next activation.
        anchorCaptured = false;
    }

    private void LateUpdate()
    {
        if (!requested || health == null || presentation == null)
            return;

        // A few legacy controllers directly assign EnemyHealth.currentHealth
        // rather than calling TakeDamage. Keep their bar truthful too.
        if (health.IsDead || health.currentHealth <= 0f)
        {
            if (presentation.activeSelf)
                presentation.SetActive(false);
            return;
        }
        float displayMaximum = GetDisplayMaximumHealth();
        if (!Mathf.Approximately(lastRenderedHealth, health.currentHealth) ||
            !Mathf.Approximately(lastRenderedMaximum, displayMaximum))
            UpdateHealth(health.currentHealth, displayMaximum);

        if (viewCamera == null || !viewCamera.isActiveAndEnabled)
            viewCamera = Camera.main;
        if (viewCamera == null)
        {
            presentation.SetActive(false);
            return;
        }

        if (!anchorCaptured)
            CaptureStableAnchor();
        Vector3 horizontalCenter = health.transform.TransformPoint(centerOffsetLocal);
        Vector3 worldPoint = new Vector3(
            horizontalCenter.x, fixedWorldHeight, horizontalCenter.z);
        Vector3 viewport = viewCamera.WorldToViewportPoint(worldPoint);
        float distance = Vector3.Distance(worldPoint, viewCamera.transform.position);

        // Never display floating bars from enemies behind the camera or offscreen.
        bool visible = viewport.z > viewCamera.nearClipPlane &&
            viewport.x > 0.015f && viewport.x < 0.985f &&
            viewport.y > 0.075f && viewport.y < 0.94f &&
            distance <= maxViewDistance;

        if (presentation.activeSelf != visible)
            presentation.SetActive(visible);
        if (!visible)
            return;

        canvasRect.position = worldPoint;
        canvasRect.rotation = viewCamera.transform.rotation;

        float worldWidth;
        if (viewCamera.orthographic)
            worldWidth = 2f * viewCamera.orthographicSize * desiredScreenWidthPixels / Screen.height;
        else
            worldWidth = 2f * viewport.z *
                Mathf.Tan(viewCamera.fieldOfView * Mathf.Deg2Rad * 0.5f) *
                desiredScreenWidthPixels / Screen.height;

        // World-space Canvas stays a consistent readable pixel width at any
        // model scale, including the giant Stage 3 model hierarchies.
        float scale = Mathf.Clamp(worldWidth / 160f, 0.001f, 0.35f);
        canvasRect.localScale = new Vector3(scale, scale, scale);
    }

    private void CaptureStableAnchor()
    {
        // Sample the mesh top only when this enemy's overhead HUD activates.
        // Animator bobbing, jumping, attack poses, and root Y movement must
        // never drag the health bar up/down after that initial capture.
        RefreshModelTop();
        fixedWorldHeight = cachedTopY +
            Mathf.Max(verticalClearance, cachedModelHeight * 0.07f);
        centerOffsetLocal = health.transform.InverseTransformPoint(
            new Vector3(cachedCenterX, health.transform.position.y, cachedCenterZ));
        anchorCaptured = true;
    }

    private void RefreshModelTop()
    {
        float originY = health.transform.position.y;
        float highest = originY;
        float lowest = originY;
        float left = health.transform.position.x;
        float right = left;
        float back = health.transform.position.z;
        float front = back;
        bool found = false;

        if (modelRenderers != null)
        {
            foreach (Renderer renderer in modelRenderers)
            {
                if (renderer == null || !renderer.enabled ||
                    !renderer.gameObject.activeInHierarchy)
                    continue;
                if (!(renderer is SkinnedMeshRenderer) && !(renderer is MeshRenderer))
                    continue;

                Bounds b = renderer.bounds;
                if (b.size.y < 0.01f || b.size.y > 60f)
                    continue; // exclude oversized arena/VFX helper meshes

                highest = Mathf.Max(highest, b.max.y);
                lowest = Mathf.Min(lowest, b.min.y);
                left = Mathf.Min(left, b.min.x);
                right = Mathf.Max(right, b.max.x);
                back = Mathf.Min(back, b.min.z);
                front = Mathf.Max(front, b.max.z);
                found = true;
            }
        }

        if (!found && fallbackCollider != null)
        {
            Bounds b = fallbackCollider.bounds;
            highest = Mathf.Max(originY, b.max.y);
            lowest = Mathf.Min(originY, b.min.y);
            left = Mathf.Min(left, b.min.x);
            right = Mathf.Max(right, b.max.x);
            back = Mathf.Min(back, b.min.z);
            front = Mathf.Max(front, b.max.z);
        }

        cachedTopY = highest;
        cachedCenterX = (left + right) * 0.5f;
        cachedCenterZ = (back + front) * 0.5f;
        cachedModelHeight = Mathf.Max(0.5f, highest - lowest);
    }

    private void CreatePresentation()
    {
        presentation = new GameObject(
            "RegularEnemyOverheadHP", typeof(RectTransform), typeof(Canvas));
        canvasRect = presentation.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(DisplayWidth, DisplayHeight);
        canvasRect.localScale = Vector3.one * 0.005f;
        Canvas canvas = presentation.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = false;

        // Noninteractable decorative images, intentionally no GraphicRaycaster.
        Sprite frameSprite = Resources.Load<Sprite>(FrameSpritePath);
        Sprite fillSprite = Resources.Load<Sprite>(FillSpritePath);
        if (frameSprite != null && fillSprite != null)
        {
            // The transparent frame stays at full size; the underlying fill
            // changes via Image.fillAmount (not RectTransform scaling).
            // The navy track shows cleanly through the empty portion.
            CreateBlock("EmptyNavyTrack", canvasRect, PanelNavy, 136f, 11.5f, Vector2.zero);
            hpFill = CreateSprite("DynamicRedHealthFill", canvasRect, fillSprite);
            hpFill.type = Image.Type.Filled;
            hpFill.fillMethod = Image.FillMethod.Horizontal;
            hpFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            hpFill.fillAmount = 1f;
            CreateSprite("ApprovedGoldNavyFrame", canvasRect, frameSprite);
            usesApprovedFillSprite = true;
            return;
        }

        Debug.LogWarning(
            "[RegularEnemyOverheadHealthBar] Missing approved UI Sprite resources. " +
            "Showing temporary basic health bar; check the SpellstrikeHUD Sprite imports.");
        CreateBlock("GoldFrame", canvasRect, FrameGold, DisplayWidth, 22f, Vector2.zero);
        CreateBlock("NavyBorder", canvasRect, PanelNavy, 157f, 19f, Vector2.zero);
        CreateBlock("HealthTrack", canvasRect, EmptyTrack, 147f, 9.5f, new Vector2(0f, -1.2f));

        RectTransform track = new GameObject(
            "HealthFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image))
            .GetComponent<RectTransform>();
        track.SetParent(canvasRect, false);
        track.anchorMin = track.anchorMax = new Vector2(0.5f, 0.5f);
        track.pivot = new Vector2(0f, 0.5f);
        track.anchoredPosition = new Vector2(-71.5f, -1.2f);
        track.sizeDelta = new Vector2(143f, 7.5f);
        Image fill = track.GetComponent<Image>();
        fill.color = HealthyFill;
        fill.raycastTarget = false;
        // Solid UI Images with no source Sprite render correctly as Simple.
        // Shrink the Rect width for HP; Image.Type.Filled can ignore fillAmount
        // when its source Sprite is null.
        fill.type = Image.Type.Simple;
        hpFill = fill;

        // Tiny gold glints at the frame ends, no names/numbers obstructing combat.
        CreateBlock("LeftRivet", canvasRect, FrameGold, 3f, 12f, new Vector2(-76f, 0f));
        CreateBlock("RightRivet", canvasRect, FrameGold, 3f, 12f, new Vector2(76f, 0f));
    }

    private static Image CreateSprite(string name, RectTransform parent, Sprite sprite)
    {
        GameObject go = new GameObject(
            name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(DisplayWidth, DisplayHeight);
        rect.anchoredPosition = Vector2.zero;
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.color = Color.white;
        image.raycastTarget = false;
        return image;
    }

    private static Image CreateBlock(
        string name, RectTransform parent, Color color,
        float width, float height, Vector2 position)
    {
        GameObject go = new GameObject(
            name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = position;
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private void UpdateHealth(float current, float maximum)
    {
        if (hpFill == null)
            return;
        float fraction = maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f;
        if (usesApprovedFillSprite)
        {
            // Clips the red asset right-to-left, preserving its height, left
            // bevel, and the untouched ornate frame above it.
            hpFill.fillAmount = fraction;
        }
        else
        {
            RectTransform fillRect = hpFill.rectTransform;
            fillRect.sizeDelta = new Vector2(143f * fraction, 7.5f);
            hpFill.color = Color.Lerp(CriticalFill, HealthyFill, fraction);
        }
        lastRenderedHealth = current;
        lastRenderedMaximum = maximum;
    }

    private void HandleHealthChanged(float current, float maximum)
    {
        UpdateHealth(current, GetDisplayMaximumHealth());
    }

    private float GetDisplayMaximumHealth()
    {
        if (health == null)
            return 0f;

        // Stage 1 Bramble's attack controller owns its actual HP and mirrors
        // currentHealth to EnemyHealth without synchronizing maxHealth.
        // Show a truthful fraction using the real Bramble maximum, without
        // changing combat values or the existing damage/death ownership.
        return brambleHealthOwner != null && brambleHealthOwner.maxHealth > 0f
            ? brambleHealthOwner.maxHealth
            : health.maxHealth;
    }

    private void HandleDied(EnemyHealth _)
    {
        requested = false;
        if (presentation != null)
            presentation.SetActive(false);
    }

    private void Unsubscribe()
    {
        if (health == null)
            return;
        health.HealthChanged -= HandleHealthChanged;
        health.Died -= HandleDied;
    }

    private void OnDestroy()
    {
        Unsubscribe();
        if (presentation != null)
            Destroy(presentation);
    }
}
