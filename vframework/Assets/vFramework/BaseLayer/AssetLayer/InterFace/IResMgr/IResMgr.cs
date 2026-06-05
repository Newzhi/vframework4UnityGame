using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace vFramework.BaseLayer.AssetLayer
{
    /// <summary>
    /// 资源管理器：引用计数、并发去重、对外统一加载入口。
    /// </summary>
    public interface IResMgr
    {
        Task<IAssetHandle> LoadAsync<T>(string location, CancellationToken cancellationToken = default) 
            where T : UnityEngine.Object;

        Task<GameObject> InstantiateAsync(
            string location,
            Transform parent = null,
            CancellationToken cancellationToken = default);

        int GetRefCount(string location);
    }
}
