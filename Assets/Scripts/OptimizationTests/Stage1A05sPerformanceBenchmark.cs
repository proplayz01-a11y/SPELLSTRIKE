#if SPELLSTRIKE_STAGE1_A05S_BENCHMARK
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

internal sealed class Stage1A05sPerformanceBenchmark : MonoBehaviour
{
    private const string SceneName = "Stage1_OptimizedMap_Test";
    private const string MobileRootName = "level1_STAGE1_MOBILE_TEST";

    private GameObject mapRoot;
    private Renderer[] mapRenderers = Array.Empty<Renderer>();
    private ShadowCastingMode[] originalShadowModes = Array.Empty<ShadowCastingMode>();
    private Camera targetCamera;
    private UniversalRenderPipelineAsset urp;
    private Stage1DistanceCullingController culler;
    private float originalRenderScale;
    private bool originalCameraHdr;
    private bool originalUrpHdr;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (!string.Equals(SceneManager.GetActiveScene().name, SceneName, StringComparison.Ordinal))
            return;
        if (FindFirstObjectByType<Stage1A05sPerformanceBenchmark>() != null)
            return;

        GameObject host = new GameObject("__Stage1A05sPerformanceBenchmark");
        DontDestroyOnLoad(host);
        host.AddComponent<Stage1A05sPerformanceBenchmark>();
    }

    private IEnumerator Start()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;

        for (int i = 0; i < 180 && mapRoot == null; i++)
        {
            mapRoot = GameObject.Find(MobileRootName);
            if (mapRoot == null) yield return null;
        }

        if (mapRoot == null)
        {
            Debug.LogError("[Stage1 PerfBench] ABORT mobile map root not found.");
            yield break;
        }

        targetCamera = Camera.main ?? FindFirstObjectByType<Camera>();
        urp = UniversalRenderPipeline.asset;
        mapRenderers = mapRoot.GetComponentsInChildren<Renderer>(true);
        originalShadowModes = mapRenderers.Select(r => r != null ? r.shadowCastingMode : ShadowCastingMode.Off).ToArray();
        originalRenderScale = urp != null ? urp.renderScale : 1f;
        originalCameraHdr = targetCamera != null && targetCamera.allowHDR;
        originalUrpHdr = urp != null && urp.supportsHDR;

        culler = mapRoot.GetComponent<Stage1DistanceCullingController>();
        if (culler == null)
            culler = mapRoot.AddComponent<Stage1DistanceCullingController>();
        culler.enabled = false;

        Debug.Log($"[Stage1 PerfBench] START renderers={mapRenderers.Length} baselineScale={originalRenderScale:F2} cameraHDR={originalCameraHdr} urpHDR={originalUrpHdr} target=60");

        yield return new WaitForSecondsRealtime(4f);

        yield return RunPhase("A_BASELINE", 0.80f, false, false, false);
        yield return RunPhase("B_SCALE_070", 0.70f, false, false, false);
        yield return RunPhase("C_SCALE_060", 0.60f, false, false, false);
        yield return RunPhase("D_060_CULL", 0.60f, true, false, false);
        yield return RunPhase("E_060_CULL_NO_MAP_SHADOWS", 0.60f, true, true, false);
        yield return RunPhase("F_060_CULL_NO_MAP_SHADOWS_NO_HDR", 0.60f, true, true, true);

        Debug.Log("[Stage1 PerfBench] COMPLETE. Leaving phase F active for visual/device inspection.");
    }

    private IEnumerator RunPhase(string label, float renderScale, bool enableCulling, bool disableMapShadows, bool disableHdr)
    {
        ApplyPhase(renderScale, enableCulling, disableMapShadows, disableHdr);
        yield return new WaitForSecondsRealtime(2f);

        int startFrames = Time.frameCount;
        float start = Time.realtimeSinceStartup;
        var frameTimes = new List<float>(1024);
        float endAt = start + 10f;

        while (Time.realtimeSinceStartup < endAt)
        {
            float dt = Time.unscaledDeltaTime;
            if (dt > 0f && dt < 0.5f)
                frameTimes.Add(dt);
            yield return null;
        }

        float elapsed = Time.realtimeSinceStartup - start;
        int frames = Time.frameCount - startFrames;
        float fps = elapsed > 0f ? frames / elapsed : 0f;
        float avgMs = frames > 0 ? elapsed * 1000f / frames : 0f;
        float onePercentLow = CalculateOnePercentLow(frameTimes);
        int forcedOff = mapRenderers.Count(r => r != null && r.forceRenderingOff);
        int shadowCasters = mapRenderers.Count(r => r != null && r.shadowCastingMode != ShadowCastingMode.Off);

        Debug.Log(
            $"[Stage1 PerfBench] RESULT {label} fps={fps:F1} avgMs={avgMs:F1} onePercentLowFps={onePercentLow:F1} " +
            $"scale={renderScale:F2} culling={enableCulling} forcedOff={forcedOff}/{mapRenderers.Length} " +
            $"mapShadowCasters={shadowCasters} hdr={!disableHdr}");
    }

    private void ApplyPhase(float renderScale, bool enableCulling, bool disableMapShadows, bool disableHdr)
    {
        if (urp != null)
        {
            urp.renderScale = renderScale;
            urp.supportsHDR = disableHdr ? false : originalUrpHdr;
        }
        if (targetCamera != null)
            targetCamera.allowHDR = disableHdr ? false : originalCameraHdr;

        if (culler != null)
            culler.enabled = enableCulling;

        for (int i = 0; i < mapRenderers.Length; i++)
        {
            Renderer renderer = mapRenderers[i];
            if (renderer == null) continue;
            renderer.shadowCastingMode = disableMapShadows ? ShadowCastingMode.Off : originalShadowModes[i];
        }
    }

    private static float CalculateOnePercentLow(List<float> frameTimes)
    {
        if (frameTimes.Count == 0) return 0f;
        frameTimes.Sort();
        int slowCount = Mathf.Max(1, Mathf.CeilToInt(frameTimes.Count * 0.01f));
        float slowSeconds = 0f;
        for (int i = frameTimes.Count - slowCount; i < frameTimes.Count; i++)
            slowSeconds += frameTimes[i];
        float meanSlow = slowSeconds / slowCount;
        return meanSlow > 0f ? 1f / meanSlow : 0f;
    }
}
#endif
