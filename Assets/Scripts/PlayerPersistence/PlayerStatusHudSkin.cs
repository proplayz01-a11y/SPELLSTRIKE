using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Presentation-only skin for the existing persistent player HealthBar Slider.
/// Uses the approved five individual portrait/HP/EXP sprites, never changes the
/// health Slider, damage logic, progression state, or responsive layout.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Slider))]
public sealed class PlayerStatusHudSkin : MonoBehaviour
{
    private const string SpriteFolder = "SpellstrikePlayerHUD/";
    public const string EditableRootName = "SpellstrikePlayerStatusSkin";
    private static readonly Color EmptyHealth = new Color(0.15f, 0.025f, 0.045f, 1f);
    private static readonly Color EmptyExperience = new Color(0.035f, 0.13f, 0.24f, 1f);
    private static readonly Color UnboundExperienceArtwork = new Color(1f, 1f, 1f, 0.22f);

    private readonly List<Graphic> originalGraphics = new List<Graphic>();
    private readonly List<bool> originalGraphicEnabled = new List<bool>();

    private Slider healthSlider;
    private GameObject skinRoot;
    private Image healthFill;
    private Image experienceFill;
    private Text healthNumber;
    private float lastHealth = float.NaN;
    private float lastMaximum = float.NaN;
    private bool hasExperienceBinding;
    private float experienceFraction;

    private void Awake()
    {
        healthSlider = GetComponent<Slider>();
    }

    public void Initialize()
    {
        if (skinRoot != null)
            return;

        if (healthSlider == null)
            healthSlider = GetComponent<Slider>();
        if (healthSlider == null)
            return;

        // An artist-authored Scene View hierarchy takes priority over runtime
        // construction. This preserves all user-edited RectTransforms, image
        // colors/sprites and typography when entering Play or changing stages.
        Transform authored = transform.Find(EditableRootName);
        if (authored != null)
        {
            // The editable scene groups HP objects under HEALTH and XP under
            // EXP. Transform.Find does not search grandchildren by name.
            // Retain flat-path fallback for older authored HUD hierarchies.
            healthFill = (authored.Find("HEALTH/HP_LiveFill") ??
                authored.Find("HP_LiveFill"))?.GetComponent<Image>();
            experienceFill = (authored.Find("EXP/EXP_LiveFill") ??
                authored.Find("EXP_LiveFill"))?.GetComponent<Image>();
            healthNumber = (authored.Find("HEALTH/HP_ActualValue") ??
                authored.Find("HP_ActualValue"))?.GetComponent<Text>();
            if (healthFill == null || experienceFill == null || healthNumber == null)
            {
                Debug.LogWarning(
                    "[PlayerStatusHudSkin] Editable HUD hierarchy is incomplete; " +
                    "keeping it untouched rather than generating a duplicate.", this);
                return;
            }

            skinRoot = authored.gameObject;
            CaptureOriginalGraphics();
            DisableOriginalGraphics();
            if (hasExperienceBinding)
            {
                experienceFill.color = Color.white;
                experienceFill.fillAmount = experienceFraction;
            }
            RefreshHealth(force: true);
            return;
        }

        if (!TryGetArt(out Sprite portrait, out Sprite hpFrame, out Sprite hpArt,
            out Sprite xpFrame, out Sprite xpArt))
        {
            Debug.LogWarning(
                "[PlayerStatusHudSkin] One or more approved HUD Sprite assets are missing; keeping original player HP Slider.",
                this);
            return;
        }

        CaptureOriginalGraphics();

        RectTransform holder = CreateVisualLayers(transform, portrait, hpFrame,
            hpArt, xpFrame, xpArt, out healthFill, out experienceFill,
            out healthNumber);
        skinRoot = holder.gameObject;

        DisableOriginalGraphics();

        RefreshHealth(force: true);
        Debug.Log(
            "[PlayerStatusHudSkin] Approved portrait + HP/EXP bars now replace " +
            "the legacy top-left player HealthBar artwork in Play Mode.",
            this);
    }

    private void CaptureOriginalGraphics()
    {
        originalGraphics.Clear();
        originalGraphicEnabled.Clear();
        foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true))
        {
            if (skinRoot != null && graphic.transform.IsChildOf(skinRoot.transform))
                continue;
            originalGraphics.Add(graphic);
            originalGraphicEnabled.Add(graphic.enabled);
        }
    }

    private void DisableOriginalGraphics()
    {
        foreach (Graphic graphic in originalGraphics)
            if (graphic != null)
                graphic.enabled = false;
    }

    private static bool TryGetArt(
        out Sprite portrait, out Sprite hpFrame, out Sprite hpArt,
        out Sprite xpFrame, out Sprite xpArt)
    {
        portrait = Resources.Load<Sprite>(SpriteFolder + "VanAert_Portrait");
        hpFrame = Resources.Load<Sprite>(SpriteFolder + "PlayerHP_Frame");
        hpArt = Resources.Load<Sprite>(SpriteFolder + "PlayerHP_Fill");
        xpFrame = Resources.Load<Sprite>(SpriteFolder + "PlayerEXP_Frame");
        xpArt = Resources.Load<Sprite>(SpriteFolder + "PlayerEXP_Fill");
        return portrait != null && hpFrame != null && hpArt != null &&
            xpFrame != null && xpArt != null;
    }

    // One visual-layout authority for real gameplay AND the editor-only
    // Scene View preview. No duplicated hand-drawn mockup or separate manager.
    private static RectTransform CreateVisualLayers(
        Transform parent, Sprite portrait, Sprite hpFrame, Sprite hpArt,
        Sprite xpFrame, Sprite xpArt, out Image hpFill,
        out Image xpFill, out Text number)
    {
        RectTransform holder = NewRect(EditableRootName, parent);
        holder.anchorMin = holder.anchorMax = new Vector2(0f, 1f);
        holder.pivot = new Vector2(0f, 1f);
        // The legacy HealthBar itself is already placed at the safe-area
        // TOP-LEFT by TutorialResponsiveLayout (44, -18). Offset its new
        // wider portrait/bars toward the viewport corner, but keep 30 units
        // of top clearance so the wizard hat is never clipped by screen edge.
        holder.anchoredPosition = new Vector2(-27f, -12f);
        holder.sizeDelta = new Vector2(357f, 127f);
        holder.localScale = Vector3.one;
        // Background tracks do not imply actual HP/XP has been gained.
        CreateTrack("HP_EmptyTrack", holder, EmptyHealth, 198f, 23f, 128f, -37f);
        CreateTrack("EXP_EmptyTrack", holder, EmptyExperience, 199f, 15f, 128f, -84f);

        // Red/blue fills are distinct transparent UI Images, below their fixed
        // gold/navy frames. Only their Image.fillAmount changes at runtime.
        hpFill = CreateSprite("HP_LiveFill", holder, hpArt, 198f, 23f, 128f, -37f);
        hpFill.type = Image.Type.Filled;
        hpFill.fillMethod = Image.FillMethod.Horizontal;
        hpFill.fillOrigin = (int)Image.OriginHorizontal.Left;

        xpFill = CreateSprite("EXP_LiveFill", holder, xpArt, 199f, 15f, 128f, -84f);
        xpFill.type = Image.Type.Filled;
        xpFill.fillMethod = Image.FillMethod.Horizontal;
        xpFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        // No runtime XP accumulation service exists yet. The faint blue
        // artwork is a cosmetic EMPTY TRACK, not a full XP indicator.
        xpFill.fillAmount = 1f;
        xpFill.color = UnboundExperienceArtwork;

        CreateSprite("HP_FixedGoldFrame", holder, hpFrame, 242f, 43f, 106f, -28f);
        CreateSprite("EXP_FixedGoldFrame", holder, xpFrame, 233f, 32f, 108f, -76f);

        number = CreateHealthNumber(holder);
        // The portrait sits on top of the left ends of both bar frames, as in
        // the approved combined concept. No numbers are baked into the art.
        CreateSprite("VanAert_StorybookPortrait", holder, portrait, 124f, 122f, 0f, 0f);

        return holder;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Called only from the explicit authoring menu. Creates ordinary scene
    /// children, fully editable through the Inspector and Undo, not a ghost
    /// Edit Mode preview. The editor menu owns registration/scene dirtiness.
    /// </summary>
    public static GameObject CreateEditableSceneHud(Slider slider)
    {
        if (slider == null ||
            !TryGetArt(out Sprite portrait, out Sprite hpFrame, out Sprite hpArt,
                out Sprite xpFrame, out Sprite xpArt))
            return null;

        if (slider.transform.Find(EditableRootName) != null)
            return null;

        RectTransform holder = CreateVisualLayers(slider.transform, portrait,
            hpFrame, hpArt, xpFrame, xpArt,
            out Image redFill, out Image blueFill, out Text number);
        float maximum = Mathf.Max(0f, slider.maxValue);
        float health = Mathf.Clamp(slider.value, 0f, maximum);
        redFill.fillAmount = maximum > 0f ? health / maximum : 0f;
        number.text = Mathf.RoundToInt(health) + " / " +
            Mathf.RoundToInt(maximum);
        return holder.gameObject;
    }

    // Called ONLY by the editor-only Scene View bridge. The preview object has
    // DontSave flags and is destroyed before Play, so it never modifies the
    // real scene or duplicates the runtime skin.
    public static GameObject CreateEditorScenePreview(Slider slider)
    {
        if (slider == null ||
            !TryGetArt(out Sprite portrait, out Sprite hpFrame, out Sprite hpArt,
                out Sprite xpFrame, out Sprite xpArt))
            return null;

        // Build detached first, so creating UI Images in Edit Mode does not
        // mutate the serialized Canvas/scene hierarchy during construction.
        RectTransform holder = CreateVisualLayers(null, portrait,
            hpFrame, hpArt, xpFrame, xpArt,
            out Image redFill, out Image blueFill, out Text number);
        holder.name = "SpellstrikePlayerStatusSkin_EditPreview";
        const HideFlags temporaryFlags =
            HideFlags.HideInHierarchy | HideFlags.DontSaveInEditor |
            HideFlags.DontSaveInBuild;
        foreach (Transform child in holder.GetComponentsInChildren<Transform>(true))
            child.gameObject.hideFlags = temporaryFlags;
        holder.SetParent(slider.transform, false);

        float maximum = Mathf.Max(0f, slider.maxValue);
        float health = Mathf.Clamp(slider.value, 0f, maximum);
        redFill.fillAmount = maximum > 0f ? health / maximum : 0f;
        number.text = Mathf.RoundToInt(health) + " / " +
            Mathf.RoundToInt(maximum);
        blueFill.fillAmount = 1f;
        blueFill.color = UnboundExperienceArtwork;
        return holder.gameObject;
    }
#endif

    /// <summary>
    /// Future integration point for a REAL normalized XP progress value only.
    /// Must be called by a gameplay XP owner, never from scene artwork.
    /// </summary>
    public void SetExperienceFraction(float fraction)
    {
        hasExperienceBinding = true;
        experienceFraction = Mathf.Clamp01(fraction);
        if (experienceFill != null)
        {
            experienceFill.color = Color.white;
            experienceFill.fillAmount = experienceFraction;
        }
    }

    private void LateUpdate()
    {
        if (skinRoot != null)
            RefreshHealth(force: false);
    }

    private void RefreshHealth(bool force)
    {
        if (healthSlider == null || healthFill == null || healthNumber == null)
            return;

        float maximum = Mathf.Max(0f, healthSlider.maxValue);
        float current = Mathf.Clamp(healthSlider.value, 0f, maximum);
        if (!force && Mathf.Approximately(lastHealth, current) &&
            Mathf.Approximately(lastMaximum, maximum))
            return;

        healthFill.fillAmount = maximum > 0f ? current / maximum : 0f;
        healthNumber.text = Mathf.RoundToInt(current) + " / " + Mathf.RoundToInt(maximum);
        lastHealth = current;
        lastMaximum = maximum;
    }

    private static RectTransform NewRect(string objectName, Transform parent)
    {
        var go = new GameObject(objectName, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static Image CreateSprite(
        string objectName, RectTransform parent, Sprite sprite,
        float width, float height, float left, float top)
    {
        var go = new GameObject(objectName,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(left, top);
        rect.sizeDelta = new Vector2(width, height);
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.raycastTarget = false;
        return image;
    }

    private static void CreateTrack(
        string objectName, RectTransform parent, Color color,
        float width, float height, float left, float top)
    {
        Image image = CreateSprite(objectName, parent, null, width, height, left, top);
        image.color = color;
    }

    private static Text CreateHealthNumber(RectTransform holder)
    {
        var go = new GameObject(
            "HP_ActualValue", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Text), typeof(Outline));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(holder, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(132f, -36f);
        rect.sizeDelta = new Vector2(185f, 24f);
        Text text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 15;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(1f, 0.97f, 0.86f, 1f);
        text.raycastTarget = false;
        Outline outline = go.GetComponent<Outline>();
        outline.effectColor = new Color(0.045f, 0.06f, 0.1f, 1f);
        outline.effectDistance = new Vector2(1f, -1f);
        outline.useGraphicAlpha = true;
        return text;
    }

    private void OnDestroy()
    {
        for (int i = 0; i < originalGraphics.Count; ++i)
        {
            if (originalGraphics[i] != null)
                originalGraphics[i].enabled = originalGraphicEnabled[i];
        }
    }
}
