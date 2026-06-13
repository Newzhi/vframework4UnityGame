using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bundle 依赖拓扑与跨包 Load 集成测试（单 Runner，默认禁用）。
/// 需 DeviceDebug 打包后 catalogue.bundles[] 含拓扑序依赖。
/// </summary>
public class MyDependencyTest : AbLoadTestRunnerBase
{
    const int TotalCases = 3;
    const string UiLoadPath = "UI/TestUI";
    const string UiBundleName = "ui.bundle";

    protected override string LogSource => "MyDependencyTest";
    protected override int CaseCount => TotalCases;

    IAssetHandle uiHandle;

    protected override IEnumerator RunCase(int caseId)
    {
        switch (caseId)
        {
            case 0: CaseCatalogueBundlesPresent(); break;
            case 1: CaseDependencyTopologicalOrder(); break;
            case 2: CaseLoadCrossBundleUiPrefab(); break;
        }

        yield return null;
    }

    void CaseCatalogueBundlesPresent()
    {
        if (!BundleResLoader.Instance.EnsureReady())
        {
            LogFail("Catalogue", "EnsureReady failed");
            return;
        }

        AssetCatalog catalog = BundleResLoader.Instance.GetCatalogue()?.Catalog;
        if (catalog == null)
        {
            LogFail("Catalogue", "catalog null");
            return;
        }

        if (string.Equals(catalog.buildMode, "EditorTest", StringComparison.OrdinalIgnoreCase))
        {
            LogSkip("Catalogue bundles", "EditorTest catalogue has no manifest dependencies");
            return;
        }

        if (catalog.bundles == null || catalog.bundles.Length == 0)
        {
            LogFail("Catalogue bundles", "bundles[] empty — run DeviceDebug pack first");
            return;
        }

        LogOk("Catalogue bundles", "count=" + catalog.bundles.Length + " buildMode=" + catalog.buildMode);
    }

    void CaseDependencyTopologicalOrder()
    {
        CatalogueReader reader = BundleResLoader.Instance.GetCatalogue();
        AssetCatalog catalog = reader?.Catalog;
        if (catalog == null || catalog.bundles == null || catalog.bundles.Length == 0)
        {
            LogSkip("Dependency order", "no bundles[] in catalogue");
            return;
        }

        string[] deps = reader.GetBundleDependencies(UiBundleName);
        if (deps == null || deps.Length == 0)
        {
            LogSkip("Dependency order", UiBundleName + " has no dependencies in catalogue");
            return;
        }

        var graph = BuildInferredGraph(catalog.bundles);
        var closure = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string dep in deps)
        {
            string normalized = BundlePlatformPaths.NormalizeBundleName(dep);
            if (!string.IsNullOrEmpty(normalized))
                closure.Add(normalized);
        }

        if (!BundleDependencyTopology.TryTopologicalSort(closure, graph, out string[] sorted, out string cycleHint))
        {
            LogFail("Dependency order", "cycle near " + cycleHint);
            return;
        }

        if (!ArraysMatchOrder(deps, sorted))
        {
            LogFail("Dependency order", "catalogue order differs from topological order");
            return;
        }

        LogOk("Dependency order", UiBundleName + " deps=" + deps.Length + " topological OK");
    }

    void CaseLoadCrossBundleUiPrefab()
    {
        AssetCatalog catalog = BundleResLoader.Instance.GetCatalogue()?.Catalog;
        if (catalog != null && string.Equals(catalog.buildMode, "EditorTest", StringComparison.OrdinalIgnoreCase))
        {
            LogSkip("Load UI cross-bundle", "EditorTest uses EDITORRESOURCES path");
            return;
        }

        uiHandle?.Release();
        uiHandle = BundleResLoader.Instance.Load<GameObject>(UiLoadPath);
        GameObject prefab = uiHandle?.GetAsset<GameObject>();
        if (prefab == null)
        {
            LogFail("Load UI cross-bundle", UiLoadPath);
            return;
        }

        uiHandle.Release();
        uiHandle = null;
        LogOk("Load UI cross-bundle", UiLoadPath);
    }

    static Dictionary<string, List<string>> BuildInferredGraph(BundleCatalogInfo[] bundles)
    {
        var graph = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (BundleCatalogInfo info in bundles)
        {
            if (info == null || string.IsNullOrEmpty(info.bundleName))
                continue;

            string key = BundlePlatformPaths.NormalizeBundleName(info.bundleName);
            if (!graph.ContainsKey(key))
                graph[key] = new List<string>();

            if (info.dependencies == null)
                continue;

            foreach (string dep in info.dependencies)
            {
                string normalizedDep = BundlePlatformPaths.NormalizeBundleName(dep);
                if (string.IsNullOrEmpty(normalizedDep))
                    continue;

                if (!graph[key].Contains(normalizedDep))
                    graph[key].Add(normalizedDep);

                if (!graph.ContainsKey(normalizedDep))
                    graph[normalizedDep] = new List<string>();
            }
        }

        return graph;
    }

    static bool ArraysMatchOrder(string[] actual, string[] expected)
    {
        if (actual == null || expected == null)
            return actual == expected;

        if (actual.Length != expected.Length)
            return false;

        for (int i = 0; i < actual.Length; i++)
        {
            if (!string.Equals(
                BundlePlatformPaths.NormalizeBundleName(actual[i]),
                BundlePlatformPaths.NormalizeBundleName(expected[i]),
                StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    protected override void OnDestroy()
    {
        uiHandle?.Release();
        base.OnDestroy();
    }
}
