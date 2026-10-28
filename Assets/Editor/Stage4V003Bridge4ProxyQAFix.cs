// Bounded QA-only collision hypothesis for Convergence Citadel v003 Bridge 04.
// Invoked from the *visible* Unity Editor menu in Edit Mode. NEVER changes the
// imported GLB, the original boss prefabs or pre-existing gameplay scenes.
#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Stage4V003Bridge4ProxyQAFix
{
    private const string QaScene = "Assets/Scenes/Stage4Scene/Stage4Scene_ConvergenceCitadel_v003_RuntimeQA.unity";
    private const string MeshAsset = "Assets/Stage 4/ConvergenceCitadel_v003/Navigation/Bridge04_SmoothCollision_QA.mesh.asset";
    private const string Backup = "Library/Stage4V003Bridge04CollisionQA_20261020/RuntimeQA.before_bridge04_proxy.unity";

    [MenuItem("Stage 4/Convergence Citadel v003/TEST Bridge04 Smooth Collision Proxy (RuntimeQA Only)")]
    public static void AddQAOnlyProxy()
    {
        if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[Bridge04 QA] Cannot edit scene while playing.");
            return;
        }
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != QaScene || scene.isDirty || !File.Exists(QaScene) || File.Exists(MeshAsset) || File.Exists(Backup))
        {
            Debug.LogError("[Bridge04 QA] Must have CLEAN, active Stage4 RuntimeQA scene and fresh QA asset/backup targets. No changes made.");
            return;
        }
        var roots = scene.GetRootGameObjects();
        var physicsRoot = roots.SelectMany(go => go.GetComponentsInChildren<Transform>(true))
            .Single(t => t.name == "Independent_StaticMeshColliders");
        var original = physicsRoot.GetComponentsInChildren<MeshCollider>(true)
            .Single(c => c.name == "COL_Bridge_04_Deck_PHYSICS");
        if (original.sharedMesh == null || !original.enabled || physicsRoot.GetComponentsInChildren<Transform>(true).Any(t => t.name == "QA_Bridge04_Smoothed_WalkSurface"))
        {
            Debug.LogError("[Bridge04 QA] Existing collider or QA object does not match expected pristine state.");
            return;
        }
        Directory.CreateDirectory(Path.GetDirectoryName(Backup));
        File.Copy(QaScene, Backup);
        try
        {
            Mesh mesh = CreateAuthorProfileMesh();
            AssetDatabase.CreateAsset(mesh, MeshAsset);
            var proxy = new GameObject("QA_Bridge04_Smoothed_WalkSurface");
            proxy.transform.SetParent(physicsRoot, false);
            proxy.isStatic = true;
            var collider = proxy.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            collider.convex = false;
            collider.isTrigger = false;
            original.enabled = false; // QA only. The original GLB remains unmodified.
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, QaScene))
                throw new IOException("Failed to save RuntimeQA scene with smooth proxy");
            Debug.Log("[Bridge04 QA] Authored smooth, closed 30.72-unit-wide walkable collision proxy from the EXACT imported bridge profile; disabled ONLY original bridge deck MeshCollider in RuntimeQA. No production scene changed. Backup=" + Backup);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError("[Bridge04 QA] Incomplete hypothesis. Do NOT rerun or promote. Original RuntimeQA backup remains at " + Backup);
        }
    }

    private static Mesh CreateAuthorProfileMesh()
    {
        // Manifest Bridge_Node3_Boss: t=[0,.5190722,.7033873,1]
        // GLB in Unity left-handed world X=-X, Z=-Y, imported deck top is authored
        // profile elevation less the existing visual deck thickness (3 units).
        Vector3[] centers =
        {
            new Vector3(-1551.928711f,15f,-752.289643f),
            new Vector3(-1714.233948f,15f,-831.248932f),
            new Vector3(-1771.866211f,27f,-859.286316f),
            new Vector3(-1864.612061f,27f,-904.405884f),
        };
        Vector3 forward = new Vector3(centers[3].x - centers[0].x,0f,centers[3].z - centers[0].z).normalized;
        Vector3 sideways = new Vector3(-forward.z,0f,forward.x);
        const float clearWidth = 30.72f;
        const float thickness = 2.5f;
        var vertices = new Vector3[centers.Length * 4];
        for (int i = 0; i < centers.Length; ++i)
        {
            vertices[i * 4] = centers[i] - sideways * (clearWidth / 2);
            vertices[i * 4 + 1] = centers[i] + sideways * (clearWidth / 2);
            vertices[i * 4 + 2] = vertices[i * 4] - Vector3.up * thickness;
            vertices[i * 4 + 3] = vertices[i * 4 + 1] - Vector3.up * thickness;
        }
        var indices = new System.Collections.Generic.List<int>();
        for (int i = 0; i < centers.Length - 1; ++i)
        {
            int a=i*4, b=(i+1)*4;
            // Closed ramp with top/bottom and left/right side walls.
            AddQuad(indices,a,a+1,b+1,b);
            AddQuad(indices,a+2,b+2,b+3,a+3);
            AddQuad(indices,a,b,b+2,a+2);
            AddQuad(indices,a+1,a+3,b+3,b+1);
        }
        AddQuad(indices,0,2,3,1);
        int last=(centers.Length-1)*4;
        AddQuad(indices,last,last+1,last+3,last+2);
        var mesh=new Mesh { name="Bridge04_SmoothCollision_QA_AuthoredProfile_30p72_ClearWidth" };
        mesh.vertices=vertices;
        mesh.triangles=indices.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void AddQuad(System.Collections.Generic.List<int> a,int x,int y,int z,int w)
    {
        a.Add(x);a.Add(y);a.Add(z);a.Add(x);a.Add(z);a.Add(w);
    }
}
#endif
