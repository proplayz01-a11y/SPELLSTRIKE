using UnityEngine;

public class FragmentPickup : MonoBehaviour
{
    [Header("Fragment State")]
    private bool isLocked = true;
    private bool playerInRange = false;
    private bool collected = false;

    [Header("Pickup Receiver")]
    public MonoBehaviour pickupReceiver;
    public string pickupMessage = "OnFragmentCollected";

    public void SetLocked(bool locked)
    {
        isLocked = locked;

        if (!isLocked && playerInRange && !collected)
            MobileStageControls.OfferInteraction(this, "COLLECT", TryCollectFragment);
        else if (isLocked)
            MobileStageControls.ClearInteraction(this);
    }

    private void Update()
    {
        if (playerInRange && !isLocked && !collected && Input.GetKeyDown(KeyCode.E))
            TryCollectFragment();
    }

    public void TryCollectFragment()
    {
        if (isLocked || collected || !playerInRange)
            return;

        collected = true;
        MobileStageControls.ClearInteraction(this);
        Debug.Log($"[FragmentPickup] Fragment collected: {gameObject.name}");

        if (pickupReceiver != null && !string.IsNullOrEmpty(pickupMessage))
        {
            pickupReceiver.SendMessage(pickupMessage, SendMessageOptions.DontRequireReceiver);
            Debug.Log($"[FragmentPickup] Sent pickup message: {pickupMessage}");
        }
        else
        {
            Debug.LogWarning("[FragmentPickup] No pickup receiver assigned.");
        }

        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;
        if (!isLocked && !collected)
            MobileStageControls.OfferInteraction(this, "COLLECT", TryCollectFragment);
        Debug.Log("[FragmentPickup] Player entered fragment range.");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        MobileStageControls.ClearInteraction(this);
        Debug.Log("[FragmentPickup] Player left fragment range.");
    }

    private void OnDisable()
    {
        MobileStageControls.ClearInteraction(this);
    }
}
