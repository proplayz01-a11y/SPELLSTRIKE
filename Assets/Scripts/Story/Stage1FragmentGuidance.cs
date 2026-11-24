using UnityEngine;

/// <summary>
/// Reuses TutorialDirectionalArrowTrail's original animated gold trail in
/// Stage 1. The Spirit's completed meeting unlocks the route; completed
/// nodes redirect it toward the next fragment without a minimap.
/// </summary>
[DisallowMultipleComponent]
public sealed class Stage1FragmentGuidance : MonoBehaviour
{
    [SerializeField] private TutorialDirectionalArrowTrail arrowTrail;
    [SerializeField] private Stage1ProgressionManager progression;
    [SerializeField] private SpiritRemnantEncounter spirit;
    [SerializeField] private Stage1FirstFragmentReveal firstFragmentReveal;
    [SerializeField] private Transform firstArena;
    [SerializeField] private Transform secondFragment;
    [SerializeField] private Transform thirdFragment;
    [SerializeField] private Transform castleApproach;

    private Transform player;
    private ObjectiveManager objectives;
    private Node1Controller firstNode;
    private Stage1BossController bossNode;
    private Transform currentTarget;
    private bool wasVisible;
    private float nextRefresh;

    public Transform CurrentTarget => currentTarget;
    public bool IsShowing => wasVisible;

    private void Awake()
    {
        arrowTrail ??= GetComponent<TutorialDirectionalArrowTrail>();
        if (arrowTrail != null)
        {
            arrowTrail.ConfigureStageVisibility(5f, 120);
            arrowTrail.SetVisible(false);
        }
    }

    private void OnEnable()
    {
        nextRefresh = 0f;
    }

    private void Start()
    {
        objectives = ObjectiveManager.Resolve(gameObject.scene);
        if (objectives != null) objectives.ProgressChanged += OnObjectiveChanged;
        firstNode = FindFirstObjectByType<Node1Controller>(FindObjectsInactive.Include);
        bossNode = FindFirstObjectByType<Stage1BossController>(FindObjectsInactive.Include);
    }

    private void OnObjectiveChanged()
    {
        // Re-evaluate after the producer finishes unlocking its pickup.
        nextRefresh = 0f;
    }

    private void LateUpdate()
    {
        if (Time.unscaledTime < nextRefresh)
            return;
        nextRefresh = Time.unscaledTime + 0.16f;
        if (player == null)
        {
            var actor = GameObject.FindGameObjectWithTag("Player");
            player = actor != null ? actor.transform : null;
        }
        if (objectives == null) objectives = ObjectiveManager.Resolve(gameObject.scene);
        var actorRoot = PersistentPlayerRoot.Instance;
        var hud = PersistentHUDRoot.Instance;
        bool eligible = player != null && objectives != null && actorRoot != null &&
                        actorRoot.Health != null && actorRoot.Health.currentHealth > 0 &&
                        actorRoot.Movement != null && actorRoot.Movement.isActiveAndEnabled &&
                        !actorRoot.Movement.IsMovementDisabled &&
                        !objectives.HasActiveEncounter &&
                        Time.timeScale > 0f && !GoalsPanelUI.IsJournalOpen &&
                        (hud == null || hud.GameplayHealthVisible) &&
                        (spirit == null || !spirit.IsTalking) &&
                        (firstFragmentReveal == null || !firstFragmentReveal.IsRunning) &&
                        !SpiritRemnantCinematic.IsCinematicActive &&
                        !Stage1FirstFragmentReveal.IsCinematicActive &&
                        !Stage1PostJesterEchoCall.IsCinematicActive &&
                        !Stage1HollowKnightConfrontation.IsCinematicActive;
        Transform goal = eligible ? ResolveCurrentTarget() : null;
        if (goal != null && player != null)
        {
            Vector3 delta = goal.position - player.position;
            delta.y = 0f;
            // Stop at interaction distance for NPCs/pickups; keep the original
            // exploration arrival radius for dormant encounter waypoints.
            float arrival = objectives.NeedsSpiritGuidance
                ? (spirit != null ? spirit.GuidanceArrivalRadius : 11f)
                : objectives.IsFragmentAwaitingPickup(objectives.NextFragmentNode) ? 7f : 52f;
            if (delta.sqrMagnitude < arrival * arrival)
                goal = null;
        }
        if (goal != currentTarget)
        {
            currentTarget = goal;
            arrowTrail?.SetEndpoints(player, currentTarget);
        }
        bool show = goal != null;
        if (wasVisible != show)
        {
            wasVisible = show;
            arrowTrail?.SetVisible(show);
        }
    }

    private Transform ResolveCurrentTarget()
    {
        if (objectives == null) return null;
        if (objectives.NeedsSpiritGuidance)
            return spirit != null ? spirit.GuidanceTarget : null;
        int node = objectives.NextFragmentNode;
        bool pickup = objectives.IsFragmentAwaitingPickup(node);
        switch (node)
        {
            case 1:
                return pickup ? ActiveTarget(firstNode != null && firstNode.fragmentPickup != null
                    ? firstNode.fragmentPickup.transform : null) : firstArena;
            case 2: return ActiveTarget(secondFragment);
            case 3: return ActiveTarget(thirdFragment);
            case 4:
                // Use castle entrance before combat, never the hidden crystal
                // inside the knight. After defeat use the unlocked pickup.
                return pickup ? ActiveTarget(bossNode != null && bossNode.bossFragment != null
                    ? bossNode.bossFragment.transform : null) : castleApproach;
            default: return null;
        }
    }

    private static Transform ActiveTarget(Transform target) =>
        target != null && target.gameObject.activeInHierarchy ? target : null;

    public void HideForReveal()
    {
        arrowTrail?.SetVisible(false);
        wasVisible = false;
        currentTarget = null;
        arrowTrail?.SetEndpoints(player, null);
    }

    private void OnDisable()
    {
        arrowTrail?.SetVisible(false);
        wasVisible = false;
    }

    private void OnDestroy()
    {
        if (objectives != null) objectives.ProgressChanged -= OnObjectiveChanged;
    }
}
