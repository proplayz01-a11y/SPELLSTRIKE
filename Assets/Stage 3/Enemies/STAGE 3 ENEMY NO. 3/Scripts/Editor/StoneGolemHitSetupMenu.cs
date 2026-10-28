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

public static class StoneGolemHitSetupMenu
{
    private const string Root = StoneGolemSetupMenu.BasePath;
    private const string PrefabPath = Root + "/Prefabs/S3N3_StoneGolem.prefab";
    private const string ControllerPath = Root + "/Animator/StoneGolem.controller";

    [MenuItem("SPELLSTRIKE/Stone Golem/Align Node 3 Spawn Surface %&F4")]
    public static void AlignSpawnSurface()
    {
        Guard();
        Scene scene=SceneManager.GetActiveScene();
        StoneGolemController golem=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<StoneGolemController>(true)).Single();
        Transform node=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="Node 3");
        var query=new UnityEngine.AI.NavMeshQueryFilter { agentTypeID=golem.GetComponent<UnityEngine.AI.NavMeshAgent>().agentTypeID,
            areaMask=UnityEngine.AI.NavMesh.AllAreas };
        if(!UnityEngine.AI.NavMesh.SamplePosition(node.position,out UnityEngine.AI.NavMeshHit arena,16f,query))
            throw new InvalidOperationException("Existing Node 3 arena NavMesh missing.");
        Vector3 before=golem.transform.position;
        Vector3 seed=new Vector3(before.x,arena.position.y,before.z);
        if(!UnityEngine.AI.NavMesh.SamplePosition(seed,out UnityEngine.AI.NavMeshHit spawn,0.5f,query)
            || Vector2.Distance(new Vector2(before.x,before.z),new Vector2(spawn.position.x,spawn.position.z))>.01f)
            throw new InvalidOperationException("No authored upper surface at the existing X/Z; no move or rebake.");
        UnityEngine.AI.NavMeshPath path=new UnityEngine.AI.NavMeshPath();
        if(!UnityEngine.AI.NavMesh.CalculatePath(arena.position,spawn.position,query,path)
            || path.status!=UnityEngine.AI.NavMeshPathStatus.PathComplete)
            throw new InvalidOperationException("Spawn is not connected to the Node 3 arena.");
        Undo.RecordObject(golem.transform,"Align Golem spawn to authored arena surface");
        golem.transform.position=new Vector3(before.x,spawn.position.y,before.z);
        PrefabUtility.RecordPrefabInstancePropertyModifications(golem.transform);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Directory.CreateDirectory("Temp/S3N3Audit");
        File.WriteAllText("Temp/S3N3Audit/unity_hit_spawn_alignment.txt",
            "before="+before.ToString("F5")+" after="+golem.transform.position.ToString("F5")
            +" scale="+golem.transform.lossyScale+"; X/Z/facing/size and existing bake preserved");
        Debug.Log("[StoneGolemHitSetup] Scene spawn height aligned to the existing upper Node 3 NavMesh.",golem);
    }

    [MenuItem("SPELLSTRIKE/Stone Golem/Add Layered Being Hit %&F7")]
    public static void Apply()
    {
        Guard();
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null || controller.layers.Length != 1)
            throw new InvalidOperationException("Expected the existing three-state Base Layer only; refusing to replace any tuned hit layers.");
        Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(Root + "/Model/StoneGolem_Rig.fbx").OfType<Avatar>().Single();
        if (!avatar.isValid || avatar.isHuman)
            throw new InvalidOperationException("Use the existing valid Generic production Avatar.");
        AnimationClip lower = Import("Step Backward", "BeingHit_StepBackward", avatar);
        AnimationClip upper = Import("Head Hit", "BeingHit_HeadHit", avatar);
        StringBuilder report = new StringBuilder();
        GameObject contents = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Transform visual = contents.transform.Find("ModelRig");
            Animator animator = visual.GetComponent<Animator>();
            GameObject sample = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Model/StoneGolem_Rig.fbx"));
            sample.hideFlags = HideFlags.HideAndDontSave;
            sample.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            sample.transform.localScale = Vector3.one;
            AnimationCurve progress;
            float distance;
            try
            {
                Transform hips = sample.GetComponentsInChildren<Transform>(true).Single(t => t.name == "mixamorig:Hips");
                string hipPath = AnimationUtility.CalculateTransformPath(hips, sample.transform);
                EditorCurveBinding travel = AnimationUtility.GetCurveBindings(lower)
                    .Where(b => b.path == hipPath && b.propertyName.StartsWith("m_LocalPosition."))
                    .OrderByDescending(b => Math.Abs(AnimationUtility.GetEditorCurve(lower,b).Evaluate(lower.length)
                        - AnimationUtility.GetEditorCurve(lower,b).Evaluate(0f))).First();
                AnimationCurve original = AnimationUtility.GetEditorCurve(lower, travel);
                float start = original.Evaluate(0f);
                float delta = original.Evaluate(lower.length) - start;
                lower.SampleAnimation(sample, 0f);
                Vector3 first = hips.position;
                lower.SampleAnimation(sample, lower.length);
                Vector3 last = hips.position;
                Vector3 horizontal = last - first;
                horizontal.y = 0f;
                distance = Vector3.Dot(horizontal, -sample.transform.forward) * visual.localScale.y;
                if (distance < 0.5f || distance > 1.5f || Mathf.Abs(delta) < 0.5f)
                    throw new InvalidOperationException("Unexpected Step Backward displacement: " + horizontal + " curve=" + travel.propertyName);
                Keyframe[] keys = original.keys.Select(k => new Keyframe(k.time / lower.length,
                    (k.value-start)/delta, k.inTangent*lower.length/delta, k.outTangent*lower.length/delta)).ToArray();
                progress = new AnimationCurve(keys);
                // Flatten only the audited backward translation. Keep authored hip
                // bob/rotation and leg articulation. Root travel now belongs to agent.Move.
                AnimationUtility.SetEditorCurve(lower, travel, AnimationCurve.Constant(0f, lower.length, start));
                lower.SampleAnimation(sample, 0f);
                first = hips.position;
                lower.SampleAnimation(sample, lower.length);
                last = hips.position;
                if (Vector2.Distance(new Vector2(first.x,first.z), new Vector2(last.x,last.z)) > 0.001f)
                    throw new InvalidOperationException("Residual horizontal Step displacement after extraction.");
                report.AppendLine("hipPath=" + hipPath + " extracted=" + travel.propertyName + " delta=" + delta);
                report.AppendLine("sourceBackwardMetres=" + horizontal.magnitude + " worldRetreatMetres=" + distance);
                report.AppendLine("flattenedEndpointXZ=" + Vector2.Distance(new Vector2(first.x,first.z),new Vector2(last.x,last.z)));
            }
            finally { Object.DestroyImmediate(sample); }
            AnimationUtility.SetAnimationEvents(lower, new[] { new AnimationEvent
                { time = lower.length - 0.0001f, functionName = "OnLowerBodyHitFinished" } });
            AnimationUtility.SetAnimationEvents(upper, new[] { new AnimationEvent
                { time = upper.length - 1f/30f, functionName = "OnUpperBodyHitFinished" } });
            AvatarMask lowerMask = CreateMask(visual, false, Root + "/Animator/StoneGolemLowerHit.mask", report);
            AvatarMask upperMask = CreateMask(visual, true, Root + "/Animator/StoneGolemUpperHit.mask", report);
            AddLayer(controller, StoneGolemHitReaction.LowerLayerName, "Step Backward", lower, lowerMask);
            AddLayer(controller, StoneGolemHitReaction.UpperLayerName, "Head Hit", upper, upperMask);
            StoneGolemHitReaction hit = contents.GetComponent<StoneGolemHitReaction>();
            if (hit == null) hit = contents.AddComponent<StoneGolemHitReaction>();
            SerializedObject serialized = new SerializedObject(hit);
            serialized.FindProperty("animator").objectReferenceValue = animator;
            serialized.FindProperty("agent").objectReferenceValue = contents.GetComponent<UnityEngine.AI.NavMeshAgent>();
            serialized.FindProperty("body").objectReferenceValue = contents.GetComponent<CapsuleCollider>();
            serialized.FindProperty("lowerClip").objectReferenceValue = lower;
            serialized.FindProperty("upperClip").objectReferenceValue = upper;
            serialized.FindProperty("retreatDistance").floatValue = distance;
            serialized.FindProperty("retreatProgress").animationCurveValue = progress;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            SerializedObject combat = new SerializedObject(contents.GetComponent<StoneGolemController>());
            combat.FindProperty("hitReaction").objectReferenceValue = hit;
            combat.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
            report.AppendLine("lengths=" + lower.length + "/" + upper.length + "; loops=false/false; avatar=" + avatar.name);
            report.AppendLine("rootMotion=false; Base Layer states/motions/events preserved");
        }
        finally { PrefabUtility.UnloadPrefabContents(contents); }
        AssetDatabase.SaveAssets();
        Scene scene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        GameObject live = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<StoneGolemController>(true))
            .Single().gameObject;
        if (live.GetComponent<StoneGolemHitReaction>() == null)
            throw new InvalidOperationException("Saved prefab component did not propagate to live instance.");
        Selection.activeGameObject = live;
        Directory.CreateDirectory("Temp/S3N3Audit");
        File.WriteAllText("Temp/S3N3Audit/unity_hit_setup.txt",report.ToString());
        Debug.Log("[StoneGolemHitSetup] " + report, live);
    }

    private static AnimationClip Import(string sourceName, string name, Avatar avatar)
    {
        string path = Root + "/Animations/MixamoSources/StoneGolem_ActualStone_Mixamo@" + sourceName + ".fbx";
        if (!File.Exists(path))
            File.Copy(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads", "StoneGolem_ActualStone_Mixamo@" + sourceName + ".fbx"), path);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(path);
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
        AnimationClip clip = Object.Instantiate(AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
            .First(c => !c.name.StartsWith("__preview__")));
        clip.name = name;
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = settings.loopBlend = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        // Generic transform masks must never animate the ModelRig/root object.
        foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip).Where(b => b.path == ""))
            AnimationUtility.SetEditorCurve(clip, binding, null);
        string output = Root + "/Animations/" + name + ".anim";
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(output) != null)
            throw new InvalidOperationException("Existing authored hit clip; refusing replacement: " + output);
        AssetDatabase.CreateAsset(clip, output);
        return clip;
    }

    private static AvatarMask CreateMask(Transform visual, bool upper, string path, StringBuilder report)
    {
        AvatarMask mask = new AvatarMask { name = upper ? "StoneGolemUpperHit" : "StoneGolemLowerHit" };
        for (int i=0; i<(int)AvatarMaskBodyPart.LastBodyPart; i++)
            mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);
        Transform[] transforms = visual.GetComponentsInChildren<Transform>(true);
        mask.transformCount = transforms.Length;
        for (int i=0; i<transforms.Length; i++)
        {
            string bonePath = AnimationUtility.CalculateTransformPath(transforms[i], visual);
            bool spine = bonePath.Contains("mixamorig:Spine");
            bool legs = bonePath.Contains("mixamorig:LeftUpLeg") || bonePath.Contains("mixamorig:RightUpLeg");
            bool hips = transforms[i].name == "mixamorig:Hips";
            bool include = upper ? spine : hips || legs;
            mask.SetTransformPath(i, bonePath);
            mask.SetTransformActive(i, include);
            if (include) report.AppendLine((upper ? "upper=" : "lower=") + bonePath);
        }
        AssetDatabase.CreateAsset(mask,path);
        return mask;
    }

    private static void AddLayer(AnimatorController controller, string name, string stateName, AnimationClip clip, AvatarMask mask)
    {
        AnimatorStateMachine machine = new AnimatorStateMachine { name=name };
        AssetDatabase.AddObjectToAsset(machine,controller);
        AnimatorState empty = machine.AddState("Empty", new Vector3(240,40));
        empty.writeDefaultValues = false;
        AnimatorState hit = machine.AddState(stateName, new Vector3(240,140));
        hit.motion = clip;
        hit.writeDefaultValues = false;
        machine.defaultState = empty;
        controller.AddLayer(new AnimatorControllerLayer { name=name, avatarMask=mask, stateMachine=machine,
            blendingMode=AnimatorLayerBlendingMode.Override, defaultWeight=0f, syncedLayerIndex=-1 });
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
