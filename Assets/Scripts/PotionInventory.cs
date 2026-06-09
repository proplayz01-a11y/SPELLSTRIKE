using System.Collections.Generic;
using UnityEngine;

public class PotionInventory : MonoBehaviour
{
    public static PotionInventory Instance;

    [Header("Potion Caps")]
    public int maxPotionCount = 20;

    [Header("Fallback Counts")]
    [SerializeField] private int fallbackHealthPotions = 0;
    [SerializeField] private int fallbackPurifyPotions = 0;
    [SerializeField] private int fallbackPowerUpPotions = 0;

    [Header("Temporary Debug Keys")]
    public bool enableDebugKeys = true;
    public KeyCode addHealthKey = KeyCode.Alpha7;
    public KeyCode addPurifyKey = KeyCode.Alpha8;
    public KeyCode addPowerUpKey = KeyCode.Alpha9;
    public KeyCode logCountsKey = KeyCode.Alpha0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        GameDatabaseManager.EnsureInstance();
        EnsurePotionEntries();
        ClampAllPotionCounts();
        SyncLegacyPotionFields();
    }

    private void Start()
    {
        GameDatabaseManager.EnsureInstance();
        EnsurePotionEntries();
        ClampAllPotionCounts();
        SyncLegacyPotionFields();
    }

    private void Update()
    {
        if (!enableDebugKeys)
            return;

        bool spendMode = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (Input.GetKeyDown(addHealthKey))
            DebugAdjustPotion(PotionType.Health, spendMode);

        if (Input.GetKeyDown(addPurifyKey))
            DebugAdjustPotion(PotionType.Cleansing, spendMode);

        if (Input.GetKeyDown(addPowerUpKey))
            DebugAdjustPotion(PotionType.PowerUp, spendMode);

        if (Input.GetKeyDown(logCountsKey))
            LogPotionCounts();
    }

    public int GetCount(PotionType type)
    {
        EnsurePotionEntries();
        PotionInventoryEntry entry = GetDatabaseEntry(type);
        if (entry != null)
            return Mathf.Clamp(entry.count, 0, maxPotionCount);

        return GetFallbackCount(type);
    }

    public int AddPotion(PotionType type, int amount = 1)
    {
        if (amount <= 0)
            return 0;

        int current = GetCount(type);
        int next = Mathf.Clamp(current + amount, 0, maxPotionCount);
        int added = next - current;

        SetCount(type, next);
        Debug.Log($"[PotionInventory] Added {added} {GetDisplayName(type)}. Count: {next}/{maxPotionCount}");
        return added;
    }

    public bool TryUsePotion(PotionType type, int amount = 1)
    {
        if (amount <= 0)
            return false;

        int current = GetCount(type);
        if (current < amount)
        {
            Debug.Log($"[PotionInventory] Not enough {GetDisplayName(type)}. Count: {current}/{maxPotionCount}");
            return false;
        }

        int next = current - amount;
        SetCount(type, next);
        Debug.Log($"[PotionInventory] Used {amount} {GetDisplayName(type)}. Count: {next}/{maxPotionCount}");
        return true;
    }

    public void SetCount(PotionType type, int count)
    {
        EnsurePotionEntries();
        int clampedCount = Mathf.Clamp(count, 0, maxPotionCount);
        PotionInventoryEntry entry = GetDatabaseEntry(type);

        if (entry != null)
        {
            entry.count = clampedCount;
            SaveDatabase();
        }
        else
        {
            SetFallbackCount(type, clampedCount);
        }

        SyncLegacyPotionFields();
    }

    public bool HasPotion(PotionType type)
    {
        return GetCount(type) > 0;
    }

    public void LogPotionCounts()
    {
        Debug.Log($"[PotionInventory] Health: {GetCount(PotionType.Health)}/{maxPotionCount} | Purify: {GetCount(PotionType.Cleansing)}/{maxPotionCount} | Power Up: {GetCount(PotionType.PowerUp)}/{maxPotionCount}");
    }

    public static string GetDisplayName(PotionType type)
    {
        switch (type)
        {
            case PotionType.Health:
                return "Health Potion";
            case PotionType.Cleansing:
                return "Purify Potion";
            case PotionType.PowerUp:
                return "Power Up Potion";
            default:
                return type.ToString();
        }
    }

    private void DebugAdjustPotion(PotionType type, bool spendMode)
    {
        if (spendMode)
            TryUsePotion(type);
        else
            AddPotion(type);
    }

    private void EnsurePotionEntries()
    {
        GameDatabaseManager database = GameDatabaseManager.EnsureInstance();
        if (database == null || database.Data == null)
            return;

        database.Data.potionInventory ??= new List<PotionInventoryEntry>();
        EnsureDatabaseEntry(PotionType.Health);
        EnsureDatabaseEntry(PotionType.Cleansing);
        EnsureDatabaseEntry(PotionType.PowerUp);
    }

    private void EnsureDatabaseEntry(PotionType type)
    {
        GameDatabaseManager database = GameDatabaseManager.EnsureInstance();
        if (database == null || database.Data == null)
            return;

        if (database.Data.potionInventory.Find(p => p.type == type) == null)
            database.Data.potionInventory.Add(new PotionInventoryEntry { type = type, count = 0 });
    }

    private PotionInventoryEntry GetDatabaseEntry(PotionType type)
    {
        GameDatabaseManager database = GameDatabaseManager.EnsureInstance();
        if (database == null || database.Data == null || database.Data.potionInventory == null)
            return null;

        return database.Data.potionInventory.Find(p => p.type == type);
    }

    private void ClampAllPotionCounts()
    {
        SetCount(PotionType.Health, GetCount(PotionType.Health));
        SetCount(PotionType.Cleansing, GetCount(PotionType.Cleansing));
        SetCount(PotionType.PowerUp, GetCount(PotionType.PowerUp));
    }

    private int GetFallbackCount(PotionType type)
    {
        switch (type)
        {
            case PotionType.Health:
                return Mathf.Clamp(fallbackHealthPotions, 0, maxPotionCount);
            case PotionType.Cleansing:
                return Mathf.Clamp(fallbackPurifyPotions, 0, maxPotionCount);
            case PotionType.PowerUp:
                return Mathf.Clamp(fallbackPowerUpPotions, 0, maxPotionCount);
            default:
                return 0;
        }
    }

    private void SetFallbackCount(PotionType type, int count)
    {
        int clampedCount = Mathf.Clamp(count, 0, maxPotionCount);
        switch (type)
        {
            case PotionType.Health:
                fallbackHealthPotions = clampedCount;
                break;
            case PotionType.Cleansing:
                fallbackPurifyPotions = clampedCount;
                break;
            case PotionType.PowerUp:
                fallbackPowerUpPotions = clampedCount;
                break;
        }
    }

    private void SaveDatabase()
    {
        GameDatabaseManager database = GameDatabaseManager.EnsureInstance();
        if (database != null)
            database.SaveDatabase();
    }

    private void SyncLegacyPotionFields()
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            gameManager.healthPotions = GetCount(PotionType.Health);
            gameManager.cleansingPotions = GetCount(PotionType.Cleansing);
            gameManager.powerUpPotions = GetCount(PotionType.PowerUp);
        }

        PotionSystem potionSystem = PotionSystem.Instance;
        if (potionSystem != null)
            potionSystem.SyncReadOnlyCountMirror(this);
    }
}
