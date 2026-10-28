#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class Stage1GroundColliderFixOnce
{
    private const string Marker = "Temp/Stage1GroundColliderFix.trigger";
    private const string ScenePath = "Assets/Scenes/OptimizationTests/Stage1_OptimizedMap_Test.unity";
    private const string MapRootName = "level1_STAGE1_OPTIMIZED_TEST";
    private const string GroundObjectName = "Plane.002";

    static Stage1GroundColliderFixOnce()
    {
        EditorApplication.delayCall += TryRun;
    }

    private static void TryRun()
    {
        if (!File.Exists(Marker)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += TryRun;
            return;
        }

        File.Delete(Marker);
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Transform mapRoot = scene.GetRootGameObjects()
            .Select(r => r.transform)
            .FirstOrDefault(t => t.name == MapRootName);
        if (mapRoot == null)
        {
            Debug.LogError($"[Stage1 Ground Fix] Map root '{MapRootName}' not found.");
            return;
        }

        Transform ground = mapRoot.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.name == GroundObjectName);
        if (ground == null)
        {
            Debug.LogError($"[Stage1 Ground Fix] Ground object '{GroundObjectName}' not found under '{MapRootName}'.");
            return;
        }

        MeshFilter mf = ground.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
        {
            Debug.LogError($"[Stage1 Ground Fix] '{GroundObjectName}' has no usable MeshFilter/sharedMesh.");
            return;
        }

        MeshCollider mc = ground.GetComponent<MeshCollider>();
        bool added = false;
        if (mc == null)
        {
            mc = ground.gameObject.AddComponent<MeshCollider>();
            added = true;
        }
        mc.sharedMesh = mf.sharedMesh;
        mc.convex = false;
        mc.isTrigger = false;
        mc.enabled = true;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Physics.SyncTransforms();

        GameObject player = scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
            .Select(t => t.gameObject)
            .FirstOrDefault(go => go.name == "VAN AERT");

        bool verified = false;
        if (player != null)
        {
            Vector3 p = player.transform.position;
            RaycastHit[] hits = Physics.RaycastAll(p + Vector3.up * 20f, Vector3.down, 500f, ~0, QueryTriggerInteraction.Ignore)
                .OrderBy(h => h.distance)
                .ToArray();
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == mc)
                {
                    verified = true;
                    Debug.Log($"[Stage1 Ground Fix] Verified player-down ray hits '{GroundObjectName}' at y={hit.point.y:F3}, distance={hit.distance:F3}.");
                    break;
                }
            }
        }

        Debug.Log($"[Stage1 Ground Fix] COMPLETE added={added} mesh='{mf.sharedMesh.name}' verifiedRayHit={verified} scene='{ScenePath}'");
    }
}
#endif
