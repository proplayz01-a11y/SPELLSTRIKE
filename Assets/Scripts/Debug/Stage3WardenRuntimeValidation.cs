using System;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class Stage3WardenRuntimeValidation : MonoBehaviour
{
    public const string DoneKey = "SpellStrike.Stage3WardenValidation.Done";
    public const string PassKey = "SpellStrike.Stage3WardenValidation.Pass";

    private int phase;
    private int waitFrames;
    private int phaseFrames;

    private SunboundWardenController warden;
    private NavMeshAgent agent;
    private EnemyHealth health;
    private CapsuleCollider capsule;
    private Animator animator;
    private Transform player;
    private Vector3 chaseStart;
    private float healthBeforeHit;

    private bool runSeen;
    private bool radiantRushSeen;
    private bool solarSlamSeen;
    private bool sunSlashSeen;
    private bool hitSeen;
    private bool deathSeen;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Time.timeScale = 1f;
    }

    private void Update()
    {
        Time.timeScale = 1f;

        try
        {
            switch (phase)
            {
                case 0:
                    WaitForStage3Runtime();
                    break;
                case 1:
                    TickChase();
                    break;
                case 2:
                    TickRadiantRush();
                    break;
                case 3:
                    TickSolarSlam();
                    break;
                case 4:
                    TickSunSlash();
                    break;
                case 5:
                    TickHitReaction();
                    break;
                case 6:
                    TickDeath();
                    break;
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError(
                $"[Stage3WardenValidation] FAIL phase={phase} frame={Time.frameCount} " +
                $"scene={SceneManager.GetActiveScene().name}");
            Finish(false);
        }
    }

    private void WaitForStage3Runtime()
    {
        if (SceneManager.GetActiveScene().name != "Stage3Scene")
            return;

        if (++waitFrames < 10)
            return;

        warden = FindSceneComponent<SunboundWardenController>();
        if (warden == null)
            throw new InvalidOperationException("Sunbound Warden controller is missing from Stage3Scene.");

        agent = warden.GetComponent<NavMeshAgent>();
        health = warden.GetComponent<EnemyHealth>();
        capsule = warden.GetComponent<CapsuleCollider>();
        animator = warden.GetComponentInChildren<Animator>(true);

        GameObject playerObject = GameObject.FindWithTag("Player");
        player = playerObject != null ? playerObject.transform : null;

        if (agent == null || health == null || capsule == null || animator == null || player == null)
        {
            if (waitFrames < 360)
                return;

            throw new InvalidOperationException(
                $"Missing runtime reference agent={agent != null} health={health != null} " +
                $"capsule={capsule != null} animator={animator != null} player={player != null}.");
        }

        // Keep the controlled test isolated from the physical encounter trigger.
        StageNodeTrigger trigger = FindSceneComponent<StageNodeTrigger>();
        if (trigger != null)
            trigger.enabled = false;

        if (!agent.enabled || !agent.isOnNavMesh)
        {
            if (waitFrames < 360)
                return;

            throw new InvalidOperationException(
                $"Stage3 NavMesh bootstrap never rebound the Warden. enabled={agent.enabled} onMesh={agent.isOnNavMesh}");
        }

        NavMeshSurface[] surfaces = FindSceneComponents<NavMeshSurface>();
        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
        if (surfaces.Length != 1 || triangulation.vertices.Length == 0 || triangulation.indices.Length == 0)
        {
            throw new InvalidOperationException(
                $"Unexpected Stage3 NavMesh state surfaces={surfaces.Length} " +
                $"vertices={triangulation.vertices.Length} indices={triangulation.indices.Length}.");
        }

        if (warden.BattleStarted)
            throw new InvalidOperationException("Warden battle started before Node 1 activation.");
        if (capsule.enabled)
            throw new InvalidOperationException("Warden damage collider is targetable before battle.");
        if (!warden.CompareTag("Untagged"))
            throw new InvalidOperationException($"Warden prebattle tag should be Untagged, got '{warden.tag}'.");

        if (animator.runtimeAnimatorController == null)
            throw new InvalidOperationException("Warden Animator has no runtime controller.");
        if (animator.avatar == null || !animator.avatar.isValid || animator.avatar.isHuman)
            throw new InvalidOperationException(
                $"Warden Generic avatar invalid. avatar={animator.avatar != null} " +
                $"valid={(animator.avatar != null && animator.avatar.isValid)} " +
                $"isHuman={(animator.avatar != null && animator.avatar.isHuman)}");

        ValidateAnimatorParameters(animator);
        ValidateSwordAndGrip();

        const float minimumChaseDistance = 11.0f;
        if (!TryFindReachableTarget(agent, minimumChaseDistance, out Vector3 chaseTarget))
        {
            float maxReachable = FindMaxReachableDistance(agent, out Vector3 farthestPoint);
            throw new InvalidOperationException(
                $"Could not find a complete same-island chase target >={minimumChaseDistance:0.0}m from Warden. " +
                $"Max sampled reachable={maxReachable:0.00}m point={farthestPoint:F2}.");
        }

        TeleportPlayer(chaseTarget);
        Physics.SyncTransforms();

        chaseStart = agent.transform.position;
        warden.StartBattle();

        if (!warden.BattleStarted || !capsule.enabled || !warden.CompareTag("Enemy"))
        {
            throw new InvalidOperationException(
                $"StartBattle gating failed battle={warden.BattleStarted} " +
                $"collider={capsule.enabled} tag={warden.tag}.");
        }

        Debug.Log(
            $"[Stage3WardenValidation] START chase start={chaseStart:F2} target={chaseTarget:F2} " +
            $"dist={FlatDistance(chaseStart, chaseTarget):0.00} " +
            $"navVertices={triangulation.vertices.Length} navIndices={triangulation.indices.Length}");

        phaseFrames = 0;
        phase = 1;
    }

    private void TickChase()
    {
        phaseFrames++;
        ObserveAnimatorStates();

        if (phaseFrames == 3)
        {
            Debug.Log(
                $"[Stage3WardenValidation] CHASE F3 actor={agent.transform.position:F2} " +
                $"player={player.position:F2} dest={agent.destination:F2} hasPath={agent.hasPath} " +
                $"pending={agent.pathPending} status={agent.pathStatus} vel={agent.velocity.magnitude:0.00}");
        }

        if (phaseFrames < 90)
            return;

        float moved = FlatDistance(chaseStart, agent.transform.position);
        bool chasePass = moved > 0.35f && agent.enabled && agent.isOnNavMesh;
        Debug.Log(
            $"[Stage3WardenValidation] CHASE RESULT moved={moved:0.00} enabled={agent.enabled} " +
            $"onMesh={agent.isOnNavMesh} hasPath={agent.hasPath} stopped={agent.isStopped} " +
            $"status={agent.pathStatus} vel={agent.velocity.magnitude:0.00} runSeen={runSeen} PASS={chasePass}");

        if (!chasePass)
            throw new InvalidOperationException("Warden did not physically chase the player.");

        MovePlayerToCombatDistance(7f);
        phaseFrames = 0;
        phase = 2;
    }

    private void TickRadiantRush()
    {
        phaseFrames++;
        ObserveAnimatorStates();

        if (phaseFrames < 70)
            return;

        if (!radiantRushSeen)
            throw new InvalidOperationException("Radiant Rush Animator state was never observed at ~7m.");

        Debug.Log("[Stage3WardenValidation] RADIANT RUSH PASS.");
        MovePlayerToCombatDistance(3.5f);
        phaseFrames = 0;
        phase = 3;
    }

    private void TickSolarSlam()
    {
        phaseFrames++;
        ObserveAnimatorStates();

        if (phaseFrames < 105)
            return;

        if (!solarSlamSeen)
            throw new InvalidOperationException("Solar Slam Animator state was never observed at ~3.5m.");

        Debug.Log("[Stage3WardenValidation] SOLAR SLAM PASS.");
        MovePlayerToCombatDistance(2.25f);
        phaseFrames = 0;
        phase = 4;
    }

    private void TickSunSlash()
    {
        phaseFrames++;
        ObserveAnimatorStates();

        if (phaseFrames < 80)
            return;

        if (!sunSlashSeen)
            throw new InvalidOperationException("Sun Slash Animator state was never observed at melee range.");

        Debug.Log("[Stage3WardenValidation] SUN SLASH PASS.");

        healthBeforeHit = health.currentHealth;
        health.TakeDamage(1f);
        phaseFrames = 0;
        phase = 5;
    }

    private void TickHitReaction()
    {
        phaseFrames++;
        ObserveAnimatorStates();

        if (phaseFrames < 45)
            return;

        if (!(health.currentHealth < healthBeforeHit) || health.IsDead || !warden.BattleStarted)
            throw new InvalidOperationException("Normal damage did not preserve live combat state.");
        if (!hitSeen)
            throw new InvalidOperationException("Being Hit Animator state was never observed after damage.");
        if (!capsule.enabled || !warden.CompareTag("Enemy"))
            throw new InvalidOperationException("Warden became untargetable after a normal hit.");

        Debug.Log(
            $"[Stage3WardenValidation] HIT PASS health={health.currentHealth:0.0}/{health.maxHealth:0.0}.");

        health.TakeDamage(health.maxHealth + 999f);
        phaseFrames = 0;
        phase = 6;
    }

    private void TickDeath()
    {
        phaseFrames++;
        ObserveAnimatorStates();

        if (phaseFrames < 12)
            return;

        bool pass = health.IsDead
            && warden.IsDead
            && !warden.BattleStarted
            && !agent.enabled
            && !capsule.enabled
            && warden.CompareTag("Untagged")
            && deathSeen;

        Debug.Log(
            $"[Stage3WardenValidation] DEATH RESULT healthDead={health.IsDead} controllerDead={warden.IsDead} " +
            $"battle={warden.BattleStarted} agentEnabled={agent.enabled} collider={capsule.enabled} " +
            $"tag={warden.tag} deathSeen={deathSeen} PASS={pass}");

        if (!pass)
            throw new InvalidOperationException("Warden death cleanup/animation validation failed.");

        Debug.Log(
            $"[Stage3WardenValidation] PASS chase+attacks+hit+death " +
            $"run={runSeen} rush={radiantRushSeen} slam={solarSlamSeen} slash={sunSlashSeen} hit={hitSeen} death={deathSeen}");
        Finish(true);
    }

    private void ObserveAnimatorStates()
    {
        if (animator == null || animator.layerCount == 0)
            return;

        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
        AnimatorStateInfo next = animator.IsInTransition(0)
            ? animator.GetNextAnimatorStateInfo(0)
            : default;

        runSeen |= IsState(current, next, "Run");
        radiantRushSeen |= IsState(current, next, "Radiant Rush");
        solarSlamSeen |= IsState(current, next, "Solar Slam");
        sunSlashSeen |= IsState(current, next, "Sun Slash");
        hitSeen |= IsState(current, next, "Being Hit");
        deathSeen |= IsState(current, next, "Death");
    }

    private static bool IsState(AnimatorStateInfo current, AnimatorStateInfo next, string stateName)
    {
        int hash = Animator.StringToHash(stateName);
        return current.shortNameHash == hash || next.shortNameHash == hash;
    }

    private static void ValidateAnimatorParameters(Animator target)
    {
        string[] required = { "Speed", "SunSlash", "RadiantRush", "SolarSlam", "BeingHit", "Death" };
        foreach (string parameterName in required)
        {
            bool found = false;
            foreach (AnimatorControllerParameter parameter in target.parameters)
            {
                if (parameter.name == parameterName)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
                throw new InvalidOperationException($"Animator parameter '{parameterName}' is missing.");
        }
    }

    private void ValidateSwordAndGrip()
    {
        SunboundWardenWeaponGrip grip = warden.GetComponent<SunboundWardenWeaponGrip>();
        if (grip == null)
            throw new InvalidOperationException("SunboundWardenWeaponGrip is missing.");

        Transform sword = FindChildRecursive(warden.transform, "SunboundWarden_Sword");
        if (sword == null || sword.parent == null
            || sword.parent.name.IndexOf("RightHand", StringComparison.OrdinalIgnoreCase) < 0)
        {
            throw new InvalidOperationException(
                $"Sword/right-hand parenting invalid. sword={sword != null} parent={(sword != null && sword.parent != null ? sword.parent.name : "null")}");
        }

        Debug.Log(
            $"[Stage3WardenValidation] SWORD PASS sword='{sword.name}' parent='{sword.parent.name}' grip='{grip.GetType().Name}'.");
    }

    private void MovePlayerToCombatDistance(float desiredDistance)
    {
        Vector3 origin = agent.transform.position;
        Vector3 baseDirection = player.position - origin;
        baseDirection.y = 0f;
        if (baseDirection.sqrMagnitude < 0.001f)
            baseDirection = Vector3.forward;
        baseDirection.Normalize();

        for (int i = 0; i < 24; i++)
        {
            float angle = i * 15f;
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * baseDirection;
            Vector3 requested = origin + direction * desiredDistance;
            if (!NavMesh.SamplePosition(requested, out NavMeshHit hit, 2f, agent.areaMask))
                continue;

            float distance = FlatDistance(origin, hit.position);
            if (Mathf.Abs(distance - desiredDistance) > 2f)
                continue;

            TeleportPlayer(hit.position);
            Physics.SyncTransforms();
            Debug.Log(
                $"[Stage3WardenValidation] Player repositioned for combat desired={desiredDistance:0.00} " +
                $"actual={distance:0.00} pos={hit.position:F2}.");
            return;
        }

        throw new InvalidOperationException($"Could not place player near {desiredDistance:0.00}m on Stage3 NavMesh.");
    }

    private void TeleportPlayer(Vector3 position)
    {
        CharacterController controller = player.GetComponent<CharacterController>()
            ?? player.GetComponentInParent<CharacterController>();
        bool wasEnabled = controller != null && controller.enabled;
        if (controller != null)
            controller.enabled = false;

        Transform root = controller != null ? controller.transform : player;
        root.position = position;

        if (controller != null)
            controller.enabled = wasEnabled;
    }

    private static bool TryFindReachableTarget(NavMeshAgent source, float minimumDistance, out Vector3 target)
    {
        Vector3 origin = source.transform.position;
        float[] radii = { 11f, 11.5f, 12f, 12.5f, 13f, 14f, 15f, 18f, 20f, 24f, 30f };

        for (int angleIndex = 0; angleIndex < 36; angleIndex++)
        {
            float radians = angleIndex * Mathf.PI * 2f / 36f;
            Vector3 direction = new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians));

            foreach (float radius in radii)
            {
                Vector3 requested = origin + direction * radius;
                if (!NavMesh.SamplePosition(requested, out NavMeshHit hit, 4f, source.areaMask))
                    continue;
                if (FlatDistance(origin, hit.position) < minimumDistance)
                    continue;

                NavMeshPath path = new NavMeshPath();
                if (!source.CalculatePath(hit.position, path))
                    continue;
                if (path.status != NavMeshPathStatus.PathComplete || path.corners.Length < 2)
                    continue;

                target = hit.position;
                return true;
            }
        }

        target = default;
        return false;
    }

    private static float FindMaxReachableDistance(NavMeshAgent source, out Vector3 farthestPoint)
    {
        Vector3 origin = source.transform.position;
        float bestDistance = 0f;
        farthestPoint = origin;

        for (float radius = 2f; radius <= 40f; radius += 1f)
        {
            for (int angleIndex = 0; angleIndex < 72; angleIndex++)
            {
                float radians = angleIndex * Mathf.PI * 2f / 72f;
                Vector3 direction = new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians));
                Vector3 requested = origin + direction * radius;

                if (!NavMesh.SamplePosition(requested, out NavMeshHit hit, 1.5f, source.areaMask))
                    continue;

                float distance = FlatDistance(origin, hit.position);
                if (distance <= bestDistance)
                    continue;

                NavMeshPath path = new NavMeshPath();
                if (!source.CalculatePath(hit.position, path)
                    || path.status != NavMeshPathStatus.PathComplete
                    || path.corners.Length < 2)
                {
                    continue;
                }

                bestDistance = distance;
                farthestPoint = hit.position;
            }
        }

        return bestDistance;
    }

    private static T FindSceneComponent<T>() where T : Component
    {
        Scene scene = SceneManager.GetActiveScene();
        foreach (T component in Resources.FindObjectsOfTypeAll<T>())
        {
            if (component != null && component.gameObject.scene == scene)
                return component;
        }

        return null;
    }

    private static T[] FindSceneComponents<T>() where T : Component
    {
        Scene scene = SceneManager.GetActiveScene();
        T[] all = Resources.FindObjectsOfTypeAll<T>();
        int count = 0;
        foreach (T component in all)
            if (component != null && component.gameObject.scene == scene)
                count++;

        T[] result = new T[count];
        int index = 0;
        foreach (T component in all)
        {
            if (component != null && component.gameObject.scene == scene)
                result[index++] = component;
        }

        return result;
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
                return child;
        }
        return null;
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void Finish(bool passed)
    {
#if UNITY_EDITOR
        SessionState.SetBool(DoneKey, true);
        SessionState.SetBool(PassKey, passed);
        EditorApplication.isPlaying = false;
#endif
        phase = 99;
    }
}
