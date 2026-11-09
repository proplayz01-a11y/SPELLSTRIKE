using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Second isolated, non-destructive Main Menu V2 QA pass.
/// Adds an actual tutorial Spellbook mesh and five reused game-tile meshes.
/// All 3D objects render into alpha-transparent RenderTextures which are
/// composited on opposite sides of illustrated Van for correct depth ordering.
/// </summary>
public static class MainMenuV2ThreeDBuilder
{
    private const string Source =
        "Assets/Scenes/MenuConcepts/MainMenuV2_Prototype_20261030.unity";
    private const string Destination =
        "Assets/Scenes/MenuConcepts/MainMenuV2_Prototype_20261030_v02.unity";
    private const string Art = "Assets/MenuV2/Art/";
    private const string Runtime = "Assets/MenuV2/RenderTextures/";
    private const string UIPath = "Assets/Prefabs/UI/MainMenuSpellbookUI.prefab";
    private const string TutorialBook = "Assets/Tutorial Assets/Navigation/SPELLBOOK.glb";
    private const string TileFolder = "Assets/Sprites/3D TILES/";

    // These are otherwise unused layers (checked against TagManager). No
    // project-wide TagManager modification needed.
    private const int BookLayer = 27;
    private const int TileBackLayer = 28;
    private const int TileFrontLayer = 29;

    [MenuItem("SPELLSTRIKE/Main Menu V2/Add Real 3D Book and Orbiting Tiles (V02)")]
    public static void BuildV02()
    {
        if (System.Diagnostics.Process.GetProcessesByName("VALORANT-Win64-Shipping").Length > 0)
        {
            Debug.LogError("[MenuV2] VALORANT running; stopped.");
            return;
        }
        if (EditorApplication.isPlaying || EditorApplication.isCompiling ||
            EditorApplication.isUpdating)
        {
            Debug.LogError("[MenuV2] Stop Play/compilation/import before modifying the isolated scene.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != Source || scene.isDirty)
        {
            Debug.LogError("[MenuV2] Open clean V1 prototype before V02; active scene is " + scene.path);
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Destination) != null)
        {
            Debug.LogWarning("[MenuV2] V02 exists already. Refusing to overwrite.");
            return;
        }

        GameObject bookAsset = AssetDatabase.LoadAssetAtPath<GameObject>(TutorialBook);
        GameObject uiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UIPath);
        if (bookAsset == null || uiPrefab == null)
        {
            Debug.LogError("[MenuV2] Tutorial Spellbook model or existing Chronicle UI prefab is missing.");
            return;
        }
        foreach (char letter in "ABCDE")
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(
                    TileFolder + "Tile_" + letter + ".prefab") == null)
            {
                Debug.LogError("[MenuV2] Missing gameplay tile prefab: " + letter);
                return;
            }
        }

        TextureImporter haloImporter =
            AssetImporter.GetAtPath(Art + "Book_HoverGoldHalo.png") as TextureImporter;
        if (haloImporter == null)
        {
            Debug.LogError("[MenuV2] Missing procedural gold book halo.");
            return;
        }
        if (haloImporter.textureType != TextureImporterType.Sprite)
        {
            haloImporter.textureType = TextureImporterType.Sprite;
            haloImporter.spriteImportMode = SpriteImportMode.Single;
            haloImporter.mipmapEnabled = false;
            haloImporter.SaveAndReimport();
        }

        if (!AssetDatabase.IsValidFolder("Assets/MenuV2/RenderTextures"))
            AssetDatabase.CreateFolder("Assets/MenuV2", "RenderTextures");
        RenderTexture backTexture = CreateTexture("OrbitTiles_Back", 900, 800);
        RenderTexture frontTexture = CreateTexture("OrbitTiles_Front", 900, 800);
        RenderTexture bookTexture = CreateTexture("TutorialSpellbook_Menu", 512, 512);

        EditorSceneManager.SaveScene(scene, Destination, true);
        scene = EditorSceneManager.OpenScene(Destination, OpenSceneMode.Single);
        Canvas canvas = GameObject.Find("MainMenuV2_Canvas").GetComponent<Canvas>();
        RectTransform canvasRoot = canvas.GetComponent<RectTransform>();

        GameObject van = canvasRoot.Find("ApprovedOlderVanAert").gameObject;
        Transform world = new GameObject("MenuV2_3DVisualWorld").transform;

        GameObject oldMainCamera = GameObject.Find("Main Camera");
        if (oldMainCamera != null)
            oldMainCamera.GetComponent<Camera>().cullingMask = 0;

        AddTileOrbit(world, canvasRoot, van.transform,
            backTexture, frontTexture);
        AddSpellbook(world, canvasRoot, bookAsset, bookTexture, uiPrefab);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[MenuV2] V02 saved: " + Destination +
            " | Real Tutorial Spellbook and 5 original 3D gameplay tile meshes.");
    }

    private static RenderTexture CreateTexture(string name, int width, int height)
    {
        string path = Runtime + name + ".renderTexture";
        RenderTexture alreadyExists = AssetDatabase.LoadAssetAtPath<RenderTexture>(path);
        if (alreadyExists != null)
            return alreadyExists;

        RenderTexture texture = new RenderTexture(
            width, height, 16, RenderTextureFormat.ARGB32)
        {
            name = name, antiAliasing = 1, useMipMap = false,
            autoGenerateMips = false, filterMode = FilterMode.Bilinear
        };
        AssetDatabase.CreateAsset(texture, path);
        return texture;
    }

    private static void AddTileOrbit(Transform world,
        RectTransform canvas, Transform van,
        RenderTexture back, RenderTexture front)
    {
        Transform rig = new GameObject("FiveGameTileOrbitRig").transform;
        rig.SetParent(world, false);
        rig.localPosition = Vector3.zero;
        for (int i = 0; i < 5; i++)
        {
            char letter = "ABCDE"[i];
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                TileFolder + "Tile_" + letter + ".prefab");
            GameObject tile = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            tile.name = "MenuOrbit_" + letter;
            tile.transform.SetParent(rig, false);
            tile.transform.localScale = new Vector3(.13f, 1.15f, 1.15f);
            foreach (Collider collider in tile.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;
            SetLayer(tile.transform, TileBackLayer);
            tile.AddComponent<MenuV2OrbitalTile>()
                .Configure((Mathf.PI * 2f / 5f) * i,
                    .31f, TileBackLayer, TileFrontLayer);
        }

        AddRenderCamera(world, "Orbit_Back_Camera",
            TileBackLayer, back, 5.3f, 15f);
        AddRenderCamera(world, "Orbit_Front_Camera",
            TileFrontLayer, front, 5.3f, 15f);

        RawImage backImage = AddRawImage(
            canvas, "Orbiting3DTiles_BEHIND_Van", back,
            1190, 422, 795, 700);
        backImage.transform.SetSiblingIndex(van.GetSiblingIndex());

        RawImage frontImage = AddRawImage(
            canvas, "Orbiting3DTiles_IN_FRONT_Van", front,
            1190, 422, 795, 700);
        frontImage.transform.SetSiblingIndex(van.GetSiblingIndex() + 1);
    }

    private static void AddSpellbook(Transform world,
        RectTransform canvas, GameObject bookPrefab,
        RenderTexture texture, GameObject chroniclePrefab)
    {
        Transform root = new GameObject("TutorialSpellbook3D_MenuVariant").transform;
        root.SetParent(world, false);
        GameObject book = (GameObject)PrefabUtility.InstantiatePrefab(bookPrefab);
        book.name = "Tutorial_ActualSpellbookMesh";
        book.transform.SetParent(root, false);
        book.transform.localPosition = Vector3.zero;
        book.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        book.transform.localScale = Vector3.one;

        Renderer[] renderers = book.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            Debug.LogError("[MenuV2] Tutorial Spellbook asset has no Renderer.");
            return;
        }
        Bounds bounds = GetBounds(renderers);
        float longest = Mathf.Max(bounds.size.x,
            Mathf.Max(bounds.size.y, bounds.size.z));
        book.transform.localScale *= 2.45f / Mathf.Max(.001f, longest);
        bounds = GetBounds(renderers);
        book.transform.position -= bounds.center;
        SetLayer(book.transform, BookLayer);
        root.gameObject.AddComponent<MenuV2BookIdle>();

        AddRenderCamera(world, "ActualTutorialBook_Camera",
            BookLayer, texture, 1.95f, 9f);

        GameObject lightObj = new GameObject("Menu3D_StudioLight", typeof(Light));
        lightObj.transform.SetParent(world, false);
        lightObj.transform.localRotation = Quaternion.Euler(35f, -35f, 0f);
        Light light = lightObj.GetComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.5f;
        light.cullingMask =
            (1 << BookLayer) | (1 << TileBackLayer) | (1 << TileFrontLayer);

        // Reuse existing Chronicle code/prefab instead of introducing a new database.
        GameObject chronicle = (GameObject)PrefabUtility.InstantiatePrefab(
            chroniclePrefab, canvas.transform);
        chronicle.name = "ExistingChronicleUI_Prototype";
        RectTransform chronicleRect = chronicle.GetComponent<RectTransform>();
        Stretch(chronicleRect);
        MainMenuSpellbookUI controller = chronicle.GetComponent<MainMenuSpellbookUI>();
        Transform oldOpenButton = chronicle.transform.Find("SpellbookButton");
        if (oldOpenButton != null)
            oldOpenButton.gameObject.SetActive(false);

        GameObject ui = UI("Clickable3DSpellbook", canvas);
        Center(ui.GetComponent<RectTransform>(), 1750, 167, 310, 310);
        Image halo = UI("WarmGoldHoverHalo", ui.transform).AddComponent<Image>();
        Stretch(halo.rectTransform);
        halo.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            Art + "Book_HoverGoldHalo.png");
        halo.raycastTarget = false;

        RawImage bookView = UI("RenderedTutorialSpellbook", ui.transform)
            .AddComponent<RawImage>();
        Stretch(bookView.rectTransform);
        bookView.texture = texture;
        bookView.color = Color.white;
        bookView.raycastTarget = false;

        GameObject labelGo = UI("HoverLabel", ui.transform);
        CenterRelative(labelGo.GetComponent<RectTransform>(), 0, -168, 250, 52);
        Image labelBG = labelGo.AddComponent<Image>();
        labelBG.color = new Color(.035f, .075f, .15f, .96f);
        labelBG.raycastTarget = false;
        Outline outline = labelGo.AddComponent<Outline>();
        outline.effectColor = new Color(.82f, .63f, .30f);
        outline.effectDistance = new Vector2(2, -2);
        CanvasGroup label = labelGo.AddComponent<CanvasGroup>();
        label.blocksRaycasts = false;
        TextMeshProUGUI text = UI("Caption", labelGo.transform)
            .AddComponent<TextMeshProUGUI>();
        Stretch(text.rectTransform);
        text.text = "SPELLBOOK";
        text.fontSize = 27;
        text.fontStyle = FontStyles.Bold;
        text.color = new Color(.96f, .90f, .74f);
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;

        GameObject button = UI("BookPointerHotspot", ui.transform);
        Stretch(button.GetComponent<RectTransform>());
        Image hitArea = button.AddComponent<Image>();
        hitArea.color = new Color(1f, 1f, 1f, .003f);
        hitArea.raycastTarget = true;
        Button selectable = button.AddComponent<Button>();
        selectable.transition = Selectable.Transition.None;
        button.AddComponent<MenuV2HoverTarget>().Configure(
            MenuV2HoverTarget.TargetAction.Spellbook, halo, label,
            null, null, null, controller);

        Transform options = canvas.transform.Find("OptionsPanel");
        Transform quit = canvas.transform.Find("QuitConfirmPanel");
        if (chronicle != null) chronicle.transform.SetAsLastSibling();
        if (options != null) options.SetAsLastSibling();
        if (quit != null) quit.SetAsLastSibling();
    }

    private static Bounds GetBounds(Renderer[] renderers)
    {
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static Camera AddRenderCamera(Transform world, string name,
        int layer, RenderTexture texture, float size, float distance)
    {
        GameObject cameraObject = new GameObject(name, typeof(Camera));
        cameraObject.transform.SetParent(world, false);
        cameraObject.transform.localPosition = new Vector3(0, 0, -distance);
        cameraObject.transform.localRotation = Quaternion.identity;
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = size;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        camera.cullingMask = 1 << layer;
        camera.allowHDR = false;
        camera.allowMSAA = false;
        camera.targetTexture = texture;
        camera.depth = -4;
        return camera;
    }

    private static RawImage AddRawImage(RectTransform parent, string name,
        RenderTexture texture, float x, float y, float w, float h)
    {
        RawImage image = UI(name, parent).AddComponent<RawImage>();
        Center(image.rectTransform, x, y, w, h);
        image.texture = texture;
        image.color = Color.white;
        image.raycastTarget = false;
        return image;
    }

    private static void SetLayer(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        foreach (Transform child in root)
            SetLayer(child, layer);
    }

    private static GameObject UI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }

    private static void Center(RectTransform rect,
        float x, float y, float w, float h)
    {
        CenterRelative(rect, x - 1200f, y - 540f, w, h);
    }

    private static void CenterRelative(RectTransform rect,
        float x, float y, float w, float h)
    {
        rect.anchorMin = new Vector2(.5f, .5f);
        rect.anchorMax = new Vector2(.5f, .5f);
        rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = new Vector2(w, h);
        rect.anchoredPosition = new Vector2(x, y);
    }
}
