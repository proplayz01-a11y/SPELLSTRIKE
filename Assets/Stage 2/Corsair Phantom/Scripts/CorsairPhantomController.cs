using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class CorsairPhantomController : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public NavMeshAgent agent;
    public Transform player;
    public Transform visualModel;
    public Transform phantomShotOrigin;
    public Transform piercingScreamOrigin;

    [Header("Movement")]
    public float glideSpeed = 3f;
    public float rotationSpeed = 360f;
    public float stoppingDistance = 1.8f;
    public float attackRange = 8f;
    public float navMeshSearchRadius = 4f;

    [Header("Ghost Glide")]
    public bool enableGhostGlide = true;
    public float ghostGlideCooldown = 6f;
    public float ghostGlideTriggerRange = 12f;
    public float ghostGlideSpeed = 18f;
    public float ghostGlidePassThroughDistance = 2.5f;
    public float ghostGlideMaxDuration = 1.2f;
    public float ghostGlideRecoveryTime = 0.8f;
    public float ghostGlideStopDistance = 0.25f;
    public float ghostGlideHitRadius = 1.15f;
    public int ghostGlideDamage = 4;
    public float ghostGlideStunDuration = 2f;

    [Header("Phantom Shot")]
    public float phantomShotRange = 10f;
    public float phantomShotAimAngle = 22f;
    public int phantomShotDamage = 6;
    public float phantomShotFireDelay = 0.45f;
    public float phantomShotFallbackFinishTime = 1.15f;
    public float phantomShotCooldown = 1.8f;

    [Header("Phantom Shot Projectile")]
    public PhantomShotProjectile phantomShotProjectilePrefab;
    public float phantomShotProjectileSpeed = 12f;
    public float phantomShotProjectileLifetime = 3f;
    public float phantomShotProjectileHitRadius = 0.45f;
    public bool createProjectileIfPrefabMissing = true;
    public bool useInstantHitFallbackIfProjectileMissing = true;

    [Header("Phantom Shot Charge VFX")]
    public PhantomShotChargeVFX phantomShotChargePrefab;
    public bool createPhantomShotChargeIfPrefabMissing = true;

    [Header("Piercing Scream")]
    public bool enablePiercingScream = true;
    public float piercingScreamCooldown = 8f;
    public float piercingScreamTriggerRange = 7f;
    public float piercingScreamRange = 7f;
    [Range(1f, 180f)] public float piercingScreamAngle = 45f;
    public float piercingScreamCastDelay = 0.6f;
    public float piercingScreamFallbackFinishTime = 1.25f;
    [SerializeField, Min(0)] private int piercingScreamDamage = 5;

    [Header("Piercing Scream VFX")]
    public PiercingScreamWaveVFX piercingScreamWavePrefab;
    public bool createPiercingScreamWaveIfPrefabMissing = true;

    [Header("Piercing Scream Layered VFX")]
    public PiercingScreamVFX piercingScreamVFXPrefab;
    public bool createPiercingScreamVFXIfPrefabMissing = true;

    [Header("Piercing Scream Tile Cracking")]
    [SerializeField] private TileDebuffManager tileDebuffManager;
    [SerializeField] private bool crackTilesOnPiercingScreamHit = true;
    [SerializeField, Min(0)] private int piercingScreamCrackedTileCount = 1;
    [SerializeField] private int piercingScreamCrackCounter = 2;

    [Header("Death Callback")]
    public MonoBehaviour deathReceiver;
    public string deathMessage = "OnEnemyDefeated";

    [Header("Hit / Death Timing")]
    public float hitFallbackFinishTime = 0.7f;
    public float deathFallbackFinishTime = 2f;

    [Header("Animator Parameters")]
    public string speedParameter = "Speed";
    public string attackTrigger = "Attack";
    public string castTrigger = "Cast";
    public string hitTrigger = "BeingHit";
    public string deathTrigger = "Die";

    [Header("Debug Testing")]
    public bool enableDebugStartBattleKey = true;
    public KeyCode debugStartBattleKey = KeyCode.T;

    [Header("Debug")]
    public bool debugLogs = true;

    [Header("Debug Gizmos")]
    public bool showRangeGizmos = true;
    public bool showNavMeshSearchGizmo = true;
    public Color attackRangeGizmoColor = new Color(0.5f, 0.9f, 1f, 0.28f);
    public Color ghostGlideRangeGizmoColor = new Color(0.55f, 0.35f, 1f, 0.2f);
    public Color ghostGlideHitGizmoColor = new Color(0.75f, 0.4f, 1f, 0.75f);
    public Color piercingScreamTriggerGizmoColor = new Color(0.2f, 0.95f, 1f, 0.22f);
    public Color piercingScreamConeGizmoColor = new Color(0.65f, 0.95f, 1f, 0.75f);
    public Color stoppingDistanceGizmoColor = new Color(0.25f, 0.6f, 1f, 0.22f);
    public Color navMeshSearchGizmoColor = new Color(0.1f, 1f, 0.8f, 0.2f);

    private bool battleStarted;
    private bool isAttacking;
    private bool isRecovering;
    private bool isGhostGliding;
    private bool isCastingPiercingScream;
    private bool isBeingHit;
    private bool isDead;
    private bool deathFinished;
    private bool phantomShotResolved;
    private bool piercingScreamResolved;
    private bool loggedMissingNavMesh;
    private float nextGhostGlideTime;
    private float nextPiercingScreamTime;
    private Vector3 piercingScreamTargetPosition;
    private Vector3 piercingScreamTargetDirection;
    private Vector3 piercingScreamVisualDirection;

    private Coroutine phantomShotRoutine;
    private Coroutine recoverRoutine;
    private Coroutine ghostGlideRoutine;
    private Coroutine piercingScreamRoutine;
    private Coroutine playerMovementStunRoutine;
    private Coroutine hitFallbackRoutine;
    private Coroutine deathFallbackRoutine;

    private PhantomShotChargeVFX activePhantomShotChargeVFX;
    private PiercingScreamVFX activePiercingScreamVFX;

    private void Awake()
    {
        agent ??= GetComponent<NavMeshAgent>();
        ResolveVisualReferences();
        ResolvePlayer();
    }

    private void Start()
    {
        ConfigureIdleState();
    }

    private void OnEnable()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        ResolveVisualReferences();
        StopMovement();
        SetAnimatorSpeed(0f);
    }

    private void Update()
    {
        HandleDebugInput();

        if (isDead)
        {
            StopMovement();
            return;
        }

        if (!battleStarted || isBeingHit)
        {
            StopMovement();
            SetAnimatorSpeed(0f);
            return;
        }

        if (isGhostGliding)
            return;

        if (isAttacking || isRecovering || isCastingPiercingScream)
        {
            StopMovement();

            if (player != null)
                FaceTarget(player.position);

            SetAnimatorSpeed(0f);
            return;
        }

        DecideAction();
    }

    private void HandleDebugInput()
    {
        if (!enableDebugStartBattleKey || isDead)
            return;

        if (Input.GetKeyDown(debugStartBattleKey))
            StartBattle();
    }

    public void StartBattle()
    {
        if (battleStarted || isDead)
            return;

        battleStarted = true;
        isAttacking = false;
        isRecovering = false;
        isGhostGliding = false;
        isCastingPiercingScream = false;
        isBeingHit = false;
        deathFinished = false;
        phantomShotResolved = false;
        piercingScreamResolved = false;
        nextGhostGlideTime = Time.time + ghostGlideCooldown;
        nextPiercingScreamTime = Time.time + piercingScreamCooldown;

        ResolvePlayer();
        ConfigureAgent();
        StopPhantomShotRoutine();
        StopRecoverRoutine();
        StopGhostGlideRoutine();
        StopPiercingScreamRoutine();
        StopHitFallbackRoutine();
        StopDeathFallbackRoutine();

        if (TryPrepareAgentForMovement())
        {
            agent.isStopped = false;
            agent.ResetPath();
        }

        SetAnimatorSpeed(0f);

        Log("[CorsairPhantom] Battle started.");
    }

    public void TriggerBeingHit()
    {
        if (!battleStarted || isDead || isBeingHit)
            return;

        isBeingHit = true;
        isAttacking = false;
        isRecovering = false;
        isGhostGliding = false;
        isCastingPiercingScream = false;
        phantomShotResolved = true;
        piercingScreamResolved = true;

        StopMovement();
        StopPhantomShotRoutine();
        StopRecoverRoutine();
        StopGhostGlideRoutine();
        StopPiercingScreamRoutine();
        StopHitFallbackRoutine();
        ResetAnimatorTrigger(attackTrigger);
        ResetAnimatorTrigger(castTrigger);

        if (TrySetAnimatorTrigger(hitTrigger, false))
            hitFallbackRoutine = StartCoroutine(HitFallbackRoutine());
        else
            OnBeingHitFinished();
    }

    // Animation Event: place this on the final frame of the BeingHit animation.
    public void OnBeingHitFinished()
    {
        if (!isBeingHit)
            return;

        StopHitFallbackRoutine();
        isBeingHit = false;
        SetAnimatorSpeed(0f);
        Log("[CorsairPhantom] Hit finished.");
    }

    public void TriggerDeath()
    {
        if (isDead)
            return;

        isDead = true;
        battleStarted = false;
        isAttacking = false;
        isRecovering = false;
        isGhostGliding = false;
        isCastingPiercingScream = false;
        isBeingHit = false;
        deathFinished = false;
        phantomShotResolved = true;
        piercingScreamResolved = true;

        StopMovement();
        StopPhantomShotRoutine();
        StopRecoverRoutine();
        StopGhostGlideRoutine();
        StopPiercingScreamRoutine();
        StopHitFallbackRoutine();
        StopDeathFallbackRoutine();
        SetAnimatorSpeed(0f);
        ResetActionTriggers();

        Log("[CorsairPhantom] Death triggered.");

        if (TrySetAnimatorTrigger(deathTrigger, false))
            deathFallbackRoutine = StartCoroutine(DeathFallbackRoutine());
        else
            OnDeathFinished();
    }

    // Animation Event: place this on the final frame of the Die animation.
    public void OnDeathFinished()
    {
        if (deathFinished)
            return;

        deathFinished = true;
        StopDeathFallbackRoutine();
        Log("[CorsairPhantom] Death finished.");

        if (deathReceiver != null)
            deathReceiver.SendMessage(deathMessage, SendMessageOptions.DontRequireReceiver);

        gameObject.SetActive(false);
    }

    private IEnumerator HitFallbackRoutine()
    {
        yield return new WaitForSeconds(Mathf.Max(0.01f, hitFallbackFinishTime));
        hitFallbackRoutine = null;

        if (isBeingHit && !isDead)
            OnBeingHitFinished();
    }

    private IEnumerator DeathFallbackRoutine()
    {
        yield return new WaitForSeconds(Mathf.Max(0.01f, deathFallbackFinishTime));
        deathFallbackRoutine = null;

        if (isDead && !deathFinished)
            OnDeathFinished();
    }

    private void ConfigureIdleState()
    {
        ResolveVisualReferences();
        ConfigureAgent();
        StopMovement();
        SetAnimatorSpeed(0f);
    }

    private void ResolveVisualReferences()
    {
        if (visualModel == null)
        {
            Animator childAnimator = GetComponentInChildren<Animator>();
            if (childAnimator != null && childAnimator.transform != transform)
                visualModel = childAnimator.transform;
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();

            if (animator == null && visualModel != null)
                animator = visualModel.GetComponent<Animator>();

            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }

        if (animator != null)
            animator.applyRootMotion = false;
    }

    private void ConfigureAgent()
    {
        if (agent == null)
            return;

        agent.speed = glideSpeed;
        agent.stoppingDistance = stoppingDistance;
        agent.updateRotation = false;
    }

    private void ResolvePlayer()
    {
        if (player != null)
            return;

        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
            player = playerObject.transform;
    }

    private void StopMovement()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        agent.isStopped = true;
        agent.ResetPath();
    }

    private void DecideAction()
    {
        if (player == null)
            ResolvePlayer();

        if (player == null)
        {
            StopMovement();
            SetAnimatorSpeed(0f);
            return;
        }

        float distance = GetFlatDistanceToPlayer();
        if (ShouldStartGhostGlide(distance))
        {
            StartGhostGlide();
            return;
        }

        if (ShouldStartPiercingScream(distance))
        {
            StartPiercingScream();
            return;
        }

        if (distance <= attackRange)
        {
            StartPhantomShot();
            return;
        }

        ChasePlayer();
    }

    private void ChasePlayer()
    {
        if (player == null)
        {
            ResolvePlayer();
            if (player == null)
            {
                StopMovement();
                SetAnimatorSpeed(0f);
                return;
            }
        }

        if (!TryPrepareAgentForMovement())
        {
            FaceTarget(player.position);
            SetAnimatorSpeed(0f);
            return;
        }

        float distance = GetFlatDistanceToPlayer();
        if (distance <= attackRange)
        {
            StopMovement();
            FaceTarget(player.position);
            SetAnimatorSpeed(0f);
            return;
        }

        agent.isStopped = false;
        agent.speed = glideSpeed;
        agent.stoppingDistance = stoppingDistance;
        agent.SetDestination(player.position);

        FaceTarget(player.position);
        SetAnimatorSpeed(agent.velocity.magnitude);
    }

    private void StartPhantomShot()
    {
        if (isAttacking || isRecovering || isBeingHit || isDead)
            return;

        isAttacking = true;
        phantomShotResolved = false;

        StopMovement();

        if (player != null)
            FaceTarget(player.position);

        SetAnimatorSpeed(0f);
        TrySetAnimatorTrigger(attackTrigger);

        Log("[CorsairPhantom] Phantom Shot started.");

        StopPhantomShotRoutine();
        BeginPhantomShotChargeVFX();
        phantomShotRoutine = StartCoroutine(PhantomShotRoutine());
    }

    private IEnumerator PhantomShotRoutine()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, phantomShotFireDelay));

        if (isAttacking && !phantomShotResolved)
            ResolvePhantomShot();

        float finishDelay = Mathf.Max(0f, phantomShotFallbackFinishTime - phantomShotFireDelay);
        yield return new WaitForSeconds(finishDelay);

        if (isAttacking)
            FinishPhantomShot(false);

        phantomShotRoutine = null;
    }

    // Animation Event: place this on the Phantom Shot release frame.
    public void OnPhantomShotFire()
    {
        Log("[CorsairPhantom] Phantom Shot fire event fired.");
        ResolvePhantomShot();
    }

    // Animation Event: place this on the final frame of Phantom Shot.
    public void OnPhantomShotFinished()
    {
        Log("[CorsairPhantom] Phantom Shot finished event fired.");
        FinishPhantomShot(true);
    }

    // Backwards-compatible animation event aliases while the animator is being renamed.
    public void OnSpectralSlashHit()
    {
        OnPhantomShotFire();
    }

    public void OnSpectralSlashFinished()
    {
        OnPhantomShotFinished();
    }

    // Animation Event: optional. Place this at the active scream frame.
    public void OnPiercingScreamHit()
    {
        Log("[CorsairPhantom] Piercing Scream hit event fired.");
        ResolvePiercingScream();
    }

    // Animation Event: optional. Place this on the final frame of Piercing Scream.
    public void OnPiercingScreamFinished()
    {
        Log("[CorsairPhantom] Piercing Scream finished event fired.");
        FinishPiercingScream(true);
    }

    private void ResolvePhantomShot()
    {
        if (!isAttacking || phantomShotResolved)
            return;

        phantomShotResolved = true;
        CancelPhantomShotChargeVFX();

        if (player == null)
        {
            Log("[CorsairPhantom] Phantom Shot missed. No player target.");
            return;
        }

        Transform originTransform = phantomShotOrigin != null ? phantomShotOrigin : transform;
        Vector3 origin = originTransform.position;
        Vector3 aimPoint = GetPlayerAimPoint();
        Vector3 toPlayer = aimPoint - origin;
        float hitDistance = toPlayer.magnitude;

        if (hitDistance > phantomShotRange)
        {
            Log($"[CorsairPhantom] Phantom Shot missed. Target out of range: {hitDistance:F2}/{phantomShotRange:F2}");
            return;
        }

        if (TrySpawnPhantomShotProjectile(origin, aimPoint))
        {
            Log("[CorsairPhantom] Phantom Shot projectile spawned.");
            return;
        }

        if (useInstantHitFallbackIfProjectileMissing)
        {
            DealDamageToPlayer(phantomShotDamage);
            Log("[CorsairPhantom] Phantom Shot fallback hit player.");
            return;
        }

        Log("[CorsairPhantom] Phantom Shot missed. No projectile prefab or fallback projectile available.");
    }

    private bool TrySpawnPhantomShotProjectile(Vector3 origin, Vector3 aimPoint)
    {
        PhantomShotProjectile projectile = null;

        if (phantomShotProjectilePrefab != null)
        {
            Quaternion rotation = Quaternion.LookRotation((aimPoint - origin).normalized);
            projectile = Instantiate(phantomShotProjectilePrefab, origin, rotation);
        }
        else if (createProjectileIfPrefabMissing)
        {
            GameObject projectileObject = new GameObject("PhantomShotProjectile");
            projectile = projectileObject.AddComponent<PhantomShotProjectile>();
        }

        if (projectile == null)
            return false;

        projectile.Launch(
            origin,
            aimPoint,
            transform,
            player,
            phantomShotDamage,
            phantomShotProjectileSpeed,
            phantomShotProjectileLifetime,
            phantomShotProjectileHitRadius
        );

        return true;
    }

    private bool ShouldStartGhostGlide(float distanceToPlayer)
    {
        if (!enableGhostGlide || isGhostGliding || isAttacking || isRecovering || isBeingHit || isDead)
            return false;

        if (Time.time < nextGhostGlideTime)
            return false;

        return distanceToPlayer <= ghostGlideTriggerRange;
    }

    private void StartGhostGlide()
    {
        if (player == null)
            return;

        StopGhostGlideRoutine();
        ghostGlideRoutine = StartCoroutine(GhostGlideRoutine(player.position));
    }

    private IEnumerator GhostGlideRoutine(Vector3 savedPlayerPosition)
    {
        isGhostGliding = true;
        isAttacking = false;
        isRecovering = false;
        phantomShotResolved = true;

        StopMovement();
        StopPhantomShotRoutine();
        StopRecoverRoutine();

        Vector3 startPosition = transform.position;
        Vector3 flatDirection = savedPlayerPosition - startPosition;
        flatDirection.y = 0f;

        if (flatDirection.sqrMagnitude <= 0.001f)
            flatDirection = transform.forward;

        flatDirection.Normalize();

        Vector3 glideTarget = savedPlayerPosition + flatDirection * ghostGlidePassThroughDistance;
        glideTarget.y = startPosition.y;

        Log("[CorsairPhantom] Ghost Glide started.");
        Log("[CorsairPhantom] Ghost Glide target saved.");

        float elapsed = 0f;
        bool hitPlayerDuringGlide = false;
        while (elapsed < ghostGlideMaxDuration && !isDead && !isBeingHit)
        {
            elapsed += Time.deltaTime;

            FaceTarget(glideTarget);
            transform.position = Vector3.MoveTowards(
                transform.position,
                glideTarget,
                ghostGlideSpeed * Time.deltaTime
            );

            SetAnimatorSpeed(ghostGlideSpeed);

            if (!hitPlayerDuringGlide && IsPlayerInsideGhostGlideHitRadius())
            {
                hitPlayerDuringGlide = true;
                DealDamageToPlayer(ghostGlideDamage);
                ApplyGhostGlideStunToPlayer();
                Log("[CorsairPhantom] Ghost Glide hit player.");
            }

            if (Vector3.Distance(transform.position, glideTarget) <= ghostGlideStopDistance)
                break;

            yield return null;
        }

        if (!hitPlayerDuringGlide)
            Log("[CorsairPhantom] Ghost Glide missed.");

        Log("[CorsairPhantom] Ghost Glide recovery started.");

        SetAnimatorSpeed(0f);
        yield return new WaitForSeconds(Mathf.Max(0f, ghostGlideRecoveryTime));

        if (!isDead)
        {
            isGhostGliding = false;
            nextGhostGlideTime = Time.time + ghostGlideCooldown;

            if (TryPrepareAgentForMovement())
                agent.isStopped = false;

            Log("[CorsairPhantom] Ghost Glide finished.");
        }

        ghostGlideRoutine = null;
    }

    private void StopGhostGlideRoutine()
    {
        if (ghostGlideRoutine == null)
            return;

        StopCoroutine(ghostGlideRoutine);
        ghostGlideRoutine = null;
        isGhostGliding = false;
    }

    private bool ShouldStartPiercingScream(float distanceToPlayer)
    {
        if (!enablePiercingScream || isCastingPiercingScream || isGhostGliding || isAttacking || isRecovering || isBeingHit || isDead)
            return false;

        if (Time.time < nextPiercingScreamTime)
            return false;

        return distanceToPlayer <= piercingScreamTriggerRange;
    }

    private void StartPiercingScream()
    {
        if (player == null)
            return;

        isCastingPiercingScream = true;
        piercingScreamResolved = false;
        piercingScreamTargetPosition = GetPlayerAimPoint();
        piercingScreamTargetDirection = GetPiercingScreamDirectionToTarget(piercingScreamTargetPosition);
        piercingScreamVisualDirection = GetPiercingScreamVisualDirectionToTarget(piercingScreamTargetPosition);

        StopMovement();

        FaceTarget(piercingScreamTargetPosition);

        SetAnimatorSpeed(0f);
        TrySetAnimatorTrigger(castTrigger);

        BeginPiercingScreamChargeVFX();

        Log("[CorsairPhantom] Piercing Scream cast started.");
        Log($"[CorsairPhantom] Piercing Scream target position saved: {FormatVector3(piercingScreamTargetPosition)}");

        StopPiercingScreamRoutine();
        piercingScreamRoutine = StartCoroutine(PiercingScreamRoutine());
    }

    private IEnumerator PiercingScreamRoutine()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, piercingScreamCastDelay));

        if (isCastingPiercingScream && !piercingScreamResolved)
            ResolvePiercingScream();

        float finishDelay = Mathf.Max(0f, piercingScreamFallbackFinishTime - piercingScreamCastDelay);
        yield return new WaitForSeconds(finishDelay);

        if (isCastingPiercingScream)
            FinishPiercingScream(false);

        piercingScreamRoutine = null;
    }

    private void ResolvePiercingScream()
    {
        if (!isCastingPiercingScream || piercingScreamResolved)
            return;

        piercingScreamResolved = true;
        Log($"[CorsairPhantom] Piercing Scream player position on resolve: {FormatVector3(GetPlayerAimPoint())}");

        bool playerHit = IsPlayerInsidePiercingScreamCone();
        Vector3 impactPoint = playerHit
            ? GetPlayerAimPoint()
            : GetPiercingScreamOrigin() + GetPiercingScreamVisualDirection() * piercingScreamRange;

        ReleasePiercingScreamVFX(playerHit, impactPoint);

        if (playerHit)
        {
            if (piercingScreamDamage > 0)
                DealDamageToPlayer(piercingScreamDamage);

            ApplyPiercingScreamTileCracking();
            Log($"[CorsairPhantom] Piercing Scream hit player for {piercingScreamDamage} damage.");
            return;
        }

        Log("[CorsairPhantom] Piercing Scream missed.");
    }

    private void ApplyPiercingScreamTileCracking()
    {
        if (!crackTilesOnPiercingScreamHit || piercingScreamCrackedTileCount <= 0)
            return;

        if (tileDebuffManager == null)
            tileDebuffManager = FindFirstObjectByType<TileDebuffManager>();

        if (tileDebuffManager == null)
        {
            Debug.LogWarning("[CorsairPhantom] Piercing Scream could not crack tiles. TileDebuffManager not found.");
            return;
        }

        tileDebuffManager.ApplyTileCracking(piercingScreamCrackedTileCount, piercingScreamCrackCounter);
        Log("[CorsairPhantom] Piercing Scream cracked tile(s).");
    }

    private bool IsPlayerInsidePiercingScreamCone()
    {
        if (player == null)
            return false;

        Vector3 origin = GetPiercingScreamOrigin();
        Vector3 playerPoint = GetPlayerAimPoint();
        Vector3 toPlayer = playerPoint - origin;
        float distance = toPlayer.magnitude;

        if (distance > piercingScreamRange)
            return false;

        Vector3 flatForward = GetPiercingScreamAimDirection();
        Vector3 flatToPlayer = toPlayer;
        flatToPlayer.y = 0f;

        if (flatToPlayer.sqrMagnitude <= 0.001f)
            return true;

        float angle = Vector3.Angle(flatForward.normalized, flatToPlayer.normalized);
        return angle <= piercingScreamAngle * 0.5f;
    }

    private Vector3 GetPiercingScreamOrigin()
    {
        if (piercingScreamOrigin != null)
            return piercingScreamOrigin.position;

        Vector3 origin = transform.position + Vector3.up * 1.4f;

        if (visualModel != null)
            origin = visualModel.position + Vector3.up * 1.4f;

        return origin;
    }

    private Vector3 GetPiercingScreamForward()
    {
        if (piercingScreamOrigin != null)
            return piercingScreamOrigin.forward;

        return transform.forward;
    }

    private Vector3 GetPiercingScreamDirectionToTarget(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - GetPiercingScreamOrigin();
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return GetPiercingScreamAimDirection();

        return direction.normalized;
    }

    private Vector3 GetPiercingScreamVisualDirectionToTarget(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - GetPiercingScreamOrigin();

        if (direction.sqrMagnitude <= 0.001f)
            return GetPiercingScreamVisualDirection();

        return direction.normalized;
    }

    private Vector3 GetPiercingScreamAimDirection()
    {
        Vector3 direction = piercingScreamTargetDirection;

        if (!isCastingPiercingScream || direction.sqrMagnitude <= 0.001f)
            direction = GetPiercingScreamForward();

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            direction = transform.forward;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return Vector3.forward;

        return direction.normalized;
    }

    private Vector3 GetPiercingScreamVisualDirection()
    {
        Vector3 direction = piercingScreamVisualDirection;

        if (!isCastingPiercingScream || direction.sqrMagnitude <= 0.001f)
            direction = GetPiercingScreamForward();

        if (direction.sqrMagnitude <= 0.001f)
            direction = GetPiercingScreamAimDirection();

        if (direction.sqrMagnitude <= 0.001f)
            return Vector3.forward;

        return direction.normalized;
    }

    private void BeginPiercingScreamChargeVFX()
    {
        activePiercingScreamVFX = null;

        Vector3 origin = GetPiercingScreamOrigin();
        Vector3 forward = GetPiercingScreamVisualDirection();

        if (piercingScreamVFXPrefab != null)
            activePiercingScreamVFX = Instantiate(piercingScreamVFXPrefab, origin, Quaternion.LookRotation(forward));
        else if (createPiercingScreamVFXIfPrefabMissing)
            activePiercingScreamVFX = new GameObject("PiercingScreamVFX").AddComponent<PiercingScreamVFX>();

        if (activePiercingScreamVFX == null)
            return;

        Transform mouth = piercingScreamOrigin != null ? piercingScreamOrigin : transform;
        activePiercingScreamVFX.BeginCharge(mouth, forward, piercingScreamCastDelay, piercingScreamRange, piercingScreamAngle);
        Log("[CorsairPhantom] Piercing Scream charge VFX started.");
    }

    private void ReleasePiercingScreamVFX(bool playerHit, Vector3 impactPoint)
    {
        Vector3 forward = GetPiercingScreamVisualDirection();

        if (activePiercingScreamVFX != null)
        {
            activePiercingScreamVFX.Release(forward, playerHit, impactPoint);
            activePiercingScreamVFX = null;
            Log("[CorsairPhantom] Piercing Scream layered VFX released.");
            return;
        }

        // Fallback when the layered orchestrator is unavailable: rings only.
        SpawnPiercingScreamWaveVFX();
    }

    private void SpawnPiercingScreamWaveVFX()
    {
        PiercingScreamWaveVFX wave = null;
        Vector3 origin = GetPiercingScreamOrigin();
        Vector3 forward = GetPiercingScreamVisualDirection();

        if (piercingScreamWavePrefab != null)
            wave = Instantiate(piercingScreamWavePrefab, origin, Quaternion.LookRotation(forward));
        else if (createPiercingScreamWaveIfPrefabMissing)
            wave = new GameObject("PiercingScreamWaveVFX").AddComponent<PiercingScreamWaveVFX>();

        if (wave == null)
            return;

        wave.range = piercingScreamRange;
        wave.coneAngle = piercingScreamAngle;
        wave.Play(origin, forward);
        Log("[CorsairPhantom] Piercing Scream wave spawned.");
    }

    private void FinishPiercingScream(bool stopFallbackRoutine)
    {
        if (!isCastingPiercingScream)
            return;

        if (!piercingScreamResolved)
        {
            piercingScreamResolved = true;
            CancelPiercingScreamVFX();
            Debug.LogWarning("[CorsairPhantom] Piercing Scream finished before cone was resolved. Check OnPiercingScreamHit, or tune Piercing Scream Cast Delay.");
        }

        isCastingPiercingScream = false;
        nextPiercingScreamTime = Time.time + piercingScreamCooldown;

        if (stopFallbackRoutine)
            StopPiercingScreamRoutine();
    }

    private void StopPiercingScreamRoutine()
    {
        if (piercingScreamRoutine == null)
            return;

        StopCoroutine(piercingScreamRoutine);
        piercingScreamRoutine = null;
        isCastingPiercingScream = false;
        CancelPiercingScreamVFX();
    }

    private void StopHitFallbackRoutine()
    {
        if (hitFallbackRoutine == null)
            return;

        StopCoroutine(hitFallbackRoutine);
        hitFallbackRoutine = null;
    }

    private void StopDeathFallbackRoutine()
    {
        if (deathFallbackRoutine == null)
            return;

        StopCoroutine(deathFallbackRoutine);
        deathFallbackRoutine = null;
    }

    private void CancelPiercingScreamVFX()
    {
        if (activePiercingScreamVFX == null)
            return;

        activePiercingScreamVFX.Cancel();
        activePiercingScreamVFX = null;
    }

    private bool IsPlayerInsideGhostGlideHitRadius()
    {
        if (player == null)
            return false;

        Vector3 playerPoint = GetPlayerAimPoint();
        Vector3 glidePoint = transform.position;
        playerPoint.y = 0f;
        glidePoint.y = 0f;

        return Vector3.Distance(glidePoint, playerPoint) <= ghostGlideHitRadius;
    }

    private void ApplyGhostGlideStunToPlayer()
    {
        if (player == null || ghostGlideStunDuration <= 0f)
            return;

        PlayerMovement playerMovement = player.GetComponentInChildren<PlayerMovement>();
        if (playerMovement == null)
            playerMovement = player.GetComponentInParent<PlayerMovement>();

        if (playerMovement != null)
        {
            playerMovement.ApplyStun(ghostGlideStunDuration);
            Log("[CorsairPhantom] Ghost Glide stunned player.");
            return;
        }

        MovementofPlayer movementOfPlayer = player.GetComponentInChildren<MovementofPlayer>();
        if (movementOfPlayer == null)
            movementOfPlayer = player.GetComponentInParent<MovementofPlayer>();

        if (movementOfPlayer != null)
        {
            if (playerMovementStunRoutine != null)
                StopCoroutine(playerMovementStunRoutine);

            playerMovementStunRoutine = StartCoroutine(TemporaryDisablePlayerMovement(movementOfPlayer, ghostGlideStunDuration));
            Log("[CorsairPhantom] Ghost Glide stunned player.");
        }
    }

    private IEnumerator TemporaryDisablePlayerMovement(MovementofPlayer movementOfPlayer, float duration)
    {
        if (movementOfPlayer == null)
            yield break;

        bool wasDisabled = movementOfPlayer.disableMovement;
        movementOfPlayer.disableMovement = true;

        yield return new WaitForSeconds(Mathf.Max(0.01f, duration));

        if (movementOfPlayer != null)
            movementOfPlayer.disableMovement = wasDisabled;

        playerMovementStunRoutine = null;
    }

    private Vector3 GetPlayerAimPoint()
    {
        if (player == null)
            return transform.position;

        PlayerHealth playerHealth = player.GetComponentInChildren<PlayerHealth>();
        if (playerHealth != null)
        {
            Collider playerCollider = playerHealth.GetComponentInChildren<Collider>();
            if (playerCollider != null)
                return playerCollider.bounds.center;
        }

        return player.position + Vector3.up;
    }

    private void FinishPhantomShot(bool stopFallbackRoutine)
    {
        if (!isAttacking)
            return;

        if (!phantomShotResolved)
        {
            phantomShotResolved = true;
            Debug.LogWarning("[CorsairPhantom] Phantom Shot finished before damage was resolved. Check OnPhantomShotFire, or tune Phantom Shot Fire Delay.");
        }

        isAttacking = false;

        if (stopFallbackRoutine)
            StopPhantomShotRoutine();

        EnterRecover(phantomShotCooldown);
    }

    private void EnterRecover(float duration)
    {
        if (isDead)
            return;

        isRecovering = true;
        StopMovement();
        StopRecoverRoutine();
        recoverRoutine = StartCoroutine(RecoverRoutine(Mathf.Max(0f, duration)));
    }

    private IEnumerator RecoverRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);

        isRecovering = false;
        recoverRoutine = null;
        Log("[CorsairPhantom] Phantom Shot cooldown finished.");
    }

    private void StopPhantomShotRoutine()
    {
        CancelPhantomShotChargeVFX();

        if (phantomShotRoutine == null)
            return;

        StopCoroutine(phantomShotRoutine);
        phantomShotRoutine = null;
    }

    private void BeginPhantomShotChargeVFX()
    {
        CancelPhantomShotChargeVFX();

        PhantomShotChargeVFX charge = null;
        Transform origin = phantomShotOrigin != null ? phantomShotOrigin : transform;

        if (phantomShotChargePrefab != null)
            charge = Instantiate(phantomShotChargePrefab, origin.position, origin.rotation);
        else if (createPhantomShotChargeIfPrefabMissing)
            charge = new GameObject("PhantomShotChargeVFX").AddComponent<PhantomShotChargeVFX>();

        if (charge == null)
            return;

        activePhantomShotChargeVFX = charge;
        activePhantomShotChargeVFX.BeginCharge(origin, phantomShotFireDelay);
        Log("[CorsairPhantom] Phantom Shot charge VFX started.");
    }

    private void CancelPhantomShotChargeVFX()
    {
        if (activePhantomShotChargeVFX == null)
            return;

        activePhantomShotChargeVFX.Cancel();
        activePhantomShotChargeVFX = null;
    }

    private void StopRecoverRoutine()
    {
        if (recoverRoutine == null)
            return;

        StopCoroutine(recoverRoutine);
        recoverRoutine = null;
    }

    private bool TryPrepareAgentForMovement()
    {
        if (agent == null || !agent.enabled)
            return false;

        ConfigureAgent();

        if (agent.isOnNavMesh)
        {
            loggedMissingNavMesh = false;
            return true;
        }

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, navMeshSearchRadius, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            loggedMissingNavMesh = false;
            return agent.isOnNavMesh;
        }

        if (!loggedMissingNavMesh)
        {
            Debug.LogWarning("[CorsairPhantom] NavMeshAgent is not on a NavMesh.");
            loggedMissingNavMesh = true;
        }

        return false;
    }

    private void FaceTarget(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
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

    private float GetFlatDistanceToPlayer()
    {
        if (player == null)
            return float.MaxValue;

        return GetFlatDistanceToPoint(player.position);
    }

    private float GetFlatDistanceToPoint(Vector3 point)
    {
        Vector3 from = transform.position;
        from.y = 0f;
        point.y = 0f;
        return Vector3.Distance(from, point);
    }

    private void SetAnimatorSpeed(float speed)
    {
        if (animator == null || string.IsNullOrWhiteSpace(speedParameter))
            return;

        int speedHash = Animator.StringToHash(speedParameter);
        if (HasAnimatorParameter(speedHash, AnimatorControllerParameterType.Float))
            animator.SetFloat(speedHash, speed);
    }

    private bool TrySetAnimatorTrigger(string triggerName, bool warnIfMissing = true)
    {
        if (animator == null || string.IsNullOrWhiteSpace(triggerName))
            return false;

        int triggerHash = Animator.StringToHash(triggerName);
        if (!HasAnimatorParameter(triggerHash, AnimatorControllerParameterType.Trigger))
        {
            if (warnIfMissing)
                Debug.LogWarning($"[CorsairPhantom] Animator is missing the {triggerName} trigger.");

            return false;
        }

        animator.SetTrigger(triggerHash);
        return true;
    }

    private void ResetActionTriggers()
    {
        ResetAnimatorTrigger(attackTrigger);
        ResetAnimatorTrigger(castTrigger);
        ResetAnimatorTrigger(hitTrigger);
    }

    private void ResetAnimatorTrigger(string triggerName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(triggerName))
            return;

        int triggerHash = Animator.StringToHash(triggerName);
        if (HasAnimatorParameter(triggerHash, AnimatorControllerParameterType.Trigger))
            animator.ResetTrigger(triggerHash);
    }

    private bool HasAnimatorParameter(int parameterHash, AnimatorControllerParameterType parameterType)
    {
        if (animator == null)
            return false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == parameterHash && parameter.type == parameterType)
                return true;
        }

        return false;
    }

    private void DealDamageToPlayer(float damage)
    {
        if (player == null)
            return;

        IDamageable damageable = player.GetComponentInChildren<IDamageable>();
        if (damageable == null)
            damageable = player.GetComponentInParent<IDamageable>();

        if (damageable != null)
            damageable.ApplyDamage(damage);
        else
            player.SendMessage("ApplyDamage", damage, SendMessageOptions.DontRequireReceiver);
    }

    private void Log(string message)
    {
        if (debugLogs)
            Debug.Log(message);
    }

    private string FormatVector3(Vector3 value)
    {
        return $"({value.x:F2}, {value.y:F2}, {value.z:F2})";
    }

    private void OnDrawGizmosSelected()
    {
        if (!showRangeGizmos)
            return;

        Vector3 center = transform.position;
        center.y += 0.1f;

        DrawFilledAndWireSphere(center, attackRange, attackRangeGizmoColor);
        DrawWireSphere(center, ghostGlideTriggerRange, ghostGlideRangeGizmoColor);
        DrawWireSphere(center, ghostGlideHitRadius, ghostGlideHitGizmoColor);
        DrawWireSphere(center, piercingScreamTriggerRange, piercingScreamTriggerGizmoColor);
        DrawWireSphere(center, phantomShotRange, new Color(0.35f, 1f, 0.95f, 0.9f));
        DrawPiercingScreamCone();
        DrawWireSphere(center, stoppingDistance, stoppingDistanceGizmoColor);

        if (showNavMeshSearchGizmo)
            DrawWireSphere(transform.position, navMeshSearchRadius, navMeshSearchGizmoColor);
    }

    private void DrawFilledAndWireSphere(Vector3 center, float radius, Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawSphere(center, radius);

        DrawWireSphere(center, radius, new Color(color.r, color.g, color.b, 1f));
    }

    private void DrawWireSphere(Vector3 center, float radius, Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawWireSphere(center, radius);
    }

    private void DrawPiercingScreamCone()
    {
        Vector3 origin = GetPiercingScreamOrigin();
        Vector3 forward = GetPiercingScreamAimDirection();
        forward.y = 0f;

        if (forward.sqrMagnitude <= 0.001f)
            forward = Vector3.forward;

        forward.Normalize();

        Quaternion leftRotation = Quaternion.AngleAxis(-piercingScreamAngle * 0.5f, Vector3.up);
        Quaternion rightRotation = Quaternion.AngleAxis(piercingScreamAngle * 0.5f, Vector3.up);
        Vector3 leftDirection = leftRotation * forward;
        Vector3 rightDirection = rightRotation * forward;

        Gizmos.color = piercingScreamConeGizmoColor;
        Gizmos.DrawLine(origin, origin + leftDirection * piercingScreamRange);
        Gizmos.DrawLine(origin, origin + rightDirection * piercingScreamRange);
        Gizmos.DrawWireSphere(origin + forward * piercingScreamRange, 0.2f);
    }
}
