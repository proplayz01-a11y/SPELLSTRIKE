using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Shared stage-clear skin using the same authored art and typography as
/// TutorialCompletionPanel. The stage controller retains progression ownership.
/// </summary>
public sealed class StageRewardPresentation : MonoBehaviour
{
    [SerializeField] private TutorialCompletionVisualAssets art;
    [SerializeField] private Sprite voiceFragment;
    [SerializeField] private Sprite memoriesFragment;
    [SerializeField] private Sprite emotionsFragment;
    [SerializeField] private Sprite genericFragment;
    [SerializeField] private Sprite swordReward;
    [SerializeField] private Sprite pearlReward;

    private readonly List<Canvas> hiddenCanvases = new();
    private readonly List<GraphicRaycaster> hiddenRaycasters = new();
    private MovementofPlayer movement;
    private AttackController attacks;
    private bool movementLock;
    private bool attackLock;
    private bool modalActive;
    private bool navigating;
    private CursorLockMode previousCursorLock;
    private bool previousCursorVisible;
    private CanvasGroup group;
    private RectTransform card;
    private RectTransform floatingReward;
    private Vector2 floatingBase;
    private Vector2 lastCanvasSize;
    private readonly Color ink = new(0.13f, 0.09f, 0.055f, 1f);

    public static StageRewardPresentation Open(int stage, string title, string boss,
        string rewards, Action onContinue, Action onMainMenu)
    {
        GameObject prefab = Resources.Load<GameObject>("StageReward/StageRewardArt");
        StageRewardPresentation template = prefab != null ? prefab.GetComponent<StageRewardPresentation>() : null;
        if (template == null || template.art == null || template.art.PanelFrame == null ||
            template.art.ParchmentFill == null || template.art.ContinueWordMaster == null ||
            template.art.MainMenuOriginal == null)
            return null;

        StageRewardPresentation view = Instantiate(template);
        view.name = "StageRewardModal";
        try
        {
            view.Build(stage, title, boss, rewards, onContinue, onMainMenu);
            view.EnterModal();
            return view;
        }
        catch (Exception e)
        {
            Debug.LogError("[StageReward] Could not build reward presentation: " + e);
            view.gameObject.SetActive(false);
            Destroy(view.gameObject);
            return null;
        }
    }

    private void Build(int stage, string title, string boss, string rewards,
        Action onContinue, Action onMainMenu)
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();
        group = gameObject.AddComponent<CanvasGroup>();

        Image backdrop = Rect("ModalBackdrop", transform, Vector2.zero, Vector2.one)
            .gameObject.AddComponent<Image>();
        backdrop.color = new Color(0.005f, 0.009f, 0.02f, 0.73f);
        backdrop.raycastTarget = true;
        card = Rect("StageRewardCard", transform, Vector2.one * 0.5f, Vector2.one * 0.5f);
        card.sizeDelta = new Vector2(1252, 684);

        // Identical trimmed tutorial pieces, on their original native pixel grid.
        Art("PanelShadow", card, art.PanelShadow, -21, -16, 1304, 734);
        Art("ParchmentFill", card, art.ParchmentFill, 23, 28, 1209, 639);
        Art("OrnamentalFrame", card, art.PanelFrame, 0, 0, 1252, 684);
        Art("HeaderDivider", card, art.HeaderDivider, 227, 146, 798, 38);

        TMP_FontAsset cinzel = art.CinzelFont != null ? art.CinzelFont : TMP_Settings.defaultFontAsset;
        TMP_FontAsset italic = art.AlegreyaItalicFont != null ? art.AlegreyaItalicFont : cinzel;
        Label("TitleText", card, title, 106, 52, 1040, 88, cinzel, 62, 42,
            TextAlignmentOptions.Center);

        Art("RewardHalo", card, art.Halo, 88, 180, 460, 380);
        Art("RewardGroundShadow", card, art.GroundShadow, 120, 488, 405, 62);
        floatingReward = Rect("RewardPreview", card, Vector2.one * 0.5f, Vector2.one * 0.5f);
        At(floatingReward, 107, 192, 422, 332);
        floatingBase = floatingReward.anchoredPosition;
        Sprite fragment = stage == 1 ? voiceFragment :
            stage == 2 ? memoriesFragment : stage == 3 ? emotionsFragment : genericFragment;
        if (fragment == null) fragment = genericFragment;
        bool secondReward = (stage == 1 && swordReward != null) ||
            (stage == 2 && pearlReward != null && rewards.Contains("Cleansing Current"));
        Image crystal = RewardImage("SoulFragmentPreview", floatingReward, fragment,
            secondReward ? new Vector2(-76, 8) : Vector2.zero,
            secondReward ? new Vector2(208, 280) : new Vector2(285, 310));
        if (fragment == genericFragment && stage != 1)
            crystal.color = stage == 2 ? new Color(0.55f, 0.82f, 1f) :
                stage == 3 ? new Color(1f, 0.78f, 0.43f) : Color.white;
        if (secondReward)
            RewardImage("ItemPreview", floatingReward, stage == 1 ? swordReward : pearlReward,
                new Vector2(90, -4), stage == 1 ? new Vector2(232, 300) : new Vector2(170, 170));

        Label("BossNameText", card, boss + " Defeated", 548, 185, 610, 62, cinzel,
            40, 28, TextAlignmentOptions.Left);
        Label("RewardHeading", card, "Rewards Acquired:", 550, 255, 603, 44, cinzel,
            31, 24, TextAlignmentOptions.Left);
        string body = rewards.StartsWith("REWARDS\n", StringComparison.Ordinal)
            ? rewards.Substring("REWARDS".Length).TrimStart('\n', '\r', ' ') : rewards;
        Label("RewardsText", card, body, 550, 305, 610, stage == 1 ? 170 : 208, cinzel,
            35, 25, TextAlignmentOptions.TopLeft);
        if (stage == 1)
            Label("RewardStoryLine", card, "One step closer to Nevali...", 550, 480, 610, 34,
                italic, 30, 24, TextAlignmentOptions.Left, FontStyles.Italic);

        Art("ContinueShadow", card, art.ContinueShadow, 274, 523, 351, 104);
        Art("MainMenuShadow", card, art.MainMenuShadow, 649, 531, 326, 91);
        Image proceed = Art("ContinueButton", card, art.ContinueWordMaster, 286, 516, 325, 108);
        Button continueButton = ButtonFor(proceed, onContinue);
        continueButton.transition = Selectable.Transition.SpriteSwap;
        SpriteState spriteState = continueButton.spriteState;
        spriteState.highlightedSprite = art.ContinueWordMasterHover;
        spriteState.pressedSprite = art.ContinueWordMasterHover;
        spriteState.selectedSprite = art.ContinueWordMasterHover;
        continueButton.spriteState = spriteState;
        Label("ContinueLabel", proceed.rectTransform, "CONTINUE", 0, 0, 325, 108,
            cinzel, 33, 27, TextAlignmentOptions.Center);
        Image main = Art("MainMenuButton", card, art.MainMenuOriginal, 661, 539, 300, 63);
        Button mainButton = ButtonFor(main, onMainMenu);
        ColorBlock colors = mainButton.colors;
        colors.highlightedColor = new Color(1, 0.94f, 0.78f);
        colors.pressedColor = new Color(0.8f, 0.74f, 0.64f);
        mainButton.colors = colors;
        Navigation nav = continueButton.navigation;
        nav.mode = Navigation.Mode.Explicit;
        nav.selectOnRight = mainButton;
        continueButton.navigation = nav;
        nav = mainButton.navigation;
        nav.mode = Navigation.Mode.Explicit;
        nav.selectOnLeft = continueButton;
        mainButton.navigation = nav;
        if (EventSystem.current == null)
            new GameObject("StageRewardEventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        EventSystem.current?.SetSelectedGameObject(null);
        Canvas.ForceUpdateCanvases();
        Fit();
    }

    private Button ButtonFor(Image face, Action action)
    {
        face.raycastTarget = true;
        Button b = face.gameObject.AddComponent<Button>();
        b.targetGraphic = face;
        b.onClick.AddListener(() =>
        {
            if (navigating) return;
            navigating = true;
            action?.Invoke();
        });
        return b;
    }

    private void EnterModal()
    {
        modalActive = true;
        Canvas own = GetComponent<Canvas>();
        foreach (Canvas other in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (other == own || !other.enabled || !other.gameObject.activeInHierarchy ||
                other.renderMode == RenderMode.WorldSpace) continue;
            other.enabled = false;
            hiddenCanvases.Add(other);
            foreach (GraphicRaycaster raycaster in other.GetComponents<GraphicRaycaster>())
            {
                if (!raycaster.enabled) continue;
                raycaster.enabled = false;
                hiddenRaycasters.Add(raycaster);
            }
        }
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            movement = player.GetComponent<MovementofPlayer>();
            if (movement != null) { movement.AcquireExternalMovementLock(); movementLock = true; }
            attacks = player.GetComponent<AttackController>();
            if (attacks != null && !attacks.IsAttackInputLocked)
            { attacks.SetAttackInputLocked(true); attackLock = true; }
        }
        previousCursorLock = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        group.alpha = 0;
        float elapsed = 0;
        while (elapsed < 0.22f)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Clamp01(elapsed / 0.22f);
            yield return null;
        }
        group.alpha = 1;
    }

    private void Update()
    {
        if (card == null) return;
        Fit();
        if (floatingReward != null)
            floatingReward.anchoredPosition = floatingBase + Vector2.up * (Mathf.Sin(Time.unscaledTime * 1.8f) * 6f);
    }

    private void Fit()
    {
        RectTransform root = transform as RectTransform;
        if (root == null || root.rect.size == lastCanvasSize) return;
        lastCanvasSize = root.rect.size;
        float scale = Mathf.Min(lastCanvasSize.x * 0.88f / 1252f, lastCanvasSize.y * 0.90f / 684f);
        if (scale > 0) card.localScale = Vector3.one * scale;
    }

    private void OnDisable()
    {
        if (!modalActive) return;
        modalActive = false;
        foreach (Canvas c in hiddenCanvases) if (c != null) c.enabled = true;
        foreach (GraphicRaycaster r in hiddenRaycasters) if (r != null) r.enabled = true;
        hiddenCanvases.Clear();
        hiddenRaycasters.Clear();
        if (movement != null && movementLock) movement.ReleaseExternalMovementLock();
        if (attacks != null && attackLock) attacks.SetAttackInputLocked(false);
        movementLock = attackLock = false;
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
    {
        RectTransform rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    private static void At(RectTransform rt, float x, float y, float w, float h)
    {
        RectTransform parent = (RectTransform)rt.parent;
        rt.anchorMin = rt.anchorMax = Vector2.one * 0.5f;
        rt.pivot = Vector2.one * 0.5f;
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x + w / 2 - parent.rect.width / 2,
            parent.rect.height / 2 - y - h / 2);
    }

    private static Image Art(string name, RectTransform parent, Sprite sprite, float x, float y, float w, float h)
    {
        RectTransform rt = Rect(name, parent, Vector2.one * 0.5f, Vector2.one * 0.5f);
        At(rt, x, y, w, h);
        Image image = rt.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        image.enabled = sprite != null;
        return image;
    }

    private static Image RewardImage(string name, RectTransform parent, Sprite sprite, Vector2 pos, Vector2 size)
    {
        RectTransform rt = Rect(name, parent, Vector2.one * 0.5f, Vector2.one * 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image image = rt.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.enabled = sprite != null;
        return image;
    }

    private TMP_Text Label(string name, RectTransform parent, string value, float x, float y, float w, float h,
        TMP_FontAsset font, float size, float min, TextAlignmentOptions align, FontStyles style = FontStyles.Normal)
    {
        RectTransform rt = Rect(name, parent, Vector2.one * 0.5f, Vector2.one * 0.5f);
        At(rt, x, y, w, h);
        TextMeshProUGUI text = rt.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = font;
        text.color = ink;
        text.fontStyle = style;
        text.fontSize = size;
        text.enableAutoSizing = true;
        text.fontSizeMin = min;
        text.fontSizeMax = size;
        text.alignment = align;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Truncate;
        text.raycastTarget = false;
        return text;
    }
}
