using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;
    public Slider healthSlider;

    private bool isDead = false;

    private void Start()
    {
        currentHealth = maxHealth;

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);

        if (healthSlider != null)
            healthSlider.value = currentHealth;

        Debug.Log($"{name} took {damage} damage. HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0f)
        {
            Die();
            return;
        }

        TriggerHitReaction();
    }

    private void TriggerHitReaction()
    {
        StoneSentinelsController stoneSentinel = GetComponent<StoneSentinelsController>();

        if (stoneSentinel != null)
        {
            stoneSentinel.TriggerBeingHit();
            return;
        }

        HollowKnightController hollowKnight = GetComponent<HollowKnightController>();

        if (hollowKnight != null)
        {
            hollowKnight.TriggerBeingHit();
            return;
        }

        CursedJesterController cursedJester = GetComponent<CursedJesterController>();

        if (cursedJester != null)
        {
            cursedJester.TriggerBeingHit();
            return;
        }
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;

        Debug.Log($"{name} died.");

        StoneSentinelsController stoneSentinel = GetComponent<StoneSentinelsController>();

        if (stoneSentinel != null)
        {
            stoneSentinel.TriggerDeath();
            return;
        }

        HollowKnightController hollowKnight = GetComponent<HollowKnightController>();

        if (hollowKnight != null)
        {
            // We will implement this later in the Death layer.
            // For now, this prevents Hollow Knight from being instantly destroyed
            // once we add TriggerDeath().
            hollowKnight.TriggerDeath();
            return;
        }

        CursedJesterController cursedJester = GetComponent<CursedJesterController>();

        if (cursedJester != null)
        {
            cursedJester.TriggerDeath();
            return;
        }

        Destroy(gameObject);
    }
}
