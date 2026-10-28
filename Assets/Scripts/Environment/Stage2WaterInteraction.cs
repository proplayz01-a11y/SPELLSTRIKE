using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public sealed class Stage2WaterInteraction : MonoBehaviour
{
    [Header("VFX Materials")]
    [SerializeField] private Material splashMaterial;
    [SerializeField] private Material rippleMaterial;

    [Header("Behaviour")]
    [SerializeField, Min(0f)] private float minimumFootstepSpeed = 0.8f;
    [SerializeField, Min(0f)] private float landingVelocityThreshold = 2.0f;
    [SerializeField, Range(0.35f, 0.9f)] private float hipModeThreshold = 0.60f;
    [SerializeField, Min(0f)] private float waterSurfaceOffset = 0.025f;
    [Header("Water Movement Resistance")]
    [SerializeField, Range(0.1f, 1f)] private float hipMovementMultiplier = 0.72f;
    [SerializeField, Range(0.1f, 1f)] private float headMovementMultiplier = 0.48f;
    [SerializeField, Range(0.1f, 1f)] private float overheadMovementMultiplier = 0.30f;
    [SerializeField, Range(0f, 0.35f)] private float headLevelBodyOffset = 0.10f;
    [SerializeField, Range(0f, 0.35f)] private float overheadBodyOffset = 0.10f;
    [SerializeField, Min(1)] private int splashPoolSize = 8;
    [SerializeField, Min(1)] private int ripplePoolSize = 24;

    private CharacterController controller;
    private Animator animator;
    private MovementofPlayer movement;
    private Transform leftFoot;
    private Transform rightFoot;
    private Transform hips;
    private Transform head;
    private Stage2ShallowWaterVolume activeVolume;
    private Stage2ShallowWaterVolume fallbackVolume;
    private bool wasGrounded;
    private float previousVerticalVelocity;
    private float lastContinuousSplashTime = -10f;
    private Vector3 lastWakePosition;
    private bool hasLastWakePosition;
    private Vector3 previousWorldPosition;
    private Vector3 measuredHorizontalVelocity;
    private float debugMeasuredSpeed;
    private float debugAnimatorSpeed;
    private bool debugHipLevel;
    private int debugContinuousEmitCount;
    private string debugContinuousReject = "not-run";
    private float nextVolumeResolveTime;
    private GameObject poolRoot;
    private readonly List<ParticleSystem> splashPool = new();
    private readonly List<RippleSlot> ripplePool = new();
    private int splashCursor;
    private int rippleCursor;
    private static Mesh rippleMesh;

    private sealed class RippleSlot
    {
        public GameObject gameObject;
        public MeshRenderer renderer;
        public MaterialPropertyBlock propertyBlock;
        public float startTime;
        public float duration;
        public float startScale;
        public float endScale;
        public bool active;
    }

    public void Configure(Material splash, Material ripple)
    {
        splashMaterial = splash;
        rippleMaterial = ripple;
    }

    public void RegisterStage2Volume(Stage2ShallowWaterVolume volume)
    {
        fallbackVolume = volume;
        if (fallbackVolume != null)
            EnsurePools();
    }

    public void EnterWaterVolume(Stage2ShallowWaterVolume volume)
    {
        activeVolume = volume;
        hasLastWakePosition = false;
        lastContinuousSplashTime = -10f;
        EnsurePools();
    }

    public void ExitWaterVolume(Stage2ShallowWaterVolume volume)
    {
        if (activeVolume == volume)
        {
            activeVolume = null;
            hasLastWakePosition = false;
        }
    }

    private Stage2ShallowWaterVolume CurrentVolume => activeVolume != null ? activeVolume : fallbackVolume;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        movement = GetComponent<MovementofPlayer>();
        leftFoot = FindDeepChild(transform, "mixamorig:LeftFoot");
        rightFoot = FindDeepChild(transform, "mixamorig:RightFoot");
        hips = FindDeepChild(transform, "WaterInteraction_Hip") ?? FindDeepChild(transform, "mixamorig:Hips");
        head = FindDeepChild(transform, "mixamorig:Head");
        previousWorldPosition = transform.position;
        wasGrounded = controller.isGrounded;
        previousVerticalVelocity = controller.velocity.y;

        // Stage 2 water VFX should exist independently of trigger callbacks.
        // Build the lightweight pools as soon as the Stage 2 player wakes up.
        if (gameObject.scene.IsValid() && gameObject.scene.name == "Stage2Scene")
            EnsurePools();
    }

    private void Update()
    {
        UpdateRipples();

        bool grounded = controller.isGrounded;
        if (!wasGrounded && grounded && previousVerticalVelocity <= -landingVelocityThreshold)
            EmitLandingSplash();

        previousVerticalVelocity = controller.velocity.y;
        wasGrounded = grounded;
    }

    private void LateUpdate()
    {
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        Vector3 delta = transform.position - previousWorldPosition;
        delta.y = 0f;
        measuredHorizontalVelocity = delta / dt;
        previousWorldPosition = transform.position;

        ResolveFallbackVolumeIfNeeded();
        EmitContinuousMovementVfx(measuredHorizontalVelocity);
    }

    private void ResolveFallbackVolumeIfNeeded()
    {
        if (CurrentVolume != null || Time.unscaledTime < nextVolumeResolveTime)
            return;

        nextVolumeResolveTime = Time.unscaledTime + 0.5f;
        Stage2ShallowWaterVolume[] volumes = Resources.FindObjectsOfTypeAll<Stage2ShallowWaterVolume>();
        for (int i = 0; i < volumes.Length; i++)
        {
            Stage2ShallowWaterVolume candidate = volumes[i];
            if (candidate == null || !candidate.gameObject.scene.IsValid())
                continue;
            if (candidate.gameObject.scene != gameObject.scene)
                continue;

            fallbackVolume = candidate;
            EnsurePools();
            return;
        }
    }

    public void WaterFootstepLeft() => EmitFootstep(leftFoot);
    public void WaterFootstepRight() => EmitFootstep(rightFoot);

    public float GetMovementSpeedMultiplier()
    {
        Stage2ShallowWaterVolume volume = CurrentVolume;
        if (volume == null || hips == null || head == null)
            return 1f;

        float leftY = leftFoot != null ? leftFoot.position.y : transform.position.y;
        float rightY = rightFoot != null ? rightFoot.position.y : transform.position.y;
        float footY = Mathf.Min(leftY, rightY);
        float hipY = hips.position.y;
        float headY = head.position.y;
        float bodyHeight = Mathf.Max(0.5f, headY - footY);
        float waterY = volume.WaterY;

        // Match the same perceived "hip-deep" threshold used by the wake VFX,
        // rather than waiting for the water plane to reach the exact hip bone.
        float hipLevelY = Mathf.Lerp(footY, hipY, hipModeThreshold);
        float headLevelY = headY - bodyHeight * headLevelBodyOffset;
        float overheadLevelY = headY + bodyHeight * overheadBodyOffset;

        if (waterY < hipLevelY)
            return 1f;

        if (waterY < headLevelY)
        {
            float t = Mathf.InverseLerp(hipLevelY, headLevelY, waterY);
            return Mathf.Lerp(hipMovementMultiplier, headMovementMultiplier, t);
        }

        if (waterY < overheadLevelY)
        {
            float t = Mathf.InverseLerp(headLevelY, overheadLevelY, waterY);
            return Mathf.Lerp(headMovementMultiplier, overheadMovementMultiplier, t);
        }

        return overheadMovementMultiplier;
    }

    private void EmitFootstep(Transform foot)
    {
        if (CurrentVolume == null || foot == null || !controller.isGrounded)
            return;

        Vector3 horizontalVelocity = controller.velocity;
        horizontalVelocity.y = 0f;
        float speed = Mathf.Max(horizontalVelocity.magnitude, measuredHorizontalVelocity.magnitude);
        if (speed < minimumFootstepSpeed)
            return;

        if (!TryGetAdaptiveWaterlinePosition(foot, out Vector3 surfacePosition, out bool hipLevel))
            return;

        float animatorSpeed = animator != null ? animator.GetFloat("Speed") : 0f;
        float walkReference = movement != null ? Mathf.Max(0.1f, movement.walkSpeed) : 6f;
        bool running = animatorSpeed >= 0.72f
                       || (movement != null && movement.IsRunning)
                       || speed >= walkReference * 1.25f;
        float levelMultiplier = hipLevel ? 1.75f : 1f;
        EmitSplash(surfacePosition, (running ? 2.35f : 1.35f) * levelMultiplier, running ? (hipLevel ? 26 : 18) : (hipLevel ? 14 : 8));
        EmitRipple(surfacePosition, (running ? 2.25f : 1.25f) * levelMultiplier, running ? 0.82f : 0.68f);
    }

    private void EmitContinuousMovementVfx(Vector3 horizontalVelocity)
    {
        debugMeasuredSpeed = horizontalVelocity.magnitude;
        debugAnimatorSpeed = animator != null ? animator.GetFloat("Speed") : 0f;
        debugHipLevel = false;

        if (CurrentVolume == null)
        {
            debugContinuousReject = "no-volume";
            return;
        }

        float speed = horizontalVelocity.magnitude;
        float animatorSpeed = debugAnimatorSpeed;
        bool moving = speed >= 0.15f;
        if (!moving)
        {
            debugContinuousReject = "no-world-displacement";
            return;
        }

        float walkReference = movement != null ? Mathf.Max(0.1f, movement.walkSpeed) : 6f;
        bool running = animatorSpeed >= 0.72f
                       || (movement != null && movement.IsRunning)
                       || speed >= walkReference * 1.25f;

        if (!TryGetContinuousWaterline(out Vector3 surfacePosition, out bool hipLevel))
        {
            debugContinuousReject = "body-not-intersecting-water";
            return;
        }
        debugHipLevel = hipLevel;

        // Animation events remain the precise shallow-water footsteps. This periodic
        // pass guarantees a visible wake while running and a body wake in hip-deep water.
        if (!running && !hipLevel)
        {
            debugContinuousReject = "not-running-and-not-hip";
            return;
        }

        float interval = running ? (hipLevel ? 0.12f : 0.14f) : 0.28f;
        float elapsed = Time.time - lastContinuousSplashTime;

        // World displacement is authoritative. Cadence alone controls the wake so
        // input/Animator/controller bookkeeping cannot silently disable water VFX.
        if (elapsed < interval)
        {
            debugContinuousReject = "cadence-wait";
            return;
        }
        lastContinuousSplashTime = Time.time;
        lastWakePosition = surfacePosition;
        hasLastWakePosition = true;
        debugContinuousReject = "emitted";
        debugContinuousEmitCount++;

        if (hipLevel)
        {
            EmitSplash(surfacePosition, running ? 4.5f : 3.0f, running ? 36 : 22);
            EmitRipple(surfacePosition, running ? 4.9f : 4.1f, running ? 1.18f : 0.96f);
            if (running)
                EmitTrailingRipple(surfacePosition, horizontalVelocity, 3.6f, 1.02f);
        }
        else if (running)
        {
            EmitSplash(surfacePosition, 2.5f, 20);
            EmitRipple(surfacePosition, 2.8f, 0.88f);
            EmitTrailingRipple(surfacePosition, horizontalVelocity, 2.1f, 0.78f);
        }
    }

    private bool TryGetContinuousWaterline(out Vector3 surfacePosition, out bool hipLevel)
    {
        surfacePosition = default;
        hipLevel = false;

        Stage2ShallowWaterVolume volume = CurrentVolume;
        if (volume == null)
            return false;

        float waterY = volume.WaterY;
        Vector3 left = leftFoot != null ? leftFoot.position : transform.position;
        Vector3 right = rightFoot != null ? rightFoot.position : transform.position;
        Vector3 hip = hips != null ? hips.position : transform.position + Vector3.up * controller.height * 0.5f;

        float footY = Mathf.Min(left.y, right.y);
        float hipY = hip.y;

        // Continuous wake is based on the body's actual intersection with the flat
        // Stage 2 water surface. It deliberately does not depend on seabed depth or
        // trigger callbacks; those were proven unreliable for this visual effect.
        if (waterY < footY - 0.20f || waterY > hipY + 1.0f)
            return false;

        float submersion01 = Mathf.InverseLerp(footY, Mathf.Max(footY + 0.01f, hipY), waterY);
        hipLevel = submersion01 >= hipModeThreshold;

        Vector3 center = hipLevel
            ? hip
            : (left + right) * 0.5f;
        surfacePosition = new Vector3(center.x, waterY + waterSurfaceOffset, center.z);
        return true;
    }

    private void EmitTrailingRipple(Vector3 surfacePosition, Vector3 horizontalVelocity, float scale, float duration)
    {
        Vector3 direction = horizontalVelocity.sqrMagnitude >= 0.01f
            ? horizontalVelocity.normalized
            : transform.forward;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
            direction = Vector3.forward;

        Vector3 behind = -direction.normalized;
        Vector3 trailingPosition = surfacePosition + behind * 0.42f;
        EmitRipple(trailingPosition, scale, duration);
    }

    private bool IsWaterAtHipLevel(out Vector3 surfacePosition)
    {
        surfacePosition = default;
        Stage2ShallowWaterVolume volume = CurrentVolume;
        if (volume == null || hips == null)
            return false;

        float waterY = volume.WaterY;
        float leftY = leftFoot != null ? leftFoot.position.y : transform.position.y;
        float rightY = rightFoot != null ? rightFoot.position.y : transform.position.y;
        float footY = Mathf.Min(leftY, rightY);
        float hipY = hips.position.y;
        float thresholdY = Mathf.Lerp(footY, hipY, hipModeThreshold);

        if (waterY < thresholdY)
            return false;

        surfacePosition = new Vector3(hips.position.x, waterY + waterSurfaceOffset, hips.position.z);
        return true;
    }

    private void EmitLandingSplash()
    {
        if (CurrentVolume == null)
            return;

        Transform landingProbe = hips != null ? hips : transform;
        if (!TryGetAdaptiveWaterlinePosition(landingProbe, out Vector3 surfacePosition, out bool hipLevel))
            return;

        float levelMultiplier = hipLevel ? 1.2f : 1f;
        EmitSplash(surfacePosition, 1.9f * levelMultiplier, hipLevel ? 15 : 12);
        EmitRipple(surfacePosition, 2.15f * levelMultiplier, 0.88f);
    }

    private bool TryGetAdaptiveWaterlinePosition(Transform contactSource, out Vector3 surfacePosition, out bool hipLevel)
    {
        surfacePosition = default;
        hipLevel = false;

        Stage2ShallowWaterVolume volume = CurrentVolume;
        if (volume == null || contactSource == null)
            return false;

        float waterY = volume.WaterY;
        float leftY = leftFoot != null ? leftFoot.position.y : transform.position.y;
        float rightY = rightFoot != null ? rightFoot.position.y : transform.position.y;
        float footReferenceY = Mathf.Min(leftY, rightY);
        float hipReferenceY = hips != null ? hips.position.y : transform.position.y + controller.height * 0.5f;

        float submersion01 = Mathf.InverseLerp(footReferenceY, hipReferenceY, waterY);
        hipLevel = submersion01 >= hipModeThreshold;

        Vector3 probePosition;
        if (hipLevel)
        {
            // Once the water reaches the upper legs/hips, suppress foot-position
            // splashes and create one body-centered splash at the true waterline.
            Transform hipSource = hips != null ? hips : transform;
            probePosition = new Vector3(hipSource.position.x, waterY, hipSource.position.z);
        }
        else
        {
            // In shallow water, preserve individual left/right foot placement but
            // still place the VFX on the actual water surface rather than the bone Y.
            probePosition = new Vector3(contactSource.position.x, waterY, contactSource.position.z);
        }

        if (!volume.CanSplashAt(probePosition, out Vector3 validatedSurfacePosition))
            return false;

        surfacePosition = new Vector3(validatedSurfacePosition.x, waterY + waterSurfaceOffset, validatedSurfacePosition.z);
        return true;
    }

    private void EmitSplash(Vector3 position, float intensity, int particleCount)
    {
        if (splashPool.Count == 0)
            return;

        ParticleSystem ps = splashPool[splashCursor++ % splashPool.Count];
        ps.transform.position = position;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.36f, 0.62f * Mathf.Sqrt(intensity));
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.4f * intensity, 3.15f * intensity);
        main.startSize = new ParticleSystem.MinMaxCurve(0.11f * intensity, 0.24f * intensity);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.65f, 0.95f, 1f, 0.72f),
            new Color(0.85f, 1f, 1f, 0.95f));

        var emission = ps.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Clamp(particleCount, 1, short.MaxValue)) });
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        // Unity requires x/y/z velocity curves to use the same MinMaxCurve mode.
        // Keep all three as TwoConstants; Y stays neutral because startSpeed + cone
        // already supplies the upward component.
        velocity.x = new ParticleSystem.MinMaxCurve(-0.9f * intensity, 0.9f * intensity);
        velocity.y = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.9f * intensity, 0.9f * intensity);
        ps.Play(true);
    }

    private void EmitRipple(Vector3 position, float scaleMultiplier, float duration)
    {
        if (ripplePool.Count == 0)
            return;

        RippleSlot slot = ripplePool[rippleCursor++ % ripplePool.Count];
        slot.gameObject.transform.position = position;
        slot.startTime = Time.time;
        slot.duration = duration;
        slot.startScale = 0.55f * scaleMultiplier;
        slot.endScale = 2.65f * scaleMultiplier;
        slot.active = true;
        slot.gameObject.SetActive(true);
        slot.gameObject.transform.localScale = Vector3.one * slot.startScale;
    }

    private void UpdateRipples()
    {
        for (int i = 0; i < ripplePool.Count; i++)
        {
            RippleSlot slot = ripplePool[i];
            if (!slot.active)
                continue;

            float t = Mathf.Clamp01((Time.time - slot.startTime) / Mathf.Max(0.01f, slot.duration));
            float eased = 1f - Mathf.Pow(1f - t, 2f);
            float scale = Mathf.Lerp(slot.startScale, slot.endScale, eased);
            slot.gameObject.transform.localScale = new Vector3(scale, 1f, scale);
            slot.propertyBlock.SetFloat("_Opacity", 1f - t);
            slot.renderer.SetPropertyBlock(slot.propertyBlock);

            if (t >= 1f)
            {
                slot.active = false;
                slot.gameObject.SetActive(false);
            }
        }
    }

    private void BuildPools()
    {
        if (poolRoot != null)
            return;

        poolRoot = new GameObject("Stage2_WaterVfxPool_Runtime");
        poolRoot.hideFlags = HideFlags.DontSave;

        Shader splashShader = splashMaterial != null ? splashMaterial.shader : null;
        if (splashMaterial != null && splashShader != null)
        {
            for (int i = 0; i < splashPoolSize; i++)
            {
                GameObject go = new GameObject($"Splash_{i:00}");
                go.hideFlags = HideFlags.DontSave;
                go.transform.SetParent(poolRoot.transform, false);
                ParticleSystem ps = go.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.loop = false;
                main.playOnAwake = false;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.maxParticles = 40;
                main.gravityModifier = 1.35f;
                var emission = ps.emission;
                emission.rateOverTime = 0f;
                var shape = ps.shape;
                shape.enabled = true;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 38f;
                shape.radius = 0.24f;
                var renderer = ps.GetComponent<ParticleSystemRenderer>();
                renderer.sharedMaterial = splashMaterial;
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.sortingOrder = 30;
                go.SetActive(true);
                splashPool.Add(ps);
            }
        }

        if (rippleMaterial != null)
        {
            Mesh mesh = GetRippleMesh();
            for (int i = 0; i < ripplePoolSize; i++)
            {
                GameObject go = new GameObject($"Ripple_{i:00}");
                go.hideFlags = HideFlags.DontSave;
                go.transform.SetParent(poolRoot.transform, false);
                MeshFilter filter = go.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;
                MeshRenderer renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = rippleMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.sortingOrder = 25;
                go.SetActive(false);
                ripplePool.Add(new RippleSlot
                {
                    gameObject = go,
                    renderer = renderer,
                    propertyBlock = new MaterialPropertyBlock()
                });
            }
        }
    }

    private void EnsurePools()
    {
        if (poolRoot == null)
            BuildPools();
    }

    private static Mesh GetRippleMesh()
    {
        if (rippleMesh != null)
            return rippleMesh;

        rippleMesh = new Mesh { name = "Stage2WaterRipple_RuntimeMesh", hideFlags = HideFlags.DontSave };
        rippleMesh.vertices = new[]
        {
            new Vector3(-0.5f, 0f, -0.5f),
            new Vector3(-0.5f, 0f,  0.5f),
            new Vector3( 0.5f, 0f,  0.5f),
            new Vector3( 0.5f, 0f, -0.5f)
        };
        rippleMesh.uv = new[] { new Vector2(0,0), new Vector2(0,1), new Vector2(1,1), new Vector2(1,0) };
        rippleMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        rippleMesh.RecalculateBounds();
        return rippleMesh;
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

    private void OnDestroy()
    {
        if (poolRoot != null)
            Destroy(poolRoot);
    }
}
