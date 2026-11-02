using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Guided tile/STRIKE pointer in the same visual language as Word Master.
/// The canvas is screen-space because the tutorial overlay and persistent
/// gameplay HUD belong to different canvases. This never intercepts input.
/// </summary>
[DisallowMultipleComponent]
public sealed class TutorialGuidedArrowOverlay : MonoBehaviour
{
    private const string ArrowResource = "WordMasterRoyal/Arrow";
    private readonly Vector3[] worldCorners = new Vector3[4];
    private readonly Vector2[] targetCorners = new Vector2[4];
    private RectTransform overlayRoot;
    private RectTransform arrowRect;
    private RectTransform outlineRoot;
    private Image arrowImage;
    private readonly Image[] outlineEdges = new Image[4];
    private RectTransform currentTarget;

    public RectTransform CurrentTarget => currentTarget;
    public bool IsVisible => arrowImage != null && arrowImage.gameObject.activeInHierarchy;

    public void Show(RectTransform target)
    {
        if (!Application.isPlaying || target == null)
        {
            Hide();
            return;
        }

        if (overlayRoot == null && !CreateOverlay())
            return;

        currentTarget = target;
        overlayRoot.gameObject.SetActive(true);
        UpdatePosition(0f);
    }

    public void Hide()
    {
        currentTarget = null;
        if (overlayRoot != null)
            overlayRoot.gameObject.SetActive(false);
    }

    private bool CreateOverlay()
    {
        Sprite sprite = Resources.Load<Sprite>(ArrowResource);
        if (sprite == null)
        {
            Debug.LogWarning("[TutorialGuidedArrowOverlay] Approved Word Master Arrow.png missing from Resources.", this);
            return false;
        }

        var root = new GameObject("Tutorial_GuidedArrow_WordMasterStyle",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        overlayRoot = root.GetComponent<RectTransform>();
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 150;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2400f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        var group = root.GetComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;

        outlineRoot = MakeRect("FocusedRealButton", overlayRoot);
        for (int i = 0; i < outlineEdges.Length; ++i)
        {
            var edge = MakeRect("AntiqueGoldBorder_" + i, outlineRoot);
            var graphic = edge.gameObject.AddComponent<Image>();
            graphic.color = new Color(0.96f, 0.73f, 0.28f, 0.82f);
            graphic.raycastTarget = false;
            outlineEdges[i] = graphic;
        }

        arrowRect = MakeRect("WordMasterArrow_PointsDown", overlayRoot);
        arrowRect.sizeDelta = new Vector2(162f, 77f);
        // Word Master's horizontal arrow is rotated clockwise to point down.
        arrowRect.localRotation = Quaternion.Euler(0f, 0f, -90f);
        arrowImage = arrowRect.gameObject.AddComponent<Image>();
        arrowImage.sprite = sprite;
        arrowImage.preserveAspect = true;
        arrowImage.raycastTarget = false;
        arrowImage.color = Color.white;
        return true;
    }

    private void LateUpdate()
    {
        if (currentTarget == null || overlayRoot == null || !isActiveAndEnabled)
        {
            Hide();
            return;
        }

        if (!currentTarget.gameObject.activeInHierarchy)
        {
            arrowImage.gameObject.SetActive(false);
            outlineRoot.gameObject.SetActive(false);
            return;
        }

        arrowImage.gameObject.SetActive(true);
        outlineRoot.gameObject.SetActive(true);
        UpdatePosition(Mathf.Sin(Time.unscaledTime * 3.4f) * 8f);
    }

    private void UpdatePosition(float bob)
    {
        if (currentTarget == null || overlayRoot == null)
            return;

        Canvas targetCanvas = currentTarget.GetComponentInParent<Canvas>();
        Camera camera = targetCanvas != null &&
            targetCanvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? targetCanvas.rootCanvas.worldCamera : null;

        currentTarget.GetWorldCorners(worldCorners);
        float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;
        for (int i = 0; i < worldCorners.Length; i++)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, worldCorners[i]);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                overlayRoot, screen, null, out targetCorners[i]);
            minX = Mathf.Min(minX, targetCorners[i].x);
            maxX = Mathf.Max(maxX, targetCorners[i].x);
            minY = Mathf.Min(minY, targetCorners[i].y);
            maxY = Mathf.Max(maxY, targetCorners[i].y);
        }

        Vector2 size = new Vector2(Mathf.Max(24f, maxX - minX + 9f),
            Mathf.Max(24f, maxY - minY + 9f));
        var center = new Vector2((minX + maxX) * .5f, (minY + maxY) * .5f);
        outlineRoot.anchoredPosition = center;
        outlineRoot.sizeDelta = size;
        SetEdge(outlineEdges[0].rectTransform,
            new Vector2(0, size.y * .5f), new Vector2(size.x, 3.5f));
        SetEdge(outlineEdges[1].rectTransform,
            new Vector2(0, -size.y * .5f), new Vector2(size.x, 3.5f));
        SetEdge(outlineEdges[2].rectTransform,
            new Vector2(-size.x * .5f, 0), new Vector2(3.5f, size.y));
        SetEdge(outlineEdges[3].rectTransform,
            new Vector2(size.x * .5f, 0), new Vector2(3.5f, size.y));

        // After rotation, the arrow's long axis is vertical. The tip sits
        // about 12px above the highlighted button, even when it is small.
        arrowRect.anchoredPosition = new Vector2(center.x, maxY + 96f + bob);
        arrowRect.localScale = Vector3.one * (1f + .035f *
            Mathf.Sin(Time.unscaledTime * 3.4f));
    }

    private static RectTransform MakeRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
        rt.pivot = new Vector2(.5f, .5f);
        rt.anchoredPosition = Vector2.zero;
        return rt;
    }

    private static void SetEdge(RectTransform edge, Vector2 position, Vector2 size)
    {
        edge.anchoredPosition = position;
        edge.sizeDelta = size;
    }

    private void OnDisable() => Hide();

    private void OnDestroy()
    {
        if (overlayRoot != null)
            Destroy(overlayRoot.gameObject);
    }
}
