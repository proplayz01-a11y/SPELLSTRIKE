#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

/// <summary>Visible-menu-only authoring of new P04 v06 assets. Earlier scenes/assets are read-only.</summary>
internal static class Panel04CorrectionV06Builder
{
    [MenuItem("SPELLSTRIKE/Cinematics/Panel 04/Build Corrected P04 v06")]
    public static void BuildFromVisibleMenu()
    {
        Scene created = default;
        SceneSetup[] previous = null;
        Scene previousActive = default;
        bool saved = false;
        try
        {
            Panel04CorrectionV06Shared.GuardEditor();
            var manifest = Panel04CorrectionV06Shared.ReadManifest();
            Panel04CorrectionV06Shared.RequireNewAsset(Panel04CorrectionV06Shared.ScenePath);
            Panel04CorrectionV06Shared.RequireNewAsset(Panel04CorrectionV06Shared.TimelinePath);
            Panel04CorrectionV06Shared.RequireNewAsset(Panel04CorrectionV06Shared.ClipPath);
            if (Directory.Exists(Panel04CorrectionV06Shared.FullPath(Panel04CorrectionV06Shared.TimelineFolder)))
                throw new IOException("The v06 Timeline folder already exists. No output will be overwritten.");
            if (!File.Exists(Panel04CorrectionV06Shared.FullPath(Panel04CorrectionV06Shared.SourceGlb)))
                throw new FileNotFoundException("Corrected v02 GLB is missing.", Panel04CorrectionV06Shared.SourceGlb);

            previous = EditorSceneManager.GetSceneManagerSetup();
            previousActive = SceneManager.GetActiveScene();
            AssetDatabase.ImportAsset(Panel04CorrectionV06Shared.SourceGlb, ImportAssetOptions.ForceUpdate);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Panel04CorrectionV06Shared.SourceGlb);
            if (prefab == null)
                throw new InvalidOperationException("glTFast has not imported the new v02 GLB as a GameObject.");
            if (!AssetDatabase.IsValidFolder(Panel04CorrectionV06Shared.TimelineParent))
                throw new DirectoryNotFoundException(Panel04CorrectionV06Shared.TimelineParent);

            // Additive authoring leaves the user's existing clean scene objects loaded and untouched.
            created = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(created);
            var stageRoot = new GameObject("P04_V06_CORRECTED_STATIC_RUIN");
            var stage = (GameObject)PrefabUtility.InstantiatePrefab(prefab, created);
            if (stage == null) throw new InvalidOperationException("Cannot instantiate the corrected GLB.");
            stage.name = "P04_V06_CORRECTED_SOURCE__STATIC_POSES";
            stage.transform.SetParent(stageRoot.transform, false);
            stage.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            stage.transform.localScale = Vector3.one;
            Panel04CorrectionV06Shared.ValidateStage(stage);

            var transforms = stage.GetComponentsInChildren<Transform>(true);
            foreach (string required in manifest.requiredObjectNames ?? Array.Empty<string>())
                RequireSingle(transforms, required);
            // There is no inherited automatic wall removal. Hides require explicit manifest names.
            foreach (string hidden in manifest.hideSourceObjects ?? Array.Empty<string>())
                RequireSingle(transforms, hidden).gameObject.SetActive(false);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.29f, .30f, .36f);
            RenderSettings.fog = false;
            var lighting = new GameObject("P04_V06_LIGHTING__EDITABLE");
            var key = new GameObject("P04_V06_ColdDawn");
            key.transform.SetParent(lighting.transform, false);
            key.transform.rotation = Quaternion.Euler(49f, -38f, 0f);
            var directional = key.AddComponent<Light>();
            directional.type = LightType.Directional;
            directional.color = new Color(.69f, .78f, 1f);
            directional.intensity = 1.35f;
            var bounce = new GameObject("P04_V06_WarmRuinBounce");
            bounce.transform.SetParent(lighting.transform, false);
            bounce.transform.position = Panel04CorrectionV06Shared.ToUnity(new Vector3(-.4f, -2.1f, 2.2f));
            var fill = bounce.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.color = new Color(1f, .59f, .35f);
            fill.range = 7f;
            fill.intensity = 2f;

            var cameraGo = new GameObject("P04_V06_CAMERA__FOUR_CORRECTED_SHOTS");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.065f, .08f, .12f);
            camera.nearClipPlane = .07f;
            camera.farClipPlane = 300f;
            camera.allowHDR = false;
            camera.fieldOfView = manifest.shots[0].verticalFov;
            cameraGo.AddComponent<AudioListener>();
            var animator = cameraGo.AddComponent<Animator>();
            animator.applyRootMotion = false;
            // Existing component is generic: only this NEW scene instance receives new arrays.
            var shotLock = cameraGo.AddComponent<Panel04A04ShotLockV05>();
            shotLock.eyes = manifest.shots.Select(s => Panel04CorrectionV06Shared.ToUnity(s.eye)).ToArray();
            shotLock.targets = manifest.shots.Select(s => Panel04CorrectionV06Shared.ToUnity(s.target)).ToArray();
            shotLock.cutTimes = (float[])Panel04CorrectionV06Shared.CutTimes.Clone();

            AssetDatabase.CreateFolder(Panel04CorrectionV06Shared.TimelineParent, "Panel04_A04_v06");
            var animation = MakeCameraClip(manifest);
            Panel04CorrectionV06Shared.RequireNewAsset(Panel04CorrectionV06Shared.ClipPath);
            AssetDatabase.CreateAsset(animation, Panel04CorrectionV06Shared.ClipPath);
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "P04 v06 | Corrected static grief | Four shots";
            timeline.editorSettings.frameRate = 24;
            Panel04CorrectionV06Shared.RequireNewAsset(Panel04CorrectionV06Shared.TimelinePath);
            AssetDatabase.CreateAsset(timeline, Panel04CorrectionV06Shared.TimelinePath);
            var directorGo = new GameObject("P04_V06_TIMELINE__STATIC_FOUR_SHOTS__10S");
            var director = directorGo.AddComponent<PlayableDirector>();
            director.playableAsset = timeline;
            director.playOnAwake = true;
            director.extrapolationMode = DirectorWrapMode.Hold;
            shotLock.director = director;
            var cameraTrack = timeline.CreateTrack<AnimationTrack>(null, "01 | CAMERA | Four corrected static shots");
            cameraTrack.trackOffset = TrackOffset.ApplyTransformOffsets;
            var clip = cameraTrack.CreateClip(animation);
            clip.start = 0;
            clip.duration = Panel04CorrectionV06Shared.Duration;
            ((AnimationPlayableAsset)clip.asset).removeStartOffset = false;
            director.SetGenericBinding(cameraTrack, animator);
            var captionGo = new GameObject("P04_V06_NARRATION__EXISTING_APPROVED_TEXT");
            captionGo.transform.SetParent(directorGo.transform, false);
            var caption = captionGo.AddComponent<Panel04A04NarrationOverlay>();
            caption.director = director;
            caption.duration = Panel04CorrectionV06Shared.Duration;
            var captionTrack = timeline.CreateTrack<ActivationTrack>(null, "02 | NARRATION | Existing Panel 04 caption");
            var captionClip = captionTrack.CreateDefaultClip();
            captionClip.start = 0;
            captionClip.duration = Panel04CorrectionV06Shared.Duration;
            director.SetGenericBinding(captionTrack, captionGo);
            director.RebuildGraph();
            director.time = 0;
            director.Evaluate();
            if (!shotLock.ApplyNow()) throw new InvalidOperationException("Initial camera shot lock failed.");
            Panel04CorrectionV06Shared.CheckCamera(camera, shotLock, manifest, 0);

            EditorUtility.SetDirty(timeline);
            EditorUtility.SetDirty(animation);
            // Save ONLY newly authored assets; do not call SaveAssets on unrelated dirty assets.
            AssetDatabase.SaveAssetIfDirty(timeline);
            AssetDatabase.SaveAssetIfDirty(animation);
            Panel04CorrectionV06Shared.RequireNewAsset(Panel04CorrectionV06Shared.ScenePath);
            if (!EditorSceneManager.SaveScene(created, Panel04CorrectionV06Shared.ScenePath))
                throw new IOException("Failed to save the new P04 v06 scene.");
            saved = true;
            Debug.Log("[P04 v06] Created new scene/Timeline/clip. Earlier versions unchanged. " +
                "Open " + Panel04CorrectionV06Shared.ScenePath + " to review; camera poses/FOV came from " +
                Panel04CorrectionV06Shared.ManifestPath);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("P04 v06 build stopped", exception.Message +
                "\nNo earlier version was overwritten. Any partial NEW v06 outputs remain for inspection.", "OK");
        }
        finally
        {
            // This is a synchronous visible menu operation; no user scene was changed.
            if (previous != null)
            {
                try
                {
                    if (created.IsValid() && created.isLoaded)
                        if (!EditorSceneManager.CloseScene(created, true))
                            throw new InvalidOperationException("Could not close the new authoring scene.");
                    // Import/scene callbacks may invalidate a saved-scene handle. Resolve its
                    // recorded path among loaded scenes first; an already-active target is a no-op.
                    string expectedPath = Panel04CorrectionV06Shared.PreviousActivePath(previous);
                    Scene target = default;
                    for (int i = 0; i < SceneManager.sceneCount; i++)
                    {
                        Scene loaded = SceneManager.GetSceneAt(i);
                        if (!loaded.isLoaded) continue;
                        if (!string.IsNullOrEmpty(expectedPath)
                            ? Panel04CorrectionV06Shared.SamePath(loaded.path, expectedPath)
                            : loaded.handle == previousActive.handle)
                        {
                            target = loaded;
                            break;
                        }
                    }
                    Scene active = SceneManager.GetActiveScene();
                    if (!target.IsValid() || !target.isLoaded)
                        throw new InvalidOperationException("Previous scene is not loaded. Expected path: '" +
                            expectedPath + "'; actual active path: '" + active.path + "'. No scene was saved.");
                    if (active.handle != target.handle)
                        SceneManager.SetActiveScene(target);
                    // Judge the resulting state, not the API's boolean (false can be a no-op).
                    active = SceneManager.GetActiveScene();
                    bool activeMatches = string.IsNullOrEmpty(expectedPath)
                        ? active.handle == target.handle
                        : Panel04CorrectionV06Shared.SamePath(active.path, expectedPath);
                    bool setupMatches = Panel04CorrectionV06Shared.SceneSetupMatches(
                        previous, EditorSceneManager.GetSceneManagerSetup());
                    if (!active.IsValid() || !active.isLoaded || !activeMatches || !setupMatches)
                        throw new InvalidOperationException("Previous active scene was not restored. Expected path: '" +
                            expectedPath + "'; actual active path: '" + active.path + "'; full setup matches=" +
                            setupMatches + ". No previous scene was saved.");
                    Debug.Log("[P04 v06] Previous active scene verified. actualActivePath='" + active.path +
                        "', name='" + active.name + "', handle=" + active.handle +
                        "; fullSceneSetupMatches=" + setupMatches + "; newSceneSaved=" + saved);
                }
                catch (Exception restoreError)
                {
                    Debug.LogError("[P04 v06] Could not restore previous scene setup: " + restoreError);
                    EditorUtility.DisplayDialog("P04 v06 scene restoration", restoreError.Message +
                        (saved ? "\nThe new scene was saved successfully." : "\nThe new scene was not confirmed saved."),
                        "OK");
                }
            }
        }
    }

    private static Transform RequireSingle(Transform[] transforms, string exactName)
    {
        var matches = transforms.Where(t => t.name == exactName).ToArray();
        if (matches.Length != 1)
            throw new InvalidOperationException("Expected one source object named '" + exactName +
                "'; found " + matches.Length + ".");
        return matches[0];
    }

    private static AnimationClip MakeCameraClip(Panel04CorrectionV06Shared.Manifest manifest)
    {
        var clip = new AnimationClip { name = "P04 v06 | Corrected camera cuts", frameRate = 24 };
        var times = Panel04CorrectionV06Shared.CutTimes.Concat(new[] { Panel04CorrectionV06Shared.Duration }).ToArray();
        var positions = manifest.shots.Select(s => Panel04CorrectionV06Shared.ToUnity(s.eye)).ToList();
        positions.Add(positions[3]);
        var rotations = manifest.shots.Select(s => Quaternion.LookRotation(
            Panel04CorrectionV06Shared.ToUnity(s.target) - Panel04CorrectionV06Shared.ToUnity(s.eye), Vector3.up)).ToList();
        rotations.Add(rotations[3]);
        for (int axis = 0; axis < 3; axis++)
        {
            int component = axis;
            clip.SetCurve("", typeof(Transform), "localPosition." + "xyz"[axis],
                Stepped(times, positions.Select(p => p[component]).ToArray()));
        }
        for (int axis = 0; axis < 4; axis++)
        {
            int component = axis;
            clip.SetCurve("", typeof(Transform), "localRotation." + "xyzw"[axis],
                Stepped(times, rotations.Select(q => q[component]).ToArray()));
        }
        var fieldsOfView = manifest.shots.Select(s => s.verticalFov).ToList();
        fieldsOfView.Add(fieldsOfView[3]);
        clip.SetCurve("", typeof(Camera), "field of view", Stepped(times, fieldsOfView.ToArray()));
        clip.EnsureQuaternionContinuity();
        return clip;
    }

    private static AnimationCurve Stepped(float[] times, float[] values)
    {
        var curve = new AnimationCurve(times.Select((time, i) => new Keyframe(time, values[i])).ToArray());
        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
        }
        return curve;
    }
}

internal static class Panel04CorrectionV06Shared
{
    internal const string ScenePath = "Assets/Scenes/Cinematics/Prologue_P04_A04_GriefStatic_v06.unity";
    internal const string SourceGlb = "Assets/Cinematics/Storyboard3D/Characters/Panel04/Panel04_GriefScene_StaticBlockout_v02.glb";
    internal const string TimelineParent = "Assets/Cinematics/Storyboard3D/Timelines";
    internal const string TimelineFolder = TimelineParent + "/Panel04_A04_v06";
    internal const string TimelinePath = TimelineFolder + "/P04_A04_Grief_ReturnHome_v06.playable";
    internal const string ClipPath = TimelineFolder + "/P04_A04_Camera_CutSequence_v06.anim";
    internal const string ManifestPath = "ArtSource/Cinematics/Panel04_v06/P04_CorrectionManifest_v06.json";
    internal const string ReviewDirectory = "ArtSource/Cinematics/Panel04_v06/Reviews/Unity_v06";
    internal const float Duration = 10f;
    internal static readonly float[] CutTimes = { 0f, 2.667f, 6.042f, 8.458f };
    internal static readonly string[] DefaultShotIds = {
        "01_RUIN_CONTEXT", "02_VAN_GRIEF", "03_MOTHER_AND_HANDS", "04_BLUE_BOW"
    };

    [Serializable] internal sealed class Shot
    {
        public string id;
        public Vector3 eye;
        public Vector3 target;
        public float verticalFov;
    }
    [Serializable] internal sealed class Manifest
    {
        public string coordinateSpace;
        public string sourceBlenderScene;
        public Shot[] shots;
        public string[] requiredObjectNames;
        public string[] hideSourceObjects;
    }

    internal static string FullPath(string projectRelative) =>
        Path.GetFullPath(Path.Combine(Application.dataPath, "..", projectRelative));
    internal static bool SamePath(string left, string right) => string.Equals(
        (left ?? "").Replace('\\', '/'), (right ?? "").Replace('\\', '/'), StringComparison.OrdinalIgnoreCase);
    internal static string PreviousActivePath(SceneSetup[] setup)
    {
        foreach (SceneSetup scene in setup)
            if (scene.isActive) return scene.path ?? "";
        return "";
    }
    internal static bool SceneSetupMatches(SceneSetup[] expected, SceneSetup[] actual)
    {
        if (expected == null || actual == null || expected.Length != actual.Length) return false;
        // Include unloaded hierarchy entries and ordering; scene handles may be recreated.
        for (int i = 0; i < expected.Length; i++)
            if (!SamePath(expected[i].path, actual[i].path) ||
                expected[i].isLoaded != actual[i].isLoaded || expected[i].isActive != actual[i].isActive)
                return false;
        return true;
    }
    // Verified for the project's glTFast import; source manifest uses Blender coordinates.
    internal static Vector3 ToUnity(Vector3 blender) => new Vector3(-blender.x, blender.z, -blender.y);
    internal static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    internal static bool Finite(Vector3 value) => Finite(value.x) && Finite(value.y) && Finite(value.z);

    internal static void GuardEditor()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            throw new InvalidOperationException("Wait for imports/compilation and stop Play Mode before this visible command.");
        foreach (var process in System.Diagnostics.Process.GetProcesses())
        {
            using (process)
            {
                string name;
                try { name = process.ProcessName; }
                catch (InvalidOperationException) { continue; } // Process exited during enumeration.
                if (name.Equals("VALORANT", StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith("VALORANT-", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("VALORANT is running. Development is stopped until it fully exits.");
            }
        }
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded && scene.isDirty)
                throw new InvalidOperationException("Loaded scene has unsaved changes: " + scene.name +
                    ". Save or resolve it visibly before continuing; this command will not save it.");
        }
        if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            throw new InvalidOperationException("Leave Prefab Mode before creating or capturing the isolated scene.");
    }

    internal static Manifest ReadManifest()
    {
        string path = FullPath(ManifestPath);
        if (!File.Exists(path)) throw new FileNotFoundException("Camera manifest is missing.", path);
        var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(path));
        if (manifest == null || manifest.shots == null || manifest.shots.Length != 4)
            throw new InvalidDataException("The v06 manifest must contain exactly four shots.");
        if (!string.IsNullOrEmpty(manifest.coordinateSpace) &&
            !manifest.coordinateSpace.Equals("Blender", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Manifest coordinates must be Blender space.");
        for (int i = 0; i < manifest.shots.Length; i++)
        {
            Shot shot = manifest.shots[i];
            if (shot == null || !Finite(shot.eye) || !Finite(shot.target) ||
                !Finite(shot.verticalFov) || shot.verticalFov < 10f || shot.verticalFov > 110f ||
                (shot.target - shot.eye).sqrMagnitude < .0001f)
                throw new InvalidDataException("Invalid eye/target/verticalFov in manifest shot " + (i + 1));
            if (string.IsNullOrWhiteSpace(shot.id)) shot.id = DefaultShotIds[i];
            if (shot.id.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                shot.id.Contains("/") || shot.id.Contains("\\"))
                throw new InvalidDataException("Shot id must be a filename-safe label: " + shot.id);
        }
        if (manifest.shots.Select(s => s.id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 4)
            throw new InvalidDataException("Shot ids must be unique.");
        return manifest;
    }

    internal static void RequireNewAsset(string path)
    {
        string full = FullPath(path);
        if (File.Exists(full) || File.Exists(full + ".meta") || AssetDatabase.LoadMainAssetAtPath(path) != null)
            throw new IOException("Refusing to overwrite existing asset or metadata: " + path);
    }

    internal static void ValidateStage(GameObject stage)
    {
        var renderers = stage.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) throw new InvalidOperationException("Corrected source has no renderers.");
        foreach (Renderer renderer in renderers)
            if (renderer.sharedMaterials.Length == 0 || renderer.sharedMaterials.Any(m => m == null || m.shader == null))
                throw new InvalidOperationException("Missing material/shader on source renderer: " + renderer.name);
    }

    internal static void CheckCamera(Camera camera, Panel04A04ShotLockV05 shotLock, Manifest manifest, int index)
    {
        Vector3 eye = ToUnity(manifest.shots[index].eye);
        Vector3 target = ToUnity(manifest.shots[index].target);
        if (Vector3.Distance(camera.transform.position, eye) > .001f ||
            Vector3.Distance(shotLock.eyes[index], eye) > .001f ||
            Vector3.Distance(shotLock.targets[index], target) > .001f ||
            Quaternion.Angle(camera.transform.rotation, Quaternion.LookRotation(target - eye, Vector3.up)) > .1f ||
            Mathf.Abs(camera.fieldOfView - manifest.shots[index].verticalFov) > .05f)
            throw new InvalidOperationException("Camera position/rotation/FOV differs from manifest shot " + (index + 1));
    }
}
#endif
