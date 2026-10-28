#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class Stage2AdaptiveWaterlineSmokeTest
{
    private const string FlagPath = "RecoveryBackups/RUN_STAGE2_ADAPTIVE_WATERLINE_SMOKE.flag";
    private const string ReportPath = "RecoveryBackups/Stage2AdaptiveWaterlineSmoke_20260919.txt";
    private static double enteredPlayTime;
    private static bool queued;

    static Stage2AdaptiveWaterlineSmokeTest()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.delayCall += MaybeStart;
    }

    private static void MaybeStart()
    {
        if (!File.Exists(FlagPath) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;

        EditorApplication.EnterPlaymode();
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (!File.Exists(FlagPath))
            return;

        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            enteredPlayTime = EditorApplication.timeSinceStartup;
            queued = true;
            EditorApplication.update += Tick;
        }
    }

    private static void Tick()
    {
        if (!queued || EditorApplication.timeSinceStartup - enteredPlayTime < 2.0)
            return;

        queued = false;
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
            if (File.Exists(FlagPath))
                File.Delete(FlagPath);
            EditorApplication.delayCall += EditorApplication.ExitPlaymode;
        }
    }

    private static void Run()
    {
        Stage2WaterInteraction interaction = Resources.FindObjectsOfTypeAll<Stage2WaterInteraction>()
            .FirstOrDefault(candidate => candidate != null && candidate.gameObject.scene.IsValid());
        Stage2ShallowWaterVolume volume = Resources.FindObjectsOfTypeAll<Stage2ShallowWaterVolume>()
            .FirstOrDefault(candidate => candidate != null && candidate.gameObject.scene.IsValid());
        if (interaction == null || volume == null)
            throw new InvalidOperationException($"Adaptive waterline runtime pieces missing: interaction={interaction != null}, volume={volume != null}.");

        Transform leftFoot = FindDeepChild(interaction.transform, "mixamorig:LeftFoot");
        Transform rightFoot = FindDeepChild(interaction.transform, "mixamorig:RightFoot");
        Transform hipAnchor = FindDeepChild(interaction.transform, "WaterInteraction_Hip");
        if (leftFoot == null || rightFoot == null || hipAnchor == null)
            throw new InvalidOperationException("Adaptive waterline anchors are incomplete.");

        MethodInfo routeMethod = typeof(Stage2WaterInteraction).GetMethod("TryGetAdaptiveWaterlinePosition", BindingFlags.Instance | BindingFlags.NonPublic);
        if (routeMethod == null)
            throw new MissingMethodException("Stage2WaterInteraction.TryGetAdaptiveWaterlinePosition was not found.");

        interaction.EnterWaterVolume(volume);

        CharacterController controller = interaction.GetComponent<CharacterController>();
        Vector3 originalPosition = interaction.transform.position;
        bool originalControllerEnabled = controller != null && controller.enabled;

        try
        {
            if (controller != null)
                controller.enabled = false;

            GameObject map = GameObject.Find("STAGE2MAP");
            if (map == null)
                throw new InvalidOperationException("STAGE2MAP is missing in Play Mode.");

            Renderer[] renderers = map.GetComponentsInChildren<Renderer>(true)
                .Where(r => !(r is ParticleSystemRenderer))
                .ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException("STAGE2MAP has no renderers.");

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            if (!TryFindNearestInteractivePoint(volume, bounds, originalPosition, out Vector3 waterPoint))
                throw new InvalidOperationException("Could not find a valid shallow-water X/Z sample.");

            Vector3 routedRoot = interaction.transform.position;
            routedRoot.x = waterPoint.x;
            routedRoot.z = waterPoint.z;
            interaction.transform.position = routedRoot;
            Physics.SyncTransforms();

            float rootY = interaction.transform.position.y;
            float footOffset = Mathf.Min(leftFoot.position.y, rightFoot.position.y) - rootY;
            float hipOffset = hipAnchor.position.y - rootY;
            float span = Mathf.Max(0.15f, hipOffset - footOffset);

            bool shallowPass = TestLevel(interaction, routeMethod, leftFoot, volume.WaterY, footOffset, span, 0.20f,
                out bool shallowHipMode, out Vector3 shallowPosition);
            bool hipPass = TestLevel(interaction, routeMethod, leftFoot, volume.WaterY, footOffset, span, 0.82f,
                out bool hipMode, out Vector3 hipPosition);

            float expectedSurfaceY = volume.WaterY + 0.025f;
            bool yPass = Mathf.Abs(shallowPosition.y - expectedSurfaceY) < 0.05f
                         && Mathf.Abs(hipPosition.y - expectedSurfaceY) < 0.05f;
            bool modePass = shallowPass && !shallowHipMode && hipPass && hipMode;
            bool hipAnchorPass = new Vector2(hipPosition.x - hipAnchor.position.x, hipPosition.z - hipAnchor.position.z).magnitude < 0.25f;
            int meshColliders = UnityEngine.Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            bool pass = modePass && yPass && hipAnchorPass && meshColliders == 409;

            string report =
                $"PASS={pass}\n" +
                $"WaterY={volume.WaterY:F3}\n" +
                $"TestWaterPoint={waterPoint}\n" +
                $"FootToHipSpan={span:F3}\n" +
                $"ShallowRouteReturned={shallowPass}\n" +
                $"ShallowHipMode={shallowHipMode}\n" +
                $"ShallowSurface={shallowPosition}\n" +
                $"HipRouteReturned={hipPass}\n" +
                $"HipMode={hipMode}\n" +
                $"HipSurface={hipPosition}\n" +
                $"HipAnchor={hipAnchor.position}\n" +
                $"ExpectedSurfaceY={expectedSurfaceY:F3}\n" +
                $"RuntimeMeshColliders={meshColliders}\n";

            File.WriteAllText(ReportPath, report);
            Debug.Log("[Stage2AdaptiveWaterlineSmokeTest]\n" + report);

            if (!pass)
                throw new InvalidOperationException("Adaptive waterline routing smoke test failed. See report.");
        }
        finally
        {
            interaction.transform.position = originalPosition;
            if (controller != null)
                controller.enabled = originalControllerEnabled;
            interaction.ExitWaterVolume(volume);
            Physics.SyncTransforms();
        }
    }

    private static bool TryFindNearestInteractivePoint(Stage2ShallowWaterVolume volume, Bounds bounds, Vector3 origin, out Vector3 point)
    {
        point = default;
        float bestSqrDistance = float.PositiveInfinity;
        const int grid = 61;

        for (int iz = 0; iz < grid; iz++)
        {
            float z = Mathf.Lerp(bounds.min.z, bounds.max.z, (iz + 0.5f) / grid);
            for (int ix = 0; ix < grid; ix++)
            {
                float x = Mathf.Lerp(bounds.min.x, bounds.max.x, (ix + 0.5f) / grid);
                Vector3 candidate = new Vector3(x, volume.WaterY, z);
                if (!volume.CanSplashAt(candidate, out Vector3 surface))
                    continue;

                float sqr = (new Vector2(x, z) - new Vector2(origin.x, origin.z)).sqrMagnitude;
                if (sqr >= bestSqrDistance)
                    continue;

                bestSqrDistance = sqr;
                point = surface;
            }
        }

        return !float.IsPositiveInfinity(bestSqrDistance);
    }

    private static bool TestLevel(Stage2WaterInteraction interaction, MethodInfo routeMethod, Transform contact,
        float waterY, float footOffset, float span, float normalizedWaterLevel,
        out bool hipMode, out Vector3 surfacePosition)
    {
        Vector3 pos = interaction.transform.position;
        pos.y = waterY - (footOffset + span * normalizedWaterLevel);
        interaction.transform.position = pos;
        Physics.SyncTransforms();

        object[] args = { contact, Vector3.zero, false };
        bool returned = (bool)routeMethod.Invoke(interaction, args);
        surfacePosition = (Vector3)args[1];
        hipMode = (bool)args[2];
        return returned;
    }

    private static Transform FindDeepChild(Transform root, string targetName)
    {
        if (root.name == targetName)
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeepChild(root.GetChild(i), targetName);
            if (found != null)
                return found;
        }
        return null;
    }
}
#endif
