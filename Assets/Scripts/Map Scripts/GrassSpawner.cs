using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SpellStrike — GrassSpawner.cs
/// GPU Instanced Grass Spawner with Paint Mask Texture
/// 
/// HOW TO USE:
///   1. Create empty GameObject, name it "GrassSpawner"
///   2. Attach this script
///   3. Assign in Inspector:
///      - Grass Prefab (LOD 0 from Illustrated Nature)
///      - Grass Mask (your GrassMask.png — must be Read/Write enabled!)
///      - Target Map (level1_REVAMPED)
///      - Grass Material (ToonGrass.shader material)
///   4. Tune Grass Count and other settings
///   5. Hit Play — grass spawns automatically
/// </summary>
public class GrassSpawner : MonoBehaviour
{
    [Header("─── Required References ───────────────────")]
    [Tooltip("The grass blade prefab (LOD 0 from Illustrated Nature)")]
    public GameObject grassPrefab;

    [Tooltip("Your painted GrassMask.png (Black = no grass, White = grass)")]
    public Texture2D grassMask;

    [Tooltip("The level1_REVAMPED parent GameObject")]
    public GameObject targetMap;

    [Tooltip("Material using ToonGrass.shader")]
    public Material grassMaterial;

    [Header("─── Grass Count & Density ──────────────────")]
    [Tooltip("Total number of grass instances to spawn. Start at 5000, increase if needed.")]
    [Range(100, 100000)]
    public int grassCount = 5000;

    [Tooltip("Minimum distance between each grass blade (avoids overlapping)")]
    [Range(1f, 50f)]
    public float minSpacing = 8f;

    [Header("─── Grass Blade Size ───────────────────────")]
    [Tooltip("Minimum scale of each grass blade")]
    public float minScale = 8f;

    [Tooltip("Maximum scale of each grass blade")]
    public float maxScale = 15f;

    [Tooltip("Random Y rotation per blade (degrees)")]
    public float randomRotation = 360f;

    [Header("─── Raycast Settings ───────────────────────")]
    [Tooltip("How high above the map to start the raycast (should be above highest point)")]
    public float raycastHeight = 2000f;

    [Tooltip("Layer mask for raycasting — set to your map's layer")]
    public LayerMask groundLayer = ~0; // Default: everything

    [Header("─── Slope Filter ───────────────────────────")]
    [Tooltip("Maximum slope angle to allow grass (degrees). 0 = flat only, 90 = everywhere")]
    [Range(0f, 90f)]
    public float maxSlopeAngle = 35f;

    [Header("─── Mask Threshold ─────────────────────────")]
    [Tooltip("How bright a pixel must be to count as 'grass area' (0-1). 0.5 = middle gray+")]
    [Range(0f, 1f)]
    public float maskThreshold = 0.5f;

    [Header("─── Debug ──────────────────────────────────")]
    public bool showDebugGizmos = true;
    public bool logSpawnStats = true;

    // ── Runtime ───────────────────────────────────────────────────────────────
    private List<GameObject> _spawnedGrass = new List<GameObject>();
    private Bounds _mapBounds;
    private bool _spawned = false;

    // ── Parent container for scene organization ───────────────────────────────
    private GameObject _grassContainer;

    void Start()
    {
        if (!ValidateReferences()) return;

        // Calculate total bounds of the map from all child renderers
        _mapBounds = CalculateMapBounds();

        // Enable Read/Write on mask at runtime (must also be set in Import Settings!)
        if (grassMask == null)
        {
            Debug.LogError("[GrassSpawner] No Grass Mask assigned!");
            return;
        }

        // Create container
        _grassContainer = new GameObject("=== GRASS ===");

        StartCoroutine(SpawnGrass());
    }

    IEnumerator SpawnGrass()
    {
        int spawned = 0;
        int attempted = 0;
        int maxAttempts = grassCount * 100; // Safety limit

        float boundsMinX = _mapBounds.min.x;
        float boundsMaxX = _mapBounds.max.x;
        float boundsMinZ = _mapBounds.min.z;
        float boundsMaxZ = _mapBounds.max.z;

        if (logSpawnStats)
            Debug.Log($"[GrassSpawner] Starting spawn — Target: {grassCount} blades | " +
                      $"Map bounds: {_mapBounds.size.x:F0} x {_mapBounds.size.z:F0} units");

        while (spawned < grassCount && attempted < maxAttempts)
        {
            attempted++;

            // ── 1. Random point within map bounds ────────────────────────
            float randX = Random.Range(boundsMinX, boundsMaxX);
            float randZ = Random.Range(boundsMinZ, boundsMaxZ);

            // ── 2. Check paint mask ───────────────────────────────────────
            float maskU = Mathf.InverseLerp(boundsMinX, boundsMaxX, randX);
            float maskV = Mathf.InverseLerp(boundsMinZ, boundsMaxZ, randZ);

            // Flip V because Unity texture V=0 is bottom, our map top is "north"
            maskV = 1f - maskV;

            Color maskPixel = grassMask.GetPixelBilinear(maskU, maskV);
            float brightness = maskPixel.grayscale;

            if (brightness < maskThreshold)
                continue; // Black area — skip

            // ── 3. Raycast to find ground ─────────────────────────────────
            Vector3 rayOrigin = new Vector3(randX, _mapBounds.max.y + raycastHeight, randZ);
            Ray ray = new Ray(rayOrigin, Vector3.down);

            if (!Physics.Raycast(ray, out RaycastHit hit, raycastHeight + _mapBounds.size.y + 100f, groundLayer))
                continue; // No ground found — skip

            // ── 4. Slope filter ───────────────────────────────────────────
            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            if (slopeAngle > maxSlopeAngle)
                continue; // Too steep — skip

            // ── 5. Spacing check (avoid too-dense clusters) ───────────────
            if (minSpacing > 0 && _spawnedGrass.Count > 0)
            {
                bool tooClose = false;
                // Only check nearby — full check would be too slow at high counts
                int checkCount = Mathf.Min(_spawnedGrass.Count, 20);
                for (int i = _spawnedGrass.Count - 1; i >= _spawnedGrass.Count - checkCount; i--)
                {
                    if (Vector3.Distance(_spawnedGrass[i].transform.position, hit.point) < minSpacing)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;
            }

            // ── 6. Spawn grass blade ──────────────────────────────────────
            float randScale = Random.Range(minScale, maxScale);
            float randRotY = Random.Range(0f, randomRotation);
            // Align grass blade to surface normal (looks natural on slopes)
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, hit.normal)
                                * Quaternion.Euler(0f, randRotY, 0f);

            GameObject blade = Instantiate(
                grassPrefab,
                hit.point,
                rotation,
                _grassContainer.transform
            );

            blade.transform.localScale = Vector3.one * randScale;

            // Apply grass material if assigned
            if (grassMaterial != null)
            {
                var renderers = blade.GetComponentsInChildren<MeshRenderer>();
                foreach (var r in renderers)
                    r.sharedMaterial = grassMaterial;
            }

            _spawnedGrass.Add(blade);
            spawned++;

            // Yield every 200 spawns to avoid freezing Unity
            if (spawned % 200 == 0)
            {
                if (logSpawnStats)
                    Debug.Log($"[GrassSpawner] Spawned {spawned}/{grassCount}...");
                yield return null;
            }
        }

        _spawned = true;

        if (logSpawnStats)
            Debug.Log($"[GrassSpawner] ✓ Done! Spawned: {spawned} blades | " +
                      $"Attempts: {attempted} | " +
                      $"Success rate: {(float)spawned / attempted * 100:F1}%");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    Bounds CalculateMapBounds()
    {
        var renderers = targetMap.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogWarning("[GrassSpawner] No renderers found on map! Using default bounds.");
            return new Bounds(targetMap.transform.position, Vector3.one * 100f);
        }

        Bounds bounds = renderers[0].bounds;
        foreach (var r in renderers)
            bounds.Encapsulate(r.bounds);

        if (logSpawnStats)
            Debug.Log($"[GrassSpawner] Map bounds — Size: {bounds.size} | Center: {bounds.center}");

        return bounds;
    }

    bool ValidateReferences()
    {
        if (grassPrefab == null)
        {
            Debug.LogError("[GrassSpawner] Grass Prefab not assigned!"); return false;
        }
        if (targetMap == null)
        {
            Debug.LogError("[GrassSpawner] Target Map not assigned!"); return false;
        }
        return true;
    }

    /// <summary>
    /// Call this from Inspector button or another script to re-spawn grass
    /// (clears existing and re-runs)
    /// </summary>
    public void RespawnGrass()
    {
        // Clear existing
        if (_grassContainer != null)
            Destroy(_grassContainer);

        _spawnedGrass.Clear();
        _spawned = false;

        _grassContainer = new GameObject("=== GRASS ===");
        _mapBounds = CalculateMapBounds();
        StartCoroutine(SpawnGrass());
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos || !Application.isPlaying) return;

        // Draw map bounds
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        Gizmos.DrawCube(_mapBounds.center, _mapBounds.size);
        Gizmos.color = new Color(0f, 1f, 0f, 1f);
        Gizmos.DrawWireCube(_mapBounds.center, _mapBounds.size);
    }
}