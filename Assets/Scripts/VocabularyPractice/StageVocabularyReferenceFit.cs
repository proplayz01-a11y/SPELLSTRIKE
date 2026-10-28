using UnityEngine;

/// <summary>Fits the 1672x941 reference proportionally inside the review Canvas.
/// Black exterior is supplied by the full-stretch ReviewRoot overlay.</summary>
[DisallowMultipleComponent]
public sealed class StageVocabularyReferenceFit : MonoBehaviour
{
    [SerializeField] private Vector2 referenceSize = new Vector2(1672f, 941f);

    private void LateUpdate()
    {
        if (!(transform is RectTransform rt) || !(transform.parent is RectTransform parent))
            return;
        var parentSize = parent.rect.size;
        if (parentSize.x < 1f || parentSize.y < 1f) return;
        float scale = Mathf.Min(parentSize.x / referenceSize.x,
            parentSize.y / referenceSize.y);
        if (scale <= 0f) return;
        var target = new Vector3(scale, scale, 1f);
        if ((rt.localScale - target).sqrMagnitude > 0.0000001f)
            rt.localScale = target;
    }
}
