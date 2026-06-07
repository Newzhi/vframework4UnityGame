// IABundleLoader.cs — ④ 加载器（Layer4_Loader）
// 用途：加载器对外接口，业务通过 location 加载资源（如 LoadHandle<T>("icon/3")）。

using System;
using UnityEngine;
using vFramework.BaseLayer.AssetLayer;

namespace vFramework.BaseLayer.AssetLayer.ABundleLayer
{
    /// <summary>
    /// ④ 加载器对外接口：Initialize、同步/异步 Load、卸载。
    /// </summary>
    public interface IABundleLoader
    {
        bool IsInitialized { get; }
        ABundleLoadMode LoadMode { get; }
        AssetCatalog Catalog { get; }
        IABundleResourceSystem Resources { get; }

        void Initialize(string catalogRelativePath = null);
        void InitializeFromRules(ABundleBuildRules rules, ABundleLoadMode? overrideMode = null);
        void InitializeWithRootPath(
            string bundleRootPath,
            string catalogFileName,
            string manifestFileName,
            ABundleLoadMode loadMode = ABundleLoadMode.RuntimeBundle);
        void Shutdown();

        IAssetHandle LoadHandle<T>(string location) where T : UnityEngine.Object;
        T LoadAsset<T>(string location) where T : UnityEngine.Object;
        void LoadAssetAsync<T>(string location, Action<T> onComplete) where T : UnityEngine.Object;
        void LoadHandleAsync<T>(string location, Action<IAssetHandle> onComplete) where T : UnityEngine.Object;

        void LoadBundle(string bundleName);
        void ReleaseAsset(string location);
        void ReleaseBundle(string bundleName);
        void UnloadAll(bool unloadAllLoadedObjects = false);

        bool ContainsLocation(string location);
        int GetBundleRefCount(string bundleName);
        string[] GetLoadedBundleNames();
    }
}
