using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// An isolated, reusable hover/press controller for illustrated Main Menu V2 objects.
/// Keeps the approved artwork stationary; only blends a hovered sprite/label.
/// </summary>
public sealed class MenuV2HoverTarget : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler,
    IPointerUpHandler, IPointerClickHandler, ISelectHandler, IDeselectHandler
{
    public enum TargetAction { Adventure, Minigame, Spellbook, Options, Quit }

    [SerializeField] private TargetAction action;
    [SerializeField] private Image highlightedImage;
    [SerializeField] private CanvasGroup labelGroup;
    [SerializeField] private AdventureButtonHandler adventureHandler;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject quitConfirmationPanel;
    [SerializeField] private MainMenuSpellbookUI spellbookUI;
    [SerializeField] private string minigameScene = "MinigameModeSelectScene";
    [SerializeField, Min(0.1f)] private float fadeSpeed = 6f;
    [SerializeField, Range(0.1f, 1f)] private float hoverOpacity = 0.83f;

    private bool hovered;
    private bool pressed;

    public bool IsHovered => hovered;

    public void Configure(TargetAction targetAction, Image hoverImage,
        CanvasGroup label, AdventureButtonHandler handler,
        GameObject options, GameObject quit, MainMenuSpellbookUI book)
    {
        action = targetAction;
        highlightedImage = hoverImage;
        labelGroup = label;
        adventureHandler = handler;
        optionsPanel = options;
        quitConfirmationPanel = quit;
        spellbookUI = book;
    }

    private void Awake()
    {
        if (highlightedImage != null)
        {
            highlightedImage.raycastTarget = false;
            Color color = highlightedImage.color;
            color.a = 0f;
            highlightedImage.color = color;
        }
        if (labelGroup != null)
        {
            labelGroup.alpha = 0f;
            labelGroup.interactable = false;
            labelGroup.blocksRaycasts = false;
        }
    }

    private void Update()
    {
        float progress = fadeSpeed * Time.unscaledDeltaTime;
        float target = hovered || pressed ? hoverOpacity : 0f;
        if (highlightedImage != null)
        {
            Color color = highlightedImage.color;
            color.a = Mathf.MoveTowards(color.a, target, progress);
            highlightedImage.color = color;
        }
        if (labelGroup != null)
            labelGroup.alpha = Mathf.MoveTowards(
                labelGroup.alpha, hovered || pressed ? 1f : 0f, progress);
    }

    public void OnPointerEnter(PointerEventData eventData) => hovered = true;
    public void OnPointerExit(PointerEventData eventData) { hovered = false; pressed = false; }
    public void OnPointerDown(PointerEventData eventData) => pressed = true;
    public void OnPointerUp(PointerEventData eventData) => pressed = false;
    public void OnSelect(BaseEventData eventData) => hovered = true;
    public void OnDeselect(BaseEventData eventData) => hovered = false;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        switch (action)
        {
            case TargetAction.Adventure:
                if (adventureHandler != null)
                    adventureHandler.LoadAdventure();
                else
                    Debug.LogError("[MenuV2] AdventureButtonHandler is not assigned.");
                break;
            case TargetAction.Minigame:
                if (Application.CanStreamedLevelBeLoaded(minigameScene))
                    SceneManager.LoadScene(minigameScene);
                else
                    Debug.LogError("[MenuV2] Missing minigame scene in Build Settings: " + minigameScene);
                break;
            case TargetAction.Spellbook:
                if (spellbookUI != null)
                    spellbookUI.Open();
                else
                    Debug.LogWarning("[MenuV2] Spellbook 3D/Chronicle binding not installed yet.");
                break;
            case TargetAction.Options:
                if (optionsPanel != null)
                    optionsPanel.SetActive(true);
                break;
            case TargetAction.Quit:
                if (quitConfirmationPanel != null)
                    quitConfirmationPanel.SetActive(true);
                break;
        }
    }
}
