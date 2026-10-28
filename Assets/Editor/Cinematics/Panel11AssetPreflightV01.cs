#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Playables;

internal static class Panel11AssetPreflightV01
{
    const string Key="SPELLSTRIKE_PANEL11_ASSET_PREFLIGHT_V01_ONCE";
    const string FbxPath="Assets/Cinematics/Storyboard3D/Characters/Panel01_v03/Nevali_v13_MediaPipe_BlockPlay_v04.fbx";
    const string ScenePath="Assets/Scenes/Cinematics/Prologue_P10_HeadBobDialogue_v07.unity";
    const string Report="Library/Panel07_10MotionAudit/Panel11_PreflightV01.txt";
    static Panel11AssetPreflightV01(){EditorApplication.delayCall+=Run;}
    static void Run()
    {
        if(SessionState.GetBool(Key,false)||EditorApplication.isPlayingOrWillChangePlaymode)return;
        var current=SceneManager.GetActiveScene();if(!current.IsValid())return;
        SessionState.SetBool(Key,true);
        var lines=new List<string>{"ORIGINAL_SCENE="+current.path+" dirty="+current.isDirty};
        Scene preview=default(Scene);
        try {
            GameObject nevali=AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
            if(!nevali)throw new Exception("Nevali rig asset missing: "+FbxPath);
            lines.Add("NEVALI_PREFAB="+nevali.name+" rootLocalScale="+nevali.transform.localScale+" childCount="+nevali.transform.childCount);
            foreach(var mesh in nevali.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                lines.Add("NEVALI_MESH="+mesh.name+" bones="+mesh.bones.Length+" bounds="+mesh.bounds+" mats="+string.Join(";",mesh.sharedMaterials.Select(m=>m?m.name+"/"+m.shader.name:"NULL")));
            foreach(var clip in AssetDatabase.LoadAllAssetsAtPath(FbxPath).OfType<AnimationClip>())lines.Add("NEVALI_CLIP="+clip.name+" length="+clip.length+" curves="+AnimationUtility.GetCurveBindings(clip).Length);
            preview=EditorSceneManager.OpenPreviewScene(ScenePath);
            if(!preview.IsValid()||!preview.isLoaded)throw new Exception("Couldn't load preview P10");
            var roots=preview.GetRootGameObjects();
            var ts=roots.SelectMany(g=>g.GetComponentsInChildren<Transform>(true));
            foreach(var t in ts.Where(t=>t.name.StartsWith("P10_") && (t.name.EndsWith("ANIMATED")||t.name.Contains("CAMERA")||t.name.Contains("CANONICAL_LIBRARY"))))
                lines.Add("P10_ROOT="+t.name+" world="+t.position+" rotation="+t.rotation.eulerAngles+" localscale="+t.localScale+" parents="+(t.parent?t.parent.name:"NONE"));
            foreach(var cam in roots.SelectMany(g=>g.GetComponentsInChildren<Camera>(true)))lines.Add("P10_CAMERA="+cam.name+" pos="+cam.transform.position+" forward="+cam.transform.forward+" fov="+cam.fieldOfView);
            foreach(var director in roots.SelectMany(g=>g.GetComponentsInChildren<PlayableDirector>(true)))lines.Add("P10_DIRECTOR="+director.name+" asset="+(director.playableAsset?director.playableAsset.name:"NULL"));
            foreach(var sr in roots.SelectMany(g=>g.GetComponentsInChildren<SkinnedMeshRenderer>(true)))lines.Add("P10_SKIN="+sr.name+" center="+sr.bounds.center+" extents="+sr.bounds.extents);
        }
        catch(Exception e){lines.Add("ERROR="+e);Debug.LogError("[P11 PREFLIGHT] "+e);}
        finally {if(preview.IsValid()&&preview.isLoaded)EditorSceneManager.ClosePreviewScene(preview);}
        lines.Add("SCENE_AFTER="+SceneManager.GetActiveScene().path+" dirty="+SceneManager.GetActiveScene().isDirty);
        Directory.CreateDirectory(Path.GetDirectoryName(Report));File.WriteAllLines(Report,lines);Debug.Log("[P11 PREFLIGHT] "+Report);
    }
}
#endif

