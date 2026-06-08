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

public enum BuildMode
{
    EditorTest,
    DeviceDebug,
    CdnHotUpdate,
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

public enum BundlePackMethod
{
    ByFolder,
    ByAssetType,
    ByDependency,
    CustomRule,
}

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
    public BundlePackMethod packMethod = BundlePackMethod.ByFolder;
    public DownloadPriority downloadPriority = DownloadPriority.Normal;
    public ResourceCategory resourceCategory = ResourceCategory.Prefab;
    public string note;
}

#endregion

#region 打包规则 ScriptableObject

[CreateAssetMenu(fileName = "BuildSetting", menuName = "Test/Build Setting")]
public class BuildSetting : ScriptableObject
{
    public BuildPlatform platform = BuildPlatform.Windows;
    public string version = "1.0.0";
    public int buildNumber = 1001;
    public string outputPath = "Assets/StreamingAssets";

    public BuildMode buildMode = BuildMode.EditorTest;
    public PackingRule packingRule = PackingRule.Default;
    public string targetDirectory = "Assets/Test/AB_Test_Target";

    public List<BundleConfigItem> customItems = new List<BundleConfigItem>();
}

#endregion
