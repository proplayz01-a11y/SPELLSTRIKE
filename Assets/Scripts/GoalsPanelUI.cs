using TMPro;
using UnityEngine;

public class GoalsPanelUI : MonoBehaviour
{
    [Header("Canvas References")]
    public GameObject objectivesCanvas;

    [Header("Goal Texts (TMP)")]
    public TextMeshProUGUI goal1Text;
    public TextMeshProUGUI goal2Text;
    public TextMeshProUGUI goal3Text;

    [Header("Goal Colors")]
    public Color defaultGoalColor = Color.white;
    public Color completedGoalColor = Color.green;

    [Header("Objective Manager")]
    public ObjectiveManager objectiveManager;

    private const int Goal1Target = 4;
    private const int Goal3Target = 3;

    private int currentFragments = 0;
    private int currentLongWords = 0;

    private bool goal1Completed = false;
    private bool goal2Completed = false;
    private bool goal3Completed = false;

    private void Start()
    {
        if (objectivesCanvas != null)
            objectivesCanvas.SetActive(false);

        if (objectiveManager == null)
            objectiveManager = FindObjectOfType<ObjectiveManager>();

        RefreshGoalTexts();
        EvaluateAllGoalsCompleted();
    }

    public void ToggleGoalsPanel()
    {
        if (objectivesCanvas == null)
        {
            Debug.LogWarning("[GoalsPanelUI] ObjectivesCanvas reference is missing.");
            return;
        }

        bool nextState = !objectivesCanvas.activeSelf;
        objectivesCanvas.SetActive(nextState);
        Debug.Log($"[GoalsPanelUI] Objectives panel toggled: {(nextState ? "Opened" : "Closed")}");
    }

    public void CloseGoalsPanel()
    {
        if (objectivesCanvas == null)
        {
            Debug.LogWarning("[GoalsPanelUI] ObjectivesCanvas reference is missing.");
            return;
        }

        objectivesCanvas.SetActive(false);
        Debug.Log("[GoalsPanelUI] Objectives panel closed.");
    }

    public void UpdateFragmentCount(int current)
    {
        currentFragments = Mathf.Clamp(current, 0, Goal1Target);
        goal1Completed = currentFragments >= Goal1Target;

        if (goal1Text != null)
        {
            if (goal1Completed)
                goal1Text.text = "1. Collect Fragments (4/4) (Completed)";
            else
                goal1Text.text = $"1. Collect Fragments ({currentFragments}/4)";

            goal1Text.color = goal1Completed ? completedGoalColor : defaultGoalColor;
        }

        Debug.Log($"[GoalsPanelUI] Goal 1 updated: {currentFragments}/4");
        EvaluateAllGoalsCompleted();
    }

    public void IncrementFragmentCount(int amount = 1)
    {
        UpdateFragmentCount(currentFragments + Mathf.Max(0, amount));
    }

    public void CompleteGoal2()
    {
        goal2Completed = true;

        if (goal2Text != null)
        {
            goal2Text.text = "2. Find the Spirit Remnant (Completed)";
            goal2Text.color = completedGoalColor;
        }

        Debug.Log("[GoalsPanelUI] Goal 2 completed.");
        EvaluateAllGoalsCompleted();
    }

    public void UpdateWordCount(int current)
    {
        currentLongWords = Mathf.Clamp(current, 0, Goal3Target);
        goal3Completed = currentLongWords >= Goal3Target;

        if (goal3Text != null)
        {
            if (goal3Completed)
                goal3Text.text = "3. Spell 6+ letter words (3/3) (Completed)";
            else
                goal3Text.text = $"3. Spell 6+ letter words ({currentLongWords}/3)";

            goal3Text.color = goal3Completed ? completedGoalColor : defaultGoalColor;
        }

        Debug.Log($"[GoalsPanelUI] Goal 3 updated: {currentLongWords}/3");
        EvaluateAllGoalsCompleted();
    }

    public void IncrementWordCount(int amount = 1)
    {
        UpdateWordCount(currentLongWords + Mathf.Max(0, amount));
    }

    private void RefreshGoalTexts()
    {
        if (goal1Text != null)
        {
            goal1Text.text = $"1. Collect Fragments ({currentFragments}/4)";
            goal1Text.color = goal1Completed ? completedGoalColor : defaultGoalColor;
        }

        if (goal2Text != null)
        {
            goal2Text.text = goal2Completed
                ? "2. Find the Spirit Remnant (Completed)"
                : "2. Find the Spirit Remnant";
            goal2Text.color = goal2Completed ? completedGoalColor : defaultGoalColor;
        }

        if (goal3Text != null)
        {
            goal3Text.text = $"3. Spell 6+ letter words ({currentLongWords}/3)";
            goal3Text.color = goal3Completed ? completedGoalColor : defaultGoalColor;
        }
    }

    private void EvaluateAllGoalsCompleted()
    {
        bool allDone = goal1Completed && goal2Completed && goal3Completed;

        if (objectiveManager != null)
        {
            objectiveManager.allGoalsCompleted = allDone;
            if (allDone)
                Debug.Log("[GoalsPanelUI] All goals completed. ObjectiveManager flag set to true.");
        }
        else
        {
            Debug.LogWarning("[GoalsPanelUI] ObjectiveManager reference missing. Cannot set allGoalsCompleted.");
        }
    }
}
