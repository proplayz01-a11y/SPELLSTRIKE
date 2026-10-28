#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

/// <summary>Four actual GPU camera stills. This is not a Play Mode or audio recording.</summary>
internal static class Panel04CorrectionV06Preview
{
    [Serializable] private sealed class ShotRecord
    {
        public string id;
        public float time;
        public Vector3 expectedEyeUnity;
        public Vector3 actualEyeUnity;
        public Vector3 expectedTargetUnity;
        public float verticalFov;
        public float positionErrorMeters;
        public float rotationErrorDegrees;
        public string capturePath;
        public int captureBytes;
        public string captureSha256;
        public float meanLuminance;
        public float maximumLuminance;
        public bool cameraMatchesManifest;
    }
    [Serializable] private sealed class PreviewReport
    {
        public string scene;
        public string sourceGlb;
        public string sourceGlbSha256;
        public string sourceBlenderScene;
        public string manifest;
        public string manifestSha256;
        public string capturedAtUTC;
        public int width;
        public int height;
        public float duration;
        public int timelineTracks;
        public int renderers;
        public bool allFourGpuCapturesComplete;
        public bool allFourCameraChecksPass;
        public bool priorSceneSetupRestored;
        public string expectedPriorActiveScenePath;
        public string actualActiveScenePathAfterCapture;
        public string sceneRestorationNote;
        public ShotRecord[] shots;
        public string[] notes;
    }

    [MenuItem("SPELLSTRIKE/Cinematics/Panel 04/Capture Corrected P04 v06")]
    public static void CaptureFromVisibleMenu()
    {
        SceneSetup[] previous = null;
        PreviewReport report = null;
        string reportPath = null;
        try
        {
            Panel04CorrectionV06Shared.GuardEditor();
            var manifest = Panel04CorrectionV06Shared.ReadManifest();
            if (!File.Exists(Panel04CorrectionV06Shared.FullPath(Panel04CorrectionV06Shared.ScenePath)))
                throw new FileNotFoundException("Build the NEW v06 scene first.");
            string outputDirectory = Panel04CorrectionV06Shared.FullPath(Panel04CorrectionV06Shared.ReviewDirectory);
            reportPath = Path.Combine(outputDirectory, "P04_Corrected_UnityPreviewQA_v06.json");
            var capturePaths = manifest.shots.Select(s =>
                Path.Combine(outputDirectory, "P04_Unity_" + s.id + "_v06.png")).ToArray();
            foreach (string path in capturePaths.Concat(new[] { reportPath }))
                if (File.Exists(path)) throw new IOException("Refusing review-file overwrite: " + path);
            // A path-based setup snapshot cannot reconstruct an unsaved Untitled scene.
            // This capture command never saves it or unloads it to work around that limitation.
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene loaded = SceneManager.GetSceneAt(i);
                if (loaded.isLoaded && string.IsNullOrEmpty(loaded.path))
                    throw new InvalidOperationException("A loaded scene has no saved path: " + loaded.name +
                        ". Open a saved clean scene before capture; the Untitled scene has not been changed.");
            }

            previous = EditorSceneManager.GetSceneManagerSetup();
            // Single-scene capture prevents other loaded scene geometry/lights contaminating stills.
            Scene scene = EditorSceneManager.OpenScene(Panel04CorrectionV06Shared.ScenePath, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            var camera = RequireOne(roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true)), "camera");
            var director = RequireOne(roots.SelectMany(r => r.GetComponentsInChildren<PlayableDirector>(true)), "director");
            var shotLock = RequireOne(roots.SelectMany(r => r.GetComponentsInChildren<Panel04A04ShotLockV05>(true)), "shot lock");
            if (shotLock.director != director || director.playableAsset == null ||
                AssetDatabase.GetAssetPath(director.playableAsset) != Panel04CorrectionV06Shared.TimelinePath)
                throw new InvalidOperationException("Scene is not bound to the new v06 Timeline.");
            var timeline = director.playableAsset as TimelineAsset;
            if (timeline == null || timeline.GetOutputTracks().Count() != 2 ||
                Math.Abs(timeline.duration - Panel04CorrectionV06Shared.Duration) > .05)
                throw new InvalidOperationException("Expected the two-track, 10-second v06 Timeline.");
            if (shotLock.cutTimes == null || shotLock.cutTimes.Length != 4 ||
                shotLock.cutTimes.Where((t, i) => Mathf.Abs(t - Panel04CorrectionV06Shared.CutTimes[i]) > .0001f).Any())
                throw new InvalidOperationException("Unexpected four-shot cut times.");
            int rendererCount = roots.Sum(r => r.GetComponentsInChildren<Renderer>(true).Length);
            Directory.CreateDirectory(outputDirectory);
            director.RebuildGraph();
            var records = new List<ShotRecord>();
            for (int i = 0; i < 4; i++)
            {
                float end = i == 3 ? Panel04CorrectionV06Shared.Duration : Panel04CorrectionV06Shared.CutTimes[i + 1];
                float time = (Panel04CorrectionV06Shared.CutTimes[i] + end) * .5f;
                director.time = time;
                director.Evaluate();
                if (!shotLock.ApplyNow() || shotLock.CurrentShotIndex != i)
                    throw new InvalidOperationException("Shot lock did not evaluate manifest shot " + (i + 1));
                Panel04CorrectionV06Shared.CheckCamera(camera, shotLock, manifest, i);
                Vector3 expectedEye = Panel04CorrectionV06Shared.ToUnity(manifest.shots[i].eye);
                Vector3 expectedTarget = Panel04CorrectionV06Shared.ToUnity(manifest.shots[i].target);
                var record = new ShotRecord {
                    id = manifest.shots[i].id, time = time,
                    expectedEyeUnity = expectedEye, actualEyeUnity = camera.transform.position,
                    expectedTargetUnity = expectedTarget, verticalFov = camera.fieldOfView,
                    positionErrorMeters = Vector3.Distance(camera.transform.position, expectedEye),
                    rotationErrorDegrees = Quaternion.Angle(camera.transform.rotation,
                        Quaternion.LookRotation(expectedTarget - expectedEye, Vector3.up)),
                    capturePath = Panel04CorrectionV06Shared.ReviewDirectory + "/" + Path.GetFileName(capturePaths[i]),
                    cameraMatchesManifest = true
                };
                Capture(camera, capturePaths[i], record);
                records.Add(record);
            }
            report = new PreviewReport {
                scene = Panel04CorrectionV06Shared.ScenePath,
                sourceGlb = Panel04CorrectionV06Shared.SourceGlb,
                sourceGlbSha256 = Hash(File.ReadAllBytes(Panel04CorrectionV06Shared.FullPath(Panel04CorrectionV06Shared.SourceGlb))),
                sourceBlenderScene = manifest.sourceBlenderScene,
                manifest = Panel04CorrectionV06Shared.ManifestPath,
                manifestSha256 = Hash(File.ReadAllBytes(Panel04CorrectionV06Shared.FullPath(Panel04CorrectionV06Shared.ManifestPath))),
                capturedAtUTC = DateTime.UtcNow.ToString("o"), width = 960, height = 540,
                duration = Panel04CorrectionV06Shared.Duration, timelineTracks = 2, renderers = rendererCount,
                allFourGpuCapturesComplete = records.Count == 4,
                allFourCameraChecksPass = records.All(r => r.cameraMatchesManifest),
                shots = records.ToArray(),
                notes = new[] {
                    "Actual Unity GPU Camera.Render stills from the saved new v06 scene; no scene save is performed during capture.",
                    "Static character poses; no actor animation, voice/audio, Play Mode playback, or device validation is claimed.",
                    "OnGUI narration is not included by Camera.Render; these PNGs are scene-image evidence.",
                    "Transform and per-shot vertical FOV are checked against the Blender-space camera manifest."
                }
            };
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("P04 v06 capture stopped", exception.Message +
                "\nAny completed NEW PNGs remain for inspection; nothing is overwritten.", "OK");
        }
        finally
        {
            if (previous != null)
            {
                Exception restoreException = null;
                try
                {
                    EditorSceneManager.RestoreSceneManagerSetup(previous);
                }
                catch (Exception exception)
                {
                    restoreException = exception;
                }
                // Verify actual loaded paths/active flags even if the restoration API reported
                // an error after reaching the requested setup. Never mark success on return alone.
                string expectedPath = Panel04CorrectionV06Shared.PreviousActivePath(previous);
                Scene actualActive = SceneManager.GetActiveScene();
                string actualPath = actualActive.IsValid() ? actualActive.path : "";
                bool restored = actualActive.IsValid() && actualActive.isLoaded &&
                    Panel04CorrectionV06Shared.SamePath(actualPath, expectedPath) &&
                    Panel04CorrectionV06Shared.SceneSetupMatches(previous, EditorSceneManager.GetSceneManagerSetup());
                string restorationNote = restored
                    ? "Verified previous loaded scene paths, loaded states and active flags."
                    : "Previous scene setup differs after capture; no previous scene was saved.";
                if (restoreException != null) restorationNote += " API message: " + restoreException.Message;
                if (report != null)
                {
                    report.priorSceneSetupRestored = restored;
                    report.expectedPriorActiveScenePath = expectedPath;
                    report.actualActiveScenePathAfterCapture = actualPath;
                    report.sceneRestorationNote = restorationNote;
                }
                if (restored)
                    Debug.Log("[P04 v06] Previous scene setup verified. actualActivePath='" + actualPath + "'.");
                else
                {
                    Debug.LogError("[P04 v06] " + restorationNote + " Expected active path='" +
                        expectedPath + "', actualActivePath='" + actualPath + "'.");
                    EditorUtility.DisplayDialog("P04 v06 scene restoration", restorationNote +
                        "\nExpected active path: " + expectedPath + "\nActual active path: " + actualPath, "OK");
                }
            }
        }
        if (report == null) return;
        try
        {
            WriteNew(reportPath, System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(report, true)));
            Debug.Log("[P04 v06] Four GPU stills and report created at " +
                Panel04CorrectionV06Shared.ReviewDirectory + "; prior setup restored=" + report.priorSceneSetupRestored);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("P04 v06 report write stopped", exception.Message, "OK");
        }
    }

    private static T RequireOne<T>(IEnumerable<T> objects, string description)
    {
        T[] found = objects.ToArray();
        if (found.Length != 1) throw new InvalidOperationException("Expected one " + description + "; found " + found.Length);
        return found[0];
    }

    private static void Capture(Camera camera, string path, ShotRecord record)
    {
        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture target = null;
        Texture2D image = null;
        try
        {
            target = new RenderTexture(960, 540, 24, RenderTextureFormat.ARGB32);
            if (!target.Create()) throw new InvalidOperationException("Cannot create GPU render target.");
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image = new Texture2D(960, 540, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
            image.Apply(false);
            Color32[] pixels = image.GetPixels32();
            double luminanceSum = 0;
            float maximum = 0;
            foreach (Color32 pixel in pixels)
            {
                float luminance = (.2126f * pixel.r + .7152f * pixel.g + .0722f * pixel.b) / 255f;
                luminanceSum += luminance;
                maximum = Mathf.Max(maximum, luminance);
            }
            record.meanLuminance = (float)(luminanceSum / pixels.Length);
            record.maximumLuminance = maximum;
            if (record.meanLuminance < .005f || maximum < .02f)
                throw new InvalidOperationException("GPU capture is effectively black: " + record.id);
            byte[] png = image.EncodeToPNG();
            if (png.Length < 20000) throw new InvalidOperationException("GPU PNG is unexpectedly small: " + record.id);
            WriteNew(path, png);
            record.captureBytes = png.Length;
            record.captureSha256 = Hash(png);
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
            if (image != null) UnityEngine.Object.DestroyImmediate(image);
        }
    }

    private static string Hash(byte[] bytes)
    {
        using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
    }

    private static void WriteNew(string path, byte[] bytes)
    {
        using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            stream.Write(bytes, 0, bytes.Length);
    }
}
#endif
