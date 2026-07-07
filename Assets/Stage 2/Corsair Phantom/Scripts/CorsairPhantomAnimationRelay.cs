using UnityEngine;

public class CorsairPhantomAnimationRelay : MonoBehaviour
{
    [SerializeField] private CorsairPhantomController controller;

    private void Awake()
    {
        ResolveController();
    }

    private void Reset()
    {
        ResolveController();
    }

    private void ResolveController()
    {
        if (controller == null)
            controller = GetComponentInParent<CorsairPhantomController>();
    }

    public void OnPhantomShotFire()
    {
        ResolveController();
        controller?.OnPhantomShotFire();
    }

    public void OnPhantomShotFinished()
    {
        ResolveController();
        controller?.OnPhantomShotFinished();
    }

    public void OnBeingHitFinished()
    {
        ResolveController();
        controller?.OnBeingHitFinished();
    }

    public void OnPiercingScreamHit()
    {
        ResolveController();
        controller?.OnPiercingScreamHit();
    }

    public void OnPiercingScreamFinished()
    {
        ResolveController();
        controller?.OnPiercingScreamFinished();
    }

    public void OnDeathFinished()
    {
        ResolveController();
        controller?.OnDeathFinished();
    }

    public void OnSpectralSlashHit()
    {
        OnPhantomShotFire();
    }

    public void OnSpectralSlashFinished()
    {
        OnPhantomShotFinished();
    }
}
