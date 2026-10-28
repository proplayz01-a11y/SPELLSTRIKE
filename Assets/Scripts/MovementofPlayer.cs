using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class MovementofPlayer : MonoBehaviour
{
    public float walkSpeed = 6f;
    public float runSpeed = 12f;
    public float jumpPower = 7f;
    public float gravity = 20f;
    private Animator animator;
    private Vector3 velocity;
    private CharacterController controller;
    private Stage2WaterInteraction waterInteraction;
    private VanGuardedHighStepTraversal guardedHighStep;

    private Vector2 movementInput;
    private bool isRunning;
    private bool wasGrounded;

    public Vector2 CurrentMovementInput => movementInput;
    public bool IsRunning => isRunning;
    public bool IsMovementDisabled => disableMovement || externalMovementLockCount > 0;
    public bool IsMoving => !IsMovementDisabled && movementInput.sqrMagnitude > 0.01f;

    public bool keepCursorVisible = true;
    public bool disableMovement = false;
    // Joystick input is normalized, so its magnitude can never exceed 1.0.
    // Keep sprinting available, but require the stick to be pushed very near its rim.
    [SerializeField, Range(0.5f, 1f)] private float mobileSprintThreshold = 0.92f;
    private bool isAttacking = false;
    private struct TimedSpeedDebuff
    {
        public float multiplier;
        public float endsAt;
    }
    // Each enemy debuff expires independently. Rebuild speeds from the
    // unslowed baseline so reapplying a slow cannot permanently compound it.
    private readonly List<TimedSpeedDebuff> speedDebuffs = new List<TimedSpeedDebuff>();
    private float appliedSpeedMultiplier = 1f;
    private int externalMovementLockCount;
    private float baselineWalkSpeed;
    private float baselineRunSpeed;
    private bool baselinesCaptured;

    //private float lastLoggedSpeed = -1f;
    //private string lastAnimState = "";

    void Start()
    {
        CaptureBaselinesIfNeeded();
        controller = GetComponent<CharacterController>();
        waterInteraction = GetComponent<Stage2WaterInteraction>();
        guardedHighStep = GetComponent<VanGuardedHighStepTraversal>();

        if (!keepCursorVisible)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        animator = GetComponent<Animator>();
        wasGrounded = controller.isGrounded;
    }

    private void CaptureBaselinesIfNeeded()
    {
        if (baselinesCaptured)
            return;

        baselineWalkSpeed = walkSpeed;
        baselineRunSpeed = runSpeed;
        baselinesCaptured = true;
    }

    public void ResetTransientStateForSceneTransition()
    {
        CaptureBaselinesIfNeeded();

        speedDebuffs.Clear();
        appliedSpeedMultiplier = 1f;

        walkSpeed = baselineWalkSpeed;
        runSpeed = baselineRunSpeed;
        movementInput = Vector2.zero;
        velocity = Vector3.zero;
        isRunning = false;
        isAttacking = false;
        externalMovementLockCount = 0;
        disableMovement = false;

        controller ??= GetComponent<CharacterController>();
        animator ??= GetComponent<Animator>();
        waterInteraction = GetComponent<Stage2WaterInteraction>();

        if (animator != null)
            animator.SetFloat("Speed", 0f);
        guardedHighStep ??= GetComponent<VanGuardedHighStepTraversal>();
        guardedHighStep?.CancelStep();
    }

    void Update()
    {
        UpdateSpeedDebuffs();
        if (controller == null || !controller.enabled || !controller.gameObject.activeInHierarchy)
            return;

        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        if (!IsMovementDisabled)
        {
            // Preserve the proven desktop path, accept physical gamepads, and let the
            // runtime mobile joystick feed the same CharacterController movement logic.
            Vector2 legacyInput = new(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            Vector2 inputSystemMove = Gamepad.current != null
                ? Gamepad.current.leftStick.ReadValue()
                : Vector2.zero;
            Vector2 mobileJoystickInput = MobileJoystickController.Current != null
                ? MobileJoystickController.Current.InputDirection
                : Vector2.zero;

            movementInput = legacyInput;
            if (inputSystemMove.sqrMagnitude > movementInput.sqrMagnitude)
                movementInput = inputSystemMove;
            if (mobileJoystickInput.sqrMagnitude > movementInput.sqrMagnitude)
                movementInput = mobileJoystickInput;

            bool mobileJoystickIsDriving = mobileJoystickInput.sqrMagnitude > 0.0001f
                                           && mobileJoystickInput.sqrMagnitude >= legacyInput.sqrMagnitude
                                           && mobileJoystickInput.sqrMagnitude >= inputSystemMove.sqrMagnitude;
            // Keep sprint on the joystick. Clamp stale serialized values defensively:
            // normalized joystick magnitude can never exceed 1.0.
            float effectiveMobileSprintThreshold = Mathf.Clamp(mobileSprintThreshold, 0.5f, 1f);
            isRunning = mobileJoystickIsDriving
                ? mobileJoystickInput.magnitude >= effectiveMobileSprintThreshold
                : Input.GetKey(KeyCode.LeftShift);
        }
        else
        {
            movementInput = Vector2.zero;
            isRunning = false;
        }

        Transform cam = Camera.main.transform;
        Vector3 camForward = cam.forward;
        Vector3 camRight = cam.right;

        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 move = camForward * movementInput.y + camRight * movementInput.x;
        float speed = isRunning ? runSpeed : walkSpeed;
        if (waterInteraction != null)
            speed *= waterInteraction.GetMovementSpeedMultiplier();

        // Track actual physical travel rather than blindly running the walk
        // animation whenever input is held against an obstacle. This also
        // follows Unity's native low-step climbing as the capsule rises.
        float horizontalMotionFraction = 0f;
        bool highStepHandledMovement = false;
        if (!IsMovementDisabled)
        {
            Vector3 intendedMove = move * (speed * Time.deltaTime);
            if (intendedMove.sqrMagnitude > 0.00000001f)
            {
                if (guardedHighStep != null &&
                    guardedHighStep.TryMove(intendedMove, Time.deltaTime,
                        out horizontalMotionFraction))
                {
                    highStepHandledMovement = true;
                }
                else
                {
                    Vector3 beforeMove = transform.position;
                    controller.Move(intendedMove);
                    Vector3 actualMove = transform.position - beforeMove;
                    actualMove.y = 0f;
                    horizontalMotionFraction = Mathf.Clamp01(
                        actualMove.magnitude / intendedMove.magnitude);
                }
            }
            else
                guardedHighStep?.CancelStep();
        }
        else
            guardedHighStep?.CancelStep();

        // Jump
        if (!highStepHandledMovement && !IsMovementDisabled &&
            Input.GetButtonDown("Jump") && controller.isGrounded)
        {
            velocity.y = jumpPower;
            if (animator != null)
            {
                animator.SetTrigger("isJumping");
                Debug.Log("[Animation] JUMP triggered");
            }
        }

        if (highStepHandledMovement)
            velocity.y = -2f;
        else
        {
            velocity.y -= gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
        }

        // Rotate toward movement
        if (!IsMovementDisabled && move.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);
        }

        UpdateAnimator(horizontalMotionFraction);

        wasGrounded = controller.isGrounded;
    }

    private void UpdateAnimator(float horizontalMotionFraction)
    {
        if (animator == null || isAttacking) return;

        float targetSpeed = 0f;

        if (movementInput.magnitude > 0.1f && !IsMovementDisabled)
        {
            if (isRunning)
                targetSpeed = 1f;   // matches Fast Run threshold
            else
                targetSpeed = 0.5f; // matches Walk threshold
            // A wall, closed door or oversized obstacle should not make Van
            // sprint in place. Low steps still animate because the
            // CharacterController actually moves across them.
            targetSpeed *= horizontalMotionFraction;
        }

        float currentSpeed = animator.GetFloat("Speed");
        float smoothSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 10f);
        animator.SetFloat("Speed", smoothSpeed);
    }

    public void BeginAttack()
    {
        disableMovement = true;
        isAttacking = true;
        if (animator != null)
            animator.SetFloat("Speed", 0f);
        Debug.Log("[Animation] Attack STARTED - movement disabled");
    }

    public void EndAttack()
    {
        disableMovement = false;
        isAttacking = false;
        Debug.Log("[Animation] Attack ENDED - movement re-enabled");
    }

    public void AcquireExternalMovementLock()
    {
        externalMovementLockCount++;
        movementInput = Vector2.zero;
        isRunning = false;
        if (animator != null)
            animator.SetFloat("Speed", 0f);
    }

    public void ReleaseExternalMovementLock()
    {
        externalMovementLockCount = Mathf.Max(0, externalMovementLockCount - 1);
    }

    /// <summary>
    /// An enemy impact can launch Van upward without moving the
    /// CharacterController from a second script. The existing gravity and
    /// ceiling/ground collisions remain authoritative even while stunned.
    /// </summary>
    public void ApplyExternalVerticalLaunch(float upwardSpeed)
    {
        controller ??= GetComponent<CharacterController>();
        if (controller == null || !controller.enabled || upwardSpeed <= 0f)
            return;

        velocity.y = Mathf.Max(velocity.y, upwardSpeed);
    }

    public void ApplySpeedDebuff(float multiplier, float duration)
    {
        CaptureBaselinesIfNeeded();
        if (duration <= 0f || multiplier >= 1f)
            return;
        speedDebuffs.Add(new TimedSpeedDebuff
        {
            multiplier = Mathf.Clamp(multiplier, 0.01f, 1f),
            endsAt = Time.time + duration
        });
        UpdateSpeedDebuffs();
    }

    private void UpdateSpeedDebuffs()
    {
        if (speedDebuffs.Count == 0 && Mathf.Approximately(appliedSpeedMultiplier, 1f))
            return;
        float strongest = 1f;
        for (int i = speedDebuffs.Count - 1; i >= 0; i--)
        {
            if (Time.time >= speedDebuffs[i].endsAt)
                speedDebuffs.RemoveAt(i);
            else
                strongest = Mathf.Min(strongest, speedDebuffs[i].multiplier);
        }
        if (Mathf.Approximately(strongest, appliedSpeedMultiplier))
            return;
        walkSpeed = baselineWalkSpeed * strongest;
        runSpeed = baselineRunSpeed * strongest;
        appliedSpeedMultiplier = strongest;
    }
}
