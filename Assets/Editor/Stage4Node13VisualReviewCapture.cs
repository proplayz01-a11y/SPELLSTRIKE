// Read-only production Stage4 SceneView render capture for Node1/Node3 review.
// The only output is PNG evidence under Library; no prefab, scene, save or asset changes.
#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

internal static class Stage4Node13VisualReviewCapture
{
    private const string ScenePath = "Assets/Scenes/Stage4Scene/Stage4Scene.unity";
    private const string Evidence = "Library/Stage4Node13RevisionIntegration_20261020_2230/VisualEvidence";
    private struct View
    {
        internal string name;
        internal Vector3 eye, target;
        internal float focal;
        internal bool ortho;
        internal float orthoSpan;
        internal View(string n, Vector3 e, Vector3 t, float f, bool o = false, float size = 0f)
        {
            name = n; eye = e; target = t; focal = f; ortho = o; orthoSpan = size;
        }
    }

    // Camera positions sampled from latest Node1Accuracy and Node3Accuracy Blender
    // render_titan source, converted with verified (Unity X,Y,Z)=(-Blender X,Blender Z,-Blender Y).
    private static readonly View[] Views =
    {
        new View("Node1_Courtyard",new Vector3(310,-175,268),new Vector3(276,155,36),35,true,470),
        new View("Node1_GroundView",new Vector3(235,125,32),new Vector3(185,267,58),35),
        new View("Node1_KnightStatue",new Vector3(208,200,91),new Vector3(178,282,89),48),
        new View("Node3_GroundView",new Vector3(1280,350,58),new Vector3(1280,1010,160),21),
        new View("Node3_LeftFace",new Vector3(1065,620,170),new Vector3(963,927,108),44),
        new View("Node3_RightFace",new Vector3(1540,660,160),new Vector3(1640,921,70),43),
        new View("Node3_Hand",new Vector3(1120,765,145),new Vector3(1086,954,63),45),
        new View("Node3_Arena",new Vector3(1400,-300,768),new Vector3(1280,720,88),35,true,1500),
        new View("MainMap_Overview",new Vector3(700,-800,2000),new Vector3(750,1200,-30),35,true,2600)
    };

    [MenuItem("Stage 4/Convergence Citadel v003/Node13 QA/Capture Both Nodes LOD0 and LOD1 in Actual Scene (PNG, Read-only)")]
    private static void Capture()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath || !scene.isLoaded || scene.isDirty || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[Stage4Node13Screens] Requires saved, open, non-playing ACTUAL Stage4Scene.");
            return;
        }
        var groups = scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<LODGroup>(true)).ToArray();
        if (groups.Length != 11) { Debug.LogError("[Stage4Node13Screens] LOD group count != 11. No capture."); return; }
        var one = groups.Single(x=>x.name == "LOD_Island01");
        var three = groups.Single(x=>x.name == "LOD_Island03");
        Directory.CreateDirectory(Evidence);
        string[] images = Directory.GetFiles(Evidence,"*.png");
        if (images.Length != 0)
        {
            Debug.LogError("[Stage4Node13Screens] Existing QA captures would be overwritten. Refusing.");
            return;
        }
        GameObject temp = new GameObject("Stage4Node13_Temporary_QACamera_NOT_SAVED");
        temp.hideFlags = HideFlags.HideAndDontSave;
        var camera = temp.AddComponent<Camera>();
        temp.AddComponent<UniversalAdditionalCameraData>();
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 5000f;
        camera.allowHDR = false;
        camera.depth = -100f;
        camera.cullingMask = -1;
        RenderTexture rt = null;
        Texture2D texture = null;
        RenderTexture prior = RenderTexture.active;
        try
        {
            const int width = 1440, height = 900;
            rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            rt.Create();
            camera.targetTexture = rt;
            texture = new Texture2D(width,height,TextureFormat.RGB24,false);
            int count = 0;
            for (int level=0; level<=1; level++)
            {
                one.ForceLOD(level);
                three.ForceLOD(level);
                foreach (var view in Views)
                {
                    camera.transform.position = Convert(view.eye);
                    camera.transform.rotation = Quaternion.LookRotation(Convert(view.target)-Convert(view.eye),Vector3.up);
                    camera.orthographic = view.ortho;
                    camera.orthographicSize = view.orthoSpan/2;
                    camera.fieldOfView = Mathf.Rad2Deg*2f*Mathf.Atan(18f/view.focal);
                    // This renders the ACTUAL currently opened Stage4Scene from the
                    // exact transformed source review cameras. No Play Mode is entered.
                    camera.Render();
                    RenderTexture.active = rt;
                    texture.ReadPixels(new Rect(0,0,width,height),0,0);
                    texture.Apply();
                    string file = Evidence+"/"+view.name+"_LOD"+level+".png";
                    File.WriteAllBytes(file,texture.EncodeToPNG());
                    count++;
                }
            }
            Debug.Log("[Stage4Node13Screens] Captured "+count+" screenshots from actual production Stage4Scene: "+Evidence);
        }
        catch (Exception e) { Debug.LogException(e); }
        finally
        {
            one.ForceLOD(-1);
            three.ForceLOD(-1);
            if (camera != null) camera.targetTexture = null;
            RenderTexture.active = prior;
            if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
            if (rt != null) { rt.Release(); UnityEngine.Object.DestroyImmediate(rt); }
            UnityEngine.Object.DestroyImmediate(temp);
            SceneView.RepaintAll();
        }
    }

    private static Vector3 Convert(Vector3 blender) => new Vector3(-blender.x,blender.z,-blender.y);
}
#endif
