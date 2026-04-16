using System.Collections;
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
    private Coroutine slowCoroutine = null;
    private float stunTimer = 0f;
    private Vector3 moveDirection = Vector3.zero;
    private Vector3 knockbackVelocity = Vector3.zero;
    private CharacterController characterController;

    [Header("Combat Control")]
    public float knockbackDamping = 10f;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Speed debuff handled by coroutine (if any). speedModifier is kept up-to-date by ApplySpeedDebuff / SlowDebuffCoroutine.

        if (stunTimer > 0f)
            stunTimer -= Time.deltaTime;

        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);
        bool isStunned = stunTimer > 0f;

        bool isRunning = !isStunned && Input.GetKey(KeyCode.LeftShift);
        float effectiveMaxSpeed = (isRunning ? runSpeed : walkSpeed) * speedModifier;
        float curSpeedX = isStunned ? 0f : effectiveMaxSpeed * Input.GetAxis("Vertical");
        float curSpeedY = isStunned ? 0f : effectiveMaxSpeed * Input.GetAxis("Horizontal");
        float movementDirectionY = moveDirection.y;

        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        if (!isStunned && Input.GetButton("Jump") && characterController.isGrounded)
            moveDirection.y = jumpPower;
        else
            moveDirection.y = movementDirectionY;

        if (!characterController.isGrounded)
            moveDirection.y -= gravity * Time.deltaTime;

        // Crouch
        if (!isStunned && Input.GetKey(KeyCode.R))
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

        Vector3 totalMove = moveDirection + knockbackVelocity;
        characterController.Move(totalMove * Time.deltaTime);
        knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, knockbackDamping * Time.deltaTime);
    }

    public void ApplySpeedDebuff(float multiplier, float duration)
    {
        // Ensure multiplier is sane (don't allow complete stop unless desired; min 0.01)
        multiplier = Mathf.Clamp(multiplier, 0.01f, 1f);

        // Stop any existing slow coroutine so the latest debuff overrides previous ones
        if (slowCoroutine != null)
        {
            StopCoroutine(slowCoroutine);
            slowCoroutine = null;
        }

        // Apply new modifier and start/reset coroutine to clear it after duration
        speedModifier = multiplier;
        slowCoroutine = StartCoroutine(SlowDebuffCoroutine(duration));
        Debug.Log($"Speed debuff applied: x{speedModifier:F2} for {duration:F1}s");
    }

    private IEnumerator SlowDebuffCoroutine(float duration)
    {
        yield return new WaitForSeconds(Mathf.Max(0.01f, duration));
        speedModifier = 1f;
        slowCoroutine = null;
        Debug.Log("Speed debuff ended, speed reset to normal.");
    }

    public void ApplyStun(float duration)
    {
        stunTimer = Mathf.Max(stunTimer, Mathf.Max(duration, 0.01f));
        Debug.Log($"Stunned for {stunTimer:F2}s");
    }

    public void ApplyKnockback(Vector3 direction, float force)
    {
        Vector3 flatDirection = direction;
        flatDirection.y = 0f;
        if (flatDirection.sqrMagnitude < 0.01f)
            flatDirection = -transform.forward;

        knockbackVelocity = flatDirection.normalized * Mathf.Max(0f, force);
    }
}
