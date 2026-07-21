using UnityEngine;

[RequireComponent(typeof(Collider))]
public class VocabularyTeachTrigger : MonoBehaviour
{
    [SerializeField] private VocabularyTeachController teachController;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool triggerOnlyOnce = true;

    private bool triggered;

    private void Reset()
    {
        Collider trigger = GetComponent<Collider>();
        trigger.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnlyOnce && triggered)
            return;

        if (!other.CompareTag(playerTag) && !other.transform.root.CompareTag(playerTag))
            return;

        triggered = true;
        teachController?.ShowTeach();
    }
}
