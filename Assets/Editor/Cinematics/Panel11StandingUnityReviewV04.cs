#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

// Single-frame-at-a-time isolated QA, no active scene switching, gameplay edits or saving.
internal static class Panel11StandingUnityReviewV04
{
    const string RunKey="SPELLSTRIKE_P11_STANDING_V04_REVIEW_ONCE";
    const string Out="Library/Panel07_10MotionAudit/Panel11_StandingUnityReview_v04";
    static readonly string[] Scenes={"Assets/Scenes/Cinematics/Prologue_P11_NevaliDream_Standing_v04.unity"};
    static readonly float[] Times={0,4,7.9f};
    static readonly List<string> Results=new List<string>();
    static Scene previousScene,preview;
    static string[] previousScenePaths;
    static Camera camera;
    static PlayableDirector director;
    static readonly Dictionary<string,Transform> HeadBones=new Dictionary<string,Transform>();
    static readonly Dictionary<string,Quaternion> FirstBones=new Dictionary<string,Quaternion>();
    static int sceneIndex,timeIndex;
    static bool running;
    static Panel11StandingUnityReviewV04(){EditorApplication.delayCall+=Begin;}
    [MenuItem("SPELLSTRIKE/Cinematics/Panels 11-14/Panel 11 Standing pose v04 playback check")]
    public static void Begin()
    {
        if(running||SessionState.GetBool(RunKey,false)||EditorApplication.isPlayingOrWillChangePlaymode)return;
        previousScene=SceneManager.GetActiveScene();
        if(!previousScene.IsValid()||!previousScene.isLoaded){Debug.LogWarning("[P11 STANDING QA] Active scene missing. No review.");return;}
        SessionState.SetBool(RunKey,true);
        previousScenePaths=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).path).ToArray();
        Directory.CreateDirectory(Out);
        Results.Clear();Results.Add("ACTIVE_BEFORE="+previousScene.path+" dirty="+previousScene.isDirty);
        sceneIndex=0;timeIndex=0;running=true;
        EditorApplication.update+=Step;
    }
    static void Step()
    {
        if(!running)return;
        if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().path!=previousScene.path){Results.Add("ABORT: Original Unity scene switched or Play Mode started");End();return;}
        if(sceneIndex>=Scenes.Length){End();return;}
        string p=(11+sceneIndex).ToString();
        try {
            if(!preview.IsValid()||!preview.isLoaded){
                preview=EditorSceneManager.OpenPreviewScene(Scenes[sceneIndex]);
                if(!preview.IsValid()||!preview.isLoaded)throw new IOException("Preview scene failed to load");
                var roots=preview.GetRootGameObjects();
                director=roots.SelectMany(g=>g.GetComponentsInChildren<PlayableDirector>(true)).Single();
                camera=roots.SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).Single();camera.scene=preview;
                if(!director.playableAsset)throw new IOException("PlayableDirector has no Timeline");
                var timeline=director.playableAsset as TimelineAsset;
                if(!timeline)throw new IOException("Expected TimelineAsset");
                HeadBones.Clear();FirstBones.Clear();
                foreach(var track in timeline.GetOutputTracks().OfType<AnimationTrack>()){
                    var animator=director.GetGenericBinding(track) as Animator;
                    var tclip=track.GetClips().FirstOrDefault();
                    var anim=(tclip==null?null:tclip.asset as AnimationPlayableAsset);
                    var clip=anim==null?null:anim.clip;
                    if(!animator||!clip)throw new IOException("Unbound actor track "+track.name);
                    var bind=AnimationUtility.GetCurveBindings(clip).FirstOrDefault(b=>b.propertyName=="m_LocalRotation.x" && b.path.Split('/').Last().ToLowerInvariant().EndsWith("head"));
                    if(string.IsNullOrWhiteSpace(bind.path))throw new IOException("Head transform clip binding absent "+track.name);
                    Transform head=animator.transform.Find(bind.path);
                    if(!head)throw new IOException("Animated head transform unresolved: "+bind.path+" animator="+animator.name);
                    HeadBones.Add(track.name,head);
                    Results.Add("P"+p+" BOUND actor="+track.name+" animator="+animator.name+" clip="+clip.name+" duration="+tclip.duration+" curves="+AnimationUtility.GetCurveBindings(clip).Length);
                }
                if(HeadBones.Count!=(sceneIndex==0||sceneIndex==3?2:1))throw new IOException("Incorrect actor track count="+HeadBones.Count);
                director.timeUpdateMode=DirectorUpdateMode.Manual;
                director.RebuildGraph();director.Play();
                Results.Add("P"+p+" LOADED scene="+Scenes[sceneIndex]+" cam="+camera.transform.position+" FOV="+camera.fieldOfView+" timeline="+timeline.name);
            }
            double t=Times[timeIndex];director.time=t;director.Evaluate();
            foreach(var actor in HeadBones){
                if(timeIndex==0)FirstBones[actor.Key]=actor.Value.localRotation;
                var delta=Quaternion.Angle(FirstBones[actor.Key],actor.Value.localRotation);
                Results.Add("P"+p+" TIME="+t.ToString("F1",CultureInfo.InvariantCulture)+" actor="+actor.Key+" HEAD_DEG_FROM_START="+delta.ToString("F5",CultureInfo.InvariantCulture));
            }
            string path=Out+"/P"+p+"_"+t.ToString("F1",CultureInfo.InvariantCulture).Replace('.','_')+"s.png";
            Capture(camera,path);
            Results.Add("P"+p+" CAPTURE="+path+" bytes="+new FileInfo(path).Length);
            timeIndex++;
            if(timeIndex>=Times.Length)ClosePreviewAndAdvance();
        }
        catch(Exception ex){Results.Add("P"+p+" ERROR="+ex);Debug.LogError("[P11 STANDING QA] "+p+" "+ex);ClosePreviewAndAdvance();}
    }
    static void ClosePreviewAndAdvance(){
        if(director)director.Stop();
        if(preview.IsValid()&&preview.isLoaded)EditorSceneManager.ClosePreviewScene(preview);
        preview=default(Scene);director=null;camera=null;HeadBones.Clear();FirstBones.Clear();
        sceneIndex++;timeIndex=0;
    }
    static void Capture(Camera c,string destination){
        var tex=RenderTexture.GetTemporary(640,360,16,RenderTextureFormat.ARGB32);
        var oldTarget=c.targetTexture;var oldActive=RenderTexture.active;
        try {
            c.targetTexture=tex;c.Render();RenderTexture.active=tex;
            var img=new Texture2D(640,360,TextureFormat.RGB24,false);
            try {img.ReadPixels(new Rect(0,0,640,360),0,0);img.Apply();File.WriteAllBytes(destination,img.EncodeToPNG());}
            finally{UnityEngine.Object.DestroyImmediate(img);}
        } finally {c.targetTexture=oldTarget;RenderTexture.active=oldActive;RenderTexture.ReleaseTemporary(tex);}
    }
    static void End(){
        running=false;EditorApplication.update-=Step;
        if(preview.IsValid()&&preview.isLoaded)EditorSceneManager.ClosePreviewScene(preview);
        Results.Add("ACTIVE_AFTER="+SceneManager.GetActiveScene().path+" dirty="+SceneManager.GetActiveScene().isDirty);
        Results.Add("LOADED_SCENES_UNCHANGED="+previousScenePaths.SequenceEqual(Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).path)));
        File.WriteAllLines(Out+"/Results.txt",Results);
        Debug.Log("[P11 STANDING QA] Finished low-memory isolated Timeline check. "+Out+"/Results.txt");
    }
}
#endif




