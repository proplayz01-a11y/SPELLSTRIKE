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

// Panel 11 production test: replace seated Panel01 rig with authentic Nevali v13 standing Mixamo T-pose rig.
// Preserve P11 v03/v04 and Stage3. Only v05 scene and v05 Timeline/clip are written.
internal static class Panel11TposeRigBuilderV05
{
 const string Key="SPELLSTRIKE_P11_AUTHENTIC_TPOSE_RIG_V05_ONCE";
 const string Input="Assets/Scenes/Cinematics/Prologue_P11_NevaliDream_Standing_v04.unity";
 const string Output="Assets/Scenes/Cinematics/Prologue_P11_NevaliDream_Standing_v05.unity";
 const string TLIn="Assets/Cinematics/Storyboard3D/Timelines/Panel11_v04/P11_NevaliDreamStanding_8s_v04.playable";
 const string TLDir="Assets/Cinematics/Storyboard3D/Timelines/Panel11_v05";
 const string TLOut=TLDir+"/P11_NevaliDreamStanding_8s_v05.playable";
 const string ClipOut=TLDir+"/P11_Nevali_v13_TPoseReachHeadBob_v05.anim";
 const string RigPath="Assets/Cinematics/Storyboard3D/Characters/Panel11_v01/Nevali_v13_Mixamo_TPose_WithSkin_P11_v01.fbx";
 const string Report="Library/Panel07_10MotionAudit/Panel11_TposeRigBuilderV05.txt";
 static Panel11TposeRigBuilderV05(){EditorApplication.delayCall+=Build;}
 [MenuItem("SPELLSTRIKE/Cinematics/Panel 11/Build Canonical Standing T-Pose Rig v05")]
 static void Build()
 {
  if(SessionState.GetBool(Key,false)||EditorApplication.isPlayingOrWillChangePlaymode)return;
  Scene original=SceneManager.GetActiveScene();
  if(!original.IsValid()||!original.isLoaded||original.isDirty||string.IsNullOrEmpty(original.path)){Debug.LogWarning("[P11 V05] Active scene must be clean; no changes made.");return;}
  SessionState.SetBool(Key,true);
  var originals=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).path).ToArray();
  var notes=new List<string>{"START active="+original.path+" dirty="+original.isDirty};
  Scene isolated=default(Scene);
  try{
   if(File.Exists(Output)||File.Exists(TLOut)||File.Exists(ClipOut))throw new IOException("Refuse to overwrite existing v05 assets");
   var newRig=AssetDatabase.LoadAssetAtPath<GameObject>(RigPath);if(!newRig)throw new IOException("Authentic standing rig not yet imported: "+RigPath);
   if(!AssetDatabase.CopyAsset(Input,Output))throw new IOException("Could not version v04 scene");
   if(!AssetDatabase.IsValidFolder(TLDir))AssetDatabase.CreateFolder("Assets/Cinematics/Storyboard3D/Timelines","Panel11_v05");
   if(!AssetDatabase.CopyAsset(TLIn,TLOut))throw new IOException("Could not version v04 Timeline");
   var timeline=AssetDatabase.LoadAssetAtPath<TimelineAsset>(TLOut);if(!timeline)throw new IOException("Copied Timeline unavailable");
   isolated=EditorSceneManager.OpenScene(Output,OpenSceneMode.Additive);
   if(!isolated.IsValid()||!isolated.isLoaded||SceneManager.GetActiveScene().path!=original.path)throw new IOException("Additive/safety gate failed");
   var roots=isolated.GetRootGameObjects();
   var actorOld=roots.SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="P11_Nevali_v13_DreamVision_ANIMATED");
   var oldVan=roots.SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="P10_OlderVan_ANIMATED");
   var oldVanAnimator=oldVan.GetComponentInChildren<Animator>(true);if(!oldVanAnimator)throw new IOException("Older Van Animator missing");
   UnityEngine.Object.DestroyImmediate(actorOld.gameObject);
   var girl=PrefabUtility.InstantiatePrefab(newRig,isolated) as GameObject;
   if(!girl)throw new IOException("Failed to instantiate standing FBX prefab");
   girl.name="P11_Nevali_V13_STANDING_DREAM_ANIMATED";
   girl.transform.localScale=Vector3.one;
   girl.transform.position=Vector3.zero;
   girl.transform.rotation=Quaternion.identity;
   var skin=girl.GetComponentsInChildren<SkinnedMeshRenderer>(true);
   if(skin.Length<1)throw new IOException("Standing FBX has no skinned mesh");
   Bounds bounds=skin[0].bounds;foreach(var sr in skin.Skip(1))bounds.Encapsulate(sr.bounds);
   float baseHeight=bounds.size.y;
   if(baseHeight<.00001f||float.IsNaN(baseHeight)||float.IsInfinity(baseHeight))throw new IOException("Invalid imported standing rig bounds height: "+baseHeight);
   float unit=1.45f/baseHeight;
   if(unit<.0000001f||unit>10f)throw new IOException("Implausible imported FBX scale: "+unit);
   girl.transform.localScale=Vector3.one*unit;
   girl.transform.position=new Vector3(-.78f,0f,4.95f);
   girl.transform.rotation=Quaternion.identity;
   Bounds located=skin[0].bounds;foreach(var sr in skin.Skip(1))located.Encapsulate(sr.bounds);
   girl.transform.position+=new Vector3(0,.04f-located.min.y,0);
   var finalBounds=skin[0].bounds;foreach(var sr in skin.Skip(1))finalBounds.Encapsulate(sr.bounds);
   notes.Add("TPOSE_FBX="+RigPath+" skinnedCount="+skin.Length+" sourceHeight="+baseHeight+" autoScale="+unit+" finalBounds="+finalBounds+" location="+girl.transform.position);
   var approvedMatFolder="Assets/Cinematics/Storyboard3D/Characters/Panel01_v03/Materials/";
   var slots=new[]{"Body","Face","Eyes","Hair"};
   var correct=slots.Select(n=>AssetDatabase.LoadAssetAtPath<Material>(approvedMatFolder+"Nevali_Upload_"+n+"_v03.mat")).ToArray();
   foreach(var render in skin){var m=render.sharedMaterials;
    for(int i=0;i<m.Length;i++){
     string name=m[i]?m[i].name.ToLowerInvariant():"";
     int index=Array.FindIndex(slots,s=>name.Contains(s.ToLowerInvariant()));
     if(index>=0 && correct[index])m[i]=correct[index];
    }
    render.sharedMaterials=m;render.updateWhenOffscreen=true;
    notes.Add("MATERIALS="+string.Join(";",render.sharedMaterials.Select(s=>s?s.name:"NULL")));
   }
   var animator=girl.GetComponentInChildren<Animator>(true);
   if(!animator)animator=girl.AddComponent<Animator>();
   if(!animator)throw new IOException("Cannot bind Animator on standing Nevali rig");
   var head=FindBone(girl,"mixamorig:Head");
   var leftArm=FindBone(girl,"mixamorig:LeftArm");
   var rightArm=FindBone(girl,"mixamorig:RightArm");
   var rightForearm=FindBone(girl,"mixamorig:RightForeArm");
   var pose=new AnimationClip{name="P11_NevaliV13_TPoseReach_HeadBob_v05",frameRate=24};
   CreateBoneCurves(pose,girl.transform,head,new[]{0f,.5f,1f,.35f,0},new Vector3(1,0,0),notes);
   CreateBoneCurves(pose,girl.transform,leftArm,new[]{-65f,-65f,-65f,-65f,-65f},new Vector3(0,0,1),notes);
   CreateBoneCurves(pose,girl.transform,rightArm,new[]{22f,22f,22f,22f,22f},new Vector3(0,0,1),notes);
   CreateBoneCurves(pose,girl.transform,rightForearm,new[]{18f,18f,18f,18f,18f},new Vector3(0,1,0),notes);
   pose.EnsureQuaternionContinuity();AssetDatabase.CreateAsset(pose,ClipOut);
   var director=isolated.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PlayableDirector>(true)).Single();
   director.playableAsset=timeline;director.playOnAwake=true;
   foreach(var track in timeline.GetOutputTracks().OfType<AnimationTrack>()){
    var asset=track.GetClips().Single().asset as AnimationPlayableAsset;
    if(!asset)throw new IOException("Invalid animation track payload "+track.name);
    if(track.name.StartsWith("Nevali",StringComparison.Ordinal)){asset.clip=pose;EditorUtility.SetDirty(asset);director.SetGenericBinding(track,animator);}
    else if(track.name.StartsWith("OlderVan",StringComparison.Ordinal)){director.SetGenericBinding(track,oldVanAnimator);}
   }
   if(!EditorSceneManager.SaveScene(isolated,Output))throw new IOException("Could not serialize new P11 scene");
   EditorUtility.SetDirty(timeline);AssetDatabase.SaveAssets();
   notes.Add("SAVED scene="+Output+" timeline="+TLOut+" clip="+ClipOut+" duration="+director.duration);
  }
  catch(Exception error){notes.Add("ERROR="+error);Debug.LogError("[P11 V05] "+error);}
  finally{if(isolated.IsValid()&&isolated.isLoaded)EditorSceneManager.CloseScene(isolated,true);}
  notes.Add("END active="+SceneManager.GetActiveScene().path+" dirty="+SceneManager.GetActiveScene().isDirty);
  notes.Add("SCENE_LIST_UNCHANGED="+originals.SequenceEqual(Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).path)));
  Directory.CreateDirectory(Path.GetDirectoryName(Report));File.WriteAllLines(Report,notes);
  Debug.Log("[P11 V05] "+Report);
 }
 static Transform FindBone(GameObject root,string name){var t=root.GetComponentsInChildren<Transform>(true).SingleOrDefault(x=>x.name==name);if(!t)throw new IOException("Cannot find "+name+" in rigged standing FBX");return t;}
 static void CreateBoneCurves(AnimationClip clip,Transform root,Transform bone,float[] degrees,Vector3 axis,List<string> notes){
  var path=AnimationUtility.CalculateTransformPath(bone,root);var baseQ=bone.localRotation;
  var seconds=new[]{0f,2f,4f,6f,8f};var rot=degrees.Select(d=>baseQ*Quaternion.AngleAxis(d,axis.normalized)).ToArray();
  var props=new[]{"m_LocalRotation.x","m_LocalRotation.y","m_LocalRotation.z","m_LocalRotation.w"};
  for(int i=0;i<4;i++){
   var keys=new Keyframe[seconds.Length];
   for(int j=0;j<seconds.Length;j++){
    var q=rot[j];keys[j]=new Keyframe(seconds[j],i==0?q.x:i==1?q.y:i==2?q.z:q.w);
   }
   AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(path,typeof(Transform),props[i]),new AnimationCurve(keys));
  }
  notes.Add("RIG_POSE "+path+" base="+baseQ.eulerAngles+" angles="+string.Join(",",degrees));
 }
}
#endif


