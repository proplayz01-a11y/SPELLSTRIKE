using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds an explicitly temporary, rig-independent Spirit Remnant proxy.
/// This tool creates ONLY new prefab and material assets; it never edits
/// SampleScene or replaces the eventual authored spirit character model.
/// </summary>
public static class SpiritRemnantPrototypeBuilder
{
    private const string PrefabPath = "Assets/Prefabs/Story/SpiritRemnant_Prototype.prefab";
    private const string MaterialsDir = "Assets/Materials/Story";

    [MenuItem("SPELLSTRIKE/Story/Spirit Remnant/Create Independent Prototype Prefab")]
    public static void CreatePrototype()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Build only in Edit Mode.");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            throw new InvalidOperationException("Spirit Remnant prefab already exists; refusing to overwrite.");

        EnsureFolder("Assets/Prefabs/Story");
        EnsureFolder(MaterialsDir);

        Material bodyMaterial = CreateMaterial("SpiritRemnant_Body", new Color(0.31f, 0.79f, 0.98f, 0.55f));
        Material faceMaterial = CreateMaterial("SpiritRemnant_Head", new Color(0.72f, 0.92f, 1f, 0.78f));
        Material hazeMaterial = CreateMaterial("SpiritRemnant_Haze", new Color(0.30f, 0.72f, 1f, 0.22f));

        Scene originalScene = SceneManager.GetActiveScene();
        Scene sandbox = EditorSceneManager.NewPreviewScene();
        GameObject root = null;
        try
        {
            root = new GameObject("SpiritRemnant_Prototype");
            SceneManager.MoveGameObjectToScene(root, sandbox);
            var visual = new GameObject("AnimatedSpiritVisual");
            visual.transform.SetParent(root.transform, false);

            Primitive(visual.transform, "LowerWisp", PrimitiveType.Cylinder, new Vector3(0f, 0.53f, 0f),
                new Vector3(0.59f, 0.53f, 0.43f), bodyMaterial);
            Primitive(visual.transform, "Torso", PrimitiveType.Capsule, new Vector3(0f, 1.38f, 0f),
                new Vector3(0.58f, 0.78f, 0.40f), bodyMaterial);
            Transform head = Primitive(visual.transform, "SpeakingHead", PrimitiveType.Sphere,
                new Vector3(0f, 2.36f, 0f), new Vector3(0.62f, 0.71f, 0.55f), faceMaterial);
            Transform leftArm = Primitive(visual.transform, "LeftSleeve", PrimitiveType.Capsule,
                new Vector3(-0.61f, 1.45f, 0f), new Vector3(0.22f, 0.61f, 0.23f), bodyMaterial);
            leftArm.localRotation = Quaternion.Euler(0f, 0f, -24f);
            Transform rightArm = Primitive(visual.transform, "RightSleeve", PrimitiveType.Capsule,
                new Vector3(0.61f, 1.45f, 0f), new Vector3(0.22f, 0.61f, 0.23f), bodyMaterial);
            rightArm.localRotation = Quaternion.Euler(0f, 0f, 24f);
            Primitive(visual.transform, "SoftAura", PrimitiveType.Sphere, new Vector3(0f, 1.5f, 0f),
                new Vector3(1.4f, 2.15f, 1.20f), hazeMaterial);

            GameObject lightObject = new GameObject("GentleSpectralLight", typeof(Light));
            lightObject.transform.SetParent(visual.transform, false);
            lightObject.transform.localPosition = new Vector3(0f, 1.55f, 0.1f);
            Light spiritLight = lightObject.GetComponent<Light>();
            spiritLight.type = LightType.Point;
            spiritLight.color = new Color(0.4f, 0.75f, 1f);
            spiritLight.intensity = 0.65f;
            spiritLight.range = 5f;
            spiritLight.shadows = LightShadows.None;

            var motion = root.AddComponent<SpiritRemnantAmbientMotion>();
            motion.Configure(visual.transform, head, spiritLight);
            root.AddComponent<SpiritRemnantEncounter>();

            GameObject asset = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            if (asset == null)
                throw new InvalidOperationException("Unity did not save Spirit Remnant prefab.");

            Debug.Log("[SpiritRemnant] Saved independent prefab " + PrefabPath +
                      ". Stage 1 scene remains untouched. This is a temporary visual proxy.");
        }
        finally
        {
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
            if (sandbox.IsValid()) EditorSceneManager.ClosePreviewScene(sandbox);
            if (originalScene.IsValid() && originalScene.isLoaded)
                SceneManager.SetActiveScene(originalScene);
        }
    }

    private static Transform Primitive(Transform parent, string name, PrimitiveType type,
        Vector3 position, Vector3 scale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;

        Collider collider = go.GetComponent<Collider>();
        if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return go.transform;
    }

    private static Material CreateMaterial(string assetName, Color color)
    {
        string path = MaterialsDir + "/" + assetName + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            throw new InvalidOperationException("URP Unlit shader unavailable in this project.");

        var mat = new Material(shader) { name = assetName };
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0f);
        mat.SetColor("_BaseColor", color);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)RenderQueue.Transparent;
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static void EnsureFolder(string path)
    {
        string[] segments = path.Split('/');
        string current = segments[0];
        for (int i = 1; i < segments.Length; i++)
        {
            string next = current + "/" + segments[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, segments[i]);
            current = next;
        }
    }
}
