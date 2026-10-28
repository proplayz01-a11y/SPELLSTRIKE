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
/// Explicit, scoped, repeatable actions for the isolated Hall of Chronicles scene.
/// The reference prefab, source scenes, production scenes and progression are untouched.
/// Invoked from the visible Unity Editor menu (also via Unity MCP execute_menu_item).
/// </summary>
public static class HallOfChroniclesPreviewBuilder
{
    private const string ScenePath = "Assets/Scenes/StageSelectBook_HallOfChronicles_Preview.unity";
    private const string ManifestPath = "Assets/Editor/HallOfChronicles/asset_manifest.json";
    private const string SpriteRoot = "Assets/Sprites/HallOfChroniclesV1/";
    private const string CanvasName = "HallOfChroniclesBackgroundCanvas";
    private const string LibraryName = "LibraryBackground";

    [Serializable]
    private sealed class Manifest
    {
        public ManifestAsset[] assets;
    }

    [Serializable]
    private sealed class ManifestAsset
    {
        public string filename;
        public string role;
        public int rendering_order;
        public int[] source_rect_xywh;
        public float default_opacity;
        public float[] wick_pivot_normalized_bottom_left;
        public ManifestAnimation animation;
    }

    [Serializable]
    private sealed class ManifestAnimation
    {
        public string type;
        public float[] alpha_range;
        public float[] scale_range;
        public float[] period_seconds;
    }

    private static Manifest ReadManifest()
    {
        string fullPath = Path.GetFullPath(ManifestPath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Hall asset manifest is required.", fullPath);
        Manifest m = JsonUtility.FromJson<Manifest>(File.ReadAllText(fullPath));
        if (m == null || m.assets == null || m.assets.Length != 42)
            throw new InvalidOperationException("Expected exactly 42 declared pack assets.");
        return m;
    }

    private static Scene RequirePreviewScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before building or saving this preview.");
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            throw new InvalidOperationException(
                "Refusing to change scene '" + scene.path + "'. Open " + ScenePath + " first.");
        return scene;
    }

    private static string SpritePath(ManifestAsset entry)
    {
        if (!entry.filename.StartsWith("PNG/", StringComparison.Ordinal))
            throw new InvalidOperationException("Manifest item escapes PNG runtime root: " + entry.filename);
        return SpriteRoot + entry.filename.Substring(4);
    }

    [MenuItem("SPELLSTRIKE/Hall of Chronicles/1 - Configure Sprite Imports")]
    public static void ConfigureSpriteImports()
    {
        RequirePreviewScene();
        Manifest m = ReadManifest();
        int count = 0;
        foreach (ManifestAsset entry in m.assets)
        {
            string path = SpritePath(entry);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new FileNotFoundException("Missing sprite import: " + path);

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            var spriteSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(spriteSettings);
            spriteSettings.spriteMeshType = SpriteMeshType.FullRect;
            spriteSettings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(spriteSettings);
            importer.sRGBTexture = true;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.anisoLevel = 1;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            bool isBase = entry.role == "base";
            importer.alphaSource = isBase ? TextureImporterAlphaSource.None : TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = !isBase;
            importer.maxTextureSize = isBase ? 4096 : Mathf.NextPowerOfTwo(
                Mathf.Max(entry.source_rect_xywh[2], entry.source_rect_xywh[3]));

            // Uncompressed comparison first. ASTC 6x6 is a separate, device-verified future pass.
            var android = importer.GetPlatformTextureSettings("Android");
            android.name = "Android";
            android.overridden = true;
            android.maxTextureSize = isBase ? 4096 : importer.maxTextureSize;
            android.format = TextureImporterFormat.RGBA32;
            importer.SetPlatformTextureSettings(android);
            importer.SaveAndReimport();
            count++;
        }
        Debug.Log("[HallOfChronicles] Configured exactly " + count +
                  " sprites as Full Rect sRGB uncompressed reference imports.");
    }

    [MenuItem("SPELLSTRIKE/Hall of Chronicles/2 - Build Static Preview")]
    public static void BuildStaticPreview()
    {
        Scene scene = RequirePreviewScene();
        Manifest manifest = ReadManifest();
        if (GameObject.Find(CanvasName) != null)
            throw new InvalidOperationException("Preview background already exists; refusing to replace user work.");

        var sourceCanvas = GameObject.Find("Canvas");
        if (sourceCanvas == null || sourceCanvas.GetComponent<Canvas>() == null)
            throw new InvalidOperationException("Saved Stage Select reference Canvas was not found.");
        var book = sourceCanvas.transform.Find("StageSelectReferenceRoot");
        if (book == null || !book.gameObject.activeSelf)
            throw new InvalidOperationException("Expected the existing sharp StageSelectReferenceRoot.");

        // This single flat preview image is opaque and would cover a lower sorting Canvas.
        // Disable only in the isolated scene; do not touch the connected original prefab.
        var oldBackdrop = book.Find("PreviewBackdrop");
        if (oldBackdrop == null || oldBackdrop.GetComponent<Image>() == null)
            throw new InvalidOperationException("Expected obsolete PreviewBackdrop under the book.");

        // Fail early if anything is missing before creating scene objects.
        foreach (ManifestAsset entry in manifest.assets)
        {
            if (entry.role == "particle")
                continue;
            if (entry.source_rect_xywh == null || entry.source_rect_xywh.Length != 4)
                throw new InvalidOperationException("Invalid crop in " + entry.filename);
            if (AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath(entry)) == null)
                throw new FileNotFoundException("Not imported as a Sprite: " + SpritePath(entry));
        }

        var canvasGO = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = -50;
        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2400f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;

        RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
        var roomGO = new GameObject(LibraryName, typeof(RectTransform), typeof(HallOfChroniclesCoverFit),
                                    typeof(HallOfChroniclesAmbient));
        roomGO.transform.SetParent(canvasRect, false);
        RectTransform roomRect = roomGO.GetComponent<RectTransform>();
        roomRect.anchorMin = roomRect.anchorMax = new Vector2(0.5f, 0.5f);
        roomRect.pivot = new Vector2(0.5f, 0.5f);
        roomRect.sizeDelta = new Vector2(2400f, 1080f);
        roomRect.anchoredPosition = Vector2.zero;

        var ambientLayers = new List<HallOfChroniclesAmbient.Layer>();
        int images = 0;
        foreach (ManifestAsset entry in manifest.assets.OrderBy(e => e.rendering_order))
        {
            if (entry.role == "particle" || entry.filename.EndsWith("glow_blue_archway.png", StringComparison.Ordinal))
                continue; // Particles optional; archway glow is hidden by the sharp book.

            Rect crop = new Rect(entry.source_rect_xywh[0], entry.source_rect_xywh[1],
                                 entry.source_rect_xywh[2], entry.source_rect_xywh[3]);
            var go = new GameObject(string.Format("{0:00}_{1}", entry.rendering_order,
                Path.GetFileNameWithoutExtension(entry.filename)), typeof(RectTransform), typeof(Image));
            go.transform.SetParent(roomRect, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            Vector2 pivot = new Vector2(0.5f, 0.5f);
            bool wickScale = entry.animation != null
                && entry.animation.type == "asynchronous_alpha_and_wick_pivot_scale";
            if (wickScale && entry.wick_pivot_normalized_bottom_left != null
                && entry.wick_pivot_normalized_bottom_left.Length == 2)
            {
                pivot = new Vector2(entry.wick_pivot_normalized_bottom_left[0],
                                    entry.wick_pivot_normalized_bottom_left[1]);
            }
            rect.pivot = pivot;
            rect.sizeDelta = new Vector2(crop.width, crop.height);
            rect.anchoredPosition = new Vector2(
                crop.x + crop.width * pivot.x - 1200f,
                540f - crop.y - crop.height * (1f - pivot.y));
            rect.localScale = Vector3.one;

            Image image = go.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath(entry));
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = false;
            image.color = Color.white;
            images++;

            if (entry.role == "base")
                continue;
            var settings = entry.animation;
            if (settings == null)
                continue;
            uint hash = 2166136261u;
            foreach (char ch in entry.filename)
            {
                hash ^= ch;
                hash *= 16777619u;
            }
            float unit = (hash % 1000u) / 999f;
            float period = Range(settings.period_seconds, 0, 3f) +
                (Range(settings.period_seconds, 1, 3f) - Range(settings.period_seconds, 0, 3f)) * unit;
            ambientLayers.Add(new HallOfChroniclesAmbient.Layer
            {
                image = image,
                minAlpha = Range(settings.alpha_range, 0, 1f),
                maxAlpha = Range(settings.alpha_range, 1, 1f),
                minScale = Range(settings.scale_range, 0, 1f),
                maxScale = Range(settings.scale_range, 1, 1f),
                periodSeconds = period,
                phaseRadians = ((hash / 1000u) % 997u) / 996f * Mathf.PI * 2f
            });
        }
        roomGO.GetComponent<HallOfChroniclesCoverFit>().Configure(canvasRect);
        roomGO.GetComponent<HallOfChroniclesAmbient>().Configure(book.gameObject, ambientLayers);

        // Only an isolated prefab instance override in this scene, never Apply to source.
        oldBackdrop.gameObject.SetActive(false);
        EditorUtility.SetDirty(oldBackdrop.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("Failed to save isolated preview scene.");
        Debug.Log("[HallOfChronicles] Static assembly saved: " + images +
            " images (1 base + 38 visible overlays), " + ambientLayers.Count +
            " ambient bindings. Occluded archway glow and two optional particles unused. Source scene/prefab untouched.");
    }

    private static float Range(float[] range, int index, float fallback)
    {
        return range != null && range.Length > index ? range[index] : fallback;
    }

    [MenuItem("SPELLSTRIKE/Hall of Chronicles/3 - Enable Ambient and Save Preview")]
    public static void EnableAmbient()
    {
        Scene scene = RequirePreviewScene();
        HallOfChroniclesAmbient ambient = GameObject.Find(CanvasName)?
            .GetComponentInChildren<HallOfChroniclesAmbient>();
        if (ambient == null || ambient.LayerCount != 38)
            throw new InvalidOperationException("Static Hall assembly must exist with 38 visible ambient layers.");
        ambient.SetAnimationPreviewEnabled(true);
        EditorUtility.SetDirty(ambient);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[HallOfChronicles] Restrained ambient animation enabled in isolated preview scene.");
    }

    [MenuItem("SPELLSTRIKE/Hall of Chronicles/4 - Restore Manifest Static Pose")]
    public static void RestorePose()
    {
        Scene scene = RequirePreviewScene();
        var ambient = GameObject.Find(CanvasName)?.GetComponentInChildren<HallOfChroniclesAmbient>();
        if (ambient == null)
            throw new InvalidOperationException("No Hall background was found.");
        ambient.RestoreManifestDefaults();
        EditorUtility.SetDirty(ambient);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    [MenuItem("SPELLSTRIKE/Hall of Chronicles/5 - Preview Minimum Ambient Pose")]
    public static void PreviewMinimumPose()
    {
        PreviewAmbientBound(false);
    }

    [MenuItem("SPELLSTRIKE/Hall of Chronicles/6 - Preview Maximum Ambient Pose")]
    public static void PreviewMaximumPose()
    {
        PreviewAmbientBound(true);
    }

    private static void PreviewAmbientBound(bool maximum)
    {
        RequirePreviewScene();
        var ambient = GameObject.Find(CanvasName)?.GetComponentInChildren<HallOfChroniclesAmbient>();
        if (ambient == null || ambient.LayerCount != 38)
            throw new InvalidOperationException("Expected 38 live ambient layers in isolated preview.");
        ambient.PreviewBound(maximum);
        Debug.Log("[HallOfChronicles] Inspecting " +
            (maximum ? "MAXIMUM" : "MINIMUM") +
            " manifest-bounded pose. Use menu 4 to restore before leaving Edit Mode.");
    }

    [MenuItem("SPELLSTRIKE/Hall of Chronicles/7 - Retire Hidden Blue Archway Glow")]
    public static void RetireHiddenBlueArchwayGlow()
    {
        Scene scene = RequirePreviewScene();
        var ambient = GameObject.Find(CanvasName)?.GetComponentInChildren<HallOfChroniclesAmbient>();
        if (ambient == null || (ambient.LayerCount != 39 && ambient.LayerCount != 38))
            throw new InvalidOperationException("Expected existing isolated Hall ambient component.");
        Undo.RecordObject(ambient, "Retire Occluded Blue Archway");
        bool retired = ambient.RetireOccludedBlueArchway();
        EditorUtility.SetDirty(ambient);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("Could not save Hall preview after retiring occluded glow.");
        Debug.Log("[HallOfChronicles] Hidden blue archway glow " +
            (retired ? "disabled and removed from the ambient update list" : "was already retired") +
            ". Retained source PNG for reference; production/book files untouched.");
    }
}
