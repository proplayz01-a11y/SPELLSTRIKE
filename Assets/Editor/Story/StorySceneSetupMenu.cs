using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class StorySceneSetupMenu
{
    private const int PageCount = 28;
    private const string ScenePath = "Assets/Scenes/StoryScene.unity";
    private const string TutorialScenePath = "Assets/Scenes/TutorialScene.unity";
    private const string ProfilePath = "Assets/Data/Story/Prologue_Sequence.asset";
    private const string ArtworkFolder = "Assets/Story/Pages";

    [MenuItem("SPELLSTRIKE/Story/Create 28-Page Prologue StoryScene")]
    public static void CreateProductionStoryScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.isDirty)
        {
            EditorUtility.DisplayDialog("Dirty scene protected",
                "The active scene has unsaved changes. Open a clean/empty scene first; this setup will not save over or discard your work.", "OK");
            return;
        }

        if (!ConfigureArtworkImporters(out Sprite[] artwork))
            return;

        EnsureFolder("Assets/Data");
        EnsureFolder("Assets/Data/Story");
        StorySequenceProfile profile = CreateOrUpdateProfile(artwork);
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Canvas canvas = CreateCanvas();
        Image background = Rect("Background", canvas.transform, Vector2.zero, Vector2.one).AddComponent<Image>();
        background.color = new Color(0.008f, 0.012f, 0.025f, 1f);

        GameObject safeArea = Rect("SafeArea", canvas.transform, Vector2.zero, Vector2.one);
        safeArea.AddComponent<StorySafeArea>();
        GameObject presentation = Rect("PagePresentation", safeArea.transform, Vector2.zero, Vector2.one);
        CanvasGroup pageGroup = presentation.AddComponent<CanvasGroup>();

        Image pageImage = Rect("PageImage", presentation.transform, Vector2.zero, Vector2.one).AddComponent<Image>();
        pageImage.sprite = artwork[0];
        pageImage.preserveAspect = true;
        pageImage.raycastTarget = false;

        TMP_Text fallback = Text("MissingArtworkFallback", presentation.transform,
            "Story page unavailable", 42, TextAlignmentOptions.Center);
        fallback.gameObject.SetActive(false);

        TMP_Text progress = Text("PageProgress", safeArea.transform, $"01 / {PageCount:00}", 24, TextAlignmentOptions.Center);
        SetRect(progress.rectTransform, new Vector2(0.45f, 0.92f), new Vector2(0.55f, 0.98f));

        Button previous = InvisibleNavigationButton(
            "PreviousPageHitArea", safeArea.transform,
            new Vector2(0f, 0f), new Vector2(0.5f, 1f));
        Button next = InvisibleNavigationButton(
            "NextPageHitArea", safeArea.transform,
            new Vector2(0.5f, 0f), new Vector2(1f, 1f));
        Button skip = ButtonWithLabel("SkipButton", safeArea.transform, "SKIP", new Vector2(0.86f, 0.91f), new Vector2(0.97f, 0.98f));

        GameObject inputBlocker = Rect("InputBlocker", safeArea.transform, Vector2.zero, Vector2.one);
        Image blockerImage = inputBlocker.AddComponent<Image>();
        blockerImage.color = Color.clear;
        inputBlocker.SetActive(false);

        GameObject confirmation = Rect("SkipConfirmation", safeArea.transform, Vector2.zero, Vector2.one);
        Image confirmationShade = confirmation.AddComponent<Image>();
        confirmationShade.color = new Color(0f, 0f, 0f, 0.78f);
        GameObject dialog = Rect("Dialog", confirmation.transform, new Vector2(0.29f, 0.34f), new Vector2(0.71f, 0.66f));
        AddShade(dialog, new Color(0.025f, 0.04f, 0.075f, 0.98f));
        TMP_Text prompt = Text("Prompt", dialog.transform, "SKIP THE PROLOGUE?\nYour progress will be recorded.", 34, TextAlignmentOptions.Center);
        SetRect(prompt.rectTransform, new Vector2(0.08f, 0.38f), new Vector2(0.92f, 0.88f));
        Button cancel = ButtonWithLabel("CancelButton", dialog.transform, "CANCEL", new Vector2(0.08f, 0.1f), new Vector2(0.45f, 0.33f));
        Button confirm = ButtonWithLabel("ConfirmButton", dialog.transform, "SKIP", new Vector2(0.55f, 0.1f), new Vector2(0.92f, 0.33f));
        confirmation.SetActive(false);

        GameObject controllerObject = new GameObject("StorySequenceController", typeof(AudioSource));
        AudioSource audioSource = controllerObject.GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        StorySequenceController controller = controllerObject.AddComponent<StorySequenceController>();
        SerializedObject serializedController = new SerializedObject(controller);
        Set(serializedController, "profile", profile);
        Set(serializedController, "pageImage", pageImage);
        Set(serializedController, "pageGroup", pageGroup);
        Set(serializedController, "fallbackText", fallback);
        Set(serializedController, "progressText", progress);
        Set(serializedController, "previousButton", previous);
        Set(serializedController, "nextButton", next);
        Set(serializedController, "skipButton", skip);
        Set(serializedController, "skipConfirmation", confirmation);
        Set(serializedController, "confirmSkipButton", confirm);
        Set(serializedController, "cancelSkipButton", cancel);
        Set(serializedController, "inputBlocker", inputBlocker);
        Set(serializedController, "audioSource", audioSource);
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath);
        AddSceneToBuildSettings(TutorialScenePath);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = controllerObject;
        Debug.Log(
            "[StoryScene] Created the 28-page Prologue profile and StoryScene, "
            + "then added StoryScene and TutorialScene to Build Settings. Main Menu routing remains unchanged until Play Mode validation passes.");
    }

    private static StorySequenceProfile CreateOrUpdateProfile(Sprite[] artwork)
    {
        StorySequenceProfile profile = AssetDatabase.LoadAssetAtPath<StorySequenceProfile>(ProfilePath);
        if (profile == null) { profile = ScriptableObject.CreateInstance<StorySequenceProfile>(); AssetDatabase.CreateAsset(profile, ProfilePath); }
        SerializedObject data = new SerializedObject(profile);
        data.FindProperty("sequenceId").stringValue = "prologue";
        data.FindProperty("completionKey").stringValue = "SpellStrike_PrologueCompleted";
        data.FindProperty("defaultNextSceneName").stringValue = "TutorialScene";
        data.FindProperty("allowSkip").boolValue = true;
        SerializedProperty pages = data.FindProperty("pages");
        pages.arraySize = PageCount;
        for (int i = 0; i < PageCount; i++)
        {
            SerializedProperty page = pages.GetArrayElementAtIndex(i);
            page.FindPropertyRelative("pageId").stringValue = $"prologue_page_{i + 1:00}";
            page.FindPropertyRelative("pageSprite").objectReferenceValue = artwork[i];
            page.FindPropertyRelative("optionalAudio").objectReferenceValue = null;
            page.FindPropertyRelative("minimumDisplayTime").floatValue = 0.35f;
            page.FindPropertyRelative("optionalNextSceneOverride").stringValue = string.Empty;
            page.FindPropertyRelative("fallbackText").stringValue =
                $"PROLOGUE · THE AGE OF WORDS\nPage {i + 1:00} artwork is unavailable.";
        }
        data.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static bool ConfigureArtworkImporters(out Sprite[] artwork)
    {
        artwork = new Sprite[PageCount];
        for (int i = 0; i < PageCount; i++)
        {
            string artworkPath = GetArtworkPath(i + 1);
            TextureImporter importer = AssetImporter.GetAtPath(artworkPath) as TextureImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(artworkPath, ImportAssetOptions.ForceSynchronousImport);
                importer = AssetImporter.GetAtPath(artworkPath) as TextureImporter;
            }

            if (importer == null)
            {
                EditorUtility.DisplayDialog(
                    "Missing Prologue artwork",
                    $"Page {i + 1:00} could not be imported:\n{artworkPath}",
                    "OK");
                return false;
            }

            bool changed = importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single
                || !importer.sRGBTexture
                || importer.mipmapEnabled
                || importer.maxTextureSize != 2048
                || importer.wrapMode != TextureWrapMode.Clamp
                || importer.alphaSource != TextureImporterAlphaSource.None
                || importer.alphaIsTransparency;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.alphaIsTransparency = false;
            if (changed)
                importer.SaveAndReimport();

            artwork[i] = AssetDatabase.LoadAssetAtPath<Sprite>(artworkPath);
            if (artwork[i] == null)
            {
                EditorUtility.DisplayDialog(
                    "Invalid Prologue artwork",
                    $"Page {i + 1:00} did not produce a Sprite:\n{artworkPath}",
                    "OK");
                return false;
            }
        }

        return true;
    }

    private static string GetArtworkPath(int pageNumber) =>
        $"{ArtworkFolder}/Prologue_Page_{pageNumber:00}.png";

    private static Canvas CreateCanvas()
    {
        GameObject root = new GameObject("StoryCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static void AddSceneToBuildSettings(string path)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
        {
            Debug.LogError($"[StoryScene] Cannot add missing scene to Build Settings: {path}");
            return;
        }

        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!scenes.Exists(entry => entry.path == path)) scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static TMP_Text Text(string name, Transform parent, string value, float size, TextAlignmentOptions alignment)
    {
        TMP_Text text = Rect(name, parent, Vector2.zero, Vector2.one).AddComponent<TextMeshProUGUI>();
        text.text = value; text.fontSize = size; text.color = new Color(1f, 0.97f, 0.89f, 1f);
        text.alignment = alignment; text.textWrappingMode = TextWrappingModes.Normal; text.raycastTarget = false;
        return text;
    }

    private static Button ButtonWithLabel(string name, Transform parent, string label, Vector2 min, Vector2 max)
    {
        GameObject buttonObject = Rect(name, parent, min, max);
        Image image = buttonObject.AddComponent<Image>(); image.color = new Color(0.02f, 0.03f, 0.06f, 0.84f);
        Button button = buttonObject.AddComponent<Button>(); button.targetGraphic = image;
        TMP_Text text = Text("Label", buttonObject.transform, label, 20, TextAlignmentOptions.Center);
        SetRect(text.rectTransform, Vector2.zero, Vector2.one);
        return button;
    }

    private static Button InvisibleNavigationButton(
        string name,
        Transform parent,
        Vector2 min,
        Vector2 max)
    {
        GameObject buttonObject = Rect(name, parent, min, max);
        Image hitArea = buttonObject.AddComponent<Image>();
        hitArea.color = Color.clear;
        hitArea.raycastTarget = true;
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = hitArea;
        return button;
    }

    private static void AddShade(GameObject target, Color color)
    {
        Image image = target.AddComponent<Image>(); image.color = color; image.raycastTarget = false;
    }

    private static GameObject Rect(string name, Transform parent, Vector2 min, Vector2 max)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        SetRect((RectTransform)gameObject.transform, min, max);
        return gameObject;
    }

    private static void SetRect(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
    }

    private static void Set(SerializedObject target, string propertyName, Object value)
    {
        target.FindProperty(propertyName).objectReferenceValue = value;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
