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

// Render separated Editor frames of four existing v06 scenes, using isolated preview scenes only.
// DOES NOT activate, close, save, or switch the user's main Unity scene.
internal static class Panels12_14TimelineReviewV01
{
    const string RunKey="SPELLSTRIKE_P12_TO_P14_RUNTIME_REVIEW_V01_ONCE";
    const string Folder="Library/Panel07_10MotionAudit/Panels12_14_Review_v01";
    static readonly string[] Panels={"12","13","14"};
    static readonly double[] Times={0,4,7.9};
    static readonly List<string> Notes=new List<string>();
    static Scene preview;
    static Camera camera;
    static PlayableDirector director;
    static Dictionary<string,Transform> heads=new Dictionary<string,Transform>();
    static Dictionary<string,Quaternion> first=new Dictionary<string,Quaternion>();
    static int panelIndex,sampleIndex,waitFrames;
    static bool running;
    static string originalPath;
    static string[] scenePaths;
    static Vector3 cameraPosition;
    static Quaternion cameraRotation;
    static Panels12_14TimelineReviewV01(){EditorApplication.delayCall+=StartIfSafe;}
    [MenuItem("SPELLSTRIKE/Cinematics/Panels 07-10/Verify P12-14 Reaction Dialogue in Isolated Unity Frames")]
    static void StartIfSafe()
    {
        if(running||SessionState.GetBool(RunKey,false)||EditorApplication.isPlayingOrWillChangePlaymode)return;
        Scene original=SceneManager.GetActiveScene();if(!original.IsValid()||!original.isLoaded)return;
        SessionState.SetBool(RunKey,true);
        originalPath=original.path;
        scenePaths=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).path).ToArray();
        Directory.CreateDirectory(Folder);
        Notes.Clear();Notes.Add("ACTIVE_BEFORE="+originalPath+" dirty="+original.isDirty);
        panelIndex=0;sampleIndex=0;waitFrames=0;running=true;
        EditorApplication.update+=Advance;
    }
    static void Advance()
    {
        if(!running)return;
        if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().path!=originalPath)
        {Notes.Add("ABORT: Play Mode or active scene changed");Finish();return;}
        if(panelIndex>=Panels.Length){Finish();return;}
        string p=Panels[panelIndex];
        try
        {
            if(!preview.IsValid()||!preview.isLoaded)
            {
                preview=EditorSceneManager.OpenPreviewScene("Assets/Scenes/Cinematics/Prologue_P"+p+"_"+(p=="12"?"VanderReflects":p=="13"?"VanderContemplatesClose":"VanderReplies")+"_v01.unity");
                if(!preview.IsValid()||!preview.isLoaded)throw new Exception("Preview scene load failed");
                var roots=preview.GetRootGameObjects();
                camera=roots.SelectMany(x=>x.GetComponentsInChildren<Camera>(true)).Single();camera.scene=preview;
                director=roots.SelectMany(x=>x.GetComponentsInChildren<PlayableDirector>(true)).Single();
                var timeline=director.playableAsset as TimelineAsset;
                if(!timeline)throw new Exception("Timeline missing");
                var tracks=timeline.GetOutputTracks().OfType<AnimationTrack>().ToArray();
                if(tracks.Length!=(p=="12"||p=="13"?1:2))throw new Exception("Incorrect actor track count: "+tracks.Length);
                heads.Clear();first.Clear();
                foreach(var track in tracks)
                {
                    var animator=director.GetGenericBinding(track) as Animator;
                    var asset=track.GetClips().Single().asset as AnimationPlayableAsset;
                    var clip=asset!=null?asset.clip:null;
                    if(!animator||!clip||!clip.name.Contains("HeadBob"))throw new Exception("Head-bob binding missing on "+track.name);
                    var binding=AnimationUtility.GetCurveBindings(clip).FirstOrDefault(b=>b.propertyName=="m_LocalRotation.x"&&b.path.Split('/').Last().ToLowerInvariant().EndsWith("head"));
                    if(string.IsNullOrEmpty(binding.path))throw new Exception("Head curve missing "+track.name);
                    var head=animator.transform.Find(binding.path);
                    if(!head)throw new Exception("Head transform path not resolved "+binding.path);
                    heads.Add(track.name,head);
                    Notes.Add("P"+p+" TRACK="+track.name+" animator="+animator.name+" clip="+clip.name+" time="+clip.length+" curves="+AnimationUtility.GetCurveBindings(clip).Length);
                }
                director.timeUpdateMode=DirectorUpdateMode.Manual;
                director.RebuildGraph();
                director.Play();
                cameraPosition=camera.transform.position;cameraRotation=camera.transform.rotation;
                Notes.Add("P"+p+" sceneRoots="+roots.Length+" timelineDuration="+director.duration+" camera="+cameraPosition+" fov="+camera.fieldOfView);
                sampleIndex=0;waitFrames=0;
                BeginSample();
                return;
            }
            if(waitFrames>0)
            {
                waitFrames--;
                EditorApplication.QueuePlayerLoopUpdate();
                return;
            }
            double time=Times[sampleIndex];
            string filename=Folder+"/P"+p+"_HeadBob_Unity_"+time.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture).Replace('.','_')+"s.png";
            Capture(camera,filename);
            Notes.Add("P"+p+" FRAME="+time+" output="+filename+" bytes="+new FileInfo(filename).Length);
            foreach(var kv in heads)
            {
                Quaternion now=kv.Value.localRotation;
                if(sampleIndex==0)first[kv.Key]=now;
                float angle=Quaternion.Angle(first[kv.Key],now);
                Notes.Add("P"+p+" BONE_TIME="+time+" actor="+kv.Key+" headFromStartDeg="+angle.ToString("F5",System.Globalization.CultureInfo.InvariantCulture));
            }
            bool staticCamera=Vector3.Distance(cameraPosition,camera.transform.position)<0.0001f && Quaternion.Angle(cameraRotation,camera.transform.rotation)<0.01f;
            Notes.Add("P"+p+" CAMERA_STATIC_AT="+time+" "+staticCamera);
            sampleIndex++;
            if(sampleIndex<Times.Length){BeginSample();return;}
            director.Stop();
            EditorSceneManager.ClosePreviewScene(preview);
            preview=default(Scene);director=null;camera=null;heads.Clear();first.Clear();
            panelIndex++;sampleIndex=0;waitFrames=0;
            EditorApplication.QueuePlayerLoopUpdate();
        }
        catch(Exception ex)
        {
            Notes.Add("P"+p+" ERROR "+ex);Debug.LogError("[P12-P14 REAL PLAYBACK] P"+p+" "+ex);
            if(preview.IsValid()&&preview.isLoaded)EditorSceneManager.ClosePreviewScene(preview);
            preview=default(Scene);director=null;camera=null;
            panelIndex++;sampleIndex=0;waitFrames=0;
        }
    }
    static void BeginSample()
    {
        director.time=Times[sampleIndex];director.Evaluate();
        waitFrames=3;
        EditorApplication.QueuePlayerLoopUpdate();
    }
    static void Capture(Camera cam,string path)
    {
        RenderTexture rt=RenderTexture.GetTemporary(1280,720,24,RenderTextureFormat.ARGB32);
        var previousTexture=cam.targetTexture;var previousActive=RenderTexture.active;
        try
        {
            cam.targetTexture=rt;
            cam.Render();
            RenderTexture.active=rt;
            Texture2D png=new Texture2D(1280,720,TextureFormat.RGB24,false);
            try {png.ReadPixels(new Rect(0,0,1280,720),0,0);png.Apply();File.WriteAllBytes(path,png.EncodeToPNG());}
            finally{UnityEngine.Object.DestroyImmediate(png);}
        }
        finally{cam.targetTexture=previousTexture;RenderTexture.active=previousActive;RenderTexture.ReleaseTemporary(rt);}
    }
    static void Finish()
    {
        running=false;
        EditorApplication.update-=Advance;
        if(preview.IsValid()&&preview.isLoaded)EditorSceneManager.ClosePreviewScene(preview);
        var active=SceneManager.GetActiveScene();
        Notes.Add("ACTIVE_AFTER="+active.path+" dirty="+active.isDirty);
        Notes.Add("SCENE_LIST_UNCHANGED="+scenePaths.SequenceEqual(Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).path)));
        string report=Folder+"/P12_P14_Unity_RuntimeReview.txt";
        File.WriteAllLines(report,Notes);
        Debug.Log("[P12-P14 REAL PLAYBACK] Review complete; evidence: "+report);
    }
}
#endif






