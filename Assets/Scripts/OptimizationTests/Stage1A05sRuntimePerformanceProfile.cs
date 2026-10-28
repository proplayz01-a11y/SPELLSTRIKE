#if SPELLSTRIKE_STAGE1_A05S_TEST
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

internal sealed class Stage1A05sRuntimePerformanceProfile : MonoBehaviour
{
    private const string SceneName = "Stage1_OptimizedMap_Test";
    private const string MobileRootName = "level1_STAGE1_MOBILE_TEST";
    private const float TargetRenderScale = 0.60f;
    private const float TargetLodBias = 0.80f;
    private const float TargetFarClipPlane = 600f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (!string.Equals(SceneManager.GetActiveScene().name, SceneName, StringComparison.Ordinal))
            return;
        if (FindFirstObjectByType<Stage1A05sRuntimePerformanceProfile>() != null)
            return;

        GameObject host = new GameObject("__Stage1A05sRuntimePerformanceProfile");
        DontDestroyOnLoad(host);
        host.AddComponent<Stage1A05sRuntimePerformanceProfile>();
    }

    private IEnumerator Start()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        QualitySettings.lodBias = TargetLodBias;

        UniversalRenderPipelineAsset urp = UniversalRenderPipeline.asset;
        if (urp != null)
        {
            urp.renderScale = TargetRenderScale;
            urp.supportsHDR = false;
        }

        Camera camera = Camera.main ?? FindFirstObjectByType<Camera>();
        if (camera != null)
        {
            camera.allowHDR = false;
            camera.farClipPlane = Mathf.Min(camera.farClipPlane, TargetFarClipPlane);
            camera.useOcclusionCulling = true;
        }

        GameObject mapRoot = null;
        for (int i = 0; i < 180 && mapRoot == null; i++)
        {
            mapRoot = GameObject.Find(MobileRootName);
            if (mapRoot == null)
                yield return null;
        }

        if (mapRoot == null)
        {
            Debug.LogError("[Stage1 A05s Profile] Mobile map root not found.");
            yield break;
        }

        int renderers = 0;
        foreach (Renderer renderer in mapRoot.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null) continue;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderers++;
        }

        Stage1DistanceCullingController culler = mapRoot.GetComponent<Stage1DistanceCullingController>();
        if (culler != null)
            culler.enabled = true;

        Debug.Log(
            $"[Stage1 A05s Profile] ACTIVE scale={TargetRenderScale:F2} lodBias={TargetLodBias:F2} " +
            $"HDR=off farClip={(camera != null ? camera.farClipPlane : 0f):F0} occlusion={(camera != null && camera.useOcclusionCulling)} " +
            $"mapShadows=off mapReceiveShadows=off renderers={renderers} " +
            $"LODGroups={mapRoot.GetComponentsInChildren<LODGroup>(true).Length} culler={(culler != null)}");
    }
}
#endif
