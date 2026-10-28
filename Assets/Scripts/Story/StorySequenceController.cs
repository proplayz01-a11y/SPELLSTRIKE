using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StorySequenceController : MonoBehaviour
{
    [SerializeField] private StorySequenceProfile profile;
    [SerializeField] private Image pageImage;
    [SerializeField] private CanvasGroup pageGroup;
    [SerializeField] private TMP_Text fallbackText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button skipButton;
    [SerializeField] private GameObject skipConfirmation;
    [SerializeField] private Button confirmSkipButton;
    [SerializeField] private Button cancelSkipButton;
    [SerializeField] private GameObject inputBlocker;
    [SerializeField] private AudioSource audioSource;
    [SerializeField, Min(0f)] private float fadeDuration = 0.3f;

    private int pageIndex;
    private float pageShownAt;
    private bool transitionInProgress;

    private void Awake()
    {
        previousButton?.onClick.AddListener(Previous);
        nextButton?.onClick.AddListener(Advance);
        skipButton?.onClick.AddListener(OpenSkipConfirmation);
        confirmSkipButton?.onClick.AddListener(ConfirmSkip);
        cancelSkipButton?.onClick.AddListener(CancelSkip);
    }

    private void Start()
    {
        if (!ValidateSetup()) { enabled = false; return; }
        pageGroup.alpha = 1f;
        skipConfirmation.SetActive(false);
        SetTransitionLock(false);
        PresentCurrentPage();
    }

    private void Update()
    {
        if (transitionInProgress || skipConfirmation.activeSelf) return;
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.RightArrow))
            Advance();
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            Previous();
    }

    public void Advance()
    {
        if (!CanAdvance()) return;
        if (pageIndex >= profile.Pages.Count - 1) { CompleteSequence(); return; }
        StartCoroutine(TransitionToPage(pageIndex + 1));
    }

    public void Previous()
    {
        if (transitionInProgress || profile == null || pageIndex <= 0)
            return;

        StartCoroutine(TransitionToPage(pageIndex - 1));
    }

    public void OpenSkipConfirmation()
    {
        if (transitionInProgress || !profile.AllowSkip) return;
        skipConfirmation.SetActive(true);
    }

    public void CancelSkip() => skipConfirmation.SetActive(false);
    public void ConfirmSkip() { if (!transitionInProgress) CompleteSequence(); }

    private bool CanAdvance()
    {
        if (transitionInProgress || profile == null) return false;
        return Time.unscaledTime - pageShownAt >= profile.Pages[pageIndex].MinimumDisplayTime;
    }

    private IEnumerator TransitionToPage(int nextIndex)
    {
        SetTransitionLock(true);
        yield return Fade(1f, 0f);
        pageIndex = nextIndex;
        PresentCurrentPage();
        yield return Fade(0f, 1f);
        SetTransitionLock(false);
    }

    private IEnumerator Fade(float from, float to)
    {
        if (fadeDuration <= 0f) { pageGroup.alpha = to; yield break; }
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            pageGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }
        pageGroup.alpha = to;
    }

    private void PresentCurrentPage()
    {
        StorySequenceProfile.Page page = profile.Pages[pageIndex];
        bool hasArtwork = page.PageSprite != null;
        pageImage.sprite = page.PageSprite;
        pageImage.enabled = hasArtwork;
        fallbackText.gameObject.SetActive(!hasArtwork);
        fallbackText.text = string.IsNullOrWhiteSpace(page.FallbackText)
            ? $"Story page unavailable\n{page.PageId}" : page.FallbackText;
        if (!hasArtwork) Debug.LogError($"[StorySequence] Missing artwork for {page.PageId}; fallback remains advanceable.", this);
        progressText.text = $"{pageIndex + 1:00} / {profile.Pages.Count:00}";
        previousButton.interactable = pageIndex > 0;
        skipButton.gameObject.SetActive(profile.AllowSkip);
        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.clip = page.OptionalAudio;
            if (audioSource.clip != null) audioSource.Play();
        }
        pageShownAt = Time.unscaledTime;
    }

    private void CompleteSequence()
    {
        SetTransitionLock(true);
        if (!string.IsNullOrWhiteSpace(profile.CompletionKey))
        {
            PlayerPrefs.SetInt(profile.CompletionKey, 1);
            PlayerPrefs.Save();
        }
        string nextScene = profile.ResolveNextScene(profile.Pages[pageIndex]);
        if (string.IsNullOrWhiteSpace(nextScene))
        {
            Debug.LogWarning("[StorySequence] Completed without a configured destination.", this);
            SetTransitionLock(false);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(nextScene))
        {
            Debug.LogError(
                $"[StorySequence] Cannot load '{nextScene}'. Add it to the active Build Profile or shared scene list.",
                this);
            SetTransitionLock(false);
            return;
        }

        SceneManager.LoadScene(nextScene);
    }

    private void SetTransitionLock(bool locked)
    {
        transitionInProgress = locked;
        if (inputBlocker != null) inputBlocker.SetActive(locked);
    }

    private bool ValidateSetup()
    {
        bool valid = profile != null && profile.Pages.Count > 0 && pageImage != null && pageGroup != null &&
                     fallbackText != null && progressText != null &&
                     previousButton != null && nextButton != null && skipButton != null &&
                     skipConfirmation != null && confirmSkipButton != null && cancelSkipButton != null;
        if (!valid) Debug.LogError("[StorySequence] Required Prologue scene references are incomplete.", this);
        return valid;
    }
}
