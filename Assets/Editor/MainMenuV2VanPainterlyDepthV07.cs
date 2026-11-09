using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Non-destructive painterly integration of the approved 2D Van in V07.
/// NOT a heavy comic outline; no Van redesign and no gameplay changes.
/// </summary>
public static class MainMenuV2VanPainterlyDepthV07
{
    public const string Source =
        "Assets/Scenes/MenuConcepts/MainMenuV2_Prototype_20261030_v06_QAPolish.unity";
    public const string Destination =
        "Assets/Scenes/MenuConcepts/MainMenuV2_Prototype_20261030_v07_VanDepth.unity";

    private const string OriginalVan =
        "Assets/MenuV2/Art/VanAert_Approved.png";
    private const string DepthVan =
        "Assets/MenuV2/Art/VanAert_PainterlyDepth_V07.png";
    private const string GroundShadow =
        "Assets/MenuV2/Art/VanGroundContact_V07.png";

    [MenuItem("SPELLSTRIKE/Main Menu V2/Build Painterly Van Integration (V07)")]
    public static void Build()
    {
        if (System.Diagnostics.Process.GetProcessesByName(
                "VALORANT-Win64-Shipping").Length > 0)
        {
            Debug.LogError("[MenuV2 V07] VALORANT running. No changes made.");
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            Debug.LogError("[MenuV2 V07] Editor busy/playing; no changes made.");
            return;
        }
        Scene source = SceneManager.GetActiveScene();
        if (source.path != Source || source.isDirty)
        {
            Debug.LogError("[MenuV2 V07] Open clean saved V06 first. Active=" +
                           source.path + " dirty=" + source.isDirty);
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Destination) != null)
        {
            Debug.LogWarning("[MenuV2 V07] Already exists; no overwrite.");
            return;
        }

        PrepareSprite(DepthVan);
        PrepareSprite(GroundShadow);
        Sprite previous = AssetDatabase.LoadAssetAtPath<Sprite>(OriginalVan);
        Sprite replacement = AssetDatabase.LoadAssetAtPath<Sprite>(DepthVan);
        Sprite shadow = AssetDatabase.LoadAssetAtPath<Sprite>(GroundShadow);
        if (previous == null || replacement == null || shadow == null ||
            previous.rect.size != replacement.rect.size)
            throw new System.InvalidOperationException(
                "V07 sprites absent or Van canvas dimensions changed.");

        // Copy before mutating, ensuring V06 remains byte-identical.
        if (!EditorSceneManager.SaveScene(source, Destination, true))
            throw new System.InvalidOperationException("Couldn't save V07 copy.");
        Scene v07 = EditorSceneManager.OpenScene(Destination, OpenSceneMode.Single);
        Transform van = GameObject.Find("ApprovedOlderVanAert").transform;
        Image vanImage = van.GetComponent<Image>();
        if (vanImage.sprite != previous)
            throw new System.InvalidOperationException(
                "Unexpected Van reference in V06; refusing to swap.");
        vanImage.sprite = replacement;
        vanImage.preserveAspect = true;
        vanImage.raycastTarget = false;
        vanImage.color = Color.white;

        // Match feet and staff-ground contact to the actual painting.
        // Keep it behind Van, and below both front/back orbit layers.
        var vanShadow = GameObject.Find("VanShadow");
        var shadowImage = vanShadow.GetComponent<Image>();
        shadowImage.sprite = shadow;
        shadowImage.color = Color.white;
        shadowImage.preserveAspect = false;
        shadowImage.raycastTarget = false;
        var sr = vanShadow.GetComponent<RectTransform>();
        sr.anchoredPosition = new Vector2(-10f, -518f);
        sr.sizeDelta = new Vector2(290f, 65f);
        if (vanShadow.transform.GetSiblingIndex() >= van.GetSiblingIndex())
            vanShadow.transform.SetSiblingIndex(van.GetSiblingIndex());

        EditorSceneManager.MarkSceneDirty(v07);
        EditorSceneManager.SaveScene(v07);
        Debug.Log("[MenuV2 V07] Painterly Van depth saved: " + Destination +
                  ". V06, source Van, prefabs and all other menu assets untouched.");
    }

    private static void PrepareSprite(string path)
    {
        TextureImporter t = AssetImporter.GetAtPath(path) as TextureImporter;
        if (t == null)
            throw new System.InvalidOperationException("Missing V07 sprite: " + path);
        if (t.textureType != TextureImporterType.Sprite ||
            t.mipmapEnabled || !t.alphaIsTransparency ||
            t.filterMode != FilterMode.Bilinear)
        {
            t.textureType = TextureImporterType.Sprite;
            t.spriteImportMode = SpriteImportMode.Single;
            t.alphaIsTransparency = true;
            t.mipmapEnabled = false;
            t.filterMode = FilterMode.Bilinear;
            t.SaveAndReimport();
        }
    }
}
