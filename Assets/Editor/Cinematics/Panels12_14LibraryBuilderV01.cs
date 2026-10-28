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
using UnityEngine.UI;
using TMPro;

// Versioned Panels 12-14, based on existing canonical library/rig/animation only.
internal static class Panels12_14LibraryBuilderV01
{
 const string Once="SPELLSTRIKE_PANEL12_TO_14_LIBRARY_REUSE_V01_ONCE";
 const string SceneBase="Assets/Scenes/Cinematics/Prologue_P10_HeadBobDialogue_v07.unity";
 const string TimelineBase="Assets/Cinematics/Storyboard3D/Timelines/Panels07_10_v07/P10_DialogueHeadBob_v07.playable";
 const string TimelineDir="Assets/Cinematics/Storyboard3D/Timelines/Panels12_14_v01";
 const string Report="Library/Panel07_10MotionAudit/Panels12_14_BuilderV01.txt";
 static Panels12_14LibraryBuilderV01(){EditorApplication.delayCall+=Build;}
 [MenuItem("SPELLSTRIKE/Cinematics/Panels 12-14/Build Library Reactions v01")]
 static void Build()
 {
  if(SessionState.GetBool(Once,false)||EditorApplication.isPlayingOrWillChangePlaymode)return;
  Scene baseScene=SceneManager.GetActiveScene();
  if(!baseScene.IsValid()||!baseScene.isLoaded||baseScene.isDirty||string.IsNullOrEmpty(baseScene.path))return;
  SessionState.SetBool(Once,true);
  var paths=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).path).ToArray();
  var notes=new List<string>{"START active="+baseScene.path+" dirty="+baseScene.isDirty};
  if(!AssetDatabase.IsValidFolder(TimelineDir))AssetDatabase.CreateFolder("Assets/Cinematics/Storyboard3D/Timelines","Panels12_14_v01");
  foreach(string p in new[]{"12","13","14"}){
   Scene scene=default(Scene);
   try{
    string name=p=="12"?"VanderReflects":p=="13"?"VanderContemplatesClose":"VanderReplies";
    string scenePath="Assets/Scenes/Cinematics/Prologue_P"+p+"_"+name+"_v01.unity";
    string timelinePath=TimelineDir+"/P"+p+"_"+name+"_8s_v01.playable";
    if(File.Exists(scenePath)||File.Exists(timelinePath))throw new IOException("No overwriting previous candidate "+p);
    if(!AssetDatabase.CopyAsset(SceneBase,scenePath)||!AssetDatabase.CopyAsset(TimelineBase,timelinePath))throw new IOException("Failed copying P10 canonical baseline");
    var tl=AssetDatabase.LoadAssetAtPath<TimelineAsset>(timelinePath);
    if(!tl)throw new IOException("Could not open versioned timeline "+p);
    scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Additive);
    if(!scene.IsValid()||!scene.isLoaded||SceneManager.GetActiveScene().path!=baseScene.path)throw new IOException("Unsafe scene state after load");
    var roots=scene.GetRootGameObjects();
    var cinematic=roots.Single(x=>x.name.Contains("CINEMATIC_CANDIDATE_ROOT"));
    cinematic.name="P"+p+"_CanonicalLibrary_CINEMATIC_ROOT";
    var actors=cinematic.GetComponentsInChildren<Transform>(true);
    var oldVan=actors.Single(x=>x.name=="P10_OlderVan_ANIMATED");
    var vander=actors.Single(x=>x.name=="P10_VanderPoel_ANIMATED");
    var vanderAnimator=vander.GetComponentInChildren<Animator>(true);var oldAnimator=oldVan.GetComponentInChildren<Animator>(true);
    if(!vanderAnimator||!oldAnimator)throw new IOException("Canonical animated rigs unavailable");
    if(p!="14")UnityEngine.Object.DestroyImmediate(oldVan.gameObject);
    var camera=roots.SelectMany(x=>x.GetComponentsInChildren<Camera>(true)).Single();
    camera.name="P"+p+"_STATIC_LIBRARY_CAMERA";
    Vector3 eye,focus;float fov;
    if(p=="12"){eye=new Vector3(-.77f,1.51f,8.46f);focus=new Vector3(-.75f,1.18f,5.94f);fov=31f;}
    else if(p=="13"){eye=new Vector3(-.72f,1.46f,8.13f);focus=new Vector3(-.75f,1.20f,5.91f);fov=26f;}
    else{eye=new Vector3(0f,1.50f,8.70f);focus=new Vector3(0f,1.15f,5.70f);fov=38f;}
    camera.transform.position=eye;camera.transform.rotation=Quaternion.LookRotation((focus-eye).normalized,Vector3.up);camera.fieldOfView=fov;
    var director=roots.SelectMany(x=>x.GetComponentsInChildren<PlayableDirector>(true)).Single();
    director.playableAsset=tl;director.playOnAwake=true;director.extrapolationMode=DirectorWrapMode.Hold;
    foreach(var track in tl.GetOutputTracks().ToArray()){
      if(track is AnimationTrack){
        var role=track.name.Split('|')[0].Trim();
        if(role=="OlderVan" && p!="14"){tl.DeleteTrack(track);continue;}
        var rig=role=="OlderVan"?oldAnimator:vanderAnimator;
        if(!rig)throw new IOException("Actor Animator not available "+role);
        director.SetGenericBinding(track,rig);
      }else if(track is ActivationTrack)director.SetGenericBinding(track,camera.gameObject);
    }
    if(p=="14")BuildSubtitle(scene,"Vander Poel: We must understand what the dream means.");
    tl.editorSettings.fps=24;EditorUtility.SetDirty(tl);
    if(!EditorSceneManager.SaveScene(scene,scenePath))throw new IOException("Scene serialization failed "+p);
    AssetDatabase.SaveAssets();
    notes.Add("P"+p+" SAVED scene="+scenePath+" timeline="+timelinePath+" duration="+director.duration+" actors="+tl.GetOutputTracks().OfType<AnimationTrack>().Count()+" cameraStatic="+camera.transform.position+" FOV="+fov);
   }catch(Exception ex){notes.Add("P"+p+" ERROR "+ex);Debug.LogError("[P12-P14 BUILD] P"+p+" "+ex);}
   finally{if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);}
  }
  notes.Add("END active="+SceneManager.GetActiveScene().path+" dirty="+SceneManager.GetActiveScene().isDirty);
  notes.Add("SCENE_LIST_UNCHANGED="+paths.SequenceEqual(Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).path)));
  Directory.CreateDirectory(Path.GetDirectoryName(Report));File.WriteAllLines(Report,notes);Debug.Log("[P12-P14 BUILD] "+Report);
 }
 static void BuildSubtitle(Scene scene,string words)
 {
  var o=new GameObject("P14_DIALOGUE_OVERLAY",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
  SceneManager.MoveGameObjectToScene(o,scene);var cv=o.GetComponent<Canvas>();cv.renderMode=RenderMode.ScreenSpaceOverlay;cv.sortingOrder=70;
  var scaler=o.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);
  var label=new GameObject("P14_DIALOGUE_VANDER",typeof(RectTransform),typeof(TextMeshProUGUI));label.transform.SetParent(o.transform,false);
  var rt=label.GetComponent<RectTransform>();rt.anchorMin=new Vector2(.12f,.034f);rt.anchorMax=new Vector2(.88f,.16f);rt.offsetMin=Vector2.zero;rt.offsetMax=Vector2.zero;
  var t=label.GetComponent<TextMeshProUGUI>();t.text=words;t.fontSize=35;t.alignment=TextAlignmentOptions.Center;t.color=new Color(1f,.94f,.80f,1f);t.raycastTarget=false;
 }
}
#endif

