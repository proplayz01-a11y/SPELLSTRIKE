using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// One-time authorized promotion of the QA-approved, independently layered
/// Mage HUD to the EXISTING production HealthBar hierarchy. No prefab apply,
/// Slider replacement, game state, or XP progression simulation.
/// </summary>
internal static class ApplyApprovedMageHudToSampleScene
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string ArtFolder = "Assets/UI/MageHUDReferenceV1/";
    private const string SkinName = PlayerStatusHudSkin.EditableRootName;
    // Isolated preview was authored with a Match Width 800x600 Canvas at
    // 2400x1080 (scale 3.0). Production correctly uses Match Height (1.8).
    // Compensate ONLY at the portrait-and-bars skin root: 3 / 1.8 = 5 / 3.
    // HP, EXP and Slider remain at their exact existing binding paths.
    private const float ApprovedProductionSkinScale = 5f / 3f;
    private static readonly Vector2 ApprovedSkinPosition = new Vector2(-27f, -27.8f);

    [MenuItem("SPELLSTRIKE/HUD/Apply Approved Layered Mage HUD to SampleScene", priority = 230)]
    private static void Apply()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            !scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
        {
            Debug.LogError("[Mage HUD Production] Open SampleScene in Edit Mode first.");
            return;
        }
        if (scene.isDirty)
        {
            Debug.LogError("[Mage HUD Production] Unsaved SampleScene changes detected. " +
                "Save them before applying the approved artwork. No changes made.");
            return;
        }

        Slider[] matches = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Slider>(true))
            .Where(slider => slider.name == "HealthBar")
            .ToArray();
        if (matches.Length != 1)
        {
            Debug.LogError("[Mage HUD Production] Expected exactly one original HealthBar Slider.");
            return;
        }

        Slider originalSlider = matches[0];
        Transform skin = originalSlider.transform.Find(SkinName);
        if (skin == null)
        {
            Debug.LogError("[Mage HUD Production] Original editable skin not found.");
            return;
        }
        if (skin.Find("MageHUD_StaticGoldAndBadges") != null ||
            skin.Find("MageHUD_BackgroundAndPortrait") != null)
        {
            Debug.LogWarning("[Mage HUD Production] Approved layers already exist. " +
                "Refusing a second assembly to prevent duplicates.");
            return;
        }

        Image hp = skin.Find("HEALTH/HP_LiveFill")?.GetComponent<Image>();
        Image xp = skin.Find("EXP/EXP_LiveFill")?.GetComponent<Image>();
        Text hpLabel = skin.Find("HEALTH/HP_ActualValue")?.GetComponent<Text>();
        Image existingPortrait = skin.Find("VanAert_StorybookPortrait")?.GetComponent<Image>();
        if (hp == null || xp == null || hpLabel == null ||
            existingPortrait == null || existingPortrait.sprite == null ||
            skin.Find("HEALTH") == null || skin.Find("EXP") == null ||
            originalSlider.fillRect == hp.rectTransform)
        {
            Debug.LogError("[Mage HUD Production] Exact original binding paths, " +
                "portrait, or independent Slider fill contract missing. Aborted.");
            return;
        }

        foreach (string filename in new[] { "mage_hud_gold_frame", "mage_hud_dark_backing",
            "mage_portrait", "portrait_mask", "hp_fill_full", "hp_mask",
            "xp_fill_full", "xp_mask", "heart_icon", "star_icon" })
        {
            if (AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + filename + ".png") == null)
            {
                Debug.LogError("[Mage HUD Production] Sprite missing: " + filename +
                    ". Nothing changed.");
                return;
            }
        }

        // Back up the exact saved scene before making any changes. If Unity
        // exits or a graphic fails to import, the prior state is recoverable.
        string source = Path.GetFullPath(Path.Combine(Application.dataPath, "../", ScenePath));
        string dir = Path.GetFullPath(Path.Combine(Application.dataPath,
            "../Library/MageHUDProductionIntegration_20261019"));
        Directory.CreateDirectory(dir);
        string backup = Path.Combine(dir,
            "SampleScene_before_layered_mage_hud_" +
            DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".unity");
        File.Copy(source, backup, overwrite: false);

        var sliderOriginalFill = originalSlider.fillRect;
        Sprite preservedPortrait = existingPortrait.sprite;
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Replace Legacy HP/EXP Artwork with Approved Mage HUD");
        Undo.RegisterFullObjectHierarchyUndo(originalSlider.gameObject,
            "Replace Legacy HP/EXP Artwork with Approved Mage HUD");

        try
        {
            // Reuse the SAME approved layered assembly as the tested isolated
            // preview, but intentionally keep the current player portrait for
            // the later character-portrait replacement pass.
            MageHudReferencePreviewBuilder.BuildLayers(originalSlider, preservedPortrait);

            RectTransform skinRect = (RectTransform)skin;
            skinRect.anchoredPosition = ApprovedSkinPosition;
            skinRect.localScale = Vector3.one * ApprovedProductionSkinScale;

            if (skin.Find("HEALTH/HP_LiveFill")?.GetComponent<Image>() != hp ||
                skin.Find("EXP/EXP_LiveFill")?.GetComponent<Image>() != xp ||
                skin.Find("HEALTH/HP_ActualValue")?.GetComponent<Text>() != hpLabel ||
                originalSlider.fillRect != sliderOriginalFill ||
                skin.Find("HEALTH")?.GetComponent<Mask>() == null ||
                skin.Find("EXP")?.GetComponent<Mask>() == null ||
                skin.Find("MageHUD_BackgroundAndPortrait/MagePortrait_ShapedMask")?.GetComponent<Mask>() == null ||
                skin.Find("MageHUD_StaticGoldAndBadges/GoldFrame_FixedFullCanvas") == null ||
                skin.Find("MageHUD_StaticGoldAndBadges/HeartBadge") == null ||
                Mathf.Abs(xp.fillAmount) > .0001f)
                throw new InvalidOperationException("The newly assembled HUD did not preserve " +
                    "the required binding, masks or empty production EXP state.");

            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = skin.gameObject;
            Canvas.ForceUpdateCanvases();
            Debug.Log("[Mage HUD Production] UPDATED ON EXISTING HealthBar: " +
                "approved backing, masks, fills, fixed frame, heart/star badges " +
                "and live numeric HP. Preserved original portrait Sprite and " +
                "Slider, original HP/EXP paths, and XP fill=0. Backup: " + backup +
                ". Inspect Game View and save SampleScene after QA.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[Mage HUD Production] Assembly interrupted. Do NOT " +
                "save a partially updated scene. Restore from: " + backup);
            throw;
        }
    }

    [MenuItem("SPELLSTRIKE/HUD/Match Layered Mage HUD Preview Proportions", priority = 232)]
    private static void MatchApprovedSize()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath || scene.isDirty)
        {
            Debug.LogWarning("[Mage HUD Production] Save SampleScene edits first. " +
                "No geometry modified.");
            return;
        }

        Slider[] sliders = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Slider>(true))
            .Where(slider => slider.name == "HealthBar").ToArray();
        if (sliders.Length != 1)
            return;
        RectTransform skin = sliders[0].transform.Find(SkinName) as RectTransform;
        if (skin == null || skin.Find("MageHUD_StaticGoldAndBadges") == null ||
            skin.Find("MageHUD_BackgroundAndPortrait/MagePortrait_ShapedMask") == null ||
            skin.Find("HEALTH/HP_LiveFill") == null ||
            skin.Find("EXP/EXP_LiveFill") == null ||
            skin.Find("HEALTH/HP_ActualValue") == null)
        {
            Debug.LogError("[Mage HUD Production] Approved HUD bindings/layers " +
                "not present. Nothing changed.");
            return;
        }

        Undo.RecordObject(skin, "Match Approved Mage HUD Runtime Proportions");
        skin.anchoredPosition = ApprovedSkinPosition;
        skin.localScale = Vector3.one * ApprovedProductionSkinScale;
        EditorUtility.SetDirty(skin);
        EditorSceneManager.MarkSceneDirty(scene);
        Canvas.ForceUpdateCanvases();
        Debug.Log("[Mage HUD Production] Approved preview proportions restored " +
            "in Edit Mode: root=" + ApprovedSkinPosition + " scale=" +
            ApprovedProductionSkinScale + ". Review 20:9 and 16:9, then save.");
    }

    [MenuItem("SPELLSTRIKE/HUD/Match Layered Mage HUD Preview Proportions", true)]
    private static bool CanMatchApprovedSize()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode &&
            SceneManager.GetActiveScene().path == ScenePath;
    }

    [MenuItem("SPELLSTRIKE/HUD/Apply Approved Layered Mage HUD to SampleScene", true)]
    private static bool CanApply()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode &&
            SceneManager.GetActiveScene().path == ScenePath;
    }
}
