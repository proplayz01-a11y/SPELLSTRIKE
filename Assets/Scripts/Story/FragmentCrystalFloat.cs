using UnityEngine;

/// <summary>
/// Visual-only motion for collectible crystals. Never moves the parent
/// FragmentPickup/BossFragmentPickup or its interaction collider.
/// </summary>
[DisallowMultipleComponent]
public sealed class FragmentCrystalFloat : MonoBehaviour
{
    [SerializeField] private Transform visualPivot;
    [SerializeField, Min(0f)] private float hoverDistance = 0.17f;
    [SerializeField, Min(0.05f)] private float hoverCyclesPerSecond = 0.29f;
    [SerializeField] private float yawDegreesPerSecond = 19f;
    [SerializeField] private float tiltDegrees = 4f;

    private Vector3 restingPosition;
    private Quaternion restingRotation;
    private float startTime;
    private float phaseOffset;

    private void Awake()
    {
        if (visualPivot == null)
            visualPivot = transform.Find("FragmentCrystal_Visual");
        if (visualPivot != null)
        {
            restingPosition = visualPivot.localPosition;
            restingRotation = visualPivot.localRotation;
        }
        phaseOffset = Mathf.Abs(GetInstanceID() % 13) * 0.43f;
    }

    private void OnEnable()
    {
        startTime = Time.time;
    }

    private void LateUpdate()
    {
        if (visualPivot == null)
            return;

        float elapsed = Time.time - startTime;
        float wave = elapsed * Mathf.PI * 2f * hoverCyclesPerSecond + phaseOffset;
        visualPivot.localPosition = restingPosition +
            Vector3.up * (Mathf.Sin(wave) * hoverDistance);
        visualPivot.localRotation = restingRotation *
            Quaternion.Euler(tiltDegrees * Mathf.Sin(wave * 0.7f),
                elapsed * yawDegreesPerSecond, tiltDegrees * Mathf.Cos(wave * 0.6f));
    }

    private void OnDisable()
    {
        if (visualPivot == null)
            return;
        visualPivot.localPosition = restingPosition;
        visualPivot.localRotation = restingRotation;
    }
}
