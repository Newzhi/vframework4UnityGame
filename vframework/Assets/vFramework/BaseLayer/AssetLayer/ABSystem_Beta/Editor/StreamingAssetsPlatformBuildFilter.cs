using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Player 构建前临时移出 StreamingAssets 下非目标平台的 AB 子目录，避免 Win 包带 Android AB、APK 带 Windows AB。
/// 构建结束后还原，工程内仍可 Win/Android 并存供 Editor 联调。
/// </summary>
public class StreamingAssetsPlatformBuildFilter : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    const string BackupFolderName = "StreamingAssetsPlatformBackup";

    static readonly string[] KnownPlatformFolders =
    {
        BundlePlatformPaths.WindowsFolder,
        BundlePlatformPaths.AndroidFolder,
        BundlePlatformPaths.IOSFolder,
        BundlePlatformPaths.MacFolder,
        BundlePlatformPaths.WebGLFolder,
    };

    static readonly List<string> MovedFolderNames = new List<string>();

    static string BackupRoot =>
        Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", BackupFolderName));

    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        string keepFolder = GetPlatformFolderForBuildTarget(report.summary.platform);
        if (string.IsNullOrEmpty(keepFolder))
            return;

        string streamingRoot = Path.Combine(Application.dataPath, "StreamingAssets");
        if (!Directory.Exists(streamingRoot))
            return;

        ClearBackupRoot();
        MovedFolderNames.Clear();

        foreach (string folder in KnownPlatformFolders)
        {
            if (folder == keepFolder)
                continue;

            MoveOut(streamingRoot, folder);
        }

        if (MovedFolderNames.Count > 0)
        {
            AssetDatabase.Refresh();
            Debug.Log(
                "[StreamingAssetsPlatformBuildFilter] Player 构建仅保留 StreamingAssets/" +
                keepFolder + "/，已临时移出 " + MovedFolderNames.Count + " 个其它平台子目录。");
        }
    }

    public void OnPostprocessBuild(BuildReport report)
    {
        Restore();
    }

    static void MoveOut(string streamingRoot, string folderName)
    {
        string src = Path.Combine(streamingRoot, folderName);
        if (!Directory.Exists(src))
            return;

        Directory.CreateDirectory(BackupRoot);
        string dest = Path.Combine(BackupRoot, folderName);
        if (Directory.Exists(dest))
            FileUtil.DeleteFileOrDirectory(dest);

        FileUtil.MoveFileOrDirectory(src, dest);
        MovedFolderNames.Add(folderName);

        string srcMeta = src + ".meta";
        if (File.Exists(srcMeta))
        {
            string destMeta = dest + ".meta";
            FileUtil.MoveFileOrDirectory(srcMeta, destMeta);
        }
    }

    static void Restore()
    {
        if (MovedFolderNames.Count == 0)
            return;

        string streamingRoot = Path.Combine(Application.dataPath, "StreamingAssets");
        foreach (string folderName in MovedFolderNames)
        {
            string src = Path.Combine(BackupRoot, folderName);
            if (!Directory.Exists(src))
                continue;

            string dest = Path.Combine(streamingRoot, folderName);
            if (Directory.Exists(dest))
                FileUtil.DeleteFileOrDirectory(dest);

            FileUtil.MoveFileOrDirectory(src, dest);

            string srcMeta = src + ".meta";
            string destMeta = dest + ".meta";
            if (File.Exists(srcMeta))
                FileUtil.MoveFileOrDirectory(srcMeta, destMeta);
        }

        MovedFolderNames.Clear();
        ClearBackupRoot();
        AssetDatabase.Refresh();
        Debug.Log("[StreamingAssetsPlatformBuildFilter] 已还原 StreamingAssets 多平台子目录。");
    }

    static void ClearBackupRoot()
    {
        if (Directory.Exists(BackupRoot))
            FileUtil.DeleteFileOrDirectory(BackupRoot);
    }

    static string GetPlatformFolderForBuildTarget(BuildTarget target)
    {
        switch (target)
        {
            case BuildTarget.Android:
                return BundlePlatformPaths.AndroidFolder;
            case BuildTarget.iOS:
                return BundlePlatformPaths.IOSFolder;
            case BuildTarget.StandaloneOSX:
                return BundlePlatformPaths.MacFolder;
            case BuildTarget.WebGL:
                return BundlePlatformPaths.WebGLFolder;
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                return BundlePlatformPaths.WindowsFolder;
            default:
                Debug.LogWarning(
                    "[StreamingAssetsPlatformBuildFilter] 未映射 BuildTarget " +
                    target + "，跳过平台过滤。");
                return null;
        }
    }
}
