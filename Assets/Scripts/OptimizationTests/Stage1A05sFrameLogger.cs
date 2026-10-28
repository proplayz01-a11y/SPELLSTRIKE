#if SPELLSTRIKE_STAGE1_A05S_TEST
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

internal sealed class Stage1A05sFrameLogger : MonoBehaviour
{
    private const string SceneName = "Stage1_OptimizedMap_Test";
    private const float WindowSeconds = 20f;
    private readonly List<float> frameMs = new List<float>(1024);
    private float elapsed;
    private int window;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (!string.Equals(SceneManager.GetActiveScene().name, SceneName, StringComparison.Ordinal)) return;
        if (FindFirstObjectByType<Stage1A05sFrameLogger>() != null) return;
        GameObject host = new GameObject("__Stage1A05sFrameLogger");
        DontDestroyOnLoad(host);
        host.AddComponent<Stage1A05sFrameLogger>();
    }

    private void Start()
    {
        Debug.Log("[A05S BENCHMARK] READY automaticWindow=20s metric=unscaledDeltaTime");
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (dt <= 0f || dt >= 0.2f) return;
        frameMs.Add(dt * 1000f);
        elapsed += dt;
        if (elapsed >= WindowSeconds) Emit();
    }

    private void Emit()
    {
        if (frameMs.Count < 2) { ResetWindow(); return; }
        frameMs.Sort();
        double sum = 0;
        for (int i = 0; i < frameMs.Count; i++) sum += frameMs[i];
        double avg = sum / frameMs.Count;
        float p95 = Percentile(0.95f);
        float p99 = Percentile(0.99f);
        float onePercentFrameMs = Percentile(0.99f);
        double onePercentLowFps = onePercentFrameMs > 0 ? 1000.0 / onePercentFrameMs : 0;
        window++;
        Debug.Log($"[A05S BENCHMARK] window={window} frames={frameMs.Count} fps={1000.0 / avg:F3} avgMs={avg:F3} p95Ms={p95:F3} p99Ms={p99:F3} onePercentLowFps={onePercentLowFps:F3}");
        ResetWindow();
    }

    private float Percentile(float p)
    {
        int index = Mathf.Clamp(Mathf.FloorToInt(p * (frameMs.Count - 1)), 0, frameMs.Count - 1);
        return frameMs[index];
    }

    private void ResetWindow()
    {
        frameMs.Clear();
        elapsed = 0f;
    }
}
#endif
