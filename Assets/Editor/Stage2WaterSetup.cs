#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class Stage2WaterSetup
{
    private const string ScenePath = "Assets/Scenes/Stage2Scene/Stage2Scene.unity";
    private const string ShaderName = "SPELLSTRIKE/Stage2/CoastalWater";
    private const string WaterFolder = "Assets/VFX/Stage2Water";
    private const string DesktopMaterialPath = WaterFolder + "/Stage2Water_PC.mat";
    private const string MobileMaterialPath = WaterFolder + "/Stage2Water_Mobile.mat";
    private const string MeshPath = WaterFolder + "/Stage2WaterPlane.asset";
    private const string VolumeProfilePath = "Assets/Scenes/Stage2Scene/Stage2Scene/Stage2_GlobalVolume Profile.asset";
    private const string AutoRunFlag = "RecoveryBackups/RUN_STAGE2_WATER_SETUP.flag";
    private const string ReportPath = "RecoveryBackups/Stage2WaterSetup_20260918.txt";

    [InitializeOnLoadMethod]
    private static void AutoRunIfRequested()
    {
        EditorApplication.delayCall += () =>
        {
            string flagPath = ProjectPath(AutoRunFlag);
            if (!File.Exists(flagPath))
                return;

            File.Delete(flagPath);
            Debug.Log("[Stage2WaterSetup] Auto-run flag detected.");
            Run();
        };
    }

    [MenuItem("SPELLSTRIKE/Stage 2/Build Coastal Water Pass")]
    public static void Run()
    {
        AssetDatabase.Refresh();

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        int colliderCountBefore = CountMeshColliders(scene);
        Shader shader = Shader.Find(ShaderName);
        if (shader == null)
            throw new InvalidOperationException($"Shader '{ShaderName}' could not be found. Import the Stage 2 water shader first.");

        Material desktop = CreateOrUpdateMaterial(DesktopMaterialPath, shader, mobile: false);
        Material mobile = CreateOrUpdateMaterial(MobileMaterialPath, shader, mobile: true);
        Mesh waterMesh = CreateOrUpdateWaterMesh();

        GameObject map = FindSceneObject(scene, "STAGE2MAP");
        if (map == null)
            throw new InvalidOperationException("STAGE2MAP was not found in Stage2Scene.");

        Renderer[] mapRenderers = map.GetComponentsInChildren<Renderer>(true)
            .Where(r => !(r is ParticleSystemRenderer))
            .ToArray();
        if (mapRenderers.Length == 0)
            throw new InvalidOperationException("STAGE2MAP contains no renderers to size the water surface against.");

        Bounds mapBounds = mapRenderers[0].bounds;
        for (int i = 1; i < mapRenderers.Length; i++)
            mapBounds.Encapsulate(mapRenderers[i].bounds);

        GameObject water = FindSceneObject(scene, "Stage2_WaterSurface");
        float width = Mathf.Clamp(mapBounds.size.x + 500f, 800f, 3200f);
        float depth = Mathf.Clamp(mapBounds.size.z + 500f, 1000f, 3400f);
        // Preserve the visually approved/current waterline on reruns. The Stage 2
        // map is scaled 9.7x and offset vertically, so world Y=0 is not its coast.
        float waterLevelY = water != null ? water.transform.position.y : -8.6f;

        if (water == null)
        {
            water = new GameObject("Stage2_WaterSurface");
            Undo.RegisterCreatedObjectUndo(water, "Create Stage 2 Water Surface");
        }

        Transform environmentParent = FindSceneObject(scene, "_Environment_PLACEHOLDER")?.transform;
        if (environmentParent != null)
            water.transform.SetParent(environmentParent, true);

        water.transform.position = new Vector3(mapBounds.center.x, waterLevelY, mapBounds.center.z);
        water.transform.rotation = Quaternion.identity;
        water.transform.localScale = new Vector3(width, 1f, depth);

        MeshFilter meshFilter = GetOrAddComponent<MeshFilter>(water);
        MeshRenderer meshRenderer = GetOrAddComponent<MeshRenderer>(water);
        Stage2WaterMaterialSelector selector = GetOrAddComponent<Stage2WaterMaterialSelector>(water);
        meshFilter.sharedMesh = waterMesh;
        meshRenderer.sharedMaterial = desktop;
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        selector.Configure(desktop, mobile);

        foreach (Collider collider in water.GetComponents<Collider>())
            UnityEngine.Object.DestroyImmediate(collider);

        TuneVolumeProfile();

        int colliderCountAfter = CountMeshColliders(scene);
        if (colliderCountAfter != colliderCountBefore)
            throw new InvalidOperationException($"MeshCollider invariant failed: before={colliderCountBefore}, after={colliderCountAfter}. Scene was not saved.");

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new IOException("Stage2Scene could not be saved after the water pass.");

        EditorUtility.SetDirty(desktop);
        EditorUtility.SetDirty(mobile);
        AssetDatabase.SaveAssets();

        string report =
            "STAGE 2 COASTAL WATER PASS\n" +
            $"Scene={ScenePath}\n" +
            $"MapBoundsCenter={Fmt(mapBounds.center)}\n" +
            $"MapBoundsSize={Fmt(mapBounds.size)}\n" +
            $"WaterPosition={Fmt(water.transform.position)}\n" +
            $"WaterSize=({width:F3},1.000,{depth:F3})\n" +
            $"WaterLevelY={waterLevelY:F3}\n" +
            $"MeshCollidersBefore={colliderCountBefore}\n" +
            $"MeshCollidersAfter={colliderCountAfter}\n" +
            $"DesktopMaterial={DesktopMaterialPath}\n" +
            $"MobileMaterial={MobileMaterialPath}\n" +
            $"VolumeProfile={VolumeProfilePath}\n";

        File.WriteAllText(ProjectPath(ReportPath), report);
        Debug.Log("[Stage2WaterSetup] PASS\n" + report);
        Selection.activeGameObject = water;
        SceneView.lastActiveSceneView?.FrameSelected();
    }

    private static Material CreateOrUpdateMaterial(string path, Shader shader, bool mobile)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.shader = shader;
        }

        material.SetColor("_ShallowColor", mobile ? new Color(0.40f, 0.76f, 0.72f, 1f) : new Color(0.38f, 0.78f, 0.74f, 1f));
        material.SetColor("_DeepColor", mobile ? new Color(0.10f, 0.28f, 0.46f, 1f) : new Color(0.08f, 0.27f, 0.48f, 1f));
        material.SetColor("_FoamTint", mobile ? new Color(0.80f, 0.93f, 0.95f, 1f) : new Color(0.82f, 0.95f, 0.96f, 1f));
        material.SetFloat("_ShallowAlpha", mobile ? 0.30f : 0.26f);
        material.SetFloat("_DeepAlpha", mobile ? 0.65f : 0.72f);
        material.SetFloat("_DepthDistance", mobile ? 8.0f : 40.0f);
        material.SetFloat("_DepthCurve", mobile ? 1.0f : 0.55f);
        material.SetFloat("_WaveScale", mobile ? 0.040f : 0.046f);
        material.SetFloat("_WaveStrength", mobile ? 0.14f : 0.22f);
        material.SetFloat("_WaveSpeed", mobile ? 0.42f : 0.55f);
        material.SetFloat("_SpecularStrength", mobile ? 0.62f : 1.05f);
        material.SetFloat("_SpecularPower", mobile ? 42f : 72f);
        material.SetFloat("_FresnelPower", mobile ? 2.5f : 3.2f);
        material.SetFloat("_RefractionStrength", mobile ? 0f : 0.008f);

        if (mobile)
        {
            material.DisableKeyword("_STAGE2_DEPTH");
            material.DisableKeyword("_STAGE2_REFRACTION");
            material.SetFloat("_UseDepth", 0f);
            material.SetFloat("_UseRefraction", 0f);
        }
        else
        {
            material.EnableKeyword("_STAGE2_DEPTH");
            material.EnableKeyword("_STAGE2_REFRACTION");
            material.SetFloat("_UseDepth", 1f);
            material.SetFloat("_UseRefraction", 1f);
        }

        material.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Mesh CreateOrUpdateWaterMesh()
    {
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
        if (mesh == null)
        {
            mesh = new Mesh { name = "Stage2WaterPlane" };
            AssetDatabase.CreateAsset(mesh, MeshPath);
        }

        mesh.Clear();
        mesh.vertices = new[]
        {
            new Vector3(-0.5f, 0f, -0.5f),
            new Vector3(-0.5f, 0f, 0.5f),
            new Vector3(0.5f, 0f, 0.5f),
            new Vector3(0.5f, 0f, -0.5f)
        };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0f)
        };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        EditorUtility.SetDirty(mesh);
        return mesh;
    }

    private static void TuneVolumeProfile()
    {
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
        if (profile == null)
            throw new InvalidOperationException("Stage2 Global Volume profile was not found.");

        // Runtime VolumeProfile.Add<T>() only creates the component in memory.
        // When editing an asset from script we must persist newly-added components
        // as sub-assets or the profile serializes them as {fileID: 0}.
        profile.components.RemoveAll(component => component == null);

        Bloom bloom = GetOrAddVolumeComponent<Bloom>(profile);
        bloom.active = true;
        bloom.threshold.Override(0.92f);
        bloom.intensity.Override(0.30f);
        bloom.scatter.Override(0.52f);
        bloom.highQualityFiltering.Override(false);

        ColorAdjustments color = GetOrAddVolumeComponent<ColorAdjustments>(profile);
        color.active = true;
        color.postExposure.Override(0.03f);
        color.contrast.Override(7f);
        color.saturation.Override(4f);
        color.colorFilter.Override(new Color(0.96f, 1f, 1f, 1f));

        WhiteBalance whiteBalance = GetOrAddVolumeComponent<WhiteBalance>(profile);
        whiteBalance.active = true;
        whiteBalance.temperature.Override(-3f);
        whiteBalance.tint.Override(1f);

        EditorUtility.SetDirty(bloom);
        EditorUtility.SetDirty(color);
        EditorUtility.SetDirty(whiteBalance);
        EditorUtility.SetDirty(profile);
        profile.Reset();
    }

    private static T GetOrAddVolumeComponent<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (profile.TryGet(out T component))
            return component;

        component = profile.Add<T>(true);
        AssetDatabase.AddObjectToAsset(component, profile);
        EditorUtility.SetDirty(component);
        EditorUtility.SetDirty(profile);
        return component;
    }

    private static T GetOrAddComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static GameObject FindSceneObject(Scene scene, string exactName)
    {
        return Resources.FindObjectsOfTypeAll<GameObject>()
            .FirstOrDefault(go => go.scene.IsValid() && go.scene == scene && go.name == exactName);
    }

    private static int CountMeshColliders(Scene scene)
    {
        return Resources.FindObjectsOfTypeAll<MeshCollider>()
            .Count(c => c.gameObject.scene.IsValid() && c.gameObject.scene == scene);
    }

    private static string ProjectPath(string relativePath)
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));
    }

    private static string Fmt(Vector3 v) => $"({v.x:F3},{v.y:F3},{v.z:F3})";
}
#endif
