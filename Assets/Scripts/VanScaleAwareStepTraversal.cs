using UnityEngine;

/// <summary>
/// Makes Van physically step over low solid obstacles using Unity's native
/// CharacterController stepping. Humanoid foot IK only follows the result;
/// it cannot lift the collision capsule on its own.
///
/// The player is scaled differently between stages (up to 10x in Stage 3),
/// but CharacterController.stepOffset remains in world units. The prefab's
/// old 0.3m offset therefore left giant Van blocked by ankle-high geometry.
/// This component recalibrates the native step offset whenever the player
/// changes scale, without teleporting, climbing walls, or bypassing colliders.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public sealed class VanScaleAwareStepTraversal : MonoBehaviour
{
    [Header("Native CharacterController obstacle stepping")]
    [SerializeField] private bool enableScaleAwareStepping = true;
    [Tooltip("How much of the controller's world height counts as a low step.")]
    [SerializeField, Range(0.025f, 0.15f)]
    private float stepHeightFraction = 0.07f;
    [Tooltip("Absolute upper bound in world metres. Taller walls must block Van.")]
    [SerializeField, Range(0.35f, 2f)]
    private float maximumWorldStepHeight = 1.25f;

    private CharacterController controller;
    private float authoredStepOffset;
    private float lastCalibratedHeight = -1f;
    private float lastAppliedStepOffset = -1f;

    public float CurrentWorldStepHeight => controller ? controller.stepOffset : 0f;
    public float AuthoredStepHeight => authoredStepOffset;
    public float CurrentWorldCapsuleHeight =>
        controller ? controller.height * Mathf.Abs(transform.lossyScale.y) : 0f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        authoredStepOffset = controller ? controller.stepOffset : 0f;
        Recalibrate();
    }

    private void OnEnable()
    {
        if (controller)
            Recalibrate();
    }

    private void Update()
    {
        if (!controller || !controller.enabled)
            return;

        // PersistentPlayerRoot may resize Van for a new stage without
        // re-instantiating him. Recalculate only when size/offset changes.
        float worldHeight = CurrentWorldCapsuleHeight;
        if (Mathf.Abs(worldHeight - lastCalibratedHeight) > 0.005f ||
            Mathf.Abs(controller.stepOffset - lastAppliedStepOffset) > 0.005f)
            Recalibrate();
    }

    private void Recalibrate()
    {
        if (!controller)
            return;

        float height = CurrentWorldCapsuleHeight;
        if (height <= 0.05f || float.IsNaN(height) || float.IsInfinity(height))
            return;

        // Don't override a deliberate external authored step setting.
        // Detect it independently of ordinary stage-scale transitions.
        if (lastAppliedStepOffset >= 0f &&
            Mathf.Abs(controller.stepOffset - lastAppliedStepOffset) > 0.005f)
            authoredStepOffset = controller.stepOffset;

        float maxSafeStep = Mathf.Min(maximumWorldStepHeight, height * 0.45f);
        float baseline = Mathf.Clamp(authoredStepOffset, 0f, maxSafeStep);
        float desired = enableScaleAwareStepping
            ? Mathf.Max(baseline, Mathf.Min(height * stepHeightFraction, maxSafeStep))
            : baseline;

        if (Mathf.Abs(controller.stepOffset - desired) > 0.005f)
            controller.stepOffset = desired;

        lastAppliedStepOffset = desired;
        lastCalibratedHeight = height;
    }
}
