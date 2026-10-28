using TMPro;
using UnityEngine;

/// <summary>
/// Optional presentation layer for the isolated Wizard's Vault potion strip.
/// Preserves the legacy PotionCounts node so ItemSelectionSceneController keeps
/// its existing binding, but paints each potion count in its own movable TMP.
/// Inventory state is read only; this component never creates or spends potions.
/// </summary>
[DisallowMultipleComponent]
public sealed class WizardsVaultPotionCounterPresenter : MonoBehaviour
{
    private TMP_Text health;
    private TMP_Text purify;
    private TMP_Text powerUp;
    private PotionInventory inventory;
    private float nextPoll;
    private int lastHealth = int.MinValue;
    private int lastPurify = int.MinValue;
    private int lastPowerUp = int.MinValue;

    private void Awake() => ResolveLabels();

    private void OnEnable()
    {
        ResolveLabels();
        nextPoll = 0f;
        UpdateCounts();
    }

    private void Update()
    {
        // PotionInventory.GetCount consults the save database. Poll at a
        // reasonable interval instead of querying it three times every frame.
        if (Time.unscaledTime < nextPoll) return;
        nextPoll = Time.unscaledTime + 0.2f;
        UpdateCounts();
    }

    private void ResolveLabels()
    {
        if (!health) health = transform.Find("HealthCountText")?.GetComponent<TMP_Text>();
        if (!purify) purify = transform.Find("PurifyCountText")?.GetComponent<TMP_Text>();
        if (!powerUp) powerUp = transform.Find("PowerUpCountText")?.GetComponent<TMP_Text>();
    }

    private void UpdateCounts()
    {
        if (!health || !purify || !powerUp) return;
        if (!inventory)
            inventory = PotionInventory.Instance != null
                ? PotionInventory.Instance
                : FindFirstObjectByType<PotionInventory>();
        if (!inventory) return;

        int h = inventory.GetCount(PotionType.Health);
        int p = inventory.GetCount(PotionType.Cleansing);
        int u = inventory.GetCount(PotionType.PowerUp);
        // Names are three separate static TMP labels beside these live counts.
        if (h != lastHealth) { health.text = "x" + h; lastHealth = h; }
        if (p != lastPurify) { purify.text = "x" + p; lastPurify = p; }
        if (u != lastPowerUp) { powerUp.text = "x" + u; lastPowerUp = u; }
    }
}
