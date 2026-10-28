#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Stage1FractureSetupMenu
{
    private const string MenuPath =
        "SPELLSTRIKE/Vocabulary/Configure Node 2 FRACTURE Counterspell";
    private const string ProfilePath =
        "Assets/Data/Vocabulary/Stage1_Vocabulary_Candidate.asset";

    [MenuItem(MenuPath)]
    public static void Configure()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("[FRACTURE Setup] Open SampleScene before running setup.");
            return;
        }

        Transform node2 = FindInScene(scene, "NODE 2");
        Transform sourceCanvas = FindInScene(scene, "VocabularyPracticeCanvas");
        StoneSentinelsController sentinel =
            FindSceneComponent<StoneSentinelsController>(scene);
        TileManager tileManager = FindSceneComponent<TileManager>(scene);
        AttackController attackController = FindSceneComponent<AttackController>(scene);
        MovementofPlayer movementController = FindSceneComponent<MovementofPlayer>(scene);
        VocabularyPracticeSession session =
            FindSceneComponent<VocabularyPracticeSession>(scene);
        StageVocabularyProfile profile =
            AssetDatabase.LoadAssetAtPath<StageVocabularyProfile>(ProfilePath);

        if (node2 == null
            || sourceCanvas == null
            || sentinel == null
            || tileManager == null
            || attackController == null
            || movementController == null
            || profile == null)
        {
            Debug.LogError(
                "[FRACTURE Setup] Required scene references are missing. "
                + $"NODE 2={node2 != null}, source canvas={sourceCanvas != null}, "
                + $"Sentinel={sentinel != null}, TileManager={tileManager != null}, "
                + $"AttackController={attackController != null}, "
                + $"Movement={movementController != null}, Profile={profile != null}.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Configure Node 2 FRACTURE Counterspell");

        try
        {
            GameObject canvas = GetOrClone(
                node2,
                "FRACTURE_VocabularyPracticeCanvas",
                sourceCanvas.gameObject);
            GameObject flowObject = GetOrCreate(node2, "FRACTURE_EncounterFlow");

            GameObject teachPanel = RequireChild(canvas.transform, "TeachPanel");
            GameObject feedbackPanel = RequireChild(canvas.transform, "FeedbackPanel");
            TMP_Text wordText = RequireComponent<TMP_Text>(canvas.transform, "Word Text");
            TMP_Text partOfSpeechText =
                RequireComponent<TMP_Text>(canvas.transform, "Part Of Speech Text");
            TMP_Text definitionText =
                RequireComponent<TMP_Text>(canvas.transform, "Definition Text");
            TMP_Text exampleText =
                RequireComponent<TMP_Text>(canvas.transform, "Example Text");
            TMP_Text instructionText =
                RequireComponent<TMP_Text>(canvas.transform, "Instruction Text");
            Button clickAnywhere =
                RequireComponent<Button>(canvas.transform, "Click Anywhere Button");
            TMP_Text feedbackText =
                RequireComponent<TMP_Text>(canvas.transform, "FeedbackMessageText 1");
            TMP_Text feedbackExplanationText =
                RequireComponent<TMP_Text>(canvas.transform, "FeedbackMessageText 2");
            Button feedbackContinue =
                RequireComponent<Button>(canvas.transform, "FeedbackContinueButton");

            VocabularyTeachController teachController =
                GetOrAdd<VocabularyTeachController>(flowObject);
            VocabularyEncounterFlowController encounterFlow =
                GetOrAdd<VocabularyEncounterFlowController>(flowObject);
            VocabularyBarrier barrier =
                GetOrAdd<VocabularyBarrier>(sentinel.gameObject);
            StoneSentinelCounterspellController counterspell =
                GetOrAdd<StoneSentinelCounterspellController>(sentinel.gameObject);

            EnemyHealth enemyHealth = sentinel.GetComponent<EnemyHealth>();
            Animator animator = sentinel.GetComponent<Animator>();
            if (enemyHealth == null || animator == null)
                throw new InvalidOperationException(
                    "Stone Sentinel needs EnemyHealth and Animator on its root.");

            GameObject groundShield =
                RequireChild(node2, "STONE-SENTINEL-SHIELD");
            Transform leftHand =
                FindRecursive(sentinel.transform, "mixamorig:LeftHand");
            if (leftHand == null)
                throw new InvalidOperationException(
                    "Stone Sentinel left-hand bone 'mixamorig:LeftHand' was not found.");

            GameObject heldShield = GetOrClone(
                leftHand,
                "FRACTURE_HeldShield",
                groundShield);
            Undo.RecordObject(heldShield.transform, "Align FRACTURE held shield");
            heldShield.transform.localPosition = Vector3.zero;
            heldShield.transform.localRotation = Quaternion.identity;
            DisableHeldShieldPhysics(heldShield);
            SetActiveWithUndo(groundShield, false);
            SetActiveWithUndo(heldShield, false);
            SetActiveWithUndo(teachPanel, false);
            SetActiveWithUndo(feedbackPanel, false);

            ConfigureTeach(
                teachController,
                profile,
                teachPanel,
                wordText,
                partOfSpeechText,
                definitionText,
                exampleText,
                instructionText,
                clickAnywhere,
                tileManager,
                attackController,
                session);
            ConfigureBarrier(barrier, profile, tileManager, session);
            ConfigureFlow(
                encounterFlow,
                teachController,
                barrier,
                tileManager,
                attackController,
                movementController,
                feedbackPanel,
                feedbackText,
                feedbackExplanationText,
                feedbackContinue);
            ConfigureCounterspell(
                counterspell,
                enemyHealth,
                sentinel,
                encounterFlow,
                barrier,
                animator,
                groundShield,
                heldShield);

            EditorUtility.SetDirty(canvas);
            EditorUtility.SetDirty(flowObject);
            EditorUtility.SetDirty(sentinel);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Selection.activeGameObject = sentinel.gameObject;
            EditorGUIUtility.PingObject(sentinel.gameObject);
            Debug.Log(
                "[FRACTURE Setup] Node 2 Counterspell configured and saved. "
                + "FRACTURE uses word index 1, the existing ground shield, "
                + "a left-hand held-shield clone, and a dedicated Teach/feedback canvas.",
                sentinel);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[FRACTURE Setup] Setup failed: {exception.Message}");
            Undo.RevertAllDownToGroup(undoGroup);
        }
        finally
        {
            Undo.CollapseUndoOperations(undoGroup);
        }
    }

    private static void ConfigureTeach(
        VocabularyTeachController controller,
        StageVocabularyProfile profile,
        GameObject panel,
        TMP_Text wordText,
        TMP_Text partOfSpeechText,
        TMP_Text definitionText,
        TMP_Text exampleText,
        TMP_Text instructionText,
        Button clickAnywhere,
        TileManager tileManager,
        AttackController attackController,
        VocabularyPracticeSession session)
    {
        SerializedObject so = new SerializedObject(controller);
        Set(so, "profile", profile);
        Set(so, "wordIndex", 1);
        Set(so, "showOnStart", false);
        Set(so, "requireGuidedReconstruction", false);
        Set(so, "panelRoot", panel);
        Set(so, "wordText", wordText);
        Set(so, "partOfSpeechText", partOfSpeechText);
        Set(so, "definitionText", definitionText);
        Set(so, "contextText", exampleText);
        Set(so, "instructionText", instructionText);
        Set(so, "feedbackText", null);
        Set(so, "primaryButton", null);
        Set(so, "primaryButtonLabel", null);
        Set(so, "hintButton", null);
        Set(so, "clickAnywhereButton", clickAnywhere);
        Set(so, "tileManager", tileManager);
        Set(so, "attackController", attackController);
        Set(so, "session", session);
        Set(so, "pauseGameplayWhileTeaching", true);
        Apply(so, controller);
    }

    private static void ConfigureBarrier(
        VocabularyBarrier barrier,
        StageVocabularyProfile profile,
        TileManager tileManager,
        VocabularyPracticeSession session)
    {
        SerializedObject so = new SerializedObject(barrier);
        Set(so, "profile", profile);
        Set(so, "wordIndex", 1);
        Set(so, "barrierActive", true);
        Set(so, "blockNonTargetDamageWhileActive", true);
        Set(so, "barrierVisual", null);
        Set(so, "combatCluePanel", null);
        Set(so, "combatClueText", null);
        Set(so, "revealChallengeOnStart", false);
        Set(so, "tileManager", tileManager);
        Set(so, "maximumPoolTiles", 16);
        Set(so, "guaranteeTargetLettersOnStart", false);
        Set(so, "session", session);
        Apply(so, barrier);
    }

    private static void ConfigureFlow(
        VocabularyEncounterFlowController flow,
        VocabularyTeachController teach,
        VocabularyBarrier barrier,
        TileManager tileManager,
        AttackController attackController,
        MovementofPlayer movementController,
        GameObject feedbackPanel,
        TMP_Text feedbackText,
        TMP_Text feedbackExplanationText,
        Button feedbackContinue)
    {
        SerializedObject so = new SerializedObject(flow);
        Set(so, "teachController", teach);
        Set(so, "vocabularyBarrier", barrier);
        Set(so, "tileManager", tileManager);
        Set(so, "attackController", attackController);
        Set(so, "movementController", movementController);
        Set(so, "feedbackPanel", feedbackPanel);
        Set(so, "feedbackText", feedbackText);
        Set(so, "feedbackExplanationText", feedbackExplanationText);
        Set(so, "feedbackContinueButton", feedbackContinue);
        Set(so, "maximumPoolTiles", 16);
        Apply(so, flow);
    }

    private static void ConfigureCounterspell(
        StoneSentinelCounterspellController counterspell,
        EnemyHealth enemyHealth,
        StoneSentinelsController sentinel,
        VocabularyEncounterFlowController encounterFlow,
        VocabularyBarrier barrier,
        Animator animator,
        GameObject groundShield,
        GameObject heldShield)
    {
        SerializedObject so = new SerializedObject(counterspell);
        Set(so, "enemyHealth", enemyHealth);
        Set(so, "sentinelController", sentinel);
        Set(so, "encounterFlow", encounterFlow);
        Set(so, "vocabularyBarrier", barrier);
        Set(so, "animator", animator);
        Set(so, "groundShield", groundShield);
        Set(so, "heldShield", heldShield);
        Set(so, "healthThreshold", 0.5f);
        Apply(so, counterspell);
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(target);
    }

    private static GameObject GetOrCreate(Transform parent, string name)
    {
        Transform existing = FindRecursive(parent, name);
        if (existing != null)
            return existing.gameObject;

        GameObject created = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(created, $"Create {name}");
        Undo.SetTransformParent(created.transform, parent, $"Parent {name}");
        created.transform.localPosition = Vector3.zero;
        created.transform.localRotation = Quaternion.identity;
        created.transform.localScale = Vector3.one;
        return created;
    }

    private static GameObject GetOrClone(
        Transform parent,
        string name,
        GameObject source)
    {
        Transform existing = FindRecursive(parent, name);
        if (existing != null)
            return existing.gameObject;

        GameObject clone = UnityEngine.Object.Instantiate(source, parent);
        clone.name = name;
        Undo.RegisterCreatedObjectUndo(clone, $"Create {name}");
        return clone;
    }

    private static GameObject RequireChild(Transform parent, string name)
    {
        Transform child = FindRecursive(parent, name);
        if (child == null)
            throw new InvalidOperationException($"Required object '{name}' was not found.");
        return child.gameObject;
    }

    private static T RequireComponent<T>(Transform parent, string name)
        where T : Component
    {
        GameObject child = RequireChild(parent, name);
        T component = child.GetComponent<T>();
        if (component == null)
            throw new InvalidOperationException(
                $"'{name}' does not contain {typeof(T).Name}.");
        return component;
    }

    private static Transform FindInScene(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = FindRecursive(root.transform, name);
            if (found != null)
                return found;
        }

        return null;
    }

    private static T FindSceneComponent<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null)
                return component;
        }

        return null;
    }

    private static Transform FindRecursive(Transform root, string name)
    {
        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindRecursive(root.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }

    private static void SetActiveWithUndo(GameObject target, bool active)
    {
        if (target.activeSelf == active)
            return;

        Undo.RecordObject(target, $"Set {target.name} active");
        target.SetActive(active);
        EditorUtility.SetDirty(target);
    }

    private static void DisableHeldShieldPhysics(GameObject heldShield)
    {
        foreach (Collider collider in heldShield.GetComponentsInChildren<Collider>(true))
        {
            if (!collider.enabled)
                continue;

            Undo.RecordObject(collider, "Disable FRACTURE held-shield collider");
            collider.enabled = false;
            EditorUtility.SetDirty(collider);
        }

        foreach (Rigidbody rigidbody in heldShield.GetComponentsInChildren<Rigidbody>(true))
        {
            Undo.RecordObject(rigidbody, "Configure FRACTURE held-shield Rigidbody");
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
            EditorUtility.SetDirty(rigidbody);
        }
    }

    private static void Apply(SerializedObject so, UnityEngine.Object target)
    {
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }

    private static void Set(
        SerializedObject so,
        string propertyName,
        UnityEngine.Object value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property == null)
            throw new InvalidOperationException(
                $"Serialized property '{propertyName}' was not found on "
                + $"{so.targetObject.GetType().Name}.");
        property.objectReferenceValue = value;
    }

    private static void Set(SerializedObject so, string propertyName, int value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property == null)
            throw new InvalidOperationException(
                $"Serialized property '{propertyName}' was not found.");
        property.intValue = value;
    }

    private static void Set(SerializedObject so, string propertyName, bool value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property == null)
            throw new InvalidOperationException(
                $"Serialized property '{propertyName}' was not found.");
        property.boolValue = value;
    }

    private static void Set(SerializedObject so, string propertyName, float value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property == null)
            throw new InvalidOperationException(
                $"Serialized property '{propertyName}' was not found.");
        property.floatValue = value;
    }
}
#endif
