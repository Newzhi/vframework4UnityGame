using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace vFramework.BaseLayer.AssetLayer
{
    /// <summary>
    /// 资源管理器：同一 location 共享底层加载，LoadAsync 返回的句柄 Release 时递减引用，归零后释放后端句柄。
    /// </summary>
    public sealed class ResMgr : IResMgr
    {
        private static ResMgr _instance;

        private readonly IResLoader _loader;
        private readonly Dictionary<string, AssetCacheEntry> _cache = new();
        private readonly object _lock = new();

        public static ResMgr Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new InvalidOperationException("ResMgr is not initialized. Call ResMgr.Initialize() first.");
                }

                return _instance;
            }
        }

        public static bool IsInitialized => _instance != null;

        public ResMgr(IResLoader loader)
        {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        }

        /// <summary>
        /// 初始化全局 ResMgr。未传入 loader 时使用 Composite（Addressables → Resources）。
        /// </summary>
        public static void Initialize(IResLoader loader = null)
        {
            if (_instance != null)
            {
                return;
            }

            loader ??= CreateDefaultLoader();
            _instance = new ResMgr(loader);
        }

        public static void Shutdown()
        {
            if (_instance == null)
            {
                return;
            }

            _instance.ClearCache();
            _instance = null;
        }

        public static IResLoader CreateDefaultLoader()
        {
            return new CompositeResLoader(
                new AddressablesResLoader(),
                new ResourcesResLoader());
        }

        public async Task<IAssetHandle> LoadAsync<T>(string location, CancellationToken cancellationToken = default)
            where T : UnityEngine.Object
        {
            if (string.IsNullOrWhiteSpace(location))
            {
                throw new ArgumentException("Location is empty.", nameof(location));
            }

            Task<AssetCacheEntry> loadTask;

            lock (_lock)
            {
                if (!_cache.TryGetValue(location, out var entry))
                {
                    entry = new AssetCacheEntry(location);
                    loadTask = entry.LoadingTask = LoadInternalAsync<T>(entry, cancellationToken);
                    _cache[location] = entry;
                }
                else if (entry.LoadingTask != null)
                {
                    loadTask = entry.LoadingTask;
                }
                else if (entry.LoadError != null)
                {
                    throw new InvalidOperationException(
                        $"Previous load failed for '{location}'.", entry.LoadError);
                }
                else
                {
                    entry.RefCount++;
                    return new RefCountedAssetHandle(entry, this);
                }
            }

            var loaded = await loadTask;
            if (loaded.LoadError != null)
            {
                throw new InvalidOperationException($"Load failed for '{location}'.", loaded.LoadError);
            }

            lock (_lock)
            {
                loaded.RefCount++;
                return new RefCountedAssetHandle(loaded, this);
            }
        }

        public async Task<GameObject> InstantiateAsync(
            string location,
            Transform parent = null,
            CancellationToken cancellationToken = default)
        {
            var handle = await LoadAsync<GameObject>(location, cancellationToken);
            try
            {
                var prefab = handle.GetAsset<GameObject>();
                if (prefab == null)
                {
                    throw new InvalidOperationException($"Asset is not GameObject: {location}");
                }

                return parent == null
                    ? UnityEngine.Object.Instantiate(prefab)
                    : UnityEngine.Object.Instantiate(prefab, parent);
            }
            finally
            {
                handle.Release();
            }
        }

        public int GetRefCount(string location)
        {
            lock (_lock)
            {
                return _cache.TryGetValue(location, out var entry) ? entry.RefCount : 0;
            }
        }

        internal void ReleaseEntry(AssetCacheEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            lock (_lock)
            {
                if (!_cache.TryGetValue(entry.Location, out var cached) || !ReferenceEquals(cached, entry))
                {
                    return;
                }

                entry.RefCount--;
                if (entry.RefCount > 0)
                {
                    return;
                }

                entry.BackendHandle?.ReleaseBackend();
                entry.BackendHandle = null;
                entry.Asset = null;
                _cache.Remove(entry.Location);
            }
        }

        private async Task<AssetCacheEntry> LoadInternalAsync<T>(AssetCacheEntry entry, CancellationToken cancellationToken)
            where T : UnityEngine.Object
        {
            try
            {
                var backendHandle = await _loader.LoadAsync<T>(entry.Location, cancellationToken);
                entry.Asset = backendHandle.Asset;
                entry.BackendHandle = backendHandle;
                entry.LoadError = null;
                return entry;
            }
            catch (Exception ex)
            {
                entry.LoadError = ex;
                lock (_lock)
                {
                    if (_cache.TryGetValue(entry.Location, out var cached) && ReferenceEquals(cached, entry))
                    {
                        _cache.Remove(entry.Location);
                    }
                }

                return entry;
            }
            finally
            {
                lock (_lock)
                {
                    entry.LoadingTask = null;
                }
            }
        }

        private void ClearCache()
        {
            lock (_lock)
            {
                foreach (var entry in _cache.Values)
                {
                    entry.BackendHandle?.ReleaseBackend();
                    entry.BackendHandle = null;
                    entry.Asset = null;
                    entry.RefCount = 0;
                }

                _cache.Clear();
            }

            Resources.UnloadUnusedAssets();
        }
    }
}
