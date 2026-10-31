using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main Menu V05 portal: no idle motion; original smooth swirl only on hover.
/// Its own material and shader avoid changing V04 behavior.
/// </summary>
public sealed class MenuV2PortalHoverOnly : MonoBehaviour
{
    [SerializeField] private Image portal;
    [SerializeField] private MenuV2HoverTarget adventureHotspot;
    [SerializeField, Min(0.05f)] private float acceleration = 3.2f;
    [SerializeField, Min(0f)] private float hoverFlowSpeed = 1f;

    private Material instance;
    private Material original;
    private float hoverBlend;
    private float phase;

    public float FlowPhase => phase;
    public float HoverBlend => hoverBlend;
    public bool IsActivelySpinning => hoverBlend > .0001f;

    public void Configure(Image portalImage, MenuV2HoverTarget hover)
    {
        portal = portalImage;
        adventureHotspot = hover;
    }

    private void OnEnable()
    {
        if (portal == null || portal.material == null)
        {
            Debug.LogWarning("[MenuV2] Hover-only portal missing its material.");
            return;
        }

        original = portal.material;
        instance = new Material(original);
        instance.SetFloat("_FlowPhase", 0f);
        instance.SetFloat("_HoverFactor", 0f);
        phase = hoverBlend = 0f;
        portal.material = instance;
    }

    private void Update()
    {
        if (instance == null) return;
        bool pointed = adventureHotspot != null && adventureHotspot.IsHovered;
        hoverBlend = Mathf.MoveTowards(hoverBlend, pointed ? 1f : 0f,
            acceleration * Time.unscaledDeltaTime);
        phase += hoverBlend * hoverFlowSpeed * Time.unscaledDeltaTime;
        instance.SetFloat("_FlowPhase", phase);
        instance.SetFloat("_HoverFactor", hoverBlend);
    }

    private void OnDisable()
    {
        if (portal != null && instance != null)
            portal.material = original;
        if (instance != null)
            Destroy(instance);
        instance = null;
        original = null;
    }
}
