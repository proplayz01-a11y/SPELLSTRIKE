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
/// Isolated Panel 01 defense shot using the approved v02 Blender room and
/// camera-ready, posed characters. Does not modify gameplay or older cinematic scenes.
/// The original animated Blender/FBX sources remain separate for later motion work.
/// </summary>
internal static class PrologueP01V02SceneAssembler
{
    private const string ScenePath =
        "Assets/Scenes/Cinematics/Prologue_P01_FamilyHome_v02.unity";
    private const string House =
        "Assets/Cinematics/Storyboard3D/Environment/DefenseSets/Prologue_Home_01_Peaceful_v02_UnityPBR.glb";
    private const string Van =
        "Assets/Cinematics/Storyboard3D/Characters/Panel01_v02/YoungVanAert_ArmsCrossed_StaticPose_v06.glb";
    private const string Nevali =
        "Assets/Cinematics/Storyboard3D/Characters/Panel01_v02/Nevali_Playing_StaticPose_v13.glb";
    private const string ReportPath =
        "Library/P01UnityIntegrationQA/Panel01_v02_EditorValidation.txt";

    [MenuItem("SPELLSTRIKE/Cinematics/Panel 01 v02/Build Isolated Shot")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Panel 01 build requires Edit Mode.");
        if (File.Exists(ScenePath))
        {
            Debug.Log("[P01 v02] Existing isolated scene preserved: " + ScenePath);
            ValidateExisting();
            return;
        }

        var current = SceneManager.GetActiveScene();
        var previousPath = current.IsValid() ? current.path : string.Empty;
        if (!Application.isBatchMode && current.IsValid() && current.isDirty)
        {
            Debug.LogError("[P01 v02] Save the currently open scene first. No changes made.");
            return;
        }

        var housePrefab = RequirePrefab(House);
        var vanPrefab = RequirePrefab(Van);
        var nevaliPrefab = RequirePrefab(Nevali);
        try
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var environment = Instantiate(housePrefab, scene, "P01_V02__HOUSE__CODEX_BLENDER");
            environment.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            environment.transform.localScale = Vector3.one;

            var actorRoot = new GameObject("P01_ACTORS__STATIC_CINEMATIC_POSES");
            var father = Instantiate(vanPrefab, scene, "YOUNG_VAN_AERT__ARMS_CROSSED_V06");
            father.transform.SetParent(actorRoot.transform, false);
            // glTFast converts the Blender right-handed coordinates to Unity
            // by mirroring X. Both actor GLBs also require +90 degrees around
            // Unity X, as verified in Unity's imported prefab root transforms.
            father.transform.SetPositionAndRotation(
                new Vector3(1.95f, 0f, 2.75f),
                Quaternion.AngleAxis(-27f, Vector3.up) * Quaternion.Euler(90f, 0f, 0f));
            father.transform.localScale = Vector3.one;

            var child = Instantiate(nevaliPrefab, scene, "NEVALI_V13__PLAYING_WITH_BLOCKS");
            child.transform.SetParent(actorRoot.transform, false);
            // Blender marker (1.28, -0.45, 0), converted to mirrored-X Unity Y-up.
            // +0.045 m floor clearance is from Codex's P01 v02 framing QA.
            child.transform.position = new Vector3(-1.28f, 0.045f, 0.45f);
            child.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            child.transform.localScale = Vector3.one;

            // Exact v02 Blender hero camera: (-2.75, -5.2, 2.05), lens 32mm,
            // sensor width 36mm, output 16:9. glTFast maps source Blender
            // (x,y,z) to Unity (-x,z,-y), including mirrored camera direction.
            var cameraRoot = new GameObject("P01_STATIC_HERO_CAMERA__MATCH_32MM");
            cameraRoot.tag = "MainCamera";
            cameraRoot.transform.position = new Vector3(2.75f, 2.05f, 5.20f);
            cameraRoot.transform.rotation =
                Quaternion.LookRotation(new Vector3(-0.505468f, -0.159708f, -0.847936f),
                    Vector3.up);
            var camera = cameraRoot.AddComponent<Camera>();
            camera.fieldOfView = 35.1154f;
            camera.nearClipPlane = 0.06f;
            camera.farClipPlane = 120f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.66f, 0.75f, 0.79f);
            camera.allowHDR = false;
            cameraRoot.AddComponent<AudioListener>();
            cameraRoot.AddComponent<Panel01CinematicPlayback>();

            // Blender preview lights are not in the geometry GLB. Recreate a
            // conservative URP-friendly lighting approximation here.
            var lighting = new GameObject("P01_CINEMATIC_LIGHTING__UNITY_PREVIEW");
            var sunlight = new GameObject("WARM_WINDOW_SUN");
            sunlight.transform.SetParent(lighting.transform);
            sunlight.transform.rotation = Quaternion.Euler(48f, 230f, 0f);
            var sun = sunlight.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.2f;
            sun.color = new Color(1.0f, 0.84f, 0.63f);
            sun.shadows = LightShadows.None;

            MakePointLight(lighting.transform, "WINDOW_BOUNCE",
                new Vector3(-3.2f, 2.7f, 0.2f),
                new Color(1f, 0.76f, 0.48f), 1.4f, 8.5f);
            MakePointLight(lighting.transform, "ACTOR_FACE_FILL",
                new Vector3(1.5f, 2.7f, 2.9f),
                new Color(1f, 0.91f, 0.78f), 0.62f, 5.5f);

            var metadata = new GameObject("P01__10_SECOND_MEMORY_SHOT__VOICEOVER_OPTIONAL");
            metadata.transform.SetParent(cameraRoot.transform, false);
            metadata.SetActive(false); // Narration text awaits approved copy.

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.48f, 0.43f, 0.39f);
            RenderSettings.fog = false;

            CheckActor(father, "Young Van");
            CheckActor(child, "Nevali");
            if (environment.GetComponentsInChildren<Renderer>(true).Length < 25)
                throw new InvalidOperationException("House v02 has unexpectedly few renderers.");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new IOException("Unity did not save " + ScenePath);
            SaveReport(scene, environment, father, child, camera);
            Debug.Log("[P01 v02] ISOLATED UNITY SCENE CREATED: " + ScenePath);
        }
        finally
        {
            // Never leave a different user scene open as a side effect in GUI mode.
            if (!Application.isBatchMode && !string.IsNullOrEmpty(previousPath) &&
                File.Exists(previousPath))
                EditorSceneManager.OpenScene(previousPath, OpenSceneMode.Single);
        }
    }

    [MenuItem("SPELLSTRIKE/Cinematics/Panel 01 v02/Validate Shot")]
    public static void ValidateExisting()
    {
        if (!File.Exists(ScenePath))
            throw new FileNotFoundException("Panel 01 v02 scene not built", ScenePath);
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var all = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .ToArray();
        var env = all.FirstOrDefault(x => x.name == "P01_V02__HOUSE__CODEX_BLENDER");
        var father = all.FirstOrDefault(x => x.name == "YOUNG_VAN_AERT__ARMS_CROSSED_V06");
        var child = all.FirstOrDefault(x => x.name == "NEVALI_V13__PLAYING_WITH_BLOCKS");
        var cam = all.Select(x => x.GetComponent<Camera>()).FirstOrDefault(x => x != null);
        if (env == null || father == null || child == null || cam == null)
            throw new InvalidOperationException("P01 v02 missing environment, actor or camera.");
        CheckActor(father.gameObject, "Young Van");
        CheckActor(child.gameObject, "Nevali");
        if (env.GetComponentsInChildren<Renderer>(true).Length < 25)
            throw new InvalidOperationException("P01 v02 missing house renderers.");
        SaveReport(scene, env.gameObject, father.gameObject, child.gameObject, cam);
        Debug.Log("[P01 v02] VALIDATION PASS (hierarchy, renderer, camera): " + ScenePath);
    }

    private static GameObject RequirePrefab(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("P01 required GLB missing", path);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
            throw new InvalidOperationException("P01 glTFast GameObject import failed: " + path);
        return prefab;
    }

    private static GameObject Instantiate(GameObject asset, Scene scene, string name)
    {
        var go = PrefabUtility.InstantiatePrefab(asset, scene) as GameObject;
        if (go == null)
            throw new InvalidOperationException("Could not instantiate " + asset.name);
        go.name = name;
        return go;
    }

    private static void CheckActor(GameObject actor, string label)
    {
        var renderers = actor.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            throw new InvalidOperationException(label + " has no renderers.");
        if (!renderers.Any(r => r.sharedMaterials.Any(m => m != null)))
            throw new InvalidOperationException(label + " has no assigned materials.");
    }

    private static void MakePointLight(Transform parent, string name,
        Vector3 position, Color color, float intensity, float range)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
    }

    private static void SaveReport(Scene scene, GameObject house,
        GameObject father, GameObject child, Camera camera)
    {
        var report =
            "Scene: " + scene.path + "\n" +
            "House renderers: " + house.GetComponentsInChildren<Renderer>(true).Length + "\n" +
            "Van renderers: " + father.GetComponentsInChildren<Renderer>(true).Length + "\n" +
            "Nevali renderers: " + child.GetComponentsInChildren<Renderer>(true).Length + "\n" +
            "Van Unity position: " + father.transform.position + "\n" +
            "Van Unity rotation: " + father.transform.rotation.eulerAngles + "\n" +
            "Nevali Unity position: " + child.transform.position + "\n" +
            "Camera Unity position: " + camera.transform.position + "\n" +
            "Camera FOV: " + camera.fieldOfView + "\n" +
            "Composition: static v02 room, v06 crossed-arms Van, v13 playing Nevali\n" +
            "Editor-only validation: hierarchy, renderers, materials and camera checked.\n" +
            "For actual Play Mode verification and screenshot evidence, see docs/PANEL01_UNITY_FINAL_VALIDATION_20261025.md\n";
        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        File.WriteAllText(ReportPath, report);
    }
}
#endif
