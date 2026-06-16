using System;

namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 服务注册表（Composition Root 专用）。业务模块在 <see cref="IGameModule.Init"/> 中 Get 并缓存，避免热路径 Service Locator。
    /// </summary>
    public interface IServiceRegistry
    {
        void Register<T>(T instance) where T : class;

        T Get<T>() where T : class;

        bool TryGet<T>(out T instance) where T : class;

        bool IsRegistered<T>() where T : class;

        void Clear();
    }
}
