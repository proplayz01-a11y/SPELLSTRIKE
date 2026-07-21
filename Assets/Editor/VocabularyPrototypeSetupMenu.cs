#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class VocabularyPrototypeSetupMenu
{
    private const string DataFolder = "Assets/Data";
    private const string VocabularyFolder = "Assets/Data/Vocabulary";
    private const string ProfilePath = "Assets/Data/Vocabulary/Stage1_Mitigate_Prototype.asset";

    [MenuItem("SPELLSTRIKE/Vocabulary/Create Stage 1 MITIGATE Prototype Profile")]
    public static void CreateStage1PrototypeProfile()
    {
        EnsureFolder(DataFolder, "Assets", "Data");
        EnsureFolder(VocabularyFolder, DataFolder, "Vocabulary");

        StageVocabularyProfile existing = AssetDatabase.LoadAssetAtPath<StageVocabularyProfile>(ProfilePath);
        if (existing != null)
        {
            Selection.activeObject = existing;
            EditorGUIUtility.PingObject(existing);
            Debug.Log($"[VocabularyPrototypeSetup] Existing profile selected: {ProfilePath}");
            return;
        }

        StageVocabularyProfile profile = ScriptableObject.CreateInstance<StageVocabularyProfile>();
        profile.ConfigurePrototype(1, new List<VocabularyWordData>
        {
            VocabularyWordData.CreateMitigatePrototype()
        });

        AssetDatabase.CreateAsset(profile, ProfilePath);
        AssetDatabase.SaveAssets();
        Selection.activeObject = profile;
        EditorGUIUtility.PingObject(profile);
        Debug.Log($"[VocabularyPrototypeSetup] Created profile: {ProfilePath}");
    }

    private static void EnsureFolder(string fullPath, string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(fullPath))
            AssetDatabase.CreateFolder(parent, name);
    }
}
#endif
