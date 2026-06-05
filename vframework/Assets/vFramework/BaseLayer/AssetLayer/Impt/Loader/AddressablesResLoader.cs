using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace vFramework.BaseLayer.AssetLayer
{
    /// <summary>
    /// Addressables 加载；可选前缀 addr://。
    /// </summary>
    public sealed class AddressablesResLoader : IResLoader
    {
        public const string Prefix = "addr://";

        public bool CanLoad(string location)
        {
            if (string.IsNullOrWhiteSpace(location))
            {
                return false;
            }

            // 无 addr:// 前缀时不参与 Composite 自动路由，避免抢占 Resources。
            if (!location.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return !string.IsNullOrEmpty(NormalizeLocation(location));
        }

        public async Task<ILoaderHandle> LoadAsync<T>(string location, CancellationToken cancellationToken = default)
            where T : UnityEngine.Object
        {
            var normalized = NormalizeLocation(location);
            if (string.IsNullOrEmpty(normalized))
            {
                throw new ArgumentException("Addressables location is empty.", nameof(location));
            }

            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(normalized);
            try
            {
                await handle.Task;
            }
            catch
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }

                throw;
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }

                throw new InvalidOperationException($"Addressables load failed: {normalized}");
            }

            return new AddressablesLoaderHandle<T>(handle);
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

        private sealed class AddressablesLoaderHandle<T> : ILoaderHandle where T : UnityEngine.Object
        {
            private AsyncOperationHandle<T> _handle;

            public UnityEngine.Object Asset => _handle.Result;

            public AddressablesLoaderHandle(AsyncOperationHandle<T> handle)
            {
                _handle = handle;
            }

            public void ReleaseBackend()
            {
                if (_handle.IsValid())
                {
                    Addressables.Release(_handle);
                }

                _handle = default;
            }
        }
    }
}
