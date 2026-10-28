using UnityEngine;
using UnityEngine.EventSystems;

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

    [Header("Mobile Touch Orbit")]
    [SerializeField] private bool mobileTouchOrbitEnabled = false;
    [SerializeField, Min(0.01f)] private float mobileOrbitSensitivity = 0.12f;
    [SerializeField, Range(0.5f, 0.9f)] private float mobileLookRegionStart = 0.5f;
    [SerializeField, Range(-15f, 15f)] private float mobileMinOrbitY = -5f;
    [SerializeField, Range(20f, 60f)] private float mobileMaxOrbitY = 45f;

    private Vector3 currentLookTarget;
    private float orbitX = 0f;
    private float orbitY = 0f;
    private int mobileLookFingerId = int.MinValue;
    private readonly System.Collections.Generic.HashSet<int> mobileRejectedFingerIds = new();

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

        // Keep right-click orbit available in the Editor even when Android is the active
        // build target. On a real mobile player it remains disabled so Legacy Input cannot
        // synthesize mouse input from multitouch and fight the owned right-side gesture.
        bool allowMouseOrbit = !MobileStageControls.IsMobilePresentation || Application.isEditor;
        if (allowMouseOrbit && Input.GetMouseButton(1)) // right mouse button
        {
            orbitX += Input.GetAxis("Mouse X") * orbitSpeed;
            orbitY -= Input.GetAxis("Mouse Y") * orbitSpeed;
            orbitY = Mathf.Clamp(orbitY, -30f, 60f); // vertical limit
        }

        UpdateMobileTouchOrbit();

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

    public void SetMobileTouchOrbitEnabled(bool enabled)
    {
        mobileTouchOrbitEnabled = enabled;
        if (enabled)
            orbitY = Mathf.Clamp(orbitY, mobileMinOrbitY, mobileMaxOrbitY);

        if (!enabled)
        {
            mobileLookFingerId = int.MinValue;
            mobileRejectedFingerIds.Clear();
        }
    }

    private void UpdateMobileTouchOrbit()
    {
        if (!mobileTouchOrbitEnabled)
            return;

        // Classify every finger at the instant it begins. A finger that starts outside
        // the right-side look region (or on joystick/UI) stays rejected for its entire
        // lifetime, even if it later crosses into the camera half of the screen.
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                mobileRejectedFingerIds.Remove(touch.fingerId);
                continue;
            }

            if (touch.phase != TouchPhase.Began || touch.fingerId == mobileLookFingerId)
                continue;

            bool startsInRightLookRegion = touch.position.x >= Screen.width * mobileLookRegionStart;
            bool joystickOwnsFinger = MobileJoystickController.Current != null
                                      && MobileJoystickController.Current.OwnsPointer(touch.fingerId);
            bool startsOverUi = EventSystem.current != null
                                && EventSystem.current.IsPointerOverGameObject(touch.fingerId);

            if (!startsInRightLookRegion || joystickOwnsFinger || startsOverUi)
                mobileRejectedFingerIds.Add(touch.fingerId);
        }

        // Keep ownership of one right-side finger until it is released. This lets the
        // left joystick continue receiving a different finger at the same time.
        if (mobileLookFingerId != int.MinValue)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.fingerId != mobileLookFingerId)
                    continue;

                if (touch.phase == TouchPhase.Moved)
                {
                    orbitX += touch.deltaPosition.x * mobileOrbitSensitivity;
                    orbitY -= touch.deltaPosition.y * mobileOrbitSensitivity;
                    // Mobile keeps the camera comfortably above the player's ground plane.
                    // The desktop right-mouse orbit intentionally retains its wider range.
                    orbitY = Mathf.Clamp(orbitY, mobileMinOrbitY, mobileMaxOrbitY);
                }

                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    mobileLookFingerId = int.MinValue;

                return;
            }

            // Defensive recovery if Android drops/cancels the owned touch between frames.
            mobileLookFingerId = int.MinValue;
        }

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.phase != TouchPhase.Began)
                continue;

            if (mobileRejectedFingerIds.Contains(touch.fingerId))
                continue;

            // Movement owns the left side. Camera look can only begin on the right side.
            // Additional fingers never inherit camera ownership: exactly one right-side
            // finger may own look until it is released.
            if (touch.position.x < Screen.width * mobileLookRegionStart)
                continue;

            // Never steal the finger currently owned by the movement joystick, even if
            // that finger is dragged across the screen into the camera region.
            if (MobileJoystickController.Current != null
                && MobileJoystickController.Current.OwnsPointer(touch.fingerId))
                continue;

            // A touch that starts on Attack, a word tile, or any other UI never becomes
            // camera input later in the gesture, even if that finger drags into empty space.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                continue;

            mobileLookFingerId = touch.fingerId;
            return;
        }
    }
}
