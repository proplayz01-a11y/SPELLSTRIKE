using System;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Visible Editor menu installer for the shared gameplay tile presentation.
/// Uses the existing Rodin V11 GLBs and separately rendered front-view PNGs.
/// Does not modify scenes, letter spawning, damage, rarity probability or UI flow.
/// </summary>
public static class RodinGameplayTileInstaller
{
    private const string Root = "Assets/MenuV2/RodinV11/";
    private const string SpriteRoot = "Assets/UI/RodinGameplayTiles/Sprites/";
    private const string CatalogFolder = "Assets/Resources/GameplayTiles";
    private const string CatalogPath = CatalogFolder + "/RodinV11Catalog.asset";
    private const string WorldPrefabPath = "Assets/Prefabs/3d_Tile.prefab";

    [MenuItem("SPELLSTRIKE/Tiles/Install Rodin V11 Gameplay Tiles")]
    public static void Install()
    {
        if (Process.GetProcessesByName("VALORANT-Win64-Shipping").Length != 0)
        {
            UnityEngine.Debug.LogWarning("[RodinGameplay] VALORANT active, refusing changes.");
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            UnityEngine.Debug.LogWarning("[RodinGameplay] Stop Play Mode and finish imports first.");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<RodinTileCatalog>(CatalogPath) != null)
        {
            UnityEngine.Debug.LogWarning("[RodinGameplay] Catalog already exists. Refusing overwrite.");
            return;
        }

        GameObject bronze = Base("Bronze");
        GameObject silver = Base("Silver");
        GameObject gold = Base("Gold");
        if (bronze == null || silver == null || gold == null)
            throw new InvalidOperationException("Missing an imported Rodin V11 shared base.");

        GameObject[] letters = new GameObject[26];
        Sprite[] sprites = new Sprite[26];
        for (int i = 0; i < 26; i++)
        {
            char ch = (char)('A' + i);
            string glyph = ch == 'Q' ? "Qu" : ch.ToString();
            letters[i] = AssetDatabase.LoadAssetAtPath<GameObject>(
                Root + "BlackLetters/SPELLSTRIKE_" + glyph +
                "_Black3D_BothFaces_MobileLean.glb");
            string spritePath = SpriteRoot + "RodinV11_" + ch + ".png";
            var importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
            if (letters[i] == null || importer == null)
                throw new InvalidOperationException(
                    "Missing Rodin letter model or generated PNG: " + ch);

            if (importer.textureType != TextureImporterType.Sprite ||
                importer.mipmapEnabled ||
                importer.textureCompression != TextureImporterCompression.CompressedHQ)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 512;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprites[i] == null)
                throw new InvalidOperationException("Could not import UI sprite " + ch);
        }

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(CatalogFolder))
            AssetDatabase.CreateFolder("Assets/Resources", "GameplayTiles");

        RodinTileCatalog catalog = ScriptableObject.CreateInstance<RodinTileCatalog>();
        catalog.SetEditorReferences(bronze, silver, gold, letters, sprites);
        AssetDatabase.CreateAsset(catalog, CatalogPath);

        GameObject world = null;
        try
        {
            world = PrefabUtility.LoadPrefabContents(WorldPrefabPath);
            if (world.GetComponent<WorldTile>() == null)
                throw new InvalidOperationException("WorldTile missing from shared prefab.");
            if (world.GetComponent<RodinWorldTileVisual>() == null)
                world.AddComponent<RodinWorldTileVisual>();

            if (!PrefabUtility.SaveAsPrefabAsset(world, WorldPrefabPath))
                throw new InvalidOperationException("Could not save shared world tile prefab.");
        }
        finally
        {
            if (world != null) PrefabUtility.UnloadPrefabContents(world);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        UnityEngine.Debug.Log(
            "[RodinGameplay] Installed 26 3D glyph references + 3 shared rarity bodies " +
            "+ 26 cropped 2D-look sprites. Updated shared 3d_Tile prefab only. " +
            "All TileManager UI tile instances now use catalog through existing " +
            "normal combat and target-challenge paths. Verify all game scenes.");
    }

    private static GameObject Base(string rarity)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(
            Root + "SharedBases/SPELLSTRIKE_" + rarity + "_SharedBase_1024.glb");
    }
}
