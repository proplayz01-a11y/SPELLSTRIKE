using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

internal static class ItemSelectionHoverInstallerTestOnly
{
    private const string PrefabPath = "Assets/Prefabs/UI/WizardsVault_ItemSelectionRevamp_Test.prefab";
    private const string MaterialPath = "Assets/Minigames/ModeSelectWorkbench/CardGoldSweep.mat";
    private const string LiveArt = "Assets/Sprites/WizardsVault/Production/";
    private const string RevampArt = "Assets/Sprites/ItemSelectionRevamp/";
    private const string HoverArt = RevampArt + "Hover/";
    private const string Footer = "WizardVaultContent/Footer/";
    private const string ItemPage = "WizardVaultContent/Body/ItemPage/";
    private const string Rows = ItemPage + "PassiveList/PassiveListViewport/PassiveListContent/";
    private const string OverlayName = "ItemSelectionGoldReflection";
    private const float SlotHoverScale = 1.04f;
    private const float PassiveIconHoverScale = 1.06f;

    [MenuItem("Tools/SPELLSTRIKE/Item Selection/Apply TEST Mini-Game Hover")]
    private static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Apply the hover setup in Edit Mode.");

        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
            throw new InvalidOperationException($"Mini-Game hover material is missing at {MaterialPath}.");

        GameObject prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var bindings = new List<Binding>
            {
                FindBinding(prefab.transform, Footer + "BackButton", PassiveItemId.None),
                FindBinding(prefab.transform, Footer + "ContinueButton", PassiveItemId.None),
            };
            foreach (PassiveItemInfo item in PassiveItemCatalog.GetAllItems())
                bindings.Add(FindBinding(prefab.transform, Rows + item.id + "Row", item.id));

            for (int slot = 1; slot <= 3; slot++)
                bindings.Add(FindBinding(prefab.transform, $"{ItemPage}EquippedSlots/EquippedSlot{slot}",
                    PassiveItemId.None, equipmentSlot: true));

            // Resolve every target and mask before making or saving prefab changes.
            foreach (Binding binding in bindings)
                Attach(binding, material);

            PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath, out bool success);
            if (!success)
                throw new InvalidOperationException($"Could not save the hover setup to {PrefabPath}.");

            Debug.Log($"[Item Selection] Applied Mini-Game hover to {bindings.Count} controls.", material);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }
    }

    private static Binding FindBinding(Transform root, string path, PassiveItemId item, bool equipmentSlot = false)
    {
        Transform node = root.Find(path);
        if (node == null)
            throw new InvalidOperationException($"Missing Item Selection control at {path}.");

        Image art;
        RectTransform scaleTarget = null;
        float scale = 1f;
        if (equipmentSlot)
        {
            art = node.GetComponent<Image>();
            scaleTarget = node as RectTransform;
            scale = SlotHoverScale;
        }
        else
        {
            Button button = node.GetComponent<Button>();
            if (button == null)
                throw new InvalidOperationException($"Missing Button at {path}.");

            if (item == PassiveItemId.None)
            {
                art = button.targetGraphic as Image;
            }
            else
            {
                art = node.Find("ProductionPassiveRowArt")?.GetComponent<Image>();
                scaleTarget = node.Find("ProductionPassiveIcon") as RectTransform;
                if (scaleTarget == null)
                    throw new InvalidOperationException($"Missing the passive icon at {path}.");
                scale = PassiveIconHoverScale;
            }
        }
        if (art == null || art.sprite == null)
            throw new InvalidOperationException($"Missing the plate Image for {path}.");

        string source = AssetDatabase.GetAssetPath(art.sprite);
        string maskName = source switch
        {
            LiveArt + "back_button.png" => "IS_LiveBack_HoverMask.png",
            LiveArt + "continue_button.png" => "IS_LiveContinue_HoverMask.png",
            LiveArt + "passive_row_background.png" => "IS_LivePassiveCard_HoverMask.png",
            LiveArt + "slot_background.png" => "IS_LiveSlot_HoverMask.png",
            RevampArt + "Buttons/IS_Back.png" => "IS_Back_HoverMask.png",
            RevampArt + "Buttons/IS_Continue.png" => "IS_Continue_HoverMask.png",
            RevampArt + "PassiveCards/IS_Card.png" => "IS_PassiveCard_HoverMask.png",
            RevampArt + "EquipmentSlots/IS_Slot.png" => "IS_Slot_HoverMask.png",
            _ => throw new InvalidOperationException($"No matched gold mask for {source} on {path}."),
        };

        Sprite mask = LoadMask(HoverArt + maskName, art.sprite);
        return new Binding(node, art, item, mask, scaleTarget, scale, equipmentSlot);
    }

    private static Sprite LoadMask(string path, Sprite art)
    {
        if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            throw new InvalidOperationException($"Missing gold border mask at {path}.");

        var textureSettings = new TextureImporterSettings();
        importer.ReadTextureSettings(textureSettings);
        if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single ||
            importer.spriteBorder != art.border || importer.spritePixelsPerUnit != art.pixelsPerUnit ||
            textureSettings.spriteMeshType != SpriteMeshType.FullRect ||
            importer.mipmapEnabled || importer.textureCompression != TextureImporterCompression.Uncompressed ||
            importer.npotScale != TextureImporterNPOTScale.None || importer.maxTextureSize != 4096 ||
            importer.alphaSource != TextureImporterAlphaSource.FromInput || importer.wrapMode != TextureWrapMode.Clamp ||
            importer.filterMode != FilterMode.Bilinear)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = art.border;
            importer.spritePixelsPerUnit = art.pixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 4096;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.ReadTextureSettings(textureSettings);
            textureSettings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(textureSettings);
            importer.SaveAndReimport();
        }

        Sprite mask = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (mask == null)
            throw new InvalidOperationException($"Unity could not import the mask {path} as a Sprite.");
        return mask;
    }

    private static void Attach(Binding binding, Material material)
    {
        Transform parent = binding.Art.transform;
        ItemSelectionHoverRelay relay = binding.Target.GetComponent<ItemSelectionHoverRelay>();
        Transform overlay = parent.Find(OverlayName);
        if (overlay == null)
        {
            var layer = new GameObject(OverlayName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            overlay = layer.transform;
            overlay.SetParent(parent, false);
        }

        RectTransform rect = (RectTransform)overlay;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.SetAsFirstSibling();

        Image image = overlay.GetComponent<Image>();
        image.sprite = binding.Mask;
        image.type = binding.Art.type;
        image.preserveAspect = binding.Art.preserveAspect;
        image.pixelsPerUnitMultiplier = binding.Art.pixelsPerUnitMultiplier;
        image.color = Color.white;
        image.material = material;
        image.raycastTarget = false;
        image.maskable = true;

        ModeSelectGoldBorderHover animation = overlay.GetComponent<ModeSelectGoldBorderHover>();
        if (animation == null)
            animation = overlay.gameObject.AddComponent<ModeSelectGoldBorderHover>();
        animation.SetReferences(image, material);
        animation.SetContinuousHover(true);

        if (binding.EquipmentSlot)
            binding.Art.raycastTarget = true;
        if (relay == null)
            relay = binding.Target.gameObject.AddComponent<ItemSelectionHoverRelay>();
        relay.Configure(animation, image, binding.Item, binding.ScaleTarget, binding.HoverScale);
    }

    private readonly struct Binding
    {
        public readonly Transform Target;
        public readonly Image Art;
        public readonly PassiveItemId Item;
        public readonly Sprite Mask;
        public readonly RectTransform ScaleTarget;
        public readonly float HoverScale;
        public readonly bool EquipmentSlot;

        public Binding(Transform target, Image art, PassiveItemId item, Sprite mask,
            RectTransform scaleTarget, float hoverScale, bool equipmentSlot)
        {
            Target = target;
            Art = art;
            Item = item;
            Mask = mask;
            ScaleTarget = scaleTarget;
            HoverScale = hoverScale;
            EquipmentSlot = equipmentSlot;
        }
    }
}

