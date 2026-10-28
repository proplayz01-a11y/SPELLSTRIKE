#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

public static class Stage1RuntimeProfileProbe
{
    private const string ExpectedSceneName = "Stage1_OptimizedMap_Test";
    private const string ReportPath = "Temp/Stage1RuntimeProfile.txt";
    private const string TriggerPath = "Temp/Stage1RuntimeProfile.trigger";
    private const double DurationSeconds = 8.0;

    private static readonly List<double> FrameMilliseconds = new List<double>(2048);
    private static readonly Dictionary<string, long> StatSums = new Dictionary<string, long>();
    private static readonly Dictionary<string, long> StatMax = new Dictionary<string, long>();
    private static readonly string[] StatNames =
    {
        "triangles",
        "vertices",
        "batches",
        "setPassCalls",
        "drawCalls",
        "shadowCasters",
        "renderTextureChanges",
    };

    private static double startedAt;
    private static Stage1RuntimeFrameProbeBehaviour runtimeFrameProbe;
    private static int samples;
    private static bool running;

    [InitializeOnLoadMethod]
    private static void InstallTriggerWatcher()
    {
        EditorApplication.update -= CheckTrigger;
        EditorApplication.update += CheckTrigger;
    }

    private static void CheckTrigger()
    {
        if (!File.Exists(TriggerPath))
            return;

        if (!EditorApplication.isPlaying)
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.Log("[Stage1 Runtime Profile] Trigger detected in Edit Mode; entering Play Mode automatically.");
                EditorApplication.EnterPlaymode();
            }

            return;
        }

        try
        {
            File.Delete(TriggerPath);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[Stage1 Runtime Profile] Could not clear trigger file: " + exception.Message);
            return;
        }

        StartProfile();
    }

    [MenuItem("SPELLSTRIKE/Optimization/7. Runtime Profile - 8 Seconds %#&p")]
    public static void StartProfile()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogError("[Stage1 Runtime Profile] Enter Play Mode in the optimized Stage 1 test scene first.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.name, ExpectedSceneName, StringComparison.Ordinal))
        {
            Debug.LogError($"[Stage1 Runtime Profile] Expected scene '{ExpectedSceneName}', but active scene is '{scene.name}'.");
            return;
        }

        if (running)
        {
            Debug.LogWarning("[Stage1 Runtime Profile] A profile capture is already running.");
            return;
        }

        if (EditorApplication.isPaused)
            EditorApplication.isPaused = false;

        FrameMilliseconds.Clear();
        StatSums.Clear();
        StatMax.Clear();
        samples = 0;
        startedAt = EditorApplication.timeSinceStartup;

        Stage1RuntimeFrameProbeBehaviour.ResetCapture();
        GameObject probeObject = new GameObject("__Stage1RuntimeFrameProbe");
        probeObject.hideFlags = HideFlags.HideAndDontSave;
        runtimeFrameProbe = probeObject.AddComponent<Stage1RuntimeFrameProbeBehaviour>();

        Stage1LodTransitionLogger.LoggingEnabled = false;
        running = true;
        EditorApplication.update += Sample;

        File.WriteAllText(ReportPath, "RUNNING\n");
        Debug.Log("[Stage1 Runtime Profile] Capturing 8 seconds of Play Mode stats...");
    }

    private static void Sample()
    {
        if (!running)
            return;

        if (!EditorApplication.isPlaying)
        {
            Finish("ABORTED: Play Mode ended before capture completed.");
            return;
        }

        foreach (string statName in StatNames)
        {
            if (!TryReadUnityStat(statName, out long value))
                continue;

            StatSums[statName] = StatSums.TryGetValue(statName, out long sum) ? sum + value : value;
            StatMax[statName] = StatMax.TryGetValue(statName, out long max) ? Math.Max(max, value) : value;
        }

        samples++;
        if (EditorApplication.timeSinceStartup - startedAt >= DurationSeconds)
            Finish(null);
    }

    private static void Finish(string abortReason)
    {
        running = false;
        EditorApplication.update -= Sample;
        Stage1LodTransitionLogger.LoggingEnabled = true;

        FrameMilliseconds.Clear();
        FrameMilliseconds.AddRange(Stage1RuntimeFrameProbeBehaviour.FrameMilliseconds);
        int renderedFrames = Stage1RuntimeFrameProbeBehaviour.RenderedFrames;

        if (runtimeFrameProbe != null)
        {
            UnityEngine.Object.DestroyImmediate(runtimeFrameProbe.gameObject);
            runtimeFrameProbe = null;
        }

        if (!string.IsNullOrEmpty(abortReason))
        {
            File.WriteAllText(ReportPath, abortReason + "\n");
            Debug.LogWarning("[Stage1 Runtime Profile] " + abortReason);
            return;
        }

        double elapsedSeconds = Math.Max(0.000001, EditorApplication.timeSinceStartup - startedAt);
        double averageFps = renderedFrames / elapsedSeconds;
        double averageMs = averageFps > 0.0 ? 1000.0 / averageFps : 0.0;
        double p95Ms = Percentile(FrameMilliseconds, 0.95);
        double p99Ms = Percentile(FrameMilliseconds, 0.99);

        var lines = new List<string>
        {
            "STAGE 1 OPTIMIZED RUNTIME PROFILE",
            $"Scene: {SceneManager.GetActiveScene().path}",
            $"Capture seconds: {DurationSeconds:F1}",
            $"Samples: {samples}",
            $"Rendered frames: {renderedFrames}",
            $"Frame samples: {FrameMilliseconds.Count}",
            $"Average FPS (Editor Play Mode): {averageFps:F1}",
            $"Average frame time: {averageMs:F2} ms",
            $"P95 frame time: {p95Ms:F2} ms",
            $"P99 frame time: {p99Ms:F2} ms",
            string.Empty,
            "RENDER STATS (Editor; averages/max while sampled)",
        };

        foreach (string statName in StatNames)
        {
            if (!StatSums.TryGetValue(statName, out long sum))
                continue;

            double average = samples > 0 ? (double)sum / samples : 0.0;
            long max = StatMax[statName];
            lines.Add($"{statName}: avg {average:N0}, max {max:N0}");
        }

        lines.Add(string.Empty);
        lines.Add("MEMORY SNAPSHOT");
        lines.Add($"Total allocated: {FormatBytes(Profiler.GetTotalAllocatedMemoryLong())}");
        lines.Add($"Total reserved: {FormatBytes(Profiler.GetTotalReservedMemoryLong())}");
        lines.Add($"Mono used: {FormatBytes(Profiler.GetMonoUsedSizeLong())}");
        lines.Add($"Mono heap: {FormatBytes(Profiler.GetMonoHeapSizeLong())}");

        string report = string.Join("\n", lines) + "\n";
        File.WriteAllText(ReportPath, report);
        Debug.Log("[Stage1 Runtime Profile] CAPTURE COMPLETE\n" + report);
    }

    private static bool TryReadUnityStat(string name, out long value)
    {
        value = 0;
        Type type = typeof(UnityStats);
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        PropertyInfo property = type.GetProperty(name, flags);
        if (property != null)
        {
            object result = property.GetValue(null, null);
            return TryConvertToLong(result, out value);
        }

        FieldInfo field = type.GetField(name, flags);
        if (field != null)
        {
            object result = field.GetValue(null);
            return TryConvertToLong(result, out value);
        }

        return false;
    }

    private static bool TryConvertToLong(object value, out long result)
    {
        result = 0;
        if (value == null)
            return false;

        try
        {
            result = Convert.ToInt64(value, CultureInfo.InvariantCulture);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static double Percentile(List<double> values, double percentile)
    {
        if (values.Count == 0)
            return 0.0;

        double[] sorted = values.OrderBy(value => value).ToArray();
        double position = (sorted.Length - 1) * percentile;
        int lower = Mathf.FloorToInt((float)position);
        int upper = Mathf.CeilToInt((float)position);
        if (lower == upper)
            return sorted[lower];

        double fraction = position - lower;
        return sorted[lower] + (sorted[upper] - sorted[lower]) * fraction;
    }

    private static string FormatBytes(long bytes)
    {
        const double mib = 1024.0 * 1024.0;
        return $"{bytes / mib:F1} MiB";
    }
}
#endif
