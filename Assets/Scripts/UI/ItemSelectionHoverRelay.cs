using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

[DisallowMultipleComponent]
public sealed class ItemSelectionHoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private ModeSelectGoldBorderHover reflection = null;
    [SerializeField] private Image reflectionOverlay = null;
    [SerializeField] private PassiveItemId passiveItem = PassiveItemId.None;
    [SerializeField] private RectTransform growthTarget = null;
    [SerializeField, Range(1f, 1.1f)] private float hoverScale = 1f;

    private const float GrowSeconds = 0.11f;
    private const float ShrinkSeconds = 0.15f;
    private const float LockedReflectionAlpha = 0.45f;

    private Button button;
    private PointerEventData hoverPointer;
    private bool keyboardSelected;
    private float growth;
    private Vector3 restingScale;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

#if UNITY_EDITOR
    public void Configure(ModeSelectGoldBorderHover effect, Image overlay, PassiveItemId item,
        RectTransform scaleTarget, float scaleMultiplier)
    {
        reflection = effect;
        reflectionOverlay = overlay;
        passiveItem = item;
        growthTarget = scaleTarget;
        hoverScale = scaleMultiplier;
    }
#endif

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsHoverPointer(eventData) || eventData.dragging || !IsAvailable())
            return;

        hoverPointer = eventData;
        ApplyReflectionTone();
        reflection.OnPointerEnter(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (hoverPointer != null && eventData != null && hoverPointer.pointerId == eventData.pointerId)
            ClearPointer();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        ClearSelection(eventData);
    }

    public void OnSelect(BaseEventData eventData)
    {
        // A tap or mouse click can select the Button. Only navigation focus owns
        // an independent highlight after the pointer leaves.
        if (button == null || eventData is PointerEventData || !IsAvailable())
            return;

        keyboardSelected = true;
        ApplyReflectionTone();
        reflection.OnSelect(eventData);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        ClearSelection(eventData);
    }

    private void Update()
    {
        if ((hoverPointer != null || keyboardSelected) && !IsAvailable())
            ClearFocus();
        else if (hoverPointer != null && hoverPointer.dragging)
            ClearPointer();

        UpdateGrowth(hoverPointer != null || keyboardSelected);
    }

    private void OnDisable()
    {
        ClearFocus();
        if (growthTarget != null && growth > 0f)
            growthTarget.localScale = restingScale;
        growth = 0f;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            ClearFocus();
    }

    private void ApplyReflectionTone()
    {
        if (reflectionOverlay == null || passiveItem == PassiveItemId.None)
            return;

        // Owned items get the full material intensity. A locked item still
        // responds to inspection while its existing lock state stays visible.
        var inventory = PassiveItemInventory.Instance;
        bool owned = inventory != null && inventory.HasOwnedPassive(passiveItem);
        Color tint = reflectionOverlay.color;
        tint.a = owned ? 1f : LockedReflectionAlpha;
        reflectionOverlay.color = tint;
    }

    private bool IsAvailable()
    {
        return isActiveAndEnabled && reflection != null && reflection.isActiveAndEnabled &&
            (button == null || (button.isActiveAndEnabled && button.IsInteractable()));
    }

    private void UpdateGrowth(bool focused)
    {
        if (growthTarget == null)
            return;

        float target = focused ? 1f : 0f;
        if (growth == target)
            return;

        if (growth == 0f)
            restingScale = growthTarget.localScale;

        float seconds = focused ? GrowSeconds : ShrinkSeconds;
        growth = Mathf.MoveTowards(growth, target, Time.unscaledDeltaTime / seconds);
        float scale = Mathf.SmoothStep(1f, hoverScale, growth);
        growthTarget.localScale = restingScale * scale;
    }

    private static bool IsHoverPointer(PointerEventData eventData)
    {
#if ENABLE_INPUT_SYSTEM
        if (eventData is ExtendedPointerEventData inputEvent)
            return inputEvent.pointerType == UIPointerType.MouseOrPen;
#endif
        return eventData != null && eventData.pointerId == PointerInputModule.kMouseLeftId;
    }

    private void ClearFocus()
    {
        ClearPointer();
        ClearSelection(null);
    }

    private void ClearPointer()
    {
        if (hoverPointer == null)
            return;

        if (reflection != null)
            reflection.OnPointerExit(hoverPointer);
        hoverPointer = null;
    }

    private void ClearSelection(BaseEventData eventData)
    {
        if (!keyboardSelected)
            return;

        if (reflection != null)
            reflection.OnDeselect(eventData);
        keyboardSelected = false;
    }
}
