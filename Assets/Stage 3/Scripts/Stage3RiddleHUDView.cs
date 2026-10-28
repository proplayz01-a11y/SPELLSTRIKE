using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class Stage3RiddleHUDView : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text progressDotsText;
    [SerializeField] private Image progressActiveDot;
    [SerializeField] private TMP_Text riddleText;
    [SerializeField] private TMP_Text instructionText;

    [Header("Feedback")]
    [SerializeField] private GameObject feedbackRoot;
    [SerializeField] private Image feedbackBackdrop;
    [SerializeField] private Image feedbackAccent;
    [SerializeField] private TMP_Text feedbackTitleText;
    [SerializeField] private TMP_Text feedbackBodyText;

    [Header("Animation")]
    [Min(0f)]
    [SerializeField] private float fadeDuration = 0.35f;
    [Min(0f)]
    [SerializeField] private float feedbackHoldSeconds = 1.6f;

    private Coroutine fadeRoutine;
    private Coroutine feedbackRoutine;
    private GameObject feedbackOverlay;

    // The authored FeedbackPopup remains in the parchment prefab unchanged.
    // During outcome cinematics its runtime copy belongs directly to the HUD
    // canvas, so the riddle panel can fade completely without hiding feedback.
    private const float FeedbackSlideSeconds = 0.28f;
    private const float FeedbackLeftMargin = 28f;

    // Centers of the four baked gold sockets in the current progress-plate art,
    // expressed in local coordinates for the authored 150x75 RectTransform.
    private static readonly float[] ProgressDotX = { -24.9f, -8.5f, 7.9f, 24.2f };
    private const float ProgressDotY = -7f;

    public bool IsVisible => canvasGroup != null && canvasGroup.alpha > 0.001f;
    public float FadeDuration => Mathf.Max(0f, fadeDuration);
    public float FeedbackHoldSeconds => Mathf.Max(0f, feedbackHoldSeconds);

    private void Awake()
    {
        if (titleText != null)
            titleText.text = "RIDDLE CHALLENGE";
        if (subtitleText != null)
            subtitleText.text = "Stage 3 Trial";
        if (instructionText != null)
            instructionText.text = "Answer using the tiles below";

        SetVisibleImmediate(false);
        HideFeedbackImmediate();
    }

    private void OnDisable()
    {
        HideFeedbackImmediate();
    }

    public void ShowRiddle(int index, int total, string body, bool immediate = false)
    {
        int safeTotal = Mathf.Max(1, total);
        int safeIndex = Mathf.Clamp(index, 1, safeTotal);

        if (progressText != null)
            progressText.text = $"Riddle {safeIndex} / {safeTotal}";
        if (progressDotsText != null)
            progressDotsText.text = BuildProgressDots(safeIndex, safeTotal);
        UpdateActiveProgressDot(safeIndex, safeTotal);
        if (riddleText != null)
            riddleText.text = body ?? string.Empty;

        gameObject.SetActive(true);

        if (immediate)
        {
            SetVisibleImmediate(true);
            return;
        }

        StartFade(1f);
    }

    public void HideRiddle(bool immediate = false)
    {
        HideFeedbackImmediate();

        if (immediate)
        {
            SetVisibleImmediate(false);
            return;
        }

        StartFade(0f);
    }

    public void ShowCorrectFeedback(string body = "Attack Damage +10%")
    {
        ShowFeedback(
            "Correct!",
            body,
            new Color(0.08f, 0.82f, 0.36f, 0.94f),
            new Color(0.18f, 1f, 0.53f, 1f));
    }

    public void ShowWrongFeedback(string body = "You took damage!")
    {
        ShowFeedback(
            "Wrong!",
            body,
            new Color(0.80f, 0.08f, 0.10f, 0.94f),
            new Color(1f, 0.22f, 0.25f, 1f));
    }

    public void HideFeedbackImmediate()
    {
        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
            feedbackRoutine = null;
        }

        if (feedbackOverlay != null)
        {
            Destroy(feedbackOverlay);
            feedbackOverlay = null;
        }

        if (feedbackRoot != null)
            feedbackRoot.SetActive(false);
    }

    public void Configure(
        CanvasGroup configuredCanvasGroup,
        TMP_Text configuredTitleText,
        TMP_Text configuredSubtitleText,
        TMP_Text configuredProgressText,
        TMP_Text configuredProgressDotsText,
        Image configuredProgressActiveDot,
        TMP_Text configuredRiddleText,
        TMP_Text configuredInstructionText,
        GameObject configuredFeedbackRoot,
        Image configuredFeedbackBackdrop,
        Image configuredFeedbackAccent,
        TMP_Text configuredFeedbackTitleText,
        TMP_Text configuredFeedbackBodyText)
    {
        canvasGroup = configuredCanvasGroup;
        titleText = configuredTitleText;
        subtitleText = configuredSubtitleText;
        progressText = configuredProgressText;
        progressDotsText = configuredProgressDotsText;
        progressActiveDot = configuredProgressActiveDot;
        riddleText = configuredRiddleText;
        instructionText = configuredInstructionText;
        feedbackRoot = configuredFeedbackRoot;
        feedbackBackdrop = configuredFeedbackBackdrop;
        feedbackAccent = configuredFeedbackAccent;
        feedbackTitleText = configuredFeedbackTitleText;
        feedbackBodyText = configuredFeedbackBodyText;
    }

    private void ShowFeedback(string title, string body, Color backdropColor, Color accentColor)
    {
        if (feedbackRoot == null || !(transform.parent is RectTransform canvasRect))
        {
            Debug.LogWarning("[Stage3RiddleHUDView] Feedback template or HUD canvas is missing.", this);
            return;

        }

        HideFeedbackImmediate();

        // Clone the EXISTING styled popup, not its parchment parent, so the
        // correct/wrong message survives the panel's CanvasGroup fade-out.
        feedbackOverlay = Instantiate(feedbackRoot, canvasRect, false);
        feedbackOverlay.name = "Stage3_RiddleFeedback_Left_Runtime";
        RectTransform panelRect = feedbackOverlay.GetComponent<RectTransform>();
        if (panelRect == null)
        {
            HideFeedbackImmediate();
            return;
        }

        float width = Mathf.Clamp(canvasRect.rect.width * 0.24f, 230f, 320f);
        panelRect.anchorMin = new Vector2(0f, 0.56f);
        panelRect.anchorMax = panelRect.anchorMin;
        panelRect.pivot = new Vector2(0f, 0.5f);
        panelRect.sizeDelta = new Vector2(width, 84f);
        panelRect.localScale = Vector3.one;
        panelRect.localRotation = Quaternion.identity;
        panelRect.anchoredPosition = new Vector2(-width - 32f, 0f);
        panelRect.SetAsLastSibling();

        Image backdrop = feedbackOverlay.GetComponent<Image>();
        Image accent = feedbackOverlay.transform.Find("Accent")?.GetComponent<Image>();
        TMP_Text feedbackTitle = feedbackOverlay.transform.Find("FeedbackTitle")?.GetComponent<TMP_Text>();
        TMP_Text feedbackBody = feedbackOverlay.transform.Find("FeedbackBody")?.GetComponent<TMP_Text>();
        if (backdrop != null)
            backdrop.color = backdropColor;
        if (accent != null)
            accent.color = accentColor;
        Outline outline = feedbackOverlay.GetComponent<Outline>();
        if (outline != null)
            outline.effectColor = accentColor;
        if (feedbackTitle != null)
            feedbackTitle.text = title;
        if (feedbackBody != null)
            feedbackBody.text = body ?? string.Empty;

        // Feedback never intercepts attacks or tile input.
        foreach (Graphic graphic in feedbackOverlay.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;

        feedbackOverlay.SetActive(true);
        feedbackRoutine = StartCoroutine(SlideFeedbackFromLeft(panelRect, width));
    }

    private IEnumerator SlideFeedbackFromLeft(RectTransform popup, float width)
    {
        float elapsed = 0f;
        while (elapsed < FeedbackSlideSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / FeedbackSlideSeconds);
            float eased = t * t * (3f - 2f * t);
            popup.anchoredPosition = new Vector2(
                Mathf.Lerp(-width - 32f, FeedbackLeftMargin, eased), 0f);
            yield return null;
        }

        popup.anchoredPosition = new Vector2(FeedbackLeftMargin, 0f);
        // The encounter controller controls the exit, ensuring the next
        // parchment cannot fade in while the feedback is still on screen.
        feedbackRoutine = null;
    }

    public IEnumerator SlideFeedbackBackLeft()
    {
        if (feedbackOverlay == null)
            yield break;

        // If the outcome ends before the entrance is complete, reverse from
        // the current position rather than snapping to the resting location.
        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
            feedbackRoutine = null;
        }

        RectTransform popup = feedbackOverlay.GetComponent<RectTransform>();
        if (popup == null)
        {
            HideFeedbackImmediate();
            yield break;
        }

        Vector2 start = popup.anchoredPosition;
        Vector2 leftOffscreen = new Vector2(-popup.rect.width - 32f, start.y);
        float elapsed = 0f;
        while (elapsed < FeedbackSlideSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / FeedbackSlideSeconds);
            float eased = t * t * (3f - 2f * t);
            popup.anchoredPosition = Vector2.Lerp(start, leftOffscreen, eased);
            yield return null;
        }

        HideFeedbackImmediate();
    }

    private void StartFade(float target)
    {
        if (canvasGroup == null)
            return;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        // As soon as a panel fades out, its full-screen CanvasGroup must stop
        // intercepting inputs. Restore interaction only once fade-in is done.
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        fadeRoutine = StartCoroutine(FadeRoutine(target));
    }

    private IEnumerator FadeRoutine(float target)
    {
        float start = canvasGroup.alpha;
        float duration = Mathf.Max(0.01f, fadeDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        canvasGroup.alpha = target;
        canvasGroup.interactable = target > 0.99f;
        canvasGroup.blocksRaycasts = target > 0.99f;
        fadeRoutine = null;
    }

    private void SetVisibleImmediate(bool visible)
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        if (canvasGroup == null)
            return;

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    private static string BuildProgressDots(int index, int total)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder(total * 32);
        for (int i = 1; i <= total; i++)
        {
            if (i > 1)
                builder.Append("  ");

            builder.Append(i == index
                ? "<color=#43D8FF>●</color>"
                : "<color=#C78B2A>○</color>");
        }

        return builder.ToString();
    }

    private void UpdateActiveProgressDot(int index, int total)
    {
        if (progressActiveDot == null)
            return;

        RectTransform rect = progressActiveDot.rectTransform;
        int zeroBased = Mathf.Clamp(index - 1, 0, Mathf.Max(0, total - 1));

        float x;
        if (total == ProgressDotX.Length && zeroBased < ProgressDotX.Length)
        {
            x = ProgressDotX[zeroBased];
        }
        else
        {
            // Fallback for non-four-riddle previews: keep the marker distributed
            // across the same socket band rather than failing visibly.
            float t = total <= 1 ? 0f : zeroBased / (float)(total - 1);
            x = Mathf.Lerp(ProgressDotX[0], ProgressDotX[ProgressDotX.Length - 1], t);
        }

        rect.anchoredPosition = new Vector2(x, ProgressDotY);
        progressActiveDot.gameObject.SetActive(true);
    }
}
