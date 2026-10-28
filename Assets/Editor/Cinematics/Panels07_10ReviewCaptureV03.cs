#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
internal static class Panels07_10ReviewCaptureV03
{
    const string Key = "SPELLSTRIKE_PANEL07_10_REVIEW_CAPTURE_V03_ONCE";
    const string Out = "Library/Panel07_10MotionAudit/UnityReview_v03";
    static Panels07_10ReviewCaptureV03() { EditorApplication.delayCall += Run; }
    [MenuItem("SPELLSTRIKE/Cinematics/Panels 07-10/Capture Unity Review v03")]
    static void Run()
    {
        if(SessionState.GetBool(Key,false)) return;
        var original=SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode || !original.IsValid() || !original.isLoaded || original.isDirty || !original.path.EndsWith("Storyboard3D_CinematicWorkbench.unity",StringComparison.Ordinal))
        { Debug.LogWarning("[P07-10 REVIEW] Safety gate: clean workbench required; active="+original.path+" dirty="+original.isDirty);return; }
        SessionState.SetBool(Key,true);
        Directory.CreateDirectory(Out);
        var lines = new List<string>();
        foreach(var p in new []{"07","08","09","10"})
        {
            Scene loaded=default(Scene);
            try
            {
                string path="Assets/Scenes/Cinematics/Prologue_P"+p+"_TimelineCandidate_v03.unity";
                loaded=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                var roots=loaded.GetRootGameObjects();
                var renderers=roots.SelectMany(x=>x.GetComponentsInChildren<Renderer>(true)).ToArray();
                var cameras=roots.SelectMany(x=>x.GetComponentsInChildren<Camera>(true)).ToArray();
                var directors=roots.SelectMany(x=>x.GetComponentsInChildren<PlayableDirector>(true)).ToArray();
                var actorGos=roots.SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).Where(x=>x.name.StartsWith("P"+p+"_") && x.name.EndsWith("_ANIMATED")).ToArray();
                lines.Add("P"+p+" roots="+roots.Length+" renderers="+renderers.Length+" skinned="+renderers.OfType<SkinnedMeshRenderer>().Count()+" cameras="+cameras.Length+" directors="+directors.Length);
                foreach(var actor in actorGos)
                {
                    var animator=actor.GetComponentInChildren<Animator>(true);
                    var rr=actor.GetComponentsInChildren<Renderer>(true);
                    lines.Add("  ACTOR "+actor.name+" pos="+actor.position+" scale="+actor.lossyScale+" animator="+(animator!=null?animator.gameObject.name:"NONE")+" meshes="+rr.Length);
                    string source="Assets/Cinematics/Storyboard3D/Panels07_10_v02/Panel"+p+"/P"+p+"_"+actor.name.Substring(4,actor.name.Length-4-9)+"_CinematicRefinement_Animated_v02.glb";
                    foreach(var clip in AssetDatabase.LoadAllAssetsAtPath(source).OfType<AnimationClip>())
                        lines.Add("    CLIP "+clip.name+" length="+clip.length.ToString("F3")+" legacy="+clip.legacy+" empty="+clip.empty);
                }
                foreach(var renderer in renderers)
                {
                    if(renderer.sharedMaterials.Any(m=>m==null || m.shader==null || m.shader.name.Contains("Error") || m.shader.name.Contains("InternalError")))
                        lines.Add("  BAD_MATERIAL "+renderer.name);
                }
                foreach(var camera in cameras)lines.Add("  CAMERA "+camera.name+" pos="+camera.transform.position+" rot="+camera.transform.rotation.eulerAngles+" fov="+camera.fieldOfView);
                if(directors.Length==0 || cameras.Length==0){lines.Add("  ERROR No director or camera");continue;}
                var director=directors[0];
                var tl=director.playableAsset as TimelineAsset;
                lines.Add("  TL duration="+director.duration+" tracks="+(tl!=null?tl.GetOutputTracks().Count():0));
                if(tl!=null)foreach(var track in tl.GetOutputTracks())
                    foreach(var clip in track.GetClips()) lines.Add("    TRACK "+track.name+" clip="+clip.displayName+" start="+clip.start+" duration="+clip.duration);
                foreach(var time in new[]{0.0,4.0,7.9})
                {
                    try
                    {
                        director.time=time;director.Evaluate();
                        var camera=cameras[0];
                        var rt=RenderTexture.GetTemporary(1280,720,24,RenderTextureFormat.ARGB32);
                        var priorRT=camera.targetTexture;var priorActive=RenderTexture.active;
                        try
                        {
                            camera.targetTexture=rt;
                            camera.Render();
                            RenderTexture.active=rt;
                            var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);
                            tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();
                            string dest=Out+"/P"+p+"_UnityTime_"+time.ToString("F1",System.Globalization.CultureInfo.InvariantCulture).Replace('.','_')+"s.png";
                            File.WriteAllBytes(dest,tex.EncodeToPNG());
                            UnityEngine.Object.DestroyImmediate(tex);
                            lines.Add("    FRAME "+time+" "+dest+" bytes="+new FileInfo(dest).Length);
                        }
                        finally{camera.targetTexture=priorRT;RenderTexture.active=priorActive;RenderTexture.ReleaseTemporary(rt);}
                    }
                    catch(Exception e){lines.Add("    FRAME_ERROR "+time+" "+e);}
                }
            }
            catch(Exception e){lines.Add("P"+p+" ERROR "+e);}
            finally{if(loaded.IsValid()&&loaded.isLoaded){EditorSceneManager.CloseScene(loaded,true);}}
        }
        File.WriteAllLines(Out+"/UnityReviewV03.txt",lines);
        Debug.Log("[P07-10 REVIEW] Finished Unity readback capture: "+Out+"; "+string.Join(" | ",lines.Where(x=>x.Contains("FRAME_ERROR")||x.Contains(" ERROR ")||x.Contains("roots="))));
    }
}
#endif

