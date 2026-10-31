using UnityEngine;

/// <summary>
/// Rotates the ACTUAL 3D Tutorial Spellbook mesh in its existing RenderTexture
/// world. Does not spin a 2D image, touch the tutorial prefab, or change routes.
/// </summary>
public sealed class MenuV2SpellbookSpinV10 : MonoBehaviour
{
    [SerializeField] private float fullRevolutionSeconds = 28f;
    [SerializeField] private float bobAmount = .115f;
    [SerializeField] private float bobFrequency = 1.05f;
    [SerializeField] private float pitchDegrees = 3.5f;

    private Quaternion initialRotation;
    private Vector3 initialPosition;

    private void Awake()
    {
        initialRotation = transform.localRotation;
        initialPosition = transform.localPosition;
    }

    private void LateUpdate()
    {
        float t = Time.unscaledTime;
        float yaw = (t * 360f / Mathf.Max(8f, fullRevolutionSeconds)) % 360f;
        float tilt = Mathf.Sin(t * .70f) * pitchDegrees;
        transform.localRotation = initialRotation *
            Quaternion.Euler(tilt, yaw, Mathf.Sin(t * .46f) * 2.0f);
        transform.localPosition = initialPosition +
            Vector3.up * (Mathf.Sin(t * bobFrequency) * bobAmount);
    }
}
