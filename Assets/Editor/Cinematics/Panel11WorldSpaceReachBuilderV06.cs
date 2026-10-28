#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

// Versioned pose repair: solve shoulder arm vectors in world space, not arbitrary local Euler axes.
internal static class Panel11WorldSpaceReachBuilderV06
{
 const string Once="SPELLSTRIKE_P11_WORLD_DIRECTION_REACH_V06_ONCE";
 const string From="Assets/Scenes/Cinematics/Prologue_P11_NevaliDream_Standing_v05.unity";
 const string To="Assets/Scenes/Cinematics/Prologue_P11_NevaliDream_StandingReach_v06.unity";
 const string Folder="Assets/Cinematics/Storyboard3D/Timelines/Panel11_v06";
 const string TimelineOld="Assets/Cinematics/Storyboard3D/Timelines/Panel11_v05/P11_NevaliDreamStanding_8s_v05.playable";
 const string TimelineNew=Folder+"/P11_NevaliDreamStandingReach_8s_v06.playable";
 const string ClipNew=Folder+"/P11_Nevali_StandingReach_HeadBob_v06.anim";
 const string Report="Library/Panel07_10MotionAudit/Panel11_WorldSpaceReachBuilderV06.txt";
 static Panel11WorldSpaceReachBuilderV06(){EditorApplication.delayCall+=Build;}
 [MenuItem("SPELLSTRIKE/Cinematics/Panel 11/Build World-Space Reach Pose v06")]
 static void Build(){
  if(SessionState.GetBool(Once,false)||EditorApplication.isPlayingOrWillChangePlaymode)return;
  Scene old=SceneManager.GetActiveScene();
  if(!old.IsValid()||!old.isLoaded||old.isDirty||string.IsNullOrWhiteSpace(old.path))return;
  SessionState.SetBool(Once,true);
  var oldPaths=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).path).ToArray();
  var log=new List<string>{"ACTIVE_BEFORE="+old.path+" dirty="+old.isDirty};
  Scene trial=default(Scene);
  try{
   if(File.Exists(To)||File.Exists(TimelineNew)||File.Exists(ClipNew))throw new IOException("Refusing overwrite of v06 assets");
   if(!AssetDatabase.CopyAsset(From,To))throw new IOException("Could not version P11 v05 scene");
   if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/Cinematics/Storyboard3D/Timelines","Panel11_v06");
   if(!AssetDatabase.CopyAsset(TimelineOld,TimelineNew))throw new IOException("Could not version P11 v05 timeline");
   var timeline=AssetDatabase.LoadAssetAtPath<TimelineAsset>(TimelineNew);if(!timeline)throw new IOException("new Timeline missing");
   trial=EditorSceneManager.OpenScene(To,OpenSceneMode.Additive);
   if(!trial.IsValid()||!trial.isLoaded||SceneManager.GetActiveScene().path!=old.path)throw new IOException("Additive safety failure");
   var all=trial.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
   var model=all.Single(t=>t.name=="P11_Nevali_V13_STANDING_DREAM_ANIMATED");
   var animator=model.GetComponentInChildren<Animator>(true);
   if(!animator)throw new IOException("Nevali Animator missing");
   var rig=all.Single(t=>t.name=="P10_OlderVan_ANIMATED");
   var oldAnimator=rig.GetComponentInChildren<Animator>(true);if(!oldAnimator)throw new IOException("OlderVan Animator missing");
   Transform head=Bone(model,"mixamorig:Head");
   Transform armL=Bone(model,"mixamorig:LeftArm"),elbowL=Bone(model,"mixamorig:LeftForeArm");
   Transform armR=Bone(model,"mixamorig:RightArm"),elbowR=Bone(model,"mixamorig:RightForeArm");
   Vector3 dirL=(elbowL.position-armL.position).normalized,dirR=(elbowR.position-armR.position).normalized;
   log.Add("IMPORT_REST leftForearm="+dirL+" rightForearm="+dirR+" leftArmWorld="+armL.position+" rightArmWorld="+armR.position);
   // Whichever shoulder naturally faces Van's world-positive X side becomes the reaching arm.
   bool leftReaches=dirL.x>=dirR.x;
   Transform reaching=leftReaches?armL:armR, reachingElbow=leftReaches?elbowL:elbowR;
   Transform relaxed=leftReaches?armR:armL, relaxedElbow=leftReaches?elbowR:elbowL;
   Vector3 towardVan=new Vector3(.94f,.27f,.05f).normalized;
   Vector3 hang=new Vector3(-.14f,-.98f,.09f).normalized;
   var reachQ=AlignUpperArm(reaching,reachingElbow,towardVan);
   var relaxQ=AlignUpperArm(relaxed,relaxedElbow,hang);
   var clip=new AnimationClip{name="P11_Nevali_StandingReach_HeadBob_v06",frameRate=24};
   SetConstantQuaternion(clip,model,reaching,reachQ,log);
   SetConstantQuaternion(clip,model,relaxed,relaxQ,log);
   SetHeadNod(clip,model,head,log);
   clip.EnsureQuaternionContinuity();AssetDatabase.CreateAsset(clip,ClipNew);
   log.Add("REACHING_ARM="+reaching.name+" targetWorld="+towardVan+" relaxedArm="+relaxed.name+" targetWorld="+hang);
   var director=trial.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PlayableDirector>(true)).Single();
   director.playableAsset=timeline;
   foreach(var track in timeline.GetOutputTracks().OfType<AnimationTrack>()){
      var asset=track.GetClips().Single().asset as AnimationPlayableAsset;
      if(!asset)throw new IOException("Unrecognized track asset: "+track.name);
      if(track.name.StartsWith("Nevali",StringComparison.Ordinal)){asset.clip=clip;EditorUtility.SetDirty(asset);director.SetGenericBinding(track,animator);}
      else if(track.name.StartsWith("OlderVan",StringComparison.Ordinal))director.SetGenericBinding(track,oldAnimator);
   }
   if(!EditorSceneManager.SaveScene(trial,To))throw new IOException("Couldn't save v06 scene");
   EditorUtility.SetDirty(timeline);AssetDatabase.SaveAssets();
   log.Add("SAVED="+To+" TIMELINE="+TimelineNew+" CLIP="+ClipNew+" duration="+director.duration);
  }catch(Exception error){log.Add("ERROR="+error);Debug.LogError("[P11 Reach V06] "+error);}
  finally{if(trial.IsValid()&&trial.isLoaded)EditorSceneManager.CloseScene(trial,true);}
  log.Add("ACTIVE_AFTER="+SceneManager.GetActiveScene().path+" dirty="+SceneManager.GetActiveScene().isDirty);
  log.Add("LOADED_SCENES_UNCHANGED="+oldPaths.SequenceEqual(Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).path)));
  Directory.CreateDirectory(Path.GetDirectoryName(Report));File.WriteAllLines(Report,log);
  Debug.Log("[P11 Reach V06] "+Report);
 }
 static Transform Bone(Transform root,string name){var b=root.GetComponentsInChildren<Transform>(true).SingleOrDefault(t=>t.name==name);if(!b)throw new IOException("Missing bone "+name);return b;}
 static Quaternion AlignUpperArm(Transform upper,Transform elbow,Vector3 desired){
    var current=(elbow.position-upper.position).normalized;
    if(current.sqrMagnitude<.01f)throw new IOException("Invalid arm direction "+upper.name);
    Quaternion delta=Quaternion.FromToRotation(current,desired.normalized);
    return Quaternion.Inverse(upper.parent.rotation)*(delta*upper.rotation);
 }
 static void SetConstantQuaternion(AnimationClip clip,Transform root,Transform bone,Quaternion q,List<string> log){
  string path=AnimationUtility.CalculateTransformPath(bone,root);
  string[] names={"m_LocalRotation.x","m_LocalRotation.y","m_LocalRotation.z","m_LocalRotation.w"};
  float[] values={q.x,q.y,q.z,q.w};
  for(int i=0;i<4;i++)AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(path,typeof(Transform),names[i]),new AnimationCurve(new Keyframe(0,values[i]),new Keyframe(8,values[i])));
  log.Add("ARM_POSE "+path+" euler="+q.eulerAngles);
 }
 static void SetHeadNod(AnimationClip clip,Transform root,Transform head,List<string> log){
  string path=AnimationUtility.CalculateTransformPath(head,root);
  string[] names={"m_LocalRotation.x","m_LocalRotation.y","m_LocalRotation.z","m_LocalRotation.w"};
  float[] times={0,2,4,6,8},angles={0,.45f,1,.3f,0};
  var rest=head.localRotation;var rotations=angles.Select(n=>rest*Quaternion.Euler(n,0,0)).ToArray();
  for(int i=0;i<4;i++){var keys=new Keyframe[times.Length];for(int j=0;j<times.Length;j++){var q=rotations[j];keys[j]=new Keyframe(times[j],i==0?q.x:i==1?q.y:i==2?q.z:q.w);}AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(path,typeof(Transform),names[i]),new AnimationCurve(keys));}
  log.Add("HEAD_POSE="+path);
 }
}
#endif

