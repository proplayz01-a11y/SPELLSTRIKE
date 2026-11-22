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

internal static class PrologueV07Oct29IsolatedFrameQA
{
    const string V07="Assets/Scenes/Cinematics/Prologue_Master_Sequence_P01_42_v07_SpellbookWalkPolish.unity";
    const string V06="Assets/Scenes/Cinematics/Prologue_Master_Sequence_P01_42_v06_DialogueContinuity.unity";
    const string SHA="283573BAA9EB7C9E5D3CEF9290FFDDC3DF1F026B8C8D65EAF6D84A73CB26EBF4";
    const string Pics="Assets/Cinematics/Storyboard3D/Timelines/PrologueMaster_v07_SpellbookWalkPolish/Validation/Screenshots/";
    const string Report="Library/PrologueV07_Oct29IsolatedFrameQA.txt";
    [MenuItem("SPELLSTRIKE/Cinematics/Prologue V07/22 Isolated camera QA Oct29 %#&i")]
    static void CaptureAll()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||
            EditorApplication.isUpdating||SceneManager.sceneCount!=1||
            SceneManager.GetActiveScene().isDirty||
            System.Diagnostics.Process.GetProcessesByName("VALORANT-Win64-Shipping").Length!=0||
            Hash(V06)!=SHA||File.Exists(Report))
        {Debug.LogError("[OCT29 FRAME QA] unsafe editor or report already exists");return;}
        var orig=SceneManager.GetActiveScene();
        Scene scene=default;
        var sb=new StringBuilder();
        try
        {
            scene=EditorSceneManager.OpenScene(V07,OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var group=scene.GetRootGameObjects().Single(g=>
                g.name=="MASTER_SHOT_GROUPS__NUMBERED_IN_STORY_ORDER").transform;
            foreach(Transform child in group) if(child.name.EndsWith("_SHOT_ROOT",StringComparison.Ordinal)) child.gameObject.SetActive(false);
            Directory.CreateDirectory(Pics);
            foreach(int panel in new[]{9,10,12,19,20,21,25,26,29,30})
            {
                var shot=group.Find("P"+panel.ToString("00")+"_SHOT_ROOT");
                if(!shot)throw new InvalidOperationException("Missing P"+panel);
                var director=shot.GetComponentsInChildren<PlayableDirector>(true)
                    .Single(d=>d.playableAsset is TimelineAsset);
                var camera=shot.GetComponentsInChildren<Camera>(true)
                    .First(c=>c.enabled);
                shot.gameObject.SetActive(true);
                director.enabled=true;
                director.RebuildGraph();
                foreach(double second in new[]{.8,4.2,6.8})
                {
                    director.time=second;
                    director.Evaluate();
                    var file=Pics+"P"+panel.ToString("00")+"_OCT29_ISOLATED_t"+second.ToString("F1").Replace('.','_')+".png";
                    if(File.Exists(file))throw new IOException("No screenshot overwrite "+file);
                    CameraRender(camera,file);
                    sb.AppendLine("P"+panel+" time="+second+" screenshot="+file);
                }
                shot.gameObject.SetActive(false);
            }
            var composite=group.Find("P15_P18_SHOT_ROOT");
            var controller=composite.GetComponentsInChildren<PlayableDirector>(true)
                .Single(d=>d.playableAsset is TimelineAsset && d.playableAsset.duration>27.9&&d.playableAsset.duration<28.1);
            var van=composite.GetComponentsInChildren<Animator>(true)
                .Single(a=>a.name=="OLDER_VAN_BLOCKING_RIG_VISUAL_ONLY");
            var vander=composite.GetComponentsInChildren<Animator>(true)
                .Single(a=>a.name=="VanderPoel_VisualRig_Mixamo" &&
                    a.transform.parent.name=="VANDER_POEL_BLOCKING_RIG_VISUAL_ONLY");
            var cameras=composite.GetComponentsInChildren<Camera>(true);
            composite.gameObject.SetActive(true);
            controller.enabled=true;
            controller.RebuildGraph();
            foreach(double time in new[]{2d,5d,8d,11d,14d,17.6d,20d,23d,26d})
            {
                controller.time=time;
                controller.Evaluate();
                var vanP=van.transform.position;
                var vanderP=vander.transform.position;
                var vanF=Vector3.ProjectOnPlane(van.transform.forward,Vector3.up).normalized;
                var vanderF=Vector3.ProjectOnPlane(vander.transform.forward,Vector3.up).normalized;
                controller.time=time+.2;
                controller.Evaluate();
                var vanVel=van.transform.position-vanP;
                var vanderVel=vander.transform.position-vanderP;
                sb.AppendLine("COMPOSITE t="+time.ToString("F1")+
                    " VAN pos="+vanP+" delta="+vanVel+" forward="+vanF+
                    " dot="+Vector3.Dot(vanF,vanVel.normalized).ToString("F3")+
                    " VANDER pos="+vanderP+" delta="+vanderVel+" forward="+vanderF+
                    " dot="+Vector3.Dot(vanderF,vanderVel.normalized).ToString("F3"));
                controller.time=time;
                controller.Evaluate();
                var c=cameras.FirstOrDefault(cam=>cam.gameObject.activeInHierarchy&&cam.enabled)??cameras.First();
                var file=Pics+"P15_18_OCT29_ISOLATED_t"+time.ToString("F1").Replace('.','_')+".png";
                if(File.Exists(file))throw new IOException("No overwrite "+file);
                CameraRender(c,file);
                sb.AppendLine("COMPOSITE_CAMERA "+c.name+" screenshot="+file);
            }
            composite.gameObject.SetActive(false);
            Directory.CreateDirectory("Library");
            File.WriteAllText(Report,sb.ToString(),new UTF8Encoding(false));
            Debug.Log("[OCT29 FRAME QA] 39 timed camera captures + composite direction dot report "+Report);
        }
        catch(Exception ex){Debug.LogException(ex);}
        finally
        {
            if(orig.IsValid()&&orig.isLoaded)SceneManager.SetActiveScene(orig);
            if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);
        }
    }
    static void CameraRender(Camera camera,string path)
    {
        var prior=camera.targetTexture;var prev=RenderTexture.active;
        RenderTexture rt=null;Texture2D png=null;
        try
        {
            rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);
            camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            png=new Texture2D(1280,720,TextureFormat.RGB24,false);
            png.ReadPixels(new Rect(0,0,1280,720),0,0);png.Apply();
            File.WriteAllBytes(path,png.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture=prior;RenderTexture.active=prev;
            if(png)UnityEngine.Object.DestroyImmediate(png);
            if(rt)UnityEngine.Object.DestroyImmediate(rt);
        }
    }
    static string Hash(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","");}
}
#endif
