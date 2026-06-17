using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

/// <summary>
/// 综合测试日志导出。路径策略与 <see cref="LoadApiTestLogCollector"/> 一致：
/// 优先 bundle 根目录下的 Logs；不可写时回退 persistentDataPath/comprehensiveTest/Logs。
/// </summary>
public static class ComprehensiveTestLogExporter
{
    /// <summary>当前运行时会写入的日志目录（绝对路径）。</summary>
    public static string GetExportDirectory()
    {
        string bundleRoot = BundleResLoader.GetDefaultRuntimeBundleRoot();
        string preferred = Path.Combine(bundleRoot, ComprehensiveTestPaths.LogSubFolder);
        if (TryEnsureWritableDirectory(preferred, out string writableDir))
            return writableDir;

        return EnsurePersistentLogDirectory();
    }

    /// <summary>真机（及 Editor 回退）persistentDataPath 下的日志目录。</summary>
    public static string GetPersistentLogDirectory()
    {
        return Path.Combine(
            Application.persistentDataPath,
            ComprehensiveTestPaths.PersistentLogRoot,
            ComprehensiveTestPaths.LogSubFolder);
    }

    static string EnsurePersistentLogDirectory()
    {
        string dir = GetPersistentLogDirectory();
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        return dir;
    }

    /// <summary>各平台典型路径说明（用于 UI / Debug.Log）。</summary>
    public static string GetLocationHint()
    {
#if UNITY_EDITOR
        return "Editor主路径: Assets/StreamingAssets/{平台}/Logs/ | 回退: " + GetPersistentLogDirectory();
#elif UNITY_ANDROID
        return "Android: " + GetPersistentLogDirectory() +
               " | adb pull \"" + GetPersistentLogDirectory() + "\" ./";
#elif UNITY_STANDALONE_WIN
        return "Windows: bundleRoot/Logs 或 " + GetPersistentLogDirectory();
#else
        return GetPersistentLogDirectory();
#endif
    }

    /// <summary>按平台选择可写目录导出日志。</summary>
    public static string ExportLog(IReadOnlyList<string> lines, string tag)
    {
        string dir = GetExportDirectory();
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        string path = Path.Combine(dir, BuildFileName(tag));
        File.WriteAllText(path, BuildHeader(tag, lines), Encoding.UTF8);
        Debug.Log("ComprehensiveTest log exported: " + path);
        return path;
    }

    public static string BuildMemorySummary()
    {
        long monoUsed = Profiler.GetMonoUsedSizeLong();
        long monoHeap = Profiler.GetMonoHeapSizeLong();
        long reserved = Profiler.GetTotalReservedMemoryLong();
        long allocated = Profiler.GetTotalAllocatedMemoryLong();
        long unusedReserved = Profiler.GetTotalUnusedReservedMemoryLong();
        long gfx = Profiler.GetAllocatedMemoryForGraphicsDriver();
        long gcManaged = GC.GetTotalMemory(false);
        return "mono=" + FormatBytes(monoUsed) +
               " heap=" + FormatBytes(monoHeap) +
               " res=" + FormatBytes(reserved) +
               " alloc=" + FormatBytes(allocated) +
               " ur=" + FormatBytes(unusedReserved) +
               " gfx=" + FormatBytes(gfx) +
               " gc=" + FormatBytes(gcManaged) +
               " g0=" + GC.CollectionCount(0) +
               " g1=" + GC.CollectionCount(1) +
               " g2=" + GC.CollectionCount(2);
    }

    static string BuildHeader(string tag, IReadOnlyList<string> lines)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== ComprehensiveTest Log Export ===");
        sb.AppendLine("Tag " + tag);
        sb.AppendLine("Unity " + Application.unityVersion);
        sb.AppendLine("Platform " + Application.platform);
        sb.AppendLine("Package " + Application.identifier);
        sb.AppendLine("Time " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine("Scene " + SceneManager.GetActiveScene().name);
        sb.AppendLine("ExportDir " + GetExportDirectory());
        sb.AppendLine("persistentDataPath " + Application.persistentDataPath);
        sb.AppendLine("streamingAssetsPath " + Application.streamingAssetsPath);
        sb.AppendLine("DeviceLogPath " + GetPersistentLogDirectory());
        sb.AppendLine("Memory " + BuildMemorySummary());
        sb.AppendLine();

        if (lines != null)
        {
            for (int i = 0; i < lines.Count; i++)
                sb.AppendLine(lines[i]);
        }

        return sb.ToString();
    }

    static string BuildFileName(string tag)
    {
        string safeTag = string.IsNullOrEmpty(tag) ? "Session" : tag.Replace(' ', '_');
        return "ComprehensiveTest_" + safeTag + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log";
    }

    static bool TryEnsureWritableDirectory(string dir, out string writableDir)
    {
        writableDir = dir;
        try
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string probe = Path.Combine(dir, ".write_probe");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);
            return true;
        }
        catch
        {
            return false;
        }
    }

    static string FormatBytes(long bytes)
    {
        if (bytes < 1024)
            return bytes + "B";
        if (bytes < 1024 * 1024)
            return (bytes / 1024f).ToString("F1") + "KB";
        return (bytes / (1024f * 1024f)).ToString("F2") + "MB";
    }
}
