using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Runtime level/EXP is independent of the save data in direct-scene tests.</summary>
public static class PlayerProgression
{
    public const int MaxLevel = 20;
    public const int HealthPerLevel = 10;
    public const float DamagePerLevel = 0.05f;

    public sealed class State
    {
        public int Level { get; private set; }
        public int Experience { get; private set; }

        public State(int level = 1, int experience = 0)
        {
            Level = Mathf.Clamp(level, 1, MaxLevel);
            Experience = 0;
            Grant(experience);
        }

        public int Required => RequiredFor(Level);
        public float Fraction => Level >= MaxLevel ? 1f : (float)Experience / Required;

        public void Grant(int amount)
        {
            if (amount <= 0 || Level >= MaxLevel)
                return;
            long remaining = (long)Experience + amount;
            while (Level < MaxLevel && remaining >= Required)
            {
                remaining -= Required;
                Level++;
            }
            Experience = Level >= MaxLevel ? 0 : (int)remaining;
        }
    }

    private static State state = new State();
    public static bool IsCampaign { get; private set; }
    public static int Level => state.Level;
    public static int Experience => state.Experience;
    public static int Required => state.Required;
    public static float Fraction => state.Fraction;
    public static int BonusHealth => (Level - 1) * HealthPerLevel;
    public static float DamageMultiplier => 1f + (Level - 1) * DamagePerLevel;
    public static int RequiredFor(int level) => 80 + 20 * (Mathf.Clamp(level, 1, MaxLevel) - 1);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetForPlay()
    {
        IsCampaign = false;
        state = new State();
        GameDatabaseManager.ResetTemporaryStage1Story();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Main Menu Scene" && mode == LoadSceneMode.Single)
        {
            IsCampaign = false;
            state = new State();
            GameDatabaseManager.ResetTemporaryStage1Story();
        }
    }

    // Only the actual Adventure navigation on the Main Menu can authorize persistence.
    public static void BeginCampaign()
    {
        if (!Application.isPlaying || SceneManager.GetActiveScene().name != "Main Menu Scene")
            return;
        var database = GameDatabaseManager.EnsureInstance();
        if (database == null || database.Data == null)
            return;
        state = new State(database.Data.playerLevel, database.Data.playerExperience);
        IsCampaign = true;
    }

    public static void ResetCampaignAfterDatabaseReset()
    {
        if (IsCampaign)
            state = new State();
    }

    public static void GrantExperience(int amount)
    {
        if (!Application.isPlaying || amount <= 0 || Level >= MaxLevel)
            return;
        int oldLevel = Level;
        state.Grant(amount);
        if (IsCampaign)
        {
            var database = GameDatabaseManager.EnsureInstance();
            database?.SavePlayerProgression(Level, Experience);
        }
        Debug.Log($"[Progression] +{amount} EXP | Lv {Level} | {Experience}/{Required} | " +
            (IsCampaign ? "campaign" : "temporary test"));
        if (Level != oldLevel)
            Debug.Log($"[Progression] Level up: {oldLevel} -> {Level}");
    }
}
