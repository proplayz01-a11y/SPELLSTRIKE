using UnityEngine;
using UnityEngine.Rendering;

namespace Spellstrike.Vfx
{
    /// <summary>
    /// Standalone strong-tier visual lifetime. This is an EXPLICIT FALLBACK
    /// lightning renderer while the authored VFX Graph from the tutorial is
    /// blocked by the unavailable Unity MCP editor connection. The game logic
    /// does not depend on this renderer or its lifetime.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ArcaneSurgeVisual : MonoBehaviour
    {
        private const int BoltPoints = 12;
        private const int RingPoints = 33;
        private const int Forks = 3;
        private const float AftershockSeconds = 0.55f;
        private const float BoltAfterImpactSeconds = 0.11f;
        private static readonly Color Cyan = new Color(0.14f, 0.93f, 1.8f, 1f);
        private static readonly Color Violet = new Color(0.56f, 0.20f, 1.3f, 1f);

        private LineRenderer halo;
        private LineRenderer core;
        private LineRenderer ring;
        private readonly LineRenderer[] branches = new LineRenderer[Forks];
        private readonly LineRenderer[] sparks = new LineRenderer[8];
        private Material material;
        private Vector3 head;
        private float shockwaveGroundY;
        private float height;
        private float progress;
        private float impactTime;
        private bool impacted;
        private bool aborted;
        private float seed;

        public void Initialize()
        {
            material = Resources.Load<Material>("Spellstrike/VFX/ArcaneSurgeEnergy");
            if (material == null)
            {
                // Safety fallback for an Editor working tree before the material imports.
                Shader shader = Shader.Find("SPELLSTRIKE/VFX/BasicChargeEnergyMobileSafe");
                if (shader == null)
                {
                    Debug.LogError("[ArcaneSurge] Missing energy material and URP fallback shader.");
                    Destroy(gameObject);
                    return;
                }
                material = new Material(shader);
                ownsMaterial = true;
            }

            halo = MakeLine("01 Cyan-violet halo", 0.52f, Violet, false);
            core = MakeLine("02 White-cyan core", 0.18f, Cyan, false);
            for (int i = 0; i < Forks; i++)
                branches[i] = MakeLine("03 Lightning branch " + i, 0.095f, i == 1 ? Violet : Cyan, false);
            ring = MakeLine("04 Ground shockwave", 0.11f, Cyan, true);
            ring.positionCount = RingPoints;
            ring.enabled = false;
            for (int i = 0; i < sparks.Length; i++)
            {
                sparks[i] = MakeLine("05 Impact spark " + i, 0.045f, i % 2 == 0 ? Cyan : Violet, false);
                sparks[i].positionCount = 2;
                sparks[i].enabled = false;
            }
            seed = Time.time * 14.7f;
        }

        private bool ownsMaterial;

        private LineRenderer MakeLine(string name, float width, Color tint, bool loop)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(transform, false);
            LineRenderer line = child.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.sharedMaterial = material;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.loop = loop;
            line.numCornerVertices = 0;
            line.numCapVertices = 0;
            line.widthMultiplier = width;
            line.startColor = tint;
            line.endColor = tint;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            line.positionCount = BoltPoints;
            return line;
        }

        public void SetDescent(Vector3 liveHead, float skyHeight, float descent01)
        {
            if (impacted || aborted) return;
            head = liveHead;
            height = skyHeight;
            progress = Mathf.Clamp01(descent01);
        }

        public void ConfirmedImpact(Vector3 contact, float groundY)
        {
            if (impacted || aborted) return;
            head = contact;
            shockwaveGroundY = groundY;
            progress = 1f;
            impacted = true;
            impactTime = Time.time;
            ring.enabled = true;
            foreach (LineRenderer spark in sparks) spark.enabled = true;
            ArcaneSurgeThunder.PlayAt(head);
            ArcaneSurgeCameraImpulse.Pulse(0.12f, 0.075f);
        }

        public void Abort()
        {
            aborted = true;
            Destroy(gameObject);
        }

        private void LateUpdate()
        {
            if (aborted || core == null || halo == null) return;
            float age = impacted ? Time.time - impactTime : 0f;
            float boltAlpha = impacted ? Mathf.Clamp01(1f - age / BoltAfterImpactSeconds) : 1f;
            core.enabled = halo.enabled = boltAlpha > 0.001f;
            for (int i = 0; i < branches.Length; i++)
                branches[i].enabled = boltAlpha > 0.001f;

            if (boltAlpha > 0.001f)
            {
                Vector3 top = head + Vector3.up * height;
                // At time t the descending leading tip moves from sky to the
                // current head position, so strafing enemies remain followed.
                float smooth = progress * progress * (3f - 2f * progress);
                Vector3 tip = Vector3.LerpUnclamped(top, head, smooth);
                float flicker = 0.88f + 0.12f * Mathf.Sin(Time.time * 71f + seed);
                SetAlpha(halo, Violet, boltAlpha * 0.60f * flicker);
                SetAlpha(core, Cyan, boltAlpha * flicker);

                Vector3[] path = new Vector3[BoltPoints];
                for (int i = 0; i < BoltPoints; i++)
                {
                    float v = i / (float)(BoltPoints - 1);
                    float jitter = Mathf.Sin(v * 23.4f + seed + Time.time * 33f) * 0.24f
                        + Mathf.Sin(v * 41.1f + seed * 2f - Time.time * 19f) * 0.11f;
                    // Both endpoints stay glued to the target column; only
                    // intermediate points zigzag.
                    float envelope = Mathf.Sin(v * Mathf.PI);
                    path[i] = Vector3.LerpUnclamped(top, tip, v)
                        + new Vector3(jitter * envelope, 0f, Mathf.Cos(v * 19f + seed) * 0.16f * envelope);
                }
                halo.SetPositions(path);
                core.SetPositions(path);
                for (int b = 0; b < branches.Length; b++)
                {
                    LineRenderer branch = branches[b];
                    branch.positionCount = 4;
                    int index = 3 + b * 2;
                    Vector3 origin = path[index];
                    float length = Mathf.Max(0.30f, (top - tip).magnitude * (0.17f + b * 0.035f));
                    float side = b % 2 == 0 ? 1f : -1f;
                    Vector3 fork = origin + new Vector3(side * length * 0.5f, -length * 0.23f, length * 0.20f);
                    branch.SetPosition(0, origin);
                    branch.SetPosition(1, origin + (fork - origin) * 0.38f + Vector3.up * 0.10f);
                    branch.SetPosition(2, fork);
                    branch.SetPosition(3, fork + Vector3.down * length * 0.23f);
                    SetAlpha(branch, b == 1 ? Violet : Cyan, boltAlpha * 0.65f);
                }
            }

            if (!impacted) return;
            float t = Mathf.Clamp01(age / AftershockSeconds);
            if (ring != null)
            {
                float radius = Mathf.Lerp(0.15f, 2.0f, 1f - (1f - t) * (1f - t));
                ring.widthMultiplier = Mathf.Lerp(0.15f, 0.018f, t);
                SetAlpha(ring, Cyan, Mathf.Pow(1f - t, 2f) * 0.75f);
                for (int i = 0; i < RingPoints; i++)
                {
                    float angle = i * Mathf.PI * 2f / (RingPoints - 1);
                    ring.SetPosition(i, new Vector3(
                        head.x + Mathf.Cos(angle) * radius,
                        shockwaveGroundY + 0.08f,
                        head.z + Mathf.Sin(angle) * radius));
                }
            }
            for (int i = 0; i < sparks.Length; i++)
            {
                float angle = i * (Mathf.PI * 2f / sparks.Length) + 0.23f;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0.55f, Mathf.Sin(angle)).normalized;
                Vector3 from = head + direction * (0.22f + t * 1.5f);
                sparks[i].SetPosition(0, from);
                sparks[i].SetPosition(1, from + direction * (0.36f * (1f - t)));
                SetAlpha(sparks[i], i % 2 == 0 ? Cyan : Violet, (1f - t) * 0.85f);
            }
            if (age >= AftershockSeconds)
                Destroy(gameObject);
        }

        private static void SetAlpha(LineRenderer line, Color tint, float alpha)
        {
            tint.a = Mathf.Clamp01(alpha);
            line.startColor = tint;
            line.endColor = tint;
        }

        private void OnDestroy()
        {
            if (ownsMaterial && material != null)
                Destroy(material);
        }
    }

    /// <summary>Single allocation after first confirmed hit; original thunder crack/rumble, no external audio asset.</summary>
    internal static class ArcaneSurgeThunder
    {
        private static AudioClip clip;

        public static void PlayAt(Vector3 worldPoint)
        {
            if (clip == null)
            {
                const int rate = 22050;
                float[] samples = new float[rate / 2];
                uint random = 17389;
                for (int i = 0; i < samples.Length; i++)
                {
                    float t = i / (float)rate;
                    random = random * 1664525u + 1013904223u;
                    float noise = ((random >> 8) / 16777216f) * 2f - 1f;
                    float crack = Mathf.Exp(-t * 42f) * noise;
                    float rumble = Mathf.Exp(-t * 7f)
                        * (Mathf.Sin(t * 2f * Mathf.PI * 54f)
                           + 0.36f * Mathf.Sin(t * 2f * Mathf.PI * 83f));
                    samples[i] = Mathf.Clamp((crack * 0.55f + rumble * 0.30f), -1f, 1f);
                }
                clip = AudioClip.Create("ArcaneSurge_Thunder_OneShot", samples.Length, 1, rate, false);
                clip.SetData(samples, 0);
            }
            AudioSource.PlayClipAtPoint(clip, worldPoint, 0.65f);
        }
    }

    /// <summary>
    /// Cinemachine-safe impact camera impulse. A late execution order applies
    /// shake AFTER CinemachineBrain positions the Main Camera and BEFORE URP
    /// captures its view matrix. The original pose is restored at render end,
    /// so Cinemachine never inherits an accumulated offset.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    internal sealed class ArcaneSurgeCameraImpulse : MonoBehaviour
    {
        private Camera hostCamera;
        private float pulseStartTime;
        private float endTime;
        private float duration = 0.18f;
        private float amplitude;
        private Vector3 originalWorldPosition;
        private Quaternion originalWorldRotation;
        private bool offsetApplied;
        private static bool missingCameraWarningLogged;

        public static void Pulse(float seconds, float strength)
        {
            Camera camera = Camera.main;
            if (camera == null || !camera.isActiveAndEnabled)
            {
                // Keep the shake functioning in a gameplay scene where the
                // physical output camera has not yet been tagged MainCamera.
                foreach (Camera candidate in Camera.allCameras)
                {
                    if (candidate == null || !candidate.isActiveAndEnabled)
                        continue;
                    camera = candidate;
                    break;
                }
            }
            if (camera == null || !camera.isActiveAndEnabled)
            {
                if (!missingCameraWarningLogged)
                {
                    Debug.LogWarning("[ArcaneSurge] Camera shake skipped: no active output Camera.");
                    missingCameraWarningLogged = true;
                }
                return;
            }
            ArcaneSurgeCameraImpulse shake = camera.GetComponent<ArcaneSurgeCameraImpulse>();
            if (shake == null) shake = camera.gameObject.AddComponent<ArcaneSurgeCameraImpulse>();
            // Also bind explicitly: the rendering camera may have been
            // reactivated/rebound by PersistentThirdPersonCamera this frame.
            shake.hostCamera = camera;
            // A new hit restarts the impulse rather than inheriting the last
            // hit's end time or amplitude (particularly across later casts).
            shake.RestoreCamera();
            shake.duration = Mathf.Clamp(seconds, 0.08f, 0.35f);
            shake.pulseStartTime = Time.unscaledTime;
            shake.endTime = shake.pulseStartTime + shake.duration;
            shake.amplitude = Mathf.Clamp(strength, 0f, 1.50f);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log(
                $"[ArcaneSurge] Impact shake triggered on {camera.name}: "
                + $"{shake.amplitude:0.00} units / {shake.duration:0.00}s",
                camera);
#endif
        }

        private void OnEnable()
        {
            hostCamera = GetComponent<Camera>();
            RenderPipelineManager.endCameraRendering += EndRendering;
        }

        private void LateUpdate()
        {
            // Repeated camera renders and disabled cameras must never carry
            // shake pose into Cinemachine's next update.
            RestoreCamera();
            if (hostCamera == null || !hostCamera.isActiveAndEnabled
                || Time.unscaledTime >= endTime || amplitude <= 0f)
                return;

            originalWorldPosition = transform.position;
            originalWorldRotation = transform.rotation;
            float elapsed = Time.unscaledTime - pulseStartTime;
            float remaining = Mathf.Clamp01((endTime - Time.unscaledTime) / duration);
            // A strong initial punch with a smooth exit that remains readable
            // on distant third-person Cinemachine cameras.
            float falloff = remaining * Mathf.Sqrt(remaining);
            float horizontal = Mathf.Sin(elapsed * 99f + 0.45f);
            float vertical = Mathf.Cos(elapsed * 85f);
            Vector3 offset = (transform.right * horizontal
                + transform.up * vertical * 0.75f) * (amplitude * falloff);
            transform.position = originalWorldPosition + offset;
            // Small angular punch guarantees a visible screen-space shake
            // even with a distant camera and a wide FOV.
            float angle = amplitude * 1.75f * falloff;
            transform.rotation = originalWorldRotation * Quaternion.Euler(
                vertical * angle * 0.65f,
                horizontal * angle * 0.55f,
                horizontal * angle);
            offsetApplied = true;
        }

        private void EndRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera == hostCamera && offsetApplied)
                RestoreCamera();
        }

        private void RestoreCamera()
        {
            if (!offsetApplied) return;
            transform.position = originalWorldPosition;
            transform.rotation = originalWorldRotation;
            offsetApplied = false;
        }

        private void OnDisable()
        {
            RenderPipelineManager.endCameraRendering -= EndRendering;
            RestoreCamera();
        }
    }
}
