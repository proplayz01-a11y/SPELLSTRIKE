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

internal static class PrologueV07WalkingRigAudit_20261028
{
    private const string ScenePath = "Assets/Scenes/Cinematics/Prologue_Master_Sequence_P01_42_v07_SpellbookWalkPolish.unity";
    private const string Output = "Library/PrologueV07WalkingRig_Audit_20261028.txt";

    [MenuItem("SPELLSTRIKE/Cinematics/Prologue V07/03 Audit P34-35 walking rigs %#F10")]
    private static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            EditorApplication.isUpdating || SceneManager.GetActiveScene().isDirty)
        {
            Debug.LogError("[V07 WALK AUDIT] Editor not idle or scene dirty.");
            return;
        }
        var active = SceneManager.GetActiveScene();
        Scene loaded = default;
        var report = new StringBuilder();
        try
        {
            loaded = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            var group = loaded.GetRootGameObjects().Single(g => g.name == "MASTER_SHOT_GROUPS__NUMBERED_IN_STORY_ORDER");
            var sourceCandidates = group.transform.Find("P15_P18_SHOT_ROOT").GetComponentsInChildren<Animator>(true)
                .Where(a => a.name == "VanderPoel_VisualRig_Mixamo").ToArray();
            report.AppendLine("P15 walker candidates=" + sourceCandidates.Length);
            foreach (var possible in sourceCandidates)
                report.AppendLine(" CANDIDATE " + Path(possible.transform) +
                    " avatar=" + (possible.avatar ? possible.avatar.name : "NULL") +
                    " human=" + (possible.avatar && possible.avatar.isHuman) +
                    " skins=" + possible.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length);
            var source = sourceCandidates
                .OrderByDescending(a => a.avatar && a.avatar.isValid && a.avatar.isHuman)
                .ThenByDescending(a => a.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length)
                .FirstOrDefault();
            if (source == null)
                throw new InvalidOperationException("No P15 source VisualRig_Mixamo Animator.");
            report.AppendLine("P15 WALK SOURCE " + Path(source.transform) +
                " avatar=" + (source.avatar ? source.avatar.name : "NULL") +
                " avatarValid=" + (source.avatar && source.avatar.isValid) +
                " human=" + (source.avatar && source.avatar.isHuman) +
                " bones=" + source.GetComponentsInChildren<Transform>(true).Length);
            var walk = AssetDatabase.LoadAllAssetsAtPath(
                "Assets/Cinematics/Storyboard3D/Characters/VanderPoel/Animations/VanderPoel_Walking.fbx")
                .OfType<AnimationClip>().Where(c => c.name == "mixamo.com").ToArray();
            foreach (var clip in walk)
                report.AppendLine("SOURCE WALK CLIP " + clip.name + " duration=" + clip.length +
                    " humanoid=" + clip.humanMotion + " legacy=" + clip.legacy);

            for (int p = 34; p <= 35; p++)
            {
                var shot = group.transform.Find("P" + p + "_SHOT_ROOT");
                var targetCandidates = shot.GetComponentsInChildren<Animator>(true)
                    .Where(a => a.name == "P10_VanderPoel_ANIMATED").ToArray();
                report.AppendLine("P" + p + " targetCandidates=" + targetCandidates.Length);
                foreach (var candidate in targetCandidates)
                    report.AppendLine(" CANDIDATE " + Path(candidate.transform) +
                        " avatar=" + (candidate.avatar ? candidate.avatar.name : "NULL") +
                        " skins=" + candidate.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length);
                var target = targetCandidates
                    .OrderByDescending(a => a.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length)
                    .FirstOrDefault();
                if (target == null)
                    throw new InvalidOperationException("No P" + p + " Vander Animator.");
                var renderers = target.GetComponentsInChildren<SkinnedMeshRenderer>(true).ToArray();
                report.AppendLine("P" + p + " TARGET " + Path(target.transform) +
                    " avatar=" + (target.avatar ? target.avatar.name : "NULL") +
                    " rendererCount=" + renderers.Length);
                foreach (var r in renderers)
                    report.AppendLine(" SKIN " + Path(r.transform) + " verts=" + (r.sharedMesh ? r.sharedMesh.vertexCount : -1) +
                        " bones=" + r.bones.Length + " nonnull=" + r.bones.Count(b => b) +
                        " firstBones=" + string.Join(",", r.bones.Where(b => b).Take(6).Select(b => b.name)));
                var director = shot.GetComponentsInChildren<PlayableDirector>(true)
                    .Single(d => d.playableAsset is TimelineAsset);
                var timeline = (TimelineAsset)director.playableAsset;
                report.AppendLine(" DIRECTOR " + Path(director.transform) +
                    " timeline=" + AssetDatabase.GetAssetPath(timeline));
                foreach (var track in timeline.GetOutputTracks())
                {
                    var binding = director.GetGenericBinding(track);
                    report.AppendLine(" TRACK " + track.GetType().Name + " " + track.name +
                        " bound=" + (binding is Component c ? Path(c.transform) + ":" + c.GetType().Name :
                        binding ? binding.name : "null"));
                    foreach (var clip in track.GetClips())
                    {
                        var playable = clip.asset as AnimationPlayableAsset;
                        report.AppendLine("  CLIP " + clip.displayName +
                            " " + clip.start.ToString("0.0") + "-" + (clip.start + clip.duration).ToString("0.0") +
                            " motion=" + (playable && playable.clip ? playable.clip.name : "null") +
                            " source=" + (playable && playable.clip ? AssetDatabase.GetAssetPath(playable.clip) : "null"));
                    }
                }
            }
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Output));
            File.WriteAllText(Output, report.ToString(),new UTF8Encoding(false));
            Debug.Log("[V07 WALK AUDIT] Rig/clip/Timeline evidence: " + Output);
        }
        catch (Exception ex) { Debug.LogException(ex); }
        finally
        {
            if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
            if (loaded.IsValid() && loaded.isLoaded) EditorSceneManager.CloseScene(loaded,true);
        }
    }

    private static string Path(Transform transform)
    {
        var parts = new System.Collections.Generic.Stack<string>();
        while (transform) { parts.Push(transform.name); transform = transform.parent; }
        return string.Join("/",parts);
    }
}
#endif
