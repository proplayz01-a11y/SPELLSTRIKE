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
internal static class Stage1TextureMemoryAudit
{
    private const string TriggerPath = "Temp/Stage1TextureMemoryAudit.trigger";
    private const string ReportPath = "Temp/Stage1TextureMemoryAudit.txt";
    private const string OptimizedMapPath = "Assets/Maps/Optimized/level1_STAGE1_OPTIMIZED.glb";

    static Stage1TextureMemoryAudit()
    {
        EditorApplication.update += WatchTrigger;
    }

    private static void WatchTrigger()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;

        string trigger = Path.GetFullPath(TriggerPath);
        if (!File.Exists(trigger))
            return;

        try
        {
            File.Delete(trigger);
            RunAudit();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    [MenuItem("SPELLSTRIKE/Optimization/A05s Audit Optimized Map Texture Memory %#&t")]
    public static void RunAudit()
    {
        UnityEngine.Object[] all = AssetDatabase.LoadAllAssetsAtPath(OptimizedMapPath);
        Texture2D[] textures = all.OfType<Texture2D>().Distinct().ToArray();
        Material[] materials = all.OfType<Material>().Distinct().ToArray();
        Mesh[] meshes = all.OfType<Mesh>().Distinct().ToArray();

        var textureMaterialUsage = new Dictionary<Texture2D, HashSet<string>>();
        foreach (Material material in materials)
        {
            if (material == null || material.shader == null)
                continue;

            foreach (string propertyName in material.GetTexturePropertyNames())
            {
                if (material.GetTexture(propertyName) is not Texture2D texture)
                    continue;

                if (!textureMaterialUsage.TryGetValue(texture, out HashSet<string> users))
                {
                    users = new HashSet<string>(StringComparer.Ordinal);
                    textureMaterialUsage.Add(texture, users);
                }

                users.Add($"{material.name}:{propertyName}");
            }
        }

        var rows = textures
            .Select(texture => new TextureRow(texture, textureMaterialUsage.TryGetValue(texture, out HashSet<string> users) ? users : null))
            .OrderByDescending(row => row.RuntimeBytes)
            .ThenByDescending(row => (long)row.Width * row.Height)
            .ToArray();

        long textureRuntimeBytes = rows.Sum(row => row.RuntimeBytes);
        long meshRuntimeBytes = meshes.Sum(mesh => mesh == null ? 0L : Profiler.GetRuntimeMemorySizeLong(mesh));
        long sourceBytes = new FileInfo(Path.GetFullPath(OptimizedMapPath)).Length;

        var sb = new StringBuilder(64 * 1024);
        sb.AppendLine("Stage 1 Optimized Map Texture Memory Audit");
        sb.AppendLine($"UTC: {DateTime.UtcNow:O}");
        sb.AppendLine($"Asset: {OptimizedMapPath}");
        sb.AppendLine($"Source GLB bytes: {sourceBytes:N0} ({ToMiB(sourceBytes):F1} MiB)");
        sb.AppendLine($"Subassets: total={all.Length:N0} textures={textures.Length:N0} materials={materials.Length:N0} meshes={meshes.Length:N0}");
        sb.AppendLine($"Texture runtime memory estimate: {textureRuntimeBytes:N0} bytes ({ToMiB(textureRuntimeBytes):F1} MiB)");
        sb.AppendLine($"Mesh runtime memory estimate: {meshRuntimeBytes:N0} bytes ({ToMiB(meshRuntimeBytes):F1} MiB)");
        sb.AppendLine($"Readable textures: {rows.Count(row => row.Readable):N0}/{rows.Length:N0}");
        sb.AppendLine($"Textures with mipmaps: {rows.Count(row => row.MipmapCount > 1):N0}/{rows.Length:N0}");
        sb.AppendLine();
        sb.AppendLine("Top textures by Unity runtime memory estimate:");
        sb.AppendLine("Rank | Runtime MiB | WxH | Mips | Format | GraphicsFormat | Readable | Materials/slots | Name");

        for (int i = 0; i < rows.Length; i++)
        {
            TextureRow row = rows[i];
            string users = row.Users == null || row.Users.Count == 0
                ? "(no material slot found)"
                : string.Join(", ", row.Users.OrderBy(x => x, StringComparer.Ordinal));
            sb.AppendLine($"{i + 1} | {ToMiB(row.RuntimeBytes):F2} | {row.Width}x{row.Height} | {row.MipmapCount} | {row.Format} | {row.GraphicsFormat} | {row.Readable} | {users} | {row.Name}");
        }

        sb.AppendLine();
        sb.AppendLine("Dimension buckets:");
        foreach (var bucket in rows.GroupBy(row => Math.Max(row.Width, row.Height)).OrderByDescending(group => group.Key))
        {
            long bytes = bucket.Sum(row => row.RuntimeBytes);
            sb.AppendLine($"maxDim={bucket.Key}: count={bucket.Count():N0}, runtime={ToMiB(bytes):F1} MiB");
        }

        sb.AppendLine();
        sb.AppendLine("Format buckets:");
        foreach (var bucket in rows.GroupBy(row => $"{row.Format}/{row.GraphicsFormat}").OrderByDescending(group => group.Sum(row => row.RuntimeBytes)))
        {
            long bytes = bucket.Sum(row => row.RuntimeBytes);
            sb.AppendLine($"{bucket.Key}: count={bucket.Count():N0}, runtime={ToMiB(bytes):F1} MiB");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(ReportPath)) ?? "Temp");
        File.WriteAllText(Path.GetFullPath(ReportPath), sb.ToString());

        Debug.Log(
            $"[Stage1 Texture Audit] COMPLETE | textures={textures.Length} | textureRuntime={ToMiB(textureRuntimeBytes):F1} MiB | " +
            $"meshes={meshes.Length} | meshRuntime={ToMiB(meshRuntimeBytes):F1} MiB | report={ReportPath}");
    }

    private static double ToMiB(long bytes) => bytes / (1024.0 * 1024.0);

    private sealed class TextureRow
    {
        public readonly string Name;
        public readonly int Width;
        public readonly int Height;
        public readonly int MipmapCount;
        public readonly TextureFormat Format;
        public readonly UnityEngine.Experimental.Rendering.GraphicsFormat GraphicsFormat;
        public readonly bool Readable;
        public readonly long RuntimeBytes;
        public readonly HashSet<string> Users;

        public TextureRow(Texture2D texture, HashSet<string> users)
        {
            Name = texture != null ? texture.name : "<null>";
            Width = texture != null ? texture.width : 0;
            Height = texture != null ? texture.height : 0;
            MipmapCount = texture != null ? texture.mipmapCount : 0;
            Format = texture != null ? texture.format : TextureFormat.RGBA32;
            GraphicsFormat = texture != null ? texture.graphicsFormat : UnityEngine.Experimental.Rendering.GraphicsFormat.None;
            Readable = texture != null && texture.isReadable;
            RuntimeBytes = texture != null ? Profiler.GetRuntimeMemorySizeLong(texture) : 0L;
            Users = users;
        }
    }
}
#endif
