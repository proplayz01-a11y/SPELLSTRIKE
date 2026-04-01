using System.Collections.Generic;
using UnityEngine;

public class EnemyWeapon : MonoBehaviour
{
    public float damage = 20f;           // damage per hit
    public Collider swordCollider;        // drag the sword collider here

    // track targets hit during current enabled window to prevent multiple hits per swing
    private HashSet<GameObject> hitTargets = new HashSet<GameObject>();

    private void Start()
    {
        if (swordCollider != null)
            swordCollider.enabled = false; // start disabled
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Hit something: " + other.name);
        if (other.CompareTag("Player") == false) return;
        var damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null)
        {
            Debug.Log("No IDamageable found.");
            return;
        }

        Debug.Log("Applying damage!");
        damageable.ApplyDamage(damage);
    }

    // functions to call from animation events
    public void EnableWeapon()
    {

        Debug.Log("Weapon Enabled");
        hitTargets.Clear();
        if (swordCollider != null)
            swordCollider.enabled = true;
    }

    public void DisableWeapon()
    {
        Debug.Log("Weapon Disabled");
        if (swordCollider != null)
            swordCollider.enabled = false;
        hitTargets.Clear();
    }
}