using UnityEngine;

public class GhostFloatBob : MonoBehaviour
{
    [Header("Float")]
    [SerializeField] private float baseHeightOffset = 0.45f;
    [SerializeField] private float bobAmplitude = 0.08f;
    [SerializeField] private float bobSpeed = 2f;

    [Header("Sway")]
    [SerializeField] private float swayAngle = 2f;
    [SerializeField] private float swaySpeed = 1.2f;

    [Header("Startup")]
    [SerializeField] private bool randomizePhase = true;

    private Vector3 startLocalPosition;
    private Quaternion startLocalRotation;
    private float phaseOffset;

    private void Awake()
    {
        startLocalPosition = transform.localPosition;
        startLocalRotation = transform.localRotation;

        if (randomizePhase)
            phaseOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void LateUpdate()
    {
        float bob = Mathf.Sin((Time.time * bobSpeed) + phaseOffset) * bobAmplitude;
        float sway = Mathf.Sin((Time.time * swaySpeed) + phaseOffset) * swayAngle;

        transform.localPosition = new Vector3(
            startLocalPosition.x,
            startLocalPosition.y + baseHeightOffset + bob,
            startLocalPosition.z
        );

        transform.localRotation = startLocalRotation * Quaternion.Euler(0f, 0f, sway);
    }
}
