using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;
    public Slider healthSlider;

    void Start()
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
    currentHealth = Mathf.Max(0f, currentHealth - damage);

    if (healthSlider != null)
        healthSlider.value = currentHealth;

    Debug.Log($"{name} took {damage} damage. HP: {currentHealth}/{maxHealth}");

    if (currentHealth <= 0f)
    {
        Die();
        return;
    }

    StoneSentinelsController stoneSentinel = GetComponent<StoneSentinelsController>();

    if (stoneSentinel != null)
    {
        stoneSentinel.TriggerBeingHit();
    }
}

   private void Die()
{
    Debug.Log($"{name} died.");

    StoneSentinelsController stoneSentinel = GetComponent<StoneSentinelsController>();

    if (stoneSentinel != null)
    {
        stoneSentinel.TriggerDeath();
        return;
    }

    Destroy(gameObject);
}
}