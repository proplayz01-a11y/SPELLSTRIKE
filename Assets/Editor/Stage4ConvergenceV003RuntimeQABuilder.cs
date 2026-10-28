#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

// A THIRD independent scene is needed for Play Mode. The canonical Player and
// Titan remain inactive; real size collision is checked using a safe probe only.
public static class Stage4ConvergenceV003RuntimeQABuilder
{
    private const string Source = "Assets/Scenes/Stage4Scene/Stage4Scene_ConvergenceCitadel_v003_Validation.unity";
    private const string Target = "Assets/Scenes/Stage4Scene/Stage4Scene_ConvergenceCitadel_v003_RuntimeQA.unity";
    private const string NavPath = "Assets/Stage 4/ConvergenceCitadel_v003/Navigation/ConvergenceCitadel_Player535_UnityQA_NavMesh.asset";

    [MenuItem("Stage 4/Convergence Citadel v003/Open Dedicated RuntimeQA Scene (No Play)")]
    public static void OpenOnlyRuntimeScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Target))
        {
            Debug.LogError("[Stage4 RuntimeQA] Play Mode active or target scene missing. Aborting.");
            return;
        }
        for (int i = 0; i < SceneManager.sceneCount; ++i)
        {
            var loaded = SceneManager.GetSceneAt(i);
            if (loaded.isDirty)
            {
                Debug.LogError("[Stage4 RuntimeQA] Cannot switch while any loaded scene is dirty: " + loaded.path);
                return;
            }
        }
        EditorSceneManager.OpenScene(Target, OpenSceneMode.Single);
        Debug.Log("[Stage4 RuntimeQA] Opened only the independent RuntimeQA scene via visible Editor menu. Play Mode not entered.");
    }

    [MenuItem("Stage 4/Convergence Citadel v003/Create Separate Runtime Capsule QA Scene")]
    public static void Build()
    {
        if (!File.Exists(Source))
        {
            Debug.LogError("[Stage4 RuntimeQA] Source missing. No output created.");
            return;
        }
        if (File.Exists(Target) && !File.ReadAllBytes(Source).SequenceEqual(File.ReadAllBytes(Target)))
        {
            Debug.LogError("[Stage4 RuntimeQA] Existing destination is not an untouched byte-identical initial copy. Refusing to overwrite or rewire it.");
            return;
        }
        var sourceOpen = SceneManager.GetSceneByPath(Source);
        if (sourceOpen.IsValid() && sourceOpen.isLoaded && sourceOpen.isDirty)
        {
            Debug.LogError("[Stage4 RuntimeQA] Source scene has unsaved Editor modifications. No copy performed.");
            return;
        }
        var nav = AssetDatabase.LoadAssetAtPath<NavMeshData>(NavPath);
        if (nav == null) { Debug.LogError("[Stage4 RuntimeQA] Player-sized NavMesh QA asset is missing."); return; }
        if (!File.Exists(Target) && !AssetDatabase.CopyAsset(Source, Target))
        {
            Debug.LogError("[Stage4 RuntimeQA] Could not copy original isolated Stage4 validation scene.");
            return;
        }
        AssetDatabase.ImportAsset(Target);
        Scene previous = SceneManager.GetActiveScene();
        Scene stage = default;
        try
        {
            stage = EditorSceneManager.OpenScene(Target, OpenSceneMode.Additive);
            var root = stage.GetRootGameObjects().Single(go => go.name.StartsWith("Stage4_ConvergenceCitadel_v003_VALIDATION"));
            var calibration = root.GetComponentsInChildren<Transform>(true).Single(t => t.name == "04_ActorCalibration_DISABLED");
            var actorRoots = calibration.GetComponentsInChildren<Transform>(true).Where(x => x.name.StartsWith("QA_")).ToArray();
            if (actorRoots.Length != 2 || actorRoots.Any(t => t.gameObject.activeSelf))
                throw new InvalidOperationException("Source calibration actors are not exactly two inactive prefabs");

            // No existing production GameDatabaseManager object or progression systems
            // are permitted in this dedicated test scene.
            var activeComponents = stage.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<MonoBehaviour>(false)).ToArray();
            var permitted = new[] { "NavMeshSurface", "UniversalAdditionalLightData", "UniversalAdditionalCameraData" };
            if (activeComponents.Any(c => c != null && !permitted.Contains(c.GetType().Name)))
                throw new InvalidOperationException("Unexpected active MonoBehaviour in isolated environment: " + string.Join(",", activeComponents.Where(c => c != null).Select(c => c.GetType().Name)));

            var qaRoot = new GameObject("Stage4V003_RuntimeCapsuleTraversal_Only");
            SceneManager.MoveGameObjectToScene(qaRoot, stage);
            var probe = qaRoot.AddComponent<Stage4V003RuntimeTraversalProbe>();
            probe.Configure(nav);
            if (!EditorSceneManager.SaveScene(stage, Target))
                throw new IOException("Could not save separate QA Runtime scene.");
            Debug.Log("[Stage4 RuntimeQA] Created safe separate scene: " + Target + ". Does NOT activate canonical actors, use player persistence, or enter Play Mode.");
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            Debug.LogError("[Stage4 RuntimeQA] Incomplete QA copy may exist; do not rerun without review. Original scenes untouched.");
        }
        finally
        {
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            if (stage.IsValid() && stage.isLoaded) EditorSceneManager.CloseScene(stage, true);
        }
    }
}
#endif
