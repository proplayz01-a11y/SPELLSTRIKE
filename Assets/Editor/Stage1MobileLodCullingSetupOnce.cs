#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class Stage1MobileLodCullingSetupOnce
{
    private const string Marker = "Temp/Stage1MobileLodCullingSetup.trigger";
    private const string ScenePath = "Assets/Scenes/OptimizationTests/Stage1_OptimizedMap_Test.unity";
    private const string RootName = "level1_STAGE1_MOBILE_TEST";
    private const string StandardContainerName = "__MOBILE_OPTIMIZED_LOD_GROUPS";
    private const string WallContainerName = "__MOBILE_STONE_WALL_LOD_GROUPS";

    private static readonly float[] StandardHeights = { 0.60f, 0.35f, 0.18f, 0.08f, 0.02f };
    private static readonly float[] ExtraBuildingHeights = { 0.50f, 0.25f, 0.10f, 0.025f };
    private static readonly float[] WallHeights = { 0.35f, 0.15f, 0.001f };

    private static readonly Regex StandardPattern = new Regex(
        @"^(?<base>.+)_LOD(?<lod>[0-4])(?<suffix>\.\d+)?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex WallPattern = new Regex(
        @"^model_LOD(?<lod>[1-3])\.(?<suffix>\d{3})$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    static Stage1MobileLodCullingSetupOnce() => EditorApplication.update += TryRun;

    private static void TryRun()
    {
        string marker = Path.GetFullPath(Marker);
        if (!File.Exists(marker)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        File.Delete(marker);

        string backup = Path.GetFullPath("Temp/Stage1_OptimizedMap_Test_before_mobile_lod_culling.unity");
        File.Copy(Path.GetFullPath(ScenePath), backup, true);

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject root = scene.GetRootGameObjects().FirstOrDefault(go => go.name == RootName);
        if (root == null)
            throw new InvalidOperationException($"Expected mobile map root '{RootName}' not found.");

        // This setup intentionally never adds/removes/configures colliders. The user's
        // manually managed ground collider is outside the scope of this optimization.
        DestroyNamedChild(root.transform, StandardContainerName);
        DestroyNamedChild(root.transform, WallContainerName);

        Transform standardContainer = NewContainer(root.transform, StandardContainerName);
        Transform wallContainer = NewContainer(root.transform, WallContainerName);

        Dictionary<string, Dictionary<int, List<Renderer>>> chains = CollectChains(root);
        int standardGroups = 0;
        int extraBuildingGroups = 0;

        foreach (KeyValuePair<string, Dictionary<int, List<Renderer>>> pair in chains.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
        {
            string key = pair.Key;
            Dictionary<int, List<Renderer>> levels = pair.Value;

            if (Enumerable.Range(0, 5).All(levels.ContainsKey))
            {
                Renderer[][] renderers = Enumerable.Range(0, 5)
                    .Select(i => levels[i].Where(r => r != null).Distinct().ToArray())
                    .ToArray();
                if (CreateLodGroup(standardContainer, $"LODGroup_{Sanitize(key)}", renderers, StandardHeights))
                    standardGroups++;
                continue;
            }

            // The newly generated Corner/Damaged/Market assets intentionally begin at
            // LOD1, which is their highest-detail mesh. Keep those useful chains too.
            if (!key.StartsWith("model", StringComparison.OrdinalIgnoreCase)
                && Enumerable.Range(1, 4).All(levels.ContainsKey))
            {
                Renderer[][] renderers = Enumerable.Range(1, 4)
                    .Select(i => levels[i].Where(r => r != null).Distinct().ToArray())
                    .ToArray();
                if (CreateLodGroup(standardContainer, $"LODGroup_{Sanitize(key)}", renderers, ExtraBuildingHeights))
                    extraBuildingGroups++;
            }
        }

        Dictionary<string, Dictionary<int, List<Renderer>>> walls = CollectWalls(root);
        int wallGroups = 0;
        foreach (KeyValuePair<string, Dictionary<int, List<Renderer>>> pair in walls.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            if (!Enumerable.Range(1, 3).All(pair.Value.ContainsKey))
                continue;

            Renderer[][] renderers = Enumerable.Range(1, 3)
                .Select(i => pair.Value[i].Where(r => r != null).Distinct().ToArray())
                .ToArray();
            if (CreateLodGroup(wallContainer, $"StoneWall_LODGroup_{pair.Key}", renderers, WallHeights))
                wallGroups++;
        }

        Stage1DistanceCullingController culler = root.GetComponent<Stage1DistanceCullingController>();
        if (culler == null)
            culler = root.AddComponent<Stage1DistanceCullingController>();

        SerializedObject serializedCuller = new SerializedObject(culler);
        serializedCuller.FindProperty("tinyDecorDistance").floatValue = 55f;
        serializedCuller.FindProperty("smallPropDistance").floatValue = 95f;
        serializedCuller.FindProperty("mediumPropDistance").floatValue = 145f;
        serializedCuller.FindProperty("structureDistance").floatValue = 240f;
        serializedCuller.FindProperty("landmarkDistance").floatValue = 400f;
        serializedCuller.FindProperty("updateInterval").floatValue = 0.25f;
        serializedCuller.FindProperty("logClassificationSummary").boolValue = false;
        serializedCuller.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(culler);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        // Audit the values Unity is actually holding after serialization. The previous
        // setup could report COMPLETE while an older scene value remained on disk.
        serializedCuller.Update();
        Debug.Log(
            $"[Stage1 Mobile LOD Setup] CULLER_VERIFY " +
            $"tiny={serializedCuller.FindProperty("tinyDecorDistance").floatValue:F0} " +
            $"small={serializedCuller.FindProperty("smallPropDistance").floatValue:F0} " +
            $"medium={serializedCuller.FindProperty("mediumPropDistance").floatValue:F0} " +
            $"structure={serializedCuller.FindProperty("structureDistance").floatValue:F0} " +
            $"landmark={serializedCuller.FindProperty("landmarkDistance").floatValue:F0}");

        int totalLodGroups = root.GetComponentsInChildren<LODGroup>(true).Length;
        string report =
            $"standard0to4={standardGroups}\n" +
            $"extra1to4={extraBuildingGroups}\n" +
            $"walls1to3={wallGroups}\n" +
            $"totalLODGroups={totalLodGroups}\n" +
            $"distanceCuller={(root.GetComponent<Stage1DistanceCullingController>() != null)}\n" +
            $"backup={backup}\n";
        File.WriteAllText(Path.GetFullPath("Temp/Stage1MobileLodCullingSetupReport.txt"), report);

        if (standardGroups != 22 || wallGroups != 56 || totalLodGroups < 78)
            Debug.LogWarning("[Stage1 Mobile LOD Setup] Unexpected group count.\n" + report);
        else
            Debug.Log("[Stage1 Mobile LOD Setup] COMPLETE\n" + report);
    }

    private static Dictionary<string, Dictionary<int, List<Renderer>>> CollectChains(GameObject root)
    {
        var result = new Dictionary<string, Dictionary<int, List<Renderer>>>(StringComparer.OrdinalIgnoreCase);
        foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
        {
            Match match = StandardPattern.Match(transform.name);
            if (!match.Success) continue;

            Renderer[] renderers = transform.GetComponents<Renderer>();
            if (renderers.Length == 0) continue;

            int level = int.Parse(match.Groups["lod"].Value);
            string key = match.Groups["base"].Value + (match.Groups["suffix"].Success ? match.Groups["suffix"].Value : string.Empty);
            if (!result.TryGetValue(key, out Dictionary<int, List<Renderer>> levels))
            {
                levels = new Dictionary<int, List<Renderer>>();
                result.Add(key, levels);
            }
            if (!levels.TryGetValue(level, out List<Renderer> list))
            {
                list = new List<Renderer>();
                levels.Add(level, list);
            }
            list.AddRange(renderers);
        }
        return result;
    }

    private static Dictionary<string, Dictionary<int, List<Renderer>>> CollectWalls(GameObject root)
    {
        var result = new Dictionary<string, Dictionary<int, List<Renderer>>>(StringComparer.OrdinalIgnoreCase);
        foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
        {
            Match match = WallPattern.Match(transform.name);
            if (!match.Success) continue;

            int suffixNumber = int.Parse(match.Groups["suffix"].Value);
            if (suffixNumber < 22 || suffixNumber > 77) continue;

            Renderer[] renderers = transform.GetComponents<Renderer>();
            if (renderers.Length == 0) continue;

            string suffix = match.Groups["suffix"].Value;
            int level = int.Parse(match.Groups["lod"].Value);
            if (!result.TryGetValue(suffix, out Dictionary<int, List<Renderer>> levels))
            {
                levels = new Dictionary<int, List<Renderer>>();
                result.Add(suffix, levels);
            }
            if (!levels.TryGetValue(level, out List<Renderer> list))
            {
                list = new List<Renderer>();
                levels.Add(level, list);
            }
            list.AddRange(renderers);
        }
        return result;
    }

    private static bool CreateLodGroup(Transform container, string name, Renderer[][] renderersByLevel, float[] heights)
    {
        Renderer primary = renderersByLevel.SelectMany(x => x).FirstOrDefault(r => r != null);
        if (primary == null || renderersByLevel.Length != heights.Length)
            return false;

        GameObject wrapper = new GameObject(name);
        wrapper.transform.SetParent(container, true);
        wrapper.transform.position = primary.bounds.center;
        wrapper.transform.rotation = Quaternion.identity;
        wrapper.transform.localScale = Vector3.one;

        LODGroup group = wrapper.AddComponent<LODGroup>();
        LOD[] lods = new LOD[renderersByLevel.Length];
        for (int i = 0; i < lods.Length; i++)
            lods[i] = new LOD(heights[i], renderersByLevel[i]);
        group.SetLODs(lods);
        group.fadeMode = LODFadeMode.None;
        group.RecalculateBounds();
        return true;
    }

    private static Transform NewContainer(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static void DestroyNamedChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null)
            UnityEngine.Object.DestroyImmediate(child.gameObject);
    }

    private static string Sanitize(string value)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            value = value.Replace(c, '_');
        return value.Replace('.', '_').Replace(' ', '_');
    }
}
#endif
