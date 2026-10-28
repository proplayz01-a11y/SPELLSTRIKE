using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Stage1DistanceCullingController : MonoBehaviour
{
    [Serializable]
    private sealed class CullEntry
    {
        public Renderer renderer;
        public float maxDistance;
    }

    [Header("Distances")]
    [SerializeField] private float tinyDecorDistance = 80f;
    [SerializeField] private float smallPropDistance = 140f;
    [SerializeField] private float mediumPropDistance = 220f;
    [SerializeField] private float structureDistance = 320f;
    [SerializeField] private float landmarkDistance = 420f;
    [SerializeField] private float majorEnvironmentDistance = 500f;

    [Header("Runtime")]
    [SerializeField, Min(0.05f)] private float updateInterval = 0.2f;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool logClassificationSummary = true;

    private readonly List<CullEntry> entries = new();
    private float nextUpdateTime;

    private static readonly string[] TinyKeywords = { "grass", "mushroom", "flower", "pebble", "twig", "weed" };
    private static readonly string[] SmallKeywords = { "crate", "barrel", "sign", "sack", "pot", "lantern", "torch", "bucket" };
    private static readonly string[] MediumKeywords = { "cart", "stall", "bench", "table", "chair", "chest", "wagon" };
    private static readonly string[] LandmarkKeywords = { "tower", "castle", "church", "windmill", "gate", "bridge", "landmark" };
    // Scene-defining geometry that should stay until the camera's real render limit.
    private static readonly string[] AlwaysVisibleKeywords =
    {
        "terrain", "ground", "road", "path", "floor", "landscape", "plane", "water", "wall"
    };
    // Houses and rocks are intentionally kept much farther than ordinary props.
    // Their LODGroups still reduce mesh cost; this controller only hides them at 500m.
    private static readonly string[] MajorEnvironmentKeywords =
    {
        "house", "building", "hut", "rock", "boulder", "cliff", "stone"
    };

    private void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        BuildEntries();
        ApplyCulling(true);
    }

    private void OnEnable() => nextUpdateTime = 0f;

    private void LateUpdate()
    {
        if (Time.unscaledTime < nextUpdateTime) return;
        nextUpdateTime = Time.unscaledTime + updateInterval;
        ApplyCulling(false);
    }

    private void OnDisable() => RestoreAllRenderers();
    private void OnDestroy() => RestoreAllRenderers();

    private void BuildEntries()
    {
        entries.Clear();
        int tiny = 0, small = 0, medium = 0, structure = 0, landmark = 0, alwaysVisible = 0, wallExcluded = 0;

        var rendererSet = new HashSet<Renderer>(GetComponentsInChildren<Renderer>(true));
        var wallRenderers = new HashSet<Renderer>();
        foreach (LODGroup lodGroup in GetComponentsInChildren<LODGroup>(true))
        {
            bool isStoneWallGroup = lodGroup.name.StartsWith("StoneWall_LODGroup_", StringComparison.OrdinalIgnoreCase);
            foreach (LOD lod in lodGroup.GetLODs())
            {
                foreach (Renderer lodRenderer in lod.renderers)
                {
                    if (lodRenderer == null) continue;

                    rendererSet.Add(lodRenderer);
                    if (isStoneWallGroup)
                        wallRenderers.Add(lodRenderer);
                }
            }
        }

        foreach (Renderer renderer in rendererSet)
        {
            if (renderer == null) continue;

            // Stone-wall LODGroups reference GLB renderers rather than parenting them,
            // so renderer-name / hierarchy keyword checks cannot reliably identify them.
            // Keep those renderers out of category distance culling entirely and let
            // LOD, baked occlusion, frustum culling, and the camera far clip own them.
            if (wallRenderers.Contains(renderer))
            {
                renderer.forceRenderingOff = false;
                alwaysVisible++;
                wallExcluded++;
                continue;
            }

            float distance = ClassifyDistance(renderer, out bool excluded);
            if (excluded)
            {
                renderer.forceRenderingOff = false;
                alwaysVisible++;
                continue;
            }

            entries.Add(new CullEntry { renderer = renderer, maxDistance = distance });
            if (Mathf.Approximately(distance, tinyDecorDistance)) tiny++;
            else if (Mathf.Approximately(distance, smallPropDistance)) small++;
            else if (Mathf.Approximately(distance, mediumPropDistance)) medium++;
            else if (Mathf.Approximately(distance, landmarkDistance)) landmark++;
            else structure++;
        }

        if (logClassificationSummary)
            Debug.Log($"[Stage1 Distance Culling] Classified {entries.Count} renderers | tiny={tiny}, small={small}, medium={medium}, structure={structure}, landmark={landmark}, alwaysVisible={alwaysVisible}, wallExcluded={wallExcluded}", this);
    }

    private float ClassifyDistance(Renderer renderer, out bool excluded)
    {
        string searchName = BuildSearchName(renderer).ToLowerInvariant();
        Vector3 size = renderer.bounds.size;
        float maxDimension = Mathf.Max(size.x, Mathf.Max(size.y, size.z));

        if (ContainsAny(searchName, AlwaysVisibleKeywords) || maxDimension >= 200f)
        {
            excluded = true;
            return float.PositiveInfinity;
        }

        excluded = false;
        if (ContainsAny(searchName, MajorEnvironmentKeywords)) return majorEnvironmentDistance;
        if (ContainsAny(searchName, LandmarkKeywords) || maxDimension > 90f) return landmarkDistance;
        if (ContainsAny(searchName, TinyKeywords)) return tinyDecorDistance;
        if (ContainsAny(searchName, SmallKeywords)) return smallPropDistance;
        if (ContainsAny(searchName, MediumKeywords)) return mediumPropDistance;
        if (maxDimension <= 15f) return tinyDecorDistance;
        if (maxDimension <= 30f) return smallPropDistance;
        if (maxDimension <= 45f) return mediumPropDistance;
        return structureDistance;
    }

    private static string BuildSearchName(Renderer renderer)
    {
        Transform t = renderer.transform;
        string result = renderer.name;
        for (int depth = 0; t != null && depth < 4; depth++)
        {
            result += " " + t.name;
            t = t.parent;
        }
        return result;
    }

    private static bool ContainsAny(string value, string[] keywords)
    {
        foreach (string keyword in keywords)
            if (value.Contains(keyword)) return true;
        return false;
    }

    private void ApplyCulling(bool force)
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null) return;

        Vector3 cameraPosition = targetCamera.transform.position;
        foreach (CullEntry entry in entries)
        {
            Renderer renderer = entry.renderer;
            if (renderer == null) continue;

            Vector3 closestPoint = renderer.bounds.ClosestPoint(cameraPosition);
            bool shouldCull = (closestPoint - cameraPosition).sqrMagnitude > entry.maxDistance * entry.maxDistance;
            if (force || renderer.forceRenderingOff != shouldCull)
                renderer.forceRenderingOff = shouldCull;
        }
    }

    private void RestoreAllRenderers()
    {
        foreach (CullEntry entry in entries)
            if (entry.renderer != null) entry.renderer.forceRenderingOff = false;
    }
}
