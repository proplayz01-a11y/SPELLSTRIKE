using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the APPROVED Wizard's Vault art as separate, addressable Unity Images
/// in an isolated working copy of ItemSelectionUI. No original scene/prefab edit.
/// Flat panel references carry baked text and therefore remain disabled guides.
/// </summary>
public static class WizardsVaultLayerAssembler
{
    private const string PrefabPath = "Assets/Prefabs/UI/WizardsVault_Layered_Working.prefab";
    private const string ImageFolder = "Assets/Sprites/WizardsVault/Layers/";

    private struct Layer
    {
        public string name;
        public int x, y, width, height;
        public bool guide;
        public Layer(string name, int x, int y, int width, int height, bool guide = false)
        {
            this.name = name;
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
            this.guide = guide;
        }
    }

    // Source positions come from the real 1815x866 approved art, uniformly
    // scaled into a 2400x1080 reference frame, not stretched to fill 20:9.
    private static Layer Make(string id, int left, int top, int right, int bottom,
        bool guide = false)
    {
        float scale = 1080f / 866f;
        float offsetX = (2400f - 1815f * scale) * .5f;
        return new Layer(id, Mathf.RoundToInt(offsetX + left * scale),
            Mathf.RoundToInt(top * scale),
            Mathf.RoundToInt((right - left) * scale),
            Mathf.RoundToInt((bottom - top) * scale), guide);
    }

    private static readonly Layer[] Artwork =
    {
        Make("01_frame_trim", 110, 0, 1704, 809),
        Make("02_vault_header", 448, 0, 1365, 190),
        Make("03_stage_panel_reference", 205, 169, 789, 736, true),
        Make("04_inventory_panel_reference", 802, 174, 1595, 736, true),
        Make("05_slot_one_reference", 836, 186, 1064, 362, true),
        Make("06_slot_two_reference", 1075, 186, 1303, 362, true),
        Make("07_slot_three_reference", 1313, 186, 1551, 362, true),
        Make("08_passive_row_one_reference", 819, 379, 1558, 496, true),
        Make("09_passive_row_two_reference", 819, 508, 1558, 621, true),
        Make("10_back_button", 589, 725, 893, 813),
        Make("11_continue_button", 907, 725, 1209, 813),
        Make("12_gold_center_connector", 864, 726, 938, 810),
        Make("13_sky_sword_icon", 843, 400, 926, 479),
        Make("14_great_attractor_icon", 843, 528, 926, 604),
        Make("15_health_potion_icon", 283, 456, 327, 515, true),
        Make("16_mana_potion_icon", 453, 456, 498, 515, true),
        Make("17_utility_potion_icon", 605, 456, 650, 515, true),
        Make("18_stage1_actual_editor_reference", 218, 509, 798, 749, true)
    };

    [MenuItem("SPELLSTRIKE/UI/Wizard Vault/5a Configure PNGs As UI Sprites")]
    private static void ConfigureImportedSprites()
    {
        if (Application.isPlaying)
            throw new InvalidOperationException("Configure sprites only in Edit Mode.");
        int changed = 0, missing = 0;
        foreach (Layer entry in Artwork)
        {
            string path = ImageFolder + entry.name + ".png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!importer)
            {
                missing++;
                continue;
            }
            if (importer.textureType == TextureImporterType.Sprite &&
                importer.spriteImportMode == SpriteImportMode.Single &&
                !importer.mipmapEnabled)
                continue;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();
            changed++;
        }
        Debug.Log("[VAULT LAYERS] Imported/updated " + changed +
                  " PNGs as UI Sprites; missing=" + missing +
                  ". Now run '5 Rebind PNG Sprites In Working Prefab'.");
    }

    [MenuItem("SPELLSTRIKE/UI/Wizard Vault/4 Build Layered Art In Isolated Prefab")]
    private static void Build()
    {
        if (Application.isPlaying ||
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException(
                "Stop Play Mode and save/discard existing scene changes first.");

        if (!AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath))
            throw new InvalidOperationException(
                "Use '2 Create Isolated Working Prefab' first.");

        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        int found = 0;
        try
        {
            var existing = root.transform.Find("WizardVaultArtLayers_2400");
            if (existing)
                throw new InvalidOperationException(
                    "Artwork hierarchy already exists. Refusing to overwrite manual adjustments.");

            RectTransform holder = NewRect("WizardVaultArtLayers_2400", root.transform);
            holder.anchorMin = holder.anchorMax = new Vector2(.5f, .5f);
            holder.pivot = new Vector2(.5f, .5f);
            holder.anchoredPosition = Vector2.zero;
            holder.sizeDelta = new Vector2(2400f, 1080f);
            holder.SetAsLastSibling();

            foreach (Layer entry in Artwork)
            {
                var rect = NewRect(entry.name, holder);
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                rect.pivot = new Vector2(.5f, .5f);
                rect.sizeDelta = new Vector2(entry.width, entry.height);
                rect.anchoredPosition = new Vector2(
                    entry.x + entry.width * .5f - 1200f,
                    540f - entry.y - entry.height * .5f);
                var image = rect.gameObject.AddComponent<Image>();
                image.raycastTarget = false;
                image.color = Color.white;
                image.preserveAspect = true;
                image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                    ImageFolder + entry.name + ".png");
                // Guides have baked stage titles, item statuses or inventory
                // counters. They are intentionally NOT active in live UI.
                rect.gameObject.SetActive(!entry.guide && image.sprite != null);
                if (image.sprite) found++;
            }

            // Place all gold-edged artwork behind the *existing live* controls
            // unless the UI designer explicitly changes ordering later.
            // Static artwork never captures clicks. The original buttons,
            // slots and PassiveItemInventory bindings remain intact.
            if (!PrefabUtility.SaveAsPrefabAsset(root, PrefabPath))
                throw new InvalidOperationException("Could not save isolated art prefab.");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }

        Debug.Log("[VAULT LAYERS] Created " + Artwork.Length +
                  " named, independently editable 2400x1080 artwork nodes in " +
                  PrefabPath + ". Imported sprites found: " + found +
                  ". Missing sprites are invisible until supplied in " + ImageFolder +
                  ". Baked-text guide layers remain disabled. Original UI/scene not touched.");
    }

    [MenuItem("SPELLSTRIKE/UI/Wizard Vault/5 Rebind PNG Sprites In Working Prefab")]
    private static void Rebind()
    {
        if (Application.isPlaying)
            throw new InvalidOperationException("Stop Play Mode before rebinding assets.");
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        int found = 0;
        try
        {
            Transform holder = root.transform.Find("WizardVaultArtLayers_2400");
            if (!holder)
                throw new InvalidOperationException(
                    "Run '4 Build Layered Art In Isolated Prefab' first.");
            foreach (Layer entry in Artwork)
            {
                Transform layer = holder.Find(entry.name);
                if (!layer) continue;
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                    ImageFolder + entry.name + ".png");
                var image = layer.GetComponent<Image>();
                if (sprite && image)
                {
                    image.sprite = sprite;
                    layer.gameObject.SetActive(!entry.guide);
                    found++;
                }
            }
            if (!PrefabUtility.SaveAsPrefabAsset(root, PrefabPath))
                throw new InvalidOperationException("Prefab save failed.");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        Debug.Log("[VAULT LAYERS] Rebound " + found + "/" + Artwork.Length +
                  " art sprites. Reference guides remain disabled, because their text is baked.");
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var child = new GameObject(name, typeof(RectTransform));
        child.layer = LayerMask.NameToLayer("UI");
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }
}
