using System;
using System.IO;
using SpellStrike.SQLite;
using UnityEditor;
using UnityEngine;

public static class OnboardingStateTestMenu
{
    private const string PrologueCompletionKey = "SpellStrike_PrologueCompleted";
    private const string TutorialCompletionKey = "SpellStrike_TutorialCompleted";

    [MenuItem("SPELLSTRIKE/Testing/Onboarding/Set Fresh Player")]
    private static void SetFreshPlayer()
    {
        ApplyState(false, false, "Fresh Player");
    }

    [MenuItem("SPELLSTRIKE/Testing/Onboarding/Set Prologue Complete Only")]
    private static void SetPrologueCompleteOnly()
    {
        ApplyState(true, false, "Prologue Complete Only");
    }

    [MenuItem("SPELLSTRIKE/Testing/Onboarding/Set Both Complete")]
    private static void SetBothComplete()
    {
        ApplyState(true, true, "Both Complete");
    }

    [MenuItem("SPELLSTRIKE/Testing/Onboarding/Log Current State")]
    private static void LogCurrentState()
    {
        bool prologueCompleted = PlayerPrefs.GetInt(PrologueCompletionKey, 0) == 1;
        bool tutorialPlayerPrefs = PlayerPrefs.GetInt(TutorialCompletionKey, 0) == 1;

        string jsonPath = Path.Combine(Application.persistentDataPath, "spellstrike_save.json");
        string sqlitePath = Path.Combine(Application.persistentDataPath, "spellstrike.db");

        string jsonState = ReadJsonTutorialState(jsonPath);
        string sqliteState = ReadSqliteTutorialState(sqlitePath);

        Debug.Log(
            "[OnboardingTest] Current state | "
            + $"Prologue PlayerPrefs: {prologueCompleted} | "
            + $"Tutorial PlayerPrefs: {tutorialPlayerPrefs} | "
            + $"Tutorial JSON: {jsonState} | Tutorial SQLite: {sqliteState}");
    }

    [MenuItem("SPELLSTRIKE/Testing/Onboarding/Set Fresh Player", true)]
    [MenuItem("SPELLSTRIKE/Testing/Onboarding/Set Prologue Complete Only", true)]
    [MenuItem("SPELLSTRIKE/Testing/Onboarding/Set Both Complete", true)]
    private static bool ValidateStateMutation()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    private static void ApplyState(bool prologueCompleted, bool tutorialCompleted, string label)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[OnboardingTest] Stop Play Mode before changing onboarding test state.");
            return;
        }

        bool confirmed = EditorUtility.DisplayDialog(
            "Set Onboarding Test State",
            $"Set onboarding state to '{label}'?\n\n"
            + "Only the Prologue and Tutorial completion flags will change. "
            + "Stage progress, inventory, items, vocabulary history, and other save data are preserved.",
            "Apply",
            "Cancel");

        if (!confirmed)
            return;

        string jsonPath = Path.Combine(Application.persistentDataPath, "spellstrike_save.json");
        string sqlitePath = Path.Combine(Application.persistentDataPath, "spellstrike.db");

        GameData jsonData = null;
        string originalJson = null;

        if (File.Exists(jsonPath))
        {
            try
            {
                originalJson = File.ReadAllText(jsonPath);
                jsonData = JsonUtility.FromJson<GameData>(originalJson);
                if (jsonData == null)
                {
                    Debug.LogError("[OnboardingTest] Existing JSON save could not be parsed. No onboarding state was changed.");
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OnboardingTest] Could not read JSON save. No onboarding state was changed. {ex.Message}");
                return;
            }
        }

        SqliteDatabase sqlite = null;
        string previousSqliteValue = null;
        bool sqliteChanged = false;

        if (File.Exists(sqlitePath))
        {
            sqlite = new SqliteDatabase();
            sqlite.Initialize(sqlitePath);
            if (!sqlite.IsAvailable)
            {
                Debug.LogError("[OnboardingTest] Existing SQLite save could not be opened. No onboarding state was changed.");
                sqlite.Close();
                return;
            }

            try
            {
                sqlite.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS player (key TEXT PRIMARY KEY, value TEXT)");
                object previousValue = sqlite.ExecuteScalar("SELECT value FROM player WHERE key = 'tutorialCompleted'");
                previousSqliteValue = previousValue?.ToString();
                sqlite.ExecuteNonQuery(
                    $"INSERT OR REPLACE INTO player (key, value) VALUES ('tutorialCompleted', '{(tutorialCompleted ? 1 : 0)}')");
                sqliteChanged = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OnboardingTest] Could not update SQLite onboarding state. No PlayerPrefs/JSON state was changed. {ex.Message}");
                sqlite.Close();
                return;
            }
        }

        try
        {
            if (jsonData != null)
            {
                jsonData.tutorialCompleted = tutorialCompleted;
                File.WriteAllText(jsonPath, JsonUtility.ToJson(jsonData, true));
            }
        }
        catch (Exception ex)
        {
            if (sqliteChanged)
                RestoreSqliteTutorialState(sqlite, previousSqliteValue);

            sqlite?.Close();
            Debug.LogError($"[OnboardingTest] Could not update JSON save. SQLite was restored and PlayerPrefs were left unchanged. {ex.Message}");
            return;
        }

        sqlite?.Close();

        PlayerPrefs.SetInt(PrologueCompletionKey, prologueCompleted ? 1 : 0);
        PlayerPrefs.SetInt(TutorialCompletionKey, tutorialCompleted ? 1 : 0);
        PlayerPrefs.Save();

        Debug.Log(
            $"[OnboardingTest] Applied '{label}' | "
            + $"Prologue complete: {prologueCompleted} | Tutorial complete: {tutorialCompleted}. "
            + "All unrelated save data was preserved.");
    }

    private static void RestoreSqliteTutorialState(SqliteDatabase sqlite, string previousValue)
    {
        if (sqlite == null || !sqlite.IsAvailable)
            return;

        if (previousValue == null)
        {
            sqlite.ExecuteNonQuery("DELETE FROM player WHERE key = 'tutorialCompleted'");
            return;
        }

        sqlite.ExecuteNonQuery(
            $"INSERT OR REPLACE INTO player (key, value) VALUES ('tutorialCompleted', '{SqliteDatabase.Escape(previousValue)}')");
    }

    private static string ReadJsonTutorialState(string jsonPath)
    {
        if (!File.Exists(jsonPath))
            return "no file";

        try
        {
            GameData data = JsonUtility.FromJson<GameData>(File.ReadAllText(jsonPath));
            return data == null ? "unreadable" : data.tutorialCompleted.ToString();
        }
        catch (Exception ex)
        {
            return $"error ({ex.Message})";
        }
    }

    private static string ReadSqliteTutorialState(string sqlitePath)
    {
        if (!File.Exists(sqlitePath))
            return "no file";

        SqliteDatabase sqlite = new SqliteDatabase();
        sqlite.Initialize(sqlitePath);
        if (!sqlite.IsAvailable)
        {
            sqlite.Close();
            return "unavailable";
        }

        try
        {
            object value = sqlite.ExecuteScalar("SELECT value FROM player WHERE key = 'tutorialCompleted'");
            return value?.ToString() ?? "unset";
        }
        catch (Exception ex)
        {
            return $"error ({ex.Message})";
        }
        finally
        {
            sqlite.Close();
        }
    }
}
