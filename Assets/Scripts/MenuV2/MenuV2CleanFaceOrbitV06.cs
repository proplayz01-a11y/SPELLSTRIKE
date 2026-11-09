using UnityEngine;

/// <summary>
/// V06-only cleaner tile animation: reuse gameplay cube face/letter, but keep
/// its letter plane presented to camera. The old full yaw spin showed the
/// letter texture cramped onto the cube's thin edge at side angles.
/// </summary>
public sealed class MenuV2CleanFaceOrbitV06 : MonoBehaviour
{
    [SerializeField] private float phase;
    [SerializeField] private float radiansPerSecond = .31f;
    [SerializeField] private int frontLayer = 29;
    [SerializeField] private int backLayer = 28;
    private float angle;
    private int currentLayer = -1;

    public void Configure(float startingPhase, float speed, int back, int front)
    {
        phase = startingPhase;
        radiansPerSecond = speed;
        backLayer = back;
        frontLayer = front;
        angle = phase;
    }

    private void OnEnable() => angle = phase;

    private void Update()
    {
        angle += radiansPerSecond * Time.unscaledDeltaTime;
        float wave = Mathf.Sin(angle);
        float bob = Mathf.Sin(Time.unscaledTime * 1.2f + phase * 1.7f) * .11f;
        transform.localPosition = new Vector3(
            Mathf.Cos(angle) * 3.1f,
            wave * 1.65f + bob,
            wave * 1.05f);

        // 90 degrees presents the large letter face of this cube to the
        // RenderTexture camera. Staying within 10 degrees avoids side-face
        // text artifacts. The Z tilt and bob still convey gentle 3D motion.
        transform.localRotation = Quaternion.Euler(
            5f + Mathf.Sin(angle * 1.15f) * 5f,
            90f + Mathf.Sin(Time.unscaledTime * .45f + phase) * 9f,
            Mathf.Sin(Time.unscaledTime * .8f + phase) * 9f);

        int layer = wave < 0 ? frontLayer : backLayer;
        if (layer != currentLayer)
        {
            SetLayer(transform, layer);
            currentLayer = layer;
        }
    }

    private static void SetLayer(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        foreach (Transform child in root)
            SetLayer(child, layer);
    }
}
