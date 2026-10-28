// Stage 4 boss encounter consultation prototype. Explicit visible Editor menu,
// additive reads of existing boss scenes, new versioned preview scene ONLY.
// Do not run while another collaborator is using the shared Editor.
#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class Stage4BossEncounterPreviewBuilder_20261027
{
    private const string Production = "Assets/Scenes/Stage4Scene/Stage4Scene.unity";
    private const string Preview = "Assets/Scenes/Stage4Scene/Stage4_BossEncounters_Preview_20261027_v02.unity";
    private const string Stage1 = "Assets/Scenes/SampleScene.unity";
    private const string Stage2 = "Assets/Scenes/Stage2Scene/Stage2Scene.unity";
    private const string Stage3 = "Assets/Scenes/Stage3Scene/Stage3Scene.unity";
    private const string VanderPrefab = "Assets/Cinematics/Storyboard3D/Characters/VanderPoel/Prefabs/VanderPoel_CinematicActor.prefab";
    private const string Backup = "Library/Stage4BossEncounters_20261027/Stage4Scene_before_boss_preview_v02.unity";

    [MenuItem("Stage 4/Convergence Citadel v003/BUILD Boss Encounters CONSULTATION PREVIEW 20261027")]
    public static void BuildConsultationPreview()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying)
        {
            Debug.LogError("[Stage4BossPreview] Exit Play Mode before creating a preview.");
            return;
        }
        if (EditorSceneManager.GetSceneManagerSetup().Any(x =>
        {
            Scene current = SceneManager.GetSceneByPath(x.path);
            return current.IsValid() && current.isLoaded && current.isDirty;
        }))
        {
            Debug.LogError("[Stage4BossPreview] A loaded scene has unsaved changes. Save it normally before building.");
            return;
        }
        if (!File.Exists(Production))
        {
            Debug.LogError("[Stage4BossPreview] Original production scene missing.");
            return;
        }
        // Every retry gets a brand new versioned scene and backup. Never reopen
        // the old v01 copy as a generation target, even if it matches production.
        if (File.Exists(Preview) || File.Exists(Backup))
        {
            Debug.LogError("[Stage4BossPreview] v02 preview or backup exists. Refusing overwrite.");
            return;
        }
        if (!File.Exists(Stage1) || !File.Exists(Stage2) || !File.Exists(Stage3) ||
            AssetDatabase.LoadAssetAtPath<GameObject>(VanderPrefab) == null)
        {
            Debug.LogError("[Stage4BossPreview] Original boss scene or existing Vander Poel prefab unavailable.");
            return;
        }

        // The working preview gets a fresh GUID from AssetDatabase.CopyAsset.
        // The production scene and its meta remain byte-for-byte unchanged.
        Directory.CreateDirectory(Path.GetDirectoryName(Backup));
        File.Copy(Production, Backup);
        File.Copy(Production + ".meta", Backup + ".meta");
        if (!AssetDatabase.CopyAsset(Production, Preview))
        {
            Debug.LogError("[Stage4BossPreview] CopyAsset failed; original backup remains at " + Backup);
            return;
        }

        Scene originalActive = SceneManager.GetActiveScene();
        Scene previewScene = default;
        bool saved = false;
        try
        {
            previewScene = EditorSceneManager.OpenScene(Preview, OpenSceneMode.Additive);
            var map = RequireUnique<Transform>(previewScene, "ConvergenceCitadel_v003_Visual_LOD0");
            var protectedRoot = RequireUnique<Transform>(previewScene, "Stage4_ConvergenceCitadel_PRODUCTION");
            var physicsRoot = protectedRoot.GetComponentsInChildren<Transform>(true)
                .Single(t => t.name == "Independent_StaticMeshColliders");
            ValidateProtectedMap(previewScene, protectedRoot, physicsRoot, "before installation");
            var progression = RequireUnique<StageProgressionManager>(previewScene, "Stage4ProgressionManager");
            var player = RequireSingleComponentInScene<PlayerHealth>(previewScene);
            GameObject vanderVisual = AssetDatabase.LoadAssetAtPath<GameObject>(VanderPrefab);

            // Markers live in the LOD0 GLB; don't accidentally take a same-named LOD1 marker.
            Transform n1Spawn = Marker(map, "Node1_BossSpawn");
            Transform n2Spawn = Marker(map, "Node2_BossSpawn");
            Transform n3Spawn = Marker(map, "Node3_BossSpawn");
            Transform n4Spawn = Marker(map, "Boss_BossSpawn");
            Transform n1Center = Marker(map, "Node1_ArenaCenter");
            Transform n2Center = Marker(map, "Node2_ArenaCenter");
            Transform n3Center = Marker(map, "Node3_ArenaCenter");
            Transform n4Center = Marker(map, "Boss_ArenaCenter");

            // Sanity-check imported coordinates to detect scene-art movement or X-axis mismatch.
            CheckMarker(n1Spawn, new Vector3(-276f, 6f, -166.5f));
            CheckMarker(n2Spawn, new Vector3(-624f, 12f, -343.5f));
            CheckMarker(n3Spawn, new Vector3(-1280f, 18f, -620f));
            CheckMarker(n4Spawn, new Vector3(-2020f, 30f, -1040f));

            GameObject root = MakeRoot(previewScene, "_Stage4BossEncounters_20261027_PREVIEW");
            Transform actors = MakeChild(root.transform, "Actors").transform;
            Transform nodes = MakeChild(root.transform, "NodeTriggersAndProgression").transform;
            Transform gates = MakeChild(root.transform, "LockedBridgeGates").transform;

            // Copy complete actual source actors, including authentic model/Animator/weapon hierarchy.
            // Only the copies are rebound. No original boss scene or prefab is saved.
            GameObject knight = CloneFromSource<HollowKnightController>(
                Stage1, "Hollow Knight", previewScene, actors, n1Spawn, n1Center, "STAGE4_Node1_HollowKnight");
            GameObject captain = CloneFromSource<DrownedCaptainController>(
                Stage2, "DrownedCaptain", previewScene, actors, n2Spawn, n2Center, "STAGE4_Node2_DrownedCaptain");
            GameObject titan = CloneFromSource<TitanEarthbreakerController>(
                Stage3, "S3N4_TitanV017_ProductionRig", previewScene, actors, n3Spawn, n3Center,
                "STAGE4_Node3_TitanEarthbreaker");
            SanitizeCopiedBoss(knight);
            SanitizeCopiedBoss(captain);
            SanitizeCopiedBoss(titan);

            GameObject vander = PrefabUtility.InstantiatePrefab(vanderVisual, previewScene) as GameObject;
            if (vander == null) throw new InvalidOperationException("Could not instantiate existing Vander Poel cinematic actor.");
            vander.name = "STAGE4_Final_VanderPoel_PREVIEW_AI";
            vander.transform.SetParent(actors, false);
            vander.transform.SetPositionAndRotation(n4Spawn.position, FaceCenter(n4Spawn, n4Center));
            vander.tag = "Enemy";
            SizeVanderVisualForArena(vander, 24f);
            var vanderHealth = vander.AddComponent<EnemyHealth>();
            vanderHealth.maxHealth = 400f;
            SetProperty(vanderHealth, "hudDisplayRole", 2);
            var visualBody = vander.AddComponent<CapsuleCollider>();
            float worldScale = Mathf.Max(0.0001f, vander.transform.lossyScale.y);
            visualBody.height = 24f / worldScale;
            visualBody.radius = 5f / worldScale;
            visualBody.center = new Vector3(0f, visualBody.height * 0.5f, 0f);
            var vanderNav = vander.AddComponent<NavMeshAgent>();
            vanderNav.radius = 5f;
            vanderNav.height = 24f;
            vanderNav.speed = 8f;
            vanderNav.angularSpeed = 105f;
            var vanderDemo = vander.AddComponent<Stage4VanderPoelPreviewEncounterV01>();
            vanderDemo.Configure(player.transform, vanderNav);

            var knightAI = knight.GetComponent<HollowKnightController>();
            knightAI.player = player.transform;
            knightAI.agent = knight.GetComponent<NavMeshAgent>();
            knightAI.animator = knight.GetComponentInChildren<Animator>(true);
            knightAI.stageBossController = null; // Stage1-specific rewards are not Stage4 rewards.
            if (knightAI.agent == null || knightAI.animator == null)
                throw new InvalidOperationException("Hollow Knight copy lost its Animator/NavMeshAgent.");

            var captainAI = captain.GetComponent<DrownedCaptainController>();
            if (captainAI == null || captain.GetComponent<NavMeshAgent>() == null)
                throw new InvalidOperationException("Drowned Captain copy lost controller/NavMeshAgent.");
            SetProperty(captainAI, "player", player.transform);
            SetProperty(captainAI, "tileSpawnArenaCenter", n2Center);
            SetProperty(captainAI, "tileSpawnArenaRadius", 85f);
            SetProperty(captainAI, "tileSpawnSurfaceLayers", 1);
            SetProperty(captainAI, "deathReceiver", (UnityEngine.Object)null);
            SetProperty(captainAI, "rootToDisableOnDeath", captain);

            var titanAI = titan.GetComponent<TitanEarthbreakerController>();
            if (titanAI == null || titan.GetComponentInChildren<EnemyHealth>(true) == null)
                throw new InvalidOperationException("Titan copy lost its V017 combat controller/health.");
            SetProperty(titanAI, "player", player);
            SetProperty(titanAI, "arenaCenter", n3Center);
            SetProperty(titanAI, "defeatReceiver", (UnityEngine.Object)null);
            // Retain the source Titan V017 damage enable flags as authored. This is
            // explicitly NOT full-damage system acceptance for the consultation.

            StageNodeController n1 = MakeEncounterNode(nodes, 1, "HollowKnight", progression,
                knight, knightAI, Marker(map, "Node1_Entry"), n1Center, 90f, "StartBattle", 3.5f);
            StageNodeController n2 = MakeEncounterNode(nodes, 2, "DrownedCaptain", progression,
                captain, captainAI, Marker(map, "Node2_Entry"), n2Center, 85f, "StartBattle", 0f);
            StageNodeController n3 = MakeEncounterNode(nodes, 3, "TitanEarthbreaker", progression,
                titan, titanAI, Marker(map, "Node3_Entry"), n3Center, 255f, "StartAwakening", 0f);
            StageNodeController n4 = MakeEncounterNode(nodes, 4, "VanderPoel_PREVIEW", progression,
                vander, vanderDemo, Marker(map, "Boss_Entry"), n4Center, 145f, "StartBattle", 1.5f);
            SetProperty(captainAI, "deathReceiver", n2);
            SetProperty(titanAI, "defeatReceiver", n3);

            GameObject gate02 = MakeGate(gates, map, "Bridge_Gate_02", "Bridge_02_Entry", "Bridge_02_Exit");
            GameObject gate03 = MakeGate(gates, map, "Bridge_Gate_03", "Bridge_03_Entry", "Bridge_03_Exit");
            GameObject gate04 = MakeGate(gates, map, "Bridge_Gate_04", "Bridge_04_Entry", "Bridge_04_Exit");
            var gateBinder = root.AddComponent<Stage4BridgeGateBinderV01>();
            gateBinder.Configure(progression, gate02, gate03, gate04);

            // No duplicate player/HUD/camera, no map LOD/collision mutation.
            ValidateProtectedMap(previewScene, protectedRoot, physicsRoot, "after installation");
            if (previewScene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PlayerHealth>(true)).Count() != 1 ||
                previewScene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<StageProgressionManager>(true)).Count() != 1)
                throw new InvalidOperationException("Protected Stage4 count changed after boss installation.");
            EditorSceneManager.MarkSceneDirty(previewScene);
            if (!EditorSceneManager.SaveScene(previewScene, Preview))
                throw new IOException("Failed to save Stage4 boss preview.");
            saved = true;
            Debug.Log("[Stage4BossPreview] Saved " + Preview +
                      ": 4 actual character actors, 4 triggers, 4 nodes, 3 locked bridge gates. " +
                      "Hollow/Captain/Titan reuse real stage combat controllers. Vander Poel AI is a " +
                      "consultation prototype. Test Play Mode, combat damage, NavMesh, and death gates before promotion.");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[Stage4BossPreview] Setup incomplete. Production scene untouched. Preview saved=" + saved);
        }
        finally
        {
            if (originalActive.IsValid() && originalActive.isLoaded)
                SceneManager.SetActiveScene(originalActive);
            if (previewScene.IsValid() && previewScene.isLoaded)
                EditorSceneManager.CloseScene(previewScene, true);
        }
    }

    private static GameObject CloneFromSource<T>(
        string sourcePath, string expectedName, Scene destination, Transform parent,
        Transform spawn, Transform face, string name) where T : Component
    {
        Scene source = SceneManager.GetSceneByPath(sourcePath);
        bool wasLoaded = source.IsValid() && source.isLoaded;
        if (!wasLoaded)
            source = EditorSceneManager.OpenScene(sourcePath, OpenSceneMode.Additive);
        try
        {
            T[] matches = source.GetRootGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<T>(true))
                .Where(c => c.gameObject.name == expectedName)
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(sourcePath + ": expected one " + expectedName +
                                                    " with " + typeof(T).Name + "; found " + matches.Length);
            GameObject actual = matches[0].gameObject;
            Vector3 originalScale = actual.transform.lossyScale;
            // Parent lives in destination preview Scene, so Instantiate creates
            // the copy there directly. It does not dirty the source gameplay scene.
            GameObject instance = UnityEngine.Object.Instantiate(actual, parent, true);
            if (instance.scene != destination)
                throw new InvalidOperationException("Boss clone was created outside Stage4 preview. Refusing scene save.");
            instance.name = name;
            instance.transform.localScale = originalScale;
            instance.transform.SetPositionAndRotation(spawn.position, FaceCenter(spawn, face));
            instance.SetActive(true);
            return instance;
        }
        finally
        {
            if (!wasLoaded && source.IsValid() && source.isLoaded)
                EditorSceneManager.CloseScene(source, true);
        }
    }

    private static StageNodeController MakeEncounterNode(
        Transform parent, int number, string name, StageProgressionManager progression,
        GameObject actor, MonoBehaviour receiver, Transform entry, Transform arenaCenter,
        float radius, string startMessage, float deathDelay)
    {
        GameObject nodeObject = MakeChild(parent, "Stage4_Node" + number + "_" + name);
        StageNodeController node = nodeObject.AddComponent<StageNodeController>();
        SetProperty(node, "progressionManager", progression);
        SetProperty(node, "nodeIndex", number);
        SetProperty(node, "completeNodeOnEnemyDefeat", true);
        SetProperty(node, "fragmentGoalIncrement", 0);
        SetProperty(node, "lockFragmentAtStart", false);

        GameObject triggerObject = MakeChild(parent, "Stage4_Node" + number + "_EnterTrigger");
        triggerObject.transform.SetPositionAndRotation(entry.position, FaceCenter(entry, arenaCenter));
        var collider = triggerObject.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = new Vector3(52f, 35f, 30f);
        collider.center = new Vector3(0f, 12f, 0f);
        StageNodeTrigger trigger = triggerObject.AddComponent<StageNodeTrigger>();
        SetProperty(trigger, "progressionManager", progression);
        SetProperty(trigger, "nodeIndex", number);
        SetProperty(trigger, "enemyRoot", actor);
        SetProperty(trigger, "battleReceiver", receiver);
        SetProperty(trigger, "startBattleMessage", startMessage);
        SetProperty(trigger, "sendStartBattleMessage", true);
        SetProperty(trigger, "configureTileSpawner", true);
        SetProperty(trigger, "arenaCenter", arenaCenter);
        SetProperty(trigger, "arenaRadius", radius);
        SetProperty(trigger, "requireVocabularyBeforeCombat", false);
        SetProperty(trigger, "configureStage3VocabularyTiles", false);

        EnemyHealth hp = actor.GetComponentInChildren<EnemyHealth>(true);
        if (hp == null)
            throw new InvalidOperationException("No EnemyHealth on " + actor.name);
        if (deathDelay > 0f)
            nodeObject.AddComponent<Stage4BossDefeatRelayV01>().Configure(hp, node, deathDelay);
        return node;
    }

    private static GameObject MakeGate(Transform parent, Transform map,
        string gateMarker, string entryMarker, string exitMarker)
    {
        Transform at = Marker(map, gateMarker);
        Transform entry = Marker(map, entryMarker);
        Transform exit = Marker(map, exitMarker);
        GameObject gate = MakeChild(parent, gateMarker + "_LOCK_COLLIDER");
        gate.transform.SetPositionAndRotation(at.position, FaceCenter(entry, exit));
        BoxCollider box = gate.AddComponent<BoxCollider>();
        box.isTrigger = false;
        box.size = new Vector3(34f, 34f, 4f);
        box.center = new Vector3(0f, 15f, 0f);
        return gate;
    }

    private static void SizeVanderVisualForArena(GameObject actor, float desiredWorldHeight)
    {
        Renderer[] renderers = actor.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            throw new InvalidOperationException("Existing Vander Poel visual has no renderers.");
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        if (bounds.size.y < 0.001f)
            throw new InvalidOperationException("Vander Poel render bounds have zero height.");
        float factor = desiredWorldHeight / bounds.size.y;
        actor.transform.localScale *= Mathf.Clamp(factor, 0.001f, 1000f);
    }

    private static void SanitizeCopiedBoss(GameObject actor)
    {
        // Original Stage1-3 encounters have scene-owned word-puzzle managers.
        // Stage4 uses its own attack/tile pipeline, so disabled copies must not
        // reach back into the source scenes after they close.
        foreach (VocabularyBarrier barrier in actor.GetComponentsInChildren<VocabularyBarrier>(true))
            barrier.enabled = false;
        foreach (EnemyHealth health in actor.GetComponentsInChildren<EnemyHealth>(true))
            health.healthSlider = null;
    }

    private static void ValidateProtectedMap(Scene scene, Transform production, Transform independentPhysics, string phase)
    {
        // Verified directly against original Stage4Scene.unity, 2026-10-27:
        // Scene 11 LODGroups; production root 21 MeshColliders/20 enabled;
        // independent physics root 20 MeshColliders/19 enabled;
        // one additional enabled bridge-visual MeshCollider outside physics root.
        LODGroup[] sceneLods = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<LODGroup>(true)).ToArray();
        MeshCollider[] allMap = production.GetComponentsInChildren<MeshCollider>(true);
        MeshCollider[] physics = independentPhysics.GetComponentsInChildren<MeshCollider>(true);
        MeshCollider[] extras = allMap.Where(c => !physics.Contains(c)).ToArray();
        bool valid = sceneLods.Length == 11 &&
                     production.GetComponentsInChildren<LODGroup>(true).Length == 11 &&
                     allMap.Length == 21 &&
                     allMap.Count(x => x.enabled) == 20 &&
                     physics.Length == 20 &&
                     physics.Count(x => x.enabled) == 19 &&
                     extras.Length == 1 &&
                     extras[0].enabled &&
                     extras[0].name == "Bridge_Node1_Node2_wood_light";
        if (!valid)
            throw new InvalidOperationException("[Stage4BossPreview] " + phase +
                " protected map mismatch: sceneLOD=" + sceneLods.Length +
                " mapLOD=" + production.GetComponentsInChildren<LODGroup>(true).Length +
                " mapMesh=" + allMap.Length +
                " mapEnabled=" + allMap.Count(x => x.enabled) +
                " independentMesh=" + physics.Length +
                " independentEnabled=" + physics.Count(x => x.enabled) +
                " extraMesh=" + extras.Length +
                " (expected 11/11/21/20/20/19/1).");
    }

    private static Quaternion FaceCenter(Transform source, Transform center)
    {
        Vector3 direction = center.position - source.position;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.01f ? Quaternion.LookRotation(direction) : Quaternion.identity;
    }

    private static void CheckMarker(Transform found, Vector3 expected)
    {
        if (Vector3.Distance(found.position, expected) > 3f)
            throw new InvalidOperationException("Stage4 marker moved or axis inverted: " + found.name +
                                                " at " + found.position + " expected " + expected);
    }

    private static Transform Marker(Transform map, string marker)
    {
        Transform[] matches = map.GetComponentsInChildren<Transform>(true)
            .Where(x => x.name == marker).ToArray();
        if (matches.Length != 1)
            throw new InvalidOperationException(marker + " must exist exactly once under map LOD0; found " + matches.Length);
        return matches[0];
    }

    private static T RequireUnique<T>(Scene scene, string objectName) where T : Component
    {
        T[] matches = scene.GetRootGameObjects()
            .SelectMany(go => go.GetComponentsInChildren<T>(true))
            .Where(x => x.gameObject.name == objectName).ToArray();
        if (matches.Length != 1)
            throw new InvalidOperationException("Expected one " + objectName + " with " + typeof(T).Name);
        return matches[0];
    }

    private static T RequireSingleComponentInScene<T>(Scene scene) where T : Component
    {
        T[] matches = scene.GetRootGameObjects()
            .SelectMany(go => go.GetComponentsInChildren<T>(true)).ToArray();
        if (matches.Length != 1)
            throw new InvalidOperationException("Expected exactly one " + typeof(T).Name + " in Stage4; found " + matches.Length);
        return matches[0];
    }

    private static GameObject MakeRoot(Scene scene, string name)
    {
        GameObject root = new GameObject(name);
        SceneManager.MoveGameObjectToScene(root, scene);
        return root;
    }

    private static GameObject MakeChild(Transform parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        return child;
    }

    private static void SetProperty(UnityEngine.Object owner, string name, UnityEngine.Object value)
    {
        SerializedObject obj = new SerializedObject(owner);
        SerializedProperty field = obj.FindProperty(name);
        if (field == null) throw new InvalidOperationException(owner.GetType().Name + "." + name + " missing");
        field.objectReferenceValue = value;
        obj.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetProperty(UnityEngine.Object owner, string name, int value)
    {
        SerializedObject obj = new SerializedObject(owner);
        SerializedProperty field = obj.FindProperty(name);
        if (field == null) throw new InvalidOperationException(owner.GetType().Name + "." + name + " missing");
        // Handles int, enum and LayerMask's underlying m_Bits via SerializedProperty.
        if (field.propertyType == SerializedPropertyType.LayerMask ||
            field.propertyType == SerializedPropertyType.Enum)
            field.intValue = value;
        else if (field.propertyType == SerializedPropertyType.Integer)
            field.intValue = value;
        else
            throw new InvalidOperationException(owner.GetType().Name + "." + name + " is not integer/enum");
        obj.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetProperty(UnityEngine.Object owner, string name, float value)
    {
        SerializedObject obj = new SerializedObject(owner);
        SerializedProperty field = obj.FindProperty(name);
        if (field == null || field.propertyType != SerializedPropertyType.Float)
            throw new InvalidOperationException(owner.GetType().Name + "." + name + " must be float");
        field.floatValue = value;
        obj.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetProperty(UnityEngine.Object owner, string name, bool value)
    {
        SerializedObject obj = new SerializedObject(owner);
        SerializedProperty field = obj.FindProperty(name);
        if (field == null || field.propertyType != SerializedPropertyType.Boolean)
            throw new InvalidOperationException(owner.GetType().Name + "." + name + " must be bool");
        field.boolValue = value;
        obj.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetProperty(UnityEngine.Object owner, string name, string value)
    {
        SerializedObject obj = new SerializedObject(owner);
        SerializedProperty field = obj.FindProperty(name);
        if (field == null || field.propertyType != SerializedPropertyType.String)
            throw new InvalidOperationException(owner.GetType().Name + "." + name + " must be string");
        field.stringValue = value;
        obj.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
