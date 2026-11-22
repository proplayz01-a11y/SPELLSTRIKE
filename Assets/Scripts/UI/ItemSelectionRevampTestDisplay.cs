using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Presentation-only bindings for the isolated Item Selection revamp test.
/// Authoritative inventory and equipping remain in ItemSelectionSceneController
/// and PassiveItemInventory. Never attach this component to the production Vault.
/// </summary>
public sealed class ItemSelectionRevampTestDisplay : MonoBehaviour
{
    [SerializeField] private GameObject[] emptyLayouts;
    [SerializeField] private GameObject[] equippedLayouts;
    [SerializeField] private TMP_Text[] equippedNames;
    [SerializeField] private Image[] equippedIcons;
    [SerializeField] private TMP_Text equippedCounter;
    [SerializeField] private TMP_Text[] potionCounters;
    [SerializeField] private Sprite skySwordIcon;
    [SerializeField] private Sprite greatAttractorIcon;
    [SerializeField] private Sprite purifyingPearlIcon;
    [SerializeField] private Sprite tidecarvedShellIcon;
#if UNITY_EDITOR
    public void Configure(GameObject[] empty, GameObject[] equipped,
        TMP_Text[] names, Image[] icons, TMP_Text equippedCount, TMP_Text[] potions,
        Sprite sky, Sprite attractor, Sprite pearl, Sprite shell)
    {
        emptyLayouts = empty;
        equippedLayouts = equipped;
        equippedNames = names;
        equippedIcons = icons;
        equippedCounter = equippedCount;
        potionCounters = potions;
        skySwordIcon = sky;
        greatAttractorIcon = attractor;
        purifyingPearlIcon = pearl;
        tidecarvedShellIcon = shell;
    }

    // Preview controls are editor-only and do not modify saved gameplay data.
    // They only switch visibility and sample sprites/names in the test prefab.
    public void PreviewLayout(bool occupied)
    {
        PassiveItemId[] sample = {
            PassiveItemId.SkySword, PassiveItemId.GreatAttractor,
            PassiveItemId.PurifyingPearl
        };
        for (int i = 0; i < 3; i++)
        {
            if (emptyLayouts != null && i < emptyLayouts.Length && emptyLayouts[i] != null)
                emptyLayouts[i].SetActive(!occupied);
            if (equippedLayouts != null && i < equippedLayouts.Length && equippedLayouts[i] != null)
                equippedLayouts[i].SetActive(occupied);
            if (!occupied) continue;
            if (equippedNames != null && i < equippedNames.Length && equippedNames[i] != null)
                equippedNames[i].text = PassiveItemCatalog.GetShortName(sample[i]);
            if (equippedIcons != null && i < equippedIcons.Length && equippedIcons[i] != null)
                equippedIcons[i].sprite = IconFor(sample[i]);
        }
    }
#endif

    private void LateUpdate()
    {
        PassiveItemInventory inventory = PassiveItemInventory.Instance;
        int count = 0;
        for (int i = 0; equippedNames != null && i < equippedNames.Length; i++)
        {
            PassiveItemId passive = inventory != null
                ? inventory.GetEquippedPassive(i) : PassiveItemId.None;
            bool occupied = passive != PassiveItemId.None;
            if (occupied)
                count++;
            if (emptyLayouts != null && i < emptyLayouts.Length && emptyLayouts[i] != null
                && emptyLayouts[i].activeSelf == occupied)
                emptyLayouts[i].SetActive(!occupied);
            if (equippedLayouts != null && i < equippedLayouts.Length && equippedLayouts[i] != null
                && equippedLayouts[i].activeSelf != occupied)
                equippedLayouts[i].SetActive(occupied);
            if (equippedNames[i] != null)
            {
                string next = occupied ? PassiveItemCatalog.GetShortName(passive) : string.Empty;
                if (occupied && equippedNames[i].text != next)
                    equippedNames[i].text = next;
            }
            if (equippedIcons != null && i < equippedIcons.Length && equippedIcons[i] != null)
            {
                Sprite next = IconFor(passive);
                Image icon = equippedIcons[i];
                if (icon.sprite != next)
                    icon.sprite = next;
                if (icon.gameObject.activeSelf != (next != null))
                    icon.gameObject.SetActive(next != null);
            }
        }
        if (equippedCounter != null)
        {
            string text = count + " / 3 Equipped";
            if (equippedCounter.text != text) equippedCounter.text = text;
        }

        PotionInventory potions = PotionInventory.Instance;
        if (potions == null || potionCounters == null)
            return;
        PotionType[] types = { PotionType.Health, PotionType.Cleansing, PotionType.PowerUp };
        for (int i = 0; i < potionCounters.Length && i < types.Length; i++)
        {
            if (potionCounters[i] == null) continue;
            string text = "x" + potions.GetCount(types[i]);
            if (potionCounters[i].text != text) potionCounters[i].text = text;
        }
    }

    private Sprite IconFor(PassiveItemId passive)
    {
        switch (passive)
        {
            case PassiveItemId.SkySword: return skySwordIcon;
            case PassiveItemId.GreatAttractor: return greatAttractorIcon;
            case PassiveItemId.PurifyingPearl: return purifyingPearlIcon;
            case PassiveItemId.TidecarvedShell: return tidecarvedShellIcon;
            default: return null;
        }
    }
}

