using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MainMenuAdventureWireEditor
{
    private const string MainMenuScenePath = "Assets/Scenes/Main Menu Scene.unity";

    private static readonly string[] MainMenuPrefabPaths =
    {
        "Assets/Prefabs/Canvas.prefab",
        "Assets/Sprites/Main Menu/Canvas.prefab"
    };

    [MenuItem("SPELLSTRIKE/UI/Wire Main Menu Adventure First-Run Router")]
    public static void WireAdventureButtons()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[MainMenu] Stop Play Mode before wiring Adventure routing.");
            return;
        }

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene openScene = SceneManager.GetSceneAt(i);
            if (openScene.isDirty)
            {
                Debug.LogError(
                    $"[MainMenu] Refusing to wire Adventure routing while '{openScene.name}' has unsaved changes. " +
                    "Save or discard those changes explicitly first.");
                return;
            }
        }

        Scene scene = SceneManager.GetSceneByPath(MainMenuScenePath);
        bool openedForSetup = !scene.IsValid() || !scene.isLoaded;
        if (openedForSetup)
            scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Additive);

        int sceneButtons = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
            sceneButtons += WireAdventureButtonsInRoot(root);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (openedForSetup)
            EditorSceneManager.CloseScene(scene, true);

        int prefabButtons = 0;
        foreach (string prefabPath in MainMenuPrefabPaths)
            prefabButtons += WirePrefab(prefabPath);

        AssetDatabase.SaveAssets();
        Debug.Log(
            $"[MainMenu] Wired Adventure buttons to the first-run router. " +
            $"Scene buttons: {sceneButtons}. Prefab buttons: {prefabButtons}.");
    }

    private static int WirePrefab(string prefabPath)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        int wiredCount = WireAdventureButtonsInRoot(root);
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        return wiredCount;
    }

    private static int WireAdventureButtonsInRoot(GameObject root)
    {
        if (root == null)
            return 0;

        AdventureButtonHandler handler = root.GetComponentInChildren<AdventureButtonHandler>(true);
        if (handler == null)
            handler = root.AddComponent<AdventureButtonHandler>();

        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        int wiredCount = 0;

        foreach (Button button in buttons)
        {
            if (!button.name.ToLowerInvariant().Contains("adventure"))
                continue;

            button.onClick = new Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(button.onClick, handler.LoadAdventure);
            EditorUtility.SetDirty(button);
            wiredCount++;
        }

        EditorUtility.SetDirty(handler);
        return wiredCount;
    }
}
