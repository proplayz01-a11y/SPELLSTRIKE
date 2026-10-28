#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

/// <summary>
/// One-time, non-destructive Timeline authoring for the A01 family-home shot.
/// Generates a versioned .playable and isolated v05 Unity scene from saved v04.
/// Never alters the earlier Panel 01 scenes or their source character assets.
/// </summary>
internal static class Panel01A01TimelineAssembler
{
    public const string SourceScene =
        "Assets/Scenes/Cinematics/Prologue_P01_FamilyHome_v04_Animated_HomeV03.unity";
    public const string OutputScene =
        "Assets/Scenes/Cinematics/Prologue_P01_FamilyHome_v05_A01_Timeline.unity";
    public const string OutputFolder =
        "Assets/Cinematics/Storyboard3D/Timelines/Panel01_A01_v05";
    public const string TimelinePath = OutputFolder + "/A01_PeaceBeforeGreatWar_v05.playable";
    private const string NevaliFbx =
        "Assets/Cinematics/Storyboard3D/Characters/Panel01_v03/Nevali_v13_MediaPipe_BlockPlay_v04.fbx";

    private const float ShotLength = 10.5f;

    [MenuItem("SPELLSTRIKE/Cinematics/Panel 01/Build A01 Timeline v05")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before building A01 Timeline.");
        if (File.Exists(OutputScene) || File.Exists(TimelinePath))
            throw new IOException("Existing v05 shot/Timeline preserved. No overwrite performed.");

        Scene openScene = SceneManager.GetActiveScene();
        if (openScene.path != SourceScene || openScene.isDirty)
            throw new InvalidOperationException("Open the saved, clean v04 scene first. Active: " + openScene.path);

        GameObject sourceCam = Require("P01_STATIC_HERO_CAMERA__MATCH_32MM");
        GameObject nevali = Require("NEVALI_V13__MEDIAPIPE_V04_ANIMATED");
        GameObject van = Require("YOUNG_VAN_AERT__ARMS_CROSSED_V06");
        GameObject home = Require("P01_V03__HOUSE__BLENDER_ROOF_AND_FLOWERS");
        if (sourceCam.GetComponent<Panel01CinematicPlayback>() == null ||
            nevali.GetComponent<Animator>() == null ||
            home.GetComponentsInChildren<Renderer>(true).Length != 84)
            throw new InvalidOperationException("Source v04 cinematic does not match validated baseline.");

        var originalBlock = home.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.name == "TOY_BUILDING_BLOCK_09");
        if (originalBlock == null || originalBlock.GetComponent<Renderer>() == null)
            throw new InvalidOperationException("Missing editable block 09 for the rough toy interaction.");

        AnimationClip nevaliClip = AssetDatabase.LoadAllAssetsAtPath(NevaliFbx)
            .OfType<AnimationClip>().FirstOrDefault(c => c.name == "Scene");
        if (nevaliClip == null || nevaliClip.length < 7.8f)
            throw new InvalidOperationException("Expected Nevali's existing 8-sec generic animation.");

        if (!AssetDatabase.IsValidFolder(OutputFolder))
        {
            string parent = "Assets/Cinematics/Storyboard3D/Timelines";
            if (!AssetDatabase.IsValidFolder(parent))
                throw new DirectoryNotFoundException(parent);
            AssetDatabase.CreateFolder(parent, "Panel01_A01_v05");
        }

        // Copy the already saved v04 scene. The loaded source is never changed.
        if (!EditorSceneManager.SaveScene(openScene, OutputScene, true))
            throw new IOException("Cannot copy source cinematic to isolated v05 scene.");
        Scene target = EditorSceneManager.OpenScene(OutputScene, OpenSceneMode.Single);

        GameObject cameraGo = Require("P01_STATIC_HERO_CAMERA__MATCH_32MM");
        GameObject childGo = Require("NEVALI_V13__MEDIAPIPE_V04_ANIMATED");
        GameObject fatherGo = Require("YOUNG_VAN_AERT__ARMS_CROSSED_V06");
        GameObject blockGo = Require("TOY_BUILDING_BLOCK_09");

        // Disable the earlier Update()-driven camera/narration controller ONLY
        // in new v05. Timeline owns the camera, animations and narration now.
        var oldPlayback = cameraGo.GetComponent<Panel01CinematicPlayback>();
        oldPlayback.enabled = false;

        var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        timeline.name = "A01 | Peace Before the Great War | 10.5s";
        timeline.editorSettings.fps = 30f;
        AssetDatabase.CreateAsset(timeline, TimelinePath);

        GameObject root = new GameObject("A01_TIMELINE__PEACE_BEFORE_THE_GREAT_WAR__10P5S");
        var director = root.AddComponent<PlayableDirector>();
        director.playableAsset = timeline;
        director.playOnAwake = true;
        director.extrapolationMode = DirectorWrapMode.Hold;

        // Track 01: a restrained 0.27 m slow, smooth push on the approved 32mm camera.
        var cameraAnimator = EnsureAnimator(cameraGo);
        AnimationClip cameraClip = MakeCameraPush(cameraGo.transform);
        AssetDatabase.CreateAsset(cameraClip, OutputFolder + "/A01_Camera_Push027_v05.anim");
        var cameraTrack = timeline.CreateTrack<AnimationTrack>(null, "01 | CAMERA 32mm | Smooth push 0.27m");
        var cameraTimelineClip = cameraTrack.CreateClip(cameraClip);
        cameraTimelineClip.start = 0;
        cameraTimelineClip.duration = ShotLength;
        ((AnimationPlayableAsset)cameraTimelineClip.asset).removeStartOffset = false;
        director.SetGenericBinding(cameraTrack, cameraAnimator);

        // Track 02: existing textured Nevali v13 Generic 57-bone rough animation.
        // Disable her previous AnimatorController to avoid independent time progression.
        var childAnimator = EnsureAnimator(childGo);
        childAnimator.runtimeAnimatorController = null;
        childAnimator.applyRootMotion = false;
        var childTrack = timeline.CreateTrack<AnimationTrack>(null, "02 | NEVALI v13 | Play with blocks (rough, 8s)");
        var childClip = childTrack.CreateClip(nevaliClip);
        childClip.start = 0;
        // Stretch the existing 8s rough body animation over the 10.5s shot.
        // This avoids snapping to a bind pose when the shorter source ends.
        childClip.timeScale = nevaliClip.length / ShotLength;
        childClip.duration = ShotLength;
        director.SetGenericBinding(childTrack, childAnimator);

        // The imported Generic FBX uses an internal root origin that Unity
        // Timeline interprets differently from the validated v04 Animator.
        // Reapply the original scene-stage transform after Timeline's animation
        // graph, without suppressing the rig's animated bone curves.
        var stageAnchor = root.AddComponent<Panel01TimelineStageAnchor>();
        stageAnchor.director = director;
        stageAnchor.character = childGo.transform;
        stageAnchor.stagePosition = new Vector3(-1.28f, 0.45f, 0.45f);
        stageAnchor.stageRotationEuler = Vector3.zero;

        // Track 03: a subtle body-orientation response from stationary Young Van.
        // He remains arms crossed; this is not an existing rigged arms animation.
        var fatherAnimator = EnsureAnimator(fatherGo);
        AnimationClip fatherClip = MakeVanWatch(fatherGo.transform);
        AssetDatabase.CreateAsset(fatherClip, OutputFolder + "/A01_Van_QuietWatch_v05.anim");
        var fatherTrack = timeline.CreateTrack<AnimationTrack>(null, "03 | VAN | Arms-crossed quiet watch");
        var fatherTimelineClip = fatherTrack.CreateClip(fatherClip);
        fatherTimelineClip.start = 0;
        fatherTimelineClip.duration = ShotLength;
        ((AnimationPlayableAsset)fatherTimelineClip.asset).removeStartOffset = false;
        director.SetGenericBinding(fatherTrack, fatherAnimator);

        // Track 04: one conservative toy block lift/place proof. This is rough
        // staged movement, not a hand-IK / true grasp-and-release solver.
        var blockAnimator = EnsureAnimator(blockGo);
        AnimationClip blockClip = MakeBlockAction(blockGo.transform);
        AssetDatabase.CreateAsset(blockClip, OutputFolder + "/A01_ToyBlock09_RoughLiftPlace_v05.anim");
        var blockTrack = timeline.CreateTrack<AnimationTrack>(null, "04 | TOY BLOCK 09 | Rough lift and place");
        var blockTimelineClip = blockTrack.CreateClip(blockClip);
        blockTimelineClip.start = 0;
        blockTimelineClip.duration = ShotLength;
        director.SetGenericBinding(blockTrack, blockAnimator);

        // Track 05: Timeline-owned activation of the locked narration overlay.
        // The overlay samples director.time to fade at the shot head/tail.
        var caption = new GameObject("A01_NARRATION__LOCKED_TEXT");
        caption.transform.SetParent(root.transform, false);
        var overlay = caption.AddComponent<Panel01TimelineNarrationOverlay>();
        overlay.timelineDirector = director;
        overlay.shotDuration = ShotLength;
        var narrationTrack = timeline.CreateTrack<ActivationTrack>(null, "05 | NARRATION | Locked Panel 01 copy");
        var narrationClip = narrationTrack.CreateDefaultClip();
        narrationClip.start = 0;
        narrationClip.duration = ShotLength;
        director.SetGenericBinding(narrationTrack, caption);

        // Ensure the playable graph and activation tracks are rebuilt on Play.
        director.RebuildGraph();
        EditorUtility.SetDirty(timeline);
        EditorUtility.SetDirty(root);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(target);
        if (!EditorSceneManager.SaveScene(target))
            throw new IOException("Failed to save isolated v05 Timeline scene.");

        Debug.Log("[P01 Timeline] CREATED " + OutputScene + " using " + TimelinePath +
                  ", tracks=" + timeline.GetOutputTracks().Count() +
                  ", animation=" + nevaliClip.length.ToString("F3") + "s, shot=" + ShotLength);
    }

    private static GameObject Require(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go == null) throw new InvalidOperationException("Panel 01 missing: " + name);
        return go;
    }

    private static Animator EnsureAnimator(GameObject go)
    {
        var a = go.GetComponent<Animator>();
        if (a == null) a = go.AddComponent<Animator>();
        a.applyRootMotion = false;
        return a;
    }

    private static AnimationCurve Ease(params Keyframe[] keys)
    {
        var curve = new AnimationCurve(keys);
        for (int i = 0; i < curve.length; i++)
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
        for (int i = 0; i < curve.length; i++)
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
        return curve;
    }

    private static void PositionCurves(AnimationClip clip, float[] times, Vector3[] points)
    {
        string[] names = { "localPosition.x", "localPosition.y", "localPosition.z" };
        for (int a = 0; a < 3; a++)
        {
            var keys = new Keyframe[times.Length];
            for (int i = 0; i < times.Length; i++) keys[i] = new Keyframe(times[i], points[i][a]);
            clip.SetCurve(string.Empty, typeof(Transform), names[a], Ease(keys));
        }
    }

    private static AnimationClip MakeCameraPush(Transform camera)
    {
        var c = new AnimationClip { name = "A01 | Camera | Gentle 0.27 m Push", frameRate = 30f };
        Vector3 start = camera.localPosition;
        Vector3 end = start + camera.localRotation * Vector3.forward * 0.27f;
        PositionCurves(c, new[] { 0f, 2.1f, 5.25f, 8.4f, ShotLength },
            new[] { start, Vector3.Lerp(start, end, .11f), Vector3.Lerp(start, end, .5f),
                    Vector3.Lerp(start, end, .89f), end });
        RotationCurves(c, new[] { 0f, ShotLength }, new[] { camera.localRotation, camera.localRotation });
        return c;
    }

    private static AnimationClip MakeVanWatch(Transform father)
    {
        var c = new AnimationClip { name = "A01 | Van | Gentle Watch", frameRate = 30f };
        Quaternion q0 = father.localRotation;
        Quaternion q1 = Quaternion.AngleAxis(-3f, Vector3.up) * q0;
        Quaternion[] rotations = { q0, q0, q1, q1, q0 };
        float[] times = { 0f, 1.6f, 3.4f, 7.5f, ShotLength };
        PositionCurves(c, new[] { 0f, ShotLength },
            new[] { father.localPosition, father.localPosition });
        RotationCurves(c, times, rotations);
        return c;
    }

    private static void RotationCurves(AnimationClip clip, float[] times, Quaternion[] rotations)
    {
        string[] fields = { "localRotation.x", "localRotation.y", "localRotation.z", "localRotation.w" };
        for (int j = 0; j < 4; j++)
        {
            var keys = new Keyframe[times.Length];
            for (int i = 0; i < times.Length; i++) keys[i] = new Keyframe(times[i], rotations[i][j]);
            clip.SetCurve(string.Empty, typeof(Transform), fields[j], Ease(keys));
        }
        clip.EnsureQuaternionContinuity();
    }

    [MenuItem("SPELLSTRIKE/Cinematics/Panel 01/Repair Existing A01 v05 Timeline Offsets")]
    public static void RepairExistingV05()
    {
        // This deliberately modifies ONLY the versioned Timeline and its three
        // generated assets, never v04, source FBXs or an unrelated active scene.
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before Timeline asset repair.");
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != OutputScene || scene.isDirty)
            throw new InvalidOperationException("Need clean, isolated v05 scene; active=" + scene.path);
        GameObject camera = Require("P01_STATIC_HERO_CAMERA__MATCH_32MM");
        GameObject father = Require("YOUNG_VAN_AERT__ARMS_CROSSED_V06");
        GameObject child = Require("NEVALI_V13__MEDIAPIPE_V04_ANIMATED");
        var director = UnityEngine.Object.FindObjectsByType<PlayableDirector>(FindObjectsSortMode.None)
            .FirstOrDefault(x => x.gameObject.name == "A01_TIMELINE__PEACE_BEFORE_THE_GREAT_WAR__10P5S");
        var timeline = director != null ? director.playableAsset as TimelineAsset : null;
        if (timeline == null || AssetDatabase.GetAssetPath(timeline) != TimelinePath)
            throw new InvalidOperationException("Wrong or missing A01 Timeline asset.");
        var tracks = timeline.GetOutputTracks().OfType<AnimationTrack>().ToArray();
        var camTrack = tracks.Single(t => t.name.StartsWith("01 | CAMERA"));
        var fatherTrack = tracks.Single(t => t.name.StartsWith("03 | VAN"));
        AnimationClip camClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
            OutputFolder + "/A01_Camera_Push027_v05.anim");
        AnimationClip fatherClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
            OutputFolder + "/A01_Van_QuietWatch_v05.anim");
        if (camClip == null || fatherClip == null)
            throw new InvalidOperationException("Missing versioned v05 .anim assets.");

        // Explicitly author absolute initial camera rotation and stationary
        // father position. Unity Timeline otherwise strips their starting
        // transform and moves the camera/character to world origin.
        RotationCurves(camClip, new[] { 0f, ShotLength },
            new[] { camera.transform.localRotation, camera.transform.localRotation });
        PositionCurves(fatherClip, new[] { 0f, ShotLength },
            new[] { father.transform.localPosition, father.transform.localPosition });

        ((AnimationPlayableAsset)camTrack.GetClips().Single().asset).removeStartOffset = false;
        ((AnimationPlayableAsset)fatherTrack.GetClips().Single().asset).removeStartOffset = false;
        camTrack.trackOffset = TrackOffset.ApplyTransformOffsets;
        fatherTrack.trackOffset = TrackOffset.ApplyTransformOffsets;

        // Keep Nevali's imported FBX's peculiar root motion from contaminating
        // the saved Edit-mode stage transform; the isolated runtime anchor fixes
        // final placement after Timeline evaluates all animated bone channels.
        var stageAnchor = director.GetComponent<Panel01TimelineStageAnchor>();
        if (stageAnchor == null) throw new InvalidOperationException("Missing A01 stage anchor.");
        child.transform.SetPositionAndRotation(stageAnchor.stagePosition,
            Quaternion.Euler(stageAnchor.stageRotationEuler));

        EditorUtility.SetDirty(camClip);
        EditorUtility.SetDirty(fatherClip);
        EditorUtility.SetDirty(timeline);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new IOException("Failed to save repaired A01 Timeline scene.");
        Debug.Log("[P01 Timeline] Repaired v05 camera orientation, Van position, and Nevali scene staging.");
    }

    private static AnimationClip MakeBlockAction(Transform block)
    {
        var c = new AnimationClip { name = "A01 | Toy Block 09 | Rough Lift Place", frameRate = 30f };
        Vector3 p = block.localPosition;
        PositionCurves(c,
            new[] { 0f, 1.8f, 2.6f, 3.5f, 4.7f, 5.7f, 6.3f, ShotLength },
            new[] { p, p, p + new Vector3(.12f, .08f, -.09f),
                p + new Vector3(.30f, .19f, -.31f),
                p + new Vector3(.28f, .14f, -.26f),
                p + new Vector3(.13f, .035f, .05f),
                p + new Vector3(.12f, 0f, .13f),
                p + new Vector3(.12f, 0f, .13f) });
        return c;
    }
}
#endif
