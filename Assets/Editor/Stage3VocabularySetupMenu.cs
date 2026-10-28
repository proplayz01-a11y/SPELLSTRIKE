using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Stage3VocabularySetupMenu
{
    private const string Stage2ScenePath = "Assets/Scenes/Stage2Scene/Stage2Scene.unity";
    private const string Stage3ScenePath = "Assets/Scenes/Stage3Scene/Stage3Scene.unity";
    private const string ProfilePath = "Assets/Data/Vocabulary/Stage3_Vocabulary_Candidate.asset";
    private const string SystemRootName = "_Stage3_VocabularySystem";
    private const string StagingRootName = "_Stage3_VocabularySystem_BUILDING";
    private const string SourceFlowName = "NAVIGATE_EncounterFlow";
    private const string SourceCanvasName = "Node2_NAVIGATE_VocabularyPracticeCanvas";
    private const string VioletMaterialPath = "Assets/Stage 3/Materials/Stage3_Fragment_Violet.mat";
    private const string FallbackMaterialPath = "Assets/Materials/FragmentMaterial.mat";

    private static readonly string[] NodeWords =
    {
        "SCORCHED",
        "DROUGHT",
        "SAND",
        "ENDURE"
    };

    [MenuItem("SPELLSTRIKE/Stage 3/Install Vocabulary Gates")]
    public static void Install()
    {
        Scene stage3Scene = SceneManager.GetActiveScene();
        if (!stage3Scene.IsValid() || stage3Scene.path != Stage3ScenePath)
        {
            Debug.LogError(
                $"[Stage3Vocabulary] Open '{Stage3ScenePath}' before running the installer. " +
                "The installer will not switch away from a dirty scene automatically.");
            return;
        }

        StageNodeTrigger[] nodeTriggers = ResolveStage3NodeTriggers(stage3Scene);
        if (nodeTriggers == null)
            return;

        bool stage2WasAlreadyLoaded = false;
        Scene stage2Scene = SceneManager.GetSceneByPath(Stage2ScenePath);
        if (stage2Scene.IsValid() && stage2Scene.isLoaded)
        {
            stage2WasAlreadyLoaded = true;
        }
        else
        {
            stage2Scene = EditorSceneManager.OpenScene(
                Stage2ScenePath,
                UnityEditor.SceneManagement.OpenSceneMode.Additive);
        }

        try
        {
            SceneManager.SetActiveScene(stage3Scene);

            GameObject sourceFlowObject = FindGameObjectInScene(stage2Scene, SourceFlowName);
            GameObject sourceCanvas = FindGameObjectInScene(stage2Scene, SourceCanvasName);
            if (sourceFlowObject == null || sourceCanvas == null)
            {
                Debug.LogError(
                    $"[Stage3Vocabulary] Could not locate Stage 2 template '{SourceFlowName}' " +
                    $"and '{SourceCanvasName}'. No Stage 3 vocabulary gates were installed.");
                return;
            }

            VocabularyEncounterFlowController sourceFlow =
                sourceFlowObject.GetComponent<VocabularyEncounterFlowController>();
            VocabularyTeachController sourceTeach =
                sourceFlowObject.GetComponent<VocabularyTeachController>();
            if (sourceFlow == null || sourceTeach == null)
            {
                Debug.LogError(
                    "[Stage3Vocabulary] Stage 2 vocabulary template is missing its Flow or Teach component.");
                return;
            }

            StageVocabularyProfile profile = GetOrCreateProfile();
            if (profile == null)
                return;

            GameObject staleStagingRoot = FindGameObjectInScene(stage3Scene, StagingRootName);
            if (staleStagingRoot != null)
                UnityEngine.Object.DestroyImmediate(staleStagingRoot);

            GameObject stagingRoot = new GameObject(StagingRootName);
            SceneManager.MoveGameObjectToScene(stagingRoot, stage3Scene);

            GameObject sessionObject = new GameObject("Stage3_VocabularyPracticeSession");
            sessionObject.transform.SetParent(stagingRoot.transform, false);
            VocabularyPracticeSession session = sessionObject.AddComponent<VocabularyPracticeSession>();

            VocabularyEncounterFlowController[] stagedFlows =
                new VocabularyEncounterFlowController[nodeTriggers.Length];

            Material targetMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(VioletMaterialPath)
                ?? AssetDatabase.LoadAssetAtPath<Material>(FallbackMaterialPath);

            try
            {
                for (int i = 0; i < nodeTriggers.Length; i++)
                {
                    int nodeIndex = i + 1;
                    string word = NodeWords[i];
                    StageNodeTrigger nodeTrigger = nodeTriggers[i];

                    GameObject nodeRoot = new GameObject($"Stage3_Node{nodeIndex}_{word}_Vocabulary");
                    nodeRoot.transform.SetParent(stagingRoot.transform, false);

                    GameObject clonedCanvas = UnityEngine.Object.Instantiate(sourceCanvas);
                    clonedCanvas.name = $"Node{nodeIndex}_{word}_VocabularyPracticeCanvas";
                    SceneManager.MoveGameObjectToScene(clonedCanvas, stage3Scene);
                    clonedCanvas.transform.SetParent(nodeRoot.transform, false);
                    NormalizeCanvasTransform(clonedCanvas);

                    GameObject clonedFlowObject = UnityEngine.Object.Instantiate(sourceFlowObject);
                    clonedFlowObject.name = $"{word}_EncounterFlow";
                    SceneManager.MoveGameObjectToScene(clonedFlowObject, stage3Scene);
                    clonedFlowObject.transform.SetParent(nodeRoot.transform, false);
                    clonedFlowObject.transform.localPosition = Vector3.zero;
                    clonedFlowObject.transform.localRotation = Quaternion.identity;
                    clonedFlowObject.transform.localScale = Vector3.one;

                    VocabularyEncounterFlowController clonedFlow =
                        clonedFlowObject.GetComponent<VocabularyEncounterFlowController>();
                    VocabularyTeachController clonedTeach =
                        clonedFlowObject.GetComponent<VocabularyTeachController>();
                    if (clonedFlow == null || clonedTeach == null)
                    {
                        throw new InvalidOperationException(
                            $"Cloned vocabulary template for Node {nodeIndex} is missing Flow or Teach.");
                    }

                    RewireTeach(
                        sourceTeach,
                        clonedTeach,
                        sourceCanvas,
                        clonedCanvas,
                        profile,
                        i,
                        session);

                    GameObject target = CreateVocabularyWorldTarget(
                        nodeTrigger.transform,
                        nodeRoot.transform,
                        clonedFlow,
                        profile,
                        i,
                        word,
                        session,
                        targetMaterial);

                    VocabularyBarrier barrier = target.GetComponent<VocabularyBarrier>();
                    RewireFlow(
                        sourceFlow,
                        clonedFlow,
                        sourceCanvas,
                        clonedCanvas,
                        clonedTeach,
                        barrier);

                    stagedFlows[i] = clonedFlow;
                }

                GameObject existingRoot = FindGameObjectInScene(stage3Scene, SystemRootName);
                if (existingRoot != null)
                    UnityEngine.Object.DestroyImmediate(existingRoot);

                stagingRoot.name = SystemRootName;

                for (int i = 0; i < nodeTriggers.Length; i++)
                {
                    int nodeIndex = i + 1;
                    // Stage 3 Node 4 is riddle -> boss only. Never resurrect the
                    // retired ENDURE vocabulary gate, even after a boss/riddle
                    // handoff exists on the node.
                    bool enableGate = nodeIndex < 4;
                    ConfigureNodeTrigger(
                        nodeTriggers[i],
                        stagedFlows[i],
                        NodeWords[i],
                        nodeIndex,
                        enableGate);
                }
            }
            catch (Exception exception)
            {
                if (stagingRoot != null)
                    UnityEngine.Object.DestroyImmediate(stagingRoot);

                Debug.LogException(exception);
                Debug.LogError(
                    "[Stage3Vocabulary] Installation aborted before replacing the existing vocabulary system.");
                return;
            }

            EditorSceneManager.MarkSceneDirty(stage3Scene);
            EditorSceneManager.SaveScene(stage3Scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[Stage3Vocabulary] PASS installed Stage 2-style pre-combat vocabulary gates for " +
                "Node 1 SCORCHED, Node 2 DROUGHT, and Node 3 SAND. " +
                "Node 4 ENDURE gameplay gate is permanently retired; Node 4 is owned by the riddle-to-boss flow.");
        }
        finally
        {
            if (!stage2WasAlreadyLoaded && stage2Scene.IsValid() && stage2Scene.isLoaded)
                EditorSceneManager.CloseScene(stage2Scene, true);

            if (stage3Scene.IsValid() && stage3Scene.isLoaded)
                SceneManager.SetActiveScene(stage3Scene);
        }
    }

    [MenuItem("SPELLSTRIKE/Stage 3/Audit Vocabulary Gates")]
    public static void Audit()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != Stage3ScenePath)
        {
            Debug.LogError($"[Stage3Vocabulary] Audit requires active scene '{Stage3ScenePath}'.");
            return;
        }

        StageVocabularyProfile profile = AssetDatabase.LoadAssetAtPath<StageVocabularyProfile>(ProfilePath);
        bool profilePass =
            profile != null
            && profile.StageNumber == 3
            && profile.WordCount == 4
            && profile.GetWord(0)?.Matches("SCORCHED") == true
            && profile.GetWord(1)?.Matches("DROUGHT") == true
            && profile.GetWord(2)?.Matches("SAND") == true
            && profile.GetWord(3)?.Matches("ENDURE") == true;

        bool pass = profilePass;
        Debug.Log($"[Stage3Vocabulary] Profile {(profilePass ? "PASS" : "FAIL")}.");

        for (int nodeIndex = 1; nodeIndex <= NodeWords.Length; nodeIndex++)
        {
            GameObject node = FindGameObjectInScene(scene, $"Node {nodeIndex}");
            StageNodeTrigger trigger = node != null ? node.GetComponent<StageNodeTrigger>() : null;
            GameObject flowObject = FindGameObjectInScene(scene, $"{NodeWords[nodeIndex - 1]}_EncounterFlow");
            GameObject targetObject =
                FindGameObjectInScene(scene, $"Stage3_Node{nodeIndex}_{NodeWords[nodeIndex - 1]}_VocabularyTarget");

            VocabularyEncounterFlowController flow =
                flowObject != null ? flowObject.GetComponent<VocabularyEncounterFlowController>() : null;
            VocabularyTeachController teach =
                flowObject != null ? flowObject.GetComponent<VocabularyTeachController>() : null;
            VocabularyBarrier barrier =
                targetObject != null ? targetObject.GetComponent<VocabularyBarrier>() : null;
            VocabularyWorldTarget worldTarget =
                targetObject != null ? targetObject.GetComponent<VocabularyWorldTarget>() : null;
            Collider targetCollider =
                targetObject != null ? targetObject.GetComponent<Collider>() : null;

            bool nodePass =
                trigger != null
                && flow != null
                && teach != null
                && barrier != null
                && worldTarget != null
                && targetCollider != null
                && targetCollider.enabled;

            if (trigger != null)
            {
                SerializedObject so = new SerializedObject(trigger);
                bool shouldRequireGate = nodeIndex < 4;
                nodePass &=
                    so.FindProperty("requireVocabularyBeforeCombat").boolValue == shouldRequireGate
                    && so.FindProperty("vocabularyEncounterFlow").objectReferenceValue == flow
                    && so.FindProperty("configureStage3VocabularyTiles").boolValue
                    && string.Equals(
                        so.FindProperty("stage3VocabularyMainWord").stringValue,
                        NodeWords[nodeIndex - 1],
                        StringComparison.Ordinal);
            }

            if (nodePass)
            {
                nodePass &= ValidateGeneratedBindings(
                    profile,
                    nodeIndex - 1,
                    flow,
                    teach,
                    barrier,
                    worldTarget,
                    targetObject);
            }

            pass &= nodePass;
            bool stagedNode4 = nodeIndex == 4 && trigger != null;
            Debug.Log(
                $"[Stage3Vocabulary] Node {nodeIndex} {NodeWords[nodeIndex - 1]} " +
                $"{(nodePass ? (stagedNode4 ? "PASS (STAGED, gate disabled)" : "PASS") : "FAIL")}.");
        }

        Debug.Log($"[Stage3Vocabulary] AUDIT {(pass ? "PASS" : "FAIL")}.");
    }

    private static StageVocabularyProfile GetOrCreateProfile()
    {
        StageVocabularyProfile profile =
            AssetDatabase.LoadAssetAtPath<StageVocabularyProfile>(ProfilePath);

        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<StageVocabularyProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);
        }

        profile.Configure(
            3,
            new List<VocabularyWordData>
            {
                VocabularyWordData.CreateStage3ScorchedCandidate(),
                VocabularyWordData.CreateStage3DroughtCandidate(),
                VocabularyWordData.CreateStage3SandCandidate(),
                VocabularyWordData.CreateStage3EndureCandidate()
            });

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        return profile;
    }

    private static StageNodeTrigger[] ResolveStage3NodeTriggers(Scene scene)
    {
        StageNodeTrigger[] triggers = new StageNodeTrigger[NodeWords.Length];
        for (int nodeIndex = 1; nodeIndex <= NodeWords.Length; nodeIndex++)
        {
            GameObject node = FindGameObjectInScene(scene, $"Node {nodeIndex}");
            StageNodeTrigger trigger = node != null ? node.GetComponent<StageNodeTrigger>() : null;
            if (trigger == null)
            {
                Debug.LogError($"[Stage3Vocabulary] Node {nodeIndex} is missing StageNodeTrigger.");
                return null;
            }

            triggers[nodeIndex - 1] = trigger;
        }

        return triggers;
    }

    private static void RewireTeach(
        VocabularyTeachController source,
        VocabularyTeachController clone,
        GameObject sourceCanvas,
        GameObject cloneCanvas,
        StageVocabularyProfile profile,
        int wordIndex,
        VocabularyPracticeSession session)
    {
        SerializedObject sourceSo = new SerializedObject(source);
        SerializedObject cloneSo = new SerializedObject(clone);

        cloneSo.FindProperty("profile").objectReferenceValue = profile;
        cloneSo.FindProperty("wordIndex").intValue = wordIndex;
        cloneSo.FindProperty("showOnStart").boolValue = false;
        cloneSo.FindProperty("requireGuidedReconstruction").boolValue = false;

        string[] uiProperties =
        {
            "panelRoot",
            "wordText",
            "partOfSpeechText",
            "pronunciationText",
            "definitionText",
            "contextText",
            "instructionText",
            "feedbackText",
            "primaryButton",
            "primaryButtonLabel",
            "hintButton",
            "clickAnywhereButton"
        };

        foreach (string propertyName in uiProperties)
            RemapSerializedReference(sourceSo, cloneSo, propertyName, sourceCanvas, cloneCanvas);

        cloneSo.FindProperty("tileManager").objectReferenceValue = null;
        cloneSo.FindProperty("attackController").objectReferenceValue = null;
        cloneSo.FindProperty("session").objectReferenceValue = session;
        cloneSo.FindProperty("pauseGameplayWhileTeaching").boolValue = true;
        cloneSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(clone);
    }

    private static void RewireFlow(
        VocabularyEncounterFlowController source,
        VocabularyEncounterFlowController clone,
        GameObject sourceCanvas,
        GameObject cloneCanvas,
        VocabularyTeachController teach,
        VocabularyBarrier barrier)
    {
        SerializedObject sourceSo = new SerializedObject(source);
        SerializedObject cloneSo = new SerializedObject(clone);

        cloneSo.FindProperty("teachController").objectReferenceValue = teach;
        cloneSo.FindProperty("vocabularyBarrier").objectReferenceValue = barrier;
        cloneSo.FindProperty("tileManager").objectReferenceValue = null;
        cloneSo.FindProperty("attackController").objectReferenceValue = null;
        cloneSo.FindProperty("movementController").objectReferenceValue = null;

        string[] uiProperties =
        {
            "feedbackPanel",
            "feedbackText",
            "feedbackExplanationText",
            "feedbackContinueButton"
        };

        foreach (string propertyName in uiProperties)
            RemapSerializedReference(sourceSo, cloneSo, propertyName, sourceCanvas, cloneCanvas);

        cloneSo.FindProperty("maximumPoolTiles").intValue = 16;
        cloneSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(clone);
    }

    private static GameObject CreateVocabularyWorldTarget(
        Transform nodeTransform,
        Transform parent,
        VocabularyEncounterFlowController flow,
        StageVocabularyProfile profile,
        int wordIndex,
        string word,
        VocabularyPracticeSession session,
        Material targetMaterial)
    {
        GameObject targetRoot = new GameObject(
            $"Stage3_Node{wordIndex + 1}_{word}_VocabularyTarget");
        targetRoot.transform.SetParent(parent, false);
        targetRoot.transform.position =
            nodeTransform.position
            + Vector3.up * 3.25f
            + nodeTransform.forward * 4.5f;

        SphereCollider collider = targetRoot.AddComponent<SphereCollider>();
        collider.isTrigger = false;
        collider.radius = 1.25f;

        VocabularyBarrier barrier = targetRoot.AddComponent<VocabularyBarrier>();
        VocabularyWorldTarget worldTarget = targetRoot.AddComponent<VocabularyWorldTarget>();

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "VocabularyBarrierVisual";
        visual.transform.SetParent(targetRoot.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one * 1.85f;

        Collider visualCollider = visual.GetComponent<Collider>();
        if (visualCollider != null)
            UnityEngine.Object.DestroyImmediate(visualCollider);

        MeshRenderer renderer = visual.GetComponent<MeshRenderer>();
        if (renderer != null && targetMaterial != null)
            renderer.sharedMaterial = targetMaterial;

        SerializedObject barrierSo = new SerializedObject(barrier);
        barrierSo.FindProperty("profile").objectReferenceValue = profile;
        barrierSo.FindProperty("wordIndex").intValue = wordIndex;
        barrierSo.FindProperty("barrierActive").boolValue = true;
        barrierSo.FindProperty("blockNonTargetDamageWhileActive").boolValue = true;
        barrierSo.FindProperty("nonTargetDamageMultiplier").floatValue = 0.35f;
        barrierSo.FindProperty("targetWordDamageMultiplier").floatValue = 1.25f;
        barrierSo.FindProperty("barrierVisual").objectReferenceValue = visual;
        barrierSo.FindProperty("combatCluePanel").objectReferenceValue = null;
        barrierSo.FindProperty("combatClueText").objectReferenceValue = null;
        barrierSo.FindProperty("revealChallengeOnStart").boolValue = false;
        barrierSo.FindProperty("tileManager").objectReferenceValue = null;
        barrierSo.FindProperty("maximumPoolTiles").intValue = 16;
        barrierSo.FindProperty("guaranteeTargetLettersOnStart").boolValue = false;
        barrierSo.FindProperty("session").objectReferenceValue = session;
        barrierSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject targetSo = new SerializedObject(worldTarget);
        targetSo.FindProperty("encounterFlow").objectReferenceValue = flow;
        targetSo.FindProperty("vocabularyBarrier").objectReferenceValue = barrier;
        targetSo.FindProperty("targetPoint").objectReferenceValue = targetRoot.transform;
        targetSo.FindProperty("intactVisual").objectReferenceValue = visual;
        targetSo.FindProperty("fracturedVisual").objectReferenceValue = null;
        targetSo.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(barrier);
        EditorUtility.SetDirty(worldTarget);
        return targetRoot;
    }

    private static void ConfigureNodeTrigger(
        StageNodeTrigger trigger,
        VocabularyEncounterFlowController flow,
        string word,
        int nodeIndex,
        bool enableGate)
    {
        SerializedObject so = new SerializedObject(trigger);
        so.FindProperty("requireVocabularyBeforeCombat").boolValue = enableGate;
        so.FindProperty("vocabularyEncounterFlow").objectReferenceValue = flow;
        so.FindProperty("startCombatIfVocabularyFlowFails").boolValue = true;
        so.FindProperty("configureStage3VocabularyTiles").boolValue = true;
        so.FindProperty("stage3VocabularyMainWord").stringValue = word;
        so.FindProperty("stage3VocabularyRemainderWord").stringValue = string.Empty;
        so.FindProperty("vocabularyStartedMessage").stringValue =
            $"[Stage3 Node{nodeIndex}] {word} Teach started.";
        so.FindProperty("vocabularyCompletedMessage").stringValue =
            $"[Stage3 Node{nodeIndex}] {word} retrieved. Starting combat.";
        so.FindProperty("vocabularyFailedMessage").stringValue =
            $"[Stage3 Node{nodeIndex}] {word} flow failed. Starting fallback combat.";
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(trigger);
    }

    private static bool ValidateGeneratedBindings(
        StageVocabularyProfile profile,
        int wordIndex,
        VocabularyEncounterFlowController flow,
        VocabularyTeachController teach,
        VocabularyBarrier barrier,
        VocabularyWorldTarget worldTarget,
        GameObject targetObject)
    {
        if (profile == null
            || flow == null
            || teach == null
            || barrier == null
            || worldTarget == null
            || targetObject == null)
        {
            return false;
        }

        SerializedObject flowSo = new SerializedObject(flow);
        SerializedObject teachSo = new SerializedObject(teach);
        SerializedObject barrierSo = new SerializedObject(barrier);
        SerializedObject worldTargetSo = new SerializedObject(worldTarget);

        bool flowPass =
            flowSo.FindProperty("teachController").objectReferenceValue == teach
            && flowSo.FindProperty("vocabularyBarrier").objectReferenceValue == barrier
            && flowSo.FindProperty("feedbackPanel").objectReferenceValue != null
            && flowSo.FindProperty("feedbackText").objectReferenceValue != null
            && flowSo.FindProperty("feedbackContinueButton").objectReferenceValue != null;

        bool teachPass =
            teachSo.FindProperty("profile").objectReferenceValue == profile
            && teachSo.FindProperty("wordIndex").intValue == wordIndex
            && teachSo.FindProperty("panelRoot").objectReferenceValue != null
            && teachSo.FindProperty("wordText").objectReferenceValue != null
            && teachSo.FindProperty("definitionText").objectReferenceValue != null
            && (teachSo.FindProperty("primaryButton").objectReferenceValue != null
                || teachSo.FindProperty("clickAnywhereButton").objectReferenceValue != null);

        bool barrierPass =
            barrierSo.FindProperty("profile").objectReferenceValue == profile
            && barrierSo.FindProperty("wordIndex").intValue == wordIndex
            && barrierSo.FindProperty("session").objectReferenceValue != null
            && barrierSo.FindProperty("barrierVisual").objectReferenceValue != null
            && !barrierSo.FindProperty("revealChallengeOnStart").boolValue
            && !barrierSo.FindProperty("guaranteeTargetLettersOnStart").boolValue;

        bool targetPass =
            worldTargetSo.FindProperty("encounterFlow").objectReferenceValue == flow
            && worldTargetSo.FindProperty("vocabularyBarrier").objectReferenceValue == barrier
            && worldTargetSo.FindProperty("targetPoint").objectReferenceValue == targetObject.transform
            && worldTargetSo.FindProperty("intactVisual").objectReferenceValue != null;

        VocabularyWordData expectedWord = profile.GetWord(wordIndex);
        bool wordPass =
            expectedWord != null
            && barrier.TargetWord != null
            && expectedWord.Matches(barrier.TargetWord.Word);

        return flowPass && teachPass && barrierPass && targetPass && wordPass;
    }

    private static void RemapSerializedReference(
        SerializedObject source,
        SerializedObject clone,
        string propertyName,
        GameObject sourceCanvas,
        GameObject cloneCanvas)
    {
        SerializedProperty sourceProperty = source.FindProperty(propertyName);
        SerializedProperty cloneProperty = clone.FindProperty(propertyName);
        if (sourceProperty == null || cloneProperty == null)
            return;

        cloneProperty.objectReferenceValue =
            RemapObjectReference(
                sourceProperty.objectReferenceValue,
                sourceCanvas.transform,
                cloneCanvas.transform);
    }

    private static UnityEngine.Object RemapObjectReference(
        UnityEngine.Object sourceReference,
        Transform sourceRoot,
        Transform cloneRoot)
    {
        if (sourceReference == null)
            return null;

        Transform sourceTransform;
        Type componentType = null;

        if (sourceReference is GameObject sourceGameObject)
        {
            sourceTransform = sourceGameObject.transform;
        }
        else if (sourceReference is Component sourceComponent)
        {
            sourceTransform = sourceComponent.transform;
            componentType = sourceComponent.GetType();
        }
        else
        {
            return null;
        }

        if (sourceTransform != sourceRoot && !sourceTransform.IsChildOf(sourceRoot))
            return null;

        string path = AnimationUtility.CalculateTransformPath(sourceTransform, sourceRoot);
        Transform cloneTransform = string.IsNullOrEmpty(path)
            ? cloneRoot
            : cloneRoot.Find(path);
        if (cloneTransform == null)
            return null;

        return componentType == null
            ? cloneTransform.gameObject
            : cloneTransform.GetComponent(componentType);
    }

    private static void NormalizeCanvasTransform(GameObject canvasObject)
    {
        if (canvasObject.transform is RectTransform rectTransform)
        {
            rectTransform.localPosition = Vector3.zero;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;
        }

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 120 + canvasObject.transform.GetSiblingIndex();
        }
    }

    private static GameObject FindGameObjectInScene(Scene scene, string objectName)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform candidate in transforms)
            {
                if (candidate != null && candidate.name == objectName)
                    return candidate.gameObject;
            }
        }

        return null;
    }
}
