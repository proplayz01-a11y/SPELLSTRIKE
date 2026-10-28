#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

internal static class Panel11PoseScoutV01
{
 const string Key="SPELLSTRIKE_PANEL11_POSE_SCOUT_V01_ONCE";
 const string Dir="Library/Panel07_10MotionAudit/Panel11PoseScout_v01";
 static Panel11PoseScoutV01(){EditorApplication.delayCall+=Run;}
 static void Run(){
  if(SessionState.GetBool(Key,false)||EditorApplication.isPlayingOrWillChangePlaymode)return;
  var active=SceneManager.GetActiveScene();if(!active.IsValid()||!active.isLoaded)return;
  SessionState.SetBool(Key,true);Directory.CreateDirectory(Dir);
  var lines=new List<string>{"ACTIVE_START="+active.path+" dirty="+active.isDirty};
  Scene preview=default(Scene);
  try{
   preview=EditorSceneManager.OpenPreviewScene("Assets/Scenes/Cinematics/Prologue_P10_HeadBobDialogue_v07.unity");
   var roots=preview.GetRootGameObjects();
   var van=roots.SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).First(x=>x.name=="P10_VanderPoel_ANIMATED");
   UnityEngine.Object.DestroyImmediate(van.gameObject);
   GameObject prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Cinematics/Storyboard3D/Characters/Panel01_v03/Nevali_v13_MediaPipe_BlockPlay_v04.fbx");
   GameObject girl=PrefabUtility.InstantiatePrefab(prefab,preview) as GameObject;
   if(!girl)throw new Exception("Failed to instantiate Nevali v13 imported rig into isolated preview");
   girl.name="P11_Nevali_V13_DreamAppearance_PREVIEW";
   var importedClip=AssetDatabase.LoadAllAssetsAtPath("Assets/Cinematics/Storyboard3D/Characters/Panel01_v03/Nevali_v13_MediaPipe_BlockPlay_v04.fbx")
       .OfType<AnimationClip>().First(x=>x.name=="Scene");
   var cam=roots.SelectMany(x=>x.GetComponentsInChildren<Camera>(true)).Single();cam.scene=preview;
   cam.fieldOfView=41f;
   var lightGo=new GameObject("P11_WarmDreamPortraitFill_PREVIEW");SceneManager.MoveGameObjectToScene(lightGo,preview);
   var light=lightGo.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.73f,.49f);light.intensity=3;light.range=6;
   lightGo.transform.position=new Vector3(-.8f,2.4f,5.0f);
   foreach(float scale in new[]{.00016f,.00020f})for(int pose=0;pose<3;pose++){
    girl.transform.localScale=Vector3.one*scale;
    girl.transform.position=new Vector3(-.78f,.38f,5.2f);
    girl.transform.rotation=Quaternion.Euler(0,180,0);
    importedClip.SampleAnimation(girl,pose==0?0:pose==1?2.5f:5.5f);
    var skin=girl.GetComponentsInChildren<SkinnedMeshRenderer>(true);
    var b=skin.First().bounds;
    foreach(var rr in skin.Skip(1))b.Encapsulate(rr.bounds);
    lines.Add("SCALE "+scale+" POSE "+pose+" BOUNDS="+b+" CAMERA_VIEWPORT="+cam.WorldToViewportPoint(b.center));
    if(pose==0||scale==.00016f){string fn=Dir+"/P11_Nevali_s"+(scale==.00016f?"016":"020")+"_pose"+pose+".png";Capture(cam,fn);lines.Add("FRAME "+fn+" "+new FileInfo(fn).Length);}
   }
  }catch(Exception e){lines.Add("ERROR "+e);Debug.LogError("[P11 SCOUT] "+e);}
  finally{if(preview.IsValid()&&preview.isLoaded)EditorSceneManager.ClosePreviewScene(preview);}
  lines.Add("ACTIVE_END="+SceneManager.GetActiveScene().path+" dirty="+SceneManager.GetActiveScene().isDirty);
  File.WriteAllLines(Dir+"/ScoutReport.txt",lines);Debug.Log("[P11 SCOUT] "+Dir);
 }
 static void Capture(Camera c,string path){var rt=RenderTexture.GetTemporary(1280,720,24,RenderTextureFormat.ARGB32);var old=c.targetTexture;var a=RenderTexture.active;try{
  c.targetTexture=rt;c.Render();RenderTexture.active=rt;var png=new Texture2D(1280,720,TextureFormat.RGB24,false);
  try{png.ReadPixels(new Rect(0,0,1280,720),0,0);png.Apply();File.WriteAllBytes(path,png.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(png);}
 }finally{c.targetTexture=old;RenderTexture.active=a;RenderTexture.ReleaseTemporary(rt);}}
}
#endif

