using System;
using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>
/// CDN 热更链路集成测试（默认禁用）。需 DeviceDebug 打包且清单含 cdnBaseUrl 方可跑下载 Case。
/// 本地模拟：将 BuildSetting.cdnBaseUrl 指向本机静态目录（含 Catalogue/ 与 bundle 文件）。
/// </summary>
public class MyCdnHotUpdateTest : AbLoadTestRunnerBase
{
    const int TotalCases = 5;

    protected override string LogSource => "MyCdnHotUpdateTest";
    protected override int CaseCount => TotalCases;

    protected override IEnumerator RunCase(int caseId)
    {
        switch (caseId)
        {
            case 0: CaseInitWithOptionalCdn(); break;
            case 1: CaseCacheRootExists(); break;
            case 2: CaseCatalogueHashField(); break;
            case 3: CasePathResolverOrder(); break;
            case 4: CaseCdnBaseUrlField(); break;
        }

        yield return null;
    }

    void CaseInitWithOptionalCdn()
    {
        if (!BundleResLoader.Instance.EnsureReady())
        {
            LogFail("Init", "EnsureReady failed");
            return;
        }

        LogOk("Init", "BundleResLoader ready buildMode="
            + BundleResLoader.Instance.GetCatalogue()?.Catalog?.buildMode);
    }

    void CaseCacheRootExists()
    {
        string cacheRoot = CdnPaths.GetCacheRoot();
        if (string.IsNullOrEmpty(cacheRoot))
        {
            LogFail("CacheRoot", "empty");
            return;
        }

        CdnPaths.EnsureDirectory(cacheRoot);
        LogOk("CacheRoot", cacheRoot);
    }

    void CaseCatalogueHashField()
    {
        AssetCatalog catalog = BundleResLoader.Instance.GetCatalogue()?.Catalog;
        if (catalog == null)
        {
            LogFail("CatalogueHash", "catalog null");
            return;
        }

        if (string.IsNullOrEmpty(catalog.catalogueHash))
        {
            LogSkip("CatalogueHash", "catalogueHash empty — repack with P1-B pipeline");
            return;
        }

        LogOk("CatalogueHash", catalog.catalogueHash.Substring(0, Math.Min(12, catalog.catalogueHash.Length)) + "...");
    }

    void CasePathResolverOrder()
    {
        string primary = BundleResLoader.GetDefaultRuntimeBundleRoot();
        var resolver = DefaultBundlePathResolver.Create(primary);
        string cache = resolver.CacheRoot;
        if (string.IsNullOrEmpty(cache))
        {
            LogFail("PathResolver", "cache root empty");
            return;
        }

        LogOk("PathResolver", "cache=" + cache + " primary=" + primary);
    }

    void CaseCdnBaseUrlField()
    {
        AssetCatalog catalog = BundleResLoader.Instance.GetCatalogue()?.Catalog;
        if (catalog == null)
        {
            LogFail("CdnBaseUrl", "catalog null");
            return;
        }

        if (string.IsNullOrEmpty(catalog.cdnBaseUrl))
        {
            LogSkip("CdnBaseUrl", "未配置 cdnBaseUrl — 在 BuildSetting 填写后重打包");
            return;
        }

        string cacheCatalogue = CdnPaths.GetCacheCataloguePath();
        LogOk("CdnBaseUrl", catalog.cdnBaseUrl + " cacheCatalogue=" + cacheCatalogue);
    }
}
