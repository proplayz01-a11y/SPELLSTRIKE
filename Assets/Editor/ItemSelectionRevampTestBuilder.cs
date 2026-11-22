using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds only the cloned Item Selection scene/prefab. Production's scene,
/// prefab, StageSelect route, inventory, and controllers are never saved.
/// Run Tools > SPELLSTRIKE > Item Selection > Build Revamp TEST Scene.
/// </summary>
internal static class ItemSelectionRevampTestBuilder
{
    private const string TestPrefab = "Assets/Prefabs/UI/WizardsVault_ItemSelectionRevamp_Test.prefab";
    private const string TestScene = "Assets/Scenes/WizardsVault_ItemSelectionRevamp_Test.unity";
    private const string Sprites = "Assets/Sprites/ItemSelectionRevamp/";
    private const float DesignWidth = 1448f;
    private const float DesignHeight = 1086f;
    private static readonly Color Ink = new Color(0.10f, 0.12f, 0.19f, 1f);
    private static readonly Color MutedInk = new Color(0.29f, 0.23f, 0.17f, 1f);
    private static readonly Color Cream = new Color(0.99f, 0.90f, 0.76f, 1f);
    private static TMP_FontAsset cinzel, alegreya, italic;

    [MenuItem("Tools/SPELLSTRIKE/Item Selection/Build Revamp TEST Scene")]
    private static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Use Edit Mode to build the isolated test scene.");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(TestPrefab) == null ||
            AssetDatabase.LoadAssetAtPath<SceneAsset>(TestScene) == null)
            throw new InvalidOperationException("Test prefab/scene clones not found. Never copy over production.");

        cinzel = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/Fonts/SPELLSTRIKE/Cinzel-Variable SDF.asset");
        alegreya = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/Fonts/SPELLSTRIKE/Alegreya-Variable SDF.asset");
        italic = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/Fonts/SPELLSTRIKE/Alegreya-Italic-Variable SDF.asset");
        if (cinzel == null || alegreya == null || italic == null)
            throw new InvalidOperationException("SPELLSTRIKE TMP font assets are missing.");

        ConfigureSpriteImports();
        BuildTestPrefab();
        BuildTestScene();
        AssetDatabase.SaveAssets();
        bool hoverSuccess = EditorApplication.ExecuteMenuItem(
            "Tools/SPELLSTRIKE/Item Selection/Apply TEST Mini-Game Hover");
        Debug.Log("[ItemSelectionRevamp TEST] Built the isolated scene and prefab. Hover installer " +
                  (hoverSuccess ? "executed." : "menu command unavailable; apply TEST hover separately.") +
                  " Production remains unchanged.");
    }

    private static string P(string group, string name)
        => Sprites + group + "/IS_" + name + ".png";

    private static Sprite SpriteAt(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            throw new InvalidOperationException("Required Item Selection sprite missing: " + path);
        return sprite;
    }

    private static void ConfigureSpriteImports()
    {
        // These are the sprite-editor borders from the delivered asset manifest.
        var borders = new Dictionary<string, Vector4>
        {
            { P("EquipmentSlots", "Slot"), new Vector4(31,31,31,31) },
            { P("EquipmentSlots", "Backing"), new Vector4(24,24,24,24) },
            { P("EquipmentSlots", "EquippedCounterPlate"), new Vector4(29,8,29,8) },
            { P("PassiveCards", "Card"), new Vector4(26,17,26,17) },
            { P("PassiveCards", "LockedBadge"), new Vector4(20,9,20,9) },
            { P("PassiveCards", "EquippedBadge"), new Vector4(20,9,20,9) },
            { P("Potions", "InventoryBacking"), new Vector4(26,26,26,26) },
            { P("Potions", "Cell"), new Vector4(19,19,19,19) },
            { P("Potions", "HeadingPlate"), new Vector4(34,9,34,9) },
            { P("Buttons", "Back"), new Vector4(57,14,57,14) },
            { P("Buttons", "Continue"), new Vector4(57,14,57,14) },
            { P("Scrollbar", "Track"), new Vector4(8,14,8,14) },
            { P("Scrollbar", "Handle"), new Vector4(7,14,7,14) },
        };
        // Sprite importer defaults for newly added loose PNGs are Texture.
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Sprites.TrimEnd('/') });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                continue;
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;
            Vector4 border = borders.TryGetValue(path, out Vector4 value) ? value : Vector4.zero;
            if (importer.textureType == TextureImporterType.Sprite &&
                importer.spriteImportMode == SpriteImportMode.Single &&
                importer.spriteBorder == border &&
                !importer.mipmapEnabled && importer.textureCompression == TextureImporterCompression.Uncompressed)
                continue;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.spriteBorder = border;
            importer.mipmapEnabled = false;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();
        }
        string transparentPath = Sprites + "Decorations/IS_TransparentHit.png";
        if (!File.Exists(transparentPath))
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.SetPixels(new[] { Color.clear, Color.clear, Color.clear, Color.clear });
            texture.Apply();
            File.WriteAllBytes(transparentPath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(transparentPath, ImportAssetOptions.ForceSynchronousImport);
        }
        var hitImporter = AssetImporter.GetAtPath(transparentPath) as TextureImporter;
        if (hitImporter != null && hitImporter.textureType != TextureImporterType.Sprite)
        {
            hitImporter.textureType = TextureImporterType.Sprite;
            hitImporter.spriteImportMode = SpriteImportMode.Single;
            hitImporter.alphaSource = TextureImporterAlphaSource.FromInput;
            hitImporter.alphaIsTransparency = true;
            hitImporter.mipmapEnabled = false;
            hitImporter.SaveAndReimport();
        }
    }

    private static RectTransform Node(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        return rect;
    }

    private static RectTransform Full(Transform parent, string name)
    {
        var rect = Node(parent, name);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static RectTransform At(Transform parent, string name,
        float x, float y, float w, float h)
    {
        // Coordinates are from the approved 1448x1086 reference's top left.
        var rect = Node(parent, name);
        rect.sizeDelta = new Vector2(w, h);
        rect.anchoredPosition = new Vector2(
            x + w * 0.5f - DesignWidth * 0.5f,
            DesignHeight * 0.5f - y - h * 0.5f);
        return rect;
    }

    private static Image ImageAt(Transform parent, string name, string spritePath,
        float x, float y, float w, float h, bool sliced = false)
    {
        var rect = At(parent, name, x, y, w, h);
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = SpriteAt(spritePath);
        image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        image.raycastTarget = false;
        return image;
    }

    private static Image ImageFill(Transform parent, string name, string spritePath, bool sliced)
    {
        var rect = Full(parent, name);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = SpriteAt(spritePath);
        image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        image.raycastTarget = false;
        return image;
    }

    private static TextMeshProUGUI TextAt(Transform parent, string name,
        string value, float x, float y, float w, float h, int size,
        TMP_FontAsset font, TextAlignmentOptions alignment, Color color)
    {
        RectTransform rect = At(parent, name, x, y, w, h);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = size;
        text.text = value;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.enableAutoSizing = true;
        text.fontSizeMax = size;
        text.fontSizeMin = Mathf.Max(12, size * 0.75f);
        text.raycastTarget = false;
        return text;
    }

    private static Button ButtonAt(Transform parent, string name, string spritePath,
        float x, float y, float w, float h)
    {
        Image img = ImageAt(parent, name, spritePath, x, y, w, h, true);
        img.raycastTarget = true;
        var button = img.gameObject.AddComponent<Button>();
        button.targetGraphic = img;
        return button;
    }

    private static void BuildTestPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(TestPrefab);
        try
        {
            for (int index = root.transform.childCount - 1; index >= 0; index--)
                UnityEngine.Object.DestroyImmediate(root.transform.GetChild(index).gameObject);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            if (rootRect == null) throw new InvalidOperationException("Test prefab root must be a RectTransform.");
            rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(DesignWidth, DesignHeight);
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.localScale = Vector3.one;
            if (root.GetComponent<ItemSelectionRevampTestScale>() == null)
                root.AddComponent<ItemSelectionRevampTestScale>();
            RectTransform panel = Full(root.transform, "WizardVaultContent");

            // Gold framing, paper and title art are independent transparent sprites.
            ImageAt(panel, "MainParchment", P("MainFrame","MainParchment"), 58,95,1339,905);
            ImageAt(panel, "OuterFrame", P("MainFrame","OuterFrame"), 24,67,1397,947);
            // The user-approved title is the sole intentional baked text asset.
            // Do not add a second TMP title on top of this ribbon.
            ImageAt(panel, "TitleRibbon", P("MainFrame","TitleRibbon_SolidGold"), 279,-1,893,159);
            TextAt(panel, "SubtitleTMP", "Prepare your loadout before battle.", 447,140,555,55,
                29, italic, TextAlignmentOptions.Center, Ink);
            ImageAt(panel, "SubtitleDividerLeft", P("Decorations","EndDiamondDivider"), 271,161,182,24);
            ImageAt(panel, "SubtitleDividerRight", P("Decorations","EndDiamondDivider"), 1006,161,182,24)
                .rectTransform.localScale = new Vector3(-1,1,1);

            RectTransform body = Full(panel, "Body");
            RectTransform stage = Full(body, "StagePage");
            BuildStage(stage);
            RectTransform itemPage = Full(body, "ItemPage");
            BuildEquipment(itemPage);
            BuildPassives(itemPage);
            BuildUnequipInteractions(itemPage);
            BuildFooter(panel);
            BuildPressedFeedback(panel, itemPage);
            ImageAt(panel, "BottomCrystal", P("Decorations","BottomCrystal"), 682,948,86,82);

            PrefabUtility.SaveAsPrefabAsset(root, TestPrefab, out bool saved);
            if (!saved) throw new InvalidOperationException("Failed to save cloned test prefab.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void BuildStage(RectTransform stage)
    {
        // Preserve the three existing stage selections; use clean scene sprites.
        RectTransform windowMask = At(stage,"StagePreviewMask",126,210,379,181);
        Image maskImg = windowMask.gameObject.AddComponent<Image>();
        maskImg.sprite = SpriteAt(P("StagePreview","WindowMask"));
        maskImg.raycastTarget = false;
        var stencil = windowMask.gameObject.AddComponent<Mask>();
        stencil.showMaskGraphic = false;
        var image = ImageFill(windowMask, "StageArt", "Assets/Sprites/WizardsVault/Production/stage_art.png", false);
        image.preserveAspect = true;
        ImageAt(stage,"StageWindowGoldFrame",P("StagePreview","WindowFrame"),118,204,395,194);
        var stageTitle = TextAt(stage, "StageTitle", "Stage 1: Enchanted Kingdom",
            116,391,404,94,33,cinzel,TextAlignmentOptions.Center,Ink);
        ImageAt(stage, "StageDivider", P("Decorations","StageDivider"), 158,480,316,24);
        TextAt(stage, "StageNote",
            "Choose up to 3 passive items before entering the stage. Potions are carried into battle and used from the in-game Bag.",
            120,503,400,109,24,italic,TextAlignmentOptions.Center,Ink);

        ImageAt(stage,"PotionInventoryBacking",P("Potions","InventoryBacking"),109,651,410,195,true);
        ImageAt(stage,"PotionHeadingPlate",P("Potions","HeadingPlate"),206,614,252,60,true);
        TextAt(stage,"PotionHeading","POTIONS",239,622,185,44,29,cinzel,TextAlignmentOptions.Center,Ink);
        RectTransform potionPanel = Full(stage, "PotionSummaryPanel");
        // Legacy ItemSelectionSceneController still receives its exact old TMP path.
        var backingText = TextAt(potionPanel,"PotionCounts","",112,645,1,1,12,alegreya,
            TextAlignmentOptions.Center,Color.clear);
        backingText.gameObject.SetActive(true);
        string[] names = { "Health", "Purify", "Power-Up" };
        string[] icons = { "HealthIcon", "PurifyIcon", "PowerUpIcon" };
        TMP_Text[] counts = new TMP_Text[3];
        for (int i=0;i<3;i++)
        {
            float x = 122 + 126 * i;
            ImageAt(potionPanel,"PotionCell"+i,P("Potions","Cell"),x,695,126,131,true);
            ImageAt(potionPanel,"PotionIcon"+i,P("Potions",icons[i]),x+35,692,55,81);
            TextAt(potionPanel,"PotionName"+i,names[i],x+5,772,116,27,21,
                alegreya,TextAlignmentOptions.Center,Ink);
            counts[i] = TextAt(potionPanel,"PotionCount"+i,"x0",x+6,798,114,24,21,
                cinzel,TextAlignmentOptions.Center,Ink);
        }

        // The existing presenter can use the Image on its own object. The
        // StageTitle sibling remains owned by ItemSelectionSceneController.
        var stagePresenter = image.gameObject.AddComponent<WizardsVaultStagePagePresenter>();
        stagePresenter.Configure(SpriteAt("Assets/Sprites/WizardsVault/Production/stage_art.png"),
            SpriteAt(P("StagePreview","Stage02Art")), SpriteAt(P("StagePreview","Stage03Art")));
        // Slot/potion display updater is attached to the overall ItemPage later.
        stage.gameObject.AddComponent<ItemSelectionRevampTestPotions>().Configure(counts);
    }

    private static void BuildEquipment(RectTransform itemPage)
    {
        ImageAt(itemPage,"EquipmentBacking",P("EquipmentSlots","Backing"),532,199,804,212,true);
        RectTransform slots = Full(itemPage, "EquippedSlots");
        var slotNames = new TMP_Text[3];
        var emptyLayouts = new GameObject[3];
        var equippedLayouts = new GameObject[3];
        var slotIcons = new Image[3];
        string[] sampleIcons = { "SkySword", "GreatAttractor", "PurifyingPearl" };
        string[] sampleNames = { "Sky Sword", "Attractor", "Pearl" };
        for (int i=0;i<3;i++)
        {
            float x=570+251*i;
            var slot = ImageAt(slots,"EquippedSlot"+(i+1),
                P("EquipmentSlots","Slot"),x,204,219,184,true);
            // The old controller expects this direct child; keep it nonvisual.
            var legacyLabel = TextAt(slot.transform,"Text","Slot "+(i+1)+"\nEmpty",
                570,204,1,1,12,alegreya,TextAlignmentOptions.Center,Color.clear);
            var hidden = legacyLabel.gameObject.AddComponent<CanvasGroup>();
            hidden.alpha = 0f; // controller changes Text.color at runtime
            hidden.interactable = false;
            hidden.blocksRaycasts = false;
            // Keep empty and occupied layouts independent. This means edits
            // to the occupied TMP font/position and Image size/position in
            // the prefab INSPECTOR persist at runtime; gameplay only toggles
            // visibility and updates the item sprite and its short name.
            // Both layouts are inside the slot's hover-scale transform.
            var empty = SlotChild(slot.rectTransform, "EmptyLayout",
                Vector2.zero, new Vector2(219f,184f));
            emptyLayouts[i] = empty.gameObject;
            var occupied = SlotChild(slot.rectTransform, "EquippedLayout",
                Vector2.zero, new Vector2(219f,184f));
            equippedLayouts[i] = occupied.gameObject;

            var heading = SlotChild(empty, "SlotHeading",
                new Vector2(0f, 50f), new Vector2(175f, 43f));
            InlineText(heading, "SLOT "+(i+1),
                29, cinzel, TextAlignmentOptions.Center, Ink);

            var rule = SlotChild(empty, "SlotRule",
                new Vector2(0f, 19f), new Vector2(142f, 8f));
            var ruleImage = rule.gameObject.AddComponent<Image>();
            ruleImage.sprite = SpriteAt(P("Decorations","SlotRule"));
            ruleImage.raycastTarget = false;

            var emptyLabel = SlotChild(empty, "EmptyLabel",
                new Vector2(0f, -26f), new Vector2(195f, 35f));
            InlineText(emptyLabel, "Empty", 27, italic, TextAlignmentOptions.Center, Ink);

            var icon = SlotChild(occupied, "ItemIcon",
                new Vector2(0f, 15f), new Vector2(140f, 117f));
            slotIcons[i] = icon.gameObject.AddComponent<Image>();
            slotIcons[i].sprite = SpriteAt(P("PassiveIcons",sampleIcons[i]));
            slotIcons[i].preserveAspect = true;
            slotIcons[i].raycastTarget = false;

            var name = SlotChild(occupied, "ItemName",
                new Vector2(0f, -65f), new Vector2(195f, 35f));
            slotNames[i] = InlineText(name, sampleNames[i], 26,
                cinzel, TextAlignmentOptions.Center, Ink);
            slotNames[i].enableAutoSizing = false;
            slotNames[i].textWrappingMode = TextWrappingModes.NoWrap;
            empty.gameObject.SetActive(false); // edit-mode preview: equipped
            slot.raycastTarget = true; // hover inspection, not an equip/unequip action
        }
        ImageAt(itemPage,"EquippedCounterBacking",P("EquipmentSlots","EquippedCounterPlate"),
            825,365,304,51,true);
        TMP_Text count=TextAt(itemPage,"EquippedCounter","0 / 3 Equipped",
            846,372,262,42,23,cinzel,TextAlignmentOptions.Center,Cream);
        var dynamic=itemPage.gameObject.AddComponent<ItemSelectionRevampTestDisplay>();
        dynamic.Configure(emptyLayouts,equippedLayouts,slotNames,slotIcons,count,null,
            SpriteAt(P("PassiveIcons","SkySword")),SpriteAt(P("PassiveIcons","GreatAttractor")),
            SpriteAt(P("PassiveIcons","PurifyingPearl")),SpriteAt(P("PassiveIcons","TidecarvedShell")));
    }

    private static RectTransform SlotChild(RectTransform slot, string name,
        Vector2 anchoredPosition, Vector2 size)
    {
        var rect = Node(slot, name);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
        return rect;
    }

    private static void BuildUnequipInteractions(RectTransform itemPage)
    {
        RectTransform content = itemPage.Find(
            "PassiveList/PassiveListViewport/PassiveListContent") as RectTransform;
        if (content == null)
            throw new InvalidOperationException("Test passive list content not found for unequip.");
        for (int i = 0; i < 3; i++)
        {
            Transform slot = itemPage.Find("EquippedSlots/EquippedSlot" + (i + 1));
            if (slot == null)
                throw new InvalidOperationException("Test unequip slot missing: " + (i + 1));
            var gesture = slot.gameObject.AddComponent<ItemSelectionSlotUnequipInput>();
            gesture.Configure(i, content);
        }

        // A quiet second line below the subtitle, away from item names,
        // equipment counter and the Available Passives heading.
        TextMeshProUGUI hint = TextAt(itemPage, "UnequipInstruction",
            "Right-click to unequip", 447, 180, 555, 24, 23,
            italic, TextAlignmentOptions.Center, Ink);
        hint.gameObject.AddComponent<ItemSelectionPlatformHint>();
    }

    private static void BuildPassives(RectTransform itemPage)
    {
        ImageAt(itemPage,"PassivesDividerLeft",P("Decorations","EndDiamondDivider"),548,436,182,24);
        ImageAt(itemPage,"PassivesDividerRight",P("Decorations","EndDiamondDivider"),1131,436,182,24)
            .rectTransform.localScale = new Vector3(-1,1,1);
        TextAt(itemPage,"PassivesHeading","AVAILABLE PASSIVES",747,412,381,56,35,
            cinzel,TextAlignmentOptions.Center,Ink);
        RectTransform list=At(itemPage,"PassiveList",519,482,790,338);
        var scroll=list.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal=false;
        scroll.vertical=true;
        scroll.movementType=ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity=34f;
        scroll.inertia=true;
        RectTransform viewport=Full(list,"PassiveListViewport");
        viewport.gameObject.AddComponent<RectMask2D>();
        RectTransform content=Node(viewport,"PassiveListContent");
        content.anchorMin=new Vector2(0f,1f);
        content.anchorMax=new Vector2(1f,1f);
        content.pivot=new Vector2(0.5f,1f);
        content.anchoredPosition=Vector2.zero;
        content.sizeDelta=new Vector2(0f,0f);
        var layout=content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing=4f;
        layout.childAlignment=TextAnchor.UpperCenter;
        layout.childForceExpandWidth=true;
        layout.childForceExpandHeight=false;
        layout.childControlWidth=true;
        layout.childControlHeight=true;
        var fitter=content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport=viewport;
        scroll.content=content;
        scroll.verticalNormalizedPosition=1;

        int idx=0;
        foreach (PassiveItemInfo item in PassiveItemCatalog.GetAllItems())
        {
            RectTransform row=Node(content,item.id+"Row");
            row.sizeDelta=new Vector2(790,108);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight=108;
            var image=row.gameObject.AddComponent<Image>();
            image.sprite=SpriteAt(Sprites+"Decorations/IS_TransparentHit.png");
            image.raycastTarget=true;
            var button=row.gameObject.AddComponent<Button>();
            button.targetGraphic=image;
            button.transition=Selectable.Transition.None;
            ImageFill(row,"ProductionPassiveRowArt",P("PassiveCards","Card"),true);
            var iconFrame=Node(row,"ProductionIconFrame");
            iconFrame.sizeDelta=new Vector2(112,96);
            iconFrame.anchoredPosition=new Vector2(-326,0);
            var frame=iconFrame.gameObject.AddComponent<Image>();
            frame.sprite=SpriteAt(P("PassiveCards","IconFrame"));
            frame.raycastTarget=false;
            var icon=Node(row,"ProductionPassiveIcon");
            icon.sizeDelta=new Vector2(78,78);
            icon.anchoredPosition=new Vector2(-326,0);
            var iconImage=icon.gameObject.AddComponent<Image>();
            iconImage.sprite=PassiveIcon(item.id);
            iconImage.preserveAspect=true;
            iconImage.raycastTarget=false;
            var title=Node(row,"Title");
            title.sizeDelta=new Vector2(485,39);
            title.anchoredPosition=new Vector2(-18,25);
            TMP_Text titleTMP=InlineText(title,item.displayName.ToUpperInvariant(),28,cinzel,TextAlignmentOptions.Left,Ink);
            var description=Node(row,"Body");
            description.sizeDelta=new Vector2(485,57);
            description.anchoredPosition=new Vector2(-18,-20);
            TMP_Text bodyTMP=InlineText(description,item.effectSummary,23,italic,TextAlignmentOptions.Left,MutedInk);
            var plate=Node(row,"StatePlate");
            plate.sizeDelta=new Vector2(151,53);
            plate.anchoredPosition=new Vector2(298,0);
            Image plateImg=plate.gameObject.AddComponent<Image>();
            plateImg.sprite=SpriteAt(P("PassiveCards","LockedBadge"));
            plateImg.type=Image.Type.Sliced;
            plateImg.raycastTarget=false;
            var status=Node(row,"Status");
            status.sizeDelta=new Vector2(142,48);
            status.anchoredPosition=new Vector2(298,0);
            TMP_Text statusText=InlineText(status,"Locked",22,alegreya,TextAlignmentOptions.Center,Cream);
            var badge=row.gameObject.AddComponent<ItemSelectionRevampTestBadge>();
            badge.Configure(item.id,plateImg,statusText,
                SpriteAt(P("PassiveCards","LockedBadge")),
                SpriteAt(P("PassiveCards","EquippedBadge")));
            idx++;
        }
        // The first three rows are visible; the fourth is reached by wheel,
        // thumb drag, touch drag, or the optional arrows.
        var bar=ImageAt(itemPage,"PassiveScrollbar",P("Scrollbar","Track"),1319,511,29,287,true);
        bar.raycastTarget=true;
        var scrollbar=bar.gameObject.AddComponent<Scrollbar>();
        scrollbar.direction=Scrollbar.Direction.BottomToTop;
        RectTransform sliding=Full(bar.transform,"Sliding Area");
        RectTransform thumb=Node(sliding,"Handle");
        thumb.anchorMin=new Vector2(0f,0.75f);
        thumb.anchorMax=Vector2.one;
        thumb.offsetMin=Vector2.zero;
        thumb.offsetMax=Vector2.zero;
        Image thumbImage=thumb.gameObject.AddComponent<Image>();
        thumbImage.sprite=SpriteAt(P("Scrollbar","Handle"));
        thumbImage.type=Image.Type.Sliced;
        thumbImage.raycastTarget=true;
        scrollbar.targetGraphic=thumbImage;
        scrollbar.handleRect=thumb;
        scroll.verticalScrollbar=scrollbar;
        scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.Permanent;
        var up=ButtonAt(itemPage,"ScrollUp",P("Scrollbar","ArrowUp"),1314,474,39,34);
        var down=ButtonAt(itemPage,"ScrollDown",P("Scrollbar","ArrowDown"),1314,794,39,34);
        up.onClick.AddListener(()=>scroll.verticalNormalizedPosition=
            Mathf.Clamp01(scroll.verticalNormalizedPosition+0.33f));
        down.onClick.AddListener(()=>scroll.verticalNormalizedPosition=
            Mathf.Clamp01(scroll.verticalNormalizedPosition-0.33f));
    }

    private static Sprite PassiveIcon(PassiveItemId id)
    {
        switch(id)
        {
            case PassiveItemId.SkySword:return SpriteAt(P("PassiveIcons","SkySword"));
            case PassiveItemId.GreatAttractor:return SpriteAt(P("PassiveIcons","GreatAttractor"));
            case PassiveItemId.PurifyingPearl:return SpriteAt(P("PassiveIcons","PurifyingPearl"));
            case PassiveItemId.TidecarvedShell:return SpriteAt(P("PassiveIcons","TidecarvedShell"));
            default:throw new ArgumentOutOfRangeException(nameof(id));
        }
    }

    private static TMP_Text InlineText(RectTransform rect,string value,int fontSize,
        TMP_FontAsset font,TextAlignmentOptions alignment,Color tint)
    {
        var text=rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text=value;
        text.font=font;
        text.fontSize=fontSize;
        text.color=tint;
        text.alignment=alignment;
        text.raycastTarget=false;
        text.textWrappingMode=TextWrappingModes.Normal;
        text.enableAutoSizing=true;
        text.fontSizeMin=18;
        text.fontSizeMax=fontSize;
        return text;
    }

    private static void BuildFooter(RectTransform panel)
    {
        RectTransform footer=Full(panel,"Footer");
        ButtonAt(footer,"BackButton",P("Buttons","Back"),293,855,362,90);
        TextAt(footer,"BackLabel","BACK",343,867,263,68,40,cinzel,TextAlignmentOptions.Center,Cream);
        ButtonAt(footer,"ContinueButton",P("Buttons","Continue"),830,855,365,90);
        TextAt(footer,"ContinueLabel","CONTINUE",886,867,253,68,40,
            cinzel,TextAlignmentOptions.Center,Ink);
    }

    private static void BuildPressedFeedback(RectTransform panel, RectTransform itemPage)
    {
        var paths = new List<(Transform target, bool slot)>
        {
            (panel.Find("Footer/BackButton"), false),
            (panel.Find("Footer/ContinueButton"), false),
            (itemPage.Find("ScrollUp"), false),
            (itemPage.Find("ScrollDown"), false),
        };
        Transform content = itemPage.Find("PassiveList/PassiveListViewport/PassiveListContent");
        foreach (PassiveItemInfo passive in PassiveItemCatalog.GetAllItems())
            paths.Add((content.Find(passive.id + "Row"), false));
        for (int i = 1; i <= 3; i++)
            paths.Add((itemPage.Find("EquippedSlots/EquippedSlot" + i), true));

        foreach (var entry in paths)
        {
            if (entry.target == null)
                throw new InvalidOperationException("Missing Item Selection press-feedback target.");
            Image source = entry.target.GetComponent<Image>();
            if (!entry.slot && entry.target.name.EndsWith("Row", StringComparison.Ordinal))
                source = entry.target.Find("ProductionPassiveRowArt")?.GetComponent<Image>();
            if (source == null || source.sprite == null)
                throw new InvalidOperationException("Missing press-feedback sprite for " + entry.target.name);
            var overlay = Full(entry.target, "ItemSelectionPressedTint");
            var image = overlay.gameObject.AddComponent<Image>();
            image.sprite = source.sprite;
            image.type = source.type;
            image.preserveAspect = source.preserveAspect;
            image.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
            image.color = new Color(0.11f, 0.08f, 0.06f, 0f);
            image.raycastTarget = false;
            overlay.SetAsLastSibling();
            var feedback = entry.target.gameObject.AddComponent<ItemSelectionPressedFeedback>();
            feedback.Configure(image, entry.slot);
        }
    }

    private static void BuildTestScene()
    {
        Scene scene=EditorSceneManager.OpenScene(TestScene,OpenSceneMode.Additive);
        try
        {
            ItemSelectionSceneController controller=null;
            Canvas canvas=null;
            foreach(GameObject go in scene.GetRootGameObjects())
            {
                if (canvas==null) canvas=go.GetComponentInChildren<Canvas>(true);
                if (controller==null) controller=go.GetComponentInChildren<ItemSelectionSceneController>(true);
            }
            if (canvas==null || controller==null || controller.prebuiltUiRoot==null)
                throw new InvalidOperationException("Test scene lacks original Canvas, controller, or prefab binding.");
            if (PrefabUtility.GetCorrespondingObjectFromSource(controller.prebuiltUiRoot.gameObject)==null)
                throw new InvalidOperationException("Test prefab instance binding was lost.");

            // Reuse exactly the library background sprite from StageSelectScene.
            Transform existing=canvas.transform.Find("LibraryBackground_RevampTest");
            RectTransform backdrop=existing as RectTransform;
            if (backdrop==null) backdrop=Full(canvas.transform,"LibraryBackground_RevampTest");
            backdrop.anchorMin=Vector2.zero;
            backdrop.anchorMax=Vector2.one;
            backdrop.offsetMin=backdrop.offsetMax=Vector2.zero;
            var bg=backdrop.GetComponent<Image>();
            if (bg==null) bg=backdrop.gameObject.AddComponent<Image>();
            bg.sprite=SpriteAt("Assets/Sprites/HallOfChroniclesV1/library_base_2400x1080.png");
            bg.raycastTarget=false;
            bg.preserveAspect=false;
            backdrop.SetAsFirstSibling();

            RectTransform prefabRoot=controller.prebuiltUiRoot;
            prefabRoot.anchorMin=prefabRoot.anchorMax=new Vector2(0.5f,0.5f);
            prefabRoot.pivot=new Vector2(0.5f,0.5f);
            prefabRoot.anchoredPosition=Vector2.zero;
            prefabRoot.sizeDelta=new Vector2(DesignWidth,DesignHeight);
            prefabRoot.localScale=Vector3.one;
            // The legacy controller's text colors are for a navy UI; override
            // on this test scene only so TMP stays readable on parchment.
            controller.textColor=Ink;
            controller.mutedTextColor=MutedInk;
            controller.goldColor=Ink;
            controller.greenColor=Ink;
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Could not save isolated test scene.");
        }
        finally
        {
            EditorSceneManager.CloseScene(scene,true);
        }
    }
}
