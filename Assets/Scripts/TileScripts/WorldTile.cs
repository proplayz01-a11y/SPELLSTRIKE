using UnityEngine;

public class WorldTile : MonoBehaviour
{
    [Header("Letter Settings")]
    public char letter;
    public Material[] letterMaterials; // 0 = A, 1 = B ... 25 = Z

    [Header("Pickup Settings")]
    public int maxPoolCount = 16;

    private Renderer tileRenderer;

    private void Awake()
    {
        tileRenderer = GetComponent<Renderer>();
    }

    // call this at spawn if you want a random letter
    public void AssignRandomLetter()
    {
        int index = Random.Range(0, 26);
        letter = (char)('A' + index);
        ApplyMaterial();
    }

    // manually assign a letter if needed
    public void SetLetter(char newLetter)
    {
        letter = newLetter;
        ApplyMaterial();
    }

    private void ApplyMaterial()
    {
        if (letterMaterials == null || letterMaterials.Length != 26)
        {
            Debug.LogWarning("Assign 26 letter materials in inspector!");
            return;
        }

        int index = letter - 'A';
        tileRenderer.material = letterMaterials[index];
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        TileManager tileManager = FindFirstObjectByType<TileManager>();
        if (tileManager == null)
            return;

        int currentPool = tileManager.tilePoolPanel.childCount;
        if (currentPool >= maxPoolCount)
            return; // pool full

        tileManager.CreateTileInPool(letter);
        Destroy(gameObject);
    }
}