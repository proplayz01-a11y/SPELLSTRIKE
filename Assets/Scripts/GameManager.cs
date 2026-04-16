using System.Collections;
using UnityEngine;

public enum GameState
{
    Idle,
    Playing,
    Paused,
    GameOver,
    Win
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game State")]
    public GameState currentState = GameState.Idle;

    [Header("Stage")]
    public int currentStageIndex = 0;

    [Header("Player Stats")]
    public int playerHealth = 100;
    public int playerMaxHealth = 100;
    public int score = 0;

    [Header("Word Tracking")]
    public float totalWordLength = 0f;
    public int wordCount = 0;

    [Header("Potions")]
    public int healthPotions = 1;
    public int powerUpPotions = 1;
    public int cleansingPotions = 1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        SetState(GameState.Idle);
    }

    // ─────────────────────────────────────────
    //  STATE MANAGEMENT
    // ─────────────────────────────────────────

    public void SetState(GameState newState)
    {
        currentState = newState;

        switch (newState)
        {
            case GameState.Playing:
                Time.timeScale = 1f;
                break;

            case GameState.Paused:
                Time.timeScale = 0f;
                break;

            case GameState.GameOver:
                Time.timeScale = 0f;
                OnGameOver();
                break;

            case GameState.Win:
                Time.timeScale = 0f;
                OnWin();
                break;

            case GameState.Idle:
                Time.timeScale = 1f;
                break;
        }

        Debug.Log($"[GameManager] State changed to: {newState}");
    }

    public void StartGame(int stageIndex)
    {
        currentStageIndex = stageIndex;
        score = 0;
        totalWordLength = 0f;
        wordCount = 0;
        SyncStatsFromDatabase();
        SetState(GameState.Playing);
    }

    public void PauseGame()
    {
        if (currentState == GameState.Playing)
            SetState(GameState.Paused);
    }

    public void ResumeGame()
    {
        if (currentState == GameState.Paused)
            SetState(GameState.Playing);
    }

    public void TriggerGameOver()
    {
        if (currentState == GameState.GameOver) return;
        SetState(GameState.GameOver);
    }

    public void TriggerWin()
    {
        if (currentState == GameState.Win) return;
        SetState(GameState.Win);
    }

    // ─────────────────────────────────────────
    //  GAME OVER / WIN
    // ─────────────────────────────────────────

    private void OnGameOver()
    {
        Debug.Log("[GameManager] Game Over.");
        GameDatabaseManager.Instance?.FlushSave();
    }

    private void OnWin()
    {
        Debug.Log($"[GameManager] Stage {currentStageIndex} cleared!");
        GameDatabaseManager.Instance?.SaveStageProgress(currentStageIndex, true);
        GameDatabaseManager.Instance?.FlushSave();
    }

    // ─────────────────────────────────────────
    //  SCORE / WORDS
    // ─────────────────────────────────────────

    public void AddScore(int points)
    {
        score += points;
        Debug.Log($"[GameManager] Score: {score}");
    }

    public void RecordWord(string word, bool valid)
    {
        if (valid && word.Length > 0)
        {
            totalWordLength += word.Length;
            wordCount++;
        }
        GameDatabaseManager.Instance?.RecordWordHistory(word, valid);
    }

    public float GetAverageWordLength()
    {
        if (wordCount == 0) return 0f;
        return totalWordLength / wordCount;
    }

    // ─────────────────────────────────────────
    //  PLAYER HEALTH
    // ─────────────────────────────────────────

    public void SetPlayerHealth(int current, int max)
    {
        playerHealth = current;
        playerMaxHealth = max;

        if (GameDatabaseManager.Instance != null)
        {
            GameDatabaseManager.Instance.Data.playerHealth = current;
            GameDatabaseManager.Instance.Data.playerMaxHealth = max;
        }

        if (playerHealth <= 0)
            TriggerGameOver();
    }

    // ─────────────────────────────────────────
    //  POTIONS
    // ─────────────────────────────────────────

    public void SyncPotionsFromDatabase()
    {
        if (GameDatabaseManager.Instance == null) return;
        healthPotions = GameDatabaseManager.Instance.GetPotionCount(PotionType.Health);
        powerUpPotions = GameDatabaseManager.Instance.GetPotionCount(PotionType.PowerUp);
        cleansingPotions = GameDatabaseManager.Instance.GetPotionCount(PotionType.Cleansing);
    }

    // ─────────────────────────────────────────
    //  SYNC FROM DATABASE
    // ─────────────────────────────────────────

    private void SyncStatsFromDatabase()
    {
        if (GameDatabaseManager.Instance == null) return;
        var data = GameDatabaseManager.Instance.Data;
        playerHealth = data.playerHealth;
        playerMaxHealth = data.playerMaxHealth;
        SyncPotionsFromDatabase();
    }

    // ─────────────────────────────────────────
    //  RESET
    // ─────────────────────────────────────────

    public void ResetSession()
    {
        score = 0;
        totalWordLength = 0f;
        wordCount = 0;
        SetState(GameState.Idle);
    }
}