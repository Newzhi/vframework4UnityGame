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

    public static AssetBundle AcquireBundleWithDependencies(string bundleName, List<string> acquiredBundles = null)
    {
        bundleName = BundlePlatformPaths.NormalizeBundleName(bundleName);

        if (catalogue != null && catalogue.IsLoaded)
        {
            foreach (string dep in catalogue.GetBundleDependencies(bundleName))
            {
                if (string.IsNullOrEmpty(dep))
                    continue;

                AssetBundle depBundle = AcquireBundle(dep);
                if (depBundle != null)
                    acquiredBundles?.Add(dep);
            }
        }

        AssetBundle bundle = AcquireBundle(bundleName);
        if (bundle != null)
            acquiredBundles?.Add(bundleName);

        return bundle;
    }

    public static AssetBundle AcquireBundle(string bundleName)
    {
        bundleName = BundlePlatformPaths.NormalizeBundleName(bundleName);

        if (loadedBundles.TryGetValue(bundleName, out BundleEntry entry))
        {
            entry.Ref++;
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
        return bundle;
    }

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
        if (entry.Ref <= 0)
        {
            entry.Bundle.Unload(true);
            loadedBundles.Remove(bundleName);
        }
    }

    public static void UnloadAll()
    {
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

    #endregion
}
