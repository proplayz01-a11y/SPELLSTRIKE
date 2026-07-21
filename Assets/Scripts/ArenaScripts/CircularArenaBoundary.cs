using UnityEngine;

[DisallowMultipleComponent]
public class CircularArenaBoundary : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private bool syncVisualRootActiveState = true;

    [Header("Collider Ring")]
    [SerializeField, Min(1f)] private float radius = 10f;
    [SerializeField, Min(0.25f)] private float wallHeight = 6f;
    [SerializeField, Min(0.1f)] private float wallThickness = 1f;
    [SerializeField] private float verticalCenter = 3f;
    [SerializeField, Range(8, 96)] private int segmentCount = 32;

    [Header("Generation")]
    [SerializeField] private bool generateOnAwake = true;
    [SerializeField] private bool rebuildOnEnable;
    [SerializeField] private string segmentNamePrefix = "GeneratedBoundarySegment";

    private void Awake()
    {
        if (generateOnAwake)
        {
            RebuildBoundary();
        }
    }

    private void OnEnable()
    {
        SetVisualRootActive(true);

        if (rebuildOnEnable)
        {
            RebuildBoundary();
        }
    }

    private void OnDisable()
    {
        SetVisualRootActive(false);
    }

    [ContextMenu("Rebuild Boundary")]
    public void RebuildBoundary()
    {
        ClearGeneratedSegments();

        int safeSegmentCount = Mathf.Max(8, segmentCount);
        float angleStep = 360f / safeSegmentCount;
        float tangentLength = 2f * radius * Mathf.Tan(Mathf.PI / safeSegmentCount);
        tangentLength += wallThickness * 0.35f;

        for (int i = 0; i < safeSegmentCount; i++)
        {
            float angle = angleStep * i;
            float radians = angle * Mathf.Deg2Rad;
            Vector3 radialDirection = new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians));

            GameObject segment = new GameObject($"{segmentNamePrefix}_{i:00}");
            segment.transform.SetParent(transform, false);
            segment.transform.localPosition = radialDirection * radius + Vector3.up * verticalCenter;
            segment.transform.localRotation = Quaternion.LookRotation(radialDirection, Vector3.up);

            BoxCollider collider = segment.AddComponent<BoxCollider>();
            collider.isTrigger = false;
            collider.size = new Vector3(tangentLength, wallHeight, wallThickness);
        }
    }

    [ContextMenu("Clear Generated Boundary")]
    public void ClearGeneratedSegments()
    {
        string safePrefix = string.IsNullOrWhiteSpace(segmentNamePrefix)
            ? "GeneratedBoundarySegment"
            : segmentNamePrefix;

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (!child.name.StartsWith(safePrefix)) continue;

            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    private void SetVisualRootActive(bool active)
    {
        if (!syncVisualRootActiveState || visualRoot == null) return;
        visualRoot.SetActive(active);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
