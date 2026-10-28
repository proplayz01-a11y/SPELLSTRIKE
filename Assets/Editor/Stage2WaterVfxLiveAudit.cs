#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class Stage2WaterVfxLiveAudit
{
    private const string FlagPath = "RecoveryBackups/RUN_STAGE2_WATER_VFX_LIVE_AUDIT.flag";
    private const string ReportPath = "RecoveryBackups/Stage2WaterVfxLiveAudit_20260919.txt";
    private static double startTime;
    private static Vector3 lastPosition;
    private static bool havePosition;
    private static float maxControllerHorizontalSpeed;
    private static float maxMeasuredHorizontalSpeed;
    private static float maxAnimatorSpeed;
    private static int maxActiveRipples;
    private static int maxPlayingParticles;
    private static bool sawActiveVolume;
    private static bool sawHipLevel;
    private static float lastWaterY;
    private static float lastHipY;
    private static float lastFootY;

    static Stage2WaterVfxLiveAudit()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.delayCall += MaybeStart;
    }

    private static void MaybeStart()
    {
        if (!File.Exists(FlagPath)) return;
        if (!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
            EditorApplication.EnterPlaymode();
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (!File.Exists(FlagPath)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            startTime = EditorApplication.timeSinceStartup;
            ResetStats();
            EditorApplication.update += Tick;
        }
    }

    private static void ResetStats()
    {
        havePosition = false;
        maxControllerHorizontalSpeed = 0f;
        maxMeasuredHorizontalSpeed = 0f;
        maxAnimatorSpeed = 0f;
        maxActiveRipples = 0;
        maxPlayingParticles = 0;
        sawActiveVolume = false;
        sawHipLevel = false;
        lastWaterY = lastHipY = lastFootY = float.NaN;
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying)
        {
            EditorApplication.update -= Tick;
            return;
        }

        Stage2WaterInteraction interaction = Resources.FindObjectsOfTypeAll<Stage2WaterInteraction>()
            .FirstOrDefault(x => x != null && x.gameObject.scene.IsValid());
        if (interaction == null) return;

        CharacterController controller = interaction.GetComponent<CharacterController>();
        Animator animator = interaction.GetComponent<Animator>();
        Vector3 current = interaction.transform.position;

        if (havePosition)
        {
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            Vector3 delta = current - lastPosition;
            delta.y = 0f;
            maxMeasuredHorizontalSpeed = Mathf.Max(maxMeasuredHorizontalSpeed, delta.magnitude / dt);
        }
        lastPosition = current;
        havePosition = true;

        if (controller != null)
        {
            Vector3 v = controller.velocity;
            v.y = 0f;
            maxControllerHorizontalSpeed = Mathf.Max(maxControllerHorizontalSpeed, v.magnitude);
        }
        if (animator != null)
            maxAnimatorSpeed = Mathf.Max(maxAnimatorSpeed, animator.GetFloat("Speed"));

        FieldInfo activeVolumeField = typeof(Stage2WaterInteraction).GetField("activeVolume", BindingFlags.Instance | BindingFlags.NonPublic);
        Stage2ShallowWaterVolume volume = activeVolumeField?.GetValue(interaction) as Stage2ShallowWaterVolume;
        if (volume != null)
        {
            sawActiveVolume = true;
            lastWaterY = volume.WaterY;
        }

        Transform hips = FindDeepChild(interaction.transform, "WaterInteraction_Hip") ?? FindDeepChild(interaction.transform, "mixamorig:Hips");
        Transform left = FindDeepChild(interaction.transform, "mixamorig:LeftFoot");
        Transform right = FindDeepChild(interaction.transform, "mixamorig:RightFoot");
        if (hips != null)
        {
            lastHipY = hips.position.y;
            float leftY = left != null ? left.position.y : interaction.transform.position.y;
            float rightY = right != null ? right.position.y : interaction.transform.position.y;
            lastFootY = Mathf.Min(leftY, rightY);
            if (volume != null)
            {
                float threshold = Mathf.Lerp(lastFootY, lastHipY, 0.60f);
                if (volume.WaterY >= threshold) sawHipLevel = true;
            }
        }

        GameObject pool = GameObject.Find("Stage2_WaterVfxPool_Runtime");
        if (pool != null)
        {
            int ripples = pool.GetComponentsInChildren<MeshRenderer>(true)
                .Count(r => r.gameObject.activeSelf && r.gameObject.name.StartsWith("Ripple_", StringComparison.Ordinal));
            int particles = pool.GetComponentsInChildren<ParticleSystem>(true).Count(p => p.isPlaying);
            maxActiveRipples = Mathf.Max(maxActiveRipples, ripples);
            maxPlayingParticles = Mathf.Max(maxPlayingParticles, particles);
        }

        if (EditorApplication.timeSinceStartup - startTime < 12.0) return;

        var sb = new StringBuilder();
        sb.AppendLine("STAGE 2 WATER VFX LIVE AUDIT");
        sb.AppendLine($"SawActiveVolume={sawActiveVolume}");
        sb.AppendLine($"SawHipLevel={sawHipLevel}");
        sb.AppendLine($"MaxControllerHorizontalSpeed={maxControllerHorizontalSpeed:F3}");
        sb.AppendLine($"MaxMeasuredHorizontalSpeed={maxMeasuredHorizontalSpeed:F3}");
        sb.AppendLine($"MaxAnimatorSpeed={maxAnimatorSpeed:F3}");
        sb.AppendLine($"MaxActiveRipples={maxActiveRipples}");
        sb.AppendLine($"MaxPlayingParticles={maxPlayingParticles}");
        sb.AppendLine($"LastWaterY={lastWaterY:F3}");
        sb.AppendLine($"LastHipY={lastHipY:F3}");
        sb.AppendLine($"LastFootY={lastFootY:F3}");
        File.WriteAllText(ReportPath, sb.ToString());
        Debug.Log("[Stage2WaterVfxLiveAudit]\n" + sb);

        File.Delete(FlagPath);
        EditorApplication.update -= Tick;
        EditorApplication.delayCall += EditorApplication.ExitPlaymode;
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
