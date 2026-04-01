using UnityEngine;

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

    private Vector2 movementInput;
    private bool isRunning;
    private bool wasGrounded;

    public bool keepCursorVisible = true;
    public bool disableMovement = false;
    private bool isAttacking = false;

    //private float lastLoggedSpeed = -1f;
    //private string lastAnimState = "";

    void Start()
    {
        controller = GetComponent<CharacterController>();

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

    void Update()
    {
        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        if (!disableMovement)
        {
            movementInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            isRunning = Input.GetKey(KeyCode.LeftShift);
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

        if (!disableMovement)
            controller.Move(move * speed * Time.deltaTime);

        // Jump
        if (!disableMovement && Input.GetButtonDown("Jump") && controller.isGrounded)
        {
            velocity.y = jumpPower;
            if (animator != null)
            {
                animator.SetTrigger("isJumping");
                Debug.Log("[Animation] JUMP triggered");
            }
        }

        velocity.y -= gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Rotate toward movement
        if (!disableMovement && move.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);
        }

        UpdateAnimator();

        wasGrounded = controller.isGrounded;
    }

    private void UpdateAnimator()
    {
        if (animator == null || isAttacking) return;

        float targetSpeed = 0f;
        //string animState = "Idle";

        //if (movementInput.magnitude > 0.1f)
        //{
        //    if (isRunning)
        //    {
        //        targetSpeed = 1f;
        //        animState = "Fast Run";
        //    }
        //    else
        //    {
        //        targetSpeed = 0.5f;
        //        //animState = "Walk";
        //    }
        //}

        // Smooth speed transition
        float currentSpeed = animator.GetFloat("Speed");
        float smoothSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 10f);
        animator.SetFloat("Speed", smoothSpeed);

        //// Log only when state changes
        //if (animState != lastAnimState)
        //{
        //    Debug.Log($"[Animation] State changed to: {animState} | Speed: {smoothSpeed:F2}");
        //    lastAnimState = animState;
        //}

        //// Log speed changes significantly
        //if (Mathf.Abs(smoothSpeed - lastLoggedSpeed) > 0.1f)
        //{
        //    Debug.Log($"[Animation] Speed parameter: {smoothSpeed:F2}");
        //    lastLoggedSpeed = smoothSpeed;
        //}
    }

    public void BeginAttack()
    {
        disableMovement = true;
        isAttacking = true;
        if (animator != null)
            animator.SetFloat("Speed", 0f);
        Debug.Log("[Animation] Attack STARTED — movement disabled");
    }

    public void EndAttack()
    {
        disableMovement = false;
        isAttacking = false;
        Debug.Log("[Animation] Attack ENDED — movement re-enabled");
    }
}