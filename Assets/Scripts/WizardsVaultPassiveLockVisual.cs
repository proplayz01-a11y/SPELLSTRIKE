using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Presentation-only lock artwork for the isolated Wizard's Vault UI.
/// ItemSelectionSceneController remains the sole owner of inventory state and
/// its live Status text; the small gold lock follows that status at runtime.
/// </summary>
[DisallowMultipleComponent]
public sealed class WizardsVaultPassiveLockVisual : MonoBehaviour
{
    private TMP_Text status;
    private GameObject lockAccent;
    private Image rowButtonHitArea;

    private void Awake()
    {
        rowButtonHitArea = GetComponent<Image>();
        Transform statusNode = transform.Find("Status");
        Transform lockNode = transform.Find("ProductionLockAccent");
        if (statusNode) status = statusNode.GetComponent<TMP_Text>();
        if (lockNode) lockAccent = lockNode.gameObject;
    }

    private void OnEnable() => UpdateVisual();

    private void LateUpdate() => UpdateVisual();

    private void UpdateVisual()
    {
        // ItemSelectionSceneController intentionally colors the Button's root
        // Image to reflect Locked/Owned/Equipped state. That legacy opaque blue
        // fill appears through the new gold-only transparent PNG. Keep the root
        // Image active and raycastable for Button clicks, but make its pixels
        // transparent after any runtime status refresh. The independent
        // ProductionPassiveRowArt remains visible behind live item controls.
        if (rowButtonHitArea != null)
        {
            Color color = rowButtonHitArea.color;
            if (color.a != 0f)
            {
                color.a = 0f;
                rowButtonHitArea.color = color;
            }
        }
        if (status == null || lockAccent == null) return;
        // The controller writes Locked, + EQUIP, or EQUIPPED\nClick to remove.
        // Never change inventory data or the Button's interactable state.
        bool shouldShow = string.Equals(status.text?.Trim(), "Locked",
            StringComparison.OrdinalIgnoreCase);
        if (lockAccent.activeSelf != shouldShow)
            lockAccent.SetActive(shouldShow);
    }
}
