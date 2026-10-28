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

/// <summary>
/// Read-only scene evaluation and GPU-backed Unity camera preview capture.
/// Does not save the loaded scene, change project settings, or affect other panels.
/// </summary>
internal static class Panel04A04UnityPreviewQAV02
{
    [Serializable] private sealed class ShotRecord
    {
        public string shot;
        public float time;
        public float camera_distance_error;
        public string capture;
    }

    [Serializable] private sealed class PreviewReport
    {
        public string scene;
        public int timeline_tracks;
        public float duration;
        public int renderers;
        public bool camera_transforms_pass;
        public bool gpu_captures_complete;
        public ShotRecord[] shots;
        public string notes;
    }

    const string ScenePath = "Assets/Scenes/Cinematics/Prologue_P04_A04_GriefStatic_v02.unity";
    const string OutputDir = "ArtSource/Cinematics/Panel04_v04/Reviews/Unity_v02";
    const string ReportPath = "Library/Panel04Audit/P04_A04_UnityPreviewQA_v02.json";

    [MenuItem("SPELLSTRIKE/Cinematics/Panel 04/Verify Unity Camera v01")]
    public static void RunBatch()
    {
        try { Verify(); }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorApplication.Exit(1);
        }
    }

    private static void Verify()
    {
        if (File.Exists(ReportPath))
            throw new IOException("Already have Unity preview QA, refusing overwrite: " + ReportPath);
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var director = UnityEngine.Object.FindFirstObjectByType<PlayableDirector>();
        var camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
        var overlay = UnityEngine.Object.FindFirstObjectByType<Panel04A04NarrationOverlay>();
        if (director == null || camera == null || overlay == null)
            throw new Exception("Expected timeline, camera and narration caption.");
        var timeline = director.playableAsset as TimelineAsset;
        if (timeline == null || timeline.GetOutputTracks().Count() != 2)
            throw new Exception("Unexpected Timeline asset or track count");
        if (Math.Abs(timeline.duration - 10.0) > 0.1)
            throw new Exception("Wrong Timeline duration");

        int rendererCount = scene.GetRootGameObjects().Sum(o => o.GetComponentsInChildren<Renderer>(true).Length);
        if (rendererCount < 700)
            throw new Exception("Stage GLB incomplete: " + rendererCount);
        var shots = new []
        {
            (time:0.65, tag:"01_RUIN_DISCOVERY", look:new Vector3(.49f,2.35f,3.78f)),
            (time:3.20, tag:"02_VAN_GRIEF", look:new Vector3(-1.16f,1.79f,-1.42f)),
            (time:6.55, tag:"03_MOTHER_FACE", look:new Vector3(.13f,1.30f,-1.10f)),
            (time:8.95, tag:"04_BLUE_BOW", look:new Vector3(-.13f,1.56f,1.78f))
        };

        Directory.CreateDirectory(OutputDir);
        Directory.CreateDirectory("Library/Panel04Audit");
        var records = new List<ShotRecord>();
        string error = "";
        foreach (var shot in shots)
        {
            director.time = shot.time;
            director.Evaluate();
            Vector3 actual = camera.transform.position;
            float discrepancy = Vector3.Distance(actual, shot.look);
            if (discrepancy > 0.65f)
                throw new Exception("Timeline camera transform mismatch at " + shot.tag +
                    " got " + actual + ", expected " + shot.look + ", error " + discrepancy);
            string output = OutputDir + "/P04_A04_Unity_" + shot.tag + "_v02.png";
            if (File.Exists(output))
                throw new IOException("Refuse screenshot overwrite: " + output);

            string state = "SKIPPED";
            RenderTexture rt = null;
            Texture2D tex = null;
            try
            {
                rt = new RenderTexture(960, 540, 24, RenderTextureFormat.ARGB32);
                rt.Create();
                camera.targetTexture = rt;
                var previous = RenderTexture.active;
                camera.Render();
                RenderTexture.active = rt;
                tex = new Texture2D(960,540,TextureFormat.RGBA32,false);
                tex.ReadPixels(new Rect(0,0,960,540),0,0);
                tex.Apply(false);
                RenderTexture.active = previous;
                byte[] png = tex.EncodeToPNG();
                if (png.Length < 20000)
                    throw new Exception("Rendered PNG unusually small: " + png.Length);
                File.WriteAllBytes(output, png);
                state = "CAPTURED_" + png.Length;
            }
            catch (Exception ex)
            {
                error += ex.Message + " | ";
                Debug.LogWarning("P04 Unity screenshot unavailable for " + shot.tag + ": " + ex);
                state = "CAPTURE_FAILED";
            }
            finally
            {
                camera.targetTexture = null;
                if (rt != null) { rt.Release(); UnityEngine.Object.DestroyImmediate(rt); }
                if (tex != null) UnityEngine.Object.DestroyImmediate(tex);
            }
            records.Add(new ShotRecord
            {
                shot = shot.tag,
                time = (float)shot.time,
                camera_distance_error = discrepancy,
                capture = state
            });
            Debug.Log("[P04 QA] " + shot.tag + " camera discrepancy=" + discrepancy + ", " + state);
        }

        bool previewCaptured = records.All(s => s.capture.StartsWith("CAPTURED_"));
        var report = new PreviewReport
        {
            scene = ScenePath,
            timeline_tracks = 2,
            duration = 10f,
            renderers = rendererCount,
            camera_transforms_pass = true,
            gpu_captures_complete = previewCaptured,
            shots = records.ToArray(),
            notes = error
        };
        File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
        Debug.Log("[P04 UNITY QA] COMPLETE renderers=" + rendererCount +
                  " four camera transform checks PASS, screenshots=" + previewCaptured);
    }
}
#endif
