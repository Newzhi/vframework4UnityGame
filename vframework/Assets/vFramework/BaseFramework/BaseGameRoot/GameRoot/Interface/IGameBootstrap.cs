namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 启动装配：集中注册 Service + Module。由热更层实现并挂到 GameRoot Bootstrap Behaviour（必填）。
    /// </summary>
    public interface IGameBootstrap
    {
        void Configure(IServiceRegistry services, IModuleRegistry modules);
    }
}
