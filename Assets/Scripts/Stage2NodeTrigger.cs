using UnityEngine;

public class Stage2NodeTrigger : MonoBehaviour
{
    private bool hasTriggered;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered || !other.CompareTag("Player"))
        {
            return;
        }

        hasTriggered = true;
        Debug.Log("[Stage2 Node1] Combat started.");
    }
}
