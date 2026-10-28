using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Spellstrike.ComicMode
{
    /// <summary>
    /// Public entry for the master cinematic mode picker.
    /// Call Open(playCinematic, onPrologueCompleted, onBack).
    /// The first callback starts the existing 3D master cinematic. The second
    /// handles story completion for either the comic or its skip button.
    /// </summary>
    public sealed class ComicModeSelection : MonoBehaviour
    {
        private static ComicModeSelection active;
        private Action cinematicCallback;
        private Action completedCallback;
        private Action exitCallback;
        private RectTransform safeRoot;
        private int lastScreenWidth;
        private int lastScreenHeight;

        public static ComicModeSelection Open(
            Action playCinematic, Action onPrologueCompleted = null, Action onBack = null)
        {
            if (active != null) return active;
            GameObject root = new GameObject("Spellstrike_StoryModeSelection_v01");
            ComicModeSelection chooser = root.AddComponent<ComicModeSelection>();
            active = chooser;
            chooser.cinematicCallback = playCinematic;
            chooser.completedCallback = onPrologueCompleted;
            chooser.exitCallback = onBack;
            chooser.Initialize();
            return chooser;
        }

        private void Initialize()
        {
            Canvas canvas = ComicModeUi.MakeCanvas("StoryModeSelectionCanvas");
            canvas.transform.SetParent(transform, false);
            ComicModeUi.Fill("Background", canvas.transform, Vector2.zero, Vector2.one,
                new Color(0.01f, 0.02f, 0.045f, 1f));
            safeRoot = ComicModeUi.SafeArea(canvas);
            RefreshSafeArea();
            ComicModeUi.Label("StoryHeading", safeRoot,
                new Vector2(0.1f, 0.75f), new Vector2(0.9f, 0.94f),
                "HOW WOULD YOU LIKE TO EXPERIENCE THE PROLOGUE?",
                44, TextAlignmentOptions.Center, new Color(0.97f, 0.9f, 0.68f));
            ComicModeUi.Label("StorySubtitle", safeRoot,
                new Vector2(0.1f, 0.65f), new Vector2(0.9f, 0.76f),
                "Choose your storytelling style", 29,
                TextAlignmentOptions.Center, Color.white);
            Button cinematic = ComicModeUi.Button("AnimatedCinematic", safeRoot,
                new Vector2(0.14f, 0.35f), new Vector2(0.48f, 0.59f),
                "ANIMATED CINEMATIC", 34);
            cinematic.onClick.AddListener(PlayCinematic);
            cinematic.interactable = cinematicCallback != null;
            Button comic = ComicModeUi.Button("ComicPanels", safeRoot,
                new Vector2(0.52f, 0.35f), new Vector2(0.86f, 0.59f),
                "COMIC PANELS", 34);
            comic.onClick.AddListener(PlayComic);
            ComicModeUi.Label("ComicDescription", safeRoot,
                new Vector2(0.52f, 0.25f), new Vector2(0.86f, 0.35f),
                "42 classic illustrated panels", 25,
                TextAlignmentOptions.Center, Color.white);
            ComicModeUi.Label("CinematicDescription", safeRoot,
                new Vector2(0.14f, 0.25f), new Vector2(0.48f, 0.35f),
                cinematicCallback == null ? "Available when linked to the master cinematic"
                                          : "Watch the animated story",
                25, TextAlignmentOptions.Center, Color.white);
            Button exit = ComicModeUi.Button("Back", safeRoot,
                new Vector2(0.03f, 0.06f), new Vector2(0.17f, 0.15f), "BACK", 24);
            exit.onClick.AddListener(Exit);
        }

        private void PlayCinematic()
        {
            Action callback = cinematicCallback;
            Close();
            callback?.Invoke();
        }

        private void PlayComic()
        {
            Action cinematic = cinematicCallback;
            Action completed = completedCallback;
            Action exit = exitCallback;
            Close();
            ComicModeViewer.Open(completed, () => Open(cinematic, completed, exit));
        }

        private void Exit()
        {
            Action callback = exitCallback;
            Close();
            callback?.Invoke();
        }

        private void Update()
        {
            if (safeRoot == null) return;
            ComicModeUi.EnsureEventSystem(safeRoot.root);
            if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
                RefreshSafeArea();
            if (Input.GetKeyDown(KeyCode.Escape)) Exit();
        }

        private void RefreshSafeArea()
        {
            ComicModeUi.UpdateSafeArea(safeRoot);
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
        }

        public void Close()
        {
            if (active == this) active = null;
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (active == this) active = null;
        }
    }
}
