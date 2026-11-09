using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// V06 isolated QA refinement of the user's 23:30:44 recording.
/// V05, original Main Menu, Tutorial and cinematic assets are read-only.
/// </summary>
public static class MainMenuV2QAPolishV06
{
    public const string Source =
        "Assets/Scenes/MenuConcepts/MainMenuV2_Prototype_20261030_v05_HoverFlow.unity";
    public const string Destination =
        "Assets/Scenes/MenuConcepts/MainMenuV2_Prototype_20261030_v06_QAPolish.unity";
    private const string TentHover =
        "Assets/MenuV2/Art/Minigame_Tent_Hover_CleanHalo_V06.png";
    private const string PortalMaterial =
        "Assets/MenuV2/Materials/PortalSimpleWhirl_V06.mat";

    [MenuItem("SPELLSTRIKE/Main Menu V2/Build QA Video Fixes (V06)")]
    public static void Build()
    {
        if (System.Diagnostics.Process.GetProcessesByName("VALORANT-Win64-Shipping").Length > 0)
        {
            Debug.LogWarning("[MenuV2 V06] VALORANT running; no editor changes.");
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            Debug.LogError("[MenuV2 V06] Stop Play Mode and imports first.");
            return;
        }
        Scene current = SceneManager.GetActiveScene();
        if (current.path != Source || current.isDirty)
        {
            Debug.LogWarning("[MenuV2 V06] Open saved clean V05; active=" +
                             current.path + " dirty=" + current.isDirty);
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Destination) != null)
        {
            Debug.LogWarning("[MenuV2 V06] Already exists. Won't overwrite.");
            return;
        }
        TextureImporter importer = AssetImporter.GetAtPath(TentHover) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("[MenuV2 V06] Clean hover art not present.");
            return;
        }
        if (importer.textureType != TextureImporterType.Sprite ||
            importer.mipmapEnabled || !importer.alphaIsTransparency)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
        Shader shader = Shader.Find("SPELLSTRIKE/MenuV2/PortalSimpleWhirlV06");
        if (shader == null || !shader.isSupported)
        {
            Debug.LogError("[MenuV2 V06] Simple whirl shader unavailable.");
            return;
        }
        if (!EditorSceneManager.SaveScene(current, Destination, true))
        {
            Debug.LogError("[MenuV2 V06] Scene duplication failed.");
            return;
        }
        Scene scene = EditorSceneManager.OpenScene(Destination, OpenSceneMode.Single);
        Transform canvas = GameObject.Find("MainMenuV2_Canvas").transform;

        // 00:40–01:05: rotate just the existing vortex spiral without ripple.
        Transform portal = canvas.Find("PortalVortex_URP_InwardFlow");
        var portalImage = portal.GetComponent<Image>();
        var mat = AssetDatabase.LoadAssetAtPath<Material>(PortalMaterial);
        if (mat == null)
        {
            mat = new Material(shader);
            mat.name = "PortalSimpleWhirl_V06";
            AssetDatabase.CreateAsset(mat, PortalMaterial);
        }
        portalImage.material = mat;

        // 03:35–04:35: no out-of-register ropes or duplicate tent details.
        Transform tent = canvas.Find("MinigameTent");
        Image baseImage = tent.Find("Normal").GetComponent<Image>();
        Image hoverImage = tent.Find("Hover_GoldOutline").GetComponent<Image>();
        Sprite aligned = AssetDatabase.LoadAssetAtPath<Sprite>(TentHover);
        if (aligned == null || baseImage.sprite.rect.size != aligned.rect.size)
            throw new System.InvalidOperationException(
                "Clean tent hover missing or mismatched in resolution.");
        hoverImage.sprite = aligned;
        hoverImage.preserveAspect = baseImage.preserveAspect;

        // 01:30–02:20: book floats above the original right-hand stone.
        SetUI(canvas, "Clickable3DSpellbook", 2202, 234, 295, 295);
        canvas.Find("Clickable3DSpellbook").gameObject
            .AddComponent<MenuV2FloatingBookUIV06>();
        RectTransform bookLabel = canvas.Find("Clickable3DSpellbook/HoverLabel")
            .GetComponent<RectTransform>();
        bookLabel.anchoredPosition = new Vector2(0f, -152f);
        SetUI(canvas, "BookShadow", 2202, 81, 220, 41);
        // Keep the entire sign AND travel boots inside 2400x1080 safe bounds.
        SetUI(canvas, "QuitSign", 1780, 105, 210, 192);
        SetUI(canvas, "QuitShadow", 1780, 37, 175, 34);

        // 02:30–03:05: keep clean letter faces toward the camera.
        // The original gameplay tile prefabs and textures are untouched.
        Transform orbit = GameObject.Find("FiveGameTileOrbitRig").transform;
        if (orbit.childCount != 5)
            throw new System.InvalidOperationException("Expected five menu tiles.");
        for (int i = 0; i < orbit.childCount; i++)
        {
            Transform tile = orbit.GetChild(i);
            var previous = tile.GetComponent<MenuV2OrbitalTile>();
            if (previous == null)
                throw new System.InvalidOperationException(
                    "Original orbit controller missing: " + tile.name);
            Object.DestroyImmediate(previous);
            tile.localScale = new Vector3(.035f, 1.15f, 1.15f);
            tile.gameObject.AddComponent<MenuV2CleanFaceOrbitV06>()
                .Configure(i * Mathf.PI * 2f / 5f, .31f, 28, 29);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[MenuV2 V06] Saved video polish copy: " + Destination +
                  "; approved V05 and production scenes protected.");
    }

    private static void SetUI(Transform canvas, string name,
        float x, float y, float width, float height)
    {
        RectTransform r = canvas.Find(name).GetComponent<RectTransform>();
        r.anchoredPosition = new Vector2(x - 1200f, y - 540f);
        r.sizeDelta = new Vector2(width, height);
    }
}
