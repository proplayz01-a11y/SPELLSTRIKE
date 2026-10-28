using UnityEngine;
using UnityEngine.Animations.Rigging;

/// <summary>
/// Additive attention only: a towering Titan follows Van's actual upper body.
/// This never rotates the actor root, writes animation curves, or touches foot IK.
/// Place the target outside the animated Animator hierarchy and bind it once.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-250)]
public sealed class TitanV017ProceduralAttention : MonoBehaviour
{
    [Header("Wired on the isolated V017 QA prefab")]
    [SerializeField] private Animator titanAnimator;
    [SerializeField] private TitanEarthbreakerController titanController;
    [SerializeField] private TitanV017FourRightStompIKPreview stomp;
    [SerializeField] private Rig attentionRig;
    [SerializeField] private MultiAimConstraint neckAim;
    [SerializeField] private MultiAimConstraint chestAim;
    [SerializeField] private Transform worldAimProxy;
    [SerializeField] private PlayerHealth player;
    [Tooltip("ONLY for an isolated QA scene with Titan combat disabled; never enable on the production prefab.")]
    [SerializeField] private bool isolatedReviewAttention;

    [Header("Tower-scale motion / anatomical attention")]
    [SerializeField, Range(20f, 85f)] private float maximumYawFromBody = 65f;
    [SerializeField, Range(-89f, -45f)] private float steepestDownwardLook = -82f;
    [SerializeField, Range(-40f, 0f)] private float shallowestDownwardLook = -8f;
    [SerializeField, Min(0.05f)] private float movingTargetSmoothSeconds = 0.45f;
    [SerializeField, Min(1f)] private float maximumTargetFollowSpeed = 65f;
    [SerializeField, Min(0.05f)] private float weightBlendSeconds = 0.85f;
    [SerializeField, Range(0f, 1f)] private float idleHeadWeight = 0.78f;
    [SerializeField, Range(0f, 0.4f)] private float idleChestWeight = 0.22f;
    [SerializeField, Range(0f, 1f)] private float walkingHeadWeight = 0.58f;
    [SerializeField, Range(0f, 0.4f)] private float walkingChestWeight = 0.13f;
    [SerializeField, Range(0f, 0.5f)] private float committedStompHeadWeight = 0.18f;

    // In the standalone arena the production boss controller is deliberately
    // disabled, so its State remains stale while the QA menu directly plays
    // Animator states. Gate attention from the real sampled Animator state in
    // that sandbox. Do not interfere with the production combat state machine.
    private static readonly int WalkHash = Animator.StringToHash("Base Layer.ColossalHeavyWalk");
    private static readonly int StompHash = Animator.StringToHash("Base Layer.ColossalStompRightFootV017");
    private static readonly int HammerHash = Animator.StringToHash("Base Layer.HammerfistV003");
    private static readonly int SlamHash = Animator.StringToHash("Base Layer.EarthbreakerSlamV003");
    private static readonly int DeathHash = Animator.StringToHash("Base Layer.TitanDeathFromRightPreview");
    private static readonly int DormantHash = Animator.StringToHash("Base Layer.DormantStatueV004");
    private static readonly int AwakeningHash = Animator.StringToHash("Base Layer.AwakeningV004");

    private Transform upperBody;
    private PlayerHealth cachedPlayer;
    private float nextPlayerSearch;
    private Vector3 targetVelocity;
    private bool proxyInitialized;
    private bool loggedRigError;

    public bool HasValidPlayer => player && player.isActiveAndEnabled && player.currentHealth > 0;
    public Transform AimProxy => worldAimProxy;

    private void Awake()
    {
        if (!titanAnimator) titanAnimator = GetComponentInChildren<Animator>(true);
        if (!titanController) titanController = GetComponentInChildren<TitanEarthbreakerController>(true);
        if (!stomp) stomp = GetComponentInChildren<TitanV017FourRightStompIKPreview>(true);
        // The actor root's Awake can run before child Rig/MultiAim behaviours
        // receive OnEnable. Their isActiveAndEnabled is briefly false despite
        // perfectly valid serialized references. Never permanently disable
        // attention during that hierarchy-initialization window.
        DisableAims();
    }

    private bool HasSafeRig()
    {
        if (!titanAnimator || !attentionRig || !attentionRig.isActiveAndEnabled ||
            !neckAim || !chestAim || !worldAimProxy ||
            !neckAim.isActiveAndEnabled || !chestAim.isActiveAndEnabled)
            return false;
        var neckSources = neckAim.data.sourceObjects;
        var chestSources = chestAim.data.sourceObjects;
        return neckSources.Count == 1 && chestSources.Count == 1 &&
            neckSources[0].transform == worldAimProxy &&
            chestSources[0].transform == worldAimProxy &&
            !worldAimProxy.IsChildOf(titanAnimator.transform) &&
            neckAim.data.constrainedObject && neckAim.data.constrainedObject.IsChildOf(titanAnimator.transform) &&
            chestAim.data.constrainedObject && chestAim.data.constrainedObject.IsChildOf(titanAnimator.transform);
    }

    private void Update()
    {
        if (!HasSafeRig())
        {
            DisableAims();
            if (!loggedRigError)
            {
                Debug.LogError("[Titan V017 Attention] Rig/aim bindings invalid after initialization; gaze safely disabled until repaired.", this);
                loggedRigError = true;
            }
            return;
        }
        loggedRigError = false;

        // Never redirect constraints at runtime. The prefab's two constraints
        // always refer to the same external moving Transform.
        ResolveVan();
        bool valid = HasValidPlayer && upperBody && !upperBody.IsChildOf(titanAnimator.transform);
        bool canWatch = isolatedReviewAttention ||
            (titanController && titanController.isActiveAndEnabled && titanController.IsAwakened &&
             titanController.State != TitanEarthbreakerController.TitanState.Dead);

        int qaAnimatorState = isolatedReviewAttention
            ? titanAnimator.GetCurrentAnimatorStateInfo(0).fullPathHash : 0;
        bool qaSuppressed = isolatedReviewAttention &&
            (qaAnimatorState == HammerHash || qaAnimatorState == SlamHash ||
             qaAnimatorState == DeathHash || qaAnimatorState == DormantHash ||
             qaAnimatorState == AwakeningHash);

        float headWeight = 0f;
        float chestWeight = 0f;
        if (valid && canWatch && !qaSuppressed)
        {
            Vector3 desired = upperBody.position; // Van's moving CHEST, not a gizmo.
            if (!proxyInitialized)
            {
                worldAimProxy.position = desired;
                targetVelocity = Vector3.zero;
                proxyInitialized = true;
            }
            else
            {
                worldAimProxy.position = Vector3.SmoothDamp(worldAimProxy.position, desired,
                    ref targetVelocity, movingTargetSmoothSeconds,
                    maximumTargetFollowSpeed, Time.deltaTime);
            }

            Transform neck = neckAim.data.constrainedObject;
            Vector3 towardVan = desired - neck.position;
            Vector3 planar = Vector3.ProjectOnPlane(towardVan, titanAnimator.transform.up);
            Vector3 actorForward = Vector3.ProjectOnPlane(titanAnimator.transform.forward,
                titanAnimator.transform.up).normalized;
            float yaw = planar.sqrMagnitude > 0.01f
                ? Vector3.Angle(actorForward, planar.normalized) : 180f;
            float elevation = Mathf.Atan2(Vector3.Dot(towardVan, titanAnimator.transform.up),
                planar.magnitude) * Mathf.Rad2Deg;

            // Behind, above, or anatomically inaccessible: stop the gaze.
            // The root must turn via ordinary Heavy Walk, not the neck joint.
            if (yaw <= maximumYawFromBody &&
                elevation >= steepestDownwardLook && elevation <= shallowestDownwardLook)
            {
                if (titanController &&
                    (titanController.State == TitanEarthbreakerController.TitanState.Hammerfist ||
                     titanController.State == TitanEarthbreakerController.TitanState.EarthbreakerSlam))
                {
                    // These authored attacks have their own torso mechanics;
                    // do not twist the shoulders or neck into a foreign aim.
                    headWeight = 0f;
                    chestWeight = 0f;
                }
                else if (stomp && stomp.IsRunning ||
                    isolatedReviewAttention && qaAnimatorState == StompHash ||
                    titanController && titanController.State == TitanEarthbreakerController.TitanState.ColossalStompV017)
                {
                    headWeight = committedStompHeadWeight;
                    chestWeight = 0f;
                }
                else if (isolatedReviewAttention && qaAnimatorState == WalkHash ||
                         titanController && titanController.State == TitanEarthbreakerController.TitanState.Chasing)
                {
                    headWeight = walkingHeadWeight;
                    chestWeight = walkingChestWeight;
                }
                else
                {
                    headWeight = idleHeadWeight;
                    chestWeight = idleChestWeight;
                }
            }
        }
        else
        {
            // Van died/despawned or attention paused: do not chase his last position.
            proxyInitialized = false;
            targetVelocity = Vector3.zero;
        }

        float weightDelta = Time.deltaTime / Mathf.Max(0.05f, weightBlendSeconds);
        neckAim.weight = Mathf.MoveTowards(neckAim.weight, headWeight, weightDelta);
        chestAim.weight = Mathf.MoveTowards(chestAim.weight, chestWeight, weightDelta);
    }

    private void ResolveVan()
    {
        if (!player && Time.time >= nextPlayerSearch)
        {
            // Only the real live Player-tagged Van qualifies. Don't pick a
            // stale parked PlayerHealth component on a staging prop.
            GameObject tagged = GameObject.FindWithTag("Player");
            if (tagged) player = tagged.GetComponent<PlayerHealth>() ??
                tagged.GetComponentInParent<PlayerHealth>();
            nextPlayerSearch = Time.time + 0.5f;
        }
        if (player == cachedPlayer) return;
        cachedPlayer = player;
        upperBody = null;
        proxyInitialized = false;
        targetVelocity = Vector3.zero;
        if (!player) return;
        foreach (Transform bone in player.GetComponentsInChildren<Transform>(true))
        {
            if (bone.name == "mixamorig:Spine2") { upperBody = bone; break; }
        }
        // No ground-level Animator-root fallback: the genuine moving upper
        // body is required. A different Van rig must be explicitly mapped.
    }

    private void DisableAims()
    {
        if (neckAim) neckAim.weight = 0f;
        if (chestAim) chestAim.weight = 0f;
    }

    private void OnDisable()
    {
        DisableAims();
    }
}
