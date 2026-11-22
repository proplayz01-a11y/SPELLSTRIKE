#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class PrologueV07GaitRetargetProbe_20261028
{
    private const string ScenePath = "Assets/Scenes/Cinematics/Prologue_Master_Sequence_P01_42_v07_SpellbookWalkPolish.unity";
    private const string WalkPath = "Assets/Cinematics/Storyboard3D/Characters/VanderPoel/Animations/VanderPoel_Walking.fbx";
    private const string ReportPath = "Library/PrologueV07_GaitRetargetProbe_20261028.txt";

    [MenuItem("SPELLSTRIKE/Cinematics/Prologue V07/04 Probe story-rig walk compatibility (read-only) %#F11")]
    private static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            EditorApplication.isUpdating || SceneManager.sceneCount != 1 ||
            SceneManager.GetActiveScene().isDirty)
        {
            Debug.LogError("[V07 GAIT PROBE] Editor unsafe: no scene or asset modified.");
            return;
        }
        var active = SceneManager.GetActiveScene();
        Scene loaded = default;
        Scene scratch = default;
        GameObject sample = null;
        var report = new StringBuilder();
        try
        {
            loaded = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            var group = loaded.GetRootGameObjects().Single(x => x.name == "MASTER_SHOT_GROUPS__NUMBERED_IN_STORY_ORDER");
            var p15 = group.transform.Find("P15_P18_SHOT_ROOT");
            var source = p15.GetComponentsInChildren<Animator>(true)
                .Single(a => a.name == "VanderPoel_VisualRig_Mixamo" &&
                    a.transform.parent.name == "VANDER_POEL_BLOCKING_RIG_VISUAL_ONLY");
            var walk = AssetDatabase.LoadAllAssetsAtPath(WalkPath)
                .OfType<AnimationClip>().Single(c => c.name == "mixamo.com");
            if (!source.avatar || !source.avatar.isHuman || !source.avatar.isValid || !walk.humanMotion)
                throw new InvalidOperationException("P15 source humanoid rig/clip invalid.");
            var sourceRenderer = source.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single();
            report.AppendLine("SOURCE " + source.name + " avatar=" + source.avatar.name +
                " valid=" + source.avatar.isValid + " sourceSkinBones=" + sourceRenderer.bones.Length +
                " animation=" + walk.name + " duration=" + walk.length + " humanoid=" + walk.humanMotion);
            var sourceNames = sourceRenderer.bones.Where(b => b).Select(b => b.name).ToHashSet();
            foreach (int p in new[] { 34, 35 })
            {
                var shot = group.transform.Find("P" + p.ToString("00") + "_SHOT_ROOT");
                var target = shot.GetComponentsInChildren<Animator>(true).Single(a => a.name == "P10_VanderPoel_ANIMATED");
                var skin = target.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single();
                var targetNames = skin.bones.Where(b => b).Select(b => b.name).ToHashSet();
                var common = sourceNames.Intersect(targetNames).OrderBy(n => n).ToArray();
                var missing = targetNames.Except(sourceNames).OrderBy(n => n).ToArray();
                report.AppendLine("P" + p + " mesh=" + skin.sharedMesh.name +
                    " verts=" + skin.sharedMesh.vertexCount + " storyBones=" + skin.bones.Length +
                    " matchingMixamoBones=" + common.Length + " storyExtra=" + missing.Length);
                report.AppendLine("P" + p + " extras=" + string.Join(",", missing));
                report.AppendLine("P" + p + " targetRig=" + skin.transform.parent.name +
                    " targetRootBone=" + (skin.rootBone ? skin.rootBone.name : "NULL"));
                if (common.Length < 45)
                    throw new InvalidOperationException("Too few matching bones P" + p + ": " + common.Length);
            }

            scratch = EditorSceneManager.NewPreviewScene();
            sample = UnityEngine.Object.Instantiate(source.gameObject);
            sample.name = "V07_TEMP_SOURCE_ONLY_NO_SAVE";
            SceneManager.MoveGameObjectToScene(sample, scratch);
            sample.SetActive(true);
            var sampleAnimator = sample.GetComponent<Animator>();
            sampleAnimator.enabled = true;
            var sourceSkin = sample.GetComponentInChildren<SkinnedMeshRenderer>(true);
            var first = sourceSkin.bones.Where(t => t).ToDictionary(t => t.name, t => t);
            walk.SampleAnimation(sample, 0.12f);
            var rotations = first.ToDictionary(kv => kv.Key, kv => kv.Value.localRotation);
            var positions = first.ToDictionary(kv => kv.Key, kv => kv.Value.localPosition);
            walk.SampleAnimation(sample, 0.42f);
            var differences = first.Select(kv =>
                new { Name = kv.Key, Angle = Quaternion.Angle(rotations[kv.Key], kv.Value.localRotation),
                    DeltaY = kv.Value.localPosition.y - positions[kv.Key].y })
                .OrderByDescending(x => x.Angle).ToArray();
            report.AppendLine("HUMANOID_SAMPLE t=.12 vs .42 total=" + differences.Length +
                " rotatedBonesAbove3deg=" + differences.Count(x => x.Angle > 3) +
                " rotatedBonesAbove10deg=" + differences.Count(x => x.Angle > 10));
            foreach (var item in differences.Take(25))
                report.AppendLine(" MOTION " + item.Name + " angleDeg=" + item.Angle.ToString("F2") +
                    " deltaLocalY=" + item.DeltaY.ToString("F4"));
            if (differences.Count(x => x.Angle > 3) < 8)
                throw new InvalidOperationException("Humanoid sampling did not actually animate enough bones.");
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath, report.ToString(), new UTF8Encoding(false));
            Debug.Log("[V07 GAIT PROBE] PASS. " + ReportPath);
        }
        catch (Exception ex) { Debug.LogError("[V07 GAIT PROBE] " + ex); }
        finally
        {
            if (sample) UnityEngine.Object.DestroyImmediate(sample);
            if (scratch.IsValid() && scratch.isLoaded) EditorSceneManager.ClosePreviewScene(scratch);
            if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
            if (loaded.IsValid() && loaded.isLoaded) EditorSceneManager.CloseScene(loaded, true);
        }
    }
}
#endif
