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
        if (!isUnlocked || collected || !playerInRange)
            return;

        if (Input.GetKeyDown(interactKey))
            TryCollectFragment();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;

        if (isUnlocked && !collected)
        {
            MobileStageControls.OfferInteraction(this, "COLLECT", TryCollectFragment);
            Debug.Log("[BossFragmentPickup] Player can collect boss fragment.");
        }
        else
        {
            Debug.Log("[BossFragmentPickup] Fragment is locked until Hollow Knight is defeated.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        MobileStageControls.ClearInteraction(this);
    }

    public void UnlockFragment()
    {
        isUnlocked = true;
        if (playerInRange && !collected)
            MobileStageControls.OfferInteraction(this, "COLLECT", TryCollectFragment);
        Debug.Log("[BossFragmentPickup] Boss fragment unlocked.");
    }

    public void TryCollectFragment()
    {
        if (!isUnlocked || collected || !playerInRange)
            return;

        collected = true;
        MobileStageControls.ClearInteraction(this);
        Debug.Log("[BossFragmentPickup] Boss fragment collected.");

        if (bossController != null)
            bossController.OnBossFragmentCollected();
        else
            Debug.LogWarning("[BossFragmentPickup] BossController reference missing.");

        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        MobileStageControls.ClearInteraction(this);
    }
}
