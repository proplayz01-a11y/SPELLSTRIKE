#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
public static class Stage1GpuDrivenExperimentMenu
{
    private const string TestSceneName = "Stage1_OptimizedMap_Test";
    private const string TriggerPath = "Temp/Stage1GpuDrivenExperiment.trigger";
    private const string BaselinePipelinePath = "Assets/Settings/Mobile_RPAsset.asset";
    private const string BaselineRendererPath = "Assets/Settings/Mobile_Renderer.asset";
    private const string ExperimentFolder = "Assets/Settings/OptimizationTests";
    private const string ForwardPlusRendererPath = ExperimentFolder + "/Stage1_ForwardPlus_Renderer.asset";
    private const string ResidentPipelinePath = ExperimentFolder + "/Stage1_GPUDriven_RPAsset.asset";
    private const string ResidentOcclusionPipelinePath = ExperimentFolder + "/Stage1_GPUDrivenOcclusion_RPAsset.asset";


    static Stage1GpuDrivenExperimentMenu()
    {
        EditorApplication.update += WatchTrigger;
    }

    private static void WatchTrigger()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;

        string fullPath = Path.GetFullPath(TriggerPath);
        if (!File.Exists(fullPath))
            return;

        string command;
        try
        {
            command = File.ReadAllText(fullPath).Trim();
            File.Delete(fullPath);
        }
        catch (IOException)
        {
            return;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            return;
        }

        if (string.Equals(command, "baseline", StringComparison.OrdinalIgnoreCase))
            UseBaselineInPlayMode();
        else if (string.Equals(command, "resident", StringComparison.OrdinalIgnoreCase))
            UseResidentDrawerInPlayMode();
        else if (string.Equals(command, "occlusion", StringComparison.OrdinalIgnoreCase))
            UseResidentDrawerOcclusionInPlayMode();
        else if (string.Equals(command, "audit", StringComparison.OrdinalIgnoreCase))
            AuditExperiment();
        else
            Debug.LogError($"[Stage1 GPU Driven] Unknown trigger command: {command}");
    }
    [MenuItem("SPELLSTRIKE/Optimization/11. Prepare GPU Driven Experiment Assets")]
    public static void PrepareExperimentAssets()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[Stage1 GPU Driven] Exit Play Mode before preparing experiment assets.");
            return;
        }

        EnsureFolder(ExperimentFolder);
        CopyIfMissing(BaselineRendererPath, ForwardPlusRendererPath);
        CopyIfMissing(BaselinePipelinePath, ResidentPipelinePath);
        CopyIfMissing(BaselinePipelinePath, ResidentOcclusionPipelinePath);
        AssetDatabase.Refresh();

        UniversalRendererData renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(ForwardPlusRendererPath);
        UniversalRenderPipelineAsset resident = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(ResidentPipelinePath);
        UniversalRenderPipelineAsset residentOcclusion = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(ResidentOcclusionPipelinePath);

        if (renderer == null || resident == null || residentOcclusion == null)
        {
            Debug.LogError("[Stage1 GPU Driven] Could not load one or more duplicated experiment assets. No production asset was modified.");
            return;
        }

        ConfigureForwardPlusRenderer(renderer);
        ConfigurePipeline(resident, renderer, enableGpuOcclusion: false);
        ConfigurePipeline(residentOcclusion, renderer, enableGpuOcclusion: true);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "[Stage1 GPU Driven] EXPERIMENT ASSETS READY\n" +
            $"Forward+ renderer: {ForwardPlusRendererPath}\n" +
            $"Resident Drawer: {ResidentPipelinePath}\n" +
            $"Resident Drawer + GPU Occlusion: {ResidentOcclusionPipelinePath}\n" +
            "Production Mobile_RPAsset and Mobile_Renderer were not modified.\n" +
            "Use the Play Mode menu items to switch variants temporarily.");
    }

    [MenuItem("SPELLSTRIKE/Optimization/12. GPU Test - Baseline Mobile Pipeline")]
    public static void UseBaselineInPlayMode() => ApplyPlayModePipeline(BaselinePipelinePath, "BASELINE Forward");

    [MenuItem("SPELLSTRIKE/Optimization/13. GPU Test - Forward+ + Resident Drawer")]
    public static void UseResidentDrawerInPlayMode() => ApplyPlayModePipeline(ResidentPipelinePath, "Forward+ + Resident Drawer");

    [MenuItem("SPELLSTRIKE/Optimization/14. GPU Test - Resident Drawer + GPU Occlusion")]
    public static void UseResidentDrawerOcclusionInPlayMode() => ApplyPlayModePipeline(ResidentOcclusionPipelinePath, "Forward+ + Resident Drawer + GPU Occlusion");

    [MenuItem("SPELLSTRIKE/Optimization/15. Audit GPU Driven Experiment")]
    public static void AuditExperiment()
    {
        UniversalRendererData renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(ForwardPlusRendererPath);
        UniversalRenderPipelineAsset resident = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(ResidentPipelinePath);
        UniversalRenderPipelineAsset residentOcclusion = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(ResidentOcclusionPipelinePath);
        UniversalRenderPipelineAsset baseline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(BaselinePipelinePath);

        if (baseline == null)
        {
            Debug.LogError($"[Stage1 GPU Driven] Missing baseline pipeline: {BaselinePipelinePath}");
            return;
        }

        string rendererMode = renderer == null ? "missing" : ReadInt(renderer, "m_RenderingMode").ToString();
        string residentMode = resident == null ? "missing" : ReadInt(resident, "m_GPUResidentDrawerMode").ToString();
        string residentOcc = resident == null ? "missing" : ReadBool(resident, "m_GPUResidentDrawerEnableOcclusionCullingInCameras").ToString();
        string gpuOccMode = residentOcclusion == null ? "missing" : ReadInt(residentOcclusion, "m_GPUResidentDrawerMode").ToString();
        string gpuOccEnabled = residentOcclusion == null ? "missing" : ReadBool(residentOcclusion, "m_GPUResidentDrawerEnableOcclusionCullingInCameras").ToString();

        Debug.Log(
            "[Stage1 GPU Driven] AUDIT\n" +
            $"Baseline asset: {BaselinePipelinePath}\n" +
            $"Experimental renderer exists: {renderer != null}\n" +
            $"Experimental renderer m_RenderingMode: {rendererMode} (expected 2 = Forward+)\n" +
            $"Resident pipeline exists: {resident != null}\n" +
            $"Resident m_GPUResidentDrawerMode: {residentMode} (expected 1)\n" +
            $"Resident GPU occlusion: {residentOcc} (expected False)\n" +
            $"GPU-occlusion pipeline exists: {residentOcclusion != null}\n" +
            $"GPU-occlusion m_GPUResidentDrawerMode: {gpuOccMode} (expected 1)\n" +
            $"GPU-occlusion enabled: {gpuOccEnabled} (expected True)\n" +
            $"Play Mode: {EditorApplication.isPlaying}\n" +
            $"Active runtime quality pipeline: {(QualitySettings.renderPipeline != null ? QualitySettings.renderPipeline.name : "null/default")}");
    }

    private static void ApplyPlayModePipeline(string assetPath, string label)
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogError("[Stage1 GPU Driven] This switch is Play-Mode-only so project quality settings are not persisted. Enter Play Mode first.");
            return;
        }

        if (!string.Equals(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, TestSceneName, StringComparison.Ordinal))
        {
            Debug.LogError($"[Stage1 GPU Driven] Refusing to switch outside {TestSceneName}.");
            return;
        }

        UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
        if (pipeline == null)
        {
            Debug.LogError($"[Stage1 GPU Driven] Missing experiment pipeline: {assetPath}. Run Prepare GPU Driven Experiment Assets first.");
            return;
        }

        QualitySettings.renderPipeline = pipeline;
        Debug.Log($"[Stage1 GPU Driven] PLAY MODE VARIANT ACTIVE: {label} | {assetPath}");
    }

    private static void ConfigureForwardPlusRenderer(UniversalRendererData renderer)
    {
        var so = new SerializedObject(renderer);
        SerializedProperty mode = so.FindProperty("m_RenderingMode");
        if (mode == null)
            throw new InvalidOperationException("UniversalRendererData no longer exposes serialized m_RenderingMode.");
        mode.intValue = (int)RenderingMode.ForwardPlus;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(renderer);
    }

    private static void ConfigurePipeline(UniversalRenderPipelineAsset pipeline, UniversalRendererData renderer, bool enableGpuOcclusion)
    {
        var so = new SerializedObject(pipeline);
        SerializedProperty rendererList = so.FindProperty("m_RendererDataList");
        SerializedProperty defaultRenderer = so.FindProperty("m_DefaultRendererIndex");
        SerializedProperty residentMode = so.FindProperty("m_GPUResidentDrawerMode");
        SerializedProperty gpuOcclusion = so.FindProperty("m_GPUResidentDrawerEnableOcclusionCullingInCameras");
        SerializedProperty smallMesh = so.FindProperty("m_SmallMeshScreenPercentage");

        if (rendererList == null || defaultRenderer == null || residentMode == null || gpuOcclusion == null)
            throw new InvalidOperationException("URP serialized GPU-driven fields do not match the expected Unity 6 / URP 17 layout.");

        if (rendererList.arraySize == 0)
            rendererList.arraySize = 1;
        rendererList.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
        defaultRenderer.intValue = 0;
        residentMode.intValue = 1; // GPUResidentDrawerMode.InstancedDrawing
        gpuOcclusion.boolValue = enableGpuOcclusion;
        if (smallMesh != null)
            smallMesh.floatValue = 0f; // Do not add small-mesh culling to this experiment yet.

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(pipeline);
    }

    private static int ReadInt(UnityEngine.Object target, string property)
    {
        SerializedProperty p = new SerializedObject(target).FindProperty(property);
        return p != null ? p.intValue : int.MinValue;
    }

    private static bool ReadBool(UnityEngine.Object target, string property)
    {
        SerializedProperty p = new SerializedObject(target).FindProperty(property);
        return p != null && p.boolValue;
    }

    private static void CopyIfMissing(string source, string destination)
    {
        if (AssetDatabase.LoadMainAssetAtPath(destination) != null)
            return;
        if (!AssetDatabase.CopyAsset(source, destination))
            throw new InvalidOperationException($"Failed to copy {source} to {destination}.");
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif
