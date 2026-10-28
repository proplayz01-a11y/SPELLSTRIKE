using TMPro;
using UnityEngine;

public sealed class TutorialHUDController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private CanvasGroup rootCanvasGroup;
    [SerializeField] private GameObject keyboardPrompt;
    [SerializeField] private GameObject mobilePrompt;
    [SerializeField] private GameObject animatedPointer;
    [SerializeField] private GameObject tileLessonImage;

    [Header("Copy")]
    [SerializeField] private TMP_Text stepText;
    [SerializeField] private TMP_Text primaryObjectiveText;
    [SerializeField] private TMP_Text secondaryObjectiveText;
    [SerializeField] private TMP_Text dialogueText;

    [Header("Progress")]
    [SerializeField] private UnityEngine.UI.Image[] progressPips;
    [SerializeField] private Color completedPipColor = new(0.95f, 0.68f, 0.20f, 1f);
    [SerializeField] private Color pendingPipColor = new(0.06f, 0.13f, 0.18f, 0.92f);

    public void SetVisible(bool visible)
    {
        if (rootCanvasGroup == null)
        {
            gameObject.SetActive(visible);
            return;
        }

        rootCanvasGroup.alpha = visible ? 1f : 0f;
        rootCanvasGroup.interactable = visible;
        // The tutorial HUD is informational. A dedicated ContinueButton handles
        // click-to-advance panels so decorative graphics never block gameplay UI.
        rootCanvasGroup.blocksRaycasts = false;
    }

    public void SetStep(int currentStep, int totalSteps)
    {
        int safeTotal = Mathf.Max(1, totalSteps);
        int safeCurrent = Mathf.Clamp(currentStep, 1, safeTotal);

        if (stepText != null)
            stepText.text = $"TUTORIAL {safeCurrent}/{safeTotal}";

        for (int i = 0; i < progressPips.Length; i++)
        {
            if (progressPips[i] != null)
                progressPips[i].color = i < safeCurrent ? completedPipColor : pendingPipColor;
        }
    }

    public void SetObjectives(string primary, string secondary = "")
    {
        if (primaryObjectiveText != null)
            primaryObjectiveText.text = primary;

        if (secondaryObjectiveText != null)
        {
            secondaryObjectiveText.text = secondary;
            secondaryObjectiveText.gameObject.SetActive(!string.IsNullOrWhiteSpace(secondary));
        }
    }

    public void SetDialogue(string message)
    {
        if (dialogueText != null)
            dialogueText.text = message;
    }

    public void SetPlatformPrompts(bool useMobileControls)
    {
#if UNITY_EDITOR
        useMobileControls |= TutorialResponsiveLayout.IsMobilePresentation;
#endif
        if (keyboardPrompt != null)
            keyboardPrompt.SetActive(!useMobileControls);
        if (mobilePrompt != null)
            mobilePrompt.SetActive(useMobileControls);
    }

    public void ShowTileLessonImage(bool visible)
    {
        if (tileLessonImage != null)
            tileLessonImage.SetActive(visible);
    }

    public void PointAt(RectTransform target)
    {
        if (animatedPointer == null)
            return;

        if (target == null)
        {
            animatedPointer.SetActive(false);
            return;
        }

        animatedPointer.SetActive(true);
        RectTransform pointerRect = animatedPointer.transform as RectTransform;
        if (pointerRect != null)
        {
            pointerRect.SetParent(target, false);
            pointerRect.anchorMin = new Vector2(0.5f, 1f);
            pointerRect.anchorMax = new Vector2(0.5f, 1f);
            pointerRect.anchoredPosition = new Vector2(0f, 36f);
        }
    }
}
