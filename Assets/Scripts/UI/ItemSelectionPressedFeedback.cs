using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Test-only pressed-state tint for Item Selection controls. The translucent
/// overlay sits above the slot/card/button visuals but never intercepts input.
/// It does not alter child transforms, sprite colors, inventory, or hover FX.
/// </summary>
[DisallowMultipleComponent]
public sealed class ItemSelectionPressedFeedback : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IBeginDragHandler
{
    [SerializeField] private Image pressedOverlay;
    [SerializeField] private bool equipmentSlot;
    [SerializeField, Range(0.05f, 0.6f)] private float pressedOpacity = 0.24f;
    [SerializeField] private float pressInSeconds = 0.06f;
    [SerializeField] private float releaseSeconds = 0.13f;

    private int activePointerId = int.MinValue;
    private bool pressed;
    private float currentOpacity;

#if UNITY_EDITOR
    public void Configure(Image overlay, bool isEquipmentSlot)
    {
        pressedOverlay = overlay;
        equipmentSlot = isEquipmentSlot;
        pressedOverlay.raycastTarget = false;
        pressedOverlay.color = new Color(0.11f, 0.08f, 0.06f, 0f);
    }
#endif

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData == null || pressedOverlay == null)
            return;

        // Right-click is the only PC slot action. On mobile, a long-press
        // begins with the same left-button event as a normal touch.
        bool rightClick = eventData.button == PointerEventData.InputButton.Right;
        bool touchOrLeftClick = eventData.button == PointerEventData.InputButton.Left;
        bool valid = equipmentSlot
            ? ItemSelectionSlotUnequipInput.IsMobile(Application.platform)
                ? touchOrLeftClick
                : rightClick
            : touchOrLeftClick;
        if (!valid)
            return;

        if (!equipmentSlot)
        {
            Button button = GetComponent<Button>();
            if (button != null && (!button.isActiveAndEnabled || !button.IsInteractable()))
                return;
        }

        activePointerId = eventData.pointerId;
        pressed = true;
        // Respond instantly to a tap, even if the scene changes that frame.
        currentOpacity = pressedOpacity * 0.7f;
        UpdateOverlay();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData != null && eventData.pointerId == activePointerId)
            Release();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (eventData != null && eventData.pointerId == activePointerId)
            Release();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData != null && eventData.pointerId == activePointerId)
            Release();
    }

    private void OnDisable()
    {
        Release();
        currentOpacity = 0f;
        UpdateOverlay();
    }

    private void Update()
    {
        if (pressedOverlay == null)
            return;

        float target = pressed ? pressedOpacity : 0f;
        float seconds = pressed ? pressInSeconds : releaseSeconds;
        float step = Time.unscaledDeltaTime * pressedOpacity / Mathf.Max(0.001f, seconds);
        currentOpacity = Mathf.MoveTowards(currentOpacity, target, step);
        UpdateOverlay();
    }

    private void Release()
    {
        pressed = false;
        activePointerId = int.MinValue;
    }

    private void UpdateOverlay()
    {
        if (pressedOverlay == null)
            return;
        Color color = pressedOverlay.color;
        if (!Mathf.Approximately(color.a, currentOpacity))
        {
            color.a = currentOpacity;
            pressedOverlay.color = color;
        }
    }
}
