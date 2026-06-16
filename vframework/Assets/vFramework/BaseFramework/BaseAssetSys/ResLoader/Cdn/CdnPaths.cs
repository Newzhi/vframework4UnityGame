using System.IO;
using UnityEngine;

/// <summary>
/// CDN / 热更缓存路径约定（ABCache → 首包 StreamingAssets）。
/// </summary>
public static class CdnPaths
{
    /// <summary>persistentDataPath/ABCache/{平台}/</summary>
    public static string GetCacheRoot()
    {
        return Path.Combine(
            Application.persistentDataPath,
            "ABCache",
            BundlePlatformPaths.GetRuntimeFolderName());
    }

    /// <summary>{cacheRoot}/Catalogue/AssetCatalog.json</summary>
    public static string GetCacheCataloguePath(string cacheRoot = null)
    {
        cacheRoot = cacheRoot ?? GetCacheRoot();
        return Path.Combine(cacheRoot, "Catalogue", CatalogueReader.RuntimeCatalogueFileName);
    }

    /// <summary>{cacheRoot}/{bundleName}</summary>
    public static string GetCacheBundlePath(string bundleName, string cacheRoot = null)
    {
        cacheRoot = cacheRoot ?? GetCacheRoot();
        return Path.Combine(cacheRoot, BundlePlatformPaths.NormalizeBundleName(bundleName));
    }

    public static void EnsureDirectory(string directoryPath)
    {
        if (string.IsNullOrEmpty(directoryPath))
            return;

        if (!Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);
    }
}
