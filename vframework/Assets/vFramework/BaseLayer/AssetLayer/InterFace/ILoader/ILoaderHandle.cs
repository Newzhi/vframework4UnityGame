using UnityEngine;

namespace vFramework.BaseLayer.AssetLayer
{
    /// <summary>
    /// 底层加载器返回的句柄，由 ResMgr 在引用计数归零时调用 ReleaseBackend。
    /// </summary>
    public interface ILoaderHandle
    {
        Object Asset { get; }
        void ReleaseBackend();
    }
}
