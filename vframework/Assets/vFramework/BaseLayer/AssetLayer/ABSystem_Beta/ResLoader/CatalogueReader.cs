using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CatalogueReader
{
    public const string RuntimeCatalogueFileName = "AssetCatalog.json";

    #region 变量定义

    AssetCatalog catalog;
    Dictionary<string, AssetCatalogEntry> entryMap = new Dictionary<string, AssetCatalogEntry>();
    Dictionary<string, string[]> dependencyMap = new Dictionary<string, string[]>();

    #endregion

    #region 属性

    public bool IsLoaded => catalog != null;

    public AssetCatalog Catalog => catalog;

    #endregion

    #region 加载

    public bool LoadFromFile(string cataloguePath)
    {
        Clear();

        if (string.IsNullOrEmpty(cataloguePath))
        {
            Debug.LogError("Catalogue path is empty");
            return false;
        }

        if (!File.Exists(cataloguePath))
        {
            Debug.LogError("Catalogue file not found: " + cataloguePath);
            return false;
        }

        string json = File.ReadAllText(cataloguePath);
        catalog = JsonUtility.FromJson<AssetCatalog>(json);
        if (catalog == null)
        {
            Debug.LogError("Catalogue parse failed: " + cataloguePath);
            return false;
        }

        BuildLookupTables();
        return true;
    }

    public bool LoadFromBundleRoot(string bundleRoot)
    {
        if (string.IsNullOrEmpty(bundleRoot))
            bundleRoot = Application.streamingAssetsPath;

        string cataloguePath = Path.Combine(bundleRoot, "Catalogue", RuntimeCatalogueFileName);
        return LoadFromFile(cataloguePath);
    }

    #endregion

    #region 查询

    public bool TryGetEntry(string assetPath, out AssetCatalogEntry entry)
    {
        entry = null;
        if (string.IsNullOrEmpty(assetPath))
            return false;

        return entryMap.TryGetValue(NormalizePath(assetPath), out entry);
    }

    public string[] GetBundleDependencies(string bundleName)
    {
        if (string.IsNullOrEmpty(bundleName))
            return new string[0];

        if (dependencyMap.TryGetValue(bundleName, out string[] deps))
            return deps ?? new string[0];

        return new string[0];
    }

    #endregion

    #region 辅助函数

    void Clear()
    {
        catalog = null;
        entryMap.Clear();
        dependencyMap.Clear();
    }

    void BuildLookupTables()
    {
        entryMap.Clear();
        dependencyMap.Clear();

        if (catalog.entries != null)
        {
            foreach (AssetCatalogEntry entry in catalog.entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.assetPath))
                    continue;

                string key = NormalizePath(entry.assetPath);
                entryMap[key] = entry;
            }
        }

        if (catalog.bundles != null)
        {
            foreach (BundleCatalogInfo info in catalog.bundles)
            {
                if (info == null || string.IsNullOrEmpty(info.bundleName))
                    continue;

                dependencyMap[info.bundleName] = info.dependencies ?? new string[0];
            }
        }
    }

    public static string NormalizePath(string path)
    {
        return string.IsNullOrEmpty(path) ? path : path.Replace("\\", "/");
    }

    #endregion
}
