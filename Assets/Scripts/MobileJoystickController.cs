using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Lightweight mobile joystick input source.
///
/// The component owns one pointer at a time, exposes normalized analog input,
/// and recenters the handle on release. It deliberately has no UnityEditor
/// dependency so it is safe for Android/iOS player builds.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class MobileJoystickController : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    public static MobileJoystickController Current { get; private set; }

    [SerializeField] private RectTransform handle;
    [SerializeField, Min(1f)] private float joystickRange = 52f;

    private RectTransform rectTransform;
    private int activePointerId = int.MinValue;

    public Vector2 InputDirection { get; private set; }
    public bool HasActivePointer => activePointerId != int.MinValue;
    public bool OwnsPointer(int pointerId) => activePointerId == pointerId;

    public void Configure(RectTransform handleRect, float range)
    {
        handle = handleRect;
        joystickRange = Mathf.Max(1f, range);
        ResetJoystick();
    }

    private void Awake()
    {
        rectTransform = (RectTransform)transform;
    }

    private void OnEnable()
    {
        if (Current != null && Current != this)
            Debug.LogWarning("[MobileJoystickController] Replacing another active mobile joystick input source.", this);

        Current = this;
        ResetJoystick();
    }

    private void OnDisable()
    {
        if (Current == this)
            Current = null;

        ResetJoystick();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData == null || activePointerId != int.MinValue)
            return;

        activePointerId = eventData.pointerId;
        UpdateInput(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData == null || eventData.pointerId != activePointerId)
            return;

        UpdateInput(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData == null || eventData.pointerId != activePointerId)
            return;

        ResetJoystick();
    }

    private void UpdateInput(PointerEventData eventData)
    {
        if (rectTransform == null)
            rectTransform = (RectTransform)transform;

        Camera eventCamera = eventData.pressEventCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform,
                eventData.position,
                eventCamera,
                out Vector2 localPoint))
        {
            return;
        }

        // ScreenPointToLocalPointInRectangle returns coordinates relative to the
        // RectTransform pivot. Subtract the rect center so the joystick stays centered
        // even when layout code uses a bottom-left pivot for safe-area positioning.
        Vector2 centeredPoint = localPoint - rectTransform.rect.center;
        Vector2 clamped = Vector2.ClampMagnitude(centeredPoint, joystickRange);
        InputDirection = clamped / joystickRange;

        if (handle != null)
            handle.anchoredPosition = clamped;
    }

    private void ResetJoystick()
    {
        activePointerId = int.MinValue;
        InputDirection = Vector2.zero;

        if (handle != null)
            handle.anchoredPosition = Vector2.zero;
    }
}
