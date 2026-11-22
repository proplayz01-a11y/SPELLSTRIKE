using UnityEngine;

/// <summary>
/// Lightweight, rig-independent movement for an ethereal NPC.
/// Kept separate from dialogue so final character art/Animator clips can replace
/// the placeholder without changing the encounter or its story state.
/// </summary>
[DisallowMultipleComponent]
public sealed class SpiritRemnantAmbientMotion : MonoBehaviour
{
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Transform head;
    [SerializeField] private Light auraLight;
    [SerializeField] private Animator gestureAnimator;
    [SerializeField] private float hoverHeight = 0.16f;
    [SerializeField] private float hoverFrequency = 1.15f;

    private Vector3 originalVisualPosition;
    private Quaternion originalHeadRotation;
    private float originalLightIntensity;
    private bool speaking;
    private static readonly int SpeakingParameter = Animator.StringToHash("IsSpeaking");
    public Transform HeadBone => head;
    public Transform VisualRoot => visualRoot;

    public void Configure(Transform visual, Transform headTransform, Light lightSource, Animator talkingAnimator = null)
    {
        visualRoot = visual;
        head = headTransform;
        auraLight = lightSource;
        gestureAnimator = talkingAnimator;
        CaptureRest();
    }

    public void SetSpeaking(bool value)
    {
        speaking = value;
        if (gestureAnimator != null && gestureAnimator.runtimeAnimatorController != null)
            gestureAnimator.SetBool(SpeakingParameter, value);
    }

    private void Awake() => CaptureRest();

    private void CaptureRest()
    {
        if (visualRoot != null) originalVisualPosition = visualRoot.localPosition;
        if (head != null) originalHeadRotation = head.localRotation;
        if (auraLight != null) originalLightIntensity = auraLight.intensity;
    }

    private void LateUpdate()
    {
        float t = Time.unscaledTime;
        if (visualRoot != null)
        {
            float floatWave = Mathf.Sin(t * hoverFrequency * Mathf.PI * 2f);
            visualRoot.localPosition = originalVisualPosition + Vector3.up * (floatWave * hoverHeight);
        }

        // When a rigged talking clip is present, let the Animator drive the
        // head/arms. Overriding the Head transform would erase its gestures.
        // Keep the procedural nod as a fallback for unrigged spirit art.
        if (head != null && gestureAnimator == null)
        {
            float slowNod = Mathf.Sin(t * (speaking ? 3.2f : 0.9f));
            float lookSway = Mathf.Sin(t * 0.7f + 1f);
            head.localRotation = originalHeadRotation *
                Quaternion.Euler(slowNod * (speaking ? 5f : 1.5f), lookSway * 4f, 0f);
        }

        if (auraLight != null)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * (speaking ? 7f : 2.2f));
            auraLight.intensity = originalLightIntensity * Mathf.Lerp(0.75f, speaking ? 1.32f : 1.04f, pulse);
        }
    }

    private void OnDisable()
    {
        if (gestureAnimator != null && gestureAnimator.runtimeAnimatorController != null)
            gestureAnimator.SetBool(SpeakingParameter, false);
        if (visualRoot != null) visualRoot.localPosition = originalVisualPosition;
        if (head != null) head.localRotation = originalHeadRotation;
        if (auraLight != null) auraLight.intensity = originalLightIntensity;
    }
}
