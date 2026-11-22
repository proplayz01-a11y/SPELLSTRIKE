#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using TMPro;

/// <summary>Read-only asset/Timeline audit for v07 authoring; never saves any scene or asset.</summary>
internal static class PrologueV07Audit_20261028
{
    private const string Source = "Assets/Scenes/Cinematics/Prologue_Master_Sequence_P01_42_v06_DialogueContinuity.unity";
    private const string Output = "Library/PrologueV07_Audit_20261028.txt";
    private static readonly int[] Panels = {15,16,17,18,19,20,21,25,26,29,30,34,35};

    [MenuItem("SPELLSTRIKE/Cinematics/Prologue V07/01 Audit v06 (read-only)")]
    private static void Audit()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            EditorApplication.isUpdating || SceneManager.GetActiveScene().isDirty)
        {
            Debug.LogError("[V07 AUDIT] Scene must be saved and Editor in idle Edit Mode.");
            return;
        }
        var previous = SceneManager.GetActiveScene();
        Scene loaded = default;
        var report = new StringBuilder();
        try
        {
            loaded = EditorSceneManager.OpenScene(Source, OpenSceneMode.Additive);
            var roots = loaded.GetRootGameObjects();
            var transforms = roots.SelectMany(x => x.GetComponentsInChildren<Transform>(true)).ToArray();
            var directors = roots.SelectMany(x => x.GetComponentsInChildren<PlayableDirector>(true)).ToArray();
            report.AppendLine("SCENE " + loaded.path + " roots=" + roots.Length +
                " transforms=" + transforms.Length + " directors=" + directors.Length);
            foreach (var d in directors.OrderBy(x => x.name))
            {
                var t = d.playableAsset as TimelineAsset;
                if (t == null) continue;
                report.AppendLine("DIRECTOR " + HierarchyPath(d.transform) + " timeline=" +
                    AssetDatabase.GetAssetPath(t) + " duration=" + t.duration.ToString("0.00") +
                    " bindings=" + t.GetOutputTracks().Count());
                foreach (var track in t.GetOutputTracks())
                {
                    var bound = d.GetGenericBinding(track);
                    var component = bound as Component;
                    var binding = component ? HierarchyPath(component.transform) + "(" + component.GetType().Name + ")" :
                        bound ? bound.name : "NULL";
                    string directorPath = HierarchyPath(d.transform);
                    bool target = Panels.Any(p => directorPath.Contains("/P" + p.ToString("00") + "_SHOT_ROOT/")) ||
                        directorPath.Contains("/P15_P18_SHOT_ROOT/") ||
                        t.name.Contains("P15_P18") ||
                        d.name.StartsWith("PROLOGUE_MASTER_TIMELINE_DIRECTOR", StringComparison.Ordinal);
                    if (!target) continue;
                    report.AppendLine(" TRACK " + track.GetType().Name + " " + track.name + " bound=" + binding);
                    foreach (var c in track.GetClips())
                    {
                        var a = c.asset as AnimationPlayableAsset;
                        var control = c.asset as ControlPlayableAsset;
                        report.AppendLine("  CLIP " + c.displayName + " start=" + c.start.ToString("0.00") +
                            " duration=" + c.duration.ToString("0.00") +
                            " anim=" + (a != null && a.clip ? a.clip.name : "none") +
                            " source=" + (a != null && a.clip ? AssetDatabase.GetAssetPath(a.clip) : "none") +
                            " loop=" + (a != null ? a.loop.ToString() : "na") +
                            " control=" + (control != null ? control.sourceGameObject.exposedName.ToString() : "none"));
                    }
                }
            }
            foreach (int panel in Panels)
            {
                string needle = "P" + panel.ToString("00");
                var shots = transforms.Where(t =>
                    t.name.StartsWith(needle + "_SHOT_ROOT", StringComparison.OrdinalIgnoreCase) ||
                    t.name.StartsWith(needle + "_STATIC_CAMERA", StringComparison.OrdinalIgnoreCase) ||
                    t.name.StartsWith(needle + "_CORRIDOR", StringComparison.OrdinalIgnoreCase) ||
                    t.name.StartsWith(needle + "_CAPTION", StringComparison.OrdinalIgnoreCase) ||
                    t.name.StartsWith(needle + "_DIALOGUE", StringComparison.OrdinalIgnoreCase))
                    .Take(30).ToArray();
                report.AppendLine("PANEL " + panel + " relevantObjects=" + shots.Length);
                foreach (var obj in shots)
                {
                    report.AppendLine(" OBJECT " + HierarchyPath(obj) + " position=" + obj.position.ToString("F2") +
                        " active=" + obj.gameObject.activeInHierarchy + " directChildren=" + obj.childCount);
                    if (obj.name.EndsWith("_SHOT_ROOT", StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var sub in obj.GetComponentsInChildren<PlayableDirector>(true))
                            report.AppendLine("  NESTED_DIRECTOR " + HierarchyPath(sub.transform) +
                                " playable=" + AssetDatabase.GetAssetPath(sub.playableAsset));
                        foreach (var a in obj.GetComponentsInChildren<Animator>(true).Take(12))
                            report.AppendLine("  ACTOR " + HierarchyPath(a.transform) + " avatar=" +
                                (a.avatar ? a.avatar.name + " human=" + a.avatar.isHuman : "NULL"));
                        foreach (var mesh in obj.GetComponentsInChildren<Renderer>(true)
                            .Where(r => r.name.IndexOf("book", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                r.name.IndexOf("page", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                r.name.IndexOf("letter", StringComparison.OrdinalIgnoreCase) >= 0)
                            .Take(25))
                            report.AppendLine("  BOOK_RENDER " + HierarchyPath(mesh.transform) + " mats=" +
                                string.Join(",", mesh.sharedMaterials.Select(m => m ? m.name : "null")));
                        foreach (var text in obj.GetComponentsInChildren<TMP_Text>(true).Take(20))
                            report.AppendLine("  TEXT " + HierarchyPath(text.transform) +
                                " value=" + text.text.Replace("\n", "[NL]"));
                    }
                }
            }
            report.AppendLine("BUILD_SCENES " + string.Join(";", EditorBuildSettings.scenes.Select((s,i) =>
                i + ":" + s.path + ":" + s.enabled).Where(s => s.Contains("Prologue_Master"))));
            Directory.CreateDirectory(Path.GetDirectoryName(Output));
            File.WriteAllText(Output, report.ToString(), new UTF8Encoding(false));
            Debug.Log("[V07 AUDIT] Read-only audit saved: " + Output + ", bytes=" + report.Length);
        }
        catch (Exception ex) { Debug.LogException(ex); }
        finally
        {
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            if (loaded.IsValid() && loaded.isLoaded) EditorSceneManager.CloseScene(loaded, true);
        }
    }

    private static string HierarchyPath(Transform t)
    {
        var names = new System.Collections.Generic.Stack<string>();
        while (t) { names.Push(t.name); t = t.parent; }
        return string.Join("/", names);
    }
}
#endif
