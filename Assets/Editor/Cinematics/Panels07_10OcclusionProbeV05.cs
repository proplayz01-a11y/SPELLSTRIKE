#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Playables;
internal static class Panels07_10OcclusionProbeV05 {
 const string Key="SPELLSTRIKE_P07_10_OCCLUSION_V05_ONCE";
 const string Out="Library/Panel07_10MotionAudit/UnityReview_v05_Diagnostics";
 static Panels07_10OcclusionProbeV05(){EditorApplication.delayCall+=Run;}
 static void Run(){if(SessionState.GetBool(Key,false))return;var current=SceneManager.GetActiveScene();if(!current.IsValid()||current.isDirty||!current.path.EndsWith("Storyboard3D_CinematicWorkbench.unity"))return;SessionState.SetBool(Key,true);Directory.CreateDirectory(Out);var rows=new List<string>();foreach(var p in new[]{"07","08","09","10"}){Scene s=default(Scene);try{
 s=EditorSceneManager.OpenScene("Assets/Scenes/Cinematics/Prologue_P"+p+"_TimelineReview_v04.unity",OpenSceneMode.Additive);
 var roots=s.GetRootGameObjects();var camera=roots.SelectMany(x=>x.GetComponentsInChildren<Camera>(true)).Single();var director=roots.SelectMany(x=>x.GetComponentsInChildren<PlayableDirector>(true)).Single();director.time=4;director.Evaluate();
 var actorRenderers=roots.SelectMany(x=>x.GetComponentsInChildren<Renderer>(true)).Where(x=>x.GetComponentInParent<Transform>().name.Contains("ANIMATED")||x.transform.root.name.Contains("ANIMATED")).ToArray();
 var actors=roots.SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).Where(x=>x.name.StartsWith("P"+p+"_")&&x.name.EndsWith("_ANIMATED")).ToArray();
 var environment=roots.SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).First(x=>x.name=="P"+p+"_CANONICAL_LIBRARY").GetComponentsInChildren<Renderer>(true);
 rows.Add("P"+p+" cameraEnabled="+camera.enabled+" gameObjectActive="+camera.gameObject.activeInHierarchy+" mask="+camera.cullingMask+" pos="+camera.transform.position+" fwd="+camera.transform.forward+" near="+camera.nearClipPlane+" far="+camera.farClipPlane);
 foreach(var actor in actors){var renderers=actor.GetComponentsInChildren<Renderer>(true);foreach(var rr in renderers){rows.Add(" P"+p+" ACTOR "+actor.name+" renderer="+rr.name+" enabled="+rr.enabled+" active="+rr.gameObject.activeInHierarchy+" bounds="+rr.bounds+" viewport="+camera.WorldToViewportPoint(rr.bounds.center)+" frustum="+GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(camera),rr.bounds));}
 var head=actor.GetComponentsInChildren<Transform>(true).FirstOrDefault(x=>x.name.ToLowerInvariant().Contains("head"));if(head!=null)rows.Add(" HEAD "+head.position+" viewport="+camera.WorldToViewportPoint(head.position));}
 foreach(var rend in environment)rend.enabled=false;
 Capture(camera,Out+"/P"+p+"_ACTORS_ONLY_4s.png");
 if(actors.Length>0){var head=actors[0].GetComponentsInChildren<Transform>(true).FirstOrDefault(x=>x.name.ToLowerInvariant().Contains("head"));if(head!=null){camera.transform.position=head.position+new Vector3(0f,0.0f,2.8f);camera.transform.rotation=Quaternion.LookRotation((head.position-new Vector3(0f,0.27f,0f)-camera.transform.position).normalized);camera.fieldOfView=50f;Capture(camera,Out+"/P"+p+"_ACTOR_DIRECT_4s.png");}}
 }catch(Exception e){rows.Add("ERROR P"+p+" "+e);}finally{if(s.IsValid()&&s.isLoaded)EditorSceneManager.CloseScene(s,true);}}
 File.WriteAllLines(Out+"/results.txt",rows);Debug.Log("[P07-10 V05 OCCLUSION] Completed read-only views: "+Out);
 }
 static void Capture(Camera cam,string path){var rt=RenderTexture.GetTemporary(960,540,24,RenderTextureFormat.ARGB32);var old=cam.targetTexture;var prev=RenderTexture.active;try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(960,540,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,960,540),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);}finally{cam.targetTexture=old;RenderTexture.active=prev;RenderTexture.ReleaseTemporary(rt);}}
}
#endif

