using UnityEngine;

/// <summary>Moves a trail anchor around its capsule to make trails visible.</summary>
public sealed class ImportedVfxShowcaseTrailMotion : MonoBehaviour
{
    [SerializeField] private float radius = 1.8f;
    [SerializeField] private float speed = 1.4f;
    private Vector3 start;
    private float phase;

    private void OnEnable()
    {
        start = transform.localPosition;
        phase = 0f;
    }

    private void Update()
    {
        phase += Time.deltaTime * speed;
        transform.localPosition = start + new Vector3(
            Mathf.Cos(phase) * radius,
            Mathf.Sin(phase * 1.8f) * 0.65f,
            Mathf.Sin(phase) * radius);
    }
}
