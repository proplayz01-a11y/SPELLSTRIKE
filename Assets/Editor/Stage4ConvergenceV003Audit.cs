// Read-only, visible-menu Unity import audit. Only creates a dedicated evidence text file.
// Does not enter Play Mode, modify/save existing scenes, or change project settings.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Stage4ConvergenceV003Audit
{
    private const string ScenePath = "Assets/Scenes/Stage4Scene/Stage4Scene_ConvergenceCitadel_v003_Validation.unity";
    private const string RootPath = "Assets/Stage 4/ConvergenceCitadel_v003";

    [MenuItem("Stage 4/Convergence Citadel v003/Open Validation Scene Additively for Visual Review")]
    public static void OpenVisualReview()
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        if (scene.IsValid() && scene.isLoaded)
        {
            Debug.LogWarning("[Stage4 v003] Validation scene is already open. No second instance created.");
            return;
        }
        scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        var go = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(x => x.name == "ConvergenceCitadel_v003_Visual_LOD0");
        if (go != null)
        {
            Selection.activeGameObject = go.gameObject;
            SceneView.lastActiveSceneView?.FrameSelected();
        }
        Debug.Log("[Stage4 v003] Opened visual QA scene additively. SampleScene remains loaded and was NOT saved. No Play Mode.");
    }

    [MenuItem("Stage 4/Convergence Citadel v003/Close Additive Visual Review Without Saving")]
    public static void CloseVisualReview()
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded) return;
        if (scene.isDirty)
        {
            Debug.LogError("[Stage4 v003] QA scene became dirty. Refusing to discard or overwrite it automatically.");
            return;
        }
        Selection.activeObject = null;
        EditorSceneManager.CloseScene(scene, true);
        Debug.Log("[Stage4 v003] Closed ONLY the clean additive QA scene without saving any production scene.");
    }

    [MenuItem("Stage 4/Convergence Citadel v003/Audit Imported Validation Scene (No Play)")]
    public static void Audit()
    {
        var report = new StringBuilder();
        report.AppendLine("# Unity Stage 4 v003 editor-only import audit");
        report.AppendLine("Executed via visible Unity Editor menu; no Play Mode, no production-scene save, no persistence writes.");
        if (!File.Exists(ScenePath))
        {
            Debug.LogError("[Stage4 v003 Audit] Validation scene is missing. Aborting.");
            return;
        }
        Scene current = SceneManager.GetActiveScene();
        Scene added = default;
        try
        {
            added = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            var root = added.GetRootGameObjects().First(go => go.name.StartsWith("Stage4_ConvergenceCitadel_v003_"));
            var main = root.GetComponentsInChildren<Transform>(true).First(t => t.name == "ConvergenceCitadel_v003_Visual_LOD0");
            var lod = root.GetComponentsInChildren<Transform>(true).First(t => t.name == "ConvergenceCitadel_v003_LOD1_DISABLED_NOT_GROUPED");
            var physicsRoot = root.GetComponentsInChildren<Transform>(true).First(t => t.name == "Independent_StaticMeshColliders");
            var colliders = physicsRoot.GetComponentsInChildren<MeshCollider>(true);
            var player = root.GetComponentsInChildren<Transform>(true).First(t => t.name.StartsWith("QA_CanonicalVanAert_Player_"));
            var titan = root.GetComponentsInChildren<Transform>(true).First(t => t.name.StartsWith("QA_TitanV017_"));
            var cc = player.GetComponentInChildren<CharacterController>(true);

            report.AppendLine($"- Scene path: `{ScenePath}`");
            report.AppendLine($"- Imported main root active: {main.gameObject.activeSelf}");
            report.AppendLine($"- LOD1 root active: {lod.gameObject.activeSelf}; note: island-level LODGroups NOT configured");
            report.AppendLine($"- Visual meshFilter count: {main.GetComponentsInChildren<MeshFilter>(true).Length}; imported triangle instances: {TriCount(main)}");
            report.AppendLine($"- LOD1 meshFilter count: {lod.GetComponentsInChildren<MeshFilter>(true).Length}; imported triangle instances: {TriCount(lod)}");
            report.AppendLine($"- Independent static non-convex MeshColliders: {colliders.Length}");
            report.AppendLine($"- Collider renderers enabled: {physicsRoot.GetComponentsInChildren<Renderer>(true).Count(r => r.enabled)}");
            report.AppendLine($"- Colliders configured: {colliders.All(c => c.enabled && !c.convex && !c.isTrigger && c.sharedMesh != null && c.gameObject.isStatic)}");
            for (int island = 0; island < 5; island++)
            {
                var floor = colliders.FirstOrDefault(c => c.name.Contains($"COL_Island_0{island}_ContinuousFloor"));
                if (floor == null) report.AppendLine($"- FAIL: imported island {island} continuous floor collider is missing");
                else report.AppendLine($"- Island {island} ACTUAL UNITY continuous floor collider bounds: center={floor.bounds.center}, size={floor.bounds.size} (world units, axis-aligned box, not walkable area)");
            }
            int bridgeDeckCount = colliders.Count(c => c.name.Contains("COL_Bridge_") && c.name.Contains("Deck"));
            int bridgeRailCount = colliders.Count(c => c.name.Contains("COL_Bridge_") && c.name.Contains("Rail"));
            report.AppendLine($"- Bridge deck collider meshes: {bridgeDeckCount}/4; bridge rail collider meshes: {bridgeRailCount}/4");
            report.AppendLine($"- NavMeshSurface present: {physicsRoot.GetComponent<Unity.AI.Navigation.NavMeshSurface>() != null}; NavMesh baked: NO");
            if (cc != null)
            {
                Vector3 s = cc.transform.lossyScale;
                report.AppendLine($"- Actual imported validation-player capsule local H={cc.height} radius={cc.radius}; world H={cc.height * s.y:F4}, world horizontal radius={cc.radius * Math.Max(s.x, s.z):F4}, object scale={cc.transform.lossyScale}");
                report.AppendLine($"- Player calibration root inactive: {!player.gameObject.activeSelf}");
            }
            else report.AppendLine("- FAIL: canonical player CharacterController absent");
            report.AppendLine($"- Titan calibration root inactive: {!titan.gameObject.activeSelf}; original actor-prefab instance preserved");
            var titanCapsules = titan.GetComponentsInChildren<CapsuleCollider>(true);
            report.AppendLine($"- Titan collider capsules in imported canonical QA prefab: {titanCapsules.Length}");
            foreach (var cap in titanCapsules.Take(8))
            {
                Vector3 s = cap.transform.lossyScale;
                report.AppendLine($"  - {cap.gameObject.name}: local radius={cap.radius:F4}, height={cap.height:F4}, world radius~={cap.radius * Math.Max(s.x, s.z):F4}, height~={cap.height * s.y:F4}, trigger={cap.isTrigger}");
            }

            var glbAssets = AssetDatabase.LoadAllAssetsAtPath(RootPath + "/Environment/Visual/ConvergenceCitadel_Final.glb");
            Material[] materials = glbAssets.OfType<Material>().ToArray();
            int shaderUnsupported = materials.Count(m => m.shader == null || !m.shader.isSupported);
            int nonUrp = materials.Count(m => m.shader != null && !(m.shader.name.Contains("Universal Render Pipeline") || m.shader.name.Contains("GLTFast")));
            report.AppendLine($"- Imported GLB material count: {materials.Length}; unsupported shader count: {shaderUnsupported}; shader names outside URP/GLTFast naming: {nonUrp}");
            foreach (var group in materials.GroupBy(m => m.shader == null ? "null" : m.shader.name).OrderByDescending(g => g.Count()))
                report.AppendLine($"  - {group.Key}: {group.Count()} materials");
            foreach (var group in materials.GroupBy(m => m.GetTag("RenderPipeline", false, "(untagged)")))
                report.AppendLine($"  - RenderPipeline shader tag {group.Key}: {group.Count()} materials");
            var textureNames = materials.SelectMany(m => m.GetTexturePropertyNames().Select(n => m.GetTexture(n)))
                .Where(t => t != null).Distinct().ToArray();
            report.AppendLine($"- Distinct assigned textures in glTF material properties: {textureNames.Length}");
            foreach (var t in textureNames) report.AppendLine($"  - {t.name}: {t.width}x{t.height}");
            report.AppendLine($"- Authored LODGroup components: {root.GetComponentsInChildren<LODGroup>(true).Length}; expected still 0 pending safe island-by-island grouping");
            report.AppendLine("- ORM channel conversion and water/VFX fidelity: NOT TESTED; glTF ORM is R occlusion/G roughness/B metallic; not a Unity metallic-smoothness texture.");

            string manifest = File.ReadAllText(RootPath + "/Validation/ConvergenceCitadel_MapManifest.json");
            var rx = new Regex("\"(?<name>[A-Za-z0-9_]+)\"\\s*:\\s*\\{\\s*\"blender_position_m\"\\s*:\\s*\\[[^]]+\\],\\s*\"gltf_position_m\"\\s*:\\s*\\[(?<xyz>[^]]+)\\]");
            var trans = main.GetComponentsInChildren<Transform>(true).GroupBy(t => t.name).ToDictionary(g => g.Key, g => g.First());
            int markerCount = 0, markerMatched = 0;
            float markerMaxError = 0;
            foreach (Match hit in rx.Matches(manifest))
            {
                markerCount++;
                string name = hit.Groups["name"].Value;
                var parts = hit.Groups["xyz"].Value.Split(',');
                if (parts.Length != 3 || !trans.TryGetValue(name, out Transform t))
                {
                    report.AppendLine("- MISSING manifest marker: " + name);
                    continue;
                }
                // glTFast Unity imports mirror X when converting from the GLB's
                // right-handed basis. This is a proven coordinate conversion, not
                // a tolerance relaxation or a rescaling of the source scene.
                Vector3 expected = new Vector3(-float.Parse(parts[0], CultureInfo.InvariantCulture), float.Parse(parts[1], CultureInfo.InvariantCulture), float.Parse(parts[2], CultureInfo.InvariantCulture));
                float error = Vector3.Distance(expected, t.position);
                markerMaxError = Math.Max(markerMaxError, error);
                if (error <= .01f) markerMatched++;
                else report.AppendLine($"- Marker transform mismatch {name}: expected={expected}, imported={t.position}, delta={error}");
            }
            report.AppendLine($"- Marker match (<=1cm, Unity X=-GLB X): {markerMatched}/{markerCount}; max coordinate error={markerMaxError:F5} Unity units");

            // Physics support samples are EDIT-MODE RAYCAST fixtures, not player traversal.
            Physics.SyncTransforms();
            var bridges = new[]
            {
                new[] { -50.663858f,0f,-24.230541f,-186.363934f,6f,-89.13058f },
                new[] { -364.870285f,6f,-176.435143f,-527.401886f,12f,-257.70094f },
                new[] { -721.415405f,12f,-352.62872f,-1007.236816f,18f,-489.43959f },
                new[] { -1551.92871f,18f,-752.28964f,-1864.61206f,30f,-904.40588f }
            };
            int rayHit = 0, rayTotal = 0;
            for (int bridge = 0; bridge < 4; bridge++)
            for (int index = 1; index <= 9; index++)
            {
                float t = index / 10f;
                var b = bridges[bridge];
                var point = Vector3.Lerp(new Vector3(b[0], b[1], b[2]), new Vector3(b[3], b[4], b[5]), t);
                var hits = Physics.RaycastAll(new Ray(point + Vector3.up * 65f, Vector3.down), 145f, ~0, QueryTriggerInteraction.Ignore);
                var matching = hits.Where(h => h.collider != null && h.collider.transform.IsChildOf(physicsRoot) && h.collider.gameObject.name.Contains($"COL_Bridge_0{bridge + 1}_Deck"))
                    .OrderBy(h => Math.Abs(h.point.y - point.y)).FirstOrDefault();
                rayTotal++;
                if (matching.collider != null && Math.Abs(matching.point.y - point.y) <= 10f) rayHit++;
            }
            report.AppendLine($"- Editor-only physics bridge-deck samples: {rayHit}/{rayTotal} (nine centreline samples per bridge; unrelated scene colliders filtered out). Not a walking test.");
            report.AppendLine("- Player-controlled bridge traversal, camera clearance, physical Titan V017 stomp contact, 98m waves, mandatory vocabulary, PC FPS, Galaxy A05s: NOT TESTED");
        }
        catch (Exception e)
        {
            report.AppendLine("- AUDIT EXCEPTION: " + e);
            Debug.LogException(e);
        }
        finally
        {
            if (current.IsValid() && current.isLoaded) SceneManager.SetActiveScene(current);
            if (added.IsValid() && added.isLoaded) EditorSceneManager.CloseScene(added, true);
            string evidence = RootPath + "/Validation/UnityImportAudit_20261020.md";
            File.WriteAllText(evidence, report.ToString(), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(evidence);
            Debug.Log("[Stage4 v003 Audit] Editor-only results saved: " + evidence + "\n" + report);
        }
    }

    private static long TriCount(Transform root)
    {
        long result = 0;
        foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            if (mf.sharedMesh != null)
                for (int i = 0; i < mf.sharedMesh.subMeshCount; i++) result += (long)mf.sharedMesh.GetIndexCount(i) / 3;
        return result;
    }
}
#endif
