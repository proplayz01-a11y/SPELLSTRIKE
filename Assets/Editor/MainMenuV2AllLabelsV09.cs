using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// User requested ALL new wordmark-label edits in the existing V09 scene.
/// Never duplicates a V10 or modifies V08, gameplay or cinematics.
/// Labels remain existing CanvasGroups, wired to their original hover actions.
/// </summary>
public static class MainMenuV2AllLabelsV09
{
    private const string ScenePath =
        "Assets/Scenes/MenuConcepts/MainMenuV2_Prototype_20261031_v09_MinigamesWordmark.unity";
    private const string Art = "Assets/MenuV2/Art/";

    [MenuItem("SPELLSTRIKE/Main Menu V2/Apply Approved Labels IN V09 (No New Scene)")]
    public static void Apply()
    {
        if (System.Diagnostics.Process.GetProcessesByName("VALORANT-Win64-Shipping").Length > 0)
        {
            Debug.LogWarning("[MenuV2 V09] VALORANT active; no changes.");
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            Debug.LogError("[MenuV2 V09] Editor must be ready and out of Play Mode.");
            return;
        }
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath || scene.isDirty)
        {
            Debug.LogError("[MenuV2 V09] Only the saved/clean existing V09 scene may be edited. " +
                           "Active=" + scene.path + " dirty=" + scene.isDirty);
            return;
        }

        string[] files =
        {
            "Adventure_Wordmark_GoldShadow_V09.png",
            "Options_Wordmark_GoldShadow_V09.png",
            "Quit_Wordmark_GoldShadow_V09.png",
            "Spellbook_Wordmark_GoldShadow_V09.png"
        };
        Sprite[] wordmarks = new Sprite[files.Length];
        for (int i = 0; i < files.Length; i++)
        {
            var importer = AssetImporter.GetAtPath(Art + files[i]) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException("Missing label asset " + files[i]);
            if (importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Single ||
                importer.mipmapEnabled || !importer.alphaIsTransparency ||
                importer.filterMode != FilterMode.Bilinear)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
            wordmarks[i] = AssetDatabase.LoadAssetAtPath<Sprite>(Art + files[i]);
            if (wordmarks[i] == null)
                throw new InvalidOperationException("Sprite import failed: " + files[i]);
        }

        Transform canvas = GameObject.Find("MainMenuV2_Canvas").transform;
        string[] targets =
            { "AdventureGateway", "OptionsWorkbench", "QuitSign", "Clickable3DSpellbook" };
        // Local offsets relative to each object center. Adventure deliberately
        // sits BELOW THE SPIRAL, over the lower stone steps.
        Vector2[] positions =
            { new Vector2(0f, -255f), Vector2.zero, Vector2.zero, Vector2.zero };
        // Compact, per-prop proportions: text is legible without overpowering
        // the chest, travel sign or actual 3D spellbook.
        Vector2[] sizes =
        {
            new Vector2(318f, 108f),
            new Vector2(264f, 96f),
            new Vector2(184f, 106f),
            new Vector2(198f, 76f)
        };

        // Prevalidate every wired hover component, avoiding a partial edit.
        for (int i = 0; i < targets.Length; i++)
        {
            Transform obj = canvas.Find(targets[i]);
            Transform label = obj != null ? obj.Find("HoverLabel") : null;
            var img = label != null ? label.GetComponent<Image>() : null;
            var cg = label != null ? label.GetComponent<CanvasGroup>() : null;
            var target = obj != null
                ? obj.GetComponentInChildren<MenuV2HoverTarget>(true) : null;
            if (img == null || cg == null || target == null)
                throw new InvalidOperationException("Missing hover label setup: " + targets[i]);
            var serialized = new SerializedObject(target);
            if (serialized.FindProperty("labelGroup")?.objectReferenceValue != cg)
                throw new InvalidOperationException("Hover fade not wired: " + targets[i]);
        }

        for (int i = 0; i < targets.Length; i++)
        {
            Transform label = canvas.Find(targets[i]).Find("HoverLabel");
            Image image = label.GetComponent<Image>();
            CanvasGroup group = label.GetComponent<CanvasGroup>();
            RectTransform rect = label.GetComponent<RectTransform>();
            image.sprite = wordmarks[i];
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = positions[i];
            rect.sizeDelta = sizes[i];
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }
        // Minigame is already approved and deliberately unchanged.
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[MenuV2 V09] Adventure/Options/Quit/Spellbook wordmarks saved " +
                  "IN V09 ONLY. Minigames preserved, hover handlers/routes intact.");
    }
}
