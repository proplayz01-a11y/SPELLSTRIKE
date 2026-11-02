using UnityEngine;

/// <summary>
/// Approved Rodin V11 visual references shared by collectible world letters and
/// lightweight UI sprites. The logical letter remains A-Z (Q displays Qu).
/// This is presentation-only; rarity never changes tile probability or damage.
/// </summary>
public sealed class RodinTileCatalog : ScriptableObject
{
    private const string ResourcePath = "GameplayTiles/RodinV11Catalog";
    private static RodinTileCatalog cached;

    [SerializeField] private GameObject bronzeBase;
    [SerializeField] private GameObject silverBase;
    [SerializeField] private GameObject goldBase;
    [SerializeField] private GameObject[] letterMeshes = new GameObject[26];
    [SerializeField] private Sprite[] tileSprites = new Sprite[26];

    public static RodinTileCatalog Load()
    {
        if (cached == null)
            cached = Resources.Load<RodinTileCatalog>(ResourcePath);
        return cached;
    }

    public bool IsComplete
    {
        get
        {
            if (bronzeBase == null || silverBase == null || goldBase == null ||
                letterMeshes == null || letterMeshes.Length != 26 ||
                tileSprites == null || tileSprites.Length != 26)
                return false;
            for (int i = 0; i < 26; i++)
                if (letterMeshes[i] == null || tileSprites[i] == null) return false;
            return true;
        }
    }

    public Sprite GetSprite(char letter)
    {
        int index = char.ToUpperInvariant(letter) - 'A';
        return tileSprites != null && index >= 0 && index < tileSprites.Length
            ? tileSprites[index] : null;
    }

    public GameObject GetLetterMesh(char letter)
    {
        int index = char.ToUpperInvariant(letter) - 'A';
        return letterMeshes != null && index >= 0 && index < letterMeshes.Length
            ? letterMeshes[index] : null;
    }

    public GameObject GetBase(char letter)
    {
        switch (char.ToUpperInvariant(letter))
        {
            case 'A': case 'D': case 'E': case 'H': case 'I':
            case 'N': case 'O': case 'R': case 'S': case 'T':
                return bronzeBase;
            case 'B': case 'C': case 'F': case 'G': case 'L':
            case 'M': case 'P': case 'U': case 'W': case 'Y':
                return silverBase;
            default:
                return goldBase; // J, K, Q(Qu), V, X, Z
        }
    }

#if UNITY_EDITOR
    public void SetEditorReferences(GameObject bronze, GameObject silver,
        GameObject gold, GameObject[] letters, Sprite[] sprites)
    {
        bronzeBase = bronze;
        silverBase = silver;
        goldBase = gold;
        letterMeshes = letters;
        tileSprites = sprites;
    }
#endif
}
