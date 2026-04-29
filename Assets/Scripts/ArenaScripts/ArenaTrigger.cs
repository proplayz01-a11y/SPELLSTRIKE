using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.AI;

public class ArenaTrigger : MonoBehaviour
{
    [Header("Enemy")]
    [SerializeField] private GameObject brambleSprite;
    [SerializeField] private Transform brambleSpriteSpawnPoint;
    [SerializeField] private Transform arenaCenter;
    [SerializeField] private float arenaRadius = 10f;
    [SerializeField] private float moveSpeed = 500f;
    [SerializeField] private float groundRayStartHeight = 50f;
    [SerializeField] private float groundRayDistance = 200f;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private bool debugBrambleSpeed = false;
    [SerializeField] private float debugLogInterval = 0.25f;

    [Header("Intro Animation States")]
    [SerializeField] private string introWalkState = "Walk";
    [SerializeField] private string introScreamState = "Scream";
    [SerializeField] private string introClawPointState = "ClawPoint";
    [SerializeField] private string introIdleState = "Idle";
    [SerializeField] private float introTransitionDuration = 0.08f;

    [Header("Dialogue")]
    [SerializeField] private GameObject dialogueCanvas;
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI enemyNameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private float dialogueShowDelayFromClawPoint = 0f;
    [SerializeField] private float dialogueDuration = 3f;

    [Header("Game UI")]
    [SerializeField] private GameObject healthBar;
    [SerializeField] private GameObject enemyHealthBar;
    [SerializeField] private GameObject tilePoolPanel;
    [SerializeField] private GameObject wordBarPanel;

    [Header("Phase 3 - Death Outcomes")]
    [SerializeField] private GameObject arenaBoundaryObject;
    [SerializeField] private Collider arenaBoundaryCollider;
    [SerializeField] private GameObject itemDropPrefab;
    [SerializeField] private Transform itemDropSpawnPoint;
    [SerializeField] private GameObject node2DirectionIndicator;
    [SerializeField] private GameObject node1CompletedMarker;
    [SerializeField] private bool saveNode1Completion = true;
    [SerializeField] private int node1StageIndex = 1;

    [Header("Player")]
    [SerializeField] private Transform player;
    [SerializeField] private MovementofPlayer playerMovement;

    private Animator brambleAnimator;
    private UnityEngine.AI.NavMeshAgent brambleAgent;
    private BrambleSpriteController brambleController;
    private Rigidbody brambleRb;
    private bool hadGravity;
    private bool wasKinematic;
    private bool triggered = false;
    private bool battleStarted = false;
    private bool deathFlowHandled = false;
    private bool startBattleInvoked = false;  // NEW: Prevent duplicate StartBattle() calls
    private float speedDebugTimer = 0f;

    void Start()
    {
        if (brambleSprite != null) brambleSprite.SetActive(false);
        if (dialogueCanvas != null) dialogueCanvas.SetActive(false);
        var renderers = GameObject.Find("level1_REVAMPED").GetComponentsInChildren<Renderer>();
        Bounds bounds = renderers[0].bounds;
        foreach (var r in renderers)
            bounds.Encapsulate(r.bounds);
        Debug.Log("Size: " + bounds.size + " Center: " + bounds.center);
        if (node2DirectionIndicator != null) node2DirectionIndicator.SetActive(false);
    }

    void Update()
    {
        if (!battleStarted || player == null || arenaCenter == null) return;

        // Circular boundary check
        float distance = Vector3.Distance(player.position, arenaCenter.position);
        if (distance > arenaRadius)
        {
            Vector3 direction = (player.position - arenaCenter.position).normalized;
            Vector3 newPos = arenaCenter.position + direction * arenaRadius;
            newPos.y = player.position.y;
            player.position = newPos;
        }

        if (debugBrambleSpeed && brambleAgent != null)
        {
            speedDebugTimer += Time.deltaTime;
            if (speedDebugTimer >= Mathf.Max(0.05f, debugLogInterval))
            {
                speedDebugTimer = 0f;
                Debug.Log($"[BrambleSpeed][Battle] agent.velocity={brambleAgent.velocity.magnitude:F2} | agent.speed={brambleAgent.speed:F2}");
            }
        }
    }

    void OnDrawGizmos()
    {
        if (arenaCenter == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(arenaCenter.position, arenaRadius);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !triggered)
        {
            triggered = true;
            StartCoroutine(IntroSequence());
        }
    }

    IEnumerator IntroSequence()
    {
        if (brambleSprite == null || brambleSpriteSpawnPoint == null || arenaCenter == null || playerMovement == null)
        {
            Debug.LogError("[ArenaTrigger] Missing required references. Please assign enemy, spawn point, arena center, and player movement.");
            yield break;
        }

        // Disable game UI
        if (healthBar != null) healthBar.SetActive(false);
        if (enemyHealthBar != null) enemyHealthBar.SetActive(false);
        if (tilePoolPanel != null) tilePoolPanel.SetActive(false);
        if (wordBarPanel != null) wordBarPanel.SetActive(false);

        // Disable player movement
        playerMovement.enabled = false;

        // Spawn Bramble Sprite at spawn point
        brambleSprite.SetActive(true);
        brambleSprite.transform.position = GetGroundedPosition(brambleSpriteSpawnPoint.position);
        brambleAnimator = brambleSprite.GetComponent<Animator>();
        brambleAgent = brambleSprite.GetComponent<UnityEngine.AI.NavMeshAgent>();
        brambleController = brambleSprite.GetComponent<BrambleSpriteController>();
        brambleRb = brambleSprite.GetComponent<Rigidbody>();
        if (brambleController != null)
        {
            brambleController.enabled = false; // Keep AI from interfering with intro movement
            brambleController.OnDeathStarted -= OnBrambleDeathStarted;
            brambleController.OnDeathStarted += OnBrambleDeathStarted;
        }
        if (brambleAgent != null)
        {
            brambleAgent.isStopped = true;
            brambleAgent.ResetPath();
            brambleAgent.updatePosition = false;
        }
        if (brambleRb != null)
        {
            hadGravity = brambleRb.useGravity;
            wasKinematic = brambleRb.isKinematic;
            brambleRb.useGravity = false;
            brambleRb.isKinematic = true;
        }
        // Walk to arena center (force state so transitions/exit time won't block intro)
        PlayIntroState(introWalkState);
        Vector3 arenaGroundPos = GetGroundedPosition(arenaCenter.position);
        while (Vector3.Distance(new Vector3(brambleSprite.transform.position.x, 0f, brambleSprite.transform.position.z), new Vector3(arenaGroundPos.x, 0f, arenaGroundPos.z)) > 0.5f)
        {
            Vector3 current = brambleSprite.transform.position;
            Vector3 currentXZ = new Vector3(current.x, 0f, current.z);
            Vector3 targetXZ = new Vector3(arenaGroundPos.x, 0f, arenaGroundPos.z);
            Vector3 nextXZ = Vector3.MoveTowards(currentXZ, targetXZ, moveSpeed * Time.deltaTime);
            brambleSprite.transform.position = GetGroundedPosition(new Vector3(nextXZ.x, current.y, nextXZ.z));

            if (debugBrambleSpeed)
            {
                speedDebugTimer += Time.deltaTime;
                if (speedDebugTimer >= Mathf.Max(0.05f, debugLogInterval))
                {
                    speedDebugTimer = 0f;
                    float introSpeed = Vector3.Distance(current, brambleSprite.transform.position) / Mathf.Max(Time.deltaTime, 0.0001f);
                    float remaining = Vector3.Distance(brambleSprite.transform.position, arenaGroundPos);
                    Debug.Log($"[BrambleSpeed][Intro] actual={introSpeed:F2} u/s | moveSpeed={moveSpeed:F2} | remaining={remaining:F2}");
                }
            }

            Vector3 lookTarget = arenaCenter.position;
            lookTarget.y = brambleSprite.transform.position.y;
            brambleSprite.transform.LookAt(lookTarget);

            yield return null;
        }
        brambleSprite.transform.position = arenaGroundPos;
        if (brambleAnimator != null) brambleAnimator.SetFloat("Speed", 0f);

        // Scream animation
        PlayIntroState(introScreamState);
        yield return new WaitForSeconds(GetClipLength(introScreamState, 2f));

        // Claw point animation + dialogue timing synced to the same phase.
        float clawLength = GetClipLength(introClawPointState, 1.5f);
        PlayIntroState(introClawPointState);

        float dialogueDelay = Mathf.Max(0f, dialogueShowDelayFromClawPoint);
        if (dialogueDelay > 0f)
            yield return new WaitForSeconds(dialogueDelay);

        ShowBrambleDialogue();

        float remainingClawAfterDialogue = Mathf.Max(0f, clawLength - dialogueDelay);
        float visibleDialogueDuration = Mathf.Max(0.1f, dialogueDuration);
        yield return new WaitForSeconds(Mathf.Max(remainingClawAfterDialogue, visibleDialogueDuration));

        HideBrambleDialogue();

        // Re-enable game UI
        if (healthBar != null) healthBar.SetActive(true);
        if (tilePoolPanel != null) tilePoolPanel.SetActive(true);
        if (enemyHealthBar != null) enemyHealthBar.SetActive(true);
        if (wordBarPanel != null) wordBarPanel.SetActive(true);

        // Re-enable player movement
        playerMovement.enabled = true;

        // Re-enable enemy nav agent and sync it to current position.
        if (brambleAgent != null)
        {
            brambleAgent.Warp(brambleSprite.transform.position);
            brambleAgent.ResetPath();
            brambleAgent.updatePosition = true;
            brambleAgent.isStopped = false;
        }
        if (brambleRb != null)
        {
            brambleRb.useGravity = hadGravity;
            brambleRb.isKinematic = wasKinematic;
        }

        // Battle starts
        battleStarted = true;
        if (TileSpawner.Instance != null)
        {
            TileSpawner.Instance.ConfigureArenaBounds(arenaCenter, arenaRadius);
        }

        PlayIntroState(introIdleState);
        if (brambleController != null)
        {
            brambleController.enabled = true;
            brambleController.StartBattle();
        }
    }

    private void OnDestroy()
    {
        if (brambleController != null)
            brambleController.OnDeathStarted -= OnBrambleDeathStarted;
    }

    private void OnBrambleDeathStarted(BrambleSpriteController deadBramble)
    {
        if (deathFlowHandled) return;
        deathFlowHandled = true;
        StartCoroutine(HandlePhase3DeathFlow(deadBramble));
    }

    private IEnumerator HandlePhase3DeathFlow(BrambleSpriteController deadBramble)
    {
        battleStarted = false;

        if (arenaBoundaryObject != null)
            arenaBoundaryObject.SetActive(false);

        if (arenaBoundaryCollider != null)
            arenaBoundaryCollider.enabled = false;
        else
        {
            Collider ownCollider = GetComponent<Collider>();
            if (ownCollider != null) ownCollider.enabled = false;
        }

        if (itemDropPrefab != null)
        {
            Vector3 dropPos = itemDropSpawnPoint != null
                ? itemDropSpawnPoint.position
                : (deadBramble != null ? deadBramble.transform.position : transform.position);
            Instantiate(itemDropPrefab, dropPos, Quaternion.identity);
        }

        if (node2DirectionIndicator != null)
            node2DirectionIndicator.SetActive(true);
        if (node1CompletedMarker != null)
            node1CompletedMarker.SetActive(true);

        if (saveNode1Completion && GameDatabaseManager.Instance != null)
        {
            GameDatabaseManager.Instance.SaveStageProgress(node1StageIndex, true);
            GameDatabaseManager.Instance.FlushSave();
        }

        yield return null;
    }

    private Vector3 GetGroundedPosition(Vector3 candidate)
    {
        if (NavMesh.SamplePosition(candidate, out NavMeshHit navHit, 10f, NavMesh.AllAreas))
        {
            return navHit.position;
        }

        Vector3 rayOrigin = new Vector3(candidate.x, candidate.y + groundRayStartHeight, candidate.z);
        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, groundRayDistance, groundMask, QueryTriggerInteraction.Ignore);
        if (hits.Length > 0)
        {
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                Transform hitTransform = hits[i].transform;
                if (brambleSprite != null && hitTransform != null && hitTransform.IsChildOf(brambleSprite.transform))
                    continue;
                return hits[i].point;
            }
        }

        return candidate;
    }

    private void ShowBrambleDialogue()
    {
        if (dialogueCanvas != null) dialogueCanvas.SetActive(true);
        if (enemyNameText != null) enemyNameText.text = "Bramble Sprite";
        if (dialogueText != null) dialogueText.text = "Who are you trying to get on my village?";
    }

    private void HideBrambleDialogue()
    {
        if (dialogueCanvas != null) dialogueCanvas.SetActive(false);
    }

    private void PlayIntroState(string stateName)
    {
        if (brambleAnimator == null || string.IsNullOrWhiteSpace(stateName)) return;
        brambleAnimator.CrossFadeInFixedTime(stateName, introTransitionDuration);
    }

    private float GetClipLength(string clipName, float fallbackLength)
    {
        if (brambleAnimator == null || brambleAnimator.runtimeAnimatorController == null || string.IsNullOrWhiteSpace(clipName))
            return fallbackLength;

        AnimationClip[] clips = brambleAnimator.runtimeAnimatorController.animationClips;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null && clips[i].name == clipName)
                return clips[i].length;
        }

        return fallbackLength;
    }
}