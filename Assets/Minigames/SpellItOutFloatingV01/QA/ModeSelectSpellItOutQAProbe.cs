#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Evidence capture for the isolated editor QA scene only.</summary>
public sealed class ModeSelectSpellItOutQAProbe : MonoBehaviour
{
    [SerializeField] private ModeSelectSpellItOutTileMotion motion;
    [SerializeField] private Transform gallery;
    [SerializeField] private Camera previewCamera;
    [SerializeField] private UnityEngine.UI.RawImage rawImage;
    [SerializeField] private string outputDirectory;
    [SerializeField, Tooltip("Historical idle capture only. Leave disabled for hover-only validation; use Capture Hover Proof instead.")]
    private bool captureLegacyIdle = false;

    [Serializable] private sealed class Pose { public string name; public int instanceId; public Vector3 position; public Vector3 rotation; }
    [Serializable] private sealed class RendererInfo { public string name; public string hierarchy; public string mesh; public string meshAssetPath; public int sharedMeshInstanceId; public int vertices; public int triangles; public string[] materials; }
    [Serializable] private sealed class Frame { public int index; public double elapsedSeconds; public Pose[] slots; public Vector3 viewportMin; public Vector3 viewportMax; public int outsideViewportRendererCount; }
    [Serializable] private sealed class Report
    {
        public bool benchmarkNotRepresentative = true;
        public string limitation = "Editor QA readback and PNG encoding affect timing; no Android performance claim.";
        public string runUtc; public int gameViewWidth; public int gameViewHeight;
        public int rtWidth; public int rtHeight; public int rtAntiAliasing;
        public float cameraNear; public float cameraFar; public bool cameraOrthographic; public float cameraOrthographicSize; public int cameraCullingMask;
        public string[] screenshotFiles; public RendererInfo[] renderers; public Frame[] frames;
        public float alphaZeroFraction; public float alphaPartialFraction; public float alphaNonzeroFraction; public byte[] alphaCorners;
        public Pose[] restPoses; public Pose[] afterDisableEnable; public bool lifecycleRestored;
        public Vector3[] minimumPositions; public Vector3[] maximumPositions;
        public int errorCount; public int exceptionCount; public string[] errors;
    }

    private readonly List<string> errors = new List<string>();
    private int errorCount, exceptionCount;
    private Texture2D readback;

    public void Configure(ModeSelectSpellItOutTileMotion source, Transform root, Camera camera, UnityEngine.UI.RawImage image, string directory)
    {
        motion = source; gallery = root; previewCamera = camera; rawImage = image; outputDirectory = directory;
    }

    private void OnEnable() { Application.logMessageReceived += OnLog; }
    private void OnDisable()
    {
        Application.logMessageReceived -= OnLog;
        if (readback) Destroy(readback);
        readback = null;
    }
    private void OnLog(string condition, string stackTrace, LogType type)
    {
        // Unity Remote failures can embed an entire environment-variable dump.
        // Preserve the error identity/device reason without exporting that dump.
        string safe = condition.Split('\n')[0];
        if (condition.Contains("no devices/emulators found")) safe += " (adb: no devices/emulators found)";
        if (type == LogType.Error || type == LogType.Assert) { errorCount++; errors.Add(safe); }
        else if (type == LogType.Exception) { exceptionCount++; errors.Add(safe); }
    }

    private IEnumerator Start()
    {
        if (!Application.isPlaying || !captureLegacyIdle) yield break;
        if (!motion || !gallery || !previewCamera || !rawImage || !(rawImage.texture is RenderTexture))
        {
            Debug.LogError("Spell It Out QA probe requires motion, gallery, camera and a RawImage RenderTexture.");
            yield break;
        }
        var rt = (RenderTexture)rawImage.texture;
        string stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fffffff");
        string run = Path.Combine(outputDirectory, "Run_" + stamp);
        string framesDir = Path.Combine(run, "Frames");
        Directory.CreateDirectory(framesDir);
        readback = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
        var renderers = gallery.GetComponentsInChildren<Renderer>(true);
        var slots = FindSlots();
        // Let unrelated Editor/Unity Remote startup work settle before timing
        // samples. Never compress delayed samples into catch-up frame bursts.
        yield return new WaitForSecondsRealtime(2f);
        var report = new Report
        {
            runUtc = DateTime.UtcNow.ToString("O"), gameViewWidth = Screen.width, gameViewHeight = Screen.height,
            rtWidth = rt.width, rtHeight = rt.height, rtAntiAliasing = rt.antiAliasing,
            cameraNear = previewCamera.nearClipPlane, cameraFar = previewCamera.farClipPlane,
            cameraOrthographic = previewCamera.orthographic, cameraOrthographicSize = previewCamera.orthographicSize,
            cameraCullingMask = previewCamera.cullingMask, renderers = Describe(renderers),
            frames = new Frame[48], minimumPositions = new Vector3[slots.Length], maximumPositions = new Vector3[slots.Length],
            screenshotFiles = new[] { "GameView_1s.png", "GameView_6s.png" }
        };
        for (int i = 0; i < slots.Length; i++) { report.minimumPositions[i] = Vector3.positiveInfinity; report.maximumPositions[i] = Vector3.negativeInfinity; }
        double start = Time.realtimeSinceStartupAsDouble;
        double nextCapture = start;
        bool shot1 = false, shot6 = false;
        for (int f = 0; f < 48; f++)
        {
            while (Time.realtimeSinceStartupAsDouble < nextCapture) yield return null;
            yield return new WaitForEndOfFrame();
            double elapsed = Time.realtimeSinceStartupAsDouble - start;
            var previous = RenderTexture.active;
            try
            {
                RenderTexture.active = rt;
                readback.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0, false);
                readback.Apply(false, false);
            }
            finally { RenderTexture.active = previous; }
            if (f == 0) MeasureAlpha(report, readback.GetPixels32(), rt.width, rt.height);
            File.WriteAllBytes(Path.Combine(framesDir, "Preview_" + f.ToString("D3") + ".png"), readback.EncodeToPNG());
            nextCapture = Time.realtimeSinceStartupAsDouble + 1.0 / 6.0;
            var frame = new Frame { index = f, elapsedSeconds = elapsed, slots = GetPoses(slots) };
            MeasureViewport(renderers, frame);
            report.frames[f] = frame;
            for (int i = 0; i < slots.Length; i++) if (slots[i])
            {
                report.minimumPositions[i] = Vector3.Min(report.minimumPositions[i], slots[i].localPosition);
                report.maximumPositions[i] = Vector3.Max(report.maximumPositions[i], slots[i].localPosition);
            }
            if (!shot1 && elapsed >= 1.0) { ScreenCapture.CaptureScreenshot(Path.Combine(run, report.screenshotFiles[0])); shot1 = true; }
            if (!shot6 && elapsed >= 6.0) { ScreenCapture.CaptureScreenshot(Path.Combine(run, report.screenshotFiles[1])); shot6 = true; }
            // Screenshot writing is deferred by Unity; advance before checking files.
            yield return null;
        }
        motion.PauseAndRestore();
        report.restPoses = GetPoses(slots);
        motion.enabled = false;
        motion.enabled = true;
        motion.PauseAndRestore();
        report.afterDisableEnable = GetPoses(slots);
        report.lifecycleRestored = SamePoses(report.restPoses, report.afterDisableEnable);
        motion.Resume();
        for (int i = 0; i < report.screenshotFiles.Length; i++)
            if (!File.Exists(Path.Combine(run, report.screenshotFiles[i]))) errors.Add("Screenshot file not yet found: " + report.screenshotFiles[i]);
        report.errorCount = errorCount; report.exceptionCount = exceptionCount; report.errors = errors.ToArray();
        File.WriteAllText(Path.Combine(run, "runtimeQA.json"), JsonUtility.ToJson(report, true));
        Debug.Log("Spell It Out QA capture complete: " + run);
    }

    // Gallery contains exactly the five separately moving slot transforms as direct children.
    private Transform[] FindSlots()
    {
        var result = new Transform[gallery.childCount];
        for (int i = 0; i < result.Length; i++) result[i] = gallery.GetChild(i);
        return result;
    }
    private static Pose[] GetPoses(Transform[] slots)
    {
        var result = new Pose[slots.Length];
        for (int i = 0; i < slots.Length; i++) if (slots[i])
            result[i] = new Pose { name = slots[i].name, instanceId = slots[i].GetInstanceID(), position = slots[i].localPosition, rotation = slots[i].localEulerAngles };
        return result;
    }
    private static bool SamePoses(Pose[] a, Pose[] b)
    {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] == null || b[i] == null || (a[i].position - b[i].position).sqrMagnitude > 1e-10f || Quaternion.Angle(Quaternion.Euler(a[i].rotation), Quaternion.Euler(b[i].rotation)) > 0.001f) return false;
        }
        return true;
    }
    private static RendererInfo[] Describe(Renderer[] renderers)
    {
        var result = new RendererInfo[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i]; var filter = r.GetComponent<MeshFilter>();
            Mesh mesh = filter ? filter.sharedMesh : (r is SkinnedMeshRenderer skinned ? skinned.sharedMesh : null);
            var mats = r.sharedMaterials; var names = new string[mats.Length];
            for (int j = 0; j < mats.Length; j++) names[j] = mats[j] ? mats[j].name + " (instance " + mats[j].GetInstanceID() + ")" : "null";
            int triangles = 0;
            if (mesh) for (int sub = 0; sub < mesh.subMeshCount; sub++)
                if (mesh.GetTopology(sub) == MeshTopology.Triangles) triangles += (int)(mesh.GetIndexCount(sub) / 3);
            result[i] = new RendererInfo { name = r.name, hierarchy = UnityEditor.AnimationUtility.CalculateTransformPath(r.transform, null),
                mesh = mesh ? mesh.name : "", meshAssetPath = mesh ? UnityEditor.AssetDatabase.GetAssetPath(mesh) : "",
                sharedMeshInstanceId = mesh ? mesh.GetInstanceID() : 0, vertices = mesh ? mesh.vertexCount : 0, triangles = triangles, materials = names };
        }
        return result;
    }
    private void MeasureViewport(Renderer[] renderers, Frame frame)
    {
        Vector3 min = Vector3.positiveInfinity, max = Vector3.negativeInfinity;
        int outside = 0, visible = 0;
        foreach (var r in renderers)
        {
            if (!r || !r.enabled || !r.gameObject.activeInHierarchy || (previewCamera.cullingMask & (1 << r.gameObject.layer)) == 0) continue;
            visible++; var b = r.bounds; bool outOfBounds = false;
            for (int n = 0; n < 8; n++)
            {
                var p = b.center + Vector3.Scale(b.extents, new Vector3((n & 1) == 0 ? -1 : 1, (n & 2) == 0 ? -1 : 1, (n & 4) == 0 ? -1 : 1));
                var v = previewCamera.WorldToViewportPoint(p); min = Vector3.Min(min, v); max = Vector3.Max(max, v);
                if (v.x < 0 || v.x > 1 || v.y < 0 || v.y > 1 || v.z < previewCamera.nearClipPlane || v.z > previewCamera.farClipPlane) outOfBounds = true;
            }
            if (outOfBounds) outside++;
        }
        frame.viewportMin = visible > 0 ? min : Vector3.zero; frame.viewportMax = visible > 0 ? max : Vector3.zero; frame.outsideViewportRendererCount = outside;
    }
    private static void MeasureAlpha(Report report, Color32[] pixels, int width, int height)
    {
        int zero = 0, partial = 0, nonzero = 0;
        foreach (var p in pixels) { if (p.a == 0) zero++; else { nonzero++; if (p.a < 255) partial++; } }
        report.alphaZeroFraction = (float)zero / pixels.Length; report.alphaPartialFraction = (float)partial / pixels.Length; report.alphaNonzeroFraction = (float)nonzero / pixels.Length;
        report.alphaCorners = new[] { pixels[0].a, pixels[width - 1].a, pixels[(height - 1) * width].a, pixels[height * width - 1].a };
    }
}
#endif
