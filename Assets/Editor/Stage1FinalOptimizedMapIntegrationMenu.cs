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

public static class Stage1FinalOptimizedMapIntegrationMenu
{
    private const string FinalMapPath = "Assets/Maps/Optimized/level1_STAGE1_OPTIMIZED.glb";
    private const string ExistingMapPath = "Assets/Maps/level1_REVAMPED.glb";
    private const string TestScenePath = "Assets/Scenes/OptimizationTests/Stage1_OptimizedMap_Test.unity";
    private const string FinalRootName = "level1_STAGE1_OPTIMIZED_TEST";
    private const string LodContainerName = "__FINAL_OPTIMIZED_LOD_GROUPS";
    private const string WallContainerName = "__STONE_WALL_LOD_GROUPS";

    private static readonly float[] StandardTransitionHeights =
    {
        0.60f,
        0.35f,
        0.18f,
        0.08f,
        0.02f,
    };

    private static readonly float[] WallTransitionHeights =
    {
        0.35f,
        0.15f,
        0.001f,
    };

    private static readonly Regex StandardLodPattern = new Regex(
        @"^(?<base>.+)_LOD(?<lod>[0-4])(?<suffix>\.\d+)?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex WallNamePattern = new Regex(
        @"^model_LOD(?<lod>[1-3])\.(?<suffix>\d{3})$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [MenuItem("SPELLSTRIKE/Optimization/4. Audit FINAL Stage 1 GLB")]
    public static void AuditFinalStage1Glb()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FinalMapPath);
        if (prefab == null)
        {
            Debug.LogError($"[Stage1 Final Optimization] Missing imported GLB at {FinalMapPath}.");
            return;
        }

        UnityEngine.Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(FinalMapPath);
        Mesh[] meshes = subAssets.OfType<Mesh>().Distinct().ToArray();
        Material[] materials = subAssets.OfType<Material>().Distinct().ToArray();
        Texture2D[] textures = subAssets.OfType<Texture2D>().Distinct().ToArray();

        long triangles = 0;
        foreach (Mesh mesh in meshes)
        {
            for (int i = 0; i < mesh.subMeshCount; i++)
                triangles += (long)mesh.GetIndexCount(i) / 3L;
        }

        var wallSets = CollectWallSets(prefab);
        int completeWallSets = wallSets.Values.Count(set => set.IsComplete);
        int incompleteWallSets = wallSets.Count - completeWallSets;

        var report = new StringBuilder();
        report.AppendLine("[Stage1 Final Optimization] FINAL GLB AUDIT");
        report.AppendLine($"Asset: {FinalMapPath}");
        report.AppendLine($"Unique mesh subassets: {meshes.Length}");
        report.AppendLine($"Stored unique mesh triangles: {triangles:N0}");
        report.AppendLine($"Material subassets: {materials.Length}");
        report.AppendLine($"Texture subassets: {textures.Length}");
        report.AppendLine($"Detected complete wall LOD1-LOD3 sets: {completeWallSets}");
        report.AppendLine($"Detected incomplete wall sets: {incompleteWallSets}");

        foreach (WallSet set in wallSets.Values.Where(set => !set.IsComplete).OrderBy(set => set.Suffix))
            report.AppendLine($"  INCOMPLETE WALL {set.Suffix}: {set.DescribeLevels()}");

        Debug.Log(report.ToString(), prefab);
    }

    [MenuItem("SPELLSTRIKE/Optimization/5. Prepare FINAL Stage 1 Test Scene")]
    public static void PrepareFinalStage1TestScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[Stage1 Final Optimization] Exit Play Mode before preparing the final test scene.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid()
            || !scene.isLoaded
            || !string.Equals(scene.path, TestScenePath, StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogError(
                $"[Stage1 Final Optimization] Open the dedicated test scene first: {TestScenePath}. " +
                "No changes were made.");
            return;
        }

        if (FindSceneObject(scene, FinalRootName) != null)
        {
            Debug.LogError(
                $"[Stage1 Final Optimization] {FinalRootName} already exists. " +
                "Undo/remove the previous final-map setup before running again.");
            return;
        }

        GameObject optimizedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FinalMapPath);
        if (optimizedPrefab == null)
        {
            Debug.LogError($"[Stage1 Final Optimization] Missing imported GLB at {FinalMapPath}.");
            return;
        }

        GameObject existingMapRoot = FindPrefabInstanceRoot(scene, ExistingMapPath);
        if (existingMapRoot == null)
        {
            Debug.LogError(
                $"[Stage1 Final Optimization] Could not find the current {ExistingMapPath} prefab instance in the test scene. " +
                "No changes were made.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Prepare FINAL Optimized Stage 1 Test");

        try
        {
            GameObject optimizedRoot = (GameObject)PrefabUtility.InstantiatePrefab(optimizedPrefab, scene);
            Undo.RegisterCreatedObjectUndo(optimizedRoot, "Instantiate final optimized Stage 1 map");
            optimizedRoot.name = FinalRootName;

            Undo.RecordObject(optimizedRoot.transform, "Match final optimized map transform");
            optimizedRoot.transform.SetPositionAndRotation(
                existingMapRoot.transform.position,
                existingMapRoot.transform.rotation);
            optimizedRoot.transform.localScale = existingMapRoot.transform.lossyScale;

            int hiddenOldMapRenderers = DisableSourceMapRenderers(existingMapRoot);

            GameObject lodContainer = new GameObject(LodContainerName);
            Undo.RegisterCreatedObjectUndo(lodContainer, "Create final optimized LOD container");
            Undo.SetTransformParent(lodContainer.transform, optimizedRoot.transform, "Parent final optimized LOD container");
            lodContainer.transform.localPosition = Vector3.zero;
            lodContainer.transform.localRotation = Quaternion.identity;
            lodContainer.transform.localScale = Vector3.one;

            Dictionary<string, LodChain> chains = CollectStandardLodChains(optimizedRoot);
            int configuredStandardGroups = 0;
            foreach (LodChain chain in chains.Values.Where(chain => chain.IsComplete).OrderBy(chain => chain.Key))
            {
                if (IsWallChain(chain.Key))
                    continue;

                if (CreateStandardLodGroup(lodContainer.transform, chain))
                    configuredStandardGroups++;
            }

            GameObject wallContainer = new GameObject(WallContainerName);
            Undo.RegisterCreatedObjectUndo(wallContainer, "Create stone-wall LOD container");
            Undo.SetTransformParent(wallContainer.transform, optimizedRoot.transform, "Parent stone-wall LOD container");
            wallContainer.transform.localPosition = Vector3.zero;
            wallContainer.transform.localRotation = Quaternion.identity;
            wallContainer.transform.localScale = Vector3.one;

            Dictionary<string, WallSet> wallSets = CollectWallSets(optimizedRoot);
            int configuredWallGroups = 0;
            foreach (WallSet set in wallSets.Values.OrderBy(set => set.Suffix))
            {
                if (!set.IsComplete)
                    continue;

                if (CreateWallLodGroup(wallContainer.transform, set))
                    configuredWallGroups++;
            }

            int disabledIncompleteRenderers = 0;
            foreach (LodChain chain in chains.Values.Where(chain => !chain.IsComplete))
            {
                if (IsWallChain(chain.Key))
                    continue;

                foreach (Renderer renderer in chain.AllRenderers())
                {
                    if (renderer == null || !renderer.enabled)
                        continue;

                    Undo.RecordObject(renderer, "Disable incomplete final-map LOD renderer");
                    renderer.enabled = false;
                    EditorUtility.SetDirty(renderer);
                    disabledIncompleteRenderers++;
                }
            }

            int incompleteWallSets = wallSets.Values.Count(set => !set.IsComplete);

            EditorUtility.SetDirty(optimizedRoot);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = optimizedRoot;
            EditorGUIUtility.PingObject(optimizedRoot);
            SceneView.RepaintAll();

            Debug.Log(
                "[Stage1 Final Optimization] FINAL TEST SCENE PREPARED (NOT AUTO-SAVED)\n" +
                $"Scene: {TestScenePath}\n" +
                $"Final map: {FinalMapPath}\n" +
                $"Old source-map renderers hidden: {hiddenOldMapRenderers}\n" +
                $"Standard LOD0-LOD4 groups configured: {configuredStandardGroups}\n" +
                $"Stone-wall LOD1-LOD3 groups configured: {configuredWallGroups}\n" +
                $"Incomplete wall sets: {incompleteWallSets}\n" +
                $"Other incomplete-chain renderers disabled: {disabledIncompleteRenderers}\n" +
                "Wall policy: Rodin LOD1 -> LOD2 -> LOD3 -> cull below 5%.\n" +
                "The scene was marked dirty but intentionally not auto-saved. Inspect visuals before saving.",
                optimizedRoot);
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

    [MenuItem("SPELLSTRIKE/Optimization/6. Validate FINAL Stage 1 Test Scene")]
    public static void ValidateFinalStage1TestScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid()
            || !scene.isLoaded
            || !string.Equals(scene.path, TestScenePath, StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogError(
                $"[Stage1 Final Optimization] Open the dedicated test scene first: {TestScenePath}.");
            return;
        }

        GameObject optimizedRoot = FindSceneObject(scene, FinalRootName);
        GameObject existingMapRoot = FindPrefabInstanceRoot(scene, ExistingMapPath);
        if (optimizedRoot == null || existingMapRoot == null)
        {
            Debug.LogError(
                "[Stage1 Final Optimization] Validation failed because the final optimized root or old source-map root is missing.");
            return;
        }

        LODGroup[] allGroups = optimizedRoot.GetComponentsInChildren<LODGroup>(true);
        int standardGroups = allGroups.Count(group =>
            group != null && group.gameObject.name.StartsWith("LODGroup_", StringComparison.Ordinal));
        int wallGroups = allGroups.Count(group =>
            group != null && group.gameObject.name.StartsWith("StoneWall_LODGroup_", StringComparison.Ordinal));

        int enabledOldSourceRenderers = 0;
        foreach (Renderer renderer in existingMapRoot.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null || !renderer.enabled)
                continue;

            GameObject sourceObject = PrefabUtility.GetCorrespondingObjectFromSource(renderer.gameObject);
            if (sourceObject == null)
                continue;

            string sourcePath = AssetDatabase.GetAssetPath(sourceObject);
            if (string.Equals(sourcePath, ExistingMapPath, StringComparison.OrdinalIgnoreCase))
                enabledOldSourceRenderers++;
        }

        int finalRenderers = optimizedRoot.GetComponentsInChildren<Renderer>(true).Length;
        int enabledFinalRenderers = optimizedRoot.GetComponentsInChildren<Renderer>(true)
            .Count(renderer => renderer != null && renderer.enabled);

        bool positionMatches = Vector3.Distance(
            optimizedRoot.transform.position,
            existingMapRoot.transform.position) < 0.0001f;
        bool rotationMatches = Quaternion.Angle(
            optimizedRoot.transform.rotation,
            existingMapRoot.transform.rotation) < 0.01f;
        bool scaleMatches = Vector3.Distance(
            optimizedRoot.transform.localScale,
            existingMapRoot.transform.lossyScale) < 0.0001f;

        bool wallPolicyValid = allGroups
            .Where(group => group != null && group.gameObject.name.StartsWith("StoneWall_LODGroup_", StringComparison.Ordinal))
            .All(group =>
            {
                LOD[] lods = group.GetLODs();
                return lods.Length == 3
                    && Mathf.Approximately(lods[0].screenRelativeTransitionHeight, WallTransitionHeights[0])
                    && Mathf.Approximately(lods[1].screenRelativeTransitionHeight, WallTransitionHeights[1])
                    && Mathf.Approximately(lods[2].screenRelativeTransitionHeight, WallTransitionHeights[2]);
            });

        bool pass = standardGroups == 22
            && wallGroups == 56
            && enabledOldSourceRenderers == 0
            && positionMatches
            && rotationMatches
            && scaleMatches
            && wallPolicyValid;

        string message =
            "[Stage1 Final Optimization] FINAL TEST SCENE VALIDATION\n" +
            $"Result: {(pass ? "PASS" : "FAIL")}\n" +
            $"Standard LOD groups: {standardGroups} (expected 22)\n" +
            $"Stone-wall LOD groups: {wallGroups} (expected 56)\n" +
            $"Old source-map renderers still enabled: {enabledOldSourceRenderers} (expected 0)\n" +
            $"Final-map renderers: {enabledFinalRenderers}/{finalRenderers} enabled\n" +
            $"Root position match: {positionMatches}\n" +
            $"Root rotation match: {rotationMatches}\n" +
            $"Root scale match: {scaleMatches}\n" +
            $"Wall LOD policy 35% -> 15% -> 5% -> cull: {wallPolicyValid}";

        if (pass)
            Debug.Log(message, optimizedRoot);
        else
            Debug.LogError(message, optimizedRoot);
    }

    private static Dictionary<string, LodChain> CollectStandardLodChains(GameObject root)
    {
        var chains = new Dictionary<string, LodChain>(StringComparer.OrdinalIgnoreCase);

        foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
        {
            Match match = StandardLodPattern.Match(transform.name);
            if (!match.Success)
                continue;

            Renderer[] renderers = transform.GetComponents<Renderer>();
            if (renderers.Length == 0)
                continue;

            int level = int.Parse(match.Groups["lod"].Value);
            string baseName = match.Groups["base"].Value;
            string suffix = match.Groups["suffix"].Success ? match.Groups["suffix"].Value : string.Empty;
            string key = baseName + suffix;

            if (!chains.TryGetValue(key, out LodChain chain))
            {
                chain = new LodChain(key);
                chains.Add(key, chain);
            }

            chain.Add(level, renderers);
        }

        return chains;
    }

    private static Dictionary<string, WallSet> CollectWallSets(GameObject root)
    {
        var sets = new Dictionary<string, WallSet>(StringComparer.OrdinalIgnoreCase);

        foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
        {
            Match match = WallNamePattern.Match(transform.name);
            if (!match.Success)
                continue;

            int suffixNumber = int.Parse(match.Groups["suffix"].Value);
            if (suffixNumber < 22 || suffixNumber > 77)
                continue;

            int sourceLod = int.Parse(match.Groups["lod"].Value);
            Renderer[] renderers = transform.GetComponents<Renderer>();
            if (renderers.Length == 0)
                continue;

            string suffix = match.Groups["suffix"].Value;
            if (!sets.TryGetValue(suffix, out WallSet set))
            {
                set = new WallSet(suffix);
                sets.Add(suffix, set);
            }

            set.Add(sourceLod, transform, renderers);
        }

        return sets;
    }

    private static bool CreateStandardLodGroup(Transform container, LodChain chain)
    {
        Renderer primary = chain.RenderersByLevel[0].FirstOrDefault(renderer => renderer != null);
        if (primary == null)
            return false;

        GameObject wrapper = new GameObject($"LODGroup_{SanitizeName(chain.Key)}");
        Undo.RegisterCreatedObjectUndo(wrapper, "Create final-map LODGroup wrapper");
        Undo.SetTransformParent(wrapper.transform, container, "Parent final-map LODGroup wrapper");
        wrapper.transform.position = primary.bounds.center;
        wrapper.transform.rotation = container.rotation;
        wrapper.transform.localScale = Vector3.one;

        LODGroup group = Undo.AddComponent<LODGroup>(wrapper);
        var lods = new LOD[5];
        for (int level = 0; level < 5; level++)
        {
            Renderer[] renderers = chain.RenderersByLevel[level]
                .Where(renderer => renderer != null)
                .Distinct()
                .ToArray();
            lods[level] = new LOD(StandardTransitionHeights[level], renderers);
        }

        group.SetLODs(lods);
        group.RecalculateBounds();
        EditorUtility.SetDirty(group);
        return true;
    }

    private static bool CreateWallLodGroup(Transform container, WallSet set)
    {
        Renderer primary = set.RenderersBySourceLod[1].FirstOrDefault(renderer => renderer != null);
        if (primary == null)
            return false;

        GameObject wrapper = new GameObject($"StoneWall_LODGroup_{set.Suffix}");
        Undo.RegisterCreatedObjectUndo(wrapper, "Create stone-wall LODGroup wrapper");
        Undo.SetTransformParent(wrapper.transform, container, "Parent stone-wall LODGroup wrapper");
        wrapper.transform.position = primary.bounds.center;
        wrapper.transform.rotation = container.rotation;
        wrapper.transform.localScale = Vector3.one;

        LODGroup group = Undo.AddComponent<LODGroup>(wrapper);
        var lods = new LOD[3];
        for (int i = 0; i < 3; i++)
        {
            int sourceLod = i + 1;
            Renderer[] renderers = set.RenderersBySourceLod[sourceLod]
                .Where(renderer => renderer != null)
                .Distinct()
                .ToArray();
            lods[i] = new LOD(WallTransitionHeights[i], renderers);
        }

        group.SetLODs(lods);
        group.RecalculateBounds();
        EditorUtility.SetDirty(group);
        return true;
    }

    private static int DisableSourceMapRenderers(GameObject existingMapRoot)
    {
        int disabled = 0;
        foreach (Renderer renderer in existingMapRoot.GetComponentsInChildren<Renderer>(true))
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
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (string.Equals(transform.name, objectName, StringComparison.Ordinal))
                    return transform.gameObject;
            }
        }

        return null;
    }

    private static bool IsWallChain(string key)
    {
        Match match = Regex.Match(key, @"^model\.?(?<suffix>\d{3})$");
        if (!match.Success)
            return false;

        int suffix = int.Parse(match.Groups["suffix"].Value);
        return suffix >= 22 && suffix <= 77;
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

        public IEnumerable<Renderer> AllRenderers()
        {
            return RenderersByLevel.Values.SelectMany(renderers => renderers).Distinct();
        }
    }

    private sealed class WallSet
    {
        public WallSet(string suffix)
        {
            Suffix = suffix;
            RenderersBySourceLod = new Dictionary<int, List<Renderer>>();
            TransformsBySourceLod = new Dictionary<int, Transform>();
        }

        public string Suffix { get; }
        public Dictionary<int, List<Renderer>> RenderersBySourceLod { get; }
        public Dictionary<int, Transform> TransformsBySourceLod { get; }

        public bool IsComplete => Enumerable.Range(1, 3).All(level =>
            RenderersBySourceLod.TryGetValue(level, out List<Renderer> renderers)
            && renderers.Any(renderer => renderer != null));

        public void Add(int sourceLod, Transform transform, IEnumerable<Renderer> renderers)
        {
            if (!RenderersBySourceLod.TryGetValue(sourceLod, out List<Renderer> levelRenderers))
            {
                levelRenderers = new List<Renderer>();
                RenderersBySourceLod.Add(sourceLod, levelRenderers);
            }

            levelRenderers.AddRange(renderers.Where(renderer => renderer != null));
            TransformsBySourceLod[sourceLod] = transform;
        }

        public string DescribeLevels()
        {
            return string.Join(",", RenderersBySourceLod.Keys.OrderBy(level => level).Select(level => $"LOD{level}"));
        }
    }
}
#endif
