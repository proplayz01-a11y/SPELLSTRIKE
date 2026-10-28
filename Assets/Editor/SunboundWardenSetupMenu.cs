using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class SunboundWardenSetupMenu
{
    private const string ScenePath = "Assets/Scenes/Stage3Scene/Stage3Scene.unity";
    private const string ModelPath = "Assets/Stage 3/Enemies/STAGE 3 ENEMY NO. 1/SunboundWarden_Rig.fbx";
    private const string SwordPath = "Assets/Stage 3/Objects/STAGE_3_SWORD_READY.glb";
    private const string AnimationRoot = "Assets/Stage 3/Enemies/STAGE 3 ENEMY NO. 1/Animations";
    private const string AnimatorFolder = "Assets/Stage 3/Enemies/STAGE 3 ENEMY NO. 1/Animator";
    private const string ControllerPath = AnimatorFolder + "/SunboundWarden.controller";
    private const string AvatarPath = AnimatorFolder + "/SunboundWardenAvatar.asset";
    private const string PrefabPath = "Assets/Stage 3/Prefabs/SunboundWarden.prefab";
    // Verified against the Stage 3 baked NavMesh: this lower Runic Plaza floor
    // has 40m+ connected reach. The old Node 1 pedestal only had ~2.6m.
    private static readonly Vector3 Node1ArenaPosition = new Vector3(-63f, 0.737f, -85.33f);

    private static readonly string IdlePath = AnimationRoot + "/SunboundWarden_CombatIdle.fbx";
    private static readonly string RunPath = AnimationRoot + "/SunboundWarden_Run.fbx";
    private static readonly string SunSlashPath = AnimationRoot + "/SunboundWarden_SunSlash.fbx";
    private static readonly string RadiantRushPath = AnimationRoot + "/SunboundWarden_RadiantRush.fbx";
    private static readonly string SolarSlamPath = AnimationRoot + "/SunboundWarden_SolarSlam.fbx";
    private static readonly string HitPath = AnimationRoot + "/SunboundWarden_Hit.fbx";
    private static readonly string DeathPath = AnimationRoot + "/SunboundWarden_Death.fbx";

    [MenuItem("Tools/SPELLSTRIKE/Stage 3/Setup Sunbound Warden Node 1")]
    public static void SetupSunboundWarden()
    {
        EnsureFolders();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        ConfigureRigImporter(ModelPath);

        ConfigureAnimationImporter(IdlePath, true);
        ConfigureAnimationImporter(RunPath, true);
        ConfigureAnimationImporter(SunSlashPath, false);
        ConfigureAnimationImporter(RadiantRushPath, false);
        ConfigureAnimationImporter(SolarSlamPath, false);
        ConfigureAnimationImporter(HitPath, false);
        ConfigureAnimationImporter(DeathPath, false);

        AnimatorController animatorController = CreateAnimatorController();
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Transform node = FindSceneTransform(scene, "Node 1");
        Transform enemyGroup = FindSceneTransform(scene, "_Enemies_CHARACTERS");
        if (node == null)
            throw new InvalidOperationException("Stage 3 setup failed: 'Node 1' was not found.");
        if (enemyGroup == null)
            throw new InvalidOperationException("Stage 3 setup failed: '_Enemies_CHARACTERS' was not found.");

        node.position = Node1ArenaPosition;

        BoxCollider encounterTrigger = node.GetComponent<BoxCollider>();
        if (encounterTrigger == null)
            encounterTrigger = node.gameObject.AddComponent<BoxCollider>();
        encounterTrigger.isTrigger = true;
        encounterTrigger.center = new Vector3(0f, 1.5f, 0f);
        encounterTrigger.size = new Vector3(24f, 6f, 24f);

        GameObject warden = FindSceneGameObject(scene, "Sunbound Warden");
        GameObject visual = null;

        if (warden != null)
        {
            visual = FindVisualChild(warden.transform);
            if (visual == null || !IsInstanceFromAsset(visual, ModelPath))
            {
                if (visual != null)
                    UnityEngine.Object.DestroyImmediate(visual);

                visual = InstantiateVisual(ModelPath, scene);
                visual.transform.SetParent(warden.transform, false);
                visual.name = "SunboundWarden_Visual";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one;
            }
        }
        else
        {
            visual = FindExistingModelInstance(scene);
            if (visual == null)
            {
                visual = InstantiateVisual(ModelPath, scene);
            }

            warden = new GameObject("Sunbound Warden");
            SceneManager.MoveGameObjectToScene(warden, scene);
            warden.transform.SetParent(enemyGroup, false);

            Vector3 originalVisualScale = visual.transform.localScale;
            visual.transform.SetParent(warden.transform, true);
            visual.name = "SunboundWarden_Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = originalVisualScale;
        }

        warden.transform.SetParent(enemyGroup, true);
        warden.transform.position = node.position;
        warden.transform.rotation = node.rotation;
        warden.transform.localScale = Vector3.one;
        warden.tag = "Enemy";

        if (visual == null)
            visual = FindVisualChild(warden.transform);
        if (visual == null)
            throw new InvalidOperationException("Sunbound Warden visual child could not be resolved.");

        visual.name = "SunboundWarden_Visual";

        // If the non-destructive helmet-weight correction has been generated,
        // keep using it whenever the Warden setup is rebuilt.
        SunboundWardenHelmetWeightFixMenu.ApplyFixedMeshIfAvailable(visual);

        Animator animator = visual.GetComponent<Animator>();
        if (animator == null)
            animator = visual.AddComponent<Animator>();
        Avatar rigAvatar = CreateOrLoadGenericAvatar(visual);
        animator.avatar = rigAvatar;
        animator.runtimeAnimatorController = animatorController;
        animator.applyRootMotion = false;

        Rigidbody body = GetOrAdd<Rigidbody>(warden);
        body.isKinematic = true;
        body.useGravity = false;
        body.interpolation = RigidbodyInterpolation.None;

        CapsuleCollider capsule = GetOrAdd<CapsuleCollider>(warden);
        capsule.isTrigger = true;
        capsule.direction = 1;
        capsule.center = new Vector3(0f, 1f, 0f);
        capsule.height = 2f;
        capsule.radius = 0.44f;
        // Keep the Warden visible at Node 1, but not physically targetable until
        // StageNodeTrigger has actually started the encounter.
        capsule.enabled = false;

        NavMeshAgent agent = GetOrAdd<NavMeshAgent>(warden);
        agent.radius = 0.42f;
        agent.height = 2f;
        agent.baseOffset = 0f;
        agent.speed = 4.2f;
        agent.acceleration = 18f;
        agent.angularSpeed = 540f;
        agent.stoppingDistance = 2.15f;
        agent.autoBraking = true;
        agent.autoRepath = true;
        agent.updateRotation = false;
        // Stage 3 registers its baked NavMesh two Update frames after scene load.
        // Keep authored enemies disabled until that bootstrap can bind them cleanly;
        // this avoids Unity's native "no valid NavMesh" creation warning.
        agent.enabled = false;

        EnemyHealth health = GetOrAdd<EnemyHealth>(warden);
        health.maxHealth = 120f;

        SunboundWardenController controller = GetOrAdd<SunboundWardenController>(warden);
        controller.agent = agent;
        controller.enemyHealth = health;
        controller.player = FindPlayer(scene);
        controller.moveSpeed = 4.2f;
        controller.acceleration = 18f;
        controller.rotationSpeed = 540f;
        controller.stoppingDistance = 2.15f;
        controller.deathReceiver = node.GetComponent<StageNodeController>();

        Transform rightHand = FindRightHand(visual.transform);
        GameObject sword = AttachSword(rightHand);
        PoseRightHandFingers(rightHand);

        SunboundWardenWeaponGrip grip = GetOrAdd<SunboundWardenWeaponGrip>(warden);
        grip.Configure(rightHand);
        EditorUtility.SetDirty(grip);

        EditorUtility.SetDirty(warden);
        EditorUtility.SetDirty(visual);
        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(animator);
        EditorUtility.SetDirty(agent);
        EditorUtility.SetDirty(capsule);
        EditorUtility.SetDirty(health);
        EditorUtility.SetDirty(encounterTrigger);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(
            warden,
            PrefabPath,
            InteractionMode.AutomatedAction);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeGameObject = warden;
        EditorGUIUtility.PingObject(warden);

        string handName = rightHand != null ? rightHand.name : "NOT FOUND";
        string swordName = sword != null ? sword.name : "NOT ATTACHED";
        Debug.Log(
            $"[SunboundWardenSetup] COMPLETE | Node={node.position} | Hand={handName} | Sword={swordName} | " +
            $"Animator={animatorController.name} | Prefab={(prefab != null ? PrefabPath : "FAILED")}");
    }

    private static void EnsureFolders()
    {
        Directory.CreateDirectory(AnimatorFolder);
        Directory.CreateDirectory("Assets/Stage 3/Prefabs");
    }

    private static void ConfigureAnimationImporter(string path, bool loop)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[SunboundWardenSetup] No ModelImporter for '{path}'.");
            return;
        }

        importer.importAnimation = true;
        importer.animationType = ModelImporterAnimationType.Generic;

        ModelImporterClipAnimation[] clips = importer.clipAnimations;
        if (clips == null || clips.Length == 0)
            clips = importer.defaultClipAnimations;

        if (clips != null && clips.Length > 0)
        {
            foreach (ModelImporterClipAnimation clip in clips)
            {
                clip.loopTime = loop;
                clip.loopPose = loop;
            }
            importer.clipAnimations = clips;
        }

        importer.SaveAndReimport();
    }

    private static void ConfigureRigImporter(string path)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[SunboundWardenSetup] No ModelImporter for rig '{path}'.");
            return;
        }

        importer.importAnimation = false;
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.SaveAndReimport();
    }

    private static Avatar CreateOrLoadGenericAvatar(GameObject visual)
    {
        Avatar existing = AssetDatabase.LoadAssetAtPath<Avatar>(AvatarPath);
        if (existing != null && existing.isValid)
            return existing;

        if (existing != null)
            AssetDatabase.DeleteAsset(AvatarPath);

        Transform hips = visual.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => string.Equals(t.name, "mixamorig:Hips", StringComparison.OrdinalIgnoreCase));
        if (hips == null)
            throw new InvalidOperationException("Sunbound Warden rig has no mixamorig:Hips bone for Generic Avatar creation.");

        Avatar avatar = AvatarBuilder.BuildGenericAvatar(visual, hips.name);
        if (avatar == null || !avatar.isValid)
            throw new InvalidOperationException("Unity failed to build a valid Generic Avatar for Sunbound Warden.");

        avatar.name = "SunboundWardenAvatar";
        AssetDatabase.CreateAsset(avatar, AvatarPath);
        AssetDatabase.SaveAssets();
        return AssetDatabase.LoadAssetAtPath<Avatar>(AvatarPath);
    }

    private static AnimatorController CreateAnimatorController()
    {
        if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath) != null)
            AssetDatabase.DeleteAsset(ControllerPath);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("SunSlash", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("RadiantRush", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("SolarSlam", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("BeingHit", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        machine.name = "Sunbound Warden Base Layer";

        // Keep locomotion on the top row, attacks in one vertical column,
        // and reactions/death in their own column.  This makes the graph
        // readable at a glance instead of letting Unity stack everything.
        AnimatorState idle = AddState(machine, "Combat Idle", IdlePath, new Vector3(260f, 40f));
        AnimatorState run = AddState(machine, "Run", RunPath, new Vector3(520f, 40f));
        AnimatorState sunSlash = AddState(machine, "Sun Slash", SunSlashPath, new Vector3(520f, 180f));
        AnimatorState radiantRush = AddState(machine, "Radiant Rush", RadiantRushPath, new Vector3(520f, 300f));
        AnimatorState solarSlam = AddState(machine, "Solar Slam", SolarSlamPath, new Vector3(520f, 420f));
        AnimatorState hit = AddState(machine, "Being Hit", HitPath, new Vector3(260f, 300f));
        AnimatorState death = AddState(machine, "Death", DeathPath, new Vector3(260f, 440f));
        machine.defaultState = idle;
        machine.entryPosition = new Vector3(20f, 40f);
        machine.anyStatePosition = new Vector3(20f, 210f);
        machine.exitPosition = new Vector3(780f, 440f);

        AddConditionTransition(idle, run, AnimatorConditionMode.Greater, 0.12f, "Speed", 0.08f);
        AddConditionTransition(run, idle, AnimatorConditionMode.Less, 0.12f, "Speed", 0.08f);

        AddTriggerTransition(machine, sunSlash, "SunSlash");
        AddTriggerTransition(machine, radiantRush, "RadiantRush");
        AddTriggerTransition(machine, solarSlam, "SolarSlam");
        AddTriggerTransition(machine, hit, "BeingHit");
        AddTriggerTransition(machine, death, "Death", 0f);

        AddExitTransition(sunSlash, idle, 0.92f, 0.08f);
        AddExitTransition(radiantRush, idle, 0.92f, 0.08f);
        AddExitTransition(solarSlam, idle, 0.92f, 0.08f);
        AddExitTransition(hit, idle, 0.92f, 0.08f);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    private static AnimatorState AddState(AnimatorStateMachine machine, string name, string clipPath, Vector3 position)
    {
        AnimationClip clip = LoadAnimationClip(clipPath);
        if (clip == null)
            Debug.LogWarning($"[SunboundWardenSetup] Animation clip missing at '{clipPath}'. State '{name}' will be empty.");

        AnimatorState state = machine.AddState(name, position);
        state.motion = clip;
        return state;
    }

    private static AnimationClip LoadAnimationClip(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<AnimationClip>()
            .FirstOrDefault(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase));
    }

    private static void AddConditionTransition(
        AnimatorState from,
        AnimatorState to,
        AnimatorConditionMode mode,
        float threshold,
        string parameter,
        float duration)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = duration;
        transition.AddCondition(mode, threshold, parameter);
    }

    private static void AddTriggerTransition(
        AnimatorStateMachine machine,
        AnimatorState to,
        string parameter,
        float duration = 0.05f)
    {
        AnimatorStateTransition transition = machine.AddAnyStateTransition(to);
        transition.hasExitTime = false;
        transition.canTransitionToSelf = false;
        transition.duration = duration;
        transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
    }

    private static void AddExitTransition(AnimatorState from, AnimatorState to, float exitTime, float duration)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = true;
        transition.exitTime = exitTime;
        transition.duration = duration;
    }

    private static GameObject FindExistingModelInstance(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(transform.gameObject);
                if (source != null && AssetDatabase.GetAssetPath(source) == ModelPath)
                    return transform.gameObject;

                if (transform.name == "SunboundWarden_Rig")
                    return transform.gameObject;
            }
        }

        return null;
    }

    private static GameObject InstantiateVisual(string assetPath, Scene scene)
    {
        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (modelAsset == null)
            throw new InvalidOperationException($"Could not load Warden model at '{assetPath}'.");

        GameObject visual = PrefabUtility.InstantiatePrefab(modelAsset, scene) as GameObject;
        if (visual == null)
            throw new InvalidOperationException($"Failed to instantiate Warden visual '{assetPath}'.");

        return visual;
    }

    private static bool IsInstanceFromAsset(GameObject instance, string assetPath)
    {
        if (instance == null)
            return false;

        GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(instance);
        return source != null && AssetDatabase.GetAssetPath(source) == assetPath;
    }

    private static GameObject FindVisualChild(Transform warden)
    {
        foreach (Transform child in warden)
        {
            if (child.name == "SunboundWarden_Visual" || child.name == "base_basic_pbr")
                return child.gameObject;
        }

        return warden.GetComponentsInChildren<Animator>(true)
            .Select(a => a.gameObject)
            .FirstOrDefault();
    }

    private static Transform FindSceneTransform(Scene scene, string name)
    {
        GameObject found = FindSceneGameObject(scene, name);
        return found != null ? found.transform : null;
    }

    private static GameObject FindSceneGameObject(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform transform in transforms)
            {
                if (transform.name == name)
                    return transform.gameObject;
            }
        }

        return null;
    }

    private static Transform FindPlayer(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (transform.CompareTag("Player"))
                    return transform;
            }
        }

        return null;
    }

    private static Transform FindRightHand(Transform visualRoot)
    {
        Transform[] transforms = visualRoot.GetComponentsInChildren<Transform>(true);
        string[] preferred =
        {
            "mixamorig:righthand",
            "mixamorig1:righthand",
            "righthand",
            "right_hand",
            "hand_r",
            "r_hand"
        };

        foreach (string exact in preferred)
        {
            Transform match = transforms.FirstOrDefault(t =>
                string.Equals(t.name, exact, StringComparison.OrdinalIgnoreCase));
            if (match != null)
                return match;
        }

        return transforms.FirstOrDefault(t =>
        {
            string lower = t.name.ToLowerInvariant();
            return (lower.Contains("righthand") || lower.Contains("right_hand") || lower.Contains("hand_r")) &&
                   !IsFingerName(lower);
        });
    }

    private static GameObject AttachSword(Transform rightHand)
    {
        if (rightHand == null)
        {
            Debug.LogWarning("[SunboundWardenSetup] Right hand bone was not found. Sword was not attached.");
            return null;
        }

        Transform existing = rightHand.Find("SunboundWarden_Sword");
        if (existing != null)
            UnityEngine.Object.DestroyImmediate(existing.gameObject);

        GameObject swordAsset = AssetDatabase.LoadAssetAtPath<GameObject>(SwordPath);
        if (swordAsset == null)
        {
            Debug.LogWarning($"[SunboundWardenSetup] Sword asset missing at '{SwordPath}'.");
            return null;
        }

        GameObject sword = PrefabUtility.InstantiatePrefab(swordAsset) as GameObject;
        if (sword == null)
            return null;

        sword.name = "SunboundWarden_Sword";
        sword.transform.SetParent(rightHand, false);
        sword.transform.localPosition = Vector3.zero;
        sword.transform.localRotation = Quaternion.identity;
        sword.transform.localScale = Vector3.one;
        return sword;
    }

    private static void PoseRightHandFingers(Transform rightHand)
    {
        if (rightHand == null)
            return;

        Transform[] bones = rightHand.GetComponentsInChildren<Transform>(true);
        int posed = 0;

        foreach (Transform bone in bones)
        {
            if (bone == rightHand)
                continue;

            string lower = bone.name.ToLowerInvariant();
            if (!IsFingerName(lower))
                continue;

            float curl = lower.Contains("thumb") ? 22f : 34f;
            if (EndsWithFingerSegment(lower, "2")) curl += 12f;
            if (EndsWithFingerSegment(lower, "3")) curl += 18f;

            Transform sourceBone = PrefabUtility.GetCorrespondingObjectFromSource(bone);
            Quaternion baseRotation = sourceBone != null ? sourceBone.localRotation : bone.localRotation;
            bone.localRotation = baseRotation * Quaternion.Euler(0f, 0f, -curl);
            posed++;
            EditorUtility.SetDirty(bone);
        }

        Debug.Log($"[SunboundWardenSetup] Grip pose applied to {posed} right-hand finger bones.");
    }

    private static bool IsFingerName(string lowerName)
    {
        return lowerName.Contains("thumb") ||
               lowerName.Contains("index") ||
               lowerName.Contains("middle") ||
               lowerName.Contains("ring") ||
               lowerName.Contains("pinky") ||
               lowerName.Contains("little");
    }

    private static bool EndsWithFingerSegment(string lowerName, string segment)
    {
        return lowerName.EndsWith(segment, StringComparison.Ordinal) ||
               lowerName.EndsWith(segment + "_end", StringComparison.Ordinal) ||
               lowerName.EndsWith(segment + "end", StringComparison.Ordinal);
    }

    private static T GetOrAdd<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }
}
