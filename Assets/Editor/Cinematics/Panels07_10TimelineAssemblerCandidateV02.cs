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

// Isolated, opt-in 07-10 cinematic assembly. NEVER run with a dirty active scene.
// Imported GLBs MUST first be copied into Assets/Cinematics/Storyboard3D/Panels07_10_v02/PanelXX/.
// This is a candidate generator, not a claim of visual or Timeline playback validation.
internal static class Panels07_10TimelineAssemblerCandidateV02
{
    const string AssetRoot = "Assets/Cinematics/Storyboard3D/Panels07_10_v02";
    const string SceneRoot = "Assets/Scenes/Cinematics";
    const string TimelineRoot = "Assets/Cinematics/Storyboard3D/Timelines/Panels07_10_v02";
    const double Duration = 8.0;

    [MenuItem("SPELLSTRIKE/Cinematics/Panels 07-10/Build Isolated Unity Candidates v02")]
    public static void BuildAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before building cinematics.");
        Scene original = SceneManager.GetActiveScene();
        if (original.IsValid() && original.isLoaded && original.isDirty)
            throw new InvalidOperationException("Unsaved current scene: refused to switch away from " + original.path);
        if (original.IsValid() && original.isLoaded && original.path.Contains("Stage3Scene"))
            throw new InvalidOperationException("Stage3Scene is active: choose a safe clean cinematic workbench before building.");

        foreach (string p in new [] { "07", "08", "09", "10" })
            ValidateInputs(p);

        foreach (string p in new [] { "07", "08", "09", "10" })
            BuildPanel(p);
        Debug.Log("[P07-10] Four isolated candidate Unity scenes and Timeline assets created. Live Game View playback NOT YET VALIDATED.");
    }

    static string[] ActorNames(string p)
    {
        if (p == "07") return new [] { "YoungVan" };
        if (p == "08") return new [] { "VanderPoel" };
        return new [] { "OlderVan", "VanderPoel" };
    }

    static string PanelFolder(string p) { return AssetRoot + "/Panel" + p; }
    static string ScenePath(string p) { return SceneRoot + "/Prologue_P" + p + "_TimelineCandidate_v02.unity"; }
    static string TimelinePath(string p) { return TimelineRoot + "/P" + p + "_Cinematic_v02.playable"; }

    static void ValidateInputs(string p)
    {
        if (File.Exists(ScenePath(p)) || File.Exists(TimelinePath(p)))
            throw new IOException("Versioned cinematic output already exists for Panel " + p);
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PanelFolder(p) + "/P" + p + "_CanonicalLibrary_Env_v01.glb") == null)
            throw new InvalidOperationException("Import the canonical library GLB before building panel " + p);
        foreach (string actor in ActorNames(p))
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PanelFolder(p) + "/P" + p + "_" + actor + "_CinematicRefinement_Animated_v02.glb") == null)
                throw new InvalidOperationException("Missing imported animated actor " + actor + " for " + p);
    }

    static void BuildPanel(string p)
    {
        if (!Directory.Exists(SceneRoot)) throw new DirectoryNotFoundException(SceneRoot);
        if (!AssetDatabase.IsValidFolder(TimelineRoot))
        {
            string parent = "Assets/Cinematics/Storyboard3D/Timelines";
            AssetDatabase.CreateFolder(parent, "Panels07_10_v02");
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        GameObject root = new GameObject("P" + p + "_CINEMATIC_CANDIDATE_ROOT");
        SceneManager.MoveGameObjectToScene(root, scene);
        var environmentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PanelFolder(p) + "/P" + p + "_CanonicalLibrary_Env_v01.glb");
        GameObject library = (GameObject)PrefabUtility.InstantiatePrefab(environmentPrefab, scene);
        library.name = "P" + p + "_CANONICAL_LIBRARY";
        library.transform.SetParent(root.transform, false);
        library.transform.localScale = Vector3.one;
        if (library.GetComponentsInChildren<Renderer>(true).Length < 150)
            throw new InvalidOperationException("Imported canonical library appears incomplete in P" + p);

        TimelineAsset tl = ScriptableObject.CreateInstance<TimelineAsset>();
        tl.name = "P" + p + " - Eight second cinematic - review candidate";
        tl.editorSettings.fps = 24;
        AssetDatabase.CreateAsset(tl, TimelinePath(p));
        GameObject directorObject = new GameObject("P" + p + "_TIMELINE_8S_CANDIDATE");
        SceneManager.MoveGameObjectToScene(directorObject, scene);
        var director = directorObject.AddComponent<PlayableDirector>();
        director.playableAsset = tl;
        director.playOnAwake = true;
        director.extrapolationMode = DirectorWrapMode.Hold;
        foreach (string actor in ActorNames(p))
        {
            string source = PanelFolder(p) + "/P" + p + "_" + actor + "_CinematicRefinement_Animated_v02.glb";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(source);
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            go.name = "P" + p + "_" + actor + "_ANIMATED";
            go.transform.SetParent(root.transform, false);
            go.transform.localScale = Vector3.one;
            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(source).OfType<AnimationClip>()
                .Where(c => c != null && !c.name.StartsWith("__preview__") && c.length > 3.0f)
                .OrderByDescending(c => c.length).ToArray();
            if (clips.Length == 0)
                throw new InvalidOperationException("No animation clips imported from " + source);
            Animator anim = go.GetComponentInChildren<Animator>(true);
            if (anim == null) anim = go.AddComponent<Animator>();
            var track = tl.CreateTrack<AnimationTrack>(null, actor + " | Imported rig performance");
            var clip = track.CreateClip(clips[0]);
            clip.start = 0;clip.duration = Duration;
            director.SetGenericBinding(track, anim);
        }

        GameObject camObject = new GameObject("P" + p + "_CAMERA_PROVISIONAL__MATCH_IN_EDITOR");
        camObject.tag = "MainCamera";
        SceneManager.MoveGameObjectToScene(camObject, scene);
        Camera cam = camObject.AddComponent<Camera>();
        cam.nearClipPlane = .07f;cam.farClipPlane = 400f;
        cam.fieldOfView = 40f;
        camObject.AddComponent<AudioListener>();
        // Camera is deliberately not given guessed Blender-to-Unity coordinates.
        // Match imported bounds and reviewed camera JSON in the visible Editor before approval.
        camObject.transform.position = new Vector3(0f, 1.5f, -8f);
        camObject.transform.rotation = Quaternion.identity;
        var cameraTrack = tl.CreateTrack<ActivationTrack>(null, "CAMERA | Eight-second shot, awaiting visible Editor calibration");
        var cameraClip = cameraTrack.CreateDefaultClip();
        cameraClip.start = 0;cameraClip.duration = Duration;
        director.SetGenericBinding(cameraTrack, camObject);
        director.RebuildGraph();
        director.time = 0; director.Evaluate();
        if (!EditorSceneManager.SaveScene(scene, ScenePath(p)))
            throw new IOException("Could not save isolated Panel " + p + " scene");
        EditorSceneManager.CloseScene(scene, true);
        EditorUtility.SetDirty(tl);
        AssetDatabase.SaveAssets();
        Debug.Log("[P" + p + "] Created versioned scene/Timeline; requires visible camera, scale, shaders, animation and playback validation.");
    }
}
#endif




