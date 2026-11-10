using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class CursedJesterController : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public NavMeshAgent agent;
    public Transform player;

    [Header("Movement")]
    public float walkSpeed = 3.0f;
    [SerializeField, Min(0.05f)] private float accelerationTime = 0.5f;
    public float rotationSpeed = 360f;
    [Tooltip("The Jester advances into closer combat instead of camping at the outer edge of its long-range Taunt/Confetti abilities.")]
    [SerializeField, Range(0.25f, 0.9f)] private float rangedApproachFraction = 0.45f;

    [Header("Disconnected NavMesh Recovery")]
    [Tooltip("Normal NavMesh path retries. If the authored NavMesh is disconnected, the Jester walks over validated ground instead of freezing.")]
    [SerializeField, Min(0.1f)] private float navigationRetrySeconds = 0.4f;
    [SerializeField, Min(0.2f)] private float groundSteeringProbeRadius = 1.4f;
    [SerializeField, Min(0.1f)] private float groundSteeringMaxStepUp = 1.5f;
    [SerializeField, Min(0.1f)] private float groundSteeringMaxDrop = 2.5f;

    [Header("Basic Attack")]
    public float basicAttackRange = 2.5f;
    public int basicAttackDamage = 7;

    [Header("Confetti Blast")]
    public float confettiBlastRange = 8f;
    public float confettiBlastCooldown = 4f;
    public GameObject confettiPelletPrefab;
    public Transform confettiSpawnPoint;
    public int confettiPelletCount = 6;
    public float confettiSpreadAngle = 14f;
    public float confettiPelletSpeed = 20f;
    public float confettiPelletDamage = 2f;

    [Header("Taunt")]
    public float tauntRange = 10f;
    public float tauntCooldown = 7f;
    public float tauntSlowMultiplier = 0.6f;
    public float tauntSlowDuration = 2f;
    public MonoBehaviour tauntDebuffReceiver;

    [Header("Recovery")]
    public float recoverTime = 0.6f;

    [Header("Attack State Recovery")]
    [Tooltip("Recover if a late animation finish event is skipped by an Animator transition. Longer than the Jester's authored Taunt, Confetti and Basic Attack clips.")]
    [SerializeField, Min(3f)] private float attackAnimationWatchdogSeconds = 4f;
    [SerializeField, Min(2f)] private float hitAnimationWatchdogSeconds = 3f;

    [Header("Death Callback")]
    public MonoBehaviour deathReceiver;
    public string deathMessage = "OnCursedJesterDefeated";

    [Header("Potion Drop")]
    public bool dropPotionOnDeath = true;
    public string potionDropLogSource = "CursedJester";
    [Range(0f, 1f)] public float potionDropChance = 1f;
    public int potionDropMinCount = 1;
    public int potionDropMaxCount = 1;
    public int potionDropAmountPerDrop = 1;
    public bool canDropHealthPotion = true;
    public bool canDropPurifyPotion = true;
    public bool canDropPowerUpPotion = true;

    [Header("Debug Gizmos")]
    public bool showRangeGizmos = true;
    public Color basicAttackGizmoColor = new Color(1f, 0.2f, 0.2f, 0.35f);
    public Color confettiBlastGizmoColor = new Color(1f, 0.6f, 0.1f, 0.25f);
    public Color tauntGizmoColor = new Color(0.5f, 0.9f, 1f, 0.2f);
    public bool showConfettiConeGizmo = true;
    public int confettiConeSegments = 12;

    private bool battleStarted = false;
    private bool isAttacking = false;
    private bool isRecovering = false;
    private bool isBeingHit = false;
    private bool isDead = false;
    private bool potionDropResolved = false;
    private float confettiBlastCooldownTimer = 0f;
    private float tauntCooldownTimer = 0f;

    private Coroutine recoverCoroutine;
    private float attackStartedAt;
    private float hitReactionStartedAt;
    private NavMeshPath chasePath;
    private readonly RaycastHit[] steeringContacts = new RaycastHit[32];
    private readonly RaycastHit[] groundContacts = new RaycastHit[32];
    private bool groundSteeringActive;
    private float nextNavigationRetry;
    private float groundSteeringSpeed;
    private int cachedAgentAreaMask = NavMesh.AllAreas;
    private int cachedAgentType;

    private void Awake()
    {
        // NavMeshPath is a native Unity object; never create it in a
        // MonoBehaviour field initializer or constructor.
        chasePath = new NavMeshPath();
        agent ??= GetComponent<NavMeshAgent>();
        animator ??= GetComponent<Animator>();

        if (player == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }
    }

    private void Start()
    {
        if (agent != null)
        {
            EnemyNavMovement.ApplySpeed(agent, walkSpeed, accelerationTime);
            cachedAgentAreaMask = agent.areaMask;
            cachedAgentType = agent.agentTypeID;
            agent.updateRotation = false;
            agent.updatePosition = true;
            agent.stoppingDistance = 0f;
            agent.autoBraking = true;
            agent.isStopped = true;
        }

        if (animator != null)
            animator.applyRootMotion = false;
    }

    public void StartBattle()
    {
        if (battleStarted || isDead) return;

        battleStarted = true;
        isAttacking = false;
        isRecovering = false;
        isBeingHit = false;
        potionDropResolved = false;

        if (recoverCoroutine != null)
        {
            StopCoroutine(recoverCoroutine);
            recoverCoroutine = null;
        }

        if (agent != null)
        {
            EnemyNavMovement.ApplySpeed(agent, walkSpeed, accelerationTime);
            cachedAgentAreaMask = agent.areaMask;
            cachedAgentType = agent.agentTypeID;
            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.ResetPath();
            }
        }
        nextNavigationRetry = 0f;

        Debug.Log("[CursedJester] Battle started.");
    }

    private void Update()
    {
        if (isDead)
        {
            StopMovement();
            return;
        }

        if (confettiBlastCooldownTimer > 0f)
            confettiBlastCooldownTimer -= Time.deltaTime;

        if (tauntCooldownTimer > 0f)
            tauntCooldownTimer -= Time.deltaTime;

        // The authored attack events sit near each clip's last frames. If an
        // Animator transition interrupts one, isAttacking / isBeingHit can
        // otherwise stay true forever even though the Animator is back on
        // its looping Walk state. Do not softlock the boss's movement.
        if (battleStarted && isAttacking &&
            Time.time - attackStartedAt >= attackAnimationWatchdogSeconds)
        {
            isAttacking = false;
            animator?.ResetTrigger("BasicAttack");
            animator?.ResetTrigger("ConfettiBlast");
            animator?.ResetTrigger("Taunt");
            Debug.LogWarning("[CursedJester] Missing attack-finished event; recovering AI movement.", this);
            EnterRecover();
        }
        if (battleStarted && isBeingHit &&
            Time.time - hitReactionStartedAt >= hitAnimationWatchdogSeconds)
        {
            isBeingHit = false;
            Debug.LogWarning("[CursedJester] Missing BeingHit-finished event; recovering AI movement.", this);
            EnterRecover();
        }

        if (!battleStarted || player == null)
        {
            SetAnimatorSpeed(0f);
            return;
        }

        if (isAttacking || isRecovering || isBeingHit)
        {
            StopMovement();
            FaceTarget(player.position);
            return;
        }

        DecideAction();
    }

    private void DecideAction()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= basicAttackRange)
        {
            TriggerBasicAttack();
            return;
        }

        // At the Stage 1 world scale the Jester's Taunt/Confetti ranges are
        // ~44/42 units. If both are always prioritized at maximum range, the
        // Jester spends most of the fight standing still casting repeatedly.
        // Chase first until a meaningful midrange engagement distance.
        float engageRange = Mathf.Max(
            basicAttackRange * 1.4f,
            Mathf.Min(tauntRange, confettiBlastRange) * rangedApproachFraction);
        if (distance > engageRange)
        {
            ChasePlayer();
            return;
        }

        if (distance <= tauntRange && tauntCooldownTimer <= 0f)
        {
            TriggerTaunt();
            return;
        }

        if (distance <= confettiBlastRange && confettiBlastCooldownTimer <= 0f)
        {
            TriggerConfettiBlast();
            return;
        }

        ChasePlayer();
    }

    private void ChasePlayer()
    {
        if (player == null)
            return;
        // Recover existing live actors after an Editor script reload: their
        // Awake may have failed in the previous revision's NavMeshPath field
        // initializer, leaving the native path null.
        if (chasePath == null)
            chasePath = new NavMeshPath();

        // The Stage 1 Jester can stand on a small NavMesh island: the agent
        // reports isOnNavMesh=true but CalculatePath to the player is PARTIAL.
        // Repeating SetDestination every Update leaves it motionless forever.
        // Verify a COMPLETE route only at a measured cadence.
        if (Time.time >= nextNavigationRetry)
        {
            nextNavigationRetry = Time.time + navigationRetrySeconds;
            if (groundSteeringActive)
                TryRestoreNavMeshChase();
            else if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                bool complete = agent.CalculatePath(player.position, chasePath) &&
                                chasePath.status == NavMeshPathStatus.PathComplete;
                if (complete)
                {
                    agent.isStopped = false;
                    agent.SetPath(chasePath);
                }
                else
                    BeginGroundSteering();
            }
            else
                BeginGroundSteering();
        }

        if (groundSteeringActive)
            StepGroundSteering();
        else if (agent != null && agent.enabled && agent.isOnNavMesh)
            SetAnimatorSpeed(agent.velocity.magnitude);

        FaceTarget(player.position);
    }

    private void BeginGroundSteering()
    {
        if (groundSteeringActive)
            return;

        groundSteeringActive = true;
        groundSteeringSpeed = 0f;
        if (agent != null && agent.enabled)
        {
            cachedAgentAreaMask = agent.areaMask;
            cachedAgentType = agent.agentTypeID;
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
            // The agent would constrain Move to its disconnected NavMesh
            // island. Disable only during the ground-validated fallback.
            agent.enabled = false;
        }
        Debug.LogWarning(
            "[CursedJester] No complete NavMesh route. " +
            "Switching to grounded obstacle-aware pursuit until a route reconnects.", this);
    }

    private void TryRestoreNavMeshChase()
    {
        if (chasePath == null)
            chasePath = new NavMeshPath();
        NavMeshQueryFilter filter = new NavMeshQueryFilter
        {
            agentTypeID = cachedAgentType,
            areaMask = cachedAgentAreaMask
        };
        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit start, 1.5f, filter) ||
            !NavMesh.SamplePosition(player.position, out NavMeshHit goal, 4f, filter) ||
            !NavMesh.CalculatePath(start.position, goal.position, filter, chasePath) ||
            chasePath.status != NavMeshPathStatus.PathComplete ||
            agent == null)
            return;

        agent.enabled = true;
        if (!agent.Warp(start.position))
        {
            agent.enabled = false;
            return;
        }

        EnemyNavMovement.ApplySpeed(agent, walkSpeed, accelerationTime);
        agent.isStopped = false;
        agent.SetPath(chasePath);
        groundSteeringActive = false;
        groundSteeringSpeed = 0f;
        Debug.Log("[CursedJester] Complete NavMesh path recovered. Normal chase resumed.", this);
    }

    private void StepGroundSteering()
    {
        StepGroundSteering(Time.deltaTime);
    }

    // Explicit timestep also allows isolated movement tests while a stage
    // completion/vocabulary overlay has intentionally paused global time.
    private void StepGroundSteering(float deltaSeconds)
    {
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.01f)
        {
            SetAnimatorSpeed(0f);
            return;
        }

        float acceleration = Mathf.Max(0.1f, walkSpeed / accelerationTime);
        groundSteeringSpeed = Mathf.MoveTowards(
            groundSteeringSpeed, walkSpeed, acceleration * deltaSeconds);
        Vector3 direction = toPlayer.normalized;
        float stepDistance = Mathf.Min(
            groundSteeringSpeed * deltaSeconds, toPlayer.magnitude);
        if (stepDistance <= 0f)
            return;

        // The collider on this enemy is a trigger, so directly changing
        // transform.position would otherwise ghost through scenery.
        // Physically sweep ahead at body height, ignoring the floor, self,
        // and the intended player target.
        float probeRadius = groundSteeringProbeRadius;
        Vector3 origin = transform.position +
                         Vector3.up * Mathf.Max(2.5f, probeRadius + 1.5f);
        int hitCount = Physics.SphereCastNonAlloc(
            origin, probeRadius, direction, steeringContacts,
            stepDistance + 0.2f, Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = steeringContacts[i];
            if (hit.collider == null || hit.normal.y >= 0.55f ||
                hit.collider.transform.IsChildOf(transform) ||
                hit.collider.transform.IsChildOf(player))
                continue;
            stepDistance = Mathf.Min(stepDistance,
                Mathf.Max(0f, hit.distance - 0.15f));
        }

        Vector3 next = transform.position + direction * stepDistance;
        Vector3 rayOrigin = next + Vector3.up * (groundSteeringMaxStepUp + 3f);
        int groundHitCount = Physics.RaycastNonAlloc(
            rayOrigin, Vector3.down, groundContacts,
            groundSteeringMaxStepUp + groundSteeringMaxDrop + 4f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        float floor = float.NegativeInfinity;
        for (int i = 0; i < groundHitCount; i++)
        {
            RaycastHit hit = groundContacts[i];
            if (hit.collider == null || hit.normal.y < 0.55f ||
                hit.collider.transform.IsChildOf(transform) ||
                hit.collider.transform.IsChildOf(player) ||
                hit.point.y > transform.position.y + groundSteeringMaxStepUp ||
                hit.point.y < transform.position.y - groundSteeringMaxDrop)
                continue;
            floor = Mathf.Max(floor, hit.point.y);
        }

        if (float.IsNegativeInfinity(floor) || stepDistance <= 0.001f)
        {
            groundSteeringSpeed = 0f;
            SetAnimatorSpeed(0f);
            return; // No safe floor or blocked by solid geometry.
        }

        next.y = floor;
        transform.position = next;
        SetAnimatorSpeed(groundSteeringSpeed);
    }

    private void TriggerBasicAttack()
    {
        if (isAttacking || isRecovering || isBeingHit || isDead) return;

        isAttacking = true;
        attackStartedAt = Time.time;

        StopMovement();
        FaceTarget(player.position);

        if (animator != null)
        {
            animator.SetTrigger("BasicAttack");
            Debug.Log("[CursedJester] BasicAttack triggered.");
        }
        else
        {
            isAttacking = false;
        }
    }

    public void OnBasicAttackHit()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= basicAttackRange + 0.5f)
        {
            DealDamageToPlayer(basicAttackDamage);
            Debug.Log("[CursedJester] BasicAttack hit player.");
        }
        else
        {
            Debug.Log("[CursedJester] BasicAttack missed.");
        }
    }

    public void OnBasicAttackFinished()
    {
        if (!isAttacking || isDead) return;
        isAttacking = false;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= basicAttackRange)
        {
            TriggerBasicAttack();
            return;
        }

        EnterRecover();
    }

    private void EnterRecover()
    {
        if (isDead) return;

        isRecovering = true;
        StopMovement();

        if (recoverCoroutine != null)
            StopCoroutine(recoverCoroutine);

        recoverCoroutine = StartCoroutine(RecoverRoutine());
    }

    private void TriggerConfettiBlast()
    {
        if (isAttacking || isRecovering || isBeingHit || isDead) return;

        isAttacking = true;
        attackStartedAt = Time.time;
        confettiBlastCooldownTimer = confettiBlastCooldown;

        StopMovement();
        FaceTarget(player.position);

        if (animator != null)
        {
            animator.SetTrigger("ConfettiBlast");
            Debug.Log("[CursedJester] ConfettiBlast triggered.");
        }
        else
        {
            isAttacking = false;
            EnterRecover();
        }
    }

    // Animation Event
    public void OnConfettiBlastCast()
    {
        SpawnConfettiPellets();
    }

    // Animation Event
    public void OnConfettiBlastFinished()
    {
        if (!isAttacking || isDead) return;
        Debug.Log("[CursedJester] ConfettiBlast finished event fired.");
        isAttacking = false;
        EnterRecover();
    }

    private void TriggerTaunt()
    {
        if (isAttacking || isRecovering || isBeingHit || isDead) return;

        isAttacking = true;
        attackStartedAt = Time.time;
        tauntCooldownTimer = tauntCooldown;

        StopMovement();
        FaceTarget(player.position);

        if (animator != null)
        {
            animator.SetTrigger("Taunt");
            Debug.Log("[CursedJester] Taunt triggered.");
        }
        else
        {
            isAttacking = false;
            EnterRecover();
        }
    }

    // Animation Event
    public void OnTauntEffect()
    {
        if (player == null) return;

        bool applied = TryApplyTauntSlowDebuff();
        if (applied)
            Debug.Log($"[CursedJester] Taunt effect fired (slow x{tauntSlowMultiplier} for {tauntSlowDuration}s).");
        else
            Debug.Log("[CursedJester] Taunt fired, but no ApplySpeedDebuff(float,float) receiver found.");
    }

    // Animation Event
    public void OnTauntFinished()
    {
        if (!isAttacking || isDead) return;
        Debug.Log("[CursedJester] Taunt finished.");
        isAttacking = false;
        EnterRecover();
    }

    private IEnumerator RecoverRoutine()
    {
        yield return new WaitForSeconds(recoverTime);

        isRecovering = false;
        recoverCoroutine = null;
    }

    private void SpawnConfettiPellets()
    {
        if (confettiPelletPrefab == null)
        {
            Debug.LogWarning("[CursedJester] ConfettiPellet prefab is missing.");
            return;
        }

        if (player == null)
        {
            Debug.LogWarning("[CursedJester] Player missing for ConfettiBlast.");
            return;
        }

        Transform spawnRef = confettiSpawnPoint != null ? confettiSpawnPoint : transform;
        Vector3 spawnPos = spawnRef.position;
        Vector3 baseDirection = (player.position - spawnPos).normalized;

        for (int i = 0; i < confettiPelletCount; i++)
        {
            float yaw = Random.Range(-confettiSpreadAngle, confettiSpreadAngle);
            float pitch = Random.Range(-confettiSpreadAngle * 0.5f, confettiSpreadAngle * 0.5f);
            Quaternion spreadRotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 shotDirection = spreadRotation * baseDirection;

            GameObject pelletObj = Instantiate(confettiPelletPrefab, spawnPos, Quaternion.LookRotation(shotDirection));
            ConfettiPellet pellet = pelletObj.GetComponent<ConfettiPellet>();
            if (pellet != null)
            {
                pellet.Init(shotDirection, confettiPelletSpeed, confettiPelletDamage);
            }
            else
            {
                Debug.LogWarning("[CursedJester] Spawned pellet prefab has no ConfettiPellet component.");
            }
        }

        Debug.Log($"[CursedJester] ConfettiBlast spawned {confettiPelletCount} pellets.");
    }

    private bool TryApplyTauntSlowDebuff()
    {
        if (TryInvokeDebuffOnReceiver(tauntDebuffReceiver))
            return true;

        MonoBehaviour[] receivers = player.root.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (MonoBehaviour receiver in receivers)
        {
            if (TryInvokeDebuffOnReceiver(receiver))
                return true;
        }

        return false;
    }

    private bool TryInvokeDebuffOnReceiver(MonoBehaviour receiver)
    {
        if (receiver == null) return false;

        MethodInfo method = receiver.GetType().GetMethod(
            "ApplySpeedDebuff",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(float), typeof(float) },
            null
        );

        if (method == null) return false;

        method.Invoke(receiver, new object[] { tauntSlowMultiplier, tauntSlowDuration });
        return true;
    }

    private void DealDamageToPlayer(float damage)
    {
        var damageable = player.root.GetComponentInChildren<IDamageable>();

        if (damageable != null)
            damageable.ApplyDamage(damage);
        else
            player.SendMessage("ApplyDamage", damage, SendMessageOptions.DontRequireReceiver);
    }

    public void TriggerBeingHit()
    {
        if (!battleStarted || isDead || isBeingHit) return;

        Debug.Log("[CursedJester] BeingHit triggered.");

        isBeingHit = true;
        hitReactionStartedAt = Time.time;
        isAttacking = false;

        if (recoverCoroutine != null)
        {
            StopCoroutine(recoverCoroutine);
            recoverCoroutine = null;
        }

        isRecovering = false;

        StopMovement();

        if (animator != null)
        {
            animator.ResetTrigger("BasicAttack");
            animator.ResetTrigger("ConfettiBlast");
            animator.ResetTrigger("Taunt");
            animator.SetTrigger("BeingHit");
        }
    }

    // Animation Event
    public void OnBeingHitFinished()
    {
        if (!isBeingHit || isDead) return;
        Debug.Log("[CursedJester] BeingHit finished.");
        isBeingHit = false;
        EnterRecover();
    }

    public void TriggerDeath()
    {
        if (isDead) return;

        Debug.Log("[CursedJester] Death triggered.");

        isDead = true;
        battleStarted = false;
        isAttacking = false;
        isRecovering = false;
        isBeingHit = false;

        if (recoverCoroutine != null)
        {
            StopCoroutine(recoverCoroutine);
            recoverCoroutine = null;
        }

        StopMovement();

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.enabled = false;
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        if (animator != null)
        {
            animator.ResetTrigger("BasicAttack");
            animator.ResetTrigger("ConfettiBlast");
            animator.ResetTrigger("Taunt");
            animator.ResetTrigger("BeingHit");
            animator.SetTrigger("Dead");
        }
        else
        {
            Debug.LogWarning("[CursedJester] Animator missing. Cannot play Dead animation.");
        }
    }

    // Animation Event
    public void OnDeathFinished()
    {
        Debug.Log("[CursedJester] Death finished. Waiting for Node3 wiring.");

        TryDropPotionReward();

        if (deathReceiver != null)
        {
            deathReceiver.SendMessage(deathMessage, SendMessageOptions.DontRequireReceiver);
        }

        gameObject.SetActive(false);
    }

    private void TryDropPotionReward()
    {
        if (!dropPotionOnDeath || potionDropResolved)
            return;

        potionDropResolved = true;

        PotionDropper.TryDropRandomPotion(
            potionDropLogSource,
            potionDropChance,
            potionDropMinCount,
            potionDropMaxCount,
            potionDropAmountPerDrop,
            canDropHealthPotion,
            canDropPurifyPotion,
            canDropPowerUpPotion
        );
    }

    private void StopMovement()
    {
        groundSteeringSpeed = 0f;
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        SetAnimatorSpeed(0f);
    }

    private void FaceTarget(Vector3 targetPos)
    {
        Vector3 direction = targetPos - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private void SetAnimatorSpeed(float speedValue)
    {
        if (animator != null)
            animator.SetFloat("Speed", speedValue);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showRangeGizmos) return;

        Vector3 center = transform.position;
        center.y += 0.1f;

        Gizmos.color = basicAttackGizmoColor;
        Gizmos.DrawSphere(center, basicAttackRange);

        Gizmos.color = new Color(basicAttackGizmoColor.r, basicAttackGizmoColor.g, basicAttackGizmoColor.b, 1f);
        Gizmos.DrawWireSphere(center, basicAttackRange);

        Gizmos.color = confettiBlastGizmoColor;
        Gizmos.DrawSphere(center, confettiBlastRange);

        Gizmos.color = new Color(confettiBlastGizmoColor.r, confettiBlastGizmoColor.g, confettiBlastGizmoColor.b, 1f);
        Gizmos.DrawWireSphere(center, confettiBlastRange);

        Gizmos.color = tauntGizmoColor;
        Gizmos.DrawSphere(center, tauntRange);

        Gizmos.color = new Color(tauntGizmoColor.r, tauntGizmoColor.g, tauntGizmoColor.b, 1f);
        Gizmos.DrawWireSphere(center, tauntRange);

        if (showConfettiConeGizmo)
            DrawConfettiConeGizmo(center);
    }

    private void DrawConfettiConeGizmo(Vector3 center)
    {
        int segments = Mathf.Max(4, confettiConeSegments);
        float halfVertical = confettiSpreadAngle * 0.5f;

        Vector3 baseDirection = transform.forward;
        Transform originRef = confettiSpawnPoint != null ? confettiSpawnPoint : transform;
        Vector3 origin = originRef.position;

        Gizmos.color = new Color(confettiBlastGizmoColor.r, confettiBlastGizmoColor.g, confettiBlastGizmoColor.b, 1f);
        Gizmos.DrawLine(origin, origin + baseDirection.normalized * confettiBlastRange);

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float yaw = Mathf.Lerp(-confettiSpreadAngle, confettiSpreadAngle, t);

            Vector3 topDir = Quaternion.Euler(halfVertical, yaw, 0f) * baseDirection;
            Vector3 bottomDir = Quaternion.Euler(-halfVertical, yaw, 0f) * baseDirection;

            Gizmos.DrawLine(origin, origin + topDir.normalized * confettiBlastRange);
            Gizmos.DrawLine(origin, origin + bottomDir.normalized * confettiBlastRange);
        }
    }
}
