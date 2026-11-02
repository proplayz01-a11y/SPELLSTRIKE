using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Changes only the model of the existing WorldTile pickup. The original
/// trigger, Rigidbody, floating, collection and respawn rules are untouched.
/// Shared GLB base + letter meshes are used, never 26 full duplicated tiles.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(WorldTile))]
public sealed class RodinWorldTileVisual : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float worldVisualScale = 1.35f;
    [SerializeField] private bool castShadows;

    private RodinTileCatalog catalog;
    private Transform visualRoot;
    private char displayedLetter;

    public bool ApplyLetter(char letter)
    {
        letter = char.ToUpperInvariant(letter);
        if (catalog == null) catalog = RodinTileCatalog.Load();
        if (catalog == null || !catalog.IsComplete) return false;
        if (visualRoot != null && displayedLetter == letter) return true;

        GameObject tileBase = catalog.GetBase(letter);
        GameObject glyph = catalog.GetLetterMesh(letter);
        if (tileBase == null || glyph == null) return false;

        if (visualRoot != null) Destroy(visualRoot.gameObject);

        var holder = new GameObject("RodinV11_" + letter + "_VisualOnly");
        visualRoot = holder.transform;
        visualRoot.SetParent(transform, false);

        // The existing prefab uses an unusual non-uniform root scale.
        // Counter that scale *only for visuals* to preserve its trigger and
        // world pickup movement without deforming the approved Rodin mesh.
        Vector3 parentScale = transform.lossyScale;
        visualRoot.localScale = new Vector3(
            worldVisualScale / Mathf.Max(Mathf.Abs(parentScale.x), 0.001f),
            worldVisualScale / Mathf.Max(Mathf.Abs(parentScale.y), 0.001f),
            worldVisualScale / Mathf.Max(Mathf.Abs(parentScale.z), 0.001f));
        visualRoot.localPosition = new Vector3(
            0f, -0.883f * worldVisualScale / Mathf.Max(Mathf.Abs(parentScale.y), 0.001f), 0f);

        GameObject bodyInstance = Instantiate(tileBase, visualRoot, false);
        GameObject letterInstance = Instantiate(glyph, visualRoot, false);
        bodyInstance.name = "RodinSharedBase";
        letterInstance.name = "RodinBlackLetter_" + letter;
        ResetLocal(bodyInstance.transform);
        ResetLocal(letterInstance.transform);

        // Imported GLBs are mesh props. Any colliders inside them must never
        // interfere with the original WorldTile pickup trigger.
        foreach (Collider c in visualRoot.GetComponentsInChildren<Collider>(true))
            c.enabled = false;
        foreach (Renderer renderer in visualRoot.GetComponentsInChildren<Renderer>(true))
        {
            renderer.shadowCastingMode = castShadows
                ? ShadowCastingMode.On : ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        Renderer legacy = GetComponent<Renderer>();
        if (legacy != null) legacy.enabled = false;
        displayedLetter = letter;
        return true;
    }

    private void Start()
    {
        if (visualRoot == null)
            ApplyLetter(GetComponent<WorldTile>().letter);
    }

    private static void ResetLocal(Transform t)
    {
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one;
    }
}
