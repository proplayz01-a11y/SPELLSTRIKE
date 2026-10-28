#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

[InitializeOnLoad]
internal static class Stage1MobileTextureAuditOnce
{
    private const string Marker = "Temp/Stage1MobileTextureAudit.trigger";
    private const string AssetPath = "Assets/Maps/Optimized/level1_STAGE1_MOBILE.glb";
    static Stage1MobileTextureAuditOnce() => EditorApplication.delayCall += TryRun;
    private static void TryRun()
    {
        if (!File.Exists(Marker)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += TryRun; return; }
        File.Delete(Marker);
        var all = AssetDatabase.LoadAllAssetsAtPath(AssetPath);
        var textures = all.OfType<Texture2D>().Distinct().ToArray();
        long bytes = textures.Sum(t => Profiler.GetRuntimeMemorySizeLong(t));
        int maxDim = textures.Length == 0 ? 0 : textures.Max(t => Mathf.Max(t.width, t.height));
        Debug.Log($"[Stage1 Mobile Texture Audit] COMPLETE textures={textures.Length} maxDim={maxDim} runtimeMiB={bytes / 1048576.0:F1} sourceMiB={new FileInfo(Path.GetFullPath(AssetPath)).Length / 1048576.0:F1}");
    }
}
#endif
