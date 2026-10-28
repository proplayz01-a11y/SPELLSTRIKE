using UnityEngine;
using UnityEngine.SceneManagement;

namespace Spellstrike.ComicMode
{
    /// <summary>
    /// Attach to the independent story-mode entry scene. This runs BEFORE the
    /// prime-owned 3D master (whose director uses Play On Awake).
    /// Prime owns adding this entry and the 3D master to the active Build Profile.
    /// </summary>
    public sealed class ComicModeRouteBootstrap : MonoBehaviour
    {
        public const string MasterScenePath =
            "Assets/Scenes/Cinematics/Prologue_Master_Sequence_P01_42_v04.unity";
        public const string TutorialSceneName = "TutorialScene";
        public const string MainMenuSceneName = "Main Menu Scene";

        private void Start()
        {
            ShowChoice();
        }

        private void ShowChoice()
        {
            bool masterAvailable = Application.CanStreamedLevelBeLoaded(MasterScenePath);
            ComicModeSelection.Open(masterAvailable ? (System.Action)StartCinematic : null,
                FinishPrologue, BackToMenu);
            if (!masterAvailable)
            {
                Debug.LogWarning("[ComicMode] 3D master scene is not in the active Build Profile: " +
                                 MasterScenePath + ". The Comic option remains usable.");
            }
        }

        private void StartCinematic()
        {
            if (!Application.CanStreamedLevelBeLoaded(MasterScenePath))
            {
                Debug.LogError("[ComicMode] Missing master cinematic: " + MasterScenePath);
                ShowChoice();
                return;
            }
            SceneManager.LoadScene(MasterScenePath);
        }

        private void FinishPrologue()
        {
            if (Application.CanStreamedLevelBeLoaded(TutorialSceneName))
            {
                SceneManager.LoadScene(TutorialSceneName);
            }
            else
            {
                Debug.LogError("[ComicMode] TutorialScene must be present in active Build Profile.");
                ShowChoice();
            }
        }

        private void BackToMenu()
        {
            if (Application.CanStreamedLevelBeLoaded(MainMenuSceneName))
                SceneManager.LoadScene(MainMenuSceneName);
            else
                ShowChoice();
        }
    }
}
