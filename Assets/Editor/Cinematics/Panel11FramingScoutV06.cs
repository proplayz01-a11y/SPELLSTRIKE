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

// Pure read-only isolated camera scouting; cannot save/change the active Unity scene.
internal static class Panel11FramingScoutV06
{
 const string Key="SPELLSTRIKE_P11_REACH_FRAMING_SCOUT_V06_ONCE";
 const string Source="Assets/Scenes/Cinematics/Prologue_P11_NevaliDream_StandingReach_v06.unity";
 const string Out="Library/Panel07_10MotionAudit/Panel11_FramingScout_v06";
 static Panel11FramingScoutV06(){EditorApplication.delayCall+=Run;}
 static void Run(){
  if(SessionState.GetBool(Key,false)||EditorApplication.isPlayingOrWillChangePlaymode)return;
  var old=SceneManager.GetActiveScene();if(!old.IsValid())return;
  SessionState.SetBool(Key,true);Directory.CreateDirectory(Out);
  var rows=new List<string>{"ACTIVE_START="+old.path+" dirty="+old.isDirty};
  Scene temp=default(Scene);
  try{
   temp=EditorSceneManager.OpenPreviewScene(Source);
   var roots=temp.GetRootGameObjects();
   var camera=roots.SelectMany(x=>x.GetComponentsInChildren<Camera>(true)).Single();camera.scene=temp;
   var director=roots.SelectMany(x=>x.GetComponentsInChildren<PlayableDirector>(true)).Single();
   director.timeUpdateMode=DirectorUpdateMode.Manual;director.RebuildGraph();director.Play();director.time=4;director.Evaluate();
   var angles=new[]{
    new {name="A_original",eye=new Vector3(0f,1.50f,8.70f),target=new Vector3(0f,1.16f,5.45f),fov=42f},
    new {name="B_midleft",eye=new Vector3(.65f,1.23f,7.50f),target=new Vector3(-.32f,1.23f,5.05f),fov=39f},
    new {name="C_olderShoulder",eye=new Vector3(1.40f,1.33f,7.20f),target=new Vector3(-.40f,1.14f,5.00f),fov=38f},
    new {name="D_nearCorner",eye=new Vector3(1.90f,1.45f,8.10f),target=new Vector3(-.35f,1.11f,5.00f),fov=38f}
   };
   foreach(var item in angles){camera.transform.position=item.eye;camera.transform.rotation=Quaternion.LookRotation((item.target-item.eye).normalized,Vector3.up);camera.fieldOfView=item.fov;
    var path=Out+"/P11_"+item.name+".png";Capture(camera,path);
    rows.Add("SHOT="+item.name+" camera="+item.eye+" focus="+item.target+" fov="+item.fov+" screenshot="+path+" bytes="+new FileInfo(path).Length);
   }
   director.Stop();
  }catch(Exception ex){rows.Add("ERROR="+ex);Debug.LogError("[P11 framing scout] "+ex);}
  finally{if(temp.IsValid()&&temp.isLoaded)EditorSceneManager.ClosePreviewScene(temp);}
  rows.Add("ACTIVE_END="+SceneManager.GetActiveScene().path+" dirty="+SceneManager.GetActiveScene().isDirty);
  File.WriteAllLines(Out+"/Results.txt",rows);Debug.Log("[P11 framing scout] "+Out);
 }
 static void Capture(Camera camera,string path){var rt=RenderTexture.GetTemporary(640,360,16,RenderTextureFormat.ARGB32);var old=camera.targetTexture;var active=RenderTexture.active;
  try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var img=new Texture2D(640,360,TextureFormat.RGB24,false);try{img.ReadPixels(new Rect(0,0,640,360),0,0);img.Apply();File.WriteAllBytes(path,img.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(img);}}
  finally{camera.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);}}
}
#endif

