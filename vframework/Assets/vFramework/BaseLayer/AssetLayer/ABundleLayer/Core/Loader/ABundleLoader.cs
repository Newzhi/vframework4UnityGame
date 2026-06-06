using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace vFramework.BaseLayer.AssetLayer.ABundleLayer
{
    /// <summary>
    /// ④ 加载器：对外薄门面，委托 ③ 抽象资源层处理包与引用计数。
    /// </summary>
    public class ABundleLoader : IABundleLoader
    {
        #region 字段

        readonly ABundleResourceSystem _resources = new();
        readonly Dictionary<string, ABundleLoadTicket> _locationTickets = new();
        readonly Dictionary<string, ABundleLoadTicket> _bundleTickets = new();

        string _manifestName = "AssetBundles";
        string _catalogName = "AssetCatalog.json";
        ABundleLoadMode _loadMode = ABundleLoadMode.RuntimeBundle;

        #endregion

        #region 属性

        public bool IsInitialized { get; private set; }
        public ABundleLoadMode LoadMode => _loadMode;
        public AssetCatalog Catalog => _resources.Catalog;
        public IABundleResourceSystem Resources => _resources;

        #endregion

        #region 初始化

        public void Initialize(string catalogRelativePath = null)
        {
            var sub = string.IsNullOrEmpty(catalogRelativePath) ? "AssetBundles" : catalogRelativePath;
            var rootPath = Path.Combine(Application.streamingAssetsPath, sub);
            Bootstrap(rootPath, loadManifest: _loadMode == ABundleLoadMode.RuntimeBundle);
        }

        public void InitializeFromRules(ABundleBuildRules rules, ABundleLoadMode? overrideMode = null)
        {
            if (rules == null)
            {
                Debug.LogError("[ABundle] InitializeFromRules: rules 为空");
                return;
            }

            _loadMode = overrideMode ?? rules.LoadMode;
            _catalogName = rules.CatalogFileName;
            _manifestName = rules.PlatformManifestFileName;
            var rootPath = ABundlePathUtility.ToFullPath(ABundlePathUtility.GetPlatformOutputAssetPath(rules));
            Bootstrap(rootPath, loadManifest: _loadMode == ABundleLoadMode.RuntimeBundle);
        }

        public void InitializeWithRootPath(
            string bundleRootPath,
            string catalogFileName,
            string manifestFileName,
            ABundleLoadMode loadMode = ABundleLoadMode.RuntimeBundle)
        {
            _loadMode = loadMode;
            _catalogName = catalogFileName;
            _manifestName = manifestFileName;
            Bootstrap(bundleRootPath, loadManifest: loadMode == ABundleLoadMode.RuntimeBundle);
        }

        void Bootstrap(string rootPath, bool loadManifest)
        {
            _resources.Initialize(rootPath, _catalogName, _manifestName, loadManifest);
            IsInitialized = true;
            Debug.Log($"[ABundle] Loader 初始化，模式={_loadMode}，根路径: {rootPath}");
        }

        public void Shutdown()
        {
            ReleaseAllTracked();
            _resources.Shutdown();
            IsInitialized = false;
        }

        #endregion

        #region 加载

        public T LoadAsset<T>(string location) where T : UnityEngine.Object
        {
            if (!_resources.TryResolveLocation(location, out var entry))
            {
                Debug.LogError($"[ABundle] Catalog 中无 location: {location}");
                return null;
            }

#if UNITY_EDITOR
            if (_loadMode == ABundleLoadMode.EditorSimulation)
            {
                return LoadFromEditor<T>(entry);
            }
#endif
            if (_locationTickets.ContainsKey(location))
            {
                Debug.LogWarning($"[ABundle] location 已加载，请先 ReleaseAsset: {location}");
            }

            var ticket = _resources.AcquireBundle(entry.BundleName);
            if (!ticket.IsValid)
            {
                return null;
            }

            _locationTickets[location] = ticket;
            var bundle = _resources.GetBundle(ticket);
            return bundle == null ? null : bundle.LoadAsset<T>(entry.AssetName);
        }

#if UNITY_EDITOR
        static T LoadFromEditor<T>(AssetLocationEntry entry) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(entry.SourceAssetPath))
            {
                return null;
            }

            var asset = AssetDatabase.LoadAssetAtPath<T>(entry.SourceAssetPath);
            if (asset == null)
            {
                Debug.LogWarning($"[ABundle] EditorSimulation 无法加载: {entry.Location}");
            }

            return asset;
        }
#endif

        public void LoadAssetAsync<T>(string location, Action<T> onComplete) where T : UnityEngine.Object
        {
            onComplete?.Invoke(LoadAsset<T>(location));
        }

        public void LoadBundle(string bundleName)
        {
            if (_loadMode == ABundleLoadMode.EditorSimulation)
            {
                return;
            }

            if (_bundleTickets.ContainsKey(bundleName))
            {
                Debug.LogWarning($"[ABundle] 包已加载，请先 ReleaseBundle: {bundleName}");
                return;
            }

            var ticket = _resources.AcquireBundle(bundleName);
            if (!ticket.IsValid)
            {
                Debug.LogError($"[ABundle] LoadBundle 失败: {bundleName}");
                return;
            }

            _bundleTickets[bundleName] = ticket;
        }

        #endregion

        #region 卸载

        public void ReleaseAsset(string location)
        {
            if (!_locationTickets.TryGetValue(location, out var ticket))
            {
                return;
            }

            _resources.ReleaseTicket(ticket);
            _locationTickets.Remove(location);
        }

        public void ReleaseBundle(string bundleName)
        {
            if (_loadMode == ABundleLoadMode.EditorSimulation)
            {
                return;
            }

            if (!_bundleTickets.TryGetValue(bundleName, out var ticket))
            {
                return;
            }

            _resources.ReleaseTicket(ticket);
            _bundleTickets.Remove(bundleName);
        }

        public void UnloadAll(bool unloadAllLoadedObjects = false)
        {
            ReleaseAllTracked();
            _resources.UnloadAll(unloadAllLoadedObjects);
        }

        void ReleaseAllTracked()
        {
            foreach (var ticket in _locationTickets.Values)
            {
                _resources.ReleaseTicket(ticket);
            }

            _locationTickets.Clear();

            foreach (var ticket in _bundleTickets.Values)
            {
                _resources.ReleaseTicket(ticket);
            }

            _bundleTickets.Clear();
        }

        #endregion

        #region 查询

        public bool ContainsLocation(string location) => _resources.TryResolveLocation(location, out _);
        public int GetBundleRefCount(string bundleName) => _resources.GetRefCount(bundleName);
        public string[] GetLoadedBundleNames() => _resources.GetLoadedBundleNames();

        #endregion
    }
}
