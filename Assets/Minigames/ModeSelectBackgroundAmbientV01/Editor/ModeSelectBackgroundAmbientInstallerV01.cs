using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Visible, explicit background-only assembly; no automatic scene mutation.</summary>
public static class ModeSelectBackgroundAmbientInstallerV01
{
    public const string SourceScene = "Assets/Scenes/MinigameModeSelect_Integration_v01.unity";
    public const string Folder = "Assets/Minigames/ModeSelectBackgroundAmbientV01";
    public const string PreviewScene = Folder + "/QA/BackgroundAmbient_Preview_v01.unity";
    public const string OwnerName = "ModeSelect_BackgroundAmbient";
    private const string Art = Folder + "/Art/";
    private const string OriginalArt = "Assets/Minigames/ModeSelectWorkbench/TentInteriorApproved.png";
    private const string SessionKey = "SPELLSTRIKE.BackgroundAmbient.V01.";
    [Serializable] private sealed class NativeSource { public int width; public int height; }
    [Serializable] private sealed class Entry {
        public string kind; public string name; public string file; public string parentLanternName;
        public float[] crop; public float[] pivotSource; public float[] attachNative; public float phase; public float period;
    }
    [Serializable] private sealed class Manifest { public NativeSource source; public Entry[] assets; }

    [MenuItem("SPELLSTRIKE/Minigames/Background Ambient/1 Open Isolated Preview")]
    public static void Preview()
    {
        Guard();
        PrepareArt();
        EnsureFolder(Folder + "/QA");
        if (!File.Exists(PreviewScene) && !AssetDatabase.CopyAsset(SourceScene, PreviewScene))
            throw new IOException("Could not copy the saved selection scene for isolated background review.");
        Scene previous = SceneManager.GetActiveScene();
        SessionState.SetString(SessionKey + "PreviousActive", previous.path);
        Scene scene = SceneManager.GetSceneByPath(PreviewScene);
        if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene(PreviewScene, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        if (scene.isDirty) throw new InvalidOperationException("Preview contains unsaved edits; preserve them before installing.");
        Install(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save owned background preview.");
        SetPlayStart(PreviewScene);
        Debug.Log("[Background Ambient] Isolated preview saved. Source scene and other open scenes remain untouched.");
    }

    [MenuItem("SPELLSTRIKE/Minigames/Background Ambient/2 Apply And Save Background Only")]
    public static void ApplyActual()
    {
        Guard();
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != SourceScene || scene.isDirty)
            throw new InvalidOperationException("Activate the saved integration scene; unrelated unsaved scene edits are never overwritten.");
        PrepareArt();
        Install(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save the background-only scene change.");
        Debug.Log("[Background Ambient] Saved only the approved background ambience. Cards, layout, hitboxes, hover/pressed behavior, tile preview and navigation were not edited.");
    }

    [MenuItem("SPELLSTRIKE/Minigames/Background Ambient/3 Use Actual Scene As Play Start")]
    public static void UseActual()
    {
        Guard();
        SetPlayStart(SourceScene);
    }

    [MenuItem("SPELLSTRIKE/Minigames/Background Ambient/5 Restore Previous Editor Setup")]
    public static void RestoreSetup()
    {
        Guard();
        if (SessionState.GetBool(SessionKey + "BackedUp", false))
        {
            string prior = SessionState.GetString(SessionKey + "PreviousStart", "");
            EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(prior) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(prior);
            SessionState.SetBool(SessionKey + "BackedUp", false);
        }
        Scene previous = SceneManager.GetSceneByPath(SessionState.GetString(SessionKey + "PreviousActive", SourceScene));
        if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        Scene owned = SceneManager.GetSceneByPath(PreviewScene);
        if (owned.IsValid() && owned.isLoaded && !owned.isDirty) EditorSceneManager.CloseScene(owned, true);
        Debug.Log("[Background Ambient] Prior Play/active scene restored; only a clean owned preview may have been closed. Other dirty scenes were retained.");
    }

    private static void SetPlayStart(string path)
    {
        if (!SessionState.GetBool(SessionKey + "BackedUp", false))
        {
            string prior = EditorSceneManager.playModeStartScene == null ? "" : AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene);
            SessionState.SetString(SessionKey + "PreviousStart", prior);
            if (string.IsNullOrEmpty(SessionState.GetString(SessionKey + "PreviousActive", "")))
                SessionState.SetString(SessionKey + "PreviousActive", SceneManager.GetActiveScene().path);
            SessionState.SetBool(SessionKey + "BackedUp", true);
        }
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
        Debug.Log("[Background Ambient] Play starts only " + path + "; pre-existing open scenes retain their Edit Mode state.");
    }

    private static Manifest LoadManifest()
    {
        var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Art + "manifest.json"));
        if (manifest == null || manifest.source == null || manifest.source.width != 1672 || manifest.source.height != 941 || manifest.assets == null)
            throw new InvalidOperationException("Expected native approved-background manifest 1672x941.");
        return manifest;
    }

    private static void PrepareArt()
    {
        foreach (Entry entry in LoadManifest().assets)
        {
            string path = Art + entry.file;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new FileNotFoundException("Missing owned ambient image", path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = entry.kind != "cleanPlate";
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = entry.kind == "cleanPlate" ? 2048 : 256;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }

    private static void Install(Scene scene)
    {
        Transform existing = Find(scene, OwnerName, false);
        if (existing != null)
        {
            if (existing.GetComponent<ModeSelectBackgroundAmbientMotion>() == null) throw new InvalidOperationException("An unknown object already uses the ambient owner name.");
            Debug.Log("[Background Ambient] Already installed; refusing duplicate painted props.");
            return;
        }
        var backdrop = Find(scene, "Tent_Backdrop_Approved", true).GetComponent<UnityEngine.UI.Image>();
        if (backdrop == null || backdrop.raycastTarget || backdrop.sprite == null || AssetDatabase.GetAssetPath(backdrop.sprite) != OriginalArt)
            throw new InvalidOperationException("Expected the original noninteractive approved tent Image.");
        Manifest data = LoadManifest();
        var sprites = new Dictionary<string, Sprite>();
        foreach (Entry entry in data.assets)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + entry.file);
            if (sprite == null) throw new FileNotFoundException("Ambient sprite was not imported", entry.file);
            sprites.Add(entry.name, sprite);
        }
        Undo.IncrementCurrentGroup();
        int undo = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Background-only tent ambience");
        Undo.RecordObject(backdrop, "Use locally repaired tent plate");
        backdrop.sprite = sprites["TentInterior_CleanPlate"];
        var owner = new GameObject(OwnerName, typeof(RectTransform), typeof(Canvas), typeof(ModeSelectBackgroundAmbientMotion));
        Undo.RegisterCreatedObjectUndo(owner, "Create owned background ambience");
        owner.transform.SetParent(backdrop.transform, false);
        RectTransform root = owner.GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;
        owner.GetComponent<Canvas>().overrideSorting = false; // isolates dynamic background rebuilds, no new raycaster/camera.
        float sx = backdrop.rectTransform.rect.width / data.source.width;
        float sy = backdrop.rectTransform.rect.height / data.source.height;
        var layers = new List<ModeSelectBackgroundAmbientMotion.Layer>();
        var images = new Dictionary<string, UnityEngine.UI.Image>();
        var links = new List<ModeSelectBackgroundAmbientMotion.ChainLink>();

        // Side chains render behind the original lantern body and main-chain cutout.
        foreach (Entry entry in data.assets) if (entry.kind == "chain")
            images.Add(entry.name, Place(entry.name, sprites[entry.name], root, entry.crop, entry.pivotSource, sx, sy));
        int lanternIndex = 0;
        foreach (Entry entry in data.assets) if (entry.kind == "lantern")
        {
            var image = Place(entry.name, sprites[entry.name], root, entry.crop, entry.pivotSource, sx, sy);
            images.Add(entry.name, image);
            layers.Add(new ModeSelectBackgroundAmbientMotion.Layer { target = image.rectTransform, image = image,
                kind = ModeSelectBackgroundAmbientMotion.LayerKind.Lantern, phase = entry.phase,
                period = lanternIndex == 0 ? 6.3f : 7.15f, swayDegrees = 4.35f,
                brightnessVariation = .095f, flickerSpeed = lanternIndex == 0 ? 1.03f : 1.17f });
            float x = lanternIndex == 0 ? 352 : 1328;
            float y = lanternIndex == 0 ? 123 : 122;
            var light = Glow(entry.name + "_GlassLight", sprites["WarmGlow"], root, new Vector2(x, y), new Vector2(78, 105), sx, sy, .24f);
            light.transform.SetParent(image.transform, true);
            layers.Add(new ModeSelectBackgroundAmbientMotion.Layer { target = light.rectTransform, image = light,
                kind = ModeSelectBackgroundAmbientMotion.LayerKind.Glow, phase = entry.phase + .19f,
                period = 1.65f + lanternIndex * .27f, alphaVariation = .65f });
            lanternIndex++;
        }
        foreach (Entry entry in data.assets) if (entry.kind == "chain")
        {
            RectTransform body = images[entry.parentLanternName].rectTransform;
            Vector3 point = root.TransformPoint(Map(entry.attachNative[0], entry.attachNative[1], sx, sy));
            links.Add(new ModeSelectBackgroundAmbientMotion.ChainLink { target = images[entry.name].rectTransform,
                attachment = body, attachmentLocalPoint = body.InverseTransformPoint(point) });
        }
        // Small pools of light remain local to each painted candle.
        foreach (Entry entry in data.assets) if (entry.kind == "flame")
        {
            var light = Glow(entry.name + "_WarmLight", sprites["WarmGlow"], root,
                new Vector2(entry.pivotSource[0], entry.pivotSource[1] - 12), new Vector2(58, 66), sx, sy, .21f);
            layers.Add(new ModeSelectBackgroundAmbientMotion.Layer { target = light.rectTransform, image = light,
                kind = ModeSelectBackgroundAmbientMotion.LayerKind.Glow, phase = entry.phase + .11f,
                period = 1.18f + entry.phase * .28f, alphaVariation = .82f });
        }
        foreach (Entry entry in data.assets) if (entry.kind == "flame")
        {
            var image = Place(entry.name, sprites[entry.name], root, entry.crop, entry.pivotSource, sx, sy);
            layers.Add(new ModeSelectBackgroundAmbientMotion.Layer { target = image.rectTransform, image = image,
                kind = ModeSelectBackgroundAmbientMotion.LayerKind.Flame, phase = entry.phase,
                period = Mathf.Max(.95f, entry.period * .62f) + .18f * entry.phase,
                swayDegrees = 6.5f,
                widthVariation = .075f, heightVariation = .24f });
        }
        owner.GetComponent<ModeSelectBackgroundAmbientMotion>().Configure(layers.ToArray(), links.ToArray());
        EditorSceneManager.MarkSceneDirty(scene);
        Undo.CollapseUndoOperations(undo);
    }

    private static UnityEngine.UI.Image Place(string name, Sprite sprite, RectTransform root, float[] crop, float[] pivot, float sx, float sy)
    {
        var image = NewImage(name, sprite, root);
        RectTransform rect = image.rectTransform;
        rect.pivot = new Vector2((pivot[0] - crop[0]) / crop[2], 1f - (pivot[1] - crop[1]) / crop[3]);
        rect.sizeDelta = new Vector2(crop[2] * sx, crop[3] * sy);
        rect.anchoredPosition = Map(pivot[0], pivot[1], sx, sy);
        return image;
    }
    private static UnityEngine.UI.Image Glow(string name, Sprite sprite, RectTransform root, Vector2 center, Vector2 size, float sx, float sy, float alpha)
    {
        var image = NewImage(name, sprite, root);
        image.rectTransform.sizeDelta = new Vector2(size.x * sx, size.y * sy);
        image.rectTransform.anchoredPosition = Map(center.x, center.y, sx, sy);
        image.color = new Color(1, 1, 1, alpha);
        return image;
    }
    private static UnityEngine.UI.Image NewImage(string name, Sprite sprite, RectTransform parent)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.pivot = new Vector2(.5f, .5f);
        var image = go.GetComponent<UnityEngine.UI.Image>();
        image.sprite = sprite; image.raycastTarget = false; image.maskable = false;
        return image;
    }
    private static Vector2 Map(float x, float y, float sx, float sy) => new Vector2((x - 836f) * sx, (470.5f - y) * sy);
    public static Transform Find(Scene scene, string name, bool required)
    {
        Transform result = null;
        foreach (GameObject root in scene.GetRootGameObjects()) foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == name) { if (result != null) throw new InvalidOperationException("Duplicate target: " + name); result = child; }
        if (required && !result) throw new InvalidOperationException("Missing target: " + name);
        return result;
    }
    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = path.Substring(0, path.LastIndexOf('/'));
        EnsureFolder(parent); AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
    }
    private static void Guard()
    {
        if (System.Diagnostics.Process.GetProcessesByName("VALORANT-Win64-Shipping").Length > 0) throw new InvalidOperationException("VALORANT running; ambient work stopped.");
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) throw new InvalidOperationException("Exit Play and finish compiling first.");
    }
}
