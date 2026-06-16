namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 可选：模块需参与 LateUpdate 相位时实现。执行顺序仍由 <see cref="IGameModule.Priority"/> 决定。
    /// </summary>
    public interface ILateUpdateModule : IGameModule
    {
        void LateUpdate(float deltaTime);
    }
}
