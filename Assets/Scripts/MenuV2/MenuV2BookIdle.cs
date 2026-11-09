using UnityEngine;

/// <summary>Lightweight 3D menu-book idle pose; does not alter the Tutorial Spellbook asset.</summary>
public sealed class MenuV2BookIdle : MonoBehaviour
{
    [SerializeField, Min(0f)] private float bobAmplitude = .08f;
    [SerializeField, Min(0f)] private float bobSpeed = 1.2f;
    [SerializeField] private float yawAmplitude = 4f;

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private void Awake()
    {
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;
    }

    private void LateUpdate()
    {
        float t = Time.unscaledTime;
        transform.localPosition = initialPosition
            + Vector3.up * (Mathf.Sin(t * bobSpeed) * bobAmplitude);
        transform.localRotation = initialRotation
            * Quaternion.Euler(0f, Mathf.Sin(t * .55f) * yawAmplitude, 0f);
    }
}
