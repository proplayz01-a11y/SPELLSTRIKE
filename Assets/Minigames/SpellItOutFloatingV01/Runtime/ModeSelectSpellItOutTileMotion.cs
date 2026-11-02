using System;
using UnityEngine;

/// <summary>
/// Applies subtle local motion to separately bound tile slots. This component owns
/// only their local position and rotation; scale and render properties are untouched.
/// </summary>
[DisallowMultipleComponent]
public sealed class ModeSelectSpellItOutTileMotion : MonoBehaviour
{
    [Serializable]
    public sealed class TileMotion
    {
        public Transform target;
        [Min(0f)] public float bobAmplitude = 0.05f;
        [Min(0.1f)] public float periodSeconds = 3.2f;
        [Tooltip("Phase in cycles; different values stagger the tiles deterministically.")]
        public float phaseCycles;
        [Tooltip("Maximum local rotation in degrees on each axis.")]
        public Vector3 rotationEuler = new Vector3(0f, 0f, 3f);
        [Range(0f, 0.025f)] public float depthAmplitude;
    }

    [SerializeField] private TileMotion[] tiles = Array.Empty<TileMotion>();
    [SerializeField] private bool useUnscaledTime = true;

    private struct RestPose
    {
        public Transform target;
        public Vector3 position;
        public Quaternion rotation;
    }

    private RestPose[] restPoses;
    private bool captured;
    private bool paused = true;
    public bool IsRunning => isActiveAndEnabled && !paused;
    private const float FadeInSeconds = 0.15f;
    private double epoch;
    private const double TwoPi = Math.PI * 2.0;

    private double Clock => useUnscaledTime ? Time.unscaledTimeAsDouble : Time.timeAsDouble;

    private void OnEnable()
    {
        // Capture only once per configuration. Disabling first restores the pose,
        // so enabling again must never adopt an animated pose as a new baseline.
        CaptureIfNeeded();
        paused = true;
        RestoreRestPoses();
    }

    private void Update()
    {
        if (!paused)
        {
            double elapsed = Clock - epoch;
            SampleAtWeight(elapsed, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((float)(elapsed / FadeInSeconds))));
        }
    }

    private void OnDisable()
    {
        paused = true;
        RestoreRestPoses();
    }

    /// <summary>
    /// Replaces bindings and records their current local poses as the new baseline.
    /// Previous targets are restored before any new pose is captured. Call after
    /// positioning the slots, and do not bind the same transform more than once.
    /// </summary>
    public void Configure(TileMotion[] bindings)
    {
        RestoreRestPoses();
        tiles = bindings ?? Array.Empty<TileMotion>();
        captured = false;
        CaptureIfNeeded();
        paused = true;
    }

    /// <summary>
    /// Restores and releases existing targets. Call before adjusting their layout,
    /// then Configure to capture the adjusted poses as a new baseline.
    /// </summary>
    public void RestoreAndClearBindings()
    {
        RestoreRestPoses();
        tiles = Array.Empty<TileMotion>();
        restPoses = null;
        captured = false;
        paused = true;
    }
    /// <summary>Stops automatic motion and returns all surviving targets to their rest poses.</summary>
    public void PauseAndRestore()
    {
        paused = true;
        RestoreRestPoses();
    }

    /// <summary>Restarts automatic motion from its initial phase and a fresh clock epoch.</summary>
    public void Resume()
    {
        if (!isActiveAndEnabled || !paused)
            return;
        CaptureIfNeeded();
        epoch = Clock;
        paused = false;
    }

    /// <summary>
    /// Applies a deterministic pose for elapsed seconds without changing the clock.
    /// For inspection, call PauseAndRestore first; Resume returns to automatic motion.
    /// </summary>
    public void SampleAt(double elapsedSeconds)
    {
        SampleAtWeight(elapsedSeconds, 1f);
    }

    private void SampleAtWeight(double elapsedSeconds, float weight)
    {
        CaptureIfNeeded();
        if (double.IsNaN(elapsedSeconds) || double.IsInfinity(elapsedSeconds))
            elapsedSeconds = 0.0;
        if (tiles == null || restPoses == null)
            return;

        int count = Math.Min(tiles.Length, restPoses.Length);
        for (int i = 0; i < count; i++)
        {
            TileMotion tile = tiles[i];
            RestPose rest = restPoses[i];
            // A changed binding requires Configure so its baseline is explicit.
            if (tile == null || !rest.target || tile.target != rest.target)
                continue;

            double period = Finite(tile.periodSeconds) && tile.periodSeconds > 0f
                ? Math.Max(0.1, tile.periodSeconds) : 3.2;
            double phase = Finite(tile.phaseCycles) ? tile.phaseCycles : 0.0;
            double cycles = (elapsedSeconds % period) / period + phase % 1.0;
            float wave = (float)Math.Sin(cycles * TwoPi) * weight;
            float bob = Finite(tile.bobAmplitude) ? Mathf.Max(0f, tile.bobAmplitude) : 0.05f;
            float depth = Finite(tile.depthAmplitude) ? Mathf.Clamp(tile.depthAmplitude, 0f, 0.025f) : 0f;
            Vector3 angle = tile.rotationEuler;
            angle.x = Finite(angle.x) ? Mathf.Clamp(angle.x, -4f, 4f) : 0f;
            angle.y = Finite(angle.y) ? Mathf.Clamp(angle.y, -4f, 4f) : 0f;
            angle.z = Finite(angle.z) ? Mathf.Clamp(angle.z, -4f, 4f) : 0f;
            rest.target.localPosition = rest.position + new Vector3(0f, bob * wave, depth * wave);
            rest.target.localRotation = rest.rotation * Quaternion.Euler(angle * wave);
        }
    }

    private void CaptureIfNeeded()
    {
        if (captured)
            return;
        int count = tiles == null ? 0 : tiles.Length;
        restPoses = new RestPose[count];
        for (int i = 0; i < count; i++)
        {
            Transform target = tiles[i] == null ? null : tiles[i].target;
            if (!target)
                continue;
            restPoses[i] = new RestPose
            {
                target = target,
                position = target.localPosition,
                rotation = target.localRotation
            };
        }
        captured = true;
    }

    private void RestoreRestPoses()
    {
        if (!captured || restPoses == null)
            return;
        for (int i = 0; i < restPoses.Length; i++)
        {
            RestPose rest = restPoses[i];
            if (!rest.target)
                continue;
            rest.target.localPosition = rest.position;
            rest.target.localRotation = rest.rotation;
        }
    }

    private static bool Finite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}

