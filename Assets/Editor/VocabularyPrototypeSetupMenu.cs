#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class VocabularyPrototypeSetupMenu
{
    private const string DataFolder = "Assets/Data";
    private const string VocabularyFolder = "Assets/Data/Vocabulary";
    private const string ProfilePath = "Assets/Data/Vocabulary/Stage1_Mitigate_Prototype.asset";
    private const string CandidateProfilePath = "Assets/Data/Vocabulary/Stage1_Vocabulary_Candidate.asset";
    private const string Stage2CandidateProfilePath = "Assets/Data/Vocabulary/Stage2_Vocabulary_Candidate.asset";

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

    [MenuItem("SPELLSTRIKE/Vocabulary/Create or Update Stage 1 Candidate Profile")]
    public static void CreateOrUpdateStage1CandidateProfile()
    {
        EnsureFolder(DataFolder, "Assets", "Data");
        EnsureFolder(VocabularyFolder, DataFolder, "Vocabulary");

        StageVocabularyProfile profile =
            AssetDatabase.LoadAssetAtPath<StageVocabularyProfile>(CandidateProfilePath);

        bool created = profile == null;
        if (created)
            profile = ScriptableObject.CreateInstance<StageVocabularyProfile>();

        profile.Configure(1, new List<VocabularyWordData>
        {
            VocabularyWordData.CreateStage1MitigateCandidate(),
            VocabularyWordData.CreateStage1FractureCandidate(),
            VocabularyWordData.CreateStage1NullifyCandidate(),
            VocabularyWordData.CreateStage1LiberateCandidate()
        });

        if (created)
            AssetDatabase.CreateAsset(profile, CandidateProfilePath);
        else
            EditorUtility.SetDirty(profile);

        AssetDatabase.SaveAssets();
        Selection.activeObject = profile;
        EditorGUIUtility.PingObject(profile);

        string action = created ? "Created" : "Updated";
        Debug.Log($"[VocabularyPrototypeSetup] {action} Stage 1 candidate profile: {CandidateProfilePath}");
    }

    [MenuItem("SPELLSTRIKE/Vocabulary/Create or Update Stage 2 Candidate Profile")]
    public static void CreateOrUpdateStage2CandidateProfile()
    {
        EnsureFolder(DataFolder, "Assets", "Data");
        EnsureFolder(VocabularyFolder, DataFolder, "Vocabulary");

        StageVocabularyProfile profile =
            AssetDatabase.LoadAssetAtPath<StageVocabularyProfile>(Stage2CandidateProfilePath);

        bool created = profile == null;
        if (created)
            profile = ScriptableObject.CreateInstance<StageVocabularyProfile>();

        profile.Configure(2, new List<VocabularyWordData>
        {
            VocabularyWordData.CreateStage2SubmergeCandidate(),
            VocabularyWordData.CreateStage2NavigateCandidate(),
            VocabularyWordData.CreateStage2UndertowCandidate(),
            VocabularyWordData.CreateStage2NauticalCandidate(),
            VocabularyWordData.CreateStage2ErodeCandidate()
        });

        if (created)
            AssetDatabase.CreateAsset(profile, Stage2CandidateProfilePath);
        else
            EditorUtility.SetDirty(profile);

        AssetDatabase.SaveAssets();
        Selection.activeObject = profile;
        EditorGUIUtility.PingObject(profile);

        string action = created ? "Created" : "Updated";
        Debug.Log($"[VocabularyPrototypeSetup] {action} Stage 2 candidate profile: {Stage2CandidateProfilePath}");
    }

    private static void EnsureFolder(string fullPath, string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(fullPath))
            AssetDatabase.CreateFolder(parent, name);
    }
}
#endif
