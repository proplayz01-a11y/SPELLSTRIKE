#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Stage2ErodeEncounterSetupMenu
{
    private const string ProfilePath = "Assets/Data/Vocabulary/Stage2_Vocabulary_Candidate.asset";
    private const string FractureGlowMaterialPath = "Assets/Stage 2/Objects/ERODE_Spiral_Glow.mat";
    private const int ErodeWordIndex = 4;

    [MenuItem("SPELLSTRIKE/Stage 2/ERODE/Configure Environmental Encounter")]
    private static void ConfigureEnvironmentalEncounter()
    {
        StageVocabularyProfile profile =
            AssetDatabase.LoadAssetAtPath<StageVocabularyProfile>(ProfilePath);
        if (profile == null || profile.WordCount <= ErodeWordIndex)
        {
            EditorUtility.DisplayDialog(
                "ERODE Setup",
                "Stage 2 vocabulary profile does not contain ERODE at index 4.",
                "OK");
            return;
        }

        GameObject root = GameObject.Find("ERODE");
        if (root == null)
        {
            EditorUtility.DisplayDialog("ERODE Setup", "Could not find the active ERODE root.", "OK");
            return;
        }

        Transform rockTransform = root.transform.Find("ERODE-ROCK");
        Transform flowTransform = root.transform.Find("ERODE_EncounterFlow");
        if (rockTransform == null || flowTransform == null)
        {
            EditorUtility.DisplayDialog(
                "ERODE Setup",
                "ERODE-ROCK or ERODE_EncounterFlow is missing under the ERODE root.",
                "OK");
            return;
        }

        GameObject rock = rockTransform.gameObject;
        VocabularyEncounterFlowController flow =
            flowTransform.GetComponent<VocabularyEncounterFlowController>();
        VocabularyTeachController teach =
            flowTransform.GetComponent<VocabularyTeachController>();
        if (flow == null || teach == null)
        {
            EditorUtility.DisplayDialog(
                "ERODE Setup",
                "ERODE_EncounterFlow needs both vocabulary flow and Teach controllers.",
                "OK");
            return;
        }

        Undo.SetCurrentGroupName("Configure ERODE environmental encounter");
        int undoGroup = Undo.GetCurrentGroup();

        RenameCanvas(root.transform);
        ConfigureTeach(teach, profile);

        VocabularyBarrier barrier = rock.GetComponent<VocabularyBarrier>();
        if (barrier == null)
            barrier = Undo.AddComponent<VocabularyBarrier>(rock);
        ConfigureBarrier(barrier, profile);

        Collider rockCollider = EnsureRockCollider(rock);
        if (rockCollider == null)
        {
            Undo.RevertAllDownToGroup(undoGroup);
            EditorUtility.DisplayDialog("ERODE Setup", "Could not create a collider for ERODE-ROCK.", "OK");
            return;
        }

        VocabularyWorldTarget worldTarget = rock.GetComponent<VocabularyWorldTarget>();
        if (worldTarget == null)
            worldTarget = Undo.AddComponent<VocabularyWorldTarget>(rock);
        GameObject fracturedPieces = ConfigureFracturedPieces(root.transform, rock.transform);
        ConfigureWorldTarget(worldTarget, flow, barrier, rock, fracturedPieces);
        ConfigureTidecarvedShellReward(root.transform, worldTarget, rockCollider.bounds);

        ConfigureFlow(flow, barrier);
        ConfigureTrigger(root, rockCollider.bounds, flow);

        EditorUtility.SetDirty(root);
        EditorSceneManager.MarkSceneDirty(root.scene);
        Undo.CollapseUndoOperations(undoGroup);
        Selection.activeGameObject = root;

        Debug.Log(
            "[ERODE Setup] Wired ERODE as a Node 2-complete environmental vocabulary target. "
            + "The scene was marked dirty but not saved.",
            root);
    }

    private static void RenameCanvas(Transform root)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name.Trim() != "ERODE_VocabularyPracticeCanvas")
                continue;

            Undo.RecordObject(child.gameObject, "Normalize ERODE Canvas name");
            child.name = "ERODE_VocabularyPracticeCanvas";
            return;
        }
    }

    private static void ConfigureTeach(
        VocabularyTeachController teach,
        StageVocabularyProfile profile)
    {
        SerializedObject serialized = new SerializedObject(teach);
        serialized.FindProperty("profile").objectReferenceValue = profile;
        serialized.FindProperty("wordIndex").intValue = ErodeWordIndex;
        serialized.FindProperty("showOnStart").boolValue = false;
        serialized.FindProperty("requireGuidedReconstruction").boolValue = false;
        serialized.ApplyModifiedProperties();
    }

    private static void ConfigureBarrier(
        VocabularyBarrier barrier,
        StageVocabularyProfile profile)
    {
        SerializedObject serialized = new SerializedObject(barrier);
        serialized.FindProperty("profile").objectReferenceValue = profile;
        serialized.FindProperty("wordIndex").intValue = ErodeWordIndex;
        serialized.FindProperty("barrierActive").boolValue = true;
        serialized.FindProperty("blockNonTargetDamageWhileActive").boolValue = true;
        serialized.FindProperty("barrierVisual").objectReferenceValue = null;
        serialized.FindProperty("combatCluePanel").objectReferenceValue = null;
        serialized.FindProperty("combatClueText").objectReferenceValue = null;
        serialized.FindProperty("revealChallengeOnStart").boolValue = false;
        serialized.FindProperty("guaranteeTargetLettersOnStart").boolValue = false;
        serialized.ApplyModifiedProperties();
    }

    private static Collider EnsureRockCollider(GameObject rock)
    {
        Collider existing = rock.GetComponent<Collider>();
        if (existing != null)
            return existing;

        MeshFilter meshFilter = rock.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
            return null;

        BoxCollider box = Undo.AddComponent<BoxCollider>(rock);
        Bounds meshBounds = meshFilter.sharedMesh.bounds;
        box.center = meshBounds.center;
        box.size = meshBounds.size;
        box.isTrigger = false;
        return box;
    }

    private static void ConfigureWorldTarget(
        VocabularyWorldTarget target,
        VocabularyEncounterFlowController flow,
        VocabularyBarrier barrier,
        GameObject rock,
        GameObject fracturedPieces)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty("encounterFlow").objectReferenceValue = flow;
        serialized.FindProperty("vocabularyBarrier").objectReferenceValue = barrier;
        serialized.FindProperty("targetPoint").objectReferenceValue = rock.transform;
        serialized.FindProperty("intactVisual").objectReferenceValue = rock;
        serialized.FindProperty("fracturedVisual").objectReferenceValue = fracturedPieces;
        serialized.ApplyModifiedProperties();
    }

    private static GameObject ConfigureFracturedPieces(Transform root, Transform intactRock)
    {
        Transform fracturedTransform = root.Find("FracturedPieces");
        if (fracturedTransform == null)
            return null;

        GameObject fracturedRoot = fracturedTransform.gameObject;
        VocabularyFractureEffect effect = fracturedRoot.GetComponent<VocabularyFractureEffect>();
        if (effect == null)
            effect = Undo.AddComponent<VocabularyFractureEffect>(fracturedRoot);

        SerializedObject effectSerialized = new SerializedObject(effect);
        effectSerialized.FindProperty("explosionOrigin").objectReferenceValue = intactRock;
        effectSerialized.ApplyModifiedProperties();

        MeshFilter[] meshFilters = fracturedRoot.GetComponentsInChildren<MeshFilter>(true);
        Material fractureGlow = AssetDatabase.LoadAssetAtPath<Material>(FractureGlowMaterialPath);
        int configuredPieces = 0;
        for (int i = 0; i < meshFilters.Length; i++)
        {
            MeshFilter meshFilter = meshFilters[i];
            if (meshFilter == null || meshFilter.sharedMesh == null)
                continue;

            GameObject piece = meshFilter.gameObject;
            MeshCollider collider = piece.GetComponent<MeshCollider>();
            if (collider == null)
                collider = Undo.AddComponent<MeshCollider>(piece);
            Undo.RecordObject(collider, "Configure ERODE shard collider");
            collider.sharedMesh = meshFilter.sharedMesh;
            collider.convex = true;
            collider.isTrigger = false;

            Rigidbody body = piece.GetComponent<Rigidbody>();
            if (body == null)
                body = Undo.AddComponent<Rigidbody>(piece);
            Undo.RecordObject(body, "Configure ERODE shard Rigidbody");
            body.mass = 1f;
            body.useGravity = true;
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            if (fractureGlow != null)
                ReplaceImportedGlowMaterial(piece, fractureGlow);

            configuredPieces++;
        }

        Undo.RecordObject(fracturedRoot, "Hide ERODE fractured pieces initially");
        fracturedRoot.SetActive(false);
        Debug.Log($"[ERODE Setup] Configured {configuredPieces} fracture pieces.", fracturedRoot);
        return fracturedRoot;
    }

    private static void ConfigureTidecarvedShellReward(
        Transform root,
        VocabularyWorldTarget revealSource,
        Bounds rockWorldBounds)
    {
        Transform rewardTransform = root.Find("TidecarvedShellReward");
        GameObject reward;
        if (rewardTransform == null)
        {
            reward = new GameObject("TidecarvedShellReward");
            Undo.RegisterCreatedObjectUndo(reward, "Create Tidecarved Shell reward");
            reward.transform.SetParent(root, true);
        }
        else
        {
            reward = rewardTransform.gameObject;
        }

        Undo.RecordObject(reward.transform, "Position Tidecarved Shell reward");
        reward.transform.position = new Vector3(
            rockWorldBounds.center.x,
            Mathf.Max(1.5f, rockWorldBounds.min.y + 2.5f),
            rockWorldBounds.center.z);
        reward.transform.rotation = Quaternion.identity;
        reward.transform.localScale = Vector3.one;
        reward.SetActive(true);

        SphereCollider pickupCollider = reward.GetComponent<SphereCollider>();
        if (pickupCollider == null)
            pickupCollider = Undo.AddComponent<SphereCollider>(reward);
        Undo.RecordObject(pickupCollider, "Configure Tidecarved Shell pickup collider");
        pickupCollider.isTrigger = true;
        pickupCollider.radius = 2f;

        Transform visualTransform = reward.transform.Find("ShellVisual");
        GameObject visualRoot;
        if (visualTransform == null)
        {
            visualRoot = new GameObject("ShellVisual");
            Undo.RegisterCreatedObjectUndo(visualRoot, "Create Tidecarved Shell visual");
            visualRoot.transform.SetParent(reward.transform, false);
            CreateShellPlaceholderMeshes(visualRoot.transform);
        }
        else
        {
            visualRoot = visualTransform.gameObject;
        }

        Undo.RecordObject(visualRoot.transform, "Scale Tidecarved Shell visual");
        visualRoot.transform.localScale = Vector3.one * 3.5f;

        PassiveRewardPickup pickup = reward.GetComponent<PassiveRewardPickup>();
        if (pickup == null)
            pickup = Undo.AddComponent<PassiveRewardPickup>(reward);

        SerializedObject serialized = new SerializedObject(pickup);
        serialized.FindProperty("itemId").enumValueIndex = (int)PassiveItemId.TidecarvedShell;
        serialized.FindProperty("revealSource").objectReferenceValue = revealSource;
        serialized.FindProperty("pickupCollider").objectReferenceValue = pickupCollider;
        serialized.FindProperty("visualRoot").objectReferenceValue = visualRoot;
        serialized.ApplyModifiedProperties();

        Undo.RecordObject(visualRoot, "Hide Tidecarved Shell initially");
        visualRoot.SetActive(false);
    }

    private static void CreateShellPlaceholderMeshes(Transform parent)
    {
        GameObject lowerShell = CreatePlaceholderSphere("LowerShell", parent);
        lowerShell.transform.localPosition = new Vector3(0f, -0.12f, 0f);
        lowerShell.transform.localRotation = Quaternion.Euler(-18f, 0f, 0f);
        lowerShell.transform.localScale = new Vector3(1.25f, 0.28f, 0.95f);

        GameObject upperShell = CreatePlaceholderSphere("UpperShell", parent);
        upperShell.transform.localPosition = new Vector3(0f, 0.18f, -0.05f);
        upperShell.transform.localRotation = Quaternion.Euler(24f, 0f, 0f);
        upperShell.transform.localScale = new Vector3(1.2f, 0.25f, 0.9f);

        GameObject pearl = CreatePlaceholderSphere("CurrentPearl", parent);
        pearl.transform.localPosition = new Vector3(0f, 0.18f, 0.35f);
        pearl.transform.localScale = Vector3.one * 0.38f;

        Material glow = AssetDatabase.LoadAssetAtPath<Material>(FractureGlowMaterialPath);
        if (glow != null)
        {
            Renderer pearlRenderer = pearl.GetComponent<Renderer>();
            if (pearlRenderer != null)
                pearlRenderer.sharedMaterial = glow;
        }
    }

    private static GameObject CreatePlaceholderSphere(string name, Transform parent)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = name;
        Undo.RegisterCreatedObjectUndo(sphere, $"Create {name}");
        sphere.transform.SetParent(parent, false);

        Collider primitiveCollider = sphere.GetComponent<Collider>();
        if (primitiveCollider != null)
            Undo.DestroyObjectImmediate(primitiveCollider);

        return sphere;
    }

    private static void ReplaceImportedGlowMaterial(GameObject piece, Material glowMaterial)
    {
        Renderer renderer = piece.GetComponent<Renderer>();
        if (renderer == null)
            return;

        Material[] materials = renderer.sharedMaterials;
        bool changed = false;
        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] == null || materials[i].name != "Atlas_Unlit")
                continue;

            materials[i] = glowMaterial;
            changed = true;
        }

        if (!changed)
            return;

        Undo.RecordObject(renderer, "Apply ERODE shard glow");
        renderer.sharedMaterials = materials;
        EditorUtility.SetDirty(renderer);
    }

    private static void ConfigureFlow(
        VocabularyEncounterFlowController flow,
        VocabularyBarrier barrier)
    {
        SerializedObject serialized = new SerializedObject(flow);
        serialized.FindProperty("vocabularyBarrier").objectReferenceValue = barrier;
        serialized.ApplyModifiedProperties();
    }

    private static void ConfigureTrigger(
        GameObject root,
        Bounds rockWorldBounds,
        VocabularyEncounterFlowController flow)
    {
        Transform triggerTransform = root.transform.Find("ERODE_Trigger");
        GameObject trigger;
        if (triggerTransform == null)
        {
            trigger = new GameObject("ERODE_Trigger");
            Undo.RegisterCreatedObjectUndo(trigger, "Create ERODE trigger");
            trigger.transform.SetParent(root.transform, true);
        }
        else
        {
            trigger = triggerTransform.gameObject;
        }

        Undo.RecordObject(trigger.transform, "Position ERODE trigger");
        trigger.transform.position = rockWorldBounds.center;
        trigger.transform.rotation = Quaternion.identity;
        trigger.transform.localScale = Vector3.one;

        BoxCollider triggerCollider = trigger.GetComponent<BoxCollider>();
        if (triggerCollider == null)
            triggerCollider = Undo.AddComponent<BoxCollider>(trigger);
        Undo.RecordObject(triggerCollider, "Configure ERODE trigger collider");
        triggerCollider.isTrigger = true;
        triggerCollider.center = Vector3.zero;
        triggerCollider.size = new Vector3(
            Mathf.Max(12f, rockWorldBounds.size.x + 10f),
            Mathf.Max(6f, rockWorldBounds.size.y),
            Mathf.Max(12f, rockWorldBounds.size.z + 10f));

        StageNodeTrigger nodeTrigger = trigger.GetComponent<StageNodeTrigger>();
        if (nodeTrigger == null)
            nodeTrigger = Undo.AddComponent<StageNodeTrigger>(trigger);

        StageProgressionManager progression =
            Object.FindFirstObjectByType<StageProgressionManager>();
        SerializedObject serialized = new SerializedObject(nodeTrigger);
        serialized.FindProperty("progressionManager").objectReferenceValue = progression;
        serialized.FindProperty("nodeIndex").intValue = 3;
        serialized.FindProperty("triggerOnce").boolValue = true;
        serialized.FindProperty("playerTag").stringValue = "Player";
        serialized.FindProperty("enemyRoot").objectReferenceValue = null;
        serialized.FindProperty("battleReceiver").objectReferenceValue = null;
        serialized.FindProperty("sendStartBattleMessage").boolValue = false;
        serialized.FindProperty("requireVocabularyBeforeCombat").boolValue = true;
        serialized.FindProperty("vocabularyEncounterFlow").objectReferenceValue = flow;
        serialized.FindProperty("startCombatIfVocabularyFlowFails").boolValue = false;
        serialized.FindProperty("configureTileSpawner").boolValue = false;
        serialized.FindProperty("arenaBoundary").objectReferenceValue = null;
        serialized.FindProperty("lockedMessage").stringValue =
            "[ERODE] Locked until Node 2 is complete.";
        serialized.FindProperty("vocabularyStartedMessage").stringValue =
            "[ERODE] Teach started.";
        serialized.FindProperty("vocabularyCompletedMessage").stringValue =
            "[ERODE] Target Challenge completed; opening the path to Node 3.";
        serialized.FindProperty("vocabularyFailedMessage").stringValue =
            "[ERODE] Encounter setup failed; trigger reset for retry.";
        serialized.ApplyModifiedProperties();
    }
}
#endif
