using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Optional graphics-only adapter for the isolated reference-art review scene.
/// No vocabulary, scoring, progression or input ownership.
/// The normal StageVocabularyReviewController remains the sole authority.
/// </summary>
[DisallowMultipleComponent]
public sealed class StageVocabularyReviewArtSkin : MonoBehaviour
{
    [SerializeField] private Sprite answerNormal;
    [SerializeField] private Sprite answerHover;
    [SerializeField] private Sprite answerSelected;
    [SerializeField] private Sprite answerCorrect;
    [SerializeField] private Sprite answerIncorrect;
    [SerializeField] private Sprite progressPending;
    [SerializeField] private Sprite progressCurrent;
    [SerializeField] private Sprite progressCompleted;
    [SerializeField] private Sprite continueNormal;
    [SerializeField] private Sprite continueHover;
    [SerializeField] private Sprite continueDisabled;
    [SerializeField] private Image feedbackAccent;
    [SerializeField] private Sprite feedbackCorrectAccent;
    [SerializeField] private Sprite feedbackIncorrectAccent;
    [SerializeField] private Image[] answerTintOverlays = new Image[4];
    [SerializeField] private GameObject progressRow;
    [SerializeField] private StageVocabularyContinueHover continueHoverAnimator;

    private static readonly Color CorrectTint = new Color(0.19f, 0.80f, 0.29f, 0.45f);
    private static readonly Color IncorrectTint = new Color(0.91f, 0.17f, 0.19f, 0.46f);
    private static readonly Color InactiveTint = new Color(0.02f, 0.07f, 0.13f, 0.38f);

#if UNITY_EDITOR
    public void Configure(
        Sprite normal, Sprite hover, Sprite selected, Sprite correct, Sprite incorrect,
        Sprite pending, Sprite current, Sprite completed,
        Sprite nextNormal, Sprite nextHover, Sprite nextDisabled,
        Image accent, Sprite correctAccent, Sprite incorrectAccent)
    {
        answerNormal=normal; answerHover=hover; answerSelected=selected;
        answerCorrect=correct; answerIncorrect=incorrect;
        progressPending=pending; progressCurrent=current; progressCompleted=completed;
        continueNormal=nextNormal; continueHover=nextHover; continueDisabled=nextDisabled;
        feedbackAccent=accent; feedbackCorrectAccent=correctAccent;
        feedbackIncorrectAccent=incorrectAccent;
    }

    public void ConfigureFeedbackVisuals(Image[] overlays, GameObject diamondRow,
        StageVocabularyContinueHover smoothHover)
    {
        answerTintOverlays = overlays;
        progressRow = diamondRow;
        continueHoverAnimator = smoothHover;
    }
#endif

    public void ResetAnswerSprites(Button[] buttons)
    {
        if (answerNormal == null || buttons == null) return;
        for (int i = 0; i < buttons.Length; i++)
        {
            Button b = buttons[i];
            if (b == null || !(b.targetGraphic is Image img)) continue;
            img.sprite = answerNormal;
            img.color = Color.white;
            img.type = Image.Type.Sliced;
            var states = b.spriteState;
            states.highlightedSprite = answerHover;
            states.pressedSprite = answerSelected;
            states.selectedSprite = answerSelected;
            states.disabledSprite = answerNormal;
            b.spriteState = states;
            b.transition = Selectable.Transition.SpriteSwap;
            var badge = b.transform.Find("LetterBadge")?.GetComponent<Image>();
            if (badge != null) badge.color = Color.white;
            if (answerTintOverlays != null && i < answerTintOverlays.Length &&
                answerTintOverlays[i] != null)
                answerTintOverlays[i].enabled = false;
        }
    }

    /// <summary>
    /// Explicitly draws the answer state after buttons become noninteractable.
    /// Plain disabled SpriteSwap would otherwise replace a correct/wrong
    /// sprite with the neutral disabled graphic.
    /// </summary>
    public void ShowAnswerResult(Button[] buttons, int selectedIndex, int correctIndex)
    {
        if (buttons == null) return;
        for (int i = 0; i < buttons.Length; i++)
        {
            var b = buttons[i];
            if (b == null || !(b.targetGraphic is Image plaque)) continue;
            bool correct = i == correctIndex;
            bool wrong = i == selectedIndex && i != correctIndex;
            bool inactive = !correct && !wrong;
            b.transition = Selectable.Transition.None;
            plaque.sprite = correct ? answerCorrect :
                wrong ? answerIncorrect : answerNormal;
            plaque.type = Image.Type.Sliced;
            plaque.color = inactive ? new Color(0.57f, 0.60f, 0.64f) :
                correct ? new Color(0.78f, 1f, 0.80f) :
                new Color(1f, 0.82f, 0.82f);

            if (answerTintOverlays != null && i < answerTintOverlays.Length &&
                answerTintOverlays[i] != null)
            {
                Image tint = answerTintOverlays[i];
                tint.enabled = true;
                tint.color = correct ? CorrectTint :
                    wrong ? IncorrectTint : InactiveTint;
                tint.raycastTarget = false;
            }

            var badge = b.transform.Find("LetterBadge")?.GetComponent<Image>();
            if (badge != null)
                badge.color = inactive ? new Color(.60f,.63f,.67f,1f) : Color.white;
        }
    }

    public void SetSummaryMode(bool enabled)
    {
        if (progressRow != null)
            progressRow.SetActive(!enabled);
    }

    public bool SetAnswerFeedbackSprite(Button button, bool correct)
    {
        if (button == null || !(button.targetGraphic is Image image)) return false;
        Sprite target = correct ? answerCorrect : answerIncorrect;
        if (target == null) return false;
        button.transition = Selectable.Transition.None;
        image.sprite = target;
        image.color = Color.white;
        image.type = Image.Type.Sliced;
        return true;
    }

    public bool SetProgressSprite(Image fill, bool completed, bool current)
    {
        if (fill == null || progressPending == null ||
            progressCurrent == null || progressCompleted == null) return false;
        Image baseImage = fill.transform.parent != null ?
            fill.transform.parent.GetComponent<Image>() : null;
        if (baseImage != null)
        {
            baseImage.sprite = progressPending;
            baseImage.type = Image.Type.Simple;
            baseImage.preserveAspect = true;
            baseImage.color = Color.white;
            baseImage.enabled = true;
        }
        fill.enabled = completed || current;
        fill.sprite = completed ? progressCompleted : progressCurrent;
        fill.type = Image.Type.Simple;
        fill.preserveAspect = true;
        fill.color = Color.white;
        return true;
    }

    public void SetContinueSprite(Button button, bool available)
    {
        if (button == null || !(button.targetGraphic is Image img) ||
            continueNormal == null) return;
        img.sprite = available ? continueNormal :
            (continueDisabled != null ? continueDisabled : continueNormal);
        img.type = Image.Type.Sliced;
        img.color = Color.white;
        // Crossfade the overlay instead of abruptly swapping 0/1 hover images.
        if (continueHoverAnimator != null)
        {
            button.transition = Selectable.Transition.None;
            continueHoverAnimator.SetAvailable(available);
        }
        else
        {
            var state = button.spriteState;
            state.highlightedSprite = continueHover;
            state.pressedSprite = continueHover;
            state.selectedSprite = continueHover;
            state.disabledSprite = continueDisabled;
            button.spriteState = state;
            button.transition = Selectable.Transition.SpriteSwap;
        }
    }

    public void SetFeedbackAccent(bool? correct)
    {
        if (feedbackAccent == null) return;
        // Derived feedback artwork occupies the header/question region, never
        // the answer plaques. Keep the chosen/correct sprites unobstructed.
        Transform content = transform.Find("ReviewRoot/ReferenceContentRoot");
        if (content != null)
        {
            Transform header = content.Find("Header");
            Transform question = content.Find("QuestionText");
            if (header != null) header.gameObject.SetActive(!correct.HasValue);
            if (question != null) question.gameObject.SetActive(!correct.HasValue);
        }
        feedbackAccent.gameObject.SetActive(correct.HasValue);
        if (!correct.HasValue) return;
        feedbackAccent.sprite = correct.Value ? feedbackCorrectAccent : feedbackIncorrectAccent;
        feedbackAccent.color = Color.white;
        feedbackAccent.raycastTarget = false;
    }
}
