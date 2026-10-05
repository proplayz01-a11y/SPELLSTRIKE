using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent), typeof(EnemyHealth), typeof(CapsuleCollider))]
public sealed class StoneGolemController : MonoBehaviour
{
    public enum CombatState { Inactive, HeavyWalk, Stop, GroundSlam, Recovery }

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private CapsuleCollider combatCollider;
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Heavy Walk")]
    [SerializeField, Min(0.05f)] private float walkSpeed = 1.196925f;
    [SerializeField, Min(0.05f)] private float accelerationTime = 0.75f;
    [Tooltip("Audited planted-foot world speed at the production ModelRig scale. Retune if visual scale changes.")]
    [SerializeField, Min(0.05f)] private float walkClipWorldSpeed = 1.196925f;
    [SerializeField, Min(1f)] private float turnSpeed = 120f;
    [SerializeField, Min(0.05f)] private float repathInterval = 0.25f;

    [Header("Ground Slam — Jump, damage only at landing")]
    [SerializeField, Min(0f)] private float damage = 15f;
    [SerializeField, Min(0.1f)] private float radius = 3f;
    [SerializeField, Min(0f)] private float cooldown = 4f;
    [SerializeField, Min(0.1f)] private float attackRange = 2.7f;
    [SerializeField, Min(0f)] private float recoverySeconds = 0.75f;
    [SerializeField, Range(1f, 45f)] private float attackFacingTolerance = 10f;
    [Tooltip("Fail closed if the authored finish event is missing. Never generates damage.")]
    [SerializeField, Min(4f)] private float animationWatchdogSeconds = 6f;

    [Header("Presentation")]
    [SerializeField, Min(0f)] private float blendSeconds = 0.12f;
    [SerializeField] private bool debugLogs;

    [Header("Runtime — read only")]
    [SerializeField] private CombatState state = CombatState.Inactive;
    [SerializeField] private bool battleStarted;
    [SerializeField] private int slamsStarted;
    [SerializeField] private int landingImpacts;
    [SerializeField] private int playerHits;

    private static readonly int WalkHash = Animator.StringToHash("Base Layer.Heavy Walk");
    private static readonly int StopHash = Animator.StringToHash("Base Layer.Stop");
    private static readonly int SlamHash = Animator.StringToHash("Base Layer.Ground Slam");
    private static readonly int WalkRateHash = Animator.StringToHash("WalkRate");
    private float enteredStateTime;
    private float nextSlamTime;
    private float nextRepathTime;
    private float nextPlayerResolveTime;
    private bool stopSettled;
    private bool impactApplied;
    private bool navWarningShown;
    private Collider playerBody;

    public CombatState State => state;
    public bool IsBattleStarted => battleStarted;
    public int SlamsStarted => slamsStarted;
    public int LandingImpacts => landingImpacts;
    public int PlayerHits => playerHits;
    public float ImpactRadius => radius;

    private void Awake()
    {
        agent = agent != null ? agent : GetComponent<NavMeshAgent>();
        combatCollider = combatCollider != null ? combatCollider : GetComponent<CapsuleCollider>();
        enemyHealth = enemyHealth != null ? enemyHealth : GetComponent<EnemyHealth>();
        animator = animator != null ? animator : GetComponentInChildren<Animator>(true);
        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Play(StopHash, 0, 0f);
        }
        agent.updateRotation = false;
        agent.updatePosition = true;
        agent.autoBraking = true;
        agent.stoppingDistance = Mathf.Max(0.1f, attackRange - 0.2f);
        EnemyNavMovement.ApplySpeed(agent, walkSpeed, accelerationTime);
        combatCollider.enabled = false;
        agent.enabled = false;
        gameObject.tag = "Untagged";
        state = CombatState.Inactive;
        battleStarted = false;
    }

    public void StartBattle()
    {
        if (battleStarted || enemyHealth == null || enemyHealth.IsDead)
            return;
        if (animator == null || !animator.HasState(0, SlamHash)
            || !animator.HasState(0, WalkHash) || !animator.HasState(0, StopHash))
        {
            Debug.LogError("[StoneGolem] Missing the production Animator/three required states.", this);
            return;
        }
        ResolvePlayer();
        battleStarted = true;
        combatCollider.enabled = true;
        gameObject.tag = "Enemy";
        agent.enabled = true;
        nextSlamTime = Time.time;
        EnterWalk();
        Log("Node 3 battle started.");
    }

    private void Update()
    {
        if (!battleStarted)
        {
            // The shared Stage 3 bootstrap can bind/enable authored agents after
            // Awake. Binding is allowed; movement still belongs to StartBattle.
            StopAgent();
            return;
        }
        if (enemyHealth == null || enemyHealth.IsDead)
        {
            StopAgent();
            return;
        }
        if (playerHealth == null && Time.time >= nextPlayerResolveTime)
        {
            ResolvePlayer();
            nextPlayerResolveTime = Time.time + 0.5f;
        }
        if (playerHealth == null || playerHealth.currentHealth <= 0)
        {
            StopAgent();
            if (state != CombatState.GroundSlam && state != CombatState.Stop)
                EnterStop();
            return;
        }
        if (!agent.enabled || !agent.isOnNavMesh)
        {
            if (!navWarningShown)
            {
                navWarningShown = true;
                Debug.LogWarning("[StoneGolem] Waiting for the authored Stage 3 NavMesh binding; no warp or attack.", this);
            }
            return;
        }
        navWarningShown = false;
        switch (state)
        {
            case CombatState.HeavyWalk:
                FacePlayer();
                if (DistanceToPlayer() <= attackRange)
                {
                    EnterStop();
                    return;
                }
                if (Time.time >= nextRepathTime)
                {
                    nextRepathTime = Time.time + repathInterval;
                    EnemyNavMovement.TrySetDestination(agent, playerHealth.transform.position);
                }
                animator.SetFloat(WalkRateHash,
                    Mathf.Clamp(agent.velocity.magnitude / walkClipWorldSpeed, 0f, 1.4f));
                break;
            case CombatState.Stop:
                FacePlayer();
                if (DistanceToPlayer() > attackRange + 0.35f)
                    EnterWalk();
                else if (stopSettled && Time.time >= nextSlamTime && IsFacingPlayer())
                    EnterSlam();
                break;
            case CombatState.GroundSlam:
                // This audited clip jumps in place. Its skinned Hips/feet contain the
                // vertical motion; the grounded agent root never warps or homes.
                if (Time.time - enteredStateTime > animationWatchdogSeconds)
                {
                    Debug.LogError("[StoneGolem] Ground Slam finish event missing; recovering without synthetic damage.", this);
                    nextSlamTime = Time.time + cooldown;
                    EnterRecovery();
                }
                break;
            case CombatState.Recovery:
                if (stopSettled && Time.time - enteredStateTime >= recoverySeconds)
                {
                    if (DistanceToPlayer() > attackRange + 0.35f)
                        EnterWalk();
                    else
                    {
                        state = CombatState.Stop;
                        enteredStateTime = Time.time;
                    }
                }
                break;
        }
    }

    public void HandleStopSettled()
    {
        if (battleStarted && (state == CombatState.Stop || state == CombatState.Recovery))
            stopSettled = true;
    }

    public void HandleGroundSlamImpact()
    {
        if (!battleStarted || state != CombatState.GroundSlam || impactApplied
            || enemyHealth == null || enemyHealth.IsDead || !agent.enabled || !agent.isOnNavMesh)
            return;
        impactApplied = true;
        landingImpacts++;
        nextSlamTime = Time.time + cooldown;
        ResolvePlayer();
        if (playerHealth != null && playerHealth.currentHealth > 0)
        {
            Vector3 closest = playerBody != null && playerBody.enabled
                ? playerBody.ClosestPoint(transform.position)
                : playerHealth.transform.position;
            if ((closest - transform.position).sqrMagnitude <= radius * radius)
            {
                playerHealth.TakeDamage(damage);
                playerHits++;
            }
        }
        Log($"Landing impact #{landingImpacts}; player hits={playerHits}.");
    }

    public void HandleGroundSlamFinished()
    {
        if (!battleStarted || state != CombatState.GroundSlam)
            return;
        if (!impactApplied)
        {
            Debug.LogError("[StoneGolem] Slam finished without its landing event; no fallback damage.", this);
            nextSlamTime = Time.time + cooldown;
        }
        EnterRecovery();
    }

    private void EnterWalk()
    {
        state = CombatState.HeavyWalk;
        enteredStateTime = Time.time;
        nextRepathTime = Time.time;
        animator.SetFloat(WalkRateHash, 0f);
        animator.CrossFadeInFixedTime(WalkHash, blendSeconds, 0, 0f);
        if (agent.enabled && agent.isOnNavMesh)
            agent.isStopped = false;
        Log("Heavy Walk.");
    }

    private void EnterStop()
    {
        StopAgent();
        state = CombatState.Stop;
        enteredStateTime = Time.time;
        stopSettled = false;
        animator.CrossFadeInFixedTime(StopHash, blendSeconds, 0, 0f);
        Log("Stop / stationary loop.");
    }

    private void EnterSlam()
    {
        StopAgent();
        state = CombatState.GroundSlam;
        enteredStateTime = Time.time;
        impactApplied = false;
        slamsStarted++;
        // Facing is committed here. Update never rotates during this attack.
        animator.CrossFadeInFixedTime(SlamHash, blendSeconds, 0, 0f);
        Log($"Ground Slam #{slamsStarted}; waiting for authored landing.");
    }

    private void EnterRecovery()
    {
        StopAgent();
        state = CombatState.Recovery;
        enteredStateTime = Time.time;
        stopSettled = false;
        animator.CrossFadeInFixedTime(StopHash, blendSeconds, 0, 0f);
        Log("Recovery / Stop.");
    }

    private void StopAgent()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;
        agent.ResetPath();
        agent.velocity = Vector3.zero;
        agent.isStopped = true;
    }

    private void ResolvePlayer()
    {
        if (playerHealth == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerHealth = player.GetComponent<PlayerHealth>()
                    ?? player.GetComponentInChildren<PlayerHealth>();
        }
        if (playerHealth != null && playerBody == null)
            playerBody = playerHealth.GetComponent<Collider>()
                ?? playerHealth.GetComponentInChildren<Collider>();
    }

    private float DistanceToPlayer()
    {
        // The canonical player has a large scaled CharacterController. Range to
        // its collider surface prevents trying to reach its obstructed centre.
        Vector3 target = playerBody != null && playerBody.enabled
            ? playerBody.ClosestPoint(transform.position) : playerHealth.transform.position;
        Vector3 delta = target - transform.position;
        delta.y = 0f;
        return delta.magnitude;
    }

    private void FacePlayer()
    {
        Vector3 delta = playerHealth.transform.position - transform.position;
        delta.y = 0f;
        if (delta.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.RotateTowards(transform.rotation,
                Quaternion.LookRotation(delta), turnSpeed * Time.deltaTime);
    }

    private bool IsFacingPlayer()
    {
        Vector3 delta = playerHealth.transform.position - transform.position;
        delta.y = 0f;
        return delta.sqrMagnitude < 0.0001f
            || Vector3.Angle(transform.forward, delta) <= attackFacingTolerance;
    }

    private void OnDisable()
    {
        StopAgent();
        battleStarted = false;
        if (combatCollider != null)
            combatCollider.enabled = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.45f, 0.08f, 1f);
        Gizmos.DrawWireSphere(transform.position, radius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    private void Log(string message)
    {
        if (debugLogs)
            Debug.Log("[StoneGolem] " + message, this);
    }
}
