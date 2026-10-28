using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class SunboundWardenSwordHitbox : MonoBehaviour
{
    [SerializeField] private SunboundWardenController controller;
    [SerializeField] private bool disableColliderOutsideDamageWindow = true;

    private Collider hitbox;
    private bool damageWindowOpen;

    public bool DamageWindowOpen => damageWindowOpen;

    private void Awake()
    {
        hitbox = GetComponent<Collider>();
        ResolveController();

        if (disableColliderOutsideDamageWindow && hitbox != null)
            hitbox.enabled = false;
    }

    private void OnDisable()
    {
        damageWindowOpen = false;
    }

    public void OpenDamageWindow()
    {
        ResolveController();
        damageWindowOpen = true;

        if (hitbox != null)
            hitbox.enabled = true;
    }

    public void CloseDamageWindow()
    {
        damageWindowOpen = false;

        if (disableColliderOutsideDamageWindow && hitbox != null)
            hitbox.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!damageWindowOpen || other == null)
            return;

        if (!other.CompareTag("Player") && !other.transform.root.CompareTag("Player"))
            return;

        ResolveController();
        if (controller == null)
        {
            Debug.LogWarning(
                "[SunboundWardenSwordHitbox] SunboundWardenController reference is missing.",
                this);
            return;
        }

        if (controller.TryResolveCrouchSlashHit(other))
            return;

        if (controller.TryResolveSunSlashHit(other))
            return;

        controller.TryResolveSwordHit(other);
    }

    private void ResolveController()
    {
        if (controller != null)
            return;

        controller = GetComponentInParent<SunboundWardenController>();
    }
}
