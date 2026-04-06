using UnityEngine;
using System.Collections;

public enum PotionType
{
    Health,
    PowerUp,
    Cleansing
}

public class PotionSystem : MonoBehaviour
{
    public static PotionSystem Instance;

    public int maxTotalPotions = 3;
    public int healthPotions = 1;
    public int powerUpPotions = 1;
    public int cleansingPotions = 1;

    public float powerUpMultiplier = 2f;
    public float powerUpDuration = 6f;

    private bool powerUpActive = false;
    private Coroutine powerUpCoroutine;

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

    public bool UsePotion(PotionType type)
    {
        switch (type)
        {
            case PotionType.Health:
                return UseHealthPotion();
            case PotionType.PowerUp:
                return UsePowerUpPotion();
            case PotionType.Cleansing:
                return UseCleansingPotion();
            default:
                return false;
        }
    }

    public float GetDamageMultiplier()
    {
        return powerUpActive ? powerUpMultiplier : 1f;
    }

    private bool UseHealthPotion()
    {
        if (healthPotions <= 0) return false;

        healthPotions = Mathf.Max(0, healthPotions - 1);
        PlayerHealth playerHealth = FindAnyObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            int healAmount = Mathf.CeilToInt(playerHealth.maxHealth * 0.3f);
            playerHealth.Heal(healAmount);
            Debug.Log($"Health potion used. Healed {healAmount} HP.");
            return true;
        }

        Debug.LogWarning("No PlayerHealth found for Health potion.");
        return false;
    }

    private bool UsePowerUpPotion()
    {
        if (powerUpPotions <= 0 || powerUpActive) return false;

        powerUpPotions = Mathf.Max(0, powerUpPotions - 1);
        powerUpActive = true;
        if (powerUpCoroutine != null)
            StopCoroutine(powerUpCoroutine);

        powerUpCoroutine = StartCoroutine(PowerUpRoutine());
        Debug.Log("Power Up potion activated.");
        return true;
    }

    private IEnumerator PowerUpRoutine()
    {
        yield return new WaitForSeconds(powerUpDuration);
        powerUpActive = false;
        powerUpCoroutine = null;
        Debug.Log("Power Up potion expired.");
    }

    private bool UseCleansingPotion()
    {
        if (cleansingPotions <= 0) return false;

        cleansingPotions = Mathf.Max(0, cleansingPotions - 1);
        TileManager tileManager = FindAnyObjectOfType<TileManager>();
        if (tileManager != null)
        {
            tileManager.ResetTileSelection();
            tileManager.AddTilesToPool(3);
            Debug.Log("Cleansing potion used: selected tiles cleared and 3 new tiles added.");
            return true;
        }

        Debug.LogWarning("No TileManager found for Cleansing potion.");
        return false;
    }

    // Compatibility helper: use FindFirstObjectByType when available, fallback to FindObjectOfType on older Unity
    private static T FindAnyObjectOfType<T>() where T : UnityEngine.Object
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<T>();
#else
        return Object.FindObjectOfType<T>();
#endif
    }
}
