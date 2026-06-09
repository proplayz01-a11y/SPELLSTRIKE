using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using SpellStrike.SQLite;

[Serializable]
public class WordHistoryEntry
{
    public string word;
    public bool valid;
    public string timestamp;
}

[Serializable]
public class StageProgressEntry
{
    public int stageIndex;
    public bool completed;
    public string timestamp;
}

[Serializable]
public class PotionInventoryEntry
{
    public PotionType type;
    public int count;
}

[Serializable]
public class GameData
{
    public int playerLevel = 1;
    public int highestStageUnlocked = 0;
    public int playerHealth = 100;
    public int playerMaxHealth = 100;
    public bool tutorialCompleted = false;
    public List<WordHistoryEntry> wordHistory = new List<WordHistoryEntry>();
    public List<StageProgressEntry> stageProgress = new List<StageProgressEntry>();
    public List<PotionInventoryEntry> potionInventory = new List<PotionInventoryEntry>();
    public List<string> ownedPassiveItems = new List<string>();
    public List<string> pendingPassiveItems = new List<string>();
    public List<string> equippedPassiveItems = new List<string>();
    public List<string> leaderboard = new List<string>();
}

public class GameDatabaseManager : MonoBehaviour
{
    public static GameDatabaseManager Instance;
    public GameData Data;
    public bool useSqlite = true;

    private string saveFilePath;
    private string sqliteDbPath;
    private SqliteDatabase sqliteDatabase;
    private bool isDirty = false;

    public static GameDatabaseManager EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        GameDatabaseManager existing = FindAnyObjectOfType<GameDatabaseManager>();
        if (existing != null)
            return existing;

        GameObject databaseObject = new GameObject("GameDatabaseManager_AutoCreated");
        return databaseObject.AddComponent<GameDatabaseManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        saveFilePath = Path.Combine(Application.persistentDataPath, "spellstrike_save.json");
        sqliteDbPath = Path.Combine(Application.persistentDataPath, "spellstrike.db");

        if (useSqlite)
            InitializeDatabase();

        LoadDatabase();
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause && isDirty)
        {
            SaveDatabase();
            isDirty = false;
        }
    }

    private void OnApplicationQuit()
    {
        if (isDirty)
        {
            SaveDatabase();
            isDirty = false;
        }
    }

    // Call this at stage end, game over, or scene transitions
    public void FlushSave()
    {
        if (isDirty)
        {
            SaveDatabase();
            isDirty = false;
        }
    }

    private void InitializeDatabase()
    {
        sqliteDatabase = new SqliteDatabase();
        sqliteDatabase.Initialize(sqliteDbPath);
        if (sqliteDatabase.IsAvailable)
        {
            CreateSqliteTables();
        }
    }

    public void LoadDatabase()
    {
        if (sqliteDatabase != null && sqliteDatabase.IsAvailable && File.Exists(sqliteDbPath))
        {
            LoadFromSqlite();
            return;
        }

        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            Data = JsonUtility.FromJson<GameData>(json);
        }
        else
        {
            Data = new GameData();
            InitializeDefaultInventory();
            SaveDatabase();
        }

        if (Data.potionInventory == null || Data.potionInventory.Count == 0)
            InitializeDefaultInventory();

        EnsurePassiveInventoryLists();
    }

    public void SaveDatabase()
    {
        SaveJsonDatabase();
        if (sqliteDatabase != null && sqliteDatabase.IsAvailable)
            SaveToSqlite();
    }

    private void SaveJsonDatabase()
    {
        try
        {
            string json = JsonUtility.ToJson(Data, true);
            File.WriteAllText(saveFilePath, json);
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to save game database: " + ex.Message);
        }
    }

    private void CreateSqliteTables()
    {
        sqliteDatabase.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS player (key TEXT PRIMARY KEY, value TEXT)");
        sqliteDatabase.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS potion_inventory (type INTEGER PRIMARY KEY, count INTEGER)");
        sqliteDatabase.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS word_history (id INTEGER PRIMARY KEY AUTOINCREMENT, word TEXT, valid INTEGER, timestamp TEXT)");
        sqliteDatabase.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS stage_progress (id INTEGER PRIMARY KEY AUTOINCREMENT, stageIndex INTEGER, completed INTEGER, timestamp TEXT)");
        sqliteDatabase.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS leaderboard (id INTEGER PRIMARY KEY AUTOINCREMENT, record TEXT)");
    }

    private void LoadFromSqlite()
    {
        Data = new GameData();
        Data.highestStageUnlocked = LoadPlayerInt("highestStageUnlocked", 0);
        Data.playerHealth = LoadPlayerInt("playerHealth", 100);
        Data.playerMaxHealth = LoadPlayerInt("playerMaxHealth", 100);
        Data.playerLevel = LoadPlayerInt("playerLevel", 1);
        Data.tutorialCompleted = LoadPlayerInt("tutorialCompleted", 0) == 1;
        Data.potionInventory = LoadPotionInventory();
        if (Data.potionInventory == null || Data.potionInventory.Count == 0)
            InitializeDefaultInventory();
        Data.ownedPassiveItems = DeserializeStringList(LoadPlayerString("ownedPassiveItems", string.Empty));
        Data.pendingPassiveItems = DeserializeStringList(LoadPlayerString("pendingPassiveItems", string.Empty));
        Data.equippedPassiveItems = DeserializeStringList(LoadPlayerString("equippedPassiveItems", string.Empty));
        EnsurePassiveInventoryLists();
        Data.wordHistory = LoadWordHistory();
        Data.stageProgress = LoadStageProgress();
        Data.leaderboard = LoadLeaderboard();
    }

    private void SaveToSqlite()
    {
        SavePlayerInt("highestStageUnlocked", Data.highestStageUnlocked);
        SavePlayerInt("playerHealth", Data.playerHealth);
        SavePlayerInt("playerMaxHealth", Data.playerMaxHealth);
        SavePlayerInt("playerLevel", Data.playerLevel);
        SavePlayerInt("tutorialCompleted", Data.tutorialCompleted ? 1 : 0);

        sqliteDatabase.ExecuteNonQuery("DELETE FROM potion_inventory");
        foreach (var potion in Data.potionInventory)
        {
            sqliteDatabase.ExecuteNonQuery($"INSERT OR REPLACE INTO potion_inventory (type, count) VALUES ({(int)potion.type}, {potion.count})");
        }

        SavePlayerString("ownedPassiveItems", SerializeStringList(Data.ownedPassiveItems));
        SavePlayerString("pendingPassiveItems", SerializeStringList(Data.pendingPassiveItems));
        SavePlayerString("equippedPassiveItems", SerializeStringList(Data.equippedPassiveItems));

        sqliteDatabase.ExecuteNonQuery("DELETE FROM word_history");
        foreach (var entry in Data.wordHistory)
        {
            sqliteDatabase.ExecuteNonQuery($"INSERT INTO word_history (word, valid, timestamp) VALUES ('{SqliteDatabase.Escape(entry.word)}', {(entry.valid ? 1 : 0)}, '{SqliteDatabase.Escape(entry.timestamp)}')");
        }

        sqliteDatabase.ExecuteNonQuery("DELETE FROM stage_progress");
        foreach (var progress in Data.stageProgress)
        {
            sqliteDatabase.ExecuteNonQuery($"INSERT INTO stage_progress (stageIndex, completed, timestamp) VALUES ({progress.stageIndex}, {(progress.completed ? 1 : 0)}, '{SqliteDatabase.Escape(progress.timestamp)}')");
        }

        sqliteDatabase.ExecuteNonQuery("DELETE FROM leaderboard");
        foreach (var record in Data.leaderboard)
        {
            sqliteDatabase.ExecuteNonQuery($"INSERT INTO leaderboard (record) VALUES ('{SqliteDatabase.Escape(record)}')");
        }
    }

    private int LoadPlayerInt(string key, int defaultValue)
    {
        object scalar = sqliteDatabase.ExecuteScalar($"SELECT value FROM player WHERE key = '{SqliteDatabase.Escape(key)}'");
        if (scalar == null)
            return defaultValue;

        if (int.TryParse(scalar.ToString(), out int result))
            return result;

        return defaultValue;
    }

    private string LoadPlayerString(string key, string defaultValue)
    {
        object scalar = sqliteDatabase.ExecuteScalar($"SELECT value FROM player WHERE key = '{SqliteDatabase.Escape(key)}'");
        if (scalar == null)
            return defaultValue;

        return scalar.ToString();
    }

    private void SavePlayerInt(string key, int value)
    {
        sqliteDatabase.ExecuteNonQuery($"INSERT OR REPLACE INTO player (key, value) VALUES ('{SqliteDatabase.Escape(key)}', '{value}')");
    }

    private void SavePlayerString(string key, string value)
    {
        sqliteDatabase.ExecuteNonQuery($"INSERT OR REPLACE INTO player (key, value) VALUES ('{SqliteDatabase.Escape(key)}', '{SqliteDatabase.Escape(value)}')");
    }

    private List<PotionInventoryEntry> LoadPotionInventory()
    {
        var list = new List<PotionInventoryEntry>();
        var rows = sqliteDatabase.ExecuteQuery("SELECT type, count FROM potion_inventory");
        foreach (var row in rows)
        {
            if (row.TryGetValue("type", out object typeObj) && row.TryGetValue("count", out object countObj))
            {
                if (int.TryParse(typeObj.ToString(), out int typeValue) && int.TryParse(countObj.ToString(), out int countValue))
                {
                    list.Add(new PotionInventoryEntry { type = (PotionType)typeValue, count = countValue });
                }
            }
        }

        return list;
    }

    private List<WordHistoryEntry> LoadWordHistory()
    {
        var list = new List<WordHistoryEntry>();
        var rows = sqliteDatabase.ExecuteQuery("SELECT word, valid, timestamp FROM word_history ORDER BY id ASC");
        foreach (var row in rows)
        {
            list.Add(new WordHistoryEntry
            {
                word = row.TryGetValue("word", out object wordObj) ? wordObj.ToString() : string.Empty,
                valid = row.TryGetValue("valid", out object validObj) && validObj.ToString() == "1",
                timestamp = row.TryGetValue("timestamp", out object timeObj) ? timeObj.ToString() : string.Empty
            });
        }

        return list;
    }

    private List<StageProgressEntry> LoadStageProgress()
    {
        var list = new List<StageProgressEntry>();
        var rows = sqliteDatabase.ExecuteQuery("SELECT stageIndex, completed, timestamp FROM stage_progress ORDER BY id ASC");
        foreach (var row in rows)
        {
            if (int.TryParse(row.TryGetValue("stageIndex", out object stageObj) ? stageObj.ToString() : "0", out int stageIndex) &&
                int.TryParse(row.TryGetValue("completed", out object completedObj) ? completedObj.ToString() : "0", out int completedValue))
            {
                list.Add(new StageProgressEntry
                {
                    stageIndex = stageIndex,
                    completed = completedValue == 1,
                    timestamp = row.TryGetValue("timestamp", out object timeObj) ? timeObj.ToString() : string.Empty
                });
            }
        }

        return list;
    }

    private List<string> LoadLeaderboard()
    {
        var list = new List<string>();
        var rows = sqliteDatabase.ExecuteQuery("SELECT record FROM leaderboard ORDER BY id ASC");
        foreach (var row in rows)
        {
            if (row.TryGetValue("record", out object recordObj))
                list.Add(recordObj.ToString());
        }

        return list;
    }

    public void InitializeDefaultInventory()
    {
        Data.potionInventory ??= new List<PotionInventoryEntry>();
        Data.potionInventory.Clear();
        Data.potionInventory.Add(new PotionInventoryEntry { type = PotionType.Health, count = 1 });
        Data.potionInventory.Add(new PotionInventoryEntry { type = PotionType.PowerUp, count = 1 });
        Data.potionInventory.Add(new PotionInventoryEntry { type = PotionType.Cleansing, count = 1 });
        EnsurePassiveInventoryLists();
    }

    public void EnsurePassiveInventoryLists(int equippedSlotCount = 3)
    {
        Data.ownedPassiveItems ??= new List<string>();
        Data.pendingPassiveItems ??= new List<string>();
        Data.equippedPassiveItems ??= new List<string>();

        for (int i = Data.equippedPassiveItems.Count; i < equippedSlotCount; i++)
            Data.equippedPassiveItems.Add(PassiveItemId.None.ToString());

        while (Data.equippedPassiveItems.Count > equippedSlotCount)
            Data.equippedPassiveItems.RemoveAt(Data.equippedPassiveItems.Count - 1);
    }

    private string SerializeStringList(List<string> values)
    {
        if (values == null || values.Count == 0)
            return string.Empty;

        return string.Join("|", values);
    }

    private List<string> DeserializeStringList(string serialized)
    {
        List<string> values = new List<string>();
        if (string.IsNullOrWhiteSpace(serialized))
            return values;

        string[] parts = serialized.Split('|');
        foreach (string part in parts)
        {
            if (!string.IsNullOrWhiteSpace(part))
                values.Add(part);
        }

        return values;
    }

    public void RecordWordHistory(string word, bool valid)
    {
        Data.wordHistory.Add(new WordHistoryEntry
        {
            word = word,
            valid = valid,
            timestamp = DateTime.UtcNow.ToString("o")
        });
        isDirty = true;
    }

    public void SaveStageProgress(int stageIndex, bool completed)
    {
        Data.highestStageUnlocked = Mathf.Max(Data.highestStageUnlocked, stageIndex);
        Data.stageProgress.Add(new StageProgressEntry
        {
            stageIndex = stageIndex,
            completed = completed,
            timestamp = DateTime.UtcNow.ToString("o")
        });
        isDirty = true;
    }

    public void SetTutorialCompleted(bool completed)
    {
        Data.tutorialCompleted = completed;
        isDirty = true;
    }

    public void AddPotion(PotionType type, int count)
    {
        var entry = Data.potionInventory.Find(p => p.type == type);
        if (entry == null)
        {
            Data.potionInventory.Add(new PotionInventoryEntry { type = type, count = count });
        }
        else
        {
            entry.count += count;
        }
        isDirty = true;
    }

    public int GetPotionCount(PotionType type)
    {
        var entry = Data.potionInventory.Find(p => p.type == type);
        return entry != null ? entry.count : 0;
    }

    public void AddLeaderboardEntry(string record)
    {
        Data.leaderboard.Add(record);
        isDirty = true;
    }

    public void ResetDatabase()
    {
        Data = new GameData();
        InitializeDefaultInventory();
        SaveDatabase();
        isDirty = false;
    }

    private static T FindAnyObjectOfType<T>() where T : UnityEngine.Object
    {
#if UNITY_2023_1_OR_NEWER
        return UnityEngine.Object.FindFirstObjectByType<T>();
#else
        return UnityEngine.Object.FindObjectOfType<T>();
#endif
    }
}
