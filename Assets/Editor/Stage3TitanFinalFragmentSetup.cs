using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// One-time production Stage3 Node4 finishing flow:
///   defeat Titan -> reveal/unlock violet fragment at the arena centre ->
///   real Player E/mobile COLLECT -> mark Node4/stage complete ->
///   Stage 3 completion panel / save / continue.
/// Reuses Node1's actual Stage3 mesh/pickup and Stage2's actual completion UI.
/// Does not touch scenes from other stages or any authored Titan animation.
/// </summary>
public static class Stage3TitanFinalFragmentSetup
{
    private const string Stage3ScenePath = "Assets/Scenes/Stage3Scene/Stage3Scene.unity";
    private const string Stage2ScenePath = "Assets/Scenes/Stage2Scene/Stage2Scene.unity";
    private const string FragmentName = "Stage3_Node4_VioletFragment";
    private const string CanvasName = "Stage3CompleteCanvas";

    [MenuItem("SPELLSTRIKE/Stage 3/Finish Titan: Install Boss Fragment and Stage Complete")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            Debug.LogError("[STAGE3 FINISH] Edit Mode only, after compilation/import.");
            return;
        }
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != Stage3ScenePath)
        {
            Debug.LogError("[STAGE3 FINISH] Refused: active scene must be production Stage3Scene.");
            return;
        }
        var nodes = scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<StageNodeController>(true)).ToArray();
        StageNodeController node4 = nodes.SingleOrDefault(n => n.gameObject.name == "Node 4");
        var node1Fragment = scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<FragmentPickup>(true))
            .SingleOrDefault(f => f.name == "Stage3_Node1_VioletFragment");
        var node4Trigger = node4 ? node4.GetComponent<StageNodeTrigger>() : null;
        if (!node4 || !node4Trigger || !node1Fragment ||
            node1Fragment.GetComponent<MeshFilter>() == null ||
            node1Fragment.GetComponent<MeshRenderer>() == null)
        {
            Debug.LogError("[STAGE3 FINISH] Node4, its trigger or genuine Node1 fragment missing.");
            return;
        }
        var triggerSO = new SerializedObject(node4Trigger);
        var center = triggerSO.FindProperty("arenaCenter").objectReferenceValue as Transform;
        if (!center || !Physics.Raycast(center.position + Vector3.up * 300f,
                Vector3.down, out RaycastHit hit, 600f, ~0, QueryTriggerInteraction.Ignore) ||
            hit.collider.name != "Boss_Stage")
        {
            Debug.LogError("[STAGE3 FINISH] Boss_Stage centre ground raycast failed. Refusing floating fragment.");
            return;
        }

        var sceneFragments = scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<FragmentPickup>(true)).ToArray();
        bool fragmentExists = sceneFragments.Any(f => f.name == FragmentName);
        var existingCanvases = scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<StageCompleteUI>(true)).ToArray();
        if (existingCanvases.Length > 1)
        {
            Debug.LogError("[STAGE3 FINISH] Multiple StageCompleteUI controllers, refuse ambiguous setup.");
            return;
        }

        // Verify the donor canvas BEFORE changing this scene. Open additively,
        // clone its complete subtree, then close Stage2 without ever saving it.
        GameObject canvasClone = null;
        if (existingCanvases.Length == 0)
        {
            Scene donor = EditorSceneManager.OpenScene(Stage2ScenePath, OpenSceneMode.Additive);
            try
            {
                var donorUIs = donor.GetRootGameObjects()
                    .SelectMany(r => r.GetComponentsInChildren<StageCompleteUI>(true)).ToArray();
                if (donorUIs.Length != 1 ||
                    !donorUIs[0].stageCompletePanel || !donorUIs[0].dimOverlay ||
                    donorUIs[0].GetComponentsInChildren<Button>(true).Length == 0)
                {
                    Debug.LogError("[STAGE3 FINISH] Stage2 donor completion UI invalid.");
                    return;
                }
                canvasClone = UnityEngine.Object.Instantiate(donorUIs[0].gameObject);
                canvasClone.name = CanvasName;
                canvasClone.transform.SetParent(null, false);
                SceneManager.MoveGameObjectToScene(canvasClone, scene);
                canvasClone.transform.localPosition = Vector3.zero;
                canvasClone.transform.localRotation = Quaternion.identity;
                canvasClone.transform.localScale = Vector3.one;
                canvasClone.SetActive(true);
            }
            finally
            {
                EditorSceneManager.CloseScene(donor, true);
                SceneManager.SetActiveScene(scene);
            }
        }

        // Existing scene is known to contain pre-existing unsaved work.
        // Keep an exact backup of the current on-disk asset before first save;
        // do not replace original maps, Titan FBX/animations, or other scenes.
        string physicalScene = Path.GetFullPath(Stage3ScenePath);
        string backup = Path.Combine("Library", "Stage3Scene_BeforeNode4Fragment_20261014.unity");
        if (!File.Exists(backup))
            File.Copy(physicalScene, backup);

        StageCompleteUI stageUI = canvasClone
            ? canvasClone.GetComponent<StageCompleteUI>()
            : existingCanvases[0];
        if (!stageUI || !stageUI.stageCompletePanel || !stageUI.dimOverlay)
        {
            Debug.LogError("[STAGE3 FINISH] Completion canvas invalid; scene has not been saved.");
            return;
        }
        var uiSO = new SerializedObject(stageUI);
        uiSO.FindProperty("completedStageNumber").intValue = 3;
        // StageSelect already contains the reusable vocabulary review gate.
        uiSO.FindProperty("requireVocabularyReview").boolValue = true;
        uiSO.FindProperty("presentVocabularyReviewInStageSelectScene").boolValue = true;
        uiSO.ApplyModifiedPropertiesWithoutUndo();
        var continueButtons = stageUI.GetComponentsInChildren<Button>(true)
            .Where(b => b.name == "ContinueButton").ToArray();
        if (continueButtons.Length != 1)
        {
            Debug.LogError("[STAGE3 FINISH] Expected one ContinueButton; no scene save.");
            return;
        }
        Button button = continueButtons[0];
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(button.onClick, i);
        UnityEventTools.AddPersistentListener(button.onClick, stageUI.OnContinuePressed);
        EditorUtility.SetDirty(button);
        EditorUtility.SetDirty(stageUI);

        FragmentPickup pickup;
        GameObject fragmentObject;
        if (!fragmentExists)
        {
            Transform originalRoot = node1Fragment.transform.parent;
            fragmentObject = UnityEngine.Object.Instantiate(node1Fragment.gameObject, originalRoot);
            fragmentObject.name = FragmentName;
        }
        else
        {
            pickup = sceneFragments.Single(f => f.name == FragmentName);
            fragmentObject = pickup.gameObject;
        }
        pickup = fragmentObject.GetComponent<FragmentPickup>();
        var meshFilter = fragmentObject.GetComponent<MeshFilter>();
        var collider = fragmentObject.GetComponent<SphereCollider>();
        var mesh = meshFilter ? meshFilter.sharedMesh : null;
        if (!pickup || !collider || !mesh)
        {
            Debug.LogError("[STAGE3 FINISH] Fragment mesh, trigger or receiver component missing.");
            return;
        }
        fragmentObject.transform.rotation = Quaternion.identity;
        fragmentObject.transform.localScale = Vector3.one * 1.61f;
        // Align visual mesh (whose imported origin is offset) over the exact
        // centre of the authored Boss_Stage surface.
        Vector3 visualSize = Vector3.Scale(mesh.bounds.size,
            fragmentObject.transform.lossyScale);
        Vector3 targetMeshCenter = hit.point + Vector3.up * (visualSize.y * .5f + .12f);
        Vector3 meshLocalOffset = Vector3.Scale(mesh.bounds.center,
            fragmentObject.transform.lossyScale);
        fragmentObject.transform.position = targetMeshCenter - meshLocalOffset;
        collider.isTrigger = true;
        collider.center = mesh.bounds.center;
        collider.radius = Mathf.Max(mesh.bounds.extents.x,
            Mathf.Max(mesh.bounds.extents.y, mesh.bounds.extents.z)) + .48f;
        pickup.pickupReceiver = node4;
        pickup.pickupMessage = "OnFragmentCollected";
        fragmentObject.SetActive(false);
        EditorUtility.SetDirty(fragmentObject);
        EditorUtility.SetDirty(pickup);
        EditorUtility.SetDirty(collider);

        var nodeSO = new SerializedObject(node4);
        nodeSO.FindProperty("fragmentPickup").objectReferenceValue = pickup;
        nodeSO.FindProperty("fragmentVisual").objectReferenceValue = fragmentObject;
        nodeSO.FindProperty("lockFragmentAtStart").boolValue = true;
        nodeSO.FindProperty("unlockFragmentOnEnemyDefeat").boolValue = true;
        nodeSO.FindProperty("completeNodeOnEnemyDefeat").boolValue = false;
        nodeSO.FindProperty("hideFragmentVisualOnComplete").boolValue = true;
        nodeSO.FindProperty("fragmentGoalIncrement").intValue = 1;
        nodeSO.FindProperty("stageCompleteUI").objectReferenceValue = stageUI;
        nodeSO.FindProperty("showStageCompleteOnNodeComplete").boolValue = true;
        nodeSO.ApplyModifiedPropertiesWithoutUndo();

        var field = typeof(StageNodeController).GetField("onEnemyDefeated",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var revealEvent = field != null ? field.GetValue(node4) as UnityEvent : null;
        if (revealEvent == null)
        {
            Debug.LogError("[STAGE3 FINISH] Node4 onEnemyDefeated UnityEvent inaccessible.");
            return;
        }
        for (int i = revealEvent.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            if (revealEvent.GetPersistentTarget(i) == fragmentObject)
                UnityEventTools.RemovePersistentListener(revealEvent, i);
        }
        UnityEventTools.AddBoolPersistentListener(revealEvent, fragmentObject.SetActive, true);
        EditorUtility.SetDirty(node4);
        EditorSceneManager.MarkSceneDirty(scene);

        if (!EditorSceneManager.SaveScene(scene))
        {
            Debug.LogError("[STAGE3 FINISH] Failed to save production scene; see Unity Console.");
            return;
        }
        Debug.Log("[STAGE3 FINISH] PASS: Node4 centre fragment cloned from actual Stage3 Node1, " +
            "spawns after authentic Titan death, requires player E/mobile COLLECT, " +
            "then Node4 completion shows real Stage3 completion UI and records Stage 3 progress. " +
            "Boss_Stage contact=" + hit.point + ", fragment visual center=" + targetMeshCenter +
            ". Source Stage2 unchanged. Scene backup=" + backup);
    }
}
