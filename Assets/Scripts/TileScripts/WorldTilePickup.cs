using UnityEngine;

public class WorldTilePickup : MonoBehaviour
{
    public char letter;

    public void SetLetter(char newLetter)
    {
        letter = newLetter;
        // update text mesh or UI display here
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        TileManager tileManager = FindFirstObjectByType<TileManager>();
        if (tileManager == null)
            return;

        int currentPool = tileManager.tilePoolPanel.childCount;
        int maxPool = 16;

        if (currentPool < maxPool)
        {
            // only pick up if there’s room
            tileManager.CreateTileInPool(letter);
            Destroy(gameObject);
        }
    }
}