#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class Stage2AdaptiveWaterlineSetup
{
    private const string PlayerPrefabPath = "Assets/Prefabs/VanAertPlayer.prefab";
    private const string FlagPath = "RecoveryBackups/RUN_STAGE2_ADAPTIVE_WATERLINE_SETUP.flag";
    private const string ReportPath = "RecoveryBackups/Stage2AdaptiveWaterlineSetup_20260919.txt";

    static Stage2AdaptiveWaterlineSetup()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(FlagPath))
                return;

            File.Delete(FlagPath);
            Run();
        };
    }

    [MenuItem("SPELLSTRIKE/Stage 2/Apply Adaptive Waterline Anchors")]
    private static void Run()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            Transform hips = FindDeepChild(root.transform, "mixamorig:Hips");
            if (hips == null)
                throw new InvalidOperationException("Van Aert hips bone was not found.");

            Transform marker = hips.Find("WaterInteraction_Hip");
            if (marker == null)
            {
                GameObject markerObject = new GameObject("WaterInteraction_Hip");
                marker = markerObject.transform;
                marker.SetParent(hips, false);
            }

            marker.localPosition = Vector3.zero;
            marker.localRotation = Quaternion.identity;
            marker.localScale = Vector3.one;

            Stage2WaterInteraction interaction = root.GetComponent<Stage2WaterInteraction>();
            if (interaction == null)
                throw new InvalidOperationException("Van Aert is missing Stage2WaterInteraction.");

            SerializedObject serialized = new SerializedObject(interaction);
            serialized.FindProperty("hipModeThreshold").floatValue = 0.60f;
            serialized.FindProperty("waterSurfaceOffset").floatValue = 0.025f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            AssetDatabase.SaveAssets();

            File.WriteAllText(ReportPath,
                "STAGE 2 ADAPTIVE WATERLINE SETUP\n" +
                $"PlayerPrefab={PlayerPrefabPath}\n" +
                $"HipAnchor={GetHierarchyPath(marker)}\n" +
                "HipModeThreshold=0.600\n" +
                "WaterSurfaceOffset=0.025\n" +
                "AnchorColliderCount=0\n");

            Debug.Log("[Stage2AdaptiveWaterlineSetup] PASS: hip anchor and adaptive routing values saved.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static Transform FindDeepChild(Transform root, string targetName)
    {
        if (root.name == targetName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeepChild(root.GetChild(i), targetName);
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
