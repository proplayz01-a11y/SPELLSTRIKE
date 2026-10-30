using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Spellstrike.ComicMode;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using UnityEngine.UI;

namespace Spellstrike.ComicMode
{
    /// <summary>
    /// Main Menu Adventure first-run chooser. Add the MASTER 3D scene to Build Settings
    /// at the END; do not change any previous gameplay scene indices.
    /// Comic artwork remains historical rejected Work variants.
    /// </summary>
    public sealed class ComicModeAdventureRouter : MonoBehaviour
    {
        public const string CompletionKey = "SpellStrike_PrologueCompleted";
        public const string CinematicScene = "Assets/Scenes/Cinematics/Prologue_Master_Sequence_P01_42_v07_SpellbookWalkPolish.unity";
        public const string TutorialScene = "TutorialScene";
        private static ComicModeAdventureRouter current;
        private Coroutine cinematicRoutine;
        private Canvas skipCanvas;
        private Canvas cinematicLoadingCanvas;
        private GameObject confirmation;
        private bool finishing;
        private PlayableDirector cinematicMaster;
        private double[] panelStarts;
        private RectTransform navigationSafeArea;
        private int lastScreenWidth;
        private int lastScreenHeight;
        private GameObject dialoguePanel;
        private TextMeshProUGUI dialogueSpeaker;
        private TextMeshProUGUI dialogueLine;
        private string displayedSpeaker;
        private string displayedLine;

        public static void Open()
        {
            if (current != null) return;
            var gameObject = new GameObject("Spellstrike_PrologueModeAdventureRouter");
            DontDestroyOnLoad(gameObject);
            current = gameObject.AddComponent<ComicModeAdventureRouter>();
            current.ShowChoice();
        }

        private void ShowChoice()
        {
            Action cinematic = Application.CanStreamedLevelBeLoaded(CinematicScene)
                ? (Action)BeginCinematic : null;
            ComicModeSelection.Open(cinematic, CompleteAndEnterTutorial, ExitToMenu);
        }

        private void BeginCinematic()
        {
            if (cinematicRoutine != null) return;
            ShowCinematicLoading();
            cinematicRoutine = StartCoroutine(PlayCinematic());
        }

        // The master scene can take several seconds to import/load on mobile.
        // Cover the underlying Main Menu while its asynchronous load is active.
        // This is a loading state, not a second menu or a fake loading timer.
        private void ShowCinematicLoading()
        {
            if (cinematicLoadingCanvas != null) return;
            cinematicLoadingCanvas = ComicModeUi.MakeCanvas("PrologueCinematicLoading");
            cinematicLoadingCanvas.transform.SetParent(transform, false);
            cinematicLoadingCanvas.sortingOrder = 480;
            ComicModeUi.Fill("LoadingBackdrop", cinematicLoadingCanvas.transform,
                Vector2.zero, Vector2.one, new Color(0.015f, 0.023f, 0.046f, 1f));
            ComicModeUi.Label("LoadingTitle", cinematicLoadingCanvas.transform,
                new Vector2(0.10f, 0.48f), new Vector2(0.90f, 0.62f),
                "OPENING THE PROLOGUE", 42, TextAlignmentOptions.Center,
                new Color(0.93f, 0.82f, 0.57f));
            ComicModeUi.Label("LoadingSubtitle", cinematicLoadingCanvas.transform,
                new Vector2(0.16f, 0.39f), new Vector2(0.84f, 0.49f),
                "Preparing the cinematic...", 26, TextAlignmentOptions.Center,
                new Color(0.82f, 0.85f, 0.93f));
        }

        private void HideCinematicLoading()
        {
            if (cinematicLoadingCanvas == null) return;
            Destroy(cinematicLoadingCanvas.gameObject);
            cinematicLoadingCanvas = null;
        }

        private IEnumerator PlayCinematic()
        {
            if (!Application.CanStreamedLevelBeLoaded(CinematicScene))
            {
                Debug.LogError("[StoryMode] Animated 42-panel scene is not registered in the active Build Profile.");
                HideCinematicLoading();
                cinematicRoutine = null;
                ShowChoice();
                yield break;
            }

            // Let the loading overlay render before starting this heavy master scene load.
            // Two frame yields guarantee at least one fully presented loading frame.
            Canvas.ForceUpdateCanvases();
            yield return null;
            yield return null;
            AsyncOperation load = SceneManager.LoadSceneAsync(CinematicScene, LoadSceneMode.Single);
            if (load == null)
            {
                Debug.LogError("[StoryMode] Master scene load failed.");
                HideCinematicLoading();
                cinematicRoutine = null;
                yield break;
            }
            yield return load;
            yield return null;

            PlayableDirector master = null;
            foreach (PlayableDirector candidate in FindObjectsByType<PlayableDirector>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.name.StartsWith("PROLOGUE_MASTER_TIMELINE_DIRECTOR", StringComparison.Ordinal))
                {
                    master = candidate;
                    break;
                }
            }
            if (master == null)
            {
                Debug.LogError("[StoryMode] Cannot locate 42-panel master PlayableDirector in cinematic scene.");
                HideCinematicLoading();
                cinematicRoutine = null;
                yield break;
            }

            cinematicMaster = master;
            panelStarts = BuildPanelStarts(master);
            CreateSkipUI();
            if (master.state != PlayState.Playing) master.Play();
            HideCinematicLoading();
            while (!finishing && master != null && SceneManager.GetActiveScene().path == CinematicScene)
            {
                // Stop states in Unity vary by wrap mode; end on completed duration,
                // not an invented timer or a guessed scene index.
                if (master.time >= master.duration - 0.07)
                {
                    CompleteAndEnterTutorial();
                    yield break;
                }
                yield return null;
            }
            cinematicRoutine = null;
        }

        private void CreateSkipUI()
        {
            if (skipCanvas != null) return;
            skipCanvas = ComicModeUi.MakeCanvas("CinematicStoryNavigation");
            skipCanvas.transform.SetParent(transform, false);
            RectTransform safe = ComicModeUi.SafeArea(skipCanvas);
            navigationSafeArea = safe;
            RefreshSafeArea();

            if (panelStarts != null && panelStarts.Length == 42)
            {
                // Cover the full height of each half of the safe screen, with
                // no visible graphics or visual changes to the movie.
                ComicModeUi.InvisiblePanelZone("CinematicPreviousHalf", safe,
                    Vector2.zero, new Vector2(0.5f, 1f))
                    .onClick.AddListener(PreviousPanel);
                ComicModeUi.InvisiblePanelZone("CinematicNextHalf", safe,
                    new Vector2(0.5f, 0f), Vector2.one)
                    .onClick.AddListener(NextPanel);
            }
            // This is a runtime-only dialogue overlay. Keeping it under the
            // visible Skip controls maintains the full-screen swipe/tap zones.
            if (SceneManager.GetActiveScene().path == CinematicScene)
                CreateDialogueOverlay(safe);

            Button skip = ComicModeUi.Button("CinematicSkip", safe,
                new Vector2(.81f, .9f), new Vector2(.99f, .985f), "SKIP STORY", 25);
            skip.onClick.AddListener(() => { if (confirmation != null) confirmation.SetActive(true); });
            Image overlay = ComicModeUi.Fill("ConfirmSkipCinematic", safe, Vector2.zero, Vector2.one,
                new Color(0, 0, 0, .88f));
            overlay.raycastTarget = true; // Block all invisible navigation while confirming.
            confirmation = overlay.gameObject;
            RectTransform center = ComicModeUi.Rect("ConfirmStorySkip", overlay.transform,
                new Vector2(.25f, .31f), new Vector2(.75f, .69f));
            ComicModeUi.Fill("ConfirmBackground", center, Vector2.zero, Vector2.one,
                new Color(.04f, .06f, .14f, .98f));
            ComicModeUi.Label("ConfirmText", center, new Vector2(.07f,.49f), new Vector2(.93f,.88f),
                "Skip the remaining cinematic?", 34, TextAlignmentOptions.Center, Color.white);
            Button resume = ComicModeUi.Button("Continue", center,
                new Vector2(.06f,.1f), new Vector2(.47f,.38f), "CONTINUE", 24);
            resume.onClick.AddListener(() => confirmation.SetActive(false));
            Button confirmSkip = ComicModeUi.Button("Confirm", center,
                new Vector2(.53f,.1f), new Vector2(.94f,.38f), "SKIP", 24);
            confirmSkip.onClick.AddListener(CompleteAndEnterTutorial);
            confirmation.SetActive(false);
        }

        private void CreateDialogueOverlay(RectTransform safe)
        {
            if (panelStarts == null || panelStarts.Length != CinematicPrologueDialogueV06.PanelCount)
            {
                Debug.LogError("[StoryDialogueV06] Timeline and dialogue panel counts differ.");
                return;
            }

            // Older draft labels are inside shot roots. Hide them only in the
            // running v06 instance to prevent two competing speaker captions.
            // This never edits the saved shots or historical v05 scene.
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (TMP_Text oldCaption in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    string n = oldCaption.name.ToUpperInvariant();
                    if (n.Contains("SUBTITLE") || n.Contains("DIALOGUE") || n.Contains("CAPTION"))
                        oldCaption.gameObject.SetActive(false);
                }
            }

            RectTransform rect = ComicModeUi.Rect("CinematicDialogueV06", safe,
                new Vector2(.105f, .025f), new Vector2(.895f, .215f));
            dialoguePanel = rect.gameObject;
            ComicModeUi.Fill("DialogueBackground", rect, Vector2.zero, Vector2.one,
                new Color(.025f, .035f, .075f, .88f));
            dialogueSpeaker = ComicModeUi.Label("DialogueSpeaker", rect,
                new Vector2(.035f, .69f), new Vector2(.965f, .94f),
                "", 31, TextAlignmentOptions.MidlineLeft,
                new Color(.97f, .83f, .53f));
            dialogueLine = ComicModeUi.Label("DialogueLine", rect,
                new Vector2(.035f, .08f), new Vector2(.965f, .73f),
                "", 43, TextAlignmentOptions.MidlineLeft, Color.white);
            dialoguePanel.SetActive(false);
        }

        private void RefreshDialogueOverlay()
        {
            if (dialoguePanel == null || cinematicMaster == null || panelStarts == null)
                return;
            int index = CurrentPanelIndex();
            if (index < 0) return;
            CinematicPrologueDialogueV06.Cue cue =
                CinematicPrologueDialogueV06.GetCue(index, cinematicMaster.time - panelStarts[index]);
            string speaker = cue.Speaker ?? "";
            string line = cue.Text ?? "";
            if (displayedSpeaker == speaker && displayedLine == line)
                return;
            displayedSpeaker = speaker;
            displayedLine = line;
            dialoguePanel.SetActive(!string.IsNullOrWhiteSpace(line));
            if (dialoguePanel.activeSelf)
            {
                dialogueSpeaker.text = speaker;
                dialogueLine.text = line;
            }
        }

        // Use the real 42 shot boundaries instead of guessing a fixed panel
        // length. The archive P15-P18 sequence is one master activation
        // track, but has four separate nested activation clips.
        private static double[] BuildPanelStarts(PlayableDirector master)
        {
            TimelineAsset timeline = master.playableAsset as TimelineAsset;
            if (timeline == null) return null;
            Dictionary<int, double> starts = new Dictionary<int, double>();
            foreach (ActivationTrack track in timeline.GetOutputTracks().OfType<ActivationTrack>())
            {
                TimelineClip clip = track.GetClips().FirstOrDefault();
                if (clip == null) continue;
                string marker = track.name.Split(' ')[0];
                if (marker == "P15_P18")
                {
                    Transform archive = SceneManager.GetActiveScene().GetRootGameObjects()
                        .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                        .FirstOrDefault(child => child.name == "P15_P18_SHOT_ROOT");
                    PlayableDirector nestedDirector = archive != null
                        ? archive.GetComponentInChildren<PlayableDirector>(true) : null;
                    TimelineAsset nested = nestedDirector != null
                        ? nestedDirector.playableAsset as TimelineAsset : null;
                    if (nested == null) continue;
                    foreach (ActivationTrack subtrack in nested.GetOutputTracks().OfType<ActivationTrack>())
                    {
                        string name = subtrack.name.Split(' ')[0];
                        int index;
                        if (name.Length != 3 || name[0] != 'P' ||
                            !int.TryParse(name.Substring(1), out index) ||
                            index < 15 || index > 18) continue;
                        TimelineClip subclip = subtrack.GetClips().FirstOrDefault();
                        if (subclip != null) starts[index] = clip.start + subclip.start;
                    }
                }
                else
                {
                    int index;
                    if (marker.Length == 3 && marker[0] == 'P' &&
                        int.TryParse(marker.Substring(1), out index) &&
                        index >= 1 && index <= 42)
                        starts[index] = clip.start;
                }
            }

            if (starts.Count != 42 || Enumerable.Range(1, 42).Any(i => !starts.ContainsKey(i)))
            {
                Debug.LogError("[StoryMode] Cannot build 42-panel navigation boundaries. Navigation disabled.");
                return null;
            }
            double[] result = Enumerable.Range(1, 42).Select(i => starts[i]).ToArray();
            for (int i = 1; i < result.Length; i++)
                if (result[i] <= result[i - 1])
                {
                    Debug.LogError("[StoryMode] Panel timestamps out of order; navigation disabled.");
                    return null;
                }
            return result;
        }

        private int CurrentPanelIndex()
        {
            if (cinematicMaster == null || panelStarts == null) return -1;
            int match = Array.BinarySearch(panelStarts, cinematicMaster.time + 0.001);
            return Mathf.Clamp(match >= 0 ? match : ~match - 1, 0, panelStarts.Length - 1);
        }

        private void NextPanel()
        {
            if (finishing || confirmation != null && confirmation.activeSelf) return;
            int index = CurrentPanelIndex();
            if (index < 0) return;
            if (index >= panelStarts.Length - 1)
            {
                CompleteAndEnterTutorial();
                return;
            }
            SeekPanel(index + 1);
        }

        private void PreviousPanel()
        {
            if (finishing || confirmation != null && confirmation.activeSelf) return;
            int index = CurrentPanelIndex();
            if (index > 0) SeekPanel(index - 1);
        }

        private void SeekPanel(int index)
        {
            if (cinematicMaster == null || panelStarts == null ||
                index < 0 || index >= panelStarts.Length) return;
            cinematicMaster.time = panelStarts[index];
            cinematicMaster.Evaluate();
        }

        private void Update()
        {
            if (navigationSafeArea == null) return;
            ComicModeUi.EnsureEventSystem(navigationSafeArea.root);
            if (lastScreenWidth != Screen.width || lastScreenHeight != Screen.height)
                RefreshSafeArea();
            if (finishing || cinematicMaster == null ||
                confirmation != null && confirmation.activeSelf) return;
            RefreshDialogueOverlay();
            if (Input.GetKeyDown(KeyCode.LeftArrow)) PreviousPanel();
            if (Input.GetKeyDown(KeyCode.RightArrow)) NextPanel();
        }

        private void RefreshSafeArea()
        {
            if (navigationSafeArea == null) return;
            ComicModeUi.UpdateSafeArea(navigationSafeArea);
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
        }

        private void CompleteAndEnterTutorial()
        {
            if (finishing) return;
            finishing = true;
            PlayerPrefs.SetInt(CompletionKey, 1);
            PlayerPrefs.Save();
            if (Application.CanStreamedLevelBeLoaded(TutorialScene))
                SceneManager.LoadScene(TutorialScene);
            else
                Debug.LogError("[StoryMode] TutorialScene is not in the active Build Profile.");
            Destroy(gameObject);
        }

        private void ExitToMenu()
        {
            if (cinematicRoutine != null) StopCoroutine(cinematicRoutine);
            HideCinematicLoading();
            if (skipCanvas != null) Destroy(skipCanvas.gameObject);
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (current == this) current = null;
        }
    }
}
