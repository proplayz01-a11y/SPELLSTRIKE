using System;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class Stage3NavMeshSetupMenu
{
    private const string ScenePath = "Assets/Scenes/Stage3Scene/Stage3Scene.unity";
    private const string MapRootName = "STAGE3MAP";
    private const string SurfaceObjectName = "Plane.001";
    private const string DataFolder = "Assets/Scenes/Stage3Scene/Stage3Scene";
    private const string DataPath = DataFolder + "/NavMesh-Plane.001.asset";
    private const string PreviousRootDataPath = DataFolder + "/NavMesh-STAGE3MAP.asset";
    private const string LegacyPlaneDataPath = DataFolder + "/NavMesh-Plane.asset";

    [MenuItem("Tools/SPELLSTRIKE/Stage 3/Bake Stage 3 NavMesh")]
    public static void Bake()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
        {
            throw new InvalidOperationException(
                $"Open '{ScenePath}' before baking Stage 3 NavMesh. " +
                "This guard prevents overwriting another dirty scene.");
        }

        GameObject mapRoot = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Where(t => t != null && t.name == MapRootName)
            .Select(t => t.gameObject)
            .FirstOrDefault();

        if (mapRoot == null)
            throw new InvalidOperationException($"Could not find '{MapRootName}' in Stage3Scene.");

        GameObject surfaceObject = mapRoot.GetComponentsInChildren<Transform>(true)
            .Where(t => t != null && t.name == SurfaceObjectName)
            .Select(t => t.gameObject)
            .FirstOrDefault();

        if (surfaceObject == null)
        {
            throw new InvalidOperationException(
                $"Could not find '{SurfaceObjectName}' under active '{MapRootName}'.");
        }

        NavMeshSurface surface = surfaceObject.GetComponent<NavMeshSurface>();
        if (surface == null)
            surface = Undo.AddComponent<NavMeshSurface>(surfaceObject);

        RemoveOtherActiveMapSurfaces(mapRoot, surface);

        surface.agentTypeID = 0;
        surface.collectObjects = CollectObjects.Children;
        surface.layerMask = ~0;
        // Stage 3's imported map only has a small set of authored physics colliders,
        // so collider-only collection produces tiny disconnected NavMesh islands.
        // Stage 1/2 both bake navigation from render meshes; use the same proven
        // pattern here while keeping the existing physics colliders untouched.
        surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
        surface.defaultArea = 0;
        surface.ignoreNavMeshAgent = true;
        surface.ignoreNavMeshObstacle = true;
        surface.overrideTileSize = false;
        surface.overrideVoxelSize = false;
        surface.minRegionArea = 2f;
        surface.buildHeightMesh = false;

        NavMeshData existingAsset = AssetDatabase.LoadAssetAtPath<NavMeshData>(DataPath);
        surface.BuildNavMesh();
        NavMeshData builtData = surface.navMeshData;
        if (builtData == null)
            throw new InvalidOperationException("Stage 3 NavMesh bake returned no NavMeshData.");

        Directory.CreateDirectory(DataFolder);

        if (existingAsset == null)
        {
            AssetDatabase.CreateAsset(builtData, DataPath);
            surface.navMeshData = builtData;
        }
        else if (builtData != existingAsset)
        {
            surface.RemoveData();
            EditorUtility.CopySerialized(builtData, existingAsset);
            existingAsset.name = SurfaceObjectName;
            surface.navMeshData = existingAsset;
            surface.AddData();
            UnityEngine.Object.DestroyImmediate(builtData);
            EditorUtility.SetDirty(existingAsset);
        }

        EditorUtility.SetDirty(surface);
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene);
        RemoveObsoleteDataAssets();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
        if (triangulation.vertices.Length == 0 || triangulation.indices.Length == 0)
            throw new InvalidOperationException("Stage 3 NavMesh bake produced an empty triangulation.");

        GameObject warden = GameObject.Find("Sunbound Warden");
        if (warden == null)
            throw new InvalidOperationException("Sunbound Warden is missing from Stage3Scene.");

        if (!NavMesh.SamplePosition(warden.transform.position, out NavMeshHit wardenHit, 6f, NavMesh.AllAreas))
        {
            throw new InvalidOperationException(
                $"Baked Stage 3 NavMesh does not reach the Warden spawn at {warden.transform.position:F3}.");
        }

        string assetGuid = AssetDatabase.AssetPathToGUID(DataPath);
        Debug.Log(
            $"[Stage3NavMeshBake] COMPLETE surface='{surface.gameObject.name}' " +
            $"asset='{DataPath}' guid={assetGuid} vertices={triangulation.vertices.Length} " +
            $"indices={triangulation.indices.Length} wardenAuthored={warden.transform.position:F3} " +
            $"wardenNav={wardenHit.position:F3} yDelta={(wardenHit.position.y - warden.transform.position.y):0.000}.",
            surface);
    }

    private static void RemoveOtherActiveMapSurfaces(GameObject mapRoot, NavMeshSurface keepSurface)
    {
        foreach (NavMeshSurface candidate in mapRoot.GetComponentsInChildren<NavMeshSurface>(true))
        {
            if (candidate == null || candidate == keepSurface)
                continue;

            string dataPath = candidate.navMeshData != null
                ? AssetDatabase.GetAssetPath(candidate.navMeshData)
                : string.Empty;

            Debug.Log(
                $"[Stage3NavMeshBake] Removing active-map surface '{candidate.gameObject.name}' " +
                $"before moving bake ownership to '{SurfaceObjectName}'. data='{dataPath}'.",
                candidate);

            candidate.RemoveData();
            Undo.DestroyObjectImmediate(candidate);
        }
    }

    private static void RemoveObsoleteDataAssets()
    {
        foreach (string obsoletePath in new[] { PreviousRootDataPath, LegacyPlaneDataPath })
        {
            if (string.Equals(obsoletePath, DataPath, StringComparison.OrdinalIgnoreCase))
                continue;

            if (AssetDatabase.LoadAssetAtPath<NavMeshData>(obsoletePath) != null)
            {
                Debug.Log($"[Stage3NavMeshBake] Removing obsolete NavMeshData '{obsoletePath}'.");
                AssetDatabase.DeleteAsset(obsoletePath);
            }
        }
    }
}
