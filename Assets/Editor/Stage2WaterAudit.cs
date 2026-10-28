#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class Stage2WaterAudit
{
    private const string ScenePath = "Assets/Scenes/Stage2Scene/Stage2Scene.unity";
    private const string AutoRunFlag = "RecoveryBackups/RUN_STAGE2_WATER_AUDIT.flag";

    [InitializeOnLoadMethod]
    private static void AutoRunIfRequested()
    {
        EditorApplication.delayCall += () =>
        {
            string flagPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", AutoRunFlag));
            if (!File.Exists(flagPath))
                return;

            File.Delete(flagPath);
            Debug.Log("[Stage2WaterAudit] Auto-run flag detected. Starting audit.");
            Run();
        };
    }

    [MenuItem("SPELLSTRIKE/Stage 2/Audit Water Setup")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var sb = new StringBuilder();
        sb.AppendLine("STAGE2 WATER AUDIT");
        sb.AppendLine($"Scene={scene.path}");

        var allObjects = Resources.FindObjectsOfTypeAll<GameObject>()
            .Where(go => go.scene.IsValid() && go.scene.path == ScenePath)
            .ToArray();

        var meshColliders = allObjects.SelectMany(go => go.GetComponents<MeshCollider>()).ToArray();
        sb.AppendLine($"MeshColliders={meshColliders.Length}; enabled={meshColliders.Count(c => c.enabled)}");

        sb.AppendLine("\nROOTS");
        foreach (var root in scene.GetRootGameObjects().OrderBy(go => go.name))
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(r => !(r is ParticleSystemRenderer))
                .ToArray();
            if (renderers.Length == 0)
            {
                sb.AppendLine($"{root.name} | renderers=0 | pos={Fmt(root.transform.position)}");
                continue;
            }

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            sb.AppendLine($"{root.name} | renderers={renderers.Length} | boundsCenter={Fmt(b.center)} size={Fmt(b.size)} minY={b.min.y:F3} maxY={b.max.y:F3}");
        }

        sb.AppendLine("\nKEY OBJECTS");
        string[] keyNames = { "VAN AERT", "Node1", "Node2", "Node3", "Node4", "DrownedCaptain", "Main Camera", "Stage2_GlobalVolume" };
        foreach (var key in keyNames)
        {
            var matches = allObjects.Where(go => go.name.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0).Take(20).ToArray();
            foreach (var go in matches)
                sb.AppendLine($"{go.name} | active={go.activeInHierarchy} | pos={Fmt(go.transform.position)} | path={GetPath(go.transform)}");
        }

        sb.AppendLine("\nWATER-LIKE OBJECTS");
        string[] waterTerms = { "water", "ocean", "sea", "lake", "river", "shore", "foam", "caustic" };
        foreach (var go in allObjects.Where(go => waterTerms.Any(t => go.name.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0)).OrderBy(go => go.name))
            sb.AppendLine($"{go.name} | active={go.activeInHierarchy} | pos={Fmt(go.transform.position)} | path={GetPath(go.transform)}");

        sb.AppendLine("\nVOLUMES");
        foreach (var volume in allObjects.SelectMany(go => go.GetComponents<Volume>()))
        {
            string profilePath = volume.sharedProfile ? AssetDatabase.GetAssetPath(volume.sharedProfile) : "<none>";
            sb.AppendLine($"{volume.gameObject.name} | global={volume.isGlobal} weight={volume.weight:F2} priority={volume.priority:F2} | profile={profilePath}");
        }

        sb.AppendLine("\nLARGEST STATIC/MESH RENDERERS");
        var largest = allObjects.SelectMany(go => go.GetComponents<Renderer>())
            .Where(r => !(r is ParticleSystemRenderer))
            .Select(r => new { R = r, Score = r.bounds.size.x * r.bounds.size.z })
            .OrderByDescending(x => x.Score)
            .Take(80);
        foreach (var x in largest)
        {
            Bounds b = x.R.bounds;
            sb.AppendLine($"{x.R.gameObject.name} | {x.R.GetType().Name} | center={Fmt(b.center)} size={Fmt(b.size)} minY={b.min.y:F3} maxY={b.max.y:F3} | path={GetPath(x.R.transform)}");
        }

        string reportPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "RecoveryBackups", "Stage2WaterAudit_20260918.txt"));
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
        File.WriteAllText(reportPath, sb.ToString());
        Debug.Log($"[Stage2WaterAudit] Wrote {reportPath}\n{sb}");
    }

    private static string Fmt(Vector3 v) => $"({v.x:F3},{v.y:F3},{v.z:F3})";

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

