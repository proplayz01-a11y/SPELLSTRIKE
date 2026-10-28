using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public sealed class DesertArcherAnimationRelay : MonoBehaviour
{
    [SerializeField] private DesertArcherController controller;

    private void Awake()
    {
        ResolveController();
    }

    public void OnDrawStarted()
    {
        ResolveController();
        controller?.HandleDrawStarted();
    }

    public void OnArrowNocked()
    {
        ResolveController();
        controller?.HandleArrowNocked();
    }

    public void OnArrowReleased()
    {
        ResolveController();
        controller?.HandleArrowReleased();
    }

    public void OnDrawEnded()
    {
        ResolveController();
        controller?.HandleDrawEnded();
    }

    public void OnRollStarted()
    {
        ResolveController();
        controller?.HandleRollStarted();
    }

    public void OnRollEnded()
    {
        ResolveController();
        controller?.HandleRollEnded();
    }

    public void OnDeathFinished()
    {
        ResolveController();
        controller?.HandleDeathFinished();
    }

    private void ResolveController()
    {
        if (controller != null)
            return;

        controller =
            GetComponent<DesertArcherController>() ??
            GetComponentInParent<DesertArcherController>() ??
            GetComponentInChildren<DesertArcherController>(true);
    }
}
