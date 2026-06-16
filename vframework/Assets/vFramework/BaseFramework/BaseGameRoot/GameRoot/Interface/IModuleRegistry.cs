namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 模块注册表：仅在 Bootstrap <see cref="IGameBootstrap.Configure"/> 阶段写入。
    /// </summary>
    public interface IModuleRegistry
    {
        void AddModule(IGameModule module);
    }
}
