using UnityEngine;

public class ShrineTrigger : MonoBehaviour
{
    public Node1Controller nodeController;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        nodeController.TriggerEncounter();
    }
}