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

// Four new v07 scene and Timeline copies: head-bobbing ONLY, STATIC cameras, no new story/audio/VFX.
internal static class Panels07_10StaticDialogueFramingV07
{
 const string Key="SPELLSTRIKE_PANELS07_10_STATIC_DIALOGUE_FRAMING_V07_ONCE";
 const string TimelineFolder="Assets/Cinematics/Storyboard3D/Timelines/Panels07_10_v07";
 const string Output="Library/Panel07_10MotionAudit/UnityV07_StaticDialogueBuildReport.txt";
 static Panels07_10StaticDialogueFramingV07(){EditorApplication.delayCall+=Build;}
 [MenuItem("SPELLSTRIKE/Cinematics/Panels 07-10/Build v07 static-headbob final review scenes")]
 public static void Build()
 {
   if(SessionState.GetBool(Key,false)||EditorApplication.isPlayingOrWillChangePlaymode)return;
   Scene original=SceneManager.GetActiveScene();
   if(!original.IsValid()||!original.isLoaded||original.isDirty||string.IsNullOrEmpty(original.path))return;
   SessionState.SetBool(Key,true);
   var before=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).path).ToArray();
   var report=new List<string>{"ACTIVE_START="+original.path+" dirty="+original.isDirty};
   if(!AssetDatabase.IsValidFolder(TimelineFolder))AssetDatabase.CreateFolder("Assets/Cinematics/Storyboard3D/Timelines","Panels07_10_v07");
   foreach(string p in new[]{"07","08","09","10"}){
     Scene work=default(Scene);
     try{
       string prior="Assets/Scenes/Cinematics/Prologue_P"+p+"_HeadBobDialogue_v06.unity";
       string scene="Assets/Scenes/Cinematics/Prologue_P"+p+"_HeadBobDialogue_v07.unity";
       string oldTL="Assets/Cinematics/Storyboard3D/Timelines/Panels07_10_v06/P"+p+"_DialogueHeadBob_v06.playable";
       string newTL=TimelineFolder+"/P"+p+"_DialogueHeadBob_v07.playable";
       if(File.Exists(scene)||File.Exists(newTL))throw new IOException("Refusing to overwrite a versioned v07 cinematic");
       if(!AssetDatabase.CopyAsset(prior,scene)||!AssetDatabase.CopyAsset(oldTL,newTL))throw new IOException("Failed to copy v06 cinema assets");
       var timeline=AssetDatabase.LoadAssetAtPath<TimelineAsset>(newTL);
       if(!timeline)throw new IOException("v07 Timeline asset missing");
       work=EditorSceneManager.OpenScene(scene,OpenSceneMode.Additive);
       if(!work.IsValid()||!work.isLoaded||SceneManager.GetActiveScene().path!=original.path)throw new Exception("Active scene changed or additive import failed");
       var roots=work.GetRootGameObjects();
       var director=roots.SelectMany(x=>x.GetComponentsInChildren<PlayableDirector>(true)).Single();
       var camera=roots.SelectMany(x=>x.GetComponentsInChildren<Camera>(true)).Single();
       camera.name="P"+p+"_STATIC_DIALOGUE_CAMERA_v07";
       director.playableAsset=timeline;
       director.playOnAwake=true;
       director.extrapolationMode=DirectorWrapMode.Hold;
       foreach(var track in timeline.GetOutputTracks()){
         if(track is AnimationTrack){
           string role=track.name.Split('|')[0].Trim();
           if(role=="CAMERA")throw new IOException("Unexpected camera animation on static shot");
           var go=roots.SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).Single(x=>x.name=="P"+p+"_"+role+"_ANIMATED");
           var animator=go.GetComponentInChildren<Animator>(true);
           if(!animator)throw new IOException("Missing actor rig "+role);
           var clip=track.GetClips().Single().asset as AnimationPlayableAsset;
           if(clip==null||!clip.clip||!clip.clip.name.Contains("HeadBob"))throw new IOException("Non-headbob animation unexpectedly bound "+role);
           director.SetGenericBinding(track,animator);
           report.Add("P"+p+" ACTOR="+role+" actorClip="+clip.clip.name);
         }
         else if(track is ActivationTrack)director.SetGenericBinding(track,camera.gameObject);
       }
       if(p=="09"){
         Vector3 eye=new Vector3(1.10f,1.85f,10.60f),focus=new Vector3(.35f,1.17f,6.00f);
         camera.transform.position=eye;
         camera.transform.rotation=Quaternion.LookRotation((focus-eye).normalized,Vector3.up);
         camera.fieldOfView=38f;
       }
       if(p=="10"){
         Vector3 eye=new Vector3(0f,1.50f,8.70f),focus=new Vector3(0f,1.15f,5.70f);
         camera.transform.position=eye;
         camera.transform.rotation=Quaternion.LookRotation((focus-eye).normalized,Vector3.up);
         camera.fieldOfView=38f;
       }
       timeline.editorSettings.fps=24;
       EditorUtility.SetDirty(timeline);
       if(!EditorSceneManager.SaveScene(work,scene))throw new IOException("Could not save v07 scene");
       AssetDatabase.SaveAssets();
       report.Add("P"+p+" SCENE="+scene+" TIMELINE="+newTL+" DURATION="+director.duration+" CAMERA_STATIC=true FOV="+camera.fieldOfView);
     }
     catch(Exception e){report.Add("P"+p+" ERROR "+e);Debug.LogError("[P07-10 V07] "+e);}
     finally{if(work.IsValid()&&work.isLoaded)EditorSceneManager.CloseScene(work,true);}
   }
   report.Add("ACTIVE_END="+SceneManager.GetActiveScene().path+" dirty="+SceneManager.GetActiveScene().isDirty);
   report.Add("SCENE_LIST_UNCHANGED="+before.SequenceEqual(Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).path)));
   Directory.CreateDirectory(Path.GetDirectoryName(Output));File.WriteAllLines(Output,report);
   Debug.Log("[P07-10 V07] Static dialogue scenes generated. Build report: "+Output);
 }
}
#endif


