using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Temporary, visual-only sky-to-player riddle result beam.
/// It intentionally owns neither combat damage nor reward calculation. The
/// riddle controller is the only gameplay authority for those outcomes.
/// </summary>
[DisallowMultipleComponent]
public sealed class Stage3RiddleOutcomeBeam : MonoBehaviour
{
    private LineRenderer halo;
    private LineRenderer core;
    private Material beamMaterial;
    private Color beamColor;
    private float beamWidth;

    public static bool Play(PlayerHealth target, Transform authoredOrigin, Color color, float duration)
    {
        if (target == null)
        {
            Debug.LogWarning("[Stage3RiddleOutcomeBeam] Missing player for outcome laser.");
            return false;
        }

        CharacterController playerCollider = target.GetComponent<CharacterController>();
        Vector3 hitPoint = playerCollider != null && playerCollider.enabled
            ? playerCollider.bounds.center
            : target.transform.position + Vector3.up * 1.5f;

        // Node 4 currently has no authored laser origin. A skyward beam is the
        // production fallback, not a fake persistent emitter in the scene.
        Vector3 origin = authoredOrigin != null
            ? authoredOrigin.position
            : hitPoint + Vector3.up * 15f + Vector3.right * 3f;
        float width = playerCollider != null && playerCollider.enabled
            ? Mathf.Clamp(playerCollider.bounds.size.y * 0.065f, 0.16f, 0.64f)
            : 0.28f;

        return SpawnBeam("Stage3_RiddleOutcomeBeam_Runtime", origin, hitPoint,
            color, duration, width);
    }

    /// <summary>
    /// Final riddle handoff ONLY: shoot a second sky-blue laser at Titan's
    /// chest AFTER the riddle UI closes. This is visual-only and does NOT
    /// awaken Titan by itself; Stage3RiddleController owns the timed handoff.
    /// </summary>
    public static bool PlayTowardsTitan(GameObject titanRoot, Transform authoredOrigin,
        Color color, float duration)
    {
        if (titanRoot == null || !titanRoot.activeInHierarchy)
        {
            Debug.LogError("[Stage3RiddleOutcomeBeam] Titan awakening laser cannot fire: dormant actor missing/inactive.");
            return false;
        }

        Transform chest = null;
        foreach (Transform bone in titanRoot.GetComponentsInChildren<Transform>(true))
        {
            if (bone.name.EndsWith("Spine2", StringComparison.OrdinalIgnoreCase))
            {
                chest = bone;
                break;
            }
        }

        Vector3 hitPoint;
        if (chest != null)
        {
            hitPoint = chest.position;
        }
        else
        {
            // Fallback for a rig whose chest is not named mixamorig:Spine2.
            Renderer renderer = titanRoot.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (renderer == null)
                renderer = titanRoot.GetComponentInChildren<Renderer>(true);
            if (renderer == null)
            {
                Debug.LogError("[Stage3RiddleOutcomeBeam] Titan has no chest bone or renderer; laser aborted.");
                return false;
            }
            hitPoint = renderer.bounds.center;
        }

        Vector3 origin = authoredOrigin != null
            ? authoredOrigin.position
            : hitPoint + Vector3.up * 25f + Vector3.forward * 24f;
        // The approved giant Titan has scale 40.01. Scale the beam's width to
        // its chest without changing the actor's transform or proportions.
        float width = Mathf.Clamp(titanRoot.transform.lossyScale.y * 0.02f, 0.45f, 1.25f);
        return SpawnBeam("Stage3_TitanAwakeningLaser_Runtime",
            origin, hitPoint, color, duration, width);
    }

    private static bool SpawnBeam(string effectName, Vector3 origin, Vector3 hitPoint,
        Color color, float duration, float width)
    {
        // Particle Unlit supports transparent LineRenderer vertex colors in
        // URP; ordinary URP Unlit can render the beam opaque.
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            Debug.LogWarning("[Stage3RiddleOutcomeBeam] No compatible unlit shader for outcome laser.");
            return false;
        }

        GameObject root = new GameObject(effectName);
        Stage3RiddleOutcomeBeam effect = root.AddComponent<Stage3RiddleOutcomeBeam>();
        effect.beamMaterial = new Material(shader)
        {
            name = "Stage3 Riddle Outcome Beam (Runtime)"
        };
        if (effect.beamMaterial.HasProperty("_BaseColor"))
            effect.beamMaterial.SetColor("_BaseColor", Color.white);
        if (effect.beamMaterial.HasProperty("_Color"))
            effect.beamMaterial.SetColor("_Color", Color.white);

        effect.beamColor = color;
        effect.beamWidth = width;
        effect.halo = effect.CreateLine("Outer Light", origin, hitPoint, effect.beamWidth * 2.8f);
        effect.core = effect.CreateLine("Bright Core", origin, hitPoint, effect.beamWidth);
        effect.StartCoroutine(effect.Pulse(Mathf.Max(0.1f, duration)));
        return true;
    }

    private LineRenderer CreateLine(string title, Vector3 start, Vector3 end, float width)
    {
        GameObject child = new GameObject(title);
        child.transform.SetParent(transform, false);
        LineRenderer line = child.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        line.material = beamMaterial;
        line.alignment = LineAlignment.View;
        line.textureMode = LineTextureMode.Stretch;
        line.numCapVertices = 6;
        line.numCornerVertices = 2;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.startWidth = width;
        line.endWidth = width;
        return line;
    }

    private IEnumerator Pulse(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            float pulse = 0.88f + 0.12f * Mathf.Sin(elapsed * 28f);
            float fade = Mathf.Clamp01((1f - t) * 3.5f);
            float alpha = pulse * fade;
            Color innerColor = Color.Lerp(beamColor, Color.white, 0.48f);
            innerColor.a = alpha;
            Color outerColor = beamColor;
            outerColor.a = alpha * 0.28f;
            core.startColor = innerColor;
            core.endColor = innerColor;
            halo.startColor = outerColor;
            halo.endColor = outerColor;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (beamMaterial != null)
            Destroy(beamMaterial);
    }
}
