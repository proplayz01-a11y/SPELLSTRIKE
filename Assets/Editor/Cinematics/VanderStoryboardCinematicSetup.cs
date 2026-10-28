#if UNITY_EDITOR
// Visible, guarded Editor authoring for the isolated storyboard workbench.
// Never auto-executes, opens gameplay scenes, or changes the Final Boss prefab.
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

internal static class VanderStoryboardCinematicSetup
{
    private const string Root = "Assets/Cinematics/Storyboard3D/Characters/VanderPoel";
    private const string Master = Root + "/Models/VanderPoel_StandingIdle_WithSkin.fbx";
    private const string Workbench = "Assets/Scenes/Cinematics/Storyboard3D_CinematicWorkbench.unity";
    private const string SceneBackup = "Library/VanderStoryboardTimeline_20261021/StoryboardWorkbench_BeforeVander.unity";
    private const string PrefabPath = Root + "/Prefabs/VanderPoel_CinematicActor.prefab";
    private const string MaterialPath = Root + "/Materials/VanderPoel_Cinematic_ShadedURP.mat";
    private const string TimelinePath = "Assets/Cinematics/Storyboard3D/Timelines/VanderPoel_StandingIdle_Preview.playable";
    private const string Mark = "VANDER_POEL_CINEMATIC_ACTOR_PLACEHOLDER";
    private const string DirectorMark = "Timeline_Director_ASSIGN_PLAYABLE_ASSET_LATER";
    private static readonly string[] Animations =
    {
        Root + "/Animations/VanderPoel_Point.fbx",
        Root + "/Animations/VanderPoel_SitToStand.fbx",
        Root + "/Animations/VanderPoel_SittingIdle.fbx",
        Root + "/Animations/VanderPoel_StandToSit.fbx",
        Root + "/Animations/VanderPoel_Talking_A.fbx",
        Root + "/Animations/VanderPoel_Talking_B.fbx",
        Root + "/Animations/VanderPoel_Thinking.fbx",
        Root + "/Animations/VanderPoel_Walking.fbx",
    };

    [MenuItem("SPELLSTRIKE/Cinematics/Vander/1 Configure Mixamo Humanoid Rigs")]
    private static void ConfigureRigs()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[Vander Cinematics] Rig setup requires Edit Mode.");
            return;
        }
        try
        {
            if (!Animations.All(File.Exists) || !File.Exists(Master))
                throw new FileNotFoundException("Staged Vander Mixamo FBXs missing. No importer changed.");

            // The With Skin FBX is authoritative. Animation-only files share its skeleton.
            var modelImporter = AssetImporter.GetAtPath(Master) as ModelImporter;
            if (modelImporter == null)
                throw new InvalidOperationException("Vander master FBX is not imported yet.");
            modelImporter.animationType = ModelImporterAnimationType.Human;
            modelImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            modelImporter.importAnimation = true;
            modelImporter.SaveAndReimport();

            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(Master).OfType<Avatar>()
                .FirstOrDefault(x => x.isValid && x.isHuman);
            if (avatar == null)
                throw new InvalidOperationException("Mixamo master Humanoid Avatar is invalid. Inspect Rig tab; other importers unchanged.");

            foreach (string path in Animations)
            {
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null)
                    throw new InvalidOperationException("Missing FBX importer: " + path);
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                importer.sourceAvatar = avatar;
                importer.importAnimation = true;
                importer.SaveAndReimport();
                if (GetClip(path) == null)
                    throw new InvalidOperationException("No animation clip imported from " + path);
            }

            Debug.Log("[Vander Cinematics] PASS: one valid Humanoid master Avatar + 8 " +
                      "animation-only Mixamo FBXs. Original Downloads files unchanged. " +
                      "Now open and SAVE the cinematic workbench in Edit Mode, then use menu step 2.");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[Vander Cinematics] Rig setup stopped. Inspect the exact FBX import state before retrying.");
        }
    }

    [MenuItem("SPELLSTRIKE/Cinematics/Vander/2 Build Actor and Idle Timeline (Workbench Only)")]
    private static void BuildPreview()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            !scene.IsValid() || !scene.isLoaded || scene.path != Workbench ||
            scene.isDirty || SceneManager.sceneCount != 1)
        {
            Debug.LogError("[Vander Cinematics] Open the saved isolated cinematic workbench " +
                           "as the ONLY loaded scene in Edit Mode first. No changes made.");
            return;
        }
        try
        {
            if (File.Exists(SceneBackup) || File.Exists(PrefabPath) ||
                File.Exists(MaterialPath) || File.Exists(TimelinePath))
                throw new InvalidOperationException("Existing backup or authoring asset found; refusing an overwrite.");
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(Master).OfType<Avatar>()
                .FirstOrDefault(x => x.isHuman && x.isValid);
            if (avatar == null)
                throw new InvalidOperationException("Run menu 1 first, and check the Humanoid Avatar.");

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(Master);
            AnimationClip idle = GetClip(Master);
            Texture2D shade = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/shaded.png");
            if (model == null || idle == null || shade == null)
                throw new InvalidOperationException("Required model, Idle clip, or diffuse texture unavailable.");

            Transform[] all = scene.GetRootGameObjects()
                .SelectMany(x => x.GetComponentsInChildren<Transform>(true)).ToArray();
            Transform[] markers = all.Where(x => x.name == Mark).ToArray();
            PlayableDirector[] directors = scene.GetRootGameObjects()
                .SelectMany(x => x.GetComponentsInChildren<PlayableDirector>(true)).ToArray();
            if (markers.Length != 1 || directors.Length != 1 ||
                directors[0].gameObject.name != DirectorMark ||
                directors[0].playableAsset != null || markers[0].childCount != 0)
                throw new InvalidOperationException("Workbench markers or director changed. No overwrite allowed.");

            // Back up even though the scene is untracked, before authoring the scene.
            Directory.CreateDirectory(Path.GetDirectoryName(SceneBackup));
            File.Copy(Workbench, SceneBackup, false);

            EnsureFolder(Root, "Materials");
            EnsureFolder(Root, "Prefabs");
            EnsureFolder("Assets/Cinematics/Storyboard3D", "Timelines");

            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null)
                throw new InvalidOperationException("URP Lit shader unavailable; do not create incorrect material.");
            Material material = new Material(lit) { name = "VanderPoel_Cinematic_ShadedURP" };
            material.SetTexture("_BaseMap", shade);
            material.SetFloat("_Smoothness", 0.3f);
            AssetDatabase.CreateAsset(material, MaterialPath);

            GameObject container = new GameObject("VanderPoel_CinematicActor");
            try
            {
                GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
                if (visual == null) throw new InvalidOperationException("Could not instantiate Mixamo model.");
                visual.name = "VanderPoel_VisualRig_Mixamo";
                visual.transform.SetParent(container.transform, false);
                Animator animator = visual.GetComponentInChildren<Animator>(true);
                SkinnedMeshRenderer[] skins = visual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                if (animator == null || skins.Length != 1)
                    throw new InvalidOperationException("Expected one Animator and one skinned Vander renderer.");

                animator.avatar = avatar;
                animator.applyRootMotion = false;
                animator.runtimeAnimatorController = null;
                skins[0].sharedMaterial = material;
                PrefabUtility.SaveAsPrefabAsset(container, PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(container);
            }

            // Intentional six-second isolated idle test, not a voiced/prologue cinematic.
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "VanderPoel_StandingIdle_Preview";
            AssetDatabase.CreateAsset(timeline, TimelinePath);
            AnimationTrack track = timeline.CreateTrack<AnimationTrack>(null, "Vander Poel - Standing Idle Preview");
            TimelineClip timelineClip = track.CreateClip(idle);
            if (timelineClip == null)
                throw new InvalidOperationException("Could not create the initial idle Timeline clip.");
            timelineClip.start = 0;
            timelineClip.duration = Math.Min(6.0, Math.Max(1.0, idle.length));
            timelineClip.displayName = "Vander - Standing Idle (test only)";

            GameObject savedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(savedPrefab, scene);
            instance.transform.SetParent(markers[0], false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            Animator sceneAnimator = instance.GetComponentInChildren<Animator>(true);
            if (sceneAnimator == null)
                throw new InvalidOperationException("Saved cinematic actor lost its Animator.");

            PlayableDirector director = directors[0];
            director.playableAsset = timeline;
            director.playOnAwake = false;
            director.SetGenericBinding(track, sceneAnimator);

            EditorUtility.SetDirty(director);
            EditorUtility.SetDirty(timeline);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, Workbench))
                throw new IOException("Could not save isolated cinematic scene.");

            Debug.Log("[Vander Cinematics] PASS: saved isolated Vander cinematic prefab and " +
                      "Standing Idle preview Timeline, bound to the workbench PlayableDirector. " +
                      "Camera/blockout/voice/full sequence unchanged; production scenes and Build Settings untouched. " +
                      "Scene backup: " + SceneBackup);
            Selection.activeGameObject = instance;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[Vander Cinematics] Authoring stopped. Inspect state and backup; do not rerun blindly.");
        }
    }

    [MenuItem("SPELLSTRIKE/Cinematics/Vander/3 Fix Workbench Camera and Enable Idle Preview")]
    private static void EnableWorkbenchPreview()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            !scene.IsValid() || !scene.isLoaded || scene.path != Workbench ||
            scene.isDirty || SceneManager.sceneCount != 1)
        {
            Debug.LogError("[Vander Cinematics] Preview setup requires a saved, isolated workbench in Edit Mode.");
            return;
        }
        try
        {
            Transform[] all = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
            Transform[] cameras = all.Where(t => t.name == "Main_CinematicCamera_ANIMATE_IN_TIMELINE").ToArray();
            PlayableDirector[] directors = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<PlayableDirector>(true)).ToArray();
            if (cameras.Length != 1 || cameras[0].GetComponent<Camera>() == null ||
                directors.Length != 1 || directors[0].gameObject.name != DirectorMark ||
                AssetDatabase.GetAssetPath(directors[0].playableAsset) != TimelinePath)
                throw new InvalidOperationException("Camera, Director, or preview Timeline differs from expected workbench; stopped.");

            // The hand-authored workbench stored (0.105,0,0,0.994), which is
            // not unit-length and repeatedly warns during URP camera culling.
            Quaternion oldRotation = cameras[0].localRotation;
            Undo.RecordObject(cameras[0], "Normalize isolated cinematic camera rotation");
            cameras[0].localRotation = Quaternion.Normalize(oldRotation);
            Undo.RecordObject(directors[0], "Enable six-second isolated Vander preview");
            directors[0].playOnAwake = true;
            EditorUtility.SetDirty(cameras[0]);
            EditorUtility.SetDirty(directors[0]);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, Workbench))
                throw new IOException("Could not save preview changes.");

            Debug.Log("[Vander Cinematics] PASS: normalized existing cinematic camera quaternion and " +
                      "enabled Director Play On Awake for six-second Standing Idle preview. " +
                      "No new shots, gameplay, or story routing modified.");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[Vander Cinematics] Preview setup stopped; inspect workbench before retrying.");
        }
    }

    private static AnimationClip GetClip(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
            .Where(x => !x.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.length).FirstOrDefault();
    }

    private static void EnsureFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + child))
            AssetDatabase.CreateFolder(parent, child);
    }
}
#endif
