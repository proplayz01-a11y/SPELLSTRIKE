#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// One-time, backed-up lighting readability pass for the three isolated
/// low-poly defense cinematic scenes. It NEVER touches gameplay, Library or
/// StoryScene, and never changes Build Settings.
/// </summary>
internal static class PrologueDefenseSceneLightingQA
{
    private const string SceneFolder="Assets/Scenes/Cinematics/";
    private const string ProofFolder="Library/StoryboardDefenseSets_20261023/";
    private const string RootName="DEFENSE_CINEMATIC_FILL__NO_SHADOWS";

    private static readonly string[] SceneNames =
    {
        "Prologue_P01_FamilyHome_Defense_v01",
        "Prologue_P02_P03_GreatWar_Defense_v01",
        "Prologue_P04_RuinedHome_Defense_v01"
    };
    [Serializable]
    private class Report
    {
        public string scene;
        public int meshes;
        public int triangles;
        public int missingScripts;
        public int missingMaterials;
        public int cameras;
        public int lights;
        public bool hasPrimarySet;
        public bool saved;
        public bool passed;
    }
    [Serializable]
    private class ReportCollection
    {
        public Report[] scenes;
        public bool allPass;
        public string originalSceneRestored;
    }

    [MenuItem("SPELLSTRIKE/Cinematics/Defense Prototype/2 Illuminate and Validate 3 Imported Sets")]
    private static void Apply()
    {
        var original=SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            SceneManager.sceneCount!=1 || !original.isLoaded ||
            original.isDirty || string.IsNullOrEmpty(original.path))
        {
            Debug.LogError("[Defense QA] Save the original Editor scene in Edit Mode first.");
            return;
        }
        foreach(var name in SceneNames)
        {
            var path=SceneFolder+name+".unity";
            var backup=ProofFolder+name+"_BEFORE_FillLight.unity";
            if(!File.Exists(path) || File.Exists(backup))
            {
                Debug.LogError("[Defense QA] Scene missing or backup already exists: "+name);
                return;
            }
        }
        string originalPath=original.path;
        var results=new Report[SceneNames.Length];
        try
        {
            Directory.CreateDirectory(ProofFolder);
            for(int i=0;i<SceneNames.Length;i++)
            {
                string name=SceneNames[i];
                string path=SceneFolder+name+".unity";
                File.Copy(path,ProofFolder+name+"_BEFORE_FillLight.unity",false);
                var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
                var roots=scene.GetRootGameObjects();
                if (roots.Any(go=>go.name==RootName) ||
                    !roots.Any(go=>go.name.StartsWith("BLENDER_EXPORT__",
                                                    StringComparison.Ordinal)))
                    throw new InvalidOperationException("Unexpected scene layout: "+path);
                var fill=new GameObject(RootName);
                fill.transform.rotation=Quaternion.Euler(43f,132f,0);
                var directional=fill.AddComponent<Light>();
                directional.type=LightType.Directional;
                directional.color=i==0
                    ? new Color(1f,.82f,.65f)
                    : i==1 ? new Color(.71f,.73f,.95f)
                    : new Color(.75f,.81f,.94f);
                directional.intensity=i==0 ? 1.10f : i==1 ? .85f : 1.55f;
                directional.shadows=LightShadows.None;
                RenderSettings.ambientMode=AmbientMode.Flat;
                RenderSettings.ambientLight=i==0
                    ? new Color(.33f,.28f,.23f)
                    : i==1 ? new Color(.22f,.21f,.27f)
                    : new Color(.34f,.34f,.40f);

                foreach(var cam in roots.SelectMany(go=>go.GetComponentsInChildren<Camera>(true)))
                {
                    cam.clearFlags=CameraClearFlags.SolidColor;
                    cam.backgroundColor=i==0
                        ? new Color(.24f,.27f,.29f)
                        : i==1 ? new Color(.16f,.14f,.22f)
                        : new Color(.19f,.21f,.25f);
                }
                EditorSceneManager.MarkSceneDirty(scene);
                var report=Audit(scene);
                report.saved=EditorSceneManager.SaveScene(scene,path);
                report.passed=report.saved && report.hasPrimarySet &&
                    report.meshes>60 && report.triangles>1200 &&
                    report.cameras>=1 && report.lights>=2 &&
                    report.missingScripts==0 && report.missingMaterials==0;
                results[i]=report;
                Debug.Log("[Defense QA] "+(report.passed?"PASS ":"FAIL ")+
                    name+" meshes="+report.meshes+" triangles="+report.triangles+
                    " missing scripts="+report.missingScripts+
                    " null materials="+report.missingMaterials);
            }
        }
        catch(Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[Defense QA] Incomplete scene validation. Scene backups available.");
        }
        finally
        {
            EditorSceneManager.OpenScene(originalPath,OpenSceneMode.Single);
            var collection=new ReportCollection {
                scenes=results.Where(r=>r!=null).ToArray(),
                allPass=results.All(r=>r!=null && r.passed),
                originalSceneRestored=SceneManager.GetActiveScene().path
            };
            File.WriteAllText(ProofFolder+"UnityImportedScenesQA.json",
                JsonUtility.ToJson(collection,true));
            Debug.Log("[Defense QA] ALL SCENES="+
                (collection.allPass?"PASS":"REVIEW")+
                "; restored "+collection.originalSceneRestored);
        }
    }
    private static Report Audit(Scene scene)
    {
        var roots=scene.GetRootGameObjects();
        var transforms=roots.SelectMany(go=>go.GetComponentsInChildren<Transform>(true)).ToArray();
        var renderers=roots.SelectMany(go=>go.GetComponentsInChildren<MeshRenderer>(true)).ToArray();
        var meshes=roots.SelectMany(go=>go.GetComponentsInChildren<MeshFilter>(true))
            .Where(m=>m.sharedMesh!=null).ToArray();
        return new Report {
            scene=scene.path,
            meshes=meshes.Length,
            triangles=meshes.Sum(m=>m.sharedMesh.triangles.Length/3),
            missingScripts=transforms.Sum(t=>
                GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)),
            missingMaterials=renderers.Sum(r=>
                r.sharedMaterials.Count(m=>m==null || m.shader==null)),
            cameras=roots.Sum(go=>go.GetComponentsInChildren<Camera>(true).Length),
            lights=roots.Sum(go=>go.GetComponentsInChildren<Light>(true).Length),
            hasPrimarySet=roots.Any(go=>
                go.name.StartsWith("BLENDER_EXPORT__",StringComparison.Ordinal))
        };
    }
}
#endif
