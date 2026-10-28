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

// Read-only preview-scene validation: DOES NOT open/close/change/save the active user scene.
internal static class Panels07_10IsolatedPreviewV05
{
    const string RunKey = "SPELLSTRIKE_P07_10_ISOLATED_PREVIEW_V05_20261027_ONCE";
    const string OutFolder = "Library/Panel07_10MotionAudit/UnityReview_v05_Isolated";
    static Panels07_10IsolatedPreviewV05() { EditorApplication.delayCall += CaptureWhenSafe; }
    [MenuItem("SPELLSTRIKE/Cinematics/Panels 07-10/Capture ISOLATED v04 scenes (no scene switch)")]
    public static void CaptureWhenSafe()
    {
        if (SessionState.GetBool(RunKey, false)) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("[P07-10 ISOLATED] Play Mode active; no preview attempted."); return; }
        var activeBefore = SceneManager.GetActiveScene();
        if (!activeBefore.IsValid()) return;
        var allBefore = Enumerable.Range(0, SceneManager.sceneCount).Select(i => SceneManager.GetSceneAt(i).path).ToArray();
        SessionState.SetBool(RunKey, true);
        Directory.CreateDirectory(OutFolder);
        var report = new List<string>();
        report.Add("ACTIVE_SCENE_BEFORE=" + activeBefore.path + " dirty=" + activeBefore.isDirty);
        foreach (string panel in new [] {"07", "08", "09", "10"})
        {
            Scene preview = default(Scene);
            try
            {
                string filename = "Assets/Scenes/Cinematics/Prologue_P" + panel + "_TimelineReview_v04.unity";
                preview = EditorSceneManager.OpenPreviewScene(filename);
                if (!preview.IsValid() || !preview.isLoaded) throw new InvalidOperationException("Preview scene didn't load " + filename);
                GameObject[] roots = preview.GetRootGameObjects();
                var camera = roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true)).Single();
                var director = roots.SelectMany(r => r.GetComponentsInChildren<PlayableDirector>(true)).Single();
                camera.scene = preview;  // Render only this preview scene, not Stage3 or the cinematic workbench.
                var allR = roots.SelectMany(r => r.GetComponentsInChildren<Renderer>(true)).ToArray();
                report.Add("P" + panel + " isolatedRootCount=" + roots.Length + " renderers=" + allR.Length + " cameraScene=" + camera.scene.path);
                var tl = director.playableAsset as TimelineAsset;
                report.Add("P"+panel+" duration="+director.duration+" tracks="+(tl == null ? -1 : tl.GetOutputTracks().Count()));
                if (tl != null) foreach(var track in tl.GetOutputTracks().OfType<AnimationTrack>())
                {
                    var animator = director.GetGenericBinding(track) as Animator;
                    var clip = track.GetClips().FirstOrDefault()?.asset as AnimationPlayableAsset;
                    report.Add("P" + panel + " animation=" + track.name + " boundAnimator=" + (animator ? animator.name : "MISSING") + " clip="+(clip && clip.clip ? clip.clip.name : "MISSING"));
                }
                director.RebuildGraph();
                foreach(double second in new[] {0.0,4.0,7.9})
                {
                    director.time = second;
                    director.Evaluate();
                    string timeTag = second.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture).Replace('.', '_');
                    string outfile = OutFolder + "/P" + panel + "_ISOLATED_" + timeTag + "s.png";
                    Render(camera,outfile);
                    report.Add("P" + panel + " screenshot " + outfile + " bytes=" + new FileInfo(outfile).Length);
                }
            }
            catch(Exception error) { report.Add("P"+panel+" ERROR "+error);Debug.LogError("[P07-10 ISOLATED] P"+panel+" failed: "+error); }
            finally { if(preview.IsValid() && preview.isLoaded) EditorSceneManager.ClosePreviewScene(preview); }
        }
        var activeAfter = SceneManager.GetActiveScene();
        var allAfter = Enumerable.Range(0,SceneManager.sceneCount).Select(i => SceneManager.GetSceneAt(i).path).ToArray();
        report.Add("ACTIVE_SCENE_AFTER="+activeAfter.path+" dirty="+activeAfter.isDirty);
        report.Add("SCENE_LIST_UNCHANGED=" + allBefore.SequenceEqual(allAfter));
        File.WriteAllLines(OutFolder+"/UnityIsolatedReviewV05.txt", report);
        Debug.Log("[P07-10 ISOLATED] Finished all four in isolated preview; active scene unchanged=" + (activeBefore.path==activeAfter.path && allBefore.SequenceEqual(allAfter)));
    }
    static void Render(Camera camera, string output)
    {
        var tex = RenderTexture.GetTemporary(1280,720,24,RenderTextureFormat.ARGB32);
        var priorTex=camera.targetTexture;
        var priorRT=RenderTexture.active;
        try {
            camera.targetTexture=tex; camera.Render(); RenderTexture.active=tex;
            var cpu=new Texture2D(1280,720,TextureFormat.RGB24,false);
            try{cpu.ReadPixels(new Rect(0,0,1280,720),0,0);cpu.Apply();File.WriteAllBytes(output,cpu.EncodeToPNG());}
            finally{UnityEngine.Object.DestroyImmediate(cpu);}
        }
        finally {camera.targetTexture=priorTex;RenderTexture.active=priorRT;RenderTexture.ReleaseTemporary(tex);}
    }
}
#endif

