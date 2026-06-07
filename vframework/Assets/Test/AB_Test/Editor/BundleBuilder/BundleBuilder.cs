using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class BundleBuilder
{
    #region 变量定义

    private const string DefaultTargetFolder = "Assets/Test/AB_Test_Target";
    private const string BundleSuffix = ".bundle";

    #endregion

    #region Unity编辑器顶部的工具调用呼出菜单

    [MenuItem("Test/Build AssetBundles (子文件夹分包)")]
    static void BuildFromMenu()
    {
        string absPath = EditorUtility.OpenFolderPanel("选择打包目标文件夹", DefaultTargetFolder, "");
        if (string.IsNullOrEmpty(absPath))
            return;

        string targetFolder = ToAssetsRelativePath(absPath);
        if (string.IsNullOrEmpty(targetFolder))
        {
            Debug.LogError("目标文件夹必须在 Assets 目录下");
            return;
        }

        Build(targetFolder);
    }

    #endregion

    #region 打包

    //规则：目标文件夹下每个一级子文件夹打成一个bundle，输出到StreamingAssets
    public static void Build(string targetFolder)
    {
        if (!AssetDatabase.IsValidFolder(targetFolder))
        {
            Debug.LogError("目标文件夹不存在: " + targetFolder);
            return;
        }

        string outputPath = Application.streamingAssetsPath;
        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

        string[] subFolders = AssetDatabase.GetSubFolders(targetFolder);
        if (subFolders.Length == 0)
        {
            Debug.LogError("目标文件夹下没有子文件夹: " + targetFolder);
            return;
        }

        List<AssetBundleBuild> builds = new List<AssetBundleBuild>();

        foreach (string subFolder in subFolders)
        {
            string[] assetPaths = CollectAssetPaths(subFolder);
            if (assetPaths.Length == 0)
            {
                Debug.LogWarning("子文件夹内没有可打包资源，已跳过: " + subFolder);
                continue;
            }

            string bundleName = Path.GetFileName(subFolder) + BundleSuffix;
            builds.Add(new AssetBundleBuild
            {
                assetBundleName = bundleName,
                assetNames = assetPaths
            });
        }

        if (builds.Count == 0)
        {
            Debug.LogError("没有可打包的内容");
            return;
        }

        BuildAssetBundleOptions options = BuildAssetBundleOptions.ChunkBasedCompression;
        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;

        BuildPipeline.BuildAssetBundles(outputPath, builds.ToArray(), options, target);
        AssetDatabase.Refresh();

        Debug.Log("打包完成，输出目录: " + outputPath + "，共 " + builds.Count + " 个 bundle");
    }

    #endregion

    #region 辅助函数

    static string[] CollectAssetPaths(string folder)
    {
        string[] guids = AssetDatabase.FindAssets("", new[] { folder });
        List<string> paths = new List<string>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetDatabase.IsValidFolder(path))
                continue;
            if (path.EndsWith(".cs"))
                continue;

            paths.Add(path);
        }

        return paths.ToArray();
    }

    static string ToAssetsRelativePath(string absPath)
    {
        absPath = absPath.Replace("\\", "/");
        string dataPath = Application.dataPath.Replace("\\", "/");

        if (!absPath.StartsWith(dataPath))
            return null;

        return "Assets" + absPath.Substring(dataPath.Length);
    }

    #endregion
}
