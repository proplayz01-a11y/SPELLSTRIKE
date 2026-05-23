using UnityEngine;

public class Stage1ProgressionManager : MonoBehaviour
{
    [Header("Stage 1 Progress")]
    public bool node1Completed = false;
    public bool node2Completed = false;
    public bool node3Completed = false;
    public bool bossCompleted = false;

    [Header("Debug")]
    public bool debugLogs = true;

    public bool stage1Cleared => bossCompleted;

    public bool CanStartNode1()
    {
        return true;
    }

    public bool CanStartNode2()
    {
        return node1Completed;
    }

    public bool CanStartNode3()
    {
        return node2Completed;
    }

    public bool CanStartBoss()
    {
        return node3Completed;
    }

    public void MarkNode1Complete()
    {
        if (node1Completed) return;
        node1Completed = true;
        Log("Node 1 completed. Node 2 unlocked.");
    }

    public void MarkNode2Complete()
    {
        if (node2Completed) return;
        node2Completed = true;
        Log("Node 2 completed. Node 3 unlocked.");
    }

    public void MarkNode3Complete()
    {
        if (node3Completed) return;
        node3Completed = true;
        Log("Node 3 completed. Boss unlocked.");
    }

    public void MarkBossComplete()
    {
        if (bossCompleted) return;
        bossCompleted = true;
        Log("Boss completed. Stage 1 cleared.");
    }

    public bool IsNodeLocked(int nodeIndex)
    {
        switch (nodeIndex)
        {
            case 1: return !CanStartNode1();
            case 2: return !CanStartNode2();
            case 3: return !CanStartNode3();
            case 4: return !CanStartBoss();
            default: return true;
        }
    }

    private void Log(string message)
    {
        if (!debugLogs) return;
        Debug.Log($"[Stage1ProgressionManager] {message}");
    }
}
