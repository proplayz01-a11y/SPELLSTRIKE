using UnityEngine;
using UnityEngine.SceneManagement;

public class AdventureButtonHandler : MonoBehaviour
{
    private const string PrologueCompletionKey = "SpellStrike_PrologueCompleted";
    private const string TutorialCompletionKey = "SpellStrike_TutorialCompleted";

    private const string StorySceneName = "StoryScene";
    private const string TutorialSceneName = "TutorialScene";
    private const string StageSelectSceneName = "StageSelectScene";

    private bool adventureNavigationInProgress;

    public void LoadAdventure()
    {
        if (adventureNavigationInProgress)
            return;

        bool tutorialCompleted = IsTutorialCompleted();
        bool prologueCompleted = PlayerPrefs.GetInt(PrologueCompletionKey, 0) == 1;
        // Consultation/replay shortcut: hold Shift when clicking Adventure to
        // choose Cinematic or Comic again without clearing saved progression.
        bool replayPrologue = Input.GetKey(KeyCode.LeftShift) ||
                              Input.GetKey(KeyCode.RightShift);

        // Tutorial completion is the final first-run onboarding gate. Treat it
        // as authoritative for legacy saves created before the Prologue existed.
        // First-run presentation choice: animated Unity Timeline or rejected-art comic.
        // Original StoryScene remains available as a safe fallback if the comic
        // resource pack is absent; no existing tutorial/stage completion logic changes.
        if ((replayPrologue || (!tutorialCompleted && !prologueCompleted)) &&
            Resources.Load<TextAsset>("ComicMode/manifest") != null)
        {
            Spellstrike.ComicMode.ComicModeAdventureRouter.Open();
            return;
        }

        string destination = tutorialCompleted
            ? StageSelectSceneName
            : prologueCompleted
                ? TutorialSceneName
                : StorySceneName;

        adventureNavigationInProgress = true;

        if (!Application.CanStreamedLevelBeLoaded(destination))
        {
            Debug.LogError(
                $"[MainMenu] Adventure cannot load '{destination}'. Add it to the active Build Profile or shared scene list.",
                this);
            adventureNavigationInProgress = false;
            return;
        }

        Time.timeScale = 1f;
        Debug.Log(
            $"[MainMenu] Adventure route -> {destination} | " +
            $"Prologue complete: {prologueCompleted} | Tutorial complete: {tutorialCompleted}",
            this);
        SceneManager.LoadScene(destination);
    }

    private static bool IsTutorialCompleted()
    {
        if (PlayerPrefs.GetInt(TutorialCompletionKey, 0) == 1)
            return true;

        GameDatabaseManager database = GameDatabaseManager.Instance;
        return database != null
               && database.Data != null
               && database.Data.tutorialCompleted;
    }

    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("[MainMenu] Cannot load an empty scene name.");
            return;
        }

        Debug.Log($"[MainMenu] Loading scene: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }
}
