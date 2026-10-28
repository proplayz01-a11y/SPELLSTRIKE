using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Read-only diagnostics for isolated Wizard's Vault UI art in Edit Mode.</summary>
public static class WizardsVaultPanelDiagnostic
{
    [MenuItem("SPELLSTRIKE/UI/Wizard Vault/9 Diagnose Panel Sprite Rendering")]
    private static void Run()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/WizardsVault_UIReview.unity" || Application.isPlaying)
            throw new System.InvalidOperationException("Read-only diagnostic requires isolated Wizard Vault Edit Mode scene.");
        var root = GameObject.Find("ItemSelectionUI_Prefab");
        if (!root) throw new System.InvalidOperationException("Review UI not found.");
        Report(root.transform, "WizardVaultContent/Body/StagePage");
        Report(root.transform, "WizardVaultContent/Body/ItemPage");
        Report(root.transform, "WizardVaultContent/Body/ItemPage/EquippedSlots/EquippedSlot1");
        Report(root.transform, "EditableVaultHeader");
    }

    private static void Report(Transform root, string path)
    {
        Transform node = root.Find(path);
        if (!node) { Debug.LogWarning("[VAULT DIAGNOSTIC] Missing " + path); return; }
        var image = node.GetComponent<Image>();
        var rect = node as RectTransform;
        if (!image || !rect) { Debug.LogWarning("[VAULT DIAGNOSTIC] Missing image/rect on " + path); return; }
        var renderer = image.canvasRenderer;
        Sprite sprite = image.sprite;
        string sample = "none";
        if (sprite)
        {
            RenderTexture old = RenderTexture.active;
            var rt = RenderTexture.GetTemporary(16, 16, 0, RenderTextureFormat.ARGB32);
            var tmp = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            try
            {
                Graphics.Blit(sprite.texture, rt);
                RenderTexture.active = rt;
                tmp.ReadPixels(new Rect(8, 8, 1, 1), 0, 0);
                tmp.Apply();
                Color c = tmp.GetPixel(0, 0);
                sample = c.ToString("F3");
            }
            finally
            {
                RenderTexture.active = old;
                RenderTexture.ReleaseTemporary(rt);
                Object.DestroyImmediate(tmp);
            }
        }
        Rect physical = image.GetPixelAdjustedRect();
        Debug.Log("[VAULT DIAGNOSTIC] " + path +
                  " active=" + node.gameObject.activeInHierarchy +
                  " imageEnabled=" + image.enabled +
                  " canvasCull=" + renderer.cull +
                  " rendererAlpha=" + renderer.GetAlpha() +
                  " rendererColor=" + renderer.GetColor() +
                  " imageColor=" + image.color +
                  " rect=" + rect.rect +
                  " adjustedRect=" + physical +
                  " scale=" + rect.lossyScale +
                  " sprite=" + (sprite ? sprite.name : "null") +
                  " spriteRect=" + (sprite ? sprite.rect.ToString() : "none") +
                  " centerSample=" + sample);
    }
}
