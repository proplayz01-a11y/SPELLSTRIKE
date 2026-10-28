using UnityEngine;

/// <summary>
/// Central encounter-to-HUD bridge. Encounter triggers call this with their enemy
/// root; the persistent HUD owns the actual Slider and EnemyHealth owns death data.
/// </summary>
public static class EnemyEncounterHUD
{
    public static void ShowFor(GameObject enemyRoot)
    {
        if (enemyRoot == null)
            return;

        EnemyHealth health = enemyRoot.GetComponent<EnemyHealth>();
        if (health == null)
            health = enemyRoot.GetComponentInChildren<EnemyHealth>(true);

        ShowFor(health);
    }

    public static void ShowFor(Component enemyComponent)
    {
        if (enemyComponent == null)
            return;

        EnemyHealth health = enemyComponent.GetComponent<EnemyHealth>();
        if (health == null)
            health = enemyComponent.GetComponentInParent<EnemyHealth>(true);
        if (health == null)
            health = enemyComponent.GetComponentInChildren<EnemyHealth>(true);

        ShowFor(health);
    }

    public static void ShowFor(EnemyHealth health)
    {
        if (health == null)
        {
            Debug.LogWarning("[EnemyEncounterHUD] Encounter started but no EnemyHealth could be resolved.");
            return;
        }

        if (health.UsesBossHud)
        {
            PersistentHUDRoot.Instance?.ShowEnemyHealth(health);
        }
        else
        {
            // Ordinary enemies use a compact, world-attached, camera-facing
            // health bar. The old boss HUD is cleared between encounters.
            PersistentHUDRoot.Instance?.HideEnemyHealth();
            RegularEnemyOverheadHealthBar.EnsureVisible(health);
        }
    }

    public static void HideCurrent()
    {
        PersistentHUDRoot.Instance?.HideEnemyHealth();
    }
}
