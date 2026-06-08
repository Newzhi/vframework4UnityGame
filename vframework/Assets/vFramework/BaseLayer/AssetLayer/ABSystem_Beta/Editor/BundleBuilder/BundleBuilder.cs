using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class BundleBuilder
{
    #region 变量定义

    public const string SystemRoot = "Assets/vFramework/BaseLayer/AssetLayer/ABSystem_Beta";
    public const string DefaultSettingPath = SystemRoot + "/BundleRuleConfig/Setting/DefaultBuildSetting.asset";

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

        BuildTarget target = ToBuildTarget(setting.platform);
        BuildAssetBundleOptions options = BuildAssetBundleOptions.ChunkBasedCompression;

        if (setting.packingRule == PackingRule.Custom)
            return BuildCustom(setting, target, options);

        List<AssetBundleBuild> builds = RuleResolver.Resolve(setting);
        if (builds.Count == 0)
        {
            Debug.LogError("没有可打包的内容");
            return false;
        }

        BuildByMode(setting.buildMode, builds, setting, target, options);
        AssetDatabase.Refresh();
        Debug.Log("打包完成，bundle 数量: " + builds.Count);
        return true;
    }

    static bool BuildCustom(BuildSetting setting, BuildTarget target, BuildAssetBundleOptions options)
    {
        Dictionary<BuildMode, List<AssetBundleBuild>> grouped =
            RuleResolver.ResolveCustomGrouped(setting.customItems);

        int totalCount = 0;
        bool anyBuild = false;

        foreach (KeyValuePair<BuildMode, List<AssetBundleBuild>> pair in grouped)
        {
            if (pair.Value.Count == 0)
                continue;

            anyBuild = true;
            totalCount += pair.Value.Count;
            BuildByMode(pair.Key, pair.Value, setting, target, options);
        }

        if (!anyBuild)
        {
            Debug.LogError("没有可打包的内容");
            return false;
        }

        AssetDatabase.Refresh();
        Debug.Log("自定义打包完成，bundle 数量: " + totalCount);
        return true;
    }

    static void BuildByMode(
        BuildMode mode,
        List<AssetBundleBuild> builds,
        BuildSetting setting,
        BuildTarget target,
        BuildAssetBundleOptions options)
    {
        string bundleRoot = ResolveBundleRoot(mode, setting);
        EnsureOutputDirectory(bundleRoot);

        if (mode != BuildMode.EditorTest)
            BuildPipeline.BuildAssetBundles(bundleRoot, builds.ToArray(), options, target);
            // TODO: 接收 BuildAssetBundles 返回值，将 AssetBundleManifest 传入 CatalogueWriter，
            //       以写入 bundles 依赖表。见 Docs/Catalogue清单说明.md。

        // TODO: 按打包模式区分清单生成策略（编辑器模拟 / 首包 / CDN）；依赖表见 CatalogueWriter.BuildBundleDependencies
        CatalogueWriter.Write(setting, builds.ToArray(), bundleRoot);
    }

    public static void Clean(BuildSetting setting)
    {
        if (setting != null)
        {
            CleanOutputPath(setting.deviceOutputPath);
            CleanOutputPath(setting.cdnOutputPath);
        }

        string cataloguePath = Path.Combine(
            Directory.GetParent(Application.dataPath).FullName,
            CatalogueWriter.CatalogueAssetPath);

        if (File.Exists(cataloguePath))
        {
            string catalogueRelative = CatalogueWriter.CatalogueAssetPath;
            if (AssetDatabase.LoadAssetAtPath<Object>(catalogueRelative) != null)
                AssetDatabase.DeleteAsset(catalogueRelative);
            else
                DeleteFileAndMeta(cataloguePath);
        }

        // TODO: 清理 Simulate 目录

        AssetDatabase.Refresh();
        Debug.Log("已清理打包输出与清单");
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

            if (setting.buildMode == BuildMode.DeviceDebug && string.IsNullOrEmpty(setting.deviceOutputPath))
            {
                Debug.LogError("真机环境输出路径不能为空");
                return false;
            }

            if (setting.buildMode == BuildMode.CdnHotUpdate && string.IsNullOrEmpty(setting.cdnOutputPath))
            {
                Debug.LogError("联网 CDN 输出路径不能为空");
                return false;
            }
        }
        else
        {
            if (setting.customItems == null || setting.customItems.Count == 0)
            {
                Debug.LogError("自定义打包模式下至少需要一个配置项");
                return false;
            }

            if (string.IsNullOrEmpty(setting.deviceOutputPath))
            {
                Debug.LogError("真机环境输出路径不能为空");
                return false;
            }

            bool needsCdnPath = false;
            foreach (BundleConfigItem item in setting.customItems)
            {
                if (item.buildMode == BuildMode.CdnHotUpdate)
                {
                    needsCdnPath = true;
                    break;
                }
            }

            if (needsCdnPath && string.IsNullOrEmpty(setting.cdnOutputPath))
            {
                Debug.LogError("联网 CDN 输出路径不能为空");
                return false;
            }
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

    public static string ResolveBundleRoot(BuildMode mode, BuildSetting setting)
    {
        switch (mode)
        {
            case BuildMode.CdnHotUpdate:
                return ResolveOutputPath(setting.cdnOutputPath);
            case BuildMode.DeviceDebug:
                return ResolveOutputPath(setting.deviceOutputPath);
            default:
                // TODO: EditorTest 占位 root，后续改为纯模拟清单目录
                return ResolveOutputPath(setting.deviceOutputPath);
        }
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

    static void EnsureOutputDirectory(string outputPath)
    {
        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);
    }

    static void CleanOutputPath(string configPath)
    {
        string outputPath = ResolveOutputPath(configPath);
        if (!Directory.Exists(outputPath))
            return;

        foreach (string file in Directory.GetFiles(outputPath, "*", SearchOption.AllDirectories))
        {
            string name = Path.GetFileName(file);
            if (!name.EndsWith(RuleResolver.BundleSuffix)
                && !name.EndsWith(".manifest")
                && name != RuntimeCatalogueFileName)
            {
                continue;
            }

            DeleteOutputFile(file);
        }

        string runtimeCatalogueDir = Path.Combine(outputPath, "Catalogue");
        if (Directory.Exists(runtimeCatalogueDir))
        {
            foreach (string file in Directory.GetFiles(runtimeCatalogueDir, "*", SearchOption.AllDirectories))
                DeleteOutputFile(file);

            string relativeCatalogueDir = ToAssetsRelativePath(runtimeCatalogueDir);
            if (!string.IsNullOrEmpty(relativeCatalogueDir) && AssetDatabase.IsValidFolder(relativeCatalogueDir))
                AssetDatabase.DeleteAsset(relativeCatalogueDir);
            else if (Directory.Exists(runtimeCatalogueDir) && Directory.GetFiles(runtimeCatalogueDir).Length == 0)
                Directory.Delete(runtimeCatalogueDir, true);
        }
    }

    static void DeleteOutputFile(string filePath)
    {
        string relative = ToAssetsRelativePath(filePath);
        if (!string.IsNullOrEmpty(relative) && relative.StartsWith("Assets/"))
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(relative) != null
                || File.Exists(Path.Combine(Directory.GetParent(Application.dataPath).FullName, relative)))
            {
                AssetDatabase.DeleteAsset(relative);
                return;
            }
        }

        DeleteFileAndMeta(filePath);
    }

    static void DeleteFileAndMeta(string filePath)
    {
        if (File.Exists(filePath))
            File.Delete(filePath);

        string metaPath = filePath + ".meta";
        if (File.Exists(metaPath))
            File.Delete(metaPath);
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
        if (string.IsNullOrEmpty(absPath))
            return null;

        absPath = absPath.Replace("\\", "/");
        string dataPath = Application.dataPath.Replace("\\", "/");

        if (!absPath.StartsWith(dataPath))
            return null;

        return "Assets" + absPath.Substring(dataPath.Length);
    }

    public static string ToAbsoluteAssetsPath(string assetsRelativePath)
    {
        string assetsRoot = Application.dataPath;

        if (string.IsNullOrEmpty(assetsRelativePath))
            return assetsRoot;

        string normalized = assetsRelativePath.Replace("\\", "/");
        string absPath;

        if (normalized == "Assets")
            absPath = assetsRoot;
        else if (normalized.StartsWith("Assets/"))
            absPath = Path.GetFullPath(Path.Combine(assetsRoot, normalized.Substring("Assets/".Length)));
        else if (Path.IsPathRooted(assetsRelativePath))
            absPath = Path.GetFullPath(assetsRelativePath);
        else
            absPath = Path.GetFullPath(Path.Combine(assetsRoot, assetsRelativePath));

        if (Directory.Exists(absPath))
            return absPath;

        string parent = Path.GetDirectoryName(absPath);
        if (!string.IsNullOrEmpty(parent) && Directory.Exists(parent))
            return parent;

        return assetsRoot;
    }

    const string RuntimeCatalogueFileName = CatalogueWriter.RuntimeCatalogueFileName;

    #endregion
}
