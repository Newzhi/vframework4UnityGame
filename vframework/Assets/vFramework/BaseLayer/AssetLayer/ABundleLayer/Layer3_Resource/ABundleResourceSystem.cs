// ABundleResourceSystem.cs — ③ 抽象资源层（Layer3_Resource）
// 用途：门面类，Catalog 寻址、依赖链加载、包缓存与包级引用计数（AcquireBundle / ReleaseTicket）。

using System.IO;
using UnityEngine;

namespace vFramework.BaseLayer.AssetLayer.ABundleLayer
{
    /// <summary>
    /// ③ 抽象资源层门面：Catalog 寻址 + 依赖加载 + 包缓存 + 引用计数（LoadTicket）。
    /// </summary>
    public class ABundleResourceSystem : IABundleResourceSystem
    {
        #region 字段

        readonly CatalogProvider _catalog = new();
        readonly DependencyResolver _dependencies = new();
        readonly BundleCache _cache = new();
        readonly BundleRefCounter _refCounter = new();

        string _bundleRootPath;

        #endregion

        #region 属性

        public bool IsInitialized { get; private set; }
        public AssetCatalog Catalog => _catalog.Catalog;

        #endregion

        #region 初始化

        public bool Initialize(
            string bundleRootPath,
            string catalogFileName,
            string manifestFileName,
            bool loadManifest)
        {
            _bundleRootPath = bundleRootPath;
            _catalog.Load(_bundleRootPath, catalogFileName);

            if (loadManifest)
            {
                _dependencies.Load(_bundleRootPath, manifestFileName);
            }

            IsInitialized = true;
            return true;
        }

        public void Shutdown()
        {
            UnloadAll(false);
            _dependencies.Clear();
            _catalog.Clear();
            IsInitialized = false;
        }

        #endregion

        #region 寻址

        public bool TryResolveLocation(string location, out AssetLocationEntry entry) =>
            _catalog.TryResolveLocation(location, out entry);

        #endregion

        #region 包加载与释放

        public ABundleLoadTicket AcquireBundle(string bundleName)
        {
            var ticket = new ABundleLoadTicket { MainBundleName = bundleName };
            if (string.IsNullOrEmpty(bundleName))
            {
                return ticket;
            }

            var loadOrder = _dependencies.IsLoaded
                ? _dependencies.GetLoadOrder(bundleName)
                : new[] { bundleName };

            ticket.RetainedBundleNames = loadOrder;

            for (var i = 0; i < loadOrder.Length; i++)
            {
                var name = loadOrder[i];
                if (!_cache.Contains(name))
                {
                    var path = Path.Combine(_bundleRootPath, name);
                    if (_cache.LoadFromFile(name, path) == null)
                    {
                        ticket.Invalidate();
                        return ticket;
                    }
                }

                _refCounter.Retain(name);
            }

            ticket.IsValid = _cache.Contains(bundleName);
            return ticket;
        }

        public AssetBundle GetBundle(ABundleLoadTicket ticket)
        {
            if (ticket == null || !ticket.IsValid)
            {
                return null;
            }

            return _cache.Get(ticket.MainBundleName);
        }

        public void ReleaseTicket(ABundleLoadTicket ticket)
        {
            if (ticket == null || !ticket.IsValid)
            {
                return;
            }

            for (var i = ticket.RetainedBundleNames.Length - 1; i >= 0; i--)
            {
                var name = ticket.RetainedBundleNames[i];
                if (_refCounter.Release(name) > 0)
                {
                    continue;
                }

                if (_refCounter.ShouldUnload(name))
                {
                    _cache.Unload(name, false);
                }
            }

            ticket.Invalidate();
        }

        public int GetRefCount(string bundleName) => _refCounter.GetRefCount(bundleName);

        public string[] GetLoadedBundleNames() => _cache.GetLoadedBundleNames();

        public void UnloadAll(bool unloadAllLoadedObjects = false)
        {
            _cache.UnloadAll(unloadAllLoadedObjects);
            _refCounter.Clear();
            _dependencies.Clear();
        }

        #endregion
    }
}
