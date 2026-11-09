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
/// P34/35 visible-walk fix #2: preserve the REAL 52-bone P15 Mixamo stride,
/// but deliberately omit source motion RootT/RootQ from a sampled in-place
/// 52-bone clip. The existing 3m parent Timeline alone owns movement.
/// This avoids 5-10m compound drift that caused P35 to leave the camera.
/// </summary>
internal static class PrologueV07P3435InPlaceStrideFix_20261029
{
    private const string ScenePath = "Assets/Scenes/Cinematics/Prologue_Master_Sequence_P01_42_v07_SpellbookWalkPolish.unity";
    private const string Original = "Assets/Scenes/Cinematics/Prologue_Master_Sequence_P01_42_v06_DialogueContinuity.unity";
    private const string Backup = "RecoveryBackups/ProloguePolishV07_20261028_Immutable_v01/Prologue_Master_v06_BEFORE_POLISH.unity";
    private const string Expected = "283573BAA9EB7C9E5D3CEF9290FFDDC3DF1F026B8C8D65EAF6D84A73CB26EBF4";
    private const string Root = "Assets/Cinematics/Storyboard3D/Timelines/PrologueMaster_v07_SpellbookWalkPolish/";
    private const string Output = Root+"VanderPoel_52Bone_TrueMixamoStride_INPLACE_v07.anim";
    private const string SourceWalk = "Assets/Cinematics/Storyboard3D/Characters/VanderPoel/Animations/VanderPoel_Walking.fbx";
    private const string Report = "Library/PrologueV07_P3435_InPlaceStrideFix_20261029.txt";

    [MenuItem("SPELLSTRIKE/Cinematics/Prologue V07/09 FIX ROOT MOTION and P35 Upright Staff %#&m")]
    private static void Fix()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            EditorApplication.isUpdating || SceneManager.sceneCount != 1 ||
            SceneManager.GetActiveScene().isDirty ||
            System.Diagnostics.Process.GetProcessesByName("VALORANT-Win64-Shipping").Length != 0 ||
            Hash(Original) != Expected || Hash(Backup) != Expected ||
            AssetDatabase.AssetPathToGUID(ScenePath) != "12405e3bd39d43d398685e74678a8516" ||
            File.Exists(Output))
        {
            Debug.LogError("[V07 INPLACE] Refusing: unsafe, already built or protected original changed.");
            return;
        }
        var active=SceneManager.GetActiveScene();
        Scene scene=default;
        Scene scratch=default;
        GameObject clone=null;
        var report=new StringBuilder();
        try
        {
            scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var group=scene.GetRootGameObjects().Single(g=>
                g.name=="MASTER_SHOT_GROUPS__NUMBERED_IN_STORY_ORDER").transform;
            var source=group.Find("P15_P18_SHOT_ROOT").GetComponentsInChildren<Animator>(true)
                .Single(a=>a.name=="VanderPoel_VisualRig_Mixamo" &&
                    a.transform.parent.name=="VANDER_POEL_BLOCKING_RIG_VISUAL_ONLY");
            var walk=AssetDatabase.LoadAllAssetsAtPath(SourceWalk).OfType<AnimationClip>()
                .Single(c=>c.name=="mixamo.com");
            if(!source.avatar || !source.avatar.isHuman || !source.avatar.isValid ||
                !walk.humanMotion || walk.length<1.5f || walk.length>1.65f)
                throw new InvalidOperationException("Approved P15 52-bone source/Mixamo walk invalid.");
            // Validate both panels and their *already existing* independent
            // walker/track/path before creating anything.
            foreach(int p in new[]{34,35})
            {
                var shot=group.Find("P"+p+"_SHOT_ROOT");
                var path=shot.Find("V07_UPRIGHT_VANDER_WALK_PATH");
                var actor=path?path.Find("V07_STANDING_VANDER_REAL_WALK_VISIBLE"):null;
                if(!actor || !actor.GetComponent<Animator>().avatar.isHuman)
                    throw new InvalidOperationException("Missing corrected P"+p+" upright Humanoid.");
                var director=shot.GetComponentsInChildren<PlayableDirector>(true)
                    .Single(d=>d.playableAsset is TimelineAsset);
                var t=(TimelineAsset)director.playableAsset;
                var gait=t.GetOutputTracks().OfType<AnimationTrack>()
                    .Single(x=>x.name=="V07 FIX | upright Vander true Mixamo walk");
                var motion=(AnimationPlayableAsset)gait.GetClips().Single().asset;
                if(AssetDatabase.GetAssetPath(motion.clip)!=SourceWalk ||
                    director.GetGenericBinding(gait)!=actor.GetComponent<Animator>())
                    throw new InvalidOperationException("P"+p+" gait no longer points to original FBX.");
            }
            // Sample actual Unity Humanoid retarget into the exact 52-bone
            // mesh rig used in P34/35. Bone-local curves DO NOT translate or
            // rotate the actor root; visual foot motion remains real.
            scratch=EditorSceneManager.NewPreviewScene();
            clone=UnityEngine.Object.Instantiate(source.gameObject);
            clone.name="V07_TEMP_TRUE_MIXAMO_INPLACE_SAMPLER";
            SceneManager.MoveGameObjectToScene(clone,scratch);
            clone.SetActive(true);
            var skeleton=clone.GetComponentInChildren<SkinnedMeshRenderer>(true);
            var boneList=skeleton.bones.Where(b=>b).Distinct().ToArray();
            if(boneList.Length<45)
                throw new InvalidOperationException("Too few valid source bones for genuine 52-bone stride: "+boneList.Length);
            var asset=new AnimationClip {name="VanderPoel_52Bone_TrueMixamoStride_INPLACE_v07",frameRate=60f};
            const int Frames=95;
            foreach(var bone in boneList)
            {
                var path=AnimationUtility.CalculateTransformPath(bone,clone.transform);
                var curves=Enumerable.Range(0,4).Select(_=>new List<Keyframe>(Frames+1)).ToArray();
                for(int frame=0;frame<=Frames;frame++)
                {
                    float t=walk.length*frame/Frames;
                    walk.SampleAnimation(clone,t);
                    var q=bone.localRotation;
                    curves[0].Add(new Keyframe(t,q.x));
                    curves[1].Add(new Keyframe(t,q.y));
                    curves[2].Add(new Keyframe(t,q.z));
                    curves[3].Add(new Keyframe(t,q.w));
                }
                string[] channels={"m_LocalRotation.x","m_LocalRotation.y",
                    "m_LocalRotation.z","m_LocalRotation.w"};
                for(int c=0;c<4;c++)
                    AnimationUtility.SetEditorCurve(asset,
                        EditorCurveBinding.FloatCurve(path,typeof(Transform),channels[c]),
                        new AnimationCurve(curves[c].ToArray()));
            }
            var settings=AnimationUtility.GetAnimationClipSettings(asset);
            settings.loopTime=true;
            AnimationUtility.SetAnimationClipSettings(asset,settings);
            AssetDatabase.CreateAsset(asset,Output);
            foreach(int panel in new[]{34,35})
            {
                var shot=group.Find("P"+panel+"_SHOT_ROOT");
                var actor=shot.Find("V07_UPRIGHT_VANDER_WALK_PATH")
                    .Find("V07_STANDING_VANDER_REAL_WALK_VISIBLE");
                var rig=actor.GetComponent<Animator>();
                var director=shot.GetComponentsInChildren<PlayableDirector>(true)
                    .Single(d=>d.playableAsset is TimelineAsset);
                var timeline=(TimelineAsset)director.playableAsset;
                var gait=timeline.GetOutputTracks().OfType<AnimationTrack>()
                    .Single(t=>t.name=="V07 FIX | upright Vander true Mixamo walk");
                var motion=(AnimationPlayableAsset)gait.GetClips().Single().asset;
                motion.clip=asset;
                motion.loop=AnimationPlayableAsset.LoopMode.On;
                motion.removeStartOffset=true;
                rig.applyRootMotion=false;
                EditorUtility.SetDirty(timeline);
                report.AppendLine("P"+panel+" path authority=V07 independent 3m parent track; "+
                    "gait=52bone sampled true Mixamo in-place "+walk.length.ToString("F3")+
                    "s; no RootT/RootQ or imported root transform curves; avatar="+rig.avatar.name);
                if(panel!=35) continue;
                // Align staff on the actual working right-hand pose so the
                // crystal points UP, not across the floor in front of Van.
                var hand=actor.GetComponentsInChildren<Transform>(true)
                    .Single(t=>t.name=="mixamorig:RightHand");
                var staff=hand.GetComponentsInChildren<Transform>(true)
                    .Single(t=>t.name=="P35_V07_VANDER_CARRIES_CANONICAL_VAN_STAFF");
                var staffMesh=staff.GetComponentInChildren<Renderer>(true);
                shot.gameObject.SetActive(true);
                director.enabled=true;
                director.RebuildGraph();
                director.time=2.35;
                director.Evaluate();
                var fromHand=staffMesh.bounds.center-hand.position;
                if(fromHand.magnitude<.04f)
                    throw new InvalidOperationException("Canonical staff pivot too close to right hand.");
                staff.rotation=Quaternion.FromToRotation(fromHand.normalized,Vector3.up)*
                    staff.rotation;
                staff.position+=hand.position+Vector3.up*.40f-staffMesh.bounds.center;
                report.AppendLine("P35 canonical P38 staff upright, center ~.40m above moving right hand; "+
                    "source renderer="+staffMesh.name);
                shot.gameObject.SetActive(false);
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene,ScenePath))
                throw new IOException("In-place v07 scene failed to save.");
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Report));
            File.WriteAllText(Report,report.ToString(),new UTF8Encoding(false));
            Debug.Log("[V07 INPLACE] SAVED in-place true 52-bone gait and vertical P35 staff; now run fresh VISUAL QA. "+Report);
        }
        catch(Exception ex){Debug.LogException(ex);Debug.LogError("[V07 INPLACE] Failed; v06 never touched.");}
        finally
        {
            if(clone) UnityEngine.Object.DestroyImmediate(clone);
            if(scratch.IsValid()&&scratch.isLoaded)EditorSceneManager.ClosePreviewScene(scratch);
            if(active.IsValid()&&active.isLoaded)SceneManager.SetActiveScene(active);
            if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);
        }
    }
    private static string Hash(string f)
    {
        if(!File.Exists(f)) return "";
        using(var sha=SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(f))).Replace("-","");
    }
}
#endif
