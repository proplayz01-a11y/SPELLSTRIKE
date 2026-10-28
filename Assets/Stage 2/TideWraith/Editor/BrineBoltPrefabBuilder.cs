#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class BrineBoltPrefabBuilder
{
    private const string PrefabFolder = "Assets/Stage 2/TideWraith/Prefabs";
    private const string PrefabPath = PrefabFolder + "/BrineBoltProjectile.prefab";

    [MenuItem("SPELLSTRIKE/Stage 2/Tide Wraith/Create Editable Brine Bolt Prefab")]
    public static void CreateEditablePrefab()
    {
        EnsureFolder("Assets/Stage 2/TideWraith", "Prefabs");

        GameObject root = new GameObject("BrineBoltProjectile");
        try
        {
            root.AddComponent<BrineBoltProjectile>();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Object prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            Debug.Log($"[Brine Bolt] Editable prefab created at {PrefabPath}. Tune its BrineBoltProjectile fields, then assign it to TideWraithController.");
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
