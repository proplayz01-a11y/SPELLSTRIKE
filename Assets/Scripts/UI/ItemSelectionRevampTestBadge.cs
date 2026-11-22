using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Test-only passive badge visual; does not change inventory state.</summary>
public sealed class ItemSelectionRevampTestBadge : MonoBehaviour
{
    [SerializeField] private PassiveItemId passive;
    [SerializeField] private Image plate;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Sprite lockedPlate;
    [SerializeField] private Sprite equippedPlate;
    private static readonly Color Dark = new Color(0.14f, 0.09f, 0.04f, 1f);
    private static readonly Color Gold = new Color(1f, 0.89f, 0.69f, 1f);

#if UNITY_EDITOR
    public void Configure(PassiveItemId id, Image image, TMP_Text text,
        Sprite locked, Sprite equipped)
    {
        passive = id;
        plate = image;
        label = text;
        lockedPlate = locked;
        equippedPlate = equipped;
    }
#endif

    private void LateUpdate()
    {
        PassiveItemInventory inventory = PassiveItemInventory.Instance;
        bool owned = inventory != null && inventory.HasOwnedPassive(passive);
        bool equipped = inventory != null && inventory.IsEquipped(passive);
        // Legacy Vault dims the entire equipped row to 64% opacity. That was
        // designed for navy cards; on the new pale parchment it hurts text
        // readability. Preserve full row opacity for both equipped and owned.
        CanvasGroup rowFade = GetComponent<CanvasGroup>();
        if (rowFade != null && !Mathf.Approximately(rowFade.alpha, 1f))
            rowFade.alpha = 1f;
        // The decorative StatePlate belongs to LOCKED items only. Once an
        // item is owned, remove the plate without removing the status label,
        // so Equip/Equipped stays readable and the row remains clickable.
        if (plate != null)
        {
            if (plate.sprite != lockedPlate) plate.sprite = lockedPlate;
            if (plate.gameObject.activeSelf == owned)
                plate.gameObject.SetActive(!owned);
        }
        if (label == null) return;
        string wantedText = !owned ? "Locked" : equipped ? "Equipped" : "Equip";
        if (label.text != wantedText) label.text = wantedText;
        Color color = owned ? Dark : Gold;
        if (label.color != color) label.color = color;
    }
}
