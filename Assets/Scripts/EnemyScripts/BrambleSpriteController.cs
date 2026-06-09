using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class BrambleSpriteController : MonoBehaviour
{
    public event System.Action<BrambleSpriteController> OnDeathStarted;

    [Header("Potion Drop")]
    public bool dropPotionOnDeath = true;
    public string potionDropLogSource = "BrambleSprite";
    [Range(0f, 1f)] public float potionDropChance = 1f;
    public int potionDropMinCount = 1;
    public int potionDropMaxCount = 1;
    public int potionDropAmountPerDrop = 1;
    public bool canDropHealthPotion = true;
    public bool canDropPurifyPotion = true;
    public bool canDropPowerUpPotion = true;

    // ─────────────────────────────────────────────
    //  STATE ENUM
    //  Only Approach and Dead are active this layer.
    //  The rest are stubs kept for future layers.
    // ─────────────────────────────────────────────
    public enum EnemyState
    {
        Approach,
        CloseAttack,
        CloseAttack2,
        ThornThrow,
        Recover,
        Dead,
        BeingHit
    }

    [Header("References")]
    public Transform player;
    public NavMeshAgent agent;
    public Animator animator;

    [Header("Stats")]
    public float maxHealth = 80f;
    private float currentHealth;

    [Header("Movement")]
    public float walkSpeed = 3.5f;

    [Header("Ranges")]
    public float closeAttackRange = 2.5f;

    [Header("Abilities — kept for future layers")]
    public GameObject thornPrefab;
    public Transform thornSpawnPoint;
    public float thornThrowRange = 6f;
    public int thornDamage = 8;
    public float thornSpeed = 14f;
    public float thornLifeTime = 5f;
    public float extendedThornThrowRange = 8f;
    public float antiKiteTime = 1.5f;
    public float clawDamage = 8f;

    // ─── Runtime state ───
    public EnemyState currentState = EnemyState.Approach;
    private bool battleStarted = false;
    private bool deathEventSent = false;
    private bool closeAttackStarted = false;
    private bool closeAttack2Started = false;
    private Coroutine recoverCoroutine;
    public float recoverTime = 0.7f;
    private bool thornThrowStarted = false;
    private bool wasInCloseRange = false;
    private float timeOutsideSlashRange = 0f;
    public float thornRearmTime = 1.25f;
    private float timeOutsideNormalThornRange = 0f;
    private bool antiKiteThornReady = false;
    private bool beingHitStarted = false;
    private bool potionDropResolved = false;


    // ─────────────────────────────────────────────
    //  AWAKE / START
    // ─────────────────────────────────────────────
    private void Awake()
    {
        agent ??= GetComponent<NavMeshAgent>();
        animator ??= GetComponent<Animator>();
        currentHealth = maxHealth;

        if (player == null)
        {
            var pgo = GameObject.FindWithTag("Player");
            if (pgo != null) player = pgo.transform;
        }
    }

    private void Start()
    {
        agent.updateRotation = false;   // We rotate manually via FaceTarget
        agent.updatePosition = true;
        agent.speed = walkSpeed;
        agent.stoppingDistance = 0f;
        agent.autoBraking = true;
        animator.applyRootMotion = false;
    }

    // ─────────────────────────────────────────────
    //  UPDATE — Layer 1 behavior only
    // ─────────────────────────────────────────────
    private void Update()
    {

        if (!battleStarted) return;

        if (currentState == EnemyState.Dead) return;
        if (player == null) return;

        FaceTarget(player.position);

        switch (currentState)
        {
            case EnemyState.Approach:
                HandleApproach();
                break;

            case EnemyState.CloseAttack:
                HandleCloseAttack();
                break;

            case EnemyState.CloseAttack2:
                HandleCloseAttack2();
                break;

            case EnemyState.Recover:
                // Recover handled by coroutine
                break;

            case EnemyState.ThornThrow:
                HandleThornThrow();
                break;
            case EnemyState.BeingHit:
                HandleBeingHit();
                break;
        }
        // Drive the blend tree Speed float from actual agent velocity
        if (animator != null && agent != null && agent.enabled)
            animator.SetFloat("Speed", agent.velocity.magnitude / Mathf.Max(walkSpeed, 0.01f));
    }

    // ─────────────────────────────────────────────
    //  LAYER 1 — APPROACH
    // ─────────────────────────────────────────────
    private void HandleApproach()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // CLOSE ZONE -> melee combo
        if (distanceToPlayer <= closeAttackRange)
        {
            agent.isStopped = true;
            agent.ResetPath();

            currentState = EnemyState.CloseAttack;
            closeAttackStarted = false;
            closeAttack2Started = false;

            timeOutsideNormalThornRange = 0f;
            antiKiteThornReady = false;

          
            return;
        }

        // NORMAL THORN ZONE
        if (distanceToPlayer <= thornThrowRange)
        {
            agent.isStopped = true;
            agent.ResetPath();

            currentState = EnemyState.ThornThrow;
            thornThrowStarted = false;

            timeOutsideNormalThornRange = 0f;
            antiKiteThornReady = false;

      
            return;
        }

        // OUTSIDE NORMAL THORN RANGE -> build anti-kite timer
        timeOutsideNormalThornRange += Time.deltaTime;

        if (timeOutsideNormalThornRange >= antiKiteTime)
            antiKiteThornReady = true;

        // EXTENDED THORN ZONE if anti-kite is armed
        if (antiKiteThornReady && distanceToPlayer <= extendedThornThrowRange)
        {
            agent.isStopped = true;
            agent.ResetPath();

            currentState = EnemyState.ThornThrow;
            thornThrowStarted = false;

            timeOutsideNormalThornRange = 0f;
            antiKiteThornReady = false;

            return;
        }

        // Otherwise keep chasing
        agent.isStopped = false;
        agent.SetDestination(player.position);
    }
    private void HandleBeingHit()
    {
        agent.isStopped = true;
        agent.ResetPath();

        if (!beingHitStarted)
        {
            beingHitStarted = true;

            if (animator != null)
                animator.SetTrigger("BeingHit");

 
        }
    }
    public void OnBeingHitFinished()
    {
        Debug.Log("[BrambleSprite] BeingHit finished -> Approach");
        beingHitStarted = false;
        currentState = EnemyState.Approach;
    }

    private void HandleCloseAttack()
    {
        agent.isStopped = true;
        agent.ResetPath();

        if (!closeAttackStarted)
        {
            closeAttackStarted = true;

            if (animator != null)
                animator.SetTrigger("ClawAttack");

            Debug.Log("[BrambleSprite] Triggered ClawAttack.");
        }
    }

    public void OnClawAttackFinished()
    {
        currentState = EnemyState.CloseAttack2;
        closeAttack2Started = false;
        Debug.Log("[BrambleSprite] ClawAttack finished -> CloseAttack2");
    }

    private void HandleCloseAttack2()
    {
        agent.isStopped = true;
        agent.ResetPath();

        if (!closeAttack2Started)
        {
            closeAttack2Started = true;

            if (animator != null)
                animator.SetTrigger("ClawAttack2");

            Debug.Log("[BrambleSprite] Triggered ClawAttack2.");
        }
    }

    public void OnClawAttack2Finished()
    {
        Debug.Log("[BrambleSprite] ClawAttack2 finished -> Recover");
        EnterRecover();
    }

    private void HandleThornThrow()
{
    agent.isStopped = true;
    agent.ResetPath();

    if (!thornThrowStarted)
    {
        thornThrowStarted = true;

        if (animator != null)
            animator.SetTrigger("ThornToss");

        Debug.Log("[BrambleSprite] Triggered ThornToss.");
    }
}

    public void OnThornTossFinished()
    {
        Debug.Log("[BrambleSprite] ThornToss finished -> Recover");

        timeOutsideNormalThornRange = 0f;
        antiKiteThornReady = false;

        EnterRecover();
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
        Debug.Log("[BrambleSprite] Recover started.");

        if (animator != null)
            animator.SetFloat("Speed", 0f);

        yield return new WaitForSeconds(recoverTime);
        recoverCoroutine = null;
        currentState = EnemyState.Approach;

        Debug.Log("[BrambleSprite] Recover ended. Back to Approach.");
    }

    // ─────────────────────────────────────────────
    //  START BATTLE
    // ─────────────────────────────────────────────
    public void StartBattle()
    {
        if (battleStarted) return;

        battleStarted = true;
        currentState = EnemyState.Approach;
        potionDropResolved = false;
        agent.isStopped = false;
        agent.ResetPath();
        timeOutsideSlashRange = 0f;
        timeOutsideNormalThornRange = 0f;
        antiKiteThornReady = false;

        Debug.Log("[BrambleSprite] Battle started — entering Approach.");
    }

    // ─────────────────────────────────────────────
    //  HEALTH / TAKE DAMAGE
    // ─────────────────────────────────────────────
    public void TakeDamage(float damage)
    {
        if (currentState == EnemyState.Dead) return;
        if (!battleStarted) return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        Debug.Log($"[BrambleSprite] Took {damage} dmg. HP: {currentHealth}/{maxHealth}");

        EnemyHealth enemyHealth = GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.currentHealth = currentHealth;
            if (enemyHealth.healthSlider != null)
                enemyHealth.healthSlider.value = currentHealth;
        }

        if (currentHealth <= 0f)
        {
            EnterDead();
            return;
        }

        // Cancel any current action and force hit reaction
        if (recoverCoroutine != null)
        {
            StopCoroutine(recoverCoroutine);
            recoverCoroutine = null;
        }

        agent.isStopped = true;
        agent.ResetPath();

        closeAttackStarted = false;
        closeAttack2Started = false;
        thornThrowStarted = false;
        beingHitStarted = false;

        currentState = EnemyState.BeingHit;
    }

    // ─────────────────────────────────────────────
    //  DEATH
    // ─────────────────────────────────────────────
    private void EnterDead()
    {
        if (currentState == EnemyState.Dead) return;

        currentState = EnemyState.Dead;

        if (recoverCoroutine != null)
        {
            StopCoroutine(recoverCoroutine);
            recoverCoroutine = null;
        }

        closeAttackStarted = false;
        closeAttack2Started = false;
        thornThrowStarted = false;
        beingHitStarted = false;

        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        if (animator != null)
        {
            animator.ResetTrigger("BeingHit");
            animator.ResetTrigger("ClawAttack");
            animator.ResetTrigger("ClawAttack2");
            animator.ResetTrigger("ThornToss");
            animator.SetTrigger("DeadTrigger");
        }
    }

    public void OnDeathAnimationFinished()
    {
        if (agent != null && agent.enabled)
            agent.enabled = false;

        if (!deathEventSent)
        {
            deathEventSent = true;
            TryDropPotionReward();
            OnDeathStarted?.Invoke(this);
        }

        Destroy(gameObject);
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

    private IEnumerator DestroyAfterDeath()
    {
        yield return new WaitForSeconds(5f);
        Destroy(gameObject);
    }

    // ─────────────────────────────────────────────
    //  ANIMATION EVENTS
    //  Only ClawAttack and ThornToss exist in the animator.
    //  Stubs kept so Unity doesn't throw missing method errors.
    // ─────────────────────────────────────────────
    public void OnClawAttackHit()
    {
        Debug.Log("[BrambleSprite] OnClawAttackHit fired.");

        if (player == null)
            return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= closeAttackRange + 0.5f)
        {
            DealDamageToPlayer(clawDamage);
            Debug.Log("[BrambleSprite] Claw attack hit the player.");
        }
    }

    private void DealDamageToPlayer(float damage)
    {
        var damageable = player.root.GetComponentInChildren<IDamageable>();

        if (damageable != null)
            damageable.ApplyDamage(damage);
        else
            player.SendMessage("ApplyDamage", damage, SendMessageOptions.DontRequireReceiver);

        Debug.Log($"[BrambleSprite] Dealt {damage} damage to player.");
    }

    
    public void OnThornToss() { 
     if (thornPrefab == null || thornSpawnPoint == null || player == null)
        {
            return;
        }

        GameObject thorn = Instantiate(thornPrefab, thornSpawnPoint.position, thornSpawnPoint.rotation);
        Vector3 direction = player.position - thornSpawnPoint.position;
        direction.y = 0f;
        direction = direction.normalized;

        ThornProjectile thornComponent = thorn.GetComponent<ThornProjectile>();
        if(thornComponent != null)
        {
            thornComponent.speed = thornSpeed;
            thornComponent.lifeTime = thornLifeTime;
            thornComponent.Launch(direction, thornDamage);
        } else
        {
            Rigidbody rb = thorn.GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = direction * thornSpeed;
            else Debug.LogWarning("[BrambleSprite] Thorn Prefab has no ThornProjectile or RigidBody.");
        }

        Debug.Log("[BrambleSprite] Thorn Projectile Launched.");

    }

    // ─────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────
    private void FaceTarget(Vector3 targetPos, float turnSpeedDegreesPerSecond = 720f)
    {
        Vector3 dir = targetPos - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(dir.normalized);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                Mathf.Max(0f, turnSpeedDegreesPerSecond) * Time.deltaTime
            );
        }
    }

    // Kept for future layers (ThornThrow, CloseAttack animation waits)
    private IEnumerator WaitForAnimationToComplete(string stateName)
    {
        if (animator == null) yield break;
        yield return null;
        float timeout = 10f;
        float elapsed = 0f;
        while (elapsed < timeout)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.IsName(stateName) && info.normalizedTime >= 1f) yield break;
            elapsed += Time.deltaTime;
            yield return null;
        }
        Debug.LogWarning($"[BrambleSprite] WaitForAnimation timeout: {stateName}");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, closeAttackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, thornThrowRange);
    }
}
