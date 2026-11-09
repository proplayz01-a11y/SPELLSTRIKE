using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Isolated V08: grounded, more dimensional Van with no comic outline.
/// The V07 scene, approved Van sources, gameplay tiles and all menu
/// interactions stay untouched. Can be run once from a clean V07 scene.
/// </summary>
public static class MainMenuV2VanGroundedV08
{
    public const string Source =
        "Assets/Scenes/MenuConcepts/MainMenuV2_Prototype_20261030_v07_VanDepth.unity";
    public const string Destination =
        "Assets/Scenes/MenuConcepts/MainMenuV2_Prototype_20261031_v08_VanGrounded.unity";
    private const string V07Art = "Assets/MenuV2/Art/VanAert_PainterlyDepth_V07.png";
    private const string V08Art = "Assets/MenuV2/Art/VanAert_IntegratedDepth_V08.png";
    private const string V08Shadow = "Assets/MenuV2/Art/VanGroundContact_Strong_V08.png";

    [MenuItem("SPELLSTRIKE/Main Menu V2/Build Van Stronger Depth and Ground Shadows (V08)")]
    public static void Build()
    {
        if (System.Diagnostics.Process.GetProcessesByName(
            "VALORANT-Win64-Shipping").Length > 0)
        {
            Debug.LogWarning("[MenuV2 V08] VALORANT active; mutation prohibited.");
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            Debug.LogWarning("[MenuV2 V08] Editor busy or playing; no mutation.");
            return;
        }
        Scene current = SceneManager.GetActiveScene();
        if (current.path != Source || current.isDirty)
        {
            Debug.LogWarning("[MenuV2 V08] Open clean, saved V07 first. Current: " +
                current.path + " dirty=" + current.isDirty);
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Destination) != null)
        {
            Debug.LogWarning("[MenuV2 V08] V08 already exists. Not overwriting.");
            return;
        }

        Prepare(V08Art);
        Prepare(V08Shadow);
        Sprite oldVan = AssetDatabase.LoadAssetAtPath<Sprite>(V07Art);
        Sprite newVan = AssetDatabase.LoadAssetAtPath<Sprite>(V08Art);
        Sprite ground = AssetDatabase.LoadAssetAtPath<Sprite>(V08Shadow);
        if (oldVan == null || newVan == null || ground == null ||
            oldVan.rect.size != newVan.rect.size)
            throw new System.InvalidOperationException(
                "V08 sprite import/registration failure. No scene changed.");

        if (!EditorSceneManager.SaveScene(current, Destination, true))
            throw new System.InvalidOperationException("Failed to duplicate clean V07.");

        Scene v08 = EditorSceneManager.OpenScene(Destination, OpenSceneMode.Single);
        Image image = GameObject.Find("ApprovedOlderVanAert").GetComponent<Image>();
        RectTransform van = image.GetComponent<RectTransform>();
        Image shadowImage = GameObject.Find("VanShadow").GetComponent<Image>();
        RectTransform shadow = shadowImage.GetComponent<RectTransform>();
        if (image.sprite != oldVan || shadowImage == null ||
            van.sizeDelta != new Vector2(560f, 700f))
            throw new System.InvalidOperationException(
                "Unexpected V07 Van layout; aborting V08 changes.");

        image.sprite = newVan;
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = false;
        shadowImage.sprite = ground;
        shadowImage.color = Color.white;
        shadowImage.preserveAspect = false;
        shadowImage.raycastTarget = false;

        // Pairs with the V08 shadow atlas:
        // x:940..1440, screen y:-7..133 at native 2400x1080 landscape.
        // Aligned to measured staff + left/right boot image contact points.
        shadow.anchoredPosition = new Vector2(-10f, -477f);
        shadow.sizeDelta = new Vector2(500f, 140f);
        var vanGO = image.gameObject;
        if (shadow.transform.GetSiblingIndex() >= vanGO.transform.GetSiblingIndex())
            shadow.transform.SetSiblingIndex(vanGO.transform.GetSiblingIndex());
        shadowImage.gameObject.name = "VanShadow";

        EditorSceneManager.MarkSceneDirty(v08);
        EditorSceneManager.SaveScene(v08);
        Debug.Log("[MenuV2 V08] Grounded Van saved at " + Destination +
                  "; user-approved V07 remains unchanged.");
    }

    private static void Prepare(string path)
    {
        TextureImporter t = AssetImporter.GetAtPath(path) as TextureImporter;
        if (t == null)
            throw new System.InvalidOperationException("Missing V08 texture: " + path);
        if (t.textureType != TextureImporterType.Sprite ||
            t.spriteImportMode != SpriteImportMode.Single ||
            t.mipmapEnabled || !t.alphaIsTransparency ||
            t.filterMode != FilterMode.Bilinear)
        {
            t.textureType = TextureImporterType.Sprite;
            t.spriteImportMode = SpriteImportMode.Single;
            t.mipmapEnabled = false;
            t.alphaIsTransparency = true;
            t.filterMode = FilterMode.Bilinear;
            t.SaveAndReimport();
        }
    }
}
