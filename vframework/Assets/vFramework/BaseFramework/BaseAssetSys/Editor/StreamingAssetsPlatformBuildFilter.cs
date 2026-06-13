using System;
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
    const string StateFileName = "moved-folders.json";

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

    static string StateFilePath => Path.Combine(BackupRoot, StateFileName);

    public int callbackOrder => 0;

    [InitializeOnLoadMethod]
    static void RecoverOrphanedBackupOnLoad()
    {
        if (!File.Exists(StateFilePath))
            return;

        if (!TryLoadState(out List<string> folders) || folders == null || folders.Count == 0)
            return;

        string streamingRoot = Path.Combine(Application.dataPath, "StreamingAssets");
        bool needsRestore = false;
        foreach (string folder in folders)
        {
            string backupPath = Path.Combine(BackupRoot, folder);
            string destPath = Path.Combine(streamingRoot, folder);
            if (Directory.Exists(backupPath) && !Directory.Exists(destPath))
            {
                needsRestore = true;
                break;
            }
        }

        if (needsRestore)
        {
            Debug.LogWarning(
                "[StreamingAssetsPlatformBuildFilter] 检测到未还原的平台目录备份，正在自动恢复…");
            RestoreFromBackup(folders);
        }
    }

    public void OnPreprocessBuild(BuildReport report)
    {
        string keepFolder = GetPlatformFolderForBuildTarget(report.summary.platform);
        if (string.IsNullOrEmpty(keepFolder))
            return;

        string streamingRoot = Path.Combine(Application.dataPath, "StreamingAssets");
        if (!Directory.Exists(streamingRoot))
            return;

        RestoreFromBackupIfPending();
        ClearBackupRoot();
        MovedFolderNames.Clear();

        foreach (string folder in KnownPlatformFolders)
        {
            if (folder == keepFolder)
                continue;

            if (TryMoveOut(streamingRoot, folder))
                MovedFolderNames.Add(folder);
        }

        if (MovedFolderNames.Count > 0)
        {
            SaveState(MovedFolderNames, keepFolder);
            AssetDatabase.Refresh();
            Debug.Log(
                "[StreamingAssetsPlatformBuildFilter] Player 构建仅保留 StreamingAssets/" +
                keepFolder + "/，已临时移出 " + MovedFolderNames.Count + " 个其它平台子目录。");
        }
    }

    public void OnPostprocessBuild(BuildReport report)
    {
        List<string> folders = MovedFolderNames.Count > 0
            ? new List<string>(MovedFolderNames)
            : LoadStateFolders();

        if (folders != null && folders.Count > 0)
            RestoreFromBackup(folders);
    }

    static bool TryMoveOut(string streamingRoot, string folderName)
    {
        string src = Path.Combine(streamingRoot, folderName);
        if (!Directory.Exists(src))
            return false;

        string assetPath = ToStreamingAssetsAssetPath(folderName);
        Directory.CreateDirectory(BackupRoot);
        string dest = Path.Combine(BackupRoot, folderName);

        if (Directory.Exists(dest) && !DeleteDirectoryRecursive(dest))
        {
            Debug.LogError(
                "[StreamingAssetsPlatformBuildFilter] 无法清理备份目录: " + dest);
            return false;
        }

        AssetDatabase.StartAssetEditing();
        try
        {
            if (!CopyDirectoryRecursive(src, dest))
            {
                Debug.LogError(
                    "[StreamingAssetsPlatformBuildFilter] 复制到备份失败: " + src + " → " + dest);
                return false;
            }

            if (!DeleteStreamingAssetsPlatformFolder(assetPath, src))
            {
                Debug.LogError(
                    "[StreamingAssetsPlatformBuildFilter] 移出 StreamingAssets 失败（拒绝访问）: " +
                    assetPath + "。请关闭占用该目录的进程后重试，或重启 Unity。");
                DeleteDirectoryRecursive(dest);
                return false;
            }

            return true;
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }
    }

    static void RestoreFromBackupIfPending()
    {
        List<string> pending = LoadStateFolders();
        if (pending != null && pending.Count > 0)
            RestoreFromBackup(pending);
    }

    static void RestoreFromBackup(List<string> folderNames)
    {
        if (folderNames == null || folderNames.Count == 0)
            return;

        string streamingRoot = Path.Combine(Application.dataPath, "StreamingAssets");
        Directory.CreateDirectory(streamingRoot);

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (string folderName in folderNames)
            {
                string src = Path.Combine(BackupRoot, folderName);
                if (!Directory.Exists(src))
                    continue;

                string dest = Path.Combine(streamingRoot, folderName);
                if (Directory.Exists(dest) && !DeleteDirectoryRecursive(dest))
                {
                    Debug.LogWarning(
                        "[StreamingAssetsPlatformBuildFilter] 还原时目标已存在且无法覆盖: " + dest);
                    continue;
                }

                if (!CopyDirectoryRecursive(src, dest))
                {
                    Debug.LogError(
                        "[StreamingAssetsPlatformBuildFilter] 还原复制失败: " + src + " → " + dest);
                    continue;
                }

                DeleteDirectoryRecursive(src);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        MovedFolderNames.Clear();
        ClearBackupRoot();
        AssetDatabase.Refresh();
        Debug.Log("[StreamingAssetsPlatformBuildFilter] 已还原 StreamingAssets 多平台子目录。");
    }

    static bool DeleteStreamingAssetsPlatformFolder(string assetPath, string absolutePath)
    {
        ClearReadOnlyRecursive(absolutePath);

        if (AssetDatabase.IsValidFolder(assetPath))
        {
            if (AssetDatabase.DeleteAsset(assetPath))
                return !Directory.Exists(absolutePath);

            AssetDatabase.Refresh();
        }

        if (!Directory.Exists(absolutePath))
            return true;

        try
        {
            FileUtil.DeleteFileOrDirectory(absolutePath);
            string metaPath = absolutePath + ".meta";
            if (File.Exists(metaPath))
                FileUtil.DeleteFileOrDirectory(metaPath);
            AssetDatabase.Refresh();
            return !Directory.Exists(absolutePath);
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[StreamingAssetsPlatformBuildFilter] Delete failed: " + absolutePath + " | " + ex.Message);
            return false;
        }
    }

    static bool CopyDirectoryRecursive(string sourceDir, string destDir)
    {
        if (!Directory.Exists(sourceDir))
            return false;

        Directory.CreateDirectory(destDir);
        ClearReadOnlyRecursive(sourceDir);

        foreach (string file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            string relative = file.Substring(sourceDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string targetFile = Path.Combine(destDir, relative);
            string targetDir = Path.GetDirectoryName(targetFile);
            if (!string.IsNullOrEmpty(targetDir))
                Directory.CreateDirectory(targetDir);

            try
            {
                File.Copy(file, targetFile, true);
            }
            catch (Exception ex)
            {
                Debug.LogError("[StreamingAssetsPlatformBuildFilter] Copy failed: " + file + " | " + ex.Message);
                return false;
            }
        }

        return true;
    }

    static bool DeleteDirectoryRecursive(string path)
    {
        if (!Directory.Exists(path))
            return true;

        ClearReadOnlyRecursive(path);
        try
        {
            Directory.Delete(path, true);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[StreamingAssetsPlatformBuildFilter] DeleteDirectory: " + path + " | " + ex.Message);
            try
            {
                FileUtil.DeleteFileOrDirectory(path);
                return !Directory.Exists(path);
            }
            catch
            {
                return false;
            }
        }
    }

    static void ClearReadOnlyRecursive(string path)
    {
        if (Directory.Exists(path))
        {
            foreach (string file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                TryClearReadOnly(file);
            return;
        }

        if (File.Exists(path))
            TryClearReadOnly(path);
    }

    static void TryClearReadOnly(string filePath)
    {
        try
        {
            FileAttributes attrs = File.GetAttributes(filePath);
            if ((attrs & FileAttributes.ReadOnly) != 0)
                File.SetAttributes(filePath, attrs & ~FileAttributes.ReadOnly);
        }
        catch
        {
            // ignored
        }
    }

    static string ToStreamingAssetsAssetPath(string folderName)
    {
        return "Assets/StreamingAssets/" + folderName;
    }

    static void SaveState(List<string> movedFolders, string keepFolder)
    {
        var state = new BackupState
        {
            keepFolder = keepFolder,
            movedFolders = movedFolders.ToArray()
        };

        Directory.CreateDirectory(BackupRoot);
        File.WriteAllText(StateFilePath, JsonUtility.ToJson(state, true));
    }

    static List<string> LoadStateFolders()
    {
        if (!TryLoadState(out List<string> folders))
            return null;

        return folders;
    }

    static bool TryLoadState(out List<string> folders)
    {
        folders = null;
        if (!File.Exists(StateFilePath))
            return false;

        try
        {
            BackupState state = JsonUtility.FromJson<BackupState>(File.ReadAllText(StateFilePath));
            if (state?.movedFolders == null || state.movedFolders.Length == 0)
                return false;

            folders = new List<string>(state.movedFolders);
            return true;
        }
        catch
        {
            return false;
        }
    }

    static void ClearBackupRoot()
    {
        if (!Directory.Exists(BackupRoot))
            return;

        try
        {
            Directory.Delete(BackupRoot, true);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[StreamingAssetsPlatformBuildFilter] ClearBackupRoot: " + ex.Message);
        }
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

    [Serializable]
    class BackupState
    {
        public string keepFolder;
        public string[] movedFolders;
    }
}
