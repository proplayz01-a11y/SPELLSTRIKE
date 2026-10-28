#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class Stage2WaterVfxHipRunAutoAudit
{
    private const string FlagPath = "RecoveryBackups/RUN_STAGE2_WATER_VFX_HIP_RUN_AUDIT.flag";
    private const string ReportPath = "RecoveryBackups/Stage2WaterVfxHipRunAudit_20260919.txt";

    private static int phase;
    private static double phaseStart;
    private static Stage2WaterInteraction interaction;
    private static Stage2ShallowWaterVolume volume;
    private static CharacterController controller;
    private static Animator animator;
    private static MovementofPlayer movement;
    private static Vector3 moveDirection;
    private static int maxRipples;
    private static int maxParticles;
    private static bool activeVolumeAfterTeleport;
    private static bool hipLevelAfterTeleport;
    private static Vector3 chosenPoint;
    private static float chosenFloorY;
    private static float chosenDepth;

    static Stage2WaterVfxHipRunAutoAudit()
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
        if (state != PlayModeStateChange.EnteredPlayMode) return;

        phase = 0;
        phaseStart = EditorApplication.timeSinceStartup;
        maxRipples = maxParticles = 0;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        try
        {
            if (!EditorApplication.isPlaying) { Cleanup(); return; }

            if (phase == 0 && EditorApplication.timeSinceStartup - phaseStart >= 1.5)
            {
                SetupHipDepthTest();
                phase = 1;
                phaseStart = EditorApplication.timeSinceStartup;
                return;
            }

            if (phase == 1 && EditorApplication.timeSinceStartup - phaseStart >= 0.8)
            {
                activeVolumeAfterTeleport = GetActiveVolume(interaction) != null;
                hipLevelAfterTeleport = InvokeHipLevel(interaction, out _);
                animator.SetFloat("Speed", 1f);
                phase = 2;
                phaseStart = EditorApplication.timeSinceStartup;
                return;
            }

            if (phase == 2)
            {
                float dt = Mathf.Clamp(Time.deltaTime, 0.005f, 0.05f);
                controller.Move(moveDirection * (12f * dt));
                animator.SetFloat("Speed", 1f);
                SampleVfx();

                if (EditorApplication.timeSinceStartup - phaseStart >= 2.5)
                {
                    WriteReport();
                    File.Delete(FlagPath);
                    Cleanup();
                    EditorApplication.delayCall += EditorApplication.ExitPlaymode;
                }
            }
        }
        catch (Exception ex)
        {
            File.WriteAllText(ReportPath, "PASS=False\nException=" + ex + "\n");
            Debug.LogException(ex);
            if (File.Exists(FlagPath)) File.Delete(FlagPath);
            Cleanup();
            EditorApplication.delayCall += EditorApplication.ExitPlaymode;
        }
    }

    private static void SetupHipDepthTest()
    {
        interaction = Resources.FindObjectsOfTypeAll<Stage2WaterInteraction>()
            .FirstOrDefault(x => x != null && x.gameObject.scene.IsValid());
        volume = Resources.FindObjectsOfTypeAll<Stage2ShallowWaterVolume>()
            .FirstOrDefault(x => x != null && x.gameObject.scene.IsValid());
        if (interaction == null || volume == null)
            throw new InvalidOperationException($"Missing runtime pieces: interaction={interaction != null}, volume={volume != null}");

        controller = interaction.GetComponent<CharacterController>();
        animator = interaction.GetComponent<Animator>();
        movement = interaction.GetComponent<MovementofPlayer>();
        GameObject map = GameObject.Find("STAGE2MAP");
        if (controller == null || animator == null || movement == null || map == null)
            throw new InvalidOperationException("Player runtime pieces or STAGE2MAP missing.");

        Transform left = FindDeepChild(interaction.transform, "mixamorig:LeftFoot");
        Transform right = FindDeepChild(interaction.transform, "mixamorig:RightFoot");
        Transform hips = FindDeepChild(interaction.transform, "WaterInteraction_Hip") ?? FindDeepChild(interaction.transform, "mixamorig:Hips");
        if (left == null || right == null || hips == null)
            throw new InvalidOperationException("Foot/hip anchors missing.");

        float rootY = interaction.transform.position.y;
        float footOffset = Mathf.Min(left.position.y, right.position.y) - rootY;
        float hipOffset = hips.position.y - rootY;
        float targetDepth = Mathf.Lerp(footOffset, hipOffset, 0.78f) - footOffset;

        Renderer[] renderers = map.GetComponentsInChildren<Renderer>(true).Where(r => !(r is ParticleSystemRenderer)).ToArray();
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

        if (!TryFindDepthPoint(map.transform, bounds, volume.WaterY, targetDepth, out chosenPoint, out chosenFloorY, out chosenDepth))
            throw new InvalidOperationException($"No hip-depth map point found near target depth {targetDepth:F2}.");

        movement.disableMovement = true;
        controller.enabled = false;
        interaction.transform.position = new Vector3(chosenPoint.x, chosenFloorY - footOffset + 0.18f, chosenPoint.z);
        controller.enabled = true;
        Physics.SyncTransforms();
        controller.Move(Vector3.down * 0.5f);
        moveDirection = interaction.transform.forward;
        moveDirection.y = 0f;
        moveDirection.Normalize();
    }

    private static void SampleVfx()
    {
        GameObject pool = GameObject.Find("Stage2_WaterVfxPool_Runtime");
        if (pool == null) return;
        int ripples = pool.GetComponentsInChildren<MeshRenderer>(true)
            .Count(r => r.gameObject.activeSelf && r.gameObject.name.StartsWith("Ripple_", StringComparison.Ordinal));
        int particles = pool.GetComponentsInChildren<ParticleSystem>(true).Count(p => p.isPlaying);
        maxRipples = Mathf.Max(maxRipples, ripples);
        maxParticles = Mathf.Max(maxParticles, particles);
    }

    private static void WriteReport()
    {
        bool activeNow = GetActiveVolume(interaction) != null;
        bool hipNow = InvokeHipLevel(interaction, out Vector3 hipSurface);
        Vector3 cv = controller.velocity; cv.y = 0f;
        var sb = new StringBuilder();
        sb.AppendLine("STAGE 2 HIP RUN AUTO AUDIT");
        sb.AppendLine($"ChosenPoint={chosenPoint}");
        sb.AppendLine($"FloorY={chosenFloorY:F3}");
        sb.AppendLine($"WaterY={volume.WaterY:F3}");
        sb.AppendLine($"Depth={chosenDepth:F3}");
        sb.AppendLine($"ActiveVolumeAfterTeleport={activeVolumeAfterTeleport}");
        sb.AppendLine($"HipLevelAfterTeleport={hipLevelAfterTeleport}");
        sb.AppendLine($"ActiveVolumeAfterRun={activeNow}");
        sb.AppendLine($"HipLevelAfterRun={hipNow}");
        sb.AppendLine($"HipSurface={hipSurface}");
        sb.AppendLine($"AnimatorSpeed={animator.GetFloat("Speed"):F3}");
        sb.AppendLine($"ControllerHorizontalSpeed={cv.magnitude:F3}");
        sb.AppendLine($"MaxActiveRipples={maxRipples}");
        sb.AppendLine($"MaxPlayingParticles={maxParticles}");
        sb.AppendLine($"SplashShaderSupported={GetMaterial(interaction, "splashMaterial")?.shader?.isSupported}");
        sb.AppendLine($"RippleShaderSupported={GetMaterial(interaction, "rippleMaterial")?.shader?.isSupported}");
        File.WriteAllText(ReportPath, sb.ToString());
        Debug.Log("[Stage2WaterVfxHipRunAutoAudit]\n" + sb);
    }

    private static Stage2ShallowWaterVolume GetActiveVolume(Stage2WaterInteraction target)
    {
        return typeof(Stage2WaterInteraction).GetField("activeVolume", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target) as Stage2ShallowWaterVolume;
    }

    private static bool InvokeHipLevel(Stage2WaterInteraction target, out Vector3 surface)
    {
        MethodInfo method = typeof(Stage2WaterInteraction).GetMethod("IsWaterAtHipLevel", BindingFlags.Instance | BindingFlags.NonPublic);
        object[] args = { Vector3.zero };
        bool result = method != null && (bool)method.Invoke(target, args);
        surface = (Vector3)args[0];
        return result;
    }

    private static Material GetMaterial(Stage2WaterInteraction target, string fieldName)
    {
        return typeof(Stage2WaterInteraction).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target) as Material;
    }

    private static bool TryFindDepthPoint(Transform mapRoot, Bounds bounds, float waterY, float targetDepth, out Vector3 point, out float floorY, out float depth)
    {
        point = default; floorY = 0f; depth = 0f;
        float best = float.PositiveInfinity;
        const int grid = 81;
        for (int iz = 0; iz < grid; iz++)
        {
            float z = Mathf.Lerp(bounds.min.z, bounds.max.z, (iz + 0.5f) / grid);
            for (int ix = 0; ix < grid; ix++)
            {
                float x = Mathf.Lerp(bounds.min.x, bounds.max.x, (ix + 0.5f) / grid);
                if (!TryGetMapFloor(mapRoot, x, z, waterY, out float y)) continue;
                float d = waterY - y;
                if (d < 3f || d > 11f) continue;
                float error = Mathf.Abs(d - targetDepth);
                if (error >= best) continue;
                best = error;
                floorY = y; depth = d; point = new Vector3(x, waterY, z);
            }
        }
        return !float.IsPositiveInfinity(best);
    }

    private static bool TryGetMapFloor(Transform mapRoot, float x, float z, float waterY, out float floorY)
    {
        floorY = float.NegativeInfinity;
        RaycastHit[] hits = Physics.RaycastAll(new Vector3(x, waterY + 20f, z), Vector3.down, 40f, ~0, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null || !hit.collider.transform.IsChildOf(mapRoot)) continue;
            if (hit.point.y <= waterY + 0.02f && hit.point.y > floorY) floorY = hit.point.y;
        }
        return !float.IsNegativeInfinity(floorY);
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

    private static void Cleanup()
    {
        EditorApplication.update -= Tick;
    }
}
#endif
