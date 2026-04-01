using UnityEngine;

public class ArenaTile : MonoBehaviour
{
    public char letter;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            TileManager tm = Object.FindFirstObjectByType<TileManager>();

            if (tm != null)
            {
                tm.CreateTileInPool(letter);
            }

            Destroy(gameObject);
        }
    }
}