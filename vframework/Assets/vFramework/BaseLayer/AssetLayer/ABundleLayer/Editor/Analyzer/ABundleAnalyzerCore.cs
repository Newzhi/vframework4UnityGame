using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace vFramework.BaseLayer.AssetLayer.ABundleLayer.Editor
{
    /// <summary>
    /// 分析已打包 Manifest / Catalog：依赖、反向依赖、Location 索引。
    /// </summary>
    public class ABundleAnalyzer
    {
        #region 状态

        readonly Dictionary<string, string[]> _dependencies = new(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, List<string>> _dependents = new(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, long> _sizes = new(StringComparer.OrdinalIgnoreCase);

        AssetCatalog _catalog;
        string _platformOutputPath;

        public bool IsLoaded { get; private set; }
        public string PlatformOutputPath => _platformOutputPath;
        public AssetCatalog Catalog => _catalog;
        public IReadOnlyCollection<string> BundleNames => _dependencies.Keys;

        #endregion

        #region 加载

        public bool LoadFromRules(ABundleBuildRules rules) =>
            LoadFromPlatformOutput(
                ABundlePathUtility.GetPlatformOutputAssetPath(rules),
                rules.PlatformManifestFileName,
                rules.CatalogFileName);

        public bool LoadFromPlatformOutput(
            string platformOutputAssetPath,
            string manifestFileName = "AssetBundles",
            string catalogFileName = "AssetCatalog.json")
        {
            Clear();
            _platformOutputPath = ABundleRulesXmlIO.NormalizeAssetPath(platformOutputAssetPath);
            var fullPath = ABundlePathUtility.ToFullPath(_platformOutputPath);

            if (!Directory.Exists(fullPath))
            {
                Debug.LogWarning($"[ABundleAnalyzer] 目录不存在: {_platformOutputPath}");
                return false;
            }

            LoadManifest(fullPath, manifestFileName);
            LoadCatalog(fullPath, catalogFileName);
            BuildReverseDependencies();
            IsLoaded = _dependencies.Count > 0;
            return IsLoaded;
        }

        void LoadManifest(string fullPath, string manifestFileName)
        {
            var manifestPath = Path.Combine(fullPath, manifestFileName);
            if (!File.Exists(manifestPath))
            {
                return;
            }

            var manifestBundle = AssetBundle.LoadFromFile(manifestPath);
            if (manifestBundle == null)
            {
                return;
            }

            var manifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            manifestBundle.Unload(true);
            if (manifest == null)
            {
                return;
            }

            foreach (var bundleName in manifest.GetAllAssetBundles())
            {
                _dependencies[bundleName] = manifest.GetAllDependencies(bundleName);
                var filePath = Path.Combine(fullPath, bundleName);
                _sizes[bundleName] = File.Exists(filePath) ? new FileInfo(filePath).Length : 0;
            }
        }

        void LoadCatalog(string fullPath, string catalogFileName)
        {
            var catalogPath = Path.Combine(fullPath, catalogFileName);
            if (!File.Exists(catalogPath))
            {
                return;
            }

            _catalog = JsonUtility.FromJson<AssetCatalog>(File.ReadAllText(catalogPath));
            _catalog?.BuildRuntimeIndex();
        }

        void BuildReverseDependencies()
        {
            foreach (var pair in _dependencies)
            {
                foreach (var dep in pair.Value)
                {
                    if (!_dependents.TryGetValue(dep, out var list))
                    {
                        list = new List<string>();
                        _dependents[dep] = list;
                    }

                    if (!list.Contains(pair.Key))
                    {
                        list.Add(pair.Key);
                    }
                }
            }
        }

        public void Clear()
        {
            _dependencies.Clear();
            _dependents.Clear();
            _sizes.Clear();
            _catalog = null;
            _platformOutputPath = null;
            IsLoaded = false;
        }

        #endregion

        #region 查询

        public string[] GetDependencies(string bundleName) =>
            _dependencies.TryGetValue(bundleName, out var deps) ? deps : Array.Empty<string>();

        public IReadOnlyList<string> GetDependents(string bundleName) =>
            _dependents.TryGetValue(bundleName, out var list) ? list : Array.Empty<string>();

        public long GetBundleSize(string bundleName) =>
            _sizes.TryGetValue(bundleName, out var size) ? size : 0;

        public string[] GetLoadOrder(string bundleName)
        {
            if (!_dependencies.ContainsKey(bundleName))
            {
                return Array.Empty<string>();
            }

            var order = new List<string>();
            var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Visit(bundleName, order, visiting);
            return order.ToArray();
        }

        void Visit(string bundleName, List<string> order, HashSet<string> visiting)
        {
            if (visiting.Contains(bundleName))
            {
                return;
            }

            visiting.Add(bundleName);
            if (_dependencies.TryGetValue(bundleName, out var deps))
            {
                foreach (var dep in deps)
                {
                    Visit(dep, order, visiting);
                }
            }

            visiting.Remove(bundleName);
            if (!order.Contains(bundleName))
            {
                order.Add(bundleName);
            }
        }

        public bool TryFindLocation(string location, out AssetLocationEntry entry)
        {
            entry = null;
            return _catalog != null &&
                   !string.IsNullOrEmpty(location) &&
                   _catalog.TryGetLocation(location, out entry);
        }

        public IReadOnlyList<AssetLocationEntry> FindLocationsByBundle(string bundleName)
        {
            var result = new List<AssetLocationEntry>();
            if (_catalog?.Locations == null || string.IsNullOrEmpty(bundleName))
            {
                return result;
            }

            foreach (var loc in _catalog.Locations)
            {
                if (string.Equals(loc.BundleName, bundleName, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(loc);
                }
            }

            return result;
        }

        #endregion
    }
}
