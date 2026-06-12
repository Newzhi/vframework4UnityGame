using System;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// AssetRouter 四源路由集成测试（单 Runner）。
/// 与同步双 Runner 套系互斥运行；Collector 建议 sessionIdPrefix=RouterLoad_，expectedConcurrentRunners=1。
/// </summary>
public class MyRouterTest : AbLoadTestRunnerBase
{
    const int TotalCases = 7;
    const string ResourcesProbePath = "Resources/ResourceSystemDesignGuide";
    const string AbProbePath = "Icon/3";
    const string AbProbeAssetPath = "Assets/AssetBundle/Icon/3.png";

    protected override string LogSource => "MyRouterTest";
    protected override int CaseCount => TotalCases;

    IAssetHandle resourcesHandle;
    IAssetHandle abHandle;

    protected override IEnumerator RunCase(int caseId)
    {
        switch (caseId)
        {
            case 0: CaseCatalogueAndBuildMode(); break;
            case 1: CaseRouteResources(); break;
            case 2: CaseLoadResources(); break;
            case 3: CaseRouteAbundle(); break;
            case 4: CaseLoadAbundleSmoke(); break;
            case 5: CaseRouteEditorResources(); break;
            case 6: CaseRouteNetCdn(); break;
        }

        yield return null;
    }

    void CaseCatalogueAndBuildMode()
    {
        if (!BundleResLoader.Instance.EnsureReady())
        {
            LogFail("Catalogue", "EnsureReady failed");
            return;
        }

        AssetCatalog catalog = BundleResLoader.Instance.GetCatalogue()?.Catalog;
        string buildMode = catalog != null ? catalog.buildMode : "null";
        LogOk("Catalogue", "buildMode=" + buildMode + " root=" + BundleResLoader.GetDefaultRuntimeBundleRoot());
    }

    void CaseRouteResources()
    {
        var ctx = new AssetLoadContext
        {
            loadPath = ResourcesProbePath,
            assetPath = "Assets/Resources/ResourceSystemDesignGuide.md",
            bundleName = "ignored.bundle",
            assetType = typeof(TextAsset)
        };

        AssetSource source = AssetRouter.Instance.RouteAssetSource(in ctx);
        if (source == AssetSource.RESOURCES)
            LogOk("Route RESOURCES", ResourcesProbePath);
        else
            LogFail("Route RESOURCES", "got " + source);
    }

    void CaseLoadResources()
    {
        resourcesHandle?.Release();
        resourcesHandle = BundleResLoader.Instance.Load<TextAsset>(ResourcesProbePath);

        TextAsset asset = resourcesHandle?.GetAsset<TextAsset>();
        if (asset == null || string.IsNullOrEmpty(asset.text))
        {
            LogFail("Load RESOURCES", ResourcesProbePath);
            return;
        }

        resourcesHandle.Release();
        resourcesHandle = null;
        LogOk("Load RESOURCES", "bytes=" + asset.text.Length);
    }

    void CaseRouteAbundle()
    {
        if (!BundleResLoader.Instance.EnsureReady())
        {
            LogFail("Route ABUNDLE", "EnsureReady failed");
            return;
        }

        var ctx = new AssetLoadContext
        {
            loadPath = AbProbePath,
            assetPath = AbProbeAssetPath,
            bundleName = "icon.bundle",
            assetType = typeof(Sprite)
        };

        AssetSource source = AssetRouter.Instance.RouteAssetSource(in ctx);
        AssetCatalog catalog = BundleResLoader.Instance.GetCatalogue()?.Catalog;
        bool editorTest = catalog != null
            && string.Equals(catalog.buildMode, "EditorTest", StringComparison.OrdinalIgnoreCase);

#if UNITY_EDITOR
        if (editorTest)
        {
            if (source == AssetSource.EDITORRESOURCES)
                LogOk("Route ABUNDLE", "EditorTest catalogue → EDITORRESOURCES (expected)");
            else
                LogFail("Route ABUNDLE", "EditorTest expected EDITORRESOURCES, got " + source);
            return;
        }
#endif

        if (source == AssetSource.ABUNDLE)
            LogOk("Route ABUNDLE", AbProbePath);
        else
            LogFail("Route ABUNDLE", "got " + source);
    }

    void CaseLoadAbundleSmoke()
    {
        abHandle?.Release();
        abHandle = BundleResLoader.Instance.Load<Sprite>(AbProbePath);

        if (abHandle?.GetAsset<Sprite>() == null)
        {
            LogFail("Load ABUNDLE", AbProbePath);
            return;
        }

        abHandle.Release();
        abHandle = null;
        LogOk("Load ABUNDLE", AbProbePath);
    }

    void CaseRouteEditorResources()
    {
#if !UNITY_EDITOR
        LogSkip("Route EDITORRESOURCES", "Player build");
        return;
#endif

        AssetCatalog catalog = BundleResLoader.Instance.GetCatalogue()?.Catalog;
        if (catalog == null || !string.Equals(catalog.buildMode, "EditorTest", StringComparison.OrdinalIgnoreCase))
        {
            LogSkip("Route EDITORRESOURCES", "catalogue buildMode=" + (catalog?.buildMode ?? "null") + " (need EditorTest pack)");
            return;
        }

        var ctx = new AssetLoadContext
        {
            loadPath = AbProbePath,
            assetPath = AbProbeAssetPath,
            bundleName = "icon.bundle",
            assetType = typeof(Sprite)
        };

        AssetSource source = AssetRouter.Instance.RouteAssetSource(in ctx);
        if (source == AssetSource.EDITORRESOURCES)
            LogOk("Route EDITORRESOURCES", AbProbePath);
        else
            LogFail("Route EDITORRESOURCES", "got " + source);
    }

    void CaseRouteNetCdn()
    {
        AssetCatalog catalog = BundleResLoader.Instance.GetCatalogue()?.Catalog;
        if (catalog != null && string.Equals(catalog.buildMode, "EditorTest", StringComparison.OrdinalIgnoreCase))
        {
            LogSkip("Route NETCDN", "EditorTest disables NETCDN in Editor");
            return;
        }

        CatalogueReader reader = BundleResLoader.Instance.GetCatalogue();
        if (reader == null || !reader.IsLoaded)
        {
            LogFail("Route NETCDN", "runtime catalogue not loaded");
            return;
        }

        AssetRouter.Instance.Init(reader, new RouterTestUnavailableResolver());

        var ctx = new AssetLoadContext
        {
            loadPath = AbProbePath,
            assetPath = AbProbeAssetPath,
            bundleName = "icon.bundle",
            assetType = typeof(Sprite)
        };

        AssetSource source = AssetRouter.Instance.RouteAssetSource(in ctx);
        if (source == AssetSource.NETCDN)
            LogOk("Route NETCDN", "fake resolver → NETCDN");
        else
            LogFail("Route NETCDN", "got " + source);

        // 恢复与 BundleResLoader 一致的 Router（同 Play 内若再跑 AB 集成需依赖此步）
        string root = BundleResLoader.GetDefaultRuntimeBundleRoot();
        DefaultBundlePathResolver resolver = DefaultBundlePathResolver.Create(root);
        AssetRouter.Instance.Init(BundleResLoader.Instance.GetCatalogue(), resolver);
    }

    protected override void OnDestroy()
    {
        resourcesHandle?.Release();
        abHandle?.Release();
        base.OnDestroy();
    }

    sealed class RouterTestUnavailableResolver : IBundlePathResolver
    {
        public bool TryResolveLocalPath(string bundleName, out string localPath)
        {
            localPath = null;
            return false;
        }

        public bool IsLocalBundleAvailable(string bundleName) => false;
    }
}
