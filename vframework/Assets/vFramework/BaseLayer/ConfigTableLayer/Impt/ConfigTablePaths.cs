namespace BaseLayer.ConfigTable
{
    /// <summary>
    /// 配置表资源路径约定（与 ExcelTool export-config 及 AssetBundle Packer Default 规则对齐）。
    /// </summary>
    public static class ConfigTablePaths
    {
        /// <summary>Unity 工程内 bytes 源目录。</summary>
        public const string AssetFolder = "Assets/AssetBundle/ConfigTables";

        /// <summary>运行时 catalogue loadPath 前缀（无扩展名）。</summary>
        public const string LoadPathPrefix = "ConfigTables";

        /// <summary>Default 打包规则下 ConfigTables 文件夹对应的 bundle 名。</summary>
        public const string BundleName = "configtables.bundle";
    }
}
