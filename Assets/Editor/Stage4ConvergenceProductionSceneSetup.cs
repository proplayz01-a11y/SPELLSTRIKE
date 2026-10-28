// Converts the EXISTING Assets/Scenes/Stage4Scene/Stage4Scene.unity map scaffold
// into the actual production Stage 4 scene. Visible Unity Editor menu only.
// This file never writes any other gameplay scene, prefab, animation, or save.
#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class Stage4ConvergenceProductionSceneSetup
{
    private const string ScenePath = "Assets/Scenes/Stage4Scene/Stage4Scene.unity";
    private const string ReportPath = "Assets/Stage 4/ConvergenceCitadel_v003/Validation/Stage4ProductionSceneIntegration_20261020.md";
    private const string NavPath = "Assets/Stage 4/ConvergenceCitadel_v003/Navigation/ConvergenceCitadel_Player535_UnityQA_NavMesh.asset";
    private const string BackupFolder = "Library/Stage4ProductionSceneIntegration_20261020";
    private const string PlayerPath = "Assets/Prefabs/VanAertPlayer.prefab";
    private const string HudPath = "Assets/Prefabs/PersistentGameplayHUD.prefab";
    private const string TilesPath = "Assets/Prefabs/TileManager.prefab";
    private const string DictionaryPath = "Assets/Prefabs/DictionaryManager.prefab";

    [MenuItem("Stage 4/Convergence Citadel v003/INTEGRATE Actual Stage4Scene Production Roots")]
    public static void Integrate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying || File.Exists(ReportPath))
        {
            Debug.LogError("[Stage4Production] Play Mode or existing integration report. Refusing to overwrite.");
            return;
        }

        var existing = SceneManager.GetSceneByPath(ScenePath);
        if (!File.Exists(ScenePath) || (existing.IsValid() && existing.isLoaded))
        {
            Debug.LogError("[Stage4Production] Stage4Scene is missing or open. Close it without discarding unsaved work before integrating.");
            return;
        }

        GameObject playerPrefab = LoadPrefab(PlayerPath);
        GameObject hudPrefab = LoadPrefab(HudPath);
        GameObject tilePrefab = LoadPrefab(TilesPath);
        GameObject dictionaryPrefab = LoadPrefab(DictionaryPath);
        NavMeshData navData = AssetDatabase.LoadAssetAtPath<NavMeshData>(NavPath);
        string worldTilePath = AssetDatabase.GUIDToAssetPath("e13f96c4a65533b4787e863b96deae50");
        GameObject worldTilePrefab = LoadPrefab(worldTilePath);
        if (playerPrefab == null || hudPrefab == null || tilePrefab == null ||
            dictionaryPrefab == null || navData == null || worldTilePrefab == null)
        {
            Debug.LogError("[Stage4Production] A canonical prefab or baked navigation asset is missing. No scene was changed.");
            return;
        }

        // All backups are raw originals, before opening a scene in the Editor.
        string backupFile = BackupFolder + "/Stage4Scene_before_production.unity";
        string oldMeta = BackupFolder + "/Stage4Scene_before_production.unity.meta";
        if (File.Exists(backupFile) || File.Exists(oldMeta))
        {
            Debug.LogError("[Stage4Production] Previous backup exists. Refusing repeat.");
            return;
        }
        Directory.CreateDirectory(BackupFolder);
        File.Copy(ScenePath, backupFile);
        File.Copy(ScenePath + ".meta", oldMeta);
        string sceneBeforeHash = Sha256(backupFile);

        Scene previous = SceneManager.GetActiveScene();
        Scene stage = default;
        bool committed = false;
        try
        {
            stage = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            GameObject oldRoot = stage.GetRootGameObjects().Single(x => x.name == "Stage4_ConvergenceCitadel_v003_SCAFFOLD");
            if (stage.GetRootGameObjects().Length != 3 ||
                oldRoot.GetComponentsInChildren<LODGroup>(true).Length != 11 ||
                oldRoot.GetComponentsInChildren<MeshCollider>(true).Count(c => c.enabled) != 19 ||
                stage.GetRootGameObjects().Any(x => x.name == "VanAertPlayer" || x.name == "PersistentHUDRoot"))
                throw new InvalidOperationException("Stage4 scaffold does not match the verified pre-production state.");

            var physics = oldRoot.GetComponentsInChildren<Transform>(true)
                .Single(t => t.name == "Independent_StaticMeshColliders");
            var originalBridge4 = physics.GetComponentsInChildren<MeshCollider>(true)
                .Single(x => x.name == "COL_Bridge_04_Deck_PHYSICS");
            var smooth = physics.GetComponentsInChildren<MeshCollider>(true)
                .Single(x => x.name == "Bridge04_SmoothCollision_Validated_Traversable");
            if (originalBridge4.enabled || !smooth.enabled)
                throw new InvalidOperationException("Validated Bridge 4 replacement is missing or inactive.");

            // Distinct, genuine Unity scene name remains Stage4Scene.unity; do not
            // create another preview or copy of the world.
            oldRoot.name = "Stage4_ConvergenceCitadel_PRODUCTION";
            RenameIfPresent(oldRoot.transform, "02_Objectives_FUTURE_LOGIC", "02_Stage4_Objectives_NotYetWired");
            RenameIfPresent(oldRoot.transform, "03_EncounterGates_FUTURE_LOGIC", "03_Stage4_EncounterGates_NotYetWired");
            RenameIfPresent(oldRoot.transform, "04_ActorCalibration_DISABLED", "04_ProductionActors_SeparateRoot");
            RenameIfPresent(oldRoot.transform, "05_Stage4_UI_FUTURE_LOGIC", "05_Stage4_AdditionalUI_NotYetWired");

            var map = oldRoot.GetComponentsInChildren<Transform>(true)
                .First(t => t.name == "ConvergenceCitadel_v003_Visual_LOD0");
            var marker = map.GetComponentsInChildren<Transform>(true)
                .Single(t => t.name == "PlayerSpawn_Start");
            var node1 = map.GetComponentsInChildren<Transform>(true)
                .Single(t => t.name == "Node1_ArenaCenter");
            var bridge1 = map.GetComponentsInChildren<Transform>(true)
                .Single(t => t.name == "Bridge_01_Entry");
            Vector3 spawnPosition = marker.position;
            if (Vector3.Distance(spawnPosition, new Vector3(6f, 1.44f, 12f)) > 0.05f)
                throw new InvalidOperationException("Imported Start marker moved unexpectedly: " + spawnPosition);
            Vector3 heading = bridge1.position - spawnPosition;
            heading.y = 0f;
            Quaternion headingRotation = Quaternion.LookRotation(heading.normalized, Vector3.up);

            // The canonical player prefab already contains PersistentPlayerRoot;
            // first-stage loads use it, subsequent scene loads rebind the existing DDOL.
            GameObject playerSetup = MakeRoot(stage, "_PlayerSetup");
            GameObject spawn = MakeRoot(stage, "PlayerSpawnPoint");
            spawn.transform.SetParent(playerSetup.transform, false);
            spawn.transform.SetPositionAndRotation(spawnPosition, headingRotation);
            spawn.AddComponent<PlayerSpawnPoint>().SetPlayerScale(Vector3.one * 10.71f);

            GameObject player = InstantiatePrefab(playerPrefab, stage, "VanAertPlayer");
            player.transform.SetParent(playerSetup.transform, true);
            player.transform.SetPositionAndRotation(spawnPosition, headingRotation);
            player.transform.localScale = Vector3.one * 10.71f;
            if (player.GetComponent<PersistentPlayerRoot>() == null ||
                player.GetComponent<CharacterController>() == null ||
                player.GetComponent<AttackController>() == null)
                throw new InvalidOperationException("Canonical player prefab is missing essential existing components.");

            // Preserve the real HUD prefab and its approved portrait/HP/EXP/tile
            // binding. PersistentHUDRoot's singleton Awake cleans a duplicate on
            // entry from a previous stage, so do not make a second independent HUD.
            GameObject hud = InstantiatePrefab(hudPrefab, stage, "PersistentHUDRoot");
            if (hud.GetComponent<PersistentHUDRoot>() == null)
                throw new InvalidOperationException("Original persistent HUD prefab is missing PersistentHUDRoot.");

            // The map previously had a static orthographic *inspection* camera.
            // Reconfigure THAT single camera to Stage3's gameplay camera pattern,
            // maintaining one MainCamera-tagged Camera and AudioListener.
            var cameraRoot = stage.GetRootGameObjects().Single(x => x.name == "Stage4_MapInspectionCamera");
            cameraRoot.name = "Main Camera";
            cameraRoot.tag = "MainCamera";
            Camera camera = cameraRoot.GetComponent<Camera>();
            if (camera == null) throw new InvalidOperationException("Map camera missing");
            camera.orthographic = false;
            camera.fieldOfView = 80f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 600f;
            camera.allowHDR = false;
            cameraRoot.transform.position = spawnPosition + new Vector3(0f, 5f, -57f);
            cameraRoot.transform.LookAt(spawnPosition);
            if (cameraRoot.GetComponent<UniversalAdditionalCameraData>() == null)
                cameraRoot.AddComponent<UniversalAdditionalCameraData>();
            CombatCamera follow = cameraRoot.GetComponent<CombatCamera>() ?? cameraRoot.AddComponent<CombatCamera>();
            follow.player = player.transform;
            follow.enemy = null;
            follow.offset = new Vector3(0f, 5f, -57f);
            follow.followSmoothSpeed = 0.125f;
            follow.lookSmoothSpeed = 5f;
            follow.orbitSpeed = 5f;
            follow.lockOnEnemy = false;
            follow.SetMobileTouchOrbitEnabled(true);

            var managersRoot = MakeRoot(stage, "_Stage4Systems");
            GameObject stageProgress = MakeChild(managersRoot.transform, "Stage4ProgressionManager");
            StageProgressionManager progression = stageProgress.AddComponent<StageProgressionManager>();
            SerializedObject progressSO = new SerializedObject(progression);
            progressSO.FindProperty("stageIndex").intValue = 4;
            progressSO.FindProperty("stageName").stringValue = "Stage 4 - Convergence Citadel";
            progressSO.FindProperty("nodeCount").intValue = 4;
            SerializedProperty nodeList = progressSO.FindProperty("nodes");
            nodeList.arraySize = 4;
            string[] labels = { "Hollow Knight", "Drowned Captain", "Titan Colossus", "Vander Poel" };
            for (int i=0;i<4;i++)
            {
                nodeList.GetArrayElementAtIndex(i).FindPropertyRelative("nodeName").stringValue = labels[i];
                nodeList.GetArrayElementAtIndex(i).FindPropertyRelative("startsUnlocked").boolValue = i == 0;
                nodeList.GetArrayElementAtIndex(i).FindPropertyRelative("completed").boolValue = false;
            }
            progressSO.ApplyModifiedPropertiesWithoutUndo();

            // Singleton owners are scene roots, never child objects. Their
            // existing Awake duplicate guards preserve the DDOL instances when
            // loading Stage4 through Stage Select/Item Selection.
            var gameManagerGo = MakeRoot(stage, "GameManager");
            GameManager gameManager = gameManagerGo.AddComponent<GameManager>();
            gameManager.currentStageIndex = 4;
            var databaseGo = MakeRoot(stage, "GameDatabaseManager");
            var database = databaseGo.AddComponent<GameDatabaseManager>();
            database.useSqlite = false; // Isolated direct launch uses authoritative existing JSON, not failing native SQLite.
            InstantiatePrefab(dictionaryPrefab, stage, "DictionaryManager");

            GameObject tileManager = InstantiatePrefab(tilePrefab, stage, "TileManager");
            var tile = tileManager.GetComponent<TileManager>();
            if (tile == null) throw new InvalidOperationException("Expected existing TileManager prefab component");
            tile.currentStage = 4;
            tile.tilePoolPanel = null;
            tile.wordBarPanel = null;
            tile.attackController = null;
            GameObject spawnerObject = MakeChild(managersRoot.transform, "Tile Spawner");
            var tileSpawner = spawnerObject.AddComponent<TileSpawner>();
            tileSpawner.worldTilePrefab = worldTilePrefab;
            tileSpawner.arenaCenter = node1;
            tileSpawner.arenaRadius = 65f;
            tileSpawner.spawnY = node1.position.y;
            tileSpawner.groundLayer = 1 << 0; // Imported collision uses Default; preserve Stage2 fallback checks.
            tileSpawner.shrineExclusionRadius = 0f;

            // Gameplay UI contains buttons: must have exactly one input EventSystem
            // in standalone scene. Existing DDOL HUD does not create one.
            var eventSystemGo = MakeRoot(stage, "EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<InputSystemUIInputModule>();

            // Use the already independently baked and 8/8 path-verified
            // player-size NavMesh (new Stage4-only surface, not world defaults).
            NavMeshSurface surface = physics.GetComponent<NavMeshSurface>();
            if (surface == null) throw new InvalidOperationException("Expected imported collision NavMeshSurface");
            surface.navMeshData = navData;
            surface.enabled = true;

            // Separate arena markers already exist inside the imported visual
            // model. The original four bosses and triggers are NOT wired yet.
            if (stage.GetRootGameObjects().Count(g => g.GetComponent<PersistentHUDRoot>() != null) != 1 ||
                stage.GetRootGameObjects().Count(g => g.GetComponent<GameDatabaseManager>() != null) != 1 ||
                stage.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PersistentPlayerRoot>(true)).Count() != 1 ||
                stage.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<LODGroup>(true)).Count() != 11 ||
                stage.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>(true)).Count() != 1)
                throw new InvalidOperationException("Singleton/player/map/camera validation failed; refusing save.");

            EditorSceneManager.MarkSceneDirty(stage);
            if (!EditorSceneManager.SaveScene(stage, ScenePath))
                throw new IOException("Unity failed to save actual Stage4Scene");
            committed = true;
            string message =
                "# Stage4Scene v003 production-scene foundation\n" +
                "- PASS: Integrated into existing actual `" + ScenePath + "`, NOT a preview/QA scene.\n" +
                "- Previous scene SHA-256: `" + sceneBeforeHash + "`, backed up under `" + BackupFolder + "`.\n" +
                "- PASS: Canonical VanAertPlayer prefab, one scene-authored PersistentGameplayHUD prefab, GameManager, JSON-backed GameDatabaseManager, DictionaryManager, TileManager stage=4, TileSpawner and EventSystem.\n" +
                "- PASS: Explicit PlayerSpawnPoint at imported marker " + spawnPosition + ", world player scale 10.71, camera CombatCamera perspective FOV80 far600, mobile right-side look, existing shader/LOD map.\n" +
                "- PASS: Enabled previously baked Stage4 player-sized NavMesh; original visual GLB/19 physics colliders, tested Bridge04 replacement and all 11 LODGroups preserved.\n" +
                "- NOT IMPLEMENTED: actual Hollow Knight, Drowned Captain, Titan and Vander Poel battle AI/activation, enforced bridge locks, scene complete/review and final cutscene.\n" +
                "- NOT TESTED: Full persistent scene transition, canonical player/hud gameplay, Galaxy A05s FPS/save longevity. Do not call stage final-defense-ready.\n" +
                "- Protected: original SampleScene/Stage2/Stage3, shared prefabs, saves, PlayerPrefs and Build Settings left untouched by this menu.\n";
            File.WriteAllText(ReportPath, message, new System.Text.UTF8Encoding(false));
            AssetDatabase.ImportAsset(ReportPath);
            Debug.Log("[Stage4Production] PRODUCTION SCENE SAVED: " + ScenePath + " (scene roots + real HUD/player/camera/tiles). Boss encounters remain unimplemented.");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            if (!committed) Debug.LogError("[Stage4Production] Scene was NOT saved; inspect the backup and any new report before retrying.");
        }
        finally
        {
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            if (stage.IsValid() && stage.isLoaded) EditorSceneManager.CloseScene(stage, true);
        }
    }

    private static GameObject LoadPrefab(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path);

    private static GameObject InstantiatePrefab(GameObject prefab, Scene scene, string name)
    {
        var spawned = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
        if (spawned == null) throw new InvalidOperationException("Prefab instantiation failed: " + prefab.name);
        spawned.name = name;
        spawned.transform.position = Vector3.zero;
        return spawned;
    }

    private static void RenameIfPresent(Transform root, string prior, string next)
    {
        var go = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == prior);
        if (go != null) go.name = next;
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

    private static string Sha256(string file)
    {
        using (var sha = SHA256.Create())
        using (var stream = File.OpenRead(file))
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
    }
}
#endif
