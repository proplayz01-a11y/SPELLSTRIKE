using UnityEngine;

public class ShrineTrigger : MonoBehaviour
{
    public Node1Controller nodeController;
    [SerializeField] private Stage1FirstFragmentReveal firstFragmentReveal;

    private void Awake()
    {
        if (firstFragmentReveal == null)
            firstFragmentReveal = GetComponent<Stage1FirstFragmentReveal>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        TryStartNode1(other);
    }

    private void OnTriggerStay(Collider other)
    {
        // Players may already be inside the wide arena collider when they
        // complete the Spirit's conversation. Re-check only while pending.
        if (firstFragmentReveal == null || !other.CompareTag("Player") ||
            firstFragmentReveal.IsRunning || firstFragmentReveal.HasRevealed)
            return;
        var db = GameDatabaseManager.Instance;
        if (db != null && db.HasMetSpiritRemnant() && db.HasEchoRuneCore())
            TryStartNode1(other);
    }

    private void TryStartNode1(Collider other)
    {
        if (nodeController == null)
            return;
        // Stage 1 now introduces fragments through the Spirit first.
        // Players who reached the arena early do not consume the first
        // vocabulary encounter before learning what Bramble is doing.
        if (firstFragmentReveal != null &&
            !firstFragmentReveal.HasPrerequisite)
            return;
        if (firstFragmentReveal != null &&
            firstFragmentReveal.TryBegin(other.transform,
                () => nodeController.TriggerEncounter()))
            return;
        nodeController.TriggerEncounter();
    }
}
