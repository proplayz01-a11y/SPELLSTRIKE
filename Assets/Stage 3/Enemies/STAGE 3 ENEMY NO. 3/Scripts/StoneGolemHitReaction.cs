using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public sealed class StoneGolemHitReaction : MonoBehaviour
{
    public const string LowerLayerName = "Lower Body Hit Step";
    public const string UpperLayerName = "Upper Body Hit Reaction";
    private static readonly int LowerState = Animator.StringToHash(LowerLayerName + ".Step Backward");
    private static readonly int UpperState = Animator.StringToHash(UpperLayerName + ".Head Hit");

    [Header("Production references")]
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private CapsuleCollider body;
    [SerializeField] private AnimationClip lowerClip;
    [SerializeField] private AnimationClip upperClip;

    [Header("Being Hit")]
    [Tooltip("World-space backward retreat derived from the authored Step Backward clip. Intentionally independent of the Golem root scale so scene scale overrides do not turn one step into a multi-metre launch. NavMesh/solids can still shorten it.")]
    [SerializeField, Min(0f)] private float retreatDistance = 1.003249f;
    [Tooltip("Normalized source hip travel; copied by the visible hit setup menu.")]
    [SerializeField] private AnimationCurve retreatProgress = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField, Min(0f)] private float fadeInSeconds = 0.06f;
    [SerializeField, Min(0.01f)] private float fadeOutSeconds = 0.12f;
    [Tooltip("Extra time after the longest clip before clearing missing events/layers.")]
    [SerializeField, Min(0.1f)] private float failSafePadding = 0.4f;

    [Header("Runtime — read only")]
    [SerializeField] private bool active;
    [SerializeField] private int reactionsStarted;

    private int lowerLayer = -1;
    private int upperLayer = -1;
    private int startedFrame;
    private float startedTime;
    private float previousProgress;
    private float lowerFinishedTime = -1f;
    private float upperFinishedTime = -1f;
    private Vector3 retreatDirection;
    private bool warnedMissingSetup;
    private readonly RaycastHit[] solidHits = new RaycastHit[12];

    public bool IsActive => active;
    public int ReactionsStarted => reactionsStarted;
    public float RetreatDistance => retreatDistance;

    public void Initialize(Animator modelAnimator, NavMeshAgent navAgent, CapsuleCollider collider)
    {
        animator = modelAnimator;
        agent = navAgent;
        body = collider;
        lowerLayer = animator != null ? animator.GetLayerIndex(LowerLayerName) : -1;
        upperLayer = animator != null ? animator.GetLayerIndex(UpperLayerName) : -1;
        ResetReaction();
    }

    // Called only by StoneGolemController after battle/attack guards. EnemyHealth
    // already dispatches TriggerBeingHit once; no HealthChanged subscription.
    public bool TryBegin()
    {
        if (!isActiveAndEnabled || animator == null || agent == null
            || !agent.enabled || !agent.isOnNavMesh || lowerClip == null || upperClip == null
            || lowerLayer < 1 || upperLayer < 1
            || !animator.HasState(lowerLayer, LowerState) || !animator.HasState(upperLayer, UpperState))
        {
            if (!warnedMissingSetup)
            {
                Debug.LogWarning("[StoneGolem] Being Hit production references/layers or NavMesh binding are missing.", this);
                warnedMissingSetup = true;
            }
            return false;
        }
        warnedMissingSetup = false;
        active = true;
        reactionsStarted++;
        startedTime = Time.time;
        startedFrame = Time.frameCount;
        previousProgress = 0f;
        lowerFinishedTime = upperFinishedTime = -1f;
        retreatDirection = -transform.forward;
        retreatDirection.y = 0f;
        retreatDirection.Normalize();
        // Play is evaluated later this frame. Completion never inspects stale
        // normalizedTime from the preceding reaction (the Archer repeated-hit fix).
        animator.Play(LowerState, lowerLayer, 0f);
        animator.Play(UpperState, upperLayer, 0f);
        animator.SetLayerWeight(lowerLayer, fadeInSeconds > 0f ? 0f : 1f);
        animator.SetLayerWeight(upperLayer, fadeInSeconds > 0f ? 0f : 1f);
        return true;
    }

    // The combat controller calls this before its normal state update. While true,
    // only this bounded retreat owns movement; Base Layer Stop continues evaluating.
    public bool Tick()
    {
        if (!active) return false;
        if (animator == null || !animator.isActiveAndEnabled || agent == null
            || !agent.enabled || !agent.isOnNavMesh)
        {
            ResetReaction();
            return false;
        }
        float elapsed = Time.time - startedTime;
        float maximum = Mathf.Max(lowerClip.length, upperClip.length) + failSafePadding + fadeOutSeconds;
        if (elapsed > maximum)
        {
            Debug.LogWarning("[StoneGolem] Being Hit finish event timed out; layers cleared and combat released.", this);
            ResetReaction();
            return false;
        }
        // Do not act on a layer before Animator.Play has had an evaluation frame.
        if (Time.frameCount <= startedFrame) return true;

        float progress = Mathf.Clamp01(retreatProgress.Evaluate(Mathf.Clamp01(elapsed / lowerClip.length)));
        float increment = Mathf.Max(0f, progress - previousProgress);
        previousProgress = progress;
        if (increment > 0f)
            MoveRetreat(retreatDirection * (retreatDistance * increment));
        float incoming = fadeInSeconds > 0f ? Mathf.Clamp01(elapsed / fadeInSeconds) : 1f;
        float lowerWeight = Weight(incoming, lowerFinishedTime);
        float upperWeight = Weight(incoming, upperFinishedTime);
        animator.SetLayerWeight(lowerLayer, lowerWeight);
        animator.SetLayerWeight(upperLayer, upperWeight);
        if (lowerFinishedTime >= 0f && upperFinishedTime >= 0f
            && lowerWeight <= 0f && upperWeight <= 0f)
        {
            ResetReaction();
            return false;
        }
        return true;
    }

    public void HandleLowerFinished()
    {
        if (AcceptFinish(lowerClip) && lowerFinishedTime < 0f)
            lowerFinishedTime = Time.time;
    }

    public void HandleUpperFinished()
    {
        if (AcceptFinish(upperClip) && upperFinishedTime < 0f)
            upperFinishedTime = Time.time;
    }

    private bool AcceptFinish(AnimationClip clip)
    {
        // An event queued from a previous evaluation cannot end a freshly
        // restarted reaction. This is a finish safeguard, never a damage window.
        return active && clip != null && Time.frameCount > startedFrame + 1
            && Time.time - startedTime >= clip.length - 1f / 30f - Time.deltaTime - 0.03f;
    }

    private float Weight(float incoming, float finishedTime)
    {
        return finishedTime < 0f ? incoming
            : Mathf.Min(incoming, Mathf.Clamp01(1f - (Time.time - finishedTime) / fadeOutSeconds));
    }

    private void MoveRetreat(Vector3 delta)
    {
        float distance = delta.magnitude;
        if (distance <= 0.00001f) return;
        Vector3 direction = delta / distance;
        // Query from the actual agent position each frame; never Warp, sample to
        // another island, chase a moving player, or assign transform.position.
        if (agent.Raycast(agent.nextPosition + delta, out NavMeshHit edge))
            distance = Mathf.Min(distance, Mathf.Max(0f, edge.distance - 0.05f));
        if (body != null && body.enabled && distance > 0f)
        {
            Vector3 centre = transform.TransformPoint(body.center);
            float radius = body.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
            float half = Mathf.Max(0f, body.height * transform.lossyScale.y * 0.5f - radius);
            int count = Physics.CapsuleCastNonAlloc(centre - Vector3.up * half,
                centre + Vector3.up * half, radius, direction, solidHits,
                distance + 0.03f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (solidHits[i].collider != null && !solidHits[i].collider.transform.IsChildOf(transform))
                    distance = Mathf.Min(distance, Mathf.Max(0f, solidHits[i].distance - 0.03f));
        }
        if (distance > 0f) agent.Move(direction * distance);
    }

    public void ResetReaction()
    {
        active = false;
        lowerFinishedTime = upperFinishedTime = -1f;
        if (animator == null) return;
        ClearLayer(lowerLayer, LowerLayerName);
        ClearLayer(upperLayer, UpperLayerName);
    }

    private void ClearLayer(int layer, string name)
    {
        if (layer < 1 || layer >= animator.layerCount) return;
        animator.SetLayerWeight(layer, 0f);
        int empty = Animator.StringToHash(name + ".Empty");
        if (animator.isActiveAndEnabled && animator.HasState(layer, empty))
            animator.Play(empty, layer, 0f);
    }

    private void OnDisable() => ResetReaction();
}
