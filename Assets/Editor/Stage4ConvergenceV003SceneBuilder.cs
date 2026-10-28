// Isolated authoring utility. Invoked ONLY from the visible Unity Editor menu.
// Never edits, saves, or closes an existing production scene.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class Stage4ConvergenceV003SceneBuilder
{
    private const string Base = "Assets/Stage 4/ConvergenceCitadel_v003";
    private const string SceneDir = "Assets/Scenes/Stage4Scene";
    private const string StageScene = SceneDir + "/Stage4Scene.unity";
    private const string ValidationScene = SceneDir + "/Stage4Scene_ConvergenceCitadel_v003_Validation.unity";
    private const string PlayerPrefabPath = "Assets/Prefabs/VanAertPlayer.prefab";
    private const string TitanPrefabPath = "Assets/Stage 3/Enemies/STAGE 3 BOSS - TITAN/Animation_V017/Titan_ProductionIntegration/S3N4_TitanV017_PhysicalCombatCandidate.prefab";

    [MenuItem("Stage 4/Convergence Citadel v003/Create Isolated Scenes (Preserve Open Scenes)")]
    public static void CreateIsolatedScenes()
    {
        if (File.Exists(StageScene) || File.Exists(ValidationScene))
        {
            Debug.LogError("[Stage4 v003] One or both target scenes already exist. Refusing to overwrite either. Review scene paths manually.");
            return;
        }

        GameObject main = AssetDatabase.LoadAssetAtPath<GameObject>(Base + "/Environment/Visual/ConvergenceCitadel_Final.glb");
        GameObject lod = AssetDatabase.LoadAssetAtPath<GameObject>(Base + "/Environment/LOD/ConvergenceCitadel_LOD1.glb");
        GameObject collision = AssetDatabase.LoadAssetAtPath<GameObject>(Base + "/Environment/Collision/ConvergenceCitadel_Collision.glb");
        GameObject player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        GameObject titan = AssetDatabase.LoadAssetAtPath<GameObject>(TitanPrefabPath);
        if (main == null || lod == null || collision == null || player == null || titan == null)
        {
            Debug.LogError($"[Stage4 v003] Import not ready. Visual={main != null}, LOD={lod != null}, Collision={collision != null}, Player={player != null}, Titan={titan != null}. Nothing was changed.");
            return;
        }

        var original = SceneManager.GetActiveScene();
        try
        {
            BuildScene(StageScene, false, main, lod, collision, player, titan);
            BuildScene(ValidationScene, true, main, lod, collision, player, titan);
            Debug.Log("[Stage4 v003] Created new Stage4Scene plus isolated calibration scene. All original open scenes remain untouched. No Play Mode or game progression executed.");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError("[Stage4 v003] Partial build may exist. Do not rerun without inspecting created files. Production scenes remain untouched.");
        }
        finally
        {
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
        }
    }

    [MenuItem("Stage 4/Convergence Citadel v003/Align v003 QA Fixtures To Imported Unity Axes")]
    public static void AlignImportedAxes()
    {
        // glTFast converts glTF's right-handed coordinates into Unity's left-handed
        // representation. Actual import audit confirmed world X = -manifest glTF X.
        // This edits ONLY our two new stage files, with precondition checks.
        foreach (string path in new[] { StageScene, ValidationScene })
        {
            if (!File.Exists(path))
            {
                Debug.LogError("[Stage4 v003] Expected new scene missing: " + path);
                return;
            }
        }
        Scene original = SceneManager.GetActiveScene();
        foreach (string path in new[] { StageScene, ValidationScene })
        {
            var loaded = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var camera = loaded.GetRootGameObjects().FirstOrDefault(go => go.name == "Stage4_MapInspectionCamera");
                if (camera == null || Mathf.Abs(camera.transform.position.x - 1050f) > 0.1f)
                    throw new InvalidOperationException("Refusing to overwrite manually adjusted camera in " + path);
                camera.transform.position = new Vector3(-1050f, 2300f, -500f);
                camera.transform.LookAt(new Vector3(-1050f, 0f, -500f));
                if (path == ValidationScene)
                {
                    var trs = loaded.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Transform>(true)).ToArray();
                    var player = trs.FirstOrDefault(t => t.name.StartsWith("QA_CanonicalVanAert_Player_"));
                    var titan = trs.FirstOrDefault(t => t.name.StartsWith("QA_TitanV017_"));
                    if (player == null || titan == null || Mathf.Abs(player.position.x + 6f) > .1f || Mathf.Abs(titan.position.x - 1280f) > .1f)
                        throw new InvalidOperationException("Refusing to overwrite manually adjusted QA actors in " + path);
                    player.position = new Vector3(6f, 1.44f, 12f);
                    titan.position = new Vector3(-1280f, 18f, -620f);
                }
                if (!EditorSceneManager.SaveScene(loaded, path)) throw new IOException("Could not save " + path);
                Debug.Log("[Stage4 v003] Unity imported X-mirror alignment applied to OUR scene only: " + path);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError("[Stage4 v003] Stop: axis alignment precondition failed for " + path);
                return;
            }
            finally
            {
                if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
                if (loaded.IsValid() && loaded.isLoaded) EditorSceneManager.CloseScene(loaded, true);
            }
        }
    }

    private static void BuildScene(string path, bool validation, GameObject main, GameObject lod, GameObject collision, GameObject player, GameObject titan)
    {
        // Additive creation allows the currently open SampleScene to remain open,
        // including when it contains unsaved user changes. Only OUR scene is saved/closed.
        var original = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            string mode = validation ? "VALIDATION" : "SCAFFOLD";
            var root = MakeRoot(scene, "Stage4_ConvergenceCitadel_v003_" + mode);
            var environment = MakeChild(root.transform, "00_Environment");
            var collisions = MakeChild(root.transform, "01_StaticCollision_19Meshes");
            MakeChild(root.transform, "02_Objectives_FUTURE_LOGIC");
            MakeChild(root.transform, "03_EncounterGates_FUTURE_LOGIC");
            var calibration = MakeChild(root.transform, "04_ActorCalibration_DISABLED");
            MakeChild(root.transform, "05_Stage4_UI_FUTURE_LOGIC");
            MakeChild(root.transform, "06_EnvironmentVFX_FUTURE_LOGIC");

            var map = (GameObject)PrefabUtility.InstantiatePrefab(main, scene);
            map.name = "ConvergenceCitadel_v003_Visual_LOD0";
            map.transform.SetParent(environment.transform, false);
            map.transform.localPosition = Vector3.zero;
            map.transform.localRotation = Quaternion.identity;
            map.transform.localScale = Vector3.one;

            var secondary = (GameObject)PrefabUtility.InstantiatePrefab(lod, scene);
            secondary.name = "ConvergenceCitadel_v003_LOD1_DISABLED_NOT_GROUPED";
            secondary.transform.SetParent(environment.transform, false);
            secondary.transform.localPosition = Vector3.zero;
            secondary.transform.localRotation = Quaternion.identity;
            secondary.transform.localScale = Vector3.one;
            secondary.SetActive(false); // Do not draw two full-world visual exports.

            // glTFast imports a model prefab. Imported-model child nodes cannot safely
            // accept new components, so create independent editable collider GameObjects
            // whose world transforms and Mesh references match the imported source.
            var colliderSource = (GameObject)PrefabUtility.InstantiatePrefab(collision, scene);
            colliderSource.name = "Reference_CollisionGLB_DISABLED";
            colliderSource.transform.SetParent(collisions.transform, false);
            colliderSource.transform.localPosition = Vector3.zero;
            colliderSource.transform.localRotation = Quaternion.identity;
            colliderSource.transform.localScale = Vector3.one;
            var colliderRoot = MakeChild(collisions.transform, "Independent_StaticMeshColliders");
            int colliderCount = 0;
            foreach (var filter in colliderSource.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null) continue;
                var physical = MakeChild(colliderRoot.transform, filter.gameObject.name + "_PHYSICS");
                physical.transform.position = filter.transform.position;
                physical.transform.rotation = filter.transform.rotation;
                physical.transform.localScale = filter.transform.lossyScale;
                var meshCollider = physical.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = filter.sharedMesh;
                meshCollider.convex = false;
                meshCollider.isTrigger = false;
                physical.isStatic = true;
                colliderCount++;
            }
            colliderSource.SetActive(false); // Do not render or collide with source prefab.
            if (colliderCount != 19)
                throw new InvalidOperationException("Expected 19 collision meshes, found " + colliderCount);

            var nav = colliderRoot.AddComponent<NavMeshSurface>();
            nav.collectObjects = CollectObjects.Children;
            nav.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            nav.enabled = false; // Requires custom agent size/calibration before ANY bake.

            // The imported GLB already contains the 36 Blender authoring markers.
            // Do not instantiate a second marker hierarchy or assign gameplay logic.
            var markerNames = new HashSet<string>(map.GetComponentsInChildren<Transform>(true).Select(t => t.name));
            string[] mandatory = { "PlayerSpawn_Start", "Node1_ArenaCenter", "Node2_ArenaCenter", "Node3_ArenaCenter", "Boss_ArenaCenter", "Boss_ThroneSeat", "Bridge_Gate_01", "Bridge_Gate_02", "Bridge_Gate_03", "Bridge_Gate_04" };
            foreach (string marker in mandatory)
                if (!markerNames.Contains(marker))
                    throw new InvalidOperationException("GLB marker absent: " + marker);

            if (validation)
            {
                var playerGo = (GameObject)PrefabUtility.InstantiatePrefab(player, scene);
                playerGo.name = "QA_CanonicalVanAert_Player_INACTIVE_NoSaveWrites";
                playerGo.transform.SetParent(calibration.transform, false);
                playerGo.transform.position = new Vector3(6f, 1.44f, 12f);
                playerGo.transform.localScale = Vector3.one * 10.71f; // Stage3 saved world-scale candidate.
                playerGo.SetActive(false);

                var titanGo = (GameObject)PrefabUtility.InstantiatePrefab(titan, scene);
                titanGo.name = "QA_TitanV017_PhysicalCandidate_INACTIVE";
                titanGo.transform.SetParent(calibration.transform, false);
                titanGo.transform.position = new Vector3(-1280f, 18f, -620f);
                // Original candidate retains its authored 40.01 actor scale internally.
                titanGo.SetActive(false);
            }
            else
            {
                calibration.SetActive(false);
            }

            var cameraGo = MakeRoot(scene, "Stage4_MapInspectionCamera");
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 1350f;
            camera.farClipPlane = 9000f;
            cameraGo.tag = "MainCamera";
            cameraGo.AddComponent<AudioListener>();
            cameraGo.transform.position = new Vector3(-1050f, 2300f, -500f);
            cameraGo.transform.LookAt(new Vector3(-1050f, 0f, -500f));
            var lightGo = MakeRoot(scene, "Stage4_EditorPreviewLight");
            var sun = lightGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.3f;
            lightGo.transform.rotation = Quaternion.Euler(46, -30, 0);

            if (!EditorSceneManager.SaveScene(scene, path))
                throw new IOException("Unity failed to save isolated scene " + path);
            Debug.Log($"[Stage4 v003] Created {path}; collider_meshes={colliderCount}; LOD1 inactive; marker names present; {(validation ? "calibration actors inactive" : "no boss or progression wiring")}. NavMesh has NOT been baked.");
        }
        finally
        {
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static GameObject MakeRoot(Scene scene, string name)
    {
        var go = new GameObject(name);
        SceneManager.MoveGameObjectToScene(go, scene);
        return go;
    }

    private static GameObject MakeChild(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }
}
#endif
