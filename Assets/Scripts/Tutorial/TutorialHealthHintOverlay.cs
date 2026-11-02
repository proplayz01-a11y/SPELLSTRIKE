using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Non-interactive guidance for the tutorial's real Bag and Health Potion
/// buttons. Reuses Word Master's approved hint artwork and arrow without
/// duplicating any controls, changing inventory logic or blocking clicks.
/// </summary>
[DisallowMultipleComponent]
public sealed class TutorialHealthHintOverlay : MonoBehaviour
{
    private const string HintResource = "WordMasterRoyal/HintDarkOrnatePopup";
    private const string ArrowResource = "WordMasterRoyal/Arrow";
    private RectTransform overlayRoot;
    private RectTransform hintPanel;
    private RectTransform arrow;
    private RectTransform focusOutline;
    private Image[] borders;
    private TextMeshProUGUI titleLabel;
    private TextMeshProUGUI messageLabel;
    private RectTransform target;
    private CanvasGroup group;
    private float elapsed;

    public void Show(RectTransform button, string title, string message)
    {
        if (button == null) return;
        if (overlayRoot == null) CreateOverlay();
        target = button;
        titleLabel.text = title;
        messageLabel.text = message;
        elapsed = 0f;
        overlayRoot.gameObject.SetActive(true);
        Reposition();
    }

    public void Hide()
    {
        target = null;
        if (overlayRoot != null) overlayRoot.gameObject.SetActive(false);
    }

    private void CreateOverlay()
    {
        var canvasObject = new GameObject("Tutorial_WordMasterStyleHealthHint",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        DontDestroyOnLoad(canvasObject);
        overlayRoot = canvasObject.GetComponent<RectTransform>();
        overlayRoot.anchorMin = Vector2.zero;
        overlayRoot.anchorMax = Vector2.one;
        overlayRoot.offsetMin = overlayRoot.offsetMax = Vector2.zero;
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 140;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2400f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        group = canvasObject.GetComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;

        hintPanel = MakeRect("OrnateHint", overlayRoot, new Vector2(500f, 232f));
        var frame = hintPanel.gameObject.AddComponent<Image>();
        frame.sprite = Resources.Load<Sprite>(HintResource);
        frame.preserveAspect = false;
        frame.raycastTarget = false;
        frame.color = Color.white;

        titleLabel = MakeText("HintTitle", hintPanel, new Vector2(440f, 46f),
            new Vector2(0f, 61f), 33f, new Color(1f, .79f, .40f), FontStyles.Bold);
        messageLabel = MakeText("HintInstructions", hintPanel, new Vector2(440f, 102f),
            new Vector2(0f, -20f), 25f, new Color(.97f, .94f, .85f), FontStyles.Normal);
        messageLabel.textWrappingMode = TextWrappingModes.Normal;

        arrow = MakeRect("WordMasterHintDirection", overlayRoot, new Vector2(156f, 74f));
        var arrowImage = arrow.gameObject.AddComponent<Image>();
        arrowImage.sprite = Resources.Load<Sprite>(ArrowResource);
        arrowImage.preserveAspect = true;
        arrowImage.raycastTarget = false;

        focusOutline = MakeRect("HighlightedActualButton", overlayRoot, new Vector2(120f, 70f));
        borders = new Image[4];
        for (int i = 0; i < borders.Length; i++)
        {
            RectTransform edge = MakeRect("GoldOutline_" + i, focusOutline, new Vector2(1f, 1f));
            borders[i] = edge.gameObject.AddComponent<Image>();
            borders[i].raycastTarget = false;
            borders[i].color = new Color(1f, .79f, .29f, .85f);
        }
    }

    private void LateUpdate()
    {
        if (target == null || overlayRoot == null || !overlayRoot.gameObject.activeSelf)
            return;
        if (!target.gameObject.activeInHierarchy)
        {
            focusOutline.gameObject.SetActive(false);
            arrow.gameObject.SetActive(false);
            return;
        }
        focusOutline.gameObject.SetActive(true);
        arrow.gameObject.SetActive(true);
        elapsed += Time.unscaledDeltaTime;
        Reposition();
        float pulse = .78f + .22f * Mathf.Sin(elapsed * 4.0f);
        for (int i = 0; i < borders.Length; i++)
        {
            Color c = borders[i].color;
            c.a = Mathf.Clamp01(.68f + pulse * .26f);
            borders[i].color = c;
        }
    }

    private void Reposition()
    {
        if (target == null || overlayRoot == null) return;
        Canvas.ForceUpdateCanvases();
        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
            overlayRoot, target);
        Vector2 center = bounds.center;
        focusOutline.anchoredPosition = center;
        Vector2 size = new Vector2(Mathf.Max(64f, bounds.size.x + 16f),
            Mathf.Max(52f, bounds.size.y + 14f));
        focusOutline.sizeDelta = size;
        float halfW = size.x * .5f, halfH = size.y * .5f;
        SetEdge(borders[0].rectTransform, Vector2.up * halfH, new Vector2(size.x, 4f));
        SetEdge(borders[1].rectTransform, Vector2.down * halfH, new Vector2(size.x, 4f));
        SetEdge(borders[2].rectTransform, Vector2.left * halfW, new Vector2(4f, size.y));
        SetEdge(borders[3].rectTransform, Vector2.right * halfW, new Vector2(4f, size.y));

        Vector2 panelPosition = center + new Vector2(385f, -30f);
        Rect c = overlayRoot.rect;
        panelPosition.x = Mathf.Clamp(panelPosition.x, c.xMin + 260f, c.xMax - 260f);
        panelPosition.y = Mathf.Clamp(panelPosition.y, c.yMin + 125f, c.yMax - 125f);
        hintPanel.anchoredPosition = panelPosition;
        Vector2 start = panelPosition + new Vector2(-245f, 0f);
        Vector2 end = center + new Vector2(halfW + 10f, 0f);
        Vector2 direction = end - start;
        arrow.anchoredPosition = Vector2.Lerp(start, end, .58f);
        arrow.localRotation = Quaternion.Euler(0f, 0f,
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
    }

    private static void SetEdge(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private static RectTransform MakeRect(string name, Transform parent, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
        return rect;
    }

    private static TextMeshProUGUI MakeText(string name, Transform parent, Vector2 size,
        Vector2 position, float fontSize, Color color, FontStyles style)
    {
        RectTransform rect = MakeRect(name, parent, size);
        rect.anchoredPosition = position;
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    private void OnDestroy()
    {
        if (overlayRoot != null)
            Destroy(overlayRoot.gameObject);
    }
}
