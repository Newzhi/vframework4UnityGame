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

// UI 文案：EditorTest=编辑器测试，DeviceDebug=真机模式/首包，CdnHotUpdate=CDN联网，DlcPackage=DLC分包
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

public enum ResourceCategory
{
    Scene,
    Prefab,
    MaterialTexture,
    Audio,
    Script,
    ConfigData,
    Mod,
    Other,
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
    public ResourceCategory resourceCategory = ResourceCategory.Prefab;
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
}

#endregion
