using System;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// 通用调试日志：用法类似 <see cref="Debug.Log"/>，Editor 输出 Console；
/// 真机（及可选 Editor）追加写入平台可写目录下的 Logs 文件夹，便于 adb / 文件管理器拉取。
/// </summary>
public static class DebugLogger
{
    public const string LogPrefix = "[DebugLogger]";

    public enum LogLevel
    {
        Info,
        Warning,
        Error
    }

    static bool enabled;
    static bool mirrorToUnityConsole = true;
    static bool writeToFile;
    static string customLogDirectory;
    static string sessionTag;
    static string activeLogDirectory;
    static string activeLogFilePath;
    static bool headerWritten;
    static long sessionStartUtcMs;
    static readonly object fileLock = new object();

    /// <summary>总开关；关闭后 Log / Warning / Error 均不输出。</summary>
    public static bool Enabled
    {
        get => enabled;
        set => enabled = value;
    }

    /// <summary>是否同步输出 Unity Console（默认 true）。</summary>
    public static bool MirrorToUnityConsole
    {
        get => mirrorToUnityConsole;
        set => mirrorToUnityConsole = value;
    }

    /// <summary>是否写入磁盘日志文件。</summary>
    public static bool WriteToFile
    {
        get => writeToFile;
        set => writeToFile = value;
    }

    /// <summary>当前会话日志文件绝对路径（尚未创建时为 null）。</summary>
    public static string ActiveLogFilePath => activeLogFilePath;

    /// <summary>当前会话使用的日志目录绝对路径。</summary>
    public static string ActiveLogDirectory => activeLogDirectory;

    static DebugLogger()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        enabled = true;
#else
        enabled = false;
#endif

#if UNITY_EDITOR
        writeToFile = false;
#else
        writeToFile = true;
#endif
    }

    #region 配置

    /// <summary>
    /// 启动前配置：指定日志目录、会话标签与是否写文件。
    /// 建议在 <c>Awake</c> / 热更入口最早处调用一次。
    /// </summary>
    public static void Configure(
        string logDirectory = null,
        string tag = null,
        bool? enableFileOutput = null,
        bool? enableConsoleMirror = null)
    {
        if (!string.IsNullOrEmpty(logDirectory))
            customLogDirectory = logDirectory;

        if (!string.IsNullOrEmpty(tag))
            sessionTag = tag;

        if (enableFileOutput.HasValue)
            writeToFile = enableFileOutput.Value;

        if (enableConsoleMirror.HasValue)
            mirrorToUnityConsole = enableConsoleMirror.Value;
    }

    /// <summary>仅覆盖日志目录（绝对路径）；null 表示恢复自动解析。</summary>
    public static void SetLogDirectory(string absoluteDirectory)
    {
        customLogDirectory = absoluteDirectory;
        ResetSessionFile();
    }

    /// <summary>解析当前会使用的日志目录（不创建文件）。</summary>
    public static string GetLogDirectory()
    {
        return DebugLoggerPaths.ResolveLogDirectory(customLogDirectory);
    }

    /// <summary>真机 persistentDataPath 下的 Logs 目录。</summary>
    public static string GetPersistentLogDirectory()
    {
        return DebugLoggerPaths.GetPersistentLogDirectory();
    }

    /// <summary>各平台典型路径说明。</summary>
    public static string GetLocationHint()
    {
        return DebugLoggerPaths.GetLocationHint(activeLogDirectory ?? GetLogDirectory());
    }

    /// <summary>刷盘并输出当前日志文件路径到 Console。</summary>
    public static void Flush()
    {
        if (!writeToFile || string.IsNullOrEmpty(activeLogFilePath))
            return;

        lock (fileLock)
        {
            Debug.Log(LogPrefix + " log file=" + activeLogFilePath);
        }
    }

    #endregion

    #region Log API（对齐 Debug.Log 习惯）

    public static void Log(object message)
    {
        Write(LogLevel.Info, message, null, null);
    }

    public static void Log(object message, UnityEngine.Object context)
    {
        Write(LogLevel.Info, message, null, context);
    }

    public static void Log(object message, string tag)
    {
        Write(LogLevel.Info, message, tag, null);
    }

    public static void LogWarning(object message)
    {
        Write(LogLevel.Warning, message, null, null);
    }

    public static void LogWarning(object message, UnityEngine.Object context)
    {
        Write(LogLevel.Warning, message, null, context);
    }

    public static void LogWarning(object message, string tag)
    {
        Write(LogLevel.Warning, message, tag, null);
    }

    public static void LogError(object message)
    {
        Write(LogLevel.Error, message, null, null);
    }

    public static void LogError(object message, UnityEngine.Object context)
    {
        Write(LogLevel.Error, message, null, context);
    }

    public static void LogError(object message, string tag)
    {
        Write(LogLevel.Error, message, tag, null);
    }

    public static void LogFormat(string format, params object[] args)
    {
        Write(LogLevel.Info, string.Format(format, args), null, null);
    }

    public static void LogFormat(string tag, string format, params object[] args)
    {
        Write(LogLevel.Info, string.Format(format, args), tag, null);
    }

    public static void LogException(Exception exception, string tag = null)
    {
        if (exception == null)
            return;

        Write(LogLevel.Error, exception.ToString(), tag, null);
    }

    #endregion

    #region 内部

    static void Write(LogLevel level, object message, string tag, UnityEngine.Object context)
    {
        if (!enabled)
            return;

        string text = message?.ToString() ?? string.Empty;
        string line = FormatLine(level, tag, text);

        if (mirrorToUnityConsole)
            MirrorToConsole(level, line, context);

        if (writeToFile)
            AppendToFile(line);
    }

    static void MirrorToConsole(LogLevel level, string line, UnityEngine.Object context)
    {
        switch (level)
        {
            case LogLevel.Warning:
                if (context != null)
                    Debug.LogWarning(line, context);
                else
                    Debug.LogWarning(line);
                break;
            case LogLevel.Error:
                if (context != null)
                    Debug.LogError(line, context);
                else
                    Debug.LogError(line);
                break;
            default:
                if (context != null)
                    Debug.Log(line, context);
                else
                    Debug.Log(line);
                break;
        }
    }

    static string FormatLine(LogLevel level, string tag, string text)
    {
        var sb = new StringBuilder(text.Length + 48);
        sb.Append('[').Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")).Append(']');
        sb.Append('[').Append(level.ToString().ToUpperInvariant()).Append(']');

        if (!string.IsNullOrEmpty(tag))
            sb.Append('[').Append(tag).Append(']');

        sb.Append(' ').Append(text);
        return sb.ToString();
    }

    static void AppendToFile(string line)
    {
        try
        {
            lock (fileLock)
            {
                EnsureLogFile();
                if (string.IsNullOrEmpty(activeLogFilePath))
                    return;

                File.AppendAllText(activeLogFilePath, line + Environment.NewLine, Encoding.UTF8);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning(LogPrefix + " file write failed: " + ex.Message);
        }
    }

    static void EnsureLogFile()
    {
        if (!string.IsNullOrEmpty(activeLogFilePath) && headerWritten)
            return;

        activeLogDirectory = DebugLoggerPaths.ResolveLogDirectory(customLogDirectory);
        if (!DebugLoggerPaths.TryEnsureWritableDirectory(activeLogDirectory, out string writableDir))
            return;

        activeLogDirectory = writableDir;
        activeLogFilePath = Path.Combine(
            activeLogDirectory,
            DebugLoggerPaths.BuildSessionFileName(sessionTag));

        sessionStartUtcMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        File.WriteAllText(activeLogFilePath, BuildFileHeader(), Encoding.UTF8);
        headerWritten = true;
    }

    static string BuildFileHeader()
    {
        var sb = new StringBuilder(512);
        sb.AppendLine("=== vFramework Debug Log ===");
        sb.AppendLine("Unity " + Application.unityVersion);
        sb.AppendLine("Platform " + Application.platform);
        sb.AppendLine("Package " + Application.identifier);
        sb.AppendLine("DeviceModel " + SystemInfo.deviceModel);
        sb.AppendLine("OS " + SystemInfo.operatingSystem);
        sb.AppendLine("SessionTag " + (sessionTag ?? string.Empty));
        sb.AppendLine("SessionStartUtcMs " + sessionStartUtcMs);
        sb.AppendLine("LogDirectory " + activeLogDirectory);
        sb.AppendLine("persistentDataPath " + Application.persistentDataPath);
        sb.AppendLine("streamingAssetsPath " + Application.streamingAssetsPath);
        sb.AppendLine("BundleRoot " + BundlePlatformPaths.ResolveRuntimeBundleRoot(null, usePlatformSubfolders: true));
        sb.AppendLine("LocationHint " + DebugLoggerPaths.GetLocationHint(activeLogDirectory));
        sb.AppendLine("---");
        return sb.ToString();
    }

    static void ResetSessionFile()
    {
        lock (fileLock)
        {
            activeLogFilePath = null;
            activeLogDirectory = null;
            headerWritten = false;
        }
    }

    #endregion
}
