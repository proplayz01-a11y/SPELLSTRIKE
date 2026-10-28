#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class Stage2AdaptiveWaterlineSceneSync
{
    private const string ScenePath = "Assets/Scenes/Stage2Scene/Stage2Scene.unity";
    private const string PlayerPrefabPath = "Assets/Prefabs/VanAertPlayer.prefab";
    private const string SplashMaterialPath = "Assets/VFX/Stage2Water/Stage2WaterSplash.mat";
    private const string RippleMaterialPath = "Assets/VFX/Stage2Water/Stage2WaterRipple.mat";
    private const string FlagPath = "RecoveryBackups/RUN_STAGE2_ADAPTIVE_WATERLINE_SCENE_SYNC.flag";
    private const string ReportPath = "RecoveryBackups/Stage2AdaptiveWaterlineSceneSync_20260919.txt";

    static Stage2AdaptiveWaterlineSceneSync()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(FlagPath) || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            File.Delete(FlagPath);
            Run();
        };
    }

    [MenuItem("SPELLSTRIKE/Stage 2/Sync Adaptive Waterline Player")]
    private static void Run()
    {
        AssetDatabase.ImportAsset(PlayerPrefabPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        int meshBefore = CountMeshColliders(scene);
        GameObject player = FindSceneObject(scene, "VAN AERT");
        if (player == null)
            throw new InvalidOperationException("Stage 2 VAN AERT was not found.");

        Stage2WaterInteraction interaction = player.GetComponent<Stage2WaterInteraction>();
        bool addedOverride = false;
        if (interaction == null)
        {
            interaction = Undo.AddComponent<Stage2WaterInteraction>(player);
            addedOverride = true;
        }

        Material splash = AssetDatabase.LoadAssetAtPath<Material>(SplashMaterialPath);
        Material ripple = AssetDatabase.LoadAssetAtPath<Material>(RippleMaterialPath);
        if (splash == null || ripple == null)
            throw new InvalidOperationException("Stage 2 water interaction materials are missing.");

        interaction.Configure(splash, ripple);
        SerializedObject serialized = new SerializedObject(interaction);
        serialized.FindProperty("hipModeThreshold").floatValue = 0.60f;
        serialized.FindProperty("waterSurfaceOffset").floatValue = 0.025f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(interaction);

        int meshAfter = CountMeshColliders(scene);
        if (meshAfter != meshBefore)
            throw new InvalidOperationException($"MeshCollider invariant failed: before={meshBefore}, after={meshAfter}.");

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new IOException("Stage2Scene could not be saved after adaptive waterline sync.");

        File.WriteAllText(ReportPath,
            "STAGE 2 ADAPTIVE WATERLINE SCENE SYNC\n" +
            $"Player={GetHierarchyPath(player.transform)}\n" +
            $"ComponentPresent={interaction != null}\n" +
            $"AddedSceneOverride={addedOverride}\n" +
            $"MeshCollidersBefore={meshBefore}\n" +
            $"MeshCollidersAfter={meshAfter}\n");

        Debug.Log("[Stage2AdaptiveWaterlineSceneSync] PASS");
    }

    private static int CountMeshColliders(Scene scene)
    {
        return scene.GetRootGameObjects().Sum(root => root.GetComponentsInChildren<MeshCollider>(true).Length);
    }

    private static GameObject FindSceneObject(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = FindDeepChild(root.transform, name);
            if (found != null)
                return found.gameObject;
        }
        return null;
    }

    private static Transform FindDeepChild(Transform root, string name)
    {
        if (root.name == name)
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeepChild(root.GetChild(i), name);
            if (found != null)
                return found;
        }
        return null;
    }

    private static string GetHierarchyPath(Transform transform)
    {
        return transform.parent == null ? transform.name : GetHierarchyPath(transform.parent) + "/" + transform.name;
    }
}
#endif
