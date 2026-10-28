#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class StoneWallLodPrefabBuilder
{
    private const string SourceGlbPath = "Assets/Maps/Optimized/level1_STAGE1_OPTIMIZED.glb";
    private const string OutputFolder = "Assets/Maps/Optimized/Prefabs";
    private const string OutputPrefabPath = OutputFolder + "/StoneWall_LOD.prefab";
    private const string StatusFilePath = "Temp/StoneWallLodBuildStatus.txt";

    private static readonly string[] SourceMeshNames =
    {
        "StoneWall_LOD1_SHARED",
        "StoneWall_LOD2_SHARED",
        "StoneWall_LOD3_SHARED",
    };

    // Provisional values for visual testing only. The final value is also the
    // culling threshold: below 5% screen-relative height, the wall is not drawn.
    private static readonly float[] TransitionHeights =
    {
        0.35f,
        0.15f,
        0.001f,
    };

    [MenuItem("SPELLSTRIKE/Optimization/3. Build Shared Stone Wall LOD Prefab %#&b")]
    public static void BuildSharedStoneWallPrefab()
    {
        WriteStatus("STARTED");

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            WriteStatus("FAILED: Editor is in or entering Play Mode.");
            Debug.LogError("[Stone Wall LOD] Exit Play Mode before building the wall prefab.");
            return;
        }

        EnsureOutputFolder();

        GameObject sourceRoot = AssetDatabase.LoadAssetAtPath<GameObject>(SourceGlbPath);
        if (sourceRoot == null)
        {
            WriteStatus($"FAILED: Could not load imported GLB: {SourceGlbPath}");
            Debug.LogError($"[Stone Wall LOD] Could not load imported GLB: {SourceGlbPath}");
            return;
        }

        UnityEngine.Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(SourceGlbPath);
        Mesh[] sourceMeshes = new Mesh[SourceMeshNames.Length];

        for (int i = 0; i < SourceMeshNames.Length; i++)
        {
            string meshName = SourceMeshNames[i];
            sourceMeshes[i] = subAssets
                .OfType<Mesh>()
                .FirstOrDefault(mesh => string.Equals(mesh.name, meshName, StringComparison.Ordinal));

            if (sourceMeshes[i] == null)
            {
                string availableMeshes = string.Join(", ", subAssets.OfType<Mesh>().Select(mesh => mesh.name).OrderBy(name => name));
                WriteStatus(
                    $"FAILED: Missing mesh subasset '{meshName}'.\n" +
                    $"Available mesh names:\n{availableMeshes}");
                Debug.LogError(
                    $"[Stone Wall LOD] Missing mesh subasset '{meshName}' in {SourceGlbPath}. " +
                    "No prefab was written.");
                return;
            }
        }

        GameObject tempRoot = new GameObject("StoneWall_LOD");

        try
        {
            LODGroup lodGroup = tempRoot.AddComponent<LODGroup>();
            lodGroup.fadeMode = LODFadeMode.None;
            lodGroup.animateCrossFading = false;

            var lods = new LOD[sourceMeshes.Length];

            for (int i = 0; i < sourceMeshes.Length; i++)
            {
                Mesh mesh = sourceMeshes[i];
                MeshRenderer sourceRenderer = FindSourceRenderer(sourceRoot, mesh);
                if (sourceRenderer == null)
                {
                    WriteStatus($"FAILED: Could not find a renderer using '{mesh.name}' in imported GLB.");
                    Debug.LogError(
                        $"[Stone Wall LOD] Could not find a renderer using '{mesh.name}' in the imported GLB. " +
                        "No prefab was written.");
                    return;
                }

                GameObject child = new GameObject($"LOD{i}_From_{SourceMeshNames[i]}");
                child.transform.SetParent(tempRoot.transform, false);

                MeshFilter meshFilter = child.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = mesh;

                MeshRenderer meshRenderer = child.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterials = sourceRenderer.sharedMaterials;
                meshRenderer.shadowCastingMode = sourceRenderer.shadowCastingMode;
                meshRenderer.receiveShadows = sourceRenderer.receiveShadows;
                meshRenderer.lightProbeUsage = sourceRenderer.lightProbeUsage;
                meshRenderer.reflectionProbeUsage = sourceRenderer.reflectionProbeUsage;

                lods[i] = new LOD(TransitionHeights[i], new Renderer[] { meshRenderer });
            }

            lodGroup.SetLODs(lods);
            lodGroup.RecalculateBounds();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(tempRoot, OutputPrefabPath);
            if (prefab == null)
            {
                WriteStatus($"FAILED: Unity failed to save prefab at {OutputPrefabPath}.");
                Debug.LogError($"[Stone Wall LOD] Unity failed to save prefab at {OutputPrefabPath}.");
                return;
            }

            AssetDatabase.SaveAssets();

            long lod1Triangles = CountTriangles(sourceMeshes[0]);
            long lod2Triangles = CountTriangles(sourceMeshes[1]);
            long lod3Triangles = CountTriangles(sourceMeshes[2]);

            WriteStatus(
                "SUCCESS\n" +
                $"Prefab: {OutputPrefabPath}\n" +
                $"LOD0/Rodin LOD1: {lod1Triangles:N0} tris\n" +
                $"LOD1/Rodin LOD2: {lod2Triangles:N0} tris\n" +
                $"LOD2/Rodin LOD3: {lod3Triangles:N0} tris\n" +
                $"Cull below: {TransitionHeights[2]:P0}");

            Debug.Log(
                "[Stone Wall LOD] SHARED WALL PREFAB BUILT\n" +
                $"Source: {SourceGlbPath}\n" +
                $"Prefab: {OutputPrefabPath}\n" +
                $"LOD0 (Rodin LOD1): {lod1Triangles:N0} tris @ {TransitionHeights[0]:P0}\n" +
                $"LOD1 (Rodin LOD2): {lod2Triangles:N0} tris @ {TransitionHeights[1]:P0}\n" +
                $"LOD2 (Rodin LOD3): {lod3Triangles:N0} tris @ {TransitionHeights[2]:P0}\n" +
                $"Cull below: {TransitionHeights[2]:P0} screen-relative height\n" +
                "All three prefab levels reference the GLB mesh/material subassets; no mesh copies were created.",
                prefab);

            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(tempRoot);
        }
    }

    private static MeshRenderer FindSourceRenderer(GameObject sourceRoot, Mesh mesh)
    {
        foreach (MeshFilter filter in sourceRoot.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter == null || filter.sharedMesh != mesh)
                continue;

            MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
            if (renderer != null)
                return renderer;
        }

        return null;
    }

    private static long CountTriangles(Mesh mesh)
    {
        long triangles = 0;
        for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            triangles += (long)mesh.GetIndexCount(subMesh) / 3L;

        return triangles;
    }

    private static void EnsureOutputFolder()
    {
        if (AssetDatabase.IsValidFolder(OutputFolder))
            return;

        const string optimizedFolder = "Assets/Maps/Optimized";
        AssetDatabase.CreateFolder(optimizedFolder, "Prefabs");
    }

    private static void WriteStatus(string text)
    {
        File.WriteAllText(StatusFilePath, text);
    }
}
#endif
