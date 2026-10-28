using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Consultation prototype for the existing Vander Poel cinematic actor.
/// Basic pursuit/contact damage only; a dedicated boss rig, abilities and death
/// animation remain necessary for production gameplay.
/// </summary>
[RequireComponent(typeof(EnemyHealth))]
public sealed class Stage4VanderPoelPreviewEncounterV01 : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField, Min(1f)] private float attackDistance = 15f;
    [SerializeField, Min(0.1f)] private float attackInterval = 2.5f;
    [SerializeField, Min(0f)] private float contactDamage = 12f;
    [SerializeField, Min(0f)] private float speed = 8f;
    private EnemyHealth health;
    private PlayerHealth playerHealth;
    private bool battleStarted;
    private bool defeated;
    private float nextAttack;
    private float nextRepath;

    public void Configure(Transform player, NavMeshAgent navigation)
    {
        target = player;
        agent = navigation;
    }

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        if (agent == null) agent = GetComponent<NavMeshAgent>();
    }

    public void StartBattle()
    {
        if (battleStarted || defeated || (health != null && health.IsDead))
            return;
        if (target == null)
        {
            PlayerHealth player = FindFirstObjectByType<PlayerHealth>();
            if (player != null) target = player.transform;
        }
        if (target == null)
        {
            Debug.LogWarning("[Stage4 Vander Poel Preview] No player. Battle remains idle.", this);
            return;
        }
        playerHealth = target.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null) playerHealth = target.GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogWarning("[Stage4 Vander Poel Preview] Missing PlayerHealth.", this);
            return;
        }
        battleStarted = true;
        nextAttack = Time.time + attackInterval;
        Debug.Log("[Stage4 Vander Poel Preview] Basic chase and contact damage enabled.", this);
    }

    private void Update()
    {
        if (!battleStarted || defeated || target == null || playerHealth == null) return;
        Vector3 delta = target.position - transform.position;
        Vector3 flat = new Vector3(delta.x, 0f, delta.z);
        float distance = flat.magnitude;

        if (flat.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(flat), 105f * Time.deltaTime);
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.speed = speed;
            agent.stoppingDistance = attackDistance * 0.75f;
            if (distance > attackDistance)
            {
                if (Time.time >= nextRepath)
                {
                    agent.SetDestination(target.position);
                    nextRepath = Time.time + 0.35f;
                }
            }
            else
                agent.ResetPath();
        }
        if (distance <= attackDistance && Time.time >= nextAttack)
        {
            nextAttack = Time.time + attackInterval;
            // Temporary proximity-based hit, not an animation impact event.
            playerHealth.TakeDamage(contactDamage);
        }
    }

    public void TriggerDeath()
    {
        if (defeated) return;
        defeated = true;
        battleStarted = false;
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }
        Debug.Log("[Stage4 Vander Poel Preview] Defeated; pending Stage 4 node relay.", this);
    }
}
