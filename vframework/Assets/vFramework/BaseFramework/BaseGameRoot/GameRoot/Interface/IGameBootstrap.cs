namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 启动装配：集中注册 Service + Module。由热更层实现；通过 <see cref="GameRoot.TryStart"/> 接入。
    /// </summary>
    public interface IGameBootstrap
    {
        void Configure(IServiceRegistry services, IModuleRegistry modules);
    }
}
