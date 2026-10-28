using UnityEngine;

public class ArenaTile : MonoBehaviour
{
    public char letter;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            TileManager tm = Object.FindFirstObjectByType<TileManager>();

            if (tm == null || tm.AreNormalCombatWorldPickupsSuspended)
                return;

            if (tm.TryCollectNormalCombatWorldTile(letter))
                Destroy(gameObject);
        }
    }
}
