using UnityEngine;

public enum PotionType
{
    Health,
    PowerUp,
    Cleansing
}

public class PotionSystem : MonoBehaviour
{
    public static PotionSystem Instance;

    [Header("References")]
    public PotionInventory potionInventory;
    public PlayerHealth playerHealth;
    public TileDebuffManager tileDebuffManager;

    [Header("Potion Effects")]
    [Range(0f, 1f)] public float healthRestorePercent = 0.4f;
    public bool purifyRestoresBrokenTiles = true;
    public float powerUpMultiplier = 1.25f;
    public int powerUpAttackCount = 2;

    private int maxTotalPotions = 20;
    private int healthPotions = 0;
    private int powerUpPotions = 0;
    private int cleansingPotions = 0;

    private int powerUpAttacksRemaining = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        ResolveReferences();
        SyncLegacyCounts();
    }

    private void Start()
    {
        ResolveReferences();
        SyncLegacyCounts();
    }

    public bool UsePotion(PotionType type)
    {
        ResolveReferences();

        switch (type)
        {
            case PotionType.Health:
                return UseHealthPotion();
            case PotionType.Cleansing:
                return UsePurifyPotion();
            case PotionType.PowerUp:
                return UsePowerUpPotion();
            default:
                return false;
        }
    }

    public float GetDamageMultiplier()
    {
        return powerUpAttacksRemaining > 0 ? powerUpMultiplier : 1f;
    }

    public void ConsumePowerUpAttackCharge()
    {
        if (powerUpAttacksRemaining <= 0)
            return;

        powerUpAttacksRemaining = Mathf.Max(0, powerUpAttacksRemaining - 1);
        Debug.Log($"[Potion] Power Up bonus applied. Remaining boosted attack(s): {powerUpAttacksRemaining}");

        if (powerUpAttacksRemaining == 0)
            Debug.Log("[Potion] Power Up expired.");
    }

    public bool IsPowerUpActive()
    {
        return powerUpAttacksRemaining > 0;
    }

    public int GetPowerUpAttacksRemaining()
    {
        return powerUpAttacksRemaining;
    }

    public int MaxTotalPotions => maxTotalPotions;
    public int HealthPotions => healthPotions;
    public int PowerUpPotions => powerUpPotions;
    public int CleansingPotions => cleansingPotions;

    private bool UseHealthPotion()
    {
        if (playerHealth == null)
            playerHealth = FindAnyObjectOfType<PlayerHealth>();

        if (playerHealth == null)
        {
            Debug.LogWarning("[Potion] Health Potion failed. PlayerHealth not found.");
            return false;
        }

        if (playerHealth.currentHealth >= playerHealth.maxHealth)
        {
            Debug.Log("[Potion] Health Potion not used. Player is already at full HP.");
            return false;
        }

        if (!TryConsumePotion(PotionType.Health))
            return false;

        int beforeHealth = playerHealth.currentHealth;
        int healAmount = Mathf.CeilToInt(playerHealth.maxHealth * healthRestorePercent);
        playerHealth.Heal(healAmount);
        int restoredAmount = playerHealth.currentHealth - beforeHealth;

        Debug.Log($"[Potion] Health Potion used. Restored {restoredAmount} HP.");
        return true;
    }

    private bool UsePurifyPotion()
    {
        if (tileDebuffManager == null)
            tileDebuffManager = FindAnyObjectOfType<TileDebuffManager>();

        if (tileDebuffManager == null)
        {
            Debug.LogWarning("[Potion] Purify Potion failed. TileDebuffManager not found.");
            return false;
        }

        int clearableDebuffs = tileDebuffManager.CountActiveTileDebuffs(purifyRestoresBrokenTiles);
        if (clearableDebuffs <= 0)
        {
            Debug.Log("[Potion] Purify Potion not used. No active debuffs found.");
            return false;
        }

        if (!TryConsumePotion(PotionType.Cleansing))
            return false;

        tileDebuffManager.ClearAllTileDebuffs(purifyRestoresBrokenTiles);
        Debug.Log("[Potion] Purify Potion used. Debuffs cleared.");
        return true;
    }

    private bool UsePowerUpPotion()
    {
        if (powerUpAttacksRemaining > 0)
        {
            Debug.Log($"[Potion] Power Up Potion not used. Power Up already active for {powerUpAttacksRemaining} attack(s).");
            return false;
        }

        if (!TryConsumePotion(PotionType.PowerUp))
            return false;

        powerUpAttacksRemaining = Mathf.Max(1, powerUpAttackCount);
        Debug.Log($"[Potion] Power Up active for next {powerUpAttacksRemaining} attacks.");
        return true;
    }

    private bool TryConsumePotion(PotionType type)
    {
        if (potionInventory == null)
            potionInventory = PotionInventory.Instance != null ? PotionInventory.Instance : FindAnyObjectOfType<PotionInventory>();

        if (potionInventory != null)
        {
            bool used = potionInventory.TryUsePotion(type);
            SyncLegacyCounts();
            return used;
        }

        Debug.LogWarning("[Potion] Potion use failed. PotionInventory not found.");
        return false;
    }

    private void ResolveReferences()
    {
        if (potionInventory == null)
            potionInventory = PotionInventory.Instance != null ? PotionInventory.Instance : FindAnyObjectOfType<PotionInventory>();

        if (playerHealth == null)
            playerHealth = FindAnyObjectOfType<PlayerHealth>();

        if (tileDebuffManager == null)
            tileDebuffManager = FindAnyObjectOfType<TileDebuffManager>();
    }

    private void SyncLegacyCounts()
    {
        if (potionInventory == null)
            return;

        healthPotions = potionInventory.GetCount(PotionType.Health);
        cleansingPotions = potionInventory.GetCount(PotionType.Cleansing);
        powerUpPotions = potionInventory.GetCount(PotionType.PowerUp);
        maxTotalPotions = potionInventory.maxPotionCount;
    }

    public void SyncReadOnlyCountMirror(PotionInventory inventory)
    {
        if (inventory == null)
            return;

        potionInventory = inventory;
        SyncLegacyCounts();
    }

    private static T FindAnyObjectOfType<T>() where T : UnityEngine.Object
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<T>();
#else
        return Object.FindObjectOfType<T>();
#endif
    }
}
