using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public sealed class StoneGolemAnimationRelay : MonoBehaviour
{
    [SerializeField] private StoneGolemController controller;

    private void Awake() => ResolveController();
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
    private void ResolveController()
    {
        if (controller == null)
            controller = GetComponentInParent<StoneGolemController>();
    }
}
