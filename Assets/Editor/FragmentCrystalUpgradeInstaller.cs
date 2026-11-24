using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Installs original, standalone crystal visuals over the current collectible
/// meshes. The fragment visuals use local Hovl particle-prefab references
/// through FragmentHovlParticleInstaller; they are not redistributable
/// without the applicable third-party license.
/// </summary>
public static class FragmentCrystalUpgradeInstaller
{
    private const string ArtFolder = "Assets/Art/Fragments";
    private const string CrystalMeshPath = ArtFolder + "/FragmentShard_Faceted.asset";
    private const string EdgeMeshPath = ArtFolder + "/FragmentShard_ArcaneEdges.asset";
    private const string HovlBase = "Assets/Hovl Studio/Magic effects pack";
    private const string LocalVendorFolder = HovlBase + "/SPELLSTRIKE Local Assembly";
    private const string GalleryPath = LocalVendorFolder + "/FragmentCrystalGallery_LOCAL.unity";
    private const string GalleryBloomPath = LocalVendorFolder + "/FragmentGalleryBloom_LOCAL.asset";
    private static readonly string[] Stages =
    {
        "Assets/Scenes/SampleScene.unity",
        "Assets/Scenes/Stage2Scene/Stage2Scene.unity",
        "Assets/Scenes/Stage3Scene/Stage3Scene.unity",
        "Assets/Scenes/Stage4Scene/Stage4Scene.unity"
    };

    [MenuItem("SPELLSTRIKE/Fragments/Install Floating Crystal Fragments (Backed Up)")]
    public static void InstallAll()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling)
        {
            Debug.LogWarning("[Crystal Fragments] Stop Play Mode/compilation first.");
            return;
        }
        var originalScene = EditorSceneManager.GetActiveScene();
        if (originalScene.isDirty)
        {
            Debug.LogWarning("[Crystal Fragments] Current scene contains unsaved edits. Save it first; nothing installed.");
            return;
        }
        string originalPath = originalScene.path;
        string backupFolder = "Temp/FragmentCrystalUpgrade_20261123";
        Directory.CreateDirectory(backupFolder);
        foreach (string path in Stages)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Stage scene not found; installation aborted.", path);
            File.Copy(path, Path.Combine(backupFolder,
                path.Replace("Assets/Scenes/", "").Replace('/', '_') + ".before_crystals.unity"), true);
        }

        EnsureFolder("Assets", "Art");
        EnsureFolder("Assets/Art", "Fragments");
        Mesh shard = LoadOrCreateMesh(CrystalMeshPath, false);
        Mesh edges = LoadOrCreateMesh(EdgeMeshPath, true);
        int total = 0;
        try
        {
            foreach (string path in Stages)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var targets = UnityEngine.Object.FindObjectsByType<FragmentPickup>(
                        FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Select(p => p.gameObject)
                    .Concat(UnityEngine.Object.FindObjectsByType<BossFragmentPickup>(
                        FindObjectsInactive.Include, FindObjectsSortMode.None).Select(p => p.gameObject))
                    .Distinct()
                    .ToArray();
                int installed = 0;
                foreach (GameObject target in targets)
                    if (InstallOne(target, shard, edges))
                        ++installed;
                total += installed;
                if (installed > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene))
                        throw new IOException("Failed to save staged crystal scene: " + path);
                }
                Debug.Log("[Crystal Fragments] " + path + ": upgraded " + installed +
                          " / " + targets.Length + " pickups.");
            }
        }
        finally
        {
            if (!string.IsNullOrEmpty(originalPath))
                EditorSceneManager.OpenScene(originalPath, OpenSceneMode.Single);
        }
        Debug.Log("[Crystal Fragments] COMPLETE: " + total +
                  " pickups with original colors, floating visuals, and unchanged trigger colliders.");
    }

    [MenuItem("SPELLSTRIKE/Fragments/Open Local Floating Crystal Gallery")]
    public static void OpenCrystalGallery()
    {
        if (EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty)
        {
            Debug.LogWarning("[Crystal Fragments] Save the scene and stop Play Mode before opening the gallery.");
            return;
        }
        if (File.Exists(GalleryPath))
        {
            EditorSceneManager.OpenScene(GalleryPath, OpenSceneMode.Single);
            PolishGallery();
            return;
        }
        EnsureFolder(HovlBase, "SPELLSTRIKE Local Assembly");
        var gallery = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var light = new GameObject("Crystal Gallery Key Light").AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.7f;
        light.color = new Color(0.80f, 0.88f, 1f);
        light.transform.rotation = Quaternion.Euler(35f, -45f, 0f);

        var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camera.tag = "MainCamera";
        camera.transform.position = new Vector3(0f, 5f, -22f);
        camera.transform.LookAt(new Vector3(0f, 2f, 0f));
        Camera view = camera.GetComponent<Camera>();
        view.clearFlags = CameraClearFlags.SolidColor;
        view.backgroundColor = new Color(0.011f, 0.019f, 0.048f);
        view.fieldOfView = 54f;
        view.allowHDR = true;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.24f, 0.30f, 0.42f);

        // Copy *our own child visuals*, NOT the original pickup logic.
        // Source stages are briefly loaded additively, then closed.
        var entries = new[]
        {
            (path: Stages[0], material: "Crystal_72ADBE", label: "STAGE 1  /  CYAN", x: -6f),
            (path: Stages[1], material: "Crystal_B500C5", label: "STAGE 2  /  PURPLE", x: 0f),
            (path: Stages[2], material: "Crystal_751FE0", label: "STAGE 3  /  VIOLET", x: 6f)
        };
        foreach (var entry in entries)
        {
            var source = EditorSceneManager.OpenScene(entry.path, OpenSceneMode.Additive);
            var sourcePickup = UnityEngine.Object.FindObjectsByType<FragmentPickup>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(p => p.gameObject.scene == source &&
                    p.transform.Find("FragmentCrystal_Visual") != null &&
                    p.transform.Find("FragmentCrystal_Visual")
                        .GetComponentInChildren<MeshRenderer>(true).sharedMaterial.name
                        .Contains(entry.material));
            if (sourcePickup == null)
            {
                Debug.LogWarning("[Crystal Fragments] Cannot show gallery entry " + entry.label);
                EditorSceneManager.CloseScene(source, true);
                continue;
            }
            GameObject visual = UnityEngine.Object.Instantiate(
                sourcePickup.transform.Find("FragmentCrystal_Visual").gameObject);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(visual, gallery);
            var root = new GameObject(entry.label);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, gallery);
            root.transform.position = new Vector3(entry.x, 2.2f, 0f);
            root.transform.localScale = Vector3.one * 1.35f;
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.name = "FragmentCrystal_Visual";
            var hover = root.AddComponent<FragmentCrystalFloat>();
            var serialized = new SerializedObject(hover);
            serialized.FindProperty("visualPivot").objectReferenceValue = visual.transform;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.CloseScene(source, true);
        }

        PolishGallery();
        EditorSceneManager.MarkSceneDirty(gallery);
        if (EditorSceneManager.SaveScene(gallery, GalleryPath))
            Debug.Log("[Crystal Fragments] Opened local-only gallery " + GalleryPath);
    }

    private static void PolishGallery()
    {
        var scene = EditorSceneManager.GetActiveScene();
        var finished = GameObject.Find("FINAL ASSEMBLED  /  HOVL CRYSTAL");
        if (finished != null)
            UnityEngine.Object.DestroyImmediate(finished);
        var galleryCanvas = GameObject.Find("CrystalGallery_Labels");
        if (galleryCanvas != null)
        {
            var oldFinalLabel = galleryCanvas.transform.Find("CrystalLabel_3");
            if (oldFinalLabel != null)
                UnityEngine.Object.DestroyImmediate(oldFinalLabel.gameObject);
        }
        foreach (var stage in UnityEngine.Object.FindObjectsByType<FragmentCrystalFloat>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (stage.gameObject.scene != scene ||
                !stage.name.StartsWith("STAGE ", StringComparison.Ordinal))
                continue;
            float x = stage.name.Contains("STAGE 1") ? -6f :
                stage.name.Contains("STAGE 2") ? 0f : 6f;
            stage.transform.position = new Vector3(x, 2.2f, 0f);
        }
        foreach (var oldText in UnityEngine.Object.FindObjectsByType<TextMesh>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (oldText.gameObject.scene == scene && oldText.name == "GalleryLabel")
                UnityEngine.Object.DestroyImmediate(oldText.gameObject);
        }

        var camera = Camera.main;
        if (camera != null)
        {
            UniversalAdditionalCameraData data =
                camera.GetComponent<UniversalAdditionalCameraData>();
            if (data == null)
                data = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
        }
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(GalleryBloomPath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = profile.Add<Bloom>();
            bloom.threshold.Override(0.55f);
            bloom.intensity.Override(0.8f);
            bloom.scatter.Override(0.70f);
            AssetDatabase.CreateAsset(profile, GalleryBloomPath);
        }
        var volumeObject = GameObject.Find("FragmentGallery_Bloom");
        if (volumeObject == null)
            volumeObject = new GameObject("FragmentGallery_Bloom", typeof(Volume));
        Volume volume = volumeObject.GetComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 12f;
        volume.sharedProfile = profile;

        var ui = GameObject.Find("CrystalGallery_Labels");
        if (ui == null)
        {
            ui = new GameObject("CrystalGallery_Labels", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler));
            var canvas = ui.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = ui.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }
        var names = new[] { "STAGE 1  |  CYAN", "STAGE 2  |  PURPLE",
            "STAGE 3  |  VIOLET" };
        float[] anchors = { 0.27f, 0.50f, 0.73f };
        for (int i = 0; i < names.Length; ++i)
        {
            string objectName = "CrystalLabel_" + i;
            Transform item = ui.transform.Find(objectName);
            if (item == null)
            {
                GameObject labelObject = new GameObject(objectName,
                    typeof(RectTransform), typeof(TextMeshProUGUI));
                item = labelObject.transform;
                item.SetParent(ui.transform, false);
            }
            var rect = item.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(anchors[i], 0.12f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(440f, 78f);
            rect.anchoredPosition = Vector2.zero;
            var tmp = item.GetComponent<TextMeshProUGUI>();
            tmp.text = names[i];
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 36f;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 24f;
            tmp.fontSizeMax = 36f;
            tmp.color = new Color(0.80f, 0.89f, 1f);
            tmp.raycastTarget = false;
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static bool InstallOne(GameObject target, Mesh shard, Mesh edges)
    {
        if (target.GetComponent<FragmentCrystalFloat>() != null ||
            target.transform.Find("FragmentCrystal_Visual") != null)
            return false;
        MeshFilter sourceMesh = target.GetComponent<MeshFilter>();
        MeshRenderer oldRenderer = target.GetComponent<MeshRenderer>();
        if (sourceMesh == null || sourceMesh.sharedMesh == null || oldRenderer == null ||
            oldRenderer.sharedMaterial == null)
        {
            Debug.LogWarning("[Crystal Fragments] Missing original mesh/material on " +
                             target.name + "; leaving this pickup unchanged.");
            return false;
        }

        Color tint = oldRenderer.sharedMaterial.HasProperty("_BaseColor")
            ? oldRenderer.sharedMaterial.GetColor("_BaseColor")
            : oldRenderer.sharedMaterial.color;
        // Some legacy transparent FragmentMaterials have alpha=0, though
        // their RGB is still the authored color. Preserve RGB, not that alpha.
        tint.a = 1f;
        if (tint.maxColorComponent < 0.05f)
            tint = new Color(0.15f, 0.75f, 1f);
        string id = ColorUtility.ToHtmlStringRGB(tint);

        Material[] facets =
        {
            GetOrCreateMaterial("Crystal_" + id + "_Deep.mat", tint * 0.42f, false, false),
            GetOrCreateMaterial("Crystal_" + id + "_Base.mat", tint * 0.91f, false, false),
            GetOrCreateMaterial("Crystal_" + id + "_Bright.mat", tint * 1.32f, false, false)
        };
        Material rim = GetOrCreateMaterial("Crystal_" + id + "_ArcaneRim.mat",
            tint * 3.0f, true, false);
        Material core = GetOrCreateMaterial("Crystal_" + id + "_InnerLight.mat",
            tint * 2.0f, true, false);

        Bounds bounds = sourceMesh.sharedMesh.bounds;
        var visual = new GameObject("FragmentCrystal_Visual");
        visual.transform.SetParent(target.transform, false);
        visual.transform.localPosition = bounds.center;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = new Vector3(
            Mathf.Max(0.3f, bounds.extents.x * 1.32f),
            Mathf.Max(0.6f, bounds.extents.y),
            Mathf.Max(0.3f, bounds.extents.z * 1.32f));

        GameObject shell = AddMesh(visual.transform, "FacetedCrystal_Shell",
            shard, facets, Vector3.one);
        AddMesh(visual.transform, "ArcaneGlowing_Seams",
            edges, new[] {rim}, Vector3.one * 1.01f);
        AddMesh(visual.transform, "Luminous_Heart",
            shard, new[] {core, core, core}, Vector3.one * 0.44f);

        FragmentHovlParticleInstaller.ApplyToVisual(visual.transform);

        var hover = target.AddComponent<FragmentCrystalFloat>();
        var so = new SerializedObject(hover);
        so.FindProperty("visualPivot").objectReferenceValue = visual.transform;
        so.FindProperty("hoverDistance").floatValue = bounds.extents.y * 0.095f;
        so.ApplyModifiedPropertiesWithoutUndo();
        oldRenderer.enabled = false; // Preserve original mesh and authored material for rollback.
        EditorUtility.SetDirty(oldRenderer);
        EditorUtility.SetDirty(target);
        return true;
    }

    private static GameObject AddMesh(Transform parent, string name,
        Mesh mesh, Material[] materials, Vector3 scale)
    {
        GameObject go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false);
        go.transform.localScale = scale;
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        go.GetComponent<MeshRenderer>().sharedMaterials = materials;
        return go;
    }

    private static Material GetOrCreateMaterial(string name, Color color,
        bool bright, bool particle)
    {
        string path = ArtFolder + "/" + name;
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
            return existing;
        Shader shader = Shader.Find(particle
            ? "Universal Render Pipeline/Particles/Unlit" : "Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");
        Material material = new Material(shader);
        material.name = Path.GetFileNameWithoutExtension(name);
        Color baseColor = new Color(color.r, color.g, color.b, 1f);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", baseColor);
        else
            material.color = baseColor;
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", 0.82f);
        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", bright ? 0f : 0.22f);
        if (material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", color * (bright ? 2f : 0.45f));
            material.EnableKeyword("_EMISSION");
        }
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static Mesh LoadOrCreateMesh(string path, bool edges)
    {
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
            return existing;
        Mesh mesh = edges ? BuildArcaneEdges() : BuildFacetedShard();
        mesh.name = Path.GetFileNameWithoutExtension(path);
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    private static Mesh BuildFacetedShard()
    {
        // Original 8-sided elongated bipyramid with a segmented central
        // facet band, not the third-party Hovl Crystal1 model.
        var vertices = new List<Vector3>();
        var submeshTriangles = new[] { new List<int>(), new List<int>(), new List<int>() };
        int sides = 8;
        Func<int, float, float, Vector3> ring = (i, y, radius) =>
        {
            float a = i * Mathf.PI * 2f / sides;
            return new Vector3(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius);
        };
        Action<Vector3, Vector3, Vector3, int> triangle = (a, b, c, mat) =>
        {
            int at = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c);
            submeshTriangles[mat].Add(at);
            submeshTriangles[mat].Add(at + 1);
            submeshTriangles[mat].Add(at + 2);
        };
        for (int i = 0; i < sides; i++)
        {
            int j = (i + 1) % sides;
            Vector3 hi = ring(i, 0.30f, 0.42f);
            Vector3 hj = ring(j, 0.30f, 0.42f);
            Vector3 lo = ring(i, -0.16f, 0.50f);
            Vector3 lj = ring(j, -0.16f, 0.50f);
            triangle(new Vector3(0f, 1f, 0f), hi, hj, i % 3);
            triangle(hi, lo, lj, (i + 1) % 3);
            triangle(hi, lj, hj, (i + 2) % 3);
            triangle(lo, new Vector3(0f, -1f, 0f), lj, (i + 1) % 3);
        }
        Mesh mesh = new Mesh { vertices = vertices.ToArray(), subMeshCount = 3 };
        for (int i = 0; i < 3; i++)
            mesh.SetTriangles(submeshTriangles[i], i);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh BuildArcaneEdges()
    {
        var v = new List<Vector3>();
        var triangles = new List<int>();
        Action<Vector3, Vector3> beam = (from, to) =>
        {
            Vector3 dir = (to - from).normalized;
            Vector3 perp = Vector3.Cross(dir, Vector3.up);
            if (perp.sqrMagnitude < 0.03f)
                perp = Vector3.Cross(dir, Vector3.right);
            perp = perp.normalized * 0.009f;
            int start = v.Count;
            v.Add(from - perp); v.Add(from + perp);
            v.Add(to + perp); v.Add(to - perp);
            triangles.AddRange(new[] {start, start + 1, start + 2, start, start + 2, start + 3,
                start + 2, start + 1, start, start + 3, start + 2, start});
        };
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI * 0.25f;
            float b = (i + 1) * Mathf.PI * 0.25f;
            Vector3 top = new Vector3(Mathf.Cos(a) * 0.42f, 0.3f, Mathf.Sin(a) * 0.42f);
            Vector3 nextTop = new Vector3(Mathf.Cos(b) * 0.42f, 0.3f, Mathf.Sin(b) * 0.42f);
            Vector3 bottom = new Vector3(Mathf.Cos(a) * 0.5f, -0.16f, Mathf.Sin(a) * 0.5f);
            beam(new Vector3(0f, 1.01f, 0f), top);
            beam(top, nextTop);
            beam(top, bottom);
            beam(bottom, new Vector3(0f, -1.01f, 0f));
        }
        Mesh mesh = new Mesh { vertices = v.ToArray(), triangles = triangles.ToArray() };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void EnsureFolder(string parent, string name)
    {
        string child = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(child))
            AssetDatabase.CreateFolder(parent, name);
    }
}
