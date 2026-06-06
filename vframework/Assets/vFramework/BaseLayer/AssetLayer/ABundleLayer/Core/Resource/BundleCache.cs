using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace vFramework.BaseLayer.AssetLayer.ABundleLayer
{
    #region 包缓存

    /// <summary>已加载 AssetBundle 实例缓存。</summary>
    public class BundleCache
    {
        readonly Dictionary<string, AssetBundle> _loaded = new();

        public bool Contains(string bundleName) =>
            _loaded.TryGetValue(bundleName, out var bundle) && bundle != null;

        public AssetBundle Get(string bundleName) =>
            Contains(bundleName) ? _loaded[bundleName] : null;

        public AssetBundle LoadFromFile(string bundleName, string filePath)
        {
            if (Contains(bundleName))
            {
                return _loaded[bundleName];
            }

            if (!File.Exists(filePath))
            {
                Debug.LogError($"[ABundle] 包文件不存在: {filePath}");
                return null;
            }

            var bundle = AssetBundle.LoadFromFile(filePath);
            if (bundle == null)
            {
                Debug.LogError($"[ABundle] LoadFromFile 失败: {filePath}");
                return null;
            }

            _loaded[bundleName] = bundle;
            Debug.Log($"[ABundle] 已加载包: {bundleName}");
            return bundle;
        }

        public void Unload(string bundleName, bool unloadAllLoadedObjects)
        {
            if (!_loaded.TryGetValue(bundleName, out var bundle) || bundle == null)
            {
                return;
            }

            bundle.Unload(unloadAllLoadedObjects);
            _loaded.Remove(bundleName);
        }

        public void UnloadAll(bool unloadAllLoadedObjects)
        {
            foreach (var pair in _loaded)
            {
                pair.Value?.Unload(unloadAllLoadedObjects);
            }

            _loaded.Clear();
        }

        public string[] GetLoadedBundleNames()
        {
            var names = new string[_loaded.Count];
            _loaded.Keys.CopyTo(names, 0);
            return names;
        }
    }

    #endregion

    #region 引用计数

    /// <summary>包级引用计数。</summary>
    public class BundleRefCounter
    {
        readonly Dictionary<string, int> _counts = new();

        public int GetRefCount(string bundleName) =>
            _counts.TryGetValue(bundleName, out var count) ? count : 0;

        public void Retain(string bundleName) =>
            _counts[bundleName] = GetRefCount(bundleName) + 1;

        public int Release(string bundleName)
        {
            var count = GetRefCount(bundleName);
            if (count <= 0)
            {
                return 0;
            }

            count--;
            if (count <= 0)
            {
                _counts.Remove(bundleName);
                return 0;
            }

            _counts[bundleName] = count;
            return count;
        }

        public bool ShouldUnload(string bundleName) => GetRefCount(bundleName) <= 0;

        public void Clear() => _counts.Clear();
    }

    #endregion
}
