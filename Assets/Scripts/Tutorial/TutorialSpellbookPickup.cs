using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class TutorialSpellbookPickup : MonoBehaviour
{
    public event Action Collected;
    public event Action<bool> PlayerRangeChanged;

    [Header("References")]
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private Collider pickupCollider;
    [SerializeField] private Transform guideTarget;
    [SerializeField] private ParticleSystem revealParticles;

    [Header("Interaction")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;

    [Header("Reveal")]
    [SerializeField, Min(0f)] private float revealDuration = 0.45f;
    [SerializeField] private AnimationCurve revealCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Floating")]
    [SerializeField, Min(0f)] private float bobHeight = 0.18f;
    [SerializeField, Min(0f)] private float bobSpeed = 1.6f;
    [SerializeField] private float rotationSpeed = 28f;

    private Vector3 baseLocalPosition;
    private Vector3 fullScale;
    private bool revealed;
    private bool collected;
    private bool playerInRange;
    private readonly HashSet<Collider> playerCollidersInRange = new();
    private Transform runtimeGuideTarget;

    public string InteractionKeyLabel => interactionKey.ToString();

    public Transform GuideTarget =>
        guideTarget != null
            ? guideTarget
            : runtimeGuideTarget != null
                ? runtimeGuideTarget
                : transform;

    private void Awake()
    {
        if (visualRoot == null)
            visualRoot = gameObject;

        if (pickupCollider == null)
            pickupCollider = GetComponent<Collider>();

        if (guideTarget == null && pickupCollider != null)
        {
            runtimeGuideTarget = new GameObject("GuideTarget_Runtime").transform;
            runtimeGuideTarget.SetParent(transform, true);
            runtimeGuideTarget.position = pickupCollider.bounds.center;
        }

        baseLocalPosition = visualRoot.transform.localPosition;
        fullScale = visualRoot.transform.localScale;
    }

    private void Update()
    {
        if (revealed
            && !collected
            && playerInRange
            && Input.GetKeyDown(interactionKey))
        {
            TryCollect();
        }

        if (!revealed || collected || visualRoot == null)
            return;

        float yOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        visualRoot.transform.localPosition = baseLocalPosition + Vector3.up * yOffset;
        visualRoot.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
    }

    public void PrepareHidden()
    {
        StopAllCoroutines();
        revealed = false;
        collected = false;
        playerCollidersInRange.Clear();
        SetPlayerInRange(false);

        if (pickupCollider != null)
            pickupCollider.enabled = false;

        if (visualRoot != null)
        {
            visualRoot.SetActive(true);
            visualRoot.transform.localPosition = baseLocalPosition;
            visualRoot.transform.localScale = Vector3.zero;
        }
    }

    public void Reveal()
    {
        if (revealed || collected)
            return;

        revealed = true;

        if (visualRoot != null)
        {
            visualRoot.SetActive(true);
            visualRoot.transform.localPosition = baseLocalPosition;
        }

        if (pickupCollider != null)
            pickupCollider.enabled = true;

        if (revealParticles != null)
            revealParticles.Play();

        StartCoroutine(AnimateReveal());
    }

    private IEnumerator AnimateReveal()
    {
        if (visualRoot == null)
            yield break;

        if (revealDuration <= 0f)
        {
            visualRoot.transform.localScale = fullScale;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < revealDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / revealDuration);
            visualRoot.transform.localScale = Vector3.LerpUnclamped(
                Vector3.zero,
                fullScale,
                revealCurve.Evaluate(normalizedTime));
            yield return null;
        }

        visualRoot.transform.localScale = fullScale;
    }

    public bool TryCollect()
    {
        if (!revealed || collected || !playerInRange)
            return false;

        collected = true;

        if (pickupCollider != null)
            pickupCollider.enabled = false;

        Collected?.Invoke();

        if (visualRoot != null)
            visualRoot.transform.localScale = Vector3.zero;

        return true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!revealed || collected || !IsPlayerCollider(other))
            return;

        bool wasInRange = playerCollidersInRange.Count > 0;
        playerCollidersInRange.Add(other);

        if (!wasInRange)
            SetPlayerInRange(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayerCollider(other))
            return;

        playerCollidersInRange.Remove(other);

        if (playerCollidersInRange.Count == 0)
            SetPlayerInRange(false);
    }

    private void SetPlayerInRange(bool inRange)
    {
        if (playerInRange == inRange)
            return;

        playerInRange = inRange;
        PlayerRangeChanged?.Invoke(playerInRange);
    }

    private static bool IsPlayerCollider(Collider other)
    {
        if (other == null)
            return false;

        return other.CompareTag("Player")
               || (other.transform.root != null
                   && other.transform.root.CompareTag("Player"));
    }
}
