using UnityEngine;

public class BossFragmentPickup : MonoBehaviour
{
    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.E;
    public bool isUnlocked = false;

    [Header("References")]
    public Stage1BossController bossController;

    private bool playerInRange = false;
    private bool collected = false;

    private void Update()
    {
        if (!isUnlocked) return;
        if (collected) return;
        if (!playerInRange) return;

        if (Input.GetKeyDown(interactKey))
        {
            CollectFragment();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;

        if (isUnlocked)
            Debug.Log("[BossFragmentPickup] Player can collect boss fragment. Press E.");
        else
            Debug.Log("[BossFragmentPickup] Fragment is locked until Hollow Knight is defeated.");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
    }

    public void UnlockFragment()
    {
        isUnlocked = true;
        Debug.Log("[BossFragmentPickup] Boss fragment unlocked.");
    }

    private void CollectFragment()
    {
        collected = true;

        Debug.Log("[BossFragmentPickup] Boss fragment collected.");

        if (bossController != null)
        {
            bossController.OnBossFragmentCollected();
        }
        else
        {
            Debug.LogWarning("[BossFragmentPickup] BossController reference missing.");
        }

        gameObject.SetActive(false);
    }
}