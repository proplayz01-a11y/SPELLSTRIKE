using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MainMenuAdventureWireEditor
{
    private const string TargetSceneName = "StageSelectScene";
    private const string MainMenuScenePath = "Assets/Scenes/Main Menu Scene.unity";

    private static readonly string[] MainMenuPrefabPaths =
    {
        "Assets/Prefabs/Canvas.prefab",
        "Assets/Sprites/Main Menu/Canvas.prefab"
    };

    [MenuItem("SPELLSTRIKE/UI/Wire Main Menu Adventure To Stage Select")]
    public static void WireAdventureButtons()
    {
        Scene scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        int sceneButtons = WireAdventureButtonsInRoot(Object.FindFirstObjectByType<Canvas>()?.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        int prefabButtons = 0;
        foreach (string prefabPath in MainMenuPrefabPaths)
            prefabButtons += WirePrefab(prefabPath);

        AssetDatabase.SaveAssets();
        Debug.Log($"[MainMenu] Wired Adventure buttons to {TargetSceneName}. Scene buttons: {sceneButtons}. Prefab buttons: {prefabButtons}.");
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
            UnityEventTools.AddStringPersistentListener(button.onClick, handler.LoadScene, TargetSceneName);
            EditorUtility.SetDirty(button);
            wiredCount++;
        }

        EditorUtility.SetDirty(handler);
        return wiredCount;
    }
}
