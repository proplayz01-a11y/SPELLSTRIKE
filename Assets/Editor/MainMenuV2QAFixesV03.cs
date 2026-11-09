using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Makes a NEW copy of the isolated menu after the user's recorded QA.
/// Never overwrites V01, V02, production Main Menu, TutorialScene, or cinematics.
/// </summary>
public static class MainMenuV2QAFixesV03
{
    public const string Source =
        "Assets/Scenes/MenuConcepts/MainMenuV2_Prototype_20261030_v02.unity";
    public const string Destination =
        "Assets/Scenes/MenuConcepts/MainMenuV2_Prototype_20261030_v03_QA.unity";
    private const string Art = "Assets/MenuV2/Art/";
    private const string ShaderPath = "Assets/MenuV2/Shaders/PortalInwardSwirl.shader";
    private const string MaterialPath =
        "Assets/MenuV2/Materials/PortalInwardSwirl_QA.mat";

    [MenuItem("SPELLSTRIKE/Main Menu V2/Build QA Refinement (V03)")]
    public static void Build()
    {
        if (System.Diagnostics.Process.GetProcessesByName("VALORANT-Win64-Shipping").Length > 0)
        {
            Debug.LogError("[MenuV2] VALORANT active. Refusing V03 builder.");
            return;
        }

        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            Debug.LogError("[MenuV2] Stop Play or import/compile before building V03.");
            return;
        }

        Scene source = SceneManager.GetActiveScene();
        if (source.path != Source || source.isDirty)
        {
            Debug.LogError("[MenuV2] Open saved V02 first. Active=" + source.path +
                           " dirty=" + source.isDirty);
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Destination) != null)
        {
            Debug.LogWarning("[MenuV2] Existing QA V03 preserved. Won't overwrite.");
            return;
        }

        foreach (string name in new[]
        {
            "Adventure_Arch_Hover_Aligned_QA.png",
            "Minigame_Tent_Hover_Aligned_QA.png",
            "Options_Workbench_Hover_Aligned_QA.png",
            "Quit_Sign_Hover_Aligned_QA.png",
            "ContactShadow_QA.png", "Portal_CyanLightSpill_QA.png",
            "LogoCrystalGlint_QA.png",
            "HoverPlaque_Adventure_QA.png", "HoverPlaque_Minigame_QA.png",
            "HoverPlaque_Spellbook_QA.png", "HoverPlaque_Options_QA.png",
            "HoverPlaque_Quit_QA.png"
        })
            EnsureSprite(name);

        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
        if (shader == null)
        {
            Debug.LogError("[MenuV2] Portal shader missing: " + ShaderPath);
            return;
        }

        if (!EditorSceneManager.SaveScene(source, Destination, true))
        {
            Debug.LogError("[MenuV2] Failed making safe V03 scene copy.");
            return;
        }

        Scene qa = EditorSceneManager.OpenScene(Destination, OpenSceneMode.Single);
        RectTransform canvas = GameObject.Find("MainMenuV2_Canvas")
            .GetComponent<RectTransform>();

        ReplaceHover(canvas, "AdventureGateway",
            "Adventure_Arch_Hover_Aligned_QA.png", "ADVENTURE", false);
        ReplaceHover(canvas, "MinigameTent",
            "Minigame_Tent_Hover_Aligned_QA.png", "MINIGAME", false);
        ReplaceHover(canvas, "OptionsWorkbench",
            "Options_Workbench_Hover_Aligned_QA.png", "OPTIONS", true);
        ReplaceHover(canvas, "QuitSign",
            "Quit_Sign_Hover_Aligned_QA.png", "QUIT", true);
        ReplaceHover(canvas, "Clickable3DSpellbook",
            null, "SPELLBOOK", true);

        // The QA video explicitly places the real book on Quit's right-hand
        // stone. Preserve Quit by moving its sign to the book's old position.
        MoveTarget(canvas, "QuitSign", 1750, 167, 255, 245);
        MoveTarget(canvas, "Clickable3DSpellbook", 2215, 157, 300, 300);

        // Reposition lower hover plaques within 2400x1080 safe bounds.
        RepositionLabel(canvas, "OptionsWorkbench", 174f);
        RepositionLabel(canvas, "QuitSign", 154f);
        RepositionLabel(canvas, "Clickable3DSpellbook", 169f);

        AddGroundShadow(canvas, "AdventureShadow", "AdventureGateway", 423, 230, 475, 72);
        AddGroundShadow(canvas, "MinigameShadow", "MinigameTent", 1962, 248, 440, 67);
        AddGroundShadow(canvas, "VanShadow", "ApprovedOlderVanAert", 1190, 35, 270, 55);
        AddGroundShadow(canvas, "OptionsShadow", "OptionsWorkbench", 265, 43, 300, 52);
        AddGroundShadow(canvas, "QuitShadow", "QuitSign", 1750, 72, 185, 36);
        AddGroundShadow(canvas, "BookShadow", "Clickable3DSpellbook", 2215, 67, 225, 48);

        // Give the existing 3D model some visible depth instead of exposing
        // a flat front-on cover. Reuse the Tutorial model, don't mutate it.
        GameObject bookRig = GameObject.Find("TutorialSpellbook3D_MenuVariant");
        if (bookRig != null)
            bookRig.transform.localRotation = Quaternion.Euler(13f, -28f, -12f);
        GameObject orbit = GameObject.Find("FiveGameTileOrbitRig");
        if (orbit != null)
            foreach (Transform tile in orbit.transform)
                tile.localScale = new Vector3(.24f, 1.15f, 1.15f);

        GameObject portalObject = GameObject.Find("PortalVortex_ShaderGraphPending");
        if (portalObject != null)
        {
            portalObject.name = "PortalVortex_URP_InwardFlow";
            Image portalImage = portalObject.GetComponent<Image>();
            portalImage.material = GetPortalMaterial(shader);
            GameObject adventure = GameObject.Find("AdventureGateway");
            MenuV2HoverTarget hotspot =
                adventure.GetComponentInChildren<MenuV2HoverTarget>(true);
            portalObject.AddComponent<MenuV2PortalSwirl>()
                .Configure(portalImage, hotspot);

            GameObject spill = UI("CyanSpill_InsideStoneOpening", canvas,
                "Portal_CyanLightSpill_QA.png", 440, 560, 392, 476);
            spill.GetComponent<Image>().color = new Color(1f, 1f, 1f, .65f);
            spill.transform.SetSiblingIndex(adventure.transform.GetSiblingIndex());
        }

        Transform logo = canvas.Find("ApprovedSpellstrikeLogo");
        if (logo != null)
        {
            GameObject glint = UI("CrystalGlint_Only", logo,
                "LogoCrystalGlint_QA.png", 0, 110, 62, 62, true);
            glint.AddComponent<MenuV2CrystalGlint>()
                .Configure(glint.GetComponent<Image>());
        }

        EditorSceneManager.MarkSceneDirty(qa);
        EditorSceneManager.SaveScene(qa);
        AssetDatabase.SaveAssets();
        Debug.Log("[MenuV2] V03 QA refinements saved safely at " + Destination);
    }

    private static Material GetPortalMaterial(Shader shader)
    {
        Material old = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (old != null) return old;
        if (!AssetDatabase.IsValidFolder("Assets/MenuV2/Materials"))
            AssetDatabase.CreateFolder("Assets/MenuV2", "Materials");
        Material material = new Material(shader);
        material.name = "PortalInwardSwirl_QA";
        AssetDatabase.CreateAsset(material, MaterialPath);
        return material;
    }

    private static void EnsureSprite(string name)
    {
        string path = Art + name;
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            throw new System.InvalidOperationException("Missing QA art: " + path);
        if (importer.textureType != TextureImporterType.Sprite ||
            importer.mipmapEnabled || !importer.alphaIsTransparency)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
    }

    private static Sprite GetSprite(string name)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + name);
        if (sprite == null)
            throw new System.InvalidOperationException("Missing imported sprite: " + name);
        return sprite;
    }

    private static void ReplaceHover(RectTransform canvas, string targetName,
        string alignedHover, string labelName, bool labelAbove)
    {
        Transform target = canvas.Find(targetName);
        if (target == null)
            throw new System.InvalidOperationException("Missing V02 target " + targetName);
        if (alignedHover != null)
        {
            Transform highlight = target.Find("Hover_GoldOutline");
            if (highlight == null)
                throw new System.InvalidOperationException("Missing hover for " + targetName);
            Image image = highlight.GetComponent<Image>();
            image.sprite = GetSprite(alignedHover);
            image.preserveAspect = true;
        }

        Transform label = target.Find("HoverLabel");
        if (label == null) return;
        foreach (var text in label.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
            Object.DestroyImmediate(text.gameObject);
        Image background = label.GetComponent<Image>();
        background.sprite = GetSprite("HoverPlaque_" + 
                                      char.ToUpper(labelName[0]) + 
                                      labelName.Substring(1).ToLowerInvariant() +
                                      "_QA.png");
        background.color = Color.white;
        background.preserveAspect = true;
        var oldOutline = label.GetComponent<Outline>();
        if (oldOutline != null)
            Object.DestroyImmediate(oldOutline);
        RectTransform rect = label.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(288f, 70f);
        if (labelAbove)
        {
            RectTransform targetRect = target.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(0, targetRect.sizeDelta.y * .5f + 18f);
        }
    }

    private static void RepositionLabel(RectTransform canvas,
        string targetName, float verticalOffset)
    {
        Transform root = canvas.Find(targetName);
        var rect = root.Find("HoverLabel").GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(0, verticalOffset);
    }

    private static void MoveTarget(RectTransform canvas, string name,
        float x, float y, float width, float height)
    {
        RectTransform rect = canvas.Find(name).GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(x - 1200f, y - 540f);
    }

    private static void AddGroundShadow(RectTransform canvas, string name,
        string precedingTarget, float x, float y, float width, float height)
    {
        GameObject target = GameObject.Find(precedingTarget);
        GameObject shadow = UI(name, canvas, "ContactShadow_QA.png",
            x, y, width, height);
        shadow.transform.SetSiblingIndex(target.transform.GetSiblingIndex());
    }

    private static GameObject UI(string name, Transform parent,
        string sprite, float x, float y, float width, float height,
        bool relative = false)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        var image = obj.GetComponent<Image>();
        image.sprite = GetSprite(sprite);
        image.raycastTarget = false;
        image.preserveAspect = false;
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = relative
            ? new Vector2(x, y)
            : new Vector2(x - 1200f, y - 540f);
        rect.sizeDelta = new Vector2(width, height);
        return obj;
    }
}
