using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MinigamesSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/MinigamesScene.unity";
    private const string ModeSelectScenePath = "Assets/Scenes/MinigameModeSelectScene.unity";
    private const string WordMasterScenePath = "Assets/Scenes/WordMasterScene.unity";
    private const string SpellItOutScenePath = "Assets/Scenes/SpellItOutScene.unity";
    private const string WordExcavationScenePath = "Assets/Scenes/WordExcavationScene.unity";
    private const string MainMenuScenePath = "Assets/Scenes/Main Menu Scene.unity";
    private const string DictionaryPrefabPath = "Assets/Prefabs/DictionaryManager.prefab";
    private const string MinigamesSceneName = "MinigamesScene";
    private const string ModeSelectSceneName = "MinigameModeSelectScene";
    private const string WordMasterSceneName = "WordMasterScene";
    private const string SpellItOutSceneName = "SpellItOutScene";
    private const string WordExcavationSceneName = "WordExcavationScene";

    [MenuItem("SPELLSTRIKE/Minigames/Build Minigames Scene")]
    public static void BuildMinigamesScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = MinigamesSceneName;

        CreateCamera();
        CreateLighting();
        CreateEventSystem();
        CreateDictionaryManager();
        CreateCanvasController();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EnsureSceneInBuildSettings(ScenePath);
        WireMainMenuMinigamesButton();

        EditorSceneManager.OpenScene(ScenePath);
        Debug.Log("[Minigames] Built MinigamesScene and wired Main Menu minigames button.");
    }

    [MenuItem("SPELLSTRIKE/Minigames/Build Split Minigame Scenes")]
    public static void BuildSplitMinigameScenes()
    {
        BuildConfiguredMinigameScene(ModeSelectScenePath, ModeSelectSceneName, MinigameSceneStartup.ModeSelect);
        BuildConfiguredMinigameScene(WordMasterScenePath, WordMasterSceneName, MinigameSceneStartup.WordMaster);
        BuildConfiguredMinigameScene(SpellItOutScenePath, SpellItOutSceneName, MinigameSceneStartup.SpellItOut);
        BuildConfiguredMinigameScene(WordExcavationScenePath, WordExcavationSceneName, MinigameSceneStartup.WordExcavation);

        EnsureSceneInBuildSettings(ModeSelectScenePath);
        EnsureSceneInBuildSettings(WordMasterScenePath);
        EnsureSceneInBuildSettings(SpellItOutScenePath);
        EnsureSceneInBuildSettings(WordExcavationScenePath);
        WireMainMenuMinigamesButton();

        EditorSceneManager.OpenScene(ModeSelectScenePath);
        Debug.Log("[Minigames] Built split minigame scenes and wired Main Menu to MinigameModeSelectScene.");
    }

    [MenuItem("SPELLSTRIKE/Minigames/Rebuild Editable UI In Open Scene")]
    public static void RebuildEditableUIInOpenScene()
    {
        MinigamesSceneController controller = Object.FindFirstObjectByType<MinigamesSceneController>();
        if (controller == null)
        {
            Debug.LogWarning("[Minigames] No MinigamesSceneController found in the open scene.");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(controller.gameObject, "Rebuild Editable Minigames UI");
        controller.RebuildEditableUI();
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        Debug.Log("[Minigames] Rebuilt editable minigames UI in the open scene.");
    }

    [MenuItem("SPELLSTRIKE/Minigames/Rebuild Word Master Editable UI")]
    public static void RebuildWordMasterEditableUI()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene scene = EditorSceneManager.OpenScene(WordMasterScenePath, OpenSceneMode.Single);
        MinigamesSceneController controller = Object.FindFirstObjectByType<MinigamesSceneController>();
        if (controller == null)
        {
            Debug.LogWarning("[Minigames] No MinigamesSceneController found in WordMasterScene.");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(controller.gameObject, "Rebuild Word Master Editable UI");
        controller.startupScene = MinigameSceneStartup.WordMaster;

        if (controller.transform.Find("MinigamesUI") == null)
            controller.RebuildEditableUI();

        controller.ShowWordMasterEditPreview();
        controller.ApplySceneIsolation();
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Minigames] Rebuilt Word Master editable UI and saved WordMasterScene.");
    }

    [MenuItem("SPELLSTRIKE/Minigames/Rebuild Word Excavation Editable UI")]
    public static void RebuildWordExcavationEditableUI()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene scene = EditorSceneManager.OpenScene(WordExcavationScenePath, OpenSceneMode.Single);
        MinigamesSceneController controller = Object.FindFirstObjectByType<MinigamesSceneController>();
        if (controller == null)
        {
            Debug.LogWarning("[Minigames] No MinigamesSceneController found in WordExcavationScene.");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(controller.gameObject, "Rebuild Word Excavation Editable UI");
        controller.startupScene = MinigameSceneStartup.WordExcavation;

        if (controller.transform.Find("MinigamesUI") == null)
            controller.RebuildEditableUI();

        controller.ShowWordExcavationEditPreview();
        controller.ApplySceneIsolation();
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Minigames] Rebuilt Word Excavation editable UI and saved WordExcavationScene.");
    }

    [MenuItem("SPELLSTRIKE/Minigames/Wire Main Menu Minigames Button")]
    public static void WireMainMenuMinigamesButton()
    {
        Scene scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        Button button = FindMinigamesButton();

        if (button == null)
        {
            Debug.LogWarning("[Minigames] Could not find a Main Menu button named Minigames or Arena.");
            return;
        }

        AdventureButtonHandler handler = Object.FindFirstObjectByType<AdventureButtonHandler>();
        if (handler == null)
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            GameObject target = canvas != null ? canvas.gameObject : new GameObject("SceneNavigation");
            handler = target.AddComponent<AdventureButtonHandler>();
        }

        ClearPersistentListeners(button);
        UnityEventTools.AddStringPersistentListener(button.onClick, handler.LoadScene, ModeSelectSceneName);

        EditorUtility.SetDirty(button);
        EditorUtility.SetDirty(handler);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        EnsureSceneInBuildSettings(ModeSelectScenePath);
        Debug.Log($"[Minigames] Wired {button.name} button to load {ModeSelectSceneName}.");
    }

    private static void BuildConfiguredMinigameScene(string path, string sceneName, MinigameSceneStartup startupScene)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = sceneName;

        CreateCamera();
        CreateLighting();
        CreateEventSystem();
        CreateDictionaryManager();
        CreateCanvasController(startupScene);

        EditorSceneManager.SaveScene(scene, path);
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.08f, 0.035f, 0.015f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
    }

    private static void CreateLighting()
    {
        GameObject lightObject = new GameObject("Directional Light", typeof(Light));
        Light light = lightObject.GetComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.7f;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    private static void CreateEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static void CreateDictionaryManager()
    {
        GameObject dictionaryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DictionaryPrefabPath);
        if (dictionaryPrefab == null)
        {
            Debug.LogWarning("[Minigames] DictionaryManager prefab missing. Minigames will use fallback practice words.");
            return;
        }

        GameObject dictionary = (GameObject)PrefabUtility.InstantiatePrefab(dictionaryPrefab);
        dictionary.name = "DictionaryManager";
    }

    private static void CreateCanvasController()
    {
        CreateCanvasController(MinigameSceneStartup.LegacyAuto);
    }

    private static void CreateCanvasController(MinigameSceneStartup startupScene)
    {
        GameObject canvasObject = new GameObject("MinigamesCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(MinigamesSceneController));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        MinigamesSceneController controller = canvasObject.GetComponent<MinigamesSceneController>();
        controller.startupScene = startupScene;
        controller.modeSelectSceneName = ModeSelectSceneName;
        controller.wordMasterSceneName = WordMasterSceneName;
        controller.spellItOutSceneName = SpellItOutSceneName;
        controller.wordExcavationSceneName = WordExcavationSceneName;
        controller.RebuildEditableUI();

        switch (startupScene)
        {
            case MinigameSceneStartup.ModeSelect:
                controller.ShowModeSelection();
                break;
            case MinigameSceneStartup.WordMaster:
                controller.ShowWordMasterEditPreview();
                break;
            case MinigameSceneStartup.SpellItOut:
                controller.SelectMode(MinigameMode.SpellItOut);
                break;
            case MinigameSceneStartup.WordExcavation:
                controller.ShowWordExcavationEditPreview();
                break;
        }

        controller.ApplySceneIsolation();
        EditorUtility.SetDirty(controller);
    }

    private static Button FindMinigamesButton()
    {
        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
        foreach (Button button in buttons)
        {
            if (IsMinigamesName(button.name))
                return button;
        }

        foreach (Button button in buttons)
        {
            string label = GetButtonLabel(button);
            if (IsMinigamesName(label))
                return button;
        }

        return null;
    }

    private static bool IsMinigamesName(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        string normalized = text.Replace(" ", string.Empty).ToLowerInvariant();
        return normalized.Contains("minigame")
            || normalized.Contains("arena");
    }

    private static string GetButtonLabel(Button button)
    {
        TextMeshProUGUI tmp = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null)
            return tmp.text;

        Text legacyText = button.GetComponentInChildren<Text>(true);
        return legacyText != null ? legacyText.text : string.Empty;
    }

    private static void ClearPersistentListeners(Button button)
    {
        int count = button.onClick.GetPersistentEventCount();
        for (int i = count - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(button.onClick, i);
    }

    private static void EnsureSceneInBuildSettings(string scenePath)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (EditorBuildSettingsScene scene in scenes)
        {
            if (scene.path == scenePath)
            {
                scene.enabled = true;
                EditorBuildSettings.scenes = scenes.ToArray();
                return;
            }
        }

        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
