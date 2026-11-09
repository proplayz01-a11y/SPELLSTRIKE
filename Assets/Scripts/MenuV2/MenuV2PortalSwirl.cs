using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives a masked inward-flow vortex from the Adventure hover state.
/// A private material instance prevents scene-to-scene material side effects.
/// </summary>
public sealed class MenuV2PortalSwirl : MonoBehaviour
{
    [SerializeField] private Image portal;
    [SerializeField] private MenuV2HoverTarget adventureHotspot;
    [SerializeField] private float changeSpeed = 3.2f;
    private Material instance;
    private float hovered;

    public void Configure(Image image, MenuV2HoverTarget hover)
    {
        portal = image;
        adventureHotspot = hover;
    }

    private void OnEnable()
    {
        if (portal == null || portal.material == null)
            return;
        instance = new Material(portal.material);
        portal.material = instance;
    }

    private void Update()
    {
        float target = adventureHotspot != null && adventureHotspot.IsHovered ? 1f : 0f;
        hovered = Mathf.MoveTowards(hovered, target,
            Mathf.Max(0.01f, changeSpeed) * Time.unscaledDeltaTime);
        if (instance != null)
            instance.SetFloat("_HoverFactor", hovered);
    }

    private void OnDisable()
    {
        if (portal != null && instance != null)
            portal.material = null;
        if (instance != null)
            Destroy(instance);
        instance = null;
    }
}
