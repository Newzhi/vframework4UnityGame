// CatalogProvider.cs — ③ 抽象资源层（Layer3_Resource）
// 用途：加载 AssetCatalog.json 并按 location 寻址；DependencyResolver 解析 Manifest 依赖顺序。

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace vFramework.BaseLayer.AssetLayer.ABundleLayer
{
    #region Catalog 索引

    /// <summary>加载并查询 AssetCatalog.json。</summary>
    public class CatalogProvider
    {
        public AssetCatalog Catalog { get; private set; }

        public bool Load(string bundleRootPath, string catalogFileName)
        {
            var path = Path.Combine(bundleRootPath, catalogFileName);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[ABundle] 未找到 Catalog: {path}");
                Catalog = new AssetCatalog();
                return false;
            }

            Catalog = JsonUtility.FromJson<AssetCatalog>(File.ReadAllText(path));
            Catalog?.BuildRuntimeIndex();
            return Catalog != null;
        }

        public bool TryResolveLocation(string location, out AssetLocationEntry entry)
        {
            entry = null;
            return Catalog != null && Catalog.TryGetLocation(location, out entry);
        }

        public void Clear() => Catalog = null;
    }

    #endregion

    #region 依赖解析

    /// <summary>读取 Manifest 并计算包加载顺序。</summary>
    public class DependencyResolver
    {
        AssetBundleManifest _manifest;

        public bool IsLoaded => _manifest != null;

        public bool Load(string bundleRootPath, string manifestFileName)
        {
            Clear();
            var path = Path.Combine(bundleRootPath, manifestFileName);
            if (!File.Exists(path))
            {
                Debug.LogError($"[ABundle] 找不到 Manifest: {path}");
                return false;
            }

            var manifestBundle = AssetBundle.LoadFromFile(path);
            if (manifestBundle == null)
            {
                return false;
            }

            _manifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            manifestBundle.Unload(false);
            return _manifest != null;
        }

        public string[] GetLoadOrder(string bundleName)
        {
            var order = new List<string>();
            CollectDependencies(bundleName, order);
            if (!order.Contains(bundleName))
            {
                order.Add(bundleName);
            }

            return order.ToArray();
        }

        void CollectDependencies(string bundleName, List<string> order)
        {
            if (_manifest == null)
            {
                return;
            }

            foreach (var dep in _manifest.GetAllDependencies(bundleName))
            {
                if (!order.Contains(dep))
                {
                    CollectDependencies(dep, order);
                    order.Add(dep);
                }
            }
        }

        public void Clear() => _manifest = null;
    }

    #endregion
}
