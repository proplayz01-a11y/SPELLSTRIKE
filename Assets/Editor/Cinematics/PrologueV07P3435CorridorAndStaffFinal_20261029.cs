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
/// V07-only P34 obstructed-camera correction and P35 staff stabilization.
/// Restores the proven visible P34 corridor composition and relocates the
/// 3m walk *inside* that corridor. P35 staff is held as a root-relative carry
/// prop so the animation's swinging hand cannot turn the staff horizontal.
/// Samples true shot cameras and writes fresh non-overwriting PNG evidence.
/// </summary>
internal static class PrologueV07P3435CorridorAndStaffFinal_20261029
{
    private const string ScenePath = "Assets/Scenes/Cinematics/Prologue_Master_Sequence_P01_42_v07_SpellbookWalkPolish.unity";
    private const string V06 = "Assets/Scenes/Cinematics/Prologue_Master_Sequence_P01_42_v06_DialogueContinuity.unity";
    private const string Backup = "RecoveryBackups/ProloguePolishV07_20261028_Immutable_v01/Prologue_Master_v06_BEFORE_POLISH.unity";
    private const string V06Hash = "283573BAA9EB7C9E5D3CEF9290FFDDC3DF1F026B8C8D65EAF6D84A73CB26EBF4";
    private const string Folder = "Assets/Cinematics/Storyboard3D/Timelines/PrologueMaster_v07_SpellbookWalkPolish/";
    private const string Evidence = Folder + "Validation/Screenshots/";
    private const string Report = "Library/PrologueV07_P3435_CorridorAndStaff_v4_20261029.txt";
    private const string GaitTrack = "V07 FIX | upright Vander true Mixamo walk";

    [MenuItem("SPELLSTRIKE/Cinematics/Prologue V07/14 REPAIR P34 corridor + stabilize P35 staff and render v4")]
    private static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isUpdating ||
            EditorApplication.isCompiling || SceneManager.sceneCount != 1 ||
            SceneManager.GetActiveScene().isDirty ||
            System.Diagnostics.Process.GetProcessesByName("VALORANT-Win64-Shipping").Length != 0 ||
            Hash(V06) != V06Hash || Hash(Backup) != V06Hash ||
            AssetDatabase.AssetPathToGUID(ScenePath) != "12405e3bd39d43d398685e74678a8516")
        { Debug.LogError("[V07 P3435 FINAL] Unsafe editor, VALORANT present or protected v06 mismatch"); return; }
        foreach (var p in new[] {34, 35})
            foreach(var suffix in new[] {"t01","t02","t04","t07"})
            {
                if(File.Exists(ImagePath(p,suffix)))
                { Debug.LogError("[V07 P3435 FINAL] Screenshot already exists; no overwrite: "+ImagePath(p,suffix)); return; }
            }
        Scene loaded = default;
        var active = SceneManager.GetActiveScene();
        var report = new StringBuilder();
        try
        {
            loaded=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
            SceneManager.SetActiveScene(loaded);
            var master=loaded.GetRootGameObjects().Single(g=>
                g.name=="MASTER_SHOT_GROUPS__NUMBERED_IN_STORY_ORDER").transform;
            var s34=master.Find("P34_SHOT_ROOT");
            var s35=master.Find("P35_SHOT_ROOT");
            if(!s34 || !s35) throw new InvalidOperationException("P34/35 missing");
            var d34=Director(s34);
            var d35=Director(s35);
            var a34=Actor(s34);
            var a35=Actor(s35);
            var c34=Camera(s34,34);
            var c35=Camera(s35,35);
            Validate(d34, a34, 34);
            Validate(d35, a35, 35);
            var anim34=((TimelineAsset)d34.playableAsset).GetOutputTracks().OfType<AnimationTrack>()
                .Single(t=>t.name=="V07 FIX | corridor position (independent root)");
            var pathAsset=(AnimationPlayableAsset)anim34.GetClips().Single().asset;
            if(!pathAsset.clip || AssetDatabase.GetAssetPath(pathAsset.clip)!=
                Folder+"P34_V07_Corridor3m_UprightStride.anim")
                throw new InvalidOperationException("P34 versioned corridor motion modified unexpectedly");
            var binding=EditorCurveBinding.FloatCurve("",typeof(Transform),"m_LocalPosition.z");
            var zCurve=AnimationUtility.GetEditorCurve(pathAsset.clip,binding);
            if(zCurve==null || zCurve.length<4)
                throw new InvalidOperationException("P34 path has no independent Z curve");
            var keys=zCurve.keys;
            // Existing P34 path is shot-local z=4.15 down to z=1.15,
            // even though the evaluated walker is world z=-.23 to -2.72.
            // That offset comes from the shot's existing parent transform.
            foreach(var key in keys)
                if(key.value>4.3f || key.value<1.0f)
                    throw new InvalidOperationException("P34 path already shifted or unexpected Z coordinate");
            // Place P34 feet on exactly the previously proven visible aisle
            // (the old shot rendered a full-body Vander from z~.5 to 5.1).
            for(int i=0;i<keys.Length;i++) keys[i].value+=4.0f;
            zCurve.keys=keys;
            AnimationUtility.SetEditorCurve(pathAsset.clip,binding,zCurve);
            EditorUtility.SetDirty(pathAsset.clip);
            // This P34 camera position and direction already rendered the
            // complete upright model with zero occlusion in the first QA.
            c34.transform.position=new Vector3(-3.5f,1.36f,3.05f);
            c34.transform.LookAt(new Vector3(0,1.0f,2.65f));
            c34.fieldOfView=58f;
            c34.nearClipPlane=.075f;
            report.AppendLine("P34 path +4.0m local Z into photographed clear tiled aisle; "+
                "camera restored proven (-3.5,1.36,3.05) facing (0,1,2.65) FOV58");

            s35.gameObject.SetActive(true);
            d35.enabled=true;
            d35.RebuildGraph();
            d35.time=.8;
            d35.Evaluate();
            var hand=a35.GetComponentsInChildren<Transform>(true)
                .Single(x=>x.name=="mixamorig:RightHand");
            var staff=hand.GetComponentsInChildren<Transform>(true)
                .Single(x=>x.name=="P35_V07_VANDER_CARRIES_CANONICAL_VAN_STAFF");
            var renderers=staff.GetComponentsInChildren<Renderer>(true);
            if(renderers.Length!=1 || !renderers[0].enabled)
                throw new InvalidOperationException("P35 staff source lost renderer");
            // At t=.8 old render showed staff held upright. Preserve that
            // pose relative to the walker (not the swinging hand) through
            // all phases of the 1.5667s gait cycle.
            var stable=new GameObject("P35_V07_STAFF_CARRY_STABILIZER");
            stable.transform.SetParent(a35.transform,false);
            stable.transform.localPosition=Vector3.zero;
            stable.transform.localRotation=Quaternion.identity;
            staff.SetParent(stable.transform,true);
            var rod=staff.GetComponentsInChildren<Renderer>(true)[0];
            var direction=rod.bounds.center-hand.position;
            if(direction.magnitude<.05f)
                throw new InvalidOperationException("Could not infer staff world orientation");
            // Existing t01 clip has near-vertical staff; enforce upright
            // while keeping its grip just inside the actor's right side.
            var desired=hand.position+Vector3.up*.45f;
            staff.rotation=Quaternion.FromToRotation(direction.normalized,Vector3.up)*staff.rotation;
            staff.position+=desired-rod.bounds.center;
            report.AppendLine("P35 original staff stabilized to non-hand-animated actor root at first sample; "+
                "right-hand offset="+Vector3.Distance(hand.position,staff.position).ToString("F3")+
                "m; frame-to-frame horizontal hand swing cannot roll staff");
            s35.gameObject.SetActive(false);

            EditorSceneManager.MarkSceneDirty(loaded);
            if(!EditorSceneManager.SaveScene(loaded,ScenePath))
                throw new IOException("Failed to save versioned v07 scene");
            Directory.CreateDirectory(Evidence);
            for(int p=34;p<=35;p++)
            {
                var shot=p==34?s34:s35;
                var director=p==34?d34:d35;
                var camera=p==34?c34:c35;
                var actor=p==34?a34:a35;
                var body=actor.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .Single(x=>x.sharedMesh!=null);
                var left=body.bones.Single(b=>b.name=="mixamorig:LeftUpLeg");
                var right=body.bones.Single(b=>b.name=="mixamorig:RightUpLeg");
                shot.gameObject.SetActive(true);
                director.enabled=true;
                director.RebuildGraph();
                var times=new[]{.8,2.35,4.25,6.75};
                var moments=new[]{"t01","t02","t04","t07"};
                var first=Vector3.zero;
                Quaternion leftFirst=Quaternion.identity,rightFirst=Quaternion.identity;
                for(int i=0;i<4;i++)
                {
                    director.time=times[i];
                    director.Evaluate();
                    var bounds=body.bounds;
                    var foot=camera.WorldToViewportPoint(new Vector3(bounds.center.x,bounds.min.y,bounds.center.z));
                    var head=camera.WorldToViewportPoint(new Vector3(bounds.center.x,bounds.max.y,bounds.center.z));
                    if(i==0){first=actor.transform.position;leftFirst=left.localRotation;rightFirst=right.localRotation;}
                    Capture(camera,ImagePath(p,moments[i]));
                    report.AppendLine("P"+p+" t="+times[i].ToString("F2")+
                        " visibleActor="+actor.transform.position.ToString("F3")+
                        " foot="+foot.ToString("F3")+" head="+head.ToString("F3")+
                        " leftDiff="+Quaternion.Angle(leftFirst,left.localRotation).ToString("F1")+
                        " rightDiff="+Quaternion.Angle(rightFirst,right.localRotation).ToString("F1")+
                        " PNG="+ImagePath(p,moments[i]));
                    if(foot.z<=0 || head.z<=0 || foot.y<.01f || head.y>.99f)
                        report.AppendLine("WARNING FRAMING: P"+p+" t="+times[i]+" inspect render");
                }
                report.AppendLine("P"+p+" REAL ROOT DISPLACEMENT between samples="+
                    Vector3.Distance(first,actor.transform.position).ToString("F3")+"m");
                shot.gameObject.SetActive(false);
            }
            Directory.CreateDirectory(Path.GetDirectoryName(Report));
            File.WriteAllText(Report,report.ToString(),new UTF8Encoding(false));
            Debug.Log("[V07 P3435 FINAL] v07 scene saved; eight new actual-camera PNGs. Visually inspect both panels! "+Report);
        }
        catch(Exception ex){Debug.LogException(ex);Debug.LogError("[V07 P3435 FINAL] Failed; inspect v07, v06 untouched");}
        finally
        {
            if(active.IsValid()&&active.isLoaded)SceneManager.SetActiveScene(active);
            if(loaded.IsValid()&&loaded.isLoaded)EditorSceneManager.CloseScene(loaded,true);
        }
    }
    private static string ImagePath(int p,string t) =>
        Evidence+"P"+p+"_UPRIGHT_STRIDE_"+t+"_clearStaff_v4_20261029.png";
    private static PlayableDirector Director(Transform shot) =>
        shot.GetComponentsInChildren<PlayableDirector>(true).Single(d=>d.playableAsset is TimelineAsset);
    private static Animator Actor(Transform shot) =>
        shot.Find("V07_UPRIGHT_VANDER_WALK_PATH").Find("V07_STANDING_VANDER_REAL_WALK_VISIBLE").GetComponent<Animator>();
    private static Camera Camera(Transform shot,int p) =>
        shot.GetComponentsInChildren<Camera>(true).Single(c=>c.name=="P"+p+"_CORRIDOR_STATIC_CAMERA");
    private static void Validate(PlayableDirector d,Animator a,int p)
    {
        var t=(TimelineAsset)d.playableAsset;
        if(!AssetDatabase.GetAssetPath(t).StartsWith(Folder,StringComparison.Ordinal))
            throw new InvalidOperationException("P"+p+" not using v07 timeline");
        var track=t.GetOutputTracks().OfType<AnimationTrack>().Single(x=>x.name==GaitTrack);
        var clip=((AnimationPlayableAsset)track.GetClips().Single().asset).clip;
        if(!clip || !clip.humanMotion ||
            AssetDatabase.GetAssetPath(clip)!=Folder+"VanderPoel_Walking_HumanoidRootBaked_v07.fbx" ||
            d.GetGenericBinding(track)!=a)
            throw new InvalidOperationException("P"+p+" source true Humanoid walk missing");
    }
    private static void Capture(Camera c,string file)
    {
        var orig=c.targetTexture;
        var active=RenderTexture.active;
        RenderTexture rt=null;
        Texture2D png=null;
        try
        {
            rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);
            c.targetTexture=rt;
            c.Render();
            RenderTexture.active=rt;
            png=new Texture2D(1280,720,TextureFormat.RGB24,false);
            png.ReadPixels(new Rect(0,0,1280,720),0,0);
            png.Apply();
            File.WriteAllBytes(file,png.EncodeToPNG());
        }
        finally
        {
            c.targetTexture=orig;
            RenderTexture.active=active;
            if(png)UnityEngine.Object.DestroyImmediate(png);
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
