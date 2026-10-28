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
internal static class Panels07_10CorrectAndCaptureV04
{
    const string Key = "SPELLSTRIKE_PANELS07_10_CORRECT_V04_ONESHOT";
    const string Out = "Library/Panel07_10MotionAudit/UnityReview_v04";
    const string TimelineFolder = "Assets/Cinematics/Storyboard3D/Timelines/Panels07_10_v04";
    static Panels07_10CorrectAndCaptureV04() { EditorApplication.delayCall += Run; }
    [MenuItem("SPELLSTRIKE/Cinematics/Panels 07-10/Build and Capture Corrected v04")]
    public static void Run()
    {
        if (SessionState.GetBool(Key, false)) return;
        var baseScene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || !baseScene.IsValid() || !baseScene.isLoaded || baseScene.isDirty || !baseScene.path.EndsWith("Storyboard3D_CinematicWorkbench.unity", StringComparison.Ordinal))
        { Debug.LogWarning("[P07-10 V04] Safety gate: clean cinematic workbench needed; active="+baseScene.path+" dirty="+baseScene.isDirty); return; }
        SessionState.SetBool(Key, true);
        Directory.CreateDirectory(Out);
        var rows = new List<string>();
        if (!AssetDatabase.IsValidFolder(TimelineFolder)) AssetDatabase.CreateFolder("Assets/Cinematics/Storyboard3D/Timelines", "Panels07_10_v04");
        foreach(var p in new[]{"07", "08", "09", "10"})
        {
            Scene s = default(Scene);
            try
            {
                string sourcePath="Assets/Scenes/Cinematics/Prologue_P"+p+"_TimelineCandidate_v03.unity";
                string targetPath="Assets/Scenes/Cinematics/Prologue_P"+p+"_TimelineReview_v04.unity";
                string oldTimeline="Assets/Cinematics/Storyboard3D/Timelines/Panels07_10_v03/P"+p+"_Cinematic_v03.playable";
                string newTimeline=TimelineFolder+"/P"+p+"_Cinematic_v04.playable";
                if (File.Exists(targetPath)||File.Exists(newTimeline)) {rows.Add("P"+p+" ERROR target already exists; refused overwrite");continue;}
                if(!AssetDatabase.CopyAsset(oldTimeline,newTimeline)) throw new Exception("Could not copy Timeline "+oldTimeline);
                var tl=AssetDatabase.LoadAssetAtPath<TimelineAsset>(newTimeline);
                if(tl==null)throw new Exception("Copied Timeline not imported "+newTimeline);
                s=EditorSceneManager.OpenScene(sourcePath,OpenSceneMode.Additive);
                var roots=s.GetRootGameObjects();
                var director=roots.SelectMany(x=>x.GetComponentsInChildren<PlayableDirector>(true)).Single();
                var camera=roots.SelectMany(x=>x.GetComponentsInChildren<Camera>(true)).Single();
                var actors=roots.SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).Where(x=>x.name.StartsWith("P"+p+"_")&&x.name.EndsWith("_ANIMATED")).ToArray();
                director.playableAsset=tl;
                foreach(var track in tl.GetOutputTracks())
                {
                    if(track is AnimationTrack)
                    {
                        string role=track.name.Split('|')[0].Trim();
                        var go=actors.Single(x=>x.name=="P"+p+"_"+role+"_ANIMATED");
                        var animator=go.GetComponentInChildren<Animator>(true);
                        if(animator==null)throw new Exception("Animator missing "+go.name);
                        string asset="Assets/Cinematics/Storyboard3D/Panels07_10_v02/Panel"+p+"/P"+p+"_"+role+"_CinematicRefinement_Animated_v02.glb";
                        var clip=AssetDatabase.LoadAllAssetsAtPath(asset).OfType<AnimationClip>().FirstOrDefault(x=>x.name.StartsWith("P"+p+"_",StringComparison.Ordinal)&&x.length>=7.9f);
                        if(clip==null)throw new Exception("Missing panel-specific 8-second animation clip: "+asset);
                        foreach(var timelineClip in track.GetClips())
                        {
                            var animationPlayable=timelineClip.asset as AnimationPlayableAsset;
                            if(animationPlayable==null)throw new Exception("Not an AnimationPlayableAsset: "+track.name);
                            animationPlayable.clip=clip;
                            timelineClip.duration=8.0;
                            timelineClip.start=0.0;
                            EditorUtility.SetDirty(animationPlayable);
                        }
                        director.SetGenericBinding(track,animator);
                        rows.Add("P"+p+" TRACK "+role+" selected="+clip.name+" duration="+clip.length.ToString("F2")+" curves="+AnimationUtility.GetCurveBindings(clip).Length);
                    }
                    else if(track is ActivationTrack) director.SetGenericBinding(track,camera.gameObject);
                }
                // Keep original authored cameras and use measured actor head positions for yaw/pitch.
                var headPositions=new List<Vector3>();
                foreach(var actor in actors)
                {
                    var head=actor.GetComponentsInChildren<Transform>(true).FirstOrDefault(x=>x.name.ToLowerInvariant().Contains("head"));
                    if(head!=null)headPositions.Add(head.position);
                }
                if(p=="09")
                {
                    // Wider, modestly elevated two-character establishing shot to retain gateway.
                    camera.transform.position=new Vector3(0.15f,1.82f,12.25f);
                    camera.fieldOfView=33f;
                }
                Vector3 focal;
                if(p=="07")focal=new Vector3(1.63f,1.26f,6.76f);
                else if(p=="08")focal=new Vector3(-1.62f,1.27f,6.76f);
                else if(p=="09")focal=new Vector3(0.58f,1.28f,6.30f);
                else focal=new Vector3(0f,1.05f,5.67f);
                camera.transform.rotation=Quaternion.LookRotation((focal-camera.transform.position).normalized,Vector3.up);
                camera.clearFlags=CameraClearFlags.SolidColor;
                camera.backgroundColor=new Color(0.065f,0.046f,0.055f,1f);
                var env=roots.SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).FirstOrDefault(x=>x.name=="P"+p+"_CANONICAL_LIBRARY");
                var environment=env!=null?env.GetComponentsInChildren<Renderer>(true):new Renderer[0];
                // Ignore geometry physically between the camera and visible faces (Blender hide_render is not transferred to GLB).
                // Preserve all meshes, simply deactivate rendering for identified camera-obstructing parts in the NEW scene.
                int hidden=0;
                var occlusionTargets=new List<Vector3>();
                if(p=="09")occlusionTargets.AddRange(headPositions);
                else occlusionTargets.AddRange(headPositions);
                foreach(var renderer in environment)
                {
                    if(!renderer.enabled)continue;
                    bool obstructs=false;
                    foreach(var target in occlusionTargets)
                    {
                        Vector3 dir=target-camera.transform.position;
                        float distance=dir.magnitude;
                        if(distance<0.01f)continue;
                        var ray=new Ray(camera.transform.position,dir/distance);
                        float hit;
                        if(renderer.bounds.IntersectRay(ray,out hit)&&hit>=0&&hit<distance-0.25f)
                        { obstructs=true;break; }
                    }
                    if(obstructs)
                    {
                        renderer.enabled=false;
                        hidden++;
                        if(hidden<=22)rows.Add("P"+p+" camera-obstructing environment renderer hidden: "+renderer.name);
                    }
                }
                rows.Add("P"+p+" preserved environments="+environment.Length+" selective hidden="+hidden+" camera="+camera.transform.position+" target="+focal);
                // Scene-local lighting only. Original scenes, pipeline settings and renderer assets remain intact.
                var fill=new GameObject("P"+p+"_CINEMATIC_WARM_FILL_LIGHT_v04");
                SceneManager.MoveGameObjectToScene(fill,s);
                var light=fill.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1.0f,0.86f,0.73f);light.intensity=2.1f;light.range=12f;light.shadows=LightShadows.None;
                fill.transform.position=p=="09"?new Vector3(0f,3.2f,9f):new Vector3(0f,3.4f,7.4f);
                var rim=new GameObject("P"+p+"_CINEMATIC_COOL_RIM_LIGHT_v04");SceneManager.MoveGameObjectToScene(rim,s);
                var rimL=rim.AddComponent<Light>();rimL.type=LightType.Point;rimL.color=new Color(0.67f,0.75f,1f);rimL.intensity=0.9f;rimL.range=12f;rimL.shadows=LightShadows.None;
                rim.transform.position=new Vector3(-2f,3.5f,4.8f);
                director.RebuildGraph();
                director.time=0;
                director.Evaluate();
                EditorUtility.SetDirty(tl);
                if(!EditorSceneManager.SaveScene(s,targetPath,true))throw new Exception("Scene saveAsCopy failed "+targetPath);
                AssetDatabase.SaveAssets();
                foreach(var time in new[]{0.0,4.0,7.9})
                {
                    director.time=time;director.Evaluate();
                    var rt=RenderTexture.GetTemporary(1280,720,24,RenderTextureFormat.ARGB32);
                    var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;
                    try
                    {
                        camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                        var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);
                        tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();
                        string filename=Out+"/P"+p+"_Unity_v04_"+time.ToString("F1",System.Globalization.CultureInfo.InvariantCulture).Replace('.','_')+"s.png";
                        File.WriteAllBytes(filename,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
                        rows.Add("P"+p+" FRAME "+time+" screenshot="+filename+" bytes="+new FileInfo(filename).Length);
                    }
                    finally{camera.targetTexture=oldTarget;RenderTexture.active=oldActive;RenderTexture.ReleaseTemporary(rt);}
                }
                rows.Add("P"+p+" PASS files scene="+targetPath+" timeline="+newTimeline);
            }
            catch(Exception e){rows.Add("P"+p+" ERROR "+e);Debug.LogError("[P07-10 V04] P"+p+" failed: "+e);}
            finally{if(s.IsValid()&&s.isLoaded)EditorSceneManager.CloseScene(s,true);}
        }
        string report=Out+"/UnityReviewV04.txt";
        File.WriteAllLines(report,rows);
        Debug.Log("[P07-10 V04] Completed scene/Timeline correction. Report="+report+" (visual inspection not yet PASS)");
    }
}
#endif

