using System;
using System.Reflection;
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
        SyncHealthSlider();
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        SyncHealthSlider();

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

        if (TryInvokeControllerMessage("TriggerBeingHit"))
            return;
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

        if (TryInvokeControllerMessage("TriggerDeath"))
            return;

        Destroy(gameObject);
    }

    private void SyncHealthSlider()
    {
        if (healthSlider == null)
            return;

        healthSlider.maxValue = maxHealth;
        healthSlider.value = currentHealth;
    }

    private bool TryInvokeControllerMessage(string methodName)
    {
        MonoBehaviour[] receivers = GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour receiver in receivers)
        {
            if (receiver == null || receiver == this)
                continue;

            MethodInfo method = receiver.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null
            );

            if (method == null)
                continue;

            method.Invoke(receiver, null);
            return true;
        }

        return false;
    }
}
