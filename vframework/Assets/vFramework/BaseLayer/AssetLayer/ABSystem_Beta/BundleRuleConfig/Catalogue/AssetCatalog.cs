using System;

#region 资源清单 - 单条资源

/// <summary>
/// 单条资源在清单中的定位：逻辑路径 → 所在 bundle + 包内 asset 名。
/// </summary>
/// <remarks>
/// 包间依赖不在此记录，见 BundleCatalogInfo / AssetCatalog.bundles。
/// </remarks>
[Serializable]
public class AssetCatalogEntry
{
    public string assetPath;
    public string bundleName;
    public string assetName;
}

#endregion

#region 资源清单 - Bundle 依赖

/// <summary>
/// 按 AssetBundle 粒度记录「直接依赖的其他 bundle 名」。
/// 与 AssetCatalogEntry（资源→包）互补，供加载器在 LoadAsset 前先 AcquireBundle 依赖包。
/// </summary>
/// <remarks>
/// 由 CatalogueWriter 从 AssetBundleManifest 写入 AssetCatalog.json。
/// 不要放在 AssetCatalogEntry 上重复记录——同一 bundle 内所有 asset 的依赖相同。
/// </remarks>
[Serializable]
public class BundleCatalogInfo
{
    /// <summary>本包名，如 ui.bundle</summary>
    public string bundleName;

    /// <summary>
    /// 直接依赖的包名列表（仅 bundle 文件名，不含路径）。
    /// 例：ui.bundle 依赖 atlas.bundle、common.bundle。
    /// </summary>
    public string[] dependencies;
}

#endregion

#region 资源清单 - 根结构

/// <summary>
/// 资源清单根结构（当前 JSON；后续可能改二进制，见 Docs/TODO.md）。
/// </summary>
/// <remarks>
/// 两张逻辑表：
/// 1. entries（已实现）— 每个资源 assetPath 落在哪个 bundle、assetName 是什么。
/// 2. bundles — 每个 bundle 依赖哪些其它 bundle，见 BundleCatalogInfo。
/// 加载器：Load(简路径) → 查 loadPathMap → LoadByBundle → bundles 依赖 → LoadAsset。
/// 详细说明：Docs/Catalogue清单说明.md
/// </remarks>
[Serializable]
public class AssetCatalog
{
    public string version;
    public int buildNumber;
    public string platform;
    public string buildMode;
    public string packingRule;
    public string bundleRoot;
    /// <summary>打包资源根目录（BuildSetting.targetDirectory），用于解析业务 Load 简路径。</summary>
    public string resourceRoot;

    /// <summary>资源 → 包 映射表</summary>
    public AssetCatalogEntry[] entries;

    /// <summary>bundle → 依赖 bundle 映射表</summary>
    public BundleCatalogInfo[] bundles;
}

#endregion
