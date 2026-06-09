using System.Collections.Generic;
using UnityEngine;

public class PassiveItemInventory : MonoBehaviour
{
    public static PassiveItemInventory Instance;

    [Header("Equipment")]
    public int maxEquippedPassives = 3;

    [Header("Temporary Debug Keys")]
    public bool enableDebugKeys = false;
    public KeyCode grantSkySwordKey = KeyCode.F6;
    public KeyCode grantGreatAttractorKey = KeyCode.F7;
    public KeyCode logInventoryKey = KeyCode.F8;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsurePassiveLists();
    }

    private void Start()
    {
        EnsurePassiveLists();
    }

    private void Update()
    {
        if (!enableDebugKeys)
            return;

        if (Input.GetKeyDown(grantSkySwordKey))
            AddPendingPassive(PassiveItemId.SkySword, "Debug");

        if (Input.GetKeyDown(grantGreatAttractorKey))
            AddPendingPassive(PassiveItemId.GreatAttractor, "Debug");

        if (Input.GetKeyDown(logInventoryKey))
            LogInventory();
    }

    public static PassiveItemInventory GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        PassiveItemInventory existing = FindFirstObjectByType<PassiveItemInventory>();
        if (existing != null)
            return existing;

        GameObject inventoryObject = new GameObject("PassiveItemInventory_AutoCreated");
        return inventoryObject.AddComponent<PassiveItemInventory>();
    }

    public IReadOnlyList<string> GetOwnedPassiveKeys()
    {
        EnsurePassiveLists();
        return GameDatabaseManager.EnsureInstance().Data.ownedPassiveItems;
    }

    public IReadOnlyList<string> GetPendingPassiveKeys()
    {
        EnsurePassiveLists();
        return GameDatabaseManager.EnsureInstance().Data.pendingPassiveItems;
    }

    public PassiveItemId GetEquippedPassive(int slotIndex)
    {
        EnsurePassiveLists();

        if (slotIndex < 0 || slotIndex >= maxEquippedPassives)
            return PassiveItemId.None;

        List<string> equipped = GameDatabaseManager.EnsureInstance().Data.equippedPassiveItems;
        return ParsePassiveId(equipped[slotIndex]);
    }

    public bool HasOwnedPassive(PassiveItemId itemId)
    {
        EnsurePassiveLists();
        return ContainsPassive(GameDatabaseManager.EnsureInstance().Data.ownedPassiveItems, itemId);
    }

    public bool HasPendingPassive(PassiveItemId itemId)
    {
        EnsurePassiveLists();
        return ContainsPassive(GameDatabaseManager.EnsureInstance().Data.pendingPassiveItems, itemId);
    }

    public bool IsEquipped(PassiveItemId itemId)
    {
        EnsurePassiveLists();
        return ContainsPassive(GameDatabaseManager.EnsureInstance().Data.equippedPassiveItems, itemId);
    }

    public bool AddPendingPassive(PassiveItemId itemId, string source = "Reward")
    {
        if (itemId == PassiveItemId.None)
            return false;

        EnsurePassiveLists();

        if (HasOwnedPassive(itemId) || HasPendingPassive(itemId))
        {
            Debug.Log($"[PassiveItemInventory] {PassiveItemCatalog.GetDisplayName(itemId)} already owned or pending.");
            return false;
        }

        GameDatabaseManager database = GameDatabaseManager.EnsureInstance();
        database.Data.pendingPassiveItems.Add(itemId.ToString());
        database.SaveDatabase();

        Debug.Log($"[PassiveItemInventory] Pending passive added from {source}: {PassiveItemCatalog.GetDisplayName(itemId)}.");
        return true;
    }

    public int PromotePendingPassivesToOwned()
    {
        EnsurePassiveLists();

        GameDatabaseManager database = GameDatabaseManager.EnsureInstance();
        List<string> pending = database.Data.pendingPassiveItems;
        List<string> owned = database.Data.ownedPassiveItems;
        int promotedCount = 0;

        foreach (string key in pending)
        {
            PassiveItemId itemId = ParsePassiveId(key);
            if (itemId == PassiveItemId.None)
                continue;

            if (ContainsPassive(owned, itemId))
                continue;

            owned.Add(itemId.ToString());
            promotedCount++;
            Debug.Log($"[PassiveItemInventory] Passive now available: {PassiveItemCatalog.GetDisplayName(itemId)}.");
        }

        if (pending.Count > 0)
        {
            pending.Clear();
            database.SaveDatabase();
        }

        return promotedCount;
    }

    public bool TryEquipPassive(PassiveItemId itemId)
    {
        if (itemId == PassiveItemId.None)
            return false;

        EnsurePassiveLists();

        if (!HasOwnedPassive(itemId))
        {
            Debug.Log($"[PassiveItemInventory] Cannot equip locked passive: {PassiveItemCatalog.GetDisplayName(itemId)}.");
            return false;
        }

        if (IsEquipped(itemId))
            return true;

        GameDatabaseManager database = GameDatabaseManager.EnsureInstance();
        List<string> equipped = database.Data.equippedPassiveItems;

        for (int i = 0; i < maxEquippedPassives; i++)
        {
            if (ParsePassiveId(equipped[i]) != PassiveItemId.None)
                continue;

            equipped[i] = itemId.ToString();
            database.SaveDatabase();
            Debug.Log($"[PassiveItemInventory] Equipped {PassiveItemCatalog.GetDisplayName(itemId)} in slot {i + 1}.");
            return true;
        }

        Debug.Log("[PassiveItemInventory] Cannot equip passive. All passive slots are full.");
        return false;
    }

    public bool TryUnequipPassive(PassiveItemId itemId)
    {
        EnsurePassiveLists();

        GameDatabaseManager database = GameDatabaseManager.EnsureInstance();
        List<string> equipped = database.Data.equippedPassiveItems;

        for (int i = 0; i < maxEquippedPassives; i++)
        {
            if (ParsePassiveId(equipped[i]) != itemId)
                continue;

            equipped[i] = PassiveItemId.None.ToString();
            database.SaveDatabase();
            Debug.Log($"[PassiveItemInventory] Unequipped {PassiveItemCatalog.GetDisplayName(itemId)}.");
            return true;
        }

        return false;
    }

    public void ClearEquippedPassives()
    {
        EnsurePassiveLists();

        GameDatabaseManager database = GameDatabaseManager.EnsureInstance();
        for (int i = 0; i < maxEquippedPassives; i++)
            database.Data.equippedPassiveItems[i] = PassiveItemId.None.ToString();

        database.SaveDatabase();
        Debug.Log("[PassiveItemInventory] Equipped passives cleared.");
    }

    public void LogInventory()
    {
        EnsurePassiveLists();

        GameDatabaseManager database = GameDatabaseManager.EnsureInstance();
        Debug.Log($"[PassiveItemInventory] Owned: {string.Join(", ", database.Data.ownedPassiveItems)} | Pending: {string.Join(", ", database.Data.pendingPassiveItems)} | Equipped: {string.Join(", ", database.Data.equippedPassiveItems)}");
    }

    private void EnsurePassiveLists()
    {
        GameDatabaseManager database = GameDatabaseManager.EnsureInstance();
        if (database == null || database.Data == null)
            return;

        database.EnsurePassiveInventoryLists(maxEquippedPassives);
    }

    private bool ContainsPassive(List<string> list, PassiveItemId itemId)
    {
        foreach (string key in list)
        {
            if (ParsePassiveId(key) == itemId)
                return true;
        }

        return false;
    }

    private PassiveItemId ParsePassiveId(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return PassiveItemId.None;

        if (System.Enum.TryParse(key, out PassiveItemId itemId))
            return itemId;

        return PassiveItemId.None;
    }
}
