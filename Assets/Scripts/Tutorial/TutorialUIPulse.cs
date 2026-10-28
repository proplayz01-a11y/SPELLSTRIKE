using UnityEngine;

public sealed class TutorialUIPulse : MonoBehaviour
{
    [SerializeField] private float distance = 12f;
    [SerializeField] private float speed = 2.5f;
    [SerializeField] private bool pulseScale = true;
    [SerializeField] private float scaleAmount = 0.08f;

    private RectTransform rectTransform;
    private Vector2 startPosition;
    private Vector3 startScale;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        if (rectTransform == null)
            return;

        startPosition = rectTransform.anchoredPosition;
        startScale = rectTransform.localScale;
    }

    private void OnEnable()
    {
        if (rectTransform == null)
            rectTransform = transform as RectTransform;

        if (rectTransform == null)
            return;

        startPosition = rectTransform.anchoredPosition;
        startScale = rectTransform.localScale;
    }

    private void Update()
    {
        if (rectTransform == null)
            return;

        float wave = (Mathf.Sin(Time.unscaledTime * speed) + 1f) * 0.5f;
        rectTransform.anchoredPosition = startPosition + Vector2.down * (wave * distance);

        if (pulseScale)
            rectTransform.localScale = startScale * (1f + wave * scaleAmount);
    }
}
