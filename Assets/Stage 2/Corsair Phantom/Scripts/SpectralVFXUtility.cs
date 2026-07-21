using UnityEngine;

/// <summary>
/// Shared helpers for the Corsair Phantom's procedural spectral VFX
/// (charge, projectile, impact burst). Centralizes transparent/additive
/// material creation, a reusable quad mesh, and particle renderer setup so
/// the effect layers stay consistent and Android-friendly.
/// </summary>
public static class SpectralVFXUtility
{
    private static Mesh cachedQuadMesh;

    /// <summary>
    /// Returns a shared unit quad mesh used for billboarded glows.
    /// </summary>
    public static Mesh QuadMesh()
    {
        if (cachedQuadMesh != null)
            return cachedQuadMesh;

        cachedQuadMesh = new Mesh { name = "SpectralQuad" };
        cachedQuadMesh.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3(0.5f, -0.5f, 0f),
            new Vector3(0.5f, 0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f)
        };
        cachedQuadMesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
        cachedQuadMesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        cachedQuadMesh.RecalculateBounds();
        return cachedQuadMesh;
    }

    /// <summary>
    /// Creates an unlit transparent material. When additive, it uses one-blend
    /// for a glowing spectral look; otherwise standard alpha blending.
    /// </summary>
    public static Material CreateMaterial(Color color, bool additive)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        Material material = new Material(shader);

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);

        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 0f);

        UnityEngine.Rendering.BlendMode destination = additive
            ? UnityEngine.Rendering.BlendMode.One
            : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;

        if (material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend"))
            material.SetFloat("_DstBlend", (float)destination);

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        return material;
    }

    /// <summary>
    /// Applies a billboard renderer with an additive spectral material.
    /// </summary>
    public static void ConfigureParticleRenderer(ParticleSystem system, Color color)
    {
        ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = CreateMaterial(color, true);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    /// <summary>
    /// Builds a color-over-lifetime gradient that fades the given color to zero alpha.
    /// </summary>
    public static Gradient FadeGradient(Color color)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new[] { new GradientAlphaKey(color.a, 0f), new GradientAlphaKey(0f, 1f) });
        return gradient;
    }
}
