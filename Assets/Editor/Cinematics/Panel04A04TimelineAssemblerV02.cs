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

/// <summary>
/// One-time, versioned Panel 04 defense cinematic creation. This never edits
/// approved Panel 01-03 scenes, gameplay scenes, or Project Settings.
/// The GLB has a baked static body pose; the Timeline animates ONLY the camera.
/// </summary>
internal static class Panel04A04TimelineAssemblerV02
{
    private const string ScenePath =
        "Assets/Scenes/Cinematics/Prologue_P04_A04_GriefStatic_v02.unity";
    private const string SourceGlb =
        "Assets/Cinematics/Storyboard3D/Characters/Panel04/Panel04_GriefScene_StaticBlockout_v01.glb";
    private const string TimelineFolder =
        "Assets/Cinematics/Storyboard3D/Timelines/Panel04_A04_v02";
    private const string TimelinePath = TimelineFolder + "/P04_A04_Grief_ReturnHome_v02.playable";
    private const string CameraClipPath = TimelineFolder + "/P04_A04_Camera_CutSequence_v02.anim";
    private const float Duration = 10.0f;

    [MenuItem("SPELLSTRIKE/Cinematics/Panel 04/Build A04 Grief Scene v02")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before creating the Panel 04 scene.");

        if (File.Exists(ScenePath) || File.Exists(TimelinePath) || File.Exists(CameraClipPath))
            throw new IOException("A04 v01 output already exists. No overwrite.");

        Scene previous = SceneManager.GetActiveScene();
        if (previous.IsValid() && previous.isLoaded && previous.isDirty)
            throw new InvalidOperationException("Active Unity scene has unsaved changes; not replacing it.");

        if (!AssetDatabase.IsValidFolder("Assets/Cinematics/Storyboard3D/Timelines"))
            throw new DirectoryNotFoundException("Expected existing Cinematics Timelines folder.");

        AssetDatabase.ImportAsset(SourceGlb, ImportAssetOptions.ForceUpdate);
        var scenePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourceGlb);
        if (scenePrefab == null)
            throw new InvalidOperationException(
                "glTFast did not import Panel04_GriefScene_StaticBlockout_v01.glb as GameObject. " +
                "No scene created. Check the GLB importer.");

        AssetDatabase.CreateFolder(
            "Assets/Cinematics/Storyboard3D/Timelines", "Panel04_A04_v02");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var stageRoot = new GameObject("P04_A04_3D_STAGED_RUIN__STATIC_DEFENSE_BLOCKOUT");
        var stage = (GameObject)PrefabUtility.InstantiatePrefab(scenePrefab, scene);
        if (stage == null) throw new InvalidOperationException("Cannot instantiate P04 GLB.");
        stage.name = "P04_A04_BLENDER_V14__POSED_GRIEF_SCENE";
        stage.transform.SetParent(stageRoot.transform, false);
        stage.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        stage.transform.localScale = Vector3.one;

        int rendererCount = stage.GetComponentsInChildren<Renderer>(true).Length;
        if (rendererCount < 680)
            throw new InvalidOperationException(
                "Imported GLB is incomplete. Expected hundreds of renderers; saw " + rendererCount);

        var allTransforms = stage.GetComponentsInChildren<Transform>(true);
        RequireActor(allTransforms, "P04_STATIC_YoungVanAert_Rodin_Mesh");
        RequireActor(allTransforms, "P04_STATIC_P04_MOTHER_STATIC_DRESS_GROUNDED_v10");
        RequireActor(allTransforms, "P04_STATIC_P04_Nevali_Bow_CenterKnot");

        // A04-only moody lighting; no project-wide URP/Android changes.
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.29f, 0.30f, 0.36f);
        RenderSettings.fog = false;
        GameObject lights = new GameObject("P04_A04_3D_LIGHTING__EDITABLE");
        GameObject key = new GameObject("P04_A04_Directional_ColdDawn");
        key.transform.SetParent(lights.transform, false);
        key.transform.rotation = Quaternion.Euler(49f, -38f, 0f);
        var keyLight = key.AddComponent<Light>();
        keyLight.type = LightType.Directional;
        keyLight.color = new Color(0.69f, 0.78f, 1f);
        keyLight.intensity = 1.35f;

        GameObject warm = new GameObject("P04_A04_WarmDestructionBounce");
        warm.transform.SetParent(lights.transform, false);
        warm.transform.position = ConvertPoint(new Vector3(-0.4f, -2.1f, 2.2f));
        var fill = warm.AddComponent<Light>();
        fill.type = LightType.Point;
        fill.color = new Color(1f, 0.59f, 0.35f);
        fill.range = 7f;
        fill.intensity = 2.0f;

        GameObject cameraGo = new GameObject("P04_A04_CAMERA__TIMELINE_FOUR_SHOTS");
        cameraGo.tag = "MainCamera";
        var camera = cameraGo.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.065f, 0.08f, 0.12f);
        camera.nearClipPlane = 0.07f;
        camera.farClipPlane = 300f;
        camera.fieldOfView = 41f;
        camera.allowHDR = false;
        var listener = cameraGo.AddComponent<AudioListener>();
        var animator = cameraGo.AddComponent<Animator>();
        animator.applyRootMotion = false;

        var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        timeline.name = "A04 | The Destroyed Home | Four-shot 10s defense sequence";
        timeline.editorSettings.fps = 24;
        AssetDatabase.CreateAsset(timeline, TimelinePath);

        var root = new GameObject("P04_A04_TIMELINE__DESTROYED_HOME__10S");
        var director = root.AddComponent<PlayableDirector>();
        director.playableAsset = timeline;
        director.playOnAwake = true;
        director.extrapolationMode = DirectorWrapMode.Hold;
        var animation = MakeCameraCuts();
        AssetDatabase.CreateAsset(animation, CameraClipPath);
        var track = timeline.CreateTrack<AnimationTrack>(null,
            "01 | CAMERA | Discovery -> Grief -> Mother -> Blue Ribbon");
        var timelineClip = track.CreateClip(animation);
        timelineClip.start = 0;
        timelineClip.duration = Duration;
        ((AnimationPlayableAsset)timelineClip.asset).removeStartOffset = false;
        track.trackOffset = TrackOffset.ApplyTransformOffsets;
        director.SetGenericBinding(track, animator);

        var overlay = new GameObject("P04_A04_NARRATION__APPROVED_PANEL04_TEXT");
        overlay.transform.SetParent(root.transform, false);
        var narration = overlay.AddComponent<Panel04A04NarrationOverlay>();
        narration.director = director;
        narration.duration = Duration;
        var narrationTrack = timeline.CreateTrack<ActivationTrack>(null,
            "02 | NARRATION | Locked Panel 04 grief caption");
        var narrationClip = narrationTrack.CreateDefaultClip();
        narrationClip.start = 0;
        narrationClip.duration = Duration;
        director.SetGenericBinding(narrationTrack, overlay);

        director.RebuildGraph();
        director.time = 0;
        director.Evaluate();

        // The cut asset has absolute animation curves. Assert the timeline did
        // not force the camera to world origin (a previous A01 issue).
        float nearOriginal = Vector3.Distance(cameraGo.transform.position,
            ConvertPoint(new Vector3(-0.49f, -3.78f, 2.35f)));
        if (nearOriginal > 0.55f)
            throw new InvalidOperationException(
                "A04 timeline camera position is invalid: " + cameraGo.transform.position +
                ", distance to shot 1=" + nearOriginal.ToString("F3"));

        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new IOException("Could not save isolated Panel 04 A04 scene.");
        EditorUtility.SetDirty(timeline);
        EditorUtility.SetDirty(animation);
        AssetDatabase.SaveAssets();
        Debug.Log("[P04 A04] CREATED " + ScenePath +
                  " | renderers=" + rendererCount +
                  " | Timeline=" + TimelinePath +
                  " | 2 tracks / 10s | STATIC character poses, camera cuts only");
    }

    public static void BuildBatch()
    {
        try { Build(); }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorApplication.Exit(1);
        }
    }

    private static void RequireActor(Transform[] transforms, string exactPrefix)
    {
        if (!transforms.Any(x => x.name.StartsWith(exactPrefix, StringComparison.Ordinal)))
            throw new InvalidOperationException("A04 source GLB is missing: " + exactPrefix);
    }

    private static Vector3 ConvertPoint(Vector3 blender)
    {
        // glTFast imports this Blender GLB with the X axis mirrored: (-x,z,-y). Verified against live imported renderer bounds.
        return new Vector3(-blender.x, blender.z, -blender.y);
    }

    private static Quaternion CameraLook(Vector3 blenderEye, Vector3 blenderTarget)
    {
        Vector3 eye = ConvertPoint(blenderEye);
        Vector3 target = ConvertPoint(blenderTarget);
        return Quaternion.LookRotation((target - eye).normalized, Vector3.up);
    }

    private static AnimationClip MakeCameraCuts()
    {
        var animation = new AnimationClip
        {
            name = "A04 | Static Storyboard Camera Cuts | 10s",
            frameRate = 24f
        };
        Vector3[] eyes =
        {
            new Vector3(-0.49f,-3.78f,2.35f),
            new Vector3(1.16f,1.42f,1.79f),
            new Vector3(-0.13f,1.10f,1.30f),
            new Vector3(0.13f,-1.78f,1.56f),
            new Vector3(0.13f,-1.78f,1.56f)
        };
        Vector3[] targets =
        {
            new Vector3(-0.45f,-0.21f,0.59f),
            new Vector3(-0.88f,-0.28f,0.55f),
            new Vector3(-1.01f,-0.20f,0.34f),
            new Vector3(-0.13f,-1.02f,0.20f),
            new Vector3(-0.13f,-1.02f,0.20f)
        };
        float[] times = { 0f, 2.667f, 6.042f, 8.458f, Duration };
        Vector3[] locations = eyes.Select(ConvertPoint).ToArray();
        Quaternion[] rotations = Enumerable.Range(0, eyes.Length)
            .Select(i => CameraLook(eyes[i], targets[i])).ToArray();
        string[] fields = { "localPosition.x", "localPosition.y", "localPosition.z" };
        for (int component = 0; component < 3; component++)
        {
            int c = component;
            var curve = Stepped(times, locations.Select(p => p[c]).ToArray());
            animation.SetCurve("", typeof(Transform), fields[component], curve);
        }
        string[] rotfields = { "localRotation.x", "localRotation.y", "localRotation.z", "localRotation.w" };
        for (int component = 0; component < 4; component++)
        {
            int c = component;
            var curve = Stepped(times, rotations.Select(q => q[c]).ToArray());
            animation.SetCurve("", typeof(Transform), rotfields[component], curve);
        }
        animation.EnsureQuaternionContinuity();
        return animation;
    }

    private static AnimationCurve Stepped(float[] times, float[] values)
    {
        var keys = new Keyframe[times.Length];
        for (int i = 0; i < times.Length; i++)
            keys[i] = new Keyframe(times[i], values[i]);
        var curve = new AnimationCurve(keys);
        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
        }
        return curve;
    }
}
#endif
