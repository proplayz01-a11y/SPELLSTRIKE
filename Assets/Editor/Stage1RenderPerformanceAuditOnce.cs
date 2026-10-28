#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
internal static class Stage1RenderPerformanceAuditOnce
{
    private const string Marker = "Temp/Stage1RenderPerformanceAudit.trigger";
    private const string ScenePath = "Assets/Scenes/OptimizationTests/Stage1_OptimizedMap_Test.unity";

    static Stage1RenderPerformanceAuditOnce() => EditorApplication.update += TryRun;

    private static void TryRun()
    {
        string marker = Path.GetFullPath(Marker);
        if (!File.Exists(marker)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        File.Delete(marker);
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        LODGroup[] lodGroups = UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Stage1DistanceCullingController[] distanceCullers = UnityEngine.Object.FindObjectsByType<Stage1DistanceCullingController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Light[] lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Camera[] cameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        long enabledTriangles = 0;
        int enabledRenderers = 0;
        int enabledMeshRenderers = 0;
        int enabledSkinnedRenderers = 0;
        var materialSet = new HashSet<Material>();

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null) continue;
            foreach (Material material in renderer.sharedMaterials)
                if (material != null) materialSet.Add(material);

            if (!renderer.enabled || !renderer.gameObject.activeInHierarchy || renderer.forceRenderingOff)
                continue;

            enabledRenderers++;
            if (renderer is MeshRenderer) enabledMeshRenderers++;
            if (renderer is SkinnedMeshRenderer) enabledSkinnedRenderers++;

            Mesh mesh = null;
            if (renderer is MeshRenderer)
                mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
            else if (renderer is SkinnedMeshRenderer smr)
                mesh = smr.sharedMesh;

            if (mesh == null) continue;
            for (int sub = 0; sub < mesh.subMeshCount; sub++)
                enabledTriangles += (long)mesh.GetIndexCount(sub) / 3L;
        }

        int realtimeLights = lights.Count(l => l != null && l.enabled && l.gameObject.activeInHierarchy && l.lightmapBakeType == LightmapBakeType.Realtime);
        int shadowLights = lights.Count(l => l != null && l.enabled && l.gameObject.activeInHierarchy && l.shadows != LightShadows.None);

        Camera main = Camera.main ?? cameras.FirstOrDefault(c => c != null && c.enabled && c.gameObject.activeInHierarchy);
        UniversalRenderPipelineAsset urp = UniversalRenderPipeline.asset;

        string report =
            $"Scene={scene.path}\n" +
            $"renderers total={renderers.Length} enabledActive={enabledRenderers} mesh={enabledMeshRenderers} skinned={enabledSkinnedRenderers}\n" +
            $"enabledTriangleEstimate={enabledTriangles:N0}\n" +
            $"uniqueMaterials={materialSet.Count}\n" +
            $"lodGroups={lodGroups.Length}\n" +
            $"distanceCullers={distanceCullers.Length}\n" +
            $"lights total={lights.Length} realtimeActive={realtimeLights} shadowActive={shadowLights}\n" +
            $"camera={(main != null ? main.name : "NONE")} far={(main != null ? main.farClipPlane : 0f):F1} hdr={(main != null && main.allowHDR)} msaa={(main != null && main.allowMSAA)} occlusion={(main != null && main.useOcclusionCulling)}\n" +
            $"urp={(urp != null ? urp.name : "NONE")} renderScale={(urp != null ? urp.renderScale : 0f):F2} shadowDistance={(urp != null ? urp.shadowDistance : 0f):F1} mainShadows={(urp != null && urp.supportsMainLightShadows)} addShadows={(urp != null && urp.supportsAdditionalLightShadows)} msaa={(urp != null ? urp.msaaSampleCount : 0)} hdr={(urp != null && urp.supportsHDR)}\n" +
            $"quality={QualitySettings.names[QualitySettings.GetQualityLevel()]} lodBias={QualitySettings.lodBias:F2} mipLimit={QualitySettings.globalTextureMipmapLimit} vSync={QualitySettings.vSyncCount}\n";

        File.WriteAllText(Path.GetFullPath("Temp/Stage1RenderPerformanceAudit.txt"), report);
        Debug.Log("[Stage1 Render Audit] " + report.Replace("\n", " | "));
    }
}
#endif
