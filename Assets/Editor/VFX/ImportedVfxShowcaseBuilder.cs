using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Creates a separate showcase for all imported Hovl and Vefects prefab VFX.
/// Creates the scene additively so even a dirty active gameplay scene remains
/// untouched and open. This does not add the showcase to build settings.
/// </summary>
public static class ImportedVfxShowcaseBuilder
{
    public const string ScenePath = "Assets/Scenes/VFXLab/Imported_VFX_Lab.unity";
    private const string LabFolder = "Assets/Scenes/VFXLab";
    private const string HovlFolder = "Assets/Hovl Studio/Magic effects pack/Prefabs";
    private const string TrailsFolder = "Assets/Vefects/Trails VFX URP/VFX/Particles";
    private const string ZapFolder = "Assets/Vefects/Zap VFX URP/VFX/Zap/Particles";

    [MenuItem("SPELLSTRIKE/VFX/Create Imported VFX Lab")]
    public static void BuildFromMenu()
    {
        string result = Build();
        Debug.Log("[ImportedVfxShowcaseBuilder] " + result);
    }

    [MenuItem("SPELLSTRIKE/VFX/Rebuild Imported VFX Lab")]
    public static void RebuildFromMenu()
    {
        string result = Build(replaceExisting: true);
        Debug.Log("[ImportedVfxShowcaseBuilder] " + result);
    }

    public static string Build(bool replaceExisting = false)
    {
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before building the standalone VFX Lab.");

        if (!replaceExisting &&
            (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null ||
             File.Exists(ScenePath)))
            return "VFX Lab already exists at " + ScenePath +
                   ". It was preserved rather than overwritten.";

        EnsureFolder("Assets/Scenes");
        EnsureFolder(LabFolder);
        EnsureFolder(LabFolder + "/Materials");

        Material ground = GetOrCreateMaterial("StageFloor.mat", new Color(0.042f, 0.066f, 0.10f));
        Material platform = GetOrCreateMaterial("StationPlatform.mat", new Color(0.11f, 0.17f, 0.24f));
        // Bright enough to read as an intentional target against the dark
        // stage; the imported Hovl demo's original capsule material is black.
        Material capsuleMaterial = GetOrCreateMaterial("TargetCapsule.mat",
            new Color(0.24f, 0.47f, 0.70f));

        Scene previousActive = SceneManager.GetActiveScene();
        Scene lab = default;
        bool sceneSaved = false;
        try
        {
            // This is explicitly ADDITIVE. Never close or save the user's
            // current Stage 1/2/3/4 scene, which may contain unsaved work.
            lab = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(lab);
            GameObject root = new GameObject("_IMPORTED_VFX_LAB");
            GameObject groupContainer = Child(root, "_CATEGORY_GALLERIES");
            Camera mainCamera = MakeCamera(root.transform);
            MakeLighting(root.transform);
            // The on-screen Play Mode UI is our readable lab header. Avoid
            // placing giant 3D text between the camera and the capsules.

            List<(string category, string path, bool trail)> prefabs = DiscoverImportedEffects();
            List<ImportedVfxShowcaseController.Entry> entries = new();
            int row = 0;
            foreach (IGrouping<string, (string category, string path, bool trail)> category
                in prefabs.GroupBy(x => x.category))
            {
                List<(string category, string path, bool trail)> items = category.ToList();
                float z = row * 21f;
                GameObject categoryRoot = Child(groupContainer, $"{row + 1:00}  {category.Key}");
                CreateBlock(categoryRoot.transform, "Category floor",
                    new Vector3(0f, -0.54f, z),
                    new Vector3(Mathf.Max(28f, items.Count * 10.5f + 10f), 0.6f, 16f),
                    ground);
                // Category grouping lives in the Hierarchy and OnGUI header.
                // Large world-space category signs would sit in front of the
                // effects when the camera focuses on a nearby capsule.

                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    float x = (i - (items.Count - 1) * 0.5f) * 10.5f;
                    GameObject station = Child(categoryRoot,
                        $"{i + 1:00}  {Path.GetFileNameWithoutExtension(item.path)}");
                    station.transform.position = new Vector3(x, 0f, z);

                    CreateBlock(station.transform, "Display plinth",
                        new Vector3(0f, 0.08f, 0f), new Vector3(7.2f, 0.3f, 7.1f), platform,
                        local: true);

                    GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    capsule.name = "Capsule  •  VFX test target";
                    capsule.transform.SetParent(station.transform, false);
                    capsule.transform.localPosition = new Vector3(0f, 1.23f, 0f);
                    capsule.transform.localScale = new Vector3(1.05f, 1.05f, 1.05f);
                    capsule.GetComponent<Renderer>().sharedMaterial = capsuleMaterial;

                    string label = Path.GetFileNameWithoutExtension(item.path);
                    CreateText(station.transform, "Effect name", label,
                        new Vector3(0f, 0.9f, -3.5f), 0.13f, true);

                    // The holder is the only thing activated during preview:
                    // capsules, labels and the whole editor gallery remain
                    // in-place as visible testing targets.
                    GameObject holder = Child(station, "Imported effect  [PREVIEW]");
                    float height = item.trail ? 2.0f : GetEffectHeight(item.category);
                    holder.transform.localPosition = new Vector3(0f, height, 0f);
                    if (item.trail)
                        holder.AddComponent<ImportedVfxShowcaseTrailMotion>();

                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(item.path);
                    if (prefab == null)
                        throw new InvalidOperationException("Missing imported VFX prefab: " + item.path);
                    GameObject instance = PrefabUtility.InstantiatePrefab(prefab, lab) as GameObject;
                    if (instance == null)
                        throw new InvalidOperationException("Failed to instantiate VFX prefab: " + item.path);
                    instance.transform.SetParent(holder.transform, false);
                    instance.transform.localPosition = Vector3.zero;
                    instance.transform.localRotation = Quaternion.identity;
                    instance.name = "SOURCE PREFAB  •  " + prefab.name;
                    holder.SetActive(false);

                    entries.Add(new ImportedVfxShowcaseController.Entry
                    {
                        category = category.Key,
                        displayName = label,
                        station = station.transform,
                        effect = holder
                    });
                }
                row++;
            }

            ImportedVfxShowcaseController controller =
                root.AddComponent<ImportedVfxShowcaseController>();
            controller.Configure(mainCamera, entries);
            if (entries.Count == 0)
                throw new InvalidOperationException("No imported VFX prefabs were found.");

            sceneSaved = EditorSceneManager.SaveScene(lab, ScenePath);
            if (!sceneSaved)
                throw new IOException("Could not save " + ScenePath);
            return $"Created {ScenePath} with {entries.Count} original imported VFX prefabs, " +
                   $"{entries.Count} capsule targets and {row} labeled categories. " +
                   "Open this scene and press Play to preview one effect at a time.";
        }
        finally
        {
            // Close only our isolated new scene. No existing open scene is
            // saved, unloaded, modified, or replaced by this builder.
            if (previousActive.IsValid() && previousActive.isLoaded)
                SceneManager.SetActiveScene(previousActive);
            if (lab.IsValid() && lab.isLoaded)
                EditorSceneManager.CloseScene(lab, true);
        }
    }

    private static List<(string category, string path, bool trail)> DiscoverImportedEffects()
    {
        var list = new List<(string category, string path, bool trail)>();
        AddFolder(HovlFolder, false, list);
        AddFolder(TrailsFolder, true, list);
        AddFolder(ZapFolder, false, list);
        return list.OrderBy(x => x.category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.path, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static void AddFolder(string folder, bool trail,
        List<(string category, string path, bool trail)> list)
    {
        if (!AssetDatabase.IsValidFolder(folder))
            throw new DirectoryNotFoundException(folder);
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                continue;
            string category = folder == HovlFolder
                ? "Hovl • " + Path.GetFileName(Path.GetDirectoryName(path))
                : trail ? "Vefects • Animated Trails" : "Vefects • Lightning Zaps";
            list.Add((category, path, trail));
        }
    }

    private static float GetEffectHeight(string category)
    {
        if (category.Contains("Magic circles") || category.Contains("AoE effects") ||
            category.Contains("Smoke effects") || category.Contains("Environment"))
            return 0.35f;
        if (category.Contains("Character auras"))
            return 0.45f;
        if (category.Contains("Magic shields") || category.Contains("Portals"))
            return 1.3f;
        return 1.7f;
    }

    private static Camera MakeCamera(Transform parent)
    {
        GameObject cameraObject = Child(parent.gameObject, "Lab Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 7.2f, -26f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.014f, 0.025f, 0.05f);
        camera.fieldOfView = 61f;
        camera.nearClipPlane = 0.04f;
        camera.farClipPlane = 1000f;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<UniversalAdditionalCameraData>();
        return camera;
    }

    private static void MakeLighting(Transform parent)
    {
        GameObject lightObject = Child(parent.gameObject, "Soft preview light");
        lightObject.transform.rotation = Quaternion.Euler(38f, -35f, 0f);
        Light sunlight = lightObject.AddComponent<Light>();
        sunlight.type = LightType.Directional;
        sunlight.intensity = 1.25f;
        sunlight.color = new Color(0.78f, 0.87f, 1f);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.24f, 0.29f, 0.39f);
        RenderSettings.fog = false;
    }

    private static GameObject Child(GameObject parent, string name)
    {
        GameObject result = new GameObject(name);
        result.transform.SetParent(parent.transform, false);
        return result;
    }

    private static void CreateBlock(Transform parent, string name, Vector3 pos,
        Vector3 scale, Material material, bool local = false)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.SetParent(parent, false);
        if (local) block.transform.localPosition = pos;
        else block.transform.position = pos;
        block.transform.localScale = scale;
        block.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static void CreateText(Transform parent, string name, string value,
        Vector3 pos, float size, bool local = false)
    {
        GameObject holder = Child(parent.gameObject, name);
        if (local) holder.transform.localPosition = pos;
        else holder.transform.position = pos;
        // TextMesh already faces the lab camera (placed on negative Z) at
        // identity rotation. Rotating it 180 degrees mirrors the lettering.
        holder.transform.rotation = Quaternion.identity;
        TextMesh text = holder.AddComponent<TextMesh>();
        text.text = value;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.fontSize = 48;
        text.characterSize = size;
        text.color = new Color(0.86f, 0.94f, 1f);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    private static Material GetOrCreateMaterial(string file, Color tint)
    {
        string path = LabFolder + "/Materials/" + file;
        Material result = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (result != null)
            return result;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ??
                        Shader.Find("Standard");
        result = new Material(shader) { name = Path.GetFileNameWithoutExtension(file) };
        if (result.HasProperty("_BaseColor"))
            result.SetColor("_BaseColor", tint);
        if (result.HasProperty("_Color"))
            result.SetColor("_Color", tint);
        AssetDatabase.CreateAsset(result, path);
        return result;
    }
}
