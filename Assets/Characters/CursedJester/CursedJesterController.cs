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
    public float rotationSpeed = 360f;

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

    [Header("Death Callback")]
    public MonoBehaviour deathReceiver;
    public string deathMessage = "OnCursedJesterDefeated";

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
    private float confettiBlastCooldownTimer = 0f;
    private float tauntCooldownTimer = 0f;

    private Coroutine recoverCoroutine;

    private void Awake()
    {
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
            agent.speed = walkSpeed;
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

        if (recoverCoroutine != null)
        {
            StopCoroutine(recoverCoroutine);
            recoverCoroutine = null;
        }

        if (agent != null)
        {
            agent.speed = walkSpeed;
            agent.isStopped = false;
            agent.ResetPath();
        }

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
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        agent.isStopped = false;
        agent.SetDestination(player.position);

        FaceTarget(player.position);
        SetAnimatorSpeed(agent.velocity.magnitude);
    }

    private void TriggerBasicAttack()
    {
        if (isAttacking || isRecovering || isBeingHit || isDead) return;

        isAttacking = true;

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
        Debug.Log("[CursedJester] ConfettiBlast finished event fired.");
        isAttacking = false;
        EnterRecover();
    }

    private void TriggerTaunt()
    {
        if (isAttacking || isRecovering || isBeingHit || isDead) return;

        isAttacking = true;
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

        if (deathReceiver != null)
        {
            deathReceiver.SendMessage(deathMessage, SendMessageOptions.DontRequireReceiver);
        }

        gameObject.SetActive(false);
    }

    private void StopMovement()
    {
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
