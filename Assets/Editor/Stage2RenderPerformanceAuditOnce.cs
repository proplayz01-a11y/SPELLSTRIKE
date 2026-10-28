#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class Stage2RenderPerformanceAuditOnce
{
    private const string ScenePath = "Assets/Scenes/Stage2Scene/Stage2Scene.unity";

    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(r => r != null && r.gameObject.scene == scene).ToArray();
        var lodGroups = UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(x => x != null && x.gameObject.scene == scene).ToArray();
        var lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(x => x != null && x.gameObject.scene == scene).ToArray();
        var cameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(x => x != null && x.gameObject.scene == scene).ToArray();
        var particles = UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(x => x != null && x.gameObject.scene == scene).ToArray();
        var probes = UnityEngine.Object.FindObjectsByType<ReflectionProbe>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(x => x != null && x.gameObject.scene == scene).ToArray();
        var volumes = UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(x => x != null && x.gameObject.scene == scene).ToArray();
        var meshColliders = UnityEngine.Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(x => x != null && x.gameObject.scene == scene).ToArray();

        var activeRenderers = renderers.Where(IsRendered).ToArray();
        var materials = new HashSet<Material>();
        var meshes = new HashSet<Mesh>();
        long triangleInstances = 0;
        long uniqueMeshTriangles = 0;
        long vertexInstances = 0;
        int materialSlots = 0;
        int castShadow = 0;
        int receiveShadow = 0;
        int staticRenderers = 0;

        var rendererRows = new List<(Renderer renderer, long triangles, long vertices, int slots)>();
        foreach (var r in activeRenderers)
        {
            foreach (var m in r.sharedMaterials)
                if (m != null) materials.Add(m);
            materialSlots += r.sharedMaterials?.Length ?? 0;
            if (r.shadowCastingMode != ShadowCastingMode.Off) castShadow++;
            if (r.receiveShadows) receiveShadow++;
            if ((GameObjectUtility.GetStaticEditorFlags(r.gameObject) & StaticEditorFlags.BatchingStatic) != 0) staticRenderers++;

            Mesh mesh = null;
            if (r is MeshRenderer)
                mesh = r.GetComponent<MeshFilter>()?.sharedMesh;
            else if (r is SkinnedMeshRenderer smr)
                mesh = smr.sharedMesh;

            long tris = 0;
            long verts = 0;
            if (mesh != null)
            {
                verts = mesh.vertexCount;
                for (int s = 0; s < mesh.subMeshCount; s++)
                    tris += (long)mesh.GetIndexCount(s) / 3L;
                triangleInstances += tris;
                vertexInstances += verts;
                meshes.Add(mesh);
            }
            rendererRows.Add((r, tris, verts, r.sharedMaterials?.Length ?? 0));
        }

        foreach (var mesh in meshes)
        {
            for (int s = 0; s < mesh.subMeshCount; s++)
                uniqueMeshTriangles += (long)mesh.GetIndexCount(s) / 3L;
        }

        var shaderCounts = materials
            .Where(m => m != null)
            .GroupBy(m => m.shader != null ? m.shader.name : "<missing>")
            .Select(g => new { Shader = g.Key, Materials = g.Count() })
            .OrderByDescending(x => x.Materials)
            .ThenBy(x => x.Shader)
            .ToArray();

        int activeParticles = particles.Count(p => p.gameObject.activeInHierarchy && p.GetComponent<ParticleSystemRenderer>()?.enabled != false);
        long particleCapacity = particles.Where(p => p.gameObject.activeInHierarchy).Sum(p => (long)p.main.maxParticles);
        int activeLights = lights.Count(l => l.enabled && l.gameObject.activeInHierarchy);
        int realtimeLights = lights.Count(l => l.enabled && l.gameObject.activeInHierarchy && l.lightmapBakeType == LightmapBakeType.Realtime);
        int shadowLights = lights.Count(l => l.enabled && l.gameObject.activeInHierarchy && l.shadows != LightShadows.None);

        Camera main = Camera.main ?? cameras.FirstOrDefault(c => c.enabled && c.gameObject.activeInHierarchy);
        UniversalRenderPipelineAsset urp = UniversalRenderPipeline.asset;
        var sb = new StringBuilder();
        sb.AppendLine($"Scene={scene.path}");
        sb.AppendLine($"MeshColliders={meshColliders.Length}");
        sb.AppendLine($"Renderers total={renderers.Length} activeEnabled={activeRenderers.Length} MeshRenderer={activeRenderers.Count(r => r is MeshRenderer)} Skinned={activeRenderers.Count(r => r is SkinnedMeshRenderer)} ParticleRenderer={activeRenderers.Count(r => r is ParticleSystemRenderer)}");
        sb.AppendLine($"Geometry instanceTriangles={triangleInstances:N0} instanceVertices={vertexInstances:N0} uniqueMeshes={meshes.Count} uniqueMeshTriangles={uniqueMeshTriangles:N0}");
        sb.AppendLine($"Materials unique={materials.Count} materialSlots={materialSlots} shadowCasters={castShadow} shadowReceivers={receiveShadow} batchingStaticRenderers={staticRenderers}");
        sb.AppendLine($"LODGroups={lodGroups.Length} totalLODLevels={lodGroups.Sum(g => g.lodCount)}");
        sb.AppendLine($"Particles total={particles.Length} active={activeParticles} activeMaxParticleCapacity={particleCapacity:N0}");
        sb.AppendLine($"Lights total={lights.Length} active={activeLights} realtimeActive={realtimeLights} shadowActive={shadowLights}");
        sb.AppendLine($"ReflectionProbes total={probes.Length} active={probes.Count(p => p.enabled && p.gameObject.activeInHierarchy)} realtime={probes.Count(p => p.enabled && p.gameObject.activeInHierarchy && p.mode == ReflectionProbeMode.Realtime)}");
        sb.AppendLine($"Volumes total={volumes.Length} active={volumes.Count(v => v.enabled && v.gameObject.activeInHierarchy)}");
        sb.AppendLine($"Camera={(main ? main.name : "NONE")} far={(main ? main.farClipPlane : 0f):F1} hdr={(main && main.allowHDR)} msaa={(main && main.allowMSAA)} occlusion={(main && main.useOcclusionCulling)} depth={(main ? main.depthTextureMode.ToString() : "NONE")}");
        sb.AppendLine($"URP={(urp ? urp.name : "NONE")} renderScale={(urp ? urp.renderScale : 0f):F2} shadowDistance={(urp ? urp.shadowDistance : 0f):F1} mainShadowRes={(urp ? urp.mainLightShadowmapResolution : 0)} mainShadows={(urp && urp.supportsMainLightShadows)} addLights={(urp ? urp.additionalLightsRenderingMode.ToString() : "NONE")} addShadows={(urp && urp.supportsAdditionalLightShadows)} msaa={(urp ? urp.msaaSampleCount : 0)} hdr={(urp && urp.supportsHDR)}");
        sb.AppendLine($"Quality={QualitySettings.names[QualitySettings.GetQualityLevel()]} lodBias={QualitySettings.lodBias:F2} mipLimit={QualitySettings.globalTextureMipmapLimit} vSync={QualitySettings.vSyncCount} shadowDistance={QualitySettings.shadowDistance:F1}");

        sb.AppendLine("\nLIGHTS");
        foreach (var l in lights.OrderByDescending(l => l.enabled && l.gameObject.activeInHierarchy).ThenBy(l => l.name))
            sb.AppendLine($"{GetPath(l.transform)} | active={l.enabled && l.gameObject.activeInHierarchy} type={l.type} bake={l.lightmapBakeType} shadows={l.shadows} intensity={l.intensity:F2} range={l.range:F1}");

        sb.AppendLine("\nCAMERAS");
        foreach (var c in cameras.OrderBy(c => c.name))
            sb.AppendLine($"{GetPath(c.transform)} | active={c.enabled && c.gameObject.activeInHierarchy} far={c.farClipPlane:F1} hdr={c.allowHDR} msaa={c.allowMSAA} occlusion={c.useOcclusionCulling} depth={c.depthTextureMode}");

        sb.AppendLine("\nSHADERS BY UNIQUE MATERIAL COUNT");
        foreach (var x in shaderCounts.Take(40))
            sb.AppendLine($"{x.Materials,4} | {x.Shader}");

        sb.AppendLine("\nTOP ACTIVE RENDERERS BY TRIANGLES");
        foreach (var x in rendererRows.OrderByDescending(x => x.triangles).Take(60))
            sb.AppendLine($"{x.triangles,10:N0} tris | {x.vertices,9:N0} verts | slots={x.slots} | cast={x.renderer.shadowCastingMode} recv={x.renderer.receiveShadows} | {GetPath(x.renderer.transform)}");

        sb.AppendLine("\nROOT ACTIVE RENDERER BREAKDOWN");
        foreach (var root in scene.GetRootGameObjects().OrderBy(go => go.name))
        {
            var rr = root.GetComponentsInChildren<Renderer>(true).Where(IsRendered).ToArray();
            long tris = 0;
            foreach (var r in rr)
            {
                Mesh mesh = r is SkinnedMeshRenderer smr ? smr.sharedMesh : r.GetComponent<MeshFilter>()?.sharedMesh;
                if (mesh == null) continue;
                for (int s = 0; s < mesh.subMeshCount; s++) tris += (long)mesh.GetIndexCount(s) / 3L;
            }
            sb.AppendLine($"{root.name} | activeRenderers={rr.Length} instanceTriangles={tris:N0}");
        }

        string reportPath = Path.GetFullPath("Temp/Stage2RenderPerformanceAudit.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
        File.WriteAllText(reportPath, sb.ToString());
        Debug.Log("[Stage2 Render Audit] " + sb.ToString().Replace("\n", " | "));
    }

    private static bool IsRendered(Renderer r) => r.enabled && r.gameObject.activeInHierarchy && !r.forceRenderingOff;

    private static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
#endif
