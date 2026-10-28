using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CanonicalVanAertPlayerMenu
{
    private const string CanonicalPrefabPath = "Assets/Prefabs/VanAertPlayer.prefab";
    private const string TutorialScenePath = "Assets/Scenes/TutorialScene.unity";
    private const string Stage1ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string Stage2ScenePath = "Assets/Scenes/Stage2Scene/Stage2Scene.unity";

    [MenuItem("SPELLSTRIKE/Player/1. Build Canonical Van Aert Prefab From Tutorial")]
    public static void BuildCanonicalPrefabFromTutorial()
    {
        if (!PrepareForSceneMutation("build the canonical Van Aert prefab"))
            return;

        Scene scene = EditorSceneManager.OpenScene(TutorialScenePath, OpenSceneMode.Single);
        GameObject source = FindGameplayVan(scene);
        if (source == null)
        {
            Debug.LogError("[CanonicalVanAert] TutorialScene has no active Player-tagged Van with MovementofPlayer.");
            return;
        }

        GameObject clone = UnityEngine.Object.Instantiate(source);
        clone.name = "VAN AERT";
        clone.hideFlags = HideFlags.HideAndDontSave;

        try
        {
            if (PrefabUtility.IsPartOfPrefabInstance(clone))
            {
                PrefabUtility.UnpackPrefabInstance(
                    clone,
                    PrefabUnpackMode.Completely,
                    InteractionMode.AutomatedAction);
            }

            clone.hideFlags = HideFlags.None;
            clone.transform.SetParent(null, false);
            clone.transform.localPosition = Vector3.zero;
            clone.transform.localRotation = Quaternion.identity;
            clone.transform.localScale = Vector3.one;

            NormalizeCanonicalPlayer(clone);
            ClearSceneOwnedReferences(clone);

            bool success;
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(clone, CanonicalPrefabPath, out success);
            if (!success || prefab == null)
            {
                Debug.LogError($"[CanonicalVanAert] Failed to create {CanonicalPrefabPath}.");
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[CanonicalVanAert] Canonical prefab created/refreshed: {CanonicalPrefabPath}");
        }
        finally
        {
            if (clone != null)
                UnityEngine.Object.DestroyImmediate(clone);
        }
    }

    [MenuItem("SPELLSTRIKE/Player/2. Migrate Tutorial To Canonical Van Aert")]
    public static void MigrateTutorial()
    {
        MigrateScene(TutorialScenePath, "TutorialScene");
    }

    [MenuItem("SPELLSTRIKE/Player/3. Migrate Stage 1 To Canonical Van Aert")]
    public static void MigrateStage1()
    {
        MigrateScene(Stage1ScenePath, "Stage 1");
    }

    [MenuItem("SPELLSTRIKE/Player/4. Migrate Stage 2 To Canonical Van Aert")]
    public static void MigrateStage2()
    {
        MigrateScene(Stage2ScenePath, "Stage 2");
    }

    [MenuItem("SPELLSTRIKE/Player/5. Audit Canonical Van Aert Wiring")]
    public static void AuditCanonicalWiring()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CanonicalPrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[CanonicalVanAert] Missing canonical prefab at {CanonicalPrefabPath}.");
            return;
        }

        string[] scenePaths = { TutorialScenePath, Stage1ScenePath, Stage2ScenePath };
        foreach (string scenePath in scenePaths)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GameObject player = FindGameplayVan(scene);
            if (player == null)
            {
                Debug.LogError($"[CanonicalVanAert] {scene.name}: no gameplay Van found.");
                continue;
            }

            string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(player);
            bool canonical = string.Equals(prefabPath, CanonicalPrefabPath, StringComparison.OrdinalIgnoreCase);
            AttackController attack = player.GetComponent<AttackController>();
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            MovementofPlayer movement = player.GetComponent<MovementofPlayer>();
            Animator animator = player.GetComponent<Animator>();
            Transform spawn = FindDescendant(player.transform, "MagicSpawnPoint");

            Debug.Log(
                $"[CanonicalVanAert] AUDIT {scene.name}: canonical={canonical}, " +
                $"movement={movement != null}, health={health != null}, attack={attack != null}, " +
                $"animator={animator != null}, magicSpawn={spawn != null}, " +
                $"attackButton={(attack != null && attack.attackButton != null)}, " +
                $"tileManager={(attack != null && attack.tileManager != null)}, " +
                $"healthSlider={(health != null && health.healthSlider != null)}.");
        }
    }

    private static void MigrateScene(string scenePath, string label)
    {
        if (!PrepareForSceneMutation($"migrate {label} to the canonical Van Aert prefab"))
            return;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CanonicalPrefabPath);
        if (prefab == null)
        {
            Debug.LogError(
                $"[CanonicalVanAert] Missing {CanonicalPrefabPath}. Run the build command first.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        GameObject oldPlayer = FindGameplayVan(scene);
        if (oldPlayer == null)
        {
            Debug.LogError($"[CanonicalVanAert] {label}: no Player-tagged Van with MovementofPlayer found.");
            return;
        }

        string existingPrefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(oldPlayer);
        if (string.Equals(existingPrefabPath, CanonicalPrefabPath, StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log($"[CanonicalVanAert] {label}: already uses the canonical prefab.");
            return;
        }

        Transform oldParent = oldPlayer.transform.parent;
        int siblingIndex = oldPlayer.transform.GetSiblingIndex();
        Vector3 localPosition = oldPlayer.transform.localPosition;
        Quaternion localRotation = oldPlayer.transform.localRotation;
        Vector3 localScale = oldPlayer.transform.localScale;

        PlayerHealth oldHealth = oldPlayer.GetComponent<PlayerHealth>();
        MovementofPlayer oldMovement = oldPlayer.GetComponent<MovementofPlayer>();
        AttackController oldAttack = oldPlayer.GetComponent<AttackController>();
        Animator oldAnimator = oldPlayer.GetComponent<Animator>();
        CharacterController oldCharacterController = oldPlayer.GetComponent<CharacterController>();
        Rigidbody oldRigidbody = oldPlayer.GetComponent<Rigidbody>();
        CapsuleCollider oldCapsule = oldPlayer.GetComponent<CapsuleCollider>();
        CharacterControllerStepClimber oldStepClimber = oldPlayer.GetComponent<CharacterControllerStepClimber>();

        GameObject newPlayer = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        newPlayer.name = "VAN AERT";
        newPlayer.transform.SetParent(oldParent, false);
        newPlayer.transform.SetSiblingIndex(siblingIndex);
        newPlayer.transform.localPosition = localPosition;
        newPlayer.transform.localRotation = localRotation;
        newPlayer.transform.localScale = localScale;

        PlayerHealth newHealth = newPlayer.GetComponent<PlayerHealth>();
        MovementofPlayer newMovement = newPlayer.GetComponent<MovementofPlayer>();
        AttackController newAttack = newPlayer.GetComponent<AttackController>();
        Animator newAnimator = newPlayer.GetComponent<Animator>();
        CharacterController newCharacterController = newPlayer.GetComponent<CharacterController>();
        Rigidbody newRigidbody = newPlayer.GetComponent<Rigidbody>();
        CapsuleCollider newCapsule = newPlayer.GetComponent<CapsuleCollider>();

        if (oldMovement != null && newMovement != null)
            EditorUtility.CopySerialized(oldMovement, newMovement);

        if (oldHealth != null && newHealth != null)
            EditorUtility.CopySerialized(oldHealth, newHealth);

        if (oldAttack != null && newAttack != null)
            EditorUtility.CopySerialized(oldAttack, newAttack);

        if (oldCharacterController != null && newCharacterController != null)
            EditorUtility.CopySerialized(oldCharacterController, newCharacterController);

        if (oldRigidbody != null && newRigidbody != null)
            EditorUtility.CopySerialized(oldRigidbody, newRigidbody);

        if (oldCapsule != null && newCapsule != null)
            EditorUtility.CopySerialized(oldCapsule, newCapsule);

        if (oldStepClimber != null && newPlayer.GetComponent<CharacterControllerStepClimber>() == null)
        {
            CharacterControllerStepClimber newStepClimber = newPlayer.AddComponent<CharacterControllerStepClimber>();
            EditorUtility.CopySerialized(oldStepClimber, newStepClimber);
        }

        FixInternalPlayerReferences(newPlayer);
        ForceMobileSprintThreshold(newPlayer, 0.92f);

        Dictionary<UnityEngine.Object, UnityEngine.Object> replacements =
            BuildReferenceReplacementMap(
                oldPlayer,
                newPlayer,
                oldHealth,
                newHealth,
                oldMovement,
                newMovement,
                oldAttack,
                newAttack,
                oldAnimator,
                newAnimator,
                oldCharacterController,
                newCharacterController,
                oldRigidbody,
                newRigidbody,
                oldCapsule,
                newCapsule);

        ReplaceSceneReferences(scene, oldPlayer, replacements);

        UnityEngine.Object.DestroyImmediate(oldPlayer);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log(
            $"[CanonicalVanAert] {label}: migrated to {CanonicalPrefabPath}. " +
            "Scene transform and stage-owned UI/combat references were preserved.");
    }

    private static void NormalizeCanonicalPlayer(GameObject player)
    {
        player.tag = "Player";

        Animator animator = player.GetComponent<Animator>();
        if (animator != null)
            animator.applyRootMotion = false;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        FixInternalPlayerReferences(player);
        ForceMobileSprintThreshold(player, 0.92f);
    }

    private static void ClearSceneOwnedReferences(GameObject player)
    {
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        if (health != null)
            health.healthSlider = null;

        AttackController attack = player.GetComponent<AttackController>();
        if (attack != null)
        {
            attack.attackButton = null;
            attack.tileManager = null;
            attack.goalsPanelUI = null;
            attack.tileDebuffManager = null;
        }
    }

    private static void FixInternalPlayerReferences(GameObject player)
    {
        Animator animator = player.GetComponent<Animator>();
        MovementofPlayer movement = player.GetComponent<MovementofPlayer>();
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        AttackController attack = player.GetComponent<AttackController>();
        Transform spawn = FindDescendant(player.transform, "MagicSpawnPoint");

        if (health != null)
        {
            SerializedObject healthSerialized = new SerializedObject(health);
            healthSerialized.FindProperty("playerAnimator").objectReferenceValue = animator;
            healthSerialized.FindProperty("movementController").objectReferenceValue = movement;
            healthSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        if (attack != null)
        {
            attack.playerAnimator = animator;
            attack.movementController = movement;
            attack.magicSpawnPoint = spawn;
            EditorUtility.SetDirty(attack);
        }
    }

    private static void ForceMobileSprintThreshold(GameObject player, float value)
    {
        MovementofPlayer movement = player.GetComponent<MovementofPlayer>();
        if (movement == null)
            return;

        SerializedObject serialized = new SerializedObject(movement);
        SerializedProperty threshold = serialized.FindProperty("mobileSprintThreshold");
        if (threshold != null)
        {
            threshold.floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static Dictionary<UnityEngine.Object, UnityEngine.Object> BuildReferenceReplacementMap(
        GameObject oldPlayer,
        GameObject newPlayer,
        PlayerHealth oldHealth,
        PlayerHealth newHealth,
        MovementofPlayer oldMovement,
        MovementofPlayer newMovement,
        AttackController oldAttack,
        AttackController newAttack,
        Animator oldAnimator,
        Animator newAnimator,
        CharacterController oldCharacterController,
        CharacterController newCharacterController,
        Rigidbody oldRigidbody,
        Rigidbody newRigidbody,
        CapsuleCollider oldCapsule,
        CapsuleCollider newCapsule)
    {
        Dictionary<UnityEngine.Object, UnityEngine.Object> map = new();
        AddReplacement(map, oldPlayer, newPlayer);
        AddReplacement(map, oldPlayer.transform, newPlayer.transform);
        AddReplacement(map, oldHealth, newHealth);
        AddReplacement(map, oldMovement, newMovement);
        AddReplacement(map, oldAttack, newAttack);
        AddReplacement(map, oldAnimator, newAnimator);
        AddReplacement(map, oldCharacterController, newCharacterController);
        AddReplacement(map, oldRigidbody, newRigidbody);
        AddReplacement(map, oldCapsule, newCapsule);

        Transform oldSpawn = FindDescendant(oldPlayer.transform, "MagicSpawnPoint");
        Transform newSpawn = FindDescendant(newPlayer.transform, "MagicSpawnPoint");
        AddReplacement(map, oldSpawn, newSpawn);

        return map;
    }

    private static void AddReplacement(
        IDictionary<UnityEngine.Object, UnityEngine.Object> map,
        UnityEngine.Object oldValue,
        UnityEngine.Object newValue)
    {
        if (oldValue != null && newValue != null)
            map[oldValue] = newValue;
    }

    private static void ReplaceSceneReferences(
        Scene scene,
        GameObject oldPlayer,
        IReadOnlyDictionary<UnityEngine.Object, UnityEngine.Object> replacements)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Component component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null || component.gameObject == oldPlayer || component.transform.IsChildOf(oldPlayer.transform))
                    continue;

                SerializedObject serialized;
                try
                {
                    serialized = new SerializedObject(component);
                }
                catch
                {
                    continue;
                }

                SerializedProperty property = serialized.GetIterator();
                bool enterChildren = true;
                bool changed = false;
                while (property.NextVisible(enterChildren))
                {
                    enterChildren = true;
                    if (property.propertyType != SerializedPropertyType.ObjectReference)
                        continue;

                    UnityEngine.Object current = property.objectReferenceValue;
                    if (current != null && replacements.TryGetValue(current, out UnityEngine.Object replacement))
                    {
                        property.objectReferenceValue = replacement;
                        changed = true;
                    }
                }

                if (changed)
                {
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(component);
                }
            }
        }
    }

    private static GameObject FindGameplayVan(Scene scene)
    {
        return scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<MovementofPlayer>(true))
            .Select(component => component.gameObject)
            .FirstOrDefault(go => go.CompareTag("Player") && go.activeInHierarchy);
    }

    private static Transform FindDescendant(Transform root, string name)
    {
        if (root == null)
            return null;

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
                return child;
        }

        return null;
    }

    private static bool PrepareForSceneMutation(string operation)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError($"[CanonicalVanAert] Cannot {operation} while entering or running Play Mode.");
            return false;
        }

        if (EditorSceneManager.GetSceneManagerSetup().Any(setup =>
                SceneManager.GetSceneByPath(setup.path).IsValid() &&
                SceneManager.GetSceneByPath(setup.path).isDirty))
        {
            Debug.LogError(
                $"[CanonicalVanAert] Refusing to {operation} while an open scene has unsaved changes. Save it first.");
            return false;
        }

        return true;
    }
}
