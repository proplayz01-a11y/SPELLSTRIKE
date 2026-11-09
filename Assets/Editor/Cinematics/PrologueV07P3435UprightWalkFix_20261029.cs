#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

/// <summary>
/// One-time versioned P34/P35 correction. The old clip applied gait to a
/// seated story mesh behind a desk. Replace its *visible* actor for these
/// shots with the already approved P15 Vander Humanoid and an authentic
/// Mixamo walk, on a separate controlled corridor path.
/// Never modifies v06, other scenes or original dialogue/Timeline assets.
/// </summary>
internal static class PrologueV07P3435UprightWalkFix_20261029
{
    private const string Original = "Assets/Scenes/Cinematics/Prologue_Master_Sequence_P01_42_v06_DialogueContinuity.unity";
    private const string Backup = "RecoveryBackups/ProloguePolishV07_20261028_Immutable_v01/Prologue_Master_v06_BEFORE_POLISH.unity";
    private const string HashExpected = "283573BAA9EB7C9E5D3CEF9290FFDDC3DF1F026B8C8D65EAF6D84A73CB26EBF4";
    private const string ScenePath = "Assets/Scenes/Cinematics/Prologue_Master_Sequence_P01_42_v07_SpellbookWalkPolish.unity";
    private const string Folder = "Assets/Cinematics/Storyboard3D/Timelines/PrologueMaster_v07_SpellbookWalkPolish/";
    private const string WalkFbx = "Assets/Cinematics/Storyboard3D/Characters/VanderPoel/Animations/VanderPoel_Walking.fbx";
    private const string Report = "Library/PrologueV07_P3435_UprightWalkFix_20261029.txt";
    private const string ShotFolder = Folder + "Validation/Screenshots/";
    private const string TrackLabel = "V07 FIX | upright Vander true Mixamo walk";
    private const string PathTrackLabel = "V07 FIX | corridor position (independent root)";

    [MenuItem("SPELLSTRIKE/Cinematics/Prologue V07/07 Fix SEATED P34-35 with UPRIGHT Real Walk %#&j")]
    private static void Build()
    {
        if (Busy() || !GoodSource() || AssetDatabase.AssetPathToGUID(ScenePath) !=
            "12405e3bd39d43d398685e74678a8516")
        {
            Debug.LogError("[V07 UPRIGHT] Editor busy, game running, protected hash or v07 GUID mismatch.");
            return;
        }
        foreach (int p in new[] {34, 35})
        {
            if (File.Exists(CurvePath(p)) || File.Exists(CurvePath(p) + ".meta"))
            {
                Debug.LogError("[V07 UPRIGHT] Versioned walking path already exists; refusing overwrite: " + CurvePath(p));
                return;
            }
            foreach (string moment in new[] {"t01","t02","t04","t07"})
            {
                string picture = ShotFolder+"P"+p+"_UprightWalking_"+moment+"_20261029.png";
                if (File.Exists(picture))
                {
                    Debug.LogError("[V07 UPRIGHT] Screenshot evidence already exists; refusing overwrite: "+picture);
                    return;
                }
            }
        }
        var active = SceneManager.GetActiveScene();
        Scene scene = default;
        var report = new StringBuilder();
        try
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var group = scene.GetRootGameObjects().Single(g =>
                g.name == "MASTER_SHOT_GROUPS__NUMBERED_IN_STORY_ORDER").transform;
            var source = group.Find("P15_P18_SHOT_ROOT").GetComponentsInChildren<Animator>(true)
                .Single(a => a.name == "VanderPoel_VisualRig_Mixamo" &&
                    a.transform.parent.name == "VANDER_POEL_BLOCKING_RIG_VISUAL_ONLY");
            var walk = AssetDatabase.LoadAllAssetsAtPath(WalkFbx).OfType<AnimationClip>()
                .Single(c => c.name == "mixamo.com");
            if (!source.avatar || !source.avatar.isValid || !source.avatar.isHuman ||
                !walk.humanMotion || Mathf.Abs(walk.length - 1.566667f) > .15f)
                throw new InvalidOperationException("P15 standing actor or actual 1.5667s gait invalid.");
            foreach (int panel in new[] {34, 35})
            {
                var shot = group.Find("P" + panel + "_SHOT_ROOT");
                if (!shot || shot.Find("V07_UPRIGHT_VANDER_WALK_PATH") != null)
                    throw new InvalidOperationException("Missing shot or already-fixed P" + panel);
                var director = shot.GetComponentsInChildren<PlayableDirector>(true)
                    .Single(d => d.playableAsset is TimelineAsset);
                var timeline = (TimelineAsset)director.playableAsset;
                var expected = Folder + (panel == 34 ?
                    "P34_P34_DialogueAggressive_v05_v07.playable" :
                    "P35_P35_VanderReturnsWithStaff_8s_v01_v07.playable");
                if (AssetDatabase.GetAssetPath(timeline) != expected)
                    throw new InvalidOperationException("Panel not on versioned v07 Timeline: P" + panel);
                var old = shot.GetComponentsInChildren<Animator>(true)
                    .Single(a => a.name == "P10_VanderPoel_ANIMATED");
                var oldSkin = old.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .Single(s => s.sharedMesh && s.sharedMesh.vertexCount == 19225 && s.bones.Length == 65);
                var cam = shot.GetComponentsInChildren<Camera>(true)
                    .Single(c => c.name == "P" + panel + "_CORRIDOR_STATIC_CAMERA");
                Vector3 forward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
                if (forward.sqrMagnitude < .75f) throw new InvalidOperationException("Camera has invalid horizontal forward.");
                // Place the walk ON the central tiled aisle beyond the table,
                // where both feet will be unobstructed in the shot.
                var aisleCenter = cam.transform.position + forward * 6.5f;
                aisleCenter.y = oldSkin.bounds.min.y;
                Vector3 direction = panel == 34 ? forward : -forward;
                var path = new GameObject("V07_UPRIGHT_VANDER_WALK_PATH");
                path.transform.SetParent(shot, true);
                path.transform.position = aisleCenter;
                path.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
                var pathAnimator = path.AddComponent<Animator>();
                pathAnimator.applyRootMotion = false;
                pathAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var walker = UnityEngine.Object.Instantiate(source.gameObject, path.transform, false);
                walker.name = "V07_STANDING_VANDER_REAL_WALK_VISIBLE";
                walker.SetActive(true);
                walker.transform.localPosition = Vector3.zero;
                walker.transform.localRotation = Quaternion.identity;
                var rig = walker.GetComponent<Animator>();
                if (rig == null || !rig.avatar || !rig.avatar.isHuman || !rig.avatar.isValid)
                    throw new InvalidOperationException("New P" + panel + " actor not humanoid.");
                rig.runtimeAnimatorController = null;
                rig.applyRootMotion = false;
                rig.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                foreach (var renderer in walker.GetComponentsInChildren<Renderer>(true))
                    renderer.enabled = true;
                var body = walker.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .First(s => s.sharedMesh != null);
                if (body.bounds.size.y < .05f)
                    throw new InvalidOperationException("Source standing model bounds invalid: " + body.bounds);
                var factor = Mathf.Clamp(1.82f / body.bounds.size.y, .01f, 100f);
                walker.transform.localScale *= factor;
                walker.transform.position += Vector3.up * (aisleCenter.y - body.bounds.min.y);
                string staffEvidence = "not in this exit shot";
                if (panel == 35)
                    staffEvidence = AttachVisibleCanonicalStaff(group, rig);
                // Do not remove original story meshes/animations: only hide the
                // seated Vander visuals in these two independent shot instances.
                int hidden = 0;
                foreach (var renderer in old.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.enabled = false;
                    hidden++;
                }
                // A real looped humanoid gait animates every stride; the root
                // path is separately authored, so do NOT apply Mixamo root motion.
                var gaitTrack = timeline.CreateTrack<AnimationTrack>(null,TrackLabel);
                var gaitClip = gaitTrack.CreateClip<AnimationPlayableAsset>();
                gaitClip.displayName = "P" + panel + " real upright Vander walk, legs and feet";
                gaitClip.start = .2;
                gaitClip.duration = 7.45;
                gaitClip.easeInDuration = .2;
                gaitClip.easeOutDuration = .25;
                var motion = (AnimationPlayableAsset)gaitClip.asset;
                motion.clip = walk;
                motion.loop = AnimationPlayableAsset.LoopMode.On;
                motion.removeStartOffset = true;
                motion.applyFootIK = true;
                director.SetGenericBinding(gaitTrack, rig);

                var distance = 3.0f;
                var curve = new AnimationClip { name = "P" + panel + "_V07_Corridor3m_UprightStride" };
                float start = panel == 34 ? -distance*.5f : -distance*.5f;
                float end = distance*.5f;
                // The parent faces the direction of travel, so both panels
                // move forward in their actor's local space, never moonwalk.
                var keys = new[]
                {
                    new Keyframe(0f,start), new Keyframe(.2f,start),
                    new Keyframe(1.65f,start + .62f),
                    new Keyframe(3.15f,start + 1.27f),
                    new Keyframe(4.65f,start + 1.91f),
                    new Keyframe(6.15f,start + 2.57f),
                    new Keyframe(7.65f,end), new Keyframe(8f,end)
                };
                var z = shot.InverseTransformPoint(aisleCenter).z;
                for (int i=0;i<keys.Length;i++) keys[i].value += z;
                var curveBinding = EditorCurveBinding.FloatCurve("",
                    typeof(Transform),"m_LocalPosition.z");
                AnimationUtility.SetEditorCurve(curve,curveBinding,new AnimationCurve(keys));
                // Translating a rotated root in the shot's local Z would
                // break direction; use the *shot-local* forward vector and
                // write all three parent position channels instead.
                var rootBase = shot.InverseTransformPoint(aisleCenter);
                var shotDir = shot.InverseTransformDirection(direction);
                foreach (char axis in new[] {'x','y','z'})
                {
                    var samples = keys.Select(k => {
                        float travel = k.value-z;
                        var v = rootBase + shotDir * travel;
                        return new Keyframe(k.time, axis=='x'?v.x:axis=='y'?v.y:v.z);
                    }).ToArray();
                    AnimationUtility.SetEditorCurve(curve,
                        EditorCurveBinding.FloatCurve("",typeof(Transform),
                            "m_LocalPosition."+axis),new AnimationCurve(samples));
                }
                AssetDatabase.CreateAsset(curve,CurvePath(panel));
                var rootTrack = timeline.CreateTrack<AnimationTrack>(null,PathTrackLabel);
                var rootClip = rootTrack.CreateClip(curve);
                rootClip.start=0;
                rootClip.duration=8;
                director.SetGenericBinding(rootTrack,pathAnimator);
                path.transform.position = aisleCenter - direction * distance*.5f;
                // Frame full body from the SIDE of the aisle, not behind
                // the study table where the original seated actor is hidden.
                Vector3 cameraAt = aisleCenter +
                    cam.transform.right.normalized * 3.5f -
                    forward * .4f + Vector3.up * 1.38f;
                cam.transform.position = cameraAt;
                cam.transform.LookAt(aisleCenter + Vector3.up * 1.07f);
                cam.fieldOfView = 48f;
                cam.nearClipPlane = .08f;
                EditorUtility.SetDirty(timeline);
                EditorUtility.SetDirty(director);
                report.AppendLine("P"+panel+" source=P15 REAL 52-bone Mixamo humanoid; walkSeconds="+walk.length+
                    " newVisible="+walker.name+" legs enabled; bodyHeight="+body.bounds.size.y.ToString("F3")+
                    " oldSeatedRenderersHidden="+hidden+" routeMetres="+distance+
                    " camera="+cam.transform.position.ToString("F2")+" aisle="+aisleCenter.ToString("F2")+
                    " sourceAvatar="+rig.avatar.name+" geometry="+body.sharedMesh.name+
                    " staff="+staffEvidence);
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene,ScenePath))
                throw new IOException("Failed v07-only SaveScene.");
            // Actually render the *visible* standing character from the final
            // shot camera at four moments. Rotation numbers alone are not QA.
            Directory.CreateDirectory(ShotFolder);
            foreach (int panel in new[] {34,35})
            {
                var shot = group.Find("P"+panel+"_SHOT_ROOT");
                var director = shot.GetComponentsInChildren<PlayableDirector>(true)
                    .Single(d => d.playableAsset is TimelineAsset);
                var camera = shot.GetComponentsInChildren<Camera>(true)
                    .Single(c => c.name == "P"+panel+"_CORRIDOR_STATIC_CAMERA");
                var model = shot.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .Single(s => s.transform.IsChildOf(shot.Find("V07_UPRIGHT_VANDER_WALK_PATH")));
                shot.gameObject.SetActive(true);
                director.enabled = true;
                director.RebuildGraph();
                var seconds = new[] { .8, 2.35, 4.25, 6.75 };
                var moments = new[] {"t01","t02","t04","t07"};
                for(int i=0;i<seconds.Length;i++)
                {
                    director.time = seconds[i];
                    director.Evaluate();
                    var bounds = model.bounds;
                    var foot = camera.WorldToViewportPoint(new Vector3(bounds.center.x,bounds.min.y,bounds.center.z));
                    var head = camera.WorldToViewportPoint(new Vector3(bounds.center.x,bounds.max.y,bounds.center.z));
                    string name = ShotFolder+"P"+panel+"_UprightWalking_"+moments[i]+"_20261029.png";
                    Capture(camera,name);
                    report.AppendLine("P"+panel+" t="+seconds[i].ToString("F2")+
                        " footViewport="+foot.ToString("F3")+
                        " headViewport="+head.ToString("F3")+
                        " sourceWalkY="+bounds.size.y.ToString("F2")+
                        " render="+name);
                    if(foot.z < 0 || head.z < 0 || foot.y < .01f || head.y > .99f)
                        report.AppendLine(" REVIEW: feet/head beyond camera framing; screenshot NOT approved.");
                }
                shot.gameObject.SetActive(false);
            }
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Report));
            File.WriteAllText(Report,report.ToString(),new UTF8Encoding(false));
            Debug.Log("[V07 UPRIGHT] SAVED P34+35 standing Mixamo walker plus EIGHT real camera PNGs; INSPECT IMAGES before approval. "+Report);
        }
        catch(Exception ex) { Debug.LogException(ex); Debug.LogError("[V07 UPRIGHT] BLOCKED, inspect v07 only."); }
        finally
        {
            if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
            if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene,true);
        }
    }

    private static void Capture(Camera cam, string filename)
    {
        var previous = RenderTexture.active;
        var original = cam.targetTexture;
        RenderTexture rt = null;
        Texture2D pic = null;
        try
        {
            rt = new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            pic = new Texture2D(1280,720,TextureFormat.RGB24,false);
            pic.ReadPixels(new Rect(0,0,1280,720),0,0);
            pic.Apply();
            File.WriteAllBytes(filename,pic.EncodeToPNG());
        }
        finally
        {
            cam.targetTexture = original;
            RenderTexture.active = previous;
            if(pic) UnityEngine.Object.DestroyImmediate(pic);
            if(rt) UnityEngine.Object.DestroyImmediate(rt);
        }
    }

    private static string AttachVisibleCanonicalStaff(Transform group, Animator walkingActor)
    {
        var hand = walkingActor.GetComponentsInChildren<Transform>(true)
            .SingleOrDefault(t => t.name == "mixamorig:RightHand");
        if (!hand) throw new InvalidOperationException("P35 walker has no right-hand bone for staff.");
        var p38 = group.Find("P38_SHOT_ROOT");
        if (!p38) throw new InvalidOperationException("P38 canonical staff shot missing.");
        var candidates = p38.GetComponentsInChildren<Transform>(true)
            .Where(t => t.name == "P38_Van_CanonicalStaff_OnGround")
            .OrderByDescending(t => t.GetComponentsInChildren<Renderer>(true).Length)
            .ToArray();
        if (candidates.Length == 0)
            throw new InvalidOperationException("Canonical staff source not found in P38 shot.");
        var source = candidates[0];
        var sourceRenderers = source.GetComponentsInChildren<Renderer>(true);
        if (sourceRenderers.Length < 1 || sourceRenderers.Length > 32)
            throw new InvalidOperationException("Canonical staff prop unexpected renderer count " + sourceRenderers.Length);
        var replica = UnityEngine.Object.Instantiate(source.gameObject, hand, false);
        replica.name = "P35_V07_VANDER_CARRIES_CANONICAL_VAN_STAFF";
        replica.SetActive(true);
        foreach (var renderer in replica.GetComponentsInChildren<Renderer>(true))
            renderer.enabled = true;
        replica.transform.localPosition = new Vector3(.08f,-.15f,.04f);
        replica.transform.localRotation = Quaternion.Euler(4f,0f,-15f);
        var bounds = replica.GetComponentsInChildren<Renderer>(true).Select(r => r.bounds)
            .Aggregate((a,b) => {a.Encapsulate(b);return a;});
        if (bounds.size.magnitude < .001f)
            throw new InvalidOperationException("P35 canonical staff bounds invalid.");
        float size = Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));
        replica.transform.localScale *= Mathf.Clamp(1.55f / size,.02f,25f);
        return "P38 canonical source, "+sourceRenderers.Length+" renderer(s), held at right hand";
    }

    private static string CurvePath(int p) => Folder+"P"+p+"_V07_Corridor3m_UprightStride.anim";
    private static bool Busy() =>
        EditorApplication.isPlayingOrWillChangePlaymode ||
        EditorApplication.isCompiling || EditorApplication.isUpdating ||
        SceneManager.sceneCount != 1 || SceneManager.GetActiveScene().isDirty ||
        System.Diagnostics.Process.GetProcessesByName("VALORANT-Win64-Shipping").Length != 0;
    private static bool GoodSource() =>
        Hash(Original) == HashExpected && Hash(Backup) == HashExpected;
    private static string Hash(string f)
    {
        if (!File.Exists(f)) return "";
        using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(f))).Replace("-","");
    }
}
#endif
