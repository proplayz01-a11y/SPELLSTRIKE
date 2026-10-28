#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Stage1NullifySetupMenu
{
    private const string MenuPath =
        "SPELLSTRIKE/Vocabulary/Configure Node 3 NULLIFY Gate";
    private const string ProfilePath =
        "Assets/Data/Vocabulary/Stage1_Vocabulary_Candidate.asset";
    private const string NullifyBarrierMaterialPath =
        "Assets/Materials/NULLIFY_Barrier_Red.mat";

    [MenuItem(MenuPath)]
    public static void Configure()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("[NULLIFY Setup] Open SampleScene before running setup.");
            return;
        }

        Transform node3 = FindInScene(scene, "NODE 3");
        Transform sourceCanvas = FindInScene(scene, "VocabularyPracticeCanvas");
        Transform sourceClue = FindInScene(scene, "MITIGATE_CombatClueCanvas");
        Transform sourceBarrierVisual = FindInScene(scene, "MITIGATE_BarrierVisual");
        Transform jesterShrineCenter =
            FindInScene(scene, "CursedJester_ShrineCenter");
        CursedJesterController jester =
            FindSceneComponent<CursedJesterController>(scene);
        CursedJesterTriggerEncounter trigger =
            FindSceneComponent<CursedJesterTriggerEncounter>(scene);
        TileManager tileManager = FindSceneComponent<TileManager>(scene);
        AttackController attackController = FindSceneComponent<AttackController>(scene);
        MovementofPlayer movementController = FindSceneComponent<MovementofPlayer>(scene);
        VocabularyPracticeSession session =
            FindSceneComponent<VocabularyPracticeSession>(scene);
        StageVocabularyProfile profile =
            AssetDatabase.LoadAssetAtPath<StageVocabularyProfile>(ProfilePath);

        if (node3 == null
            || sourceCanvas == null
            || sourceBarrierVisual == null
            || jesterShrineCenter == null
            || jester == null
            || trigger == null
            || tileManager == null
            || attackController == null
            || movementController == null
            || profile == null)
        {
            Debug.LogError(
                "[NULLIFY Setup] Required references are missing. "
                + $"NODE 3={node3 != null}, source canvas={sourceCanvas != null}, "
                + $"source barrier={sourceBarrierVisual != null}, "
                + $"Jester shrine center={jesterShrineCenter != null}, "
                + $"Jester={jester != null}, Trigger={trigger != null}, "
                + $"TileManager={tileManager != null}, "
                + $"AttackController={attackController != null}, "
                + $"Movement={movementController != null}, Profile={profile != null}.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Configure Node 3 NULLIFY Gate");

        try
        {
            GameObject canvas = GetOrClone(
                node3,
                "NULLIFY_VocabularyPracticeCanvas",
                sourceCanvas.gameObject);
            GameObject flowObject = GetOrCreate(node3, "NULLIFY_EncounterFlow");
            GameObject barrierVisual = ConfigureBarrierVisual(
                node3,
                sourceBarrierVisual,
                jesterShrineCenter,
                jester);

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

            GameObject cluePanel = null;
            TMP_Text clueText = null;
            if (sourceClue != null)
            {
                cluePanel = GetOrClone(
                    node3,
                    "NULLIFY_CombatClueCanvas",
                    sourceClue.gameObject);
                clueText = cluePanel.GetComponentInChildren<TMP_Text>(true);
            }

            VocabularyTeachController teachController =
                GetOrAdd<VocabularyTeachController>(flowObject);
            VocabularyEncounterFlowController encounterFlow =
                GetOrAdd<VocabularyEncounterFlowController>(flowObject);
            VocabularyBarrier barrier =
                GetOrAdd<VocabularyBarrier>(jester.gameObject);

            SetActiveWithUndo(teachPanel, false);
            SetActiveWithUndo(feedbackPanel, false);
            if (cluePanel != null)
                SetActiveWithUndo(cluePanel, false);

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
            ConfigureBarrier(
                barrier,
                profile,
                tileManager,
                session,
                barrierVisual,
                cluePanel,
                clueText);
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
            ConfigureTrigger(trigger, encounterFlow);

            EditorUtility.SetDirty(canvas);
            EditorUtility.SetDirty(flowObject);
            EditorUtility.SetDirty(jester);
            EditorUtility.SetDirty(trigger);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Selection.activeGameObject = trigger.gameObject;
            EditorGUIUtility.PingObject(trigger.gameObject);
            Debug.Log(
                "[NULLIFY Setup] Node 3 pre-combat gate configured and saved. "
                + "NULLIFY uses word index 2, a dedicated Teach/feedback canvas, "
                + "temporary challenge tiles, a red Bramble-style barrier visual, "
                + "and the existing Jester battle as the post-restoration handoff.",
                trigger);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[NULLIFY Setup] Setup failed: {exception.Message}");
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
        Set(so, "wordIndex", 2);
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
        VocabularyPracticeSession session,
        GameObject barrierVisual,
        GameObject cluePanel,
        TMP_Text clueText)
    {
        SerializedObject so = new SerializedObject(barrier);
        Set(so, "profile", profile);
        Set(so, "wordIndex", 2);
        Set(so, "barrierActive", true);
        Set(so, "blockNonTargetDamageWhileActive", true);
        Set(so, "barrierVisual", barrierVisual);
        Set(so, "combatCluePanel", cluePanel);
        Set(so, "combatClueText", clueText);
        Set(so, "revealChallengeOnStart", false);
        Set(so, "tileManager", tileManager);
        Set(so, "maximumPoolTiles", 16);
        Set(so, "guaranteeTargetLettersOnStart", false);
        Set(so, "session", session);
        Apply(so, barrier);
    }

    private static GameObject ConfigureBarrierVisual(
        Transform node3,
        Transform sourceBarrierVisual,
        Transform shrineCenter,
        CursedJesterController jester)
    {
        if (sourceBarrierVisual == null)
        {
            throw new InvalidOperationException(
                "MITIGATE_BarrierVisual was not found. The NULLIFY setup "
                + "requires Bramble's barrier visual as its reusable source.");
        }

        GameObject visual = GetOrClone(
            node3,
            "NULLIFY_BarrierVisual",
            sourceBarrierVisual.gameObject);

        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            throw new InvalidOperationException(
                "NULLIFY_BarrierVisual does not contain a Renderer.");
        }

        Material sourceMaterial = renderers[0].sharedMaterial;
        if (sourceMaterial == null)
        {
            throw new InvalidOperationException(
                "MITIGATE_BarrierVisual has no source material.");
        }

        Material redMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(NullifyBarrierMaterialPath);
        if (redMaterial == null)
        {
            redMaterial = new Material(sourceMaterial)
            {
                name = "NULLIFY_Barrier_Red"
            };
            AssetDatabase.CreateAsset(redMaterial, NullifyBarrierMaterialPath);
        }

        Color red = new Color(0.88f, 0.06f, 0.10f, 0.24f);
        if (redMaterial.HasProperty("_BaseColor"))
            redMaterial.SetColor("_BaseColor", red);
        if (redMaterial.HasProperty("_Color"))
            redMaterial.SetColor("_Color", red);
        EditorUtility.SetDirty(redMaterial);

        foreach (Renderer barrierRenderer in renderers)
        {
            Undo.RecordObject(barrierRenderer, "Assign NULLIFY barrier material");
            barrierRenderer.sharedMaterial = redMaterial;
            EditorUtility.SetDirty(barrierRenderer);
        }

        Bounds jesterBounds = GetCombinedRendererBounds(jester.gameObject);
        float diameter = Mathf.Max(
            jesterBounds.size.x,
            jesterBounds.size.y,
            jesterBounds.size.z) * 1.35f;

        Undo.RecordObject(visual.transform, "Position NULLIFY barrier visual");
        visual.transform.position = shrineCenter.position;
        visual.transform.rotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one * Mathf.Max(1f, diameter);
        EditorUtility.SetDirty(visual.transform);

        Collider[] colliders = visual.GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
        {
            Undo.RecordObject(collider, "Disable NULLIFY visual collider");
            collider.enabled = false;
            EditorUtility.SetDirty(collider);
        }

        SetActiveWithUndo(visual, false);
        return visual;
    }

    private static Bounds GetCombinedRendererBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(root.transform.position, Vector3.one * 2f);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds;
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

    private static void ConfigureTrigger(
        CursedJesterTriggerEncounter trigger,
        VocabularyEncounterFlowController encounterFlow)
    {
        SerializedObject so = new SerializedObject(trigger);
        Set(so, "vocabularyEncounterFlow", encounterFlow);
        Set(so, "requireNullifyBeforeCombat", true);
        Apply(so, trigger);
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
        SerializedProperty property = RequireProperty(so, propertyName);
        property.intValue = value;
    }

    private static void Set(SerializedObject so, string propertyName, bool value)
    {
        SerializedProperty property = RequireProperty(so, propertyName);
        property.boolValue = value;
    }

    private static SerializedProperty RequireProperty(
        SerializedObject so,
        string propertyName)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property == null)
            throw new InvalidOperationException(
                $"Serialized property '{propertyName}' was not found on "
                + $"{so.targetObject.GetType().Name}.");
        return property;
    }
}
#endif
