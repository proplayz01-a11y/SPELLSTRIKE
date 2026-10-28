using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Visual-only smooth crossfade for the Vocabulary Review Continue button.
/// Does not affect the button's click events or review progression.
/// </summary>
[DisallowMultipleComponent]
public sealed class StageVocabularyContinueHover : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Button button;
    [SerializeField] private Image hoverOverlay;
    [SerializeField, Range(0.08f, 0.6f)] private float fadeInSeconds = 0.22f;
    [SerializeField, Range(0.08f, 0.6f)] private float fadeOutSeconds = 0.28f;

    private bool hovered;
    private bool available;
    private float alpha;

#if UNITY_EDITOR
    public void Configure(Button target, Image graphic)
    {
        button = target;
        hoverOverlay = graphic;
        if (hoverOverlay != null)
        {
            hoverOverlay.raycastTarget = false;
            hoverOverlay.color = new Color(1f, 1f, 1f, 0f);
        }
    }
#endif

    public void SetAvailable(bool enabled)
    {
        available = enabled;
        if (!enabled) hovered = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
    }

    private void Update()
    {
        if (hoverOverlay == null) return;
        bool show = hovered && available && button != null && button.IsInteractable();
        float target = show ? 1f : 0f;
        float duration = show ? fadeInSeconds : fadeOutSeconds;
        alpha = Mathf.MoveTowards(alpha, target, Time.unscaledDeltaTime / duration);
        hoverOverlay.color = new Color(1f, 1f, 1f, alpha);
    }

    private void OnDisable()
    {
        hovered = false;
        alpha = 0f;
        if (hoverOverlay != null)
            hoverOverlay.color = new Color(1f, 1f, 1f, 0f);
    }
}
