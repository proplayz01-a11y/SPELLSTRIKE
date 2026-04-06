using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public float walkSpeed = 6f;
    public float runSpeed = 12f;
    public float jumpPower = 7f;
    public float gravity = 10f;
    public float crouchHeight = 1f;
    public float defaultHeight = 2f;

    private float speedModifier = 1f;
    private float slowTimer = 0f;
    private Vector3 moveDirection = Vector3.zero;
    private CharacterController characterController;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (slowTimer > 0f)
        {
            slowTimer -= Time.deltaTime;
            if (slowTimer <= 0f)
                speedModifier = 1f;
        }

        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float effectiveMaxSpeed = (isRunning ? runSpeed : walkSpeed) * speedModifier;
        float curSpeedX = effectiveMaxSpeed * Input.GetAxis("Vertical");
        float curSpeedY = effectiveMaxSpeed * Input.GetAxis("Horizontal");
        float movementDirectionY = moveDirection.y;

        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        if (Input.GetButton("Jump") && characterController.isGrounded)
            moveDirection.y = jumpPower;
        else
            moveDirection.y = movementDirectionY;

        if (!characterController.isGrounded)
            moveDirection.y -= gravity * Time.deltaTime;

        // Crouch
        if (Input.GetKey(KeyCode.R))
        {
            characterController.height = crouchHeight;
            walkSpeed = crouchHeight * 3; // optional
            runSpeed = crouchHeight * 3;
        }
        else
        {
            characterController.height = defaultHeight;
            walkSpeed = 6f;
            runSpeed = 12f;
        }

        characterController.Move(moveDirection * Time.deltaTime);
    }

    public void ApplySpeedDebuff(float multiplier, float duration)
    {
        speedModifier = Mathf.Clamp(multiplier, 0.1f, 1f);
        slowTimer = Mathf.Max(duration, 0.01f);
        Debug.Log($"Speed debuff applied: x{speedModifier:F2} for {slowTimer:F1}s");
    }
}
