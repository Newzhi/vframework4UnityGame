namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 可选：模块需参与 FixedUpdate 相位时实现。执行顺序仍由 <see cref="IGameModule.Priority"/> 决定。
    /// </summary>
    public interface IFixedUpdateModule : IGameModule
    {
        void FixedUpdate(float fixedDeltaTime);
    }
}
