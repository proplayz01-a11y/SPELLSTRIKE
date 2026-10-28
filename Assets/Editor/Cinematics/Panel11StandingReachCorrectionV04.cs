#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

// Versioned, conservative repair: imported rig standing rest instead of Panel01 seated animation.
// Shoulder poses static; only head performs small nod. Does not touch Stage3Scene or source assets.
[InitializeOnLoad]
internal static class Panel11StandingReachCorrectionV04
{
    const string Key="SPELLSTRIKE_PANEL11_STANDING_REACH_V04_BUILD_ONCE";
    const string SourceScene="Assets/Scenes/Cinematics/Prologue_P11_NevaliDream_v03.unity";
    const string DestScene="Assets/Scenes/Cinematics/Prologue_P11_NevaliDream_Standing_v04.unity";
    const string SourceTL="Assets/Cinematics/Storyboard3D/Timelines/Panel11_v03/P11_NevaliDream_8s_v03.playable";
    const string Folder="Assets/Cinematics/Storyboard3D/Timelines/Panel11_v04";
    const string DestTL=Folder+"/P11_NevaliDreamStanding_8s_v04.playable";
    const string DestAnim=Folder+"/P11_Nevali_StandingReachHeadBob_v04.anim";
    const string Log="Library/Panel07_10MotionAudit/Panel11_StandingCorrection_v04.txt";
    static Panel11StandingReachCorrectionV04(){EditorApplication.delayCall+=Run;}
    [MenuItem("SPELLSTRIKE/Cinematics/Panel 11/Build Standing Nevali v04")]
    static void Run(){
        if(SessionState.GetBool(Key,false)||EditorApplication.isPlayingOrWillChangePlaymode)return;
        Scene before=SceneManager.GetActiveScene();
        if(!before.IsValid()||!before.isLoaded||before.isDirty||string.IsNullOrWhiteSpace(before.path)){Debug.LogWarning("[P11 Stand] Active scene must be clean/saved");return;}
        SessionState.SetBool(Key,true);
        var existing=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).path).ToArray();
        var audit=new List<string>{"ACTIVE_START="+before.path+" dirty="+before.isDirty};
        Scene draft=default(Scene);
        try{
            if(File.Exists(DestScene)||File.Exists(DestTL)||File.Exists(DestAnim))throw new IOException("Refusing overwrite: v04 already exists");
            if(!AssetDatabase.CopyAsset(SourceScene,DestScene))throw new IOException("Could not copy source P11 scene");
            if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/Cinematics/Storyboard3D/Timelines","Panel11_v04");
            if(!AssetDatabase.CopyAsset(SourceTL,DestTL))throw new IOException("Could not copy P11 Timeline");
            var tl=AssetDatabase.LoadAssetAtPath<TimelineAsset>(DestTL);
            if(!tl)throw new IOException("New P11 Timeline asset not found");
            draft=EditorSceneManager.OpenScene(DestScene,OpenSceneMode.Additive);
            if(!draft.IsValid()||!draft.isLoaded||SceneManager.GetActiveScene().path!=before.path)throw new InvalidOperationException("Unsafe active scene or additive load failure");
            var roots=draft.GetRootGameObjects();
            var nevali=roots.SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="P11_Nevali_v13_DreamVision_ANIMATED");
            var animator=nevali.GetComponent<Animator>();if(!animator)throw new IOException("Nevali rig animator unavailable");
            var head=nevali.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="mixamorig:Head");
            var left=nevali.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="mixamorig:LeftArm");
            var right=nevali.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="mixamorig:RightArm");
            var rightForearm=nevali.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="mixamorig:RightForeArm");
            audit.Add("REST neutralHead="+head.localRotation.eulerAngles+" leftArm="+left.localRotation.eulerAngles+" rightArm="+right.localRotation.eulerAngles+" rightForearm="+rightForearm.localRotation.eulerAngles);
            // Only author 4x4 pose curves. Leave hips/spine/legs at imported standing rest, unlike seated Panel01 clip.
            var clip=new AnimationClip{name="P11_Nevali_StandingReach_GentleHeadBob_v04",frameRate=24};
            SetPose(clip,nevali,head,new float[]{0,.5f,1f,.25f,0},new float[]{0,2,4,6,8},new Vector3(1,0,0),audit);
            SetPose(clip,nevali,left,new float[]{-65,-65,-65,-65,-65},new float[]{0,2,4,6,8},new Vector3(0,0,1),audit);
            SetPose(clip,nevali,right,new float[]{22,22,22,22,22},new float[]{0,2,4,6,8},new Vector3(0,0,1),audit);
            SetPose(clip,nevali,rightForearm,new float[]{18,18,18,18,18},new float[]{0,2,4,6,8},new Vector3(0,1,0),audit);
            clip.EnsureQuaternionContinuity();AssetDatabase.CreateAsset(clip,DestAnim);
            var director=roots.SelectMany(g=>g.GetComponentsInChildren<PlayableDirector>(true)).Single();
            director.playableAsset=tl;director.playOnAwake=true;
            foreach(var track in tl.GetOutputTracks().OfType<AnimationTrack>()){
                var tclip=track.GetClips().Single();
                var playable=tclip.asset as AnimationPlayableAsset;
                if(playable==null)throw new IOException("Non-animation playable");
                if(track.name.StartsWith("Nevali",StringComparison.Ordinal)){
                    playable.clip=clip;EditorUtility.SetDirty(playable);director.SetGenericBinding(track,animator);
                }else if(track.name.StartsWith("OlderVan",StringComparison.Ordinal)){
                    var older=roots.SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="P10_OlderVan_ANIMATED");
                    var oldAnimator=older.GetComponentInChildren<Animator>(true);
                    if(!oldAnimator)throw new IOException("Older Van has no Animator");
                    director.SetGenericBinding(track,oldAnimator);
                }
            }
            if(!EditorSceneManager.SaveScene(draft,DestScene))throw new IOException("Could not save v04 scene");
            EditorUtility.SetDirty(tl);AssetDatabase.SaveAssets();
            audit.Add("SAVED scene="+DestScene+" timeline="+DestTL+" standingClip="+DestAnim+" duration="+director.duration);
        }catch(Exception error){audit.Add("ERROR="+error);Debug.LogError("[P11 Standing V04] "+error);}
        finally{if(draft.IsValid()&&draft.isLoaded)EditorSceneManager.CloseScene(draft,true);}
        audit.Add("ACTIVE_END="+SceneManager.GetActiveScene().path+" dirty="+SceneManager.GetActiveScene().isDirty);
        audit.Add("SCENE_LIST_UNCHANGED="+existing.SequenceEqual(Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).path)));
        Directory.CreateDirectory(Path.GetDirectoryName(Log));File.WriteAllLines(Log,audit);
        Debug.Log("[P11 Standing V04] " + Log);
    }
    static void SetPose(AnimationClip clip,Transform characterRoot,Transform target,float[] angles,float[] seconds,Vector3 axis,List<string> logs){
        string path=AnimationUtility.CalculateTransformPath(target,characterRoot);
        Quaternion basis=target.localRotation;
        var q=angles.Select(a=>basis*Quaternion.AngleAxis(a,axis.normalized)).ToArray();
        var prop=new[]{"m_LocalRotation.x","m_LocalRotation.y","m_LocalRotation.z","m_LocalRotation.w"};
        for(int i=0;i<4;i++){
            var frames=new Keyframe[seconds.Length];
            for(int t=0;t<seconds.Length;t++){
                Quaternion r=q[t];float v=i==0?r.x:i==1?r.y:i==2?r.z:r.w;
                frames[t]=new Keyframe(seconds[t],v);
            }
            AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(path,typeof(Transform),prop[i]),new AnimationCurve(frames));
        }
        logs.Add("POSE="+path+" startEuler="+basis.eulerAngles+" endpointAngles="+string.Join(",",angles));
    }
}
#endif

