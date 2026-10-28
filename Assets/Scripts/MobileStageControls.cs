using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MobileStageControls : MonoBehaviour
{
    public static MobileStageControls Current { get; private set; }

    [Header("Controls")]
    [SerializeField] private RectTransform movementJoystick;
    [SerializeField] private GameObject interactionButtonRoot;
    [SerializeField] private Button interactionButton;
    [SerializeField] private TMP_Text interactionLabel;
    [SerializeField] private CombatCamera combatCamera;

    private UnityEngine.Object interactionOwner;
    private Action interactionAction;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;

    public static bool IsMobilePresentation
    {
        get
        {
            if (Application.isMobilePlatform)
                return true;
#if UNITY_EDITOR
            return UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.Android;
#else
            return false;
#endif
        }
    }

    public void Configure(
        RectTransform joystick,
        GameObject buttonRoot,
        Button button,
        TMP_Text label,
        CombatCamera camera)
    {
        movementJoystick = joystick;
        interactionButtonRoot = buttonRoot;
        interactionButton = button;
        interactionLabel = label;
        combatCamera = camera;
        ApplyPresentation(force: true);
    }

    public void RefreshForCurrentScene()
    {
        combatCamera = FindFirstObjectByType<CombatCamera>();
        ApplyPresentation(force: true);
    }

    private void OnEnable()
    {
        Current = this;
        if (interactionButton != null)
        {
            interactionButton.onClick.RemoveListener(HandleInteractionPressed);
            interactionButton.onClick.AddListener(HandleInteractionPressed);
        }
        ApplyPresentation(force: true);
    }

    private void OnDisable()
    {
        if (interactionButton != null)
            interactionButton.onClick.RemoveListener(HandleInteractionPressed);

        ClearInteractionInternal();
        if (Current == this)
            Current = null;
    }

    private void Update()
    {
        ApplyPresentation(force: false);
    }

    public static void OfferInteraction(UnityEngine.Object owner, string label, Action action)
    {
        if (Current == null || owner == null || action == null)
            return;

        Current.interactionOwner = owner;
        Current.interactionAction = action;
        if (Current.interactionLabel != null)
            Current.interactionLabel.text = string.IsNullOrWhiteSpace(label) ? "INTERACT" : label;
        if (Current.interactionButtonRoot != null)
            Current.interactionButtonRoot.SetActive(IsMobilePresentation);
    }

    public static void ClearInteraction(UnityEngine.Object owner)
    {
        if (Current == null || owner == null || Current.interactionOwner != owner)
            return;

        Current.ClearInteractionInternal();
    }

    private void HandleInteractionPressed()
    {
        if (!IsMobilePresentation || interactionOwner == null || interactionAction == null)
            return;

        Action action = interactionAction;
        action.Invoke();
    }

    private void ClearInteractionInternal()
    {
        interactionOwner = null;
        interactionAction = null;
        if (interactionButtonRoot != null)
            interactionButtonRoot.SetActive(false);
    }

    private void ApplyPresentation(bool force)
    {
        if (Screen.width <= 0 || Screen.height <= 0)
            return;

        Rect safeArea = Screen.safeArea;
        Vector2Int screenSize = new(Screen.width, Screen.height);
        if (!force && safeArea == lastSafeArea && screenSize == lastScreenSize)
            return;

        bool mobile = IsMobilePresentation;
        if (movementJoystick != null)
            movementJoystick.gameObject.SetActive(mobile);

        if (!mobile)
            ClearInteractionInternal();

        if (combatCamera == null)
            combatCamera = FindFirstObjectByType<CombatCamera>();
        if (combatCamera != null)
            combatCamera.SetMobileTouchOrbitEnabled(mobile);

        if (mobile)
        {
            Vector2 safeMin = new(safeArea.xMin / Screen.width, safeArea.yMin / Screen.height);
            Vector2 safeMax = new(safeArea.xMax / Screen.width, safeArea.yMax / Screen.height);

            if (movementJoystick != null)
            {
                movementJoystick.anchorMin = safeMin;
                movementJoystick.anchorMax = safeMin;
                movementJoystick.pivot = Vector2.zero;
                movementJoystick.anchoredPosition = new Vector2(32f, 28f);
                movementJoystick.sizeDelta = new Vector2(180f, 180f);
                movementJoystick.localScale = Vector3.one;

                RectTransform knob = movementJoystick.Find("Knob") as RectTransform;
                if (knob != null)
                {
                    // Exact TutorialScene joystick geometry.
                    knob.anchorMin = new Vector2(0.5f, 0.5f);
                    knob.anchorMax = new Vector2(0.5f, 0.5f);
                    knob.pivot = new Vector2(0.5f, 0.5f);
                    knob.anchoredPosition = Vector2.zero;
                    knob.sizeDelta = new Vector2(112f, 112f);
                    knob.localScale = new Vector3(0.72f, 0.72f, 0.72f);
                }

                MobileJoystickController joystickController = movementJoystick.GetComponent<MobileJoystickController>();
                if (joystickController != null && knob != null)
                    joystickController.Configure(knob, 52f);
            }

            if (interactionButtonRoot != null && interactionButtonRoot.transform is RectTransform buttonRect)
            {
                buttonRect.anchorMin = new Vector2(safeMax.x, safeMin.y);
                buttonRect.anchorMax = new Vector2(safeMax.x, safeMin.y);
                buttonRect.pivot = new Vector2(1f, 0f);
                buttonRect.anchoredPosition = new Vector2(-32f, 28f);
                buttonRect.sizeDelta = new Vector2(190f, 64f);
                buttonRect.localScale = Vector3.one;
            }
        }

        lastSafeArea = safeArea;
        lastScreenSize = screenSize;
    }
}
