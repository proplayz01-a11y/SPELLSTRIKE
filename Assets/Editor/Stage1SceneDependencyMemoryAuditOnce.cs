#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

[InitializeOnLoad]
internal static class Stage1SceneDependencyMemoryAuditOnce
{
    private const string Marker = "Temp/Stage1SceneDependencyMemoryAudit.trigger";
    private const string ScenePath = "Assets/Scenes/OptimizationTests/Stage1_OptimizedMap_Test.unity";
    private const string ReportPath = "Temp/Stage1SceneDependencyMemoryAudit.txt";

    static Stage1SceneDependencyMemoryAuditOnce() => EditorApplication.update += TryRun;

    private static void TryRun()
    {
        if (!File.Exists(Path.GetFullPath(Marker))) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        File.Delete(Path.GetFullPath(Marker));
        Run();
    }

    [MenuItem("SPELLSTRIKE/Optimization/A05s Audit Stage1 Scene Runtime Memory")]
    private static void Run()
    {
        string[] deps = AssetDatabase.GetDependencies(ScenePath, true)
            .Where(path => !string.Equals(path, ScenePath, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var uniqueTextures = new HashSet<Texture2D>();
        var uniqueMeshes = new HashSet<Mesh>();
        var rows = new List<Row>(deps.Length);

        foreach (string path in deps)
        {
            UnityEngine.Object[] objects;
            try
            {
                objects = AssetDatabase.LoadAllAssetsAtPath(path);
            }
            catch
            {
                continue;
            }

            long textureBytes = 0;
            long meshBytes = 0;
            int textureCount = 0;
            int meshCount = 0;

            var textureDetails = new List<string>();
            foreach (Texture2D texture in objects.OfType<Texture2D>().Where(t => t != null).Distinct())
            {
                textureCount++;
                textureBytes += Profiler.GetRuntimeMemorySizeLong(texture);
                uniqueTextures.Add(texture);
                textureDetails.Add($"{texture.name}={texture.width}x{texture.height}/{texture.format}/{texture.mipmapCount}mips");
            }

            foreach (Mesh mesh in objects.OfType<Mesh>().Where(m => m != null).Distinct())
            {
                meshCount++;
                meshBytes += Profiler.GetRuntimeMemorySizeLong(mesh);
                uniqueMeshes.Add(mesh);
            }

            long sourceBytes = 0;
            string fullPath = Path.GetFullPath(path);
            if (File.Exists(fullPath))
                sourceBytes = new FileInfo(fullPath).Length;

            if (textureBytes > 0 || meshBytes > 0 || sourceBytes > 1024 * 1024)
                rows.Add(new Row(path, sourceBytes, textureBytes, meshBytes, textureCount, meshCount, textureDetails));
        }

        long uniqueTextureBytes = uniqueTextures.Sum(Profiler.GetRuntimeMemorySizeLong);
        long uniqueMeshBytes = uniqueMeshes.Sum(Profiler.GetRuntimeMemorySizeLong);

        var sb = new StringBuilder(64 * 1024);
        sb.AppendLine("Stage 1 Scene Dependency Runtime Memory Audit");
        sb.AppendLine($"UTC: {DateTime.UtcNow:O}");
        sb.AppendLine($"Scene: {ScenePath}");
        sb.AppendLine($"Dependencies: {deps.Length:N0}");
        sb.AppendLine($"Unique Texture2D: {uniqueTextures.Count:N0}, runtime={ToMiB(uniqueTextureBytes):F1} MiB");
        sb.AppendLine($"Unique Mesh: {uniqueMeshes.Count:N0}, runtime={ToMiB(uniqueMeshBytes):F1} MiB");
        sb.AppendLine($"Combined unique texture+mesh runtime estimate: {ToMiB(uniqueTextureBytes + uniqueMeshBytes):F1} MiB");
        sb.AppendLine();
        sb.AppendLine("Top dependencies by texture+mesh runtime estimate:");
        sb.AppendLine("Runtime MiB | Texture MiB | Mesh MiB | Source MiB | Textures | Meshes | Asset | Texture details");

        foreach (Row row in rows.OrderByDescending(r => r.RuntimeBytes).Take(100))
        {
            sb.AppendLine($"{ToMiB(row.RuntimeBytes),11:F1} | {ToMiB(row.TextureBytes),11:F1} | {ToMiB(row.MeshBytes),8:F1} | {ToMiB(row.SourceBytes),10:F1} | {row.TextureCount,8} | {row.MeshCount,6} | {row.Path} | {string.Join(", ", row.TextureDetails)}");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(ReportPath)) ?? "Temp");
        File.WriteAllText(Path.GetFullPath(ReportPath), sb.ToString());

        Debug.Log($"[Stage1 Scene Memory Audit] COMPLETE deps={deps.Length} textures={uniqueTextures.Count} textureMiB={ToMiB(uniqueTextureBytes):F1} meshes={uniqueMeshes.Count} meshMiB={ToMiB(uniqueMeshBytes):F1} combinedMiB={ToMiB(uniqueTextureBytes + uniqueMeshBytes):F1} report={ReportPath}");
    }

    private static double ToMiB(long bytes) => bytes / (1024.0 * 1024.0);

    private sealed class Row
    {
        public readonly string Path;
        public readonly long SourceBytes;
        public readonly long TextureBytes;
        public readonly long MeshBytes;
        public readonly int TextureCount;
        public readonly int MeshCount;
        public readonly IReadOnlyList<string> TextureDetails;
        public long RuntimeBytes => TextureBytes + MeshBytes;

        public Row(string path, long sourceBytes, long textureBytes, long meshBytes, int textureCount, int meshCount, IReadOnlyList<string> textureDetails)
        {
            Path = path;
            SourceBytes = sourceBytes;
            TextureBytes = textureBytes;
            MeshBytes = meshBytes;
            TextureCount = textureCount;
            MeshCount = meshCount;
            TextureDetails = textureDetails;
        }
    }
}
#endif
