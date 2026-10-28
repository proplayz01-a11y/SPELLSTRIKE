#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Stage1OptimizedMapIntegrationMenu
{
    private const string OptimizedMapPath = "Assets/Maps/Optimized/level1_OPTIMIZED.glb";
    private const string ExistingMapPath = "Assets/Maps/level1_REVAMPED.glb";
    private const string TestScenePath = "Assets/Scenes/OptimizationTests/Stage1_OptimizedMap_Test.unity";
    private const string OptimizedRootName = "level1_OPTIMIZED_TEST";
    private const string LodContainerName = "__OPTIMIZED_MAP_LOD_GROUPS";

    // These are intentionally only first-pass visual-test thresholds, not final
    // production tuning. They must be tuned in Scene/Game view and profiled on
    // the Android target before the optimized map is promoted into live Stage 1.
    private static readonly float[] ProvisionalTransitionHeights =
    {
        0.60f,
        0.35f,
        0.18f,
        0.08f,
        0.02f,
    };

    private static readonly Regex LodNamePattern = new Regex(
        @"^(?<base>.+)_LOD(?<lod>[0-4])(?<suffix>\.\d+)?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [MenuItem("SPELLSTRIKE/Optimization/1. Audit Optimized Stage 1 GLB")]
    public static void AuditOptimizedStage1Glb()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(OptimizedMapPath);
        if (prefab == null)
        {
            Debug.LogError($"[Stage1 Map Optimization] Missing imported GLB at {OptimizedMapPath}.");
            return;
        }

        UnityEngine.Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(OptimizedMapPath);
        Mesh[] meshes = subAssets.OfType<Mesh>().Distinct().ToArray();
        Material[] materials = subAssets.OfType<Material>().Distinct().ToArray();
        Texture2D[] textures = subAssets.OfType<Texture2D>().Distinct().ToArray();
        Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
        LODGroup[] existingLodGroups = prefab.GetComponentsInChildren<LODGroup>(true);

        long uniqueMeshTriangles = 0;
        foreach (Mesh mesh in meshes)
        {
            for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                uniqueMeshTriangles += (long)mesh.GetIndexCount(subMesh) / 3L;
        }

        Dictionary<string, LodChain> chains = CollectLodChains(prefab);
        LodChain[] complete = chains.Values.Where(chain => chain.IsComplete).OrderBy(chain => chain.Key).ToArray();
        LodChain[] incomplete = chains.Values.Where(chain => !chain.IsComplete).OrderBy(chain => chain.Key).ToArray();

        var textureDimensions = textures
            .GroupBy(texture => $"{texture.width}x{texture.height}")
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Select(group => $"{group.Key}={group.Count()}")
            .ToArray();

        var report = new StringBuilder();
        report.AppendLine("[Stage1 Map Optimization] OPTIMIZED GLB AUDIT");
        report.AppendLine($"Asset: {OptimizedMapPath}");
        report.AppendLine($"Prefab renderers: {renderers.Length}");
        report.AppendLine($"Unique mesh subassets: {meshes.Length}");
        report.AppendLine($"Unique mesh triangles stored in imported asset: {uniqueMeshTriangles:N0}");
        report.AppendLine($"Material subassets: {materials.Length}");
        report.AppendLine($"Texture subassets: {textures.Length}");
        report.AppendLine($"Texture dimensions: {(textureDimensions.Length > 0 ? string.Join(", ", textureDimensions) : "none exposed as Texture2D subassets")}");
        report.AppendLine($"Existing Unity LODGroups in imported prefab: {existingLodGroups.Length}");
        report.AppendLine($"Detected complete LOD0-LOD4 chains: {complete.Length}");
        report.AppendLine($"Detected incomplete LOD chains: {incomplete.Length}");

        foreach (LodChain chain in incomplete)
            report.AppendLine($"  INCOMPLETE {chain.Key}: levels {chain.DescribeLevels()}");

        report.AppendLine(
            "Note: stored mesh triangles include every LOD mesh. Runtime geometry depends on the active LOD renderer, not this stored total.");

        Debug.Log(report.ToString(), prefab);
    }

    [MenuItem("SPELLSTRIKE/Optimization/2. Prepare Optimized Stage 1 Test Scene")]
    public static void PrepareOptimizedStage1TestScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[Stage1 Map Optimization] Exit Play Mode before preparing the optimization test scene.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid()
            || !scene.isLoaded
            || !string.Equals(scene.path, TestScenePath, StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogError(
                $"[Stage1 Map Optimization] Open the dedicated test scene first: {TestScenePath}. " +
                "The command intentionally refuses to modify another scene.");
            return;
        }

        if (FindSceneObject(scene, OptimizedRootName) != null)
        {
            Debug.LogError(
                $"[Stage1 Map Optimization] {OptimizedRootName} already exists. " +
                "Undo/remove the previous test setup before running this command again.");
            return;
        }

        GameObject optimizedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OptimizedMapPath);
        if (optimizedPrefab == null)
        {
            Debug.LogError($"[Stage1 Map Optimization] Missing imported GLB at {OptimizedMapPath}.");
            return;
        }

        GameObject existingMapRoot = FindPrefabInstanceRoot(scene, ExistingMapPath);
        if (existingMapRoot == null)
        {
            Debug.LogError(
                $"[Stage1 Map Optimization] Could not find the current {ExistingMapPath} prefab instance in the test scene. " +
                "No changes were made.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Prepare Optimized Stage 1 Map Test");

        try
        {
            GameObject optimizedRoot = (GameObject)PrefabUtility.InstantiatePrefab(optimizedPrefab, scene);
            Undo.RegisterCreatedObjectUndo(optimizedRoot, "Instantiate optimized Stage 1 map");
            optimizedRoot.name = OptimizedRootName;

            Undo.RecordObject(optimizedRoot.transform, "Match optimized map transform");
            optimizedRoot.transform.SetPositionAndRotation(
                existingMapRoot.transform.position,
                existingMapRoot.transform.rotation);
            optimizedRoot.transform.localScale = existingMapRoot.transform.lossyScale;

            int hiddenOldMapRenderers = DisableSourceMapRenderers(existingMapRoot);

            Dictionary<string, LodChain> chains = CollectLodChains(optimizedRoot);
            LodChain[] completeChains = chains.Values
                .Where(chain => chain.IsComplete)
                .OrderBy(chain => chain.Key)
                .ToArray();
            LodChain[] incompleteChains = chains.Values
                .Where(chain => !chain.IsComplete)
                .OrderBy(chain => chain.Key)
                .ToArray();

            GameObject lodContainer = new GameObject(LodContainerName);
            Undo.RegisterCreatedObjectUndo(lodContainer, "Create optimized map LOD container");
            Undo.SetTransformParent(lodContainer.transform, optimizedRoot.transform, "Parent LOD container");
            lodContainer.transform.localPosition = Vector3.zero;
            lodContainer.transform.localRotation = Quaternion.identity;
            lodContainer.transform.localScale = Vector3.one;

            int configuredGroups = 0;
            foreach (LodChain chain in completeChains)
            {
                if (CreateLodGroup(lodContainer.transform, chain))
                    configuredGroups++;
            }

            int disabledIncompleteRenderers = 0;
            foreach (LodChain chain in incompleteChains)
            {
                foreach (List<Renderer> levelRenderers in chain.RenderersByLevel.Values)
                {
                    foreach (Renderer renderer in levelRenderers)
                    {
                        if (renderer == null || !renderer.enabled)
                            continue;

                        Undo.RecordObject(renderer, "Disable incomplete optimized-map LOD renderer");
                        renderer.enabled = false;
                        EditorUtility.SetDirty(renderer);
                        disabledIncompleteRenderers++;
                    }
                }
            }

            EditorUtility.SetDirty(optimizedRoot);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = optimizedRoot;
            EditorGUIUtility.PingObject(optimizedRoot);
            SceneView.RepaintAll();

            var report = new StringBuilder();
            report.AppendLine("[Stage1 Map Optimization] TEST SCENE PREPARED (NOT AUTO-SAVED)");
            report.AppendLine($"Scene: {TestScenePath}");
            report.AppendLine($"Optimized root: {OptimizedRootName}");
            report.AppendLine($"Old source-map renderers hidden: {hiddenOldMapRenderers}");
            report.AppendLine($"Complete LOD0-LOD4 groups configured: {configuredGroups}");
            report.AppendLine($"Incomplete-chain renderers disabled: {disabledIncompleteRenderers}");
            report.AppendLine(
                "LOD thresholds are provisional visual-test values only. Tune them in the Editor and profile them on Android before production use.");
            report.AppendLine(
                "The old map prefab, its colliders, and scene-added gameplay objects remain in place for rollback/reference. Inspect the scene before saving.");

            foreach (LodChain chain in incompleteChains)
                report.AppendLine($"  DISABLED INCOMPLETE {chain.Key}: levels {chain.DescribeLevels()}");

            Debug.Log(report.ToString(), optimizedRoot);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Undo.RevertAllDownToGroup(undoGroup);
        }
        finally
        {
            Undo.CollapseUndoOperations(undoGroup);
        }
    }

    private static int DisableSourceMapRenderers(GameObject existingMapRoot)
    {
        int disabled = 0;
        Renderer[] renderers = existingMapRoot.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled)
                continue;

            GameObject sourceObject = PrefabUtility.GetCorrespondingObjectFromSource(renderer.gameObject);
            if (sourceObject == null)
                continue;

            string sourcePath = AssetDatabase.GetAssetPath(sourceObject);
            if (!string.Equals(sourcePath, ExistingMapPath, StringComparison.OrdinalIgnoreCase))
                continue;

            Undo.RecordObject(renderer, "Hide old Stage 1 source renderer");
            renderer.enabled = false;
            EditorUtility.SetDirty(renderer);
            disabled++;
        }

        return disabled;
    }

    private static bool CreateLodGroup(Transform container, LodChain chain)
    {
        if (!chain.IsComplete)
            return false;

        Renderer primary = chain.RenderersByLevel[0].FirstOrDefault(renderer => renderer != null);
        if (primary == null)
        {
            Debug.LogError($"[Stage1 Map Optimization] {chain.Key} has no usable LOD0 renderer.");
            return false;
        }

        GameObject wrapper = new GameObject($"LODGroup_{SanitizeName(chain.Key)}");
        Undo.RegisterCreatedObjectUndo(wrapper, "Create optimized-map LODGroup wrapper");
        Undo.SetTransformParent(wrapper.transform, container, "Parent optimized-map LODGroup wrapper");

        wrapper.transform.position = primary.bounds.center;
        wrapper.transform.rotation = container.rotation;
        wrapper.transform.localScale = Vector3.one;

        LODGroup group = Undo.AddComponent<LODGroup>(wrapper);
        group.fadeMode = LODFadeMode.None;
        group.animateCrossFading = false;

        var lods = new LOD[5];
        for (int level = 0; level < 5; level++)
        {
            Renderer[] renderers = chain.RenderersByLevel[level]
                .Where(renderer => renderer != null)
                .Distinct()
                .ToArray();
            lods[level] = new LOD(ProvisionalTransitionHeights[level], renderers);
        }

        group.SetLODs(lods);
        group.RecalculateBounds();
        EditorUtility.SetDirty(group);
        return true;
    }

    private static Dictionary<string, LodChain> CollectLodChains(GameObject root)
    {
        var chains = new Dictionary<string, LodChain>(StringComparer.OrdinalIgnoreCase);
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);

        foreach (Transform transform in transforms)
        {
            Match match = LodNamePattern.Match(transform.name);
            if (!match.Success)
                continue;

            int level = int.Parse(match.Groups["lod"].Value);
            string baseName = match.Groups["base"].Value;
            string suffix = match.Groups["suffix"].Success ? match.Groups["suffix"].Value : string.Empty;
            string key = baseName + suffix;

            Renderer[] renderers = transform.GetComponents<Renderer>();
            if (renderers.Length == 0)
                continue;

            if (!chains.TryGetValue(key, out LodChain chain))
            {
                chain = new LodChain(key);
                chains.Add(key, chain);
            }

            chain.Add(level, renderers);
        }

        return chains;
    }

    private static GameObject FindPrefabInstanceRoot(Scene scene, string prefabAssetPath)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            GameObject instanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(root);
            if (instanceRoot == null)
                continue;

            string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(instanceRoot);
            if (string.Equals(path, prefabAssetPath, StringComparison.OrdinalIgnoreCase))
                return instanceRoot;
        }

        return null;
    }

    private static GameObject FindSceneObject(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform transform in transforms)
            {
                if (string.Equals(transform.name, objectName, StringComparison.Ordinal))
                    return transform.gameObject;
            }
        }

        return null;
    }

    private static string SanitizeName(string value)
    {
        return value.Replace('.', '_').Replace(' ', '_');
    }

    private sealed class LodChain
    {
        public LodChain(string key)
        {
            Key = key;
            RenderersByLevel = new Dictionary<int, List<Renderer>>();
        }

        public string Key { get; }

        public Dictionary<int, List<Renderer>> RenderersByLevel { get; }

        public bool IsComplete => Enumerable.Range(0, 5).All(level =>
            RenderersByLevel.TryGetValue(level, out List<Renderer> renderers)
            && renderers.Any(renderer => renderer != null));

        public void Add(int level, IEnumerable<Renderer> renderers)
        {
            if (!RenderersByLevel.TryGetValue(level, out List<Renderer> levelRenderers))
            {
                levelRenderers = new List<Renderer>();
                RenderersByLevel.Add(level, levelRenderers);
            }

            levelRenderers.AddRange(renderers.Where(renderer => renderer != null));
        }

        public string DescribeLevels()
        {
            return string.Join(",", RenderersByLevel.Keys.OrderBy(level => level).Select(level => $"LOD{level}"));
        }
    }
}
#endif
