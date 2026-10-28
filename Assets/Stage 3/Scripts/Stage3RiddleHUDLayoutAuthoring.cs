using TMPro;
using UnityEngine;

/// <summary>
/// Inspector-facing authoring handles for the Stage 3 riddle HUD.
/// This component never changes layout automatically. Use the context-menu
/// commands only when you explicitly want to capture/apply the centralized
/// layout values below. Direct RectTransform edits remain fully supported.
/// </summary>
[DisallowMultipleComponent]
public sealed class Stage3RiddleHUDLayoutAuthoring : MonoBehaviour
{
    [Header("Editable Panels")]
    [SerializeField] private RectTransform bodyFrame;
    [SerializeField] private RectTransform parchmentArt;
    [SerializeField] private RectTransform titlePlaque;
    [SerializeField] private RectTransform progressPlate;
    [SerializeField] private RectTransform divider;

    [Header("Editable Body Border")]
    [SerializeField] private RectTransform borderTop;
    [SerializeField] private RectTransform borderBottom;
    [SerializeField] private RectTransform borderLeft;
    [SerializeField] private RectTransform borderRight;

    [Header("Editable Corners + Crystals")]
    [SerializeField] private RectTransform cornerTopLeft;
    [SerializeField] private RectTransform cornerTopRight;
    [SerializeField] private RectTransform cornerBottomLeft;
    [SerializeField] private RectTransform cornerBottomRight;
    [SerializeField] private RectTransform crystalTopLeft;
    [SerializeField] private RectTransform crystalTopRight;
    [SerializeField] private RectTransform crystalBottomLeft;
    [SerializeField] private RectTransform crystalBottomRight;

    [Header("Editable Text")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text progressDotsText;
    [SerializeField] private TMP_Text riddleText;
    [SerializeField] private TMP_Text instructionText;

    [Header("Body Frame")]
    [SerializeField] private Vector2 bodyPosition = new Vector2(0f, 90f);
    [SerializeField] private Vector2 bodySize = new Vector2(760f, 254f);

    [Header("Title Plaque")]
    [SerializeField] private Vector2 titlePosition = new Vector2(0f, 292f);
    [SerializeField] private Vector2 titleSize = new Vector2(460f, 153f);

    [Header("Progress Plate")]
    [SerializeField] private Vector2 progressPosition = new Vector2(300f, 278f);
    [SerializeField] private Vector2 progressSize = new Vector2(150f, 75f);

    [Header("Divider")]
    [SerializeField] private Vector2 dividerPosition = new Vector2(0f, -67.9f);
    [SerializeField] private Vector2 dividerSize = new Vector2(66f, 18f);

    public RectTransform BodyFrame => bodyFrame;
    public RectTransform ParchmentArt => parchmentArt;
    public RectTransform TitlePlaque => titlePlaque;
    public RectTransform ProgressPlate => progressPlate;
    public RectTransform Divider => divider;
    public RectTransform BorderTop => borderTop;
    public RectTransform BorderBottom => borderBottom;
    public RectTransform BorderLeft => borderLeft;
    public RectTransform BorderRight => borderRight;

    public void Configure(
        RectTransform body,
        RectTransform parchment,
        RectTransform title,
        RectTransform progress,
        RectTransform dividerRect,
        TMP_Text titleLabel,
        TMP_Text subtitleLabel,
        TMP_Text progressLabel,
        TMP_Text progressDotsLabel,
        TMP_Text riddleLabel,
        TMP_Text instructionLabel,
        RectTransform topBorder = null,
        RectTransform bottomBorder = null,
        RectTransform leftBorder = null,
        RectTransform rightBorder = null,
        RectTransform topLeftCorner = null,
        RectTransform topRightCorner = null,
        RectTransform bottomLeftCorner = null,
        RectTransform bottomRightCorner = null,
        RectTransform topLeftCrystal = null,
        RectTransform topRightCrystal = null,
        RectTransform bottomLeftCrystal = null,
        RectTransform bottomRightCrystal = null)
    {
        bodyFrame = body;
        parchmentArt = parchment;
        titlePlaque = title;
        progressPlate = progress;
        divider = dividerRect;
        titleText = titleLabel;
        subtitleText = subtitleLabel;
        progressText = progressLabel;
        progressDotsText = progressDotsLabel;
        riddleText = riddleLabel;
        instructionText = instructionLabel;
        borderTop = topBorder;
        borderBottom = bottomBorder;
        borderLeft = leftBorder;
        borderRight = rightBorder;
        cornerTopLeft = topLeftCorner;
        cornerTopRight = topRightCorner;
        cornerBottomLeft = bottomLeftCorner;
        cornerBottomRight = bottomRightCorner;
        crystalTopLeft = topLeftCrystal;
        crystalTopRight = topRightCrystal;
        crystalBottomLeft = bottomLeftCrystal;
        crystalBottomRight = bottomRightCrystal;
        CaptureCurrentLayout();
    }

    [ContextMenu("Capture Current RectTransforms Into Layout Values")]
    public void CaptureCurrentLayout()
    {
        Capture(bodyFrame, ref bodyPosition, ref bodySize);
        Capture(titlePlaque, ref titlePosition, ref titleSize);
        Capture(progressPlate, ref progressPosition, ref progressSize);
        Capture(divider, ref dividerPosition, ref dividerSize);
    }

    [ContextMenu("Apply Layout Values To RectTransforms")]
    public void ApplyLayoutValues()
    {
        Apply(bodyFrame, bodyPosition, bodySize);
        Apply(titlePlaque, titlePosition, titleSize);
        Apply(progressPlate, progressPosition, progressSize);
        Apply(divider, dividerPosition, dividerSize);
    }

    private static void Capture(RectTransform rect, ref Vector2 position, ref Vector2 size)
    {
        if (rect == null)
            return;
        position = rect.anchoredPosition;
        size = rect.sizeDelta;
    }

    private static void Apply(RectTransform rect, Vector2 position, Vector2 size)
    {
        if (rect == null)
            return;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
