using TMPro;
using UnityEngine;

/// <summary>Runtime-only instruction, no changes to equipped items.</summary>
[RequireComponent(typeof(TMP_Text))]
public sealed class ItemSelectionPlatformHint : MonoBehaviour
{
    private void Awake()
    {
        TMP_Text label = GetComponent<TMP_Text>();
        if (label == null) return;
        label.text = InstructionFor(Application.platform);
    }

    public static string InstructionFor(RuntimePlatform platform)
    {
        return ItemSelectionSlotUnequipInput.IsMobile(platform)
            ? "Tap and hold to unequip"
            : "Right-click to unequip";
    }
}
