using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class CursedJesterController : MonoBehaviour
{
    public enum EnemyState
    {
        Approach,
        BasicAttack,
        ConfettiBlast,
        Taunt,
        Recover,
        BeingHit,
        Dead
    }

    [Header("References")]
    public Animator animator;
    public NavMeshAgent agent;
    public Transform player;

    [Header("Stats")]
    public float walkSpeed = 3.0f;
    public float recoverTime = 0.6f;

    [Header("Basic Attack")]
    public float basicAttackRange = 2.5f;
    public int basicAttackDamage = 7;

    [Header("Confetti Blast")]
    public float confettiBlastRange = 8f;
    public int confettiBlastDamage = 9;
    public float confettiBlastCooldown = 4f;

    [Header("Taunt")]
    public float tauntRange = 6f;
    public float tauntCooldown = 7f;
    public float tauntSlowMultiplier = 0.6f;
    public float tauntSlowDuration = 2.0f;

    private EnemyState currentState = EnemyState.Approach;
    private float confettiBlastTimer = 0f;
    private float tauntTimer = 0f;
    private bool battleStarted = false;
    private bool basicAttackStarted = false;
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
        }

        if (animator != null)
            animator.applyRootMotion = false;
    }

    public void StartBattle()
    {
        if (battleStarted) return;

        battleStarted = true;
        if (agent != null)
        {
            agent.speed = walkSpeed;
            agent.isStopped = false;
            agent.ResetPath();
        }

        currentState = EnemyState.Approach;
    }

    private void Update()
{
    if (!battleStarted) return;
    if (currentState == EnemyState.Dead) return;
    if (player == null) return;

    UpdateAnimatorSpeed();
    FaceTarget(player.position);

    switch (currentState)
    {
        case EnemyState.Approach:
            HandleApproach();
            break;
        case EnemyState.BasicAttack:
            HandleBasicAttack();
            break;
        case EnemyState.Recover:
            break;
        case EnemyState.BeingHit:
            break;
    }
}

private void HandleApproach()
{
    float distance = Vector3.Distance(transform.position, player.position);

    if (distance <= basicAttackRange)
    {
        agent.isStopped = true;
        agent.ResetPath();
        currentState = EnemyState.BasicAttack;
        basicAttackStarted = false;
        return;
    }

    agent.isStopped = false;
    agent.SetDestination(player.position);
}

    private void HandleBasicAttack()
{
    agent.isStopped = true;
    agent.ResetPath();

    if (!basicAttackStarted)
    {
        basicAttackStarted = true;

        if (animator != null)
            animator.SetTrigger("BasicAttack");

        Debug.Log("[CursedJester] BasicAttack started.");
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
    Debug.Log("[CursedJester] BasicAttack finished -> Recover.");
    EnterRecover();
}

private void DealDamageToPlayer(float damage)
{
    var damageable = player.root.GetComponentInChildren<IDamageable>();

    if (damageable != null)
        damageable.ApplyDamage(damage);
    else
        player.SendMessage("ApplyDamage", damage, SendMessageOptions.DontRequireReceiver);
}

private void EnterRecover()
{
    currentState = EnemyState.Recover;
    agent.isStopped = true;
    agent.ResetPath();

    if (recoverCoroutine != null)
        StopCoroutine(recoverCoroutine);

    recoverCoroutine = StartCoroutine(RecoverRoutine());
}

private IEnumerator RecoverRoutine()
{
    yield return new WaitForSeconds(recoverTime);

    recoverCoroutine = null;
    basicAttackStarted = false;

    currentState = EnemyState.Approach;

    Debug.Log("[CursedJester] Recover done -> Approach.");
}

    public void TriggerBeingHit()
    {
        // Layer 7
    }

    public void TriggerDeath()
    {
        // Layer 8
    }

    private void UpdateAnimatorSpeed()
    {
        if (animator == null || agent == null) return;

        float speedValue = 0f;

        if (!agent.isStopped)
            speedValue = agent.velocity.magnitude;

        animator.SetFloat("Speed", speedValue);
    }

    private void FaceTarget(Vector3 targetPos, float turnSpeed = 360f)
    {
        Vector3 direction = targetPos - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            turnSpeed * Time.deltaTime
        );
    }
}
