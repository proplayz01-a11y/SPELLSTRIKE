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
internal static class Panels07_10AssetDiagnosticsV04 {
 const string Key="SPELLSTRIKE_PANEL07_10_DIAG_V04_ONCE";
 static Panels07_10AssetDiagnosticsV04(){EditorApplication.delayCall+=Run;}
 static void Run(){if(SessionState.GetBool(Key,false))return;var original=SceneManager.GetActiveScene();if(EditorApplication.isPlayingOrWillChangePlaymode||!original.IsValid()||original.isDirty||!original.path.EndsWith("Storyboard3D_CinematicWorkbench.unity"))return; SessionState.SetBool(Key,true);
 var lines=new List<string>();
 foreach(var p in new[]{"07","08","09","10"}){Scene s=default(Scene);try{
 s=EditorSceneManager.OpenScene("Assets/Scenes/Cinematics/Prologue_P"+p+"_TimelineCandidate_v03.unity",OpenSceneMode.Additive);
 var roots=s.GetRootGameObjects();var cam=roots.SelectMany(x=>x.GetComponentsInChildren<Camera>(true)).First();var director=roots.SelectMany(x=>x.GetComponentsInChildren<PlayableDirector>(true)).First();var tl=director.playableAsset as TimelineAsset;
 lines.Add("P"+p+" CAMERA "+cam.transform.position+" fwd="+cam.transform.forward);
 foreach(var root in roots){var rends=root.GetComponentsInChildren<Renderer>(true);if(rends.Length==0)continue;Bounds b=rends[0].bounds;foreach(var rr in rends.Skip(1))b.Encapsulate(rr.bounds);lines.Add(" ROOT "+root.name+" count="+rends.Length+" BOUNDS center="+b.center+" extents="+b.extents+" viewport="+cam.WorldToViewportPoint(b.center));}
 foreach(var track in tl.GetOutputTracks()){
 var anim=director.GetGenericBinding(track) as Animator;
 lines.Add(" TRACK "+track.name+" binding="+(anim!=null?anim.name:"not Animator"));
 foreach(var clip in track.GetClips()){var ap=clip.asset as AnimationPlayableAsset;if(ap==null)continue;var ac=ap.clip;lines.Add(" CLIP "+ac.name+" duration="+ac.length+" curves="+AnimationUtility.GetCurveBindings(ac).Length+" animator="+(anim?anim.gameObject.name:"NULL"));foreach(var cb in AnimationUtility.GetCurveBindings(ac).Take(9))lines.Add(" CURVE "+cb.path+" : "+cb.propertyName);}
 }
 foreach(var time in new[]{0.0,4.0,7.9}){director.time=time;director.Evaluate();foreach(var root in roots){var actor=root.GetComponentsInChildren<Transform>(true).Where(x=>x.name.EndsWith("_ANIMATED")).ToArray();foreach(var a in actor){var head=a.GetComponentsInChildren<Transform>(true).FirstOrDefault(x=>x.name.ToLowerInvariant().Contains("head"));var hand=a.GetComponentsInChildren<Transform>(true).FirstOrDefault(x=>x.name.ToLowerInvariant().Contains("righthand"));lines.Add(" TIME "+time+" "+a.name+" head="+(head?head.position.ToString():"NONE")+" rightHand="+(hand?hand.position.ToString():"NONE"));}}}
 }catch(Exception e){lines.Add("ERROR P"+p+" "+e);}finally{if(s.IsValid()&&s.isLoaded)EditorSceneManager.CloseScene(s,true);}}
 string path="Library/Panel07_10MotionAudit/UnityReview_v03/UnityDiagnosticsV04.txt";Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllLines(path,lines);Debug.Log("[P07-10 DIAGNOSTICS] Wrote "+path);
 }
}
#endif

