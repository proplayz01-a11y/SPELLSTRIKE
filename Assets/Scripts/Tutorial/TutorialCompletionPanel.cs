using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class TutorialCompletionPanel : MonoBehaviour
{
    private const string StageSelectSceneName = "StageSelectScene";
    private const string MainMenuSceneName = "Main Menu Scene";

    private CanvasGroup canvasGroup;
    private float previousTimeScale = 1f;
    private bool ownsPause;
    private bool navigating;

    public static void Show()
    {
        TutorialCompletionPanel existing = FindFirstObjectByType<TutorialCompletionPanel>();
        if (existing != null)
        {
            existing.ShowPanel();
            return;
        }

        GameObject root = new GameObject("TutorialCompletionPanel");
        TutorialCompletionPanel panel = root.AddComponent<TutorialCompletionPanel>();
        panel.BuildPresentation();
        panel.ShowPanel();
    }

    private void BuildPresentation()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();
        canvasGroup = gameObject.AddComponent<CanvasGroup>();

        Image backdrop = Rect("Backdrop", transform, Vector2.zero, Vector2.one).AddComponent<Image>();
        backdrop.color = new Color(0.005f, 0.009f, 0.02f, 0.86f);

        GameObject dialog = Rect(
            "CompletionDialog",
            transform,
            new Vector2(0.23f, 0.25f),
            new Vector2(0.77f, 0.75f));
        Image dialogImage = dialog.AddComponent<Image>();
        dialogImage.color = new Color(0.025f, 0.055f, 0.095f, 0.98f);

        TMP_Text title = Text(
            "Title",
            dialog.transform,
            "TUTORIAL COMPLETE!",
            54f,
            TextAlignmentOptions.Center,
            new Color(0.95f, 0.72f, 0.25f, 1f));
        SetRect(title.rectTransform, new Vector2(0.08f, 0.68f), new Vector2(0.92f, 0.9f));

        TMP_Text reward = Text(
            "Reward",
            dialog.transform,
            "NEW ITEM ACQUIRED: SPELLBOOK!",
            38f,
            TextAlignmentOptions.Center,
            new Color(0.4f, 0.86f, 1f, 1f));
        SetRect(reward.rectTransform, new Vector2(0.08f, 0.48f), new Vector2(0.92f, 0.68f));

        TMP_Text description = Text(
            "Description",
            dialog.transform,
            "Every valid word you cast can now be recorded in your Spellbook.",
            25f,
            TextAlignmentOptions.Center,
            new Color(0.92f, 0.94f, 0.98f, 1f));
        SetRect(description.rectTransform, new Vector2(0.1f, 0.34f), new Vector2(0.9f, 0.49f));

        Button continueButton = Button(
            "ContinueButton",
            dialog.transform,
            "CONTINUE",
            new Vector2(0.08f, 0.09f),
            new Vector2(0.48f, 0.28f),
            new Color(0.08f, 0.55f, 0.74f, 1f));
        continueButton.onClick.AddListener(ContinueToStageSelect);

        Button mainMenuButton = Button(
            "MainMenuButton",
            dialog.transform,
            "MAIN MENU",
            new Vector2(0.52f, 0.09f),
            new Vector2(0.92f, 0.28f),
            new Color(0.12f, 0.16f, 0.23f, 1f));
        mainMenuButton.onClick.AddListener(ReturnToMainMenu);

        if (EventSystem.current == null)
            new GameObject("TutorialCompletionEventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private void ShowPanel()
    {
        gameObject.SetActive(true);
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        ownsPause = true;
        StopAllCoroutines();
        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        canvasGroup.alpha = 0f;
        float elapsed = 0f;
        const float duration = 0.22f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    private void ContinueToStageSelect() => LoadDestination(StageSelectSceneName);

    private void ReturnToMainMenu() => LoadDestination(MainMenuSceneName);

    private void LoadDestination(string sceneName)
    {
        if (navigating)
            return;

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                $"[TutorialCompletion] Cannot load '{sceneName}'. Add it to the active Build Profile or shared scene list.",
                this);
            return;
        }

        navigating = true;
        RestoreTimeScale();
        SceneManager.LoadScene(sceneName);
    }

    private void OnDestroy() => RestoreTimeScale();

    private void RestoreTimeScale()
    {
        if (!ownsPause)
            return;
        Time.timeScale = previousTimeScale;
        ownsPause = false;
    }

    private static Button Button(
        string name,
        Transform parent,
        string label,
        Vector2 min,
        Vector2 max,
        Color color)
    {
        GameObject buttonObject = Rect(name, parent, min, max);
        Image image = buttonObject.AddComponent<Image>();
        image.color = color;
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        TMP_Text text = Text(
            "Label",
            buttonObject.transform,
            label,
            27f,
            TextAlignmentOptions.Center,
            Color.white);
        SetRect(text.rectTransform, Vector2.zero, Vector2.one);
        return button;
    }

    private static TMP_Text Text(
        string name,
        Transform parent,
        string value,
        float size,
        TextAlignmentOptions alignment,
        Color color)
    {
        TMP_Text text = Rect(name, parent, Vector2.zero, Vector2.one).AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }

    private static GameObject Rect(string name, Transform parent, Vector2 min, Vector2 max)
    {
        GameObject target = new GameObject(name, typeof(RectTransform));
        target.transform.SetParent(parent, false);
        SetRect((RectTransform)target.transform, min, max);
        return target;
    }

    private static void SetRect(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
