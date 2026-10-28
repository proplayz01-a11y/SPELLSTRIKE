#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class Stage2WaterInteractionSmokeTest
{
    private const string FlagPath = "RecoveryBackups/RUN_STAGE2_WATER_INTERACTION_SMOKE.flag";
    private const string ReportPath = "RecoveryBackups/Stage2WaterInteractionSmoke_20260919.txt";
    private static double enteredPlayTime;
    private static bool queued;

    static Stage2WaterInteractionSmokeTest()
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
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != "Stage2Scene")
            throw new InvalidOperationException($"Expected Stage2Scene, got '{scene.name}'.");

        Stage2WaterInteraction interaction = UnityEngine.Object.FindFirstObjectByType<Stage2WaterInteraction>();
        Stage2ShallowWaterVolume volume = UnityEngine.Object.FindFirstObjectByType<Stage2ShallowWaterVolume>();
        GameObject map = GameObject.Find("STAGE2MAP");
        if (interaction == null || volume == null || map == null)
            throw new InvalidOperationException("Water interaction, runtime volume, or STAGE2MAP is missing.");

        CharacterController controller = interaction.GetComponent<CharacterController>();
        Animator animator = interaction.GetComponent<Animator>();
        MovementofPlayer movement = interaction.GetComponent<MovementofPlayer>();
        if (controller == null || animator == null || movement == null)
            throw new InvalidOperationException("Van Aert is missing CharacterController, Animator, or MovementofPlayer.");

        Renderer[] renderers = map.GetComponentsInChildren<Renderer>(true)
            .Where(r => !(r is ParticleSystemRenderer))
            .ToArray();
        if (renderers.Length == 0)
            throw new InvalidOperationException("STAGE2MAP has no renderers.");

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        Vector3 originalPosition = interaction.transform.position;
        Quaternion originalRotation = interaction.transform.rotation;
        bool originalDisableMovement = movement.disableMovement;

        if (!TryFindNearestShallowPoint(volume, map.transform, bounds, originalPosition, out Vector3 surfacePoint, out float floorY))
            throw new InvalidOperationException("Could not find a walkable shallow-water sample in STAGE2MAP.");

        movement.disableMovement = true;
        controller.enabled = false;
        interaction.transform.position = new Vector3(surfacePoint.x, floorY + 0.12f, surfacePoint.z);
        controller.enabled = true;
        Physics.SyncTransforms();

        // Settle onto the submerged terrain, then create real CharacterController
        // horizontal velocity so the production stationary-suppression path is tested.
        controller.Move(Vector3.down * 0.35f);
        interaction.EnterWaterVolume(volume);
        animator.SetFloat("Speed", 0.5f);
        controller.Move(interaction.transform.forward * 0.35f);

        bool groundedAtTest = controller.isGrounded;
        float speedAtTest = new Vector3(controller.velocity.x, 0f, controller.velocity.z).magnitude;
        interaction.WaterFootstepLeft();

        GameObject poolRoot = GameObject.Find("Stage2_WaterVfxPool_Runtime");
        if (poolRoot == null)
            throw new InvalidOperationException("Water VFX pool was not created after entering the shallow-water volume.");

        int footstepParticles = poolRoot.GetComponentsInChildren<ParticleSystem>(true).Count(ps => ps.isPlaying);
        int footstepRipples = poolRoot.GetComponentsInChildren<MeshRenderer>(true).Count(r => r.gameObject.activeSelf && r.gameObject.name.StartsWith("Ripple_", StringComparison.Ordinal));

        controller.Move(interaction.transform.right * 0.25f);
        animator.SetFloat("Speed", 1f);
        interaction.WaterFootstepRight();
        int runParticles = poolRoot.GetComponentsInChildren<ParticleSystem>(true).Count(ps => ps.isPlaying);
        int runRipples = poolRoot.GetComponentsInChildren<MeshRenderer>(true).Count(r => r.gameObject.activeSelf && r.gameObject.name.StartsWith("Ripple_", StringComparison.Ordinal));

        MethodInfo landingMethod = typeof(Stage2WaterInteraction).GetMethod("EmitLandingSplash", BindingFlags.Instance | BindingFlags.NonPublic);
        if (landingMethod == null)
            throw new MissingMethodException("Stage2WaterInteraction.EmitLandingSplash was not found.");
        landingMethod.Invoke(interaction, null);

        int landingParticles = poolRoot.GetComponentsInChildren<ParticleSystem>(true).Count(ps => ps.isPlaying);
        int landingRipples = poolRoot.GetComponentsInChildren<MeshRenderer>(true).Count(r => r.gameObject.activeSelf && r.gameObject.name.StartsWith("Ripple_", StringComparison.Ordinal));
        int runtimeMeshColliders = UnityEngine.Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;

        bool pass = groundedAtTest
                    && speedAtTest >= 0.8f
                    && footstepParticles >= 1
                    && footstepRipples >= 1
                    && runParticles >= 1
                    && runRipples >= 2
                    && landingParticles >= 1
                    && landingRipples >= 3
                    && runtimeMeshColliders == 409;

        string report =
            $"PASS={pass}\n" +
            $"Scene={scene.name}\n" +
            $"WaterY={volume.WaterY:F3}\n" +
            $"TestSurfacePoint={surfacePoint}\n" +
            $"TestFloorY={floorY:F3}\n" +
            $"GroundedAtTest={groundedAtTest}\n" +
            $"HorizontalSpeedAtTest={speedAtTest:F3}\n" +
            $"FootstepParticlesPlaying={footstepParticles}\n" +
            $"FootstepRipplesActive={footstepRipples}\n" +
            $"RunParticlesPlaying={runParticles}\n" +
            $"RunRipplesActive={runRipples}\n" +
            $"LandingParticlesPlaying={landingParticles}\n" +
            $"LandingRipplesActive={landingRipples}\n" +
            $"RuntimeMeshColliders={runtimeMeshColliders}\n";

        File.WriteAllText(ReportPath, report);
        Debug.Log("[Stage2WaterInteractionSmokeTest]\n" + report);

        interaction.ExitWaterVolume(volume);
        controller.enabled = false;
        interaction.transform.SetPositionAndRotation(originalPosition, originalRotation);
        controller.enabled = true;
        movement.disableMovement = originalDisableMovement;
        Physics.SyncTransforms();

        if (!pass)
            throw new InvalidOperationException("Stage 2 water interaction smoke test failed. See report for details.");
    }

    private static bool TryFindNearestShallowPoint(Stage2ShallowWaterVolume volume, Transform mapRoot, Bounds bounds, Vector3 origin, out Vector3 surfacePoint, out float floorY)
    {
        surfacePoint = default;
        floorY = 0f;
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
                if (!TryGetMapFloor(mapRoot, x, z, volume.WaterY, out float candidateFloorY))
                    continue;

                float depth = volume.WaterY - candidateFloorY;
                if (depth < 0.15f || depth > 2.35f)
                    continue;

                float sqr = (new Vector2(x, z) - new Vector2(origin.x, origin.z)).sqrMagnitude;
                if (sqr >= bestSqrDistance)
                    continue;

                bestSqrDistance = sqr;
                surfacePoint = surface;
                floorY = candidateFloorY;
            }
        }

        return !float.IsPositiveInfinity(bestSqrDistance);
    }

    private static bool TryGetMapFloor(Transform mapRoot, float x, float z, float waterY, out float floorY)
    {
        floorY = float.NegativeInfinity;
        RaycastHit[] hits = Physics.RaycastAll(new Vector3(x, waterY + 10f, z), Vector3.down, 30f, ~0, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null || !hit.collider.transform.IsChildOf(mapRoot))
                continue;
            if (hit.point.y <= waterY + 0.02f && hit.point.y > floorY)
                floorY = hit.point.y;
        }

        return !float.IsNegativeInfinity(floorY);
    }
}
#endif
