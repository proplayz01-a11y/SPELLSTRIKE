using UnityEngine;

/// <summary>
/// Tracks the unmodified collectible visual's size so a scene installer can
/// safely apply the approved size multiple times without compounding.
/// Attach to FragmentCrystal_Visual, NOT the pickup/collider parent.
/// </summary>
[DisallowMultipleComponent]
public sealed class FragmentCrystalSizeStamp : MonoBehaviour
{
    // Previous authored scale (108%) plus a further 12%, not 12 percentage points.
    public const float ApprovedMultiplier = 1.08f * 1.12f;
    [SerializeField] private Vector3 originalVisualScale;
    [SerializeField, Range(1f, 1.25f)] private float sizeMultiplier = ApprovedMultiplier;
    [SerializeField] private bool initialized;

    public float Multiplier => sizeMultiplier;

    private void Awake()
    {
        // Existing campaign scenes have the earlier 1.08 stamp serialized.
        // Migrate visuals when loaded, including late-activated collectibles.
        // The stored original scale is retained across retries and replays.
        SetSize(ApprovedMultiplier);
    }

    public void SetSize(float multiplier)
    {
        if (!initialized)
        {
            originalVisualScale = transform.localScale;
            initialized = true;
        }
        sizeMultiplier = Mathf.Clamp(multiplier, 1f, 1.25f);
        transform.localScale = Vector3.Scale(originalVisualScale,
            Vector3.one * sizeMultiplier);
    }

    private void OnValidate()
    {
        if (initialized)
            transform.localScale = originalVisualScale * sizeMultiplier;
    }
}
