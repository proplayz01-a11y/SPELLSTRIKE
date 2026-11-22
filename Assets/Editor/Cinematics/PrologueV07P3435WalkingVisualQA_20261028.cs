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

/// <summary>Samples saved, independently versioned P34/35 Timeline and renders their real shot cameras.</summary>
internal static class PrologueV07P3435WalkingVisualQA_20261028
{
    private const string ScenePath = "Assets/Scenes/Cinematics/Prologue_Master_Sequence_P01_42_v07_SpellbookWalkPolish.unity";
    private const string Original = "Assets/Scenes/Cinematics/Prologue_Master_Sequence_P01_42_v06_DialogueContinuity.unity";
    private const string SourceHash = "283573BAA9EB7C9E5D3CEF9290FFDDC3DF1F026B8C8D65EAF6D84A73CB26EBF4";
    private const string Report = "Library/PrologueV07_P3435_GaitVisualQA_20261028.txt";
    private const string Screenshots = "Assets/Cinematics/Storyboard3D/Timelines/PrologueMaster_v07_SpellbookWalkPolish/Validation/Screenshots";

    [MenuItem("SPELLSTRIKE/Cinematics/Prologue V07/06 Render P34-35 Walking Camera QA %#&h")]
    private static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            EditorApplication.isUpdating || SceneManager.sceneCount != 1 ||
            SceneManager.GetActiveScene().isDirty ||
            System.Diagnostics.Process.GetProcessesByName("VALORANT-Win64-Shipping").Length != 0)
        {
            Debug.LogError("[V07 GAIT VISUAL QA] Unsafe Editor state.");
            return;
        }
        if (Hash(Original) != SourceHash)
        {
            Debug.LogError("[V07 GAIT VISUAL QA] Protected v06 SHA mismatch.");
            return;
        }
        var active = SceneManager.GetActiveScene();
        Scene scene = default;
        var report = new StringBuilder();
        try
        {
            foreach (int p in new [] {34,35})
                foreach (string mark in new[] {"t01","t04","t06"})
                    if (File.Exists(System.IO.Path.Combine(Screenshots,"P"+p+"_Gait_"+mark+"_v07.png")))
                        throw new IOException("Screenshot slot occupied. Refusing overwrite P" + p + " " + mark);
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var shots = scene.GetRootGameObjects().Single(x =>
                x.name == "MASTER_SHOT_GROUPS__NUMBERED_IN_STORY_ORDER");
            report.AppendLine("SAVED SCENE " + scene.path + "; rootCount=" + scene.rootCount);
            foreach (int p in new[] {34,35})
            {
                var shot = shots.transform.Find("P" + p + "_SHOT_ROOT");
                var director = shot.GetComponentsInChildren<PlayableDirector>(true)
                    .Single(d => d.playableAsset is TimelineAsset);
                var timeline = (TimelineAsset)director.playableAsset;
                var track = timeline.GetOutputTracks().OfType<AnimationTrack>()
                    .Single(x => x.name.StartsWith("VanderPoel | Subtle head bob"));
                var clip = ((AnimationPlayableAsset)track.GetClips().Single().asset).clip;
                string expected = "Assets/Cinematics/Storyboard3D/Timelines/PrologueMaster_v07_SpellbookWalkPolish/P" +
                    p + "_Vander_Story65Bone_RealMixamoGait_v07.anim";
                if (AssetDatabase.GetAssetPath(clip) != expected)
                    throw new InvalidOperationException("P" + p + " gait is not in saved versioned Timeline: " +
                        AssetDatabase.GetAssetPath(clip));
                var actor = shot.GetComponentsInChildren<Animator>(true)
                    .Single(a => a.name == "P10_VanderPoel_ANIMATED");
                var skin = actor.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single();
                if (skin.sharedMesh.vertexCount != 19225 || skin.bones.Length != 65 || skin.bones.Any(b=>!b))
                    throw new InvalidOperationException("P" + p + " source story mesh/bones changed.");
                var arm = skin.bones.Single(b => b.name == "mixamorig:LeftArm");
                var thigh = skin.bones.Single(b => b.name == "mixamorig:LeftUpLeg");
                var opposite = skin.bones.Single(b => b.name == "mixamorig:RightUpLeg");
                var camera = shot.GetComponentsInChildren<Camera>(true)
                    .Single(c => c.name == "P" + p + "_CORRIDOR_STATIC_CAMERA");
                var clipPaths = AnimationUtility.GetCurveBindings(clip);
                var legCurves = clipPaths.Count(c => c.path.EndsWith("LeftUpLeg") ||
                    c.path.EndsWith("RightUpLeg") || c.path.EndsWith("LeftLeg") || c.path.EndsWith("RightLeg"));
                report.AppendLine("P" + p + " Timeline=" + AssetDatabase.GetAssetPath(timeline) +
                    " clip=" + expected + " curves=" + clipPaths.Length +
                    " selectedLegChannels=" + legCurves + " sourceMesh=" + skin.sharedMesh.name);
                shot.gameObject.SetActive(true);
                director.enabled = true;
                director.RebuildGraph();
                Quaternion thighFirst = Quaternion.identity, oppositeFirst = Quaternion.identity;
                float zFirst = 0;
                int count = 0;
                var times = new [] {1.25, 3.50, 6.25};
                var names = new [] {"t01","t04","t06"};
                for (int i=0;i<times.Length;i++)
                {
                    director.time = times[i];
                    director.Evaluate();
                    var qThigh=thigh.localRotation;
                    var qOpp=opposite.localRotation;
                    var z=skin.transform.position.z;
                    if (i==0){thighFirst=qThigh;oppositeFirst=qOpp;zFirst=z;}
                    report.AppendLine(" SAMPLE P" + p + " t="+times[i].ToString("F2") +
                        " leftThigh=" + qThigh.eulerAngles.ToString("F1") +
                        " rightThigh=" + qOpp.eulerAngles.ToString("F1") +
                        " arm=" + arm.localRotation.eulerAngles.ToString("F1") +
                        " movingRigZ=" + z.ToString("F3") +
                        " bounds=" + skin.bounds.size.ToString("F2"));
                    string output = System.IO.Path.Combine(Screenshots,
                        "P" + p + "_Gait_" + names[i] + "_v07.png");
                    Directory.CreateDirectory(Screenshots);
                    Capture(camera,output);
                    count++;
                }
                float leftChange = Quaternion.Angle(thighFirst,thigh.localRotation);
                float rightChange = Quaternion.Angle(oppositeFirst,opposite.localRotation);
                float zChange = Mathf.Abs(zFirst-skin.transform.position.z);
                report.AppendLine(" VALIDATE P" + p + " thighMotionDeg L=" + leftChange.ToString("F2") +
                    " R=" + rightChange.ToString("F2") + " corridorDeltaZ=" + zChange.ToString("F3") +
                    " cameraRenders=" + count);
                if (leftChange < 2f && rightChange < 2f)
                    throw new InvalidOperationException("P" + p + " story leg motion not visible in evaluated Timeline!");
                if (zChange < .3f)
                    throw new InvalidOperationException("P" + p + " world-space gait corridor path did not advance.");
                shot.gameObject.SetActive(false);
            }
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Report));
            File.WriteAllText(Report, report.ToString(), new UTF8Encoding(false));
            Debug.Log("[V07 GAIT VISUAL QA] PASS - 6 actual shot-camera PNGs, real animated leg bones and corridor translation. " + Report);
        }
        catch(Exception ex)
        {
            Debug.LogError("[V07 GAIT VISUAL QA] BLOCKED: " + ex +
                " -- no source scene or Timeline was saved.");
        }
        finally
        {
            if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
            if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
        }
    }
    private static void Capture(Camera camera,string file)
    {
        RenderTexture rt = null;
        Texture2D image = null;
        RenderTexture previous = RenderTexture.active;
        var original = camera.targetTexture;
        try
        {
            rt = new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            image = new Texture2D(1280,720,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1280,720),0,0);
            image.Apply();
            File.WriteAllBytes(file,image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = original;
            RenderTexture.active = previous;
            if (image) UnityEngine.Object.DestroyImmediate(image);
            if (rt) UnityEngine.Object.DestroyImmediate(rt);
        }
    }
    private static string Hash(string path)
    {
        if (!File.Exists(path)) return "";
        using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","");
    }
}
#endif
