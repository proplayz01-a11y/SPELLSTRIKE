#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class TidalWavePrefabBuilder
{
    private const string PrefabFolder = "Assets/Stage 2/TideWraith/Prefabs";
    private const string PrefabPath = PrefabFolder + "/TidalWaveProjectile.prefab";

    [MenuItem("SPELLSTRIKE/Stage 2/Tide Wraith/Create Editable Tidal Wave Prefab")]
    public static void CreateEditablePrefab()
    {
        EnsureFolder("Assets/Stage 2/TideWraith", "Prefabs");

        GameObject root = new GameObject("TidalWaveProjectile");
        try
        {
            root.AddComponent<TidalWaveProjectile>();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Object prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            Debug.Log($"[Tidal Wave] Editable prefab created at {PrefabPath}. Tune its visual fields, then assign it to TideWraithController.");
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
