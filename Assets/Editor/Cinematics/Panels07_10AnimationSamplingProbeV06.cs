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

// Isolated scene-only playback diagnosis. Does not switch/save the active scene.
internal static class Panels07_10AnimationSamplingProbeV06
{
    const string Key="SPELLSTRIKE_P07_10_SAMPLE_ANIMATION_V06_ONE_TIME";
    const string Folder="Library/Panel07_10MotionAudit/UnityReview_v06_AnimationProbe";
    static Panels07_10AnimationSamplingProbeV06(){EditorApplication.delayCall+=Run;}
    [MenuItem("SPELLSTRIKE/Cinematics/Panels 07-10/Probe animation poses ISOLATED v06")]
    static void Run()
    {
        if(SessionState.GetBool(Key,false) || EditorApplication.isPlayingOrWillChangePlaymode) return;
        var original=SceneManager.GetActiveScene();if(!original.IsValid())return;
        string originalPath=original.path;bool originalDirty=original.isDirty;
        SessionState.SetBool(Key,true);Directory.CreateDirectory(Folder);
        var lines=new List<string>{"ACTIVE_BEFORE="+originalPath+" dirty="+originalDirty};
        foreach(string p in new[]{"07","08","09","10"})
        {
            Scene s=default(Scene);
            try
            {
                s=EditorSceneManager.OpenPreviewScene("Assets/Scenes/Cinematics/Prologue_P"+p+"_TimelineReview_v04.unity");
                var roots=s.GetRootGameObjects();
                var camera=roots.SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).Single();camera.scene=s;
                var director=roots.SelectMany(g=>g.GetComponentsInChildren<PlayableDirector>(true)).Single();
                var tl=director.playableAsset as TimelineAsset;
                var bindings=tl.GetOutputTracks().OfType<AnimationTrack>().Select(track => new { track, go=(director.GetGenericBinding(track) as Animator)?.gameObject, clip=(track.GetClips().FirstOrDefault()?.asset as AnimationPlayableAsset)?.clip }).ToArray();
                foreach(var b in bindings)
                {
                    if(b.go==null || b.clip==null)throw new Exception("Animation binding/clip missing "+b.track.name);
                    lines.Add("P"+p+" "+b.go.name+" clip="+b.clip.name+" length="+b.clip.length+" isEmpty="+b.clip.empty+" curves="+AnimationUtility.GetCurveBindings(b.clip).Length);
                    var path=AnimationUtility.GetCurveBindings(b.clip).First().path;
                    var tr=b.go.transform.Find(path);
                    lines.Add("P"+p+" curveBindPath="+path+" resolved="+(tr!=null)+" rootChildren="+b.go.transform.childCount);
                    if(tr!=null)lines.Add("P"+p+" FIRSTBONE initial="+tr.localRotation.eulerAngles);
                }
                // A: Try true Timeline PlayableGraph sampling without entering Play Mode.
                director.RebuildGraph();
                director.Play();
                foreach(double t in new[]{0.0,4.0,7.9})
                {
                    director.time=t;director.Evaluate();
                    string suffix=t.ToString("F1",System.Globalization.CultureInfo.InvariantCulture).Replace('.','_');
                    foreach(var b in bindings){var bone=b.go.transform.Find(AnimationUtility.GetCurveBindings(b.clip).First().path);if(bone!=null)lines.Add("P"+p+" TIMELINE t="+t+" role="+b.go.name+" boneLocal="+bone.localRotation.eulerAngles);}
                    Capture(camera,Folder+"/P"+p+"_TIMELINE_"+suffix+"s.png");
                }
                director.Stop();
                // B: Sample raw approved imported clips into preview-only GameObjects.
                foreach(double t in new[]{0.0,4.0,7.9})
                {
                    foreach(var b in bindings)b.clip.SampleAnimation(b.go,(float)t);
                    string suffix=t.ToString("F1",System.Globalization.CultureInfo.InvariantCulture).Replace('.','_');
                    foreach(var b in bindings){var bone=b.go.transform.Find(AnimationUtility.GetCurveBindings(b.clip).First().path);if(bone!=null)lines.Add("P"+p+" SAMPLED t="+t+" role="+b.go.name+" boneLocal="+bone.localRotation.eulerAngles);}
                    Capture(camera,Folder+"/P"+p+"_SAMPLED_"+suffix+"s.png");
                }
            }
            catch(Exception err){lines.Add("P"+p+" ERROR "+err);Debug.LogError("[P07-10 SAMPLE PROBE] "+p+" "+err);}
            finally{if(s.IsValid()&&s.isLoaded)EditorSceneManager.ClosePreviewScene(s);}
        }
        lines.Add("ACTIVE_AFTER="+SceneManager.GetActiveScene().path+" dirty="+SceneManager.GetActiveScene().isDirty);
        File.WriteAllLines(Folder+"/AnimationProbeV06.txt",lines);
        Debug.Log("[P07-10 SAMPLE PROBE] Captured Timeline vs raw animation evaluations without switching current scene.");
    }
    static void Capture(Camera camera,string path)
    {
        var rt=RenderTexture.GetTemporary(960,540,24,RenderTextureFormat.ARGB32);
        var old=camera.targetTexture;var active=RenderTexture.active;
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
        var image=new Texture2D(960,540,TextureFormat.RGB24,false);
        try{image.ReadPixels(new Rect(0,0,960,540),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
        finally{UnityEngine.Object.DestroyImmediate(image);}}
        finally{camera.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);}
    }
}
#endif

