using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

/// <summary>
/// Drowned Captain foundation controller.
///
/// Current approved layer:
/// - encounter activation through StartBattle()
/// - slow NavMesh walk approach
/// - configurable stopping range
/// - player-facing rotation
/// - hit reaction and death/progression callback
/// - committed basic Anchor Slam with Animation Event impact timing
/// - committed Spin Sweep into Pursuit Strike combo
///
/// Stage 2 debuffs and boss progression remain intentionally deferred.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class DrownedCaptainController : MonoBehaviour
{
    private enum AttackType
    {
        None,
        AnchorSlam,
        AnchorSpinSweep,
        AnchorPursuitStrike
    }
    [Header("Core References")]
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform actorRoot;
    [SerializeField] private Transform player;

    [Header("World Tile Spawn Area")]
    [Tooltip("Ship-deck center used by the shared TileSpawner. Falls back to DC_ArenaCenter when unassigned.")]
    [SerializeField] private Transform tileSpawnArenaCenter;
    [SerializeField, Min(0.5f)] private float tileSpawnArenaRadius = 45f;
    [Tooltip("Only colliders on these layers may receive replacement tiles. Assign the ship deck layer; do not include Ground for Node 4.")]
    [SerializeField] private LayerMask tileSpawnSurfaceLayers;
    [SerializeField] private Transform tileSpawnExclusionCenter;
    [SerializeField, Min(0f)] private float tileSpawnExclusionRadius;

    [Header("Slow Boss Movement")]
    [Tooltip("Maximum walking speed. Keep this slower than regular Stage 2 enemies.")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 16f;
    [Tooltip("Time required to accelerate to the configured walking speed.")]
    [SerializeField, Min(0.05f)] private float accelerationTime = 0.9f;
    [Tooltip("Distance at which the Captain stops approaching and holds position.")]
    [SerializeField, Min(0f)] private float stoppingRange = 18f;
    [SerializeField, Min(1f)] private float rotationSpeed = 180f;
    [SerializeField, Min(0.1f)] private float navMeshSearchRadius = 8f;

    [Header("Anchor Spin Sweep")]
    [Tooltip("The Captain begins the Spin Sweep inside this distance.")]
    [SerializeField, Min(0.1f)] private float spinSweepRange = 24f;
    [Tooltip("Shared trigger BoxCollider on the animated anchor. Must start disabled.")]
    [FormerlySerializedAs("anchorSpinSweepHitbox")]
    [SerializeField] private Collider anchorWeaponHitbox;
    [Tooltip("Damage dealt to the player when Spin Sweep connects.")]
    [SerializeField, Min(0f)] private float spinSweepDamage = 32f;
    [Tooltip("Force applied as knockback when Spin Sweep connects.")]
    [SerializeField, Min(0f)] private float spinSweepKnockbackForce = 18f;
    [Tooltip("Duration of stun applied when Spin Sweep connects (currently unused; placeholder for future integration).")]
    [SerializeField, Min(0f)] private float spinSweepStunDuration = 0.6f;
    [Tooltip("Cooldown applied after the Spin Sweep combo ends or misses.")]
    [SerializeField, Min(0.1f)] private float spinSweepCooldown = 8f;
    [Tooltip("Safety timeout only. The authored finish Animation Event should normally end Spin Sweep first.")]
    [SerializeField, Min(0.05f)] private float spinSweepFinishFallbackTime = 4f;

    [Header("Anchor Pursuit Strike")]
    [Tooltip("Damage dealt to the player when Pursuit Strike connects. Pure damage, no knockback or stun.")]
    [SerializeField, Min(0f)] private float anchorPursuitStrikeDamage = 35f;
    [Tooltip("Short locked delay between a connected Spin Sweep and Pursuit Strike.")]
    [SerializeField, Min(0f)] private float pursuitStrikeDelay = 0.03f;
    [Tooltip("Animator state that contains the Pursuit Strike clip. The current controller state is AnchorPursuitStrike.")]
    [SerializeField] private string pursuitStrikeStateName = "AnchorPursuitStrike";
    [Tooltip("Safety timeout only. The authored finish Animation Event should normally end Pursuit Strike first.")]
    [SerializeField, Min(0.05f)] private float pursuitStrikeFinishFallbackTime = 4f;

    [Header("Basic Anchor Slam")]
    [Tooltip("The Captain begins the committed slam inside this distance.")]
    [SerializeField, Min(0.1f)] private float anchorSlamRange = 22f;
    [Tooltip("Damage dealt when the player is inside the impact radius on the impact frame.")]
    [SerializeField, Min(0f)] private float anchorSlamDamage = 28f;
    [Tooltip("Radius of the frontal anchor impact.")]
    [SerializeField, Min(0.1f)] private float anchorSlamImpactRadius = 8f;
    [Tooltip("Moves the impact center forward from the Captain's actor root.")]
    [SerializeField, Min(0f)] private float anchorSlamForwardOffset = 5f;
    [Tooltip("Moves the impact center sideways relative to the Captain. Positive values move right; negative values move left.")]
    [SerializeField] private float anchorSlamSideOffset;
    [SerializeField, Min(0f)] private float anchorSlamCooldown = 3.5f;
    [Tooltip("Minimum time reserved for the authored impact/finish Animation Events. Keep this later than the final event in the clip.")]
    [SerializeField, Min(0.05f)] private float anchorSlamEventWindow = 5.75f;
    [Tooltip("Safety fallback for ending the committed state only. It never applies damage.")]
    [SerializeField, Min(0.05f)] private float anchorSlamFinishFallbackTime = 2.1f;

    [Header("Reaction Timing Fallbacks")]
    [SerializeField, Min(0.05f)] private float hitFinishFallback = 0.9f;
    [SerializeField, Min(0.05f)] private float deathFinishFallback = 2.5f;

    [Header("Encounter Completion")]
    [SerializeField] private MonoBehaviour deathReceiver;
    [SerializeField] private string deathMessage = "OnEnemyDefeated";
    [SerializeField] private GameObject rootToDisableOnDeath;

    [Header("Animator Parameters")]
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private string anchorSlamTrigger = "AnchorSlam";
    [SerializeField] private string anchorSpinSweepTrigger = "Anchor Spin Sweep";
    [SerializeField] private string idleStateName = "Idle";
    [SerializeField] private string beingHitTrigger = "BeingHit";
    [SerializeField] private string deathTrigger = "Dead";
    [Tooltip("Animator state that must immediately override every active behaviour when the Captain dies.")]
    [SerializeField] private string deathStateName = "Death";

    [Header("Debug")]
    [SerializeField] private bool enableDebugStartBattleKey;
    [SerializeField] private KeyCode debugStartBattleKey = KeyCode.T;
    [SerializeField] private bool debugLogs = true;
    [SerializeField] private bool showGizmos = true;

    private bool battleStarted;
    private bool isAttacking;
    private AttackType currentAttack = AttackType.None;
    private bool slamImpactResolved;
    private bool spinSweepHitResolved;
    private bool pursuitStrikeHitResolved;
    private bool isBeingHit;
    private bool isDead;
    private bool deathFinished;
    private bool loggedMissingNavMesh;
    private Coroutine hitRoutine;
    private Coroutine deathRoutine;
    private Coroutine slamRoutine;
    private Coroutine comboRoutine;
    private float nextAnchorSlamTime;
    private float nextSpinSweepTime;

    public bool BattleStarted => battleStarted;
    public bool IsDead => isDead;

    private Transform ActorTransform => actorRoot != null ? actorRoot : transform;
    private Vector3 ActorPosition => ActorTransform.position;

    private void Awake()
    {
        ResolveReferences();
        ConfigureAgent();
        if (anchorWeaponHitbox != null)
            anchorWeaponHitbox.enabled = false;
    }

    private void Start()
    {
        StopMovement();
        SetAnimatorSpeed(0f);
        if (anchorWeaponHitbox != null)
            anchorWeaponHitbox.enabled = false;
    }

    private void OnEnable()
    {
        ResolveReferences();
        ConfigureAgent();
        StopMovement();
        SetAnimatorSpeed(0f);
        if (anchorWeaponHitbox != null)
            anchorWeaponHitbox.enabled = false;
    }

    private void OnDisable()
    {
        StopRoutine(ref hitRoutine);
        StopRoutine(ref deathRoutine);
        StopRoutine(ref slamRoutine);
        StopRoutine(ref comboRoutine);
        if (anchorWeaponHitbox != null)
            anchorWeaponHitbox.enabled = false;
    }

    private void Update()
    {
        if (enableDebugStartBattleKey && !battleStarted && !isDead && Input.GetKeyDown(debugStartBattleKey))
            StartBattle();

        if (isDead)
        {
            StopMovement();
            SetAnimatorSpeed(0f);
            return;
        }

        if (isAttacking)
        {
            StopMovement();
            SetAnimatorSpeed(0f);
            FacePlayer();
            return;
        }

        if (!battleStarted || !ResolvePlayer())
        {
            StopMovement();
            SetAnimatorSpeed(0f);
            return;
        }

        if (isBeingHit)
        {
            StopMovement();
            SetAnimatorSpeed(0f);
            FacePlayer();
            return;
        }

        UpdateApproach();
    }

    public void StartBattle()
    {
        if (battleStarted || isDead)
            return;

        ResolveReferences();
        ConfigureAgent();
        ConfigureTileSpawnArea();

        battleStarted = true;
        isAttacking = false;
        currentAttack = AttackType.None;
        slamImpactResolved = false;
        spinSweepHitResolved = false;
        pursuitStrikeHitResolved = false;
        isBeingHit = false;
        deathFinished = false;
        nextAnchorSlamTime = Time.time;
        if (anchorWeaponHitbox != null)
            anchorWeaponHitbox.enabled = false;

        if (!TryPrepareAgentForMovement())
        {
            StopMovement();
            SetAnimatorSpeed(0f);
        }

        Log("Battle started. Slow approach layer active.");
    }

    private void ConfigureTileSpawnArea()
    {
        if (TileSpawner.Instance == null)
        {
            Debug.LogError("[DrownedCaptain] Cannot configure replacement-tile spawning because TileSpawner.Instance is missing.", this);
            return;
        }

        if (tileSpawnArenaCenter == null)
        {
            GameObject fallbackCenter = GameObject.Find("DC_ArenaCenter");
            if (fallbackCenter != null)
                tileSpawnArenaCenter = fallbackCenter.transform;
        }

        if (tileSpawnArenaCenter == null)
        {
            Debug.LogError("[DrownedCaptain] Tile Spawn Arena Center is unassigned and DC_ArenaCenter was not found.", this);
            return;
        }

        if (tileSpawnSurfaceLayers.value == 0)
        {
            Debug.LogError(
                "[DrownedCaptain] Tile Spawn Surface Layers is empty. Assign the ship deck's dedicated layer so replacement tiles cannot fall through to Ground.",
                this);
            return;
        }

        TileSpawner.Instance.ConfigureArenaBounds(
            tileSpawnArenaCenter,
            tileSpawnArenaRadius,
            tileSpawnExclusionCenter,
            tileSpawnExclusionRadius,
            tileSpawnSurfaceLayers
        );

        Log(
            $"Replacement tiles configured for ship deck around {tileSpawnArenaCenter.name} " +
            $"(radius {tileSpawnArenaRadius:0.##}, surface mask {tileSpawnSurfaceLayers.value}).");
    }

    private void UpdateApproach()
    {
        FacePlayer();

        float distance = FlatDistance(ActorPosition, player.position);
        if (distance <= Mathf.Max(0.1f, spinSweepRange)
            && Time.time >= nextSpinSweepTime)
        {
            BeginAnchorSpinSweep();
            return;
        }

        if (distance <= Mathf.Max(0.1f, anchorSlamRange)
            && Time.time >= nextAnchorSlamTime)
        {
            BeginAnchorSlam();
            return;
        }

        if (distance <= Mathf.Max(0f, stoppingRange))
        {
            StopMovement();
            SetAnimatorSpeed(0f);
            return;
        }

        if (!TryPrepareAgentForMovement())
        {
            StopMovement();
            SetAnimatorSpeed(0f);
            return;
        }

        EnemyNavMovement.ApplySpeed(agent, moveSpeed, accelerationTime);
        agent.stoppingDistance = Mathf.Max(0f, stoppingRange);
        agent.isStopped = false;
        if (!EnemyNavMovement.TrySetDestination(agent, player.position))
        {
            StopMovement();
            SetAnimatorSpeed(0f);
            return;
        }
        SetAnimatorSpeed(0.5f);
    }

    private void BeginAnchorSlam()
    {
        if (!battleStarted || isDead || isBeingHit || isAttacking || player == null)
            return;

        isAttacking = true;
        slamImpactResolved = false;
        StopMovement();
        SetAnimatorSpeed(0f);
        FacePlayerImmediately();
        StopRoutine(ref slamRoutine);

        if (!TrySetTrigger(anchorSlamTrigger))
        {
            Log($"Cannot start Anchor Slam because Animator trigger '{anchorSlamTrigger}' is missing.");
            isAttacking = false;
            currentAttack = AttackType.None;
            nextAnchorSlamTime = Time.time + Mathf.Max(0.1f, anchorSlamCooldown);
            return;
        }

        currentAttack = AttackType.AnchorSlam;
        slamRoutine = StartCoroutine(AnchorSlamFallbackRoutine());
        Log("Anchor Slam committed. Movement and other actions are locked until finish.");
    }

    private void BeginAnchorSpinSweep()
    {
        if (!battleStarted || isDead || isBeingHit || isAttacking || player == null)
            return;

        isAttacking = true;
        currentAttack = AttackType.AnchorSpinSweep;
        spinSweepHitResolved = false;
        StopMovement();
        SetAnimatorSpeed(0f);
        FacePlayerImmediately();
        StopRoutine(ref comboRoutine);

        if (!TrySetTrigger(anchorSpinSweepTrigger))
        {
            Log($"Cannot start Anchor Spin Sweep because Animator trigger '{anchorSpinSweepTrigger}' is missing.");
            FinishSpinSweepCombo("trigger missing");
            return;
        }

        comboRoutine = StartCoroutine(SpinSweepFallbackRoutine());
        Log("Anchor Spin Sweep committed. Waiting for the authored hitbox Animation Events.");
    }

    public void TriggerBeingHit()
    {
        if (!battleStarted || isDead || isBeingHit || isAttacking)
            return;

        isBeingHit = true;
        StopMovement();
        SetAnimatorSpeed(0f);
        StopRoutine(ref hitRoutine);

        if (TrySetTrigger(beingHitTrigger))
            hitRoutine = StartCoroutine(HitFallbackRoutine());
        else
            OnBeingHitFinished();
    }

    public void OnBeingHitFinished()
    {
        if (!isBeingHit || isDead)
            return;

        StopRoutine(ref hitRoutine);
        isBeingHit = false;
        Log("BeingHit finished. Slow approach resumed.");
    }

    public void TriggerDeath()
    {
        if (isDead)
            return;

        isDead = true;
        battleStarted = false;
        isAttacking = false;
        currentAttack = AttackType.None;
        slamImpactResolved = false;
        spinSweepHitResolved = false;
        pursuitStrikeHitResolved = false;
        isBeingHit = false;
        deathFinished = false;
        StopRoutine(ref hitRoutine);
        StopRoutine(ref deathRoutine);
        StopRoutine(ref slamRoutine);
        StopRoutine(ref comboRoutine);
        if (anchorWeaponHitbox != null)
            anchorWeaponHitbox.enabled = false;
        StopMovement();
        SetAnimatorSpeed(0f);

        if (ForceDeathAnimation())
            deathRoutine = StartCoroutine(DeathFallbackRoutine());
        else
            OnDeathFinished();

        Log("Death triggered.");
    }

    private bool ForceDeathAnimation()
    {
        if (animator == null)
            return false;

        // Clear every queued trigger so no attack or hit transition can win
        // after the terminal death state has been selected.
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger)
                animator.ResetTrigger(parameter.nameHash);
        }

        if (!string.IsNullOrWhiteSpace(deathStateName))
        {
            int stateHash = Animator.StringToHash(deathStateName);
            int fullPathHash = Animator.StringToHash($"Base Layer.{deathStateName}");

            if (animator.HasState(0, stateHash))
            {
                animator.Play(stateHash, 0, 0f);
                animator.Update(0f);
                return true;
            }

            if (animator.HasState(0, fullPathHash))
            {
                animator.Play(fullPathHash, 0, 0f);
                animator.Update(0f);
                return true;
            }

            Log($"Animator death state '{deathStateName}' is missing. Falling back to trigger '{deathTrigger}'.");
        }

        return TrySetTrigger(deathTrigger);
    }

    public void OnDeathFinished()
    {
        if (!isDead || deathFinished)
            return;

        deathFinished = true;
        StopRoutine(ref deathRoutine);

        if (deathReceiver != null && !string.IsNullOrWhiteSpace(deathMessage))
            deathReceiver.SendMessage(deathMessage, SendMessageOptions.DontRequireReceiver);

        GameObject target = rootToDisableOnDeath != null ? rootToDisableOnDeath : gameObject;
        target.SetActive(false);
        Log("Death finished and progression callback sent.");
    }

    // Animation Event: place on the exact ground/contact frame of Anchor Slam.
    public void OnAnchorSlamImpact()
    {
        ResolveAnchorSlamImpact();
    }

    // Animation Event: place on the final frame of Anchor Slam.
    public void OnAnchorSlamFinished()
    {
        FinishAnchorSlam(true);
    }

    // Animation Event: place on the frame when the hitbox should activate during Spin Sweep.
    public void OnAnchorSpinSweepHitboxEnabled()
    {
        if (!isAttacking || currentAttack != AttackType.AnchorSpinSweep)
            return;

        spinSweepHitResolved = false;
        if (anchorWeaponHitbox != null)
            anchorWeaponHitbox.enabled = true;
    }

    // Animation Event: place on the frame when the hitbox should deactivate during Spin Sweep.
    public void OnAnchorSpinSweepHitboxDisabled()
    {
        if (anchorWeaponHitbox != null)
            anchorWeaponHitbox.enabled = false;
    }

    // Animation Event: place on the final frame of Spin Sweep.
    public void OnAnchorSpinSweepFinished()
    {
        if (!isAttacking || currentAttack != AttackType.AnchorSpinSweep)
            return;

        if (anchorWeaponHitbox != null)
            anchorWeaponHitbox.enabled = false;

        StopRoutine(ref comboRoutine);

        if (!spinSweepHitResolved)
        {
            FinishSpinSweepCombo("sweep missed");
            return;
        }

        currentAttack = AttackType.None;
        comboRoutine = StartCoroutine(BeginPursuitStrikeAfterDelay());
        Log($"Anchor Spin Sweep connected. Pursuit Strike committed in {pursuitStrikeDelay:0.00}s.");
    }

    // Animation Event: place on the frame when the hitbox should activate during Pursuit Strike.
    public void OnAnchorPursuitStrikeHitboxEnabled()
    {
        if (!isAttacking || currentAttack != AttackType.AnchorPursuitStrike)
            return;

        pursuitStrikeHitResolved = false;
        if (anchorWeaponHitbox != null)
            anchorWeaponHitbox.enabled = true;
    }

    // Animation Event: place on the frame when the hitbox should deactivate during Pursuit Strike.
    public void OnAnchorPursuitStrikeHitboxDisabled()
    {
        if (anchorWeaponHitbox != null)
            anchorWeaponHitbox.enabled = false;
    }

    // Animation Event: place on the final frame of Pursuit Strike.
    public void OnAnchorPursuitStrikeFinished()
    {
        if (!isAttacking || currentAttack != AttackType.AnchorPursuitStrike)
            return;

        if (anchorWeaponHitbox != null)
            anchorWeaponHitbox.enabled = false;

        FinishSpinSweepCombo("pursuit finished");
    }

    private IEnumerator SpinSweepFallbackRoutine()
    {
        yield return new WaitForSeconds(Mathf.Max(0.05f, spinSweepFinishFallbackTime));
        comboRoutine = null;

        if (isAttacking && currentAttack == AttackType.AnchorSpinSweep)
        {
            if (spinSweepHitResolved)
            {
                Log("Spin Sweep finish Animation Event was not received, but the hit was confirmed. Continuing to Pursuit Strike through the safety fallback.");
                currentAttack = AttackType.None;
                comboRoutine = StartCoroutine(BeginPursuitStrikeAfterDelay());
            }
            else
            {
                Log("Spin Sweep finish Animation Event was not received and no hit was confirmed. Ending the combo without extra damage.");
                FinishSpinSweepCombo("sweep fallback timeout");
            }
        }
    }

    private IEnumerator BeginPursuitStrikeAfterDelay()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, pursuitStrikeDelay));
        comboRoutine = null;

        if (!battleStarted || isDead || !isAttacking || !spinSweepHitResolved)
        {
            FinishSpinSweepCombo("pursuit cancelled");
            yield break;
        }

        StopMovement();
        SetAnimatorSpeed(0f);
        FacePlayerImmediately();
        pursuitStrikeHitResolved = false;
        currentAttack = AttackType.AnchorPursuitStrike;

        if (!TryResolvePursuitState(out int pursuitStateHash, out string resolvedStateName))
        {
            Log($"Cannot start Anchor Pursuit Strike because neither the configured state '{pursuitStrikeStateName}' nor a compatible Pursuit state was found on Animator layer 0.");
            FinishSpinSweepCombo("pursuit state missing");
            yield break;
        }

        animator.CrossFadeInFixedTime(pursuitStateHash, 0.05f, 0, 0f);
        comboRoutine = StartCoroutine(PursuitStrikeFallbackRoutine());
        Log($"Anchor Pursuit Strike started through Animator state '{resolvedStateName}'.");
    }

    private bool TryResolvePursuitState(out int stateHash, out string resolvedStateName)
    {
        stateHash = 0;
        resolvedStateName = string.Empty;

        if (animator == null)
            return false;

        // Keep old scene data compatible: Unity may retain the former
        // "SWORD ANIM 3" serialized value even after the Animator state was
        // renamed to AnchorPursuitStrike.
        string[] candidates =
        {
            pursuitStrikeStateName,
            "AnchorPursuitStrike",
            "SWORD ANIM 3"
        };

        for (int i = 0; i < candidates.Length; i++)
        {
            string candidate = candidates[i];
            if (string.IsNullOrWhiteSpace(candidate))
                continue;

            int candidateHash = Animator.StringToHash(candidate);
            if (!animator.HasState(0, candidateHash))
                continue;

            stateHash = candidateHash;
            resolvedStateName = candidate;

            if (!string.Equals(candidate, pursuitStrikeStateName, System.StringComparison.Ordinal))
                Log($"Configured Pursuit state '{pursuitStrikeStateName}' was unavailable. Using compatible state '{candidate}'.");

            return true;
        }

        return false;
    }

    private IEnumerator PursuitStrikeFallbackRoutine()
    {
        yield return new WaitForSeconds(Mathf.Max(0.05f, pursuitStrikeFinishFallbackTime));
        comboRoutine = null;

        if (isAttacking && currentAttack == AttackType.AnchorPursuitStrike)
        {
            Log("Pursuit Strike finish Animation Event was not received. Ending the combo without extra damage.");
            FinishSpinSweepCombo("pursuit fallback timeout");
        }
    }

    private void FinishSpinSweepCombo(string reason)
    {
        if (anchorWeaponHitbox != null)
            anchorWeaponHitbox.enabled = false;

        StopRoutine(ref comboRoutine);
        isAttacking = false;
        currentAttack = AttackType.None;
        nextSpinSweepTime = Time.time + Mathf.Max(0.1f, spinSweepCooldown);

        if (animator != null && !string.IsNullOrWhiteSpace(idleStateName))
        {
            int idleStateHash = Animator.StringToHash(idleStateName);
            if (animator.HasState(0, idleStateHash))
                animator.CrossFadeInFixedTime(idleStateHash, 0.05f, 0, 0f);
        }

        Log($"Spin Sweep combo ended ({reason}). Cooldown: {Mathf.Max(0.1f, spinSweepCooldown):0.00}s.");
    }
    private IEnumerator AnchorSlamFallbackRoutine()
    {
        // Never let an old/short serialized fallback close the committed state
        // before the authored impact and finish Animation Events can arrive.
        float emergencyTimeout = Mathf.Max(
            0.05f,
            anchorSlamEventWindow,
            anchorSlamFinishFallbackTime);
        yield return new WaitForSeconds(emergencyTimeout);

        if (isAttacking)
        {
            if (!slamImpactResolved)
                Log($"Anchor Slam reached its {emergencyTimeout:0.00}s emergency timeout without receiving OnAnchorSlamImpact. No damage was applied.");

            FinishAnchorSlam(false);
        }

        slamRoutine = null;
    }

    private void ResolveAnchorSlamImpact()
    {
        if (!isAttacking || currentAttack != AttackType.AnchorSlam || slamImpactResolved)
            return;

        slamImpactResolved = true;
        if (!ResolvePlayer())
            return;

        Vector3 impactCenter = GetAnchorSlamImpactCenter();
        Vector3 playerPoint = player.position;
        CharacterController playerController = player.GetComponent<CharacterController>();
        if (playerController != null)
            playerPoint = playerController.bounds.center;

        float distance = Vector3.Distance(impactCenter, playerPoint);
        if (distance > Mathf.Max(0.1f, anchorSlamImpactRadius))
        {
            Log($"Anchor Slam missed. Distance: {distance:0.00}/{anchorSlamImpactRadius:0.00}.");
            return;
        }

        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>()
            ?? player.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null)
        {
            Log("Anchor Slam found the player in range, but no PlayerHealth component was found.");
            return;
        }

        playerHealth.TakeDamage(anchorSlamDamage);
        Log($"Anchor Slam hit for {anchorSlamDamage:0.##} damage.");
    }

    private void FinishAnchorSlam(bool stopFallbackRoutine)
    {
        if (!isAttacking || currentAttack != AttackType.AnchorSlam)
            return;

        if (!slamImpactResolved)
            Log("Anchor Slam ended before its impact resolved. Check the impact Animation Event.");

        isAttacking = false;
        currentAttack = AttackType.None;
        slamImpactResolved = true;
        nextAnchorSlamTime = Time.time + Mathf.Max(0.1f, anchorSlamCooldown);

        if (stopFallbackRoutine)
            StopRoutine(ref slamRoutine);

        Log($"Anchor Slam finished. Cooldown: {Mathf.Max(0.1f, anchorSlamCooldown):0.00}s.");
    }

    public void TryResolveAnchorWeaponHit(Collider other)
    {
        if (!battleStarted || isDead || !isAttacking)
            return;

        if (!ResolvePlayer() || other.transform.root != player.transform.root)
            return;

        switch (currentAttack)
        {
            case AttackType.AnchorSpinSweep:
                ResolveAnchorSpinSweepHit(other);
                break;

            case AttackType.AnchorPursuitStrike:
                ResolveAnchorPursuitStrikeHit(other);
                break;
        }
    }

    private void ResolveAnchorSpinSweepHit(Collider other)
    {
        if (currentAttack != AttackType.AnchorSpinSweep)
            return;

        if (spinSweepHitResolved)
            return;

        spinSweepHitResolved = true;

        if (anchorWeaponHitbox != null)
            anchorWeaponHitbox.enabled = false;

        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>()
            ?? player.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null)
        {
            Log("Spin Sweep found the player in range, but no PlayerHealth component was found.");
            return;
        }

        playerHealth.TakeDamage(spinSweepDamage);
        Log($"Anchor Spin Sweep hit for {spinSweepDamage:0.##} damage.");

        // Apply knockback using the existing player movement API.
        PlayerMovement playerMovement = player.GetComponent<PlayerMovement>()
            ?? player.GetComponentInParent<PlayerMovement>();
        if (playerMovement != null && spinSweepKnockbackForce > 0.01f)
        {
            Vector3 knockbackDirection = (player.position - ActorPosition).normalized;
            knockbackDirection.y = 0f;
            if (knockbackDirection.sqrMagnitude > 0.01f)
                playerMovement.ApplyKnockback(knockbackDirection.normalized, spinSweepKnockbackForce);
        }

        // TODO: Integrate stun duration once PlayerMovement.ApplyStun(duration) is implemented.
        // For now, the stun field is a serialized placeholder.
    }

    private void ResolveAnchorPursuitStrikeHit(Collider other)
    {
        if (currentAttack != AttackType.AnchorPursuitStrike)
            return;

        if (pursuitStrikeHitResolved)
            return;

        pursuitStrikeHitResolved = true;

        if (anchorWeaponHitbox != null)
            anchorWeaponHitbox.enabled = false;

        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>()
            ?? player.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null)
        {
            Log("Pursuit Strike found the player in range, but no PlayerHealth component was found.");
            return;
        }

        playerHealth.TakeDamage(anchorPursuitStrikeDamage);
        Log($"Anchor Pursuit Strike hit for {anchorPursuitStrikeDamage:0.##} damage.");

        // Pursuit Strike applies damage only. No knockback, no stun.
    }

    private IEnumerator HitFallbackRoutine()
    {
        yield return new WaitForSeconds(Mathf.Max(0.05f, hitFinishFallback));
        hitRoutine = null;
        OnBeingHitFinished();
    }

    private IEnumerator DeathFallbackRoutine()
    {
        yield return new WaitForSeconds(Mathf.Max(0.05f, deathFinishFallback));
        deathRoutine = null;
        OnDeathFinished();
    }

    private void ResolveReferences()
    {
        if (animator == null)
            animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();

        if (agent == null)
            agent = GetComponent<NavMeshAgent>() ?? GetComponentInParent<NavMeshAgent>();

        if (actorRoot == null)
            actorRoot = agent != null ? agent.transform : transform;

        if (rootToDisableOnDeath == null)
            rootToDisableOnDeath = ActorTransform.gameObject;

        if (animator != null)
            animator.applyRootMotion = false;

        ResolvePlayer();
    }

    private bool ResolvePlayer()
    {
        if (PersistentPlayerRoot.Instance != null)
        {
            player = PersistentPlayerRoot.Instance.transform;
            return true;
        }

        if (player != null)
            return true;

        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject == null)
            return false;

        player = playerObject.transform;
        return true;
    }

    private void ConfigureAgent()
    {
        if (agent == null)
            return;

        EnemyNavMovement.ApplySpeed(agent, moveSpeed, accelerationTime);
        agent.stoppingDistance = Mathf.Max(0f, stoppingRange);
        agent.updateRotation = false;
        agent.autoBraking = true;
    }

    private bool TryPrepareAgentForMovement()
    {
        if (agent == null || !agent.enabled)
        {
            LogMissingNavMesh("No enabled NavMeshAgent is assigned.");
            return false;
        }

        if (!agent.isOnNavMesh)
        {
            if (!NavMesh.SamplePosition(ActorPosition, out NavMeshHit hit, navMeshSearchRadius, NavMesh.AllAreas)
                || !agent.Warp(hit.position))
            {
                LogMissingNavMesh("NavMeshAgent is not on a baked NavMesh.");
                return false;
            }
        }

        loggedMissingNavMesh = false;
        return true;
    }

    private void StopMovement()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        agent.isStopped = true;
        agent.ResetPath();
    }

    private void FacePlayer()
    {
        if (player == null)
            return;

        Vector3 direction = player.position - ActorPosition;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        ActorTransform.rotation = Quaternion.RotateTowards(
            ActorTransform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private void FacePlayerImmediately()
    {
        if (player == null)
            return;

        Vector3 direction = player.position - ActorPosition;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
            ActorTransform.rotation = Quaternion.LookRotation(direction.normalized);
    }

    private Vector3 GetAnchorSlamImpactCenter()
    {
        return ActorPosition
            + ActorTransform.forward * Mathf.Max(0f, anchorSlamForwardOffset)
            + ActorTransform.right * anchorSlamSideOffset;
    }

    private bool TrySetTrigger(string parameterName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(parameterName))
            return false;

        int hash = Animator.StringToHash(parameterName);
        if (!HasAnimatorParameter(hash, AnimatorControllerParameterType.Trigger))
        {
            Log($"Animator trigger '{parameterName}' is missing.");
            return false;
        }

        animator.SetTrigger(hash);
        return true;
    }

    private void SetAnimatorSpeed(float value)
    {
        if (animator == null || string.IsNullOrWhiteSpace(speedParameter))
            return;

        int hash = Animator.StringToHash(speedParameter);
        if (HasAnimatorParameter(hash, AnimatorControllerParameterType.Float))
            animator.SetFloat(hash, value, 0.1f, Time.deltaTime);
    }

    private bool HasAnimatorParameter(int hash, AnimatorControllerParameterType type)
    {
        if (animator == null)
            return false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == hash && parameter.type == type)
                return true;
        }

        return false;
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void StopRoutine(ref Coroutine routine)
    {
        if (routine == null)
            return;

        StopCoroutine(routine);
        routine = null;
    }

    private void LogMissingNavMesh(string message)
    {
        if (loggedMissingNavMesh)
            return;

        loggedMissingNavMesh = true;
        Debug.LogWarning($"[DrownedCaptain] {message}", this);
    }

    private void Log(string message)
    {
        if (debugLogs)
            Debug.Log($"[DrownedCaptain] {message}", this);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos)
            return;

        Gizmos.color = new Color(0.1f, 0.7f, 1f, 0.85f);
        Gizmos.DrawWireSphere(ActorPosition, Mathf.Max(0f, stoppingRange));

        Gizmos.color = new Color(1f, 0.8f, 0f, 0.85f);
        Gizmos.DrawWireSphere(ActorPosition, Mathf.Max(0.1f, anchorSlamRange));

        Gizmos.color = new Color(0.8f, 0.2f, 0.8f, 0.85f);
        Gizmos.DrawWireSphere(ActorPosition, Mathf.Max(0.1f, spinSweepRange));

        Gizmos.color = new Color(1f, 0.35f, 0.1f, 0.85f);
        Gizmos.DrawWireSphere(GetAnchorSlamImpactCenter(), Mathf.Max(0.1f, anchorSlamImpactRadius));
    }
}
