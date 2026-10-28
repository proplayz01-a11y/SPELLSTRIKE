using UnityEngine;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(CharacterController))]
public sealed class CharacterControllerStepClimber : MonoBehaviour
{
    [Header("Step Detection")]
    [SerializeField, Min(0.05f)] private float maximumStepHeight = 0.45f;
    [SerializeField, Min(0.01f)] private float lowerRayHeight = 0.08f;
    [SerializeField, Min(0.05f)] private float detectionDistance = 0.4f;
    [SerializeField, Min(0.1f)] private float climbSpeed = 4f;
    [SerializeField, Range(0f, 1f)] private float sideRayOffset = 0.65f;
    [SerializeField] private LayerMask climbableLayers = ~0;

    [Header("Rules")]
    [SerializeField] private bool requireGrounded = true;
    [SerializeField] private bool drawDebugRays;

    private CharacterController characterController;
    private MovementofPlayer playerMovement;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerMovement = GetComponent<MovementofPlayer>();
    }

    private void Update()
    {
        if (characterController == null || !characterController.enabled)
            return;

        if (playerMovement != null && playerMovement.IsMovementDisabled)
            return;

        if (requireGrounded && !characterController.isGrounded)
            return;

        Vector3 moveDirection = ReadCameraRelativeMoveDirection();
        if (moveDirection.sqrMagnitude < 0.01f)
            return;

        if (CanClimbStep(moveDirection))
            characterController.Move(Vector3.up * (climbSpeed * Time.deltaTime));
    }

    private Vector3 ReadCameraRelativeMoveDirection()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        if (Mathf.Abs(horizontal) < 0.01f && Mathf.Abs(vertical) < 0.01f)
            return Vector3.zero;

        Transform cameraTransform = Camera.main != null ? Camera.main.transform : transform;
        Vector3 cameraForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 cameraRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;

        return (cameraForward * vertical + cameraRight * horizontal).normalized;
    }

    private bool CanClimbStep(Vector3 direction)
    {
        Bounds bounds = characterController.bounds;
        float footY = bounds.min.y;
        Vector3 center = new Vector3(bounds.center.x, footY, bounds.center.z);
        Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
        float horizontalOffset = characterController.radius * sideRayOffset;

        return CheckRayPair(center, direction, Vector3.zero)
            || CheckRayPair(center, direction, right * horizontalOffset)
            || CheckRayPair(center, direction, -right * horizontalOffset);
    }

    private bool CheckRayPair(Vector3 footCenter, Vector3 direction, Vector3 lateralOffset)
    {
        Vector3 lowerOrigin = footCenter + lateralOffset + Vector3.up * lowerRayHeight;
        Vector3 upperOrigin = footCenter + lateralOffset + Vector3.up * maximumStepHeight;

        bool lowerBlocked = Physics.Raycast(
            lowerOrigin,
            direction,
            out _,
            detectionDistance,
            climbableLayers,
            QueryTriggerInteraction.Ignore);

        bool upperBlocked = Physics.Raycast(
            upperOrigin,
            direction,
            out _,
            detectionDistance,
            climbableLayers,
            QueryTriggerInteraction.Ignore);

        if (drawDebugRays)
        {
            Debug.DrawRay(lowerOrigin, direction * detectionDistance, lowerBlocked ? Color.red : Color.green);
            Debug.DrawRay(upperOrigin, direction * detectionDistance, upperBlocked ? Color.red : Color.cyan);
        }

        // Open-tread stairs have gaps behind their risers. A downward landing
        // probe can fall through those gaps and reject a valid step, so the
        // reliable rule here is simply: lower ray blocked, upper ray clear.
        return lowerBlocked && !upperBlocked;
    }
}
