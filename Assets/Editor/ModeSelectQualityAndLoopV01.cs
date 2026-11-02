using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Visible menu-only isolated fix for imported 2D UI fidelity and repeated hover sweeps.</summary>
public static class ModeSelectQualityAndLoopV01
{
    private const string TargetScene = "Assets/Scenes/MinigameModeSelect_Integration_v01.unity";
    private const string Separated = "Assets/Minigames/ModeSelectWorkbench/Separated";

    [MenuItem("SPELLSTRIKE/Minigames/QA - Sharpen UI Textures and Loop Gold Hover")]
    public static void Apply()
    {
        if (System.Diagnostics.Process.GetProcessesByName("VALORANT-Win64-Shipping").Length != 0)
        {
            Debug.LogWarning("[Mode Select Quality] VALORANT active; changes skipped."); return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            Debug.LogWarning("[Mode Select Quality] Exit Play Mode and finish importing first."); return;
        }
        Scene active = SceneManager.GetActiveScene();
        if (!active.IsValid() || active.path != TargetScene)
        {
            Debug.LogError("[Mode Select Quality] Open the isolated integration scene first, not production."); return;
        }
        int count = 0;
        foreach (string assetPath in AssetDatabase.GetAllAssetPaths())
        {
            bool belongs = assetPath.StartsWith(Separated + "/", StringComparison.Ordinal)
                || assetPath == "Assets/Minigames/ModeSelectWorkbench/TentInteriorApproved.png";
            if (!belongs || !assetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) continue;
            TextureImporter ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (ti == null) throw new IOException("Could not find UI importer: " + assetPath);
            bool changed = ti.mipmapEnabled || ti.textureCompression != TextureImporterCompression.Uncompressed ||
                ti.maxTextureSize < 2048 || ti.filterMode != FilterMode.Bilinear;
            if (!changed) { count++; continue; }
            ti.mipmapEnabled = false;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.maxTextureSize = 4096;
            ti.filterMode = FilterMode.Bilinear;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.alphaIsTransparency = true;
            ti.SaveAndReimport();
            count++;
        }
        var hovers = UnityEngine.Object.FindObjectsByType<ModeSelectGoldBorderHover>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int sceneHoverCount = 0;
        foreach (var hover in hovers)
        {
            if (hover.gameObject.scene != active) continue;
            Undo.RecordObject(hover, "Enable continuous clockwise hover sweep");
            hover.SetContinuousHover(true);
            EditorUtility.SetDirty(hover);
            sceneHoverCount++;
        }
        if (sceneHoverCount != 3) throw new InvalidOperationException("Expected three isolated card hovers, got " + sceneHoverCount);
        EditorSceneManager.MarkSceneDirty(active);
        if (!EditorSceneManager.SaveScene(active)) throw new IOException("Isolated scene save failed");
        AssetDatabase.SaveAssets();
        Debug.Log("[Mode Select Quality] Saved " + count + " sharp, uncompressed, no-mipmap UI textures; " +
            sceneHoverCount + " continuous independent clockwise hover loops in " + TargetScene +
            ". Source Downloads images and production scenes untouched.");
    }
}
