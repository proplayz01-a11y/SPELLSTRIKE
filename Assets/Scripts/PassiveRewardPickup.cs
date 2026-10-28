using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PassiveRewardPickup : MonoBehaviour
{
    [Header("Reward")]
    [SerializeField] private PassiveItemId itemId = PassiveItemId.TidecarvedShell;
    [SerializeField] private VocabularyWorldTarget revealSource;

    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Collider pickupCollider;
    [SerializeField] private GameObject visualRoot;

    [Header("Presentation")]
    [SerializeField] private float rotationSpeed = 45f;
    [SerializeField] private float bobHeight = 0.2f;
    [SerializeField] private float bobSpeed = 2f;

    private bool revealed;
    private bool collected;
    private bool playerInRange;
    private Vector3 visualStartLocalPosition;

    private void Awake()
    {
        if (pickupCollider == null)
            pickupCollider = GetComponent<Collider>();
        if (pickupCollider != null)
        {
            pickupCollider.isTrigger = true;
            pickupCollider.enabled = false;
        }

        if (visualRoot != null)
        {
            visualStartLocalPosition = visualRoot.transform.localPosition;
            visualRoot.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (revealSource != null)
            revealSource.TargetCompleted += HandleTargetCompleted;
    }

    private void Start()
    {
        PassiveItemInventory inventory = PassiveItemInventory.GetOrCreate();
        if (inventory != null
            && (inventory.HasOwnedPassive(itemId) || inventory.HasPendingPassive(itemId)))
        {
            collected = true;
            HidePickup();
        }
    }

    private void OnDisable()
    {
        if (revealSource != null)
            revealSource.TargetCompleted -= HandleTargetCompleted;
        MobileStageControls.ClearInteraction(this);
    }

    private void Update()
    {
        if (!revealed || collected)
            return;

        if (visualRoot != null)
        {
            Transform visual = visualRoot.transform;
            visual.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            Vector3 localPosition = visualStartLocalPosition;
            localPosition.y += Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            visual.localPosition = localPosition;
        }

        if (playerInRange && Input.GetKeyDown(interactKey))
            TryCollect();
    }

    public void Reveal()
    {
        if (revealed || collected)
            return;

        revealed = true;
        if (visualRoot != null)
            visualRoot.SetActive(true);
        if (pickupCollider != null)
            pickupCollider.enabled = true;
        if (playerInRange)
            OfferMobileInteraction();

        Debug.Log($"[PassiveRewardPickup] {PassiveItemCatalog.GetDisplayName(itemId)} revealed and is ready to collect.", this);
    }

    public bool TryCollect()
    {
        if (!revealed || collected || !playerInRange)
            return false;

        bool granted = PassiveRewardManager.GrantPassiveReward(itemId, "ERODE hidden reward");
        PassiveItemInventory inventory = PassiveItemInventory.GetOrCreate();
        bool alreadyAvailable = inventory != null
            && (inventory.HasOwnedPassive(itemId) || inventory.HasPendingPassive(itemId));
        if (!granted && !alreadyAvailable)
            return false;

        collected = true;
        MobileStageControls.ClearInteraction(this);
        HidePickup();
        Debug.Log($"[PassiveRewardPickup] Collected {PassiveItemCatalog.GetDisplayName(itemId)}.", this);
        return true;
    }

    private void HandleTargetCompleted(VocabularyWorldTarget source)
    {
        if (source == revealSource)
            Reveal();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
            return;

        playerInRange = true;
        if (revealed && !collected)
        {
            OfferMobileInteraction();
            Debug.Log($"[PassiveRewardPickup] {PassiveItemCatalog.GetDisplayName(itemId)} is in interaction range.", this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
            return;

        playerInRange = false;
        MobileStageControls.ClearInteraction(this);
    }

    private void OfferMobileInteraction()
    {
        MobileStageControls.OfferInteraction(
            this,
            "COLLECT",
            () => TryCollect());
    }

    private bool IsPlayer(Collider other)
    {
        if (other == null)
            return false;
        if (other.CompareTag(playerTag))
            return true;
        if (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(playerTag))
            return true;
        return other.transform.root != null && other.transform.root.CompareTag(playerTag);
    }

    private void HidePickup()
    {
        if (pickupCollider != null)
            pickupCollider.enabled = false;
        if (visualRoot != null)
            visualRoot.SetActive(false);
    }
}
