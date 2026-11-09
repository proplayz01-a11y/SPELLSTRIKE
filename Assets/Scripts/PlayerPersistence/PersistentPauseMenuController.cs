using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Owns the gameplay pause flow on the persistent gameplay HUD.
/// The Main Menu OptionsPanel is reused as a child panel; its existing
/// MenuV2VolumeSetting continues to own the shared SpellStrike_MainVolume key.
/// </summary>
[DisallowMultipleComponent]
public sealed class PersistentPauseMenuController : MonoBehaviour
{
    private const string MainMenuSceneName = "Main Menu Scene";

    private Button pauseButton;
    private Button resumeButton;
    private Button optionsButton;
    private Button mainMenuButton;
    private Button cancelReturnButton;
    private Button confirmReturnButton;

    private GameObject pausePanel;
    private GameObject optionsPanel;
    private GameObject returnConfirmationPanel;

    private bool isPaused;
    private bool isLeavingGameplay;
    private float timeScaleBeforePause = 1f;
    private bool audioWasPaused;

    public bool IsPaused => isPaused;
    public bool IsLeavingGameplay => isLeavingGameplay;

    private void Awake()
    {
        ResolveReferences();
        SetPanelsVisible(false, false, false);
    }

    private void OnEnable()
    {
        ResolveReferences();
        BindButtons();
    }

    private void OnDisable()
    {
        UnbindButtons();
    }

private void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= HandleMainMenuSceneLoaded;
        UnbindButtons();

        // A stale duplicate HUD can be destroyed during a scene handoff. Only
        // the canonical HUD owner may restore global pause state; otherwise a
        // duplicate's stale isPaused flag could resume the real gameplay session.
        PersistentHUDRoot owner = GetComponent<PersistentHUDRoot>();
        bool ownsCanonicalHud = PersistentHUDRoot.Instance == null ||
            PersistentHUDRoot.Instance == owner;

        // Exiting Play Mode or unloading the canonical owner must never leave
        // the editor/game stuck at zero time scale.
        if (isPaused && !isLeavingGameplay && ownsCanonicalHud)
        {
            Time.timeScale = timeScaleBeforePause;
            AudioListener.pause = audioWasPaused;
            isPaused = false;
        }
    }

    private void ResolveReferences()
    {
        Transform canvas = transform.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("[PersistentPauseMenu] The persistent HUD Canvas was not found.", this);
            return;
        }

        pauseButton = FindButton(canvas, "PauseButton");
        pausePanel = FindObject(canvas, "PauseMenuPanel");
        optionsPanel = FindObject(canvas, "GameplayOptionsPanel");
        returnConfirmationPanel = FindObject(canvas, "ReturnToMainMenuConfirmationPanel");

        resumeButton = FindButton(canvas, "PauseMenuPanel/Panel/ResumeButton");
        optionsButton = FindButton(canvas, "PauseMenuPanel/Panel/OptionsButton");
        mainMenuButton = FindButton(canvas, "PauseMenuPanel/Panel/MainMenuButton");
        cancelReturnButton = FindButton(canvas, "ReturnToMainMenuConfirmationPanel/Panel/CancelButton");
        confirmReturnButton = FindButton(canvas, "ReturnToMainMenuConfirmationPanel/Panel/ConfirmButton");
    }

    private static GameObject FindObject(Transform root, string path)
    {
        Transform found = root.Find(path);
        return found != null ? found.gameObject : null;
    }

    private static Button FindButton(Transform root, string path)
    {
        Transform found = root.Find(path);
        return found != null ? found.GetComponent<Button>() : null;
    }

    private void BindButtons()
    {
        Bind(pauseButton, OpenPauseMenu);
        Bind(resumeButton, ResumeGame);
        Bind(optionsButton, OpenOptions);
        Bind(mainMenuButton, ShowReturnConfirmation);
        Bind(cancelReturnButton, CancelReturnToMenu);
        Bind(confirmReturnButton, ConfirmReturnToMainMenu);
    }

    private void UnbindButtons()
    {
        Unbind(pauseButton, OpenPauseMenu);
        Unbind(resumeButton, ResumeGame);
        Unbind(optionsButton, OpenOptions);
        Unbind(mainMenuButton, ShowReturnConfirmation);
        Unbind(cancelReturnButton, CancelReturnToMenu);
        Unbind(confirmReturnButton, ConfirmReturnToMainMenu);
    }

    private static void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private static void Unbind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
            button.onClick.RemoveListener(action);
    }

    private void SetPanelsVisible(bool pause, bool options, bool confirmReturn)
    {
        if (pausePanel != null)
            pausePanel.SetActive(pause);
        if (optionsPanel != null)
            optionsPanel.SetActive(options);
        if (returnConfirmationPanel != null)
            returnConfirmationPanel.SetActive(confirmReturn);
    }

    public void OpenPauseMenu()
    {
        if (isPaused || isLeavingGameplay)
            return;

        if (pausePanel == null)
        {
            ResolveReferences();
            if (pausePanel == null)
            {
                Debug.LogError("[PersistentPauseMenu] PauseMenuPanel is missing from the persistent HUD.", this);
                return;
            }
        }

        timeScaleBeforePause = Time.timeScale;
        audioWasPaused = AudioListener.pause;
        isPaused = true;

        Time.timeScale = 0f;
        AudioListener.pause = true;

        SetPanelsVisible(true, false, false);
        ApplyOverlaySorting(pausePanel, 100);
        if (pauseButton != null)
            pauseButton.gameObject.SetActive(false);
    }

    public void ResumeGame()
    {
        if (!isPaused || isLeavingGameplay)
            return;

        Time.timeScale = timeScaleBeforePause;
        AudioListener.pause = audioWasPaused;
        isPaused = false;

        SetPanelsVisible(false, false, false);
        if (pauseButton != null)
            pauseButton.gameObject.SetActive(true);
    }

    private void OpenOptions()
    {
        if (!isPaused || isLeavingGameplay)
            return;

        if (optionsPanel == null)
        {
            ResolveReferences();
            if (optionsPanel == null)
            {
                Debug.LogError("[PersistentPauseMenu] The reused Main Menu OptionsPanel is missing.", this);
                return;
            }
        }

        // Keep the pause panel underneath. The reused OptionsPanel owns its
        // own full-screen backdrop and its existing close button returns here.
        optionsPanel.SetActive(true);
        ApplyOverlaySorting(optionsPanel, 101);
    }

    private void ShowReturnConfirmation()
    {
        if (!isPaused || isLeavingGameplay)
            return;

        if (returnConfirmationPanel == null)
        {
            Debug.LogError("[PersistentPauseMenu] Return confirmation panel is missing.", this);
            return;
        }

        returnConfirmationPanel.SetActive(true);
        ApplyOverlaySorting(returnConfirmationPanel, 102);
    }

    private void CancelReturnToMenu()
    {
        if (!isPaused || isLeavingGameplay)
            return;

        if (returnConfirmationPanel != null)
            returnConfirmationPanel.SetActive(false);
    }

    private void ConfirmReturnToMainMenu()
    {
        if (!isPaused || isLeavingGameplay)
            return;

        if (!Application.CanStreamedLevelBeLoaded(MainMenuSceneName))
        {
            Debug.LogError("[PersistentPauseMenu] Main Menu Scene is not enabled in Build Settings.", this);
            CancelReturnToMenu();
            return;
        }

        isLeavingGameplay = true;
        isPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;

        // Clear the old gameplay session state without touching shared audio
        // preferences or the reusable Main Menu systems.
        if (GameManager.Instance != null)
            GameManager.Instance.ResetSession();

        // This debug overlay owns its own DDOL Canvas, separate from the HUD.
        TileGenerationDebugOverlay.Hide();

        // Keep the persistent player alive until the old gameplay scene and its
        // CombatCamera have unloaded. The sceneLoaded callback then safely tears
        // down the player and HUD without leaving a camera following a destroyed Transform.
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= HandleMainMenuSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += HandleMainMenuSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.LoadScene(MainMenuSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    private void HandleMainMenuSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (!isLeavingGameplay || scene.name != MainMenuSceneName)
            return;

        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= HandleMainMenuSceneLoaded;

        // Clean every gameplay root, not only the current singleton reference.
        // A stale non-singleton can survive an editor/domain reload even though
        // the canonical player/HUD has already been replaced.
        PersistentPlayerRoot[] players = FindObjectsByType<PersistentPlayerRoot>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (PersistentPlayerRoot player in players)
        {
            if (player == null)
                continue;

            player.gameObject.SetActive(false);
            Destroy(player.gameObject);
        }

        PersistentHUDRoot[] hudRoots = FindObjectsByType<PersistentHUDRoot>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (PersistentHUDRoot hud in hudRoots)
        {
            if (hud == null)
                continue;

            hud.gameObject.SetActive(false);
            Destroy(hud.gameObject);
        }
    }



    private static void ApplyOverlaySorting(GameObject panel, int sortingOrder)
    {
        if (panel == null)
            return;

        Canvas overlayCanvas = panel.GetComponent<Canvas>();
        if (overlayCanvas == null)
            overlayCanvas = panel.AddComponent<Canvas>();

        // Nested canvases may inherit the parent order when first enabled.
        // Reapply after activation so pause UI stays above scene-owned tutorial UI.
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = sortingOrder;
    }
}
