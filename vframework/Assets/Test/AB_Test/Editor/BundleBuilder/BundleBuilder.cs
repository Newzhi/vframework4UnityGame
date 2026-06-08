using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class BundleBuilder
{
    #region 变量定义

    public const string DefaultSettingPath = "Assets/Test/AB_Test/BundleRuleConfig/Setting/DefaultBuildSetting.asset";

    #endregion

    #region 打包

    public static bool Build(BuildSetting setting)
    {
        if (setting == null)
        {
            Debug.LogError("BuildSetting 为空");
            return false;
        }

        if (!Validate(setting))
            return false;

        List<AssetBundleBuild> builds = RuleResolver.Resolve(setting);
        if (builds.Count == 0)
        {
            Debug.LogError("没有可打包的内容");
            return false;
        }

        string outputPath = ResolveOutputPath(setting.outputPath);
        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

        BuildTarget target = ToBuildTarget(setting.platform);
        BuildAssetBundleOptions options = BuildAssetBundleOptions.ChunkBasedCompression;

        BuildPipeline.BuildAssetBundles(outputPath, builds.ToArray(), options, target);
        CatalogueWriter.Write(setting, builds.ToArray(), outputPath);
        AssetDatabase.Refresh();

        Debug.Log("打包完成，输出: " + outputPath + "，bundle 数量: " + builds.Count);
        return true;
    }

    public static void Clean(BuildSetting setting)
    {
        string outputPath = ResolveOutputPath(setting != null ? setting.outputPath : "Assets/StreamingAssets");
        if (!Directory.Exists(outputPath))
            return;

        foreach (string file in Directory.GetFiles(outputPath, "*", SearchOption.AllDirectories))
        {
            string name = Path.GetFileName(file);
            if (name.EndsWith(RuleResolver.BundleSuffix)
                || name.EndsWith(".manifest")
                || name == RuntimeCatalogueFileName)
            {
                File.Delete(file);
            }
        }

        string cataloguePath = Path.Combine(
            Directory.GetParent(Application.dataPath).FullName,
            CatalogueWriter.CatalogueAssetPath);

        if (File.Exists(cataloguePath))
            File.Delete(cataloguePath);

        AssetDatabase.Refresh();
        Debug.Log("已清理打包输出: " + outputPath);
    }

    #endregion

    #region 辅助函数

    public static bool Validate(BuildSetting setting)
    {
        if (setting.packingRule != PackingRule.Custom)
        {
            if (!AssetDatabase.IsValidFolder(setting.targetDirectory))
            {
                Debug.LogError("目标资源目录不存在: " + setting.targetDirectory);
                return false;
            }
        }
        else if (setting.customItems == null || setting.customItems.Count == 0)
        {
            Debug.LogError("自定义打包模式下至少需要一个配置项");
            return false;
        }

        return true;
    }

    public static string[] CollectAssetPaths(string folder)
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

    public static string ResolveOutputPath(string outputPath)
    {
        if (string.IsNullOrEmpty(outputPath))
            return Application.streamingAssetsPath;

        string normalized = outputPath.Replace("\\", "/");
        if (normalized == "Assets/StreamingAssets")
            return Application.streamingAssetsPath;

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        return Path.GetFullPath(Path.Combine(projectRoot, normalized));
    }

    public static BuildTarget ToBuildTarget(BuildPlatform platform)
    {
        switch (platform)
        {
            case BuildPlatform.iOS:
                return BuildTarget.iOS;
            case BuildPlatform.Android:
                return BuildTarget.Android;
            case BuildPlatform.macOS:
                return BuildTarget.StandaloneOSX;
            case BuildPlatform.WebGL:
                return BuildTarget.WebGL;
            default:
                return BuildTarget.StandaloneWindows64;
        }
    }

    public static string ToAssetsRelativePath(string absPath)
    {
        absPath = absPath.Replace("\\", "/");
        string dataPath = Application.dataPath.Replace("\\", "/");

        if (!absPath.StartsWith(dataPath))
            return null;

        return "Assets" + absPath.Substring(dataPath.Length);
    }

    const string RuntimeCatalogueFileName = CatalogueWriter.RuntimeCatalogueFileName;

    #endregion
}
