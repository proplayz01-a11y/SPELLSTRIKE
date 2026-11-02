using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Owns an independent material instance per selectable card. Only the mask alpha
/// illuminates; the underlying gold sprite is never changed or hidden.
/// </summary>
[DisallowMultipleComponent]
public sealed class ModeSelectGoldBorderHover : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private Image reflectionOverlay;
    [SerializeField] private Material reflectionTemplate;
    [SerializeField, Min(0.1f)] private float clockwiseDuration = 1f;
    [SerializeField, Range(0.002f, 0.15f)] private float bandWidth = 0.034f;
    [SerializeField, Range(0.002f, 0.1f)] private float softness = 0.022f;
    [SerializeField] private Color reflectionTint = new Color(1f, 0.93f, 0.76f, 1f);
    [SerializeField, Range(0f, 4f)] private float intensity = 1.8f;
    [SerializeField, Min(0.025f)] private float fadeOutSeconds = 0.12f;
    [SerializeField, Range(0f, 0.5f)] private float steadyHoverGlow = 0.075f;
    [SerializeField] private bool loopWhileHovered = true;

    private Material owned;
    private bool pointerInside, selected;
    private bool sweeping;
    private float elapsed, currentIntensity, currentAmbient;

    public void SetReferences(Image image, Material source)
    {
        reflectionOverlay = image;
        reflectionTemplate = source;
    }

    public void SetContinuousHover(bool enabled) { loopWhileHovered = enabled; }

    private void OnEnable()
    {
        if (!Application.isPlaying || reflectionOverlay == null || reflectionTemplate == null) return;
        owned = new Material(reflectionTemplate) { name = reflectionTemplate.name + " (" + gameObject.name + " owned)" };
        reflectionOverlay.material = owned;
        reflectionOverlay.raycastTarget = false;
        ResetProperties();
    }

    private void ResetProperties()
    {
        pointerInside = selected = sweeping = false;
        elapsed = currentIntensity = currentAmbient = 0f;
        if (owned != null)
        {
            owned.SetFloat("_Phase", 0f);
            owned.SetFloat("_Intensity", 0f);
            owned.SetFloat("_AmbientIntensity", 0f);
        }
    }

    public void OnPointerEnter(PointerEventData data) { pointerInside = true; BeginSweep(); }
    public void OnPointerExit(PointerEventData data) { pointerInside = false; if (!selected) sweeping = false; }
    public void OnSelect(BaseEventData data) { selected = true; BeginSweep(); }
    public void OnDeselect(BaseEventData data) { selected = false; if (!pointerInside) sweeping = false; }

    private void BeginSweep()
    {
        if (owned == null) return;
        elapsed = 0f;
        sweeping = true;
        currentIntensity = intensity;
        owned.SetColor("_Tint", reflectionTint);
        owned.SetFloat("_SweepWidth", bandWidth);
        owned.SetFloat("_Softness", softness);
    }

    private void Update()
    {
        if (owned == null) return;
        bool focused = pointerInside || selected;
        if (focused && sweeping)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / clockwiseDuration;
            if (t >= 1f)
            {
                if (loopWhileHovered) elapsed = Mathf.Repeat(elapsed, clockwiseDuration);
                else { sweeping = false; t = 1f; }
            }
            owned.SetFloat("_Phase", Mathf.Repeat(t, 1f));
        }
        float desired = focused && sweeping ? intensity : 0f;
        float targetAmbient = focused && !sweeping ? steadyHoverGlow : 0f;
        currentIntensity = Mathf.MoveTowards(currentIntensity, desired,
            intensity * Time.unscaledDeltaTime / fadeOutSeconds);
        currentAmbient = Mathf.MoveTowards(currentAmbient, targetAmbient,
            Mathf.Max(steadyHoverGlow, .01f) * Time.unscaledDeltaTime / fadeOutSeconds);
        owned.SetFloat("_Intensity", currentIntensity);
        owned.SetFloat("_AmbientIntensity", currentAmbient);
    }

    private void OnDisable() { DestroyOwned(); ResetProperties(); }
    private void OnDestroy() { DestroyOwned(); }
    private void DestroyOwned()
    {
        if (owned == null) return;
        if (reflectionOverlay != null) reflectionOverlay.material = reflectionTemplate;
        Destroy(owned);
        owned = null;
    }
}
