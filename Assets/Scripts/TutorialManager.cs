using UnityEngine;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    public GameObject tutorialPanel;
    public TMP_Text tutorialText;
    public string[] tutorialSteps;

    private int currentStep = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        bool completed = PlayerPrefs.GetInt("SpellStrike_TutorialCompleted", 0) == 1;
        if (completed)
        {
            if (tutorialPanel != null)
                tutorialPanel.SetActive(false);
            return;
        }

        StartTutorial();
    }

    public void StartTutorial()
    {
        currentStep = 0;
        ShowCurrentStep();
    }

    public void AdvanceStep()
    {
        currentStep++;
        if (currentStep >= tutorialSteps.Length)
        {
            CompleteTutorial();
            return;
        }

        ShowCurrentStep();
    }

    public void ShowCurrentStep()
    {
        if (tutorialPanel == null || tutorialText == null)
            return;

        if (currentStep < 0 || currentStep >= tutorialSteps.Length)
            return;

        tutorialText.text = tutorialSteps[currentStep];
        tutorialPanel.SetActive(true);
    }

    public void CompleteTutorial()
    {
        PlayerPrefs.SetInt("SpellStrike_TutorialCompleted", 1);
        PlayerPrefs.Save();
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);
        Debug.Log("Tutorial completed.");
        if (GameDatabaseManager.Instance != null)
            GameDatabaseManager.Instance.SetTutorialCompleted(true);
    }
}
