#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;

internal static class Panel04A04CompositionExperiment
{
    public static void RunBatch()
    {
        try { Run(); }
        catch(Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
    }

    static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Cinematics/Prologue_P04_A04_GriefStatic_v02.unity");
        var timeline = UnityEngine.Object.FindFirstObjectByType<PlayableDirector>();
        var camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
        if (timeline==null || camera==null) throw new Exception("Missing director/camera");
        var wall = UnityEngine.Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(t => t.name == "HOUSE_LEFT_WALL");
        Debug.Log("[P04 EXPERIMENT] wall exists=" + (wall!=null));
        string dir = "ArtSource/Cinematics/Panel04_v04/Reviews/UnityCompositionExperiment";
        Directory.CreateDirectory(dir);
        if (wall != null) wall.gameObject.SetActive(false);
        timeline.time=0.65;
        timeline.Evaluate();
        Capture(camera,dir+"/P04_Wide_Corrected_NoWall_v01.png");

        var poses = new []
        {
            (name:"P04_Wide_AltA_v01", eye:new Vector3(1.35f,1.90f,2.80f), target:new Vector3(.65f,.54f,.25f)),
            (name:"P04_Wide_AltB_v01", eye:new Vector3(2.08f,1.80f,2.65f), target:new Vector3(.70f,.58f,.25f)),
            (name:"P04_Wide_AltC_v01", eye:new Vector3(.90f,1.70f,2.75f), target:new Vector3(.75f,.55f,.27f))
        };
        foreach(var pose in poses)
        {
            camera.transform.position=pose.eye;
            camera.transform.rotation=Quaternion.LookRotation((pose.target-pose.eye).normalized,Vector3.up);
            Capture(camera,dir+"/"+pose.name+".png");
        }
        timeline.time=6.55;
        timeline.Evaluate();
        Capture(camera,dir+"/P04_Face_Corrected_NoWall_v01.png");
        var faces = new []
        {
            (name:"P04_Face_AltA_v01", eye:new Vector3(.85f,1.12f,-.86f), target:new Vector3(.96f,.43f,.25f)),
            (name:"P04_Face_AltB_v01", eye:new Vector3(1.15f,1.17f,-1.20f), target:new Vector3(1.03f,.42f,.20f))
        };
        foreach(var pose in faces)
        {
            camera.transform.position=pose.eye;
            camera.transform.rotation=Quaternion.LookRotation((pose.target-pose.eye).normalized,Vector3.up);
            Capture(camera,dir+"/"+pose.name+".png");
        }
        Debug.Log("[P04 EXPERIMENT] CAPTURED 7 camera candidates. No scene saved.");
    }

    static void Capture(Camera camera, string path)
    {
        if (File.Exists(path)) throw new IOException("Refuse overwriting candidate "+path);
        var target = new RenderTexture(960,540,24,RenderTextureFormat.ARGB32);
        Texture2D image=null;
        var old=RenderTexture.active;
        try
        {
            target.Create();
            camera.targetTexture=target;
            camera.Render();
            RenderTexture.active=target;
            image = new Texture2D(960,540,TextureFormat.RGBA32,false);
            image.ReadPixels(new Rect(0,0,960,540),0,0);
            image.Apply(false);
            var bytes=image.EncodeToPNG();
            if (bytes.Length<20000) throw new Exception("Preview too small: "+path);
            File.WriteAllBytes(path,bytes);
            Debug.Log("[P04 CANDIDATE] "+path+" bytes="+bytes.Length);
        }
        finally
        {
            camera.targetTexture=null;
            RenderTexture.active=old;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            if(image!=null) UnityEngine.Object.DestroyImmediate(image);
        }
    }
}
#endif
