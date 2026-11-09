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
/// Corrects P34/P35 walk V07 without changing canonical source FBX:
/// duplicate the proven humanoid clip and bake Root XZ/rotation into pose
/// through Unity's official ModelImporterClipAnimation flags. This retains
/// leg muscle retargeting unlike generic skeleton-only animation curves.
/// </summary>
internal static class PrologueV07P3435HumanoidInPlaceImport_20261029
{
    private const string V07 = "Assets/Scenes/Cinematics/Prologue_Master_Sequence_P01_42_v07_SpellbookWalkPolish.unity";
    private const string V06 = "Assets/Scenes/Cinematics/Prologue_Master_Sequence_P01_42_v06_DialogueContinuity.unity";
    private const string Backup = "RecoveryBackups/ProloguePolishV07_20261028_Immutable_v01/Prologue_Master_v06_BEFORE_POLISH.unity";
    private const string V06Hash = "283573BAA9EB7C9E5D3CEF9290FFDDC3DF1F026B8C8D65EAF6D84A73CB26EBF4";
    private const string Root = "Assets/Cinematics/Storyboard3D/Timelines/PrologueMaster_v07_SpellbookWalkPolish/";
    private const string SourceFbx = "Assets/Cinematics/Storyboard3D/Characters/VanderPoel/Animations/VanderPoel_Walking.fbx";
    private const string NewFbx = Root + "VanderPoel_Walking_HumanoidRootBaked_v07.fbx";
    private const string Report = "Library/PrologueV07_P3435_HumanoidRootBaked_20261029.txt";

    [MenuItem("SPELLSTRIKE/Cinematics/Prologue V07/11 ROOT-BAKED Humanoid Stride + Camera FIX %#&b")]
    private static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            EditorApplication.isUpdating || SceneManager.sceneCount != 1 ||
            SceneManager.GetActiveScene().isDirty ||
            System.Diagnostics.Process.GetProcessesByName("VALORANT-Win64-Shipping").Length != 0 ||
            Hash(V06) != V06Hash || Hash(Backup) != V06Hash ||
            AssetDatabase.AssetPathToGUID(V07) != "12405e3bd39d43d398685e74678a8516" ||
            File.Exists(NewFbx))
        {
            Debug.LogError("[V07 ROOTBAKE] Unsafe Editor / existing alternate FBX / protected original mismatch.");
            return;
        }
        var prior = SceneManager.GetActiveScene();
        Scene scene = default;
        var report = new StringBuilder();
        try
        {
            scene = EditorSceneManager.OpenScene(V07,OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var group = scene.GetRootGameObjects().Single(g =>
                g.name=="MASTER_SHOT_GROUPS__NUMBERED_IN_STORY_ORDER").transform;
            foreach(int panel in new[]{34,35})
            {
                var shot=group.Find("P"+panel+"_SHOT_ROOT");
                var rig=shot.Find("V07_UPRIGHT_VANDER_WALK_PATH")
                    .Find("V07_STANDING_VANDER_REAL_WALK_VISIBLE").GetComponent<Animator>();
                var director=shot.GetComponentsInChildren<PlayableDirector>(true)
                    .Single(d=>d.playableAsset is TimelineAsset);
                var t=(TimelineAsset)director.playableAsset;
                var track=t.GetOutputTracks().OfType<AnimationTrack>()
                    .Single(x=>x.name=="V07 FIX | upright Vander true Mixamo walk");
                var playback=((AnimationPlayableAsset)track.GetClips().Single().asset);
                if(!rig.avatar || !rig.avatar.isHuman ||
                    AssetDatabase.GetAssetPath(playback.clip)!=
                    Root+"VanderPoel_52Bone_TrueMixamoStride_INPLACE_v07.anim")
                    throw new InvalidOperationException("Unexpected P"+panel+" gait before replacing root motion.");
            }
            if(!AssetDatabase.CopyAsset(SourceFbx,NewFbx))
                throw new IOException("Could not duplicate approved Humanoid gait source into versioned v07.");
            var importer=AssetImporter.GetAtPath(NewFbx) as ModelImporter;
            if(importer==null)
                throw new InvalidOperationException("Duplicate FBX has no ModelImporter.");
            importer.importAnimation=true;
            importer.animationType=ModelImporterAnimationType.Human;
            importer.avatarSetup=ModelImporterAvatarSetup.CopyFromOther;
            var avatar=AssetDatabase.LoadAllAssetsAtPath(
                "Assets/Cinematics/Storyboard3D/Characters/VanderPoel/Models/VanderPoel_StandingIdle_WithSkin.fbx")
                .OfType<Avatar>().FirstOrDefault(a=>a.isValid && a.isHuman);
            if(!avatar)
                throw new InvalidOperationException("P15 original human Avatar missing.");
            importer.sourceAvatar=avatar;
            var importedClips=importer.defaultClipAnimations;
            if(importedClips.Length==0)
                throw new InvalidOperationException("Duplicated walking FBX has no default imported clip.");
            foreach(var c in importedClips)
            {
                c.loopTime=true;
                c.loopPose=true;
                c.lockRootPositionXZ=true;
                c.lockRootHeightY=true;
                c.lockRootRotation=true;
                c.keepOriginalPositionXZ=true;
                c.keepOriginalPositionY=true;
                c.keepOriginalOrientation=true;
            }
            importer.clipAnimations=importedClips;
            importer.SaveAndReimport();
            var human=AssetDatabase.LoadAllAssetsAtPath(NewFbx)
                .OfType<AnimationClip>().FirstOrDefault(c=>c.humanMotion &&
                    !c.name.StartsWith("__preview__",StringComparison.Ordinal));
            if(!human || human.length<1.45f || human.length>1.7f)
                throw new InvalidOperationException("Root-baked versioned FBX imported without valid Humanoid walk.");
            report.AppendLine("IMPORT "+NewFbx+" source="+SourceFbx+
                " length="+human.length.ToString("F3")+
                " humanMotion="+human.humanMotion+" lockRootPositionXZ/Y/Rotation=true; sourceAvatar="+avatar.name);
            foreach(int p in new[]{34,35})
            {
                var shot=group.Find("P"+p+"_SHOT_ROOT");
                var walker=shot.Find("V07_UPRIGHT_VANDER_WALK_PATH")
                    .Find("V07_STANDING_VANDER_REAL_WALK_VISIBLE");
                var director=shot.GetComponentsInChildren<PlayableDirector>(true)
                    .Single(d=>d.playableAsset is TimelineAsset);
                var t=(TimelineAsset)director.playableAsset;
                var walkTrack=t.GetOutputTracks().OfType<AnimationTrack>()
                    .Single(x=>x.name=="V07 FIX | upright Vander true Mixamo walk");
                var playable=(AnimationPlayableAsset)walkTrack.GetClips().Single().asset;
                playable.clip=human;
                playable.loop=AnimationPlayableAsset.LoopMode.On;
                playable.removeStartOffset=true;
                walker.GetComponent<Animator>().applyRootMotion=false;
                shot.gameObject.SetActive(true);
                director.enabled=true;
                director.RebuildGraph();
                director.time=.8;
                director.Evaluate();
                Vector3 a=walker.position;
                director.time=6.75;
                director.Evaluate();
                Vector3 b=walker.position;
                float route=Vector3.Distance(a,b);
                var camera=shot.GetComponentsInChildren<Camera>(true)
                    .Single(c=>c.name=="P"+p+"_CORRIDOR_STATIC_CAMERA");
                Vector3 midpoint=(a+b)*.5f;
                // A practical full-body SIDE view centered on the ACTUAL
                // sampled motion, not the obsolete seated table location.
                camera.transform.position=new Vector3(-3.55f,midpoint.y+1.58f,midpoint.z+.20f);
                camera.transform.LookAt(midpoint+Vector3.up*.98f);
                camera.fieldOfView=56f;
                camera.nearClipPlane=.075f;
                report.AppendLine("P"+p+" start="+a.ToString("F3")+" end="+b.ToString("F3")+
                    " observed displacement="+route.ToString("F3")+"m, camera="+
                    camera.transform.position.ToString("F3")+" FOV="+camera.fieldOfView+
                    " bodyAvatar="+walker.GetComponent<Animator>().avatar.name);
                shot.gameObject.SetActive(false);
                EditorUtility.SetDirty(t);
                EditorUtility.SetDirty(director);
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene,V07))
                throw new IOException("Failed to save versioned v07 Humanoid root bake.");
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Report));
            File.WriteAllText(Report,report.ToString(),new UTF8Encoding(false));
            Debug.Log("[V07 ROOTBAKE] SAVED real Humanoid source with locked Root XZ/Y/rotation and 2 re-centered shot cameras. NEED NEW VISUAL QA: "+Report);
        }
        catch(Exception ex){Debug.LogException(ex);Debug.LogError("[V07 ROOTBAKE] INCOMPLETE; v06 protected, inspect v07.");}
        finally
        {
            if(prior.IsValid()&&prior.isLoaded) SceneManager.SetActiveScene(prior);
            if(scene.IsValid()&&scene.isLoaded) EditorSceneManager.CloseScene(scene,true);
        }
    }
    private static string Hash(string f)
    {
        if(!File.Exists(f))return "";
        using(var sha=SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(f))).Replace("-","");
    }
}
#endif
