using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SunboundWardenHelmetWeightFixMenu
{
    public const string ModelPath = "Assets/Stage 3/Enemies/STAGE 3 ENEMY NO. 1/SunboundWarden_Rig.fbx";
    public const string FixedMeshFolder = "Assets/Stage 3/Enemies/STAGE 3 ENEMY NO. 1/Meshes";
    public const string FixedMeshPath = FixedMeshFolder + "/SunboundWarden_HelmetFixedMesh.asset";
    private const string PrefabPath = "Assets/Stage 3/Prefabs/SunboundWarden.prefab";
    private const string ScenePath = "Assets/Scenes/Stage3Scene/Stage3Scene.unity";

    private static readonly string[] ShoulderArmBoneNames =
    {
        "mixamorig:LeftShoulder",
        "mixamorig:RightShoulder",
        "mixamorig:LeftArm",
        "mixamorig:RightArm",
    };

    [MenuItem("Tools/SPELLSTRIKE/Stage 3/Fix Sunbound Warden Helmet Weights")]
    public static void ApplyFix()
    {
        EnsureFolder(FixedMeshFolder);

        ModelImporter importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        if (importer == null)
            throw new InvalidOperationException($"Could not resolve ModelImporter for {ModelPath}");

        bool originalReadable = importer.isReadable;
        GameObject temp = null;

        try
        {
            if (!originalReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }

            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (modelAsset == null)
                throw new InvalidOperationException($"Could not load model asset: {ModelPath}");

            temp = UnityEngine.Object.Instantiate(modelAsset);
            temp.name = "SunboundWarden_HelmetFix_Temp";
            temp.hideFlags = HideFlags.HideAndDontSave;

            SkinnedMeshRenderer sourceRenderer = temp.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (sourceRenderer == null || sourceRenderer.sharedMesh == null)
                throw new InvalidOperationException("Sunbound Warden model has no SkinnedMeshRenderer/sharedMesh.");

            Mesh source = sourceRenderer.sharedMesh;
            BoneWeight[] weights = source.boneWeights;
            Vector3[] vertices = source.vertices;
            int[] triangles = source.triangles;

            if (weights.Length != vertices.Length)
                throw new InvalidOperationException($"BoneWeight/vertex mismatch: {weights.Length} vs {vertices.Length}");

            Transform[] bones = sourceRenderer.bones;
            Dictionary<string, int> boneIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i] != null && !boneIndex.ContainsKey(bones[i].name))
                    boneIndex.Add(bones[i].name, i);
            }

            if (!boneIndex.TryGetValue("mixamorig:Head", out int headIndex))
                throw new InvalidOperationException("mixamorig:Head was not found in the SkinnedMeshRenderer bone array.");

            HashSet<int> shoulderArmIndices = new HashSet<int>();
            foreach (string boneName in ShoulderArmBoneNames)
            {
                if (boneIndex.TryGetValue(boneName, out int index))
                    shoulderArmIndices.Add(index);
            }

            List<int>[] adjacency = BuildAdjacency(vertices.Length, triangles);
            List<List<int>> components = BuildComponents(adjacency);

            float helmetMinY = source.bounds.min.y + source.bounds.size.y * 0.855f;
            HashSet<int> selectedVertices = new HashSet<int>();
            List<string> selectedDescriptions = new List<string>();

            for (int componentIndex = 0; componentIndex < components.Count; componentIndex++)
            {
                List<int> component = components[componentIndex];
                float maxY = float.NegativeInfinity;
                float minY = float.PositiveInfinity;
                float headSum = 0f;
                float shoulderArmSum = 0f;

                foreach (int vertexIndex in component)
                {
                    Vector3 v = vertices[vertexIndex];
                    minY = Mathf.Min(minY, v.y);
                    maxY = Mathf.Max(maxY, v.y);
                    BoneWeight bw = weights[vertexIndex];
                    headSum += GetWeight(bw, headIndex);
                    foreach (int upperIndex in shoulderArmIndices)
                        shoulderArmSum += GetWeight(bw, upperIndex);
                }

                float headAverage = headSum / component.Count;
                float shoulderArmAverage = shoulderArmSum / component.Count;

                if (maxY >= helmetMinY && headAverage >= 0.40f && shoulderArmAverage < 0.03f)
                {
                    foreach (int vertexIndex in component)
                        selectedVertices.Add(vertexIndex);

                    selectedDescriptions.Add(
                        $"C{componentIndex}: verts={component.Count} y={minY:F4}..{maxY:F4} " +
                        $"HeadAvg={headAverage:F3} ShoulderArmAvg={shoulderArmAverage:F3}");
                }
            }

            int changed = 0;
            foreach (int vertexIndex in selectedVertices)
            {
                BoneWeight current = weights[vertexIndex];
                float headWeight = GetWeight(current, headIndex);
                float otherWeight = 1f - headWeight;
                if (headWeight >= 0.999999f && otherWeight <= 0.000001f)
                    continue;

                weights[vertexIndex] = new BoneWeight
                {
                    boneIndex0 = headIndex,
                    weight0 = 1f,
                    boneIndex1 = 0,
                    weight1 = 0f,
                    boneIndex2 = 0,
                    weight2 = 0f,
                    boneIndex3 = 0,
                    weight3 = 0f,
                };
                changed++;
            }

            Mesh fixedMesh = UnityEngine.Object.Instantiate(source);
            fixedMesh.name = "SunboundWarden_HelmetFixedMesh";
            fixedMesh.boneWeights = weights;

            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(FixedMeshPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(fixedMesh, FixedMeshPath);
                existing = fixedMesh;
            }
            else
            {
                EditorUtility.CopySerialized(fixedMesh, existing);
                UnityEngine.Object.DestroyImmediate(fixedMesh);
                EditorUtility.SetDirty(existing);
            }

            AssetDatabase.SaveAssets();
            ApplyFixedMeshToPrefab(existing);
            ApplyFixedMeshToStage3Scene(existing);

            Debug.Log(
                "[SunboundWardenHelmetFix] COMPLETE\n" +
                $"Source mesh verts={vertices.Length}, components={components.Count}, selectedVerts={selectedVertices.Count}, changedVerts={changed}\n" +
                $"Source bounds={source.bounds} helmetMinY={helmetMinY:F4}\n" +
                string.Join("\n", selectedDescriptions) + "\n" +
                $"Fixed mesh={FixedMeshPath}");
        }
        finally
        {
            if (temp != null)
                UnityEngine.Object.DestroyImmediate(temp);

            importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer != null && importer.isReadable != originalReadable)
            {
                importer.isReadable = originalReadable;
                importer.SaveAndReimport();
            }
        }
    }

    public static void ApplyFixedMeshIfAvailable(GameObject visual)
    {
        Mesh fixedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(FixedMeshPath);
        if (fixedMesh == null || visual == null)
            return;

        SkinnedMeshRenderer renderer = visual.GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (renderer == null)
            return;

        renderer.sharedMesh = fixedMesh;
        EditorUtility.SetDirty(renderer);
    }

    private static void ApplyFixedMeshToPrefab(Mesh fixedMesh)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            SkinnedMeshRenderer renderer = root.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (renderer == null)
                throw new InvalidOperationException("Sunbound Warden prefab has no SkinnedMeshRenderer.");

            renderer.sharedMesh = fixedMesh;
            EditorUtility.SetDirty(renderer);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ApplyFixedMeshToStage3Scene(Mesh fixedMesh)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject warden = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Where(t => t.name == "Sunbound Warden")
            .Select(t => t.gameObject)
            .FirstOrDefault();

        if (warden == null)
            throw new InvalidOperationException("Sunbound Warden was not found in Stage 3 scene.");

        SkinnedMeshRenderer renderer = warden.GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (renderer == null)
            throw new InvalidOperationException("Stage 3 Sunbound Warden has no SkinnedMeshRenderer.");

        renderer.sharedMesh = fixedMesh;
        PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        EditorUtility.SetDirty(renderer);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static float GetWeight(BoneWeight bw, int boneIndex)
    {
        float total = 0f;
        if (bw.boneIndex0 == boneIndex) total += bw.weight0;
        if (bw.boneIndex1 == boneIndex) total += bw.weight1;
        if (bw.boneIndex2 == boneIndex) total += bw.weight2;
        if (bw.boneIndex3 == boneIndex) total += bw.weight3;
        return total;
    }

    private static List<int>[] BuildAdjacency(int vertexCount, int[] triangles)
    {
        List<int>[] adjacency = new List<int>[vertexCount];
        for (int i = 0; i < vertexCount; i++)
            adjacency[i] = new List<int>();

        for (int i = 0; i + 2 < triangles.Length; i += 3)
        {
            AddEdge(adjacency, triangles[i], triangles[i + 1]);
            AddEdge(adjacency, triangles[i + 1], triangles[i + 2]);
            AddEdge(adjacency, triangles[i + 2], triangles[i]);
        }

        return adjacency;
    }

    private static void AddEdge(List<int>[] adjacency, int a, int b)
    {
        adjacency[a].Add(b);
        adjacency[b].Add(a);
    }

    private static List<List<int>> BuildComponents(List<int>[] adjacency)
    {
        bool[] visited = new bool[adjacency.Length];
        List<List<int>> components = new List<List<int>>();

        for (int start = 0; start < adjacency.Length; start++)
        {
            if (visited[start])
                continue;

            List<int> component = new List<int>();
            Stack<int> stack = new Stack<int>();
            stack.Push(start);
            visited[start] = true;

            while (stack.Count > 0)
            {
                int current = stack.Pop();
                component.Add(current);
                foreach (int next in adjacency[current])
                {
                    if (visited[next])
                        continue;
                    visited[next] = true;
                    stack.Push(next);
                }
            }

            components.Add(component);
        }

        return components;
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;

        string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
        string leaf = Path.GetFileName(folder);
        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf))
            throw new InvalidOperationException($"Invalid asset folder: {folder}");

        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
