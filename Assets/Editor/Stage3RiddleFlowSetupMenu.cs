using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Stage3RiddleFlowSetupMenu
{
    private const string Stage3ScenePath = "Assets/Scenes/Stage3Scene/Stage3Scene.unity";
    private const string Node4Name = "Node 4";
    private const string RiddleTargetName = "Stage3_RiddleValidationTarget";
    private const string LegacyNode4TargetName = "Stage3_Node4_ENDURE_VocabularyTarget";
    private const string LaserOriginName = "LaserSpawnPoint";
    private const string LegacyLaserOriginName = "Stage3_RiddleLaserOrigin";
    private const string LaserPrefabPath = "Assets/Scripts/Attacks/LaserBeam.prefab";
    private const string PersistentHudPrefabPath = "Assets/Prefabs/PersistentGameplayHUD.prefab";

    // Batch-only entry point used when the normal Unity Editor is closed. It
    // intentionally opens Stage3Scene because a headless batch process has no
    // user's dirty scene to protect. Normal menu-driven Install() remains
    // conservative and never switches scenes automatically.
    public static void InstallBatchSafe()
    {
        if (!Application.isBatchMode)
            throw new InvalidOperationException(
                "Stage3RiddleFlowSetupMenu.InstallBatchSafe is batch-mode only.");

        Scene scene = EditorSceneManager.OpenScene(
            Stage3ScenePath,
            UnityEditor.SceneManagement.OpenSceneMode.Single);
        if (!scene.IsValid() || scene.path != Stage3ScenePath)
            throw new InvalidOperationException("Could not open Stage3Scene in batch mode.");

        Install();
        Audit();
        AssetDatabase.SaveAssets();
        Debug.Log("[Stage3RiddleFlowSetup] BATCH INSTALL COMPLETE.");
    }

    [MenuItem("SPELLSTRIKE/Stage 3/Riddle/Install Node 4 Riddle Flow")]
    public static void Install()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != Stage3ScenePath)
        {
            Debug.LogError(
                $"[Stage3RiddleFlowSetup] Open '{Stage3ScenePath}' before installing. "
                + "This tool never switches away from a dirty scene automatically.");
            return;
        }

        GameObject node4 = FindGameObjectInScene(scene, Node4Name);
        GameObject targetObject = FindGameObjectInScene(scene, RiddleTargetName)
            ?? FindGameObjectInScene(scene, LegacyNode4TargetName);
        if (node4 == null || targetObject == null)
        {
            Debug.LogError(
                "[Stage3RiddleFlowSetup] Node 4 or its riddle validation target is missing. "
                + "No scene changes were made.");
            return;
        }

        StageNodeTrigger trigger = node4.GetComponent<StageNodeTrigger>();
        VocabularyBarrier barrier = targetObject.GetComponent<VocabularyBarrier>();
        VocabularyWorldTarget worldTarget = targetObject.GetComponent<VocabularyWorldTarget>();
        SphereCollider targetCollider = targetObject.GetComponent<SphereCollider>();
        if (trigger == null || barrier == null || worldTarget == null || targetCollider == null)
        {
            Debug.LogError(
                "[Stage3RiddleFlowSetup] Required Node 4 trigger/target components are missing. "
                + "No scene changes were made.");
            return;
        }

        GameObject laserPrefabObject = AssetDatabase.LoadAssetAtPath<GameObject>(LaserPrefabPath);
        LaserBeam laserPrefab = laserPrefabObject != null
            ? laserPrefabObject.GetComponent<LaserBeam>()
            : null;
        if (laserPrefab == null)
        {
            Debug.Log(
                "[Stage3RiddleFlowSetup] Optional legacy LaserBeam prefab is not bound. "
                + "Node 4 uses its dedicated, colored Stage3RiddleOutcomeBeam runtime effect instead.");
        }

        Undo.RegisterCompleteObjectUndo(node4, "Install Stage 3 Riddle Flow");
        Undo.RegisterCompleteObjectUndo(targetObject, "Configure Stage 3 Riddle Target");
        Undo.RegisterCompleteObjectUndo(trigger, "Wire Node 4 Riddle Trigger");
        Undo.RegisterCompleteObjectUndo(barrier, "Configure Riddle Barrier");
        Undo.RegisterCompleteObjectUndo(worldTarget, "Configure Riddle World Target");
        Undo.RegisterCompleteObjectUndo(targetCollider, "Configure Riddle Target Collider");

        Stage3RiddleController controller = node4.GetComponent<Stage3RiddleController>();
        if (controller == null)
            controller = Undo.AddComponent<Stage3RiddleController>(node4);

        Transform laserOrigin = FindGameObjectInScene(scene, LaserOriginName)?.transform
            ?? node4.transform.Find(LegacyLaserOriginName);
        if (laserOrigin == null)
            laserOrigin = ReadObjectReference(controller, "laserOrigin") as Transform;

        if (laserPrefab == null)
            laserPrefab = ReadObjectReference(controller, "laserPrefab") as LaserBeam;

        if (laserOrigin == null)
            Debug.Log(
                "[Stage3RiddleFlowSetup] No authored LaserSpawnPoint exists; "
                + "the blue/red outcome beam will originate above Van until one is authored.");

        targetCollider.isTrigger = true;
        targetCollider.radius = Mathf.Max(1.25f, targetCollider.radius);

        ConfigureBarrier(barrier);
        ConfigureWorldTarget(worldTarget, barrier, targetObject.transform);
        ConfigureRiddleController(controller, barrier, worldTarget, laserPrefab, laserOrigin);
        ConfigureNodeTrigger(trigger, controller);

        EditorUtility.SetDirty(node4);
        EditorUtility.SetDirty(targetObject);
        EditorUtility.SetDirty(trigger);
        EditorUtility.SetDirty(barrier);
        EditorUtility.SetDirty(worldTarget);
        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(targetCollider);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log(
            "[Stage3RiddleFlowSetup] PASS. Node 4 now hands off to Stage3RiddleController, "
            + "reuses the staged ENDURE world target as the four-riddle projectile target, "
            + "and no longer feeds ENDURE into Stage 3 combat tiles. "
            + "Correct/incorrect outcome laser colors are provided at runtime. "
            + "Post-riddle boss hooks remain optional authored references and are never fabricated by this installer.");
    }

    [MenuItem("SPELLSTRIKE/Stage 3/Riddle/Audit Node 4 Riddle Flow")]
    public static void Audit()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != Stage3ScenePath)
        {
            Debug.LogError($"[Stage3RiddleFlowSetup] Audit requires '{Stage3ScenePath}' to be open.");
            return;
        }

        GameObject node4 = FindGameObjectInScene(scene, Node4Name);
        GameObject targetObject = FindGameObjectInScene(scene, RiddleTargetName)
            ?? FindGameObjectInScene(scene, LegacyNode4TargetName);
        StageNodeTrigger trigger = node4 != null ? node4.GetComponent<StageNodeTrigger>() : null;
        Stage3RiddleController controller = node4 != null ? node4.GetComponent<Stage3RiddleController>() : null;
        VocabularyBarrier barrier = targetObject != null ? targetObject.GetComponent<VocabularyBarrier>() : null;
        VocabularyWorldTarget worldTarget = targetObject != null ? targetObject.GetComponent<VocabularyWorldTarget>() : null;
        SphereCollider targetCollider = targetObject != null ? targetObject.GetComponent<SphereCollider>() : null;
        Transform laserOrigin = node4 != null
            ? FindGameObjectInScene(scene, LaserOriginName)?.transform
                ?? node4.transform.Find(LegacyLaserOriginName)
            : null;

        GameObject persistentHudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PersistentHudPrefabPath);
        Stage3RiddleHUDView hudView = persistentHudPrefab != null
            ? persistentHudPrefab.GetComponentInChildren<Stage3RiddleHUDView>(true)
            : null;

        if (controller != null && laserOrigin == null)
            laserOrigin = ReadObjectReference(controller, "laserOrigin") as Transform;

        UnityEngine.Object laserPrefab = controller != null
            ? ReadObjectReference(controller, "laserPrefab")
            : null;
        UnityEngine.Object bossRoot = controller != null
            ? ReadObjectReference(controller, "bossRoot")
            : null;
        UnityEngine.Object bossReceiver = controller != null
            ? ReadObjectReference(controller, "bossBattleReceiver")
            : null;

        bool pass = node4 != null
            && targetObject != null
            && trigger != null
            && controller != null
            && barrier != null
            && worldTarget != null
            && targetCollider != null
            && targetCollider.enabled
            && ReferenceEquals(ReadObjectReference(trigger, "battleReceiver"), controller)
            && ReadBool(trigger, "sendStartBattleMessage")
            && !ReadBool(trigger, "requireVocabularyBeforeCombat")
            && ReadObjectReference(trigger, "vocabularyEncounterFlow") == null
            && !ReadBool(trigger, "configureStage3VocabularyTiles")
            && string.IsNullOrEmpty(ReadString(trigger, "stage3VocabularyMainWord"))
            && string.IsNullOrEmpty(ReadString(trigger, "stage3VocabularyRemainderWord"))
            && ReadObjectReference(barrier, "profile") == null
            && ReferenceEquals(ReadObjectReference(controller, "riddleBarrier"), barrier)
            && ReferenceEquals(ReadObjectReference(controller, "worldTarget"), worldTarget)
            && ReadObjectReference(worldTarget, "encounterFlow") == null
            && ReferenceEquals(ReadObjectReference(worldTarget, "vocabularyBarrier"), barrier)
            && ReferenceEquals(ReadObjectReference(worldTarget, "targetPoint"), targetObject.transform);

        string report =
            $"[Stage3RiddleFlowSetup] AUDIT {(pass ? "PASS" : "FAIL")}\n"
            + $"Node4={(node4 != null)} Controller={(controller != null)} Target={(targetObject != null)}\n"
            + $"BattleReceiver={(trigger != null ? ReadObjectReference(trigger, "battleReceiver")?.name : "null")}\n"
            + $"VocabularyGateRequired={(trigger != null && ReadBool(trigger, "requireVocabularyBeforeCombat"))} "
            + $"VocabularyTileFeed={(trigger != null && ReadBool(trigger, "configureStage3VocabularyTiles"))}\n"
            + $"RiddleBarrierDetachedFromENDURE={(barrier != null && ReadObjectReference(barrier, "profile") == null)} "
            + $"BarrierBound={(controller != null && ReferenceEquals(ReadObjectReference(controller, "riddleBarrier"), barrier))} "
            + $"WorldTargetBound={(controller != null && ReferenceEquals(ReadObjectReference(controller, "worldTarget"), worldTarget))} "
            + $"WorldTargetDetachedFromENDURE={(worldTarget != null && ReadObjectReference(worldTarget, "encounterFlow") == null)}\n"
            + $"PersistentRiddleHUD={(hudView != null)} "
            + $"LegacyLaserPrefab={(laserPrefab != null)} OptionalLaserOrigin={(laserOrigin != null)} "
            + "RiddleOutcomeBeam=runtime-blue-or-red "
            + $"BossRoot={(bossRoot != null)} BossReceiver={(bossReceiver != null)}";

        if (pass)
            Debug.Log(report);
        else
            Debug.LogError(report);
    }

    [MenuItem("SPELLSTRIKE/Stage 3/Riddle/Bind LaserSpawnPoint To Node 4 (No Scene Save)")]
    public static void BindAuthoredLaserSpawnPoint()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != Stage3ScenePath)
        {
            Debug.LogError(
                "[Stage3RiddleFlowSetup] Open Stage3Scene before binding the riddle laser.");
            return;
        }

        GameObject node4 = FindGameObjectInScene(scene, Node4Name);
        GameObject spawnPoint = FindGameObjectInScene(scene, LaserOriginName);
        Stage3RiddleController controller = node4 != null
            ? node4.GetComponent<Stage3RiddleController>()
            : null;

        if (controller == null || spawnPoint == null)
        {
            Debug.LogError(
                "[Stage3RiddleFlowSetup] Node 4 controller or LaserSpawnPoint is missing. "
                + "No changes made; create or locate the authored spawn point first.");
            return;
        }

        Transform current = ReadObjectReference(controller, "laserOrigin") as Transform;
        if (current != null && current != spawnPoint.transform)
        {
            Debug.LogWarning(
                "[Stage3RiddleFlowSetup] Node 4 already has a different laser origin assigned. "
                + "Preserving that Inspector reference instead of overwriting it.");
            return;
        }

        Undo.RecordObject(controller, "Bind Riddle LaserSpawnPoint");
        SerializedObject so = new SerializedObject(controller);
        SetObjectReference(so, "laserOrigin", spawnPoint.transform);
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log(
            "[Stage3RiddleFlowSetup] Bound Node 4 blue/red riddle beam origin to LaserSpawnPoint. "
            + "Stage3Scene is marked dirty but was NOT saved; save it when your other scene edits are ready.");
    }

    private static void ConfigureBarrier(VocabularyBarrier barrier)
    {
        SerializedObject so = new SerializedObject(barrier);
        SetObjectReference(so, "profile", null);
        SerializedProperty wordIndex = so.FindProperty("wordIndex");
        if (wordIndex != null)
            wordIndex.intValue = 0;
        SetBool(so, "revealChallengeOnStart", false);
        SetBool(so, "blockNonTargetDamageWhileActive", true);
        SetBool(so, "guaranteeTargetLettersOnStart", false);
        SetObjectReference(so, "combatCluePanel", null);
        SetObjectReference(so, "combatClueText", null);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureWorldTarget(
        VocabularyWorldTarget worldTarget,
        VocabularyBarrier barrier,
        Transform targetPoint)
    {
        SerializedObject so = new SerializedObject(worldTarget);
        SetObjectReference(so, "encounterFlow", null);
        SetObjectReference(so, "vocabularyBarrier", barrier);
        SetObjectReference(so, "targetPoint", targetPoint);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureRiddleController(
        Stage3RiddleController controller,
        VocabularyBarrier barrier,
        VocabularyWorldTarget worldTarget,
        LaserBeam laserPrefab,
        Transform laserOrigin)
    {
        SerializedObject so = new SerializedObject(controller);
        SetObjectReference(so, "riddleBarrier", barrier);
        SetObjectReference(so, "worldTarget", worldTarget);
        if (laserPrefab != null)
            SetObjectReference(so, "laserPrefab", laserPrefab);
        if (laserOrigin != null)
            SetObjectReference(so, "laserOrigin", laserOrigin);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureNodeTrigger(StageNodeTrigger trigger, Stage3RiddleController controller)
    {
        SerializedObject so = new SerializedObject(trigger);
        SetObjectReference(so, "enemyRoot", null);
        SetObjectReference(so, "battleReceiver", controller);
        SetString(so, "startBattleMessage", "StartBattle");
        SetBool(so, "sendStartBattleMessage", true);
        SetBool(so, "requireVocabularyBeforeCombat", false);
        SetObjectReference(so, "vocabularyEncounterFlow", null);
        SetBool(so, "configureStage3VocabularyTiles", false);
        SetString(so, "stage3VocabularyMainWord", string.Empty);
        SetString(so, "stage3VocabularyRemainderWord", string.Empty);
        SetString(so, "combatStartedMessage", "[Stage3 Node4] Riddle trial started.");
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject FindGameObjectInScene(Scene scene, string objectName)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            Transform found = transforms.FirstOrDefault(item => item.name == objectName);
            if (found != null)
                return found.gameObject;
        }

        return null;
    }

    private static UnityEngine.Object ReadObjectReference(UnityEngine.Object target, string propertyName)
    {
        if (target == null) return null;
        SerializedProperty property = new SerializedObject(target).FindProperty(propertyName);
        return property != null ? property.objectReferenceValue : null;
    }

    private static bool ReadBool(UnityEngine.Object target, string propertyName)
    {
        if (target == null) return false;
        SerializedProperty property = new SerializedObject(target).FindProperty(propertyName);
        return property != null && property.boolValue;
    }

    private static string ReadString(UnityEngine.Object target, string propertyName)
    {
        if (target == null) return string.Empty;
        SerializedProperty property = new SerializedObject(target).FindProperty(propertyName);
        return property != null ? property.stringValue : string.Empty;
    }

    private static void SetObjectReference(SerializedObject so, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property == null)
            throw new MissingFieldException(so.targetObject.GetType().Name, propertyName);
        property.objectReferenceValue = value;
    }

    private static void SetBool(SerializedObject so, string propertyName, bool value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property == null)
            throw new MissingFieldException(so.targetObject.GetType().Name, propertyName);
        property.boolValue = value;
    }

    private static void SetString(SerializedObject so, string propertyName, string value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property == null)
            throw new MissingFieldException(so.targetObject.GetType().Name, propertyName);
        property.stringValue = value ?? string.Empty;
    }
}
