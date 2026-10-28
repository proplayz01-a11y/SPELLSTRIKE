// Promotes the exact approved Stage 1 combat HUD artwork and Android Edit-Mode
// layout into Stage 2's scene-owned HUD and the Stage 3/4 shared HUD prefab.
// This is a guarded, visible Editor menu action. No Play Mode, no new HUD
// manager, and no replacement of gameplay/button/Slider event bindings.
#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

internal static class Stage234HudEditModeParityPromotion
{
    private const string Stage1 = "Assets/Scenes/SampleScene.unity";
    private const string Stage2 = "Assets/Scenes/Stage2Scene/Stage2Scene.unity";
    private const string Stage3 = "Assets/Scenes/Stage3Scene/Stage3Scene.unity";
    private const string Stage4 = "Assets/Scenes/Stage4Scene/Stage4Scene.unity";
    private const string HudPrefab = "Assets/Prefabs/PersistentGameplayHUD.prefab";
    private const string BackupRoot = "Library/GameplayHudEditModeParity_20261021";
    private const string CommitBackup = BackupRoot + "/OfficialParityBefore";
    private const string FrameName = "WordBar_OrnateFrame";

    [MenuItem("SPELLSTRIKE/HUD/Make Stage 2-4 Edit HUD Match Stage 1 Play HUD (Official)", priority = 235)]
    private static void Promote()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android ||
            !scene.IsValid() || !scene.isLoaded || scene.isDirty ||
            SceneManager.sceneCount != 1 ||
            PrefabStageUtility.GetCurrentPrefabStage() != null)
        {
            Debug.LogError("[HUD WYSIWYG] Stop Play, save your open scene and use Android Build Target first.");
            return;
        }
        if (Directory.Exists(CommitBackup))
        {
            Debug.LogError("[HUD WYSIWYG] Existing dedicated integration backup. Refusing a second promotion.");
            return;
        }

        Scene sourceScene = default, stage2Scene = default;
        GameObject prefab = null;
        try
        {
            string[] production = { Stage1, Stage2, Stage3, Stage4, HudPrefab };
            foreach (string file in production)
            {
                string safetyCopy = BackupRoot + "/" + file;
                if (!File.Exists(file) || !File.Exists(safetyCopy) ||
                    !ByteEquals(file, safetyCopy))
                    throw new InvalidOperationException("Production changed since safety snapshot: " + file);
            }

            sourceScene = scene.path == Stage1
                ? scene : EditorSceneManager.OpenScene(Stage1, OpenSceneMode.Additive);
            if (sourceScene.isDirty)
                throw new InvalidOperationException("Stage 1 is dirty; do not copy unfinished artwork.");
            Transform src = FindHud(sourceScene);
            ValidateSource(src);
            stage2Scene = EditorSceneManager.OpenScene(Stage2, OpenSceneMode.Additive);
            if (stage2Scene.isDirty)
                throw new InvalidOperationException("Stage 2 is dirty; save or inspect user changes first.");
            Transform second = FindHud(stage2Scene);
            prefab = PrefabUtility.LoadPrefabContents(HudPrefab);
            Transform shared = prefab.transform;
            ValidateTarget(second, "Stage2");
            ValidateTarget(shared, "SharedPrefab");

            // Exact fresh snapshots before modifying user-authored scene or prefab.
            foreach (string file in production)
            {
                string backupPath = CommitBackup + "/" + file;
                Directory.CreateDirectory(Path.GetDirectoryName(backupPath));
                File.Copy(file, backupPath, false);
            }

            ApplyArt(src, second);
            ApplyArt(src, shared);
            ValidateInstalled(second);
            ValidateInstalled(shared);
            PrefabUtility.SaveAsPrefabAsset(prefab, HudPrefab);
            if (!TutorialResponsiveLayout.SyncAndroidEditModeCombatHud(stage2Scene, out string msg2))
                throw new InvalidOperationException("Stage 2 Android edit layout sync failed: " + msg2);
            EditorSceneManager.MarkSceneDirty(stage2Scene);
            if (!EditorSceneManager.SaveScene(stage2Scene))
                throw new IOException("Stage 2 scene save failed.");
            Debug.Log("[HUD WYSIWYG] Stage 2 saved: approved Word Bar/Tile Pool frames, STRIKE button, Mage HP and Android layout.");
            EditorSceneManager.CloseScene(stage2Scene, true);
            stage2Scene = default;
            PrefabUtility.UnloadPrefabContents(prefab);
            prefab = null;

            for (int i = 0; i < 2; i++)
            {
                string path = i == 0 ? Stage3 : Stage4;
                Scene loaded = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    if (loaded.isDirty)
                        throw new InvalidOperationException(path + " was unexpectedly dirty on load.");
                    Transform hud = FindHud(loaded);
                    ValidateInstalled(hud);
                    if (!TutorialResponsiveLayout.SyncAndroidEditModeCombatHud(loaded, out string msg))
                        throw new InvalidOperationException(path + ": " + msg);
                    EditorSceneManager.MarkSceneDirty(loaded);
                    if (!EditorSceneManager.SaveScene(loaded, path))
                        throw new IOException("Could not save " + path);
                    Debug.Log("[HUD WYSIWYG] Saved production " + path +
                        " with the same Android Edit Mode combat HUD layout as Stage 1.");
                }
                finally
                {
                    if (loaded.IsValid() && loaded.isLoaded)
                        EditorSceneManager.CloseScene(loaded, true);
                }
            }
            Debug.Log("[HUD WYSIWYG] PASS: Stage 2, 3 and 4 production HUD now use the approved Stage 1 combat art + edit/runtime Android layout. Original UI controls, click bindings, mobile input, progression and save untouched. Backup: " + CommitBackup);
            SceneView.RepaintAll();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError("[HUD WYSIWYG] Promotion not fully complete. Check Console, current files and exact integration backups before making further changes. Do not run again blindly.");
        }
        finally
        {
            if (prefab != null) PrefabUtility.UnloadPrefabContents(prefab);
            if (stage2Scene.IsValid() && stage2Scene.isLoaded)
                EditorSceneManager.CloseScene(stage2Scene, true);
            if (sourceScene.IsValid() && sourceScene.isLoaded && sourceScene != scene)
                EditorSceneManager.CloseScene(sourceScene, true);
        }
    }

    private static Transform FindHud(Scene scene)
    {
        Transform[] hits = scene.GetRootGameObjects()
            .SelectMany(go => go.GetComponentsInChildren<Transform>(true))
            .Where(t => t.name == "PersistentHUDRoot").ToArray();
        if (hits.Length != 1)
            throw new InvalidOperationException(scene.path + " must have exactly one persistent HUD root; found " + hits.Length);
        return hits[0];
    }

    private static Transform Part(Transform hud, string name)
    {
        Transform[] matches = hud.GetComponentsInChildren<Transform>(true)
            .Where(t => t.name == name).ToArray();
        if (matches.Length != 1)
            throw new InvalidOperationException("Expected exactly one " + name + " in " + hud.name + ", found " + matches.Length);
        return matches[0];
    }

    private static void ValidateSource(Transform hud)
    {
        Transform word = Part(hud, "WordBarPanel");
        if (word.Find(FrameName)?.GetComponent<Image>() == null)
            throw new InvalidOperationException("Approved Stage 1 WordBar_OrnateFrame not found.");
        if (Part(hud, "TilePoolPanelDesign").GetComponent<Image>()?.sprite == null)
            throw new InvalidOperationException("Approved Stage 1 tile rack sprite missing.");
        Button button = Part(hud, "AttackButton").GetComponent<Button>();
        if (button == null || button.GetComponent<Image>()?.sprite == null ||
            button.GetComponentInChildren<TMP_Text>(true)?.text != "S T R I K E")
            throw new InvalidOperationException("Stage 1 ornate STRIKE artwork is missing.");
        if (Part(hud, "HealthBar").Find("SpellstrikePlayerStatusSkin/MageHUD_StaticGoldAndBadges") == null)
            throw new InvalidOperationException("Approved Stage 1 Mage HUD layers are missing.");
    }

    private static void ValidateTarget(Transform hud, string label)
    {
        foreach (string name in new[] { "WordBarPanel", "TilePoolPanel",
            "TilePoolPanelDesign", "TilePoolPanelDesign (1)", "UI_Buttons",
            "AttackButton", "HealthBar" })
            Part(hud, name);
        if (Part(hud, "WordBarPanel").Find(FrameName) != null)
            throw new InvalidOperationException(label + " already has ornate Word Bar. Refusing duplicate.");
        if (Part(hud, "AttackButton").GetComponent<Button>() == null ||
            Part(hud, "AttackButton").GetComponent<Image>() == null ||
            Part(hud, "AttackButton").GetComponentInChildren<TMP_Text>(true) == null)
            throw new InvalidOperationException(label + " original Attack button is missing.");
        if (Part(hud, "HealthBar").Find("SpellstrikePlayerStatusSkin/MageHUD_StaticGoldAndBadges") == null)
            throw new InvalidOperationException(label + " approved Mage HUD is missing.");
    }

    private static void ApplyArt(Transform source, Transform target)
    {
        foreach (string name in new[] { "WordBarPanel", "TilePoolPanel",
            "TilePoolPanelDesign", "TilePoolPanelDesign (1)", "UI_Buttons",
            "AttackButton" })
        {
            Transform from = Part(source, name);
            Transform to = Part(target, name);
            CopyRect(from as RectTransform, to as RectTransform);
            Image fromImage = from.GetComponent<Image>();
            Image toImage = to.GetComponent<Image>();
            if (fromImage != null && toImage != null)
            {
                toImage.sprite = fromImage.sprite;
                toImage.type = fromImage.type;
                toImage.color = fromImage.color;
                toImage.preserveAspect = fromImage.preserveAspect;
                toImage.raycastTarget = fromImage.raycastTarget;
            }
        }
        Canvas sourceCanvas = Part(source, "TilePoolPanel").GetComponentInParent<Canvas>();
        Canvas targetCanvas = Part(target, "TilePoolPanel").GetComponentInParent<Canvas>();
        if (sourceCanvas == null || targetCanvas == null)
            throw new InvalidOperationException("Could not find TilePoolPanel's real combat Canvas.");
        CanvasScaler sourceScaler = sourceCanvas.GetComponent<CanvasScaler>();
        CanvasScaler targetScaler = targetCanvas.GetComponent<CanvasScaler>();
        if (sourceScaler != null && targetScaler != null)
        {
            targetScaler.uiScaleMode = sourceScaler.uiScaleMode;
            targetScaler.referenceResolution = sourceScaler.referenceResolution;
            targetScaler.screenMatchMode = sourceScaler.screenMatchMode;
            targetScaler.matchWidthOrHeight = sourceScaler.matchWidthOrHeight;
        }
        Image sourceAttack = Part(source, "AttackButton").GetComponent<Image>();
        Image targetAttack = Part(target, "AttackButton").GetComponent<Image>();
        if (sourceAttack.sprite == null || targetAttack.sprite != sourceAttack.sprite)
            throw new InvalidOperationException("Could not apply approved STRIKE sprite.");
        TMP_Text sourceLabel = Part(source, "AttackButton").GetComponentInChildren<TMP_Text>(true);
        TMP_Text targetLabel = Part(target, "AttackButton").GetComponentInChildren<TMP_Text>(true);
        targetLabel.text = sourceLabel.text;
        targetLabel.font = sourceLabel.font;
        targetLabel.fontSize = sourceLabel.fontSize;
        targetLabel.fontStyle = sourceLabel.fontStyle;
        targetLabel.color = sourceLabel.color;
        targetLabel.alignment = sourceLabel.alignment;
        targetLabel.characterSpacing = sourceLabel.characterSpacing;
        targetLabel.raycastTarget = sourceLabel.raycastTarget;
        // Preserve the ORIGINAL Button and its attack events. Only art changes.
        Transform srcFrame = Part(source, "WordBarPanel").Find(FrameName);
        Transform dstWord = Part(target, "WordBarPanel");
        GameObject newFrame = UnityEngine.Object.Instantiate(srcFrame.gameObject, dstWord, false);
        newFrame.name = FrameName;
        CopyRect(srcFrame as RectTransform, newFrame.transform as RectTransform);
        newFrame.transform.SetSiblingIndex(0);
    }

    private static void ValidateInstalled(Transform hud)
    {
        if (Part(hud, "WordBarPanel").Find(FrameName)?.GetComponent<Image>()?.sprite == null ||
            Part(hud, "TilePoolPanelDesign").GetComponent<Image>()?.sprite == null ||
            Part(hud, "AttackButton").GetComponent<Image>()?.sprite == null ||
            Part(hud, "AttackButton").GetComponentInChildren<TMP_Text>(true)?.text != "S T R I K E")
            throw new InvalidOperationException("Approved visual components are missing under " + hud.name);
        if (Part(hud, "AttackButton").GetComponent<Button>() == null ||
            Part(hud, "HealthBar").GetComponent<Slider>() == null ||
            Part(hud, "TilePoolPanel").GetComponent<GridLayoutGroup>() == null)
            throw new InvalidOperationException("Existing gameplay HUD components were lost.");
    }

    private static void CopyRect(RectTransform from, RectTransform to)
    {
        if (from == null || to == null)
            throw new InvalidOperationException("Expected UI RectTransform in both HUDs.");
        to.anchorMin = from.anchorMin;
        to.anchorMax = from.anchorMax;
        to.pivot = from.pivot;
        to.anchoredPosition = from.anchoredPosition;
        to.sizeDelta = from.sizeDelta;
        to.localRotation = from.localRotation;
        to.localScale = from.localScale;
    }

    private static bool ByteEquals(string left, string right)
    {
        return File.ReadAllBytes(left).SequenceEqual(File.ReadAllBytes(right));
    }
}
#endif
