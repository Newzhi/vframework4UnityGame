using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class BundleBuilder
{
    #region 变量定义

    public const string SystemRoot = BundlePlatformPaths.SystemRoot;
    public const string DefaultSettingPath = BundlePlatformPaths.DefaultBuildSettingRelativePath;

    #endregion

    #region 打包

    public static bool Build(BuildSetting setting)
    {
        return Build(setting, BundleBuildExecutionMode.Incremental);
    }

    public static bool Build(BuildSetting setting, BundleBuildExecutionMode executionMode)
    {
        return BundleBuildPipeline.Execute(setting, executionMode);
    }

    public static void EnsureOutputDirectoryPublic(string outputPath)
    {
        EnsureOutputDirectory(outputPath);
    }

    #endregion

    #region 清理

    public static void Clean(BuildSetting setting)
    {
        if (setting != null)
        {
            CleanOutputPath(setting.deviceOutputPath, setting);
            CleanOutputPath(setting.cdnOutputPath, setting);
            BuildManifestService.DeleteCache(BundleBuilder.ResolveBundleRoot(BuildMode.DeviceDebug, setting));
            BuildManifestService.DeleteCache(BundleBuilder.ResolveBundleRoot(BuildMode.CdnHotUpdate, setting));
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

        AssetDatabase.Refresh();
        Debug.Log("已清理打包输出、清单与 BuildCache");
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

            // TODO: DlcPackage 校验独立 dlcOutputPath、DLC 包名/id
            if (setting.buildMode == BuildMode.DlcPackage && string.IsNullOrEmpty(setting.cdnOutputPath))
            {
                Debug.LogError("DLC分包模式：联网 CDN 输出路径暂作占位，不能为空（TODO：改为 dlcOutputPath）");
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
            bool needsDlcPath = false;
            foreach (BundleConfigItem item in setting.customItems)
            {
                if (item.buildMode == BuildMode.CdnHotUpdate)
                    needsCdnPath = true;
                if (item.buildMode == BuildMode.DlcPackage)
                    needsDlcPath = true;
            }

            if (needsCdnPath && string.IsNullOrEmpty(setting.cdnOutputPath))
            {
                Debug.LogError("联网 CDN 输出路径不能为空");
                return false;
            }

            // TODO: DlcPackage Custom 项校验 dlcOutputPath
            if (needsDlcPath && string.IsNullOrEmpty(setting.cdnOutputPath))
            {
                Debug.LogError("DLC分包配置项：输出路径占位不能为空（TODO：改为 dlcOutputPath）");
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
        string basePath;
        switch (mode)
        {
            case BuildMode.CdnHotUpdate:
                basePath = setting.cdnOutputPath;
                break;
            case BuildMode.DlcPackage:
                // TODO: setting.dlcOutputPath（如 Bundles/DLC/{PackageName}），与 CDN 热更主线分离
                basePath = setting.cdnOutputPath;
                break;
            case BuildMode.DeviceDebug:
                basePath = setting.deviceOutputPath;
                break;
            default:
                // TODO: EditorTest 占位 root，后续改为纯模拟清单目录
                basePath = setting.deviceOutputPath;
                break;
        }

        return ResolvePlatformOutputPath(basePath, setting.platform, setting.usePlatformSubfolders);
    }

    public static string ResolvePlatformOutputPath(
        string outputPath,
        BuildPlatform platform,
        bool usePlatformSubfolders)
    {
        return BundlePlatformPaths.ResolvePlatformOutputPath(outputPath, platform, usePlatformSubfolders);
    }

    public static string ResolveOutputPath(string outputPath)
    {
        return BundlePlatformPaths.ResolveBaseOutputPath(outputPath);
    }

    static void EnsureOutputDirectory(string outputPath)
    {
        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);
    }

    static void CleanOutputPath(string configPath, BuildSetting setting)
    {
        string outputPath = ResolvePlatformOutputPath(
            configPath,
            setting.platform,
            setting.usePlatformSubfolders);

        if (!Directory.Exists(outputPath))
            return;

        foreach (string file in Directory.GetFiles(outputPath, "*", SearchOption.AllDirectories))
        {
            if (ShouldDeleteBuildArtifact(file))
                DeleteOutputFile(file);
        }

        DeleteReportsArtifacts(outputPath);

        foreach (string file in Directory.GetFiles(outputPath, "*", SearchOption.TopDirectoryOnly))
        {
            if (ShouldDeleteExtensionlessManifest(file))
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

    static void DeleteReportsArtifacts(string outputPath)
    {
        string reportsDir = Path.Combine(outputPath, BundleBuildAnalyzer.ReportsFolderName);
        if (!Directory.Exists(reportsDir))
            return;

        foreach (string file in Directory.GetFiles(reportsDir, "*", SearchOption.TopDirectoryOnly))
            DeleteOutputFile(file);
    }

    static bool ShouldDeleteBuildArtifact(string filePath)
    {
        string name = Path.GetFileName(filePath);
        return name.EndsWith(RuleResolver.BundleSuffix)
            || name.EndsWith(".manifest")
            || name == RuntimeCatalogueFileName;
    }

    static bool ShouldDeleteExtensionlessManifest(string filePath)
    {
        if (Directory.Exists(filePath))
            return false;

        string name = Path.GetFileName(filePath);
        return !string.IsNullOrEmpty(name) && name.IndexOf('.') < 0;
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
