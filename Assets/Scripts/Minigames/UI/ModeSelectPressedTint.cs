using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Restrained pressed-state feedback for the approved minigame-selection artwork.
/// Darkens existing independent graphics while the pointer is held down.
/// Keeps the continuously animated gold-reflection overlay and navigation intact.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class ModeSelectPressedTint : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IBeginDragHandler
{
    [SerializeField, Range(0.65f, 1f)] private float heldBrightness = 0.83f;
    [SerializeField, Min(0.01f)] private float pressSeconds = 0.075f;
    [SerializeField, Min(0.01f)] private float releaseSeconds = 0.12f;

    private readonly List<Graphic> graphics = new List<Graphic>();
    private readonly List<Color> originalColors = new List<Color>();
    private Button button;
    private bool isHeld;
    private float pressedAmount;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (button == null) button = GetComponent<Button>();
        graphics.Clear();
        originalColors.Clear();
        foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true))
        {
            if (graphic == null || graphic.transform == transform ||
                !graphic.gameObject.activeInHierarchy ||
                graphic.gameObject.name == "GoldReflection_Masked") continue;
            graphics.Add(graphic);
            originalColors.Add(graphic.color);
        }
        isHeld = false;
        pressedAmount = 0f;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (button == null || !button.IsActive() || !button.IsInteractable() ||
            eventData == null || eventData.button != PointerEventData.InputButton.Left) return;
        isHeld = true;
    }

    public void OnPointerUp(PointerEventData eventData) { isHeld = false; }
    public void OnPointerExit(PointerEventData eventData) { isHeld = false; }
    public void OnBeginDrag(PointerEventData eventData) { isHeld = false; }

    private void Update()
    {
        if (isHeld && (button == null || !button.IsActive() || !button.IsInteractable()))
            isHeld = false;

        float target = isHeld ? 1f : 0f;
        float duration = isHeld ? pressSeconds : releaseSeconds;
        float next = Mathf.MoveTowards(pressedAmount, target, Time.unscaledDeltaTime / duration);
        if (Mathf.Approximately(pressedAmount, next)) return;
        pressedAmount = next;
        float brightness = Mathf.Lerp(1f, heldBrightness, pressedAmount);

        for (int i = 0; i < graphics.Count; i++)
        {
            Graphic graphic = graphics[i];
            if (graphic == null) continue;
            Color original = originalColors[i];
            graphic.color = new Color(original.r * brightness, original.g * brightness,
                original.b * brightness, original.a);
        }
    }

    private void OnDisable()
    {
        isHeld = false;
        pressedAmount = 0f;
        for (int i = 0; i < graphics.Count; i++)
            if (graphics[i] != null) graphics[i].color = originalColors[i];
        graphics.Clear();
        originalColors.Clear();
    }
}
