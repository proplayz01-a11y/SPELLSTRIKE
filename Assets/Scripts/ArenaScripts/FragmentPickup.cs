using UnityEngine;

public class FragmentPickup : MonoBehaviour
{
    [Header("Fragment State")]
    private bool isLocked = true;
    private bool playerInRange = false;

    [Header("Pickup Receiver")]
    public MonoBehaviour pickupReceiver;
    public string pickupMessage = "OnFragmentCollected";

    public void SetLocked(bool locked)
    {
        isLocked = locked;
    }

    private void Update()
    {
        if (playerInRange && !isLocked && Input.GetKeyDown(KeyCode.E))
        {
            CollectFragment();
        }
    }

    private void CollectFragment()
    {
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
        Debug.Log("[FragmentPickup] Player entered fragment range.");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        Debug.Log("[FragmentPickup] Player left fragment range.");
    }
}