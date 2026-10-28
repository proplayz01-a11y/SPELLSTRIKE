#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Read-only alternate STATIC-camera framing for P07/P09/P10; no production scene writes.
internal static class Panels07_10StaticCameraScoutV06
{
 const string Key="SPELLSTRIKE_P07_10_STATIC_SHOT_SCOUT_V06_ONCE";
 const string Folder="Library/Panel07_10MotionAudit/UnityReview_v06_StaticShotScout";
 struct Shot{public string Name; public Vector3 Eye,Target;public float Fov;
   public Shot(string n,Vector3 e,Vector3 t,float f){Name=n;Eye=e;Target=t;Fov=f;}}
 static Panels07_10StaticCameraScoutV06(){EditorApplication.delayCall+=Run;}
 static void Run()
 {
   if(SessionState.GetBool(Key,false)||EditorApplication.isPlayingOrWillChangePlaymode)return;
   var original=SceneManager.GetActiveScene();if(!original.IsValid()||!original.isLoaded)return;
   SessionState.SetBool(Key,true);Directory.CreateDirectory(Folder);
   var before=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).path).ToArray();
   var rows=new List<string>{"ACTIVE_BEFORE="+original.path+" dirty="+original.isDirty};
   foreach(string panel in new[]{"07","09","10"})
   {
     Scene preview=default(Scene);
     try{
       string filename="Assets/Scenes/Cinematics/Prologue_P"+panel+"_HeadBobDialogue_v06.unity";
       preview=EditorSceneManager.OpenPreviewScene(filename);
       var cam=preview.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Camera>(true)).Single();cam.scene=preview;
       var shots=BuildShots(panel);
       foreach(var s in shots)
       {
         cam.transform.position=s.Eye;
         cam.transform.rotation=Quaternion.LookRotation((s.Target-s.Eye).normalized,Vector3.up);
         cam.fieldOfView=s.Fov;
         string output=Folder+"/P"+panel+"_"+s.Name+".png";
         Render(cam,output);
         rows.Add("P"+panel+" "+s.Name+" pos="+s.Eye+" target="+s.Target+" fov="+s.Fov+" screenshot="+output+" bytes="+new FileInfo(output).Length);
       }
     }
     catch(Exception e){rows.Add("P"+panel+" ERROR "+e);Debug.LogError("[P07-10 SHOT SCOUT] P"+panel+" "+e);}
     finally{if(preview.IsValid()&&preview.isLoaded)EditorSceneManager.ClosePreviewScene(preview);}
   }
   rows.Add("ACTIVE_AFTER="+SceneManager.GetActiveScene().path+" dirty="+SceneManager.GetActiveScene().isDirty);
   rows.Add("SCENE_LIST_UNCHANGED="+before.SequenceEqual(Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).path)));
   File.WriteAllLines(Folder+"/StaticShotScoutV06.txt",rows);
   Debug.Log("[P07-10 SHOT SCOUT] Saved static camera options in "+Folder);
 }
 static Shot[] BuildShots(string p)
 {
   if(p=="07")return new[]{
     new Shot("A_existing",new Vector3(.16f,1.533f,8.452f),new Vector3(1.55f,1.16f,6.75f),25.06f),
     new Shot("B_headfront",new Vector3(2.00f,1.50f,9.15f),new Vector3(1.55f,1.18f,6.75f),29f),
     new Shot("C_chestfront",new Vector3(3.00f,1.48f,8.65f),new Vector3(1.55f,1.16f,6.75f),32f),
     new Shot("D_headcenter",new Vector3(1.60f,1.55f,9.20f),new Vector3(1.55f,1.17f,6.75f),30f)};
   if(p=="09")return new[]{
     new Shot("A_v04",new Vector3(.15f,1.82f,12.25f),new Vector3(.58f,1.28f,6.30f),33f),
     new Shot("B_insidecenter",new Vector3(.45f,1.80f,11.00f),new Vector3(.40f,1.20f,6.00f),39f),
     new Shot("C_insideleft",new Vector3(-.55f,1.75f,10.35f),new Vector3(.40f,1.12f,6.00f),42f),
     new Shot("D_insideright",new Vector3(1.10f,1.85f,10.60f),new Vector3(.35f,1.17f,6.00f),38f)};
   return new[]{
     new Shot("A_v06",new Vector3(0f,1.80f,9.34f),new Vector3(0f,1.12f,5.70f),34f),
     new Shot("B_lowwide",new Vector3(0f,1.42f,9.15f),new Vector3(0f,1.20f,5.55f),35f),
     new Shot("C_closer",new Vector3(0f,1.50f,8.70f),new Vector3(0f,1.15f,5.70f),38f),
     new Shot("D_lowafar",new Vector3(0f,1.36f,9.35f),new Vector3(0f,1.15f,5.70f),33f)};
 }
 static void Render(Camera cam,string path)
 {
   var rt=RenderTexture.GetTemporary(1280,720,24,RenderTextureFormat.ARGB32);
   var old=cam.targetTexture;var prev=RenderTexture.active;
   try{
     cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
     var img=new Texture2D(1280,720,TextureFormat.RGB24,false);
     try{img.ReadPixels(new Rect(0,0,1280,720),0,0);img.Apply();File.WriteAllBytes(path,img.EncodeToPNG());}
     finally{UnityEngine.Object.DestroyImmediate(img);}
   }
   finally{cam.targetTexture=old;RenderTexture.active=prev;RenderTexture.ReleaseTemporary(rt);}
 }
}
#endif

