// Stage 4 v003 specific visible-menu LOD assembly. Do not invoke Unity MCP.
// Only the two independently authored Stage4 scenes are modified; all other
// loaded scenes, production prefabs, player data and global settings are untouched.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Stage4ConvergenceV003LODSetup
{
    private const string Root = "Assets/Stage 4/ConvergenceCitadel_v003";
    private const string ProductionScaffold = "Assets/Scenes/Stage4Scene/Stage4Scene.unity";
    private const string ValidationScene = "Assets/Scenes/Stage4Scene/Stage4Scene_ConvergenceCitadel_v003_Validation.unity";
    private const string Output = Root + "/Validation/UnityLODAssembly_20261020.md";
    private const string BackupDir = "Library/Stage4ConvergenceV003_LOD_Backup_20261020";

    [MenuItem("Stage 4/Convergence Citadel v003/Preview LOD0 in Active QA Scene (Unsaved)")]
    public static void PreviewLOD0() => ForcePreviewLOD(0);

    [MenuItem("Stage 4/Convergence Citadel v003/Preview LOD1 in Active QA Scene (Unsaved)")]
    public static void PreviewLOD1() => ForcePreviewLOD(1);

    [MenuItem("Stage 4/Convergence Citadel v003/Restore Automatic LOD in Active QA Scene (Unsaved)")]
    public static void RestoreAutomaticLOD() => ForcePreviewLOD(-1);

    private static void ForcePreviewLOD(int requested)
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ValidationScene || Application.isPlaying)
        {
            Debug.LogError("[Stage4 LOD Preview] Requires active Stage4 v003 VALIDATION scene in Edit Mode; will not touch production scenes.");
            return;
        }
        var groups = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<LODGroup>(true)).ToArray();
        if (groups.Length != 11 || groups.Any(g => g.GetLODs().Length != 2))
        {
            Debug.LogError("[Stage4 LOD Preview] Expected 11 validated two-level LODGroups; refusing to force partial state.");
            return;
        }
        foreach (var group in groups) group.ForceLOD(requested);
        SceneView.RepaintAll();
        Debug.Log("[Stage4 LOD Preview] Forced " + requested + " for all 11 groups in active Stage4 validation SceneView only. No scene saved.");
    }

    [MenuItem("Stage 4/Convergence Citadel v003/Assemble Scene-Local LOD Groups (Backed Up)")]
    public static void Assemble()
    {
        if (File.Exists(Output))
        {
            Debug.LogError("[Stage4 LOD] A previous LOD assembly report exists; refusing a repeat until reviewed.");
            return;
        }
        var paths = new[] { ProductionScaffold, ValidationScene };
        foreach (string path in paths)
        {
            var open = SceneManager.GetSceneByPath(path);
            if (!File.Exists(path) || (open.IsValid() && open.isLoaded && open.isDirty))
            {
                Debug.LogError("[Stage4 LOD] Existing scene missing or has unsaved changes, do not overwrite: " + path);
                return;
            }
        }
        // Snapshots are raw files, never a re-creation from stale production scenes.
        Directory.CreateDirectory(BackupDir);
        foreach (string path in paths)
        {
            var dest = BackupDir + "/" + Path.GetFileName(path) + ".before_LOD";
            if (File.Exists(dest)) { Debug.LogError("[Stage4 LOD] Backup already exists: " + dest); return; }
        }
        foreach (string path in paths)
            File.Copy(path, BackupDir + "/" + Path.GetFileName(path) + ".before_LOD");

        var report = new StringBuilder();
        report.AppendLine("# Stage4 Convergence Citadel v003 — Unity LOD integration");
        report.AppendLine("Visible Unity Editor menu; Stage4 scenes only. Pre-LOD backups: `" + BackupDir + "`.");
        report.AppendLine("All LOD0 and LOD1 renderer names must match exactly. One LODGroup per environment category, not one per mesh.");
        Scene original = SceneManager.GetActiveScene();
        bool success = true;
        foreach (string path in paths)
        {
            Scene stage = default;
            bool wasAlreadyOpen = false;
            try
            {
                var existing = SceneManager.GetSceneByPath(path);
                wasAlreadyOpen = existing.IsValid() && existing.isLoaded;
                stage = wasAlreadyOpen ? existing : EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                var stageRoot = stage.GetRootGameObjects().Single(go => go.name.StartsWith("Stage4_ConvergenceCitadel_v003_"));
                var env = stageRoot.GetComponentsInChildren<Transform>(true).First(x => x.name == "00_Environment");
                var baseLod = env.GetComponentsInChildren<Transform>(true).First(x => x.name == "ConvergenceCitadel_v003_Visual_LOD0");
                var lesserLod = env.GetComponentsInChildren<Transform>(true).First(x => x.name == "ConvergenceCitadel_v003_LOD1_DISABLED_NOT_GROUPED");
                if (!baseLod.gameObject.activeSelf || lesserLod.gameObject.activeSelf ||
                    env.GetComponentsInChildren<LODGroup>(true).Length != 0)
                    throw new InvalidOperationException("Expected original scaffold with enabled LOD0, disabled LOD1, no groups: " + path);

                var a = baseLod.GetComponentsInChildren<MeshRenderer>(true);
                var b = lesserLod.GetComponentsInChildren<MeshRenderer>(true);
                if (a.Length != 110 || b.Length != 110)
                    throw new InvalidOperationException($"Expected 110/110 mesh renderers; imported {a.Length}/{b.Length}");
                var first = UniqueNames(a);
                var second = UniqueNames(b);
                if (!first.Keys.OrderBy(x => x).SequenceEqual(second.Keys.OrderBy(x => x)))
                    throw new InvalidOperationException("Cannot pair GLB renderer names one-to-one");

                string manifest = File.ReadAllText(Root + "/Validation/ConvergenceCitadel_MapManifest.json");
                var markerRx = new Regex("\"(?<name>[A-Za-z0-9_]+)\"\\s*:\\s*\\{\\s*\"blender_position_m\"");
                var markerNames = new HashSet<string>(markerRx.Matches(manifest).Cast<Match>().Select(m => m.Groups["name"].Value));
                if (markerNames.Count != 36) throw new InvalidOperationException("Expected 36 marker names, found " + markerNames.Count);
                var secondaryMarkers = lesserLod.GetComponentsInChildren<Transform>(true).Where(t => markerNames.Contains(t.name)).ToArray();
                if (secondaryMarkers.Length != 36) throw new InvalidOperationException("LOD1 has an unexpected marker set: " + secondaryMarkers.Length);

                // The imported LOD1 prefab is immutable. Disabling these scene-only
                // GameObjects prevents two active spawn/encounter marker sets.
                foreach (var marker in secondaryMarkers) marker.gameObject.SetActive(false);
                lesserLod.gameObject.SetActive(true);
                lesserLod.gameObject.name = "ConvergenceCitadel_v003_LOD1_SWITCHED_BY_LODGroups";

                var controller = new GameObject("07_SceneLocalLODGroups_11Categories");
                controller.transform.SetParent(env, false);
                var grouped = first.Keys.GroupBy(GroupName).OrderBy(x => x.Key).ToArray();
                int total0 = 0, total1 = 0;
                foreach (var group in grouped)
                {
                    var o = new GameObject("LOD_" + group.Key);
                    o.transform.SetParent(controller.transform, false);
                    var lod = o.AddComponent<LODGroup>();
                    lod.fadeMode = LODFadeMode.None;
                    var names = group.OrderBy(x => x).ToArray();
                    var renderer0 = names.Select(n => first[n]).Cast<Renderer>().ToArray();
                    var renderer1 = names.Select(n => second[n]).Cast<Renderer>().ToArray();
                    var lods = new[] { new LOD(0.26f, renderer0), new LOD(0.002f, renderer1) };
                    lod.SetLODs(lods);
                    lod.RecalculateBounds();
                    if (lod.GetLODs().Length != 2 || lod.GetLODs()[0].renderers.Length != names.Length || lod.GetLODs()[1].renderers.Length != names.Length)
                        throw new InvalidOperationException("Created invalid LOD group " + group.Key);
                    total0 += renderer0.Length;
                    total1 += renderer1.Length;
                    report.AppendLine($"- `{path}` group `{group.Key}`: {names.Length} renderer pairs; LODGroup center `{lod.localReferencePoint}`, size `{lod.size:F3}`");
                }

                if (total0 != 110 || total1 != 110)
                    throw new InvalidOperationException($"Renderer count mismatch {total0}/{total1}");
                int activeSecondaryMarkers = lesserLod.GetComponentsInChildren<Transform>(false).Count(t => markerNames.Contains(t.name));
                int activePrimaryMarkers = baseLod.GetComponentsInChildren<Transform>(false).Count(t => markerNames.Contains(t.name));
                if (activeSecondaryMarkers != 0 || activePrimaryMarkers != 36)
                    throw new InvalidOperationException($"Marker duplication: LOD0={activePrimaryMarkers}, LOD1={activeSecondaryMarkers}");

                var groups = controller.GetComponentsInChildren<LODGroup>();
                foreach (var group in groups) group.ForceLOD(-1);
                EditorSceneManager.MarkSceneDirty(stage);
                if (!EditorSceneManager.SaveScene(stage, path))
                    throw new IOException("Failed to save LODs in " + path);
                report.AppendLine($"- `{path}`: PASS, {groups.Length} categories, {total0}/{total1} renderer references, {activePrimaryMarkers} active primary markers, {activeSecondaryMarkers} secondary markers, backup preserved.");
                Debug.Log($"[Stage4 LOD] Scene-only {path}: {groups.Length} groups, {total0} paired renderers, no active duplicate markers.");
            }
            catch (Exception e)
            {
                success = false;
                report.AppendLine("- FAIL " + path + ": " + e);
                Debug.LogException(e);
                break;
            }
            finally
            {
                if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
                if (!wasAlreadyOpen && stage.IsValid() && stage.isLoaded) EditorSceneManager.CloseScene(stage, true);
            }
        }
        report.AppendLine(success ? "- Result: PASS for serialized renderer pairing; actual camera-distance transitions need explicit visual validation." : "- Result: FAIL/PARTIAL. Do not rerun; inspect backup and scene before continuing.");
        File.WriteAllText(Output, report.ToString(), new UTF8Encoding(false));
        AssetDatabase.ImportAsset(Output);
        Debug.Log("[Stage4 LOD] Report: " + Output + "; successful=" + success);
    }

    private static Dictionary<string, MeshRenderer> UniqueNames(IEnumerable<MeshRenderer> renderers)
    {
        var list = renderers.ToArray();
        var result = list.GroupBy(r => r.gameObject.name).ToDictionary(g => g.Key, g => g.First());
        if (result.Count != list.Length)
            throw new InvalidOperationException("Duplicate renderer names prevent deterministic pairing");
        return result;
    }

    private static string GroupName(string name)
    {
        for (int i = 0; i <= 4; i++)
            if (name.StartsWith($"0{i}_", StringComparison.Ordinal) || name.StartsWith($"I{i}_", StringComparison.Ordinal))
                return "Island0" + i;
        if (name.StartsWith("Bridge_Start_Node1_")) return "Bridge01";
        if (name.StartsWith("Bridge_Node1_Node2_")) return "Bridge02";
        if (name.StartsWith("Bridge_Node2_Node3_")) return "Bridge03";
        if (name.StartsWith("Bridge_Node3_Boss_")) return "Bridge04";
        if (name.StartsWith("BACKGROUND_ISLANDS_")) return "Background";
        if (name.StartsWith("WATERFALLS_")) return "Waterfalls";
        // A small named landmark should not be lost or drawn twice: assign it
        // explicitly to a category instead of disappearing from the LOD assembly.
        if (name.StartsWith("LastGate_") || name.StartsWith("Spawn_")) return "Island00";
        if (name.StartsWith("Kingdom_")) return "Island01";
        if (name.StartsWith("Drowned_") || name.StartsWith("Sunken_")) return "Island02";
        if (name.StartsWith("Titan_")) return "Island03";
        if (name.StartsWith("Vander_") || name.StartsWith("Celestial_")) return "Island04";
        return "Misc";
    }
}
#endif
