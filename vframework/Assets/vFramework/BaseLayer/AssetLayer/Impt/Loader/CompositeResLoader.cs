using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace vFramework.BaseLayer.AssetLayer
{
    /// <summary>
    /// 按顺序尝试多个 IResLoader；支持前缀 res:// / addr:// 直达指定后端。
    /// </summary>
    public sealed class CompositeResLoader : IResLoader
    {
        private readonly IResLoader[] _loaders;

        public CompositeResLoader(params IResLoader[] loaders)
        {
            if (loaders == null || loaders.Length == 0)
            {
                throw new ArgumentException("At least one loader is required.", nameof(loaders));
            }

            _loaders = loaders;
        }

        public bool CanLoad(string location)
        {
            var loader = ResolveLoader(location, onlyCheck: true);
            return loader != null;
        }

        public Task<ILoaderHandle> LoadAsync<T>(string location, CancellationToken cancellationToken = default)
            where T : UnityEngine.Object
        {
            var loader = ResolveLoader(location, onlyCheck: false);
            if (loader == null)
            {
                throw new InvalidOperationException($"No loader can load location: {location}");
            }

            return loader.LoadAsync<T>(location, cancellationToken);
        }

        private IResLoader ResolveLoader(string location, bool onlyCheck)
        {
            if (string.IsNullOrWhiteSpace(location))
            {
                return null;
            }

            if (location.StartsWith(ResourcesResLoader.Prefix, StringComparison.OrdinalIgnoreCase))
            {
                return Find<ResourcesResLoader>() ?? FindFallback(typeof(ResourcesResLoader));
            }

            if (location.StartsWith(AddressablesResLoader.Prefix, StringComparison.OrdinalIgnoreCase))
            {
                return Find<AddressablesResLoader>() ?? FindFallback(typeof(AddressablesResLoader));
            }

            for (var i = 0; i < _loaders.Length; i++)
            {
                if (_loaders[i].CanLoad(location))
                {
                    return _loaders[i];
                }
            }

            return onlyCheck ? null : null;
        }

        private IResLoader Find<T>() where T : class, IResLoader
        {
            for (var i = 0; i < _loaders.Length; i++)
            {
                if (_loaders[i] is T loader)
                {
                    return loader;
                }
            }

            return null;
        }

        private IResLoader FindFallback(Type loaderType)
        {
            for (var i = 0; i < _loaders.Length; i++)
            {
                if (_loaders[i].GetType() == loaderType)
                {
                    return _loaders[i];
                }
            }

            return null;
        }
    }
}
