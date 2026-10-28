#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class Stage2WaterResistanceSmokeTest
{
    private const string FlagPath = "RecoveryBackups/RUN_STAGE2_WATER_RESISTANCE_SMOKE.flag";
    private const string ReportPath = "RecoveryBackups/Stage2WaterResistanceSmoke_20260919.txt";
    private static double enteredAt;

    static Stage2WaterResistanceSmokeTest()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (!File.Exists(FlagPath) || state != PlayModeStateChange.EnteredPlayMode)
            return;

        enteredAt = EditorApplication.timeSinceStartup;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying)
        {
            EditorApplication.update -= Tick;
            return;
        }

        if (EditorApplication.timeSinceStartup - enteredAt < 1.5)
            return;

        EditorApplication.update -= Tick;
        try
        {
            Run();
        }
        catch (Exception ex)
        {
            File.WriteAllText(ReportPath, "PASS=False\nException=" + ex + "\n");
            Debug.LogException(ex);
        }
        finally
        {
            if (File.Exists(FlagPath)) File.Delete(FlagPath);
            EditorApplication.delayCall += EditorApplication.ExitPlaymode;
        }
    }

    private static void Run()
    {
        Stage2WaterInteraction interaction = Resources.FindObjectsOfTypeAll<Stage2WaterInteraction>()
            .FirstOrDefault(x => x != null && x.gameObject.scene.IsValid());
        Stage2ShallowWaterVolume volume = Resources.FindObjectsOfTypeAll<Stage2ShallowWaterVolume>()
            .FirstOrDefault(x => x != null && x.gameObject.scene.IsValid());
        MovementofPlayer movement = interaction != null ? interaction.GetComponent<MovementofPlayer>() : null;
        CharacterController controller = interaction != null ? interaction.GetComponent<CharacterController>() : null;

        if (interaction == null || volume == null || movement == null || controller == null)
            throw new InvalidOperationException("Stage 2 player/water runtime pieces are missing.");

        Transform left = FindDeepChild(interaction.transform, "mixamorig:LeftFoot");
        Transform right = FindDeepChild(interaction.transform, "mixamorig:RightFoot");
        Transform hips = FindDeepChild(interaction.transform, "WaterInteraction_Hip") ?? FindDeepChild(interaction.transform, "mixamorig:Hips");
        Transform head = FindDeepChild(interaction.transform, "mixamorig:Head");
        if (left == null || right == null || hips == null || head == null)
            throw new InvalidOperationException("Feet/hips/head anchors are incomplete.");

        interaction.RegisterStage2Volume(volume);

        Vector3 originalPosition = interaction.transform.position;
        bool controllerWasEnabled = controller.enabled;
        controller.enabled = false;

        float originalRootY = interaction.transform.position.y;
        float footOffset = Mathf.Min(left.position.y, right.position.y) - originalRootY;
        float hipOffset = hips.position.y - originalRootY;
        float headOffset = head.position.y - originalRootY;
        float bodyHeight = Mathf.Max(0.5f, headOffset - footOffset);

        float hipLevelOffset = Mathf.Lerp(footOffset, hipOffset, 0.60f);
        float headLevelOffset = headOffset - bodyHeight * 0.10f;
        float overheadLevelOffset = headOffset + bodyHeight * 0.10f;

        float shallow = SampleAt(interaction, volume.WaterY, footOffset + (hipLevelOffset - footOffset) * 0.45f);
        float hip = SampleAt(interaction, volume.WaterY, hipLevelOffset);
        float headLevel = SampleAt(interaction, volume.WaterY, headLevelOffset);
        float overhead = SampleAt(interaction, volume.WaterY, overheadLevelOffset + 0.05f);

        interaction.transform.position = originalPosition;
        controller.enabled = controllerWasEnabled;
        Physics.SyncTransforms();

        bool pass = Mathf.Abs(shallow - 1f) < 0.02f
                    && Mathf.Abs(hip - 0.72f) < 0.02f
                    && Mathf.Abs(headLevel - 0.48f) < 0.02f
                    && Mathf.Abs(overhead - 0.30f) < 0.02f;

        string report =
            $"PASS={pass}\n" +
            $"WaterY={volume.WaterY:F3}\n" +
            $"ShallowMultiplier={shallow:F3}\n" +
            $"HipMultiplier={hip:F3}\n" +
            $"HeadMultiplier={headLevel:F3}\n" +
            $"OverheadMultiplier={overhead:F3}\n" +
            $"WalkSpeed={movement.walkSpeed:F3}\n" +
            $"RunSpeed={movement.runSpeed:F3}\n";

        File.WriteAllText(ReportPath, report);
        Debug.Log("[Stage2WaterResistanceSmokeTest]\n" + report);
        if (!pass)
            throw new InvalidOperationException("Water resistance multipliers did not match expected values.");
    }

    private static float SampleAt(Stage2WaterInteraction interaction, float waterY, float targetWaterOffsetFromRoot)
    {
        Vector3 p = interaction.transform.position;
        p.y = waterY - targetWaterOffsetFromRoot;
        interaction.transform.position = p;
        Physics.SyncTransforms();
        return interaction.GetMovementSpeedMultiplier();
    }

    private static Transform FindDeepChild(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeepChild(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }
}
#endif
