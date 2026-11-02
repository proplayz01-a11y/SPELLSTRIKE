using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Explicit, read-only observation of real pointer-driven motion in Play Mode.</summary>
public static class ModeSelectSpellItOutHoverQA
{
    [Serializable] private sealed class Pose { public string name; public int instanceId; public Vector3 position; public Quaternion rotation; }
    [Serializable] private sealed class Sample {
        public double elapsed; public bool hovered; public bool running; public Pose[] poses; public string textureFrame;
    }
    [Serializable] private sealed class Report {
        public string scene; public string startedUtc; public string finishReason; public int screenWidth; public int screenHeight;
        public int rtWidth; public int rtHeight; public int rtAA; public List<Sample> samples = new List<Sample>();
        public List<string> screenshots = new List<string>(); public string limitation = "Actual Editor pointer events and sampled renderer frames; not an Android or FPS benchmark.";
    }
    private static Report report;
    private static ModeSelectSpellItOutTileMotion motion;
    private static ModeSelectSpellItOutHoverGate gate;
    private static Transform gallery;
    private static RenderTexture rt;
    private static Texture2D pixels;
    private static string directory;
    private static double start, next, stateStart;
    private static bool previousHover, stateCaptured;
    private static int transition;

    [MenuItem("SPELLSTRIKE/Minigames/SIO SPELL/6 Capture Hover Proof - Play Mode")]
    public static void Begin()
    {
        if (System.Diagnostics.Process.GetProcessesByName("VALORANT-Win64-Shipping").Length > 0)
            throw new InvalidOperationException("VALORANT running; QA stopped.");
        if (!EditorApplication.isPlaying || report != null)
            throw new InvalidOperationException("Enter Play Mode in the actual card scene; only one capture may run.");
        Scene scene = SceneManager.GetSceneByPath(ModeSelectSpellItOutInstallerV01.SourceScene);
        if (!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("The actual integration scene must be running.");
        gallery = Find(scene, "V11_SPEL_3D_AssetReuse");
        motion = gallery.GetComponent<ModeSelectSpellItOutTileMotion>();
        gate = Find(scene, "SpellItOutButton").GetComponent<ModeSelectSpellItOutHoverGate>();
        UnityEngine.UI.RawImage raw = Find(scene, "V11_Actual3DTiles_SPEL_RawImage").GetComponent<UnityEngine.UI.RawImage>();
        rt = raw.texture as RenderTexture;
        if (!motion || !gate || !rt || gallery.childCount != 5) throw new InvalidOperationException("Installed five-tile hover setup not found.");
        directory = Path.Combine("C:/Users/David/Documents/Codex/2026-11-01/he/outputs/SpellItOut_Hover_QA", "Run_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fffffff"));
        Directory.CreateDirectory(Path.Combine(directory, "Frames"));
        pixels = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
        report = new Report { scene = scene.path, startedUtc = DateTime.UtcNow.ToString("O"),
            screenWidth = Screen.width, screenHeight = Screen.height, rtWidth = rt.width, rtHeight = rt.height, rtAA = rt.antiAliasing };
        start = EditorApplication.timeSinceStartup;
        next = start + .25;
        previousHover = gate.IsHovered;
        stateStart = start;
        transition = 0;
        stateCaptured = false;
        EditorApplication.update += Observe;
        Debug.Log("[SIO HOVER QA] Recording 25 seconds. Move pointer outside, onto the SIO card, outside again, then re-enter. This recorder does not create pointer events or change motion.");
    }

    private static void Observe()
    {
        if (report == null) return;
        if (!EditorApplication.isPlaying || !motion || !gate || !gallery || !rt) { Finish("Scene or Play Mode changed"); return; }
        double now = EditorApplication.timeSinceStartup;
        if (now < next) return;
        next = now + .20;
        bool hover = gate.IsHovered;
        if (hover != previousHover)
        {
            previousHover = hover;
            stateStart = now;
            transition++;
            stateCaptured = false;
        }
        var sample = new Sample { elapsed = now - start, hovered = hover, running = motion.IsRunning, poses = new Pose[5] };
        for (int i = 0; i < 5; i++)
        {
            Transform slot = gallery.GetChild(i);
            sample.poses[i] = new Pose { name = slot.name, instanceId = slot.GetInstanceID(), position = slot.localPosition, rotation = slot.localRotation };
        }
        RenderTexture prior = RenderTexture.active;
        try
        {
            RenderTexture.active = rt;
            pixels.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0, false);
            pixels.Apply(false, false);
        }
        finally { RenderTexture.active = prior; }
        sample.textureFrame = "Frames/Preview_" + report.samples.Count.ToString("D3") + ".png";
        File.WriteAllBytes(Path.Combine(directory, sample.textureFrame), pixels.EncodeToPNG());
        report.samples.Add(sample);
        if (!stateCaptured && now - stateStart >= (hover ? .8 : .4))
        {
            string name = (hover ? "Hover_" : "Rest_") + transition.ToString("D2") + ".png";
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, name));
            report.screenshots.Add(name);
            stateCaptured = true;
        }
        if (now - start >= 25.0) Finish("25-second observation completed");
    }

    private static void Finish(string reason)
    {
        EditorApplication.update -= Observe;
        if (report != null)
        {
            report.finishReason = reason;
            File.WriteAllText(Path.Combine(directory, "HoverProof.json"), JsonUtility.ToJson(report, true));
            Debug.Log("[SIO HOVER QA] Evidence saved: " + directory);
        }
        report = null;
        if (pixels) UnityEngine.Object.Destroy(pixels);
        pixels = null;
    }

    private static Transform Find(Scene scene, string name)
    {
        Transform result = null;
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name)
                {
                    if (result != null) throw new InvalidOperationException("Duplicate target: " + name);
                    result = child;
                }
        if (!result) throw new InvalidOperationException("Missing target: " + name);
        return result;
    }
}
