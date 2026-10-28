using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class TutorialCheckpoint : MonoBehaviour
{
    public enum CheckpointType
    {
        Bridge,
        Shrine
    }

    [SerializeField] private TutorialDirector tutorialDirector;
    [SerializeField] private CheckpointType checkpointType;
    [SerializeField] private string playerTag = "Player";

    private void Reset()
    {
        Collider trigger = GetComponent<Collider>();
        trigger.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)
            && !other.transform.root.CompareTag(playerTag))
        {
            return;
        }

        if (tutorialDirector == null)
            tutorialDirector = FindFirstObjectByType<TutorialDirector>();

        if (tutorialDirector != null)
            tutorialDirector.NotifyCheckpointReached(checkpointType);
    }
}
