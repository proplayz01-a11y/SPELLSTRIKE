using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Assembles clean, independent production sprites around the existing live Item
/// Selection controls. Operates ONLY on the isolated Wizard's Vault prefab.
/// Run Assembly once; use Upgrade to resync an earlier isolated production
/// layout, and Rebind after Play Mode tuning (Rebind never changes geometry).
/// </summary>
public static class WizardsVaultProductionAssembly
{
    private const string WorkingPrefab = "Assets/Prefabs/UI/WizardsVault_Layered_Working.prefab";
    private const string SpriteFolder = "Assets/Sprites/WizardsVault/Production/";
    private const string ProductionArt = "WizardVaultProductionArt_2400";
    private const string LegacyArt = "WizardVaultArtLayers_2400";

    // All PNGs are independent, text-free sprites. One slot sprite is shared
    // by three slot Images; one row sprite is shared by both passive Buttons.
    private static readonly string[] SpriteNames =
    {
        "header", "header_ribbon", "header_ornaments", "stage_panel", "stage_panel_clean_potions", "stage2_panel_clean_potions_v3", "stage3_panel_clean_potions_v5", "stage_art", "right_panel",
        "slot_background", "passive_row_background", "back_button",
        "continue_button", "sky_sword_icon", "great_attractor_icon",
        "health_potion_icon", "cleansing_potion_icon", "utility_potion_icon",
        "purifying_pearl_icon", "tidecarved_shell_icon",
        "gold_ornament_divider", "gold_padlock_icon"
    };

    private static readonly Color WarmText = new Color(0.15f, 0.10f, 0.065f, 1f);
    private static readonly Color MutedText = new Color(0.37f, 0.28f, 0.18f, 1f);
    private static readonly Color NavyFallback = new Color(.075f, .13f, .235f, .98f);
    private static readonly Color GoldFallback = new Color(.77f, .59f, .30f, .95f);

    // Stage panel art was extracted directly from the approved 1205x1306
    // composition. Keep its artwork and all live overlay coordinates in the
    // same source coordinate space for precise first-pass alignment.
    private const float StageSourceWidth = 1205f;
    private const float StageSourceHeight = 1306f;
    // Approved 1815x866 composite scaled into a 2400x1080 presentation:
    // left panel is ~43% of the connected board, right is ~57%.
    // 760/823 retains the 1205/1306 source-stage art aspect ratio.
    private const float StageWidth = 760f;
    private const float StageHeight = 823f;
    private const float ItemWidth = 1150f;
    // Both source PNGs have substantial transparent outer margins. Their
    // RectTransforms touching still leaves a black seam in GameView. Overlap
    // the actual Image bounds by 115 UI units so the gold frames visually meet.
    private const float PanelOverlap = 115f;
    private const float CompositionWidth = StageWidth + ItemWidth - PanelOverlap;

    [MenuItem("SPELLSTRIKE/UI/Wizard Vault/6a Import Production PNGs As Sprites")]
    private static void ImportSprites()
    {
        CheckEditMode();
        int changed = 0;
        int missing = 0;
        foreach (string name in SpriteNames)
        {
            var importer = AssetImporter.GetAtPath(SpriteFolder + name + ".png") as TextureImporter;
            if (importer == null)
            {
                missing++;
                continue;
            }

            bool rightPanel = name == "right_panel";
            bool passiveRow = name == "passive_row_background";
            Vector4 panelBorder = new Vector4(190f, 190f, 190f, 190f);
            // New downloaded 1993x426 GOLD OUTER FRAME is transparent inside,
            // and has no baked item holder. Protect its corners with 9-slicing.
            Vector4 rowBorder = new Vector4(120f, 90f, 120f, 90f);
            if (importer.textureType == TextureImporterType.Sprite &&
                importer.spriteImportMode == SpriteImportMode.Single &&
                importer.alphaIsTransparency && !importer.mipmapEnabled &&
                (!rightPanel || (importer.spriteBorder == panelBorder &&
                                 Mathf.Approximately(importer.spritePixelsPerUnit, 100f))) &&
                (!passiveRow || (importer.spriteBorder == rowBorder &&
                                 Mathf.Approximately(importer.spritePixelsPerUnit, 300f))))
                continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 4096;
            if (rightPanel)
            {
                // 9-slice the wide inventory panel to retain ornate corners.
                // Source 1448x1086, target 1150x650: stretching as a Simple
                // Image would flatten decorative gems and sculpted filigree.
                // Match the CanvasScaler.referencePixelsPerUnit (100): PPU=1
                // makes each 190px border consume ~19,000 UI units, entirely
                // collapsing the sliced center at a 1150x650 target size.
                importer.spritePixelsPerUnit = 100f;
                importer.spriteBorder = panelBorder;
            }
            if (passiveRow)
            {
                importer.spritePixelsPerUnit = 300f;
                importer.spriteBorder = rowBorder;
            }
            importer.SaveAndReimport();
            changed++;
        }

        Debug.Log("[VAULT PRODUCTION] Imported " + changed + " sprites; " +
                  missing + " PNGs are not available yet in " + SpriteFolder +
                  ". Missing images remain editable placeholders.");
    }

    [MenuItem("SPELLSTRIKE/UI/Wizard Vault/6 Build Production In Isolated Prefab")]
    private static void Assemble()
    {
        CheckEditMode();
        // Import whatever production PNGs already exist so that running menu 6
        // directly also recognizes the approved base panel as a Sprite.
        ImportSprites();
        GameObject root = LoadWorkingPrefab();
        try
        {
            if (root.transform.Find(ProductionArt))
                throw new InvalidOperationException(
                    "Production assembly already exists. Use 6c Upgrade Existing Isolated Working Prefab To Current Approved Layout.");

            ApplyApprovedLayoutToRoot(root.transform);

            if (PrefabUtility.SaveAsPrefabAsset(root, WorkingPrefab) == null)
                throw new InvalidOperationException("Could not save isolated working prefab.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        Debug.Log("[VAULT PRODUCTION] Created clean, independent artwork around existing " +
                  "live Item Selection controls in " + WorkingPrefab + ". " +
                  "Original ItemSelectionUI prefab and ItemSelectionScene were not changed.");
    }

    [MenuItem("SPELLSTRIKE/UI/Wizard Vault/6b Rebind Production Sprites (Keep Layout)")]
    private static void Rebind()
    {
        CheckEditMode();
        ImportSprites();
        GameObject root = LoadWorkingPrefab();
        try
        {
            if (!root.transform.Find(ProductionArt))
                throw new InvalidOperationException("Run 6 Build Production In Isolated Prefab first.");
            CheckControllerContract(root.transform);
            RebindSprites(root.transform);
            if (PrefabUtility.SaveAsPrefabAsset(root, WorkingPrefab) == null)
                throw new InvalidOperationException("Could not save isolated working prefab.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        Debug.Log("[VAULT PRODUCTION] Rebound available production PNGs without touching the " +
                  "working prefab's tuned RectTransforms or the original scene/prefab.");
    }

    [MenuItem("SPELLSTRIKE/UI/Wizard Vault/6c Upgrade Existing Isolated Working Prefab To Current Approved Layout")]
    private static void UpgradeExistingWorkingPrefab()
    {
        CheckEditMode();
        ImportSprites();
        GameObject root = LoadWorkingPrefab();
        try
        {
            // Upgrade only a previous isolated production build. This menu
            // intentionally resynchronizes authored geometry before the user
            // starts adjusting it in Play Mode. Run repeatedly without clones.
            if (root.transform.Find(ProductionArt) == null)
                throw new InvalidOperationException(
                    "No existing production assembly. Run 6 Build Production In Isolated Prefab first.");

            ApplyApprovedLayoutToRoot(root.transform);
            if (PrefabUtility.SaveAsPrefabAsset(root, WorkingPrefab) == null)
                throw new InvalidOperationException("Could not save isolated working prefab.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        Debug.Log("[VAULT PRODUCTION] Upgraded existing isolated working prefab to current approved " +
                  "stage, slot, inventory and decoration layout with current sprites. " +
                  "Existing controller paths, buttons and ScrollRect remain intact. " +
                  "Safe to run 6c again; use 6b after Play Mode tuning.");
    }

    [MenuItem("SPELLSTRIKE/UI/Wizard Vault/6d Split Potion Counters (Keep Other Layout)")]
    private static void SplitPotionCounters()
    {
        CheckEditMode();
        GameObject prefab = LoadWorkingPrefab();
        try
        {
            Transform panel = Require(prefab.transform,
                "WizardVaultContent/Body/StagePage/PotionSummaryPanel");
            ConfigureSeparatePotionCounters(panel);
            if (PrefabUtility.SaveAsPrefabAsset(prefab, WorkingPrefab) == null)
                throw new InvalidOperationException("Could not save isolated working prefab.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }

        // The review scene intentionally holds an unpacked clone of the
        // isolated prefab. Update only its potion panel, not other transforms
        // or scene overrides the user has fine-tuned manually.
        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        if (scene.path == "Assets/Scenes/WizardsVault_UIReview.unity")
        {
            GameObject liveRoot = GameObject.Find("ItemSelectionUI_Prefab");
            if (liveRoot == null || liveRoot.scene != scene)
                throw new InvalidOperationException("Review scene has no expected ItemSelectionUI_Prefab.");
            Transform livePanel = Require(liveRoot.transform,
                "WizardVaultContent/Body/StagePage/PotionSummaryPanel");
            ConfigureSeparatePotionCounters(livePanel);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        }

        Debug.Log("[VAULT PRODUCTION] Created three compact potion columns with " +
                  "six independent TMP labels/counts and smaller artwork in isolated prefab/preview.");
    }

    [MenuItem("SPELLSTRIKE/UI/Wizard Vault/6e Bind Stage 1, 2 and 3 Pages (Keep Layout)")]
    private static void BindStagePages()
    {
        CheckEditMode();
        ImportSprites();

        Sprite stage1 = Sprite("stage_panel_clean_potions");
        Sprite stage2 = Sprite("stage2_panel_clean_potions_v3");
        Sprite stage3 = Sprite("stage3_panel_clean_potions_v5");
        if (stage1 == null || stage2 == null || stage3 == null)
            throw new InvalidOperationException("Missing Stage 1, Stage 2, or Stage 3 approved baked StagePage sprite.");

        GameObject prefab = LoadWorkingPrefab();
        try
        {
            Transform page = Require(prefab.transform, "WizardVaultContent/Body/StagePage");
            EnsureComponent<WizardsVaultStagePagePresenter>(page.gameObject)
                .Configure(stage1, stage2, stage3);
            if (PrefabUtility.SaveAsPrefabAsset(prefab, WorkingPrefab) == null)
                throw new InvalidOperationException("Could not save isolated working prefab with stage pages.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }

        // The UI review scene is an unpacked independent test clone. Bind only
        // its StagePage component without rebuilding the manually tuned layout.
        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        if (scene.path == "Assets/Scenes/WizardsVault_UIReview.unity")
        {
            GameObject live = GameObject.Find("ItemSelectionUI_Prefab");
            if (live == null || live.scene != scene)
                throw new InvalidOperationException("Missing isolated review UI. Original scene untouched.");
            Transform page = Require(live.transform, "WizardVaultContent/Body/StagePage");
            EnsureComponent<WizardsVaultStagePagePresenter>(page.gameObject)
                .Configure(stage1, stage2, stage3);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        }

        Debug.Log("[VAULT STAGES] Bound Stage 1 village, Stage 2 desert and Stage 3 arena baked parchment " +
                  "without changing stage titles, potion TMP paths or layout geometry.");
    }

    private static void ApplyApprovedLayoutToRoot(Transform root)
    {
        // Contract validation happens before any geometry is changed.
        CheckControllerContract(root);
        Transform legacy = root.Find(LegacyArt);
        if (legacy != null)
            legacy.gameObject.SetActive(false);

        EnsureAllCatalogRows(root);
        CreateFrame(root);
        ConfigureContent(root);
        ConfigureHeader(root);
        ConfigureStage(root);
        ConfigureInventory(root);
        ConfigureFooter(root);
        RebindSprites(root);
        // Assert the paths are still valid after rebuilding decoration nodes.
        CheckControllerContract(root);
    }

    private static void CheckEditMode()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before editing prefab artwork.");
    }

    private static GameObject LoadWorkingPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(WorkingPrefab) == null)
            throw new InvalidOperationException("Missing isolated prefab: " + WorkingPrefab +
                ". Use the existing Wizard Vault 2 Create Isolated Working Prefab menu first.");
        return PrefabUtility.LoadPrefabContents(WorkingPrefab);
    }

    private static Transform Require(Transform root, string path)
    {
        Transform found = root.Find(path);
        if (found == null)
            throw new InvalidOperationException("Working prefab is missing required controller path: " + path);
        return found;
    }

    private static T RequireComponent<T>(Transform node) where T : Component
    {
        T component = node.GetComponent<T>();
        if (component == null)
            throw new InvalidOperationException(node.name + " must retain its " + typeof(T).Name);
        return component;
    }

    private static void CheckControllerContract(Transform root)
    {
        const string body = "WizardVaultContent/Body/";
        RequireComponent<TextMeshProUGUI>(Require(root, body + "StagePage/StageTitle"));
        RequireComponent<TextMeshProUGUI>(Require(root,
            body + "StagePage/PotionSummaryPanel/PotionCounts"));
        for (int i = 1; i <= 3; i++)
        {
            Transform slot = Require(root, body + "ItemPage/EquippedSlots/EquippedSlot" + i);
            RequireComponent<Image>(slot);
            RequireComponent<TextMeshProUGUI>(Require(slot, "Text"));
        }

        Transform passiveList = Require(root, body + "ItemPage/PassiveList");
        ScrollRect scroll = RequireComponent<ScrollRect>(passiveList);
        Transform viewport = Require(passiveList, "PassiveListViewport");
        Transform content = Require(viewport, "PassiveListContent");
        if (scroll.viewport != viewport || scroll.content != content)
            throw new InvalidOperationException("Existing PassiveList ScrollRect references must remain intact.");

        // The present source prefab has two prebuilt rows; the runtime catalog
        // has four entries. The other two are created as safe copies on Build.
        foreach (string rowName in new[] { "SkySwordRow", "GreatAttractorRow" })
        {
            Transform row = Require(content, rowName);
            RequireComponent<Button>(row);
            RequireComponent<Image>(row);
            RequireComponent<TextMeshProUGUI>(Require(row, "Title"));
            RequireComponent<TextMeshProUGUI>(Require(row, "Body"));
            RequireComponent<TextMeshProUGUI>(Require(row, "Status"));
        }

        RequireComponent<Button>(Require(root, "WizardVaultContent/Footer/BackButton"));
        RequireComponent<Button>(Require(root, "WizardVaultContent/Footer/ContinueButton"));
    }

    private static void EnsureAllCatalogRows(Transform root)
    {
        Transform content = Require(root,
            "WizardVaultContent/Body/ItemPage/PassiveList/PassiveListViewport/PassiveListContent");
        Transform template = Require(content, "SkySwordRow");
        foreach (PassiveItemInfo item in PassiveItemCatalog.GetAllItems())
        {
            if (content.Find(item.id + "Row") != null)
                continue;

            // A cloned Button retains its Image, targetGraphic, children,
            // LayoutElement, and shared catalog row hierarchy. At runtime
            // CachePrebuiltReferences replaces its button listener as usual.
            GameObject copy = UnityEngine.Object.Instantiate(template.gameObject, content, false);
            copy.name = item.id + "Row";
            Transform row = copy.transform;
            RequireComponent<TextMeshProUGUI>(Require(row, "Title")).text =
                item.displayName.ToUpperInvariant();
            RequireComponent<TextMeshProUGUI>(Require(row, "Body")).text = item.effectSummary;
            RequireComponent<TextMeshProUGUI>(Require(row, "Status")).text = "Locked";
        }
    }

    private static void ConfigureContent(Transform root)
    {
        RectTransform content = (RectTransform)Require(root, "WizardVaultContent");
        content.anchorMin = content.anchorMax = new Vector2(.5f, .5f);
        content.pivot = new Vector2(.5f, .5f);
        // Frame the two approved panels as one continuous board inside the
        // 2400x1080 reference canvas. Keep the bottom footer separate from the
        // two panels so Unity's VerticalLayoutGroup cannot insert a seam.
        content.sizeDelta = new Vector2(CompositionWidth, 880f);
        // Drop the joined board just below the overhanging header artwork:
        // its rail must NOT obscure SELECTED STAGE or the first slot labels.
        content.anchoredPosition = new Vector2(0f, -59f);

        VerticalLayoutGroup contentLayout = RequireComponent<VerticalLayoutGroup>(content);
        // Independent anchors below own the final approved panel/footer
        // geometry. Preserve the component for reversible legacy layouts.
        contentLayout.enabled = false;
        contentLayout.padding = new RectOffset(0, 0, 0, 0);
        contentLayout.spacing = 0f;

        RectTransform body = RequireComponent<RectTransform>(Require(content, "Body"));
        body.anchorMin = body.anchorMax = new Vector2(0f, 1f);
        body.pivot = new Vector2(0f, 1f);
        body.anchoredPosition = new Vector2(0f, -35f);
        body.sizeDelta = new Vector2(CompositionWidth, StageHeight);
        LayoutElement bodySize = RequireComponent<LayoutElement>(body);
        bodySize.minWidth = bodySize.preferredWidth = CompositionWidth;
        bodySize.minHeight = bodySize.preferredHeight = StageHeight;
        HorizontalLayoutGroup bodyLayout = RequireComponent<HorizontalLayoutGroup>(body);
        bodyLayout.enabled = true;
        bodyLayout.padding = new RectOffset(0, 0, 0, 0);
        bodyLayout.spacing = -PanelOverlap;
        bodyLayout.childAlignment = TextAnchor.UpperLeft;
        bodyLayout.childControlWidth = true;
        bodyLayout.childControlHeight = true;
        bodyLayout.childForceExpandWidth = false;
        bodyLayout.childForceExpandHeight = false;

        RectTransform footer = RequireComponent<RectTransform>(Require(content, "Footer"));
        footer.anchorMin = footer.anchorMax = new Vector2(.5f, 0f);
        footer.pivot = new Vector2(.5f, .5f);
        // Footer art has transparent padding, so raise it across the lower
        // frame instead of leaving a black strip between buttons and board.
        footer.anchoredPosition = new Vector2(0f, 54f);
        footer.sizeDelta = new Vector2(1100f, 110f);
        LayoutElement footerSize = RequireComponent<LayoutElement>(footer);
        footerSize.minHeight = footerSize.preferredHeight = 110f;
    }

    private static void ConfigureHeader(Transform root)
    {
        RectTransform header = (RectTransform)Require(root, "EditableVaultHeader");
        header.anchorMin = header.anchorMax = new Vector2(.5f, 1f);
        // The actual 2172x724 PNG is 3:1, with transparent lower padding.
        // Its opaque gold lettering sits on the upper rim of the joined
        // boards when the full banner is authored at its native proportions.
        header.pivot = new Vector2(.5f, .5f);
        header.sizeDelta = new Vector2(1470f, 490f);
        // Lift the ornament 36 units while lowering the board 36 units.
        // The rail then meets the upper edge without covering live text.
        header.anchoredPosition = new Vector2(0f, -171f);
        header.SetAsLastSibling();
        Image background = RequireComponent<Image>(header);
        background.raycastTarget = false;

        TextMeshProUGUI title = RequireComponent<TextMeshProUGUI>(Require(header, "VaultTitleText"));
        title.color = new Color(.96f, .79f, .43f, 1f);
        title.fontSize = 34f;
        TextMeshProUGUI subtitle = RequireComponent<TextMeshProUGUI>(Require(header, "VaultSubtitleText"));
        subtitle.color = new Color(.93f, .74f, .39f, 1f);
        subtitle.fontSize = 16f;
    }

    private static void ConfigureStage(Transform root)
    {
        Transform page = Require(root, "WizardVaultContent/Body/StagePage");
        // Approved 1205x1306 proportions: 760/823 = 0.92345.
        ConfigurePage(page, StageWidth, StageHeight, new RectOffset(0, 0, 0, 0), 0f);
        // The extracted panel already has all the authored visual landmarks.
        // A layout group would move live labels away from those landmarks.
        RequireComponent<VerticalLayoutGroup>(page).enabled = false;

        Transform upperHeading = Require(page, "Heading");
        PlaceInStage(upperHeading, 202f, 110f, 780f, 55f);
        TextMeshProUGUI selectedStage = RequireComponent<TextMeshProUGUI>(upperHeading);
        selectedStage.fontSize = 23f;
        selectedStage.color = WarmText;
        selectedStage.alignment = TextAlignmentOptions.MidlineLeft;

        Transform title = Require(page, "StageTitle");
        TextMeshProUGUI stageTitle = RequireComponent<TextMeshProUGUI>(title);
        stageTitle.fontSize = 40f;
        stageTitle.color = new Color(.085f, .18f, .32f, 1f); // Deep navy.
        stageTitle.alignment = TextAlignmentOptions.MidlineLeft;
        stageTitle.textWrappingMode = TextWrappingModes.Normal;
        // Two lines of 40pt TMP need more than the old 132-source-pixel box.
        // Otherwise Truncate clips 'KINGDOM' despite the correct live string.
        PlaceInStage(title, 200f, 196f, 790f, 157f);

        // A real, clean Stage 1 image will be bound here. Its layout node is a
        // regular child of the old StagePage, leaving every controller path live.
        RectTransform stageArt = NewRect("ProductionStageArtViewport", page);
        PlaceInStage(stageArt, 134f, 720f, 925f, 505f);
        EnsureComponent<RectMask2D>(stageArt.gameObject);
        Image backing = EnsureComponent<Image>(stageArt.gameObject);
        backing.color = new Color(.15f, .22f, .20f, 1f);
        backing.raycastTarget = false;
        RectTransform photo = NewRect("ProductionStageArtImage", stageArt);
        Stretch(photo);
        EnsureComponent<AspectRatioFitter>(photo.gameObject).aspectMode =
            AspectRatioFitter.AspectMode.FitInParent;
        Image photoImage = EnsureComponent<Image>(photo.gameObject);
        photoImage.color = Color.clear;
        photoImage.raycastTarget = false;
        photoImage.preserveAspect = true;

        Transform note = Require(page, "StageNote");
        PlaceInStage(note, 203f, 365f, 785f, 129f);
        TextMeshProUGUI noteText = RequireComponent<TextMeshProUGUI>(note);
        noteText.fontSize = 22f;
        noteText.color = MutedText;
        noteText.alignment = TextAlignmentOptions.TopLeft;
        noteText.textWrappingMode = TextWrappingModes.Normal;

        Transform potions = Require(page, "PotionSummaryPanel");
        PlaceInStage(potions, 184f, 511f, 836f, 206f);
        VerticalLayoutGroup group = RequireComponent<VerticalLayoutGroup>(potions);
        group.enabled = false;
        TextMeshProUGUI counts = RequireComponent<TextMeshProUGUI>(Require(potions, "PotionCounts"));
        counts.fontSize = 23f;
        counts.color = WarmText;
        counts.alignment = TextAlignmentOptions.MidlineLeft;
        counts.characterSpacing = 5f;
        // PotionCounts is a single live TMP string from the controller. Keep
        // the actual text (and updates) intact while positioning it across the
        // three item cells in the source pill band. Start at global source
        // x306 (the pill band's x184 + local x122) to clear the red bottle at
        // x225..280 while leaving one controller-owned live TMP string.
        PlaceInStage(counts.transform, 122f, 105f, 714f, 72f);
        ConfigureSeparatePotionCounters(potions);
        TextMeshProUGUI heading = RequireComponent<TextMeshProUGUI>(Require(potions, "PotionHeader"));
        heading.alignment = TextAlignmentOptions.MidlineLeft;
        heading.color = WarmText;
        heading.fontSize = 23f;
        PlaceInStage(heading.transform, 17f, 20f, 510f, 54f);

        RectTransform icons = NewRect("ProductionPotionIcons", potions);
        PlaceInStage(icons, 0f, 90f, 836f, 115f);
        CreatePotionIcon(icons, "HealthIcon", -170f, new Color(.92f, .24f, .18f, 1f));
        CreatePotionIcon(icons, "CleansingIcon", -30f, new Color(.22f, .50f, 1f, 1f));
        CreatePotionIcon(icons, "UtilityIcon", 110f, new Color(.29f, .79f, .34f, 1f));

        // Retain a standalone scenery layer for a future split-art revision or
        // for projects without the approved stage_panel.png. The source panel
        // already has this village and these potion icons, so RebindSprites
        // disables both duplicates whenever the panel sprite is present.
        stageArt.SetAsLastSibling();
    }

    private static void ConfigureSeparatePotionCounters(Transform potions)
    {
        TextMeshProUGUI legacy = RequireComponent<TextMeshProUGUI>(Require(potions, "PotionCounts"));
        // Controller keeps writing the original combined field. It remains
        // serialized, but invisible, to preserve the existing production logic.
        legacy.enabled = false;
        // The cleaned derivative removes ONLY three baked large potion bottles
        // while retaining the parchment, pill border, and gold separators.
        // Never overwrite the approved original stage_panel.png.
        Transform stage = potions.parent;
        Sprite clean = Sprite("stage_panel_clean_potions");
        if (clean != null)
        {
            Image stageImage = RequireComponent<Image>(stage);
            stageImage.sprite = clean;
            stageImage.color = Color.white;
        }
        bool useIndependentIcons = clean != null;
        // Existing old icon guide stays inactive. Standalone icons are placed
        // in three equal columns and scaled smaller than the prior baked art.
        Transform oldIcons = potions.Find("ProductionPotionIcons");
        if (oldIcons) oldIcons.gameObject.SetActive(false);

        CreatePotionColumn(potions, legacy, "Health", "HealthCountText",
            "HealthLabelText", "HealthPotionIconSmall", "health_potion_icon", 140f,
            useIndependentIcons);
        CreatePotionColumn(potions, legacy, "Purify", "PurifyCountText",
            "PurifyLabelText", "PurifyPotionIconSmall", "cleansing_potion_icon", 420f,
            useIndependentIcons);
        CreatePotionColumn(potions, legacy, "Power Up", "PowerUpCountText",
            "PowerUpLabelText", "PowerUpPotionIconSmall", "utility_potion_icon", 700f,
            useIndependentIcons);
        EnsureComponent<WizardsVaultPotionCounterPresenter>(potions.gameObject).enabled = true;
    }

    private static void CreatePotionColumn(Transform panel, TextMeshProUGUI template,
        string title, string countName, string labelName, string iconName,
        string spriteName, float centerX, bool showIcon)
    {
        // All positions use the 1205x1306 source's potion-strip coordinate
        // system. Each label/count can be edited independently in Unity.
        RectTransform iconRect = NewRect(iconName, panel);
        PlaceInStage(iconRect, centerX - 26f, 78f, 52f, 52f);
        Image icon = EnsureComponent<Image>(iconRect.gameObject);
        icon.sprite = Sprite(spriteName);
        icon.type = Image.Type.Simple;
        icon.preserveAspect = true;
        icon.color = Color.white;
        icon.raycastTarget = false;
        iconRect.gameObject.SetActive(showIcon && icon.sprite != null);

        // Names and live counts share ONE baseline ("Health x20"), rather
        // than stacking name above quantity. The right-aligned name and
        // left-aligned count have an 18-source-pixel gap, while remaining two
        // independently editable TMP objects for future fine-tuning.
        CreatePotionCountLabel(panel, template, labelName, title,
            centerX - 115f, 144f, 157f, 42f, 22f, false);
        CreatePotionCountLabel(panel, template, countName, "x0",
            centerX + 60f, 144f, 70f, 42f, 21f, true);
    }

    private static void CreatePotionCountLabel(Transform parent, TextMeshProUGUI legacy,
        string name, string initialText, float x, float y, float width,
        float height, float fontSize, bool isCount)
    {
        RectTransform node = NewRect(name, parent);
        PlaceInStage(node, x, y, width, height);
        TextMeshProUGUI label = EnsureComponent<TextMeshProUGUI>(node.gameObject);
        label.font = legacy.font;
        label.fontSharedMaterial = legacy.fontSharedMaterial;
        label.fontSize = fontSize;
        label.enableAutoSizing = false;
        label.fontStyle = isCount ? FontStyles.Bold : FontStyles.Normal;
        label.color = WarmText;
        label.alignment = isCount ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.MidlineRight;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Truncate;
        label.characterSpacing = 0f;
        label.raycastTarget = false;
        // Never show TMP's default "New Text" before the first inventory poll.
        label.text = initialText;
        node.gameObject.SetActive(true);
    }

    private static void PlaceInStage(Transform node, float sourceX, float sourceY,
        float sourceWidth, float sourceHeight)
    {
        RectTransform rect = RequireComponent<RectTransform>(node);
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        // Used for children of both StagePage and PotionSummaryPanel; when the
        // parent is the potion group, source coordinates are local to its top.
        float x = StageWidth / StageSourceWidth;
        float y = StageHeight / StageSourceHeight;
        rect.anchoredPosition = new Vector2(sourceX * x, -sourceY * y);
        rect.sizeDelta = new Vector2(sourceWidth * x, sourceHeight * y);
    }

    private static void ConfigureInventory(Transform root)
    {
        Transform page = Require(root, "WizardVaultContent/Body/ItemPage");
        // The approved replacement ItemPage has ornate gold filigree all around
        // the perimeter. Keep the live slots and scroll list safely INSET so
        // they do not paint over its upper compass/side corner ornaments.
        ConfigurePage(page, ItemWidth, StageHeight, new RectOffset(70, 70, 82, 40), 12f);
        Transform heading = Require(page, "Heading");
        SetHeight(heading, 32f);
        // The approved screenshot uses only the ornate frame above the three
        // cards. Retain the old Heading GameObject for safe editor fallback,
        // but prevent its PASSIVE ITEMS text from covering the empty top rim.
        RequireComponent<TextMeshProUGUI>(heading).enabled = false;
        heading.gameObject.SetActive(false);
        Transform slots = Require(page, "EquippedSlots");
        // The replacement master equipped-slot image is essentially square.
        // Give it more height; the Image preserves the source aspect ratio.
        SetHeight(slots, 272f);
        HorizontalLayoutGroup slotsLayout = RequireComponent<HorizontalLayoutGroup>(slots);
        slotsLayout.spacing = 12f;
        slotsLayout.childAlignment = TextAnchor.MiddleCenter;
        slotsLayout.childForceExpandWidth = false;
        for (int i = 1; i <= 3; i++)
        {
            Transform slot = Require(slots, "EquippedSlot" + i);
            LayoutElement element = RequireComponent<LayoutElement>(slot);
            // 3 x 310 + 2 x 12 = 954 within the inset 1010-unit page width.
            // Square source sprites keep their aspect without covering the rim.
            element.minWidth = element.preferredWidth = 310f;
            element.minHeight = element.preferredHeight = 265f;
            TextMeshProUGUI label = RequireComponent<TextMeshProUGUI>(Require(slot, "Text"));
            label.fontSize = 26f;
            label.color = new Color(.96f, .83f, .56f, 1f);
            label.enableAutoSizing = true;
            label.fontSizeMin = 15f;
            label.fontSizeMax = 26f;
            label.alignment = TextAlignmentOptions.Bottom;
            label.textWrappingMode = TextWrappingModes.Normal;
            RectTransform labelRect = RequireComponent<RectTransform>(label.transform);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(18f, 55f);
            labelRect.offsetMax = new Vector2(-18f, -18f);

            // Keep the legacy chip node for reversibility, but the final
            // screenshot uses gold/ivory lettering directly on the navy slot,
            // never a pale rectangle obscuring the ornate blue master sprite.
            RectTransform labelBacking = NewRect("ProductionSlotLabelBacking", slot);
            labelBacking.anchorMin = labelBacking.anchorMax = new Vector2(.5f, 0f);
            labelBacking.pivot = new Vector2(.5f, .5f);
            labelBacking.sizeDelta = new Vector2(215f, 62f);
            labelBacking.anchoredPosition = new Vector2(0f, 49f);
            Image labelBackingImage = EnsureComponent<Image>(labelBacking.gameObject);
            labelBackingImage.color = new Color(.94f, .79f, .48f, .89f);
            labelBackingImage.raycastTarget = false;
            labelBacking.SetAsFirstSibling();
            labelBacking.gameObject.SetActive(false);
        }

        Transform passiveList = Require(page, "PassiveList");
        SetHeight(passiveList, 420f);
        // ScrollRect, viewport, content, ContentSizeFitter and current buttons
        // are deliberately retained. The controller registers their listeners.
        Transform scrollContent = Require(passiveList,
            "PassiveListViewport/PassiveListContent");
        VerticalLayoutGroup rowsLayout = RequireComponent<VerticalLayoutGroup>(scrollContent);
        rowsLayout.spacing = 12f;
        foreach (PassiveItemInfo item in PassiveItemCatalog.GetAllItems())
        {
            Transform row = Require(scrollContent, item.id + "Row");
            // The scrollable list continues below the three taller slot cards.
            SetHeight(row, 180f);
            // The ItemSelectionSceneController changes the Button's own Image
            // alpha for equipped/owned/locked states. Keep the approved frame
            // on a separate rear child so locked items do not lose their navy
            // and gold border when that alpha drops to 0.24 at runtime.
            RectTransform rowArt = NewRect("ProductionPassiveRowArt", row);
            Stretch(rowArt);
            Image rowArtImage = EnsureComponent<Image>(rowArt.gameObject);
            rowArtImage.raycastTarget = false;
            rowArt.SetAsFirstSibling();
            // New transparent empty gold row frame has no built-in square.
            // Add one live navy icon-holder under the independently bound icon,
            // matching the supplied third reference without baking labels.
            RectTransform iconHolder = NewRect("ProductionRowIconHolder", row);
            iconHolder.anchorMin = iconHolder.anchorMax = new Vector2(0f, .5f);
            iconHolder.pivot = new Vector2(.5f, .5f);
            iconHolder.sizeDelta = new Vector2(112f, 112f);
            iconHolder.anchoredPosition = new Vector2(76f, 0f);
            Image iconHolderImage = EnsureComponent<Image>(iconHolder.gameObject);
            iconHolderImage.color = new Color(.035f, .105f, .195f, .95f);
            iconHolderImage.raycastTarget = false;
            Outline holderOutline = EnsureComponent<Outline>(iconHolder.gameObject);
            holderOutline.effectColor = new Color(.87f, .67f, .32f, .98f);
            holderOutline.effectDistance = new Vector2(2.5f, -2.5f);
            holderOutline.useGraphicAlpha = true;
            iconHolder.SetSiblingIndex(1);
            RectTransform icon = NewRect("ProductionPassiveIcon", row);
            icon.anchorMin = icon.anchorMax = new Vector2(0f, .5f);
            icon.pivot = new Vector2(.5f, .5f);
            // Keep the live item icon inside its new gold-edged square.
            icon.sizeDelta = new Vector2(80f, 80f);
            icon.anchoredPosition = new Vector2(76f, 0f);
            Image iconImage = EnsureComponent<Image>(icon.gameObject);
            iconImage.raycastTarget = false;
            iconImage.color = new Color(.70f, .51f, .24f, .25f);
            RectTransform initialsRect = NewRect("FallbackInitial", icon);
            Stretch(initialsRect);
            TextMeshProUGUI initials = EnsureComponent<TextMeshProUGUI>(initialsRect.gameObject);
            initials.text = item.displayName.Substring(0, 1).ToUpperInvariant();
            initials.fontSize = 36f;
            initials.fontStyle = FontStyles.Bold;
            initials.alignment = TextAlignmentOptions.Center;
            initials.color = new Color(.85f, .65f, .32f, 1f);
            initials.raycastTarget = false;

            RectTransform title = (RectTransform)Require(row, "Title");
            title.offsetMin = new Vector2(160f, title.offsetMin.y);
            TextMeshProUGUI titleText = RequireComponent<TextMeshProUGUI>(title);
            titleText.fontSize = 30f;
            RectTransform body = (RectTransform)Require(row, "Body");
            body.offsetMin = new Vector2(160f, body.offsetMin.y);
            RequireComponent<TextMeshProUGUI>(body).fontSize = 20f;
            RectTransform status = (RectTransform)Require(row, "Status");
            status.offsetMin = new Vector2(10f, status.offsetMin.y);
            // Leave a small clear patch at the trailing edge for an independent
            // ornamental lock PNG; the status string remains controller-owned.
            status.offsetMax = new Vector2(-62f, status.offsetMax.y);
            RequireComponent<TextMeshProUGUI>(status).fontSize = 18f;

            RectTransform lockAccent = NewRect("ProductionLockAccent", row);
            lockAccent.anchorMin = lockAccent.anchorMax = new Vector2(1f, .5f);
            lockAccent.pivot = new Vector2(.5f, .5f);
            lockAccent.sizeDelta = new Vector2(49f, 49f);
            lockAccent.anchoredPosition = new Vector2(-30f, 0f);
            Image lockImage = EnsureComponent<Image>(lockAccent.gameObject);
            lockImage.raycastTarget = false;
            lockImage.preserveAspect = true;
            lockImage.color = Color.clear;
            // Decorative padlocks must disappear for equipped/owned items.
            // Inventory state still belongs exclusively to the existing
            // controller; this component reads its live Status TMP label.
            EnsureComponent<WizardsVaultPassiveLockVisual>(row.gameObject);
        }
    }

    private static void ConfigureFooter(Transform root)
    {
        Transform footer = Require(root, "WizardVaultContent/Footer");
        HorizontalLayoutGroup group = RequireComponent<HorizontalLayoutGroup>(footer);
        group.spacing = 42f;
        group.childAlignment = TextAnchor.MiddleCenter;
        foreach (string name in new[] { "BackButton", "ContinueButton" })
        {
            Transform child = Require(footer, name);
            RectTransform rect = RequireComponent<RectTransform>(child);
            rect.sizeDelta = new Vector2(400f, 100f);
            Image buttonImage = RequireComponent<Image>(child);
            buttonImage.color = name == "ContinueButton"
                ? new Color(.74f, .50f, .19f, .98f)
                : new Color(.37f, .22f, .12f, .96f);
            TextMeshProUGUI text = RequireComponent<TextMeshProUGUI>(Require(child, "Text"));
            text.fontSize = 25f;
            text.color = name == "ContinueButton" ? WarmText :
                new Color(.97f, .82f, .57f, 1f);
        }

        // The source connector is a separate GOLD-only ornament, with no blue
        // crystal, and sits centered between the original two live buttons.
        // IgnoreLayout prevents it from becoming a third button-sized cell.
        RectTransform connector = NewRect("ProductionGoldConnector", footer);
        connector.anchorMin = connector.anchorMax = new Vector2(.5f, .5f);
        connector.pivot = new Vector2(.5f, .5f);
        connector.sizeDelta = new Vector2(172f, 112f);
        connector.anchoredPosition = Vector2.zero;
        EnsureComponent<LayoutElement>(connector.gameObject).ignoreLayout = true;
        Image image = EnsureComponent<Image>(connector.gameObject);
        image.raycastTarget = false;
        image.preserveAspect = true;
        image.color = Color.clear;
        connector.SetAsFirstSibling(); // Draw behind buttons; never cover text/clicks.
    }

    private static void ConfigurePage(Transform page, float width, float height,
        RectOffset padding, float spacing)
    {
        LayoutElement size = RequireComponent<LayoutElement>(page);
        size.minWidth = size.preferredWidth = width;
        size.minHeight = size.preferredHeight = height;
        VerticalLayoutGroup group = RequireComponent<VerticalLayoutGroup>(page);
        group.padding = padding;
        group.spacing = spacing;
        group.childForceExpandWidth = true;
        group.childForceExpandHeight = false;
        Image image = RequireComponent<Image>(page);
        image.color = Color.white;
        image.raycastTarget = false;
    }

    private static void SetHeight(Transform node, float height)
    {
        LayoutElement element = RequireComponent<LayoutElement>(node);
        element.minHeight = element.preferredHeight = height;
    }

    private static void CreateFrame(Transform root)
    {
        RectTransform holder = NewRect(ProductionArt, root);
        holder.SetAsFirstSibling();
        Stretch(holder);
        AspectRatioFitter fit = EnsureComponent<AspectRatioFitter>(holder.gameObject);
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = 2400f / 1080f;
        RectTransform frame = NewRect("Frame", holder);
        Stretch(frame);
        Image background = EnsureComponent<Image>(frame.gameObject);
        background.color = Color.clear;
        background.raycastTarget = false;
    }

    private static void CreatePotionIcon(Transform parent, string name, float x, Color fallbackColor)
    {
        RectTransform icon = NewRect(name, parent);
        icon.anchorMin = icon.anchorMax = new Vector2(.5f, .5f);
        icon.pivot = new Vector2(.5f, .5f);
        icon.sizeDelta = new Vector2(31f, 31f);
        icon.anchoredPosition = new Vector2(x, 0f);
        Image image = EnsureComponent<Image>(icon.gameObject);
        image.color = fallbackColor;
        image.raycastTarget = false;
        image.preserveAspect = true;
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        // Idempotent node creation: 6c upgrades previous isolated production
        // layouts without introducing duplicate art, Images or child objects.
        Transform existing = parent.Find(name);
        if (existing != null)
            return RequireComponent<RectTransform>(existing);

        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.layer = parent.gameObject.layer;
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }

    private static Sprite Sprite(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + name + ".png");
    }

    private static void BindImage(Transform node, string name, bool preserveAspect = false,
        Color? fallback = null)
    {
        Image image = RequireComponent<Image>(node);
        Sprite sprite = Sprite(name);
        // Clear inherited/legacy sprites when a clean production sprite has not
        // been imported. Otherwise flattened parchment art leaks into the new
        // layout and missing assets falsely appear complete.
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = preserveAspect;
        image.color = sprite ? Color.white : fallback ?? Color.clear;
        // Preserve each original Button Image's raycastTarget. The live
        // controller relies on those graphics receiving pointer clicks.
    }

    private static void RebindSprites(Transform root)
    {
        Transform holder = Require(root, ProductionArt);
        Transform frame = Require(holder, "Frame");
        // `frame.png` in the downloaded pack is a separate square parchment,
        // NOT the approved 2400x1080 screen frame. Keep the old frame handle
        // for reversibility but NEVER stretch or display that loose asset.
        RequireComponent<Image>(frame).sprite = null;
        frame.gameObject.SetActive(false);
        Transform header = Require(root, "EditableVaultHeader");
        // Codex's approved header is TWO distinct, pixel-preserved PNGs:
        // rear left/right ornaments, then the center ribbon in front. Its
        // source composition is 2692x724, not the old 2172x724 flat image.
        // Preserve the user's Play Mode header height and position, changing
        // only width to maintain one uniform scale across both new layers.
        Sprite ornamentsSprite = Sprite("header_ornaments");
        Sprite ribbonSprite = Sprite("header_ribbon");
        bool useSeparatedHeader = ornamentsSprite != null && ribbonSprite != null;
        Image legacyHeaderImage = RequireComponent<Image>(header);
        if (useSeparatedHeader)
        {
            RectTransform headerRect = RequireComponent<RectTransform>(header);
            float height = headerRect.sizeDelta.y > 0f ? headerRect.sizeDelta.y : 490f;
            float scale = height / 724f;
            headerRect.sizeDelta = new Vector2(2692f * scale, height);
            legacyHeaderImage.sprite = null;
            legacyHeaderImage.color = Color.clear;
            legacyHeaderImage.raycastTarget = false;

            RectTransform ornaments = NewRect("ProductionHeaderOrnaments", header);
            ConfigureSeparatedHeaderLayer(ornaments, ornamentsSprite,
                new Vector2(2665f, 276f) * scale, new Vector2(.5f, 14f) * scale);
            ornaments.SetAsFirstSibling();
            RectTransform ribbon = NewRect("ProductionHeaderRibbon", header);
            ConfigureSeparatedHeaderLayer(ribbon, ribbonSprite,
                new Vector2(1381f, 417f) * scale, new Vector2(.5f, 54.5f) * scale);
            ribbon.SetAsLastSibling();
        }
        else
        {
            BindImage(header, "header", false, NavyFallback);
            Transform oldOrnaments = header.Find("ProductionHeaderOrnaments");
            Transform oldRibbon = header.Find("ProductionHeaderRibbon");
            if (oldOrnaments) oldOrnaments.gameObject.SetActive(false);
            if (oldRibbon) oldRibbon.gameObject.SetActive(false);
        }
        // Both versions include baked title text. Keep the original TMP
        // labels serialized and available as fallback only, never on top.
        bool hasBakedHeaderText = useSeparatedHeader || legacyHeaderImage.sprite != null;
        RequireComponent<TextMeshProUGUI>(Require(header, "VaultTitleText")).enabled = !hasBakedHeaderText;
        RequireComponent<TextMeshProUGUI>(Require(header, "VaultSubtitleText")).enabled = !hasBakedHeaderText;
        Transform stage = Require(root, "WizardVaultContent/Body/StagePage");
        BindImage(stage, Sprite("stage_panel_clean_potions") ? "stage_panel_clean_potions" : "stage_panel",
            false, new Color(.94f, .82f, .60f, .98f));
        bool hasApprovedPanel = RequireComponent<Image>(stage).sprite != null;
        Transform stageArt = Require(stage, "ProductionStageArtViewport");
        Transform photo = Require(stage, "ProductionStageArtViewport/ProductionStageArtImage");
        BindImage(photo, "stage_art", true);
        Image artImage = RequireComponent<Image>(photo);
        if (artImage.sprite != null)
        {
            float ratio = artImage.sprite.rect.width / artImage.sprite.rect.height;
            RequireComponent<AspectRatioFitter>(photo).aspectRatio = ratio;
        }
        // The approved panel already includes the village below y720.
        stageArt.gameObject.SetActive(!hasApprovedPanel);
        Transform potions = Require(stage, "PotionSummaryPanel");
        RequireComponent<Image>(potions).color = hasApprovedPanel
            ? Color.clear : new Color(.83f, .68f, .43f, .42f);
        Transform itemPage = Require(root, "WizardVaultContent/Body/ItemPage");
        BindImage(itemPage, "right_panel", false, NavyFallback);
        Image rightImage = RequireComponent<Image>(itemPage);
        if (rightImage.sprite != null)
        {
            // ImportSprites supplies a 190px 9-slice border: keep ornate
            // corners intact while scaling the navy body to 1150x650.
            rightImage.type = Image.Type.Sliced;
            rightImage.fillCenter = true;
        }

        for (int i = 1; i <= 3; i++)
            BindImage(Require(itemPage, "EquippedSlots/EquippedSlot" + i),
                "slot_background", true, GoldFallback);

        Transform rows = Require(itemPage, "PassiveList/PassiveListViewport/PassiveListContent");
        foreach (PassiveItemInfo item in PassiveItemCatalog.GetAllItems())
        {
            Transform row = Require(rows, item.id + "Row");
            // The Button Image remains the runtime state target; a separate
            // rear Image keeps the approved artwork fully readable for locked
            // and owned rows. A row sprite is shared for all four catalog IDs.
            Image buttonGraphic = RequireComponent<Image>(row);
            buttonGraphic.sprite = null;
            buttonGraphic.type = Image.Type.Simple;
            // This is still Button.targetGraphic (raycastTarget=true), but
            // never a visible slate-blue fill behind the gold-only row sprite.
            // WizardsVaultPassiveLockVisual also restores zero alpha whenever
            // the runtime controller applies equipped/owned/locked row colors.
            buttonGraphic.color = new Color(1f, 1f, 1f, 0f);
            buttonGraphic.raycastTarget = true;
            Transform art = Require(row, "ProductionPassiveRowArt");
            BindImage(art, "passive_row_background", false, NavyFallback);
            bool missingRowSprite = RequireComponent<Image>(art).sprite == null;
            if (!missingRowSprite)
            {
                Image rowArtImage = RequireComponent<Image>(art);
                rowArtImage.type = Image.Type.Sliced;
                rowArtImage.fillCenter = true;
            }
            var rowOutline = EnsureComponent<Outline>(row.gameObject);
            rowOutline.effectColor = new Color(.72f, .51f, .22f, .90f);
            rowOutline.effectDistance = new Vector2(2f, -2f);
            rowOutline.useGraphicAlpha = true;
            rowOutline.enabled = missingRowSprite;
            if (missingRowSprite)
            {
                RequireComponent<TextMeshProUGUI>(Require(row, "Title")).color =
                    new Color(.95f, .83f, .55f, 1f);
                RequireComponent<TextMeshProUGUI>(Require(row, "Body")).color =
                    new Color(.80f, .81f, .82f, 1f);
                RequireComponent<TextMeshProUGUI>(Require(row, "Status")).color =
                    new Color(.85f, .72f, .45f, 1f);
            }
            Transform lockAccent = Require(row, "ProductionLockAccent");
            BindImage(lockAccent, "gold_padlock_icon", true);
            lockAccent.gameObject.SetActive(RequireComponent<Image>(lockAccent).sprite != null);
            string iconName = null;
            switch (item.id)
            {
                case PassiveItemId.SkySword: iconName = "sky_sword_icon"; break;
                case PassiveItemId.GreatAttractor: iconName = "great_attractor_icon"; break;
                case PassiveItemId.PurifyingPearl: iconName = "purifying_pearl_icon"; break;
                case PassiveItemId.TidecarvedShell: iconName = "tidecarved_shell_icon"; break;
            }
            if (iconName == null)
                continue;
            Transform icon = Require(row, "ProductionPassiveIcon");
            BindImage(icon, iconName, true, new Color(.70f, .51f, .24f, .25f));
            Image iconImage = RequireComponent<Image>(icon);
            Require(icon, "FallbackInitial").gameObject.SetActive(iconImage.sprite == null);
        }

        Transform back = Require(root, "WizardVaultContent/Footer/BackButton");
        Transform forward = Require(root, "WizardVaultContent/Footer/ContinueButton");
        BindImage(back, "back_button", false, NavyFallback);
        BindImage(forward, "continue_button", false, GoldFallback);
        // Imported button art includes the words BACK and CONTINUE. Keep the
        // existing Button and Text nodes for listeners and asset fallback.
        RequireComponent<TextMeshProUGUI>(Require(back, "Text")).enabled =
            RequireComponent<Image>(back).sprite == null;
        RequireComponent<TextMeshProUGUI>(Require(forward, "Text")).enabled =
            RequireComponent<Image>(forward).sprite == null;

        Transform connector = Require(root, "WizardVaultContent/Footer/ProductionGoldConnector");
        BindImage(connector, "gold_ornament_divider", true);
        connector.gameObject.SetActive(RequireComponent<Image>(connector).sprite != null);

        Transform potion = Require(stage, "PotionSummaryPanel/ProductionPotionIcons");
        BindPotionIcon(Require(potion, "HealthIcon"), "health_potion_icon", new Color(.93f, .27f, .21f));
        BindPotionIcon(Require(potion, "CleansingIcon"), "cleansing_potion_icon", new Color(.27f, .51f, .94f));
        BindPotionIcon(Require(potion, "UtilityIcon"), "utility_potion_icon", new Color(.30f, .78f, .36f));
        // The blanked source panel retains all 3 colored potion icons. Keep
        // the independent icon handles in the prefab for later revisions,
        // hidden until artwork is split into standalone assets.
        potion.gameObject.SetActive(!hasApprovedPanel);
    }

    private static void ConfigureSeparatedHeaderLayer(RectTransform node, Sprite sprite,
        Vector2 nativeScaledSize, Vector2 nativeScaledPosition)
    {
        node.gameObject.SetActive(true);
        node.anchorMin = node.anchorMax = new Vector2(.5f, .5f);
        node.pivot = new Vector2(.5f, .5f);
        node.anchoredPosition = nativeScaledPosition;
        node.sizeDelta = nativeScaledSize;
        node.localRotation = Quaternion.identity;
        node.localScale = Vector3.one;
        Image image = EnsureComponent<Image>(node.gameObject);
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = Color.white;
        image.raycastTarget = false;
    }

    private static void BindPotionIcon(Transform node, string spriteName, Color fallback)
    {
        BindImage(node, spriteName, true);
        // White preserves color in the icon's PNG. Missing placeholder still
        // uses the semantic H(red), P(blue), U(green) palette.
        Image image = RequireComponent<Image>(node);
        image.color = image.sprite == null ? fallback : Color.white;
    }
}
