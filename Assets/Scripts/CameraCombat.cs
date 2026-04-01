using UnityEngine;

public class CombatCamera : MonoBehaviour
{
    [Header("Targets")]
    public Transform player;
    public Transform enemy;

    [Header("Camera Settings")]
    public Vector3 offset = new Vector3(0, 5, -7);
    public float followSmoothSpeed = 0.125f;
    public float lookSmoothSpeed = 5f;
    public float orbitSpeed = 5f; // mouse orbit sensitivity
    public float minY = 1f;

    [Header("Lock-On Toggle")]
    public bool lockOnEnemy = true;
    public KeyCode toggleKey = KeyCode.Tab;

    private Vector3 currentLookTarget;
    private float orbitX = 0f;
    private float orbitY = 0f;

    void Start()
    {
        if (player == null)
            Debug.LogError("Player not assigned in CombatCamera!");
        currentLookTarget = lockOnEnemy && enemy != null ? enemy.position : player.position;
    }

    void LateUpdate()
    {
        // Toggle lock-on
        if (Input.GetKeyDown(toggleKey))
        {
            lockOnEnemy = !lockOnEnemy;
        }

        // Right-click orbit
        if (Input.GetMouseButton(1)) // right mouse button
        {
            orbitX += Input.GetAxis("Mouse X") * orbitSpeed;
            orbitY -= Input.GetAxis("Mouse Y") * orbitSpeed;
            orbitY = Mathf.Clamp(orbitY, -30f, 60f); // vertical limit
        }

        // Apply orbit to offset
        Quaternion orbitRotation = Quaternion.Euler(orbitY, orbitX, 0);
        Vector3 rotatedOffset = orbitRotation * offset;

        // Smooth camera position
        Vector3 desiredPosition = player.position + rotatedOffset;
        if (desiredPosition.y < minY) desiredPosition.y = minY;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSmoothSpeed);

        // Determine look target
        Vector3 targetPosition = lockOnEnemy && enemy != null ? enemy.position : player.position;

        // Smoothly interpolate LookAt
        currentLookTarget = Vector3.Lerp(currentLookTarget, targetPosition, lookSmoothSpeed * Time.deltaTime);
        transform.LookAt(currentLookTarget);
    }
}
