using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour
{
    public enum HudDisplayRole
    {
        Auto = 0,
        RegularOverhead = 1,
        BossHud = 2
    }

    public event Action<float, float> HealthChanged;
    public event Action<EnemyHealth> Died;

    public float maxHealth = 100f;
    public float currentHealth;
    public Slider healthSlider;
    [SerializeField] private bool invulnerable;
    [SerializeField, Tooltip("Auto recognizes existing major bosses. Override for future enemy types without editing controllers.")]
    private HudDisplayRole hudDisplayRole = HudDisplayRole.Auto;

    private bool isDead = false;

    public bool IsDead => isDead;
    public bool IsInvulnerable => invulnerable;
    public float HealthNormalized => maxHealth > 0f
        ? Mathf.Clamp01(currentHealth / maxHealth)
        : 0f;
    public bool UsesBossHud
    {
        get
        {
            if (hudDisplayRole != HudDisplayRole.Auto)
                return hudDisplayRole == HudDisplayRole.BossHud;

            // Avoid brittle scene-name/HP thresholds: controller type expresses
            // the actual encounter role, independent of arena size or difficulty.
            MonoBehaviour[] parents = GetComponentsInParent<MonoBehaviour>(true);
            MonoBehaviour[] children = GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour component in parents)
            {
                if (IsKnownBossController(component))
                    return true;
            }
            foreach (MonoBehaviour component in children)
                if (IsKnownBossController(component))
                    return true;
            return false;
        }
    }

    private static bool IsKnownBossController(MonoBehaviour component)
    {
        if (component == null)
            return false;
        switch (component.GetType().Name)
        {
            case "HollowKnightController":
            case "Stage1BossController":
            case "HollowKnightBossController_LATER":
            case "DrownedCaptainController":
            case "TitanEarthbreakerController":
            case "CrimsonDuchessController":
            case "MasterWizardController":
            case "VanderPoelController":
            case "RemnantKnightController":
                return true;
            default:
                return false;
        }
    }

    private void Awake()
    {
        // Enemies can be inactive until an encounter trigger enables them. Initialize
        // health before Start so the persistent HUD can bind on that same trigger frame.
        currentHealth = maxHealth;
    }

    private void Start()
    {
        SyncHealthSlider();
    }

    public void BindHealthSlider(Slider slider)
    {
        healthSlider = slider;
        SyncHealthSlider();
    }

    public void TakeDamage(float damage)
    {
        if (isDead || invulnerable) return;

        float beforeDamage = currentHealth;
        currentHealth = Mathf.Max(0f, currentHealth - damage);
        FloatingDamageNumbersV01.Show(this, beforeDamage - currentHealth);
        SyncHealthSlider();
        HealthChanged?.Invoke(currentHealth, maxHealth);

        // Standard encounters create their overhead bar on activation through
        // EnemyEncounterHUD. Summoned enemies that bypass that bridge still get
        // a bar on first surviving hit; no new managers or scene rewiring.
        if (currentHealth > 0f && !UsesBossHud)
            RegularEnemyOverheadHealthBar.EnsureVisible(this);

        Debug.Log($"{name} took {damage} damage. HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0f)
        {
            Die();
            return;
        }

        TriggerHitReaction();
    }

    /// <summary>
    /// Used by authored invulnerability phases such as Titan's dormant statue
    /// and laser-triggered Awakening. Defaults to false for existing enemies.
    /// </summary>
    public void SetInvulnerable(bool value)
    {
        invulnerable = value;
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
        SyncHealthSlider();
        HealthChanged?.Invoke(currentHealth, maxHealth);
        Died?.Invoke(this);

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
