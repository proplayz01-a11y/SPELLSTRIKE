#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class Stage2WaterResistanceSetup
{
    private const string PrefabPath = "Assets/Prefabs/VanAertPlayer.prefab";
    private const string FlagPath = "RecoveryBackups/RUN_STAGE2_WATER_RESISTANCE_SETUP.flag";
    private const string ReportPath = "RecoveryBackups/Stage2WaterResistanceSetup_20260919.txt";

    static Stage2WaterResistanceSetup()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(FlagPath) || EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            File.Delete(FlagPath);
            Run();
        };
    }

    [MenuItem("SPELLSTRIKE/Stage 2/Apply Water Resistance")]
    private static void Run()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Stage2WaterInteraction interaction = root.GetComponent<Stage2WaterInteraction>();
            if (interaction == null)
                throw new InvalidOperationException("Canonical Van Aert prefab is missing Stage2WaterInteraction.");

            SerializedObject so = new SerializedObject(interaction);
            so.FindProperty("hipMovementMultiplier").floatValue = 0.72f;
            so.FindProperty("headMovementMultiplier").floatValue = 0.48f;
            so.FindProperty("overheadMovementMultiplier").floatValue = 0.30f;
            so.FindProperty("headLevelBodyOffset").floatValue = 0.10f;
            so.FindProperty("overheadBodyOffset").floatValue = 0.10f;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();

            File.WriteAllText(ReportPath,
                "STAGE 2 WATER RESISTANCE SETUP\n" +
                "HipMultiplier=0.72\n" +
                "HeadMultiplier=0.48\n" +
                "OverheadMultiplier=0.30\n" +
                "HeadLevelBodyOffset=0.10\n" +
                "OverheadBodyOffset=0.10\n");

            Debug.Log("[Stage2WaterResistanceSetup] PASS");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
#endif
