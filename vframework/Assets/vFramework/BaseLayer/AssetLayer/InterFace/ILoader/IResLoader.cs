using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace vFramework.BaseLayer.AssetLayer
{
    /// <summary>
    /// 资源后端适配器：Resources / Addressables 等各自实现。
    /// </summary>
    public interface IResLoader
    {
        bool CanLoad(string location);

        Task<ILoaderHandle> LoadAsync<T>(string location, CancellationToken cancellationToken = default)
            where T : UnityEngine.Object;
    }
}
