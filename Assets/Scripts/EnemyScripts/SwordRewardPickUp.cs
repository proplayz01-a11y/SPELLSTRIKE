using UnityEngine;

public class SwordRewardPickup : MonoBehaviour
{
    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.E;
    public bool isUnlocked = false;
    public bool isCollected = false;

    [Header("Reward")]
    public string itemName = "Sky Sword";
    public float damageBonusPercent = 10f;

    [Header("References")]
    public Stage1BossController bossController;
    public Collider pickupCollider;
    public GameObject swordRootToHide;

    private bool playerInRange = false;

    private void Awake()
    {
        if (pickupCollider == null)
            pickupCollider = GetComponent<Collider>();

        if (pickupCollider != null)
            pickupCollider.enabled = false;
    }

    private void Update()
    {
        if (!isUnlocked || isCollected || !playerInRange)
            return;

        if (Input.GetKeyDown(interactKey))
            TryCollectSword();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;
        if (isUnlocked && !isCollected)
        {
            MobileStageControls.OfferInteraction(this, "COLLECT", TryCollectSword);
            Debug.Log($"[SwordRewardPickup] {itemName} is in interaction range.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        MobileStageControls.ClearInteraction(this);
    }

    public void UnlockSwordPickup()
    {
        if (isCollected) return;

        isUnlocked = true;

        if (pickupCollider != null)
            pickupCollider.enabled = true;
        if (playerInRange)
            MobileStageControls.OfferInteraction(this, "COLLECT", TryCollectSword);

        Debug.Log($"[SwordRewardPickup] {itemName} is now collectable.");
    }

    public void TryCollectSword()
    {
        if (!isUnlocked || isCollected || !playerInRange)
            return;

        isCollected = true;
        MobileStageControls.ClearInteraction(this);
        Debug.Log($"[SwordRewardPickup] Collected {itemName}. Passive: +{damageBonusPercent}% word attack damage.");

        if (bossController != null)
            bossController.OnSwordRewardCollected();
        else
            Debug.LogWarning("[SwordRewardPickup] BossController reference missing.");

        HideSwordObject();
    }

    public void AutoGrantSwordReward()
    {
        if (isCollected) return;

        isCollected = true;
        MobileStageControls.ClearInteraction(this);
        Debug.Log($"[SwordRewardPickup] Auto-granted {itemName}. Passive: +{damageBonusPercent}% word attack damage.");
        HideSwordObject();
    }

    private void HideSwordObject()
    {
        if (pickupCollider != null)
            pickupCollider.enabled = false;

        if (swordRootToHide != null)
            swordRootToHide.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        MobileStageControls.ClearInteraction(this);
    }
}
