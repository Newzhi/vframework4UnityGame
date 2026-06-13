using System;
using System.Collections.Generic;
using UnityEngine;

#region 枚举 - 目标平台

public enum BuildPlatform
{
    Windows,
    iOS,
    Android,
    macOS,
    WebGL,
}

#endregion

#region 枚举 - 打包模式

// UI 文案：EditorTest=编辑器测试，DeviceDebug=首包（真机模式），CdnHotUpdate=CDN联网，DlcPackage=DLC分包
public enum BuildMode
{
    EditorTest,
    DeviceDebug,
    CdnHotUpdate,
    DlcPackage,
}

#endregion

#region 枚举 - 打包规则

public enum PackingRule
{
    Default,
    Detailed,
    Custom,
}

#endregion

#region 枚举 - 自定义配置项

public enum DownloadPriority
{
    High,
    Normal,
    Low,
    Optional,
}

/// <summary>自定义项为文件夹路径时的 AB 拆分粒度（与全局 Default/Detailed 规则语义一致）。</summary>
public enum BundleFolderRule
{
    /// <summary>整个目录打成一个包，使用配置项「包名」。</summary>
    EntireFolder,
    /// <summary>目录下每个第一级子文件夹各打一个包，包名取子文件夹名。</summary>
    FirstLevelSubfolders,
    /// <summary>目录下每一个子文件夹（含嵌套）各打一个包。</summary>
    AllSubfolders,
}

#endregion

#region 自定义打包配置项

[Serializable]
public class BundleConfigItem
{
    public string assetPath = "Assets/";
    public string bundleName = "bundle";
    public BuildMode buildMode = BuildMode.EditorTest;
    public DownloadPriority downloadPriority = DownloadPriority.Normal;
    /// <summary>资源路径为文件夹时生效；单文件路径时忽略。</summary>
    public BundleFolderRule folderPackingRule = BundleFolderRule.EntireFolder;
    public string note;
}

#endregion

#region 打包规则 ScriptableObject

[CreateAssetMenu(fileName = "BuildSetting", menuName = "vFramework/Build Setting")]
public class BuildSetting : ScriptableObject
{
    public BuildPlatform platform = BuildPlatform.Windows;
    public string version = "1.0.0";
    public int buildNumber = 1001;
    public string deviceOutputPath = "Assets/StreamingAssets";
    public string cdnOutputPath = "Bundles/CDN";
    /// <summary>为 true 时在输出路径下追加平台子目录（如 StandaloneWindows64、Android），多端产物可并存。</summary>
    public bool usePlatformSubfolders = true;

    public BuildMode buildMode = BuildMode.DeviceDebug;
    public PackingRule packingRule = PackingRule.Default;
    public string targetDirectory = "Assets/AssetBundle";

    public List<BundleConfigItem> customItems = new List<BundleConfigItem>();

    [Header("Catalogue / 构建分析")]
    [Tooltip("写入 bundles[] 时对依赖做拓扑排序（推荐开启）")]
    public bool useTopologicalSort = true;

    [Tooltip("打包成功后生成 BundleBuildReport.json")]
    public bool runBuildAnalyzer = true;

    [Tooltip("loadPath 重复时阻断写清单；关闭则仅 Warning")]
    public bool loadPathDuplicateAsError = false;
}

#endregion
