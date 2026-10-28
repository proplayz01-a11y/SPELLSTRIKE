#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Stage2SubmergeSetupMenu
{
    private const string MenuPath =
        "SPELLSTRIKE/Vocabulary/Configure Stage 2 Node 1 SUBMERGE Gate";
    private const string ProfilePath =
        "Assets/Data/Vocabulary/Stage2_Vocabulary_Candidate.asset";
    private const string SessionName = "Stage2_VocabularyPracticeSession";

    private readonly struct NodeSetup
    {
        public readonly int NodeIndex;
        public readonly int WordIndex;
        public readonly string Word;
        public readonly string CanvasName;
        public readonly string FlowName;
        public readonly string EnemyName;

        public NodeSetup(
            int nodeIndex,
            int wordIndex,
            string word,
            string canvasName,
            string flowName,
            string enemyName)
        {
            NodeIndex = nodeIndex;
            WordIndex = wordIndex;
            Word = word;
            CanvasName = canvasName;
            FlowName = flowName;
            EnemyName = enemyName;
        }
    }

    [MenuItem(MenuPath)]
    public static void ConfigureSubmerge()
    {
        ConfigureNode(new NodeSetup(
            1,
            0,
            "SUBMERGE",
            "Node1_SUBMERGE_VocabularyPracticeCanvas",
            "SUBMERGE_EncounterFlow",
            "Barnacle Husk"));
    }

    [MenuItem("SPELLSTRIKE/Vocabulary/Configure Stage 2 Node 2 NAVIGATE Gate")]
    public static void ConfigureNavigate()
    {
        ConfigureNode(new NodeSetup(
            2,
            1,
            "NAVIGATE",
            "Node2_NAVIGATE_VocabularyPracticeCanvas",
            "NAVIGATE_EncounterFlow",
            "Corsair Phantom"));
    }

    [MenuItem("SPELLSTRIKE/Vocabulary/Configure Stage 2 Node 3 UNDERTOW Gate")]
    public static void ConfigureUndertow()
    {
        ConfigureNode(new NodeSetup(
            3,
            2,
            "UNDERTOW",
            "Node3_UNDERTOW_VocabularyPracticeCanvas",
            "UNDERTOW_EncounterFlow",
            "Tide Wraith"));
    }

    [MenuItem("SPELLSTRIKE/Vocabulary/Configure Stage 2 Node 4 NAUTICAL Gate")]
    public static void ConfigureNautical()
    {
        ConfigureNode(new NodeSetup(
            4,
            3,
            "NAUTICAL",
            "Node4_NAUTICAL_VocabularyPracticeCanvas",
            "NAUTICAL_EncounterFlow",
            "Drowned Captain"));
    }

    private static void ConfigureNode(NodeSetup setup)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded || scene.name != "Stage2Scene")
        {
            Debug.LogError(
                "[SUBMERGE Setup] Open Stage2Scene before running this command. "
                + "No scene changes were made.");
            return;
        }

        StageVocabularyProfile profile =
            AssetDatabase.LoadAssetAtPath<StageVocabularyProfile>(ProfilePath);
        if (profile == null)
        {
            VocabularyPrototypeSetupMenu.CreateOrUpdateStage2CandidateProfile();
            profile = AssetDatabase.LoadAssetAtPath<StageVocabularyProfile>(ProfilePath);
        }

        StageNodeTrigger trigger = FindNodeTrigger(scene, setup.NodeIndex);
        GameObject enemyRoot = GetTriggerAssignedObject(trigger, "enemyRoot");
        TileManager tileManager = FindSceneComponent<TileManager>(scene);
        AttackController attackController = FindSceneComponent<AttackController>(scene);
        MovementofPlayer movementController = FindSceneComponent<MovementofPlayer>(scene);
        GameObject practiceCanvasObject = FindSceneObject(scene, setup.CanvasName);
        Canvas practiceCanvas = practiceCanvasObject != null
            ? practiceCanvasObject.GetComponent<Canvas>()
            : null;

        if (profile == null
            || profile.StageNumber != 2
            || profile.WordCount < 1
            || profile.GetWord(setup.WordIndex) == null
            || !profile.GetWord(setup.WordIndex).Matches(setup.Word)
            || trigger == null
            || enemyRoot == null
            || tileManager == null
            || attackController == null
            || movementController == null
            || practiceCanvas == null
            || !HasRequiredPracticeCanvasContract(practiceCanvas.transform))
        {
            Debug.LogError(
                $"[{setup.Word} Setup] Required references are missing or the Stage 2 profile is invalid. "
                + $"Profile={profile != null}, Trigger={trigger != null}, "
                + $"EnemyRoot={enemyRoot != null}, TileManager={tileManager != null}, "
                + $"AttackController={attackController != null}, Movement={movementController != null}, "
                + $"{setup.CanvasName}={practiceCanvas != null}. "
                + "No scene changes were made.");
            return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName($"Configure Stage 2 Node {setup.NodeIndex} {setup.Word} Gate");
        int undoGroup = Undo.GetCurrentGroup();

        try
        {
            ConfigurePracticeCanvas(practiceCanvas, setup.Word);
            VocabularyPracticeSession session = GetOrCreateSession(scene);
            GameObject flowObject = GetOrCreateSceneObject(scene, setup.FlowName);

            GameObject teachPanel =
                FindRecursive(practiceCanvas.transform, "TeachPanel").gameObject;
            GameObject feedbackPanel =
                FindRecursive(practiceCanvas.transform, "FeedbackPanel").gameObject;

            VocabularyTeachController teach =
                GetOrAdd<VocabularyTeachController>(flowObject);
            VocabularyEncounterFlowController flow =
                GetOrAdd<VocabularyEncounterFlowController>(flowObject);
            VocabularyBarrier barrier =
                GetOrAdd<VocabularyBarrier>(enemyRoot);

            ConfigureTeach(
                teach,
                profile,
                teachPanel,
                tileManager,
                attackController,
                session,
                setup.WordIndex);
            ConfigureBarrier(
                barrier,
                profile,
                tileManager,
                session,
                setup.WordIndex);
            ConfigureFlow(
                flow,
                teach,
                barrier,
                tileManager,
                attackController,
                movementController,
                feedbackPanel);
            ConfigureTrigger(trigger, flow, setup);

            SetActiveWithUndo(teachPanel, false);
            SetActiveWithUndo(feedbackPanel, false);

            EditorUtility.SetDirty(flowObject);
            EditorUtility.SetDirty(enemyRoot);
            EditorUtility.SetDirty(trigger);
            EditorSceneManager.MarkSceneDirty(scene);

            Selection.activeGameObject = trigger.gameObject;
            EditorGUIUtility.PingObject(trigger.gameObject);
            Debug.Log(
                $"[{setup.Word} Setup] Node {setup.NodeIndex} gate configured but not auto-saved. "
                + $"Inspect {setup.CanvasName}, {setup.FlowName}, "
                + $"{setup.EnemyName}'s VocabularyBarrier, and the Node {setup.NodeIndex} trigger before saving Stage2Scene.",
                trigger);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[{setup.Word} Setup] Setup failed: {exception.Message}");
            Undo.RevertAllDownToGroup(undoGroup);
        }
        finally
        {
            Undo.CollapseUndoOperations(undoGroup);
        }
    }

    private static VocabularyPracticeSession GetOrCreateSession(Scene scene)
    {
        VocabularyPracticeSession existing =
            FindSceneComponent<VocabularyPracticeSession>(scene);
        if (existing != null)
            return existing;

        GameObject root = GetOrCreateSceneObject(scene, SessionName);
        return GetOrAdd<VocabularyPracticeSession>(root);
    }

    private static void ConfigurePracticeCanvas(Canvas canvas, string word)
    {
        if (canvas == null)
            return;

        Transform canvasTransform = canvas.transform;
        if (canvasTransform.parent != null)
        {
            Undo.SetTransformParent(
                canvasTransform,
                null,
                $"Move {word} Vocabulary Canvas to Scene Root");
        }

        RectTransform rect = canvas.GetComponent<RectTransform>();
        if (rect != null)
        {
            Undo.RecordObject(rect, $"Configure {word} Vocabulary Canvas Transform");
            rect.localScale = Vector3.one;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        Undo.RecordObject(canvas, $"Configure {word} Vocabulary Canvas");
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            Undo.RecordObject(scaler, $"Configure {word} Vocabulary Canvas Scaler");
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;
            scaler.referencePixelsPerUnit = 100f;
        }
    }

    private static bool HasRequiredPracticeCanvasContract(Transform canvas)
    {
        return FindRecursive(canvas, "TeachPanel") != null
               && FindRequiredComponent<TMP_Text>(canvas, "Word Text")
               && FindRequiredComponent<TMP_Text>(canvas, "Part Of Speech Text")
               && FindRequiredComponent<TMP_Text>(canvas, "PronunciationText")
               && FindRequiredComponent<TMP_Text>(canvas, "Definition Text")
               && FindRequiredComponent<TMP_Text>(canvas, "Example Text")
               && FindRequiredComponent<TMP_Text>(canvas, "Instruction Text")
               && FindRequiredComponent<Button>(canvas, "Click Anywhere Button")
               && FindRecursive(canvas, "FeedbackPanel") != null
               && FindRequiredComponent<TMP_Text>(canvas, "FeedbackMessageText 1")
               && FindRequiredComponent<TMP_Text>(canvas, "FeedbackMessageText 2")
               && FindRequiredComponent<Button>(canvas, "FeedbackContinueButton");
    }

    private static bool FindRequiredComponent<T>(Transform parent, string name)
        where T : Component
    {
        Transform found = FindRecursive(parent, name);
        return found != null && found.GetComponent<T>() != null;
    }

    private static void ConfigureTeach(
        VocabularyTeachController teach,
        StageVocabularyProfile profile,
        GameObject panel,
        TileManager tileManager,
        AttackController attackController,
        VocabularyPracticeSession session,
        int wordIndex)
    {
        SerializedObject so = new SerializedObject(teach);
        Set(so, "profile", profile);
        Set(so, "wordIndex", wordIndex);
        Set(so, "showOnStart", false);
        Set(so, "requireGuidedReconstruction", false);
        Set(so, "panelRoot", panel);
        Set(so, "wordText", Require<TMP_Text>(panel.transform, "Word Text"));
        Set(so, "partOfSpeechText", Require<TMP_Text>(panel.transform, "Part Of Speech Text"));
        Set(so, "pronunciationText", Require<TMP_Text>(panel.transform, "PronunciationText"));
        Set(so, "definitionText", Require<TMP_Text>(panel.transform, "Definition Text"));
        Set(so, "contextText", Require<TMP_Text>(panel.transform, "Example Text"));
        Set(so, "instructionText", Require<TMP_Text>(panel.transform, "Instruction Text"));
        Set(so, "feedbackText", (UnityEngine.Object)null);
        Set(so, "primaryButton", (UnityEngine.Object)null);
        Set(so, "primaryButtonLabel", (UnityEngine.Object)null);
        Set(so, "hintButton", (UnityEngine.Object)null);
        Set(so, "clickAnywhereButton", Require<Button>(panel.transform, "Click Anywhere Button"));
        Set(so, "tileManager", tileManager);
        Set(so, "attackController", attackController);
        Set(so, "session", session);
        Set(so, "pauseGameplayWhileTeaching", true);
        Apply(so, teach);
    }

    private static void ConfigureBarrier(
        VocabularyBarrier barrier,
        StageVocabularyProfile profile,
        TileManager tileManager,
        VocabularyPracticeSession session,
        int wordIndex)
    {
        SerializedObject so = new SerializedObject(barrier);
        Set(so, "profile", profile);
        Set(so, "wordIndex", wordIndex);
        Set(so, "barrierActive", true);
        Set(so, "blockNonTargetDamageWhileActive", true);
        Set(so, "barrierVisual", (UnityEngine.Object)null);
        Set(so, "combatCluePanel", (UnityEngine.Object)null);
        Set(so, "combatClueText", (UnityEngine.Object)null);
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
        GameObject feedbackPanel)
    {
        SerializedObject so = new SerializedObject(flow);
        Set(so, "teachController", teach);
        Set(so, "vocabularyBarrier", barrier);
        Set(so, "tileManager", tileManager);
        Set(so, "attackController", attackController);
        Set(so, "movementController", movementController);
        Set(so, "feedbackPanel", feedbackPanel);
        Set(so, "feedbackText", Require<TMP_Text>(feedbackPanel.transform, "FeedbackMessageText 1"));
        Set(so, "feedbackExplanationText", Require<TMP_Text>(feedbackPanel.transform, "FeedbackMessageText 2"));
        Set(so, "feedbackContinueButton", Require<Button>(feedbackPanel.transform, "FeedbackContinueButton"));
        Set(so, "maximumPoolTiles", 16);
        Apply(so, flow);
    }

    private static void ConfigureTrigger(
        StageNodeTrigger trigger,
        VocabularyEncounterFlowController flow,
        NodeSetup setup)
    {
        SerializedObject so = new SerializedObject(trigger);
        Set(so, "requireVocabularyBeforeCombat", true);
        Set(so, "vocabularyEncounterFlow", flow);
        Set(so, "startCombatIfVocabularyFlowFails", true);
        Set(so, "vocabularyStartedMessage", $"[Stage2 Node{setup.NodeIndex}] {setup.Word} Teach started.");
        Set(so, "vocabularyCompletedMessage", $"[Stage2 Node{setup.NodeIndex}] {setup.Word} retrieved. Starting {setup.EnemyName} combat.");
        Set(so, "vocabularyFailedMessage", $"[Stage2 Node{setup.NodeIndex}] {setup.Word} flow failed. Starting fallback combat.");
        Apply(so, trigger);
    }

    private static GameObject GetTriggerAssignedObject(
        StageNodeTrigger trigger,
        string propertyName)
    {
        if (trigger == null)
            return null;

        SerializedObject so = new SerializedObject(trigger);
        SerializedProperty property = so.FindProperty(propertyName);
        return property != null ? property.objectReferenceValue as GameObject : null;
    }

    private static StageNodeTrigger FindNodeTrigger(Scene scene, int nodeIndex)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            StageNodeTrigger[] triggers = root.GetComponentsInChildren<StageNodeTrigger>(true);
            foreach (StageNodeTrigger trigger in triggers)
            {
                SerializedObject so = new SerializedObject(trigger);
                SerializedProperty property = so.FindProperty("nodeIndex");
                if (property != null && property.intValue == nodeIndex)
                    return trigger;
            }
        }

        return null;
    }

    private static GameObject GetOrCreateSceneObject(Scene scene, string name)
    {
        GameObject existing = FindSceneObject(scene, name);
        if (existing != null)
            return existing;

        GameObject created = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(created, $"Create {name}");
        return created;
    }

    private static GameObject FindSceneObject(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = FindRecursive(root.transform, name);
            if (found != null)
                return found.gameObject;
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

    private static T Require<T>(Transform parent, string name) where T : Component
    {
        Transform found = FindRecursive(parent, name);
        if (found == null)
            throw new InvalidOperationException($"Required object '{name}' was not found.");
        T component = found.GetComponent<T>();
        if (component == null)
            throw new InvalidOperationException($"'{name}' does not contain {typeof(T).Name}.");
        return component;
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

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T existing = target.GetComponent<T>();
        return existing != null ? existing : Undo.AddComponent<T>(target);
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

    private static void Set(SerializedObject so, string name, UnityEngine.Object value)
    {
        SerializedProperty property = RequireProperty(so, name);
        property.objectReferenceValue = value;
    }

    private static void Set(SerializedObject so, string name, string value)
    {
        SerializedProperty property = RequireProperty(so, name);
        property.stringValue = value;
    }

    private static void Set(SerializedObject so, string name, int value)
    {
        SerializedProperty property = RequireProperty(so, name);
        property.intValue = value;
    }

    private static void Set(SerializedObject so, string name, bool value)
    {
        SerializedProperty property = RequireProperty(so, name);
        property.boolValue = value;
    }

    private static SerializedProperty RequireProperty(SerializedObject so, string name)
    {
        SerializedProperty property = so.FindProperty(name);
        if (property == null)
            throw new InvalidOperationException(
                $"Serialized property '{name}' was not found on {so.targetObject.GetType().Name}.");
        return property;
    }
}
#endif
