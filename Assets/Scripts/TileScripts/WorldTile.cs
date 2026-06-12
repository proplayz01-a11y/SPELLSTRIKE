using UnityEngine;

public class WorldTile : MonoBehaviour
{
    [Header("Letter Settings")]
    public char letter;
    public Material[] letterMaterials; // 0 = A, 1 = B ... 25 = Z

    [Header("Pickup Settings")]
    public int maxPoolCount = 16;

    [Header("Great Attractor Passive")]
    public bool enableGreatAttractorPull = true;
    public float attractorRadius = 45f;
    public float attractorPullSpeed = 2.25f;
    public float attractorStopDistance = 0.35f;
    public float passiveCheckInterval = 0.35f;

    private Renderer tileRenderer;
    private FloatingTile floatingTile;
    private Transform player;
    private PassiveItemInventory passiveInventory;
    private float nextPassiveCheckTime;
    private bool greatAttractorEquipped;
    private static bool loggedGreatAttractorActive;

    private void Awake()
    {
        tileRenderer = GetComponent<Renderer>();
        floatingTile = GetComponent<FloatingTile>();
    }

    private void Update()
    {
        if (!enableGreatAttractorPull || IsInsideUiCanvas())
            return;

        RefreshPassiveStateIfNeeded();

        if (!greatAttractorEquipped)
            return;

        ResolvePlayer();

        if (player == null)
            return;

        PullTowardPlayer();
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

    private void RefreshPassiveStateIfNeeded()
    {
        if (Time.time < nextPassiveCheckTime)
            return;

        nextPassiveCheckTime = Time.time + passiveCheckInterval;

        if (passiveInventory == null)
            passiveInventory = PassiveItemInventory.GetOrCreate();

        greatAttractorEquipped = passiveInventory != null && passiveInventory.IsEquipped(PassiveItemId.GreatAttractor);
    }

    private void ResolvePlayer()
    {
        if (player != null && player.gameObject.activeInHierarchy)
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        player = playerObject != null ? playerObject.transform : null;
    }

    private void PullTowardPlayer()
    {
        Vector3 current = floatingTile != null ? floatingTile.TargetPosition : transform.position;
        Vector3 target = player.position;
        target.y = current.y;

        float distance = Vector3.Distance(current, target);
        if (distance > attractorRadius || distance <= attractorStopDistance)
            return;

        if (!loggedGreatAttractorActive)
        {
            loggedGreatAttractorActive = true;
            Debug.Log("[GreatAttractor] Pulling world letter tiles toward the player.");
        }

        float step = attractorPullSpeed * Time.deltaTime;
        if (floatingTile != null)
            floatingTile.MoveTargetTowards(target, step);
        else
            transform.position = Vector3.MoveTowards(transform.position, target, step);
    }

    private bool IsInsideUiCanvas()
    {
        return GetComponentInParent<Canvas>() != null;
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
