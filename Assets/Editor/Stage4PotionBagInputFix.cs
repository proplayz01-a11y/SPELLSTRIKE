// Fix ONLY the Stage 4 scene's broken InputSystemUIInputModule action
// references. The old asset GUID is absent, so UGUI buttons cannot receive
// clicks in direct Stage4 startup. Existing shared project input actions are
// used instead. No player movement, save, joystick or gameplay changes.
#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

internal static class Stage4PotionBagInputFix
{
    private const string ScenePath = "Assets/Scenes/Stage4Scene/Stage4Scene.unity";
    private const string ActionsPath = "Assets/InputSystem_Actions.inputactions";
    private const string Backup = "Library/PresentationUIProductionPromotion_20261020_2352/Stage4_Before_BagInputActionFix.unity";
    private const string MissingLegacyGuid = "ca9f5fa95ffab41fb9a615ab714db018";

    [MenuItem("SPELLSTRIKE/HUD & Potion/Diagnose Stage 4 Input Actions (Read Only)")]
    private static void Diagnose()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
        {
            Debug.LogWarning("[Stage4 Input Diagnostic] Open the production Stage4Scene first.");
            return;
        }
        var modules = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<InputSystemUIInputModule>(true))
            .ToArray();
        var target = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
        var references = AssetDatabase.LoadAllAssetsAtPath(ActionsPath)
            .OfType<InputActionReference>().Where(item => item != null && item.action != null)
            .ToArray();
        Debug.Log("[Stage4 Input Diagnostic] missingLegacyAssetPath='" +
            AssetDatabase.GUIDToAssetPath(MissingLegacyGuid) +
            "' moduleCount=" + modules.Length +
            " currentModuleAsset=" + (modules.Length == 1 && modules[0].actionsAsset != null
                ? modules[0].actionsAsset.name : "(null)") +
            " canonicalAsset=" + (target != null ? target.name : "(null)") +
            " canonicalUIDefinitions=" + (target?.FindActionMap("UI")?.actions.Count ?? 0) +
            " savedActionReferences=" + references.Length + " actions=" +
            string.Join(", ", references.Select(item => item.action.actionMap.name + "/" + item.action.name)));
    }

    [MenuItem("SPELLSTRIKE/HUD & Potion/Stage 4 Bag Raycast Report (Read Only, Play Mode)")]
    private static void ReportBagRaycast()
    {
        if (!EditorApplication.isPlaying || EventSystem.current == null)
        {
            Debug.LogWarning("[Stage4 Bag Raycast] Requires Stage4 Play Mode.");
            return;
        }

        Button[] bagButtons = UnityEngine.Object.FindObjectsByType<Button>(
            FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(button => button.name == "PotionBagButton").ToArray();
        if (bagButtons.Length != 1)
        {
            Debug.LogError("[Stage4 Bag Raycast] Expected ONE Bag Button; found " + bagButtons.Length);
            return;
        }

        Button bag = bagButtons[0];
        RectTransform rect = bag.GetComponent<RectTransform>();
        Canvas parentCanvas = bag.GetComponentInParent<Canvas>(true);
        Canvas.ForceUpdateCanvases();
        Vector2 position = RectTransformUtility.WorldToScreenPoint(
            parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? parentCanvas.worldCamera : null, rect.TransformPoint(rect.rect.center));
        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = position
        };
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        var modules = UnityEngine.Object.FindObjectsByType<InputSystemUIInputModule>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        var module = EventSystem.current.currentInputModule as InputSystemUIInputModule;
        string input = module == null ? "activeModule=" +
            EventSystem.current.currentInputModule?.GetType().Name :
            "activeModule=InputSystemUIInputModule inputAsset=" +
            (module.actionsAsset != null ? module.actionsAsset.name : "null") +
            " point=" + (module.point?.action?.enabled.ToString() ?? "null") +
            " click=" + (module.leftClick?.action?.enabled.ToString() ?? "null");
        Debug.Log("[Stage4 Bag Raycast] active="+bag.gameObject.activeInHierarchy +
            " interactable="+bag.interactable +
            " canvas="+(parentCanvas != null ? parentCanvas.name : "null")+
            " graphicRaycaster="+(parentCanvas != null && parentCanvas.GetComponent<GraphicRaycaster>() != null)+
            " rectScreenCenter="+position+" screen="+Screen.width+"x"+Screen.height+
            " eventSystemCount="+UnityEngine.Object.FindObjectsByType<EventSystem>(
                FindObjectsInactive.Include, FindObjectsSortMode.None).Length+
            " inputModuleCount="+modules.Length+" "+input+
            " hits="+string.Join(" > ", results.Take(15)
                .Select(hit=>hit.gameObject.name+"("+hit.module.GetType().Name+")")));
    }

    [MenuItem("SPELLSTRIKE/HUD & Potion/Fix Stage 4 Broken UI Input Actions (Official)")]
    private static void Apply()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            !scene.IsValid() || !scene.isLoaded || scene.path != ScenePath || scene.isDirty)
        {
            Debug.LogError("[Stage4 Potion Bag Input] Open and SAVE actual Stage4Scene in Edit Mode.");
            return;
        }

        try
        {
            if (File.Exists(Backup))
                throw new InvalidOperationException("Stage4 UI input repair already attempted; refusing repeat.");
            var eventSystems = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<EventSystem>(true)).ToArray();
            if (eventSystems.Length != 1)
                throw new InvalidOperationException("Expected exactly one Stage4 EventSystem.");
            InputSystemUIInputModule module = eventSystems[0].GetComponent<InputSystemUIInputModule>();
            if (module == null)
                throw new InvalidOperationException("Stage4 InputSystemUIInputModule not found.");
            if (module.actionsAsset != null)
                throw new InvalidOperationException("Stage4 UI actions are already bound. Refusing to overwrite.");

            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
            if (actions == null || actions.FindActionMap("UI") == null)
                throw new InvalidOperationException("Shared InputSystem_Actions/UI map is missing.");
            string[] wanted =
            {
                "Point","Navigate","Submit","Cancel","Click","MiddleClick",
                "RightClick","ScrollWheel","TrackedDevicePosition","TrackedDeviceOrientation"
            };
            var candidates = AssetDatabase.LoadAllAssetsAtPath(ActionsPath)
                .OfType<InputActionReference>()
                .Where(reference => reference != null && reference.action != null &&
                    reference.action.actionMap != null && reference.action.actionMap.name == "UI")
                .GroupBy(reference => reference.action.name)
                .ToDictionary(group => group.Key, group => group.First());
            foreach (string actionName in wanted)
                if (actions.FindAction("UI/" + actionName) == null || !candidates.ContainsKey(actionName))
                    throw new InvalidOperationException("Missing saved UI InputActionReference subasset: " + actionName);

            Directory.CreateDirectory(Path.GetDirectoryName(Backup));
            File.Copy(ScenePath, Backup, overwrite:false);
            var serialized = new SerializedObject(module);
            serialized.FindProperty("m_ActionsAsset").objectReferenceValue = actions;
            string[] fields =
            {
                "m_PointAction","m_MoveAction","m_SubmitAction","m_CancelAction",
                "m_LeftClickAction","m_MiddleClickAction","m_RightClickAction",
                "m_ScrollWheelAction","m_TrackedDevicePositionAction","m_TrackedDeviceOrientationAction"
            };
            for (int i = 0; i < wanted.Length; ++i)
            {
                SerializedProperty property = serialized.FindProperty(fields[i]);
                if (property == null)
                    throw new InvalidOperationException("Missing InputSystemUIInputModule serialized field: " + fields[i]);
                property.objectReferenceValue = candidates[wanted[i]];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new IOException("Could not save repaired Stage4Scene.");
            Debug.Log("[Stage4 Potion Bag Input] PASS: Actual Stage4Scene InputSystemUIInputModule now uses the project's existing UI action map with ten valid serialized InputActionReference subassets. No player/joystick/gameplay/scene-GUID changes. Backup: " + Backup);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError("[Stage4 Potion Bag Input] Stopped; inspect backup before saving any potentially dirty scene.");
        }
    }
}
#endif
