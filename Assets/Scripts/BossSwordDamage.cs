using UnityEngine;

public class BossSwordDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    public float damage = 12f;

    [Header("State")]
    public bool canDamage = false;

    private bool hasDamagedThisSwing = false;

    public void EnableDamage()
    {
        canDamage = true;
        hasDamagedThisSwing = false;

        Debug.Log("[BossSwordDamage] Sword damage enabled.");
    }

    public void DisableDamage()
    {
        canDamage = false;

        Debug.Log("[BossSwordDamage] Sword damage disabled.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!canDamage) return;
        if (hasDamagedThisSwing) return;
        if (!other.CompareTag("Player")) return;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();

        if (playerHealth == null)
            playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null)
        {
            Debug.LogWarning("[BossSwordDamage] PlayerHealth not found on player.");
            return;
        }

        playerHealth.TakeDamage(damage);
        hasDamagedThisSwing = true;

        Debug.Log("[BossSwordDamage] Player hit by sword.");
    }
}