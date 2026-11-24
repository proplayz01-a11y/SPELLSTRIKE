using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Locally uses Hovl's actual Sparks and Flares prefab particles around our
/// own fragment meshes. Never duplicates vendor meshes/materials into the
/// first-party art folders. The source Hovl pack is a LOCAL dependency only;
/// neither it nor these generated scene references may be published as a
/// self-contained asset bundle without the appropriate licensing.
/// </summary>
public static class FragmentHovlParticleInstaller
{
    public const string HovlCrystalPath =
        "Assets/Hovl Studio/Magic effects pack/Prefabs/Environment/Crystal effect blue.prefab";
    public const string HaloName = "HovlCrystalParticles_LOCAL";
    public const string GalleryPath =
        "Assets/Hovl Studio/Magic effects pack/SPELLSTRIKE Local Assembly/FragmentCrystalGallery_LOCAL.unity";

    private static readonly string[] Scenes =
    {
        "Assets/Scenes/SampleScene.unity",
        "Assets/Scenes/Stage2Scene/Stage2Scene.unity",
        "Assets/Scenes/Stage3Scene/Stage3Scene.unity",
        "Assets/Scenes/Stage4Scene/Stage4Scene.unity",
        GalleryPath
    };

    [MenuItem("SPELLSTRIKE/Fragments/Use Hovl Particles Around Fragments (Local Only)")]
    public static void ApplyAll()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling ||
            EditorSceneManager.GetActiveScene().isDirty)
        {
            Debug.LogWarning("[Fragment Hovl FX] Stop Play Mode and save the scene first.");
            return;
        }
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HovlCrystalPath);
        if (prefab == null || prefab.transform.Find("Sparks") == null ||
            prefab.transform.Find("Flares") == null)
        {
            Debug.LogError("[Fragment Hovl FX] Missing local Hovl source prefab or particles.");
            return;
        }

        string originalScene = EditorSceneManager.GetActiveScene().path;
        string backupFolder = "Temp/FragmentHovlHalo_20261123";
        Directory.CreateDirectory(backupFolder);
        foreach (string path in Scenes.Where(File.Exists))
        {
            string dest = Path.Combine(backupFolder,
                path.Replace('/', '_') + ".before_hovl.unity");
            if (!File.Exists(dest))
                File.Copy(path, dest);
        }

        int count = 0;
        try
        {
            foreach (string path in Scenes)
            {
                if (!File.Exists(path))
                    continue;
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                int modified = 0;
                if (path == GalleryPath)
                {
                    foreach (GameObject root in scene.GetRootGameObjects())
                    {
                        if (!root.name.StartsWith("STAGE ", StringComparison.Ordinal))
                            continue;
                        if (ApplyToVisual(root.transform.Find("FragmentCrystal_Visual"), prefab))
                            ++modified;
                    }
                }
                else
                {
                    var pick = UnityEngine.Object.FindObjectsByType<FragmentPickup>(
                            FindObjectsInactive.Include, FindObjectsSortMode.None)
                        .Select(c => c.gameObject);
                    var boss = UnityEngine.Object.FindObjectsByType<BossFragmentPickup>(
                            FindObjectsInactive.Include, FindObjectsSortMode.None)
                        .Select(c => c.gameObject);
                    foreach (GameObject target in pick.Concat(boss).Distinct()
                                 .Where(g => g.scene == scene))
                        if (ApplyToVisual(target.transform.Find("FragmentCrystal_Visual"), prefab))
                            ++modified;
                }

                if (path == GalleryPath)
                    RemoveRejectedFinalFromGallery(scene);
                if (modified > 0 || path == GalleryPath)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene))
                        throw new IOException("Unable to save scene: " + path);
                }
                count += modified;
                Debug.Log("[Fragment Hovl FX] " + scene.name + " halos=" + modified);
            }
        }
        finally
        {
            if (!string.IsNullOrEmpty(originalScene))
                EditorSceneManager.OpenScene(originalScene, OpenSceneMode.Single);
        }
        Debug.Log("[Fragment Hovl FX] Complete: " + count +
                  " original Hovl particle halos. Custom particles removed.");
    }

    /// <summary>
    /// Apply Hovl's particle systems in a shell around the existing first-party
    /// shard. Only the Sparks/Flares render, never the rejected Hovl mesh.
    /// </summary>
    public static bool ApplyToVisual(Transform visual, GameObject sourcePrefab = null)
    {
        if (visual == null)
            return false;
        if (sourcePrefab == null)
            sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HovlCrystalPath);
        if (sourcePrefab == null)
            return false;

        var shell = visual.Find("FacetedCrystal_Shell")?.GetComponent<MeshRenderer>();
        if (shell == null || shell.sharedMaterial == null)
            return false;

        // Match the pickup's original authored RGB, not Hovl's blue default.
        Color tint = OriginalColor(visual);
        foreach (string oldName in new[]
                 { "SoftMysticSparkles", "CrystalStarFlares", "CrystalSoftAura" })
        {
            Transform old = visual.Find(oldName);
            if (old != null)
                UnityEngine.Object.DestroyImmediate(old.gameObject);
        }

        Transform haloTransform = visual.Find(HaloName);
        GameObject halo;
        if (haloTransform == null)
        {
            halo = (GameObject)PrefabUtility.InstantiatePrefab(sourcePrefab, visual.gameObject.scene);
            halo.name = HaloName;
            halo.transform.SetParent(visual, false);
            halo.transform.localPosition = Vector3.zero;
            halo.transform.localRotation = Quaternion.identity;
            halo.transform.localScale = Vector3.one;
        }
        else
            halo = haloTransform.gameObject;

        ParticleSystem originalCrystal = halo.GetComponent<ParticleSystem>();
        if (originalCrystal != null)
        {
            var emission = originalCrystal.emission;
            emission.enabled = false;
            var main = originalCrystal.main;
            main.playOnAwake = false;
        }
        // The Hovl crystal particle's MeshRenderer is the REJECTED model.
        var crystalMesh = halo.GetComponent<ParticleSystemRenderer>();
        if (crystalMesh != null)
            crystalMesh.enabled = false;

        Transform glow = halo.transform.Find("Glow");
        if (glow != null)
            glow.gameObject.SetActive(false); // user wants external particles, not an internal glow

        Configure(halo.transform.Find("Sparks"), tint, false);
        Configure(halo.transform.Find("Flares"), tint, true);

        EditorUtility.SetDirty(halo);
        return true;
    }

    private static Color OriginalColor(Transform visual)
    {
        // The first-party shell uses material suffixes such as 72ADBE.
        // This is read-only material lookup; no vendor tint is persisted.
        string name = visual.Find("FacetedCrystal_Shell")
            .GetComponent<MeshRenderer>().sharedMaterial.name;
        var match = System.Text.RegularExpressions.Regex.Match(name,
            @"Crystal_([0-9A-Fa-f]{6})_");
        if (match.Success && ColorUtility.TryParseHtmlString(
                "#" + match.Groups[1].Value, out Color parsed))
            return parsed;
        return new Color(0.17f, 0.71f, 1f);
    }

    private static void Configure(Transform transform, Color tint, bool flare)
    {
        if (transform == null)
            throw new InvalidOperationException("Hovl's particle prefab is missing " +
                                                (flare ? "Flares" : "Sparks"));
        transform.gameObject.SetActive(true);
        var ps = transform.GetComponent<ParticleSystem>();
        if (ps == null)
            throw new InvalidOperationException("Expected Hovl ParticleSystem on " +
                                                transform.name);
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = flare ? 32 : 96;
        main.startLifetime = flare
            ? new ParticleSystem.MinMaxCurve(0.75f, 1.5f)
            : new ParticleSystem.MinMaxCurve(1.1f, 2.0f);
        main.startSpeed = flare
            ? new ParticleSystem.MinMaxCurve(0.01f, 0.085f)
            : new ParticleSystem.MinMaxCurve(0.04f, 0.18f);
        main.startSize = flare
            ? new ParticleSystem.MinMaxCurve(0.28f, 0.50f)
            : new ParticleSystem.MinMaxCurve(0.14f, 0.29f);
        main.startColor = new Color(tint.r * 1.35f, tint.g * 1.35f,
            tint.b * 1.35f, flare ? 0.98f : 0.93f);

        // A hollow shell keeps initial particles *outside* the shard's
        // 0.5-radius body and 1.0-height tips, unlike Hovl's default 0.01
        // radius which spawned its sparkles essentially inside the mesh.
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = flare ? 1.21f : 1.32f;
        shape.radiusThickness = 0f;
        shape.scale = Vector3.one;
        shape.position = Vector3.zero;
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = flare ? 6f : 21f;
        var color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient fade = new Gradient
        {
            colorKeys = new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            alphaKeys = new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.2f),
                new GradientAlphaKey(0.95f, 0.72f),
                new GradientAlphaKey(0f, 1f)
            }
        };
        color.color = new ParticleSystem.MinMaxGradient(fade);
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
            renderer.enabled = true;
        EditorUtility.SetDirty(ps);
        if (renderer != null)
            EditorUtility.SetDirty(renderer);
    }

    private static void RemoveRejectedFinalFromGallery(Scene gallery)
    {
        foreach (GameObject root in gallery.GetRootGameObjects())
        {
            if (root.name == "FINAL ASSEMBLED  /  HOVL CRYSTAL")
                UnityEngine.Object.DestroyImmediate(root);
            else if (root.name.StartsWith("STAGE ", StringComparison.Ordinal))
            {
                // Center the remaining three showcase crystals.
                float x = root.name.Contains("STAGE 1") ? -6f :
                    root.name.Contains("STAGE 2") ? 0f : 6f;
                root.transform.position = new Vector3(x, 2.2f, 0f);
            }
        }
        var labels = GameObject.Find("CrystalGallery_Labels");
        if (labels != null)
        {
            var final = labels.transform.Find("CrystalLabel_3");
            if (final != null)
                UnityEngine.Object.DestroyImmediate(final.gameObject);
            for (int i = 0; i < 3; ++i)
            {
                Transform label = labels.transform.Find("CrystalLabel_" + i);
                if (label == null)
                    continue;
                var rect = label.GetComponent<RectTransform>();
                float x = i == 0 ? 0.27f : i == 1 ? 0.50f : 0.73f;
                rect.anchorMin = rect.anchorMax = new Vector2(x, 0.12f);
            }
        }
    }
}
