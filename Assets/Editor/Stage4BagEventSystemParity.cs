// Safely make Stage4 use the same UI click input module proven in Stage2/3.
// This is a scene-only, reversible Editor operation, not a player movement/
// touch-orbit or gameplay-script change. Preserve the old component disabled.
#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

internal static class Stage4BagEventSystemParity
{
    private const string ScenePath = "Assets/Scenes/Stage4Scene/Stage4Scene.unity";
    private const string BackupPath =
        "Library/GameplayHudEditModeParity_20261021/Stage4_before_StandaloneUIInput_20261021.unity";

    [MenuItem("SPELLSTRIKE/HUD & Potion/Stage 4 Use Stage 2-3 Working UI Click Module (Official)")]
    private static void Apply()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || !scene.IsValid() ||
            !scene.isLoaded || scene.path != ScenePath || scene.isDirty ||
            SceneManager.sceneCount != 1 || File.Exists(BackupPath))
        {
            Debug.LogError("[Stage4 Bag Click] Requires saved production Stage4Scene in Edit Mode, with fresh backup.");
            return;
        }
        try
        {
            EventSystem[] events = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<EventSystem>(true))
                .ToArray();
            if (events.Length != 1)
                throw new InvalidOperationException("Stage4 must have exactly one EventSystem.");
            EventSystem eventSystem = events[0];
            InputSystemUIInputModule oldModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (oldModule == null || !oldModule.enabled ||
                eventSystem.GetComponent<StandaloneInputModule>() != null)
                throw new InvalidOperationException("Unexpected UI module setup; no replacement made.");
            if (eventSystem.GetComponents<BaseInputModule>().Length != 1)
                throw new InvalidOperationException("Expected exactly one input module before change.");
            string previousSceneGuid = AssetDatabase.AssetPathToGUID(ScenePath);

            Directory.CreateDirectory(Path.GetDirectoryName(BackupPath));
            File.Copy(ScenePath, BackupPath, false);
            oldModule.enabled = false;
            StandaloneInputModule workingModule = eventSystem.gameObject.AddComponent<StandaloneInputModule>();
            if (workingModule == null || !workingModule.enabled)
                throw new InvalidOperationException("Cannot create the Stage2/3-compatible input module.");
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new IOException("Cannot save production Stage4Scene.");
            if (AssetDatabase.AssetPathToGUID(ScenePath) != previousSceneGuid)
                throw new InvalidOperationException("Scene GUID changed unexpectedly.");
            Debug.Log("[Stage4 Bag Click] PASS: Stage4 now uses the same StandaloneInputModule as Stage2/3. Original InputSystemUIInputModule retained disabled for reversible rollback. No HUD art, actor input, joystick, save or progression changes. Exact scene backup: " + BackupPath);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError("[Stage4 Bag Click] Stopped. Inspect production scene and exact backup; do not rerun blindly.");
        }
    }
}
#endif
