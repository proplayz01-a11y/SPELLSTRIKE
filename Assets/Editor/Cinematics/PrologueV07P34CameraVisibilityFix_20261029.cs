#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

/// <summary>
/// V07-only P34 camera repair: prior lateral camera (-3.55,1.58,-1.28)
/// fell inside a solid bookshelf and all prior screenshots were blocked.
/// Reuses P35's independently proven clear corridor camera *location*,
/// while framing the actual P34 walk path. Renders four new shots for QA.
/// </summary>
internal static class PrologueV07P34CameraVisibilityFix_20261029
{
    private const string ScenePath = "Assets/Scenes/Cinematics/Prologue_Master_Sequence_P01_42_v07_SpellbookWalkPolish.unity";
    private const string Protected = "Assets/Scenes/Cinematics/Prologue_Master_Sequence_P01_42_v06_DialogueContinuity.unity";
    private const string Backup = "RecoveryBackups/ProloguePolishV07_20261028_Immutable_v01/Prologue_Master_v06_BEFORE_POLISH.unity";
    private const string ProtectedHash = "283573BAA9EB7C9E5D3CEF9290FFDDC3DF1F026B8C8D65EAF6D84A73CB26EBF4";
    private const string Screens = "Assets/Cinematics/Storyboard3D/Timelines/PrologueMaster_v07_SpellbookWalkPolish/Validation/Screenshots/";
    private const string Report = "Library/PrologueV07_P34_CameraVisibilityFix_20261029.txt";

    [MenuItem("SPELLSTRIKE/Cinematics/Prologue V07/13 FIX P34 bookshelf occlusion + four camera PNGs")]
    private static void Execute()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            EditorApplication.isUpdating || SceneManager.sceneCount != 1 ||
            SceneManager.GetActiveScene().isDirty ||
            System.Diagnostics.Process.GetProcessesByName("VALORANT-Win64-Shipping").Length != 0 ||
            Hash(Protected) != ProtectedHash || Hash(Backup) != ProtectedHash ||
            AssetDatabase.AssetPathToGUID(ScenePath) != "12405e3bd39d43d398685e74678a8516")
        {
            Debug.LogError("[V07 P34 CAMERA] Unsafe editor, game running or protected scene mismatch.");
            return;
        }
        string[] stamps = {"t01","t02","t04","t07"};
        foreach(string stamp in stamps)
        {
            string filename=Screens+"P34_UPRIGHT_STRIDE_"+stamp+"_visible_20261029.png";
            if(File.Exists(filename))
            {
                Debug.LogError("[V07 P34 CAMERA] Existing screenshot, no overwrite: "+filename);
                return;
            }
        }
        var prior=SceneManager.GetActiveScene();
        Scene loaded=default;
        var report=new StringBuilder();
        try
        {
            loaded=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
            SceneManager.SetActiveScene(loaded);
            var shots=loaded.GetRootGameObjects().Single(g=>
                g.name=="MASTER_SHOT_GROUPS__NUMBERED_IN_STORY_ORDER").transform;
            var p34=shots.Find("P34_SHOT_ROOT");
            var p35=shots.Find("P35_SHOT_ROOT");
            if (!p34 || !p35) throw new InvalidOperationException("P34/P35 shot root missing.");
            var camera=p34.GetComponentsInChildren<Camera>(true)
                .Single(c=>c.name=="P34_CORRIDOR_STATIC_CAMERA");
            var p35Camera=p35.GetComponentsInChildren<Camera>(true)
                .Single(c=>c.name=="P35_CORRIDOR_STATIC_CAMERA");
            var director=p34.GetComponentsInChildren<PlayableDirector>(true)
                .Single(d=>d.playableAsset is TimelineAsset);
            var timeline=(TimelineAsset)director.playableAsset;
            if(AssetDatabase.GetAssetPath(timeline)!=
                "Assets/Cinematics/Storyboard3D/Timelines/PrologueMaster_v07_SpellbookWalkPolish/P34_P34_DialogueAggressive_v05_v07.playable")
                throw new InvalidOperationException("P34 no longer uses independent v07 Timeline.");
            var route=p34.Find("V07_UPRIGHT_VANDER_WALK_PATH");
            var walker=route?route.Find("V07_STANDING_VANDER_REAL_WALK_VISIBLE"):null;
            if(!walker) throw new InvalidOperationException("P34 upright walking actor missing.");
            var visible=walker.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(s=>s.sharedMesh!=null);
            var track=timeline.GetOutputTracks().OfType<AnimationTrack>()
                .Single(t=>t.name=="V07 FIX | upright Vander true Mixamo walk");
            var gait=((AnimationPlayableAsset)track.GetClips().Single().asset).clip;
            if(!gait || !gait.humanMotion || gait.length<1.45f || gait.length>1.7f)
                throw new InvalidOperationException("P34 walk no longer uses bona fide Humanoid gait.");
            p34.gameObject.SetActive(true);
            director.enabled=true;
            director.RebuildGraph();
            director.time=.8;
            director.Evaluate();
            Vector3 first=walker.position;
            director.time=6.75;
            director.Evaluate();
            Vector3 last=walker.position;
            Vector3 midpoint=(first+last)*.5f;
            float distance=Vector3.Distance(first,last);
            if(distance<2.3f || distance>2.65f)
                throw new InvalidOperationException("P34 corridor expected 2.49m during sampled .8–6.75s, got "+distance);
            Vector3 occluded=camera.transform.position;
            // P35's own screenshot evidence proves THIS global camera location
            // is outside the tall shelf and gets clear sight down the corridor.
            camera.transform.position=p35Camera.transform.position;
            camera.transform.LookAt(midpoint+Vector3.up*1.02f);
            camera.fieldOfView=50f;
            camera.nearClipPlane=.075f;
            report.AppendLine("P34 camera repaired: old bookshelf-obstructed="+occluded.ToString("F3")+
                " new P35-proven-clear location="+camera.transform.position.ToString("F3")+
                " actual P34 midpoint="+midpoint.ToString("F3")+
                " path interval="+distance.ToString("F3")+"m FOV="+camera.fieldOfView);
            EditorSceneManager.MarkSceneDirty(loaded);
            if(!EditorSceneManager.SaveScene(loaded,ScenePath))
                throw new IOException("Versioned v07 SaveScene failed.");
            Directory.CreateDirectory(Screens);
            double[] times={.8,2.35,4.25,6.75};
            for(int i=0;i<times.Length;i++)
            {
                director.time=times[i];
                director.Evaluate();
                var box=visible.bounds;
                var foot=camera.WorldToViewportPoint(new Vector3(box.center.x,box.min.y,box.center.z));
                var head=camera.WorldToViewportPoint(new Vector3(box.center.x,box.max.y,box.center.z));
                string filename=Screens+"P34_UPRIGHT_STRIDE_"+stamps[i]+"_visible_20261029.png";
                Render(camera,filename);
                report.AppendLine("P34 t="+times[i].ToString("F2")+
                    " actor="+walker.position.ToString("F2")+
                    " footViewport="+foot.ToString("F2")+
                    " headViewport="+head.ToString("F2")+
                    " shot="+filename);
                if(foot.z<0 || head.z<0 || foot.y<.03f || head.y>.97f)
                    report.AppendLine("WARNING: frame vertical/outside risk at "+times[i]);
            }
            p34.gameObject.SetActive(false);
            Directory.CreateDirectory(Path.GetDirectoryName(Report));
            File.WriteAllText(Report,report.ToString(),new UTF8Encoding(false));
            Debug.Log("[V07 P34 CAMERA] Saved v07-only camera and FOUR NEW real shot PNGs; visually inspect shelf clearance: "+Report);
        }
        catch(Exception ex){Debug.LogException(ex);Debug.LogError("[V07 P34 CAMERA] Failed, v06 remains untouched.");}
        finally
        {
            if(prior.IsValid()&&prior.isLoaded) SceneManager.SetActiveScene(prior);
            if(loaded.IsValid()&&loaded.isLoaded) EditorSceneManager.CloseScene(loaded,true);
        }
    }
    private static void Render(Camera camera,string filename)
    {
        var prev=RenderTexture.active;
        var target=camera.targetTexture;
        RenderTexture rt=null;
        Texture2D pic=null;
        try
        {
            rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);
            camera.targetTexture=rt;
            camera.Render();
            RenderTexture.active=rt;
            pic=new Texture2D(1280,720,TextureFormat.RGB24,false);
            pic.ReadPixels(new Rect(0,0,1280,720),0,0);
            pic.Apply();
            File.WriteAllBytes(filename,pic.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture=target;
            RenderTexture.active=prev;
            if(pic) UnityEngine.Object.DestroyImmediate(pic);
            if(rt) UnityEngine.Object.DestroyImmediate(rt);
        }
    }
    private static string Hash(string filename)
    {
        if(!File.Exists(filename))return "";
        using(var sha=SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(filename))).Replace("-","");
    }
}
#endif
