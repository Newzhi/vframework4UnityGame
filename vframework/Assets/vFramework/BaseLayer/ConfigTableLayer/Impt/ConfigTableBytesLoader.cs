using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BaseLayer.ConfigTable
{
    /// <summary>
    /// 阶段一：从 <see cref="ConfigTablePaths.BundleName"/> 预加载全部表 bytes，不做 XTBL 解析。
    /// </summary>
    public static class ConfigTableBytesLoader
    {
        /// <summary>
        /// 加载全部配置表 bytes。优先走 AssetBundle + catalogue；Editor 下 catalogue 无条目时回退直读磁盘。
        /// </summary>
        public static IReadOnlyDictionary<string, byte[]> Load()
        {
            var cache = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

            if (TryLoadFromBundles(cache) && cache.Count > 0)
                return cache;

#if UNITY_EDITOR
            if (TryLoadFromProjectFolder(cache) && cache.Count > 0)
                return cache;
#endif

            if (cache.Count == 0)
                Debug.LogError("[ConfigTable] No table bytes loaded from bundle or project folder.");

            return cache;
        }

        static bool TryLoadFromBundles(Dictionary<string, byte[]> cache)
        {
            if (!BundleResLoader.Instance.EnsureReady())
            {
                Debug.LogError("[ConfigTable] BundleResLoader not ready.");
                return false;
            }

            BundleResLoader.Instance.PreLoadBundles(new[] { ConfigTablePaths.BundleName });

            CatalogueReader catalogueReader = BundleResLoader.Instance.GetCatalogue();
            AssetCatalog catalog = catalogueReader.Catalog;
            if (catalog?.entries == null || catalog.entries.Length == 0)
                return false;

            string targetBundle = BundlePlatformPaths.NormalizeBundleName(ConfigTablePaths.BundleName);
            string resourceRoot = catalog.resourceRoot;

            foreach (AssetCatalogEntry entry in catalog.entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.bundleName))
                    continue;

                if (!string.Equals(
                        BundlePlatformPaths.NormalizeBundleName(entry.bundleName),
                        targetBundle,
                        StringComparison.OrdinalIgnoreCase))
                    continue;

                string loadPath = CatalogueReader.ToLoadPath(entry.assetPath, resourceRoot);
                if (string.IsNullOrEmpty(loadPath))
                    continue;

                IAssetHandle handle = BundleResLoader.Instance.Load<TextAsset>(loadPath);
                TextAsset textAsset = handle?.GetAsset<TextAsset>();
                if (textAsset == null || textAsset.bytes == null || textAsset.bytes.Length == 0)
                {
                    Debug.LogError("[ConfigTable] Failed to load TextAsset: " + loadPath);
                    continue;
                }

                string key = string.IsNullOrEmpty(entry.assetName)
                    ? Path.GetFileNameWithoutExtension(entry.assetPath)
                    : entry.assetName;
                cache[key] = textAsset.bytes;
            }

            return true;
        }

#if UNITY_EDITOR
        static bool TryLoadFromProjectFolder(Dictionary<string, byte[]> cache)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string folder = Path.Combine(
                projectRoot,
                ConfigTablePaths.AssetFolder.Replace('/', Path.DirectorySeparatorChar));

            if (!Directory.Exists(folder))
                return false;

            foreach (string filePath in Directory.EnumerateFiles(folder, "*.bytes"))
            {
                string tableName = Path.GetFileNameWithoutExtension(filePath);
                cache[tableName] = File.ReadAllBytes(filePath);
            }

            if (cache.Count > 0)
                Debug.Log("[ConfigTable] Loaded " + cache.Count + " table(s) from project folder (Editor fallback).");

            return cache.Count > 0;
        }
#endif
    }
}
