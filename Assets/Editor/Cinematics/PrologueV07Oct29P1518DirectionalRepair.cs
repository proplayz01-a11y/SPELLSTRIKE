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

internal static class PrologueV07Oct29P1518DirectionalRepair
{
    const string ScenePath="Assets/Scenes/Cinematics/Prologue_Master_Sequence_P01_42_v07_SpellbookWalkPolish.unity";
    const string V06="Assets/Scenes/Cinematics/Prologue_Master_Sequence_P01_42_v06_DialogueContinuity.unity";
    const string SHA="283573BAA9EB7C9E5D3CEF9290FFDDC3DF1F026B8C8D65EAF6D84A73CB26EBF4";
    const string Dir="Assets/Cinematics/Storyboard3D/Timelines/PrologueMaster_v07_SpellbookWalkPolish/";
    const string Report="Library/PrologueV07_Oct29_P1518DirectionalRepair.txt";
    [MenuItem("SPELLSTRIKE/Cinematics/Prologue V07/21 Fix P17 shelf and P18 return facing direction")]
    static void Fix()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||
            EditorApplication.isUpdating||SceneManager.sceneCount!=1||SceneManager.GetActiveScene().isDirty||
            System.Diagnostics.Process.GetProcessesByName("VALORANT-Win64-Shipping").Length!=0||
            Hash(V06)!=SHA||File.Exists(Report))
        {Debug.LogError("[V07 DIRECTION] unsafe editor or repair already run");return;}
        var active=SceneManager.GetActiveScene();
        Scene scene=default;
        var sb=new StringBuilder();
        try
        {
            scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var root=scene.GetRootGameObjects().Single(x=>x.name=="MASTER_SHOT_GROUPS__NUMBERED_IN_STORY_ORDER").transform;
            var shot=root.Find("P15_P18_SHOT_ROOT");
            var director=shot.GetComponentsInChildren<PlayableDirector>(true)
                .Single(d=>d.playableAsset is TimelineAsset&&d.playableAsset.duration>27.9&&d.playableAsset.duration<28.1);
            var timeline=(TimelineAsset)director.playableAsset;
            if(!AssetDatabase.GetAssetPath(timeline).StartsWith(Dir,StringComparison.Ordinal))
                throw new InvalidOperationException("P15–18 not on independent v07 Timeline");
            var assetV=AssetDatabase.LoadAssetAtPath<AnimationClip>(Dir+"Block_Vander_Path_Approved_v02_IndependentV07.anim");
            var assetVan=AssetDatabase.LoadAssetAtPath<AnimationClip>(Dir+"Block_Van_Path_Approved_v02_IndependentV07.anim");
            if(!assetV||!assetVan)throw new InvalidOperationException("Missing two independent v07 movement curves");
            var vander=shot.GetComponentsInChildren<Animator>(true).Single(a=>
                a.name=="VanderPoel_VisualRig_Mixamo"&&a.transform.parent.name=="VANDER_POEL_BLOCKING_RIG_VISUAL_ONLY");
            var van=shot.GetComponentsInChildren<Animator>(true).Single(a=>
                a.name=="OLDER_VAN_BLOCKING_RIG_VISUAL_ONLY");
            var vanRoot=timeline.GetOutputTracks().OfType<AnimationTrack>()
                .Single(t=>t.name=="Van BLOCKED MOTION (follow -> return)");
            var vanderRoot=timeline.GetOutputTracks().OfType<AnimationTrack>()
                .Single(t=>t.name=="Vander BLOCKED MOTION (study -> shelf -> study)");
            if(((AnimationPlayableAsset)vanRoot.GetClips().Single().asset).clip!=assetVan||
               ((AnimationPlayableAsset)vanderRoot.GetClips().Single().asset).clip!=assetV)
                throw new InvalidOperationException("P15 scene no longer uses approved v07 path clips");
            Directory.CreateDirectory("Library");
            foreach(var pair in new[]{(assetVan,"Van"),(assetV,"Vander")})
            {
                string source=AssetDatabase.GetAssetPath(pair.Item1);
                string backup="Library/Oct29_BeforeDirection_"+pair.Item2+".anim.bak";
                if(File.Exists(backup))throw new IOException("Backup already exists "+backup);
                File.Copy(source,backup);
                sb.AppendLine("BACKUP "+backup);
            }
            CorrectPath(assetV,true);
            CorrectPath(assetVan,false);
            // The composite includes historical seated *stand-in* figures in
            // addition to the separately animated walking actors. These must
            // not remain suspended or duplicate the moving actors on camera.
            foreach(var t in shot.GetComponentsInChildren<Transform>(true).Where(t=>
                t.name=="P10_OlderVan_ANIMATED"||t.name=="P10_VanderPoel_ANIMATED"))
            {
                int hidden=0;
                foreach(var r in t.GetComponentsInChildren<Renderer>(true))
                    if(r.enabled){r.enabled=false;hidden++;}
                sb.AppendLine("HIDE COMPOSITE ONLY static seated stand-in "+t.name+" count="+hidden);
            }
            shot.gameObject.SetActive(true);
            director.enabled=true;
            director.RebuildGraph();
            var cams=shot.GetComponentsInChildren<Camera>(true);
            Directory.CreateDirectory(Dir+"Validation/Screenshots");
            foreach(var second in new[]{5d,8d,14d,17.6d,18.4d,20d,23d,26d})
            {
                director.time=second;
                director.Evaluate();
                var vPos=vander.transform.position;
                var vanPos=van.transform.position;
                var vf=Vector3.ProjectOnPlane(vander.transform.forward,Vector3.up).normalized;
                var f=Vector3.ProjectOnPlane(van.transform.forward,Vector3.up).normalized;
                director.time=second+.15;
                director.Evaluate();
                var vd=vander.transform.position-vPos;
                var d=van.transform.position-vanPos;
                sb.AppendLine("t="+second.ToString("F1")+" VANDER rootDelta="+vd+
                    " headingDot="+Vector3.Dot(vf,vd.normalized).ToString("F3")+
                    " VAN rootDelta="+d+" headingDot="+Vector3.Dot(f,d.normalized).ToString("F3"));
                director.time=second;
                director.Evaluate();
                var cam=cams.FirstOrDefault(c=>c.enabled&&c.gameObject.activeInHierarchy)??cams.First();
                var filename=Dir+"Validation/Screenshots/P15_P18_DIRECTION_FIXED_t"+
                    second.ToString("F1").Replace('.','_')+"_20261029.png";
                if(File.Exists(filename))throw new IOException("Refuse screenshot overwrite "+filename);
                Capture(cam,filename);
                sb.AppendLine(" FRAME "+cam.name+" "+filename);
            }
            shot.gameObject.SetActive(false);
            EditorUtility.SetDirty(timeline);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene,ScenePath))
                throw new IOException("Unable to save v07 scene");
            File.WriteAllText(Report,sb.ToString(),new UTF8Encoding(false));
            Debug.Log("[V07 DIRECTION] P16 outbound/P17 shelf/P18 return reoriented with new timed video PNGs; INSPECT frames "+Report);
        }
        catch(Exception ex){Debug.LogException(ex);}
        finally
        {
            if(active.IsValid()&&active.isLoaded)SceneManager.SetActiveScene(active);
            if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);
        }
    }
    static void CorrectPath(AnimationClip clip,bool isVander)
    {
        var bindings=new[]{"m_LocalRotation.x","m_LocalRotation.y","m_LocalRotation.z","m_LocalRotation.w"}
            .Select(s=>EditorCurveBinding.FloatCurve("",typeof(Transform),s)).ToArray();
        var old=bindings.Select(b=>AnimationUtility.GetEditorCurve(clip,b)).ToArray();
        if(old.Any(c=>c==null))throw new InvalidOperationException("Missing quaternion component "+clip.name);
        // Original animation and motion fully preserved through P16. Starting
        // with P17/P18 adjust only the turn and return facing, not speed or
        // RootT; the earlier opposite-facing slide was due to late 18.1s yaw.
        float[] time=isVander?
            new[]{0f,3.2f,4.6f,6f,12.7f,13.7f,15.5f,17.2f,18.1f,20f,23f,28f}:
            new[]{0f,3.2f,4.6f,6f,13.1f,16.6f,17.5f,19.9f,23f,25.2f,28f};
        var curves=new AnimationCurve[4];
        for(int i=0;i<4;i++)curves[i]=new AnimationCurve();
        foreach(float t in time)
        {
            Quaternion q;
            if(isVander)
            {
                if(t<=12.7f) q=Read(old,t);
                else if(t<13.7f)q=Quaternion.Slerp(Read(old,12.7f),Quaternion.identity,(t-12.7f)/1f);
                else q=Quaternion.identity;
            }
            else
            {
                if(t<=16.6f)q=Read(old,t);
                else if(t<17.5f)q=Quaternion.Slerp(Read(old,16.6f),Quaternion.Euler(0,33,0),(t-16.6f)/.9f);
                else if(t<19.9f)q=Quaternion.Slerp(Quaternion.Euler(0,33,0),Quaternion.Euler(0,-45,0),(t-17.5f)/2.4f);
                else if(t<25.2f)q=Quaternion.Euler(0,-45,0);
                else q=Quaternion.identity;
            }
            curves[0].AddKey(t,q.x);curves[1].AddKey(t,q.y);
            curves[2].AddKey(t,q.z);curves[3].AddKey(t,q.w);
        }
        for(int i=0;i<4;i++)AnimationUtility.SetEditorCurve(clip,bindings[i],curves[i]);
        EditorUtility.SetDirty(clip);
    }
    static Quaternion Read(AnimationCurve[] curves,float t)
    {
        var q=new Quaternion(curves[0].Evaluate(t),curves[1].Evaluate(t),
            curves[2].Evaluate(t),curves[3].Evaluate(t));
        return Quaternion.Normalize(q);
    }
    static void Capture(Camera camera,string filename)
    {
        var prev=camera.targetTexture;
        var old=RenderTexture.active;
        RenderTexture rt=null;Texture2D pic=null;
        try
        {
            rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);
            camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            pic=new Texture2D(1280,720,TextureFormat.RGB24,false);
            pic.ReadPixels(new Rect(0,0,1280,720),0,0);pic.Apply();
            File.WriteAllBytes(filename,pic.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture=prev;RenderTexture.active=old;
            if(pic)UnityEngine.Object.DestroyImmediate(pic);
            if(rt)UnityEngine.Object.DestroyImmediate(rt);
        }
    }
    static string Hash(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","");}
}
#endif
