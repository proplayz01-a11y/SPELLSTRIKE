using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// One-owner V10 additive polish. Makes the existing actual 3D Tutorial
/// Spellbook rotate and glow-free shadow respond to height, and puts an
/// occasional isolated glint at older Van's painted staff crystal.
/// V09, TutorialScene, gameplay scenes/prefabs and logo art are untouched.
/// </summary>
public static class MainMenuV2BookAndStaffPolishV10
{
    public const string Source =
        "Assets/Scenes/MenuConcepts/MainMenuV2_Prototype_20261031_v09_MinigamesWordmark.unity";
    public const string Destination =
        "Assets/Scenes/MenuConcepts/MainMenuV2_Prototype_20261031_v10_BookStaffPolish.unity";
    const string ShadowSprite = "Assets/MenuV2/Art/Book_GroundShadow_V10.png";
    const string GlintSprite = "Assets/MenuV2/Art/LogoCrystalGlint_QA.png";

    [MenuItem("SPELLSTRIKE/Main Menu V2/Build Book Rotation and Staff Glint (V10)")]
    public static void Build()
    {
        if (System.Diagnostics.Process.GetProcessesByName("VALORANT-Win64-Shipping").Length > 0)
        {
            Debug.LogWarning("[Menu V10] VALORANT active; no modification.");
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            Debug.LogWarning("[Menu V10] Editor busy; no modification.");
            return;
        }
        var scene = SceneManager.GetActiveScene();
        if (scene.path != Source || scene.isDirty)
        {
            Debug.LogError("[Menu V10] Open clean V09 scene first: " + scene.path);
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Destination) != null)
        {
            Debug.LogWarning("[Menu V10] Existing V10 protected; no overwrite.");
            return;
        }
        PrepareSprite(ShadowSprite);
        var shadowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ShadowSprite);
        var glintSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GlintSprite);
        var worldBook = GameObject.Find("TutorialSpellbook3D_MenuVariant");
        var ground = GameObject.Find("BookShadow");
        var van = GameObject.Find("ApprovedOlderVanAert");
        var bookUI = GameObject.Find("Clickable3DSpellbook");
        if (shadowSprite == null || glintSprite == null || worldBook == null ||
            ground == null || van == null || bookUI == null ||
            worldBook.GetComponent<MenuV2BookIdle>() == null)
            throw new InvalidOperationException("V10 source objects/sprites missing.");
        if (GameObject.Find("VanStaffCrystalGlint_V10") != null)
            throw new InvalidOperationException("V10 staff glint already exists.");

        if (!EditorSceneManager.SaveScene(scene, Destination, true))
            throw new InvalidOperationException("V10 scene copy failed");
        scene = EditorSceneManager.OpenScene(Destination, OpenSceneMode.Single);

        worldBook = GameObject.Find("TutorialSpellbook3D_MenuVariant");
        UnityEngine.Object.DestroyImmediate(worldBook.GetComponent<MenuV2BookIdle>());
        worldBook.AddComponent<MenuV2SpellbookSpinV10>();
        // Keep actual tutorial GLB and its isolated camera/materials exactly
        // as they are. The script moves the existing visual root only.

        ground = GameObject.Find("BookShadow");
        Image groundImage = ground.GetComponent<Image>();
        groundImage.sprite = shadowSprite;
        groundImage.color = Color.white;
        groundImage.preserveAspect = true;
        groundImage.raycastTarget = false;
        RectTransform shadowRect = ground.GetComponent<RectTransform>();
        shadowRect.anchoredPosition = new Vector2(1002f, -448f);
        shadowRect.sizeDelta = new Vector2(274f, 79f);
        ground.AddComponent<MenuV2BookShadowPulseV10>();

        van = GameObject.Find("ApprovedOlderVanAert");
        GameObject glint = new GameObject("VanStaffCrystalGlint_V10",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform gr = glint.GetComponent<RectTransform>();
        gr.SetParent(van.transform, false);
        gr.anchorMin = gr.anchorMax = new Vector2(.5f, .5f);
        gr.pivot = new Vector2(.5f, .5f);
        // Measured on the actual source sprite at x≈313,y≈129 of
        // 1122x1402, mapped to the existing 560x700 Van Image:
        gr.anchoredPosition = new Vector2(-124f, 285f);
        gr.sizeDelta = new Vector2(58f, 58f);
        Image sparkle = glint.GetComponent<Image>();
        sparkle.sprite = glintSprite;
        sparkle.color = new Color(.77f, .96f, 1f, 0f);
        sparkle.raycastTarget = false;
        sparkle.preserveAspect = true;
        glint.AddComponent<MenuV2StaffCrystalGlintV10>();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Menu V10] Saved real 3D Spellbook spin, book shadow, " +
            "and staff crystal glint. V09 original protected.");
    }

    static void PrepareSprite(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException(
            "Missing shadow asset: " + path);
        if (importer.textureType == TextureImporterType.Sprite &&
            !importer.mipmapEnabled && importer.alphaIsTransparency) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();
    }
}
