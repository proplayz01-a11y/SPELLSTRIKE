using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    public int maxHealth = 100;
    public int currentHealth;
    public Slider healthSlider; // Drag your UI Slider here

    void Start()
    {
        currentHealth = maxHealth;

        // Auto-find the slider in the scene if it's null
        if (healthSlider == null)
        {
#if UNITY_2023_1_OR_NEWER
            healthSlider = FindFirstObjectByType<Slider>();
#else
            healthSlider = FindObjectOfType<Slider>();
#endif
            if (healthSlider != null)
                Debug.Log("HealthSlider auto-assigned: " + healthSlider.name);
            else
                Debug.LogWarning("No Slider found in scene!");
        }

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            TakeDamage(10);
        }
    }

    public void TakeDamage(float damage)
    {
        // Clamp between 0 and maxHealth
        currentHealth = Mathf.Max(0, currentHealth - Mathf.RoundToInt(damage));

        if (healthSlider != null)
            healthSlider.value = currentHealth;

        Debug.Log($"{name} took {damage} damage. Current health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
            Die();
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        if (healthSlider != null)
            healthSlider.value = currentHealth;
        Debug.Log($"{name} healed {amount}. Current health: {currentHealth}/{maxHealth}");
    }

    public void RestoreFullHealth()
    {
        currentHealth = maxHealth;
        if (healthSlider != null)
            healthSlider.value = currentHealth;
        Debug.Log($"{name} restored to full health: {currentHealth}/{maxHealth}");
    }

    // Support legacy SendMessage("ApplyDamage", amount) calls from other scripts/animations
    public void ApplyDamage(float damage)
    {
        TakeDamage(damage);
    }

    // Overload for integer damage calls (e.g. SendMessage with int)
    public void ApplyDamage(int damage)
    {
        TakeDamage((float)damage);
    }

    void Die()
    {
        Debug.Log("Player Died");
        // Play death animation, disable movement, show game over UI
    }
}
