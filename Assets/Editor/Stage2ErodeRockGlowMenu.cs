#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Stage2ErodeRockGlowMenu
{
    private const string SourceMaterialPath = "Assets/Stage 2/Objects/Atlas_Unlit.mat";
    private const string LegacyGlowMaterialName = "ERODE_Atlas_Glow";
    private const string GlowMaterialPath = "Assets/Stage 2/Objects/ERODE_Spiral_Glow.mat";

    [MenuItem("SPELLSTRIKE/Stage 2/ERODE/Apply Glow To Selected Rock")]
    private static void ApplyGlowToSelectedRock()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("ERODE Rock Glow", "Select ERODE-ROCK in the Hierarchy first.", "OK");
            return;
        }

        Renderer renderer = selected.GetComponent<Renderer>();
        if (renderer == null)
        {
            renderer = selected.GetComponentInChildren<Renderer>(true);
        }

        if (renderer == null)
        {
            EditorUtility.DisplayDialog("ERODE Rock Glow", "The selected object has no Renderer.", "OK");
            return;
        }

        Material sourceMaterial = AssetDatabase.LoadAssetAtPath<Material>(SourceMaterialPath);
        if (sourceMaterial == null)
        {
            EditorUtility.DisplayDialog("ERODE Rock Glow", "Could not load Atlas_Unlit as the texture source.", "OK");
            return;
        }

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            EditorUtility.DisplayDialog("ERODE Rock Glow", "URP/Lit shader was not found.", "OK");
            return;
        }

        Material glowMaterial = AssetDatabase.LoadAssetAtPath<Material>(GlowMaterialPath);
        if (glowMaterial == null)
        {
            glowMaterial = new Material(urpLit) { name = "ERODE_Spiral_Glow" };
            AssetDatabase.CreateAsset(glowMaterial, GlowMaterialPath);
        }

        if (glowMaterial == null)
        {
            EditorUtility.DisplayDialog("ERODE Rock Glow", "The glow material could not be loaded.", "OK");
            return;
        }

        Texture spiralTexture = sourceMaterial.GetTexture("baseColorTexture");
        glowMaterial.shader = urpLit;
        glowMaterial.name = "ERODE_Spiral_Glow";
        glowMaterial.SetTexture("_BaseMap", spiralTexture);
        glowMaterial.SetColor("_BaseColor", new Color(0.02f, 0.35f, 0.45f, 1f));
        glowMaterial.SetTexture("_EmissionMap", spiralTexture);
        glowMaterial.SetColor("_EmissionColor", new Color(0f, 4f, 8f, 1f));
        glowMaterial.EnableKeyword("_EMISSION");
        glowMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        EditorUtility.SetDirty(glowMaterial);

        Material[] materials = renderer.sharedMaterials;
        bool replacedAtlas = false;
        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] != null &&
                (materials[i].name == "Atlas_Unlit" || materials[i].name == LegacyGlowMaterialName))
            {
                materials[i] = glowMaterial;
                replacedAtlas = true;
            }
        }

        if (!replacedAtlas)
        {
            EditorUtility.DisplayDialog(
                "ERODE Rock Glow",
                "The selected Renderer does not use Atlas_Unlit, so nothing was changed.",
                "OK");
            return;
        }

        Undo.RecordObject(renderer, "Apply ERODE rock glow");
        renderer.sharedMaterials = materials;
        EditorUtility.SetDirty(renderer);
        EditorSceneManager.MarkSceneDirty(selected.scene);
        AssetDatabase.SaveAssets();

        Selection.activeObject = selected;
        Debug.Log("[ERODE] Applied new URP/Lit ERODE_Spiral_Glow to " + renderer.name + ".", selected);
    }
}
#endif
