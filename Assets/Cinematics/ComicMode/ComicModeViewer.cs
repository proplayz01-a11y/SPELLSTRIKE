using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Spellstrike.ComicMode
{
    /// <summary>
    /// Full 42-panel comic presentation using historical rejected Work art.
    /// One JPEG TextAsset is decoded at a time and promptly released.
    /// Prime/master integration: ComicModeViewer.Open(onComplete, onBack).
    /// </summary>
    public sealed class ComicModeViewer : MonoBehaviour
    {
        private const string CompletionKey = "SpellStrike_PrologueCompleted";
        private static readonly Color Cream = new Color(0.96f, 0.91f, 0.76f);
        private static ComicModeViewer activeViewer;

        private ComicModeManifest manifest;
        private Action completeCallback;
        private Action backCallback;
        private Canvas canvas;
        private RectTransform safeRoot;
        private Image artwork;
        private TextMeshProUGUI captionText;
        private TextMeshProUGUI speakerText;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI counterText;
        private TextMeshProUGUI loadingText;
        private Button previousButton;
        private Button nextButton;
        private GameObject skipDialog;
        private Texture2D activeTexture;
        private Sprite activeSprite;
        private Coroutine imageLoad;
        private int currentIndex;
        private int lastWidth;
        private int lastHeight;
        private bool isLoading;

        public static ComicModeViewer Open(Action onComplete = null, Action onBack = null)
        {
            if (activeViewer != null) return activeViewer;
            GameObject root = new GameObject("ComicModeViewer_v01");
            ComicModeViewer viewer = root.AddComponent<ComicModeViewer>();
            activeViewer = viewer;
            viewer.completeCallback = onComplete;
            viewer.backCallback = onBack;
            viewer.Initialize();
            return viewer;
        }

        private void Initialize()
        {
            manifest = ComicModeManifest.Load();
            if (manifest == null)
            {
                Debug.LogError("[ComicMode] Cannot open the viewer without all 42 mapped panels.");
                Close();
                return;
            }

            canvas = ComicModeUi.MakeCanvas("ComicModeCanvas_v01");
            canvas.transform.SetParent(transform, false);
            ComicModeUi.Fill("Background", canvas.transform, Vector2.zero,
                Vector2.one, new Color(0.015f, 0.024f, 0.05f, 1f));
            safeRoot = ComicModeUi.SafeArea(canvas);
            ReadSafeArea();

            // Invisible full-height left/right regions for touch and mouse.
            // Created first so all existing visible Back/Skip/Prev/Next controls
            // and the skip-confirmation dialog remain above the hit targets.
            ComicModeUi.InvisiblePanelZone("ComicPreviousHalf", safeRoot,
                Vector2.zero, new Vector2(0.5f, 1f))
                .onClick.AddListener(Previous);
            ComicModeUi.InvisiblePanelZone("ComicNextHalf", safeRoot,
                new Vector2(0.5f, 0f), Vector2.one)
                .onClick.AddListener(Next);

            titleText = ComicModeUi.Label("PanelTitle", safeRoot,
                new Vector2(0.18f, 0.915f), new Vector2(0.82f, 0.985f),
                "SPELLSTRIKE", 34, TextAlignmentOptions.Center, Cream);
            counterText = ComicModeUi.Label("Counter", safeRoot,
                new Vector2(0.81f, 0.915f), new Vector2(0.91f, 0.985f),
                "01 / 42", 25, TextAlignmentOptions.Center, Cream);
            Button back = ComicModeUi.Button("BackToSelection", safeRoot,
                new Vector2(0.015f, 0.925f), new Vector2(0.15f, 0.982f), "BACK", 23);
            back.onClick.AddListener(BackToSelection);
            Button skip = ComicModeUi.Button("SkipComic", safeRoot,
                new Vector2(0.92f, 0.925f), new Vector2(0.985f, 0.982f), "SKIP", 22);
            skip.onClick.AddListener(OpenSkipDialog);

            artwork = ComicModeUi.Fill("ArchivePanel", safeRoot,
                new Vector2(0.06f, 0.265f), new Vector2(0.94f, 0.906f), Color.white);
            artwork.type = Image.Type.Simple;
            artwork.preserveAspect = true;
            artwork.enabled = false;
            loadingText = ComicModeUi.Label("Loading", safeRoot,
                new Vector2(0.22f, 0.47f), new Vector2(0.78f, 0.62f),
                "Loading historic panel...", 30, TextAlignmentOptions.Center, Cream);

            ComicModeUi.Fill("CaptionBacking", safeRoot, new Vector2(0.16f, 0.025f),
                new Vector2(0.84f, 0.248f), new Color(0.055f, 0.078f, 0.13f, 0.98f));
            speakerText = ComicModeUi.Label("Speaker", safeRoot,
                new Vector2(0.185f, 0.200f), new Vector2(0.82f, 0.24f), "",
                24, TextAlignmentOptions.Left, Cream);
            captionText = ComicModeUi.Label("Dialogue", safeRoot,
                new Vector2(0.185f, 0.05f), new Vector2(0.815f, 0.199f), "",
                31, TextAlignmentOptions.MidlineLeft, Color.white);

            previousButton = ComicModeUi.Button("PreviousPanel", safeRoot,
                new Vector2(0.015f, 0.066f), new Vector2(0.145f, 0.186f), "PREV", 27);
            nextButton = ComicModeUi.Button("NextPanel", safeRoot,
                new Vector2(0.855f, 0.066f), new Vector2(0.985f, 0.186f), "NEXT", 27);
            previousButton.onClick.AddListener(Previous);
            nextButton.onClick.AddListener(Next);

            skipDialog = BuildSkipDialog(safeRoot);
            skipDialog.SetActive(false);
            SetPanel(0);
        }

        private GameObject BuildSkipDialog(Transform parent)
        {
            Image shade = ComicModeUi.Fill("ConfirmSkip", parent,
                Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.9f));
            shade.raycastTarget = true;
            RectTransform dialog = ComicModeUi.Rect("Dialog", shade.transform,
                new Vector2(0.25f, 0.31f), new Vector2(0.75f, 0.68f));
            ComicModeUi.Fill("DialogBackground", dialog, Vector2.zero, Vector2.one,
                new Color(0.07f, 0.09f, 0.16f, 1f));
            ComicModeUi.Label("SkipPrompt", dialog, new Vector2(0.1f, 0.48f),
                new Vector2(0.9f, 0.88f), "Skip the rest of the comic?",
                34, TextAlignmentOptions.Center, Cream);
            Button cancel = ComicModeUi.Button("Cancel", dialog,
                new Vector2(0.06f, 0.1f), new Vector2(0.45f, 0.37f), "CONTINUE", 24);
            Button confirm = ComicModeUi.Button("Confirm", dialog,
                new Vector2(0.55f, 0.1f), new Vector2(0.94f, 0.37f), "SKIP", 24);
            cancel.onClick.AddListener(() => skipDialog.SetActive(false));
            confirm.onClick.AddListener(Complete);
            return shade.gameObject;
        }

        private void Update()
        {
            if (canvas == null) return;
            ComicModeUi.EnsureEventSystem(canvas.transform);
            if (lastWidth != Screen.width || lastHeight != Screen.height) ReadSafeArea();
            if (skipDialog.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.Escape)) skipDialog.SetActive(false);
                return;
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow)) Previous();
            if (Input.GetKeyDown(KeyCode.RightArrow) ||
                Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) Next();
            if (Input.GetKeyDown(KeyCode.Escape)) BackToSelection();
        }

        private void ReadSafeArea()
        {
            if (safeRoot == null) return;
            ComicModeUi.UpdateSafeArea(safeRoot);
            lastWidth = Screen.width;
            lastHeight = Screen.height;
        }

        private void SetPanel(int newIndex)
        {
            if (manifest == null || newIndex < 0 || newIndex >= manifest.panels.Length) return;
            currentIndex = newIndex;
            ComicModePanel panel = manifest.panels[currentIndex];
            titleText.text = panel.title;
            counterText.text = (currentIndex + 1).ToString("00") + " / 42";
            string status = panel.textStatus == "proposed" ? "  ·  DRAFT SCRIPT" : "";
            speakerText.text = string.IsNullOrEmpty(panel.caption)
                ? "SILENT PANEL" : panel.speaker + status;
            captionText.text = panel.caption ?? "";
            previousButton.interactable = currentIndex > 0;
            nextButton.GetComponentInChildren<TextMeshProUGUI>().text =
                currentIndex == manifest.panels.Length - 1 ? "FINISH" : "NEXT";
            loadingText.gameObject.SetActive(true);
            loadingText.text = "Loading panel " + (currentIndex + 1).ToString("00") + "...";
            isLoading = true;
            if (imageLoad != null) StopCoroutine(imageLoad);
            imageLoad = StartCoroutine(LoadPanel(panel));
        }

        private IEnumerator LoadPanel(ComicModePanel panel)
        {
            ResourceRequest request = Resources.LoadAsync<TextAsset>(panel.imageResource);
            yield return request;
            TextAsset bytes = request.asset as TextAsset;
            if (bytes == null)
            {
                artwork.enabled = false;
                loadingText.text = "Historical artwork unavailable for panel " +
                    panel.id.ToString("00") + ".";
                ReleaseArtwork();
                isLoading = false;
                imageLoad = null;
                yield break;
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
            bool okay = false;
            try { okay = ImageConversion.LoadImage(texture, bytes.bytes, true); }
            catch (Exception error) { Debug.LogWarning("[ComicMode] Decode error: " + error.Message); }
            Resources.UnloadAsset(bytes);

            if (!okay)
            {
                Destroy(texture);
                ReleaseArtwork();
                artwork.enabled = false;
                loadingText.text = "Cannot decode historical panel " + panel.id.ToString("00") + ".";
                isLoading = false;
                imageLoad = null;
                yield break;
            }
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 100f);
            ReleaseArtwork();
            activeTexture = texture;
            activeSprite = sprite;
            artwork.sprite = activeSprite;
            artwork.enabled = true;
            loadingText.gameObject.SetActive(false);
            isLoading = false;
            imageLoad = null;
        }

        private void ReleaseArtwork()
        {
            if (artwork != null) artwork.sprite = null;
            if (activeSprite != null) Destroy(activeSprite);
            if (activeTexture != null) Destroy(activeTexture);
            activeSprite = null;
            activeTexture = null;
        }

        public void Next()
        {
            if (isLoading || skipDialog.activeSelf || manifest == null) return;
            if (currentIndex == manifest.panels.Length - 1) Complete();
            else SetPanel(currentIndex + 1);
        }

        public void Previous()
        {
            if (isLoading || skipDialog.activeSelf || currentIndex == 0) return;
            SetPanel(currentIndex - 1);
        }

        public void BackToSelection()
        {
            Action callback = backCallback;
            Close();
            callback?.Invoke();
        }

        public void OpenSkipDialog()
        {
            if (skipDialog != null) skipDialog.SetActive(true);
        }

        public void Complete()
        {
            PlayerPrefs.SetInt(CompletionKey, 1);
            PlayerPrefs.Save();
            Action callback = completeCallback;
            Close();
            if (callback != null)
            {
                callback();
            }
            else if (Application.CanStreamedLevelBeLoaded("TutorialScene"))
            {
                SceneManager.LoadScene("TutorialScene");
            }
            else
            {
                Debug.Log("[ComicMode] Completed. Hook onComplete in cinematic master for routing.");
            }
        }

        public void Close()
        {
            if (imageLoad != null) StopCoroutine(imageLoad);
            imageLoad = null;
            ReleaseArtwork();
            if (activeViewer == this) activeViewer = null;
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            ReleaseArtwork();
            if (activeViewer == this) activeViewer = null;
        }
    }
}
