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
internal static class Panels07_10RigBindingAuditV08{
const string Key="SPELLSTRIKE_PANELS07_10_RIG_BINDING_V08_ONCE";
static Panels07_10RigBindingAuditV08(){EditorApplication.delayCall+=Run;}
static void Run(){if(SessionState.GetBool(Key,false)||EditorApplication.isPlayingOrWillChangePlaymode)return;var original=SceneManager.GetActiveScene();if(!original.IsValid())return;SessionState.SetBool(Key,true);
var rows=new List<string>{"original scene="+original.path+" dirty="+original.isDirty};
foreach(string p in new[]{"07","08","09","10"}){Scene s=default(Scene);try{s=EditorSceneManager.OpenPreviewScene("Assets/Scenes/Cinematics/Prologue_P"+p+"_TimelineReview_v04.unity");var roots=s.GetRootGameObjects();var director=roots.SelectMany(x=>x.GetComponentsInChildren<PlayableDirector>(true)).Single();var tl=(TimelineAsset)director.playableAsset;
foreach(var track in tl.GetOutputTracks().OfType<AnimationTrack>()){
var animator=director.GetGenericBinding(track) as Animator;var go=animator.gameObject;var clip=(track.GetClips().First().asset as AnimationPlayableAsset).clip;
var bs=AnimationUtility.GetCurveBindings(clip);
var dynamicBinding=bs.FirstOrDefault(b=>{var c=AnimationUtility.GetEditorCurve(clip,b);return c!=null && Math.Abs(c.Evaluate(4)-c.Evaluate(0))>0.01f;});
if(string.IsNullOrWhiteSpace(dynamicBinding.path)){rows.Add("P"+p+" "+go.name+" NO dynamic binding threshold");continue;}
Transform target=go.transform.Find(dynamicBinding.path);var curve=AnimationUtility.GetEditorCurve(clip,dynamicBinding);if(target==null){rows.Add("P"+p+" "+go.name+" PATH NOT FOUND "+dynamicBinding.path);continue;}
var mesh=go.GetComponentInChildren<SkinnedMeshRenderer>(true);
rows.Add("P"+p+" "+go.name+" dynamic="+dynamicBinding.path+" property="+dynamicBinding.propertyName+" curve0="+curve.Evaluate(0)+" curve4="+curve.Evaluate(4)+" boneCount="+(mesh?mesh.bones.Length:0)+" boneUsed="+(mesh?mesh.bones.Contains(target):false)+" relative="+target.name);
var before=target.localRotation;
clip.SampleAnimation(go,0f);var at0=target.localRotation;
clip.SampleAnimation(go,4f);var at4=target.localRotation;
rows.Add("P"+p+" SAMPLE rot quaternion 0="+at0+" 4="+at4+" AngleDiff="+Quaternion.Angle(at0,at4)+" initial-to-0="+Quaternion.Angle(before,at0));
if(mesh){rows.Add("P"+p+" skin.rootBone="+(mesh.rootBone?mesh.rootBone.name:"null")+" skin.firstBone="+(mesh.bones.Length>0&&mesh.bones[0]?mesh.bones[0].name:"null")+" updateOffscreen="+mesh.updateWhenOffscreen);}
}
}catch(Exception ex){rows.Add("P"+p+" ERROR "+ex);}finally{if(s.IsValid()&&s.isLoaded)EditorSceneManager.ClosePreviewScene(s);}}
rows.Add("active scene after="+SceneManager.GetActiveScene().path+" dirty="+SceneManager.GetActiveScene().isDirty);
string file="Library/Panel07_10MotionAudit/UnityRigBindingAuditV08.txt";Directory.CreateDirectory(Path.GetDirectoryName(file));File.WriteAllLines(file,rows);Debug.Log("[P07-10 RIG BINDING] "+file);
}}
#endif

