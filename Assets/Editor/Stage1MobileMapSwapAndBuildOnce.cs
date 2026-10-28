#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class Stage1MobileMapSwapAndBuildOnce
{
    private const string Marker = "Temp/Stage1MobileMapSwapAndBuild.trigger";
    private const string ScenePath = "Assets/Scenes/OptimizationTests/Stage1_OptimizedMap_Test.unity";
    private const string MobileMapPath = "Assets/Maps/Optimized/level1_STAGE1_MOBILE.glb";
    private const string OldRootName = "level1_STAGE1_OPTIMIZED_TEST";
    private const string NewRootName = "level1_STAGE1_MOBILE_TEST";

    static Stage1MobileMapSwapAndBuildOnce() => EditorApplication.update += TryRun;

    private static void TryRun()
    {
        if (!File.Exists(Path.GetFullPath(Marker))) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        File.Delete(Path.GetFullPath(Marker));
        AssetDatabase.ImportAsset(MobileMapPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        GameObject mobilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MobileMapPath);
        if (mobilePrefab == null) throw new InvalidOperationException("Mobile GLB did not import as a GameObject.");

        string backup = Path.GetFullPath("Temp/Stage1_OptimizedMap_Test_before_mobile_swap.unity");
        File.Copy(Path.GetFullPath(ScenePath), backup, true);

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject oldRoot = scene.GetRootGameObjects().FirstOrDefault(go => go.name == OldRootName);
        if (oldRoot == null) throw new InvalidOperationException($"Expected old map root '{OldRootName}' not found.");

        // Clean up anything left by an interrupted/failed previous attempt. The
        // on-disk test scene still has the old root, so these are safe transient
        // objects only and make the one-shot rerunnable.
        foreach (GameObject stale in scene.GetRootGameObjects().Where(go => go.name == NewRootName).ToArray())
            UnityEngine.Object.DestroyImmediate(stale);

        Transform oldGround = oldRoot.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Plane.002");
        bool oldHadGroundCollider = oldGround != null && oldGround.GetComponent<MeshCollider>() != null;

        Vector3 pos = oldRoot.transform.position;
        Quaternion rot = oldRoot.transform.rotation;
        Vector3 scale = oldRoot.transform.localScale;
        int sibling = oldRoot.transform.GetSiblingIndex();
        bool active = oldRoot.activeSelf;

        GameObject newRoot = (GameObject)PrefabUtility.InstantiatePrefab(mobilePrefab, scene);
        newRoot.name = NewRootName;
        newRoot.transform.SetPositionAndRotation(pos, rot);
        newRoot.transform.localScale = scale;
        newRoot.transform.SetSiblingIndex(sibling);
        newRoot.SetActive(active);

        Transform newGround = newRoot.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Plane.002");
        if (newGround == null) throw new InvalidOperationException("Mobile map has no Plane.002 ground object.");
        MeshFilter mf = newGround.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) throw new InvalidOperationException("Mobile Plane.002 has no mesh.");

        // Ground collider is intentionally user-managed. Do not add, remove,
        // replace, or reconfigure it here.

        UnityEngine.Object.DestroyImmediate(oldRoot);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        string[] deps = AssetDatabase.GetDependencies(ScenePath, true);
        bool mobileDependency = deps.Contains(MobileMapPath);
        bool oldDependency = deps.Contains("Assets/Maps/Optimized/level1_STAGE1_OPTIMIZED.glb");
        Debug.Log($"[Stage1 Mobile Swap] COMPLETE oldGroundCollider={oldHadGroundCollider} colliderPreserved=True mobileDependency={mobileDependency} oldMapDependency={oldDependency} backup='{backup}'");
        if (!mobileDependency) throw new InvalidOperationException("Scene dependency verification failed: mobile map is not a dependency.");
        if (oldDependency)
        {
            Debug.LogWarning("[Stage1 Mobile Swap] Old optimized GLB remains referenced after mobile root swap. Build deferred until the remaining helper references are identified/remapped.");
            return;
        }

        TemporaryAndroidBuildRunner.BuildStage1OptimizedTest();
    }

}
#endif
