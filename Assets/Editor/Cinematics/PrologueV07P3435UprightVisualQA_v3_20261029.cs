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

/// <summary>Independent camera-space validation: P34/35 new visible walker + staff.</summary>
internal static class PrologueV07P3435UprightVisualQA_v3_20261029
{
    private const string ScenePath = "Assets/Scenes/Cinematics/Prologue_Master_Sequence_P01_42_v07_SpellbookWalkPolish.unity";
    private const string Protected = "Assets/Scenes/Cinematics/Prologue_Master_Sequence_P01_42_v06_DialogueContinuity.unity";
    private const string Expected = "283573BAA9EB7C9E5D3CEF9290FFDDC3DF1F026B8C8D65EAF6D84A73CB26EBF4";
    private const string Pics = "Assets/Cinematics/Storyboard3D/Timelines/PrologueMaster_v07_SpellbookWalkPolish/Validation/Screenshots/";
    private const string Report = "Library/PrologueV07_P3435_Upright_VisualQA_v3_20261029.txt";

    [MenuItem("SPELLSTRIKE/Cinematics/Prologue V07/12 CAPTURE HUMANOID baked walk v3 (8 new screenshots) %#&o")]
    private static void CaptureAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            EditorApplication.isUpdating || SceneManager.sceneCount != 1 ||
            SceneManager.GetActiveScene().isDirty ||
            System.Diagnostics.Process.GetProcessesByName("VALORANT-Win64-Shipping").Length > 0 ||
            Hash(Protected) != Expected)
        {
            Debug.LogError("[V07 UPRIGHT QA V3] Unsafe Editor state or v06 changed.");
            return;
        }
        foreach(int p in new[] {34,35})
            foreach(string name in new[] {"t01","t02","t04","t07"})
                if(File.Exists(Pics+"P"+p+"_UPRIGHT_STRIDE_"+name+"_baked_20261029.png"))
                {
                    Debug.LogError("[V07 UPRIGHT QA V3] Existing screenshot: P"+p+" "+name+". No overwrite.");
                    return;
                }
        Scene target = default;
        var active = SceneManager.GetActiveScene();
        var log = new StringBuilder();
        try
        {
            target = EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
            SceneManager.SetActiveScene(target);
            var group=target.GetRootGameObjects().Single(g=>
                g.name=="MASTER_SHOT_GROUPS__NUMBERED_IN_STORY_ORDER").transform;
            Directory.CreateDirectory(Pics);
            foreach(int panel in new[]{34,35})
            {
                var shot=group.Find("P"+panel+"_SHOT_ROOT");
                var director=shot.GetComponentsInChildren<PlayableDirector>(true)
                    .Single(d=>d.playableAsset is TimelineAsset);
                var timeline=(TimelineAsset)director.playableAsset;
                var parent=shot.Find("V07_UPRIGHT_VANDER_WALK_PATH");
                var actor=parent ? parent.Find("V07_STANDING_VANDER_REAL_WALK_VISIBLE") : null;
                if(!actor) throw new InvalidOperationException("Missing P"+panel+" visible standing walker");
                var rig=actor.GetComponent<Animator>();
                var body=actor.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .Single(r=>r.sharedMesh != null);
                var camera=shot.GetComponentsInChildren<Camera>(true)
                    .Single(c=>c.name=="P"+panel+"_CORRIDOR_STATIC_CAMERA");
                var gaitTrack=timeline.GetOutputTracks().OfType<AnimationTrack>()
                    .Single(t=>t.name=="V07 FIX | upright Vander true Mixamo walk");
                var pathTrack=timeline.GetOutputTracks().OfType<AnimationTrack>()
                    .Single(t=>t.name=="V07 FIX | corridor position (independent root)");
                var realClip=((AnimationPlayableAsset)gaitTrack.GetClips().Single().asset).clip;
                if(!rig.avatar || !rig.avatar.isHuman || AssetDatabase.GetAssetPath(realClip) != "Assets/Cinematics/Storyboard3D/Timelines/PrologueMaster_v07_SpellbookWalkPolish/VanderPoel_Walking_HumanoidRootBaked_v07.fbx" ||
                    Math.Abs(realClip.length-1.566667f)>.14f ||
                    director.GetGenericBinding(gaitTrack)!=rig ||
                    director.GetGenericBinding(pathTrack)!=parent.GetComponent<Animator>() ||
                    rig.applyRootMotion)
                    throw new InvalidOperationException("P"+panel+" genuine walk/Animator binding invalid.");
                var old=shot.GetComponentsInChildren<Animator>(true)
                    .Single(a=>a.name=="P10_VanderPoel_ANIMATED");
                var seated=old.GetComponentsInChildren<Renderer>(true);
                if(seated.Any(r=>r.enabled))
                    throw new InvalidOperationException("P"+panel+" seated old model still visible.");
                if(!body.enabled)
                    throw new InvalidOperationException("P"+panel+" upright walker renderer disabled.");
                if(panel==35)
                {
                    var carried=actor.GetComponentsInChildren<Renderer>(true)
                        .Where(r=>r.transform.GetComponentsInParent<Transform>(true)
                            .Any(t=>t.name=="P35_V07_VANDER_CARRIES_CANONICAL_VAN_STAFF"))
                        .ToArray();
                    if(carried.Length==0 || carried.Any(r=>!r.enabled))
                        throw new InvalidOperationException("P35 staff missing or hidden.");
                }
                log.AppendLine("P"+panel+" REAL Humanoid walk "+realClip.length.ToString("F4")+
                    " seconds, old seated renderers="+seated.Length+
                    ", source mesh="+body.sharedMesh.name+", camera="+camera.transform.position.ToString("F2"));
                shot.gameObject.SetActive(true);
                director.enabled=true;
                director.RebuildGraph();
                var times=new[]{.8,2.35,4.25,6.75};
                var marks=new[]{"t01","t02","t04","t07"};
                Quaternion previousRight=Quaternion.identity,previousLeft=Quaternion.identity;
                var bones=body.bones;
                var left=bones.Single(b=>b.name=="mixamorig:LeftUpLeg");
                var right=bones.Single(b=>b.name=="mixamorig:RightUpLeg");
                for(int i=0;i<times.Length;i++)
                {
                    director.time=times[i];
                    director.Evaluate();
                    var box=body.bounds;
                    var foot=camera.WorldToViewportPoint(new Vector3(box.center.x,box.min.y,box.center.z));
                    var head=camera.WorldToViewportPoint(new Vector3(box.center.x,box.max.y,box.center.z));
                    var pos=actor.position;
                    var filename=Pics+"P"+panel+"_UPRIGHT_STRIDE_"+marks[i]+"_baked_20261029.png";
                    Render(camera,filename);
                    log.AppendLine("P"+panel+" t="+times[i].ToString("F2")+
                        " actor="+pos.ToString("F2")+" height="+box.size.y.ToString("F2")+
                        " foot="+foot.ToString("F2")+" head="+head.ToString("F2")+
                        " leftThigh="+left.localRotation.eulerAngles.ToString("F1")+
                        " rightThigh="+right.localRotation.eulerAngles.ToString("F1")+
                        " screenshot="+filename);
                    if(foot.z<=0 || head.z<=0 || foot.y<.02f || head.y>.98f)
                        log.AppendLine("P"+panel+" t="+times[i]+" FRAMING RISK: see image; do not approve blindly");
                    if(i>0)
                        log.AppendLine("  legDelta="+Quaternion.Angle(previousLeft,left.localRotation).ToString("F2")+
                            " / "+Quaternion.Angle(previousRight,right.localRotation).ToString("F2")+" degrees");
                    previousLeft=left.localRotation;
                    previousRight=right.localRotation;
                }
                shot.gameObject.SetActive(false);
            }
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Report));
            File.WriteAllText(Report,log.ToString(),new UTF8Encoding(false));
            Debug.Log("[V07 UPRIGHT QA V3] 8 NEW FULL-BODY CAMERA PNGs captured; examine images BEFORE accepting! "+Report);
        }
        catch(Exception ex) { Debug.LogException(ex); Debug.LogError("[V07 UPRIGHT QA V3] Failed, see exception."); }
        finally
        {
            if(active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
            if(target.IsValid() && target.isLoaded) EditorSceneManager.CloseScene(target,true);
        }
    }
    private static void Render(Camera camera,string output)
    {
        RenderTexture prev=RenderTexture.active;
        RenderTexture rt=null;
        Texture2D texture=null;
        var target=camera.targetTexture;
        try
        {
            rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);
            camera.targetTexture=rt;
            camera.Render();
            RenderTexture.active=rt;
            texture=new Texture2D(1280,720,TextureFormat.RGB24,false);
            texture.ReadPixels(new Rect(0,0,1280,720),0,0);
            texture.Apply();
            File.WriteAllBytes(output,texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture=target;
            RenderTexture.active=prev;
            if(texture) UnityEngine.Object.DestroyImmediate(texture);
            if(rt) UnityEngine.Object.DestroyImmediate(rt);
        }
    }
    private static string Hash(string path)
    {
        using(var sha=SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","");
    }
}
#endif
