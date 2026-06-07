// IABundleResourceSystem.cs — ③ 抽象资源层（Layer3_Resource）
// 用途：抽象资源层对外接口，定义初始化、寻址、包获取与票据释放。

using UnityEngine;

namespace vFramework.BaseLayer.AssetLayer.ABundleLayer
{
    /// <summary>
    /// ③ 抽象资源层：包缓存、引用计数、Catalog 寻址、依赖解析。
    /// </summary>
    public interface IABundleResourceSystem
    {
        bool IsInitialized { get; }
        AssetCatalog Catalog { get; }

        bool Initialize(string bundleRootPath, string catalogFileName, string manifestFileName, bool loadManifest);
        void Shutdown();

        bool TryResolveLocation(string location, out AssetLocationEntry entry);
        ABundleLoadTicket AcquireBundle(string bundleName);
        AssetBundle GetBundle(ABundleLoadTicket ticket);
        void ReleaseTicket(ABundleLoadTicket ticket);

        int GetRefCount(string bundleName);
        string[] GetLoadedBundleNames();
        void UnloadAll(bool unloadAllLoadedObjects = false);
    }
}
