using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Isolated V09: replace ONLY the Minigame tent's old hover plaque with the
/// user-approved transparent gold MINIGAMES wordmark, centered in the tent.
/// Keep V08 and original artwork unchanged, preserve hover/click routing.
/// </summary>
public static class MainMenuV2MinigamesWordmarkV09
{
    public const string Source =
        "Assets/Scenes/MenuConcepts/MainMenuV2_Prototype_20261031_v08_VanGrounded.unity";
    public const string Destination =
        "Assets/Scenes/MenuConcepts/MainMenuV2_Prototype_20261031_v09_MinigamesWordmark.unity";
    private const string Wordmark =
        "Assets/MenuV2/Art/Minigames_Wordmark_GoldShadow_V09.png";

    [MenuItem("SPELLSTRIKE/Main Menu V2/Build Minigames Wordmark Label (V09)")]
    public static void Build()
    {
        if (System.Diagnostics.Process.GetProcessesByName("VALORANT-Win64-Shipping").Length > 0)
        {
            Debug.LogWarning("[MenuV2 V09] VALORANT active. No changes made.");
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            Debug.LogWarning("[MenuV2 V09] Editor busy or playing. No changes made.");
            return;
        }
        Scene current = SceneManager.GetActiveScene();
        if (current.path != Source || current.isDirty)
        {
            Debug.LogWarning("[MenuV2 V09] Need clean saved V08 scene. Active=" +
                current.path + " dirty=" + current.isDirty);
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Destination) != null)
        {
            Debug.LogWarning("[MenuV2 V09] V09 exists, refusing overwrite.");
            return;
        }

        TextureImporter importer = AssetImporter.GetAtPath(Wordmark) as TextureImporter;
        if (importer == null)
            throw new System.InvalidOperationException(
                "Approved wordmark PNG is missing: " + Wordmark);
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
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Wordmark);
        if (sprite == null || sprite.rect.width < 1000)
            throw new System.InvalidOperationException(
                "Wordmark import invalid, not changing the scene.");

        Transform existingTent = GameObject.Find("MainMenuV2_Canvas")
            .transform.Find("MinigameTent");
        if (existingTent == null ||
            existingTent.Find("HoverLabel") == null ||
            existingTent.Find("PointerHotspot") == null)
            throw new System.InvalidOperationException(
                "V08 tent controls missing; refusing to build.");

        // Saving a fresh copy protects all user-approved work in V08.
        if (!EditorSceneManager.SaveScene(current, Destination, true))
            throw new System.InvalidOperationException("V09 scene copy failed.");
        Scene scene = EditorSceneManager.OpenScene(Destination, OpenSceneMode.Single);
        Transform tent = GameObject.Find("MainMenuV2_Canvas")
            .transform.Find("MinigameTent");
        Transform label = tent.Find("HoverLabel");
        Image graphic = label.GetComponent<Image>();
        CanvasGroup fadeGroup = label.GetComponent<CanvasGroup>();
        MenuV2HoverTarget target =
            tent.Find("PointerHotspot").GetComponent<MenuV2HoverTarget>();
        if (graphic == null || fadeGroup == null || target == null)
            throw new System.InvalidOperationException(
                "V09 image/hover controller was not correctly bound.");

        // Replace the old illustrated plaque with transparent text. The
        // existing CanvasGroup is still driven by MenuV2HoverTarget,
        // so idle stays invisible and mouse/tap hover smoothly fades in.
        graphic.sprite = sprite;
        graphic.type = Image.Type.Simple;
        graphic.preserveAspect = true;
        graphic.raycastTarget = false;
        graphic.color = Color.white;
        RectTransform rect = label.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = new Vector2(0f, -65f);
        rect.sizeDelta = new Vector2(380f, 130f);
        // Explicitly keep wordmark visually above tent but behind hotspot.
        label.SetSiblingIndex(tent.Find("PointerHotspot").GetSiblingIndex() - 1);
        fadeGroup.alpha = 0f;
        fadeGroup.blocksRaycasts = false;
        fadeGroup.interactable = false;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[MenuV2 V09] Approved transparent MINIGAMES wordmark centered " +
                  "on tent (hover label only). V08 and old label asset untouched.");
    }
}
