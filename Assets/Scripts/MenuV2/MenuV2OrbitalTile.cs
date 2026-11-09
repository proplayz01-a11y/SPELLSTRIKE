using UnityEngine;

/// <summary>
/// Motion-only presentation of a reused combat letter-tile prefab.
/// Uses no physics or word-game logic; swaps between two private render layers
/// so the 3D tile appears alternately behind and in front of illustrated Van.
/// </summary>
public sealed class MenuV2OrbitalTile : MonoBehaviour
{
    [SerializeField] private float initialPhaseRadians;
    [SerializeField] private float orbitSpeedRadians = .35f;
    [SerializeField] private float horizontalRadius = 3.1f;
    [SerializeField] private float verticalRadius = 1.65f;
    [SerializeField] private float depthRadius = 1.05f;
    [SerializeField] private int backRenderLayer = 28;
    [SerializeField] private int frontRenderLayer = 29;
    [SerializeField] private float bobAmplitude = .14f;

    private float angle;
    private int currentLayer = -1;

    public void Configure(float phase, float speed, int back, int front)
    {
        initialPhaseRadians = phase;
        orbitSpeedRadians = speed;
        backRenderLayer = back;
        frontRenderLayer = front;
        angle = phase;
    }

    private void OnEnable() => angle = initialPhaseRadians;

    private void Update()
    {
        angle += orbitSpeedRadians * Time.unscaledDeltaTime;
        float wave = Mathf.Sin(angle);
        float bob = Mathf.Sin(Time.unscaledTime * 1.2f + initialPhaseRadians * 1.7f) * bobAmplitude;
        transform.localPosition = new Vector3(
            Mathf.Cos(angle) * horizontalRadius,
            wave * verticalRadius + bob,
            wave * depthRadius);

        // Imported letter tiles are thin along their local X axis.
        // Present their face toward the menu camera (which looks along +Z).
        // Visible depth: an independent, slow 3-axis turn makes the real
        // gameplay tile thickness readable. Previously +/-11 degrees around
        // its face-on pose looked like a spinning flat UI square.
        float turn = Time.unscaledTime * (34f + 7f * Mathf.Sin(initialPhaseRadians));
        transform.localRotation = Quaternion.Euler(
            11f + 17f * Mathf.Sin(angle * .9f),
            90f + turn,
            12f * Mathf.Sin(Time.unscaledTime * .7f + initialPhaseRadians));

        int layer = wave < 0f ? frontRenderLayer : backRenderLayer;
        if (layer != currentLayer)
        {
            SetLayerRecursively(transform, layer);
            currentLayer = layer;
        }
    }

    private static void SetLayerRecursively(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        foreach (Transform child in root)
            SetLayerRecursively(child, layer);
    }
}
