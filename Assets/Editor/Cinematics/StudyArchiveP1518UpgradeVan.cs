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
/// One-shot edit for the P15-18 *cinematic instance only*: replaces obsolete
/// VAN AERT with canonical VanAertPlayer and rebinds its humanoid animation.
/// Never edits or saves the source player prefab / gameplay scenes.
/// </summary>
internal static class StudyArchiveP1518UpgradeVan
{
    private const string ScenePath =
        "Assets/Scenes/Cinematics/StudyArchive_CinematicSet_v01.unity";
    private const string CanonicalPath = "Assets/Prefabs/VanAertPlayer.prefab";
    private const string BackupPath =
        "Library/StoryboardTimeline_P15_P18_20261023/StudyArchive_BEFORE_UpdatedVan.unity";
    private const string MotionRoot = "OLDER_VAN_BLOCKING_RIG";
    private const string VisualName = MotionRoot + "_VISUAL_ONLY";
    private const string TrackName = "Older Van HUMANOID motions";

    [MenuItem("SPELLSTRIKE/Cinematics/Library/10 Replace Legacy Van with Updated Van %#u")]
    private static void Replace()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            SceneManager.sceneCount != 1 || !scene.isLoaded ||
            scene.path != ScenePath || scene.isDirty)
        {
            Debug.LogError("[P15-18 Van Upgrade] Open and SAVE the isolated " +
                "StudyArchive scene in Edit Mode first. No changes made.");
            return;
        }
        if (File.Exists(BackupPath))
        {
            Debug.LogError("[P15-18 Van Upgrade] Upgrade backup already exists. " +
                "Refusing a repeated destructive replacement.");
            return;
        }
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CanonicalPath);
        if (prefab == null)
        {
            Debug.LogError("[P15-18 Van Upgrade] Missing canonical VanAertPlayer prefab.");
            return;
        }
        var all = scene.GetRootGameObjects()
            .SelectMany(go => go.GetComponentsInChildren<Transform>(true)).ToArray();
        var motion = all.SingleOrDefault(t => t.name == MotionRoot);
        if (motion == null)
        {
            Debug.LogError("[P15-18 Van Upgrade] Expected cinematic motion root absent.");
            return;
        }
        var legacy = motion.Find(VisualName);
        if (legacy == null || motion.GetComponentsInChildren<Animator>(true).Length < 2)
        {
            Debug.LogError("[P15-18 Van Upgrade] Legacy Van not present exactly once.");
            return;
        }
        var directors = scene.GetRootGameObjects()
            .SelectMany(go => go.GetComponentsInChildren<PlayableDirector>(true))
            .Where(d => d.playableAsset is TimelineAsset).ToArray();
        var director = directors.SingleOrDefault(d => d.gameObject.name.StartsWith(
            "04_TIMELINE_DIRECTOR__", StringComparison.Ordinal));
        if (director == null)
        {
            Debug.LogError("[P15-18 Van Upgrade] Cinematic director not found.");
            return;
        }
        var timeline = director.playableAsset as TimelineAsset;
        var humanoidTrack = timeline.GetOutputTracks().OfType<AnimationTrack>()
            .SingleOrDefault(track => track.name == TrackName);
        if (humanoidTrack == null)
        {
            Debug.LogError("[P15-18 Van Upgrade] Van Humanoid Timeline track missing.");
            return;
        }
        var boundOld = director.GetGenericBinding(humanoidTrack) as Animator;
        if (boundOld == null || !boundOld.transform.IsChildOf(legacy))
        {
            Debug.LogError("[P15-18 Van Upgrade] Track binding differs from legacy instance.");
            return;
        }

        // Inspect prefab rig before making any scene or user file changes.
        var prefabRig = prefab.GetComponentsInChildren<Animator>(true)
            .FirstOrDefault(a => a.avatar != null && a.avatar.isValid && a.avatar.isHuman);
        if (prefabRig == null)
        {
            Debug.LogError("[P15-18 Van Upgrade] Canonical prefab Humanoid Avatar invalid.");
            return;
        }
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(BackupPath));
            File.Copy(ScenePath, BackupPath, false);

            var updated = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            if (updated == null)
                throw new InvalidOperationException("Canonical prefab instantiation failed.");
            updated.name = VisualName;
            updated.transform.SetParent(motion, false);
            updated.transform.localPosition = Vector3.zero;
            updated.transform.localRotation = Quaternion.identity;
            updated.transform.localScale = Vector3.one;

            // Gameplay systems must NEVER drive a cinematic-only actor.
            foreach (var script in updated.GetComponentsInChildren<MonoBehaviour>(true))
                if (script != null) script.enabled = false;
            foreach (var body in updated.GetComponentsInChildren<Rigidbody>(true))
            {
                body.isKinematic = true;
                body.detectCollisions = false;
            }
            foreach (var collider in updated.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;
            foreach (var camera in updated.GetComponentsInChildren<Camera>(true))
                camera.enabled = false;
            foreach (var listener in updated.GetComponentsInChildren<AudioListener>(true))
                listener.enabled = false;

            var newRig = updated.GetComponentsInChildren<Animator>(true)
                .FirstOrDefault(a => a.avatar != null && a.avatar.isValid && a.avatar.isHuman);
            if (newRig == null)
                throw new InvalidOperationException("New Van scene rig has no valid Humanoid Avatar.");
            newRig.runtimeAnimatorController = null;
            newRig.applyRootMotion = false;
            newRig.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            director.SetGenericBinding(humanoidTrack, newRig);
            if (director.GetGenericBinding(humanoidTrack) != newRig)
                throw new InvalidOperationException("Humanoid Timeline track not rebound.");

            // Only destroy the old *scene instance*, never the old prefab asset
            // nor the production VanAertPlayer source.
            UnityEngine.Object.DestroyImmediate(legacy.gameObject);
            EditorUtility.SetDirty(director);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new IOException("Saving updated Van scene failed.");

            Debug.Log("[P15-18 Van Upgrade] PASS: canonical updated " +
                "VanAertPlayer.prefab replaces legacy cinematic Van; " +
                "Humanoid Timeline binding now targets updated Avatar. " +
                "Path clips, Vander, books and cameras preserved. Source " +
                "prefabs/gameplay scenes untouched; backup: " + BackupPath);
            Selection.activeGameObject = updated;
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            Debug.LogError("[P15-18 Van Upgrade] Upgrade interrupted. Restore " +
                "the backed-up isolated scene only after inspecting its state.");
        }
    }
}
#endif
