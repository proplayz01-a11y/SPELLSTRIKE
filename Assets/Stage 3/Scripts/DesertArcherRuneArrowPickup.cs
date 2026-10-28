using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class DesertArcherRuneArrowPickup : MonoBehaviour
{
    [Header("Pickup")]
    [SerializeField] private string itemName = "Desert Rune Arrow";
    [SerializeField, Min(1)] private int amount = 1;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private string playerTag = "Player";

    private bool playerInRange;
    private bool collected;
    private Collider pickupCollider;

    public bool Collected => collected;
    public int Amount => amount;

    public void Configure(string displayName, int itemAmount, float triggerRadius)
    {
        itemName = string.IsNullOrWhiteSpace(displayName)
            ? "Desert Rune Arrow"
            : displayName;
        amount = Mathf.Max(1, itemAmount);

        pickupCollider = GetComponent<Collider>();
        if (pickupCollider == null)
            pickupCollider = gameObject.AddComponent<SphereCollider>();

        pickupCollider.isTrigger = true;
        pickupCollider.enabled = true;

        if (pickupCollider is SphereCollider sphere)
            sphere.radius = Mathf.Max(0.15f, triggerRadius);

        Rigidbody body = GetComponent<Rigidbody>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
    }

    private void Awake()
    {
        pickupCollider = GetComponent<Collider>();
        if (pickupCollider != null)
            pickupCollider.isTrigger = true;
    }

    private void Update()
    {
        if (!collected && playerInRange && Input.GetKeyDown(interactKey))
            TryCollect();
    }

    public bool TryCollect()
    {
        if (collected || !playerInRange)
            return false;

        GameDatabaseManager database = GameDatabaseManager.EnsureInstance();
        if (database == null)
        {
            Debug.LogWarning($"[DesertArcherRuneArrowPickup] Could not collect {itemName}: database unavailable.", this);
            return false;
        }

        database.AddDesertArcherRuneArrow(amount);
        database.FlushSave();

        collected = true;
        MobileStageControls.ClearInteraction(this);
        Debug.Log(
            $"[DesertArcherRuneArrowPickup] Collected {itemName} x{amount}. " +
            $"Owned: {database.GetDesertArcherRuneArrowCount()}.",
            this);
        Destroy(gameObject);
        return true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected || !IsPlayer(other))
            return;

        playerInRange = true;
        MobileStageControls.OfferInteraction(this, "COLLECT RUNE ARROW", () => TryCollect());
        Debug.Log($"[DesertArcherRuneArrowPickup] {itemName} is in interaction range.", this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
            return;

        playerInRange = false;
        MobileStageControls.ClearInteraction(this);
    }

    private void OnDisable()
    {
        MobileStageControls.ClearInteraction(this);
    }

    private bool IsPlayer(Collider other)
    {
        if (other == null)
            return false;
        if (other.CompareTag(playerTag))
            return true;
        if (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(playerTag))
            return true;

        Transform root = other.transform.root;
        return root != null && root.CompareTag(playerTag);
    }
}
