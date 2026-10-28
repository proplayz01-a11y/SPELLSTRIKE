using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class StoneGolemPunchSetupMenu
{
    private const string Root = StoneGolemSetupMenu.BasePath;
    private const string PrefabPath = Root + "/Prefabs/S3N3_StoneGolem.prefab";
    private const string ControllerPath = Root + "/Animator/StoneGolem.controller";
    private const string ModelPath = Root + "/Model/StoneGolem_Rig.fbx";
    private const string SourceFolder = Root + "/Animations/MixamoSources/";
    private const string RightSource = "StoneGolem_ActualStone_Mixamo@Mutant Punch.fbx";
    private const string LeftSource = "StoneGolem_ActualStone_Mixamo@Mutant Punch (1).fbx";
    private const string RightClipPath = Root + "/Animations/MutantPunch_Right.anim";
    private const string LeftClipPath = Root + "/Animations/MutantPunch_Left.anim";
    private const string MaskPath = Root + "/Animator/StoneGolemUpperPunch.mask";
    private const string LayerName = "Upper Body Punch";

    [MenuItem("SPELLSTRIKE/Stone Golem/Add Mutant Punch R-L Combo %#F6")]
    public static void Apply()
    {
        Guard();
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            throw new InvalidOperationException("StoneGolem.controller is missing.");
        if (controller.layers.Any(l => l.name == LayerName))
            throw new InvalidOperationException("Upper Body Punch layer already exists; refusing to duplicate it.");

        Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().Single();
        if (!avatar.isValid || avatar.isHuman)
            throw new InvalidOperationException("Expected the existing valid Generic Stone Golem Avatar.");

        AnimationClip right = Import(RightSource, "MutantPunch_Right", RightClipPath, avatar,
            "OnRightPunchImpact", "OnRightPunchFinished");
        AnimationClip left = Import(LeftSource, "MutantPunch_Left", LeftClipPath, avatar,
            "OnLeftPunchImpact", "OnLeftPunchFinished");

        GameObject contents = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Transform visual = contents.transform.Find("ModelRig");
            if (visual == null)
                throw new InvalidOperationException("Production ModelRig is missing.");
            AvatarMask mask = CreateUpperMask(visual);
            AddLayer(controller, right, left, mask);

            StoneGolemController combat = contents.GetComponent<StoneGolemController>();
            SerializedObject serialized = new SerializedObject(combat);
            serialized.FindProperty("rightPunchClip").objectReferenceValue = right;
            serialized.FindProperty("leftPunchClip").objectReferenceValue = left;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }

        AssetDatabase.SaveAssets();
        Scene scene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Directory.CreateDirectory("Temp/S3N3Audit");
        File.WriteAllText("Temp/S3N3Audit/unity_punch_setup.txt",
            "Mutant Punch R/L layered setup saved.\n"
            + "Right=" + right.length.ToString("F6") + "s impact=" + (13f / 30f).ToString("F6") + "s\n"
            + "Left=" + left.length.ToString("F6") + "s impact=" + (13f / 30f).ToString("F6") + "s\n"
            + "Layer=" + LayerName + "; upper-body Generic mask; Base Layer/lower body unchanged; rootMotion=false.\n"
            + "Controller flow: Slam keeps priority; while Slam cools down, Stop may commit Right then chain Left only if player remains in range/front.");
        Debug.Log("[StoneGolemPunchSetup] Mutant Punch R->L upper-body combo saved to prefab/controller.");
    }

    [MenuItem("SPELLSTRIKE/Stone Golem/Audit Mutant Punch R-L Combo %#F5")]
    public static void Audit()
    {
        Guard();
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        AnimatorControllerLayer layer = controller.layers.Single(l => l.name == LayerName);
        AnimationClip right = AssetDatabase.LoadAssetAtPath<AnimationClip>(RightClipPath);
        AnimationClip left = AssetDatabase.LoadAssetAtPath<AnimationClip>(LeftClipPath);
        if (right == null || left == null || layer.avatarMask == null)
            throw new InvalidOperationException("Punch clips/mask are incomplete.");
        string[] rightEvents = AnimationUtility.GetAnimationEvents(right).Select(e => e.functionName + "@" + e.time.ToString("F4")).ToArray();
        string[] leftEvents = AnimationUtility.GetAnimationEvents(left).Select(e => e.functionName + "@" + e.time.ToString("F4")).ToArray();
        AnimatorStateMachine machine = layer.stateMachine;
        string[] states = machine.states.Select(s => s.state.name).OrderBy(x => x).ToArray();
        if (!states.Contains("Empty") || !states.Contains("Right Punch") || !states.Contains("Left Punch"))
            throw new InvalidOperationException("Punch layer states are incomplete.");

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        StoneGolemController combat = prefab.GetComponent<StoneGolemController>();
        SerializedObject serialized = new SerializedObject(combat);
        if (serialized.FindProperty("rightPunchClip").objectReferenceValue != right
            || serialized.FindProperty("leftPunchClip").objectReferenceValue != left)
            throw new InvalidOperationException("Punch clips are not wired on the production prefab.");

        Directory.CreateDirectory("Temp/S3N3Audit");
        string text = "PASS Mutant Punch structural audit\n"
            + "layers=" + string.Join(", ", controller.layers.Select(l => l.name)) + "\n"
            + "states=" + string.Join(", ", states) + "\n"
            + "rightEvents=" + string.Join(", ", rightEvents) + "\n"
            + "leftEvents=" + string.Join(", ", leftEvents) + "\n"
            + "mask=" + AssetDatabase.GetAssetPath(layer.avatarMask) + " transforms=" + layer.avatarMask.transformCount;
        File.WriteAllText("Temp/S3N3Audit/unity_punch_audit.txt", text);
        Debug.Log("[StoneGolemPunchAudit] " + text);
    }

    private static AnimationClip Import(string sourceFile, string clipName, string outputPath, Avatar avatar,
        string impactEvent, string finishEvent)
    {
        string destination = SourceFolder + sourceFile;
        if (!File.Exists(destination))
        {
            string source = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", sourceFile);
            if (!File.Exists(source))
                throw new FileNotFoundException("Downloaded punch source missing.", source);
            File.Copy(source, destination);
        }
        AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceSynchronousImport);
        ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(destination);
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
        importer.sourceAvatar = avatar;
        importer.motionNodeName = "";
        importer.importAnimation = true;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.animationCompression = ModelImporterAnimationCompression.Off;
        ModelImporterClipAnimation spec = importer.defaultClipAnimations.Single();
        spec.loopTime = spec.loopPose = false;
        spec.lockRootRotation = spec.lockRootPositionXZ = spec.lockRootHeightY = true;
        spec.keepOriginalOrientation = spec.keepOriginalPositionXZ = spec.keepOriginalPositionY = true;
        importer.clipAnimations = new[] { spec };
        importer.SaveAndReimport();

        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(outputPath) != null)
            throw new InvalidOperationException("Existing punch clip would be overwritten: " + outputPath);
        AnimationClip clip = Object.Instantiate(AssetDatabase.LoadAllAssetsAtPath(destination).OfType<AnimationClip>()
            .First(c => !c.name.StartsWith("__preview__")));
        clip.name = clipName;
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = settings.loopBlend = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip).Where(b => b.path == ""))
            AnimationUtility.SetEditorCurve(clip, binding, null);
        AnimationUtility.SetAnimationEvents(clip, new[]
        {
            new AnimationEvent { time = 13f / 30f, functionName = impactEvent },
            new AnimationEvent { time = clip.length - 1f / 30f, functionName = finishEvent }
        });
        AssetDatabase.CreateAsset(clip, outputPath);
        return clip;
    }

    private static AvatarMask CreateUpperMask(Transform visual)
    {
        if (AssetDatabase.LoadAssetAtPath<AvatarMask>(MaskPath) != null)
            throw new InvalidOperationException("Existing punch mask would be overwritten: " + MaskPath);
        AvatarMask mask = new AvatarMask { name = "StoneGolemUpperPunch" };
        for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
            mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);
        Transform[] transforms = visual.GetComponentsInChildren<Transform>(true);
        mask.transformCount = transforms.Length;
        for (int i = 0; i < transforms.Length; i++)
        {
            string bonePath = AnimationUtility.CalculateTransformPath(transforms[i], visual);
            mask.SetTransformPath(i, bonePath);
            mask.SetTransformActive(i, bonePath.Contains("mixamorig:Spine"));
        }
        AssetDatabase.CreateAsset(mask, MaskPath);
        return mask;
    }

    private static void AddLayer(AnimatorController controller, AnimationClip right, AnimationClip left, AvatarMask mask)
    {
        AnimatorStateMachine machine = new AnimatorStateMachine { name = LayerName };
        AssetDatabase.AddObjectToAsset(machine, controller);
        AnimatorState empty = machine.AddState("Empty", new Vector3(240, 40));
        empty.writeDefaultValues = false;
        AnimatorState r = machine.AddState("Right Punch", new Vector3(240, 140));
        r.motion = right;
        r.writeDefaultValues = false;
        AnimatorState l = machine.AddState("Left Punch", new Vector3(480, 140));
        l.motion = left;
        l.writeDefaultValues = false;
        machine.defaultState = empty;
        controller.AddLayer(new AnimatorControllerLayer
        {
            name = LayerName,
            avatarMask = mask,
            stateMachine = machine,
            blendingMode = AnimatorLayerBlendingMode.Override,
            defaultWeight = 0f,
            syncedLayerIndex = -1
        });
        EditorUtility.SetDirty(controller);
    }

    private static void Guard()
    {
        if (System.Diagnostics.Process.GetProcessesByName("VALORANT-Win64-Shipping").Length > 0)
            throw new InvalidOperationException("VALORANT is running; no authoring.");
        if (EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating
            || SceneManager.GetActiveScene().path != StoneGolemSetupMenu.ScenePath)
            throw new InvalidOperationException("Use the settled Stage3Scene Edit Mode.");
    }
}
