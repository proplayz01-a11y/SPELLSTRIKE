using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public sealed class StoneGolemAnimationRelay : MonoBehaviour
{
    [SerializeField] private StoneGolemController controller;

    private void Awake() => ResolveController();

    public void OnGroundSlamTakeoff()
    {
        ResolveController();
        controller?.HandleGroundSlamTakeoff();
    }

    public void OnGroundSlamImpact()
    {
        ResolveController();
        controller?.HandleGroundSlamImpact();
    }
    public void OnGroundSlamFinished()
    {
        ResolveController();
        controller?.HandleGroundSlamFinished();
    }
    public void OnStopSettled()
    {
        ResolveController();
        controller?.HandleStopSettled();
    }
    public void OnLowerBodyHitFinished()
    {
        ResolveController();
        controller?.HandleLowerBodyHitFinished();
    }
    public void OnUpperBodyHitFinished()
    {
        ResolveController();
        controller?.HandleUpperBodyHitFinished();
    }
    public void OnRightPunchImpact()
    {
        ResolveController();
        controller?.HandleRightPunchImpact();
    }
    public void OnRightPunchFinished()
    {
        ResolveController();
        controller?.HandleRightPunchFinished();
    }
    public void OnLeftPunchImpact()
    {
        ResolveController();
        controller?.HandleLeftPunchImpact();
    }
    public void OnLeftPunchFinished()
    {
        ResolveController();
        controller?.HandleLeftPunchFinished();
    }
    public void OnDeathFinished()
    {
        ResolveController();
        controller?.HandleDeathFinished();
    }
    private void ResolveController()
    {
        if (controller == null)
            controller = GetComponentInParent<StoneGolemController>();
    }
}
