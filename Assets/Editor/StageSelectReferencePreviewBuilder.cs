using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Faithful isolated layer assembly using Ultracode's 18 distinct RGBA assets.
/// Deliberately NEVER modifies the original StageSelectScene or StageSelectManager.
/// Existing progression, locking, vocabulary gate and navigation remain authoritative.
/// </summary>
public static class StageSelectReferencePreviewBuilder
{
    private const string SourceScene = "Assets/Scenes/StageSelectScene.unity";
    private const string ReviewScene = "Assets/Scenes/StageSelectBook_VisualReview.unity";
    private const string CorrectedReviewScene = "Assets/Scenes/StageSelectBook_ReferenceFix.unity";
    private const string PreviewPrefab = "Assets/Prefabs/UI/StageSelectBook_Reference.prefab";
    private const string SpriteFolder = "Assets/Sprites/StageSelectReferenceV1/";

    private const float SourceWidth = 1448f;
    private const float SourceHeight = 1086f;
    private static readonly Color Gold = new Color(.98f,.81f,.49f,1);
    private static readonly Color Ink = new Color(.23f,.16f,.11f,1);
    private static readonly Color Navy = new Color(.055f,.10f,.20f,1);
    private static TMP_FontAsset cinzel;
    private static TMP_FontAsset alegreya;

    private sealed class BuiltNode
    {
        public Button button;
        public Image image;
        public GameObject check;
        public GameObject locked;
    }

    [MenuItem("SPELLSTRIKE/UI/Stage Select/1 Build Isolated Reference Book Preview")]
    public static void BuildIsolated()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("Build in a stable Edit Mode only.");
        var active = EditorSceneManager.GetActiveScene();
        if (active.isDirty)
            throw new InvalidOperationException("Active scene has unsaved user edits. Nothing was changed.");
        if (active.path != SourceScene && active.path != ReviewScene)
            throw new InvalidOperationException("Open StageSelectScene or the isolated StageSelectBook_VisualReview first.");

        ImportReferenceSprites();
        cinzel = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/Fonts/SPELLSTRIKE/Cinzel-Variable SDF.asset");
        alegreya = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/Fonts/SPELLSTRIKE/Alegreya-Variable SDF.asset");
        if (cinzel == null || alegreya == null)
            throw new InvalidOperationException("Expected existing SPELLSTRIKE TMP font assets are missing.");

        if (active.path == SourceScene)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ReviewScene) != null)
                throw new InvalidOperationException("Review scene already exists. Open the isolated review scene explicitly to rebuild it.");
            if (!AssetDatabase.CopyAsset(SourceScene, ReviewScene))
                throw new InvalidOperationException("Failed to clone production scene.");
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(ReviewScene, OpenSceneMode.Single);
        }

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ReviewScene)
            throw new InvalidOperationException("Refusing to change a non-review scene.");

        var canvasObject = GameObject.Find("Canvas");
        var managerObject = GameObject.Find("StageSelectManager");
        var legacy = canvasObject != null
            ? (canvasObject.transform.Find("StageSelectRoot") ??
               canvasObject.transform.Find("OriginalStageSelectRoot_Disabled"))
            : null;
        if (canvasObject == null || managerObject == null || legacy == null)
            throw new InvalidOperationException("Expected production UI shell missing in cloned scene.");
        var manager = managerObject.GetComponent<StageSelectManager>();
        if (manager == null) throw new InvalidOperationException("StageSelectManager missing.");
        // No destructive UI mutation: retain the old root, disabled, for visual
        // comparisons and recovery. Existing review overlay remains active.
        legacy.name = "OriginalStageSelectRoot_Disabled";
        legacy.gameObject.SetActive(false);

        var oldPreview = canvasObject.transform.Find("StageSelectReferenceRoot");
        if (oldPreview != null)
            UnityEngine.Object.DestroyImmediate(oldPreview.gameObject);

        var root = CreateRect(canvasObject.transform, "StageSelectReferenceRoot");
        root.anchorMin = root.anchorMax = new Vector2(.5f,.5f);
        root.pivot = new Vector2(.5f,.5f);
        root.sizeDelta = new Vector2(SourceWidth,SourceHeight);
        root.anchoredPosition = Vector2.zero;
        root.gameObject.AddComponent<StageSelectReferenceFitToCanvas>();

        // Slightly muted blue-grey background belongs to the preview, not
        // the baked PNG. Can later be replaced with the living 3D library.
        var bg = ImageAt(root, "PreviewBackdrop", null, 0,0,SourceWidth,SourceHeight);
        bg.color = new Color(.34f,.43f,.57f,1f);
        bg.rectTransform.SetAsFirstSibling();

        // The newly user-approved version already contains both cartographic
        // parchment pages. Do not paint the earlier standalone page designs
        // over it again, or the mountains/spine alignment will double.
        ImageAt(root,"OrnateOpenBook",Sprite("stage_select_book_with_cartography_v2"),
            28,48,1396,854);
        var leftMap = ImageAt(root,"LeftParchmentCartography",Sprite("stage_select_left_page_design"),
            122,143,565,666);
        var rightMap = ImageAt(root,"RightParchmentCartography",Sprite("stage_select_right_page_design"),
            774,147,574,662);
        leftMap.gameObject.SetActive(false);
        rightMap.gameObject.SetActive(false);

        TextAt(root,"BookOneSubtitle","Book One",292,153,320,22,
            15,FontStyles.Italic,Ink,TextAlignmentOptions.Center,alegreya);
        TextAt(root,"LeftChapterTitle","The Enchanted World",286,174,333,35,
            24,FontStyles.Italic,Ink,TextAlignmentOptions.Center,alegreya);
        TextAt(root,"RightSubtitle","The Journey Continues...",859,154,360,32,
            22,FontStyles.Italic,Ink,TextAlignmentOptions.Center,alegreya);

        // Route elements must be separate objects because StageSelectManager
        // dynamically toggles segments according to unlocked destinations.
        var paths = CreateFullSizeLayer(root,"StageRouteLayer");
        var routes = new GameObject[4];
        var curve = Sprite("stage_path_segment_curve");
        var straight = Sprite("stage_path_segment_straight");
        var nodeSprite = Sprite("stage_path_node");
        routes[0] = Route(paths,"Route1_to_2",curve,390,451,74,93,
            58,0f,nodeSprite,new Vector2(431,449),new Vector2(386,525));
        routes[1] = Route(paths,"Route2_to_3",curve,473,617,379,75,
            0,0f,nodeSprite,new Vector2(480,638),new Vector2(844,670));
        routes[2] = Route(paths,"Route3_to_4",curve,1117,574,92,77,
            -44,0f,nodeSprite,new Vector2(1148,619),new Vector2(1192,574));
        routes[3] = Route(paths,"Route4_to_5",curve,1068,327,102,108,
            46,0f,nodeSprite,new Vector2(1146,409),new Vector2(1104,348));

        // Stage buttons retain the authoritative StageSelectManager listeners.
        var stages = CreateFullSizeLayer(root,"StageIllustrationsAndButtons");
        var names = new [] {
            "Stage 1: Enchanted Kingdom",
            "Stage 2: Sunken Seas",
            "Stage 3: Mythic Ruins",
            "Stage 4: Cursed Gothic",
            "Stage 5: The Epilogue"
        };
        var ids = new [] { "stage1_enchanted_kingdom", "stage2_sunken_seas",
            "stage3_mythic_ruins", "stage4_cursed_gothic","stage5_epilogue" };
        // x, y, illustration width/height, plaque x/y/w, check x/y.
        var artRects = new [] {
            new Rect(278,225,320,173), new Rect(171,520,317,177),
            new Rect(841,546,317,164), new Rect(1023,393,317,171),
            new Rect(871,207,258,155)
        };
        var plaques = new [] {
            new Rect(285,389,294,44), new Rect(224,677,244,45),
            new Rect(843,691,259,45), new Rect(1047,542,245,45),
            new Rect(870,337,247,44)
        };
        var checks = new [] {
            new Vector2(545,262), new Vector2(431,551),
            new Vector2(1029,590), new Vector2(1234,416),
            new Vector2(1060,240)
        };
        var buttons = new Button[5];
        var images = new Image[5];
        var checkObjects = new GameObject[5];
        var lockObjects = new GameObject[5];
        var artSprites = new Sprite[5];
        for (int i=0;i<5;i++)
        {
            var built = Stage(stages,i,names[i],ids[i],artRects[i],plaques[i],checks[i]);
            buttons[i]=built.button;
            images[i]=built.image;
            checkObjects[i]=built.check;
            lockObjects[i]=built.locked;
            artSprites[i]=Sprite(ids[i]);
        }

        // The top ribbon must render above the stage artwork and spine.
        // The baked-title v2 has a taller native silhouette than the original
        // blank ribbon. Give it its own height and move its top above the book
        // rather than vertically squashing the gold trim into a thin bar.
        var header = ImageAt(root,"TopHeaderRibbon",
            Sprite("stage_select_header_baked_title_v2"),295,-56,856,260);
        // The user selected the precise baked-title ribbon. Keep a disabled
        // live TMP slot for any later localization, never render two titles.
        var alternateHeaderText = TextAt(header.rectTransform,"HeaderTitle_AlternateLive",
            "SPELLSTRIKE — SELECT YOUR STAGE",
            82,51,692,61,32,FontStyles.Normal,Gold,TextAlignmentOptions.Center,cinzel);
        alternateHeaderText.gameObject.SetActive(false);

        var bottom = CreateFullSizeLayer(root,"StageInformationPanelLayer");
        ImageAt(bottom,"StageInformationPanelFrame",Sprite("stage_info_panel"),
            36,841,1378,217);
        Image atPreviewBg = ImageAt(bottom,"SelectedStagePreviewBackground",null,
            165,881,148,143);
        atPreviewBg.color = new Color(.05f,.11f,.22f,1);
        var currentPreview = ImageAt(bottom,"SelectedStagePreview",
            Sprite("stage1_enchanted_kingdom"),169,888,137,130);
        currentPreview.preserveAspect = true;
        ImageAt(bottom,"SelectedStagePreviewGoldFrame",Sprite("stage_info_icon_frame"),
            161,876,156,155);
        var infoSlots = new GameObject[5];
        string[] foes = {
            "Bramble Sprite • Stone Sentinel • Cursed Jester",
            "Barnacle Husk • Corsair Phantom • Tide Wraith",
            "Runic Golem • Specter Archer • Minotaur Shade",
            "Plague Wraith • Vampiric Shade • Gargoyle Sentinel",
            "Unknown enemies await..."
        };
        string[] bosses = {
            "Hollow Knight","Drowned Captain","Titan Colossus","Crimson Duchess","Master Wizard"
        };
        string[] notes = {
            "Begin your vocabulary journey in the enchanted kingdom.",
            "Venture into ancient seas and master powerful tile debuffs.",
            "Discover the forgotten ruins and the wizard's secret seal.",
            "Enter the cursed gothic citadel at the edge of darkness.",
            "The final chapter of Van Aert's journey awaits."
        };
        for(int i=0;i<5;i++)
        {
            var info=CreateFullSizeLayer(bottom,"Stage"+(i+1)+"_Info");
            TextAt(info,"SelectedStageTitle",names[i],334,864,646,41,24,
                FontStyles.Normal,Ink,TextAlignmentOptions.MidlineLeft,cinzel);
            TextAt(info,"Enemies","Enemies: "+foes[i],338,909,650,29,16,
                FontStyles.Normal,Ink,TextAlignmentOptions.MidlineLeft,alegreya);
            TextAt(info,"Boss","Boss: "+bosses[i],338,936,650,29,17,
                FontStyles.Bold,Ink,TextAlignmentOptions.MidlineLeft,alegreya);
            TextAt(info,"Description",notes[i],338,966,650,29,16,
                FontStyles.Italic,Ink,TextAlignmentOptions.MidlineLeft,alegreya);
            infoSlots[i]=info.gameObject;
            info.gameObject.SetActive(false);
        }
        // Make the preview readable in Edit Mode before Start() selects a stage.
        infoSlots[0].SetActive(true);
        var enterGraphic=ImageAt(bottom,"EnterStageButton",Sprite("enter_stage_button_blank"),
            979,882,326,99);
        enterGraphic.raycastTarget = true;
        var enterButton=enterGraphic.gameObject.AddComponent<Button>();
        enterButton.targetGraphic=enterGraphic;
        TextAt(enterGraphic.rectTransform,"LiveEnterStageText","Enter Stage",
            34,23,260,52,29,FontStyles.Normal,Ink,TextAlignmentOptions.Center,cinzel);

        // The old warning popup lives under the disabled legacy root.
        // Provide a new one and let the unchanged manager activate it.
        var popup=CreateFullSizeLayer(root,"LockWarningPopup");
        var overlay=ImageAt(popup,"WarningPlate",null,479,470,490,130);
        overlay.color=new Color(.05f,.10f,.19f,.96f);
        TextAt(popup,"WarningText","Stage Locked!",518,491,410,90,
            30,FontStyles.Bold,Gold,TextAlignmentOptions.Center,cinzel);
        popup.gameObject.SetActive(false);

        manager.stageButtons=buttons;
        manager.stageNodeImages=images;
        manager.lockOverlays=lockObjects;
        manager.checkmarks=checkObjects;
        manager.pathSegments=routes;
        manager.sparkleEffects=new ParticleSystem[5];
        manager.stageInfoPanels=infoSlots;
        manager.stagePreviewSprites=artSprites;
        manager.infoStagePreview=currentPreview;
        manager.enterButton=enterButton;
        manager.lockWarningPopup=popup.gameObject;

        ApplyReferenceLayout(root, false);

        // Save an isolated reusable prefab, connect the preview instance, then
        // save the review scene. Original scene and prefabs remain unchanged.
        PrefabUtility.SaveAsPrefabAssetAndConnect(root.gameObject,PreviewPrefab,
            InteractionMode.AutomatedAction);
        // The manager belongs to this scene, outside the reusable UI prefab.
        // Bind after saving so the scene retains a persistent external target.
        BindEnterButtonPersistently(enterButton, manager);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject=root.gameObject;
        Debug.Log("[STAGE SELECT REFERENCE] Isolated review scene and separate-layer prefab built. "+
            "Production StageSelectScene remains unchanged.");
    }

    [MenuItem("SPELLSTRIKE/UI/Stage Select/2 Apply Reference Layout Corrections")]
    public static void ApplyReferenceLayoutCorrections()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("Apply layout corrections in stable Edit Mode only.");
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ReviewScene && scene.path != CorrectedReviewScene)
            throw new InvalidOperationException("Open an isolated StageSelectBook review scene first.");
        if (scene.isDirty)
            throw new InvalidOperationException("Review scene has unsaved user edits. Nothing was changed.");

        RectTransform root = null;
        StageSelectManager manager = null;
        foreach (var sceneRoot in scene.GetRootGameObjects())
        {
            foreach (var candidate in sceneRoot.GetComponentsInChildren<RectTransform>(true))
                if (candidate.name == "StageSelectReferenceRoot") root = candidate;
            foreach (var candidate in sceneRoot.GetComponentsInChildren<StageSelectManager>(true))
                if (candidate.name == "StageSelectManager") manager = candidate;
        }
        if (root == null || manager == null)
            throw new InvalidOperationException("Expected reference root and existing manager are missing.");

        // Validate every required object and dependency before the first mutation.
        LoadReferenceFonts();
        Sprite("stage_path_node");
        ValidateLayoutTargets(root);
        var enter = RequiredRect(root,"StageInformationPanelLayer/EnterStageButton")
            .GetComponent<Button>();
        if (enter == null || manager.enterButton != enter)
            throw new InvalidOperationException("Existing Enter button binding does not match this reference root.");

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply Stage Select Reference Layout Corrections");
        Undo.RegisterFullObjectHierarchyUndo(root.gameObject,
            "Apply Stage Select Reference Layout Corrections");
        try
        {
            ApplyReferenceLayout(root, true);
            BindEnterButtonPersistently(enter, manager);
            foreach (var component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null) continue;
                EditorUtility.SetDirty(component);
                if (PrefabUtility.IsPartOfPrefabInstance(component))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = root.gameObject;
            SceneView.RepaintAll();
            Debug.Log("[STAGE SELECT REFERENCE] Layout corrected in place with Undo. " +
                "Review scene is dirty for inspection and has NOT been saved. " +
                "Existing progression, selection, and review references were retained.", root);
        }
        catch
        {
            Undo.RevertAllDownToGroup(undoGroup);
            throw;
        }
        finally
        {
            Undo.CollapseUndoOperations(undoGroup);
        }
    }

    [MenuItem("SPELLSTRIKE/UI/Stage Select/3 Fix Information Title Clipping %#F8")]
    public static void FixInformationTitleClipping()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("Fix title clipping in stable Edit Mode only.");
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ReviewScene && scene.path != CorrectedReviewScene)
            throw new InvalidOperationException("Open an isolated StageSelectBook review scene first.");
        RectTransform root = null;
        foreach (var sceneRoot in scene.GetRootGameObjects())
            foreach (var candidate in sceneRoot.GetComponentsInChildren<RectTransform>(true))
                if (candidate.name == "StageSelectReferenceRoot") root = candidate;
        if (root == null) throw new InvalidOperationException("Reference root missing.");
        var titles = new RectTransform[5];
        for (int i = 0; i < titles.Length; i++)
            titles[i] = RequiredRect(root,"StageInformationPanelLayer/Stage" + (i+1) + "_Info/SelectedStageTitle");
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Fix Stage Information Title Clipping");
        foreach (var title in titles)
        {
            Undo.RecordObject(title,"Fix Stage Information Title Clipping");
            // Alegreya 28pt requires 38.108px; 38px triggers TMP Ellipsis.
            // Leave every user-adjusted position and all other properties alone.
            title.sizeDelta = new Vector2(title.sizeDelta.x,44f);
            if (PrefabUtility.IsPartOfPrefabInstance(title))
                PrefabUtility.RecordPrefabInstancePropertyModifications(title);
            EditorUtility.SetDirty(title);
        }
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[STAGE SELECT REFERENCE] Five title boxes increased to 44px; other layout retained. Save the review copy after inspection.");
    }

    private static void LoadReferenceFonts()
    {
        cinzel = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/Fonts/SPELLSTRIKE/Cinzel-Variable SDF.asset");
        alegreya = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/Fonts/SPELLSTRIKE/Alegreya-Variable SDF.asset");
        if (cinzel == null || alegreya == null)
            throw new InvalidOperationException("Expected existing SPELLSTRIKE TMP font assets are missing.");
    }

    private static RectTransform RequiredRect(Transform parent, string path)
    {
        var target = parent.Find(path) as RectTransform;
        if (target == null)
            throw new InvalidOperationException("Reference layout object missing: " + path);
        return target;
    }

    private static void ValidateLayoutTargets(RectTransform root)
    {
        RequiredRect(root,"StageInformationPanelLayer/StageInformationPanelFrame");
        RequiredRect(root,"StageInformationPanelLayer/SelectedStagePreviewBackground");
        RequiredRect(root,"StageInformationPanelLayer/SelectedStagePreview");
        RequiredRect(root,"StageInformationPanelLayer/SelectedStagePreviewGoldFrame");
        RequiredRect(root,"StageInformationPanelLayer/EnterStageButton/LiveEnterStageText");
        for (int i = 1; i <= 5; i++)
        {
            string stage = "StageIllustrationsAndButtons/Stage" + i + "_Button/";
            foreach (string child in new[] { "StageIllustration", "StageNamePlaque",
                "StageNameTMP", "CompletionBadge", "LockedStateOverlay", "LockedStateOverlay/LockedLabel" })
                RequiredRect(root,stage + child);
            var art = RequiredRect(root,stage + "StageIllustration").GetComponent<Image>();
            if (art == null || art.sprite == null)
                throw new InvalidOperationException("Stage " + i + " artwork sprite is missing.");
            string info = "StageInformationPanelLayer/Stage" + i + "_Info/";
            foreach (string child in new[] { "SelectedStageTitle", "Enemies", "Boss", "Description" })
            {
                if (RequiredRect(root,info + child).GetComponent<TextMeshProUGUI>() == null)
                    throw new InvalidOperationException("Expected live TMP component missing: " + info + child);
            }
        }
        foreach (string route in new[] { "Route1_to_2", "Route2_to_3", "Route3_to_4", "Route4_to_5" })
            RequiredRect(root,"StageRouteLayer/" + route);
    }

    private static void ApplyReferenceLayout(RectTransform root, bool registerCreatedUndo)
    {
        if (alegreya == null || cinzel == null) LoadReferenceFonts();
        var originalArt = new[] {
            new Rect(278,229,317,177), new Rect(169,523,315,177),
            new Rect(839,548,317,172), new Rect(1032,394,314,171),
            new Rect(874,203,251,153)
        };
        var plaques = new[] {
            new Rect(297,398,292,44), new Rect(224,699,265,39),
            new Rect(878,710,251,40), new Rect(1075,558,251,42),
            new Rect(887,347,247,43)
        };
        var badgeCenters = new[] {
            new Vector2(560,271), new Vector2(440,568), new Vector2(1057.5f,607.5f),
            new Vector2(1274.5f,425.5f), new Vector2(1083.5f,241)
        };
        for (int i = 0; i < 5; i++)
        {
            var button = RequiredRect(root,"StageIllustrationsAndButtons/Stage" + (i+1) + "_Button");
            var illustration = RequiredRect(button,"StageIllustration").GetComponent<Image>();
            // These exports retain about 16 transparent pixels per edge. Fit
            // the visible width, and keep the original sprite's natural aspect.
            Vector2 native = illustration.sprite.rect.size;
            float width = originalArt[i].width * native.x / Mathf.Max(1f,native.x - 32f);
            float height = width * native.y / Mathf.Max(1f,native.x);
            Vector2 center = originalArt[i].center;
            var art = new Rect(center.x-width*.5f,center.y-height*.5f,width,height);
            var badge = new Rect(badgeCenters[i].x-30f,badgeCenters[i].y-30f,60f,60f);
            var bound = Rect.MinMaxRect(Mathf.Min(art.xMin,plaques[i].xMin,badge.xMin),
                Mathf.Min(art.yMin,badge.yMin),
                Mathf.Max(art.xMax,plaques[i].xMax,badge.xMax),
                Mathf.Max(art.yMax,plaques[i].yMax,badge.yMax));
            Place(button,bound.x,bound.y,bound.width,bound.height);
            PlaceRelative(illustration.rectTransform,art,bound.position);
            illustration.preserveAspect = true;
            PlaceRelative(RequiredRect(button,"StageNamePlaque"),plaques[i],bound.position);
            var title = new Rect(plaques[i].x+15f,plaques[i].y+3f,
                plaques[i].width-30f,plaques[i].height-6f);
            StyleLiveText(RequiredRect(button,"StageNameTMP"),title,bound.position,
                22f,FontStyles.Normal,Gold,TextAlignmentOptions.Center);
            PlaceRelative(RequiredRect(button,"CompletionBadge"),badge,bound.position);
            var locked = RequiredRect(button,"LockedStateOverlay");
            PlaceRelative(locked,art,bound.position);
            Place(RequiredRect(locked,"LockedLabel"),10f,height*.38f,width-20f,46f);
            // Keep active state, sprite tint, and manager references unchanged.
        }

        var bottom = RequiredRect(root,"StageInformationPanelLayer");
        bottom.anchoredPosition = Vector2.zero;
        Place(RequiredRect(bottom,"StageInformationPanelFrame"),36,848,1378,211);
        Place(RequiredRect(bottom,"SelectedStagePreviewBackground"),171,886,140,126);
        var preview = RequiredRect(bottom,"SelectedStagePreview").GetComponent<Image>();
        Place(preview.rectTransform,174,889,134,120);
        preview.preserveAspect = true;
        Place(RequiredRect(bottom,"SelectedStagePreviewGoldFrame"),163,878,156,142);
        var readableInk = new Color(36f/255f,23f/255f,8f/255f,1f);
        for (int i = 1; i <= 5; i++)
        {
            var info = RequiredRect(bottom,"Stage" + i + "_Info");
            StyleLiveText(RequiredRect(info,"SelectedStageTitle"),new Rect(334,875,646,44),
                Vector2.zero,28f,FontStyles.Bold,readableInk,TextAlignmentOptions.MidlineLeft);
            StyleLiveText(RequiredRect(info,"Enemies"),new Rect(338,919,650,28),
                Vector2.zero,19f,FontStyles.Normal,readableInk,TextAlignmentOptions.MidlineLeft);
            StyleLiveText(RequiredRect(info,"Boss"),new Rect(338,948,650,28),
                Vector2.zero,20f,FontStyles.Bold,readableInk,TextAlignmentOptions.MidlineLeft);
            StyleLiveText(RequiredRect(info,"Description"),new Rect(338,980,650,28),
                Vector2.zero,18f,FontStyles.Italic,readableInk,TextAlignmentOptions.MidlineLeft);
        }
        var enter = RequiredRect(bottom,"EnterStageButton");
        Place(enter,987,901,310,86);
        StyleLiveText(RequiredRect(enter,"LiveEnterStageText"),new Rect(30,16,250,54),
            Vector2.zero,36f,FontStyles.Bold,readableInk,TextAlignmentOptions.Center);

        var curves = new[] {
            new[] {new Vector2(445,441),new Vector2(445,483),new Vector2(404,506),new Vector2(391,548)},
            new[] {new Vector2(459,641),new Vector2(595,716),new Vector2(693,674),new Vector2(872,688)},
            new[] {new Vector2(1084,679),new Vector2(1167,660),new Vector2(1200,617),new Vector2(1202,574)},
            new[] {new Vector2(1188,420),new Vector2(1188,374),new Vector2(1171,344),new Vector2(1094,321)}
        };
        var nodes = new[] {
            new[] {new Vector2(445,463),new Vector2(403,523)},
            new[] {new Vector2(868,688)},new[] {new Vector2(1177,616)},
            new[] {new Vector2(1158,365)}
        };
        string[] routeNames = {"Route1_to_2","Route2_to_3","Route3_to_4","Route4_to_5"};
        var nodeSprite = Sprite("stage_path_node");
        for (int i = 0; i < routeNames.Length; i++)
            CorrectRoute(RequiredRect(root,"StageRouteLayer/" + routeNames[i]),curves[i],
                nodes[i],nodeSprite,i == 1,registerCreatedUndo);
    }

    private static void PlaceRelative(RectTransform rect, Rect source, Vector2 origin)
    {
        Place(rect,source.x-origin.x,source.y-origin.y,source.width,source.height);
    }

    private static void StyleLiveText(RectTransform rect, Rect source, Vector2 origin,
        float size, FontStyles style, Color color, TextAlignmentOptions alignment)
    {
        PlaceRelative(rect,source,origin);
        var text = rect.GetComponent<TextMeshProUGUI>();
        text.font = alegreya;
        text.fontSharedMaterial = alegreya.material;
        text.enableAutoSizing = false;
        text.fontSize = size;
        text.fontStyle = style;
        text.fontWeight = (style & FontStyles.Bold) != 0 ? FontWeight.Bold : FontWeight.Regular;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        // Text content and each information panel's visibility are authored
        // data/state, so this visual correction deliberately retains them.
    }

    private static void CorrectRoute(RectTransform route, Vector2[] curve, Vector2[] nodes,
        Sprite sprite, bool hideSpineCrossing, bool registerCreatedUndo)
    {
        foreach (string oldName in new[] {"LinkedGoldPath","StartingCompass","EndingCompass"})
        {
            var old = route.Find(oldName);
            if (old != null) old.gameObject.SetActive(false);
        }
        // Approximate arc length densely, then place uniformly spaced beads.
        var samples = new List<Vector2> {curve[0]};
        Vector2 previous = curve[0];
        float carried = 0f;
        const float spacing = 10f;
        for (int i = 1; i <= 256; i++)
        {
            float t = i / 256f;
            float u = 1f-t;
            Vector2 current = u*u*u*curve[0] + 3f*u*u*t*curve[1] +
                3f*u*t*t*curve[2] + t*t*t*curve[3];
            float distance = Vector2.Distance(previous,current);
            while (distance > 0f && carried + distance >= spacing)
            {
                float along = (spacing-carried) / distance;
                previous = Vector2.Lerp(previous,current,along);
                samples.Add(previous);
                distance = Vector2.Distance(previous,current);
                carried = 0f;
            }
            carried += distance;
            previous = current;
        }
        for (int i = 0; i < samples.Count; i++)
        {
            var bead = ReuseRouteImage(route,"ReferenceBead_" + i.ToString("000"),sprite,registerCreatedUndo);
            Vector2 center = samples[i];
            Place(bead.rectTransform,center.x,center.y,6.2f,4.2f);
            bead.rectTransform.pivot = new Vector2(.5f,.5f);
            Vector2 tangent = samples[Mathf.Min(i+1,samples.Count-1)] - samples[Mathf.Max(0,i-1)];
            bead.rectTransform.localRotation = Quaternion.Euler(0,0,-Mathf.Atan2(tangent.y,tangent.x)*Mathf.Rad2Deg);
            bead.gameObject.SetActive(!hideSpineCrossing || center.x < 674f || center.x > 777f);
        }
        for (int i = 0; i < nodes.Length; i++)
        {
            var compass = ReuseRouteImage(route,"ReferenceCompass_" + i.ToString("00"),sprite,registerCreatedUndo);
            Place(compass.rectTransform,nodes[i].x-12f,nodes[i].y-12f,24f,24f);
            compass.rectTransform.localRotation = Quaternion.identity;
            compass.gameObject.SetActive(true);
        }
        // If a later reference adjustment needs fewer samples, retain surplus
        // objects disabled so repeated menu use never destroys user objects.
        foreach (Transform child in route)
        {
            int index;
            if (child.name.StartsWith("ReferenceBead_",StringComparison.Ordinal) &&
                int.TryParse(child.name.Substring("ReferenceBead_".Length),out index) && index >= samples.Count)
                child.gameObject.SetActive(false);
            if (child.name.StartsWith("ReferenceCompass_",StringComparison.Ordinal) &&
                int.TryParse(child.name.Substring("ReferenceCompass_".Length),out index) && index >= nodes.Length)
                child.gameObject.SetActive(false);
        }
    }

    private static Image ReuseRouteImage(RectTransform parent, string name, Sprite sprite, bool registerCreatedUndo)
    {
        var child = parent.Find(name);
        Image image;
        if (child == null)
        {
            var gameObject = new GameObject(name,typeof(RectTransform),typeof(Image));
            if (registerCreatedUndo)
                Undo.RegisterCreatedObjectUndo(gameObject,"Create Reference Route Bead");
            gameObject.transform.SetParent(parent,false);
            gameObject.layer = parent.gameObject.layer;
            image = gameObject.GetComponent<Image>();
        }
        else
        {
            image = child.GetComponent<Image>();
            if (image == null)
                throw new InvalidOperationException("Existing reference route child is not an Image: " + name);
        }
        image.sprite = sprite;
        image.color = Color.white;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.raycastTarget = false;
        return image;
    }

    private static void BindEnterButtonPersistently(Button enter, StageSelectManager manager)
    {
        // Remove only this handler's nonpersistent copy from older builder runs.
        enter.onClick.RemoveListener(manager.OnEnterPressed);
        bool found = false;
        for (int i = 0; i < enter.onClick.GetPersistentEventCount(); i++)
        {
            if (enter.onClick.GetPersistentTarget(i) == manager &&
                enter.onClick.GetPersistentMethodName(i) == nameof(StageSelectManager.OnEnterPressed))
            {
                found = true;
                break;
            }
        }
        if (!found) UnityEventTools.AddPersistentListener(enter.onClick,manager.OnEnterPressed);
        EditorUtility.SetDirty(enter);
        if (PrefabUtility.IsPartOfPrefabInstance(enter))
            PrefabUtility.RecordPrefabInstancePropertyModifications(enter);
    }

    private static void ImportReferenceSprites()
    {
        foreach (string file in Directory.GetFiles(SpriteFolder,"*.png"))
        {
            string p=file.Replace('\\','/');
            var importer=AssetImporter.GetAtPath(p) as TextureImporter;
            if(importer==null)
            {
                AssetDatabase.ImportAsset(p,ImportAssetOptions.ForceUpdate);
                importer=AssetImporter.GetAtPath(p) as TextureImporter;
            }
            if(importer==null) throw new InvalidOperationException("Missing PNG importer: "+p);
            if (importer.textureType==TextureImporterType.Sprite &&
                importer.spriteImportMode==SpriteImportMode.Single &&
                importer.alphaIsTransparency && !importer.mipmapEnabled &&
                importer.maxTextureSize>=4096)
                continue;
            importer.textureType=TextureImporterType.Sprite;
            importer.spriteImportMode=SpriteImportMode.Single;
            importer.alphaIsTransparency=true;
            importer.mipmapEnabled=false;
            importer.maxTextureSize=4096;
            importer.wrapMode=TextureWrapMode.Clamp;
            importer.filterMode=FilterMode.Bilinear;
            importer.SaveAndReimport();
        }
    }

    private static Sprite Sprite(string name)
    {
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder+name+".png");
        if(sprite==null)throw new InvalidOperationException("Required sprite is missing: "+name);
        return sprite;
    }

    private static RectTransform CreateRect(Transform parent,string name)
    {
        var o=new GameObject(name,typeof(RectTransform));
        var rt=o.GetComponent<RectTransform>();
        rt.SetParent(parent,false);
        return rt;
    }

    private static RectTransform CreateFullSizeLayer(Transform parent,string name)
    {
        var rt=CreateRect(parent,name);
        rt.anchorMin=Vector2.zero;
        rt.anchorMax=Vector2.one;
        rt.offsetMin=Vector2.zero;
        rt.offsetMax=Vector2.zero;
        return rt;
    }

    private static void Place(RectTransform rt,float x,float y,float w,float h)
    {
        rt.anchorMin=rt.anchorMax=new Vector2(0,1);
        rt.pivot=new Vector2(0,1);
        rt.anchoredPosition=new Vector2(x,-y);
        rt.sizeDelta=new Vector2(w,h);
    }

    private static Image ImageAt(Transform parent,string name,Sprite sprite,
        float x,float y,float w,float h)
    {
        var rt=CreateRect(parent,name);
        Place(rt,x,y,w,h);
        var img=rt.gameObject.AddComponent<Image>();
        img.sprite=sprite;
        img.type=Image.Type.Simple;
        img.color=Color.white;
        img.raycastTarget=false;
        return img;
    }

    private static TextMeshProUGUI TextAt(Transform parent,string name,string value,
        float x,float y,float w,float h,float fontSize,FontStyles style,
        Color color,TextAlignmentOptions align,TMP_FontAsset font)
    {
        var rt=CreateRect(parent,name);
        Place(rt,x,y,w,h);
        var text=rt.gameObject.AddComponent<TextMeshProUGUI>();
        text.font=font;
        text.text=value;
        text.fontSize=fontSize;
        text.fontStyle=style;
        text.color=color;
        text.alignment=align;
        text.textWrappingMode=TextWrappingModes.Normal;
        text.overflowMode=TextOverflowModes.Ellipsis;
        text.raycastTarget=false;
        return text;
    }

    private static GameObject Route(Transform parent,string name,Sprite segment,
        float x,float y,float w,float h,float degrees,float unused,
        Sprite nodeSprite,Vector2 first,Vector2 last)
    {
        var layer=CreateFullSizeLayer(parent,name);
        var chain=ImageAt(layer,"LinkedGoldPath",segment,x,y,w,h);
        chain.rectTransform.localRotation=Quaternion.Euler(0,0,degrees);
        ImageAt(layer,"StartingCompass",nodeSprite,first.x-12,first.y-12,25,25);
        ImageAt(layer,"EndingCompass",nodeSprite,last.x-12,last.y-12,25,25);
        return layer.gameObject;
    }

    private static BuiltNode Stage(Transform parent,int index,string title,string spriteName,
        Rect art,Rect plaque,Vector2 badge)
    {
        var rect=CreateRect(parent,"Stage"+(index+1)+"_Button");
        // Use stage image+label full hitbox with no opaque Button background.
        var bound=Rect.MinMaxRect(Mathf.Min(art.xMin,plaque.xMin),art.yMin,
            Mathf.Max(art.xMax,plaque.xMax),Mathf.Max(art.yMax,plaque.yMax));
        Place(rect,bound.x,bound.y,bound.width,bound.height);
        var hit=rect.gameObject.AddComponent<Image>();
        hit.color=new Color(1,1,1,0);
        hit.raycastTarget=true;
        var button=rect.gameObject.AddComponent<Button>();
        button.targetGraphic=hit;
        var illustration=ImageAt(rect,"StageIllustration",Sprite(spriteName),
            art.x-bound.x,art.y-bound.y,art.width,art.height);
        illustration.preserveAspect=true;
        ImageAt(rect,"StageNamePlaque",Sprite("stage_label_plaque"),
            plaque.x-bound.x,plaque.y-bound.y,plaque.width,plaque.height);
        TextAt(rect,"StageNameTMP",title,plaque.x-bound.x+15,plaque.y-bound.y+6,
            plaque.width-30,plaque.height-12,index==0?18.5f:18.5f,
            FontStyles.Normal,Gold,TextAlignmentOptions.Center,alegreya);
        Image check=ImageAt(rect,"CompletionBadge",Sprite("stage_completed_badge"),
            badge.x-bound.x-26,badge.y-bound.y-28,54,54);
        // Locked display is not a baked/generated checkmark. The existing
        // StageSelectManager toggles this live to match real progression.
        var lockOverlay=ImageAt(rect,"LockedStateOverlay",null,
            art.x-bound.x,art.y-bound.y,art.width,art.height);
        // No opaque rectangular blocking plate over painterly stage art.
        // Locked-stage Image gets its muted tint from StageSelectManager;
        // the transparent overlay carries only a readable live LOCKED label.
        lockOverlay.color=Color.clear;
        TextAt(lockOverlay.rectTransform,"LockedLabel","LOCKED",10,art.height*.38f,
            art.width-20,46,24,FontStyles.Bold,Gold,TextAlignmentOptions.Center,cinzel);
        lockOverlay.gameObject.SetActive(false);
        return new BuiltNode{button=button,image=illustration,
            check=check.gameObject,locked=lockOverlay.gameObject};
    }
}
