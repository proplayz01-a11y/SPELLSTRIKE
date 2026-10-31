using UnityEngine;

/// <summary>
/// Menu V11 only. Animate one approved Rodin tile around illustrated Van,
/// splitting the old shared orbit camera layers in front of/behind him.
/// Both sides use actual black 3D letters; no physics or gameplay code.
/// </summary>
[DisallowMultipleComponent]
public sealed class MenuV11RodinOrbitTile : MonoBehaviour
{
    [SerializeField] private float phaseRadians;
    [SerializeField] private float speedRadiansPerSecond = 0.31f;
    [SerializeField] private float horizontalRadius = 3.10f;
    [SerializeField] private float verticalRadius = 1.65f;
    [SerializeField] private float depthRadius = 1.05f;
    [SerializeField] private float selfSpinDegreesPerSecond = 23.0f;
    [SerializeField] private float bobAmplitude = 0.11f;
    [SerializeField] private int behindVanLayer = 28;
    [SerializeField] private int inFrontOfVanLayer = 29;

    private float currentAngle;
    private int currentRenderLayer = -1;

    public void Configure(float phase, float speed, int back, int front)
    {
        phaseRadians = phase;
        speedRadiansPerSecond = speed;
        behindVanLayer = back;
        inFrontOfVanLayer = front;
        currentAngle = phase;
        currentRenderLayer = -1;
        ApplyPose(0f);
    }

    private void OnEnable()
    {
        currentAngle = phaseRadians;
        currentRenderLayer = -1;
        ApplyPose(0f);
    }

    private void Update()
    {
        float now = Time.unscaledTime;
        currentAngle += speedRadiansPerSecond * Time.unscaledDeltaTime;
        ApplyPose(now);
    }

    private void ApplyPose(float timeSeconds)
    {
        float wave = Mathf.Sin(currentAngle);
        float bob = Mathf.Sin(timeSeconds * 1.2f + phaseRadians * 1.7f)
                    * bobAmplitude;

        transform.localPosition = new Vector3(
            Mathf.Cos(currentAngle) * horizontalRadius,
            wave * verticalRadius + bob,
            wave * depthRadius);

        // With five tiles on screen, synchronized constant 360-degree spins
        // make ALL five tiles disappear edge-on at the same time. Stage the
        // flip for one letter at a time so the word remains readable, but
        // each true 3D tile still shows its reverse face and thickness.
        const float cycleSeconds = 20f;
        const float flipSeconds = 1.4f;
        const float backHoldEnd = 10f;
        float stagger = phaseRadians * (cycleSeconds / (2f * Mathf.PI));
        float withinCycle = Mathf.Repeat(timeSeconds + stagger, cycleSeconds);
        float yaw;
        if (withinCycle < flipSeconds)
            yaw = Mathf.SmoothStep(0f, 180f, withinCycle / flipSeconds);
        else if (withinCycle < backHoldEnd)
            yaw = 180f;
        else if (withinCycle < backHoldEnd + flipSeconds)
            yaw = Mathf.SmoothStep(180f, 360f,
                        (withinCycle - backHoldEnd) / flipSeconds);
        else
            yaw = 360f;
        yaw += Mathf.Sin(timeSeconds * .55f + phaseRadians) * 7f;

        transform.localRotation = Quaternion.Euler(
            5f + 10f * Mathf.Sin(currentAngle),
            yaw,
            Mathf.Sin(timeSeconds * 0.7f + phaseRadians) * 6f);

        int newLayer = wave < 0f ? inFrontOfVanLayer : behindVanLayer;
        if (newLayer != currentRenderLayer)
        {
            ApplyLayer(transform, newLayer);
            currentRenderLayer = newLayer;
        }
    }

    private static void ApplyLayer(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        foreach (Transform child in root)
            ApplyLayer(child, layer);
    }
}
