using System;
using UnityEngine;

public class StageProgressionManager : MonoBehaviour
{
    [Serializable]
    public class NodeState
    {
        public string nodeName = "Node";
        public bool startsUnlocked;
        public bool completed;
    }

    [Header("Stage")]
    [SerializeField] private int stageIndex = 2;
    [SerializeField] private string stageName = "Stage";
    [SerializeField, Min(1)] private int nodeCount = 4;

    [Header("Nodes")]
    [SerializeField] private NodeState[] nodes = new NodeState[0];

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    public int StageIndex => stageIndex;
    public string StageName => stageName;
    public int NodeCount => nodes != null ? nodes.Length : 0;
    public bool StageCleared => AreAllNodesComplete();

    private void Awake()
    {
        EnsureNodeArray();
    }

    private void OnValidate()
    {
        EnsureNodeArray();
    }

    public bool CanStartNode(int nodeIndex)
    {
        int index = ToArrayIndex(nodeIndex);
        if (!IsValidIndex(index)) return false;
        if (nodes[index].completed) return false;
        if (nodes[index].startsUnlocked) return true;
        if (index == 0) return true;

        return nodes[index - 1].completed;
    }

    public bool IsNodeComplete(int nodeIndex)
    {
        int index = ToArrayIndex(nodeIndex);
        return IsValidIndex(index) && nodes[index].completed;
    }

    public void MarkNodeComplete(int nodeIndex)
    {
        int index = ToArrayIndex(nodeIndex);
        if (!IsValidIndex(index)) return;
        if (nodes[index].completed) return;

        nodes[index].completed = true;

        int nextIndex = index + 1;
        if (IsValidIndex(nextIndex))
        {
            nodes[nextIndex].startsUnlocked = true;
        }

        Log($"{GetNodeLabel(index)} completed.");
    }

    public void ResetProgress()
    {
        EnsureNodeArray();

        for (int i = 0; i < nodes.Length; i++)
        {
            nodes[i].completed = false;
            nodes[i].startsUnlocked = i == 0;
        }

        Log("Progress reset.");
    }

    private bool AreAllNodesComplete()
    {
        EnsureNodeArray();

        if (nodes.Length == 0) return false;

        for (int i = 0; i < nodes.Length; i++)
        {
            if (!nodes[i].completed) return false;
        }

        return true;
    }

    private int ToArrayIndex(int nodeIndex)
    {
        return nodeIndex - 1;
    }

    private bool IsValidIndex(int index)
    {
        return nodes != null && index >= 0 && index < nodes.Length;
    }

    private string GetNodeLabel(int index)
    {
        if (!IsValidIndex(index) || string.IsNullOrWhiteSpace(nodes[index].nodeName))
        {
            return $"Node {index + 1}";
        }

        return nodes[index].nodeName;
    }

    private void EnsureNodeArray()
    {
        nodeCount = Mathf.Max(1, nodeCount);

        if (nodes == null)
        {
            nodes = new NodeState[0];
        }

        if (nodes.Length == nodeCount)
        {
            EnsureNodeDefaults();
            return;
        }

        NodeState[] resizedNodes = new NodeState[nodeCount];
        for (int i = 0; i < resizedNodes.Length; i++)
        {
            resizedNodes[i] = i < nodes.Length && nodes[i] != null
                ? nodes[i]
                : new NodeState { nodeName = $"Node {i + 1}" };
        }

        nodes = resizedNodes;
        EnsureNodeDefaults();
    }

    private void EnsureNodeDefaults()
    {
        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i] == null)
            {
                nodes[i] = new NodeState();
            }

            if (string.IsNullOrWhiteSpace(nodes[i].nodeName))
            {
                nodes[i].nodeName = $"Node {i + 1}";
            }
        }

        if (nodes.Length > 0 && !nodes[0].completed)
        {
            nodes[0].startsUnlocked = true;
        }
    }

    private void Log(string message)
    {
        if (!debugLogs) return;
        Debug.Log($"[StageProgressionManager] {stageName} ({stageIndex}): {message}");
    }
}
