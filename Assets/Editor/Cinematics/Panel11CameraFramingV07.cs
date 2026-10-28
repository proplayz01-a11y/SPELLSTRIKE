#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

// Versioned camera-only framing improvement for verified standing/reaching Panel 11.
[InitializeOnLoad]
internal static class Panel11CameraFramingV07
{
 const string Key="SPELLSTRIKE_PANEL11_CLOSE_CAMERA_FRAMING_V07_ONCE";
 const string Before="Assets/Scenes/Cinematics/Prologue_P11_NevaliDream_StandingReach_v06.unity";
 const string After="Assets/Scenes/Cinematics/Prologue_P11_NevaliDream_StandingReach_v07.unity";
 const string TLOld="Assets/Cinematics/Storyboard3D/Timelines/Panel11_v06/P11_NevaliDreamStandingReach_8s_v06.playable";
 const string TimelineDir="Assets/Cinematics/Storyboard3D/Timelines/Panel11_v07";
 const string TLNew=TimelineDir+"/P11_NevaliDreamStandingReach_8s_v07.playable";
 const string Result="Library/Panel07_10MotionAudit/Panel11_CameraFramingV07.txt";
 static Panel11CameraFramingV07(){EditorApplication.delayCall+=Run;}
 [MenuItem("SPELLSTRIKE/Cinematics/Panel 11/Build v07 Close Framing")]
 static void Run(){
  if(SessionState.GetBool(Key,false)||EditorApplication.isPlayingOrWillChangePlaymode)return;
  var original=SceneManager.GetActiveScene();
  if(!original.IsValid()||!original.isLoaded||original.isDirty||string.IsNullOrWhiteSpace(original.path))return;
  SessionState.SetBool(Key,true);
  var prevPaths=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).path).ToArray();
  var log=new List<string>{"ACTIVE_BEFORE="+original.path+" dirty="+original.isDirty};
  Scene draft=default(Scene);
  try{
    if(File.Exists(After)||File.Exists(TLNew))throw new IOException("Refusing overwrite of new Panel11 v07 assets");
    if(!AssetDatabase.CopyAsset(Before,After))throw new IOException("Source v06 scene copy failed");
    if(!AssetDatabase.IsValidFolder(TimelineDir))AssetDatabase.CreateFolder("Assets/Cinematics/Storyboard3D/Timelines","Panel11_v07");
    if(!AssetDatabase.CopyAsset(TLOld,TLNew))throw new IOException("Source v06 Timeline copy failed");
    var tl=AssetDatabase.LoadAssetAtPath<TimelineAsset>(TLNew);if(!tl)throw new IOException("New v07 Timeline unavailable");
    draft=EditorSceneManager.OpenScene(After,OpenSceneMode.Additive);
    if(!draft.IsValid()||!draft.isLoaded||SceneManager.GetActiveScene().path!=original.path)throw new IOException("Active scene safety check failed");
    var roots=draft.GetRootGameObjects();
    var cam=roots.SelectMany(x=>x.GetComponentsInChildren<Camera>(true)).Single();
    cam.name="P11_STATIC_DREAM_CAMERA_CLOSE_v07";
    var eye=new Vector3(1.40f,1.33f,7.20f);
    var look=new Vector3(-.40f,1.14f,5.00f);
    cam.transform.position=eye;
    cam.transform.rotation=Quaternion.LookRotation((look-eye).normalized,Vector3.up);
    cam.fieldOfView=38f;
    var director=roots.SelectMany(x=>x.GetComponentsInChildren<PlayableDirector>(true)).Single();
    director.playableAsset=tl;director.playOnAwake=true;
    foreach(var track in tl.GetOutputTracks().OfType<AnimationTrack>()){
      var role=track.name.StartsWith("Nevali",StringComparison.Ordinal)?"P11_Nevali_V13_STANDING_DREAM_ANIMATED":"P10_OlderVan_ANIMATED";
      var actor=roots.SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).Single(t=>t.name==role);
      var anim=actor.GetComponentInChildren<Animator>(true);
      if(!anim)throw new IOException("Animation track actor missing: "+track.name);
      director.SetGenericBinding(track,anim);
      log.Add("TRACK="+track.name+" animator="+anim.name);
    }
    if(!EditorSceneManager.SaveScene(draft,After))throw new IOException("Failed saving versioned v07 scene");
    EditorUtility.SetDirty(tl);AssetDatabase.SaveAssets();
    log.Add("SAVED scene="+After+" timeline="+TLNew+" duration="+director.duration+" staticCamera="+eye+" fov=38");
  }catch(Exception error){log.Add("ERROR="+error);Debug.LogError("[P11 v07 camera] "+error);}
  finally{if(draft.IsValid()&&draft.isLoaded)EditorSceneManager.CloseScene(draft,true);}
  log.Add("ACTIVE_AFTER="+SceneManager.GetActiveScene().path+" dirty="+SceneManager.GetActiveScene().isDirty);
  log.Add("SCENES_UNCHANGED="+prevPaths.SequenceEqual(Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).path)));
  Directory.CreateDirectory(Path.GetDirectoryName(Result));File.WriteAllLines(Result,log);
  Debug.Log("[P11 v07 camera] "+Result);
 }
}
#endif
