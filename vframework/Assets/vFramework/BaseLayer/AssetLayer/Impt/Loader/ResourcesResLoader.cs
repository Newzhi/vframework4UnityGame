using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace vFramework.BaseLayer.AssetLayer
{
    /// <summary>
    /// Resources 目录加载，location 为相对 Resources 的路径（无扩展名）。
    /// 可选前缀 res:// 会被剥离。
    /// </summary>
    public sealed class ResourcesResLoader : IResLoader
    {
        public const string Prefix = "res://";

        public bool CanLoad(string location)
        {
            var path = NormalizeLocation(location);
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            // 带其它后端前缀时不由 Resources 处理。
            if (location.StartsWith(AddressablesResLoader.Prefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        public async Task<ILoaderHandle> LoadAsync<T>(string location, CancellationToken cancellationToken = default)
            where T : UnityEngine.Object
        {
            var path = NormalizeLocation(location);
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Resources location is empty.", nameof(location));
            }

            var request = Resources.LoadAsync<T>(path);
            while (!request.isDone)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
            }

            if (request.asset == null)
            {
                throw new InvalidOperationException($"Resources asset not found: {path}");
            }

            return new ResourcesLoaderHandle(request.asset);
        }

        public static string NormalizeLocation(string location)
        {
            if (string.IsNullOrWhiteSpace(location))
            {
                return string.Empty;
            }

            return location.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)
                ? location.Substring(Prefix.Length)
                : location;
        }

        private sealed class ResourcesLoaderHandle : ILoaderHandle
        {
            public UnityEngine.Object Asset { get; }

            public ResourcesLoaderHandle(UnityEngine.Object asset)
            {
                Asset = asset;
            }

            public void ReleaseBackend()
            {
                // Resources 无单资源 Release，由 ResMgr 移除缓存后依赖 UnloadUnusedAssets。
            }
        }
    }
}
