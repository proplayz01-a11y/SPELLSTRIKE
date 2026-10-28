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

// Builds static-camera, head-only, dialogue-ready P07-P10 versioned Unity scenes.
// No Unity MCP, gameplay edits, audio inventions, VFX, camera cuts or camera motion.
internal static class Panels07_10HeadBobDialogueFinalizerV06
{
    const string Key="SPELLSTRIKE_HEADBOB_DIALOGUE_P07_10_V06_ONCE";
    const string TimelineDir="Assets/Cinematics/Storyboard3D/Timelines/Panels07_10_v06";
    const string ClipDir=TimelineDir+"/HeadBobClips";
    const string Report="Library/Panel07_10MotionAudit/UnityHeadBobV06_BuildReport.txt";
    static Panels07_10HeadBobDialogueFinalizerV06(){EditorApplication.delayCall+=Build;}
    [MenuItem("SPELLSTRIKE/Cinematics/Panels 07-10/Build static-camera head bob v06")]
    public static void Build()
    {
        if(SessionState.GetBool(Key,false)||EditorApplication.isPlayingOrWillChangePlaymode)return;
        Scene original=SceneManager.GetActiveScene();
        if(!original.IsValid() || !original.isLoaded || original.isDirty || string.IsNullOrEmpty(original.path))
        {Debug.LogWarning("[P07-10 HEADBOB] Refusing to build: active scene must be saved and clean.");return;}
        SessionState.SetBool(Key,true);
        string[] before=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).path).ToArray();
        var lines=new List<string>{"START active="+original.path+" dirty="+original.isDirty};
        EnsureFolder("Assets/Cinematics/Storyboard3D/Timelines","Panels07_10_v06");
        EnsureFolder(TimelineDir,"HeadBobClips");
        foreach(var p in new[]{"07","08","09","10"})
        {
            Scene work=default(Scene);
            try
            {
                string sceneOld="Assets/Scenes/Cinematics/Prologue_P"+p+"_TimelineReview_v04.unity";
                string sceneNew="Assets/Scenes/Cinematics/Prologue_P"+p+"_HeadBobDialogue_v06.unity";
                string timelineOld="Assets/Cinematics/Storyboard3D/Timelines/Panels07_10_v04/P"+p+"_Cinematic_v04.playable";
                string timelineNew=TimelineDir+"/P"+p+"_DialogueHeadBob_v06.playable";
                if(File.Exists(sceneNew)||File.Exists(timelineNew))throw new IOException("Refusing overwrite of existing v06 output");
                if(!AssetDatabase.CopyAsset(sceneOld,sceneNew))throw new IOException("Cannot version scene "+sceneOld);
                if(!AssetDatabase.CopyAsset(timelineOld,timelineNew))throw new IOException("Cannot version Timeline "+timelineOld);
                var timeline=AssetDatabase.LoadAssetAtPath<TimelineAsset>(timelineNew);
                if(!timeline)throw new IOException("Could not load versioned timeline "+timelineNew);
                work=EditorSceneManager.OpenScene(sceneNew,OpenSceneMode.Additive);
                if(!work.IsValid()||!work.isLoaded || SceneManager.GetActiveScene().path!=original.path)
                    throw new InvalidOperationException("Safety gate: additive scene failed or active scene changed");
                GameObject[] roots=work.GetRootGameObjects();
                var director=roots.SelectMany(x=>x.GetComponentsInChildren<PlayableDirector>(true)).Single();
                var camera=roots.SelectMany(x=>x.GetComponentsInChildren<Camera>(true)).Single();
                director.playableAsset=timeline;
                director.playOnAwake=true;
                director.extrapolationMode=DirectorWrapMode.Hold;
                // STATIC cameras only; P10 zoomed out to give both characters room above the table.
                if(p=="10")
                {
                    Vector3 newEye=new Vector3(0f,1.80f,9.34f),newTarget=new Vector3(0f,1.12f,5.70f);
                    camera.transform.position=newEye;
                    camera.transform.rotation=Quaternion.LookRotation((newTarget-newEye).normalized,Vector3.up);
                    camera.fieldOfView=34f;
                }
                else if(p=="09")
                {
                    Vector3 newEye=new Vector3(-0.25f,1.83f,12.10f),newTarget=new Vector3(0.38f,1.25f,6.15f);
                    camera.transform.position=newEye;
                    camera.transform.rotation=Quaternion.LookRotation((newTarget-newEye).normalized,Vector3.up);
                    camera.fieldOfView=35f;
                }
                foreach(var track in timeline.GetOutputTracks().ToArray())
                {
                    if(track is AnimationTrack)
                    {
                        string role=track.name.Split('|')[0].Trim();
                        if(role=="CAMERA") {timeline.DeleteTrack(track);lines.Add("P"+p+" removed unexpected camera AnimationTrack");continue;}
                        var actor=roots.SelectMany(x=>x.GetComponentsInChildren<Transform>(true))
                            .FirstOrDefault(x=>x.name=="P"+p+"_"+role+"_ANIMATED");
                        if(actor==null)throw new IOException("Actor missing for "+role+" P"+p);
                        Animator anim=actor.GetComponentInChildren<Animator>(true);
                        if(!anim)throw new IOException("Animator missing for "+role+" P"+p);
                        string source="Assets/Cinematics/Storyboard3D/Panels07_10_v02/Panel"+p+"/P"+p+"_"+role+"_CinematicRefinement_Animated_v02.glb";
                        var originalClip=AssetDatabase.LoadAllAssetsAtPath(source).OfType<AnimationClip>().FirstOrDefault(c=>c.name.StartsWith("P"+p+"_",StringComparison.Ordinal)&&c.length>=7.9f);
                        if(!originalClip)throw new IOException("No correct approved animation source for "+p+" "+role);
                        string output=ClipDir+"/P"+p+"_"+role+"_SubtleHeadBob_v06.anim";
                        if(File.Exists(output))throw new IOException("Refusing overwrite of "+output);
                        AnimationClip bob=CreateHeadOnly(originalClip,output,p,role,lines);
                        foreach(TimelineClip subClip in track.GetClips())
                        {
                            var asset=subClip.asset as AnimationPlayableAsset;
                            if(asset==null)throw new IOException("Invalid animation playable asset: "+track.name);
                            asset.clip=bob;
                            subClip.start=0;
                            subClip.duration=8.0;
                            EditorUtility.SetDirty(asset);
                        }
                        track.name=role+" | Subtle head bob only (8s)";
                        director.SetGenericBinding(track,anim);
                        lines.Add("P"+p+" ACTOR "+role+" head-only clip="+bob.name+" animator="+anim.name);
                    }
                    else if(track is ActivationTrack) director.SetGenericBinding(track,camera.gameObject);
                }
                // Do not create dialogue lines or audio without provided/signed-off script.
                timeline.editorSettings.fps=24;
                EditorUtility.SetDirty(timeline);
                if(!EditorSceneManager.SaveScene(work,sceneNew))throw new IOException("Failed to save isolated scene "+sceneNew);
                AssetDatabase.SaveAssets();
                var skinned=roots.SelectMany(x=>x.GetComponentsInChildren<SkinnedMeshRenderer>(true)).Count();
                var tracks=timeline.GetOutputTracks().OfType<AnimationTrack>().Count();
                lines.Add("P"+p+" SAVED scene="+sceneNew+" timeline="+timelineNew+" animatedActors="+tracks+" skinnedMeshes="+skinned+" staticCamera="+camera.transform.position+" FOV="+camera.fieldOfView);
            }
            catch(Exception e){lines.Add("P"+p+" ERROR "+e);Debug.LogError("[P07-10 HEADBOB] P"+p+" failed: "+e);}
            finally{if(work.IsValid()&&work.isLoaded)EditorSceneManager.CloseScene(work,true);}
        }
        lines.Add("END active="+SceneManager.GetActiveScene().path+" dirty="+SceneManager.GetActiveScene().isDirty);
        lines.Add("SCENES_UNCHANGED="+before.SequenceEqual(Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).path)));
        Directory.CreateDirectory(Path.GetDirectoryName(Report));
        File.WriteAllLines(Report,lines);
        Debug.Log("[P07-10 HEADBOB] Build finished. Report="+Report);
    }
    static AnimationClip CreateHeadOnly(AnimationClip source,string output,string panel,string role,List<string> log)
    {
        var all=AnimationUtility.GetCurveBindings(source);
        var headPaths=all.Where(b=>{var part=b.path.Split('/').Last().ToLowerInvariant();return part=="head" || part.EndsWith(":head");}).Select(b=>b.path).Distinct().ToArray();
        if(headPaths.Length!=1)throw new InvalidOperationException("Expected one head bone, got "+headPaths.Length+" for "+panel+" "+role);
        string head=headPaths[0];
        var components=new[]{"m_LocalRotation.x","m_LocalRotation.y","m_LocalRotation.z","m_LocalRotation.w"};
        float[] q=new float[4];
        for(int j=0;j<4;j++){
            var bind=all.FirstOrDefault(b=>b.path==head&&b.propertyName==components[j]);
            if(bind.path==null)throw new IOException("Missing head rotation "+components[j]);
            var curve=AnimationUtility.GetEditorCurve(source,bind);
            if(curve==null)throw new IOException("Head quaternion curve missing "+components[j]);
            q[j]=curve.Evaluate(0);
        }
        Quaternion baseRotation=new Quaternion(q[0],q[1],q[2],q[3]);
        baseRotation.Normalize();
        var clip=new AnimationClip{name="P"+panel+"_"+role+"_HeadBobOnly_v06",frameRate=24,wrapMode=WrapMode.Once};
        float[] times={0,2,4,6,8};
        float amp=panel=="07"?0.45f:panel=="08"?0.9f:panel=="09"?0.65f:role=="OlderVan"?1.25f:0.6f;
        float[] factors={0,0.9f,-0.4f,0.55f,0};
        Quaternion[] poses=times.Select((t,i)=>baseRotation*Quaternion.Euler(factors[i]*amp,0,0)).ToArray();
        int bindings=0,headAnimated=0;
        foreach(var b in all)
        {
            var input=AnimationUtility.GetEditorCurve(source,b);
            if(input==null)continue;
            AnimationCurve result;
            int quaternionIndex=Array.IndexOf(components,b.propertyName);
            if(b.path==head&&quaternionIndex>=0)
            {
                var keys=new Keyframe[times.Length];
                for(int i=0;i<times.Length;i++){
                    float value=quaternionIndex==0?poses[i].x:quaternionIndex==1?poses[i].y:quaternionIndex==2?poses[i].z:poses[i].w;
                    keys[i]=new Keyframe(times[i],value);
                }
                result=new AnimationCurve(keys);
                headAnimated++;
            }
            else
            {
                float baseline=input.Evaluate(0);
                result=new AnimationCurve(new Keyframe(0,baseline),new Keyframe(8,baseline));
            }
            AnimationUtility.SetEditorCurve(clip,b,result);bindings++;
        }
        if(headAnimated!=4)throw new IOException("Head motion not created for "+panel+" "+role);
        clip.EnsureQuaternionContinuity();
        AssetDatabase.CreateAsset(clip,output);
        log.Add("P"+panel+" "+role+" head="+head+" totalFrozenOrBobbedCurves="+bindings+" movingHeadQuaternionCurves="+headAnimated+" maxNodDegrees="+amp);
        return clip;
    }
    static void EnsureFolder(string parent,string sub)
    {
        string full=parent+"/"+sub;
        if(!AssetDatabase.IsValidFolder(full)){
            if(!AssetDatabase.IsValidFolder(parent))throw new DirectoryNotFoundException(parent);
            AssetDatabase.CreateFolder(parent,sub);
        }
    }
}
#endif

