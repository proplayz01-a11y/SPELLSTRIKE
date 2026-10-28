// Source-controlled, visible-menu-only promotion of Stage 1's approved
// layered HP/EXP artwork onto the shared HUD prefab and Stage 2 direct-start
// scene. Stage 3 and Stage 4 already instantiate that shared HUD prefab.
// All gameplay bindings and player prefabs remain untouched.
#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

internal static class StageHudPotionOfficialPromotion
{
    private const string StageOne = "Assets/Scenes/SampleScene.unity";
    private const string StageTwo = "Assets/Scenes/Stage2Scene/Stage2Scene.unity";
    private const string SharedPrefab = "Assets/Prefabs/PersistentGameplayHUD.prefab";
    private const string BackupFolder = "Library/PresentationUIProductionPromotion_20261020_2352/OfficialHudPromotion";
    private const string Skin = "SpellstrikePlayerStatusSkin";

    [MenuItem("SPELLSTRIKE/HUD & Potion/Promote Stage1 Approved Mage HUD to ALL Stage Prefabs (Official)")]
    private static void Promote()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.sceneCount != 1 ||
            SceneManager.GetActiveScene().isDirty)
        {
            Debug.LogError("[Stage HUD Promotion] Exit Play, close additive scenes and SAVE current user edits first. Nothing changed.");
            return;
        }

        Scene stage1 = default, stage2 = default;
        GameObject prefabRoot = null;
        try
        {
            if (Directory.Exists(BackupFolder))
                throw new InvalidOperationException("Promotion backup already exists. Refusing a second run.");
            string[] paths = { StageOne, StageTwo, SharedPrefab };
            foreach (string path in paths)
                if (!File.Exists(path)) throw new FileNotFoundException(path);

            stage1 = EditorSceneManager.OpenScene(StageOne, OpenSceneMode.Additive);
            if (stage1.isDirty)
                throw new InvalidOperationException("Original Stage 1 contains unsaved changes.");
            Slider source = FindHealthSlider(stage1);
            Transform skin = source.transform.Find(Skin);
            if (skin == null ||
                skin.Find("MageHUD_StaticGoldAndBadges") == null ||
                skin.Find("MageHUD_BackgroundAndPortrait") == null ||
                skin.Find("HEALTH/HP_LiveFill") == null ||
                skin.Find("EXP/EXP_LiveFill") == null ||
                skin.Find("HEALTH/HP_ActualValue") == null)
                throw new InvalidOperationException("Stage1 approved layered Mage HUD is missing/incomplete. No promotion.");

            prefabRoot = PrefabUtility.LoadPrefabContents(SharedPrefab);
            Slider prefabHealth = prefabRoot.GetComponentsInChildren<Slider>(true)
                .Single(x => x.name == "HealthBar");
            if (prefabHealth.transform.Find(Skin) != null)
                throw new InvalidOperationException("Shared prefab already contains a Mage HUD skin. Refusing duplicate.");
            stage2 = EditorSceneManager.OpenScene(StageTwo, OpenSceneMode.Additive);
            if (stage2.isDirty)
                throw new InvalidOperationException("Stage2 contains unsaved changes.");
            Slider secondHealth = FindHealthSlider(stage2);
            if (secondHealth.transform.Find(Skin) != null)
                throw new InvalidOperationException("Stage2 already has Mage HUD, refusing duplicate.");

            // Source scene, target prefab and target Stage2 have passed all
            // preflight checks. Preserve their EXACT prechange bytes.
            Directory.CreateDirectory(BackupFolder);
            foreach (string path in paths)
                File.Copy(path, BackupFolder + "/" + Path.GetFileName(path), false);
            CopySkin(skin, prefabHealth.transform);
            CopySkin(skin, secondHealth.transform);

            GameObject copiedPrefabSkin = prefabHealth.transform.Find(Skin).gameObject;
            GameObject copiedStage2Skin = secondHealth.transform.Find(Skin).gameObject;
            if (!VerifySkin(copiedPrefabSkin.transform) || !VerifySkin(copiedStage2Skin.transform))
                throw new InvalidOperationException("Transferred Mage HUD missing an approved child. Do not use partial result.");

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, SharedPrefab);
            EditorSceneManager.MarkSceneDirty(stage2);
            if (!EditorSceneManager.SaveScene(stage2, StageTwo))
                throw new IOException("Stage 2 scene save failed.");

            Debug.Log("[Stage HUD Promotion] OFFICIAL: Stage 1 Mage HUD copied onto shared prefab (Stage 3/4 direct-start) and Stage 2 HealthBar; Stage1 original preserved. No HP/EXP Slider, AttackButton event, tile manager, mobile control or gameplay binding replaced. SharedPrefab SHA=" + Hash(SharedPrefab) + "; Stage2 SHA=" + Hash(StageTwo));
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError("[Stage HUD Promotion] Promotion stopped. Inspect user-edited state and guarded backup before any further save.");
        }
        finally
        {
            if (prefabRoot != null) PrefabUtility.UnloadPrefabContents(prefabRoot);
            if (stage2.IsValid() && stage2.isLoaded)
                EditorSceneManager.CloseScene(stage2, true);
            if (stage1.IsValid() && stage1.isLoaded)
                EditorSceneManager.CloseScene(stage1, true);
        }
    }

    private static Slider FindHealthSlider(Scene scene)
    {
        return scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Slider>(true))
            .Single(x => x.name == "HealthBar");
    }

    private static void CopySkin(Transform approved, Transform destination)
    {
        GameObject instance = UnityEngine.Object.Instantiate(approved.gameObject, destination, false);
        instance.name = Skin;
        RectTransform sourceRect = (RectTransform)approved;
        RectTransform targetRect = (RectTransform)instance.transform;
        targetRect.anchorMin = sourceRect.anchorMin;
        targetRect.anchorMax = sourceRect.anchorMax;
        targetRect.pivot = sourceRect.pivot;
        targetRect.anchoredPosition = sourceRect.anchoredPosition;
        targetRect.sizeDelta = sourceRect.sizeDelta;
        targetRect.localScale = sourceRect.localScale;
        targetRect.localRotation = sourceRect.localRotation;
    }

    private static bool VerifySkin(Transform skin)
    {
        return skin.Find("MageHUD_StaticGoldAndBadges/GoldFrame_FixedFullCanvas") != null
            && skin.Find("MageHUD_BackgroundAndPortrait/MagePortrait_ShapedMask") != null
            && skin.Find("HEALTH/HP_LiveFill")?.GetComponent<Image>() != null
            && skin.Find("EXP/EXP_LiveFill")?.GetComponent<Image>() != null
            && skin.Find("HEALTH/HP_ActualValue")?.GetComponent<Text>() != null;
    }
    private static string Hash(string file)
    {
        using (var sha = SHA256.Create())
        using (var stream = File.OpenRead(file))
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
    }
}
#endif
