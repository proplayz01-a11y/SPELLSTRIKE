#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class Stage2WaterVfxVisualCaptureAudit
{
    private const string FlagPath = "RecoveryBackups/RUN_STAGE2_WATER_VFX_VISUAL_CAPTURE.flag";
    private const string ScreenshotPath = "RecoveryBackups/Stage2WaterVfxVisualCapture_20260919.png";
    private const string ReportPath = "RecoveryBackups/Stage2WaterVfxVisualCapture_20260919.txt";

    private static Stage2WaterInteraction interaction;
    private static Stage2ShallowWaterVolume volume;
    private static CharacterController controller;
    private static Animator animator;
    private static MovementofPlayer movement;
    private static Vector3 moveDirection;
    private static double phaseStart;
    private static int phase;

    static Stage2WaterVfxVisualCaptureAudit()
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
        if (!File.Exists(FlagPath) || state != PlayModeStateChange.EnteredPlayMode) return;
        phase = 0;
        phaseStart = EditorApplication.timeSinceStartup;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        try
        {
            if (!EditorApplication.isPlaying) { EditorApplication.update -= Tick; return; }

            if (phase == 0 && EditorApplication.timeSinceStartup - phaseStart > 1.5)
            {
                Setup();
                phase = 1;
                phaseStart = EditorApplication.timeSinceStartup;
                return;
            }

            if (phase == 1)
            {
                float dt = Mathf.Clamp(Time.deltaTime, 0.005f, 0.05f);
                SetMovementState(Vector2.up, true);
                Vector3 testVelocity = moveDirection * 12f;
                controller.Move(testVelocity * dt);
                animator.SetFloat("Speed", 1f);
                InvokeContinuousVfx(testVelocity);

                if (EditorApplication.timeSinceStartup - phaseStart > 2.2)
                {
                    CaptureReport();
                    ScreenCapture.CaptureScreenshot(ScreenshotPath, 1);
                    phase = 2;
                    phaseStart = EditorApplication.timeSinceStartup;
                }
                return;
            }

            if (phase == 2 && EditorApplication.timeSinceStartup - phaseStart > 2.0)
            {
                File.Delete(FlagPath);
                EditorApplication.update -= Tick;
                EditorApplication.ExitPlaymode();
            }
        }
        catch (Exception ex)
        {
            File.WriteAllText(ReportPath, "ERROR=" + ex + "\n");
            if (File.Exists(FlagPath)) File.Delete(FlagPath);
            EditorApplication.update -= Tick;
            EditorApplication.ExitPlaymode();
        }
    }

    private static void Setup()
    {
        interaction = Resources.FindObjectsOfTypeAll<Stage2WaterInteraction>()
            .FirstOrDefault(x => x != null && x.gameObject.scene.IsValid());
        volume = Resources.FindObjectsOfTypeAll<Stage2ShallowWaterVolume>()
            .FirstOrDefault(x => x != null && x.gameObject.scene.IsValid());
        if (interaction == null || volume == null) throw new InvalidOperationException("Runtime water pieces missing.");

        controller = interaction.GetComponent<CharacterController>();
        animator = interaction.GetComponent<Animator>();
        movement = interaction.GetComponent<MovementofPlayer>();
        GameObject map = GameObject.Find("STAGE2MAP");
        if (controller == null || animator == null || movement == null || map == null)
            throw new InvalidOperationException("Player/map pieces missing.");

        Renderer[] renderers = map.GetComponentsInChildren<Renderer>(true).Where(r => !(r is ParticleSystemRenderer)).ToArray();
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

        if (!TryFindDepthPoint(map.transform, bounds, volume.WaterY, 7f, out Vector3 point, out float floorY))
            throw new InvalidOperationException("No ~7-unit hip-depth point found.");

        Transform left = FindDeepChild(interaction.transform, "mixamorig:LeftFoot");
        Transform right = FindDeepChild(interaction.transform, "mixamorig:RightFoot");
        float rootY = interaction.transform.position.y;
        float footOffset = Mathf.Min(left.position.y, right.position.y) - rootY;

        movement.disableMovement = true;
        controller.enabled = false;
        interaction.transform.position = new Vector3(point.x, floorY - footOffset + 0.18f, point.z);
        controller.enabled = true;
        Physics.SyncTransforms();
        controller.Move(Vector3.down * 0.5f);

        moveDirection = interaction.transform.forward;
        moveDirection.y = 0f;
        moveDirection.Normalize();
    }

    private static void SetMovementState(Vector2 input, bool running)
    {
        if (movement == null) return;
        movement.disableMovement = false;

        FieldInfo inputField = typeof(MovementofPlayer).GetField("movementInput", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo runningField = typeof(MovementofPlayer).GetField("isRunning", BindingFlags.Instance | BindingFlags.NonPublic);
        inputField?.SetValue(movement, input);
        runningField?.SetValue(movement, running);
    }

    private static void InvokeContinuousVfx(Vector3 horizontalVelocity)
    {
        MethodInfo method = typeof(Stage2WaterInteraction).GetMethod(
            "EmitContinuousMovementVfx",
            BindingFlags.Instance | BindingFlags.NonPublic);
        method?.Invoke(interaction, new object[] { horizontalVelocity });
    }

    private static void CaptureReport()
    {
        GameObject pool = GameObject.Find("Stage2_WaterVfxPool_Runtime");
        Camera cam = Camera.main;
        Transform hip = FindDeepChild(interaction.transform, "WaterInteraction_Hip") ?? FindDeepChild(interaction.transform, "mixamorig:Hips");
        int rippleCount = 0;
        int visibleRippleCount = 0;
        string rippleLines = "";

        if (pool != null)
        {
            foreach (MeshRenderer r in pool.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.gameObject.activeSelf && r.gameObject.name.StartsWith("Ripple_", StringComparison.Ordinal)))
            {
                rippleCount++;
                bool visible = cam != null && GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(cam), r.bounds);
                if (visible) visibleRippleCount++;
                rippleLines += $"{r.gameObject.name} pos={r.transform.position} scale={r.transform.localScale} bounds={r.bounds.size} frustum={visible} enabled={r.enabled} queue={r.sharedMaterial?.renderQueue}\n";
            }
        }

        string text =
            $"PlayerPos={interaction.transform.position}\n" +
            $"HipPos={(hip != null ? hip.position.ToString() : "missing")}\n" +
            $"WaterY={volume.WaterY:F3}\n" +
            $"CameraPos={(cam != null ? cam.transform.position.ToString() : "missing")}\n" +
            $"CameraMask={(cam != null ? cam.cullingMask.ToString() : "missing")}\n" +
            $"PoolExists={pool != null}\n" +
            $"ActiveRipples={rippleCount}\n" +
            $"RipplesInsideCameraFrustum={visibleRippleCount}\n" +
            $"DebugMeasuredSpeed={ReadField<float>(interaction, "debugMeasuredSpeed"):F3}\n" +
            $"DebugAnimatorSpeed={ReadField<float>(interaction, "debugAnimatorSpeed"):F3}\n" +
            $"DebugHipLevel={ReadField<bool>(interaction, "debugHipLevel")}\n" +
            $"DebugContinuousEmitCount={ReadField<int>(interaction, "debugContinuousEmitCount")}\n" +
            $"DebugContinuousReject={ReadField<string>(interaction, "debugContinuousReject")}\n" +
            rippleLines;
        File.WriteAllText(ReportPath, text);
        Debug.Log("[Stage2WaterVfxVisualCaptureAudit]\n" + text);
    }

    private static T ReadField<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null) return default;
        object value = field.GetValue(target);
        return value is T typed ? typed : default;
    }

    private static bool TryFindDepthPoint(Transform mapRoot, Bounds bounds, float waterY, float targetDepth, out Vector3 point, out float floorY)
    {
        point = default; floorY = 0f;
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
                if (d < 5.5f || d > 8.5f) continue;
                float e = Mathf.Abs(d - targetDepth);
                if (e >= best) continue;
                best = e; point = new Vector3(x, waterY, z); floorY = y;
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
}
#endif
