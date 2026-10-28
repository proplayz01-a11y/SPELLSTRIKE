#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class Stage1GroundDiagnosticOnce
{
    private const string Marker = "Temp/Stage1GroundDiagnostic.trigger";
    private const string ScenePath = "Assets/Scenes/OptimizationTests/Stage1_OptimizedMap_Test.unity";

    static Stage1GroundDiagnosticOnce()
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
        GameObject player = scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
            .Select(t => t.gameObject)
            .FirstOrDefault(go => go.name == "VAN AERT");

        if (player == null)
        {
            Debug.LogError("[Stage1 Ground Diagnostic] VAN AERT not found.");
            return;
        }

        Physics.SyncTransforms();
        Vector3 p = player.transform.position;
        Debug.Log($"[Stage1 Ground Diagnostic] Player world position={p}");

        RaycastHit[] hits = Physics.RaycastAll(p + Vector3.up * 20f, Vector3.down, 500f, ~0, QueryTriggerInteraction.Ignore)
            .OrderBy(h => h.distance)
            .ToArray();
        foreach (RaycastHit hit in hits.Take(20))
            Debug.Log($"[Stage1 Ground Diagnostic] RayHit go='{hit.collider.gameObject.name}' type={hit.collider.GetType().Name} y={hit.point.y:F3} distance={hit.distance:F3}");
        if (hits.Length == 0)
            Debug.LogWarning("[Stage1 Ground Diagnostic] No collider found below player within 500 units.");

        var candidates = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(r => r != null && r.bounds.Contains(new Vector3(p.x, r.bounds.center.y, p.z)))
            .OrderBy(r => Mathf.Abs(r.bounds.max.y - p.y))
            .Take(30)
            .ToArray();

        foreach (MeshRenderer r in candidates)
        {
            MeshFilter mf = r.GetComponent<MeshFilter>();
            Collider c = r.GetComponent<Collider>();
            Debug.Log($"[Stage1 Ground Diagnostic] Candidate '{r.gameObject.name}' path='{GetPath(r.transform)}' boundsMin={r.bounds.min} boundsMax={r.bounds.max} mesh={(mf && mf.sharedMesh ? mf.sharedMesh.name : "<none>")} collider={(c ? c.GetType().Name : "<none>")}");
        }

        Debug.Log($"[Stage1 Ground Diagnostic] COMPLETE hits={hits.Length} candidates={candidates.Length}");
    }

    private static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
#endif
