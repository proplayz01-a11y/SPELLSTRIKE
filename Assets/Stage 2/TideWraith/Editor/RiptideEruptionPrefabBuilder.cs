#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class RiptideEruptionPrefabBuilder
{
    private const string PrefabFolder = "Assets/Stage 2/TideWraith/Prefabs";
    private const string WarningPath = PrefabFolder + "/RiptideWarning.prefab";
    private const string ImpactPath = PrefabFolder + "/RiptideImpact.prefab";

    [MenuItem("SPELLSTRIKE/Stage 2/Tide Wraith/Create Editable Riptide Prefabs")]
    public static void CreateEditablePrefabs()
    {
        EnsureFolder("Assets/Stage 2/TideWraith", "Prefabs");
        CreatePrefab("RiptideWarning", RiptideEruptionVisual.VisualPhase.Warning, WarningPath);
        CreatePrefab("RiptideImpact", RiptideEruptionVisual.VisualPhase.Impact, ImpactPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Object impact = AssetDatabase.LoadAssetAtPath<GameObject>(ImpactPath);
        Selection.activeObject = impact;
        EditorGUIUtility.PingObject(impact);
        Debug.Log($"[Riptide Eruption] Editable warning and impact prefabs created in {PrefabFolder}. Assign both to TideWraithController.");
    }

    private static void CreatePrefab(string name, RiptideEruptionVisual.VisualPhase phase, string path)
    {
        GameObject root = new GameObject(name);
        try
        {
            RiptideEruptionVisual visual = root.AddComponent<RiptideEruptionVisual>();
            visual.Configure(phase);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }
}
#endif
