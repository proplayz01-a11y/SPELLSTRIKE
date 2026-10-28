using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class Stage1DistanceCullingSetup
{
    private const string MarkerPath = "Temp/Stage1DistanceCullingSetup.trigger";
    private const string ExpectedScene = "Assets/Scenes/OptimizationTests/Stage1_OptimizedMap_Test.unity";
    private const string OptimizedRootName = "level1_STAGE1_OPTIMIZED_TEST";
    private const string LegacyControllerRootName = "__FINAL_OPTIMIZED_LOD_GROUPS";

    static Stage1DistanceCullingSetup() => EditorApplication.update += TryConsumeMarker;

    private static void TryConsumeMarker()
    {
        if (!File.Exists(MarkerPath)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(MarkerPath);
        Apply();
    }

    [MenuItem("SPELLSTRIKE/Optimization/LEGACY Stage1 Distance Culling Test (Do Not Use)")]
    public static void Apply()
    {
        Debug.LogError("[Stage1 Distance Culling Setup] BLOCKED: legacy setup is obsolete and can overwrite the mobile test configuration. Use Stage1MobileLodCullingSetupOnce instead. Far clip must remain 600 for the current A05s optimization pass.");
        return;
#if false
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ExpectedScene)
        {
            Debug.LogError($"[Stage1 Distance Culling Setup] Wrong active scene: {scene.path}");
            return;
        }

        Camera camera = Camera.main;
        if (camera == null)
        {
            Debug.LogError("[Stage1 Distance Culling Setup] Main Camera not found.");
            return;
        }

        GameObject root = GameObject.Find(OptimizedRootName);
        if (root == null)
        {
            Debug.LogError($"[Stage1 Distance Culling Setup] Optimized root '{OptimizedRootName}' not found.");
            return;
        }

        GameObject legacyRoot = GameObject.Find(LegacyControllerRootName);
        if (legacyRoot != null)
        {
            Stage1DistanceCullingController legacyController = legacyRoot.GetComponent<Stage1DistanceCullingController>();
            if (legacyController != null)
                Undo.DestroyObjectImmediate(legacyController);
        }

        Undo.RecordObject(camera, "Set Stage1 Android far clip to 450");
        camera.farClipPlane = 450f;
        EditorUtility.SetDirty(camera);

        Stage1DistanceCullingController controller = root.GetComponent<Stage1DistanceCullingController>();
        if (controller == null) controller = Undo.AddComponent<Stage1DistanceCullingController>(root);

        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("tinyDecorDistance").floatValue = 80f;
        serializedController.FindProperty("smallPropDistance").floatValue = 140f;
        serializedController.FindProperty("mediumPropDistance").floatValue = 220f;
        serializedController.FindProperty("structureDistance").floatValue = 320f;
        serializedController.FindProperty("landmarkDistance").floatValue = 420f;
        serializedController.FindProperty("updateInterval").floatValue = 0.2f;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Stage1 Distance Culling Setup] APPLIED | farClip=450 | tiny=80 | small=140 | medium=220 | structure=320 | landmark=420");
#endif
    }
}
