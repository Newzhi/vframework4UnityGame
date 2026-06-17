namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// <see cref="IGameFlowContext"/> 默认实现；UserData 在每次 ChangeState 时更新。
    /// </summary>
    internal sealed class GameFlowContext : IGameFlowContext
    {
        public GameFlowContext(IServiceRegistry services, IGameFlowService flow)
        {
            Services = services;
            Flow = flow;
        }

        public IServiceRegistry Services { get; }
        public IGameFlowService Flow { get; }
        public object UserData { get; internal set; }
    }
}
