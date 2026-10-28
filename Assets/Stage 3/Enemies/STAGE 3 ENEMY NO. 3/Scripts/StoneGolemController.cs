using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent), typeof(EnemyHealth), typeof(CapsuleCollider))]
public sealed class StoneGolemController : MonoBehaviour
{
    public enum CombatState { Inactive, HeavyWalk, Stop, GroundSlam, Recovery, Dead }
    private enum PunchPhase { None, Right, Left, Recovery }

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private CapsuleCollider combatCollider;
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private StoneGolemHitReaction hitReaction;
    [SerializeField] private StageNodeController defeatReceiver;

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

    [Header("Ground Slam Crowd Control")]
    [SerializeField, Min(0f)] private float groundSlamStunDuration = 3f;
    [SerializeField, Range(0.01f, 1f)] private float groundSlamSlowMultiplier = 0.2f;
    [SerializeField, Min(0f)] private float groundSlamSlowDuration = 4f;
    [SerializeField, Range(0.05f, 1f)] private float debuffChaseSpeedMultiplier = 0.6f;

    [Header("Mutant Punch R/L — upper body layered")]
    [SerializeField] private AnimationClip rightPunchClip;
    [SerializeField] private AnimationClip leftPunchClip;
    [SerializeField, Min(0f)] private float punchDamage = 8f;
    [SerializeField, Min(0.1f)] private float punchRange = 2.7f;
    [SerializeField, Range(1f, 90f)] private float punchFacingTolerance = 55f;
    [SerializeField, Min(0f)] private float punchComboCooldown = 1f;
    [SerializeField, Min(0f)] private float punchRecoverySeconds = 0.25f;
    [SerializeField, Min(0f)] private float punchBlendSeconds = 0.12f;
    [Tooltip("Extra time beyond the authored punch clip before failing safe back to Stop.")]
    [SerializeField, Min(0.1f)] private float punchWatchdogPadding = 0.4f;

    [Header("Presentation")]
    [SerializeField, Min(0f)] private float blendSeconds = 0.12f;
    [SerializeField] private AnimationClip deathClip;
    [SerializeField, Min(0.1f)] private float deathCompletionFallbackPadding = 0.6f;
    [SerializeField] private bool debugLogs;

    [Header("Runtime — read only")]
    [SerializeField] private CombatState state = CombatState.Inactive;
    [SerializeField] private bool battleStarted;
    [SerializeField] private int slamsStarted;
    [SerializeField] private int landingImpacts;
    [SerializeField] private int playerHits;
    [SerializeField] private int punchCombosStarted;
    [SerializeField] private int punchImpacts;
    [SerializeField] private int punchPlayerHits;
    [SerializeField] private PunchPhase punchPhase;
    [SerializeField] private bool slamAirborne;
    [SerializeField] private bool deathCompleted;
    [SerializeField] private bool deathAnimationEventReceived;

    private static readonly int WalkHash = Animator.StringToHash("Base Layer.Heavy Walk");
    private static readonly int StopHash = Animator.StringToHash("Base Layer.Stop");
    private static readonly int SlamHash = Animator.StringToHash("Base Layer.Ground Slam");
    private static readonly int DeathHash = Animator.StringToHash("Base Layer.Death");
    private static readonly int WalkRateHash = Animator.StringToHash("WalkRate");
    private const string PunchLayerName = "Upper Body Punch";
    private static readonly int PunchEmptyHash = Animator.StringToHash(PunchLayerName + ".Empty");
    private static readonly int RightPunchHash = Animator.StringToHash(PunchLayerName + ".Right Punch");
    private static readonly int LeftPunchHash = Animator.StringToHash(PunchLayerName + ".Left Punch");
    private float enteredStateTime;
    private float nextSlamTime;
    private float nextRepathTime;
    private float nextPlayerResolveTime;
    private bool stopSettled;
    private bool impactApplied;
    private bool navWarningShown;
    private Collider playerBody;
    private MovementofPlayer playerMovement;
    private StoneGolemSlamDebuff activeSlamDebuff;
    private bool resumeWalkAfterHit;
    private int punchLayer = -1;
    private float punchEnteredTime;
    private float nextPunchTime;
    private bool punchImpactApplied;
    private bool punchUsesWalkLocomotion;
    private bool debuffPunchPressure;
    private PunchPhase nextDebuffPunchPhase = PunchPhase.Left;
    private float punchLayerTargetWeight;
    private bool debuffChaseLocomotionApplied;
    private float deathStartedTime = -1f;

    public CombatState State => state;
    public bool IsBattleStarted => battleStarted;
    public int SlamsStarted => slamsStarted;
    public int LandingImpacts => landingImpacts;
    public int PlayerHits => playerHits;
    public float ImpactRadius => radius;
    public float AttackRange => attackRange;
    public bool IsBeingHit => hitReaction != null && hitReaction.IsActive;
    public int HitReactionsStarted => hitReaction != null ? hitReaction.ReactionsStarted : 0;
    public bool IsPunching => punchPhase != PunchPhase.None;
    public int PunchCombosStarted => punchCombosStarted;
    public int PunchImpacts => punchImpacts;
    public int PunchPlayerHits => punchPlayerHits;
    public bool DebuffPunchPressureActive => debuffPunchPressure;
    public bool IsSlamAirborne => slamAirborne;
    public bool DeathCompleted => deathCompleted;
    public bool DeathAnimationEventReceived => deathAnimationEventReceived;

    private void Awake()
    {
        agent = agent != null ? agent : GetComponent<NavMeshAgent>();
        combatCollider = combatCollider != null ? combatCollider : GetComponent<CapsuleCollider>();
        enemyHealth = enemyHealth != null ? enemyHealth : GetComponent<EnemyHealth>();
        animator = animator != null ? animator : GetComponentInChildren<Animator>(true);
        hitReaction = hitReaction != null ? hitReaction : GetComponent<StoneGolemHitReaction>();
        if (hitReaction != null)
            hitReaction.Initialize(animator, agent, combatCollider);
        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Play(StopHash, 0, 0f);
            punchLayer = animator.GetLayerIndex(PunchLayerName);
            ResetPunchLayer();
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
        nextPunchTime = Time.time;
        EnterWalk();
        Log("Node 2 battle started.");
    }

    private void Update()
    {
        UpdatePunchLayerBlend();

        if (state == CombatState.Dead)
        {
            UpdateDeathCompletionFallback();
            return;
        }

        if (!battleStarted)
        {
            // The shared Stage 3 bootstrap can bind/enable authored agents after
            // Awake. Binding is allowed; movement still belongs to StartBattle.
            StopAgent();
            return;
        }
        if (enemyHealth == null || enemyHealth.IsDead)
        {
            hitReaction?.ResetReaction();
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
            hitReaction?.ResetReaction();
            resumeWalkAfterHit = false;
            debuffPunchPressure = false;
            ResetPunchLayer();
            StopAgent();
            if (state != CombatState.GroundSlam && state != CombatState.Stop)
                EnterStop();
            return;
        }
        if (!agent.enabled || !agent.isOnNavMesh)
        {
            hitReaction?.ResetReaction();
            if (!navWarningShown)
            {
                navWarningShown = true;
                Debug.LogWarning("[StoneGolem] Waiting for the authored Stage 3 NavMesh binding; no warp or attack.", this);
            }
            return;
        }
        navWarningShown = false;
        if (hitReaction != null && hitReaction.IsActive && hitReaction.Tick())
            return;
        if (resumeWalkAfterHit)
        {
            resumeWalkAfterHit = false;
            if (debuffPunchPressure && IsSlamDebuffActive())
                EnterDebuffWalk();
            else
                EnterWalk();
        }
        if (debuffPunchPressure)
        {
            UpdateDebuffPunchPressure();
            return;
        }
        if (punchPhase != PunchPhase.None)
        {
            UpdatePunch();
            return;
        }
        switch (state)
        {
            case CombatState.HeavyWalk:
                FacePlayer();
                float distanceToPlayer = DistanceToPlayer();
                if (Time.time >= nextSlamTime
                    && distanceToPlayer <= attackRange
                    && IsFacingPlayer())
                {
                    EnterSlam();
                    return;
                }
                if (Time.time < nextSlamTime
                    && Time.time >= nextPunchTime
                    && distanceToPlayer <= punchRange
                    && PunchSetupReady()
                    && IsFacingPlayer(punchFacingTolerance))
                {
                    EnterRightPunch();
                    return;
                }
                UpdateWalkLocomotion();
                break;
            case CombatState.Stop:
                FacePlayer();
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
                    EnterWalk();
                break;
        }
    }

    public void TriggerBeingHit()
    {
        if (!battleStarted || slamAirborne
            || enemyHealth == null || enemyHealth.IsDead || hitReaction == null)
            return;

        bool interruptingGroundedSlam = state == CombatState.GroundSlam;
        bool restarting = hitReaction.IsActive;
        if (!hitReaction.TryBegin())
            return;

        // Ground Slam is the only committed action that ignores BeingHit.
        // L/R punches are interruptible. Clear the punch layer immediately so
        // the upper-body Head Hit layer is not visually covered by a punch that
        // was already at full weight.
        if (punchPhase != PunchPhase.None)
            CancelPunchForHitReaction();

        if (!restarting)
        {
            resumeWalkAfterHit = state == CombatState.HeavyWalk || interruptingGroundedSlam;

            if (interruptingGroundedSlam)
            {
                // Before takeoff or after landing, Ground Slam is interruptible.
                // A pre-takeoff interruption consumes the attempt's cooldown so
                // the Golem cannot instantly restart the jump after BeingHit.
                slamAirborne = false;
                if (!impactApplied)
                    nextSlamTime = Time.time + cooldown;
                EnterStop();
            }
            else if (resumeWalkAfterHit)
                EnterStop();
        }
        StopAgent();
    }

    public void TriggerDeath()
    {
        if (state == CombatState.Dead)
            return;

        battleStarted = false;
        state = CombatState.Dead;
        deathCompleted = false;
        deathAnimationEventReceived = false;
        deathStartedTime = Time.time;
        slamAirborne = false;
        resumeWalkAfterHit = false;
        debuffPunchPressure = false;

        hitReaction?.ResetReaction();
        ResetPunchLayer();
        StopAgent();

        if (agent != null && agent.enabled)
        {
            if (agent.isOnNavMesh)
            {
                agent.ResetPath();
                agent.velocity = Vector3.zero;
                agent.isStopped = true;
            }
            agent.enabled = false;
        }

        if (combatCollider != null)
            combatCollider.enabled = false;

        gameObject.tag = "Untagged";
        ResolveDefeatReceiver();

        if (animator == null || !animator.HasState(0, DeathHash))
        {
            Debug.LogWarning("[StoneGolem] Death state is missing. Completing defeat immediately to avoid progression soft-lock.", this);
            CompleteDeath(false);
            return;
        }

        animator.CrossFadeInFixedTime(DeathHash, blendSeconds, 0, 0f);
        Log("Death started; combat disabled and waiting for OnDeathFinished.");
    }

    public void HandleDeathFinished()
    {
        if (state != CombatState.Dead || deathCompleted)
            return;

        CompleteDeath(true);
    }

    private void UpdateDeathCompletionFallback()
    {
        if (deathCompleted || deathStartedTime < 0f)
            return;

        float expectedLength = deathClip != null ? deathClip.length : 0f;
        float fallbackAt = expectedLength + Mathf.Max(0.1f, deathCompletionFallbackPadding);
        if (Time.time - deathStartedTime < fallbackAt)
            return;

        Debug.LogWarning(
            $"[StoneGolem] OnDeathFinished did not arrive within {fallbackAt:0.00}s; completing defeat via safety fallback.",
            this);
        CompleteDeath(false);
    }

    private void CompleteDeath(bool animationEventReceived)
    {
        if (deathCompleted)
            return;

        deathCompleted = true;
        deathAnimationEventReceived = animationEventReceived;
        ResolveDefeatReceiver();
        defeatReceiver?.OnEnemyDefeated();

        Log(animationEventReceived
            ? "Death animation complete; Node 2 defeat receiver notified. Corpse preserved."
            : "Death fallback complete; Node 2 defeat receiver notified. Corpse preserved.");
    }

    private void ResolveDefeatReceiver()
    {
        if (defeatReceiver != null)
            return;

        GameObject node2 = GameObject.Find("Node 2");
        if (node2 != null)
            defeatReceiver = node2.GetComponent<StageNodeController>();
    }

    private void CancelPunchForHitReaction()
    {
        punchPhase = PunchPhase.None;
        punchImpactApplied = false;
        punchUsesWalkLocomotion = false;
        punchLayerTargetWeight = 0f;

        if (animator == null || punchLayer <= 0 || punchLayer >= animator.layerCount)
            return;

        animator.SetLayerWeight(punchLayer, 0f);
        if (animator.HasState(punchLayer, PunchEmptyHash))
            animator.Play(PunchEmptyHash, punchLayer, 0f);
    }

    public void HandleLowerBodyHitFinished() => hitReaction?.HandleLowerFinished();
    public void HandleUpperBodyHitFinished() => hitReaction?.HandleUpperFinished();
    public void HandleRightPunchImpact() => HandlePunchImpact(PunchPhase.Right);
    public void HandleLeftPunchImpact() => HandlePunchImpact(PunchPhase.Left);

    public void HandleRightPunchFinished()
    {
        if (punchPhase != PunchPhase.Right)
            return;

        if (debuffPunchPressure)
        {
            nextDebuffPunchPhase = PunchPhase.Left;
            if (IsSlamDebuffActive() && CanStartDebuffPunch())
                StartNextDebuffPunch();
            else
                StopDebuffPunchForChase();
            return;
        }

        if (CanChainLeftPunch())
            EnterLeftPunch();
        else
            EnterPunchRecovery();
    }

    public void HandleLeftPunchFinished()
    {
        if (punchPhase != PunchPhase.Left)
            return;

        if (debuffPunchPressure)
        {
            nextDebuffPunchPhase = PunchPhase.Right;
            if (IsSlamDebuffActive() && CanStartDebuffPunch())
                StartNextDebuffPunch();
            else
                StopDebuffPunchForChase();
            return;
        }

        EnterPunchRecovery();
    }

    public void HandleStopSettled()
    {
        if (battleStarted && (state == CombatState.Stop || state == CombatState.Recovery))
            stopSettled = true;
    }

    public void HandleGroundSlamTakeoff()
    {
        if (!battleStarted || state != CombatState.GroundSlam
            || enemyHealth == null || enemyHealth.IsDead)
            return;

        slamAirborne = true;
        Log("Ground Slam takeoff: BeingHit suppressed until landing impact.");
    }

    public void HandleGroundSlamImpact()
    {
        if (!battleStarted || state != CombatState.GroundSlam || impactApplied
            || enemyHealth == null || enemyHealth.IsDead || !agent.enabled || !agent.isOnNavMesh)
            return;
        slamAirborne = false;
        impactApplied = true;
        landingImpacts++;
        nextSlamTime = Time.time + cooldown;
        ResolvePlayer();
        if (playerHealth != null && playerHealth.currentHealth > 0
            && DistanceToPlayer() <= radius)
        {
            playerHealth.TakeDamage(damage);
            playerHits++;
            if (playerHealth.currentHealth > 0)
                ApplyGroundSlamCrowdControl();
        }
        Log($"Landing impact #{landingImpacts}; player hits={playerHits}.");
    }

    public void HandleGroundSlamFinished()
    {
        if (!battleStarted || state != CombatState.GroundSlam)
            return;
        slamAirborne = false;
        if (!impactApplied)
        {
            Debug.LogError("[StoneGolem] Slam finished without its landing event; no fallback damage.", this);
            nextSlamTime = Time.time + cooldown;
        }
        EnterRecovery();
    }

    private void EnterWalk()
    {
        RestoreNormalWalkAgentSettings();
        ResetPunchLayer();
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
        ResetPunchLayer();
        StopAgent();
        state = CombatState.GroundSlam;
        enteredStateTime = Time.time;
        impactApplied = false;
        slamAirborne = false;
        slamsStarted++;
        // Facing is committed here. Update never rotates during this attack.
        animator.CrossFadeInFixedTime(SlamHash, blendSeconds, 0, 0f);
        Log($"Ground Slam #{slamsStarted}; waiting for authored landing.");
    }

    private void EnterRecovery()
    {
        slamAirborne = false;
        if (!debuffPunchPressure)
            ResetPunchLayer();
        StopAgent();
        state = CombatState.Recovery;
        enteredStateTime = Time.time;
        stopSettled = false;
        animator.CrossFadeInFixedTime(StopHash, blendSeconds, 0, 0f);
        Log("Recovery / Stop.");
    }

    private bool PunchSetupReady()
    {
        return animator != null && punchLayer > 0 && rightPunchClip != null && leftPunchClip != null
            && animator.HasState(punchLayer, PunchEmptyHash)
            && animator.HasState(punchLayer, RightPunchHash)
            && animator.HasState(punchLayer, LeftPunchHash);
    }

    private void EnterRightPunch()
    {
        if (!PunchSetupReady())
            return;
        punchUsesWalkLocomotion = !debuffPunchPressure && state == CombatState.HeavyWalk;
        if (!punchUsesWalkLocomotion)
            StopAgent();
        punchPhase = PunchPhase.Right;
        punchEnteredTime = Time.time;
        punchImpactApplied = false;
        punchCombosStarted++;
        punchLayerTargetWeight = 1f;
        animator.CrossFadeInFixedTime(RightPunchHash, punchBlendSeconds, punchLayer, 0f);
        Log($"Mutant Punch combo #{punchCombosStarted}: Right committed.");
    }

    private void EnterLeftPunch()
    {
        if (debuffPunchPressure)
            punchUsesWalkLocomotion = false;
        if (!punchUsesWalkLocomotion)
            StopAgent();
        punchPhase = PunchPhase.Left;
        punchEnteredTime = Time.time;
        punchImpactApplied = false;
        punchLayerTargetWeight = 1f;
        animator.CrossFadeInFixedTime(LeftPunchHash, punchBlendSeconds, punchLayer, 0f);
        Log("Mutant Punch chain: Left committed.");
    }

    private void UpdatePunch()
    {
        if (punchUsesWalkLocomotion)
        {
            FacePlayer();
            UpdateWalkLocomotion();
        }
        else
        {
            StopAgent();
        }

        if (punchPhase == PunchPhase.Recovery)
        {
            if (Time.time - punchEnteredTime >= punchRecoverySeconds)
                FinishPunchCombo();
            return;
        }

        AnimationClip clip = punchPhase == PunchPhase.Right ? rightPunchClip : leftPunchClip;
        float maximum = (clip != null ? clip.length : 0f) + punchWatchdogPadding;
        if (clip == null || Time.time - punchEnteredTime > maximum)
        {
            Debug.LogWarning("[StoneGolem] Mutant Punch finish event timed out; returning safely to Stop.", this);
            EnterPunchRecovery();
        }
    }

    private void HandlePunchImpact(PunchPhase expected)
    {
        if (punchPhase != expected || punchImpactApplied || enemyHealth == null || enemyHealth.IsDead)
            return;
        punchImpactApplied = true;
        punchImpacts++;
        ResolvePlayer();
        if (playerHealth == null || playerHealth.currentHealth <= 0)
            return;
        if (DistanceToPlayer() > punchRange || !IsFacingPlayer(punchFacingTolerance))
            return;
        playerHealth.TakeDamage(punchDamage);
        punchPlayerHits++;
        Log($"Mutant Punch impact #{punchImpacts}; player hits={punchPlayerHits}.");
    }

    private bool CanChainLeftPunch()
    {
        ResolvePlayer();
        return playerHealth != null && playerHealth.currentHealth > 0
            && DistanceToPlayer() <= punchRange
            && IsFacingPlayer(punchFacingTolerance);
    }

    private void EnterPunchRecovery()
    {
        punchPhase = PunchPhase.Recovery;
        punchEnteredTime = Time.time;
        punchImpactApplied = false;
        nextPunchTime = Time.time + punchComboCooldown;
        if (animator != null && punchLayer > 0)
        {
            punchLayerTargetWeight = 0f;
            if (animator.HasState(punchLayer, PunchEmptyHash))
                animator.CrossFadeInFixedTime(PunchEmptyHash, punchBlendSeconds, punchLayer, 0f);
        }
    }

    private void FinishPunchCombo()
    {
        punchPhase = PunchPhase.None;
        punchImpactApplied = false;
        if (animator != null && punchLayer > 0)
        {
            punchLayerTargetWeight = 0f;
            if (animator.HasState(punchLayer, PunchEmptyHash))
                animator.CrossFadeInFixedTime(PunchEmptyHash, punchBlendSeconds, punchLayer, 0f);
        }
        punchUsesWalkLocomotion = false;
        debuffPunchPressure = false;
        Log("Mutant Punch combo recovery complete.");
    }

    private void ResetPunchLayer()
    {
        punchPhase = PunchPhase.None;
        punchImpactApplied = false;
        punchUsesWalkLocomotion = false;
        debuffPunchPressure = false;
        punchLayerTargetWeight = 0f;
        if (animator == null || punchLayer <= 0 || punchLayer >= animator.layerCount)
            return;
        animator.SetLayerWeight(punchLayer, 0f);
        if (animator.HasState(punchLayer, PunchEmptyHash))
            animator.Play(PunchEmptyHash, punchLayer, 0f);
    }

    private void UpdateWalkLocomotion()
    {
        if (Time.time >= nextRepathTime)
        {
            nextRepathTime = Time.time + repathInterval;
            EnemyNavMovement.TrySetDestination(agent, playerHealth.transform.position);
        }
        animator.SetFloat(WalkRateHash,
            Mathf.Clamp(agent.velocity.magnitude / walkClipWorldSpeed, 0f, 1.4f));
    }

    private void UpdatePunchLayerBlend()
    {
        if (animator == null || punchLayer <= 0 || punchLayer >= animator.layerCount)
            return;

        if (punchBlendSeconds <= 0f)
        {
            animator.SetLayerWeight(punchLayer, punchLayerTargetWeight);
            return;
        }

        float current = animator.GetLayerWeight(punchLayer);
        float step = Time.deltaTime / punchBlendSeconds;
        animator.SetLayerWeight(punchLayer,
            Mathf.MoveTowards(current, punchLayerTargetWeight, step));
    }

    private void ApplyGroundSlamCrowdControl()
    {
        ResolvePlayer();
        if (playerMovement == null || playerHealth == null || playerHealth.currentHealth <= 0)
            return;

        activeSlamDebuff = playerMovement.GetComponent<StoneGolemSlamDebuff>();
        if (activeSlamDebuff == null)
            activeSlamDebuff = playerMovement.gameObject.AddComponent<StoneGolemSlamDebuff>();

        activeSlamDebuff.Begin(
            playerMovement,
            playerHealth,
            groundSlamStunDuration,
            groundSlamSlowMultiplier,
            groundSlamSlowDuration);

        if (activeSlamDebuff.IsActive && PunchSetupReady())
            BeginDebuffPunchPressure();
    }

    private void BeginDebuffPunchPressure()
    {
        debuffPunchPressure = true;
        punchUsesWalkLocomotion = false;
        nextDebuffPunchPhase = PunchPhase.Left;
        if (CanStartDebuffPunch())
            StartNextDebuffPunch();
        Log("Ground Slam punish window started: chase into Punch Range, then alternate L/R while crowd control is active.");
    }

    private bool IsSlamDebuffActive()
        => activeSlamDebuff != null && activeSlamDebuff.IsActive;

    private void UpdateDebuffPunchPressure()
    {
        if (!IsSlamDebuffActive())
        {
            EndDebuffPunchPressure();
            return;
        }

        ResolvePlayer();
        if (playerHealth == null || playerHealth.currentHealth <= 0)
        {
            EndDebuffPunchPressure();
            return;
        }

        if (state == CombatState.GroundSlam)
        {
            if (punchPhase == PunchPhase.None && CanStartDebuffPunch())
                StartNextDebuffPunch();

            UpdateDebuffPunchWatchdog();

            if (Time.time - enteredStateTime > animationWatchdogSeconds)
            {
                Debug.LogError("[StoneGolem] Ground Slam finish event missing during punish window; recovering without synthetic damage.", this);
                nextSlamTime = Time.time + cooldown;
                EnterRecovery();
            }
            return;
        }

        if (state == CombatState.Recovery || state == CombatState.Stop)
        {
            StopAgent();
            FacePlayer();

            if (punchPhase == PunchPhase.None && CanStartDebuffPunch())
            {
                StartNextDebuffPunch();
                return;
            }

            if (punchPhase == PunchPhase.None)
            {
                // The Slam punish window starts during the stun itself. If Van
                // landed outside Punch Range, do not wait for the stun to end,
                // the slow phase to begin, or the normal recovery timer. Start
                // the special 60% Heavy Walk chase immediately after the Slam
                // animation has handed control back to Recovery.
                EnterDebuffWalk();
                return;
            }

            if (punchPhase != PunchPhase.None)
                UpdateDebuffPunchWatchdog();
            return;
        }

        if (state != CombatState.HeavyWalk)
            EnterDebuffWalk();

        FacePlayer();

        if (punchPhase != PunchPhase.None)
        {
            StopAgent();
            UpdateDebuffPunchWatchdog();
            return;
        }

        if (CanStartDebuffPunch())
        {
            StartNextDebuffPunch();
            return;
        }

        ApplyDebuffChaseLocomotionSettings();
        UpdateWalkLocomotion();
    }

    private void UpdateDebuffPunchWatchdog()
    {
        if (punchPhase == PunchPhase.None)
            return;

        if (punchPhase == PunchPhase.Recovery)
        {
            StopDebuffPunchForChase();
            return;
        }

        AnimationClip clip = punchPhase == PunchPhase.Right ? rightPunchClip : leftPunchClip;
        float maximum = (clip != null ? clip.length : 0f) + punchWatchdogPadding;
        if (clip == null || Time.time - punchEnteredTime > maximum)
        {
            Debug.LogWarning("[StoneGolem] Debuff punch finish event timed out; resuming range-based chase.", this);
            nextDebuffPunchPhase = punchPhase == PunchPhase.Right ? PunchPhase.Left : PunchPhase.Right;
            StopDebuffPunchForChase();
        }
    }

    private bool CanStartDebuffPunch()
    {
        return PunchSetupReady()
            && DistanceToPlayer() <= punchRange
            && IsFacingPlayer(punchFacingTolerance);
    }

    private void StartNextDebuffPunch()
    {
        if (!CanStartDebuffPunch())
            return;

        StopAgent();

        if (nextDebuffPunchPhase == PunchPhase.Right)
        {
            EnterRightPunch();
            nextDebuffPunchPhase = PunchPhase.Left;
        }
        else
        {
            EnterLeftPunch();
            nextDebuffPunchPhase = PunchPhase.Right;
        }
    }

    private void StopDebuffPunchForChase()
    {
        punchPhase = PunchPhase.None;
        punchImpactApplied = false;
        punchUsesWalkLocomotion = false;
        if (animator != null && punchLayer > 0)
        {
            punchLayerTargetWeight = 0f;
            if (animator.HasState(punchLayer, PunchEmptyHash))
                animator.CrossFadeInFixedTime(PunchEmptyHash, punchBlendSeconds, punchLayer, 0f);
        }
    }

    private void EnterDebuffWalk()
    {
        StopDebuffPunchForChase();
        ApplyDebuffChaseLocomotionSettings();
        state = CombatState.HeavyWalk;
        enteredStateTime = Time.time;
        nextRepathTime = Time.time;
        animator.SetFloat(WalkRateHash, 0f);
        animator.CrossFadeInFixedTime(WalkHash, blendSeconds, 0, 0f);
        if (agent.enabled && agent.isOnNavMesh)
            agent.isStopped = false;
        Log($"Debuff pressure chase: Heavy Walk at x{debuffChaseSpeedMultiplier:0.00} speed toward Punch Range.");
    }

    private void EndDebuffPunchPressure()
    {
        debuffPunchPressure = false;
        StopDebuffPunchForChase();
        RestoreNormalWalkAgentSettings();
        EnterWalk();
        Log("Ground Slam crowd control ended; returning to normal chase/Slam loop.");
    }

    private void ApplyDebuffChaseLocomotionSettings()
    {
        if (agent == null)
            return;

        float chaseSpeed = Mathf.Max(0.05f, walkSpeed * debuffChaseSpeedMultiplier);
        EnemyNavMovement.ApplySpeed(agent, chaseSpeed, accelerationTime);

        // Punch Range is measured using body-surface distance, so during this
        // special chase the NavMeshAgent must not stop early based on the much
        // larger Ground Slam activation range.
        agent.stoppingDistance = 0.05f;
        debuffChaseLocomotionApplied = true;
    }

    private void RestoreNormalWalkAgentSettings()
    {
        if (agent == null)
            return;

        EnemyNavMovement.ApplySpeed(agent, walkSpeed, accelerationTime);
        agent.stoppingDistance = Mathf.Max(0.1f, attackRange - 0.2f);
        debuffChaseLocomotionApplied = false;
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
        {
            playerBody = playerHealth.GetComponent<CharacterController>()
                ?? playerHealth.GetComponent<Collider>()
                ?? playerHealth.GetComponentInChildren<Collider>();
        }
        if (playerHealth != null && playerMovement == null)
        {
            playerMovement = playerHealth.GetComponent<MovementofPlayer>()
                ?? playerHealth.GetComponentInChildren<MovementofPlayer>()
                ?? playerHealth.GetComponentInParent<MovementofPlayer>();
        }
    }

    private float DistanceToPlayer()
    {
        if (playerHealth == null)
            return float.PositiveInfinity;

        // Melee ranges are measured as the horizontal free space between the
        // Golem's body and Van's body. Both actors intentionally run at large
        // Stage 3 scales, so centre-to-centre checks encourage bogus 40-50m
        // compensation values and make the Golem attack before approaching.
        if (combatCollider != null && combatCollider.enabled && playerBody != null && playerBody.enabled)
        {
            Vector3 playerPoint = playerBody.ClosestPoint(combatCollider.bounds.center);
            Vector3 golemPoint = combatCollider.ClosestPoint(playerPoint);
            playerPoint = playerBody.ClosestPoint(golemPoint);
            Vector3 gap = playerPoint - golemPoint;
            gap.y = 0f;
            return gap.magnitude;
        }

        Vector3 delta = playerHealth.transform.position - transform.position;
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
        => IsFacingPlayer(attackFacingTolerance);

    private bool IsFacingPlayer(float tolerance)
    {
        Vector3 delta = playerHealth.transform.position - transform.position;
        delta.y = 0f;
        return delta.sqrMagnitude < 0.0001f
            || Vector3.Angle(transform.forward, delta) <= tolerance;
    }

    private void OnDisable()
    {
        hitReaction?.ResetReaction();
        ResetPunchLayer();
        resumeWalkAfterHit = false;
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
