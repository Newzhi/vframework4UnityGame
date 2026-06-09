using System.IO;
using UnityEngine;

/// <summary>
/// 各平台 AB 输出/加载子目录名。构建与运行时共用，便于 Win/Android 等产物并存。
/// </summary>
public static class BundlePlatformPaths
{
    public const string WindowsFolder = "StandaloneWindows64";
    public const string AndroidFolder = "Android";
    public const string IOSFolder = "iOS";
    public const string MacFolder = "StandaloneOSX";
    public const string WebGLFolder = "WebGL";

    public static string GetFolderName(BuildPlatform platform)
    {
        switch (platform)
        {
            case BuildPlatform.Android:
                return AndroidFolder;
            case BuildPlatform.iOS:
                return IOSFolder;
            case BuildPlatform.macOS:
                return MacFolder;
            case BuildPlatform.WebGL:
                return WebGLFolder;
            default:
                return WindowsFolder;
        }
    }

    /// <summary>当前运行环境对应的平台子目录（Editor 下 Windows 编辑器等 → StandaloneWindows64）。</summary>
    public static string GetRuntimeFolderName()
    {
        switch (Application.platform)
        {
            case RuntimePlatform.Android:
                return AndroidFolder;
            case RuntimePlatform.IPhonePlayer:
                return IOSFolder;
            case RuntimePlatform.OSXPlayer:
            case RuntimePlatform.OSXEditor:
                return MacFolder;
            case RuntimePlatform.WebGLPlayer:
                return WebGLFolder;
            default:
                return WindowsFolder;
        }
    }

    public static string AppendPlatformFolder(string basePath, string platformFolder)
    {
        if (string.IsNullOrEmpty(basePath) || string.IsNullOrEmpty(platformFolder))
            return basePath;

        return Path.Combine(basePath, platformFolder);
    }

    /// <summary>将配置中的输出路径（如 Assets/StreamingAssets、Bundles/CDN）解析为绝对路径。</summary>
    public static string ResolveBaseOutputPath(string outputPath)
    {
        if (string.IsNullOrEmpty(outputPath))
            return Application.streamingAssetsPath;

        string normalized = outputPath.Replace("\\", "/");
        if (normalized == "Assets/StreamingAssets")
            return Application.streamingAssetsPath;

#if UNITY_EDITOR
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        return Path.GetFullPath(Path.Combine(projectRoot, normalized));
#else
        if (Path.IsPathRooted(outputPath))
            return Path.GetFullPath(outputPath);

        return Path.Combine(Application.streamingAssetsPath, normalized);
#endif
    }

    public static string ResolvePlatformOutputPath(
        string outputPath,
        BuildPlatform platform,
        bool usePlatformSubfolders)
    {
        string baseAbs = ResolveBaseOutputPath(outputPath);
        if (!usePlatformSubfolders)
            return baseAbs;

        return Path.GetFullPath(AppendPlatformFolder(baseAbs, GetFolderName(platform)));
    }

    /// <summary>运行时默认 bundle 根：首包 base + 当前平台子目录。</summary>
    public static string ResolveRuntimeBundleRoot(string baseOutputPath, bool usePlatformSubfolders)
    {
        string baseAbs = string.IsNullOrEmpty(baseOutputPath)
            ? Application.streamingAssetsPath
            : ResolveBaseOutputPath(baseOutputPath);

        if (!usePlatformSubfolders)
            return baseAbs;

        return Path.GetFullPath(AppendPlatformFolder(baseAbs, GetRuntimeFolderName()));
    }
}
