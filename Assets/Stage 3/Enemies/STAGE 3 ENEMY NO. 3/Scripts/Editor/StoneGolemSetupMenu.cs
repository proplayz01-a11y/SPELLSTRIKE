using System;
using System.IO;
using System.Linq;
using System.Text;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class StoneGolemSetupMenu
{
    public const string BasePath = "Assets/Stage 3/Enemies/STAGE 3 ENEMY NO. 3";
    public const string ScenePath = "Assets/Scenes/Stage3Scene/Stage3Scene.unity";
    private const string ModelPath = BasePath + "/Model/StoneGolem_Rig.fbx";
    private const string ControllerPath = BasePath + "/Animator/StoneGolem.controller";
    private const string PrefabPath = BasePath + "/Prefabs/S3N3_StoneGolem.prefab";
    private const string SourceFolder = BasePath + "/Animations/MixamoSources/";
    private const string SourcePrefix = "StoneGolem_ActualStone_Mixamo@";

    [MenuItem("SPELLSTRIKE/Stone Golem/Audit Node 3 %&F9")]
    public static void Audit()
    {
        Guard();
        Scene scene = SceneManager.GetActiveScene();
        GameObject node = Find(scene, "Node 3");
        StageProgressionManager progression = scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<StageProgressionManager>(true)).Single();
        NavMeshSurface surface = scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<NavMeshSurface>(true))
            .First(s => s.navMeshData != null);
        var query = new NavMeshQueryFilter { agentTypeID = surface.agentTypeID, areaMask = NavMesh.AllAreas };
        if (node == null || node.GetComponent<StageNodeTrigger>() == null || node.GetComponent<StageNodeController>() == null)
            throw new InvalidOperationException("The authored Node 3 components are missing.");
        NavMeshTriangulation nav = NavMesh.CalculateTriangulation();
        if (!NavMesh.SamplePosition(node.transform.position, out NavMeshHit hit, 16f, query))
            throw new InvalidOperationException("The existing Stage 3 bake does not reach Node 3; no rebake was performed.");
        BoxCollider box = node.GetComponent<BoxCollider>();
        SerializedObject trigger = new SerializedObject(node.GetComponent<StageNodeTrigger>());
        StringBuilder log = new StringBuilder();
        log.AppendLine("Stone Golem Node 3 — live audit");
        log.AppendLine("scene=" + scene.path + " dirty=" + scene.isDirty);
        log.AppendLine("nodePosition=" + node.transform.position.ToString("F4") + " scale=" + node.transform.lossyScale);
        log.AppendLine("nodeIndex=" + trigger.FindProperty("nodeIndex").intValue);
        log.AppendLine("triggerProgression=" + trigger.FindProperty("progressionManager").objectReferenceValue);
        log.AppendLine("enemyRoot=" + trigger.FindProperty("enemyRoot").objectReferenceValue);
        log.AppendLine("battleReceiver=" + trigger.FindProperty("battleReceiver").objectReferenceValue);
        log.AppendLine("triggerBounds=" + box.bounds + " trigger=" + box.isTrigger);
        log.AppendLine("navSurface=" + surface.name + " asset=" + AssetDatabase.GetAssetPath(surface.navMeshData));
        log.AppendLine("navVertices=" + nav.vertices.Length + " navIndices=" + nav.indices.Length);
        log.AppendLine("nodeGround=" + hit.position.ToString("F4") + " triggerFloor=" + box.bounds.min.y.ToString("F4"));
        log.AppendLine("node3Complete=" + progression.IsNodeComplete(3) + " node4CanStart=" + progression.CanStartNode(4));
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.parent == null || t.name.StartsWith("_") || t.name.StartsWith("Node "))
                    log.AppendLine("hierarchy=" + t.name + " active=" + t.gameObject.activeSelf);
        Directory.CreateDirectory("Temp/S3N3Audit");
        File.WriteAllText("Temp/S3N3Audit/unity_node3_audit.txt", log.ToString());
        Debug.Log("[StoneGolemAudit] " + log);
    }

    [MenuItem("SPELLSTRIKE/Stone Golem/Apply Node 3 Production Setup %&F10")]
    public static void Apply()
    {
        Guard();
        Audit();
        Scene scene = SceneManager.GetActiveScene();
        GameObject node = Find(scene, "Node 3");
        if (Find(scene, "S3N3_StoneGolem") != null)
            throw new InvalidOperationException("The Golem is already present. This setup will not overwrite a tuned scene instance.");
        Avatar avatar = ConfigureModel();
        AnimationClip walk = CreateClip("Mutant Walking", "HeavyWalk", true, avatar);
        AnimationClip stop = CreateClip("Mutant Idle", "Stop_StationaryLoop", true, avatar);
        AnimationClip jump = CreateClip("Mutant Jumping", "GroundSlam_Jump", false, avatar);
        AnimationUtility.SetAnimationEvents(stop, new[] {
            new AnimationEvent { functionName = "OnStopSettled", time = 10f / 30f }
        });
        AnimationUtility.SetAnimationEvents(jump, new[] {
            new AnimationEvent { functionName = "OnGroundSlamImpact", time = 68f / 30f },
            new AnimationEvent { functionName = "OnGroundSlamFinished", time = jump.length - 1f / 30f }
        });
        AnimatorController controller = CreateAnimator(walk, stop, jump);
        NavMeshSurface surface = scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<NavMeshSurface>(true)).First(s => s.navMeshData != null);
        var query = new NavMeshQueryFilter { agentTypeID = surface.agentTypeID, areaMask = NavMesh.AllAreas };
        if (!NavMesh.SamplePosition(node.transform.position, out NavMeshHit ground, 16f, query))
            throw new InvalidOperationException("Node 3 ground sample unavailable.");
        Vector3 spawn = FindSpawn(node.transform, ground.position, query);
        GameObject golem = new GameObject("S3N3_StoneGolem");
        SceneManager.MoveGameObjectToScene(golem, scene);
        Undo.RegisterCreatedObjectUndo(golem, "Create Node 3 Stone Golem");
        GameObject parent = Find(scene, "_Enemies_CHARACTERS");
        if (parent != null)
            golem.transform.SetParent(parent.transform, false);
        golem.transform.SetPositionAndRotation(spawn, Quaternion.LookRotation(-node.transform.forward));
        GameObject visual = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath), scene) as GameObject;
        visual.name = "ModelRig";
        visual.transform.SetParent(golem.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;
        Bounds bounds = BoundsOf(visual);
        float scale = 3.2f / Mathf.Max(0.01f, bounds.size.y);
        visual.transform.localScale = Vector3.one * scale;
        // The evaluated stationary/jump clips plant at -0.0047m at source scale.
        // Rest-pose renderer bounds extend lower and must not lift the animated feet.
        visual.transform.localPosition = Vector3.up * (0.0047f * scale);
        ConvertMaterials(visual);
        Animator animator = visual.GetComponent<Animator>();
        if (animator == null) animator = visual.AddComponent<Animator>();
        animator.avatar = avatar;
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.updateMode = AnimatorUpdateMode.Normal;

        NavMeshAgent agent = golem.AddComponent<NavMeshAgent>();
        agent.enabled = false;
        agent.agentTypeID = surface.agentTypeID;
        agent.radius = 0.75f;
        agent.height = 3.2f;
        agent.baseOffset = 0f;
        agent.updateRotation = false;
        agent.updatePosition = true;
        agent.speed = 0.8697f * scale;
        agent.acceleration = agent.speed / 0.75f;
        agent.stoppingDistance = 2.5f;
        agent.autoBraking = true;
        CapsuleCollider capsule = golem.AddComponent<CapsuleCollider>();
        capsule.direction = 1;
        capsule.height = 3.05f;
        capsule.radius = 0.75f;
        capsule.center = new Vector3(0f, 1.55f, 0f);
        capsule.enabled = false;
        Rigidbody body = golem.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        EnemyHealth health = golem.AddComponent<EnemyHealth>();
        health.maxHealth = 200f;
        health.currentHealth = 200f;
        StoneGolemController combat = golem.AddComponent<StoneGolemController>();
        SetReference(combat, "animator", animator);
        SetReference(combat, "agent", agent);
        SetReference(combat, "combatCollider", capsule);
        SetReference(combat, "enemyHealth", health);
        SetFloat(combat, "walkSpeed", agent.speed);
        SetFloat(combat, "walkClipWorldSpeed", agent.speed);
        StoneGolemAnimationRelay relay = visual.AddComponent<StoneGolemAnimationRelay>();
        SetReference(relay, "controller", combat);

        PrefabUtility.SaveAsPrefabAssetAndConnect(golem, PrefabPath, InteractionMode.UserAction);
        StageNodeTrigger trigger = node.GetComponent<StageNodeTrigger>();
        Undo.RecordObject(trigger, "Wire Node 3 Golem");
        SetReference(trigger, "enemyRoot", golem);
        SetReference(trigger, "battleReceiver", combat);
        StageNodeController nodeController = node.GetComponent<StageNodeController>();
        Undo.RecordObject(nodeController, "Defer Node 3 death/progression");
        SetBool(nodeController, "completeNodeOnEnemyDefeat", false);
        // The original marker can float above the walkable terrain. Keep its transform
        // and 48m trigger footprint; align the trigger's bottom to the existing bake.
        BoxCollider box = node.GetComponent<BoxCollider>();
        if (box.bounds.min.y > ground.position.y + 0.5f || box.bounds.max.y < ground.position.y + 2f)
        {
            Undo.RecordObject(box, "Ground Node 3 trigger volume");
            Vector3 centerWorld = box.bounds.center;
            centerWorld.y = ground.position.y - 0.5f + box.bounds.size.y * 0.5f;
            box.center = node.transform.InverseTransformPoint(centerWorld);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = golem;
        StringBuilder result = new StringBuilder();
        result.AppendLine("Production Golem setup saved.");
        result.AppendLine("avatar=" + avatar.name + " valid=" + avatar.isValid + " human=" + avatar.isHuman);
        result.AppendLine("walkLength=" + walk.length + " stopLength=" + stop.length + " jumpLength=" + jump.length);
        result.AppendLine("stopSource=Mutant Idle; clean loop, not a one-shot braking clip");
        result.AppendLine("impactTime=" + (68f / 30f) + " finishTime=" + (jump.length - 1f / 30f));
        result.AppendLine("spawn=" + spawn.ToString("F4") + " modelScale=" + scale + " walkSpeed=" + agent.speed);
        result.AppendLine("triggerBounds=" + box.bounds + " rootMotion=false; NavMesh unchanged");
        result.AppendLine("death/progression has no callback; Node 3/4 flags were not changed");
        File.WriteAllText("Temp/S3N3Audit/unity_golem_setup.txt", result.ToString());
        Debug.Log("[StoneGolemSetup] " + result, golem);
    }

    [MenuItem("SPELLSTRIKE/Stone Golem/Align Authored Clip Grounding %&F8")]
    public static void AlignGrounding()
    {
        Guard();
        Scene scene = SceneManager.GetActiveScene();
        GameObject golem = Find(scene, "S3N3_StoneGolem");
        if (golem == null || PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(golem) != PrefabPath)
            throw new InvalidOperationException("Select the existing production scene/prefab integration first.");
        Transform visual = golem.transform.Find("ModelRig");
        if (visual == null) throw new InvalidOperationException("Production ModelRig is missing.");
        Undo.RecordObject(visual, "Align Golem animated feet");
        SerializedObject serialized = new SerializedObject(visual);
        SerializedProperty height = serialized.FindProperty("m_LocalPosition").FindPropertyRelative("y");
        height.floatValue = 0.0047f * visual.localScale.y;
        serialized.ApplyModifiedProperties();
        PrefabUtility.RecordPrefabInstancePropertyModifications(visual);
        // A nested model's root position is a default override; applying that
        // single property from the scene does not reliably save the outer prefab.
        GameObject contents = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Transform prefabVisual = contents.transform.Find("ModelRig");
            Vector3 position = prefabVisual.localPosition;
            position.y = height.floatValue;
            prefabVisual.localPosition = position;
            PrefabUtility.RecordPrefabInstancePropertyModifications(prefabVisual);
            PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(contents); }
        Transform savedVisual = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).transform.Find("ModelRig");
        if (Mathf.Abs(savedVisual.localPosition.y - visual.localPosition.y) > 0.00001f)
            throw new InvalidOperationException("Prefab ground alignment was not saved; scene was not saved.");
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene);
        Directory.CreateDirectory("Temp/S3N3Audit");
        File.WriteAllText("Temp/S3N3Audit/unity_grounding.txt",
            "ModelRig localY=" + visual.localPosition.y.ToString("F7")
            + "; corrected from animated mesh contact; agent root, clips/events and source model unchanged.");
        Debug.Log("[StoneGolemSetup] Authored clip floor alignment saved to scene and production prefab.", golem);
    }

    private static Avatar ConfigureModel()
    {
        ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = false;
        importer.motionNodeName = "";
        importer.SaveAndReimport();
        Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault();
        if (avatar == null || !avatar.isValid)
            throw new InvalidOperationException("The new Golem Generic Avatar is not valid.");
        return avatar;
    }

    private static AnimationClip CreateClip(string sourceName, string name, bool loop, Avatar avatar)
    {
        string path = SourceFolder + SourcePrefix + sourceName + ".fbx";
        ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(path);
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
        importer.sourceAvatar = avatar;
        importer.motionNodeName = "";
        importer.importAnimation = true;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.animationCompression = ModelImporterAnimationCompression.Off;
        ModelImporterClipAnimation spec = importer.defaultClipAnimations.Single();
        spec.loopTime = loop;
        spec.loopPose = loop;
        spec.lockRootRotation = true;
        spec.lockRootPositionXZ = true;
        spec.lockRootHeightY = true;
        spec.keepOriginalOrientation = true;
        spec.keepOriginalPositionXZ = true;
        spec.keepOriginalPositionY = true;
        importer.clipAnimations = new[] { spec };
        importer.SaveAndReimport();
        AnimationClip imported = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
            .First(c => !c.name.StartsWith("__preview__"));
        AnimationClip clip = Object.Instantiate(imported);
        clip.name = name;
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        settings.loopBlend = loop;
        settings.keepOriginalPositionY = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        string output = BasePath + "/Animations/" + name + ".anim";
        AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(output);
        if (existing == null) AssetDatabase.CreateAsset(clip, output);
        else
        {
            EditorUtility.CopySerialized(clip, existing);
            Object.DestroyImmediate(clip);
            clip = existing;
        }
        return clip;
    }

    private static AnimatorController CreateAnimator(AnimationClip walk, AnimationClip stop, AnimationClip jump)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
            throw new InvalidOperationException("A Golem Animator already exists; refusing to replace a tuned controller.");
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("WalkRate", AnimatorControllerParameterType.Float);
        AnimatorControllerParameter parameter = controller.parameters.Single();
        parameter.defaultFloat = 1f;
        controller.parameters = new[] { parameter };
        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState stationary = machine.AddState("Stop", new Vector3(250, 50));
        stationary.motion = stop;
        stationary.writeDefaultValues = false;
        AnimatorState moving = machine.AddState("Heavy Walk", new Vector3(250, 150));
        moving.motion = walk;
        moving.writeDefaultValues = false;
        moving.speedParameter = "WalkRate";
        moving.speedParameterActive = true;
        AnimatorState slam = machine.AddState("Ground Slam", new Vector3(550, 100));
        slam.motion = jump;
        slam.writeDefaultValues = false;
        machine.defaultState = stationary;
        // No auto exits/Any State transitions. The controller and exact clip events
        // own every transition, including returning through the stationary loop.
        return controller;
    }

    private static Vector3 FindSpawn(Transform node, Vector3 origin, NavMeshQueryFilter query)
    {
        foreach (Vector3 offset in new[] { node.forward * 9f, -node.forward * 9f,
            node.right * 9f, -node.right * 9f, node.forward * 6f })
        {
            if (!NavMesh.SamplePosition(origin + offset, out NavMeshHit hit, 2.5f, query))
                continue;
            NavMeshPath path = new NavMeshPath();
            if (NavMesh.CalculatePath(origin, hit.position, query, path) && path.status == NavMeshPathStatus.PathComplete
                && Vector3.Distance(origin, hit.position) > 4f)
                return hit.position;
        }
        throw new InvalidOperationException("No connected Golem spawn near Node 3. The existing bake was preserved.");
    }

    private static void ConvertMaterials(GameObject visual)
    {
        string folder = BasePath + "/Model/Materials";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(BasePath + "/Model", "Materials");
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) throw new InvalidOperationException("The existing URP Lit shader is unavailable.");
        foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                Material source = materials[i];
                string name = source != null ? source.name : "Stone_Default";
                string path = folder + "/" + name.Replace("/", "_") + ".mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(shader) { name = name };
                    Color color = source != null && source.HasProperty("_Color")
                        ? source.GetColor("_Color") : new Color(0.3f, 0.32f, 0.34f);
                    material.SetColor("_BaseColor", color);
                    material.SetFloat("_Smoothness", 0.18f);
                    AssetDatabase.CreateAsset(material, path);
                }
                materials[i] = material;
            }
            renderer.sharedMaterials = materials;
        }
    }

    private static Bounds BoundsOf(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    private static GameObject Find(Scene scene, string name)
    {
        return scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
            .Where(t => t.name == name).Select(t => t.gameObject).FirstOrDefault();
    }

    private static void Guard()
    {
        if (System.Diagnostics.Process.GetProcessesByName("VALORANT-Win64-Shipping").Length > 0)
            throw new InvalidOperationException("VALORANT is running. Golem tooling will not write or run.");
        if (EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating)
            throw new InvalidOperationException("Wait for Unity Edit Mode and imports to finish.");
        if (SceneManager.GetActiveScene().path != ScenePath)
            throw new InvalidOperationException("Open the existing Stage3Scene; no scene switch is performed.");
    }

    private static void SetReference(Object target, string propertyName, Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(propertyName).objectReferenceValue = value;
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }
    private static void SetFloat(Object target, string propertyName, float value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(propertyName).floatValue = value;
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }
    private static void SetBool(Object target, string propertyName, bool value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(propertyName).boolValue = value;
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }
}
