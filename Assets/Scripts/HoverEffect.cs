using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class HoverEffect : MonoBehaviour
{
    [Header("Scale Settings")]
    public float hoverScale = 1.15f;
    public float animSpeed = 8f;

    [Header("Glow Settings")]
    public Image glowImage;
    public Color glowColor = new Color(1f, 0.85f, 0f, 0.6f);
    public float glowFadeSpeed = 6f;

    private Vector3 originalScale;
    private Vector3 targetScale;
    private float targetGlowAlpha = 0f;

    void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;

        if (glowImage != null)
        {
            Color c = glowColor;
            c.a = 0f;
            glowImage.color = c;
        }
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.deltaTime * animSpeed
        );

        if (glowImage != null)
        {
            Color current = glowImage.color;
            current.a = Mathf.Lerp(current.a, targetGlowAlpha, Time.deltaTime * glowFadeSpeed);
            glowImage.color = current;
        }
    }

    // Called by Event Trigger - Pointer Enter
    public void OnHoverEnter()
    {
        Debug.Log("HOVER ENTER triggered!");
        targetScale = originalScale * hoverScale;
        targetGlowAlpha = 0.8f;
    }

    // Called by Event Trigger - Pointer Exit
    public void OnHoverExit()
    {
        Debug.Log("HOVER EXIT triggered!");
        targetScale = originalScale;
        targetGlowAlpha = 0f;
    }
}