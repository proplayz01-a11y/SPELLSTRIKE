using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Creates runtime-only Stage 2 VFX materials from a shader that is stored in
/// Resources. The hard Resources reference prevents Android shader stripping
/// from removing the shader before Shader.Find-based procedural VFX need it.
/// </summary>
public static class Stage2RuntimeVFXMaterialUtility
{
    private const string ResourcePath = "Stage2RuntimeVFXMobileSafe";
    private const string ShaderName = "SPELLSTRIKE/Stage2/RuntimeVFXMobileSafe";

    private static Shader cachedShader;

    public static Material CreateMaterial(Color color, bool additive = false, string materialName = null)
    {
        Shader shader = GetShader();
        Material material = new Material(shader);

        if (!string.IsNullOrEmpty(materialName))
            material.name = materialName;

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend"))
            material.SetFloat("_Blend", additive ? 2f : 0f);
        if (material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend"))
            material.SetFloat("_DstBlend", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 0f);

        material.renderQueue = (int)RenderQueue.Transparent;
        return material;
    }

    private static Shader GetShader()
    {
        if (cachedShader != null)
            return cachedShader;

        cachedShader = Resources.Load<Shader>(ResourcePath);
        if (cachedShader == null)
            cachedShader = Shader.Find(ShaderName);
        if (cachedShader == null)
            cachedShader = Shader.Find("Sprites/Default");
        if (cachedShader == null)
            cachedShader = Shader.Find("Hidden/InternalErrorShader");

        if (cachedShader == null)
            throw new System.InvalidOperationException(
                "Stage 2 runtime VFX shader could not be loaded. Expected Resources/" +
                ResourcePath + " or shader '" + ShaderName + "'.");

        return cachedShader;
    }
}
