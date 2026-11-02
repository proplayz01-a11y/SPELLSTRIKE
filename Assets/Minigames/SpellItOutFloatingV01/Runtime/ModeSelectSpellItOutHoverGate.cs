using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Runs the card's tile motion only while its interactable button is hovered.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(UnityEngine.UI.Button))]
public sealed class ModeSelectSpellItOutHoverGate : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private ModeSelectSpellItOutTileMotion motion;
    private UnityEngine.UI.Button button;
    private bool hovered;

    public bool IsHovered => hovered;

    private void OnEnable()
    {
        button = GetComponent<UnityEngine.UI.Button>();
        StopAndRestore();
    }

    private void OnDisable()
    {
        StopAndRestore();
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused) StopAndRestore();
    }

    private void Update()
    {
        if (hovered && (!button || !button.isActiveAndEnabled || !button.IsInteractable() || !motion || !motion.isActiveAndEnabled))
            StopAndRestore();
    }

    public void Configure(ModeSelectSpellItOutTileMotion newMotion)
    {
        StopAndRestore();
        motion = newMotion;
        button = GetComponent<UnityEngine.UI.Button>();
        if (motion)
            motion.PauseAndRestore();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isActiveAndEnabled || !button || !button.isActiveAndEnabled || !button.IsInteractable() || !motion || !motion.isActiveAndEnabled)
            return;
        hovered = true;
        motion.Resume();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isActiveAndEnabled)
            return;
        StopAndRestore();
    }

    private void StopAndRestore()
    {
        hovered = false;
        if (motion)
            motion.PauseAndRestore();
    }
}
