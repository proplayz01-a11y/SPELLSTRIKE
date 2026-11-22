using TMPro;
using UnityEngine;

/// <summary>Presentation-only potion counts for the isolated revamp test.</summary>
public sealed class ItemSelectionRevampTestPotions : MonoBehaviour
{
    [SerializeField] private TMP_Text[] counters;

#if UNITY_EDITOR
    public void Configure(TMP_Text[] labels) { counters = labels; }
#endif

    private void LateUpdate()
    {
        PotionInventory inventory = PotionInventory.Instance;
        if (inventory == null || counters == null) return;
        PotionType[] types = { PotionType.Health, PotionType.Cleansing, PotionType.PowerUp };
        for (int i = 0; i < 3 && i < counters.Length; i++)
        {
            if (counters[i] == null) continue;
            string wanted = "x" + inventory.GetCount(types[i]);
            if (counters[i].text != wanted) counters[i].text = wanted;
        }
    }
}
