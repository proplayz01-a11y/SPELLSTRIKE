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
/// Read-only candidate-camera renders on the saved v07 shot, to solve P34
/// foreground-statue/bookshelf occlusion. Does not save scene or assets.
/// </summary>
internal static class PrologueV07P34CameraSurvey_20261029
{
    private const string ScenePath = "Assets/Scenes/Cinematics/Prologue_Master_Sequence_P01_42_v07_SpellbookWalkPolish.unity";
    private const string Protected = "Assets/Scenes/Cinematics/Prologue_Master_Sequence_P01_42_v06_DialogueContinuity.unity";
    private const string Sha = "283573BAA9EB7C9E5D3CEF9290FFDDC3DF1F026B8C8D65EAF6D84A73CB26EBF4";
    private const string Output = "Assets/Cinematics/Storyboard3D/Timelines/PrologueMaster_v07_SpellbookWalkPolish/Validation/Screenshots/";
    private const string Report = "Library/PrologueV07_P34_CameraSurvey_20261029.txt";

    [MenuItem("SPELLSTRIKE/Cinematics/Prologue V07/15 SURVEY P34 clear angles actual actor (14 camera PNGs)")]
    private static void Survey()
    {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.sceneCount!=1 ||
            SceneManager.GetActiveScene().isDirty ||
            System.Diagnostics.Process.GetProcessesByName("VALORANT-Win64-Shipping").Length!=0 ||
            Hash(Protected)!=Sha)
        { Debug.LogError("[V07 CAMERA SURVEY] Unsafe state/v06 mismatch."); return; }
        // Candidate positions are placed away from the existing large book
        // table, twin bookshelf walls and prominent foreground statues.
        var positions = new[] {
            new Vector3(-3.5f,1.55f,3.05f),
            new Vector3(-5.0f,1.75f,1.0f),
            new Vector3(4.0f,1.75f,1.0f),
            new Vector3(0.0f,1.75f,5.0f),
            new Vector3(4.0f,1.75f,-3.7f),
            new Vector3(-4.0f,1.75f,-3.7f),
            new Vector3(-2.5f,1.75f,5.0f)
        };
        for(int i=0;i<positions.Length;i++)
            foreach(var time in new[]{"t04","t07"})
                if(File.Exists(Filename(i,time)))
                { Debug.LogError("[V07 CAMERA SURVEY] Output already exists, no overwrite "+Filename(i,time)); return; }
        var active=SceneManager.GetActiveScene();
        Scene scene=default;
        var report=new StringBuilder();
        try
        {
            scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var root=scene.GetRootGameObjects().Single(r=>
                r.name=="MASTER_SHOT_GROUPS__NUMBERED_IN_STORY_ORDER");
            var shot=root.transform.Find("P34_SHOT_ROOT");
            var actor=shot.Find("V07_UPRIGHT_VANDER_WALK_PATH")
                .Find("V07_STANDING_VANDER_REAL_WALK_VISIBLE");
            var body=actor.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(r=>r.sharedMesh!=null);
            var director=shot.GetComponentsInChildren<PlayableDirector>(true)
                .Single(d=>d.playableAsset is TimelineAsset);
            var camera=shot.GetComponentsInChildren<Camera>(true)
                .Single(c=>c.name=="P34_CORRIDOR_STATIC_CAMERA");
            shot.gameObject.SetActive(true);
            director.enabled=true;
            director.RebuildGraph();
            // Actual evaluated actor world z travels about -.23 to -2.72.
            var t4=4.25;
            director.time=t4;
            director.Evaluate();
            Vector3 center=body.bounds.center;
            var aim=new Vector3(center.x,center.y,center.z-.10f);
            report.AppendLine("Real P34 t4 center="+center+" actor="+actor.position);
            Directory.CreateDirectory(Output);
            for(int i=0;i<positions.Length;i++)
            {
                camera.transform.position=positions[i];
                camera.transform.LookAt(aim);
                camera.fieldOfView=70f;
                camera.nearClipPlane=.075f;
                foreach(var instant in new[]{new {name="t04",seconds=4.25},new{name="t07",seconds=6.75}})
                {
                    director.time=instant.seconds;
                    director.Evaluate();
                    var b=body.bounds;
                    Vector3 foot=camera.WorldToViewportPoint(new Vector3(b.center.x,b.min.y,b.center.z));
                    Vector3 head=camera.WorldToViewportPoint(new Vector3(b.center.x,b.max.y,b.center.z));
                    var filename=Filename(i,instant.name);
                    Capture(camera,filename);
                    report.AppendLine("candidate "+(char)('A'+i)+" "+instant.name+
                        " camera="+positions[i]+" aim="+aim+
                        " actor="+actor.position+" foot="+foot+" head="+head+
                        " png="+filename);
                }
            }
            shot.gameObject.SetActive(false);
            Directory.CreateDirectory(Path.GetDirectoryName(Report));
            File.WriteAllText(Report,report.ToString(),new UTF8Encoding(false));
            Debug.Log("[V07 CAMERA SURVEY] Captured 14 different P34 frames, v07 scene NOT SAVED. "+Report);
        }
        catch(Exception ex){Debug.LogException(ex);Debug.LogError("[V07 CAMERA SURVEY] Failed");}
        finally
        {
            if(active.IsValid() && active.isLoaded)SceneManager.SetActiveScene(active);
            if(scene.IsValid() && scene.isLoaded)EditorSceneManager.CloseScene(scene,true);
        }
    }
    private static string Filename(int i,string time) =>
        Output+"P34_CAM_"+(char)('A'+i)+"_"+time+"_20261029.png";
    private static void Capture(Camera cam,string output)
    {
        var old=cam.targetTexture;
        var previous=RenderTexture.active;
        RenderTexture rt=null;
        Texture2D img=null;
        try
        {
            rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);
            cam.targetTexture=rt;
            cam.Render();
            RenderTexture.active=rt;
            img=new Texture2D(1280,720,TextureFormat.RGB24,false);
            img.ReadPixels(new Rect(0,0,1280,720),0,0);
            img.Apply();
            File.WriteAllBytes(output,img.EncodeToPNG());
        }
        finally
        {
            cam.targetTexture=old;
            RenderTexture.active=previous;
            if(img)UnityEngine.Object.DestroyImmediate(img);
            if(rt)UnityEngine.Object.DestroyImmediate(rt);
        }
    }
    private static string Hash(string path)
    {
        if(!File.Exists(path))return "";
        using(var sha=SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","");
    }
}
#endif
