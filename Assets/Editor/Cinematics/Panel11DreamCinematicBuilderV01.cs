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
using UnityEngine.UI;
using TMPro;

// Versioned Panel 11 dream-memory visual: existing canonical library, Van and rigged Nevali.
// Safe additive save only; never switches or saves active Stage3Scene, no new gameplay systems.
internal static class Panel11DreamCinematicBuilderV01
{
    const string Once="SPELLSTRIKE_PANEL11_DREAM_CINEMATIC_BUILDER_V01_ONCE";
    const string SceneOriginal="Assets/Scenes/Cinematics/Prologue_P10_HeadBobDialogue_v07.unity";
    const string SceneNew="Assets/Scenes/Cinematics/Prologue_P11_NevaliDream_v01.unity";
    const string TimelineFolder="Assets/Cinematics/Storyboard3D/Timelines/Panel11_v01";
    const string TimelinePath=TimelineFolder+"/P11_NevaliDream_8s_v01.playable";
    const string ClipPath=TimelineFolder+"/P11_Nevali_v13_SubtleHeadBob_v01.anim";
    const string Model="Assets/Cinematics/Storyboard3D/Characters/Panel01_v03/Nevali_v13_MediaPipe_BlockPlay_v04.fbx";
    const string Report="Library/Panel07_10MotionAudit/Panel11_BuilderV01.txt";
    static Panel11DreamCinematicBuilderV01(){EditorApplication.delayCall+=Build;}
    [MenuItem("SPELLSTRIKE/Cinematics/Panel 11/Build Dream Memory v01")]
    static void Build()
    {
        if(SessionState.GetBool(Once,false)||EditorApplication.isPlayingOrWillChangePlaymode)return;
        var orig=SceneManager.GetActiveScene();
        if(!orig.IsValid()||!orig.isLoaded||string.IsNullOrEmpty(orig.path)||orig.isDirty){Debug.LogWarning("[P11 BUILD] Clean active scene required. No changes made.");return;}
        SessionState.SetBool(Once,true);
        var loaded=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).path).ToArray();
        var lines=new List<string>{"START active="+orig.path+" dirty="+orig.isDirty};
        Scene editing=default(Scene);
        try
        {
            if(File.Exists(SceneNew)||File.Exists(TimelinePath)||File.Exists(ClipPath))throw new IOException("Versioned Panel 11 outputs already exist. Refusing overwrite.");
            if(!AssetDatabase.CopyAsset(SceneOriginal,SceneNew))throw new IOException("Failed to copy approved v07 library scene into new Panel 11 scene");
            if(!AssetDatabase.IsValidFolder(TimelineFolder))AssetDatabase.CreateFolder("Assets/Cinematics/Storyboard3D/Timelines","Panel11_v01");
            editing=EditorSceneManager.OpenScene(SceneNew,OpenSceneMode.Additive);
            if(!editing.IsValid()||!editing.isLoaded||SceneManager.GetActiveScene().path!=orig.path)throw new Exception("Scene mode safety check failed");
            var roots=editing.GetRootGameObjects();
            var root=roots.Single(x=>x.name.Contains("CINEMATIC_CANDIDATE_ROOT"));
            root.name="P11_NevaliDream_CINEMATIC_ROOT";
            var all=root.GetComponentsInChildren<Transform>(true);
            var vander=all.Single(x=>x.name=="P10_VanderPoel_ANIMATED");
            UnityEngine.Object.DestroyImmediate(vander.gameObject);
            var older=all.Single(x=>x.name=="P10_OlderVan_ANIMATED");
            var olderAnimator=older.GetComponentInChildren<Animator>(true);
            if(!olderAnimator)throw new Exception("OlderVan animation rig missing");
            var cam=roots.SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).Single();
            cam.name="P11_STATIC_DREAM_LIBRARY_CAMERA";
            // OlderVan sits left, Nevali stands right: preserve familiar two-shot from P10.
            var eye=new Vector3(0f,1.50f,8.70f);var look=new Vector3(0f,1.16f,5.45f);
            cam.transform.position=eye;cam.transform.rotation=Quaternion.LookRotation((look-eye).normalized,Vector3.up);cam.fieldOfView=42f;
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(Model);if(!model)throw new IOException("Nevali v13 rigged model missing");
            var nevali=PrefabUtility.InstantiatePrefab(model,editing) as GameObject;
            if(!nevali)throw new IOException("Could not instantiate Nevali v13 FBX");
            nevali.name="P11_Nevali_v13_DreamVision_ANIMATED";
            nevali.transform.localScale=Vector3.one*.00016f;
            nevali.transform.position=new Vector3(-.78f,.38f,5.2f);
            nevali.transform.rotation=Quaternion.identity;
            var nevaliAnimator=nevali.GetComponentInChildren<Animator>(true);
            if(!nevaliAnimator)throw new IOException("Nevali imported rig has no Animator");
            var matdir="Assets/Cinematics/Storyboard3D/Characters/Panel01_v03/Materials/";
            var names=new[]{"Body","Face","Eyes","Hair"};
            var actualMats=names.Select(x=>AssetDatabase.LoadAssetAtPath<Material>(matdir+"Nevali_Upload_"+x+"_v03.mat")).ToArray();
            foreach(var skinned in nevali.GetComponentsInChildren<SkinnedMeshRenderer>(true)){
                var materialSlots=skinned.sharedMaterials;
                for(int i=0;i<materialSlots.Length;i++){
                    string text=materialSlots[i]?materialSlots[i].name.ToLowerInvariant():"";
                    int mapped=Array.FindIndex(names,n=>text.Contains(n.ToLowerInvariant()));
                    if(mapped>=0 && actualMats[mapped])materialSlots[i]=actualMats[mapped];
                }
                skinned.sharedMaterials=materialSlots;
                skinned.updateWhenOffscreen=true;
                lines.Add("NEVALI_SKIN="+skinned.name+" bones="+skinned.bones.Length+" materials="+string.Join(",",skinned.sharedMaterials.Select(m=>m?m.name:"NULL")));
            }
            var glowing=new GameObject("P11_DREAM_MEMORY_WARM_PORTRAIT_LIGHT");SceneManager.MoveGameObjectToScene(glowing,editing);
            var fill=glowing.AddComponent<Light>();fill.type=LightType.Point;fill.range=4.8f;fill.intensity=2.4f;fill.color=new Color(1f,.78f,.5f);fill.shadows=LightShadows.None;
            glowing.transform.position=new Vector3(-.72f,1.65f,5.28f);
            // New Timeline; never mutate the v07 copy's Timeline asset.
            var timeline=ScriptableObject.CreateInstance<TimelineAsset>();timeline.name="P11_NevaliDream_8s_v01";timeline.editorSettings.fps=24;
            AssetDatabase.CreateAsset(timeline,TimelinePath);
            var olderClip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Cinematics/Storyboard3D/Timelines/Panels07_10_v06/HeadBobClips/P10_OlderVan_SubtleHeadBob_v06.anim");
            if(!olderClip)throw new IOException("Older Van approved head animation absent");
            AnimationClip nevaliClip=MakeNevaliHeadClip(lines);
            var trackVan=timeline.CreateTrack<AnimationTrack>(null,"OlderVan | In dream, subtle head bob");
            var tc1=trackVan.CreateClip(olderClip);tc1.start=0;tc1.duration=8;
            var trackNevali=timeline.CreateTrack<AnimationTrack>(null,"Nevali | Dream memory gentle head bob");
            var tc2=trackNevali.CreateClip(nevaliClip);tc2.start=0;tc2.duration=8;
            var director=roots.SelectMany(x=>x.GetComponentsInChildren<PlayableDirector>(true)).Single();
            director.playableAsset=timeline;director.playOnAwake=true;director.extrapolationMode=DirectorWrapMode.Hold;
            director.SetGenericBinding(trackVan,olderAnimator);director.SetGenericBinding(trackNevali,nevaliAnimator);
            // Dialogue remains a Unity UI overlay, never embedded in approved storyboard source art.
            BuildSubtitleUI(editing);
            EditorUtility.SetDirty(timeline);
            if(!EditorSceneManager.SaveScene(editing,SceneNew))throw new IOException("Panel 11 scene save failed");
            AssetDatabase.SaveAssets();
            lines.Add("SAVED scene="+SceneNew+" timeline="+TimelinePath+" clip="+ClipPath);
            lines.Add("DIRECTOR duration="+director.duration+" tracks="+timeline.GetOutputTracks().Count());
            lines.Add("CAMERA stationary="+cam.transform.position+" FOV="+cam.fieldOfView);
            lines.Add("SUBTITLE=Van Aert: Nevali...? (provisional text; no voice)");
        }
        catch(Exception error){lines.Add("ERROR="+error);Debug.LogError("[P11 BUILD] "+error);}
        finally{if(editing.IsValid()&&editing.isLoaded)EditorSceneManager.CloseScene(editing,true);}
        var active=SceneManager.GetActiveScene();lines.Add("END active="+active.path+" dirty="+active.isDirty);
        lines.Add("LOADED_SCENES_UNCHANGED="+loaded.SequenceEqual(Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).path)));
        Directory.CreateDirectory(Path.GetDirectoryName(Report));File.WriteAllLines(Report,lines);
        Debug.Log("[P11 BUILD] completed attempt, report "+Report);
    }
    static AnimationClip MakeNevaliHeadClip(List<string> lines)
    {
        var source=AssetDatabase.LoadAllAssetsAtPath(Model).OfType<AnimationClip>().FirstOrDefault(x=>x.name=="Scene"&&x.length>7.0);
        if(!source)throw new IOException("Nevali imported FBX has no valid animation source");
        var all=AnimationUtility.GetCurveBindings(source);
        var heads=all.Where(b=>b.path.Split('/').Last().ToLowerInvariant().EndsWith("head") && b.propertyName.StartsWith("m_LocalRotation."))
            .Select(b=>b.path).Distinct().ToArray();
        if(heads.Length!=1)throw new IOException("Expected single Head transform curve group, found "+heads.Length);
        string head=heads[0];
        string[] axes={"m_LocalRotation.x","m_LocalRotation.y","m_LocalRotation.z","m_LocalRotation.w"};
        float[] components=axes.Select(n=>{
            var b=all.First(x=>x.path==head&&x.propertyName==n);
            return AnimationUtility.GetEditorCurve(source,b).Evaluate(0);
        }).ToArray();
        var q=new Quaternion(components[0],components[1],components[2],components[3]);q.Normalize();
        var clip=new AnimationClip{name="P11_Nevali_v13_HeadBobOnly_v01",frameRate=24};
        float[] times={0f,2f,4f,6f,8f};float[] nod={0f,.75f,-.25f,.35f,0f};
        Quaternion[] rotations=nod.Select(n=>q*Quaternion.Euler(n,0f,0f)).ToArray();
        int animated=0,count=0;
        foreach(var b in all){var input=AnimationUtility.GetEditorCurve(source,b);if(input==null)continue;
            AnimationCurve result;
            int axis=Array.IndexOf(axes,b.propertyName);
            if(b.path==head&&axis>=0){var keys=new Keyframe[times.Length];
                for(int i=0;i<times.Length;i++){var r=rotations[i];keys[i]=new Keyframe(times[i],axis==0?r.x:axis==1?r.y:axis==2?r.z:r.w);}
                result=new AnimationCurve(keys);animated++;
            }else{float value=input.Evaluate(0);result=new AnimationCurve(new Keyframe(0,value),new Keyframe(8,value));}
            AnimationUtility.SetEditorCurve(clip,b,result);count++;
        }
        if(animated!=4)throw new IOException("Nevali head quaternion could not be generated");
        clip.EnsureQuaternionContinuity();AssetDatabase.CreateAsset(clip,ClipPath);
        lines.Add("NEVALI_HEAD_PATH="+head+" allCurves="+count+" movingHeadCurves="+animated);
        return clip;
    }
    static void BuildSubtitleUI(Scene scene)
    {
        var canvasObj=new GameObject("P11_DREAM_DIALOGUE_TEXT_CANVAS",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        SceneManager.MoveGameObjectToScene(canvasObj,scene);
        var cv=canvasObj.GetComponent<Canvas>();cv.renderMode=RenderMode.ScreenSpaceOverlay;cv.sortingOrder=70;
        var scaler=canvasObj.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);
        var subtitle=new GameObject("P11_DIALOGUE_VAN_WHISPERS_NEVALI",typeof(RectTransform),typeof(TextMeshProUGUI));
        subtitle.transform.SetParent(canvasObj.transform,false);
        var rt=subtitle.GetComponent<RectTransform>();rt.anchorMin=new Vector2(.16f,.035f);rt.anchorMax=new Vector2(.84f,.16f);rt.offsetMin=Vector2.zero;rt.offsetMax=Vector2.zero;
        var text=subtitle.GetComponent<TextMeshProUGUI>();text.text="Van Aert: Nevali...?";text.fontSize=37;text.color=new Color(1,.94f,.80f,1);text.alignment=TextAlignmentOptions.Center;
        text.raycastTarget=false; text.enableWordWrapping=true;
        // Text appears as an in-engine overlay; absence of approved voice is intentional.
    }
}
#endif


