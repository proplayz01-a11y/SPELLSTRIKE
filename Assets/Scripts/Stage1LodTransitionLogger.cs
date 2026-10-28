using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Development-only LOD transition monitor for the optimized Stage 1 test scene.
/// It evaluates LODGroup thresholds from the active gameplay camera and logs only
/// when a group changes LOD, so the Console does not get spammed every frame.
/// </summary>
public sealed class Stage1LodTransitionLogger : MonoBehaviour
{
    public static bool LoggingEnabled { get; set; } = true;

    private const string ExpectedSceneName = "Stage1_OptimizedMap_Test";
    private const string OptimizedRootName = "level1_STAGE1_OPTIMIZED_TEST";
    private const string WallPrefix = "StoneWall_LODGroup_";
    private const float PollIntervalSeconds = 0.10f;

    private readonly Dictionary<LODGroup, int> previousStates = new Dictionary<LODGroup, int>();
    private LODGroup[] groups = Array.Empty<LODGroup>();
    private Camera targetCamera;
    private float nextPollTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (!Application.isEditor && !Debug.isDebugBuild)
            return;

        Scene scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.name, ExpectedSceneName, StringComparison.Ordinal))
            return;

        if (FindFirstObjectByType<Stage1LodTransitionLogger>() != null)
            return;

        GameObject host = new GameObject("__Stage1LodTransitionLogger");
        DontDestroyOnLoad(host);
        host.AddComponent<Stage1LodTransitionLogger>();
    }

    private void Start()
    {
        targetCamera = Camera.main;
        if (targetCamera == null)
            targetCamera = FindFirstObjectByType<Camera>();

        GameObject root = GameObject.Find(OptimizedRootName);
        groups = root != null
            ? root.GetComponentsInChildren<LODGroup>(true)
            : FindObjectsByType<LODGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        groups = groups
            .Where(group => group != null && group.enabled)
            .OrderBy(group => group.gameObject.name, StringComparer.Ordinal)
            .ToArray();

        previousStates.Clear();
        foreach (LODGroup group in groups)
            previousStates[group] = CalculateLodState(group);

        int wallGroups = groups.Count(IsWallGroup);
        int standardGroups = groups.Length - wallGroups;

        if (LoggingEnabled)
        {
            Debug.Log(
                "[Stage1 LOD Monitor] ACTIVE\n" +
                $"Tracking: {groups.Length} LODGroups ({wallGroups} walls, {standardGroups} standard)\n" +
                "Wall labels: Near=Rodin LOD1 / Mid=Rodin LOD2 / Far=Rodin LOD3 / Cull=not rendered\n" +
                "Logs are emitted only when a LOD state changes.");
        }

        LogWallDistribution("Initial");
    }

    private void Update()
    {
        if (Time.unscaledTime < nextPollTime)
            return;

        nextPollTime = Time.unscaledTime + PollIntervalSeconds;

        if (targetCamera == null || !targetCamera.isActiveAndEnabled)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
                return;
        }

        var wallTransitionCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        bool anyWallChanged = false;

        foreach (LODGroup group in groups)
        {
            if (group == null || !group.enabled || !group.gameObject.activeInHierarchy)
                continue;

            int currentState = CalculateLodState(group);
            if (!previousStates.TryGetValue(group, out int previousState))
            {
                previousStates[group] = currentState;
                continue;
            }

            if (currentState == previousState)
                continue;

            previousStates[group] = currentState;

            if (IsWallGroup(group))
            {
                anyWallChanged = true;
                string transition = $"{DescribeWallState(previousState)} -> {DescribeWallState(currentState)}";
                wallTransitionCounts[transition] = wallTransitionCounts.TryGetValue(transition, out int count)
                    ? count + 1
                    : 1;
            }
            else
            {
                float relativeHeight = CalculateScreenRelativeHeight(group);
                float distance = CalculateDistance(group);
                if (LoggingEnabled)
                {
                    Debug.Log(
                        $"[Stage1 LOD] {group.gameObject.name}: " +
                        $"{DescribeStandardState(previousState)} -> {DescribeStandardState(currentState)} " +
                        $"| screen {relativeHeight * 100f:F1}% | distance {distance:F1}");
                }
            }
        }

        if (LoggingEnabled && wallTransitionCounts.Count > 0)
        {
            foreach (KeyValuePair<string, int> pair in wallTransitionCounts.OrderBy(pair => pair.Key, StringComparer.Ordinal))
                Debug.Log($"[Stage1 LOD] Walls x{pair.Value}: {pair.Key}");
        }

        if (LoggingEnabled && anyWallChanged)
            LogWallDistribution("Current");
    }

    private int CalculateLodState(LODGroup group)
    {
        if (group == null || targetCamera == null)
            return -1;

        float relativeHeight = CalculateScreenRelativeHeight(group);
        LOD[] lods = group.GetLODs();

        for (int i = 0; i < lods.Length; i++)
        {
            if (relativeHeight >= lods[i].screenRelativeTransitionHeight)
                return i;
        }

        return -1;
    }

    private float CalculateScreenRelativeHeight(LODGroup group)
    {
        Vector3 referencePoint = group.transform.TransformPoint(group.localReferencePoint);
        Vector3 scale = group.transform.lossyScale;
        float largestScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        float worldSize = Mathf.Max(0.0001f, group.size * largestScale);

        if (targetCamera.orthographic)
        {
            float viewportHeight = Mathf.Max(0.0001f, targetCamera.orthographicSize * 2f);
            return (worldSize / viewportHeight) * QualitySettings.lodBias;
        }

        float distance = Mathf.Max(0.0001f, Vector3.Distance(targetCamera.transform.position, referencePoint));
        float halfFovRadians = targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
        float viewportHeightAtDistance = 2f * distance * Mathf.Tan(halfFovRadians);
        return (worldSize / Mathf.Max(0.0001f, viewportHeightAtDistance)) * QualitySettings.lodBias;
    }

    private float CalculateDistance(LODGroup group)
    {
        if (targetCamera == null || group == null)
            return 0f;

        Vector3 referencePoint = group.transform.TransformPoint(group.localReferencePoint);
        return Vector3.Distance(targetCamera.transform.position, referencePoint);
    }

    private void LogWallDistribution(string prefix)
    {
        if (!LoggingEnabled)
            return;

        int near = 0;
        int mid = 0;
        int far = 0;
        int culled = 0;

        foreach (LODGroup group in groups)
        {
            if (!IsWallGroup(group))
                continue;

            int state = previousStates.TryGetValue(group, out int previousState)
                ? previousState
                : CalculateLodState(group);

            switch (state)
            {
                case 0:
                    near++;
                    break;
                case 1:
                    mid++;
                    break;
                case 2:
                    far++;
                    break;
                default:
                    culled++;
                    break;
            }
        }

        Debug.Log(
            $"[Stage1 LOD] {prefix} wall distribution: " +
            $"Near/LOD1={near} | Mid/LOD2={mid} | Far/LOD3={far} | Culled={culled}");
    }

    private static bool IsWallGroup(LODGroup group)
    {
        return group != null && group.gameObject.name.StartsWith(WallPrefix, StringComparison.Ordinal);
    }

    private static string DescribeWallState(int state)
    {
        switch (state)
        {
            case 0:
                return "Near (Rodin LOD1, 976 tris)";
            case 1:
                return "Mid (Rodin LOD2, 480 tris)";
            case 2:
                return "Far (Rodin LOD3, 232 tris)";
            default:
                return "CULL (0 tris)";
        }
    }

    private static string DescribeStandardState(int state)
    {
        return state >= 0 ? $"LOD{state}" : "CULL";
    }
}
