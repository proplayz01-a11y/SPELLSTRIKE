using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class StoneGolemDeathSetupMenu
{
    private const string Root = StoneGolemSetupMenu.BasePath;
    private const string ScenePath = StoneGolemSetupMenu.ScenePath;
    private const string PrefabPath = Root + "/Prefabs/S3N3_StoneGolem.prefab";
    private const string ControllerPath = Root + "/Animator/StoneGolem.controller";
    private const string ModelPath = Root + "/Model/StoneGolem_Rig.fbx";
    private const string SourceFolder = Root + "/Animations/MixamoSources/";
    private const string SourceFile = "StoneGolem_ActualStone_Mixamo@Zombie Death.fbx";
    private const string ClipPath = Root + "/Animations/ZombieDeath.anim";
    private const string StateName = "Death";

    [MenuItem("SPELLSTRIKE/Stone Golem/Install Zombie Death")]
    public static void Apply()
    {
        Guard();

        Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().Single();
        if (!avatar.isValid || avatar.isHuman)
            throw new InvalidOperationException("Expected the existing valid Generic Stone Golem Avatar.");

        AnimationClip deathClip = ImportDeathClip(avatar);
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            throw new InvalidOperationException("StoneGolem.controller is missing.");

        AnimatorStateMachine baseMachine = controller.layers[0].stateMachine;
        AnimatorState deathState = baseMachine.states.Select(s => s.state).FirstOrDefault(s => s.name == StateName);
        if (deathState == null)
        {
            deathState = baseMachine.AddState(StateName, new Vector3(520f, 260f));
            deathState.writeDefaultValues = false;
        }
        deathState.motion = deathClip;
        EditorUtility.SetDirty(deathState);
        EditorUtility.SetDirty(controller);

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            StoneGolemController combat = prefabRoot.GetComponent<StoneGolemController>();
            if (combat == null)
                throw new InvalidOperationException("StoneGolemController missing from production prefab.");

            SerializedObject serialized = new SerializedObject(combat);
            serialized.FindProperty("deathClip").objectReferenceValue = deathClip;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        Scene scene = SceneManager.GetActiveScene();
        GameObject node3 = scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(t => t.name == "Node 3")?.gameObject;
        if (node3 == null)
            throw new InvalidOperationException("Node 3 scene object is missing.");

        StageNodeController nodeController = node3.GetComponent<StageNodeController>();
        if (nodeController == null)
            throw new InvalidOperationException("Node 3 StageNodeController is missing.");

        SerializedObject nodeSerialized = new SerializedObject(nodeController);
        nodeSerialized.FindProperty("completeNodeOnEnemyDefeat").boolValue = true;
        nodeSerialized.ApplyModifiedPropertiesWithoutUndo();

        StoneGolemController sceneGolem = scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<StoneGolemController>(true))
            .Single(g => g.name == "S3N3_StoneGolem");
        SerializedObject sceneSerialized = new SerializedObject(sceneGolem);
        sceneSerialized.FindProperty("defeatReceiver").objectReferenceValue = nodeController;
        sceneSerialized.FindProperty("deathClip").objectReferenceValue = deathClip;
        sceneSerialized.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Directory.CreateDirectory("Temp/S3N3Audit");
        float eventTime = AnimationUtility.GetAnimationEvents(deathClip)
            .Single(e => e.functionName == "OnDeathFinished").time;
        File.WriteAllText("Temp/S3N3Audit/unity_death_setup.txt",
            "PASS Stone Golem Zombie Death setup\n"
            + $"source={SourceFile}\n"
            + $"clip={ClipPath} length={deathClip.length:F6}s event=OnDeathFinished@{eventTime:F6}s\n"
            + $"animatorState=Base Layer.{StateName}\n"
            + "rootMotion=false; root curves stripped; loop=false\n"
            + "Node3 completeNodeOnEnemyDefeat=true; defeatReceiver=Node 3\n"
            + "Corpse preserved after CompleteDeath; fallback remains active if event is missed.\n");

        Debug.Log("[StoneGolemDeathSetup] Zombie Death installed and wired to Node 3 progression.");
    }

    [MenuItem("SPELLSTRIKE/Stone Golem/Audit Zombie Death")]
    public static void Audit()
    {
        Guard();

        AnimationClip deathClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (deathClip == null || controller == null || prefab == null)
            throw new InvalidOperationException("Death setup assets are incomplete.");

        bool hasDeathState = controller.layers[0].stateMachine.states.Any(s => s.state.name == StateName && s.state.motion == deathClip);
        string[] events = AnimationUtility.GetAnimationEvents(deathClip)
            .Select(e => e.functionName + "@" + e.time.ToString("F6"))
            .ToArray();
        SerializedObject prefabCombat = new SerializedObject(prefab.GetComponent<StoneGolemController>());
        bool prefabClipWired = prefabCombat.FindProperty("deathClip").objectReferenceValue == deathClip;

        Scene scene = SceneManager.GetActiveScene();
        GameObject node3 = scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(t => t.name == "Node 3")?.gameObject;
        StageNodeController nodeController = node3 != null ? node3.GetComponent<StageNodeController>() : null;
        StoneGolemController sceneGolem = scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<StoneGolemController>(true))
            .SingleOrDefault(g => g.name == "S3N3_StoneGolem");

        if (!hasDeathState || !prefabClipWired || !events.Any(e => e.StartsWith("OnDeathFinished@"))
            || nodeController == null || sceneGolem == null)
            throw new InvalidOperationException("Zombie Death structural audit failed.");

        SerializedObject nodeSerialized = new SerializedObject(nodeController);
        SerializedObject sceneSerialized = new SerializedObject(sceneGolem);
        bool completesOnDefeat = nodeSerialized.FindProperty("completeNodeOnEnemyDefeat").boolValue;
        bool receiverWired = sceneSerialized.FindProperty("defeatReceiver").objectReferenceValue == nodeController;

        if (!completesOnDefeat || !receiverWired)
            throw new InvalidOperationException("Node 3 death progression wiring is incomplete.");

        Directory.CreateDirectory("Temp/S3N3Audit");
        string report = "PASS Stone Golem Zombie Death structural audit\n"
            + $"clipLength={deathClip.length:F6}s events={string.Join(", ", events)}\n"
            + $"deathState={hasDeathState} prefabClip={prefabClipWired}\n"
            + $"node3CompleteOnDefeat={completesOnDefeat} receiverWired={receiverWired}\n";
        File.WriteAllText("Temp/S3N3Audit/unity_death_audit.txt", report);
        Debug.Log("[StoneGolemDeathAudit] " + report);
    }

    private static AnimationClip ImportDeathClip(Avatar avatar)
    {
        string destination = SourceFolder + SourceFile;
        string source = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", SourceFile);
        if (!File.Exists(source))
            throw new FileNotFoundException("Downloaded Golem Zombie Death source is missing.", source);

        if (!File.Exists(destination))
            File.Copy(source, destination);

        AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceSynchronousImport);
        ModelImporter importer = AssetImporter.GetAtPath(destination) as ModelImporter;
        if (importer == null)
            throw new InvalidOperationException("Zombie Death source did not import as a model.");

        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
        importer.sourceAvatar = avatar;
        importer.motionNodeName = "";
        importer.importAnimation = true;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.animationCompression = ModelImporterAnimationCompression.Off;

        ModelImporterClipAnimation spec = importer.defaultClipAnimations.Single();
        spec.loopTime = false;
        spec.loopPose = false;
        spec.lockRootRotation = true;
        spec.lockRootPositionXZ = true;
        spec.lockRootHeightY = true;
        spec.keepOriginalOrientation = true;
        spec.keepOriginalPositionXZ = true;
        spec.keepOriginalPositionY = true;
        importer.clipAnimations = new[] { spec };
        importer.SaveAndReimport();

        AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
        if (existing != null)
            AssetDatabase.DeleteAsset(ClipPath);

        AnimationClip sourceClip = AssetDatabase.LoadAllAssetsAtPath(destination)
            .OfType<AnimationClip>()
            .First(c => !c.name.StartsWith("__preview__"));
        AnimationClip clip = Object.Instantiate(sourceClip);
        clip.name = "ZombieDeath";

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        settings.loopBlend = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip).Where(b => b.path == ""))
            AnimationUtility.SetEditorCurve(clip, binding, null);

        float eventTime = Mathf.Max(0f, clip.length - 1f / 30f);
        AnimationUtility.SetAnimationEvents(clip, new[]
        {
            new AnimationEvent { time = eventTime, functionName = "OnDeathFinished" }
        });

        AssetDatabase.CreateAsset(clip, ClipPath);
        return clip;
    }

    private static void Guard()
    {
        if (System.Diagnostics.Process.GetProcessesByName("VALORANT-Win64-Shipping").Length > 0)
            throw new InvalidOperationException("VALORANT is running; no authoring.");
        if (EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating
            || SceneManager.GetActiveScene().path != ScenePath)
            throw new InvalidOperationException("Use the settled Stage3Scene Edit Mode.");
    }
}
