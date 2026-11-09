#if UNITY_EDITOR
using System;
using System.Collections.Generic;
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

internal static class PrologueV07Oct29PoseChairBookRepair
{
    const string V07="Assets/Scenes/Cinematics/Prologue_Master_Sequence_P01_42_v07_SpellbookWalkPolish.unity";
    const string V06="Assets/Scenes/Cinematics/Prologue_Master_Sequence_P01_42_v06_DialogueContinuity.unity";
    const string SHA="283573BAA9EB7C9E5D3CEF9290FFDDC3DF1F026B8C8D65EAF6D84A73CB26EBF4";
    const string PathRoot="Assets/Cinematics/Storyboard3D/Timelines/PrologueMaster_v07_SpellbookWalkPolish/";
    const string Report="Library/PrologueV07_Oct29PoseChairBookRepair.txt";
    const string Pics=PathRoot+"Validation/Screenshots/";

    [MenuItem("SPELLSTRIKE/Cinematics/Prologue V07/19 FIX P09 standing, chair support and physical book scale")]
    static void Run()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||
            SceneManager.sceneCount!=1||SceneManager.GetActiveScene().isDirty||
            System.Diagnostics.Process.GetProcessesByName("VALORANT-Win64-Shipping").Length!=0||
            Hash(V06)!=SHA||AssetDatabase.AssetPathToGUID(V07)!="12405e3bd39d43d398685e74678a8516"||
            File.Exists(Report))
        {Debug.LogError("[OCT29 REPAIR] unsafe, wrong GUID or already completed");return;}
        var original=SceneManager.GetActiveScene();
        Scene scene=default;
        var sb=new StringBuilder();
        try
        {
            scene=EditorSceneManager.OpenScene(V07,OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var group=scene.GetRootGameObjects().Single(g=>g.name=="MASTER_SHOT_GROUPS__NUMBERED_IN_STORY_ORDER").transform;
            var shot09=group.Find("P09_SHOT_ROOT");
            var cloneAt=shot09.Find("P09_VANDER_STANDING_AFTER_DECADES__V07");
            if(cloneAt)throw new InvalidOperationException("P09 already contains new upright actor");
            var oldVander=shot09.GetComponentsInChildren<Animator>(true)
                .Single(a=>a.name=="P09_VanderPoel_ANIMATED");
            var oldRenderers=oldVander.GetComponentsInChildren<Renderer>(true).Where(x=>x.enabled).ToArray();
            if(oldRenderers.Length<1)throw new InvalidOperationException("P09 old Vander visual not enabled");
            var P15=group.Find("P15_P18_SHOT_ROOT");
            var source=P15.GetComponentsInChildren<Animator>(true)
                .Single(a=>a.name=="VanderPoel_VisualRig_Mixamo" &&
                    a.transform.parent.name=="VANDER_POEL_BLOCKING_RIG_VISUAL_ONLY");
            if(!source.avatar||!source.avatar.isValid||!source.avatar.isHuman)
                throw new InvalidOperationException("P15 source upright human Avatar missing");
            var standingAsset=AssetDatabase.LoadAllAssetsAtPath(
                "Assets/Cinematics/Storyboard3D/Characters/VanderPoel/Models/VanderPoel_StandingIdle_WithSkin.fbx")
                .OfType<AnimationClip>().FirstOrDefault(c=>c.humanMotion);
            if(!standingAsset)throw new InvalidOperationException("No genuine humanoid standing idle");
            // Retain canonical P09 chair for older Van only. New Vander is
            // explicitly STANDING in this time-passage shot, not air seated.
            var P09Root=shot09.Find("P09_CINEMATIC_CANDIDATE_ROOT");
            var rig=UnityEngine.Object.Instantiate(source.gameObject,P09Root,false);
            rig.name="P09_VANDER_STANDING_AFTER_DECADES__V07";
            rig.SetActive(true);
            var animator=rig.GetComponent<Animator>();
            animator.runtimeAnimatorController=null;
            animator.applyRootMotion=false;
            foreach(var r in rig.GetComponentsInChildren<Renderer>(true))r.enabled=true;
            rig.transform.SetPositionAndRotation(
                // This is the approved P09 Vander gateway actor position,
                // NOT the other seated character's chair location.
                new Vector3(-.10f,0f,2.70f),Quaternion.Euler(0,0,0));
            var skin=rig.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .First(r=>r.sharedMesh!=null);
            var factor=1.82f/Mathf.Max(.001f,skin.bounds.size.y);
            rig.transform.localScale*=factor;
            // Sample idle and freeze in the scene's authored pose. No animated
            // walking clip is played for this silent P09 "Decades passed" card.
            standingAsset.SampleAnimation(rig,Mathf.Min(.15f,standingAsset.length*.5f));
            animator.enabled=false;
            float bottom=skin.bounds.min.y;
            rig.transform.position+=Vector3.up*(0-bottom);
            foreach(var renderer in oldRenderers)renderer.enabled=false;
            sb.AppendLine("P09 NEW UPRIGHT Vander "+rig.name+" at "+rig.transform.position+
                " Avatar="+animator.avatar.name+" bodyHeight="+skin.bounds.size.y+
                " seatedRenderersHidden="+oldRenderers.Length+" idleSample="+standingAsset.name);
            foreach(var chair in shot09.GetComponentsInChildren<Renderer>(true)
                .Where(r=>r.name.StartsWith("STUDY_CinematicChair_")))
            {
                chair.enabled=chair.transform.position.x>0;
                sb.AppendLine("P09 chair "+chair.name+" enabled="+chair.enabled+
                    " world="+chair.transform.position);
            }
            // Chairs exist in the library environment but were disabled in
            // each cloned seated dialogue shot. Enable matching visible seat
            // meshes in the shot-local library, never add a chair to P09 Vander.
            foreach(int panel in new[]{6,7,8,10,12,13,14,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,36})
            {
                var shot=group.Find("P"+panel.ToString("00")+"_SHOT_ROOT");
                if(!shot)continue;
                bool seated=shot.GetComponentsInChildren<Transform>(true).Any(t=>
                    (t.name.Contains("OlderVan_ANIMATED")||t.name.Contains("VanderPoel_ANIMATED")) &&
                    t.GetComponentsInChildren<SkinnedMeshRenderer>(true).Any(r=>r.enabled));
                if(!seated)continue;
                var chairs=shot.GetComponentsInChildren<Renderer>(true)
                    .Where(r=>r.name.StartsWith("STUDY_CinematicChair_")&&
                        r.transform.position.z>4&&r.transform.position.z<9).ToArray();
                foreach(var r in chairs)r.enabled=true;
                sb.AppendLine("P"+panel+" seated actors=YES chairsEnabled="+chairs.Length);
            }
            // All seven panels use one physical volume. P20 imported root=1.0
            // was 2.33 times the matching open-book P21/25/26/29/30 root=.43.
            // We normalize the P20 *same-model* root, preserving its page mesh
            // and P26 handwritten letter texture verbatim.
            foreach(int panel in new[]{20,21,25,26,29,30})
            {
                var shot=group.Find("P"+panel.ToString("00")+"_SHOT_ROOT");
                if(!shot)throw new InvalidOperationException("Missing book shot P"+panel);
                var book=shot.GetComponentsInChildren<Transform>(true)
                    .Single(t=>t.name=="P20_SINGLE_OPEN_ANCIENT_VOLUME__ILLUSTRATED");
                float before=book.localScale.x;
                if(panel==20)
                {
                    if(Mathf.Abs(before-1)>0.002f)
                        throw new InvalidOperationException("P20 unexpected book root scale "+before);
                    book.localScale=Vector3.one*.43f;
                }
                else if(Mathf.Abs(before-.43f)>.03f)
                    throw new InvalidOperationException("P"+panel+" unexpected canonical book scale "+before);
                var renderers=book.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled).ToArray();
                sb.AppendLine("P"+panel+" SAME OPEN BOOK physical scale "+before+" => "+
                    book.localScale+" pieces="+renderers.Length);
            }
            // Closed P19 volume may be a DIFFERENT mesh/proportions. Do not
            // force an arbitrary transform just to match raw scale numbers.
            var closed=group.Find("P19_SHOT_ROOT").GetComponentsInChildren<Transform>(true)
                .Single(t=>t.name=="P19_ANCIENT_VOLUME_CLOSED__PHYSICAL");
            sb.AppendLine("P19 CLOSED DIFFERENT MESH scale="+closed.localScale+
                " (requires footprint comparison; no blind scaling)");
            Directory.CreateDirectory("Library");
            var backup="Library/PrologueV07_Before_Oct29PoseChairBookRepair.scene.bak";
            if(File.Exists(backup))throw new IOException("Backup already present "+backup);
            File.Copy(V07,backup);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            if(!EditorSceneManager.SaveScene(scene,V07))
                throw new IOException("Failed v07-only save");
            File.WriteAllText(Report,sb.ToString(),new UTF8Encoding(false));
            Debug.Log("[OCT29 REPAIR] P09 upright, seated library chair support, P20 physical .43 restored. NOT VISUAL QA yet. "+Report);
        }
        catch(Exception ex){Debug.LogException(ex);}
        finally
        {
            if(original.IsValid()&&original.isLoaded)SceneManager.SetActiveScene(original);
            if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);
        }
    }
    static string Hash(string p){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(p))).Replace("-","");}
}
#endif
