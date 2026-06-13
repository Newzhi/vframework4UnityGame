using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CatalogueReader
{
    public const string RuntimeCatalogueFileName = "AssetCatalog.json";
    const string DefaultResourceRoot = "Assets/AssetBundle";

    #region 变量定义

    AssetCatalog catalog;
    Dictionary<string, AssetCatalogEntry> entryMap = new Dictionary<string, AssetCatalogEntry>();
    Dictionary<string, AssetCatalogEntry> loadPathMap = new Dictionary<string, AssetCatalogEntry>();
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

        string json;
        try
        {
            json = StreamingAssetsIO.ReadAllText(cataloguePath);
        }
        catch (IOException ex)
        {
            Debug.LogError("Catalogue read failed: " + cataloguePath + " | " + ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError("Catalogue read failed: " + cataloguePath + " | " + ex.Message);
            return false;
        }
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

        string cataloguePath = StreamingAssetsIO.CombinePath(bundleRoot, "Catalogue", RuntimeCatalogueFileName);
        return LoadFromFile(cataloguePath);
    }

#if UNITY_EDITOR
    public bool LoadFromProjectCatalogue(string relativeAssetPath = null)
    {
        if (string.IsNullOrEmpty(relativeAssetPath))
            relativeAssetPath = BundlePlatformPaths.ProjectCatalogueRelativePath;

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string fullPath = Path.GetFullPath(Path.Combine(projectRoot, relativeAssetPath.Replace("/", Path.DirectorySeparatorChar.ToString())));
        return LoadFromFile(fullPath);
    }
#endif

    #endregion

    #region 查询

    /// <summary>Unity 工程完整路径，如 Assets/AssetBundle/Atlas/Role/Hog.png</summary>
    public bool TryGetEntry(string assetPath, out AssetCatalogEntry entry)
    {
        entry = null;
        if (string.IsNullOrEmpty(assetPath))
            return false;

        return entryMap.TryGetValue(NormalizePath(assetPath), out entry);
    }

    /// <summary>业务简路径，相对 resourceRoot、无扩展名，如 Atlas/Role/Hog_Attack_000</summary>
    public bool TryGetEntryByLoadPath(string loadPath, out AssetCatalogEntry entry)
    {
        entry = null;
        if (string.IsNullOrEmpty(loadPath))
            return false;

        return loadPathMap.TryGetValue(NormalizeLoadPath(loadPath), out entry);
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
        loadPathMap.Clear();
        dependencyMap.Clear();
    }

    void BuildLookupTables()
    {
        entryMap.Clear();
        loadPathMap.Clear();
        dependencyMap.Clear();

        string resourceRoot = string.IsNullOrEmpty(catalog.resourceRoot)
            ? DefaultResourceRoot
            : catalog.resourceRoot;

        if (catalog.entries != null)
        {
            foreach (AssetCatalogEntry entry in catalog.entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.assetPath))
                    continue;

                string assetKey = NormalizePath(entry.assetPath);
                entryMap[assetKey] = entry;

                string loadKey = ToLoadPath(entry.assetPath, resourceRoot);
                if (!string.IsNullOrEmpty(loadKey))
                    loadPathMap[loadKey] = entry;
            }
        }

        if (catalog.bundles != null)
        {
            foreach (BundleCatalogInfo info in catalog.bundles)
            {
                if (info == null || string.IsNullOrEmpty(info.bundleName))
                    continue;

                string[] deps = info.dependencies ?? new string[0];
                if (deps.Length > 1)
                {
                    deps = BundleDependencyTopology.SortUsingCatalogAllDeps(
                        catalog.bundles,
                        info.bundleName,
                        deps);
                }

                dependencyMap[info.bundleName] = deps;
            }
        }
    }

    public static string NormalizePath(string path)
    {
        return string.IsNullOrEmpty(path) ? path : path.Replace("\\", "/");
    }

    public static string NormalizeLoadPath(string loadPath)
    {
        if (string.IsNullOrEmpty(loadPath))
            return loadPath;

        string normalized = NormalizePath(loadPath).Trim('/');
        int lastSlash = normalized.LastIndexOf('/');
        int lastDot = normalized.LastIndexOf('.');
        if (lastDot > lastSlash)
            normalized = normalized.Substring(0, lastDot);

        return normalized;
    }

    public static string ToLoadPath(string assetPath, string resourceRoot)
    {
        if (string.IsNullOrEmpty(assetPath))
            return null;

        string normalized = NormalizePath(assetPath);
        string root = NormalizePath(resourceRoot).TrimEnd('/');

        if (!string.IsNullOrEmpty(root))
        {
            if (normalized.StartsWith(root + "/"))
                normalized = normalized.Substring(root.Length + 1);
            else if (normalized == root)
                normalized = Path.GetFileNameWithoutExtension(normalized);
        }

        return NormalizeLoadPath(normalized);
    }

    #endregion
}
