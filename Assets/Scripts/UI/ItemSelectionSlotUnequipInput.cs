using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Test Item Selection only. PC: right-click equipped slot. Mobile: press and
/// hold the slot. Routes the action through its existing passive-list Button,
/// which already performs the authoritative unequip and refreshes the UI.
/// </summary>
[DisallowMultipleComponent]
public sealed class ItemSelectionSlotUnequipInput : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IBeginDragHandler
{
    [SerializeField, Range(0.3f, 1.5f)] private float holdSeconds = 0.6f;
    [SerializeField] private int slotIndex;
    [SerializeField] private RectTransform passiveListContent;

    private int activePointerId = int.MinValue;
    private float heldSeconds;
    private bool holding;

    public static bool IsMobile(RuntimePlatform platform)
    {
        return platform == RuntimePlatform.Android ||
               platform == RuntimePlatform.IPhonePlayer;
    }

#if UNITY_EDITOR
    public void Configure(int index, RectTransform listContent)
    {
        slotIndex = index;
        passiveListContent = listContent;
    }
#endif

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData == null || GetCurrentPassive() == PassiveItemId.None)
            return;

        if (IsMobile(Application.platform))
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;
            holding = true;
            activePointerId = eventData.pointerId;
            heldSeconds = 0f;
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            UnequipCurrentPassive();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData != null && eventData.pointerId == activePointerId)
            CancelHold();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (eventData != null && eventData.pointerId == activePointerId)
            CancelHold();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData != null && eventData.pointerId == activePointerId)
            CancelHold();
    }

    private void OnDisable() => CancelHold();

    private void Update()
    {
        if (!holding)
            return;

        if (GetCurrentPassive() == PassiveItemId.None)
        {
            CancelHold();
            return;
        }

        heldSeconds += Time.unscaledDeltaTime;
        if (heldSeconds < holdSeconds)
            return;

        CancelHold(); // Never trigger twice for the same touch.
        UnequipCurrentPassive();
    }

    private void CancelHold()
    {
        holding = false;
        activePointerId = int.MinValue;
        heldSeconds = 0f;
    }

    private PassiveItemId GetCurrentPassive()
    {
        PassiveItemInventory inventory = PassiveItemInventory.Instance;
        if (inventory == null || slotIndex < 0 || slotIndex >= inventory.maxEquippedPassives)
            return PassiveItemId.None;
        return inventory.GetEquippedPassive(slotIndex);
    }

    private void UnequipCurrentPassive()
    {
        PassiveItemId passive = GetCurrentPassive();
        if (passive == PassiveItemId.None)
            return;

        // Using the existing list Button avoids duplicating save / refresh
        // logic. In this scene the item is always one of the four catalog rows.
        Transform row = passiveListContent != null
            ? passiveListContent.Find(passive + "Row") : null;
        Button button = row != null ? row.GetComponent<Button>() : null;
        if (button == null || !button.IsActive() || !button.IsInteractable())
        {
            Debug.LogWarning("[ItemSelection TEST] Unequip cancelled: passive row unavailable for " + passive);
            return;
        }
        button.onClick.Invoke();
    }
}
