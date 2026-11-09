using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Isolated V05 QA build: geometry-locked GOLD hover images derived from the
/// user's UPDATED ASSETS and a dedicated idle-static/hover-flow portal.
/// The source V04 still uses its original shader/controller without changes.
/// Does not modify build settings, production Main Menu, or earlier prototypes.
/// </summary>
public static class MainMenuV2HoverAndVortexV05
{
    public const string Source =
        "Assets/Scenes/MenuConcepts/MainMenuV2_Prototype_20261030_v04_AdventureGold.unity";
    public const string Destination =
        "Assets/Scenes/MenuConcepts/MainMenuV2_Prototype_20261030_v05_HoverFlow.unity";
    private const string Art = "Assets/MenuV2/Art/";
    private const string HoverOnlyMaterial =
        "Assets/MenuV2/Materials/PortalInwardSwirl_HoverOnly_V05.mat";

    [MenuItem("SPELLSTRIKE/Main Menu V2/Build Registered Hover and Static Idle Portal (V05)")]
    public static void Build()
    {
        if (System.Diagnostics.Process.GetProcessesByName("VALORANT-Win64-Shipping").Length > 0)
        {
            Debug.LogError("[MenuV2 V05] VALORANT is active; stopped.");
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            Debug.LogError("[MenuV2 V05] Cannot build while playing/compiling/importing.");
            return;
        }
        Scene current = SceneManager.GetActiveScene();
        if (current.path != Source || current.isDirty)
        {
            Debug.LogError("[MenuV2 V05] Open clean V04 first. Active: " + current.path);
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Destination) != null)
        {
            Debug.LogWarning("[MenuV2 V05] Scene already exists; refusing to overwrite.");
            return;
        }
        EnsureSprite("Adventure_Arch_Hover_UserApproved_V05.png");
        EnsureSprite("Minigame_Tent_Hover_UserApproved_V05.png");
        EnsureSprite("Options_Workbench_Hover_UserApproved_V05.png");
        Shader hoverOnlyShader =
            Shader.Find("SPELLSTRIKE/MenuV2/PortalInwardSwirlHoverOnly");
        if (hoverOnlyShader == null)
        {
            Debug.LogError("[MenuV2 V05] Hover-only shader not yet imported.");
            return;
        }
        if (!EditorSceneManager.SaveScene(current, Destination, true))
        {
            Debug.LogError("[MenuV2 V05] Could not safely copy V04.");
            return;
        }
        Scene scene = EditorSceneManager.OpenScene(Destination, OpenSceneMode.Single);
        Transform canvas = GameObject.Find("MainMenuV2_Canvas").transform;
        Replace(canvas, "AdventureGateway", "Adventure_Arch_Hover_UserApproved_V05.png");
        Replace(canvas, "MinigameTent", "Minigame_Tent_Hover_UserApproved_V05.png");
        Replace(canvas, "OptionsWorkbench", "Options_Workbench_Hover_UserApproved_V05.png");

        // Replace V04's always-moving vortex WITHIN THIS NEW SCENE ONLY.
        // The shader/material are independent: V04 is not changed.
        Transform vortex = canvas.Find("PortalVortex_URP_InwardFlow");
        MenuV2PortalSwirl previous = vortex != null
            ? vortex.GetComponent<MenuV2PortalSwirl>() : null;
        if (previous == null)
            throw new System.InvalidOperationException(
                "V04 is missing the existing 2.5D vortex controller.");
        Image vortexImage = vortex.GetComponent<Image>();
        Material material = AssetDatabase.LoadAssetAtPath<Material>(HoverOnlyMaterial);
        if (material == null)
        {
            material = new Material(hoverOnlyShader);
            material.name = "PortalInwardSwirl_HoverOnly_V05";
            AssetDatabase.CreateAsset(material, HoverOnlyMaterial);
        }
        vortexImage.material = material;
        Object.DestroyImmediate(previous);
        var adventure = canvas.Find("AdventureGateway");
        MenuV2HoverTarget hover = adventure.GetComponentInChildren<MenuV2HoverTarget>(true);
        vortex.gameObject.AddComponent<MenuV2PortalHoverOnly>()
            .Configure(vortexImage, hover);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[MenuV2 V05] Saved registered hover + hover-only vortex QA copy at " +
                  Destination);
    }

    private static void EnsureSprite(string name)
    {
        TextureImporter importer =
            AssetImporter.GetAtPath(Art + name) as TextureImporter;
        if (importer == null)
            throw new System.InvalidOperationException("Missing approved hover: " + name);
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

    private static void Replace(Transform canvas, string objectName, string art)
    {
        Transform root = canvas.Find(objectName);
        if (root == null)
            throw new System.InvalidOperationException("Missing target: " + objectName);
        Image normal = root.Find("Normal").GetComponent<Image>();
        Image hover = root.Find("Hover_GoldOutline").GetComponent<Image>();
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + art);
        if (sprite == null)
            throw new System.InvalidOperationException("Not imported: " + art);
        if (normal.sprite.rect.size != sprite.rect.size)
            throw new System.InvalidOperationException(
                "Normal/hover texture dimensions mismatch: " + objectName);
        hover.sprite = sprite;
        hover.preserveAspect = normal.preserveAspect;
        hover.raycastTarget = false;
        var normalRect = normal.GetComponent<RectTransform>();
        var hoverRect = hover.GetComponent<RectTransform>();
        if (normalRect.anchoredPosition != hoverRect.anchoredPosition ||
            normalRect.sizeDelta != hoverRect.sizeDelta ||
            normalRect.pivot != hoverRect.pivot)
            throw new System.InvalidOperationException(
                "Unity RectTransform mismatch for " + objectName);
    }
}
