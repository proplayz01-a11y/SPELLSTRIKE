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

/// <summary>One-shot versioned 3D Timeline polish. Never changes v06 or older assets.</summary>
internal static class PrologueV07SpellbookPolishBuilder_20261028
{
    private const string Original = "Assets/Scenes/Cinematics/Prologue_Master_Sequence_P01_42_v06_DialogueContinuity.unity";
    private const string V07 = "Assets/Scenes/Cinematics/Prologue_Master_Sequence_P01_42_v07_SpellbookWalkPolish.unity";
    private const string AssetsRoot = "Assets/Cinematics/Storyboard3D/Timelines/PrologueMaster_v07_SpellbookWalkPolish/";
    private const string Backup = "RecoveryBackups/ProloguePolishV07_20261028_Immutable_v01/Prologue_Master_v06_BEFORE_POLISH.unity";
    private const string ExpectedOriginalHash = "283573BAA9EB7C9E5D3CEF9290FFDDC3DF1F026B8C8D65EAF6D84A73CB26EBF4";
    private static readonly int[] BookPanels = {19,20,21,25,26,29,30};

    [MenuItem("SPELLSTRIKE/Cinematics/Prologue V07/02 Build Spellbook and P15-18 Walk Polish %#8")]
    private static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            EditorApplication.isUpdating || SceneManager.sceneCount != 1 ||
            SceneManager.GetActiveScene().isDirty ||
            System.Diagnostics.Process.GetProcessesByName("VALORANT-Win64-Shipping").Length != 0)
        {
            Debug.LogError("[PrologueV07] Editor busy/dirty, extra scene loaded, or VALORANT running. Refusing.");
            return;
        }
        if (Hash(Original) != ExpectedOriginalHash || Hash(Backup) != ExpectedOriginalHash ||
            AssetDatabase.AssetPathToGUID(Original) != "143e0b853ca11b84190743d04aedaefe")
        {
            Debug.LogError("[PrologueV07] Protected original/backup SHA or original GUID changed. Refusing.");
            return;
        }
        if (!File.Exists(V07) || AssetDatabase.AssetPathToGUID(V07) != "12405e3bd39d43d398685e74678a8516")
        {
            Debug.LogError("[PrologueV07] Missing expected versioned master scene/GUID.");
            return;
        }
        if (BookPanels.Any(p => File.Exists(AssetsRoot + "P" + p.ToString("00") + "_BookIllumination_v07.anim")))
        {
            Debug.LogError("[PrologueV07] v07 Timeline book animation already exists. Never repeat/overwrite.");
            return;
        }
        var current = SceneManager.GetActiveScene();
        Scene scene = default;
        try
        {
            scene = EditorSceneManager.OpenScene(V07, OpenSceneMode.Additive);
            var main = scene.GetRootGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<PlayableDirector>(true))
                .SingleOrDefault(d => d.name.StartsWith("PROLOGUE_MASTER_TIMELINE_DIRECTOR", StringComparison.Ordinal));
            if (main == null || !(main.playableAsset is TimelineAsset master) ||
                AssetDatabase.GetAssetPath(master) != AssetsRoot + "Prologue_Master_P01_42_Sequence_v07.playable" ||
                master.duration < 336 || master.duration > 338)
                throw new InvalidOperationException("v07 master 42-panel director, independent master Timeline or 336.6s duration missing.");

            var group = scene.GetRootGameObjects().Single(x =>
                x.name == "MASTER_SHOT_GROUPS__NUMBERED_IN_STORY_ORDER");
            var bookDirectors = new System.Collections.Generic.List<(int, PlayableDirector, TimelineAsset, Transform)>();
            foreach (int panel in BookPanels)
            {
                var shot = group.transform.Find("P" + panel.ToString("00") + "_SHOT_ROOT");
                if (shot == null) throw new InvalidOperationException("Missing shot root P" + panel);
                var d = shot.GetComponentsInChildren<PlayableDirector>(true).SingleOrDefault(
                    x => x.playableAsset is TimelineAsset);
                if (d == null || !(d.playableAsset is TimelineAsset t) ||
                    !AssetDatabase.GetAssetPath(t).StartsWith(AssetsRoot, StringComparison.Ordinal) ||
                    t.duration < 7.9 || t.duration > 8.1)
                    throw new InvalidOperationException("P" + panel + " not bound to its independent eight-second v07 Timeline.");
                if (shot.Find("V07_ILLUMINATED_SPELLBOOK_LIGHT") != null)
                    throw new InvalidOperationException("P" + panel + " already has a versioned book light.");
                bookDirectors.Add((panel,d,t,shot));
            }
            var walkingDirector = group.GetComponentsInChildren<PlayableDirector>(true)
                .SingleOrDefault(d => d.name.StartsWith("04_TIMELINE_DIRECTOR__") &&
                    AssetDatabase.GetAssetPath(d.playableAsset).StartsWith(AssetsRoot, StringComparison.Ordinal));
            if (walkingDirector == null || !(walkingDirector.playableAsset is TimelineAsset walking) ||
                walking.duration < 27.9 || walking.duration > 28.1)
                throw new InvalidOperationException("P15-18 independent 28s motion Timeline not present.");
            var walkLabels = new[] {
                "P16 walk into archive", "P18 return carrying book",
                "P16 follow Vander", "P18 follow back to table"
            };
            var sourceWalks = walking.GetOutputTracks().OfType<AnimationTrack>()
                .SelectMany(t => t.GetClips()).Where(c => walkLabels.Contains(c.displayName)).ToArray();
            if (sourceWalks.Length != 4 ||
                sourceWalks.Any(c => !(c.asset is AnimationPlayableAsset a) ||
                    a.clip == null || a.loop != AnimationPlayableAsset.LoopMode.On))
                throw new InvalidOperationException("Missing four already-approved looping Mixamo walk clips.");

            var expectedText = "Please help me.\nI've been trapped in this book for so long.\nMy name is Nevali.";
            // P26's canonical text is part of the already-authored book art, not an overlay.
            // We deliberately don't create ANY TMP_Text, dialog or caption component.
            foreach (var pair in bookDirectors)
            {
                var shot = pair.Item4;
                var book = shot.GetComponentsInChildren<Transform>(true).FirstOrDefault(t =>
                    t.name == (pair.Item1 == 19
                        ? "P19_ANCIENT_VOLUME_CLOSED__PHYSICAL"
                        : "P20_SINGLE_OPEN_ANCIENT_VOLUME__ILLUSTRATED"));
                if (book == null) throw new InvalidOperationException("Missing original physical book prop on P" + pair.Item1);
                var candidate = book.GetComponentsInChildren<Renderer>(true)
                    .Where(r => r.bounds.size.sqrMagnitude > 0.0001f &&
                        r.bounds.size.sqrMagnitude < 400f)
                    .OrderByDescending(r => r.bounds.size.x * r.bounds.size.z).FirstOrDefault();
                if (candidate == null) throw new InvalidOperationException("Book on P" + pair.Item1 + " has no physical renderer/bounds.");
                var lightRoot = new GameObject("V07_ILLUMINATED_SPELLBOOK_LIGHT");
                lightRoot.transform.SetParent(shot, true);
                lightRoot.transform.position = candidate.bounds.center + Vector3.up * 0.55f;
                lightRoot.transform.rotation = Quaternion.identity;
                var lamp = lightRoot.AddComponent<Light>();
                lamp.type = LightType.Point;
                lamp.range = pair.Item1 == 26 ? 2.5f : 2.15f;
                lamp.intensity = 0.25f;
                lamp.shadows = LightShadows.None;
                lamp.color = pair.Item1 == 25 ? new Color(.76f,.89f,1f) :
                    pair.Item1 == 29 || pair.Item1 == 30 ? new Color(.68f,.84f,1f) :
                    new Color(1f,.87f,.64f);
                lamp.renderMode = LightRenderMode.ForcePixel;
                var animator = lightRoot.AddComponent<Animator>();
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.applyRootMotion = false;

                var assetPath = AssetsRoot + "P" + pair.Item1.ToString("00") + "_BookIllumination_v07.anim";
                if (AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath) != null)
                    throw new InvalidOperationException("Versioned light clip already exists: " + assetPath);
                var clip = new AnimationClip { name = "P" + pair.Item1.ToString("00") + "_BookIllumination_v07" };
                float high = pair.Item1 == 26 ? .83f : pair.Item1 == 25 ? .55f : .67f;
                AnimationUtility.SetEditorCurve(clip,
                    EditorCurveBinding.FloatCurve("", typeof(Light), "m_Intensity"),
                    new AnimationCurve(
                        new Keyframe(0f,.22f), new Keyframe(1.6f,high*.85f),
                        new Keyframe(4.2f,high), new Keyframe(6.2f,high*.92f),
                        new Keyframe(8f,.23f)));
                AssetDatabase.CreateAsset(clip,assetPath);
                var track = pair.Item3.CreateTrack<AnimationTrack>(null,
                    "V07 | Real 3D spellbook page illumination (no text overlays)");
                var timelineClip = track.CreateClip(clip);
                timelineClip.start = 0;
                timelineClip.duration = 8.0;
                timelineClip.easeInDuration = .3;
                timelineClip.easeOutDuration = .55;
                pair.Item2.SetGenericBinding(track,animator);
                EditorUtility.SetDirty(pair.Item3);
                EditorUtility.SetDirty(pair.Item2);
                Debug.Log("[PrologueV07] P" + pair.Item1 +
                    " scripted physical book light, color/intensity Timeline, no caption; anchor=" +
                    lightRoot.transform.position.ToString("F2") +
                    (pair.Item1 == 26 ? "; Nevali's locked book text untouched: " + expectedText.Replace("\n"," / ") : ""));
            }

            // Four real, looped rig clips ALREADY exist. Improve the source-approved
            // gait transition instead of double-animating the same Humanoid.
            foreach (var c in sourceWalks)
            {
                c.easeInDuration = Math.Min(.4,c.duration*.15);
                c.easeOutDuration = Math.Min(.45,c.duration*.15);
            }
            EditorUtility.SetDirty(walking);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene,V07))
                throw new IOException("Saving versioned v07 master scene failed.");
            Debug.Log("[PrologueV07] SAVED v07: Seven 3D book-illumination Timeline tracks + four real P15-18 looped walks eased. " +
                "P26 book message unmodified. v06 protected. Runtime route unchanged pending actual playback QA.");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[PrologueV07] Polishing incomplete; no claim of persisted QA. Inspect v07 only, never original v06.");
        }
        finally
        {
            if (current.IsValid() && current.isLoaded)
                SceneManager.SetActiveScene(current);
            if (scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene,true);
        }
    }

    private static string Hash(string path)
    {
        if (!File.Exists(path)) return "";
        using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "");
    }
}
#endif
