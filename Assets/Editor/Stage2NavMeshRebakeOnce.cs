using System;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class Stage2NavMeshRebakeOnce
{
    private const string ScenePath = "Assets/Scenes/Stage2Scene/Stage2Scene.unity";
    private const string TriggerPath = "Stage2NavMeshRebake.trigger";

    static Stage2NavMeshRebakeOnce()
    {
        if (!File.Exists(TriggerPath))
            return;

        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(TriggerPath))
                return;

            File.Delete(TriggerPath);
            Run();
        };
    }

    [MenuItem("Tools/SPELLSTRIKE/Rebake Stage 2 NavMesh")]
    public static void RunFromMenu() => Run();

    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var surfaces = UnityEngine.Object
            .FindObjectsByType<NavMeshSurface>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(s => s != null && (s.gameObject.name == "Plane.002" || s.gameObject.name == "STAGE2MAP"))
            .OrderBy(s => s.gameObject.name)
            .ToArray();

        if (surfaces.Length != 2)
            throw new InvalidOperationException($"Expected exactly 2 Stage 2 NavMeshSurface components, found {surfaces.Length}.");

        foreach (var surface in surfaces)
        {
            var existingAsset = surface.navMeshData;
            if (existingAsset == null)
                throw new InvalidOperationException($"Surface '{surface.gameObject.name}' has no existing NavMeshData asset.");

            var existingPath = AssetDatabase.GetAssetPath(existingAsset);
            if (string.IsNullOrWhiteSpace(existingPath))
                throw new InvalidOperationException($"Surface '{surface.gameObject.name}' NavMeshData is not a saved asset.");

            var existingGuid = AssetDatabase.AssetPathToGUID(existingPath);
            Debug.Log($"[Stage2NavMeshRebake] Rebuilding '{surface.gameObject.name}' into existing asset '{existingPath}' guid={existingGuid}.");

            surface.BuildNavMesh();

            var rebuiltData = surface.navMeshData;
            if (rebuiltData == null)
                throw new InvalidOperationException($"BuildNavMesh returned no data for '{surface.gameObject.name}'.");

            // Preserve the existing asset object/GUID so the serialized scene reference
            // remains stable. Copy the freshly baked native data into that asset.
            surface.RemoveData();
            EditorUtility.CopySerialized(rebuiltData, existingAsset);
            existingAsset.name = rebuiltData.name;
            surface.navMeshData = existingAsset;
            surface.AddData();

            EditorUtility.SetDirty(existingAsset);
            EditorUtility.SetDirty(surface);
            UnityEngine.Object.DestroyImmediate(rebuiltData);

            Debug.Log($"[Stage2NavMeshRebake] Rebuilt '{surface.gameObject.name}' and preserved guid={existingGuid}.");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        foreach (var surface in surfaces)
        {
            var path = AssetDatabase.GetAssetPath(surface.navMeshData);
            var guid = AssetDatabase.AssetPathToGUID(path);
            Debug.Log($"[Stage2NavMeshRebake] FINAL surface='{surface.gameObject.name}' asset='{path}' guid={guid}.");
        }

        Debug.Log("[Stage2NavMeshRebake] COMPLETE");
    }
}
