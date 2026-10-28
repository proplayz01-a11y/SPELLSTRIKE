using System;
using UnityEngine;

public sealed class TutorialGroundTilePickup : MonoBehaviour
{
    public static event Action<char> TileCollected;

    [SerializeField] private char letter = 'A';
    [SerializeField, Min(1)] private int maximumPoolTiles = 16;

    private bool collected;

    public void SetLetter(char newLetter)
    {
        letter = char.ToUpperInvariant(newLetter);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected || !other.CompareTag("Player"))
            return;

        TileManager tileManager = FindFirstObjectByType<TileManager>();
        if (tileManager == null || tileManager.tilePoolPanel == null)
        {
            Debug.LogError("[TutorialGroundTilePickup] TileManager or Tile Pool reference is missing.");
            return;
        }

        if (tileManager.tilePoolPanel.childCount >= maximumPoolTiles)
            return;

        collected = true;
        char normalizedLetter = char.ToUpperInvariant(letter);
        tileManager.CreateTileInPool(normalizedLetter);
        TileCollected?.Invoke(normalizedLetter);
        Destroy(gameObject);
    }
}
