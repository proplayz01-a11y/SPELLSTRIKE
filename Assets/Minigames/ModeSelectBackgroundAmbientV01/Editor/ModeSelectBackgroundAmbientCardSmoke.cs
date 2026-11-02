#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

/// <summary>Explicit component-level hover exercise; does not establish physical pointer hit testing.</summary>
public static class ModeSelectBackgroundAmbientCardSmoke
{
    [Serializable] private sealed class Pose
    {
        public string path; public int id; public Vector3 position, scale; public Quaternion rotation;
    }
    [Serializable] private sealed class Observation
    {
        public double elapsed; public List<Pose> poses = new List<Pose>();
        public bool sioHovered, sioRunning;
    }
    [Serializable] private sealed class CardResult
    {
        public string button; public Observation before, during, after;
        public bool motionObserved, allFiveSlotsMoved, sioStoppedAfterExit;
        public string screenshot;
    }
    [Serializable] private sealed class Report
    {
        public string testMethod = "explicit Unity EventSystem handlers; OS pointer hit path not verified by this fixture";
        public string scene, startedUtc, endedUtc, finishReason;
        public List<CardResult> cards = new List<CardResult>();
        public List<string> errors = new List<string>();
    }

    private static readonly string[] Names = { "WordMasterButton", "WordExcavationButton", "SpellItOutButton" };
    private const string OutputRoot = @"C:\Users\David\Documents\Codex\2026-11-01\he\outputs\BackgroundAmbient_CardSmoke";
    private static Scene scene;
    private static UnityEngine.UI.Button[] buttons;
    private static Transform gallery;
    private static ModeSelectSpellItOutHoverGate gate;
    private static ModeSelectSpellItOutTileMotion motion;
    private static Report report;
    private static CardResult current;
    private static string directory;
    private static int index, stage;
    private static double stageStart;
    private static bool entered;

    [MenuItem("SPELLSTRIKE/Minigames/Background Ambient/6 Runtime Card Hover Smoke - Play Mode")]
    private static void Begin()
    {
        if (report != null || !EditorApplication.isPlaying || ValorantRunning()) { Debug.LogWarning("Card smoke requires Play Mode, no active capture, and VALORANT closed."); return; }
        scene = SceneManager.GetActiveScene();
        if (scene.path != ModeSelectBackgroundAmbientInstallerV01.SourceScene || !EventSystem.current)
        { Debug.LogWarning("Card smoke requires the active integration scene and an EventSystem."); return; }
        buttons = new UnityEngine.UI.Button[3];
        for (int i = 0; i < Names.Length; i++)
        {
            Transform t = Unique(Names[i]);
            buttons[i] = t ? t.GetComponent<UnityEngine.UI.Button>() : null;
            if (!buttons[i] || !buttons[i].isActiveAndEnabled || !buttons[i].IsInteractable())
            { Debug.LogWarning("Card smoke requires each unique active interactable card button."); return; }
        }
        gallery = Unique("V11_SPEL_3D_AssetReuse");
        gate = buttons[2].GetComponent<ModeSelectSpellItOutHoverGate>();
        motion = gallery ? gallery.GetComponent<ModeSelectSpellItOutTileMotion>() : null;
        if (!gallery || gallery.childCount != 5 || !gate || !motion)
        { Debug.LogWarning("Card smoke requires the SIO gate and five-slot gallery."); return; }
        directory = Path.Combine(OutputRoot, "Run_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fffffff") + "_UTC");
        Directory.CreateDirectory(directory);
        report = new Report { scene = scene.path, startedUtc = DateTime.UtcNow.ToString("o") };
        index = 0; stage = 0; entered = false;
        Application.logMessageReceived += OnLog;
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += OnPlayMode;
        Debug.Log("Card smoke started; synthetic EventSystem handlers, not OS pointer proof: " + directory);
    }

    private static Transform Unique(string name)
    {
        Transform found = null;
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) { if (found) return null; found = t; }
        return found;
    }
    private static bool ValorantRunning()
    {
        Process[] processes = Process.GetProcessesByName("VALORANT-Win64-Shipping");
        bool running = processes.Length != 0;
        foreach (Process process in processes) process.Dispose();
        return running;
    }
    private static void Tick()
    {
        if (report == null) return;
        try
        {
            if (!EditorApplication.isPlaying || SceneManager.GetActiveScene() != scene || !EventSystem.current || !buttons[index] || !gallery || !gate || !motion || ValorantRunning())
            { Finish("play stopped, scene changed, required object missing, or VALORANT running"); return; }
            double now = EditorApplication.timeSinceStartup;
            if (stage == 0)
            {
                current = new CardResult { button = Names[index], before = Observe(0) };
                report.cards.Add(current);
                ExecuteEvents.Execute(buttons[index].gameObject, new PointerEventData(EventSystem.current) { pointerId = -1 }, ExecuteEvents.pointerEnterHandler);
                entered = true; stageStart = now; stage = 1;
            }
            else if (stage == 1 && now - stageStart >= 0.8)
            {
                current.during = Observe(now - stageStart);
                current.screenshot = Names[index] + "_hover.png";
                ScreenCapture.CaptureScreenshot(Path.Combine(directory, current.screenshot));
                stage = 2;
            }
            else if (stage == 2 && now - stageStart >= 1.6)
            {
                Observation later = Observe(now - stageStart);
                current.motionObserved = ChangedAny(current.before, current.during) || ChangedAny(current.before, later);
                if (index == 2) current.allFiveSlotsMoved = AllSlotsMoved(current.before, current.during, later);
                ExitCurrent(); stageStart = now; stage = 3;
            }
            else if (stage == 3 && now - stageStart >= 0.4)
            {
                current.after = Observe(now - stageStart);
                if (index == 2) current.sioStoppedAfterExit = !gate.IsHovered && !motion.IsRunning;
                index++; stage = 0;
                if (index == buttons.Length) Finish("completed");
            }
        }
        catch (Exception ex)
        {
            report.errors.Add(ex.GetType().Name + ": " + FirstLine(ex.Message));
            Finish("exception");
        }
    }
    private static Observation Observe(double elapsed)
    {
        Observation observation = new Observation { elapsed = elapsed, sioHovered = gate.IsHovered, sioRunning = motion.IsRunning };
        foreach (Transform t in buttons[index].GetComponentsInChildren<Transform>(true)) observation.poses.Add(Snapshot(t));
        if (index == 2) for (int i = 0; i < gallery.childCount; i++) observation.poses.Add(Snapshot(gallery.GetChild(i)));
        return observation;
    }
    private static Pose Snapshot(Transform t)
    {
        string path = t.name; Transform p = t.parent;
        while (p) { path = p.name + "/" + path; p = p.parent; }
        return new Pose { path = path, id = t.GetInstanceID(), position = t.localPosition, rotation = t.localRotation, scale = t.localScale };
    }
    private static bool Changed(Pose a, Pose b) => (a.position - b.position).sqrMagnitude > 1e-10f || Quaternion.Angle(a.rotation, b.rotation) > 0.001f || (a.scale - b.scale).sqrMagnitude > 1e-10f;
    private static bool ChangedAny(Observation a, Observation b)
    {
        foreach (Pose p in a.poses) foreach (Pose q in b.poses) if (p.id == q.id && Changed(p, q)) return true;
        return false;
    }
    private static bool AllSlotsMoved(Observation before, Observation during, Observation later)
    {
        for (int i = 0; i < gallery.childCount; i++)
        {
            int id = gallery.GetChild(i).GetInstanceID(); Pose a = before.poses.Find(p => p.id == id);
            Pose b = during.poses.Find(p => p.id == id); Pose c = later.poses.Find(p => p.id == id);
            if (a == null || b == null || c == null || (!Changed(a, b) && !Changed(a, c))) return false;
        }
        return true;
    }
    private static void ExitCurrent()
    {
        if (!entered) return;
        entered = false;
        if (buttons != null && index < buttons.Length && buttons[index] && EventSystem.current)
            ExecuteEvents.Execute(buttons[index].gameObject, new PointerEventData(EventSystem.current) { pointerId = -1 }, ExecuteEvents.pointerExitHandler);
    }
    private static string FirstLine(string text) { int i = text.IndexOfAny(new[] { '\r', '\n' }); return i < 0 ? text : text.Substring(0, i); }
    private static void OnLog(string text, string trace, LogType type)
    { if (report != null && (type == LogType.Error || type == LogType.Exception)) report.errors.Add(type + ": " + FirstLine(text)); }
    private static void OnPlayMode(PlayModeStateChange state) { if (state == PlayModeStateChange.ExitingPlayMode) Finish("play mode exited early"); }
    private static void Finish(string reason)
    {
        EditorApplication.update -= Tick; EditorApplication.playModeStateChanged -= OnPlayMode; Application.logMessageReceived -= OnLog;
        try { ExitCurrent(); } catch (Exception ex) { if (report != null) report.errors.Add(ex.GetType().Name + ": " + FirstLine(ex.Message)); }
        if (report == null) return;
        report.finishReason = reason; report.endedUtc = DateTime.UtcNow.ToString("o");
        File.WriteAllText(Path.Combine(directory, "CardSmoke.json"), JsonUtility.ToJson(report, true));
        report = null; Debug.Log("Card smoke ended: " + reason + "; " + directory);
    }
}
#endif
