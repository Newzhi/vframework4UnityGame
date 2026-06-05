using UnityEngine;

namespace vFramework.BaseLayer.AssetLayer
{
    /// <summary>
    /// ResMgr 对外暴露的引用计数句柄，每次 LoadAsync 返回独立句柄，Release 递减引用。
    /// </summary>
    public interface IAssetHandle
    {
        string Location { get; }
        bool IsValid { get; }
        UnityEngine.Object RawObject { get; }
        T GetAsset<T>() where T : UnityEngine.Object;
        void Release();
    }
}
