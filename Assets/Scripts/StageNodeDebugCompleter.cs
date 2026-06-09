using UnityEngine;

public class StageNodeDebugCompleter : MonoBehaviour
{
    [Header("Debug Completion")]
    [SerializeField] private StageNodeController nodeController;
    [SerializeField] private KeyCode completeKey = KeyCode.K;
    [SerializeField] private bool enableKeyboardComplete = true;

    [Header("Messages")]
    [SerializeField] private string nextNodeUnlockedMessage = "[Stage2] Node2 unlocked.";

    private void Update()
    {
        if (!enableKeyboardComplete) return;
        if (!Input.GetKeyDown(completeKey)) return;

        DebugCompleteNode();
    }

    public void DebugCompleteNode()
    {
        if (nodeController == null || nodeController.NodeCompleted) return;

        nodeController.CompleteNode();

        if (nodeController.NodeCompleted && !string.IsNullOrWhiteSpace(nextNodeUnlockedMessage))
        {
            Debug.Log(nextNodeUnlockedMessage);
        }
    }
}
