using System;
using System.Collections.Generic;

namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 默认 IOC 容器：接口 → 单例实例。无反射、无 per-frame 分配。
    /// </summary>
    public sealed class ServiceContainer : IServiceRegistry
    {
        private readonly Dictionary<Type, object> _map = new Dictionary<Type, object>(16);

        public void Register<T>(T instance) where T : class
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            _map[typeof(T)] = instance;
        }

        public T Get<T>() where T : class
        {
            if (_map.TryGetValue(typeof(T), out object value))
                return (T)value;

            throw new InvalidOperationException(
                $"Service not registered: {typeof(T).FullName}. Register it in IGameBootstrap.Configure.");
        }

        public bool TryGet<T>(out T instance) where T : class
        {
            if (_map.TryGetValue(typeof(T), out object value))
            {
                instance = (T)value;
                return true;
            }

            instance = null;
            return false;
        }

        public bool IsRegistered<T>() where T : class => _map.ContainsKey(typeof(T));

        public void Clear() => _map.Clear();
    }
}
