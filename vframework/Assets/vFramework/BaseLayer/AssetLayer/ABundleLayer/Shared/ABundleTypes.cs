// ABundleTypes.cs — 公共层（Shared）
// 用途：ABundle 子模块跨层共享的枚举与常量（平台、分包模式、加载模式等）。

namespace vFramework.BaseLayer.AssetLayer.ABundleLayer
{
    #region 加载模式

    /// <summary>
    /// EditorSimulation：Editor 下 AssetDatabase 直读工程资源。
    /// RuntimeBundle：从已打包 AB 文件 LoadFromFile。
    /// </summary>
    public enum ABundleLoadMode
    {
        EditorSimulation = 0,
        RuntimeBundle = 1,
    }

    #endregion

    #region 分包模式

    public enum ABundlePackMode
    {
        ByTopLevelFolder = 0,
        ByDirectoryTree = 1,
        SingleRootBundle = 2,
        CustomRules = 3,
    }

    #endregion

    #region 平台

    public enum ABundlePlatform
    {
        Windows = 0,
        iOS = 1,
        Android = 2,
    }

    public static class ABundlePlatformNames
    {
        public static readonly string[] All = { "Windows", "iOS", "Android" };

        public static ABundlePlatform Parse(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return ABundlePlatform.Windows;
            }

            if (System.Enum.TryParse(value, true, out ABundlePlatform platform))
            {
                return platform;
            }

            if (value.Contains("iOS") || value.Contains("iPhone"))
            {
                return ABundlePlatform.iOS;
            }

            if (value.Contains("Android"))
            {
                return ABundlePlatform.Android;
            }

            return ABundlePlatform.Windows;
        }

        public static string ToName(ABundlePlatform platform) => platform.ToString();
    }

    #endregion
}
