using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    public int maxHealth = 100;
    public int currentHealth;
    public Slider healthSlider; // Drag your UI Slider here

    [Header("Animation")]
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private MovementofPlayer movementController;

    private static readonly int BeingHitTrigger = Animator.StringToHash("BeingHit");
    private static readonly int IsDeadTrigger = Animator.StringToHash("isDead");
    private static readonly int SpeedParameter = Animator.StringToHash("Speed");
    private bool isDead;

    void Start()
    {
        currentHealth = maxHealth;
        playerAnimator ??= GetComponent<Animator>();
        movementController ??= GetComponent<MovementofPlayer>();

        // The prototype overlay owns only death presentation/return routing.
        // It tracks runtime word submissions without changing save/progression.
        if (GetComponent<PlayerDeathPrototypeUI>() == null)
            gameObject.AddComponent<PlayerDeathPrototypeUI>();

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
        if (isDead || damage <= 0f)
            return;

        // Clamp between 0 and maxHealth
        currentHealth = Mathf.Max(0, currentHealth - Mathf.RoundToInt(damage));

        if (healthSlider != null)
            healthSlider.value = currentHealth;

        Debug.Log($"{name} took {damage} damage. Current health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
            Die();
        else if (playerAnimator != null)
            playerAnimator.SetTrigger(BeingHitTrigger);
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
        bool wasDead = isDead;
        isDead = false;
        currentHealth = maxHealth;

        if (movementController != null)
            movementController.disableMovement = false;

        if (playerAnimator != null)
        {
            playerAnimator.ResetTrigger(IsDeadTrigger);
            playerAnimator.SetFloat(SpeedParameter, 0f);

            if (wasDead)
                playerAnimator.Play("Blend Tree", 0, 0f);
        }

        if (healthSlider != null)
            healthSlider.value = currentHealth;
        Debug.Log($"{name} restored to full health: {currentHealth}/{maxHealth}");
    }

    public void BindHealthSlider(Slider slider)
    {
        healthSlider = slider;
        if (healthSlider == null)
            return;

        healthSlider.maxValue = maxHealth;
        healthSlider.value = currentHealth;
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
        if (isDead)
            return;

        isDead = true;

        if (movementController != null)
            movementController.disableMovement = true;

        if (playerAnimator != null)
        {
            playerAnimator.SetFloat(SpeedParameter, 0f);
            playerAnimator.SetTrigger(IsDeadTrigger);
        }

        Debug.Log("Player Died");
        PlayerDeathPrototypeUI deathUI = GetComponent<PlayerDeathPrototypeUI>();
        if (deathUI != null)
            deathUI.OnPlayerDied(this);
    }
}
