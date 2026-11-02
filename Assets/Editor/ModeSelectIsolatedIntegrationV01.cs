using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Visible-only menu action for the isolated mode-select review scene.
/// Never invokes the generic runtime UI builder or edits production scenes.
/// </summary>
public static class ModeSelectIsolatedIntegrationV01
{
    private const string ScenePath = "Assets/Scenes/MinigameModeSelect_Integration_v01.unity";
    private const string Folder = "Assets/Minigames/ModeSelectWorkbench/";
    private const string Sprites = Folder + "Separated/";
    private const string MaterialPath = Folder + "CardGoldSweep.mat";
    private const string RenderTexturePath = Folder + "SpellItOut_V11_TilesPreview.renderTexture";
    private const float Scale = 1080f / 941f;
    private const float RefX = (2400f - 1672f * Scale) * .5f;

    [MenuItem("SPELLSTRIKE/Minigames/Open Isolated Integration Additively (Preserve Dirty Scenes)")]
    public static void OpenIntegrationAdditively()
    {
        if (System.Diagnostics.Process.GetProcessesByName("VALORANT-Win64-Shipping").Length > 0)
        {
            Debug.LogWarning("[Mode Select] VALORANT running; no scene manipulation.");
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene target = SceneManager.GetSceneByPath(ScenePath);
        if (!target.IsValid() || !target.isLoaded)
            target = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(target);
        Debug.Log("[Mode Select] Isolated scene opened additively. Other dirty scenes remain loaded, untouched and unsaved.");
    }

    [MenuItem("SPELLSTRIKE/Minigames/Assemble Isolated Mode Select Integration v01")]
    public static void Assemble()
    {
        if (System.Diagnostics.Process.GetProcessesByName("VALORANT-Win64-Shipping").Length > 0)
        {
            Debug.LogWarning("[Mode Select] VALORANT running; integration is blocked.");
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
        {
            Debug.LogError("[Mode Select] Exit Play Mode and let compilation finish.");
            return;
        }
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
        {
            Debug.LogError("[Mode Select] Open the isolated scene " + ScenePath + " before using this menu action.");
            return;
        }
        if (Array.Exists(scene.GetRootGameObjects(), go => FindDescendant(go.transform, "ModeSelect_ApprovedArtwork") != null))
        {
            Debug.LogWarning("[Mode Select] Already assembled. Refusing to duplicate the UI.");
            return;
        }

        Canvas canvas = null;
        foreach (GameObject sceneRoot in scene.GetRootGameObjects())
        {
            canvas = Array.Find(sceneRoot.GetComponentsInChildren<Canvas>(true),
                component => component.gameObject.name == "MinigamesCanvas");
            if (canvas != null) break;
        }
        if (canvas == null || canvas.gameObject.name != "MinigamesCanvas")
            throw new InvalidOperationException("Expected existing MinigamesCanvas not found.");
        Transform uiRoot = FindDescendant(canvas.transform, "MinigamesUI");
        Transform selection = FindDescendant(uiRoot, "MinigameSelectionScreen");
        Transform oldPanel = FindDescendant(selection, "SelectionBoard");
        if (uiRoot == null || selection == null || oldPanel == null)
            throw new InvalidOperationException("Original MinigamesUI binding chain not found.");

        Button[] controls = {
            FindNamedButton(selection, "WordMasterButton"),
            FindNamedButton(selection, "SpellItOutButton"),
            FindNamedButton(selection, "WordExcavationButton"),
            FindNamedButton(selection, "BackButton")
        };
        foreach (Button button in controls)
            if (button == null) throw new InvalidOperationException("Missing one of the four original controller buttons.");

        // Required runtime contract: this component retains the names, methods,
        // event handlers, and configuration expected by the original controller.
        MinigamesSceneController controller = canvas.GetComponent<MinigamesSceneController>();
        if (controller == null)
            throw new InvalidOperationException("Existing MinigamesSceneController not found.");
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("useEditableSceneUI").boolValue = true;
        so.FindProperty("rebuildUIOnStart").boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2400, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f; // 1920x1080: uniform fit, no horizontal clipping.

        Image oldBackground = uiRoot.GetComponent<Image>();
        if (oldBackground != null)
        {
            oldBackground.color = Color.clear;
            oldBackground.raycastTarget = false;
        }

        // Move the already-wired buttons *before* hiding the old layout group.
        foreach (Button button in controls)
        {
            button.transform.SetParent(selection, false);
            button.gameObject.SetActive(true);
            Image buttonHit = button.GetComponent<Image>();
            if (buttonHit == null) throw new InvalidOperationException("Button hit graphic missing: " + button.name);
            buttonHit.color = new Color(1f, 1f, 1f, .001f);
            buttonHit.raycastTarget = true;
            button.transition = Selectable.Transition.None;
            LayoutElement layout = button.GetComponent<LayoutElement>();
            if (layout != null) layout.enabled = false;
            for (int i = 0; i < button.transform.childCount; i++)
                button.transform.GetChild(i).gameObject.SetActive(false);
        }
        oldPanel.gameObject.SetActive(false);

        GameObject owner = new GameObject("ModeSelect_ApprovedArtwork", typeof(RectTransform));
        owner.transform.SetParent(selection, false);
        RectTransform ownerRect = owner.GetComponent<RectTransform>();
        ownerRect.anchorMin = Vector2.zero;
        ownerRect.anchorMax = Vector2.one;
        ownerRect.offsetMin = ownerRect.offsetMax = Vector2.zero;
        owner.transform.SetAsFirstSibling();

        Image background = AddImage("Tent_Backdrop_Approved", uiRoot, Folder + "TentInteriorApproved.png",
            new Vector2(0, 0), new Vector2(2400, 1351), true);
        background.transform.SetAsFirstSibling();

        // Asset's alpha bbox starts at (31,34) and is placed at (311,91) in reference.
        // Apply the manifest's 0.757185332 source-to-display mapping, then the screen scale.
        float bodyScale = 0.757185332f;
        float bodyLeft = 311f - 31f * bodyScale;
        float bodyTop = 91f - 34f * bodyScale;
        AddReferenceImage("ParchmentBody", owner.transform, "MainPanel/MainBody_Empty.png",
            bodyLeft, bodyTop, 1448f * bodyScale, 1086f * bodyScale);

        Material material = GetOrCreateReflectionMaterial();
        BuildCard(controls[0], "WordMaster",
            new[] {"WM_01_Base.png", "WM_03_GreenAura.png", "WM_04_Spellbook.png",
                "WM_06_MagicRings.png", "WM_05_MagicStar.png", "WM_07_Text.png", "WM_08_Arrow.png"},
            "WM_02_GoldReflectionMask.png", 203f, material);
        BuildCard(controls[1], "SpellItOut",
            new[] {"SIO_01_Base.png", "SIO_03_MagicalGlow.png",
                "SIO_04_Text.png", "SIO_05_Arrow.png"},
            "SIO_02_GoldReflectionMask.png", 375f, material);
        BuildCard(controls[2], "WordExcavation",
            new[] {"WE_01_Base.png", "WE_03_MagicalGlow.png", "WE_05_RocksAndLetter.png",
                "WE_04_Pickaxe.png", "WE_06_Sparkles.png", "WE_07_Text.png", "WE_08_Arrow.png"},
            "WE_02_GoldReflectionMask.png", 543f, material);

        AddReferenceImage("HeaderRibbon_BakedText", owner.transform,
            "MainPanel/Header_SpellstrikeMiniGames.png", 389f, 7f, 899f, 196f);

        Place(controls[3].GetComponent<RectTransform>(), 639f, 730f, 395f, 83f);
        AddImage("MainMenu_BakedText", controls[3].transform,
            Sprites + "MainPanel/Button_MainMenu.png", Vector2.zero,
            new Vector2(395f * Scale, 83f * Scale), false);
        controls[3].transform.SetAsLastSibling();
        if (controls[3].GetComponent<ModeSelectPressedTint>() == null)
            controls[3].gameObject.AddComponent<ModeSelectPressedTint>();

        Build3DTiles(scene, controls[1].transform);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new IOException("Failed to save isolated mode-select scene.");
        AssetDatabase.SaveAssets();
        Debug.Log("[Mode Select] ISOLATED scene saved. Three cards with independent alpha-mask hover materials, " +
                  "baked text and buttons, V11 S/P/E/L 3D preview, separated background/body/ribbon. " +
                  "Production scene and controller script unchanged. Scene=" + scene.path);
    }

    private static Material GetOrCreateReflectionMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material != null) return material;
        Shader shader = Shader.Find("SPELLSTRIKE/UI/BeveledGoldHoverReflection");
        if (shader == null) throw new InvalidOperationException("Reflection shader did not compile/import.");
        material = new Material(shader);
        material.name = "CardGoldSweep";
        material.SetColor("_Tint", new Color(1, .93f, .76f, 1f));
        AssetDatabase.CreateAsset(material, MaterialPath);
        return material;
    }

    private static void BuildCard(Button target, string section, string[] visualLayers,
        string reflectionLayer, float referenceTop, Material material)
    {
        RectTransform rect = target.GetComponent<RectTransform>();
        Place(rect, 400f, referenceTop, 872f, 172f);
        string path = Sprites + section + "/";
        foreach (string spriteName in visualLayers)
            AddImage(spriteName.Replace(".png", ""), rect, path + spriteName,
                Vector2.zero, new Vector2(872f * Scale, 172f * Scale), false);

        Image overlay = AddImage("GoldReflection_Masked", rect,
            path + reflectionLayer, Vector2.zero, new Vector2(872f * Scale, 172f * Scale), false);
        overlay.material = material;
        overlay.raycastTarget = false;
        ModeSelectGoldBorderHover hover = target.GetComponent<ModeSelectGoldBorderHover>();
        if (hover == null) hover = target.gameObject.AddComponent<ModeSelectGoldBorderHover>();
        hover.SetReferences(overlay, material);
        hover.SetContinuousHover(true);
        if (target.GetComponent<ModeSelectPressedTint>() == null)
            target.gameObject.AddComponent<ModeSelectPressedTint>();
        target.gameObject.SetActive(true);
    }

    private static Button FindNamedButton(Transform root, string name)
    {
        Transform t = FindDescendant(root, name);
        return t == null ? null : t.GetComponent<Button>();
    }

    private static Transform FindDescendant(Transform root, string name)
    {
        if (root == null) return null;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == name) return child;
        return null;
    }

    private static Image AddReferenceImage(string name, Transform parent, string file,
        float x, float y, float w, float h)
    {
        float sx = (RefX + (x + w * .5f) * Scale) - 1200f;
        float sy = 540f - (y + h * .5f) * Scale;
        return AddImage(name, parent, Sprites + file,
            new Vector2(sx, sy), new Vector2(w * Scale, h * Scale), false);
    }

    private static Image AddImage(string name, Transform parent, string path,
        Vector2 pos, Vector2 size, bool background)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) throw new FileNotFoundException("Missing imported sprite", path);
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.raycastTarget = false;
        image.color = Color.white;
        return image;
    }

    private static void Place(RectTransform rect, float x, float y, float w, float h)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = new Vector2(
            RefX + (x + w * .5f) * Scale - 1200f,
            540f - (y + h * .5f) * Scale);
        rect.sizeDelta = new Vector2(w * Scale, h * Scale);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static void Build3DTiles(Scene scene, Transform card)
    {
        RenderTexture texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(RenderTexturePath);
        if (texture == null)
        {
            texture = new RenderTexture(512, 256, 16, RenderTextureFormat.ARGB32);
            texture.name = "V11_SPEL_Letters_Transparent";
            texture.antiAliasing = 1;
            AssetDatabase.CreateAsset(texture, RenderTexturePath);
        }

        GameObject gallery = new GameObject("V11_SPEL_3D_AssetReuse");
        gallery.transform.position = new Vector3(10000f, 10000f, 10000f);
        // Layer 30: isolated render channel, without touching TagManager.
        const int layer = 30;
        string letters = "SPEL";
        string[] rarity = {"Bronze", "Silver", "Bronze", "Silver"};
        float[] x = {-1.7f, -.57f, .57f, 1.7f};
        float[] tilts = {12f, -8f, 6f, -13f};
        for (int i = 0; i < letters.Length; i++)
        {
            Transform slot = new GameObject("V11_" + letters[i] + "_Slot").transform;
            slot.SetParent(gallery.transform, false);
            slot.localPosition = new Vector3(x[i], (i % 2 == 0 ? -.08f : .12f), 0f);
            slot.localRotation = Quaternion.Euler(0f, 0f, tilts[i]);
            Transform visual = new GameObject("CenteredRodinV11").transform;
            visual.SetParent(slot, false);
            visual.localScale = Vector3.one * .67f;
            visual.localPosition = new Vector3(0f, -.883f * .67f, 0f);
            GameObject baseAsset = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/MenuV2/RodinV11/SharedBases/SPELLSTRIKE_" + rarity[i] + "_SharedBase_1024.glb");
            GameObject glyphAsset = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/MenuV2/RodinV11/BlackLetters/SPELLSTRIKE_" + letters[i] + "_Black3D_BothFaces_MobileLean.glb");
            if (baseAsset == null || glyphAsset == null)
                throw new FileNotFoundException("V11 tile GLB import missing for " + letters[i]);
            var body = (GameObject)PrefabUtility.InstantiatePrefab(baseAsset, visual);
            var glyph = (GameObject)PrefabUtility.InstantiatePrefab(glyphAsset, visual);
            body.name = "ExistingV11_" + rarity[i] + "_Base";
            glyph.name = "ExistingV11_Black3D_" + letters[i];
            foreach (Transform child in visual.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = layer;
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;
        }

        GameObject cameraGo = new GameObject("V11_SP_E_L_DedicatedPreviewCamera", typeof(Camera));
        cameraGo.transform.position = gallery.transform.position + new Vector3(0f, .25f, -8f);
        cameraGo.transform.LookAt(gallery.transform.position);
        Camera cam = cameraGo.GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 1.65f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
        cam.cullingMask = 1 << layer;
        cam.targetTexture = texture;
        cam.depth = -50f;

        GameObject lightGo = new GameObject("V11_TilePreviewKey", typeof(Light));
        lightGo.transform.rotation = Quaternion.Euler(40f, -25f, 0f);
        Light light = lightGo.GetComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.25f;
        light.cullingMask = 1 << layer;
        light.shadows = LightShadows.None;

        GameObject ui = new GameObject("V11_Actual3DTiles_SPEL_RawImage", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(RawImage));
        ui.transform.SetParent(card, false);
        RectTransform tileRect = ui.GetComponent<RectTransform>();
        tileRect.anchorMin = tileRect.anchorMax = new Vector2(.5f, .5f);
        tileRect.sizeDelta = new Vector2(258f * Scale, 129f * Scale);
        tileRect.anchoredPosition = new Vector2(-297f * Scale, 0f);
        RawImage raw = ui.GetComponent<RawImage>();
        raw.texture = texture;
        raw.raycastTarget = false;
        raw.color = Color.white;
        // Draw above independent SIO magical glow, without hiding the baked text.
        ui.transform.SetSiblingIndex(2);
    }
}
