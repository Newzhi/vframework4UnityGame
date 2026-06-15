using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class BundleManager
{
    #region 变量定义

    private static string bundleRootPath;
    private static CatalogueReader catalogue;
    private static IBundlePathResolver pathResolver;
    private static Dictionary<string, BundleEntry> loadedBundles = new Dictionary<string, BundleEntry>();

    private class BundleEntry
    {
        public AssetBundle Bundle;
        public int Ref;
    }

    #endregion

    #region 初始化

    public static void Init(string rootPath, CatalogueReader reader = null)
    {
        if (loadedBundles.Count > 0)
        {
            foreach (BundleEntry entry in loadedBundles.Values)
            {
                if (entry?.Bundle != null)
                    entry.Bundle.Unload(true);
            }
        }

        bundleRootPath = rootPath;
        catalogue = reader;
        loadedBundles.Clear();
    }

    public static void SetCatalogue(CatalogueReader reader)
    {
        catalogue = reader;
    }

    public static void SetPathResolver(IBundlePathResolver resolver)
    {
        pathResolver = resolver;
    }

    #endregion

    #region 加载/卸载

    /// <summary>
    /// 按清单顺序 Acquire 依赖包再 Acquire 主包。
    /// catalogue.bundles[].dependencies 已为拓扑序（叶→根），与 Unity Manifest 一致。
    /// </summary>
    public static AssetBundle AcquireBundleWithDependencies(string bundleName, List<string> acquiredBundles = null)
    {
        bundleName = BundlePlatformPaths.NormalizeBundleName(bundleName);

        string[] deps = null;
        if (catalogue != null && catalogue.IsLoaded)
        {
            deps = catalogue.GetBundleDependencies(bundleName);
#if DEVELOPMENT_BUILD
            ValidateDependencyOrder(bundleName, deps);
#endif
        }

        AssetRefTraceLogger.TraceBundleLoadScopeBegin(bundleName, deps);

        if (deps != null)
        {
            foreach (string dep in deps)
            {
                if (string.IsNullOrEmpty(dep))
                    continue;

                AssetBundle depBundle = AcquireBundle(dep, "Dep", bundleName);
                if (depBundle != null)
                    acquiredBundles?.Add(dep);
            }
        }

        AssetBundle bundle = AcquireBundle(bundleName, "Main", bundleName);
        if (bundle != null)
            acquiredBundles?.Add(bundleName);

        return bundle;
    }

    public static AssetBundle AcquireBundle(string bundleName)
    {
        return AcquireBundle(bundleName, null, null);
    }

    static AssetBundle AcquireBundle(string bundleName, string role, string mainBundle)
    {
        bundleName = BundlePlatformPaths.NormalizeBundleName(bundleName);

        if (loadedBundles.TryGetValue(bundleName, out BundleEntry entry))
        {
            entry.Ref++;
            AssetRefTraceLogger.TraceBundle(bundleName, entry.Ref, +1, "AcquireBundle", role, mainBundle);
            return entry.Bundle;
        }

        string path = ResolveBundleFilePath(bundleName);
        AssetBundle bundle = AssetBundle.LoadFromFile(path);
        if (bundle == null)
        {
            Debug.LogError("Bundle load failed: " + path);
            return null;
        }

        loadedBundles[bundleName] = new BundleEntry { Bundle = bundle, Ref = 1 };
        AssetRefTraceLogger.TraceBundle(bundleName, 1, +1, "AcquireBundle(new)", role, mainBundle);
        return bundle;
    }

    /// <summary>
    /// 释放包
    /// </summary>
    /// <param name="bundleName"></param>
    public static void ReleaseBundle(string bundleName)
    {
        bundleName = BundlePlatformPaths.NormalizeBundleName(bundleName);

        if (!loadedBundles.TryGetValue(bundleName, out BundleEntry entry))
        {
            Debug.LogError("ReleaseBundle failed, bundle not loaded: " + bundleName);
            return;
        }

        if (entry.Ref <= 0)
        {
            Debug.LogError("ReleaseBundle failed, ref already 0: " + bundleName);
            return;
        }

        entry.Ref--;
        AssetRefTraceLogger.TraceBundle(bundleName, entry.Ref, -1, "ReleaseBundle", "Release", null);
        if (entry.Ref <= 0)
        {
            entry.Bundle.Unload(true);
            loadedBundles.Remove(bundleName);
        }
    }

    /// <summary>
    /// 关闭游戏之前或者调试的方法
    /// </summary>
    public static void UnloadAll()
    {
        int count = loadedBundles.Count;
        AssetRefTraceLogger.TraceBundleUnloadAll(count);

        foreach (BundleEntry entry in loadedBundles.Values)
            entry.Bundle.Unload(true);

        loadedBundles.Clear();
    }

    #endregion

    #region 辅助函数

    static string ResolveBundleFilePath(string bundleName)
    {
        if (pathResolver != null && pathResolver.TryResolveLocalPath(bundleName, out string resolvedPath))
            return resolvedPath;

        string root = string.IsNullOrEmpty(bundleRootPath)
            ? Application.streamingAssetsPath
            : bundleRootPath;

        return ResolveBundleFilePath(root, bundleName);
    }

    static string ResolveBundleFilePath(string root, string bundleName)
    {
        string path = StreamingAssetsIO.CombinePath(root, bundleName);
        if (StreamingAssetsIO.IsNonFileProtocolPath(root))
            return path;

        if (File.Exists(path))
            return path;

        if (!Directory.Exists(root))
            return path;

        string fileName = Path.GetFileName(bundleName);
        foreach (string file in Directory.GetFiles(root, "*.bundle"))
        {
            if (string.Equals(Path.GetFileName(file), fileName, System.StringComparison.OrdinalIgnoreCase))
                return file;
        }

        return path;
    }

#if DEVELOPMENT_BUILD
    static void ValidateDependencyOrder(string bundleName, string[] deps)
    {
        if (deps == null || deps.Length <= 1 || catalogue?.Catalog?.bundles == null)
            return;

        var graph = new Dictionary<string, List<string>>(System.StringComparer.OrdinalIgnoreCase);
        foreach (BundleCatalogInfo info in catalogue.Catalog.bundles)
        {
            if (info == null || string.IsNullOrEmpty(info.bundleName))
                continue;

            string key = BundlePlatformPaths.NormalizeBundleName(info.bundleName);
            var list = new List<string>();
            if (info.dependencies != null)
            {
                foreach (string dep in info.dependencies)
                {
                    string normalizedDep = BundlePlatformPaths.NormalizeBundleName(dep);
                    if (!string.IsNullOrEmpty(normalizedDep) && !list.Contains(normalizedDep))
                        list.Add(normalizedDep);
                }
            }

            graph[key] = list;
        }

        var closure = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        foreach (string dep in deps)
        {
            string normalized = BundlePlatformPaths.NormalizeBundleName(dep);
            if (!string.IsNullOrEmpty(normalized))
                closure.Add(normalized);
        }

        if (!BundleDependencyTopology.TryTopologicalSort(closure, graph, out _, out string cycleHint))
            Debug.LogWarning("Bundle dependency order may be invalid for " + bundleName + ": cycle near " + cycleHint);
    }
#endif

    #endregion
}
