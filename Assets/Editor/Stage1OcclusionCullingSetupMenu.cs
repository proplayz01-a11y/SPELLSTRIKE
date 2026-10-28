#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class Stage1OcclusionCullingSetupMenu
{
    private const string TestScenePath = "Assets/Scenes/OptimizationTests/Stage1_OptimizedMap_Test.unity";
    private const string OptimizedRootName = "level1_STAGE1_OPTIMIZED_TEST";
    private const string TriggerPath = "Temp/Stage1OcclusionSetup.trigger";
    private const string OcclusionAreaName = "__STAGE1_GAMEPLAY_OCCLUSION_AREA";

    private const float GameplayHorizontalMargin = 600f;
    private const float GameplayVerticalMargin = 300f;
    private const float BroadFlatSpanThreshold = 1000f;
    private const float BroadFlatHeightRatio = 0.10f;

    // Conservative first-pass occluder thresholds. Large village walls/building pieces qualify;
    // small clutter and thin horizontal surfaces remain occludees only.
    private const float MinimumOccluderHeight = 1.5f;
    private const float MinimumOccluderHorizontalSpan = 4f;

    private static readonly string[] VegetationKeywords =
    {
        "grass", "tree", "bush", "plant", "flower", "mushroom", "leaf", "leaves",
        "branch", "vine", "fern", "weed", "foliage"
    };

    private static bool bakeMonitorActive;
    private static bool bakeObservedRunning;
    private static double bakeStartTime;

    static Stage1OcclusionCullingSetupMenu()
    {
        EditorApplication.update += WatchTrigger;
    }

    [MenuItem("SPELLSTRIKE/Optimization/8. Audit Stage 1 Occlusion Candidates")]
    public static void AuditCandidates()
    {
        if (!TryGetTestSceneAndRoot(out Scene scene, out GameObject optimizedRoot))
            return;

        OcclusionPlan plan = BuildPlan(optimizedRoot);
        Camera mainCamera = FindMainCamera(scene);
        bool hasOccludeeBounds = TryCalculateCombinedBounds(plan.Occludees, out Bounds occludeeBounds);
        bool hasOccluderBounds = TryCalculateCombinedBounds(plan.Occluders, out Bounds occluderBounds);

        Debug.Log(
            "[Stage1 Occlusion] AUDIT\n" +
            $"Scene: {TestScenePath}\n" +
            $"Optimized root: {optimizedRoot.name}\n" +
            $"LOD groups: {plan.LodGroupCount}\n" +
            $"Enabled mesh renderers considered: {plan.Occludees.Count}\n" +
            $"Occluder candidates: {plan.Occluders.Count}\n" +
            $"Lower-LOD renderers kept occludee-only: {plan.LowerLodCount}\n" +
            $"Vegetation excluded from occluders: {plan.VegetationExcludedCount}\n" +
            $"Transparent renderers excluded from occluders: {plan.TransparentExcludedCount}\n" +
            $"Too-small/flat renderers excluded from occluders: {plan.SizeExcludedCount}\n" +
            $"Broad flat/background renderers excluded from occluders: {plan.BroadFlatExcludedCount}\n" +
            $"Occludee bounds: {(hasOccludeeBounds ? FormatBounds(occludeeBounds) : "none")}\n" +
            $"Occluder bounds: {(hasOccluderBounds ? FormatBounds(occluderBounds) : "none")}\n" +
            $"Main Camera found: {mainCamera != null}\n" +
            $"Main Camera position: {(mainCamera != null ? FormatVector(mainCamera.transform.position) : "n/a")}\n" +
            $"Main Camera occlusion enabled: {(mainCamera != null && mainCamera.useOcclusionCulling)}\n" +
            "Gameplay anchors:\n" + BuildGameplayAnchorSummary(scene) + "\n" +
            "Largest renderer bounds:\n" + BuildLargestRendererSummary(optimizedRoot, 15),
            optimizedRoot);
    }

    [MenuItem("SPELLSTRIKE/Optimization/9. Configure + Bake Stage 1 Occlusion")]
    public static void ConfigureAndBake()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[Stage1 Occlusion] Exit Play Mode before configuring occlusion culling.");
            return;
        }

        if (StaticOcclusionCulling.isRunning)
        {
            Debug.LogError("[Stage1 Occlusion] An occlusion bake is already running. No changes were made.");
            return;
        }

        if (!TryGetTestSceneAndRoot(out Scene scene, out GameObject optimizedRoot))
            return;

        OcclusionPlan plan = BuildPlan(optimizedRoot);
        if (plan.Occludees.Count == 0 || plan.Occluders.Count == 0)
        {
            Debug.LogError(
                $"[Stage1 Occlusion] Refusing to bake: occludees={plan.Occludees.Count}, occluders={plan.Occluders.Count}. " +
                "The optimized root may not be configured as expected.",
                optimizedRoot);
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Configure Stage 1 Occlusion Culling");

        try
        {
            HashSet<GameObject> rendererObjects = new HashSet<GameObject>(
                optimizedRoot.GetComponentsInChildren<Renderer>(true)
                    .Where(renderer => renderer != null)
                    .Select(renderer => renderer.gameObject));

            foreach (GameObject go in rendererObjects)
            {
                Undo.RecordObject(go, "Configure occlusion static flags");
                StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(go);
                flags &= ~(StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);

                if (plan.Occludees.Contains(go))
                    flags |= StaticEditorFlags.OccludeeStatic;

                if (plan.Occluders.Contains(go))
                    flags |= StaticEditorFlags.OccluderStatic;

                GameObjectUtility.SetStaticEditorFlags(go, flags);
                EditorUtility.SetDirty(go);
            }

            Camera mainCamera = FindMainCamera(scene);
            if (mainCamera == null)
            {
                Debug.LogError("[Stage1 Occlusion] Main Camera was not found in the test scene. Occlusion flags were not baked.");
                Undo.RevertAllDownToGroup(undoGroup);
                return;
            }

            Undo.RecordObject(mainCamera, "Enable camera occlusion culling");
            mainCamera.useOcclusionCulling = true;
            EditorUtility.SetDirty(mainCamera);

            OcclusionArea occlusionArea = EnsureGameplayOcclusionArea(scene, mainCamera);
            if (occlusionArea == null)
                throw new InvalidOperationException("Could not create the Stage 1 gameplay OcclusionArea.");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Unity failed to save the Stage 1 optimization test scene before the occlusion bake.");

            StaticOcclusionCulling.Clear();
            StaticOcclusionCulling.GenerateInBackground();

            bakeMonitorActive = true;
            bakeObservedRunning = StaticOcclusionCulling.isRunning;
            bakeStartTime = EditorApplication.timeSinceStartup;
            EditorApplication.update -= MonitorBake;
            EditorApplication.update += MonitorBake;

            Debug.Log(
                "[Stage1 Occlusion] CONFIGURED; BAKE STARTED\n" +
                $"Occludees: {plan.Occludees.Count}\n" +
                $"Occluders: {plan.Occluders.Count}\n" +
                $"LOD groups: {plan.LodGroupCount}\n" +
                $"Lower LODs excluded as occluders: {plan.LowerLodCount}\n" +
                $"Broad flat/background renderers excluded as occluders: {plan.BroadFlatExcludedCount}\n" +
                $"Occlusion area: center={FormatVector(occlusionArea.transform.position)}, size={FormatVector(occlusionArea.size)}\n" +
                "Scope: optimized visual root only. Gameplay objects were not modified.\n" +
                "Bake scope: bounded to the Stage 1 gameplay route plus safety margin.",
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

    [MenuItem("SPELLSTRIKE/Optimization/10. Validate Stage 1 Occlusion Setup")]
    public static void ValidateSetup()
    {
        if (!TryGetTestSceneAndRoot(out Scene scene, out GameObject optimizedRoot))
            return;

        OcclusionPlan plan = BuildPlan(optimizedRoot);
        int flaggedOccludees = 0;
        int flaggedOccluders = 0;

        foreach (Renderer renderer in optimizedRoot.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null)
                continue;

            StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(renderer.gameObject);
            if ((flags & StaticEditorFlags.OccludeeStatic) != 0)
                flaggedOccludees++;
            if ((flags & StaticEditorFlags.OccluderStatic) != 0)
                flaggedOccluders++;
        }

        Camera mainCamera = FindMainCamera(scene);
        OcclusionArea occlusionArea = FindOcclusionArea(scene);
        bool pass = flaggedOccludees == plan.Occludees.Count
            && flaggedOccluders == plan.Occluders.Count
            && mainCamera != null
            && mainCamera.useOcclusionCulling
            && occlusionArea != null
            && StaticOcclusionCulling.umbraDataSize > 0;

        Debug.Log(
            "[Stage1 Occlusion] VALIDATION\n" +
            $"Result: {(pass ? "PASS" : "FAIL")}\n" +
            $"Occludee flags: {flaggedOccludees} (expected {plan.Occludees.Count})\n" +
            $"Occluder flags: {flaggedOccluders} (expected {plan.Occluders.Count})\n" +
            $"Camera occlusion enabled: {(mainCamera != null && mainCamera.useOcclusionCulling)}\n" +
            $"Gameplay OcclusionArea present: {occlusionArea != null}\n" +
            $"OcclusionArea size: {(occlusionArea != null ? FormatVector(occlusionArea.size) : "n/a")}\n" +
            $"Baked PVS size: {StaticOcclusionCulling.umbraDataSize:N0} bytes\n" +
            $"Bake currently running: {StaticOcclusionCulling.isRunning}",
            optimizedRoot);
    }

    private static void WatchTrigger()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        string fullPath = Path.GetFullPath(TriggerPath);
        if (!File.Exists(fullPath))
            return;

        string command;
        try
        {
            command = File.ReadAllText(fullPath).Trim();
            File.Delete(fullPath);
        }
        catch (IOException)
        {
            // Trigger files are written by an external helper. If we collide with the tiny
            // write window, leave the file in place and retry on the next editor update.
            return;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            return;
        }

        if (string.Equals(command, "audit", StringComparison.OrdinalIgnoreCase))
            AuditCandidates();
        else if (string.Equals(command, "validate", StringComparison.OrdinalIgnoreCase))
            ValidateSetup();
        else
            ConfigureAndBake();
    }

    private static void MonitorBake()
    {
        if (!bakeMonitorActive)
        {
            EditorApplication.update -= MonitorBake;
            return;
        }

        if (StaticOcclusionCulling.isRunning)
        {
            bakeObservedRunning = true;
            return;
        }

        // Give GenerateInBackground a brief chance to transition into its running state.
        if (!bakeObservedRunning && EditorApplication.timeSinceStartup - bakeStartTime < 2.0)
            return;

        bakeMonitorActive = false;
        EditorApplication.update -= MonitorBake;

        Scene scene = SceneManager.GetActiveScene();
        if (scene.IsValid() && scene.isLoaded && string.Equals(scene.path, TestScenePath, StringComparison.OrdinalIgnoreCase))
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        long dataSize = StaticOcclusionCulling.umbraDataSize;
        if (dataSize > 0)
        {
            Debug.Log($"[Stage1 Occlusion] BAKE COMPLETE. PVS data size: {dataSize:N0} bytes.");
            ValidateSetup();
        }
        else
        {
            Debug.LogError(
                "[Stage1 Occlusion] Bake stopped without producing PVS data. Static flags remain configured; inspect the Occlusion Culling window before retrying.");
        }
    }

    private static OcclusionPlan BuildPlan(GameObject optimizedRoot)
    {
        var plan = new OcclusionPlan();
        var firstLodRenderers = new HashSet<Renderer>();
        var anyLodRenderers = new HashSet<Renderer>();

        LODGroup[] lodGroups = optimizedRoot.GetComponentsInChildren<LODGroup>(true);
        plan.LodGroupCount = lodGroups.Length;

        foreach (LODGroup group in lodGroups)
        {
            if (group == null)
                continue;

            LOD[] lods = group.GetLODs();
            for (int i = 0; i < lods.Length; i++)
            {
                foreach (Renderer renderer in lods[i].renderers)
                {
                    if (renderer == null)
                        continue;

                    anyLodRenderers.Add(renderer);
                    if (i == 0)
                        firstLodRenderers.Add(renderer);
                }
            }
        }

        foreach (Renderer renderer in optimizedRoot.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                continue;

            if (!(renderer is MeshRenderer))
                continue;

            GameObject go = renderer.gameObject;
            plan.Occludees.Add(go);

            if (anyLodRenderers.Contains(renderer) && !firstLodRenderers.Contains(renderer))
            {
                plan.LowerLodCount++;
                continue;
            }

            if (HasVegetationKeyword(renderer.transform, optimizedRoot.transform))
            {
                plan.VegetationExcludedCount++;
                continue;
            }

            if (IsTransparent(renderer))
            {
                plan.TransparentExcludedCount++;
                continue;
            }

            Bounds bounds = renderer.bounds;
            float horizontalSpan = Mathf.Max(bounds.size.x, bounds.size.z);
            if (bounds.size.y < MinimumOccluderHeight || horizontalSpan < MinimumOccluderHorizontalSpan)
            {
                plan.SizeExcludedCount++;
                continue;
            }

            if (horizontalSpan >= BroadFlatSpanThreshold
                && bounds.size.y / Mathf.Max(horizontalSpan, 0.001f) < BroadFlatHeightRatio)
            {
                plan.BroadFlatExcludedCount++;
                continue;
            }

            plan.Occluders.Add(go);
        }

        return plan;
    }

    private static bool IsTransparent(Renderer renderer)
    {
        foreach (Material material in renderer.sharedMaterials)
        {
            if (material != null && material.renderQueue > 2500)
                return true;
        }

        return false;
    }

    private static bool HasVegetationKeyword(Transform transform, Transform stopAt)
    {
        Transform current = transform;
        while (current != null)
        {
            string lowerName = current.name.ToLowerInvariant();
            for (int i = 0; i < VegetationKeywords.Length; i++)
            {
                if (lowerName.Contains(VegetationKeywords[i]))
                    return true;
            }

            if (current == stopAt)
                break;
            current = current.parent;
        }

        return false;
    }

    private static string BuildGameplayAnchorSummary(Scene scene)
    {
        string[] anchorNames =
        {
            "NODE 1", "NODE 2", "NODE 3", "NODE 4",
            "Node1_ShrineTrigger", "HollowKnight_TriggerEncounter",
            "StoneSentinelTrigger", "CursedJester_EncounterTrigger",
            "Boss Shrine Center"
        };

        var transformsByName = new Dictionary<string, Transform>(StringComparer.Ordinal);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (transform != null && !transformsByName.ContainsKey(transform.name))
                    transformsByName.Add(transform.name, transform);
            }
        }

        return string.Join("\n", anchorNames.Select(name =>
            transformsByName.TryGetValue(name, out Transform transform)
                ? $"  {name}: {FormatVector(transform.position)}"
                : $"  {name}: not found"));
    }

    private static string BuildLargestRendererSummary(GameObject optimizedRoot, int count)
    {
        return string.Join("\n", optimizedRoot.GetComponentsInChildren<Renderer>(true)
            .Where(renderer => renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy)
            .OrderByDescending(renderer => Mathf.Max(renderer.bounds.size.x, Mathf.Max(renderer.bounds.size.y, renderer.bounds.size.z)))
            .Take(count)
            .Select(renderer => $"  {GetRelativePath(renderer.transform, optimizedRoot.transform)} | {FormatBounds(renderer.bounds)}"));
    }

    private static string GetRelativePath(Transform transform, Transform root)
    {
        var names = new List<string>();
        Transform current = transform;
        while (current != null)
        {
            names.Add(current.name);
            if (current == root)
                break;
            current = current.parent;
        }
        names.Reverse();
        return string.Join("/", names);
    }

    private static string FormatVector(Vector3 value)
    {
        return $"({value.x:F1}, {value.y:F1}, {value.z:F1})";
    }

    private static bool TryCalculateCombinedBounds(IEnumerable<GameObject> objects, out Bounds combinedBounds)
    {
        combinedBounds = default;
        bool initialized = false;

        foreach (GameObject go in objects)
        {
            if (go == null)
                continue;

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer == null)
                continue;

            if (!initialized)
            {
                combinedBounds = renderer.bounds;
                initialized = true;
            }
            else
            {
                combinedBounds.Encapsulate(renderer.bounds);
            }
        }

        return initialized;
    }

    private static string FormatBounds(Bounds bounds)
    {
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        Vector3 size = bounds.size;
        Vector3 center = bounds.center;
        return $"center=({center.x:F1}, {center.y:F1}, {center.z:F1}), " +
               $"size=({size.x:F1}, {size.y:F1}, {size.z:F1}), " +
               $"min=({min.x:F1}, {min.y:F1}, {min.z:F1}), " +
               $"max=({max.x:F1}, {max.y:F1}, {max.z:F1})";
    }

    private static OcclusionArea EnsureGameplayOcclusionArea(Scene scene, Camera mainCamera)
    {
        var points = new List<Vector3>();
        string[] anchorNames =
        {
            "NODE 1", "NODE 2", "NODE 3", "NODE 4",
            "Node1_ShrineTrigger", "HollowKnight_TriggerEncounter",
            "StoneSentinelTrigger", "CursedJester_EncounterTrigger",
            "Boss Shrine Center"
        };

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (transform != null && anchorNames.Contains(transform.name))
                    points.Add(transform.position);
            }
        }

        if (mainCamera != null)
            points.Add(mainCamera.transform.position);

        if (points.Count == 0)
            return null;

        Bounds gameplayBounds = new Bounds(points[0], Vector3.zero);
        for (int i = 1; i < points.Count; i++)
            gameplayBounds.Encapsulate(points[i]);

        gameplayBounds.Expand(new Vector3(
            GameplayHorizontalMargin * 2f,
            GameplayVerticalMargin * 2f,
            GameplayHorizontalMargin * 2f));

        OcclusionArea area = FindOcclusionArea(scene);
        if (area == null)
        {
            GameObject areaObject = new GameObject(OcclusionAreaName);
            Undo.RegisterCreatedObjectUndo(areaObject, "Create Stage 1 gameplay occlusion area");
            SceneManager.MoveGameObjectToScene(areaObject, scene);
            area = Undo.AddComponent<OcclusionArea>(areaObject);
        }
        else
        {
            Undo.RecordObject(area.transform, "Update Stage 1 gameplay occlusion area");
            Undo.RecordObject(area, "Update Stage 1 gameplay occlusion area");
        }

        area.transform.position = gameplayBounds.center;
        area.transform.rotation = Quaternion.identity;
        area.transform.localScale = Vector3.one;
        area.center = Vector3.zero;
        area.size = gameplayBounds.size;
        EditorUtility.SetDirty(area);
        EditorUtility.SetDirty(area.gameObject);
        return area;
    }

    private static OcclusionArea FindOcclusionArea(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (OcclusionArea area in root.GetComponentsInChildren<OcclusionArea>(true))
            {
                if (area != null && string.Equals(area.gameObject.name, OcclusionAreaName, StringComparison.Ordinal))
                    return area;
            }
        }
        return null;
    }

    private static Camera FindMainCamera(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Camera camera in root.GetComponentsInChildren<Camera>(true))
            {
                if (camera != null && camera.CompareTag("MainCamera"))
                    return camera;
            }
        }

        return null;
    }

    private static bool TryGetTestSceneAndRoot(out Scene scene, out GameObject optimizedRoot)
    {
        scene = SceneManager.GetActiveScene();
        optimizedRoot = null;

        if (!scene.IsValid() || !scene.isLoaded || !string.Equals(scene.path, TestScenePath, StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogError($"[Stage1 Occlusion] Open the dedicated test scene first: {TestScenePath}. No changes were made.");
            return false;
        }

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform match = root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(transform => string.Equals(transform.name, OptimizedRootName, StringComparison.Ordinal));
            if (match != null)
            {
                optimizedRoot = match.gameObject;
                break;
            }
        }

        if (optimizedRoot == null)
        {
            Debug.LogError($"[Stage1 Occlusion] Could not find optimized visual root '{OptimizedRootName}'. No changes were made.");
            return false;
        }

        return true;
    }

    private sealed class OcclusionPlan
    {
        public readonly HashSet<GameObject> Occludees = new HashSet<GameObject>();
        public readonly HashSet<GameObject> Occluders = new HashSet<GameObject>();
        public int LodGroupCount;
        public int LowerLodCount;
        public int VegetationExcludedCount;
        public int TransparentExcludedCount;
        public int SizeExcludedCount;
        public int BroadFlatExcludedCount;
    }
}
#endif