using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public sealed class SunboundWardenAnimationEventRelay : MonoBehaviour
{
    [SerializeField] private SunboundWardenController controller;

    private void Awake()
    {
        ResolveController();
    }

    // Put this method on an Animation Event at the first frame where the
    // Radiant Rush sword should be lethal.
    public void OpenRadiantRushSwordHitbox()
    {
        ResolveController();
        if (controller != null)
            controller.HandleRadiantRushSwordHitboxOpen();
    }

    // Put this method on an Animation Event immediately after the sword's
    // active swing has passed Van.
    public void CloseRadiantRushSwordHitbox()
    {
        ResolveController();
        if (controller != null)
            controller.HandleRadiantRushSwordHitboxClose();
    }

    public void ApplySunbindEffect()
    {
        ResolveController();
        if (controller != null)
            controller.HandleSunbindEffect();
    }

    public void OpenSunSlashHitbox()
    {
        ResolveController();
        if (controller != null)
            controller.HandleSunSlashHitboxOpen();
    }

    public void CloseSunSlashHitbox()
    {
        ResolveController();
        if (controller != null)
            controller.HandleSunSlashHitboxClose();
    }

    public void OpenCrouchSlashHitbox()
    {
        ResolveController();
        if (controller != null)
            controller.HandleCrouchSlashHitboxOpen();
    }

    public void CloseCrouchSlashHitbox()
    {
        ResolveController();
        if (controller != null)
            controller.HandleCrouchSlashHitboxClose();
    }

    public void OnDeathFinished()
    {
        ResolveController();
        if (controller != null)
            controller.OnDeathFinished();
    }

    private void ResolveController()
    {
        if (controller != null)
            return;

        controller =
            GetComponent<SunboundWardenController>() ??
            GetComponentInParent<SunboundWardenController>() ??
            GetComponentInChildren<SunboundWardenController>(true);
    }
}
