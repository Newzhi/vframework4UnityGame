using System;
using System.IO;
using UnityEngine;

/// <summary>
/// <see cref="DebugLogger"/> 的日志目录解析：按平台选择可写路径，优先 bundle 根目录下的 Logs，回退 persistentDataPath。
/// 路径策略与 Assets/Test 下 ComprehensiveTestLogExporter、LoadApiTestLogCollector 一致。
/// </summary>
public static class DebugLoggerPaths
{
    /// <summary>日志子目录名（与 AB_Test、综合测试约定一致）。</summary>
    public const string LogSubFolder = "Logs";

    /// <summary>persistentDataPath 下的根目录名。</summary>
    public const string PersistentLogRoot = "vFramework";

    /// <summary>Editor 工程内可选归档目录（相对 Assets 父目录）。</summary>
    public const string EditorRelativeLogFolder = "Assets/Logs";

    /// <summary>真机 persistentDataPath 下的日志目录。</summary>
    public static string GetPersistentLogDirectory()
    {
        return Path.Combine(Application.persistentDataPath, PersistentLogRoot, LogSubFolder);
    }

    /// <summary>
    /// 解析当前运行时会写入的日志目录（绝对路径）。
    /// 优先级：customDir → bundleRoot/Logs → persistentDataPath/vFramework/Logs → Editor 工程 Logs。
    /// </summary>
    public static string ResolveLogDirectory(string customDirectory = null)
    {
        if (!string.IsNullOrEmpty(customDirectory) &&
            TryEnsureWritableDirectory(customDirectory, out string customWritable))
            return customWritable;

        string bundleRoot = BundlePlatformPaths.ResolveRuntimeBundleRoot(null, usePlatformSubfolders: true);
        string bundleLogs = Path.Combine(bundleRoot, LogSubFolder);
        if (TryEnsureWritableDirectory(bundleLogs, out string bundleWritable))
            return bundleWritable;

        string persistent = GetPersistentLogDirectory();
        if (TryEnsureWritableDirectory(persistent, out string persistentWritable))
            return persistentWritable;

#if UNITY_EDITOR
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string editorLogs = Path.GetFullPath(Path.Combine(projectRoot, EditorRelativeLogFolder));
        if (TryEnsureWritableDirectory(editorLogs, out string editorWritable))
            return editorWritable;
#endif

        return persistent;
    }

    /// <summary>各平台典型路径说明（用于 UI / Console 提示）。</summary>
    public static string GetLocationHint(string activeDirectory = null)
    {
        string dir = string.IsNullOrEmpty(activeDirectory)
            ? ResolveLogDirectory()
            : activeDirectory;

#if UNITY_EDITOR
        return "Editor: bundleRoot/Logs 或 " + GetPersistentLogDirectory() +
               " | 工程: Assets/Logs | 当前=" + dir;
#elif UNITY_ANDROID
        return "Android persistentDataPath: " + dir +
               " | adb pull \"" + dir + "\" ./device_logs";
#elif UNITY_IOS
        return "iOS: " + dir + "（Xcode Devices → Download Container → AppData）";
#elif UNITY_STANDALONE_WIN
        return "Windows: " + dir + "（bundleRoot/Logs 或 persistentDataPath）";
#elif UNITY_STANDALONE_OSX
        return "macOS: " + dir;
#else
        return dir;
#endif
    }

    public static string BuildSessionFileName(string tag = null)
    {
        string safeTag = string.IsNullOrEmpty(tag) ? "session" : SanitizeFileName(tag);
        return "game_" + safeTag + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log";
    }

    public static bool TryEnsureWritableDirectory(string dir, out string writableDir)
    {
        writableDir = dir;
        if (string.IsNullOrEmpty(dir))
            return false;

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

    static string SanitizeFileName(string value)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        var sb = new System.Text.StringBuilder(value.Length);
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            bool bad = false;
            for (int j = 0; j < invalid.Length; j++)
            {
                if (c == invalid[j])
                {
                    bad = true;
                    break;
                }
            }

            sb.Append(bad ? '_' : c);
        }

        return sb.ToString().Replace(' ', '_');
    }
}
