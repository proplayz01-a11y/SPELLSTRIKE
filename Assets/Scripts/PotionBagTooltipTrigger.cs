using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Restores the standalone Unity MonoScript for authored potion/passive slot
/// tooltip targets. Keep this class in a matching file so prefab components
/// can retain a stable script asset reference.
/// </summary>
public class PotionBagTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private PotionSidebarUI owner;
    private string title;
    private string body;
    private bool pointerInside;
    private bool pointerHeld;
    private bool holdTooltipShown;
    private float pointerDownTime;

    public void Initialize(PotionSidebarUI tooltipOwner, string tooltipTitle, string tooltipBody)
    {
        owner = tooltipOwner;
        title = tooltipTitle;
        body = tooltipBody;
    }

    private void Update()
    {
        if (!pointerHeld || holdTooltipShown || owner == null)
            return;

        if (Time.unscaledTime - pointerDownTime < owner.holdTooltipDelay)
            return;

        holdTooltipShown = true;
        owner.HandleTooltipHold(title, body);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        if (owner != null && owner.showTooltipOnHover)
            owner.ShowTooltip(title, body);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        pointerHeld = false;
        holdTooltipShown = false;
        owner?.HideTooltip();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pointerHeld = true;
        holdTooltipShown = false;
        pointerDownTime = Time.unscaledTime;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pointerHeld = false;
        holdTooltipShown = false;

        if (!pointerInside)
            owner?.HideTooltip();
    }
}
