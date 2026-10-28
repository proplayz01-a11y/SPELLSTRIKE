#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

// HISTORICAL candidate, superseded by user direction: NO camera motion. Manual invocation only.
internal static class Panels07_10IsolatedRevisionV05
{
    const string Key="SPELLSTRIKE_PANELS07_10_VERSIONED_REVISION_V05_ADDITIVE_GUARDED_ONCE";
    const string TimelineFolder="Assets/Cinematics/Storyboard3D/Timelines/Panels07_10_v05";
    const string ReportFolder="Library/Panel07_10MotionAudit/UnityReview_v05_Revision";
    // No automatic startup execution: user wants restrained head motion and dialogue only.
    // Removed menu action: this superseded experimental camera-push candidate must not be run.
    private static void BuildHistoricalDisabled()
    {
        if(SessionState.GetBool(Key,false)||EditorApplication.isPlayingOrWillChangePlaymode)return;
        Scene before=SceneManager.GetActiveScene();if(!before.IsValid())return;
        var beforePaths=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).path).ToArray();
        bool beforeDirty=before.isDirty; if(beforeDirty){Debug.LogWarning("[P07-10 V05] No build while active scene has unsaved work: "+before.path);return;}
        SessionState.SetBool(Key,true);
        Directory.CreateDirectory(ReportFolder);
        var report=new List<string>{"START active="+before.path+" dirty="+beforeDirty};
        if(!AssetDatabase.IsValidFolder(TimelineFolder))AssetDatabase.CreateFolder("Assets/Cinematics/Storyboard3D/Timelines","Panels07_10_v05");
        foreach(string p in new[]{"07","08","09","10"})
        {
            Scene scene=default(Scene);
            try
            {
                string sceneOld="Assets/Scenes/Cinematics/Prologue_P"+p+"_TimelineReview_v04.unity";
                string sceneNew="Assets/Scenes/Cinematics/Prologue_P"+p+"_TimelineReview_v05.unity";
                string timelineOld="Assets/Cinematics/Storyboard3D/Timelines/Panels07_10_v04/P"+p+"_Cinematic_v04.playable";
                string timelineNew=TimelineFolder+"/P"+p+"_Cinematic_v05.playable";
                string motionPath=TimelineFolder+"/P"+p+"_CameraMotion_v05.anim";
                bool sceneAlready=File.Exists(sceneNew),timelineAlready=File.Exists(timelineNew);if(sceneAlready!=timelineAlready){report.Add("P"+p+" REFUSED inconsistent or already-finished destination");continue;}
                if(!sceneAlready && !AssetDatabase.CopyAsset(sceneOld,sceneNew))throw new IOException("scene copy failed");
                if(!timelineAlready && !AssetDatabase.CopyAsset(timelineOld,timelineNew))throw new IOException("timeline copy failed");
                var timeline=AssetDatabase.LoadAssetAtPath<TimelineAsset>(timelineNew);
                if(timeline==null)throw new IOException("cannot load new timeline");
                scene=EditorSceneManager.OpenScene(sceneNew,OpenSceneMode.Additive);
                if(!scene.IsValid()||!scene.isLoaded)throw new IOException("cannot load versioned scene additively");if(SceneManager.GetActiveScene().path!=before.path)throw new InvalidOperationException("Active scene changed unexpectedly; abort");
                var rootGos=scene.GetRootGameObjects();
                var director=rootGos.SelectMany(x=>x.GetComponentsInChildren<PlayableDirector>(true)).Single();
                var camera=rootGos.SelectMany(x=>x.GetComponentsInChildren<Camera>(true)).Single();
                
                director.playableAsset=timeline;
                foreach(var track in timeline.GetOutputTracks()){
                    if(track is AnimationTrack){var role=track.name.Split('|')[0].Trim();var t=rootGos.SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).FirstOrDefault(x=>x.name=="P"+p+"_"+role+"_ANIMATED");
                       if(t==null)throw new Exception("Actor missing "+role);var anim=t.GetComponentInChildren<Animator>(true);if(!anim)throw new Exception("Actor Animator missing "+role);director.SetGenericBinding(track,anim);}
                    else if(track is ActivationTrack){director.SetGenericBinding(track,camera.gameObject);}
                }
                var start=camera.transform.position;
                Vector3 focus;float fov;
                switch(p){
                    case "07":start=new Vector3(0.12f,1.68f,9.05f);focus=new Vector3(1.55f,1.13f,6.75f);fov=34f;break;
                    case "08":start=new Vector3(-0.12f,1.70f,9.20f);focus=new Vector3(-1.56f,1.19f,6.75f);fov=29f;break;
                    case "09":start=new Vector3(-0.35f,1.84f,12.50f);focus=new Vector3(0.26f,1.21f,6.15f);fov=34f;break;
                    default:start=new Vector3(0f,2.05f,10.20f);focus=new Vector3(0f,1.14f,5.67f);fov=33f;break;
                }
                camera.transform.position=start;
                camera.transform.rotation=Quaternion.LookRotation((focus-start).normalized,Vector3.up);
                camera.fieldOfView=fov;
                var animator=camera.GetComponent<Animator>();if(!animator)animator=camera.gameObject.AddComponent<Animator>();if(!animator)throw new Exception("Cannot add Animator to versioned camera");
                
                var cameraMotion=AssetDatabase.LoadAssetAtPath<AnimationClip>(motionPath);bool createMotion=!cameraMotion;if(createMotion)cameraMotion=new AnimationClip{name="P"+p+"_CinematicCameraSubtlePush_v05",frameRate=24};
                Vector3 end=start+(focus-start).normalized*(p=="09"?0.22f:0.14f);
                AnimationUtility.SetEditorCurve(cameraMotion,EditorCurveBinding.FloatCurve("",typeof(Transform),"m_LocalPosition.x"),AnimationCurve.EaseInOut(0,start.x,8,end.x));
                AnimationUtility.SetEditorCurve(cameraMotion,EditorCurveBinding.FloatCurve("",typeof(Transform),"m_LocalPosition.y"),AnimationCurve.EaseInOut(0,start.y,8,end.y));
                AnimationUtility.SetEditorCurve(cameraMotion,EditorCurveBinding.FloatCurve("",typeof(Transform),"m_LocalPosition.z"),AnimationCurve.EaseInOut(0,start.z,8,end.z));
                if(createMotion)AssetDatabase.CreateAsset(cameraMotion,motionPath);
                var cameraTrack=timeline.GetOutputTracks().OfType<AnimationTrack>().FirstOrDefault(t=>t.name=="CAMERA | Subtle continuous eight-second push v05");if(cameraTrack==null)cameraTrack=timeline.CreateTrack<AnimationTrack>(null,"CAMERA | Subtle continuous eight-second push v05");
                if(!cameraTrack.GetClips().Any()){var clip=cameraTrack.CreateClip(cameraMotion);clip.start=0;clip.duration=8;}
                director.SetGenericBinding(cameraTrack,animator);
                timeline.editorSettings.fps=24;
                director.RebuildGraph();director.time=0;director.Evaluate();
                EditorUtility.SetDirty(timeline);
                // Saving isolated preview might be unsupported in some Unity versions. Never fallback to touching active Stage3.
                bool saved=EditorSceneManager.SaveScene(scene,sceneNew,false);
                report.Add("P"+p+" SaveScene additive result="+saved+" timeline="+timelineNew+" cameraAnim="+motionPath);
                if(!saved)throw new IOException("Could not save versioned additive scene; active scene preserved");
                AssetDatabase.SaveAssets();
                for(int i=0;i<3;i++){
                    double time=(i==0)?0:(i==1)?4:7.9;
                    director.time=time;director.Evaluate();
                    report.Add("P"+p+" sample time="+time+" camera="+camera.transform.position);
                    string output=ReportFolder+"/P"+p+"_Unity_v05_"+time.ToString("F1",System.Globalization.CultureInfo.InvariantCulture).Replace('.','_')+"s.png";
                    report.Add("P"+p+" additive time="+time+" camera="+camera.transform.position+" (no render capture during additive scene)");
                }
                report.Add("P"+p+" SUCCESS scene="+sceneNew+" director="+director.name+" timeline="+timelineNew+" fov="+fov);
            }
            catch(Exception e){report.Add("P"+p+" ERROR "+e);Debug.LogError("[P07-10 V05 revision] "+p+" "+e);}
            finally{if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);}
        }
        report.Add("END active="+SceneManager.GetActiveScene().path+" dirty="+SceneManager.GetActiveScene().isDirty);
        report.Add("LOADED_SCENES_UNCHANGED="+beforePaths.SequenceEqual(Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).path)));
        File.WriteAllLines(ReportFolder+"/ReviewRevisionV05.txt",report);
        Debug.Log("[P07-10 V05 revision] Completed attempted isolated versioned revisions; inspect " + ReportFolder + "/ReviewRevisionV05.txt");
    }
    static void Capture(Camera cam,string filename){var rt=RenderTexture.GetTemporary(1280,720,24,RenderTextureFormat.ARGB32);var prior=cam.targetTexture;var active=RenderTexture.active;
        try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
            try{image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(filename,image.EncodeToPNG());}
            finally{UnityEngine.Object.DestroyImmediate(image);}}
        finally{cam.targetTexture=prior;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);}
    }
}
#endif




