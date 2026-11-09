#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

/// <summary>
/// One-time 52-bone Humanoid walk sampler -> real 65-bone story mesh gait.
/// Extends each panel's EXISTING head-bob clip, never adds conflicting Animators,
/// and preserves existing independent P34/P35 corridor translation tracks.
/// </summary>
internal static class PrologueV07P3435GaitBuilder_20261028
{
    private const string Original = "Assets/Scenes/Cinematics/Prologue_Master_Sequence_P01_42_v06_DialogueContinuity.unity";
    private const string OriginalHash = "283573BAA9EB7C9E5D3CEF9290FFDDC3DF1F026B8C8D65EAF6D84A73CB26EBF4";
    private const string OriginalBackup = "RecoveryBackups/ProloguePolishV07_20261028_Immutable_v01/Prologue_Master_v06_BEFORE_POLISH.unity";
    private const string ScenePath = "Assets/Scenes/Cinematics/Prologue_Master_Sequence_P01_42_v07_SpellbookWalkPolish.unity";
    private const string AssetRoot = "Assets/Cinematics/Storyboard3D/Timelines/PrologueMaster_v07_SpellbookWalkPolish/";
    private const string WalkPath = "Assets/Cinematics/Storyboard3D/Characters/VanderPoel/Animations/VanderPoel_Walking.fbx";
    private const string ReportPath = "Library/PrologueV07_P3435_RealStoryRigGait_20261028.txt";
    private static readonly string[] AnimatedBones =
    {
        "mixamorig:LeftUpLeg", "mixamorig:LeftLeg", "mixamorig:LeftFoot",
        "mixamorig:LeftToeBase", "mixamorig:RightUpLeg", "mixamorig:RightLeg",
        "mixamorig:RightFoot", "mixamorig:RightToeBase",
        "mixamorig:LeftArm", "mixamorig:LeftForeArm",
        "mixamorig:RightArm", "mixamorig:RightForeArm"
    };

    [MenuItem("SPELLSTRIKE/Cinematics/Prologue V07/05 Build P34-35 Real Story-Rig Gait %#&g")]
    private static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            EditorApplication.isUpdating || SceneManager.sceneCount != 1 ||
            SceneManager.GetActiveScene().isDirty ||
            System.Diagnostics.Process.GetProcessesByName("VALORANT-Win64-Shipping").Length != 0)
        {
            Debug.LogError("[V07 GAIT BUILD] Editor dirty/busy, scenes loaded or VALORANT running. Refusing.");
            return;
        }
        if (Hash(Original) != OriginalHash || Hash(OriginalBackup) != OriginalHash ||
            AssetDatabase.AssetPathToGUID(Original) != "143e0b853ca11b84190743d04aedaefe" ||
            AssetDatabase.AssetPathToGUID(ScenePath) != "12405e3bd39d43d398685e74678a8516")
        {
            Debug.LogError("[V07 GAIT BUILD] Original/backup SHA or versioned scene GUID mismatch. Refusing.");
            return;
        }
        foreach (int panel in new[] { 34, 35 })
        {
            if (File.Exists(OutputPath(panel)) || File.Exists(OutputPath(panel) + ".meta"))
            {
                Debug.LogError("[V07 GAIT BUILD] Already exists: " + OutputPath(panel) + ". Never overwrite.");
                return;
            }
        }
        var prior = SceneManager.GetActiveScene();
        Scene sourceScene = default;
        Scene scratch = default;
        GameObject sample = null;
        var report = new StringBuilder();
        try
        {
            sourceScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            var group = sourceScene.GetRootGameObjects()
                .Single(g => g.name == "MASTER_SHOT_GROUPS__NUMBERED_IN_STORY_ORDER");
            var walker = group.transform.Find("P15_P18_SHOT_ROOT")
                .GetComponentsInChildren<Animator>(true)
                .Single(a => a.name == "VanderPoel_VisualRig_Mixamo" &&
                    a.transform.parent.name == "VANDER_POEL_BLOCKING_RIG_VISUAL_ONLY");
            var clip = AssetDatabase.LoadAllAssetsAtPath(WalkPath)
                .OfType<AnimationClip>().Single(a => a.name == "mixamo.com");
            if (!walker.avatar || !walker.avatar.isValid || !walker.avatar.isHuman ||
                !clip.humanMotion || clip.length < 1.45f || clip.length > 1.7f)
                throw new InvalidOperationException("Source actor/real Mixamo Humanoid walk invalid.");

            var destinations = new List<Panel>();
            foreach (int number in new[] { 34, 35 })
            {
                var shot = group.transform.Find("P" + number + "_SHOT_ROOT");
                var director = shot.GetComponentsInChildren<PlayableDirector>(true)
                    .Single(d => d.playableAsset is TimelineAsset);
                var timeline = director.playableAsset as TimelineAsset;
                if (AssetDatabase.GetAssetPath(timeline) !=
                    AssetRoot + (number == 34 ? "P34_P34_DialogueAggressive_v05_v07.playable" :
                        "P35_P35_VanderReturnsWithStaff_8s_v01_v07.playable"))
                    throw new InvalidOperationException("P" + number + " is not independent v07 Timeline.");
                var actor = shot.GetComponentsInChildren<Animator>(true)
                    .Single(a => a.name == "P10_VanderPoel_ANIMATED");
                var skin = actor.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single();
                if (skin.sharedMesh.vertexCount != 19225 || skin.bones.Length != 65 ||
                    skin.bones.Any(b => !b))
                    throw new InvalidOperationException("P" + number + " canonical 65-bone story skin invalid.");
                var track = timeline.GetOutputTracks().OfType<AnimationTrack>()
                    .Single(t => t.name.StartsWith("VanderPoel | Subtle head bob", StringComparison.Ordinal));
                if (director.GetGenericBinding(track) != actor)
                    throw new InvalidOperationException("P" + number + " head-bob animator binding mismatch.");
                var motion = track.GetClips().Single();
                var headbob = (motion.asset as AnimationPlayableAsset);
                if (headbob == null || !headbob.clip ||
                    AssetDatabase.GetAssetPath(headbob.clip).StartsWith(AssetRoot, StringComparison.Ordinal))
                    throw new InvalidOperationException("P" + number + " original headbob source unexpectedly versioned.");
                // The existing separate track must keep translation on the
                // actor's seated/standing root, not our bone curves.
                var motionTrack = timeline.GetOutputTracks().OfType<AnimationTrack>()
                    .Single(t => t.name == "P" + number + "_GatewayMovement");
                if (director.GetGenericBinding(motionTrack) != actor ||
                    motionTrack.GetClips().Count() != 1 ||
                    !(motionTrack.GetClips().Single().asset is AnimationPlayableAsset rootMotion) ||
                    rootMotion.clip == null ||
                    !AssetDatabase.GetAssetPath(rootMotion.clip).StartsWith(AssetRoot, StringComparison.Ordinal))
                    throw new InvalidOperationException("P" + number + " existing versioned corridor path is missing.");
                var rootBindings = AnimationUtility.GetCurveBindings(rootMotion.clip);
                if (rootBindings.Length != 1 || rootBindings[0].propertyName != "m_LocalPosition.z")
                    throw new InvalidOperationException("P" + number + " corridor motion unexpectedly animates bone data.");
                var boneMap = skin.bones.ToDictionary(b => b.name, b => b);
                foreach (string bone in AnimatedBones)
                    if (!boneMap.ContainsKey(bone))
                        throw new InvalidOperationException("P" + number + " missing story leg/arm bone: " + bone);
                destinations.Add(new Panel
                {
                    Number = number, Actor = actor, Skin = skin, Timeline = timeline,
                    Track = track, Headbob = headbob, SourceClip = headbob.clip,
                    BoneMap = boneMap
                });
            }

            // Sampling does not mutate any source shot or asset.
            scratch = EditorSceneManager.NewPreviewScene();
            sample = UnityEngine.Object.Instantiate(walker.gameObject);
            sample.name = "TEMP_V07_HUMANOID_WALK_SAMPLE_NOT_SAVED";
            SceneManager.MoveGameObjectToScene(sample, scratch);
            sample.SetActive(true);
            var avatar = sample.GetComponent<Animator>();
            avatar.enabled = true;
            if (!avatar.avatar || !avatar.avatar.isValid)
                throw new InvalidOperationException("Temporary 52-bone sample avatar invalid.");
            var sourceSkinned = sample.GetComponentInChildren<SkinnedMeshRenderer>(true);
            var sourceBones = sourceSkinned.bones.ToDictionary(b => b.name, b => b);
            foreach (string name in AnimatedBones)
                if (!sourceBones.ContainsKey(name))
                    throw new InvalidOperationException("Source Mixamo gait missing bone " + name);
            const int samples = 96;
            var bank = new Dictionary<string, Quaternion[]>();
            foreach (string name in AnimatedBones) bank[name] = new Quaternion[samples];
            for (int frame = 0; frame < samples; frame++)
            {
                clip.SampleAnimation(sample, clip.length * frame / samples);
                foreach (string name in AnimatedBones)
                    bank[name][frame] = sourceBones[name].localRotation;
            }
            int varied = AnimatedBones.Count(name => Quaternion.Angle(bank[name][0], bank[name][samples / 4]) > 3f);
            if (varied < 8)
                throw new InvalidOperationException("The sampled Mixamo walk contains insufficient leg/arm motion.");
            report.AppendLine("SOURCE real Mixamo clip=" + WalkPath + " duration=" + clip.length +
                " cyclesPer7s=" + (7f/clip.length).ToString("F3") +
                " sampledFrames=" + samples + " variedBones=" + varied);

            // PREPARED all scene assets/paths above. From this point on, save
            // only isolated v07 copies, never the original headbob clips.
            foreach (var destination in destinations)
            {
                string dst = OutputPath(destination.Number);
                string original = AssetDatabase.GetAssetPath(destination.SourceClip);
                if (!AssetDatabase.CopyAsset(original, dst))
                    throw new IOException("Could not create unique v07 gait headbob " + dst);
                var combined = AssetDatabase.LoadAssetAtPath<AnimationClip>(dst);
                if (combined == null)
                    throw new IOException("Imported v07 gait headbob clip missing " + dst);
                int rotations = 0;
                const int frames = 192;
                float start = destination.Number == 34 ? 1f : 0f;
                float end = destination.Number == 34 ? 8f : 7f;
                foreach (string name in AnimatedBones)
                {
                    string bonePath = AnimationUtility.CalculateTransformPath(
                        destination.BoneMap[name],destination.Actor.transform);
                    var bound = new EditorCurveBinding[4];
                    var curves = new AnimationCurve[4];
                    string[] channelNames =
                    {
                        "m_LocalRotation.x","m_LocalRotation.y",
                        "m_LocalRotation.z","m_LocalRotation.w"
                    };
                    for (int i=0;i<4;i++)
                    {
                        bound[i] = EditorCurveBinding.FloatCurve(bonePath,typeof(Transform),channelNames[i]);
                        curves[i] = AnimationUtility.GetEditorCurve(combined,bound[i]);
                        if (curves[i] == null)
                            throw new InvalidOperationException("Missing baseline P" +
                                destination.Number + " " + name + "." + channelNames[i]);
                    }
                    var keys = Enumerable.Range(0,4).Select(_ => new List<Keyframe>()).ToArray();
                    for (int frame=0;frame<=frames;frame++)
                    {
                        float t = 8f*frame/frames;
                        var baseQ = new Quaternion(curves[0].Evaluate(t),curves[1].Evaluate(t),
                            curves[2].Evaluate(t),curves[3].Evaluate(t)).normalized;
                        float move = t<start || t>end ? 0f :
                            Mathf.SmoothStep(0,1,Mathf.Clamp01((t-start)/.38f)) *
                            Mathf.SmoothStep(0,1,Mathf.Clamp01((end-t)/.42f));
                        float phase = Mathf.Repeat(Mathf.Max(0f,t-start)/clip.length,1f);
                        int index = Mathf.Clamp(Mathf.RoundToInt(phase*samples),0,samples-1);
                        Quaternion rawDelta = Quaternion.Inverse(bank[name][0])*bank[name][index];
                        bool arm = name.Contains("Arm");
                        float amp = arm ? .62f : name.Contains("Toe") ? .42f : .90f;
                        var delta = Quaternion.Slerp(Quaternion.identity,rawDelta,move*amp);
                        var composed = (baseQ*delta).normalized;
                        for(int axis=0;axis<4;axis++)
                        {
                            float val = axis==0?composed.x:axis==1?composed.y:axis==2?composed.z:composed.w;
                            keys[axis].Add(new Keyframe(t,val));
                        }
                    }
                    for (int axis=0;axis<4;axis++)
                        AnimationUtility.SetEditorCurve(combined,bound[axis],new AnimationCurve(keys[axis].ToArray()));
                    rotations++;
                }
                EditorUtility.SetDirty(combined);
                destination.Headbob.clip = combined;
                EditorUtility.SetDirty(destination.Timeline);
                report.AppendLine("P" + destination.Number + " NEW gait=" + dst +
                    " 12 leg/arm bone paths x4 channels x193 keys; existing headbob curves maintained on all other bones; " +
                    "existing P" + destination.Number + "_GatewayMovement unchanged and independently bound.");
                Debug.Log("[V07 GAIT BUILD] P" + destination.Number +
                    " real 52->65 bone walk baked, " + rotations + " active leg/arm bones; clip=" + dst);
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(sourceScene);
            if (!EditorSceneManager.SaveScene(sourceScene, ScenePath))
                throw new IOException("Failed to persist v07 scene.");
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath,report.ToString(),new UTF8Encoding(false));
            Debug.Log("[V07 GAIT BUILD] SAVED v07 gait + Timeline references. v06 untouched. " + ReportPath);
        }
        catch (Exception error)
        {
            Debug.LogError("[V07 GAIT BUILD] INCOMPLETE, no runtime route change: " + error);
        }
        finally
        {
            if (sample) UnityEngine.Object.DestroyImmediate(sample);
            if (scratch.IsValid() && scratch.isLoaded) EditorSceneManager.ClosePreviewScene(scratch);
            if (prior.IsValid() && prior.isLoaded) SceneManager.SetActiveScene(prior);
            if (sourceScene.IsValid() && sourceScene.isLoaded)
                EditorSceneManager.CloseScene(sourceScene, true);
        }
    }
    private sealed class Panel
    {
        internal int Number;
        internal Animator Actor;
        internal SkinnedMeshRenderer Skin;
        internal TimelineAsset Timeline;
        internal AnimationTrack Track;
        internal AnimationPlayableAsset Headbob;
        internal AnimationClip SourceClip;
        internal Dictionary<string,Transform> BoneMap;
    }
    private static string OutputPath(int p) =>
        AssetRoot + "P" + p + "_Vander_Story65Bone_RealMixamoGait_v07.anim";
    private static string Hash(string path)
    {
        if (!File.Exists(path)) return "";
        using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","");
    }
}
#endif
