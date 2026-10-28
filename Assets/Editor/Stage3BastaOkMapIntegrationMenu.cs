using System;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class Stage3BastaOkMapIntegrationMenu
{
    private const string ScenePath = "Assets/Scenes/Stage3Scene/Stage3Scene.unity";
    private const string NewMapAssetPath = "Assets/Stage 3/Map/STAGE3MAP_REPLACEMENT.glb";
    private const string MapName = "STAGE3MAP";
    private const string BackupName = "_STAGE3MAP_OLD_DISABLED";
    private const string EnvironmentName = "_Environment";
    [MenuItem("SPELLSTRIKE/Stage 3/Replace Map With BASTA OK + Bake")]
    public static void InstallAndBake()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
            throw new InvalidOperationException($"Open '{ScenePath}' before replacing the Stage 3 map.");

        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(NewMapAssetPath);
        if (asset == null)
            throw new InvalidOperationException($"Could not load imported map asset '{NewMapAssetPath}'.");

        Transform environment = FindNamedTransform(scene, EnvironmentName);
        if (environment == null)
            throw new InvalidOperationException($"Could not find '{EnvironmentName}' in Stage3Scene.");

        GameObject currentMap = FindNamedTransform(scene, MapName)?.gameObject;
        if (currentMap == null)
            throw new InvalidOperationException($"Could not find existing '{MapName}' to replace.");

        string currentAssetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(currentMap);
        bool alreadyInstalled = string.Equals(
            currentAssetPath,
            NewMapAssetPath,
            StringComparison.OrdinalIgnoreCase);

        GameObject newMap = currentMap;
        if (!alreadyInstalled)
        {
            GameObject stalePending = FindNamedTransform(scene, "_STAGE3MAP_REPLACEMENT_PENDING")?.gameObject;
            if (stalePending != null)
                Undo.DestroyObjectImmediate(stalePending);

            newMap = PrefabUtility.InstantiatePrefab(asset, scene) as GameObject;
            if (newMap == null)
                throw new InvalidOperationException("Unity could not instantiate the replacement Stage 3 GLB.");

            Undo.RegisterCreatedObjectUndo(newMap, "Install Stage 3 replacement map");
            newMap.name = "_STAGE3MAP_REPLACEMENT_PENDING";
            newMap.transform.SetParent(environment, false);
            newMap.transform.localPosition = Vector3.zero;
            newMap.transform.localRotation = Quaternion.identity;
            newMap.transform.localScale = Vector3.one;

            RemoveImportedNonEnvironmentComponents(newMap);
            ValidateImportedGeometry(newMap);
            LogAuthoredAnchorDeltas(scene, newMap);

            GameObject existingBackup = FindNamedTransform(scene, BackupName)?.gameObject;
            if (existingBackup != null && existingBackup != currentMap)
                existingBackup.name = BackupName + "_PREVIOUS";

            currentMap.name = BackupName;
            currentMap.SetActive(false);

            newMap.name = MapName;
            newMap.SetActive(true);
        }

        RemoveImportedNonEnvironmentComponents(newMap);
        int colliderCount = EnsureEnvironmentColliders(newMap);
        int meshCount = newMap.GetComponentsInChildren<MeshFilter>(true)
            .Count(filter => filter != null && filter.sharedMesh != null);

        EditorUtility.SetDirty(newMap);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Stage3NavMeshSetupMenu.Bake();

        ValidateBakedNavMesh(scene);

        string guid = AssetDatabase.AssetPathToGUID(NewMapAssetPath);
        Debug.Log(
            $"[Stage3MapReplace] PASS new Stage 3 map installed from '{NewMapAssetPath}' " +
            $"guid={guid}. meshes={meshCount} meshColliders={colliderCount}. " +
            $"Old map retained inactive as '{BackupName}'. Stage 3 NavMesh rebaked successfully.",
            newMap);
    }

    private static void LogAuthoredAnchorDeltas(Scene scene, GameObject newMap)
    {
        LogAnchorDelta(scene, newMap, "Node 2", "CT_Node_Foot");
        LogAnchorDelta(scene, newMap, "Node 3", "MNA_Altar_Bottom");
        LogAnchorDelta(scene, newMap, "Node 4", "Boss_Stage");
    }

    private static void RemoveImportedNonEnvironmentComponents(GameObject root)
    {
        foreach (Camera camera in root.GetComponentsInChildren<Camera>(true))
            Undo.DestroyObjectImmediate(camera);

        foreach (Light light in root.GetComponentsInChildren<Light>(true))
            Undo.DestroyObjectImmediate(light);

        foreach (AudioListener listener in root.GetComponentsInChildren<AudioListener>(true))
            Undo.DestroyObjectImmediate(listener);
    }

    private static void ValidateImportedGeometry(GameObject root)
    {
        MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
        int meshCount = filters.Count(filter => filter != null && filter.sharedMesh != null);
        if (meshCount < 150)
        {
            throw new InvalidOperationException(
                $"Replacement Stage 3 map imported only {meshCount} meshes; expected roughly 184. " +
                "Aborting before the old map is disabled.");
        }
    }

    private static void LogAnchorDelta(
        Scene scene,
        GameObject newMap,
        string nodeName,
        string mapAnchorName)
    {
        Transform node = FindNamedTransform(scene, nodeName);
        Transform anchor = newMap.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t != null && t.name == mapAnchorName);

        if (node == null || anchor == null)
        {
            Debug.LogWarning(
                $"[Stage3MapReplace] Could not compare authored anchor '{mapAnchorName}' with '{nodeName}'.");
            return;
        }

        Vector2 nodeXZ = new Vector2(node.position.x, node.position.z);
        Vector2 anchorXZ = new Vector2(anchor.position.x, anchor.position.z);
        float horizontalDistance = Vector2.Distance(nodeXZ, anchorXZ);

        Debug.Log(
            $"[Stage3MapReplace] Authored anchor delta {nodeName} <-> {mapAnchorName}: " +
            $"{horizontalDistance:0.000}m. Current gameplay-node positions are preserved; " +
            "fresh baked NavMesh coverage is authoritative.");
    }

    private static int EnsureEnvironmentColliders(GameObject mapRoot)
    {
        int colliderCount = 0;
        foreach (MeshFilter filter in mapRoot.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter == null || filter.sharedMesh == null)
                continue;

            GameObject go = filter.gameObject;
            go.layer = LayerMask.NameToLayer("Default");

            MeshCollider collider = go.GetComponent<MeshCollider>();
            if (collider == null)
                collider = Undo.AddComponent<MeshCollider>(go);

            collider.sharedMesh = filter.sharedMesh;
            collider.convex = false;
            collider.enabled = true;
            colliderCount++;
            EditorUtility.SetDirty(collider);
        }

        return colliderCount;
    }

    private static void ValidateBakedNavMesh(Scene scene)
    {
        foreach (string nodeName in new[] { "Node 1", "Node 2", "Node 3", "Node 4" })
        {
            Transform node = FindNamedTransform(scene, nodeName);
            if (node == null)
                throw new InvalidOperationException($"Missing '{nodeName}' after Stage 3 map replacement.");

            if (!NavMesh.SamplePosition(node.position, out NavMeshHit hit, 12f, NavMesh.AllAreas))
            {
                throw new InvalidOperationException(
                    $"New Stage 3 NavMesh does not reach '{nodeName}' near {node.position:F3}.");
            }

            Debug.Log(
                $"[Stage3MapReplace] NavMesh PASS {nodeName} authored={node.position:F3} " +
                $"sample={hit.position:F3} distance={Vector3.Distance(node.position, hit.position):0.000}m.");
        }
    }

    private static Transform FindNamedTransform(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t != null && t.name == objectName);
            if (found != null)
                return found;
        }

        return null;
    }
}
