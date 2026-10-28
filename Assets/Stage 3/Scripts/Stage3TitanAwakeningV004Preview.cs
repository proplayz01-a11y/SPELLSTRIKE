using UnityEngine;

/// <summary>
/// Animation-only review driver for the Google Flow -> GVHMR Titan awakening.
/// This is NOT the Node 4 boss AI and does not deal damage or advance progression.
/// </summary>
[DisallowMultipleComponent]
public sealed class Stage3TitanAwakeningV004Preview : MonoBehaviour
{
    private static readonly int Awaken = Animator.StringToHash("Awaken");
    private static readonly int Walking = Animator.StringToHash("Walking");
    private static readonly int Hammerfist = Animator.StringToHash("Hammerfist");

    [SerializeField] private Animator animator;
    [SerializeField] private bool playOnStart;
    private bool awakeningStarted;

    private void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
    }

    private void Start()
    {
        if (playOnStart) PreviewAwakening();
    }

    // Explicit post-laser entry point. Never starts the boss combat AI.
    public void StartAwakening() => PreviewAwakening();

    [ContextMenu("Preview Titan Awakening V004")]
    public void PreviewAwakening()
    {
        if (awakeningStarted)
            return;
        if (!animator || !gameObject.activeInHierarchy)
        {
            Debug.LogWarning("[Titan V004] Preview requires an active GameObject with Animator.", this);
            return;
        }
        animator.SetBool(Walking, false);
        animator.ResetTrigger(Hammerfist);
        animator.ResetTrigger(Awaken);
        animator.Play("DormantStatueV004", 0, 0f);
        animator.Update(0f);
        animator.SetTrigger(Awaken);
        awakeningStarted = true;
    }

    [ContextMenu("Toggle Titan Walk Preview")]
    public void ToggleWalk()
    {
        if (animator) animator.SetBool(Walking, !animator.GetBool(Walking));
    }

    [ContextMenu("Preview Hammerfist V003")]
    public void PreviewHammerfist()
    {
        if (animator) animator.SetTrigger(Hammerfist);
    }

    // Existing approved Hammerfist clip has one AnimationEvent at impact.
    // Preview-only: never apply combat damage here.
    public void OnHammerfistImpact()
    {
        Debug.Log("[Titan V004] Hammerfist contact event (visual review only, zero damage).", this);
    }
}
