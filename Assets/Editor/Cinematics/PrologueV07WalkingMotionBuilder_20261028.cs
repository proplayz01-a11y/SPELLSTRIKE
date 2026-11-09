#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

/// <summary>
/// Adds actual Mixamo gait, with a hidden Humanoid source rig and an explicit
/// rotation retargeter, to the EXISTING canonical 65-bone Vander story mesh.
/// The existing 8s corridor movement Timeline remains authoritative.
/// Versioned v07 only. Once-only, fail-closed.
/// </summary>
internal static class PrologueV07WalkingMotionBuilder_20261028
{
    private const string ScenePath = "Assets/Scenes/Cinematics/Prologue_Master_Sequence_P01_42_v07_SpellbookWalkPolish.unity";
    private const string Folder = "Assets/Cinematics/Storyboard3D/Timelines/PrologueMaster_v07_SpellbookWalkPolish/";
    private const string Original = "Assets/Scenes/Cinematics/Prologue_Master_Sequence_P01_42_v06_DialogueContinuity.unity";
    private const string Backup = "RecoveryBackups/ProloguePolishV07_20261028_Immutable_v01/Prologue_Master_v06_BEFORE_POLISH.unity";
    private const string OriginalHash = "283573BAA9EB7C9E5D3CEF9290FFDDC3DF1F026B8C8D65EAF6D84A73CB26EBF4";
    private const string SourceWalkFbx = "Assets/Cinematics/Storyboard3D/Characters/VanderPoel/Animations/VanderPoel_Walking.fbx";
    private const string WalkTrackName = "V07 | REAL Mixamo Vander full-body walking gait";

    [MenuItem("SPELLSTRIKE/Cinematics/Prologue V07/04 Build Real Vander P34-35 Walk %#F11")]
    private static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            EditorApplication.isUpdating || SceneManager.sceneCount != 1 ||
            SceneManager.GetActiveScene().isDirty ||
            System.Diagnostics.Process.GetProcessesByName("VALORANT-Win64-Shipping").Length != 0)
        {
            Debug.LogError("[V07 REAL WALK] Editor busy/dirty, scene count wrong or VALORANT running.");
            return;
        }
        if (Hash(Original) != OriginalHash || Hash(Backup) != OriginalHash ||
            AssetDatabase.AssetPathToGUID(ScenePath) != "12405e3bd39d43d398685e74678a8516")
        {
            Debug.LogError("[V07 REAL WALK] Protected source/backup/v07 GUID changed.");
            return;
        }

        var clip = AssetDatabase.LoadAllAssetsAtPath(SourceWalkFbx)
            .OfType<AnimationClip>().SingleOrDefault(c => c.name == "mixamo.com");
        if (clip == null || !clip.humanMotion || clip.length < 1.2f || clip.length > 2.5f)
        {
            Debug.LogError("[V07 REAL WALK] Real imported Mixamo walking clip invalid.");
            return;
        }
        var sceneWas = SceneManager.GetActiveScene();
        Scene scene = default;
        try
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            var root = scene.GetRootGameObjects().Single(g =>
                g.name == "MASTER_SHOT_GROUPS__NUMBERED_IN_STORY_ORDER");
            var source = root.transform.Find("P15_P18_SHOT_ROOT")
                .GetComponentsInChildren<Animator>(true)
                .Single(a => a.name == "VanderPoel_VisualRig_Mixamo" &&
                    AncestorName(a.transform,"STORYBOARD_P15_P18__TIMELINE_BLOCKING_V01"));
            if (!source.avatar || !source.avatar.isHuman || !source.avatar.isValid)
                throw new InvalidOperationException("Approved P15 Vander source rig lacks valid Humanoid Avatar.");
            var shots = new System.Collections.Generic.List<(int p, Transform shot, PlayableDirector director,
                TimelineAsset timeline, SkinnedMeshRenderer visual)>();
            for (int panel = 34; panel <= 35; panel++)
            {
                var shot = root.transform.Find("P" + panel + "_SHOT_ROOT");
                if (shot == null || shot.Find("V07_HIDDEN_MIXAMO_WALK_DRIVER") != null ||
                    shot.GetComponent<PrologueV07VanderWalkRetarget>() != null)
                    throw new InvalidOperationException("P" + panel + " missing or already migrated.");
                var director = shot.GetComponentsInChildren<PlayableDirector>(true)
                    .Single(d => d.playableAsset is TimelineAsset);
                var timeline = (TimelineAsset)director.playableAsset;
                if (AssetDatabase.GetAssetPath(timeline) != Folder +
                    (panel == 34 ? "P34_P34_DialogueAggressive_v05_v07.playable" :
                        "P35_P35_VanderReturnsWithStaff_8s_v01_v07.playable") ||
                    timeline.duration < 7.9 || timeline.duration > 8.1 ||
                    timeline.GetOutputTracks().Any(t => t.name == WalkTrackName) ||
                    !timeline.GetOutputTracks().Any(t => t.name == "P" + panel + "_GatewayMovement"))
                    throw new InvalidOperationException("P" + panel + " missing independent v07 Timeline or existing corridor track.");
                var targetAnimator = shot.GetComponentsInChildren<Animator>(true)
                    .Single(a => a.name == "P10_VanderPoel_ANIMATED");
                var targetVisual = targetAnimator.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .Single(r => r.sharedMesh && r.sharedMesh.vertexCount == 19225 && r.bones.Length == 65);
                if (targetVisual.bones.Any(b => b == null))
                    throw new InvalidOperationException("Canonical P" + panel + " story mesh has missing bones.");
                shots.Add((panel,shot,director,timeline,targetVisual));
            }

            foreach (var s in shots)
            {
                var driverObject = UnityEngine.Object.Instantiate(source.gameObject,s.shot,false);
                driverObject.name = "V07_HIDDEN_MIXAMO_WALK_DRIVER";
                driverObject.transform.localPosition = Vector3.zero;
                driverObject.transform.localRotation = Quaternion.identity;
                driverObject.SetActive(true);
                // Keep the actual source skeleton alive and animated while removing
                // its visual impact. The canonical P10 model stays the only visible Vander.
                foreach (var renderer in driverObject.GetComponentsInChildren<Renderer>(true))
                    renderer.enabled = false;
                foreach (var cam in driverObject.GetComponentsInChildren<Camera>(true))
                    cam.enabled = false;
                foreach (var col in driverObject.GetComponentsInChildren<Collider>(true))
                    col.enabled = false;
                var driver = driverObject.GetComponent<Animator>();
                if (driver == null || !driver.avatar || !driver.avatar.isHuman ||
                    driver.GetComponentsInChildren<Transform>(true).Length < 50)
                    throw new InvalidOperationException("P" + s.p + " hidden walking source is not a compatible Mixamo rig.");
                driver.runtimeAnimatorController = null;
                driver.applyRootMotion = false;
                driver.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                float walkStart = s.p == 34 ? 2.2f : 0f;
                float walkEnd = s.p == 34 ? 8f : 6.1f;
                var retarget = s.shot.gameObject.AddComponent<PrologueV07VanderWalkRetarget>();
                retarget.Configure(driver, s.visual,s.director,walkStart,walkEnd);
                if (retarget.MappedBoneCount < 45 || retarget.MappedLegBoneCount != 8)
                    throw new InvalidOperationException("P" + s.p +
                        " walk transfer has insufficient canonical joint mappings: " +
                        retarget.MappedBoneCount + " total, " + retarget.MappedLegBoneCount + " leg joints");

                var track = s.timeline.CreateTrack<AnimationTrack>(null,WalkTrackName);
                var timelineClip = track.CreateClip<AnimationPlayableAsset>();
                timelineClip.displayName = "P" + s.p + " true full-body Vander walk on Humanoid rig";
                timelineClip.start = walkStart;
                timelineClip.duration = walkEnd-walkStart;
                timelineClip.easeInDuration = .32;
                timelineClip.easeOutDuration = .48;
                var playable = timelineClip.asset as AnimationPlayableAsset;
                if (playable == null) throw new InvalidOperationException("AnimationPlayableAsset creation failed.");
                playable.clip = clip;
                playable.loop = AnimationPlayableAsset.LoopMode.On;
                playable.removeStartOffset = false;
                s.director.SetGenericBinding(track,driver);
                if (s.director.GetGenericBinding(track) != driver)
                    throw new InvalidOperationException("P" + s.p + " Timeline-to-driver binding not retained.");
                EditorUtility.SetDirty(s.timeline);
                EditorUtility.SetDirty(s.director);
                Debug.Log("[V07 REAL WALK] P" + s.p + " Mixamo full-body walk, source " +
                    AssetDatabase.GetAssetPath(clip) + " " + timelineClip.start + "-" +
                    (timelineClip.start + timelineClip.duration) + "s, bound source Humanoid and mirrored " +
                    retarget.MappedBoneCount + "/65 matching canonical P10 skinned joints, " +
                    retarget.MappedLegBoneCount + " leg joints actually transferred; " +
                    "head, spine, staff hand remain owned by original P34/P35 tracks. Corridor movement retained.");
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene,ScenePath))
                throw new IOException("Versioned v07 save failed after walking integration.");
            Debug.Log("[V07 REAL WALK] SAVED independent P34 + P35 walking Timelines and canonical rig transfer. " +
                "v06 untouched. Preview/testing and route promotion still pending.");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[V07 REAL WALK] Incomplete or unsaved. Inspect v07 only. Do not modify original v06.");
        }
        finally
        {
            if (sceneWas.IsValid() && sceneWas.isLoaded) SceneManager.SetActiveScene(sceneWas);
            if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene,true);
        }
    }

    private static bool AncestorName(Transform transform,string name)
    {
        while (transform) { if (transform.name == name) return true; transform = transform.parent; }
        return false;
    }

    private static string Hash(string path)
    {
        if (!File.Exists(path)) return "";
        using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "");
    }
}
#endif
