using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Isolated reference preview ONLY. SaveScene(saveAsCopy:true) captures the
/// open in-memory scene, including unsaved artwork changes, without saving or
/// closing SampleScene. No prefab apply, player progression, or live save data.
/// </summary>
internal static class MageHudReferencePreviewBuilder
{
    private const string SourcePath = "Assets/Scenes/SampleScene.unity";
    private const string PreviewPath = "Assets/Scenes/MageHud_Reference_Preview.unity";
    private const string ArtDir = "Assets/UI/MageHUDReferenceV1/";
    private const string RootName = "SpellstrikePlayerStatusSkin";
    private const float NativeWidth = 2172f;
    private const float NativeHeight = 724f;
    private static readonly Dictionary<Canvas, bool> OriginalCanvasStates =
        new Dictionary<Canvas, bool>();

    [InitializeOnLoadMethod]
    private static void InstallSafetyCleanup()
    {
        AssemblyReloadEvents.beforeAssemblyReload -= RestoreOriginalCanvas;
        AssemblyReloadEvents.beforeAssemblyReload += RestoreOriginalCanvas;
        EditorApplication.playModeStateChanged -= RestoreOnPlay;
        EditorApplication.playModeStateChanged += RestoreOnPlay;
    }

    private static void RestoreOnPlay(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
            RestoreOriginalCanvas();
    }

    [MenuItem("SPELLSTRIKE/HUD/Mage Reference/QA Hide Original HUD Canvas Temporarily")]
    private static void HideOriginalCanvas()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            SceneManager.GetActiveScene().path != PreviewPath)
            return;
        Scene source = SceneManager.GetSceneByPath(SourcePath);
        if (!source.IsValid() || !source.isLoaded)
            return;
        foreach (GameObject root in source.GetRootGameObjects())
        foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
        {
            if (canvas.GetComponentsInChildren<Slider>(true)
                .Any(x => x != null && x.name == "HealthBar"))
            {
                if (!OriginalCanvasStates.ContainsKey(canvas))
                    OriginalCanvasStates.Add(canvas, canvas.enabled);
                canvas.enabled = false;
            }
        }
        Debug.Log("[MageHUD QA] Temporarily hidden original HealthBar Canvas. " +
            "Source scene/authoring remains loaded, and is auto-restored before compilation or Play.");
    }

    [MenuItem("SPELLSTRIKE/HUD/Mage Reference/QA Restore Original HUD Canvas")]
    private static void RestoreOriginalCanvas()
    {
        foreach (var kv in OriginalCanvasStates)
            if (kv.Key != null) kv.Key.enabled = kv.Value;
        OriginalCanvasStates.Clear();
    }

    private static string AuditPath => Path.Combine(Application.dataPath,
        "../Library/MageHUDReferenceV1_SourcePack/unity_assembly_audit.txt");

    [MenuItem("SPELLSTRIKE/HUD/Mage Reference/01 Build Isolated Preview")]
    private static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[MageHUD] Exit Play Mode before creating the isolated preview.");
            return;
        }
        var source = SceneManager.GetActiveScene();
        if (source.path != SourcePath || !source.isLoaded)
        {
            Debug.LogError("[MageHUD] Open the current SampleScene first. Nothing changed.");
            return;
        }
        if (File.Exists(PreviewPath))
        {
            Debug.LogError("[MageHUD] Preview already exists. Refusing to overwrite it.");
            return;
        }
        var sourceSlider = FindSlider(source);
        if (sourceSlider == null || !HasBindings(sourceSlider))
        {
            Debug.LogError("[MageHUD] Existing HealthBar Slider and all three exact binding paths required. Nothing changed.");
            return;
        }

        // This preserves the current UNSAVED scene in the original Editor
        // session; only the new path receives a serialized in-memory copy.
        bool originalDirty = source.isDirty;
        string originalPath = source.path;
        if (!EditorSceneManager.SaveScene(source, PreviewPath, true))
        {
            Debug.LogError("[MageHUD] Could not save isolated scene copy.");
            return;
        }
        if (source.path != originalPath || source.isDirty != originalDirty)
        {
            Debug.LogError("[MageHUD] Source scene changed its path or dirty state during SaveAsCopy! Inspect immediately.");
            return;
        }
        Scene preview = EditorSceneManager.OpenScene(PreviewPath, OpenSceneMode.Additive);
        if (!preview.IsValid() || !preview.isLoaded)
        {
            Debug.LogError("[MageHUD] Preview copy could not be opened.");
            return;
        }
        var slider = FindSlider(preview);
        if (slider == null || !HasBindings(slider))
        {
            Debug.LogError("[MageHUD] Preview copy does not contain the exact binding hierarchy. Original untouched.");
            return;
        }
        foreach (string filename in new[] { "mage_hud_gold_frame", "mage_hud_dark_backing",
            "mage_portrait", "portrait_mask", "hp_fill_full", "hp_mask",
            "xp_fill_full", "xp_mask", "heart_icon", "star_icon" })
        {
            if (LoadSprite(filename) == null)
            {
                Debug.LogError("[MageHUD] Missing Sprite: " + filename + ". Reimport assets before retry.");
                return;
            }
        }
        BuildLayers(slider);
        EditorSceneManager.SetActiveScene(preview);
        EditorSceneManager.MarkSceneDirty(preview);
        if (!EditorSceneManager.SaveScene(preview))
            Debug.LogError("[MageHUD] Could not save completed preview scene.");

        var skin = slider.transform.Find(RootName).GetComponent<RectTransform>();
        string audit = "Mage HUD isolated preview assembly\n" +
            "SourceScene=" + source.path + "\nSourceDirtyUnchanged=" + (source.isDirty == originalDirty) +
            "\nPreviewScene=" + preview.path + "\nSlider=" + GetPath(slider.transform) +
            "\nSkin=" + GetPath(skin) + "\nHP=" + GetPath(skin.Find("HEALTH/HP_LiveFill")) +
            "\nText=" + GetPath(skin.Find("HEALTH/HP_ActualValue")) +
            "\nXP=" + GetPath(skin.Find("EXP/EXP_LiveFill")) +
            "\nSliderFillRect=" + (slider.fillRect == null ? "null" : GetPath(slider.fillRect)) +
            "\nArtFrameStatic=true\nPortraitMask=true\nHPShapedMask=true\nXPShapedMask=true" +
            "\nProductionXP=0;LevelAndXPLabels=hidden\n";
        Directory.CreateDirectory(Path.GetDirectoryName(AuditPath));
        File.WriteAllText(AuditPath, audit);
        Selection.activeGameObject = skin.gameObject;
        Debug.Log("[MageHUD] Assembled preview-only scene at " + PreviewPath +
            ". Original SampleScene remains open with unsaved changes intact.\n" + audit);
    }

    [MenuItem("SPELLSTRIKE/HUD/Mage Reference/00 Import Sprite Settings")]
    private static void ImportSpriteSettings()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[MageHUD] Import while Unity is in Edit Mode.");
            return;
        }
        foreach (string file in new[] { "mage_hud_gold_frame", "mage_hud_dark_backing",
            "mage_portrait", "portrait_mask", "hp_fill_full", "hp_mask",
            "xp_fill_full", "xp_mask", "heart_icon", "star_icon" })
        {
            string path = ArtDir + file + ".png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException("[MageHUD] Missing import path: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = file.StartsWith("mage_hud_") ? 4096 :
                (file.StartsWith("hp_") ? 2048 : 1024);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[MageHUD] Imported ten full-rect lossless UI sprites without modifying existing Resources.");
    }

    private static Slider FindSlider(Scene scene)
    {
        foreach (var go in scene.GetRootGameObjects())
        {
            var slider = go.GetComponentsInChildren<Slider>(true)
                .FirstOrDefault(item => item.name == "HealthBar");
            if (slider != null) return slider;
        }
        return null;
    }

    private static bool HasBindings(Slider slider)
    {
        var root = slider.transform.Find(RootName);
        return root != null &&
            root.Find("HEALTH/HP_LiveFill")?.GetComponent<Image>() != null &&
            root.Find("HEALTH/HP_ActualValue")?.GetComponent<Text>() != null &&
            root.Find("EXP/EXP_LiveFill")?.GetComponent<Image>() != null &&
            root.Find("HEALTH")?.GetComponent<RectTransform>() != null &&
            root.Find("EXP")?.GetComponent<RectTransform>() != null;
    }

    private static string GetPath(Transform t)
    {
        if (t == null) return "NULL";
        var names = new List<string>();
        for (; t != null; t = t.parent) names.Add(t.name);
        names.Reverse();
        return string.Join("/", names);
    }

    private static Sprite LoadSprite(string name) =>
        AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir + name + ".png");

    // Shared approved artwork assembly. Production may invoke this ONLY from
    // the separately guarded, backed-up explicit integration menu. The
    // preview uses its originally authored character art by default.
    internal static void BuildLayers(Slider slider, Sprite portraitOverride = null)
    {
        // Production PersistentHUDRoot adds this *existing* presentation
        // component during runtime Awake. An Edit-Mode preview needs the same
        // component serialized on its cloned Slider so we can test without
        // running gameplay or changing a prefab.
        if (slider.GetComponent<PlayerStatusHudSkin>() == null)
            slider.gameObject.AddComponent<PlayerStatusHudSkin>();
        var root = (RectTransform)slider.transform.Find(RootName);
        var health = (RectTransform)root.Find("HEALTH");
        var exp = (RectTransform)root.Find("EXP");
        Image hpFill = root.Find("HEALTH/HP_LiveFill").GetComponent<Image>();
        Image xpFill = root.Find("EXP/EXP_LiveFill").GetComponent<Image>();
        Text hpValue = root.Find("HEALTH/HP_ActualValue").GetComponent<Text>();

        // Source root transform is authoritative; never move or resize the
        // artist-authored Slider or the editable skin root in this clone.
        float scale = Mathf.Min(root.rect.width / NativeWidth,
            root.rect.height / NativeHeight);
        if (scale <= 0f || float.IsNaN(scale))
            scale = 0.164f;

        // Hide superseded artwork on PREVIEW instances only. Keep binding
        // Images and Text as the exact original component instances.
        foreach (Graphic g in root.GetComponentsInChildren<Graphic>(true))
            if (g != hpFill && g != xpFill && g != hpValue) g.enabled = false;

        // One common native-coordinate surface for backing and portrait.
        var behind = MakeRect("MageHUD_BackgroundAndPortrait", root,
            0, 0, NativeWidth * scale, NativeHeight * scale);
        behind.SetSiblingIndex(0);
        CreateSprite("NavyBacking_IndependentOpacity", behind,
            "mage_hud_dark_backing", 0, 0, NativeWidth * scale, NativeHeight * scale);

        var portraitMask = MakeRect("MagePortrait_ShapedMask", behind,
            109 * scale, 76 * scale, 514 * scale, 506 * scale);
        Image portraitMaskImage = portraitMask.gameObject.AddComponent<Image>();
        ConfigureImage(portraitMaskImage, LoadSprite("portrait_mask"));
        AddStencilMask(portraitMask.gameObject);
        Image maskedPortrait = CreateSprite("MagePortrait_Masked", portraitMask,
            "mage_portrait", 0, 0, 514 * scale, 506 * scale);
        if (portraitOverride != null)
            maskedPortrait.sprite = portraitOverride;

        PlaceExistingGroup(health, 922, 206, 1127, 118, scale);
        ConfigureGroupMask(health, "hp_mask");
        PlaceExistingGroup(exp, 1153, 383, 808, 106, scale);
        ConfigureGroupMask(exp, "xp_mask");

        FillSetup(hpFill, "hp_fill_full");
        StretchToParent((RectTransform)hpFill.transform);
        hpFill.fillAmount = slider.maxValue > 0f ?
            Mathf.Clamp01(slider.value / slider.maxValue) : 0f;
        hpFill.enabled = true;
        hpFill.transform.SetSiblingIndex(0);

        FillSetup(xpFill, "xp_fill_full");
        StretchToParent((RectTransform)xpFill.transform);
        xpFill.fillAmount = 0f;
        xpFill.enabled = true; // Zero fill retains the real XP Image for future binding.
        xpFill.transform.SetSiblingIndex(0);

        RectTransform liveText = (RectTransform)hpValue.transform;
        // Keep the numeric baseline centred while allowing multi-digit
        // real HP values (120 / 120 and higher) without hitting the bezel.
        // The prior 500px source text box was unnecessarily narrow.
        PlaceRect(liveText, (981f - 922f) * scale, (219f - 206f) * scale,
            1058f * scale, 93f * scale);
        hpValue.enabled = true;
        hpValue.alignment = TextAnchor.MiddleCenter;
        hpValue.raycastTarget = false;
        hpValue.text = Mathf.RoundToInt(slider.value) + " / " +
            Mathf.RoundToInt(slider.maxValue);
        liveText.SetAsLastSibling();
        // The reference frame is a later sibling than HEALTH. A dedicated
        // nested canvas brings only the existing live Text above the frame,
        // without changing HEALTH/HP_ActualValue or baking any label into art.
        // This is isolated preview presentation; no production binding change.
        EnsureLiveLabelSorting(hpValue);

        // Put frame/icons above portrait and shaped fills without changing
        // the HEALTH and EXP direct binding paths or Slider hierarchy.
        var top = MakeRect("MageHUD_StaticGoldAndBadges", root,
            0, 0, NativeWidth * scale, NativeHeight * scale);
        CreateSprite("GoldFrame_FixedFullCanvas", top, "mage_hud_gold_frame",
            0, 0, NativeWidth * scale, NativeHeight * scale);
        // Exact FINAL icon_placements in asset_manifest.json. No default duplicates.
        CreateSprite("HeartBadge", top, "heart_icon", 764.5f * scale,
            208f * scale, 113f * scale, 107f * scale);
        CreateSprite("StarBadge_Large", top, "star_icon", 100.5f * scale,
            502f * scale, 133f * scale, 140f * scale);
        CreateSprite("StarBadge_Small", top, "star_icon", 771f * scale,
            388.13157895f * scale, 90f * scale, 94.7368421f * scale);

        behind.SetSiblingIndex(0);
        health.SetSiblingIndex(1);
        exp.SetSiblingIndex(2);
        top.SetSiblingIndex(root.childCount - 1);

        // Previous legacy fill remains separate and hidden, not repurposed
        // to drive the new full-length art's RectTransform width.
        if (slider.fillRect == hpFill.rectTransform)
            throw new InvalidOperationException("New HP fill must not be assigned to Slider.fillRect");
        foreach (Graphic g in slider.GetComponentsInChildren<Graphic>(true))
        {
            if (!g.transform.IsChildOf(root) && g.GetComponent<Selectable>() == null)
                g.enabled = false; // Old slider graphics; PREVIEW scene only.
        }
    }

    private static void PlaceExistingGroup(RectTransform rect, float x, float y,
        float w, float h, float scale)
    {
        PlaceRect(rect, x * scale, y * scale, w * scale, h * scale);
    }

    private static void ConfigureGroupMask(RectTransform group, string spriteName)
    {
        Image image = group.GetComponent<Image>();
        if (image == null) image = group.gameObject.AddComponent<Image>();
        ConfigureImage(image, LoadSprite(spriteName));
        image.enabled = true;
        AddStencilMask(group.gameObject);
    }

    private static void AddStencilMask(GameObject obj)
    {
        Mask mask = obj.GetComponent<Mask>();
        if (mask == null) mask = obj.AddComponent<Mask>();
        mask.showMaskGraphic = false;
    }

    private static void FillSetup(Image image, string spriteName)
    {
        ConfigureImage(image, LoadSprite(spriteName));
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = (int)Image.OriginHorizontal.Left;
        image.fillClockwise = true;
        image.color = Color.white;
    }

    private static void ConfigureImage(Image image, Sprite sprite)
    {
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.color = Color.white;
        image.preserveAspect = false;
        image.raycastTarget = false;
    }

    private static RectTransform MakeRect(string name, Transform parent,
        float x, float y, float width, float height)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        var rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        PlaceRect(rect, x, y, width, height);
        return rect;
    }

    private static void PlaceRect(RectTransform rect, float x, float y,
        float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
        rect.localScale = Vector3.one;
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static Image CreateSprite(string name, Transform parent,
        string spriteName, float x, float y, float width, float height)
    {
        var go = new GameObject(name, typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        PlaceRect(rect, x, y, width, height);
        var image = go.GetComponent<Image>();
        ConfigureImage(image, LoadSprite(spriteName));
        return image;
    }

    // Fixtures are EDIT-MODE only. Never call PlayerHealth damage/death,
    // progression or save. They affect the standalone preview scene alone.
    [MenuItem("SPELLSTRIKE/HUD/Mage Reference/QA 00 Percent")]
    private static void Qa0() => SetFixture(0f);
    [MenuItem("SPELLSTRIKE/HUD/Mage Reference/QA 50 Percent")]
    private static void Qa50() => SetFixture(.5f);
    [MenuItem("SPELLSTRIKE/HUD/Mage Reference/QA 60 Percent")]
    private static void Qa60() => SetFixture(.6f);
    [MenuItem("SPELLSTRIKE/HUD/Mage Reference/QA 100 Percent")]
    private static void Qa100() => SetFixture(1f);
    [MenuItem("SPELLSTRIKE/HUD/Mage Reference/QA Reset Production Defaults")]
    private static void QaReset() => SetFixture(-1f);

    [MenuItem("SPELLSTRIKE/HUD/Mage Reference/QA Save Preview Only")]
    private static void QaSavePreviewOnly()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            SceneManager.GetActiveScene().path != PreviewPath) return;
        if (EditorSceneManager.SaveScene(SceneManager.GetActiveScene()))
            Debug.Log("[MageHUD QA] Saved ONLY " + PreviewPath +
                ". Original SampleScene was not saved.");
        else
            Debug.LogError("[MageHUD QA] Preview scene save failed.");
    }

    [MenuItem("SPELLSTRIKE/HUD/Mage Reference/QA Diagnose HP Text Visibility")]
    private static void QaText()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            SceneManager.GetActiveScene().path != PreviewPath) return;
        var slider = FindSlider(SceneManager.GetActiveScene());
        if (slider == null || !HasBindings(slider)) return;
        var root = slider.transform.Find(RootName);
        var label = root.Find("HEALTH/HP_ActualValue").GetComponent<Text>();
        CanvasRenderer renderer = label.canvasRenderer;
        float oldAlpha = renderer.GetAlpha();
        Color oldColor = renderer.GetColor();
        Debug.Log("[MageHUD QA] HP label before: text=" + label.text +
            " component=" + label.enabled + " active=" + label.gameObject.activeInHierarchy +
            " graphicAlpha=" + label.color.a + " rendererAlpha=" + oldAlpha +
            " rendererColor=" + oldColor + " culled=" + renderer.cull +
            " worldRect=" + label.rectTransform.rect +
            " font=" + (label.font != null ? label.font.name : "NULL") +
            " textVertices=" + label.cachedTextGenerator.vertexCount +
            " textColor=" + label.color +
            " hasCanvas=" + (label.GetComponent<Canvas>() != null));
        renderer.SetAlpha(1f);
        renderer.SetColor(Color.white);
        label.SetAllDirty();
        Canvas.ForceUpdateCanvases();
        Debug.Log("[MageHUD QA] HP label after CanvasRenderer reset: alpha=" +
            renderer.GetAlpha() + " color=" + renderer.GetColor() +
            " culled=" + renderer.cull);
    }

    [MenuItem("SPELLSTRIKE/HUD/Mage Reference/QA Bring Live HP Text To Front")]
    private static void QaBringLabelForward()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            SceneManager.GetActiveScene().path != PreviewPath) return;
        Slider slider = FindSlider(SceneManager.GetActiveScene());
        if (slider == null || !HasBindings(slider)) return;
        Text label = slider.transform.Find(RootName + "/HEALTH/HP_ActualValue")
            .GetComponent<Text>();
        EnsureLiveLabelSorting(label);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[MageHUD QA] Restored the same live HP Text above the frame. " +
            "Path HEALTH/HP_ActualValue unchanged; no duplicated text/manager.");
    }

    private static void EnsureLiveLabelSorting(Text label)
    {
        Canvas canvas = label.GetComponent<Canvas>();
        if (canvas == null) canvas = label.gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 20;
        label.enabled = true;
        // Match the approved manuscript-style HUD using an EXISTING project
        // font. The component stays UnityEngine.UI.Text for live HP binding.
        Font manuscript = AssetDatabase.LoadAssetAtPath<Font>(
            "Assets/Fonts/SPELLSTRIKE/Alegreya-Variable.ttf");
        if (manuscript != null)
            label.font = manuscript;
        label.fontSize = 19;
        label.fontStyle = FontStyle.Bold;
        label.color = new Color(1f, 0.98f, 0.9f, 1f);
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.raycastTarget = false;
        label.canvasRenderer.SetAlpha(1f);
        label.SetAllDirty();
    }

    [MenuItem("SPELLSTRIKE/HUD/Mage Reference/QA Verify Slider Live Binding")]
    private static void QaLiveSliderBinding()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            SceneManager.GetActiveScene().path != PreviewPath) return;
        var slider = FindSlider(SceneManager.GetActiveScene());
        if (slider == null || !HasBindings(slider)) return;
        var skin = slider.GetComponent<PlayerStatusHudSkin>();
        if (skin == null)
        {
            skin = slider.gameObject.AddComponent<PlayerStatusHudSkin>();
            Debug.Log("[MageHUD QA] Attached existing PlayerStatusHudSkin " +
                "to preview-only HealthBar Slider for binding verification.");
        }
        var authored = slider.transform.Find(RootName);
        Image hp = authored.Find("HEALTH/HP_LiveFill").GetComponent<Image>();
        Text label = authored.Find("HEALTH/HP_ActualValue").GetComponent<Text>();
        Image xp = authored.Find("EXP/EXP_LiveFill").GetComponent<Image>();
        float previousMin = slider.minValue, previousMax = slider.maxValue,
            previousValue = slider.value;
        try
        {
            skin.Initialize(); // Actual production PlayerStatusHudSkin; no mock manager.
            slider.minValue = 0f;
            slider.maxValue = 100f;
            foreach (float fraction in new[] { 0f, .5f, .6f, 1f })
            {
                slider.value = 100f * fraction;
                InvokeHudRefreshForEditMode(skin);
                string expectedLabel = Mathf.RoundToInt(100f * fraction) + " / 100";
                if (Mathf.Abs(hp.fillAmount - fraction) > .0001f ||
                    label.text != expectedLabel)
                    throw new InvalidOperationException("Slider binding failed at " +
                        fraction + ": fill=" + hp.fillAmount + " label=" + label.text);
                Debug.Log("[MageHUD QA] live Slider -> skin OK at " + fraction +
                    ": HP=" + hp.fillAmount + " label=" + label.text);
            }
            slider.value = 0f;
            slider.maxValue = 0f;
            InvokeHudRefreshForEditMode(skin);
            if (hp.fillAmount != 0f)
                throw new InvalidOperationException("Zero-max HP must have empty fill");
            if (xp.fillAmount != 0f)
                throw new InvalidOperationException("Production XP is not empty");
            Debug.Log("[MageHUD QA] live zero-max HP OK; production XP=0.");
        }
        finally
        {
            slider.minValue = previousMin;
            slider.maxValue = previousMax;
            slider.value = previousValue;
            InvokeHudRefreshForEditMode(skin);
            xp.fillAmount = 0f;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }

    private static void InvokeHudRefreshForEditMode(PlayerStatusHudSkin skin)
    {
        // SendMessage to a MonoBehaviour lifecycle function in Edit Mode
        // triggers native 'ShouldRunBehaviour()' assertions in Unity 6.3.
        // Invoke the existing private refresh method directly for this
        // editor-only fixture; production runtime is untouched.
        var refresh = typeof(PlayerStatusHudSkin).GetMethod("LateUpdate",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);
        if (refresh == null)
            throw new MissingMethodException("PlayerStatusHudSkin.LateUpdate");
        refresh.Invoke(skin, null);
    }

    private static void SetFixture(float fraction)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            SceneManager.GetActiveScene().path != PreviewPath)
        {
            Debug.LogError("[MageHUD QA] Fixture only in Preview Edit Mode.");
            return;
        }
        Slider slider = FindSlider(SceneManager.GetActiveScene());
        if (slider == null || !HasBindings(slider)) return;
        Transform root = slider.transform.Find(RootName);
        Image hp = root.Find("HEALTH/HP_LiveFill").GetComponent<Image>();
        Image xp = root.Find("EXP/EXP_LiveFill").GetComponent<Image>();
        Text text = root.Find("HEALTH/HP_ActualValue").GetComponent<Text>();
        if (fraction < 0f)
        {
            slider.value = slider.maxValue;
            hp.fillAmount = slider.maxValue > 0f ?
                Mathf.Clamp01(slider.value / slider.maxValue) : 0f;
            xp.fillAmount = 0f;
        }
        else
        {
            slider.value = slider.maxValue * fraction;
            hp.fillAmount = fraction;
            xp.fillAmount = fraction;
        }
        hp.enabled = true;
        xp.enabled = true;
        text.text = Mathf.RoundToInt(slider.value) + " / " +
            Mathf.RoundToInt(slider.maxValue);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[MageHUD QA] preview=" + (fraction < 0f ? 0f : fraction) +
            " HP=" + hp.fillAmount + " XP=" + xp.fillAmount +
            ", text=" + text.text + ", fixedFrame=" +
            (root.Find("MageHUD_StaticGoldAndBadges/GoldFrame_FixedFullCanvas") != null));
    }
}
