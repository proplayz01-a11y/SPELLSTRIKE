#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Explicit, passive Play Mode observation of the active integration or preview scene.</summary>
public static class ModeSelectBackgroundAmbientQA
{
    [Serializable] private sealed class Pose
    {
        public string path;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public bool hasImage;
        public Color color;
        public bool raycastTarget;
    }
    [Serializable] private sealed class ButtonState
    {
        public string path;
        public bool active, interactable, hasRootImage, rootImageRaycast;
    }
    [Serializable] private sealed class Sample
    {
        public double elapsed;
        public int screenWidth, screenHeight;
        public bool hasGate, isHovered, isRunning;
        public List<Pose> ambient = new List<Pose>();
        public List<Pose> regression = new List<Pose>();
        public List<ButtonState> buttons = new List<ButtonState>();
    }
    [Serializable] private sealed class Report
    {
        public string scene, startedUtc, endedUtc, stopReason;
        public int ambientImages, ambientRaycastImages, cameras, lights, particleSystems, motionLayerCount;
        public string startupScene, mainMenuSceneName, modeSelectSceneName, wordMasterSceneName, spellItOutSceneName, wordExcavationSceneName;
        public bool useEditableSceneUI, rebuildUIOnStart;
        public int errors, exceptions;
        public List<string> errorFirstLines = new List<string>();
        public List<Sample> samples = new List<Sample>();
        public List<string> screenshots = new List<string>();
    }

    private const string OwnerName = "ModeSelect_BackgroundAmbient";
    private const string OutputRoot = @"C:\Users\David\Documents\Codex\2026-11-01\he\outputs\BackgroundAmbient_Unity_QA";
    private static readonly double[] ScreenshotTimes = { 0.5, 5.0, 10.0, 15.0 };
    private static Report report;
    private static Scene scene;
    private static Transform owner;
    private static string outputDirectory;
    private static double start, nextSample;
    private static int nextScreenshot;

    [MenuItem("SPELLSTRIKE/Minigames/Background Ambient/4 Capture Ambient QA - Play Mode")]
    private static void StartCapture()
    {
        if (System.Diagnostics.Process.GetProcessesByName("VALORANT-Win64-Shipping").Length > 0) return;
        if (report != null) { Debug.LogWarning("Ambient QA capture is already running."); return; }
        if (!EditorApplication.isPlaying) { Debug.LogWarning("Enter Play Mode before starting ambient QA."); return; }
        scene = SceneManager.GetActiveScene();
        if (scene.path != ModeSelectBackgroundAmbientInstallerV01.SourceScene && scene.path != ModeSelectBackgroundAmbientInstallerV01.PreviewScene)
        { Debug.LogWarning("Ambient QA requires the active integration or preview scene."); return; }
        owner = FindUniqueOwner(scene);
        if (!owner) { Debug.LogWarning("Ambient QA requires exactly one background ambient owner in the active scene."); return; }
        string stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
        outputDirectory = Path.Combine(OutputRoot, "Run_" + stamp + "_UTC");
        Directory.CreateDirectory(outputDirectory);
        report = new Report { scene = scene.path, startedUtc = DateTime.UtcNow.ToString("o") };
        GatherSceneFacts();
        start = EditorApplication.timeSinceStartup;
        nextSample = 0;
        nextScreenshot = 0;
        Application.logMessageReceived += OnLog;
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        Debug.Log("Ambient QA capture started: " + outputDirectory);
    }

    private static Transform FindUniqueOwner(Scene active)
    {
        Transform found = null;
        foreach (GameObject root in active.GetRootGameObjects())
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                if (item.name == OwnerName) { if (found) return null; found = item; }
        return found;
    }

    private static void GatherSceneFacts()
    {
        UnityEngine.UI.Image[] images = owner.GetComponentsInChildren<UnityEngine.UI.Image>(true);
        report.ambientImages = images.Length;
        foreach (UnityEngine.UI.Image image in images) if (image.raycastTarget) report.ambientRaycastImages++;
        ModeSelectBackgroundAmbientMotion motion = owner.GetComponent<ModeSelectBackgroundAmbientMotion>();
        if (!motion) motion = owner.GetComponentInChildren<ModeSelectBackgroundAmbientMotion>(true);
        report.motionLayerCount = motion ? motion.LayerCount : 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            report.cameras += root.GetComponentsInChildren<Camera>(true).Length;
            report.lights += root.GetComponentsInChildren<Light>(true).Length;
            report.particleSystems += root.GetComponentsInChildren<ParticleSystem>(true).Length;
            foreach (MinigamesSceneController c in root.GetComponentsInChildren<MinigamesSceneController>(true))
            {
                report.startupScene = c.startupScene.ToString();
                report.mainMenuSceneName = c.mainMenuSceneName; report.modeSelectSceneName = c.modeSelectSceneName;
                report.wordMasterSceneName = c.wordMasterSceneName; report.spellItOutSceneName = c.spellItOutSceneName;
                report.wordExcavationSceneName = c.wordExcavationSceneName;
                report.useEditableSceneUI = c.useEditableSceneUI; report.rebuildUIOnStart = c.rebuildUIOnStart;
            }
        }
    }

    private static void Tick()
    {
        if (report == null) return;
        if (!EditorApplication.isPlaying || !scene.IsValid() || SceneManager.GetActiveScene() != scene || !owner)
        { Finish("play stopped, active scene changed, or owner disappeared"); return; }
        double elapsed = EditorApplication.timeSinceStartup - start;
        if (elapsed >= nextSample && elapsed <= 20.2)
        {
            report.samples.Add(CaptureSample(elapsed));
            nextSample = elapsed + 0.2;
        }
        while (nextScreenshot < ScreenshotTimes.Length && elapsed >= ScreenshotTimes[nextScreenshot])
        {
            string filename = "screen_" + ScreenshotTimes[nextScreenshot].ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "s.png";
            ScreenCapture.CaptureScreenshot(Path.Combine(outputDirectory, filename));
            report.screenshots.Add(filename + " requested at " + elapsed.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) + "s");
            nextScreenshot++;
        }
        if (elapsed >= 20.0) Finish("completed");
    }

    private static Sample CaptureSample(double elapsed)
    {
        Sample result = new Sample { elapsed = elapsed, screenWidth = Screen.width, screenHeight = Screen.height };
        foreach (RectTransform rect in owner.GetComponentsInChildren<RectTransform>(true)) result.ambient.Add(Snapshot(rect));
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name.ToLowerInvariant();
                if (n.Contains("book") || n.Contains("pickaxe") || n == "v11_s_slot" || n == "v11_p_slot" || n == "v11_e_slot" || n == "v11_l_slot" || n == "v11_l2_slot")
                    result.regression.Add(Snapshot(t));
            }
            foreach (UnityEngine.UI.Button b in root.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                UnityEngine.UI.Image image = b.GetComponent<UnityEngine.UI.Image>();
                result.buttons.Add(new ButtonState { path = PathOf(b.transform), active = b.isActiveAndEnabled,
                    interactable = b.IsInteractable(), hasRootImage = image, rootImageRaycast = image && image.raycastTarget });
            }
            foreach (ModeSelectSpellItOutHoverGate gate in root.GetComponentsInChildren<ModeSelectSpellItOutHoverGate>(true))
            {
                result.hasGate = true; result.isHovered = gate.IsHovered;
                SerializedProperty binding = new SerializedObject(gate).FindProperty("motion");
                ModeSelectSpellItOutTileMotion motion = binding == null ? null : binding.objectReferenceValue as ModeSelectSpellItOutTileMotion;
                if (motion) result.isRunning = motion.IsRunning;
            }
        }
        return result;
    }

    private static Pose Snapshot(Transform t)
    {
        UnityEngine.UI.Image image = t.GetComponent<UnityEngine.UI.Image>();
        return new Pose { path = PathOf(t), position = t.localPosition, rotation = t.localRotation, scale = t.localScale,
            hasImage = image, color = image ? image.color : Color.clear, raycastTarget = image && image.raycastTarget };
    }
    private static string PathOf(Transform t)
    {
        string path = t.name;
        while (t.parent) { t = t.parent; path = t.name + "/" + path; }
        return path;
    }
    private static void OnLog(string condition, string stackTrace, LogType type)
    {
        if (report == null || (type != LogType.Error && type != LogType.Exception)) return;
        if (type == LogType.Error) report.errors++; else report.exceptions++;
        int end = condition.IndexOfAny(new[] { '\r', '\n' });
        report.errorFirstLines.Add(type + ": " + (end < 0 ? condition : condition.Substring(0, end)));
    }
    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode) Finish("play mode exited early");
    }
    private static void Finish(string reason)
    {
        EditorApplication.update -= Tick;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        Application.logMessageReceived -= OnLog;
        if (report == null) return;
        report.stopReason = reason; report.endedUtc = DateTime.UtcNow.ToString("o");
        File.WriteAllText(Path.Combine(outputDirectory, "AmbientQA.json"), JsonUtility.ToJson(report, true));
        report = null; owner = null;
        Debug.Log("Ambient QA capture ended: " + reason + "; " + outputDirectory);
    }
}
#endif
